using NUnit.Framework;
using ThirdPersonAnimation.EditorTools;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor
{
    public sealed class CorinInplaceVisualClipCleanerTests
    {
        [Test]
        public void BuildCleanClipRemovesOnlyRootMotionCarrierCurves()
        {
            AnimationClip source = new AnimationClip
            {
                name = "TurnBackInplace",
                frameRate = 60f,
            };

            SetAnimatorCurve(source, "RootT.x", AnimationCurve.Linear(0f, 2f, 1f, 4f));
            SetAnimatorCurve(source, "RootT.y", AnimationCurve.Linear(0f, 0.5f, 1f, 0.75f));
            SetAnimatorCurve(source, "RootT.z", AnimationCurve.Linear(0f, -1f, 1f, -3f));
            SetAnimatorCurve(source, "RootQ.x", AnimationCurve.Linear(0f, 0.1f, 1f, 0.2f));
            SetAnimatorCurve(source, "RootQ.y", AnimationCurve.Linear(0f, 0.3f, 1f, 0.7f));
            SetAnimatorCurve(source, "RootQ.z", AnimationCurve.Linear(0f, 0.4f, 1f, 0.8f));
            SetAnimatorCurve(source, "RootQ.w", AnimationCurve.Linear(0f, 0.9f, 1f, 0.1f));
            SetTransformCurve(source, "Bip001", "m_LocalPosition.x", AnimationCurve.Linear(0f, 1f, 1f, 2f));
            SetTransformCurve(source, "Bip001", "m_LocalPosition.y", AnimationCurve.Linear(0f, 3f, 1f, 4f));
            SetTransformCurve(source, "Bip001", "m_LocalPosition.z", AnimationCurve.Linear(0f, 5f, 1f, 6f));
            SetTransformCurve(source, "Bip001", "m_LocalRotation.x", AnimationCurve.Linear(0f, 0.11f, 1f, 0.12f));
            SetTransformCurve(source, "Bip001", "m_LocalRotation.y", AnimationCurve.Linear(0f, 0.21f, 1f, 0.22f));
            SetTransformCurve(source, "Bip001", "m_LocalRotation.z", AnimationCurve.Linear(0f, 0.31f, 1f, 0.32f));
            SetTransformCurve(source, "Bip001", "m_LocalRotation.w", AnimationCurve.Linear(0f, 0.91f, 1f, 0.92f));
            SetTransformCurve(source, "Bip001/Bip001 Pelvis", "m_LocalRotation.y", AnimationCurve.Linear(0f, 0.41f, 1f, 0.42f));
            SetTransformCurve(source, "Bip001/Bip001 Pelvis", "m_LocalPosition.x", AnimationCurve.Linear(0f, 7f, 1f, 8f));

            AnimationClip output = CorinInplaceVisualClipCleaner.BuildCleanClip(source, "Bip001", "CleanTurnBackInplace");

            Assert.AreEqual("CleanTurnBackInplace", output.name);
            AssertCurve(output, string.Empty, typeof(Animator), "RootT.x", 0f, 0f);
            AssertCurve(output, string.Empty, typeof(Animator), "RootT.y", 0.5f, 0.75f);
            AssertCurve(output, string.Empty, typeof(Animator), "RootT.z", 0f, 0f);
            AssertCurve(output, string.Empty, typeof(Animator), "RootQ.x", 0.1f, 0.1f);
            AssertCurve(output, string.Empty, typeof(Animator), "RootQ.y", 0.3f, 0.3f);
            AssertCurve(output, string.Empty, typeof(Animator), "RootQ.z", 0.4f, 0.4f);
            AssertCurve(output, string.Empty, typeof(Animator), "RootQ.w", 0.9f, 0.9f);
            AssertCurve(output, "Bip001", typeof(Transform), "m_LocalPosition.x", 0f, 0f);
            AssertCurve(output, "Bip001", typeof(Transform), "m_LocalPosition.y", 3f, 4f);
            AssertCurve(output, "Bip001", typeof(Transform), "m_LocalPosition.z", 0f, 0f);
            AssertCurve(output, "Bip001", typeof(Transform), "m_LocalRotation.x", 0.11f, 0.11f);
            AssertCurve(output, "Bip001", typeof(Transform), "m_LocalRotation.y", 0.21f, 0.21f);
            AssertCurve(output, "Bip001", typeof(Transform), "m_LocalRotation.z", 0.31f, 0.31f);
            AssertCurve(output, "Bip001", typeof(Transform), "m_LocalRotation.w", 0.91f, 0.91f);
            AssertCurve(output, "Bip001/Bip001 Pelvis", typeof(Transform), "m_LocalRotation.y", 0.41f, 0.42f);
            AssertCurve(output, "Bip001/Bip001 Pelvis", typeof(Transform), "m_LocalPosition.x", 7f, 8f);
        }

        [Test]
        public void BuildCleanClipTreatsRootAndNestedRootPathAsRootCarrier()
        {
            AnimationClip source = new AnimationClip
            {
                name = "NestedRootInplace",
                frameRate = 30f,
            };

            SetTransformCurve(source, "Root", "m_LocalRotation.y", AnimationCurve.Linear(0f, 0.2f, 1f, 0.4f));
            SetTransformCurve(source, "Armature/Bip001", "m_LocalPosition.z", AnimationCurve.Linear(0f, 2f, 1f, 4f));
            SetTransformCurve(source, "Armature/Bip001/Bip001 Pelvis", "m_LocalRotation.y", AnimationCurve.Linear(0f, 0.6f, 1f, 0.8f));

            AnimationClip output = CorinInplaceVisualClipCleaner.BuildCleanClip(source, "Armature/Bip001", "NestedClean");

            AssertCurve(output, "Root", typeof(Transform), "m_LocalRotation.y", 0.2f, 0.2f);
            AssertCurve(output, "Armature/Bip001", typeof(Transform), "m_LocalPosition.z", 0f, 0f);
            AssertCurve(output, "Armature/Bip001/Bip001 Pelvis", typeof(Transform), "m_LocalRotation.y", 0.6f, 0.8f);
        }

        static void AssertCurve(AnimationClip clip, string path, System.Type type, string propertyName, float start, float end)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, type, propertyName));

            Assert.NotNull(curve, path + "." + propertyName);
            Assert.AreEqual(start, curve.Evaluate(0f), 0.0001f, propertyName);
            Assert.AreEqual(end, curve.Evaluate(clip.length), 0.0001f, propertyName);
        }

        static void SetAnimatorCurve(AnimationClip clip, string propertyName, AnimationCurve curve)
        {
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), propertyName),
                curve);
        }

        static void SetTransformCurve(AnimationClip clip, string path, string propertyName, AnimationCurve curve)
        {
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName),
                curve);
        }
    }
}
