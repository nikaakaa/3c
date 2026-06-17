using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ThirdPersonRendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ThirdPersonRendering.Tests
{
    public sealed class ScreenSpaceDotTransparencyTests
    {
        const string ToonShaderGuid = "dbfab398bab503a4e87654d5c7bf6230";
        const string ToonInputGuid = "a4ad4c5107ea70e4e880735bd15440f2";
        const string ToonForwardPassGuid = "6fee24d9396a75b4d8dda4a7b9f9cb82";
        const string ToonOutlinePassGuid = "6f49655ece7e36147a388340bc4f12fb";
        const string DotIncludeGuid = "7fd6f248da664d44b3db97c4d92a8c7f";
        const string SharedDotIncludeGuid = "843a994a6d1d47bb83605f836d30bbe0";
        const string DepthOnlyPassGuid = "094793510fb24669852459588cbaecf2";
        const string DepthNormalsPassGuid = "e60a3b40abf64dbeaa7191e82955c2b6";
        const string HasteEyeShellShaderGuid = "538bf12a6353aa546ae76af0a3cf4925";
        const string HasteGlassShaderGuid = "9425295c21cbeaf449d356c6ed9062b3";
        const string MaterialPath = "Assets/Art/Mat/Rendering/ScreenSpaceDotTransparency/ScreenSpaceDotTransparencyPreview.mat";

        [Test]
        public void DefaultSettingsDoNotActivateDotTransparency()
        {
            Assert.False(ScreenSpaceDotTransparencySettings.Disabled.IsActive);
            Assert.AreEqual(0f, ScreenSpaceDotTransparencySettings.Disabled.BuildPrimaryParams().x);
        }

        [Test]
        public void SettingsClampUnsafeValues()
        {
            ScreenSpaceDotTransparencySettings settings = new ScreenSpaceDotTransparencySettings(
                true,
                99f,
                -5f,
                12f,
                float.NaN,
                new Vector2(999999f, -999999f));

            Assert.True(settings.IsActive);
            Assert.AreEqual(ScreenSpaceDotTransparencySettings.MaxCoverage, settings.Coverage);
            Assert.AreEqual(ScreenSpaceDotTransparencySettings.MinSpacingPixels, settings.SpacingPixels);
            Assert.AreEqual(ScreenSpaceDotTransparencySettings.MaxRadius, settings.Radius);
            Assert.AreEqual(ScreenSpaceDotTransparencySettings.MinHardness, settings.Hardness);
            Assert.AreEqual(ScreenSpaceDotTransparencySettings.MaxOffsetPixels, settings.OffsetPixels.x);
            Assert.AreEqual(-ScreenSpaceDotTransparencySettings.MaxOffsetPixels, settings.OffsetPixels.y);
        }

        [Test]
        public void ProfileOnValidateClampsUnsafeValues()
        {
            ScreenSpaceDotTransparencyProfile profile = ScriptableObject.CreateInstance<ScreenSpaceDotTransparencyProfile>();
            try
            {
                SerializedObject serializedObject = new SerializedObject(profile);
                serializedObject.FindProperty("enabled").boolValue = true;
                serializedObject.FindProperty("coverage").floatValue = 9f;
                serializedObject.FindProperty("spacingPixels").floatValue = -4f;
                serializedObject.FindProperty("radius").floatValue = 8f;
                serializedObject.FindProperty("hardness").floatValue = -3f;
                serializedObject.FindProperty("offsetPixels").vector2Value = new Vector2(99999f, -99999f);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

                typeof(ScreenSpaceDotTransparencyProfile)
                    .GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(profile, null);

                Assert.AreEqual(ScreenSpaceDotTransparencySettings.MaxCoverage, profile.Coverage);
                Assert.AreEqual(ScreenSpaceDotTransparencySettings.MinSpacingPixels, profile.SpacingPixels);
                Assert.AreEqual(ScreenSpaceDotTransparencySettings.MaxRadius, profile.Radius);
                Assert.AreEqual(ScreenSpaceDotTransparencySettings.MinHardness, profile.Hardness);
                Assert.AreEqual(ScreenSpaceDotTransparencySettings.MaxOffsetPixels, profile.OffsetPixels.x);
                Assert.AreEqual(-ScreenSpaceDotTransparencySettings.MaxOffsetPixels, profile.OffsetPixels.y);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }


        [Test]
        public void RuntimeApplyUsesMaterialPropertyBlockWithoutChangingSharedMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                Renderer renderer = gameObject.GetComponent<Renderer>();
                renderer.sharedMaterial = material;
                float sharedCoverage = material.GetFloat("_ScreenDotCoverage");

                ScreenSpaceDotTransparencySettings settings = new ScreenSpaceDotTransparencySettings(
                    true,
                    0.75f,
                    18f,
                    0.8f,
                    0.65f,
                    new Vector2(3f, 4f));

                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                Assert.True(ScreenSpaceDotTransparencyController.ApplyToRenderer(renderer, settings, propertyBlock));

                MaterialPropertyBlock readback = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(readback);
                Assert.AreEqual(1f, readback.GetFloat("_ScreenDotTransparencyEnabled"));
                Assert.AreEqual(0.75f, readback.GetFloat("_ScreenDotCoverage"));
                Assert.AreEqual(18f, readback.GetFloat("_ScreenDotSpacingPixels"));
                Assert.AreEqual(0.8f, readback.GetFloat("_ScreenDotRadius"));
                Assert.AreEqual(0.65f, readback.GetFloat("_ScreenDotHardness"));
                Assert.AreEqual(new Vector4(3f, 4f, 0f, 0f), readback.GetVector("_ScreenDotOffsetPixels"));
                Assert.AreEqual(sharedCoverage, material.GetFloat("_ScreenDotCoverage"));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ToonShaderExposesDotTransparencyProperties()
        {
            string shader = ReadAssetByGuid(ToonShaderGuid);
            string input = ReadAssetByGuid(ToonInputGuid);
            string wrapper = ReadAssetByGuid(DotIncludeGuid);
            string include = ReadAssetByGuid(SharedDotIncludeGuid);

            StringAssert.Contains("_ScreenDotTransparencyEnabled", shader);
            StringAssert.Contains("_ScreenDotCoverage", shader);
            StringAssert.Contains("_ScreenDotSpacingPixels", shader);
            StringAssert.Contains("_ScreenDotRadius", shader);
            StringAssert.Contains("_ScreenDotHardness", shader);
            StringAssert.Contains("_ScreenDotOffsetPixels", shader);
            StringAssert.Contains("_ScreenDotTransparencyEnabled", input);
            StringAssert.Contains("ScreenSpaceDotTransparency.hlsl", wrapper);
            StringAssert.Contains("ScreenDotTransparencyOpaqueMask", include);
            StringAssert.Contains("ApplyScreenDotTransparencyClip", include);
            StringAssert.Contains("positionCS.xy + _ScreenDotOffsetPixels.xy", include);
        }


        [Test]
        public void ToonVisibleAndCameraDepthPassesUseSharedDotClip()
        {
            string forward = ReadAssetByGuid(ToonForwardPassGuid);
            string outline = ReadAssetByGuid(ToonOutlinePassGuid);
            string depthOnly = ReadAssetByGuid(DepthOnlyPassGuid);
            string depthNormals = ReadAssetByGuid(DepthNormalsPassGuid);

            StringAssert.Contains("ApplyScreenDotTransparencyClip(input.positionCS);", forward);
            StringAssert.Contains("ApplyScreenDotTransparencyClip(input.positionCS);", outline);
            StringAssert.Contains("ApplyScreenDotTransparencyClip(input.positionCS);", depthOnly);
            StringAssert.Contains("ApplyScreenDotTransparencyClip(input.positionCS);", depthNormals);
        }


        static string ReadAssetByGuid(string guid)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Assert.False(string.IsNullOrEmpty(assetPath), guid);
            return ReadAssetYaml(assetPath);
        }

        static void AssertHasteDiffuseShaderUsesScreenDotClip(string shader)
        {
            StringAssert.Contains("_ScreenDotTransparencyEnabled", shader);
            StringAssert.Contains("_ScreenDotCoverage", shader);
            StringAssert.Contains("_ScreenDotSpacingPixels", shader);
            StringAssert.Contains("_ScreenDotRadius", shader);
            StringAssert.Contains("_ScreenDotHardness", shader);
            StringAssert.Contains("_ScreenDotOffsetPixels", shader);
            StringAssert.Contains("ScreenSpaceDotTransparency.hlsl", shader);
            StringAssert.Contains("ApplyScreenDotTransparencyClip(input.pos);", shader);
            StringAssert.Contains("\"RenderType\" = \"Opaque\"", shader);
            StringAssert.DoesNotContain("\"Queue\" = \"Transparent\"", shader);
        }

        static string ReadAssetYaml(string assetPath)
        {
            string fullPath = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
            return File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
        }

        static string ExtractBetween(string source, string startMarker, string endMarker)
        {
            int start = source.IndexOf(startMarker, System.StringComparison.Ordinal);
            int end = source.IndexOf(endMarker, start + startMarker.Length, System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0, startMarker);
            Assert.Greater(end, start, endMarker);
            return source.Substring(start, end - start);
        }
    }
}
