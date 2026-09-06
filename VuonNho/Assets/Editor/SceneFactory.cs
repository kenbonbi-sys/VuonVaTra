using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VuonNho.Core;
using VuonNho.Views;

namespace VuonNho.EditorTools
{
    /// <summary>
    /// Dung scene theo hop dong prefab trong tai lieu asset. Lay hinh anh tu GardenSkin;
    /// cho nao con trong thi rot ve primitive cua moc A, nen thay art duoc tung phan mot.
    /// Chay bang menu, khong chay moi lan Play.
    /// </summary>
    public static class SceneFactory
    {
        public const string ScenePath = "Assets/Scenes/Garden.unity";
        public const string MaterialFolder = "Assets/Settings/Materials";
        public const string SkinPath = "Assets/Settings/GardenSkin.asset";
        public const string MarkerMaterialPath = MaterialFolder + "/M_ClickMarker.mat";

        const int Columns = 4;
        const int Rows = 3;

        // Model duoc dung theo quy uoc "mat truoc la +Z cua wrapper" (muc 3 tai lieu asset),
        // ma camera lai nhin ve phia +Z, nen dat vao scene phai xoay cho mat truoc quay ra nguoi choi.
        // Day la quyet dinh dat canh, khong phai bu truc tren tung prefab.
        const float HelperFacingYaw = 135f;    // robot nhin thang vao nguoi choi
        const float StationFacingYaw = 180f;   // quay tra mo ve phia luong cay

        [MenuItem("Vườn Nhỏ/0. Tạo hoặc mở GardenSkin")]
        public static void OpenSkin()
        {
            var skin = LoadOrCreateSkin();
            Selection.activeObject = skin;
            EditorGUIUtility.PingObject(skin);
        }

        [MenuItem("Vườn Nhỏ/2. Dựng lại scene Garden")]
        public static void BuildSceneMenu()
        {
            BuildScene();
            EditorSceneManager.OpenScene(ScenePath);
        }

        /// <summary>Goi duoc tu dong lenh: -executeMethod VuonNho.EditorTools.SceneFactory.BuildScene</summary>
        public static void BuildScene()
        {
            ProjectSetup.Configure();
            var skin = LoadOrCreateSkin();
            var catalog = DefaultContent.Create();
            WarnIfLayoutDiffers(skin, catalog);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = BuildCamera(skin);
            BuildLighting(skin);
            BuildGround(skin);

            var plots = BuildPlots(skin, catalog);
            var machine = BuildTeaStation(skin);
            var helper = BuildHelper(skin);
            var character = BuildCharacter(skin, catalog);
            var marker = BuildClickMarker();
            var preview = BuildPlacementPreview(skin);
            BuildProps(skin);

            var decorations = BuildDecorationLayer(skin);
            var hud = BuildHud();

            var gameObjectRoot = new GameObject("Game");
            var bootstrap = gameObjectRoot.AddComponent<GameBootstrap>();
            bootstrap.Skin = skin;
            bootstrap.GameCamera = camera;
            bootstrap.Hud = hud;
            bootstrap.Machine = machine;
            bootstrap.Helper = helper;
            bootstrap.Character = character;
            bootstrap.Marker = marker;
            bootstrap.Preview = preview;
            bootstrap.Rig = camera.GetComponent<CameraRig>();
            bootstrap.Plots = plots.ToArray();
            bootstrap.Decorations = decorations;

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VuonNho] Da dung scene: " + ScenePath + " — " + DescribeSkin(skin, catalog));
        }

