using UnityEngine;
using VuonNho.Core;

namespace VuonNho.Views
{
    /// <summary>Nhan Idle/Running/Waiting va tien do me. Khong tu tru nguyen lieu hay cong xu.</summary>
    public sealed class MachineView : MonoBehaviour
    {
        public Transform VisualRoot;
        public Transform SteamAnchor;
        public Renderer StatusRenderer;
        public Renderer StationRenderer;

        public static readonly Color StatusIdle = new Color(0.55f, 0.55f, 0.55f);
        public static readonly Color StatusRunning = new Color(0.45f, 0.85f, 0.50f);
        public static readonly Color StatusWaiting = new Color(0.90f, 0.65f, 0.30f);

        float _steamPhase;
        Transform _cachedSteamAnchor;
        Vector3 _steamRestPosition;
        Vector3 _steamRestScale;

        void CacheSteamRestTransform()
        {
            // Lazy capture also supports Editor previews and references assigned after Awake.
            if (SteamAnchor == null || _cachedSteamAnchor == SteamAnchor) return;
            _cachedSteamAnchor = SteamAnchor;
            _steamRestPosition = SteamAnchor.localPosition;
            _steamRestScale = SteamAnchor.localScale;
        }

        public void Render(GameState state, FarmSimulation simulation)
        {
            if (StationRenderer != null) StationRenderer.material.color = GardenPalette.Wood;

            bool running = state.Machine.BatchRunning;
            string missingCropId;
            long missingAmount;
            bool waiting = !running && simulation.TryGetMissingInput(state, out missingCropId, out missingAmount);

            if (StatusRenderer != null)
                StatusRenderer.material.color = running ? StatusRunning : (waiting ? StatusWaiting : StatusIdle);

            if (SteamAnchor != null)
            {
                CacheSteamRestTransform();
                SteamAnchor.gameObject.SetActive(running);
                if (running)
                {
                    float progress = BatchProgress(state);
                    float lift = 0.05f + 0.25f * progress;
                    SteamAnchor.localPosition = _steamRestPosition + Vector3.up * lift;
                    float pulse = 0.9f + 0.2f * Mathf.Sin(_steamPhase * 4f);
                    SteamAnchor.localScale = _steamRestScale * pulse;
                }
                else
                {
                    SteamAnchor.localPosition = _steamRestPosition;
                    SteamAnchor.localScale = _steamRestScale;
                }
            }
        }

        public static float BatchProgress(GameState state)
        {
            var machine = state.Machine;
            if (!machine.BatchRunning) return 0f;
            long span = machine.BatchFinishAtMs - machine.BatchStartAtMs;
            if (span <= 0) return 1f;
            long elapsed = state.SimulationTimeMs - machine.BatchStartAtMs;
            return Mathf.Clamp01((float)elapsed / span);
        }

        void Update()
        {
            _steamPhase += Time.deltaTime;
        }
    }
}
