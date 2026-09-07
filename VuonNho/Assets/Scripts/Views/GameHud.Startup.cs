using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VuonNho.Core;

namespace VuonNho.Views
{
    /// <summary>
    /// Nam bang cua ban mo phong khoi nghiep tra: von va no, nong hoc, can lua, phap ly, mo hinh
    /// kinh doanh.
    ///
    /// Hai nguyen tac cua ca nam:
    ///
    /// **Moi con so hien ra deu doc tu GameState hoac tu mot ham thuan tuy trong Core.** Khong
    /// mot con so nao duoc go tay o day. Cho nao la bang tham chieu — bang sinh hoa bon mua, bang
    /// phan hang thu hai, khung tien phat — thi lay tu chinh du lieu ma mo phong dung, nen doi can
    /// bang o Core la bang trong so tay doi theo, khong lech.
    ///
    /// **Dung component cua <see cref="UiKit"/>, khong dung khoi chu.** Ban dau nam bang nay deu
    /// la nhung khoi chu nhieu dong. Doc duoc, nhung doc nhu mot trang tai lieu: mat khong bat
    /// duoc con so nao truoc, khong biet cai nao dang tot cai nao dang hong. Gio moi con so la mot
    /// hang co nhan trai gia tri phai, moi trang thai la mot the mang mau cua no, va moi thong so
    /// co thang do rieng voi khoang chuan ve san tren nen.
    ///
    /// Khong dung slider: cac nut "− gia tri +" doc duoc trong nen art nay va khong can thanh keo
    /// moi. Moi buoc nhay la mot lenh qua GameSession nhu moi lenh khac.
    /// </summary>
    public sealed partial class GameHud
    {
        GameObject _financePanel, _agronomyPanel, _craftPanel, _legalPanel, _modelPanel;

        // Von va no
        Text _loanStateBadge, _loanHint;
        UiKit.Row _loanDebtRow, _loanRateRow, _loanTermRow, _loanDueRow, _loanOverdueRow;
        UiKit.Row _quoteRateRow, _quotePrincipalRow, _quoteInterestRow, _quotePmtRow;
        Text _loanAmountLabel, _loanTermLabel;
        Button _borrowButton, _repayButton;
        readonly List<UiKit.Gauge> _cashRows = new List<UiKit.Gauge>();
        Text _cashEmptyNote;
        long _loanAmountCoins = 1000;
        int _loanTermMonths = 24;
        LoanKind _loanKind = LoanKind.Unsecured;
        readonly List<Button> _loanKindButtons = new List<Button>();

        // Nong hoc
        Text _seasonBadge, _leafhopperBadge;
        UiKit.Row _seasonYieldRow, _seasonPriceRow, _seasonFlavourRow;
        UiKit.Gauge _theanineGauge, _polyphenolGauge;

        // Can lua
        Text _craftVerdictBadge, _craftProductName, _craftNote, _craftRouteNote;
        UiKit.Row _craftLiquorRow, _craftAromaRow, _craftStorageRow, _craftPriceRow;
        readonly List<UiKit.Gauge> _craftGauges = new List<UiKit.Gauge>();
        readonly List<Button> _craftRouteButtons = new List<Button>();

        // Phap ly
        UiKit.Row _entityRow, _foodSafetyRow, _gearRow, _oneWayRow, _taxRow, _fineRow, _inspectionRow;
        Text _suspendedBadge;
        Button _hkdButton, _tnhhButton, _foodSafetyButton;

        // Mo hinh kinh doanh
        Text _phaseBadge, _phaseGoal;
        UiKit.Row _tourismRow, _receivableRow;
        readonly List<Button> _branchButtons = new List<Button>();
        readonly List<Text> _branchBadges = new List<Text>();
        readonly List<Button> _channelButtons = new List<Button>();

        void BuildStartupPanels()
        {
            Transform body;

            _financePanel = BuildSidePanel("FinancePanel", "Vốn và nợ", out body);
            BuildFinanceRows(body);

            _agronomyPanel = BuildSidePanel("AgronomyPanel", "Nông học và mùa vụ", out body);
            BuildAgronomyRows(body);

            _craftPanel = BuildSidePanel("CraftPanel", "Kỹ thuật căn lửa", out body);
            BuildCraftRows(body);

            _legalPanel = BuildSidePanel("LegalPanel", "Pháp lý và ATTP", out body);
            BuildLegalRows(body);

            _modelPanel = BuildSidePanel("ModelPanel", "Mô hình kinh doanh", out body);
            BuildModelRows(body);

            _financePanel.SetActive(false);
            _agronomyPanel.SetActive(false);
            _craftPanel.SetActive(false);
            _legalPanel.SetActive(false);
            _modelPanel.SetActive(false);
        }

        // ---------------------------------------------------------------- tien ich dung chung

        string Coins(long amount)
        {
            return amount.ToString("N0") + DefaultContent.CoinGlyph;
        }

        /// <summary>Xu doi ra trieu dong, de doi chieu voi ban mo phong. 10 xu = 1 trieu.</summary>
        string Million(long coins)
        {
            int per = _session.Catalog.Balance.CoinsPerMillionVnd;
            if (per <= 0) return "";
            long whole = coins / per;
            long tenth = (coins % per) * 10 / per;
            return tenth == 0 ? whole + " tr" : whole + "," + tenth + " tr";
        }

        static string Percent(int bps)
        {
            return (bps / 100) + "," + ((bps % 100) / 10) + "%";
        }

        /// <summary>Hang nut chia deu be ngang. Dung cho chon goi vay, chon duong, chon kenh.</summary>
        static GameObject ButtonRow(Transform parent, string name)
        {
            var row = UiFactory.Node(parent, name);
            var list = UiFactory.HorizontalList(row, UiTokens.Space2, UiTokens.NoPadding);
            list.childForceExpandWidth = true;
            row.AddComponent<LayoutElement>().minHeight = UiFactory.ButtonHeight;
            return row;
        }

