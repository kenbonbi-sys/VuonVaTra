using NUnit.Framework;
using UnityEngine;
using VuonNho.Views;

namespace VuonNho.Tests.ArtViews
{
    public sealed class StationHoverTests
    {
        [TestCase(1366f, 768f, -680f, -380f)]
        [TestCase(1366f, 768f, 680f, -380f)]
        [TestCase(1366f, 768f, -680f, 380f)]
        [TestCase(1366f, 768f, 680f, 380f)]
        [TestCase(1920f, 1080f, -958f, -538f)]
        [TestCase(1920f, 1080f, 958f, -538f)]
        [TestCase(1920f, 1080f, -958f, 538f)]
        [TestCase(1920f, 1080f, 958f, 538f)]
        public void EntireTooltipStaysWithinCanvasAtAllCorners(float width, float height, float x, float y)
        {
            var bounds = new Rect(-width / 2f, -height / 2f, width, height);
            var card = new Vector2(336f, 320f);
            Vector2 topLeft = StationHoverController.ClampTooltipPosition(bounds, new Vector2(x, y), card);
            Assert.That(topLeft.x, Is.GreaterThanOrEqualTo(bounds.xMin + 12f));
            Assert.That(topLeft.x + card.x, Is.LessThanOrEqualTo(bounds.xMax - 12f));
            Assert.That(topLeft.y, Is.LessThanOrEqualTo(bounds.yMax - 12f));
            Assert.That(topLeft.y - card.y, Is.GreaterThanOrEqualTo(bounds.yMin + 12f));
        }

        [Test]
        public void HighlightRestoresIndexedOverridesWithoutMutatingSharedMaterialOrStatusLight()
        {
            var root = new GameObject("HoverTest");
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            int colorId = Shader.PropertyToID(material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color");
            int extraId = Shader.PropertyToID("_HoverTestExtra");
            Color sharedColor = new Color(0.2f, 0.35f, 0.15f, 0.8f);
            Color globalColor = new Color(0.32f, 0.14f, 0.23f, 0.9f);
            Color indexedColor = new Color(0.11f, 0.21f, 0.31f, 0.7f);
            material.SetColor(colorId, sharedColor);
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { material, material };
            var statusObject = new GameObject("StatusLight");
            statusObject.transform.SetParent(root.transform);
            var status = statusObject.AddComponent<MeshRenderer>();
            status.sharedMaterial = material;
            var global = new MaterialPropertyBlock();
            global.SetColor(colorId, globalColor);
            global.SetFloat(extraId, 7f);
            renderer.SetPropertyBlock(global);
            status.SetPropertyBlock(global);
            var indexed = new MaterialPropertyBlock();
            indexed.SetColor(colorId, indexedColor);
            indexed.SetFloat(extraId, 19f);
            renderer.SetPropertyBlock(indexed, 1);
            var highlight = new MachineHoverHighlight();

            try
            {
                highlight.Apply(root.transform, status);
                var read = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(read, 0);
                Assert.That(read.GetColor(colorId).r, Is.GreaterThan(globalColor.r));
                Assert.That(read.GetColor(colorId).a, Is.EqualTo(globalColor.a).Within(0.001f));
                Assert.That(read.GetFloat(extraId), Is.EqualTo(7f));
                renderer.GetPropertyBlock(read, 1);
                Assert.That(read.GetColor(colorId).r, Is.GreaterThan(indexedColor.r));
                Assert.That(read.GetFloat(extraId), Is.EqualTo(19f));
                Assert.That(renderer.sharedMaterials[0], Is.SameAs(material));
                AssertColorUnchanged(material.GetColor(colorId), sharedColor);
                status.GetPropertyBlock(read, 0);
                Assert.That(read.isEmpty, Is.True, "The status light must not get a hover override.");
                status.GetPropertyBlock(read);
                AssertColorUnchanged(read.GetColor(colorId), globalColor);

                // Entering twice must not compound the tint or lose the original indexed block.
                highlight.Apply(root.transform, status);
                highlight.Restore();
                highlight.Restore();
                renderer.GetPropertyBlock(read, 0);
                Assert.That(read.isEmpty, Is.True, "An absent indexed block must be absent again on exit.");
                renderer.GetPropertyBlock(read, 1);
                AssertColorUnchanged(read.GetColor(colorId), indexedColor);
                Assert.That(read.GetFloat(extraId), Is.EqualTo(19f));
                renderer.GetPropertyBlock(read);
                AssertColorUnchanged(read.GetColor(colorId), globalColor);
                Assert.That(read.GetFloat(extraId), Is.EqualTo(7f));
                AssertColorUnchanged(material.GetColor(colorId), sharedColor);
            }
            finally
            {
                highlight.Restore();
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(material);
            }
        }

        /// <summary>
        /// Mau di qua Material hoac MaterialPropertyBlock cua Unity thi lech o nhung bit cuoi.
        /// Nhung phep so nay hoi "mau co bi doi khong", khong phai "co khop tung bit khong".
        /// </summary>
        static void AssertColorUnchanged(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.001f));
        }
    }
}
