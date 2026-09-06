using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VuonNho.Core;
using VuonNho.Views;
using Object = UnityEngine.Object;

namespace VuonNho.EditorTools
{
    /// <summary>Explicit L01/B02 import. Reimports stable model paths and fills empty skin slots only.</summary>
    public static class GardenArtImporter
    {
        public const string ModelFolder = "Assets/Art/Models";
        public const string MaterialFolder = "Assets/Art/Materials";
        public const string PrefabFolder = "Assets/Art/Prefabs";
        public const string IconFolder = "Assets/Art/Icons";
        public const string ReportPath = "Docs/Art/import-report.json";
        public const string CalibrationPath = ModelFolder + "/SM_CalibrationCube.fbx";

        static readonly string[] MaterialNames =
        {
            "M_Grass", "M_Leaf", "M_Soil", "M_Wood", "M_Cream", "M_Dark", "M_Yellow", "M_Red", "M_Accent"
        };
        static readonly string[] MaterialColors =
        {
            "#A8BC78", "#4F7B50", "#916447", "#B88858", "#F2E3BE", "#303C32", "#E9BF5C", "#D97672", "#81B8AB"
        };
        // New names go at the END of both arrays: Run pairs them by index and
        // ValidateCalibrationAndMint reads MaximumDimensions[2] for Mint.
        static readonly string[] ModelNames =
        {
            "Plot", "Seedling", "Mint", "Chamomile", "Strawberry", "Lemongrass", "Jasmine",
            "Helper", "TeaStation", "BackgroundTree", "Bush", "Fence", "Rock",
            "StonePath", "Planter", "Lantern", "Bench", "Signboard", "Gardener"
        };
        static readonly Vector3[] MaximumDimensions =
        {
            new Vector3(1.6f, .3f, 1.6f), new Vector3(1.1f, .5f, 1.1f),
            new Vector3(1.15f, 1f, 1.15f), new Vector3(1.15f, 1f, 1.15f), new Vector3(1.15f, 1f, 1.15f),
            new Vector3(1.15f, 1f, 1.15f), new Vector3(1.15f, 1f, 1.15f),
            new Vector3(1.2f, 1.5f, 1.2f), new Vector3(3.4f, 2.3f, 1.9f),
            new Vector3(3f, 4f, 3f), new Vector3(1.8f, 1.4f, 1.8f),
            new Vector3(1.8f, 1.3f, .5f), new Vector3(1.5f, 1f, 1.5f),
            new Vector3(.44f, .30f, .44f), new Vector3(.52f, .80f, .52f),
            new Vector3(.44f, 1.30f, .44f), new Vector3(1.04f, 1f, 1.04f),
            new Vector3(.84f, 1.40f, .84f), new Vector3(.9f, 1.6f, .9f)
        };
        static readonly string[] DecorationIds =
        {
            DefaultDecorations.StonePath, DefaultDecorations.Planter, DefaultDecorations.Lantern,
            DefaultDecorations.Bench, DefaultDecorations.Signboard
        };
        static readonly string[] DecorationModels =
        {
            "StonePath", "Planter", "Lantern", "Bench", "Signboard"
        };
        // The gameplay camera sits at Euler(35,-45,0) and looks toward (-X,+Z), while a model's
        // authored front is Blender +Y = Unity +Z, and GameBootstrap always places a decoration
        // with rotationDeg 0. Bench and signboard therefore need the same facing yaw SceneFactory
        // gives the robot, or they show the player their back.
        const float DecorationFacingYaw = 135f;

        /// <summary>Model name for a decoration id; null for ids an artist added by hand.</summary>
        public static string DecorationModelName(string decorationId)
        {
            for (int i = 0; i < DecorationIds.Length; i++)
                if (string.Equals(DecorationIds[i], decorationId, StringComparison.Ordinal))
                    return DecorationModels[i];
            return null;
        }

        [Serializable]
        public sealed class CalibrationInspection
        {
            public Vector3 cubeSize;
            public Vector3 cubeBottomCenter;
            public Vector3 frontMarker;
            public Vector3 upMarker;
            public List<string> errors = new List<string>();
            public bool Passed => errors.Count == 0;
        }