        /// <summary>Hang "− gia tri +". Hai nut goi lai <paramref name="onStep"/> voi buoc nhay.</summary>
        static Text StepperRow(Transform parent, string name, string caption,
                               Action<int> onStep, int step)
        {
            var row = UiFactory.Node(parent, name);
            var layout = UiFactory.HorizontalList(row, UiTokens.Space2, UiTokens.NoPadding);
            layout.childForceExpandWidth = false;
            layout.childAlignment = TextAnchor.MiddleLeft;
            row.AddComponent<LayoutElement>().minHeight = UiFactory.ButtonHeight;

            var title = UiFactory.Label(row.transform, "Caption", caption, UiTokens.TextMeta,
                                        TextAnchor.MiddleLeft, GardenPalette.TextMuted);
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            SizeStepButton(UiFactory.TextButton(row.transform, "Minus", "−",
                delegate { onStep(-step); }, UiFactory.ButtonStyle.Quiet));

            var value = UiFactory.Label(row.transform, "Value", "", UiTokens.TextNumber,
                                        TextAnchor.MiddleCenter, GardenPalette.TextPrimary, true);
            var valueSize = value.gameObject.AddComponent<LayoutElement>();
            valueSize.preferredWidth = 108f;
            valueSize.minWidth = 108f;
            valueSize.flexibleWidth = 0f;

            SizeStepButton(UiFactory.TextButton(row.transform, "Plus", "+",
                delegate { onStep(step); }, UiFactory.ButtonStyle.Quiet));
            return value;
        }

        /// <summary>
        /// Hang "nhan − +", khong co o gia tri.
        ///
        /// Dung cho cho nao **da co mot cho khac hien gia tri** — bon nut can lua deu co thanh do
        /// ngay ben duoi, va thanh do do da mang ca ten day du lan con so. Giu them mot o gia tri
        /// o hang nut la in con so hai lan, va no an mat 108 px lam cai nhan gay xuong hai dong.
        /// </summary>
        static void StepperButtons(Transform parent, string name, string caption,
                                   Action<int> onStep, int step)
        {
            var row = UiFactory.Node(parent, name);
            var layout = UiFactory.HorizontalList(row, UiTokens.Space2, UiTokens.NoPadding);
            layout.childForceExpandWidth = false;
            layout.childAlignment = TextAnchor.MiddleLeft;
            row.AddComponent<LayoutElement>().minHeight = UiFactory.ButtonHeight;

            var title = UiFactory.Label(row.transform, "Caption", caption, UiTokens.TextBody,
                                        TextAnchor.MiddleLeft, GardenPalette.TextPrimary, true);
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            SizeStepButton(UiFactory.TextButton(row.transform, "Minus", "−",
                delegate { onStep(-step); }, UiFactory.ButtonStyle.Quiet));
            SizeStepButton(UiFactory.TextButton(row.transform, "Plus", "+",
                delegate { onStep(step); }, UiFactory.ButtonStyle.Quiet));
        }

        static void SizeStepButton(Button button)
        {
            var size = button.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = UiFactory.ButtonHeight;
            size.minWidth = UiFactory.ButtonHeight;
            size.flexibleWidth = 0f;
        }

        // ---------------------------------------------------------------- 1. von va no

        void BuildFinanceRows(Transform body)
        {
            var balance = _session.Catalog.Balance;

            UiKit.Section(body, "Tình trạng khoản vay");
            var status = UiKit.Card(body, "LoanStatus");
            _loanStateBadge = UiKit.Badge(status, "LoanState", "Chưa vay", UiTone.Neutral);
            _loanDebtRow = UiKit.StatRow(status, "Debt", "Dư nợ còn lại");
            _loanRateRow = UiKit.StatRow(status, "Rate", "Lãi suất");
            _loanTermRow = UiKit.StatRow(status, "Term", "Đã trả");
            _loanDueRow = UiKit.StatRow(status, "Due", "Nghĩa vụ kỳ tới");
            _loanOverdueRow = UiKit.StatRow(status, "Overdue", "Nợ quá hạn");
            _loanHint = UiKit.Note(status, "Hint", "");
            _repayButton = UiFactory.TextButton(status, "Repay", "Trả hết nợ", delegate
            {
                Run(_session.RepayLoan(Finance.PayoffAmount(_session.State.Loan)));
            });

            UiKit.Section(body, "Cấu trúc vốn mồi");
            var seed = UiKit.Card(body, "SeedCapital");
            UiKit.StatRow(seed, "Own", "Vốn tự có", balance.SeedCapitalMillionVnd + " triệu");
            UiKit.StatRow(seed, "Unsecured", "Hạn mức tín chấp",
                Million(balance.UnsecuredLoanCapCoins) + " · " +
                Percent(balance.UnsecuredMinRateBps) + "–" + Percent(balance.UnsecuredMaxRateBps) + "/năm");
            UiKit.StatRow(seed, "Secured", "Hạn mức thế chấp",
                Million(balance.SecuredLoanCapCoins) + " · " +
                Percent(balance.SecuredMinRateBps) + "–" + Percent(balance.SecuredMaxRateBps) + "/năm");
            UiKit.Callout(seed, "SeedNote",
                "Vốn tự có chỉ đủ thuê đất 1–2 năm và cọc thiết bị ban đầu. Vay tín chấp cần UBND " +
                "xã xác nhận dự án; vay thế chấp lấy 70% định giá sổ đỏ và cần xong giấy an toàn " +
                "thực phẩm.");

            UiKit.Section(body, "Bảng mô phỏng tín dụng");
            var form = UiKit.Card(body, "LoanForm");

            var kindRow = ButtonRow(form, "KindRow");
            _loanKindButtons.Clear();
            foreach (var offer in Finance.Offers(balance))
            {
                var kind = offer.Kind;
                _loanKindButtons.Add(UiFactory.TextButton(kindRow.transform, "Kind_" + kind,
                    offer.DisplayName, delegate { SelectLoanKind(kind); }, UiFactory.ButtonStyle.Quiet));
            }

            _loanAmountLabel = StepperRow(form, "Amount", "Số tiền vay (L₀)",
                                          delegate(int step) { StepLoanAmount(step); }, 100);
            _loanTermLabel = StepperRow(form, "Term", "Thời hạn (n)",
                                        delegate(int step) { StepLoanTerm(step); }, 3);

            _quoteRateRow = UiKit.StatRow(form, "QuoteRate", "Lãi suất áp dụng");
            _quotePrincipalRow = UiKit.StatRow(form, "QuotePrincipal", "Trả gốc mỗi kỳ (L₀/n)");
            _quoteInterestRow = UiKit.StatRow(form, "QuoteInterest", "Lãi kỳ đầu");
            _quotePmtRow = UiKit.StatRow(form, "QuotePmt", "Tổng phải trả kỳ đầu (PMT₁)");
            _borrowButton = UiFactory.TextButton(form, "Borrow", "Nhận tiền vay", delegate
            {
                Run(_session.TakeLoan(_loanKind, _loanAmountCoins, _loanTermMonths));
            });
            UiKit.Callout(form, "BorrowNote",
                "Vay dài hơn thì lãi suất cao hơn. Tiền vay không tính là doanh thu — nó đổi một " +
                "cục tiền bây giờ lấy một chuỗi nghĩa vụ về sau.");

            UiKit.Section(body, "Dòng tiền theo kỳ");
            var chart = UiKit.Card(body, "CashFlow");
            _cashEmptyNote = UiKit.Note(chart, "Empty", "");
            _cashRows.Clear();
            for (int i = 0; i < balance.CashHistoryCycles; i++)
            {
                var gauge = UiKit.Meter(chart, "Cycle" + i, "");
                gauge.Node.SetActive(false);
                _cashRows.Add(gauge);
            }
            UiKit.Callout(chart, "GameOverNote",
                "Nếu " + balance.LoanSealShortfalls + " kỳ liền không trả đủ nghĩa vụ, ngân hàng " +
                "niêm phong nương chè: xưởng, quầy trà và robot dừng cho tới khi trả hết nợ. " +
                "Vẫn hái và bán lá tươi được — đó là đường về, và là đường chậm nhất.",
                UiTone.Warn);
        }

