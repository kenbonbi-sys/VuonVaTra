using System.Collections.Generic;

namespace VuonNho.Core
{
    /// <summary>
    /// Bon phan hang tieu chuan thu hai bup che, theo bang "Phan Hang Tieu Chuan Thu Hai" cua
    /// ban mo phong. Thu tu tu bup non nhat den bup xo.
    /// </summary>
    public enum PluckGrade
    {
        Dinh = 0,
        NonTom = 1,
        MocCau = 2,
        BupXo = 3
    }

    /// <summary>
    /// Mot phan hang thu hai. Day la **ban ta** cua nhung con so da nam trong catalog: moi phan
    /// hang la mot <see cref="CropDefinition"/> rieng, va lop nay giai thich vi sao con so cua no
    /// nhu vay, de nguoi doc khong phai suy nguoc tu bang gia.
    ///
    /// Ba con so quyet dinh nam o day: hai duoc bao nhieu bup mot ngay, may kilogram tuoi cho ra
    /// mot kilogram kho, va mot kilogram kho ban duoc bao nhieu. Nhan lai voi nhau thi **doanh thu
    /// mot ngay cua bon phan hang gan bang nhau** — do la ket qua that cua bang trong ban mo
    /// phong, khong phai mot su co y lam cho de. Cai khac nhau la **so luong**: bup xo cho gap
    /// muoi lam lan so kilogram phai chay qua day chuyen de kiem cung mot so tien. Nen khi day
    /// chuyen la gioi han thi hai non hon la thang, con khi day chuyen con rong thi hai xo lai
    /// nhanh hon.
    /// </summary>
    public sealed class PluckGradeDefinition
    {
        public PluckGrade Grade;

        /// <summary>Cach nguoi lam che noi ve no: "1 tôm 2 lá".</summary>
        public string PluckName;

        /// <summary>Ten thuong pham: "Trà Móc Câu thượng hạng".</summary>
        public string ProductName;

        /// <summary>Phan khuc ban: "Bán lẻ phổ thông".</summary>
        public string Segment;

        /// <summary>Nang suat hai, gram bup tuoi moi ngay — dau tren cua khoang trong bang.</summary>
        public int FreshGramsPerDay;

        /// <summary>Bao nhieu gram tuoi cho ra 1000 gram kho. 5000 = 5,0 kg tuoi cho 1 kg kho.</summary>
        public int FreshPerDryPermille;

        /// <summary>Gia mot kilogram kho, nghin dong — diem giua cua khoang trong bang.</summary>
        public int PricePerDryKiloThousandVnd;

        /// <summary>Id cay tuong ung trong catalog.</summary>
        public string CropId;
    }

    /// <summary>
    /// Mot mua trong nam: nhiet do, sinh hoa va anh huong len nang suat va gia.
    ///
    /// Theanine la vi ngot em, polyphenol la vi chat duom. Hai chi so nay bien thien nguoc nhau
    /// theo nhiet do — nong thi cay tich polyphenol, mat thi tich theanine — nen mua khong chi la
    /// mot cai cong dong mo cho vai loai cay, no la mot cai nut xoay giua **nhieu va re** voi
    /// **it va dat**.
    /// </summary>
    public sealed class SeasonProfile
    {
        public Season Season;
        public string DisplayName;
        public int MinTempC;
        public int MaxTempC;

        /// <summary>Huong vi dac trung, cau chu hien trong so tay.</summary>
        public string Flavour;

        /// <summary>Ham luong theanine va polyphenol, phan nghin khoi luong kho.</summary>
        public int TheaninePermille;
        public int PolyphenolPermille;

        /// <summary>Nang suat va gia ban thuong pham cua mua nay, phan tram so voi muc chuan.</summary>
        public int YieldPercent;
        public int PricePercent;

        /// <summary>Che ngu dong: chi don tia va bon phan, khong co san luong dang ke.</summary>
        public bool Dormant;

