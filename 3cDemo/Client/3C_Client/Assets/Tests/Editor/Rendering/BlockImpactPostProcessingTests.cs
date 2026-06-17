using System.IO;
using NUnit.Framework;
using ThirdPersonRendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ThirdPersonRendering.Tests
{
    public sealed class BlockImpactPostProcessingTests
    {
        const string HighFidelityRendererPath = "Assets/Settings/URP-HighFidelity-Renderer.asset";
        const string BalancedRendererPath = "Assets/Settings/URP-Balanced-Renderer.asset";
        const string PerformantRendererPath = "Assets/Settings/URP-Performant-Renderer.asset";
        const string ShaderPath = "Assets/Shader/PostProcessing/BlockImpact/BlockImpact.shader";
        const string FeatureScriptGuid = "e18b08e353f34dbd88e84cdb1ef3189f";
        const string ShaderGuid = "a05ef7bd7ed24ab28fa86e457398bf34";

        [SetUp]
        public void ResetPulse()
        {
            BlockImpactPostProcessPulse.Reset();
        }

        [Test]
        public void DefaultSettingsDoNotActivateBlockImpact()
        {
            Assert.False(BlockImpactPostProcessSettings.Disabled.IsActive);
        }

        [Test]
        public void PositiveGlobalIntensityActivatesBlockImpact()
        {
            BlockImpactPostProcessSettings settings = CreateActiveSettings();

            Assert.True(settings.IsActive);
        }

        [Test]
        public void SettingsClampToSafeRanges()
        {
            BlockImpactPostProcessSettings settings = new BlockImpactPostProcessSettings(
                999f,
                999f,
                999f,
                999f,
                999f,
                -5f,
                99,
                999f,
                -5f,
                999f);

            Assert.AreEqual(BlockImpactPostProcessSettings.MaxGlobalIntensity, settings.GlobalIntensity);
            Assert.AreEqual(BlockImpactPostProcessSettings.MaxFlashIntensity, settings.FlashIntensity);
            Assert.AreEqual(BlockImpactPostProcessSettings.MaxRadialStrength, settings.RadialStrength);
            Assert.AreEqual(BlockImpactPostProcessSettings.MaxStreakIntensity, settings.StreakIntensity);
            Assert.AreEqual(BlockImpactPostProcessSettings.MaxChromaticStrength, settings.ChromaticStrength);
            Assert.AreEqual(BlockImpactPostProcessSettings.MinRadius, settings.Radius);
            Assert.AreEqual(BlockImpactPostProcessSettings.MaxSampleCount, settings.SampleCount);
            Assert.AreEqual(BlockImpactPostProcessSettings.MaxStreakLength, settings.StreakLength);
            Assert.AreEqual(BlockImpactPostProcessSettings.MinStreakThickness, settings.StreakThickness);
            Assert.AreEqual(BlockImpactPostProcessSettings.MaxStreakSoftness, settings.StreakSoftness);
        }

        [Test]
        public void SettingsBuildShaderParamsFromPulse()
        {
            BlockImpactPostProcessSettings settings = new BlockImpactPostProcessSettings(0.5f, 1f, 0.25f, 0.75f, 0.01f, 0.8f, 8, 1.4f, 0.03f, 0.4f);
            BlockImpactPostProcessPulseState pulse = new BlockImpactPostProcessPulseState(
                Vector2.one * 0.25f,
                2f,
                1f,
                0.5f,
                1.1f,
                0.02f,
                0.8f,
                Color.green,
                0.2f,
                0.3f,
                0.4f,
                0.5f);

            Assert.AreEqual(new Vector4(0.25f, 0.25f, 0.5f, 0.8f), settings.BuildPrimaryParams(pulse));
            AssertVector(new Vector4(0.2f, 0.075f, 0.3f, 0.005f), settings.BuildEffectParams(pulse));
            Assert.AreEqual(new Vector4(1.1f, 0.02f, 0.8f, 0.5f), settings.BuildStreakParams(pulse));
        }

        [Test]
        public void RuntimePulseSubmitsAndFades()
        {
            BlockImpactPostProcessPulse.Submit(new Vector2(2f, -1f), 0.8f, 0.5f, 1.3f, 0.025f, 0.6f, Color.red, 1f, 0.5f, 1f, 0.25f);

            Assert.True(BlockImpactPostProcessPulse.Current.IsActive);
            Assert.AreEqual(Vector2.right, BlockImpactPostProcessPulse.Current.Center);
            Assert.AreEqual(0.8f, BlockImpactPostProcessPulse.Current.Intensity);
            Assert.AreEqual(1.3f, BlockImpactPostProcessPulse.Current.StreakLength);
            Assert.AreEqual(0.025f, BlockImpactPostProcessPulse.Current.StreakThickness);
            Assert.AreEqual(0.6f, BlockImpactPostProcessPulse.Current.StreakSoftness);
            Assert.AreEqual(Color.red, BlockImpactPostProcessPulse.Current.StreakColor);
            Assert.AreEqual(0.5f, BlockImpactPostProcessPulse.Current.RadialWeight);
            Assert.AreEqual(0.25f, BlockImpactPostProcessPulse.Current.ChromaticWeight);

            BlockImpactPostProcessPulse.Tick(0.25f);

            Assert.AreEqual(0.5f, BlockImpactPostProcessPulse.Current.Fade);

            BlockImpactPostProcessPulse.Tick(0.25f);

            Assert.False(BlockImpactPostProcessPulse.Current.IsActive);
        }

        [Test]
        public void RuntimePulseKeepsAmplifiedLayerWeights()
        {
            BlockImpactPostProcessPulse.Submit(Vector2.one * 0.5f, 1f, 0.3f, 1.2f, 0.018f, 0.7f, Color.white, 2.2f, 2.4f, 2.6f, 2.8f);

            Assert.AreEqual(2.2f, BlockImpactPostProcessPulse.Current.FlashWeight);
            Assert.AreEqual(2.4f, BlockImpactPostProcessPulse.Current.RadialWeight);
            Assert.AreEqual(2.6f, BlockImpactPostProcessPulse.Current.StreakWeight);
            Assert.AreEqual(2.8f, BlockImpactPostProcessPulse.Current.ChromaticWeight);
        }

        [Test]
        public void RuntimePulseKeepsHigherActiveIntensity()
        {
            BlockImpactPostProcessPulse.Submit(Vector2.one * 0.5f, 1.2f, 0.5f);
            BlockImpactPostProcessPulse.Submit(Vector2.one * 0.25f, 0.4f, 0.5f);

            Assert.AreEqual(1.2f, BlockImpactPostProcessPulse.Current.Intensity);
            Assert.AreEqual(Vector2.one * 0.25f, BlockImpactPostProcessPulse.Current.Center);
        }

        [Test]
        public void VolumeDefaultDoesNotActivateBlockImpact()
        {
            BlockImpactPostProcess blockImpact = ScriptableObject.CreateInstance<BlockImpactPostProcess>();
            try
            {
                Assert.False(blockImpact.IsActive());
            }
            finally
            {
                Object.DestroyImmediate(blockImpact);
            }
        }

        [Test]
        public void VolumePositiveIntensityActivatesBlockImpact()
        {
            BlockImpactPostProcess blockImpact = ScriptableObject.CreateInstance<BlockImpactPostProcess>();
            try
            {
                blockImpact.globalIntensity.value = 0.4f;

                Assert.True(blockImpact.IsActive());
                Assert.False(blockImpact.IsTileCompatible());
            }
            finally
            {
                Object.DestroyImmediate(blockImpact);
            }
        }

        [Test]
        public void RendererFeatureWithoutShaderCannotRender()
        {
            BlockImpactPostProcessRendererFeature feature = ScriptableObject.CreateInstance<BlockImpactPostProcessRendererFeature>();
            try
            {
                feature.Create();

                Assert.False(feature.HasMaterial);
                Assert.False(feature.HasPass);
                Assert.False(feature.CanRender(CreateActiveSettings(), CreateActivePulse()));
            }
            finally
            {
                Object.DestroyImmediate(feature);
            }
        }

        [Test]
        public void RendererFeatureWithShaderCanRenderActiveSettingsAndPulse()
        {
            BlockImpactPostProcessRendererFeature feature = ScriptableObject.CreateInstance<BlockImpactPostProcessRendererFeature>();
            try
            {
                feature.Shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
                feature.Create();

                Assert.NotNull(feature.Shader);
                Assert.True(feature.HasMaterial);
                Assert.True(feature.HasPass);
                Assert.True(feature.CanRender(CreateActiveSettings(), CreateActivePulse()));
            }
            finally
            {
                Object.DestroyImmediate(feature);
            }
        }

        [Test]
        public void RenderPassRequiresCameraColorInput()
        {
            Assert.AreEqual(ScriptableRenderPassInput.Color, BlockImpactPostProcessRenderPass.RequiredInputs);
        }

        [Test]
        public void QualityRenderersReferenceBlockImpactFeatureAndShader()
        {
            AssertRendererReferencesBlockImpact(HighFidelityRendererPath);
            AssertRendererReferencesBlockImpact(BalancedRendererPath);
            AssertRendererReferencesBlockImpact(PerformantRendererPath);
        }

        [Test]
        public void ShaderContainsScreenStreakFlashRadialAndChromaticPaths()
        {
            string shader = ReadAssetYaml(ShaderPath);

            StringAssert.Contains("Hidden/3C/PostProcessing/BlockImpact", shader);
            StringAssert.Contains("_BlockImpactParams", shader);
            StringAssert.Contains("_BlockImpactEffectParams", shader);
            StringAssert.Contains("_BlockImpactStreakParams", shader);
            StringAssert.Contains("_BlockImpactStreakColor", shader);
            StringAssert.Contains("_BlockImpactSampleCount", shader);
            StringAssert.Contains("radialStrength", shader);
            StringAssert.Contains("flashIntensity", shader);
            StringAssert.Contains("streakIntensity", shader);
            StringAssert.Contains("streakLength", shader);
            StringAssert.Contains("streakThickness", shader);
            StringAssert.Contains("streakSoftness", shader);
            StringAssert.Contains("chromaticStrength", shader);
            StringAssert.Contains("SAMPLE_TEXTURE2D_X", shader);
            StringAssert.Contains("Fallback Off", shader);
        }

        static BlockImpactPostProcessSettings CreateActiveSettings()
        {
            return new BlockImpactPostProcessSettings(0.5f, 1.1f, 0.35f, 1.1f, 0.012f, 0.85f, 8, 1.2f, 0.018f, 0.7f);
        }

        static BlockImpactPostProcessPulseState CreateActivePulse()
        {
            return new BlockImpactPostProcessPulseState(Vector2.one * 0.5f, 1f, 0.25f, 0f);
        }

        static void AssertRendererReferencesBlockImpact(string assetPath)
        {
            string yaml = ReadAssetYaml(assetPath);

            StringAssert.Contains($"guid: {FeatureScriptGuid}", yaml);
            StringAssert.Contains($"guid: {ShaderGuid}", yaml);
            StringAssert.Contains("m_Name: 3C Block Impact", yaml);
            StringAssert.Contains("injectionPoint: 550", yaml);
            StringAssert.Contains("0a26fefa435a", yaml);
        }

        static void AssertVector(Vector4 expected, Vector4 actual)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.0001f));
            Assert.That(actual.w, Is.EqualTo(expected.w).Within(0.0001f));
        }

        static string ReadAssetYaml(string assetPath)
        {
            string fullPath = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
            return File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
        }
    }
}