        void SelectLoanKind(LoanKind kind)
        {
            _loanKind = kind;
            var offer = Finance.Offer(_session.Catalog.Balance, kind);
            if (offer != null && _loanAmountCoins > offer.MaxPrincipalCoins)
                _loanAmountCoins = offer.MaxPrincipalCoins;
            RefreshVolatile();
        }

        void StepLoanAmount(int stepCoins)
        {
            var offer = Finance.Offer(_session.Catalog.Balance, _loanKind);
            long max = offer == null ? 0 : offer.MaxPrincipalCoins;
            _loanAmountCoins = Mathf.Clamp((int)(_loanAmountCoins + stepCoins), 100, (int)max);
            RefreshVolatile();
        }

        void StepLoanTerm(int stepMonths)
        {
            var balance = _session.Catalog.Balance;
            _loanTermMonths = Mathf.Clamp(_loanTermMonths + stepMonths,
                                          balance.LoanMinTermMonths, balance.LoanMaxTermMonths);
            RefreshVolatile();
        }

        void RefreshFinancePanel(GameState state)
        {
            var balance = _session.Catalog.Balance;
            var loan = state.Loan;
            var offer = Finance.Offer(balance, _loanKind);
            int rateBps = Finance.RateBpsFor(offer, _loanTermMonths);

            for (int i = 0; i < _loanKindButtons.Count; i++)
                UiFactory.SetButtonState(_loanKindButtons[i],
                    (LoanKind)(i + 1) == _loanKind ? UiFactory.ButtonState.Selected
                                                   : UiFactory.ButtonState.Normal);

            _loanAmountLabel.text = Coins(_loanAmountCoins);
            _loanTermLabel.text = _loanTermMonths + " tháng";

            long principalDue = Finance.PrincipalDue(_loanAmountCoins, _loanTermMonths, 0);
            long interestDue = Finance.InterestDue(_loanAmountCoins, rateBps);
            _quoteRateRow.Set(Percent(rateBps) + "/năm");
            _quotePrincipalRow.Set(Coins(principalDue));
            _quoteInterestRow.Set(Coins(interestDue));
            _quotePmtRow.Set(Coins(principalDue + interestDue), UiTone.Accent);

            string borrowReason;
            bool canBorrow = _session.CanBorrow(_loanKind, _loanAmountCoins, _loanTermMonths, out borrowReason);
            UiFactory.SetInteractable(_borrowButton, canBorrow);

            bool owing = loan.Active || loan.OverdueCoins > 0;
            _loanDebtRow.SetVisible(owing);
            _loanRateRow.SetVisible(owing);
            _loanTermRow.SetVisible(owing);
            _loanDueRow.SetVisible(owing);
            _loanOverdueRow.SetVisible(owing && loan.OverdueCoins > 0);
            _repayButton.gameObject.SetActive(owing);

            if (!owing)
            {
                UiKit.SetBadge(_loanStateBadge, "Chưa vay", UiTone.Neutral);
                _loanHint.text = canBorrow
                    ? "Vốn tự có chỉ đủ mở đầu. Vay là cách duy nhất dựng xong dây chuyền sớm."
                    : borrowReason;
                return;
            }

            UiTone tone = loan.Sealed ? UiTone.Bad
                : loan.ConsecutiveShortfalls > 0 ? UiTone.Warn : UiTone.Good;
            UiKit.SetBadge(_loanStateBadge,
                loan.Sealed ? "Đang bị niêm phong"
                    : loan.ConsecutiveShortfalls > 0
                        ? "Thiếu " + loan.ConsecutiveShortfalls + "/" + balance.LoanSealShortfalls + " kỳ"
                        : "Đang trả đúng hạn",
                tone);

            _loanDebtRow.Set(Coins(loan.RemainingPrincipalCoins), tone);
            _loanRateRow.Set(Percent(loan.AnnualRateBps) + "/năm");
            _loanTermRow.Set(loan.MonthsPaid + " / " + loan.TermMonths + " kỳ");
            _loanDueRow.Set(Coins(Finance.InstallmentDue(loan)));
            _loanOverdueRow.Set(Coins(loan.OverdueCoins), UiTone.Bad);

            long payoff = Finance.PayoffAmount(loan);
            UiFactory.SetButtonCaption(_repayButton, "Trả hết nợ · " + Coins(payoff));
            UiFactory.SetInteractable(_repayButton, state.Coins >= payoff);
            _loanHint.text = loan.Sealed
                ? "Xưởng, quầy trà và robot đang dừng. Hái tay và bán lá tươi để gom đủ " +
                  Coins(payoff) + "."
                : "Kỳ tới chốt sau " +
                  Mathf.CeilToInt(Math.Max(0, state.NextCycleCloseAtMs - state.SimulationTimeMs) / 1000f) +
                  " giây.";

            RefreshCashFlow(state);
        }

