using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VuonNho.Views;

namespace VuonNho.EditorTools
{
    /// <summary>A continuous cultivated bed. Individual surfaces retain plot fertility feedback.</summary>
    public static class FarmSoilArt
    {
        const string Folder = "Assets/Art/Terrain";
        const int Segments = 32;

        public static bool UsesCultivatedSoil(GardenSkin skin)
        {
            return skin.SoilPrefab == null || skin.SoilPrefab.name == "PF_Plot";
        }

        public static void BuildBed(Transform parent, GardenSkin skin, int columns, int rows)
        {
            if (!UsesCultivatedSoil(skin)) return;
            Directory.CreateDirectory(Folder);
            var root = new GameObject("ContinuousEarthBed");
            root.transform.SetParent(parent, false);
            float width = columns * skin.PlotSpacing;
            float depth = rows * skin.PlotSpacing;
            var earth = Material("EarthEdge", new Color(.36f, .25f, .17f), false);
            Box(root.transform, "EarthFoundation", new Vector3(0f, .045f, 0f),
                new Vector3(width, .09f, depth), earth);
            // Only the outside of the whole field has a bank. Shared tile edges have no gaps.
            var bankVertices = new List<Vector3>();
            var bankTriangles = new List<int>();
            Vector3[] corners = {new Vector3(-width/2, 0, -depth/2), new Vector3(width/2, 0, -depth/2),
                new Vector3(width/2, 0, depth/2), new Vector3(-width/2, 0, depth/2)};
            for (int side = 0; side < 4; side++)
            {
                Vector3 a = corners[side], b = corners[(side + 1) % 4];
                Vector3 outward = new Vector3((b-a).z, 0, -(b-a).x).normalized;
                int samples = (side % 2 == 0 ? columns : rows) * Segments;
                for (int s = 0; s < samples; s++)
                {
                    Vector3 p = Vector3.Lerp(a,b,s/(float)samples), q = Vector3.Lerp(a,b,(s+1)/(float)samples);
                    int n = bankVertices.Count;
                    bankVertices.Add(new Vector3(p.x, SurfaceHeight(p.x,p.z,skin.PlotSpacing), p.z));
                    bankVertices.Add(new Vector3(q.x, SurfaceHeight(q.x,q.z,skin.PlotSpacing), q.z));
                    bankVertices.Add(q + outward * .065f + Vector3.up * .014f);
                    bankVertices.Add(p + outward * .065f + Vector3.up * .014f);
                    bankTriangles.AddRange(new[] {n,n+1,n+2,n,n+2,n+3});
                }
            }
            MeshObject(root.transform, "CrumbledOuterBank", "FieldBank", bankVertices, bankTriangles, earth);
            // Tiny pebbles and dry earth crumbs stay at the border, clear of crops and hitboxes.
            var stone = Material("Pebbles", new Color(.61f,.52f,.37f), false);
            var combines = new List<CombineInstance>();
            var sample = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var pebbleMesh = sample.GetComponent<MeshFilter>().sharedMesh;
            for (int i = 0; i < 50; i++)
            {
                float t = (i + .35f) / 50f * 4;
                int side = Mathf.FloorToInt(t);
                Vector3 p = Vector3.Lerp(corners[side], corners[(side+1)%4], t-side);
                p += new Vector3(Mathf.Sin(i*7.3f)*.025f,.12f,Mathf.Cos(i*5.2f)*.025f);
                float size = .026f + (i%4)*.009f;
                combines.Add(new CombineInstance {mesh=pebbleMesh,
                    transform=Matrix4x4.TRS(p,Quaternion.Euler(0,i*41,15),new Vector3(size*1.5f,size*.65f,size))});
            }
            Object.DestroyImmediate(sample);
            var mesh = new Mesh {name="FieldBorderPebbles"};
            mesh.CombineMeshes(combines.ToArray(), true, true);
            AssignMesh(root.transform,"ScatteredPebbles",SaveMesh(mesh,"FieldBorderPebbles"),stone);
        }

