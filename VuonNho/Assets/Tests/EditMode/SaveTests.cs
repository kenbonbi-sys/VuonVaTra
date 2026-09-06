using System.IO;
using NUnit.Framework;
using VuonNho.Core;
using VuonNho.Infrastructure;

namespace VuonNho.Tests
{
    public sealed class SaveTests
    {
        ContentCatalog _catalog;
        FarmSimulation _simulation;

        [SetUp]
        public void SetUp()
        {
            _catalog = TestKit.Catalog();
            _simulation = new FarmSimulation(_catalog);
        }

        string Write(GameState state)
        {
            var snapshot = new SaveSnapshot
            {
                BalanceVersion = _catalog.Balance.Version,
                BuildId = "test",
                State = state
            };
            return SaveSerializer.Write(snapshot, _catalog, true);
        }

        [Test]
        public void LuuRoiDocLaiGiuNguyenTrangThaiGiuaVuVaGiuaMe()
        {
            var state = TestKit.NewState(_catalog);
            state.RobotUnlocked = true;
            state.UnlockedCropIds.Add(DefaultContent.CropChamomile);
            state.UnlockedRecipeIds.Add(DefaultContent.RecipeChamomile);
            state.UpgradeLevels[DefaultContent.UpgradeGrowthSpeed] = 1;
            TestKit.PlantAllUnlocked(state, _simulation, DefaultContent.CropMint);
            _simulation.AdvanceTo(state, 47321);   // dung giua mot vu va giua mot me

            Assert.IsTrue(state.Machine.BatchRunning, "Bai test can mot me dang chay.");

            string json = Write(state);
            var loaded = SaveSerializer.Read(json, _catalog).State;

            Assert.AreEqual(TestKit.Signature(state, _catalog), TestKit.Signature(loaded, _catalog));
            Assert.AreEqual(state.CheckpointUtcMs, loaded.CheckpointUtcMs);
            Assert.AreEqual(state.SaveRevision, loaded.SaveRevision);
        }

        [Test]
        public void HaiLanGhiCungStateChoCungChuoi()
        {
            var state = TestKit.NewState(_catalog);
            _simulation.Plant(state, state.Plot(0), DefaultContent.CropMint, 0);
            Assert.AreEqual(Write(state), Write(state));
        }

        [Test]
        public void MoPhongTiepTuTrangThaiDaLuuChoKetQuaGiongKhongLuu()
        {
            var live = TestKit.NewState(_catalog);
            live.RobotUnlocked = true;
            TestKit.PlantAllUnlocked(live, _simulation, DefaultContent.CropMint);
            _simulation.AdvanceTo(live, 123456);

            var reloaded = SaveSerializer.Read(Write(live), _catalog).State;

            _simulation.AdvanceTo(live, 600000);
            _simulation.AdvanceTo(reloaded, 600000);

            Assert.AreEqual(TestKit.Signature(live, _catalog), TestKit.Signature(reloaded, _catalog));
        }

        [Test]
        public void SchemaMoiHonUngDungThiBaoLoi_KhongAmThamReset()
        {
            var state = TestKit.NewState(_catalog);
            string json = Write(state).Replace(
                "\"schemaVersion\": " + SaveSnapshot.CurrentSchemaVersion,
                "\"schemaVersion\": " + (SaveSnapshot.CurrentSchemaVersion + 97));

            Assert.Throws<SaveIncompatibleException>(delegate { SaveSerializer.Read(json, _catalog); });
        }

        [Test]
        public void IdCayLaLamSaveKhongHopLe()
        {
            var state = TestKit.NewState(_catalog);
            _simulation.Plant(state, state.Plot(0), DefaultContent.CropMint, 0);
            string json = Write(state).Replace("crop_mint", "crop_khong_ton_tai");

            Assert.Throws<SaveCorruptException>(delegate { SaveSerializer.Read(json, _catalog); });
        }

        [Test]
        public void SoAmTrongSaveBiTuChoi()
        {
            var state = TestKit.NewState(_catalog);
            string json = Write(state).Replace("\"coins\": 0", "\"coins\": -5");

            Assert.Throws<SaveCorruptException>(delegate { SaveSerializer.Read(json, _catalog); });
        }

