using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VuonNho.Core;
using VuonNho.Views;

namespace VuonNho.EditorTools
{
    /// <summary>Permanent site dressing. Machines, crops and player decorations remain separate.</summary>
    public static class FarmFactoryScenery
    {
        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        static readonly Color Paving = new Color(0.75f, 0.72f, 0.60f);
        static readonly Color Edge = new Color(0.57f, 0.57f, 0.46f);
        static readonly Color Cream = new Color(0.94f, 0.90f, 0.77f);
        static readonly Color Sage = new Color(0.30f, 0.47f, 0.39f);
        static readonly Color Timber = new Color(0.56f, 0.38f, 0.22f);
        static readonly Color Gold = new Color(0.89f, 0.69f, 0.32f);

        public static void Build(GardenSkin skin, Camera camera)
        {
            var site = new GameObject("FarmAndFactory").transform;
            var floor = new GameObject("FactoryCourtyard").transform;
            floor.SetParent(site, false);
            float minX = ProductionLayout.MinXMm / 1000f;
            float maxX = ProductionLayout.MaxXMm / 1000f;
            float minZ = ProductionLayout.MinZMm / 1000f;
            float maxZ = ProductionLayout.MaxZMm / 1000f;
            float centreX = (minX + maxX) * 0.5f;
            float centreZ = (minZ + maxZ) * 0.5f;
            Cube(floor, "Foundation", new Vector3(centreX, -0.12f, centreZ),
                new Vector3(maxX - minX + 0.25f, 0.24f, maxZ - minZ + 0.25f), "Edge", Edge);
            Cube(floor, "WarmConcrete", new Vector3(centreX, -0.005f, centreZ),
                new Vector3(maxX - minX, 0.025f, maxZ - minZ), "Paving", Paving);

            // Expansion joints and a contrasting pedestrian aisle give the workshop a human scale.
            for (float x = minX + 1.68f; x < maxX; x += 1.68f)
                Cube(floor, "ConcreteJoint", new Vector3(x, 0.009f, centreZ),
                    new Vector3(0.018f, 0.004f, maxZ - minZ), "Joint", Edge * 1.05f);
            foreach (float z in new[] { -2.2f, -0.2f, 2f, 4.1f })
                Cube(floor, "CrossJoint", new Vector3(centreX, 0.011f, z),
                    new Vector3(maxX - minX, 0.004f, 0.018f), "Joint", Edge * 1.05f);
            Cube(floor, "ServiceAisle", new Vector3(centreX, 0.014f, 0.9f),
                new Vector3(maxX - minX - 0.5f, 0.015f, 0.88f), "Aisle", new Color(0.64f, 0.66f, 0.54f));
            for (float x = minX + 0.3f; x < maxX - 0.4f; x += 0.8f)
                Cube(floor, "AisleDash", new Vector3(x, 0.025f, 0.9f),
                    new Vector3(0.36f, 0.007f, 0.045f), "Cream", Cream);

            for (int i = 0; i < ProductionLayout.StationCount; i++)
            {
                float x = ProductionLayout.StationCenterXMm(i) / 1000f;
                float z = ProductionLayout.StationCenterZMm(i) / 1000f;
                // Bay marks stay visible before the machine is purchased.
                foreach (float side in new[] { -1f, 1f })
                {
                    Cube(floor, "BayCorner", new Vector3(x + side * 1.39f, 0.026f, z + 0.65f),
                        new Vector3(0.05f, 0.012f, 0.36f), "Cream", Cream);
                    Cube(floor, "BayCorner", new Vector3(x + side * 1.24f, 0.026f, z + 0.8f),
                        new Vector3(0.36f, 0.012f, 0.05f), "Cream", Cream);
                }
            }

            // Farm paths avoid the twelve crop click targets and meet the receiving bay.
            var paths = new GameObject("GardenPaths").transform;
            paths.SetParent(site, false);
            Cube(paths, "FarmFrontWalk", new Vector3(-0.05f, -0.015f, -3.18f),
                new Vector3(7.8f, 0.04f, 0.84f), "Path", new Color(0.79f, 0.75f, 0.60f));
            Cube(paths, "FarmToWorkshop", new Vector3(3.52f, -0.012f, -0.1f),
                new Vector3(0.72f, 0.04f, 6.9f), "Path", new Color(0.79f, 0.75f, 0.60f));
            for (int i = 0; i < 11; i++)
                Cube(paths, "WalkPaver", new Vector3(-3.5f + i * 0.7f, 0.011f, -3.18f),
                    new Vector3(0.66f, 0.017f, 0.7f), "Paver", new Color(0.84f, 0.80f, 0.66f));

            // An open loading shelter reads as a workshop without a roof covering the machines.
            var shelter = new GameObject("DispatchShelter").transform;
            shelter.SetParent(site, false);
            shelter.localPosition = new Vector3(6.1f, 0f, -4.5f);
            Cube(shelter, "Deck", new Vector3(0f, 0.045f, 0f), new Vector3(3.2f, 0.09f, 1.3f), "Timber", Timber);
            for (int i = 0; i < 8; i++)
                Cube(shelter, "DeckPlank", new Vector3(-1.39f + i * 0.397f, 0.101f, 0f),
                    new Vector3(0.382f, 0.022f, 1.26f), i % 3 == 0 ? "TimberLight" : "Timber", i % 3 == 0 ? Timber * 1.08f : Timber);
            foreach (float x in new[] { -1.43f, 1.43f })
            {
                Cube(shelter, "Post", new Vector3(x, 0.84f, 0.45f), new Vector3(0.10f, 1.68f, 0.10f), "Timber", Timber);
                var brace = Cube(shelter, "RoofBrace", new Vector3(x - Mathf.Sign(x) * 0.15f, 1.43f, 0.45f),
                    new Vector3(0.075f, 0.48f, 0.075f), "Timber", Timber);
                brace.transform.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Sign(x) * 42f);
            }
            Cube(shelter, "Roof", new Vector3(0f, 1.68f, 0f), new Vector3(3.48f, 0.12f, 1.7f), "Sage", Sage);
            Cube(shelter, "Fascia", new Vector3(0f, 1.58f, -0.81f), new Vector3(3.45f, 0.17f, 0.055f), "Cream", Cream);
            for (int i = 0; i < 6; i++)
                Cube(shelter, "RoofSeam", new Vector3(-1.45f + i * 0.58f, 1.749f, 0f),
                    new Vector3(0.026f, 0.024f, 1.7f), "SageLight", Sage * 1.12f);
            foreach (float side in new[] { -1f, 1f })
                Cube(shelter, "RoofEdge", new Vector3(side * 1.72f, 1.68f, 0f),
                    new Vector3(0.045f, 0.13f, 1.72f), "SageDark", Sage * 0.85f);
            for (int i = 0; i < 3; i++)
                Pallet(shelter, new Vector3(-0.92f + 0.90f * i, 0.09f, 0f), i != 2);

