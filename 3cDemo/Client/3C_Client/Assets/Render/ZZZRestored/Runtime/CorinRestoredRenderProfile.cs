using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ZZZ.Rendering.Restored
{
    [CreateAssetMenu(menuName = "ZZZ/Restored/Corin Render Profile")]
    public sealed class CorinRestoredRenderProfile : ScriptableObject
    {
        [SerializeField] CorinRestoredMaterialSet materials;
        [SerializeField] Shader deferredCompositeShader;
        [SerializeField] ComputeShader entityPrepareShader;
        [SerializeField] Texture2D characterLut;
        [SerializeField] Texture2D characterOverlay;
        [SerializeField] CorinRestoredOutlineParameters outline;
        [SerializeField] Vector4 characterAmbient;
        [SerializeField] Vector4 ambientGradientColor;
        [SerializeField] Vector4 characterLightTonemapParams;
        [SerializeField] Vector4 postFrontTint;
        [SerializeField] Vector4 postShallowTint;
        [SerializeField] Vector4 postShallowFadeTint;
        [SerializeField] Vector4 postShadowTint;
        [SerializeField] Vector4 postSssTint;
        [SerializeField] Vector4 postShadowFadeTint;
        [SerializeField] Vector4 skinFrontTint;
        [SerializeField] Vector4 skinShallowTint;
        [SerializeField] Vector4 skinShallowFadeTint;
        [SerializeField] Vector4 skinShadowTint;
        [SerializeField] Vector4 skinSssTint;
        [SerializeField] Vector4 skinShadowFadeTint;
        [SerializeField] Matrix4x4 lutToneParams;
        [SerializeField] Vector4 lutParams;
        [SerializeField] Matrix4x4 sceneWeatherParamsPart1;
        [SerializeField] Matrix4x4 sceneFogParamsPart1;
        [SerializeField] Matrix4x4 sceneFogParamsPart2;
        [SerializeField] Matrix4x4 sceneFogParamsPart3;

        public CorinRestoredMaterialSet Materials => materials;
        public Shader DeferredCompositeShader => deferredCompositeShader;
        public ComputeShader EntityPrepareShader => entityPrepareShader;
        public Texture2D CharacterLut => characterLut;
        public Texture2D CharacterOverlay => characterOverlay;
        public CorinRestoredOutlineParameters Outline => outline;

        public void Initialize(CorinRestoredMaterialSet materialSet, Shader compositeShader, ComputeShader prepareShader,
            Texture2D lut, Texture2D overlay, Matrix4x4 weather, Matrix4x4 fog1, Matrix4x4 fog2, Matrix4x4 fog3,
            CorinRestoredOutlineParameters outlineParameters)
        {
            materials = materialSet;
            deferredCompositeShader = compositeShader;
            entityPrepareShader = prepareShader;
            characterLut = lut;
            characterOverlay = overlay;
            outline = outlineParameters;
            sceneWeatherParamsPart1 = weather;
            sceneFogParamsPart1 = fog1;
            sceneFogParamsPart2 = fog2;
            sceneFogParamsPart3 = fog3;
            characterAmbient = new Vector4(0.2f, 0.16f, 0.16f, 1f);
            ambientGradientColor = new Vector4(0.7019608f, 0.7803922f, 0.8392158f, 0.2f);
            characterLightTonemapParams = new Vector4(100f, 1000f, 0f, 0.0001f);
            postFrontTint = new Vector4(1f, 0.97647065f, 0.8745099f, 1f);
            postShallowTint = new Vector4(1f, 0.90196085f, 0.9333334f, 1f);
            postShallowFadeTint = new Vector4(0.8000001f, 0.6784314f, 0.72156864f, 1f);
            postShadowTint = new Vector4(0.72156864f, 0.69411767f, 0.7686275f, 1f);
            postSssTint = new Vector4(1f, 0.8235295f, 0.78823537f, 1f);
            postShadowFadeTint = new Vector4(0.6517895f, 0.61200005f, 0.72f, 1f);
            skinFrontTint = postFrontTint;
            skinShallowTint = new Vector4(1f, 0.90196085f, 0.9294118f, 1f);
            skinShallowFadeTint = new Vector4(0.7803922f, 0.60784316f, 0.6666667f, 1f);
            skinShadowTint = new Vector4(0.7686275f, 0.654902f, 0.7254902f, 1f);
            skinSssTint = new Vector4(1f, 0.79215693f, 0.7490196f, 1f);
            skinShadowFadeTint = new Vector4(0.7490196f, 0.57254905f, 0.62352943f, 1f);
            lutToneParams = Matrix4x4.zero;
            lutToneParams.SetColumn(0, new Vector4(0f, 0f, 0.5f, 0f));
            lutToneParams.SetColumn(3, new Vector4(1f, 1f, 0f, 0f));
            lutParams = new Vector4(0.0009765625f, 0.03125f, 31f, 1.0717734f);
        }

        public void ApplyGlobals(CommandBuffer command, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            var projection = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
            var view = camera.worldToCameraMatrix;
            var viewProjection = projection * view;
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            ResolveMainLight(ref renderingData, out var mainLightPosition, out var mainLightColor);
            command.SetGlobalVector("_AvatarMainLightPosition", mainLightPosition);
            command.SetGlobalVector("_AvatarMainLightColor", mainLightColor);
            command.SetGlobalVector("_CharacterAmbient", characterAmbient);
            command.SetGlobalVector("_AmbientGradientColor", ambientGradientColor);
            command.SetGlobalVector("_CharacterLightTonemapParams", characterLightTonemapParams);
            command.SetGlobalVector("_PostFrontTint", postFrontTint);
            command.SetGlobalVector("_PostShallowTint", postShallowTint);
            command.SetGlobalVector("_PostShallowFadeTint", postShallowFadeTint);
            command.SetGlobalVector("_PostShadowTint", postShadowTint);
            command.SetGlobalVector("_PostSssTint", postSssTint);
            command.SetGlobalVector("_PostShadowFadeTint", postShadowFadeTint);
            command.SetGlobalVector("_SkinFrontTint", skinFrontTint);
            command.SetGlobalVector("_SkinShallowTint", skinShallowTint);
            command.SetGlobalVector("_SkinShallowFadeTint", skinShallowFadeTint);
            command.SetGlobalVector("_SkinShadowTint", skinShadowTint);
            command.SetGlobalVector("_SkinSssTint", skinSssTint);
            command.SetGlobalVector("_SkinShadowFadeTint", skinShadowFadeTint);
            command.SetGlobalMatrix("_FXCC_LutToneParams", lutToneParams);
            command.SetGlobalVector("_Lut_Params_Char", lutParams);
            command.SetGlobalTexture("_InternalLut_Char", characterLut);
            command.SetGlobalTexture("_CharacterOverlayTex", characterOverlay);
            command.SetGlobalVector("_CharStyleParams", outline.CharacterStyle);
            command.SetGlobalVector("_PostOutlineTint", outline.PostTint);
            command.SetGlobalVector("_BloomThreshold", outline.BloomThreshold);
            command.SetGlobalVector("_AlphaBlendAlphaParams", outline.AlphaBlend);
            command.SetGlobalFloat("_GlobalMipBias", outline.GlobalMipBias);
            command.SetGlobalMatrix("_SceneWeatherParamsPart1", sceneWeatherParamsPart1);
            command.SetGlobalMatrix("_SceneFogParamsPart1", sceneFogParamsPart1);
            command.SetGlobalMatrix("_SceneFogParamsPart2", sceneFogParamsPart2);
            command.SetGlobalMatrix("_SceneFogParamsPart3", sceneFogParamsPart3);
            command.SetGlobalMatrix("_InvViewProjMatrix", viewProjection.inverse);
            command.SetGlobalMatrix("_NonJitteredViewProjMatrix", viewProjection);
            command.SetGlobalMatrix("_NonJitteredProjMatrix", projection);
            command.SetGlobalMatrix("_PrevViewProjMatrix", viewProjection);
            command.SetGlobalMatrix("_PrevViewMatrix", view);
            command.SetGlobalMatrix("_PrevProjMatrix", projection);
            command.SetGlobalVector("_ScreenSize", new Vector4(descriptor.width, descriptor.height,
                1f / descriptor.width, 1f / descriptor.height));
            command.SetGlobalVector("_TaaFrameInfo", new Vector4(0.6f, 0f, 0f, 1f));
            command.SetGlobalVector("_RimGlowColorForChara", Vector4.one);
            command.SetGlobalVector("_RimGlowStyleForChara", Vector4.one);
            command.SetGlobalVector("_PerObjectShadowAtlas_TexelSize", new Vector4(0.5f, 0.5f, 2f, 2f));
            command.SetGlobalVector("_MainLightShadowParamsNew", Vector4.one);
            command.SetGlobalVector("_ExposureParams", new Vector4(1f, 0f, 1f, 0f));
            command.SetGlobalFloat("_CharacterSampleTextureBias", -2f);
            command.SetGlobalFloat("_GlobalAdditionalLightIntensity", 0.1f);
            command.SetGlobalFloat("_CharacterMatCapEnable", 1f);
            command.SetGlobalFloat("_RimGlowWidthForCharacter", 1f);
            command.SetGlobalFloat("_RimGlowIntensityForChara", 0f);
            command.SetGlobalFloat("_is_apply_lut_character_on", characterLut != null ? 1f : 0f);
            command.SetGlobalFloat("_is_main_light_shadows_on", 0f);
            command.SetGlobalFloat("_NapCharacterGIEnabled", 0f);
            command.SetGlobalFloat("_CharacterGIDataLength", 0f);
            command.SetGlobalFloat("_CharacterGIDataRowSize", 0f);
            command.SetGlobalFloat("_CharacterGIDataColSize", 0f);
            command.SetGlobalFloat("_IsBlackCanvasOn", 0f);
            command.SetGlobalFloat("_IsStencilReceiverPass", 0f);
            command.SetGlobalFloat("_CharacterStencilReadMask", 255f);
            command.SetGlobalFloat("_CharacterStencilWriteMask", 255f);
            command.SetGlobalFloat("_CharacterStencilComp", 8f);
            command.SetGlobalFloat("_CharacterStencilPass", 2f);
            command.SetGlobalFloat("_StencilShadowStencilRef", 132f);
            command.SetGlobalFloat("_StencilShadowStencil", 4f);
            command.SetGlobalFloat("_StencilShadowBlendDebugMode", 0f);
            command.SetGlobalVector("_ClipSpaceOffset", Vector4.zero);
            command.SetGlobalVector("_PackedParams0", Vector4.zero);
            command.SetGlobalVector("_PackedParams1", Vector4.zero);
            command.SetGlobalVector("_EntityInfo", Vector4.zero);
        }

        public void ResolveMainLight(ref RenderingData renderingData, out Vector4 mainLightPosition,
            out Vector4 mainLightColor)
        {
            mainLightPosition = new Vector4(0f, 0f, 1f, 0f);
            mainLightColor = Vector4.zero;
            var mainLightIndex = renderingData.lightData.mainLightIndex;
            if (mainLightIndex >= 0)
            {
                var light = renderingData.lightData.visibleLights[mainLightIndex];
                if (light.lightType == LightType.Directional)
                {
                    var direction = -light.localToWorldMatrix.GetColumn(2);
                    mainLightPosition = new Vector4(direction.x, direction.y, direction.z, 0f);
                    var color = light.finalColor;
                    mainLightColor = new Vector4(color.r, color.g, color.b, 1f);
                }
            }
        }
    }
}
