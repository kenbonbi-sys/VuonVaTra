using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VuonNho.Core;
using VuonNho.Infrastructure;
using Debug = UnityEngine.Debug;

namespace VuonNho.Views
{
    /// <summary>
    /// Chay muc "Kiem thu build va giao dien" cua muc 10 ke hoach ngay ben trong ban build,
    /// roi ghi bao cao ra file. Khong co tham so thi component nay khong lam gi.
    ///
    /// VuonNho.exe -vuonnho-qa "D:\bao-cao.md"
    /// </summary>
    public sealed class QaChecklist : MonoBehaviour
    {
        public const string Argument = "-vuonnho-qa";

        string _reportPath;
        readonly List<string> _lines = new List<string>();
        int _passed;
        int _failed;
        float _startupSeconds;

        GameBootstrap _bootstrap;
        GameHud _hud;
        GameSession _session;

        void Awake()
        {
            var arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
                if (arguments[i] == Argument && i + 1 < arguments.Length)
                    _reportPath = arguments[i + 1];

            if (string.IsNullOrEmpty(_reportPath)) enabled = false;
        }

        void Start()
        {
            if (!enabled) return;
            _startupSeconds = Time.realtimeSinceStartup;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            yield return null;

            _bootstrap = FindAnyObjectByType<GameBootstrap>();
            _hud = FindAnyObjectByType<GameHud>();
            if (_bootstrap == null || _bootstrap.Session == null)
            {
                Check("Bootstrap khởi tạo được phiên chơi", false, "Không tìm thấy GameBootstrap/GameSession.");
                Finish();
                yield break;
            }
            _session = _bootstrap.Session;

            Section("Khởi động");
            // Tach rieng phan game chiu trach nhiem khoi phan engine khoi dong, vi splash Unity
            // khong tat duoc o ban Personal va khong phai thu game co the toi uu.
            Check("Nạp save và dựng phiên chơi dưới 2 giây",
                  GameBootstrap.SessionLoadSeconds < 2f,
                  GameBootstrap.SessionLoadSeconds.ToString("0.000") + " s");
            Check("Engine khởi động + splash Unity (chỉ ghi nhận, không phải mục tiêu của game)",
                  true, GameBootstrap.EngineBootSeconds.ToString("0.00") + " s");
            Check("Tổng thời gian tới khung hình chơi được", true,
                  _startupSeconds.ToString("0.00") + " s (bao gồm splash)");
            Check("Độ phân giải chạy thật", true, Screen.width + " × " + Screen.height);

            // Bat dau tu vuon moi bat ke may co san save gi.
            _bootstrap.ResetProgress();
            yield return null;
            _session = _bootstrap.Session;

            Section("Vườn mới");
            Check("Bắt đầu với 0 xu", _session.State.Coins == 0, _session.State.Coins + " xu");
            Check("Mở sẵn đúng 4 ô", _session.State.UnlockedPlotCount() == 4,
                  _session.State.UnlockedPlotCount() + " ô");
            Check("Chưa có robot", !_session.State.RobotUnlocked, null);

            yield return RunPlaythrough();
            yield return RunReloadCheck();
            yield return RunUiChecks();
            yield return RunOfflineAndPerf();
            RunSettingsChecks();

            Finish();
        }

        // ---------------------------------------------------------------- chơi tới 12 ô

