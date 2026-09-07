using System.Collections.Generic;

namespace VuonNho.Core
{
    /// <summary>Hai duong che bien co ban cua muc 3: triet tieu men, hay khai thac men.</summary>
    public enum TeaRoute
    {
        /// <summary>Tra xanh: sao nong de dung han enzyme PPO, giu diep luc.</summary>
        Green = 0,
        /// <summary>Hong tra: khong diet men, de PPO oxy hoa het.</summary>
        Black = 1,
        /// <summary>Dong Phuong My Nhan: oxy hoa mot phan tren la da bi ray xanh chich hut.</summary>
        OrientalBeauty = 2
    }

    public enum BatchQuality
    {
        Flawed = 0,
        Standard = 1,
        Premium = 2
    }

    /// <summary>
    /// Bon nut can lua ma nguoi choi xoay. Nam trong save vi day la mot **cai chot cua xuong**,
    /// khong phai mot o nhap lieu tam: dat lai roi tat game thi mo lai phai con nguyen.
    /// </summary>
    public sealed class CraftSettings
    {
        public TeaRoute Route;

        /// <summary>Nhiet do sao thanh bom, do C. Chinh la $T_1$ trong ban mo phong.</summary>
        public int FixTempC = 255;

        /// <summary>Thoi gian vo dinh hinh, phut.</summary>
        public int RollMinutes = 18;

        /// <summary>Do am thanh pham, phan nghin. 45 = 4,5%.</summary>
        public int MoisturePermille = 45;

        /// <summary>Muc oxy hoa, phan tram.</summary>
        public int OxidationPercent = 3;

        public CraftSettings Clone()
        {
            return (CraftSettings)MemberwiseClone();
        }
    }

    /// <summary>Khoang dung cua mot nut, theo tung duong che bien.</summary>
    public sealed class CraftWindow
    {
        public string DisplayName;
        public string Unit;
        public int Minimum;
        public int Maximum;

        /// <summary>Khoang chuan — trong day thi khong loi gi.</summary>
        public int IdealLow;
        public int IdealHigh;

        /// <summary>Khoang con cham nhan duoc. Ra ngoai la me tra bi loi.</summary>
        public int AcceptableLow;
        public int AcceptableHigh;

        /// <summary>Cau bao loi khi thap qua, va khi cao qua.</summary>
        public string TooLow;
        public string TooHigh;
    }

    /// <summary>Ket qua danh gia mot me tra.</summary>
    public sealed class CraftVerdict
    {
        public BatchQuality Quality;

        /// <summary>Mot cau ket luan: "Đạt tiêu chuẩn thượng hạng".</summary>
        public string Title;

        /// <summary>Ten thuong pham cua me nay.</summary>
        public string ProductName;

        public string Liquor;
        public string Aroma;
        public string StorageRisk;

        /// <summary>Giai thich, hoac cau bao loi cua nut lech nhat.</summary>
        public string Note;

        /// <summary>He so gia, phan tram.</summary>
        public int PricePercent;

        /// <summary>Dat chuan Dong Phuong My Nhan — chi co khi la co ray xanh va oxy hoa dung khoang.</summary>
        public bool OrientalBeauty;
    }

    /// <summary>
    /// Dong hoc che bien thuc nghiem: bon thong so can lua quyet dinh me tra ra thanh pham
    /// thuong hang, dat, hay loi.
    ///
    /// Diem quan trong ve thiet ke: khoang chuan **hep** con khoang cham nhan duoc **rong**, nen
    /// mot nguoi choi khong doc so tay van lam ra tra ban duoc, chi la khong bao gio dat gia cao
    /// nhat. Bat buoc dung chuan moi ra hang thi bon cai nut nay thanh mot cau do phai tra loi
    /// dung mot lan roi khong bao gio xoay lai nua.
    /// </summary>
    public static class Crafting
    {
        public static string RouteName(TeaRoute route)
        {
            switch (route)
            {
                case TeaRoute.Green: return "Trà xanh";
                case TeaRoute.Black: return "Hồng trà";
                default: return "Đông Phương Mỹ Nhân";
            }
        }

