using System.IO;
using NUnit.Framework;
using ThirdPersonRendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ThirdPersonRendering.Tests
{
    public sealed class BlackWhiteFlashTests
    {
        const string HighFidelityRendererPath = "Assets/Settings/URP-HighFidelity-Renderer.asset";
        const string BalancedRendererPath = "Assets/Settings/URP-Balanced-Renderer.asset";
        const string PerformantRendererPath = "Assets/Settings/URP-Performant-Renderer.asset";
        const string ProfilePath = "Assets/Settings/SampleSceneProfile.asset";
        const string DefaultCurveProfilePath = "Assets/Settings/BlackWhiteFlashProfile.asset";
        const string SandboxScenePath = "Assets/Scenes/Sandbox.unity";
        const string ShaderPath = "Assets/Shader/PostProcessing/BlackWhiteFlash/BlackWhiteFlash.shader";
        const string FeatureScriptGuid = "a104c47cc4f24fdcb896006090d3ca16";
        const string VolumeScriptGuid = "607257caa3c24ecaa65bc652386eb6b2";
        const string CurveProfileScriptGuid = "2c17775e41604c898dc738505ddd256f";
        const string ControllerScriptGuid = "5e25db62ad3645929d6b4b50d4bb0e56";
        const string DefaultCurveProfileGuid = "1d1551a62e2b442c87bb8f5e30fc3b4f";
        const string ShaderGuid = "9824e2c884d842149a735f0eca663321";

        [Test]
        public void DefaultSettingsDoNotActivateBlackWhiteFlash()
        {
            Assert.False(BlackWhiteFlashSettings.Disabled.IsActive);
        }

        [Test]
        public void PositiveIntensityActivatesBlackWhiteFlash()
        {
            BlackWhiteFlashSettings settings = CreateActiveSettings(BlackWhiteFlashMode.FullScreen);

            Assert.True(settings.IsActive);
        }

        [Test]
        public void SettingsClampToSafeRanges()
        {
            BlackWhiteFlashSettings settings = new BlackWhiteFlashSettings(
                (BlackWhiteFlashMode)99,
                9f,
                -1f,
                -5f,
                99f,
                99f,
                99f,
                new Vector2(-2f, 3f),
                -5f,
                9f);

            Assert.AreEqual(BlackWhiteFlashMode.FullScreen, settings.Mode);
            Assert.AreEqual(BlackWhiteFlashSettings.MaxIntensity, settings.Intensity);
            Assert.AreEqual(BlackWhiteFlashSettings.MinThreshold, settings.Threshold);
            Assert.AreEqual(BlackWhiteFlashSettings.MinContrast, settings.Contrast);
            Assert.AreEqual(BlackWhiteFlashSettings.MaxWhiteBoost, settings.WhiteBoost);
            Assert.AreEqual(BlackWhiteFlashSettings.MaxBlackCrush, settings.BlackCrush);
            Assert.AreEqual(BlackWhiteFlashSettings.MaxInvertAmount, settings.InvertAmount);
            Assert.AreEqual(Vector2.up, settings.Center);
            Assert.AreEqual(BlackWhiteFlashSettings.MinRadius, settings.Radius);
            Assert.AreEqual(BlackWhiteFlashSettings.MaxSoftness, settings.Softness);
        }

        [Test]
        public void InvalidModeFallsBackToFullScreen()
        {
            BlackWhiteFlashSettings settings = new BlackWhiteFlashSettings(
                (BlackWhiteFlashMode)99,
                0.5f,
                0.5f,
                8f,
                1f,
                0.45f,
                0f,
                Vector2.one * 0.5f,
                0.55f,
                0.25f);

            Assert.AreEqual(BlackWhiteFlashMode.FullScreen, settings.Mode);
        }

        [Test]
        public void FullScreenShaderParamsExposeToneAndMode()
        {
            BlackWhiteFlashSettings settings = new BlackWhiteFlashSettings(
                BlackWhiteFlashMode.FullScreen,
                0.6f,
                0.4f,
                10f,
                1.2f,
                0.3f,
                0.7f,
                new Vector2(0.25f, 0.75f),
                0.8f,
                0.2f);

            Assert.AreEqual(new Vector4(0.6f, 0.4f, 10f, 0.7f), settings.ToneParams);
            Assert.AreEqual(new Vector4(0.25f, 0.75f, 0.8f, 0.2f), settings.RangeParams);
            Assert.AreEqual(new Vector4(1.2f, 0.3f, 0f, 0f), settings.StyleParams);
        }

        [Test]
        public void RadialImpactShaderParamsExposeMode()
        {
            BlackWhiteFlashSettings settings = CreateActiveSettings(BlackWhiteFlashMode.RadialImpact);

            Assert.AreEqual(BlackWhiteFlashMode.RadialImpact, settings.Mode);
            Assert.AreEqual((float)BlackWhiteFlashMode.RadialImpact, settings.StyleParams.z);
        }

        [Test]
        public void VolumeDefaultDoesNotActivateBlackWhiteFlash()
        {
            BlackWhiteFlash blackWhiteFlash = ScriptableObject.CreateInstance<BlackWhiteFlash>();
            try
            {
                Assert.False(blackWhiteFlash.IsActive());
            }
            finally
            {
                Object.DestroyImmediate(blackWhiteFlash);
            }
        }

        [Test]
        public void VolumePositiveIntensityActivatesBlackWhiteFlash()
        {
            BlackWhiteFlash blackWhiteFlash = ScriptableObject.CreateInstance<BlackWhiteFlash>();
            try
            {
                blackWhiteFlash.intensity.value = 0.4f;

                Assert.True(blackWhiteFlash.IsActive());
                Assert.False(blackWhiteFlash.IsTileCompatible());
            }
            finally
            {
                Object.DestroyImmediate(blackWhiteFlash);
            }
        }

        [Test]
        public void RendererFeatureWithoutShaderCannotRender()
        {
            BlackWhiteFlashRendererFeature feature = ScriptableObject.CreateInstance<BlackWhiteFlashRendererFeature>();
            try
            {
                feature.Create();

                Assert.False(feature.HasMaterial);
                Assert.False(feature.HasPass);
                Assert.False(feature.CanRender(CreateActiveSettings(BlackWhiteFlashMode.FullScreen)));
                Assert.False(feature.ShouldEnqueue());
            }
            finally
            {
                Object.DestroyImmediate(feature);
            }
        }

        [Test]
        public void RendererFeatureWithShaderCanRenderActiveSettings()
        {
            BlackWhiteFlashRendererFeature feature = ScriptableObject.CreateInstance<BlackWhiteFlashRendererFeature>();
            try
            {
                feature.Shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
                feature.Create();

                Assert.NotNull(feature.Shader);
                Assert.True(feature.HasMaterial);
                Assert.True(feature.HasPass);
                Assert.True(feature.CanRender(CreateActiveSettings(BlackWhiteFlashMode.FullScreen)));
            }
            finally
            {
                Object.DestroyImmediate(feature);
            }
        }

        [Test]
        public void RenderPassRequiresCameraColorInput()
        {
            Assert.AreEqual(ScriptableRenderPassInput.Color, BlackWhiteFlashRenderPass.RequiredInputs);
        }

        [Test]
        public void QualityRenderersReferenceBlackWhiteFlashFeatureAndShader()
        {
            AssertRendererReferencesBlackWhiteFlash(HighFidelityRendererPath);
            AssertRendererReferencesBlackWhiteFlash(BalancedRendererPath);
            AssertRendererReferencesBlackWhiteFlash(PerformantRendererPath);
        }

        [Test]
        public void ShaderContainsBlackWhiteAndRadialPaths()
        {
            string shader = ReadAssetYaml(ShaderPath);

            StringAssert.Contains("Hidden/3C/PostProcessing/BlackWhiteFlash", shader);
            StringAssert.Contains("_BlackWhiteFlashToneParams", shader);
            StringAssert.Contains("_BlackWhiteFlashRangeParams", shader);
            StringAssert.Contains("_BlackWhiteFlashStyleParams", shader);
            StringAssert.Contains("dot(source.rgb", shader);
            StringAssert.Contains("threshold", shader);
            StringAssert.Contains("contrast", shader);
            StringAssert.Contains("whiteBoost", shader);
            StringAssert.Contains("blackCrush", shader);
            StringAssert.Contains("invertAmount", shader);
            StringAssert.Contains("radialMask", shader);
            StringAssert.Contains("SAMPLE_TEXTURE2D_X", shader);
            StringAssert.Contains("Fallback Off", shader);
        }

        [Test]
        public void SampleSceneProfileContainsBlackWhiteFlashVolume()
        {
            string yaml = ReadAssetYaml(ProfilePath);

            StringAssert.Contains($"guid: {VolumeScriptGuid}", yaml);
            StringAssert.Contains("m_Name: Black White Flash", yaml);
            StringAssert.Contains("mode:", yaml);
            StringAssert.Contains("intensity:", yaml);
            StringAssert.Contains("threshold:", yaml);
            StringAssert.Contains("contrast:", yaml);
            StringAssert.Contains("whiteBoost:", yaml);
            StringAssert.Contains("blackCrush:", yaml);
            StringAssert.Contains("invertAmount:", yaml);
            StringAssert.Contains("center:", yaml);
            StringAssert.Contains("radius:", yaml);
            StringAssert.Contains("softness:", yaml);
        }

        [Test]
        public void DefaultCurveProfileStartsActiveAndEndsDisabled()
        {
            BlackWhiteFlashProfile profile = ScriptableObject.CreateInstance<BlackWhiteFlashProfile>();
            try
            {
                BlackWhiteFlashSettings start = profile.Evaluate(0f);
                BlackWhiteFlashSettings end = profile.Evaluate(1f);

                Assert.True(profile.HasValidCurves);
                Assert.AreEqual(BlackWhiteFlashMode.RadialImpact, start.Mode);
                Assert.True(start.IsActive);
                Assert.False(end.IsActive);
                Assert.Greater(end.Radius, start.Radius);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void CurveProfileClampsSampledSettings()
        {
            BlackWhiteFlashProfile profile = ScriptableObject.CreateInstance<BlackWhiteFlashProfile>();
            try
            {
                SerializedObject serializedProfile = new SerializedObject(profile);
                serializedProfile.FindProperty("threshold").floatValue = -1f;
                serializedProfile.FindProperty("contrast").floatValue = -5f;
                serializedProfile.FindProperty("whiteBoost").floatValue = 99f;
                serializedProfile.FindProperty("blackCrush").floatValue = 99f;
                serializedProfile.FindProperty("invertAmount").floatValue = 99f;
                serializedProfile.FindProperty("baseRadius").floatValue = -5f;
                serializedProfile.FindProperty("peakRadius").floatValue = 99f;
                serializedProfile.FindProperty("softness").floatValue = 99f;
                serializedProfile.ApplyModifiedPropertiesWithoutUndo();

                BlackWhiteFlashSettings settings = profile.Evaluate(new Vector2(-2f, 3f), 0.5f, 9f);

                Assert.AreEqual(BlackWhiteFlashSettings.MaxIntensity, settings.Intensity);
                Assert.AreEqual(BlackWhiteFlashSettings.MinThreshold, settings.Threshold);
                Assert.AreEqual(BlackWhiteFlashSettings.MinContrast, settings.Contrast);
                Assert.AreEqual(BlackWhiteFlashSettings.MaxWhiteBoost, settings.WhiteBoost);
                Assert.AreEqual(BlackWhiteFlashSettings.MaxBlackCrush, settings.BlackCrush);
                Assert.LessOrEqual(settings.InvertAmount, BlackWhiteFlashSettings.MaxInvertAmount);
                Assert.AreEqual(Vector2.up, settings.Center);
                Assert.GreaterOrEqual(settings.Radius, BlackWhiteFlashSettings.MinRadius);
                Assert.LessOrEqual(settings.Radius, BlackWhiteFlashSettings.MaxRadius);
                Assert.AreEqual(BlackWhiteFlashSettings.MaxSoftness, settings.Softness);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void ControllerWritesCurveSampleIntoVolume()
        {
            CreateControllerFixture(out GameObject gameObject, out Volume volume, out BlackWhiteFlashProfile profile, out BlackWhiteFlashController controller);
            try
            {
                controller.Play(new Vector2(0.25f, 0.75f));

                Assert.True(controller.IsPlaying);
                Assert.True(volume.profile.TryGet(out BlackWhiteFlash blackWhiteFlash));
                Assert.True(blackWhiteFlash.active);
                Assert.Greater(blackWhiteFlash.intensity.value, 0f);
                Assert.AreEqual(new Vector2(0.25f, 0.75f), blackWhiteFlash.center.value);
                Assert.AreEqual(profile.Mode, blackWhiteFlash.mode.value);
            }
            finally
            {
                Object.DestroyImmediate(volume.sharedProfile);
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void ControllerRestoresIntensityWhenPlaybackEnds()
        {
            CreateControllerFixture(out GameObject gameObject, out Volume volume, out BlackWhiteFlashProfile profile, out BlackWhiteFlashController controller);
            try
            {
                controller.PlayDefault();
                controller.Tick(profile.Duration + 0.01f);

                Assert.False(controller.IsPlaying);
                Assert.True(volume.profile.TryGet(out BlackWhiteFlash blackWhiteFlash));
                Assert.False(blackWhiteFlash.active);
                Assert.AreEqual(0f, blackWhiteFlash.intensity.value);
            }
            finally
            {
                Object.DestroyImmediate(volume.sharedProfile);
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void DefaultCurveProfileAssetIsConfigured()
        {
            BlackWhiteFlashProfile profile = AssetDatabase.LoadAssetAtPath<BlackWhiteFlashProfile>(DefaultCurveProfilePath);
            string yaml = ReadAssetYaml(DefaultCurveProfilePath);

            Assert.NotNull(profile);
            Assert.True(profile.HasValidCurves);
            Assert.AreEqual(BlackWhiteFlashMode.RadialImpact, profile.Mode);
            Assert.Greater(profile.Duration, 0f);
            StringAssert.Contains($"guid: {CurveProfileScriptGuid}", yaml);
            StringAssert.Contains("intensityCurve:", yaml);
            StringAssert.Contains("radiusCurve:", yaml);
            StringAssert.Contains("invertCurve:", yaml);
        }

        [Test]
        public void SandboxSceneContainsCurveController()
        {
            string yaml = ReadAssetYaml(SandboxScenePath);

            StringAssert.Contains("m_Name: Global Volume", yaml);
            StringAssert.Contains($"guid: {ControllerScriptGuid}", yaml);
            StringAssert.Contains($"guid: {DefaultCurveProfileGuid}", yaml);
            StringAssert.Contains("targetVolume: {fileID: 832575518}", yaml);
            StringAssert.Contains("playOnEnable: 0", yaml);
            StringAssert.Contains("restoreIntensityOnStop: 1", yaml);
        }

        static BlackWhiteFlashSettings CreateActiveSettings(BlackWhiteFlashMode mode)
        {
            return new BlackWhiteFlashSettings(
                mode,
                0.5f,
                0.5f,
                8f,
                1.1f,
                0.45f,
                0f,
                Vector2.one * 0.5f,
                0.55f,
                0.25f);
        }

        static void CreateControllerFixture(
            out GameObject gameObject,
            out Volume volume,
            out BlackWhiteFlashProfile profile,
            out BlackWhiteFlashController controller)
        {
            gameObject = new GameObject("BlackWhiteFlashControllerTest");
            volume = gameObject.AddComponent<Volume>();
            VolumeProfile volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            volumeProfile.Add<BlackWhiteFlash>();
            volume.sharedProfile = volumeProfile;
            profile = ScriptableObject.CreateInstance<BlackWhiteFlashProfile>();
            controller = gameObject.AddComponent<BlackWhiteFlashController>();
            controller.TargetVolume = volume;
            controller.Profile = profile;
        }

        static void AssertRendererReferencesBlackWhiteFlash(string assetPath)
        {
            string yaml = ReadAssetYaml(assetPath);

            StringAssert.Contains($"guid: {FeatureScriptGuid}", yaml);
            StringAssert.Contains($"guid: {ShaderGuid}", yaml);
            StringAssert.Contains("m_Name: 3C Black White Flash", yaml);
            StringAssert.Contains("injectionPoint: 550", yaml);
        }

        static string ReadAssetYaml(string assetPath)
        {
            string fullPath = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
            return File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
        }
    }
}