        IEnumerator RunPlaythrough()
        {
            Section("Chơi từ vườn mới tới mở 12 ô");

            PlantEveryEmptyPlot();
            long simAtStart = _session.State.SimulationTimeMs;

            // Truoc robot phai thu bang tay, dung dung mot duong voi automation.
            int manualHarvests = 0;
            long firstCoinsAtMs = -1;
            long robotCost = _session.Catalog.Upgrade(DefaultContent.UpgradeRobot).Cost;
            int guard = 0;

            while (_session.State.Coins < robotCost && guard++ < 2000)
            {
                _session.DebugAdvance(5000);
                if (firstCoinsAtMs < 0 && _session.State.Coins > 0)
                    firstCoinsAtMs = _session.State.SimulationTimeMs - simAtStart;

                for (int i = 0; i < _session.State.Plots.Count; i++)
                {
                    if (_session.State.Plot(i).Phase != PlotPhase.Ready) continue;
                    if (_session.HarvestAndReplant(i).Success) manualHarvests++;
                }
                if (guard % 40 == 0) yield return null;
            }

            Check("Có xu đầu tiên từ trà", firstCoinsAtMs >= 0,
                  firstCoinsAtMs >= 0 ? "ở phút mô phỏng " + Minutes(firstCoinsAtMs) : "không có xu nào");
            Check("Trà đầu trong vòng 2 phút mô phỏng", firstCoinsAtMs >= 0 && firstCoinsAtMs <= 120000,
                  Minutes(firstCoinsAtMs) + " phút");

            long robotAtMs = _session.State.SimulationTimeMs - simAtStart;
            var robotResult = _session.Purchase(DefaultContent.UpgradeRobot);
            Check("Mua được robot", robotResult.Success, robotResult.FailureReason);
            Check("Đủ tiền mua robot trong 8 phút mô phỏng", robotAtMs <= 480000,
                  Minutes(robotAtMs) + " phút, " + manualHarvests + " lần thu tay");

            // Sau robot: mua theo dung lo trinh o muc 5 ke hoach.
            string[] route =
            {
                DefaultContent.UpgradeChamomile,
                DefaultContent.UpgradeExpand8,
                DefaultContent.UpgradeBrewSpeed,
                DefaultContent.UpgradeStrawberry,
                DefaultContent.UpgradeGrowthSpeed,
                DefaultContent.UpgradeExpand12,
                DefaultContent.UpgradeBrewSpeed2,
                DefaultContent.UpgradeGrowthSpeed2,
                DefaultContent.UpgradeLemongrass,
                DefaultContent.UpgradeJasmine
            };

            foreach (string upgradeId in route)
            {
                var upgrade = _session.Catalog.Upgrade(upgradeId);
                int wait = 0;
                while (_session.State.Coins < upgrade.Cost && wait++ < 4000)
                {
                    _session.DebugAdvance(10000);
                    if (wait % 40 == 0) yield return null;
                }

                var result = _session.Purchase(upgradeId);
                Check("Mua " + upgrade.DisplayName, result.Success, result.FailureReason);

                // Mo cay moi thi chuyen ca vuon sang cay do, dung nhu lo trinh mo phong.
                if (upgrade.Kind == UpgradeKind.UnlockCrop)
                {
                    for (int i = 0; i < _session.State.Plots.Count; i++)
                        if (_session.State.Plot(i).Unlocked)
                            _session.SetNextCrop(i, upgrade.TargetId);
                    var recipe = _session.Catalog.RecipeForCrop(upgrade.TargetId);
                    if (recipe != null) _session.SelectRecipe(recipe.Id);
                }
                if (upgrade.Kind == UpgradeKind.ExpandPlots) PlantEveryEmptyPlot();
                yield return null;
            }

            long totalMs = _session.State.SimulationTimeMs - simAtStart;
            Check("Mở đủ 12 ô", _session.State.UnlockedPlotCount() == 12,
                  _session.State.UnlockedPlotCount() + " ô sau " + Minutes(totalMs) + " phút mô phỏng");

            bool allBought = true;
            foreach (var upgrade in _session.Catalog.Upgrades)
                if (_session.State.UpgradeLevel(upgrade.Id) < 1) allBought = false;
            Check("Mua hết toàn bộ nâng cấp MVP", allBought, null);
            Check("Mở hết mọi loại cây trong catalog",
                  _session.State.UnlockedCropIds.Count == _session.Catalog.Crops.Count,
                  _session.State.UnlockedCropIds.Count + "/" + _session.Catalog.Crops.Count + " loại");
            Check("Không có xu âm", _session.State.Coins >= 0, _session.State.Coins + " xu");
        }

        void PlantEveryEmptyPlot()
        {
            string cropId = DefaultContent.CropMint;
            foreach (var crop in _session.Catalog.Crops)
                if (_session.State.UnlockedCropIds.Contains(crop.Id)) cropId = crop.Id;

            for (int i = 0; i < _session.State.Plots.Count; i++)
            {
                var plot = _session.State.Plot(i);
                if (plot.Unlocked && plot.Phase == PlotPhase.Empty) _session.Plant(i, cropId);
            }
        }

        // ---------------------------------------------------------------- reload giữa chu kỳ

