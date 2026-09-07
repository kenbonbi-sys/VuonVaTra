using System.Collections.Generic;

namespace VuonNho.Core
{
    /// <summary>Ba phan nhanh kinh doanh cua muc 5 trong ban mo phong.</summary>
    public enum BusinessBranch
    {
        None = 0,
        /// <summary>Gia cong tho B2B: tra moc dong bao 25–50kg ban si.</summary>
        BulkB2B = 1,
        /// <summary>Ban le thu cong: dong hop tinh xao, thuong hieu ban dia.</summary>
        ArtisanalDtc = 2,
        /// <summary>Du lich trai nghiem: tour hai tra, workshop, luu tru.</summary>
        Farmstay = 3
    }

    /// <summary>
    /// Mot phan nhanh. Ba con so cua bang so sanh — bien loi nhuan, CAPEX, vong quay von — o day
    /// khong phai de trang tri: chung la **ba cai nut** ma phan nhanh do xoay trong mo phong.
    ///
    /// Bien loi nhuan thanh <see cref="PricePercent"/>. Vong quay von thanh
    /// <see cref="PayoutDelayDays"/> — tien ve cham thi ky nao cung phai co du tien mat tra no
    /// trong khi hang da ban roi ma chua thu. CAPEX thanh <see cref="UnlockCostCoins"/>.
    /// </summary>
    public sealed class BranchDefinition
    {
        public BusinessBranch Branch;
        public string DisplayName;
        public string Summary;

        public int MinMarginPercent;
        public int MaxMarginPercent;
        public long MinCapexMillionVnd;
        public long MaxCapexMillionVnd;
        public int MinCashCycleDays;
        public int MaxCashCycleDays;

        public long UnlockCostCoins;

        /// <summary>Dieu kien ngoai tien. Rong nghia la chi can du tien.</summary>
        public string Requirement;

        /// <summary>He so gia ban tra qua kenh nay, phan tram.</summary>
        public int PricePercent;

        /// <summary>Tien ve sau bao nhieu ngay. 0 = thu ngay.</summary>
        public int PayoutDelayDays;

        /// <summary>Ban duoc tu chang say, khong can qua dong goi.</summary>
        public bool SellsUnpacked;

        /// <summary>Co doanh thu ca trong mua che ngu dong.</summary>
        public bool WinterIncome;
    }

    /// <summary>Mot khoan da ban ma chua thu tien. Kenh ban le thu cong sinh ra chung.</summary>
    public sealed class Receivable
    {
        public long AmountCoins;
        public long DueAtMs;

        public Receivable Clone()
        {
            return (Receivable)MemberwiseClone();
        }
    }

    /// <summary>
    /// Cay phat trien phan nhanh kinh doanh va lo trinh ba giai doan.
    ///
    /// Ba nhanh **khong loai tru nhau** — dung nhu ban mo phong noi: ket hop lai moi toi uu duoc
    /// dong tien va giam rui ro mua vu. Cai phai chon la **kenh ban chinh** cho moi me tra, va
    /// lua chon do doi duoc bat cu luc nao.
    /// </summary>
    public static class Branches
    {
        public static List<BranchDefinition> All()
        {
            return new List<BranchDefinition>
            {
                new BranchDefinition
                {
                    Branch = BusinessBranch.BulkB2B,
                    DisplayName = "Gia công thô B2B",
                    Summary = "Chế biến nhanh thành trà mộc, đóng bao lớn bán sỉ. " +
                              "Thu hồi vốn cực nhanh nên trang trải được nợ ngân hàng ngay từ đầu.",
                    MinMarginPercent = 10, MaxMarginPercent = 15,
                    MinCapexMillionVnd = 75, MaxCapexMillionVnd = 95,
                    MinCashCycleDays = 3, MaxCashCycleDays = 7,
                    UnlockCostCoins = 250,
                    PricePercent = 65,
                    PayoutDelayDays = 0,
                    SellsUnpacked = true
                },
                new BranchDefinition
                {
                    Branch = BusinessBranch.ArtisanalDtc,
                    DisplayName = "Bán lẻ thủ công DTC",
                    Summary = "Tuyển búp phẩm cấp cao, đóng hộp tinh xảo, kể câu chuyện bản địa. " +
                              "Biên lợi nhuận rất cao nhưng tiền về chậm vì tiếp thị và tồn kho bao bì.",
                    MinMarginPercent = 50, MaxMarginPercent = 70,
                    MinCapexMillionVnd = 90, MaxCapexMillionVnd = 130,
                    MinCashCycleDays = 30, MaxCashCycleDays = 60,
                    UnlockCostCoins = 900,
                    Requirement = "Cần giấy an toàn thực phẩm và máy đóng gói.",
                    PricePercent = 165,
                    PayoutDelayDays = 45
                },
                new BranchDefinition
                {
                    Branch = BusinessBranch.Farmstay,
                    DisplayName = "Du lịch trải nghiệm",
                    Summary = "Tour hái trà, hướng dẫn chế biến và lưu trú. CAPEX cao nhất nhưng " +
                              "thu tiền mặt ngay, và giải quyết được mùa đông chè không có sản lượng.",
                    MinMarginPercent = 70, MaxMarginPercent = 85,
                    MinCapexMillionVnd = 180, MaxCapexMillionVnd = 250,
                    MinCashCycleDays = 0, MaxCashCycleDays = 1,
                    UnlockCostCoins = 2200,
                    Requirement = "Cần ít nhất 6 món cảnh quan trong vườn để khách có chỗ đi.",
                    PricePercent = 100,
                    PayoutDelayDays = 0,
                    WinterIncome = true
                }
            };
        }

