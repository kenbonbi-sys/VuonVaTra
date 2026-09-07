using System;
using System.Collections.Generic;

namespace VuonNho.Core
{
    /// <summary>
    /// View va log dang ky de nghe. Simulation khong biet gi ve GameObject.
    /// Khi chay bu offline, GameSession tat listener de khong phat lai hang nghin hieu ung.
    /// </summary>
    public interface ISimulationListener
    {
        void OnCropReady(int plotId, string cropId, long atMs);
        void OnHarvested(int plotId, string cropId, int amount, bool byRobot, long atMs);
        void OnPlanted(int plotId, string cropId, long atMs);
        void OnBatchStarted(string recipeId, long atMs);
        void OnBatchCompleted(string recipeId, long coins, long atMs);
        void OnPestAppeared(int plotId, string cropId, long atMs);
        void OnStationStarted(string stageId, string cropId, long atMs);
        void OnStationCompleted(string stageId, string cropId, int amount, long atMs);
        void OnWagesPaid(long coins, int workersPaid, int workersUnpaid, long atMs);

        /// <summary>
        /// Mot su kien kinh doanh: chot ky, tra no, doan kiem tra ghe, giay phep xong, ngan hang
        /// niem phong. Mot ham cho ca nhom vi ca nhom deu di ve cung mot cho — mot cau bao cho
        /// nguoi choi biet vua co chuyen gi — chu khong ai trong so chung can mot hieu ung rieng.
        ///
        /// <paramref name="kind"/> la ma su kien de log doc duoc; <paramref name="detail"/> la
        /// cau **da san sang hien ra man hinh**.
        /// </summary>
        void OnBusinessEvent(string kind, string detail, long coins, long atMs);
    }

    /// <summary>
    /// Mot thuat toan dung cho ca online va offline (muc 9 cua ke hoach).
    /// AdvanceTo luon tien thoi gian ve phia truoc va giai quyet su kien theo deadline chinh xac,
    /// nen do tre tick khong lam troi chu ky.
    /// </summary>
    public sealed class FarmSimulation
    {
        /// <summary>Chan vong lap vo han neu du lieu loi lam chu ky bang 0.</summary>
        public const int MaxEventsPerAdvance = 20000000;

        readonly ContentCatalog _catalog;

        readonly List<ISimulationListener> _listeners = new List<ISimulationListener>();

        /// <summary>Bat khi chay bu offline hoac khi mo phong tren ban sao cua mot transaction.</summary>
        public bool ListenersMuted;

        public void AddListener(ISimulationListener listener)
        {
            if (listener != null && !_listeners.Contains(listener)) _listeners.Add(listener);
        }

        public void RemoveListener(ISimulationListener listener)
        {
            _listeners.Remove(listener);
        }

        public ContentCatalog Catalog { get { return _catalog; } }

        public FarmSimulation(ContentCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException("catalog");
            _catalog = catalog;
        }

        public int GrowthSpeedLevel(GameState state)
        {
            int total = 0;
            for (int i = 0; i < _catalog.Upgrades.Count; i++)
                if (_catalog.Upgrades[i].Kind == UpgradeKind.GrowthSpeed)
                    total += state.UpgradeLevel(_catalog.Upgrades[i].Id);
            return total;
        }

        public int BrewSpeedLevel(GameState state)
        {
            int total = 0;
            for (int i = 0; i < _catalog.Upgrades.Count; i++)
                if (_catalog.Upgrades[i].Kind == UpgradeKind.BrewSpeed)
                    total += state.UpgradeLevel(_catalog.Upgrades[i].Id);
            return total;
        }

        public long GrowthMsFor(GameState state, string cropId)
        {
            return _catalog.ScaleGrowth(_catalog.Crop(cropId).BaseGrowthMs, GrowthSpeedLevel(state));
        }

        public long BrewMsFor(GameState state, string recipeId)
        {
            return _catalog.ScaleBrew(_catalog.Recipe(recipeId).BaseBrewMs, BrewSpeedLevel(state));
        }

        /// <summary>
        /// Tien mo phong den targetSimulationTimeMs. Goi nhieu buoc nho hay mot buoc lon
        /// deu phai cho cung ket qua khi lich lenh giong nhau.
        /// </summary>
        public void AdvanceTo(GameState state, long targetSimulationTimeMs)
        {
            if (state == null) throw new ArgumentNullException("state");
            if (targetSimulationTimeMs < state.SimulationTimeMs) return;

            int guard = 0;
            while (true)
            {
                long nextEventMs;
                if (!TryGetNextEventTime(state, out nextEventMs)) break;
                if (nextEventMs > targetSimulationTimeMs) break;
                if (nextEventMs < state.SimulationTimeMs) nextEventMs = state.SimulationTimeMs;

                ResolveAt(state, nextEventMs);

                if (++guard > MaxEventsPerAdvance)
                    throw new InvalidOperationException("Vuot so su kien cho phep trong mot lan AdvanceTo.");
            }

            state.SimulationTimeMs = targetSimulationTimeMs;

            // Co va do phi khong phai su kien nen khong keo AdvanceTo dung lai, nhung HUD doc
            // chung moi khung hinh. Cong not phan le o day de so tren man hinh khong bao gio cu.
            for (int i = 0; i < state.Plots.Count; i++)
                AdvanceGround(state, state.Plots[i], targetSimulationTimeMs);
        }

        /// <summary>
        /// Giai quyet nhung viec da den han ngay tai thoi diem hien tai. Dung sau khi mot lenh
        /// nguoi choi lam doi kho, cong thuc hoac trang thai robot.
        /// </summary>
        public void ResolveImmediate(GameState state)
        {
            ResolveAt(state, state.SimulationTimeMs);
        }

