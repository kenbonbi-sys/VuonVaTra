using UnityEngine;
using UnityEngine.EventSystems;

namespace VuonNho.Views
{
    /// <summary>
    /// Lan chuot de phong to thu nho, giu chuot trai de keo man hinh.
    ///
    /// Camera la orthographic va nhin xuong mot goc co dinh, nen moi phep tinh deu quy ve mot
    /// diem tren mat dat: cho no giu nguyen duoi con tro thi ca keo lan zoom deu tu nhien.
    ///
    /// Rig con la noi quyet dinh mot lan nhan chuot trai la BAM hay KEO. Vi the
    /// <see cref="GameBootstrap"/> phai xu ly thao tac luc tha chuot chu khong phai luc nhan:
    /// luc nhan thi chua biet nguoi choi dinh lam gi.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public Camera Camera;

        [Header("Phóng to thu nhỏ")]
        [Tooltip("Orthographic size nhỏ nhất — nhìn gần nhất.")]
        public float MinSize = 3.2f;

        [Tooltip("Orthographic size lớn nhất — nhìn xa nhất.")]
        public float MaxSize = 11f;

        [Tooltip("Mỗi nấc lăn chuột nhân kích thước với số này.")]
        public float ZoomPerNotch = 0.88f;

        [Header("Kéo màn hình")]
        [Tooltip("Camera không rời quá xa tâm vườn, tính bằng mét.")]
        public float PanLimit = 9f;

        [Tooltip("Kéo quá bao nhiêu pixel thì tính là kéo chứ không phải bấm.")]
        public float DragThresholdPixels = 6f;

        /// <summary>Lan nhan vua roi la keo man hinh, khong phai bam vao vuon.</summary>
        public bool ClickWasDrag { get; private set; }

        Vector3 _homePosition;
        float _homeSize;
        Vector3 _grabbedGroundPoint;
        Vector3 _pressScreenPosition;
        bool _pressStartedOnWorld;
        bool _dragging;

        void Awake()
        {
            if (Camera == null) Camera = Camera.main;
            if (Camera == null) return;
            _homePosition = Camera.transform.position;
            _homeSize = Camera.orthographicSize;
        }

        void Update()
        {
            if (Camera == null) return;
            HandleZoom();
            HandleDrag();
        }

        /// <summary>Ve lai goc nhin ban dau. Dung cho nut "ve giua vuon" hoac khi reset.</summary>
        public void ResetView()
        {
            if (Camera == null) return;
            Camera.transform.position = _homePosition;
            Camera.orthographicSize = _homeSize;
        }

        /// <summary>Jump between the two work areas without changing the orthographic angle.</summary>
        public void FocusGround(Vector3 centre, float size)
        {
            if (Camera == null) return;
            centre.y = 0f;
            float distance = Camera.transform.position.y / -Camera.transform.forward.y;
            Camera.transform.position = centre - Camera.transform.forward * distance;
            Camera.orthographicSize = Mathf.Clamp(size, MinSize, MaxSize);
        }

        // ---------------------------------------------------------------- zoom

        void HandleZoom()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(scroll, 0f)) return;
            // Lan chuot tren panel la cuon panel, khong phai zoom vuon.
            if (PointerOverUi()) return;
            ZoomBy(scroll, Input.mousePosition);
        }

        /// <summary>
        /// Phong to thu nho quanh mot diem tren man hinh. Tach khoi <see cref="Update"/> de bo
        /// kiem tra trong build goi duoc dung doan ma nay chu khong phai mot ban chep.
        /// </summary>
        public void ZoomBy(float notches, Vector3 screenAnchor)
        {
            if (Camera == null) return;
            Vector3 before;
            bool hasAnchor = TryGroundPoint(screenAnchor, out before);

            Camera.orthographicSize = Mathf.Clamp(
                Camera.orthographicSize * Mathf.Pow(ZoomPerNotch, notches), MinSize, MaxSize);

            // Giu nguyen diem dang nam duoi con tro: zoom huong vao cho dang nhin, khong nhay ve giua.
            Vector3 after;
            if (hasAnchor && TryGroundPoint(screenAnchor, out after))
                PanBy(before - after);
        }

        // ---------------------------------------------------------------- keo

        void HandleDrag()
        {
            if (Input.GetMouseButtonDown(0))
            {
                ClickWasDrag = false;
                _dragging = false;
                _pressScreenPosition = Input.mousePosition;
                _pressStartedOnWorld = !PointerOverUi() &&
                                       TryGroundPoint(Input.mousePosition, out _grabbedGroundPoint);
                return;
            }

            if (Input.GetMouseButton(0) && _pressStartedOnWorld)
            {
                if (!_dragging)
                {
                    float moved = Vector2.Distance(Input.mousePosition, _pressScreenPosition);
                    if (moved < DragThresholdPixels) return;
                    _dragging = true;
                    ClickWasDrag = true;
                }

                Vector3 current;
                if (TryGroundPoint(Input.mousePosition, out current))
                    PanBy(_grabbedGroundPoint - current);
                return;
            }

            if (Input.GetMouseButtonUp(0)) _dragging = false;
        }

        // ---------------------------------------------------------------- tien ich

        /// <summary>Doi cho camera nhin, tinh bang met tren mat dat.</summary>
        public void PanBy(Vector3 groundDelta)
        {
            groundDelta.y = 0f;
            var next = Camera.transform.position + groundDelta;

            // Chan khong cho troi ra khoi khu vuon. Gioi han tinh tren diem camera dang nhin toi,
            // khong phai tren vi tri camera, vi camera nam lui rat xa phia sau.
            Vector3 focus;
            if (TryGroundPointFrom(next, out focus))
            {
                var clamped = new Vector3(Mathf.Clamp(focus.x, -PanLimit, PanLimit), 0f,
                                          Mathf.Clamp(focus.z, -PanLimit, PanLimit));
                // Day camera lui lai dung bang phan bi cat, nen no dung ngay o mep gioi han.
                next += clamped - focus;
            }
            Camera.transform.position = next;
        }

        /// <summary>Diem giua man hinh dang nham toi. Dung de do gioi han keo.</summary>
        public bool TryFocusPoint(out Vector3 point)
        {
            if (Camera == null) { point = Vector3.zero; return false; }
            return TryGroundPointFrom(Camera.transform.position, out point);
        }

        static bool PointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        /// <summary>Diem tren mat dat (y = 0) ma tia tu con tro cham toi.</summary>
        bool TryGroundPoint(Vector3 screenPosition, out Vector3 point)
        {
            return RayToGround(Camera.ScreenPointToRay(screenPosition), out point);
        }

        /// <summary>Diem giua man hinh se nhin vao neu camera dung o vi tri nay.</summary>
        bool TryGroundPointFrom(Vector3 cameraPosition, out Vector3 point)
        {
            var ray = new Ray(cameraPosition, Camera.transform.forward);
            return RayToGround(ray, out point);
        }

        static bool RayToGround(Ray ray, out Vector3 point)
        {
            point = Vector3.zero;
            if (Mathf.Approximately(ray.direction.y, 0f)) return false;
            float distance = -ray.origin.y / ray.direction.y;
            if (distance <= 0f) return false;
            point = ray.GetPoint(distance);
            return true;
        }
    }
}
