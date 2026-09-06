using NUnit.Framework;
using VuonNho.Core;

namespace VuonNho.Tests
{
    /// <summary>
    /// Pha 2 — trang tri. Diem quan trong nhat la no phai thuan tham my: dat bao nhieu mon
    /// cung khong duoc lam doi mot dong nao cua vong kinh te.
    /// </summary>
    public sealed class DecorationTests
    {
        FakeClock _clock;
        MemorySaveRepository _repository;
        GameSession _session;

        // Cho trong o phia truoc luoi o dat: khong cham o dat, quay tra hay robot.
        const int FreeXMm = 0;
        const int FreeZMm = -4000;

        [SetUp]
        public void SetUp()
        {
            _session = TestKit.NewSession(out _clock, out _repository);
        }

        void CompleteSetupPhase()
        {
            _session.State.Coins = 100000;
            Assert.IsTrue(_session.Purchase(DefaultContent.UpgradeRobot).Success);
            Assert.IsTrue(_session.Purchase(DefaultContent.UpgradeExpand8).Success);
            Assert.IsTrue(_session.Purchase(DefaultContent.UpgradeExpand12).Success);
            Assert.IsTrue(_session.DecoratingUnlocked, "Bai test can pha 1 da xong.");
        }

        // ---- cong mo pha 2 -------------------------------------------------------------

        [Test]
        public void ChuaDungXongFarmThiChuaTrangTriDuoc()
        {
            Assert.IsFalse(_session.DecoratingUnlocked);

            _session.State.Coins = 100000;
            var result = _session.PlaceDecoration(DefaultDecorations.Planter, FreeXMm, FreeZMm, 0);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(0, _session.State.Decorations.Count);
        }

        [Test]
        public void MoRobotThoiChuaDu_PhaiMoHetDat()
        {
            _session.State.Coins = 100000;
            Assert.IsTrue(_session.Purchase(DefaultContent.UpgradeRobot).Success);
            Assert.IsFalse(_session.DecoratingUnlocked, "Con thieu dat thi chua mo trang tri.");

            Assert.IsTrue(_session.Purchase(DefaultContent.UpgradeExpand8).Success);
            Assert.IsFalse(_session.DecoratingUnlocked);

            Assert.IsTrue(_session.Purchase(DefaultContent.UpgradeExpand12).Success);
            Assert.IsTrue(_session.DecoratingUnlocked);
        }

        // ---- dat, di chuyen, go ---------------------------------------------------------

        [Test]
        public void DatMonTrangTriTruDungGiaVaGhiDungViTri()
        {
            CompleteSetupPhase();
            long before = _session.State.Coins;
            long cost = _session.Catalog.Decoration(DefaultDecorations.Bench).Cost;

            var result = _session.PlaceDecoration(DefaultDecorations.Bench, -1500, FreeZMm, 90);

            Assert.IsTrue(result.Success, result.FailureReason);
            Assert.AreEqual(before - cost, _session.State.Coins);
            Assert.AreEqual(1, _session.State.Decorations.Count);

            var placed = _session.State.Decorations[0];
            Assert.AreEqual(DefaultDecorations.Bench, placed.DefinitionId);
            Assert.AreEqual(-1500, placed.XMm);
            Assert.AreEqual(FreeZMm, placed.ZMm);
            Assert.AreEqual(90, placed.RotationDeg);
        }

        [Test]
        public void KhongDatDuocHaiMonChongLenNhau()
        {
            CompleteSetupPhase();
            Assert.IsTrue(_session.PlaceDecoration(DefaultDecorations.Bench, FreeXMm, FreeZMm, 0).Success);

            var overlapping = _session.PlaceDecoration(DefaultDecorations.Bench, FreeXMm + 100, FreeZMm, 0);
            Assert.IsFalse(overlapping.Success);
            Assert.AreEqual(1, _session.State.Decorations.Count);

            // Ra du xa thi dat duoc.
            var apart = _session.PlaceDecoration(DefaultDecorations.Bench, 3000, FreeZMm, 0);
            Assert.IsTrue(apart.Success, apart.FailureReason);
            Assert.AreEqual(2, _session.State.Decorations.Count);
        }

        [Test]
        public void KhongDatDuocRaNgoaiKhuVuon()
        {
            CompleteSetupPhase();
            int outside = _session.Catalog.Balance.GardenHalfExtentMm + 1;

            Assert.IsFalse(_session.PlaceDecoration(DefaultDecorations.Planter, outside, 0, 0).Success);
            Assert.IsFalse(_session.PlaceDecoration(DefaultDecorations.Planter, 0, -outside, 0).Success);
            Assert.AreEqual(0, _session.State.Decorations.Count);
        }

        [Test]
        public void ThieuXuThiKhongDatDuoc()
        {
            CompleteSetupPhase();
            _session.State.Coins = 0;

            var result = _session.PlaceDecoration(DefaultDecorations.Signboard, FreeXMm, FreeZMm, 0);
            Assert.IsFalse(result.Success);
            Assert.AreEqual(0, _session.State.Decorations.Count);
        }