        /// <summary>
        /// Thu tu co dinh tai mot timestamp: den han -> tra luong -> robot -> may che bien ->
        /// quay tra. Thu tu nay la mot phan cua hop dong, khong phai tinh co: doi cho hai buoc
        /// cho nhau la hai duong chay online va offline se cho ra hai ket qua khac nhau.
        /// </summary>
        void ResolveAt(GameState state, long atMs)
        {
            state.SimulationTimeMs = atMs;

            // 1. Cay den han sang Ready, me may den han hoan thanh va cong xu dung mot lan.
            for (int i = 0; i < state.Plots.Count; i++)
            {
                var plot = state.Plots[i];
                AdvanceGround(state, plot, atMs);
                if (plot.PestPending && !plot.PestActive && plot.PestAtMs <= atMs &&
                    plot.Phase == PlotPhase.Growing)
                {
                    plot.PestActive = true;
                    NotifyPestAppeared(plot.PlotId, plot.CurrentCropId, atMs);
                }
                if (plot.Phase == PlotPhase.Growing && plot.FinishAtMs <= atMs)
                {
                    plot.Phase = PlotPhase.Ready;
                    NotifyCropReady(plot.PlotId, plot.CurrentCropId, atMs);
                }
            }

            if (state.Machine.BatchRunning && state.Machine.BatchFinishAtMs <= atMs)
            {
                long coins = state.Machine.BatchOutputCoins;
                string recipeId = state.Machine.BatchRecipeId;
                CollectOrDefer(state, coins, atMs);
                state.Machine.BatchRunning = false;
                state.Machine.BatchRecipeId = null;
                state.Machine.BatchFromPacked = false;
                state.Machine.BatchOutputCoins = 0;
                state.Machine.BatchStartAtMs = 0;
                state.Machine.BatchFinishAtMs = 0;
                NotifyBatchCompleted(recipeId, coins, atMs);
            }

            // 2. May che bien den han: hang ra vao kho, may tro lai ranh.
            for (int i = 0; i < state.Stations.Count; i++)
            {
                var station = state.Stations[i];
                if (!station.Running || station.BatchFinishAtMs > atMs) continue;

                ProcessStageDefinition stage;
                if (!_catalog.TryGetStage(station.StageId, out stage)) { station.Running = false; continue; }

                string cropId = station.BatchCropId;
                int amount = station.BatchOutput;
                state.AddInventory(ProcessChain.ItemId(cropId, stage.OutputSuffix), amount);
                station.Running = false;
                station.BatchCropId = null;
                station.BatchOutput = 0;
                station.BatchStartAtMs = 0;
                station.BatchFinishAtMs = 0;
                NotifyStationCompleted(stage.Id, cropId, amount, atMs);
            }

            // 3. Tra luong. Truoc khi bat dau me moi: so tho tra duoc luong quyet dinh bao nhieu
            //    may duoc phep chay trong ky sap toi.
            PayWagesIfDue(state, atMs);

            // 3b. Ho so phap ly den han, doan kiem tra ghe, va chot ky tai chinh. Ba viec nay
            //     phai chay **truoc** khi may bat dau me moi: mot lenh dinh chi hay mot lan siet
            //     no ma den sau thi ky do van co mot me tra chay lot vao, va nguoi choi se thay
            //     xuong minh vua bi dong cua lai vua dang san xuat.
            ResolvePermits(state, atMs);
            InspectIfDue(state, atMs);
            CloseCycleIfDue(state, atMs);

            // 4. Robot: toi noi thi thu dung o da nham, roi nham o chin gan nhat con lai.
            //    Nuong che bi niem phong thi robot dung — do la y nghia cua chu "niem phong".
            if (state.RobotUnlocked && !state.Loan.Sealed) ServiceRobot(state, atMs);

            // 5. May che bien ranh va du nguyen lieu thi bat dau me moi, theo thu tu day chuyen.
            //    Uu tien chang dau: mot day chuyen dung o giua thi hang o dau se don lai, con
            //    dung o dau thi ca day chuyen doi — nen dau vao phai duoc chay truoc.
            TryStartStations(state, atMs);

            // 6. Quay tra ranh va du nguyen lieu thi bat dau me moi.
            TryStartBatch(state, atMs);
        }

        /// <summary>
        /// Tru luong cua ky vua qua va chot so tho se chay may trong ky toi.
        ///
        /// Khong du tien thi **khong ai bi mat**, chi la ky nay it nguoi chay may hon. Duoi so am
        /// va sa thai tu dong deu la nhung cach lam nguoi choi mat thu ma ho khong bam vao dau ca.
        /// </summary>
        void PayWagesIfDue(GameState state, long atMs)
        {
            if (state.HiredWorkers <= 0)
            {
                state.StaffedWorkers = 0;
                return;
            }

            long period = _catalog.Balance.PayrollPeriodMs;
            if (state.NextPayrollAtMs <= 0) state.NextPayrollAtMs = atMs + period;
            if (state.NextPayrollAtMs > atMs) return;

            long wage = _catalog.Balance.WorkerWageCoins;
            int paid = state.HiredWorkers;
            if (wage > 0)
            {
                long affordable = state.Coins / wage;
                if (affordable < paid) paid = (int)affordable;
                state.SpendCoins(paid * wage);
            }
            state.StaffedWorkers = paid;
            state.NextPayrollAtMs = atMs + period;
            NotifyWagesPaid(paid * wage, paid, state.HiredWorkers - paid, atMs);
        }

        /// <summary>Ho so dang cho tham dinh da xong chua. Xong thi giay phep co hieu luc ngay.</summary>
        void ResolvePermits(GameState state, long atMs)
        {
            var compliance = state.Compliance;
            if (compliance.PendingEntity != BusinessEntity.None && compliance.EntityReadyAtMs <= atMs)
            {
                var option = Compliance.Entity(compliance.PendingEntity);
                compliance.Entity = compliance.PendingEntity;
                compliance.PendingEntity = BusinessEntity.None;
                compliance.EntityReadyAtMs = 0;
                NotifyBusinessEvent("entity_registered",
                    "Đã đăng ký xong " + (option != null ? option.DisplayName : "hình thức kinh doanh") +
                    ". Từ kỳ này bắt đầu tính thuế.", 0, atMs);
            }

            if (compliance.FoodSafetyPending && compliance.FoodSafetyReadyAtMs <= atMs)
            {
                compliance.FoodSafetyPending = false;
                compliance.FoodSafetyReadyAtMs = 0;
                compliance.FoodSafetyCertified = true;
                NotifyBusinessEvent("food_safety_certified",
                    "Có giấy chứng nhận an toàn thực phẩm. Xưởng được phép hoạt động.", 0, atMs);
            }
        }

        /// <summary>
        /// Doan kiem tra ghe xuong theo ky. Doc trang thai that cua co so roi lap bien ban.
        ///
        /// Duoi mot cai may thi khong kiem tra: mot nguoi moi co bon o dat va chua xay gi ca thi
        /// khong co xuong nao de kiem, va phat ho luc do chi la mot cai bay khong day duoc gi.
        /// </summary>
        void InspectIfDue(GameState state, long atMs)
        {
            var compliance = state.Compliance;
            long period = Finance.CycleMs(_catalog.Balance) * _catalog.Balance.InspectionEveryCycles;
            if (compliance.NextInspectionAtMs <= 0) compliance.NextInspectionAtMs = atMs + period;
            if (compliance.NextInspectionAtMs > atMs) return;
            compliance.NextInspectionAtMs = atMs + period;

            if (!Compliance.HasFactory(state)) return;

            var result = Compliance.Inspect(_catalog, state, atMs);
            compliance.InspectionCount = result.Index;
            compliance.LastInspectionIndex = result.Index;
            compliance.LastFineCoins = result.FineCoins;
            compliance.LastViolationIds = string.Join(",", result.ViolationIds.ToArray());

            if (result.Clean)
            {
                NotifyBusinessEvent("inspection_clean",
                    "Đoàn kiểm tra an toàn thực phẩm ghé xưởng: không có vi phạm nào.", 0, atMs);
                return;
            }

            if (result.LicenceWarningOnly)
            {
                compliance.LicenceWarned = true;
                NotifyBusinessEvent("inspection_warning",
                    "Đoàn kiểm tra nhắc: xưởng chưa có giấy an toàn thực phẩm. Lần này chỉ nhắc, " +
                    "kỳ sau là phạt 20–30 triệu kèm đình chỉ. Xin giấy ở bảng Pháp lý.", 0, atMs);
                return;
            }

            if (result.FineCoins > 0)
            {
                state.SpendCoins(result.FineCoins);
                compliance.TotalFinesCoins += result.FineCoins;
            }
            if (result.Suspended)
                compliance.SuspendedUntilMs = atMs + Finance.CycleMs(_catalog.Balance) *
                                              _catalog.Balance.SuspensionCycles;

            var first = Compliance.Violation(result.ViolationIds[0]);
            string detail = "Biên bản kiểm tra: " + (first != null ? first.DisplayName : "có vi phạm");
            if (result.ViolationIds.Count > 1)
                detail += " và " + (result.ViolationIds.Count - 1) + " lỗi nữa";
            detail += ". Phạt " + result.FineCoins.ToString("N0") + DefaultContent.CoinGlyph;
            if (result.Suspended) detail += ", đình chỉ sản xuất";
            detail += ".";
            NotifyBusinessEvent("inspection_fined", detail, result.FineCoins, atMs);
        }

