using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ThirdPersonRendering.ShapeProjection
{
    readonly struct ShapeProjectionWorkspaceKey : IEquatable<ShapeProjectionWorkspaceKey>
    {
        public readonly int CameraId;
        public readonly int SourceInstanceId;
        public readonly int SourceGeneration;

        public ShapeProjectionWorkspaceKey(Camera camera, CharacterShapeProjectionSource source)
        {
            CameraId = camera.GetInstanceID();
            SourceInstanceId = source.GetInstanceID();
            SourceGeneration = source.Generation;
        }

        public bool Equals(ShapeProjectionWorkspaceKey other)
        {
            return CameraId == other.CameraId && SourceInstanceId == other.SourceInstanceId
                                              && SourceGeneration == other.SourceGeneration;
        }

        public override bool Equals(object obj) => obj is ShapeProjectionWorkspaceKey other && Equals(other);
        public override int GetHashCode() => ((CameraId * 397) ^ SourceInstanceId) * 397 ^ SourceGeneration;
    }

    sealed class CharacterShapeProjectionWorkspacePool : IDisposable
    {
        readonly int maxWorkspaces;
        readonly Dictionary<ShapeProjectionWorkspaceKey, CharacterShapeProjectionRuntimeWorkspace> workspaces;
        readonly List<ShapeProjectionWorkspaceKey> staleKeys;

        public CharacterShapeProjectionWorkspacePool(int maxWorkspaces)
        {
            this.maxWorkspaces = Mathf.Max(1, maxWorkspaces);
            workspaces = new Dictionary<ShapeProjectionWorkspaceKey, CharacterShapeProjectionRuntimeWorkspace>(this.maxWorkspaces);
            staleKeys = new List<ShapeProjectionWorkspaceKey>(this.maxWorkspaces);
        }

        public CharacterShapeProjectionRuntimeWorkspace GetOrCreate(Camera camera, CharacterShapeProjectionSource source)
        {
            ShapeProjectionWorkspaceKey key = new ShapeProjectionWorkspaceKey(camera, source);
            if (workspaces.TryGetValue(key, out CharacterShapeProjectionRuntimeWorkspace workspace))
                return workspace;
            if (workspaces.Count >= maxWorkspaces)
            {
                source.SetFault($"Shape Projection Workspace超过固定上限{maxWorkspaces}");
                return null;
            }

            try
            {
                workspace = new CharacterShapeProjectionRuntimeWorkspace(source);
                workspaces.Add(key, workspace);
                return workspace;
            }
            catch (Exception exception)
            {
                source.SetFault(exception.Message);
                return null;
            }
        }

        public bool TryGet(Camera camera, CharacterShapeProjectionSource source,
            out CharacterShapeProjectionRuntimeWorkspace workspace)
        {
            return workspaces.TryGetValue(new ShapeProjectionWorkspaceKey(camera, source), out workspace);
        }

        public void Sweep()
        {
            staleKeys.Clear();
            foreach (KeyValuePair<ShapeProjectionWorkspaceKey, CharacterShapeProjectionRuntimeWorkspace> pair in workspaces)
            {
                CharacterShapeProjectionSource source = pair.Value.Source;
                if (source == null || !source.isActiveAndEnabled || source.Generation != pair.Key.SourceGeneration || !source.IsPrepared)
                    staleKeys.Add(pair.Key);
            }
            for (int i = 0; i < staleKeys.Count; i++)
            {
                ShapeProjectionWorkspaceKey key = staleKeys[i];
                workspaces[key].Dispose();
                workspaces.Remove(key);
            }
        }

        public void Dispose()
        {
            foreach (KeyValuePair<ShapeProjectionWorkspaceKey, CharacterShapeProjectionRuntimeWorkspace> pair in workspaces)
                pair.Value.Dispose();
            workspaces.Clear();
            staleKeys.Clear();
        }
    }

    sealed class CharacterShapeProjectionMaskPass : ScriptableRenderPass
    {
        readonly ComputeShader compute;
        readonly CharacterShapeProjectionWorkspacePool pool;
        readonly ProfilingSampler sampler = new ProfilingSampler("3C Character Shape Projection Mask");
        readonly int clearKernel;
        readonly int rasterKernel;
        readonly int completeKernel;

        public CharacterShapeProjectionMaskPass(ComputeShader compute, CharacterShapeProjectionWorkspacePool pool)
        {
            this.compute = compute;
            this.pool = pool;
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            if (compute != null)
            {
                clearKernel = compute.FindKernel("ClearAtlas");
                rasterKernel = compute.FindKernel("RasterRegion");
                completeKernel = compute.FindKernel("CompleteDepth");
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (compute == null || !IsFormalCamera(ref renderingData))
                return;

            Camera camera = renderingData.cameraData.camera;
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            Matrix4x4 view = renderingData.cameraData.GetViewMatrix();
            Matrix4x4 screenProjection = renderingData.cameraData.GetProjectionMatrix();
            Matrix4x4 depthProjection = renderingData.cameraData.GetGPUProjectionMatrix();
            Vector2 depthScaleBias = GetDepthScaleBias();
            pool.Sweep();
            CommandBuffer cmd = CommandBufferPool.Get();
            for (int sourceIndex = 0; sourceIndex < CharacterShapeProjectionRegistry.Count; sourceIndex++)
            {
                CharacterShapeProjectionSource source = CharacterShapeProjectionRegistry.Get(sourceIndex);
                if (source == null || !source.IsPrepared || !source.RenderInGameCamera)
                    continue;
                CharacterShapeProjectionRuntimeWorkspace workspace = pool.GetOrCreate(camera, source);
                if (workspace == null)
                    continue;
                workspace.UpdateCamera(camera, descriptor.width, descriptor.height, screenProjection);
                workspace.ProcessCompletedContours();
                ShapeProjectionFrameSlot slot = workspace.TryPrepareSubmission(camera,
                    screenProjection * view, depthProjection * view, depthScaleBias);
                if (slot == null)
                    continue;
                double maskStart = Time.realtimeSinceStartupAsDouble;
                using (new ProfilingScope(cmd, sampler))
                    workspace.RecordMask(cmd, compute, clearKernel, rasterKernel, completeKernel, slot);
                slot.RecordMaskCommand((float)((Time.realtimeSinceStartupAsDouble - maskStart) * 1000.0));
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                slot.RequestReadback();
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        internal static bool IsFormalCamera(ref RenderingData renderingData)
        {
            return renderingData.cameraData.cameraType == CameraType.Game
                   && renderingData.cameraData.renderType == CameraRenderType.Base
                   && !renderingData.cameraData.xr.enabled;
        }

        static Vector2 GetDepthScaleBias()
        {
            GraphicsDeviceType device = SystemInfo.graphicsDeviceType;
            return device == GraphicsDeviceType.OpenGLCore || device == GraphicsDeviceType.OpenGLES2
                || device == GraphicsDeviceType.OpenGLES3
                ? new Vector2(0.5f, 0.5f)
                : new Vector2(1f, 0f);
        }
    }

    sealed class CharacterShapeProjectionCompositePass : ScriptableRenderPass
    {
        static readonly int PointsId = Shader.PropertyToID("_ShapePoints");
        static readonly int AnchorsId = Shader.PropertyToID("_ShapeAnchorFlags");
        static readonly int LoopsId = Shader.PropertyToID("_ShapeLoops");
        static readonly int RegionsId = Shader.PropertyToID("_ShapeCompositeRegions");
        static readonly int DepthAtlasId = Shader.PropertyToID("_ShapeDepthAtlas");
        static readonly int MaskAtlasId = Shader.PropertyToID("_ShapeMaskAtlas");
        static readonly int AtlasSizeId = Shader.PropertyToID("_ShapeAtlasSize");
        static readonly int DebugViewId = Shader.PropertyToID("_ShapeDebugView");
        static readonly int ScreenSizeId = Shader.PropertyToID("_ShapeScreenSize");
        static readonly int OutlineColorId = Shader.PropertyToID("_ShapeOutlineColor");
        static readonly int OutlineWidthId = Shader.PropertyToID("_ShapeOutlineWidth");

        readonly Material material;
        readonly CharacterShapeProjectionWorkspacePool pool;
        readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        readonly ProfilingSampler sampler = new ProfilingSampler("3C Character Shape Projection Composite");
        RTHandle cameraColor;
        RTHandle cameraDepth;

        public CharacterShapeProjectionCompositePass(Material material, CharacterShapeProjectionWorkspacePool pool)
        {
            this.material = material;
            this.pool = pool;
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public void Configure(RTHandle color, RTHandle depth)
        {
            cameraColor = color;
            cameraDepth = depth;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (cameraColor != null && cameraDepth != null)
                ConfigureTarget(cameraColor, cameraDepth);
            ConfigureClear(ClearFlag.None, Color.clear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null || cameraColor == null || cameraDepth == null
                                 || !CharacterShapeProjectionMaskPass.IsFormalCamera(ref renderingData))
                return;

            Camera camera = renderingData.cameraData.camera;
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, sampler))
            {
                CoreUtils.SetRenderTarget(cmd, cameraColor, cameraDepth, ClearFlag.None, Color.clear);
                for (int sourceIndex = 0; sourceIndex < CharacterShapeProjectionRegistry.Count; sourceIndex++)
                {
                    CharacterShapeProjectionSource source = CharacterShapeProjectionRegistry.Get(sourceIndex);
                    if (source == null || !source.IsPrepared || !pool.TryGet(camera, source, out CharacterShapeProjectionRuntimeWorkspace workspace))
                        continue;
                    workspace.ProcessCompletedContours();
                    ShapeProjectionFrameSlot slot = workspace.PublishedSlot;
                    if (slot == null || !slot.IsValid)
                        continue;

                    properties.Clear();
                    double compositeStart = Time.realtimeSinceStartupAsDouble;
                    properties.SetBuffer(PointsId, slot.PointBuffer);
                    properties.SetBuffer(AnchorsId, slot.AnchorBuffer);
                    properties.SetBuffer(LoopsId, slot.LoopBuffer);
                    properties.SetBuffer(RegionsId, slot.CompositeRegionBuffer);
                    properties.SetTexture(DepthAtlasId, slot.CompletedDepthTexture.rt);
                    properties.SetTexture(MaskAtlasId, slot.MaskTexture.rt);
                    properties.SetVector(AtlasSizeId, new Vector4(source.Profile.Capacity.AtlasWidth,
                        source.Profile.Capacity.AtlasHeight, 1f / source.Profile.Capacity.AtlasWidth,
                        1f / source.Profile.Capacity.AtlasHeight));
                    properties.SetInt(DebugViewId, (int)source.DebugView);
                    properties.SetVector(ScreenSizeId, new Vector4(descriptor.width, descriptor.height,
                        1f / descriptor.width, 1f / descriptor.height));
                    properties.SetColor(OutlineColorId, source.Profile.OutlineColor);
                    properties.SetFloat(OutlineWidthId, source.Profile.OutlineWidthPixels);
                    cmd.DrawProceduralIndirect(Matrix4x4.identity, material, 0, MeshTopology.Triangles,
                        slot.IndirectArgsBuffer, 0, properties);
                    workspace.RecordCompositeCommand((float)((Time.realtimeSinceStartupAsDouble - compositeStart) * 1000.0));
                }
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
