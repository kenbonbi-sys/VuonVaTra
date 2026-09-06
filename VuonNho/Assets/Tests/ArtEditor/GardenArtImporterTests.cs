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
                    "TeaStation", "BackgroundTree", "Bush", "Fence", "Rock" }) prefabs[name] = imported;
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