        /// <summary>
        /// Chot mot ky: thu tien hang ban tra cham, thu tien du lich, dong thue va chi phi ke
        /// toan, tra no ngan hang, roi ghi mot dong vao lich su dong tien.
        ///
        /// Thu tu o day la mot phan cua hop dong. Thu truoc — vi thue tinh tren doanh thu cua ky,
        /// va tien ban tra cham thu duoc trong ky nay la doanh thu cua ky nay. Tra no sau cung —
        /// vi ngan hang la nguoi duoc tra sau khi moi thu khac da tinh xong, va do dung la cho
        /// nguoi choi thay ro nhat vi sao thang nay khong du tien.
        /// </summary>
        void CloseCycleIfDue(GameState state, long atMs)
        {
            long period = Finance.CycleMs(_catalog.Balance);
            if (state.NextCycleCloseAtMs <= 0) state.NextCycleCloseAtMs = atMs + period;
            if (state.NextCycleCloseAtMs > atMs) return;
            state.NextCycleCloseAtMs = atMs + period;
            state.CycleIndex++;

            CollectReceivables(state, atMs);

            long tourism = Branches.FarmstayIncome(_catalog.Balance, state);
            if (tourism > 0)
            {
                state.EarnCoins(tourism);
                NotifyBusinessEvent("farmstay_income",
                    "Tour hái trà và workshop kỳ này thu " + tourism.ToString("N0") +
                    DefaultContent.CoinGlyph + ".", tourism, atMs);
            }

            // Chot doanh thu va chi phi van hanh **truoc** khi cong thue vao chi phi: thue tinh
            // tren hai con so cua ky, khong tinh tren chinh no.
            long revenue = state.CycleRevenueCoins;
            long operatingExpense = state.CycleExpenseCoins;

            long tax = Compliance.TaxFor(state.Compliance.Entity, revenue, operatingExpense);
            var entity = Compliance.Entity(state.Compliance.Entity);
            long bookkeeping = entity == null ? 0 : entity.BookkeepingCoinsPerCycle;
            if (tax + bookkeeping > 0)
            {
                state.SpendCoins(tax + bookkeeping);
                state.Compliance.TotalTaxCoins += tax;
                if (tax > 0)
                    NotifyBusinessEvent("tax_paid",
                        "Nộp thuế kỳ này " + tax.ToString("N0") + DefaultContent.CoinGlyph +
                        (bookkeeping > 0 ? " và " + bookkeeping.ToString("N0") + DefaultContent.CoinGlyph +
                                           " phí kế toán" : "") + ".", tax + bookkeeping, atMs);
            }

            long debtService = ServiceLoan(state, atMs);

            var record = new CashCycleRecord
            {
                Month = state.CycleIndex,
                RevenueCoins = revenue,
                ExpenseCoins = operatingExpense + tax + bookkeeping,
                DebtServiceCoins = debtService,
                DebtRemainingCoins = state.Loan.RemainingPrincipalCoins + state.Loan.OverdueCoins,
                Shortfall = state.Loan.ConsecutiveShortfalls > 0
            };
            record.NetCashCoins = record.RevenueCoins - record.ExpenseCoins - record.DebtServiceCoins;
            state.CashHistory.Add(record);
            while (state.CashHistory.Count > _catalog.Balance.CashHistoryCycles)
                state.CashHistory.RemoveAt(0);

            state.CycleRevenueCoins = 0;
            state.CycleExpenseCoins = 0;
        }

        /// <summary>Tien hang ban tra cham da den han thi ve tui. Kenh ban le thu cong sinh ra chung.</summary>
        void CollectReceivables(GameState state, long atMs)
        {
            long collected = 0;
            for (int i = state.Receivables.Count - 1; i >= 0; i--)
            {
                if (state.Receivables[i].DueAtMs > atMs) continue;
                collected += state.Receivables[i].AmountCoins;
                state.Receivables.RemoveAt(i);
            }
            if (collected <= 0) return;
            state.EarnCoins(collected);
            NotifyBusinessEvent("receivables_collected",
                "Khách bán lẻ đã thanh toán " + collected.ToString("N0") + DefaultContent.CoinGlyph + ".",
                collected, atMs);
        }

