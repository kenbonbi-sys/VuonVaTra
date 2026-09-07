using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VuonNho.Core;

namespace VuonNho.Views
{
    /// <summary>Floating farm HUD. Every counter comes from the existing simulation.</summary>
    public sealed partial class GameHud
    {
        static readonly Color HudRim = new Color32(255, 250, 223, 255);
        static readonly Color HudFace = new Color32(242, 237, 212, 255);
        static readonly Color HudShadow = new Color32(99, 104, 63, 100);
        static readonly Color HudGreen = new Color32(113, 174, 70, 255);
        static readonly Color HudBrown = new Color32(135, 105, 71, 255);
        Text _actionCaption, _bookGoal;
        Image _seasonProgress;
        GameObject _journalPanel;
        Button _journalButton;
        Image _debtChip;
        Text _debtCaption, _debtValue;
        int _actionPlot = -1;

        /// <summary>Cot nut ben trai bat dau ngay duoi the ho so, va moi nut la mot o vuong.</summary>
        const float RailTop = 124f;
        const float RailButtonSize = 68f;
        const float RailGap = 10f;
        const float RailTooltipGap = 10f;

        /// <summary>Be ngang the xu: icon 62 px roi mot o so bon chu so.</summary>
        const float StatusPillWidth = 210f;