        [Serializable]
        public sealed class ModelInspection
        {
            public string name;
            public string path;
            public string guid;
            public Vector3 size;
            public Vector3 center;
            public int triangles;
            public int renderers;
            public float fileScale;
            public List<string> materialNames = new List<string>();
            public List<string> errors = new List<string>();
        }

        [Serializable]
        public sealed class ImportReport
        {
            public string utc;
            public string unityVersion;
            public string preset = "FBX static; globalScale 1; Convert Units on; Bake Axis Conversion on; imported normals; hierarchy preserved; no animation, cameras, lights or colliders";
            public bool passed;
            public bool skinAssigned;
            public bool sceneRebuilt;
            public CalibrationInspection calibration;
            public List<ModelInspection> models = new List<ModelInspection>();
            public List<string> createdPrefabs = new List<string>();
            public List<string> preservedPrefabs = new List<string>();
            public List<string> filledSlots = new List<string>();
            public List<string> errors = new List<string>();
        }

        /// <summary>
        /// Ten cac model bo nhap ky vong co. Test dung chinh danh sach nay chu khong chep lai:
        /// mot danh sach chep doi la mot lan them model la mot lan test do vi ly do sai.
        /// </summary>
        public static string[] AllModelNames() { return (string[])ModelNames.Clone(); }

        [MenuItem("Vườn Nhỏ/Art/1. Import Blender và điền GardenSkin")]
        public static void ImportOnly() { Run(false); }

        /// <summary>Batch: -executeMethod VuonNho.EditorTools.GardenArtImporter.ImportAndBuild.</summary>
        [MenuItem("Vườn Nhỏ/Art/2. Import Blender và dựng scene Garden")]
        public static void ImportAndBuild() { Run(true); }

