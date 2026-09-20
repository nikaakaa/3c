using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using ZZZ.Rendering.Restored;
using Object = UnityEngine.Object;

namespace ZZZ.Rendering.Restored.Editor
{
    public static class CorinRestoredMaterialImporter
    {
        const string SourceRoot = "Assets/AssetArt/Model/ZZZ/可琳/可琳tex/ZZZ导出";
        const string RawRoot = SourceRoot + "/Raw";
        const string TextureRoot = SourceRoot + "/Textures/Original";
        const string MaterialRoot = SourceRoot + "/Materials/Original";
        const string ProfilePath = MaterialRoot + "/CorinRestoredMaterialSet.asset";
        const string RenderProfilePath = MaterialRoot + "/CorinRestoredRenderProfile.asset";
        const string OriginalModelPath = "Assets/AssetArt/Model/ZZZ/可琳/Original/Avatar_Female_Size01_Corin_Model_ZZZ_Original.fbx";
        const string OriginalPrefabPath = "Assets/AssetArt/Model/ZZZ/可琳/Original/Corin_ZZZ_Original.prefab";
        const string DeferredShaderPath = "Assets/Render/ZZZRestored/Generated/Deferred/CharacterDeferredComposite.shader";
        const string EntityPreparePath = "Assets/Render/ZZZRestored/Generated/EntityLighting/NapEntityPrepare.compute";
        const string CharacterLutPath = "Assets/Render/ZZZRestored/Generated/CharacterLut/CorinOriginalCharacterLut.asset";

        static readonly string[] RendererDataPaths =
        {
            "Assets/Settings/URP-HighFidelity-Renderer.asset",
            "Assets/Settings/URP-Balanced-Renderer.asset",
            "Assets/Settings/URP-Performant-Renderer.asset",
            "Assets/RockyDesert/UniversalRenderPipelineAsset_Renderer.asset"
        };

        sealed class MaterialSource
        {
            public MaterialSource(string name, string json, string shader,
                IReadOnlyDictionary<string, string> textures)
            {
                Name = name;
                Json = json;
                Shader = shader;
                Textures = textures;
            }

            public string Name { get; }
            public string Json { get; }
            public string Shader { get; }
            public IReadOnlyDictionary<string, string> Textures { get; }
        }

        static readonly MaterialSource[] Sources =
        {
            new MaterialSource("Body", "MAT_Corin_Body_ZZZ.json", "ZZZ/Restored/NapAvatarStandard",
                new Dictionary<string, string>
                {
                    ["_MainTex"] = "Corin_Body_D",
                    ["_LightTex"] = "Corin_Body_N",
                    ["_OtherDataTex"] = "Corin_Body_M",
                    ["_OtherDataTex2"] = "Corin_Body_A",
                    ["_MatCapTex"] = "Eff_MatCap_019",
                    ["_MatCapTex2"] = "Eff_MatCap_019",
                    ["_MatCapTex3"] = "Eff_MatCap_019",
                    ["_MatCapTex4"] = "Eff_MatCap_019",
                    ["_MatCapTex5"] = "Eff_MatCap_019"
                }),
            new MaterialSource("Face", "MAT_Corin_Face_ZZZ.json", "ZZZ/Restored/NapAvatarStandardFace",
                new Dictionary<string, string>
                {
                    ["_MainTex"] = "Corin_Face_D",
                    ["_LightTex"] = "Female_Face_Lightmap"
                }),
            new MaterialSource("Eye", "MAT_Corin_Eye_ZZZ.json", "ZZZ/Restored/NapAvatarStandardEye",
                new Dictionary<string, string>
                {
                    ["_MainTex"] = "Corin_Face_D",
                    ["_LightTex"] = "Female_Face_Lightmap",
                    ["_EyeColorMap"] = "Eye_E"
                }),
            new MaterialSource("Hair", "MAT_Corin_Hair_ZZZ.json", "ZZZ/Restored/NapAvatarStandard",
                new Dictionary<string, string>
                {
                    ["_MainTex"] = "Corin_Hair_D",
                    ["_LightTex"] = "Corin_Hair_N",
                    ["_OtherDataTex"] = "Corin_Hair_M",
                    ["_OtherDataTex2"] = "Corin_Hair_A"
                }),
            new MaterialSource("HairShadow", "MAT_HairShadow_ZZZ.json", "ZZZ/Restored/NapStencilShadowCaster",
                new Dictionary<string, string>()),
            new MaterialSource("Weapon", "MAT_Corin_Weapon_ZZZ.json", "ZZZ/Restored/NapAvatarStandard",
                new Dictionary<string, string>
                {
                    ["_MainTex"] = "Corin_Weapon_D",
                    ["_LightTex"] = "Corin_Weapon_N",
                    ["_OtherDataTex"] = "Corin_Weapon_M",
                    ["_OtherDataTex2"] = "Corin_Weapon_A"
                })
        };

