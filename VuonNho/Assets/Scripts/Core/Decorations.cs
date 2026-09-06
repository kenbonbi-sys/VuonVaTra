using System.Collections.Generic;

namespace VuonNho.Core
{
    /// <summary>
    /// Mot mon trang tri. Thuan tham my: khong xuat hien trong FarmSimulation, khong doi
    /// nang suat, khong doi thoi gian. Dat sai cho cung khong lam hong vong choi.
    /// </summary>
    public sealed class DecorationDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public long Cost;
        /// <summary>Ban kinh chiem cho, milimet. Dung de chan hai mon dat chong len nhau.</summary>
        public int FootprintMm;
        public int SortOrder;
    }

    /// <summary>
    /// Mot mon da dat trong vuon. Toa do luu bang milimet nguyen de save doc lai dung y nguyen,
    /// theo dung ky luat so nguyen cua muc 8 ke hoach.
    /// </summary>
    public sealed class PlacedDecoration
    {
        public string DefinitionId;
        public int XMm;
        public int ZMm;
        public int RotationDeg;

        public PlacedDecoration Clone()
        {
            return (PlacedDecoration)MemberwiseClone();
        }
    }

    /// <summary>
    /// Ranh gioi hai pha: pha 1 la dung xong farm co dan dat, pha 2 la mo rong va tuy bien.
    /// Dat o Core de ca view lan test deu hoi cung mot cho.
    /// </summary>
    public static class GardenPhase
    {
        /// <summary>
        /// Pha 1 xong khi nguoi choi da co robot va da mo het dat. Luc do cai farm da "dung xong"
        /// theo dung nghia cua luong dan dat, va phan trang tri moi mo ra.
        /// </summary>
        public static bool IsSetupComplete(GameState state, ContentCatalog catalog)
        {
            if (!state.RobotUnlocked) return false;
            return state.UnlockedPlotCount() >= catalog.Balance.MaximumPlots;
        }

        /// <summary>Con thieu gi de mo pha 2 — dung cho cau nhac trong HUD.</summary>
        public static string SetupRemainingHint(GameState state, ContentCatalog catalog)
        {
            if (!state.RobotUnlocked) return "robot";
            int missing = catalog.Balance.MaximumPlots - state.UnlockedPlotCount();
            return missing > 0 ? missing + " ô đất" : null;
        }
    }

    public static class DefaultDecorations
    {
        public const string Planter = "deco_planter";
        public const string Bench = "deco_bench";
        public const string Lantern = "deco_lantern";
        public const string Signboard = "deco_signboard";
        public const string StonePath = "deco_stone_path";

        public static List<DecorationDefinition> Create()
        {
            return new List<DecorationDefinition>
            {
                new DecorationDefinition
                {
                    Id = StonePath, DisplayName = "Phiến đá", Cost = 60, FootprintMm = 220,
                    Description = "Lát lối đi quanh vườn.", SortOrder = 0
                },
                new DecorationDefinition
                {
                    Id = Planter, DisplayName = "Chậu hoa", Cost = 120, FootprintMm = 260,
                    Description = "Chậu hoa nhỏ đặt ven luống.", SortOrder = 1
                },
                new DecorationDefinition
                {
                    Id = Lantern, DisplayName = "Đèn lồng", Cost = 260, FootprintMm = 220,
                    Description = "Đèn lồng treo trên cọc gỗ.", SortOrder = 2
                },
                new DecorationDefinition
                {
                    Id = Bench, DisplayName = "Ghế gỗ", Cost = 320, FootprintMm = 520,
                    Description = "Chỗ ngồi nhìn ra vườn.", SortOrder = 3
                },
                new DecorationDefinition
                {
                    Id = Signboard, DisplayName = "Bảng hiệu", Cost = 480, FootprintMm = 420,
                    Description = "Bảng gỗ đề tên quán.", SortOrder = 4
                }
            };
        }
    }
}
