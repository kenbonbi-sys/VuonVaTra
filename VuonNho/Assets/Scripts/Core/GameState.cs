using System;
using System.Collections.Generic;

namespace VuonNho.Core
{
    public enum PlotPhase
    {
        Locked = 0,
        Empty = 1,
        Growing = 2,
        Ready = 3
    }

    public sealed class PlotState
    {
        public int PlotId;
        public bool Unlocked;
        public PlotPhase Phase;
        /// <summary>Cay dang tren o. Null khi Empty/Locked.</summary>
        public string CurrentCropId;
        /// <summary>Cay se gieo o vong tiep theo. Null = de trong sau khi thu.</summary>
        public string NextCropId;
        public long StartAtMs;
        public long FinishAtMs;
        /// <summary>Yield da chot luc gieo, khong doc lai catalog khi thu.</summary>
        public int PendingYield;

        public PlotState Clone()
        {
            return (PlotState)MemberwiseClone();
        }
    }

    public sealed class MachineState
    {
        public string SelectedRecipeId;
        public bool BatchRunning;
        public string BatchRecipeId;
        /// <summary>Me nay pha tu tra da dong goi hay tu la tuoi. Chi de HUD noi dung chuyen.</summary>
        public bool BatchFromPacked;
        public long BatchStartAtMs;
        public long BatchFinishAtMs;
        /// <summary>Xu duoc chot luc bat dau me, khong tinh lai khi hoan thanh.</summary>
        public long BatchOutputCoins;

        public MachineState Clone()
        {
            return (MachineState)MemberwiseClone();
        }
    }

    public sealed class InventoryDelta
    {
        public string CropId;
        public long Amount;
    }

    /// <summary>Bao cao quay lai. Chi danh dau da xem, khong cong lai tien.</summary>
    public sealed class OfflineSummary
    {
        public string Id;
        public long ElapsedMs;
        public long CoinsGained;
        public List<InventoryDelta> ItemsGained = new List<InventoryDelta>();
        public bool MachineWaiting;
        public bool Seen;

        public OfflineSummary Clone()
        {
            var copy = new OfflineSummary
            {
                Id = Id,
                ElapsedMs = ElapsedMs,
                CoinsGained = CoinsGained,
                MachineWaiting = MachineWaiting,
                Seen = Seen,
                ItemsGained = new List<InventoryDelta>(ItemsGained.Count)
            };
            for (int i = 0; i < ItemsGained.Count; i++)
                copy.ItemsGained.Add(new InventoryDelta { CropId = ItemsGained[i].CropId, Amount = ItemsGained[i].Amount });
            return copy;
        }
    }

    /// <summary>
    /// Nguon su that duy nhat cho tien, kho, timer va mo khoa.
    /// Khong giu tham chieu prefab hay Transform.
    /// </summary>
    public sealed class GameState
    {
        public long SimulationTimeMs;
        public long CheckpointUtcMs;
        public long Coins;
        public int TutorialStep;
        public long SaveRevision;
        public bool RobotUnlocked;

        public readonly Dictionary<string, long> Inventory = new Dictionary<string, long>(StringComparer.Ordinal);
        public readonly List<PlotState> Plots = new List<PlotState>();
        public MachineState Machine = new MachineState();

        /// <summary>Mot phan tu cho moi cong doan trong catalog, ke ca may chua mua.</summary>
        public readonly List<StationState> Stations = new List<StationState>();

        /// <summary>So tho da thue. Moi tho chay duoc mot may cung luc.</summary>
        public int HiredWorkers;

        /// <summary>
        /// So tho **tra duoc luong** cua ky hien tai. It hon so da thue khi trong tui khong du xu.
        ///
        /// Giu rieng khoi HiredWorkers de het tien khong lam mat nguoi: tho van con day, chi la
        /// ky nay khong ai chay may. Tra duoc luong ky sau la day chuyen chay lai ngay.
        /// </summary>
        public int StaffedWorkers;

        /// <summary>Moc tra luong tiep theo, theo thoi gian mo phong.</summary>
        public long NextPayrollAtMs;
        public readonly HashSet<string> UnlockedCropIds = new HashSet<string>(StringComparer.Ordinal);
        public readonly HashSet<string> UnlockedRecipeIds = new HashSet<string>(StringComparer.Ordinal);
        public readonly Dictionary<string, int> UpgradeLevels = new Dictionary<string, int>(StringComparer.Ordinal);
        /// <summary>Trang tri da dat. Thuan tham my, FarmSimulation khong bao gio doc den.</summary>
        public readonly List<PlacedDecoration> Decorations = new List<PlacedDecoration>();
        public OfflineSummary PendingOfflineSummary;

