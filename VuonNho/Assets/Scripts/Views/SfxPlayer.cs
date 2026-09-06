using UnityEngine;
using VuonNho.Core;

namespace VuonNho.Views
{
    /// <summary>Five optional skin cues, synthesized fallback, and a fixed four-voice pool.</summary>
    public sealed class SfxPlayer : MonoBehaviour, ISimulationListener
    {
        public const string VolumePrefKey = "vuonnho.sfx.enabled";
        public const int MaximumVoices = 4;
        const double CoalesceSeconds = 0.1;
        const float VoiceGain = 0.45f;

        enum Cue { Click, Plant, Harvest, Brew, Upgrade, Count }

        AudioSource[] _voices;
        AudioClip[] _fallbacks;
        AudioClip[] _clips;
        readonly double[] _voiceEnds = new double[MaximumVoices];
        readonly double[] _nextCueAt = new double[(int)Cue.Count];

        void Awake() { EnsureInitialized(); }

        void EnsureInitialized()
        {
            if (_voices != null) return;
            _voices = new AudioSource[MaximumVoices];
            for (int i = 0; i < _voices.Length; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.volume = Enabled ? VoiceGain : 0f;
                _voices[i] = source;
                _voiceEnds[i] = double.NegativeInfinity;
            }

            _fallbacks = new[]
            {
                Tone("sfx_click", 0.06f, 620f, 520f, 0f, 0.35f),
                Tone("sfx_plant", 0.12f, 300f, 520f, 0.12f, 0.35f),
                Tone("sfx_harvest", 0.16f, 480f, 760f, 0.08f, 0.40f),
                Tone("sfx_brew", 0.18f, 880f, 1320f, 0f, 0.35f),
                Tone("sfx_upgrade", 0.35f, 420f, 880f, 0f, 0.45f)
            };
            _clips = (AudioClip[])_fallbacks.Clone();
            for (int i = 0; i < _nextCueAt.Length; i++) _nextCueAt[i] = double.NegativeInfinity;
        }

        public void Configure(GardenSkin skin)
        {
            EnsureInitialized();
            _clips[(int)Cue.Click] = skin != null && skin.ClickClip != null ? skin.ClickClip : _fallbacks[(int)Cue.Click];
            _clips[(int)Cue.Plant] = skin != null && skin.PlantClip != null ? skin.PlantClip : _fallbacks[(int)Cue.Plant];
            _clips[(int)Cue.Harvest] = skin != null && skin.HarvestClip != null ? skin.HarvestClip : _fallbacks[(int)Cue.Harvest];
            _clips[(int)Cue.Brew] = skin != null && skin.BrewClip != null ? skin.BrewClip : _fallbacks[(int)Cue.Brew];
            _clips[(int)Cue.Upgrade] = skin != null && skin.UpgradeClip != null ? skin.UpgradeClip : _fallbacks[(int)Cue.Upgrade];
        }

        public bool Enabled
        {
            get { return PlayerPrefs.GetInt(VolumePrefKey, 1) == 1; }
            set
            {
                PlayerPrefs.SetInt(VolumePrefKey, value ? 1 : 0);
                PlayerPrefs.Save();
                if (_voices == null) return;
                for (int i = 0; i < _voices.Length; i++)
                {
                    _voices[i].volume = value ? VoiceGain : 0f;
                    if (!value)
                    {
                        _voices[i].Stop();
                        _voiceEnds[i] = double.NegativeInfinity;
                    }
                }
            }
        }

        public void PlayClick() { Play(Cue.Click); }
        public void PlayUnlock() { Play(Cue.Upgrade); }

        void Play(Cue cue)
        {
            EnsureInitialized();
            if (!Enabled) return;
            int cueIndex = (int)cue;
            var clip = _clips[cueIndex];
            double now = Time.realtimeSinceStartupAsDouble;
            if (clip == null || now < _nextCueAt[cueIndex]) return;
            _nextCueAt[cueIndex] = now + CoalesceSeconds;

            int voiceIndex = 0;
            for (int i = 0; i < _voices.Length; i++)
            {
                if (_voiceEnds[i] <= now) { voiceIndex = i; break; }
                if (_voiceEnds[i] < _voiceEnds[voiceIndex]) voiceIndex = i;
            }
            // Replace the voice nearest completion when all four are busy.
            var voice = _voices[voiceIndex];
            voice.Stop();
            voice.clip = clip;
            voice.Play();
            _voiceEnds[voiceIndex] = now + clip.length;
        }

        static AudioClip Tone(string name, float durationSeconds, float startHz, float endHz,
                              float noiseAmount, float gain)
        {
            const int sampleRate = 48000;
            int sampleCount = Mathf.Max(16, Mathf.RoundToInt(durationSeconds * sampleRate));
            var data = new float[sampleCount];
            // Stable seed instead of runtime-dependent string.GetHashCode().
            int seed = 17;
            foreach (char letter in name) seed = unchecked(seed * 31 + letter);
            var random = new System.Random(seed);
            float phase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / (sampleCount - 1);
                float frequency = Mathf.Lerp(startHz, endHz, t);
                phase += 2f * Mathf.PI * frequency / sampleRate;
                float attack = Mathf.Clamp01(i / (sampleRate * 0.004f));
                float envelope = attack * Mathf.Pow(1f - t, 2.2f);
                float noise = noiseAmount > 0f ? ((float)random.NextDouble() * 2f - 1f) * noiseAmount : 0f;
                data[i] = (Mathf.Sin(phase) + noise) * envelope * gain;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        void OnDestroy()
        {
            if (_fallbacks == null) return;
            foreach (var clip in _fallbacks)
            {
                if (clip == null) continue;
                if (Application.isPlaying) Destroy(clip);
                else DestroyImmediate(clip);
            }
        }

        public void OnCropReady(int plotId, string cropId, long atMs) { }
        public void OnHarvested(int plotId, string cropId, int amount, bool byRobot, long atMs) { Play(Cue.Harvest); }
        public void OnPlanted(int plotId, string cropId, long atMs) { Play(Cue.Plant); }
        public void OnBatchStarted(string recipeId, long atMs) { }
        public void OnBatchCompleted(string recipeId, long coins, long atMs) { Play(Cue.Brew); }
    }
}
