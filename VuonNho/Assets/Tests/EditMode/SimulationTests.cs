using NUnit.Framework;
using VuonNho.Core;

namespace VuonNho.Tests
{
    /// <summary>Cac ca kiem tra bat buoc o muc 10 cua ke hoach MVP.</summary>
    public sealed class SimulationTests
    {
        ContentCatalog _catalog;
        FarmSimulation _simulation;

        [SetUp]
        public void SetUp()
        {
            _catalog = TestKit.Catalog();
            _simulation = new FarmSimulation(_catalog);
        }

        // ---- Tinh nhat quan thoi gian -------------------------------------------------

        [Test]
        public void MotBuoc30PhutBangNhieuBuocCongLai()
        {
            var oneStep = TestKit.NewState(_catalog);
            oneStep.RobotUnlocked = true;
            TestKit.PlantAllUnlocked(oneStep, _simulation, DefaultContent.CropMint);

            var manySteps = oneStep.Clone();

            _simulation.AdvanceTo(oneStep, 1800000);
            for (int i = 0; i < 1800; i++)
                _simulation.AdvanceTo(manySteps, manySteps.SimulationTimeMs + 1000);

            Assert.AreEqual(TestKit.Signature(oneStep, _catalog), TestKit.Signature(manySteps, _catalog));
            Assert.Greater(oneStep.Coins, 0);
        }

        [Test]
        public void TickLeThapPhanGiayVanCungKetQua()
        {
            var reference = TestKit.NewState(_catalog);
            reference.RobotUnlocked = true;
            TestKit.PlantAllUnlocked(reference, _simulation, DefaultContent.CropMint);
            var jitter = reference.Clone();

            _simulation.AdvanceTo(reference, 600000);

            long time = 0;
            int step = 0;
            while (time < 600000)
            {
                // Do tre tick khong lam troi chu ky vi su kien dung deadline chinh xac.
                long delta = 133 + (step % 7) * 41;
                time += delta;
                if (time > 600000) time = 600000;
                _simulation.AdvanceTo(jitter, time);
                step++;
            }

            Assert.AreEqual(TestKit.Signature(reference, _catalog), TestKit.Signature(jitter, _catalog));
        }

        // ---- Bien timer ---------------------------------------------------------------

        [Test]
        public void CayChinDungThoiDiemDeadline()
        {
            var state = TestKit.NewState(_catalog);
            var plot = state.Plot(0);
            _simulation.Plant(state, plot, DefaultContent.CropMint, 0);

            long growth = _simulation.GrowthMsFor(state, DefaultContent.CropMint);

            _simulation.AdvanceTo(state, growth - 1);
            Assert.AreEqual(PlotPhase.Growing, plot.Phase);

            _simulation.AdvanceTo(state, growth);
            Assert.AreEqual(PlotPhase.Ready, plot.Phase);

            _simulation.AdvanceTo(state, growth + 1);
            Assert.AreEqual(PlotPhase.Ready, plot.Phase, "Khong duoc tao them mot vu khi da chin.");
            Assert.AreEqual(0, state.InventoryOf(DefaultContent.CropMint));
        }

        [Test]
        public void MeMayCongXuDungMotLanTaiDeadline()
        {
            var state = TestKit.NewState(_catalog);
            state.AddInventory(DefaultContent.CropMint, 2);
            Assert.IsTrue(_simulation.TryStartBatch(state, 0));

            long brew = _simulation.BrewMsFor(state, DefaultContent.RecipeMint);
            long payout = _catalog.Recipe(DefaultContent.RecipeMint).OutputCoins;

            _simulation.AdvanceTo(state, brew - 1);
            Assert.AreEqual(0, state.Coins);

            _simulation.AdvanceTo(state, brew);
            Assert.AreEqual(payout, state.Coins);

            _simulation.AdvanceTo(state, brew * 8);
            Assert.AreEqual(payout, state.Coins, "Khong duoc cong xu cua mot me hai lan.");
        }

        // ---- Truoc va sau robot -------------------------------------------------------