        IEnumerator RunReloadCheck()
        {
            Section("Reload khi cây và máy đang chạy");

            PlantEveryEmptyPlot();

            // Vuon vua duoc gieo lai nen kho rong; tien them cho toi khi may thuc su co me chay,
            // roi moi luu. Neu khong cho, bai kiem se phu thuoc vao nhip balance dang dung.
            int settle = 0;
            while (!_session.State.Machine.BatchRunning && settle++ < 60)
            {
                _session.DebugAdvance(1000);
                if (settle % 10 == 0) yield return null;
            }
            yield return null;

            bool anyGrowing = false;
            for (int i = 0; i < _session.State.Plots.Count; i++)
                if (_session.State.Plot(i).Phase == PlotPhase.Growing) anyGrowing = true;
            Check("Có cây đang lớn lúc lưu", anyGrowing, null);
            Check("Máy đang pha lúc lưu", _session.State.Machine.BatchRunning, null);

            // Suspend dat checkpoint ve hien tai roi luu, giong luc nguoi choi thoat game.
            _session.Suspend();
            long coins = _session.State.Coins;
            long simulationTime = _session.State.SimulationTimeMs;
            var expectedPhases = new List<PlotPhase>();
            for (int i = 0; i < _session.State.Plots.Count; i++)
                expectedPhases.Add(_session.State.Plot(i).Phase);
            var expectedStep = TutorialGuide.CurrentStep(_session.State, _session.Catalog);

            var repository = new FileSaveRepository(Application.persistentDataPath);
            var reloaded = new GameSession(DefaultContent.Create(), new SystemClock(), repository, null, "qa");
            var outcome = reloaded.Initialize();

            Check("Nạp lại được file save vừa ghi", outcome == LoadOutcome.LoadedPrimary, outcome.ToString());
            Check("Giữ nguyên số xu sau khi nạp lại", reloaded.State.Coins == coins,
                  coins + " → " + reloaded.State.Coins);
            Check("Thời gian mô phỏng không lùi", reloaded.State.SimulationTimeMs >= simulationTime,
                  simulationTime + " → " + reloaded.State.SimulationTimeMs);

            bool phasesMatch = true;
            for (int i = 0; i < expectedPhases.Count; i++)
                if (reloaded.State.Plot(i).Phase != expectedPhases[i] &&
                    expectedPhases[i] != PlotPhase.Growing) phasesMatch = false;
            Check("Trạng thái ô đất dựng lại đúng", phasesMatch, null);
            Check("Hướng dẫn tiếp tục đúng bước",
                  TutorialGuide.CurrentStep(reloaded.State, reloaded.Catalog) == expectedStep,
                  expectedStep.ToString());

            _session.ResumeFromBackground();
            yield return null;

            // View phai dung dung theo state, khong tu quyet dinh gi.
            int mismatch = 0;
            if (_bootstrap.Plots != null)
            {
                for (int i = 0; i < _bootstrap.Plots.Length; i++)
                {
                    var view = _bootstrap.Plots[i];
                    if (view == null) continue;
                    var plot = _session.State.Plot(view.PlotId);
                    bool badgeShouldShow = plot.Phase == PlotPhase.Ready;
                    if (view.ReadyBadge != null && view.ReadyBadge.activeSelf != badgeShouldShow) mismatch++;
                    if (!plot.Unlocked && view.SeedlingVisual != null && view.SeedlingVisual.activeSelf) mismatch++;
                }
            }
            Check("Hình ảnh ô đất khớp trạng thái sau khi dựng lại", mismatch == 0,
                  mismatch + " ô lệch");
        }

        // ---------------------------------------------------------------- giao diện

        IEnumerator RunUiChecks()
        {
            Section("Giao diện ở " + Screen.width + " × " + Screen.height);

            // Popup cho offline khong bao gio tu dong lai; neu no con mo thi scrim cua no la vat
            // trung raycast tren cung va moi phep do UI phia sau se do nham scrim.
            DismissOfflinePopupIfOpen();
            yield return null;

            CheckGardenClickReachable();
            yield return CheckModalHasEscape("OfflineModal");
            yield return CheckModalHasEscape("BlockedModal");

            string[] panels = { "inventory", "upgrade", "decorate", "settings", "plot" };
            foreach (string panelName in panels)
            {
                _hud.OpenPanelByName(panelName);
                yield return null;
                yield return null;

                var opened = FindOpenSurface(panelName);
                Check("Mở được bề mặt " + panelName, opened != null, null);
                if (opened == null) continue;

                CheckTextOverflow(panelName, opened);
                CheckBlocksClicks(panelName, opened);
                // Phai do truoc khi bam nut dong that: sau do be mat khong con tren man hinh nua.
                CheckDoesNotCoverTopBar(panelName, opened);
                CheckButtonReceivesClick(panelName, opened);

                CloseSurface(panelName);
                yield return null;
            }

            CheckTextOverflow("HUD", _hud.transform as RectTransform);
        }

