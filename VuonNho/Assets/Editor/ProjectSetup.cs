using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VuonNho.EditorTools
{
    /// <summary>
    /// Chay bang menu chu dong, khong chay moi lan Play. Chi tao/cap nhat dung cac duong dan
    /// duoc chi dinh va khong ghi de visual da chinh tay.
    /// </summary>
    public static class ProjectSetup
    {
        public const string SettingsFolder = "Assets/Settings";
        public const string RendererPath = SettingsFolder + "/VuonNho-UniversalRenderer.asset";
        public const string PipelinePath = SettingsFolder + "/VuonNho-UniversalRenderPipeline.asset";

        [MenuItem("Vườn Nhỏ/1. Thiết lập project (URP + tên sản phẩm)")]
        public static void Configure()
        {
            PlayerSettings.companyName = "Vuon va Tra";
            PlayerSettings.productName = "Vuon va Tra";
            PlayerSettings.bundleVersion = "0.2.0";
            PlayerSettings.defaultScreenWidth = 1366;
            PlayerSettings.defaultScreenHeight = 768;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;

            SetupUniversalRenderPipeline();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VuonNho] Da thiet lap project. Render pipeline: " +
                      (GraphicsSettings.defaultRenderPipeline != null
                          ? GraphicsSettings.defaultRenderPipeline.name
                          : "Built-in (URP chua duoc gan)"));
        }

        static void SetupUniversalRenderPipeline()
        {
            try
            {
                if (!Directory.Exists(SettingsFolder)) Directory.CreateDirectory(SettingsFolder);

                var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
                if (rendererData == null)
                {
                    rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                    AssetDatabase.CreateAsset(rendererData, RendererPath);
                }

                var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
                if (pipeline == null)
                {
                    pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                    AssetDatabase.CreateAsset(pipeline, PipelinePath);
                }

                AssetDatabase.SaveAssets();

                GraphicsSettings.defaultRenderPipeline = pipeline;
                QualitySettings.renderPipeline = pipeline;
                EditorUtility.SetDirty(GraphicsSettings.GetGraphicsSettings());
                AssetDatabase.SaveAssets();
            }
            catch (Exception error)
            {
                // Khong chan viec dung scene: bang shader se tu chuyen ve Built-in.
                Debug.LogWarning("[VuonNho] Chua gan duoc URP, van dung Built-in: " + error.Message);
            }
        }

        /// <summary>
        /// Material Lit mac dinh cua pipeline dang bat, dung lam ban mau khi tao material moi.
        /// </summary>
        public static Material LitTemplate()
        {
            var pipeline = GraphicsSettings.defaultRenderPipeline;
            if (pipeline != null)
            {
                if (pipeline.defaultMaterial != null) return pipeline.defaultMaterial;
                var packaged = AssetDatabase.LoadAssetAtPath<Material>(
                    "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Lit.mat");
                if (packaged != null) return packaged;
            }
            return null;
        }

        /// <summary>Shader hop voi pipeline dang bat, de scene khong bi mau hong.</summary>
        public static Shader LitShader()
        {
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                var urpLit = Shader.Find("Universal Render Pipeline/Lit");
                if (urpLit != null) return urpLit;
            }
            return Shader.Find("Standard");
        }
    }
}
