using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VuonNho.Core;
using VuonNho.EditorTools;
using VuonNho.Views;

namespace VuonNho.Tests.ArtEditor
{
    public sealed class GardenArtImporterTests
    {
        GameObject calibration;

        [SetUp]
        public void SetUp()
        {
            calibration = new GameObject("SM_CalibrationCube");
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "CalibrationCube";
            cube.transform.SetParent(calibration.transform, false);
            cube.transform.localPosition = new Vector3(0, .5f, 0);
            AddMarker("FrontMarker", new Vector3(0, .5f, .65f));
            AddMarker("UpMarker", new Vector3(0, 1.15f, 0));
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(calibration); }

        [Test]
        public void CalibrationAcceptsOneMetreBottomPivotAndUnityAxes()
        {
            var result = GardenArtImporter.InspectCalibration(calibration);
            Assert.That(result.errors, Is.Empty);
            Assert.That(result.cubeSize, Is.EqualTo(Vector3.one));
            Assert.That(result.cubeBottomCenter, Is.EqualTo(Vector3.zero));
        }

        [TestCase(.01f)]
        [TestCase(100f)]
        public void CalibrationRejectsWrongExportUnits(float size)
        {
            calibration.transform.Find("CalibrationCube").localScale = Vector3.one * size;
            Assert.That(GardenArtImporter.InspectCalibration(calibration).Passed, Is.False);
        }

        [Test]
        public void CalibrationRejectsCentredPivot()
        {
            calibration.transform.Find("CalibrationCube").localPosition = Vector3.zero;
            Assert.That(GardenArtImporter.InspectCalibration(calibration).errors,
                Has.Some.Contains("pivot"));
        }

        [Test]
        public void CalibrationRejectsReversedFrontAxis()
        {
            calibration.transform.Find("FrontMarker").localPosition = new Vector3(0, .5f, -.65f);
            Assert.That(GardenArtImporter.InspectCalibration(calibration).errors,
                Has.Some.Contains("+Z"));
        }

        [Test]
        public void CalibrationRejectsZUp()
        {
            calibration.transform.Find("UpMarker").localPosition = new Vector3(0, 0, 1.15f);
            Assert.That(GardenArtImporter.InspectCalibration(calibration).errors,
                Has.Some.Contains("+Y"));
        }

        [Test]
        public void CalibrationRejectsMirroredChildEvenWhenBoundsMatch()
        {
            calibration.transform.Find("CalibrationCube").localScale = new Vector3(-1, 1, 1);
            Assert.That(GardenArtImporter.InspectCalibration(calibration).errors,
                Has.Some.Contains("negative scale"));
        }

        [Test]
        public void SkinFillPreservesArtistReferencesArrayIdentityOrderAndUnknownIds()
        {
            var skin = ScriptableObject.CreateInstance<GardenSkin>();
            var artist = new GameObject("ArtistEdited");
            var imported = new GameObject("Imported");
            try
            {
                skin.SoilPrefab = artist;
                var trees = new[] { artist, null, artist };
                skin.TreePrefabs = trees;
                var crops = new[]
                {
                    new CropSkinEntry { CropId = "artist_custom", MaturePrefab = null },
                    new CropSkinEntry { CropId = DefaultContent.CropStrawberry, MaturePrefab = artist },
                    new CropSkinEntry { CropId = DefaultContent.CropMint, MaturePrefab = null }
                };
                skin.Crops = crops;
                var prefabs = new Dictionary<string, GameObject>();
                foreach (var name in new[] { "Plot", "Seedling", "Mint", "Chamomile", "Strawberry", "Helper",
                    "TeaStation", "BackgroundTree", "Bush", "Fence", "Rock",
                    "StonePath", "Planter", "Lantern", "Bench", "Signboard" }) prefabs[name] = imported;
                var filled = new List<string>();
                GardenArtImporter.FillEmptySkinSlots(skin, prefabs, filled);
                Assert.That(skin.SoilPrefab, Is.SameAs(artist));
                Assert.That(skin.TreePrefabs, Is.SameAs(trees));
                Assert.That(skin.TreePrefabs, Is.EqualTo(new[] { artist, imported, artist }));
                Assert.That(skin.Crops, Is.SameAs(crops));
                Assert.That(crops[0].MaturePrefab, Is.Null);
                Assert.That(crops[1].MaturePrefab, Is.SameAs(artist));
                Assert.That(crops[2].MaturePrefab, Is.SameAs(imported));
                Assert.That(skin.Crops.Length, Is.EqualTo(3));
                filled.Clear();
                GardenArtImporter.FillEmptySkinSlots(skin, prefabs, filled);
                Assert.That(filled, Is.Empty, "A repeat import should not change any filled slot.");
            }
            finally
            {
                Object.DestroyImmediate(artist);
                Object.DestroyImmediate(imported);
                Object.DestroyImmediate(skin);
            }
        }