        void DismissOfflinePopupIfOpen()
        {
            var modal = _hud.transform.Find("OfflineModal");
            if (modal == null || !modal.gameObject.activeInHierarchy) return;

            Canvas.ForceUpdateCanvases();
            var buttonTransform = modal.Find("Card/Continue");
            var button = buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
            string detail;
            bool clicked = TryClickButton(button, out detail);
            bool acknowledged = _session.State.PendingOfflineSummary == null ||
                                _session.State.PendingOfflineSummary.Seen;
            Check("Đóng báo cáo offline bằng nút Tiếp tục và xác nhận báo cáo",
                  clicked && !modal.gameObject.activeInHierarchy && acknowledged, detail);
        }

        /// <summary>
        /// Bat tam modal de kiem loi thoat co the nhan pointer qua scrim. Khong bam cac hanh
        /// dong thoat game/reset; sau phep do tra lai dung trang thai hien thi truoc do.
        /// </summary>
        IEnumerator CheckModalHasEscape(string modalName)
        {
            var modal = _hud.transform.Find(modalName);
            if (modal == null)
            {
                Check("Modal " + modalName + " có lối thoát bấm được", false, "Không tìm thấy modal.");
                yield break;
            }

            bool wasActive = modal.gameObject.activeSelf;
            var reachable = new List<string>();
            var failures = new List<string>();

            modal.gameObject.SetActive(true);
            var card = modal.Find("Card") as RectTransform;
            if (card != null) LayoutRebuilder.ForceRebuildLayoutImmediate(card);
            Canvas.ForceUpdateCanvases();

            // Canvas chi dung xong hinh hoc o cuoi khung hinh. Do ngay trong khung hinh vua bat
            // modal thi moi phep do deu tra ve "khong co gi" va bao nham la modal hong.
            yield return null;

            foreach (var button in modal.GetComponentsInChildren<Button>(false))
            {
                if (!IsModalEscape(button)) continue;
                string detail;
                if (ButtonIsReachable(button, out detail)) reachable.Add(Path(button.transform));
                else failures.Add(detail);
            }

            Check("Modal " + modalName + " có lối thoát bấm được", reachable.Count > 0,
                  reachable.Count > 0 ? string.Join(", ", reachable.ToArray())
                  : failures.Count > 0 ? string.Join("; ", failures.ToArray())
                  : "Không có nút Tiếp tục/Đóng/Cài đặt/Thoát trong modal.");

            modal.gameObject.SetActive(wasActive);
            Canvas.ForceUpdateCanvases();
            yield return null;
        }

        static bool IsModalEscape(Button button)
        {
            string name = button.name.ToLowerInvariant();
            if (name == "continue" || name.Contains("close") || name.Contains("settings") ||
                name == "back" || name == "quit" || name == "exit") return true;
            var caption = button.GetComponentInChildren<Text>(true);
            string text = caption != null ? caption.text.Trim() : "";
            return text == "Tiếp tục" || text == "Đóng" || text == "Cài đặt" ||
                   text == "Thoát" || text == "Thoát game";
        }

        void CheckButtonReceivesClick(string surface, RectTransform panel)
        {
            string buttonName = surface == "plot" ? "ClosePopup" : "Close";
            Button close = null;
            foreach (var candidate in panel.GetComponentsInChildren<Button>(false))
                if (candidate.name == buttonName) { close = candidate; break; }

            string detail;
            bool clicked = TryClickButton(close, out detail);
            Check("Nút đóng nhận click thật và đóng bề mặt " + surface,
                  clicked && !panel.gameObject.activeInHierarchy, detail);
        }

        bool ButtonIsReachable(Button button, out string detail)
        {
            detail = "Không tìm thấy nút.";
            if (button == null) return false;
            detail = Path(button.transform) + ": nút không hoạt động hoặc không tương tác được.";
            if (!button.isActiveAndEnabled || !button.IsInteractable()) return false;
            var rect = button.transform as RectTransform;
            if (rect == null || rect.rect.width <= 1f || rect.rect.height <= 1f) return false;
            var point = ScreenCenterOf(rect);
            detail = Path(button.transform) + ": tâm nút nằm ngoài màn hình.";
            if (point.x < 0f || point.x >= Screen.width || point.y < 0f || point.y >= Screen.height) return false;
            var probe = ProbeClick(point, false);
            var handler = probe.UiTarget != null
                ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(probe.UiTarget) : null;
            bool reached = probe.BlockedByUi && probe.GardenTarget == null && handler == button.gameObject;
            detail = Path(button.transform) + "; graphic trên cùng: " +
                     (probe.UiTarget != null ? Path(probe.UiTarget.transform) : "không có") +
                     "; click handler: " + (handler != null ? Path(handler.transform) : "không có");
            return reached;
        }

