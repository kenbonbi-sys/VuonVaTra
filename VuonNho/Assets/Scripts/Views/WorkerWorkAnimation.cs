using UnityEngine;

namespace VuonNho.Views
{
    /// <summary>
    /// Presentation-only hand work. Shoulder pivots live in the worker art, so hiring,
    /// waiting for ingredients and finishing a batch never rebuild the character.
    /// </summary>
    public sealed class WorkerWorkAnimation : MonoBehaviour
    {
        [Tooltip("Shoulder pivots. Found by name beneath the worker when left empty.")]
        public Transform ArmLeft;
        public Transform ArmRight;
        [Tooltip("Visual facing frame, if a walker rotates the model inside the worker root.")]
        public Transform FacingRoot;

        const float BlendPerSecond = 3.5f;
        const float CyclesPerSecond = 1.25f;
        const float ReachDegrees = 62f;
        const float StrokeDegrees = 13f;

        Transform _cachedLeft;
        Transform _cachedRight;
        Quaternion _leftRest;
        Quaternion _rightRest;
        int _searchedChildCount = -1;
        bool _working;
        float _blend;
        float _phase;

        public bool IsWorking { get { return _working; } }

        void Awake()
        {
            _phase = Mathf.Repeat(transform.position.x * 1.31f + transform.position.z * .73f,
                                  Mathf.PI * 2f);
            CacheArms();
        }

        // An di roi hien lai thi tay phai bat dau tu tu the nghi, khong phai tu giua nhip dang do.
        void OnEnable() { SnapToRest(); }

        void OnDisable() { SnapToRest(); }

        /// <summary>
        /// Ve ngay tu the nghi va bo nhip dang do.
        ///
        /// La mot ham cong khai chu khong chi nam trong OnEnable/OnDisable: Edit Mode khong gui
        /// thong diep vong doi nao cho component nay, nen bo kiem tra phai goi duoc dung viec ma
        /// hai thong diep do lam.
        /// </summary>
        public void SnapToRest()
        {
            CacheArms();
            _blend = 0f;
            RestoreArms();
        }

        public void SetWorking(bool working)
        {
            CacheArms();
            _working = working;
        }

        void Update() { Animate(Time.deltaTime); }

        void Animate(float deltaTime)
        {
            CacheArms();
            deltaTime = Mathf.Max(0f, deltaTime);
            _blend = Mathf.MoveTowards(_blend, _working ? 1f : 0f, deltaTime * BlendPerSecond);
            if (_blend <= 0f)
            {
                RestoreArms();
                return;
            }

            _phase = Mathf.Repeat(_phase + deltaTime * CyclesPerSecond * Mathf.PI * 2f,
                                  Mathf.PI * 2f);
            float stroke = Mathf.Sin(_phase) * StrokeDegrees;
            float blend = Mathf.SmoothStep(0f, 1f, _blend);
            PoseArm(ArmLeft, _leftRest, ReachDegrees + stroke, blend);
            PoseArm(ArmRight, _rightRest, ReachDegrees - stroke, blend);
        }

        void PoseArm(Transform arm, Quaternion rest, float reach, float blend)
        {
            if (arm == null) return;
            // Convert the worker's right axis into the FBX parent's space. This preserves
            // the imported rest rotation and also works when the worker faces another bay.
            Vector3 right = FacingRoot != null ? FacingRoot.right : transform.right;
            Vector3 axis = arm.parent != null
                ? arm.parent.InverseTransformDirection(right).normalized
                : right;
            arm.localRotation = Quaternion.AngleAxis(-reach * blend, axis) * rest;
        }

        void CacheArms()
        {
            // Tim lai khi con thieu tay VA so con da doi. Model cua tho co the duoc gan vao sau
            // khi component da duoc them, ma OnTransformChildrenChanged thi khong chay trong Edit
            // Mode. So con lam moc de mot model that su khong co tay khong bi quet lai moi khung hinh.
            if ((ArmLeft == null || ArmRight == null) && transform.childCount != _searchedChildCount)
            {
                _searchedChildCount = transform.childCount;
                var children = GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < children.Length; i++)
                {
                    if (ArmLeft == null && children[i].name == "ArmLeft") ArmLeft = children[i];
                    if (ArmRight == null && children[i].name == "ArmRight") ArmRight = children[i];
                }
            }
            if (ArmLeft != null && ArmLeft != _cachedLeft)
            {
                _cachedLeft = ArmLeft;
                _leftRest = ArmLeft.localRotation;
            }
            if (ArmRight != null && ArmRight != _cachedRight)
            {
                _cachedRight = ArmRight;
                _rightRest = ArmRight.localRotation;
            }
        }

        void RestoreArms()
        {
            if (_cachedLeft != null) _cachedLeft.localRotation = _leftRest;
            if (_cachedRight != null) _cachedRight.localRotation = _rightRest;
        }

    }
}
