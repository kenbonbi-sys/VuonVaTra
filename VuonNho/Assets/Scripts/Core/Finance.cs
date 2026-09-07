using System;
using System.Collections.Generic;

namespace VuonNho.Core
{
    /// <summary>Ba lua chon von: khong vay, vay tin chap, vay the chap.</summary>
    public enum LoanKind
    {
        None = 0,
        Unsecured = 1,
        Secured = 2
    }

    /// <summary>
    /// Mot san pham vay cua ngan hang, theo muc 1 cua ban mo phong khoi nghiep tra
    /// (Nghi dinh 55/2015, 116/2018 va 156/2025).
    ///
    /// Lai suat la mot **khoang** chu khong phai mot con so: vay dai hon thi ngan hang doi lai
    /// cao hon. Nho vay chon thoi han la mot danh doi thuc — dai thi tra nhe moi ky nhung tong
    /// lai nhieu hon — chu khong phai mot cai o nhap lieu chi co mot dap an dung.
    /// </summary>
    public sealed class LoanOffer
    {
        public LoanKind Kind;
        public string DisplayName;

        /// <summary>Dieu kien ngoai tien: "Can UBND xa xac nhan du an", "70% dinh gia so do".</summary>
        public string Requirement;

        public long MaxPrincipalCoins;

        /// <summary>Lai suat nam, tinh bang diem co ban. 850 = 8,5%/nam.</summary>
        public int MinRateBps;
        public int MaxRateBps;

        public int MinTermMonths;
        public int MaxTermMonths;
    }

    /// <summary>
    /// Mot ky (mot "thang" trong game) da dong lai. Muoi hai ban ghi gan nhat ve thanh do thi
    /// dong tien va du no cua ban mo phong.
    /// </summary>
    public sealed class CashCycleRecord
    {
        /// <summary>Thang thu bao nhieu ke tu luc bat dau van, dem tu 1.</summary>
        public int Month;
        public long RevenueCoins;
        public long ExpenseCoins;

        /// <summary>Goc cong lai da tra trong ky. Nam rieng khoi ExpenseCoins de doc duoc PMT.</summary>
        public long DebtServiceCoins;

        public long NetCashCoins;
        public long DebtRemainingCoins;

        /// <summary>Ky nay khong tra du nghia vu. Ba ky lien tiep nhu vay la ngan hang siet no.</summary>
        public bool Shortfall;

        public CashCycleRecord Clone()
        {
            return (CashCycleRecord)MemberwiseClone();
        }
    }

    /// <summary>
    /// Khoan vay dang chay va so du no. Nam trong save vi no la mot nghia vu chu khong phai
    /// mot con so hien thi: nap lai giua ky phai con dung so ky da tra va dung moc den han.
    /// </summary>
    public sealed class LoanState
    {
        public LoanKind Kind;

        /// <summary>Goc ban dau L0. Giu lai de tinh goc phai tra moi thang = L0 / n.</summary>
        public long PrincipalCoins;

        public long RemainingPrincipalCoins;
        public int AnnualRateBps;
        public int TermMonths;
        public int MonthsPaid;

        /// <summary>Moc den han cua ky ke tiep, theo thoi gian mo phong.</summary>
        public long NextDueAtMs;

        /// <summary>So ky lien tiep khong tra du. Reset ve 0 ngay khi tra du mot ky.</summary>
        public int ConsecutiveShortfalls;

        /// <summary>Phan con thieu da cong don lai. Phai tra het moi mo duoc niem phong.</summary>
        public long OverdueCoins;

        /// <summary>Tong lai da tra. Chi de bao cao, khong tham gia tinh toan nao.</summary>
        public long InterestPaidCoins;

        /// <summary>Ngan hang da siet no va niem phong nuong che.</summary>
        public bool Sealed;
        public long SealedAtMs;

        public bool Active
        {
            get { return Kind != LoanKind.None && RemainingPrincipalCoins > 0; }
        }

        public LoanState Clone()
        {
            return (LoanState)MemberwiseClone();
        }
    }

    /// <summary>
    /// Luat tai chinh: han muc vay, nghia vu tra no hang thang va nguong siet no.
    ///
    /// Tat ca deu la ham thuan tuy tren so nguyen, giong <see cref="Cultivation"/>: cung dau vao
    /// cho cung dau ra, khong dong ho that, khong Random. Nho vay bang mo phong ma nguoi choi
    /// nhin trong HUD va so tien that bi tru khi den han la cung mot phep tinh, khong the lech.
    ///
    /// Don vi tien trong game la **xu**, va mot xu la 100.000 VNĐ
    /// (<see cref="BalanceConfig.CoinsPerMillionVnd"/> = 10). Von tu co 200 trieu cua ban mo
    /// phong vi the la 2.000 xu, han muc the chap 350 trieu la 3.500 xu. Doi ti le o mot cho duy
    /// nhat do se doi het moi con so tai chinh hien ra man hinh.
    /// </summary>
    public static class Finance
    {
        /// <summary>Mot ky tra no dai bao lau. Mot mua chia lam <c>MonthsPerSeason</c> thang.</summary>
        public static long CycleMs(BalanceConfig balance)
        {
            if (balance.MonthsPerSeason <= 0) return balance.SeasonLengthMs;
            long ms = balance.SeasonLengthMs / balance.MonthsPerSeason;
            return ms < 1 ? 1 : ms;
        }

