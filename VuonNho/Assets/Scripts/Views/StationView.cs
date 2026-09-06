using UnityEngine;
using VuonNho.Core;

namespace VuonNho.Views
{
    /// <summary>
    /// Mot cai may trong day chuyen. Chua mua thi khong hien gi ca — mot cai may mo o cho san
    /// se lam nguoi choi tuong no da hong chu khong phai chua co.
    ///
    /// WorkshopCrewView keeps hired workers present between batches and routes them to jobs.
    /// </summary>
    public sealed class StationView : MonoBehaviour
    {
        public string StageId;

        [Tooltip("Cụm model của máy. Tắt cả cụm khi chưa mua.")]
        public Transform VisualRoot;

        [Tooltip("Nhân vật thợ trong nhóm WorkshopCrewView. Không tắt theo từng mẻ máy.")]
        public Transform WorkerRoot;

        [Tooltip("Đèn báo máy đang chạy. Để trống thì bỏ qua.")]
        public Renderer StatusRenderer;

        [Tooltip("Máy nhún nhẹ khi đang chạy, mét.")]
        public float RunningBob = 0.02f;

        [Tooltip("Số nhịp nhún mỗi giây.")]
        public float BobPerSecond = 2.2f;

        Vector3 _visualRest;
        bool _cachedRest;
        MaterialPropertyBlock _block;
        bool _lastRunning;
        bool _lastOwned = true;
        Collider[] _colliders;

        void Awake()
        {
            CacheRest();
            SetOwned(false);
        }

        void CacheRest()
        {
            if (_cachedRest || VisualRoot == null) return;
            _visualRest = VisualRoot.localPosition;
            _cachedRest = true;
        }

        public void Render(GameState state)
        {
            if (state == null) return;
            var station = state.Station(StageId);
            if (station == null) { SetOwned(false); return; }

            SetOwned(station.Owned);
            if (!station.Owned) return;

            CacheRest();

            if (VisualRoot != null)
            {
                // Nhun theo thoi gian that chu khong theo thoi gian mo phong: day la hieu ung
                // nhin, khong phai nguon timer cua kinh te.
                float lift = station.Running
                    ? Mathf.Abs(Mathf.Sin(Time.time * BobPerSecond * Mathf.PI)) * RunningBob
                    : 0f;
                VisualRoot.localPosition = _visualRest + new Vector3(0f, lift, 0f);
            }

            if (StatusRenderer != null && _lastRunning != station.Running)
            {
                _lastRunning = station.Running;
                if (_block == null) _block = new MaterialPropertyBlock();
                var colour = station.Running ? GardenPalette.StateOk : GardenPalette.TextMuted;
                StatusRenderer.GetPropertyBlock(_block);
                _block.SetColor("_BaseColor", colour);
                _block.SetColor("_Color", colour);
                StatusRenderer.SetPropertyBlock(_block);
            }
        }

        void SetOwned(bool owned)
        {
            if (_lastOwned == owned) return;
            _lastOwned = owned;
            // An unbuilt bay is walkable; its hidden machine must not intercept clicks.
            if (_colliders == null) _colliders = GetComponents<Collider>();
            for (int i = 0; i < _colliders.Length; i++) _colliders[i].enabled = owned;
            if (VisualRoot != null) VisualRoot.gameObject.SetActive(owned);
        }
    }
}
