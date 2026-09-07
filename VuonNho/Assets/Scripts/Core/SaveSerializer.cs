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
        /// <summary>
        /// 5 = canh tac: do phi, co dai, sau benh va so thu tu vu cua tung o.
        /// 6 = von va no, ho so phap ly, bon nut can lua, phan nhanh kinh doanh, dong tien.
        /// </summary>
        public const int CurrentSchemaVersion = 6;

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
            root.Set("pestSeed", state.PestSeed);
            root.Set("robotXMm", state.RobotXMm);
            root.Set("robotZMm", state.RobotZMm);
            root.Set("robotTargetPlotId", state.RobotTargetPlotId);
            root.Set("robotReadyAtMs", state.RobotReadyAtMs);
            root.Set("hiredWorkers", state.HiredWorkers);
            root.Set("staffedWorkers", state.StaffedWorkers);
            root.Set("nextPayrollAtMs", state.NextPayrollAtMs);

            // Thu tu theo catalog de hai lan ghi cung state cho cung chuoi byte. Danh sach nay
            // gom ca hang trung gian cua day chuyen, khong chi la tuoi.
            var inventory = JsonValue.NewArray();
            for (int i = 0; i < catalog.Items.Count; i++)
            {
                string itemId = catalog.Items[i];
                inventory.Add(JsonValue.NewObject()
                    .Set("itemId", itemId)
                    .Set("amount", state.InventoryOf(itemId)));
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
                    .Set("pendingYield", plot.PendingYield)
                    .Set("fertility", plot.Fertility)
                    .Set("weeds", plot.Weeds)
                    .Set("weedsUpdatedAtMs", plot.WeedsUpdatedAtMs)
                    .Set("fertilityUpdatedAtMs", plot.FertilityUpdatedAtMs)
                    .Set("cycleIndex", plot.CycleIndex)
                    .Set("pestPending", plot.PestPending)
                    .Set("pestActive", plot.PestActive)
                    .Set("pestAtMs", plot.PestAtMs)
                    .Set("leafhopper", plot.Leafhopper));
            }
            root.Set("plots", plots);

            root.Set("machine", JsonValue.NewObject()
                .Set("selectedRecipeId", state.Machine.SelectedRecipeId)
                .Set("batchRunning", state.Machine.BatchRunning)
                .Set("batchRecipeId", state.Machine.BatchRecipeId)
                .Set("batchFromPacked", state.Machine.BatchFromPacked)
                .Set("batchStartAtMs", state.Machine.BatchStartAtMs)
                .Set("batchFinishAtMs", state.Machine.BatchFinishAtMs)
                .Set("batchOutputCoins", state.Machine.BatchOutputCoins));

            // Thu tu theo catalog, khong theo thu tu trong state: mot may them vao giua day chuyen
            // sau nay se khong lam xao tron ca file.
            var stations = JsonValue.NewArray();
            for (int i = 0; i < catalog.Stages.Count; i++)
            {
                var station = state.Station(catalog.Stages[i].Id);
                if (station == null) continue;
                stations.Add(JsonValue.NewObject()
                    .Set("stageId", station.StageId)
                    .Set("owned", station.Owned)
                    .Set("running", station.Running)
                    .Set("batchCropId", station.BatchCropId)
                    .Set("batchOutput", station.BatchOutput)
                    .Set("batchStartAtMs", station.BatchStartAtMs)
                    .Set("batchFinishAtMs", station.BatchFinishAtMs));
            }
            root.Set("stations", stations);

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

            // --- bon he cua ban mo phong khoi nghiep tra
            var loan = state.Loan;
            root.Set("loan", JsonValue.NewObject()
                .Set("kind", (int)loan.Kind)
                .Set("principalCoins", loan.PrincipalCoins)
                .Set("remainingPrincipalCoins", loan.RemainingPrincipalCoins)
                .Set("annualRateBps", loan.AnnualRateBps)
                .Set("termMonths", loan.TermMonths)
                .Set("monthsPaid", loan.MonthsPaid)
                .Set("nextDueAtMs", loan.NextDueAtMs)
                .Set("consecutiveShortfalls", loan.ConsecutiveShortfalls)
                .Set("overdueCoins", loan.OverdueCoins)
                .Set("interestPaidCoins", loan.InterestPaidCoins)
                .Set("sealed", loan.Sealed)
                .Set("sealedAtMs", loan.SealedAtMs));

            var compliance = state.Compliance;
            root.Set("compliance", JsonValue.NewObject()
                .Set("entity", (int)compliance.Entity)
                .Set("pendingEntity", (int)compliance.PendingEntity)
                .Set("entityReadyAtMs", compliance.EntityReadyAtMs)
                .Set("foodSafetyCertified", compliance.FoodSafetyCertified)
                .Set("foodSafetyPending", compliance.FoodSafetyPending)
                .Set("foodSafetyReadyAtMs", compliance.FoodSafetyReadyAtMs)
                .Set("protectiveGear", compliance.ProtectiveGear)
                .Set("licenceWarned", compliance.LicenceWarned)
                .Set("nextInspectionAtMs", compliance.NextInspectionAtMs)
                .Set("inspectionCount", compliance.InspectionCount)
                .Set("suspendedUntilMs", compliance.SuspendedUntilMs)
                .Set("totalFinesCoins", compliance.TotalFinesCoins)
                .Set("totalTaxCoins", compliance.TotalTaxCoins)
                .Set("lastInspectionIndex", compliance.LastInspectionIndex)
                .Set("lastViolationIds", compliance.LastViolationIds)
                .Set("lastFineCoins", compliance.LastFineCoins));

            root.Set("craft", JsonValue.NewObject()
                .Set("route", (int)state.Craft.Route)
                .Set("fixTempC", state.Craft.FixTempC)
                .Set("rollMinutes", state.Craft.RollMinutes)
                .Set("moisturePermille", state.Craft.MoisturePermille)
                .Set("oxidationPercent", state.Craft.OxidationPercent));

            // Thu tu theo enum, khong theo thu tu them vao: hai lan ghi cung mot state phai cho
            // cung mot chuoi byte, va HashSet khong hua gi ve thu tu duyet.
            var branches = JsonValue.NewArray();
            for (int kind = (int)BusinessBranch.BulkB2B; kind <= (int)BusinessBranch.Farmstay; kind++)
                if (state.UnlockedBranches.Contains((BusinessBranch)kind)) branches.Add(JsonValue.Of(kind));
            root.Set("unlockedBranches", branches);
            root.Set("salesChannel", (int)state.SalesChannel);

            var receivables = JsonValue.NewArray();
            for (int i = 0; i < state.Receivables.Count; i++)
                receivables.Add(JsonValue.NewObject()
                    .Set("amountCoins", state.Receivables[i].AmountCoins)
                    .Set("dueAtMs", state.Receivables[i].DueAtMs));
            root.Set("receivables", receivables);

            root.Set("nextCycleCloseAtMs", state.NextCycleCloseAtMs);
            root.Set("cycleIndex", state.CycleIndex);
            root.Set("cycleRevenueCoins", state.CycleRevenueCoins);
            root.Set("cycleExpenseCoins", state.CycleExpenseCoins);
            root.Set("introSeen", state.IntroSeen);

            var cashHistory = JsonValue.NewArray();
            for (int i = 0; i < state.CashHistory.Count; i++)
            {
                var row = state.CashHistory[i];
                cashHistory.Add(JsonValue.NewObject()
                    .Set("month", row.Month)
                    .Set("revenueCoins", row.RevenueCoins)
                    .Set("expenseCoins", row.ExpenseCoins)
                    .Set("debtServiceCoins", row.DebtServiceCoins)
                    .Set("netCashCoins", row.NetCashCoins)
                    .Set("debtRemainingCoins", row.DebtRemainingCoins)
                    .Set("shortfall", row.Shortfall));
            }
            root.Set("cashHistory", cashHistory);

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
                RobotUnlocked = root.GetBool("robotUnlocked", false),
                // Save cu khong co bon truong nay: mac dinh la robot dung o cho cua no va dang ranh.
                PestSeed = root.GetLong("pestSeed", 0),
                RobotXMm = root.GetInt("robotXMm", catalog.Balance.RobotCenterXMm),
                RobotZMm = root.GetInt("robotZMm", catalog.Balance.RobotCenterZMm),
                RobotTargetPlotId = root.GetInt("robotTargetPlotId", -1),
                RobotReadyAtMs = root.GetLong("robotReadyAtMs", 0),
                HiredWorkers = root.GetInt("hiredWorkers", 0),
                StaffedWorkers = root.GetInt("staffedWorkers", 0),
                NextPayrollAtMs = root.GetLong("nextPayrollAtMs", 0)
            };

            if (state.RobotReadyAtMs < 0)
                throw new SaveCorruptException("robotReadyAtMs am.");
            if (state.RobotTargetPlotId < -1 || state.RobotTargetPlotId >= catalog.Balance.MaximumPlots)
                throw new SaveCorruptException("Robot nham o khong ton tai: " + state.RobotTargetPlotId);

            if (state.HiredWorkers < 0 || state.StaffedWorkers < 0 || state.NextPayrollAtMs < 0)
                throw new SaveCorruptException("So tho hoac moc tra luong am.");
            if (state.HiredWorkers > catalog.Balance.MaximumWorkers)
                throw new SaveCorruptException("So tho vuot tran cau hinh.");
            if (state.StaffedWorkers > state.HiredWorkers)
                throw new SaveCorruptException("So tho dang lam nhieu hon so da thue.");

            if (state.SimulationTimeMs < 0) throw new SaveCorruptException("simulationTimeMs am hoac thieu.");
            if (state.CheckpointUtcMs < 0) throw new SaveCorruptException("checkpointUtcMs am hoac thieu.");
            if (state.Coins < 0) throw new SaveCorruptException("coins am.");
            if (state.TutorialStep < 0) throw new SaveCorruptException("tutorialStep am.");

            foreach (var itemId in catalog.Items) state.Inventory[itemId] = 0;

            var knownItems = new HashSet<string>(catalog.Items, StringComparer.Ordinal);
            var inventory = root.Require("inventory");
            for (int i = 0; i < inventory.Count; i++)
            {
                var entry = inventory.Items[i];
                // "cropId" la ten cu cua truong nay; save schema 2 tro ve truoc dung ten do.
                string itemId = entry.GetStringOrNull("itemId") ?? entry.GetStringOrNull("cropId");
                long amount = entry.GetLong("amount", -1);
                if (itemId == null || !knownItems.Contains(itemId))
                    throw new SaveCorruptException("Kho co mat hang khong ton tai: " + itemId);
                if (amount < 0) throw new SaveCorruptException("So luong kho am: " + itemId);
                state.Inventory[itemId] = amount;
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
                    PendingYield = entry.GetInt("pendingYield", -1),
                    // Save cu khong co bon he canh tac: dat con tot nguyen, khong co, khong sau benh.
                    Fertility = entry.GetInt("fertility", 100),
                    Weeds = entry.GetInt("weeds", 0),
                    WeedsUpdatedAtMs = entry.GetLong("weedsUpdatedAtMs", 0),
                    FertilityUpdatedAtMs = entry.GetLong("fertilityUpdatedAtMs", 0),
                    CycleIndex = entry.GetInt("cycleIndex", 0),
                    PestPending = entry.GetBool("pestPending", false),
                    PestActive = entry.GetBool("pestActive", false),
                    PestAtMs = entry.GetLong("pestAtMs", 0),
                    Leafhopper = entry.GetBool("leafhopper", false)
                };

                int phase = entry.GetInt("phase", -1);
                if (phase < (int)PlotPhase.Locked || phase > (int)PlotPhase.Ready)
                    throw new SaveCorruptException("Trang thai o dat khong hop le.");
                plot.Phase = (PlotPhase)phase;

                if (plot.PlotId < 0 || plot.PlotId >= catalog.Balance.MaximumPlots || !seenPlotIds.Add(plot.PlotId))
                    throw new SaveCorruptException("plotId khong hop le hoac trung.");
                if (plot.StartAtMs < 0 || plot.FinishAtMs < 0 || plot.PendingYield < 0)
                    throw new SaveCorruptException("So am tren o dat " + plot.PlotId + ".");
                if (plot.Fertility < 0 || plot.Fertility > 100 || plot.Weeds < 0 || plot.Weeds > 100)
                    throw new SaveCorruptException("Do phi hoac co dai ngoai khoang 0..100 tren o " +
                                                   plot.PlotId + ".");
                if (plot.CycleIndex < 0 || plot.PestAtMs < 0 ||
                    plot.WeedsUpdatedAtMs < 0 || plot.FertilityUpdatedAtMs < 0)
                    throw new SaveCorruptException("So am trong trang thai canh tac o o " + plot.PlotId + ".");

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
                BatchFromPacked = machine.GetBool("batchFromPacked", false),
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

            foreach (var stage in catalog.Stages)
                state.Stations.Add(new StationState { StageId = stage.Id, Owned = false });

            var stations = root.Require("stations");
            for (int i = 0; i < stations.Count; i++)
            {
                var entry = stations.Items[i];
                string stageId = entry.GetStringOrNull("stageId");
                var station = state.Station(stageId);
                if (station == null)
                    throw new SaveCorruptException("May tro toi cong doan khong ton tai: " + stageId);

                station.Owned = entry.GetBool("owned", false);
                station.Running = entry.GetBool("running", false);
                station.BatchCropId = entry.GetStringOrNull("batchCropId");
                station.BatchOutput = entry.GetInt("batchOutput", -1);
                station.BatchStartAtMs = entry.GetLong("batchStartAtMs", -1);
                station.BatchFinishAtMs = entry.GetLong("batchFinishAtMs", -1);

                if (station.BatchOutput < 0 || station.BatchStartAtMs < 0 || station.BatchFinishAtMs < 0)
                    throw new SaveCorruptException("So am trong trang thai may " + stageId + ".");
                if (station.Running)
                {
                    CropDefinition unusedCrop;
                    if (!catalog.TryGetCrop(station.BatchCropId, out unusedCrop))
                        throw new SaveCorruptException("Me dang chay tro toi cay khong ton tai: " + stageId);
                    if (station.BatchFinishAtMs < station.BatchStartAtMs)
                        throw new SaveCorruptException("Me dang chay co deadline nguoc: " + stageId);
                }
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

            ReadBusinessState(root, state, catalog);

            snapshot.State = state;
            return snapshot;
        }

        /// <summary>
        /// Bon he cua ban mo phong khoi nghiep tra.
        ///
        /// Moi truong deu co mac dinh, va mac dinh do la **trang thai ban dau cua he thong**:
        /// khong vay dong nao, chua dang ky gi, bon nut can lua o chuan tra xanh, chua mo phan
        /// nhanh nao. Nho vay mot save cua ban truoc doc len la mot van dang choi hop le chu
        /// khong phai mot van bi khoa vi thieu du lieu.
        /// </summary>
        static void ReadBusinessState(JsonValue root, GameState state, ContentCatalog catalog)
        {
            var loanValue = root.Get("loan");
            if (loanValue != null && loanValue.Kind == JsonKind.Object)
            {
                var loan = state.Loan;
                loan.Kind = (LoanKind)loanValue.GetInt("kind", 0);
                loan.PrincipalCoins = loanValue.GetLong("principalCoins", 0);
                loan.RemainingPrincipalCoins = loanValue.GetLong("remainingPrincipalCoins", 0);
                loan.AnnualRateBps = loanValue.GetInt("annualRateBps", 0);
                loan.TermMonths = loanValue.GetInt("termMonths", 0);
                loan.MonthsPaid = loanValue.GetInt("monthsPaid", 0);
                loan.NextDueAtMs = loanValue.GetLong("nextDueAtMs", 0);
                loan.ConsecutiveShortfalls = loanValue.GetInt("consecutiveShortfalls", 0);
                loan.OverdueCoins = loanValue.GetLong("overdueCoins", 0);
                loan.InterestPaidCoins = loanValue.GetLong("interestPaidCoins", 0);
                loan.Sealed = loanValue.GetBool("sealed", false);
                loan.SealedAtMs = loanValue.GetLong("sealedAtMs", 0);

                if (loan.PrincipalCoins < 0 || loan.RemainingPrincipalCoins < 0 ||
                    loan.OverdueCoins < 0 || loan.AnnualRateBps < 0)
                    throw new SaveCorruptException("Khoan vay co so am.");
                if (loan.RemainingPrincipalCoins > loan.PrincipalCoins)
                    throw new SaveCorruptException("Du no lon hon goc ban dau.");
                if (loan.TermMonths < 0 || loan.MonthsPaid < 0)
                    throw new SaveCorruptException("Ky han khoan vay am.");
            }

            var complianceValue = root.Get("compliance");
            if (complianceValue != null && complianceValue.Kind == JsonKind.Object)
            {
                var compliance = state.Compliance;
                compliance.Entity = (BusinessEntity)complianceValue.GetInt("entity", 0);
                compliance.PendingEntity = (BusinessEntity)complianceValue.GetInt("pendingEntity", 0);
                compliance.EntityReadyAtMs = complianceValue.GetLong("entityReadyAtMs", 0);
                compliance.FoodSafetyCertified = complianceValue.GetBool("foodSafetyCertified", false);
                compliance.FoodSafetyPending = complianceValue.GetBool("foodSafetyPending", false);
                compliance.FoodSafetyReadyAtMs = complianceValue.GetLong("foodSafetyReadyAtMs", 0);
                compliance.ProtectiveGear = complianceValue.GetBool("protectiveGear", false);
                compliance.LicenceWarned = complianceValue.GetBool("licenceWarned", false);
                compliance.NextInspectionAtMs = complianceValue.GetLong("nextInspectionAtMs", 0);
                compliance.InspectionCount = complianceValue.GetInt("inspectionCount", 0);
                compliance.SuspendedUntilMs = complianceValue.GetLong("suspendedUntilMs", 0);
                compliance.TotalFinesCoins = complianceValue.GetLong("totalFinesCoins", 0);
                compliance.TotalTaxCoins = complianceValue.GetLong("totalTaxCoins", 0);
                compliance.LastInspectionIndex = complianceValue.GetInt("lastInspectionIndex", 0);
                compliance.LastViolationIds = complianceValue.GetStringOrNull("lastViolationIds");
                compliance.LastFineCoins = complianceValue.GetLong("lastFineCoins", 0);

                if (compliance.InspectionCount < 0 || compliance.TotalFinesCoins < 0 ||
                    compliance.TotalTaxCoins < 0)
                    throw new SaveCorruptException("Ho so phap ly co so am.");
            }

            var craftValue = root.Get("craft");
            if (craftValue != null && craftValue.Kind == JsonKind.Object)
            {
                var route = (TeaRoute)craftValue.GetInt("route", 0);
                var windows = Crafting.Windows(route);
                state.Craft = new CraftSettings
                {
                    Route = route,
                    // Kep vao khoang cho phep ngay luc doc: mot save bi sua tay voi nhiet do
                    // 99999 khong duoc bien thanh mot me tra khong the danh gia.
                    FixTempC = Cultivation.Clamp(craftValue.GetInt("fixTempC", windows[0].IdealLow),
                                                 windows[0].Minimum, windows[0].Maximum),
                    RollMinutes = Cultivation.Clamp(craftValue.GetInt("rollMinutes", windows[1].IdealLow),
                                                    windows[1].Minimum, windows[1].Maximum),
                    MoisturePermille = Cultivation.Clamp(
                        craftValue.GetInt("moisturePermille", windows[2].IdealHigh),
                        windows[2].Minimum, windows[2].Maximum),
                    OxidationPercent = Cultivation.Clamp(
                        craftValue.GetInt("oxidationPercent", windows[3].IdealLow),
                        windows[3].Minimum, windows[3].Maximum)
                };
            }

            var branchesValue = root.Get("unlockedBranches");
            if (branchesValue != null && branchesValue.Kind == JsonKind.Array)
                for (int i = 0; i < branchesValue.Count; i++)
                {
                    var branch = (BusinessBranch)branchesValue.Items[i].AsInt();
                    if (Branches.Definition(branch) == null)
                        throw new SaveCorruptException("Phan nhanh khong ton tai: " + (int)branch);
                    state.UnlockedBranches.Add(branch);
                }

            state.SalesChannel = (BusinessBranch)root.GetInt("salesChannel", 0);
            if (state.SalesChannel != BusinessBranch.None &&
                !state.UnlockedBranches.Contains(state.SalesChannel))
                state.SalesChannel = BusinessBranch.None;

            var receivablesValue = root.Get("receivables");
            if (receivablesValue != null && receivablesValue.Kind == JsonKind.Array)
                for (int i = 0; i < receivablesValue.Count; i++)
                {
                    var entry = receivablesValue.Items[i];
                    long amount = entry.GetLong("amountCoins", 0);
                    if (amount < 0) throw new SaveCorruptException("Khoan phai thu am.");
                    state.Receivables.Add(new Receivable
                    {
                        AmountCoins = amount,
                        DueAtMs = entry.GetLong("dueAtMs", 0)
                    });
                }

            state.NextCycleCloseAtMs = root.GetLong("nextCycleCloseAtMs",
                                                    Finance.CycleMs(catalog.Balance));
            state.CycleIndex = root.GetInt("cycleIndex", 0);
            state.CycleRevenueCoins = root.GetLong("cycleRevenueCoins", 0);
            state.CycleExpenseCoins = root.GetLong("cycleExpenseCoins", 0);
            state.IntroSeen = root.GetBool("introSeen", false);

            if (state.NextCycleCloseAtMs < 0 || state.CycleIndex < 0 ||
                state.CycleRevenueCoins < 0 || state.CycleExpenseCoins < 0)
                throw new SaveCorruptException("So lieu ky tai chinh am.");

            // Ky dau tien phai co mot moc, khong thi chot ky se khong bao gio den han va ca he
            // tai chinh nam im — mot loi khong bao gi ca.
            if (state.NextCycleCloseAtMs == 0)
                state.NextCycleCloseAtMs = state.SimulationTimeMs + Finance.CycleMs(catalog.Balance);
            if (state.Compliance.NextInspectionAtMs == 0)
                state.Compliance.NextInspectionAtMs = state.SimulationTimeMs +
                    Finance.CycleMs(catalog.Balance) * catalog.Balance.InspectionEveryCycles;

            var historyValue = root.Get("cashHistory");
            if (historyValue != null && historyValue.Kind == JsonKind.Array)
                for (int i = 0; i < historyValue.Count; i++)
                {
                    var entry = historyValue.Items[i];
                    state.CashHistory.Add(new CashCycleRecord
                    {
                        Month = entry.GetInt("month", 0),
                        RevenueCoins = entry.GetLong("revenueCoins", 0),
                        ExpenseCoins = entry.GetLong("expenseCoins", 0),
                        DebtServiceCoins = entry.GetLong("debtServiceCoins", 0),
                        NetCashCoins = entry.GetLong("netCashCoins", 0),
                        DebtRemainingCoins = entry.GetLong("debtRemainingCoins", 0),
                        Shortfall = entry.GetBool("shortfall", false)
                    });
                }
            while (state.CashHistory.Count > catalog.Balance.CashHistoryCycles)
                state.CashHistory.RemoveAt(0);
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
                fromSchemaVersion = 2;
            }

            // 2 -> 3: them day chuyen che bien. Save cu khong co may nao va khong co tho nao, ma
            // do dung la trang thai ban dau cua he thong moi — nen mot danh sach rong la du.
            // Kho cua save cu chi co la tuoi, va ten truong "cropId" van doc duoc o ban moi.
            if (fromSchemaVersion == 2)
            {
                if (!root.Has("stations")) root.Set("stations", JsonValue.NewArray());
                if (!root.Has("hiredWorkers")) root.Set("hiredWorkers", 0);
                if (!root.Has("staffedWorkers")) root.Set("staffedWorkers", 0);
                if (!root.Has("nextPayrollAtMs")) root.Set("nextPayrollAtMs", 0);
                fromSchemaVersion = 3;
            }

            // 3 -> 4: robot di toi tung o thay vi thu sach tuc thi. Khong truong nao doi y nghia;
            // bon truong moi deu co mac dinh dung o buoc doc, nen o day khong phai them gi.
            if (fromSchemaVersion == 3) fromSchemaVersion = 4;

            // 4 -> 5: them do phi, co dai, sau benh. Cung the: moi truong moi deu co mac dinh o
            // buoc doc, va mac dinh do la mot khu vuon dat con tot, sach co, khong sau benh —
            // dung trang thai ma mot van dang choi dang o.
            if (fromSchemaVersion == 4) fromSchemaVersion = 5;

            // 5 -> 6: them von va no, phap ly, can lua, phan nhanh kinh doanh. ReadBusinessState
            // co mac dinh dung cho tat ca — khong vay dong nao, chua dang ky gi — nen o day khong
            // phai them gi. Ba truong doc tu goc (moc chot ky va moc kiem tra) duoc bu bang
            // SimulationTimeMs cua chinh save do, nen mot van dang choi doc len se bat dau tinh
            // ky tai chinh tu luc no dang o chu khong tu moc 0.
            if (fromSchemaVersion == 5) return root;

            throw new SaveCorruptException("Khong co buoc migration tu schema " + fromSchemaVersion + ".");
        }
    }
}
