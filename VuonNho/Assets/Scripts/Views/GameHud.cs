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
        /// <summary>Nguon icon. Co the null: HUD phai chay duoc khi chua co bo icon nao.</summary>
        GardenSkin _skin;

        Text _coinsLabel;
        Text _goalLabel;
        Text _machineLabel;
        Image _machineDot;
        Image _machineBarFill;
        Image _machineBarTrack;
        Image _machineIcon;

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
        GameObject _offlineMachineRow;
        Text _offlineMachineText;
        GameObject _offlineRobotRow;
        Text _offlineRobotText;
        GameObject _workshopPanel;
        Button _workshopButton;
        Text _workerSummary;
        Text _workerWarning;
        Button _hireButton;
        Button _fireButton;

        /// <summary>Mot hang trong bang xuong, ung voi mot cong doan.</summary>
        sealed class StationRow
        {
            public string StageId;
            public Image Card;
            public Text Title;
            public Text Cost;
            public Text Flow;
            public Text Status;
            public Button Buy;
        }

        readonly List<StationRow> _stationRows = new List<StationRow>();
        Text _offlineCapText;
        Text _blockedText;
        Text _toastLabel;
        Image _toastPanel;
        float _toastHideTime;

        int _selectedPlotId = -1;

        readonly List<InventorySlot> _slots = new List<InventorySlot>();
        string _selectedItemId;
        Image _detailIcon;
        Text _detailName;
        Text _detailMeta;
        Button _sellOne;
        Button _sellAll;
        readonly List<UpgradeRow> _upgradeRows = new List<UpgradeRow>();
        readonly List<CropChoiceRow> _cropChoiceRows = new List<CropChoiceRow>();
        readonly List<RecipeRow> _recipeRows = new List<RecipeRow>();

        /// <summary>Mot o trong tui do: mot mat hang, mot chang cua no.</summary>
        sealed class InventorySlot
        {
            public string ItemId;
            public string CropId;
            public string Suffix;
            public Button Button;
            public Image Background;
            public Image Icon;
            public Text Count;
            public Image CountPill;
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

        public void Bind(GameSession session, GardenSkin skin)
        {
            _session = session;
            // Truyen skin truoc BuildUi: nhieu o icon duoc dat ngay luc dung UI.
            _skin = skin;
            BuildUi();
            _session.StateChanged += Refresh;
            Refresh();
        }

        /// <summary>Khong co skin hay khong co anh thi tra ve null va HUD giu nguyen chu.</summary>
        Sprite IconFor(string id)
        {
            return _skin != null ? _skin.IconFor(id) : null;
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
            Transform inventoryFooter;
            _inventoryPanel = BuildSidePanel("InventoryPanel", "Kho nguyên liệu", out body,
                                             out inventoryFooter, InventoryFooterHeight);
            BuildInventoryGrid(body);
            BuildInventoryDetail(inventoryFooter);

            _upgradePanel = BuildSidePanel("UpgradePanel", "Nâng cấp và mở khóa", out body);
            BuildUpgradeRows(body);

            _decoratePanel = BuildSidePanel("DecoratePanel", "Trang trí khu vườn", out body);
            BuildDecorationRows(body);

            _workshopPanel = BuildSidePanel("WorkshopPanel", "Xưởng chế biến trà", out body);
            BuildWorkshopRows(body);

            _settingsPanel = BuildSidePanel("SettingsPanel", "Cài đặt và công cụ test", out body);
            BuildSettingsRows(body);

            _plotPopup = BuildPlotPopup();
            // Hai popup nay phu scrim len ca panel nen phai dung sau chung trong hierarchy.
            _offlinePopup = BuildOfflinePopup();
            _blockedPanel = BuildBlockedPanel();
            // Toast dung cuoi cung de luon noi tren moi thu.
            BuildToast();

            _decoratePanel.SetActive(false);
            _workshopPanel.SetActive(false);
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
            // Bo chu "xu" thi chip hep lai dung bang the, khong de lai khoang trong: 144 px van
            // du cho so tam chu so, bang dung so cho ma phien ban co chu "xu" cho duoc.
            chipRect.sizeDelta = new Vector2(144f, 40f);

            var coinRow = UiFactory.Node(coinChip.transform, "Coins");
            UiFactory.Stretch(UiFactory.Rect(coinRow), Vector2.zero, Vector2.one,
                              new Vector2(14f, 0f), new Vector2(-14f, 0f));
            var coinLayout = UiFactory.HorizontalList(coinRow, 8f, new RectOffset(0, 0, 0, 0));
            coinLayout.childAlignment = TextAnchor.MiddleLeft;
            coinLayout.childForceExpandWidth = false;
            coinLayout.childForceExpandHeight = false;

            // Chip xu chi cao 40 px: dung co icon cua the thi chieu cao dong glyph vuot ra ngoai.
            var coinSymbol = UiFactory.Symbol(coinRow.transform, "CoinSymbol",
                                              UiFactory.Symbols.LocalAtm, UiFactory.IconSizeRow);
            coinSymbol.color = GardenPalette.TextCoin;

            // Da co icon tien dung canh thi chu "xu" chi la nhan lap lai.
            _coinsLabel = UiFactory.Label(coinRow.transform, "Value", "0", UiFactory.FontSizeTitle,
                                          TextAnchor.MiddleLeft, GardenPalette.TextCoin, true);
            _coinsLabel.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Muc tieu la loi nhac, khong phai tieu de: de mo hon chip xu ben trai.
            _goalLabel = UiFactory.Label(topBar.transform, "Goal", "", UiFactory.FontSizeBody,
                                         TextAnchor.MiddleLeft, GardenPalette.TextMuted);
            _goalLabel.verticalOverflow = VerticalWrapMode.Truncate;
            UiFactory.Stretch(UiFactory.Rect(_goalLabel.gameObject), new Vector2(0f, 0f), new Vector2(1f, 1f),
                              new Vector2(176f, 0f), new Vector2(-728f, 0f));

            // Nam nut deu nhau: nut rong nhat la "Nang cap" can khoang 140 px khi co ca icon lan
            // chu. "Xuong" ngan hon han nen ca day duoc 712 px la du. Hep hon la chu bi xuong dong.
            var topButtons = UiFactory.Node(topBar.transform, "Buttons");
            UiFactory.Stretch(UiFactory.Rect(topButtons), new Vector2(1f, 0f), new Vector2(1f, 1f),
                              new Vector2(-712f, 10f), new Vector2(-UiFactory.EdgeMargin, -10f));
            UiFactory.HorizontalList(topButtons, 8f, new RectOffset(0, 0, 0, 0));

            _inventoryButton = UiFactory.TextButton(topButtons.transform, "InventoryButton", "Kho",
                                                    delegate { TogglePanel(_inventoryPanel); },
                                                    UiFactory.ButtonStyle.Quiet,
                                                   symbol: UiFactory.Symbols.Inventory2);
            _upgradeButton = UiFactory.TextButton(topButtons.transform, "UpgradeButton", "Nâng cấp",
                                                  delegate { TogglePanel(_upgradePanel); },
                                                  UiFactory.ButtonStyle.Quiet,
                                                   symbol: UiFactory.Symbols.Upgrade);
            _decorateButton = UiFactory.TextButton(topButtons.transform, "DecorateButton", "Trang trí",
                                                   delegate { TogglePanel(_decoratePanel); },
                                                   UiFactory.ButtonStyle.Quiet,
                                                   symbol: UiFactory.Symbols.FormatPaint);
            _workshopButton = UiFactory.TextButton(topButtons.transform, "WorkshopButton", "Xưởng",
                                                   delegate { TogglePanel(_workshopPanel); },
                                                   UiFactory.ButtonStyle.Quiet,
                                                   symbol: UiFactory.Symbols.Factory);
            _settingsButton = UiFactory.TextButton(topButtons.transform, "SettingsButton", "Cài đặt",
                                                   delegate { TogglePanel(_settingsPanel); },
                                                   UiFactory.ButtonStyle.Quiet,
                                                   symbol: UiFactory.Symbols.Settings);

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
            cardRect.sizeDelta = new Vector2(464f, 88f);

            var dot = UiFactory.Panel(card.transform, "MachineDot", MachineView.StatusIdle, UiFactory.RadiusDot);
            var dotRect = UiFactory.Rect(dot.gameObject);
            dotRect.anchorMin = new Vector2(0f, 1f);
            dotRect.anchorMax = new Vector2(0f, 1f);
            dotRect.pivot = new Vector2(0f, 1f);
            dotRect.anchoredPosition = new Vector2(432f, -16f);
            dotRect.sizeDelta = new Vector2(12f, 12f);
            _machineDot = dot;

            // Neo o mep phai the, khong nam trong layout group nao. Nhan may la MiddleLeft va
            // HorizontalWrapMode.Overflow nen o nay khong xe dich mot chu nao.
            _machineIcon = UiFactory.Icon(card.transform, "MachineIcon", null, UiFactory.IconSizeCard);
            var machineIconRect = UiFactory.Rect(_machineIcon.gameObject);
            machineIconRect.anchorMin = new Vector2(0f, 1f);
            machineIconRect.anchorMax = new Vector2(0f, 1f);
            machineIconRect.pivot = new Vector2(0f, 1f);
            machineIconRect.anchoredPosition = new Vector2(UiFactory.EdgeMargin, -18f);
            machineIconRect.sizeDelta = new Vector2(UiFactory.IconSizeCard, UiFactory.IconSizeCard);

            _machineLabel = UiFactory.Label(card.transform, "MachineLabel", "", UiFactory.FontSizeBody,
                                            TextAnchor.MiddleLeft, GardenPalette.TextPrimary);
            _machineLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            // Hai dong chu 18 px voi lineSpacing 1,15 can 52 px; chua 50 px thi dong duoi bi cat.
            UiFactory.Stretch(UiFactory.Rect(_machineLabel.gameObject), new Vector2(0f, 1f), new Vector2(1f, 1f),
                              new Vector2(80f, -62f), new Vector2(-40f, -6f));

            Image machineFill;
            var track = UiFactory.ProgressBar(card.transform, "MachineBar", MachineView.StatusRunning, out machineFill);
            UiFactory.Stretch(UiFactory.Rect(track.gameObject), new Vector2(0f, 0f), new Vector2(1f, 0f),
                              new Vector2(80f, 14f), new Vector2(-UiFactory.EdgeMargin, 22f));
            _machineBarFill = machineFill;
            _machineBarTrack = track;
        }

        GameObject BuildSidePanel(string name, string title, out Transform body)
        {
            Transform unusedFooter;
            return BuildSidePanel(name, title, out body, out unusedFooter, 0f);
        }

        /// <summary>
        /// Panel ben phai. <paramref name="footerHeight"/> lon hon 0 thi chua duoi day mot khoi
        /// **khong cuon theo**: dung cho cho nao phai luon nhin thay duoc du danh sach dai bao
        /// nhieu — vi du o chi tiet cua mon dang chon trong tui do.
        /// </summary>
        GameObject BuildSidePanel(string name, string title, out Transform body,
                                  out Transform footer, float footerHeight)
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
                              new Vector2(UiFactory.EdgeMargin, UiFactory.EdgeMargin + footerHeight),
                              new Vector2(-26f, -72f));
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

            var rail = UiFactory.Panel(panel.transform, "ScrollRail", GardenPalette.PanelSoft, 3);
            UiFactory.Stretch(UiFactory.Rect(rail.gameObject), new Vector2(1f, 0f), Vector2.one,
                new Vector2(-13f, 20f), new Vector2(-7f, -76f));
            var thumb = UiFactory.Panel(rail.transform, "Thumb", GardenPalette.ButtonNormal, 3);
            UiFactory.Stretch(UiFactory.Rect(thumb.gameObject), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var scrollbar = rail.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = UiFactory.Rect(thumb.gameObject);
            scrollbar.targetGraphic = thumb;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            body = content.transform;

            // Chan panel: neo o day, ngoai vung cuon nen no khong bao gio troi khoi man hinh.
            if (footerHeight > 0f)
            {
                var footerNode = UiFactory.Node(panel.transform, "Footer");
                UiFactory.Stretch(UiFactory.Rect(footerNode), Vector2.zero, new Vector2(1f, 0f),
                                  new Vector2(UiFactory.EdgeMargin, UiFactory.EdgeMargin),
                                  new Vector2(-UiFactory.EdgeMargin, UiFactory.EdgeMargin + footerHeight));
                footer = footerNode.transform;
            }
            else
            {
                footer = null;
            }

            return panel.gameObject;
        }

        const float InventoryFooterHeight = 132f;
        const int InventoryColumns = 4;
        const float SlotWidth = 72f;
        const float SlotHeight = 86f;
        const float SlotSpacing = 10f;

        /// <summary>
        /// Tui do kieu o. Moi mat hang mot o cung co, xep theo tung cay: la tuoi truoc roi sau
        /// chang che bien cua chinh cay do.
        ///
        /// Truoc day cho nay la danh sach the ba tang, moi mon cao gan 150 px cho dung mot con
        /// so. Voi nam loai cay da phai cuon, ma day chuyen them ba muoi mat hang trung gian nua
        /// thi danh sach do dai gap bay lan — va truoc do chung khong hien o dau ca, tuc la cai
        /// bang ten "Kho" khong ke het nhung gi dang nam trong kho.
        /// </summary>
        void BuildInventoryGrid(Transform body)
        {
            foreach (var crop in _session.Catalog.Crops)
            {
                var header = UiFactory.Node(body, "Section_" + crop.Id);
                var headerLayout = UiFactory.HorizontalList(header, 8f, new RectOffset(0, 0, 0, 0));
                headerLayout.childAlignment = TextAnchor.MiddleLeft;
                headerLayout.childForceExpandWidth = false;
                headerLayout.childForceExpandHeight = false;
                header.AddComponent<LayoutElement>().minHeight = 30f;
                UiFactory.Icon(header.transform, "Icon", IconFor(crop.Id), UiFactory.IconSizeRow);
                UiFactory.Label(header.transform, "Name", crop.DisplayName, UiFactory.FontSizeRowTitle,
                                TextAnchor.MiddleLeft, GardenPalette.TextPrimary);

                var grid = UiFactory.Node(body, "Grid_" + crop.Id);
                UiFactory.Grid(grid, new Vector2(SlotWidth, SlotHeight), SlotSpacing, InventoryColumns);
                var gridFit = grid.AddComponent<ContentSizeFitter>();
                gridFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                BuildSlot(grid.transform, crop.Id, ProcessChain.SuffixFresh);
                foreach (var stage in _session.Catalog.Stages)
                    BuildSlot(grid.transform, crop.Id, stage.OutputSuffix);
            }

            var recipeTitle = UiFactory.Label(body, "RecipeTitle", "Công thức đang chọn", UiFactory.FontSizeMeta,
                                              TextAnchor.MiddleLeft, GardenPalette.TextMuted);
            var recipeTitleLayout = recipeTitle.gameObject.AddComponent<LayoutElement>();
            recipeTitleLayout.minHeight = 28f;
            recipeTitleLayout.preferredHeight = 28f;

            foreach (var recipe in _session.Catalog.Recipes)
            {
                string recipeId = recipe.Id;
                // Khong co ICO_Recipe*, va cay dau vao moi la thu can nhan ra nhanh o the cong thuc.
                var button = UiFactory.TextButton(body, "Recipe_" + recipeId, recipe.DisplayName,
                                                  delegate { Run(_session.SelectRecipe(recipeId)); },
                                                  UiFactory.ButtonStyle.Primary, IconFor(recipe.InputCropId));
                _recipeRows.Add(new RecipeRow { RecipeId = recipeId, Select = button });
            }
        }

        /// <summary>
        /// Bang xuong: mot khoi tho o tren, roi moi cong doan mot hang theo dung thu tu day
        /// chuyen. Thu tu la thong tin: nguoi choi doc tu tren xuong la thay la tra di duong nao.
        /// </summary>
        void BuildWorkshopRows(Transform body)
        {
            var note = UiFactory.Label(body, "Note",
                                       "Lá tươi đi qua sáu công đoạn rồi mới thành trà đóng gói. " +
                                       "Quầy trà trả cao hơn hẳn cho trà đã đóng gói.",
                                       UiFactory.FontSizeMeta, TextAnchor.UpperLeft, GardenPalette.TextMuted);
            note.gameObject.AddComponent<LayoutElement>().minHeight = 44f;

            var staff = UiFactory.Panel(body, "Staff", GardenPalette.PanelSoft, UiFactory.RadiusControl);
            UiFactory.VerticalList(staff.gameObject, 6f, new RectOffset(12, 12, 10, 10));
            staff.gameObject.AddComponent<LayoutElement>().minHeight = 120f;

            _workerSummary = UiFactory.Label(staff.transform, "Summary", "", UiFactory.FontSizeRowTitle,
                                             TextAnchor.MiddleLeft, GardenPalette.TextPrimary);
            _workerWarning = UiFactory.Label(staff.transform, "Warning", "", UiFactory.FontSizeMeta,
                                             TextAnchor.UpperLeft, GardenPalette.StateWarn);

            var staffButtons = UiFactory.Node(staff.transform, "StaffButtons");
            var staffLayout = UiFactory.HorizontalList(staffButtons, 8f, new RectOffset(0, 0, 0, 0));
            staffLayout.childForceExpandHeight = false;
            _hireButton = UiFactory.TextButton(staffButtons.transform, "Hire", "Thuê thợ",
                                               delegate { Run(_session.HireWorker()); },
                                               UiFactory.ButtonStyle.Primary,
                                               symbol: UiFactory.Symbols.GroupAdd);
            _fireButton = UiFactory.TextButton(staffButtons.transform, "Fire", "Cho nghỉ",
                                               delegate { Run(_session.FireWorker()); },
                                               UiFactory.ButtonStyle.Quiet,
                                               symbol: UiFactory.Symbols.PersonRemove);

            foreach (var stage in _session.Catalog.Stages)
            {
                string stageId = stage.Id;

                var card = UiFactory.Panel(body, "Row_" + stageId, GardenPalette.PanelSoft, UiFactory.RadiusControl);
                UiFactory.VerticalList(card.gameObject, 6f, new RectOffset(12, 12, 10, 10));
                card.gameObject.AddComponent<LayoutElement>().minHeight = 104f;

                var titleRow = UiFactory.Node(card.transform, "TitleRow");
                var titleLayout = UiFactory.HorizontalList(titleRow, 8f, new RectOffset(0, 0, 0, 0));
                titleLayout.childForceExpandWidth = false;
                titleLayout.childForceExpandHeight = false;
                titleRow.AddComponent<LayoutElement>().minHeight = 26f;

                var row = new StationRow { StageId = stageId, Card = card };

                row.Title = UiFactory.Label(titleRow.transform, "Title", stage.DisplayName,
                                            UiFactory.FontSizeRowTitle, TextAnchor.MiddleLeft,
                                            GardenPalette.TextPrimary);
                row.Title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

                row.Cost = UiFactory.CoinValue(titleRow.transform, "Cost", UiFactory.FontSizeRowTitle,
                                               UiFactory.IconSizeRow, TextAnchor.MiddleRight);
                var costWidth = row.Cost.transform.parent.gameObject.AddComponent<LayoutElement>();
                costWidth.preferredWidth = 96f;
                costWidth.minWidth = 96f;
                costWidth.flexibleWidth = 0f;

                row.Flow = UiFactory.Label(card.transform, "Flow", "", UiFactory.FontSizeMeta,
                                           TextAnchor.UpperLeft, GardenPalette.TextMuted);
                row.Status = UiFactory.Label(card.transform, "Status", "", UiFactory.FontSizeMeta,
                                             TextAnchor.UpperLeft, GardenPalette.TextMuted);

                row.Buy = UiFactory.TextButton(card.transform, "Buy", "Mua máy",
                                               delegate
                {
                    var result = _session.BuyStation(stageId);
                    Run(result);
                    if (result.Success)
                    {
                        var sfx = FindAnyObjectByType<SfxPlayer>();
                        if (sfx != null) sfx.PlayUnlock();
                    }
                },
                                               symbol: UiFactory.Symbols.ShoppingCart);
                _stationRows.Add(row);
            }
        }

        /// <summary>Ten mat hang o mot chang, vi du "Bạc hà héo". Dung o dong luu luong cua hang.</summary>
        string ItemName(string cropId, string suffix)
        {
            string cropName = _session.Catalog.Crop(cropId).DisplayName;
            if (string.IsNullOrEmpty(suffix)) return cropName + " tươi";
            switch (suffix)
            {
                case ProcessChain.SuffixWithered: return cropName + " héo";
                case ProcessChain.SuffixFixed: return cropName + " đã diệt men";
                case ProcessChain.SuffixRolled: return cropName + " đã vò";
                case ProcessChain.SuffixOxidised: return cropName + " đã lên men";
                case ProcessChain.SuffixDried: return cropName + " khô";
                case ProcessChain.SuffixPacked: return "Trà " + cropName.ToLowerInvariant() + " đóng gói";
                default: return cropName;
            }
        }

        /// <summary>Tong ton kho cua mot chang, cong het moi loai cay.</summary>
        long StockAt(string suffix)
        {
            long total = 0;
            foreach (var crop in _session.Catalog.Crops)
                total += _session.State.InventoryOf(ProcessChain.ItemId(crop.Id, suffix));
            return total;
        }

        void RefreshWorkshop()
        {
            if (_workshopPanel == null || !_workshopPanel.activeSelf) return;

            var state = _session.State;
            var balance = _session.Catalog.Balance;

            _workerSummary.text = "Thợ: " + state.HiredWorkers + " / " + balance.MaximumWorkers +
                                  " · lương " + balance.WorkerWageCoins + " xu mỗi thợ, " +
                                  (balance.PayrollPeriodMs / 1000) + " giây một kỳ";

            int owned = state.OwnedStationCount();
            string warning = null;
            if (state.StaffedWorkers < state.HiredWorkers)
                warning = "Kỳ này không đủ xu trả lương: " + (state.HiredWorkers - state.StaffedWorkers) +
                          " thợ đang nghỉ. Trả đủ là họ làm lại ngay.";
            else if (owned > state.HiredWorkers)
                warning = "Có " + owned + " máy nhưng chỉ " + state.HiredWorkers +
                          " thợ, nên " + (owned - state.HiredWorkers) + " máy nằm không.";
            _workerWarning.gameObject.SetActive(warning != null);
            if (warning != null) _workerWarning.text = warning;

            string hireReason;
            bool canHire = _session.CanHireWorker(out hireReason);
            UiFactory.SetButtonCaption(_hireButton, canHire
                ? "Thuê thợ · " + balance.WorkerHireCost + " xu"
                : hireReason);
            UiFactory.SetButtonState(_hireButton, canHire
                ? UiFactory.ButtonState.Normal : UiFactory.ButtonState.Disabled);
            UiFactory.SetButtonState(_fireButton, state.HiredWorkers > 0
                ? UiFactory.ButtonState.Normal : UiFactory.ButtonState.Disabled);

            for (int i = 0; i < _stationRows.Count; i++)
            {
                var row = _stationRows[i];
                var stage = _session.Catalog.Stage(row.StageId);
                var station = state.Station(row.StageId);

                row.Flow.text = stage.MachineName + " · " +
                                stage.InputCount + " " + StageName(stage.InputSuffix) + " → " +
                                stage.OutputCount + " " + StageName(stage.OutputSuffix) +
                                "   (kho " + StockAt(stage.InputSuffix) + " → " +
                                StockAt(stage.OutputSuffix) + ")";

                if (station == null || !station.Owned)
                {
                    row.Cost.text = stage.Cost.ToString();
                    row.Cost.transform.parent.gameObject.SetActive(true);
                    row.Buy.gameObject.SetActive(true);
                    string reason;
                    bool can = _session.CanBuyStation(row.StageId, out reason);
                    UiFactory.SetButtonState(row.Buy, can
                        ? UiFactory.ButtonState.Normal : UiFactory.ButtonState.Disabled);
                    UiFactory.SetButtonCaption(row.Buy, can ? "Mua máy" : reason);
                    row.Status.text = stage.Description;
                    row.Status.color = GardenPalette.TextMuted;
                    continue;
                }

                row.Cost.transform.parent.gameObject.SetActive(false);
                row.Buy.gameObject.SetActive(false);

                if (station.Running)
                {
                    long remaining = station.BatchFinishAtMs - state.SimulationTimeMs;
                    if (remaining < 0) remaining = 0;
                    row.Status.text = "Đang chạy " + ItemName(station.BatchCropId, stage.InputSuffix) +
                                      " · còn " + WholeTime(remaining);
                    row.Status.color = GardenPalette.StateOk;
                }
                else if (state.StaffedWorkers <= state.RunningStationCount())
                {
                    row.Status.text = "Không có thợ đứng máy.";
                    row.Status.color = GardenPalette.StateWarn;
                }
                else
                {
                    row.Status.text = "Chờ đủ " + stage.InputCount + " " + StageName(stage.InputSuffix) + ".";
                    row.Status.color = GardenPalette.TextMuted;
                }
            }
        }

        /// <summary>Ten chung cua mot chang, khong gan voi cay nao.</summary>
        static string StageName(string suffix)
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

        void BuildSlot(Transform parent, string cropId, string suffix)
        {
            string itemId = ProcessChain.ItemId(cropId, suffix);

            var background = UiFactory.Panel(parent, "Slot_" + itemId, GardenPalette.PanelSoft,
                                             UiFactory.RadiusControl);
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            button.onClick.AddListener(delegate { SelectInventoryItem(itemId); });

            var icon = UiFactory.Icon(background.transform, "Icon", IconFor(cropId), UiFactory.IconSizeCard);
            var iconRect = UiFactory.Rect(icon.gameObject);
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -6f);
            iconRect.sizeDelta = new Vector2(UiFactory.IconSizeCard, UiFactory.IconSizeCard);

            var tag = UiFactory.Label(background.transform, "Tag", ShortStageName(suffix),
                                      UiFactory.FontSizeMeta - 3, TextAnchor.MiddleCenter,
                                      GardenPalette.TextMuted);
            UiFactory.Stretch(UiFactory.Rect(tag.gameObject), new Vector2(0f, 0f), new Vector2(1f, 0f),
                              new Vector2(2f, 4f), new Vector2(-2f, 22f));

            // Vien so o goc, kieu tui do quen thuoc. Tat han khi trong kho khong co mon nay:
            // mot so 0 dam o moi o lam ca luoi nhin nhu dang co day hang.
            var pill = UiFactory.Panel(background.transform, "CountPill", GardenPalette.PanelScrim,
                                       UiFactory.RadiusDot);
            var pillRect = UiFactory.Rect(pill.gameObject);
            pillRect.anchorMin = new Vector2(1f, 1f);
            pillRect.anchorMax = new Vector2(1f, 1f);
            pillRect.pivot = new Vector2(1f, 1f);
            pillRect.anchoredPosition = new Vector2(-3f, -3f);
            pillRect.sizeDelta = new Vector2(34f, 20f);

            var count = UiFactory.Label(pill.transform, "Count", "", UiFactory.FontSizeMeta - 2,
                                        TextAnchor.MiddleCenter, GardenPalette.TextOnPrimary);
            UiFactory.Stretch(UiFactory.Rect(count.gameObject), Vector2.zero, Vector2.one,
                              new Vector2(2f, 0f), new Vector2(-2f, 0f));
            count.horizontalOverflow = HorizontalWrapMode.Overflow;

            _slots.Add(new InventorySlot
            {
                ItemId = itemId,
                CropId = cropId,
                Suffix = suffix,
                Button = button,
                Background = background,
                Icon = icon,
                Count = count,
                CountPill = pill
            });
        }

        /// <summary>
        /// O chi tiet duoi day panel. Chi mot bo nut ban cho ca tui do, thay vi mot bo tren moi
        /// mon: "Ban het" la thao tac khong hoan tac duoc, cang it cho de bam nham cang tot.
        /// </summary>
        void BuildInventoryDetail(Transform footer)
        {
            if (footer == null) return;

            var card = UiFactory.Panel(footer, "Detail", GardenPalette.PanelSoft, UiFactory.RadiusControl);
            UiFactory.Stretch(UiFactory.Rect(card.gameObject), Vector2.zero, Vector2.one,
                              Vector2.zero, Vector2.zero);
            UiFactory.VerticalList(card.gameObject, 4f, new RectOffset(12, 12, 8, 8));

            var titleRow = UiFactory.Node(card.transform, "TitleRow");
            var titleLayout = UiFactory.HorizontalList(titleRow, 8f, new RectOffset(0, 0, 0, 0));
            titleLayout.childAlignment = TextAnchor.MiddleLeft;
            titleLayout.childForceExpandWidth = false;
            titleLayout.childForceExpandHeight = false;
            titleRow.AddComponent<LayoutElement>().minHeight = 34f;

            _detailIcon = UiFactory.Icon(titleRow.transform, "Icon", null, UiFactory.IconSizeRow);
            _detailName = UiFactory.Label(titleRow.transform, "Name", "", UiFactory.FontSizeRowTitle,
                                          TextAnchor.MiddleLeft, GardenPalette.TextPrimary);
            _detailName.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            _detailMeta = UiFactory.Label(card.transform, "Meta", "", UiFactory.FontSizeMeta,
                                          TextAnchor.UpperLeft, GardenPalette.TextMuted);
            _detailMeta.gameObject.AddComponent<LayoutElement>().minHeight = 22f;

            var buttons = UiFactory.Node(card.transform, "Buttons");
            UiFactory.HorizontalList(buttons, 8f, new RectOffset(0, 0, 0, 0));
            buttons.AddComponent<LayoutElement>().minHeight = UiFactory.ButtonHeight;

            _sellOne = UiFactory.TextButton(buttons.transform, "SellOne", "Bán 1",
                                            delegate { SellSelected(false); });
            // "Ban het" nhe hon "Ban 1" mot bac: no ban sach kho va khong co duong lui.
            _sellAll = UiFactory.TextButton(buttons.transform, "SellAll", "Bán hết",
                                            delegate { SellSelected(true); },
                                            UiFactory.ButtonStyle.Quiet);
        }

        void SelectInventoryItem(string itemId)
        {
            _selectedItemId = itemId;
            RefreshInventoryPanel(_session.State, _session.Catalog);
        }

        void SellSelected(bool all)
        {
            if (_selectedItemId == null) return;
            var slot = _slots.Find(candidate => candidate.ItemId == _selectedItemId);
            if (slot == null || !string.IsNullOrEmpty(slot.Suffix)) return;
            Run(all ? _session.SellAllRaw(slot.CropId) : _session.SellRaw(slot.CropId, 1));
        }

        /// <summary>Ten ngan dat duoi o. Phai vua mot dong 66 px nen khong dung duoc ten day du.</summary>
        static string ShortStageName(string suffix)
        {
            switch (suffix)
            {
                case ProcessChain.SuffixWithered: return "héo";
                case ProcessChain.SuffixFixed: return "diệt men";
                case ProcessChain.SuffixRolled: return "vò";
                case ProcessChain.SuffixOxidised: return "lên men";
                case ProcessChain.SuffixDried: return "khô";
                case ProcessChain.SuffixPacked: return "đóng gói";
                default: return "tươi";
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
                string upgradeIconId = upgrade.Kind == UpgradeKind.UnlockCrop ? upgrade.TargetId
                    : upgrade.Kind == UpgradeKind.UnlockRobot ? GardenSkin.IconHelper
                    : upgrade.Kind == UpgradeKind.ExpandPlots ? GardenSkin.IconPlot
                    : upgrade.Kind == UpgradeKind.GrowthSpeed ? GardenSkin.IconSeedling : GardenSkin.IconStation;
                titleLayout.childForceExpandHeight = false;
                UiFactory.Icon(titleRow.transform, "Icon", IconFor(upgradeIconId), UiFactory.IconSizeRow);

                row.Title = UiFactory.Label(titleRow.transform, "Title", upgrade.DisplayName,
                                            UiFactory.FontSizeRowTitle, TextAnchor.MiddleLeft,
                                            GardenPalette.TextPrimary);
                var titleFlex = row.Title.gameObject.AddComponent<LayoutElement>();
                titleFlex.flexibleWidth = 1f;

                row.Cost = UiFactory.CoinValue(titleRow.transform, "Cost", UiFactory.FontSizeRowTitle,
                                               UiFactory.IconSizeRow, TextAnchor.MiddleRight);
                // Icon 32 px + khe 6 px + so bon chu so. Rong hon nua thi tieu de bi xuong dong.
                var costWidth = row.Cost.transform.parent.gameObject.AddComponent<LayoutElement>();
                costWidth.preferredWidth = 96f;
                costWidth.minWidth = 96f;
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
                },
                                                   symbol: UiFactory.Symbols.ShoppingCart);
                _upgradeRows.Add(row);
            }
        }

        void BuildDecorationRows(Transform body)
        {
            var note = UiFactory.Label(body, "Note",
                                       "Chọn một món rồi đặt quanh vườn. Gỡ đồ để nhận lại đủ xu.",
                                       UiFactory.FontSizeMeta, TextAnchor.UpperLeft, GardenPalette.TextMuted);
            var noteLayout = note.gameObject.AddComponent<LayoutElement>();
            noteLayout.minHeight = 44f;

            foreach (var decoration in _session.Catalog.Decorations)
            {
                string decorationId = decoration.Id;

                var rowGo = UiFactory.Panel(body, "Row_" + decorationId, GardenPalette.PanelSoft,
                    UiFactory.RadiusControl).gameObject;
                var rowLayout = rowGo.AddComponent<LayoutElement>();
                rowLayout.minHeight = 116f;
                UiFactory.VerticalList(rowGo, 8f, new RectOffset(12, 12, 12, 12));

                // Icon tinh, khong doi theo trang thai, nen khong can giu tham chieu.
                var titleRow = UiFactory.Node(rowGo.transform, "TitleRow");
                var titleLayout = UiFactory.HorizontalList(titleRow, 8f, new RectOffset(0, 0, 0, 0));
                titleLayout.childAlignment = TextAnchor.UpperLeft;
                titleLayout.childForceExpandWidth = false;
                titleLayout.childForceExpandHeight = false;

                UiFactory.Icon(titleRow.transform, "Icon", IconFor(decorationId), UiFactory.IconSizePreview);
                var label = UiFactory.Label(titleRow.transform, "Label", decoration.DisplayName,
                                            UiFactory.FontSizeMeta, TextAnchor.UpperLeft,
                                            GardenPalette.TextPrimary);
                // Van la minHeight tren chinh cai nhan: nhan nay chua ba dong chu tieng Viet.
                var labelLayout = label.gameObject.AddComponent<LayoutElement>();
                labelLayout.minHeight = 56f;
                labelLayout.flexibleWidth = 1f;

                var cost = UiFactory.CoinValue(titleRow.transform, "Cost", UiFactory.FontSizeMeta,
                                               UiFactory.IconSizeRow, TextAnchor.UpperRight);
                cost.text = decoration.Cost.ToString();

                var choose = UiFactory.TextButton(rowGo.transform, "Choose", "Chọn",
                                                  delegate { BeginPlacing(decorationId); },
                                                   symbol: UiFactory.Symbols.Add);
                _decorationRows.Add(new DecorationRow
                {
                    DecorationId = decorationId, Label = label, Choose = choose
                });
            }

            _removeModeButton = UiFactory.TextButton(body, "RemoveMode", "Gỡ đồ đã đặt",
                                                     delegate { ToggleRemoveMode(); },
                                                     UiFactory.ButtonStyle.Quiet,
                                                   symbol: UiFactory.Symbols.Delete);
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

                // Gia da nam o nhan rieng ben phai kem icon tien, nen dong nay chi con ten va mo ta.
                string text = decoration.DisplayName + "\n" + decoration.Description;
                if (isPlacing) text += "\nĐang đặt — bấm vào vườn, chuột phải để thoát.";
                else if (!affordable) text += "\nThiếu " + (decoration.Cost - state.Coins) + " xu.";
                row.Label.text = text;

                UiFactory.SetButtonCaption(row.Choose, isPlacing ? "Đang đặt…" : "Đặt vào vườn");
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
                            "Tùy chỉnh âm thanh, quản lý tiến độ và xuất dữ liệu chơi thử.",
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
            },
                                                   symbol: UiFactory.Symbols.VolumeUp);

            UiFactory.TextButton(body, "Export", "Xuất dữ liệu test", delegate
            {
                var bootstrap = GetComponentInParent<GameBootstrap>();
                if (bootstrap == null) bootstrap = FindAnyObjectByType<GameBootstrap>();
                if (bootstrap != null) ShowToast(bootstrap.ExportTestData());
            },
                                                   symbol: UiFactory.Symbols.Download);

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
                                                   UiFactory.ButtonStyle.Quiet,
                                                   symbol: UiFactory.Symbols.RestartAlt);
            resetButton.transform.SetSiblingIndex(resetConfirm.transform.GetSiblingIndex());

            // Nut giu lai dung truoc, nut xoa mang mau canh bao rieng.
            UiFactory.TextButton(resetButtons.transform, "ResetNo", "Giữ lại",
                                 delegate { resetConfirm.SetActive(false); },
                                                   symbol: UiFactory.Symbols.Undo);
            var resetYes = UiFactory.TextButton(resetButtons.transform, "ResetYes", "Xóa tiến độ", delegate
            {
                var bootstrap = FindAnyObjectByType<GameBootstrap>();
                if (bootstrap != null) bootstrap.ResetProgress();
                resetConfirm.SetActive(false);
                ShowToast("Đã tạo vườn mới.");
            },
                                                   symbol: UiFactory.Symbols.DeleteForever);
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
                }, UiFactory.ButtonStyle.Quiet,
                                                   symbol: UiFactory.Symbols.FastForward);
                UiFactory.TextButton(body, "Skip60", "Tua 60 phút", delegate
                {
                    _session.DebugAdvance(3600000);
                    ShowToast("Đã tua 60 phút mô phỏng.");
                }, UiFactory.ButtonStyle.Quiet,
                                                   symbol: UiFactory.Symbols.FastForward);
            }

            UiFactory.TextButton(body, "Quit", "Thoát game", delegate
            {
                var bootstrap = FindAnyObjectByType<GameBootstrap>();
                if (bootstrap != null) bootstrap.QuitGame();
            }, UiFactory.ButtonStyle.Quiet,
                                                   symbol: UiFactory.Symbols.Logout);
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
                }, UiFactory.ButtonStyle.Primary, IconFor(cropId));
                _cropChoiceRows.Add(new CropChoiceRow { CropId = cropId, Choose = button });
            }

            UiFactory.TextButton(panel.transform, "ClosePopup", "Đóng", delegate { ClosePlotPopup(); },
                                 UiFactory.ButtonStyle.Quiet,
                                                   symbol: UiFactory.Symbols.Close);
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
            var shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.12f, 0.18f, 0.10f, 0.20f);
            shadow.effectDistance = new Vector2(0f, -4f);
            var fitter = card.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Icon dong hang voi tieu de chu khong nam rieng mot dong: the nay von da cao, them
            // mot tang nua chi day cai nut ra xa hon.
            var titleRow = UiFactory.Node(card, "TitleRow");
            var titleLayout = UiFactory.HorizontalList(titleRow, 10f, new RectOffset(0, 0, 0, 0));
            titleLayout.childAlignment = TextAnchor.MiddleLeft;
            titleLayout.childForceExpandWidth = false;
            titleLayout.childForceExpandHeight = false;
            titleRow.AddComponent<LayoutElement>().minHeight = 36f;
            UiFactory.Symbol(titleRow.transform, "Symbol", UiFactory.Symbols.Schedule,
                             UiFactory.IconSizeRow).color = GardenPalette.TextMuted;
            var title = UiFactory.Label(titleRow.transform, "Title", "Vườn vẫn chạy khi bạn vắng mặt",
                                        UiFactory.FontSizeTitle, TextAnchor.MiddleLeft,
                                        GardenPalette.TextPrimary, true);
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            _offlineText = UiFactory.Label(card, "Body", "", UiFactory.FontSizeBody,
                                           TextAnchor.UpperLeft, GardenPalette.TextPrimary);
            _offlineText.lineSpacing = 1.25f;

            _offlineMachineRow = UiFactory.Node(card, "MachineStatus");
            var machineLayout = UiFactory.HorizontalList(_offlineMachineRow, 8f, new RectOffset(0, 0, 0, 0));
            machineLayout.childAlignment = TextAnchor.MiddleLeft;
            machineLayout.childForceExpandWidth = false;
            machineLayout.childForceExpandHeight = false;
            UiFactory.Symbol(_offlineMachineRow.transform, "Symbol", UiFactory.Symbols.Factory, 24f)
                .color = GardenPalette.StateWarn;
            _offlineMachineText = UiFactory.Label(_offlineMachineRow.transform, "Text", "",
                UiFactory.FontSizeBody, TextAnchor.MiddleLeft, GardenPalette.TextPrimary);
            _offlineMachineText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            _offlineRobotRow = UiFactory.Node(card, "RobotStatus");
            var robotLayout = UiFactory.HorizontalList(_offlineRobotRow, 8f, new RectOffset(0, 0, 0, 0));
            robotLayout.childAlignment = TextAnchor.MiddleLeft;
            robotLayout.childForceExpandWidth = false;
            robotLayout.childForceExpandHeight = false;
            UiFactory.Symbol(_offlineRobotRow.transform, "Symbol", UiFactory.Symbols.PersonRemove, 24f)
                .color = GardenPalette.StateWarn;
            _offlineRobotText = UiFactory.Label(_offlineRobotRow.transform, "Text", "",
                UiFactory.FontSizeBody, TextAnchor.MiddleLeft, GardenPalette.TextPrimary);
            _offlineRobotText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            // Tran offline la mot luat, khong phai ket qua cua lan vang mat nay. De mo va nho hon
            // de no khong tranh cho voi may dong that su noi ve khu vuon.
            _offlineCapText = UiFactory.Label(card, "Cap", "", UiFactory.FontSizeMeta,
                                              TextAnchor.UpperLeft, GardenPalette.TextMuted);

            // Nut vua bang chu va nam giua: the nay chi co mot loi thoat, khong can mot thanh
            // chay het be ngang moi tim thay no.
            var actionRow = UiFactory.Node(card, "ActionRow");
            var actionLayout = UiFactory.HorizontalList(actionRow, 0f, new RectOffset(0, 0, 4, 0));
            actionLayout.childAlignment = TextAnchor.MiddleCenter;
            actionLayout.childForceExpandWidth = false;
            actionLayout.childForceExpandHeight = false;
            var continueButton = UiFactory.TextButton(actionRow.transform, "Continue", "Tiếp tục", delegate
            {
                _session.AcknowledgeOfflineSummary();
                container.SetActive(false);
            },
                                                      symbol: UiFactory.Symbols.PlayArrow);
            var continueStyle = continueButton.GetComponent<UiButtonStyle>();
            if (continueStyle != null && continueStyle.Symbol != null)
            {
                continueStyle.Symbol.fontSize = 18;
                var symbolLayout = continueStyle.Symbol.GetComponent<LayoutElement>();
                if (symbolLayout != null)
                {
                    symbolLayout.minWidth = 24f;
                    symbolLayout.preferredWidth = 24f;
                    symbolLayout.preferredHeight = 24f;
                }
            }
            UiFactory.HugContent(continueButton);

            return container;
        }

        GameObject BuildBlockedPanel()
        {
            Transform card;
            var container = BuildModal("BlockedModal", out card, new Vector2(600f, 360f));

            var accent = UiFactory.Panel(card, "Accent", GardenPalette.ButtonDanger);
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
                              new Vector2(24f, 82f), new Vector2(-24f, -78f));

            var actions = UiFactory.Node(card, "Actions");
            UiFactory.Stretch(UiFactory.Rect(actions), Vector2.zero, new Vector2(1f, 0f),
                new Vector2(24f, 20f), new Vector2(-24f, 64f));
            UiFactory.HorizontalList(actions, 12f, new RectOffset(0, 0, 0, 0));
            UiFactory.TextButton(actions.transform, "Settings", "Mở cài đặt", delegate
            {
                container.SetActive(false);
                TogglePanel(_settingsPanel);
            },
                                                   symbol: UiFactory.Symbols.Settings);
            UiFactory.TextButton(actions.transform, "Quit", "Thoát game", delegate
            {
                var bootstrap = FindAnyObjectByType<GameBootstrap>();
                if (bootstrap != null) bootstrap.QuitGame();
            }, UiFactory.ButtonStyle.Quiet);

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

            _coinsLabel.text = state.Coins.ToString();
            _goalLabel.text = GoalText(TutorialGuide.CurrentStep(state, catalog), state, catalog);
            RefreshWorkshop();

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
                _machineLabel.text = "Đang pha " + runningName +
                                     "\nCòn " + Seconds(remaining) + " giây · " +
                                     state.Machine.BatchOutputCoins + " xu / mẻ";
                _machineDot.color = MachineView.StatusRunning;
                _machineBarTrack.color = GardenPalette.TrackEmpty;
                _machineBarFill.color = MachineView.StatusRunning;
                UiFactory.SetIcon(_machineIcon, MachineIcon(running != null ? running : selected));
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
                    _machineLabel.text = selected.DisplayName + " · Đang chờ\nCần thêm " + missingAmount + " " +
                                         catalog.Crop(missingCropId).DisplayName;
                    status = MachineView.StatusWaiting;
                }
                else
                {
                    _machineLabel.text = selected.DisplayName + "\nMáy sẵn sàng";
                    status = MachineView.StatusIdle;
                }
            }

            _machineDot.color = status;
            UiFactory.SetIcon(_machineIcon, MachineIcon(selected));
            // Ranh luon toi: to mau ranh khi chua co me nao chay lam thanh rong trong nhu da day.
            _machineBarTrack.color = GardenPalette.TrackEmpty;
            _machineBarFill.color = status;
            UiFactory.SetProgress(_machineBarFill, 0f);
        }

        /// <summary>Icon may: cay dang duoc pha, khong co thi den icon quan tra, het thi de trong.</summary>
        Sprite MachineIcon(RecipeDefinition recipe)
        {
            if (recipe != null)
            {
                var cropIcon = IconFor(recipe.InputCropId);
                if (cropIcon != null) return cropIcon;
            }
            return IconFor(GardenSkin.IconStation);
        }

        void RefreshInventoryPanel(GameState state, ContentCatalog catalog)
        {
            RecipeDefinition selected;
            catalog.TryGetRecipe(state.Machine.SelectedRecipeId, out selected);
            string usedCropId = selected != null ? selected.InputCropId : null;

            // Chua chon gi thi chon san mon dau tien DANG CO hang: mo tui do ra ma o chi tiet noi
            // ve mot mon bang 0 thi lan bam dau tien nao cung la de sua lai lua chon do.
            if (_selectedItemId == null && _slots.Count > 0)
            {
                var first = _slots.Find(candidate => state.InventoryOf(candidate.ItemId) > 0);
                _selectedItemId = (first ?? _slots[0]).ItemId;
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                long amount = state.InventoryOf(slot.ItemId);
                bool unlocked = state.UnlockedCropIds.Contains(slot.CropId);
                bool empty = amount <= 0;
                bool isChosen = slot.ItemId == _selectedItemId;

                slot.Background.color = isChosen ? GardenPalette.ButtonActive
                    : empty ? GardenPalette.PanelSoftMuted : GardenPalette.PanelSoft;

                // O rong va cay chua mo khoa deu lam mo di, nhung khong an: tui do phai cho thay
                // ca nhung o dang trong thi nguoi choi moi nho duoc cho cua tung mon.
                slot.Icon.color = !unlocked || empty ? GardenPalette.IconMuted : GardenPalette.IconTint;

                slot.CountPill.gameObject.SetActive(!empty);
                if (!empty) slot.Count.text = amount > 9999 ? "9k+" : amount.ToString();
            }

            var chosen = _slots.Find(candidate => candidate.ItemId == _selectedItemId);
            if (chosen != null)
            {
                long amount = state.InventoryOf(chosen.ItemId);
                var crop = catalog.Crop(chosen.CropId);
                bool raw = string.IsNullOrEmpty(chosen.Suffix);
                bool unlocked = state.UnlockedCropIds.Contains(chosen.CropId);

                UiFactory.SetIcon(_detailIcon, IconFor(chosen.CropId));
                _detailName.text = ItemName(chosen.CropId, chosen.Suffix) + ": " + amount;

                string meta;
                if (!raw)
                    meta = "Hàng trên dây chuyền, không bán lẻ được.";
                else if (!unlocked)
                    meta = crop.RawSellPrice + " xu/đơn vị  ·  chưa mở khóa";
                else if (chosen.CropId == usedCropId)
                    meta = crop.RawSellPrice + " xu/đơn vị  ·  máy đang dùng";
                else
                    meta = crop.RawSellPrice + " xu/đơn vị";
                _detailMeta.text = meta;
                _detailMeta.color = raw && chosen.CropId == usedCropId
                    ? GardenPalette.StateOk : GardenPalette.TextMuted;

                UiFactory.SetInteractable(_sellOne, raw && amount >= 1);
                UiFactory.SetInteractable(_sellAll, raw && amount >= 1);
                UiFactory.SetButtonCaption(_sellAll,
                                           raw && amount >= 1
                                               ? "Bán hết · " + amount * crop.RawSellPrice + " xu"
                                               : "Bán hết");
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
                // Icon tien da noi ro day la xu, nen cot gia chi can con so.
                row.Cost.text = upgrade.Cost.ToString();
                row.Cost.color = bought ? GardenPalette.TextMuted : GardenPalette.TextCoin;
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
            else if (panelName == "workshop") TogglePanel(_workshopPanel);
            else if (panelName == "plot") OpenPlotPopup(0);
            else if (panelName == "offline")
            {
                var preview = new OfflineSummary { ElapsedMs = 180000, CoinsGained = 0, MachineWaiting = true };
                preview.ItemsGained.Add(new InventoryDelta { CropId = DefaultContent.CropLemongrass, Amount = 72 });
                ShowOfflineReport(preview, false);
            }
        }

        void TogglePanel(GameObject panel)
        {
            bool willOpen = !panel.activeSelf;
            _inventoryPanel.SetActive(false);
            _upgradePanel.SetActive(false);
            _settingsPanel.SetActive(false);
            _decoratePanel.SetActive(false);
            _workshopPanel.SetActive(false);
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

        /// <summary>
        /// Bao cao vang mat. Ba con so cua lan vang mat nay — thoi gian, xu, kho — di chung mot
        /// dong: chung tra loi cung mot cau hoi "trong luc toi di vang thi duoc gi", tach ra ba
        /// dong chi bat nguoi doc doc ba lan. Hai dong sau chi hien khi that su co chuyen.
        /// </summary>
        void ShowOfflineReport(OfflineSummary summary)
        {
            ShowOfflineReport(summary, !_session.State.RobotUnlocked);
        }

        void ShowOfflineReport(OfflineSummary summary, bool showRobotWarning)
        {
            var catalog = _session.Catalog;
            string text = "Vắng " + WholeTime(summary.ElapsedMs) + " · " +
                          (summary.CoinsGained > 0 ? "+" : "") + summary.CoinsGained + " xu · ";

            if (summary.ItemsGained.Count == 0)
            {
                text += "kho không đổi.";
            }
            else
            {
                text += "kho ";
                for (int i = 0; i < summary.ItemsGained.Count; i++)
                {
                    var item = summary.ItemsGained[i];
                    if (i > 0) text += ", ";
                    text += catalog.Crop(item.CropId).DisplayName + " " +
                            (item.Amount > 0 ? "+" : "") + item.Amount;
                }
                text += ".";
            }

            _offlineMachineText.text = "Máy đang chờ nguyên liệu.";
            _offlineMachineRow.SetActive(summary.MachineWaiting);
            _offlineRobotText.text = "Chưa có robot — cây chín đang chờ bạn thu.";
            _offlineRobotRow.SetActive(showRobotWarning);

            _offlineCapText.text = "Tối đa " + (_session.Catalog.Balance.OfflineCapMs / 3600000) +
                                   " giờ cho một lần vắng mặt.";
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
