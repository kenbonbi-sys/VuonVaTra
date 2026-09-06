using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using VuonNho.Core;
using VuonNho.Infrastructure;

namespace VuonNho.EditorTools
{
    /// <summary>Builds the regular Garden scene under a separate product identity for disposable HUD QA.</summary>
    public static class HudReviewBuild
    {
        public const string ReviewProductName = "VuonNho-HudReview";
        public const string ExecutablePath = "Build/HudReview/VuonNho.exe";
        public const string BuildReportPath = "Docs/Art/hud-review-build.json";
        public const string SeedReportPath = "Docs/Art/hud-review-seed.json";
        const long ReviewCropDurationMs = 600000;

        [Serializable]
        sealed class ReviewReport
        {
            public string utc;
            public string company;
            public string product = ReviewProductName;
            public string executable;
            public string dataDirectory;
            public string saveFile;
            public string result;
            public string note;
            public long coins;
            public int plots;
            public int growingPlots;
            public int crops;
            public int decorations;
            public string checklistArguments = "-vuonnho-qa <absolute-report.md> -screen-fullscreen 0 -screen-width 1366 -screen-height 768";
            public List<string> decorationPlacements = new List<string>();
        }

        [MenuItem("Vườn Nhỏ/QA/Build HUD review và tạo save riêng")]
        public static void BuildAndSeed()
        {
            Build();
            PrepareReviewSave();
        }

        /// <summary>Batch: -executeMethod VuonNho.EditorTools.HudReviewBuild.Build.</summary>
        public static void Build()
        {
            if (!File.Exists(SceneFactory.ScenePath))
                throw new InvalidOperationException("Build the Garden scene before creating its HUD review player.");
            string previousProduct = PlayerSettings.productName;
            string company = PlayerSettings.companyName;
            string dataDirectory = GetReviewDataDirectory();
            var report = NewReport(company, dataDirectory);
            report.note = "Regular Garden scene; distinct product isolates both persistent files and Windows PlayerPrefs. Project product name restored in finally.";
            try
            {
                PlayerSettings.productName = ReviewProductName;
                Directory.CreateDirectory(Path.GetDirectoryName(ExecutablePath));
                var build = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { SceneFactory.ScenePath },
                    locationPathName = ExecutablePath,
                    target = BuildTarget.StandaloneWindows64,
                    targetGroup = BuildTargetGroup.Standalone,
                    options = BuildOptions.None
                });
                report.result = build.summary.result.ToString();
                if (build.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("HUD review build failed: " + build.summary.totalErrors + " errors.");
                Debug.Log("[HudReview] Build ready: " + Path.GetFullPath(ExecutablePath) + "; QA-only data: " + dataDirectory);
            }
            catch (Exception error) { report.result = error.Message; throw; }
            finally
            {
                PlayerSettings.productName = previousProduct;
                PlayerSettings.companyName = company;
                AssetDatabase.SaveAssets();
                WriteReport(BuildReportPath, report);
            }
        }