        /// <summary>
        /// Do thi dong tien: moi ky mot thanh, do dai theo do lon so voi ky manh nhat.
        ///
        /// Truoc day cho nay la mot day ky tu khoi ve bang chinh dong chu. Doc duoc, nhung khong
        /// so sanh duoc: mat khong do duoc do dai cua nhung khoi chu nam tren nhung dong khac
        /// nhau. Mot thanh that tren mot cai rãnh that thi so sanh duoc ngay.
        /// </summary>
        void RefreshCashFlow(GameState state)
        {
            int count = state.CashHistory.Count;
            _cashEmptyNote.gameObject.SetActive(count == 0);
            if (count == 0)
            {
                _cashEmptyNote.text = "Chưa chốt kỳ nào. Một kỳ dài " +
                                      (Finance.CycleMs(_session.Catalog.Balance) / 1000) + " giây.";
            }

            long peak = 1;
            for (int i = 0; i < count; i++)
            {
                long magnitude = Math.Abs(state.CashHistory[i].NetCashCoins);
                if (magnitude > peak) peak = magnitude;
            }

            for (int i = 0; i < _cashRows.Count; i++)
            {
                var gauge = _cashRows[i];
                // Ky moi nhat len tren cung: no la thu nguoi choi hoi den truoc.
                int source = count - 1 - i;
                if (source < 0)
                {
                    gauge.Node.SetActive(false);
                    continue;
                }
                gauge.Node.SetActive(true);
                var record = state.CashHistory[source];
                bool negative = record.NetCashCoins < 0;
                gauge.Caption.text = "Kỳ " + record.Month + " · nợ " + Coins(record.DebtServiceCoins);
                gauge.Value.text = (negative ? "−" : "+") + Coins(Math.Abs(record.NetCashCoins));
                gauge.Value.color = UiTokens.TextOf(negative ? UiTone.Bad : UiTone.Good);
                gauge.SetBand(0f, 0f);
                gauge.Set(Math.Abs(record.NetCashCoins) / (float)peak,
                          negative ? UiTone.Bad : UiTone.Good);
            }
        }

        // ---------------------------------------------------------------- 2. nong hoc

        void BuildAgronomyRows(Transform body)
        {
            UiKit.Section(body, "Mùa đang chạy");
            var now = UiKit.Card(body, "SeasonNow");
            _seasonBadge = UiKit.Badge(now, "Season", "", UiTone.Neutral);
            _seasonFlavourRow = UiKit.StatRow(now, "Flavour", "Hương vị đặc trưng");
            _seasonYieldRow = UiKit.StatRow(now, "Yield", "Năng suất chè");
            _seasonPriceRow = UiKit.StatRow(now, "Price", "Giá thương phẩm");
            _theanineGauge = UiKit.Meter(now, "Theanine", "Theanine · vị ngọt êm");
            _polyphenolGauge = UiKit.Meter(now, "Polyphenol", "Polyphenol · vị chát đượm");
            _leafhopperBadge = UiKit.Badge(now, "Leafhopper", "", UiTone.Good);

            UiKit.Section(body, "Bốn mùa");
            foreach (var profile in Agronomy.AllProfiles())
            {
                var card = UiKit.Card(body, "Season_" + profile.Season);
                UiFactory.Label(card, "Name",
                    profile.DisplayName + "  ·  " + profile.MinTempC + "–" + profile.MaxTempC + "°C",
                    UiTokens.TextRowTitle, TextAnchor.MiddleLeft, GardenPalette.TextPrimary, true);
                UiKit.StatRow(card, "Yield", "Năng suất", profile.YieldPercent + "%");
                UiKit.StatRow(card, "Price", "Giá", profile.PricePercent + "%");
                UiKit.StatRow(card, "Theanine", "Theanine", (profile.TheaninePermille / 10f) + "%");
                UiKit.StatRow(card, "Polyphenol", "Polyphenol", (profile.PolyphenolPermille / 10f) + "%");
                UiKit.Callout(card, "Work", profile.Work,
                              profile.Dormant ? UiTone.Warn : UiTone.Neutral);
            }
            UiKit.Callout(body, "Biochem",
                "Theanine là vị ngọt êm, polyphenol là vị chát đượm. Hai chỉ số biến thiên ngược " +
                "nhau theo nhiệt độ, nên mùa là một nút xoay giữa nhiều-và-rẻ với ít-và-đắt.");

            UiKit.Section(body, "Phân hạng thu hái");
            foreach (var grade in Agronomy.Grades())
            {
                var crop = _session.Catalog.Crop(grade.CropId);
                var recipe = _session.Catalog.RecipeForCrop(grade.CropId);
                var card = UiKit.Card(body, "Grade_" + grade.Grade);
                UiFactory.Label(card, "Name", grade.PluckName + "  ·  " + grade.ProductName,
                    UiTokens.TextRowTitle, TextAnchor.MiddleLeft, GardenPalette.TextPrimary, true);
                UiKit.StatRow(card, "Fresh", "Hái mỗi ngày",
                              (grade.FreshGramsPerDay / 1000f) + " kg búp tươi");
                UiKit.StatRow(card, "Ratio", "Tươi cho một kg khô",
                              (grade.FreshPerDryPermille / 1000f) + " kg");
                UiKit.StatRow(card, "Price", "Giá một kg khô",
                              grade.PricePerDryKiloThousandVnd + "k · " + grade.Segment);
                UiKit.StatRow(card, "InGame", "Trong vườn",
                              crop.Yield + " búp một ô", UiTone.Accent);
                UiKit.StatRow(card, "Packed", "Mỗi " + recipe.PackedInputCount + " búp đóng gói bán",
                              Coins(recipe.PackedOutputCoins), UiTone.Accent);
            }
            UiKit.Callout(body, "GradeNote",
                "Doanh thu một vụ của bốn phân hạng gần bằng nhau. Cái khác là số lượng: búp xô " +
                "cho gấp mười lăm lần số hàng phải chạy qua dây chuyền để kiếm cùng số tiền. Dây " +
                "chuyền đang là giới hạn thì hái non hơn là thắng.");

            UiKit.Section(body, "Cơ chế rầy xanh");
            UiKit.Callout(body, "Leafhopper",
                "Rầy xanh chích hút nhẹ vào cuối xuân đầu hè. Cây chè giải phóng linalool và " +
                "geraniol để gọi thiên địch, và chính hai chất đó làm nên Đông Phương Mỹ Nhân — " +
                "giá gấp bốn đến sáu lần.\n\n" +
                "Ô bị rầy xanh thu về \"lá rầy xanh\" chứ không phải búp chè thường, và vụ đó " +
                "không bị sâu bệnh. Muốn giữ được giá thì phải chế biến theo đường Đông Phương " +
                "Mỹ Nhân với mức oxy hoá 60–75%.",
                UiTone.Good);
        }

