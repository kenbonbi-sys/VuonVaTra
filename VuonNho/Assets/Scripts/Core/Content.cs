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

        /// <summary>
        /// Mua hop de trong, moi bit mot mua theo <see cref="Season"/>. 0 = mua nao cung duoc.
        ///
        /// Bac ha de tinh nen khong khai bao gi: nguoi choi moi vao game khong nen vap ngay vao
        /// mot cai luat ma ho chua co cach nao doc ra.
        /// </summary>
        public int SeasonMask;

        /// <summary>
        /// Cay che, hai theo phan hang bup — xem <see cref="Agronomy"/>. Chi cay che chiu bang
        /// sinh hoa theo mua va co the bi ray xanh chich hut; tra thao moc thi khong.
        /// </summary>
        public bool PluckGraded;

        /// <summary>
        /// Khong gieo duoc, chi sinh ra tu mot su kien trong vuon. Dong Phuong My Nhan la la cua
        /// mot o da bi ray xanh chich hut, khong ai gieo ra no duoc.
        /// </summary>
        public bool EventOnly;
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

        /// <summary>
        /// Bac thu hai cua cung cong thuc: pha tu tra da qua het day chuyen thay vi tu la tuoi.
        ///
        /// Day chuyen la duong **nang thu nhap**, khong phai mot cai cong chan duong. Neu quay
        /// tra chi nhan tra da dong goi thi nguoi choi moi phai mua sau cai may truoc khi kiem
        /// duoc dong xu dau tien tu tra — mot doan mo dau dai va cham ma khong ai xin.
        /// </summary>
        public int PackedInputCount;
        public long PackedOutputCoins;
    }

    public enum UpgradeKind
    {
        UnlockRobot,
        UnlockCrop,
        ExpandPlots,
        GrowthSpeed,
        BrewSpeed,
        /// <summary>Phong tru sinh hoc. IntValue = ty le sau benh CON LAI, tinh bang phan tram.</summary>
        PestControl,
        /// <summary>Do bao ho y te cho tho. Thieu no thi ky kiem tra nao cung bi phat.</summary>
        ProtectiveGear
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

        // --- canh tac: dat, co dai, sau benh, thoi vu
        /// <summary>Do phi tru di moi lan gieo. 100 do phi = muoi vu neu khong bon gi them.</summary>
        public int PlantFertilityCost = 4;
        /// <summary>
        /// Dat tu hoi bao lau mot diem do phi.
        ///
        /// Phai nhanh hon **luong rut ra moi giay** cua nhip gieo, khong phai chi nhanh hon mot
        /// chut: bac ha rut 4 diem moi 9 giay, tuc 0,44 diem mot giay. Cham hon con so do thi do
        /// phi tut ve 0 va NaturalFertilityCap tro thanh vo nghia — dat khong bao gio cham toi no.
        /// Nhanh hon thi dat dung o dung muc tran, va tran moi la thu quyet dinh san luong
        /// ma thanh mot cai doc: gieo lien tuc rut nhanh hon hoi thi khong co cach nao giu dat tot.
        /// Tu hoi chi len toi NaturalFertilityCap — muon cao hon phai bon, va do la cho tien di ra.
        /// </summary>
        public long FertilityRegenMs = 2000;
        /// <summary>Tu hoi chi len toi day. Muon cao hon phai bon — do la cho tien di ra.</summary>
        public int NaturalFertilityCap = 45;
        /// <summary>Gia mot lan bon phan huu co, va do phi sau khi bon.</summary>
        public long CompostCost = 40;
        public int CompostFertility = 100;

        /// <summary>Co moc them mot diem sau moi quang nay, tren moi o da mo.</summary>
        public long WeedGrowthMs = 40000;
        /// <summary>Co day thi cay lon cham hon bay nhieu phan tram.</summary>
        public int FullWeedGrowthPenaltyPercent = 60;

        /// <summary>Bao nhieu phan tram so vu dinh sau benh.</summary>
        public int PestChancePercent = 20;
        /// <summary>Gia mot lan phong tru sinh hoc.</summary>
        public long PestTreatmentCost = 28;

        /// <summary>Mot mua dai bao lau, tinh bang thoi gian mo phong.</summary>
        public long SeasonLengthMs = 240000;
        /// <summary>Trong trai vu thi chi con bay nhieu phan tram nang suat.</summary>
        public int OffSeasonYieldPercent = 50;

        // --- robot thu hoach
        /// <summary>Toc do robot di trong vuon, milimet moi giay.</summary>
        public int RobotSpeedMmPerSecond = 7800;
        /// <summary>Robot dung lai bao lau de thu mot o. Phai duong, neu khong vong su kien khong dung.</summary>
        public long RobotHarvestMs = 350;

        // --- tho che bien
        /// <summary>Gia thue mot tho. Tra mot lan luc thue.</summary>
        public long WorkerHireCost = 300;
        /// <summary>Luong mot tho cho moi ky tra luong.</summary>
        public long WorkerWageCoins = 12;
        /// <summary>Bao lau tra luong mot lan, tinh bang thoi gian mo phong.</summary>
        public long PayrollPeriodMs = 60000;
        /// <summary>Tran so tho thue duoc. Bang so may, thue them nua cung khong chay them may nao.</summary>
        public int MaximumWorkers = 6;

        // --- tai chinh: von, vay ngan hang va dong tien (muc 1 cua ban mo phong)
        /// <summary>
        /// Bao nhieu xu la mot trieu dong. Mot cho duy nhat quy doi giua don vi cua game va don
        /// vi cua ban mo phong — doi so nay la doi het moi con so tai chinh hien ra man hinh.
        /// </summary>
        public int CoinsPerMillionVnd = 10;

        /// <summary>Von tu co dau game trong kich ban, trieu dong. Chi de ke va de doi chieu.</summary>
        public long SeedCapitalMillionVnd = 200;

        /// <summary>Han muc vay tin chap: 200 trieu, lai 6,5%–8,5%/nam.</summary>
        public long UnsecuredLoanCapCoins = 2000;
        public int UnsecuredMinRateBps = 650;
        public int UnsecuredMaxRateBps = 850;

        /// <summary>Han muc the chap: 70% dinh gia so do 500 trieu, lai 7,5%–9,0%/nam.</summary>
        public long SecuredLoanCapCoins = 3500;
        public int SecuredMinRateBps = 750;
        public int SecuredMaxRateBps = 900;

        public int LoanMinTermMonths = 12;
        public int LoanMaxTermMonths = 36;

        /// <summary>Mot mua chia lam bao nhieu ky tra no. Mua la mot quy, nen ba thang.</summary>
        public int MonthsPerSeason = 3;

        /// <summary>Bao nhieu ky lien tiep khong tra du thi ngan hang niem phong nuong che.</summary>
        public int LoanSealShortfalls = 3;

        /// <summary>Giu lai bao nhieu ky gan nhat cho do thi dong tien.</summary>
        public int CashHistoryCycles = 12;

        // --- phap ly va an toan thuc pham (muc 4)
        /// <summary>Le phi va thoi gian tham dinh giay an toan thuc pham.</summary>
        public long FoodSafetyFeeCoins = 180;
        public int FoodSafetyMinDays = 15;
        public int FoodSafetyMaxDays = 45;

        /// <summary>Bao nhieu ky thi doan kiem tra ghe mot lan.</summary>
        public int InspectionEveryCycles = 2;

        /// <summary>Bi dinh chi thi dung san xuat bao lau, tinh bang ky.</summary>
        public int SuspensionCycles = 1;

        // --- che bien (muc 3)
        /// <summary>He so gia cua me tra thuong hang va cua me bi loi, phan tram.</summary>
        public int PremiumBatchPricePercent = 130;
        public int FlawedBatchPricePercent = 55;

        // --- ray xanh va Dong Phuong My Nhan (muc 2)
        /// <summary>Bao nhieu phan tram so vu che bi ray xanh chich hut, trong mua ray.</summary>
        public int LeafhopperChancePercent = 22;

        /// <summary>
        /// La ray xanh bi lam sai duong thi chi con bay nhieu phan tram gia. Dong Phuong My Nhan
        /// dat gap nam lan hong tra thuong, nen lam sai la mat gan het phan chenh do.
        /// </summary>
        public int OrientalBeautyFallbackPercent = 20;

        // --- du lich trai nghiem (muc 5)
        public long FarmstayBaseIncomeCoins = 40;
        public long FarmstayIncomePerDecorationCoins = 12;

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
        readonly Dictionary<string, ProcessStageDefinition> _stages = new Dictionary<string, ProcessStageDefinition>(StringComparer.Ordinal);

        public readonly List<CropDefinition> Crops = new List<CropDefinition>();
        public readonly List<RecipeDefinition> Recipes = new List<RecipeDefinition>();
        public readonly List<UpgradeDefinition> Upgrades = new List<UpgradeDefinition>();
        public readonly List<DecorationDefinition> Decorations = new List<DecorationDefinition>();

        /// <summary>Sau cong doan che bien, theo dung thu tu day chuyen.</summary>
        public readonly List<ProcessStageDefinition> Stages = new List<ProcessStageDefinition>();

        /// <summary>
        /// Moi ten mat hang co the nam trong kho, theo mot thu tu co dinh: la tuoi cua tung cay,
        /// roi tung chang che bien cua cay do.
        ///
        /// Co danh sach nay thi save ghi kho theo mot thu tu on dinh — hai lan ghi cung mot state
        /// cho ra cung mot chuoi byte — va HUD khong phai tu doan ra ten mat hang trung gian.
        /// </summary>
        public readonly List<string> Items = new List<string>();

        public BalanceConfig Balance { get; private set; }

        public ContentCatalog(BalanceConfig balance,
                              IEnumerable<CropDefinition> crops,
                              IEnumerable<RecipeDefinition> recipes,
                              IEnumerable<UpgradeDefinition> upgrades,
                              IEnumerable<DecorationDefinition> decorations = null,
                              IEnumerable<ProcessStageDefinition> stages = null)
        {
            if (balance == null) throw new ContentValidationException("BalanceConfig khong duoc null.");
            Balance = balance;
            foreach (var c in crops) AddCrop(c);
            foreach (var r in recipes) AddRecipe(r);
            foreach (var u in upgrades) AddUpgrade(u);
            if (decorations != null) foreach (var d in decorations) AddDecoration(d);
            if (stages != null) foreach (var g in stages) AddStage(g);
            Decorations.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            Crops.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            Recipes.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            Upgrades.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            Stages.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            BuildItemList();
            Validate();
        }

        void BuildItemList()
        {
            Items.Clear();
            for (int i = 0; i < Crops.Count; i++)
            {
                Items.Add(Crops[i].Id);
                for (int j = 0; j < Stages.Count; j++)
                    Items.Add(ProcessChain.ItemId(Crops[i].Id, Stages[j].OutputSuffix));
            }
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

        void AddStage(ProcessStageDefinition g)
        {
            if (g == null || string.IsNullOrEmpty(g.Id)) throw new ContentValidationException("Cong doan thieu id.");
            if (_stages.ContainsKey(g.Id)) throw new ContentValidationException("Trung id cong doan: " + g.Id);
            _stages.Add(g.Id, g);
            Stages.Add(g);
        }

        public bool TryGetStage(string id, out ProcessStageDefinition stage)
        {
            stage = null;
            return id != null && _stages.TryGetValue(id, out stage);
        }

        public ProcessStageDefinition Stage(string id)
        {
            ProcessStageDefinition g;
            if (!TryGetStage(id, out g)) throw new ContentValidationException("Khong co cong doan: " + id);
            return g;
        }

        /// <summary>Duoi ten cua chang cuoi cung — mat hang ma quay tra tra gia cao.</summary>
        public string PackedSuffix
        {
            get { return Stages.Count == 0 ? null : Stages[Stages.Count - 1].OutputSuffix; }
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
                // Do phi chia theo phan tram roi lam tron xuong. Yield qua nho thi ca thang do phi
                // don ve cung mot con so, va he do phi tro thanh vo hinh voi nguoi choi.
                if (c.Yield < 4)
                    throw new ContentValidationException("Yield phai tu 4 tro len de do phi co y nghia: " + c.Id);
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
                if (r.PackedInputCount < 0 || r.PackedOutputCoins < 0)
                    throw new ContentValidationException("Bac tra dong goi am: " + r.Id);
                if (r.PackedInputCount > 0 && r.PackedOutputCoins <= r.OutputCoins)
                    throw new ContentValidationException(
                        "Tra dong goi khong tra hon tra tu la tuoi thi khong ai xay day chuyen: " + r.Id);
            }

            if (Balance.PlantFertilityCost < 0 || Balance.CompostCost < 0 || Balance.PestTreatmentCost < 0)
                throw new ContentValidationException("Gia canh tac am.");
            if (Balance.FertilityRegenMs <= 0 || Balance.WeedGrowthMs <= 0 || Balance.SeasonLengthMs <= 0)
                throw new ContentValidationException("Chu ky canh tac phai duong.");
            if (Balance.NaturalFertilityCap < 0 || Balance.NaturalFertilityCap > 100)
                throw new ContentValidationException("Tran do phi tu hoi phai nam trong 0..100.");
            if (Balance.CompostFertility <= 0 || Balance.CompostFertility > 100)
                throw new ContentValidationException("Do phi sau khi bon phai nam trong 1..100.");
            if (Balance.PestChancePercent < 0 || Balance.PestChancePercent > 100)
                throw new ContentValidationException("Ty le sau benh phai nam trong 0..100.");
            if (Balance.OffSeasonYieldPercent < 0 || Balance.OffSeasonYieldPercent > 100)
                throw new ContentValidationException("Nang suat trai vu phai nam trong 0..100.");
            if (Balance.FullWeedGrowthPenaltyPercent < 0)
                throw new ContentValidationException("Phat co dai am.");

            if (Balance.RobotSpeedMmPerSecond <= 0)
                throw new ContentValidationException("Toc do robot phai duong.");
            // Robot thu tuc thi thi "ranh va co o chin" se lap lai mai o cung mot moc thoi gian.
            if (Balance.RobotHarvestMs <= 0)
                throw new ContentValidationException("Thoi gian robot thu mot o phai duong.");

            if (Balance.WorkerHireCost < 0 || Balance.WorkerWageCoins < 0)
                throw new ContentValidationException("Gia thue hoac luong tho am.");
            if (Balance.PayrollPeriodMs <= 0)
                throw new ContentValidationException("Ky tra luong phai duong.");
            if (Balance.MaximumWorkers < 0)
                throw new ContentValidationException("Tran so tho am.");

            // --- tai chinh
            if (Balance.CoinsPerMillionVnd <= 0)
                throw new ContentValidationException("Ti le quy doi xu ra trieu dong phai duong.");
            if (Balance.MonthsPerSeason <= 0)
                throw new ContentValidationException("So thang mot mua phai duong.");
            // Mot ky ngan hon mot millisecond thi vong su kien se quay tai cho o moc den han.
            if (Balance.SeasonLengthMs / Balance.MonthsPerSeason <= 0)
                throw new ContentValidationException("Ky tra no ngan hon mot millisecond.");
            if (Balance.UnsecuredLoanCapCoins < 0 || Balance.SecuredLoanCapCoins < 0)
                throw new ContentValidationException("Han muc vay am.");
            if (Balance.UnsecuredMinRateBps < 0 || Balance.UnsecuredMaxRateBps < Balance.UnsecuredMinRateBps)
                throw new ContentValidationException("Khoang lai suat tin chap khong hop le.");
            if (Balance.SecuredMinRateBps < 0 || Balance.SecuredMaxRateBps < Balance.SecuredMinRateBps)
                throw new ContentValidationException("Khoang lai suat the chap khong hop le.");
            if (Balance.LoanMinTermMonths <= 0 || Balance.LoanMaxTermMonths < Balance.LoanMinTermMonths)
                throw new ContentValidationException("Khoang thoi han vay khong hop le.");
            if (Balance.LoanSealShortfalls <= 0)
                throw new ContentValidationException("Nguong siet no phai duong.");
            if (Balance.CashHistoryCycles <= 0)
                throw new ContentValidationException("So ky luu cho do thi dong tien phai duong.");

            // --- phap ly
            if (Balance.FoodSafetyFeeCoins < 0)
                throw new ContentValidationException("Le phi giay an toan thuc pham am.");
            if (Balance.FoodSafetyMinDays <= 0 || Balance.FoodSafetyMaxDays < Balance.FoodSafetyMinDays)
                throw new ContentValidationException("Khoang tham dinh giay an toan thuc pham khong hop le.");
            if (Balance.InspectionEveryCycles <= 0)
                throw new ContentValidationException("Ky kiem tra phai duong.");
            if (Balance.SuspensionCycles < 0)
                throw new ContentValidationException("So ky dinh chi am.");

            // --- che bien va thoi vu
            if (Balance.PremiumBatchPricePercent < 100)
                throw new ContentValidationException("Me thuong hang phai duoc gia cao hon me dat.");
            if (Balance.FlawedBatchPricePercent < 0 || Balance.FlawedBatchPricePercent > 100)
                throw new ContentValidationException("He so gia me loi phai nam trong 0..100.");
            if (Balance.LeafhopperChancePercent < 0 || Balance.LeafhopperChancePercent > 100)
                throw new ContentValidationException("Ty le ray xanh phai nam trong 0..100.");
            if (Balance.OrientalBeautyFallbackPercent < 0 || Balance.OrientalBeautyFallbackPercent > 100)
                throw new ContentValidationException("He so la ray xanh lam sai phai nam trong 0..100.");
            if (Balance.FarmstayBaseIncomeCoins < 0 || Balance.FarmstayIncomePerDecorationCoins < 0)
                throw new ContentValidationException("Thu nhap du lich am.");

            // Cay che phai co du bon phan hang, va phan hang nao cung phai tro toi mot cay co that:
            // thieu mot phan hang thi bang thu hai trong so tay se co mot dong khong bam duoc.
            var grades = Agronomy.Grades();
            for (int i = 0; i < grades.Count; i++)
            {
                CropDefinition crop;
                if (!TryGetCrop(grades[i].CropId, out crop))
                    throw new ContentValidationException("Phan hang thu hai tro toi cay khong ton tai: " +
                                                        grades[i].CropId);
                if (!crop.PluckGraded)
                    throw new ContentValidationException("Cay cua phan hang thu hai phai la cay che: " +
                                                        crop.Id);
                if (grades[i].FreshPerDryPermille <= 0)
                    throw new ContentValidationException("Ty le tuoi tren kho phai duong: " + grades[i].CropId);
            }

            // Day chuyen phai noi lien: dau vao cua chang sau dung bang dau ra cua chang truoc.
            // Dut mot mat xich thi mot cai may se khong bao gio nhan duoc nguyen lieu, va loi do
            // khong bao gi ca — no chi hien ra thanh mot cai may nam khong mai mai.
            string expected = ProcessChain.SuffixFresh;
            foreach (var g in Stages)
            {
                if (g.BaseProcessMs <= 0)
                    throw new ContentValidationException("Thoi gian che bien phai duong: " + g.Id);
                if (g.InputCount <= 0 || g.OutputCount <= 0)
                    throw new ContentValidationException("So luong vao/ra phai duong: " + g.Id);
                if (g.Cost < 0) throw new ContentValidationException("Gia may am: " + g.Id);
                if (string.IsNullOrEmpty(g.OutputSuffix))
                    throw new ContentValidationException("Chang phai nha ra mat hang khac la tuoi: " + g.Id);
                if (!string.Equals(g.InputSuffix ?? "", expected, StringComparison.Ordinal))
                    throw new ContentValidationException(
                        "Day chuyen dut o " + g.Id + ": cho dau vao \"" + expected +
                        "\" nhung nhan \"" + (g.InputSuffix ?? "") + "\".");
                expected = g.OutputSuffix;
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
