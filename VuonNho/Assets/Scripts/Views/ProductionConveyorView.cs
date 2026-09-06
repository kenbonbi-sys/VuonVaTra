using UnityEngine;
using VuonNho.Core;

namespace VuonNho.Views
{
    /// <summary>
    /// Presentation only: the line reflects station ownership/running state. Cargo is decorative
    /// and never transfers inventory, advances a batch, or changes the production simulation.
    /// </summary>
    public sealed class ProductionConveyorView : MonoBehaviour
    {
        public string SourceStageId;
        public string DestinationStageId;
        public GameObject VisualRoot;
        public Transform[] Cargo = new Transform[0];
        public Vector3[] Path = new Vector3[0];
        public float BeltSpeed = 0.32f;
        public float CargoHeight = 0.60f;

        public bool IsVisible { get; private set; }
        public bool IsRunning { get; private set; }

        GameBootstrap _bootstrap;
        float _nextBootstrapSearch;
        float _pathLength;
        float _travelled;

        void Awake()
        {
            MeasurePath();
            Render(null);
            PositionCargo();
        }

        void Update()
        {
            if (_bootstrap == null && Time.unscaledTime >= _nextBootstrapSearch)
            {
                _bootstrap = FindFirstObjectByType<GameBootstrap>();
                _nextBootstrapSearch = Time.unscaledTime + 0.5f;
            }

            var session = _bootstrap != null ? _bootstrap.Session : null;
            Render(session != null ? session.State : null);
            if (!IsRunning || _pathLength <= 0f) return;
            _travelled = Mathf.Repeat(_travelled + Time.deltaTime * BeltSpeed, _pathLength);
            PositionCargo();
        }

        /// <summary>Physical blockers are children of VisualRoot, so hidden links cannot block walking.</summary>
        public void Render(GameState state)
        {
            var source = state != null ? state.Station(SourceStageId) : null;
            var destination = state != null ? state.Station(DestinationStageId) : null;
            IsVisible = source != null && destination != null && source.Owned && destination.Owned;
            IsRunning = IsVisible && source.Running;
            if (VisualRoot != null && VisualRoot.activeSelf != IsVisible)
                VisualRoot.SetActive(IsVisible);
        }

        void MeasurePath()
        {
            _pathLength = 0f;
            if (Path == null) return;
            for (int i = 1; i < Path.Length; i++)
                _pathLength += Vector3.Distance(Path[i - 1], Path[i]);
        }

        void PositionCargo()
        {
            if (Cargo == null || Cargo.Length == 0 || Path == null || Path.Length < 2 || _pathLength <= 0f)
                return;

            for (int cargoIndex = 0; cargoIndex < Cargo.Length; cargoIndex++)
            {
                var cargo = Cargo[cargoIndex];
                if (cargo == null) continue;
                float distance = Mathf.Repeat(_travelled + _pathLength * (cargoIndex + 0.5f) / Cargo.Length,
                                              _pathLength);
                for (int segment = 1; segment < Path.Length; segment++)
                {
                    var direction = Path[segment] - Path[segment - 1];
                    float length = direction.magnitude;
                    if (length <= 0.001f) continue;
                    if (distance <= length || segment == Path.Length - 1)
                    {
                        cargo.localPosition = Path[segment - 1] + direction * Mathf.Clamp01(distance / length)
                                              + Vector3.up * CargoHeight;
                        cargo.localRotation = Quaternion.LookRotation(direction, Vector3.up);
                        break;
                    }
                    distance -= length;
                }
            }
        }
    }
}
