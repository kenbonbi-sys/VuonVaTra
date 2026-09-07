using UnityEngine;

namespace VuonNho.Views
{
    /// <summary>
    /// Sac thai cua mot thong tin. Khong phai mot mau — mot **y nghia**, va mau la thu suy ra.
    ///
    /// Nho vay cho goi noi "cai nay dang sai" chu khong noi "cai nay mau cam", va doi bang mau
    /// mot lan la doi khap noi.
    /// </summary>
    public enum UiTone
    {
        /// <summary>Thong tin thuong, khong khen khong che.</summary>
        Neutral = 0,
        /// <summary>Dang dung, dang dat.</summary>
        Good = 1,
        /// <summary>Can de y, chua hong.</summary>
        Warn = 2,
        /// <summary>Dang hong, dang mat tien.</summary>
        Bad = 3,
        /// <summary>Cho nguoi choi nen nhin vao truoc.</summary>
        Accent = 4
    }

    /// <summary>
    /// Bang tham so nen cua giao dien: khoang cach, ban kinh, co chu, va mau theo sac thai.
    ///
    /// Truoc day moi bang tu go so cua rieng no — cho nay padding 12, cho kia 10, co chu 16 o day
    /// va 17 o kia. Khong ai co y lam vay, no chi la thu xay ra khi khong co mot bang chung de
    /// tra cuu. Ket qua la giao dien **doc nhu nhieu nguoi lam roi ghep lai**, va sua mot chi tiet
    /// cho dep hon o mot bang lam no lech khoi nhung bang con lai.
    ///
    /// Khoang cach di theo bac 4: 4, 8, 12, 16, 24, 32. Mat nguoi doc quan he giua cac khoang
    /// cach chu khong doc gia tri tuyet doi cua chung, nen mot thang bac deu quan trong hon viec
    /// tung con so co "dung" hay khong.
    ///
    /// <see cref="UiFactory"/> co san vai hang so tu truoc (`FontSizeBody`, `RadiusPanel`…). Lop
    /// nay **tro ve chinh chung** thay vi khai bao lai: hai nguon so cho cung mot thu la hai
    /// nguon se lech nhau, va cai lech do khong bao gio bao loi.
    /// </summary>
    public static class UiTokens
    {
        // --- khoang cach ------------------------------------------------------------------
        public const float Space1 = 4f;
        public const float Space2 = 8f;
        public const float Space3 = 12f;
        public const float Space4 = 16f;
        public const float Space6 = 24f;
        public const float Space8 = 32f;

        /// <summary>Le trong cua mot the noi dung.</summary>
        public static RectOffset CardPadding
        {
            get { return new RectOffset((int)Space3, (int)Space3, (int)Space3, (int)Space3); }
        }

        public static RectOffset NoPadding
        {
            get { return new RectOffset(0, 0, 0, 0); }
        }

        // --- co chu -----------------------------------------------------------------------
        public const int TextMeta = UiFactory.FontSizeMeta;      // 16 — chu phu, ghi chu
        public const int TextBody = UiFactory.FontSizeBody;      // 18 — chu doc
        public const int TextRowTitle = UiFactory.FontSizeRowTitle;  // 20 — tieu de mot dong
        public const int TextTitle = UiFactory.FontSizeTitle;    // 24 — tieu de muc

        /// <summary>Chu cua the nho: nhan trang thai, don vi.</summary>
        public const int TextBadge = 14;

        /// <summary>Con so duoc doc nhu mot con so, khong nhu mot chu.</summary>
        public const int TextNumber = 22;

        // --- ban kinh ---------------------------------------------------------------------
        public const int RadiusCard = UiFactory.RadiusControl;   // 10
        public const int RadiusBadge = 9;
        public const int RadiusTrack = UiFactory.RadiusTrack;    // 5

        // --- chieu cao --------------------------------------------------------------------
        /// <summary>Mot hang so lieu. Du cho mot dong chu 18 px va le tren duoi.</summary>
        public const float RowHeight = 30f;
        public const float BadgeHeight = 24f;
        /// <summary>Day hon mot chut so voi thanh tien do cu: khoang chuan ve tren no phai doc duoc.</summary>
        public const float TrackHeight = 12f;

        /// <summary>Vach mau ben trai mot khoi ghi chu.</summary>
        public const float AccentBarWidth = 3f;

        // --- mau theo sac thai ------------------------------------------------------------

        /// <summary>Mau chu cua mot sac thai.</summary>
        public static Color TextOf(UiTone tone)
        {
            switch (tone)
            {
                case UiTone.Good: return GardenPalette.StateOk;
                case UiTone.Warn: return GardenPalette.StateWarn;
                case UiTone.Bad: return GardenPalette.ButtonDanger;
                case UiTone.Accent: return GardenPalette.TextPrimary;
                default: return GardenPalette.TextPrimary;
            }
        }

        /// <summary>
        /// Mau nen cua mot sac thai, da lam nhat.
        ///
        /// Nen phai nhat hon chu cua chinh no rat nhieu: mot the mau dam voi chu mau dam la mot
        /// vet mau khong doc duoc, va nam cai the nhu vay canh nhau bien mot bang so lieu thanh
        /// mot bang mau.
        /// </summary>
        public static Color FillOf(UiTone tone)
        {
            switch (tone)
            {
                case UiTone.Good: return Tint(GardenPalette.StateOk);
                case UiTone.Warn: return Tint(GardenPalette.StateWarn);
                case UiTone.Bad: return Tint(GardenPalette.ButtonDanger);
                case UiTone.Accent: return GardenPalette.ButtonActive;
                default: return GardenPalette.PanelSoftMuted;
            }
        }

        /// <summary>Keo mot mau ve phia nen kem, giu nguyen sac.</summary>
        static Color Tint(Color source)
        {
            return Color.Lerp(source, GardenPalette.PanelBackground, 0.78f);
        }
    }
}