        public static List<LoanOffer> Offers(BalanceConfig balance)
        {
            return new List<LoanOffer>
            {
                new LoanOffer
                {
                    Kind = LoanKind.Unsecured,
                    DisplayName = "Vay tín chấp",
                    Requirement = "Cần UBND xã xác nhận dự án",
                    MaxPrincipalCoins = balance.UnsecuredLoanCapCoins,
                    MinRateBps = balance.UnsecuredMinRateBps,
                    MaxRateBps = balance.UnsecuredMaxRateBps,
                    MinTermMonths = balance.LoanMinTermMonths,
                    MaxTermMonths = balance.LoanMaxTermMonths
                },
                new LoanOffer
                {
                    Kind = LoanKind.Secured,
                    DisplayName = "Vay thế chấp",
                    Requirement = "70% định giá sổ đỏ, cần xong giấy an toàn thực phẩm",
                    MaxPrincipalCoins = balance.SecuredLoanCapCoins,
                    MinRateBps = balance.SecuredMinRateBps,
                    MaxRateBps = balance.SecuredMaxRateBps,
                    MinTermMonths = balance.LoanMinTermMonths,
                    MaxTermMonths = balance.LoanMaxTermMonths
                }
            };
        }

        public static LoanOffer Offer(BalanceConfig balance, LoanKind kind)
        {
            var offers = Offers(balance);
            for (int i = 0; i < offers.Count; i++)
                if (offers[i].Kind == kind) return offers[i];
            return null;
        }

        /// <summary>
        /// Lai suat cho mot thoi han cu the: noi thang giua hai dau cua khoang.
        /// Vay ngan nhat duoc muc thap nhat, vay dai nhat phai chiu muc cao nhat.
        /// </summary>
        public static int RateBpsFor(LoanOffer offer, int termMonths)
        {
            if (offer == null) return 0;
            int span = offer.MaxTermMonths - offer.MinTermMonths;
            if (span <= 0) return offer.MinRateBps;
            int over = Cultivation.Clamp(termMonths, offer.MinTermMonths, offer.MaxTermMonths) - offer.MinTermMonths;
            return offer.MinRateBps + (offer.MaxRateBps - offer.MinRateBps) * over / span;
        }

        /// <summary>
        /// Goc phai tra o ky thu <paramref name="monthIndex"/> (dem tu 0).
        ///
        /// Ky cuoi cung ganh phan le, nen tong goc cua ca <c>n</c> ky luon dung bang L0. Chia deu
        /// roi lam tron xuong o moi ky se de lai mot vai xu khong bao gio duoc tra, va khoan vay
        /// se khong bao gio dong duoc.
        /// </summary>
        public static long PrincipalDue(long principalCoins, int termMonths, int monthIndex)
        {
            if (termMonths <= 0) return principalCoins;
            long even = principalCoins / termMonths;
            if (monthIndex >= termMonths - 1) return principalCoins - even * (termMonths - 1);
            return even;
        }

        /// <summary>Lai cua mot ky, tinh tren du no dau ky. Lam tron len de ngan hang khong lo.</summary>
        public static long InterestDue(long remainingPrincipalCoins, int annualRateBps)
        {
            if (remainingPrincipalCoins <= 0 || annualRateBps <= 0) return 0;
            long numerator = remainingPrincipalCoins * annualRateBps;
            long denominator = 10000L * 12L;
            return (numerator + denominator - 1) / denominator;
        }

        /// <summary>Tong nghia vu cua ky ke tiep: goc cong lai.</summary>
        public static long InstallmentDue(LoanState loan)
        {
            if (loan == null || !loan.Active) return 0;
            long principal = PrincipalDue(loan.PrincipalCoins, loan.TermMonths, loan.MonthsPaid);
            if (principal > loan.RemainingPrincipalCoins) principal = loan.RemainingPrincipalCoins;
            return principal + InterestDue(loan.RemainingPrincipalCoins, loan.AnnualRateBps);
        }

