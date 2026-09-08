using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VuonNho.Views
{
    /// <summary>
    /// Presentation-only grass for a plot. Replaces legacy cube weeds once, then
    /// animates cached transforms. Folded blades are solid, tapered mesh geometry;
    /// they need no texture, transparent shader, collider, or per-frame mesh edits.
    /// </summary>
    internal sealed class PlotVegetation
    {
        const int TuftCount = 8;
        static readonly float[] DensityThresholds = { -0.12f, -0.12f, 0.14f, 0.28f, 0.43f, 0.58f, 0.73f, 0.88f };
        static Mesh[] sharedMeshes;
        static Material[] sharedMaterials;
        static int owners;

        readonly Transform root;
        readonly Transform[] tufts = new Transform[TuftCount];
        readonly Quaternion[] restRotations = new Quaternion[TuftCount];
        readonly float[] variations = new float[TuftCount];
        readonly float phase;
        float severity;
        float targetSeverity;
        bool hasSeverity;
        bool disposed;

        public PlotVegetation(Transform weedRoot, int plotId)
        {
            root = weedRoot;
            phase = plotId * 1.73f;
            Material sourceMaterial = null;
            float reach = 0.448f;
            int existing = root.childCount;
            for (int i = 0; i < existing; i++)
            {
                Transform child = root.GetChild(i);
                if (!child.name.StartsWith("Weed_", System.StringComparison.Ordinal)) continue;
                Renderer oldRenderer = child.GetComponent<Renderer>();
                if (sourceMaterial == null && oldRenderer != null) sourceMaterial = oldRenderer.sharedMaterial;
                float extent = Mathf.Max(Mathf.Abs(child.localPosition.x), Mathf.Abs(child.localPosition.z));
                if (extent > 0.1f) reach = extent;
                child.gameObject.SetActive(false);
            }

            AcquireAssets(sourceMaterial);
            for (int i = 0; i < TuftCount; i++)
            {
                // Opposing corners appear first; denser weeds fill the remaining edges.
                Vector3 position;
                switch (i)
                {
                    case 0: position = new Vector3(-reach, 0f, -reach); break;
                    case 1: position = new Vector3(reach, 0f, reach); break;
                    case 2: position = new Vector3(reach, 0f, -reach); break;
                    case 3: position = new Vector3(-reach, 0f, reach); break;
                    case 4: position = new Vector3(-reach * 1.09f, 0f, reach * 0.08f); break;
                    case 5: position = new Vector3(reach * 1.08f, 0f, -reach * 0.14f); break;
                    case 6: position = new Vector3(reach * 0.12f, 0f, -reach * 1.05f); break;
                    default: position = new Vector3(-reach * 0.19f, 0f, reach * 1.07f); break;
                }
                position.x += (Noise(plotId * 19 + i * 7) - 0.5f) * 0.052f;
                position.z += (Noise(plotId * 31 + i * 11) - 0.5f) * 0.052f;
                GameObject tuft = new GameObject("GrassTuft_" + i);
                tuft.layer = root.gameObject.layer;
                Transform tr = tuft.transform;
                tr.SetParent(root, false);
                tr.localPosition = position;
                restRotations[i] = Quaternion.Euler(0f, Noise(plotId * 13 + i * 17) * 360f, 0f);
                tr.localRotation = restRotations[i];
                variations[i] = Mathf.Lerp(0.86f, 1.12f, Noise(plotId * 23 + i * 29));
                tufts[i] = tr;
                MeshFilter filter = tuft.AddComponent<MeshFilter>();
                filter.sharedMesh = sharedMeshes[(plotId + i) % sharedMeshes.Length];
                MeshRenderer renderer = tuft.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = sharedMaterials;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
        }

        public void SetSeverity(float value)
        {
            targetSeverity = Mathf.Clamp01(value);
            if (hasSeverity) return;
            hasSeverity = true;
            severity = targetSeverity;
            ApplyPose(Time.time);
        }

        public void Tick(float time, float deltaTime)
        {
            if (disposed || !hasSeverity || root == null || !root.gameObject.activeInHierarchy) return;
            severity = Mathf.Lerp(severity, targetSeverity, 1f - Mathf.Exp(-deltaTime * 5f));
            ApplyPose(time);
        }

        void ApplyPose(float time)
        {
            float height = Mathf.Lerp(0.65f, 1.16f, severity);
            float width = Mathf.Lerp(0.73f, 1.04f, severity);
            for (int i = 0; i < TuftCount; i++)
            {
                float emergence = Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(DensityThresholds[i] - 0.09f, DensityThresholds[i] + 0.10f, severity));
                bool visible = emergence > 0.025f;
                Transform tuft = tufts[i];
                if (tuft.gameObject.activeSelf != visible) tuft.gameObject.SetActive(visible);
                if (!visible) continue;
                float size = variations[i] * Mathf.Lerp(0.40f, 1f, emergence);
                tuft.localScale = new Vector3(width * size, height * size, width * size);
                float wave = Mathf.Sin(time * 1.34f + phase + i * 0.87f);
                float ripple = Mathf.Sin(time * 2.07f + phase * 0.61f + i * 1.21f);
                tuft.localRotation = restRotations[i] * Quaternion.Euler(wave * 2.5f, 0f, ripple * 1.65f);
            }
        }

        static void AcquireAssets(Material source)
        {
            if (owners++ > 0) return;
            sharedMaterials = new Material[3];
            Color weed = GardenPalette.Weed;
            Color[] colors =
            {
                Color.Lerp(weed, new Color(0.30f, 0.41f, 0.20f), 0.48f),
                Color.Lerp(weed, new Color(0.43f, 0.56f, 0.28f), 0.40f),
                Color.Lerp(weed, new Color(0.76f, 0.77f, 0.43f), 0.40f)
            };
            for (int i = 0; i < sharedMaterials.Length; i++)
            {
                Material material = source != null
                    ? new Material(source)
                    : new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                material.name = "Farm grass " + i;
                material.hideFlags = HideFlags.DontSave;
                material.enableInstancing = true;
                material.color = colors[i];
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", colors[i]);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.12f);
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.12f);
                if (material.HasProperty("_SpecularHighlights")) material.SetFloat("_SpecularHighlights", 0f);
                sharedMaterials[i] = material;
            }
            sharedMeshes = new Mesh[3];
            for (int i = 0; i < sharedMeshes.Length; i++) sharedMeshes[i] = BuildTuft(i);
        }

        static Mesh BuildTuft(int variant)
        {
            var vertices = new List<Vector3>(600);
            var triangles = new[] { new List<int>(400), new List<int>(400), new List<int>(400) };
            // Broad lower blades, narrow upright centres and bent tips make a recognisable
            // grass silhouette at the game's camera distance, without a block-shaped core.
            for (int blade = 0; blade < 7; blade++)
            {
                int seed = variant * 197 + blade * 37;
                float angle = blade * 2.399963f + variant * 0.48f;
                Vector3 forward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 right = new Vector3(-forward.z, 0f, forward.x);
                float height = Mathf.Lerp(0.25f, 0.43f, Noise(seed + 3));
                float bladeWidth = Mathf.Lerp(0.056f, 0.091f, Noise(seed + 7));
                float lean = Mathf.Lerp(0.10f, 0.22f, Noise(seed + 11));
                if (blade == 6) { height = 0.46f; lean = 0.045f; bladeWidth = 0.052f; }
                Vector3 basePoint = forward * Mathf.Lerp(0.008f, 0.045f, Noise(seed + 19));
                Vector3[] previous = BladeSection(basePoint, forward, right, height, bladeWidth, lean, 0f);
                for (int section = 1; section <= 4; section++)
                {
                    float t = section * 0.22f;
                    Vector3[] next = BladeSection(basePoint, forward, right, height, bladeWidth, lean, t);
                    for (int side = 0; side < 4; side++)
                    {
                        int shade = side == 0 ? 2 : side == 1 ? 1 : 0;
                        AddQuad(vertices, triangles[shade], previous[side], previous[(side + 1) % 4],
                            next[(side + 1) % 4], next[side]);
                    }
                    previous = next;
                }
                Vector3 tip = basePoint + forward * lean + Vector3.up * height;
                for (int side = 0; side < 4; side++)
                    AddTriangle(vertices, triangles[side < 2 ? 2 : 1], previous[side], previous[(side + 1) % 4], tip);
            }
            Mesh mesh = new Mesh { name = "Folded grass tuft " + variant, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(vertices);
            mesh.subMeshCount = triangles.Length;
            for (int i = 0; i < triangles.Length; i++) mesh.SetTriangles(triangles[i], i);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        static Vector3[] BladeSection(Vector3 origin, Vector3 forward, Vector3 right,
            float height, float width, float lean, float t)
        {
            Vector3 centre = origin + Vector3.up * (height * t) + forward * (lean * t * t);
            float profile = t < 0.24f ? Mathf.Lerp(0.45f, 1f, t / 0.24f) : Mathf.Pow((1f - t) / 0.76f, 0.8f);
            float halfWidth = width * profile * 0.5f;
            float ridge = width * profile * 0.18f;
            return new[]
            {
                centre - right * halfWidth,
                centre + forward * ridge,
                centre + right * halfWidth,
                centre - forward * ridge * 0.55f
            };
        }

        static void AddQuad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int start = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
            triangles.Add(start); triangles.Add(start + 3); triangles.Add(start + 2);
        }

        static void AddTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            int start = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
        }

        static float Noise(int seed)
        {
            uint bits = unchecked((uint)seed * 747796405u + 2891336453u);
            bits = ((bits >> (int)((bits >> 28) + 4)) ^ bits) * 277803737u;
            bits = (bits >> 22) ^ bits;
            return (bits & 0x00ffffff) / 16777215f;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (--owners > 0) return;
            if (sharedMeshes != null)
                for (int i = 0; i < sharedMeshes.Length; i++) Object.Destroy(sharedMeshes[i]);
            if (sharedMaterials != null)
                for (int i = 0; i < sharedMaterials.Length; i++) Object.Destroy(sharedMaterials[i]);
            sharedMeshes = null;
            sharedMaterials = null;
        }
    }
}
