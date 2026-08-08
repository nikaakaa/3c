using System;
using UnityEngine;

namespace ThirdPersonRendering.ShapeProjection
{
    public sealed class CharacterShapeProjectionArtifact : ScriptableObject
    {
        [SerializeField] ShapeProjectionArtifactId artifactId;
        [SerializeField] ShapeProjectionProfileId profileId;
        [SerializeField] int profileRevision;
        [SerializeField] Hash128 profileContentHash;
        [SerializeField] Hash128 contentHash;
        [SerializeField] ShapeProjectionCapacity capacity;
        [SerializeField] ShapeProjectionAssetDependency[] dependencies = Array.Empty<ShapeProjectionAssetDependency>();
        [SerializeField] ShapeProjectionRendererRecord[] renderers = Array.Empty<ShapeProjectionRendererRecord>();
        [SerializeField] ShapeProjectionRegionRecord[] regions = Array.Empty<ShapeProjectionRegionRecord>();
        [SerializeField] int[] triangleIndices = Array.Empty<int>();
        [SerializeField] ShapeProjectionSharedChainRecord[] sharedChains = Array.Empty<ShapeProjectionSharedChainRecord>();
        [SerializeField] int[] sharedChainVertexIndices = Array.Empty<int>();
        [SerializeField] int[] regionSharedChainIndices = Array.Empty<int>();
        [SerializeField] int bakeTriangleCount;
        [SerializeField] int excludedTriangleCount;
        [SerializeField] string bakedUtc;

        public ShapeProjectionArtifactId ArtifactId => artifactId;
        public ShapeProjectionProfileId ProfileId => profileId;
        public int ProfileRevision => profileRevision;
        public Hash128 ProfileContentHash => profileContentHash;
        public Hash128 ContentHash => contentHash;
        public ShapeProjectionCapacity Capacity => capacity;
        public ShapeProjectionAssetDependency[] Dependencies => dependencies;
        public ShapeProjectionRendererRecord[] Renderers => renderers;
        public ShapeProjectionRegionRecord[] Regions => regions;
        public int[] TriangleIndices => triangleIndices;
        public ShapeProjectionSharedChainRecord[] SharedChains => sharedChains;
        public int[] SharedChainVertexIndices => sharedChainVertexIndices;
        public int[] RegionSharedChainIndices => regionSharedChainIndices;
        public int BakeTriangleCount => bakeTriangleCount;
        public int ExcludedTriangleCount => excludedTriangleCount;
        public string BakedUtc => bakedUtc;

