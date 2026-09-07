using System.Collections.Generic;

namespace VuonNho.Core
{
    /// <summary>Hinh thuc phap ly cua co so. Muc 4 cua ban mo phong.</summary>
    public enum BusinessEntity
    {
        None = 0,
        /// <summary>Ho kinh doanh ca the.</summary>
        Hkd = 1,
        /// <summary>Cong ty TNHH mot thanh vien.</summary>
        Tnhh = 2
    }

    /// <summary>
    /// Mot lua chon dang ky. Hai cot cua bang so sanh trong ban mo phong, dua thanh du lieu.
    ///
    /// Danh doi that nam o cho: HKD **re va nhanh** nhung khong xuat duoc hoa don GTGT, nen kenh
    /// B2B va sieu thi dong lai; TNHH mo duoc nhung kenh do va chiu thue tren loi nhuan chu khong
    /// tren doanh thu — co loi khi bien loi nhuan thap, bat loi khi bien cao.
    /// </summary>
    public sealed class EntityOption
    {
        public BusinessEntity Entity;
        public string DisplayName;

        /// <summary>Giai doan phu hop: "Giai đoạn đầu", "Mở rộng quy mô".</summary>
        public string Stage;

        /// <summary>Noi dang ky: UBND cap huyen, So Ke hoach va Dau tu.</summary>
        public string Authority;

        /// <summary>Thoi gian tham dinh, ngay trong game.</summary>
        public int MinDays;
        public int MaxDays;

        public long FeeCoins;

        /// <summary>Thue khoan tren doanh thu, diem co ban. 450 = 4,5%.</summary>
        public int RevenueTaxBps;

        /// <summary>Thue thu nhap doanh nghiep tren loi nhuan rong, diem co ban.</summary>
        public int ProfitTaxBps;

        /// <summary>Thue gia tri gia tang tren doanh thu, diem co ban. Chi de hien ra bang so sanh.</summary>
        public int VatBps;

        /// <summary>
        /// GTGT duoc thu cua nguoi mua va khau tru dau vao, nen no **khong phai chi phi cua co so**.
        ///
        /// Dem 8% GTGT cong thang vao thue phai dong la sai, va sai theo huong lam TNHH khong bao
        /// gio re hon HKD — 8% GTGT mot minh da gan gap doi ca 4,5% thue khoan. Ma dung cai kha
        /// nang xuat hoa don GTGT khau tru moi la loi the cua TNHH trong ban mo phong. Nen o day
        /// giu lai con so de hien ra bang so sanh, va khong tinh no vao tien phai dong.
        /// </summary>
        public bool VatPassedThrough;

        /// <summary>Chi phi ke toan moi ky.</summary>
        public long BookkeepingCoinsPerCycle;

        public bool CanIssueVatInvoice;
        public string Bookkeeping;
        public string Advantage;
        public string Drawback;
    }

    /// <summary>Mot loi bi xu phat theo Nghi dinh 115/2018/ND-CP.</summary>
    public sealed class ViolationDefinition
    {
        public string Id;
        public string DisplayName;

        /// <summary>Khung tien phat. Muc cu the cua mot lan kiem tra bam trong khoang nay.</summary>
        public long MinFineCoins;
        public long MaxFineCoins;

        /// <summary>Kem dinh chi hoat dong.</summary>
        public bool Suspends;

        /// <summary>Cach khac phuc, mot cau.</summary>
        public string Remedy;
    }

    /// <summary>Ket qua mot lan kiem tra: cac loi tim thay va tong tien phat.</summary>
    public sealed class InspectionResult
    {
        public int Index;
        public long AtMs;
        public readonly List<string> ViolationIds = new List<string>();
        public long FineCoins;
        public bool Suspended;

        /// <summary>Lan nay chi nhac ve chuyen chua co giay phep, chua phat.</summary>
        public bool LicenceWarningOnly;

        public bool Clean
        {
            get { return ViolationIds.Count == 0; }
        }
    }

    /// <summary>Mot khu trong xuong so che mot chieu.</summary>
    public sealed class FactoryZone
    {
        public int Order;
        public string DisplayName;
        public string Detail;

        /// <summary>Cong doan trong catalog thuoc khu nay. Rong nghia la khu khong co may.</summary>
        public string StageId;
    }

    /// <summary>
    /// Ho so phap ly cua co so. Nam trong save: giay phep la mot thu da xin duoc thi khong mat.
    /// </summary>
    public sealed class ComplianceState
    {
        public BusinessEntity Entity;

