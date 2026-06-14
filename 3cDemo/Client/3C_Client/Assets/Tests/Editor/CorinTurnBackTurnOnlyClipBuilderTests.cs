using NUnit.Framework;
using ThirdPersonAnimation.EditorTools;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor
{
    public sealed class CorinTurnBackTurnOnlyClipBuilderTests
    {
        [Test]
        public void BuildTurnOnlyClipZeroesRootTAndClipsRootQ()
        {
            AnimationClip source = new AnimationClip
            {
                name = "SourceTurnBack",
                frameRate = 60f,
            };
            SetAnimatorCurve(source, "RootT.x", AnimationCurve.Linear(0f, 0.25f, 1f, 2f));
            SetAnimatorCurve(source, "RootT.y", AnimationCurve.Linear(0f, 0.68f, 1f, 0.1f));
            SetAnimatorCurve(source, "RootT.z", AnimationCurve.Linear(0f, 3.384f, 1f, 0.348f));
            SetAnimatorCurve(source, "RootQ.y", AnimationCurve.Linear(0f, 0f, 1f, 1f));
            SetAnimatorCurve(source, "RootQ.w", AnimationCurve.Linear(0f, 1f, 1f, 0f));
            SetTransformCurve(source, "Bip001", "m_LocalPosition.z", AnimationCurve.Linear(0f, 10f, 1f, 20f));
            SetTransformCurve(source, "Bip001", "m_LocalPosition.y", AnimationCurve.Linear(0f, 0.68f, 1f, 0.42f));
            SetTransformCurve(source, "Root", "m_LocalPosition.z", AnimationCurve.Linear(0f, 3.384f, 1f, 4.384f));
            SetTransformCurve(source, "Bip001/Bip001 Pelvis", "m_LocalPosition.z", AnimationCurve.Linear(0f, 2f, 1f, 4f));

            AnimationClip output = CorinTurnBackTurnOnlyClipBuilder.BuildTurnOnlyClip(source, 0.47f, "TurnOnly");

            Assert.AreEqual("TurnOnly", output.name);
            Assert.AreEqual(0.47f, output.length, 0.0001f);
            AssertRootCurve(output, "RootT.x", 0f, 0f);
            AssertRootCurve(output, "RootT.y", 0f, 0f);
            AssertRootCurve(output, "RootT.z", 0f, 0f);
            AssertRootCurve(output, "RootQ.y", 0f, 0.47f);
            AssertRootCurve(output, "RootQ.w", 1f, 0.53f);
            AnimationCurve bipZ = AnimationUtility.GetEditorCurve(
                output,
                EditorCurveBinding.FloatCurve("Bip001", typeof(Transform), "m_LocalPosition.z"));
            Assert.NotNull(bipZ);
            Assert.AreEqual(0f, bipZ.Evaluate(0f), 0.0001f);
            Assert.AreEqual(0f, bipZ.Evaluate(0.47f), 0.0001f);

            AnimationCurve bipY = AnimationUtility.GetEditorCurve(
                output,
                EditorCurveBinding.FloatCurve("Bip001", typeof(Transform), "m_LocalPosition.y"));
            Assert.NotNull(bipY);
            Assert.AreEqual(0.68f, bipY.Evaluate(0f), 0.0001f);
            Assert.AreEqual(0.5578f, bipY.Evaluate(0.47f), 0.0001f);

            AnimationCurve rootZ = AnimationUtility.GetEditorCurve(
                output,
                EditorCurveBinding.FloatCurve("Root", typeof(Transform), "m_LocalPosition.z"));
            Assert.NotNull(rootZ);
            Assert.AreEqual(0f, rootZ.Evaluate(0f), 0.0001f);
            Assert.AreEqual(0f, rootZ.Evaluate(0.47f), 0.0001f);

            AnimationCurve pelvisZ = AnimationUtility.GetEditorCurve(
                output,
                EditorCurveBinding.FloatCurve("Bip001/Bip001 Pelvis", typeof(Transform), "m_LocalPosition.z"));
            Assert.NotNull(pelvisZ);
            Assert.AreEqual(2f, pelvisZ.Evaluate(0f), 0.0001f);
            Assert.AreEqual(2.94f, pelvisZ.Evaluate(0.47f), 0.0001f);
        }

        static void AssertRootCurve(AnimationClip clip, string propertyName, float start, float end)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), propertyName));
            Assert.NotNull(curve, propertyName);
            Assert.AreEqual(start, curve.Evaluate(0f), 0.0001f, propertyName);
            Assert.AreEqual(end, curve.Evaluate(0.47f), 0.0001f, propertyName);
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
