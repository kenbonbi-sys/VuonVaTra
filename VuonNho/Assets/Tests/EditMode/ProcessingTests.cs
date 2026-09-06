using NUnit.Framework;
using VuonNho.Core;

namespace VuonNho.Tests
{
    /// <summary>
    /// Day chuyen che bien tra: sau cai may giua thu hoach va quay tra, cong tho va luong.
    ///
    /// Trong tam la nhung thu de hong ma khong bao gi: hang khong di het chuoi, chay online va
    /// chay bu offline ra hai ket qua khac nhau, hoac het tien lam mat luon tho.
    /// </summary>
    public sealed class ProcessingTests
    {
        ContentCatalog _catalog;
        FarmSimulation _simulation;

        int StageInput { get { return _catalog.Stages[0].InputCount; } }
        int PackedInput { get { return _catalog.Recipe(DefaultContent.RecipeMint).PackedInputCount; } }

        [SetUp]
        public void SetUp()
        {
            _catalog = TestKit.Catalog();
            _simulation = new FarmSimulation(_catalog);
        }

        static string Packed(string cropId)
        {
            return ProcessChain.ItemId(cropId, ProcessChain.SuffixPacked);
        }

        // ---- chuoi cong doan ----------------------------------------------------------

        [Test]
        public void DayChuyenNoiLienTuLaTuoiToiTraDongGoi()
        {
            string expected = ProcessChain.SuffixFresh;
            foreach (var stage in _catalog.Stages)
            {
                Assert.AreEqual(expected, stage.InputSuffix ?? "",
                                "Dut mat xich o " + stage.Id);
                expected = stage.OutputSuffix;
            }
            Assert.AreEqual(ProcessChain.SuffixPacked, expected);
            Assert.AreEqual(ProcessChain.SuffixPacked, _catalog.PackedSuffix);
        }

        [Test]
        public void LaTuoiDiHetChuoiThanhTraDongGoi()
        {
            var state = TestKit.NewState(_catalog);
            TestKit.OpenWholeChain(state, _catalog, _catalog.Stages.Count);
            state.AddInventory(DefaultContent.CropMint, StageInput);

            _simulation.AdvanceTo(state, 600000);

            Assert.AreEqual(0, state.InventoryOf(DefaultContent.CropMint),
                            "La tuoi phai duoc dung het.");
            // Hai la vao dau chuoi phai ra thanh hai tra dong goi — hoac da bi quay tra pha het.
            Assert.That(state.InventoryOf(Packed(DefaultContent.CropMint)) > 0 || state.Coins > 0,
                        "Khong co gi di ra o cuoi chuoi.");
        }

        [Test]
        public void MayChuaMuaThiHangDungLaiODauChang()
        {
            var state = TestKit.NewState(_catalog);
            state.HiredWorkers = 3;
            state.StaffedWorkers = 3;
            // Tat quay tra: bai nay do day chuyen, ma quay tra thi cung an la tuoi.
            state.Machine.SelectedRecipeId = null;
            // Chi mua may dau tien.
            state.Station(DefaultStages.Wither).Owned = true;
            state.AddInventory(DefaultContent.CropMint, StageInput * 2);

            _simulation.AdvanceTo(state, 300000);

            Assert.AreEqual(0, state.InventoryOf(DefaultContent.CropMint));
            Assert.AreEqual(StageInput * 2, state.InventoryOf(
                ProcessChain.ItemId(DefaultContent.CropMint, ProcessChain.SuffixWithered)),
                "Hang phai dung lai ngay sau chang da mua.");
        }

        // ---- tho va luong -------------------------------------------------------------

        [Test]
        public void ItThoHonSoMayThiKhongChayHetCungLuc()
        {
            var state = TestKit.NewState(_catalog);
            TestKit.OpenWholeChain(state, _catalog, 1);
            state.Coins = 100000;
            state.AddInventory(DefaultContent.CropMint, 100);

            _simulation.AdvanceTo(state, 1000);

            Assert.AreEqual(1, state.RunningStationCount(),
                            "Mot tho thi chi mot may duoc chay.");
        }

