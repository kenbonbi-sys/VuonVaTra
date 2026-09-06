using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VuonNho.Core;

namespace VuonNho.Views
{
    /// <summary>
    /// HUD, kho, nang cap, cai dat va bao cao quay lai. UI goi GameSession chu khong sua inventory
    /// truc tiep. Click len UI khong truyen xuong dat vi GraphicRaycaster chan truoc.
    ///
    /// Bo cuc theo mot rail duy nhat: le ngoai 16 px, khoang cach giua hai be mat 12 px, moi
    /// be mat la mot the bo goc duc. Chieu cao dong do uGUI tinh — khong dat cung so px cho
    /// dong nao co chu tieng Viet, vi chu xuong dong la de len nut ben duoi.
    /// </summary>
    public sealed class GameHud : MonoBehaviour
    {
        const float TopBarHeight = 64f;
        /// <summary>Mep tren cua moi be mat noi duoi thanh tren.</summary>
        const float ContentTop = TopBarHeight + UiFactory.Gutter;
        const float SidePanelWidth = 360f;

        GameSession _session;

        Text _coinsLabel;
        Text _goalLabel;
        Text _machineLabel;
        Image _machineDot;
        Image _machineBarFill;
        Image _machineBarTrack;

        Button _inventoryButton;
        Button _upgradeButton;
        Button _decorateButton;
        GameObject _decoratePanel;
        Button _removeModeButton;
        readonly List<DecorationRow> _decorationRows = new List<DecorationRow>();

        sealed class DecorationRow
        {
            public string DecorationId;
            public Text Label;
            public Button Choose;
        }
        Button _settingsButton;

        GameObject _inventoryPanel;
        GameObject _upgradePanel;
        GameObject _settingsPanel;
        GameObject _plotPopup;
        GameObject _offlinePopup;
        GameObject _blockedPanel;

        Text _plotPopupTitle;
        Text _plotPopupState;
        Text _offlineText;
        Text _blockedText;
        Text _toastLabel;
        Image _toastPanel;
        float _toastHideTime;

        int _selectedPlotId = -1;

        readonly List<InventoryRow> _inventoryRows = new List<InventoryRow>();
        readonly List<UpgradeRow> _upgradeRows = new List<UpgradeRow>();
        readonly List<CropChoiceRow> _cropChoiceRows = new List<CropChoiceRow>();
        readonly List<RecipeRow> _recipeRows = new List<RecipeRow>();

        sealed class InventoryRow
        {
            public string CropId;
            public Text Name;
            public Text Meta;
            public Button SellOne;
            public Button SellAll;
        }

        sealed class UpgradeRow
        {
            public string UpgradeId;
            public Image Card;
            public Text Title;
            public Text Cost;
            public Text Description;
            public Text Status;
            public Button Buy;
        }

        sealed class CropChoiceRow
        {
            public string CropId;
            public Button Choose;
        }

        sealed class RecipeRow
        {
            public string RecipeId;
            public Button Select;
        }

        public void Bind(GameSession session)
        {
            _session = session;
            BuildUi();
            _session.StateChanged += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (_session != null) _session.StateChanged -= Refresh;
        }

        // ---------------------------------------------------------------- dung UI

        void BuildUi()
        {
            var root = transform;

            BuildTopBar(root);
            BuildMachineCard(root);

            // --- cac panel ben phai
            Transform body;
            _inventoryPanel = BuildSidePanel("InventoryPanel", "Kho nguyên liệu", out body);
            BuildInventoryRows(body);

            _upgradePanel = BuildSidePanel("UpgradePanel", "Nâng cấp và mở khóa", out body);
            BuildUpgradeRows(body);

            _decoratePanel = BuildSidePanel("DecoratePanel", "Trang trí khu vườn", out body);
            BuildDecorationRows(body);

            _settingsPanel = BuildSidePanel("SettingsPanel", "Cài đặt và công cụ test", out body);
            BuildSettingsRows(body);

            _plotPopup = BuildPlotPopup();
            // Hai popup nay phu scrim len ca panel nen phai dung sau chung trong hierarchy.
            _offlinePopup = BuildOfflinePopup();
            _blockedPanel = BuildBlockedPanel();
            // Toast dung cuoi cung de luon noi tren moi thu.
            BuildToast();

            _decoratePanel.SetActive(false);
            _inventoryPanel.SetActive(false);
            _upgradePanel.SetActive(false);
            _settingsPanel.SetActive(false);
            _plotPopup.SetActive(false);
            _offlinePopup.SetActive(false);
            _blockedPanel.SetActive(false);
        }