        /// <summary>
        /// Mo phong 12 ky toi voi doanh thu va chi phi van hanh du kien — chinh la bang va do thi
        /// cua muc 1 trong ban mo phong.
        ///
        /// Chay tren mot ban sao cua so du no chu khong dong vao <paramref name="loan"/>: day la
        /// mot ban du bao de nguoi choi xem truoc khi vay, khong phai mot buoc cua mo phong.
        /// </summary>
        public static List<CashCycleRecord> Project(long principalCoins, int annualRateBps, int termMonths,
                                                    long revenuePerCycle, long opexPerCycle, int cycles)
        {
            var rows = new List<CashCycleRecord>();
            long remaining = principalCoins;
            long cash = 0;
            for (int month = 0; month < cycles; month++)
            {
                long principal = month < termMonths ? PrincipalDue(principalCoins, termMonths, month) : 0;
                if (principal > remaining) principal = remaining;
                long interest = InterestDue(remaining, annualRateBps);
                long service = principal + interest;
                long net = revenuePerCycle - opexPerCycle - service;
                remaining -= principal;
                cash += net;
                rows.Add(new CashCycleRecord
                {
                    Month = month + 1,
                    RevenueCoins = revenuePerCycle,
                    ExpenseCoins = opexPerCycle,
                    DebtServiceCoins = service,
                    NetCashCoins = net,
                    DebtRemainingCoins = remaining,
                    Shortfall = cash < 0
                });
            }
            return rows;
        }

        /// <summary>
        /// Vay duoc khong, va vi sao khong. Ly do tra ve la cau se hien ra man hinh.
        ///
        /// Da vay roi thi phai tra xong moi vay tiep: hai khoan vay song song bien man hinh no
        /// thanh mot bang ke toan, va ca cai gia cua viec vay — mot con so phai tra moi ky — tan
        /// ra thanh nhieu con so nho khong ai theo noi.
        /// </summary>
        public static bool CanBorrow(BalanceConfig balance, GameState state, LoanKind kind,
                                     long principalCoins, int termMonths, out string reason)
        {
            reason = null;
            if (state.Loan.Sealed)
            {
                reason = "Nương chè đang bị niêm phong. Trả hết nợ quá hạn trước đã.";
                return false;
            }
            if (state.Loan.Active)
            {
                reason = "Đang còn một khoản vay. Trả xong khoản này rồi mới vay tiếp được.";
                return false;
            }
            var offer = Offer(balance, kind);
            if (offer == null)
            {
                reason = "Ngân hàng không có gói vay này.";
                return false;
            }
            if (kind == LoanKind.Secured && !state.Compliance.FoodSafetyCertified)
            {
                reason = "Vay thế chấp cần giấy an toàn thực phẩm để ngân hàng định giá dự án.";
                return false;
            }
            if (principalCoins <= 0)
            {
                reason = "Số tiền vay phải lớn hơn 0.";
                return false;
            }
            if (principalCoins > offer.MaxPrincipalCoins)
            {
                reason = offer.DisplayName + " chỉ tối đa " + offer.MaxPrincipalCoins.ToString("N0") +
                         DefaultContent.CoinGlyph + ".";
                return false;
            }
            if (termMonths < offer.MinTermMonths || termMonths > offer.MaxTermMonths)
            {
                reason = "Thời hạn phải từ " + offer.MinTermMonths + " đến " + offer.MaxTermMonths + " tháng.";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Nhan tien vay. Goi sau khi <see cref="CanBorrow"/> da dong y.
        ///
        /// Tien vao tui **khong** tinh la doanh thu: mot khoan vay khong lam vuon giau hon, no chi
        /// doi mot cuc tien bay gio lay mot chuoi nghia vu ve sau. Cong vao doanh thu thi do thi
        /// dong tien se bao mot thang cuc lai, va do dung la cai bay ma muc 1 cua ban mo phong
        /// muon nguoi choi nhin thay.
        /// </summary>
        public static void Borrow(BalanceConfig balance, GameState state, LoanKind kind,
                                  long principalCoins, int termMonths, long atMs)
        {
            var offer = Offer(balance, kind);
            var loan = state.Loan;
            loan.Kind = kind;
            loan.PrincipalCoins = principalCoins;
            loan.RemainingPrincipalCoins = principalCoins;
            loan.AnnualRateBps = RateBpsFor(offer, termMonths);
            loan.TermMonths = termMonths;
            loan.MonthsPaid = 0;
            loan.NextDueAtMs = atMs + CycleMs(balance);
            loan.ConsecutiveShortfalls = 0;
            loan.OverdueCoins = 0;
            loan.InterestPaidCoins = 0;
            state.Coins += principalCoins;
        }

        /// <summary>
        /// Tra het no truoc han. Tra ca goc con lai, khong tinh them lai cua ky chua den han —
        /// tra som la mot lua chon tot, khong phai mot cai bi phat.
        /// </summary>
        public static long PayoffAmount(LoanState loan)
        {
            if (loan == null) return 0;
            return loan.RemainingPrincipalCoins + loan.OverdueCoins;
        }
    }
}
