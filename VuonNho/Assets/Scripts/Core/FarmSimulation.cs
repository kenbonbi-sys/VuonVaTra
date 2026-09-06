using System;
using System.Collections.Generic;

namespace VuonNho.Core
{
    /// <summary>
    /// View va log dang ky de nghe. Simulation khong biet gi ve GameObject.
    /// Khi chay bu offline, GameSession tat listener de khong phat lai hang nghin hieu ung.
    /// </summary>
    public interface ISimulationListener
    {
        void OnCropReady(int plotId, string cropId, long atMs);
        void OnHarvested(int plotId, string cropId, int amount, bool byRobot, long atMs);
        void OnPlanted(int plotId, string cropId, long atMs);
        void OnBatchStarted(string recipeId, long atMs);
        void OnBatchCompleted(string recipeId, long coins, long atMs);
    }

    /// <summary>
    /// Mot thuat toan dung cho ca online va offline (muc 9 cua ke hoach).
    /// AdvanceTo luon tien thoi gian ve phia truoc va giai quyet su kien theo deadline chinh xac,
    /// nen do tre tick khong lam troi chu ky.
    /// </summary>
    public sealed class FarmSimulation
    {
        /// <summary>Chan vong lap vo han neu du lieu loi lam chu ky bang 0.</summary>
        public const int MaxEventsPerAdvance = 20000000;

        readonly ContentCatalog _catalog;

        readonly List<ISimulationListener> _listeners = new List<ISimulationListener>();

        /// <summary>Bat khi chay bu offline hoac khi mo phong tren ban sao cua mot transaction.</summary>
        public bool ListenersMuted;

        public void AddListener(ISimulationListener listener)
        {
            if (listener != null && !_listeners.Contains(listener)) _listeners.Add(listener);
        }

        public void RemoveListener(ISimulationListener listener)
        {
            _listeners.Remove(listener);
        }

        public ContentCatalog Catalog { get { return _catalog; } }

        public FarmSimulation(ContentCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException("catalog");
            _catalog = catalog;
        }

        public int GrowthSpeedLevel(GameState state)
        {
            int total = 0;
            for (int i = 0; i < _catalog.Upgrades.Count; i++)
                if (_catalog.Upgrades[i].Kind == UpgradeKind.GrowthSpeed)
                    total += state.UpgradeLevel(_catalog.Upgrades[i].Id);
            return total;
        }

        public int BrewSpeedLevel(GameState state)
        {
            int total = 0;
            for (int i = 0; i < _catalog.Upgrades.Count; i++)
                if (_catalog.Upgrades[i].Kind == UpgradeKind.BrewSpeed)
                    total += state.UpgradeLevel(_catalog.Upgrades[i].Id);
            return total;
        }

        public long GrowthMsFor(GameState state, string cropId)
        {
            return _catalog.ScaleGrowth(_catalog.Crop(cropId).BaseGrowthMs, GrowthSpeedLevel(state));
        }

        public long BrewMsFor(GameState state, string recipeId)
        {
            return _catalog.ScaleBrew(_catalog.Recipe(recipeId).BaseBrewMs, BrewSpeedLevel(state));
        }

        /// <summary>
        /// Tien mo phong den targetSimulationTimeMs. Goi nhieu buoc nho hay mot buoc lon
        /// deu phai cho cung ket qua khi lich lenh giong nhau.
        /// </summary>
        public void AdvanceTo(GameState state, long targetSimulationTimeMs)
        {
            if (state == null) throw new ArgumentNullException("state");
            if (targetSimulationTimeMs < state.SimulationTimeMs) return;

            int guard = 0;
            while (true)
            {
                long nextEventMs;
                if (!TryGetNextEventTime(state, out nextEventMs)) break;
                if (nextEventMs > targetSimulationTimeMs) break;
                if (nextEventMs < state.SimulationTimeMs) nextEventMs = state.SimulationTimeMs;

                ResolveAt(state, nextEventMs);

                if (++guard > MaxEventsPerAdvance)
                    throw new InvalidOperationException("Vuot so su kien cho phep trong mot lan AdvanceTo.");
            }

            state.SimulationTimeMs = targetSimulationTimeMs;
        }

