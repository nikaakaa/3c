using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ZZZ.Rendering.Restored.Editor
{
    public static class CorinRestoredLutImporter
    {
        const string Root = "Assets/Render/ZZZRestored/Generated/CharacterLut";

        [Serializable]
        sealed class Parameter
        {
            public string name;
            public float[] values;
        }

        [Serializable]
        sealed class Curve
        {
            public string name;
            public int width;
            public int height;
            public int format;
            public int filterMode;
            public int wrapMode;
            public string rawBase64;
        }

        [Serializable]
        sealed class CurveBinding
        {
            public string name;
            public int textureIndex;
        }

        [Serializable]
        sealed class Source
        {
            public int schemaVersion;
            public int lutSize;
            public string keyword;
            public bool userLutEnabled;
            public Parameter[] parameters;
            public Curve[] textures;
            public CurveBinding[] curveBindings;
        }

        [Serializable]
        sealed class Result
        {
            public string shader;
            public string material;
            public string texture;
            public int width;
            public int height;
            public int nonFinitePixels;
            public bool hasColorVariation;
            public Color firstPixel;
            public Vector4 inputLutParameters;
            public string[] compilerMessages;
        }

        [MenuItem("Tools/ZZZ/Restored/Bake Original Character LUT")]
        public static void Bake()
        {
            var source = JsonUtility.FromJson<Source>(File.ReadAllText(Root + "/CorinSourceLut.json", Encoding.UTF8));
            if (source.schemaVersion != 1 || source.lutSize != 32 || source.keyword != "_TONEMAP_CUSTOM" || source.userLutEnabled)
                throw new InvalidOperationException("源 LUT 配置不是已核实的 ZZZ 角色 HDR 通道。");
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/CharacterLutHDR.shader");
            if (shader == null)
                throw new InvalidOperationException("还原 LUT Shader 尚未导入。");
            var pass = ShaderUtil.GetShaderData(shader).GetSubshader(0).GetPass(0);
            var vertex = pass.CompileVariant(ShaderType.Vertex, Array.Empty<string>(), ShaderCompilerPlatform.D3D, BuildTarget.StandaloneWindows64);
            var fragment = pass.CompileVariant(ShaderType.Fragment, Array.Empty<string>(), ShaderCompilerPlatform.D3D, BuildTarget.StandaloneWindows64);
            var messages = vertex.Messages.Concat(fragment.Messages).Select(m => m.message).ToArray();
            if (!vertex.Success || !fragment.Success)
                throw new InvalidOperationException(string.Join("\n", messages));

            var material = new Material(shader) { name = "CorinOriginalLutHDR" };
            var curves = new Texture2D[source.textures.Length];
            for (var index = 0; index < curves.Length; index++)
            {
                var input = source.textures[index];
                var curve = new Texture2D(input.width, input.height, (TextureFormat)input.format, false, true)
                {
                    name = input.name,
                    filterMode = (FilterMode)input.filterMode,
                    wrapMode = (TextureWrapMode)input.wrapMode
                };
                curve.LoadRawTextureData(Convert.FromBase64String(input.rawBase64));
                curve.Apply(false, false);
                curves[index] = Save(curve, Root + "/" + input.name + ".asset");
            }
            material = Save(material, Root + "/CorinOriginalLutHDR.mat");
            foreach (var parameter in source.parameters)
            {
                if (parameter.values.Length == 1)
                    material.SetFloat(parameter.name, parameter.values[0]);
                else
                    material.SetVector(parameter.name, new Vector4(parameter.values[0], parameter.values[1], parameter.values[2], parameter.values[3]));
            }
            material.SetVector("_UserLut_Params", Vector4.zero);
            foreach (var binding in source.curveBindings)
                material.SetTexture(binding.name, curves[binding.textureIndex]);
            EditorUtility.SetDirty(material);

            var width = source.lutSize * source.lutSize;
            var height = source.lutSize;
            var target = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear)
            {
                name = "CorinOriginalLutBake",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var previous = RenderTexture.active;
            Texture2D output = null;
            try
            {
                target.Create();
                Graphics.Blit(null, target, material, 0);
                RenderTexture.active = target;
                output = new Texture2D(width, height, TextureFormat.RGBAHalf, false, true)
                {
                    name = "CorinOriginalCharacterLut",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                output.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                output.Apply(false, false);
                var pixels = output.GetPixels();
                var nonFinite = pixels.Count(p => !Finite(p.r) || !Finite(p.g) || !Finite(p.b) || !Finite(p.a));
                var variation = pixels.Any(p => p.r != pixels[0].r || p.g != pixels[0].g || p.b != pixels[0].b);
                var directory = Path.GetFullPath("Diagnostics/Rendering/ZZZRestored/LutBakes/" +
                    DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
                Directory.CreateDirectory(directory);
                var preview = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
                preview.SetPixels(pixels);
                preview.Apply(false, false);
                File.WriteAllBytes(Path.Combine(directory, "CorinOriginalCharacterLut.png"), preview.EncodeToPNG());
                Object.DestroyImmediate(preview);
                File.WriteAllBytes(Path.Combine(directory, "LutVertex.bin"), vertex.ShaderData);
                File.WriteAllBytes(Path.Combine(directory, "LutFragment.bin"), fragment.ShaderData);
                var result = new Result
                {
                    shader = AssetDatabase.GetAssetPath(shader), material = AssetDatabase.GetAssetPath(material),
                    texture = Root + "/CorinOriginalCharacterLut.asset", width = width, height = height,
                    nonFinitePixels = nonFinite, hasColorVariation = variation, compilerMessages = messages,
                    firstPixel = pixels[0], inputLutParameters = material.GetVector("_Lut_Params")
                };
                File.WriteAllText(Path.Combine(directory, "lut-bake.json"), JsonUtility.ToJson(result, true), Encoding.UTF8);
                if (nonFinite != 0 || !variation)
                    throw new InvalidOperationException($"LUT 实际回读失败：非有限像素 {nonFinite}，颜色变化 {variation}，首像素 {pixels[0]}。");
                output = Save(output, result.texture);
                AssetDatabase.SaveAssets();
                Debug.Log("ZZZ 原角色 LUT 已由还原程序实际绘制并回读：" + result.texture);
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
                Object.DestroyImmediate(target);
                if (output != null && !AssetDatabase.Contains(output))
                    Object.DestroyImmediate(output);
            }
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

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
