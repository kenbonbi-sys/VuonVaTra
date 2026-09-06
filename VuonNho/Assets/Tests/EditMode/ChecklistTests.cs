using System;
using NUnit.Framework;
using VuonNho.Core;
using VuonNho.Infrastructure;

namespace VuonNho.Tests
{
    /// <summary>
    /// Cac o con thieu cua bang "Kiem thu logic bat buoc" (muc 10 ke hoach) sau khi ra soat:
    /// balance khac, chuyen mui gio, crash khi popup quay lai dang mo, va nghi khi cay da chin.
    /// </summary>
    public sealed class ChecklistTests
    {
        // ---- Phien ban: balance khac --------------------------------------------------

        [Test]
        public void SaveTaoBoiBalanceKhacVanNapDuocVaGiuGiaTriDaChot()
        {
            var oldCatalog = DefaultContent.Create(new BalanceConfig { Version = "balance-v0-cu" });
            var state = GameState.CreateNew(oldCatalog, 1000000000000);

            // Gia tri da chot cua vu va me dang chay, co tinh khac voi balance hien hanh.
            var plot = state.Plot(0);
            plot.Phase = PlotPhase.Growing;
            plot.CurrentCropId = DefaultContent.CropMint;
            plot.NextCropId = DefaultContent.CropMint;
            plot.StartAtMs = 0;
            plot.FinishAtMs = 45000;
            plot.PendingYield = 5;

            state.Machine.BatchRunning = true;
            state.Machine.BatchRecipeId = DefaultContent.RecipeMint;
            state.Machine.BatchStartAtMs = 0;
            state.Machine.BatchFinishAtMs = 10000;
            state.Machine.BatchOutputCoins = 999;

            string json = SaveSerializer.Write(new SaveSnapshot
            {
                BalanceVersion = oldCatalog.Balance.Version,
                BuildId = "test",
                State = state
            }, oldCatalog, true);

            var clock = new FakeClock { Utc = state.CheckpointUtcMs };
            var repository = new MemorySaveRepository { Main = json };
            var session = new GameSession(TestKit.Catalog(), clock, repository, new RecordingLogger(), "test");

            string diagnostic = null;
            session.Diagnostic += delegate(string message) { diagnostic = message; };

            var outcome = session.Initialize();

            Assert.AreEqual(LoadOutcome.LoadedPrimary, outcome, "Balance khac khong duoc lam hong save.");
            Assert.IsTrue(session.LoadedWithDifferentBalance);
            Assert.AreEqual("balance-v0-cu", session.LoadedBalanceVersion);
            Assert.IsNotNull(diagnostic, "Phai bao ro la save dung balance khac.");

            // Khong am tham chuyen doi du lieu: vu va me dang chay giu nguyen so cu.
            Assert.AreEqual(5, session.State.Plot(0).PendingYield);
            Assert.AreEqual(45000, session.State.Plot(0).FinishAtMs);
            Assert.AreEqual(999, session.State.Machine.BatchOutputCoins);

            session.DebugAdvance(10000);
            Assert.AreEqual(999, session.State.Coins, "Me dang chay tra dung so xu da chot luc bat dau.");
        }

        [Test]
        public void BalanceGiongNhauThiKhongBaoGiDuoc()
        {
            FakeClock clock;
            MemorySaveRepository repository;
            var first = TestKit.NewSession(out clock, out repository);
            first.SaveNow();

            var second = new GameSession(TestKit.Catalog(), clock, repository, new RecordingLogger(), "test");
            second.Initialize();

            Assert.IsFalse(second.LoadedWithDifferentBalance);
        }

        // ---- Dong ho: chuyen mui gio --------------------------------------------------

        [Test]
        public void DongHoTrongGameDungUtcNenDoiMuiGioKhongAnhHuong()
        {
            var clock = new SystemClock();
            long utcFromClock = clock.UtcNowMs;
            long utcReference = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                                .TotalMilliseconds;

            Assert.That(Math.Abs(utcFromClock - utcReference), Is.LessThan(2000),
                        "UtcNowMs phai la thoi gian UTC.");

            long offsetMs = (long)TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalMilliseconds;
            if (offsetMs != 0)
            {
                long localEpochMs = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;
                Assert.That(Math.Abs(utcFromClock - localEpochMs), Is.GreaterThan(Math.Abs(offsetMs) / 2),
                            "Dong ho khong duoc lay gio dia phuong; doi mui gio se lam sai quang nghi.");
            }
        }