        /// <summary>Thanh tren: chip xu, muc tieu hien tai, ba nut mo panel.</summary>
        void BuildTopBar(Transform root)
        {
            var topBar = UiFactory.Panel(root, "TopBar", GardenPalette.PanelBackground);
            UiFactory.Stretch(UiFactory.Rect(topBar.gameObject), new Vector2(0f, 1f), new Vector2(1f, 1f),
                              new Vector2(0f, -TopBarHeight), new Vector2(0f, 0f));

            var coinChip = UiFactory.Panel(topBar.transform, "CoinChip", GardenPalette.PanelSoft,
                                           UiFactory.RadiusControl);
            var chipRect = UiFactory.Rect(coinChip.gameObject);
            chipRect.anchorMin = new Vector2(0f, 0.5f);
            chipRect.anchorMax = new Vector2(0f, 0.5f);
            chipRect.pivot = new Vector2(0f, 0.5f);
            chipRect.anchoredPosition = new Vector2(UiFactory.EdgeMargin, 0f);
            chipRect.sizeDelta = new Vector2(176f, 40f);

            _coinsLabel = UiFactory.Label(coinChip.transform, "Coins", "0 xu", UiFactory.FontSizeTitle,
                                          TextAnchor.MiddleLeft, GardenPalette.Coin, true);
            UiFactory.Stretch(UiFactory.Rect(_coinsLabel.gameObject), Vector2.zero, Vector2.one,
                              new Vector2(14f, 0f), new Vector2(-14f, 0f));

            // Muc tieu la loi nhac, khong phai tieu de: de mo hon chip xu ben trai.
            _goalLabel = UiFactory.Label(topBar.transform, "Goal", "", UiFactory.FontSizeBody,
                                         TextAnchor.MiddleLeft, GardenPalette.TextMuted);
            _goalLabel.verticalOverflow = VerticalWrapMode.Truncate;
            UiFactory.Stretch(UiFactory.Rect(_goalLabel.gameObject), new Vector2(0f, 0f), new Vector2(1f, 1f),
                              new Vector2(208f, 0f), new Vector2(-481f, 0f));

            var topButtons = UiFactory.Node(topBar.transform, "Buttons");
            UiFactory.Stretch(UiFactory.Rect(topButtons), new Vector2(1f, 0f), new Vector2(1f, 1f),
                              new Vector2(-465f, 10f), new Vector2(-UiFactory.EdgeMargin, -10f));
            UiFactory.HorizontalList(topButtons, 8f, new RectOffset(0, 0, 0, 0));

            _inventoryButton = UiFactory.TextButton(topButtons.transform, "InventoryButton", "Kho",
                                                    delegate { TogglePanel(_inventoryPanel); },
                                                    UiFactory.ButtonStyle.Quiet);
            _upgradeButton = UiFactory.TextButton(topButtons.transform, "UpgradeButton", "Nâng cấp",
                                                  delegate { TogglePanel(_upgradePanel); },
                                                  UiFactory.ButtonStyle.Quiet);
            _decorateButton = UiFactory.TextButton(topButtons.transform, "DecorateButton", "Trang trí",
                                                   delegate { TogglePanel(_decoratePanel); },
                                                   UiFactory.ButtonStyle.Quiet);
            _settingsButton = UiFactory.TextButton(topButtons.transform, "SettingsButton", "Cài đặt",
                                                   delegate { TogglePanel(_settingsPanel); },
                                                   UiFactory.ButtonStyle.Quiet);

            var rule = UiFactory.Panel(topBar.transform, "Rule", GardenPalette.PanelDivider);
            UiFactory.Stretch(UiFactory.Rect(rule.gameObject), new Vector2(0f, 0f), new Vector2(1f, 0f),
                              Vector2.zero, new Vector2(0f, 1f));
        }

        /// <summary>The trang thai may: den mau, mot dong chu, mot thanh tien do.</summary>
        void BuildMachineCard(Transform root)
        {
            var card = UiFactory.Panel(root, "MachineCard", GardenPalette.PanelBackground, UiFactory.RadiusPanel);
            var cardRect = UiFactory.Rect(card.gameObject);
            cardRect.anchorMin = new Vector2(0f, 1f);
            cardRect.anchorMax = new Vector2(0f, 1f);
            cardRect.pivot = new Vector2(0f, 1f);
            cardRect.anchoredPosition = new Vector2(UiFactory.EdgeMargin, -ContentTop);
            cardRect.sizeDelta = new Vector2(560f, 60f);

            var dot = UiFactory.Panel(card.transform, "MachineDot", MachineView.StatusIdle, UiFactory.RadiusDot);
            var dotRect = UiFactory.Rect(dot.gameObject);
            dotRect.anchorMin = new Vector2(0f, 1f);
            dotRect.anchorMax = new Vector2(0f, 1f);
            dotRect.pivot = new Vector2(0f, 1f);
            dotRect.anchoredPosition = new Vector2(UiFactory.EdgeMargin, -14f);
            dotRect.sizeDelta = new Vector2(12f, 12f);
            _machineDot = dot;

            _machineLabel = UiFactory.Label(card.transform, "MachineLabel", "", UiFactory.FontSizeBody,
                                            TextAnchor.MiddleLeft, GardenPalette.TextPrimary);
            _machineLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiFactory.Stretch(UiFactory.Rect(_machineLabel.gameObject), new Vector2(0f, 1f), new Vector2(1f, 1f),
                              new Vector2(36f, -32f), new Vector2(-UiFactory.EdgeMargin, -8f));

            Image machineFill;
            var track = UiFactory.ProgressBar(card.transform, "MachineBar", MachineView.StatusRunning, out machineFill);
            UiFactory.Stretch(UiFactory.Rect(track.gameObject), new Vector2(0f, 0f), new Vector2(1f, 0f),
                              new Vector2(UiFactory.EdgeMargin, 14f), new Vector2(-UiFactory.EdgeMargin, 26f));
            _machineBarFill = machineFill;
            _machineBarTrack = track;
        }

        GameObject BuildSidePanel(string name, string title, out Transform body)
        {
            var panel = UiFactory.Panel(transform, name, GardenPalette.PanelBackground, UiFactory.RadiusPanel);
            UiFactory.Stretch(UiFactory.Rect(panel.gameObject), new Vector2(1f, 0f), new Vector2(1f, 1f),
                              new Vector2(-(SidePanelWidth + UiFactory.EdgeMargin), UiFactory.EdgeMargin),
                              new Vector2(-UiFactory.EdgeMargin, -ContentTop));

            var header = UiFactory.Label(panel.transform, "Title", title, UiFactory.FontSizeTitle,
                                         TextAnchor.MiddleLeft, GardenPalette.TextPrimary, true);
            UiFactory.Stretch(UiFactory.Rect(header.gameObject), new Vector2(0f, 1f), new Vector2(1f, 1f),
                              new Vector2(UiFactory.EdgeMargin, -56f), new Vector2(-64f, -UiFactory.EdgeMargin));

            var headerRule = UiFactory.Panel(panel.transform, "HeaderRule", GardenPalette.PanelDivider);
            UiFactory.Stretch(UiFactory.Rect(headerRule.gameObject), new Vector2(0f, 1f), new Vector2(1f, 1f),
                              new Vector2(UiFactory.EdgeMargin, -65f), new Vector2(-UiFactory.EdgeMargin, -64f));

            var close = UiFactory.TextButton(panel.transform, "Close", "×",
                                             delegate { panel.gameObject.SetActive(false); },
                                             UiFactory.ButtonStyle.Quiet);
            var closeCaption = close.GetComponent<UiButtonStyle>();
            if (closeCaption != null && closeCaption.Caption != null)
            {
                closeCaption.Caption.fontSize = UiFactory.FontSizeTitle;
                closeCaption.Caption.color = GardenPalette.TextMuted;
            }
            var closeRect = UiFactory.Rect(close.gameObject);
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-6f, -6f);
            closeRect.sizeDelta = new Vector2(UiFactory.ButtonHeight, UiFactory.ButtonHeight);

