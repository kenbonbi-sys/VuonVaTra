using System.Collections.Generic;

namespace VuonNho.Core
{
    /// <summary>
    /// Balance v1 — nhip "thang nhanh, thuong nhanh".
    ///
    /// Khac v0 (muc 5 ke hoach) o cho v0 duoc dung cho cam giac thu gian: 30 giay mot vu,
    /// robot o phut 5,5. v1 rut chu ky xuong con vai giay de nguoi choi thay ket qua lien tay,
    /// va bu lai bang thang nang cap dai hon de mot phien 25-30 phut khong bi het do mua.
    ///
    /// Day van la gia tri khoi diem de test, chua phai thiet ke da chung minh la vui.
    /// Doi so o day roi chay lai test va bo QA trong build de do lai cac moc phut.
    /// </summary>
    public static class DefaultContent
    {
        /// <summary>
        /// Ky tu icon dong tien, dung THAY cho chu "xu" trong moi cau chu hien ra man hinh.
        ///
        /// Glyph nay da duoc ghep san vao font chu cua game (xem
        /// SourceArt/Fonts/merge_coin_glyph.py), nen no nam trong chinh dong chu chu khong phai
        /// mot o icon rieng ben canh — nho vay no dat duoc vao giua mot cau, cho ma mot o icon
        /// khong voi toi. Hang so nam o Core vi ca Core lan Views deu dung no.
        /// </summary>
        public const string CoinGlyph = "\ue53e";

        public const string CropMint = "crop_mint";
        public const string CropChamomile = "crop_chamomile";
        public const string CropStrawberry = "crop_strawberry";
        public const string CropLemongrass = "crop_lemongrass";
        public const string CropJasmine = "crop_jasmine";

        public const string RecipeMint = "recipe_mint";
        public const string RecipeChamomile = "recipe_chamomile";
        public const string RecipeStrawberry = "recipe_strawberry";
        public const string RecipeLemongrass = "recipe_lemongrass";
        public const string RecipeJasmine = "recipe_jasmine";

        public const string UpgradeRobot = "upgrade_robot";
        public const string UpgradeChamomile = "upgrade_unlock_chamomile";
        public const string UpgradeStrawberry = "upgrade_unlock_strawberry";
        public const string UpgradeExpand8 = "upgrade_expand_8";
        public const string UpgradeExpand12 = "upgrade_expand_12";
        public const string UpgradeGrowthSpeed = "upgrade_growth_speed";
        public const string UpgradeBrewSpeed = "upgrade_brew_speed";
        public const string UpgradeGrowthSpeed2 = "upgrade_growth_speed_2";
        public const string UpgradeBrewSpeed2 = "upgrade_brew_speed_2";
        public const string UpgradeLemongrass = "upgrade_unlock_lemongrass";
        public const string UpgradeJasmine = "upgrade_unlock_jasmine";
        public const string UpgradePestControl = "upgrade_pest_control";

        // Mat na mua, theo dung thu tu cua enum Season.
        public const int SeasonMaskXuan = 1 << (int)Season.Xuan;
        public const int SeasonMaskHa = 1 << (int)Season.Ha;
        public const int SeasonMaskThu = 1 << (int)Season.Thu;
        public const int SeasonMaskDong = 1 << (int)Season.Dong;

        public static ContentCatalog Create()
        {
            return Create(new BalanceConfig());
        }