        public ShapeProjectionValidationResult ValidateArtifact()
        {
            if (!artifactId.IsValid || !profileId.IsValid || profileRevision < 1 || !contentHash.isValid || !profileContentHash.isValid)
                return ShapeProjectionValidationResult.Fail("Artifact identity、revision或hash无效");
            if (!capacity.Validate(out string capacityError))
                return ShapeProjectionValidationResult.Fail(capacityError);
            if (renderers == null || renderers.Length == 0 || renderers.Length > capacity.MaxRenderers)
                return ShapeProjectionValidationResult.Fail("Artifact Renderer数量无效");
            if (regions == null || regions.Length == 0 || regions.Length > capacity.MaxRegions)
                return ShapeProjectionValidationResult.Fail("Artifact Region数量无效");
            if (regions.Length > capacity.MaxIndirectInstances)
                return ShapeProjectionValidationResult.Fail("Artifact Region超过Indirect Instance固定容量");
            if (triangleIndices == null || triangleIndices.Length == 0 || triangleIndices.Length % 3 != 0)
                return ShapeProjectionValidationResult.Fail("Artifact triangle index页面无效");
            if (sharedChains == null || sharedChains.Length > capacity.MaxSharedChains || sharedChainVertexIndices == null
                                     || regionSharedChainIndices == null)
                return ShapeProjectionValidationResult.Fail("Artifact Shared Chain页面无效");
            if (dependencies == null || dependencies.Length == 0)
                return ShapeProjectionValidationResult.Fail("Artifact没有源资产lineage");

            for (int i = 0; i < dependencies.Length; i++)
            {
                ShapeProjectionAssetDependency dependency = dependencies[i];
                if (dependency.Asset == null || string.IsNullOrWhiteSpace(dependency.Guid)
                                             || dependency.LocalId == 0 || !dependency.DependencyHash.isValid)
                    return ShapeProjectionValidationResult.Fail($"Artifact源资产lineage {i}无效");
                for (int j = i + 1; j < dependencies.Length; j++)
                {
                    if (dependency.Guid == dependencies[j].Guid && dependency.LocalId == dependencies[j].LocalId)
                        return ShapeProjectionValidationResult.Fail($"Artifact源资产lineage重复：{dependency.Guid}/{dependency.LocalId}");
                }
            }

            int totalVertices = 0;
            int totalTriangles = 0;
            int totalRegions = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                ShapeProjectionRendererRecord renderer = renderers[i];
                if (string.IsNullOrWhiteSpace(renderer.SlotId) || renderer.SourceMesh == null || renderer.VertexCount < 3
                    || string.IsNullOrWhiteSpace(renderer.MeshGuid) || renderer.MeshLocalId == 0
                    || renderer.SourceMaterials == null || renderer.SourceMaterials.Length == 0
                    || renderer.SourceMaterials.Length != renderer.SourceMesh.subMeshCount)
                    return ShapeProjectionValidationResult.Fail($"Renderer记录{i}无效");
                if (renderer.VertexStart != totalVertices || renderer.TriangleStart != totalTriangles
                                                           || renderer.RegionStart != totalRegions
                                                           || renderer.TriangleCount < 1 || renderer.RegionCount < 1
                                                           || renderer.RegionStart + renderer.RegionCount > regions.Length
                                                           || renderer.TriangleStart + renderer.TriangleCount > triangleIndices.Length / 3)
                    return ShapeProjectionValidationResult.Fail($"Renderer记录{i}范围不连续");
                for (int material = 0; material < renderer.SourceMaterials.Length; material++)
                {
                    if (renderer.SourceMaterials[material] == null)
                        return ShapeProjectionValidationResult.Fail($"Renderer记录{i}包含空Material");
                    if (!HasDependency(ShapeProjectionDependencyKind.Material, renderer.SourceMaterials[material], null, 0))
                        return ShapeProjectionValidationResult.Fail($"Renderer记录{i}的Material未进入源资产lineage");
                }
                if (!HasDependency(ShapeProjectionDependencyKind.Mesh, renderer.SourceMesh,
                        renderer.MeshGuid, renderer.MeshLocalId))
                    return ShapeProjectionValidationResult.Fail($"Renderer记录{i}的Mesh未进入源资产lineage");
                for (int j = i + 1; j < renderers.Length; j++)
                {
                    if (string.Equals(renderer.SlotId, renderers[j].SlotId, StringComparison.Ordinal))
                        return ShapeProjectionValidationResult.Fail($"Renderer SlotId重复：{renderer.SlotId}");
                }
                totalVertices += renderer.VertexCount;
                totalTriangles += renderer.TriangleCount;
                totalRegions += renderer.RegionCount;
            }

            if (totalVertices > capacity.MaxVertices || triangleIndices.Length / 3 > capacity.MaxTriangles
                                                     || totalTriangles != triangleIndices.Length / 3
                                                     || totalRegions != regions.Length
                                                     || bakeTriangleCount != triangleIndices.Length / 3
                                                     || excludedTriangleCount < 0)
                return ShapeProjectionValidationResult.Fail("Artifact顶点或三角超过固定容量");

            for (int i = 0; i < regions.Length; i++)
            {
                ShapeProjectionRegionRecord region = regions[i];
                if (!region.RegionId.IsValid || region.RendererSlot < 0 || region.RendererSlot >= renderers.Length
                                             || region.TriangleStart < 0 || region.TriangleCount < 1
                                             || (region.TriangleStart + region.TriangleCount) * 3 > triangleIndices.Length
                                             || region.SharedChainStart < 0 || region.SharedChainCount < 0
                                             || region.SharedChainStart + region.SharedChainCount > regionSharedChainIndices.Length
                                             || !IsFinite(region.RepresentativeColor))
                    return ShapeProjectionValidationResult.Fail($"Region记录{i}无效");
                ShapeProjectionRendererRecord owner = renderers[region.RendererSlot];
                if (i < owner.RegionStart || i >= owner.RegionStart + owner.RegionCount
                                          || region.TriangleStart < owner.TriangleStart
                                          || region.TriangleStart + region.TriangleCount > owner.TriangleStart + owner.TriangleCount)
                    return ShapeProjectionValidationResult.Fail($"Region记录{i}不属于声明的Renderer范围");
                for (int j = i + 1; j < regions.Length; j++)
                {
                    if (region.RegionId.Equals(regions[j].RegionId))
                        return ShapeProjectionValidationResult.Fail($"RegionId重复：{region.RegionId}");
                }
            }

            for (int i = 0; i < triangleIndices.Length; i++)
            {
                if (triangleIndices[i] < 0 || triangleIndices[i] >= totalVertices)
                    return ShapeProjectionValidationResult.Fail($"Triangle index {i}越界");
            }