        /// <summary>
        /// Giai quyet nhung viec da den han ngay tai thoi diem hien tai. Dung sau khi mot lenh
        /// nguoi choi lam doi kho, cong thuc hoac trang thai robot.
        /// </summary>
        public void ResolveImmediate(GameState state)
        {
            ResolveAt(state, state.SimulationTimeMs);
        }

        /// <summary>Thu tu co dinh tai mot timestamp: den han -> robot -> may.</summary>
        void ResolveAt(GameState state, long atMs)
        {
            state.SimulationTimeMs = atMs;

            // 1. Cay den han sang Ready, me may den han hoan thanh va cong xu dung mot lan.
            for (int i = 0; i < state.Plots.Count; i++)
            {
                var plot = state.Plots[i];
                if (plot.Phase == PlotPhase.Growing && plot.FinishAtMs <= atMs)
                {
                    plot.Phase = PlotPhase.Ready;
                    NotifyCropReady(plot.PlotId, plot.CurrentCropId, atMs);
                }
            }

            if (state.Machine.BatchRunning && state.Machine.BatchFinishAtMs <= atMs)
            {
                long coins = state.Machine.BatchOutputCoins;
                string recipeId = state.Machine.BatchRecipeId;
                state.Coins += coins;
                state.Machine.BatchRunning = false;
                state.Machine.BatchRecipeId = null;
                state.Machine.BatchOutputCoins = 0;
                state.Machine.BatchStartAtMs = 0;
                state.Machine.BatchFinishAtMs = 0;
                NotifyBatchCompleted(recipeId, coins, atMs);
            }

            // 2. Robot thu cac o Ready theo plotId roi gieo lai cay da chon.
            if (state.RobotUnlocked)
            {
                for (int i = 0; i < state.Plots.Count; i++)
                {
                    var plot = state.Plots[i];
                    if (plot.Phase == PlotPhase.Ready)
                        HarvestAndReplant(state, plot, atMs, true);
                }
            }

            // 3. May ranh va du nguyen lieu thi bat dau me moi.
            TryStartBatch(state, atMs);
        }

        public bool TryGetNextEventTime(GameState state, out long nextEventMs)
        {
            bool found = false;
            long best = 0;

            for (int i = 0; i < state.Plots.Count; i++)
            {
                var plot = state.Plots[i];
                if (plot.Phase != PlotPhase.Growing) continue;
                if (!found || plot.FinishAtMs < best) { best = plot.FinishAtMs; found = true; }
            }

            if (state.Machine.BatchRunning)
            {
                if (!found || state.Machine.BatchFinishAtMs < best)
                {
                    best = state.Machine.BatchFinishAtMs;
                    found = true;
                }
            }

            // Robot dang doi mot o Ready (vi du vua mua robot giua chung) cung la mot su kien tuc thi.
            if (state.RobotUnlocked)
            {
                for (int i = 0; i < state.Plots.Count; i++)
                {
                    if (state.Plots[i].Phase != PlotPhase.Ready) continue;
                    long now = state.SimulationTimeMs;
                    if (!found || now < best) { best = now; found = true; }
                    break;
                }
            }

            nextEventMs = best;
            return found;
        }

        /// <summary>Thu thu cong va thu bang robot chay cung mot quy tac.</summary>
        public bool HarvestAndReplant(GameState state, PlotState plot, long atMs, bool byRobot)
        {
            if (plot == null || plot.Phase != PlotPhase.Ready) return false;

            string harvestedCropId = plot.CurrentCropId;
            int amount = plot.PendingYield;
            if (amount > 0 && harvestedCropId != null)
                state.AddInventory(harvestedCropId, amount);
            NotifyHarvested(plot.PlotId, harvestedCropId, amount, byRobot, atMs);

            plot.Phase = PlotPhase.Empty;
            plot.CurrentCropId = null;
            plot.PendingYield = 0;
            plot.StartAtMs = 0;
            plot.FinishAtMs = 0;

            // Gieo lai ngay trong cung mot hanh dong neu o da co lua chon cay hop le.
            string nextCropId = plot.NextCropId;
            if (nextCropId != null && state.UnlockedCropIds.Contains(nextCropId))
                Plant(state, plot, nextCropId, atMs);

            return true;
        }

