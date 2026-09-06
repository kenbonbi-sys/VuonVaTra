using UnityEngine;

namespace VuonNho.Views
{
    /// <summary>
    /// Danh dau mot collider ma nhan vat khong di xuyen qua duoc.
    ///
    /// Phai la mot thanh phan rieng chu khong phai "moi collider deu chan": o dat, nen vuon va
    /// quay tra deu co collider gameplay de nhan click, ma di len o dat thi phai duoc. Danh dau
    /// tung mon cung la cach duy nhat de loi di lat da vua co collider vua khong chan duong.
    ///
    /// Nam ngay tren cung vat the voi collider, khong phai tren cha: <see cref="CharacterView"/>
    /// doc no bang GetComponent cho moi collider tim thay quanh minh, moi khung hinh.
    /// </summary>
    public sealed class WalkBlocker : MonoBehaviour
    {
    }
}
