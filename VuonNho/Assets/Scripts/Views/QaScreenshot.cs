using System;
using UnityEngine;
using VuonNho.Core;

namespace VuonNho.Views
{
    /// <summary>
    /// Cong cu QA: chup man hinh tu chinh ban build roi thoat, de kiem tra hinh anh that
    /// tren may chuan thay vi tin vao anh render trong Editor.
    ///
    /// VuonNho.exe -vuonnho-screenshot "D:\anh.png" -vuonnho-screenshot-delay 6
    ///
    /// Khong co tham so thi component nay khong lam gi.
    /// </summary>
    public sealed class QaScreenshot : MonoBehaviour
    {
        public const string PathArgument = "-vuonnho-screenshot";
        public const string DelayArgument = "-vuonnho-screenshot-delay";
        public const string PanelArgument = "-vuonnho-open-panel";
        public const string SeedArgument = "-vuonnho-screenshot-seed";
        public const string WalkArgument = "-vuonnho-screenshot-walk";
        public const string PlaceArgument = "-vuonnho-screenshot-place";

        string _outputPath;
        string _panelName;
        bool _seed;
        bool _hasWalkPoint;
        Vector3 _walkPoint;
        bool _walked;
        string _placeDefinitionId;
        float _placeRotationDeg;
        bool _placing;
        float _delaySeconds = 5f;
        float _captureAt = -1f;
        bool _captured;
        bool _seeded;
        string _mapView;
        bool _hideHud;
        bool _viewApplied;
        string _hoverStage;
        bool _hoverApplied;
        int _motionFrames;
        int _motionFrame;

        void Awake()
        {
            var arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (arguments[i] == PathArgument && i + 1 < arguments.Length)
                    _outputPath = arguments[i + 1];
                else if (arguments[i] == DelayArgument && i + 1 < arguments.Length)
                    float.TryParse(arguments[i + 1], out _delaySeconds);
                else if (arguments[i] == PanelArgument && i + 1 < arguments.Length)
                    _panelName = arguments[i + 1];
                else if (arguments[i] == SeedArgument)
                    _seed = true;
                else if (arguments[i] == WalkArgument && i + 1 < arguments.Length)
                    _hasWalkPoint = TryParsePoint(arguments[i + 1], out _walkPoint);
                else if (arguments[i] == PlaceArgument && i + 1 < arguments.Length)
                    ParsePlaceArgument(arguments[i + 1]);
                else if (arguments[i] == "-vuonnho-map-view" && i + 1 < arguments.Length)
                    _mapView = arguments[i + 1];
                else if (arguments[i] == "-vuonnho-hide-hud")
                    _hideHud = true;
                else if (arguments[i] == "-vuonnho-hover" && i + 1 < arguments.Length)
                    _hoverStage = arguments[i + 1];
                else if (arguments[i] == "-vuonnho-motion-frames" && i + 1 < arguments.Length)
                    int.TryParse(arguments[i + 1], out _motionFrames);
            }

            if (string.IsNullOrEmpty(_outputPath))
            {
                enabled = false;
                return;
            }
            _captureAt = Time.realtimeSinceStartup + _delaySeconds;
        }