        void RefreshAgronomyPanel(GameState state)
        {
            var balance = _session.Catalog.Balance;
            var season = Cultivation.SeasonAt(balance, state.SimulationTimeMs);
            var profile = Agronomy.Profile(season);
            int leafhopperPlots = 0;
            for (int i = 0; i < state.Plots.Count; i++)
                if (state.Plots[i].Leafhopper) leafhopperPlots++;

            UiKit.SetBadge(_seasonBadge, profile.DisplayName,
                           profile.Dormant ? UiTone.Warn : UiTone.Accent);
            _seasonFlavourRow.Set(profile.Flavour);
            _seasonYieldRow.Set(profile.YieldPercent + "%",
                profile.YieldPercent >= 100 ? UiTone.Good : UiTone.Warn);
            _seasonPriceRow.Set(profile.PricePercent + "%",
                profile.PricePercent >= 100 ? UiTone.Good : UiTone.Warn);

            // Thang do lay theo dinh cua ca bon mua, khong theo 100%: hai chi so nay khong bao gio
            // toi 100‰, ap thang 0–100 thi ca bon mua deu ra mot vach ti hon nhu nhau.
            _theanineGauge.Value.text = (profile.TheaninePermille / 10f) + "%";
            _theanineGauge.Set(profile.TheaninePermille / 30f, UiTone.Good);
            _polyphenolGauge.Value.text = (profile.PolyphenolPermille / 10f) + "%";
            _polyphenolGauge.Set(profile.PolyphenolPermille / 360f, UiTone.Warn);

            bool anyLeafhopper = leafhopperPlots > 0;
            _leafhopperBadge.transform.parent.gameObject.SetActive(anyLeafhopper || profile.Leafhopper);
            UiKit.SetBadge(_leafhopperBadge,
                anyLeafhopper ? "Đang có " + leafhopperPlots + " ô lá rầy xanh"
                              : "Mùa rầy xanh · " + balance.LeafhopperChancePercent + "% số vụ chè",
                anyLeafhopper ? UiTone.Good : UiTone.Neutral);
        }

        // ---------------------------------------------------------------- 3. can lua

        void BuildCraftRows(Transform body)
        {
            UiKit.Section(body, "Đường chế biến");
            var routeRow = ButtonRow(body, "RouteRow");
            _craftRouteButtons.Clear();
            foreach (TeaRoute route in Enum.GetValues(typeof(TeaRoute)))
            {
                var captured = route;
                _craftRouteButtons.Add(UiFactory.TextButton(routeRow.transform, "Route_" + route,
                    Crafting.RouteName(route), delegate { Run(_session.SetCraftRoute(captured)); },
                    UiFactory.ButtonStyle.Quiet));
            }
            _craftRouteNote = UiKit.Note(body, "RouteSummary", "");

            UiKit.Section(body, "Thông số căn lửa");
            _craftGauges.Clear();
            var windows = Crafting.Windows(TeaRoute.Green);
            for (int i = 0; i < windows.Count; i++)
            {
                int index = i;
                var card = UiKit.Card(body, "Dial" + i);
                StepperButtons(card, "Step", ShortDialName(index),
                               delegate(int step) { StepCraft(index, step); }, CraftStepFor(index));
                _craftGauges.Add(UiKit.Meter(card, "Gauge" + i, ""));
            }
            UiKit.Callout(body, "BandNote",
                "Mỗi thanh trải hết khoảng chấp nhận được, và vệt sáng trên nó là khoảng chuẩn. " +
                "Nằm trong vệt sáng cả bốn thông số thì mẻ đạt thượng hạng; lệch ra mà vẫn trên " +
                "thanh thì bán được, chỉ không được giá cao nhất; chạm hai đầu thanh là mẻ lỗi.");

            UiKit.Section(body, "Mẻ trà thành phẩm");
            var verdict = UiKit.Card(body, "Verdict");
            _craftVerdictBadge = UiKit.Badge(verdict, "Quality", "", UiTone.Neutral);
            _craftProductName = UiFactory.Label(verdict, "Product", "", UiTokens.TextRowTitle,
                TextAnchor.MiddleLeft, GardenPalette.TextPrimary, true);
            _craftLiquorRow = UiKit.StatRow(verdict, "Liquor", "Sắc nước");
            _craftAromaRow = UiKit.StatRow(verdict, "Aroma", "Hương vị");
            _craftStorageRow = UiKit.StatRow(verdict, "Storage", "Rủi ro bảo quản");
            _craftPriceRow = UiKit.StatRow(verdict, "Price", "Hệ số giá");
            _craftNote = UiKit.Note(verdict, "Note", "");

            UiKit.Callout(body, "CraftNote",
                "Hệ số này nhân với hệ số mùa vụ và hệ số kênh bán, rồi áp lên tiền thật của mỗi " +
                "mẻ ở quầy trà.");
        }

        /// <summary>Buoc nhay cua tung nut. Nhiet do nhay 5 do, con lai nhay 1.</summary>
        static int CraftStepFor(int windowIndex)
        {
            return windowIndex == 0 ? 5 : 1;
        }

        /// <summary>Ten ngan cua tung nut, cho cot nhan cua hang stepper.</summary>
        static string ShortDialName(int windowIndex)
        {
            switch (windowIndex)
            {
                case 0: return "Nhiệt diệt men T₁";
                case 1: return "Thời gian vò";
                case 2: return "Độ ẩm M";
                default: return "Mức oxy hoá";
            }
        }

        void StepCraft(int windowIndex, int step)
        {
            var values = Crafting.Values(_session.State.Craft);
            Run(_session.SetCraftValue(windowIndex, values[windowIndex] + step));
        }

