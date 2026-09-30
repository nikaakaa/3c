using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZZZ.Rendering.Restored.Editor
{
    public static class CorinShaderPropertyMapper
    {
        const string OutputPath =
            @"D:\ZZZ_Dump\output\corin_replication\20260930_effect_shader_recovery_v1\shader_property_offsets.json";

        [MenuItem("Tools/ZZZ/Restored/Dump Corin Effect Shader Property Offsets")]
        public static void Dump()
        {
            var shaderRoot = "Assets/Render/ZZZRestored/Generated/Effect";
            var result = new Dictionary<string, Dictionary<string, object>>(StringComparer.Ordinal);
            foreach (var guid in AssetDatabase.FindAssets("t:Shader", new[] { shaderRoot }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shader == null) continue;
                var props = new Dictionary<string, object>(StringComparer.Ordinal);
                var count = shader.GetPropertyCount();
                for (var i = 0; i < count; i++)
                {
                    var name = shader.GetPropertyName(i);
                    var type = shader.GetPropertyType(i);
                    var attributes = shader.GetPropertyAttributes(i);
                    var entry = new Dictionary<string, object>
                    {
                        ["type"] = type.ToString(),
                        ["attributes"] = attributes
                    };
                    if (type == ShaderPropertyType.Texture)
                    {
                        var texDim = shader.GetPropertyTextureDimension(i);
                        entry["dimension"] = texDim.ToString();
                    }
                    props[name] = entry;
                }
                result[Path.GetFileNameWithoutExtension(path)] = props;
            }
            var json = JsonSerializerSerialize(result);
            File.WriteAllText(OutputPath, json, Encoding.UTF8);
            Debug.Log($"Shader property offsets dumped: {result.Count} shaders -> {OutputPath}");
        }

        static string JsonSerializerSerialize(Dictionary<string, Dictionary<string, object>> data)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            var shaderKeys = data.Keys.OrderBy(k => k).ToArray();
            for (var si = 0; si < shaderKeys.Length; si++)
            {
                var shaderName = shaderKeys[si];
                sb.Append($"  \"{shaderName}\": {{\n");
                var props = data[shaderName];
                var propKeys = props.Keys.OrderBy(k => k).ToArray();
                for (var pi = 0; pi < propKeys.Length; pi++)
                {
                    var propName = propKeys[pi];
                    var entry = (Dictionary<string, object>)props[propName];
                    var type = (string)entry["type"];
                    var attrs = (string[])entry["attributes"];
                    var attrStr = string.Join(",", attrs.Select(a => $"\"{a}\""));
                    sb.Append($"    \"{propName}\": {{\"type\": \"{type}\"");
                    if (entry.ContainsKey("dimension"))
                        sb.Append($", \"dimension\": \"{entry["dimension"]}\"");
                    sb.Append($", \"attributes\": [{attrStr}]}}");
                    sb.Append(pi < propKeys.Length - 1 ? ",\n" : "\n");
                }
                sb.Append(si < shaderKeys.Length - 1 ? "  },\n" : "  }\n");
            }
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
