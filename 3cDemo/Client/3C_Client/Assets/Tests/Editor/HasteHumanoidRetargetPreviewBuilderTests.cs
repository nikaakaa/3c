using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace HasteAnimation.Tests
{
    public sealed class HasteHumanoidRetargetPreviewBuilderTests
    {
        const string SourcePrefabPath = "Assets/Art/Animation/Haste/HasteMainCharacter/GameObject/Courier_Retake.prefab";
        const string SourceAvatarPath = "Assets/Art/Animation/Haste/HasteMainCharacter/HUMANOID/HasteCourier_HumanoidAvatar.asset";
        const string SourceClipPath = "Assets/Art/Animation/Haste/HasteMainCharacter/AnimationClip/New_Courier_Dash_Loop.anim";
        const string TargetHumanoidPrefabPath = "Packages/com.kybernetik.animancer/Art/Animancer Humanoid/AnimancerHumanoid.prefab";
        const string TargetModelPath = "Assets/Art/Animation/ZZZ/可琳/可琳（基本动作）.fbx";
        const string OutputRoot = "Assets/Temp/HasteRetargetPreviewBuilderTests";
        const string TempClipPath = OutputRoot + "/Humanoid_New_Courier_Dash_Loop_Test.anim";

        [SetUp]
        public void SetUp()
        {
            DeleteTempRoot();
            EnsureFolder(OutputRoot);
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTempRoot();
        }

        [Test]
        public void CreatePreviewAssignsHumanoidClipToTargetAnimator()
        {
            AnimationClip clip = CreateSavedHumanoidClip();
            GameObject target = LoadRequired<GameObject>(TargetHumanoidPrefabPath);

            HasteHumanoidRetargetPreviewBuilder.RetargetPreviewResult result =
                HasteHumanoidRetargetPreviewBuilder.CreatePreview(target, clip, OutputRoot);

            GameObject preview = LoadRequired<GameObject>(result.PreviewPrefabPath);
            Animator animator = preview.GetComponentInChildren<Animator>(true);
            Assert.NotNull(animator);
            Assert.NotNull(animator.avatar);
            Assert.IsTrue(animator.avatar.isHuman);
            Assert.AreSame(result.Controller, animator.runtimeAnimatorController);

            var controller = result.Controller as AnimatorController;
            Assert.NotNull(controller);
            Assert.AreSame(clip, controller.layers[0].stateMachine.defaultState.motion);
        }

        [Test]
        public void CreatePreviewRejectsTargetWithoutHumanoidAvatar()
        {
            AnimationClip clip = CreateSavedHumanoidClip();
            var target = new GameObject("TargetWithoutAvatar");
            target.AddComponent<Animator>();

            try
            {
                InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                    () => HasteHumanoidRetargetPreviewBuilder.CreatePreview(target, clip, OutputRoot));

                StringAssert.Contains("valid Humanoid Avatar", exception.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void TryResolveTargetModelImporterFindsSelectedModelAsset()
        {
            GameObject target = LoadRequired<GameObject>(TargetModelPath);

            bool resolved = HasteHumanoidRetargetPreviewBuilder.TryResolveTargetModelImporter(
                target,
                out string modelPath,
                out ModelImporter importer);

            Assert.IsTrue(resolved);
            Assert.IsFalse(string.IsNullOrEmpty(modelPath));
            Assert.NotNull(importer);
        }

        [Test]
        public void TryResolveTargetModelImporterReturnsFalseWithoutModelAsset()
        {
            var target = new GameObject("SceneOnlyTarget");
            target.AddComponent<Animator>();

            try
            {
                bool resolved = HasteHumanoidRetargetPreviewBuilder.TryResolveTargetModelImporter(
                    target,
                    out string modelPath,
                    out ModelImporter importer);

                Assert.IsFalse(resolved);
                Assert.IsNull(modelPath);
                Assert.IsNull(importer);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        static AnimationClip CreateSavedHumanoidClip()
        {
            AnimationClip clip = HasteHumanoidClipBaker.BakeClip(
                LoadRequired<GameObject>(SourcePrefabPath),
                LoadRequired<Avatar>(SourceAvatarPath),
                LoadRequired<AnimationClip>(SourceClipPath));

            AssetDatabase.CreateAsset(clip, TempClipPath);
            AssetDatabase.SaveAssets();
            return LoadRequired<AnimationClip>(TempClipPath);
        }

        static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.NotNull(asset, path);
            return asset;
        }

        static void EnsureFolder(string assetFolder)
        {
            string[] parts = assetFolder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        static void DeleteTempRoot()
        {
            if (AssetDatabase.IsValidFolder(OutputRoot))
                AssetDatabase.DeleteAsset(OutputRoot);
        }
    }
}
