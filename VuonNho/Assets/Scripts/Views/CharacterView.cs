using UnityEngine;

namespace VuonNho.Views
{
    /// <summary>
    /// Nhan vat chinh: bam chuot phai len dat thi di toi do.
    ///
    /// Di thang, roi truot doc theo do dac neu dam vao — khong tim duong. Vuon la mot khoang
    /// trong voi vai mon do dat roi rac, khong phai me cung: truot doc mot canh la du de di vong
    /// qua chung, con mot thuat toan tim duong thi them nhieu ma nguon ma gan nhu khong bao gio
    /// cho ra duong di khac.
    ///
    /// Cai gia phai tra: dung giua hai mon do ke sat nhau thanh mot goc lom thi nhan vat dung
    /// lai chu khong lui ra de vong. Bam mot cu nua la di tiep duoc.
    ///
    /// Khong co xuong va khong co animation clip: hai chan la hai nhom mesh rieng, goc xoay
    /// nam ngay hong, nen chi can xoay transform la co buoc di. Cung cach robot dang lam.
    /// </summary>
    public sealed class CharacterView : MonoBehaviour
    {
        [Tooltip("Cụm mesh xoay và nhún theo bước đi. Để trống thì xoay cả object.")]
        public Transform VisualRoot;

        [Tooltip("Nhóm chân trái và chân phải, gốc xoay đặt ở hông.")]
        public Transform LegLeft;
        public Transform LegRight;

        [Tooltip("Mét mỗi giây.")]
        public float Speed = 2.8f;

        [Tooltip("Độ mỗi giây khi quay người.")]
        public float TurnSpeed = 900f;

        [Tooltip("Nhân vật không đi ra ngoài ô vuông này quanh tâm vườn, tính bằng mét.")]
        public float WalkLimit = 8f;

        [Tooltip("Bán kính thân người, dùng để tránh đồ đã đặt trong vườn. Mét.")]
        public float BodyRadius = 0.3f;

        const float ArrivalDistance = 0.06f;
        const float StepDegrees = 32f;
        const float StepsPerMetre = 1.15f;
        const float BobAmplitude = 0.035f;
        const float StridePerSecond = 6f;

        readonly Collider[] _nearby = new Collider[16];

        Vector3 _target;
        bool _walking;
        float _stepPhase;
        float _stride;
        Vector3 _restLocalPosition;
        Transform _cachedVisual;

        /// <summary>Dang tren duong di.</summary>
        public bool IsWalking { get { return _walking; } }

        /// <summary>Diem dang huong toi. Chi co nghia khi <see cref="IsWalking"/> dung.</summary>
        public Vector3 Destination { get { return _target; } }

        void Awake()
        {
            CacheRest();
            _target = transform.position;
        }

        void CacheRest()
        {
            if (VisualRoot == null || _cachedVisual == VisualRoot) return;
            _cachedVisual = VisualRoot;
            _restLocalPosition = VisualRoot.localPosition;
        }

        /// <summary>Dat nhan vat ve mot cho va huy lenh dang chay. Dung cho bo kiem tra.</summary>
        public void Teleport(Vector3 worldPoint)
        {
            transform.position = new Vector3(worldPoint.x, transform.position.y, worldPoint.z);
            _target = transform.position;
            _walking = false;
        }

        /// <summary>
        /// Nhan lenh di. Diem ngoai vuon bi keo ve trong bo, de mot cu bam hut van dan den
        /// mot buoc di co nghia thay vi khong co gi xay ra.
        /// </summary>
        public void WalkTo(Vector3 worldPoint)
        {
            _target = new Vector3(Mathf.Clamp(worldPoint.x, -WalkLimit, WalkLimit),
                                  transform.position.y,
                                  Mathf.Clamp(worldPoint.z, -WalkLimit, WalkLimit));
            _walking = Horizontal(_target - transform.position).magnitude > ArrivalDistance;
        }

