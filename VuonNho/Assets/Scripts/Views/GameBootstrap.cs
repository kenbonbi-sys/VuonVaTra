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
        public CameraRig Rig;
        public CharacterView Character;
        public ClickMarker Marker;
        public PlacementPreview Preview;
        public StationView[] Stations;

        GameSession _session;
        FileSaveRepository _repository;
        FileTestLogger _logger;
        SystemClock _clock;
        SfxPlayer _sfx;
        WorkshopCrewView _crew;
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
            if (Skin != null) UiFactory.SetFonts(Skin.BodyFont, Skin.DisplayFont, Skin.SymbolFont);
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
            _crew = gameObject.AddComponent<WorkshopCrewView>();
            _crew.Bind(Stations);
            gameObject.AddComponent<StationHoverController>().Bootstrap = this;
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
            UpdatePlacementPreview();
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
            if (Stations != null)
                for (int i = 0; i < Stations.Length; i++)
                    if (Stations[i] != null) Stations[i].Render(state);
            if (_crew != null) _crew.Render(state);
            if (Helper != null) Helper.Render(state, _session.Simulation);
        }

        void HandleClick()
        {
            if (_placingDecorationId != null || _removingDecorations)
            {
                if (_placingDecorationId != null && Input.GetKeyDown(FlipKey) && Preview != null)
                    Preview.Flip();
                HandleDecorationRightMouse();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                HandleWalkCommand();
                return;
            }

            // Thao tac cua chuot trai chot luc THA chu khong luc nhan: luc nhan chua biet day
            // la mot cu bam hay la khoi dau cua mot lan keo man hinh.
            if (!Input.GetMouseButtonUp(0)) return;
            if (Rig != null && Rig.ClickWasDrag) return;
            if (ClicksAreBlocked) return;
            TryWorldClick(Input.mousePosition);
        }

        /// <summary>
        /// Xu ly mot cu bam vao vuon tai mot diem tren man hinh. Tach khoi <see cref="HandleClick"/>
        /// de bo kiem tra trong build goi duoc: doan quyet dinh "bam hay keo" nam o tren, con doan
        /// lam viec nam o day, va chi doan nay moi doi duoc trang thai van.
        /// </summary>
        /// <param name="ignorePointerOverUi">
        /// Bo qua cua "con tro dang o tren HUD". Chi bo kiem tra dung: no chay trong mot cua so ma
        /// chuot that cua nguoi dung co the dang nam bat cu dau, ke ca tren thanh HUD, va mot phep
        /// do khong duoc phu thuoc vao cho de tay cua nguoi ngoi truoc may.
        /// </param>
        /// <returns>Tia co cham vao thu gi trong vuon hay khong.</returns>
        public bool TryWorldClick(Vector3 screenPosition, bool ignorePointerOverUi = false)
        {
            if (!ignorePointerOverUi && PointerIsOverUi()) return false;

            var camera = GameCamera != null ? GameCamera : Camera.main;
            if (camera == null) return false;

            RaycastHit hit;
            if (!Physics.Raycast(camera.ScreenPointToRay(screenPosition), out hit, 500f)) return false;

            if (_placingDecorationId != null)
            {
                HandlePlacementClick(hit);
                return true;
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
                return true;
            }

            var plotView = hit.collider.GetComponentInParent<PlotView>();
            if (plotView != null)
            {
                OnPlotClicked(plotView);
                return true;
            }

            if (hit.collider.GetComponentInParent<MachineView>() != null && Hud != null)
                Hud.ShowToast(MachineHint());
            if (hit.collider.GetComponentInParent<StationView>() != null && Hud != null)
                Hud.ShowWorkshopPanel();
            return true;
        }

        /// <summary>So lenh di da nhan. Bo kiem tra dung de biet co input la lot vao hay khong.</summary>
        public int WalkCommandCount { get; private set; }

        /// <summary>Chuot phai len dat: nhan vat di toi diem vua bam.</summary>
        void HandleWalkCommand()
        {
            TryWalkCommand(Input.mousePosition);
        }

        static bool PointerIsOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        /// <summary>
        /// Ra lenh di tai mot diem tren man hinh. Tach khoi <see cref="HandleWalkCommand"/> vi
        /// cung mot ly do da tach <see cref="TryWorldClick"/>: bo kiem tra trong build phai goi
        /// duoc dung doan ma nay chu khong phai mot ban chep gan giong.
        /// </summary>
        /// <returns>Tia co cham dat va nhan vat co nhan lenh hay khong.</returns>
        public bool TryWalkCommand(Vector3 screenPosition, bool ignorePointerOverUi = false)
        {
            if (Character == null) return false;
            if (ClicksAreBlocked) return false;
            if (!ignorePointerOverUi && PointerIsOverUi()) return false;

            var camera = GameCamera != null ? GameCamera : Camera.main;
            if (camera == null) return false;

            RaycastHit hit;
            if (!Physics.Raycast(camera.ScreenPointToRay(screenPosition), out hit, 500f)) return false;
            WalkCommandCount++;
            Character.WalkTo(hit.point);

            // Bao lai o DICH chu khong o cho con tro: bam ra ngoai vuon thi nhan vat dung o mep,
            // va vong tron phai nam dung cho no se dung chu khong noi mot chuyen khac.
            if (Marker != null) Marker.Show(Character.Destination);
            return true;
        }

        // ---------------------------------------------------------------- trang tri (pha 2)

        /// <summary>Luoi 20 cm cho de dat thang hang ma khong can canh chinh ti mi.</summary>
        const int PlacementGridMm = 200;

        public void BeginPlacingDecoration(string definitionId)
        {
            _placingDecorationId = definitionId;
            _removingDecorations = false;
            if (Preview != null) Preview.Begin(definitionId);
            if (Hud != null)
                Hud.ShowToast("Bấm để đặt · R xoay 180° · giữ chuột phải kéo ngang để xoay · " +
                              "chuột phải để thoát.");
        }

        public void SetRemovingDecorations(bool removing)
        {
            _removingDecorations = removing;
            _placingDecorationId = null;
            if (Preview != null) Preview.Cancel();
            if (Hud != null && removing) Hud.ShowToast("Bấm vào món muốn gỡ. Chuột phải để thoát.");
        }

        public void CancelDecorationMode()
        {
            _placingDecorationId = null;
            _removingDecorations = false;
            if (Preview != null) Preview.Cancel();
        }

        /// <summary>Phim doi mat truoc ra sau. Mot phim rieng vi day la lan xoay hay dung nhat.</summary>
        const KeyCode FlipKey = KeyCode.R;

        const float RotateDegreesPerPixel = 0.9f;
        const float RotateDragThresholdPixels = 6f;

        float _rotatePressScreenX;
        bool _rotateWasDrag;

        /// <summary>
        /// Trong che do dat/go, chuot phai mang hai nghia. Giu va keo ngang la xoay mon dang cam;
        /// nhan roi tha ngay tai cho la thoat che do.
        ///
        /// Phan biet luc THA chu khong luc nhan, cung ky luat voi chuot trai va viec keo man hinh:
        /// luc nhan xuong thi chua biet nguoi choi dinh lam gi. Lam khac di se mat duong thoat ma
        /// nguoi choi da quen — do van la loi ra duy nhat cua che do nay.
        /// </summary>
        void HandleDecorationRightMouse()
        {
            if (Input.GetMouseButtonDown(1))
            {
                _rotatePressScreenX = Input.mousePosition.x;
                _rotateWasDrag = false;
                return;
            }

            if (Input.GetMouseButton(1))
            {
                float moved = Input.mousePosition.x - _rotatePressScreenX;
                if (!_rotateWasDrag && Mathf.Abs(moved) < RotateDragThresholdPixels) return;
                _rotateWasDrag = true;
                _rotatePressScreenX = Input.mousePosition.x;
                if (Preview != null) Preview.RotateBy(moved * RotateDegreesPerPixel);
                return;
            }

            if (Input.GetMouseButtonUp(1) && !_rotateWasDrag)
            {
                CancelDecorationMode();
                if (Hud != null) Hud.Refresh();
            }
        }

        /// <summary>
        /// Bong ma bam theo con tro. Vi tri va tinh hop le deu tinh lai moi khung hinh chu khong
        /// nho lai tu lan click truoc: con tro di lien tuc, con click thi khong.
        ///
        /// Cho dat khong duoc thi bong ma **do len chu khong bien mat**. Bien mat la mot cau tra
        /// loi mo ho — nguoi choi khong biet la minh dua chuot ra ngoai vuon hay la cho do vuong.
        /// </summary>
        void UpdatePlacementPreview()
        {
            if (Preview == null) return;
            if (_placingDecorationId == null) { Preview.Cancel(); return; }

            var camera = GameCamera != null ? GameCamera : Camera.main;
            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            Vector3 ground;
            if (ClicksAreBlocked || overUi || camera == null ||
                !TryGroundPoint(camera, Input.mousePosition, out ground))
            {
                Preview.HideGhost();
                return;
            }

            int xMm = SnapToGrid(ground.x);
            int zMm = SnapToGrid(ground.z);

            // Cham dung dat trong moi dat duoc — dung cung dieu kien ma cu click that se dung,
            // nen mau cua bong ma khong bao gio hua mot chuyen ma cu click lai tu choi.
            RaycastHit hit;
            bool onBareGround =
                Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out hit, 500f) &&
                hit.collider.GetComponentInParent<GardenGround>() != null;

            string reason;
            bool canPlace = onBareGround &&
                            _session.CanPlaceDecoration(_placingDecorationId, xMm, zMm, -1, out reason);
            Preview.ShowAt(new Vector3(xMm / 1000f, 0f, zMm / 1000f), canPlace);
        }

        /// <summary>
        /// Diem tren mat dat (y = 0) ma tia tu con tro cham toi, tinh bang hinh hoc chu khong
        /// bang collider: bong ma phai dung yen tren mat dat ke ca khi tia dang cham vao mot cai
        /// cay hay mot luong rau cao hon mat dat.
        /// </summary>
        static bool TryGroundPoint(Camera camera, Vector3 screenPosition, out Vector3 point)
        {
            point = Vector3.zero;
            var ray = camera.ScreenPointToRay(screenPosition);
            if (Mathf.Approximately(ray.direction.y, 0f)) return false;
            float distance = -ray.origin.y / ray.direction.y;
            if (distance <= 0f) return false;
            point = ray.GetPoint(distance);
            return true;
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
            int rotationDeg = Preview != null ? Mathf.RoundToInt(Preview.RotationDeg) : 0;
            var result = _session.PlaceDecoration(_placingDecorationId, xMm, zMm, rotationDeg);

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
        public void OnStationStarted(string stageId, string cropId, long atMs) { }
        public void OnStationCompleted(string stageId, string cropId, int amount, long atMs) { }
        public void OnWagesPaid(long coins, int paid, int unpaid, long atMs) { }

        PlotView FindPlotView(int plotId)
        {
            if (Plots == null) return null;
            for (int i = 0; i < Plots.Length; i++)
                if (Plots[i] != null && Plots[i].PlotId == plotId) return Plots[i];
            return null;
        }
    }
}