        [Test]
        public void DecorationIdsMapToTheirModelNames()
        {
            Assert.That(GardenArtImporter.DecorationModelName(DefaultDecorations.StonePath), Is.EqualTo("StonePath"));
            Assert.That(GardenArtImporter.DecorationModelName(DefaultDecorations.Planter), Is.EqualTo("Planter"));
            Assert.That(GardenArtImporter.DecorationModelName(DefaultDecorations.Lantern), Is.EqualTo("Lantern"));
            Assert.That(GardenArtImporter.DecorationModelName(DefaultDecorations.Bench), Is.EqualTo("Bench"));
            Assert.That(GardenArtImporter.DecorationModelName(DefaultDecorations.Signboard), Is.EqualTo("Signboard"));
            Assert.That(GardenArtImporter.DecorationModelName("deco_artist_custom"), Is.Null);
        }

        [Test]
        public void EveryDefaultDecorationHasAModel()
        {
            foreach (var definition in DefaultContent.Create().Decorations)
            {
                string model = GardenArtImporter.DecorationModelName(definition.Id);
                Assert.That(model, Is.Not.Null, "Decoration " + definition.Id + " has no Blender model mapped.");
                Assert.That(GardenArtImporter.PrefabFolder + "/PF_" + model + ".prefab",
                    Does.StartWith("Assets/Art/Prefabs/PF_").And.EndsWith(".prefab"));
            }
        }

        [Test]
        public void DecorationSlotsAppendOnceAndKeepArtistRows()
        {
            var skin = ScriptableObject.CreateInstance<GardenSkin>();
            var artist = new GameObject("ArtistEdited");
            var imported = new GameObject("Imported");
            try
            {
                var rows = new[]
                {
                    new DecorationSkinEntry { DecorationId = "deco_artist_custom", Prefab = artist },
                    new DecorationSkinEntry { DecorationId = DefaultDecorations.Bench, Prefab = artist }
                };
                skin.Decorations = rows;
                var prefabs = new Dictionary<string, GameObject>();
                foreach (var name in new[] { "Plot", "Seedling", "Mint", "Chamomile", "Strawberry", "Helper",
                    "TeaStation", "BackgroundTree", "Bush", "Fence", "Rock",
                    "StonePath", "Planter", "Lantern", "Bench", "Signboard" }) prefabs[name] = imported;
                var filled = new List<string>();
                GardenArtImporter.FillEmptySkinSlots(skin, prefabs, filled);
                Assert.That(skin.Decorations.Length, Is.EqualTo(6));
                Assert.That(skin.DecorationPrefabFor("deco_artist_custom"), Is.SameAs(artist));
                Assert.That(skin.DecorationPrefabFor(DefaultDecorations.Bench), Is.SameAs(artist));
                Assert.That(skin.DecorationPrefabFor(DefaultDecorations.StonePath), Is.SameAs(imported));
                Assert.That(skin.DecorationPrefabFor(DefaultDecorations.Planter), Is.SameAs(imported));
                Assert.That(skin.DecorationPrefabFor(DefaultDecorations.Lantern), Is.SameAs(imported));
                Assert.That(skin.DecorationPrefabFor(DefaultDecorations.Signboard), Is.SameAs(imported));
                filled.Clear();
                GardenArtImporter.FillEmptySkinSlots(skin, prefabs, filled);
                Assert.That(skin.Decorations.Length, Is.EqualTo(6), "A repeat import must not duplicate a decoration row.");
                Assert.That(filled, Is.Empty, "A repeat import should not change any filled slot.");
            }
            finally
            {
                Object.DestroyImmediate(artist);
                Object.DestroyImmediate(imported);
                Object.DestroyImmediate(skin);
            }
        }

        [Test]
        public void DecorationModelsFitInsideTheirFootprint()
        {
            foreach (var definition in DefaultContent.Create().Decorations)
            {
                string name = GardenArtImporter.DecorationModelName(definition.Id);
                Assert.That(name, Is.Not.Null, definition.Id);
                string path = GardenArtImporter.ModelFolder + "/SM_" + name + ".fbx";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (model == null || importer == null || !importer.bakeAxisConversion)
                    Assert.Ignore("Run the explicit Blender import before measuring real footprints.");
                Bounds bounds;
                Assert.That(GardenArtImporter.TryGetBounds(model.transform, model.transform, out bounds), Is.True, path);
                // FootprintMm is the radius of a circle in GameSession, so the measure is the
                // longest horizontal reach from the pivot, not half of the bounding box.
                float dx = Mathf.Max(Mathf.Abs(bounds.min.x), Mathf.Abs(bounds.max.x));
                float dz = Mathf.Max(Mathf.Abs(bounds.min.z), Mathf.Abs(bounds.max.z));
                Assert.That(Mathf.Sqrt(dx * dx + dz * dz),
                    Is.LessThanOrEqualTo(definition.FootprintMm / 1000f + .001f),
                    name + " reaches outside the placement radius of " + definition.Id);
            }
        }

        [Test]
        public void ImportedCalibrationRetainsRealMetreBounds()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(GardenArtImporter.CalibrationPath);
            if (model == null) Assert.Ignore("Run the explicit Blender import before checking production exports.");
            var result = GardenArtImporter.InspectCalibration(model);
            Assert.That(result.errors, Is.Empty, string.Join("; ", result.errors));
        }

        void AddMarker(string name, Vector3 position)
        {
            var marker = new GameObject(name);
            marker.transform.SetParent(calibration.transform, false);
            marker.transform.localPosition = position;
        }
    }
}