        [MenuItem("Tools/ZZZ/Restored/Rebuild Original Corin Materials")]
        public static void Rebuild()
        {
            EnsureFolder(TextureRoot);
            EnsureFolder(MaterialRoot);
            var contract = JObject.Parse(File.ReadAllText(RawRoot + "/texture-raw-export.json", Encoding.UTF8));
            if ((string)contract["Schema"] != "zzz-texture-source-contract/1")
                throw new InvalidOperationException("可琳纹理来源合同版本不匹配。");
            var textures = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
            foreach (var token in (JArray)contract["Textures"])
            {
                var source = (JObject)token;
                var name = (string)source["Name"];
                var format = Enum.Parse<TextureFormat>((string)source["Format"]);
                if (!SystemInfo.SupportsTextureFormat(format))
                    throw new InvalidOperationException($"当前图形设备不支持原纹理格式 {format}：{name}");
                var bytes = File.ReadAllBytes(RawRoot + "/" + name + ".bytes");
                var generated = new Texture2D((int)source["Width"], (int)source["Height"], format,
                    (int)source["MipCount"], (int)source["ColorSpace"] == 0)
                {
                    name = name,
                    filterMode = (FilterMode)(int)source["FilterMode"],
                    anisoLevel = (int)source["Aniso"],
                    mipMapBias = (float)source["MipBias"],
                    wrapModeU = (TextureWrapMode)(int)source["WrapU"],
                    wrapModeV = (TextureWrapMode)(int)source["WrapV"],
                    wrapModeW = (TextureWrapMode)(int)source["WrapW"]
                };
                generated.LoadRawTextureData(bytes);
                generated.Apply(false, true);
                textures.Add(name, Save(generated, TextureRoot + "/" + name + ".asset"));
            }
            var matCap = new Texture2DArray(256, 256, 1, TextureFormat.BC7, 5, false)
            {
                name = "CorinMatCapArray",
                filterMode = FilterMode.Bilinear,
                anisoLevel = 1,
                wrapMode = TextureWrapMode.Repeat
            };
            for (var mip = 0; mip < 5; mip++)
                Graphics.CopyTexture(textures["Eff_MatCap_019"], 0, mip, matCap, 0, mip);
            matCap.Apply(false, true);
            matCap = Save(matCap, TextureRoot + "/CorinMatCapArray.asset");

            var materials = new Dictionary<string, Material>(StringComparer.Ordinal);
            ValidateShader(Shader.Find("ZZZ/Restored/NapAvatarStandard"), new Dictionary<int, string[]>
            {
                [0] = Array.Empty<string>(),
                [1] = Array.Empty<string>(),
                [2] = Array.Empty<string>(),
                [3] = Array.Empty<string>(),
                [4] = Array.Empty<string>()
            });
            ValidateShader(Shader.Find("ZZZ/Restored/NapAvatarStandard"), new Dictionary<int, string[]>
            {
                [2] = new[] { "_MATCAP_ON" }
            });
            ValidateShader(Shader.Find("ZZZ/Restored/NapAvatarStandardFace"), Enumerable.Range(0, 6)
                .ToDictionary(index => index, _ => Array.Empty<string>()));
            ValidateShader(Shader.Find("ZZZ/Restored/NapAvatarStandardEye"), new Dictionary<int, string[]>
            {
                [0] = Array.Empty<string>(),
                [1] = Array.Empty<string>(),
                [2] = Array.Empty<string>()
            });
            ValidateShader(Shader.Find("ZZZ/Restored/NapStencilShadowCaster"), new Dictionary<int, string[]>
            {
                [0] = new[] { "_NAP_SHADER_QUALITY_HIGH" }
            });
            ValidateShader(Shader.Find("ZZZ/Restored/CharacterDeferredComposite"), new Dictionary<int, string[]>
            {
                [0] = Array.Empty<string>(),
                [1] = Array.Empty<string>()
            });
            foreach (var source in Sources)
            {
                var shader = Shader.Find(source.Shader);
                if (shader == null)
                    throw new InvalidOperationException("原 Shader 尚未导入：" + source.Shader);
                var document = JObject.Parse(File.ReadAllText(SourceRoot + "/" + source.Json, Encoding.UTF8));
                var material = Save(new Material(shader) { name = "Corin_" + source.Name + "_ZZZ_Original" },
                    MaterialRoot + "/Corin_" + source.Name + "_ZZZ_Original.mat");
                material.shader = shader;
                material.shaderKeywords = document["m_ValidKeywords"]?.Values<string>().ToArray() ?? Array.Empty<string>();
                material.renderQueue = (int?)document["m_CustomRenderQueue"] ?? -1;
                ApplyValues(material, (JObject)document["m_SavedProperties"]);
                foreach (var binding in source.Textures)
                {
                    material.SetTexture(binding.Key, textures[binding.Value]);
                    if (document["m_SavedProperties"]?["m_TexEnvs"]?[binding.Key] is JObject texture)
                    {
                        material.SetTextureScale(binding.Key, Vector2Value((JObject)texture["m_Scale"]));
                        material.SetTextureOffset(binding.Key, Vector2Value((JObject)texture["m_Offset"]));
                    }
                }
                if (source.Name == "Body")
                    material.SetTexture("_MatCap2DArray", matCap);
                if (document["stringTagMap"] is JObject tags)
                    foreach (var property in tags.Properties())
                        material.SetOverrideTag(property.Name, (string)property.Value);
                if (document["disabledShaderPasses"] is JArray disabled)
                    foreach (var pass in disabled.Values<string>())
                        material.SetShaderPassEnabled(pass, false);
                EditorUtility.SetDirty(material);
                materials.Add(source.Name, material);
            }
            var profile = AssetDatabase.LoadAssetAtPath<CorinRestoredMaterialSet>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<CorinRestoredMaterialSet>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            profile.Initialize(materials["Body"], materials["Face"], materials["Eye"], materials["Hair"],
                materials["HairShadow"], materials["Weapon"], matCap);
            EditorUtility.SetDirty(profile);
            var renderProfile = AssetDatabase.LoadAssetAtPath<CorinRestoredRenderProfile>(RenderProfilePath);
            if (renderProfile == null)
            {
                renderProfile = ScriptableObject.CreateInstance<CorinRestoredRenderProfile>();
                AssetDatabase.CreateAsset(renderProfile, RenderProfilePath);
            }
            var environment = JObject.Parse(File.ReadAllText(SourceRoot + "/CorinEyeEnvironmentSource.json", Encoding.UTF8));
            if ((string)environment["schema"] != "zzz-corin-eye-environment/1" ||
                (string)environment["matrixStorage"] != "column-major")
                throw new InvalidOperationException("可琳眼部环境参数来源合同不匹配。");
            var matrices = ((JArray)environment["properties"]).Cast<JObject>()
                .ToDictionary(value => (string)value["name"], MatrixValue, StringComparer.Ordinal);
            var outlineSource = JObject.Parse(File.ReadAllText(SourceRoot + "/CorinOutlineSource.json", Encoding.UTF8));
            if ((string)outlineSource["schema"] != "zzz-corin-outline-inputs/1")
                throw new InvalidOperationException("可琳描边参数来源合同不匹配。");
            var outlineValues = ((JArray)outlineSource["properties"]).Cast<JObject>()
                .ToDictionary(value => (string)value["name"], value => (JArray)value["float_view"], StringComparer.Ordinal);
            Vector4 OutlineVector(string name) => new((float)outlineValues[name][0], (float)outlineValues[name][1],
                (float)outlineValues[name][2], (float)outlineValues[name][3]);
            var outline = new CorinRestoredOutlineParameters
            {
                CharacterStyle = OutlineVector("_CharStyleParams"),
                PostTint = OutlineVector("_PostOutlineTint"),
                BloomThreshold = OutlineVector("_BloomThreshold"),
                AlphaBlend = OutlineVector("_AlphaBlendAlphaParams"),
                GlobalMipBias = (float)outlineValues["_GlobalMipBias"][0]
            };
            renderProfile.Initialize(profile, AssetDatabase.LoadAssetAtPath<Shader>(DeferredShaderPath),
                AssetDatabase.LoadAssetAtPath<ComputeShader>(EntityPreparePath),
                AssetDatabase.LoadAssetAtPath<Texture2D>(CharacterLutPath),
                textures["CharacterOverlayTex"],
                matrices["_SceneWeatherParamsPart1"], matrices["_SceneFogParamsPart1"],
                matrices["_SceneFogParamsPart2"], matrices["_SceneFogParamsPart3"], outline);
            EditorUtility.SetDirty(renderProfile);
            AssetDatabase.SaveAssets();
            Debug.Log("可琳 ZZZ 原纹理、原材质与 MatCap 数组已重建：" + ProfilePath);
        }