        [Test]
        public void QuangNghiChiPhuThuocKhoangUtc()
        {
            // Hai may dat mui gio khac nhau nhung cung mot thoi diem UTC phai cho cung ket qua.
            long checkpointUtc = 1700000000000;
            string signatureA = RunOfflineFrom(checkpointUtc, checkpointUtc + 3600000);
            string signatureB = RunOfflineFrom(checkpointUtc, checkpointUtc + 3600000);
            Assert.AreEqual(signatureA, signatureB);

            string signatureLonger = RunOfflineFrom(checkpointUtc, checkpointUtc + 7200000);
            Assert.AreNotEqual(signatureA, signatureLonger, "Khoang UTC dai hon phai cho tien do khac.");
        }

        static string RunOfflineFrom(long checkpointUtcMs, long nowUtcMs)
        {
            var catalog = TestKit.Catalog();
            var state = GameState.CreateNew(catalog, checkpointUtcMs);
            state.RobotUnlocked = true;
            var simulation = new FarmSimulation(catalog);
            TestKit.PlantAllUnlocked(state, simulation, DefaultContent.CropMint);

            string json = SaveSerializer.Write(new SaveSnapshot
            {
                BalanceVersion = catalog.Balance.Version,
                BuildId = "test",
                State = state
            }, catalog, true);

            var clock = new FakeClock { Utc = nowUtcMs };
            var repository = new MemorySaveRepository { Main = json };
            var session = new GameSession(catalog, clock, repository, new RecordingLogger(), "test");
            session.Initialize();
            return TestKit.Signature(session.State, catalog);
        }

        // ---- Resume: crash khi popup quay lai dang mo ---------------------------------

        [Test]
        public void CrashKhiDangMoBaoCaoQuayLaiThiKhongCongLaiTienCuaQuangNghiCu()
        {
            FakeClock clock;
            MemorySaveRepository repository;
            var session = TestKit.NewSession(out clock, out repository);

            for (int i = 0; i < session.State.Plots.Count; i++)
                if (session.State.Plots[i].Unlocked)
                    session.Plant(i, DefaultContent.CropMint);
            session.State.Coins = 200;
            Assert.IsTrue(session.Purchase(DefaultContent.UpgradeRobot).Success);

            session.Suspend();
            clock.AdvanceUtcOnly(600000);
            session.ResumeFromBackground();

            long coinsAfterReturn = session.State.Coins;
            Assert.IsNotNull(session.State.PendingOfflineSummary, "Bai test can popup dang mo.");
            Assert.IsFalse(session.State.PendingOfflineSummary.Seen);

            // Crash: khong goi Acknowledge, khong Suspend, state trong RAM mat het.
            var afterCrash = new GameSession(TestKit.Catalog(), clock, repository, new RecordingLogger(), "test");
            afterCrash.Initialize();

            Assert.AreEqual(coinsAfterReturn, afterCrash.State.Coins,
                            "Quang nghi cu khong duoc cong lai sau khi crash.");
            Assert.IsNotNull(afterCrash.State.PendingOfflineSummary,
                             "Bao cao co the hien lai, nhung khong kem theo tien moi.");

            afterCrash.AcknowledgeOfflineSummary();
            Assert.AreEqual(coinsAfterReturn, afterCrash.State.Coins);
        }

        // ---- Truoc robot: nghi khi cay da chin -----------------------------------------

        [Test]
        public void TruocRobotNghiKhiCayDaChinThiVanChiLaChin()
        {
            FakeClock clock;
            MemorySaveRepository repository;
            var session = TestKit.NewSession(out clock, out repository);

            for (int i = 0; i < session.State.Plots.Count; i++)
                if (session.State.Plots[i].Unlocked)
                    session.Plant(i, DefaultContent.CropMint);

            clock.AdvanceBoth(30000);
            session.Tick();
            for (int i = 0; i < 4; i++)
                Assert.AreEqual(PlotPhase.Ready, session.State.Plot(i).Phase, "Bai test can cay da chin.");

            session.Suspend();
            clock.AdvanceUtcOnly(3600000);
            session.ResumeFromBackground();

            for (int i = 0; i < 4; i++)
                Assert.AreEqual(PlotPhase.Ready, session.State.Plot(i).Phase,
                                "Chua co robot thi cay chin cho vo thoi han.");
            Assert.AreEqual(0, session.State.InventoryOf(DefaultContent.CropMint));
            Assert.AreEqual(0, session.State.Coins);
        }
    }
}
