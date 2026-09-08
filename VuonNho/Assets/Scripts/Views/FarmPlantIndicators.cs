using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VuonNho.Views
{
    /// <summary>
    /// Reusable world art for harvest and pests. Built once per plot with shared meshes;
    /// Tick only moves cached transforms and never changes simulation or hitboxes.
    /// </summary>
    internal sealed class FarmPlantIndicators
    {
        static Mesh harvestMesh;
        static Mesh pestMesh;
        static Mesh damageMesh;
        static Material[] materials;
        static int owners;

        readonly Transform ready;
        readonly Transform pest;
        readonly Transform caterpillar;
        readonly float phase;
        Camera camera;
        bool disposed;

        public FarmPlantIndicators(PlotView view)
        {
            AcquireAssets();
            phase = view.PlotId * 1.73f;
            ready = PrepareRoot(view.ReadyBadge, view.transform, "HarvestBasketIndicator");
            pest = PrepareRoot(view.PestBadge, view.transform, "PlantPestIndicator");
            view.ReadyBadge = ready.gameObject;
            view.ReadyBadgeRenderer = null;
            view.PestBadge = pest.gameObject;
            AddMesh(ready, "TeaBasketAndDownPointer", harvestMesh, false);
            AddMesh(pest, "ChewedLeaves", damageMesh, true);
            caterpillar = AddMesh(pest, "LeafCaterpillar", pestMesh, true);
            pest.localPosition = new Vector3(0.35f, 0.19f, -0.34f);
            pest.localRotation = Quaternion.Euler(0f, 18f + view.PlotId % 3 * 13f, 0f);
            ready.localPosition = new Vector3(0f, 1.27f, 0f);
            camera = Camera.main;
            ready.gameObject.SetActive(false);
            pest.gameObject.SetActive(false);
        }

        static Transform PrepareRoot(GameObject existing, Transform parent, string name)
        {
            Transform tr = existing != null ? existing.transform : new GameObject(name).transform;
            // Legacy prefabs contain the old cube/sphere. Keep their objects available for
            // serialized references, but disable their artwork before adding the new mesh.
            Renderer oldRenderer = tr.GetComponent<Renderer>();
            if (oldRenderer != null) oldRenderer.enabled = false;
            Collider oldCollider = tr.GetComponent<Collider>();
            if (oldCollider != null) oldCollider.enabled = false;
            for (int i = 0; i < tr.childCount; i++) tr.GetChild(i).gameObject.SetActive(false);
            tr.name = name;
            tr.SetParent(parent, false);
            tr.localScale = Vector3.one;
            tr.localRotation = Quaternion.identity;
            tr.gameObject.layer = parent.gameObject.layer;
            return tr;
        }

        static Transform AddMesh(Transform parent, string name, Mesh mesh, bool shadows)
        {
            var go = new GameObject(name);
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = shadows;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go.transform;
        }

        public void Tick(float time)
        {
            if (disposed) return;
            if (ready != null && ready.gameObject.activeInHierarchy)
            {
                if (camera == null) camera = Camera.main;
                if (camera != null) ready.rotation = camera.transform.rotation;
                // No orbit or spin: the pointer stays above the exact plot centre.
                ready.localPosition = new Vector3(0f, 1.27f + Mathf.Sin(time * 2.25f + phase) * 0.035f, 0f);
            }
            if (pest != null && pest.gameObject.activeInHierarchy)
            {
                float crawl = Mathf.Sin(time * 1.25f + phase);
                caterpillar.localPosition = new Vector3(crawl * 0.018f, 0.004f, 0f);
                caterpillar.localRotation = Quaternion.Euler(0f, crawl * 2f, 0f);
            }
        }

        static void AcquireAssets()
        {
            if (owners++ > 0) return;
            Color[] colors =
            {
                new Color(0.64f, 0.39f, 0.19f), // woven basket
                new Color(0.86f, 0.66f, 0.37f), // wicker highlights
                new Color(0.28f, 0.43f, 0.19f), // fresh tea / caterpillar
                new Color(0.53f, 0.64f, 0.27f), // leaf ridge / body highlight
                new Color(0.97f, 0.92f, 0.72f), // cream pointer
                new Color(0.25f, 0.20f, 0.12f), // head, feet and leaf damage
                new Color(0.62f, 0.55f, 0.27f)  // damaged leaf edge
            };
            materials = new Material[colors.Length];
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            for (int i = 0; i < colors.Length; i++)
            {
                var material = new Material(shader);
                material.name = "Farm plant indicator " + i;
                material.hideFlags = HideFlags.DontSave;
                material.enableInstancing = true;
                material.color = colors[i];
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", colors[i]);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.1f);
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.1f);
                if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
                // A little fill keeps the small notification readable on the shaded field.
                if (i == 4 && material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", colors[i] * 0.16f);
                }
                materials[i] = material;
            }
            harvestMesh = BuildHarvest();
            pestMesh = BuildCaterpillar();
            damageMesh = BuildDamage();
        }

        static Mesh BuildHarvest()
        {
            var b = new MeshBuilder();
            // Tapered front/back wicker basket, never a coloured cube.
            b.Prism(new[] { new Vector2(-0.19f, -0.025f), new Vector2(0.19f, -0.025f),
                new Vector2(0.15f, -0.20f), new Vector2(-0.15f, -0.20f) }, -0.045f, 0.07f, 0);
            b.Bar(new Vector3(-0.20f, -0.022f, -0.065f), new Vector3(0.20f, -0.022f, -0.065f), 0.029f, 1);
            for (int i = 0; i < 3; i++)
            {
                float y = -0.066f - i * 0.049f;
                float half = 0.18f - i * 0.012f;
                b.Bar(new Vector3(-half, y, -0.063f), new Vector3(half, y, -0.063f), 0.012f, 1);
            }
            for (int i = -2; i <= 2; i++)
                b.Bar(new Vector3(i * 0.066f, -0.035f, -0.071f),
                    new Vector3(i * 0.051f, -0.184f, -0.071f), 0.01f, 1);
            // Arched handle and two unmistakable tea leaves.
            for (int i = 0; i < 12; i++)
            {
                float a = Mathf.PI * i / 12f;
                float c = Mathf.PI * (i + 1) / 12f;
                b.Bar(new Vector3(Mathf.Cos(a) * 0.145f, Mathf.Sin(a) * 0.16f - 0.018f, 0.008f),
                    new Vector3(Mathf.Cos(c) * 0.145f, Mathf.Sin(c) * 0.16f - 0.018f, 0.008f), 0.022f, 1);
            }
            b.Leaf(new Vector3(-0.04f, -0.002f, -0.076f), new Vector3(-0.105f, 0.17f, 0f), 0.061f, 2, 3);
            b.Leaf(new Vector3(0.006f, -0.007f, -0.083f), new Vector3(0.16f, 0.12f, 0f), 0.068f, 2, 3);
            // The separated down-pointing chevron has a dark edge and warm cream face.
            b.Prism(new[] { new Vector2(-0.13f, -0.25f), new Vector2(0.13f, -0.25f),
                new Vector2(0f, -0.40f) }, -0.052f, 0.015f, 2);
            b.Prism(new[] { new Vector2(-0.096f, -0.262f), new Vector2(0.096f, -0.262f),
                new Vector2(0f, -0.374f) }, -0.065f, -0.053f, 4);
            return b.Finish("Harvest wicker basket and downward pointer");
        }

        static Mesh BuildCaterpillar()
        {
            var b = new MeshBuilder();
            // Six overlapping oval segments and a separate brown head read as an insect.
            for (int i = 0; i < 6; i++)
            {
                float x = -0.18f + i * 0.062f;
                float y = 0.061f + Mathf.Sin(i * 0.58f) * 0.012f;
                float radius = 0.046f * (i == 0 ? 0.77f : 1f);
                b.Ellipsoid(new Vector3(x, y, 0f), new Vector3(0.049f, radius, 0.042f), 2, 3);
                b.Ellipsoid(new Vector3(x, y + radius * 0.75f, -0.006f), new Vector3(0.025f, 0.012f, 0.027f), 3, 3);
                if (i == 0 || i == 5) continue;
                for (int side = -1; side <= 1; side += 2)
                    b.Bar(new Vector3(x + 0.013f, y - 0.017f, side * 0.02f),
                        new Vector3(x + 0.032f, 0.012f, side * 0.058f), 0.008f, 5);
            }
            b.Ellipsoid(new Vector3(0.179f, 0.069f, 0f), new Vector3(0.043f, 0.043f, 0.046f), 5, 6);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Bar(new Vector3(0.190f, 0.092f, side * 0.018f),
                    new Vector3(0.224f, 0.134f, side * 0.034f), 0.006f, 5);
                b.Ellipsoid(new Vector3(0.204f, 0.079f, side * 0.032f), new Vector3(0.009f, 0.010f, 0.009f), 4, 4);
            }
            return b.Finish("Natural segmented leaf caterpillar");
        }

        static Mesh BuildDamage()
        {
            var b = new MeshBuilder();
            // The cut-in outline is real missing leaf geometry, with dry brown bites.
            b.DamagedLeaf(new Vector3(0f, 0.008f, 0f), 0f, 1f);
            b.DamagedLeaf(new Vector3(-0.08f, 0.003f, 0.10f), -42f, 0.66f);
            return b.Finish("Chewed tea leaves with dry bite edges");
        }

        sealed class MeshBuilder
        {
            readonly List<Vector3> vertices = new List<Vector3>(1600);
            readonly List<int>[] triangles = new List<int>[7];

            public MeshBuilder()
            {
                for (int i = 0; i < triangles.Length; i++) triangles[i] = new List<int>();
            }

            void Tri(Vector3 a, Vector3 b, Vector3 c, int material)
            {
                int start = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                triangles[material].Add(start); triangles[material].Add(start + 1); triangles[material].Add(start + 2);
            }

            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int material)
            {
                Tri(a, b, c, material); Tri(a, c, d, material);
            }

            public void Prism(Vector2[] outline, float front, float back, int material)
            {
                Vector2 centre = Vector2.zero;
                for (int i = 0; i < outline.Length; i++) centre += outline[i];
                centre /= outline.Length;
                for (int i = 0; i < outline.Length; i++)
                {
                    Vector2 a = outline[i], c = outline[(i + 1) % outline.Length];
                    Tri(new Vector3(centre.x, centre.y, front), new Vector3(a.x, a.y, front), new Vector3(c.x, c.y, front), material);
                    Tri(new Vector3(centre.x, centre.y, back), new Vector3(c.x, c.y, back), new Vector3(a.x, a.y, back), material);
                    Quad(new Vector3(c.x, c.y, front), new Vector3(a.x, a.y, front),
                        new Vector3(a.x, a.y, back), new Vector3(c.x, c.y, back), material);
                }
            }

            public void Bar(Vector3 from, Vector3 to, float width, int material)
            {
                Vector3 axis = (to - from).normalized;
                Vector3 side = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.forward).normalized * width * 0.5f;
                Vector3 depth = Vector3.Cross(axis, side).normalized * width * 0.5f;
                Vector3[] ring = { side + depth, side - depth, -side - depth, -side + depth };
                for (int i = 0; i < 4; i++) Quad(from + ring[i], to + ring[i], to + ring[(i + 1) % 4], from + ring[(i + 1) % 4], material);
                Quad(from + ring[3], from + ring[2], from + ring[1], from + ring[0], material);
                Quad(to + ring[0], to + ring[1], to + ring[2], to + ring[3], material);
            }

            public void Leaf(Vector3 origin, Vector3 direction, float width, int dark, int light)
            {
                Vector3 side = new Vector3(-direction.y, direction.x, 0f).normalized * width;
                Vector3 shoulder = origin + direction * 0.47f;
                Vector3 ridge = shoulder + Vector3.back * 0.026f;
                Vector3 tip = origin + direction;
                Tri(origin, ridge, shoulder - side, dark); Tri(shoulder - side, ridge, tip, dark);
                Tri(origin, shoulder + side, ridge, light); Tri(ridge, shoulder + side, tip, light);
                Bar(origin, tip, 0.006f, light);
            }

            public void Ellipsoid(Vector3 centre, Vector3 scale, int dark, int light)
            {
                const int sides = 8, rings = 5;
                for (int ring = 0; ring < rings; ring++)
                    for (int side = 0; side < sides; side++)
                    {
                        float p0 = -Mathf.PI * 0.5f + Mathf.PI * ring / rings;
                        float p1 = -Mathf.PI * 0.5f + Mathf.PI * (ring + 1) / rings;
                        float a0 = Mathf.PI * 2f * side / sides;
                        float a1 = Mathf.PI * 2f * (side + 1) / sides;
                        Quad(Point(centre, scale, p0, a1), Point(centre, scale, p0, a0),
                            Point(centre, scale, p1, a0), Point(centre, scale, p1, a1), ring >= 3 ? light : dark);
                    }
            }

            static Vector3 Point(Vector3 centre, Vector3 scale, float pitch, float angle)
            {
                return centre + Vector3.Scale(scale, new Vector3(Mathf.Cos(pitch) * Mathf.Cos(angle),
                    Mathf.Sin(pitch), Mathf.Cos(pitch) * Mathf.Sin(angle)));
            }

            public void DamagedLeaf(Vector3 origin, float yaw, float size)
            {
                Vector3[] edge =
                {
                    new Vector3(-0.25f, 0f, 0f), new Vector3(-0.14f, 0f, -0.077f),
                    new Vector3(-0.083f, 0f, -0.089f), new Vector3(-0.068f, 0f, -0.038f),
                    new Vector3(-0.025f, 0f, -0.09f), new Vector3(0.105f, 0f, -0.062f),
                    new Vector3(0.23f, 0f, 0f), new Vector3(0.098f, 0f, 0.080f),
                    new Vector3(0.048f, 0f, 0.033f), new Vector3(0.018f, 0f, 0.090f),
                    new Vector3(-0.11f, 0f, 0.083f)
                };
                Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
                Vector3 centre = origin + Vector3.up * 0.016f;
                for (int i = 0; i < edge.Length; i++)
                {
                    Vector3 a = origin + rotation * (edge[i] * size);
                    Vector3 c = origin + rotation * (edge[(i + 1) % edge.Length] * size);
                    bool bitten = i == 2 || i == 3 || i == 7 || i == 8;
                    Tri(centre, c, a, i < 6 ? 2 : 3);
                    if (bitten) Bar(a, c, 0.010f * size, 6);
                }
                Bar(origin + rotation * new Vector3(-0.25f * size, 0.019f, 0f),
                    origin + rotation * new Vector3(0.21f * size, 0.019f, 0f), 0.008f, 6);
            }

            public Mesh Finish(string name)
            {
                Mesh mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
                mesh.SetVertices(vertices);
                mesh.subMeshCount = triangles.Length;
                for (int i = 0; i < triangles.Length; i++) mesh.SetTriangles(triangles[i], i);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                mesh.UploadMeshData(true);
                return mesh;
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (--owners > 0) return;
            Object.Destroy(harvestMesh); Object.Destroy(pestMesh); Object.Destroy(damageMesh);
            if (materials != null)
                for (int i = 0; i < materials.Length; i++) Object.Destroy(materials[i]);
            harvestMesh = null; pestMesh = null; damageMesh = null; materials = null;
        }
    }
}
