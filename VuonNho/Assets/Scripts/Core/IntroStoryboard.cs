using System.Collections.Generic;

namespace VuonNho.Core
{
    /// <summary>Mot phan canh cua doan mo man.</summary>
    public sealed class IntroScene
    {
        public int Index;

        /// <summary>Moc thoi gian trong doan phim, "0:00 – 0:15".</summary>
        public string TimeCode;

        /// <summary>Goc quay, de nguoi lam art doc.</summary>
        public string Camera;

        public string Title;
        public string Visual;
        public string Audio;

        /// <summary>Cau chu hien duoi man hinh.</summary>
        public string Subtitle;

        /// <summary>Phan canh nay chiem bao lau, millisecond.</summary>
        public long DurationMs;
    }

    /// <summary>
    /// Kich ban doan mo man, muc 6 cua ban mo phong.
    ///
    /// Nam trong Core chu khong trong Views vi day la **noi dung**, khong phai cach ve: bo dem
    /// phan canh, cau phu de va do dai deu can test duoc ma khong phai bat Unity len. Views chi
    /// doc danh sach nay roi ve chu va lam mo dan.
    ///
    /// Hinh ve va am thanh cua tung phan canh chua co, nen ban chay duoc hien tai la mot ban
    /// **doc kich ban**: chu trang tren nen toi, dung nhip 15–20 giay mot canh nhu ban dung phim.
    /// Do dai o day chinh la do dai trong kich ban, nen khi co art thay vao thi nhip khong doi.
    /// </summary>
    public static class IntroStoryboard
    {
        public const string GameTitle = "VƯỜN VÀ TRÀ";
        public const string GameSubtitle = "The Tea Artisan";

        public static List<IntroScene> Scenes()
        {
            return new List<IntroScene>
            {
                new IntroScene
                {
                    Index = 1,
                    TimeCode = "0:00 – 0:15",
                    Camera = "Close-up tĩnh",
                    Title = "Gánh nặng tài chính và di sản dang dở",
                    Visual = "Cận cảnh bàn gỗ cũ kỹ. Khung ảnh gia đình đeo băng tang đen cạnh cuốn " +
                             "sổ tay sờn rách. Điện thoại nứt màn hình phát sáng: \"Số dư khả dụng: " +
                             "200.000.000 VNĐ\". Bàn tay chai sần lướt qua, dừng lại phân vân. Camera " +
                             "lia ra cửa sổ: đồi chè già cỗi xơ xác chìm trong sương mờ sau cơn mưa.",
                    Audio = "Mưa nhỏ giọt trên mái tôn rỉ sét, tiếng lật trang sổ nợ khô khốc, " +
                            "guitar mộc hoà lofi piano chậm rãi.",
                    Subtitle = "(Tiếng thở dài trầm ngâm… tiếng kim đồng hồ gõ nhịp tích tắc)",
                    DurationMs = 15000
                },
                new IntroScene
                {
                    Index = 2,
                    TimeCode = "0:15 – 0:35",
                    Camera = "Medium shot, ánh sáng tương phản",
                    Title = "Sự ràng buộc của đòn bẩy nợ ngân hàng",
                    Visual = "Bộ hồ sơ thế chấp sổ đỏ mộc đỏ. Bút máy run rẩy ký tên. Các thông số " +
                             "phát sáng nổi lên: thuê đồi chè −40tr, bộ bốn máy mini −95tr, lãi suất " +
                             "8,5%/năm. Nhịp kim đồng hồ dồn dập.",
                    Audio = "Ngòi bút sột soạt dứt khoát trên giấy, máy đếm tiền xoạch xoạch, " +
                            "đồng hồ tích tắc nhanh dần.",
                    Subtitle = "Ngày đến hạn thanh toán đầu tiên: 30 ngày…",
                    DurationMs = 20000
                },
                new IntroScene
                {
                    Index = 3,
                    TimeCode = "0:35 – 0:50",
                    Camera = "Low-angle wide chuyển macro",
                    Title = "Thức tỉnh và kết nối thổ nhưỡng",
                    Visual = "Bước chân mang ủng lún trên đất đỏ ẩm. Nhân vật quỳ xuống chạm tay vào " +
                             "búp chè non vừa nhú đọng giọt sương mai. Cận cảnh nâng niu búp chè một " +
                             "tôm đưa lên ngửi, ánh mắt kiên định trở lại.",
                    Audio = "Bước chân trên đất mềm, tiếng ngắt búp chè giòn tan \"tách\", " +
                            "lofi guitar chuyển tone ấm.",
                    Subtitle = "Đất không phụ người, nếu người đem lòng trân quý đất…",
                    DurationMs = 15000
                },
                new IntroScene
                {
                    Index = 4,
                    TimeCode = "0:50 – 1:05",
                    Camera = "Dynamic action close-up",
                    Title = "Thử lửa trong xưởng",
                    Visual = "Cửa buồng đốt bom sao bật mở, ngọn lửa củi đỏ rực. Bàn tay thoăn thoắt " +
                             "đảo chè trong chảo gang bốc khói. Làn khói hương cốm cuộn tròn thành " +
                             "tên game.",
                    Audio = "Củi cháy nổ lách tách, lá chè đảo loạt soạt trên vách tôn quay, " +
                            "một giọng đọc trầm ấm vang lên.",
                    Subtitle = "Đời người cũng như một mẻ trà. Muốn thơm, ắt phải qua lửa đỏ.",
                    DurationMs = 15000
                }
            };
        }

        /// <summary>Tong do dai doan mo man.</summary>
        public static long TotalDurationMs()
        {
            var scenes = Scenes();
            long total = 0;
            for (int i = 0; i < scenes.Count; i++) total += scenes[i].DurationMs;
            return total;
        }
    }
}