        [MenuItem("Tools/ZZZ/Restored/Install Original Corin Renderer")]
        public static void InstallRenderer()
        {
            var renderProfile = AssetDatabase.LoadAssetAtPath<CorinRestoredRenderProfile>(RenderProfilePath);
            if (renderProfile == null || renderProfile.DeferredCompositeShader == null || renderProfile.CharacterLut == null)
                throw new InvalidOperationException("原角色渲染配置尚未完整重建。");
            foreach (var path in RendererDataPaths)
            {
                var data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
                if (data == null)
                    throw new InvalidOperationException("URP RendererData 不存在：" + path);
                var existingFeatures = AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<CorinRestoredRendererFeature>()
                    .ToArray();
                if (existingFeatures.Length > 1)
                    throw new InvalidOperationException("URP RendererData存在重复的CorinRestoredRendererFeature：" + path);

                var feature = existingFeatures.SingleOrDefault();
                if (feature == null)
                {
                    feature = ScriptableObject.CreateInstance<CorinRestoredRendererFeature>();
                    feature.name = "CorinRestoredRendererFeature";
                    AssetDatabase.AddObjectToAsset(feature, data);
                }

                var serialized = new SerializedObject(data);
                var features = serialized.FindProperty("m_RendererFeatures");
                var featureMap = serialized.FindProperty("m_RendererFeatureMap");
                var featureIndex = -1;
                var missingFeatureIndex = -1;
                for (var index = 0; index < features.arraySize; index++)
                {
                    var reference = features.GetArrayElementAtIndex(index).objectReferenceValue;
                    if (reference == feature)
                    {
                        featureIndex = index;
                        break;
                    }

                    if (reference == null)
                    {
                        if (missingFeatureIndex >= 0)
                            throw new InvalidOperationException("URP RendererData存在多个丢失的RendererFeature：" + path);
                        missingFeatureIndex = index;
                    }
                }

                if (featureIndex < 0)
                {
                    if (missingFeatureIndex >= 0)
                    {
                        featureIndex = missingFeatureIndex;
                        features.GetArrayElementAtIndex(featureIndex).objectReferenceValue = feature;
                    }
                    else
                    {
                        featureIndex = features.arraySize;
                        features.InsertArrayElementAtIndex(featureIndex);
                        features.GetArrayElementAtIndex(featureIndex).objectReferenceValue = feature;
                    }
                }

                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localFileId))
                    throw new InvalidOperationException("无法取得CorinRestoredRendererFeature的持久文件标识：" + path);
                featureMap.arraySize = features.arraySize;
                featureMap.GetArrayElementAtIndex(featureIndex).longValue = localFileId;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                feature.Profile = renderProfile;
                feature.SetActive(true);
                EditorUtility.SetDirty(feature);
                EditorUtility.SetDirty(data);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("可琳 ZZZ 原角色 Deferred Renderer 已安装到全部 URP RendererData。");
        }

