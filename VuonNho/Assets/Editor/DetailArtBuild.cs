using UnityEditor;

namespace VuonNho.EditorTools
{
    /// <summary>One reproducible import/build path for the detailed farm assets.</summary>
    public static class DetailArtBuild
    {
        [MenuItem("Vườn Nhỏ/Art/Import chi tiết và build kiểm tra")]
        public static void BuildReview()
        {
            GardenArtImporter.ImportAndBuild();
            HudReviewBuild.Build();
        }
    }
}
