using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using AnimeStudio;

var rawPath = args[0];
var outputPath = args[1];
long? pathId = args.Length > 2 && long.TryParse(args[2], out var requestedPathId) ? requestedPathId : null;

var assetsManager = new AssetsManager
{
    Game = GameManager.GetGame("ZZZ") as Mhy ?? throw new InvalidOperationException("ZZZ game definition unavailable"),
    Silent = true,
    SkipProcess = false,
    SkipPostProcess = true
};
assetsManager.LoadFiles(rawPath);
var shaderObject = assetsManager.assetsFileList
    .SelectMany(file => file.Objects)
    .FirstOrDefault(o => o.type == ClassIDType.Shader && (pathId == null || o.m_PathID == pathId));
if (shaderObject == null)
{
    Console.Error.WriteLine("No Shader object found.");
    return 1;
}

var shader = (Shader)shaderObject;
var result = new Dictionary<string, object>
{
    ["platforms"] = shader.platforms.Select(p => p.ToString()).ToArray(),
    ["compressedBlob"] = Convert.ToBase64String(shader.compressedBlob),
    ["offsets"] = shader.offsets,
    ["compressedLengths"] = shader.compressedLengths,
    ["decompressedLengths"] = shader.decompressedLengths,
    ["m_ParsedForm"] = Serializer.Serialize(shader.m_ParsedForm)
};

var options = new JsonSerializerOptions { WriteIndented = false, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
File.WriteAllText(outputPath, JsonSerializer.Serialize(result, options));
Console.WriteLine($"Written {outputPath}");
return 0;

static class Serializer
{
    public static object Serialize(object value)
    {
        if (value == null) return null;
        var type = value.GetType();
        if (type.IsPrimitive || value is string || value is decimal) return value;
        if (value is byte[] bytes) return Convert.ToBase64String(bytes);
        if (value is Array array)
        {
            var list = new List<object>();
            foreach (var item in array) list.Add(Serialize(item));
            return list;
        }
        if (typeof(System.Collections.IEnumerable).IsInstanceOfType(value) && value is not string)
        {
            var list = new List<object>();
            foreach (var item in (System.Collections.IEnumerable)value) list.Add(Serialize(item));
            return list;
        }
        var dict = new Dictionary<string, object>();
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            dict[field.Name] = Serialize(field.GetValue(value));
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.CanRead && prop.GetIndexParameters().Length == 0)
                dict[prop.Name] = Serialize(prop.GetValue(value));
        }
        return dict;
    }
}

