using System;

namespace VuonNho.Core
{
    /// <summary>
    /// Fixed production yard beside the existing garden. Millimetres keep the scene and
    /// decoration rules on the same coordinates without introducing a Unity dependency.
    /// Stations follow catalog stage order around a U, leaving the centre as a service aisle.
    /// </summary>
    public static class ProductionLayout
    {
        public const int StationCount = 6;
        public const int MinXMm = 3700;
        public const int MaxXMm = 13800;
        public const int MinZMm = -3000;
        public const int MaxZMm = 4400;

        static readonly int[] StationXs = { 5400, 8800, 12200, 12200, 8800, 5400 };
        static readonly int[] StationZs = { 3000, 3000, 3000, -1000, -1000, -1000 };

        public static int StationCenterXMm(int stageIndex)
        {
            ValidateIndex(stageIndex);
            return StationXs[stageIndex];
        }

        public static int StationCenterZMm(int stageIndex)
        {
            ValidateIndex(stageIndex);
            return StationZs[stageIndex];
        }

        static void ValidateIndex(int stageIndex)
        {
            if (stageIndex < 0 || stageIndex >= StationCount)
                throw new ArgumentOutOfRangeException(nameof(stageIndex));
        }
    }
}