            Pallet(site, new Vector3(4.4f, 0f, 4.7f), false);
            Pallet(site, new Vector3(3.55f, 0f, 4.7f), false);
            Sign(site, skin, camera, "FarmSign", "NÔNG TRẠI", null, new Vector3(-2.4f, 0f, -3.8f), 2.45f);
            Sign(site, skin, camera, "FactorySign", "XƯỞNG CHẾ BIẾN", null, new Vector3(8.9f, 0f, 5.05f), 3.1f);
            var dispatchSign = new GameObject("DispatchNameplate").transform;
            dispatchSign.SetParent(shelter, false);
            dispatchSign.localPosition = new Vector3(0f, 2.07f, 0f);
            dispatchSign.rotation = camera.transform.rotation;
            Cube(dispatchSign, "Backing", Vector3.zero, new Vector3(2.7f, 0.38f, 0.05f), "Cream", Cream);
            Label(dispatchSign, skin, camera, "DispatchLabel", "KHO THÀNH PHẨM", new Vector3(0f, 0f, -0.035f), 0.033f, Sage);

            // Short perimeter sections frame the site; the front route remains open.
            for (int i = 0; i < 6; i++)
                Prop(skin.FencePostPrefab, site, "WorkshopFence", new Vector3(4.8f + i * 1.5f, 0f, 5.4f));
            foreach (float x in new[] { 3.95f, 13.5f })
            {
                Cube(site, "SafetyBollard", new Vector3(x, 0.26f, -3.03f), new Vector3(0.14f, 0.52f, 0.14f), "Gold", Gold);
                Cube(site, "BollardBand", new Vector3(x, 0.31f, -3.03f), new Vector3(0.146f, 0.1f, 0.146f), "Sage", Sage);
            }
            var bench = Prop(skin.DecorationPrefabFor(DefaultDecorations.Bench), site, "GardenRestBench", new Vector3(-3.1f, 0f, 3.8f));
            if (bench != null) bench.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Prop(skin.DecorationPrefabFor(DefaultDecorations.Planter), site, "GardenPlanter", new Vector3(-4.8f, 0f, 3.3f));
            Prop(skin.DecorationPrefabFor(DefaultDecorations.Lantern), site, "PathLantern", new Vector3(2.95f, 0f, -3.7f));
            BuildWorkshopFixtures(site);
        }