        static void Place(RectTransform rect, Vector2 anchor, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        static Image FarmSurface(Transform parent, string name, Vector2 anchor,
                                 float x, float y, float width, float height, int radius = 16)
        {
            var rim = UiFactory.Panel(parent, name, HudRim, radius);
            Place(rim.rectTransform, anchor, x, y, width, height);
            var shadow = rim.gameObject.AddComponent<Shadow>();
            shadow.effectColor = HudShadow;
            shadow.effectDistance = new Vector2(0, -5);
            shadow.useGraphicAlpha = true;
            var face = UiFactory.Panel(rim.transform, "Inset", HudFace, Math.Max(2, radius - 4));
            UiFactory.Stretch(face.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(4, 5), new Vector2(-4, -4));
            face.raycastTarget = false;
            return rim;
        }

        static FarmHudIcon FarmIcon(Transform parent, FarmHudIcon.Kind kind,
                                    float x, float y, float size)
        {
            var node = UiFactory.Node(parent, kind + "Icon");
            Place(UiFactory.Rect(node), new Vector2(0, 1), x, y, size, size);
            var icon = node.AddComponent<FarmHudIcon>();
            icon.Icon = kind;
            icon.raycastTarget = false;
            return icon;
        }

        static Text FarmText(Transform parent, string name, string value, int size,
                             float x, float y, float width, float height, bool bold = false)
        {
            var label = UiFactory.Label(parent, name, value, size,
                TextAnchor.MiddleLeft, GardenPalette.TextPrimary, bold);
            Place(label.rectTransform, new Vector2(0, 1), x, y, width, height);
            return label;
        }

        static Button FarmButton(Transform parent, string name, string caption,
                                 FarmHudIcon.Kind icon, Vector2 anchor, float x, float y,
                                 UnityAction onClick, float width = 62, float height = 64)
        {
            var rim = FarmSurface(parent, name, anchor, x, y, width, height, 15);
            var button = rim.gameObject.AddComponent<Button>();
            var inset = rim.transform.Find("Inset").GetComponent<Image>();
            button.targetGraphic = inset;
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 0.91f);
            colors.pressedColor = new Color(0.78f, 0.82f, 0.65f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.12f;
            button.colors = colors;
            button.onClick.AddListener(onClick);
            FarmIcon(rim.transform, icon, (width - 42) / 2, -3, 42);
            var label = FarmText(rim.transform, "Label", caption, 12,
                3, -42, width - 6, 17, true);
            label.alignment = TextAnchor.MiddleCenter;
            var style = rim.gameObject.AddComponent<UiButtonStyle>();
            style.Background = inset;
            style.Caption = label;
            style.BaseColor = HudFace;
            style.BaseTextColor = GardenPalette.TextPrimary;
            return button;
        }

        void BuildTopBar(Transform root)
        {
            // No full-screen Graphic: the space between the floating controls stays clickable.
            var hud = UiFactory.Node(root, "FarmHud");
            UiFactory.Stretch(UiFactory.Rect(hud), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var tl = new Vector2(0, 1);
            var tr = Vector2.one;
            var bl = Vector2.zero;
            var br = new Vector2(1, 0);

            var profile = FarmSurface(hud.transform, "Profile", tl, 22, -24, 290, 76, 18);
            var portrait = FarmSurface(profile.transform, "Portrait", tl, -5, 8, 88, 88, 44);
            FarmIcon(portrait.transform, FarmHudIcon.Kind.Avatar, 6, -5, 76);
            FarmText(profile.transform, "Name", "Vườn và Trà", 21, 94, -8, 186, 29, true);
            _seasonLabel = FarmText(profile.transform, "Season", "", 14, 96, -35, 178, 22);
            FarmIcon(profile.transform, FarmHudIcon.Kind.Leaf, 89, -55, 18);
            var seasonTrack = UiFactory.ProgressBar(profile.transform, "SeasonProgress", HudGreen,
                out _seasonProgress);
            Place(seasonTrack.rectTransform, tl, 113, -60, 158, 7);

            BuildLeftRail(hud.transform);
            BuildStatusPill(hud.transform, tr);

            // Ba nut ben phai neo vao **giua chieu doc** chu khong vao day man hinh: cot trai da
            // chiem het mep trai tu tren xuong, de ca hai cung tut xuong day thi hai cum dinh
            // nhau o goc duoi. Giua phai la cho duy nhat con trong ma tay van voi tan.
            var rightMiddle = new Vector2(1, .5f);
            _workshopButton = FarmButton(hud.transform, "WorkshopButton", "Xưởng", FarmHudIcon.Kind.Factory,
                rightMiddle, -22, 100, delegate { TogglePanel(_workshopPanel); }, 74, 78);
            FarmButton(hud.transform, "WorkersButton", "Nhân sự", FarmHudIcon.Kind.Workers,
                rightMiddle, -22, 12, ShowWorkshopPanel, 74, 78);
            _settingsButton = FarmButton(hud.transform, "SettingsButton", "Cài đặt", FarmHudIcon.Kind.Settings,
                rightMiddle, -22, -76, delegate { TogglePanel(_settingsPanel); }, 74, 78);

            var dock = FarmSurface(hud.transform, "ActionDock", new Vector2(.5f, 0), 0, 22, 292, 78, 20);
            // 74 px chu khong 64: o 64 px thi o chu chi con 58 px, va "Xem xưởng" o co chu 12
            // xuong hai dong roi tran ra khoi o — co mot muc kiem trong bo QA giu dieu do. Dock
            // rong 292 px voi nut giua 108 px nen 74 + 108 + 74 van con thua le hai ben.
            FarmButton(dock.transform, "ResetCameraButton", "Về giữa", FarmHudIcon.Kind.Reset,
                bl, 8, 6, ResetFarmView, 74, 63);
            FarmButton(dock.transform, "FocusWorkshopButton", "Xem xưởng", FarmHudIcon.Kind.Play,
                br, -8, 6, delegate
                {
                    var boot = FindAnyObjectByType<GameBootstrap>();
                    if (boot != null && boot.Rig != null)
                        boot.Rig.FocusGround(new Vector3(8.5f, 0, .7f), 5.7f);
                }, 74, 63);
            var action = FarmSurface(dock.transform, "FarmActionButton", new Vector2(.5f, 0), 0, -1, 108, 108, 54);
            var green = action.transform.Find("Inset").GetComponent<Image>();
            green.color = new Color32(80, 140, 52, 255);
            var centre = UiFactory.Panel(action.transform, "GreenFace", HudGreen, 45);
            UiFactory.Stretch(centre.rectTransform, Vector2.zero, Vector2.one, new Vector2(11, 12), new Vector2(-11, -10));
            centre.raycastTarget = false;
            var actionButton = action.gameObject.AddComponent<Button>();
            actionButton.targetGraphic = centre;
            actionButton.onClick.AddListener(DoFarmAction);
            FarmIcon(action.transform, FarmHudIcon.Kind.Leaf, 37, -16, 34);
            _actionCaption = FarmText(action.transform, "Label", "Gieo hạt", 17, 10, -54, 88, 34, true);
            _actionCaption.alignment = TextAnchor.MiddleCenter;
            _actionCaption.color = HudRim;
        }

        /// <summary>
        /// Cot nut doc o mep trai: chi icon, ten hien ra thanh mot the ben phai khi re chuot len.
        ///
        /// Dung VerticalLayoutGroup chu khong dat toa do cung cho tung nut, vi nut trang tri chi
        /// hien sau khi nguoi choi dung xong vuon o pha 1 — dat cung toa do thi truoc do cot se
        /// co mot cai lo o dau, con layout thi tu khep lai. Them nut moi ve sau cung khong phai
        /// tinh lai toa do cua nhung nut ben duoi.
        /// </summary>
        void BuildLeftRail(Transform hud)
        {
            var rail = UiFactory.Node(hud, "LeftRail");
            var railRect = UiFactory.Rect(rail);
            railRect.anchorMin = railRect.anchorMax = railRect.pivot = new Vector2(0, 1);
            railRect.anchoredPosition = new Vector2(22, -RailTop);
            railRect.sizeDelta = new Vector2(RailButtonSize, 0);
            var list = UiFactory.VerticalList(rail, RailGap, new RectOffset(0, 0, 0, 0));
            // childControl bat, childForceExpand tat: layout lay kich thuoc tu LayoutElement cua
            // tung nut (68 x 68) chu khong keo gian chung. Tat childControl thi layout doc
            // sizeDelta cua nut, ma nut vua tao ra chua co kich thuoc nao — no se ra o 100 x 100
            // mac dinh cua RectTransform, tuc mot cot hinh chu nhat cao thay vi nam o vuong.
            list.childForceExpandWidth = false;
            list.childForceExpandHeight = false;
            list.childControlWidth = true;
            list.childControlHeight = true;
            var fitter = rail.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _decorateButton = RailButton(rail.transform, "DecorateButton", "Trang trí",
                FarmHudIcon.Kind.Sprout, delegate { TogglePanel(_decoratePanel); });
            _upgradeButton = RailButton(rail.transform, "UpgradeButton", "Nâng cấp",
                FarmHudIcon.Kind.Tools, delegate { TogglePanel(_upgradePanel); });
            _journalButton = RailButton(rail.transform, "JournalButton", "Sổ tay",
                FarmHudIcon.Kind.Book, delegate { TogglePanel(_journalPanel); });
            RailButton(rail.transform, "HomeButton", "Khu vườn", FarmHudIcon.Kind.House, ResetFarmView);
            _inventoryButton = RailButton(rail.transform, "InventoryButton", "Kho",
                FarmHudIcon.Kind.Crate, delegate { TogglePanel(_inventoryPanel); });

            // Thẻ nợ nằm dưới cùng cột, ngoài layout: một khoản vay là nghĩa vụ có kỳ hạn nên nó
            // phải nhìn thấy được mà không phải mở bảng nào, nhưng người chưa vay thì không cần
            // một ô trống nên nó ẩn hẳn chứ không nằm đó ở dạng rỗng.
            _debtChip = FarmSurface(hud, "DebtChip", new Vector2(0, 1), 22,
                -(RailTop + 5 * RailButtonSize + 5 * RailGap), 210, 66, 16);
            FarmIcon(_debtChip.transform, FarmHudIcon.Kind.Banknotes, 2, -2, 54);
            _debtCaption = FarmText(_debtChip.transform, "Caption", "Dư nợ", 13, 62, -6, 132, 22);
            _debtValue = FarmText(_debtChip.transform, "Value", "", 18, 62, -26, 132, 30, true);
            var debtOpen = _debtChip.gameObject.AddComponent<Button>();
            debtOpen.targetGraphic = _debtChip;
            debtOpen.onClick.AddListener(delegate { TogglePanel(_financePanel); });
            _debtChip.gameObject.SetActive(false);
        }

        /// <summary>Mot nut cua cot trai: o vuong chi co icon, kem the ten hien khi re chuot.</summary>
        Button RailButton(Transform rail, string name, string caption, FarmHudIcon.Kind icon,
                          UnityAction onClick)
        {
            var rim = UiFactory.Panel(rail, name, HudRim, 15);
            var size = rim.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = RailButtonSize;
            size.preferredHeight = RailButtonSize;
            size.minWidth = RailButtonSize;
            size.minHeight = RailButtonSize;
            var shadow = rim.gameObject.AddComponent<Shadow>();
            shadow.effectColor = HudShadow;
            shadow.effectDistance = new Vector2(0, -5);
            shadow.useGraphicAlpha = true;

            var inset = UiFactory.Panel(rim.transform, "Inset", HudFace, 11);
            UiFactory.Stretch(inset.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(4, 5), new Vector2(-4, -4));
            inset.raycastTarget = false;

            var button = rim.gameObject.AddComponent<Button>();
            button.targetGraphic = inset;
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 0.91f);
            colors.pressedColor = new Color(0.78f, 0.82f, 0.65f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.12f;
            button.colors = colors;
            button.onClick.AddListener(onClick);

            float glyph = RailButtonSize - 24f;
            FarmIcon(rim.transform, icon, (RailButtonSize - glyph) / 2f, -(RailButtonSize - glyph) / 2f, glyph);

            var style = rim.gameObject.AddComponent<UiButtonStyle>();
            style.Background = inset;
            style.BaseColor = HudFace;
            style.BaseTextColor = GardenPalette.TextPrimary;

            AttachRailTooltip(rim.transform, caption);
            return button;
        }

        /// <summary>
        /// The ten hien ben phai nut khi re chuot len.
        ///
        /// Khong ve chu ngay duoi icon nhu cac nut khac: nam cai nut xep doc, moi cai mang mot
        /// dong chu, la mot cot chu chay doc mep trai man hinh che mat khu vuon. The chi hien mot
        /// cai moi luc va chi khi nguoi choi dang hoi den no.
        ///
        /// The khong nhan raycast — no de len chinh cai nut vua duoc re chuot toi, va cuop mat
        /// cu bam do la loi de xay ra nhat o day.
        /// </summary>
        void AttachRailTooltip(Transform button, string caption)
        {
            var chip = UiFactory.Panel(button, "Tooltip", HudGreen, 10);
            Place(chip.rectTransform, new Vector2(1, .5f), RailTooltipGap, 0, 0, 34);
            chip.rectTransform.pivot = new Vector2(0, .5f);
            chip.raycastTarget = false;

            var label = UiFactory.Label(chip.transform, "Text", caption, 15,
                TextAnchor.MiddleCenter, HudRim, true);
            UiFactory.Stretch(label.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(12, 0), new Vector2(-12, 0));
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Be ngang do theo chinh chu, khong go cung: "Trang trí" va "Kho" dai khac nhau, va
            // mot be ngang co dinh se hoac cat chu dai hoac de mot khoang trong sau chu ngan.
            var fitter = chip.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var layout = UiFactory.HorizontalList(chip.gameObject, 0, new RectOffset(12, 12, 0, 0));
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;

            chip.gameObject.SetActive(false);

            var trigger = button.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(delegate { chip.gameObject.SetActive(true); });
            trigger.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(delegate { chip.gameObject.SetActive(false); });
            trigger.triggers.Add(exit);
        }

        /// <summary>
        /// The xu o goc phai tren.
        ///
        /// Truoc day the nay con mang so tho va so hang trong kho. Bo ca hai vi chung **da co
        /// duong vao rieng**: nut "Nhan su" o mep phai va nut "Kho" o cot trai. Mot con so vua
        /// hien o goc man hinh vua co mot cai nut rieng la hai cho noi cung mot chuyen, va cai o
        /// goc man hinh thi khong bam duoc de lam gi sau hon.
        ///
        /// Xu o lai vi no khac han: no doi sau **moi** hanh dong — gieo, thu, mua, tra no — nen
        /// no la con so duy nhat can nhin thay lien tuc ma khong phai mo gi ca.
        /// </summary>
        void BuildStatusPill(Transform hud, Vector2 topRight)
        {
            var pill = FarmSurface(hud, "StatusPill", topRight, -22, -24, StatusPillWidth, 68, 18);
            FarmIcon(pill.transform, FarmHudIcon.Kind.Coin, 4, -3, 62);
            _coinsLabel = FarmText(pill.transform, "Value", "0", 24, 70, -17, 132, 36, true);
            _coinsLabel.alignment = TextAnchor.MiddleRight;
            _coinsLabel.resizeTextForBestFit = true;
            _coinsLabel.resizeTextMinSize = 13;
            _coinsLabel.resizeTextMaxSize = 24;
        }

        void BuildMachineCard(Transform root)
        {
            var card = FarmSurface(root, "MachineCard", new Vector2(.5f, 1), 0, -24, 342, 64, 16);
            _machineIcon = UiFactory.Icon(card.transform, "MachineIcon", null, 39);
            Place(_machineIcon.rectTransform, new Vector2(0, 1), 10, -8, 39, 39);
            _machineLabel = FarmText(card.transform, "MachineLabel", "", 14, 59, -5, 255, 44);
            _machineDot = UiFactory.Panel(card.transform, "MachineDot", MachineView.StatusIdle, 4);
            Place(_machineDot.rectTransform, Vector2.one, -11, -12, 8, 8);
            _machineDot.raycastTarget = false;
            _machineBarTrack = UiFactory.ProgressBar(card.transform, "MachineBar", HudGreen, out _machineBarFill);
            Place(_machineBarTrack.rectTransform, new Vector2(0, 1), 60, -53, 266, 5);
        }

        void BuildJournal()
        {
            Transform body;
            _journalPanel = BuildSidePanel("JournalPanel", "Sổ tay Vườn và Trà", out body);
            UiFactory.Label(body, "GoalTitle", "Việc tiếp theo", 20,
                TextAnchor.MiddleLeft, GardenPalette.TextPrimary, true);
            _goalLabel = UiFactory.Label(body, "Goal", "", 18,
                TextAnchor.UpperLeft, GardenPalette.TextPrimary);
            UiFactory.Label(body, "HowTo", "Chọn cây → gieo → thu hoạch → chế biến → bán trà → mở rộng khu vườn.",
                18, TextAnchor.UpperLeft, GardenPalette.TextMuted);
            UiFactory.Label(body, "SourceTitle", "Mô phỏng khởi nghiệp trà", 20,
                TextAnchor.MiddleLeft, GardenPalette.TextPrimary, true);
            // Nam muc nay truoc day la mot khoi chu liet ke. Gio moi muc la mot cai cua: so tay
            // la cho nguoi choi doc **va** la cho ho vao lam, khong phai mot ban muc luc.
            UiFactory.TextButton(body, "OpenFinance", "Vốn và nợ ngân hàng",
                delegate { TogglePanel(_financePanel); }, UiFactory.ButtonStyle.Quiet);
            UiFactory.TextButton(body, "OpenAgronomy", "Nông học và mùa vụ",
                delegate { TogglePanel(_agronomyPanel); }, UiFactory.ButtonStyle.Quiet);
            UiFactory.TextButton(body, "OpenCraft", "Căn lửa và chất lượng mẻ",
                delegate { TogglePanel(_craftPanel); }, UiFactory.ButtonStyle.Quiet);
            UiFactory.TextButton(body, "OpenLegal", "Pháp lý và xưởng một chiều",
                delegate { TogglePanel(_legalPanel); }, UiFactory.ButtonStyle.Quiet);
            UiFactory.TextButton(body, "OpenModel", "Mô hình kinh doanh",
                delegate { TogglePanel(_modelPanel); }, UiFactory.ButtonStyle.Quiet);
            UiFactory.TextButton(body, "OpenSource", "Mở bảng mô phỏng HTML", delegate
            {
                string path = Path.Combine(Application.streamingAssetsPath, "Mo-phong-khoi-nghiep-tra.html");
                if (File.Exists(path)) Application.OpenURL(new Uri(path).AbsoluteUri);
                else ShowToast("Không tìm thấy bảng mô phỏng trong bản cài đặt.");
            });
            UiFactory.Label(body, "Controls", "Kéo chuột trái để di chuyển góc nhìn. Lăn chuột để phóng to. Chuột phải để nhân vật đi tới. Nút ngôi nhà đưa bạn về toàn cảnh.",
                16, TextAnchor.UpperLeft, GardenPalette.TextMuted);
            _journalPanel.SetActive(false);

            // The short prompt is in open grass beside the bottom dock, not above the map.
            var hint = FarmSurface(transform, "GoalHint", new Vector2(.5f, 0), 0, 136, 388, 35, 12);
            _bookGoal = FarmText(hint.transform, "Hint", "", 14, 12, -2, 364, 30);
            _bookGoal.alignment = TextAnchor.MiddleCenter;
            hint.raycastTarget = false;
        }

        void RefreshFarmHud(GameState state, ContentCatalog catalog, long untilNextSeason)
        {
            _coinsLabel.text = state.Coins.ToString("N0");
            int ready = 0, pests = 0;
            long stock = 0;
            _actionPlot = -1;
            foreach (var pair in state.Inventory) stock += pair.Value;
            foreach (var plot in state.Plots)
            {
                if (!plot.Unlocked) continue;
                if (plot.PestActive) pests++;
                if (plot.Phase == PlotPhase.Ready) { ready++; if (_actionPlot < 0) _actionPlot = plot.PlotId; }
            }
            _actionCaption.text = "Thu hoạch";
            if (_actionPlot < 0)
            {
                foreach (var plot in state.Plots)
                    if (plot.Unlocked && plot.Phase == PlotPhase.Empty) { _actionPlot = plot.PlotId; break; }
                _actionCaption.text = _actionPlot >= 0 ? "Gieo hạt" : "Chăm cây";
            }
            UiFactory.SetProgress(_seasonProgress, 1f - (float)untilNextSeason / catalog.Balance.SeasonLengthMs);
            int seconds = Mathf.CeilToInt(untilNextSeason / 1000f);
            var season = Cultivation.SeasonAt(catalog.Balance, state.SimulationTimeMs);
            _seasonLabel.text = "Mùa " + SeasonName(season) + "  ·  " + seconds / 60 + ":" + (seconds % 60).ToString("D2");

            // Dòng nhắc dưới nút giữa là chỗ duy nhất còn nói về tình trạng khu vườn sau khi thẻ
            // "12/12 ô đất" biến mất khỏi HUD, nên nó phải nói đủ: mấy ô có sâu, mấy ô chờ thu,
            // hay chưa gieo gì. Nút giữa cũng chính là nút làm việc đó, nên câu nhắc và cái nút
            // luôn nói về cùng một việc.
            _bookGoal.text = pests > 0 ? pests + " ô có sâu · Bấm nút giữa để chăm cây" :
                ready > 0 ? "Có " + ready + " ô đã chín · Sẵn sàng thu hoạch" :
                _actionPlot >= 0 ? "Gieo những mầm đầu tiên cho khu vườn" : "Cây đang lớn · Ghé xưởng pha một mẻ trà";
            SetNavActive(_workshopButton, _workshopPanel.activeSelf);
            SetNavActive(_journalButton, _journalPanel.activeSelf);
            RefreshDebtChip(state, catalog);
        }

        /// <summary>
        /// Thẻ nợ ở góc trái. Ba trạng thái, và ba trạng thái đó là ba việc khác nhau người chơi
        /// cần làm: đang trả bình thường, đang thiếu kỳ, và đã bị niêm phong.
        /// </summary>
        void RefreshDebtChip(GameState state, ContentCatalog catalog)
        {
            if (_debtChip == null) return;
            var loan = state.Loan;
            bool show = loan.Active || loan.OverdueCoins > 0;
            _debtChip.gameObject.SetActive(show);
            if (!show) return;

            long due = Finance.InstallmentDue(loan) + loan.OverdueCoins;
            long untilDue = Math.Max(0, state.NextCycleCloseAtMs - state.SimulationTimeMs);
            _debtValue.text = loan.RemainingPrincipalCoins.ToString("N0") + DefaultContent.CoinGlyph;

            if (loan.Sealed)
            {
                _debtCaption.text = "Bị niêm phong";
                _debtCaption.color = GardenPalette.Pest;
            }
            else if (loan.ConsecutiveShortfalls > 0)
            {
                _debtCaption.text = "Thiếu " + loan.ConsecutiveShortfalls + "/" +
                                    catalog.Balance.LoanSealShortfalls + " kỳ";
                _debtCaption.color = GardenPalette.StateWarn;
            }
            else
            {
                int seconds = Mathf.CeilToInt(untilDue / 1000f);
                _debtCaption.text = "Trả " + due.ToString("N0") + DefaultContent.CoinGlyph +
                                    " sau " + seconds + "s";
                _debtCaption.color = state.Coins >= due ? GardenPalette.TextMuted : GardenPalette.StateWarn;
            }
        }

        void ResetFarmView()
        {
            var boot = FindAnyObjectByType<GameBootstrap>();
            if (boot != null && boot.Rig != null) boot.Rig.ResetView();
        }

        void OpenCarePlot()
        {
            PlotState best = null;
            foreach (var plot in _session.State.Plots)
            {
                if (!plot.Unlocked) continue;
                if (best == null || (plot.PestActive && !best.PestActive) ||
                    (plot.PestActive == best.PestActive && plot.Weeds > best.Weeds)) best = plot;
            }
            if (best != null) OpenPlotPopup(best.PlotId);
        }

        void DoFarmAction()
        {
            if (_actionPlot < 0) { OpenCarePlot(); return; }
            var plot = _session.State.Plot(_actionPlot);
            if (plot != null && plot.Phase == PlotPhase.Ready)
                Run(_session.HarvestAndReplant(_actionPlot));
            else OpenPlotPopup(_actionPlot);
        }
    }
}
