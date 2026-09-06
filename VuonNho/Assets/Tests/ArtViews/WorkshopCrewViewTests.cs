using NUnit.Framework;
using UnityEngine;
using VuonNho.Core;
using VuonNho.Views;

namespace VuonNho.Tests.ArtViews
{
    public sealed class WorkshopCrewViewTests
    {
        GameObject _root;
        WorkshopCrewView _crew;
        StationView[] _stations;
        GameState _state;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("CrewTest");
            var catalog = DefaultContent.Create();
            _state = GameState.CreateNew(catalog, 1000);
            _stations = new StationView[catalog.Stages.Count];
            for (int i = 0; i < _stations.Length; i++)
            {
                var station = new GameObject(catalog.Stages[i].Id);
                station.transform.SetParent(_root.transform);
                station.transform.position = new Vector3(ProductionLayout.StationCenterXMm(i) / 1000f,
                    0f, ProductionLayout.StationCenterZMm(i) / 1000f);
                _stations[i] = station.AddComponent<StationView>();
                _stations[i].StageId = catalog.Stages[i].Id;
                var worker = new GameObject("Worker");
                worker.transform.SetParent(station.transform, false);
                worker.transform.localPosition = new Vector3(0f, 0f, -1.15f);
                _stations[i].WorkerRoot = worker.transform;
                _state.Station(catalog.Stages[i].Id).Owned = true;
            }
            _crew = _root.AddComponent<WorkshopCrewView>();
            _crew.Bind(_stations);
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(_root); }

        [Test]
        public void CompletingBatchesOrWaitingForWagesNeverHidesHiredWorkers()
        {
            _state.HiredWorkers = _stations.Length;
            _state.StaffedWorkers = _stations.Length;
            foreach (var station in _state.Stations) station.Running = true;
            _crew.Render(_state);
            for (int frame = 0; frame < 8; frame++)
            {
                foreach (var station in _state.Stations) station.Running = frame % 2 == 0;
                _state.StaffedWorkers = frame % 2 == 0 ? 6 : 0;
                foreach (var station in _stations) station.Render(_state);
                _crew.Render(_state);
                Assert.That(_crew.VisibleWorkerCount, Is.EqualTo(6));
                foreach (var station in _stations)
                {
                    Assert.That(station.WorkerRoot.gameObject.activeSelf, Is.True);
                    Assert.That(_crew.HasWorkerAt(station.StageId), Is.True);
                }
            }
        }

        [Test]
        public void SharedWorkerChangesJobWithoutCreatingOrHidingAnNpc()
        {
            _state.HiredWorkers = _state.StaffedWorkers = 1;
            _state.Stations[0].Running = true;
            _crew.Render(_state);
            var identity = _stations[0].WorkerRoot;
            var start = identity.position;
            _state.Stations[0].Running = false;
            _crew.Render(_state);
            Assert.That(identity.gameObject.activeSelf, Is.True);
            Assert.That(identity.position, Is.EqualTo(start));
            _state.Stations[4].Running = true;
            _crew.Render(_state);
            Assert.That(_crew.VisibleWorkerCount, Is.EqualTo(1));
            Assert.That(identity.gameObject.activeSelf, Is.True);
            for (int i = 1; i < _stations.Length; i++)
                Assert.That(_stations[i].WorkerRoot.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void FiringAndResetChangeOnlyTheHiredPopulation()
        {
            _state.HiredWorkers = 3;
            _crew.Render(_state);
            Assert.That(_crew.VisibleWorkerCount, Is.EqualTo(3));
            _state.HiredWorkers = 2;
            _crew.Render(_state);
            Assert.That(_crew.VisibleWorkerCount, Is.EqualTo(2));
            Assert.That(_stations[2].WorkerRoot.gameObject.activeSelf, Is.False);
            _crew.Render(null);
            Assert.That(_crew.VisibleWorkerCount, Is.Zero);
            foreach (var station in _stations) Assert.That(station.WorkerRoot.gameObject.activeSelf, Is.False);
        }
    }
}
