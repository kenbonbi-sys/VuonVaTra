using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using VuonNho.Views;
using Object = UnityEngine.Object;

namespace VuonNho.EditorTools
{
    /// <summary>Deterministic scene art for the six-stage U-shaped tea production line.</summary>
    public static class ProductionLineArt
    {
        const float BeltWidth = 0.48f;
        const float OuterWidth = 0.66f;
        static Material _cream, _belt, _metal, _paint, _leaves, _dried, _parcel, _label;

        public static void Build(GardenSkin skin, Camera camera, IReadOnlyList<StationView> stations)
        {
            if (stations == null || stations.Count == 0) return;
            PrepareMaterials();
            Physics.SyncTransforms();
            var root = new GameObject("ProductionLine");

            for (int i = 0; i < stations.Count; i++)
            {
                BuildNumber(root.transform, stations[i], i + 1, skin, camera);
                if (i == stations.Count - 1) continue;
                var source = stations[i];
                var destination = stations[i + 1];
                Vector3[] path;
                if (Mathf.Abs(source.transform.position.x - destination.transform.position.x) < 0.2f)
                {
                    // The return runs outside the machines; the worker remains in front of stage 03.
                    var sourceBounds = MachineBounds(source);
                    var destinationBounds = MachineBounds(destination);
                    float outsideX = Mathf.Max(sourceBounds.max.x, destinationBounds.max.x) + 0.40f;
                    path = new[]
                    {
                        Edge(source, Vector3.right),
                        new Vector3(outsideX, 0f, source.transform.position.z),
                        new Vector3(outsideX, 0f, destination.transform.position.z),
                        Edge(destination, Vector3.right)
                    };
                }
                else
                {
                    var direction = (destination.transform.position - source.transform.position).normalized;
                    path = new[] { Edge(source, direction), Edge(destination, -direction) };
                }
                BuildConnection(root.transform, "Link_" + (i + 1).ToString("00") + "_" + (i + 2).ToString("00"),
                    source.StageId, destination.StageId, path, i >= 3, false);
            }

            var first = stations[0];
            var last = stations[stations.Count - 1];
            BuildConnection(root.transform, "Intake", first.StageId, first.StageId,
                new[] { new Vector3(3.7f, 0f, first.transform.position.z), Edge(first, Vector3.left) }, false, false);
            BuildConnection(root.transform, "PackedTeaOutput", last.StageId, last.StageId,
                new[] { Edge(last, Vector3.left), new Vector3(3.8f, 0f, last.transform.position.z) }, true, true);
        }

        static void BuildConnection(Transform parent, string name, string source, string destination,
                                    Vector3[] path, bool dried, bool parcels)
        {
            var root = Child(parent, name);
            var visuals = Child(root, "OwnedVisuals");
            var cargoRoot = Child(visuals, "DecorativeCargo");
            float distance = 0f;
            for (int i = 1; i < path.Length; i++)
            {
                float length = Vector3.Distance(path[i - 1], path[i]);
                if (length < 0.05f) continue;
                BuildBelt(visuals, path[i - 1], path[i], i);
                BuildArrow(root, "PaintedDirection_" + i, (path[i - 1] + path[i]) * 0.5f + Vector3.up * 0.026f,
                           path[i] - path[i - 1], 0.85f, _paint);
                distance += length;
            }

            // Overlapping dark squares make continuous, simple low-poly transfer corners.
            for (int i = 1; i < path.Length - 1; i++)
            {
                var corner = Child(visuals, "TransferCorner_" + i);
                corner.localPosition = path[i];
                Cube(corner, "Housing", new Vector3(0f, 0.48f, 0f), new Vector3(OuterWidth, 0.15f, OuterWidth), _cream);
                Cube(corner, "Turntable", new Vector3(0f, 0.566f, 0f), new Vector3(BeltWidth, 0.025f, BeltWidth), _belt);
                var cornerBlocker = corner.gameObject.AddComponent<BoxCollider>();
                cornerBlocker.center = new Vector3(0f, 0.30f, 0f);
                cornerBlocker.size = new Vector3(OuterWidth, 0.60f, OuterWidth);
                corner.gameObject.AddComponent<WalkBlocker>();
            }

            var view = root.gameObject.AddComponent<ProductionConveyorView>();
            view.SourceStageId = source;
            view.DestinationStageId = destination;
            view.VisualRoot = visuals.gameObject;
            view.Path = path;
            int count = Mathf.Clamp(Mathf.FloorToInt(distance / 1.2f), 1, 4);
            view.Cargo = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                var cargo = parcels ? BuildParcel(cargoRoot, i) : BuildTeaTray(cargoRoot, i, dried);
                view.Cargo[i] = cargo;
                PositionForEditor(cargo, path, distance * (i + 0.5f) / count, view.CargoHeight);
            }
        }

