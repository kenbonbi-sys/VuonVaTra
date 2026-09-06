using NUnit.Framework;
using VuonNho.Core;

namespace VuonNho.Tests
{
    /// <summary>Offline, quay lai va giao dich — nhom de sinh loi cong lai tien nhat.</summary>
    public sealed class LifecycleTests
    {
        FakeClock _clock;
        MemorySaveRepository _repository;
        GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _session = TestKit.NewSession(out _clock, out _repository);
        }

        void PlantAllAndUnlockRobot()
        {
            for (int i = 0; i < _session.State.Plots.Count; i++)
                if (_session.State.Plots[i].Unlocked)
                    _session.Plant(i, DefaultContent.CropMint);

            _session.State.Coins = 200;
            var result = _session.Purchase(DefaultContent.UpgradeRobot);
            Assert.IsTrue(result.Success, result.FailureReason);
        }

        // ---- Cap offline --------------------------------------------------------------

        [Test]
        public void BonGioVaHaiMuoiBonGioChoCungPhanTienDo()
        {
            PlantAllAndUnlockRobot();
            _session.Suspend();
            string snapshot = _repository.Main;

            _clock.AdvanceUtcOnly(4 * 3600000);
            _session.ResumeFromBackground();
            string fourHours = TestKit.Signature(_session.State, _session.Catalog);

            // Mo lai dung snapshot do nhung de 24 gio troi qua.
            var clock24 = new FakeClock { Utc = ReadCheckpoint(snapshot) + 24L * 3600000, Monotonic = 0 };
            var repository24 = new MemorySaveRepository { Main = snapshot };
            var session24 = new GameSession(TestKit.Catalog(), clock24, repository24, new RecordingLogger(), "test");
            session24.Initialize();

            Assert.AreEqual(fourHours, TestKit.Signature(session24.State, session24.Catalog),
                            "Bo game 24 gio chi duoc mo phong 4 gio.");
        }

        static long ReadCheckpoint(string json)
        {
            return JsonValue.Parse(json).GetLong("checkpointUtcMs", 0);
        }

        [Test]
        public void PhanDuTrenCapKhongDuocNhanOLanMoTiepTheo()
        {
            PlantAllAndUnlockRobot();
            _session.Suspend();

            _clock.AdvanceUtcOnly(24 * 3600000);
            _session.ResumeFromBackground();
            long coinsAfterFirstReturn = _session.State.Coins;

            _session.Suspend();
            _session.ResumeFromBackground();   // quay lai ngay, khong co thoi gian troi

            Assert.AreEqual(coinsAfterFirstReturn, _session.State.Coins,
                            "20 gio du khong duoc nhan o lan mo tiep theo.");
        }

        // ---- Chong cong lai tien ------------------------------------------------------

        [Test]
        public void GoiResumeNhieuLanChiCongTienMotLan()
        {
            PlantAllAndUnlockRobot();
            _session.Suspend();

            _clock.AdvanceUtcOnly(600000);
            _session.ResumeFromBackground();
            long coinsAfterResume = _session.State.Coins;
            Assert.Greater(coinsAfterResume, 0);

            // Callback focus/pause co the ban ra nhieu lan cho cung mot lan quay lai.
            _session.ResumeFromBackground();
            _session.ResumeFromBackground();

            Assert.AreEqual(coinsAfterResume, _session.State.Coins);
        }

        [Test]
        public void SuspendHaiLanKhongLamDoiTienDo()
        {
            PlantAllAndUnlockRobot();
            _clock.AdvanceBoth(60000);
            _session.Tick();

            _session.Suspend();
            string afterFirst = TestKit.Signature(_session.State, _session.Catalog);
            _session.Suspend();

            Assert.AreEqual(afterFirst, TestKit.Signature(_session.State, _session.Catalog));
        }

        [Test]
        public void XemBaoCaoQuayLaiKhongCongThemTien()
        {
            PlantAllAndUnlockRobot();
            _session.Suspend();
            _clock.AdvanceUtcOnly(600000);
            _session.ResumeFromBackground();

            Assert.IsNotNull(_session.State.PendingOfflineSummary);
            long coins = _session.State.Coins;

            _session.AcknowledgeOfflineSummary();
            Assert.AreEqual(coins, _session.State.Coins);
            Assert.IsNull(_session.State.PendingOfflineSummary);
        }