        public static BranchDefinition Definition(BusinessBranch branch)
        {
            var all = All();
            for (int i = 0; i < all.Count; i++)
                if (all[i].Branch == branch) return all[i];
            return null;
        }

        /// <summary>So mon canh quan phai co truoc khi mo duoc du lich trai nghiem.</summary>
        public const int FarmstayDecorationsRequired = 6;

        public static bool CanUnlock(ContentCatalog catalog, GameState state, BusinessBranch branch,
                                     out string reason)
        {
            reason = null;
            var definition = Definition(branch);
            if (definition == null)
            {
                reason = "Không có phân nhánh này.";
                return false;
            }
            if (state.UnlockedBranches.Contains(branch))
            {
                reason = "Đã mở.";
                return false;
            }
            if (branch == BusinessBranch.ArtisanalDtc)
            {
                if (!state.Compliance.FoodSafetyCertified)
                {
                    reason = "Bán lẻ thủ công cần giấy an toàn thực phẩm.";
                    return false;
                }
                var pack = state.Station(DefaultStages.Pack);
                if (pack == null || !pack.Owned)
                {
                    reason = "Cần máy sàng và đóng gói để đóng hộp bán lẻ.";
                    return false;
                }
            }
            if (branch == BusinessBranch.Farmstay && state.Decorations.Count < FarmstayDecorationsRequired)
            {
                reason = "Cần " + FarmstayDecorationsRequired + " món cảnh quan trong vườn, đang có " +
                         state.Decorations.Count + ".";
                return false;
            }
            if (state.Coins < definition.UnlockCostCoins)
            {
                reason = "Thiếu " + (definition.UnlockCostCoins - state.Coins) + " " + DefaultContent.CoinGlyph;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Thu nhap du lich moi ky. Tinh theo so mon canh quan: tai dau tu canh quan nuong che la
        /// dung cai ma giai doan 3 cua lo trinh yeu cau, nen no phai la thu lam tang tien that.
        /// </summary>
        public static long FarmstayIncome(BalanceConfig balance, GameState state)
        {
            if (!state.UnlockedBranches.Contains(BusinessBranch.Farmstay)) return 0;
            long income = balance.FarmstayBaseIncomeCoins +
                          balance.FarmstayIncomePerDecorationCoins * state.Decorations.Count;
            return income < 0 ? 0 : income;
        }

        /// <summary>
        /// He so gia cua kenh ban dang chon. Kenh chua mo thi khong co he so nao — ban nhu cu.
        /// </summary>
        public static int PricePercentFor(GameState state)
        {
            if (state.SalesChannel == BusinessBranch.None) return 100;
            if (!state.UnlockedBranches.Contains(state.SalesChannel)) return 100;
            var definition = Definition(state.SalesChannel);
            return definition == null ? 100 : definition.PricePercent;
        }

        /// <summary>Giai doan hien tai cua lo trinh, 1 den 3.</summary>
        public static int PhaseOf(GameState state)
        {
            if (state.UnlockedBranches.Contains(BusinessBranch.Farmstay)) return 3;
            if (state.UnlockedBranches.Contains(BusinessBranch.ArtisanalDtc)) return 2;
            return 1;
        }

        public static string PhaseName(int phase)
        {
            switch (phase)
            {
                case 3: return "Giai đoạn 3 — Hệ sinh thái";
                case 2: return "Giai đoạn 2 — Nâng biên lợi nhuận";
                default: return "Giai đoạn 1 — Sinh tồn";
            }
        }

        public static string PhaseGoal(int phase)
        {
            switch (phase)
            {
                case 3:
                    return "Tái đầu tư cảnh quan nương chè và mở workshop trà đạo để mùa đông vẫn có doanh thu.";
                case 2:
                    return "Trích dòng tiền đầu tư bao bì DTC và chuẩn an toàn thực phẩm để nâng biên lên 60%.";
                default:
                    return "Tập trung B2B, chạy hết công suất thiết bị để có dòng tiền trả nợ đúng hạn.";
            }
        }
    }
}
