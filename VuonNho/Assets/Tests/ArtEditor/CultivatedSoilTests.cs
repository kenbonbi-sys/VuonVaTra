using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VuonNho.EditorTools;
using VuonNho.Views;

namespace VuonNho.Tests.ArtEditor
{
    public sealed class CultivatedSoilTests
    {
        [Test]
        public void AdjacentSoilMeshesMeetWithoutGapsOrLightingSeams()
        {
            var skin=AssetDatabase.LoadAssetAtPath<GardenSkin>(SceneFactory.SkinPath);
            Assert.That(skin,Is.Not.Null);
            const int stride=33;
            for(int row=0;row<3;row++) for(int column=0;column<4;column++)
            {
                int id=row*4+column;
                var mesh=Load(id);
                Assert.That(mesh.bounds.size.x,Is.EqualTo(skin.PlotSpacing).Within(.0001f));
                Assert.That(mesh.bounds.size.z,Is.EqualTo(skin.PlotSpacing).Within(.0001f));
                for(int edge=0;edge<stride;edge++)
                {
                    if(column<3) CheckEdge(mesh,edge*stride+32,Load(id+1),edge*stride,
                        new Vector3(skin.PlotSpacing,0,0));
                    if(row<2) CheckEdge(mesh,32*stride+edge,Load(id+4),edge,
                        new Vector3(0,0,skin.PlotSpacing));
                }
            }
        }

        static Mesh Load(int id)
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Terrain/TilledSoil_"+id+".asset");
            Assert.That(mesh,Is.Not.Null,"Rebuild the detailed Garden scene before this art check.");
            return mesh;
        }

        static void CheckEdge(Mesh a,int ai,Mesh b,int bi,Vector3 offset)
        {
            Assert.That(Vector3.Distance(a.vertices[ai],b.vertices[bi]+offset),Is.LessThan(.0001f),
                a.name+" / "+b.name+" must share surface positions.");
            Assert.That(Vector3.Distance(a.normals[ai],b.normals[bi]),Is.LessThan(.002f),
                a.name+" / "+b.name+" must share surface lighting.");
        }
    }
}
