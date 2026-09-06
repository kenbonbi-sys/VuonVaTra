using UnityEngine;

namespace VuonNho.Views
{
    /// <summary>
    /// Vong tron hien ra duoi dat o cho vua bam chuot phai, roi co lai va nhat dan.
    ///
    /// Khong co no thi mot cu bam hut — bam vao cho nhan vat dang dung, hay bam luc dang bi chan
    /// — nhin y het mot cu bam khong an: nguoi choi khong biet la game chua nghe thay hay la
    /// da nghe roi ma khong co gi de lam.
    ///
    /// Mesh dung ngay trong code, khong phai asset: mot hinh vanh khan nam ngang, 48 canh.
    /// Ban kinh viet lai moi khung hinh chu khong scale transform, de be day cua vanh giu nguyen
    /// trong luc vong tron co lai — vanh mong dan theo se nhin nhu net ve bi xoa.
    /// </summary>
    public sealed class ClickMarker : MonoBehaviour
    {
        [Tooltip("Vật liệu của vành. SceneFactory tạo sẵn; để trống thì tự dựng lúc chạy.")]
        public Material RingMaterial;

        [Tooltip("Bán kính lúc vừa hiện ra, tính bằng mét.")]
        public float StartRadius = 0.70f;

        [Tooltip("Bán kính lúc sắp tắt hẳn.")]
        public float EndRadius = 0.40f;

        [Tooltip("Bề dày của vành, tính bằng mét.")]
        public float Thickness = 0.09f;

        [Tooltip("Bao lâu thì tắt hẳn, tính bằng giây.")]
        public float Duration = 0.5f;

        [Tooltip("Nhấc khỏi mặt đất bấy nhiêu mét cho khỏi chớp với nền.")]
        public float GroundOffset = 0.02f;

        public Color Colour = new Color(0.99f, 0.97f, 0.88f, 1f);

        const int Segments = 48;

        Mesh _mesh;
        Vector3[] _vertices;
        MeshRenderer _renderer;
        Material _material;
        float _age = -1f;

        /// <summary>Vong tron dang hien. Bo kiem tra dung de biet cu bam co duoc bao lai hay khong.</summary>
        public bool IsShowing { get { return _age >= 0f; } }

        void Awake()
        {
            Build();
            Hide();
        }

        void Build()
        {
            _vertices = new Vector3[Segments * 2];
            var triangles = new int[Segments * 6];
            for (int i = 0; i < Segments; i++)
            {
                int next = (i + 1) % Segments;
                int t = i * 6;
                triangles[t] = i * 2;
                triangles[t + 1] = next * 2;
                triangles[t + 2] = i * 2 + 1;
                triangles[t + 3] = next * 2;
                triangles[t + 4] = next * 2 + 1;
                triangles[t + 5] = i * 2 + 1;
            }

            var colours = new Color[Segments * 2];
            for (int i = 0; i < colours.Length; i++) colours[i] = Color.white;

            _mesh = new Mesh { name = "ClickMarkerRing" };
            _mesh.MarkDynamic();
            WriteRing(StartRadius);
            _mesh.triangles = triangles;
            // Shader cua vanh nhan mau tu vertex colour; mesh khong co kenh mau thi mau ra khong
            // xac dinh tuy nen do hoa. Ghi trang het roi de _Color lo phan mau va do mo.
            _mesh.colors = colours;

            gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = gameObject.AddComponent<MeshRenderer>();
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

            // Vat lieu goc la asset do SceneFactory tao, nen shader chac chan nam trong ban build
            // — mot shader chi duoc tim bang Shader.Find thi co the bi loai khi dong goi. Ban sao
            // rieng cho tung instance vi mau cua no doi tung khung hinh.
            _material = RingMaterial != null
                ? new Material(RingMaterial)
                : new Material(Shader.Find("Sprites/Default"));
            _material.name = "ClickMarker";

            // Ve de len tren moi thu trong vuon. Vanh nam sat mat dat, ma luong cay cao 17 cm va
            // chiem gan het khu giua — noi nguoi choi bam nhieu nhat — nen neu de kiem tra do sau
            // thi mot cu bam vao luong cay khong duoc bao lai gi ca. Day la co y, khong phai
            // z-fighting.
            //
            // Dat o day chu khong o file material: unity_GUIZTestMode khong nam trong khoi
            // Properties cua shader nen khong duoc luu vao asset.
            _material.SetInt("unity_GUIZTestMode",
                             (int)UnityEngine.Rendering.CompareFunction.Always);
            _material.color = Colour;
            _renderer.sharedMaterial = _material;
        }

        void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
            if (_material != null) Destroy(_material);
        }

        /// <summary>Bao lai mot cu bam tai diem nay tren mat dat.</summary>
        public void Show(Vector3 groundPoint)
        {
            transform.position = new Vector3(groundPoint.x, groundPoint.y + GroundOffset, groundPoint.z);
            _age = 0f;
            if (_renderer != null) _renderer.enabled = true;
            Step(0f);
        }

        /// <summary>Tat ngay. Dung khi reset hoac khi bo kiem tra can mot diem xuat phat sach.</summary>
        public void Hide()
        {
            _age = -1f;
            if (_renderer != null) _renderer.enabled = false;
        }

        void Update()
        {
            if (_age < 0f) return;
            _age += Time.deltaTime;
            if (_age >= Duration) { Hide(); return; }
            Step(_age / Duration);
        }

        /// <summary>
        /// Giu nguyen co roi moi co va nhat o cuoi, chu khong co ngay tu dau. Ban dau lam nguoc
        /// lai — co nhanh roi cham dan — va ket qua la vong tron song gan het doi o trang thai
        /// nho va mo, tuc la nguoi choi thay ro no dung mot phan sau cua khoang thoi gian no ton tai.
        /// </summary>
        void Step(float t)
        {
            WriteRing(Mathf.Lerp(StartRadius, EndRadius, t * t));

            var colour = Colour;
            colour.a = Colour.a * (1f - t * t);
            _material.color = colour;
        }

        void WriteRing(float radius)
        {
            float inner = Mathf.Max(0.001f, radius - Thickness);
            for (int i = 0; i < Segments; i++)
            {
                float angle = i * (Mathf.PI * 2f / Segments);
                float x = Mathf.Cos(angle);
                float z = Mathf.Sin(angle);
                _vertices[i * 2] = new Vector3(x * inner, 0f, z * inner);
                _vertices[i * 2 + 1] = new Vector3(x * radius, 0f, z * radius);
            }
            _mesh.vertices = _vertices;
        }
    }
}