            for (int i = 0; i < sharedChains.Length; i++)
            {
                ShapeProjectionSharedChainRecord chain = sharedChains[i];
                if (!chain.ChainId.IsValid || chain.RendererSlot < 0 || chain.RendererSlot >= renderers.Length
                                             || chain.LeftRegion < 0 || chain.LeftRegion >= regions.Length
                                             || chain.RightRegion < -1 || chain.RightRegion >= regions.Length
                                             || chain.VertexStart < 0 || chain.VertexCount < 2
                                             || chain.VertexStart + chain.VertexCount > sharedChainVertexIndices.Length)
                    return ShapeProjectionValidationResult.Fail($"Shared Chain记录{i}无效");
                if (regions[chain.LeftRegion].RendererSlot != chain.RendererSlot
                    || chain.RightRegion >= 0 && regions[chain.RightRegion].RendererSlot != chain.RendererSlot)
                    return ShapeProjectionValidationResult.Fail($"Shared Chain记录{i}跨Renderer");
                for (int j = i + 1; j < sharedChains.Length; j++)
                {
                    if (chain.ChainId.Equals(sharedChains[j].ChainId))
                        return ShapeProjectionValidationResult.Fail($"Shared ChainId重复：{chain.ChainId}");
                }
                ShapeProjectionRendererRecord chainRenderer = renderers[chain.RendererSlot];
                for (int vertex = 0; vertex < chain.VertexCount; vertex++)
                {
                    int vertexIndex = sharedChainVertexIndices[chain.VertexStart + vertex];
                    if (vertexIndex < chainRenderer.VertexStart
                        || vertexIndex >= chainRenderer.VertexStart + chainRenderer.VertexCount)
                        return ShapeProjectionValidationResult.Fail($"Shared Chain记录{i}的顶点越过Renderer范围");
                }
            }

            for (int i = 0; i < regionSharedChainIndices.Length; i++)
            {
                if (regionSharedChainIndices[i] < 0 || regionSharedChainIndices[i] >= sharedChains.Length)
                    return ShapeProjectionValidationResult.Fail($"Region Shared Chain index {i}越界");
            }

            for (int regionIndex = 0; regionIndex < regions.Length; regionIndex++)
            {
                ShapeProjectionRegionRecord region = regions[regionIndex];
                for (int relation = 0; relation < region.SharedChainCount; relation++)
                {
                    ShapeProjectionSharedChainRecord chain = sharedChains[regionSharedChainIndices[region.SharedChainStart + relation]];
                    if (chain.LeftRegion != regionIndex && chain.RightRegion != regionIndex)
                        return ShapeProjectionValidationResult.Fail($"Region记录{regionIndex}引用了无关Shared Chain");
                }
            }

            return ShapeProjectionValidationResult.Success;
        }

#if UNITY_EDITOR
        public void ReplaceContent(
            ShapeProjectionArtifactId newArtifactId,
            ShapeProjectionProfileId newProfileId,
            int newProfileRevision,
            Hash128 newProfileContentHash,
            Hash128 newContentHash,
            ShapeProjectionCapacity newCapacity,
            ShapeProjectionAssetDependency[] newDependencies,
            ShapeProjectionRendererRecord[] newRenderers,
            ShapeProjectionRegionRecord[] newRegions,
            int[] newTriangleIndices,
            ShapeProjectionSharedChainRecord[] newSharedChains,
            int[] newSharedChainVertexIndices,
            int[] newRegionSharedChainIndices,
            int newBakeTriangleCount,
            int newExcludedTriangleCount)
        {
            artifactId = newArtifactId;
            profileId = newProfileId;
            profileRevision = newProfileRevision;
            profileContentHash = newProfileContentHash;
            contentHash = newContentHash;
            capacity = newCapacity;
            dependencies = newDependencies ?? Array.Empty<ShapeProjectionAssetDependency>();
            renderers = newRenderers ?? Array.Empty<ShapeProjectionRendererRecord>();
            regions = newRegions ?? Array.Empty<ShapeProjectionRegionRecord>();
            triangleIndices = newTriangleIndices ?? Array.Empty<int>();
            sharedChains = newSharedChains ?? Array.Empty<ShapeProjectionSharedChainRecord>();
            sharedChainVertexIndices = newSharedChainVertexIndices ?? Array.Empty<int>();
            regionSharedChainIndices = newRegionSharedChainIndices ?? Array.Empty<int>();
            bakeTriangleCount = newBakeTriangleCount;
            excludedTriangleCount = newExcludedTriangleCount;
            bakedUtc = DateTime.UtcNow.ToString("O");
        }
#endif

        static bool IsFinite(Color value)
        {
            return IsFinite(value.r) && IsFinite(value.g) && IsFinite(value.b) && IsFinite(value.a);
        }

        bool HasDependency(ShapeProjectionDependencyKind kind, UnityEngine.Object asset, string guid, long localId)
        {
            for (int i = 0; i < dependencies.Length; i++)
            {
                ShapeProjectionAssetDependency dependency = dependencies[i];
                if (dependency.Kind == kind && dependency.Asset == asset
                    && (string.IsNullOrEmpty(guid) || dependency.Guid == guid && dependency.LocalId == localId))
                    return true;
            }
            return false;
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