        /// <summary>L01 gate can run before the other ten production models exist.</summary>
        [MenuItem("Vườn Nhỏ/Art/0. Kiểm cube 1 m và cây bạc hà")]
        public static void ValidateCalibrationAndMint()
        {
            const string path = "Docs/Art/l01-calibration-report.json";
            var report = new ImportReport { utc = DateTime.UtcNow.ToString("O"), unityVersion = Application.unityVersion };
            try
            {
                EnsureFolder(MaterialFolder);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var materials = LoadMaterials();
                ImportModel(CalibrationPath, materials);
                report.calibration = InspectCalibration(AssetDatabase.LoadAssetAtPath<GameObject>(CalibrationPath));
                if (!report.calibration.Passed)
                    throw new InvalidOperationException("Calibration failed: " + string.Join("; ", report.calibration.errors));
                string mintPath = ModelFolder + "/SM_Mint.fbx";
                ImportModel(mintPath, materials);
                var mint = InspectModel(AssetDatabase.LoadAssetAtPath<GameObject>(mintPath), "Mint", mintPath, MaximumDimensions[2], materials);
                report.models.Add(mint);
                if (mint.errors.Count != 0) throw new InvalidOperationException("Mint gate failed: " + string.Join("; ", mint.errors));
                report.passed = true;
                Debug.Log("[VuonNho Art] L01 cube and mint passed. " + path);
            }
            catch (Exception error) { report.errors.Add(error.Message); throw; }
            finally
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(report, true));
            }
        }

        static void Run(bool buildScene)
        {
            var report = new ImportReport
            {
                utc = DateTime.UtcNow.ToString("O"), unityVersion = Application.unityVersion
            };
            try
            {
                EnsureFolder(MaterialFolder);
                EnsureFolder(PrefabFolder);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var materials = LoadMaterials();
                ImportModel(CalibrationPath, materials);
                report.calibration = InspectCalibration(AssetDatabase.LoadAssetAtPath<GameObject>(CalibrationPath));
                if (!report.calibration.Passed)
                    throw new InvalidOperationException("Calibration failed: " + string.Join("; ", report.calibration.errors));

                var models = new Dictionary<string, GameObject>();
                for (int i = 0; i < ModelNames.Length; i++)
                {
                    string name = ModelNames[i];
                    string path = ModelFolder + "/SM_" + name + ".fbx";
                    ImportModel(path, materials);
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    var inspection = InspectModel(model, name, path, MaximumDimensions[i], materials);
                    report.models.Add(inspection);
                    models.Add(name, model);
                }
                foreach (var inspection in report.models)
                    foreach (var error in inspection.errors) report.errors.Add(inspection.name + ": " + error);
                if (report.errors.Count != 0)
                    throw new InvalidOperationException("Model import gate failed. See " + ReportPath);

                var prefabs = new Dictionary<string, GameObject>();
                foreach (var entry in models)
                    prefabs.Add(entry.Key, LoadOrCreateWrapper(entry.Key, entry.Value, report));
                var skin = SceneFactory.LoadOrCreateSkin();
                FillEmptySkinSlots(skin, prefabs, report.filledSlots);
                if (skin.ReadyBadgePrefab == null)
                    skin.ReadyBadgePrefab = LoadOrCreateUtilityPrefab("ReadyBadge", materials, skin, report);
                if (skin.LockedOverlayPrefab == null)
                    skin.LockedOverlayPrefab = LoadOrCreateUtilityPrefab("LockedOverlay", materials, skin, report);
                if (skin.GroundPrefab == null)
                    skin.GroundPrefab = LoadOrCreateUtilityPrefab("Ground", materials, skin, report);
                FillEmptyAudio(skin, report.filledSlots);
                FillEmptyIcons(skin, report.filledSlots);
                EditorUtility.SetDirty(skin);
                AssetDatabase.SaveAssets();
                report.skinAssigned = true;
                if (buildScene)
                {
                    SceneFactory.BuildScene();
                    report.sceneRebuilt = true;
                }
                report.passed = true;
                Debug.Log("[VuonNho Art] Calibration + " + ModelNames.Length +
                          " models passed. Existing prefabs and non-empty GardenSkin slots preserved. " + ReportPath);
            }
            catch (Exception error)
            {
                if (!report.errors.Contains(error.Message)) report.errors.Add(error.Message);
                Debug.LogError("[VuonNho Art] " + error.Message);
                throw;
            }
            finally
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
                File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            }
        }

        static Dictionary<string, Material> LoadMaterials()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP/Lit shader is missing.");
            var template = AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Lit.mat");
            var result = new Dictionary<string, Material>();
            for (int i = 0; i < MaterialNames.Length; i++)
            {
                string name = MaterialNames[i];
                string path = MaterialFolder + "/" + name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = template != null ? new Material(template) : new Material(shader);
                    material.name = name;
                    ColorUtility.TryParseHtmlString(MaterialColors[i], out var color);
                    material.SetColor("_BaseColor", color);
                    material.SetColor("_Color", color);
                    material.SetFloat("_Metallic", 0f);
                    material.SetFloat("_Smoothness", .25f);
                    AssetDatabase.CreateAsset(material, path);
                }
                // Keep an artist's calibrated colors on repeat imports, with the same asset GUID.
                if (material.shader != shader)
                    throw new InvalidOperationException(path + " must use Universal Render Pipeline/Lit.");
                result.Add(name, material);
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        static void ImportModel(string path, Dictionary<string, Material> materials)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Required Blender export is missing.", path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new InvalidOperationException(path + " is not a model asset.");
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.preserveHierarchy = true;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.None;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            foreach (var material in materials)
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), material.Key), material.Value);
            importer.SaveAndReimport();
        }

        /// <summary>Measures geometry in model-root space, so the gate detects 0.01/100 scale compensation.</summary>
        public static CalibrationInspection InspectCalibration(GameObject model)
        {
            var result = new CalibrationInspection();
            if (model == null) { result.errors.Add("Calibration model missing."); return result; }
            ValidateTransforms(model, result.errors);
            var cube = FindDeep(model.transform, "CalibrationCube");
            if (cube == null || !TryGetBounds(cube, model.transform, out var bounds))
                result.errors.Add("Mesh CalibrationCube is missing.");
            else
            {
                result.cubeSize = bounds.size;
                result.cubeBottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                if (!Approximately(bounds.size, Vector3.one, .005f)) result.errors.Add("Cube must measure exactly 1 x 1 x 1 Unity metres.");
                if (!Approximately(result.cubeBottomCenter, Vector3.zero, .005f)) result.errors.Add("Cube pivot must be at bottom centre.");
            }
            var front = FindDeep(model.transform, "FrontMarker");
            if (front == null) result.errors.Add("FrontMarker is missing.");
            else
            {
                result.frontMarker = model.transform.InverseTransformPoint(front.position);
                if (result.frontMarker.z <= .5f || Mathf.Abs(result.frontMarker.x) > .02f)
                    result.errors.Add("FrontMarker must point toward Unity +Z, beyond the cube front.");
            }
            var up = FindDeep(model.transform, "UpMarker");
            if (up == null) result.errors.Add("UpMarker is missing.");
            else
            {
                result.upMarker = model.transform.InverseTransformPoint(up.position);
                if (result.upMarker.y <= 1f || Mathf.Abs(result.upMarker.x) > .02f || Mathf.Abs(result.upMarker.z) > .02f)
                    result.errors.Add("UpMarker must point toward Unity +Y, above the cube.");
            }
            return result;
        }

        static ModelInspection InspectModel(GameObject model, string name, string path, Vector3 maximum,
                                             Dictionary<string, Material> materials)
        {
            var result = new ModelInspection { name = name, path = path, guid = AssetDatabase.AssetPathToGUID(path) };
            if (model == null) { result.errors.Add("Model asset missing."); return result; }
            ValidateTransforms(model, result.errors);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                result.fileScale = importer.fileScale;
                if (!Mathf.Approximately(importer.globalScale, 1f) || !importer.useFileScale)
                    result.errors.Add("Importer must use scale factor 1 and Convert Units.");
            }
            if (!TryGetBounds(model.transform, model.transform, out var bounds)) result.errors.Add("No mesh geometry.");
            else
            {
                result.size = bounds.size;
                result.center = bounds.center;
                if (bounds.size.x < .05f || bounds.size.y < .05f || bounds.size.z < .01f ||
                    bounds.size.x > maximum.x || bounds.size.y > maximum.y || bounds.size.z > maximum.z)
                    result.errors.Add("Dimensions outside the art budget: " + bounds.size + "; maximum " + maximum);
                if (name != "Helper" && Mathf.Abs(bounds.min.y) > .025f)
                    result.errors.Add("Bottom pivot must sit on y=0; mesh bottom is " + bounds.min.y);
                if (name == "Plot" && (Mathf.Abs(bounds.size.x - 1.4f) > .03f || Mathf.Abs(bounds.size.z - 1.4f) > .03f || Mathf.Abs(bounds.max.y - .17f) > .015f))
                    result.errors.Add("Plot must be 1.4 m square with top at y=0.17 to meet CropAnchor.");
            }
            var meshFilters = model.GetComponentsInChildren<MeshFilter>(true);
            foreach (var filter in meshFilters)
            {
                var mesh = filter.sharedMesh;
                if (mesh == null) { result.errors.Add(filter.name + " has no mesh."); continue; }
                if (!mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Normal)) result.errors.Add(filter.name + " has no imported normals.");
                for (int i = 0; i < mesh.subMeshCount; i++) result.triangles += (int)(mesh.GetIndexCount(i) / 3);
                var renderer = filter.GetComponent<Renderer>();
                if (renderer == null || renderer.sharedMaterials.Length != mesh.subMeshCount)
                    result.errors.Add(filter.name + " material slot count does not match submeshes.");
            }
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                result.renderers++;
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null) { result.errors.Add(renderer.name + " has an empty material slot."); continue; }
                    if (!materials.TryGetValue(material.name, out var canonical) || material != canonical)
                        result.errors.Add(renderer.name + " material is not remapped to the shared palette: " + material.name);
                    if (!result.materialNames.Contains(material.name)) result.materialNames.Add(material.name);
                }
            }
            if (model.GetComponentsInChildren<Collider>(true).Length != 0 || model.GetComponentsInChildren<Camera>(true).Length != 0 ||
                model.GetComponentsInChildren<Light>(true).Length != 0 || model.GetComponentsInChildren<Animator>(true).Length != 0)
                result.errors.Add("Static visual contains a collider, camera, light or Animator.");
            if (name == "Mint" || name == "Chamomile" || name == "Strawberry")
            {
                RequireChild(model, "FoliageRoot", result.errors);
                RequireChild(model, "ReadyAccents", result.errors);
            }
            if (name == "Helper")
            {
                RequireChild(model, "Body", result.errors);
                RequireChild(model, "Face", result.errors);
                RequireChild(model, "Antenna", result.errors);
            }
            if (name == "TeaStation")
            {
                RequireChild(model, "StatusAnchor", result.errors);
                RequireChild(model, "SteamAnchor", result.errors);
            }
            return result;
        }

        static void ValidateTransforms(GameObject model, List<string> errors)
        {
            if (!Approximately(model.transform.localScale, Vector3.one, .0001f)) errors.Add("Model root scale must be 1,1,1.");
            if (!Approximately(model.transform.localPosition, Vector3.zero, .0001f) || Quaternion.Angle(model.transform.localRotation, Quaternion.identity) > .01f)
                errors.Add("Model root must have zero position and identity rotation; fix source/export axes.");
            foreach (var child in model.GetComponentsInChildren<Transform>(true))
            {
                var scale = child.localScale;
                if (!float.IsFinite(scale.x) || !float.IsFinite(scale.y) || !float.IsFinite(scale.z) || scale.x <= 0 || scale.y <= 0 || scale.z <= 0)
                    errors.Add(child.name + " contains invalid or negative scale.");
            }
        }

        static GameObject LoadOrCreateWrapper(string name, GameObject model, ImportReport report)
        {
            string path = PrefabFolder + "/PF_" + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) { report.preservedPrefabs.Add(path); return existing; }
            var root = new GameObject("PF_" + name);
            try
            {
                var visual = new GameObject("VisualRoot");
                visual.transform.SetParent(root.transform, false);
                // The yaw has to live on VisualRoot: DecorationLayer forces the wrapper root back to
                // identity on every rebuild, so a yaw stored there would be wiped at runtime.
                visual.transform.localRotation = Quaternion.Euler(0f, VisualYawFor(name), 0f);
                // Nest the original model prefab: reexport updates geometry without replacing wrappers.
                var instance = PrefabUtility.InstantiatePrefab(model, visual.transform) as GameObject;
                if (instance == null) throw new InvalidOperationException("Unable to instantiate " + name);
                var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                report.createdPrefabs.Add(path);
                return saved;
            }
            finally { Object.DestroyImmediate(root); }
        }

        /// <summary>Only a decoration with an authored front needs a facing yaw; everything else stays at 0.</summary>
        static float VisualYawFor(string name)
        {
            return name == "Bench" || name == "Signboard" ? DecorationFacingYaw : 0f;
        }

        /// <summary>Only known IDs are matched; arrays, order, custom entries and existing references survive.</summary>
        public static void FillEmptySkinSlots(GardenSkin skin, IDictionary<string, GameObject> prefabs, List<string> filled)
        {
            Fill(ref skin.SoilPrefab, prefabs["Plot"], "SoilPrefab", filled);
            Fill(ref skin.SeedlingPrefab, prefabs["Seedling"], "SeedlingPrefab", filled);
            Fill(ref skin.RobotPrefab, prefabs["Helper"], "RobotPrefab", filled);
            Fill(ref skin.CharacterPrefab, prefabs["Gardener"], "CharacterPrefab", filled);
            Fill(ref skin.StationPrefab, prefabs["TeaStation"], "StationPrefab", filled);
            Fill(ref skin.FencePostPrefab, prefabs["Fence"], "FencePostPrefab", filled);
            FillOptions(ref skin.TreePrefabs, prefabs["BackgroundTree"], "TreePrefabs", filled);
            FillOptions(ref skin.BushPrefabs, prefabs["Bush"], "BushPrefabs", filled);
            FillOptions(ref skin.RockPrefabs, prefabs["Rock"], "RockPrefabs", filled);
            // Before the early return below: a skin with no crop rows still deserves its decorations.
            FillDecorationSlots(skin, prefabs, filled);
            if (skin.Crops == null) return;
            foreach (var crop in skin.Crops)
            {
                if (crop == null || crop.MaturePrefab != null) continue;
                string model = crop.CropId == DefaultContent.CropMint ? "Mint" :
                    crop.CropId == DefaultContent.CropChamomile ? "Chamomile" :
                    crop.CropId == DefaultContent.CropStrawberry ? "Strawberry" :
                    crop.CropId == DefaultContent.CropLemongrass ? "Lemongrass" :
                    crop.CropId == DefaultContent.CropJasmine ? "Jasmine" : null;
                if (model != null) Fill(ref crop.MaturePrefab, prefabs[model], crop.CropId, filled);
            }
        }

        static void Fill(ref GameObject slot, GameObject prefab, string name, List<string> filled)
        {
            if (slot != null) return;
            slot = prefab;
            filled.Add(name);
        }

        static void FillOptions(ref GameObject[] slots, GameObject prefab, string name, List<string> filled)
        {
            if (slots == null || slots.Length == 0) { slots = new[] { prefab }; filled.Add(name + "[0]"); return; }
            for (int i = 0; i < slots.Length; i++) Fill(ref slots[i], prefab, name + "[" + i + "]", filled);
        }

        /// <summary>
        /// Nothing generates decoration rows, so an id the skin has never seen is appended.
        /// An id already present keeps whatever prefab the artist put there.
        /// </summary>
        static void FillDecorationSlots(GardenSkin skin, IDictionary<string, GameObject> prefabs, List<string> filled)
        {
            var entries = new List<DecorationSkinEntry>(skin.Decorations ?? new DecorationSkinEntry[0]);
            bool added = false;
            for (int i = 0; i < DecorationIds.Length; i++)
            {
                GameObject prefab;
                // TryGetValue, not the indexer: a missing model is skipped, never thrown over.
                if (!prefabs.TryGetValue(DecorationModels[i], out prefab) || prefab == null) continue;
                int found = -1;
                for (int j = 0; j < entries.Count; j++)
                    if (entries[j] != null && string.Equals(entries[j].DecorationId, DecorationIds[i], StringComparison.Ordinal))
                    {
                        found = j;
                        break;
                    }
                if (found < 0)
                {
                    entries.Add(new DecorationSkinEntry { DecorationId = DecorationIds[i], Prefab = prefab });
                    filled.Add(DecorationIds[i]);
                    added = true;
                }
                else
                {
                    DecorationSkinEntry row = entries[found];
                    Fill(ref row.Prefab, prefab, DecorationIds[i], filled);
                }
            }
            if (added) skin.Decorations = entries.ToArray();
        }

        static GameObject LoadOrCreateUtilityPrefab(string name, Dictionary<string, Material> materials, GardenSkin skin, ImportReport report)
        {
            string path = PrefabFolder + "/PF_" + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) { report.preservedPrefabs.Add(path); return existing; }
            var root = new GameObject("PF_" + name);
            try
            {
                if (name == "Ground")
                    AddPrimitive(root.transform, "GroundMesh", PrimitiveType.Cube, new Vector3(0, -.25f, 0),
                        new Vector3(skin.GroundSize, .5f, skin.GroundSize), materials["M_Grass"]);
                else if (name == "ReadyBadge")
                {
                    var diamond = AddPrimitive(root.transform, "ReadyDiamond", PrimitiveType.Cube, Vector3.zero,
                        Vector3.one * .15f, materials["M_Yellow"]);
                    diamond.transform.localRotation = Quaternion.Euler(0, 0, 45);
                }
                else
                {
                    for (int i = 0; i < 2; i++)
                    {
                        var cross = AddPrimitive(root.transform, "LockedCross_" + i, PrimitiveType.Cube,
                            new Vector3(0, .19f, 0), new Vector3(.105f, .025f, 1.13f), materials["M_Dark"]);
                        cross.transform.localRotation = Quaternion.Euler(0, i == 0 ? 45 : -45, 0);
                    }
                }
                var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                report.createdPrefabs.Add(path);
                report.filledSlots.Add(name + "Prefab");
                return saved;
            }
            finally { Object.DestroyImmediate(root); }
        }

        static GameObject AddPrimitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        static void FillEmptyAudio(GardenSkin skin, List<string> filled)
        {
            var serialized = new SerializedObject(skin);
            foreach (string cue in new[] { "Click", "Plant", "Harvest", "Brew", "Upgrade" })
            {
                string path = "Assets/Audio/SFX_" + cue + ".wav";
                if (!File.Exists(path)) continue;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                var slot = serialized.FindProperty(cue + "Clip");
                if (slot != null && slot.objectReferenceValue == null && clip != null)
                {
                    slot.objectReferenceValue = clip;
                    filled.Add(cue + "Clip");
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Icon HUD cua nam cay. Anh nguon la file ve tay, khong phai dau ra cua Blender, nen no
        /// chi di qua buoc chinh lai cai dat import chu khong qua cong kiem model.
        ///
        /// Chinh cai dat trong code chu khong dua vao file .meta co san: anh moi keo vao du an se
        /// vao voi kieu Default va HUD se khong hien duoc no, ma loi do khong noi ra la vi sao.
        /// </summary>
        static void FillEmptyIcons(GardenSkin skin, List<string> filled)
        {
            var cropIcons = new Dictionary<string, string>
            {
                { DefaultContent.CropMint, "Mint" },
                { DefaultContent.CropChamomile, "Chamomile" },
                { DefaultContent.CropStrawberry, "Strawberry" },
                { DefaultContent.CropLemongrass, "Lemongrass" },
                { DefaultContent.CropJasmine, "Jasmine" }
            };

            var icons = new List<IconSkinEntry>(skin.Icons ?? new IconSkinEntry[0]);
            foreach (var entry in cropIcons)
            {
                if (icons.Exists(existing => existing != null && existing.Id == entry.Key)) continue;

                string path = IconFolder + "/ICO_" + entry.Value + ".png";
                if (!File.Exists(path)) continue;
                ConfigureIconImport(path);

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) continue;
                icons.Add(new IconSkinEntry { Id = entry.Key, Icon = sprite });
                filled.Add("Icons[" + entry.Key + "]");
            }
            skin.Icons = icons.ToArray();
        }

        /// <summary>Anh HUD: sprite, giu alpha, khong mipmap vi no luon duoc ve dung mot co.</summary>
        static void ConfigureIconImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            if (importer.textureType == TextureImporterType.Sprite &&
                importer.spriteImportMode == SpriteImportMode.Single &&
                importer.alphaIsTransparency && !importer.mipmapEnabled) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 256;
            importer.SaveAndReimport();
        }

        public static bool TryGetBounds(Transform target, Transform relativeTo, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (var filter in target.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                var localBounds = filter.sharedMesh.bounds;
                Matrix4x4 matrix = relativeTo.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 offset = Vector3.Scale(localBounds.extents,
                        new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                    Vector3 point = matrix.MultiplyPoint3x4(localBounds.center + offset);
                    if (found) bounds.Encapsulate(point);
                    else { bounds = new Bounds(point, Vector3.zero); found = true; }
                }
            }
            return found;
        }

        static void RequireChild(GameObject model, string name, List<string> errors)
        {
            if (FindDeep(model.transform, name) == null) errors.Add("Missing named hierarchy node " + name);
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
            return null;
        }

        static bool Approximately(Vector3 a, Vector3 b, float tolerance)
        {
            return Mathf.Abs(a.x - b.x) <= tolerance && Mathf.Abs(a.y - b.y) <= tolerance && Mathf.Abs(a.z - b.z) <= tolerance;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
