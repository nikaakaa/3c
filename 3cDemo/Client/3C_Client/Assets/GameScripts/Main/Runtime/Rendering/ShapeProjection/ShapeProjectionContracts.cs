using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ThirdPersonRendering.ShapeProjection
{
    public enum ShapeProjectionRuntimeState
    {
        Stale = 0,
        Ready = 1,
        WaitingForFirstCompatibleResult = 2,
        Faulted = 3,
        Disabled = 4
    }

    public enum ShapeProjectionDebugView
    {
        Final = 0,
        RegionBounds = 1,
        AtlasMask = 2,
        SharedAnchors = 3,
        SimplifiedLoops = 4,
        CompletedDepth = 5
    }

    public enum ShapeProjectionMaterialMode
    {
        IncludeOpaque = 0,
        IncludeCutout = 1,
        Exclude = 2
    }

    public enum ShapeProjectionDependencyKind
    {
        Mesh = 0,
        Material = 1,
        Texture = 2
    }

    [Serializable]
    public struct ShapeProjectionAssetDependency
    {
        public ShapeProjectionDependencyKind Kind;
        public UnityEngine.Object Asset;
        public string Guid;
        public long LocalId;
        public Hash128 DependencyHash;
    }

    [Serializable]
    public struct ShapeProjectionSubmeshRule
    {
        [SerializeField] string rendererSlotId;
        [SerializeField, Min(0)] int submeshIndex;
        [SerializeField] Material material;
        [SerializeField] ShapeProjectionMaterialMode mode;
        [SerializeField] float alphaThreshold;
        [SerializeField] bool overrideRepresentativeColor;
        [SerializeField] Color representativeColor;

        public string RendererSlotId => rendererSlotId;
        public int SubmeshIndex => submeshIndex;
        public Material Material => material;
        public ShapeProjectionMaterialMode Mode => mode;
        public float AlphaThreshold => alphaThreshold;
        public bool OverrideRepresentativeColor => overrideRepresentativeColor;
        public Color RepresentativeColor => representativeColor;

        public ShapeProjectionSubmeshRule(string rendererSlotId, int submeshIndex, Material material,
            ShapeProjectionMaterialMode mode, float alphaThreshold, bool overrideRepresentativeColor,
            Color representativeColor)
        {
            this.rendererSlotId = rendererSlotId;
            this.submeshIndex = submeshIndex;
            this.material = material;
            this.mode = mode;
            this.alphaThreshold = alphaThreshold;
            this.overrideRepresentativeColor = overrideRepresentativeColor;
            this.representativeColor = representativeColor;
        }
    }

    [Serializable]
    public struct ShapeProjectionCapacity
    {
        [Min(1)] public int MaxRenderers;
        [Min(3)] public int MaxVertices;
        [Min(1)] public int MaxTriangles;
        [Min(1)] public int MaxRegions;
        [Min(1)] public int MaxSharedChains;
        [Min(64)] public int AtlasWidth;
        [Min(64)] public int AtlasHeight;
        [Min(3)] public int MaxContourPoints;
        [Min(1)] public int MaxLoops;
        [Min(1)] public int MaxIndirectInstances;
        [Range(2, 4)] public int ReadbackSlots;

        public bool Validate(out string error)
        {
            if (MaxRenderers < 1 || MaxVertices < 3 || MaxTriangles < 1 || MaxRegions < 1 || MaxSharedChains < 1)
            {
                error = "Renderer、顶点、三角、Region或Shared Chain容量无效";
                return false;
            }

            if (AtlasWidth < 64 || AtlasHeight < 64 || MaxContourPoints < 3 || MaxLoops < 1
                || MaxIndirectInstances < 1 || MaxIndirectInstances > MaxRegions)
            {
                error = "Atlas、轮廓点、Loop或Indirect Instance容量无效";
                return false;
            }

            if (ReadbackSlots < 2 || ReadbackSlots > 4)
            {
                error = "Readback Slot数量必须处于2到4";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public bool Equals(ShapeProjectionCapacity other)
        {
            return MaxRenderers == other.MaxRenderers && MaxVertices == other.MaxVertices
                   && MaxTriangles == other.MaxTriangles && MaxRegions == other.MaxRegions
                   && MaxSharedChains == other.MaxSharedChains && AtlasWidth == other.AtlasWidth
                   && AtlasHeight == other.AtlasHeight && MaxContourPoints == other.MaxContourPoints
                   && MaxLoops == other.MaxLoops && MaxIndirectInstances == other.MaxIndirectInstances
                   && ReadbackSlots == other.ReadbackSlots;
        }
    }

    [Serializable]
    public struct ShapeProjectionRendererRecord
    {
        public string SlotId;
        public Mesh SourceMesh;
        public string MeshGuid;
        public long MeshLocalId;
        public int VertexStart;
        public int VertexCount;
        public int TriangleStart;
        public int TriangleCount;
        public int RegionStart;
        public int RegionCount;
        public Material[] SourceMaterials;
    }

    [Serializable]
    public struct ShapeProjectionRegionRecord
    {
        public ShapeProjectionRegionId RegionId;
        public int RendererSlot;
        public int TriangleStart;
        public int TriangleCount;
        public int SharedChainStart;
        public int SharedChainCount;
        public Color RepresentativeColor;
        public string Label;
    }

    [Serializable]
    public struct ShapeProjectionSharedChainRecord
    {
        public ShapeProjectionChainId ChainId;
        public int RendererSlot;
        public int LeftRegion;
        public int RightRegion;
        public int VertexStart;
        public int VertexCount;
    }

    [Serializable]
    public struct ShapeProjectionAtlasRect
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public bool IsValid => Width > 0 && Height > 0;
    }

    [Serializable]
    public struct ShapeProjectionLoopRange
    {
        public int RegionIndex;
        public int PointStart;
        public int PointCount;
    }

    [Serializable]
    public struct ShapeProjectionResultIdentity : IEquatable<ShapeProjectionResultIdentity>
    {
        public ShapeProjectionSourceId SourceId;
        public int SourceGeneration;
        public int CameraInstanceId;
        public Hash128 ProjectionHash;
        public int ViewportWidth;
        public int ViewportHeight;
        public ShapeProjectionProfileId ProfileId;
        public int ProfileRevision;
        public int ProfileRuntimeTuningRevision;
        public ShapeProjectionArtifactId ArtifactId;
        public Hash128 ArtifactHash;
        public int RenderFrame;
        public ulong SubmissionSequence;
        public int SlotGeneration;

        public bool Equals(ShapeProjectionResultIdentity other)
        {
            return SourceId.Equals(other.SourceId)
                   && SourceGeneration == other.SourceGeneration
                   && CameraInstanceId == other.CameraInstanceId
                   && ProjectionHash == other.ProjectionHash
                   && ViewportWidth == other.ViewportWidth
                   && ViewportHeight == other.ViewportHeight
                   && ProfileId.Equals(other.ProfileId)
                   && ProfileRevision == other.ProfileRevision
                   && ProfileRuntimeTuningRevision == other.ProfileRuntimeTuningRevision
                   && ArtifactId.Equals(other.ArtifactId)
                   && ArtifactHash == other.ArtifactHash
                   && RenderFrame == other.RenderFrame
                   && SubmissionSequence == other.SubmissionSequence
                   && SlotGeneration == other.SlotGeneration;
        }

        public override bool Equals(object obj) => obj is ShapeProjectionResultIdentity other && Equals(other);
        public override int GetHashCode() => SourceId.GetHashCode() ^ CameraInstanceId ^ SlotGeneration;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ShapeProjectionProjectedVertex
    {
        public Vector2 Screen;
        public float Depth;
        public float ClipW;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ShapeProjectionRegionGpu
    {
        public Vector4 ScreenRect;
        public Vector4 AtlasRect;
        public Vector4 Color;
        public Vector4 Meta;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ShapeProjectionCompositeRegionGpu
    {
        public Vector4 ScreenRect;
        public Vector4 AtlasRect;
        public Vector4 Color;
        public Vector4 LoopRange;
    }

    public readonly struct ShapeProjectionValidationResult
    {
        public readonly bool IsValid;
        public readonly string Error;

        public ShapeProjectionValidationResult(bool isValid, string error)
        {
            IsValid = isValid;
            Error = error ?? string.Empty;
        }

        public static ShapeProjectionValidationResult Success => new ShapeProjectionValidationResult(true, string.Empty);
        public static ShapeProjectionValidationResult Fail(string error) => new ShapeProjectionValidationResult(false, error);
    }
}
