using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VuonNho.Views
{
    /// <summary>
    /// Phan hoi mem cho nut noi: lon nhe khi tro vao, lun xuong khi nhan.
    /// Chi doi scale, giu nguyen vi tri va cac rang buoc layout cua HUD.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class FarmButtonMotion : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
        IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        /// <summary>De trong de animate ca nut; gan mot con neu muon giu hitbox co dinh.</summary>
        public RectTransform Visual;

        [Range(1f, 1.12f)] public float HoverScale = 1.04f;
        [Range(0.85f, 1f)] public float PressScale = 0.94f;
        [Range(0.04f, 0.3f)] public float ResponseTime = 0.085f;

        Button _button;
        bool _hovered;
        bool _pressed;
        bool _selected;
        float _scale = 1f;
        float _velocity;

        Transform VisualTransform { get { return Visual != null ? Visual : transform; } }

        bool CanInteract
        {
            get { return _button != null && _button.IsActive() && _button.IsInteractable(); }
        }

        void Awake()
        {
            _button = GetComponent<Button>();
        }

        void Update()
        {
            float target = 1f;
            if (CanInteract)
            {
                // Keo ra khoi nut trong khi van giu chuot thi tra ve kich thuoc thuong.
                if (_pressed) target = _hovered ? PressScale : 1f;
                else if (_hovered || _selected) target = HoverScale;
            }
            else
            {
                _hovered = false;
                _pressed = false;
                _selected = false;
            }

            _scale = Mathf.SmoothDamp(_scale, target, ref _velocity,
                                     Mathf.Max(0.01f, ResponseTime), Mathf.Infinity,
                                     Time.unscaledDeltaTime);
            if (Mathf.Abs(_scale - target) < 0.0001f && Mathf.Abs(_velocity) < 0.001f)
            {
                _scale = target;
                _velocity = 0f;
            }
            VisualTransform.localScale = new Vector3(_scale, _scale, 1f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (CanInteract) _hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanInteract || eventData.button != PointerEventData.InputButton.Left) return;
            _pressed = true;
            _hovered = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) _pressed = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (CanInteract) _selected = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _selected = false;
            _pressed = false;
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) ResetMotion();
        }

        void OnDisable()
        {
            ResetMotion();
        }

        void ResetMotion()
        {
            _hovered = false;
            _pressed = false;
            _selected = false;
            _scale = 1f;
            _velocity = 0f;
            VisualTransform.localScale = Vector3.one;
        }
    }
}
