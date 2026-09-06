using System;
using UnityEngine;
using VuonNho.Core;

namespace VuonNho.Views
{
    /// <summary>
    /// Mot loai cay truong thanh tren o dat. Model rieng cho tung cay; hai truong con
    /// chi dung khi phan hinh la primitive hoac model muon doi mau luc chin.
    /// </summary>
    [Serializable]
    public sealed class CropVisual
    {
        public string CropId;
        public GameObject Root;
        public Renderer FoliageRenderer;
        public GameObject ReadyAccents;
    }

    /// <summary>
    /// Dung hinh tu state. Khong tu cong vat pham hay xu.
    /// Root giu scale 1 va collider co dinh, khong to nho theo cay; chi VisualRoot doi.
    /// </summary>
    public sealed class PlotView : MonoBehaviour
    {
        public int PlotId;
        public Transform VisualRoot;
        public Transform CropAnchor;

        [Tooltip("Mầm chung cho mọi loại cây.")]
        public GameObject SeedlingVisual;

        [Tooltip("Chỉ gán khi mầm là primitive; model thật tự có màu.")]
        public Renderer SeedlingRenderer;

        [Tooltip("Mỗi loại cây một model trưởng thành.")]
        public CropVisual[] CropVisuals = new CropVisual[0];

        public GameObject ReadyBadge;
        public Renderer ReadyBadgeRenderer;

        [Tooltip("Bui co dai; to nho theo luong co cua o.")]
        public Transform WeedTufts;

        [Tooltip("Hien khi o dang co sau benh.")]
        public GameObject PestBadge;

        [Tooltip("Chỉ gán khi mặt đất là primitive; ô khóa được tô màu tối.")]
        public Renderer SoilRenderer;

        [Tooltip("Hiện khi ô chưa mở khóa. Dùng khi bộ art có riêng hình ô khóa.")]
        public GameObject LockedOverlay;

        [Tooltip("Ba hạt/lá dùng lại cho phản hồi thu; không chứa logic kinh tế.")]
        public Transform HarvestFeedback;

        /// <summary>Duoi muc nay thi co chua dang ke; hien bui co li ti chi lam nhieu mat.</summary>
        const int WeedVisibleThreshold = 12;

        const float MatureStageThreshold = 0.34f;
        const float PopDurationSeconds = 0.20f;

        float _popEndTime = -1f;
        float _readySpin;
        float _harvestEndTime = -1f;
        PlotPhase _lastPhase = PlotPhase.Locked;

        public PlotPhase LastPhase { get { return _lastPhase; } }

        void RenderGround(PlotState plot)
        {
            if (WeedTufts != null)
            {
                bool weedy = plot.Unlocked && plot.Weeds >= WeedVisibleThreshold;
                SetActive(WeedTufts.gameObject, weedy);
                if (weedy)
                {
                    // Cao dan tu 40% den 100%: o vua chom co va o day co phai nhin ra khac nhau.
                    float grown = 0.4f + 0.6f * Mathf.Clamp01(plot.Weeds / 100f);
                    WeedTufts.localScale = new Vector3(grown, grown, grown);
                }
            }

            SetActive(PestBadge, plot.Unlocked && plot.PestActive);
        }

        public void Render(GameState state, FarmSimulation simulation)
        {
            var plot = state.Plot(PlotId);
            if (plot == null) return;

            // Do phi doc thang tren mat dat: dat tot thi tham, dat bac mau thi nhat di. Nguoi
            // choi liec ca vuon la thay o nao can bon ma khong phai mo tung popup.
            if (SoilRenderer != null)
            {
                SoilRenderer.material.color = plot.Unlocked
                    ? Color.Lerp(GardenPalette.SoilPoor, GardenPalette.Soil, plot.Fertility / 100f)
                    : GardenPalette.SoilLocked;
            }
            if (LockedOverlay != null) SetActive(LockedOverlay, !plot.Unlocked);

            // Co dai va sau benh khong phu thuoc vao viec o co cay hay khong: o trong bo do
            // van moc co, va do chinh la cai nguoi choi phai thay.
            RenderGround(plot);

            bool hasCrop = plot.Unlocked &&
                           (plot.Phase == PlotPhase.Growing || plot.Phase == PlotPhase.Ready);
            if (!hasCrop)
            {
                SetActive(SeedlingVisual, false);
                HideAllCropVisuals();
                SetActive(ReadyBadge, false);
                _lastPhase = plot.Phase;
                return;
            }

            float progress = Progress(plot, state.SimulationTimeMs);
            bool ready = plot.Phase == PlotPhase.Ready;
            bool showMature = ready || progress >= MatureStageThreshold;

            Color body = GardenPalette.CropBody(plot.CurrentCropId);
            Color accent = GardenPalette.CropReadyAccent(plot.CurrentCropId);

            SetActive(SeedlingVisual, !showMature);
            if (SeedlingRenderer != null) SeedlingRenderer.material.color = body;

            var active = FindCropVisual(plot.CurrentCropId);
            for (int i = 0; i < CropVisuals.Length; i++)
            {
                var visual = CropVisuals[i];
                if (visual == null || visual.Root == null) continue;
                SetActive(visual.Root, visual == active && showMature);
            }

            if (active != null && showMature && active.Root != null)
            {
                // Ba trang thai hinh anh: mam, giua vu, chin. Giua vu lon dan de nhin ra dang lon.
                float t = ready ? 1f : Mathf.InverseLerp(MatureStageThreshold, 1f, progress);
                float scale = Mathf.Lerp(0.55f, 1f, t);
                active.Root.transform.localScale = new Vector3(scale, scale, scale);

                if (active.FoliageRenderer != null)
                    active.FoliageRenderer.material.color = ready ? accent : body;
                if (active.ReadyAccents != null) SetActive(active.ReadyAccents, ready);
            }

            SetActive(ReadyBadge, ready);
            if (ReadyBadgeRenderer != null) ReadyBadgeRenderer.material.color = accent;

            _lastPhase = plot.Phase;
        }

