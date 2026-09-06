using System;
using System.Collections.Generic;

namespace VuonNho.Core
{
    /// <summary>File save doc duoc nhung khong dung voi ung dung: cau truc hong, id la, so am.</summary>
    public sealed class SaveCorruptException : Exception
    {
        public SaveCorruptException(string message) : base(message) { }
        public SaveCorruptException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>Save co schema moi hon ung dung. Khong duoc am tham reset.</summary>
    public sealed class SaveIncompatibleException : Exception
    {
        public int SaveSchemaVersion;
        public int AppSchemaVersion;

        public SaveIncompatibleException(int saveSchemaVersion, int appSchemaVersion)
            : base("Save co schema " + saveSchemaVersion + ", ung dung chi hieu den " + appSchemaVersion + ".")
        {
            SaveSchemaVersion = saveSchemaVersion;
            AppSchemaVersion = appSchemaVersion;
        }
    }

    public sealed class SaveSnapshot
    {
        /// <summary>2 = them danh sach trang tri cua pha 2.</summary>
        public const int CurrentSchemaVersion = 2;

        public int SchemaVersion = CurrentSchemaVersion;
        public string BalanceVersion;
        public string BuildId;
        public GameState State;
    }

    /// <summary>
    /// DTO co phien ban, JSON. Dung stable ID thay cho ten hien thi hoac vi tri trong mang.
    /// Inventory luu bang danh sach entry de khong phu thuoc serializer ho tro dictionary.
    /// </summary>
    public static class SaveSerializer
    {
        public static string Write(SaveSnapshot snapshot, ContentCatalog catalog, bool pretty)
        {
            if (snapshot == null || snapshot.State == null) throw new ArgumentNullException("snapshot");
            var state = snapshot.State;
            var root = JsonValue.NewObject();

            root.Set("schemaVersion", snapshot.SchemaVersion);
            root.Set("balanceVersion", snapshot.BalanceVersion);
            root.Set("buildId", snapshot.BuildId);
            root.Set("saveRevision", state.SaveRevision);

            root.Set("simulationTimeMs", state.SimulationTimeMs);
            root.Set("checkpointUtcMs", state.CheckpointUtcMs);
            root.Set("coins", state.Coins);
            root.Set("tutorialStep", state.TutorialStep);
            root.Set("robotUnlocked", state.RobotUnlocked);

            // Thu tu theo catalog de hai lan ghi cung state cho cung chuoi byte.
            var inventory = JsonValue.NewArray();
            for (int i = 0; i < catalog.Crops.Count; i++)
            {
                string cropId = catalog.Crops[i].Id;
                inventory.Add(JsonValue.NewObject()
                    .Set("cropId", cropId)
                    .Set("amount", state.InventoryOf(cropId)));
            }
            root.Set("inventory", inventory);

            var plots = JsonValue.NewArray();
            for (int i = 0; i < state.Plots.Count; i++)
            {
                var plot = state.Plots[i];
                plots.Add(JsonValue.NewObject()
                    .Set("plotId", plot.PlotId)
                    .Set("unlocked", plot.Unlocked)
                    .Set("phase", (int)plot.Phase)
                    .Set("currentCropId", plot.CurrentCropId)
                    .Set("nextCropId", plot.NextCropId)
                    .Set("startAtMs", plot.StartAtMs)
                    .Set("finishAtMs", plot.FinishAtMs)
                    .Set("pendingYield", plot.PendingYield));
            }
            root.Set("plots", plots);

            root.Set("machine", JsonValue.NewObject()
                .Set("selectedRecipeId", state.Machine.SelectedRecipeId)
                .Set("batchRunning", state.Machine.BatchRunning)
                .Set("batchRecipeId", state.Machine.BatchRecipeId)
                .Set("batchStartAtMs", state.Machine.BatchStartAtMs)
                .Set("batchFinishAtMs", state.Machine.BatchFinishAtMs)
                .Set("batchOutputCoins", state.Machine.BatchOutputCoins));

            root.Set("unlockedCropIds", IdArray(catalog.Crops, state.UnlockedCropIds));
            root.Set("unlockedRecipeIds", RecipeIdArray(catalog.Recipes, state.UnlockedRecipeIds));

            var upgrades = JsonValue.NewArray();
            for (int i = 0; i < catalog.Upgrades.Count; i++)
            {
                string upgradeId = catalog.Upgrades[i].Id;
                upgrades.Add(JsonValue.NewObject()
                    .Set("upgradeId", upgradeId)
                    .Set("level", state.UpgradeLevel(upgradeId)));
            }
            root.Set("upgradeLevels", upgrades);

            var decorations = JsonValue.NewArray();
            for (int i = 0; i < state.Decorations.Count; i++)
            {
                var item = state.Decorations[i];
                decorations.Add(JsonValue.NewObject()
                    .Set("definitionId", item.DefinitionId)
                    .Set("xMm", item.XMm)
                    .Set("zMm", item.ZMm)
                    .Set("rotationDeg", item.RotationDeg));
            }
            root.Set("decorations", decorations);

            if (state.PendingOfflineSummary == null)
            {
                root.Set("pendingOfflineSummary", JsonValue.Null());
            }
            else
            {
                var summary = state.PendingOfflineSummary;
                var items = JsonValue.NewArray();
                for (int i = 0; i < summary.ItemsGained.Count; i++)
                    items.Add(JsonValue.NewObject()
                        .Set("cropId", summary.ItemsGained[i].CropId)
                        .Set("amount", summary.ItemsGained[i].Amount));

                root.Set("pendingOfflineSummary", JsonValue.NewObject()
                    .Set("id", summary.Id)
                    .Set("elapsedMs", summary.ElapsedMs)
                    .Set("coinsGained", summary.CoinsGained)
                    .Set("machineWaiting", summary.MachineWaiting)
                    .Set("seen", summary.Seen)
                    .Set("itemsGained", items));
            }

            return root.ToJson(pretty);
        }