            // Danh sach dai hon panel thi cuon, khong de chu bi cat.
            var viewport = UiFactory.Node(panel.transform, "Viewport");
            UiFactory.Stretch(UiFactory.Rect(viewport), Vector2.zero, Vector2.one,
                              new Vector2(UiFactory.EdgeMargin, UiFactory.EdgeMargin),
                              new Vector2(-UiFactory.EdgeMargin, -72f));
            viewport.AddComponent<RectMask2D>();

            var content = UiFactory.Node(viewport.transform, "Body");
            var contentRect = UiFactory.Rect(content);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            UiFactory.VerticalList(content, UiFactory.Gutter, new RectOffset(0, 0, 0, 8));
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = panel.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = UiFactory.Rect(viewport);
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            body = content.transform;
            return panel.gameObject;
        }

        void BuildInventoryRows(Transform body)
        {
            foreach (var crop in _session.Catalog.Crops)
            {
                string cropId = crop.Id;

                var card = UiFactory.Panel(body, "Row_" + cropId, GardenPalette.PanelSoft, UiFactory.RadiusControl);
                UiFactory.VerticalList(card.gameObject, 6f, new RectOffset(12, 12, 10, 10));

                var row = new InventoryRow { CropId = cropId };
                row.Name = UiFactory.Label(card.transform, "Name", crop.DisplayName, UiFactory.FontSizeRowTitle,
                                           TextAnchor.MiddleLeft, GardenPalette.TextPrimary);
                row.Meta = UiFactory.Label(card.transform, "Meta", "", UiFactory.FontSizeMeta,
                                           TextAnchor.MiddleLeft, GardenPalette.TextMuted);

                var buttons = UiFactory.Node(card.transform, "Buttons");
                UiFactory.HorizontalList(buttons, 8f, new RectOffset(0, 0, 0, 0));
                var buttonsLayout = buttons.AddComponent<LayoutElement>();
                buttonsLayout.minHeight = UiFactory.ButtonHeight;

                row.SellOne = UiFactory.TextButton(buttons.transform, "SellOne", "Bán 1",
                                                   delegate { Run(_session.SellRaw(cropId, 1)); });
                row.SellAll = UiFactory.TextButton(buttons.transform, "SellAll", "Bán hết",
                                                   delegate { Run(_session.SellAllRaw(cropId)); });
                _inventoryRows.Add(row);
            }

            var recipeTitle = UiFactory.Label(body, "RecipeTitle", "Công thức đang chọn", UiFactory.FontSizeMeta,
                                              TextAnchor.MiddleLeft, GardenPalette.TextMuted);
            var recipeTitleLayout = recipeTitle.gameObject.AddComponent<LayoutElement>();
            recipeTitleLayout.minHeight = 28f;
            recipeTitleLayout.preferredHeight = 28f;

            foreach (var recipe in _session.Catalog.Recipes)
            {
                string recipeId = recipe.Id;
                var button = UiFactory.TextButton(body, "Recipe_" + recipeId, recipe.DisplayName,
                                                  delegate { Run(_session.SelectRecipe(recipeId)); });
                _recipeRows.Add(new RecipeRow { RecipeId = recipeId, Select = button });
            }
        }

        void BuildUpgradeRows(Transform body)
        {
            foreach (var upgrade in _session.Catalog.Upgrades)
            {
                string upgradeId = upgrade.Id;

                var card = UiFactory.Panel(body, "Row_" + upgradeId, GardenPalette.PanelSoft, UiFactory.RadiusControl);
                UiFactory.VerticalList(card.gameObject, 6f, new RectOffset(12, 12, 10, 10));
                var cardLayout = card.gameObject.AddComponent<LayoutElement>();
                cardLayout.minHeight = 92f;

                var titleRow = UiFactory.Node(card.transform, "TitleRow");
                var titleLayout = UiFactory.HorizontalList(titleRow, 8f, new RectOffset(0, 0, 0, 0));
                titleLayout.childForceExpandWidth = false;
                // Chi dat san, khong dat cung: ten nang cap dai thi hang tu cao them.
                var titleRowLayout = titleRow.AddComponent<LayoutElement>();
                titleRowLayout.minHeight = 26f;

                var row = new UpgradeRow { UpgradeId = upgradeId, Card = card };

                row.Title = UiFactory.Label(titleRow.transform, "Title", upgrade.DisplayName,
                                            UiFactory.FontSizeRowTitle, TextAnchor.MiddleLeft,
                                            GardenPalette.TextPrimary);
                var titleFlex = row.Title.gameObject.AddComponent<LayoutElement>();
                titleFlex.flexibleWidth = 1f;

                row.Cost = UiFactory.Label(titleRow.transform, "Cost", "", UiFactory.FontSizeRowTitle,
                                           TextAnchor.MiddleRight, GardenPalette.Coin);
                row.Cost.horizontalOverflow = HorizontalWrapMode.Overflow;
                var costWidth = row.Cost.gameObject.AddComponent<LayoutElement>();
                costWidth.preferredWidth = 88f;
                costWidth.flexibleWidth = 0f;

                row.Description = UiFactory.Label(card.transform, "Desc", upgrade.Description,
                                                  UiFactory.FontSizeMeta, TextAnchor.UpperLeft,
                                                  GardenPalette.TextMuted);

                row.Status = UiFactory.Label(card.transform, "Status", "", UiFactory.FontSizeMeta,
                                             TextAnchor.UpperLeft, GardenPalette.StateWarn);
                row.Status.gameObject.SetActive(false);

                row.Buy = UiFactory.TextButton(card.transform, "Buy", "Mua",
                                               delegate
                {
                    var result = _session.Purchase(upgradeId);
                    Run(result);
                    if (result.Success)
                    {
                        var sfx = FindAnyObjectByType<SfxPlayer>();
                        if (sfx != null) sfx.PlayUnlock();
                    }
                });
                _upgradeRows.Add(row);
            }
        }