        /// <summary>
        /// Tra nghia vu cua mot ky. Tra du thi lich tien mot buoc; khong du thi lich **dung yen**
        /// va phan lai chua tra cong don thanh no qua han.
        ///
        /// Goc chua tra khong bi cong hai lan: no van nam nguyen trong du no, chi la ky nay khong
        /// giam duoc. Nen thieu tien lam khoan vay **dai ra** chu khong lam no **phinh ra** — dung
        /// mot cai gia, khong phai hai.
        /// </summary>
        long ServiceLoan(GameState state, long atMs)
        {
            var loan = state.Loan;
            if (!loan.Active && loan.OverdueCoins <= 0) return 0;

            long interest = Finance.InterestDue(loan.RemainingPrincipalCoins, loan.AnnualRateBps);
            long principal = Finance.PrincipalDue(loan.PrincipalCoins, loan.TermMonths, loan.MonthsPaid);
            if (principal > loan.RemainingPrincipalCoins) principal = loan.RemainingPrincipalCoins;

            long carried = loan.OverdueCoins;
            long due = carried + interest + principal;
            if (due <= 0) return 0;

            long paid = state.Coins < due ? state.Coins : due;
            if (paid > 0) state.SpendCoins(paid);

            long left = paid;
            long payCarried = left < carried ? left : carried;
            loan.OverdueCoins -= payCarried;
            left -= payCarried;

            long payInterest = left < interest ? left : interest;
            loan.InterestPaidCoins += payInterest;
            loan.OverdueCoins += interest - payInterest;
            left -= payInterest;

            long payPrincipal = left < principal ? left : principal;
            loan.RemainingPrincipalCoins -= payPrincipal;

            if (paid >= due)
            {
                loan.MonthsPaid++;
                loan.ConsecutiveShortfalls = 0;
            }
            else
            {
                loan.ConsecutiveShortfalls++;
            }
            loan.NextDueAtMs = atMs + Finance.CycleMs(_catalog.Balance);

            if (loan.RemainingPrincipalCoins <= 0 && loan.OverdueCoins <= 0)
            {
                loan.Kind = LoanKind.None;
                loan.PrincipalCoins = 0;
                loan.RemainingPrincipalCoins = 0;
                loan.TermMonths = 0;
                loan.MonthsPaid = 0;
                loan.AnnualRateBps = 0;
                loan.NextDueAtMs = 0;
                loan.ConsecutiveShortfalls = 0;
                NotifyBusinessEvent("loan_cleared", "Đã trả xong nợ ngân hàng. Nương chè là của mình.",
                                    paid, atMs);
                return paid;
            }

            if (!loan.Sealed && loan.ConsecutiveShortfalls >= _catalog.Balance.LoanSealShortfalls)
            {
                loan.Sealed = true;
                loan.SealedAtMs = atMs;
                NotifyBusinessEvent("loan_sealed",
                    "Ba kỳ liền không trả đủ nợ. Ngân hàng siết nợ và niêm phong nương chè: " +
                    "trả hết " + Finance.PayoffAmount(loan).ToString("N0") + DefaultContent.CoinGlyph +
                    " mới mở lại được.", 0, atMs);
                return paid;
            }

            if (paid < due)
            {
                NotifyBusinessEvent("loan_shortfall",
                    "Kỳ này chỉ trả được " + paid.ToString("N0") + DefaultContent.CoinGlyph + " trên " +
                    due.ToString("N0") + DefaultContent.CoinGlyph + " phải trả. Thiếu " +
                    loan.ConsecutiveShortfalls + " kỳ liền — đủ " + _catalog.Balance.LoanSealShortfalls +
                    " kỳ là bị siết nợ.", paid, atMs);
            }
            else
            {
                NotifyBusinessEvent("loan_paid",
                    "Trả nợ kỳ " + loan.MonthsPaid + "/" + loan.TermMonths + ": " +
                    paid.ToString("N0") + DefaultContent.CoinGlyph + " (lãi " +
                    payInterest.ToString("N0") + DefaultContent.CoinGlyph + ").", paid, atMs);
            }
            return paid;
        }

        void NotifyBusinessEvent(string kind, string detail, long coins, long atMs)
        {
            if (ListenersMuted) return;
            for (int i = 0; i < _listeners.Count; i++) _listeners[i].OnBusinessEvent(kind, detail, coins, atMs);
        }

        /// <summary>
        /// Xuong co duoc phep chay khong. Bi dinh chi hay bi niem phong thi khong.
        ///
        /// Hai cai nay khac nhau: **dinh chi** la lenh cua doan kiem tra, chi dong xuong, vuon van
        /// lon binh thuong. **Niem phong** la ngan hang siet no, dong ca vuon. Nen o day tra loi
        /// cho xuong, con vuon co mot cau tra loi rieng.
        /// </summary>
        public bool FactoryAllowed(GameState state, long atMs)
        {
            return !state.Loan.Sealed && !state.Compliance.Suspended(atMs);
        }

        /// <summary>
        /// Bat dau nhung me che bien co the bat dau. So may chay cung luc khong vuot qua so tho
        /// tra duoc luong — do la cho "thieu nguoi" hien ra thanh mot cai may nam khong.
        /// </summary>
        public void TryStartStations(GameState state, long atMs)
        {
            if (!FactoryAllowed(state, atMs)) return;
            int slots = state.StaffedWorkers * Workforce.StationsPerWorker - state.RunningStationCount();
            if (slots <= 0) return;

            for (int i = 0; i < _catalog.Stages.Count && slots > 0; i++)
            {
                var stage = _catalog.Stages[i];
                var station = state.Station(stage.Id);
                if (station == null || !station.Owned || station.Running) continue;

                string cropId = ChooseCropFor(state, stage);
                if (cropId == null) continue;

                state.AddInventory(ProcessChain.ItemId(cropId, stage.InputSuffix), -stage.InputCount);
                station.Running = true;
                station.BatchCropId = cropId;
                station.BatchOutput = stage.OutputCount;
                station.BatchStartAtMs = atMs;
                station.BatchFinishAtMs = atMs + ProcessMsFor(state, stage.Id);
                slots--;
                NotifyStationStarted(stage.Id, cropId, atMs);
            }
        }

        /// <summary>
        /// Cay nao dang don nhieu nhat o dau vao thi che bien cay do. Bang nhau thi lay cay dung
        /// truoc trong catalog — hoa co dinh nen chay lai cung mot lich cho cung mot ket qua.
        ///
        /// Chon tu dong chu khong bat nguoi choi chon tung may: sau cai may nhan mot lua chon la
        /// sau lan bam moi khi doi cay, ma quyet dinh do gan nhu luon la "cai nao dang nhieu nhat".
        /// </summary>
        public string ChooseCropFor(GameState state, ProcessStageDefinition stage)
        {
            string best = null;
            long bestStock = 0;
            for (int i = 0; i < _catalog.Crops.Count; i++)
            {
                string cropId = _catalog.Crops[i].Id;
                long stock = state.InventoryOf(ProcessChain.ItemId(cropId, stage.InputSuffix));
                if (stock < stage.InputCount) continue;
                if (best == null || stock > bestStock) { best = cropId; bestStock = stock; }
            }
            return best;
        }

        public long ProcessMsFor(GameState state, string stageId)
        {
            // Nang cap toc do may ap dung cho ca day chuyen: mot bac "may nhanh hon" ma chi nhanh
            // moi quay tra thi ve sau se thanh vo nghia.
            return _catalog.ScaleBrew(_catalog.Stage(stageId).BaseProcessMs, BrewSpeedLevel(state));
        }