        /// <summary>Ho so dang ky dang cho tham dinh, va no xong luc nao.</summary>
        public BusinessEntity PendingEntity;
        public long EntityReadyAtMs;

        /// <summary>Da co giay chung nhan an toan thuc pham.</summary>
        public bool FoodSafetyCertified;
        public bool FoodSafetyPending;
        public long FoodSafetyReadyAtMs;

        /// <summary>Da mua do bao ho y te cho tho.</summary>
        public bool ProtectiveGear;

        /// <summary>
        /// Da bi nhac mieng ve chuyen chua co giay an toan thuc pham.
        ///
        /// Lan kiem tra dau tien chi nhac, khong phat. Mot nguoi vua mua cai may dau tien ma bi
        /// phat 200 xu kem dinh chi thi khong hoc duoc gi — ho chi thay xuong minh dong cua ma
        /// khong biet minh da lam gi. Nhac truoc roi phat sau van giu du rang cua cai luat, va
        /// khac han o cho nguoi choi co mot ky de di xin giay.
        /// </summary>
        public bool LicenceWarned;

        /// <summary>Moc kiem tra ke tiep va so lan da kiem tra.</summary>
        public long NextInspectionAtMs;
        public int InspectionCount;

        /// <summary>Dang bi dinh chi den luc nao. 0 = khong bi dinh chi.</summary>
        public long SuspendedUntilMs;

        public long TotalFinesCoins;
        public long TotalTaxCoins;

        /// <summary>Ket qua lan kiem tra gan nhat, de HUD noi lai duoc.</summary>
        public int LastInspectionIndex;
        public string LastViolationIds;
        public long LastFineCoins;

        public bool Suspended(long atMs)
        {
            return SuspendedUntilMs > atMs;
        }

        public ComplianceState Clone()
        {
            return (ComplianceState)MemberwiseClone();
        }
    }

    /// <summary>
    /// Khung phap ly, an toan thuc pham va nguyen tac dong chay mot chieu.
    ///
    /// Ba he thong khac nhau nhung deu do vao **mot lan kiem tra dinh ky**: den ky thi doan kiem
    /// tra ghe xuong, doc trang thai that cua co so va lap bien ban. Nho vay nguoi choi khong
    /// phai theo ba cai dong ho khac nhau, va moi thu ho phai lo deu hien ra trong cung mot cai
    /// bien ban ho doc duoc.
    /// </summary>
    public static class Compliance
    {
        public const string ViolationOneWay = "violation_one_way";
        public const string ViolationPests = "violation_pests";
        public const string ViolationGear = "violation_gear";
        public const string ViolationNoLicence = "violation_no_licence";

        /// <summary>Mot ngay trong game dai bao lau. Mot thang chia lam 30 ngay.</summary>
        public static long DayMs(BalanceConfig balance)
        {
            long ms = Finance.CycleMs(balance) / 30;
            return ms < 1 ? 1 : ms;
        }

        public static List<EntityOption> Entities()
        {
            return new List<EntityOption>
            {
                new EntityOption
                {
                    Entity = BusinessEntity.Hkd,
                    DisplayName = "Hộ kinh doanh cá thể",
                    Stage = "Giai đoạn đầu",
                    Authority = "UBND cấp huyện",
                    MinDays = 3, MaxDays = 5,
                    FeeCoins = 30,
                    RevenueTaxBps = 450,
                    ProfitTaxBps = 0,
                    VatBps = 0,
                    BookkeepingCoinsPerCycle = 0,
                    CanIssueVatInvoice = false,
                    Bookkeeping = "Sổ sách đơn giản theo Thông tư 88/2021, không cần kế toán trưởng.",
                    Advantage = "Rẻ, nhanh, thuế khoán 4,5% doanh thu là hết.",
                    Drawback = "Không xuất được hoá đơn VAT nên kênh B2B và siêu thị đóng lại."
                },
                new EntityOption
                {
                    Entity = BusinessEntity.Tnhh,
                    DisplayName = "Công ty TNHH một thành viên",
                    Stage = "Mở rộng quy mô",
                    Authority = "Sở Kế hoạch & Đầu tư",
                    MinDays = 5, MaxDays = 10,
                    FeeCoins = 120,
                    RevenueTaxBps = 0,
                    ProfitTaxBps = 2000,
                    VatBps = 800,
                    VatPassedThrough = true,
                    BookkeepingCoinsPerCycle = 20,
                    CanIssueVatInvoice = true,
                    Bookkeeping = "Bắt buộc báo cáo tài chính năm, chi phí kế toán mỗi tháng.",
                    Advantage = "Tư cách pháp nhân đầy đủ, hoá đơn GTGT điện tử, đủ chuẩn xuất khẩu.",
                    Drawback = "Thuế 20% lợi nhuận cộng GTGT, và một khoản kế toán cố định mỗi tháng."
                }
            };
        }