        void RefreshCraftPanel(GameState state)
        {
            var settings = state.Craft;
            var windows = Crafting.Windows(settings.Route);
            var values = Crafting.Values(settings);

            for (int i = 0; i < _craftRouteButtons.Count; i++)
                UiFactory.SetButtonState(_craftRouteButtons[i],
                    (TeaRoute)i == settings.Route ? UiFactory.ButtonState.Selected
                                                  : UiFactory.ButtonState.Normal);
            _craftRouteNote.text = Crafting.RouteSummary(settings.Route);

            for (int i = 0; i < _craftGauges.Count && i < windows.Count; i++)
            {
                var window = windows[i];
                var gauge = _craftGauges[i];
                int compare = Crafting.CompareToIdeal(window, values[i]);

                // Truc lay **khoang chap nhan duoc**, khong lay ca dai chinh duoc. Nhiet do chinh
                // duoc tu 80 den 320 do, ma khoang chuan cua tra xanh chi la 250–260 — ve tren
                // truc 240 do thi vet sang do rong bon phan tram, tuc mot soi chi khong nhin ra.
                // Tren truc 235–275 thi no chiem mot phan tu thanh, va no tro thanh thu doc duoc.
                float low = window.AcceptableLow;
                float high = window.AcceptableHigh;
                float span = Mathf.Max(1f, high - low);

                // Hang nut ngay tren da mang ten nut roi, nen o day chi con khoang chuan.
                gauge.Caption.text = "chuẩn " + window.IdealLow + "–" + window.IdealHigh + window.Unit;
                gauge.Value.text = values[i] + window.Unit;
                gauge.Value.color = UiTokens.TextOf(compare == 0 ? UiTone.Good : UiTone.Warn);
                gauge.SetBand((window.IdealLow - low) / span, (window.IdealHigh - low) / span);
                gauge.Set((values[i] - low) / span, compare == 0 ? UiTone.Good : UiTone.Warn);
            }

            bool leafhopperLeaves = HasLeafhopperLeaves(state);
            var balance = _session.Catalog.Balance;
            var verdict = Crafting.Evaluate(settings, leafhopperLeaves,
                                            balance.PremiumBatchPricePercent,
                                            balance.FlawedBatchPricePercent);
            int price = Crafting.BatchPricePercent(verdict, leafhopperLeaves,
                                                   balance.OrientalBeautyFallbackPercent);
            var tone = verdict.Quality == BatchQuality.Flawed ? UiTone.Bad
                : verdict.Quality == BatchQuality.Premium ? UiTone.Good : UiTone.Neutral;

            UiKit.SetBadge(_craftVerdictBadge, verdict.Title, tone);
            _craftProductName.text = verdict.ProductName;
            _craftLiquorRow.Set(verdict.Liquor);
            _craftAromaRow.Set(verdict.Aroma);
            _craftStorageRow.Set(verdict.StorageRisk);
            _craftPriceRow.Set(price + "%", price >= 100 ? UiTone.Good : UiTone.Bad);
            _craftNote.text = verdict.Note;
        }

        /// <summary>
        /// Trong kho co la ray xanh hay hang lam tu no khong.
        ///
        /// Doc ca chuoi che bien, khong chi la tuoi: la ray xanh da di vao day chuyen roi thi
        /// nguoi choi van dang lam mot me Dong Phuong My Nhan, va bang danh gia phai noi dung
        /// dieu do — do la luc bon cai nut con kip cuu duoc me tra.
        /// </summary>
        static bool HasLeafhopperLeaves(GameState state)
        {
            foreach (var pair in state.Inventory)
            {
                if (pair.Value <= 0) continue;
                if (pair.Key == DefaultContent.CropOrientalBeauty ||
                    pair.Key.StartsWith(DefaultContent.CropOrientalBeauty + "_", StringComparison.Ordinal))
                    return true;
            }
            for (int i = 0; i < state.Plots.Count; i++)
                if (state.Plots[i].Leafhopper) return true;
            return false;
        }

        // ---------------------------------------------------------------- 4. phap ly

