using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace ZZZ.Rendering.Restored
{
    public sealed class CorinRestoredRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] CorinRestoredRenderProfile profile;

        Material compositeMaterial;
        Material copyDepthMaterial;
        CorinRestoredEntityLighting entityLighting;
        CharacterPreparePass preparePass;
        CharacterBufferPass bufferPass;
        CopyDepthPass copyDepthPass;
        CharacterCompositePass compositePass;
        RTHandle gBuffer0;
        RTHandle gBuffer1;
        RTHandle gBuffer2;
        RTHandle normalBuffer;
        RTHandle resolvedDepth;
        RTHandle characterColor;

        public CorinRestoredRenderProfile Profile
        {
            get => profile;
            set
            {
                profile = value;
                Rebuild();
            }
        }

        public override void Create()
        {
            Rebuild();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (profile == null || compositeMaterial == null || bufferPass == null || copyDepthPass == null ||
                compositePass == null || renderingData.cameraData.isPreviewCamera ||
                renderingData.cameraData.cameraType == CameraType.Reflection)
                return;
            renderer.EnqueuePass(preparePass);
            renderer.EnqueuePass(bufferPass);
            renderer.EnqueuePass(copyDepthPass);
            renderer.EnqueuePass(compositePass);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (profile == null || compositeMaterial == null || bufferPass == null || copyDepthPass == null ||
                compositePass == null || renderingData.cameraData.isPreviewCamera ||
                renderingData.cameraData.cameraType == CameraType.Reflection)
                return;
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            var bufferDescriptor = descriptor;
            bufferDescriptor.depthBufferBits = 0;
            bufferDescriptor.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
            bufferDescriptor.bindMS = false;
            RenderingUtils.ReAllocateIfNeeded(ref gBuffer0, bufferDescriptor, FilterMode.Point, TextureWrapMode.Clamp,
                name: "_ZZZCharacterGBuffer0");
            RenderingUtils.ReAllocateIfNeeded(ref gBuffer1, bufferDescriptor, FilterMode.Point, TextureWrapMode.Clamp,
                name: "_ZZZCharacterGBuffer1");
            RenderingUtils.ReAllocateIfNeeded(ref gBuffer2, bufferDescriptor, FilterMode.Point, TextureWrapMode.Clamp,
                name: "_ZZZCharacterGBuffer2");
            RenderingUtils.ReAllocateIfNeeded(ref normalBuffer, bufferDescriptor, FilterMode.Point, TextureWrapMode.Clamp,
                name: "_ZZZCharacterNormalBuffer");
            RenderingUtils.ReAllocateIfNeeded(ref characterColor, bufferDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp,
                name: "_ZZZCharacterColor");
            var depthDescriptor = descriptor;
            depthDescriptor.graphicsFormat = GraphicsFormat.None;
            depthDescriptor.depthStencilFormat = GraphicsFormat.D32_SFloat_S8_UInt;
            depthDescriptor.msaaSamples = 1;
            RenderingUtils.ReAllocateIfNeeded(ref resolvedDepth, depthDescriptor, FilterMode.Point, TextureWrapMode.Clamp,
                name: "_ZZZCharacterDepth");
            var buffers = new[] { gBuffer0, gBuffer1, gBuffer2, normalBuffer };
            bufferPass.Setup(renderer.cameraDepthTargetHandle, buffers);
            copyDepthPass.Setup(renderer.cameraDepthTargetHandle, resolvedDepth);
            compositePass.Setup(renderer.cameraColorTargetHandle, renderer.cameraDepthTargetHandle, resolvedDepth,
                buffers, characterColor);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(compositeMaterial);
            CoreUtils.Destroy(copyDepthMaterial);
            entityLighting?.Dispose();
            gBuffer0?.Release();
            gBuffer1?.Release();
            gBuffer2?.Release();
            normalBuffer?.Release();
            resolvedDepth?.Release();
            characterColor?.Release();
            compositeMaterial = null;
            copyDepthMaterial = null;
            entityLighting = null;
            preparePass = null;
            bufferPass = null;
            copyDepthPass = null;
            compositePass = null;
        }

        void Rebuild()
        {
            CoreUtils.Destroy(compositeMaterial);
            CoreUtils.Destroy(copyDepthMaterial);
            entityLighting?.Dispose();
            compositeMaterial = profile != null && profile.DeferredCompositeShader != null
                ? CoreUtils.CreateEngineMaterial(profile.DeferredCompositeShader)
                : null;
            var copyShader = Shader.Find("Hidden/Universal Render Pipeline/CopyDepth");
            copyDepthMaterial = copyShader != null ? CoreUtils.CreateEngineMaterial(copyShader) : null;
            entityLighting = profile != null && profile.EntityPrepareShader != null
                ? new CorinRestoredEntityLighting(profile.EntityPrepareShader)
                : null;
            if (compositeMaterial == null || copyDepthMaterial == null || entityLighting == null)
                return;
            if (profile.CharacterOverlay == null)
                throw new System.InvalidOperationException("原角色渲染缺少 CharacterOverlayTex，请重建原材质资源。");
            profile.Materials.ApplyRuntimeArrays();
            preparePass = new CharacterPreparePass(profile, entityLighting,
                (RenderPassEvent)((int)RenderPassEvent.BeforeRenderingPrePasses - 1));
            bufferPass = new CharacterBufferPass(RenderPassEvent.AfterRenderingOpaques);
            copyDepthPass = new CopyDepthPass((RenderPassEvent)((int)RenderPassEvent.AfterRenderingOpaques + 1),
                copyDepthMaterial);
            compositePass = new CharacterCompositePass(profile, compositeMaterial,
                (RenderPassEvent)((int)RenderPassEvent.AfterRenderingOpaques + 2));
        }

        sealed class CharacterPreparePass : ScriptableRenderPass
        {
            readonly CorinRestoredRenderProfile profile;
            readonly CorinRestoredEntityLighting entityLighting;
            readonly ProfilingSampler sampler = new("ZZZ Corin Prepare Inputs");

            public CharacterPreparePass(CorinRestoredRenderProfile renderProfile,
                CorinRestoredEntityLighting lighting, RenderPassEvent passEvent)
            {
                profile = renderProfile;
                entityLighting = lighting;
                renderPassEvent = passEvent;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var command = CommandBufferPool.Get();
                using (new ProfilingScope(command, sampler))
                {
                    profile.ApplyGlobals(command, ref renderingData);
                    profile.ResolveMainLight(ref renderingData, out var position, out var color);
                    entityLighting.Prepare(command, ref renderingData, position, color);
                }
                context.ExecuteCommandBuffer(command);
                CommandBufferPool.Release(command);
            }
        }

        sealed class CharacterBufferPass : ScriptableRenderPass
        {
            static readonly List<ShaderTagId> Tags = new()
            {
                new ShaderTagId("CharacterToonDeferred"),
                new ShaderTagId("FaceToonDeferred")
            };
            static readonly List<ShaderTagId> HairShadowTags = new()
            {
                new ShaderTagId("StencilShadowCaster")
            };
            static readonly List<ShaderTagId> OutlineTags = new()
            {
                new ShaderTagId("CharacterOutlineDeferred"),
                new ShaderTagId("FaceOutlineDeferred")
            };
            static readonly List<ShaderTagId> FaceShadowTags = new()
            {
                new ShaderTagId("FaceToonDeferredWithStencilShadow")
            };

            FilteringSettings filtering = new(RenderQueueRange.opaque);
            RTHandle depth;
            RTHandle[] buffers;

            public CharacterBufferPass(RenderPassEvent passEvent)
            {
                renderPassEvent = passEvent;
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }

            public void Setup(RTHandle depthTarget, RTHandle[] colorBuffers)
            {
                depth = depthTarget;
                buffers = colorBuffers;
            }

            public override void OnCameraSetup(CommandBuffer command, ref RenderingData renderingData)
            {
                ConfigureTarget(buffers, depth);
                ConfigureClear(ClearFlag.Color, Color.clear);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var command = CommandBufferPool.Get();
                var drawing = CreateDrawingSettings(Tags, ref renderingData, SortingCriteria.CommonOpaque);
                context.DrawRenderers(renderingData.cullResults, ref drawing, ref filtering);
                command.SetGlobalFloat("_CharacterStencilReadMask", 128f);
                context.ExecuteCommandBuffer(command);
                command.Clear();
                drawing = CreateDrawingSettings(OutlineTags, ref renderingData, SortingCriteria.CommonOpaque);
                context.DrawRenderers(renderingData.cullResults, ref drawing, ref filtering);
                command.SetGlobalFloat("_CharacterStencilReadMask", 255f);
                context.ExecuteCommandBuffer(command);
                command.Clear();
                drawing = CreateDrawingSettings(HairShadowTags, ref renderingData, SortingCriteria.CommonOpaque);
                context.DrawRenderers(renderingData.cullResults, ref drawing, ref filtering);
                command.SetGlobalFloat("_IsStencilReceiverPass", 1f);
                command.SetGlobalFloat("_CharacterStencilComp", 3f);
                context.ExecuteCommandBuffer(command);
                command.Clear();
                drawing = CreateDrawingSettings(FaceShadowTags, ref renderingData, SortingCriteria.CommonOpaque);
                context.DrawRenderers(renderingData.cullResults, ref drawing, ref filtering);
                command.SetGlobalFloat("_IsStencilReceiverPass", 0f);
                command.SetGlobalFloat("_CharacterStencilComp", 8f);
                context.ExecuteCommandBuffer(command);
                CommandBufferPool.Release(command);
            }
        }

        sealed class CharacterCompositePass : ScriptableRenderPass
        {
            static readonly ShaderTagId EyeTag = new("CharacterOpaqueEye");
            static readonly int CameraNormalTexture = Shader.PropertyToID("_CameraNormalTexture");
            static readonly int CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
            static readonly int GBuffer0 = Shader.PropertyToID("_GBuffer0");
            static readonly int GBuffer1 = Shader.PropertyToID("_GBuffer1");
            static readonly int GBuffer2 = Shader.PropertyToID("_GBuffer2");
            static readonly int InputTexture = Shader.PropertyToID("_InputTex");

            readonly CorinRestoredRenderProfile profile;
            readonly Material material;
            readonly ProfilingSampler sampler = new("ZZZ Corin Character Composite");
            FilteringSettings eyeFiltering = new(RenderQueueRange.opaque);
            RTHandle cameraColor;
            RTHandle cameraDepth;
            RTHandle resolvedDepth;
            RTHandle[] buffers;
            RTHandle characterColor;

            public CharacterCompositePass(CorinRestoredRenderProfile renderProfile, Material composite,
                RenderPassEvent passEvent)
            {
                profile = renderProfile;
                material = composite;
                renderPassEvent = passEvent;
            }

            public void Setup(RTHandle colorTarget, RTHandle depthTarget, RTHandle depthTexture,
                RTHandle[] colorBuffers, RTHandle intermediateColor)
            {
                cameraColor = colorTarget;
                cameraDepth = depthTarget;
                resolvedDepth = depthTexture;
                buffers = colorBuffers;
                characterColor = intermediateColor;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var command = CommandBufferPool.Get();
                using (new ProfilingScope(command, sampler))
                {
                    profile.ApplyGlobals(command, ref renderingData);
                    command.SetGlobalTexture(GBuffer0, buffers[0].nameID);
                    command.SetGlobalTexture(GBuffer1, buffers[1].nameID);
                    command.SetGlobalTexture(GBuffer2, buffers[2].nameID);
                    command.SetGlobalTexture(CameraNormalTexture, buffers[3].nameID);
                    command.SetGlobalTexture(CameraDepthTexture, resolvedDepth.nameID);
                    CoreUtils.SetRenderTarget(command, characterColor, cameraDepth, ClearFlag.Color, Color.clear);
                    CoreUtils.DrawFullScreen(command, material, shaderPassId: 0);
                    command.SetGlobalTexture(InputTexture, characterColor.nameID);
                    CoreUtils.SetRenderTarget(command, cameraColor, cameraDepth, ClearFlag.None, Color.clear);
                    CoreUtils.DrawFullScreen(command, material, shaderPassId: 1);
                }
                context.ExecuteCommandBuffer(command);
                command.Clear();
                var eyeDrawing = CreateDrawingSettings(EyeTag, ref renderingData, SortingCriteria.CommonOpaque);
                context.DrawRenderers(renderingData.cullResults, ref eyeDrawing, ref eyeFiltering);
                CommandBufferPool.Release(command);
            }
        }
    }
}
