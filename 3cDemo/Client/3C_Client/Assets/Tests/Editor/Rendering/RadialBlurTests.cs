using System.IO;
using NUnit.Framework;
using ThirdPersonRendering;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonRendering.Tests
{
    public sealed class RadialBlurTests
    {
        const string RendererPath = "Assets/Settings/URP-HighFidelity-Renderer.asset";
        const string BalancedRendererPath = "Assets/Settings/URP-Balanced-Renderer.asset";
        const string PerformantRendererPath = "Assets/Settings/URP-Performant-Renderer.asset";
        const string FeatureScriptGuid = "98453fcc2fe1406ea127f67cc87260a1";
        const string ShaderGuid = "c3d54722b9a8424ebd99749ccbd152b1";

        [Test]
        public void DefaultSettingsDoNotActivateRadialBlur()
        {
            Assert.False(RadialBlurSettings.Disabled.IsActive);
        }

        [Test]
        public void PositiveIntensityActivatesRadialBlur()
        {
            RadialBlurSettings settings = new RadialBlurSettings(0.2f, new Vector2(0.5f, 0.5f), 0.35f, 8);

            Assert.True(settings.IsActive);
        }

        [Test]
        public void SettingsClampToSafeRanges()
        {
            RadialBlurSettings settings = new RadialBlurSettings(9f, new Vector2(-2f, 3f), -5f, 100);

            Assert.AreEqual(RadialBlurSettings.MaxIntensity, settings.Intensity);
            Assert.AreEqual(Vector2.up, settings.Center);
            Assert.AreEqual(RadialBlurSettings.MinRadius, settings.Radius);
            Assert.AreEqual(RadialBlurSettings.MaxSampleCount, settings.SampleCount);
        }

        [Test]
        public void VolumeDefaultDoesNotActivateRadialBlur()
        {
            RadialBlur radialBlur = ScriptableObject.CreateInstance<RadialBlur>();
            try
            {
                Assert.False(radialBlur.IsActive());
            }
            finally
            {
                Object.DestroyImmediate(radialBlur);
            }
        }

        [Test]
        public void VolumePositiveIntensityActivatesRadialBlur()
        {
            RadialBlur radialBlur = ScriptableObject.CreateInstance<RadialBlur>();
            try
            {
                radialBlur.intensity.value = 0.4f;

                Assert.True(radialBlur.IsActive());
                Assert.False(radialBlur.IsTileCompatible());
            }
            finally
            {
                Object.DestroyImmediate(radialBlur);
            }
        }

        [Test]
        public void RendererFeatureWithoutShaderCannotRender()
        {
            RadialBlurRendererFeature feature = ScriptableObject.CreateInstance<RadialBlurRendererFeature>();
            try
            {
                feature.Create();

                Assert.False(feature.HasMaterial);
                Assert.False(feature.HasPass);
                Assert.False(feature.CanRender(new RadialBlurSettings(0.5f, Vector2.one * 0.5f, 0.5f, 8)));
            }
            finally
            {
                Object.DestroyImmediate(feature);
            }
        }


        [Test]
        public void QualityRenderersReferenceRadialBlurFeatureAndShader()
        {
            AssertRendererReferencesRadialBlur(RendererPath, "injectionPoint: 550");
            AssertRendererReferencesRadialBlur(BalancedRendererPath, "injectionPoint: 600");
            AssertRendererReferencesRadialBlur(PerformantRendererPath, "injectionPoint: 600");
        }

        static void AssertRendererReferencesRadialBlur(string assetPath, string expectedInjectionPoint)
        {
            string yaml = ReadAssetYaml(assetPath);

            StringAssert.Contains($"guid: {FeatureScriptGuid}", yaml);
            StringAssert.Contains($"guid: {ShaderGuid}", yaml);
            StringAssert.Contains(expectedInjectionPoint, yaml);
        }

        static string ReadAssetYaml(string assetPath)
        {
            string fullPath = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
            return File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
        }
    }
}