        void BuildDecorationRows(Transform body)
        {
            var note = UiFactory.Label(body, "Note",
                                       "Trang trí chỉ để nhìn cho vui — không đổi năng suất. " +
                                       "Gỡ ra được hoàn đủ xu nên cứ thử thoải mái.",
                                       UiFactory.FontSizeMeta, TextAnchor.UpperLeft, GardenPalette.TextMuted);
            var noteLayout = note.gameObject.AddComponent<LayoutElement>();
            noteLayout.minHeight = 60f;

            foreach (var decoration in _session.Catalog.Decorations)
            {
                string decorationId = decoration.Id;

                var rowGo = UiFactory.Node(body, "Row_" + decorationId);
                var rowLayout = rowGo.AddComponent<LayoutElement>();
                rowLayout.minHeight = 118f;
                UiFactory.VerticalList(rowGo, 4f, new RectOffset(0, 0, 0, 8));

                var label = UiFactory.Label(rowGo.transform, "Label", decoration.DisplayName,
                                            UiFactory.FontSizeMeta, TextAnchor.UpperLeft,
                                            GardenPalette.TextPrimary);
                var labelLayout = label.gameObject.AddComponent<LayoutElement>();
                labelLayout.minHeight = 56f;

                var choose = UiFactory.TextButton(rowGo.transform, "Choose", "Chọn",
                                                  delegate { BeginPlacing(decorationId); });
                _decorationRows.Add(new DecorationRow
                {
                    DecorationId = decorationId, Label = label, Choose = choose
                });
            }

            _removeModeButton = UiFactory.TextButton(body, "RemoveMode", "Gỡ đồ đã đặt",
                                                     delegate { ToggleRemoveMode(); },
                                                     UiFactory.ButtonStyle.Quiet);
        }

        void BeginPlacing(string decorationId)
        {
            var bootstrap = FindAnyObjectByType<GameBootstrap>();
            if (bootstrap != null) bootstrap.BeginPlacingDecoration(decorationId);
            RefreshVolatile();
        }

        void ToggleRemoveMode()
        {
            var bootstrap = FindAnyObjectByType<GameBootstrap>();
            if (bootstrap == null) return;
            bootstrap.SetRemovingDecorations(!bootstrap.RemovingDecorations);
            RefreshVolatile();
        }

        void CancelDecorationMode()
        {
            var bootstrap = FindAnyObjectByType<GameBootstrap>();
            if (bootstrap != null) bootstrap.CancelDecorationMode();
        }

        void RefreshDecoratePanel(GameState state, ContentCatalog catalog)
        {
            var bootstrap = FindAnyObjectByType<GameBootstrap>();
            string placing = bootstrap != null ? bootstrap.PlacingDecorationId : null;
            bool removing = bootstrap != null && bootstrap.RemovingDecorations;

            for (int i = 0; i < _decorationRows.Count; i++)
            {
                var row = _decorationRows[i];
                var decoration = catalog.Decoration(row.DecorationId);
                bool affordable = state.Coins >= decoration.Cost;
                bool isPlacing = row.DecorationId == placing;

                string text = decoration.DisplayName + "  ·  " + decoration.Cost + " xu\n" + decoration.Description;
                if (isPlacing) text += "\nĐang đặt — bấm vào vườn, chuột phải để thoát.";
                else if (!affordable) text += "\nThiếu " + (decoration.Cost - state.Coins) + " xu.";
                row.Label.text = text;

                UiFactory.SetButtonCaption(row.Choose, isPlacing ? "Đang đặt…" : "Chọn");
                UiFactory.SetButtonState(row.Choose, !affordable
                    ? UiFactory.ButtonState.Disabled
                    : (isPlacing ? UiFactory.ButtonState.Selected : UiFactory.ButtonState.Normal));
            }

            int placed = state.Decorations.Count;
            UiFactory.SetButtonCaption(_removeModeButton,
                removing ? "Đang gỡ — chuột phải để thoát" : "Gỡ đồ đã đặt (" + placed + ")");
            UiFactory.SetButtonState(_removeModeButton, placed == 0
                ? UiFactory.ButtonState.Disabled
                : (removing ? UiFactory.ButtonState.Selected : UiFactory.ButtonState.Normal));
        }

        void BuildSettingsRows(Transform body)
        {
            UiFactory.Label(body, "Note",
                            "Bản dựng mốc A dùng khối primitive. Dữ liệu test được ghi cục bộ, " +
                            "không gửi ra ngoài.",
                            UiFactory.FontSizeMeta, TextAnchor.UpperLeft, GardenPalette.TextMuted);

            // Trang thai am thanh nam trong PlayerPrefs, phai doc that chu khong doan la dang bat.
            var sfx = FindAnyObjectByType<SfxPlayer>();
            bool soundOn = sfx == null || sfx.Enabled;

            Button soundButton = null;
            soundButton = UiFactory.TextButton(body, "Sound",
                                               soundOn ? "Âm thanh: đang bật" : "Âm thanh: đang tắt", delegate
            {
                var player = FindAnyObjectByType<SfxPlayer>();
                if (player == null) return;
                player.Enabled = !player.Enabled;
                UiFactory.SetButtonCaption(soundButton, player.Enabled ? "Âm thanh: đang bật" : "Âm thanh: đang tắt");
            });

            UiFactory.TextButton(body, "Export", "Xuất dữ liệu test", delegate
            {
                var bootstrap = GetComponentInParent<GameBootstrap>();
                if (bootstrap == null) bootstrap = FindAnyObjectByType<GameBootstrap>();
                if (bootstrap != null) ShowToast(bootstrap.ExportTestData());
            });

            var resetConfirm = UiFactory.Node(body, "ResetConfirm");
            UiFactory.VerticalList(resetConfirm, 8f, new RectOffset(0, 0, 0, 0));
            resetConfirm.SetActive(false);

            UiFactory.Label(resetConfirm.transform, "ResetPrompt",
                            "Xóa toàn bộ tiến độ và bắt đầu vườn mới. Không hoàn lại được.",
                            UiFactory.FontSizeMeta, TextAnchor.UpperLeft, GardenPalette.StateWarn);

            var resetButtons = UiFactory.Node(resetConfirm.transform, "ResetButtons");
            UiFactory.HorizontalList(resetButtons, 8f, new RectOffset(0, 0, 0, 0));
            var resetButtonsLayout = resetButtons.AddComponent<LayoutElement>();
            resetButtonsLayout.minHeight = UiFactory.ButtonHeight;

            var resetButton = UiFactory.TextButton(body, "Reset", "Chơi lại từ đầu",
                                                   delegate { resetConfirm.SetActive(true); },
                                                   UiFactory.ButtonStyle.Quiet);
            resetButton.transform.SetSiblingIndex(resetConfirm.transform.GetSiblingIndex());

            // Nut giu lai dung truoc, nut xoa mang mau canh bao rieng.
            UiFactory.TextButton(resetButtons.transform, "ResetNo", "Giữ lại",
                                 delegate { resetConfirm.SetActive(false); });
            var resetYes = UiFactory.TextButton(resetButtons.transform, "ResetYes", "Xóa tiến độ", delegate
            {
                var bootstrap = FindAnyObjectByType<GameBootstrap>();
                if (bootstrap != null) bootstrap.ResetProgress();
                resetConfirm.SetActive(false);
                ShowToast("Đã tạo vườn mới.");
            });
            var dangerStyle = resetYes.GetComponent<UiButtonStyle>();
            if (dangerStyle != null)
            {
                dangerStyle.BaseColor = GardenPalette.ButtonDanger;
                if (dangerStyle.Background != null) dangerStyle.Background.color = GardenPalette.ButtonDanger;
            }

            if (Debug.isDebugBuild || Application.isEditor)
            {
                UiFactory.Label(body, "DevNote", "Công cụ dev (không có trong bản playtest thường)",
                                UiFactory.FontSizeMeta, TextAnchor.UpperLeft, GardenPalette.TextMuted);

                UiFactory.TextButton(body, "Skip1", "Tua 1 phút", delegate
                {
                    _session.DebugAdvance(60000);
                    ShowToast("Đã tua 1 phút mô phỏng.");
                }, UiFactory.ButtonStyle.Quiet);
                UiFactory.TextButton(body, "Skip60", "Tua 60 phút", delegate
                {
                    _session.DebugAdvance(3600000);
                    ShowToast("Đã tua 60 phút mô phỏng.");
                }, UiFactory.ButtonStyle.Quiet);
            }

            UiFactory.TextButton(body, "Quit", "Thoát game", delegate
            {
                var bootstrap = FindAnyObjectByType<GameBootstrap>();
                if (bootstrap != null) bootstrap.QuitGame();
            }, UiFactory.ButtonStyle.Quiet);
        }

