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
        public static readonly Color PanelBackground = new Color(0.125f, 0.108f, 0.090f, 1f);
        /// <summary>The mot dong nam tren panel — sang hon nen mot bac.</summary>
        public static readonly Color PanelSoft = new Color(0.196f, 0.169f, 0.137f, 1f);
        /// <summary>The cua nang cap da mua — lui ve sau nhung van doc duoc.</summary>
        public static readonly Color PanelSoftMuted = new Color(0.157f, 0.137f, 0.114f, 1f);
        /// <summary>Lop toi phu ca man hinh sau popup bat buoc tra loi.</summary>
        public static readonly Color PanelScrim = new Color(0f, 0f, 0f, 0.58f);
        /// <summary>Duong ke 1 px duoi thanh tren va duoi tieu de panel.</summary>
        public static readonly Color PanelDivider = new Color(1f, 1f, 1f, 0.08f);
        /// <summary>Nen toast.</summary>
        public static readonly Color Toast = new Color(0.145f, 0.125f, 0.102f, 0.96f);
        /// <summary>Ranh tien do luc may dang pha.</summary>
        public static readonly Color TrackEmpty = new Color(0.055f, 0.047f, 0.039f, 0.92f);

        // --- chu
        public static readonly Color TextPrimary = new Color(0.96f, 0.95f, 0.90f);
        public static readonly Color TextMuted = new Color(0.686f, 0.647f, 0.580f);
        /// <summary>Chu tren nut da tat. Phai khac han chu nut con bam duoc.</summary>
        public static readonly Color TextDisabled = new Color(0.510f, 0.486f, 0.443f);

        // --- nut
        /// <summary>Nut hanh dong chinh.</summary>
        public static readonly Color ButtonNormal = new Color(0.325f, 0.428f, 0.278f);
        /// <summary>Nut phu: dieu huong, dong, huy. Khong duoc to bang nut hanh dong.</summary>
        public static readonly Color ButtonQuiet = new Color(0.243f, 0.212f, 0.173f);
        /// <summary>Nut dang o trang thai duoc chon (panel dang mo, cong thuc dang dung).</summary>
        public static readonly Color ButtonActive = new Color(0.463f, 0.580f, 0.376f);
        public static readonly Color ButtonDisabled = new Color(0.216f, 0.192f, 0.165f);
        /// <summary>Nut xoa tien do — mau canh bao, chi dung mot cho.</summary>
        public static readonly Color ButtonDanger = new Color(0.450f, 0.240f, 0.220f);

        // --- trang thai
        public static readonly Color StateOk = new Color(0.573f, 0.812f, 0.427f);
        public static readonly Color StateWarn = new Color(0.925f, 0.639f, 0.310f);

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
