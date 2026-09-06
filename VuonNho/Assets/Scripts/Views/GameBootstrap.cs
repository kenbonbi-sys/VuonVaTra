using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VuonNho.Core;
using VuonNho.Infrastructure;

namespace VuonNho.Views
{
    /// <summary>
    /// Noi GameSession voi scene: mot vong tick online, dinh tuyen click, va cac callback vong doi
    /// duoc gop thanh mot lan Suspend/Resume.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour, ISimulationListener
    {
        public Camera GameCamera;
        public GameHud Hud;
        public MachineView Machine;
        public HelperView Helper;
        public PlotView[] Plots;
        public DecorationLayer Decorations;
        public GardenSkin Skin;

        GameSession _session;
        FileSaveRepository _repository;
        FileTestLogger _logger;
        SystemClock _clock;
        SfxPlayer _sfx;
        bool _blocked;
        /// <summary>Cu click de dua cua so ve foreground khong duoc tinh la thao tac trong vuon.</summary>
        /// <summary>
        /// Cu click chi de dua cua so len foreground khong duoc tinh la thao tac trong game —
        /// ke ca thao tac tren UI. Chan theo thoi gian chu khong theo khung hinh, vi su kien
        /// chuot cua he dieu hanh co the toi sau callback focus mot vai khung hinh.
        /// </summary>
        const float IgnoreClicksAfterFocusSeconds = 0.25f;
        float _ignoreClicksUntilTime;
        GraphicRaycaster _hudRaycaster;

        bool ClicksAreBlocked
        {
            get { return Time.unscaledTime < _ignoreClicksUntilTime; }
        }

        /// <summary>Pha 2: id mon dang cho dat, va che do go. Ca hai la trang thai cua UI, khong vao save.</summary>
        string _placingDecorationId;
        bool _removingDecorations;

        public string PlacingDecorationId { get { return _placingDecorationId; } }
        public bool RemovingDecorations { get { return _removingDecorations; } }

        public GameSession Session { get { return _session; } }

        /// <summary>
        /// Do thoi gian nap de tach phan ung dung tu chiu trach nhiem khoi phan engine khoi dong
        /// (bao gom splash Unity, khong tat duoc o ban Personal).
        /// </summary>
        public static float EngineBootSeconds { get; private set; }
        public static float SessionLoadSeconds { get; private set; }

        void Awake()
        {
            EngineBootSeconds = Time.realtimeSinceStartup;
            Application.targetFrameRate = 60;

            var catalog = DefaultContent.Create();
            string buildId = Application.version + (Debug.isDebugBuild ? "-dev" : "-release");

            _clock = new SystemClock();
            _repository = new FileSaveRepository(Application.persistentDataPath);
            _logger = new FileTestLogger(Application.persistentDataPath, buildId, catalog.Balance.Version);
            _session = new GameSession(catalog, _clock, _repository, _logger, buildId);
            _logger.SimulationTimeProvider = delegate
            {
                return _session.State != null ? _session.State.SimulationTimeMs : -1;
            };
            _session.Diagnostic += OnDiagnostic;

            _sfx = gameObject.AddComponent<SfxPlayer>();
            _sfx.Configure(Skin);
            gameObject.AddComponent<QaScreenshot>();
            gameObject.AddComponent<QaChecklist>();
            _session.Simulation.AddListener(this);
            _session.Simulation.AddListener(_sfx);
            if (Helper != null) _session.Simulation.AddListener(Helper);

            // Phai dat truoc Bind: HUD dung toan bo nhan ngay trong Bind.
            if (Skin != null) UiFactory.SetFonts(Skin.BodyFont, Skin.DisplayFont);
            if (Hud != null)
            {
                Hud.Bind(_session, Skin);
                _hudRaycaster = Hud.GetComponent<GraphicRaycaster>();
            }
            // Cu click mo game cung khong duoc tinh: chan ngay tu khung hinh dau.
            _ignoreClicksUntilTime = Time.unscaledTime + IgnoreClicksAfterFocusSeconds;
            if (Decorations != null) Decorations.Bind(_session);

            var outcome = _session.Initialize();
            HandleLoadOutcome(outcome);
            SessionLoadSeconds = Time.realtimeSinceStartup - EngineBootSeconds;
        }

        void HandleLoadOutcome(LoadOutcome outcome)
        {
            switch (outcome)
            {
                case LoadOutcome.NewGame:
                    break;
                case LoadOutcome.LoadedPrimary:
                    break;
                case LoadOutcome.LoadedBackup:
                    if (Hud != null) Hud.ShowToast("File save chính lỗi, đã khôi phục từ bản backup.");
                    break;
                case LoadOutcome.BlockedIncompatible:
                    _blocked = true;
                    if (Hud != null)
                        Hud.ShowBlockedMessage(
                            "Không mở được tiến độ.\n\n" + _session.LastLoadError +
                            "\n\nBản save này được tạo bởi phiên bản game mới hơn. " +
                            "Hãy dùng bản game mới hơn, hoặc vào Cài đặt và chọn chơi lại từ đầu " +
                            "nếu bạn chấp nhận mất tiến độ.");
                    break;
                case LoadOutcome.BlockedCorrupt:
                    _blocked = true;
                    if (Hud != null)
                        Hud.ShowBlockedMessage(
                            "Không mở được tiến độ.\n\n" + (_session.LastLoadError ?? "File save hỏng.") +
                            "\n\nCả file chính và backup đều không đọc được. Game không tự xóa dữ liệu; " +
                            "vào Cài đặt và chọn chơi lại từ đầu nếu bạn chấp nhận mất tiến độ.");
                    break;
            }
        }

        void Update()
        {
            if (_session == null || _blocked || _session.State == null) return;

            // Tat raycaster cua HUD trong khoang cho, neu khong cu click lay focus se bam
            // trung nut dang nam duoi con tro — bao cao quay lai bi dong ngay khi vua hien.
            if (_hudRaycaster != null) _hudRaycaster.enabled = !ClicksAreBlocked;

            _session.Tick();
            RenderScene();
            if (Decorations != null) Decorations.RefreshIfChanged();
            HandleClick();
        }

        void RenderScene()
        {
            var state = _session.State;
            if (Plots != null)
            {
                for (int i = 0; i < Plots.Length; i++)
                    if (Plots[i] != null) Plots[i].Render(state, _session.Simulation);
            }
            if (Machine != null) Machine.Render(state, _session.Simulation);
            if (Helper != null) Helper.Render(state);
        }

        void HandleClick()
        {
            // Chuot phai luon la duong thoat khoi che do dat/go.
            if (Input.GetMouseButtonDown(1) && (_placingDecorationId != null || _removingDecorations))
            {
                CancelDecorationMode();
                if (Hud != null) Hud.Refresh();
                return;
            }

            if (!Input.GetMouseButtonDown(0)) return;
            if (ClicksAreBlocked) return;
            // Click len UI khong truyen xuong dat.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            var camera = GameCamera != null ? GameCamera : Camera.main;
            if (camera == null) return;

            RaycastHit hit;
            if (!Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out hit, 500f)) return;

            if (_placingDecorationId != null)
            {
                HandlePlacementClick(hit);
                return;
            }

            if (_removingDecorations)
            {
                var handle = hit.collider.GetComponentInParent<DecorationHandle>();
                if (handle != null)
                {
                    var removed = _session.RemoveDecoration(handle.Index);
                    if (Hud != null)
                        Hud.ShowToast(removed.Success ? "Đã gỡ và hoàn lại xu." : removed.FailureReason);
                }
                return;
            }

            var plotView = hit.collider.GetComponentInParent<PlotView>();
            if (plotView != null)
            {
                OnPlotClicked(plotView);
                return;
            }

            if (hit.collider.GetComponentInParent<MachineView>() != null && Hud != null)
                Hud.ShowToast(MachineHint());
        }

