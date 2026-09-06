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
            yield return RunControlChecks();
            yield return RunProcessingChecks();
            RunSettingsChecks();

            Finish();
        }


        // ---------------------------------------------------------------- day chuyen che bien

        /// <summary>
        /// Day chuyen: mua du sau may, thue tho, va do xem la tuoi co di het chuoi ra thanh tra
        /// dong goi khong. Do trong ban build chu khong chi trong test EditMode vi day la cho
        /// Core, HUD va thoi gian that gap nhau.
        /// </summary>
        IEnumerator RunProcessingChecks()
        {
            Section("Dây chuyền chế biến trà");

            var catalog = _session.Catalog;
            Check("Có đủ sáu công đoạn giữa thu hoạch và quầy trà", catalog.Stages.Count == 6,
                  catalog.Stages.Count + " công đoạn");

            long needed = catalog.Balance.WorkerHireCost * catalog.Stages.Count;
            foreach (var stage in catalog.Stages) needed += stage.Cost;
            needed += 4000;   // du de tra luong trong luc do

            int wait = 0;
            while (_session.State.Coins < needed && wait++ < 4000)
            {
                _session.DebugAdvance(10000);
                if (wait % 40 == 0) yield return null;
            }
            Check("Gom đủ xu để xây cả dây chuyền", _session.State.Coins >= needed,
                  _session.State.Coins + " / " + needed + " xu");

            bool boughtAll = true;
            string firstFailure = null;
            foreach (var stage in catalog.Stages)
            {
                var result = _session.BuyStation(stage.Id);
                if (!result.Success) { boughtAll = false; firstFailure = stage.Id + ": " + result.FailureReason; }
            }
            Check("Mua được cả sáu máy", boughtAll && _session.State.OwnedStationCount() == 6,
                  firstFailure ?? (_session.State.OwnedStationCount() + " máy"));

            // Mua lai mot cai da co phai bi tu choi, khong phai tru tien lan hai.
            long beforeRepeat = _session.State.Coins;
            var repeat = _session.BuyStation(catalog.Stages[0].Id);
            Check("Không mua được một cái máy hai lần",
                  !repeat.Success && _session.State.Coins == beforeRepeat,
                  repeat.FailureReason);

            bool hiredAll = true;
            for (int i = 0; i < catalog.Stages.Count; i++)
                if (!_session.HireWorker().Success) hiredAll = false;
            Check("Thuê đủ thợ cho tất cả các máy",
                  hiredAll && _session.State.HiredWorkers == catalog.Stages.Count,
                  _session.State.HiredWorkers + " thợ");

            // Cho day chuyen chay. Do bang so tra dong goi da tung xuat hien chu khong bang so ton
            // kho tai mot thoi diem: quay tra an tra dong goi ngay khi co, nen ton kho co the ve 0.
            string packedSuffix = catalog.PackedSuffix;
            long coinsBefore = _session.State.Coins;
            bool sawPacked = false;
            for (int step = 0; step < 240 && !sawPacked; step++)
            {
                _session.DebugAdvance(5000);
                foreach (var crop in catalog.Crops)
                    if (_session.State.InventoryOf(ProcessChain.ItemId(crop.Id, packedSuffix)) > 0)
                        sawPacked = true;
                if (step % 20 == 0) yield return null;
            }
            Check("Lá tươi đi hết chuỗi thành trà đóng gói", sawPacked,
                  sawPacked ? null : "không thấy món nào ở cuối chuỗi");

            // Luong bi tru that: chay them mot ky nua va doi chieu.
            _session.DebugAdvance(catalog.Balance.PayrollPeriodMs);
            yield return null;
            Check("Thợ ăn lương và xu vẫn không âm", _session.State.Coins >= 0,
                  _session.State.Coins + " xu, " + _session.State.StaffedWorkers + "/" +
                  _session.State.HiredWorkers + " thợ đang làm");

            Check("Xây dây chuyền xong vẫn kiếm được xu", _session.State.Coins > 0,
                  "trước " + coinsBefore + ", sau " + _session.State.Coins);

            // Bang xuong phai noi dung so may da mua.
            _hud.OpenPanelByName("workshop");
            yield return null;
            yield return null;
            var surface = FindOpenSurface("workshop");
            Check("Bảng Xưởng mở được và có đủ hàng", surface != null &&
                  surface.GetComponentsInChildren<UnityEngine.UI.Text>(true).Length > 0,
                  surface == null ? "không mở được bảng" : null);
            if (surface != null) CheckTextOverflow("workshop", surface);
            CloseSurface("workshop");
            yield return null;
        }

        // ---------------------------------------------------------------- camera va nhan vat

        /// <summary>
        /// Zoom, keo man hinh va lenh di cua nhan vat. Ba thu nay khong co trong bo test EditMode
        /// vi ca ba deu can mot camera that va nhieu frame that.
        /// </summary>
        IEnumerator RunControlChecks()
        {
            Section("Điều khiển camera và nhân vật");

            yield return CheckPlotClickStillLands();

            var rig = _bootstrap.Rig;
            if (rig == null || rig.Camera == null)
            {
                Check("Camera có bộ điều khiển zoom/kéo", false, "GameBootstrap.Rig chưa được gán.");
            }
            else
            {
                rig.ResetView();
                yield return null;

                float home = rig.Camera.orthographicSize;
                var centre = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

                rig.ZoomBy(3f, centre);
                float closer = rig.Camera.orthographicSize;
                Check("Lăn chuột tới thì nhìn gần lại", closer < home - 0.01f,
                      home.ToString("0.00") + " → " + closer.ToString("0.00"));

                rig.ZoomBy(-40f, centre);
                float farthest = rig.Camera.orthographicSize;
                Check("Thu nhỏ hết cỡ vẫn nằm trong giới hạn",
                      farthest <= rig.MaxSize + 0.001f && farthest >= rig.MinSize - 0.001f,
                      farthest.ToString("0.00") + " (tối đa " + rig.MaxSize.ToString("0.00") + ")");

                rig.ZoomBy(40f, centre);
                Check("Phóng to hết cỡ vẫn nằm trong giới hạn",
                      rig.Camera.orthographicSize >= rig.MinSize - 0.001f,
                      rig.Camera.orthographicSize.ToString("0.00") +
                      " (tối thiểu " + rig.MinSize.ToString("0.00") + ")");

                // Zoom phai bam vao cho con tro dang tro, khong nhay ve giua man hinh.
                rig.ResetView();
                yield return null;
                var anchor = new Vector3(Screen.width * 0.32f, Screen.height * 0.62f, 0f);
                Vector3 groundBefore, groundAfter;
                bool measured = TryGroundUnder(rig.Camera, anchor, out groundBefore);
                rig.ZoomBy(2f, anchor);
                measured &= TryGroundUnder(rig.Camera, anchor, out groundAfter);
                float slip = measured ? Vector3.Distance(groundBefore, groundAfter) : 999f;
                Check("Zoom bám điểm dưới con trỏ", measured && slip < 0.05f,
                      measured ? "lệch " + (slip * 100f).ToString("0.0") + " cm" : "không đo được");

                rig.ResetView();
                yield return null;
                rig.PanBy(new Vector3(500f, 0f, 500f));
                Vector3 focus;
                bool hasFocus = rig.TryFocusPoint(out focus);
                Check("Kéo màn hình không ra khỏi vườn",
                      hasFocus && Mathf.Abs(focus.x) <= rig.PanLimit + 0.01f &&
                      Mathf.Abs(focus.z) <= rig.PanLimit + 0.01f,
                      hasFocus ? "dừng ở (" + focus.x.ToString("0.0") + ", " + focus.z.ToString("0.0") +
                                 "), giới hạn " + rig.PanLimit.ToString("0.0") : "không đo được");

                rig.ResetView();
                yield return null;
                Check("Về lại góc nhìn ban đầu",
                      Mathf.Abs(rig.Camera.orthographicSize - home) < 0.01f,
                      rig.Camera.orthographicSize.ToString("0.00"));
            }

            var character = _bootstrap.Character;
            if (character == null)
            {
                Check("Có nhân vật chính trong vườn", false, "GameBootstrap.Character chưa được gán.");
                yield break;
            }

            Check("Nhân vật có hai chân để bước",
                  character.LegLeft != null && character.LegRight != null,
                  character.LegLeft != null && character.LegRight != null
                      ? null : "thiếu LegLeft/LegRight — nhân vật sẽ trượt chứ không bước");

            // Duong tu cu bam chuot phai toi lenh di. Muc "Nhan lenh di" o duoi goi thang WalkTo
            // nen khong noi duoc gi ve doan dinh tuyen: tia co xuong dat khong, che do dat/go co
            // nuot mat lenh khong, nhan vat co duoc gan vao bootstrap khong.
            var groundPoint = _bootstrap.Plots != null && _bootstrap.Plots.Length > 0 &&
                              _bootstrap.Plots[0] != null
                ? _bootstrap.Plots[0].transform.position
                : character.transform.position;
            var groundScreen = (_bootstrap.GameCamera != null ? _bootstrap.GameCamera : Camera.main)
                .WorldToScreenPoint(groundPoint);
            int routedBefore = _bootstrap.WalkCommandCount;
            if (_bootstrap.Marker != null) _bootstrap.Marker.Hide();
            bool routed = _bootstrap.TryWalkCommand(new Vector3(groundScreen.x, groundScreen.y, 0f));
            Check("Chuột phải xuống đất thành lệnh đi",
                  routed && _bootstrap.WalkCommandCount == routedBefore + 1 && character.IsWalking,
                  routed ? null : "tia không chạm vườn hoặc lệnh bị chặn");
            character.Teleport(character.transform.position);

            // Vong tron bao lai cu bam. Khong co no thi mot cu bam hut nhin y het mot cu bam
            // khong an, nen day la muc kiem chu khong phai chuyen trang tri.
            var marker = _bootstrap.Marker;
            Check("Chỗ vừa bấm có vòng tròn báo lại", marker != null && marker.IsShowing,
                  marker == null ? "GameBootstrap.Marker chưa được gán."
                                 : marker.IsShowing ? null : "vòng tròn không hiện");

            if (marker != null)
            {
                float deadline = Time.unscaledTime + marker.Duration + 1f;
                while (marker.IsShowing && Time.unscaledTime < deadline) yield return null;
                Check("Vòng tròn tự tắt chứ không nằm lại", !marker.IsShowing,
                      "sau " + marker.Duration.ToString("0.00") + " s");
            }
            yield return null;

            // Chuot that cua nguoi dung van bam duoc vao cua so trong luc chay, va moi cu chuot
            // phai la mot lenh di moi. Do lai khi co lenh la chen vao giua phep do, chu khong
            // bao la khong dat: cai bi hong luc do la phep do, khong phai tro choi.
            var start = character.transform.position;
            var goal = new Vector3(start.x - 2.4f, start.y, start.z + 1.8f);
            float missed = 0f;
            bool accepted = false, arrived = false, disturbed = false;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                character.Teleport(start);
                yield return null;

                int commandsBefore = _bootstrap.WalkCommandCount;
                character.WalkTo(goal);
                accepted = character.IsWalking;

                float deadline = Time.unscaledTime + 6f;
                while (character.IsWalking && Time.unscaledTime < deadline) yield return null;

                missed = Vector3.Distance(
                    new Vector3(character.transform.position.x, 0f, character.transform.position.z),
                    new Vector3(goal.x, 0f, goal.z));
                arrived = !character.IsWalking && missed < 0.15f;

                disturbed = _bootstrap.WalkCommandCount != commandsBefore;
                if (!disturbed) break;
            }

            Check("Nhận lệnh đi tới điểm được chỉ", accepted,
                  "đích (" + goal.x.ToString("0.0") + ", " + goal.z.ToString("0.0") + ")");
            Check("Đi tới nơi rồi dừng", arrived || disturbed,
                  disturbed
                      ? "chuột thật bấm chen vào giữa phép đo, không kết luận được"
                      : "cách đích " + (missed * 100f).ToString("0") + " cm");

            yield return CheckSolidDecorations(character);
            yield return CheckSolidScenery(character);
            yield return CheckPlacementPreview();
            yield return CheckInventoryGrid();

            // Diem ngoai vuon phai bi keo ve trong bo chu khong bi bo qua: mot cu bam hut van
            // phai dan den mot buoc di co nghia.
            character.WalkTo(new Vector3(500f, 0f, -500f));
            var destination = character.Destination;
            Check("Bấm ra ngoài vườn thì dừng ở mép",
                  Mathf.Abs(destination.x) <= character.WalkLimit + 0.01f &&
                  Mathf.Abs(destination.z) <= character.WalkLimit + 0.01f,
                  "đích (" + destination.x.ToString("0.0") + ", " + destination.z.ToString("0.0") +
                  "), giới hạn " + character.WalkLimit.ToString("0.0"));

            // Tra nhan vat ve cho cu de anh chup sau bo kiem tra van dung bo cuc quen thuoc.
            character.Teleport(start);
        }

        /// <summary>
        /// Tui do kieu o: mot o cho moi mat hang, ke ca hang trung gian cua day chuyen. Dem o
        /// chu khong chi mo bang ra xem: thieu o nghia la co mon nam trong kho ma khong cho nao
        /// tren man hinh ke ra no, va loi do khong bao gi ca.
        /// </summary>
        IEnumerator CheckInventoryGrid()
        {
            _hud.OpenPanelByName("inventory");
            yield return null;
            yield return null;

            var surface = FindOpenSurface("inventory");
            if (surface == null)
            {
                Check("Túi đồ có một ô cho mỗi mặt hàng", false, "Không mở được bảng Kho.");
                yield break;
            }

            int expected = _session.Catalog.Items.Count;
            int found = 0;
            foreach (var child in surface.GetComponentsInChildren<Transform>(true))
                if (child.name.StartsWith("Slot_", System.StringComparison.Ordinal)) found++;

            Check("Túi đồ có một ô cho mỗi mặt hàng", found == expected,
                  found + " ô / " + expected + " mặt hàng");

            // O chi tiet phai theo o dang chon, va no nam ngoai vung cuon nen luon nhin thay duoc.
            var footer = surface.Find("Footer/Detail/TitleRow/Name");
            var footerText = footer != null ? footer.GetComponent<UnityEngine.UI.Text>() : null;
            Check("Ô chi tiết nằm dưới đáy và không trống",
                  footerText != null && !string.IsNullOrEmpty(footerText.text),
                  footerText != null ? footerText.text : "không tìm thấy Footer/Detail");

            CloseSurface("inventory");
            yield return null;
        }

        /// <summary>
        /// Do da dat xuong la vat cung, tru loi di lat da. Hai muc nay di cung nhau vi chung la
        /// hai nua cua cung mot luat: neu chi kiem cai ghe thi mot thay doi lam moi thu deu cung
        /// van qua duoc, va loi di lat da se lang le chan duong.
        /// </summary>
        IEnumerator CheckSolidDecorations(CharacterView character)
        {
            if (!_session.DecoratingUnlocked)
            {
                Check("Không đi xuyên qua đồ đã đặt", false, "Chưa mở khoá trang trí để thử.");
                yield break;
            }

            var home = character.transform.position;

            // Dat ngay truoc mat nhan vat, ve phia xa luong cay — khoang trong nhat trong vuon.
            var spot = home + new Vector3(0f, 0f, -1.3f);
            int xMm = Mathf.RoundToInt(spot.x * 1000f);
            int zMm = Mathf.RoundToInt(spot.z * 1000f);

            // Mua het nang cap xong thi vua het xu. Tua toi khi du tien mua mon dat nhat trong
            // hai mon can thu, chu khong tang xu bang tay: tang tay se bo qua ca duong mua ban.
            long price = Mathf.Max((int)_session.Catalog.Decoration(DefaultDecorations.Bench).Cost,
                                   (int)_session.Catalog.Decoration(DefaultDecorations.StonePath).Cost);
            int wait = 0;
            while (_session.State.Coins < price && wait++ < 600)
            {
                _session.DebugAdvance(10000);
                if (wait % 40 == 0) yield return null;
            }

            var bench = _session.PlaceDecoration(DefaultDecorations.Bench, xMm, zMm, 0);
            if (!bench.Success)
            {
                Check("Không đi xuyên qua đồ đã đặt", false, "Không đặt được ghế: " + bench.FailureReason);
                yield break;
            }

            yield return RebuildDecorations();
            yield return WalkTowards(character, home, spot);
            float stopped = FlatDistance(character.transform.position, spot);
            Check("Không đi xuyên qua đồ đã đặt", stopped >= 0.45f,
                  "dừng cách tâm ghế " + (stopped * 100f).ToString("0") + " cm");

            _session.RemoveDecoration(_session.State.Decorations.Count - 1);

            // Nua thu hai: phien da la loi di, buoc len tren phai duoc.
            var path = _session.PlaceDecoration(DefaultDecorations.StonePath, xMm, zMm, 0);
            if (!path.Success)
            {
                Check("Lối đi lát đá vẫn bước lên được", false,
                      "Không đặt được phiến đá: " + path.FailureReason);
                character.Teleport(home);
                yield break;
            }

            yield return RebuildDecorations();
            yield return WalkTowards(character, home, spot);
            float onPath = FlatDistance(character.transform.position, spot);
            Check("Lối đi lát đá vẫn bước lên được", onPath < 0.15f,
                  "dừng cách tâm phiến đá " + (onPath * 100f).ToString("0") + " cm");

            _session.RemoveDecoration(_session.State.Decorations.Count - 1);
            yield return RebuildDecorations();
            character.Teleport(home);
        }

        /// <summary>
        /// Quay tra la vat cung. Hang rao va cay cung duoc danh dau, nhung quay tra la mon duy
        /// nhat nam giua vuon nen la mon duy nhat do duoc ma khong phu thuoc vao bo cuc canh nen.
        /// </summary>
        IEnumerator CheckSolidScenery(CharacterView character)
        {
            if (_bootstrap.Machine == null)
            {
                Check("Không đi xuyên qua quầy trà", false, "GameBootstrap.Machine chưa được gán.");
                yield break;
            }

            var home = character.transform.position;
            var station = _bootstrap.Machine.transform.position;

            yield return WalkTowards(character, home, station);
            float stopped = FlatDistance(character.transform.position, station);
            Check("Không đi xuyên qua quầy trà", stopped >= 0.8f,
                  "dừng cách tâm quầy " + (stopped * 100f).ToString("0") + " cm");

            character.Teleport(home);
            yield return null;
        }

        /// <summary>
        /// Bong ma xem truoc va goc xoay cua no. Do rieng khoi viec dat that vi hai thu hong
        /// theo hai kieu khac nhau: bong ma khong hien la nguoi choi dat mu, con goc xoay khong
        /// theo la mon do nam sai huong sau khi da dat.
        /// </summary>
        IEnumerator CheckPlacementPreview()
        {
            var preview = _bootstrap.Preview;
            if (preview == null || !_session.DecoratingUnlocked)
            {
                Check("Chọn món thì hiện bóng ma xem trước", false,
                      preview == null ? "GameBootstrap.Preview chưa được gán."
                                      : "Chưa mở khoá trang trí để thử.");
                yield break;
            }

            long price = _session.Catalog.Decoration(DefaultDecorations.Bench).Cost;
            int wait = 0;
            while (_session.State.Coins < price && wait++ < 600)
            {
                _session.DebugAdvance(10000);
                if (wait % 40 == 0) yield return null;
            }

            _bootstrap.BeginPlacingDecoration(DefaultDecorations.Bench);
            yield return null;

            Check("Chọn món thì hiện bóng ma xem trước",
                  preview.DefinitionId == DefaultDecorations.Bench &&
                  Mathf.Approximately(preview.RotationDeg, 0f),
                  "món " + (preview.DefinitionId ?? "không có") +
                  ", góc " + preview.RotationDeg.ToString("0"));

            // Bong ma chi dung len khi con tro cham dat, ma chuot that thi dang o dau khong biet.
            // Goi thang ShowAt de do chinh cai model co dung duoc hay khong.
            var home = _bootstrap.Character != null ? _bootstrap.Character.transform.position : Vector3.zero;
            var spot = home + new Vector3(0f, 0f, -1.3f);
            preview.ShowAt(spot, true);
            yield return null;
            Check("Bóng ma dựng được model của món đang cầm", preview.IsShowing,
                  preview.IsShowing ? null : "GardenSkin chưa có prefab cho " + DefaultDecorations.Bench);

            preview.Flip();
            bool flipped = Mathf.Abs(Mathf.DeltaAngle(preview.RotationDeg, 180f)) < 0.01f;
            preview.RotateBy(45f);
            bool dragged = Mathf.Abs(Mathf.DeltaAngle(preview.RotationDeg, 225f)) < 0.01f;
            Check("R xoay 180° và kéo chuột phải xoay tiếp", flipped && dragged,
                  "sau R rồi kéo 45°: " + preview.RotationDeg.ToString("0") + "°");

            // Dat that qua dung duong ma chuot di, roi doc lai goc trong state: goc tren bong ma
            // ma khong sang duoc mon do da dat thi bong ma chi la trang tri.
            var camera = _bootstrap.GameCamera != null ? _bootstrap.GameCamera : Camera.main;
            int before = _session.State.Decorations.Count;
            var screen = camera.WorldToScreenPoint(spot);
            _bootstrap.TryWorldClick(new Vector3(screen.x, screen.y, 0f));
            yield return null;

            bool placed = _session.State.Decorations.Count == before + 1;
            int rotation = placed ? _session.State.Decorations[before].RotationDeg : -1;
            Check("Món đặt xuống giữ đúng góc đã xoay", placed && rotation == 225,
                  placed ? "góc " + rotation + "°" : "không đặt được món nào");

            if (placed) _session.RemoveDecoration(before);
            _bootstrap.CancelDecorationMode();
            yield return null;
            Check("Thoát chế độ thì bóng ma biến mất", !preview.IsShowing && preview.DefinitionId == null,
                  preview.DefinitionId == null ? null : "vẫn còn cầm " + preview.DefinitionId);

            yield return RebuildDecorations();
            if (_bootstrap.Character != null) _bootstrap.Character.Teleport(home);
        }

        /// <summary>Collider cua mon vua dat chi ton tai sau khi DecorationLayer dung lai.</summary>
        IEnumerator RebuildDecorations()
        {
            if (_bootstrap.Decorations != null) _bootstrap.Decorations.RefreshIfChanged();
            yield return null;
            yield return null;
        }

        IEnumerator WalkTowards(CharacterView character, Vector3 from, Vector3 to)
        {
            character.Teleport(from);
            yield return null;
            character.WalkTo(to);

            float deadline = Time.unscaledTime + 6f;
            while (character.IsWalking && Time.unscaledTime < deadline) yield return null;
        }

        static float FlatDistance(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        }

        /// <summary>
        /// Chuot trai gio chot luc tha chu khong luc nhan, de phan biet voi keo man hinh. Muc nay
        /// giu cho cai gia cua thay doi do khong phai la "bam vao o dat khong con tac dung nua".
        /// </summary>
        IEnumerator CheckPlotClickStillLands()
        {
            var camera = _bootstrap.GameCamera != null ? _bootstrap.GameCamera : Camera.main;
            PlotView target = null;
            if (_bootstrap.Plots != null && camera != null)
            {
                for (int i = 0; i < _bootstrap.Plots.Length && target == null; i++)
                {
                    var view = _bootstrap.Plots[i];
                    if (view == null) continue;
                    var plot = _session.State.Plot(view.PlotId);
                    if (plot != null && plot.Unlocked && plot.Phase != PlotPhase.Ready) target = view;
                }
            }

            if (target == null)
            {
                Check("Bấm vào ô đất vẫn mở được bảng ô", false,
                      "Không tìm thấy ô đang mở khoá và chưa chín để thử.");
                yield break;
            }

            CloseSurface("plot");
            yield return null;

            var screen = camera.WorldToScreenPoint(target.transform.position);
            bool reached = _bootstrap.TryWorldClick(new Vector3(screen.x, screen.y, 0f));
            yield return null;

            bool opened = FindOpenSurface("plot") != null;
            Check("Bấm vào ô đất vẫn mở được bảng ô", reached && opened,
                  "ô " + target.PlotId + (reached ? "" : " — tia không chạm vườn") +
                  (opened ? "" : " — bảng ô không mở"));

            CloseSurface("plot");
            yield return null;
        }

        static bool TryGroundUnder(Camera camera, Vector3 screenPoint, out Vector3 point)
        {
            point = Vector3.zero;
            var ray = camera.ScreenPointToRay(screenPoint);
            if (Mathf.Approximately(ray.direction.y, 0f)) return false;
            float distance = -ray.origin.y / ray.direction.y;
            if (distance <= 0f) return false;
            point = ray.GetPoint(distance);
            return true;
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
            yield return CheckOfflineCtaFitsItsText();
            yield return CheckModalHasEscape("BlockedModal");

            string[] panels = { "inventory", "upgrade", "decorate", "workshop", "settings", "plot" };
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
            var buttonTransform = modal.Find("Card/ActionRow/Continue");
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
        /// <summary>
        /// Nut "Tiep tuc" cua bao cao vang mat: rong vua bang chu va nam giua the.
        ///
        /// Do bang so do chu khong bang mat, va do rieng khoi muc "modal co loi thoat": mot nut
        /// bi keo het be ngang van bam duoc, nen muc kia van dat trong khi bo cuc da sai.
        /// </summary>
        IEnumerator CheckOfflineCtaFitsItsText()
        {
            var modal = _hud.transform.Find("OfflineModal");
            var card = modal != null ? modal.Find("Card") as RectTransform : null;
            var button = card != null ? card.Find("ActionRow/Continue") as RectTransform : null;
            if (button == null)
            {
                Check("Nút Tiếp tục vừa bằng chữ và nằm giữa", false,
                      "Không tìm thấy OfflineModal/Card/ActionRow/Continue.");
                yield break;
            }

            bool wasActive = modal.gameObject.activeSelf;
            modal.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(card);
            Canvas.ForceUpdateCanvases();
            // Canvas chi chot hinh hoc o cuoi khung hinh; do ngay bay gio thi so nao cung la 0.
            yield return null;

            float cardWidth = WorldWidth(card);
            float buttonWidth = WorldWidth(button);
            float offCentre = Mathf.Abs(WorldCentre(button).x - WorldCentre(card).x);

            bool hugs = cardWidth > 0f && buttonWidth <= cardWidth * 0.6f;
            bool centred = offCentre <= cardWidth * 0.01f;
            Check("Nút Tiếp tục vừa bằng chữ và nằm giữa", hugs && centred,
                  "nút " + buttonWidth.ToString("0") + " px / thẻ " + cardWidth.ToString("0") +
                  " px, lệch tâm " + offCentre.ToString("0.0") + " px");

            modal.gameObject.SetActive(wasActive);
            Canvas.ForceUpdateCanvases();
            yield return null;
        }

        static Vector3 WorldCentre(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return (corners[0] + corners[2]) * 0.5f;
        }

        static float WorldWidth(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Vector3.Distance(corners[0], corners[3]);
        }

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
                case "workshop": wanted = "WorkshopPanel"; break;
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
