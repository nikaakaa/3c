using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace ThirdPersonRendering.ShapeProjection
{
    enum ShapeProjectionSlotState
    {
        Free = 0,
        Recorded = 1,
        Readback = 2,
        Contour = 3,
        Published = 4
    }

    sealed class ShapeProjectionFrameSlot : IDisposable
    {
        readonly CharacterShapeProjectionRuntimeWorkspace owner;
        readonly Action<AsyncGPUReadbackRequest> readbackCallback;
        readonly int maxRegions;
        readonly int atlasWidth;
        readonly int atlasHeight;
        AsyncGPUReadbackRequest readbackRequest;
        JobHandle contourHandle;
        double readbackStarted;
        double contourStarted;
        bool valid;

        public readonly int Index;
        public int Generation;
        public ShapeProjectionSlotState State;
        public ShapeProjectionResultIdentity Identity;
        public int ActiveRegionCount;
        public readonly int[] ActiveRegionIndices;
        public readonly NativeArray<ShapeProjectionProjectedVertex> ProjectedVertices;
        public NativeArray<ShapeProjectionRegionGpu> RegionGpu;
        public NativeArray<byte> MaskReadback;
        public readonly NativeArray<ShapeProjectionBoundaryEdge> Edges;
        public readonly NativeArray<byte> EdgeUsed;
        public readonly NativeParallelMultiHashMap<int, int> EdgeStarts;
        public readonly NativeArray<float2> RawPoints;
        public readonly NativeArray<byte> RawAnchors;
        public readonly NativeArray<ShapeProjectionLoopNative> RawLoops;
        public readonly NativeArray<float2> OutputPoints;
        public readonly NativeArray<uint> OutputAnchorFlags;
        public readonly NativeArray<ShapeProjectionLoopNative> OutputLoops;
        public readonly NativeArray<ShapeProjectionCompositeRegionGpu> CompositeRegions;
        public readonly NativeArray<float2> RdpInput;
        public readonly NativeArray<byte> RdpAnchors;
        public readonly NativeArray<byte> RdpKeep;
        public readonly NativeArray<int2> RdpStack;
        public readonly NativeArray<uint> IndirectArgs;
        public readonly NativeReference<int> ErrorCode;
        public readonly NativeReference<int> RawPointCount;
        public readonly NativeReference<int> RawLoopCount;
        public readonly NativeReference<int> OutputPointCount;
        public readonly NativeReference<int> OutputLoopCount;
        public readonly NativeReference<int> CompositeRegionCount;
        public readonly GraphicsBuffer ProjectedVertexBuffer;
        public readonly GraphicsBuffer RegionBuffer;
        public readonly GraphicsBuffer PointBuffer;
        public readonly GraphicsBuffer AnchorBuffer;
        public readonly GraphicsBuffer LoopBuffer;
        public readonly GraphicsBuffer CompositeRegionBuffer;
        public readonly GraphicsBuffer IndirectArgsBuffer;
        public readonly RTHandle MaskTexture;
        public readonly RTHandle RawDepthTexture;
        public readonly RTHandle CompletedDepthTexture;
        public float ReadbackMilliseconds;
        public float ContourMilliseconds;
        public float MaskCommandMilliseconds;

        public ShapeProjectionFrameSlot(CharacterShapeProjectionRuntimeWorkspace owner, int index,
            ShapeProjectionCapacity capacity)
        {
            this.owner = owner;
            Index = index;
            maxRegions = capacity.MaxRegions;
            atlasWidth = capacity.AtlasWidth;
            atlasHeight = capacity.AtlasHeight;
            int edgeCapacity = capacity.MaxContourPoints * 2;
            int rdpCapacity = capacity.MaxContourPoints + 1;
            ActiveRegionIndices = new int[capacity.MaxRegions];
            ProjectedVertices = new NativeArray<ShapeProjectionProjectedVertex>(capacity.MaxVertices, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            RegionGpu = new NativeArray<ShapeProjectionRegionGpu>(capacity.MaxRegions, Allocator.Persistent,
                NativeArrayOptions.ClearMemory);
            MaskReadback = new NativeArray<byte>(capacity.AtlasWidth * capacity.AtlasHeight, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            Edges = new NativeArray<ShapeProjectionBoundaryEdge>(edgeCapacity, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            EdgeUsed = new NativeArray<byte>(edgeCapacity, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            EdgeStarts = new NativeParallelMultiHashMap<int, int>(edgeCapacity, Allocator.Persistent);
            RawPoints = new NativeArray<float2>(capacity.MaxContourPoints, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            RawAnchors = new NativeArray<byte>(capacity.MaxContourPoints, Allocator.Persistent,
                NativeArrayOptions.ClearMemory);
            RawLoops = new NativeArray<ShapeProjectionLoopNative>(capacity.MaxLoops, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            OutputPoints = new NativeArray<float2>(capacity.MaxContourPoints, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            OutputAnchorFlags = new NativeArray<uint>(capacity.MaxContourPoints, Allocator.Persistent,
                NativeArrayOptions.ClearMemory);
            OutputLoops = new NativeArray<ShapeProjectionLoopNative>(capacity.MaxLoops, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            CompositeRegions = new NativeArray<ShapeProjectionCompositeRegionGpu>(capacity.MaxIndirectInstances, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            RdpInput = new NativeArray<float2>(rdpCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            RdpAnchors = new NativeArray<byte>(rdpCapacity, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            RdpKeep = new NativeArray<byte>(rdpCapacity, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            RdpStack = new NativeArray<int2>(rdpCapacity * 2, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            IndirectArgs = new NativeArray<uint>(4, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            ErrorCode = new NativeReference<int>(Allocator.Persistent);
            RawPointCount = new NativeReference<int>(Allocator.Persistent);
            RawLoopCount = new NativeReference<int>(Allocator.Persistent);
            OutputPointCount = new NativeReference<int>(Allocator.Persistent);
            OutputLoopCount = new NativeReference<int>(Allocator.Persistent);
            CompositeRegionCount = new NativeReference<int>(Allocator.Persistent);

            ProjectedVertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacity.MaxVertices, 16);
            RegionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacity.MaxRegions, 64);
            PointBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacity.MaxContourPoints, 8);
            AnchorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacity.MaxContourPoints, 4);
            LoopBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacity.MaxLoops, 12);
            CompositeRegionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacity.MaxIndirectInstances, 64);
            IndirectArgsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 4, sizeof(uint));
            MaskTexture = AllocateAtlas(capacity, GraphicsFormat.R8_UNorm, $"3C Shape Mask {index}");
            RawDepthTexture = AllocateAtlas(capacity, GraphicsFormat.R32_SFloat, $"3C Shape Raw Depth {index}");
            CompletedDepthTexture = AllocateAtlas(capacity, GraphicsFormat.R32_SFloat, $"3C Shape Completed Depth {index}");
            readbackCallback = OnReadbackCompleted;
            State = ShapeProjectionSlotState.Free;
        }

        public bool IsValid => valid;

        public void Begin(ShapeProjectionResultIdentity identity, int activeRegionCount)
        {
            Generation++;
            Identity = identity;
            Identity.SlotGeneration = Generation;
            ActiveRegionCount = activeRegionCount;
            ReadbackMilliseconds = 0f;
            ContourMilliseconds = 0f;
            MaskCommandMilliseconds = 0f;
            valid = true;
            State = ShapeProjectionSlotState.Recorded;
        }

        public void RequestReadback()
        {
            if (State != ShapeProjectionSlotState.Recorded)
                return;
            State = ShapeProjectionSlotState.Readback;
            readbackStarted = Time.realtimeSinceStartupAsDouble;
            readbackRequest = AsyncGPUReadback.RequestIntoNativeArray(ref MaskReadback, MaskTexture.rt, 0,
                GraphicsFormat.R8_UNorm, readbackCallback);
        }

        public bool TryCompleteContour()
        {
            if (State != ShapeProjectionSlotState.Contour || !contourHandle.IsCompleted)
                return false;
            contourHandle.Complete();
            ContourMilliseconds = (float)((Time.realtimeSinceStartupAsDouble - contourStarted) * 1000.0);
            if (!valid)
            {
                State = ShapeProjectionSlotState.Free;
                return true;
            }
            if (ErrorCode.Value != 0)
            {
                owner.SetFault($"Contour Job失败，错误码{ErrorCode.Value}");
                valid = false;
                State = ShapeProjectionSlotState.Free;
                return true;
            }

            int pointCount = OutputPointCount.Value;
            int loopCount = OutputLoopCount.Value;
            int regionCount = CompositeRegionCount.Value;
            if (pointCount > 0)
            {
                PointBuffer.SetData(OutputPoints, 0, 0, pointCount);
                AnchorBuffer.SetData(OutputAnchorFlags, 0, 0, pointCount);
            }
            if (loopCount > 0)
                LoopBuffer.SetData(OutputLoops, 0, 0, loopCount);
            if (regionCount > 0)
                CompositeRegionBuffer.SetData(CompositeRegions, 0, 0, regionCount);
            IndirectArgsBuffer.SetData(IndirectArgs);
            owner.Publish(this);
            return true;
        }

        public void Invalidate()
        {
            valid = false;
            if (State == ShapeProjectionSlotState.Published || State == ShapeProjectionSlotState.Recorded)
                State = ShapeProjectionSlotState.Free;
        }

        public void MarkPublished()
        {
            State = ShapeProjectionSlotState.Published;
        }

        public void RecordMaskCommand(float milliseconds)
        {
            MaskCommandMilliseconds = milliseconds;
        }

        public void ReleasePublished()
        {
            valid = false;
            State = ShapeProjectionSlotState.Free;
        }

        public void Dispose()
        {
            if (State == ShapeProjectionSlotState.Readback && !readbackRequest.done)
                readbackRequest.WaitForCompletion();
            if (State == ShapeProjectionSlotState.Contour)
                contourHandle.Complete();
            DisposeNative(ProjectedVertices);
            DisposeNative(RegionGpu);
            DisposeNative(MaskReadback);
            DisposeNative(Edges);
            DisposeNative(EdgeUsed);
            if (EdgeStarts.IsCreated) EdgeStarts.Dispose();
            DisposeNative(RawPoints);
            DisposeNative(RawAnchors);
            DisposeNative(RawLoops);
            DisposeNative(OutputPoints);
            DisposeNative(OutputAnchorFlags);
            DisposeNative(OutputLoops);
            DisposeNative(CompositeRegions);
            DisposeNative(RdpInput);
            DisposeNative(RdpAnchors);
            DisposeNative(RdpKeep);
            DisposeNative(RdpStack);
            DisposeNative(IndirectArgs);
            if (ErrorCode.IsCreated) ErrorCode.Dispose();
            if (RawPointCount.IsCreated) RawPointCount.Dispose();
            if (RawLoopCount.IsCreated) RawLoopCount.Dispose();
            if (OutputPointCount.IsCreated) OutputPointCount.Dispose();
            if (OutputLoopCount.IsCreated) OutputLoopCount.Dispose();
            if (CompositeRegionCount.IsCreated) CompositeRegionCount.Dispose();
            ProjectedVertexBuffer.Dispose();
            RegionBuffer.Dispose();
            PointBuffer.Dispose();
            AnchorBuffer.Dispose();
            LoopBuffer.Dispose();
            CompositeRegionBuffer.Dispose();
            IndirectArgsBuffer.Dispose();
            MaskTexture.Release();
            RawDepthTexture.Release();
            CompletedDepthTexture.Release();
        }

        void OnReadbackCompleted(AsyncGPUReadbackRequest request)
        {
            ReadbackMilliseconds = (float)((Time.realtimeSinceStartupAsDouble - readbackStarted) * 1000.0);
            if (State != ShapeProjectionSlotState.Readback)
                return;
            if (request.hasError)
            {
                owner.SetFault("R8 Mask Async GPU Readback失败");
                valid = false;
                State = ShapeProjectionSlotState.Free;
                return;
            }
            if (!valid)
            {
                State = ShapeProjectionSlotState.Free;
                return;
            }

            ShapeProjectionContourJob job = new ShapeProjectionContourJob
            {
                Mask = MaskReadback,
                RegionGpu = RegionGpu,
                Regions = owner.Regions,
                RegionSharedChainIndices = owner.RegionSharedChainIndices,
                SharedChains = owner.SharedChains,
                SharedChainVertexIndices = owner.SharedChainVertexIndices,
                ProjectedVertices = ProjectedVertices,
                RegionCount = owner.RegionCount,
                AtlasWidth = atlasWidth,
                AtlasHeight = atlasHeight,
                SimplifyEpsilon = owner.Profile.SimplifyEpsilonPixels,
                MinimumLoopArea = owner.Profile.MinimumLoopAreaPixels,
                MinimumSharedEdgeLength = owner.Profile.MinimumSharedEdgePixels,
                Edges = Edges,
                EdgeUsed = EdgeUsed,
                EdgeStarts = EdgeStarts,
                RawPoints = RawPoints,
                RawAnchors = RawAnchors,
                RawLoops = RawLoops,
                OutputPoints = OutputPoints,
                OutputAnchorFlags = OutputAnchorFlags,
                OutputLoops = OutputLoops,
                CompositeRegions = CompositeRegions,
                RdpInput = RdpInput,
                RdpAnchors = RdpAnchors,
                RdpKeep = RdpKeep,
                RdpStack = RdpStack,
                IndirectArgs = IndirectArgs,
                ErrorCode = ErrorCode,
                RawPointCount = RawPointCount,
                RawLoopCount = RawLoopCount,
                OutputPointCount = OutputPointCount,
                OutputLoopCount = OutputLoopCount,
                CompositeRegionCount = CompositeRegionCount
            };
            contourStarted = Time.realtimeSinceStartupAsDouble;
            contourHandle = job.Schedule();
            State = ShapeProjectionSlotState.Contour;
        }

        static RTHandle AllocateAtlas(ShapeProjectionCapacity capacity, GraphicsFormat format, string name)
        {
            return RTHandles.Alloc(capacity.AtlasWidth, capacity.AtlasHeight, depthBufferBits: DepthBits.None,
                colorFormat: format, filterMode: FilterMode.Point, wrapMode: TextureWrapMode.Clamp,
                enableRandomWrite: true, useMipMap: false, autoGenerateMips: false, name: name);
        }

        static void DisposeNative<T>(NativeArray<T> value) where T : struct
        {
            if (value.IsCreated)
                value.Dispose();
        }
    }

    sealed class CharacterShapeProjectionRuntimeWorkspace : IDisposable
    {
        const float CameraCutDistance = 2f;
        const float CameraCutAngle = 45f;
        const float ProjectionCutThreshold = 0.04f;

        readonly CharacterShapeProjectionSource source;
        readonly Mesh[] bakedMeshes;
        readonly List<Vector3>[] bakedVertices;
        NativeArray<float3> captureVertices;
        readonly NativeArray<int> vertexRendererSlots;
        NativeArray<float4x4> localToWorld;
        readonly NativeArray<ShapeProjectionRegionBounds> regionBounds;
        readonly NativeArray<int> triangleIndices;
        readonly GraphicsBuffer triangleIndexBuffer;
        readonly ShapeProjectionFrameSlot[] slots;
        readonly ShapeProjectionProfileId profileId;
        readonly ShapeProjectionArtifactId artifactId;
        readonly Hash128 profileHash;
        readonly Hash128 artifactHash;
        readonly int profileRevision;
        readonly int sourceGeneration;
        bool hasCameraState;
        Vector3 lastCameraPosition;
        Quaternion lastCameraRotation;
        Matrix4x4 lastProjection;
        int cameraInstanceId;
        int viewportWidth;
        int viewportHeight;
        int projectionGeneration;
        ulong submissionSequence;
        int publishedSlot = -1;
        int skippedSubmissions;
        int activeRegions;
        int atlasUsedPixels;
        readonly int totalVertices;
        readonly int totalTriangles;
        float deformationMilliseconds;
        float projectionMilliseconds;
        float compositeCommandMilliseconds;

        public CharacterShapeProjectionProfile Profile => source.Profile;
        public NativeArray<ShapeProjectionRegionNative> Regions { get; }
        public NativeArray<int> RegionSharedChainIndices { get; }
        public NativeArray<ShapeProjectionSharedChainNative> SharedChains { get; }
        public NativeArray<int> SharedChainVertexIndices { get; }
        public int RegionCount => source.Artifact.Regions.Length;
        public CharacterShapeProjectionSource Source => source;
        public ShapeProjectionFrameSlot PublishedSlot => publishedSlot >= 0 ? slots[publishedSlot] : null;

        public CharacterShapeProjectionRuntimeWorkspace(CharacterShapeProjectionSource source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            ShapeProjectionValidationResult validation = source.ValidateSource();
            if (!validation.IsValid)
                throw new InvalidOperationException(validation.Error);
            if (!SystemInfo.IsFormatSupported(GraphicsFormat.R8_UNorm, FormatUsage.LoadStore)
                || !SystemInfo.IsFormatSupported(GraphicsFormat.R32_SFloat, FormatUsage.LoadStore))
                throw new InvalidOperationException("当前图形设备不支持R8_UNorm或R32_SFloat UAV");

            CharacterShapeProjectionArtifact artifact = source.Artifact;
            profileId = source.Profile.ProfileId;
            artifactId = artifact.ArtifactId;
            profileHash = source.Profile.ContentHash;
            artifactHash = artifact.ContentHash;
            profileRevision = source.Profile.Revision;
            sourceGeneration = source.Generation;
            ShapeProjectionCapacity capacity = source.Profile.Capacity;
            bakedMeshes = new Mesh[artifact.Renderers.Length];
            bakedVertices = new List<Vector3>[artifact.Renderers.Length];
            for (int i = 0; i < artifact.Renderers.Length; i++)
            {
                bakedMeshes[i] = new Mesh { name = $"{source.name}_ShapeProjectionBake_{i}" };
                bakedMeshes[i].MarkDynamic();
                bakedVertices[i] = new List<Vector3>(artifact.Renderers[i].VertexCount);
                totalVertices += artifact.Renderers[i].VertexCount;
            }
            totalTriangles = artifact.TriangleIndices.Length / 3;

            captureVertices = new NativeArray<float3>(capacity.MaxVertices, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            vertexRendererSlots = new NativeArray<int>(capacity.MaxVertices, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            localToWorld = new NativeArray<float4x4>(capacity.MaxRenderers, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            regionBounds = new NativeArray<ShapeProjectionRegionBounds>(capacity.MaxRegions, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            triangleIndices = new NativeArray<int>(artifact.TriangleIndices, Allocator.Persistent);
            NativeArray<ShapeProjectionRegionNative> regions = new NativeArray<ShapeProjectionRegionNative>(capacity.MaxRegions, Allocator.Persistent,
                NativeArrayOptions.ClearMemory);
            Regions = regions;
            RegionSharedChainIndices = new NativeArray<int>(artifact.RegionSharedChainIndices, Allocator.Persistent);
            NativeArray<ShapeProjectionSharedChainNative> sharedChains = new NativeArray<ShapeProjectionSharedChainNative>(artifact.SharedChains.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            SharedChains = sharedChains;
            SharedChainVertexIndices = new NativeArray<int>(artifact.SharedChainVertexIndices, Allocator.Persistent);

            for (int renderer = 0; renderer < artifact.Renderers.Length; renderer++)
            {
                ShapeProjectionRendererRecord record = artifact.Renderers[renderer];
                for (int vertex = 0; vertex < record.VertexCount; vertex++)
                    vertexRendererSlots[record.VertexStart + vertex] = renderer;
            }
            for (int region = 0; region < artifact.Regions.Length; region++)
            {
                ShapeProjectionRegionRecord record = artifact.Regions[region];
                regions[region] = new ShapeProjectionRegionNative
                {
                    TriangleStart = record.TriangleStart,
                    TriangleCount = record.TriangleCount,
                    RendererSlot = record.RendererSlot,
                    SharedChainStart = record.SharedChainStart,
                    SharedChainCount = record.SharedChainCount
                };
            }
            for (int chain = 0; chain < artifact.SharedChains.Length; chain++)
            {
                ShapeProjectionSharedChainRecord record = artifact.SharedChains[chain];
                sharedChains[chain] = new ShapeProjectionSharedChainNative
                {
                    LeftRegion = record.LeftRegion,
                    RightRegion = record.RightRegion,
                    VertexStart = record.VertexStart,
                    VertexCount = record.VertexCount
                };
            }

            triangleIndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured,
                Mathf.Max(1, artifact.TriangleIndices.Length), sizeof(int));
            triangleIndexBuffer.SetData(artifact.TriangleIndices);
            slots = new ShapeProjectionFrameSlot[capacity.ReadbackSlots];
            for (int i = 0; i < slots.Length; i++)
                slots[i] = new ShapeProjectionFrameSlot(this, i, capacity);
        }

        public void UpdateCamera(Camera camera, int width, int height, Matrix4x4 projection)
        {
            bool cut = hasCameraState && (width != viewportWidth || height != viewportHeight
                || Vector3.Distance(lastCameraPosition, camera.transform.position) > CameraCutDistance
                || Quaternion.Angle(lastCameraRotation, camera.transform.rotation) > CameraCutAngle
                || ProjectionDistance(lastProjection, projection) > ProjectionCutThreshold);
            if (cut)
            {
                projectionGeneration++;
                InvalidateAll();
            }
            hasCameraState = true;
            cameraInstanceId = camera.GetInstanceID();
            viewportWidth = width;
            viewportHeight = height;
            lastCameraPosition = camera.transform.position;
            lastCameraRotation = camera.transform.rotation;
            lastProjection = projection;
        }

        public void ProcessCompletedContours()
        {
            for (int i = 0; i < slots.Length; i++)
                slots[i].TryCompleteContour();
            PublishDiagnostics();
        }

        public ShapeProjectionFrameSlot TryPrepareSubmission(Camera camera, Matrix4x4 screenViewProjection,
            Matrix4x4 depthViewProjection, Vector2 depthScaleBias)
        {
            if (!IsLineageCompatible())
            {
                SetFault("Source、Profile或Artifact在Workspace存活期间发生了lineage变化");
                return null;
            }

            ShapeProjectionFrameSlot slot = null;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].State == ShapeProjectionSlotState.Free)
                {
                    slot = slots[i];
                    break;
                }
            }
            if (slot == null)
            {
                skippedSubmissions++;
                PublishDiagnostics();
                return null;
            }

            double deformationStart = Time.realtimeSinceStartupAsDouble;
            CharacterShapeProjectionArtifact artifact = source.Artifact;
            CharacterShapeProjectionSource.RendererBinding[] bindings = source.RendererBindings;
            int totalVertices = 0;
            for (int rendererIndex = 0; rendererIndex < bindings.Length; rendererIndex++)
            {
                ShapeProjectionRendererRecord record = artifact.Renderers[rendererIndex];
                SkinnedMeshRenderer renderer = bindings[rendererIndex].Renderer;
                renderer.BakeMesh(bakedMeshes[rendererIndex], false);
                List<Vector3> vertices = bakedVertices[rendererIndex];
                vertices.Clear();
                bakedMeshes[rendererIndex].GetVertices(vertices);
                if (vertices.Count != record.VertexCount)
                {
                    SetFault($"Renderer {record.SlotId}变形顶点数{vertices.Count}与Artifact {record.VertexCount}不一致");
                    return null;
                }
                for (int vertex = 0; vertex < vertices.Count; vertex++)
                    captureVertices[record.VertexStart + vertex] = vertices[vertex];
                localToWorld[rendererIndex] = ToFloat4x4(renderer.localToWorldMatrix);
                totalVertices += vertices.Count;
            }
            deformationMilliseconds = (float)((Time.realtimeSinceStartupAsDouble - deformationStart) * 1000.0);

            double projectionStart = Time.realtimeSinceStartupAsDouble;
            ShapeProjectionProjectVerticesJob projectJob = new ShapeProjectionProjectVerticesJob
            {
                LocalVertices = captureVertices,
                VertexRendererSlots = vertexRendererSlots,
                LocalToWorld = localToWorld,
                ScreenViewProjection = ToFloat4x4(screenViewProjection),
                DepthViewProjection = ToFloat4x4(depthViewProjection),
                DepthScaleBias = new float2(depthScaleBias.x, depthScaleBias.y),
                ViewportSize = new float2(viewportWidth, viewportHeight),
                ProjectedVertices = slot.ProjectedVertices
            };
            JobHandle projectHandle = projectJob.Schedule(totalVertices, 128);
            ShapeProjectionRegionBoundsJob boundsJob = new ShapeProjectionRegionBoundsJob
            {
                Regions = Regions,
                TriangleIndices = triangleIndices,
                ProjectedVertices = slot.ProjectedVertices,
                ViewportSize = new float2(viewportWidth, viewportHeight),
                Bounds = regionBounds
            };
            JobHandle boundsHandle = boundsJob.Schedule(artifact.Regions.Length, 32, projectHandle);
            boundsHandle.Complete();
            if (!PackRegions(slot))
                return null;
            projectionMilliseconds = (float)((Time.realtimeSinceStartupAsDouble - projectionStart) * 1000.0);

            if (activeRegions == 0)
            {
                ClearPublished();
                source.MarkWaitingForFirstResult();
                PublishDiagnostics();
                return null;
            }

            submissionSequence++;
            ShapeProjectionResultIdentity identity = new ShapeProjectionResultIdentity
            {
                SourceId = source.SourceId,
                SourceGeneration = source.Generation,
                CameraInstanceId = camera.GetInstanceID(),
                ProjectionHash = CurrentProjectionHash(),
                ViewportWidth = viewportWidth,
                ViewportHeight = viewportHeight,
                ProfileId = source.Profile.ProfileId,
                ProfileRevision = source.Profile.Revision,
                ArtifactId = artifact.ArtifactId,
                ArtifactHash = artifact.ContentHash,
                RenderFrame = Time.frameCount,
                SubmissionSequence = submissionSequence
            };
            slot.Begin(identity, activeRegions);
            slot.ProjectedVertexBuffer.SetData(slot.ProjectedVertices, 0, 0, totalVertices);
            slot.RegionBuffer.SetData(slot.RegionGpu, 0, 0, artifact.Regions.Length);
            PublishDiagnostics();
            return slot;
        }

        public void RecordMask(CommandBuffer cmd, ComputeShader compute, int clearKernel, int rasterKernel, int completeKernel,
            ShapeProjectionFrameSlot slot)
        {
            ShapeProjectionCapacity capacity = Profile.Capacity;
            cmd.SetComputeVectorParam(compute, "_ShapeAtlasSize",
                new Vector4(capacity.AtlasWidth, capacity.AtlasHeight, 1f / capacity.AtlasWidth, 1f / capacity.AtlasHeight));
            cmd.SetComputeTextureParam(compute, clearKernel, "_ShapeMask", slot.MaskTexture.rt);
            cmd.SetComputeTextureParam(compute, clearKernel, "_ShapeRawDepth", slot.RawDepthTexture.rt);
            cmd.SetComputeTextureParam(compute, clearKernel, "_ShapeCompletedDepth", slot.CompletedDepthTexture.rt);
            cmd.DispatchCompute(compute, clearKernel, DivideRoundUp(capacity.AtlasWidth, 8), DivideRoundUp(capacity.AtlasHeight, 8), 1);

            cmd.SetComputeBufferParam(compute, rasterKernel, "_ShapeProjectedVertices", slot.ProjectedVertexBuffer);
            cmd.SetComputeBufferParam(compute, rasterKernel, "_ShapeTriangleIndices", triangleIndexBuffer);
            cmd.SetComputeBufferParam(compute, rasterKernel, "_ShapeRegions", slot.RegionBuffer);
            cmd.SetComputeTextureParam(compute, rasterKernel, "_ShapeMask", slot.MaskTexture.rt);
            cmd.SetComputeTextureParam(compute, rasterKernel, "_ShapeRawDepth", slot.RawDepthTexture.rt);
            for (int active = 0; active < slot.ActiveRegionCount; active++)
            {
                int regionIndex = slot.ActiveRegionIndices[active];
                ShapeProjectionRegionGpu region = slot.RegionGpu[regionIndex];
                cmd.SetComputeIntParam(compute, "_ShapeRegionIndex", regionIndex);
                cmd.DispatchCompute(compute, rasterKernel, DivideRoundUp((int)region.AtlasRect.z, 8),
                    DivideRoundUp((int)region.AtlasRect.w, 8), 1);
            }

            cmd.SetComputeBufferParam(compute, completeKernel, "_ShapeRegions", slot.RegionBuffer);
            cmd.SetComputeTextureParam(compute, completeKernel, "_ShapeRawDepth", slot.RawDepthTexture.rt);
            cmd.SetComputeTextureParam(compute, completeKernel, "_ShapeCompletedDepth", slot.CompletedDepthTexture.rt);
            cmd.SetComputeIntParam(compute, "_ShapeCompletionRadius", Mathf.CeilToInt(Profile.SimplifyEpsilonPixels + Profile.OutlineWidthPixels) + 2);
            for (int active = 0; active < slot.ActiveRegionCount; active++)
            {
                int regionIndex = slot.ActiveRegionIndices[active];
                ShapeProjectionRegionGpu region = slot.RegionGpu[regionIndex];
                cmd.SetComputeIntParam(compute, "_ShapeRegionIndex", regionIndex);
                cmd.DispatchCompute(compute, completeKernel, DivideRoundUp((int)region.AtlasRect.z, 8),
                    DivideRoundUp((int)region.AtlasRect.w, 8), 1);
            }
        }

        public void Publish(ShapeProjectionFrameSlot slot)
        {
            if (!slot.IsValid || slot.Identity.SourceGeneration != source.Generation
                              || !IsLineageCompatible()
                              || slot.Identity.CameraInstanceId != cameraInstanceId
                              || slot.Identity.ViewportWidth != viewportWidth
                              || slot.Identity.ViewportHeight != viewportHeight
                              || slot.Identity.ProjectionHash != CurrentProjectionHash())
            {
                slot.ReleasePublished();
                return;
            }
            if (publishedSlot >= 0
                && slots[publishedSlot].Identity.SubmissionSequence >= slot.Identity.SubmissionSequence)
            {
                slot.ReleasePublished();
                return;
            }
            if (publishedSlot >= 0 && slots[publishedSlot] != slot)
                slots[publishedSlot].ReleasePublished();
            publishedSlot = slot.Index;
            slot.MarkPublished();
            source.MarkReady();
            PublishDiagnostics();
        }

        public void SetFault(string message)
        {
            source.SetFault(message);
            InvalidateAll();
            PublishDiagnostics();
        }

        public void RecordCompositeCommand(float milliseconds)
        {
            compositeCommandMilliseconds = milliseconds;
            PublishDiagnostics();
        }

        public void Dispose()
        {
            for (int i = 0; i < slots.Length; i++)
                slots[i].Dispose();
            triangleIndexBuffer.Dispose();
            DisposeNative(captureVertices);
            DisposeNative(vertexRendererSlots);
            DisposeNative(localToWorld);
            DisposeNative(regionBounds);
            DisposeNative(triangleIndices);
            DisposeNative(Regions);
            DisposeNative(RegionSharedChainIndices);
            DisposeNative(SharedChains);
            DisposeNative(SharedChainVertexIndices);
            for (int i = 0; i < bakedMeshes.Length; i++)
            {
                if (bakedMeshes[i] != null)
                    UnityEngine.Object.Destroy(bakedMeshes[i]);
            }
        }

        bool PackRegions(ShapeProjectionFrameSlot slot)
        {
            int padding = Mathf.CeilToInt(Profile.SimplifyEpsilonPixels + Profile.OutlineWidthPixels) + 2;
            int atlasX = 0;
            int atlasY = 0;
            int rowHeight = 0;
            activeRegions = 0;
            atlasUsedPixels = 0;
            for (int regionIndex = 0; regionIndex < RegionCount; regionIndex++)
            {
                ShapeProjectionRegionBounds bounds = regionBounds[regionIndex];
                ShapeProjectionRegionRecord artifactRegion = source.Artifact.Regions[regionIndex];
                ShapeProjectionRegionGpu gpu = default;
                if (bounds.Valid != 0)
                {
                    int screenMinX = Mathf.Max(0, Mathf.FloorToInt(bounds.Min.x) - padding);
                    int screenMinY = Mathf.Max(0, Mathf.FloorToInt(bounds.Min.y) - padding);
                    int screenMaxX = Mathf.Min(viewportWidth, Mathf.CeilToInt(bounds.Max.x) + padding);
                    int screenMaxY = Mathf.Min(viewportHeight, Mathf.CeilToInt(bounds.Max.y) + padding);
                    int width = screenMaxX - screenMinX;
                    int height = screenMaxY - screenMinY;
                    if (width > 0 && height > 0 && width * height >= Profile.MinimumLoopAreaPixels)
                    {
                        if (width > Profile.Capacity.AtlasWidth || height > Profile.Capacity.AtlasHeight)
                        {
                            SetFault($"Region {artifactRegion.RegionId}超过Atlas固定尺寸");
                            return false;
                        }
                        if (atlasX + width > Profile.Capacity.AtlasWidth)
                        {
                            atlasX = 0;
                            atlasY += rowHeight;
                            rowHeight = 0;
                        }
                        if (atlasY + height > Profile.Capacity.AtlasHeight)
                        {
                            SetFault("有效Region无法装入固定Mask Atlas容量");
                            return false;
                        }
                        gpu = new ShapeProjectionRegionGpu
                        {
                            ScreenRect = new Vector4(screenMinX, screenMinY, width, height),
                            AtlasRect = new Vector4(atlasX, atlasY, width, height),
                            Color = artifactRegion.RepresentativeColor,
                            Meta = new Vector4(artifactRegion.TriangleStart, artifactRegion.TriangleCount, 1f, padding)
                        };
                        slot.ActiveRegionIndices[activeRegions++] = regionIndex;
                        atlasUsedPixels += width * height;
                        atlasX += width + 1;
                        rowHeight = Mathf.Max(rowHeight, height + 1);
                    }
                }
                slot.RegionGpu[regionIndex] = gpu;
            }
            return true;
        }

        void InvalidateAll()
        {
            for (int i = 0; i < slots.Length; i++)
                slots[i].Invalidate();
            publishedSlot = -1;
            source.MarkWaitingForFirstResult();
        }

        void ClearPublished()
        {
            if (publishedSlot >= 0)
                slots[publishedSlot].ReleasePublished();
            publishedSlot = -1;
        }

        bool IsLineageCompatible()
        {
            return source != null
                   && source.Generation == sourceGeneration
                   && source.Profile != null
                   && source.Artifact != null
                   && source.Profile.ProfileId.Equals(profileId)
                   && source.Profile.Revision == profileRevision
                   && source.Profile.ContentHash == profileHash
                   && source.Artifact.ArtifactId.Equals(artifactId)
                   && source.Artifact.ContentHash == artifactHash;
        }

        Hash128 CurrentProjectionHash()
        {
            return new Hash128((uint)cameraInstanceId, (uint)projectionGeneration,
                (uint)viewportWidth, (uint)viewportHeight);
        }

        void PublishDiagnostics()
        {
            int occupied = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].State != ShapeProjectionSlotState.Free)
                    occupied++;
            }
            ShapeProjectionFrameSlot published = PublishedSlot;
            source.PublishDiagnostics(new ShapeProjectionDiagnosticsSnapshot
            {
                SourceId = source.SourceId,
                SourceGeneration = source.Generation,
                CameraInstanceId = published?.Identity.CameraInstanceId ?? 0,
                ProfileId = published?.Identity.ProfileId ?? profileId,
                ProfileRevision = published?.Identity.ProfileRevision ?? profileRevision,
                ArtifactId = published?.Identity.ArtifactId ?? artifactId,
                ArtifactHash = published?.Identity.ArtifactHash ?? artifactHash,
                ProjectionHash = published?.Identity.ProjectionHash ?? CurrentProjectionHash(),
                SlotGeneration = published?.Identity.SlotGeneration ?? 0,
                LastSubmissionSequence = submissionSequence,
                LastSubmissionFrame = published?.Identity.RenderFrame ?? 0,
                DisplayFrame = Time.frameCount,
                DisplayResultFrame = published?.Identity.RenderFrame ?? 0,
                ResultAgeFrames = published == null ? 0 : Mathf.Max(0, Time.frameCount - published.Identity.RenderFrame),
                OccupiedSlots = occupied,
                SkippedSubmissions = skippedSubmissions,
                RendererCount = source.RendererBindings.Length,
                VertexCount = totalVertices,
                VertexCapacity = Profile.Capacity.MaxVertices,
                TriangleCount = totalTriangles,
                TriangleCapacity = Profile.Capacity.MaxTriangles,
                SharedChainCount = source.Artifact.SharedChains.Length,
                SharedChainCapacity = Profile.Capacity.MaxSharedChains,
                RegionCapacity = Profile.Capacity.MaxRegions,
                ActiveRegions = activeRegions,
                FilteredRegions = Mathf.Max(0, RegionCount - activeRegions),
                AtlasWidth = Profile.Capacity.AtlasWidth,
                AtlasHeight = Profile.Capacity.AtlasHeight,
                AtlasUsedPixels = atlasUsedPixels,
                ContourPointCount = published?.OutputPointCount.Value ?? 0,
                ContourPointCapacity = Profile.Capacity.MaxContourPoints,
                LoopCount = published?.OutputLoopCount.Value ?? 0,
                LoopCapacity = Profile.Capacity.MaxLoops,
                IndirectInstanceCount = published?.CompositeRegionCount.Value ?? 0,
                IndirectInstanceCapacity = Profile.Capacity.MaxIndirectInstances,
                DeformationCaptureMilliseconds = deformationMilliseconds,
                ProjectionMilliseconds = projectionMilliseconds,
                MaskCommandMilliseconds = published?.MaskCommandMilliseconds ?? 0f,
                MaskGpuMilliseconds = ShapeProjectionStageRecorder.MaskGpuMilliseconds,
                ReadbackMilliseconds = published?.ReadbackMilliseconds ?? 0f,
                ContourMilliseconds = published?.ContourMilliseconds ?? 0f,
                CompositeCommandMilliseconds = compositeCommandMilliseconds,
                CompositeGpuMilliseconds = ShapeProjectionStageRecorder.CompositeGpuMilliseconds,
                State = source.RuntimeState,
                Fault = source.Fault
            });
        }

        static float ProjectionDistance(Matrix4x4 left, Matrix4x4 right)
        {
            float maximum = 0f;
            for (int i = 0; i < 16; i++)
                maximum = Mathf.Max(maximum, Mathf.Abs(left[i] - right[i]));
            return maximum;
        }

        static float4x4 ToFloat4x4(Matrix4x4 value)
        {
            return new float4x4(
                new float4(value.m00, value.m10, value.m20, value.m30),
                new float4(value.m01, value.m11, value.m21, value.m31),
                new float4(value.m02, value.m12, value.m22, value.m32),
                new float4(value.m03, value.m13, value.m23, value.m33));
        }

        static int DivideRoundUp(int value, int divisor)
        {
            return (value + divisor - 1) / divisor;
        }

        static void DisposeNative<T>(NativeArray<T> value) where T : struct
        {
            if (value.IsCreated)
                value.Dispose();
        }
    }
}
