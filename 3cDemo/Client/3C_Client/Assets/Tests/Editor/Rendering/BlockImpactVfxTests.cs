using System.IO;
using System.Reflection;
using NUnit.Framework;
using ThirdPersonRendering;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonRendering.Tests
{
    public sealed class BlockImpactVfxTests
    {
        const string ProfilePath = "Assets/Settings/BlockImpact/BlockImpactVfxProfile.asset";
        const string PrefabPath = "Assets/Prefabs/Rendering/BlockImpactVfx.prefab";
        const string AdditiveShaderPath = "Assets/Shader/VFX/BlockImpact/BlockImpactAdditive.shader";
        const string SparkShaderPath = "Assets/Shader/VFX/BlockImpact/BlockImpactSpark.shader";

        [Test]
        public void RequestDefaultContainsPlayableValues()
        {
            BlockImpactVfxRequest request = BlockImpactVfxRequest.Default;

            Assert.AreEqual(Vector3.zero, request.WorldHitPoint);
            Assert.AreEqual(Vector3.forward, request.AttackDirection);
            Assert.AreEqual(Vector3.back, request.HitNormal);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), request.ScreenCenter);
            Assert.AreEqual(1f, request.Intensity);
            Assert.AreEqual(0.28f, request.Duration);
            Assert.True(request.FlashEnabled);
            Assert.True(request.SparksEnabled);
            Assert.True(request.ArcsEnabled);
            Assert.True(request.StreakEnabled);
            Assert.True(request.ScreenImpactEnabled);
        }

        [Test]
        public void RequestClampsIntensityDurationAndScreenCenter()
        {
            BlockImpactVfxRequest request = new BlockImpactVfxRequest(
                Vector3.one,
                Vector3.forward,
                new Vector2(-2f, 3f),
                999f,
                -9f,
                7,
                true,
                true,
                true,
                true,
                true);

            Assert.AreEqual(BlockImpactVfxRequest.MaxIntensity, request.Intensity);
            Assert.AreEqual(BlockImpactVfxRequest.MinDuration, request.Duration);
            Assert.AreEqual(Vector2.up, request.ScreenCenter);
            Assert.AreEqual(7, request.RandomSeed);
        }

        [Test]
        public void RequestNormalizesAttackDirection()
        {
            BlockImpactVfxRequest request = new BlockImpactVfxRequest(
                Vector3.zero,
                new Vector3(0f, 0f, 8f),
                Vector2.one * 0.5f,
                1f,
                0.2f,
                0,
                true,
                true,
                true,
                true,
                true);

            Assert.AreEqual(Vector3.forward, request.AttackDirection);
            Assert.AreEqual(Vector3.forward, BlockImpactVfxRequest.NormalizeDirection(Vector3.zero));
        }

        [Test]
        public void RequestDoesNotContainUnityObjectReferences()
        {
            foreach (FieldInfo field in typeof(BlockImpactVfxRequest).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                Assert.False(typeof(Object).IsAssignableFrom(field.FieldType), field.Name);

            foreach (PropertyInfo property in typeof(BlockImpactVfxRequest).GetProperties(BindingFlags.Instance | BindingFlags.Public))
                Assert.False(typeof(Object).IsAssignableFrom(property.PropertyType), property.Name);
        }

        [Test]
        public void EmptyProfileReportsMissingRequiredTextures()
        {
            BlockImpactVfxProfile profile = ScriptableObject.CreateInstance<BlockImpactVfxProfile>();
            try
            {
                Assert.False(profile.HasRequiredTextures);
                Assert.False(profile.ValidateRequiredTextures(out string message));
                StringAssert.Contains("爆闪贴图", message);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void ProfileOnValidateClampsUnsafeValues()
        {
            BlockImpactVfxProfile profile = ScriptableObject.CreateInstance<BlockImpactVfxProfile>();
            try
            {
                SerializedObject serializedObject = new SerializedObject(profile);
                serializedObject.FindProperty("hdrIntensity").floatValue = 999f;
                serializedObject.FindProperty("flashSoftness").floatValue = -1f;
                serializedObject.FindProperty("duration").floatValue = -5f;
                serializedObject.FindProperty("sparkCount").intValue = 999;
                serializedObject.FindProperty("sparkSpeed").floatValue = -2f;
                serializedObject.FindProperty("sparkLifetime").floatValue = -1f;
                serializedObject.FindProperty("sparkConeAngle").floatValue = 999f;
                serializedObject.FindProperty("sparkVelocityScale").floatValue = 999f;
                serializedObject.FindProperty("sparkLengthScale").floatValue = 999f;
                serializedObject.FindProperty("sparkTrailLifetime").floatValue = -1f;
                serializedObject.FindProperty("sparkTrailWidth").floatValue = -1f;
                serializedObject.FindProperty("sparkGravityModifier").floatValue = 99f;
                serializedObject.FindProperty("sparkVelocityDampen").floatValue = 99f;
                serializedObject.FindProperty("flashScale").vector2Value = new Vector2(-1f, 99f);
                serializedObject.FindProperty("screenStreakLength").floatValue = 999f;
                serializedObject.FindProperty("screenStreakThickness").floatValue = -1f;
                serializedObject.FindProperty("screenStreakSoftness").floatValue = 999f;
                serializedObject.FindProperty("screenFlashWeight").floatValue = 99f;
                serializedObject.FindProperty("screenRadialWeight").floatValue = 99f;
                serializedObject.FindProperty("screenStreakWeight").floatValue = 99f;
                serializedObject.FindProperty("screenChromaticWeight").floatValue = 99f;
                serializedObject.FindProperty("screenImpactStrength").floatValue = 99f;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

                typeof(BlockImpactVfxProfile)
                    .GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(profile, null);

                Assert.AreEqual(BlockImpactVfxProfile.MaxHdrIntensity, profile.HdrIntensity);
                Assert.AreEqual(0.001f, profile.FlashSoftness);
                Assert.AreEqual(BlockImpactVfxProfile.MinDuration, profile.Duration);
                Assert.AreEqual(BlockImpactVfxProfile.MaxSparkCount, profile.SparkCount);
                Assert.AreEqual(BlockImpactVfxProfile.MinSparkSpeed, profile.SparkSpeed);
                Assert.AreEqual(BlockImpactVfxProfile.MinSparkLifetime, profile.SparkLifetime);
                Assert.AreEqual(BlockImpactVfxProfile.MaxSparkAngle, profile.SparkConeAngle);
                Assert.AreEqual(BlockImpactVfxProfile.MaxSparkStretch, profile.SparkVelocityScale);
                Assert.AreEqual(BlockImpactVfxProfile.MaxSparkStretch, profile.SparkLengthScale);
                Assert.AreEqual(BlockImpactVfxProfile.MinSparkLifetime, profile.SparkTrailLifetime);
                Assert.AreEqual(BlockImpactVfxProfile.MinTrailWidth, profile.SparkTrailWidth);
                Assert.AreEqual(BlockImpactVfxProfile.MaxSparkGravity, profile.SparkGravityModifier);
                Assert.AreEqual(BlockImpactVfxProfile.MaxSparkDampen, profile.SparkVelocityDampen);
                Assert.AreEqual(new Vector2(BlockImpactVfxProfile.MinLayerScale, BlockImpactVfxProfile.MaxLayerScale), profile.FlashScale);
                Assert.AreEqual(BlockImpactVfxProfile.MaxScreenStreakLength, profile.ScreenStreakLength);
                Assert.AreEqual(BlockImpactVfxProfile.MinScreenStreakThickness, profile.ScreenStreakThickness);
                Assert.AreEqual(BlockImpactVfxProfile.MaxScreenStreakSoftness, profile.ScreenStreakSoftness);
                Assert.AreEqual(3f, profile.ScreenFlashWeight);
                Assert.AreEqual(3f, profile.ScreenRadialWeight);
                Assert.AreEqual(3f, profile.ScreenStreakWeight);
                Assert.AreEqual(3f, profile.ScreenChromaticWeight);
                Assert.AreEqual(3f, profile.ScreenImpactStrength);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void WorldSpaceShadersExposeRequiredBlockImpactProperties()
        {
            string additive = ReadAssetYaml(AdditiveShaderPath);
            string spark = ReadAssetYaml(SparkShaderPath);

            StringAssert.Contains("Blend One One", additive);
            StringAssert.Contains("ZWrite Off", additive);
            StringAssert.Contains("_BaseMap", additive);
            StringAssert.Contains("_TintColor", additive);
            StringAssert.Contains("_Intensity", additive);
            StringAssert.Contains("_Alpha", additive);
            StringAssert.Contains("_Softness", additive);
            StringAssert.Contains("_UvScaleOffset", additive);
            StringAssert.Contains("half4 color : COLOR", spark);
            StringAssert.Contains("_TintColor.rgb * _Intensity * alpha", additive);
            StringAssert.Contains("half shape = max(max(tex.r, tex.g), tex.b) * tex.a", additive);
            StringAssert.Contains("half shape = max(max(tex.r, tex.g), tex.b) * tex.a", spark);
            StringAssert.DoesNotContain("tex.rgb * _TintColor.rgb", additive);
            StringAssert.DoesNotContain("tex.rgb * _TintColor.rgb", spark);
            StringAssert.Contains("Fallback Off", additive);
            StringAssert.Contains("Fallback Off", spark);
        }

        [Test]
        public void PlayConfiguresSparksWithLightweightPhysics()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                BlockImpactVfxController controller = instance.GetComponent<BlockImpactVfxController>();
                BlockImpactVfxProfile profile = AssetDatabase.LoadAssetAtPath<BlockImpactVfxProfile>(ProfilePath);
                BlockImpactVfxRequest request = new BlockImpactVfxRequest(
                    Vector3.zero,
                    Vector3.forward,
                    Vector2.one * 0.5f,
                    1f,
                    0.25f,
                    1,
                    true,
                    true,
                    false,
                    true,
                    true);

                controller.Play(request);

                ParticleSystem particles = instance.GetComponentInChildren<ParticleSystem>(true);
                Assert.AreEqual(profile.SparkGravityModifier, particles.main.gravityModifier.constant);
                Assert.True(particles.limitVelocityOverLifetime.enabled);
                Assert.AreEqual(profile.SparkVelocityDampen, particles.limitVelocityOverLifetime.dampen);
                Assert.IsNull(instance.GetComponentInChildren<Rigidbody>(true));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        static string ReadAssetYaml(string assetPath)
        {
            string fullPath = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
            return File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
        }
    }
}
