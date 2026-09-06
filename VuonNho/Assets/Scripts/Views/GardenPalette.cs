using UnityEngine;
using VuonNho.Core;

namespace VuonNho.Views
{
    /// <summary>
    /// Bang mau tam cho moc A: xanh co diu, kem dat, nau go, diem nhan vang cho xu.
    /// Khi co material that o moc B, chi thay VisualRoot chu khong sua logic.
    ///
    /// Mau HUD duoc khai bao het o day: khong noi nao trong GameHud tu che mau nua.
    /// Mat panel la mau go toi va **duc hoan toan** — alpha duoi 1 lam vuon 3D loi qua
    /// chu, moi cho mot mau khac nhau tuy canh phia sau.
    /// </summary>
    public static class GardenPalette
    {
        // --- mau cua canh 3D
        public static readonly Color Grass = new Color(0.42f, 0.62f, 0.35f);
        public static readonly Color Soil = new Color(0.45f, 0.34f, 0.25f);
        public static readonly Color SoilLocked = new Color(0.30f, 0.28f, 0.26f);
        public static readonly Color Cream = new Color(0.94f, 0.90f, 0.80f);
        public static readonly Color Wood = new Color(0.55f, 0.40f, 0.27f);
        public static readonly Color Coin = new Color(0.95f, 0.78f, 0.25f);
        public static readonly Color Robot = new Color(0.78f, 0.82f, 0.86f);

        // --- be mat HUD
        /// <summary>Nen panel, popup, thanh tren. Duc hoan toan.</summary>
        public static readonly Color PanelBackground = new Color(0.969f, 0.949f, 0.898f, 1f);
        /// <summary>The mot dong nam tren panel — sang hon nen mot bac.</summary>
        public static readonly Color PanelSoft = new Color(0.933f, 0.898f, 0.827f, 1f);
        /// <summary>The cua nang cap da mua — lui ve sau nhung van doc duoc.</summary>
        public static readonly Color PanelSoftMuted = new Color(0.929f, 0.929f, 0.863f, 1f);
        /// <summary>Lop toi phu ca man hinh sau popup bat buoc tra loi.</summary>
        public static readonly Color PanelScrim = new Color(0f, 0f, 0f, 0.58f);
        /// <summary>Duong ke 1 px duoi thanh tren va duoi tieu de panel.</summary>
        public static readonly Color PanelDivider = new Color(0.25f, 0.30f, 0.22f, 0.14f);
        /// <summary>Nen toast.</summary>
        public static readonly Color Toast = new Color(0.969f, 0.949f, 0.898f, 1f);
        /// <summary>Ranh tien do luc may dang pha.</summary>
        public static readonly Color TrackEmpty = new Color(0.82f, 0.84f, 0.73f, 1f);

        // --- chu
        public static readonly Color TextPrimary = new Color(0.188f, 0.235f, 0.196f);
        public static readonly Color TextMuted = new Color(0.39f, 0.42f, 0.35f);
        public static readonly Color TextOnPrimary = new Color(0.99f, 0.98f, 0.94f);
        public static readonly Color TextCoin = new Color(0.48f, 0.32f, 0.08f);
        /// <summary>Chu tren nut da tat. Phai khac han chu nut con bam duoc.</summary>
        public static readonly Color TextDisabled = new Color(0.45f, 0.46f, 0.40f);

        // --- icon
        /// <summary>Ve icon dung mau anh chup, khong nhuom lai.</summary>
        public static readonly Color IconTint = new Color(1f, 1f, 1f, 1f);
        /// <summary>Icon cua muc chua mo khoa hay nut da tat — mo di cung bac voi chu.</summary>
        public static readonly Color IconMuted = new Color(1f, 1f, 1f, 0.45f);

        // --- nut
        /// <summary>Nut hanh dong chinh.</summary>
        public static readonly Color ButtonNormal = new Color(0.29f, 0.40f, 0.28f);
        /// <summary>Nut phu: dieu huong, dong, huy. Khong duoc to bang nut hanh dong.</summary>
        public static readonly Color ButtonQuiet = new Color(0.89f, 0.865f, 0.79f);
        /// <summary>Nut dang o trang thai duoc chon (panel dang mo, cong thuc dang dung).</summary>
        public static readonly Color ButtonActive = new Color(0.78f, 0.85f, 0.65f);
        public static readonly Color ButtonDisabled = new Color(0.865f, 0.855f, 0.79f);
        /// <summary>Nut xoa tien do — mau canh bao, chi dung mot cho.</summary>
        public static readonly Color ButtonDanger = new Color(0.450f, 0.240f, 0.220f);

        // --- trang thai
        public static readonly Color StateOk = new Color(0.27f, 0.44f, 0.24f);
        public static readonly Color StateWarn = new Color(0.56f, 0.32f, 0.12f);

        public static Color CropBody(string cropId)
        {
            switch (cropId)
            {
                case DefaultContent.CropMint: return new Color(0.36f, 0.70f, 0.42f);
                case DefaultContent.CropChamomile: return new Color(0.86f, 0.84f, 0.45f);
                case DefaultContent.CropStrawberry: return new Color(0.80f, 0.30f, 0.35f);
                case DefaultContent.CropLemongrass: return new Color(0.62f, 0.74f, 0.38f);
                case DefaultContent.CropJasmine: return new Color(0.92f, 0.92f, 0.86f);
                default: return Grass;
            }
        }

        /// <summary>Cay chin phai co dau hieu rieng, khong chi doi mau.</summary>
        public static Color CropReadyAccent(string cropId)
        {
            switch (cropId)
            {
                case DefaultContent.CropMint: return new Color(0.72f, 0.95f, 0.62f);
                case DefaultContent.CropChamomile: return new Color(1.00f, 0.98f, 0.78f);
                case DefaultContent.CropStrawberry: return new Color(1.00f, 0.55f, 0.55f);
                case DefaultContent.CropLemongrass: return new Color(0.88f, 0.95f, 0.60f);
                case DefaultContent.CropJasmine: return new Color(1.00f, 1.00f, 0.95f);
                default: return Cream;
            }
        }
    }
}