        /// <summary>Mua ray xanh chich hut — cuoi xuan dau he.</summary>
        public bool Leafhopper;

        /// <summary>Viec cua nguoi lam vuon trong mua nay, mot cau.</summary>
        public string Work;
    }

    /// <summary>
    /// Dong hoc nong hoc: thoi vu, sinh hoa va co che ray xanh cua muc 2 trong ban mo phong.
    ///
    /// Cung ky luat voi <see cref="Cultivation"/>: ham thuan tuy tren so nguyen, khong Random,
    /// nen chay bu offline va chay online ra dung cung mot ket qua.
    /// </summary>
    public static class Agronomy
    {
        /// <summary>Chi so sinh hoa va anh huong cua tung mua. Thu tu theo enum <see cref="Season"/>.</summary>
        public static SeasonProfile Profile(Season season)
        {
            switch (season)
            {
                case Season.Xuan:
                    return new SeasonProfile
                    {
                        Season = Season.Xuan, DisplayName = "Vụ xuân (tiền minh)",
                        MinTempC = 18, MaxTempC = 24,
                        Flavour = "Hương cốm, ngọt hậu",
                        TheaninePermille = 24, PolyphenolPermille = 220,
                        YieldPercent = 90, PricePercent = 130,
                        Leafhopper = true,
                        Work = "Hái búp non trước tiết Thanh minh — vụ trà đắt nhất năm."
                    };
                case Season.Ha:
                    return new SeasonProfile
                    {
                        Season = Season.Ha, DisplayName = "Vụ hè (chính vụ)",
                        MinTempC = 32, MaxTempC = 38,
                        Flavour = "Đắng gắt, năng suất cao",
                        TheaninePermille = 10, PolyphenolPermille = 320,
                        YieldPercent = 130, PricePercent = 80,
                        Leafhopper = true,
                        Work = "Chạy hết công suất: nhiều búp, giá thấp, tiền về đều."
                    };
                case Season.Thu:
                    return new SeasonProfile
                    {
                        Season = Season.Thu, DisplayName = "Vụ thu (trà thu)",
                        MinTempC = 22, MaxTempC = 28,
                        Flavour = "Đượm vị, nước vàng óng",
                        TheaninePermille = 18, PolyphenolPermille = 260,
                        YieldPercent = 100, PricePercent = 110,
                        Work = "Vụ cân bằng nhất: đủ búp mà nước vẫn đượm."
                    };
                default:
                    return new SeasonProfile
                    {
                        Season = Season.Dong, DisplayName = "Vụ đông (ngủ đông)",
                        MinTempC = 8, MaxTempC = 15,
                        Flavour = "Cây nghỉ, dồn nhựa vào gốc",
                        TheaninePermille = 12, PolyphenolPermille = 180,
                        YieldPercent = 25, PricePercent = 100,
                        Dormant = true,
                        Work = "Đốn tỉa và bón phân. Doanh thu mùa này phải đến từ nơi khác."
                    };
            }
        }

        public static List<SeasonProfile> AllProfiles()
        {
            return new List<SeasonProfile>
            {
                Profile(Season.Xuan), Profile(Season.Ha), Profile(Season.Thu), Profile(Season.Dong)
            };
        }

