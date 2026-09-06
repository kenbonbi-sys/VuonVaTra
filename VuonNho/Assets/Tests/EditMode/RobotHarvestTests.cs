using NUnit.Framework;
using VuonNho.Core;

namespace VuonNho.Tests
{
    /// <summary>
    /// Robot di toi tung o de thu thay vi thu sach tuc thi.
    ///
    /// Trong tam: cay chin phai NAM CHO chu khong bien mat ngay, va quang di phai la mot deadline
    /// that — neu khong thi chay online va chay bu offline se ra hai ket qua khac nhau.
    /// </summary>
    public sealed class RobotHarvestTests
    {
        ContentCatalog _catalog;
        FarmSimulation _simulation;

        [SetUp]
        public void SetUp()
        {
            _catalog = TestKit.Catalog();
            _simulation = new FarmSimulation(_catalog);
        }

        GameState Ready(int workers = 0)
        {
            var state = TestKit.NewState(_catalog);
            state.RobotUnlocked = true;
            // Tat quay tra: bai nay do robot, ma quay tra thi cung an la tuoi.
            state.Machine.SelectedRecipeId = null;
            return state;
        }

        [Test]
        public void CayChinNamChoChoToiKhiRobotToiNoi()
        {
            var state = Ready();
            TestKit.PlantAllUnlocked(state, _simulation, DefaultContent.CropMint);
            long growth = _simulation.GrowthMsFor(state, DefaultContent.CropMint);

            // Ngay sau khi chin: robot moi chi vua nham o dau tien, chua o nao duoc thu.
            _simulation.AdvanceTo(state, growth);

            int ready = 0;
            for (int i = 0; i < state.Plots.Count; i++)
                if (state.Plots[i].Phase == PlotPhase.Ready) ready++;

            Assert.Greater(ready, 0, "Cây chín phải nằm chờ chứ không bị thu sạch tức thì.");
            Assert.AreEqual(0, state.InventoryOf(DefaultContent.CropMint),
                            "Chưa tới nơi thì chưa có hàng vào kho.");
            Assert.GreaterOrEqual(state.RobotTargetPlotId, 0, "Robot phải đang nhắm một ô.");
            Assert.Greater(state.RobotReadyAtMs, growth, "Đi tới nơi phải mất thời gian.");
        }

        [Test]
        public void ThuTungOMotChuKhongThuCaLuot()
        {
            var state = Ready();
            TestKit.PlantAllUnlocked(state, _simulation, DefaultContent.CropMint);
            long growth = _simulation.GrowthMsFor(state, DefaultContent.CropMint);

            // Toi dung luc robot nham xong o dau tien, roi cho dung moc no toi noi. Khong doan
            // truoc quang duong: robot xuat phat tu cho cua no, con o gan nhat la o nao thi do
            // bo cuc vuon quyet dinh.
            _simulation.AdvanceTo(state, growth);
            Assert.GreaterOrEqual(state.RobotTargetPlotId, 0);
            _simulation.AdvanceTo(state, state.RobotReadyAtMs);

            Assert.AreEqual(1, state.InventoryOf(DefaultContent.CropMint),
                            "Một chuyến đi thu đúng một ô.");
        }

        [Test]
        public void RobotDungLaiODaThuChuKhongNhayVeCho()
        {
            var state = Ready();
            TestKit.PlantAllUnlocked(state, _simulation, DefaultContent.CropMint);
            _simulation.AdvanceTo(state, 120000);

            bool onSomePlot = false;
            for (int i = 0; i < state.Plots.Count; i++)
                if (_simulation.PlotXMm(i) == state.RobotXMm && _simulation.PlotZMm(i) == state.RobotZMm)
                    onSomePlot = true;

            Assert.IsTrue(onSomePlot, "Thu xong thì robot đứng lại ngay ô đó.");
        }

