using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VuonNho.EditorTools
{
    /// <summary>
    /// Build Windows x64 dang thu muc. Ban playtest khong bat development build nen cong cu
    /// tua thoi gian khong xuat hien.
    /// </summary>
    public static class BuildTool
    {
        public const string OutputRoot = "Build";

        [MenuItem("Vườn Nhỏ/3. Build Windows (bản playtest)")]
        public static void BuildWindowsPlaytest()
        {
            Build(false);
        }

        [MenuItem("Vườn Nhỏ/4. Build Windows (bản dev, có tua thời gian)")]
        public static void BuildWindowsDev()
        {
            Build(true);
        }

        static void Build(bool development)
        {
            if (!File.Exists(SceneFactory.ScenePath))
                throw new Exception("Chua co scene " + SceneFactory.ScenePath +
                                               ". Chay menu 'Dung lai scene Garden' truoc.");

            string folderName = development ? "VuonNho-dev" : "VuonNho-playtest";
            string directory = Path.Combine(OutputRoot, folderName);
            Directory.CreateDirectory(directory);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { SceneFactory.ScenePath },
                locationPathName = Path.Combine(directory, "VuonNho.exe"),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = development
                    ? BuildOptions.Development
                    : BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
                throw new Exception("Build that bai: " + summary.result +
                                               ", " + summary.totalErrors + " loi.");

            Debug.Log("[VuonNho] Build xong: " + options.locationPathName +
                      " (" + (summary.totalSize / (1024 * 1024)) + " MB, " +
                      Math.Round(summary.totalTime.TotalSeconds) + " giay)");
        }
    }
}