        public bool TryGetNextEventTime(GameState state, out long nextEventMs)
        {
            bool found = false;
            long best = 0;

            for (int i = 0; i < state.Plots.Count; i++)
            {
                var plot = state.Plots[i];
                if (plot.Phase != PlotPhase.Growing) continue;
                if (!found || plot.FinishAtMs < best) { best = plot.FinishAtMs; found = true; }
                // Luc sau benh lo ra cung la mot deadline: khong co no thi mot vuon khong co gi
                // khac dang chay se bo qua ca dot sau benh khi chay bu offline.
                if (plot.PestPending && !plot.PestActive && (!found || plot.PestAtMs < best))
                {
                    best = plot.PestAtMs;
                    found = true;
                }
            }

            if (state.Machine.BatchRunning)
            {
                if (!found || state.Machine.BatchFinishAtMs < best)
                {
                    best = state.Machine.BatchFinishAtMs;
                    found = true;
                }
            }

            for (int i = 0; i < state.Stations.Count; i++)
            {
                var station = state.Stations[i];
                if (!station.Running) continue;
                if (!found || station.BatchFinishAtMs < best)
                {
                    best = station.BatchFinishAtMs;
                    found = true;
                }
            }

            if (state.HiredWorkers > 0 && state.NextPayrollAtMs > 0)
            {
                if (!found || state.NextPayrollAtMs < best)
                {
                    best = state.NextPayrollAtMs;
                    found = true;
                }
            }

            // Chot ky tai chinh, lan kiem tra ke tiep va hai moc tham dinh ho so deu la deadline
            // nhu moi deadline khac. Thieu chung thi mot vuon khong gieo gi se ngu qua ca ky tra
            // no khi chay bu offline, roi tinh dot mot phat ba ky thieu tien luc quay lai.
            if (state.NextCycleCloseAtMs > 0 && (!found || state.NextCycleCloseAtMs < best))
            {
                best = state.NextCycleCloseAtMs;
                found = true;
            }
            if (state.Compliance.NextInspectionAtMs > 0 &&
                (!found || state.Compliance.NextInspectionAtMs < best))
            {
                best = state.Compliance.NextInspectionAtMs;
                found = true;
            }
            if (state.Compliance.PendingEntity != BusinessEntity.None &&
                (!found || state.Compliance.EntityReadyAtMs < best))
            {
                best = state.Compliance.EntityReadyAtMs;
                found = true;
            }
            if (state.Compliance.FoodSafetyPending &&
                (!found || state.Compliance.FoodSafetyReadyAtMs < best))
            {
                best = state.Compliance.FoodSafetyReadyAtMs;
                found = true;
            }

            // Luc lenh dinh chi het hieu luc cung la mot deadline: het dinh chi thi xuong duoc chay
            // lai ngay, khong phai doi cho toi khi co chuyen gi khac xay ra.
            if (state.Compliance.SuspendedUntilMs > state.SimulationTimeMs &&
                (!found || state.Compliance.SuspendedUntilMs < best))
            {
                best = state.Compliance.SuspendedUntilMs;
                found = true;
            }

            if (state.RobotUnlocked && !state.Loan.Sealed)
            {
                if (state.RobotTargetPlotId >= 0)
                {
                    // Dang tren duong: moc toi noi la mot deadline nhu moi deadline khac, nen chay
                    // bu offline khong can luat rieng — no chi la nhieu buoc AdvanceTo lien tiep.
                    if (!found || state.RobotReadyAtMs < best)
                    {
                        best = state.RobotReadyAtMs;
                        found = true;
                    }
                }
                else if (NearestReadyPlot(state) != null)
                {
                    // Ranh ma co o dang chin: nham ngay bay gio. Nham xong thi RobotReadyAtMs luon
                    // lon hon bay gio (RobotHarvestMs duong), nen vong su kien khong quay tai cho.
                    long now = state.SimulationTimeMs;
                    if (!found || now < best) { best = now; found = true; }
                }
            }

            // Mot cai may dang ranh ma da du nguyen lieu va du tho cung la su kien tuc thi. Dieu
            // kien phai chat dung bang dieu kien de bat dau me: rong hon mot chut la vong lap su
            // kien quay mai o cung mot moc thoi gian.
            if (CanStartAnyStation(state) || CanStartBatchNow(state))
            {
                long now = state.SimulationTimeMs;
                if (!found || now < best) { best = now; found = true; }
            }

            nextEventMs = best;
            return found;
        }

        /// <summary>
        /// Mot nhip cua robot. No khong con thu sach moi o chin trong cung mot khoanh khac: no
        /// di toi tung o, thu, roi moi nham o tiep theo.
        ///
        /// Cay chin **nam cho** cho toi khi robot toi noi — do la y nghia cua viec robot co mat
        /// trong vuon. Ca quang di lan luc dung lai thu deu la deadline, nen mot buoc AdvanceTo
        /// dai bang nhieu buoc ngan cong lai, va quang vang mat cung ra dung ket qua do.
        /// </summary>
        public void ServiceRobot(GameState state, long atMs)
        {
            if (state.RobotTargetPlotId >= 0 && state.RobotReadyAtMs <= atMs)
            {
                var arrived = state.Plot(state.RobotTargetPlotId);
                state.RobotXMm = PlotXMm(state.RobotTargetPlotId);
                state.RobotZMm = PlotZMm(state.RobotTargetPlotId);
                state.RobotTargetPlotId = -1;

                // O co the da duoc nguoi choi thu bang tay trong luc robot dang di. Chuyen di do
                // coi nhu bo, khong bu lai gi — robot khong biet truoc thi nguoi choi cung vay.
                if (arrived != null && arrived.Phase == PlotPhase.Ready)
                    HarvestAndReplant(state, arrived, atMs, true);
            }

            if (state.RobotTargetPlotId >= 0) return;

            var next = NearestReadyPlot(state);
            if (next == null) return;

            state.RobotTargetPlotId = next.PlotId;
            state.RobotReadyAtMs = atMs + TravelMsTo(state, next.PlotId) + _catalog.Balance.RobotHarvestMs;
        }

        /// <summary>
        /// O chin gan cho robot nhat. Bang nhau thi lay o co plotId nho hon — hoa co dinh, nen
        /// chay lai cung mot lich cho ra cung mot ket qua.
        /// </summary>
        public PlotState NearestReadyPlot(GameState state)
        {
            PlotState best = null;
            long bestDistance = 0;
            for (int i = 0; i < state.Plots.Count; i++)
            {
                var plot = state.Plots[i];
                if (plot.Phase != PlotPhase.Ready) continue;

                long dx = PlotXMm(plot.PlotId) - state.RobotXMm;
                long dz = PlotZMm(plot.PlotId) - state.RobotZMm;
                long distance = dx * dx + dz * dz;
                if (best == null || distance < bestDistance ||
                    (distance == bestDistance && plot.PlotId < best.PlotId))
                {
                    best = plot;
                    bestDistance = distance;
                }
            }
            return best;
        }

        /// <summary>Quang duong tu cho robot dang dung toi mot o, quy ra millisecond.</summary>
        public long TravelMsTo(GameState state, int plotId)
        {
            long dx = PlotXMm(plotId) - state.RobotXMm;
            long dz = PlotZMm(plotId) - state.RobotZMm;
            return IntegerSqrt(dx * dx + dz * dz) * 1000L / _catalog.Balance.RobotSpeedMmPerSecond;
        }

        public int PlotXMm(int plotId)
        {
            return GardenLayout.PlotCenterXMm(_catalog.Balance, plotId % _catalog.Balance.GardenColumns);
        }