        public void Plant(GameState state, PlotState plot, string cropId, long atMs)
        {
            var crop = _catalog.Crop(cropId);
            plot.CurrentCropId = cropId;
            plot.NextCropId = cropId;
            plot.Phase = PlotPhase.Growing;
            plot.StartAtMs = atMs;
            plot.FinishAtMs = atMs + GrowthMsFor(state, cropId);
            plot.PendingYield = crop.Yield;
            NotifyPlanted(plot.PlotId, cropId, atMs);
        }

        /// <summary>
        /// Tru toan bo nguyen lieu dung luc bat dau me. Xu duoc chot ngay de nang cap
        /// hay doi cong thuc giua chung khong lam thay doi me dang chay.
        /// </summary>
        public bool TryStartBatch(GameState state, long atMs)
        {
            var machine = state.Machine;
            if (machine.BatchRunning) return false;

            RecipeDefinition recipe;
            if (!_catalog.TryGetRecipe(machine.SelectedRecipeId, out recipe)) return false;
            if (!state.UnlockedRecipeIds.Contains(recipe.Id)) return false;
            if (state.InventoryOf(recipe.InputCropId) < recipe.InputCount) return false;

            state.AddInventory(recipe.InputCropId, -recipe.InputCount);
            machine.BatchRunning = true;
            machine.BatchRecipeId = recipe.Id;
            machine.BatchStartAtMs = atMs;
            machine.BatchFinishAtMs = atMs + BrewMsFor(state, recipe.Id);
            machine.BatchOutputCoins = recipe.OutputCoins;
            NotifyBatchStarted(recipe.Id, atMs);
            return true;
        }


        void NotifyCropReady(int plotId, string cropId, long atMs)
        {
            if (ListenersMuted) return;
            for (int i = 0; i < _listeners.Count; i++) _listeners[i].OnCropReady(plotId, cropId, atMs);
        }

        void NotifyHarvested(int plotId, string cropId, int amount, bool byRobot, long atMs)
        {
            if (ListenersMuted) return;
            for (int i = 0; i < _listeners.Count; i++) _listeners[i].OnHarvested(plotId, cropId, amount, byRobot, atMs);
        }

        void NotifyPlanted(int plotId, string cropId, long atMs)
        {
            if (ListenersMuted) return;
            for (int i = 0; i < _listeners.Count; i++) _listeners[i].OnPlanted(plotId, cropId, atMs);
        }

        void NotifyBatchStarted(string recipeId, long atMs)
        {
            if (ListenersMuted) return;
            for (int i = 0; i < _listeners.Count; i++) _listeners[i].OnBatchStarted(recipeId, atMs);
        }

        void NotifyBatchCompleted(string recipeId, long coins, long atMs)
        {
            if (ListenersMuted) return;
            for (int i = 0; i < _listeners.Count; i++) _listeners[i].OnBatchCompleted(recipeId, coins, atMs);
        }

        /// <summary>Ten va so nguyen lieu con thieu de UI giai thich vi sao may dung.</summary>
        public bool TryGetMissingInput(GameState state, out string cropId, out long missing)
        {
            cropId = null;
            missing = 0;
            if (state.Machine.BatchRunning) return false;

            RecipeDefinition recipe;
            if (!_catalog.TryGetRecipe(state.Machine.SelectedRecipeId, out recipe)) return false;

            long have = state.InventoryOf(recipe.InputCropId);
            if (have >= recipe.InputCount) return false;

            cropId = recipe.InputCropId;
            missing = recipe.InputCount - have;
            return true;
        }
    }
}