        // ---------------------------------------------------------------- trang tri (pha 2)

        /// <summary>Luoi 20 cm cho de dat thang hang ma khong can canh chinh ti mi.</summary>
        const int PlacementGridMm = 200;

        public void BeginPlacingDecoration(string definitionId)
        {
            _placingDecorationId = definitionId;
            _removingDecorations = false;
            if (Hud != null) Hud.ShowToast("Bấm vào khoảng trống trong vườn để đặt. Chuột phải để thoát.");
        }

        public void SetRemovingDecorations(bool removing)
        {
            _removingDecorations = removing;
            _placingDecorationId = null;
            if (Hud != null && removing) Hud.ShowToast("Bấm vào món muốn gỡ. Chuột phải để thoát.");
        }

        public void CancelDecorationMode()
        {
            _placingDecorationId = null;
            _removingDecorations = false;
        }

        void HandlePlacementClick(RaycastHit hit)
        {
            if (hit.collider.GetComponentInParent<GardenGround>() == null)
            {
                if (Hud != null) Hud.ShowToast("Chỉ đặt được trên khoảng đất trống.");
                return;
            }

            int xMm = SnapToGrid(hit.point.x);
            int zMm = SnapToGrid(hit.point.z);
            var result = _session.PlaceDecoration(_placingDecorationId, xMm, zMm, 0);

            if (!result.Success)
            {
                if (Hud != null) Hud.ShowToast(result.FailureReason);
                return;
            }
            if (_sfx != null) _sfx.PlayClick();
            // Giu nguyen che do de dat lien may mon, khong bat chon lai moi lan.
        }

