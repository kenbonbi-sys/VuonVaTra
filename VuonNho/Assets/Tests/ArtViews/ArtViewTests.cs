using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VuonNho.Core;
using VuonNho.Views;

namespace VuonNho.Tests.ArtViews
{
    public sealed class ArtViewTests
    {
        GameObject _root;
        bool _hadSfxPreference;
        int _sfxPreference;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("ArtViewTest");
            _hadSfxPreference = PlayerPrefs.HasKey(SfxPlayer.VolumePrefKey);
            _sfxPreference = PlayerPrefs.GetInt(SfxPlayer.VolumePrefKey, 1);
            PlayerPrefs.SetInt(SfxPlayer.VolumePrefKey, 1);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            if (_hadSfxPreference) PlayerPrefs.SetInt(SfxPlayer.VolumePrefKey, _sfxPreference);
            else PlayerPrefs.DeleteKey(SfxPlayer.VolumePrefKey);
            PlayerPrefs.Save();
        }

        static GameState RunningState(ContentCatalog catalog)
        {
            var state = GameState.CreateNew(catalog, 1000000000000);
            state.Machine.BatchRunning = true;
            state.Machine.BatchRecipeId = DefaultContent.RecipeMint;
            state.Machine.BatchStartAtMs = 0;
            state.Machine.BatchFinishAtMs = 10000;
            state.Machine.BatchOutputCoins = 9;
            state.SimulationTimeMs = 5000;
            return state;
        }

        Transform Anchor(string name, Vector3 position, Vector3 scale)
        {
            var child = new GameObject(name).transform;
            child.SetParent(_root.transform, false);
            child.localPosition = position;
            child.localScale = scale;
            return child;
        }