        static void BuildBelt(Transform parent, Vector3 from, Vector3 to, int index)
        {
            float length = Vector3.Distance(from, to);
            var segment = Child(parent, "ConveyorSpan_" + index);
            segment.localPosition = (from + to) * 0.5f;
            segment.localRotation = Quaternion.LookRotation(to - from, Vector3.up);
            Cube(segment, "Frame", new Vector3(0f, 0.465f, 0f), new Vector3(OuterWidth, 0.17f, length), _cream);
            Cube(segment, "Belt", new Vector3(0f, 0.55f, 0f), new Vector3(BeltWidth, 0.035f, length), _belt);
            for (int side = -1; side <= 1; side += 2)
                Cube(segment, "EdgeRail_" + side, new Vector3(side * 0.30f, 0.583f, 0f),
                     new Vector3(0.045f, 0.08f, length), _metal);

            int supports = length > 2.0f ? 3 : 2;
            for (int i = 0; i < supports; i++)
            {
                float z = Mathf.Lerp(-length * 0.5f + 0.12f, length * 0.5f - 0.12f, (float)i / (supports - 1));
                if (length < 0.30f) z = 0f;
                for (int side = -1; side <= 1; side += 2)
                {
                    Cube(segment, "Leg_" + i + "_" + side, new Vector3(side * 0.255f, 0.21f, z),
                         new Vector3(0.065f, 0.42f, 0.075f), _metal);
                    Cube(segment, "Foot_" + i + "_" + side, new Vector3(side * 0.255f, 0.035f, z),
                         new Vector3(0.14f, 0.07f, 0.18f), _metal);
                }
                var roller = Primitive(segment, PrimitiveType.Cylinder, "Roller_" + i,
                    new Vector3(0f, 0.503f, z), new Vector3(0.115f, 0.28f, 0.115f), _metal);
                roller.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }

            int arrows = Mathf.Max(1, Mathf.RoundToInt(length / 0.75f));
            for (int i = 0; i < arrows; i++)
                BuildArrow(segment, "BeltDirection_" + i,
                    new Vector3(0f, 0.575f, Mathf.Lerp(-length * 0.34f, length * 0.34f, (i + 0.5f) / arrows)),
                    Vector3.forward, 0.44f, _cream);

            var blocker = segment.gameObject.AddComponent<BoxCollider>();
            blocker.center = new Vector3(0f, 0.30f, 0f);
            blocker.size = new Vector3(OuterWidth, 0.60f, length);
            segment.gameObject.AddComponent<WalkBlocker>();
        }

        static void BuildArrow(Transform parent, string name, Vector3 position, Vector3 direction,
                               float scale, Material material)
        {
            var arrow = Child(parent, name);
            arrow.localPosition = position;
            arrow.localRotation = Quaternion.LookRotation(direction, Vector3.up);
            arrow.localScale = Vector3.one * scale;
            for (int side = -1; side <= 1; side += 2)
            {
                var stroke = Cube(arrow, "Chevron_" + side, new Vector3(side * 0.10f, 0f, 0f),
                                  new Vector3(0.055f, 0.006f, 0.28f), material);
                stroke.localRotation = Quaternion.Euler(0f, -side * 45f, 0f);
            }
        }

        static Transform BuildTeaTray(Transform parent, int index, bool dried)
        {
            var tray = Child(parent, "TeaTray_" + index);
            Cube(tray, "TrayBase", Vector3.zero, new Vector3(0.35f, 0.055f, 0.29f), _parcel);
            for (int side = -1; side <= 1; side += 2)
                Cube(tray, "Lip_" + side, new Vector3(side * 0.165f, 0.038f, 0f),
                     new Vector3(0.022f, 0.07f, 0.29f), _cream);
            for (int i = 0; i < 3; i++)
            {
                var leaf = Primitive(tray, PrimitiveType.Sphere, "TeaLeaf_" + i,
                    new Vector3((i - 1) * 0.082f, 0.052f, (i % 2 == 0 ? -1f : 1f) * 0.035f),
                    new Vector3(0.075f, 0.055f, 0.19f), dried ? _dried : _leaves);
                leaf.localRotation = Quaternion.Euler(0f, i * 24f - 20f, 0f);
            }
            return tray;
        }