        public static string RouteSummary(TeaRoute route)
        {
            switch (route)
            {
                case TeaRoute.Green:
                    return "Sao nóng diệt men để dừng oxy hoá, giữ màu diệp lục và hương cốm.";
                case TeaRoute.Black:
                    return "Không diệt men. Vò kỹ rồi ủ cho PPO oxy hoá hết, ra nước đỏ và vị mật.";
                default:
                    return "Lá bị rầy xanh chích hút, oxy hoá dở dang 60–75% để giữ linalool và geraniol.";
            }
        }

        /// <summary>Bon khoang cua mot duong che bien, theo dung thu tu nguoi choi xoay.</summary>
        public static List<CraftWindow> Windows(TeaRoute route)
        {
            var temperature = new CraftWindow
            {
                DisplayName = "Nhiệt độ diệt men (T₁)", Unit = "°C",
                Minimum = 80, Maximum = 320,
                TooLow = "Nhiệt thấp, enzyme PPO chưa chết hẳn: nước đỏ bã, vị chát gắt.",
                TooHigh = "Quá lửa: lá cháy khét, mất hương cốm, nước có mùi khê."
            };
            var roll = new CraftWindow
            {
                DisplayName = "Thời gian vò định hình", Unit = " phút",
                Minimum = 4, Maximum = 60,
                TooLow = "Vò non, tế bào chưa dập đủ: không ra móc câu, nước nhạt.",
                TooHigh = "Vò quá tay: lá nát thành vụn, nước đục và đắng."
            };
            var moisture = new CraftWindow
            {
                DisplayName = "Độ ẩm thành phẩm (M)", Unit = "‰",
                Minimum = 10, Maximum = 120,
                IdealLow = 10, IdealHigh = 45,
                AcceptableLow = 10, AcceptableHigh = 50,
                TooLow = "Sấy quá khô, cánh trà giòn vụn khi đóng gói.",
                TooHigh = "Trên 5% độ ẩm: trà hút ẩm rồi mốc, không giữ được 12 tháng."
            };
            var oxidation = new CraftWindow
            {
                DisplayName = "Mức oxy hoá", Unit = "%",
                Minimum = 0, Maximum = 100
            };

            switch (route)
            {
                case TeaRoute.Green:
                    temperature.IdealLow = 250; temperature.IdealHigh = 260;
                    temperature.AcceptableLow = 235; temperature.AcceptableHigh = 275;
                    roll.IdealLow = 15; roll.IdealHigh = 20;
                    roll.AcceptableLow = 11; roll.AcceptableHigh = 26;
                    oxidation.IdealLow = 0; oxidation.IdealHigh = 5;
                    oxidation.AcceptableLow = 0; oxidation.AcceptableHigh = 15;
                    oxidation.TooHigh = "Trà xanh mà đã oxy hoá: nước ngả vàng đỏ, mất vị tươi.";
                    oxidation.TooLow = "";
                    break;
                case TeaRoute.Black:
                    temperature.IdealLow = 100; temperature.IdealHigh = 130;
                    temperature.AcceptableLow = 90; temperature.AcceptableHigh = 160;
                    temperature.TooHigh = "Sao quá nóng là diệt men: PPO chết thì không lên men được nữa.";
                    roll.IdealLow = 25; roll.IdealHigh = 40;
                    roll.AcceptableLow = 20; roll.AcceptableHigh = 48;
                    roll.TooLow = "Vò chưa đủ dập tế bào thì oxy hoá không đều, nước hồng nhạt.";
                    oxidation.IdealLow = 85; oxidation.IdealHigh = 95;
                    oxidation.AcceptableLow = 75; oxidation.AcceptableHigh = 100;
                    oxidation.TooLow = "Lên men chưa tới: nước vừa chát vừa nhạt, không ra vị mật.";
                    oxidation.TooHigh = "Ủ quá lâu, cánh trà chua và có mùi men.";
                    break;
                default:
                    temperature.IdealLow = 110; temperature.IdealHigh = 140;
                    temperature.AcceptableLow = 95; temperature.AcceptableHigh = 170;
                    temperature.TooHigh = "Nóng quá thì linalool và geraniol bay hết — mất luôn cái đáng giá nhất.";
                    roll.IdealLow = 18; roll.IdealHigh = 28;
                    roll.AcceptableLow = 14; roll.AcceptableHigh = 34;
                    oxidation.IdealLow = 60; oxidation.IdealHigh = 75;
                    oxidation.AcceptableLow = 55; oxidation.AcceptableHigh = 80;
                    oxidation.TooLow = "Dưới 60% thì hương mật ong chưa chuyển hoá, vẫn chỉ là trà xanh.";
                    oxidation.TooHigh = "Trên 75% thì thành hồng trà thường, mất phần giá gấp mấy lần.";
                    break;
            }

            return new List<CraftWindow> { temperature, roll, moisture, oxidation };
        }