        [Test]
        public void QuangNghiNganKhongHienBaoCaoNhungVanApDungTienDo()
        {
            PlantAllAndUnlockRobot();
            _session.Suspend();

            _clock.AdvanceUtcOnly(31000);
            _session.ResumeFromBackground();

            Assert.IsNull(_session.State.PendingOfflineSummary, "Nghi duoi mot phut khong can popup.");
            Assert.AreEqual(31000, _session.State.SimulationTimeMs, "Tien do van duoc ap dung.");
        }

        // ---- Truoc robot khi offline --------------------------------------------------

        [Test]
        public void TruocRobotThiOfflineChiChoCayChinKhongThuThem()
        {
            for (int i = 0; i < _session.State.Plots.Count; i++)
                if (_session.State.Plots[i].Unlocked)
                    _session.Plant(i, DefaultContent.CropMint);

            _session.Suspend();
            _clock.AdvanceUtcOnly(3600000);
            _session.ResumeFromBackground();

            Assert.AreEqual(0, _session.State.Coins);
            Assert.AreEqual(0, _session.State.InventoryOf(DefaultContent.CropMint));
            for (int i = 0; i < 4; i++)
                Assert.AreEqual(PlotPhase.Ready, _session.State.Plot(i).Phase);
        }

        // ---- Dong ho ------------------------------------------------------------------

        [Test]
        public void ChinhGioLuiKhongTaoTienDoAmVaKhongLamHongState()
        {
            PlantAllAndUnlockRobot();
            _session.Suspend();
            long coinsBefore = _session.State.Coins;

            _clock.Utc -= 5 * 3600000;   // nguoi choi chinh dong ho lui
            string diagnostic = null;
            _session.Diagnostic += delegate(string message) { diagnostic = message; };
            _session.ResumeFromBackground();

            Assert.AreEqual(coinsBefore, _session.State.Coins);
            Assert.IsNotNull(diagnostic, "Phai ghi lai diagnostic khi dong ho lui.");
            Assert.AreEqual(_clock.Utc, _session.State.CheckpointUtcMs, "Moc duoc dat lai ve hien tai.");
        }

        [Test]
        public void ChinhGioTienVanBiChanBoiCap()
        {
            PlantAllAndUnlockRobot();
            _session.Suspend();
            string snapshot = _repository.Main;
            long checkpoint = ReadCheckpoint(snapshot);

            // Nghi dung bang cap: day la moc so sanh.
            var capClock = new FakeClock { Utc = checkpoint + _session.Catalog.Balance.OfflineCapMs };
            var capSession = new GameSession(TestKit.Catalog(), capClock,
                                             new MemorySaveRepository { Main = snapshot },
                                             new RecordingLogger(), "test");
            capSession.Initialize();

            // Nghi mot nam: phai cho dung ket qua nhu nghi 4 gio, khong hon mot xu nao.
            _clock.AdvanceUtcOnly(365L * 24 * 3600000);
            _session.ResumeFromBackground();

            Assert.Greater(capSession.State.Coins, 0, "Bai test can co thu nhap trong quang nghi.");
            Assert.AreEqual(capSession.State.Coins, _session.State.Coins,
                            "Chinh gio tien khong duoc vuot qua cap offline.");
        }

        // ---- Giao dich ----------------------------------------------------------------

        [Test]
        public void ThieuXuThiKhongMuaDuoc()
        {
            var result = _session.Purchase(DefaultContent.UpgradeRobot);
            Assert.IsFalse(result.Success);
            Assert.AreEqual(0, _session.State.Coins);
            Assert.IsFalse(_session.State.RobotUnlocked);
        }

        [Test]
        public void TruocRobotThiCuaHangChiCoRobot()
        {
            _session.State.Coins = 5000;

            var blocked = _session.Purchase(DefaultContent.UpgradeExpand8);
            Assert.IsFalse(blocked.Success);
            Assert.AreEqual(5000, _session.State.Coins);

            var robot = _session.Purchase(DefaultContent.UpgradeRobot);
            Assert.IsTrue(robot.Success, robot.FailureReason);

            var expand = _session.Purchase(DefaultContent.UpgradeExpand8);
            Assert.IsTrue(expand.Success, expand.FailureReason);
            Assert.AreEqual(8, _session.State.UnlockedPlotCount());
        }

        [Test]
        public void MuaHaiLanCungMotNangCapChiTruTienMotLan()
        {
            _session.State.Coins = 1000;
            Assert.IsTrue(_session.Purchase(DefaultContent.UpgradeRobot).Success);
            long afterFirst = _session.State.Coins;

            var again = _session.Purchase(DefaultContent.UpgradeRobot);
            Assert.IsFalse(again.Success);
            Assert.AreEqual(afterFirst, _session.State.Coins);
        }