        public int PlotZMm(int plotId)
        {
            return GardenLayout.PlotCenterZMm(_catalog.Balance, plotId / _catalog.Balance.GardenColumns);
        }

        /// <summary>
        /// Can bac hai tren so nguyen. Dung so nguyen chu khong dung Math.Sqrt de quang duong cua
        /// robot khong phu thuoc vao cach tung may lam tron so thuc — cung ky luat so nguyen ma
        /// tien, kho va toa do trong game deu theo.
        /// </summary>
        public static long IntegerSqrt(long value)
        {
            if (value <= 0) return 0;
            long root = value;
            long next = (root + 1) / 2;
            while (next < root)
            {
                root = next;
                next = (root + value / root) / 2;
            }
            return root;
        }

        /// <summary>Thu thu cong va thu bang robot chay cung mot quy tac.</summary>
        public bool HarvestAndReplant(GameState state, PlotState plot, long atMs, bool byRobot)
        {
            if (plot == null || plot.Phase != PlotPhase.Ready) return false;

            string harvestedCropId = plot.CurrentCropId;
            // O bi ray xanh chich hut cho ra **mat hang khac**: la ray xanh, khong phai bup che
            // thuong. Doi ten mat hang ngay tai day chu khong doi luc pha tra, vi kho phai la noi
            // nguoi choi nhin thay minh dang co gi — mot dong "la ray xanh 28" trong kho la thu
            // duy nhat noi cho ho biet lan nay duoc mon qua.
            CropDefinition leafhopperCrop;
            if (plot.Leafhopper && harvestedCropId != null &&
                _catalog.TryGetCrop(DefaultContent.CropOrientalBeauty, out leafhopperCrop))
                harvestedCropId = leafhopperCrop.Id;
            // Sau benh khong duoc chua thi vu do mat trang. O van duoc gieo lai binh thuong: mat
            // mot vu da du dau, mat luon cho dat thi nguoi choi khong con duong nao go lai.
            int amount = plot.PestActive ? 0 : plot.PendingYield;
            if (amount > 0 && harvestedCropId != null)
                state.AddInventory(harvestedCropId, amount);
            NotifyHarvested(plot.PlotId, harvestedCropId, amount, byRobot, atMs);

            plot.PestActive = false;
            plot.PestPending = false;
            plot.PestAtMs = 0;
            plot.Leafhopper = false;

            plot.Phase = PlotPhase.Empty;
            plot.CurrentCropId = null;
            plot.PendingYield = 0;
            plot.StartAtMs = 0;
            plot.FinishAtMs = 0;

            // Gieo lai ngay trong cung mot hanh dong neu o da co lua chon cay hop le.
            string nextCropId = plot.NextCropId;
            if (nextCropId != null && state.UnlockedCropIds.Contains(nextCropId))
                Plant(state, plot, nextCropId, atMs);

            return true;
        }

        /// <summary>
        /// Gieo mot vu. Ba con so cua vu — thu bao nhieu, lon bao lau, co dinh sau benh khong —
        /// deu **chot ngay tai day** chu khong tinh lai luc thu.
        ///
        /// Do la mot quyet dinh thiet ke, khong phai cho tien: no bat nguoi choi lo cho manh dat
        /// TRUOC khi gieo. Bon phan giua vu khong cuu duoc vu dang chay, va don co giua vu khong
        /// lam vu do lon nhanh len.
        /// </summary>
        public void Plant(GameState state, PlotState plot, string cropId, long atMs)
        {
            AdvanceGround(state, plot, atMs);

            var crop = _catalog.Crop(cropId);
            var season = Cultivation.SeasonAt(_catalog.Balance, atMs);
            bool inSeason = Cultivation.IsInSeason(crop, season);

            plot.CurrentCropId = cropId;
            plot.NextCropId = cropId;
            plot.Phase = PlotPhase.Growing;
            plot.StartAtMs = atMs;
            plot.CycleIndex++;

            long growthMs = Cultivation.GrowthWithWeeds(_catalog.Balance, GrowthMsFor(state, cropId), plot.Weeds);
            plot.FinishAtMs = atMs + growthMs;
            int yield = Cultivation.YieldFor(_catalog.Balance, crop, plot.Fertility, inSeason);
            // Che khong "trai vu" theo mat na mua nhu tra thao moc: no chiu bang sinh hoa cua tung
            // mua — he thi nhieu bup ma gia thap, xuan thi it bup ma dat, dong thi gan nhu khong
            // co gi. He so nay la nua "so luong" cua bang do; nua "gia" nam o Agronomy.PricePercentFor.
            if (crop.PluckGraded)
                yield = yield * Agronomy.Profile(season).YieldPercent / 100;
            plot.PendingYield = yield < 0 ? 0 : yield;

            plot.Fertility = Cultivation.Clamp(plot.Fertility - _catalog.Balance.PlantFertilityCost, 0, 100);

            // Ray xanh dung truoc sau benh: mot vu duoc ray xanh chich hut la mot vu **khong** bi
            // sau benh. Hai cai do la cung mot con vat, chi khac o muc do — chich hut nhe thi cay
            // tiet linalool va geraniol, nang thi mat trang.
            plot.Leafhopper = crop.PluckGraded &&
                              Agronomy.LeafhopperStrikes(state.PestSeed, plot.PlotId, plot.CycleIndex,
                                                         season, _catalog.Balance.LeafhopperChancePercent);

            plot.PestActive = false;
            plot.PestPending = !plot.Leafhopper &&
                               Cultivation.PestStrikes(state.PestSeed, plot.PlotId, plot.CycleIndex,
                                                       PestChancePercent(state));
            plot.PestAtMs = plot.PestPending
                ? Cultivation.PestAppearsAtMs(state.PestSeed, plot.PlotId, plot.CycleIndex, atMs, growthMs)
                : 0;

            NotifyPlanted(plot.PlotId, cropId, atMs);
        }

        /// <summary>Ty le sau benh sau khi tru phan nang cap phong tru sinh hoc da mua.</summary>
        public int PestChancePercent(GameState state)
        {
            int chance = _catalog.Balance.PestChancePercent;
            for (int i = 0; i < _catalog.Upgrades.Count; i++)
            {
                var upgrade = _catalog.Upgrades[i];
                if (upgrade.Kind != UpgradeKind.PestControl) continue;
                if (state.UpgradeLevel(upgrade.Id) < 1) continue;
                chance = chance * upgrade.IntValue / 100;
            }
            return Cultivation.Clamp(chance, 0, 100);
        }