        /// <summary>
        /// Permanent preparation/storage fittings give a new yard a purpose before its machines
        /// are purchased. All upright fittings sit behind z=4.4, clear of the six production bays,
        /// moving conveyors and central service aisle; the front drainage is flush with the floor.
        /// These are scenery meshes only, with no production state or click targets.
        /// </summary>
        static void BuildWorkshopFixtures(Transform site)
        {
            var fixtures = new GameObject("WorkshopFittings").transform;
            fixtures.SetParent(site, false);

            // A modest cloth canopy gives real directional shadows across the preparation bench.
            var preparation = new GameObject("PreparationAwning").transform;
            preparation.SetParent(fixtures, false);
            preparation.localPosition = new Vector3(6.55f, 0f, 4.91f);
            foreach (float x in new[] { -1.06f, 1.06f })
            {
                Cube(preparation, "AwningPost", new Vector3(x, 1.01f, 0.23f),
                    new Vector3(0.085f, 2.02f, 0.085f), "Timber", Timber);
                var brace = Cube(preparation, "AwningBrace", new Vector3(x - Mathf.Sign(x) * 0.13f, 1.80f, 0.23f),
                    new Vector3(0.06f, 0.39f, 0.06f), "Timber", Timber);
                brace.transform.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Sign(x) * 42f);
            }
            Cube(preparation, "AwningBeam", new Vector3(0f, 2f, 0.23f),
                new Vector3(2.34f, 0.09f, 0.09f), "Timber", Timber);
            for (int i = 0; i < 7; i++)
            {
                float x = (i - 3) * 0.34f;
                string key = i % 2 == 0 ? "CanvasCream" : "CanvasSage";
                Color colour = i % 2 == 0 ? new Color(0.89f, 0.83f, 0.64f) : new Color(0.42f, 0.61f, 0.48f);
                var cloth = Cube(preparation, "CanvasStripe", new Vector3(x, 2.06f, -0.12f),
                    new Vector3(0.337f, 0.035f, 1.20f), key, colour);
                cloth.transform.localRotation = Quaternion.Euler(-7f, 0f, 0f);
                Cube(preparation, "CanvasValance", new Vector3(x, 1.94f, -0.717f),
                    new Vector3(0.337f, 0.16f, 0.035f), key, colour);
            }
            Cube(preparation, "WorkbenchTop", new Vector3(0f, 0.83f, 0f),
                new Vector3(1.91f, 0.08f, 0.72f), "TimberLight", Timber * 1.08f);
            foreach (float x in new[] { -0.79f, 0.79f })
                foreach (float z in new[] { -0.24f, 0.24f })
                    Cube(preparation, "WorkbenchLeg", new Vector3(x, 0.39f, z),
                        new Vector3(0.09f, 0.78f, 0.09f), "Sage", Sage);
            Cube(preparation, "WorkbenchShelf", new Vector3(0f, 0.22f, 0f),
                new Vector3(1.73f, 0.055f, 0.57f), "Timber", Timber);
            Cube(preparation, "ToolDrawer", new Vector3(0.48f, 0.65f, 0f),
                new Vector3(0.71f, 0.23f, 0.61f), "Cream", Cream);
            Cube(preparation, "DrawerHandle", new Vector3(0.48f, 0.66f, -0.319f),
                new Vector3(0.19f, 0.035f, 0.04f), "Sage", Sage);
            Cube(preparation, "SortingTray", new Vector3(-0.39f, 0.9f, -0.01f),
                new Vector3(0.58f, 0.05f, 0.43f), "Sage", Sage);
            foreach (float x in new[] { -0.66f, -0.12f })
                Cube(preparation, "SortingTrayLip", new Vector3(x, 0.94f, -0.01f),
                    new Vector3(0.03f, 0.075f, 0.43f), "Timber", Timber);
            Cube(preparation, "SupplyBox", new Vector3(-0.24f, 0.43f, 0f),
                new Vector3(0.61f, 0.34f, 0.48f), "Carton", new Color(0.78f, 0.62f, 0.39f));
            Cube(preparation, "SupplyBoxLabel", new Vector3(-0.24f, 0.45f, -0.248f),
                new Vector3(0.23f, 0.10f, 0.014f), "Cream", Cream);