        void BuildLegalRows(Transform body)
        {
            var balance = _session.Catalog.Balance;

            UiKit.Section(body, "Tình trạng hồ sơ");
            var status = UiKit.Card(body, "LegalStatus");
            _suspendedBadge = UiKit.Badge(status, "Suspended", "", UiTone.Bad);
            _entityRow = UiKit.StatRow(status, "Entity", "Hình thức kinh doanh");
            _foodSafetyRow = UiKit.StatRow(status, "FoodSafety", "Giấy an toàn thực phẩm");
            _gearRow = UiKit.StatRow(status, "Gear", "Đồ bảo hộ y tế");
            _oneWayRow = UiKit.StatRow(status, "OneWay", "Dây chuyền một chiều");
            _taxRow = UiKit.StatRow(status, "Tax", "Đã nộp thuế");
            _fineRow = UiKit.StatRow(status, "Fine", "Đã bị phạt");
            _inspectionRow = UiKit.StatRow(status, "Inspection", "Kỳ kiểm tra tới");

            UiKit.Section(body, "Hình thức kinh doanh");
            foreach (var option in Compliance.Entities())
            {
                var entity = option.Entity;
                var card = UiKit.Card(body, "Entity_" + entity);
                UiFactory.Label(card, "Name", option.DisplayName, UiTokens.TextRowTitle,
                                TextAnchor.MiddleLeft, GardenPalette.TextPrimary, true);
                UiKit.Badge(card, "Stage_" + entity, option.Stage, UiTone.Neutral);
                UiKit.StatRow(card, "Where", "Đăng ký tại",
                              option.Authority + " · " + option.MinDays + "–" + option.MaxDays + " ngày");
                UiKit.StatRow(card, "Tax", "Nghĩa vụ thuế",
                    option.RevenueTaxBps > 0
                        ? "khoán " + Percent(option.RevenueTaxBps) + " doanh thu"
                        : "TNDN " + Percent(option.ProfitTaxBps) + " lợi nhuận");
                if (option.VatBps > 0)
                    UiKit.StatRow(card, "Vat", "Hoá đơn GTGT",
                                  Percent(option.VatBps) + " · thu của người mua, khấu trừ");
                if (option.BookkeepingCoinsPerCycle > 0)
                    UiKit.StatRow(card, "Books", "Chi phí kế toán",
                                  Coins(option.BookkeepingCoinsPerCycle) + " mỗi kỳ");
                UiKit.Callout(card, "Pro", option.Advantage, UiTone.Good);
                UiKit.Callout(card, "Con", option.Drawback, UiTone.Warn);
                var button = UiFactory.TextButton(card, "Register",
                    "Nộp hồ sơ · " + Coins(option.FeeCoins),
                    delegate { Run(_session.RegisterEntity(entity)); });
                if (entity == BusinessEntity.Hkd) _hkdButton = button; else _tnhhButton = button;
            }

            UiKit.Section(body, "Giấy an toàn thực phẩm");
            var attp = UiKit.Card(body, "FoodSafetyCard");
            UiKit.StatRow(attp, "Fee", "Lệ phí", Coins(balance.FoodSafetyFeeCoins));
            UiKit.StatRow(attp, "Days", "Thẩm định",
                          balance.FoodSafetyMinDays + "–" + balance.FoodSafetyMaxDays + " ngày in-game");
            UiKit.Callout(attp, "Note",
                "Thông tư 38/2018/TT-BNNPTNT. Phải nộp trước khi cần đến nó — hoạt động khi chưa " +
                "có giấy là bị phạt kèm đình chỉ.", UiTone.Warn);
            _foodSafetyButton = UiFactory.TextButton(attp, "FoodSafety",
                "Xin giấy · " + Coins(balance.FoodSafetyFeeCoins),
                delegate { Run(_session.ApplyFoodSafety()); });

            UiKit.Section(body, "Xưởng sơ chế một chiều");
            var zones = UiKit.Card(body, "Zones");
            foreach (var zone in Compliance.Zones())
                UiKit.StatRow(zones, "Zone" + zone.Order, zone.Order + ". " + zone.DisplayName,
                              zone.Detail);
            UiKit.Callout(zones, "ZoneNote",
                "Nguyên liệu và công nhân đi theo một chiều duy nhất để chống nhiễm chéo. Trong " +
                "game: để hở một chặng giữa dây chuyền là nguyên liệu phải vòng ngược lại qua khu " +
                "đã xử lý, và đó là vi phạm. Thiếu máy ở cuối dây chuyền chỉ là chưa xây xong.");

            UiKit.Section(body, "Chế tài xử phạt");
            var fines = UiKit.Card(body, "Fines");
            foreach (var violation in Compliance.Violations())
            {
                UiKit.StatRow(fines, violation.Id, violation.DisplayName,
                    Million(violation.MinFineCoins) + "–" + Million(violation.MaxFineCoins) +
                    (violation.Suspends ? " + đình chỉ" : ""),
                    violation.Suspends ? UiTone.Bad : UiTone.Warn);
                UiKit.Callout(fines, "Fix_" + violation.Id, violation.Remedy);
            }
        }

        void RefreshLegalPanel(GameState state)
        {
            var compliance = state.Compliance;
            var option = Compliance.Entity(compliance.Entity);
            long dayMs = Compliance.DayMs(_session.Catalog.Balance);
            bool suspended = compliance.Suspended(state.SimulationTimeMs);

            _suspendedBadge.transform.parent.gameObject.SetActive(suspended);
            if (suspended) UiKit.SetBadge(_suspendedBadge, "Xưởng đang bị đình chỉ", UiTone.Bad);

            if (compliance.PendingEntity != BusinessEntity.None)
            {
                long left = Math.Max(0, compliance.EntityReadyAtMs - state.SimulationTimeMs);
                _entityRow.Set("đang thẩm định · còn " + (left / dayMs + 1) + " ngày", UiTone.Warn);
            }
            else
            {
                _entityRow.Set(option != null ? option.DisplayName : "chưa đăng ký",
                               option != null ? UiTone.Good : UiTone.Warn);
            }

            if (compliance.FoodSafetyCertified) _foodSafetyRow.Set("đã có", UiTone.Good);
            else if (compliance.FoodSafetyPending)
                _foodSafetyRow.Set("đang thẩm định · còn " +
                    (Math.Max(0, compliance.FoodSafetyReadyAtMs - state.SimulationTimeMs) / dayMs + 1) +
                    " ngày", UiTone.Warn);
            else _foodSafetyRow.Set("chưa có", UiTone.Bad);

            _gearRow.Set(compliance.ProtectiveGear ? "đã có" : "chưa mua",
                         compliance.ProtectiveGear ? UiTone.Good : UiTone.Warn);
            bool oneWay = Compliance.OneWayRespected(_session.Catalog, state);
            _oneWayRow.Set(oneWay ? "đạt" : "có lỗ hổng", oneWay ? UiTone.Good : UiTone.Bad);
            _taxRow.Set(Coins(compliance.TotalTaxCoins));
            _fineRow.Set(Coins(compliance.TotalFinesCoins),
                         compliance.TotalFinesCoins > 0 ? UiTone.Warn : UiTone.Neutral);

            long nextInspection = Math.Max(0, compliance.NextInspectionAtMs - state.SimulationTimeMs);
            _inspectionRow.Set("sau " + (nextInspection / 1000) + " giây · đã kiểm " +
                               compliance.InspectionCount + " lần");

            string reason;
            UiFactory.SetInteractable(_hkdButton, _session.CanRegisterEntity(BusinessEntity.Hkd, out reason));
            UiFactory.SetInteractable(_tnhhButton, _session.CanRegisterEntity(BusinessEntity.Tnhh, out reason));
            UiFactory.SetInteractable(_foodSafetyButton, _session.CanApplyFoodSafety(out reason));
        }

        // ---------------------------------------------------------------- 5. mo hinh kinh doanh

