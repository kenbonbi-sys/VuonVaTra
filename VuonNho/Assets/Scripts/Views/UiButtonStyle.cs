using UnityEngine;
using UnityEngine.UI;

namespace VuonNho.Views
{
    /// <summary>
    /// Giu san Image nen va Text nhan cua mot nut, va nho mau nut tro ve khi o trang thai
    /// binh thuong. Nho vay <see cref="UiFactory.SetButtonState"/> khong phai tim con moi lan
    /// cap nhat, va nut da tat co the lam mo ca chu chu khong chi lam toi nen.
    ///
    /// Lop rong khong logic; nam file rieng vi Unity can ten file trung ten MonoBehaviour.
    /// </summary>
    public sealed class UiButtonStyle : MonoBehaviour
    {
        public Image Background;
        public Text Caption;
        public Color BaseColor;
    }
}