        GameObject BuildPlotPopup()
        {
            // Chieu cao do so nut quyet dinh: them mot cay nua la popup tu cao them.
            var panel = UiFactory.Panel(transform, "PlotPopup", GardenPalette.PanelBackground, UiFactory.RadiusPanel);
            var rect = UiFactory.Rect(panel.gameObject);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(UiFactory.EdgeMargin, UiFactory.EdgeMargin);
            rect.sizeDelta = new Vector2(340f, 0f);
            UiFactory.VerticalList(panel.gameObject, 8f, new RectOffset(16, 16, 16, 16));
            var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            _plotPopupTitle = UiFactory.Label(panel.transform, "Title", "", UiFactory.FontSizeRowTitle,
                                              TextAnchor.MiddleLeft, GardenPalette.TextPrimary);
            _plotPopupState = UiFactory.Label(panel.transform, "State", "", UiFactory.FontSizeBody,
                                              TextAnchor.UpperLeft, GardenPalette.TextMuted);

            foreach (var crop in _session.Catalog.Crops)
            {
                string cropId = crop.Id;
                var button = UiFactory.TextButton(panel.transform, "Crop_" + cropId, crop.DisplayName, delegate
                {
                    if (_selectedPlotId < 0) return;
                    Run(_session.SetNextCrop(_selectedPlotId, cropId));
                });
                _cropChoiceRows.Add(new CropChoiceRow { CropId = cropId, Choose = button });
            }

            UiFactory.TextButton(panel.transform, "ClosePopup", "Đóng", delegate { ClosePlotPopup(); },
                                 UiFactory.ButtonStyle.Quiet);
            return panel.gameObject;
        }

