namespace VuonNho.Core
{
    /// <summary>Bon mua lap lai. Chi suy ra tu SimulationTimeMs, khong luu trong save.</summary>
    public enum Season
    {
        Xuan = 0,
        Ha = 1,
        Thu = 2,
        Dong = 3
    }

    /// <summary>
    /// Luat canh tac: do phi cua dat, co dai, sau benh va thoi vu.
    ///
    /// Bon he deu doi vao **mot con so duy nhat cua moi o** roi dung o do, thay vi dan vao nhau:
    /// do phi va thoi vu quyet dinh **thu duoc bao nhieu**, co dai quyet dinh **lon nhanh hay
    /// cham**, sau benh quyet dinh **co mat trang vu hay khong**. Nguoi choi nhin mot o dang kem
    /// la doc ra duoc ngay la thieu cai gi; ba he cung bop mot cho thi khong ai biet vi sao.
    ///
    /// Tat ca deu la ham thuan tuy tren so nguyen: cung dau vao cho cung dau ra, khong dong ho
    /// that, khong Random. Nho vay chay bu offline va chay online ra cung mot ket qua.
    /// </summary>
    public static class Cultivation
    {
        public static Season SeasonAt(BalanceConfig balance, long simulationTimeMs)
        {
            if (balance.SeasonLengthMs <= 0) return Season.Xuan;
            long index = simulationTimeMs / balance.SeasonLengthMs;
            return (Season)(int)(((index % 4) + 4) % 4);
        }

        /// <summary>Bao lau nua thi sang mua khac. Dung lam deadline nhu moi deadline khac.</summary>
        public static long NextSeasonChangeMs(BalanceConfig balance, long simulationTimeMs)
        {
            if (balance.SeasonLengthMs <= 0) return long.MaxValue;
            long elapsed = simulationTimeMs % balance.SeasonLengthMs;
            return simulationTimeMs + (balance.SeasonLengthMs - elapsed);
        }

        public static bool IsInSeason(CropDefinition crop, Season season)
        {
            // Khong khai bao mua nao thi trong luc nao cung duoc — cay de tinh cua thang dau game.
            if (crop == null || crop.SeasonMask == 0) return true;
            return (crop.SeasonMask & (1 << (int)season)) != 0;
        }

        /// <summary>
        /// So don vi thu duoc, chot ngay luc gieo.
        ///
        /// Chot luc gieo chu khong tinh luc thu la co y, va giong dung cach PendingYield van lam:
        /// bon phan giua vu khong cuu duoc vu dang chay, nen nguoi choi phai lo dat TRUOC khi gieo.
        /// </summary>
        public static int YieldFor(BalanceConfig balance, CropDefinition crop, int fertility, bool inSeason)
        {
            if (crop == null) return 0;
            // Lam tron chu khong cat cut: Yield chi co bon bac, cat cut lam mot o do phi 96 tut
            // xuong cung bac voi o do phi 75, va nguoi choi vua bon phan xong khong thay gi doi.
            int yield = (crop.Yield * Clamp(fertility, 0, 100) + 50) / 100;
            if (!inSeason) yield = yield * balance.OffSeasonYieldPercent / 100;
            return yield < 0 ? 0 : yield;
        }

        /// <summary>Co dai lam cay lon cham. 0 co = 100%, day co = 100% + OverWeedGrowthPercent.</summary>
        public static long GrowthWithWeeds(BalanceConfig balance, long growthMs, int weeds)
        {
            int extra = Clamp(weeds, 0, 100) * balance.FullWeedGrowthPenaltyPercent / 100;
            long slowed = growthMs * (100 + extra) / 100;
            return slowed < 1 ? 1 : slowed;
        }

        /// <summary>
        /// Vu nay co dinh sau benh khong.
        ///
        /// Bam tu (hat giong cua van, id o, so thu tu vu) chu khong dung Random: chay lai cung mot
        /// van cho ra dung cung mot lich sau benh, va quang vang mat cung the.
        /// </summary>
        public static bool PestStrikes(long seed, int plotId, int cycleIndex, int chancePercent)
        {
            if (chancePercent <= 0) return false;
            if (chancePercent >= 100) return true;
            return (int)(Hash(seed, plotId, cycleIndex) % 100) < chancePercent;
        }

        /// <summary>Sau benh xuat hien o quang nao cua vu. Cung mot vu luon ra dung mot moc.</summary>
        public static long PestAppearsAtMs(long seed, int plotId, int cycleIndex, long startAtMs, long growthMs)
        {
            if (growthMs <= 1) return startAtMs;
            // Trong khoang 25%–75% cua vu: som qua thi nguoi choi chua kip nhin, muon qua thi
            // khong con cach nao cuu.
            long span = growthMs / 2;
            long offset = growthMs / 4 + (long)(Hash(seed + 977, plotId, cycleIndex) % (ulong)span);
            return startAtMs + offset;
        }

        /// <summary>
        /// Bam so nguyen 64 bit kieu splitmix. Viet tay trong Core de khong phu thuoc vao
        /// GetHashCode cua .NET — cai do khong hua giu nguyen giua cac ban chay.
        /// </summary>
        public static ulong Hash(long seed, int a, int b)
        {
            ulong x = (ulong)seed;
            x ^= (ulong)(uint)a * 0x9E3779B97F4A7C15UL;
            x ^= (ulong)(uint)b * 0xBF58476D1CE4E5B9UL;
            x += 0x9E3779B97F4A7C15UL;
            x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
            x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
            return x ^ (x >> 31);
        }

        public static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