        [Test]
        public void DieuKienMoDauCanMoCucTruoc()
        {
            _session.State.Coins = 5000;
            Assert.IsTrue(_session.Purchase(DefaultContent.UpgradeRobot).Success);

            var tooEarly = _session.Purchase(DefaultContent.UpgradeStrawberry);
            Assert.IsFalse(tooEarly.Success);

            Assert.IsTrue(_session.Purchase(DefaultContent.UpgradeChamomile).Success);
            Assert.IsTrue(_session.Purchase(DefaultContent.UpgradeStrawberry).Success);
            Assert.IsTrue(_session.State.UnlockedCropIds.Contains(DefaultContent.CropStrawberry));
            Assert.IsTrue(_session.State.UnlockedRecipeIds.Contains(DefaultContent.RecipeStrawberry));
        }

        [Test]
        public void BamThuHaiLanTrenCungMotOChiThuDuocMotLan()
        {
            _session.Plant(0, DefaultContent.CropMint);
            _clock.AdvanceBoth(30000);
            _session.Tick();

            var first = _session.HarvestAndReplant(0);
            var second = _session.HarvestAndReplant(0);

            Assert.IsTrue(first.Success, first.FailureReason);
            Assert.IsFalse(second.Success, "Lan bam thu hai khong duoc thu them.");
            Assert.AreEqual(_session.Catalog.Crop(DefaultContent.CropMint).Yield,
                            _session.State.InventoryOf(DefaultContent.CropMint));
        }

        [Test]
        public void KhongTrongDuocTrenODaKhoa()
        {
            var result = _session.Plant(8, DefaultContent.CropMint);
            Assert.IsFalse(result.Success);
            Assert.AreEqual(PlotPhase.Locked, _session.State.Plot(8).Phase);
        }

        [Test]
        public void KhongChonDuocCayChuaMoKhoa()
        {
            var result = _session.SetNextCrop(0, DefaultContent.CropStrawberry);
            Assert.IsFalse(result.Success);
        }

        [Test]
        public void BanThoCongTienDungGiaVaKhongLamAmKho()
        {
            // Ban cuc: may dang chon cong thuc bac ha nen kho cuc khong bi me nao tieu thu.
            _session.State.AddInventory(DefaultContent.CropChamomile, 5);

            Assert.IsTrue(_session.SellRaw(DefaultContent.CropChamomile, 1).Success);
            Assert.AreEqual(5, _session.State.Coins);

            var tooMany = _session.SellRaw(DefaultContent.CropChamomile, 99);
            Assert.IsFalse(tooMany.Success);
            Assert.AreEqual(4, _session.State.InventoryOf(DefaultContent.CropChamomile));

            Assert.IsTrue(_session.SellAllRaw(DefaultContent.CropChamomile).Success);
            Assert.AreEqual(25, _session.State.Coins);
            Assert.AreEqual(0, _session.State.InventoryOf(DefaultContent.CropChamomile));
        }

        [Test]
        public void NguyenLieuDaVaoMeThiKhongConTrongKhoDeBanTay()
        {
            int input = _session.Catalog.Recipe(DefaultContent.RecipeMint).InputCount;
            _session.State.AddInventory(DefaultContent.CropMint, input + 1);
            _session.SelectRecipe(DefaultContent.RecipeMint);

            Assert.IsTrue(_session.State.Machine.BatchRunning);
            Assert.AreEqual(1, _session.State.InventoryOf(DefaultContent.CropMint),
                            "Nguyen lieu cua me da bi tru khi me bat dau.");
        }

        [Test]
        public void HuongDanTiepTucDungBuocSauKhiLoadLai()
        {
            _session.Plant(0, DefaultContent.CropMint);
            Assert.AreEqual(TutorialStep.PlantAll,
                            TutorialGuide.CurrentStep(_session.State, _session.Catalog));

            for (int i = 1; i < 4; i++) _session.Plant(i, DefaultContent.CropMint);
            _session.SaveNow();

            var reopened = new GameSession(TestKit.Catalog(), _clock, _repository, new RecordingLogger(), "test");
            reopened.Initialize();

            Assert.AreEqual(TutorialStep.WaitAndHarvest,
                            TutorialGuide.CurrentStep(reopened.State, reopened.Catalog));
        }
    }
}