        /// <summary>Popup bat buoc tra loi: mot lop toi phu ca man hinh roi moi den the noi dung.</summary>
        GameObject BuildModal(string name, out Transform card, Vector2 cardSize)
        {
            var container = UiFactory.Node(transform, name);
            UiFactory.Stretch(UiFactory.Rect(container), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var scrim = UiFactory.Panel(container.transform, "Scrim", GardenPalette.PanelScrim);
            UiFactory.Stretch(UiFactory.Rect(scrim.gameObject), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var panel = UiFactory.Panel(container.transform, "Card", GardenPalette.PanelBackground,
                                        UiFactory.RadiusPanel);
            var rect = UiFactory.Rect(panel.gameObject);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = cardSize;

            card = panel.transform;
            return container;
        }

        GameObject BuildOfflinePopup()
        {
            Transform card;
            // Chieu cao theo bao cao: bao cao ngan thi the khong hoac mot khoang trong nua duoi.
            var container = BuildModal("OfflineModal", out card, new Vector2(560f, 0f));
            UiFactory.VerticalList(card.gameObject, 12f, new RectOffset(24, 24, 20, 20));
            var fitter = card.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            UiFactory.Label(card, "Title", "Khu vườn vẫn chạy khi bạn vắng mặt",
                            UiFactory.FontSizeTitle, TextAnchor.UpperLeft, GardenPalette.TextPrimary);

            _offlineText = UiFactory.Label(card, "Body", "", UiFactory.FontSizeBody,
                                           TextAnchor.UpperLeft, GardenPalette.TextPrimary);
            _offlineText.lineSpacing = 1.25f;

            // Mot loi thoat duy nhat cho ca the: de no chay het be ngang cho khoi phai tim.
            UiFactory.TextButton(card, "Continue", "Tiếp tục", delegate
            {
                _session.AcknowledgeOfflineSummary();
                container.SetActive(false);
            });

            return container;
        }

        GameObject BuildBlockedPanel()
        {
            Transform card;
            var container = BuildModal("BlockedModal", out card, new Vector2(600f, 360f));

            var accent = UiFactory.Panel(card, "Accent", new Color(0.85f, 0.35f, 0.30f));
            UiFactory.Stretch(UiFactory.Rect(accent.gameObject), new Vector2(0f, 1f), new Vector2(1f, 1f),
                              new Vector2(0f, -4f), new Vector2(0f, 0f));

            var title = UiFactory.Label(card, "Title", "Không mở được tiến độ", UiFactory.FontSizeTitle,
                                        TextAnchor.UpperLeft, GardenPalette.TextPrimary);
            UiFactory.Stretch(UiFactory.Rect(title.gameObject), new Vector2(0f, 1f), new Vector2(1f, 1f),
                              new Vector2(24f, -70f), new Vector2(-24f, -20f));

            _blockedText = UiFactory.Label(card, "Body", "", UiFactory.FontSizeBody,
                                           TextAnchor.UpperLeft, GardenPalette.TextPrimary);
            _blockedText.lineSpacing = 1.25f;
            UiFactory.Stretch(UiFactory.Rect(_blockedText.gameObject), Vector2.zero, Vector2.one,
                              new Vector2(24f, 24f), new Vector2(-24f, -78f));

            return container;
        }

        void BuildToast()
        {
            // Toast khong duoc chan click xuong vuon trong 3 giay no hien.
            _toastPanel = UiFactory.Panel(transform, "Toast", GardenPalette.Toast, UiFactory.RadiusPanel);
            _toastPanel.raycastTarget = false;
            var rect = UiFactory.Rect(_toastPanel.gameObject);
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 24f);
            rect.sizeDelta = new Vector2(560f, 0f);
            UiFactory.VerticalList(_toastPanel.gameObject, 0f, new RectOffset(16, 16, 10, 10));
            var toastFitter = _toastPanel.gameObject.AddComponent<ContentSizeFitter>();
            toastFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            toastFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            var toastLayout = _toastPanel.gameObject.AddComponent<LayoutElement>();
            toastLayout.minHeight = UiFactory.ButtonHeight;

            _toastLabel = UiFactory.Label(_toastPanel.transform, "Label", "", UiFactory.FontSizeBody,
                                          TextAnchor.MiddleCenter, GardenPalette.TextPrimary);
            _toastPanel.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- cap nhat

        void Update()
        {
            if (_session == null || _session.State == null) return;
            RefreshVolatile();
            if (_toastPanel != null && _toastPanel.gameObject.activeSelf && Time.unscaledTime > _toastHideTime)
                _toastPanel.gameObject.SetActive(false);
        }

        public void Refresh()
        {
            if (_session == null || _session.State == null) return;
            RefreshVolatile();

            var state = _session.State;
            if (state.PendingOfflineSummary != null && !state.PendingOfflineSummary.Seen)
                ShowOfflineReport(state.PendingOfflineSummary);
        }

        void RefreshVolatile()
        {
            var state = _session.State;
            var catalog = _session.Catalog;

            _coinsLabel.text = state.Coins + " xu";
            _goalLabel.text = GoalText(TutorialGuide.CurrentStep(state, catalog), state, catalog);

            RefreshMachineCard(state);

            SetNavActive(_inventoryButton, _inventoryPanel.activeSelf);
            SetNavActive(_upgradeButton, _upgradePanel.activeSelf);
            SetNavActive(_decorateButton, _decoratePanel.activeSelf);
            SetNavActive(_settingsButton, _settingsPanel.activeSelf);
            _decorateButton.gameObject.SetActive(_session.DecoratingUnlocked);

            if (_inventoryPanel.activeSelf) RefreshInventoryPanel(state, catalog);
            if (_upgradePanel.activeSelf) RefreshUpgradePanel(state, catalog);
            if (_decoratePanel.activeSelf) RefreshDecoratePanel(state, catalog);
            if (_plotPopup.activeSelf) RefreshPlotPopup(state, catalog);
        }

        /// <summary>Nut cua panel dang mo phai nhin ra ngay la dang mo.</summary>
        void SetNavActive(Button button, bool active)
        {
            if (button == null) return;
            UiFactory.SetButtonState(button, active ? UiFactory.ButtonState.Selected : UiFactory.ButtonState.Normal);
            button.interactable = true;
        }

        void RefreshMachineCard(GameState state)
        {
            var catalog = _session.Catalog;
            RecipeDefinition selected;
            catalog.TryGetRecipe(state.Machine.SelectedRecipeId, out selected);

            if (state.Machine.BatchRunning)
            {
                RecipeDefinition running;
                catalog.TryGetRecipe(state.Machine.BatchRecipeId, out running);
                string runningName = running != null ? running.DisplayName
                                                     : (selected != null ? selected.DisplayName : "trà");
                long remaining = state.Machine.BatchFinishAtMs - state.SimulationTimeMs;
                if (remaining < 0) remaining = 0;
                _machineLabel.text = "Máy đang pha " + runningName +
                                     " — còn " + Seconds(remaining) + " s, được " +
                                     state.Machine.BatchOutputCoins + " xu";
                _machineDot.color = MachineView.StatusRunning;
                _machineBarTrack.color = GardenPalette.TrackEmpty;
                _machineBarFill.color = MachineView.StatusRunning;
                UiFactory.SetProgress(_machineBarFill, MachineView.BatchProgress(state));
                return;
            }

            Color status;
            if (selected == null)
            {
                _machineLabel.text = "Máy chưa chọn công thức nào.";
                status = MachineView.StatusIdle;
            }
            else
            {
                string missingCropId;
                long missingAmount;
                if (_session.Simulation.TryGetMissingInput(state, out missingCropId, out missingAmount))
                {
                    _machineLabel.text = "Máy chờ: thiếu " + missingAmount + " " +
                                         catalog.Crop(missingCropId).DisplayName +
                                         " cho " + selected.DisplayName;
                    status = MachineView.StatusWaiting;
                }
                else
                {
                    _machineLabel.text = "Máy sẵn sàng với " + selected.DisplayName;
                    status = MachineView.StatusIdle;
                }
            }

            _machineDot.color = status;
            // Ranh luon toi: to mau ranh khi chua co me nao chay lam thanh rong trong nhu da day.
            _machineBarTrack.color = GardenPalette.TrackEmpty;
            _machineBarFill.color = status;
            UiFactory.SetProgress(_machineBarFill, 0f);
        }

        void RefreshInventoryPanel(GameState state, ContentCatalog catalog)
        {
            RecipeDefinition selected;
            catalog.TryGetRecipe(state.Machine.SelectedRecipeId, out selected);
            string usedCropId = selected != null ? selected.InputCropId : null;

            for (int i = 0; i < _inventoryRows.Count; i++)
            {
                var row = _inventoryRows[i];
                var crop = catalog.Crop(row.CropId);
                long amount = state.InventoryOf(row.CropId);
                bool unlocked = state.UnlockedCropIds.Contains(row.CropId);
                bool usedByMachine = row.CropId == usedCropId;

                row.Name.text = crop.DisplayName + ": " + amount;
                row.Name.color = unlocked ? GardenPalette.TextPrimary : GardenPalette.TextMuted;

                string meta = crop.RawSellPrice + " xu/đơn vị";
                if (usedByMachine) meta += "  ·  máy đang dùng";
                if (!unlocked) meta += "  ·  chưa mở khóa";
                row.Meta.text = meta;
                row.Meta.color = usedByMachine ? GardenPalette.StateOk : GardenPalette.TextMuted;

                UiFactory.SetInteractable(row.SellOne, amount >= 1);
                UiFactory.SetInteractable(row.SellAll, amount >= 1);
                UiFactory.SetButtonCaption(row.SellAll,
                                           amount >= 1 ? "Bán hết · " + amount * crop.RawSellPrice + " xu" : "Bán hết");
            }

            for (int i = 0; i < _recipeRows.Count; i++)
            {
                var row = _recipeRows[i];
                var recipe = catalog.Recipe(row.RecipeId);
                bool unlocked = state.UnlockedRecipeIds.Contains(row.RecipeId);
                bool isSelected = row.RecipeId == state.Machine.SelectedRecipeId;
                string caption = recipe.DisplayName + " — " + recipe.InputCount + " " +
                                 catalog.Crop(recipe.InputCropId).DisplayName + " → " + recipe.OutputCoins + " xu";
                // Dau dan dat truoc: them chu o duoi lam nhan dai qua mot dong.
                if (!unlocked) caption += " (chưa mở khóa)";
                else if (isSelected) caption = "▶ " + caption;
                UiFactory.SetButtonCaption(row.Select, caption);
                UiFactory.SetButtonState(row.Select, !unlocked
                    ? UiFactory.ButtonState.Disabled
                    : (isSelected ? UiFactory.ButtonState.Selected : UiFactory.ButtonState.Normal));
            }
        }

        void RefreshUpgradePanel(GameState state, ContentCatalog catalog)
        {
            bool machineIsBottleneck = IsMachineBottleneck(state, catalog);

            for (int i = 0; i < _upgradeRows.Count; i++)
            {
                var row = _upgradeRows[i];
                var upgrade = catalog.Upgrade(row.UpgradeId);
                bool bought = state.UpgradeLevel(row.UpgradeId) >= 1;

                string reason;
                bool purchasable = _session.IsPurchasable(row.UpgradeId, out reason);

                row.Title.text = upgrade.DisplayName;
                row.Title.color = bought ? GardenPalette.TextMuted : GardenPalette.TextPrimary;
                row.Cost.text = upgrade.Cost + " xu";
                row.Cost.color = bought ? GardenPalette.TextMuted : GardenPalette.Coin;
                row.Description.text = upgrade.Description;
                row.Card.color = bought ? GardenPalette.PanelSoftMuted : GardenPalette.PanelSoft;

                string status = null;
                Color statusColor = GardenPalette.StateWarn;
                if (bought)
                {
                    status = "Đã mua.";
                    statusColor = GardenPalette.StateOk;
                }
                else if (upgrade.Kind == UpgradeKind.ExpandPlots && machineIsBottleneck)
                {
                    status = "Máy đang là giới hạn công suất, mở đất chưa làm tăng tiền trà.";
                }
                else if (!purchasable && reason != null)
                {
                    status = reason;
                }

                row.Status.gameObject.SetActive(status != null);
                if (status != null)
                {
                    row.Status.text = status;
                    row.Status.color = statusColor;
                }

                // Nang cap da mua khong can nut nua — thu gon de cai mua duoc noi len tren.
                row.Buy.gameObject.SetActive(!bought);
                if (!bought)
                {
                    UiFactory.SetButtonCaption(row.Buy, "Mua " + upgrade.Cost + " xu");
                    UiFactory.SetInteractable(row.Buy, purchasable);
                }
            }
        }

        /// <summary>
        /// Vuon co the cap nhieu nguyen lieu hon suc pha cua may. Panel nang cap can noi ro dieu nay.
        /// </summary>
        bool IsMachineBottleneck(GameState state, ContentCatalog catalog)
        {
            RecipeDefinition recipe;
            if (!catalog.TryGetRecipe(state.Machine.SelectedRecipeId, out recipe)) return false;

            int plotsGrowingRecipeCrop = 0;
            for (int i = 0; i < state.Plots.Count; i++)
            {
                var plot = state.Plots[i];
                if (!plot.Unlocked) continue;
                if (plot.NextCropId == recipe.InputCropId || plot.CurrentCropId == recipe.InputCropId)
                    plotsGrowingRecipeCrop++;
            }
            if (plotsGrowingRecipeCrop == 0) return false;

            var crop = catalog.Crop(recipe.InputCropId);
            double growthMs = _session.Simulation.GrowthMsFor(state, crop.Id);
            double brewMs = _session.Simulation.BrewMsFor(state, recipe.Id);
            double unitsPerMinuteFromField = plotsGrowingRecipeCrop * crop.Yield * 60000.0 / growthMs;
            double unitsPerMinuteMachineCanUse = 60000.0 / brewMs * recipe.InputCount;
            return unitsPerMinuteFromField >= unitsPerMinuteMachineCanUse;
        }

        void RefreshPlotPopup(GameState state, ContentCatalog catalog)
        {
            var plot = state.Plot(_selectedPlotId);
            if (plot == null) { ClosePlotPopup(); return; }

            _plotPopupTitle.text = "Ô " + (plot.PlotId + 1);

            string info;
            if (!plot.Unlocked)
            {
                info = "Chưa mở. Mua nâng cấp mở vườn để dùng ô này.";
            }
            else if (plot.Phase == PlotPhase.Empty)
            {
                info = "Đang trống. Chọn cây để gieo ngay, miễn phí.";
            }
            else
            {
                var current = catalog.Crop(plot.CurrentCropId);
                long remaining = plot.FinishAtMs - state.SimulationTimeMs;
                if (remaining < 0) remaining = 0;
                info = plot.Phase == PlotPhase.Ready
                    ? current.DisplayName + " đã chín, bấm vào ô để thu."
                    : current.DisplayName + ", còn " + Seconds(remaining) + " s.";
                if (plot.NextCropId != null)
                    info += "\nVụ tiếp theo: " + catalog.Crop(plot.NextCropId).DisplayName + ".";
            }
            _plotPopupState.text = info;

            for (int i = 0; i < _cropChoiceRows.Count; i++)
            {
                var row = _cropChoiceRows[i];
                var crop = catalog.Crop(row.CropId);
                bool unlocked = state.UnlockedCropIds.Contains(row.CropId);
                bool selectable = unlocked && plot.Unlocked;
                bool isNext = plot.NextCropId == row.CropId;

                string caption = crop.DisplayName + " — " + Seconds(_session.Simulation.GrowthMsFor(state, crop.Id)) + " s";
                if (!unlocked) caption += " (chưa mở khóa)";
                else if (isNext) caption += " · vụ tiếp theo";
                UiFactory.SetButtonCaption(row.Choose, caption);
                UiFactory.SetButtonState(row.Choose, !selectable
                    ? UiFactory.ButtonState.Disabled
                    : (isNext ? UiFactory.ButtonState.Selected : UiFactory.ButtonState.Normal));
            }
        }

        static string GoalText(TutorialStep step, GameState state, ContentCatalog catalog)
        {
            switch (step)
            {
                case TutorialStep.PlantAll:
                    return "Mục tiêu: bấm vào ô đất trống và chọn cây để gieo.";
                case TutorialStep.WaitAndHarvest:
                    return "Mục tiêu: chờ cây chín rồi bấm vào ô để thu và gieo lại.";
                case TutorialStep.WatchTeaSell:
                    return "Mục tiêu: máy đang pha trà, trà bán xong sẽ thành xu.";
                case TutorialStep.SaveForRobot:
                    return "Mục tiêu: gom " + catalog.Upgrade(DefaultContent.UpgradeRobot).Cost +
                           " xu để kích hoạt robot (còn thiếu " +
                           System.Math.Max(0, catalog.Upgrade(DefaultContent.UpgradeRobot).Cost - state.Coins) +
                           " xu).";
                default:
                    return "Mục tiêu: chọn nâng cấp tiếp theo trong bảng Nâng cấp.";
            }
        }

        // ---------------------------------------------------------------- tuong tac

        public void OpenPlotPopup(int plotId)
        {
            if (_session == null || _session.State == null) return;
            _selectedPlotId = plotId;
            _plotPopup.SetActive(true);
            RefreshPlotPopup(_session.State, _session.Catalog);
            // Popup tu tinh chieu cao: phai dung lai ngay chu khong doi frame sau.
            LayoutRebuilder.ForceRebuildLayoutImmediate(UiFactory.Rect(_plotPopup));
        }

        public void ClosePlotPopup()
        {
            _selectedPlotId = -1;
            _plotPopup.SetActive(false);
        }

        /// <summary>Dung cho anh chup QA: mo thang mot panel tu dong lenh.</summary>
        public void OpenPanelByName(string panelName)
        {
            if (panelName == "upgrade") TogglePanel(_upgradePanel);
            else if (panelName == "inventory") TogglePanel(_inventoryPanel);
            else if (panelName == "settings") TogglePanel(_settingsPanel);
            else if (panelName == "decorate") TogglePanel(_decoratePanel);
            else if (panelName == "plot") OpenPlotPopup(0);
        }

        void TogglePanel(GameObject panel)
        {
            bool willOpen = !panel.activeSelf;
            _inventoryPanel.SetActive(false);
            _upgradePanel.SetActive(false);
            _settingsPanel.SetActive(false);
            _decoratePanel.SetActive(false);
            panel.SetActive(willOpen);
            if (panel != _decoratePanel) CancelDecorationMode();
            if (!willOpen) return;

            RefreshVolatile();
            var scroll = panel.GetComponent<ScrollRect>();
            if (scroll != null && scroll.content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
        }

        void Run(CommandResult result)
        {
            if (result == null) return;
            if (!result.Success) ShowToast(result.FailureReason);
        }

        public void ShowToast(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            _toastLabel.text = message;
            _toastPanel.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(UiFactory.Rect(_toastPanel.gameObject));
            _toastHideTime = Time.unscaledTime + 3f;
        }

        void ShowOfflineReport(OfflineSummary summary)
        {
            var catalog = _session.Catalog;
            string text = "Thời gian được tính: " + WholeTime(summary.ElapsedMs) + ".\n" +
                          "Xu kiếm được: " + summary.CoinsGained + ".\n";

            if (summary.ItemsGained.Count == 0)
            {
                text += "Kho không đổi.\n";
            }
            else
            {
                text += "Kho thay đổi: ";
                for (int i = 0; i < summary.ItemsGained.Count; i++)
                {
                    var item = summary.ItemsGained[i];
                    if (i > 0) text += ", ";
                    text += catalog.Crop(item.CropId).DisplayName + " " +
                            (item.Amount > 0 ? "+" : "") + item.Amount;
                }
                text += ".\n";
            }

            if (summary.MachineWaiting)
                text += "Máy đang chờ nguyên liệu cho công thức đang chọn.\n";
            if (!_session.State.RobotUnlocked)
                text += "Chưa có robot nên cây chỉ lớn đến khi chín, không được thu tự động.\n";

            text += "Tối đa " + (_session.Catalog.Balance.OfflineCapMs / 3600000) + " giờ tiến độ cho một lần vắng mặt.";

            _offlineText.text = text;
            _offlinePopup.SetActive(true);
            // The tu tinh chieu cao theo do dai bao cao: dung lai ngay trong frame nay.
            LayoutRebuilder.ForceRebuildLayoutImmediate(UiFactory.Rect(_offlineText.transform.parent.gameObject));
        }

        public void ShowBlockedMessage(string message)
        {
            _blockedText.text = message;
            _blockedPanel.SetActive(true);
        }

        /// <summary>
        /// Giay tron, lam tron len. So le kieu "14.4" nhay muoi lan mot giay va con doi dau
        /// thap phan theo locale cua may.
        /// </summary>
        static string Seconds(long milliseconds)
        {
            if (milliseconds < 0) milliseconds = 0;
            return Mathf.CeilToInt(milliseconds / 1000f).ToString();
        }

        static string WholeTime(long milliseconds)
        {
            if (milliseconds < 60000) return Mathf.RoundToInt(milliseconds / 1000f) + " giây";
            return Mathf.RoundToInt(milliseconds / 60000f) + " phút";
        }
    }
}
