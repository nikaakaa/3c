using System.IO;
using NUnit.Framework;
using ThirdPersonRendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ThirdPersonRendering.Tests
{
    public sealed class GlitchTests
    {
        const string HighFidelityRendererPath = "Assets/Settings/URP-HighFidelity-Renderer.asset";
        const string BalancedRendererPath = "Assets/Settings/URP-Balanced-Renderer.asset";
        const string PerformantRendererPath = "Assets/Settings/URP-Performant-Renderer.asset";
        const string GlobalSettingsPath = "Assets/Settings/UniversalRenderPipelineGlobalSettings.asset";
        const string ShaderPath = "Assets/Shader/PostProcessing/Glitch/Glitch.shader";
        const string FeatureScriptGuid = "17863a8457c84489a443192465eef6bc";
        const string ShaderGuid = "f3ca4285bcfb4073a1d0d7edfe0cba29";
        const string MaskShaderGuid = "3ad786e179c1482da3dcc22ec570e72e";

        [Test]
        public void DefaultSettingsDoNotActivateGlitch()
        {
            Assert.False(GlitchSettings.Disabled.IsActive);
        }

        [Test]
        public void PositiveIntensityActivatesGlitch()
        {
            GlitchSettings settings = new GlitchSettings(0.2f, 48f, 0.02f, 0.01f, 0.35f, 24f, false, 1f, 0.04f);

            Assert.True(settings.IsActive);
        }

        [Test]
        public void SettingsClampToSafeRanges()
        {
            GlitchSettings settings = new GlitchSettings(9f, -10f, 9f, 9f, 9f, 999f, true, 9f, 9f);

            Assert.AreEqual(GlitchSettings.MaxIntensity, settings.Intensity);
            Assert.AreEqual(GlitchSettings.MinBlockSize, settings.BlockSize);
            Assert.AreEqual(GlitchSettings.MaxHorizontalJitter, settings.HorizontalJitter);
            Assert.AreEqual(GlitchSettings.MaxRgbSplit, settings.RgbSplit);
            Assert.AreEqual(GlitchSettings.MaxScanLineIntensity, settings.ScanLineIntensity);
            Assert.AreEqual(GlitchSettings.MaxSpeed, settings.Speed);
            Assert.True(settings.UseTargetMask);
            Assert.AreEqual(GlitchSettings.MaxMaskInfluence, settings.MaskInfluence);
            Assert.AreEqual(GlitchSettings.MaxMaskExpansion, settings.MaskExpansion);
            Assert.AreEqual(GlitchMode.DigitalGlitch, settings.Mode);
        }

        [Test]
        public void SlashTearSettingsClampToSafeRanges()
        {
            GlitchSettings settings = new GlitchSettings(
                GlitchMode.SlashTear,
                0.5f,
                48f,
                0.02f,
                0.01f,
                0.35f,
                24f,
                false,
                1f,
                0.04f,
                9999f,
                999f,
                999f,
                999f,
                999f);

            Assert.AreEqual(GlitchMode.SlashTear, settings.Mode);
            Assert.AreEqual(GlitchSettings.MaxSlashSliceDensity, settings.SlashSliceDensity);
            Assert.AreEqual(GlitchSettings.MaxSlashSmearWidth, settings.SlashSmearWidth);
            Assert.AreEqual(GlitchSettings.MaxSlashHighlightStretch, settings.SlashHighlightStretch);
            Assert.AreEqual(GlitchSettings.MaxSlashDirection, settings.SlashDirection);
            Assert.AreEqual(GlitchSettings.MaxSlashBlend, settings.SlashBlend);
        }

        [Test]
        public void SlashTearSettingsNormalizeAsDistinctMode()
        {
            GlitchSettings settings = new GlitchSettings(
                GlitchMode.SlashTear,
                0.5f,
                48f,
                0.02f,
                0.01f,
                0.35f,
                24f,
                true,
                1f,
                0.04f,
                420f,
                0.06f,
                1.2f,
                -1f,
                0.75f);

            Assert.AreEqual(GlitchMode.SlashTear, settings.Mode);
            Assert.AreEqual(420f, settings.SlashSliceDensity);
            Assert.AreEqual(0.06f, settings.SlashSmearWidth);
            Assert.AreEqual(1.2f, settings.SlashHighlightStretch);
            Assert.AreEqual(-1f, settings.SlashDirection);
            Assert.AreEqual(0.75f, settings.SlashBlend);
            Assert.AreEqual((float)GlitchMode.SlashTear, settings.ModeParams.x);
            Assert.AreEqual(0.75f, settings.ModeParams.y);
        }

        [Test]
        public void InvalidModeFallsBackToDigitalGlitch()
        {
            GlitchSettings settings = new GlitchSettings(
                (GlitchMode)99,
                0.5f,
                48f,
                0.02f,
                0.01f,
                0.35f,
                24f,
                false,
                1f,
                0.04f,
                360f,
                0.04f,
                0.8f,
                1f,
                1f);

            Assert.AreEqual(GlitchMode.DigitalGlitch, settings.Mode);
        }

        [Test]
        public void VolumeDefaultDisablesTargetMask()
        {
            Glitch glitch = ScriptableObject.CreateInstance<Glitch>();
            try
            {
                Assert.False(glitch.NormalizedSettings.UseTargetMask);
                Assert.AreEqual(GlitchMode.DigitalGlitch, glitch.NormalizedSettings.Mode);
            }
            finally
            {
                Object.DestroyImmediate(glitch);
            }
        }

        [Test]
        public void VolumeDefaultDoesNotActivateGlitch()
        {
            Glitch glitch = ScriptableObject.CreateInstance<Glitch>();
            try
            {
                Assert.False(glitch.IsActive());
            }
            finally
            {
                Object.DestroyImmediate(glitch);
            }
        }

        [Test]
        public void VolumePositiveIntensityActivatesGlitch()
        {
            Glitch glitch = ScriptableObject.CreateInstance<Glitch>();
            try
            {
                glitch.intensity.value = 0.4f;

                Assert.True(glitch.IsActive());
                Assert.False(glitch.IsTileCompatible());
            }
            finally
            {
                Object.DestroyImmediate(glitch);
            }
        }

        [Test]
        public void RendererFeatureWithoutShaderCannotRender()
        {
            GlitchRendererFeature feature = ScriptableObject.CreateInstance<GlitchRendererFeature>();
            try
            {
                feature.Create();

                Assert.False(feature.HasMaterial);
                Assert.False(feature.HasPass);
                Assert.False(feature.CanRender(new GlitchSettings(0.5f, 48f, 0.02f, 0.01f, 0.35f, 24f, false, 1f, 0.04f)));
            }
            finally
            {
                Object.DestroyImmediate(feature);
            }
        }

        [Test]
        public void RendererFeatureDefaultsToGlitchTargetRenderingLayer()
        {
            GlitchRendererFeature feature = ScriptableObject.CreateInstance<GlitchRendererFeature>();
            try
            {
                Assert.AreEqual(2u, feature.TargetRenderingLayerMask);
            }
            finally
            {
                Object.DestroyImmediate(feature);
            }
        }

        [Test]
        public void MaskDescriptorKeepsSingleSampleTextureForGlitchSampling()
        {
            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(128, 64)
            {
                depthBufferBits = 24,
                msaaSamples = 4,
                graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm
            };

            RenderTextureDescriptor maskDescriptor = GlitchMaskRenderPass.CreateMaskDescriptor(descriptor);

            Assert.AreEqual(0, maskDescriptor.depthBufferBits);
            Assert.AreEqual(1, maskDescriptor.msaaSamples);
            Assert.AreEqual(UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm, maskDescriptor.graphicsFormat);
        }


        [Test]
        public void QualityRenderersReferenceGlitchFeatureAndShader()
        {
            AssertRendererReferencesGlitch(HighFidelityRendererPath);
            AssertRendererReferencesGlitch(BalancedRendererPath);
            AssertRendererReferencesGlitch(PerformantRendererPath);
        }

        [Test]
        public void ShaderContainsSlashTearMode()
        {
            string shader = ReadAssetYaml(ShaderPath);

            StringAssert.Contains("_GlitchSlashParams", shader);
            StringAssert.Contains("_GlitchModeParams", shader);
            StringAssert.Contains("slashSliceDensity", shader);
            StringAssert.Contains("slashSmearWidth", shader);
            StringAssert.Contains("slashHighlightStretch", shader);
            StringAssert.Contains("slashModeWeight", shader);
        }

        [Test]
        public void ProjectDefinesGlitchTargetRenderingLayer()
        {
            string yaml = ReadAssetYaml(GlobalSettingsPath);

            StringAssert.Contains("- Glitch Target", yaml);
            StringAssert.Contains("lightLayerName1: Glitch Target", yaml);
        }

        static void AssertRendererReferencesGlitch(string assetPath)
        {
            string yaml = ReadAssetYaml(assetPath);

            StringAssert.Contains($"guid: {FeatureScriptGuid}", yaml);
            StringAssert.Contains($"guid: {ShaderGuid}", yaml);
            StringAssert.Contains($"guid: {MaskShaderGuid}", yaml);
            StringAssert.Contains("targetRenderingLayerMask: 2", yaml);
        }

        static string ReadAssetYaml(string assetPath)
        {
            string fullPath = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
            return File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
        }
    }
}
