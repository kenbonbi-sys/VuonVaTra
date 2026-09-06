using NUnit.Framework;
using VuonNho.Core;

namespace VuonNho.Tests
{
    public sealed class ProductionLayoutTests
    {
        FakeClock _clock;
        MemorySaveRepository _repository;
        GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _session = TestKit.NewSession(out _clock, out _repository);
            _session.State.Coins = 100000;
            Assert.IsTrue(_session.Purchase(DefaultContent.UpgradeRobot).Success);
            Assert.IsTrue(_session.Purchase(DefaultContent.UpgradeExpand8).Success);
            Assert.IsTrue(_session.Purchase(DefaultContent.UpgradeExpand12).Success);
        }

        [Test]
        public void EveryCatalogStageHasAStationInsideTheReservedYard()
        {
            Assert.AreEqual(_session.Catalog.Stages.Count, ProductionLayout.StationCount,
                "A new processing stage needs an explicit place in the production route.");
            for (int i = 0; i < _session.Catalog.Stages.Count; i++)
            {
                int x = ProductionLayout.StationCenterXMm(i);
                int z = ProductionLayout.StationCenterZMm(i);
                Assert.That(x, Is.InRange(ProductionLayout.MinXMm, ProductionLayout.MaxXMm));
                Assert.That(z, Is.InRange(ProductionLayout.MinZMm, ProductionLayout.MaxZMm));
                if (i == 0) continue;

                int previousX = ProductionLayout.StationCenterXMm(i - 1);
                int previousZ = ProductionLayout.StationCenterZMm(i - 1);
                Assert.IsTrue((x == previousX) != (z == previousZ),
                    "Consecutive stations need a nonzero straight belt connection.");
            }
        }

        [Test]
        public void UnownedFactoryStillReservesMachinesAndServiceAisle()
        {
            Assert.AreEqual(0, _session.State.OwnedStationCount());
            long before = _session.State.Coins;

            var atMachine = _session.PlaceDecoration(DefaultDecorations.Planter,
                ProductionLayout.StationCenterXMm(0), ProductionLayout.StationCenterZMm(0), 0);
            var inAisle = _session.PlaceDecoration(DefaultDecorations.StonePath, 7000, 1000, 0);

            Assert.IsFalse(atMachine.Success);
            Assert.IsFalse(inAisle.Success);
            StringAssert.Contains("dây chuyền sản xuất", atMachine.FailureReason);
            StringAssert.Contains("dây chuyền sản xuất", inAisle.FailureReason);
            Assert.AreEqual(before, _session.State.Coins, "Rejected placement cannot charge coins.");
            Assert.AreEqual(0, _session.State.Decorations.Count);
        }

        [Test]
        public void DecorationFootprintCannotCrossFactoryBoundaryButMayTouchIt()
        {
            int radius = _session.Catalog.Decoration(DefaultDecorations.Lantern).FootprintMm;
            string reason;

            Assert.IsFalse(_session.CanPlaceDecoration(DefaultDecorations.Lantern,
                ProductionLayout.MinXMm - radius + 1, -1000, -1, out reason));
            StringAssert.Contains("dây chuyền sản xuất", reason);
            Assert.IsTrue(_session.CanPlaceDecoration(DefaultDecorations.Lantern,
                ProductionLayout.MinXMm - radius, -1000, -1, out reason), reason);
        }

        [Test]
        public void ExistingSavedDecorationInFactoryYardKeepsPositionAndCanBeRemoved()
        {
            // This was valid open grass in earlier maps. Loading must not delete or relocate it.
            _session.State.Decorations.Add(new PlacedDecoration
            {
                DefinitionId = DefaultDecorations.Bench,
                XMm = 6000,
                ZMm = 1000,
                RotationDeg = 90
            });
            _session.SaveNow();

            var reopened = new GameSession(TestKit.Catalog(), _clock, _repository,
                new RecordingLogger(), "test");
            reopened.Initialize();

            Assert.AreEqual(1, reopened.State.Decorations.Count);
            var decoration = reopened.State.Decorations[0];
            Assert.AreEqual(DefaultDecorations.Bench, decoration.DefinitionId);
            Assert.AreEqual(6000, decoration.XMm);
            Assert.AreEqual(1000, decoration.ZMm);
            Assert.AreEqual(90, decoration.RotationDeg);
            Assert.IsTrue(reopened.RemoveDecoration(0).Success);
            Assert.AreEqual(0, reopened.State.Decorations.Count);
        }
    }
}
