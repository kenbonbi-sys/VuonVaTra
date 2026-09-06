using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using VuonNho.Core;

namespace VuonNho.Views
{
    /// <summary>Danh dau nen vuon de raycast biet cho nao duoc phep dat trang tri.</summary>
    public sealed class GardenGround : MonoBehaviour
    {
    }

    /// <summary>Gan chi so cua mon trang tri vao vat the trong scene de bam go duoc.</summary>
    public sealed class DecorationHandle : MonoBehaviour
    {
        public int Index;
    }

    /// <summary>
    /// Dung lai toan bo trang tri tu GameState moi khi danh sach doi. Khong giu trang thai rieng:
    /// state van la nguon su that duy nhat, view mat di dung lai van y nguyen.
    /// </summary>
    public sealed class DecorationLayer : MonoBehaviour
    {
        public GardenSkin Skin;

        GameSession _session;
        readonly List<GameObject> _spawned = new List<GameObject>();
        string _renderedSignature = "";

        public void Bind(GameSession session)
        {
            _session = session;
            _renderedSignature = "";
            Rebuild();
        }

        /// <summary>Goi moi frame; chi dung lai khi danh sach thuc su doi.</summary>
        public void RefreshIfChanged()
        {
            if (_session == null || _session.State == null) return;
            if (Signature(_session.State) == _renderedSignature) return;
            Rebuild();
        }

        static string Signature(GameState state)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < state.Decorations.Count; i++)
            {
                var item = state.Decorations[i];
                builder.Append(item.DefinitionId).Append('|')
                       .Append(item.XMm).Append('|')
                       .Append(item.ZMm).Append('|')
                       .Append(item.RotationDeg).Append(';');
            }
            return builder.ToString();
        }

        public void Rebuild()
        {
            if (_session == null || _session.State == null) return;

            for (int i = 0; i < _spawned.Count; i++)
                if (_spawned[i] != null) Destroy(_spawned[i]);
            _spawned.Clear();

            var state = _session.State;
            for (int i = 0; i < state.Decorations.Count; i++)
                _spawned.Add(BuildOne(state.Decorations[i], i));

            _renderedSignature = Signature(state);
        }

        GameObject BuildOne(PlacedDecoration placed, int index)
        {
            var root = new GameObject("Deco_" + index + "_" + placed.DefinitionId);
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(placed.XMm / 1000f, 0f, placed.ZMm / 1000f);
            root.transform.localRotation = Quaternion.Euler(0f, placed.RotationDeg, 0f);

            var handle = root.AddComponent<DecorationHandle>();
            handle.Index = index;

            var prefab = Skin != null ? Skin.DecorationPrefabFor(placed.DefinitionId) : null;
            if (prefab != null)
            {
                var art = Instantiate(prefab, root.transform);
                art.name = "VisualRoot";
                art.transform.localPosition = Vector3.zero;
                art.transform.localRotation = Quaternion.identity;
                DisableColliders(art);
            }
            else
            {
                BuildPlaceholder(root.transform, placed.DefinitionId);
            }

            // Collider gameplay nam tren wrapper, khong tren mesh trang tri — cung hop dong
            // prefab nhu o dat va quay tra.
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.35f, 0f);
            collider.size = new Vector3(0.7f, 0.7f, 0.7f);
            return root;
        }

        static void DisableColliders(GameObject art)
        {
            var colliders = art.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;
        }

        /// <summary>Chua co model thi van phai nhin thay duoc, giong cach o dat rot ve primitive.</summary>
        void BuildPlaceholder(Transform parent, string definitionId)
        {
            switch (definitionId)
            {
                case DefaultDecorations.StonePath:
                    Primitive(parent, PrimitiveType.Cube, new Vector3(0f, 0.03f, 0f),
                              new Vector3(0.5f, 0.06f, 0.5f), GardenPalette.Cream);
                    break;
                case DefaultDecorations.Planter:
                    Primitive(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.13f, 0f),
                              new Vector3(0.34f, 0.13f, 0.34f), GardenPalette.Wood);
                    Primitive(parent, PrimitiveType.Sphere, new Vector3(0f, 0.34f, 0f),
                              new Vector3(0.36f, 0.28f, 0.36f), GardenPalette.Grass);
                    break;
                case DefaultDecorations.Lantern:
                    Primitive(parent, PrimitiveType.Cube, new Vector3(0f, 0.35f, 0f),
                              new Vector3(0.08f, 0.7f, 0.08f), GardenPalette.Wood);
                    Primitive(parent, PrimitiveType.Cube, new Vector3(0f, 0.78f, 0f),
                              new Vector3(0.24f, 0.26f, 0.24f), GardenPalette.Coin);
                    break;
                case DefaultDecorations.Bench:
                    Primitive(parent, PrimitiveType.Cube, new Vector3(0f, 0.34f, 0f),
                              new Vector3(1.0f, 0.1f, 0.36f), GardenPalette.Wood);
                    Primitive(parent, PrimitiveType.Cube, new Vector3(0f, 0.56f, -0.16f),
                              new Vector3(1.0f, 0.34f, 0.08f), GardenPalette.Wood);
                    Primitive(parent, PrimitiveType.Cube, new Vector3(-0.4f, 0.15f, 0f),
                              new Vector3(0.09f, 0.3f, 0.3f), GardenPalette.Wood);
                    Primitive(parent, PrimitiveType.Cube, new Vector3(0.4f, 0.15f, 0f),
                              new Vector3(0.09f, 0.3f, 0.3f), GardenPalette.Wood);
                    break;
                default:
                    Primitive(parent, PrimitiveType.Cube, new Vector3(0f, 0.42f, 0f),
                              new Vector3(0.1f, 0.84f, 0.1f), GardenPalette.Wood);
                    Primitive(parent, PrimitiveType.Cube, new Vector3(0f, 0.86f, 0f),
                              new Vector3(0.8f, 0.42f, 0.07f), GardenPalette.Cream);
                    break;
            }
        }

        static void Primitive(Transform parent, PrimitiveType type, Vector3 position,
                              Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;

            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = MaterialFor(color);
        }

        static readonly Dictionary<Color, Material> MaterialCache = new Dictionary<Color, Material>();

        /// <summary>
        /// CreatePrimitive gan material cua Built-in nen duoi URP se ra mau hong. Lay material
        /// mac dinh cua chinh pipeline dang chay roi nhan ban theo mau, va dung chung giua cac mon.
        /// </summary>
        static Material MaterialFor(Color color)
        {
            Material cached;
            if (MaterialCache.TryGetValue(color, out cached) && cached != null) return cached;

            Material material = null;
            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline != null && pipeline.defaultMaterial != null)
                material = new Material(pipeline.defaultMaterial);

            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader != null) material = new Material(shader);
            }
            if (material == null) return null;

            material.color = color;
            MaterialCache[color] = material;
            return material;
        }
    }
}
