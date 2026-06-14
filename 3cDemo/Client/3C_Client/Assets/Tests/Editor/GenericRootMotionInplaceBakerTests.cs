using NUnit.Framework;
using ThirdPersonAnimation;
using ThirdPersonAnimation.EditorTools;
using ThirdPersonMovement;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor
{
    public sealed class GenericRootMotionInplaceBakerTests
    {
        [Test]
        public void BuildInplaceClipCompensatesRootAgainstMotionProfile()
        {
            AnimationClip source = CreateSourceClip();
            LocomotionMotionProfileSO profile = CreateProfile();

            try
            {
                var request = new GenericRootMotionInplaceBakeRequest(
                    source,
                    profile,
                    "Bip001",
                    30,
                    1f,
                    "TurnBackInplace",
                    GenericRootTransformBakeMode.CompensateRootTransform);
                AnimationClip output = GenericRootMotionInplaceBaker.BuildInplaceClip(in request);

                Assert.AreEqual("TurnBackInplace", output.name);
                AssertVector(output, "Bip001", "m_LocalPosition", 0f, new Vector3(0.25f, 1f, 0.5f));
                AssertVector(output, "Bip001", "m_LocalPosition", 1f, new Vector3(0.5f, 1.25f, 0.25f));
                AssertQuaternion(output, "Bip001", 0f, Quaternion.Euler(10f, 0f, 0f));
                AssertQuaternion(output, "Bip001", 1f, Quaternion.Euler(20f, 0f, 0f));
                AssertCurve(output, "Bip001/Bip001 Pelvis", typeof(Transform), "m_LocalPosition.x", 5f, 6f);
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void BuildInplaceClipPreservesRootTransformByDefault()
        {
            AnimationClip source = CreateSourceClip();
            LocomotionMotionProfileSO profile = CreateProfile();

            try
            {
                var request = new GenericRootMotionInplaceBakeRequest(source, profile, "Bip001", 30, 1f, "TurnBackInplace");
                AnimationClip output = GenericRootMotionInplaceBaker.BuildInplaceClip(in request);

                AssertVector(output, "Bip001", "m_LocalPosition", 0f, SourceStartPosition);
                AssertVector(output, "Bip001", "m_LocalPosition", 1f, SourceEndPosition);
                AssertQuaternion(output, "Bip001", 0f, SourceStartRotation);
                AssertQuaternion(output, "Bip001", 1f, SourceEndRotation);
                AssertCurve(output, "Bip001/Bip001 Pelvis", typeof(Transform), "m_LocalPosition.x", 5f, 6f);
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void BuildInplaceClipNeutralizesAnimatorRootMotionCurves()
        {
            AnimationClip source = CreateSourceClip();
            LocomotionMotionProfileSO profile = CreateProfile();

            try
            {
                var request = new GenericRootMotionInplaceBakeRequest(source, profile, "Bip001", 30, 1f, "TurnBackInplace");
                AnimationClip output = GenericRootMotionInplaceBaker.BuildInplaceClip(in request);

                AssertCurve(output, string.Empty, typeof(Animator), "RootT.x", 0f, 0f);
                AssertCurve(output, string.Empty, typeof(Animator), "RootT.y", 0f, 0f);
                AssertCurve(output, string.Empty, typeof(Animator), "RootT.z", 0f, 0f);
                AssertCurve(output, string.Empty, typeof(Animator), "RootQ.x", 0f, 0f);
                AssertCurve(output, string.Empty, typeof(Animator), "RootQ.y", 0f, 0f);
                AssertCurve(output, string.Empty, typeof(Animator), "RootQ.z", 0f, 0f);
                AssertCurve(output, string.Empty, typeof(Animator), "RootQ.w", 1f, 1f);
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(source);
            }
        }

        static AnimationClip CreateSourceClip()
        {
            var clip = new AnimationClip
            {
                name = "TurnBackRootMotion",
                frameRate = 30f,
            };

            SetVectorCurve(clip, "Bip001", "m_LocalPosition", SourceStartPosition, SourceEndPosition);
            SetQuaternionCurve(clip, "Bip001", SourceStartRotation, SourceEndRotation);
            SetVectorCurve(clip, "Bip001/Bip001 Pelvis", "m_LocalPosition", new Vector3(5f, 0f, 0f), new Vector3(6f, 0f, 0f));
            SetAnimatorRootMotionCurves(clip);
            return clip;
        }

        static readonly Vector3 SourceStartPosition = new Vector3(0.25f, 1f, 0.5f);
        static readonly Vector3 VisualEndPosition = new Vector3(0.5f, 1.25f, 0.25f);
        static readonly Vector3 SourceEndPosition = SourceStartPosition + new Vector3(2f, 0f, 3f) + Quaternion.Euler(0f, 90f, 0f) * (VisualEndPosition - SourceStartPosition);
        static readonly Quaternion SourceStartRotation = Quaternion.Euler(10f, 0f, 0f);
        static readonly Quaternion VisualEndRotation = Quaternion.Euler(20f, 0f, 0f);
        static readonly Quaternion SourceEndRotation = Quaternion.Euler(0f, 90f, 0f) * VisualEndRotation;

        static LocomotionMotionProfileSO CreateProfile()
        {
            LocomotionMotionProfileSO profile = ScriptableObject.CreateInstance<LocomotionMotionProfileSO>();
            profile.SetBakedData(
                BasicMovementPhase.TurnBack,
                BasicMovementGait.Run,
                "Locomotion.Turn.Back",
                1f,
                AnimationCurve.Linear(0f, 0f, 1f, 2f),
                AnimationCurve.Linear(0f, 0f, 1f, 3f),
                AnimationCurve.Linear(0f, 0f, 1f, 90f),
                "TurnBackRootMotion",
                string.Empty);
            return profile;
        }

        static void SetAnimatorRootMotionCurves(AnimationClip clip)
        {
            SetCurve(clip, string.Empty, typeof(Animator), "RootT.x", AnimationCurve.Linear(0f, 10f, 1f, 12f));
            SetCurve(clip, string.Empty, typeof(Animator), "RootT.y", AnimationCurve.Linear(0f, 2f, 1f, 3f));
            SetCurve(clip, string.Empty, typeof(Animator), "RootT.z", AnimationCurve.Linear(0f, -1f, 1f, 3f));
            SetCurve(clip, string.Empty, typeof(Animator), "RootQ.x", AnimationCurve.Linear(0f, 0.1f, 1f, 0.2f));
            SetCurve(clip, string.Empty, typeof(Animator), "RootQ.y", AnimationCurve.Linear(0f, 0.2f, 1f, 0.3f));
            SetCurve(clip, string.Empty, typeof(Animator), "RootQ.z", AnimationCurve.Linear(0f, 0.3f, 1f, 0.4f));
            SetCurve(clip, string.Empty, typeof(Animator), "RootQ.w", AnimationCurve.Linear(0f, 0.9f, 1f, 0.8f));
        }

        static void SetVectorCurve(AnimationClip clip, string path, string property, Vector3 start, Vector3 end)
        {
            SetCurve(clip, path, typeof(Transform), property + ".x", AnimationCurve.Linear(0f, start.x, 1f, end.x));
            SetCurve(clip, path, typeof(Transform), property + ".y", AnimationCurve.Linear(0f, start.y, 1f, end.y));
            SetCurve(clip, path, typeof(Transform), property + ".z", AnimationCurve.Linear(0f, start.z, 1f, end.z));
        }

        static void SetQuaternionCurve(AnimationClip clip, string path, Quaternion start, Quaternion end)
        {
            SetCurve(clip, path, typeof(Transform), "m_LocalRotation.x", AnimationCurve.Linear(0f, start.x, 1f, end.x));
            SetCurve(clip, path, typeof(Transform), "m_LocalRotation.y", AnimationCurve.Linear(0f, start.y, 1f, end.y));
            SetCurve(clip, path, typeof(Transform), "m_LocalRotation.z", AnimationCurve.Linear(0f, start.z, 1f, end.z));
            SetCurve(clip, path, typeof(Transform), "m_LocalRotation.w", AnimationCurve.Linear(0f, start.w, 1f, end.w));
        }

        static void SetCurve(AnimationClip clip, string path, System.Type type, string propertyName, AnimationCurve curve)
        {
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, type, propertyName), curve);
        }

        static void AssertVector(AnimationClip clip, string path, string property, float time, Vector3 expected)
        {
            Assert.AreEqual(expected.x, Evaluate(clip, path, typeof(Transform), property + ".x", time), 0.0001f);
            Assert.AreEqual(expected.y, Evaluate(clip, path, typeof(Transform), property + ".y", time), 0.0001f);
            Assert.AreEqual(expected.z, Evaluate(clip, path, typeof(Transform), property + ".z", time), 0.0001f);
        }

        static void AssertQuaternion(AnimationClip clip, string path, float time, Quaternion expected)
        {
            var actual = new Quaternion(
                Evaluate(clip, path, typeof(Transform), "m_LocalRotation.x", time),
                Evaluate(clip, path, typeof(Transform), "m_LocalRotation.y", time),
                Evaluate(clip, path, typeof(Transform), "m_LocalRotation.z", time),
                Evaluate(clip, path, typeof(Transform), "m_LocalRotation.w", time));

            if (Quaternion.Dot(actual, expected) < 0f)
                actual = new Quaternion(-actual.x, -actual.y, -actual.z, -actual.w);

            Assert.AreEqual(0f, Quaternion.Angle(expected, actual), 0.01f);
        }

        static void AssertCurve(AnimationClip clip, string path, System.Type type, string propertyName, float start, float end)
        {
            Assert.AreEqual(start, Evaluate(clip, path, type, propertyName, 0f), 0.0001f, propertyName);
            Assert.AreEqual(end, Evaluate(clip, path, type, propertyName, 1f), 0.0001f, propertyName);
        }

        static float Evaluate(AnimationClip clip, string path, System.Type type, string propertyName, float time)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, type, propertyName));
            Assert.NotNull(curve, path + "." + propertyName);
            return curve.Evaluate(time);
        }
    }
}
