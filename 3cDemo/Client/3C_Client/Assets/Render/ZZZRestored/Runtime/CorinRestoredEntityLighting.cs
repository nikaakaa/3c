using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ZZZ.Rendering.Restored
{
    public sealed class CorinRestoredEntityLighting : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        struct PrepareInput
        {
            public Vector3 Position;
            public float ForceInShadow;
            public Vector3 FaceForward;
            public int BlendLightStart;
            public Vector3 FacePosition;
            public int BlendLightEnd;
            public Vector4 MainLightData;
            public Vector4 ToonLightIndices;
            public int ToonLightCount;
            public float IsGpuCrowd;
            public float IndexChanged;
            public float DirectionalLightSize;
            public Matrix4x4 WorldToObjectMatrix;
            public Vector3 CameraPosition;
            public int IsFirstTimeCalculateRadian;
            public Vector4 OverridenMainLightColor;
            public float LockLightAngleRatio;
            public float FixShadowCoverageOutOfFrustum;
            public float DisableLightBlendShadowTemporalFade;
            public float Dummy;
        }

        readonly ComputeShader shader;
        readonly int kernel;
        ComputeBuffer input;
        ComputeBuffer output;
        ComputeBuffer lightData;
        ComputeBuffer giPosition;
        ComputeBuffer giColor;
        ComputeBuffer giIndex;
        ComputeBuffer blendLightIndices;
        RenderTexture shadowTexture;
        PrepareInput[] inputs;
        readonly List<CorinRestoredRenderEntity> activeEntities = new();

        public CorinRestoredEntityLighting(ComputeShader computeShader)
        {
            if (Marshal.SizeOf<PrepareInput>() != 208)
                throw new InvalidOperationException("Nap entity prepare input layout is not 208 bytes");
            shader = computeShader;
            kernel = shader.FindKernel("NapEntityPrepare");
        }

        public int Prepare(CommandBuffer command, ref RenderingData renderingData, Vector4 mainLightPosition,
            Vector4 mainLightColor)
        {
            CorinRestoredRenderEntity.CollectActive(activeEntities);
            var count = activeEntities.Count;
            if (count == 0)
            {
                command.SetGlobalInt("_RenderedEntityCount", 0);
                return 0;
            }
            EnsureCapacity(count);
            var camera = renderingData.cameraData.camera;
            for (var index = 0; index < count; index++)
            {
                var entity = activeEntities[index];
                entity.ApplyEntityIndex(index, mainLightPosition);
                inputs[index] = new PrepareInput
                {
                    Position = entity.Position,
                    FaceForward = entity.FaceForward,
                    FacePosition = entity.FacePosition,
                    DirectionalLightSize = 50f,
                    WorldToObjectMatrix = entity.WorldToObject,
                    CameraPosition = camera.transform.position,
                    FixShadowCoverageOutOfFrustum = 1f
                };
            }
            input.SetData(inputs, 0, 0, count);
            command.SetComputeIntParam(shader, "_RenderedEntityCount", count);
            command.SetComputeIntParam(shader, "_CharacterLightDataList", count);
            command.SetComputeIntParam(shader, "_CharacterLightDataCount", -1);
            command.SetComputeIntParam(shader, "_PunctualLightCountForChar", 0);
            command.SetComputeIntParam(shader, "_NapEntityBlendLight", 0);
            command.SetComputeIntParam(shader, "_NapCharacterGIEnabled", 0);
            command.SetComputeIntParam(shader, "_CharacterGIDataLength", 0);
            command.SetComputeIntParam(shader, "_CharacterGIDataRowSize", 0);
            command.SetComputeIntParam(shader, "_CharacterGIDataColSize", 0);
            command.SetComputeFloatParam(shader, "_CharacterGISampleRange", 0f);
            command.SetComputeFloatParam(shader, "_LightCullEnabled", 0f);
            command.SetComputeFloatParam(shader, "_LightFadeDistance", 0f);
            command.SetComputeFloatParam(shader, "_LightCullDistance", 10000f);
            command.SetComputeFloatParam(shader, "_FixCharDirLightNoIntensityBlendWeight", 0f);
            command.SetComputeFloatParam(shader, "_IsBlackCanvasOn", 0f);
            command.SetComputeVectorParam(shader, "_AvatarMainLightPosition", mainLightPosition);
            command.SetComputeVectorParam(shader, "_AvatarMainLightColor", mainLightColor);
            command.SetComputeVectorParam(shader, "_CharacterAmbient", new Vector4(0.2f, 0.16f, 0.16f, 1f));
            command.SetComputeVectorParam(shader, "_TaaFrameInfo", new Vector4(0.6f, 0f, 0f, 1f));
            command.SetComputeVectorParam(shader, "_MainLightShadowParamsNew", Vector4.zero);
            command.SetComputeVectorParam(shader, "_CloudShadowMoveSpeed", new Vector4(20f, 20f, 1f, 0f));
            command.SetComputeMatrixParam(shader, "_GlobalTimeParamsB", Matrix4x4.identity);
            command.SetComputeMatrixParam(shader, "_CloudShadowRotateMatrix", Matrix4x4.identity);
            command.SetComputeVectorArrayParam(shader, "_FrustumePlanes", FrustumPlanes(camera));
            command.SetComputeMatrixArrayParam(shader, "_MainLightWorldToShadow", IdentityMatrices(5));
            command.SetComputeBufferParam(shader, kernel, "_RenderEntityPrepareInput", input);
            command.SetComputeBufferParam(shader, kernel, "_NapEntityGPUData", output);
            command.SetComputeBufferParam(shader, kernel, "_LightDatasForChar", lightData);
            command.SetComputeBufferParam(shader, kernel, "_CharacterGIPositionBuffer", giPosition);
            command.SetComputeBufferParam(shader, kernel, "_CharacterGIColorBuffer", giColor);
            command.SetComputeBufferParam(shader, kernel, "_CharacterGIIndexBuffer", giIndex);
            command.SetComputeBufferParam(shader, kernel, "_CharacterBlendLightIndices", blendLightIndices);
            command.SetComputeTextureParam(shader, kernel, "_MainLightShadowmapTexture", shadowTexture);
            command.SetComputeTextureParam(shader, kernel, "_CloudShadow", Texture2D.blackTexture);
            command.DispatchCompute(shader, kernel, Mathf.CeilToInt(count / 64f), 1, 1);
            command.SetGlobalBuffer("_NapEntityGPUData", output);
            command.SetGlobalInt("_RenderedEntityCount", count);
            return count;
        }

        public void Dispose()
        {
            input?.Dispose();
            output?.Dispose();
            lightData?.Dispose();
            giPosition?.Dispose();
            giColor?.Dispose();
            giIndex?.Dispose();
            blendLightIndices?.Dispose();
            if (shadowTexture != null)
            {
                shadowTexture.Release();
                CoreUtils.Destroy(shadowTexture);
            }
        }

        void EnsureCapacity(int count)
        {
            if (input != null && input.count >= count)
                return;
            input?.Dispose();
            output?.Dispose();
            var capacity = Mathf.NextPowerOfTwo(count);
            input = new ComputeBuffer(capacity, 208, ComputeBufferType.Structured);
            output = new ComputeBuffer(capacity, 128, ComputeBufferType.Structured);
            inputs = new PrepareInput[capacity];
            output.SetData(new uint[capacity * 32]);
            if (lightData != null)
                return;
            lightData = new ComputeBuffer(1, 360, ComputeBufferType.Structured);
            giPosition = new ComputeBuffer(1, 12, ComputeBufferType.Structured);
            giColor = new ComputeBuffer(1, 8, ComputeBufferType.Structured);
            giIndex = new ComputeBuffer(1, 4, ComputeBufferType.Structured);
            blendLightIndices = new ComputeBuffer(1, 4, ComputeBufferType.Structured);
            lightData.SetData(new uint[90]);
            giPosition.SetData(new uint[3]);
            giColor.SetData(new uint[2]);
            giIndex.SetData(new uint[1]);
            blendLightIndices.SetData(new uint[1]);
            shadowTexture = new RenderTexture(1, 1, 24, RenderTextureFormat.Shadowmap)
            {
                dimension = TextureDimension.Tex2DArray,
                volumeDepth = 1,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            shadowTexture.Create();
            var previous = RenderTexture.active;
            Graphics.SetRenderTarget(shadowTexture, 0, CubemapFace.Unknown, 0);
            GL.Clear(true, false, Color.black, 1f);
            RenderTexture.active = previous;
        }

        static Vector4[] FrustumPlanes(Camera camera)
        {
            var source = GeometryUtility.CalculateFrustumPlanes(camera);
            var result = new Vector4[source.Length];
            for (var index = 0; index < source.Length; index++)
                result[index] = new Vector4(source[index].normal.x, source[index].normal.y,
                    source[index].normal.z, source[index].distance);
            return result;
        }

        static Matrix4x4[] IdentityMatrices(int count)
        {
            var result = new Matrix4x4[count];
            for (var index = 0; index < count; index++)
                result[index] = Matrix4x4.identity;
            return result;
        }
    }
}
