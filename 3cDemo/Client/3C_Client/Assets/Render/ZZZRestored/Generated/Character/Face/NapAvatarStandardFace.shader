Shader "ZZZ/Restored/NapAvatarStandardFace"
{
    Properties
    {
        [HideInInspector] _MaterialNum("_MaterialNum", Float) = 0
        [HideInInspector] _RenderType("_RenderType", Float) = 0
        [HideInInspector] _Surface("_Surface", Float) = 0
        [HideInInspector] _Color("_Color", Color) = (1,1,1,1)
        [HideInInspector] _MainTex("_MainTex", 2D) = "white" {}
        [HideInInspector] _LightTex("_LightTex", 2D) = "bump" {}
        [HideInInspector] _LightMapUVFlip("_LightMapUVFlip", Float) = 0
        [HideInInspector] _FixedLightDirection("_FixedLightDirection", Float) = 0
        [HideInInspector] _UseOverlayTex("_UseOverlayTex", Float) = 0
        [HideInInspector] _OverlayTexScale("_OverlayTexScale", Float) = 1000
        [HideInInspector] _NoseLineHoriDisp("_NoseLineHoriDisp", Range(0.85,0.98)) = 0.92
        [HideInInspector] _NoseLineLkDnDisp("_NoseLineLkDnDisp", Range(0.5,0.7)) = 0.62
        [HideInInspector] _NoseSpecularScale("_NoseSpecularScale", Range(0,1)) = 1
        [HideInInspector] _NoseLineScale("_NoseLineScale", Range(0,1)) = 1
        [HideInInspector] _HeadMatrixWS2OS0("_HeadMatrixWS2OS0", Vector) = (1,0,0,0)
        [HideInInspector] _HeadMatrixWS2OS1("_HeadMatrixWS2OS1", Vector) = (0,1,0,0)
        [HideInInspector] _HeadMatrixWS2OS2("_HeadMatrixWS2OS2", Vector) = (0,0,1,0)
        [HideInInspector] _HeadMatrixWS2OS3("_HeadMatrixWS2OS3", Vector) = (1,1,1,0)
        [HideInInspector] _AmbientColorCG("_AmbientColorCG", Color) = (0,0,0,1)
        [HideInInspector] _AmbientColorScale("_AmbientColorScale", Range(0,1)) = 1
        [HideInInspector] _VolumeLutScale("_VolumeLutScale", Range(0,1)) = 1
        [HideInInspector] _VolumeLutScale2("_VolumeLutScale2", Range(0,1)) = 1
        [HideInInspector] _VolumeLutScale3("_VolumeLutScale3", Range(0,1)) = 1
        [HideInInspector] _PowMin("_PowMin", Range(1,4)) = 1.25
        [HideInInspector] _PowMax("_PowMax", Range(1,6)) = 1.5
        [HideInInspector] _TransSpeed("_TransSpeed", Range(1,3)) = 1
        [HideInInspector] _DarkAOScale("_DarkAOScale", Range(0,1)) = 0.5
        [HideInInspector] _AOParameters("_AOParameters", Vector) = (1.25,1.5,1,0.5)
        [HideInInspector] _TabSelection("_TabSelection", Float) = 0
        [HideInInspector] _ShallowColor("_ShallowColor", Color) = (0.8,0.8,0.8,1)
        [HideInInspector] _ShallowColor2("_ShallowColor2", Color) = (0.95,0.95,0.95,1)
        [HideInInspector] _ShallowColor3("_ShallowColor3", Color) = (0.8,0.8,0.8,1)
        [HideInInspector] _ShadowColor("_ShadowColor", Color) = (0.6,0.6,0.6,1)
        [HideInInspector] _ShadowColor2("_ShadowColor2", Color) = (0.9,0.9,0.9,1)
        [HideInInspector] _ShadowColor3("_ShadowColor3", Color) = (0.6,0.6,0.6,1)
        [HideInInspector] _CharacterRampTex("_CharacterRampTex", 2D) = "white" {}
        [HideInInspector] _RampTexParams0("_RampTexParams0", Vector) = (0,0,0,0)
        [HideInInspector] _RampTexParams1("_RampTexParams1", Vector) = (0,0,0,0)
        [HideInInspector] _RampTexParams2("_RampTexParams2", Vector) = (0,0,0,0)
        [HideInInspector] _RampSource("_RampSource", Float) = 0
        [HideInInspector] _AlbedoSmoothness("_AlbedoSmoothness", Range(0,1)) = 0.05
        [HideInInspector] _AlbedoSmoothness2("_AlbedoSmoothness2", Range(0,1)) = 0.05
        [HideInInspector] _AlbedoSmoothness3("_AlbedoSmoothness3", Range(0,1)) = 0.05
        [HideInInspector] _ShadowNormalBias("_ShadowNormalBias", Range(0,0.2)) = 0.01
        [HideInInspector] _UseFaceShadowPoint("_UseFaceShadowPoint", Float) = 1
        [HideInInspector] _ShadowColorFadeByZ("_ShadowColorFadeByZ", Float) = 1
        [HideInInspector] _RimGlow("_RimGlow", Float) = 1
        [HideInInspector] _RimGlowLightColor("_RimGlowLightColor", Color) = (0.55,0.55,0.55,1)
        [HideInInspector] _RimGlowLightColor2("_RimGlowLightColor2", Color) = (0.55,0.55,0.55,1)
        [HideInInspector] _RimGlowLightColor3("_RimGlowLightColor3", Color) = (0.55,0.55,0.55,1)
        [HideInInspector] _Outline("_Outline", Float) = 1
        [HideInInspector] _UseAlpha("_UseAlpha", Float) = 0
        [HideInInspector] _OutlineColor("_OutlineColor", Color) = (1,0.5,0.5,1)
        [HideInInspector] _OutlineColor2("_OutlineColor2", Color) = (1,1,1,1)
        [HideInInspector] _OutlineColor3("_OutlineColor3", Color) = (1,0.5,0.5,1)
        [HideInInspector] _OutlineWidth("_OutlineWidth", Range(0,10)) = 1
        [HideInInspector] _MaxOutlineZOffset("_MaxOutlineZOffset", Range(0,1)) = 0.01
        [HideInInspector] _UseChannelMixer("_UseChannelMixer", Float) = 0
        [HideInInspector] _ChannelMixTex("_ChannelMixTex", 2D) = "black" {}
        [HideInInspector] _ChannelMixerUsingUV4("_ChannelMixerUsingUV4", Float) = 0
        [HideInInspector] _RChannelColor("_RChannelColor", Color) = (1,0,0,1)
        [HideInInspector] _GChannelColor("_GChannelColor", Color) = (0,1,0,1)
        [HideInInspector] _BChannelColor("_BChannelColor", Color) = (0,0,1,1)
        [HideInInspector] _AChannelColor("_AChannelColor", Color) = (1,0,1,1)
        [HideInInspector] _RChannelColorPrecomputed("_RChannelColorPrecomputed", Color) = (1,0,0,1)
        [HideInInspector] _GChannelColorPrecomputed("_GChannelColorPrecomputed", Color) = (0,1,0,1)
        [HideInInspector] _BChannelColorPrecomputed("_BChannelColorPrecomputed", Color) = (0,0,1,1)
        [HideInInspector] _AChannelColorPrecomputed("_AChannelColorPrecomputed", Color) = (1,0,1,1)
        [HideInInspector] _SecondaryEmissionForCGLighting("_SecondaryEmissionForCGLighting", Float) = 0
        [HideInInspector] _SecondaryEmissionTexForCGLighting("_SecondaryEmissionTexForCGLighting", 2D) = "white" {}
        [HideInInspector] _SecondaryEmissionTexRotationForCGLighting("_SecondaryEmissionTexRotationForCGLighting", Range(0,1)) = 0
        [HideInInspector] _SecondaryEmissionColorForCGLighting("_SecondaryEmissionColorForCGLighting", Color) = (1,1,1,1)
        [HideInInspector] _SecondaryEmissionUseUV2ForCGLighting("_SecondaryEmissionUseUV2ForCGLighting", Float) = 0
        [HideInInspector] _SecondaryEmissionChannelForCGLighting("_SecondaryEmissionChannelForCGLighting", Float) = 0
        [HideInInspector] _MultiplyAlbedoForCGLighting("_MultiplyAlbedoForCGLighting", Float) = 1
        [HideInInspector] _SecondaryEmissionMaskTexForCGLighting("_SecondaryEmissionMaskTexForCGLighting", 2D) = "white" {}
        [HideInInspector] _SecondaryEmissionMaskChannelForCGLighting("_SecondaryEmissionMaskChannelForCGLighting", Float) = 0
        [HideInInspector] _Override2ToneForCGLighting("_Override2ToneForCGLighting", Float) = 0
        [HideInInspector] _LightSourceForCGLighting("_LightSourceForCGLighting", Float) = 0
        [HideInInspector] _PointPositionForCGLighting("_PointPositionForCGLighting", Vector) = (0,0,0,0)
        [HideInInspector] _PointSpaceForCGLighting("_PointSpaceForCGLighting", Float) = 0
        [HideInInspector] _ShiftAngleForCGLighting("_ShiftAngleForCGLighting", Range(0,360)) = 120
        [HideInInspector] _ColorAForCGLighting("_ColorAForCGLighting", Color) = (1,1,1,1)
        [HideInInspector] _ColorBForCGLighting("_ColorBForCGLighting", Color) = (0,0,0,1)
        [HideInInspector] _LerpPositionForCGLighting("_LerpPositionForCGLighting", Range(0,1)) = 0.5
        [HideInInspector] _SoftnessForCGLighting("_SoftnessForCGLighting", Range(0,1)) = 0
        [HideInInspector] _Override2ToneMultiplyAlbedoForCGLighting("_Override2ToneMultiplyAlbedoForCGLighting", Float) = 0
        [HideInInspector] _Override2ToneBlendModeForCGLighting("_Override2ToneBlendModeForCGLighting", Float) = 0
        [HideInInspector] _MatCapFX("_MatCapFX", Float) = 0
        [HideInInspector] _MatCapTexFx("_MatCapTexFx", 2D) = "white" {}
        [HideInInspector] _MatCapBumpMapFx("_MatCapBumpMapFx", 2D) = "bump" {}
        [HideInInspector] _MatCapBumpScaleFx("_MatCapBumpScaleFx", Range(-5,5)) = 1
        [HideInInspector] _MatCapColorTintFx("_MatCapColorTintFx", Color) = (1,1,1,1)
        [HideInInspector] _MatCapColorBurstFx("_MatCapColorBurstFx", Range(0,10)) = 1
        [HideInInspector] _MatCapAlphaBurstFx("_MatCapAlphaBurstFx", Range(0,10)) = 1
        [HideInInspector] _MatCapUSpeedFx("_MatCapUSpeedFx", Range(-1,1)) = 0
        [HideInInspector] _MatCapVSpeedFx("_MatCapVSpeedFx", Range(-1,1)) = 0
        [HideInInspector] _MatCapNormalUSpeedFx("_MatCapNormalUSpeedFx", Range(-1,1)) = 0
        [HideInInspector] _MatCapNormalVSpeedFx("_MatCapNormalVSpeedFx", Range(-1,1)) = 0
        [HideInInspector] _VertexOffset("_VertexOffset", Range(0,1)) = 0.5
        [HideInInspector] _ColorOverrideAlbedo("_ColorOverrideAlbedo", Vector) = (1,1,1,0)
        [HideInInspector] _MatCapBlendModeFx("_MatCapBlendModeFx", Float) = 0
        [HideInInspector] _SecondaryEmission("_SecondaryEmission", Float) = 0
        [HideInInspector] _SecondaryEmissionTex("_SecondaryEmissionTex", 2D) = "white" {}
        [HideInInspector] _SecondaryEmissionTexSpeed("_SecondaryEmissionTexSpeed", Vector) = (0,0,0,0)
        [HideInInspector] _SecondaryEmissionTexRotation("_SecondaryEmissionTexRotation", Range(0,1)) = 0
        [HideInInspector] _SecondaryEmissionColor("_SecondaryEmissionColor", Color) = (1,1,1,1)
        [HideInInspector] _SecondaryEmissionUseUV2("_SecondaryEmissionUseUV2", Float) = 0
        [HideInInspector] _SecondaryEmissionChannel("_SecondaryEmissionChannel", Float) = 0
        [HideInInspector] _MultiplyAlbedo("_MultiplyAlbedo", Float) = 1
        [HideInInspector] _SecondaryEmissionMaskTex("_SecondaryEmissionMaskTex", 2D) = "white" {}
        [HideInInspector] _SecondaryEmissionMaskChannel("_SecondaryEmissionMaskChannel", Float) = 0
        [HideInInspector] _SpecialWeaponEmission("_SpecialWeaponEmission", Float) = 0
        [HideInInspector] _SpecialWeaponEmissionTex("_SpecialWeaponEmissionTex", 2D) = "white" {}
        [HideInInspector] _SpecialWeaponEmissionTexSpeed("_SpecialWeaponEmissionTexSpeed", Vector) = (0,0,0,0)
        [HideInInspector] _SpecialWeaponEmissionColor("_SpecialWeaponEmissionColor", Color) = (1,1,1,1)
        [HideInInspector] _SpecialWeaponEmissionMaskTex("_SpecialWeaponEmissionMaskTex", 2D) = "white" {}
        [HideInInspector] _SpecialWeaponEmissionColor2("_SpecialWeaponEmissionColor2", Color) = (1,1,1,1)
        [HideInInspector] _SpecialWeaponMergeParam01("_SpecialWeaponMergeParam01", Vector) = (0,0,1,0)
        [HideInInspector] _SpecialWeaponMergeParam02("_SpecialWeaponMergeParam02", Vector) = (0,0,0,0)
        [HideInInspector] _SpecialWeaponMergeParam03("_SpecialWeaponMergeParam03", Vector) = (0,0,0,1)
        [HideInInspector] _ScreenImage("_ScreenImage", Float) = 0
        [HideInInspector] _ScreenScale("_ScreenScale", Float) = 1
        [HideInInspector] _MultiplySrcColor("_MultiplySrcColor", Float) = 0
        [HideInInspector] _ScreenColor("_ScreenColor", Color) = (1,1,1,1)
        [HideInInspector] _ScreenTex("_ScreenTex", 2D) = "gray" {}
        [HideInInspector] _ScreenTexRotation("_ScreenTexRotation", Range(0,1)) = 0
        [HideInInspector] _ScreenTexRotationAxis("_ScreenTexRotationAxis", Vector) = (0.5,0.5,0,0)
        [HideInInspector] _ScreenMask("_ScreenMask", 2D) = "white" {}
        [HideInInspector] _ScreenMaskUV("_ScreenMaskUV", Float) = 0
        [HideInInspector] _ScreenImageUvMove("_ScreenImageUvMove", Vector) = (0,0,0,0)
        [HideInInspector] _Blink("_Blink", Float) = 0
        [HideInInspector] _BlinkFrequency("_BlinkFrequency", Range(0,5)) = 1
        [HideInInspector] _BlinkOpacity("_BlinkOpacity", Vector) = (0,1,0,0)
        [HideInInspector] _Override("_Override", Float) = 0
        [HideInInspector] _OverrideColor("_OverrideColor", Color) = (1,1,1,1)
        [HideInInspector] _Override2Tone("_Override2Tone", Float) = 0
        [HideInInspector] _LightSource("_LightSource", Float) = 0
        [HideInInspector] _PointPosition("_PointPosition", Vector) = (0,0,0,0)
        [HideInInspector] _PointSpace("_PointSpace", Float) = 0
        [HideInInspector] _ShiftAngle("_ShiftAngle", Range(0,360)) = 120
        [HideInInspector] _ColorA("_ColorA", Color) = (1,1,1,1)
        [HideInInspector] _ColorB("_ColorB", Color) = (0,0,0,1)
        [HideInInspector] _LerpPosition("_LerpPosition", Range(0,1)) = 0.5
        [HideInInspector] _Softness("_Softness", Range(0,1)) = 0
        [HideInInspector] _Override2ToneMultiplyAlbedo("_Override2ToneMultiplyAlbedo", Float) = 0
        [HideInInspector] _Override2ToneBlendMode("_Override2ToneBlendMode", Float) = 0
        [HideInInspector] _OverrideRimGlow("_OverrideRimGlow", Float) = 0
        [HideInInspector] _OverrideRimGlowColor("_OverrideRimGlowColor", Color) = (1,1,1,1)
        [HideInInspector] _OverrideRimGlowTexFX("_OverrideRimGlowTexFX", 2D) = "white" {}
        [HideInInspector] _OverrideRimGlowSpeed("_OverrideRimGlowSpeed", Vector) = (0,0,0,0)
        [HideInInspector] _OverrideRimGlowUseUV2("_OverrideRimGlowUseUV2", Float) = 0
        [HideInInspector] _OverrideRimGlowMode("_OverrideRimGlowMode", Float) = 0
        [HideInInspector] _OverrideOutline("_OverrideOutline", Float) = 0
        [HideInInspector] _OverrideOutlineColor("_OverrideOutlineColor", Color) = (0,0,0,0)
        [HideInInspector] _OverrideOutlineTex("_OverrideOutlineTex", 2D) = "white" {}
        [HideInInspector] _OverrideOutlineSpeed("_OverrideOutlineSpeed", Vector) = (0,0,0,0)
        [HideInInspector] _OverrideOutlineUseUV2("_OverrideOutlineUseUV2", Float) = 1
        [HideInInspector] _OutlineFX("_OutlineFX", Float) = 0
        [HideInInspector] _OutlineColorFX("_OutlineColorFX", Color) = (0,0,0,1)
        [HideInInspector] _OutlineWidthFX("_OutlineWidthFX", Range(0,100)) = 1
        [HideInInspector] _UseClipPlane("_UseClipPlane", Float) = 0
        [HideInInspector] _ClipPlane("_ClipPlane", Float) = 0
        [HideInInspector] _HardLight("_HardLight", Float) = 0
        [HideInInspector] _HardLightWidth("_HardLightWidth", Range(0,10)) = 0.1
        [HideInInspector] _HardLightColor("_HardLightColor", Color) = (1,1,1,1)
        [HideInInspector] _SoftLight("_SoftLight", Float) = 0
        [HideInInspector] _SoftLightWidth("_SoftLightWidth", Range(0,10)) = 0.1
        [HideInInspector] _SoftLightColor("_SoftLightColor", Color) = (1,1,1,1)
        [HideInInspector] _PlaneClipReverse("_PlaneClipReverse", Float) = 0
        [HideInInspector] _ClipPlaneXZ("_ClipPlaneXZ", Float) = 0
        [HideInInspector] _ReversePlaneXZ("_ReversePlaneXZ", Float) = 0
        [HideInInspector] _PlaneXZScale("_PlaneXZScale", Vector) = (1,1,1,1)
        [HideInInspector] _VertexStretch("_VertexStretch", Float) = 0
        [HideInInspector] _StretchDirection("_StretchDirection", Float) = 0
        [HideInInspector] _StretchMask("_StretchMask", 2D) = "white" {}
        [HideInInspector] _MaskRChannelUVSpeed("_MaskRChannelUVSpeed", Vector) = (1,1,0,0)
        [HideInInspector] _MaskTexFactor("_MaskTexFactor", Range(0,1)) = 1
        [HideInInspector] _NormalThreshold("_NormalThreshold", Range(-1,1)) = 0
        [HideInInspector] _StretchDistance("_StretchDistance", Range(0,1)) = 0
        [HideInInspector] _StretchVector("_StretchVector", Vector) = (0,0,0,0)
        [HideInInspector] _StretchToPoint("_StretchToPoint", Vector) = (0,0,0,1)
        [HideInInspector] _StretchPercentage("_StretchPercentage", Range(0,10)) = 0
        [HideInInspector] _Glitch("_Glitch", Float) = 0
        [HideInInspector] _BlockMaskTex("_BlockMaskTex", 2D) = "black" {}
        [HideInInspector] _BlockColorA("_BlockColorA", Color) = (1,0,0,1)
        [HideInInspector] _BlockColorB("_BlockColorB", Color) = (0.5,1,0,1)
        [HideInInspector] _BlockColorC("_BlockColorC", Color) = (0,1,1,1)
        [HideInInspector] _BlockColorD("_BlockColorD", Color) = (0.5,0,1,1)
        [HideInInspector] _BlockMoveSpeed("_BlockMoveSpeed", Vector) = (66,88,0,0)
        [HideInInspector] _Transition("_Transition", Float) = 0
        [HideInInspector] _TransitionCompletion("_TransitionCompletion", Range(0,1)) = 0
        [HideInInspector] _TransitionTex("_TransitionTex", 2D) = "white" {}
        [HideInInspector] _TransitionWidth("_TransitionWidth", Range(0,1)) = 0
        [HideInInspector] _TransitionColor("_TransitionColor", Color) = (1,1,1,1)
        [HideInInspector] _AbnormalProperty("_AbnormalProperty", Float) = 0
        [HideInInspector] _AbnormalPropertyElectro("_AbnormalPropertyElectro", Float) = 0
        [HideInInspector] _AbnormalPropertyBurn("_AbnormalPropertyBurn", Float) = 0
        [HideInInspector] _AbnormalPropertyFreeze("_AbnormalPropertyFreeze", Float) = 0
        [HideInInspector] _PropertyType("_PropertyType", Float) = 0
        [HideInInspector] _PropertyColor("_PropertyColor", Color) = (2.635,3.388,23.968,1)
        [HideInInspector] _FresnelColor("_FresnelColor", Color) = (0,0.2,1.625,1)
        [HideInInspector] _DetailColor("_DetailColor", Color) = (0.384,0.565,1.498,1)
        [HideInInspector] _PropertyTexUseUV2("_PropertyTexUseUV2", Float) = 0
        [HideInInspector] _PropertyMaskUseUV2("_PropertyMaskUseUV2", Float) = 0
        [HideInInspector] _PropertyMask2UseUV2("_PropertyMask2UseUV2", Float) = 0
        [HideInInspector] _PropertyNormalUseUV2("_PropertyNormalUseUV2", Float) = 0
        [HideInInspector] _PropertyMaskChannel("_PropertyMaskChannel", Float) = 0
        [HideInInspector] _PropertyMask2Channel("_PropertyMask2Channel", Float) = 0
        [HideInInspector] _PropertyTexUVSpeed("_PropertyTexUVSpeed", Vector) = (3,3,0,0)
        [HideInInspector] _PropertyTexUVFlipSpeed("_PropertyTexUVFlipSpeed", Range(0,50)) = 25
        [HideInInspector] _PropertyMaskUVSpeed("_PropertyMaskUVSpeed", Vector) = (8,2,0,0)
        [HideInInspector] _PropertyMaskUVFlipSpeed("_PropertyMaskUVFlipSpeed", Range(0,50)) = 50
        [HideInInspector] _PropertyMask2UVSpeed("_PropertyMask2UVSpeed", Vector) = (0,0,0,0)
        [HideInInspector] _PropertyNormalUVSpeed("_PropertyNormalUVSpeed", Vector) = (0,0,0,0)
        [HideInInspector] _FresnelWidth("_FresnelWidth", Range(0,10)) = 3.5
        [HideInInspector] _FresnelMaskWidth("_FresnelMaskWidth", Range(0,10)) = 5
        [HideInInspector] _FresnelFlashing("_FresnelFlashing", Range(0,10)) = 4
        [HideInInspector] _DitherAlpha("_DitherAlpha", Range(0,1)) = 1
        [HideInInspector] _DitherAlpha2("_DitherAlpha2", Range(0,1)) = 1
        [HideInInspector] _DecolorizationContrast("_DecolorizationContrast", Vector) = (1,1,0,0)
        [HideInInspector] _ReceiveShadows("_ReceiveShadows", Float) = 1
        [HideInInspector] _ReceiveAddShadows("_ReceiveAddShadows", Float) = 1
        [HideInInspector] _ZWrite("_ZWrite", Float) = 1
        [HideInInspector] _ZTest("_ZTest", Float) = 4
        [HideInInspector] _Cull("_Cull", Float) = 2
        [HideInInspector] _DoubleSided("_DoubleSided", Float) = 0
        [HideInInspector] _IgnoreTimeScale("_IgnoreTimeScale", Float) = 0
        [HideInInspector] _LightDirectionFromCamera("_LightDirectionFromCamera", Range(0,1)) = 0
        [HideInInspector] _CameraToLightRadian("_CameraToLightRadian", Float) = 0
        [HideInInspector] _MiddlePointPosition("_MiddlePointPosition", Vector) = (0,0,0,0)
        [HideInInspector] _CharacterMainLightData("_CharacterMainLightData", Vector) = (-1,0,0,0)
        [HideInInspector] _CharacterMainLightData1("_CharacterMainLightData1", Vector) = (0,0,0,0)
        [HideInInspector] _OverrideMainLightParam("_OverrideMainLightParam", Float) = 0
        [HideInInspector] _OverrideMainLightParam2("_OverrideMainLightParam2", Float) = 0
        [HideInInspector] _OverrideMainLightParam3("_OverrideMainLightParam3", Float) = 0
        [HideInInspector] _OverrideMainLightColor("_OverrideMainLightColor", Color) = (1,1,1,1)
        [HideInInspector] _OverrideMainLightColor2("_OverrideMainLightColor2", Color) = (1,1,1,1)
        [HideInInspector] _OverrideMainLightColor3("_OverrideMainLightColor3", Color) = (1,1,1,1)
        [HideInInspector] _BrightMultiplier("_BrightMultiplier", Range(0,100)) = 1
        [HideInInspector] _BrightMultiplier2("_BrightMultiplier2", Range(0,100)) = 1
        [HideInInspector] _BrightMultiplier3("_BrightMultiplier3", Range(0,100)) = 1
        [HideInInspector] _UseInvSecondaryEmissionMask("_UseInvSecondaryEmissionMask", Float) = 0
        [HideInInspector] _CharacterSimplify("_CharacterSimplify", Float) = 0
        [HideInInspector] _PerObjectShadowIntensity("_PerObjectShadowIntensity", Range(0,1)) = 1
        [HideInInspector] _PerObjectShadowIntensity2("_PerObjectShadowIntensity2", Range(0,1)) = 1
        [HideInInspector] _PerObjectShadowIntensity3("_PerObjectShadowIntensity3", Range(0,1)) = 1
        [HideInInspector] _ShadowZOffset("_ShadowZOffset", Float) = 0
        [HideInInspector] _ShadowZOffsetWholeMesh("_ShadowZOffsetWholeMesh", Float) = 0
        [HideInInspector] _AdditionalLightIntensity("_AdditionalLightIntensity", Float) = 1
        [HideInInspector] _MarkAsIgnisFatuusMask("_MarkAsIgnisFatuusMask", Float) = 0
        [HideInInspector] _MarkAsGhostMask("_MarkAsGhostMask", Float) = 0
        [HideInInspector] _MarkAsVfxMask("_MarkAsVfxMask", Float) = 0
        [HideInInspector] _ShadowDitherOff("_ShadowDitherOff", Float) = 0
        [HideInInspector] _StencilReadMask("_StencilReadMask", Float) = 1
        [HideInInspector] _StencilComFunc("_StencilComFunc", Float) = 0
        [HideInInspector] _LayerIDColor("_LayerIDColor", Color) = (1,1,1,1)
        [HideInInspector] _StencilRef("_StencilRef", Float) = 128
        [HideInInspector] _CharacterStencil("_CharacterStencil", Float) = 128
        [HideInInspector] _StencilWriteMask("_StencilWriteMask", Float) = 255
        [HideInInspector] _StencilRefShadow("_StencilRefShadow", Float) = 132
        [HideInInspector] _StencilWriteMaskShadow("_StencilWriteMaskShadow", Float) = 128
        [HideInInspector] _ZWritePreZ("_ZWritePreZ", Float) = 1
        [HideInInspector] _ZTestPreZ("_ZTestPreZ", Float) = 4
        [HideInInspector] _CharacterHalfResSrcBlend("_CharacterHalfResSrcBlend", Float) = 1
        [HideInInspector] _CharacterHalfResDstBlend("_CharacterHalfResDstBlend", Float) = 0
        [HideInInspector] _CharacterHalfResAlphaSrcBlend("_CharacterHalfResAlphaSrcBlend", Float) = 1
        [HideInInspector] _CharacterHalfResAlphaDstBlend("_CharacterHalfResAlphaDstBlend", Float) = 0
        [HideInInspector] _SpecSocketBindPoseMat0("_SpecSocketBindPoseMat0", Vector) = (1,0,0,0)
        [HideInInspector] _SpecSocketBindPoseMat1("_SpecSocketBindPoseMat1", Vector) = (0,1,0,0)
        [HideInInspector] _SpecSocketBindPoseMat2("_SpecSocketBindPoseMat2", Vector) = (0,0,1,0)
        [HideInInspector] _SpecSocketBindPoseMat3("_SpecSocketBindPoseMat3", Vector) = (0,0,0,1)
        [HideInInspector] _SpecSocketPosOffset("_SpecSocketPosOffset", Vector) = (0,0,0,1)
        [HideInInspector] _SpecSocketGPUAnimInstanceID("_SpecSocketGPUAnimInstanceID", Float) = 0
        [HideInInspector] _SpecSocketTotalNumberOfBones("_SpecSocketTotalNumberOfBones", Float) = 0
        [HideInInspector] _SpecSocketBoneIndex("_SpecSocketBoneIndex", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull Back
            ZTest LEqual
            ZWrite On
            HLSLPROGRAM
            #pragma target 5.0
            #pragma only_renderers d3d11
            #pragma vertex main
            #pragma fragment main
            #if defined(SHADER_STAGE_VERTEX)
                #include "OriginalStage0.hlsl"
            #elif defined(SHADER_STAGE_FRAGMENT)
                #include "OriginalStage8.hlsl"
            #endif
            ENDHLSL
        }
        Pass
        {
            Name "FaceOutlineDeferred"
            Tags { "LightMode"="FaceOutlineDeferred" }
            Cull Front
            ZTest LEqual
            ZWrite On
            HLSLPROGRAM
            #pragma target 5.0
            #pragma only_renderers d3d11
            #pragma vertex main
            #pragma fragment main
            #if defined(SHADER_STAGE_VERTEX)
                #include "OriginalStage12.hlsl"
            #elif defined(SHADER_STAGE_FRAGMENT)
                #include "OriginalStage44.hlsl"
            #endif
            ENDHLSL
        }
        Pass
        {
            Name "FaceToonDeferred"
            Tags { "LightMode"="FaceToonDeferred" }
            Cull Back
            ZTest [_ZTestPreZ]
            ZWrite [_ZWritePreZ]
            Stencil
            {
                Ref [_StencilRef]
                ReadMask [_CharacterStencilReadMask]
                WriteMask [_StencilWriteMask]
                Comp [_CharacterStencilComp]
                Pass [_CharacterStencilPass]
                Fail Keep
                ZFail Keep
            }
            HLSLPROGRAM
            #pragma target 5.0
            #pragma only_renderers d3d11
            #pragma vertex main
            #pragma fragment main
            #if defined(SHADER_STAGE_VERTEX)
                #include "OriginalStage60.hlsl"
            #elif defined(SHADER_STAGE_FRAGMENT)
                #include "OriginalStage1980.hlsl"
            #endif
            ENDHLSL
        }
        Pass
        {
            Name "FaceToonDeferredWithStencilShadow"
            Tags { "LightMode"="FaceToonDeferredWithStencilShadow" }
            Cull Back
            ZTest [_ZTestPreZ]
            ZWrite [_ZWritePreZ]
            Stencil
            {
                Ref [_StencilRefShadow]
                ReadMask [_CharacterStencilReadMask]
                WriteMask [_StencilWriteMaskShadow]
                Comp [_CharacterStencilComp]
                Pass [_CharacterStencilPass]
                Fail Keep
                ZFail Keep
            }
            HLSLPROGRAM
            #pragma target 5.0
            #pragma only_renderers d3d11
            #pragma vertex main
            #pragma fragment main
            #if defined(SHADER_STAGE_VERTEX)
                #include "OriginalStage60.hlsl"
            #elif defined(SHADER_STAGE_FRAGMENT)
                #include "OriginalStage1980.hlsl"
            #endif
            ENDHLSL
        }
        Pass
        {
            Name "CharDepthOnly"
            Tags { "LightMode"="CharDepthOnly" }
            Cull Back
            ZTest LEqual
            ZWrite On
            Blend 0 Zero One, Zero One
            Blend 1 Zero One, Zero One
            Blend 2 Zero One, Zero One
            Blend 3 Zero One, Zero One
            Blend 4 One Zero, One Zero
            Blend 5 One Zero, One Zero
            Blend 6 One Zero, One Zero
            Blend 7 One Zero, One Zero
            Stencil
            {
                Ref [_CharacterStencil]
                ReadMask [_CharacterStencilReadMask]
                WriteMask [_CharacterStencilWriteMask]
                Comp [_CharacterStencilComp]
                Pass [_CharacterStencilPass]
                Fail Keep
                ZFail Keep
            }
            HLSLPROGRAM
            #pragma target 5.0
            #pragma only_renderers d3d11
            #pragma vertex main
            #pragma fragment main
            #if defined(SHADER_STAGE_VERTEX)
                #include "OriginalStage2940.hlsl"
            #elif defined(SHADER_STAGE_FRAGMENT)
                #include "OriginalStage2946.hlsl"
            #endif
            ENDHLSL
        }
    }
}
