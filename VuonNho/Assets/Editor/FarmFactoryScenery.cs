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
            foreach (float x in new[] { -1.43f, 1.43f })
                Cube(shelter, "Post", new Vector3(x, 0.84f, 0.45f), new Vector3(0.10f, 1.68f, 0.10f), "Timber", Timber);
            Cube(shelter, "Roof", new Vector3(0f, 1.68f, 0f), new Vector3(3.48f, 0.12f, 1.7f), "Sage", Sage);
            Cube(shelter, "Fascia", new Vector3(0f, 1.58f, -0.81f), new Vector3(3.45f, 0.17f, 0.055f), "Cream", Cream);
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
                }
            }
            else
            {
                Cube(root, "CrateInside", new Vector3(0f, 0.24f, 0f), new Vector3(0.58f, 0.2f, 0.56f), "Leaves", Sage);
                foreach (float side in new[] { -1f, 1f })
                {
                    Cube(root, "CrateSide", new Vector3(side * 0.35f, 0.27f, 0f), new Vector3(0.07f, 0.33f, 0.74f), "Timber", Timber);
                    Cube(root, "CrateSide", new Vector3(0f, 0.27f, side * 0.35f), new Vector3(0.64f, 0.33f, 0.07f), "Timber", Timber);
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
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
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
                EditorUtility.SetDirty(material);
                Materials[key] = material;
            }
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }
    }
}
