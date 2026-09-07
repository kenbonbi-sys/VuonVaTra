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
    /// Nguyen tac chung cua ca nam: **moi con so hien ra deu doc tu GameState hoac tu mot ham
    /// thuan tuy trong Core**, khong mot con so nao duoc go tay o day. Cho nao la bang tham chieu
    /// — bang sinh hoa bon mua, bang phan hang thu hai, khung tien phat — thi lay tu chinh du lieu
    /// ma mo phong dung, nen doi can bang o Core la bang trong so tay doi theo, khong lech.
    ///
    /// Khong dung slider: cac nut "− gia tri +" doc duoc trong nen art nay va khong can thanh
    /// keo moi. Moi buoc nhay la mot lenh qua GameSession nhu moi lenh khac.
    /// </summary>
    public sealed partial class GameHud
    {
        GameObject _financePanel, _agronomyPanel, _craftPanel, _legalPanel, _modelPanel;

        // Von va no
        Text _loanStatus, _loanTerms, _loanProjection, _cashFlowChart;
        long _loanAmountCoins = 1000;
        int _loanTermMonths = 24;
        LoanKind _loanKind = LoanKind.Unsecured;
        Text _loanAmountLabel, _loanTermLabel;
        Button _borrowButton, _repayButton;

        // Can lua
        Text _craftVerdictTitle, _craftVerdictBody;
        readonly List<Text> _craftValueLabels = new List<Text>();
        readonly List<Text> _craftWindowLabels = new List<Text>();
        readonly List<Button> _craftRouteButtons = new List<Button>();

        // Phap ly
        Text _legalStatus, _inspectionLog;
        Button _hkdButton, _tnhhButton, _foodSafetyButton;

        // Mo hinh kinh doanh
        Text _phaseLabel;
        readonly List<Button> _branchButtons = new List<Button>();
        readonly List<Text> _branchStatus = new List<Text>();
        readonly List<Button> _channelButtons = new List<Button>();

        // Nong hoc
        Text _seasonNow;

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

        static Text SectionTitle(Transform body, string text)
        {
            return UiFactory.Label(body, "T_" + text, text, UiFactory.FontSizeTitle,
                                   TextAnchor.MiddleLeft, GardenPalette.TextPrimary, true);
        }

        static Text Paragraph(Transform body, string name, string text)
        {
            return UiFactory.Label(body, name, text, UiFactory.FontSizeMeta,
                                   TextAnchor.UpperLeft, GardenPalette.TextMuted);
        }

        /// <summary>
        /// Mot khoi chu don khoang cach dong, dung cho bang tham chieu.
        ///
        /// Bang cua ban mo phong co bon, nam cot; dung layout thanh cot that o day se lam moi
        /// bang thanh mot cai luoi phai canh tay. Chu don khoi voi dau gach giua doc duoc va
        /// khong bao gio tran ra ngoai panel.
        /// </summary>
        static Text Monoblock(Transform body, string name, string text)
        {
            var label = UiFactory.Label(body, name, text, UiFactory.FontSizeMeta,
                                        TextAnchor.UpperLeft, GardenPalette.TextPrimary);
            label.lineSpacing = 1.25f;
            return label;
        }

        static Image Card(Transform body, string name)
        {
            var card = UiFactory.Panel(body, name, GardenPalette.PanelSoft, UiFactory.RadiusControl);
            UiFactory.VerticalList(card.gameObject, 6f, new RectOffset(12, 12, 10, 10));
            return card;
        }

        /// <summary>Hang "− gia tri +". Hai nut goi lai <paramref name="onStep"/> voi buoc nhay.</summary>
        static Text StepperRow(Transform parent, string name, string caption,
                               Action<int> onStep, int step)
        {
            var row = UiFactory.Node(parent, name);
            var layout = UiFactory.HorizontalList(row, 8f, new RectOffset(0, 0, 0, 0));
            layout.childForceExpandWidth = false;
            layout.childAlignment = TextAnchor.MiddleLeft;
            var rowSize = row.AddComponent<LayoutElement>();
            rowSize.minHeight = UiFactory.ButtonHeight;

            var title = UiFactory.Label(row.transform, "Caption", caption, UiFactory.FontSizeMeta,
                                        TextAnchor.MiddleLeft, GardenPalette.TextMuted);
            var titleFlex = title.gameObject.AddComponent<LayoutElement>();
            titleFlex.flexibleWidth = 1f;

            var minus = UiFactory.TextButton(row.transform, "Minus", "−",
                                             delegate { onStep(-step); }, UiFactory.ButtonStyle.Quiet);
            SizeStepButton(minus);

            var value = UiFactory.Label(row.transform, "Value", "", UiFactory.FontSizeRowTitle,
                                        TextAnchor.MiddleCenter, GardenPalette.TextPrimary);
            var valueSize = value.gameObject.AddComponent<LayoutElement>();
            valueSize.preferredWidth = 108f;
            valueSize.minWidth = 108f;
            valueSize.flexibleWidth = 0f;

            var plus = UiFactory.TextButton(row.transform, "Plus", "+",
                                            delegate { onStep(step); }, UiFactory.ButtonStyle.Quiet);
            SizeStepButton(plus);
            return value;
        }

        static void SizeStepButton(Button button)
        {
            var size = button.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = UiFactory.ButtonHeight;
            size.minWidth = UiFactory.ButtonHeight;
            size.flexibleWidth = 0f;
        }

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

        // ---------------------------------------------------------------- 1. von va no

        void BuildFinanceRows(Transform body)
        {
            var balance = _session.Catalog.Balance;

            SectionTitle(body, "Cấu trúc vốn mồi");
            Monoblock(body, "SeedCapital",
                "Vốn tự có · " + balance.SeedCapitalMillionVnd + " triệu\n" +
                "   Chỉ đủ thuê đất 1–2 năm và cọc thiết bị ban đầu.\n" +
                "Hạn mức tín chấp · " + Million(balance.UnsecuredLoanCapCoins) + "  ·  " +
                Percent(balance.UnsecuredMinRateBps) + "–" + Percent(balance.UnsecuredMaxRateBps) + "/năm\n" +
                "   Cần UBND xã xác nhận dự án.\n" +
                "Hạn mức thế chấp · " + Million(balance.SecuredLoanCapCoins) + "  ·  " +
                Percent(balance.SecuredMinRateBps) + "–" + Percent(balance.SecuredMaxRateBps) + "/năm\n" +
                "   70% định giá sổ đỏ 500tr, cần xong giấy an toàn thực phẩm.");

            _loanStatus = Monoblock(body, "LoanStatus", "");

            SectionTitle(body, "Bảng mô phỏng tín dụng");
            var form = Card(body, "LoanForm");

            var kindRow = UiFactory.Node(form.transform, "KindRow");
            var kindLayout = UiFactory.HorizontalList(kindRow, 8f, new RectOffset(0, 0, 0, 0));
            kindLayout.childForceExpandWidth = true;
            var kindSize = kindRow.AddComponent<LayoutElement>();
            kindSize.minHeight = UiFactory.ButtonHeight;
            foreach (var offer in Finance.Offers(balance))
            {
                var kind = offer.Kind;
                UiFactory.TextButton(kindRow.transform, "Kind_" + kind, offer.DisplayName,
                                     delegate { SelectLoanKind(kind); }, UiFactory.ButtonStyle.Quiet);
            }

            _loanAmountLabel = StepperRow(form.transform, "Amount", "Số tiền vay (L₀)",
                                          delegate(int step) { StepLoanAmount(step); }, 100);
            _loanTermLabel = StepperRow(form.transform, "Term", "Thời hạn (n)",
                                        delegate(int step) { StepLoanTerm(step); }, 3);
            _loanTerms = Monoblock(form.transform, "Terms", "");

            _borrowButton = UiFactory.TextButton(form.transform, "Borrow", "Nhận tiền vay",
                                                 delegate
            {
                Run(_session.TakeLoan(_loanKind, _loanAmountCoins, _loanTermMonths));
            });

            _repayButton = UiFactory.TextButton(body, "Repay", "Trả hết nợ", delegate
            {
                Run(_session.RepayLoan(Finance.PayoffAmount(_session.State.Loan)));
            });

            SectionTitle(body, "Dòng tiền và dư nợ 12 kỳ");
            _loanProjection = Monoblock(body, "Projection", "");
            _cashFlowChart = Monoblock(body, "CashFlow", "");
            Paragraph(body, "GameOver",
                "Nếu " + balance.LoanSealShortfalls + " kỳ liền không trả đủ nghĩa vụ, ngân hàng siết " +
                "nợ và niêm phong nương chè: cả vườn lẫn xưởng dừng cho tới khi trả hết.");
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

            _loanAmountLabel.text = Coins(_loanAmountCoins);
            _loanTermLabel.text = _loanTermMonths + " tháng";

            long principalDue = Finance.PrincipalDue(_loanAmountCoins, _loanTermMonths, 0);
            long interestDue = Finance.InterestDue(_loanAmountCoins, rateBps);
            _loanTerms.text =
                offer.DisplayName + " · lãi " + Percent(rateBps) + "/năm · " + Million(_loanAmountCoins) + "\n" +
                "Trả gốc mỗi kỳ (L₀/n) · " + Coins(principalDue) + "\n" +
                "Lãi kỳ đầu · " + Coins(interestDue) + "\n" +
                "Tổng phải trả kỳ đầu (PMT₁) · " + Coins(principalDue + interestDue) + "\n" +
                offer.Requirement;

            string borrowReason;
            bool canBorrow = _session.CanBorrow(_loanKind, _loanAmountCoins, _loanTermMonths, out borrowReason);
            UiFactory.SetInteractable(_borrowButton, canBorrow);

            if (loan.Active || loan.OverdueCoins > 0)
            {
                _loanStatus.text =
                    "Đang vay · " + Coins(loan.RemainingPrincipalCoins) + " dư nợ\n" +
                    "Lãi " + Percent(loan.AnnualRateBps) + "/năm · đã trả " + loan.MonthsPaid + "/" +
                    loan.TermMonths + " kỳ\n" +
                    "Nghĩa vụ kỳ tới · " + Coins(Finance.InstallmentDue(loan)) +
                    (loan.OverdueCoins > 0 ? "\nNợ quá hạn · " + Coins(loan.OverdueCoins) : "") +
                    (loan.ConsecutiveShortfalls > 0
                        ? "\nĐã thiếu " + loan.ConsecutiveShortfalls + "/" + balance.LoanSealShortfalls +
                          " kỳ liền"
                        : "") +
                    (loan.Sealed ? "\nNGÂN HÀNG ĐÃ NIÊM PHONG NƯƠNG CHÈ" : "");
                _loanStatus.color = loan.Sealed || loan.ConsecutiveShortfalls > 0
                    ? GardenPalette.StateWarn : GardenPalette.TextPrimary;
                _repayButton.gameObject.SetActive(true);
                long payoff = Finance.PayoffAmount(loan);
                UiFactory.SetButtonCaption(_repayButton, "Trả hết nợ · " + Coins(payoff));
                UiFactory.SetInteractable(_repayButton, state.Coins >= payoff);
            }
            else
            {
                _loanStatus.text = canBorrow
                    ? "Chưa vay đồng nào. Vốn tự có chỉ đủ mở đầu."
                    : borrowReason;
                _loanStatus.color = GardenPalette.TextMuted;
                _repayButton.gameObject.SetActive(false);
            }

            // Du bao dung doanh thu va chi phi cua ky vua chot, khong dung mot con so uoc: nguoi
            // choi quyet dinh vay dua tren cai ho dang lam duoc, khong dua tren mot vi du.
            long revenue = 0, expense = 0;
            if (state.CashHistory.Count > 0)
            {
                var last = state.CashHistory[state.CashHistory.Count - 1];
                revenue = last.RevenueCoins;
                expense = last.ExpenseCoins;
            }
            var rows = Finance.Project(_loanAmountCoins, rateBps, _loanTermMonths, revenue, expense, 6);
            var projection = new System.Text.StringBuilder();
            projection.Append("Dự báo với doanh thu ").Append(Coins(revenue))
                      .Append(" và chi phí ").Append(Coins(expense)).Append(" mỗi kỳ:\n");
            long running = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                running += rows[i].NetCashCoins;
                projection.Append("Kỳ ").Append(rows[i].Month)
                          .Append(" · trả ").Append(Coins(rows[i].DebtServiceCoins))
                          .Append(" · ròng ").Append(Coins(rows[i].NetCashCoins))
                          .Append(" · lũy kế ").Append(Coins(running))
                          .Append(" · dư nợ ").Append(Coins(rows[i].DebtRemainingCoins))
                          .Append('\n');
            }
            _loanProjection.text = projection.ToString();
            _cashFlowChart.text = BuildCashFlowChart(state);
        }

        /// <summary>
        /// Do thi dong tien bang chinh dong chu.
        ///
        /// Mot do thi ve bang Image se can mot lop ve rieng va mot lan bo tri lai moi ky; mot day
        /// khoi vuong doc ngay ra duoc chieu cao tuong doi va ai am ai duong, ma no la mot Text.
        /// </summary>
        string BuildCashFlowChart(GameState state)
        {
            if (state.CashHistory.Count == 0)
                return "Chưa chốt kỳ nào. Kỳ đầu dài " +
                       (Finance.CycleMs(_session.Catalog.Balance) / 1000) + " giây.";

            long peak = 1;
            for (int i = 0; i < state.CashHistory.Count; i++)
            {
                long magnitude = Math.Abs(state.CashHistory[i].NetCashCoins);
                if (magnitude > peak) peak = magnitude;
            }

            var text = new System.Text.StringBuilder();
            for (int i = 0; i < state.CashHistory.Count; i++)
            {
                var row = state.CashHistory[i];
                int bars = (int)(Math.Abs(row.NetCashCoins) * 12 / peak);
                text.Append("Kỳ ").Append(row.Month.ToString("D2")).Append(' ');
                text.Append(row.NetCashCoins < 0 ? '−' : '+');
                for (int b = 0; b < bars; b++) text.Append('▌');
                text.Append(' ').Append(Coins(row.NetCashCoins));
                if (row.Shortfall) text.Append("  thiếu nợ");
                text.Append('\n');
            }
            return text.ToString();
        }

        // ---------------------------------------------------------------- 2. nong hoc

        void BuildAgronomyRows(Transform body)
        {
            SectionTitle(body, "Bốn mùa và chỉ số sinh hoá");
            _seasonNow = Monoblock(body, "SeasonNow", "");

            var seasons = new System.Text.StringBuilder();
            foreach (var profile in Agronomy.AllProfiles())
            {
                seasons.Append(profile.DisplayName).Append("  ·  ")
                       .Append(profile.MinTempC).Append('–').Append(profile.MaxTempC).Append("°C\n")
                       .Append("   ").Append(profile.Flavour).Append('\n')
                       .Append("   Theanine ").Append(profile.TheaninePermille / 10f).Append("%")
                       .Append("  ·  Polyphenol ").Append(profile.PolyphenolPermille / 10f).Append("%\n")
                       .Append("   Năng suất ").Append(profile.YieldPercent).Append("%")
                       .Append("  ·  Giá ").Append(profile.PricePercent).Append("%\n")
                       .Append("   ").Append(profile.Work).Append('\n');
            }
            Monoblock(body, "SeasonTable", seasons.ToString());
            Paragraph(body, "Biochem",
                "Theanine là vị ngọt êm, polyphenol là vị chát đượm. Hai chỉ số biến thiên ngược " +
                "nhau theo nhiệt độ, nên mùa là một nút xoay giữa nhiều-và-rẻ với ít-và-đắt.");

            SectionTitle(body, "Phân hạng tiêu chuẩn thu hái");
            var grades = new System.Text.StringBuilder();
            foreach (var grade in Agronomy.Grades())
            {
                var crop = _session.Catalog.Crop(grade.CropId);
                var recipe = _session.Catalog.RecipeForCrop(grade.CropId);
                grades.Append(grade.PluckName).Append("  ·  ").Append(grade.ProductName).Append('\n')
                      .Append("   Hái ").Append(grade.FreshGramsPerDay / 1000f).Append(" kg búp tươi/ngày")
                      .Append("  ·  ").Append(grade.FreshPerDryPermille / 1000f)
                      .Append(" kg tươi cho 1 kg khô\n")
                      .Append("   ").Append(grade.PricePerDryKiloThousandVnd).Append("k/kg khô  ·  ")
                      .Append(grade.Segment).Append('\n')
                      .Append("   Trong vườn: ").Append(crop.Yield).Append(" búp một ô, mỗi ")
                      .Append(recipe.PackedInputCount).Append(" búp đóng gói bán ")
                      .Append(Coins(recipe.PackedOutputCoins)).Append('\n');
            }
            Monoblock(body, "GradeTable", grades.ToString());
            Paragraph(body, "GradeNote",
                "Doanh thu một vụ của bốn phân hạng gần bằng nhau. Cái khác là số lượng: búp xô cho " +
                "gấp mười lăm lần số hàng phải chạy qua dây chuyền để kiếm cùng số tiền. Dây chuyền " +
                "đang là giới hạn thì hái non hơn là thắng.");

            SectionTitle(body, "Cơ chế rầy xanh");
            Monoblock(body, "Leafhopper",
                "Rầy xanh (Jacobiasca formosana) chích hút nhẹ vào cuối xuân đầu hè. Cây chè giải " +
                "phóng linalool và geraniol để gọi thiên địch, và chính hai chất đó làm nên Đông " +
                "Phương Mỹ Nhân — giá gấp bốn đến sáu lần.\n\n" +
                "Ô bị rầy xanh thu về \"lá rầy xanh\" chứ không phải búp chè thường, và vụ đó không " +
                "bị sâu bệnh. Muốn giữ được giá thì phải chế biến theo đường Đông Phương Mỹ Nhân " +
                "với mức oxy hoá 60–75%; sao theo đường trà xanh là mất gần hết phần chênh.");
        }

        void RefreshAgronomyPanel(GameState state)
        {
            var balance = _session.Catalog.Balance;
            var season = Cultivation.SeasonAt(balance, state.SimulationTimeMs);
            var profile = Agronomy.Profile(season);
            int leafhopperPlots = 0;
            for (int i = 0; i < state.Plots.Count; i++)
                if (state.Plots[i].Leafhopper) leafhopperPlots++;

            _seasonNow.text =
                "Đang là " + profile.DisplayName + "  ·  " + profile.Flavour + "\n" +
                "Năng suất chè " + profile.YieldPercent + "%  ·  giá thương phẩm " +
                profile.PricePercent + "%\n" +
                (profile.Leafhopper ? "Mùa rầy xanh: " + balance.LeafhopperChancePercent +
                                      "% số vụ chè được chích hút.\n" : "") +
                (leafhopperPlots > 0 ? "Đang có " + leafhopperPlots + " ô mang lá rầy xanh." : "");
            _seasonNow.color = leafhopperPlots > 0 ? GardenPalette.StateOk : GardenPalette.TextPrimary;
        }

        // ---------------------------------------------------------------- 3. can lua

        void BuildCraftRows(Transform body)
        {
            SectionTitle(body, "Đường chế biến");
            var routeRow = UiFactory.Node(body.gameObject.transform, "RouteRow");
            var routeLayout = UiFactory.HorizontalList(routeRow, 6f, new RectOffset(0, 0, 0, 0));
            routeLayout.childForceExpandWidth = true;
            var routeSize = routeRow.AddComponent<LayoutElement>();
            routeSize.minHeight = UiFactory.ButtonHeight;
            _craftRouteButtons.Clear();
            foreach (TeaRoute route in Enum.GetValues(typeof(TeaRoute)))
            {
                var captured = route;
                _craftRouteButtons.Add(UiFactory.TextButton(routeRow.transform, "Route_" + route,
                    Crafting.RouteName(route), delegate { Run(_session.SetCraftRoute(captured)); },
                    UiFactory.ButtonStyle.Quiet));
            }
            var routeNote = Paragraph(body, "RouteNote", "");
            routeNote.name = "RouteSummary";

            SectionTitle(body, "Thông số căn lửa");
            var dials = Card(body, "Dials");
            _craftValueLabels.Clear();
            _craftWindowLabels.Clear();
            var windows = Crafting.Windows(TeaRoute.Green);
            for (int i = 0; i < windows.Count; i++)
            {
                int index = i;
                _craftValueLabels.Add(StepperRow(dials.transform, "Dial" + i, ShortDialName(index),
                    delegate(int step) { StepCraft(index, step); }, CraftStepFor(index)));
                _craftWindowLabels.Add(Paragraph(dials.transform, "Window" + i, ""));
            }

            SectionTitle(body, "Trạng thái mẻ trà thành phẩm");
            var verdict = Card(body, "Verdict");
            _craftVerdictTitle = UiFactory.Label(verdict.transform, "VerdictTitle", "",
                                                 UiFactory.FontSizeRowTitle, TextAnchor.MiddleLeft,
                                                 GardenPalette.TextPrimary);
            _craftVerdictBody = Monoblock(verdict.transform, "VerdictBody", "");

            Paragraph(body, "CraftNote",
                "Khoảng chuẩn hẹp còn khoảng chấp nhận được rộng: không đọc sổ tay vẫn làm ra trà " +
                "bán được, chỉ là không bao giờ đạt giá cao nhất. Hệ số này nhân với hệ số mùa vụ " +
                "và hệ số kênh bán, và áp lên tiền thật của mỗi mẻ ở quầy trà.");
        }

        /// <summary>Buoc nhay cua tung nut. Nhiet do nhay 5 do, con lai nhay 1.</summary>
        static int CraftStepFor(int windowIndex)
        {
            return windowIndex == 0 ? 5 : 1;
        }

        /// <summary>
        /// Ten ngan cua tung nut, cho cot nhan cua hang stepper.
        ///
        /// <see cref="CraftWindow.DisplayName"/> la ten day du va no gay ba dong trong cot rong
        /// 100 px con lai sau hai cai nut va o gia tri. Ten day du van hien, o dong "chuan …"
        /// ngay ben duoi — cho do rong ca hang nen khong bao gio gay.
        /// </summary>
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

            var summary = transform.Find("CraftPanel/Viewport/Body/RouteSummary");
            if (summary != null)
            {
                var text = summary.GetComponent<Text>();
                if (text != null) text.text = Crafting.RouteSummary(settings.Route);
            }

            for (int i = 0; i < _craftValueLabels.Count && i < windows.Count; i++)
            {
                _craftValueLabels[i].text = values[i] + windows[i].Unit;
                int compare = Crafting.CompareToIdeal(windows[i], values[i]);
                _craftValueLabels[i].color = compare == 0 ? GardenPalette.StateOk
                                                          : GardenPalette.StateWarn;
                _craftWindowLabels[i].text = windows[i].DisplayName + " · chuẩn " +
                                             windows[i].IdealLow + "–" + windows[i].IdealHigh +
                                             windows[i].Unit;
            }

            bool leafhopperLeaves = HasLeafhopperLeaves(state);
            var balance = _session.Catalog.Balance;
            var verdict = Crafting.Evaluate(settings, leafhopperLeaves,
                                            balance.PremiumBatchPricePercent,
                                            balance.FlawedBatchPricePercent);
            int price = Crafting.BatchPricePercent(verdict, leafhopperLeaves,
                                                   balance.OrientalBeautyFallbackPercent);
            _craftVerdictTitle.text = verdict.Title;
            _craftVerdictTitle.color = verdict.Quality == BatchQuality.Flawed ? GardenPalette.StateWarn
                : verdict.Quality == BatchQuality.Premium ? GardenPalette.StateOk
                : GardenPalette.TextPrimary;
            _craftVerdictBody.text =
                verdict.ProductName + "\n" +
                "Sắc nước · " + verdict.Liquor + "\n" +
                "Hương vị · " + verdict.Aroma + "\n" +
                "Rủi ro bảo quản · " + verdict.StorageRisk + "\n" +
                "Hệ số giá · " + price + "%\n" +
                verdict.Note;
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

            SectionTitle(body, "Hình thức kinh doanh");
            _legalStatus = Monoblock(body, "LegalStatus", "");

            foreach (var option in Compliance.Entities())
            {
                var entity = option.Entity;
                var card = Card(body, "Entity_" + entity);
                UiFactory.Label(card.transform, "Name", option.DisplayName + "  ·  " + option.Stage,
                                UiFactory.FontSizeRowTitle, TextAnchor.MiddleLeft, GardenPalette.TextPrimary);
                Monoblock(card.transform, "Detail",
                    "Đăng ký · " + option.Authority + " (" + option.MinDays + "–" + option.MaxDays + " ngày)\n" +
                    "Nghĩa vụ thuế · " +
                    (option.RevenueTaxBps > 0 ? "thuế khoán " + Percent(option.RevenueTaxBps) + " doanh thu"
                                              : "TNDN " + Percent(option.ProfitTaxBps) + " lợi nhuận ròng") +
                    (option.VatBps > 0 ? " + GTGT " + Percent(option.VatBps) + " (thu của người mua, khấu trừ)"
                                       : "") + "\n" +
                    (option.BookkeepingCoinsPerCycle > 0
                        ? "Kế toán · " + Coins(option.BookkeepingCoinsPerCycle) + " mỗi kỳ\n" : "") +
                    "Sổ sách · " + option.Bookkeeping + "\n" +
                    "Được · " + option.Advantage + "\n" +
                    "Mất · " + option.Drawback);
                var button = UiFactory.TextButton(card.transform, "Register",
                    "Nộp hồ sơ · " + Coins(option.FeeCoins),
                    delegate { Run(_session.RegisterEntity(entity)); });
                if (entity == BusinessEntity.Hkd) _hkdButton = button; else _tnhhButton = button;
            }

            SectionTitle(body, "Giấy an toàn thực phẩm");
            Paragraph(body, "FoodSafetyNote",
                "Thông tư 38/2018/TT-BNNPTNT. Thẩm định " + balance.FoodSafetyMinDays + "–" +
                balance.FoodSafetyMaxDays + " ngày in-game, nên phải nộp trước khi cần đến nó. " +
                "Hoạt động khi chưa có giấy là bị đình chỉ.");
            _foodSafetyButton = UiFactory.TextButton(body, "FoodSafety",
                "Xin giấy · " + Coins(balance.FoodSafetyFeeCoins),
                delegate { Run(_session.ApplyFoodSafety()); });

            SectionTitle(body, "Xưởng sơ chế một chiều");
            var zones = new System.Text.StringBuilder();
            foreach (var zone in Compliance.Zones())
            {
                zones.Append(zone.Order).Append(". ").Append(zone.DisplayName).Append('\n')
                     .Append("   ").Append(zone.Detail).Append('\n');
            }
            Monoblock(body, "Zones", zones.ToString());
            Paragraph(body, "ZoneNote",
                "Nguyên liệu và công nhân đi theo một chiều duy nhất để chống nhiễm chéo. Trong " +
                "game: để hở một chặng giữa dây chuyền là nguyên liệu phải vòng ngược lại qua khu " +
                "đã xử lý, và đó là vi phạm. Thiếu máy ở cuối dây chuyền chỉ là chưa xây xong.");

            SectionTitle(body, "Chế tài xử phạt");
            var fines = new System.Text.StringBuilder();
            foreach (var violation in Compliance.Violations())
            {
                fines.Append(violation.DisplayName).Append('\n')
                     .Append("   Phạt ").Append(Million(violation.MinFineCoins)).Append('–')
                     .Append(Million(violation.MaxFineCoins))
                     .Append(violation.Suspends ? " kèm đình chỉ" : "").Append('\n')
                     .Append("   ").Append(violation.Remedy).Append('\n');
            }
            Monoblock(body, "Fines", fines.ToString());
            _inspectionLog = Monoblock(body, "InspectionLog", "");
        }

        void RefreshLegalPanel(GameState state)
        {
            var compliance = state.Compliance;
            var option = Compliance.Entity(compliance.Entity);
            long dayMs = Compliance.DayMs(_session.Catalog.Balance);

            var status = new System.Text.StringBuilder();
            status.Append("Hình thức hiện tại · ")
                  .Append(option != null ? option.DisplayName : "chưa đăng ký").Append('\n');
            if (compliance.PendingEntity != BusinessEntity.None)
            {
                long left = Math.Max(0, compliance.EntityReadyAtMs - state.SimulationTimeMs);
                status.Append("Hồ sơ đang thẩm định · còn ").Append(left / dayMs + 1).Append(" ngày\n");
            }
            status.Append("Giấy an toàn thực phẩm · ")
                  .Append(compliance.FoodSafetyCertified ? "đã có"
                        : compliance.FoodSafetyPending
                            ? "đang thẩm định, còn " +
                              (Math.Max(0, compliance.FoodSafetyReadyAtMs - state.SimulationTimeMs) / dayMs + 1) +
                              " ngày"
                            : "chưa có").Append('\n');
            status.Append("Đồ bảo hộ y tế · ")
                  .Append(compliance.ProtectiveGear ? "đã có" : "chưa mua (ở bảng Nâng cấp)").Append('\n');
            status.Append("Dây chuyền một chiều · ")
                  .Append(Compliance.OneWayRespected(_session.Catalog, state) ? "đạt" : "CÓ LỖ HỔNG").Append('\n');
            status.Append("Đã nộp thuế · ").Append(Coins(compliance.TotalTaxCoins))
                  .Append("  ·  đã bị phạt · ").Append(Coins(compliance.TotalFinesCoins));
            if (compliance.Suspended(state.SimulationTimeMs))
                status.Append("\nXƯỞNG ĐANG BỊ ĐÌNH CHỈ");
            _legalStatus.text = status.ToString();
            _legalStatus.color = compliance.Suspended(state.SimulationTimeMs)
                ? GardenPalette.StateWarn : GardenPalette.TextPrimary;

            string reason;
            UiFactory.SetInteractable(_hkdButton, _session.CanRegisterEntity(BusinessEntity.Hkd, out reason));
            UiFactory.SetInteractable(_tnhhButton, _session.CanRegisterEntity(BusinessEntity.Tnhh, out reason));
            UiFactory.SetInteractable(_foodSafetyButton, _session.CanApplyFoodSafety(out reason));

            long nextInspection = Math.Max(0, compliance.NextInspectionAtMs - state.SimulationTimeMs);
            _inspectionLog.text =
                "Đã kiểm tra " + compliance.InspectionCount + " lần  ·  kỳ tới sau " +
                (nextInspection / 1000) + " giây\n" +
                (compliance.LastInspectionIndex > 0
                    ? "Biên bản gần nhất · " +
                      (string.IsNullOrEmpty(compliance.LastViolationIds)
                          ? "không vi phạm"
                          : compliance.LastViolationIds + ", phạt " + Coins(compliance.LastFineCoins))
                    : "Chưa có biên bản nào.");
        }

        // ---------------------------------------------------------------- 5. mo hinh kinh doanh

        void BuildModelRows(Transform body)
        {
            SectionTitle(body, "Lộ trình ba giai đoạn");
            _phaseLabel = Monoblock(body, "Phase", "");

            SectionTitle(body, "Ba phân nhánh khởi nghiệp");
            _branchButtons.Clear();
            _branchStatus.Clear();
            foreach (var branch in Branches.All())
            {
                var captured = branch.Branch;
                var card = Card(body, "Branch_" + captured);
                UiFactory.Label(card.transform, "Name",
                    branch.DisplayName + "  ·  biên " + branch.MinMarginPercent + "–" +
                    branch.MaxMarginPercent + "%",
                    UiFactory.FontSizeRowTitle, TextAnchor.MiddleLeft, GardenPalette.TextPrimary);
                Monoblock(card.transform, "Detail",
                    branch.Summary + "\n" +
                    "CAPEX · " + branch.MinCapexMillionVnd + "–" + branch.MaxCapexMillionVnd + " triệu\n" +
                    "Vòng quay vốn · " + branch.MinCashCycleDays + "–" + branch.MaxCashCycleDays + " ngày\n" +
                    "Trong game · giá " + branch.PricePercent + "%" +
                    (branch.PayoutDelayDays > 0 ? ", tiền về sau " + branch.PayoutDelayDays + " ngày"
                                                : ", thu tiền ngay") +
                    (branch.SellsUnpacked ? ", bán được trà mộc" : "") +
                    (branch.WinterIncome ? ", có thu cả mùa đông" : "") +
                    (string.IsNullOrEmpty(branch.Requirement) ? "" : "\n" + branch.Requirement));
                _branchStatus.Add(Paragraph(card.transform, "Status", ""));
                _branchButtons.Add(UiFactory.TextButton(card.transform, "Unlock",
                    "Mở nhánh · " + Coins(branch.UnlockCostCoins),
                    delegate { Run(_session.UnlockBranch(captured)); }));
            }

            SectionTitle(body, "Kênh bán chính");
            var channelRow = UiFactory.Node(body, "ChannelRow");
            var channelLayout = UiFactory.HorizontalList(channelRow, 6f, new RectOffset(0, 0, 0, 0));
            channelLayout.childForceExpandWidth = true;
            var channelSize = channelRow.AddComponent<LayoutElement>();
            channelSize.minHeight = UiFactory.ButtonHeight;
            _channelButtons.Clear();
            var channels = new[] { BusinessBranch.None, BusinessBranch.BulkB2B, BusinessBranch.ArtisanalDtc };
            foreach (var channel in channels)
            {
                var captured = channel;
                string caption = channel == BusinessBranch.None
                    ? "Bán như cũ" : Branches.Definition(channel).DisplayName;
                _channelButtons.Add(UiFactory.TextButton(channelRow.transform, "Channel_" + channel,
                    caption, delegate { Run(_session.SetSalesChannel(captured)); },
                    UiFactory.ButtonStyle.Quiet));
            }
            Paragraph(body, "ChannelNote",
                "Ba nhánh không loại trừ nhau — kết hợp lại mới tối ưu được dòng tiền và giảm rủi ro " +
                "mùa vụ. Cái phải chọn là kênh bán chính cho mỗi mẻ trà, và đổi được bất cứ lúc nào. " +
                "Du lịch trải nghiệm không phải một kênh bán trà: nó là một nguồn thu riêng mỗi kỳ.");
        }

        void RefreshModelPanel(GameState state)
        {
            int phase = Branches.PhaseOf(state);
            long tourism = Branches.FarmstayIncome(_session.Catalog.Balance, state);
            long receivable = 0;
            for (int i = 0; i < state.Receivables.Count; i++) receivable += state.Receivables[i].AmountCoins;

            _phaseLabel.text =
                Branches.PhaseName(phase) + "\n" + Branches.PhaseGoal(phase) + "\n" +
                (tourism > 0 ? "Thu du lịch mỗi kỳ · " + Coins(tourism) + "\n" : "") +
                (receivable > 0
                    ? "Đang chờ khách bán lẻ thanh toán · " + Coins(receivable) +
                      " trong " + state.Receivables.Count + " lô"
                    : "Không có khoản nào chờ thu.");

            var all = Branches.All();
            for (int i = 0; i < all.Count && i < _branchButtons.Count; i++)
            {
                var branch = all[i].Branch;
                bool owned = state.UnlockedBranches.Contains(branch);
                string reason;
                bool can = !owned && _session.CanUnlockBranch(branch, out reason);
                if (owned) reason = "Đã mở.";
                else if (can) reason = "";
                else _session.CanUnlockBranch(branch, out reason);

                _branchButtons[i].gameObject.SetActive(!owned);
                UiFactory.SetInteractable(_branchButtons[i], can);
                _branchStatus[i].text = owned ? "Đã mở." : reason;
                _branchStatus[i].color = owned ? GardenPalette.StateOk : GardenPalette.StateWarn;
            }

            var channels = new[] { BusinessBranch.None, BusinessBranch.BulkB2B, BusinessBranch.ArtisanalDtc };
            for (int i = 0; i < _channelButtons.Count && i < channels.Length; i++)
            {
                // Ba trang thai, ba y nghia khac nhau: dang chon, chon duoc, va chua mo nhanh.
                // SetButtonState tu tat interactable cho Selected va Disabled, nen mot lan goi
                // la du — goi them SetInteractable sau do se ghi de mau cua Selected.
                bool selected = state.SalesChannel == channels[i];
                bool available = channels[i] == BusinessBranch.None ||
                                 state.UnlockedBranches.Contains(channels[i]);
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
