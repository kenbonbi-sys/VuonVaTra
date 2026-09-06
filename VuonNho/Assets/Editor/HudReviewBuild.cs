using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VuonNho.EditorTools
{
    /// <summary>
    /// Builds the real Garden HUD under a distinct product identity, so visual QA cannot read or
    /// update the player's normal save in Application.persistentDataPath.
    /// </summary>
    public static class HudReviewBuild
    {
        public const string OutputPath = "Build/HudReview/VuonNho-HudReview.exe";
        const string ReviewProductName = "VuonNho-HudReview";

        [MenuItem("Vườn Nhỏ/Art/Build bản kiểm tra HUD (save riêng)")]
        public static void Build()
        {
            string previousProductName = PlayerSettings.productName;
            try
            {
                PlayerSettings.productName = ReviewProductName;
                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { SceneFactory.ScenePath },
                    locationPathName = OutputPath,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("HUD review build failed: " + report.summary.result);
                Debug.Log("[HUD QA] Build xong với save riêng: " + Path.GetFullPath(OutputPath));
            }
            finally
            {
                PlayerSettings.productName = previousProductName;
                AssetDatabase.SaveAssets();
            }
        }
    }
}