        /// <summary>
        /// Cong don co dai va do phi tu lan cuoi toi bay gio.
        ///
        /// Cong theo **so buoc tron** roi doi moc, chu khong tick tung giay: bon gio vang mat cung
        /// chi ton dung mot phep chia, va ket qua khong doi du chia lam bao nhieu lan goi.
        /// </summary>
        public void AdvanceGround(GameState state, PlotState plot, long atMs)
        {
            if (plot == null) return;
            if (plot.WeedsUpdatedAtMs > atMs) plot.WeedsUpdatedAtMs = atMs;
            if (plot.FertilityUpdatedAtMs > atMs) plot.FertilityUpdatedAtMs = atMs;

            if (!plot.Unlocked)
            {
                plot.WeedsUpdatedAtMs = atMs;
                plot.FertilityUpdatedAtMs = atMs;
                return;
            }

            long weedSteps = (atMs - plot.WeedsUpdatedAtMs) / _catalog.Balance.WeedGrowthMs;
            if (weedSteps > 0)
            {
                long weeds = plot.Weeds + weedSteps;
                plot.Weeds = weeds > 100 ? 100 : (int)weeds;
                plot.WeedsUpdatedAtMs += weedSteps * _catalog.Balance.WeedGrowthMs;
            }

            long fertilitySteps = (atMs - plot.FertilityUpdatedAtMs) / _catalog.Balance.FertilityRegenMs;
            if (fertilitySteps > 0)
            {
                int cap = _catalog.Balance.NaturalFertilityCap;
                if (plot.Fertility < cap)
                {
                    long grown = plot.Fertility + fertilitySteps;
                    plot.Fertility = grown > cap ? cap : (int)grown;
                }
                plot.FertilityUpdatedAtMs += fertilitySteps * _catalog.Balance.FertilityRegenMs;
            }
        }

        /// <summary>
        /// Tru toan bo nguyen lieu dung luc bat dau me. Xu duoc chot ngay de nang cap
        /// hay doi cong thuc giua chung khong lam thay doi me dang chay.
        /// </summary>
        public bool TryStartBatch(GameState state, long atMs)
        {
            var machine = state.Machine;
            if (machine.BatchRunning) return false;
            if (state.Loan.Sealed) return false;

            RecipeDefinition recipe;
            if (!_catalog.TryGetRecipe(machine.SelectedRecipeId, out recipe)) return false;
            if (!state.UnlockedRecipeIds.Contains(recipe.Id)) return false;

            var sale = ResolveSale(state, recipe, atMs);
            if (sale == null) return false;

            state.AddInventory(sale.ItemId, -sale.Count);
            machine.BatchRunning = true;
            machine.BatchRecipeId = recipe.Id;
            machine.BatchFromPacked = sale.FromPacked;
            machine.BatchStartAtMs = atMs;
            machine.BatchFinishAtMs = atMs + BrewMsFor(state, recipe.Id);
            machine.BatchOutputCoins = sale.Coins;
            NotifyBatchStarted(recipe.Id, atMs);
            return true;
        }

        /// <summary>
        /// Tien cua mot me da ra: vao tui ngay, hay thanh mot khoan phai thu.
        ///
        /// Kenh ban le thu cong tra cham 30–60 ngay. Do la **cai gia cua bien loi nhuan cao**, va
        /// no phai la mot cai gia that: hang ghi nhan ban xong roi ma tien chua ve tay thi ky nay
        /// van co the khong du tra no. Cong ngay vao tui thi bien 165% tro thanh mot mon qua
        /// khong kem dieu kien nao.
        /// </summary>
        void CollectOrDefer(GameState state, long coins, long atMs)
        {
            if (coins <= 0) return;
            var channel = state.UnlockedBranches.Contains(state.SalesChannel)
                ? Branches.Definition(state.SalesChannel) : null;
            int delayDays = channel == null ? 0 : channel.PayoutDelayDays;
            if (delayDays <= 0)
            {
                state.EarnCoins(coins);
                return;
            }
            state.Receivables.Add(new Receivable
            {
                AmountCoins = coins,
                DueAtMs = atMs + delayDays * Compliance.DayMs(_catalog.Balance)
            });
        }

        /// <summary>
        /// Quay tra se ban gi, lay bao nhieu, duoc bao nhieu xu — mot cho duy nhat tra loi.
        ///
        /// Truoc day cau tra loi nay bi chep hai lan: mot ban trong TryStartBatch va mot ban trong
        /// CanStartBatchNow. Hai ban do phai giong nhau **tuyet doi**, khong thi vong su kien se
        /// bao "co viec ngay bay gio" o mot moc ma TryStartBatch tu choi lam, va AdvanceTo se
        /// quay tai cho cho toi khi vuot MaxEventsPerAdvance. Gop lai lam mot la cach duy nhat
        /// giu duoc dieu do khi cong thuc tinh gia dai them ba he so.
        ///
        /// Tra ve null khi khong ban duoc gi.
        /// </summary>
        public CounterSale ResolveSale(GameState state, RecipeDefinition recipe, long atMs)
        {
            if (recipe == null) return null;

            string packedItem = PackedItemFor(recipe);
            string bulkItem = BulkItemFor(recipe);
            var channel = state.UnlockedBranches.Contains(state.SalesChannel)
                ? Branches.Definition(state.SalesChannel) : null;

            CounterSale sale = null;
            if (recipe.PackedInputCount > 0 && packedItem != null &&
                state.InventoryOf(packedItem) >= recipe.PackedInputCount)
            {
                sale = new CounterSale
                {
                    ItemId = packedItem, Count = recipe.PackedInputCount,
                    Coins = recipe.PackedOutputCoins, FromPacked = true
                };
            }
            else if (channel != null && channel.SellsUnpacked && recipe.PackedInputCount > 0 &&
                     bulkItem != null && state.InventoryOf(bulkItem) >= recipe.PackedInputCount)
            {
                // Gia cong tho B2B ban tra **moc**: bo qua chang dong goi, an dung gia cua hang da
                // dong goi roi chiu he so kenh. Do la ly do nhanh nay ton it may nhat — va cung la
                // ly do ai da mua may dong goi thi nen doi sang kenh khac.
                sale = new CounterSale
                {
                    ItemId = bulkItem, Count = recipe.PackedInputCount,
                    Coins = recipe.PackedOutputCoins, FromPacked = true
                };
            }
            else if (state.InventoryOf(recipe.InputCropId) >= recipe.InputCount)
            {
                sale = new CounterSale
                {
                    ItemId = recipe.InputCropId, Count = recipe.InputCount,
                    Coins = recipe.OutputCoins, FromPacked = false
                };
            }
            if (sale == null) return null;

            sale.PricePercent = SalePricePercent(state, recipe, atMs);
            sale.Coins = sale.Coins * sale.PricePercent / 100;
            return sale;
        }

