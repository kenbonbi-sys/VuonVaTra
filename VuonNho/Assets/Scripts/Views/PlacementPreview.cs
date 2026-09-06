using System.Collections.Generic;
using UnityEngine;

namespace VuonNho.Views
{
    /// <summary>
    /// Bong ma cua mon trang tri dang cam, bam theo con tro truoc khi dat that.
    ///
    /// Dung chinh prefab cua mon do chu khong phai mot hinh thay the: cau hoi nguoi choi dang
    /// hoi la "dat xuong day thi no trong the nao", va chi ban than mon do tra loi duoc.
    ///
    /// Cho khong dat duoc thi bong ma do len. Bao bang mau chu khong bang toast: toast noi mot
    /// lan roi tat, con mau thi tra loi lien tuc trong luc con tro con dang di tim cho.
    /// </summary>
    public sealed class PlacementPreview : MonoBehaviour
    {
        public GardenSkin Skin;

        [Tooltip("Màu phủ lên bóng ma khi chỗ đó không đặt được.")]
        public Color BlockedTint = new Color(0.86f, 0.34f, 0.30f);

        string _definitionId;
        GameObject _ghost;
        float _rotationDeg;
        bool _blocked;
        readonly List<Renderer> _renderers = new List<Renderer>();
        MaterialPropertyBlock _block;

        /// <summary>Goc xoay dang cam, do. Doc luc dat that de mon do nam dung huong da xoay.</summary>
        public float RotationDeg { get { return _rotationDeg; } }

        /// <summary>Bong ma dang hien. Bo kiem tra dung de biet no co that su bat len khong.</summary>
        public bool IsShowing { get { return _ghost != null && _ghost.activeSelf; } }

        /// <summary>Mon dang cam, null neu khong cam gi.</summary>
        public string DefinitionId { get { return _definitionId; } }

        /// <summary>Chon mon moi. Goc xoay ve 0 de moi lan chon lai deu bat dau giong nhau.</summary>
        public void Begin(string definitionId)
        {
            if (_definitionId != definitionId) Clear();
            _definitionId = definitionId;
            _rotationDeg = 0f;
        }

        public void Cancel()
        {
            _definitionId = null;
            Clear();
        }

        /// <summary>Xoay 180 do — doi mat truoc ra sau, thao tac hay dung nhat nen co phim rieng.</summary>
        public void Flip()
        {
            RotateBy(180f);
        }

        public void RotateBy(float degrees)
        {
            _rotationDeg = Mathf.Repeat(_rotationDeg + degrees, 360f);
            if (_ghost != null) _ghost.transform.localRotation = Quaternion.Euler(0f, _rotationDeg, 0f);
        }

        /// <summary>Dua bong ma toi mot cho tren mat dat.</summary>
        public void ShowAt(Vector3 groundPoint, bool canPlace)
        {
            if (_definitionId == null) return;
            EnsureGhost();
            if (_ghost == null) return;

            _ghost.SetActive(true);
            _ghost.transform.position = new Vector3(groundPoint.x, 0f, groundPoint.z);
            _ghost.transform.localRotation = Quaternion.Euler(0f, _rotationDeg, 0f);
            Tint(!canPlace);
        }

        /// <summary>Tam an — con tro dang o tren HUD hoac ngoai vuon.</summary>
        public void HideGhost()
        {
            if (_ghost != null) _ghost.SetActive(false);
        }

        void EnsureGhost()
        {
            if (_ghost != null) return;
            var prefab = Skin != null ? Skin.DecorationPrefabFor(_definitionId) : null;
            if (prefab == null) return;

            _ghost = Instantiate(prefab, transform);
            _ghost.name = "Ghost_" + _definitionId;

            // Bong ma khong duoc chan tia cua chinh no: con tro can cham toi mat dat ben duoi de
            // biet dat vao dau, ma bong ma thi luon nam ngay duoi con tro.
            var colliders = _ghost.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;

            _renderers.Clear();
            _ghost.GetComponentsInChildren(true, _renderers);
            _block = new MaterialPropertyBlock();
            _blocked = false;
        }

        void Tint(bool blocked)
        {
            if (_blocked == blocked && _block != null) return;
            _blocked = blocked;
            for (int i = 0; i < _renderers.Count; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null) continue;
                renderer.GetPropertyBlock(_block);
                if (blocked)
                {
                    // Ca hai ten: URP doc _BaseColor, shader cu doc _Color.
                    _block.SetColor("_BaseColor", BlockedTint);
                    _block.SetColor("_Color", BlockedTint);
                }
                else
                {
                    // Xoa de tra ve mau that cua material — do moi la cau tra loi cho "trong the nao".
                    _block.Clear();
                }
                renderer.SetPropertyBlock(_block);
            }
        }

        void Clear()
        {
            if (_ghost != null) Destroy(_ghost);
            _ghost = null;
            _renderers.Clear();
        }
    }
}