        public static EntityOption Entity(BusinessEntity entity)
        {
            var options = Entities();
            for (int i = 0; i < options.Count; i++)
                if (options[i].Entity == entity) return options[i];
            return null;
        }

        public static List<ViolationDefinition> Violations()
        {
            return new List<ViolationDefinition>
            {
                new ViolationDefinition
                {
                    Id = ViolationNoLicence,
                    DisplayName = "Hoạt động khi chưa có giấy an toàn thực phẩm",
                    MinFineCoins = 200, MaxFineCoins = 300, Suspends = true,
                    Remedy = "Xin giấy an toàn thực phẩm ở bảng Pháp lý."
                },
                new ViolationDefinition
                {
                    Id = ViolationOneWay,
                    DisplayName = "Sản xuất không theo nguyên tắc một chiều",
                    MinFineCoins = 50, MaxFineCoins = 70,
                    Remedy = "Mua đủ máy của các chặng trước, không để hở giữa dây chuyền."
                },
                new ViolationDefinition
                {
                    Id = ViolationPests,
                    DisplayName = "Côn trùng, động vật xâm nhập xưởng",
                    MinFineCoins = 10, MaxFineCoins = 30,
                    Remedy = "Trị hết sâu bệnh trên các ô đất trước kỳ kiểm tra."
                },
                new ViolationDefinition
                {
                    Id = ViolationGear,
                    DisplayName = "Không mang đầy đủ đồ bảo hộ y tế",
                    MinFineCoins = 5, MaxFineCoins = 10,
                    Remedy = "Mua đồ bảo hộ cho thợ ở bảng Nâng cấp."
                }
            };
        }

        public static ViolationDefinition Violation(string id)
        {
            var all = Violations();
            for (int i = 0; i < all.Count; i++)
                if (all[i].Id == id) return all[i];
            return null;
        }

        /// <summary>
        /// Nam khu cua xuong so che mot chieu, theo dung so do trong ban mo phong. Khu nao co may
        /// thi tro toi cong doan tuong ung trong catalog.
        /// </summary>
        public static List<FactoryZone> Zones()
        {
            return new List<FactoryZone>
            {
                new FactoryZone
                {
                    Order = 1, DisplayName = "Tiếp nhận lá tươi",
                    Detail = "Khu phân loại thô, cân và rải mỏng.",
                    StageId = DefaultStages.Wither
                },
                new FactoryZone
                {
                    Order = 2, DisplayName = "Diệt men và vò",
                    Detail = "Khu nhiệt độ cao, tách khỏi lối đi chung.",
                    StageId = DefaultStages.Fix
                },
                new FactoryZone
                {
                    Order = 3, DisplayName = "Sấy khô và sao hương",
                    Detail = "Kiểm soát độ ẩm xuống dưới 5%.",
                    StageId = DefaultStages.Dry
                },
                new FactoryZone
                {
                    Order = 4, DisplayName = "Đóng gói vô trùng",
                    Detail = "Hút chân không, màng nhôm, khoá oxy hoá.",
                    StageId = DefaultStages.Pack
                },
                new FactoryZone
                {
                    Order = 5, DisplayName = "Kho thành phẩm",
                    Detail = "Xuất hàng B2B hoặc bán lẻ trực tiếp.",
                    StageId = null
                }
            };
        }

        /// <summary>
        /// Day chuyen co chay mot chieu khong.
        ///
        /// Vi pham khong phai la "mua thieu may" — thieu thi chi la chua xay xong. Vi pham la
        /// **co lo hong o giua**: mot cai may dung sau mot chang chua co may thi nguyen lieu phai
        /// vong nguoc lai qua khu da xu ly, va do dung la cai nguyen tac mot chieu cam.
        /// </summary>
        public static bool OneWayRespected(ContentCatalog catalog, GameState state)
        {
            bool sawGap = false;
            for (int i = 0; i < catalog.Stages.Count; i++)
            {
                var station = state.Station(catalog.Stages[i].Id);
                bool owned = station != null && station.Owned;
                if (!owned) sawGap = true;
                else if (sawGap) return false;
            }
            return true;
        }

