using System.IO;
using NUnit.Framework;
using ThirdPersonRendering;
using UnityEngine;

namespace ThirdPersonRendering.Tests
{
    public sealed class LocalHeatDistortionTests
    {
        const string HighFidelityRendererPath = "Assets/Settings/URP-HighFidelity-Renderer.asset";
        const string BalancedRendererPath = "Assets/Settings/URP-Balanced-Renderer.asset";
        const string PerformantRendererPath = "Assets/Settings/URP-Performant-Renderer.asset";
        const string ShaderPath = "Assets/Shader/PostProcessing/LocalHeatDistortion/LocalHeatDistortion.shader";
        const string FeatureScriptGuid = "3027c54bb40843fca68718d6bb1e2f88";
        const string ShaderGuid = "b4e9ce9f5575465b91b8df197b211e9f";

        [Test]
        public void DefaultSettingsDoNotActivateLocalHeatDistortion()
        {
            Assert.False(LocalHeatDistortionSettings.Disabled.IsActive);
        }

        [Test]
        public void PositiveIntensityActivatesLocalHeatDistortion()
        {
            LocalHeatDistortionSettings settings = new LocalHeatDistortionSettings(
                0.25f,
                LocalHeatDistortionMode.HeatHaze,
                12f,
                24f,
                0.02f,
                0.25f,
                1f);

            Assert.True(settings.IsActive);
        }

        [Test]
        public void PreviewDebugActivatesLocalHeatDistortion()
        {
            LocalHeatDistortionSettings settings = new LocalHeatDistortionSettings(
                0f,
                LocalHeatDistortionMode.HeatHaze,
                12f,
                24f,
                0f,
                0.25f,
                1f,
                true);

            Assert.True(settings.IsActive);
            Assert.AreEqual(1f, settings.DebugParams.x);
        }

        [Test]
        public void SettingsClampToSafeRanges()
        {
            LocalHeatDistortionSettings settings = new LocalHeatDistortionSettings(
                9f,
                (LocalHeatDistortionMode)99,
                999f,
                -8f,
                9f,
                9f,
                9f);

            Assert.AreEqual(LocalHeatDistortionSettings.MaxIntensity, settings.Intensity);
            Assert.AreEqual(LocalHeatDistortionMode.HeatHaze, settings.Mode);
            Assert.AreEqual(LocalHeatDistortionSettings.MaxSpeed, settings.Speed);
            Assert.AreEqual(LocalHeatDistortionSettings.MinNoiseScale, settings.NoiseScale);
            Assert.AreEqual(LocalHeatDistortionSettings.MaxDistortionStrength, settings.DistortionStrength);
            Assert.AreEqual(LocalHeatDistortionSettings.MaxAreaSoftness, settings.AreaSoftness);
            Assert.AreEqual(LocalHeatDistortionSettings.MaxParticleVisibility, settings.ParticleVisibility);
            Assert.False(settings.PreviewDebug);
        }

        [Test]
        public void AreaSettingsClampToSafeRanges()
        {
            LocalHeatDistortionAreaSettings area = new LocalHeatDistortionAreaSettings(
                new Vector2(-4f, 8f),
                99f,
                99f,
                0f,
                99f,
                -5f,
                (LocalHeatDistortionAreaShape)99);

            Assert.AreEqual(Vector2.up, area.Center);
            Assert.AreEqual(LocalHeatDistortionAreaSettings.MaxRadius, area.Radius);
            Assert.AreEqual(LocalHeatDistortionAreaSettings.MaxAspect, area.Aspect);
            Assert.AreEqual(LocalHeatDistortionAreaSettings.MaxSoftness, area.Softness);
            Assert.AreEqual(0f, area.SourceViewDepth);
            Assert.AreEqual(LocalHeatDistortionAreaSettings.DepthFadeDistance, area.AreaDepthParams.y);
            Assert.AreEqual(LocalHeatDistortionAreaShape.ScreenEllipse, area.Shape);
        }

        [Test]
        public void AllModesNormalizeAsDistinctCandidates()
        {
            AssertMode(LocalHeatDistortionMode.HeatHaze);
            AssertMode(LocalHeatDistortionMode.SpiralPressure);
            AssertMode(LocalHeatDistortionMode.PulseShockwave);
            AssertMode(LocalHeatDistortionMode.VerticalFlow);
        }

        [Test]
        public void DisabledAreaSourceDoesNotResolveArea()
        {
            GameObject areaObject = new GameObject("Area");
            Camera camera = CreateCamera();
            try
            {
                LocalHeatDistortionAreaSource source = areaObject.AddComponent<LocalHeatDistortionAreaSource>();
                areaObject.SetActive(false);

                Assert.False(source.TryBuildAreaSettings(camera, 0.25f, out _));
            }
            finally
            {
                Object.DestroyImmediate(camera.gameObject);
                Object.DestroyImmediate(areaObject);
            }
        }