        /// <summary>
        /// Bon phan hang thu hai, kem con so goc de doi chieu voi bang trong ban mo phong.
        /// </summary>
        public static List<PluckGradeDefinition> Grades()
        {
            return new List<PluckGradeDefinition>
            {
                new PluckGradeDefinition
                {
                    Grade = PluckGrade.Dinh, PluckName = "1 tôm (đinh trà)",
                    ProductName = "Trà Đinh Ngọc", Segment = "Quà tặng cao cấp",
                    FreshGramsPerDay = 1600, FreshPerDryPermille = 5000,
                    PricePerDryKiloThousandVnd = 2500, CropId = DefaultContent.CropTeaDinh
                },
                new PluckGradeDefinition
                {
                    Grade = PluckGrade.NonTom, PluckName = "1 tôm 1 lá",
                    ProductName = "Trà Nõn Tôm", Segment = "Giới thưởng trà",
                    FreshGramsPerDay = 4250, FreshPerDryPermille = 4500,
                    PricePerDryKiloThousandVnd = 900, CropId = DefaultContent.CropTeaNon
                },
                new PluckGradeDefinition
                {
                    Grade = PluckGrade.MocCau, PluckName = "1 tôm 2 lá",
                    ProductName = "Trà Móc Câu thượng hạng", Segment = "Bán lẻ phổ thông",
                    FreshGramsPerDay = 10000, FreshPerDryPermille = 4150,
                    PricePerDryKiloThousandVnd = 400, CropId = DefaultContent.CropTeaMocCau
                },
                new PluckGradeDefinition
                {
                    Grade = PluckGrade.BupXo, PluckName = "1 tôm 3 lá / búp xô",
                    ProductName = "Trà búp thông thường", Segment = "Bán sỉ bao lớn, F&B",
                    FreshGramsPerDay = 21500, FreshPerDryPermille = 3900,
                    PricePerDryKiloThousandVnd = 160, CropId = DefaultContent.CropTeaXo
                }
            };
        }

        public static PluckGradeDefinition GradeOf(string cropId)
        {
            var grades = Grades();
            for (int i = 0; i < grades.Count; i++)
                if (grades[i].CropId == cropId) return grades[i];
            return null;
        }

        /// <summary>Kilogram kho hai duoc mot ngay o phan hang nay. Chi de hien trong so tay.</summary>
        public static int DryGramsPerDay(PluckGradeDefinition grade)
        {
            if (grade == null || grade.FreshPerDryPermille <= 0) return 0;
            return grade.FreshGramsPerDay * 1000 / grade.FreshPerDryPermille;
        }

        /// <summary>
        /// Ray xanh (Jacobiasca formosana) co chich hut vu nay khong.
        ///
        /// Bam tu cung ba dau vao nhu <see cref="Cultivation.PestStrikes"/> nhung khac hat muoi,
        /// nen mot vu khong the vua bi sau benh vua duoc ray xanh do trung mot phep bam.
        ///
        /// Chi xay ra o mua co <see cref="SeasonProfile.Leafhopper"/> — cuoi xuan dau he — va chi
        /// tren cay che. Bi chich hut **nhe** la mot mon qua chu khong phai mot tai hoa: cay giai
        /// phong linalool va geraniol de goi thien dich, va chinh hai chat do lam nen Dong Phuong
        /// My Nhan.
        /// </summary>
        public static bool LeafhopperStrikes(long seed, int plotId, int cycleIndex, Season season,
                                             int chancePercent)
        {
            if (chancePercent <= 0) return false;
            if (!Profile(season).Leafhopper) return false;
            if (chancePercent >= 100) return true;
            return (int)(Cultivation.Hash(seed + 104729, plotId, cycleIndex) % 100) < chancePercent;
        }

        /// <summary>
        /// He so gia cua mua, phan tram. Ap len tien thu duoc cua mot me tra tai quay.
        ///
        /// Chi ap cho che: bac ha va cuc la tra thao moc, chung khong co bang sinh hoa nao trong
        /// ban mo phong, nen dem mot he so mua ap cho chung se la mot con so tu bay ra.
        /// </summary>
        public static int PricePercentFor(ContentCatalog catalog, string cropId, Season season)
        {
            if (!IsTea(catalog, cropId)) return 100;
            return Profile(season).PricePercent;
        }

        /// <summary>Cay nay co phai che khong — tuc co phan hang thu hai va bang sinh hoa.</summary>
        public static bool IsTea(ContentCatalog catalog, string cropId)
        {
            CropDefinition crop;
            if (catalog == null || !catalog.TryGetCrop(cropId, out crop)) return false;
            return crop.PluckGraded || crop.Id == DefaultContent.CropOrientalBeauty;
        }
    }
}
