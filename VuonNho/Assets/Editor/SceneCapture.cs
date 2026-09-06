using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VuonNho.Views;

namespace VuonNho.EditorTools
{
    /// <summary>
    /// Chup khung hinh scene o 1366 x 768 de kiem tra framing, mau va do doc duoc
    /// ma khong can vao Play. Chay bang menu hoac -executeMethod.
    /// </summary>
    public static class SceneCapture
    {
        public const string OutputPath = "Build/garden-preview.png";

        [MenuItem("Vườn Nhỏ/5. Chụp ảnh scene 1366x768")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene(SceneFactory.ScenePath);

            var camera = Object.FindAnyObjectByType<Camera>();
            if (camera == null)
            {
                Debug.LogError("[VuonNho] Khong tim thay camera trong scene.");
                return;
            }

            // Bat tam vai trang thai cay de anh phan anh dung luc dang choi.
            var plots = Object.FindObjectsByType<PlotView>(FindObjectsSortMode.None);
            for (int i = 0; i < plots.Length; i++)
            {
                var plot = plots[i];
                if (plot.PlotId >= 8) continue;              // hai hang dau da mo trong anh minh hoa
                if (plot.CropVisuals == null || plot.CropVisuals.Length == 0) continue;

                var visual = plot.CropVisuals[plot.PlotId % plot.CropVisuals.Length];
                if (visual == null || visual.Root == null) continue;

                visual.Root.SetActive(true);
                float scale = plot.PlotId % 3 == 0 ? 1f : (plot.PlotId % 3 == 1 ? 0.7f : 0.85f);
                visual.Root.transform.localScale = new Vector3(scale, scale, scale);
                if (plot.ReadyBadge != null) plot.ReadyBadge.SetActive(plot.PlotId % 3 == 0);
            }

            const int width = 1366;
            const int height = 768;
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;

            // Trong batchmode, SRP Batcher khong nap dung constant buffer cua tung material,
            // lam anh chup ra mot mau duy nhat. Tat khi chup roi tra lai nguyen trang.
            bool previousBatching = GraphicsSettings.useScriptableRenderPipelineBatching;
            GraphicsSettings.useScriptableRenderPipelineBatching = false;

            camera.targetTexture = renderTexture;
            // Duoi URP phai dung render request; Camera.Render truc tiep khong di qua pipeline.
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = renderTexture };
            if (GraphicsSettings.currentRenderPipeline != null &&
                RenderPipeline.SupportsRenderRequest(camera, request))
                RenderPipeline.SubmitRenderRequest(camera, request);
            else
                camera.Render();
            RenderTexture.active = renderTexture;

            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();

            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            GraphicsSettings.useScriptableRenderPipelineBatching = previousBatching;

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            File.WriteAllBytes(OutputPath, texture.EncodeToPNG());

            Object.DestroyImmediate(texture);
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);

            Debug.Log("[VuonNho] Da chup: " + Path.GetFullPath(OutputPath));
        }
    }
}
