using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ThirdPersonRendering.ShapeProjection.Editor
{
    sealed class ShapeProjectionEditorMeshData
    {
        public Vector3[] Vertices;
        public Vector2[] Uv;
        public int[][] SubmeshIndices;

        public static ShapeProjectionEditorMeshData Read(Mesh mesh)
        {
            if (mesh == null)
                throw new InvalidOperationException("不能读取空Mesh");

            using (Mesh.MeshDataArray array = Mesh.AcquireReadOnlyMeshData(mesh))
            {
                Mesh.MeshData data = array[0];
                NativeArray<Vector3> vertices = new NativeArray<Vector3>(data.vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                NativeArray<Vector2> uv = new NativeArray<Vector2>(data.vertexCount, Allocator.Temp, NativeArrayOptions.ClearMemory);
                try
                {
                    data.GetVertices(vertices);
                    if (data.HasVertexAttribute(VertexAttribute.TexCoord0))
                        data.GetUVs(0, uv);

                    int[][] submeshIndices = new int[data.subMeshCount][];
                    for (int submesh = 0; submesh < data.subMeshCount; submesh++)
                    {
                        SubMeshDescriptor descriptor = data.GetSubMesh(submesh);
                        if (descriptor.topology != MeshTopology.Triangles)
                            throw new InvalidOperationException($"Mesh {mesh.name}的Submesh {submesh}不是Triangle topology");
                        NativeArray<int> indices = new NativeArray<int>(descriptor.indexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                        try
                        {
                            data.GetIndices(indices, submesh, true);
                            submeshIndices[submesh] = indices.ToArray();
                        }
                        finally
                        {
                            indices.Dispose();
                        }
                    }

                    return new ShapeProjectionEditorMeshData
                    {
                        Vertices = vertices.ToArray(),
                        Uv = uv.ToArray(),
                        SubmeshIndices = submeshIndices
                    };
                }
                finally
                {
                    vertices.Dispose();
                    uv.Dispose();
                }
            }
        }
    }

    sealed class ShapeProjectionMaterialSamplerCache : IDisposable
    {
        sealed class Sampler
        {
            public Color BaseColor;
            public Texture2D Texture;
            public Vector2 Scale;
            public Vector2 Offset;
        }

        static readonly Vector3[] SampleWeights =
        {
            new Vector3(0.6f, 0.2f, 0.2f),
            new Vector3(0.2f, 0.6f, 0.2f),
            new Vector3(0.2f, 0.2f, 0.6f),
            new Vector3(1f / 3f, 1f / 3f, 1f / 3f)
        };

        readonly Dictionary<Material, Sampler> samplers = new Dictionary<Material, Sampler>();

        public ShapeProjectionMaterialSamplerCache()
        {
        }

        public bool TrySampleTriangle(ShapeProjectionSubmeshRule rule, Vector2 uv0, Vector2 uv1, Vector2 uv2,
            out Color color)
        {
            if (rule.Mode == ShapeProjectionMaterialMode.Exclude)
            {
                color = default;
                return false;
            }
            Sampler sampler = GetSampler(rule.Material);

            Color sum = Color.clear;
            for (int i = 0; i < SampleWeights.Length; i++)
            {
                Vector3 weights = SampleWeights[i];
                Vector2 uv = uv0 * weights.x + uv1 * weights.y + uv2 * weights.z;
                uv = Vector2.Scale(uv, sampler.Scale) + sampler.Offset;
                Color sample = sampler.Texture != null ? sampler.Texture.GetPixelBilinear(uv.x, uv.y) : Color.white;
                sum += sample * sampler.BaseColor;
            }

            color = sum / SampleWeights.Length;
            if (rule.Mode == ShapeProjectionMaterialMode.IncludeCutout
                && color.a < rule.AlphaThreshold)
                return false;

            if (rule.OverrideRepresentativeColor)
            {
                Color replacement = rule.RepresentativeColor;
                replacement.a = color.a;
                color = replacement;
            }

            return true;
        }

        public void Dispose()
        {
            foreach (KeyValuePair<Material, Sampler> pair in samplers)
            {
                if (pair.Value.Texture != null)
                    UnityEngine.Object.DestroyImmediate(pair.Value.Texture);
            }
            samplers.Clear();
        }

        Sampler GetSampler(Material material)
        {
            if (material == null)
                throw new InvalidOperationException("Renderer包含空Material");
            if (samplers.TryGetValue(material, out Sampler sampler))
                return sampler;
            string textureProperty = material.HasProperty("_BaseMap") ? "_BaseMap"
                : material.HasProperty("_MainTex") ? "_MainTex"
                : string.Empty;
            string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor"
                : material.HasProperty("_Color") ? "_Color"
                : string.Empty;

            Texture texture = string.IsNullOrEmpty(textureProperty) ? null : material.GetTexture(textureProperty);
            sampler = new Sampler
            {
                BaseColor = string.IsNullOrEmpty(colorProperty) ? Color.white : material.GetColor(colorProperty),
                Texture = texture == null ? null : CreateReadableCopy(texture),
                Scale = string.IsNullOrEmpty(textureProperty) ? Vector2.one : material.GetTextureScale(textureProperty),
                Offset = string.IsNullOrEmpty(textureProperty) ? Vector2.zero : material.GetTextureOffset(textureProperty)
            };
            samplers.Add(material, sampler);
            return sampler;
        }

        static Texture2D CreateReadableCopy(Texture source)
        {
            int width = source.width;
            int height = source.height;
            RenderTexture temporary = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;
                Texture2D copy = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
                {
                    name = $"{source.name}_ShapeProjectionBakeCopy",
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = source.wrapMode,
                    filterMode = FilterMode.Bilinear
                };
                copy.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                copy.Apply(false, false);
                return copy;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);
            }
        }
    }

    static class ShapeProjectionAssetIdentity
    {
        public static void Get(UnityEngine.Object asset, out string guid, out long localId)
        {
            if (asset == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out guid, out localId))
                throw new InvalidOperationException($"无法解析资产identity：{asset}");
        }

        public static ShapeProjectionAssetDependency Create(ShapeProjectionDependencyKind kind, UnityEngine.Object asset)
        {
            Get(asset, out string guid, out long localId);
            string path = AssetDatabase.GetAssetPath(asset);
            Hash128 hash = AssetDatabase.GetAssetDependencyHash(path);
            if (!hash.isValid)
                throw new InvalidOperationException($"无法计算资产dependency hash：{path}");
            return new ShapeProjectionAssetDependency
            {
                Kind = kind,
                Asset = asset,
                Guid = guid,
                LocalId = localId,
                DependencyHash = hash
            };
        }
    }

    static class ShapeProjectionDependencyValidator
    {
        public static ShapeProjectionValidationResult Validate(CharacterShapeProjectionArtifact artifact)
        {
            ShapeProjectionValidationResult structure = artifact.ValidateArtifact();
            if (!structure.IsValid)
                return structure;
            ShapeProjectionAssetDependency[] dependencies = artifact.Dependencies;
            for (int i = 0; i < dependencies.Length; i++)
            {
                ShapeProjectionAssetDependency expected = dependencies[i];
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(expected.Asset, out string guid, out long localId)
                    || guid != expected.Guid || localId != expected.LocalId)
                    return ShapeProjectionValidationResult.Fail($"源资产identity已变化：{expected.Asset}");
                Hash128 current = AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(expected.Asset));
                if (current != expected.DependencyHash)
                    return ShapeProjectionValidationResult.Fail($"源资产内容已变化，Artifact已Stale：{expected.Asset.name}");
            }
            return ShapeProjectionValidationResult.Success;
        }
    }
}