        static void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.That(Vector3.Distance(expected, actual), Is.LessThan(0.00001f));
        }

        [Test]
        public void SteamKeepsAuthoredNozzleOffsetScaleAndClickCollider()
        {
            var catalog = DefaultContent.Create();
            var state = RunningState(catalog);
            var machine = _root.AddComponent<MachineView>();
            var collider = _root.AddComponent<BoxCollider>();
            collider.size = new Vector3(3.2f, 1.4f, 1.6f);
            var rest = new Vector3(0.85f, 1.35f, -0.28f);
            var scale = new Vector3(0.12f, 0.18f, 0.15f);
            machine.SteamAnchor = Anchor("SteamAnchor", rest, scale);
            machine.SteamAnchor.gameObject.SetActive(false);

            // Direct EditMode rendering must work even though Awake never captured this reference.
            machine.Render(state, new FarmSimulation(catalog));

            Assert.IsTrue(machine.SteamAnchor.gameObject.activeSelf);
            AssertVector(rest + Vector3.up * 0.175f, machine.SteamAnchor.localPosition);
            AssertVector(scale * 0.9f, machine.SteamAnchor.localScale);
            AssertVector(new Vector3(3.2f, 1.4f, 1.6f), collider.size);
            AssertVector(Vector3.one, _root.transform.localScale);
        }

        [Test]
        public void SteamDoesNotDriftAndReturnsToRestBetweenBatches()
        {
            var catalog = DefaultContent.Create();
            var state = RunningState(catalog);
            var simulation = new FarmSimulation(catalog);
            var machine = _root.AddComponent<MachineView>();
            var rest = new Vector3(-0.6f, 1.9f, 0.3f);
            var scale = Vector3.one * 0.2f;
            machine.SteamAnchor = Anchor("SteamAnchor", rest, scale);
            for (int i = 0; i < 50; i++) machine.Render(state, simulation);
            AssertVector(rest + Vector3.up * 0.175f, machine.SteamAnchor.localPosition);

            state.Machine.BatchRunning = false;
            machine.Render(state, simulation);
            Assert.IsFalse(machine.SteamAnchor.gameObject.activeSelf);
            AssertVector(rest, machine.SteamAnchor.localPosition);
            AssertVector(scale, machine.SteamAnchor.localScale);

            state.Machine.BatchRunning = true;
            machine.Render(state, simulation);
            AssertVector(rest + Vector3.up * 0.175f, machine.SteamAnchor.localPosition);
        }

        [Test]
        public void NewSteamAnchorCapturesItsOwnRestTransform()
        {
            var catalog = DefaultContent.Create();
            var state = RunningState(catalog);
            var simulation = new FarmSimulation(catalog);
            var machine = _root.AddComponent<MachineView>();
            machine.SteamAnchor = Anchor("OldSteam", Vector3.zero, Vector3.one);
            machine.Render(state, simulation);
            var nextPosition = new Vector3(2f, 3f, -4f);
            var nextScale = Vector3.one * 0.3f;
            machine.SteamAnchor = Anchor("ReplacementSteam", nextPosition, nextScale);
            machine.Render(state, simulation);
            AssertVector(nextPosition + Vector3.up * 0.175f, machine.SteamAnchor.localPosition);
            AssertVector(nextScale * 0.9f, machine.SteamAnchor.localScale);
        }

        [Test]
        public void RenderingViewsCannotChangeSerializedGameProgress()
        {
            var catalog = DefaultContent.Create();
            var state = RunningState(catalog);
            state.RobotUnlocked = true;
            var plot = state.Plot(0);
            plot.CurrentCropId = DefaultContent.CropMint;
            plot.Phase = PlotPhase.Ready;
            plot.PendingYield = 3;
            var simulation = new FarmSimulation(catalog);
            var snapshot = new SaveSnapshot { BalanceVersion = catalog.Balance.Version, BuildId = "art-test", State = state };
            string before = SaveSerializer.Write(snapshot, catalog, false);

            var machine = _root.AddComponent<MachineView>();
            machine.SteamAnchor = Anchor("SteamAnchor", new Vector3(.8f, 1.3f, .2f), Vector3.one * .1f);
            var plotView = _root.AddComponent<PlotView>();
            plotView.PlotId = 0;
            plotView.CropVisuals = new[] { new CropVisual { CropId = DefaultContent.CropMint, Root = Anchor("Mint", Vector3.zero, Vector3.one).gameObject } };
            plotView.ReadyBadge = Anchor("ReadyBadge", Vector3.up, Vector3.one).gameObject;
            var helper = _root.AddComponent<HelperView>();
            helper.VisualRoot = Anchor("HelperVisual", Vector3.up, Vector3.one);
            for (int i = 0; i < 10; i++)
            {
                machine.Render(state, simulation);
                plotView.Render(state, simulation);
                helper.Render(state);
            }

            Assert.AreEqual(before, SaveSerializer.Write(snapshot, catalog, false));
            Assert.IsTrue(plotView.ReadyBadge.activeSelf);
            AssertVector(Vector3.one, _root.transform.localScale);
        }

        [Test]
        public void ConfiguredClipIsPlayedAndRepeatedSameCueUsesOneVoice()
        {
            var skin = ScriptableObject.CreateInstance<GardenSkin>();
            var clip = AudioClip.Create("custom-click", 48000, 1, 48000, false);
            try
            {
                skin.ClickClip = clip;
                var player = _root.AddComponent<SfxPlayer>();
                player.Configure(skin);
                player.PlayClick();
                for (int i = 0; i < 12; i++) player.PlayClick();
                var sources = _root.GetComponents<AudioSource>();
                Assert.AreEqual(SfxPlayer.MaximumVoices, sources.Length);
                Assert.AreEqual(1, sources.Count(source => source.clip == clip));
                Assert.IsTrue(sources.All(source => !source.playOnAwake && source.spatialBlend == 0f));
            }
            finally
            {
                Object.DestroyImmediate(skin);
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void FiveDifferentCuesStayWithinFourVoicesAndMuteStopsEveryVoice()
        {
            var player = _root.AddComponent<SfxPlayer>();
            player.Configure(null);
            player.PlayClick();
            player.OnPlanted(0, DefaultContent.CropMint, 0);
            player.OnHarvested(0, DefaultContent.CropMint, 3, false, 0);
            player.OnBatchCompleted(DefaultContent.RecipeMint, 9, 0);
            player.PlayUnlock();
            var sources = _root.GetComponents<AudioSource>();
            Assert.AreEqual(4, sources.Length);
            Assert.IsTrue(sources.Any(source => source.clip != null && source.clip.name == "sfx_upgrade"));
            player.Enabled = false;
            Assert.IsTrue(sources.All(source => source.volume == 0f && !source.isPlaying));
            Assert.AreEqual(0, PlayerPrefs.GetInt(SfxPlayer.VolumePrefKey, 1));
        }
    }
}