        /// <summary>So may dang co. Duoi mot cai may thi khong co xuong nao de kiem tra.</summary>
        public static bool HasFactory(GameState state)
        {
            return state.OwnedStationCount() > 0;
        }

        /// <summary>
        /// Chay mot lan kiem tra. Ham thuan tuy: khong doi <paramref name="state"/>, chi doc.
        /// Muc phat cu the bam tu so lan kiem tra nen chay lai cung mot van cho cung mot bien ban.
        /// </summary>
        public static InspectionResult Inspect(ContentCatalog catalog, GameState state, long atMs)
        {
            var result = new InspectionResult { Index = state.Compliance.InspectionCount + 1, AtMs = atMs };
            if (!HasFactory(state)) return result;

            if (!state.Compliance.FoodSafetyCertified)
            {
                result.ViolationIds.Add(ViolationNoLicence);
                // Lan dau chi nhac. Xem ComplianceState.LicenceWarned.
                result.LicenceWarningOnly = !state.Compliance.LicenceWarned;
            }
            if (!OneWayRespected(catalog, state)) result.ViolationIds.Add(ViolationOneWay);
            if (AnyActivePest(state)) result.ViolationIds.Add(ViolationPests);
            if (state.HiredWorkers > 0 && !state.Compliance.ProtectiveGear)
                result.ViolationIds.Add(ViolationGear);

            if (result.LicenceWarningOnly) return result;

            for (int i = 0; i < result.ViolationIds.Count; i++)
            {
                var violation = Violation(result.ViolationIds[i]);
                if (violation == null) continue;
                result.FineCoins += FineFor(violation, state.PestSeed, result.Index, i);
                if (violation.Suspends) result.Suspended = true;
            }
            return result;
        }

        static bool AnyActivePest(GameState state)
        {
            for (int i = 0; i < state.Plots.Count; i++)
                if (state.Plots[i].PestActive) return true;
            return false;
        }

        /// <summary>Muc phat trong khung, bam tu hat giong cua van va so lan kiem tra.</summary>
        public static long FineFor(ViolationDefinition violation, long seed, int inspectionIndex, int slot)
        {
            long span = violation.MaxFineCoins - violation.MinFineCoins;
            if (span <= 0) return violation.MinFineCoins;
            ulong hash = Cultivation.Hash(seed + 7919, inspectionIndex, slot);
            return violation.MinFineCoins + (long)(hash % (ulong)(span + 1));
        }

        /// <summary>So ngay tham dinh mot ho so, bam trong khung de van nao cung nhu van do.</summary>
        public static int ReviewDays(long seed, int minDays, int maxDays, int salt)
        {
            if (maxDays <= minDays) return minDays;
            ulong hash = Cultivation.Hash(seed + 65537, salt, maxDays);
            return minDays + (int)(hash % (ulong)(maxDays - minDays + 1));
        }

        /// <summary>
        /// Thue phai dong cho mot ky, tinh tu doanh thu va chi phi cua ky do.
        ///
        /// HKD dong thue khoan tren **doanh thu** — lo van phai dong. TNHH dong tren **loi nhuan**,
        /// cong mot khoan ke toan co dinh moi ky (khong tinh o day, xem
        /// <see cref="EntityOption.BookkeepingCoinsPerCycle"/>).
        ///
        /// Do la ly do doi hinh thuc phap ly khong phai mot buoc nang cap mot chieu: khi bien loi
        /// nhuan mong, thue khoan tren doanh thu an nang hon; khi bien day, 20% loi nhuan lai dat
        /// hon. GTGT khong tinh vao day — xem <see cref="EntityOption.VatPassedThrough"/>.
        /// </summary>
        public static long TaxFor(BusinessEntity entity, long revenueCoins, long expenseCoins)
        {
            var option = Entity(entity);
            if (option == null || revenueCoins <= 0) return 0;
            long tax = revenueCoins * option.RevenueTaxBps / 10000;
            if (!option.VatPassedThrough) tax += revenueCoins * option.VatBps / 10000;
            long profit = revenueCoins - expenseCoins;
            if (profit > 0) tax += profit * option.ProfitTaxBps / 10000;
            return tax;
        }
    }
}