            // Empty, stacked drying trays are storage fixtures, not an unlocked processing stage.
            var rack = new GameObject("DryingTrayStorage").transform;
            rack.SetParent(fixtures, false);
            rack.localPosition = new Vector3(11.75f, 0f, 4.94f);
            foreach (float x in new[] { -0.77f, 0.77f })
                foreach (float z in new[] { -0.26f, 0.26f })
                    Cube(rack, "RackUpright", new Vector3(x, 0.69f, z),
                        new Vector3(0.065f, 1.38f, 0.065f), "Sage", Sage);
            foreach (float y in new[] { 0.26f, 0.64f, 1.02f })
            {
                Cube(rack, "TrayShelf", new Vector3(0f, y, 0f),
                    new Vector3(1.62f, 0.05f, 0.61f), "Timber", Timber);
                foreach (float z in new[] { -0.267f, 0.267f })
                    Cube(rack, "TrayRail", new Vector3(0f, y + 0.045f, z),
                        new Vector3(1.59f, 0.07f, 0.025f), "TimberLight", Timber * 1.08f);
            }
            Cube(rack, "RackBackBrace", new Vector3(0f, 0.88f, 0.27f),
                new Vector3(1.60f, 0.05f, 0.045f), "Sage", Sage);

            Pipe(fixtures, "WashWaterMain", new Vector3(9.99f, 0.48f, 5.12f), new Vector3(13.12f, 0.48f, 5.12f), 0.035f);
            Pipe(fixtures, "WashWaterRiser", new Vector3(10.02f, 0.06f, 5.12f), new Vector3(10.02f, 1.02f, 5.12f), 0.035f);
            Pipe(fixtures, "WashWaterTap", new Vector3(10.02f, 1.02f, 5.12f), new Vector3(10.02f, 1.02f, 4.94f), 0.027f);
            Cube(fixtures, "TapLever", new Vector3(10.02f, 1.06f, 5.055f),
                new Vector3(0.16f, 0.035f, 0.045f), "Gold", Gold);
            Cube(fixtures, "WashBasin", new Vector3(10.02f, 0.69f, 4.86f),
                new Vector3(0.68f, 0.17f, 0.51f), "Cream", Cream);
            Cube(fixtures, "BasinInset", new Vector3(10.02f, 0.78f, 4.86f),
                new Vector3(0.54f, 0.015f, 0.38f), "Sage", Sage);
            foreach (float x in new[] { 9.77f, 10.27f })
                Cube(fixtures, "BasinSupport", new Vector3(x, 0.32f, 4.96f),
                    new Vector3(0.065f, 0.64f, 0.065f), "Sage", Sage);
            foreach (float x in new[] { 10.06f, 12.97f })
                Cube(fixtures, "UtilityPipeBracket", new Vector3(x, 0.48f, 5.165f),
                    new Vector3(0.08f, 0.13f, 0.045f), "Timber", Timber);

