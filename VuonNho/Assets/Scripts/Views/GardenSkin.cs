using System;
using UnityEngine;

namespace VuonNho.Views
{
    [Serializable]
    public sealed class CropSkinEntry
    {
        [Tooltip("Id cay trong DefaultContent: crop_mint, crop_chamomile, crop_strawberry.")]
        public string CropId;

        [Tooltip("Model cay truong thanh. Pivot o giua chan, +Y len, scale 1.")]
        public GameObject MaturePrefab;
    }

    [Serializable]
    public sealed class DecorationSkinEntry
    {
        [Tooltip("Id trong DefaultDecorations: deco_planter, deco_bench, deco_lantern...")]
        public string DecorationId;

        [Tooltip("Model mon trang tri. Pivot o giua chan, +Y len, scale 1.")]
        public GameObject Prefab;
    }

    [Serializable]
    public sealed class StationSkinEntry
    {
        [Tooltip("Id công đoạn: stage_wither, stage_fix, stage_roll, stage_oxidise, stage_dry, stage_pack.")]
        public string StageId;

        [Tooltip("Model của máy. Pivot ở giữa chân, +Y lên, scale 1.")]
        public GameObject Prefab;
    }

    [Serializable]
    public sealed class IconSkinEntry
    {
        [Tooltip("Id icon: id cây (crop_mint...), id trang trí (deco_bench...), hoặc plot/seedling/helper/station.")]
        public string Id;

        [Tooltip("Ảnh vuông 256 × 256. Để trống thì HUD hiện chữ như cũ.")]
        public Sprite Icon;
    }

    /// <summary>
    /// Bo hinh anh cua khu vuon. SceneFactory dung prefab o day khi co, khong co thi tu rot ve
    /// primitive cua moc A. Chi chua art va kich thuoc, khong chua luat choi.
    ///
    /// Tao bang menu "Vuon Nho/0. Tao hoac mo GardenSkin", file nam o Assets/Settings/GardenSkin.asset.
    /// Doi prefab xong thi chay lai menu "2. Dung lai scene Garden".
    /// </summary>
    [CreateAssetMenu(menuName = "Vườn Nhỏ/Garden Skin", fileName = "GardenSkin")]
    public sealed class GardenSkin : ScriptableObject
    {
        // Bon id icon khong gan voi cay hay mon trang tri nao.
        public const string IconPlot = "plot";
        public const string IconSeedling = "seedling";
        public const string IconHelper = "helper";
        public const string IconStation = "station";

        [Header("Kích thước — quy chuẩn mục 3 tài liệu asset")]
        [Tooltip("Khoảng cách tâm hai ô liền nhau, mét.")]
        public float PlotSpacing = 1.6f;

        [Tooltip("Cạnh mặt đất của một ô, mét.")]
        public float PlotSize = 1.4f;

        [Tooltip("Cạnh nền cỏ lớn, mét.")]
        public float GroundSize = 34f;

        [Tooltip("Orthographic size của camera. Tăng nếu model mới làm vườn tràn khung.")]
        public float CameraOrthographicSize = 6.6f;

        [Header("Ô đất")]
        [Tooltip("Mặt đất của một ô. Để trống thì dùng khối primitive.")]
        public GameObject SoilPrefab;

        [Tooltip("Hiện đè lên ô chưa mở khóa. Để trống thì ô khóa được tô màu tối thay thế.")]
        public GameObject LockedOverlayPrefab;

        [Tooltip("Mầm chung cho cả ba loại cây.")]
        public GameObject SeedlingPrefab;

        [Tooltip("Dấu hiệu cây đã chín, nổi phía trên ô.")]
        public GameObject ReadyBadgePrefab;

        [Header("Cây trưởng thành — mỗi loại một model")]
        public CropSkinEntry[] Crops = new CropSkinEntry[0];

        [Header("Quán và robot")]
        [Tooltip("Cụm quán trà kèm máy pha. Đặt con tên StatusAnchor và SteamAnchor nếu muốn đèn trạng thái và hơi nước.")]
        public GameObject StationPrefab;

        [Tooltip("Robot nổi. Đặt con tên Face nếu muốn mặt robot đổi màu khi thức.")]
        public GameObject RobotPrefab;