        /// <summary>Gia tri nguoi choi dang dat cho tung khoang, cung thu tu voi <see cref="Windows"/>.</summary>
        public static List<int> Values(CraftSettings settings)
        {
            return new List<int>
            {
                settings.FixTempC, settings.RollMinutes, settings.MoisturePermille, settings.OxidationPercent
            };
        }

        /// <summary>-1 = thap qua, 0 = trong khoang, 1 = cao qua. So sanh voi khoang chuan.</summary>
        public static int CompareToIdeal(CraftWindow window, int value)
        {
            if (value < window.IdealLow) return -1;
            if (value > window.IdealHigh) return 1;
            return 0;
        }

        static int CompareToAcceptable(CraftWindow window, int value)
        {
            if (value < window.AcceptableLow) return -1;
            if (value > window.AcceptableHigh) return 1;
            return 0;
        }

        /// <summary>Cai dat mac dinh dung chuan cho mot duong che bien. Dung khi nguoi choi doi duong.</summary>
        public static CraftSettings DefaultsFor(TeaRoute route)
        {
            var windows = Windows(route);
            return new CraftSettings
            {
                Route = route,
                FixTempC = Middle(windows[0]),
                RollMinutes = Middle(windows[1]),
                MoisturePermille = Middle(windows[2]),
                OxidationPercent = Middle(windows[3])
            };
        }

        static int Middle(CraftWindow window)
        {
            return (window.IdealLow + window.IdealHigh) / 2;
        }

        /// <summary>
        /// Danh gia mot me tra tu bon thong so.
        ///
        /// <paramref name="leafhopperLeaves"/> la la cua o da bi ray xanh chich hut. Khong co la
        /// do thi duong Dong Phuong My Nhan chi ra hong tra thuong — cai lam nen no la con ray,
        /// khong phai cai nut oxy hoa.
        /// </summary>
        public static CraftVerdict Evaluate(CraftSettings settings, bool leafhopperLeaves,
                                            int premiumBonusPercent, int flawedPenaltyPercent)
        {
            var windows = Windows(settings.Route);
            var values = Values(settings);

            CraftWindow worst = null;
            int worstDirection = 0;
            bool anyOffIdeal = false;

            for (int i = 0; i < windows.Count; i++)
            {
                int outside = CompareToAcceptable(windows[i], values[i]);
                if (outside != 0 && worst == null)
                {
                    worst = windows[i];
                    worstDirection = outside;
                }
                if (CompareToIdeal(windows[i], values[i]) != 0) anyOffIdeal = true;
            }

            var verdict = new CraftVerdict();
            if (worst != null)
            {
                verdict.Quality = BatchQuality.Flawed;
                verdict.Title = "Mẻ trà bị lỗi";
                verdict.Note = worstDirection < 0 ? worst.TooLow : worst.TooHigh;
                if (string.IsNullOrEmpty(verdict.Note))
                    verdict.Note = worst.DisplayName + " nằm ngoài khoảng chấp nhận được.";
                verdict.PricePercent = flawedPenaltyPercent;
            }
            else if (anyOffIdeal)
            {
                verdict.Quality = BatchQuality.Standard;
                verdict.Title = "Đạt tiêu chuẩn thương phẩm";
                verdict.Note = "Bán được, nhưng chưa vào khoảng chuẩn nên không được giá cao nhất.";
                verdict.PricePercent = 100;
            }
            else
            {
                verdict.Quality = BatchQuality.Premium;
                verdict.Title = "Đạt tiêu chuẩn thượng hạng";
                verdict.PricePercent = premiumBonusPercent;
            }

            FillSensory(verdict, settings.Route, leafhopperLeaves);
            return verdict;
        }