        [Test]
        public void HetXuThiThoNghiChuKhongMatNguoi()
        {
            var state = TestKit.NewState(_catalog);
            TestKit.OpenWholeChain(state, _catalog, 3);
            state.Coins = 0;
            // Tat quay tra: neu no van ban tra thi trong tui lai co xu va bai nay khong con do
            // duoc chuyen "het xu".
            state.Machine.SelectedRecipeId = null;
            state.AddInventory(DefaultContent.CropMint, 40);

            _simulation.AdvanceTo(state, _catalog.Balance.PayrollPeriodMs + 1000);

            Assert.AreEqual(3, state.HiredWorkers, "Tho khong duoc tu bien mat.");
            Assert.AreEqual(0, state.StaffedWorkers, "Khong du xu thi khong ai chay may ky nay.");
            Assert.GreaterOrEqual(state.Coins, 0, "Xu khong duoc am.");
        }

        [Test]
        public void TraDuLuongThiDayChuyenChayLai()
        {
            var state = TestKit.NewState(_catalog);
            TestKit.OpenWholeChain(state, _catalog, 2);
            state.Coins = 0;
            state.Machine.SelectedRecipeId = null;
            state.AddInventory(DefaultContent.CropMint, 40);

            _simulation.AdvanceTo(state, _catalog.Balance.PayrollPeriodMs + 1000);
            Assert.AreEqual(0, state.StaffedWorkers);

            state.Coins = 10000;
            _simulation.AdvanceTo(state, state.NextPayrollAtMs + 1000);

            Assert.AreEqual(2, state.StaffedWorkers, "Tra duoc luong thi tho lam lai ngay.");
        }

        [Test]
        public void LuongBiTruDungBangSoThoTraDuoc()
        {
            var state = TestKit.NewState(_catalog);
            TestKit.OpenWholeChain(state, _catalog, 2);
            long wage = _catalog.Balance.WorkerWageCoins;
            state.Coins = wage * 2;

            _simulation.AdvanceTo(state, _catalog.Balance.PayrollPeriodMs);

            Assert.AreEqual(0, state.Coins);
            Assert.AreEqual(2, state.StaffedWorkers);
        }

        // ---- quay tra -----------------------------------------------------------------

        [Test]
        public void QuayTraUuTienTraDongGoiVaTraNhieuXuHon()
        {
            var raw = TestKit.NewState(_catalog);
            raw.AddInventory(DefaultContent.CropMint, StageInput);
            _simulation.AdvanceTo(raw, 60000);

            var packed = TestKit.NewState(_catalog);
            packed.AddInventory(Packed(DefaultContent.CropMint), PackedInput);
            _simulation.AdvanceTo(packed, 60000);

            Assert.Greater(packed.Coins, raw.Coins,
                           "Xay day chuyen ma khong duoc tra hon thi khong ai xay.");
        }

        [Test]
        public void ChuaCoMayNaoThiVanBanDuocTraNhuCu()
        {
            var state = TestKit.NewState(_catalog);
            Assert.AreEqual(0, state.OwnedStationCount());
            state.AddInventory(DefaultContent.CropMint, StageInput);

            _simulation.AdvanceTo(state, 120000);

            Assert.Greater(state.Coins, 0,
                           "Day chuyen la duong nang thu nhap, khong phai cai cong chan duong.");
        }

        // ---- tinh nhat quan thoi gian --------------------------------------------------

        [Test]
        public void MotBuocDaiBangNhieuBuocNgan_CoDayChuyen()
        {
            var oneStep = TestKit.NewState(_catalog);
            oneStep.RobotUnlocked = true;
            oneStep.Coins = 100000;
            TestKit.OpenWholeChain(oneStep, _catalog, 3);
            TestKit.PlantAllUnlocked(oneStep, _simulation, DefaultContent.CropMint);

            var manySteps = oneStep.Clone();

            _simulation.AdvanceTo(oneStep, 1800000);
            for (int i = 0; i < 1800; i++)
                _simulation.AdvanceTo(manySteps, manySteps.SimulationTimeMs + 1000);

            Assert.AreEqual(TestKit.Signature(oneStep, _catalog), TestKit.Signature(manySteps, _catalog));
        }

        [Test]
        public void TickLeThapPhanGiayVanCungKetQua_CoDayChuyen()
        {
            var reference = TestKit.NewState(_catalog);
            reference.RobotUnlocked = true;
            reference.Coins = 100000;
            TestKit.OpenWholeChain(reference, _catalog, 3);
            TestKit.PlantAllUnlocked(reference, _simulation, DefaultContent.CropMint);
            var jitter = reference.Clone();

            _simulation.AdvanceTo(reference, 600000);

            long time = 0;
            int step = 0;
            while (time < 600000)
            {
                step = (step + 137) % 900 + 7;
                time += step;
                if (time > 600000) time = 600000;
                _simulation.AdvanceTo(jitter, time);
            }

            Assert.AreEqual(TestKit.Signature(reference, _catalog), TestKit.Signature(jitter, _catalog));
        }