        [Tooltip("Nhân vật chính. Đặt con tên LegLeft và LegRight nếu muốn có bước đi.")]
        public GameObject CharacterPrefab;

        [Tooltip("Thợ đứng máy. Cùng hợp đồng với nhân vật chính: con tên LegLeft và LegRight.")]
        public GameObject WorkerPrefab;

        [Header("Dây chuyền chế biến")]
        [Tooltip("Máy cho từng công đoạn. Để trống thì máy đó dựng bằng primitive.")]
        public StationSkinEntry[] Stations = new StationSkinEntry[0];

        [Header("Nền và props")]
        public GameObject GroundPrefab;
        public GameObject[] TreePrefabs = new GameObject[0];
        public GameObject[] BushPrefabs = new GameObject[0];
        public GameObject[] RockPrefabs = new GameObject[0];
        public GameObject FencePostPrefab;

        [Header("Chữ")]
        [Tooltip("Chữ chính của HUD. Để trống thì dùng font hệ thống.")]
        public Font BodyFont;

        [Tooltip("Chữ nhấn mạnh: tiêu đề, số xu, nhãn nút.")]
        public Font DisplayFont;

        [Tooltip("Font icon Material Symbols. Để trống thì nút chỉ có chữ.")]
        public Font SymbolFont;

        [Header("Trang trí — pha 2")]
        public DecorationSkinEntry[] Decorations = new DecorationSkinEntry[0];

        [Header("Icon HUD — để trống thì HUD chỉ có chữ")]
        public IconSkinEntry[] Icons = new IconSkinEntry[0];

        [Header("Âm thanh — để trống dùng cue tổng hợp sẵn")]
        public AudioClip ClickClip;
        public AudioClip PlantClip;
        public AudioClip HarvestClip;
        public AudioClip BrewClip;
        public AudioClip UpgradeClip;

        [Header("Ánh sáng")]
        public Color SkyColor = new Color(0.62f, 0.78f, 0.82f);
        public Color AmbientColor = new Color(0.45f, 0.48f, 0.46f);
        public float KeyLightIntensity = 1.15f;

        public GameObject MaturePrefabFor(string cropId)
        {
            if (Crops == null) return null;
            for (int i = 0; i < Crops.Length; i++)
                if (Crops[i] != null && string.Equals(Crops[i].CropId, cropId, StringComparison.Ordinal))
                    return Crops[i].MaturePrefab;
            return null;
        }

        /// <summary>Chon mot prefab trong mang theo chi so on dinh, de scene dung lai luon giong nhau.</summary>
        public GameObject DecorationPrefabFor(string decorationId)
        {
            if (Decorations == null) return null;
            for (int i = 0; i < Decorations.Length; i++)
                if (Decorations[i] != null &&
                    string.Equals(Decorations[i].DecorationId, decorationId, StringComparison.Ordinal))
                    return Decorations[i].Prefab;
            return null;
        }

        /// <summary>Chua nhap anh thi tra ve null — HUD phai chay duoc khi khong co icon nao.</summary>
        public GameObject StationPrefabFor(string stageId)
        {
            if (Stations == null || string.IsNullOrEmpty(stageId)) return null;
            for (int i = 0; i < Stations.Length; i++)
                if (Stations[i] != null && string.Equals(Stations[i].StageId, stageId, StringComparison.Ordinal))
                    return Stations[i].Prefab;
            return null;
        }

        public Sprite IconFor(string id)
        {
            if (Icons == null || string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < Icons.Length; i++)
                if (Icons[i] != null && string.Equals(Icons[i].Id, id, StringComparison.Ordinal))
                    return Icons[i].Icon;
            return null;
        }

        public static GameObject Pick(GameObject[] options, int index)
        {
            if (options == null || options.Length == 0) return null;
            return options[((index % options.Length) + options.Length) % options.Length];
        }

        void OnValidate()
        {
            if (PlotSpacing < 0.2f) PlotSpacing = 0.2f;
            if (PlotSize < 0.1f) PlotSize = 0.1f;
            if (PlotSize > PlotSpacing) PlotSize = PlotSpacing;
            if (GroundSize < 4f) GroundSize = 4f;
            if (CameraOrthographicSize < 1f) CameraOrthographicSize = 1f;
        }
    }
}