        static int SnapToGrid(float metres)
        {
            int millimetres = Mathf.RoundToInt(metres * 1000f);
            return Mathf.RoundToInt((float)millimetres / PlacementGridMm) * PlacementGridMm;
        }

        string MachineHint()
        {
            var state = _session.State;
            string missingCropId;
            long missingAmount;
            if (_session.Simulation.TryGetMissingInput(state, out missingCropId, out missingAmount))
                return "Máy thiếu " + missingAmount + " " +
                       _session.Catalog.Crop(missingCropId).DisplayName +
                       ". Mở Kho để đổi công thức hoặc trồng thêm loại cây này.";
            return "Mở Kho để đổi công thức đang pha.";
        }

        void OnPlotClicked(PlotView plotView)
        {
            var plot = _session.State.Plot(plotView.PlotId);
            if (plot == null) return;

            if (plot.Phase == PlotPhase.Ready)
            {
                var result = _session.HarvestAndReplant(plotView.PlotId);
                if (!result.Success && Hud != null) Hud.ShowToast(result.FailureReason);
                return;
            }

            if (Hud != null) Hud.OpenPlotPopup(plotView.PlotId);
            if (_sfx != null) _sfx.PlayClick();
        }

        // ---------------------------------------------------------------- vong doi ung dung

        void OnApplicationPause(bool paused)
        {
            if (_session == null || _blocked) return;
            if (paused) _session.Suspend();
            else _session.ResumeFromBackground();
        }

        void OnApplicationFocus(bool focused)
        {
            if (_session == null || _blocked) return;
            if (!focused)
            {
                _session.Suspend();
                return;
            }
            _ignoreClicksUntilTime = Time.unscaledTime + IgnoreClicksAfterFocusSeconds;
            _session.ResumeFromBackground();
        }

        void OnApplicationQuit()
        {
            if (_session == null) return;
            _session.Suspend();
            _session.EndSession();
            if (_logger != null) _logger.Flush();
        }

        // ---------------------------------------------------------------- lenh tu HUD

        public string ExportTestData()
        {
            if (_logger == null) return "Chưa có log.";
            string destination = Path.Combine(Application.persistentDataPath,
                                              "vuon-nho-export-" + System.DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log");
            string error;
            if (_logger.TryExport(destination, out error))
                return "Đã xuất dữ liệu test: " + destination;
            return "Không xuất được dữ liệu test: " + error;
        }

        public void ResetProgress()
        {
            if (_session == null) return;
            _repository.DeleteAll();
            _session.StartNewGame();
            _blocked = false;
            if (Hud != null) Hud.Refresh();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void OnDiagnostic(string message)
        {
            Debug.LogWarning("[VuonNho] " + message);
            if (_logger != null) _logger.Log("diagnostic", "message", message);
        }

        // ---------------------------------------------------------------- phan hoi hinh anh

        public void OnCropReady(int plotId, string cropId, long atMs) { }

        public void OnHarvested(int plotId, string cropId, int amount, bool byRobot, long atMs)
        {
            var view = FindPlotView(plotId);
            if (view != null) view.PlayHarvestFeedback();
        }

        public void OnPlanted(int plotId, string cropId, long atMs)
        {
            var view = FindPlotView(plotId);
            if (view != null) view.PlayPlantPop();
        }

        public void OnBatchStarted(string recipeId, long atMs) { }
        public void OnBatchCompleted(string recipeId, long coins, long atMs) { }

        PlotView FindPlotView(int plotId)
        {
            if (Plots == null) return null;
            for (int i = 0; i < Plots.Length; i++)
                if (Plots[i] != null && Plots[i].PlotId == plotId) return Plots[i];
            return null;
        }
    }
}
