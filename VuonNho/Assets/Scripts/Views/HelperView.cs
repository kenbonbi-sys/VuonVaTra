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

        public void Render(GameState state)
        {
            _awake = state.RobotUnlocked;
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
    }
}
