using System;
using UnityEngine.Profiling;

namespace ThirdPersonRendering.ShapeProjection
{
    [Serializable]
    public struct ShapeProjectionDiagnosticsSnapshot
    {
        public ShapeProjectionSourceId SourceId;
        public int SourceGeneration;
        public int CameraInstanceId;
        public ShapeProjectionProfileId ProfileId;
        public int ProfileRevision;
        public ShapeProjectionArtifactId ArtifactId;
        public UnityEngine.Hash128 ArtifactHash;
        public UnityEngine.Hash128 ProjectionHash;
        public int SlotGeneration;
        public ulong LastSubmissionSequence;
        public int LastSubmissionFrame;
        public int DisplayFrame;
        public int DisplayResultFrame;
        public int ResultAgeFrames;
        public int OccupiedSlots;
        public int SkippedSubmissions;
        public int RendererCount;
        public int VertexCount;
        public int VertexCapacity;
        public int TriangleCount;
        public int TriangleCapacity;
        public int SharedChainCount;
        public int SharedChainCapacity;
        public int RegionCapacity;
        public int ActiveRegions;
        public int FilteredRegions;
        public int AtlasWidth;
        public int AtlasHeight;
        public int AtlasUsedPixels;
        public int ContourPointCount;
        public int ContourPointCapacity;
        public int LoopCount;
        public int LoopCapacity;
        public int IndirectInstanceCount;
        public int IndirectInstanceCapacity;
        public float DeformationCaptureMilliseconds;
        public float ProjectionMilliseconds;
        public float MaskCommandMilliseconds;
        public float MaskGpuMilliseconds;
        public float ReadbackMilliseconds;
        public float ContourMilliseconds;
        public float CompositeCommandMilliseconds;
        public float CompositeGpuMilliseconds;
        public ShapeProjectionRuntimeState State;
        public string Fault;
    }

    static class ShapeProjectionStageRecorder
    {
        static readonly Recorder Mask = Create("3C Character Shape Projection Mask");
        static readonly Recorder Composite = Create("3C Character Shape Projection Composite");

        public static float MaskGpuMilliseconds => Milliseconds(Mask.gpuElapsedNanoseconds);
        public static float CompositeGpuMilliseconds => Milliseconds(Composite.gpuElapsedNanoseconds);

        static Recorder Create(string name)
        {
            Recorder recorder = Recorder.Get(name);
            recorder.enabled = true;
            return recorder;
        }

        static float Milliseconds(long nanoseconds)
        {
            return nanoseconds > 0 ? nanoseconds / 1000000f : 0f;
        }
    }
}