        public static GameState CreateNew(ContentCatalog catalog, long nowUtcMs)
        {
            var state = new GameState
            {
                SimulationTimeMs = 0,
                CheckpointUtcMs = nowUtcMs,
                Coins = 0,
                TutorialStep = 0,
                SaveRevision = 1,
                RobotUnlocked = false
            };

            for (int i = 0; i < catalog.Balance.MaximumPlots; i++)
            {
                state.Plots.Add(new PlotState
                {
                    PlotId = i,
                    Unlocked = i < catalog.Balance.InitialPlots,
                    Phase = i < catalog.Balance.InitialPlots ? PlotPhase.Empty : PlotPhase.Locked,
                    CurrentCropId = null,
                    NextCropId = null,
                    StartAtMs = 0,
                    FinishAtMs = 0,
                    PendingYield = 0
                });
            }

            foreach (var crop in catalog.Crops)
                if (crop.UnlockUpgradeId == null) state.UnlockedCropIds.Add(crop.Id);

            // Ke ca hang trung gian: kho co du dong ngay tu dau thi HUD khong phai phan biet
            // "chua co mon nay" voi "co 0 mon nay".
            foreach (var itemId in catalog.Items) state.Inventory[itemId] = 0;

            foreach (var stage in catalog.Stages)
                state.Stations.Add(new StationState { StageId = stage.Id, Owned = false });

            foreach (var recipe in catalog.Recipes)
                if (recipe.UnlockUpgradeId == null) state.UnlockedRecipeIds.Add(recipe.Id);

            foreach (var upgrade in catalog.Upgrades)
                state.UpgradeLevels[upgrade.Id] = 0;

            // May khoi dau bat san voi cong thuc bac ha.
            var first = catalog.Recipes.Count > 0 ? catalog.Recipes[0] : null;
            state.Machine.SelectedRecipeId = first != null ? first.Id : null;
            return state;
        }

        public long InventoryOf(string itemId)
        {
            long value;
            return Inventory.TryGetValue(itemId, out value) ? value : 0;
        }

        public void AddInventory(string itemId, long amount)
        {
            long current = InventoryOf(itemId);
            long next = current + amount;
            if (next < 0) throw new InvalidOperationException("Kho khong duoc am: " + itemId);
            Inventory[itemId] = next;
        }

        public StationState Station(string stageId)
        {
            for (int i = 0; i < Stations.Count; i++)
                if (Stations[i].StageId == stageId) return Stations[i];
            return null;
        }

        public int OwnedStationCount()
        {
            int count = 0;
            for (int i = 0; i < Stations.Count; i++)
                if (Stations[i].Owned) count++;
            return count;
        }

        public int RunningStationCount()
        {
            int count = 0;
            for (int i = 0; i < Stations.Count; i++)
                if (Stations[i].Running) count++;
            return count;
        }

        public int UpgradeLevel(string upgradeId)
        {
            int level;
            return UpgradeLevels.TryGetValue(upgradeId, out level) ? level : 0;
        }

        public PlotState Plot(int plotId)
        {
            for (int i = 0; i < Plots.Count; i++)
                if (Plots[i].PlotId == plotId) return Plots[i];
            return null;
        }

        public int UnlockedPlotCount()
        {
            int count = 0;
            for (int i = 0; i < Plots.Count; i++)
                if (Plots[i].Unlocked) count++;
            return count;
        }

        /// <summary>Ban sao sau de mo phong ma khong dong vao tien trinh dang hien thi.</summary>
        public GameState Clone()
        {
            var copy = new GameState
            {
                SimulationTimeMs = SimulationTimeMs,
                CheckpointUtcMs = CheckpointUtcMs,
                Coins = Coins,
                TutorialStep = TutorialStep,
                SaveRevision = SaveRevision,
                RobotUnlocked = RobotUnlocked,
                HiredWorkers = HiredWorkers,
                StaffedWorkers = StaffedWorkers,
                NextPayrollAtMs = NextPayrollAtMs,
                Machine = Machine.Clone(),
                PendingOfflineSummary = PendingOfflineSummary == null ? null : PendingOfflineSummary.Clone()
            };
            foreach (var pair in Inventory) copy.Inventory[pair.Key] = pair.Value;
            for (int i = 0; i < Plots.Count; i++) copy.Plots.Add(Plots[i].Clone());
            foreach (var id in UnlockedCropIds) copy.UnlockedCropIds.Add(id);
            foreach (var id in UnlockedRecipeIds) copy.UnlockedRecipeIds.Add(id);
            foreach (var pair in UpgradeLevels) copy.UpgradeLevels[pair.Key] = pair.Value;
            for (int i = 0; i < Decorations.Count; i++) copy.Decorations.Add(Decorations[i].Clone());
            for (int i = 0; i < Stations.Count; i++) copy.Stations.Add(Stations[i].Clone());
            return copy;
        }
    }
}
