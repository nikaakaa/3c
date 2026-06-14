using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class CorinHumanoidMuscleClipBakerTests
{
    const string OldFilteredRunLoopPath = "Assets/Art/Animation/MyDemoNeed/Corin/Humanoid/Inplace/Corin_RunLoop_Inplace.anim";

    [Test]
    public void BakeClipWritesUnityHumanoidMuscleCurves()
    {
        AnimationClip bakedClip = BakeFor("Corin_RunLoop", true);

        try
        {
            Assert.IsTrue(bakedClip.humanMotion);
            Assert.IsTrue(CorinHumanoidMuscleClipBaker.IsHumanoidMuscleClip(bakedClip));
            Assert.That(GetMaxMuscleCurveAbs(bakedClip), Is.GreaterThan(0.1f));
        }
        finally
        {
            Object.DestroyImmediate(bakedClip);
        }
    }

    [Test]
    public void BakeClipCanUseStandaloneRunEndSource()
    {
        AnimationClip bakedClip = BakeFor("Corin_Run_End", true);

        try
        {
            Assert.IsTrue(bakedClip.humanMotion);
            Assert.IsTrue(CorinHumanoidMuscleClipBaker.IsHumanoidMuscleClip(bakedClip));
        }
        finally
        {
            Object.DestroyImmediate(bakedClip);
        }
    }

    [Test]
    public void InplaceBakeKeepsRootXZConstant()
    {
        AnimationClip bakedClip = BakeFor("Corin_RunLoop", true);

        try
        {
            AssertCurveIsConstant(bakedClip, "RootT.x");
            AssertCurveIsConstant(bakedClip, "RootT.z");
        }
        finally
        {
            Object.DestroyImmediate(bakedClip);
        }
    }

    [Test]
    public void OldFilteredHumanoidClipIsNotHumanoidMuscleClip()
    {
        AnimationClip oldClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(OldFilteredRunLoopPath);
        Assert.NotNull(oldClip);
        Assert.IsFalse(CorinHumanoidMuscleClipBaker.IsHumanoidMuscleClip(oldClip));
    }

    static AnimationClip BakeFor(string outputName, bool neutralizeRootXZ)
    {
        return CorinHumanoidMuscleClipBaker.BakeClip(
            LoadRequired<GameObject>(CorinHumanoidMuscleClipBaker.HumanoidModelPath),
            FindHumanoidAvatar(),
            CorinHumanoidMuscleClipBaker.LoadSourceClipForTest(outputName),
            neutralizeRootXZ);
    }

    static Avatar FindHumanoidAvatar()
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(CorinHumanoidMuscleClipBaker.HumanoidModelPath))
        {
            if (asset is Avatar avatar && avatar.isValid && avatar.isHuman)
                return avatar;
        }

        Assert.Fail("Missing Corin Humanoid Avatar.");
        return null;
    }

    static float GetMaxMuscleCurveAbs(AnimationClip clip)
    {
        float max = 0f;
        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (binding.type != typeof(Animator) || IsRootBinding(binding.propertyName))
                continue;

            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
            foreach (Keyframe key in curve.keys)
                max = Mathf.Max(max, Mathf.Abs(key.value));
        }

        return max;
    }

    static void AssertCurveIsConstant(AnimationClip clip, string propertyName)
    {
        AnimationCurve curve = AnimationUtility.GetEditorCurve(
            clip,
            EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), propertyName));

        Assert.NotNull(curve, propertyName);
        Assert.That(curve.length, Is.GreaterThanOrEqualTo(2));

        float value = curve.keys[0].value;
        foreach (Keyframe key in curve.keys)
            Assert.That(key.value, Is.EqualTo(value).Within(0.0001f), propertyName);
    }

    static bool IsRootBinding(string propertyName)
    {
        return propertyName == "RootT.x" ||
               propertyName == "RootT.y" ||
               propertyName == "RootT.z" ||
               propertyName == "RootQ.x" ||
               propertyName == "RootQ.y" ||
               propertyName == "RootQ.z" ||
               propertyName == "RootQ.w";
    }

    static T LoadRequired<T>(string path) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        Assert.NotNull(asset, path);
        return asset;
    }
}