        [Test]
        public void TruocRobotKhongTuThuVaKhongGieoThem()
        {
            var state = TestKit.NewState(_catalog);
            TestKit.PlantAllUnlocked(state, _simulation, DefaultContent.CropMint);

            _simulation.AdvanceTo(state, 600000);

            for (int i = 0; i < 4; i++)
                Assert.AreEqual(PlotPhase.Ready, state.Plot(i).Phase);
            Assert.AreEqual(0, state.InventoryOf(DefaultContent.CropMint));
            Assert.AreEqual(0, state.Coins, "Chua co robot thi khong co nguyen lieu cho may.");
        }

        [Test]
        public void SauRobotKhopVoiMoPhongOnline([Values(0, 60000, 300000, 14400000)] int elapsedMs)
        {
            var offline = TestKit.NewState(_catalog);
            offline.RobotUnlocked = true;
            TestKit.PlantAllUnlocked(offline, _simulation, DefaultContent.CropMint);
            var online = offline.Clone();

            _simulation.AdvanceTo(offline, elapsedMs);
            for (int t = 0; t < elapsedMs; t += 250)
                _simulation.AdvanceTo(online, System.Math.Min(t + 250, elapsedMs));

            Assert.AreEqual(TestKit.Signature(online, _catalog), TestKit.Signature(offline, _catalog));
        }

        [Test]
        public void BonOBacHaChayOnDinhChoDungMuoiXuMoiMe()
        {
            var state = TestKit.NewState(_catalog);
            state.RobotUnlocked = true;
            TestKit.PlantAllUnlocked(state, _simulation, DefaultContent.CropMint);

            const long tenMinutes = 600000;
            _simulation.AdvanceTo(state, tenMinutes);

            // Cong thuc trang thai on dinh o muc 5 ke hoach:
            // xu/phut = min(so o x 60 / giay lon / so nguyen lieu, 60 / giay pha) x xu moi me.
            var crop = _catalog.Crop(DefaultContent.CropMint);
            var recipe = _catalog.Recipe(DefaultContent.RecipeMint);
            double fieldBatchesPerMinute = 4 * 60000.0 / crop.BaseGrowthMs / recipe.InputCount;
            double machineBatchesPerMinute = 60000.0 / recipe.BaseBrewMs;
            double expected = System.Math.Min(fieldBatchesPerMinute, machineBatchesPerMinute)
                              * recipe.OutputCoins * (tenMinutes / 60000.0);

            // Cho phep lech 8% vi phan khoi dong: vu dau va me dau chua chay ngay tu giay 0.
            Assert.GreaterOrEqual(state.Coins, (long)(expected * 0.92),
                                  "Thu nhap on dinh thap hon cong thuc balance.");
            Assert.LessOrEqual(state.Coins, (long)(expected * 1.02),
                               "Thu nhap on dinh cao hon cong thuc balance.");
        }

        // ---- Dong thoi ----------------------------------------------------------------

        [Test]
        public void NhieuSuKienCungTimestampChoKetQuaOnDinh()
        {
            var first = TestKit.NewState(_catalog);
            first.RobotUnlocked = true;
            TestKit.PlantAllUnlocked(first, _simulation, DefaultContent.CropMint);
            var second = first.Clone();

            // 60 giay la boi so chung cua 30 giay (cay) va 15 giay (me): nhieu deadline trung nhau.
            _simulation.AdvanceTo(first, 60000);
            _simulation.AdvanceTo(second, 30000);
            _simulation.AdvanceTo(second, 45000);
            _simulation.AdvanceTo(second, 60000);

            Assert.AreEqual(TestKit.Signature(first, _catalog), TestKit.Signature(second, _catalog));
        }

        // ---- Doi cay va cong thuc -----------------------------------------------------