        [Test]
        public void DiChuyenMonDaDatKhongTonTien()
        {
            CompleteSetupPhase();
            Assert.IsTrue(_session.PlaceDecoration(DefaultDecorations.Lantern, FreeXMm, FreeZMm, 0).Success);
            long afterPlacing = _session.State.Coins;

            var moved = _session.MoveDecoration(0, 2000, FreeZMm, 180);
            Assert.IsTrue(moved.Success, moved.FailureReason);
            Assert.AreEqual(afterPlacing, _session.State.Coins, "Di chuyen khong duoc tinh tien.");
            Assert.AreEqual(2000, _session.State.Decorations[0].XMm);
            Assert.AreEqual(180, _session.State.Decorations[0].RotationDeg);
        }

        [Test]
        public void GoMonTrangTriHoanDuTien()
        {
            CompleteSetupPhase();
            long before = _session.State.Coins;
            Assert.IsTrue(_session.PlaceDecoration(DefaultDecorations.Signboard, FreeXMm, FreeZMm, 0).Success);
            Assert.Less(_session.State.Coins, before);

            Assert.IsTrue(_session.RemoveDecoration(0).Success);
            Assert.AreEqual(before, _session.State.Coins, "Go phai hoan du de nguoi choi thu bo cuc thoai mai.");
            Assert.AreEqual(0, _session.State.Decorations.Count);
        }

        [Test]
        public void GocXoayDuocChuanHoaVeKhoang0Den359()
        {
            CompleteSetupPhase();
            Assert.IsTrue(_session.PlaceDecoration(DefaultDecorations.Planter, FreeXMm, FreeZMm, -90).Success);
            Assert.AreEqual(270, _session.State.Decorations[0].RotationDeg);
        }

        // ---- vung cam quanh o dat, quay tra, robot ---------------------------------------

        [Test]
        public void KhongDatDuocDeLenTamODat()
        {
            CompleteSetupPhase();
            long before = _session.State.Coins;

            // (-800, 0) la tam o dat cot 1 hang 1 theo bo cuc trong BalanceConfig.
            var result = _session.PlaceDecoration(DefaultDecorations.Planter, -800, 0, 0);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("ô đất", result.FailureReason);
            Assert.AreEqual(0, _session.State.Decorations.Count);
            Assert.AreEqual(before, _session.State.Coins, "Dat truot thi khong duoc tru tien.");
        }

        [Test]
        public void DatDuocVaoKheGiuaBonODat()
        {
            CompleteSetupPhase();

            // (0, 800) la giao diem cua bon o. Ghe go qua to nen khong lot,
            // nhung phien da thi vua khe.
            string reason;
            Assert.IsFalse(_session.CanPlaceDecoration(DefaultDecorations.Bench, 0, 800, -1, out reason));
            StringAssert.Contains("ô đất", reason);

            var result = _session.PlaceDecoration(DefaultDecorations.StonePath, 0, 800, 0);
            Assert.IsTrue(result.Success, result.FailureReason);
            Assert.AreEqual(1, _session.State.Decorations.Count);
        }

        [Test]
        public void KhongDatDuocDeLenQuayTra()
        {
            CompleteSetupPhase();

            var result = _session.PlaceDecoration(DefaultDecorations.Lantern, 0, 4200, 0);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("quán trà", result.FailureReason);
            Assert.AreEqual(0, _session.State.Decorations.Count);
        }

        [Test]
        public void KhongDatDuocDeLenRobot()
        {
            CompleteSetupPhase();

            var result = _session.PlaceDecoration(DefaultDecorations.Lantern, -4400, 400, 0);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("robot", result.FailureReason);
            Assert.AreEqual(0, _session.State.Decorations.Count);
        }

        [Test]
        public void DiChuyenCungChiuLuatVungCam()
        {
            CompleteSetupPhase();
            Assert.IsTrue(_session.PlaceDecoration(DefaultDecorations.Lantern, FreeXMm, FreeZMm, 0).Success);

            var moved = _session.MoveDecoration(0, -800, 0, 0);

            Assert.IsFalse(moved.Success, "Di chuyen phai chiu dung luat cua lenh dat.");
            StringAssert.Contains("ô đất", moved.FailureReason);
            Assert.AreEqual(FreeXMm, _session.State.Decorations[0].XMm);
            Assert.AreEqual(FreeZMm, _session.State.Decorations[0].ZMm);
        }

        [Test]
        public void LoiGhiSaveThiKhongApDungViTriMoi()
        {
            CompleteSetupPhase();
            Assert.IsTrue(_session.PlaceDecoration(DefaultDecorations.Lantern, FreeXMm, FreeZMm, 0).Success);

            _repository.FailNextSave = true;
            var moved = _session.MoveDecoration(0, 2000, FreeZMm, 180);

            Assert.IsFalse(moved.Success);
            Assert.AreEqual(FreeXMm, _session.State.Decorations[0].XMm,
                            "Ghi hong thi vi tri cu phai con nguyen.");
            Assert.AreEqual(FreeZMm, _session.State.Decorations[0].ZMm);
            Assert.AreEqual(0, _session.State.Decorations[0].RotationDeg);
        }

