using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VuonNho.Core;

namespace VuonNho.Views
{
    /// <summary>Machine inspection follows the same world colliders used for workshop clicks.</summary>
    public sealed class StationHoverController : MonoBehaviour
    {
        public GameBootstrap Bootstrap;

        public StationView HoveredStation { get; private set; }
        public RectTransform TooltipRect { get { return _card; } }
        public bool TooltipVisible { get { return _card != null && _card.gameObject.activeSelf; } }

        const float CardWidth = 336f;
        const float RefreshInterval = 0.1f;
        readonly MachineHoverHighlight _highlight = new MachineHoverHighlight();
        readonly List<RaycastResult> _uiHits = new List<RaycastResult>(16);

        RectTransform _canvasRect;
        Canvas _canvas;
        GraphicRaycaster _raycaster;
        PointerEventData _pointer;
        EventSystem _eventSystem;
        RectTransform _card;
        Transform _offlineModal;
        Transform _blockedModal;
        WorkshopCrewView _crew;
        Text _title;
        Text _stageName;
        Text _flow;
        Text _stock;
        Text _status;
        Text _worker;
        Image _progress;
        float _nextRefresh;

        void LateUpdate()
        {
            UpdateHoverAt(Input.mousePosition);
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused) ClearHover();
        }

        void OnDisable() { ClearHover(); }

        void OnDestroy()
        {
            ClearHover();
            if (_card != null) Destroy(_card.gameObject);
        }

        /// <summary>Public so player QA can exercise the real UI blocking, raycast and tooltip path.</summary>
        public void UpdateHoverAt(Vector2 screenPosition)
        {
            if (!EnsureBuilt() || IsBlocked(screenPosition))
            {
                ClearHover();
                return;
            }

            var camera = Bootstrap.GameCamera != null ? Bootstrap.GameCamera : Camera.main;
            RaycastHit hit;
            StationView station = null;
            if (camera != null && Physics.Raycast(camera.ScreenPointToRay(screenPosition), out hit,
                                                  500f, Physics.DefaultRaycastLayers,
                                                  QueryTriggerInteraction.Ignore))
                station = hit.collider.GetComponentInParent<StationView>();

            var state = Bootstrap.Session.State;
            var owned = station == null ? null : state.Station(station.StageId);
            if (owned == null || !owned.Owned || station.VisualRoot == null ||
                !station.VisualRoot.gameObject.activeInHierarchy)
            {
                ClearHover();
                return;
            }

            if (HoveredStation != station)
            {
                _highlight.Restore();
                HoveredStation = station;
                _highlight.Apply(station.VisualRoot, station.StatusRenderer);
                _card.gameObject.SetActive(true);
                _card.SetAsLastSibling();
                _nextRefresh = 0f;
            }
            if (Time.unscaledTime >= _nextRefresh)
            {
                RefreshContents();
                _nextRefresh = Time.unscaledTime + RefreshInterval;
                LayoutRebuilder.ForceRebuildLayoutImmediate(_card);
            }
            PositionCard(screenPosition);
        }

        public void ClearHover()
        {
            _highlight.Restore();
            HoveredStation = null;
            if (_card != null) _card.gameObject.SetActive(false);
        }

        bool IsBlocked(Vector2 position)
        {
            if (!Application.isFocused || Bootstrap.Session.State == null ||
                position.x < 0f || position.y < 0f || position.x >= Screen.width || position.y >= Screen.height ||
                Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2) ||
                Bootstrap.PlacingDecorationId != null || Bootstrap.RemovingDecorations ||
                (_offlineModal != null && _offlineModal.gameObject.activeInHierarchy) ||
                (_blockedModal != null && _blockedModal.gameObject.activeInHierarchy)) return true;