        // ---- lenh nguoi choi ------------------------------------------------------------

        [Test]
        public void MuaMayVaThueThoTruTienDung()
        {
            FakeClock clock;
            MemorySaveRepository repository;
            var session = TestKit.NewSession(out clock, out repository);
            var stage = session.Catalog.Stages[0];

            session.DebugAddCoins(stage.Cost + session.Catalog.Balance.WorkerHireCost);
            long before = session.State.Coins;

            Assert.IsTrue(session.BuyStation(stage.Id).Success);
            Assert.IsTrue(session.State.Station(stage.Id).Owned);
            Assert.AreEqual(before - stage.Cost, session.State.Coins);

            Assert.IsTrue(session.HireWorker().Success);
            Assert.AreEqual(1, session.State.HiredWorkers);
            Assert.AreEqual(before - stage.Cost - session.Catalog.Balance.WorkerHireCost, session.State.Coins);
        }

        [Test]
        public void KhongDuXuThiKhongMuaDuocMay()
        {
            FakeClock clock;
            MemorySaveRepository repository;
            var session = TestKit.NewSession(out clock, out repository);
            var stage = session.Catalog.Stages[0];

            var result = session.BuyStation(stage.Id);
            Assert.IsFalse(result.Success);
            Assert.IsFalse(session.State.Station(stage.Id).Owned);
            StringAssert.Contains("Thiếu", result.FailureReason);
        }

        [Test]
        public void KhongMuaDuocMotCaiMayHaiLan()
        {
            FakeClock clock;
            MemorySaveRepository repository;
            var session = TestKit.NewSession(out clock, out repository);
            var stage = session.Catalog.Stages[0];

            session.DebugAddCoins(stage.Cost * 3);
            Assert.IsTrue(session.BuyStation(stage.Id).Success);
            var again = session.BuyStation(stage.Id);
            Assert.IsFalse(again.Success);
            StringAssert.Contains("Đã mua", again.FailureReason);
        }

        /// <summary>
        /// Save cua ban truoc phai mo len duoc: nguoi da choi khong duoc mat van vi mot he thong
        /// moi duoc them vao. Save schema 2 khong co may, khong co tho, va goi mat hang la
        /// "cropId" thay vi "itemId".
        /// </summary>
        [Test]
        public void SaveCuKhongCoDayChuyenVanMoDuoc()
        {
            var catalog = TestKit.Catalog();
            var state = TestKit.NewState(catalog);
            state.Coins = 500;
            state.AddInventory(DefaultContent.CropMint, 7);

            string json = SaveSerializer.Write(
                new SaveSnapshot { State = state, BalanceVersion = catalog.Balance.Version, BuildId = "test" },
                catalog, false);

            // Ha xuong schema 2 va doi lai ten truong cu, dung nhu mot file save cua ban truoc.
            json = json.Replace("\"schemaVersion\":3", "\"schemaVersion\":2")
                       .Replace("\"itemId\"", "\"cropId\"");

            var reopened = SaveSerializer.Read(json, catalog);

            Assert.AreEqual(500, reopened.State.Coins);
            Assert.AreEqual(7, reopened.State.InventoryOf(DefaultContent.CropMint));
            Assert.AreEqual(catalog.Stages.Count, reopened.State.Stations.Count,
                            "Moi cong doan phai co mot o may, ke ca khi chua mua cai nao.");
            Assert.AreEqual(0, reopened.State.OwnedStationCount());
            Assert.AreEqual(0, reopened.State.HiredWorkers);
        }

        [Test]
        public void ChoThoNghiKhongLamXuAmVaKhongHoanTien()
        {
            FakeClock clock;
            MemorySaveRepository repository;
            var session = TestKit.NewSession(out clock, out repository);

            session.DebugAddCoins(session.Catalog.Balance.WorkerHireCost);
            Assert.IsTrue(session.HireWorker().Success);
            long afterHire = session.State.Coins;

            Assert.IsTrue(session.FireWorker().Success);
            Assert.AreEqual(0, session.State.HiredWorkers);
            Assert.AreEqual(afterHire, session.State.Coins, "Cho nghi khong hoan tien thue.");
            Assert.IsFalse(session.FireWorker().Success);
        }
    }
}