        void BuildModelRows(Transform body)
        {
            UiKit.Section(body, "Lộ trình ba giai đoạn");
            var phase = UiKit.Card(body, "Phase");
            _phaseBadge = UiKit.Badge(phase, "Phase", "", UiTone.Accent);
            _phaseGoal = UiKit.Note(phase, "Goal", "");
            _tourismRow = UiKit.StatRow(phase, "Tourism", "Thu du lịch mỗi kỳ");
            _receivableRow = UiKit.StatRow(phase, "Receivable", "Đang chờ khách thanh toán");

            UiKit.Section(body, "Ba phân nhánh");
            _branchButtons.Clear();
            _branchBadges.Clear();
            foreach (var branch in Branches.All())
            {
                var captured = branch.Branch;
                var card = UiKit.Card(body, "Branch_" + captured);
                UiFactory.Label(card, "Name", branch.DisplayName, UiTokens.TextRowTitle,
                                TextAnchor.MiddleLeft, GardenPalette.TextPrimary, true);
                _branchBadges.Add(UiKit.Badge(card, "State_" + captured, "", UiTone.Neutral));
                UiKit.StatRow(card, "Margin", "Biên lợi nhuận",
                              branch.MinMarginPercent + "–" + branch.MaxMarginPercent + "%");
                UiKit.StatRow(card, "Capex", "CAPEX",
                              branch.MinCapexMillionVnd + "–" + branch.MaxCapexMillionVnd + " triệu");
                UiKit.StatRow(card, "Cycle", "Vòng quay vốn",
                              branch.MinCashCycleDays + "–" + branch.MaxCashCycleDays + " ngày");
                UiKit.StatRow(card, "Price", "Giá bán trong game", branch.PricePercent + "%",
                              branch.PricePercent >= 100 ? UiTone.Good : UiTone.Warn);
                if (branch.PayoutDelayDays > 0)
                    UiKit.StatRow(card, "Delay", "Tiền về sau",
                                  branch.PayoutDelayDays + " ngày", UiTone.Warn);
                if (branch.SellsUnpacked)
                    UiKit.StatRow(card, "Bulk", "Bán được trà mộc", "không cần máy đóng gói",
                                  UiTone.Good);
                if (branch.WinterIncome)
                    UiKit.StatRow(card, "Winter", "Mùa đông", "vẫn có thu", UiTone.Good);
                UiKit.Callout(card, "Summary", branch.Summary);
                _branchButtons.Add(UiFactory.TextButton(card, "Unlock",
                    "Mở nhánh · " + Coins(branch.UnlockCostCoins),
                    delegate { Run(_session.UnlockBranch(captured)); }));
            }

            UiKit.Section(body, "Kênh bán chính");
            var channelRow = ButtonRow(body, "ChannelRow");
            _channelButtons.Clear();
            foreach (var channel in SalesChannels)
            {
                var captured = channel;
                string caption = channel == BusinessBranch.None
                    ? "Bán như cũ" : Branches.Definition(channel).DisplayName;
                _channelButtons.Add(UiFactory.TextButton(channelRow.transform, "Channel_" + channel,
                    caption, delegate { Run(_session.SetSalesChannel(captured)); },
                    UiFactory.ButtonStyle.Quiet));
            }
            UiKit.Callout(body, "ChannelNote",
                "Ba nhánh không loại trừ nhau — kết hợp lại mới tối ưu được dòng tiền và giảm rủi " +
                "ro mùa vụ. Cái phải chọn là kênh bán chính cho mỗi mẻ trà, và đổi được bất cứ lúc " +
                "nào. Du lịch trải nghiệm không phải một kênh bán trà: nó là một nguồn thu riêng " +
                "mỗi kỳ.");
        }

        /// <summary>Ba lua chon kenh ban. Du lich khong nam trong day — no khong ban tra.</summary>
        static readonly BusinessBranch[] SalesChannels =
        {
            BusinessBranch.None, BusinessBranch.BulkB2B, BusinessBranch.ArtisanalDtc
        };

        void RefreshModelPanel(GameState state)
        {
            int phase = Branches.PhaseOf(state);
            long tourism = Branches.FarmstayIncome(_session.Catalog.Balance, state);
            long receivable = 0;
            for (int i = 0; i < state.Receivables.Count; i++) receivable += state.Receivables[i].AmountCoins;

            UiKit.SetBadge(_phaseBadge, Branches.PhaseName(phase), UiTone.Accent);
            _phaseGoal.text = Branches.PhaseGoal(phase);
            _tourismRow.SetVisible(tourism > 0);
            _tourismRow.Set(Coins(tourism), UiTone.Good);
            _receivableRow.Set(receivable > 0
                ? Coins(receivable) + " trong " + state.Receivables.Count + " lô"
                : "không có", receivable > 0 ? UiTone.Warn : UiTone.Neutral);

            var all = Branches.All();
            for (int i = 0; i < all.Count && i < _branchButtons.Count; i++)
            {
                var branch = all[i].Branch;
                bool owned = state.UnlockedBranches.Contains(branch);
                string reason = "Đã mở";
                bool can = false;
                if (!owned)
                {
                    // Ly do tu choi la cau se hien tren the trang thai, nen phai goi CanUnlockBranch
                    // ke ca khi da biet la khong mo duoc — chinh no dung ra cau do.
                    can = _session.CanUnlockBranch(branch, out reason);
                    if (can) reason = "Mở được ngay";
                }

                _branchButtons[i].gameObject.SetActive(!owned);
                UiFactory.SetInteractable(_branchButtons[i], can);
                UiKit.SetBadge(_branchBadges[i], reason,
                               owned ? UiTone.Good : can ? UiTone.Accent : UiTone.Warn);
            }

            for (int i = 0; i < _channelButtons.Count && i < SalesChannels.Length; i++)
            {
                // Ba trang thai, ba y nghia khac nhau: dang chon, chon duoc, va chua mo nhanh.
                // SetButtonState tu tat interactable cho Selected va Disabled, nen mot lan goi la
                // du — goi them SetInteractable sau do se ghi de mau cua Selected.
                bool selected = state.SalesChannel == SalesChannels[i];
                bool available = SalesChannels[i] == BusinessBranch.None ||
                                 state.UnlockedBranches.Contains(SalesChannels[i]);
                UiFactory.SetButtonState(_channelButtons[i],
                    selected ? UiFactory.ButtonState.Selected
                             : available ? UiFactory.ButtonState.Normal
                                         : UiFactory.ButtonState.Disabled);
            }
        }

        // ---------------------------------------------------------------- lam moi ca nam bang

        void RefreshStartupPanels(GameState state)
        {
            if (_financePanel != null && _financePanel.activeSelf) RefreshFinancePanel(state);
            if (_agronomyPanel != null && _agronomyPanel.activeSelf) RefreshAgronomyPanel(state);
            if (_craftPanel != null && _craftPanel.activeSelf) RefreshCraftPanel(state);
            if (_legalPanel != null && _legalPanel.activeSelf) RefreshLegalPanel(state);
            if (_modelPanel != null && _modelPanel.activeSelf) RefreshModelPanel(state);
        }
    }
}