        void Update()
        {
            CacheRest();

            float stepped = 0f;
            if (_walking)
            {
                var toTarget = Horizontal(_target - transform.position);
                float distance = toTarget.magnitude;
                if (distance <= ArrivalDistance)
                {
                    _walking = false;
                }
                else
                {
                    var direction = toTarget / distance;
                    stepped = StepWithSlide(direction, Mathf.Min(Speed * Time.deltaTime, distance));

                    // Di khong noi nua thi dung han, khong day mai vao mot mon do: dung im la
                    // loi bao "toi khong den duoc do", con rung tai cho thi khong noi gi ca.
                    if (stepped <= 0f) _walking = false;

                    // Quay nguoi tach khoi buoc di: doi huong dot ngot van muot chu khong giat.
                    var body = VisualRoot != null ? VisualRoot : transform;
                    body.rotation = Quaternion.RotateTowards(
                        body.rotation, Quaternion.LookRotation(direction, Vector3.up),
                        TurnSpeed * Time.deltaTime);
                }
            }

            AnimateStride(stepped);
        }

        /// <summary>
        /// Nhip chan tinh theo quang duong da di chu khong theo thoi gian, nen chan luon cham
        /// dat dung nhip du toc do co doi. Luc dung lai thi ha bien do ve 0 chu khong quay pha
        /// nguoc ve 0 — quay pha se thanh mot cu da chan ve phia sau.
        /// </summary>
        void AnimateStride(float stepped)
        {
            if (stepped > 0f)
            {
                _stepPhase += stepped * StepsPerMetre * 2f * Mathf.PI;
                _stride = Mathf.MoveTowards(_stride, 1f, Time.deltaTime * StridePerSecond);
            }
            else
            {
                _stride = Mathf.MoveTowards(_stride, 0f, Time.deltaTime * StridePerSecond);
                if (_stride <= 0f) _stepPhase = 0f;
            }

            float swing = Mathf.Sin(_stepPhase) * StepDegrees * _stride;
            if (LegLeft != null) LegLeft.localRotation = Quaternion.Euler(swing, 0f, 0f);
            if (LegRight != null) LegRight.localRotation = Quaternion.Euler(-swing, 0f, 0f);

            // Nguoi nhun hai lan moi chu ky chan, nen lay tri tuyet doi cua sin.
            if (VisualRoot != null)
                VisualRoot.localPosition = _restLocalPosition +
                    new Vector3(0f, Mathf.Abs(Mathf.Sin(_stepPhase)) * BobAmplitude * _stride, 0f);
        }

        /// <summary>
        /// Di mot buoc theo huong da cho. Dam vao do dac thi bo mot truc va di not truc kia —
        /// do trang tri deu la hop vuong goc voi truc nen bo mot thanh phan la du de truot doc
        /// theo canh, khong can tinh phap tuyen va cung khong the truot vong ra sau vat can.
        /// </summary>
        /// <returns>Quang duong that su di duoc; 0 nghia la bi chan hoan toan.</returns>
        float StepWithSlide(Vector3 direction, float distance)
        {
            var from = transform.position;

            var full = from + direction * distance;
            if (!Blocked(full)) { transform.position = full; return distance; }

            var alongX = from + new Vector3(direction.x * distance, 0f, 0f);
            if (Mathf.Abs(direction.x) > 0.01f && !Blocked(alongX))
            {
                transform.position = alongX;
                return Mathf.Abs(direction.x) * distance;
            }

            var alongZ = from + new Vector3(0f, 0f, direction.z * distance);
            if (Mathf.Abs(direction.z) > 0.01f && !Blocked(alongZ))
            {
                transform.position = alongZ;
                return Mathf.Abs(direction.z) * distance;
            }

            return 0f;
        }

        /// <summary>
        /// Cho nay co mon do nao chan khong. Chi do trang tri chan: o dat va nen vuon cung co
        /// collider, ma di len o dat thi phai duoc.
        /// </summary>
        bool Blocked(Vector3 position)
        {
            var centre = position + Vector3.up * BodyRadius;
            int count = Physics.OverlapSphereNonAlloc(centre, BodyRadius, _nearby,
                                                      ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                // Collider gameplay nam ngay tren wrapper cua mon trang tri, cung cho voi handle.
                var handle = _nearby[i].GetComponent<DecorationHandle>();
                if (handle != null && handle.BlocksWalking) return true;
            }
            return false;
        }

        static Vector3 Horizontal(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