        /// <summary>Overwrites only the disposable HudReview product save. Never loads the production save.</summary>
        [MenuItem("Vườn Nhỏ/QA/Tạo lại save HUD review riêng")]
        public static void PrepareReviewSave()
        {
            string directory = GetReviewDataDirectory();
            var report = NewReport(PlayerSettings.companyName, directory);
            report.note = "Art fixture: ten growing plots use ten-minute stored cycle durations for stable visual inspection; two plots stay empty. Production rules are unchanged. QaChecklist resets this isolated save and tests normal catalog timings. Speed I upgrades are affordable; Speed II prerequisites remain locked. Reseed before each screenshot session.";
            try
            {
                var catalog = DefaultContent.Create();
                var clock = new ReviewClock(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                var memory = new ReviewMemoryRepository();
                var session = new GameSession(catalog, clock, memory, null, "hud-review-fixture");
                if (session.Initialize() != LoadOutcome.NewGame)
                    throw new InvalidOperationException("Review fixture must start in an empty in-memory repository.");

                // The fixture supplies a budget; all progression/placement then uses the actual Core commands.
                session.State.Coins = 20000;
                foreach (string upgrade in new[]
                {
                    DefaultContent.UpgradeRobot, DefaultContent.UpgradeChamomile,
                    DefaultContent.UpgradeExpand8, DefaultContent.UpgradeStrawberry,
                    DefaultContent.UpgradeExpand12, DefaultContent.UpgradeLemongrass,
                    DefaultContent.UpgradeJasmine
                }) Require(session.Purchase(upgrade), "purchase " + upgrade);

                // Coordinates are explicitly rechecked against current Core keep-outs and footprints.
                Place(session, DefaultDecorations.StonePath, -1800, -3200, report);
                Place(session, DefaultDecorations.Planter, 0, -3400, report);
                Place(session, DefaultDecorations.Lantern, 1800, -3400, report);
                Place(session, DefaultDecorations.Bench, 3800, 1000, report);
                Place(session, DefaultDecorations.Signboard, 2800, 4000, report);

                for (int i = 0; i < 10; i++)
                    Require(session.Plant(i, catalog.Crops[i % catalog.Crops.Count].Id), "plant review plot " + i);
                Require(session.SelectRecipe(DefaultContent.RecipeJasmine), "select jasmine tea");

                // Persisted durations are authoritative in Core, including across balance-version changes.
                // A long snapshot keeps the art review readable without adding any runtime debug rule.
                var state = session.State;
                state.SimulationTimeMs = ReviewCropDurationMs;
                state.CheckpointUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                state.Coins = 2500;
                state.PendingOfflineSummary = null;
                for (int i = 0; i < catalog.Crops.Count; i++) state.Inventory[catalog.Crops[i].Id] = 16 + i * 7;
                for (int i = 0; i < 10; i++)
                {
                    var plot = state.Plot(i);
                    // Mature foliage for every species, followed by seedlings and later growth.
                    float progress = i < 5 ? .55f + i * .06f : (i % 2 == 0 ? .10f : .32f);
                    plot.Phase = PlotPhase.Growing;
                    plot.StartAtMs = state.SimulationTimeMs - (long)(ReviewCropDurationMs * progress);
                    plot.FinishAtMs = plot.StartAtMs + ReviewCropDurationMs;
                }
                var recipe = catalog.Recipe(DefaultContent.RecipeJasmine);
                state.Inventory[recipe.InputCropId] -= recipe.InputCount;
                state.Machine.BatchRunning = true;
                state.Machine.BatchRecipeId = recipe.Id;
                state.Machine.BatchStartAtMs = state.SimulationTimeMs - recipe.BaseBrewMs / 3;
                state.Machine.BatchFinishAtMs = state.Machine.BatchStartAtMs + recipe.BaseBrewMs;
                state.Machine.BatchOutputCoins = recipe.OutputCoins;

                // Roundtrip and independently validate final placements before touching even the QA folder.
                string json = SaveSerializer.Write(new SaveSnapshot
                {
                    BalanceVersion = catalog.Balance.Version, BuildId = "hud-review-fixture", State = state
                }, catalog, true);
                var roundtrip = SaveSerializer.Read(json, catalog).State;
                for (int i = 0; i < roundtrip.Decorations.Count; i++)
                {
                    var item = roundtrip.Decorations[i];
                    if (!session.CanPlaceDecoration(item.DefinitionId, item.XMm, item.ZMm, i, out var reason))
                        throw new InvalidOperationException("Invalid final review decoration: " + item.DefinitionId + ": " + reason);
                }
                var repository = new FileSaveRepository(directory);
                repository.Save(json);
                report.result = "Succeeded";
                report.coins = roundtrip.Coins;
                report.plots = roundtrip.Plots.Count;
                report.growingPlots = 10;
                report.crops = catalog.Crops.Count;
                report.decorations = roundtrip.Decorations.Count;
                Debug.Log("[HudReview] Isolated QA save ready: " + repository.MainPath);
            }
            catch (Exception error) { report.result = error.Message; throw; }
            finally { WriteReport(SeedReportPath, report); }
        }

        public static string GetReviewDataDirectory()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                throw new PlatformNotSupportedException("HUD review save preparation targets Windows LocalLow.");
            string company = PlayerSettings.companyName;
            if (string.IsNullOrWhiteSpace(company) || company == "." || company == ".." ||
                company.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new InvalidOperationException("Company name must be a single valid directory segment.");
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appData = Directory.GetParent(local);
            if (appData == null) throw new InvalidOperationException("Cannot resolve Windows AppData directory.");
            string companyDirectory = Path.GetFullPath(Path.Combine(appData.FullName, "LocalLow", company));
            string result = Path.GetFullPath(Path.Combine(companyDirectory, ReviewProductName));
            if (!string.Equals(Path.GetFileName(result), ReviewProductName, StringComparison.Ordinal) ||
                !string.Equals(Path.GetDirectoryName(result), companyDirectory, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Review data directory escaped its dedicated product path.");
            if (string.Equals(Path.GetFullPath(Application.persistentDataPath).TrimEnd(Path.DirectorySeparatorChar),
                    result.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Run save preparation from the normal project identity, not the QA product identity.");
            foreach (string directory in new[] { companyDirectory, result })
                if (Directory.Exists(directory) && (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("Review data path must not redirect through a junction: " + directory);
            return result;
        }

        static void Place(GameSession session, string id, int xMm, int zMm, ReviewReport report)
        {
            if (!session.CanPlaceDecoration(id, xMm, zMm, -1, out var reason))
                throw new InvalidOperationException("Review placement failed for " + id + ": " + reason);
            Require(session.PlaceDecoration(id, xMm, zMm, 0), "place " + id);
            report.decorationPlacements.Add(id + " at (" + xMm + ", " + zMm + ") mm, yaw 0");
        }

        static void Require(CommandResult result, string operation)
        {
            if (!result.Success) throw new InvalidOperationException(operation + ": " + result.FailureReason);
        }

        static ReviewReport NewReport(string company, string directory)
        {
            return new ReviewReport
            {
                utc = DateTime.UtcNow.ToString("O"), company = company,
                executable = Path.GetFullPath(ExecutablePath), dataDirectory = directory,
                saveFile = Path.Combine(directory, FileSaveRepository.MainFileName)
            };
        }

        static void WriteReport(string path, ReviewReport report)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
        }

        sealed class ReviewClock : IClock
        {
            public long UtcNowMs { get; private set; }
            public long MonotonicMs { get { return 0; } }
            public ReviewClock(long utcMs) { UtcNowMs = utcMs; }
        }

        sealed class ReviewMemoryRepository : ISaveRepository
        {
            string json;
            public bool HasSave { get { return json != null; } }
            public IList<SaveCandidate> LoadCandidates()
            {
                return new List<SaveCandidate> { new SaveCandidate { Name = "review-memory", Json = json } };
            }
            public void Save(string value) { json = value; }
            public void DeleteAll() { json = null; }
        }
    }
}
