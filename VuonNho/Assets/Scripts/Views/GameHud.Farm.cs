using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
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
        Text _landCount, _readyCount, _stockCount, _staffCount, _actionCaption;
        Text _careBadge, _bookGoal;
        Image _seasonProgress;
        GameObject _journalPanel;
        Button _journalButton;
        Image _debtChip;
        Text _debtCaption, _debtValue;
        int _actionPlot = -1;

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

            var land = FarmSurface(hud.transform, "FarmStats", tl, 22, -114, 187, 87);
            FarmIcon(land.transform, FarmHudIcon.Kind.Sprout, 10, -6, 35);
            _landCount = FarmText(land.transform, "Plots", "", 18, 53, -7, 124, 31, true);
            var divider = UiFactory.Panel(land.transform, "Divider", GardenPalette.PanelDivider);
            Place(divider.rectTransform, tl, 13, -43, 160, 1);
            divider.raycastTarget = false;
            FarmIcon(land.transform, FarmHudIcon.Kind.Leaf, 13, -48, 29);
            _readyCount = FarmText(land.transform, "Ready", "", 16, 53, -46, 126, 30);

            var care = FarmButton(hud.transform, "CareButton", "Chăm cây", FarmHudIcon.Kind.Heart,
                tl, 22, -214, OpenCarePlot);
            _careBadge = FarmText(care.transform, "CareBadge", "", 13, 44, 4, 22, 23, true);
            _careBadge.alignment = TextAnchor.MiddleCenter;
            _careBadge.color = GardenPalette.ButtonDanger;

            var coins = FarmSurface(hud.transform, "CoinChip", tr, -22, -24, 209, 48, 17);
            FarmIcon(coins.transform, FarmHudIcon.Kind.Coin, -8, 5, 59);
            _coinsLabel = FarmText(coins.transform, "Value", "0", 23, 56, -5, 137, 36, true);
            _coinsLabel.alignment = TextAnchor.MiddleRight;
            _coinsLabel.resizeTextForBestFit = true;
            _coinsLabel.resizeTextMinSize = 13;
            _coinsLabel.resizeTextMaxSize = 23;

            var workers = FarmSurface(hud.transform, "WorkerChip", tr, -246, -24, 133, 48, 17);
            FarmIcon(workers.transform, FarmHudIcon.Kind.Workers, 0, 0, 47);
            _staffCount = FarmText(workers.transform, "Value", "", 19, 52, -7, 70, 33, true);
            var workerOpen = workers.gameObject.AddComponent<Button>();
            workerOpen.targetGraphic = workers;
            workerOpen.onClick.AddListener(ShowWorkshopPanel);

            var stock = FarmSurface(hud.transform, "StockChip", tr, -22, -87, 172, 72, 17);
            FarmIcon(stock.transform, FarmHudIcon.Kind.Crate, 1, -1, 64);
            FarmText(stock.transform, "Caption", "Trong kho", 13, 71, -7, 90, 23);
            _stockCount = FarmText(stock.transform, "Value", "", 23, 71, -29, 88, 32, true);
            _stockCount.resizeTextForBestFit = true;
            _stockCount.resizeTextMinSize = 12;
            _stockCount.resizeTextMaxSize = 23;
            var stockOpen = stock.gameObject.AddComponent<Button>();
            stockOpen.targetGraphic = stock;
            stockOpen.onClick.AddListener(delegate { TogglePanel(_inventoryPanel); });

            // Thẻ nợ: một khoản vay là nghĩa vụ có kỳ hạn, nên nó phải nhìn thấy được mà không
            // phải mở bảng nào. Chỉ hiện khi đang có nợ — người chưa vay không cần một ô trống.
            // Dưới nút Chăm cây, không chen vào giữa cột: hồ sơ dừng ở −100, thẻ vườn ở −114…−201,
            // nút Chăm cây ở −214…−278. Đặt cao hơn −290 là đè lên một trong ba cái đó.
            _debtChip = FarmSurface(hud.transform, "DebtChip", new Vector2(0, 1), 22, -290, 210, 66, 16);
            FarmIcon(_debtChip.transform, FarmHudIcon.Kind.Banknotes, 2, -2, 54);
            _debtCaption = FarmText(_debtChip.transform, "Caption", "Dư nợ", 13, 62, -6, 132, 22);
            _debtValue = FarmText(_debtChip.transform, "Value", "", 18, 62, -26, 132, 30, true);
            var debtOpen = _debtChip.gameObject.AddComponent<Button>();
            debtOpen.targetGraphic = _debtChip;
            debtOpen.onClick.AddListener(delegate { TogglePanel(_financePanel); });
            _debtChip.gameObject.SetActive(false);

            _upgradeButton = FarmButton(hud.transform, "UpgradeButton", "Nâng cấp", FarmHudIcon.Kind.Tools,
                bl, 22, 174, delegate { TogglePanel(_upgradePanel); });
            _journalButton = FarmButton(hud.transform, "JournalButton", "Sổ tay", FarmHudIcon.Kind.Book,
                bl, 22, 98, delegate { TogglePanel(_journalPanel); });
            FarmButton(hud.transform, "HomeButton", "Khu vườn", FarmHudIcon.Kind.House,
                bl, 22, 22, ResetFarmView);
            _inventoryButton = FarmButton(hud.transform, "InventoryButton", "Kho", FarmHudIcon.Kind.Clipboard,
                bl, 96, 22, delegate { TogglePanel(_inventoryPanel); });
            _decorateButton = FarmButton(hud.transform, "DecorateButton", "Trang trí", FarmHudIcon.Kind.Sprout,
                bl, 170, 22, delegate { TogglePanel(_decoratePanel); });

            _workshopButton = FarmButton(hud.transform, "WorkshopButton", "Xưởng", FarmHudIcon.Kind.Factory,
                br, -22, 174, delegate { TogglePanel(_workshopPanel); });
            FarmButton(hud.transform, "WorkersButton", "Nhân sự", FarmHudIcon.Kind.Workers,
                br, -22, 98, ShowWorkshopPanel);
            _settingsButton = FarmButton(hud.transform, "SettingsButton", "Cài đặt", FarmHudIcon.Kind.Settings,
                br, -22, 22, delegate { TogglePanel(_settingsPanel); });

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
            _landCount.text = state.UnlockedPlotCount() + " / " + catalog.Balance.MaximumPlots + " ô đất";
            _readyCount.text = ready + " ô chờ thu";
            _stockCount.text = stock.ToString("N0");
            _staffCount.text = state.HiredWorkers + " thợ";
            _careBadge.text = pests > 0 ? pests.ToString() : "";
            UiFactory.SetProgress(_seasonProgress, 1f - (float)untilNextSeason / catalog.Balance.SeasonLengthMs);
            int seconds = Mathf.CeilToInt(untilNextSeason / 1000f);
            var season = Cultivation.SeasonAt(catalog.Balance, state.SimulationTimeMs);
            _seasonLabel.text = "Mùa " + SeasonName(season) + "  ·  " + seconds / 60 + ":" + (seconds % 60).ToString("D2");
            _bookGoal.text = pests > 0 ? pests + " ô có sâu · Bấm trái tim để chăm cây" :
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
