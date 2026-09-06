using System.Collections.Generic;

namespace VuonNho.Core
{
    /// <summary>
    /// Mot cong doan che bien tra. Moi cong doan an mot mat hang va nha ra mat hang cua cong
    /// doan sau, nen ca day chuyen la mot chuoi noi duoi nhau bang ten mat hang chu khong bang
    /// mot danh sach thu tu cung.
    ///
    /// Cong doan **thu hoach** khong nam o day: no da la o dat va robot. Cong doan **phan phoi**
    /// cung khong: do la quay tra, no doi tra thanh xu. Sau cong doan trong danh sach nay la sau
    /// cai may dung giua hai dau do.
    /// </summary>
    public sealed class ProcessStageDefinition
    {
        public string Id;

        /// <summary>Ten cong doan — "Lam heo". Dung o tieu de hang trong bang day chuyen.</summary>
        public string DisplayName;

        /// <summary>Ten cai may lam cong doan do — "Mang lam heo". Dung khi noi ve viec mua may.</summary>
        public string MachineName;

        public string Description;

        /// <summary>Duoi ten mat hang an vao. Chuoi rong nghia la la tuoi, tuc la chinh cropId.</summary>
        public string InputSuffix;

        /// <summary>Duoi ten mat hang nha ra.</summary>
        public string OutputSuffix;

        public int InputCount;
        public int OutputCount;
        public long BaseProcessMs;

        /// <summary>Gia mua may. Mua mot lan, khong co bac nang cap.</summary>
        public long Cost;

        public int SortOrder;
    }

    /// <summary>
    /// Ten mat hang trong kho. La tuoi giu nguyen cropId de save cu doc lai van dung; cac chang
    /// sau them duoi vao sau cropId.
    /// </summary>
    public static class ProcessChain
    {
        public const string SuffixFresh = "";
        public const string SuffixWithered = "heo";
        public const string SuffixFixed = "dietmen";
        public const string SuffixRolled = "vo";
        public const string SuffixOxidised = "lenmen";
        public const string SuffixDried = "say";
        public const string SuffixPacked = "goi";

        public static string ItemId(string cropId, string suffix)
        {
            return string.IsNullOrEmpty(suffix) ? cropId : cropId + "_" + suffix;
        }
    }

    /// <summary>Mot cai may trong vuon: da mua chua, dang chay me nao.</summary>
    public sealed class StationState
    {
        public string StageId;
        public bool Owned;
        public bool Running;

        /// <summary>Cay dang duoc che bien trong me nay. Null khi may dang ranh.</summary>
        public string BatchCropId;

        /// <summary>So luong ra, chot luc bat dau me — doi may giua chung khong doi me dang chay.</summary>
        public int BatchOutput;

        public long BatchStartAtMs;
        public long BatchFinishAtMs;

        public StationState Clone()
        {
            return (StationState)MemberwiseClone();
        }
    }

    /// <summary>
    /// Tho che bien. Chi co mot vai tro chu khong phai moi may mot nghe: cai lam nen do kho la
    /// **so nguoi**, khong phai bang phan vai, va them nghe chi lam bang hiring UI phinh ra chu
    /// khong lam quyet dinh nao kho hon.
    ///
    /// Mot tho chay duoc mot may tai mot thoi diem. It tho hon so may dang co thi may cuoi day
    /// chuyen nam khong — day la y nghia cua "thieu nguoi".
    /// </summary>
    public static class Workforce
    {
        /// <summary>So may mot tho chay duoc cung luc.</summary>
        public const int StationsPerWorker = 1;
    }

    public static class DefaultStages
    {
        public const string Wither = "stage_wither";
        public const string Fix = "stage_fix";
        public const string Roll = "stage_roll";
        public const string Oxidise = "stage_oxidise";
        public const string Dry = "stage_dry";
        public const string Pack = "stage_pack";

        /// <summary>
        /// Sau cong doan giua thu hoach va quay tra, dung thu tu nghe lam tra that.
        ///
        /// Thoi gian tang dan theo chang: chang cang ve sau cang lam mat hang dat hon, nen phai
        /// cham hon, neu khong ca day chuyen se don ve mot nut co chai duy nhat o dau vao.
        /// </summary>
        public static List<ProcessStageDefinition> Create()
        {
            return new List<ProcessStageDefinition>
            {
                new ProcessStageDefinition
                {
                    Id = Wither, DisplayName = "Làm héo", MachineName = "Máng làm héo",
                    Description = "Quạt gió rút bớt nước, lá mềm lại để vò không nát.",
                    InputSuffix = ProcessChain.SuffixFresh, OutputSuffix = ProcessChain.SuffixWithered,
                    InputCount = 2, OutputCount = 2, BaseProcessMs = 6000,
                    Cost = 200, SortOrder = 0
                },
                new ProcessStageDefinition
                {
                    Id = Fix, DisplayName = "Diệt men", MachineName = "Máy sao diệt men",
                    Description = "Sao nóng để dừng oxy hoá, giữ màu và hương.",
                    InputSuffix = ProcessChain.SuffixWithered, OutputSuffix = ProcessChain.SuffixFixed,
                    InputCount = 2, OutputCount = 2, BaseProcessMs = 8000,
                    Cost = 420, SortOrder = 1
                },
                new ProcessStageDefinition
                {
                    Id = Roll, DisplayName = "Vò và tạo hình", MachineName = "Máy vò trà",
                    Description = "Làm dập tế bào cho dịch trà rướm ra, xoăn mép lá.",
                    InputSuffix = ProcessChain.SuffixFixed, OutputSuffix = ProcessChain.SuffixRolled,
                    InputCount = 2, OutputCount = 2, BaseProcessMs = 10000,
                    Cost = 760, SortOrder = 2
                },
                new ProcessStageDefinition
                {
                    Id = Oxidise, DisplayName = "Lên men", MachineName = "Phòng lên men",
                    Description = "Ủ trong nhiệt ẩm kiểm soát để ra màu và vị đặc trưng.",
                    InputSuffix = ProcessChain.SuffixRolled, OutputSuffix = ProcessChain.SuffixOxidised,
                    InputCount = 2, OutputCount = 2, BaseProcessMs = 14000,
                    Cost = 1200, SortOrder = 3
                },
                new ProcessStageDefinition
                {
                    Id = Dry, DisplayName = "Sấy khô", MachineName = "Máy sấy băng tải",
                    Description = "Hạ độ ẩm xuống mức bảo quản được.",
                    InputSuffix = ProcessChain.SuffixOxidised, OutputSuffix = ProcessChain.SuffixDried,
                    InputCount = 2, OutputCount = 2, BaseProcessMs = 12000,
                    Cost = 1800, SortOrder = 4
                },
                new ProcessStageDefinition
                {
                    Id = Pack, DisplayName = "Phân loại và đóng gói", MachineName = "Máy sàng và đóng gói",
                    Description = "Sàng bỏ tạp chất, ướp hương rồi đóng kín khí.",
                    InputSuffix = ProcessChain.SuffixDried, OutputSuffix = ProcessChain.SuffixPacked,
                    InputCount = 2, OutputCount = 2, BaseProcessMs = 10000,
                    Cost = 2600, SortOrder = 5
                }
            };
        }
    }
}
