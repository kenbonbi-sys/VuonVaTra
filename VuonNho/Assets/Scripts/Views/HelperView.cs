using UnityEngine;
using VuonNho.Core;

namespace VuonNho.Views
{
    /// <summary>
    /// Robot noi: 0 rig, 0 clip, chuyen dong bang transform. Toc do di khong anh huong nang suat.
    /// Chi bieu dien mot so thao tac, khong tru nguyen lieu hay cong xu.
    /// </summary>
    public sealed class HelperView : MonoBehaviour, ISimulationListener
    {
        public Transform VisualRoot;
        public Renderer BodyRenderer;
        public Renderer FaceRenderer;
        public GameObject SleepingHint;

        const float BobAmplitude = 0.045f;
        const float BobPeriodSeconds = 2.5f;
        const float ReactDurationSeconds = 0.30f;

        Transform _cachedVisualRoot;
        Vector3 _restLocalPosition;
        Quaternion _restLocalRotation = Quaternion.identity;
        float _phase;
        float _reactEndTime = -1f;
        bool _awake;

        /// <summary>
        /// Giu lai vi tri va HUONG da dat trong scene. Model that co mat o mot phia, nen scene
        /// phai xoay robot cho mat quay ve nguoi choi; animation nghieng chi duoc cong them vao
        /// huong do chu khong duoc ep ve identity.
        /// </summary>
        void CacheRestTransform()
        {
            if (VisualRoot == null || _cachedVisualRoot == VisualRoot) return;
            _cachedVisualRoot = VisualRoot;
            _restLocalPosition = VisualRoot.localPosition;
            _restLocalRotation = VisualRoot.localRotation;
        }

        void Awake()
        {
            CacheRestTransform();
        }

        [Tooltip("Robot quay đầu nhanh cỡ nào khi đổi hướng, độ mỗi giây.")]
        public float TurnSpeed = 420f;

        /// <summary>
        /// Dua robot toi dung cho ma GameState noi no dang o.
        ///
        /// Quang dang di khong duoc luu trong save: chi can cho xuat phat, o dang nham va moc toi
        /// noi la suy ra du. Lam vay thi hinh anh khong bao gio troi khoi mo phong — nap lai giua
        /// mot chuyen di, robot van dung dung cho no phai dung.
        /// </summary>
        public void Render(GameState state, FarmSimulation simulation)
        {
            _awake = state.RobotUnlocked;
            MoveToStatePosition(state, simulation);
            if (BodyRenderer != null)
                BodyRenderer.material.color = _awake
                    ? GardenPalette.Robot
                    : GardenPalette.Robot * 0.55f;
            if (FaceRenderer != null)
                FaceRenderer.material.color = _awake
                    ? new Color(0.35f, 0.85f, 0.95f)
                    : new Color(0.30f, 0.30f, 0.32f);
            if (SleepingHint != null && SleepingHint.activeSelf == _awake)
                SleepingHint.SetActive(!_awake);
        }

        void MoveToStatePosition(GameState state, FarmSimulation simulation)
        {
            if (simulation == null) return;

            var from = Ground(state.RobotXMm, state.RobotZMm);
            var to = from;

            if (state.RobotTargetPlotId >= 0)
            {
                to = Ground(simulation.PlotXMm(state.RobotTargetPlotId),
                            simulation.PlotZMm(state.RobotTargetPlotId));

                // Moc toi noi tru thoi gian dung lai thu, tru quang di, ra moc xuat phat.
                long harvestMs = simulation.Catalog.Balance.RobotHarvestMs;
                long travelMs = simulation.TravelMsTo(state, state.RobotTargetPlotId);
                long arriveAtMs = state.RobotReadyAtMs - harvestMs;
                long departAtMs = arriveAtMs - travelMs;

                float progress = travelMs <= 0
                    ? 1f
                    : Mathf.Clamp01((state.SimulationTimeMs - departAtMs) / (float)travelMs);
                to = Vector3.Lerp(from, to, progress);
            }

            var position = transform.position;
            transform.position = new Vector3(to.x, position.y, to.z);

            // Quay ve huong dang di. Duoi nguong nay thi coi nhu dang dung yen, khong quay lung tung.
            var heading = to - position;
            heading.y = 0f;
            if (heading.sqrMagnitude > 0.0004f)
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, Quaternion.LookRotation(heading, Vector3.up),
                    TurnSpeed * Time.deltaTime);
        }

        static Vector3 Ground(int xMm, int zMm)
        {
            return new Vector3(xMm / 1000f, 0f, zMm / 1000f);
        }

        void Update()
        {
            if (VisualRoot == null) return;
            CacheRestTransform();

            _phase += Time.deltaTime;
            float bob = _awake
                ? Mathf.Sin(_phase * (2f * Mathf.PI / BobPeriodSeconds)) * BobAmplitude
                : 0f;
            VisualRoot.localPosition = _restLocalPosition + new Vector3(0f, bob, 0f);

            // Nghieng ngan khi vua thu, khong xep hang phan ung dai.
            if (_reactEndTime > 0f && Time.time < _reactEndTime)
            {
                float t = 1f - (_reactEndTime - Time.time) / ReactDurationSeconds;
                VisualRoot.localRotation =
                    _restLocalRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI) * -14f);
            }
            else if (VisualRoot.localRotation != _restLocalRotation)
            {
                VisualRoot.localRotation = _restLocalRotation;
                _reactEndTime = -1f;
            }
        }

        public void OnCropReady(int plotId, string cropId, long atMs) { }

        public void OnHarvested(int plotId, string cropId, int amount, bool byRobot, long atMs)
        {
            if (byRobot) _reactEndTime = Time.time + ReactDurationSeconds;
        }

        public void OnPlanted(int plotId, string cropId, long atMs) { }
        public void OnBatchStarted(string recipeId, long atMs) { }
        public void OnBatchCompleted(string recipeId, long coins, long atMs) { }
        public void OnPestAppeared(int plotId, string cropId, long atMs) { }
        public void OnStationStarted(string stageId, string cropId, long atMs) { }
        public void OnStationCompleted(string stageId, string cropId, int amount, long atMs) { }
        public void OnWagesPaid(long coins, int paid, int unpaid, long atMs) { }

        public void OnBusinessEvent(string kind, string detail, long coins, long atMs) { }
    }
}
