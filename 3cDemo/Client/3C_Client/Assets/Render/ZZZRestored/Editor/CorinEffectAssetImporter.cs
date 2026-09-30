using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using ZZZ.Rendering.Restored;
using Object = UnityEngine.Object;

namespace ZZZ.Rendering.Restored.Editor
{
    public static class CorinEffectAssetImporter
    {
        const string ReportPath =
            @"D:\ZZZ_Dump\output\corin_replication\20260930_effect_material_texture_export_v34\effect-material-texture-export.json";
        const string OutputRoot = "Assets/AssetArt/Effect/ZZZ/Corin";
        const string TextureRoot = OutputRoot + "/Textures";
        const string MaterialRoot = OutputRoot + "/Materials";
        const string SetPath = OutputRoot + "/CorinEffectMaterialSet.asset";

        [MenuItem("Tools/ZZZ/Restored/Rebuild Corin Effect Assets")]
        public static void Rebuild()
        {
            EnsureFolder(TextureRoot);
            EnsureFolder(MaterialRoot);
            var report = JObject.Parse(File.ReadAllText(ReportPath, Encoding.UTF8));
            if ((string)report["Schema"] != "zzz-effect-material-texture-export/2")
                throw new InvalidOperationException("特效材质来源合同版本不匹配。");
            if ((int)report["ErrorCount"] != 0)
                throw new InvalidOperationException("特效导出报告包含解析错误，不能导入。");

            var textures = ImportTextures(report);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var shaderMap = BuildShaderMap(report);
            var materials = ImportMaterials(report, textures, shaderMap);
            SaveMaterialSet(materials);
            AssetDatabase.SaveAssets();
            Debug.Log($"可琳特效资源已重建：{textures.Count} 纹理、{materials.Count} 材质 -> {SetPath}");
        }

        static Dictionary<string, Texture2D> ImportTextures(JObject report)
        {
            var textures = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
            foreach (var token in (JArray)report["Textures"])
            {
                var source = (JObject)token;
                var name = (string)source["Name"];
                var format = Enum.Parse<TextureFormat>((string)source["Format"]);
                if (!SystemInfo.SupportsTextureFormat(format))
                    throw new InvalidOperationException($"当前图形设备不支持原纹理格式 {format}：{name}");
                var bytes = File.ReadAllBytes(
                    Path.GetDirectoryName(ReportPath) + "/" + (string)source["OutputFile"]);
                var generated = new Texture2D((int)source["Width"], (int)source["Height"], format,
                    (int)source["MipCount"], false)
                {
                    name = name,
                    filterMode = (FilterMode)(int)source["FilterMode"],
                    anisoLevel = (int)source["Aniso"],
                    mipMapBias = (float)source["MipBias"],
                    wrapMode = (TextureWrapMode)(int)source["WrapMode"]
                };
                generated.LoadRawTextureData(bytes);
                generated.Apply(false, true);
                textures.Add(name, Save(generated, TextureRoot + "/" + name + ".asset"));
            }
            return textures;
        }

        static Dictionary<string, Shader> BuildShaderMap(JObject report)
        {
            const string shaderRoot = "Assets/Render/ZZZRestored/Generated/Effect";
            var shaderNames = ((JArray)report["Shaders"])
                .Select(token => (string)token["Name"])
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var map = new Dictionary<string, Shader>(StringComparer.Ordinal);
            var missing = new List<string>();
            foreach (var name in shaderNames)
            {
                var safeName = name.Replace("/", "_").Replace(" ", "_");
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderRoot + "/" + safeName + ".shader");
                if (shader == null)
                    missing.Add(name);
                else
                    map.Add(name, shader);
            }
            if (missing.Count > 0)
                throw new InvalidOperationException(
                    "以下特效 Shader 在项目中不存在，需先创建 .shader 源文件：\n" +
                    string.Join("\n", missing));
            return map;
        }

        static Dictionary<string, Material> ImportMaterials(JObject report,
            Dictionary<string, Texture2D> textures, Dictionary<string, Shader> shaderMap)
        {
            var textureByKey = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
            foreach (var token in (JArray)report["Textures"])
            {
                var source = (JObject)token;
                var key = (string)source["Cab"] + ":" + (long)source["PathId"];
                textureByKey.Add(key, textures[(string)source["Name"]]);
            }

            var shaderByKey = new Dictionary<string, Shader>(StringComparer.Ordinal);
            foreach (var token in (JArray)report["Shaders"])
            {
                var source = (JObject)token;
                var key = (string)source["Cab"] + ":" + (long)source["PathId"];
                shaderByKey.Add(key, shaderMap[(string)source["Name"]]);
            }

            var materials = new Dictionary<string, Material>(StringComparer.Ordinal);
            foreach (var token in (JArray)report["Materials"])
            {
                var source = (JObject)token;
                var name = (string)source["Name"];
                var shaderKey = (string)source["Shader"]["TargetCab"] + ":" + (long)source["Shader"]["PathId"];
                var shader = shaderByKey[shaderKey];
                var material = Save(new Material(shader) { name = name },
                    MaterialRoot + "/" + name + ".mat");
                material.shader = shader;
                material.shaderKeywords = ParseKeywords((string)source["ShaderKeywords"]);
                material.renderQueue = (int?)source["CustomRenderQueue"] ?? -1;
                material.enableInstancing = (bool)source["EnableInstancingVariants"];
                ApplyValues(material, source, textureByKey);
                ApplyDisabledPasses(material, source);
                EditorUtility.SetDirty(material);
                var sourceKey = (string)source["Cab"] + ":" + (long)source["PathId"];
                materials.Add(sourceKey, material);
            }
            return materials;
        }

        static string[] ParseKeywords(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<string>();
            return value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

        static void ApplyValues(Material material, JObject source,
            Dictionary<string, Texture2D> textureByKey)
        {
            if (source["Floats"] is JArray floats)
                foreach (var property in floats)
                    material.SetFloat((string)property["Name"], (float)property["Value"]);
            if (source["Colors"] is JArray colors)
                foreach (var property in colors)
                    material.SetVector((string)property["Name"], ColorValue((JObject)property));
            if (source["TextureEnvs"] is JArray texEnvs)
            {
                foreach (var property in texEnvs)
                {
                    var env = (JObject)property;
                    if ((string)env["TargetState"] != "Texture2D")
                        continue;
                    var key = (string)env["Cab"] + ":" + (long)env["PathId"];
                    material.SetTexture((string)env["Name"], textureByKey[key]);
                    material.SetTextureScale((string)env["Name"], new Vector2((float)env["ScaleX"], (float)env["ScaleY"]));
                    material.SetTextureOffset((string)env["Name"], new Vector2((float)env["OffsetX"], (float)env["OffsetY"]));
                }
            }
        }

        static void ApplyDisabledPasses(Material material, JObject source)
        {
            if (source["DisabledShaderPasses"] is JArray disabled)
                foreach (var pass in disabled.Values<string>())
                    material.SetShaderPassEnabled(pass, false);
        }

        static Color ColorValue(JObject source) => new(
            (float)source["R"], (float)source["G"], (float)source["B"], (float)source["A"]);

        static void SaveMaterialSet(Dictionary<string, Material> materials)
        {
            var set = AssetDatabase.LoadAssetAtPath<CorinEffectMaterialSet>(SetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<CorinEffectMaterialSet>();
                AssetDatabase.CreateAsset(set, SetPath);
            }
            set.Initialize(materials.Select(pair => new CorinEffectMaterialSet.Entry
            {
                SourceKey = pair.Key,
                Material = pair.Value
            }));
            EditorUtility.SetDirty(set);
        }

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