        CropVisual FindCropVisual(string cropId)
        {
            CropVisual fallback = null;
            for (int i = 0; i < CropVisuals.Length; i++)
            {
                var visual = CropVisuals[i];
                if (visual == null || visual.Root == null) continue;
                if (fallback == null) fallback = visual;
                if (string.Equals(visual.CropId, cropId, StringComparison.Ordinal)) return visual;
            }
            // Cay chua co model rieng van phai nhin thay duoc mot cai gi do.
            return fallback;
        }

        void HideAllCropVisuals()
        {
            for (int i = 0; i < CropVisuals.Length; i++)
                if (CropVisuals[i] != null) SetActive(CropVisuals[i].Root, false);
        }

        public static float Progress(PlotState plot, long simulationTimeMs)
        {
            if (plot.Phase == PlotPhase.Ready) return 1f;
            if (plot.Phase != PlotPhase.Growing) return 0f;
            long span = plot.FinishAtMs - plot.StartAtMs;
            if (span <= 0) return 1f;
            long elapsed = simulationTimeMs - plot.StartAtMs;
            return Mathf.Clamp01((float)elapsed / span);
        }

        /// <summary>Chi scale VisualRoot, khong scale collider.</summary>
        public void PlayPlantPop()
        {
            _popEndTime = Time.time + PopDurationSeconds;
        }

        public void PlayHarvestFeedback()
        {
            if (HarvestFeedback == null) return;
            _harvestEndTime = Time.time + 0.3f;
            HarvestFeedback.gameObject.SetActive(true);
        }

        void Update()
        {
            if (HarvestFeedback != null && HarvestFeedback.gameObject.activeSelf)
            {
                float t = 1f - (_harvestEndTime - Time.time) / 0.3f;
                if (t >= 1f || _harvestEndTime < 0f) HarvestFeedback.gameObject.SetActive(false);
                else for (int i = 0; i < HarvestFeedback.childCount; i++)
                {
                    var leaf = HarvestFeedback.GetChild(i);
                    float angle = (i * 120f + 20f) * Mathf.Deg2Rad;
                    leaf.localPosition = new Vector3(Mathf.Cos(angle) * t * 0.42f,
                        0.5f + 0.35f * Mathf.Sin(t * Mathf.PI * 0.75f), Mathf.Sin(angle) * t * 0.42f);
                    leaf.localScale = new Vector3(0.10f, 0.05f, 0.16f) * (1f - t);
                }
            }

            if (VisualRoot != null)
            {
                if (_popEndTime > 0f && Time.time < _popEndTime)
                {
                    float t = 1f - (_popEndTime - Time.time) / PopDurationSeconds;
                    float bump = 1f + 0.18f * Mathf.Sin(t * Mathf.PI);
                    VisualRoot.localScale = new Vector3(bump, bump, bump);
                }
                else if (VisualRoot.localScale != Vector3.one)
                {
                    VisualRoot.localScale = Vector3.one;
                    _popEndTime = -1f;
                }
            }

            if (ReadyBadge != null && ReadyBadge.activeSelf)
            {
                _readySpin += Time.deltaTime * 90f;
                ReadyBadge.transform.localRotation = Quaternion.Euler(0f, _readySpin, 0f);
            }
        }

        static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active) target.SetActive(active);
        }
    }
}