        [Test]
        public void EnabledAreaSourceBuildsValidAreaSettings()
        {
            GameObject areaObject = new GameObject("Area");
            Camera camera = CreateCamera();
            try
            {
                areaObject.transform.position = Vector3.zero;
                LocalHeatDistortionAreaSource source = areaObject.AddComponent<LocalHeatDistortionAreaSource>();
                source.Radius = 1.5f;
                source.Aspect = 1.25f;

                Assert.True(source.TryBuildAreaSettings(camera, 0.25f, out LocalHeatDistortionAreaSettings area));
                Assert.True(area.IsValid);
                Assert.Greater(area.Radius, 0f);
                Assert.AreEqual(1.25f, area.Aspect);
                Assert.AreEqual(10f, area.SourceViewDepth);
                Assert.AreEqual(10f, area.AreaDepthParams.x);
            }
            finally
            {
                Object.DestroyImmediate(camera.gameObject);
                Object.DestroyImmediate(areaObject);
            }
        }

        [Test]
        public void VolumeDefaultDoesNotActivateLocalHeatDistortion()
        {
            LocalHeatDistortion distortion = ScriptableObject.CreateInstance<LocalHeatDistortion>();
            try
            {
                Assert.False(distortion.IsActive());
            }
            finally
            {
                Object.DestroyImmediate(distortion);
            }
        }

        [Test]
        public void VolumePositiveIntensityActivatesLocalHeatDistortion()
        {
            LocalHeatDistortion distortion = ScriptableObject.CreateInstance<LocalHeatDistortion>();
            try
            {
                distortion.intensity.value = 0.35f;

                Assert.True(distortion.IsActive());
                Assert.False(distortion.IsTileCompatible());
            }
            finally
            {
                Object.DestroyImmediate(distortion);
            }
        }

        [Test]
        public void RendererFeatureWithoutShaderCannotRender()
        {
            LocalHeatDistortionRendererFeature feature = ScriptableObject.CreateInstance<LocalHeatDistortionRendererFeature>();
            try
            {
                feature.Create();

                LocalHeatDistortionSettings settings = new LocalHeatDistortionSettings(0.3f, LocalHeatDistortionMode.HeatHaze, 12f, 24f, 0.02f, 0.25f, 1f);
                LocalHeatDistortionAreaSettings area = new LocalHeatDistortionAreaSettings(Vector2.one * 0.5f, 0.25f, 1f, 0f, 0.25f, 10f, LocalHeatDistortionAreaShape.ScreenEllipse);

                Assert.False(feature.HasMaterial);
                Assert.False(feature.HasPass);
                Assert.False(feature.CanRender(settings, area, null));
            }
            finally
            {
                Object.DestroyImmediate(feature);
            }
        }

        [Test]
        public void QualityRenderersReferenceLocalHeatDistortionFeatureAndShader()
        {
            AssertRendererReferencesLocalHeatDistortion(HighFidelityRendererPath);
            AssertRendererReferencesLocalHeatDistortion(BalancedRendererPath);
            AssertRendererReferencesLocalHeatDistortion(PerformantRendererPath);
        }

        [Test]
        public void RenderPassRequiresCameraDepthInput()
        {
            Assert.AreEqual(
                UnityEngine.Rendering.Universal.ScriptableRenderPassInput.Color | UnityEngine.Rendering.Universal.ScriptableRenderPassInput.Depth,
                LocalHeatDistortionRenderPass.RequiredInputs);
        }

        [Test]
        public void ShaderUsesCameraDepthForOcclusion()
        {
            string shader = ReadAssetYaml(ShaderPath);

            StringAssert.Contains("DeclareDepthTexture.hlsl", shader);
            StringAssert.Contains("_LocalHeatDistortionAreaDepthParams", shader);
            StringAssert.Contains("SampleSceneDepth", shader);
            StringAssert.Contains("smoothstep(0.0, depthFade, sceneDepth - sourceDepth)", shader);
        }

        static void AssertMode(LocalHeatDistortionMode mode)
        {
            LocalHeatDistortionSettings settings = new LocalHeatDistortionSettings(0.2f, mode, 12f, 24f, 0.02f, 0.25f, 1f);
            Assert.AreEqual(mode, settings.Mode);
        }

        static void AssertRendererReferencesLocalHeatDistortion(string assetPath)
        {
            string yaml = ReadAssetYaml(assetPath);

            StringAssert.Contains($"guid: {FeatureScriptGuid}", yaml);
            StringAssert.Contains($"guid: {ShaderGuid}", yaml);
            StringAssert.Contains("m_Name: 3C Local Heat Distortion", yaml);
        }

        static Camera CreateCamera()
        {
            GameObject cameraObject = new GameObject("Camera");
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            return camera;
        }

        static string ReadAssetYaml(string assetPath)
        {
            string fullPath = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
            return File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
        }
    }
}