        static JsonValue IdArray(List<CropDefinition> ordered, HashSet<string> selected)
        {
            var array = JsonValue.NewArray();
            for (int i = 0; i < ordered.Count; i++)
                if (selected.Contains(ordered[i].Id)) array.Add(JsonValue.Of(ordered[i].Id));
            return array;
        }

        static JsonValue RecipeIdArray(List<RecipeDefinition> ordered, HashSet<string> selected)
        {
            var array = JsonValue.NewArray();
            for (int i = 0; i < ordered.Count; i++)
                if (selected.Contains(ordered[i].Id)) array.Add(JsonValue.Of(ordered[i].Id));
            return array;
        }

        public static SaveSnapshot Read(string json, ContentCatalog catalog)
        {
            JsonValue root;
            try
            {
                root = JsonValue.Parse(json);
            }
            catch (JsonParseException error)
            {
                throw new SaveCorruptException("Khong doc duoc JSON cua save.", error);
            }

            if (root.Kind != JsonKind.Object) throw new SaveCorruptException("Save khong phai object.");

            int schemaVersion = root.GetInt("schemaVersion", -1);
            if (schemaVersion < 1) throw new SaveCorruptException("schemaVersion khong hop le.");
            if (schemaVersion > SaveSnapshot.CurrentSchemaVersion)
                throw new SaveIncompatibleException(schemaVersion, SaveSnapshot.CurrentSchemaVersion);
            root = Migrate(root, schemaVersion);

            var snapshot = new SaveSnapshot
            {
                SchemaVersion = SaveSnapshot.CurrentSchemaVersion,
                BalanceVersion = root.GetStringOrNull("balanceVersion"),
                BuildId = root.GetStringOrNull("buildId")
            };

            var state = new GameState
            {
                SaveRevision = root.GetLong("saveRevision", 1),
                SimulationTimeMs = root.GetLong("simulationTimeMs", -1),
                CheckpointUtcMs = root.GetLong("checkpointUtcMs", -1),
                Coins = root.GetLong("coins", -1),
                TutorialStep = root.GetInt("tutorialStep", 0),
                RobotUnlocked = root.GetBool("robotUnlocked", false)
            };

            if (state.SimulationTimeMs < 0) throw new SaveCorruptException("simulationTimeMs am hoac thieu.");
            if (state.CheckpointUtcMs < 0) throw new SaveCorruptException("checkpointUtcMs am hoac thieu.");
            if (state.Coins < 0) throw new SaveCorruptException("coins am.");
            if (state.TutorialStep < 0) throw new SaveCorruptException("tutorialStep am.");

            foreach (var crop in catalog.Crops) state.Inventory[crop.Id] = 0;

            var inventory = root.Require("inventory");
            for (int i = 0; i < inventory.Count; i++)
            {
                var entry = inventory.Items[i];
                string cropId = entry.GetStringOrNull("cropId");
                long amount = entry.GetLong("amount", -1);
                CropDefinition crop;
                if (!catalog.TryGetCrop(cropId, out crop))
                    throw new SaveCorruptException("Kho co id cay khong ton tai: " + cropId);
                if (amount < 0) throw new SaveCorruptException("So luong kho am: " + cropId);
                state.Inventory[cropId] = amount;
            }

            var plots = root.Require("plots");
            if (plots.Count != catalog.Balance.MaximumPlots)
                throw new SaveCorruptException("So o dat trong save khong khop cau hinh.");

            var seenPlotIds = new HashSet<int>();
            for (int i = 0; i < plots.Count; i++)
            {
                var entry = plots.Items[i];
                var plot = new PlotState
                {
                    PlotId = entry.GetInt("plotId", -1),
                    Unlocked = entry.GetBool("unlocked", false),
                    CurrentCropId = entry.GetStringOrNull("currentCropId"),
                    NextCropId = entry.GetStringOrNull("nextCropId"),
                    StartAtMs = entry.GetLong("startAtMs", -1),
                    FinishAtMs = entry.GetLong("finishAtMs", -1),
                    PendingYield = entry.GetInt("pendingYield", -1)
                };

                int phase = entry.GetInt("phase", -1);
                if (phase < (int)PlotPhase.Locked || phase > (int)PlotPhase.Ready)
                    throw new SaveCorruptException("Trang thai o dat khong hop le.");
                plot.Phase = (PlotPhase)phase;

                if (plot.PlotId < 0 || plot.PlotId >= catalog.Balance.MaximumPlots || !seenPlotIds.Add(plot.PlotId))
                    throw new SaveCorruptException("plotId khong hop le hoac trung.");
                if (plot.StartAtMs < 0 || plot.FinishAtMs < 0 || plot.PendingYield < 0)
                    throw new SaveCorruptException("So am tren o dat " + plot.PlotId + ".");

                CropDefinition unused;
                if (plot.CurrentCropId != null && !catalog.TryGetCrop(plot.CurrentCropId, out unused))
                    throw new SaveCorruptException("O dat tro toi cay khong ton tai: " + plot.CurrentCropId);
                if (plot.NextCropId != null && !catalog.TryGetCrop(plot.NextCropId, out unused))
                    throw new SaveCorruptException("O dat tro toi cay khong ton tai: " + plot.NextCropId);
                if ((plot.Phase == PlotPhase.Growing || plot.Phase == PlotPhase.Ready) && plot.CurrentCropId == null)
                    throw new SaveCorruptException("O dat dang co cay nhung thieu currentCropId.");

                state.Plots.Add(plot);
            }
            state.Plots.Sort((a, b) => a.PlotId.CompareTo(b.PlotId));

            var machine = root.Require("machine");
            state.Machine = new MachineState
            {
                SelectedRecipeId = machine.GetStringOrNull("selectedRecipeId"),
                BatchRunning = machine.GetBool("batchRunning", false),
                BatchRecipeId = machine.GetStringOrNull("batchRecipeId"),
                BatchStartAtMs = machine.GetLong("batchStartAtMs", -1),
                BatchFinishAtMs = machine.GetLong("batchFinishAtMs", -1),
                BatchOutputCoins = machine.GetLong("batchOutputCoins", -1)
            };
            if (state.Machine.BatchStartAtMs < 0 || state.Machine.BatchFinishAtMs < 0 ||
                state.Machine.BatchOutputCoins < 0)
                throw new SaveCorruptException("So am trong trang thai may.");

            RecipeDefinition recipe;
            if (state.Machine.SelectedRecipeId != null &&
                !catalog.TryGetRecipe(state.Machine.SelectedRecipeId, out recipe))
                throw new SaveCorruptException("May tro toi cong thuc khong ton tai.");
            if (state.Machine.BatchRunning)
            {
                if (!catalog.TryGetRecipe(state.Machine.BatchRecipeId, out recipe))
                    throw new SaveCorruptException("Me dang chay tro toi cong thuc khong ton tai.");
                if (state.Machine.BatchFinishAtMs < state.Machine.BatchStartAtMs)
                    throw new SaveCorruptException("Me dang chay co deadline nguoc.");
            }

            ReadIdSet(root.Require("unlockedCropIds"), state.UnlockedCropIds, catalog, true);
            ReadIdSet(root.Require("unlockedRecipeIds"), state.UnlockedRecipeIds, catalog, false);

            foreach (var upgrade in catalog.Upgrades) state.UpgradeLevels[upgrade.Id] = 0;
            var upgradeLevels = root.Require("upgradeLevels");
            for (int i = 0; i < upgradeLevels.Count; i++)
            {
                var entry = upgradeLevels.Items[i];
                string upgradeId = entry.GetStringOrNull("upgradeId");
                int level = entry.GetInt("level", -1);
                UpgradeDefinition upgrade;
                if (!catalog.TryGetUpgrade(upgradeId, out upgrade))
                    throw new SaveCorruptException("Nang cap khong ton tai trong catalog: " + upgradeId);
                if (level < 0) throw new SaveCorruptException("Cap nang cap am: " + upgradeId);
                state.UpgradeLevels[upgradeId] = level;
            }

            var decorations = root.Get("decorations");
            if (decorations != null && decorations.Kind == JsonKind.Array)
            {
                int halfExtent = catalog.Balance.GardenHalfExtentMm;
                for (int i = 0; i < decorations.Count; i++)
                {
                    var entry = decorations.Items[i];
                    string definitionId = entry.GetStringOrNull("definitionId");
                    DecorationDefinition definition;
                    if (!catalog.TryGetDecoration(definitionId, out definition))
                        throw new SaveCorruptException("Trang tri co id khong ton tai: " + definitionId);

                    var placed = new PlacedDecoration
                    {
                        DefinitionId = definitionId,
                        XMm = entry.GetInt("xMm", 0),
                        ZMm = entry.GetInt("zMm", 0),
                        RotationDeg = entry.GetInt("rotationDeg", 0)
                    };
                    if (Math.Abs(placed.XMm) > halfExtent || Math.Abs(placed.ZMm) > halfExtent)
                        throw new SaveCorruptException("Trang tri nam ngoai khu vuon: " + definitionId);
                    state.Decorations.Add(placed);
                }
            }

            var summaryValue = root.Get("pendingOfflineSummary");
            if (summaryValue != null && summaryValue.Kind == JsonKind.Object)
            {
                var summary = new OfflineSummary
                {
                    Id = summaryValue.GetStringOrNull("id"),
                    ElapsedMs = summaryValue.GetLong("elapsedMs", 0),
                    CoinsGained = summaryValue.GetLong("coinsGained", 0),
                    MachineWaiting = summaryValue.GetBool("machineWaiting", false),
                    Seen = summaryValue.GetBool("seen", false)
                };
                var items = summaryValue.Get("itemsGained");
                if (items != null && items.Kind == JsonKind.Array)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        var entry = items.Items[i];
                        summary.ItemsGained.Add(new InventoryDelta
                        {
                            CropId = entry.GetStringOrNull("cropId"),
                            Amount = entry.GetLong("amount", 0)
                        });
                    }
                }
                state.PendingOfflineSummary = summary;
            }

