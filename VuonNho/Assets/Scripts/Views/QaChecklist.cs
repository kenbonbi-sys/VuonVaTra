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

            yield return CheckPrimaryFarmAction();
            yield return RunPlaythrough();
            yield return RunReloadCheck();
            yield return RunUiChecks();
            yield return RunOfflineAndPerf();
            yield return RunControlChecks();
            yield return RunProcessingChecks();
            yield return CheckWorkshopPresentation();
            yield return CheckRobotHarvestTrip();
            yield return CheckCultivation();
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

        IEnumerator CheckWorkshopPresentation()
        {
            Section("Thợ thường trực và hover máy");
            var crew = _bootstrap.GetComponent<WorkshopCrewView>();
            var hover = _bootstrap.GetComponent<StationHoverController>();
            Check("Có quản lý thợ thường trực và hover máy", crew != null && hover != null,
                  crew == null && hover == null ? "thiếu cả WorkshopCrewView lẫn StationHoverController"
                  : crew == null ? "thiếu WorkshopCrewView"
                  : hover == null ? "thiếu StationHoverController" : null);
            if (crew == null || hover == null) yield break;

            var stations = _bootstrap.Stations;
            if (stations == null || stations.Length == 0)
            {
                Check("Thợ vẫn hiện qua nhiều mẻ làm việc và chờ nguyên liệu", false,
                      "GameBootstrap.Stations chưa được gán.");
                yield break;
            }

            bool hasArmPivots = true;
            string missingPivots = null;
            var poses = new Dictionary<WorkerWorkAnimation, Quaternion>();
            foreach (var station in stations)
            {
                var root = station != null ? station.WorkerRoot : null;
                var work = root != null ? root.GetComponent<WorkerWorkAnimation>() : null;
                bool ok = work != null && work.ArmLeft != null && work.ArmRight != null;
                if (!ok && missingPivots == null)
                    missingPivots = station != null ? station.StageId : "máy không rõ";
                hasArmPivots &= ok;
                if (work != null && work.ArmLeft != null) poses[work] = work.ArmLeft.localRotation;
            }

            // Do qua nhieu me lien tiep: cai loi cu la tho bat/tat theo tung me, nen no chi lo ra
            // khi mot cai may vua chay xong va chua co nguyen lieu cho me sau.
            bool stayedVisible = true;
            int worstVisible = int.MaxValue;
            string vanishedAt = null;
            float maxArmAngle = 0f;
            for (int step = 0; step < 16; step++)
            {
                _session.DebugAdvance(3000);
                yield return new WaitForSeconds(0.12f);

                if (crew.VisibleWorkerCount < worstVisible) worstVisible = crew.VisibleWorkerCount;
                stayedVisible &= crew.VisibleWorkerCount == _session.State.HiredWorkers;
                foreach (var station in stations)
                {
                    var root = station != null ? station.WorkerRoot : null;
                    bool shown = root != null && root.gameObject.activeInHierarchy;
                    if (!shown && vanishedAt == null)
                        vanishedAt = station != null ? station.StageId : "máy không rõ";
                    stayedVisible &= shown;
                }
                foreach (var pose in poses)
                {
                    float angle = Quaternion.Angle(pose.Value, pose.Key.ArmLeft.localRotation);
                    if (angle > maxArmAngle) maxArmAngle = angle;
                }
            }

            Check("Thợ vẫn hiện qua nhiều mẻ làm việc và chờ nguyên liệu", stayedVisible,
                  stayedVisible
                      ? "ít nhất " + worstVisible + "/" + _session.State.HiredWorkers + " thợ luôn có mặt"
                      : "biến mất ở " + (vanishedAt ?? "một lúc nào đó") + ", thấp nhất " + worstVisible +
                        "/" + _session.State.HiredWorkers);
            Check("Model có đủ hai tay xoay tại vai", hasArmPivots,
                  hasArmPivots ? poses.Count + " thợ có ArmLeft và ArmRight"
                               : "thiếu khớp vai ở " + missingPivots);
            Check("Tay thợ chuyển động trong player", maxArmAngle > 5f,
                  "lệch nhiều nhất " + maxArmAngle.ToString("0.0") + "°");

            // Tat Update cua hover de tu dieu khien diem tro; chuot that cua nguoi dung dang o
            // dau khong biet, ma bai nay phai do dung sau cai may chu khong do cho con tro.
            hover.enabled = false;
            int hoverHits = 0;
            int measured = 0;
            bool cardsFit = true;
            float worstOverflow = 0f;
            foreach (var station in stations)
            {
                var collider = station != null ? station.GetComponent<Collider>() : null;
                if (collider == null || _bootstrap.GameCamera == null) continue;
                measured++;

                var screen = _bootstrap.GameCamera.WorldToScreenPoint(collider.bounds.center);
                hover.UpdateHoverAt(new Vector2(screen.x, screen.y));
                yield return null;

                if (hover.TooltipVisible && hover.HoveredStation == station) hoverHits++;
                if (!hover.TooltipVisible) continue;

                var corners = new Vector3[4];
                hover.TooltipRect.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    float outside = Mathf.Max(-corner.x, -corner.y,
                                              corner.x - Screen.width, corner.y - Screen.height);
                    if (outside > worstOverflow) worstOverflow = outside;
                    if (outside > 0.5f) cardsFit = false;
                }
                CheckTextOverflow("hover " + station.StageId, hover.TooltipRect);
            }

            Check("Rê chuột nhận đủ sáu máy và hiện thông tin", measured > 0 && hoverHits == measured,
                  hoverHits + "/" + measured + " máy" +
                  (hoverHits == 0 && !Application.isFocused
                       ? " — cửa sổ không có focus, hover tự tắt; đóng bớt cửa sổ game rồi chạy lại"
                       : ""));
            Check("Bảng hover nằm trong màn hình", cardsFit,
                  cardsFit ? "sát mép nhất còn trong màn hình"
                           : "tràn ra ngoài " + worstOverflow.ToString("0") + " px");

            hover.UpdateHoverAt(new Vector2(30f, Screen.height - 25f));
            yield return null;
            Check("Hover ẩn khi chuột vào HUD", !hover.TooltipVisible && hover.HoveredStation == null,
                  hover.TooltipVisible ? "bảng vẫn hiện khi con trỏ ở trên thanh HUD" : null);

            hover.UpdateHoverAt(new Vector2(-1f, -1f));
            yield return null;
            Check("Hover ẩn khi chuột rời màn hình", !hover.TooltipVisible,
                  hover.TooltipVisible ? "bảng vẫn hiện khi con trỏ ra ngoài cửa sổ" : null);
            hover.enabled = true;
        }

        // ---------------------------------------------------------------- robot di thu

        /// <summary>
        /// Robot di toi tung o de thu. Do trong ban build vi day la cho mo phong va hinh anh phai
        /// khop nhau: state noi robot dang o dau, va cai nguoi choi nhin thay phai dung o do.
        /// </summary>
        IEnumerator CheckRobotHarvestTrip()
        {
            Section("Robot đi thu từng ô");

            if (!_session.State.RobotUnlocked)
            {
                Check("Robot mất thời gian đi tới ô", false, "Chưa mở khoá robot để thử.");
                yield break;
            }

            // Tua toi khi robot dang tren duong toi mot o. Khong doi mai: neu khong bat duoc thi
            // bao la khong do duoc, chu khong bao la hong.
            int guard = 0;
            while (_session.State.RobotTargetPlotId < 0 && guard++ < 400)
            {
                _session.DebugAdvance(500);
                if (guard % 20 == 0) yield return null;
            }

            var state = _session.State;
            if (state.RobotTargetPlotId < 0)
            {
                Check("Robot mất thời gian đi tới ô", false, "Không bắt được lúc robot đang trên đường.");
                yield break;
            }

            long remaining = state.RobotReadyAtMs - state.SimulationTimeMs;
            Check("Robot mất thời gian đi tới ô", remaining > 0,
                  "còn " + remaining + " ms nữa mới tới ô " + state.RobotTargetPlotId);

            int waiting = 0;
            for (int i = 0; i < state.Plots.Count; i++)
                if (state.Plots[i].Phase == PlotPhase.Ready) waiting++;
            Check("Cây chín nằm chờ chứ không bị thu sạch tức thì", waiting > 0,
                  waiting + " ô đang chín chờ robot");

            // Hinh robot phai chay theo mo phong chu khong dung yen mot cho.
            var before = _bootstrap.Helper != null ? _bootstrap.Helper.transform.position : Vector3.zero;
            for (int i = 0; i < 30; i++) yield return null;
            var after = _bootstrap.Helper != null ? _bootstrap.Helper.transform.position : Vector3.zero;
            float moved = Vector3.Distance(before, after);
            Check("Hình robot chạy theo mô phỏng", _bootstrap.Helper != null && moved > 0.01f,
                  _bootstrap.Helper == null ? "GameBootstrap.Helper chưa được gán."
                                            : "đi được " + (moved * 100f).ToString("0") + " cm");

            // Toi noi thi dung lai ngay o do — khong nhay ve cho cu.
            int target = state.RobotTargetPlotId;
            int wait = 0;
            while (state.RobotTargetPlotId == target && wait++ < 200) { _session.DebugAdvance(200); }
            yield return null;

            bool parked = state.RobotXMm == _session.Simulation.PlotXMm(target) &&
                          state.RobotZMm == _session.Simulation.PlotZMm(target);
            Check("Thu xong thì robot đứng lại ngay ô đó", parked,
                  parked ? "ô " + target
                         : "robot ở (" + state.RobotXMm + ", " + state.RobotZMm + "), ô " + target +
                           " ở (" + _session.Simulation.PlotXMm(target) + ", " +
                           _session.Simulation.PlotZMm(target) + ")");
            yield return null;
        }


        // ---------------------------------------------------------------- canh tac

        /// <summary>
        /// Bon he canh tac: do phi, co dai, sau benh, thoi vu.
        ///
        /// EditMode da chung minh tung luat mot. Cho nay do thu khac: nguoi choi co **nhin thay**
        /// va **lam duoc gi** khong — con so co len HUD khong, nut cham soc co doi trang thai
        /// khong, va manh dat trong canh 3D co doi theo khong.
        /// </summary>
        IEnumerator CheckCultivation()
        {
            Section("Canh tác: độ phì, cỏ, sâu bệnh, thời vụ");

            var balance = _session.Catalog.Balance;

            // Moi lenh cua GameSession commit bang cach thay ca GameState bang mot ban sao moi,
            // nen KHONG duoc giu lai bien plot qua mot lenh. Doc lai qua Plot(0) moi lan.
            Plot(0).Weeds = 0;
            Plot(0).Fertility = 100;
            Plot(0).Phase = PlotPhase.Empty;
            Plot(0).CurrentCropId = null;

            // --- do phi tru khi gieo, va tu hoi len den tran tu nhien chu khong hon
            int before = Plot(0).Fertility;
            var planted = _session.Plant(0, DefaultContent.CropMint);
            Check("Gieo một vụ thì trừ độ phì của ô",
                  planted.Success && Plot(0).Fertility == before - balance.PlantFertilityCost,
                  planted.Success ? before + " → " + Plot(0).Fertility + " (trừ " + balance.PlantFertilityCost + ")"
                                  : planted.FailureReason);

            // De o trong truoc khi do phan tu hoi: o dang co cay se duoc thu roi gieo lai giua
            // chung, va moi lan gieo lai tru them do phi — do vao do la do lan hai thu.
            Plot(0).Phase = PlotPhase.Empty;
            Plot(0).CurrentCropId = null;
            Plot(0).NextCropId = null;
            Plot(0).Fertility = 0;
            _session.DebugAdvance(balance.FertilityRegenMs * (balance.NaturalFertilityCap + 20));
            Check("Đất bỏ không tự hồi nhưng dừng ở trần tự nhiên",
                  Plot(0).Fertility == balance.NaturalFertilityCap,
                  Plot(0).Fertility + "/100, trần " + balance.NaturalFertilityCap);

            // --- bon phan: mat xu, dat ve muc tot nhat
            _session.DebugAddCoins(balance.CompostCost);
            long coinsBefore = _session.State.Coins;
            var composted = _session.Compost(0);
            Check("Bón phân đưa đất về mức tốt nhất và trừ đúng tiền",
                  composted.Success && Plot(0).Fertility == balance.CompostFertility &&
                  _session.State.Coins == coinsBefore - balance.CompostCost,
                  composted.Success ? "độ phì " + Plot(0).Fertility + ", còn " + _session.State.Coins + " xu"
                                    : composted.FailureReason);
            Check("Đất đang tốt thì không cho bón thêm", !_session.Compost(0).Success, null);

            // --- co dai moc len, lam cham, va lam co xoa sach
            Plot(0).Weeds = 0;
            _session.DebugAdvance(balance.WeedGrowthMs * 120);
            Check("Bỏ bê một lúc thì cỏ mọc kín ô", Plot(0).Weeds >= 90, Plot(0).Weeds + "/100");

            long clean = _session.Simulation.GrowthMsFor(_session.State, DefaultContent.CropMint);
            long weedy = Cultivation.GrowthWithWeeds(balance, clean, Plot(0).Weeds);
            Check("Cỏ dại làm cây lớn chậm hơn hẳn", weedy > clean, clean + " ms → " + weedy + " ms");

            var weeded = _session.ClearWeeds(0);
            Check("Làm cỏ xoá sạch cỏ của ô", weeded.Success && Plot(0).Weeds == 0,
                  weeded.Success ? Plot(0).Weeds + "/100" : weeded.FailureReason);

            // --- sau benh: tri duoc, va o sach thi khong cho tri
            Plot(0).PestActive = true;
            _session.DebugAddCoins(balance.PestTreatmentCost);
            coinsBefore = _session.State.Coins;
            var treated = _session.TreatPest(0);
            Check("Trị được sâu bệnh và trừ đúng tiền",
                  treated.Success && !Plot(0).PestActive &&
                  _session.State.Coins == coinsBefore - balance.PestTreatmentCost,
                  treated.Success ? "còn " + _session.State.Coins + " xu" : treated.FailureReason);
            Check("Ô không có sâu thì không cho trị", !_session.TreatPest(0).Success, null);

            // --- thoi vu: cay trai vu thu it hon han
            var jasmine = _session.Catalog.Crop(DefaultContent.CropJasmine);
            int inSeason = Cultivation.YieldFor(balance, jasmine, 100, true);
            int offSeason = Cultivation.YieldFor(balance, jasmine, 100, false);
            Check("Trồng trái vụ thu ít hơn trồng đúng vụ", offSeason < inSeason,
                  inSeason + " → " + offSeason + " đơn vị");
            Check("Mùa suy ra được từ đồng hồ mô phỏng", true,
                  "đang là mùa " +
                  GameHud.SeasonName(Cultivation.SeasonAt(balance, _session.State.SimulationTimeMs)));

            // --- HUD: popup o phai noi ra ca bon con so va cho bam duoc
            _hud.OpenPlotPopup(0);
            yield return null;
            yield return null;

            var popup = _hud.transform.Find("PlotPopup");
            var groundText = popup != null ? TextAt(popup, "Ground") : null;
            Check("Popup ô hiện độ phì, cỏ và mùa",
                  groundText != null && groundText.text.Contains("Độ phì") &&
                  groundText.text.Contains("cỏ") && groundText.text.Contains("mùa"),
                  groundText != null ? groundText.text : "không tìm thấy dòng tình trạng đất");

            var careRow = popup != null ? popup.Find("CareRow") : null;
            bool hasCareButtons = careRow != null && careRow.Find("Weed") != null &&
                                  careRow.Find("Compost") != null && popup.Find("Treat") != null;
            Check("Popup ô có đủ ba nút chăm sóc", hasCareButtons, null);

            // O sach sau thi nut tri sau bien mat han, de popup khong cao them mot hang vo ich.
            var treat = popup != null ? popup.Find("Treat") : null;
            Check("Ô sạch sâu bệnh thì không có nút trị sâu",
                  treat != null && !treat.gameObject.activeSelf,
                  treat == null ? "không tìm thấy nút" : "activeSelf = " + treat.gameObject.activeSelf);

            // Co sau thi nut phai hien ra ngay, chu khong doi mo lai popup.
            Plot(0).PestActive = true;
            _hud.Refresh();
            yield return null;
            Check("Ô có sâu bệnh thì nút trị sâu hiện ra",
                  treat != null && treat.gameObject.activeSelf, null);
            Plot(0).PestActive = false;
            _hud.Refresh();
            yield return null;

            if (popup != null) CheckTextOverflow("popup ô", popup as RectTransform);
            _hud.ClosePlotPopup();
            yield return null;

            // --- mua hien tai trong the ho so noi
            var seasonText = TextAt(_hud.transform, "FarmHud/Profile/Season");
            string currentSeason = GameHud.SeasonName(Cultivation.SeasonAt(balance, _session.State.SimulationTimeMs));
            Check("Thẻ hồ sơ hiện đúng mùa hiện tại và đồng hồ mùa",
                  seasonText != null && seasonText.text.StartsWith("Mùa " + currentSeason) &&
                  seasonText.text.Contains(":"),
                  seasonText != null ? seasonText.text : "không tìm thấy mùa trong thẻ hồ sơ");

            // --- canh 3D: co dai va sau benh phai nhin thay duoc tren o dat
            var view = PlotViewFor(0);
            if (view == null)
            {
                Check("Cỏ dại và sâu bệnh hiện ra trên ô đất", false, "Không tìm thấy PlotView của ô 1.");
                yield break;
            }

            Plot(0).Weeds = 100;
            Plot(0).PestActive = true;
            yield return null;
            yield return null;
            bool weedShown = view.WeedTufts != null && view.WeedTufts.gameObject.activeSelf;
            bool pestShown = view.PestBadge != null && view.PestBadge.activeSelf;
            Check("Ô đầy cỏ và có sâu thì thấy được ngay trong vườn", weedShown && pestShown,
                  "cỏ " + weedShown + ", sâu " + pestShown);

            Plot(0).Weeds = 0;
            Plot(0).PestActive = false;
            yield return null;
            yield return null;
            Check("Làm cỏ trị sâu xong thì dấu hiệu biến mất",
                  (view.WeedTufts == null || !view.WeedTufts.gameObject.activeSelf) &&
                  (view.PestBadge == null || !view.PestBadge.activeSelf), null);
            yield return null;
        }

        /// <summary>O dat doc lai tu phien hien tai: lenh nao commit cung thay ca GameState.</summary>
        PlotState Plot(int plotId)
        {
            return _session.State.Plot(plotId);
        }

        static Text TextAt(Transform root, string path)
        {
            var found = root.Find(path);
            return found != null ? found.GetComponent<Text>() : null;
        }

        PlotView PlotViewFor(int plotId)
        {
            if (_bootstrap == null || _bootstrap.Plots == null) return null;
            for (int i = 0; i < _bootstrap.Plots.Length; i++)
                if (_bootstrap.Plots[i] != null && _bootstrap.Plots[i].PlotId == plotId)
                    return _bootstrap.Plots[i];
            return null;
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
            bool routed = _bootstrap.TryWalkCommand(new Vector3(groundScreen.x, groundScreen.y, 0f), true);
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

            Vector3 spot;
            if (!TryFindFreeSpot(home, DefaultDecorations.Bench, out spot))
            {
                Check("Không đi xuyên qua đồ đã đặt", false,
                      "Không tìm được chỗ trống nào quanh nhân vật để thử.");
                yield break;
            }
            int xMm = Mathf.RoundToInt(spot.x * 1000f);
            int zMm = Mathf.RoundToInt(spot.z * 1000f);

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
            Vector3 spot;
            if (!TryFindFreeSpot(home, DefaultDecorations.Bench, out spot))
            {
                Check("Bóng ma dựng được model của món đang cầm", false,
                      "Không tìm được chỗ trống nào quanh nhân vật để thử.");
                yield break;
            }
            // Do NGAY, khong yield: UpdatePlacementPreview chay moi khung hinh va an bong ma di
            // khi con tro that khong nam tren dat trong. Cho mot khung hinh la do trung cai khac.
            preview.ShowAt(spot, true);
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
            _bootstrap.TryWorldClick(new Vector3(screen.x, screen.y, 0f), true);
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

        /// <summary>
        /// Mot cho trong quanh nhan vat de thu dat do.
        ///
        /// Do bang chinh luat dat cua Core chu khong chon cung mot diem co dinh: bo cuc vuon doi
        /// la diem cung roi vao vung cam, va bo kiem tra se bao mot loi khong co that — cai hong
        /// luc do la phep do, khong phai tro choi.
        /// </summary>
        bool TryFindFreeSpot(Vector3 near, string definitionId, out Vector3 spot)
        {
            for (float radius = 1.3f; radius <= 4.5f; radius += 1.6f)
            {
                for (int step = 0; step < 8; step++)
                {
                    float angle = step * Mathf.PI * 0.25f;
                    var candidate = near + new Vector3(Mathf.Sin(angle) * radius, 0f,
                                                       Mathf.Cos(angle) * radius);
                    int x = Mathf.RoundToInt(candidate.x * 1000f);
                    int z = Mathf.RoundToInt(candidate.z * 1000f);
                    string reason;
                    if (!_session.CanPlaceDecoration(definitionId, x, z, -1, out reason)) continue;
                    spot = new Vector3(x / 1000f, 0f, z / 1000f);
                    return true;
                }
            }
            spot = near;
            return false;
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
            bool reached = _bootstrap.TryWorldClick(new Vector3(screen.x, screen.y, 0f), true);
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
                DefaultContent.UpgradeJasmine,
                DefaultContent.UpgradePestControl,
                // Nuong che va tay nghe hai bup: bon bac cuoi cua lo trinh, mo dan tu bup xo len
                // tan dinh tra. Do bao ho di cuoi cung vi no chi can den khi da co xuong va co tho.
                DefaultContent.UpgradeTea,
                DefaultContent.UpgradePluckMocCau,
                DefaultContent.UpgradePluckNon,
                DefaultContent.UpgradePluckDinh,
                DefaultContent.UpgradeProtectiveGear
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
            // Cay chi sinh ra tu su kien khong bao gio nam trong danh sach gieo duoc — do la luat,
            // khong phai mot thu con thieu. Dem ca no vao day thi muc kiem nay khong bao gio dat
            // duoc, va mot muc khong bao gio dat duoc thi khong con kiem duoc gi.
            int plantable = 0;
            foreach (var crop in _session.Catalog.Crops)
                if (!crop.EventOnly) plantable++;
            Check("Mở hết mọi loại cây gieo được",
                  _session.State.UnlockedCropIds.Count == plantable,
                  _session.State.UnlockedCropIds.Count + "/" + plantable + " loại");
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
            while (!_session.State.Machine.BatchRunning && settle++ < 600)
            {
                _session.DebugAdvance(1000);
                if (settle % 20 == 0) yield return null;
            }
            yield return null;

            bool anyGrowing = false;
            for (int i = 0; i < _session.State.Plots.Count; i++)
                if (_session.State.Plot(i).Phase == PlotPhase.Growing) anyGrowing = true;
            Check("Có cây đang lớn lúc lưu", anyGrowing, null);
            var brewing = _session.Catalog.Recipe(_session.State.Machine.SelectedRecipeId);
            Check("Máy đang pha lúc lưu", _session.State.Machine.BatchRunning,
                  brewing == null ? "chưa chọn công thức nào"
                                  : "công thức " + brewing.DisplayName + ", kho còn " +
                                    _session.State.InventoryOf(brewing.InputCropId) + "/" +
                                    brewing.InputCount + " nguyên liệu");

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

        IEnumerator CheckPrimaryFarmAction()
        {
            Section("Nút gieo và thu hoạch giữa màn hình");
            DismissOfflinePopupIfOpen();
            _hud.Refresh();
            yield return null;

            var actionTransform = _hud.transform.Find("FarmHud/ActionDock/FarmActionButton");
            var action = actionTransform != null ? actionTransform.GetComponent<Button>() : null;
            string detail;
            bool opened = TryClickButton(action, out detail);
            yield return null;
            var popup = FindOpenSurface("plot");
            Check("Bấm Gieo hạt ở vườn mới mở bảng chọn cây",
                  opened && popup != null && Plot(0).Phase == PlotPhase.Empty, detail);

            // Tim theo duong day du: danh sach cay nam trong mot vung cuon duoc, khong con la con
            // truc tiep cua popup. Find khong tim xuong sau nen duong phai ghi ra het.
            var cropTransform = popup != null
                ? popup.Find("CropViewport/CropList/Crop_" + DefaultContent.CropMint) : null;
            var cropButton = cropTransform != null ? cropTransform.GetComponent<Button>() : null;
            bool planted = TryClickButton(cropButton, out detail);
            planted &= Plot(0).Phase == PlotPhase.Growing && Plot(0).CurrentCropId == DefaultContent.CropMint;
            Check("Chọn Bạc hà từ nút gieo bắt đầu một vụ thật", planted, detail);
            CloseSurface("plot");

            if (planted)
            {
                // Dung vu vua gieo, chua co robot, de thao tac UI la nguon thu hoach duy nhat.
                _session.DebugAdvance(Math.Max(1L, Plot(0).FinishAtMs - _session.State.SimulationTimeMs));

                // Sau benh lam vu nay mat trang, va hat giong sau benh lay tu dong ho that nen muc
                // kiem nay se hong khoang mot phan nam so lan chay. Mot muc kiem khong dung tin
                // duoc thi lam ca bo bao cao mat gia tri.
                //
                // Bo qua nhung vu dinh sau benh cho toi khi gap mot vu sach: lich sau benh bam tu
                // (hat giong, id o, so thu tu vu) nen doi so thu tu vu la doi ket qua. Vu bi bo
                // cho ra dung 0 don vi nen no khong lam lech mot phep do nao ve sau — ke ca moc
                // "du tien mua robot trong 8 phut" o ngay duoi. Thu dang do o day la nut Thu hoach
                // co lay du san luong khong; xac suat sau benh co bai test rieng trong EditMode.
                for (int skip = 0; skip < 8 && Plot(0).PestActive; skip++)
                {
                    _session.HarvestAndReplant(0);
                    if (Plot(0).Phase != PlotPhase.Growing) break;
                    _session.DebugAdvance(Math.Max(1L, Plot(0).FinishAtMs - _session.State.SimulationTimeMs));
                }
                _hud.Refresh();
                yield return null;
                bool ready = Plot(0).Phase == PlotPhase.Ready;
                int cycle = Plot(0).CycleIndex;
                int expectedYield = Plot(0).PendingYield;
                long inventoryBefore = _session.State.InventoryOf(DefaultContent.CropMint);
                bool wasBrewing = _session.State.Machine.BatchRunning;
                bool harvested = TryClickButton(action, out detail);
                long received = _session.State.InventoryOf(DefaultContent.CropMint) - inventoryBefore;
                // Neu balance cho phep pha ngay, nguyen lieu nam trong me van phai duoc tinh.
                var machine = _session.State.Machine;
                if (!wasBrewing && machine.BatchRunning && !machine.BatchFromPacked)
                {
                    var recipe = _session.Catalog.Recipe(machine.BatchRecipeId);
                    if (recipe != null && recipe.InputCropId == DefaultContent.CropMint) received += recipe.InputCount;
                }
                Check("Bấm Thu hoạch thu đủ sản lượng và gieo lại cây đang chọn",
                      ready && harvested && received == expectedYield && expectedYield > 0 &&
                      Plot(0).Phase == PlotPhase.Growing && Plot(0).CycleIndex == cycle + 1 &&
                      Plot(0).CurrentCropId == DefaultContent.CropMint && FindOpenSurface("plot") == null,
                      "sản lượng " + received + "/" + expectedYield + "; " + detail);
            }

            // Tra lai fixture vuon moi de khong thay doi moc thoi gian cua bai choi toi 12 o.
            _bootstrap.ResetProgress();
            _session = _bootstrap.Session;
            yield return null;
        }

        IEnumerator RunUiChecks()
        {
            Section("Giao diện ở " + Screen.width + " × " + Screen.height);

            // Popup cho offline khong bao gio tu dong lai; neu no con mo thi scrim cua no la vat
            // trung raycast tren cung va moi phep do UI phia sau se do nham scrim.
            DismissOfflinePopupIfOpen();
            yield return null;

            CheckFloatingHud();
            CheckGardenClickReachable();
            yield return CheckModalHasEscape("OfflineModal");
            yield return CheckOfflineCtaFitsItsText();
            yield return CheckModalHasEscape("BlockedModal");

            // Nam be mat cuoi mo tu trong so tay chu khong tu mot nut o mep man hinh, nen chung
            // khong co "<Ten>Button" trong FarmHud. Vao thang bang OpenPanelByName va bo qua
            // rieng phep do nut dieu huong; ba phep do con lai — chu tran, click xuyen UI, che
            // the tren cung — moi la thu can o day, va chung la nhung bang nhieu chu nhat game co.
            string[] panels =
            {
                "inventory", "upgrade", "decorate", "workshop", "settings", "journal", "plot",
                "finance", "agronomy", "craft", "legal", "model"
            };
            foreach (string panelName in panels)
            {
                if (panelName == "plot") _hud.OpenPlotPopup(0);
                else if (!HasFarmHudNavButton(panelName)) _hud.OpenPanelByName(panelName);
                else
                {
                    string buttonName = char.ToUpperInvariant(panelName[0]) + panelName.Substring(1) + "Button";
                    var nav = _hud.transform.Find("FarmHud/" + buttonName);
                    string detail;
                    bool clicked = TryClickButton(nav != null ? nav.GetComponent<Button>() : null, out detail);
                    Check("Nút điều hướng mở " + panelName, clicked && FindOpenSurface(panelName) != null, detail);
                }
                yield return null;
                yield return null;

                var opened = FindOpenSurface(panelName);
                Check("Mở được bề mặt " + panelName, opened != null, null);
                if (opened == null) continue;

                CheckTextOverflow(panelName, opened);
                CheckBlocksClicks(panelName, opened);
                // Phai do truoc khi bam nut dong that: sau do be mat khong con tren man hinh nua.
                CheckDoesNotCoverTopCounters(panelName, opened);
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

        /// <summary>
        /// Be mat nay co mot nut dieu huong o mep man hinh khong.
        ///
        /// Co thi phep do phai di qua dung cai nut do — mot be mat mo duoc bang code ma nut cua
        /// no khong nhan click la dung loi ma bo QA nay ton tai de bat.
        /// </summary>
        bool HasFarmHudNavButton(string panelName)
        {
            string buttonName = char.ToUpperInvariant(panelName[0]) + panelName.Substring(1) + "Button";
            return _hud.transform.Find("FarmHud/" + buttonName) != null;
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
                case "journal": wanted = "JournalPanel"; break;
                case "finance": wanted = "FinancePanel"; break;
                case "agronomy": wanted = "AgronomyPanel"; break;
                case "craft": wanted = "CraftPanel"; break;
                case "legal": wanted = "LegalPanel"; break;
                case "model": wanted = "ModelPanel"; break;
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

        void CheckFloatingHud()
        {
            var farmHud = _hud.transform.Find("FarmHud");
            if (farmHud == null)
            {
                Check("HUD nổi hiển thị và các nút bấm tới được", false, "Không tìm thấy FarmHud.");
                return;
            }

            foreach (string name in new[] { "Profile", "FarmStats", "CoinChip", "WorkerChip", "StockChip" })
            {
                var region = farmHud.Find(name) as RectTransform;
                bool visible = region != null && region.gameObject.activeInHierarchy;
                if (visible)
                {
                    var bounds = WorldRect(region);
                    var probe = ProbeClick(ScreenCenterOf(region), false);
                    visible = bounds.xMin >= -0.5f && bounds.yMin >= -0.5f &&
                              bounds.xMax <= Screen.width + 0.5f && bounds.yMax <= Screen.height + 0.5f &&
                              probe.UiTarget != null && probe.UiTarget.transform.IsChildOf(region);
                }
                Check("Thẻ " + name + " nằm trong màn hình và không bị che", visible, null);
            }

            var buttons = farmHud.GetComponentsInChildren<Button>(false);
            var unreachable = new List<string>();
            foreach (var button in buttons)
            {
                string detail;
                if (!ButtonIsReachable(button, out detail)) unreachable.Add(detail);
            }
            Check("Các nút nổi nhận được click khi không mở bảng", buttons.Length > 0 && unreachable.Count == 0,
                  unreachable.Count == 0 ? buttons.Length + " nút" : string.Join("; ", unreachable.ToArray()));
        }

        void CheckDoesNotCoverTopCounters(string surface, RectTransform panel)
        {
            var covered = new List<string>();
            // Bang ben duoc phep de len the kho thap hon, nhung ho so va hai so tren cung phai con ro.
            foreach (string name in new[] { "Profile", "CoinChip", "WorkerChip" })
            {
                var region = _hud.transform.Find("FarmHud/" + name) as RectTransform;
                if (region == null || !region.gameObject.activeInHierarchy) covered.Add(name + " không hiển thị");
                else if (Overlaps(panel, region)) covered.Add(name);
            }

            Check(surface + " không che hồ sơ, xu và số thợ", covered.Count == 0,
                  covered.Count == 0 ? "ba thẻ trên cùng vẫn nhìn thấy" : string.Join(", ", covered.ToArray()));
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