        public static ContentCatalog Create(BalanceConfig balance)
        {
            var crops = new List<CropDefinition>
            {
                new CropDefinition
                {
                    Id = CropMint, DisplayName = "Bạc hà", BaseGrowthMs = 8000,
                    Yield = 4, RawSellPrice = 2, UnlockUpgradeId = null, SortOrder = 0
                },
                new CropDefinition
                {
                    Id = CropChamomile, DisplayName = "Cúc", BaseGrowthMs = 16000,
                    Yield = 4, RawSellPrice = 5, UnlockUpgradeId = UpgradeChamomile, SortOrder = 1,
                    SeasonMask = SeasonMaskXuan | SeasonMaskThu
                },
                new CropDefinition
                {
                    Id = CropStrawberry, DisplayName = "Dâu", BaseGrowthMs = 26000,
                    Yield = 4, RawSellPrice = 9, UnlockUpgradeId = UpgradeStrawberry, SortOrder = 2,
                    SeasonMask = SeasonMaskXuan | SeasonMaskHa
                },
                new CropDefinition
                {
                    Id = CropLemongrass, DisplayName = "Sả", BaseGrowthMs = 40000,
                    Yield = 4, RawSellPrice = 14, UnlockUpgradeId = UpgradeLemongrass, SortOrder = 3,
                    SeasonMask = SeasonMaskHa | SeasonMaskThu
                },
                new CropDefinition
                {
                    Id = CropJasmine, DisplayName = "Nhài", BaseGrowthMs = 60000,
                    Yield = 4, RawSellPrice = 22, UnlockUpgradeId = UpgradeJasmine, SortOrder = 4,
                    SeasonMask = SeasonMaskHa
                }
            };

            // Thoi gian pha duoc chon de 4 o cung loai cap vua du cho may: nguoi choi thay ro
            // rang mo them dat khong tu nhien lam tang tien tra khi may dang la gioi han.
            var recipes = new List<RecipeDefinition>
            {
                new RecipeDefinition
                {
                    Id = RecipeMint, DisplayName = "Trà bạc hà", InputCropId = CropMint, InputCount = 8,
                    BaseBrewMs = 4000, OutputCoins = 6, UnlockUpgradeId = null, SortOrder = 0,
                    PackedInputCount = 8, PackedOutputCoins = 30
                },
                new RecipeDefinition
                {
                    Id = RecipeChamomile, DisplayName = "Trà hoa cúc", InputCropId = CropChamomile, InputCount = 8,
                    BaseBrewMs = 6000, OutputCoins = 16, UnlockUpgradeId = UpgradeChamomile, SortOrder = 1,
                    PackedInputCount = 8, PackedOutputCoins = 80
                },
                new RecipeDefinition
                {
                    Id = RecipeStrawberry, DisplayName = "Trà dâu", InputCropId = CropStrawberry, InputCount = 8,
                    BaseBrewMs = 9000, OutputCoins = 30, UnlockUpgradeId = UpgradeStrawberry, SortOrder = 2,
                    PackedInputCount = 8, PackedOutputCoins = 150
                },
                new RecipeDefinition
                {
                    Id = RecipeLemongrass, DisplayName = "Trà sả", InputCropId = CropLemongrass, InputCount = 8,
                    BaseBrewMs = 13000, OutputCoins = 52, UnlockUpgradeId = UpgradeLemongrass, SortOrder = 3,
                    PackedInputCount = 8, PackedOutputCoins = 260
                },
                new RecipeDefinition
                {
                    Id = RecipeJasmine, DisplayName = "Trà nhài", InputCropId = CropJasmine, InputCount = 8,
                    BaseBrewMs = 18000, OutputCoins = 86, UnlockUpgradeId = UpgradeJasmine, SortOrder = 4,
                    PackedInputCount = 8, PackedOutputCoins = 430
                }
            };

            var upgrades = new List<UpgradeDefinition>
            {
                new UpgradeDefinition
                {
                    Id = UpgradeRobot, DisplayName = "Kích hoạt robot", Cost = 100,
                    Kind = UpgradeKind.UnlockRobot,
                    Description = "Tự thu và gieo lại các ô đã được chọn cây.",
                    SortOrder = 0
                },
                new UpgradeDefinition
                {
                    Id = UpgradeChamomile, DisplayName = "Mở cúc + công thức", Cost = 60,
                    Kind = UpgradeKind.UnlockCrop, TargetId = CropChamomile,
                    Description = "Cho chọn cây cúc và trà hoa cúc.",
                    SortOrder = 1
                },
                new UpgradeDefinition
                {
                    Id = UpgradeExpand8, DisplayName = "Mở vườn lần 1", Cost = 150,
                    Kind = UpgradeKind.ExpandPlots, IntValue = 8,
                    Description = "4 → 8 ô. Không tự tăng thu nhập nếu máy đã là giới hạn.",
                    SortOrder = 2
                },
                new UpgradeDefinition
                {
                    Id = UpgradeBrewSpeed, DisplayName = "Tốc độ máy I", Cost = 220,
                    Kind = UpgradeKind.BrewSpeed,
                    Description = "Thời gian pha × 0,5. Chỉ áp dụng cho mẻ bắt đầu sau khi mua.",
                    SortOrder = 3
                },
                new UpgradeDefinition
                {
                    Id = UpgradeStrawberry, DisplayName = "Mở dâu + công thức", Cost = 320,
                    Kind = UpgradeKind.UnlockCrop, TargetId = CropStrawberry,
                    RequiresUpgradeId = UpgradeChamomile,
                    Description = "Cho chọn cây dâu và trà dâu.",
                    SortOrder = 4
                },
                new UpgradeDefinition
                {
                    Id = UpgradeGrowthSpeed, DisplayName = "Tốc độ cây I", Cost = 320,
                    Kind = UpgradeKind.GrowthSpeed,
                    Description = "Thời gian lớn × 0,8. Chỉ áp dụng cho vụ gieo sau khi mua.",
                    SortOrder = 5
                },
                new UpgradeDefinition
                {
                    Id = UpgradeExpand12, DisplayName = "Mở vườn lần 2", Cost = 520,
                    Kind = UpgradeKind.ExpandPlots, IntValue = 12,
                    RequiresUpgradeId = UpgradeExpand8,
                    Description = "8 → 12 ô.",
                    SortOrder = 6
                },
                new UpgradeDefinition
                {
                    Id = UpgradeBrewSpeed2, DisplayName = "Tốc độ máy II", Cost = 900,
                    Kind = UpgradeKind.BrewSpeed,
                    RequiresUpgradeId = UpgradeBrewSpeed,
                    Description = "Thời gian pha × 0,5 một lần nữa.",
                    SortOrder = 7
                },
                new UpgradeDefinition
                {
                    Id = UpgradeGrowthSpeed2, DisplayName = "Tốc độ cây II", Cost = 900,
                    Kind = UpgradeKind.GrowthSpeed,
                    RequiresUpgradeId = UpgradeGrowthSpeed,
                    Description = "Thời gian lớn × 0,8 một lần nữa.",
                    SortOrder = 8
                },
                new UpgradeDefinition
                {
                    Id = UpgradeLemongrass, DisplayName = "Mở sả + công thức", Cost = 1400,
                    Kind = UpgradeKind.UnlockCrop, TargetId = CropLemongrass,
                    RequiresUpgradeId = UpgradeStrawberry,
                    Description = "Cho chọn cây sả và trà sả.",
                    SortOrder = 9
                },
                new UpgradeDefinition
                {
                    Id = UpgradePestControl, DisplayName = "Phòng trừ sinh học", Cost = 640,
                    Kind = UpgradeKind.PestControl, IntValue = 45,
                    Description = "Thiên địch và tỉa tán: sâu bệnh chỉ còn 45% số vụ.",
                    RequiresUpgradeId = UpgradeRobot, SortOrder = 25
                },
                new UpgradeDefinition
                {
                    Id = UpgradeJasmine, DisplayName = "Mở nhài + công thức", Cost = 2600,
                    Kind = UpgradeKind.UnlockCrop, TargetId = CropJasmine,
                    RequiresUpgradeId = UpgradeLemongrass,
                    Description = "Cho chọn cây nhài và trà nhài.",
                    SortOrder = 10
                }
            };

            return new ContentCatalog(balance, crops, recipes, upgrades,
                                      DefaultDecorations.Create(), DefaultStages.Create());
        }
    }
}