        [Test]
        public void LuoiODatKhongKhopSoOThiCatalogTuChoi()
        {
            Assert.Throws<ContentValidationException>(delegate
            {
                DefaultContent.Create(new BalanceConfig { GardenColumns = 5 });
            });
        }

        // ---- thuan tham my ---------------------------------------------------------------

        [Test]
        public void TrangTriKhongLamDoiKetQuaKinhTe()
        {
            CompleteSetupPhase();
            for (int i = 0; i < _session.State.Plots.Count; i++)
                if (_session.State.Plot(i).Unlocked)
                    _session.Plant(i, DefaultContent.CropMint);

            var withoutDecorations = _session.State.Clone();
            var withDecorations = _session.State.Clone();
            withDecorations.Decorations.Add(new PlacedDecoration
            {
                DefinitionId = DefaultDecorations.Bench, XMm = 1000, ZMm = 3000, RotationDeg = 0
            });
            withDecorations.Decorations.Add(new PlacedDecoration
            {
                DefinitionId = DefaultDecorations.Lantern, XMm = -2500, ZMm = 1200, RotationDeg = 45
            });

            var simulation = new FarmSimulation(TestKit.Catalog());
            simulation.AdvanceTo(withoutDecorations, 600000);
            simulation.AdvanceTo(withDecorations, 600000);

            Assert.AreEqual(withoutDecorations.Coins, withDecorations.Coins);
            Assert.AreEqual(TestKit.Signature(withoutDecorations, _session.Catalog),
                            TestKit.Signature(withDecorations, _session.Catalog),
                            "Trang tri khong duoc dong vao bat ky con so nao cua vong choi.");
        }

        // ---- save ------------------------------------------------------------------------

        [Test]
        public void LuuRoiDocLaiGiuNguyenViTriTrangTri()
        {
            CompleteSetupPhase();
            Assert.IsTrue(_session.PlaceDecoration(DefaultDecorations.Bench, -1500, FreeZMm, 90).Success);
            Assert.IsTrue(_session.PlaceDecoration(DefaultDecorations.Lantern, 2600, FreeZMm, 270).Success);
            _session.SaveNow();

            var reopened = new GameSession(TestKit.Catalog(), _clock, _repository, new RecordingLogger(), "test");
            reopened.Initialize();

            Assert.AreEqual(2, reopened.State.Decorations.Count);
            Assert.AreEqual(DefaultDecorations.Bench, reopened.State.Decorations[0].DefinitionId);
            Assert.AreEqual(-1500, reopened.State.Decorations[0].XMm);
            Assert.AreEqual(FreeZMm, reopened.State.Decorations[0].ZMm);
            Assert.AreEqual(90, reopened.State.Decorations[0].RotationDeg);
            Assert.AreEqual(2600, reopened.State.Decorations[1].XMm);
            Assert.AreEqual(270, reopened.State.Decorations[1].RotationDeg);
        }

        [Test]
        public void SaveSchema1CuVanNapDuocVaCoDanhSachTrangTriRong()
        {
            var catalog = TestKit.Catalog();
            var state = GameState.CreateNew(catalog, 1000000000000);
            state.Coins = 777;

            string json = SaveSerializer.Write(new SaveSnapshot
            {
                BalanceVersion = catalog.Balance.Version, BuildId = "test", State = state
            }, catalog, true);

            // Gia lap file cua ban truoc: schema 1, chua he co truong decorations.
            string legacy = json
                .Replace("\"schemaVersion\": 2", "\"schemaVersion\": 1")
                .Replace("\"decorations\": [],", "")
                .Replace("\"decorations\": [", "\"unusedLegacyField\": [");

            var loaded = SaveSerializer.Read(legacy, catalog);

            Assert.AreEqual(SaveSnapshot.CurrentSchemaVersion, loaded.SchemaVersion);
            Assert.AreEqual(777, loaded.State.Coins, "Migration khong duoc lam mat du lieu cu.");
            Assert.AreEqual(0, loaded.State.Decorations.Count);
        }

        [Test]
        public void TrangTriCoIdLaLamSaveKhongHopLe()
        {
            CompleteSetupPhase();
            Assert.IsTrue(_session.PlaceDecoration(DefaultDecorations.Bench, FreeXMm, FreeZMm, 0).Success);
            string json = _session.Serialize(_session.State)
                .Replace(DefaultDecorations.Bench, "deco_khong_ton_tai");

            Assert.Throws<SaveCorruptException>(delegate
            {
                SaveSerializer.Read(json, TestKit.Catalog());
            });
        }
    }
}