        [MenuItem("Tools/ZZZ/Restored/Rebuild Original Corin Model Prefab")]
        public static void RebuildOriginalModelPrefab()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(OriginalModelPath);
            var profile = AssetDatabase.LoadAssetAtPath<CorinRestoredMaterialSet>(ProfilePath);
            if (source == null || profile == null)
                throw new InvalidOperationException("原模型或原材质集合尚未导入。");
            profile.ApplyRuntimeArrays();
            var instance = Object.Instantiate(source);
            instance.name = "Corin_ZZZ_Original";
            var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .ToDictionary(renderer => renderer.name, StringComparer.Ordinal);
            var expected = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["Corin_body"] = 1,
                ["Corin_body_02"] = 1,
                ["Corin_face"] = 2,
                ["Corin_hair"] = 1,
                ["Corin_HairShadow"] = 1,
                ["Corin_Weapon_01"] = 1
            };
            if (renderers.Count != expected.Count || expected.Any(pair => !renderers.TryGetValue(pair.Key, out var renderer) ||
                    renderer.sharedMesh == null || renderer.sharedMesh.subMeshCount != pair.Value))
            {
                var actual = string.Join("\n", instance.GetComponentsInChildren<Renderer>(true)
                    .Select(renderer => renderer.name + "|" + renderer.GetType().Name + "|subMeshes=" +
                        (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null
                            ? skinned.sharedMesh.subMeshCount
                            : -1) + "|materials=" + renderer.sharedMaterials.Length)
                    .OrderBy(value => value, StringComparer.Ordinal));
                Object.DestroyImmediate(instance);
                throw new InvalidOperationException("原模型 Renderer 或 SubMesh 拓扑不匹配：\n" + actual);
            }
            renderers["Corin_body"].sharedMaterial = profile.Body;
            renderers["Corin_body_02"].sharedMaterial = profile.Body;
            renderers["Corin_face"].sharedMaterials = new[] { profile.Face, profile.Eye };
            renderers["Corin_hair"].sharedMaterial = profile.Hair;
            renderers["Corin_HairShadow"].sharedMaterial = profile.HairShadow;
            renderers["Corin_Weapon_01"].sharedMaterial = profile.Weapon;
            var head = instance.GetComponentsInChildren<Transform>(true)
                .Single(transform => transform.name == "Bip001 Head");
            var entity = instance.AddComponent<CorinRestoredRenderEntity>();
            entity.Initialize(head, renderers.Values.ToArray());
            PrefabUtility.SaveAsPrefabAsset(instance, OriginalPrefabPath);
            Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();
            Debug.Log("可琳 ZZZ 原模型与六个 Renderer 已绑定：" + OriginalPrefabPath);
        }

        [MenuItem("Tools/ZZZ/Restored/Inspect Corin Renderer Topology")]
        public static void InspectTopology()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Characters/RuntimeProfiles/Local/CorinStandalonePlayer.prefab");
            var names = new HashSet<string>(new[]
            {
                "Corin_body", "Corin_body_02", "Corin_face", "Corin_hair", "Corin_Weapon"
            }, StringComparer.Ordinal);
            var rows = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(renderer => names.Contains(renderer.name))
                .Select(renderer => renderer.name + "|subMeshes=" + renderer.sharedMesh.subMeshCount +
                    "|materials=" + renderer.sharedMaterials.Length)
                .OrderBy(value => value, StringComparer.Ordinal);
            Debug.Log("ZZZ Corin topology\n" + string.Join("\n", rows));
        }

        static void ValidateShader(Shader shader, IReadOnlyDictionary<int, string[]> variants)
        {
            if (shader == null)
                throw new InvalidOperationException("待验证的原 Shader 尚未导入。");
            var subshader = ShaderUtil.GetShaderData(shader).GetSubshader(0);
            foreach (var variant in variants)
            {
                var pass = subshader.GetPass(variant.Key);
                var vertex = pass.CompileVariant(ShaderType.Vertex, variant.Value, ShaderCompilerPlatform.D3D,
                    BuildTarget.StandaloneWindows64);
                var fragment = pass.CompileVariant(ShaderType.Fragment, variant.Value, ShaderCompilerPlatform.D3D,
                    BuildTarget.StandaloneWindows64);
                if (!vertex.Success || !fragment.Success)
                    throw new InvalidOperationException(shader.name + " pass " + variant.Key + " 编译失败：\n" +
                        string.Join("\n", vertex.Messages.Concat(fragment.Messages).Select(message => message.message)));
            }
        }

        static void ApplyValues(Material material, JObject properties)
        {
            if (properties["m_Floats"] is JObject floats)
                foreach (var property in floats.Properties())
                    if (material.HasProperty(property.Name))
                        material.SetFloat(property.Name, (float)property.Value);
            if (properties["m_Colors"] is JObject colors)
                foreach (var property in colors.Properties())
                    if (material.HasProperty(property.Name))
                        material.SetVector(property.Name, Vector4Value((JObject)property.Value));
        }

        static Matrix4x4 MatrixValue(JObject source)
        {
            if ((string)source["status"] != "published" || source["float_view"] is not JArray values || values.Count != 16)
                throw new InvalidOperationException("原环境矩阵不是完整的已发布值：" + (string)source["name"]);
            var matrix = Matrix4x4.zero;
            for (var column = 0; column < 4; column++)
            {
                var offset = column * 4;
                matrix.SetColumn(column, new Vector4((float)values[offset], (float)values[offset + 1],
                    (float)values[offset + 2], (float)values[offset + 3]));
            }
            return matrix;
        }

        static Vector2 Vector2Value(JObject value) => new((float)value["X"], (float)value["Y"]);

        static Vector4 Vector4Value(JObject value) => new(
            (float)value["r"], (float)value["g"], (float)value["b"], (float)value["a"]);

        static void EnsureFolder(string path)
        {
            var current = "Assets";
            foreach (var segment in path.Split('/').Skip(1))
            {
                var next = current + "/" + segment;
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segment);
                current = next;
            }
        }

        static T Save<T>(T generated, string path) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, path);
                return generated;
            }
            EditorUtility.CopySerialized(generated, existing);
            Object.DestroyImmediate(generated);
            EditorUtility.SetDirty(existing);
            return existing;
        }
    }
}