        static Transform BuildParcel(Transform parent, int index)
        {
            var parcel = Child(parent, "TeaParcel_" + index);
            Cube(parcel, "KraftBox", new Vector3(0f, 0.09f, 0f), new Vector3(0.28f, 0.20f, 0.24f), _parcel);
            Cube(parcel, "SageBand", new Vector3(0f, 0.094f, 0f), new Vector3(0.065f, 0.211f, 0.251f), _metal);
            Cube(parcel, "CreamLabel", new Vector3(0.075f, 0.197f, 0f), new Vector3(0.075f, 0.005f, 0.09f), _cream);
            return parcel;
        }

        static void BuildNumber(Transform parent, StationView station, int number, GardenSkin skin, Camera camera)
        {
            var bounds = MachineBounds(station);
            var marker = Child(parent, "StageNumber_" + number.ToString("00"));
            marker.localPosition = new Vector3(station.transform.position.x - 0.48f, 0f, bounds.min.z - 0.28f);
            Cube(marker, "Post", new Vector3(0f, 0.24f, 0f), new Vector3(0.035f, 0.48f, 0.035f), _metal);
            var face = Child(marker, "Face");
            face.localPosition = Vector3.up * 0.51f;
            if (camera != null) face.rotation = camera.transform.rotation;
            Cube(face, "CreamPlate", Vector3.zero, new Vector3(0.44f, 0.265f, 0.035f), _cream);
            var textRoot = Child(face, "Number");
            textRoot.localPosition = new Vector3(0f, -0.005f, -0.021f);
            var text = textRoot.gameObject.AddComponent<TextMesh>();
            text.text = number.ToString("00");
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 52;
            text.characterSize = 0.044f;
            text.color = _label.color;
            text.font = skin != null && skin.DisplayFont != null
                ? skin.DisplayFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var renderer = text.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = text.font.material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        static Bounds MachineBounds(StationView station)
        {
            var collider = station.GetComponent<Collider>();
            if (collider != null && collider.enabled && collider.bounds.size.sqrMagnitude > 0f)
                return collider.bounds;
            // Box data also works if an ownership view has disabled the gameplay collider.
            var box = collider as BoxCollider;
            if (box != null)
                return new Bounds(station.transform.TransformPoint(box.center),
                    Vector3.Scale(box.size, station.transform.lossyScale));
            return new Bounds(station.transform.position + Vector3.up * 0.6f, new Vector3(1.4f, 1.2f, 1.2f));
        }

        static Vector3 Edge(StationView station, Vector3 direction)
        {
            var bounds = MachineBounds(station);
            var position = station.transform.position;
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
                position.x = direction.x > 0f ? bounds.max.x + 0.025f : bounds.min.x - 0.025f;
            else
                position.z = direction.z > 0f ? bounds.max.z + 0.025f : bounds.min.z - 0.025f;
            position.y = 0f;
            return position;
        }

        static void PositionForEditor(Transform cargo, Vector3[] path, float distance, float height)
        {
            for (int i = 1; i < path.Length; i++)
            {
                var direction = path[i] - path[i - 1];
                float length = direction.magnitude;
                if (length < 0.001f) continue;
                if (distance <= length || i == path.Length - 1)
                {
                    cargo.localPosition = path[i - 1] + direction * Mathf.Clamp01(distance / length) + Vector3.up * height;
                    cargo.localRotation = Quaternion.LookRotation(direction, Vector3.up);
                    return;
                }
                distance -= length;
            }
        }

        static Transform Child(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        static Transform Cube(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            return Primitive(parent, PrimitiveType.Cube, name, position, size, material);
        }

        static Transform Primitive(Transform parent, PrimitiveType type, string name, Vector3 position,
                                   Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go.transform;
        }

        static void PrepareMaterials()
        {
            _cream = Material("Cream", new Color(0.88f, 0.86f, 0.68f));
            _belt = Material("Belt", new Color(0.24f, 0.34f, 0.25f));
            _metal = Material("Sage", new Color(0.40f, 0.59f, 0.48f));
            _paint = Material("RoutePaint", new Color(0.61f, 0.63f, 0.47f));
            _leaves = Material("FreshTea", new Color(0.38f, 0.53f, 0.19f));
            _dried = Material("DriedTea", new Color(0.43f, 0.36f, 0.17f));
            _parcel = Material("Kraft", new Color(0.66f, 0.46f, 0.25f));
            _label = Material("Ink", new Color(0.22f, 0.32f, 0.24f));
        }

        static Material Material(string key, Color color)
        {
            const string folder = "Assets/Settings/Materials";
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            string path = folder + "/Mat_Line" + key + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var template = ProjectSetup.LitTemplate();
                material = template != null ? new Material(template) : new Material(ProjectSetup.LitShader());
                material.name = "Mat_Line" + key;
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.12f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
