using System;
using System.Collections.Generic;

namespace VuonNho.Core
{
    /// <summary>Cay trong. Thoi gian tinh bang millisecond, tien va so luong la so nguyen.</summary>
    public sealed class CropDefinition
    {
        public string Id;
        public string DisplayName;
        public long BaseGrowthMs;
        public int Yield;
        public long RawSellPrice;
        /// <summary>Null = mo san tu dau.</summary>
        public string UnlockUpgradeId;
        public int SortOrder;
    }

    public sealed class RecipeDefinition
    {
        public string Id;
        public string DisplayName;
        public string InputCropId;
        public int InputCount;
        public long BaseBrewMs;
        public long OutputCoins;
        public string UnlockUpgradeId;
        public int SortOrder;
    }

    public enum UpgradeKind
    {
        UnlockRobot,
        UnlockCrop,
        ExpandPlots,
        GrowthSpeed,
        BrewSpeed
    }

    public sealed class UpgradeDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public long Cost;
        public UpgradeKind Kind;
        /// <summary>ExpandPlots: tong so o sau khi mua.</summary>
        public int IntValue;
        /// <summary>UnlockCrop: cropId duoc mo.</summary>
        public string TargetId;
        public string RequiresUpgradeId;
        public int SortOrder;
    }

    /// <summary>Cac so cua balance v0 (muc 5 cua ke hoach MVP).</summary>
    public sealed class BalanceConfig
    {
        public string Version = "balance-v1";
        public int InitialPlots = 4;
        public int MaximumPlots = 12;
        public long OfflineCapMs = 14400000;   // 4 gio
        public long AutosaveIntervalMs = 15000;
        public long OnlineTickMs = 250;
        /// <summary>Nua canh khu vuc duoc phep dat trang tri, milimet.</summary>
        public int GardenHalfExtentMm = 8000;

        // --- Bo cuc vuon, milimet. Phai khop voi GardenSkin ben Unity; SceneFactory doi
        // chieu lai luc dung scene va canh bao neu hai nguon so lech nhau.
        public int GardenColumns = 4;
        public int GardenRows = 3;
        /// <summary>Khoang cach tam hai o dat lien nhau.</summary>
        public int PlotSpacingMm = 1600;
        /// <summary>Canh mat dat cua mot o.</summary>
        public int PlotEdgeMm = 1400;
        /// <summary>
        /// Canh vung cam quanh mot o dat. Nho hon canh o that mot vien 10 cm moi ben,
        /// de con lat duoc phien da vao khe giua bon o.
        /// </summary>
        public int PlotKeepOutEdgeMm = 1200;
        /// <summary>Tam quay tra va nua kich thuoc vung cam quanh no, lay theo mai quay.</summary>
        public int StationCenterXMm = 0;
        public int StationCenterZMm = 4200;
        public int StationKeepOutHalfWidthMm = 1700;
        public int StationKeepOutHalfDepthMm = 900;
        /// <summary>Tam robot va nua kich thuoc vung cam quanh no.</summary>
        public int RobotCenterXMm = -4400;
        public int RobotCenterZMm = 400;
        public int RobotKeepOutHalfWidthMm = 700;
        public int RobotKeepOutHalfDepthMm = 700;

        // Toc do cay x0,8 -> 4/5. Toc do may x0,5 -> 1/2.
        public int GrowthSpeedNumerator = 4;
        public int GrowthSpeedDenominator = 5;
        public int BrewSpeedNumerator = 1;
        public int BrewSpeedDenominator = 2;

        public BalanceConfig Clone()
        {
            return (BalanceConfig)MemberwiseClone();
        }
    }

    public sealed class ContentValidationException : Exception
    {
        public ContentValidationException(string message) : base(message) { }
    }

    /// <summary>Cau hinh chi doc duoc dung luc khoi dong. Khong giu tien trinh nguoi choi.</summary>
    public sealed class ContentCatalog
    {
        readonly Dictionary<string, CropDefinition> _crops = new Dictionary<string, CropDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, RecipeDefinition> _recipes = new Dictionary<string, RecipeDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, UpgradeDefinition> _upgrades = new Dictionary<string, UpgradeDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, DecorationDefinition> _decorations = new Dictionary<string, DecorationDefinition>(StringComparer.Ordinal);

        public readonly List<CropDefinition> Crops = new List<CropDefinition>();
        public readonly List<RecipeDefinition> Recipes = new List<RecipeDefinition>();
        public readonly List<UpgradeDefinition> Upgrades = new List<UpgradeDefinition>();
        public readonly List<DecorationDefinition> Decorations = new List<DecorationDefinition>();
        public BalanceConfig Balance { get; private set; }

        public ContentCatalog(BalanceConfig balance,
                              IEnumerable<CropDefinition> crops,
                              IEnumerable<RecipeDefinition> recipes,
                              IEnumerable<UpgradeDefinition> upgrades,
                              IEnumerable<DecorationDefinition> decorations = null)
        {
            if (balance == null) throw new ContentValidationException("BalanceConfig khong duoc null.");
            Balance = balance;
            foreach (var c in crops) AddCrop(c);
            foreach (var r in recipes) AddRecipe(r);
            foreach (var u in upgrades) AddUpgrade(u);
            if (decorations != null) foreach (var d in decorations) AddDecoration(d);
            Decorations.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            Crops.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            Recipes.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            Upgrades.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            Validate();
        }

        void AddCrop(CropDefinition c)
        {
            if (c == null || string.IsNullOrEmpty(c.Id)) throw new ContentValidationException("Cay thieu id.");
            if (_crops.ContainsKey(c.Id)) throw new ContentValidationException("Trung id cay: " + c.Id);
            _crops.Add(c.Id, c);
            Crops.Add(c);
        }

        void AddRecipe(RecipeDefinition r)
        {
            if (r == null || string.IsNullOrEmpty(r.Id)) throw new ContentValidationException("Cong thuc thieu id.");
            if (_recipes.ContainsKey(r.Id)) throw new ContentValidationException("Trung id cong thuc: " + r.Id);
            _recipes.Add(r.Id, r);
            Recipes.Add(r);
        }

        void AddUpgrade(UpgradeDefinition u)
        {
            if (u == null || string.IsNullOrEmpty(u.Id)) throw new ContentValidationException("Nang cap thieu id.");
            if (_upgrades.ContainsKey(u.Id)) throw new ContentValidationException("Trung id nang cap: " + u.Id);
            _upgrades.Add(u.Id, u);
            Upgrades.Add(u);
        }

        void AddDecoration(DecorationDefinition d)
        {
            if (d == null || string.IsNullOrEmpty(d.Id)) throw new ContentValidationException("Trang tri thieu id.");
            if (_decorations.ContainsKey(d.Id)) throw new ContentValidationException("Trung id trang tri: " + d.Id);
            _decorations.Add(d.Id, d);
            Decorations.Add(d);
        }

        public bool TryGetDecoration(string id, out DecorationDefinition decoration)
        {
            decoration = null;
            return id != null && _decorations.TryGetValue(id, out decoration);
        }

        public DecorationDefinition Decoration(string id)
        {
            DecorationDefinition d;
            if (!TryGetDecoration(id, out d)) throw new ContentValidationException("Khong co trang tri: " + id);
            return d;
        }

        /// <summary>Tu choi id trung/khong ton tai, thoi gian khong duong, gia am, yield khong duong.</summary>
        public void Validate()
        {
            if (Balance.InitialPlots <= 0 || Balance.MaximumPlots < Balance.InitialPlots)
                throw new ContentValidationException("So o dat khong hop le.");
            if (Balance.OfflineCapMs <= 0) throw new ContentValidationException("OfflineCapMs phai duong.");
            if (Balance.GardenHalfExtentMm <= 0)
                throw new ContentValidationException("GardenHalfExtentMm phai duong.");
            if (Balance.GardenColumns <= 0 || Balance.GardenRows <= 0)
                throw new ContentValidationException("So cot va so hang o dat phai duong.");
            if (Balance.GardenColumns * Balance.GardenRows != Balance.MaximumPlots)
                throw new ContentValidationException("Luoi o dat khong khop MaximumPlots.");
            if (Balance.PlotSpacingMm <= 0 || Balance.PlotEdgeMm <= 0)
                throw new ContentValidationException("Kich thuoc o dat phai duong.");
            if (Balance.PlotEdgeMm > Balance.PlotSpacingMm)
                throw new ContentValidationException("Canh o dat lon hon khoang cach tam o.");
            if (Balance.PlotKeepOutEdgeMm <= 0 || Balance.PlotKeepOutEdgeMm > Balance.PlotEdgeMm)
                throw new ContentValidationException("Vung cam quanh o dat khong hop le.");
            if (Balance.StationKeepOutHalfWidthMm <= 0 || Balance.StationKeepOutHalfDepthMm <= 0)
                throw new ContentValidationException("Vung cam quanh quay tra phai duong.");
            if (Balance.RobotKeepOutHalfWidthMm <= 0 || Balance.RobotKeepOutHalfDepthMm <= 0)
                throw new ContentValidationException("Vung cam quanh robot phai duong.");
            // Khu dat trang tri phai chua het luoi o dat, khong thi co o nam ngoai vung kiem tra.
            if (GardenLayout.GridHalfWidthMm(Balance) > Balance.GardenHalfExtentMm ||
                GardenLayout.GridHalfDepthMm(Balance) > Balance.GardenHalfExtentMm)
                throw new ContentValidationException("Khu vuon khong chua het luoi o dat.");
            if (Balance.GrowthSpeedDenominator <= 0 || Balance.GrowthSpeedNumerator <= 0)
                throw new ContentValidationException("He so toc do cay khong hop le.");
            if (Balance.BrewSpeedDenominator <= 0 || Balance.BrewSpeedNumerator <= 0)
                throw new ContentValidationException("He so toc do may khong hop le.");

            foreach (var c in Crops)
            {
                if (c.BaseGrowthMs <= 0) throw new ContentValidationException("Thoi gian lon phai duong: " + c.Id);
                if (c.Yield <= 0) throw new ContentValidationException("Yield phai duong: " + c.Id);
                if (c.RawSellPrice < 0) throw new ContentValidationException("Gia ban tho am: " + c.Id);
                if (c.UnlockUpgradeId != null && !_upgrades.ContainsKey(c.UnlockUpgradeId))
                    throw new ContentValidationException("Cay tro toi nang cap khong ton tai: " + c.Id);
                // Chu ky sau khi nhan he so toc do van phai duong de tranh vong lap su kien vo han.
                if (ScaleGrowth(c.BaseGrowthMs, 1) <= 0)
                    throw new ContentValidationException("Toc do cay lam chu ky bang 0: " + c.Id);
            }

            foreach (var r in Recipes)
            {
                if (r.BaseBrewMs <= 0) throw new ContentValidationException("Thoi gian pha phai duong: " + r.Id);
                if (r.InputCount <= 0) throw new ContentValidationException("So nguyen lieu phai duong: " + r.Id);
                if (r.OutputCoins < 0) throw new ContentValidationException("Xu moi me am: " + r.Id);
                if (!_crops.ContainsKey(r.InputCropId))
                    throw new ContentValidationException("Cong thuc dung cay khong ton tai: " + r.Id);
                if (r.UnlockUpgradeId != null && !_upgrades.ContainsKey(r.UnlockUpgradeId))
                    throw new ContentValidationException("Cong thuc tro toi nang cap khong ton tai: " + r.Id);
                if (ScaleBrew(r.BaseBrewMs, 1) <= 0)
                    throw new ContentValidationException("Toc do may lam chu ky bang 0: " + r.Id);
            }

            foreach (var d in Decorations)
            {
                if (d.Cost < 0) throw new ContentValidationException("Gia trang tri am: " + d.Id);
                if (d.FootprintMm <= 0) throw new ContentValidationException("Footprint phai duong: " + d.Id);
                if (d.FootprintMm > Balance.GardenHalfExtentMm)
                    throw new ContentValidationException("Trang tri lon hon ca khu vuon: " + d.Id);
            }

            foreach (var u in Upgrades)
            {
                if (u.Cost < 0) throw new ContentValidationException("Gia nang cap am: " + u.Id);
                if (u.RequiresUpgradeId != null && !_upgrades.ContainsKey(u.RequiresUpgradeId))
                    throw new ContentValidationException("Dieu kien tro toi nang cap khong ton tai: " + u.Id);
                if (u.Kind == UpgradeKind.UnlockCrop && !_crops.ContainsKey(u.TargetId))
                    throw new ContentValidationException("Nang cap mo cay khong ton tai: " + u.Id);
                if (u.Kind == UpgradeKind.ExpandPlots &&
                    (u.IntValue <= Balance.InitialPlots || u.IntValue > Balance.MaximumPlots))
                    throw new ContentValidationException("Moc mo dat khong hop le: " + u.Id);
            }
        }

        public bool TryGetCrop(string id, out CropDefinition crop)
        {
            crop = null;
            return id != null && _crops.TryGetValue(id, out crop);
        }

        public bool TryGetRecipe(string id, out RecipeDefinition recipe)
        {
            recipe = null;
            return id != null && _recipes.TryGetValue(id, out recipe);
        }

        public bool TryGetUpgrade(string id, out UpgradeDefinition upgrade)
        {
            upgrade = null;
            return id != null && _upgrades.TryGetValue(id, out upgrade);
        }

        public CropDefinition Crop(string id)
        {
            CropDefinition c;
            if (!TryGetCrop(id, out c)) throw new ContentValidationException("Khong co cay: " + id);
            return c;
        }

        public RecipeDefinition Recipe(string id)
        {
            RecipeDefinition r;
            if (!TryGetRecipe(id, out r)) throw new ContentValidationException("Khong co cong thuc: " + id);
            return r;
        }

        public UpgradeDefinition Upgrade(string id)
        {
            UpgradeDefinition u;
            if (!TryGetUpgrade(id, out u)) throw new ContentValidationException("Khong co nang cap: " + id);
            return u;
        }

        /// <summary>Cong thuc dau tien dung loai cay nay, theo thu tu hien thi.</summary>
        public RecipeDefinition RecipeForCrop(string cropId)
        {
            for (int i = 0; i < Recipes.Count; i++)
                if (string.Equals(Recipes[i].InputCropId, cropId, StringComparison.Ordinal))
                    return Recipes[i];
            return null;
        }

        public long ScaleGrowth(long baseMs, int level)
        {
            long value = baseMs;
            for (int i = 0; i < level; i++)
                value = value * Balance.GrowthSpeedNumerator / Balance.GrowthSpeedDenominator;
            return value < 1 ? 1 : value;
        }

        public long ScaleBrew(long baseMs, int level)
        {
            long value = baseMs;
            for (int i = 0; i < level; i++)
                value = value * Balance.BrewSpeedNumerator / Balance.BrewSpeedDenominator;
            return value < 1 ? 1 : value;
        }
    }
}