        [Test]
        public void DoiCayGiuaVuChiApDungTuVuSau()
        {
            var state = TestKit.NewState(_catalog);
            state.UnlockedCropIds.Add(DefaultContent.CropChamomile);
            var plot = state.Plot(0);
            _simulation.Plant(state, plot, DefaultContent.CropMint, 0);

            long mintGrowth = _simulation.GrowthMsFor(state, DefaultContent.CropMint);
            long chamomileGrowth = _simulation.GrowthMsFor(state, DefaultContent.CropChamomile);

            _simulation.AdvanceTo(state, mintGrowth / 2);
            plot.NextCropId = DefaultContent.CropChamomile;

            Assert.AreEqual(DefaultContent.CropMint, plot.CurrentCropId);
            Assert.AreEqual(mintGrowth, plot.FinishAtMs, "Vu dang chay khong duoc doi deadline.");

            _simulation.AdvanceTo(state, mintGrowth);
            Assert.IsTrue(_simulation.HarvestAndReplant(state, plot, mintGrowth, false));
            Assert.AreEqual(DefaultContent.CropChamomile, plot.CurrentCropId);
            Assert.AreEqual(mintGrowth + chamomileGrowth, plot.FinishAtMs,
                            "Cuc bat dau chu ky rieng ke tu luc gieo lai.");
            Assert.AreEqual(1, state.InventoryOf(DefaultContent.CropMint));
        }

        [Test]
        public void DoiCongThucGiuaMeThiMeHienTaiVanHoanThanhTruoc()
        {
            var state = TestKit.NewState(_catalog);
            state.UnlockedRecipeIds.Add(DefaultContent.RecipeChamomile);
            state.AddInventory(DefaultContent.CropMint, 2);
            state.AddInventory(DefaultContent.CropChamomile, 2);
            Assert.IsTrue(_simulation.TryStartBatch(state, 0));

            long mintBrew = _simulation.BrewMsFor(state, DefaultContent.RecipeMint);
            long mintPayout = _catalog.Recipe(DefaultContent.RecipeMint).OutputCoins;
            long chamomilePayout = _catalog.Recipe(DefaultContent.RecipeChamomile).OutputCoins;

            _simulation.AdvanceTo(state, mintBrew / 2);
            state.Machine.SelectedRecipeId = DefaultContent.RecipeChamomile;
            _simulation.ResolveImmediate(state);

            Assert.AreEqual(DefaultContent.RecipeMint, state.Machine.BatchRecipeId);
            Assert.AreEqual(mintBrew, state.Machine.BatchFinishAtMs);

            _simulation.AdvanceTo(state, mintBrew);
            Assert.AreEqual(mintPayout, state.Coins, "Me dang chay van tra dung so xu da chot.");
            Assert.AreEqual(DefaultContent.RecipeChamomile, state.Machine.BatchRecipeId);
            Assert.AreEqual(chamomilePayout, state.Machine.BatchOutputCoins);
        }

        // ---- Nang toc do --------------------------------------------------------------

        [Test]
        public void MuaTocDoGiuaChuKyKhongDoiDeadlineDangChay()
        {
            var state = TestKit.NewState(_catalog);
            var plot = state.Plot(0);
            _simulation.Plant(state, plot, DefaultContent.CropMint, 0);
            state.AddInventory(DefaultContent.CropMint, 2);
            _simulation.TryStartBatch(state, 0);

            long growthBefore = _simulation.GrowthMsFor(state, DefaultContent.CropMint);
            long brewBefore = _simulation.BrewMsFor(state, DefaultContent.RecipeMint);

            _simulation.AdvanceTo(state, growthBefore / 4);
            state.UpgradeLevels[DefaultContent.UpgradeGrowthSpeed] = 1;
            state.UpgradeLevels[DefaultContent.UpgradeBrewSpeed] = 1;

            Assert.AreEqual(growthBefore, plot.FinishAtMs, "Vu dang chay giu nguyen deadline.");
            Assert.AreEqual(brewBefore, state.Machine.BatchFinishAtMs, "Me dang chay giu nguyen deadline.");

            long growthAfter = _simulation.GrowthMsFor(state, DefaultContent.CropMint);
            long brewAfter = _simulation.BrewMsFor(state, DefaultContent.RecipeMint);
            Assert.Less(growthAfter, growthBefore, "Nang cap phai lam chu ky cay ngan hon.");
            Assert.Less(brewAfter, brewBefore, "Nang cap phai lam chu ky may ngan hon.");
            Assert.AreEqual(_catalog.ScaleGrowth(_catalog.Crop(DefaultContent.CropMint).BaseGrowthMs, 1),
                            growthAfter);
            Assert.AreEqual(_catalog.ScaleBrew(_catalog.Recipe(DefaultContent.RecipeMint).BaseBrewMs, 1),
                            brewAfter);

            _simulation.AdvanceTo(state, growthBefore);
            _simulation.HarvestAndReplant(state, plot, growthBefore, false);
            Assert.AreEqual(growthBefore + growthAfter, plot.FinishAtMs,
                            "Vu moi dung thoi gian da nang cap.");
        }

