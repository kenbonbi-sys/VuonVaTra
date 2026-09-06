using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VuonNho.Core;
using VuonNho.EditorTools;
using VuonNho.Views;

namespace VuonNho.Tests.ArtEditor
{
    public sealed class ProductionSceneTests
    {
        Scene _scene;
        Scene _previousActive;
        bool _openedForTest;
        GameBootstrap _bootstrap;

        [SetUp]
        public void OpenGardenForInspection()
        {
            _previousActive = SceneManager.GetActiveScene();
            _scene = SceneManager.GetSceneByPath(SceneFactory.ScenePath);
            _openedForTest = !_scene.IsValid() || !_scene.isLoaded;
            if (_openedForTest)
                _scene = EditorSceneManager.OpenScene(SceneFactory.ScenePath, OpenSceneMode.Additive);
            var bootstraps = ComponentsInGarden<GameBootstrap>();
            Assert.That(bootstraps.Count, Is.EqualTo(1), "Garden needs its serialized GameBootstrap.");
            _bootstrap = bootstraps[0];
        }

        [TearDown]
        public void CloseOnlyTheSceneOpenedByThisTest()
        {
            // Never save, close, or replace a scene that was already open before the test.
            if (_openedForTest && _scene.IsValid() && _scene.isLoaded)
                EditorSceneManager.CloseScene(_scene, true);
            if (_previousActive.IsValid() && _previousActive.isLoaded)
                SceneManager.SetActiveScene(_previousActive);
        }

        [Test]
        public void SerializedStationsMatchSharedLayoutAndRemainWithinWalkingBounds()
        {
            var catalog = DefaultContent.Create();
            Assert.That(_bootstrap.Stations, Has.Length.EqualTo(catalog.Stages.Count));
            Assert.That(_bootstrap.Character, Is.Not.Null);
            var stations = ComponentsInGarden<StationView>();
            Assert.That(stations.Count, Is.EqualTo(ProductionLayout.StationCount));

            for (int i = 0; i < catalog.Stages.Count; i++)
            {
                var station = _bootstrap.Stations[i];
                Assert.That(station, Is.Not.Null);
                Assert.That(station.StageId, Is.EqualTo(catalog.Stages[i].Id));
                Assert.That(station.transform.position.x,
                    Is.EqualTo(ProductionLayout.StationCenterXMm(i) / 1000f).Within(.001f), station.StageId);
                Assert.That(station.transform.position.z,
                    Is.EqualTo(ProductionLayout.StationCenterZMm(i) / 1000f).Within(.001f), station.StageId);
                Assert.That(Mathf.Abs(station.transform.position.x),
                    Is.LessThan(_bootstrap.Character.WalkLimit), "The walking boundary cuts off " + station.StageId);
                Assert.That(Mathf.Abs(station.transform.position.z),
                    Is.LessThan(_bootstrap.Character.WalkLimit), "The walking boundary cuts off " + station.StageId);
            }
        }

