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

        /// <summary>
        /// Nhan vat co phai di vong qua mon nay khong. Mac dinh la co: mot cai ghe ma di xuyen
        /// qua duoc thi nhin nhu anh nen chu khong nhu do vat.
        ///
        /// Loi di lat da la ngoai le duy nhat, va no khong phai chuyen ky thuat: mon do sinh ra
        /// de nguoi ta di len tren.
        /// </summary>
        public bool BlocksWalking = true;

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

    /// <summary>Loai vat can khien mot cho khong dat trang tri duoc.</summary>
    public enum GardenBlockKind
    {
        Plot,
        Station,
        Robot
    }

    /// <summary>
    /// Mot hinh chu nhat cam dat trang tri, milimet nguyen. Truc X sang phai, truc Z ra xa
    /// nguoi choi — cung he toa do voi scene nhung khong dinh gi toi Unity.
    /// </summary>
    public sealed class GardenRect
    {
        public GardenBlockKind Kind;
        public int MinXMm;
        public int MaxXMm;
        public int MinZMm;
        public int MaxZMm;
    }

    /// <summary>
    /// Bo cuc vuon tinh tu BalanceConfig. Ben Unity (SceneFactory + GardenSkin) dung dung nhung
    /// so nay de dat o dat, quay tra va robot; Core khong doc duoc chung nen giu ban sao o day,
    /// con SceneFactory doi chieu lai luc dung scene.
    /// </summary>
    public static class GardenLayout
    {
        /// <summary>Tam o theo cot. Nhan doi truoc khi chia de khong hut mat nua milimet.</summary>
        public static int PlotCenterXMm(BalanceConfig balance, int column)
        {
            return CenterMm(column, balance.GardenColumns, balance.PlotSpacingMm);
        }

        /// <summary>Tam o theo hang, cung cach can giua nhu cot.</summary>
        public static int PlotCenterZMm(BalanceConfig balance, int row)
        {
            return CenterMm(row, balance.GardenRows, balance.PlotSpacingMm);
        }

        /// <summary>Nua be ngang cua ca luoi, tinh ca canh o o hai dau.</summary>
        public static int GridHalfWidthMm(BalanceConfig balance)
        {
            return HalfSpanMm(balance.GardenColumns, balance.PlotSpacingMm, balance.PlotEdgeMm);
        }

        /// <summary>Nua be sau cua ca luoi, tinh ca canh o o hai dau.</summary>
        public static int GridHalfDepthMm(BalanceConfig balance)
        {
            return HalfSpanMm(balance.GardenRows, balance.PlotSpacingMm, balance.PlotEdgeMm);
        }

        /// <summary>
        /// Toan bo hinh chu nhat cam dat: tung o dat theo thu tu hang roi cot, den quay tra,
        /// roi robot. Thu tu co dinh nen ly do tu choi tra ve luon giong nhau.
        /// </summary>
        public static List<GardenRect> KeepOutRects(BalanceConfig balance)
        {
            var rects = new List<GardenRect>();
            int plotHalf = balance.PlotKeepOutEdgeMm / 2;

            for (int row = 0; row < balance.GardenRows; row++)
            {
                for (int column = 0; column < balance.GardenColumns; column++)
                {
                    rects.Add(Build(GardenBlockKind.Plot,
                                    PlotCenterXMm(balance, column),
                                    PlotCenterZMm(balance, row),
                                    plotHalf, plotHalf));
                }
            }

            rects.Add(Build(GardenBlockKind.Station,
                            balance.StationCenterXMm, balance.StationCenterZMm,
                            balance.StationKeepOutHalfWidthMm, balance.StationKeepOutHalfDepthMm));
            rects.Add(Build(GardenBlockKind.Robot,
                            balance.RobotCenterXMm, balance.RobotCenterZMm,
                            balance.RobotKeepOutHalfWidthMm, balance.RobotKeepOutHalfDepthMm));
            return rects;
        }

        /// <summary>
        /// Hinh tron ban kinh radiusMm tai (xMm, zMm) co cham vao hinh chu nhat khong.
        /// Cham dung mep thi cho qua, giong cach hai mon trang tri so khoang cach voi nhau.
        /// </summary>
        public static bool Touches(GardenRect rect, int xMm, int zMm, int radiusMm)
        {
            long dx = 0;
            if (xMm < rect.MinXMm) dx = rect.MinXMm - xMm;
            else if (xMm > rect.MaxXMm) dx = xMm - rect.MaxXMm;

            long dz = 0;
            if (zMm < rect.MinZMm) dz = rect.MinZMm - zMm;
            else if (zMm > rect.MaxZMm) dz = zMm - rect.MaxZMm;

            long radius = radiusMm;
            return dx * dx + dz * dz < radius * radius;
        }

        static int CenterMm(int index, int count, int spacingMm)
        {
            return (2 * index - (count - 1)) * spacingMm / 2;
        }

        static int HalfSpanMm(int count, int spacingMm, int edgeMm)
        {
            return (count - 1) * spacingMm / 2 + edgeMm / 2;
        }

        static GardenRect Build(GardenBlockKind kind, int centerXMm, int centerZMm,
                                int halfWidthMm, int halfDepthMm)
        {
            return new GardenRect
            {
                Kind = kind,
                MinXMm = centerXMm - halfWidthMm,
                MaxXMm = centerXMm + halfWidthMm,
                MinZMm = centerZMm - halfDepthMm,
                MaxZMm = centerZMm + halfDepthMm
            };
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
                    Description = "Lát lối đi quanh vườn.", SortOrder = 0,
                    BlocksWalking = false
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