        // ---- Kho ----------------------------------------------------------------------

        [Test]
        public void BanHetNguyenLieuKhiMayDangPhaThiMeHienTaiVanXong()
        {
            var state = TestKit.NewState(_catalog);
            state.AddInventory(DefaultContent.CropMint, 4);
            Assert.IsTrue(_simulation.TryStartBatch(state, 0));
            Assert.AreEqual(2, state.InventoryOf(DefaultContent.CropMint),
                            "Nguyen lieu bi tru ngay luc bat dau me.");

            state.AddInventory(DefaultContent.CropMint, -2);   // nguoi choi ban het phan con lai

            _simulation.AdvanceTo(state, _simulation.BrewMsFor(state, DefaultContent.RecipeMint));
            Assert.AreEqual(_catalog.Recipe(DefaultContent.RecipeMint).OutputCoins, state.Coins);
            Assert.IsFalse(state.Machine.BatchRunning, "Me tiep theo phai cho vi kho trong.");

            string missingCropId;
            long missing;
            Assert.IsTrue(_simulation.TryGetMissingInput(state, out missingCropId, out missing));
            Assert.AreEqual(DefaultContent.CropMint, missingCropId);
            Assert.AreEqual(2, missing);
        }

        [Test]
        public void MayKhongTuDoiCongThucKhiThieuNguyenLieu()
        {
            var state = TestKit.NewState(_catalog);
            state.UnlockedRecipeIds.Add(DefaultContent.RecipeChamomile);
            state.Machine.SelectedRecipeId = DefaultContent.RecipeChamomile;
            state.AddInventory(DefaultContent.CropMint, 10);

            _simulation.AdvanceTo(state, 120000);

            Assert.IsFalse(state.Machine.BatchRunning);
            Assert.AreEqual(DefaultContent.RecipeChamomile, state.Machine.SelectedRecipeId);
            Assert.AreEqual(10, state.InventoryOf(DefaultContent.CropMint));
            Assert.AreEqual(0, state.Coins);
        }

        // ---- O khoa -------------------------------------------------------------------

        [Test]
        public void OKhoaKhongNhanLenhTrongVaKhongDuocRobotDungToi()
        {
            var state = TestKit.NewState(_catalog);
            state.RobotUnlocked = true;
            var locked = state.Plot(8);
            Assert.IsFalse(locked.Unlocked);

            locked.NextCropId = DefaultContent.CropMint;
            _simulation.AdvanceTo(state, 600000);

            Assert.AreEqual(PlotPhase.Locked, locked.Phase);
        }

        [Test]
        public void ONgungGieoKhiKhongConLuaChonCay()
        {
            var state = TestKit.NewState(_catalog);
            state.RobotUnlocked = true;
            var plot = state.Plot(0);
            _simulation.Plant(state, plot, DefaultContent.CropMint, 0);
            plot.NextCropId = null;

            _simulation.AdvanceTo(state, 60000);

            Assert.AreEqual(PlotPhase.Empty, plot.Phase);
            Assert.AreEqual(1, state.InventoryOf(DefaultContent.CropMint));
        }
    }
}
