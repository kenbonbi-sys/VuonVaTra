using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using VuonNho.ArtQa;
using VuonNho.Core;
using VuonNho.Views;

namespace VuonNho.EditorTools
{
    public static class ArtQaBuild
    {
        const string PrefabFolder = "Assets/Art/Prefabs";
        const string IconFolder = "Assets/Art/Icons";

        /// <summary>
        /// Mot danh sach cho ca ba viec: chup model, chup icon, nhap sprite. Prefab chua co
        /// thi bo qua chu khong dung ca me, nen agent khac lam xong prefab la co icon ngay.
        /// Ten icon = "ICO_" + ten trong mang nay.
        /// </summary>
        /// <summary>
        /// Chup dung bo model ma bo nhap dung, khong chep lai danh sach: them mot model moi ma
        /// quen sua o day thi no lang le vang mat khoi tap anh de xet duyet.
        /// </summary>
        static readonly string[] Names = GardenArtImporter.AllModelNames();

        /// <summary>Anh chup nam o dau tuy tham so -art-output luc chup, nen thu lan luot.</summary>
        static readonly string[] CaptureFolders = { "Docs/Art/review/icons", "Docs/Art/captures/icons" };

        /// <summary>Id icon trong GardenSkin, di cung <see cref="IconModels"/> theo tung cap.</summary>
        static readonly string[] IconIds =
        {
            DefaultContent.CropMint, DefaultContent.CropChamomile, DefaultContent.CropStrawberry,
            DefaultContent.CropLemongrass, DefaultContent.CropJasmine,
            DefaultDecorations.StonePath, DefaultDecorations.Planter, DefaultDecorations.Lantern,
            DefaultDecorations.Bench, DefaultDecorations.Signboard,
            GardenSkin.IconPlot, GardenSkin.IconSeedling, GardenSkin.IconHelper, GardenSkin.IconStation
        };

        static readonly string[] IconModels =
        {
            "Mint", "Chamomile", "Strawberry", "Lemongrass", "Jasmine",
            "StonePath", "Planter", "Lantern", "Bench", "Signboard",
            "Plot", "Seedling", "Helper", "TeaStation"
        };

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

            // Prefab thieu chi lam mat mot tam anh, khong dang de hong ca ban dung.
            capture.Models = new GameObject[Names.Length];
            int found = 0;
            for (int i = 0; i < Names.Length; i++)
            {
                capture.Models[i] = AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrefabFolder + "/PF_" + Names[i] + ".prefab");
                if (capture.Models[i] != null) found++;
                else Debug.Log("[ArtQA] Chua co PF_" + Names[i] + ".prefab, bo qua.");
            }
            Debug.Log("[ArtQA] Se chup " + found + "/" + Names.Length + " prefab.");

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

        [MenuItem("Vườn Nhỏ/Art/Import icon từ ảnh kiểm tra")]
        public static void ImportIcons()
        {
            Directory.CreateDirectory(IconFolder);
            // Thu muc vua tao chua co .meta, AssetImporter.GetAtPath se tra null neu khong Refresh.
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            int imported = 0;
            var missing = new List<string>();
            for (int i = 0; i < Names.Length; i++)
            {
                string name = "ICO_" + Names[i] + ".png";
                string source = FindCapture(name);
                if (source == null) { missing.Add(name); continue; }

                string path = IconFolder + "/" + name;
                File.Copy(source, path, true);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) { missing.Add(name); continue; }
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 256;
                importer.SaveAndReimport();
                imported++;
            }

            var skin = SceneFactory.LoadOrCreateSkin();
            var filled = new List<string>();
            FillEmptyIconSlots(skin, filled);
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            Debug.Log("[ArtQA] Da nhap " + imported + "/" + Names.Length + " icon vao " + IconFolder +
                      "; gan them " + filled.Count + " o trong GardenSkin." +
                      (missing.Count == 0 ? "" : " Chua co anh chup: " + string.Join(", ", missing.ToArray())));
        }

        static string FindCapture(string fileName)
        {
            for (int i = 0; i < CaptureFolders.Length; i++)
            {
                string candidate = CaptureFolders[i] + "/" + fileName;
                if (File.Exists(candidate)) return candidate;
            }
            return null;
        }

        /// <summary>
        /// Chi dien vao o con trong, giu nguyen o da co — ai gan tay roi thi khong bi de len.
        /// Cung ky luat voi GardenArtImporter.
        /// </summary>
        static void FillEmptyIconSlots(GardenSkin skin, List<string> filled)
        {
            var entries = new List<IconSkinEntry>();
            if (skin.Icons != null)
                for (int i = 0; i < skin.Icons.Length; i++)
                    if (skin.Icons[i] != null) entries.Add(skin.Icons[i]);

            for (int i = 0; i < IconIds.Length; i++)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    IconFolder + "/ICO_" + IconModels[i] + ".png");
                if (sprite == null) continue;

                var existing = Find(entries, IconIds[i]);
                if (existing != null)
                {
                    if (existing.Icon != null) continue;
                    existing.Icon = sprite;
                }
                else
                {
                    entries.Add(new IconSkinEntry { Id = IconIds[i], Icon = sprite });
                }
                filled.Add(IconIds[i]);
            }
            skin.Icons = entries.ToArray();
        }

        static IconSkinEntry Find(List<IconSkinEntry> entries, string id)
        {
            for (int i = 0; i < entries.Count; i++)
                if (string.Equals(entries[i].Id, id, StringComparison.Ordinal)) return entries[i];
            return null;
        }
    }
}
