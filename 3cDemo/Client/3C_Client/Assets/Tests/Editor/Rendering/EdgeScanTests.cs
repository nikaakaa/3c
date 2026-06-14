using System.IO;
using NUnit.Framework;
using ThirdPersonRendering;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonRendering.Tests
{
    public sealed class EdgeScanTests
    {
        const string HighFidelityRendererPath = "Assets/Settings/URP-HighFidelity-Renderer.asset";
        const string BalancedRendererPath = "Assets/Settings/URP-Balanced-Renderer.asset";
        const string PerformantRendererPath = "Assets/Settings/URP-Performant-Renderer.asset";
        const string ProfilePath = "Assets/Settings/SampleSceneProfile.asset";
        const string ShaderPath = "Assets/Shader/PostProcessing/EdgeScan/EdgeScan.shader";
        const string FeatureScriptGuid = "070bb63e8c9a4d10b7c428f48a3b5825";
        const string VolumeScriptGuid = "af4288aee78a4eef819ce5c7cb3ab271";
        const string ShaderGuid = "2b23266d39e54f869c6782515becc274";

        [Test]
        public void DefaultSettingsDoNotActivateEdgeScan()
        {
            Assert.False(EdgeScanSettings.Disabled.IsActive);
        }

        [Test]
        public void PositiveIntensityAndValidWidthActivateEdgeScan()
        {
            EdgeScanSettings settings = new EdgeScanSettings(
                0.35f,
                Vector3.zero,
                6f,
                1.5f,
                Color.cyan,
                0.08f,
                0.25f,
                1.5f,
                80f,
                Vector3.forward,
                120f,
                1.15f,
                0.08f,
                1.2f,
                1.8f,
                0.22f);

            Assert.True(settings.IsActive);
        }

        [Test]
        public void SettingsClampToSafeRanges()
        {
            EdgeScanSettings settings = new EdgeScanSettings(
                9f,
                new Vector3(1f, 2f, 3f),
                -5f,
                -8f,
                Color.white,
                -1f,
                9f,
                9f,
                999f,
                Vector3.zero,
                -30f,
                -1f,
                -1f,
                9f,
                9f,
                9f);

            Assert.AreEqual(EdgeScanSettings.MaxIntensity, settings.Intensity);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), settings.Origin);
            Assert.AreEqual(EdgeScanSettings.MinRadius, settings.Radius);
            Assert.AreEqual(EdgeScanSettings.MinWidth, settings.Width);
            Assert.AreEqual(EdgeScanSettings.MinDepthThreshold, settings.DepthThreshold);
            Assert.AreEqual(EdgeScanSettings.MaxNormalThreshold, settings.NormalThreshold);
            Assert.AreEqual(EdgeScanSettings.MaxEdgeStrength, settings.EdgeStrength);
            Assert.AreEqual(EdgeScanSettings.MaxDistanceFade, settings.DistanceFade);
            Assert.AreEqual(Vector3.forward, settings.Direction);
            Assert.AreEqual(EdgeScanSettings.MinArcAngle, settings.ArcAngle);
            Assert.AreEqual(EdgeScanSettings.MinScanLineSpacing, settings.ScanLineSpacing);
            Assert.AreEqual(EdgeScanSettings.MinScanLineWidth, settings.ScanLineWidth);
            Assert.AreEqual(EdgeScanSettings.MaxScanLineStrength, settings.ScanLineStrength);
            Assert.AreEqual(EdgeScanSettings.MaxFrontGlowStrength, settings.FrontGlowStrength);
            Assert.AreEqual(EdgeScanSettings.MaxDarkenStrength, settings.DarkenStrength);
        }

        [Test]
        public void ShaderParamsExposeScanShellAndEdges()
        {
            EdgeScanSettings settings = new EdgeScanSettings(
                1f,
                new Vector3(1f, 2f, 3f),
                8f,
                2f,
                Color.cyan,
                0.08f,
                0.25f,
                1.5f,
                80f,
                new Vector3(0f, 9f, 2f),
                120f,
                1.15f,
                0.08f,
                1.2f,
                1.8f,
                0.22f);

            Assert.AreEqual(new Vector4(1f, 2f, 3f, 8f), settings.OriginRadiusParams);
            Assert.AreEqual(new Vector4(1f, 2f, 1.5f, 80f), settings.ScanParams);
            Assert.AreEqual(new Vector4(0.08f, 0.25f, 0f, 0f), settings.EdgeParams);
            Assert.AreEqual(new Vector4(0f, 0f, 1f, settings.DirectionArcParams.w), settings.DirectionArcParams);
            Assert.That(settings.DirectionArcParams.w, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.AreEqual(new Vector4(1.15f, 0.08f, 1.2f, 1.8f), settings.LineParams);
            Assert.AreEqual(new Vector4(0.22f, 0f, 0f, 0f), settings.ToneParams);
        }

        [Test]
        public void VolumeDefaultDoesNotActivateEdgeScan()
        {
            EdgeScan edgeScan = ScriptableObject.CreateInstance<EdgeScan>();
            try
            {
                Assert.False(edgeScan.IsActive());
            }
            finally
            {
                Object.DestroyImmediate(edgeScan);
            }
        }

        [Test]
        public void VolumePositiveIntensityActivatesEdgeScan()
        {
            EdgeScan edgeScan = ScriptableObject.CreateInstance<EdgeScan>();
            try
            {
                edgeScan.intensity.value = 0.4f;

                Assert.True(edgeScan.IsActive());
                Assert.False(edgeScan.IsTileCompatible());
            }
            finally
            {
                Object.DestroyImmediate(edgeScan);
            }
        }

        [Test]
        public void RendererFeatureWithoutShaderCannotRender()
        {
            EdgeScanRendererFeature feature = ScriptableObject.CreateInstance<EdgeScanRendererFeature>();
            try
            {
                feature.Create();

                EdgeScanSettings settings = CreateActiveSettings();

                Assert.False(feature.HasMaterial);
                Assert.False(feature.HasPass);
                Assert.False(feature.CanRender(settings));
            }
            finally
            {
                Object.DestroyImmediate(feature);
            }
        }

        [Test]
        public void RendererFeatureWithShaderCanRenderActiveSettings()
        {
            EdgeScanRendererFeature feature = ScriptableObject.CreateInstance<EdgeScanRendererFeature>();
            try
            {
                feature.Shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
                feature.Create();

                EdgeScanSettings settings = CreateActiveSettings();

                Assert.NotNull(feature.Shader);
                Assert.True(feature.HasMaterial);
                Assert.True(feature.HasPass);
                Assert.True(feature.CanRender(settings));
            }
            finally
            {
                Object.DestroyImmediate(feature);
            }
        }

        [Test]
        public void RenderPassRequiresCameraColorDepthAndNormalInputs()
        {
            Assert.AreEqual(
                UnityEngine.Rendering.Universal.ScriptableRenderPassInput.Color |
                UnityEngine.Rendering.Universal.ScriptableRenderPassInput.Depth |
                UnityEngine.Rendering.Universal.ScriptableRenderPassInput.Normal,
                EdgeScanRenderPass.RequiredInputs);
        }

        [Test]
        public void QualityRenderersReferenceEdgeScanFeatureAndShader()
        {
            AssertRendererReferencesEdgeScan(HighFidelityRendererPath);
            AssertRendererReferencesEdgeScan(BalancedRendererPath);
            AssertRendererReferencesEdgeScan(PerformantRendererPath);
        }

        [Test]
        public void ShaderUsesDepthNormalsAndWorldShell()
        {
            string shader = ReadAssetYaml(ShaderPath);

            StringAssert.Contains("DeclareDepthTexture.hlsl", shader);
            StringAssert.Contains("DeclareNormalsTexture.hlsl", shader);
            StringAssert.Contains("ComputeWorldSpacePosition", shader);
            StringAssert.Contains("SampleSceneDepth", shader);
            StringAssert.Contains("SampleSceneNormals", shader);
            StringAssert.Contains("_EdgeScanOriginRadius", shader);
            StringAssert.Contains("_EdgeScanDirectionArc", shader);
            StringAssert.Contains("_EdgeScanLineParams", shader);
            StringAssert.Contains("_EdgeScanToneParams", shader);
            StringAssert.Contains("ScanArcMask", shader);
            StringAssert.Contains("ScanLineMask", shader);
            StringAssert.Contains("bodyMask", shader);
            StringAssert.Contains("frontMask", shader);
        }

        [Test]
        public void SampleSceneProfileContainsEdgeScanVolume()
        {
            string yaml = ReadAssetYaml(ProfilePath);

            StringAssert.Contains($"guid: {VolumeScriptGuid}", yaml);
            StringAssert.Contains("m_Name: Edge Scan", yaml);
            StringAssert.Contains("intensity:", yaml);
            StringAssert.Contains("origin:", yaml);
            StringAssert.Contains("radius:", yaml);
            StringAssert.Contains("width:", yaml);
            StringAssert.Contains("depthThreshold:", yaml);
            StringAssert.Contains("normalThreshold:", yaml);
            StringAssert.Contains("edgeStrength:", yaml);
            StringAssert.Contains("distanceFade:", yaml);
            StringAssert.Contains("direction:", yaml);
            StringAssert.Contains("arcAngle:", yaml);
            StringAssert.Contains("scanLineSpacing:", yaml);
            StringAssert.Contains("scanLineWidth:", yaml);
            StringAssert.Contains("scanLineStrength:", yaml);
            StringAssert.Contains("frontGlowStrength:", yaml);
            StringAssert.Contains("darkenStrength:", yaml);
        }

        static EdgeScanSettings CreateActiveSettings()
        {
            return new EdgeScanSettings(
                0.5f,
                Vector3.zero,
                5f,
                1f,
                Color.cyan,
                0.08f,
                0.25f,
                1f,
                80f,
                Vector3.forward,
                120f,
                1.15f,
                0.08f,
                1.2f,
                1.8f,
                0.22f);
        }

        static void AssertRendererReferencesEdgeScan(string assetPath)
        {
            string yaml = ReadAssetYaml(assetPath);

            StringAssert.Contains($"guid: {FeatureScriptGuid}", yaml);
            StringAssert.Contains($"guid: {ShaderGuid}", yaml);
            StringAssert.Contains("m_Name: 3C Edge Scan", yaml);
            StringAssert.Contains("injectionPoint: 550", yaml);
        }

        static string ReadAssetYaml(string assetPath)
        {
            string fullPath = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
            return File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
        }
    }
}