        public static GardenSkin LoadOrCreateSkin()
        {
            var skin = AssetDatabase.LoadAssetAtPath<GardenSkin>(SkinPath);
            if (skin != null)
            {
                SyncCropSlots(skin);
                SyncFonts(skin);
                return skin;
            }

            if (!Directory.Exists(ProjectSetup.SettingsFolder))
                Directory.CreateDirectory(ProjectSetup.SettingsFolder);

            skin = ScriptableObject.CreateInstance<GardenSkin>();
            var catalog = DefaultContent.Create();
            var entries = new List<CropSkinEntry>();
            foreach (var crop in catalog.Crops)
                entries.Add(new CropSkinEntry { CropId = crop.Id, MaturePrefab = null });
            skin.Crops = entries.ToArray();

            AssetDatabase.CreateAsset(skin, SkinPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[VuonNho] Da tao " + SkinPath + ". Tha prefab vao day roi dung lai scene.");
            return skin;
        }

        /// <summary>
        /// Them cay moi cua catalog vao skin duoi dang o trong. Khong dong vao o da co prefab,
        /// nen doi noi dung khong lam mat art da gan.
        /// </summary>
        static void SyncCropSlots(GardenSkin skin)
        {
            var catalog = DefaultContent.Create();
            var entries = new List<CropSkinEntry>(skin.Crops ?? new CropSkinEntry[0]);
            bool changed = false;

            foreach (var crop in catalog.Crops)
            {
                bool found = false;
                for (int i = 0; i < entries.Count; i++)
                    if (entries[i] != null && entries[i].CropId == crop.Id) found = true;
                if (found) continue;
                entries.Add(new CropSkinEntry { CropId = crop.Id, MaturePrefab = null });
                changed = true;
            }

            if (!changed) return;
            skin.Crops = entries.ToArray();
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            Debug.Log("[VuonNho] Da them o trong trong GardenSkin cho cay moi cua catalog.");
        }

        public const string FontFolder = "Assets/Art/Fonts";

        /// <summary>
        /// Dien font vao o con trong. Khong dong vao o da co, nen doi font bang tay van giu nguyen.
        /// </summary>
        static void SyncFonts(GardenSkin skin)
        {
            bool changed = false;
            if (skin.BodyFont == null)
            {
                skin.BodyFont = AssetDatabase.LoadAssetAtPath<Font>(FontFolder + "/NationalPark-Medium.ttf");
                changed |= skin.BodyFont != null;
            }
            if (skin.SymbolFont == null)
            {
                skin.SymbolFont = AssetDatabase.LoadAssetAtPath<Font>(
                    FontFolder + "/MaterialSymbolsRounded.ttf");
                changed |= skin.SymbolFont != null;
            }
            if (skin.DisplayFont == null)
            {
                skin.DisplayFont = AssetDatabase.LoadAssetAtPath<Font>(FontFolder + "/NationalPark-ExtraBold.ttf");
                changed |= skin.DisplayFont != null;
            }
            if (!changed) return;

            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            Debug.Log("[VuonNho] Da gan font National Park vao GardenSkin.");
        }

        static string DescribeSkin(GardenSkin skin, ContentCatalog catalog)
        {
            int filled = 0;
            int total = 4 + catalog.Crops.Count;
            if (skin.SoilPrefab != null) filled++;
            if (skin.SeedlingPrefab != null) filled++;
            if (skin.StationPrefab != null) filled++;
            if (skin.RobotPrefab != null) filled++;
            foreach (var crop in catalog.Crops)
                if (skin.MaturePrefabFor(crop.Id) != null) filled++;
            return "skin " + filled + "/" + total + " cho da co model, con lai dung primitive.";
        }

        // ---------------------------------------------------------------- camera va anh sang

        static Camera BuildCamera(GardenSkin skin)
        {
            var go = new GameObject("MainCamera");
            go.tag = "MainCamera";
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = skin.CameraOrthographicSize;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = skin.SkyColor;

            // Quy chuan muc 3 tai lieu asset: orthographic, yaw 45 do, pitch khoang 35 do.
            go.transform.rotation = Quaternion.Euler(35f, -45f, 0f);
            var target = new Vector3(0f, 0f, 0.5f * skin.PlotSpacing);
            go.transform.position = target - go.transform.forward * 24f;

            go.AddComponent<AudioListener>();

            // Lan chuot de phong to thu nho, giu chuot trai de keo man hinh.
            var rig = go.AddComponent<CameraRig>();
            rig.Camera = camera;
            rig.MinSize = skin.CameraOrthographicSize * 0.55f;
            rig.MaxSize = skin.CameraOrthographicSize * 1.85f;
            return camera;
        }

        static void BuildLighting(GardenSkin skin)
        {
            var go = new GameObject("KeyLight");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.97f, 0.90f);
            light.intensity = skin.KeyLightIntensity;
            light.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(48f, -30f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = skin.AmbientColor;
        }

        static void BuildGround(GardenSkin skin)
        {
            var root = new GameObject("Ground");
            root.transform.position = new Vector3(0f, 0f, 0.5f * skin.PlotSpacing);

            // Collider gameplay de biet nguoi choi bam vao cho trong nao khi dang dat trang tri.
            // Mat tren nam dung o cao do 0 nen diem cham chinh la mat dat.
            root.AddComponent<GardenGround>();
            var groundCollider = root.AddComponent<BoxCollider>();
            groundCollider.center = new Vector3(0f, -0.25f, 0f);
            groundCollider.size = new Vector3(skin.GroundSize, 0.5f, skin.GroundSize);

            if (skin.GroundPrefab != null)
            {
                SpawnArt(skin.GroundPrefab, root.transform, "GroundVisual", Vector3.zero);
                return;
            }

            Primitive(PrimitiveType.Cube, root.transform, "GroundVisual",
                      new Vector3(0f, -0.25f, 0f),
                      new Vector3(skin.GroundSize, 0.5f, skin.GroundSize), "Grass");
        }

        // ---------------------------------------------------------------- o dat

        static List<PlotView> BuildPlots(GardenSkin skin, ContentCatalog catalog)
        {
            var root = new GameObject("Plots");
            var views = new List<PlotView>();

            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    int plotId = row * Columns + column;
                    views.Add(BuildPlot(root.transform, plotId, PlotPosition(skin, column, row),
                                        skin, catalog));
                }
            }
            return views;
        }

        static PlotView BuildPlot(Transform parent, int plotId, Vector3 position,
                                  GardenSkin skin, ContentCatalog catalog)
        {
            var plotRoot = new GameObject("PlotRoot_" + plotId);
            plotRoot.transform.SetParent(parent, false);
            plotRoot.transform.localPosition = position;
            plotRoot.transform.localScale = Vector3.one;   // root giu scale 1

            float colliderSide = skin.PlotSize + 0.1f;
            var collider = plotRoot.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.25f, 0f);
            collider.size = new Vector3(colliderSide, 0.7f, colliderSide);

            var view = plotRoot.AddComponent<PlotView>();
            view.PlotId = plotId;

            var visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(plotRoot.transform, false);
            view.VisualRoot = visualRoot.transform;

            // --- mat dat
            if (skin.SoilPrefab != null)
            {
                var soil = SpawnArt(skin.SoilPrefab, visualRoot.transform, "SoilMesh", Vector3.zero);
                if (skin.LockedOverlayPrefab != null)
                {
                    view.LockedOverlay = SpawnArt(skin.LockedOverlayPrefab, visualRoot.transform,
                                                  "LockedOverlay", Vector3.zero);
                }
                else if (soil != null)
                {
                    // Khong co hinh rieng cho o khoa thi van phai phan biet duoc bang mau.
                    view.SoilRenderer = soil.GetComponentInChildren<Renderer>();
                }
            }
            else
            {
                var soil = Primitive(PrimitiveType.Cube, visualRoot.transform, "SoilMesh",
                                     new Vector3(0f, 0.06f, 0f),
                                     new Vector3(skin.PlotSize, 0.22f, skin.PlotSize), "Soil");
                view.SoilRenderer = soil.GetComponent<Renderer>();
            }

            var cropAnchor = new GameObject("CropAnchor");
            cropAnchor.transform.SetParent(visualRoot.transform, false);
            cropAnchor.transform.localPosition = new Vector3(0f, 0.17f, 0f);
            view.CropAnchor = cropAnchor.transform;

            // --- mam chung cho ca ba loai cay
            if (skin.SeedlingPrefab != null)
            {
                view.SeedlingVisual = SpawnArt(skin.SeedlingPrefab, cropAnchor.transform,
                                               "SeedlingVisual", Vector3.zero);
            }
            else
            {
                var seedling = Primitive(PrimitiveType.Cube, cropAnchor.transform, "SeedlingVisual",
                                         new Vector3(0f, 0.12f, 0f),
                                         new Vector3(0.16f, 0.24f, 0.16f), "CropMint");
                view.SeedlingVisual = seedling;
                view.SeedlingRenderer = seedling.GetComponent<Renderer>();
            }

            // --- mot model truong thanh cho moi loai cay
            var cropVisuals = new List<CropVisual>();
            foreach (var crop in catalog.Crops)
                cropVisuals.Add(BuildCropVisual(cropAnchor.transform, crop, skin));
            view.CropVisuals = cropVisuals.ToArray();

            // --- dau hieu chin
            var badgeAnchor = new GameObject("ReadyBadgeAnchor");
            badgeAnchor.transform.SetParent(plotRoot.transform, false);
            badgeAnchor.transform.localPosition = new Vector3(0f, 1.15f, 0f);

            if (skin.ReadyBadgePrefab != null)
            {
                view.ReadyBadge = SpawnArt(skin.ReadyBadgePrefab, badgeAnchor.transform,
                                           "ReadyBadge", Vector3.zero);
            }
            else
            {
                var badge = Primitive(PrimitiveType.Cube, badgeAnchor.transform, "ReadyBadge",
                                      Vector3.zero, new Vector3(0.2f, 0.2f, 0.2f), "Accent");
                badge.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
                view.ReadyBadge = badge;
                view.ReadyBadgeRenderer = badge.GetComponent<Renderer>();
            }

            SetActive(view.SeedlingVisual, false);
            SetActive(view.ReadyBadge, false);
            var harvest = new GameObject("HarvestFeedback");
            harvest.transform.SetParent(plotRoot.transform, false);
            for (int i = 0; i < 3; i++)
                Primitive(PrimitiveType.Sphere, harvest.transform, "Leaf_" + i,
                    Vector3.zero, new Vector3(0.10f, 0.05f, 0.16f), "Cream");
            view.HarvestFeedback = harvest.transform;
            harvest.SetActive(false);
            return view;
        }

        static CropVisual BuildCropVisual(Transform cropAnchor, CropDefinition crop, GardenSkin skin)
        {
            var visual = new CropVisual { CropId = crop.Id };
            var maturePrefab = skin.MaturePrefabFor(crop.Id);

            if (maturePrefab != null)
            {
                visual.Root = SpawnArt(maturePrefab, cropAnchor, "Mature_" + crop.Id, Vector3.zero);
                var accents = FindDeep(visual.Root.transform, "ReadyAccents");
                if (accents != null) visual.ReadyAccents = accents.gameObject;
            }
            else
            {
                var container = new GameObject("Mature_" + crop.Id);
                container.transform.SetParent(cropAnchor, false);

                Primitive(PrimitiveType.Cube, container.transform, "Stem",
                          new Vector3(0f, 0.22f, 0f), new Vector3(0.1f, 0.45f, 0.1f), "Wood");
                var foliage = Primitive(PrimitiveType.Sphere, container.transform, "FoliageRoot",
                                        new Vector3(0f, 0.55f, 0f),
                                        new Vector3(0.58f, 0.5f, 0.58f), MaterialKeyFor(crop.Id));

                visual.Root = container;
                visual.FoliageRenderer = foliage.GetComponent<Renderer>();
            }

            SetActive(visual.Root, false);
            return visual;
        }

        static string MaterialKeyFor(string cropId)
        {
            switch (cropId)
            {
                case DefaultContent.CropChamomile: return "CropChamomile";
                case DefaultContent.CropStrawberry: return "CropStrawberry";
                default: return "CropMint";
            }
        }

        // ---------------------------------------------------------------- quan tra va robot

        static MachineView BuildTeaStation(GardenSkin skin)
        {
            var root = new GameObject("TeaStationRoot");
            root.transform.position = StationPosition(skin);

            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.6f, 0f);
            collider.size = new Vector3(3.2f, 1.4f, 1.6f);
            root.AddComponent<WalkBlocker>();

            var view = root.AddComponent<MachineView>();

            if (skin.StationPrefab != null)
            {
                var art = SpawnArt(skin.StationPrefab, root.transform, "VisualRoot", Vector3.zero);
                art.transform.localRotation = Quaternion.Euler(0f, StationFacingYaw, 0f);
                view.VisualRoot = art.transform;

                var status = FindDeep(art.transform, "StatusAnchor");
                if (status != null) view.StatusRenderer = status.GetComponentInChildren<Renderer>();

                var steam = FindDeep(art.transform, "SteamAnchor");
                if (steam != null)
                {
                    view.SteamAnchor = steam;
                    steam.gameObject.SetActive(false);
                }
                return view;
            }

            var visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform, false);
            view.VisualRoot = visualRoot.transform;

            var station = Primitive(PrimitiveType.Cube, visualRoot.transform, "StationMesh",
                                    new Vector3(0f, 0.45f, 0f), new Vector3(3f, 0.9f, 1.4f), "Wood");
            view.StationRenderer = station.GetComponent<Renderer>();

            Primitive(PrimitiveType.Cube, visualRoot.transform, "Roof",
                      new Vector3(0f, 2.05f, 0f), new Vector3(3.4f, 0.16f, 1.8f), "Cream");
            Primitive(PrimitiveType.Cube, visualRoot.transform, "PostLeft",
                      new Vector3(-1.5f, 1.45f, -0.7f), new Vector3(0.12f, 1.1f, 0.12f), "Wood");
            Primitive(PrimitiveType.Cube, visualRoot.transform, "PostRight",
                      new Vector3(1.5f, 1.45f, -0.7f), new Vector3(0.12f, 1.1f, 0.12f), "Wood");
            Primitive(PrimitiveType.Cylinder, visualRoot.transform, "Brewer",
                      new Vector3(0.85f, 1.05f, 0f), new Vector3(0.5f, 0.25f, 0.5f), "Robot");

            var lamp = Primitive(PrimitiveType.Sphere, visualRoot.transform, "StatusLamp",
                                 new Vector3(-1.15f, 1.05f, 0f), new Vector3(0.22f, 0.22f, 0.22f), "Accent");
            view.StatusRenderer = lamp.GetComponent<Renderer>();

            var steamAnchor = new GameObject("SteamAnchor");
            steamAnchor.transform.SetParent(root.transform, false);
            steamAnchor.transform.localPosition = new Vector3(0.85f, 1.35f, 0f);
            Primitive(PrimitiveType.Sphere, steamAnchor.transform, "Steam", Vector3.zero, Vector3.one, "Cream");
            view.SteamAnchor = steamAnchor.transform;
            steamAnchor.SetActive(false);

            return view;
        }

        /// <summary>
        /// Nhan vat chinh dung san trong vuon. Khong gan collider: nguoi choi bam xuyen qua
        /// nhan vat de cham vao o dat phia sau, khong bao gio bi chinh nhan vat che mat thao tac.
        /// </summary>
        static CharacterView BuildCharacter(GardenSkin skin, ContentCatalog catalog)
        {
            var root = new GameObject("CharacterRoot");
            root.transform.position = CharacterStartPosition(skin);
            var view = root.AddComponent<CharacterView>();
            // Cung khu dat ma Core dung de chan dat trang tri, lui vao mot chut cho khoi cham hang rao.
            view.WalkLimit = catalog.Balance.GardenHalfExtentMm / 1000f - 0.7f;

            var art = skin.CharacterPrefab != null
                ? SpawnArt(skin.CharacterPrefab, root.transform, "VisualRoot", Vector3.zero)
                : new GameObject("VisualRoot");
            if (skin.CharacterPrefab == null)
            {
                art.transform.SetParent(root.transform, false);
                Primitive(PrimitiveType.Capsule, art.transform, "Body",
                          new Vector3(0f, 0.7f, 0f), new Vector3(0.5f, 0.7f, 0.5f), "Accent");
            }
            art.transform.localRotation = Quaternion.Euler(0f, HelperFacingYaw, 0f);
            view.VisualRoot = art.transform;

            var legLeft = FindDeep(art.transform, "LegLeft");
            if (legLeft != null) view.LegLeft = legLeft;
            var legRight = FindDeep(art.transform, "LegRight");
            if (legRight != null) view.LegRight = legRight;
            return view;
        }

        /// <summary>Vong tron bao lai cu bam chuot phai. Mesh dung trong code nen khong can prefab.</summary>
        static ClickMarker BuildClickMarker()
        {
            var go = new GameObject("ClickMarker");
            var marker = go.AddComponent<ClickMarker>();
            marker.RingMaterial = LoadOrCreateMarkerMaterial();
            return marker;
        }

        /// <summary>
        /// Vat lieu cua vanh. Dung UI/Default vi no cho phep dat ZTest bang unity_GUIZTestMode —
        /// <see cref="ClickMarker"/> dat gia tri do luc chay, xem ly do o day.
        ///
        /// Ton tai thanh asset chu khong chi la Shader.Find luc chay: mot shader khong duoc asset
        /// nao tham chieu co the bi loai khoi ban build, va loi do chi lo ra trong build chu
        /// khong trong editor.
        /// </summary>
        static Material LoadOrCreateMarkerMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MarkerMaterialPath);
            if (existing != null) return existing;

            var shader = Shader.Find("UI/Default");
            if (shader == null) return null;

            var material = new Material(shader) { name = "M_ClickMarker" };
            Directory.CreateDirectory(Path.GetDirectoryName(MarkerMaterialPath));
            AssetDatabase.CreateAsset(material, MarkerMaterialPath);
            return material;
        }

        /// <summary>Bong ma cua mon trang tri dang cam. Dung prefab tu skin nen khong can prefab rieng.</summary>
        static PlacementPreview BuildPlacementPreview(GardenSkin skin)
        {
            var go = new GameObject("PlacementPreview");
            var preview = go.AddComponent<PlacementPreview>();
            preview.Skin = skin;
            return preview;
        }

        static HelperView BuildHelper(GardenSkin skin)
        {
            var root = new GameObject("HelperRoot");
            root.transform.position = HelperPosition(skin);

            var view = root.AddComponent<HelperView>();

            if (skin.RobotPrefab != null)
            {
                var art = SpawnArt(skin.RobotPrefab, root.transform, "VisualRoot",
                                   new Vector3(0f, 0.95f, 0f));
                art.transform.localRotation = Quaternion.Euler(0f, HelperFacingYaw, 0f);
                view.VisualRoot = art.transform;

                var face = FindDeep(art.transform, "Face");
                if (face != null) view.FaceRenderer = face.GetComponentInChildren<Renderer>();

                var hint = FindDeep(art.transform, "SleepingHint");
                if (hint != null) view.SleepingHint = hint.gameObject;
                return view;
            }

            var visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform, false);
            visualRoot.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            view.VisualRoot = visualRoot.transform;

            var body = Primitive(PrimitiveType.Cube, visualRoot.transform, "Body",
                                 Vector3.zero, new Vector3(0.7f, 0.8f, 0.6f), "Robot");
            view.BodyRenderer = body.GetComponent<Renderer>();

            var face2 = Primitive(PrimitiveType.Cube, visualRoot.transform, "Face",
                                  new Vector3(0f, 0.12f, -0.32f), new Vector3(0.44f, 0.26f, 0.05f), "Accent");
            view.FaceRenderer = face2.GetComponent<Renderer>();

            Primitive(PrimitiveType.Cube, visualRoot.transform, "Antenna",
                      new Vector3(0f, 0.55f, 0f), new Vector3(0.06f, 0.32f, 0.06f), "Robot");
            Primitive(PrimitiveType.Sphere, visualRoot.transform, "AntennaTip",
                      new Vector3(0f, 0.74f, 0f), new Vector3(0.14f, 0.14f, 0.14f), "Accent");

            view.SleepingHint = Primitive(PrimitiveType.Cube, root.transform, "SleepingHint",
                                          new Vector3(0f, 1.75f, 0f),
                                          new Vector3(0.16f, 0.16f, 0.16f), "Cream");
            return view;
        }

        // ---------------------------------------------------------------- props

        static void BuildProps(GardenSkin skin)
        {
            var root = new GameObject("Props");
            float halfWidth = (Columns - 1) * 0.5f * skin.PlotSpacing;
            float halfDepth = (Rows - 1) * 0.5f * skin.PlotSpacing;

            // Cay nen va bui o mep de khong che muc tieu click.
            CreateTree(root.transform, skin, 0, new Vector3(-halfWidth - 4.0f, 0f, halfDepth + 3.6f), 1.15f);
            CreateTree(root.transform, skin, 1, new Vector3(halfWidth + 4.2f, 0f, halfDepth + 3.0f), 0.95f);
            CreateTree(root.transform, skin, 2, new Vector3(-halfWidth - 4.6f, 0f, -halfDepth - 1.8f), 0.85f);

            CreateBush(root.transform, skin, 0, new Vector3(halfWidth + 2.5f, 0f, -halfDepth - 2.0f), 0.9f);
            CreateBush(root.transform, skin, 1, new Vector3(-halfWidth - 2.8f, 0f, -halfDepth - 2.8f), 0.7f);
            CreateBush(root.transform, skin, 2, new Vector3(halfWidth + 3.2f, 0f, -0.2f), 0.8f);

            CreateRock(root.transform, skin, 0, new Vector3(halfWidth + 1.0f, 0f, -halfDepth - 2.8f), 0.55f);
            CreateRock(root.transform, skin, 1, new Vector3(-halfWidth - 1.2f, 0f, halfDepth + 3.4f), 0.42f);

            // Hang rao chay doc canh truoc cua cum luong.
            float fenceZ = -halfDepth - 3.2f;
            for (int i = 0; i < 9; i++)
                CreateFencePost(root.transform, skin, new Vector3(-halfWidth - 3.6f + i * 1.5f, 0f, fenceZ));
        }

        static void CreateTree(Transform parent, GardenSkin skin, int index, Vector3 position, float scale)
        {
            var prefab = GardenSkin.Pick(skin.TreePrefabs, index);
            if (prefab != null)
            {
                var art = SpawnArt(prefab, parent, "Tree", position);
                art.transform.localScale = Vector3.one * scale;
                art.transform.localRotation = Quaternion.Euler(0f, index * 73f, 0f);
                BlockTrunk(art);
                return;
            }

            var tree = new GameObject("Tree");
            tree.transform.SetParent(parent, false);
            tree.transform.localPosition = position;
            tree.transform.localScale = Vector3.one * scale;
            tree.transform.localRotation = Quaternion.Euler(0f, index * 73f, 0f);
            BlockTrunk(tree);

            Primitive(PrimitiveType.Cylinder, tree.transform, "Trunk",
                      new Vector3(0f, 0.9f, 0f), new Vector3(0.28f, 0.9f, 0.28f), "Wood");
            Primitive(PrimitiveType.Sphere, tree.transform, "Canopy",
                      new Vector3(0f, 2.1f, 0f), new Vector3(1.9f, 1.6f, 1.9f), "Grass");
        }

        static void CreateBush(Transform parent, GardenSkin skin, int index, Vector3 position, float scale)
        {
            var prefab = GardenSkin.Pick(skin.BushPrefabs, index);
            if (prefab != null)
            {
                var art = SpawnArt(prefab, parent, "Bush", position);
                art.transform.localScale = Vector3.one * scale;
                art.transform.localRotation = Quaternion.Euler(0f, index * 97f, 0f);
                return;
            }

            var bush = Primitive(PrimitiveType.Sphere, parent, "Bush",
                                 position + new Vector3(0f, 0.32f * scale, 0f),
                                 new Vector3(0.9f, 0.6f, 0.9f) * scale, "Grass");
            bush.transform.localRotation = Quaternion.Euler(0f, index * 97f, 0f);
        }

        static void CreateRock(Transform parent, GardenSkin skin, int index, Vector3 position, float scale)
        {
            var prefab = GardenSkin.Pick(skin.RockPrefabs, index);
            if (prefab != null)
            {
                var art = SpawnArt(prefab, parent, "Rock", position);
                art.transform.localScale = Vector3.one * scale;
                return;
            }

            var rock = Primitive(PrimitiveType.Cube, parent, "Rock",
                                 position + new Vector3(0f, 0.18f * scale, 0f),
                                 new Vector3(0.7f, 0.45f, 0.6f) * scale, "Cream");
            rock.transform.localRotation = Quaternion.Euler(8f, index * 61f, 6f);
        }

        static void CreateFencePost(Transform parent, GardenSkin skin, Vector3 position)
        {
            if (skin.FencePostPrefab != null)
            {
                BlockBounds(SpawnArt(skin.FencePostPrefab, parent, "FencePost", position));
                return;
            }

            Primitive(PrimitiveType.Cube, parent, "FencePost",
                      position + new Vector3(0f, 0.42f, 0f), new Vector3(0.14f, 0.84f, 0.14f), "Wood");
            Primitive(PrimitiveType.Cube, parent, "FenceRail",
                      position + new Vector3(0.75f, 0.6f, 0f), new Vector3(1.5f, 0.1f, 0.08f), "Wood");
            BlockBox(parent, "FenceBlock", position + new Vector3(0.75f, 0.5f, 0f),
                     new Vector3(1.6f, 1f, 0.24f));
        }

        /// <summary>
        /// Chan dung phan than cay, khong chan tan la. Tan la vuon ra hon mot met ma di duoi tan
        /// cay thi phai duoc — chan ca tan la se thanh mot buc tuong tron vo hinh giua bai co.
        /// </summary>
        static void BlockTrunk(GameObject tree)
        {
            var collider = tree.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1f, 0f);
            collider.size = new Vector3(0.55f, 2f, 0.55f);
            tree.AddComponent<WalkBlocker>();
        }

        /// <summary>
        /// Chan dung khoi ma model chiem cho. Do tu renderer chu khong dat cung so: doi sang
        /// model khac kich thuoc thi vung chan doi theo, khong de lai mot buc tuong lech cho.
        /// </summary>
        static void BlockBounds(GameObject art)
        {
            Bounds bounds;
            if (!GardenArtImporter.TryGetBounds(art.transform, art.transform, out bounds)) return;
            var collider = art.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;
            art.AddComponent<WalkBlocker>();
        }

        /// <summary>Khoi chan roi, khong gan vao mesh nao — dung khi mon do la nhieu primitive rieng.</summary>
        static void BlockBox(Transform parent, string name, Vector3 centre, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = centre;
            var collider = go.AddComponent<BoxCollider>();
            collider.size = size;
            go.AddComponent<WalkBlocker>();
        }

        static DecorationLayer BuildDecorationLayer(GardenSkin skin)
        {
            var root = new GameObject("Decorations");
            var layer = root.AddComponent<DecorationLayer>();
            layer.Skin = skin;
            return layer;
        }

        // ---------------------------------------------------------------- UI

        static GameHud BuildHud()
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();

            var canvasGo = new GameObject("HudCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1366f, 768f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            return canvasGo.AddComponent<GameHud>();
        }

        // ---------------------------------------------------------------- bo cuc vuon

        /// <summary>Vi tri tam mot o dat trong scene. Cot can giua, hang dem tu hang giua.</summary>
        static Vector3 PlotPosition(GardenSkin skin, int column, int row)
        {
            return new Vector3((column - (Columns - 1) * 0.5f) * skin.PlotSpacing,
                               0f,
                               (row - 1) * skin.PlotSpacing);
        }

        /// <summary>Quay tra dung sau luong cay, can giua theo truc X.</summary>
        static Vector3 StationPosition(GardenSkin skin)
        {
            return new Vector3(0f, 0f, (Rows - 1) * 0.5f * skin.PlotSpacing + 2.6f);
        }

        /// <summary>Nhan vat dung phia truoc luong cay, khong dam vao o dat nao.</summary>
        static Vector3 CharacterStartPosition(GardenSkin skin)
        {
            return new Vector3(skin.PlotSpacing, 0f, -((Rows - 1) * 0.5f * skin.PlotSpacing + 1.6f));
        }

        /// <summary>Robot dung ben trai luong cay.</summary>
        static Vector3 HelperPosition(GardenSkin skin)
        {
            return new Vector3(-((Columns - 1) * 0.5f * skin.PlotSpacing + 2.0f),
                               0f, 0.25f * skin.PlotSpacing);
        }

        /// <summary>
        /// Doi chieu so cua GardenSkin voi bo cuc ma Core dung de chan dat trang tri.
        /// Hai nguon su that ma im lang la cai bay, nen lech cho nao thi noi ro lech bao nhieu.
        /// </summary>
        static void WarnIfLayoutDiffers(GardenSkin skin, ContentCatalog catalog)
        {
            var balance = catalog.Balance;
            var differences = new List<string>();

            AddDifference(differences, "PlotSpacing",
                          Mathf.RoundToInt(skin.PlotSpacing * 1000f), balance.PlotSpacingMm);
            AddDifference(differences, "PlotSize",
                          Mathf.RoundToInt(skin.PlotSize * 1000f), balance.PlotEdgeMm);
            AddDifference(differences, "so cot", Columns, balance.GardenColumns);
            AddDifference(differences, "so hang", Rows, balance.GardenRows);

            // So ca o dau lan o cuoi: cong thuc hang o day dem tu hang giua chu khong can giua
            // nhu cot, hai cach chi trung nhau khi Rows = 3.
            if (Columns == balance.GardenColumns && Rows == balance.GardenRows)
            {
                var first = PlotPosition(skin, 0, 0);
                var last = PlotPosition(skin, Columns - 1, Rows - 1);
                AddDifference(differences, "o dau X",
                              Mathf.RoundToInt(first.x * 1000f), GardenLayout.PlotCenterXMm(balance, 0));
                AddDifference(differences, "o dau Z",
                              Mathf.RoundToInt(first.z * 1000f), GardenLayout.PlotCenterZMm(balance, 0));
                AddDifference(differences, "o cuoi X",
                              Mathf.RoundToInt(last.x * 1000f),
                              GardenLayout.PlotCenterXMm(balance, Columns - 1));
                AddDifference(differences, "o cuoi Z",
                              Mathf.RoundToInt(last.z * 1000f),
                              GardenLayout.PlotCenterZMm(balance, Rows - 1));
            }

            var station = StationPosition(skin);
            AddDifference(differences, "quay tra X",
                          Mathf.RoundToInt(station.x * 1000f), balance.StationCenterXMm);
            AddDifference(differences, "quay tra Z",
                          Mathf.RoundToInt(station.z * 1000f), balance.StationCenterZMm);

            var helper = HelperPosition(skin);
            AddDifference(differences, "robot X",
                          Mathf.RoundToInt(helper.x * 1000f), balance.RobotCenterXMm);
            AddDifference(differences, "robot Z",
                          Mathf.RoundToInt(helper.z * 1000f), balance.RobotCenterZMm);

            if (differences.Count == 0) return;

            Debug.LogWarning("[VuonNho] GardenSkin lech voi BalanceConfig: " +
                             string.Join("; ", differences.ToArray()) +
                             ". Core dang chan dat trang tri theo so cua BalanceConfig " +
                             "nen luat se khong khop scene.");
        }

        static void AddDifference(List<string> differences, string label, int sceneValue, int balanceValue)
        {
            if (sceneValue == balanceValue) return;
            differences.Add(label + " scene " + sceneValue + " vs balance " + balanceValue +
                            " (lech " + (sceneValue - balanceValue) + ")");
        }

        // ---------------------------------------------------------------- tien ich

        /// <summary>
        /// Dat prefab art vao scene va giu lien ket prefab, de sua prefab la scene cap nhat theo.
        /// Collider cua art bi tat de raycast chi trung collider gameplay tren wrapper.
        /// </summary>
        static GameObject SpawnArt(GameObject prefab, Transform parent, string name, Vector3 localPosition)
        {
            var instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null) instance = Object.Instantiate(prefab, parent);

            instance.name = name;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.identity;

            var colliders = instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;

            return instance;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        static void SetActive(GameObject target, bool active)
        {
            if (target != null) target.SetActive(active);
        }

        static GameObject Primitive(PrimitiveType type, Transform parent, string name,
                                    Vector3 localPosition, Vector3 localScale, string materialKey)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            var collider = go.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = Material(materialKey);
            return go;
        }

        static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();

        static Material Material(string key)
        {
            Material cached;
            if (MaterialCache.TryGetValue(key, out cached) && cached != null) return cached;

            if (!Directory.Exists(MaterialFolder)) Directory.CreateDirectory(MaterialFolder);
            string path = MaterialFolder + "/Mat_" + key + ".mat";

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                // Nhan ban tu material Lit mac dinh de co du keyword va gia tri mac dinh cua pipeline;
                // tao bang new Material(shader) se thieu cac property nay va render sai mau.
                var template = ProjectSetup.LitTemplate();
                material = template != null
                    ? new Material(template)
                    : new Material(ProjectSetup.LitShader());
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = ColorFor(key);
            EditorUtility.SetDirty(material);
            MaterialCache[key] = material;
            return material;
        }

        /// <summary>Chin material mau phang dung chung cho ca bo asset.</summary>
        static Color ColorFor(string key)
        {
            switch (key)
            {
                case "Grass": return GardenPalette.Grass;
                case "Soil": return GardenPalette.Soil;
                case "Wood": return GardenPalette.Wood;
                case "Cream": return GardenPalette.Cream;
                case "Robot": return GardenPalette.Robot;
                case "Accent": return GardenPalette.Coin;
                case "CropMint": return GardenPalette.CropBody(DefaultContent.CropMint);
                case "CropChamomile": return GardenPalette.CropBody(DefaultContent.CropChamomile);
                case "CropStrawberry": return GardenPalette.CropBody(DefaultContent.CropStrawberry);
                default: return Color.magenta;
            }
        }
    }
}
