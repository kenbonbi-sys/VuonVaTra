using System.Collections.Generic;
using System.Text;
using VuonNho.Core;

namespace VuonNho.Tests
{
    public sealed class FakeClock : IClock
    {
        public long Utc = 1000000000000;
        public long Monotonic;

        public long UtcNowMs { get { return Utc; } }
        public long MonotonicMs { get { return Monotonic; } }

        /// <summary>Thoi gian troi trong phien: ca hai dong ho cung tien.</summary>
        public void AdvanceBoth(long milliseconds)
        {
            Utc += milliseconds;
            Monotonic += milliseconds;
        }

        /// <summary>Quang nghi ngoai phien: chi UTC tien, dong ho trong phien khong tick.</summary>
        public void AdvanceUtcOnly(long milliseconds)
        {
            Utc += milliseconds;
        }
    }

    public sealed class MemorySaveRepository : ISaveRepository
    {
        public string Main;
        public string Backup;
        public int SaveCount;
        public bool FailNextSave;
        public bool FailAllSaves;

        public bool HasSave { get { return Main != null || Backup != null; } }

        public IList<SaveCandidate> LoadCandidates()
        {
            return new List<SaveCandidate>
            {
                new SaveCandidate { Name = "main", Json = Main },
                new SaveCandidate { Name = "backup", Json = Backup }
            };
        }

        public void Save(string json)
        {
            if (FailAllSaves || FailNextSave)
            {
                FailNextSave = false;
                throw new System.IO.IOException("Loi ghi gia lap.");
            }
            Backup = Main;
            Main = json;
            SaveCount++;
        }

        public void DeleteAll()
        {
            Main = null;
            Backup = null;
        }
    }

    public sealed class RecordingLogger : ITestLogger
    {
        public readonly List<string> Events = new List<string>();

        public void Log(string eventName, params string[] fields)
        {
            Events.Add(eventName);
        }

        public int CountOf(string eventName)
        {
            int count = 0;
            for (int i = 0; i < Events.Count; i++)
                if (Events[i] == eventName) count++;
            return count;
        }
    }

    public static class TestKit
    {
        public static ContentCatalog Catalog()
        {
            return DefaultContent.Create();
        }

        public static GameState NewState(ContentCatalog catalog)
        {
            return GameState.CreateNew(catalog, 1000000000000);
        }

        public static GameSession NewSession(out FakeClock clock, out MemorySaveRepository repository)
        {
            clock = new FakeClock();
            repository = new MemorySaveRepository();
            var session = new GameSession(Catalog(), clock, repository, new RecordingLogger(), "test");
            session.Initialize();
            return session;
        }

        /// <summary>Gieo cay cho moi o da mo, dung cho cac bai test khong quan tam thao tac UI.</summary>
        public static void PlantAllUnlocked(GameState state, FarmSimulation simulation, string cropId)
        {
            for (int i = 0; i < state.Plots.Count; i++)
            {
                var plot = state.Plots[i];
                if (plot.Unlocked && plot.Phase == PlotPhase.Empty)
                    simulation.Plant(state, plot, cropId, state.SimulationTimeMs);
            }
        }

        /// <summary>
        /// Chuoi so sanh state, bo qua saveRevision va checkpoint vi hai duong chay khac nhau
        /// co the luu so lan khac nhau.
        /// </summary>
        public static string Signature(GameState state, ContentCatalog catalog)
        {
            var builder = new StringBuilder();
            builder.Append("t=").Append(state.SimulationTimeMs);
            builder.Append(";coins=").Append(state.Coins);
            builder.Append(";robot=").Append(state.RobotUnlocked ? 1 : 0);

            builder.Append(";inv=");
            for (int i = 0; i < catalog.Crops.Count; i++)
                builder.Append(catalog.Crops[i].Id).Append(':')
                       .Append(state.InventoryOf(catalog.Crops[i].Id)).Append(',');

            builder.Append(";plots=");
            for (int i = 0; i < state.Plots.Count; i++)
            {
                var plot = state.Plots[i];
                builder.Append(plot.PlotId).Append('|')
                       .Append(plot.Unlocked ? 1 : 0).Append('|')
                       .Append((int)plot.Phase).Append('|')
                       .Append(plot.CurrentCropId ?? "-").Append('|')
                       .Append(plot.NextCropId ?? "-").Append('|')
                       .Append(plot.StartAtMs).Append('|')
                       .Append(plot.FinishAtMs).Append('|')
                       .Append(plot.PendingYield).Append(',');
            }

            var machine = state.Machine;
            builder.Append(";machine=").Append(machine.SelectedRecipeId ?? "-").Append('|')
                   .Append(machine.BatchRunning ? 1 : 0).Append('|')
                   .Append(machine.BatchRecipeId ?? "-").Append('|')
                   .Append(machine.BatchStartAtMs).Append('|')
                   .Append(machine.BatchFinishAtMs).Append('|')
                   .Append(machine.BatchOutputCoins);

            builder.Append(";upgrades=");
            for (int i = 0; i < catalog.Upgrades.Count; i++)
                builder.Append(catalog.Upgrades[i].Id).Append(':')
                       .Append(state.UpgradeLevel(catalog.Upgrades[i].Id)).Append(',');

            return builder.ToString();
        }
    }
}