        void Update()
        {
            if (_captured || _captureAt < 0f || Time.realtimeSinceStartup < _captureAt) return;

            if (_seed && !_seeded)
            {
                _seeded = true;
                SeedGrownGarden();
                // Cho vuon dung lai theo trang thai moi: HUD tinh lai muc tieu, cay moc len,
                // may bat dau pha. Chup ngay frame sau se dinh mot khung hinh nua voi nua cu.
                _captureAt = Time.realtimeSinceStartup + 1.5f;
                return;
            }

            if (!string.IsNullOrEmpty(_panelName))
            {
                var hud = FindAnyObjectByType<GameHud>();
                if (hud != null) hud.OpenPanelByName(_panelName);
                _panelName = null;
                return;   // cho mot frame de panel kip dung xong
            }

            if (_placeDefinitionId != null && !_placing)
            {
                _placing = true;
                var bootstrap = FindAnyObjectByType<GameBootstrap>();
                if (bootstrap != null)
                {
                    bootstrap.BeginPlacingDecoration(_placeDefinitionId);
                    if (bootstrap.Preview != null) bootstrap.Preview.RotateBy(_placeRotationDeg);
                }
                // Bong ma bam theo con tro that, nen anh chi co no khi con tro dang o tren vuon.
                // Cho mot nhip de nguoi goi kip dat con tro vao giua man hinh.
                _captureAt = Time.realtimeSinceStartup + 1.2f;
                return;
            }

            if (_hasWalkPoint && !_walked)
            {
                _walked = true;
                SendWalkCommand();
                // Cho mot nhip ngan roi moi chup: vong tron van con ro, ma nhan vat da roi cho
                // va dang do buoc — anh chup ke duoc ca hai nua cua chuyen nay.
                _captureAt = Time.realtimeSinceStartup + 0.15f;
                return;
            }

            if (!_viewApplied)
            {
                _viewApplied = true;
                var bootstrap = FindAnyObjectByType<GameBootstrap>();
                if (bootstrap != null && bootstrap.Rig != null)
                {
                    if (_mapView == "farm") bootstrap.Rig.FocusGround(new Vector3(0f, 0f, 0.5f), 5.6f);
                    if (_mapView == "factory") bootstrap.Rig.FocusGround(new Vector3(8.5f, 0f, 0.7f), 5.7f);
                    if (string.IsNullOrEmpty(_mapView)) bootstrap.Rig.ResetView();
                }
                if (_hideHud)
                {
                    var hud = FindAnyObjectByType<GameHud>();
                    if (hud != null) hud.GetComponent<Canvas>().enabled = false;
                }
                return;
            }

            if (!_hoverApplied && !string.IsNullOrEmpty(_hoverStage))
            {
                _hoverApplied = true;
                var bootstrap = FindAnyObjectByType<GameBootstrap>();
                var hover = bootstrap != null ? bootstrap.GetComponent<StationHoverController>() : null;
                if (hover != null)
                {
                    hover.enabled = false;
                    foreach (var station in bootstrap.Stations)
                        if (station.StageId == _hoverStage)
                        {
                            var screen = bootstrap.GameCamera.WorldToScreenPoint(station.GetComponent<Collider>().bounds.center);
                            hover.UpdateHoverAt(new Vector2(screen.x, screen.y));
                            Debug.Log("[Hover QA] " + _hoverStage + " visible=" + hover.TooltipVisible);
                            break;
                        }
                }
                return;
            }

            if (_motionFrame < Mathf.Clamp(_motionFrames, 0, 200))
            {
                string folder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_outputPath),
                    System.IO.Path.GetFileNameWithoutExtension(_outputPath) + "-frames");
                System.IO.Directory.CreateDirectory(folder);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,
                    "frame" + _motionFrame.ToString("D3") + ".png"));
                _motionFrame++;
                _captureAt = Time.realtimeSinceStartup + 0.1f;
                return;
            }

            _captured = true;
            ScreenCapture.CaptureScreenshot(_outputPath);
            Debug.Log("[VuonNho] QA screenshot: " + _outputPath);
            // Cho vai frame de file duoc ghi xong roi moi thoat.
            Invoke("QuitAfterCapture", 2f);
        }

        /// <summary>
        /// Tua vuon toi luc da mo het 12 o va mua het nang cap, de anh chup tai lieu cho thay
        /// mot khu vuon co thu de nhin chu khong phai bon o dat trong.
        ///
        /// Mua theo dung thu tu catalog: thu tu do da thoa dieu kien mo khoa cua tung muc va
        /// tang dan theo gia, nen khong can chep lai lo trinh cua ban can bang o day.
        /// </summary>
        void SeedGrownGarden()
        {
            var bootstrap = FindAnyObjectByType<GameBootstrap>();
            if (bootstrap == null || bootstrap.Session == null || bootstrap.Session.State == null)
            {
                Debug.LogWarning("[VuonNho] Khong tim thay phien choi de tua vuon.");
                return;
            }

            var session = bootstrap.Session;
            var hud = FindAnyObjectByType<GameHud>();

            // Popup vang mat che kin giua man hinh. Anh tai lieu chup khu vuon, khong chup popup.
            session.AcknowledgeOfflineSummary();
            if (hud != null)
            {
                var modal = hud.transform.Find("OfflineModal");
                if (modal != null) modal.gameObject.SetActive(false);
            }

            PlantEveryEmptyPlot(session);

            foreach (var upgrade in session.Catalog.Upgrades)
            {
                if (session.State.UpgradeLevel(upgrade.Id) >= 1) continue;

                int guard = 0;
                while (session.State.Coins < upgrade.Cost && guard++ < 4000)
                {
                    session.DebugAdvance(10000);
                    HarvestEveryReadyPlot(session);
                }

                if (!session.Purchase(upgrade.Id).Success) break;

                if (upgrade.Kind == UpgradeKind.UnlockCrop)
                {
                    for (int i = 0; i < session.State.Plots.Count; i++)
                        if (session.State.Plot(i).Unlocked) session.SetNextCrop(i, upgrade.TargetId);
                    var recipe = session.Catalog.RecipeForCrop(upgrade.TargetId);
                    if (recipe != null) session.SelectRecipe(recipe.Id);
                }
                if (upgrade.Kind == UpgradeKind.ExpandPlots) PlantEveryEmptyPlot(session);
            }

            // Mua het nang cap thi vua het xu va nhieu o vua thu xong dang de trong. Trong lai
            // roi chay them mot doan de co xu du tru, sau do trong lan cuoi va chi tua mot doan
            // ngan: anh chup can vuon dang len cay, khong phai vuon vua bi vet sach.
            BuildWholeChain(session);

            PlantEveryEmptyPlot(session);
            session.DebugAdvance(300000);
            HarvestEveryReadyPlot(session);
            PlantEveryEmptyPlot(session);
            SelectRecipeForPlantedCrop(session);
            session.DebugAdvance(20000);

            Debug.Log("[VuonNho] Da tua vuon: " + session.State.UnlockedPlotCount() + " o, " +
                      session.State.Coins + " xu.");
            if (hud != null) hud.Refresh();
        }

        /// <summary>
        /// Gia lap mot cu bam chuot phai xuong diem (x, z) trong vuon, de anh tai lieu bat duoc
        /// nhan vat dang di va vong tron bao lai. Di qua dung duong ma chuot that di, khong goi
        /// thang WalkTo: anh chup phai la anh cua thu nguoi choi se thay.
        /// </summary>
        void SendWalkCommand()
        {
            var bootstrap = FindAnyObjectByType<GameBootstrap>();
            if (bootstrap == null) return;
            var camera = bootstrap.GameCamera != null ? bootstrap.GameCamera : Camera.main;
            if (camera == null) return;

            var screen = camera.WorldToScreenPoint(_walkPoint);
            if (!bootstrap.TryWalkCommand(new Vector3(screen.x, screen.y, 0f)))
                Debug.LogWarning("[VuonNho] Lenh di khong toi noi: tia khong cham vuon o diem da cho.");
        }

        /// <summary>Doc "id" hoac "id,goc" — mon trang tri can cam len tay va goc xoay san.</summary>
        void ParsePlaceArgument(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            var parts = text.Split(',');
            _placeDefinitionId = parts[0];
            if (parts.Length > 1) float.TryParse(parts[1], out _placeRotationDeg);
        }

        /// <summary>Doc "x,z" tren dong lenh thanh mot diem tren mat dat.</summary>
        static bool TryParsePoint(string text, out Vector3 point)
        {
            point = Vector3.zero;
            if (string.IsNullOrEmpty(text)) return false;
            var parts = text.Split(',');
            if (parts.Length != 2) return false;
            float x, z;
            if (!float.TryParse(parts[0], out x) || !float.TryParse(parts[1], out z)) return false;
            point = new Vector3(x, 0f, z);
            return true;
        }

        /// <summary>
        /// Xay ca day chuyen va thue du tho, de anh tai lieu co san may chu khong phai mot bai co
        /// trong. Tua toi khi du tien tung buoc mot chu khong tang xu: anh chup phai la anh cua
        /// mot van choi that.
        /// </summary>
        static void BuildWholeChain(GameSession session)
        {
            foreach (var stage in session.Catalog.Stages)
            {
                int guard = 0;
                while (session.State.Coins < stage.Cost && guard++ < 4000)
                {
                    session.DebugAdvance(10000);
                    HarvestEveryReadyPlot(session);
                }
                session.BuyStation(stage.Id);
            }

            for (int i = 0; i < session.Catalog.Stages.Count; i++)
            {
                int guard = 0;
                while (session.State.Coins < session.Catalog.Balance.WorkerHireCost && guard++ < 4000)
                {
                    session.DebugAdvance(10000);
                    HarvestEveryReadyPlot(session);
                }
                session.HireWorker();
            }

            Debug.Log("[VuonNho] Day chuyen: " + session.State.OwnedStationCount() + " may, " +
                      session.State.HiredWorkers + " tho.");
        }

        static void PlantEveryEmptyPlot(GameSession session)
        {
            string cropId = BestCrop(session);
            for (int i = 0; i < session.State.Plots.Count; i++)
            {
                var plot = session.State.Plot(i);
                if (plot.Unlocked && plot.Phase == PlotPhase.Empty) session.Plant(i, cropId);
            }
        }

        /// <summary>May dang cho nguyen lieu cua mot cong thuc khong ai trong nua thi nhin nhu hong.</summary>
        static void SelectRecipeForPlantedCrop(GameSession session)
        {
            var recipe = session.Catalog.RecipeForCrop(BestCrop(session));
            if (recipe != null) session.SelectRecipe(recipe.Id);
        }

        /// <summary>Cay moi nhat da mo khoa — cung la cay dat nhat, dung nhu nguoi choi se trong.</summary>
        static string BestCrop(GameSession session)
        {
            string cropId = DefaultContent.CropMint;
            foreach (var crop in session.Catalog.Crops)
                if (session.State.UnlockedCropIds.Contains(crop.Id)) cropId = crop.Id;
            return cropId;
        }

        /// <summary>Truoc khi co robot thi khong thu tay se khong bao gio du xu mua no.</summary>
        static void HarvestEveryReadyPlot(GameSession session)
        {
            for (int i = 0; i < session.State.Plots.Count; i++)
                if (session.State.Plot(i).Phase == PlotPhase.Ready) session.HarvestAndReplant(i);
        }

        void QuitAfterCapture()
        {
            Application.Quit();
        }
    }
}