        bool TryClickButton(Button button, out string detail)
        {
            if (!ButtonIsReachable(button, out detail)) return false;
            var point = ScreenCenterOf(button.transform as RectTransform);
            var probe = ProbeClick(point, true);
            var handler = probe.UiTarget != null
                ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(probe.UiTarget) : null;
            if (handler != button.gameObject) return false;

            var pointer = new PointerEventData(EventSystem.current)
            {
                position = point,
                pressPosition = point,
                button = PointerEventData.InputButton.Left,
                pointerPress = handler,
                rawPointerPress = probe.UiTarget,
                pointerEnter = probe.UiTarget,
                eligibleForClick = true,
                clickCount = 1
            };
            return ExecuteEvents.Execute(handler, pointer, ExecuteEvents.pointerClickHandler);
        }

        /// <summary>
        /// Chi dong khi be mat con mo. Nut dong that o muc kiem tren co the da dong no roi,
        /// ma OpenPanelByName la lenh bat/tat nen goi vo dieu kien se mo lai chinh panel do.
        /// </summary>
        void CloseSurface(string panelName)
        {
            if (FindOpenSurface(panelName) == null) return;
            if (panelName == "plot") _hud.ClosePlotPopup();
            else _hud.OpenPanelByName(panelName);   // goi lai la dong
        }

        RectTransform FindOpenSurface(string panelName)
        {
            string wanted;
            switch (panelName)
            {
                case "inventory": wanted = "InventoryPanel"; break;
                case "upgrade": wanted = "UpgradePanel"; break;
                case "settings": wanted = "SettingsPanel"; break;
                case "decorate": wanted = "DecoratePanel"; break;
                default: wanted = "PlotPopup"; break;
            }
            var found = _hud.transform.Find(wanted);
            if (found == null || !found.gameObject.activeInHierarchy) return null;
            return found as RectTransform;
        }

        /// <summary>Chu tran ra ngoai o chua no la loi bo cuc, khong phai chuyen thier my thuat.</summary>
        void CheckTextOverflow(string surface, RectTransform root)
        {
            var labels = root.GetComponentsInChildren<Text>(false);
            var offenders = new List<string>();

            foreach (var label in labels)
            {
                if (string.IsNullOrEmpty(label.text)) continue;
                var rect = label.rectTransform.rect;
                if (rect.width <= 1f || rect.height <= 1f) continue;

                bool tooTall = label.preferredHeight > rect.height + 1.5f;
                bool tooWide = label.horizontalOverflow == HorizontalWrapMode.Overflow &&
                               label.preferredWidth > rect.width + 1.5f;
                if (tooTall || tooWide)
                    offenders.Add(Path(label.transform) + " (" +
                                  Mathf.RoundToInt(label.preferredWidth) + "×" +
                                  Mathf.RoundToInt(label.preferredHeight) + " trong " +
                                  Mathf.RoundToInt(rect.width) + "×" + Mathf.RoundToInt(rect.height) + ")");
            }

            Check("Chữ không tràn khỏi ô chứa — " + surface, offenders.Count == 0,
                  offenders.Count == 0 ? labels.Length + " nhãn" : string.Join("; ", offenders.ToArray()));
        }

        /// <summary>
        /// Click len panel phai bi UI chan, khong duoc truyen xuong dat. Khong dem graphic mot cach
        /// gian tiep nua: bom mot pointer that qua ExecuteEvents roi khang dinh dung chot ma
        /// GameBootstrap.HandleClick dua vao, va khang dinh nhanh Physics.Raycast xuong vuon
        /// khong he chay.
        /// </summary>
        void CheckBlocksClicks(string surface, RectTransform panel)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                Check("Click trên " + surface + " bị UI chặn, không xuống tới vườn", false,
                      "Không có EventSystem.");
                return;
            }

            var probe = ProbeClick(ScreenCenterOf(panel), true);
            bool insidePanel = probe.UiTarget != null && probe.UiTarget.transform.IsChildOf(panel);
            bool ok = probe.BlockedByUi && insidePanel && probe.GardenTarget == null;