        /// <summary>
        /// He so gia cuoi cung cua mot me, sau khi tinh ca truong hop la ray xanh bi lam sai duong.
        ///
        /// La ray xanh la thu dat nhat trong vuon, va no **de mat gia nhat**: sao no theo duong tra
        /// xanh, hay u qua 75%, thi linalool va geraniol khong chuyen hoa va me do chi con la hong
        /// tra thuong. Nguoi choi giu lai duoc mon qua chi khi ho nhan ra minh vua duoc no.
        /// </summary>
        public static int BatchPricePercent(CraftVerdict verdict, bool leafhopperLeaves, int fallbackPercent)
        {
            if (verdict == null) return 100;
            if (!leafhopperLeaves || verdict.OrientalBeauty) return verdict.PricePercent;
            return verdict.PricePercent * fallbackPercent / 100;
        }

        static void FillSensory(CraftVerdict verdict, TeaRoute route, bool leafhopperLeaves)
        {
            bool good = verdict.Quality != BatchQuality.Flawed;
            switch (route)
            {
                case TeaRoute.Green:
                    verdict.ProductName = good ? "Trà xanh móc câu hương cốm non" : "Trà xanh lỗi lửa";
                    verdict.Liquor = good ? "Xanh trong" : "Đỏ bã, đục";
                    verdict.Aroma = good ? "Hương cốm non" : "Chát gắt, khê nhẹ";
                    verdict.StorageRisk = good ? "An toàn, trên 12 tháng" : "Xuống hương sau 3 tháng";
                    if (good && string.IsNullOrEmpty(verdict.Note))
                        verdict.Note = "Nhiệt diệt men chính xác cố định màu diệp lục và ức chế hoàn toàn PPO.";
                    break;
                case TeaRoute.Black:
                    verdict.ProductName = good ? "Hồng trà cánh xoăn vị mật" : "Hồng trà ủ lỗi";
                    verdict.Liquor = good ? "Đỏ hổ phách" : "Nâu xỉn";
                    verdict.Aroma = good ? "Mật ong và quả chín" : "Mùi men, chua nhẹ";
                    verdict.StorageRisk = good ? "An toàn, trên 24 tháng" : "Dễ ẩm mốc";
                    if (good && string.IsNullOrEmpty(verdict.Note))
                        verdict.Note = "PPO được giữ sống suốt quá trình vò và ủ, oxy hoá đi hết chặng.";
                    break;
                default:
                    if (!leafhopperLeaves)
                    {
                        verdict.OrientalBeauty = false;
                        verdict.ProductName = "Hồng trà thường (thiếu lá rầy xanh)";
                        verdict.Liquor = "Đỏ nhạt";
                        verdict.Aroma = "Không có hương mật";
                        verdict.StorageRisk = "An toàn";
                        verdict.Note = "Không có ô nào bị rầy xanh chích hút, nên mẻ này chỉ là hồng trà.";
                        if (verdict.PricePercent > 100) verdict.PricePercent = 100;
                        break;
                    }
                    verdict.OrientalBeauty = good;
                    verdict.ProductName = good ? "Đông Phương Mỹ Nhân" : "Lá rầy xanh ủ lỗi";
                    verdict.Liquor = good ? "Hổ phách ánh cam" : "Nâu đục";
                    verdict.Aroma = good ? "Mật ong, hoa chín, hậu ngọt dài" : "Mất hương, còn vị chua";
                    verdict.StorageRisk = good ? "An toàn, càng để càng đượm" : "Không để được lâu";
                    if (good && string.IsNullOrEmpty(verdict.Note))
                        verdict.Note = "Linalool và geraniol do cây tiết ra để gọi thiên địch đã chuyển hoá trọn vẹn.";
                    break;
            }
        }
    }
}
