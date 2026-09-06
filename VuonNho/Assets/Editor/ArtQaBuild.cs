using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using VuonNho.ArtQa;
using VuonNho.Views;

namespace VuonNho.EditorTools
{
    public static class ArtQaBuild
    {
        static readonly string[] Names = { "Plot", "Seedling", "Mint", "Chamomile", "Strawberry",
            "Helper", "TeaStation", "BackgroundTree", "Bush", "Fence", "Rock" };

        [MenuItem("Vườn Nhỏ/Art/Build bản kiểm tra art (không dùng save)")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(SceneFactory.ScenePath);
            var bootstrap = UnityEngine.Object.FindAnyObjectByType<GameBootstrap>();
            var capture = new GameObject("ArtCapture").AddComponent<ArtCapture>();
            capture.Camera = bootstrap.GameCamera;
            capture.Plots = bootstrap.Plots;
            capture.Machine = bootstrap.Machine;
            capture.Helper = bootstrap.Helper;
            UnityEngine.Object.DestroyImmediate(bootstrap.Hud.gameObject);
            UnityEngine.Object.DestroyImmediate(bootstrap.gameObject);
            capture.Models = new GameObject[Names.Length];
            for (int i = 0; i < Names.Length; i++)
            {
                capture.Models[i] = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prefabs/PF_" + Names[i] + ".prefab");
                if (capture.Models[i] == null) throw new Exception("Missing QA model: " + Names[i]);
            }
            capture.Calibration = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/SM_CalibrationCube.fbx");
            const string scenePath = "Assets/QA/ArtReview.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { scenePath }, locationPathName = "Build/ArtReview/ArtReview.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
            });
            EditorSceneManager.OpenScene(SceneFactory.ScenePath);
            if (result.summary.result != BuildResult.Succeeded) throw new Exception("Art QA build failed");
        }

        [MenuItem("Vườn Nhỏ/Art/Import 7 icon từ ảnh kiểm tra")]
        public static void ImportIcons()
        {
            const string folder = "Assets/Art/Icons";
            Directory.CreateDirectory(folder);
            for (int i = 0; i < 7; i++)
            {
                string name = "ICO_" + Names[i] + ".png";
                string path = folder + "/" + name;
                string source = "Docs/Art/captures/icons/" + name;
                if (!File.Exists(source)) throw new Exception("Capture missing: " + source);
                File.Copy(source, path, true);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 256;
                importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
        }
    }
}
