using NUnit.Framework;
using ThirdPersonAnimation;
using ThirdPersonAnimation.EditorTools;
using ThirdPersonMovement;
using UnityEditor;
using UnityEngine;

public sealed class LocomotionMotionProfileBakeUtilityTests
{
    [Test]
    public void BakeIntoProfileSamplesSelectedMotionRootTransform()
    {
        GameObject target = new GameObject("motion-profile-bake-target");
        GameObject motionRoot = new GameObject("MotionRoot");
        motionRoot.transform.SetParent(target.transform, false);
        AnimationClip clip = CreateMotionRootClip();
        LocomotionMotionProfileSO profile = ScriptableObject.CreateInstance<LocomotionMotionProfileSO>();

        try
        {
            LocomotionMotionProfileBakeRequest request = new LocomotionMotionProfileBakeRequest(
                target,
                clip,
                BasicMovementPhase.MoveStop,
                BasicMovementGait.Run,
                "RunEnd",
                "MotionRoot",
                30);

            LocomotionMotionProfileBakeUtility.BakeIntoProfile(profile, in request);

            Vector3 endDelta = profile.EvaluateCumulativeLocalPlanarDelta(1f);
            Assert.AreEqual(BasicMovementPhase.MoveStop, profile.Phase);
            Assert.AreEqual(BasicMovementGait.Run, profile.Gait);
            Assert.AreEqual("RunEnd", profile.AliasKey);
            Assert.AreEqual(2f, endDelta.x, 0.001f);
            Assert.AreEqual(3f, endDelta.z, 0.001f);
            Assert.AreEqual(90f, profile.EvaluateCumulativeYaw(1f), 0.01f);
            Assert.AreEqual(clip.name, profile.SourceClipName);
        }
        finally
        {
            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(clip);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void BakeIntoProfileRequiresTargetPrefab()
    {
        AnimationClip clip = CreateMotionRootClip();
        LocomotionMotionProfileSO profile = ScriptableObject.CreateInstance<LocomotionMotionProfileSO>();

        try
        {
            LocomotionMotionProfileBakeRequest request = new LocomotionMotionProfileBakeRequest(
                null,
                clip,
                BasicMovementPhase.MoveStop,
                BasicMovementGait.Run,
                "RunEnd",
                "MotionRoot",
                30);

            Assert.Throws<System.ArgumentException>(() =>
                LocomotionMotionProfileBakeUtility.BakeIntoProfile(profile, in request));
        }
        finally
        {
            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(clip);
        }
    }

    [Test]
    public void BakeIntoProfileFallsBackToAnimatorRootCurvesWhenMotionRootIsStatic()
    {
        GameObject target = new GameObject("motion-profile-root-curve-target");
        AnimationClip clip = CreateAnimatorRootClip();
        LocomotionMotionProfileSO profile = ScriptableObject.CreateInstance<LocomotionMotionProfileSO>();

        try
        {
            LocomotionMotionProfileBakeRequest request = new LocomotionMotionProfileBakeRequest(
                target,
                clip,
                BasicMovementPhase.MoveStop,
                BasicMovementGait.Run,
                "RunEnd",
                "MissingMotionRoot",
                30);

            LocomotionMotionProfileBakeUtility.BakeIntoProfile(profile, in request);

            Vector3 endDelta = profile.EvaluateCumulativeLocalPlanarDelta(1f);
            Assert.AreEqual(1.5f, endDelta.x, 0.001f);
            Assert.AreEqual(-2.5f, endDelta.z, 0.001f);
            Assert.AreEqual(45f, profile.EvaluateCumulativeYaw(1f), 0.01f);
        }
        finally
        {
            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(clip);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void BakeIntoProfileFallsBackToAnimatorRootTranslationWhenMotionRootOnlyRotates()
    {
        GameObject target = new GameObject("motion-profile-partial-root-curve-target");
        GameObject motionRoot = new GameObject("MotionRoot");
        motionRoot.transform.SetParent(target.transform, false);
        AnimationClip clip = CreateRotatingMotionRootWithAnimatorTranslationClip();
        LocomotionMotionProfileSO profile = ScriptableObject.CreateInstance<LocomotionMotionProfileSO>();

        try
        {
            LocomotionMotionProfileBakeRequest request = new LocomotionMotionProfileBakeRequest(
                target,
                clip,
                BasicMovementPhase.TurnBack,
                BasicMovementGait.Run,
                "Locomotion.Turn.Back",
                "MotionRoot",
                30,
                0.5f);

            LocomotionMotionProfileBakeUtility.BakeIntoProfile(profile, in request);

            Vector3 endDelta = profile.EvaluateCumulativeLocalPlanarDelta(1f);
            Assert.AreEqual(0.5f, profile.Duration, 0.0001f);
            Assert.AreEqual(0f, endDelta.x, 0.001f);
            Assert.AreEqual(1f, endDelta.z, 0.001f);
            Assert.AreEqual(45f, profile.EvaluateCumulativeYaw(1f), 0.01f);
        }
        finally
        {
            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(clip);
            Object.DestroyImmediate(target);
        }
    }

    static AnimationClip CreateMotionRootClip()
    {
        AnimationClip clip = new AnimationClip
        {
            name = "TestMotionRootClip",
            frameRate = 30f,
        };

        SetCurve(clip, "MotionRoot", "m_LocalPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 2f));
        SetCurve(clip, "MotionRoot", "m_LocalPosition.z", AnimationCurve.Linear(0f, 0f, 1f, 3f));
        SetCurve(clip, "MotionRoot", "localEulerAnglesRaw.y", AnimationCurve.Linear(0f, 0f, 1f, 90f));
        return clip;
    }

    static AnimationClip CreateAnimatorRootClip()
    {
        AnimationClip clip = new AnimationClip
        {
            name = "TestAnimatorRootClip",
            frameRate = 30f,
        };

        AnimationUtility.SetEditorCurve(
            clip,
            EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), "RootT.x"),
            AnimationCurve.Linear(0f, 0f, 1f, 1.5f));
        AnimationUtility.SetEditorCurve(
            clip,
            EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), "RootT.z"),
            AnimationCurve.Linear(0f, 0f, 1f, -2.5f));

        Quaternion end = Quaternion.Euler(0f, 45f, 0f);
        AnimationUtility.SetEditorCurve(
            clip,
            EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), "RootQ.x"),
            AnimationCurve.Linear(0f, 0f, 1f, end.x));
        AnimationUtility.SetEditorCurve(
            clip,
            EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), "RootQ.y"),
            AnimationCurve.Linear(0f, 0f, 1f, end.y));
        AnimationUtility.SetEditorCurve(
            clip,
            EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), "RootQ.z"),
            AnimationCurve.Linear(0f, 0f, 1f, end.z));
        AnimationUtility.SetEditorCurve(
            clip,
            EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), "RootQ.w"),
            AnimationCurve.Linear(0f, 1f, 1f, end.w));
        return clip;
    }

    static AnimationClip CreateRotatingMotionRootWithAnimatorTranslationClip()
    {
        AnimationClip clip = new AnimationClip
        {
            name = "TestMotionRootRotationWithAnimatorTranslationClip",
            frameRate = 30f,
        };

        SetCurve(clip, "MotionRoot", "localEulerAnglesRaw.y", AnimationCurve.Linear(0f, 0f, 1f, 90f));
        AnimationUtility.SetEditorCurve(
            clip,
            EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), "RootT.z"),
            AnimationCurve.Linear(0f, 3f, 1f, 5f));
        return clip;
    }

    static void SetCurve(AnimationClip clip, string path, string propertyName, AnimationCurve curve)
    {
        AnimationUtility.SetEditorCurve(
            clip,
            EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName),
            curve);
    }
}