            snapshot.State = state;
            return snapshot;
        }

        static void ReadIdSet(JsonValue array, HashSet<string> target, ContentCatalog catalog, bool crops)
        {
            for (int i = 0; i < array.Count; i++)
            {
                string id = array.Items[i].AsStringOrNull();
                if (crops)
                {
                    CropDefinition crop;
                    if (!catalog.TryGetCrop(id, out crop))
                        throw new SaveCorruptException("Danh sach mo khoa co id cay la: " + id);
                }
                else
                {
                    RecipeDefinition recipe;
                    if (!catalog.TryGetRecipe(id, out recipe))
                        throw new SaveCorruptException("Danh sach mo khoa co id cong thuc la: " + id);
                }
                target.Add(id);
            }
        }

        /// <summary>
        /// Cho cac phien ban schema cu hon. Hien tai chi co v1 nen khong co buoc nao;
        /// khi doi schema, them buoc migration dung phien ban da ton tai va viet test cho no.
        /// </summary>
        static JsonValue Migrate(JsonValue root, int fromSchemaVersion)
        {
            if (fromSchemaVersion == SaveSnapshot.CurrentSchemaVersion) return root;

            // 1 -> 2: them pha trang tri. Save cu khong co truong nao bi doi y nghia,
            // chi thieu danh sach trang tri, nen bo sung mot danh sach rong la du.
            if (fromSchemaVersion == 1)
            {
                if (!root.Has("decorations")) root.Set("decorations", JsonValue.NewArray());
                return root;
            }

            throw new SaveCorruptException("Khong co buoc migration tu schema " + fromSchemaVersion + ".");
        }
    }
}
