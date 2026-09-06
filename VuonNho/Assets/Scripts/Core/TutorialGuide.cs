namespace VuonNho.Core
{
    public enum TutorialStep
    {
        PlantAll,
        WaitAndHarvest,
        WatchTeaSell,
        SaveForRobot,
        ChooseNextUpgrade
    }

    /// <summary>
    /// Mot chi dan ngan tai mot thoi diem, suy ra tu hanh dong thuc te nen sau khi load save
    /// van tiep tuc dung buoc.
    /// </summary>
    public static class TutorialGuide
    {
        public static TutorialStep CurrentStep(GameState state, ContentCatalog catalog)
        {
            bool anyEmptyPlot = false;
            for (int i = 0; i < state.Plots.Count; i++)
            {
                var plot = state.Plots[i];
                if (plot.Unlocked && plot.Phase == PlotPhase.Empty) { anyEmptyPlot = true; break; }
            }

            if (!state.RobotUnlocked)
            {
                if (anyEmptyPlot) return TutorialStep.PlantAll;

                long totalInventory = 0;
                foreach (var pair in state.Inventory) totalInventory += pair.Value;

                if (state.Coins == 0 && totalInventory == 0 && !state.Machine.BatchRunning)
                    return TutorialStep.WaitAndHarvest;
                if (state.Coins == 0) return TutorialStep.WatchTeaSell;
                return TutorialStep.SaveForRobot;
            }

            if (anyEmptyPlot) return TutorialStep.PlantAll;
            return TutorialStep.ChooseNextUpgrade;
        }

        /// <summary>Chi so buoc de luu va de doc log; text hien thi nam o lop view.</summary>
        public static int StepIndex(TutorialStep step)
        {
            return (int)step;
        }
    }
}
