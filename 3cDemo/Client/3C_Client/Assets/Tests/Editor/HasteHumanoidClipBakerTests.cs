using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HasteAnimation.Tests
{
    public sealed class HasteHumanoidClipBakerTests
    {
        const string SourcePrefabPath = "Assets/Art/Animation/Haste/HasteMainCharacter/GameObject/Courier_Retake.prefab";
        const string SourceAvatarPath = "Assets/Art/Animation/Haste/HasteMainCharacter/HUMANOID/HasteCourier_HumanoidAvatar.asset";
        const string SourceClipPath = "Assets/Art/Animation/Haste/HasteMainCharacter/AnimationClip/New_Courier_Dash_Loop.anim";
        const string TargetHumanoidPrefabPath = "Packages/com.kybernetik.animancer/Art/Animancer Humanoid/AnimancerHumanoid.prefab";

        [Test]
        public void BakeClipWritesNonZeroHumanoidMuscleCurves()
        {
            AnimationClip bakedClip = BakeDashLoop();

            try
            {
                Assert.IsTrue(bakedClip.humanMotion);
                Assert.That(GetMaxMuscleCurveAbs(bakedClip), Is.GreaterThan(0.1f));
            }
            finally
            {
                Object.DestroyImmediate(bakedClip);
            }
        }

        [Test]
        public void BakedClipRetargetsSourcePoseToAnotherHumanoidAvatar()
        {
            AnimationClip sourceClip = LoadRequired<AnimationClip>(SourceClipPath);
            AnimationClip bakedClip = BakeDashLoop();
            GameObject sourceInstance = null;
            GameObject targetInstance = null;

            try
            {
                sourceInstance = InstantiatePrefab(SourcePrefabPath);
                Transform courier = FindRequired(sourceInstance.transform, "Courier");
                RemoveAnimator(courier.gameObject);
                HumanPose sourcePose = SamplePose(courier, LoadRequired<Avatar>(SourceAvatarPath), sourceClip, sourceClip.length * 0.5f);

                targetInstance = InstantiatePrefab(TargetHumanoidPrefabPath);
                Animator targetAnimator = targetInstance.GetComponentInChildren<Animator>();
                Assert.NotNull(targetAnimator);
                Assert.NotNull(targetAnimator.avatar);
                Assert.IsTrue(targetAnimator.avatar.isHuman);

                HumanPose targetPose = SamplePose(targetAnimator.transform, targetAnimator.avatar, bakedClip, bakedClip.length * 0.5f);

                Assert.That(Vector3.Distance(sourcePose.bodyPosition, targetPose.bodyPosition), Is.LessThan(0.01f));
                Assert.That(Quaternion.Angle(sourcePose.bodyRotation, targetPose.bodyRotation), Is.LessThan(0.1f));
                Assert.That(GetPrimaryMuscleDifference(sourcePose, targetPose), Is.LessThan(0.05f));
            }
            finally
            {
                Object.DestroyImmediate(bakedClip);
                if (sourceInstance != null)
                    Object.DestroyImmediate(sourceInstance);
                if (targetInstance != null)
                    Object.DestroyImmediate(targetInstance);
            }
        }

        static AnimationClip BakeDashLoop()
        {
            return HasteHumanoidClipBaker.BakeClip(
                LoadRequired<GameObject>(SourcePrefabPath),
                LoadRequired<Avatar>(SourceAvatarPath),
                LoadRequired<AnimationClip>(SourceClipPath));
        }

        static HumanPose SamplePose(Transform root, Avatar avatar, AnimationClip clip, float time)
        {
            clip.SampleAnimation(root.gameObject, time);

            var handler = new HumanPoseHandler(avatar, root);
            var pose = new HumanPose();
            handler.GetHumanPose(ref pose);
            handler.Dispose();
            return pose;
        }

        static float GetPrimaryMuscleDifference(HumanPose expected, HumanPose actual)
        {
            float sum = 0f;
            for (int i = 0; i < expected.muscles.Length; i++)
            {
                if (HumanTrait.MuscleName[i].IndexOf("Twist", System.StringComparison.Ordinal) >= 0)
                    continue;

                sum += Mathf.Abs(expected.muscles[i] - actual.muscles[i]);
            }

            return sum;
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

        static GameObject InstantiatePrefab(string path)
        {
            GameObject prefab = LoadRequired<GameObject>(path);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            return instance != null ? instance : Object.Instantiate(prefab);
        }

        static Transform FindRequired(Transform root, string name)
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == name)
                    return transform;
            }

            Assert.Fail("Missing transform: " + name);
            return null;
        }

        static void RemoveAnimator(GameObject target)
        {
            Animator animator = target.GetComponent<Animator>();
            if (animator != null)
                Object.DestroyImmediate(animator);
        }
    }
}
