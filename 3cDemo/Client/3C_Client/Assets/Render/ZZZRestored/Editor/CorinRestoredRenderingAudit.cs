using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MCPForUnity.Runtime.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ZZZ.Rendering.Restored.Editor
{
    public static class CorinRestoredRenderingAudit
    {
        static readonly string[] EntityVectors =
        {
            "_PackedParams1", "_MiddlePointPosition", "_AmbientGradientShape", "_EntityInfo",
            "_HeadMatrixWS2OS0", "_HeadMatrixWS2OS1", "_HeadMatrixWS2OS2"
        };

        static readonly string[] MaterialArrays =
        {
            "_RefractParamArray", "_MatCapColorTintArray",
            "_MatCapTexID_MatCapColorBurst_MatCapAlphaBurst_MatCapUSpeed",
            "_MatCapVSpeed_MatCapBlendMode_MatCapRefract_RefractDepth"
        };

        [MenuItem("Tools/ZZZ/Restored/Capture Original Outline Comparison")]
        public static void CaptureOutlineComparison()
        {
            var camera = Object.FindObjectsOfType<Camera>().Single(value => value.name == "ZZZ_RenderCheck_Camera");
            var entries = Object.FindObjectsOfType<CorinRestoredRenderEntity>()
                .SelectMany(entity => entity.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                .SelectMany(renderer => renderer.sharedMaterials).Distinct()
                .SelectMany(material => new[] { "CharacterOutlineDeferred", "FaceOutlineDeferred" }
                    .Where(pass => material.FindPass(pass) >= 0)
                    .Select(pass => (Material: material, Pass: pass, Enabled: material.GetShaderPassEnabled(pass))))
                .ToArray();
            if (entries.Length == 0 || entries.Any(entry => !entry.Enabled))
                throw new InvalidOperationException("描边对照要求原材质的描边 Pass 均已启用。");
            var folder = "Diagnostics/Rendering/ZZZRestored/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff") + "-outline-pair";
            var before = ScreenshotUtility.CaptureFromCameraToProjectFolder(camera, "OutlineOn", folderOverride: folder);
            try
            {
                foreach (var entry in entries)
                    entry.Material.SetShaderPassEnabled(entry.Pass, false);
                ScreenshotUtility.CaptureFromCameraToProjectFolder(camera, "OutlineOff", folderOverride: folder);
            }
            finally
            {
                foreach (var entry in entries)
                    entry.Material.SetShaderPassEnabled(entry.Pass, entry.Enabled);
            }
            ScreenshotUtility.CaptureFromCameraToProjectFolder(camera, "OutlineRestored", folderOverride: folder);
            Capture();
            Debug.Log("原描边同参数开关对照完成，材质开关已恢复，未保存场景：" + before.ProjectRelativePath);
        }

        [MenuItem("Tools/ZZZ/Restored/Audit Current Original Corin Rendering")]
        public static void Capture()
        {
            var entities = Object.FindObjectsOfType<CorinRestoredRenderEntity>();
            if (entities.Length == 0)
                throw new InvalidOperationException("当前场景没有可琳原版渲染实体。");
            var profile = AssetDatabase.LoadAssetAtPath<CorinRestoredRenderProfile>(
                "Assets/AssetArt/Model/ZZZ/可琳/可琳tex/ZZZ导出/Materials/Original/CorinRestoredRenderProfile.asset");
            var configuredMatrices = new JObject();
            foreach (var name in new[] { "sceneWeatherParamsPart1", "sceneFogParamsPart1",
                         "sceneFogParamsPart2", "sceneFogParamsPart3" })
            {
                var field = typeof(CorinRestoredRenderProfile).GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var matrix = (Matrix4x4)field.GetValue(profile);
                configuredMatrices[name] = new JArray(Enumerable.Range(0, 4).Select(row => Vector(matrix.GetRow(row))));
            }
            var globals = new JObject();
            foreach (var name in new[] { "_SceneWeatherParamsPart1", "_SceneFogParamsPart1",
                         "_SceneFogParamsPart2", "_SceneFogParamsPart3" })
            {
                var matrix = Shader.GetGlobalMatrix(name);
                globals[name] = new JArray(Enumerable.Range(0, 4).Select(row => Vector(matrix.GetRow(row))));
            }
            globals["_InternalLut_Char"] = Texture(Shader.GetGlobalTexture("_InternalLut_Char"));
            globals["_AvatarMainLightColor"] = Vector(Shader.GetGlobalVector("_AvatarMainLightColor"));
            globals["_AvatarMainLightPosition"] = Vector(Shader.GetGlobalVector("_AvatarMainLightPosition"));
            foreach (var name in new[] { "_CharStyleParams", "_PostOutlineTint", "_BloomThreshold", "_AlphaBlendAlphaParams" })
                globals[name] = Vector(Shader.GetGlobalVector(name));
            globals["_GlobalMipBias"] = Shader.GetGlobalFloat("_GlobalMipBias");
            var entityRows = new JArray();
            foreach (var entity in entities)
            {
                var rendererRows = new JArray();
                foreach (var renderer in entity.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block);
                    var vectors = new JObject();
                    foreach (var name in EntityVectors)
                        vectors[name] = Vector(block.GetVector(name));
                    var materials = new JArray();
                    foreach (var material in renderer.sharedMaterials)
                    {
                        var arrays = new JObject();
                        foreach (var name in MaterialArrays)
                        {
                            var values = material.GetVectorArray(name);
                            arrays[name] = values == null ? JValue.CreateNull() : new JArray(values.Select(Vector));
                        }
                        materials.Add(new JObject
                        {
                            ["asset"] = AssetDatabase.GetAssetPath(material),
                            ["shader"] = material.shader.name,
                            ["supported"] = material.shader.isSupported,
                            ["keywords"] = new JArray(material.shaderKeywords),
                            ["mainTexture"] = material.HasProperty("_MainTex")
                                ? Texture(material.GetTexture("_MainTex")) : JValue.CreateNull(),
                            ["arrays"] = arrays
                        });
                    }
                    rendererRows.Add(new JObject
                    {
                        ["name"] = renderer.name,
                        ["enabled"] = renderer.enabled,
                        ["mesh"] = renderer.sharedMesh.name,
                        ["subMeshCount"] = renderer.sharedMesh.subMeshCount,
                        ["vertexCount"] = renderer.sharedMesh.vertexCount,
                        ["vertexAttributes"] = new JArray(renderer.sharedMesh.GetVertexAttributes().Select(attribute => new JObject
                        {
                            ["name"] = attribute.attribute.ToString(),
                            ["format"] = attribute.format.ToString(),
                            ["dimension"] = attribute.dimension,
                            ["stream"] = attribute.stream
                        })),
                        ["propertyBlock"] = vectors,
                        ["blendShapeWeights"] = new JArray(Enumerable.Range(0, renderer.sharedMesh.blendShapeCount)
                            .Select(index => new JObject
                            {
                                ["name"] = renderer.sharedMesh.GetBlendShapeName(index),
                                ["weight"] = renderer.GetBlendShapeWeight(index)
                            })),
                        ["materials"] = materials
                    });
                }
                entityRows.Add(new JObject
                {
                    ["name"] = entity.name,
                    ["position"] = Vector(entity.transform.position),
                    ["renderers"] = rendererRows
                });
            }
            var report = new JObject
            {
                ["schema"] = "zzz-corin-rendering-audit/1",
                ["utc"] = DateTime.UtcNow.ToString("O"),
                ["project"] = Application.dataPath,
                ["editorLog"] = Application.consoleLogPath,
                ["unityVersion"] = Application.unityVersion,
                ["graphicsDevice"] = SystemInfo.graphicsDeviceVersion,
                ["loadedProfileInitializeParameterCount"] = typeof(CorinRestoredRenderProfile)
                    .GetMethod("Initialize").GetParameters().Length,
                ["profileConfiguredMatrices"] = configuredMatrices,
                ["profileOutlineInputs"] = new JObject
                {
                    ["_CharStyleParams"] = Vector(profile.Outline.CharacterStyle),
                    ["_PostOutlineTint"] = Vector(profile.Outline.PostTint),
                    ["_BloomThreshold"] = Vector(profile.Outline.BloomThreshold),
                    ["_AlphaBlendAlphaParams"] = Vector(profile.Outline.AlphaBlend),
                    ["_GlobalMipBias"] = profile.Outline.GlobalMipBias
                },
                ["profileTextureInputs"] = new JObject
                {
                    ["_InternalLut_Char"] = Texture(profile.CharacterLut),
                    ["_CharacterOverlayTex"] = Texture(profile.CharacterOverlay)
                },
                ["cpuGlobalPropertiesAfterRendering"] = globals,
                ["entities"] = entityRows,
                ["lights"] = new JArray(Object.FindObjectsOfType<Light>().Select(light => new JObject
                {
                    ["name"] = light.name,
                    ["enabled"] = light.enabled,
                    ["type"] = light.type.ToString(),
                    ["intensity"] = light.intensity,
                    ["color"] = Vector(light.color),
                    ["rotation"] = Vector(light.transform.eulerAngles)
                }))
            };
            var path = Path.GetFullPath("Diagnostics/Rendering/ZZZRestored/" +
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff") + "-rendering-audit.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, report.ToString(), new UTF8Encoding(false));
            Debug.Log("可琳原版渲染只读检查：" + path);
        }

        static JArray Vector(Vector4 value) => new(value.x, value.y, value.z, value.w);

        static JToken Texture(Texture texture) => texture == null ? JValue.CreateNull() : new JObject
        {
            ["name"] = texture.name,
            ["asset"] = AssetDatabase.GetAssetPath(texture),
            ["width"] = texture.width,
            ["height"] = texture.height
        };
    }
}