        [Test]
        public void OGanHonDuocThuTruoc()
        {
            var state = Ready();
            var far = state.Plot(state.Plots.Count - 1);
            var near = state.Plot(0);
            near.Unlocked = true;
            far.Unlocked = true;

            // Dat robot ngay canh o 0 roi cho hai o cung chin mot luc.
            state.RobotXMm = _simulation.PlotXMm(0);
            state.RobotZMm = _simulation.PlotZMm(0);
            foreach (var plot in new[] { near, far })
            {
                plot.Phase = PlotPhase.Ready;
                plot.CurrentCropId = DefaultContent.CropMint;
                plot.PendingYield = 1;
            }

            Assert.AreEqual(near.PlotId, _simulation.NearestReadyPlot(state).PlotId);
        }

        [Test]
        public void NguoiChoiThuTayTruocThiRobotBoChuyenDoChuKhongHong()
        {
            var state = Ready();
            TestKit.PlantAllUnlocked(state, _simulation, DefaultContent.CropMint);
            long growth = _simulation.GrowthMsFor(state, DefaultContent.CropMint);
            _simulation.AdvanceTo(state, growth);

            int targeted = state.RobotTargetPlotId;
            Assert.GreaterOrEqual(targeted, 0);

            // Nguoi choi nhanh tay hon robot.
            _simulation.HarvestAndReplant(state, state.Plot(targeted), state.SimulationTimeMs, false);

            Assert.DoesNotThrow(delegate { _simulation.AdvanceTo(state, growth + 60000); });
            Assert.GreaterOrEqual(state.Coins + state.InventoryOf(DefaultContent.CropMint), 1);
        }

        [Test]
        public void MotBuocDaiBangNhieuBuocNgan_CoRobotDiLai()
        {
            var oneStep = Ready();
            TestKit.PlantAllUnlocked(oneStep, _simulation, DefaultContent.CropMint);
            var manySteps = oneStep.Clone();

            _simulation.AdvanceTo(oneStep, 900000);
            for (int i = 0; i < 900; i++)
                _simulation.AdvanceTo(manySteps, manySteps.SimulationTimeMs + 1000);

            Assert.AreEqual(TestKit.Signature(oneStep, _catalog), TestKit.Signature(manySteps, _catalog));
        }

        [Test]
        public void TickLeThapPhanGiayVanCungKetQua_CoRobotDiLai()
        {
            var reference = Ready();
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

        [Test]
        public void CanBacHaiSoNguyenDungVoiSoChinhPhuong()
        {
            Assert.AreEqual(0, FarmSimulation.IntegerSqrt(0));
            Assert.AreEqual(0, FarmSimulation.IntegerSqrt(-9));
            Assert.AreEqual(1, FarmSimulation.IntegerSqrt(1));
            Assert.AreEqual(1600, FarmSimulation.IntegerSqrt(1600L * 1600L));
            Assert.AreEqual(3, FarmSimulation.IntegerSqrt(15));
            Assert.AreEqual(4, FarmSimulation.IntegerSqrt(16));
        }

        [Test]
        public void SaveCuKhongCoChoRobotThiRobotVeChoCuaNo()
        {
            var catalog = TestKit.Catalog();
            var state = TestKit.NewState(catalog);
            state.RobotUnlocked = true;

            string json = SaveSerializer.Write(
                new SaveSnapshot { State = state, BalanceVersion = catalog.Balance.Version, BuildId = "test" },
                catalog, false);

            // Ha xuong schema 3 va bo bon truong cua robot, dung nhu mot file save cua ban truoc.
            json = json.Replace("\"schemaVersion\":4", "\"schemaVersion\":3");
            foreach (var field in new[] { "robotXMm", "robotZMm", "robotTargetPlotId", "robotReadyAtMs" })
            {
                int start = json.IndexOf("\"" + field + "\":");
                Assert.Greater(start, 0, "Save mới phải có trường " + field + ".");
                int end = json.IndexOf(',', start) + 1;
                json = json.Remove(start, end - start);
            }

            var reopened = SaveSerializer.Read(json, catalog).State;

            Assert.AreEqual(catalog.Balance.RobotCenterXMm, reopened.RobotXMm);
            Assert.AreEqual(catalog.Balance.RobotCenterZMm, reopened.RobotZMm);
            Assert.AreEqual(-1, reopened.RobotTargetPlotId, "Save cũ thì robot đang rảnh.");
        }
    }
}