            var detail = new StringBuilder();
            detail.Append("UI nhận click: ").Append(probe.UiTarget != null ? probe.UiTarget.name : "không có");
            if (probe.UiTarget != null && !insidePanel) detail.Append(" (nằm ngoài bề mặt đang kiểm)");
            detail.Append("; đối tượng nhận sự kiện pointer: ")
                  .Append(probe.Handler != null ? probe.Handler.name : "không có");
            detail.Append("; nếu không chặn sẽ trúng ")
                  .Append(probe.BehindUi != null ? probe.BehindUi.name : "phía sau không có gì trong vườn");
            if (probe.GardenTarget != null)
                detail.Append("; click ĐÃ lọt xuống ").Append(probe.GardenTarget.name);

            Check("Click trên " + surface + " bị UI chặn, không xuống tới vườn", ok, detail.ToString());
        }

        /// <summary>Ket qua mot lan bom click that: ai nhan, co bi chan khong, phia sau la gi.</summary>
        sealed class ClickProbe
        {
            public GameObject UiTarget;      // graphic tren cung duoi con tro
            public GameObject Handler;       // doi tuong that su nhan su kien pointer
            public bool BlockedByUi;         // dung bang thu ma IsPointerOverGameObject tra ve
            public GameObject GardenTarget;  // collider vuon ma HandleClick se cham, null neu bi chan
            public GameObject BehindUi;      // collider nam sau UI, chi de ghi vao bao cao
        }

        /// <summary>
        /// Lam lai dung chuoi quyet dinh cua GameBootstrap.HandleClick.
        ///
        /// Khong dat duoc vi tri chuot that tu code quan ly, ma EventSystem.IsPointerOverGameObject()
        /// lai doc pointerEnter cua con tro chuot that. Nen o day dung lai chot chan bang chinh
        /// truong ma no doc: RaycastAll roi gan pointerEnter = hits[0].gameObject, dung cach
        /// StandaloneInputModule.HandlePointerExitAndEnter lam.
        /// </summary>
        ClickProbe ProbeClick(Vector2 screenPoint, bool deliverEvents)
        {
            var probe = new ClickProbe();
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return probe;

            var pointer = new PointerEventData(eventSystem);
            pointer.position = screenPoint;
            pointer.button = PointerEventData.InputButton.Left;

            var hits = new List<RaycastResult>();
            eventSystem.RaycastAll(pointer, hits);

            if (hits.Count > 0)
            {
                pointer.pointerCurrentRaycast = hits[0];
                pointer.pointerPressRaycast = hits[0];
                pointer.pointerEnter = hits[0].gameObject;
                probe.UiTarget = hits[0].gameObject;

                if (deliverEvents)
                {
                    // Chi enter/down/up. Khong bom pointerClick o giua panel: cho do co the roi
                    // trung "Thoát game" hoac "Xóa tiến độ" va giet ca lan QA.
                    ExecuteEvents.ExecuteHierarchy(probe.UiTarget, pointer, ExecuteEvents.pointerEnterHandler);
                    probe.Handler = ExecuteEvents.ExecuteHierarchy(probe.UiTarget, pointer,
                                                                   ExecuteEvents.pointerDownHandler);
                    ExecuteEvents.ExecuteHierarchy(probe.UiTarget, pointer, ExecuteEvents.pointerUpHandler);
                }
            }

            probe.BlockedByUi = pointer.pointerEnter != null;
            probe.BehindUi = RaycastGarden(screenPoint);
            // Dung thu tu short-circuit y het HandleClick: bi chan thi tia vat ly khong he duoc ban.
            if (!probe.BlockedByUi) probe.GardenTarget = probe.BehindUi;
            return probe;
        }

        /// <summary>Chinh nhanh Physics.Raycast ma HandleClick chay khi click khong bi UI chan.</summary>
        GameObject RaycastGarden(Vector2 screenPoint)
        {
            var camera = _bootstrap.GameCamera != null ? _bootstrap.GameCamera : Camera.main;
            if (camera == null) return null;

            RaycastHit hit;
            if (!Physics.Raycast(camera.ScreenPointToRay(screenPoint), out hit, 500f)) return null;
            return hit.collider != null ? hit.collider.gameObject : null;
        }

        /// <summary>Canvas la ScreenSpaceOverlay nen camera phai la null khi doi ra toa do man hinh.</summary>
        static Vector2 ScreenCenterOf(RectTransform rect)
        {
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        }

        /// <summary>
        /// Muc doi chung cho muc chan click o tren: neu khong o dat nao bam toi duoc thi "bi chan"
        /// chang chung minh dieu gi — co the chi vi camera khong nham vao vuon. Goi khi chua mo panel.
        /// </summary>
        void CheckGardenClickReachable()
        {
            var camera = _bootstrap.GameCamera != null ? _bootstrap.GameCamera : Camera.main;
            if (camera == null)
            {
                Check("Click ngoài panel vẫn xuống được vườn", false, "Không tìm thấy camera.");
                return;
            }

            string reached = null;
            int blockedByUi = 0;
            int noCollider = 0;

            if (_bootstrap.Plots != null)
            {
                for (int i = 0; i < _bootstrap.Plots.Length && reached == null; i++)
                {
                    var view = _bootstrap.Plots[i];
                    if (view == null) continue;

                    var screen = camera.WorldToScreenPoint(view.transform.position);
                    var probe = ProbeClick(new Vector2(screen.x, screen.y), false);
                    if (probe.BlockedByUi) { blockedByUi++; continue; }
                    if (probe.GardenTarget == null ||
                        probe.GardenTarget.GetComponentInParent<PlotView>() == null) { noCollider++; continue; }
                    reached = probe.GardenTarget.name;
                }
            }

            Check("Click ngoài panel vẫn xuống được vườn", reached != null,
                  reached != null
                      ? "click trúng " + reached
                      : blockedByUi + " ô bị UI che, " + noCollider + " ô không có collider nhận tia");
        }

        void CheckDoesNotCoverTopBar(string surface, RectTransform panel)
        {
            var topBar = _hud.transform.Find("TopBar") as RectTransform;
            if (topBar == null)
            {
                Check(surface + " không che thanh trên", false, "Không tìm thấy TopBar.");
                return;
            }

            var buttons = topBar.GetComponentsInChildren<Button>(false);
            var covered = new List<string>();
            foreach (var button in buttons)
            {
                if (Overlaps(panel, button.transform as RectTransform))
                    covered.Add(button.name);
            }

            Check(surface + " không che nút trên thanh HUD", covered.Count == 0,
                  covered.Count == 0 ? buttons.Length + " nút vẫn bấm được" : string.Join(", ", covered.ToArray()));
        }

        static bool Overlaps(RectTransform a, RectTransform b)
        {
            if (a == null || b == null) return false;
            return WorldRect(a).Overlaps(WorldRect(b));
        }

        static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return new Rect(corners[0].x, corners[0].y,
                            corners[2].x - corners[0].x, corners[2].y - corners[0].y);
        }

        static string Path(Transform node)
        {
            var builder = new StringBuilder(node.name);
            var parent = node.parent;
            int depth = 0;
            while (parent != null && depth++ < 3)
            {
                builder.Insert(0, parent.name + "/");
                parent = parent.parent;
            }
            return builder.ToString();
        }

        // ---------------------------------------------------------------- offline và hiệu năng

        IEnumerator RunOfflineAndPerf()
        {
            Section("Offline và hiệu năng");

            // Cap 4 gio la phan nang nhat cua mot lan quay lai; do truc tiep tren ban build.
            var catalog = _session.Catalog;
            var simulation = new FarmSimulation(catalog);
            long worstMs = 0;
            for (int round = 0; round < 5; round++)
            {
                var copy = _session.State.Clone();
                var stopwatch = Stopwatch.StartNew();
                simulation.AdvanceTo(copy, copy.SimulationTimeMs + catalog.Balance.OfflineCapMs);
                stopwatch.Stop();
                if (stopwatch.ElapsedMilliseconds > worstMs) worstMs = stopwatch.ElapsedMilliseconds;
                yield return null;
            }
            Check("Mô phỏng bù 4 giờ dưới 250 ms", worstMs < 250,
                  "lần chậm nhất " + worstMs + " ms trên 5 lần, vườn 12 ô");

            // Nhieu vong quay lai lien tiep khong duoc lam dong bang mot frame nao.
            float worstFrame = 0f;
            for (int round = 0; round < 5; round++)
            {
                _session.Suspend();
                _session.ResumeFromBackground();
                yield return null;
                if (Time.unscaledDeltaTime > worstFrame) worstFrame = Time.unscaledDeltaTime;
            }
            Check("5 vòng thoát/quay lại không đóng băng khung hình", worstFrame < 0.5f,
                  "frame dài nhất " + (worstFrame * 1000f).ToString("0") + " ms");

            // Warm-up roi do frame time voi vuon day.
            PlantEveryEmptyPlot();
            for (int i = 0; i < 30; i++) yield return null;

            var samples = new List<float>(400);
            float elapsed = 0f;
            while (elapsed < 4f)
            {
                yield return null;
                samples.Add(Time.unscaledDeltaTime);
                elapsed += Time.unscaledDeltaTime;
            }
            samples.Sort();
            float p95 = samples[Mathf.Clamp(Mathf.FloorToInt(samples.Count * 0.95f), 0, samples.Count - 1)];
            float average = 0f;
            foreach (float sample in samples) average += sample;
            average /= samples.Count;

            string detail = "trung bình " + (1f / average).ToString("0") + " FPS, p95 " +
                            (p95 * 1000f).ToString("0.0") + " ms, " + samples.Count + " khung hình";
            Check("p95 frame time dưới 16,7 ms khi vườn đầy", p95 < 0.0167f, detail);
            Check("Trung bình đạt 60 FPS", 1f / average >= 58f, detail);
        }

        // ---------------------------------------------------------------- âm thanh, reset, thoát

        void RunSettingsChecks()
        {
            Section("Âm lượng, reset và thoát");

            var sfx = FindAnyObjectByType<SfxPlayer>();
            if (sfx == null)
            {
                Check("Bật tắt được âm thanh", false, "Không tìm thấy SfxPlayer.");
            }
            else
            {
                bool original = sfx.Enabled;
                sfx.Enabled = false;
                bool offStored = PlayerPrefs.GetInt(SfxPlayer.VolumePrefKey, 1) == 0 && !sfx.Enabled;
                sfx.Enabled = true;
                bool onStored = PlayerPrefs.GetInt(SfxPlayer.VolumePrefKey, 0) == 1 && sfx.Enabled;
                sfx.Enabled = original;
                Check("Bật tắt âm thanh và ghi nhớ được lựa chọn", offStored && onStored, null);
            }

            string exportMessage = _bootstrap.ExportTestData();
            Check("Xuất được dữ liệu test", exportMessage != null && exportMessage.StartsWith("Đã xuất"),
                  exportMessage);

            _bootstrap.ResetProgress();
            var afterReset = _bootstrap.Session.State;
            Check("Reset trả về vườn mới", afterReset.Coins == 0 && afterReset.UnlockedPlotCount() == 4 &&
                                            !afterReset.RobotUnlocked,
                  afterReset.Coins + " xu, " + afterReset.UnlockedPlotCount() + " ô");

            string savePath = System.IO.Path.Combine(Application.persistentDataPath,
                                                     FileSaveRepository.MainFileName);
            Check("Sau reset vẫn có file save hợp lệ", File.Exists(savePath), savePath);
        }

        // ---------------------------------------------------------------- báo cáo

        void Section(string title)
        {
            _lines.Add("");
            _lines.Add("## " + title);
            _lines.Add("");
        }

        void Check(string name, bool ok, string detail)
        {
            if (ok) _passed++; else _failed++;
            string line = (ok ? "- ĐẠT — " : "- **KHÔNG ĐẠT** — ") + name;
            if (!string.IsNullOrEmpty(detail)) line += " · " + detail;
            _lines.Add(line);
            Debug.Log("[QA] " + (ok ? "PASS " : "FAIL ") + name + (detail != null ? " | " + detail : ""));
        }

        static string Minutes(long milliseconds)
        {
            if (milliseconds < 0) return "—";
            return (milliseconds / 60000f).ToString("0.0");
        }

        void Finish()
        {
            var header = new List<string>
            {
                "# Kết quả checklist mục 10 — kiểm thử build và giao diện",
                "",
                "Chạy tự động bên trong bản build Windows, không phải trong Editor.",
                "",
                "- Thời điểm: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                "- Build: " + Application.version + (Debug.isDebugBuild ? " (development)" : " (release)"),
                "- Độ phân giải: " + Screen.width + " × " + Screen.height,
                "- Kết quả: **" + _passed + " đạt, " + _failed + " không đạt**",
                ""
            };
            header.AddRange(_lines);

            try
            {
                string directory = System.IO.Path.GetDirectoryName(_reportPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(_reportPath, string.Join("\n", header.ToArray()), new UTF8Encoding(false));
                Debug.Log("[QA] Bao cao: " + _reportPath + " — " + _passed + " dat, " + _failed + " khong dat.");
            }
            catch (Exception error)
            {
                Debug.LogError("[QA] Khong ghi duoc bao cao: " + error.Message);
            }

            Application.Quit(_failed == 0 ? 0 : 1);
        }
    }
}