        [Test]
        public void JsonHongBiTuChoi()
        {
            Assert.Throws<SaveCorruptException>(delegate { SaveSerializer.Read("{ khong phai json", _catalog); });
            Assert.Throws<SaveCorruptException>(delegate { SaveSerializer.Read("[]", _catalog); });
        }

        [Test]
        public void FileChinhHongThiLoadTuBackup()
        {
            FakeClock clock;
            MemorySaveRepository repository;
            var session = TestKit.NewSession(out clock, out repository);
            session.Plant(0, DefaultContent.CropMint);
            session.SaveNow();

            Assert.IsNotNull(repository.Backup, "Bai test can mot ban backup hop le.");
            repository.Main = "{ file chinh da hong";

            var reopened = new GameSession(TestKit.Catalog(), clock, repository, new RecordingLogger(), "test");
            var outcome = reopened.Initialize();

            Assert.AreEqual(LoadOutcome.LoadedBackup, outcome);
            Assert.IsNotNull(reopened.State);
        }

        [Test]
        public void CaHaiFileHongThiKhongTuTaoVuonMoi()
        {
            FakeClock clock;
            MemorySaveRepository repository;
            var session = TestKit.NewSession(out clock, out repository);
            session.SaveNow();

            repository.Main = "{ hong";
            repository.Backup = "{ cung hong";

            var reopened = new GameSession(TestKit.Catalog(), clock, repository, new RecordingLogger(), "test");
            var outcome = reopened.Initialize();

            Assert.AreEqual(LoadOutcome.BlockedCorrupt, outcome);
            Assert.IsNull(reopened.State, "Khong duoc am tham thay bang mot vuon moi.");
            Assert.AreEqual("{ hong", repository.Main, "Khong duoc ghi de len du lieu chua doc duoc.");
        }

        [Test]
        public void LoiGhiKhiMuaThiKhongTruTienVaChoThuLai()
        {
            FakeClock clock;
            MemorySaveRepository repository;
            var session = TestKit.NewSession(out clock, out repository);
            session.State.Coins = 500;

            repository.FailNextSave = true;
            var failed = session.Purchase(DefaultContent.UpgradeRobot);

            Assert.IsFalse(failed.Success);
            Assert.AreEqual(500, session.State.Coins, "Loi ghi khong duoc tru tien.");
            Assert.IsFalse(session.State.RobotUnlocked);

            var retried = session.Purchase(DefaultContent.UpgradeRobot);
            Assert.IsTrue(retried.Success, retried.FailureReason);
            Assert.AreEqual(500 - session.Catalog.Upgrade(DefaultContent.UpgradeRobot).Cost,
                            session.State.Coins);
            Assert.IsTrue(session.State.RobotUnlocked);
        }

        // ---- FileSaveRepository tren o dia -------------------------------------------

        string _tempDirectory;

        [TearDown]
        public void TearDown()
        {
            if (_tempDirectory != null && Directory.Exists(_tempDirectory))
                Directory.Delete(_tempDirectory, true);
            _tempDirectory = null;
        }

        string TempDirectory()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "vuon-nho-test-" + Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDirectory);
            return _tempDirectory;
        }

        [Test]
        public void GhiFileTaoBackupVaDocLaiDuoc()
        {
            var repository = new FileSaveRepository(TempDirectory());
            Assert.IsFalse(repository.HasSave);

            repository.Save("{\"a\":1}");
            Assert.IsTrue(repository.HasSave);
            Assert.IsFalse(File.Exists(repository.TempPath), "File tam phai duoc don sach.");

            repository.Save("{\"a\":2}");
            var candidates = repository.LoadCandidates();

            Assert.AreEqual("{\"a\":2}", candidates[0].Json, "File chinh phai la ban moi nhat.");
            Assert.AreEqual("{\"a\":1}", candidates[1].Json, "Ban tot truoc do duoc giu lam backup.");
        }

        [Test]
        public void XoaHetRoiThiKhongConSave()
        {
            var repository = new FileSaveRepository(TempDirectory());
            repository.Save("{\"a\":1}");
            repository.DeleteAll();
            Assert.IsFalse(repository.HasSave);
        }
    }
}