            // The bootstrap temporarily disables this raycaster while the window regains focus.
            if (_raycaster != null && !_raycaster.isActiveAndEnabled) return true;
            if (_raycaster == null || EventSystem.current == null) return false;
            if (_eventSystem != EventSystem.current)
            {
                _eventSystem = EventSystem.current;
                _pointer = new PointerEventData(_eventSystem);
            }
            _pointer.Reset();
            _pointer.position = position;
            _uiHits.Clear();
            _raycaster.Raycast(_pointer, _uiHits);
            return _uiHits.Count > 0;
        }

        bool EnsureBuilt()
        {
            if (_card != null) return Bootstrap != null && Bootstrap.Session != null;
            if (Bootstrap == null || Bootstrap.Hud == null || Bootstrap.Session == null) return false;
            _canvasRect = Bootstrap.Hud.transform as RectTransform;
            _canvas = Bootstrap.Hud.GetComponentInParent<Canvas>();
            if (_canvasRect == null || _canvas == null) return false;
            _raycaster = Bootstrap.Hud.GetComponent<GraphicRaycaster>();
            _offlineModal = Bootstrap.Hud.transform.Find("OfflineModal");
            _blockedModal = Bootstrap.Hud.transform.Find("BlockedModal");

            var background = UiFactory.Panel(Bootstrap.Hud.transform, "StationHoverCard",
                                             GardenPalette.PanelBackground, UiFactory.RadiusPanel);
            background.raycastTarget = false;
            _card = background.rectTransform;
            _card.anchorMin = _card.anchorMax = new Vector2(0.5f, 0.5f);
            _card.pivot = new Vector2(0f, 1f);
            _card.sizeDelta = new Vector2(CardWidth, 0f);
            var group = background.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            var shadow = background.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.13f, 0.20f, 0.12f, 0.24f);
            shadow.effectDistance = new Vector2(0f, -4f);
            UiFactory.VerticalList(background.gameObject, 7f, new RectOffset(16, 16, 13, 14));
            background.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            var header = UiFactory.Node(_card, "Header");
            var headerLayout = UiFactory.HorizontalList(header, 8f, new RectOffset(0, 0, 0, 0));
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = false;
            var symbol = UiFactory.Symbol(header.transform, "FactorySymbol", UiFactory.Symbols.Factory, 28f);
            symbol.color = GardenPalette.StateOk;
            _title = UiFactory.Label(header.transform, "MachineName", "", 22,
                                     TextAnchor.UpperLeft, GardenPalette.TextPrimary, true);
            _title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            _stageName = UiFactory.Label(_card, "StageName", "", 16,
                                         TextAnchor.UpperLeft, GardenPalette.TextMuted);
            var divider = UiFactory.Panel(_card, "Divider", GardenPalette.PanelDivider);
            divider.raycastTarget = false;
            divider.gameObject.AddComponent<LayoutElement>().preferredHeight = 1f;
            _flow = UiFactory.Label(_card, "BatchFlow", "", 18,
                                    TextAnchor.UpperLeft, GardenPalette.TextPrimary);
            _stock = UiFactory.Label(_card, "Stock", "", 16,
                                     TextAnchor.UpperLeft, GardenPalette.TextMuted);
            _status = UiFactory.Label(_card, "Status", "", 18,
                                      TextAnchor.UpperLeft, GardenPalette.StateOk);
            var track = UiFactory.ProgressBar(_card, "BatchProgress", GardenPalette.StateOk, out _progress);
            track.raycastTarget = false;
            _progress.raycastTarget = false;
            track.gameObject.AddComponent<LayoutElement>().preferredHeight = 6f;

            var workerRow = UiFactory.Node(_card, "WorkerRow");
            var workerLayout = UiFactory.HorizontalList(workerRow, 5f, new RectOffset(0, 0, 0, 0));
            workerLayout.childForceExpandWidth = false;
            workerLayout.childForceExpandHeight = false;
            UiFactory.Symbol(workerRow.transform, "WorkerSymbol", UiFactory.Symbols.GroupAdd, 23f)
                .color = GardenPalette.TextMuted;
            _worker = UiFactory.Label(workerRow.transform, "WorkerStatus", "", 16,
                                      TextAnchor.MiddleLeft, GardenPalette.TextMuted);
            _worker.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            background.gameObject.SetActive(false);
            return true;
        }

        void RefreshContents()
        {
            var session = Bootstrap.Session;
            var state = session.State;
            var station = state.Station(HoveredStation.StageId);
            var stage = session.Catalog.Stage(HoveredStation.StageId);
            _title.text = stage.MachineName;
            _stageName.text = "Công đoạn: " + stage.DisplayName;
            _flow.text = stage.InputCount + " " + ItemStageName(stage.InputSuffix) + "  →  " +
                         (station.Running ? station.BatchOutput : stage.OutputCount) + " " +
                         ItemStageName(stage.OutputSuffix);

            string cropId = station.Running ? station.BatchCropId : session.Simulation.ChooseCropFor(state, stage);
            if (cropId != null)
            {
                _stock.text = session.Catalog.Crop(cropId).DisplayName + " · kho " +
                              state.InventoryOf(ProcessChain.ItemId(cropId, stage.InputSuffix)) + " → " +
                              state.InventoryOf(ProcessChain.ItemId(cropId, stage.OutputSuffix));
            }
            else
            {
                long input = 0, output = 0;
                for (int i = 0; i < session.Catalog.Crops.Count; i++)
                {
                    string id = session.Catalog.Crops[i].Id;
                    input += state.InventoryOf(ProcessChain.ItemId(id, stage.InputSuffix));
                    output += state.InventoryOf(ProcessChain.ItemId(id, stage.OutputSuffix));
                }
                _stock.text = "Trong kho: " + input + " đầu vào · " + output + " đầu ra";
            }

            bool workerAvailable = state.StaffedWorkers * Workforce.StationsPerWorker > state.RunningStationCount();
            float progress = 0f;
            if (station.Running)
            {
                long remaining = Math.Max(0L, station.BatchFinishAtMs - state.SimulationTimeMs);
                long duration = Math.Max(1L, station.BatchFinishAtMs - station.BatchStartAtMs);
                progress = Mathf.Clamp01(1f - (float)remaining / duration);
                _status.text = "Đang chạy · " + Mathf.FloorToInt(progress * 100f) + "% · còn " +
                               ((remaining + 999L) / 1000L) + " giây";
                _status.color = GardenPalette.StateOk;
            }
            else
            {
                _status.text = !workerAvailable ? "Chờ thợ vận hành" : cropId == null
                    ? "Chờ đủ nguyên liệu" : "Sẵn sàng cho mẻ tiếp theo";
                _status.color = !workerAvailable ? GardenPalette.StateWarn : GardenPalette.TextMuted;
            }
            UiFactory.SetProgress(_progress, progress);

            if (_crew == null) _crew = Bootstrap.GetComponent<WorkshopCrewView>();
            bool present = _crew != null && _crew.HasWorkerAt(HoveredStation.StageId);
            if (station.Running) _worker.text = present ? "Thợ đang vận hành máy" : "Thợ đang tới máy";
            else if (state.HiredWorkers == 0) _worker.text = "Chưa thuê thợ";
            else if (state.StaffedWorkers == 0) _worker.text = "Thợ đang nghỉ · thiếu lương";
            else if (present) _worker.text = "Thợ đang chờ bên máy";
            else _worker.text = "Thợ đang ở công đoạn khác";
        }

        static string ItemStageName(string suffix)
        {
            switch (suffix)
            {
                case ProcessChain.SuffixWithered: return "lá héo";
                case ProcessChain.SuffixFixed: return "lá đã diệt men";
                case ProcessChain.SuffixRolled: return "lá đã vò";
                case ProcessChain.SuffixOxidised: return "lá đã lên men";
                case ProcessChain.SuffixDried: return "trà khô";
                case ProcessChain.SuffixPacked: return "trà đóng gói";
                default: return "lá tươi";
            }
        }

        void PositionCard(Vector2 screenPosition)
        {
            Vector2 local;
            var eventCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPosition,
                                                                         eventCamera, out local)) return;
            _card.anchoredPosition = ClampTooltipPosition(_canvasRect.rect, local, _card.rect.size);
        }

        /// <summary>Returns a top-left pivot position in canvas units, with room around the pointer.</summary>
        public static Vector2 ClampTooltipPosition(Rect canvas, Vector2 pointer, Vector2 size)
        {
            const float margin = 12f;
            const float offset = 20f;
            float x = pointer.x + offset;
            float y = pointer.y - offset;
            if (x + size.x > canvas.xMax - margin) x = pointer.x - offset - size.x;
            if (y - size.y < canvas.yMin + margin) y = pointer.y + offset + size.y;
            x = Mathf.Clamp(x, canvas.xMin + margin, Mathf.Max(canvas.xMin + margin, canvas.xMax - margin - size.x));
            y = Mathf.Clamp(y, Mathf.Min(canvas.yMax - margin, canvas.yMin + margin + size.y), canvas.yMax - margin);
            return new Vector2(x, y);
        }
    }
}