        /// <summary>
        /// He so gia cua mot me: mua vu, roi can lua, roi kenh ban. Ba he nhan lien tiep nhau.
        ///
        /// Chi che chiu hai he dau. Tra thao moc khong co bang sinh hoa nao trong ban mo phong va
        /// khong ai sao diet men mot bong cuc, nen dem hai con so do ap cho chung se la hai con so
        /// tu bay ra.
        /// </summary>
        public int SalePricePercent(GameState state, RecipeDefinition recipe, long atMs)
        {
            int percent = 100;
            if (Agronomy.IsTea(_catalog, recipe.InputCropId))
            {
                var season = Cultivation.SeasonAt(_catalog.Balance, atMs);
                percent = percent * Agronomy.Profile(season).PricePercent / 100;

                bool leafhopperLeaves = recipe.InputCropId == DefaultContent.CropOrientalBeauty;
                var verdict = Crafting.Evaluate(state.Craft, leafhopperLeaves,
                                                _catalog.Balance.PremiumBatchPricePercent,
                                                _catalog.Balance.FlawedBatchPricePercent);
                percent = percent * Crafting.BatchPricePercent(
                    verdict, leafhopperLeaves, _catalog.Balance.OrientalBeautyFallbackPercent) / 100;
            }
            percent = percent * Branches.PricePercentFor(state) / 100;
            return percent < 1 ? 1 : percent;
        }

        /// <summary>Duoi ten cua chang truoc chang dong goi — tra moc, hang cua kenh B2B.</summary>
        public string BulkItemFor(RecipeDefinition recipe)
        {
            if (recipe == null || _catalog.Stages.Count < 2) return null;
            string suffix = _catalog.Stages[_catalog.Stages.Count - 2].OutputSuffix;
            return suffix == null ? null : ProcessChain.ItemId(recipe.InputCropId, suffix);
        }

        /// <summary>
        /// Quay tra uu tien tra da qua het day chuyen. Co du thi pha bac do, khong thi quay ve la
        /// tuoi nhu cu — day chuyen la duong nang thu nhap chu khong phai cai cong chan duong,
        /// nen mot nguoi choi chua xay may nao van ban tra duoc nhu truoc.
        /// </summary>
        public bool HasPackedInput(GameState state, RecipeDefinition recipe)
        {
            if (recipe == null || recipe.PackedInputCount <= 0) return false;
            string itemId = PackedItemFor(recipe);
            return itemId != null && state.InventoryOf(itemId) >= recipe.PackedInputCount;
        }

        public string PackedItemFor(RecipeDefinition recipe)
        {
            string suffix = _catalog.PackedSuffix;
            return suffix == null || recipe == null ? null : ProcessChain.ItemId(recipe.InputCropId, suffix);
        }


        /// <summary>
        /// Quay tra co the bat dau me ngay bay gio khong.
        ///
        /// Truoc day viec bat dau me khong duoc tinh la mot su kien, nen mot kho day nguyen lieu
        /// voi mot cai may dang ranh se nam yen cho toi khi co chuyen gi khac xay ra. Trong van
        /// choi that thi luon co cay dang lon nen khong ai thay, nhung khi chay bu offline voi
        /// vuon khong gieo gi thi quang nghi do bi mat trang.
        /// </summary>
        public bool CanStartBatchNow(GameState state)
        {
            if (state.Machine.BatchRunning) return false;
            if (state.Loan.Sealed) return false;

            RecipeDefinition recipe;
            if (!_catalog.TryGetRecipe(state.Machine.SelectedRecipeId, out recipe)) return false;
            if (!state.UnlockedRecipeIds.Contains(recipe.Id)) return false;

            return ResolveSale(state, recipe, state.SimulationTimeMs) != null;
        }

        /// <summary>Co may nao co the bat dau ngay bay gio khong. Dung chung dieu kien voi TryStartStations.</summary>
        public bool CanStartAnyStation(GameState state)
        {
            // Dieu kien phai **dung bang** dieu kien cua TryStartStations. Rong hon mot chut thi
            // TryGetNextEventTime se bao "co viec ngay bay gio" mai mai o cung mot moc thoi gian.
            if (!FactoryAllowed(state, state.SimulationTimeMs)) return false;
            if (state.StaffedWorkers * Workforce.StationsPerWorker - state.RunningStationCount() <= 0)
                return false;
            for (int i = 0; i < _catalog.Stages.Count; i++)
            {
                var stage = _catalog.Stages[i];
                var station = state.Station(stage.Id);
                if (station == null || !station.Owned || station.Running) continue;
                if (ChooseCropFor(state, stage) != null) return true;
            }
            return false;
        }

        void NotifyPestAppeared(int plotId, string cropId, long atMs)
        {
            if (ListenersMuted) return;
            for (int i = 0; i < _listeners.Count; i++) _listeners[i].OnPestAppeared(plotId, cropId, atMs);
        }

        void NotifyStationStarted(string stageId, string cropId, long atMs)
        {
            if (ListenersMuted) return;
            for (int i = 0; i < _listeners.Count; i++) _listeners[i].OnStationStarted(stageId, cropId, atMs);
        }

        void NotifyStationCompleted(string stageId, string cropId, int amount, long atMs)
        {
            if (ListenersMuted) return;
            for (int i = 0; i < _listeners.Count; i++)
                _listeners[i].OnStationCompleted(stageId, cropId, amount, atMs);
        }

        void NotifyWagesPaid(long coins, int paid, int unpaid, long atMs)
        {
            if (ListenersMuted) return;
            for (int i = 0; i < _listeners.Count; i++) _listeners[i].OnWagesPaid(coins, paid, unpaid, atMs);
        }

        void NotifyCropReady(int plotId, string cropId, long atMs)
        {
            if (ListenersMuted) return;
            for (int i = 0; i < _listeners.Count; i++) _listeners[i].OnCropReady(plotId, cropId, atMs);
        }

        void NotifyHarvested(int plotId, string cropId, int amount, bool byRobot, long atMs)
        {
            if (ListenersMuted) return;
            for (int i = 0; i < _listeners.Count; i++) _listeners[i].OnHarvested(plotId, cropId, amount, byRobot, atMs);
        }

        void NotifyPlanted(int plotId, string cropId, long atMs)
        {
            if (ListenersMuted) return;
            for (int i = 0; i < _listeners.Count; i++) _listeners[i].OnPlanted(plotId, cropId, atMs);
        }

        void NotifyBatchStarted(string recipeId, long atMs)
        {
            if (ListenersMuted) return;
            for (int i = 0; i < _listeners.Count; i++) _listeners[i].OnBatchStarted(recipeId, atMs);
        }

        void NotifyBatchCompleted(string recipeId, long coins, long atMs)
        {
            if (ListenersMuted) return;
            for (int i = 0; i < _listeners.Count; i++) _listeners[i].OnBatchCompleted(recipeId, coins, atMs);
        }

        /// <summary>Ten va so nguyen lieu con thieu de UI giai thich vi sao may dung.</summary>
        public bool TryGetMissingInput(GameState state, out string cropId, out long missing)
        {
            cropId = null;
            missing = 0;
            if (state.Machine.BatchRunning) return false;

            RecipeDefinition recipe;
            if (!_catalog.TryGetRecipe(state.Machine.SelectedRecipeId, out recipe)) return false;

            long have = state.InventoryOf(recipe.InputCropId);
            if (have >= recipe.InputCount) return false;

            cropId = recipe.InputCropId;
            missing = recipe.InputCount - have;
            return true;
        }
    }
}