        public static Renderer BuildSurface(Transform parent, int plotId, Vector3 center, GardenSkin skin)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uv = new List<Vector2>();
            for (int z=0; z<=Segments; z++) for (int x=0; x<=Segments; x++)
            {
                float px=(x/(float)Segments-.5f)*skin.PlotSpacing;
                float pz=(z/(float)Segments-.5f)*skin.PlotSpacing;
                vertices.Add(new Vector3(px,SurfaceHeight(center.x+px,center.z+pz,skin.PlotSpacing),pz));
                uv.Add(new Vector2((center.x+px)*2f,(center.z+pz)*2f));
                if(x==Segments || z==Segments) continue;
                int n=z*(Segments+1)+x;
                triangles.AddRange(new[]{n,n+Segments+1,n+1,n+1,n+Segments+1,n+Segments+2});
            }
            var mesh = new Mesh {name="TilledSoil_"+plotId};
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.SetUVs(0,uv);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            // Identical normals at shared boundaries prevent visible tile seams in sunlight.
            var normals=mesh.normals;
            for(int i=0;i<vertices.Count;i++)
            {
                Vector3 p=vertices[i]+center;
                const float d=.002f;
                float dx=(SurfaceHeight(p.x+d,p.z,skin.PlotSpacing)-SurfaceHeight(p.x-d,p.z,skin.PlotSpacing))/(2*d);
                float dz=(SurfaceHeight(p.x,p.z+d,skin.PlotSpacing)-SurfaceHeight(p.x,p.z-d,skin.PlotSpacing))/(2*d);
                normals[i]=new Vector3(-dx,1,-dz).normalized;
            }
            mesh.normals=normals;
            return AssignMesh(parent,"CultivatedSoil",SaveMesh(mesh,"TilledSoil_"+plotId),
                Material("CultivatedSoil",GardenPalette.Soil,true));
        }

        public static float SurfaceHeight(float x,float z,float spacing)
        {
            float furrow=Mathf.Sin(z/spacing*Mathf.PI*8f + Mathf.Sin(x*2f)*.12f);
            return .151f + furrow*.014f + (Mathf.PerlinNoise(x*8.7f+50,z*8.7f+50)-.5f)*.006f;
        }

        public static GameObject BuildLockedBoundary(Transform parent,float spacing)
        {
            var root=new GameObject("ReservedPlotBoundary"); root.transform.SetParent(parent,false);
            var wood=Material("SurveyStake",new Color(.63f,.47f,.28f),false);
            var rope=Material("Twine",new Color(.79f,.73f,.53f),false);
            float z=spacing*.38f;
            for(int i=-1;i<=1;i+=2)
                Box(root.transform,"BoundaryStake",new Vector3(i*spacing*.36f,.24f,z),new Vector3(.034f,.20f,.035f),wood);
            Box(root.transform,"GardenTwine",new Vector3(0,.30f,z),new Vector3(spacing*.72f,.012f,.012f),rope);
            // A modest timber marker replaces the oversized cross on the planting surface.
            Box(root.transform,"ReservedMarker",new Vector3(spacing*.33f,.28f,z-.019f),new Vector3(.16f,.12f,.022f),wood);
            return root;
        }

        static Material Material(string name,Color color,bool textured)
        {
            Directory.CreateDirectory(Folder);
            string path=Folder+"/M_"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null)
            {
                material=new Material(ProjectSetup.LitTemplate()) {name=name};
                AssetDatabase.CreateAsset(material,path);
            }
            material.color=color; material.SetFloat("_Smoothness",.04f); material.enableInstancing=true;
            if(textured) material.mainTexture=SoilTexture();
            EditorUtility.SetDirty(material);
            return material;
        }

        static Texture2D SoilTexture()
        {
            string path=Folder+"/SoilGrain.png";
            var existing=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(existing!=null) return existing;
            const int size=256;
            var texture=new Texture2D(size,size,TextureFormat.RGB24,false);
            var colors=new Color[size*size];
            var random=new System.Random(6207);
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                float noise=(float)random.NextDouble();
                // Tileable periodic clods with fine grain; neutral modulation of fertility colour.
                float clod=Mathf.Sin(x*Mathf.PI/16)*Mathf.Cos(y*Mathf.PI/16);
                float v=.87f+noise*.16f+clod*.045f;
                if(noise<.025f) v=.59f;
                colors[y*size+x]=new Color(v,v,v);
            }
            texture.SetPixels(colors); texture.Apply();
            File.WriteAllBytes(path,texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode=TextureWrapMode.Repeat; importer.filterMode=FilterMode.Trilinear;
            importer.anisoLevel=4; importer.mipmapEnabled=true; importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void Box(Transform parent,string name,Vector3 position,Vector3 size,Material material)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name=name;
            Object.DestroyImmediate(obj.GetComponent<Collider>());
            obj.transform.SetParent(parent,false); obj.transform.localPosition=position; obj.transform.localScale=size;
            obj.GetComponent<Renderer>().sharedMaterial=material;
        }

        static Mesh SaveMesh(Mesh mesh,string name)
        {
            string path=Folder+"/"+name+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null) {AssetDatabase.CreateAsset(mesh,path);return mesh;}
            EditorUtility.CopySerialized(mesh,existing); Object.DestroyImmediate(mesh);return existing;
        }

        static Renderer AssignMesh(Transform parent,string name,Mesh mesh,Material material)
        {
            var obj=new GameObject(name);obj.transform.SetParent(parent,false);
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;return renderer;
        }

        static void MeshObject(Transform parent,string name,string asset,List<Vector3> vertices,List<int> triangles,Material material)
        {
            var mesh=new Mesh {name=asset};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            AssignMesh(parent,name,SaveMesh(mesh,asset),material);
        }
    }
}
