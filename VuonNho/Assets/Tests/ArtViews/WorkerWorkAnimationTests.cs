using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VuonNho.Views;

namespace VuonNho.Tests.ArtViews
{
    public sealed class WorkerWorkAnimationTests
    {
        static readonly MethodInfo Animate = typeof(WorkerWorkAnimation).GetMethod("Animate",
            BindingFlags.Instance | BindingFlags.NonPublic);

        GameObject _worker;

        [TearDown]
        public void TearDown()
        {
            if (_worker != null) Object.DestroyImmediate(_worker);
        }

        WorkerWorkAnimation CreateWorker()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prefabs/PF_Worker.prefab");
            Assert.IsNotNull(prefab, "The actual worker prefab is needed to verify FBX pivot conversion.");
            _worker = Object.Instantiate(prefab);
            var animation = _worker.AddComponent<WorkerWorkAnimation>();
            animation.SetWorking(false);
            return animation;
        }

        static void Step(WorkerWorkAnimation animation, float seconds)
        {
            Animate.Invoke(animation, new object[] { seconds });
        }

        static Vector3 LowestBoundPoint(Transform arm, Transform worker)
        {
            // Bounds stay accessible when production FBX meshes have Read/Write disabled.
            var bounds = arm.GetComponent<MeshFilter>().sharedMesh.bounds;
            var lowest = bounds.min;
            float height = float.PositiveInfinity;
            for (int i = 0; i < 8; i++)
            {
                var point = new Vector3((i & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (i & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (i & 4) == 0 ? bounds.min.z : bounds.max.z);
                float y = worker.InverseTransformPoint(arm.TransformPoint(point)).y;
                if (y < height) { height = y; lowest = point; }
            }
            return lowest;
        }

        [Test]
        public void ImportedWorkerHasIndependentArmsPivotedAtTheShoulders()
        {
            var animation = CreateWorker();
            Assert.IsNotNull(animation.ArmLeft);
            Assert.IsNotNull(animation.ArmRight);
            foreach (var arm in new[] { animation.ArmLeft, animation.ArmRight })
            {
                Assert.IsNotNull(arm.GetComponent<MeshFilter>());
                var shoulder = _worker.transform.InverseTransformPoint(arm.position);
                Assert.That(shoulder.y, Is.InRange(.84f, .92f));
                Assert.That(Mathf.Abs(shoulder.x), Is.InRange(.22f, .27f));
                Assert.That(arm.GetComponent<Renderer>().bounds.size.y, Is.GreaterThan(.45f));
            }
        }

        [Test]
        public void BothHandsReachForwardWithoutMovingTheWorkerOrShoulders()
        {
            var animation = CreateWorker();
            _worker.transform.SetPositionAndRotation(new Vector3(3f, 0f, -2f), Quaternion.Euler(0f, 137f, 0f));
            var rootPosition = _worker.transform.position;
            var rootRotation = _worker.transform.rotation;
            var leftShoulder = animation.ArmLeft.position;
            var rightShoulder = animation.ArmRight.position;
            var leftWrist = LowestBoundPoint(animation.ArmLeft, _worker.transform);
            var rightWrist = LowestBoundPoint(animation.ArmRight, _worker.transform);
            var leftBefore = animation.ArmLeft.TransformPoint(leftWrist);
            var rightBefore = animation.ArmRight.TransformPoint(rightWrist);

            animation.SetWorking(true);
            Step(animation, 1f);

            Assert.That(Vector3.Dot(animation.ArmLeft.TransformPoint(leftWrist) - leftBefore,
                _worker.transform.forward), Is.GreaterThan(.2f));
            Assert.That(Vector3.Dot(animation.ArmRight.TransformPoint(rightWrist) - rightBefore,
                _worker.transform.forward), Is.GreaterThan(.2f));
            Assert.That(Vector3.Distance(leftShoulder, animation.ArmLeft.position), Is.LessThan(.00001f));
            Assert.That(Vector3.Distance(rightShoulder, animation.ArmRight.position), Is.LessThan(.00001f));
            Assert.AreEqual(rootPosition, _worker.transform.position);
            Assert.AreEqual(rootRotation, _worker.transform.rotation);
            var leftPose = animation.ArmLeft.localRotation;
            var rightPose = animation.ArmRight.localRotation;
            Step(animation, .2f);
            Assert.That(Quaternion.Angle(leftPose, animation.ArmLeft.localRotation), Is.GreaterThan(5f));
            Assert.That(Quaternion.Angle(rightPose, animation.ArmRight.localRotation), Is.GreaterThan(5f));
        }

        [Test]
        public void WaitingBlendsBackToAuthoredRestWithoutHidingTheWorker()
        {
            var animation = CreateWorker();
            var leftRest = animation.ArmLeft.localRotation;
            var rightRest = animation.ArmRight.localRotation;
            animation.SetWorking(true);
            Step(animation, 1f);
            var workingPose = animation.ArmLeft.localRotation;

            animation.SetWorking(false);
            Assert.AreEqual(workingPose, animation.ArmLeft.localRotation, "Finishing must not snap the hands.");
            Step(animation, 1f / 60f);
            Assert.That(Quaternion.Angle(leftRest, animation.ArmLeft.localRotation), Is.GreaterThan(10f));
            for (int i = 0; i < 60; i++) Step(animation, 1f / 60f);

            Assert.AreEqual(leftRest, animation.ArmLeft.localRotation);
            Assert.AreEqual(rightRest, animation.ArmRight.localRotation);
            Assert.IsTrue(_worker.activeSelf);
            Assert.IsFalse(animation.IsWorking);
            Step(animation, 10f);
            Assert.AreEqual(leftRest, animation.ArmLeft.localRotation);
        }

        [Test]
        public void DisabledAnimationRestoresHandsBeforeTheWorkerIsShownAgain()
        {
            var animation = CreateWorker();
            var rest = animation.ArmLeft.localRotation;
            animation.SetWorking(true);
            Step(animation, 1f);
            Assert.That(Quaternion.Angle(rest, animation.ArmLeft.localRotation), Is.GreaterThan(10f),
                        "Tay phải rời khỏi tư thế nghỉ thì hai phép đo dưới mới có nghĩa.");

            // Đúng việc mà OnEnable và OnDisable làm khi thợ bị ẩn đi rồi hiện lại. Gọi thẳng vì
            // Edit Mode không gửi thông điệp vòng đời nào cho component này.
            animation.SnapToRest();
            Assert.AreEqual(rest, animation.ArmLeft.localRotation);

            // Hết việc mà không bị ẩn thì tay cũng về chỗ nghỉ, chỉ là hạ từ từ chứ không snap.
            animation.SetWorking(true);
            Step(animation, 1f);
            animation.SetWorking(false);
            Step(animation, 1f);
            Assert.AreEqual(rest, animation.ArmLeft.localRotation);
        }

        [Test]
        public void MissingOrInactiveArtIsSafeAndCanBeBoundAfterComponentCreation()
        {
            _worker = new GameObject("WorkerWithoutArt");
            var animation = _worker.AddComponent<WorkerWorkAnimation>();
            animation.SetWorking(true);
            Assert.DoesNotThrow(() => Step(animation, .1f));

            var model = new GameObject("LateWorkerModel");
            model.SetActive(false);
            var left = new GameObject("ArmLeft").transform;
            left.SetParent(model.transform, false);
            var right = new GameObject("ArmRight").transform;
            right.SetParent(model.transform, false);
            model.transform.SetParent(_worker.transform, false);
            animation.SetWorking(true);
            Step(animation, 1f);

            Assert.AreSame(left, animation.ArmLeft);
            Assert.AreSame(right, animation.ArmRight);
            Assert.That(Quaternion.Angle(Quaternion.identity, left.localRotation), Is.GreaterThan(10f));
            Assert.IsFalse(model.activeSelf);
        }
    }
}