        [Test]
        public void DefaultCameraFramesEveryMachineAtDesktopAspectRatio()
        {
            var cameraObject = new GameObject("ProductionProjectionTestCamera");
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                Assert.That(_bootstrap.GameCamera, Is.Not.Null);
                camera.CopyFrom(_bootstrap.GameCamera);
                camera.enabled = false;
                camera.transform.SetPositionAndRotation(_bootstrap.GameCamera.transform.position,
                    _bootstrap.GameCamera.transform.rotation);
                // Do not depend on the editor's current Game view size, or alter its real camera.
                camera.aspect = 1366f / 768f;
                foreach (var station in _bootstrap.Stations)
                {
                    var collider = station.GetComponent<BoxCollider>();
                    Assert.That(collider, Is.Not.Null, station.StageId);
                    for (int corner = 0; corner < 8; corner++)
                    {
                        var offset = new Vector3((corner & 1) == 0 ? -.5f : .5f,
                            (corner & 2) == 0 ? -.5f : .5f, (corner & 4) == 0 ? -.5f : .5f);
                        var world = collider.transform.TransformPoint(collider.center + Vector3.Scale(collider.size, offset));
                        var projected = camera.WorldToViewportPoint(world);
                        Assert.That(projected.z, Is.GreaterThan(0f), station.StageId + " is behind the camera.");
                        Assert.That(projected.x, Is.InRange(0f, 1f), station.StageId + " is clipped horizontally.");
                        Assert.That(projected.y, Is.InRange(0f, 1f), station.StageId + " is clipped vertically.");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void StationOwnershipControlsBothVisibleMachineAndGameplayColliders()
        {
            var catalog = DefaultContent.Create();
            foreach (var original in _bootstrap.Stations)
            {
                var copy = Object.Instantiate(original.gameObject);
                try
                {
                    var view = copy.GetComponent<StationView>();
                    var colliders = copy.GetComponents<Collider>();
                    Assert.That(colliders, Is.Not.Empty, view.StageId);
                    Assert.That(view.VisualRoot, Is.Not.Null, view.StageId);
                    var state = GameState.CreateNew(catalog, 1000000000000L);

                    view.Render(state);
                    Assert.That(view.VisualRoot.gameObject.activeSelf, Is.False, view.StageId);
                    foreach (var collider in colliders)
                        Assert.That(collider.enabled, Is.False, "Unowned " + view.StageId + " blocks an empty bay.");

                    state.Station(view.StageId).Owned = true;
                    view.Render(state);
                    Assert.That(view.VisualRoot.gameObject.activeSelf, Is.True, view.StageId);
                    foreach (var collider in colliders)
                        Assert.That(collider.enabled, Is.True, "Owned " + view.StageId + " lost its collider.");

                    state.Station(view.StageId).Owned = false;
                    view.Render(state);
                    Assert.That(view.VisualRoot.gameObject.activeSelf, Is.False, view.StageId);
                    foreach (var collider in colliders)
                        Assert.That(collider.enabled, Is.False, "Reset leaves an invisible " + view.StageId + " obstacle.");
                }
                finally
                {
                    Object.DestroyImmediate(copy);
                }
            }
        }

        [Test]
        public void NewFactorySceneryDoesNotInterceptAnyOfTheTwelvePlotTargets()
        {
            Assert.That(_bootstrap.Plots, Has.Length.EqualTo(DefaultContent.Create().Balance.MaximumPlots));
            Assert.That(_bootstrap.GameCamera, Is.Not.Null);
            Physics.SyncTransforms();
            foreach (var plot in _bootstrap.Plots)
            {
                var collider = plot.GetComponent<BoxCollider>();
                Assert.That(collider, Is.Not.Null, "Plot " + plot.PlotId);
                var centre = collider.transform.TransformPoint(collider.center);
                var origin = _bootstrap.GameCamera.transform.position;
                var ray = new Ray(origin, centre - origin);
                // Orthographic rays are parallel; start at the matching point on its near plane.
                ray = _bootstrap.GameCamera.ViewportPointToRay(_bootstrap.GameCamera.WorldToViewportPoint(centre));
                Collider closest = null;
                float closestDistance = float.PositiveInfinity;
                foreach (var hit in Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Ignore))
                {
                    // Other additive editor scenes must not affect this garden's regression.
                    if (hit.collider.gameObject.scene != _scene || hit.distance >= closestDistance) continue;
                    closest = hit.collider;
                    closestDistance = hit.distance;
                }
                Assert.That(closest, Is.Not.Null, "No collider at plot " + plot.PlotId);
                Assert.That(closest.GetComponentInParent<PlotView>(), Is.SameAs(plot),
                    "Plot " + plot.PlotId + " is covered by " + closest.name);
            }
        }

        [Test]
        public void FactoryHasFloorAndAnUnbrokenOrderedChainOfTransferLinks()
        {
            Transform floor = null;
            foreach (var candidate in ComponentsInGarden<Transform>())
                if (candidate.name == "FactoryCourtyard") { floor = candidate; break; }
            Assert.That(floor, Is.Not.Null, "The processing machines need a visible factory site.");
            Assert.That(floor.GetComponentsInChildren<Renderer>(true), Is.Not.Empty);

            var links = ComponentsInGarden<ProductionConveyorView>();
            var stages = DefaultContent.Create().Stages;
            for (int i = 0; i < stages.Count - 1; i++)
            {
                var matching = links.FindAll(link => link.SourceStageId == stages[i].Id &&
                    link.DestinationStageId == stages[i + 1].Id);
                Assert.That(matching.Count, Is.EqualTo(1), "Missing or duplicated transfer after " + stages[i].Id);
                var link = matching[0];
                Assert.That(link.VisualRoot, Is.Not.Null, link.name);
                Assert.That(link.VisualRoot.GetComponentsInChildren<Renderer>(true), Is.Not.Empty, link.name);
                Assert.That(link.Path, Has.Length.GreaterThanOrEqualTo(2), link.name);
                float length = 0f;
                for (int point = 1; point < link.Path.Length; point++)
                    length += Vector3.Distance(link.Path[point - 1], link.Path[point]);
                Assert.That(length, Is.GreaterThan(.05f), link.name + " has no usable transfer span.");
            }
        }

        List<T> ComponentsInGarden<T>() where T : Component
        {
            var result = new List<T>();
            foreach (var root in _scene.GetRootGameObjects())
                result.AddRange(root.GetComponentsInChildren<T>(true));
            return result;
        }
    }
}