            // The drainage strip is flat scenery, leaving every worker route passable.
            Cube(fixtures, "DrainChannel", new Vector3(10.45f, 0.022f, -2.72f),
                new Vector3(4.5f, 0.018f, 0.18f), "DrainDark", new Color(0.31f, 0.36f, 0.29f));
            for (int i = 0; i < 18; i++)
                Cube(fixtures, "DrainGrille", new Vector3(8.28f + i * 0.254f, 0.035f, -2.72f),
                    new Vector3(0.045f, 0.015f, 0.16f), "Sage", Sage);
            // These fixtures never move; let the player build batch their shared palette meshes.
            foreach (var renderer in fixtures.GetComponentsInChildren<MeshRenderer>())
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic);
        }

        static void Pipe(Transform parent, string name, Vector3 from, Vector3 to, float radius)
        {
            var direction = to - from;
            var pipe = Primitive(PrimitiveType.Cylinder, parent, name, (from + to) * 0.5f,
                new Vector3(radius * 2f, direction.magnitude * 0.5f, radius * 2f), "Sage", Sage);
            pipe.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction);
        }

        static void Pallet(Transform parent, Vector3 position, bool packed)
        {
            var root = new GameObject(packed ? "TeaParcelPallet" : "HarvestCrate").transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            for (int i = 0; i < 4; i++)
                Cube(root, "Slat", new Vector3(-0.3f + i * 0.2f, 0.08f, 0f), new Vector3(0.16f, 0.09f, 0.72f), "Timber", Timber);
            if (packed)
            {
                for (int i = 0; i < 2; i++)
                {
                    Cube(root, "TeaCarton", new Vector3(0f, 0.27f + i * 0.3f, 0f), new Vector3(0.62f, 0.28f, 0.54f), "Carton", new Color(0.78f, 0.62f, 0.39f));
                    Cube(root, "PackingBand", new Vector3(0f, 0.27f + i * 0.3f, -0.275f), new Vector3(0.12f, 0.28f, 0.012f), "Cream", Cream);
                    Cube(root, "TopPackingBand", new Vector3(0f, 0.414f + i * 0.3f, 0f), new Vector3(0.12f, 0.012f, 0.55f), "Cream", Cream);
                    Cube(root, "TeaSeal", new Vector3(-0.16f, 0.29f + i * 0.3f, -0.278f), new Vector3(0.1f, 0.11f, 0.014f), "Sage", Sage);
                }
            }
            else
            {
                Cube(root, "CrateInside", new Vector3(0f, 0.24f, 0f), new Vector3(0.58f, 0.2f, 0.56f), "Leaves", Sage);
                foreach (float side in new[] { -1f, 1f })
                {
                    for (int slat = 0; slat < 3; slat++)
                    {
                        Cube(root, "CrateSideSlat", new Vector3(side * 0.35f, 0.16f + slat * 0.11f, 0f), new Vector3(0.07f, 0.088f, 0.74f), "Timber", Timber);
                        Cube(root, "CrateEndSlat", new Vector3(0f, 0.16f + slat * 0.11f, side * 0.35f), new Vector3(0.64f, 0.088f, 0.07f), "Timber", Timber);
                    }
                    foreach (float end in new[] { -1f, 1f })
                        Cube(root, "CrateCorner", new Vector3(side * 0.31f, 0.27f, end * 0.31f),
                            new Vector3(0.075f, 0.33f, 0.075f), "TimberLight", Timber * 1.08f);
                }
            }
            var collider = root.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.35f, 0f);
            collider.size = new Vector3(0.76f, 0.7f, 0.76f);
            root.gameObject.AddComponent<WalkBlocker>();
        }

        static void Sign(Transform parent, GardenSkin skin, Camera camera, string name, string title, string caption, Vector3 position, float width)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            root.rotation = Quaternion.Euler(0f, -45f, 0f);
            foreach (float x in new[] { -width * 0.36f, width * 0.36f })
                Cube(root, "Post", new Vector3(x, 0.48f, 0.04f), new Vector3(0.085f, 0.96f, 0.085f), "Timber", Timber);
            bool hasCaption = !string.IsNullOrEmpty(caption);
            var board = Cube(root, "Signboard", new Vector3(0f, 1f, 0f), new Vector3(width, hasCaption ? 0.72f : 0.48f, 0.10f), "Sage", Sage);
            var titleLabel = Label(root, skin, camera, "Title", title, new Vector3(0f, hasCaption ? 1.13f : 1f, -0.065f), 0.043f, Cream);
            titleLabel.transform.localRotation = Quaternion.identity;
            if (hasCaption)
            {
                var subtitle = Label(root, skin, camera, "Caption", caption, new Vector3(0f, 0.88f, -0.065f), 0.019f, Cream);
                subtitle.transform.localRotation = Quaternion.identity;
            }
            var collider = board.AddComponent<BoxCollider>();
            root.gameObject.AddComponent<WalkBlocker>();
        }

        static GameObject Label(Transform parent, GardenSkin skin, Camera camera, string name, string text, Vector3 position, float size, Color colour)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.rotation = camera.transform.rotation;
            var label = go.AddComponent<TextMesh>();
            label.text = text;
            label.fontSize = 80;
            label.characterSize = size;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = colour;
            if (skin.DisplayFont != null)
            {
                label.font = skin.DisplayFont;
                go.GetComponent<MeshRenderer>().sharedMaterial = skin.DisplayFont.material;
            }
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        static GameObject Prop(GameObject prefab, Transform parent, string name, Vector3 position)
        {
            if (prefab == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.localPosition = position;
            foreach (var collider in go.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            Bounds bounds;
            if (GardenArtImporter.TryGetBounds(go.transform, go.transform, out bounds))
            {
                var collider = go.AddComponent<BoxCollider>();
                collider.center = bounds.center;
                collider.size = bounds.size;
                go.AddComponent<WalkBlocker>();
            }
            return go;
        }

        static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 size, string key, Color colour)
        {
            return Primitive(PrimitiveType.Cube, parent, name, position, size, key, colour);
        }

        static GameObject Primitive(PrimitiveType type, Transform parent, string name, Vector3 position, Vector3 size, string key, Color colour)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            Material material;
            if (!Materials.TryGetValue(key, out material) || material == null)
            {
                string path = SceneFactory.MaterialFolder + "/Mat_Site" + key + ".mat";
                material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    var template = ProjectSetup.LitTemplate();
                    material = template != null ? new Material(template) : new Material(ProjectSetup.LitShader());
                    AssetDatabase.CreateAsset(material, path);
                }
                material.color = colour;
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.12f);
                EditorUtility.SetDirty(material);
                Materials[key] = material;
            }
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }
    }
}
