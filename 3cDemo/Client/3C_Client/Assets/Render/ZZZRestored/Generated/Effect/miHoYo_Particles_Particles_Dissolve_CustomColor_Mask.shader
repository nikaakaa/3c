Shader "miHoYo/Particles/Particles_Dissolve_CustomColor_Mask"
{
    Properties
    {
        _group_basic ("Basic@基本参数", Float) = 0.0
        _BlendMode ("Blend Mode@混合模式", Float) = 0.0
        _Cull ("Cull@背面剔除模式", Float) = 0.0
        _AlphaFade ("Alpha Fade@透明度控制", Float) = 1.0
        _AlphaFade_Timeline ("Alpha Fade Timeline@Timeline中的透明度控制", Float) = 1.0
        _IgnoreVertexColor ("Ignore Vertex Color@忽略顶点色", Float) = 0.0
        _CustomData1Z ("CustomData1Z", Float) = 0.0
        _CustomData1W ("CustomData1W", Float) = 0.0
        _SrcFactor ("Src Factor", Float) = 1.0
        _DstFactor ("Dst Factor", Float) = 10.0
        _HalfResSrcFactor ("Src Factor", Float) = 1.0
        _HalfResDstFactor ("Dst Factor", Float) = 5.0
        _HalfResSrcAlphaFactor ("Src Factor", Float) = 7.0
        _HalfResDstAlphaFactor ("Dst Factor", Float) = 0.0
        _group_maintex ("MainTex@主图", Float) = 0.0
        _MainTex ("Particle Texture@主图", 2D) = "white" {}
        _MainTexUVMode ("UV Mode@UV模式", Float) = 0.0
        _MainTexClampU ("Clamp U@限制U", Float) = 0.0
        _MainTexClampV ("Clamp V@限制V", Float) = 0.0
        _MainTexFlip ("Flip@翻转", Float) = 0.0
        _MainTexRotation ("Rotation@旋转90度", Float) = 0.0
        _MainTexScreenUvScaleWithCamera ("ScreenUV Scale With Camera@屏幕UV适应", Float) = 0.0
        _MainTexMoveStep ("Move Step@流动步长", Vector) = (0.0, 0.0, 0.0, 0.0)
        _MainTexUVSpeed ("Main Tex UV Speed@流动速度", Vector) = (0.0, 0.0, 0.0, 0.0)
        _MainTexRandomUV ("Main Tex Random UV@随机UV", Float) = 0.0
        _BackfaceRevert ("Backface Revert@背面翻转UV的U方向", Float) = 0.0
        _AnimationSheets_Switch ("AnimationSheets@帧动画", Float) = 0.0
        _AnimationSheets_Rows ("Rows@行数", Float) = 4.0
        _AnimationSheets_Columns ("Columns@列数", Float) = 4.0
        _AnimationSheetsVectorMap ("Vector Map@运动向量图", 2D) = "gray" {}
        _AnimationSheetsVectorMap_Scale ("Vector Map Scale@运动向量图强度", Float) = 0.1
        _group_ramptex ("MainTex Ramp@主图渐变图", Float) = 0.0
        _RampTex_UseMainTexUV ("Use MainTex UV@跟随主贴图UV", Float) = 1.0
        _RampTex ("Ramp Texture@渐变图", 2D) = "white" {}
        _RampTexClampU ("Clamp U@限制U", Float) = 0.0
        _RampTexClampV ("Clamp V@限制V", Float) = 0.0
        _RampTexFlip ("Flip@翻转", Float) = 0.0
        _RampTexRotation ("Rotation@旋转90度", Float) = 0.0
        _RampTexChannelMapping ("Mask Channel Mapping@通道选择", Float) = 4.0
        _RampTexOnlyAffectAlpha ("Only Affect Alpha@仅影响Alpha(需选择单通道)", Float) = 0.0
        _UseMask ("Mask@遮罩图", Float) = 0.0
        _MaskTex ("Mask Texture@遮罩", 2D) = "white" {}
        _MaskChannelMapping ("Mask Channel Mapping@遮罩通道选择", Float) = 0.0
        _MaskDistortionChannelMapping ("Mask Distortion Channel Mapping@遮罩对扭曲的通道选择", Float) = 0.0
        _MaskTexUVMode ("UV Mode@UV模式", Float) = 0.0
        _MaskTexClampU ("Clamp U@限制U", Float) = 0.0
        _MaskTexClampV ("Clamp V@限制V", Float) = 0.0
        _MaskTexFlip ("Flip@翻转", Float) = 0.0
        _MaskTexRotation ("Rotation@旋转90度", Float) = 0.0
        _MaskTexUVSpeed ("Mask Tex UV Speed@流动速度", Vector) = (0.0, 0.0, 0.0, 0.0)
        _MaskAffectsDistortion ("Mask Affects Distortion@对扭曲作用强度", Float) = 0.0
        _group_2tone ("2Tone", Float) = 0.0
        _UVMove ("UV Move@开启Custom.1XY的UV移动", Float) = 0.0
        _ColorChannelMapping ("Color Channel Mapping@主帖图颜色通道选择", Float) = 0.0
        _AlphaChannelMapping ("Alpha Channel Mapping@主帖图透明通道选择", Float) = 0.0
        _UsingAlphaAsDissolve ("Using Alpha As Dissolve@开启溶解预乘透明通道", Float) = 1.0
        _MultiplyParticleColor ("Multiply Particle Color", Color) = (0.0, 0.0, 0.0, 1.0)
        _ZNetBakedVertexColor ("Multiply Particle Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LerpBrightness ("Lerp Brightness", Float) = 1.0
        _PowerRGB ("Power RGB", Float) = 1.0
        _PowerAlpha ("Power Alpha", Float) = 1.0
        _2ToneUsingVertexAlpha ("使用顶点 Alpha  ( 使用顶点 Alpha 和与 Custom2 的 Alpha 混合 )", Float) = 0.0
        _AffectedByMainLightColor ("Affected By Main Light Color", Float) = 0.0
        _AmbientColor ("Ambient Color", Color) = (0.0, 0.0, 0.0, 0.0)
        _CG_Blink ("Blink (CG Only)", Float) = 0.0
        _CG_BlinkFrequency ("   Blink Frequency (CG Only)", Float) = 0.0
        _UseDissolveTex ("Dissolve@溶解图", Float) = 0.0
        _DissolveTex ("Dissolve Tex@溶解", 2D) = "white" {}
        _DissolveChannel ("Dissolve Channel@溶解图通道选择", Float) = 0.0
        _DissolveIntensityOnMobile ("Mobile Dissolve Intensity@移动端溶解强度倍率", Float) = 1.0
        _DissolveTexUVMode ("UV Mode@UV模式", Float) = 0.0
        _DissolveTexClampU ("Clamp U@限制U", Float) = 0.0
        _DissolveTexClampV ("Clamp V@限制V", Float) = 0.0
        _DissolveTexFlip ("Flip@翻转", Float) = 0.0
        _DissolveTexRotation ("Rotation@旋转90度", Float) = 0.0
        _DissolveUVSpeed ("Dissolve UV Speed@流动速度", Vector) = (0.0, 0.0, 0.0, 0.0)
        _DissolveRandomUV ("Dissolve Random UV@随机UV", Float) = 0.0
        _DissolveAffects2Tone ("Dissolve Affects 2 Tone", Float) = 1.0
        _SoftEdge ("Soft Edge@软边", Float) = 0.0
        _SoftRange ("   Soft Range@软边范围", Float) = 0.0
        _SoftEdgeUsingOldFunction ("   使用旧版溶解方法", Float) = 0.0
        _group_dissolve_ramptex ("DissolveTex Ramp@溶解渐变图", Float) = 0.0
        _DissolveRampTex_UseDissolveTexUV ("Use MainTex UV@跟随渐变图UV", Float) = 1.0
        _DissolveRampTex_BlendOp ("Blend Option@叠加方式", Float) = 0.0
        _DissolveRampTex ("Ramp Texture@渐变图", 2D) = "white" {}
        _DissolveRampTexClampU ("Clamp U@限制U", Float) = 0.0
        _DissolveRampTexClampV ("Clamp V@限制V", Float) = 0.0
        _DissolveRampTexFlip ("Flip@翻转", Float) = 0.0
        _DissolveRampTexRotation ("Rotation@旋转90度", Float) = 0.0
        _DissolveRampTexChannelMapping ("Channel Mapping@通道选择", Float) = 0.0
        _UseDistortionTexture ("Distortion@扭曲图", Float) = 0.0
        _DistortionTex ("Distortion Tex@扭曲", 2D) = "bump" {}
        _DistortionChannel ("Distortion Channel@通道选择", Float) = 0.0
        _DistortionTexUVMode ("UV Mode@UV模式", Float) = 0.0
        _DistortionTexClampU ("Clamp U@限制U", Float) = 0.0
        _DistortionTexClampV ("Clamp V@限制V", Float) = 0.0
        _DistortionTexFlip ("Flip@翻转", Float) = 0.0
        _DistortionTexRotation ("Rotation@旋转90度", Float) = 0.0
        _DistortionUVSpeed ("Distortion UV Speed@流动速度", Vector) = (0.0, 0.0, 0.0, 0.0)
        _DistortionRandomUV ("Distortion Random UV@随机UV", Float) = 0.0
        _DistortionIntensity ("Distortion Intensity@扭曲强度", Float) = 0.0
        _DissolveDistortionIntensity ("Dissolve Distortion Intensity@扭曲对溶解的强度", Float) = 0.0
        _MaskDistortionIntensity ("Mask Distortion Intensity@扭曲对遮罩的强度", Float) = 0.0
        _UseDistortionTexture2 ("Distortion2@扭曲图2", Float) = 0.0
        _DistortionTex2 ("Distortion Tex@扭曲", 2D) = "bump" {}
        _Distortion2Channel ("Distortion Channel@通道选择", Float) = 0.0
        _DistortionTex2UVMode ("UV Mode@UV模式", Float) = 0.0
        _DistortionTex2ClampU ("Clamp U@限制U", Float) = 0.0
        _DistortionTex2ClampV ("Clamp V@限制V", Float) = 0.0
        _DistortionTex2Flip ("Flip@翻转", Float) = 0.0
        _DistortionTex2Rotation ("Rotation@旋转90度", Float) = 0.0
        _Distortion2UVSpeed ("Distortion UV Speed@流动速度", Vector) = (0.0, 0.0, 0.0, 0.0)
        _Distortion2RandomUV ("Distortion Random UV@随机UV", Float) = 0.0
        _Distortion2Intensity ("Distortion Intensity@扭曲强度", Float) = 0.0
        _DissolveDistortion2Intensity ("Dissolve Distortion Intensity@扭曲对溶解的强度", Float) = 0.0
        _MaskDistortion2Intensity ("Mask Distortion Intensity@扭曲对遮罩的强度", Float) = 0.0
        _InvFresnel ("Inv Fresnel@反向菲尼尔#用作模型边缘软化", Float) = 0.0
        _FresnelInvert ("Fresnel Invert@使用正向菲尼尔", Float) = 0.0
        _FresnelBlendMode ("Blend Mode@混合模式", Float) = 1.0
        _FresnelColor ("Fresnel Color@颜色", Color) = (1.0, 1.0, 1.0, 1.0)
        _FresnelBias ("Fresnel Bias@菲尼尔阈值", Float) = 0.0
        _FresnelScale ("Fresnel Scale@菲尼尔强度", Float) = 1.0
        _FresnelPower ("Fresnel Power@菲尼尔范围", Float) = 5.0
        _UsingNonPSR ("Non Particle System Parameters@非粒子系统模式#用于模型的时候勾上", Float) = 0.0
        _DissolveProgress ("Dissolve Progress@溶解值", Float) = 0.0
        _DistortionIntensity_NonPSR ("Distortion Intensity@总扭曲强度", Float) = 0.0
        _VertexExtrusionIntensity_NonPSR ("Vertex Extrusion Intensity@顶点偏移强度", Float) = 0.0
        _CustomDataColor ("Custom Data Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _AnimationSheets_UseNormalizedTime ("Use Normalized Time@使用0~1时间范围", Float) = 0.0
        _AnimationSheets_NormalizedTime ("Current Time@当前时间", Float) = 0.0
        _AnimationSheets_Frame ("Current Frame@当前帧", Float) = 0.0
        _AnimationSheets_Speed ("Animation Speed@动画速度", Float) = 0.0
        _ScreenEffects ("Screen Effects@屏幕特效", Float) = 0.0
        _SE_UVMode ("UV Mode@UV模式", Float) = 0.0
        _SE_SquarePixels ("Square Pixels", Float) = 1.0
        _SE_Boundary ("Boundary", Float) = 0.3
        _SE_BoundaryAspect ("Boundary Aspect", Float) = 1.0
        _SE_Feather ("Feather", Float) = 1.0
        _SE_MaxOpacity ("Max Opacity", Float) = 1.0
        _ScreenEffectUseUV ("UseUVForScreenEdge@屏幕特效使用UV代替顶点计算屏幕边界", Float) = 0.0
        _SE_Invert ("Invert@反转", Float) = 0.0
        _Distortion ("Distortion Post FX@后处理扭曲", Float) = 0.0
        _DistortionMode ("Distortion Mode@扭曲影响模式", Float) = 0.0
        _DTTex ("Distortion Post Tex@后处理扭曲图", 2D) = "linearGray" {}
        _DTTexUVMode ("UV Mode@UV模式", Float) = 0.0
        _DTTexClampU ("Clamp U@限制U", Float) = 0.0
        _DTTexClampV ("Clamp V@限制V", Float) = 0.0
        _DTTexFlip ("Flip@翻转", Float) = 0.0
        _DTTexRotation ("Rotation@旋转90度", Float) = 0.0
        _DTIntensity ("Distortion Intensity@扭曲强度", Float) = 0.0
        _Dist_Intensity_PostProcessing ("Distortion Intensity Post Processing", Float) = 1.0
        _DtUvMove ("Distortion UV Move@开启UV移动", Float) = 0.0
        _DtUSpeed ("Distortion U Speed@U速度", Float) = 1.0
        _DtVSpeed ("Distortion V Speed@V速度", Float) = 1.0
        _SeparateRGBIntensity ("Separate RGB Intensity@色相分离强度", Float) = 0.0
        _VertexExtrusion ("Vertex Extrusion@顶点变形#基于法线方向的顶点变形", Float) = 0.0
        _VertexExtrusionUseTriPlanarUV ("UV Mode", Float) = 0.0
        _VertexExtrusionTex ("Vertex Extrusion Tex@顶点变形图", 2D) = "black" {}
        _VertexExtrusionIntensityChannel_X ("X Axis Channel Mask@X轴 通道选择", Float) = 0.0
        _VertexExtrusionIntensityChannel_Y ("Y Axis Channel Mask@Y轴 通道选择", Float) = 1.0
        _VertexExtrusionIntensityChannel_Z ("Z Axis Channel Mask@Z轴 通道选择", Float) = 2.0
        _VertexExtrusionWorldAxisMask ("World Axis Power@世界坐标轴强度", Vector) = (0.0, 0.0, 0.0, 0.0)
        _VertexExtrusionWorldAxis_X_ST ("X Axis Tilling Speed@X轴 重复度 速度", Vector) = (1.0, 1.0, 0.0, 0.0)
        _VertexExtrusionWorldAxis_Y_ST ("Y Axis Tilling Speed@Y轴 重复度 速度", Vector) = (1.0, 1.0, 0.0, 0.0)
        _VertexExtrusionWorldAxis_Z_ST ("Z Axis Tilling Speed@Z轴 重复度 速度", Vector) = (1.0, 1.0, 0.0, 0.0)
        _VertexExtrusionIntensityChannel ("Vertex Extrusion Intensity Channel@通道1选择", Float) = 0.0
        _VertexExtrusionIntensityChannel2 ("Vertex Extrusion Intensity Channel 2@通道2选择", Float) = 1.0
        _VertexExtrusionIntensity ("Vertex Extrusion Intensity@通道1强度", Float) = 1.0
        _VertexExtrusionIntensity2 ("Vertex Extrusion Intensity 2@通道2强度", Float) = -1.0
        _VertexExtrusionUVSpeed ("UV Speed@流动速度", Vector) = (0.0, 0.0, 0.0, 1.0)
        _VertexExtrusionTexRandomUV ("Random UV@随机UV", Float) = 0.0
        _CG_VEMask ("Vertex Extrusion Mask@遮罩功能", Float) = 0.0
        _CG_VEMaskTex ("Vertex Extrusion Mask Tex@遮罩图", 2D) = "white" {}
        _CG_VEMaskChannel ("Vertex Extrusion Mask Channel@遮罩图通道选择", Float) = 0.0
        _CollideWithAvatar ("Collide with Avatar@跟随角色碰撞#中心点在角色的脚底", Float) = 0.0
        _CollisionRadius ("Collision Radius@碰撞的圆形半径大小", Vector) = (1.0, 1.5, 0.0, 0.0)
        _AvatarYOffset ("Avatar Y Offset@角色中心点Y轴偏移量", Float) = 1.0
        _UseSphereFade ("Sphere Fade@球形遮罩", Float) = 0.0
        _AlphaSphereFadeUseLocal ("Use Local Position@使用本地(粒子)坐标", Float) = 0.0
        _AlphaSphereFadeCenter ("Center@中心点", Vector) = (0.0, 0.0, 0.0, 0.0)
        _AlphaSphereFadeOut ("Fade Out@外半径虚化强度", Float) = 0.0
        _AlphaSphereFadeIn ("Fade In@内半径虚化强度", Float) = 0.0
        _HollowMask ("Hollow Mask@中间镂空遮罩", Float) = 0.0
        _SphereFadeUseScreenUV ("Use Screen UV@使用屏幕UV", Float) = 0.0
        _SphereFadeKeepAspect ("Keep Aspect@保持宽高比", Float) = 0.0
        _FadeFromCameraOn ("FadeFromCamera@按相机距离渐隐", Float) = 0.0
        _FadeFromCamera_Invert ("Invert Alpha@反转Fade透明", Float) = 0.0
        _FadeFromCameraType ("   Fade Type@模式", Float) = 0.0
        _FadeFromCamera ("   Fade消失距离(离相机)", Float) = 1.0
        _RcpFadeLength ("   Fade过渡距离软硬参数", Float) = 1.0
        _group_nearlyDepthClip ("Near Depth Clip@近点深度剔除", Float) = 0.0
        _NearDepthClip_Distance ("Nearly Distance@剔除开始距离", Float) = 0.0
        _group_dither ("Dither Clip@点状透明", Float) = 0.0
        _DitherAlpha ("Dither Alpha@点状透明", Float) = 1.0
        _DitherAlpha2 ("Dither Alpha 2@点状透明2", Float) = 1.0
        _MPIgnore ("Ignore Mobile Platform@忽略移动端", Float) = 0.0
        _group_colorAdjustment ("Color Adjustment@颜色矫正", Float) = 0.0
        _DisableColorSaturation ("不受去色影响", Float) = 0.0
        _Saturation ("Saturation@去色", Float) = 1.0
        _CloserHighlight ("Closer Highlight@接近高亮", Float) = 0.0
        _CloserHighlightParams ("Closer Highlight Params@近处高亮参数", Vector) = (5.0, 2.0, 2.0, 0.0)
        _UseClipPlane ("Clip Planet@平面裁剪", Float) = 0.0
        _ClipPlane ("On@开启", Float) = 0.0
        _PlaneClipReverse ("Reverse (Enable when 2 Planes Upward Direction are Opposite)@翻转(两个裁切方向相对时开启)", Float) = 0.0
        _ClipPlaneXZ ("Clip Plane XZ@裁切X & Z平面", Float) = 0.0
        _ReversePlaneXZ ("Reverse Plane XZ@翻转X & Z平面", Float) = 0.0
        _PlaneXZScale ("Plane XZ Scale", Vector) = (1.0, 1.0, 1.0, 1.0)
        _ClipPlane_Soft ("Soft Edge@软边", Float) = 0.0
        _ClipPlane_SoftDistance ("Soft Edge Distance@软边距离", Float) = 1.0
        _VertexAnimation ("Vertex Animation@VAT动画", Float) = 0.0
        _Use_Frame ("Use Frame@使用面板动画帧", Float) = 0.0
        _Current_Frame ("Current Frame@当前帧", Float) = 0.0
        _Use_CustomData_Frame ("Use CustomData Frame@粒子系统使用customData1.w动画帧", Float) = 0.0
        _SkipInterpolation ("Disable Interpolation@关闭帧混合", Float) = 0.0
        _VertexPositionTex ("Vertex Position", 2D) = "black" {}
        _VertexNormalTex ("Vertex Normal", 2D) = "bump" {}
        _VertexColorTex ("Vertex Color", 2D) = "white" {}
        _Frames ("Frames", Float) = 24.0
        _AgeClip ("Age Clip", Float) = 1.0
        _group_CharacterVfxMask ("Character Vfx Mask@角色遮罩", Float) = 0.0
        _CharacterVfxMask_Invert ("Invert@反向", Float) = 0.0
        _group_maintex_blur ("MainTex Blur@主贴图模糊", Float) = 0.0
        _MainTex_Blur_Amount ("Blur Amount@模糊强度", Float) = 5.0
        _MainTex_Blur_Step ("Blur Step@模糊步数", Float) = 3.0
        _group_globalParams ("Global Parameters@全局参数", Float) = 0.0
        _Arc_Mask ("妮妮薇大招遮罩", Float) = 0.0
        _AffectByGlobalEtherColor ("受全局异化物特效颜色控制", Float) = 0.0
        _AffectByGlobalUseMultiply ("使用乘法计算", Float) = 0.0
        _AffectByGlobalWaterColor ("受全局水特效颜色控制", Float) = 0.0
        _AffectByEffectFogColor ("受全局场景烟雾特效颜色控制", Float) = 0.0
        _AffectByCamera3DUIAlpha ("受相机额外透明度权重控制", Float) = 0.0
        _AffectByEffectSandstormColor ("受全局场景风沙特效颜色控制", Float) = 0.0
        _WorldSoftParticle ("WorldSoftParticle@世界空间Y轴渐变", Float) = 0.0
        _WorldSoftStartY ("WorldSoftStartY@Y轴位置", Float) = 0.0
        _WorldSoftLength ("WorldSoftLength@渐变长度", Float) = 1.0
        _Vfx_Volumetric_Shadow ("受阴影影响", Float) = 0.0
        _VFXVolumetricShadowGap ("受阴影影响粒度", Float) = 0.5
        _VfxShadow_EffectType ("特效阴影类型", Float) = 0.0
        _IgnoreTimeScale ("Ignore Time Scale@忽略时间缩放影响", Float) = 0.0
        _SoftParticles ("Soft Particles@软粒子", Float) = 0.0
        _SoftParticlesNearFadeDistance ("Soft Particles Near Fade", Float) = 0.0
        _SoftParticlesFarFadeDistance ("Soft Particles Far Fade", Float) = 1.0
        _SoftParticlesRcpDistance ("Soft Particles Rcp Distance", Float) = 1.0
        _OpaquenessFadeByScript ("Opaqueness Fade By Script", Float) = 1.0
        _group_alphaCut ("Alpha Cutoff@透明度剔除", Float) = 0.0
        _AlphaCutoff ("Alpha Cutoff@透明度剔除阈值", Float) = 0.0
        _TextureLodBiasOn ("TextureBias@贴图LOD", Float) = 0.0
        _TextureLodBias ("Texture Lod Bias", Float) = 0.0
        _MoonRockMode ("MoonRockMode@月亮遮挡模式", Float) = 0.0
        _CenterPos ("Center", Vector) = (1.0, 1.0, 1.0, 1.0)
        _MoonMaskAllOn ("MoonOcclusion All", Float) = 0.0
        _MoonUVMode ("MoonUVMode@月亮UV模式", Float) = 0.0
        _MoonUVScale ("MoonUVScale@月亮UVScale", Float) = 1.0
        _group_timescale ("Time Scale@时间缩放", Float) = 0.0
        _TimeScaleSpeed ("Time Scale Speed@时间缩放对速度的强度", Float) = 0.0
        _TimeOffset ("Time Offset", Float) = 0.0
        _group_renderstate ("Render State@渲染设置", Float) = 0.0
        _HighShadingRate ("High Shading Rate@全分辨率渲染", Float) = 0.0
        _ZOffset ("Z Offset", Float) = 0.0
        _ZWrite ("ZWrite", Float) = 0.0
        _ClampZToFar ("ClampZToFar@限定深度值不超出相机", Float) = 0.0
        _ZTest ("Render On Top@置于顶层渲染", Float) = 4.0
        _OffsetFactor ("Offset Factor", Float) = 0.0
        _OffsetUnits ("Offset Units", Float) = 0.0
        _ApplySceneFog ("Apply Scene Fog@受场景雾颜色影响", Float) = 0.0
        _FogIntensity ("Fog Intensity@雾效强度", Float) = 1.0
        _BlendWithLightAttenuation ("Blend with light attenuation【CG】", Float) = 0.0
        _BlendWithLightAttenuationValue ("   Blend with light attenuation Value【CG】", Float) = 1.0
        _StencilMode ("Stencil Mode", Float) = 0.0
        _StencilRef ("Stencil Ref", Float) = 0.0
        _StencilReadMask ("Stencil ReadMask", Float) = 0.0
        _StencilComp ("Stencil Comp", Float) = 0.0
        _StencilOp ("Stencil Op", Float) = 0.0
        _MirrorPos ("镜像", Float) = 0.0
        _IsMirrorUp ("IsMirroUp@平面上部分", Float) = 1.0
        _SceneClipOffset ("平面裁剪偏移", Float) = 0.0
        _UiPreTransformFix ("Fix URP Shader UI PreTransform", Float) = 0.0
        _group_debug ("Debug", Float) = 0.0
        _DebugOctagonShape ("Debug Octagon Shape", Float) = 0.0
        _OctagonClipErrorColor ("   Octagon Clip Error Color", Color) = (1.0, 0.0, 1.0, 1.0)
        _DebugOctagonDistortion ("Debug Octagon Distortion", Float) = 0.0
    }
    SubShader
    {
        Pass
        {
            Name "TransparentFullRes"
            Blend One Zero
            BlendOp Add
            ZTest LEqual
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma target 4.0
            #pragma vertex CorinVertex
            #pragma fragment CorinPixel
            #if defined(SHADER_STAGE_VERTEX)
cbuffer cb0 : register(b0) { float4 cb0[180]; }
cbuffer cb1 : register(b1) { float4 cb1[4]; }
cbuffer cb2 : register(b2) { float4 cb2[65]; }
static uint4 x0[2];
static uint4 x1[6];
static uint4 x2[6];
struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float4 output1 : TEXCOORD0;
    float4 output2 : TEXCOORD1;
    float4 output3 : TEXCOORD2;
    float4 output4 : TEXCOORD3;
    float4 output5 : TEXCOORD4;
    float4 output6 : TEXCOORD5;
    float4 output7 : TEXCOORD6;
    float4 output8 : TEXCOORD7;
    float3 output9 : TEXCOORD8;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float3 input1 : NORMAL0, float4 input2 : COLOR0, float4 input3 : TEXCOORD0, float4 input4 : TEXCOORD1, float4 input5 : TEXCOORD2, float4 input6 : TEXCOORD3, float4 input7 : TEXCOORD4, float4 input8 : TEXCOORD6)
{
    uint4 r0, r1, r2, r3, r4, r5, r6, r7, r8, r9, r10, o0, o1, o2, o3, o4, o5, o6, o7, o8, o9, v0, v1, v2, v3, v4, v5, v6, v7, v8;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyz = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
    v5.xyzw = asuint(input5);
    v6.xyzw = asuint(input6);
    v7.xyzw = asuint(input7);
    v8.xyzw = asuint(input8);
    r0.x = (uint)(asfloat(asuint(cb2[43].z)));
    r0.y = ((asfloat(0x3f000000u) < asfloat(asuint(cb2[43].y))) ? 0xffffffffu : 0u);
    r0.x = ((r0.y != 0u) ? asuint(cb0[14].x) : asuint(cb0[r0.x+17].y));
    r0.x = asuint((asfloat(r0.x) + asfloat((asuint(cb2[61].z) ^ 0x80000000u))));
    r0.y = ((asfloat(0x3f000000u) < asfloat(asuint(cb2[28].y))) ? 0xffffffffu : 0u);
    r1.xyz = asuint((asfloat(asuint(cb2[14].xyz)) * asfloat(uint3(0x3d9e8391u, 0x3d9e8391u, 0x3d9e8391u))));
    r2.xyz = asuint((asfloat(asuint(cb2[14].xyz)) + asfloat(uint3(0x3d6147aeu, 0x3d6147aeu, 0x3d6147aeu))));
    r2.xyz = asuint((asfloat(r2.xyz) * asfloat(uint3(0x3f72a76fu, 0x3f72a76fu, 0x3f72a76fu))));
    r2.xyz = asuint(log2(asfloat((r2.xyz & 0x7fffffffu))));
    r2.xyz = asuint((asfloat(r2.xyz) * asfloat(uint3(0x4019999au, 0x4019999au, 0x4019999au))));
    r2.xyz = asuint(exp2(asfloat(r2.xyz)));
    r3.xyz = ((asfloat(uint3(0x3d25aee6u, 0x3d25aee6u, 0x3d25aee6u)) >= asfloat(asuint(cb2[14].xyz))) ? 0xffffffffu : 0u);
    r1.xyz = ((r3.xyz != 0u) ? r1.xyz : r2.xyz);
    r1.xyz = asuint((asfloat(r1.xyz) * asfloat(v2.xyz)));
    r1.w = asuint((asfloat(v2.w) * asfloat(asuint(cb2[14].w))));
    r1.xyzw = ((r0.yyyy != 0u) ? r1.xyzw : v2.xyzw);
    r2.xy = v4.xy;
    r2.zw = v5.xy;
    r3.xy = v6.zw;
    r3.zw = v8.zw;
    r2.xyzw = ((r0.yyyy != 0u) ? r2.xyzw : r3.xyzw);
    r0.z = asuint((asfloat((r1.w ^ 0x80000000u)) + asfloat(0x3f800000u)));
    x0[1].x = r0.z;
    x1[1].x = 0x3f800000u;
    r0.zw = (uint2)(asfloat(asuint(cb2[64].xy)));
    x0[r0.z + 0].x = v5.x;
    x1[r0.w + 0].x = v5.y;
    r0.z = x0[1].x;
    o4.x = ((r0.y != 0u) ? asuint(cb2[28].z) : r0.z);
    r0.z = x1[1].x;
    o4.z = ((r0.y != 0u) ? asuint(cb2[64].z) : r0.z);
    r0.z = ((asfloat(0x00000000u) != asfloat(asuint(cb2[12].x))) ? 0xffffffffu : 0u);
    r1.xyzw = ((r0.zzzz != 0u) ? uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u) : r1.xyzw);
    r0.z = ((asfloat(0x3f000000u) < asfloat(asuint(cb2[54].w))) ? 0xffffffffu : 0u);
    if (r0.z != 0u) {
        r3.xz = asuint(cb0[22].xz);
        r3.y = asuint((asfloat(asuint(cb0[22].y)) + asfloat(asuint(cb2[54].z))));
        r3.xyz = asuint((asfloat(r3.xyz) + asfloat((v7.xyz ^ 0x80000000u))));
        r0.z = asuint(dot(asfloat(r3.xyz), asfloat(r3.xyz)));
        r0.z = asuint(sqrt(asfloat(r0.z)));
        r0.w = asuint(dot(asfloat((r3.xyz ^ 0x80000000u)), asfloat((r3.xyz ^ 0x80000000u))));
        r0.w = asuint(rsqrt(asfloat(r0.w)));
        r3.xyz = asuint((asfloat(r0.www) * asfloat((r3.xyz ^ 0x80000000u))));
        r0.z = asuint((asfloat(r0.z) + asfloat((asuint(cb2[54].x) ^ 0x80000000u))));
        r0.w = asuint((asfloat((asuint(cb2[54].x) ^ 0x80000000u)) + asfloat(asuint(cb2[54].y))));
        r0.z = asuint(saturate((asfloat(r0.z) / asfloat(r0.w))));
        r0.z = asuint((asfloat((r0.z ^ 0x80000000u)) + asfloat(0x3f800000u)));
        r3.xyz = asuint((asfloat(r0.zzz) * asfloat(r3.xyz)));
        r3.xyz = asuint(mad(asfloat(r3.xyz), asfloat(asuint(cb2[54].xxx)), asfloat(v0.xyz)));
    } else {
        r3.xyz = v0.xyz;
    }
    r4.xyz = asuint((asfloat(r3.yyy) * asfloat(asuint(cb1[1].xyz))));
    r3.xyw = asuint(mad(asfloat(asuint(cb1[0].xyz)), asfloat(r3.xxx), asfloat(r4.xyz)));
    r3.xyz = asuint(mad(asfloat(asuint(cb1[2].xyz)), asfloat(r3.zzz), asfloat(r3.xyw)));
    r3.xyz = asuint((asfloat(r3.xyz) + asfloat(asuint(cb1[3].xyz))));
    r4.xyz = asuint((asfloat((r3.xyz ^ 0x80000000u)) + asfloat(asuint(cb0[30].xyz))));
    r0.z = asuint(dot(asfloat(r4.xyz), asfloat(r4.xyz)));
    o7.w = asuint(sqrt(asfloat(r0.z)));
    r4.xyz = asuint((asfloat(r3.yyy) * asfloat(asuint(cb0[95].xyz))));
    r4.xyz = asuint(mad(asfloat(asuint(cb0[94].xyz)), asfloat(r3.xxx), asfloat(r4.xyz)));
    r4.xyz = asuint(mad(asfloat(asuint(cb0[96].xyz)), asfloat(r3.zzz), asfloat(r4.xyz)));
    r4.xyz = asuint((asfloat(r4.xyz) + asfloat(asuint(cb0[97].xyz))));
    r0.z = asuint(dot(asfloat(r4.xyz), asfloat(r4.xyz)));
    r0.z = asuint(rsqrt(asfloat(r0.z)));
    r5.xyz = asuint((asfloat(r0.zzz) * asfloat(r4.xyz)));
    r4.xyz = asuint(mad(asfloat(r5.xyz), asfloat(asuint(cb2[11].www)), asfloat(r4.xyz)));
    r5.x = asuint(cb0[90].x);
    r5.y = asuint(cb0[92].x);
    r5.z = asuint(cb0[93].x);
    r4.w = 0x3f800000u;
    r5.x = asuint(dot(asfloat(r5.xyz), asfloat(r4.xzw)));
    r6.x = asuint(cb0[91].y);
    r6.y = asuint(cb0[92].y);
    r6.zw = asuint(cb0[93].yz);
    r5.y = asuint(dot(asfloat(r6.xyz), asfloat(r4.yzw)));
    r6.x = asuint(cb0[90].z);
    r6.y = asuint(cb0[91].z);
    r6.z = asuint(cb0[92].z);
    r0.z = asuint(dot(asfloat(r6.xyzw), asfloat(r4.xyzw)));
    r4.x = asuint(cb0[92].w);
    r4.y = asuint(cb0[93].w);
    r0.w = asuint(dot(asfloat(r4.xy), asfloat(r4.zw)));
    r3.w = ((asfloat(0x00000000u) != asfloat(asuint(cb2[58].z))) ? 0xffffffffu : 0u);
    r4.xy = v5.zw;
    r5.zw = v6.xy;
    r6.x = ((r3.w != 0u) ? asuint(cb0[35].w) : r4.x);
    r6.y = ((r3.w != 0u) ? asuint(cb0[36].w) : r4.y);
    r6.z = ((r3.w != 0u) ? asuint(cb0[37].w) : r5.z);
    r6.w = ((r3.w != 0u) ? asuint(cb0[38].w) : r5.w);
    r6.xyzw = ((r0.yyyy != 0u) ? asuint(cb2[27].xyzw) : r6.xyzw);
    r1.xyzw = asuint((asfloat(r1.xyzw) * asfloat(asuint(cb2[13].xyzw))));
    r4.xy = ((asfloat(uint2(0x3f000000u, 0x3f000000u)) < asfloat(asuint(cb2[58].wy))) ? 0xffffffffu : 0u);
    r7.x = asuint(cb0[39].y);
    r7.y = asuint(cb0[40].y);
    r7.z = asuint(cb0[41].y);
    r7.w = asuint(cb0[42].y);
    r8.xyzw = asuint((asfloat(r6.xyzw) * asfloat(r7.xyzw)));
    r9.xyzw = asuint((asfloat(r1.xyzw) * asfloat(r7.xyzw)));
    r8.xyzw = ((r4.yyyy != 0u) ? r8.xyzw : r7.xyzw);
    r7.xyzw = ((r4.yyyy != 0u) ? r9.xyzw : r7.xyzw);
    o5.xyzw = ((r4.xxxx != 0u) ? r8.xyzw : r6.xyzw);
    o6.xyzw = ((r4.xxxx != 0u) ? r7.xyzw : r1.xyzw);
    r0.y = asuint(max(asfloat(asuint(cb2[16].x)), asfloat(0x00000000u)));
    r0.y = asuint(min(asfloat(r0.y), asfloat(0x40a00000u)));
    x2[0].xy = v3.xy;
    x2[1].xy = r2.xy;
    x2[2].xy = v3.xy;
    x2[3].xy = r2.xy;
    x2[4].xy = v3.xy;
    x2[5].xy = r2.zw;
    r0.y = asuint(floor(asfloat(r0.y)));
    r0.y = (uint)(asfloat(r0.y));
    r1.xy = x2[r0.y + 0].xy;
    r0.y = ((asfloat(asuint(cb2[28].y)) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r2.xy = (r0.yy & v3.zw);
    r4.xy = (r0.yy & v4.zw);
    r1.zw = asuint((asfloat((r1.xy ^ 0x80000000u)) + asfloat(uint2(0x3f800000u, 0x3f800000u))));
    r6.xyzw = asuint((asfloat((r1.xyzw ^ 0x80000000u)) + asfloat(r1.yzwx)));
    r1.xyzw = asuint(mad(asfloat(asuint(cb2[17].xxxx)), asfloat(r6.xyzw), asfloat(r1.xyzw)));
    r6.xyzw = ((asfloat(asuint(cb2[16].wwww)) < asfloat(uint4(0x3f000000u, 0x3fc00000u, 0x40200000u, 0x477fe000u))) ? 0xffffffffu : 0u);
    r7.xyzw = (r6.xyzw & uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
    r6.xyz = ((r6.xyz != 0u) ? uint3(0xbf800000u, 0xbf800000u, 0xbf800000u) : uint3(0x80000000u, 0x80000000u, 0x80000000u));
    r6.xyz = asuint((asfloat(r6.xyz) + asfloat(r7.yzw)));
    r6.xyz = asuint(max(asfloat(r6.xyz), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r8.xy = r1.zw;
    r8.z = 0x3f800000u;
    r8.w = asuint((asfloat((asuint(cb2[17].x) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r9.xy = r1.xw;
    r9.z = 0x00000000u;
    r9.w = asuint((asfloat((asuint(cb2[17].x) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r10.xyzw = asuint((asfloat(r6.yyyy) * asfloat(r9.xyzw)));
    r10.xyzw = asuint(mad(asfloat(r8.xyzw), asfloat(r6.zzzz), asfloat(r10.xyzw)));
    r1.xz = r8.xz;
    r1.w = asuint(cb2[17].x);
    r6.xyzw = asuint(mad(asfloat(r1.xyzw), asfloat(r6.xxxx), asfloat(r10.xyzw)));
    r1.xz = r9.xz;
    r1.xyzw = asuint(mad(asfloat(r1.xyzw), asfloat(r7.xxxx), asfloat(r6.xyzw)));
    r1.xy = asuint((asfloat((r1.zw ^ 0x80000000u)) + asfloat(r1.xy)));
    r1.xy = asuint(mad(asfloat(r1.xy), asfloat(asuint(cb2[1].xy)), asfloat(asuint(cb2[1].zw))));
    r1.xy = asuint((asfloat(r1.zw) + asfloat(r1.xy)));
    r0.y = ((asfloat(0x3f000000u) < asfloat(asuint(cb2[25].z))) ? 0xffffffffu : 0u);
    r1.zw = asuint((asfloat(r0.xx) * asfloat(asuint(cb2[9].xy))));
    r1.zw = asuint(frac(asfloat(r1.zw)));
    r1.zw = asuint((asfloat(r1.zw) + asfloat(r4.xy)));
    r4.xy = ((asfloat(asuint(cb2[18].xy)) != asfloat(uint2(0x00000000u, 0x00000000u))) ? 0xffffffffu : 0u);
    r5.zw = asuint((asfloat(r1.zw) / asfloat(asuint(cb2[18].xy))));
    r5.zw = asuint(floor(asfloat(r5.zw)));
    r5.zw = asuint((asfloat(r5.zw) * asfloat(asuint(cb2[18].xy))));
    r1.zw = ((r4.xy != 0u) ? r5.zw : r1.zw);
    r1.zw = asuint((asfloat(r1.zw) + asfloat(r1.xy)));
    r0.xy = ((r0.yy != 0u) ? r1.zw : r1.xy);
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint(cb2[17].y))) ? 0xffffffffu : 0u);
    r1.yz = asuint(mad(asfloat(r2.xy), asfloat(uint2(0x40490fdbu, 0x3fb8aa3bu)), asfloat(r0.xy)));
    o2.xy = ((r1.xx != 0u) ? r1.yz : r0.xy);
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint(cb2[0].x))) ? 0xffffffffu : 0u);
    r0.y = asuint(max(asfloat(r0.z), asfloat(0x00000000u)));
    o0.z = ((r0.x != 0u) ? r0.y : r0.z);
    o0.xy = asuint(mad(asfloat(asuint(cb0[179].xy)), asfloat(r0.ww), asfloat(r5.xy)));
    o0.w = r0.w;
    o1.xyzw = v3.xyzw;
    o2.zw = uint2(0x00000000u, 0x00000000u);
    o3.xyzw = uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u);
    o4.y = 0x00000000u;
    o4.w = r4.z;
    o7.xyz = r3.xyz;
    o8.xy = v5.xy;
    o8.zw = uint2(0x00000000u, 0x00000000u);
    o9.xy = r2.zw;
    o9.z = v7.w;
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    result.output2 = asfloat(o2.xyzw);
    result.output3 = asfloat(o3.xyzw);
    result.output4 = asfloat(o4.xyzw);
    result.output5 = asfloat(o5.xyzw);
    result.output6 = asfloat(o6.xyzw);
    result.output7 = asfloat(o7.xyzw);
    result.output8 = asfloat(o8.xyzw);
    result.output9 = asfloat(o9.xyz);
    return result;
}

            #elif defined(SHADER_STAGE_FRAGMENT)
cbuffer ZZZGlobals : register(b0)
{
    column_major float4x4 _SceneWeatherParamsPart1 : packoffset(c31);
    column_major float4x4 _SceneWeatherParamsPart6 : packoffset(c51);
    column_major float4x4 _SceneWeatherParamsPart7 : packoffset(c55);
    float4 _MainLightColor : packoffset(c7);
    float4 _SceneParticleFogColorMultiply : packoffset(c10);
    float _GlobalMipBias : packoffset(c61);
    float4 _NapEffectBrightnessParams4 : packoffset(c169);
    float4 _NapEffectBrightnessExtraParams : packoffset(c170);
    float4 _SceneWeatherSandstorm : packoffset(c177);
    float _UIFxExtraAlphaWeight : packoffset(c182);
}

cbuffer UnityPerMaterial : register(b1)
{
    float _LerpBrightness : packoffset(c10.z);
    float4 _AmbientColor : packoffset(c15);
    float _MainTexClampU : packoffset(c16.y);
    float _MainTexClampV : packoffset(c16.z);
    float _PowerRGB : packoffset(c24.z);
    float _PowerAlpha : packoffset(c24.w);
    float _ColorChannelMapping : packoffset(c25);
    float _AlphaChannelMapping : packoffset(c25.y);
    float _OpaquenessFadeByScript : packoffset(c25.w);
    float _2ToneUsingVertexAlpha : packoffset(c26.y);
    float _AffectedByMainLightColor : packoffset(c26.w);
    float _AlphaFade : packoffset(c28.w);
    float _AlphaFade_Timeline : packoffset(c29);
    float _SceneClipOffset : packoffset(c29.z);
    float _BlendMode : packoffset(c43);
    float _Saturation : packoffset(c57.z);
    float _AffectByGlobalUseMultiply : packoffset(c58.y);
    float _AffectByEffectFogColor : packoffset(c59);
    float _AffectByCamera3DUIAlpha : packoffset(c59.y);
    float _AffectByEffectSandstormColor : packoffset(c59.z);
    float _CustomData1W : packoffset(c64.y);
    float _BackfaceRevert : packoffset(c65.y);
}
static const uint4 icb[4] = { uint4(0x3f800000u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x3f800000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x3f800000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u) };


SamplerState sampler_MainTex;
Texture2D<float4> _MainTex : register(t0);
static uint4 x0[5];
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float4 input1 : TEXCOORD0, float4 input2 : TEXCOORD1, float4 input3 : TEXCOORD2, float4 input4 : TEXCOORD3, float4 input5 : TEXCOORD4, float4 input6 : TEXCOORD5, float4 input7 : TEXCOORD6, float4 input8 : TEXCOORD7, float3 input9 : TEXCOORD8, bool input10 : SV_IsFrontFace0)
{
    uint4 r0, r1, r2, r3, r4, o0, v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
    v5.xyzw = asuint(input5);
    v6.xyzw = asuint(input6);
    v7.xyzw = asuint(input7);
    v8.xyzw = asuint(input8);
    v9.xyz = asuint(input9);
    v10.x = (input10 ? 0xffffffffu : 0u);
    x0[4].x = asuint((_Saturation));
    r0.x = (uint)(asfloat(asuint((_CustomData1W))));
    x0[r0.x + 0].x = v8.y;
    r0.xy = ((asfloat(asuint((float2(_MainTexClampU, _MainTexClampV)))) >= asfloat(uint2(0x3f000000u, 0x3f000000u))) ? 0xffffffffu : 0u);
    r0.xy = (r0.xy & uint2(0x3f800000u, 0x3f800000u));
    r0.z = ((asfloat(0x3f000000u) < asfloat(asuint((_BackfaceRevert)))) ? 0xffffffffu : 0u);
    r0.w = ((v10.x != 0u) ? 0x3f800000u : 0xbf800000u);
    r0.z = ((r0.z != 0u) ? r0.w : 0x3f800000u);
    r1.x = asuint((asfloat(r0.z) * asfloat(v2.x)));
    r1.y = v2.y;
    r0.zw = asuint(saturate(asfloat(r1.xy)));
    r0.zw = asuint((asfloat((r1.xy ^ 0x80000000u)) + asfloat(r0.zw)));
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(r0.zw), asfloat(r1.xy)));
    r0.xyzw = asuint(_MainTex.SampleBias(sampler_MainTex, asfloat(r0.xy), asfloat(asuint((_GlobalMipBias)))).xyzw);
    r1.xy = (uint2)(asfloat(asuint((float2(_ColorChannelMapping, _AlphaChannelMapping)))));
    r1.xy = min(r1.xy, uint2(0x00000003u, 0x00000003u));
    r1.y = asuint(dot(asfloat(r0.xyzw), asfloat(icb[r1.y+0].xyzw)));
    r0.w = asuint(dot(asfloat(r0.xyzw), asfloat(icb[r1.x+0].xyzw)));
    r2.xyzw = asuint((asfloat((v5.xyzw ^ 0x80000000u)) + asfloat(v6.xyzw)));
    r1.x = asuint(mad(asfloat(r1.y), asfloat(r2.w), asfloat(v5.w)));
    r1.z = ((asfloat(asuint((_2ToneUsingVertexAlpha))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r1.x = ((r1.z != 0u) ? v6.w : r1.x);
    r1.x = asuint((asfloat(r1.x) * asfloat(r1.y)));
    r1.y = asuint((asfloat(asuint((_SceneWeatherParamsPart6[0][3]))) + asfloat(asuint((_SceneClipOffset)))));
    r1.y = ((asfloat(v7.y) < asfloat(r1.y)) ? 0xffffffffu : 0u);
    r1.w = ((r1.y != 0u) ? 0x00000000u : r1.x);
    r2.w = ((asfloat(0x40600000u) < asfloat(asuint((_ColorChannelMapping)))) ? 0xffffffffu : 0u);
    r0.w = ((r2.w != 0u) ? 0x3f800000u : r0.w);
    r0.xyz = ((r2.www != 0u) ? r0.xyz : uint3(0x3f800000u, 0x3f800000u, 0x3f800000u));
    r0.w = asuint(saturate((asfloat(r0.w) * asfloat(asuint((_LerpBrightness))))));
    r2.xyz = asuint(mad(asfloat(r0.www), asfloat(r2.xyz), asfloat(v5.xyz)));
    r0.xyz = asuint((asfloat(r0.xyz) * asfloat(r2.xyz)));
    r2.xyz = asuint((asfloat(asuint((float3(_MainLightColor.x, _MainLightColor.y, _MainLightColor.z)))) + asfloat(uint3(0xbf800000u, 0xbf800000u, 0xbf800000u))));
    r2.xyz = asuint(mad(asfloat(asuint((float3(_AffectedByMainLightColor, _AffectedByMainLightColor, _AffectedByMainLightColor)))), asfloat(r2.xyz), asfloat(asuint((float3(_AmbientColor.x, _AmbientColor.y, _AmbientColor.z))))));
    r2.xyz = asuint((asfloat(r2.xyz) + asfloat(uint3(0x3f800000u, 0x3f800000u, 0x3f800000u))));
    r0.xyz = asuint((asfloat(r0.xyz) * asfloat(r2.xyz)));
    r2.xyz = asuint((asfloat(r1.www) * asfloat(r0.xyz)));
    r3.xyz = asuint(mad(asfloat(r2.xyz), asfloat(asuint((float3(_NapEffectBrightnessParams4.x, _NapEffectBrightnessParams4.x, _NapEffectBrightnessParams4.x)))), asfloat(asuint((float3(_NapEffectBrightnessParams4.y, _NapEffectBrightnessParams4.y, _NapEffectBrightnessParams4.y))))));
    r0.w = asuint(max(asfloat(r1.w), asfloat(0x3a83126fu)));
    r0.w = asuint((asfloat(0x3f800000u) / asfloat(r0.w)));
    r3.xyz = asuint(mad(asfloat(r3.xyz), asfloat(r0.www), asfloat((r0.xyz ^ 0x80000000u))));
    r4.xyz = ((asfloat(r2.xyz) >= asfloat(asuint((float3(_NapEffectBrightnessParams4.w, _NapEffectBrightnessParams4.w, _NapEffectBrightnessParams4.w))))) ? 0xffffffffu : 0u);
    r4.xyz = (r4.xyz & uint3(0x3f800000u, 0x3f800000u, 0x3f800000u));
    r3.xyz = asuint(mad(asfloat(r4.xyz), asfloat(r3.xyz), asfloat(r0.xyz)));
    r3.xyz = asuint(mad(asfloat(r3.xyz), asfloat(asuint((float3(_NapEffectBrightnessParams4.z, _NapEffectBrightnessParams4.z, _NapEffectBrightnessParams4.z)))), asfloat((r0.xyz ^ 0x80000000u))));
    r0.w = asuint(max(asfloat(r2.y), asfloat(r2.x)));
    r0.w = asuint(max(asfloat(r2.z), asfloat(r0.w)));
    r0.w = asuint((asfloat(r0.w) + asfloat((asuint((_NapEffectBrightnessExtraParams.x)) ^ 0x80000000u))));
    r0.w = asuint(saturate((asfloat(r0.w) * asfloat(asuint((_NapEffectBrightnessExtraParams.y))))));
    r0.xyz = asuint(mad(asfloat(r0.www), asfloat(r3.xyz), asfloat(r0.xyz)));
    r2.xyz = asuint((asfloat((r0.xyz ^ 0x80000000u)) + asfloat(asuint((float3(_SceneParticleFogColorMultiply.x, _SceneParticleFogColorMultiply.y, _SceneParticleFogColorMultiply.z))))));
    r0.w = asuint((asfloat(asuint((_SceneParticleFogColorMultiply.w))) * asfloat(asuint((_AffectByEffectFogColor)))));
    r2.xyz = asuint(mad(asfloat(r0.www), asfloat(r2.xyz), asfloat(r0.xyz)));
    r3.xyz = asuint((asfloat((r2.xyz ^ 0x80000000u)) + asfloat(asuint((float3(_SceneWeatherSandstorm.x, _SceneWeatherSandstorm.y, _SceneWeatherSandstorm.z))))));
    r0.w = asuint((asfloat(asuint((_SceneWeatherSandstorm.w))) * asfloat(asuint((_AffectByEffectSandstormColor)))));
    r2.xyz = asuint(mad(asfloat(r0.www), asfloat(r3.xyz), asfloat(r2.xyz)));
    r3.xyz = asuint((asfloat(asuint((float3(_SceneParticleFogColorMultiply.x, _SceneParticleFogColorMultiply.y, _SceneParticleFogColorMultiply.z)))) + asfloat(uint3(0xbf800000u, 0xbf800000u, 0xbf800000u))));
    r3.xyz = asuint(mad(asfloat(asuint((float3(_AffectByEffectFogColor, _AffectByEffectFogColor, _AffectByEffectFogColor)))), asfloat(r3.xyz), asfloat(uint3(0x3f800000u, 0x3f800000u, 0x3f800000u))));
    r0.xyz = asuint((asfloat(r0.xyz) * asfloat(r3.xyz)));
    r3.xyz = asuint((asfloat(asuint((float3(_SceneWeatherSandstorm.x, _SceneWeatherSandstorm.y, _SceneWeatherSandstorm.z)))) + asfloat(uint3(0xbf800000u, 0xbf800000u, 0xbf800000u))));
    r3.xyz = asuint(mad(asfloat(asuint((float3(_AffectByEffectSandstormColor, _AffectByEffectSandstormColor, _AffectByEffectSandstormColor)))), asfloat(r3.xyz), asfloat(uint3(0x3f800000u, 0x3f800000u, 0x3f800000u))));
    r0.xyz = asuint((asfloat(r0.xyz) * asfloat(r3.xyz)));
    r0.w = ((asfloat(0x3f000000u) < asfloat(asuint((_AffectByGlobalUseMultiply)))) ? 0xffffffffu : 0u);
    r1.xyz = ((r0.www != 0u) ? r0.xyz : r2.xyz);
    r0.xyzw = asuint(max(asfloat(r1.xyzw), asfloat(uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u))));
    r0.xyzw = asuint(log2(asfloat(r0.xyzw)));
    r0.xyzw = asuint((asfloat(r0.xyzw) * asfloat(asuint((float4(_PowerRGB, _PowerRGB, _PowerRGB, _PowerAlpha))))));
    r1.x = asuint((asfloat((asuint((_OpaquenessFadeByScript)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r1.x = asuint(mad(asfloat(r1.x), asfloat(0x3e99999au), asfloat(0x3f800000u)));
    r1.xyz = asuint((asfloat(r0.xyz) * asfloat(r1.xxx)));
    r0.xyzw = asuint(exp2(asfloat(r0.xyzw)));
    r1.xyz = asuint(exp2(asfloat(r1.xyz)));
    r1.w = ((asfloat(asuint((_BlendMode))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r0.xyz = ((r1.www != 0u) ? r1.xyz : r0.xyz);
    r0.w = asuint((asfloat(r0.w) * asfloat(asuint((_AlphaFade)))));
    r0.w = asuint(saturate((asfloat(r0.w) * asfloat(asuint((_AlphaFade_Timeline))))));
    r0.w = asuint((asfloat(r0.w) * asfloat(asuint((_OpaquenessFadeByScript)))));
    r1.x = asuint(dot(asfloat(r0.xyz), asfloat(uint3(0x3e59c6edu, 0x3f371437u, 0x3d93d07du))));
    r1.yzw = asuint((asfloat(r0.xyz) + asfloat((r1.xxx ^ 0x80000000u))));
    r2.x = x0[4].x;
    r2.y = asuint((asfloat(r2.x) * asfloat(asuint((_SceneWeatherParamsPart1[2][2])))));
    r2.x = asuint(mad(asfloat(r2.x), asfloat(asuint((_SceneWeatherParamsPart1[2][2]))), asfloat(0xbf800000u)));
    r2.x = ((asfloat(0x3a83126fu) < asfloat((r2.x & 0x7fffffffu))) ? 0xffffffffu : 0u);
    r1.xyz = asuint(mad(asfloat(r2.yyy), asfloat(r1.yzw), asfloat(r1.xxx)));
    r0.xyz = ((r2.xxx != 0u) ? r1.xyz : r0.xyz);
    r1.x = asuint((asfloat(r0.w) * asfloat(asuint((_SceneWeatherParamsPart7[0][3])))));
    r2.x = (asuint((_SceneWeatherParamsPart7[1][0])) ^ 0x80000000u);
    r2.y = (asuint((_SceneWeatherParamsPart7[1][2])) ^ 0x80000000u);
    r1.yz = asuint((asfloat(r2.xy) + asfloat(v7.xz)));
    r1.y = ((asfloat(asuint((_SceneWeatherParamsPart7[0][0]))) < asfloat((r1.y & 0x7fffffffu))) ? 0xffffffffu : 0u);
    r1.z = ((asfloat(asuint((_SceneWeatherParamsPart7[0][2]))) < asfloat((r1.z & 0x7fffffffu))) ? 0xffffffffu : 0u);
    r1.y = (r1.z | r1.y);
    r1.x = ((r1.y != 0u) ? r1.x : r0.w);
    r1.y = ((asfloat(0x3c23d70au) < asfloat(asuint((_SceneWeatherParamsPart7[1][3])))) ? 0xffffffffu : 0u);
    r0.w = ((r1.y != 0u) ? r1.x : r0.w);
    r1.x = asuint(mad(asfloat(r0.w), asfloat(asuint((_UIFxExtraAlphaWeight))), asfloat((asuint((_UIFxExtraAlphaWeight)) ^ 0x80000000u))));
    r1.x = asuint(saturate((asfloat(r1.x) + asfloat(0x3f800000u))));
    r1.x = asuint((asfloat(r0.w) * asfloat(r1.x)));
    r1.y = ((asfloat(0x3f000000u) < asfloat(asuint((_AffectByCamera3DUIAlpha)))) ? 0xffffffffu : 0u);
    r0.w = ((r1.y != 0u) ? r1.x : r0.w);
    o0.xyz = asuint((asfloat(r0.www) * asfloat(r0.xyz)));
    o0.w = r0.w;
    result.output0 = asfloat(o0.xyzw);
    return result;
}

            #endif
            ENDHLSL
        }
        Pass
        {
            Name "TransparentHalfRes"
            Blend One Zero
            BlendOp Add
            ZTest LEqual
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma target 4.0
            #pragma vertex CorinVertex
            #pragma fragment CorinPixel
            #if defined(SHADER_STAGE_VERTEX)
cbuffer cb0 : register(b0) { float4 cb0[180]; }
cbuffer cb1 : register(b1) { float4 cb1[4]; }
cbuffer cb2 : register(b2) { float4 cb2[65]; }
static uint4 x0[2];
static uint4 x1[6];
static uint4 x2[6];
struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float4 output1 : TEXCOORD0;
    float4 output2 : TEXCOORD1;
    float4 output3 : TEXCOORD2;
    float4 output4 : TEXCOORD3;
    float4 output5 : TEXCOORD4;
    float4 output6 : TEXCOORD5;
    float4 output7 : TEXCOORD6;
    float4 output8 : TEXCOORD7;
    float3 output9 : TEXCOORD8;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float3 input1 : NORMAL0, float4 input2 : COLOR0, float4 input3 : TEXCOORD0, float4 input4 : TEXCOORD1, float4 input5 : TEXCOORD2, float4 input6 : TEXCOORD3, float4 input7 : TEXCOORD4, float4 input8 : TEXCOORD6)
{
    uint4 r0, r1, r2, r3, r4, r5, r6, r7, r8, r9, r10, o0, o1, o2, o3, o4, o5, o6, o7, o8, o9, v0, v1, v2, v3, v4, v5, v6, v7, v8;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyz = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
    v5.xyzw = asuint(input5);
    v6.xyzw = asuint(input6);
    v7.xyzw = asuint(input7);
    v8.xyzw = asuint(input8);
    r0.x = (uint)(asfloat(asuint(cb2[43].z)));
    r0.y = ((asfloat(0x3f000000u) < asfloat(asuint(cb2[43].y))) ? 0xffffffffu : 0u);
    r0.x = ((r0.y != 0u) ? asuint(cb0[14].x) : asuint(cb0[r0.x+17].y));
    r0.x = asuint((asfloat(r0.x) + asfloat((asuint(cb2[61].z) ^ 0x80000000u))));
    r0.y = ((asfloat(0x3f000000u) < asfloat(asuint(cb2[28].y))) ? 0xffffffffu : 0u);
    r1.xyz = asuint((asfloat(asuint(cb2[14].xyz)) * asfloat(uint3(0x3d9e8391u, 0x3d9e8391u, 0x3d9e8391u))));
    r2.xyz = asuint((asfloat(asuint(cb2[14].xyz)) + asfloat(uint3(0x3d6147aeu, 0x3d6147aeu, 0x3d6147aeu))));
    r2.xyz = asuint((asfloat(r2.xyz) * asfloat(uint3(0x3f72a76fu, 0x3f72a76fu, 0x3f72a76fu))));
    r2.xyz = asuint(log2(asfloat((r2.xyz & 0x7fffffffu))));
    r2.xyz = asuint((asfloat(r2.xyz) * asfloat(uint3(0x4019999au, 0x4019999au, 0x4019999au))));
    r2.xyz = asuint(exp2(asfloat(r2.xyz)));
    r3.xyz = ((asfloat(uint3(0x3d25aee6u, 0x3d25aee6u, 0x3d25aee6u)) >= asfloat(asuint(cb2[14].xyz))) ? 0xffffffffu : 0u);
    r1.xyz = ((r3.xyz != 0u) ? r1.xyz : r2.xyz);
    r1.xyz = asuint((asfloat(r1.xyz) * asfloat(v2.xyz)));
    r1.w = asuint((asfloat(v2.w) * asfloat(asuint(cb2[14].w))));
    r1.xyzw = ((r0.yyyy != 0u) ? r1.xyzw : v2.xyzw);
    r2.xy = v4.xy;
    r2.zw = v5.xy;
    r3.xy = v6.zw;
    r3.zw = v8.zw;
    r2.xyzw = ((r0.yyyy != 0u) ? r2.xyzw : r3.xyzw);
    r0.z = asuint((asfloat((r1.w ^ 0x80000000u)) + asfloat(0x3f800000u)));
    x0[1].x = r0.z;
    x1[1].x = 0x3f800000u;
    r0.zw = (uint2)(asfloat(asuint(cb2[64].xy)));
    x0[r0.z + 0].x = v5.x;
    x1[r0.w + 0].x = v5.y;
    r0.z = x0[1].x;
    o4.x = ((r0.y != 0u) ? asuint(cb2[28].z) : r0.z);
    r0.z = x1[1].x;
    o4.z = ((r0.y != 0u) ? asuint(cb2[64].z) : r0.z);
    r0.z = ((asfloat(0x00000000u) != asfloat(asuint(cb2[12].x))) ? 0xffffffffu : 0u);
    r1.xyzw = ((r0.zzzz != 0u) ? uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u) : r1.xyzw);
    r0.z = ((asfloat(0x3f000000u) < asfloat(asuint(cb2[54].w))) ? 0xffffffffu : 0u);
    if (r0.z != 0u) {
        r3.xz = asuint(cb0[22].xz);
        r3.y = asuint((asfloat(asuint(cb0[22].y)) + asfloat(asuint(cb2[54].z))));
        r3.xyz = asuint((asfloat(r3.xyz) + asfloat((v7.xyz ^ 0x80000000u))));
        r0.z = asuint(dot(asfloat(r3.xyz), asfloat(r3.xyz)));
        r0.z = asuint(sqrt(asfloat(r0.z)));
        r0.w = asuint(dot(asfloat((r3.xyz ^ 0x80000000u)), asfloat((r3.xyz ^ 0x80000000u))));
        r0.w = asuint(rsqrt(asfloat(r0.w)));
        r3.xyz = asuint((asfloat(r0.www) * asfloat((r3.xyz ^ 0x80000000u))));
        r0.z = asuint((asfloat(r0.z) + asfloat((asuint(cb2[54].x) ^ 0x80000000u))));
        r0.w = asuint((asfloat((asuint(cb2[54].x) ^ 0x80000000u)) + asfloat(asuint(cb2[54].y))));
        r0.z = asuint(saturate((asfloat(r0.z) / asfloat(r0.w))));
        r0.z = asuint((asfloat((r0.z ^ 0x80000000u)) + asfloat(0x3f800000u)));
        r3.xyz = asuint((asfloat(r0.zzz) * asfloat(r3.xyz)));
        r3.xyz = asuint(mad(asfloat(r3.xyz), asfloat(asuint(cb2[54].xxx)), asfloat(v0.xyz)));
    } else {
        r3.xyz = v0.xyz;
    }
    r4.xyz = asuint((asfloat(r3.yyy) * asfloat(asuint(cb1[1].xyz))));
    r3.xyw = asuint(mad(asfloat(asuint(cb1[0].xyz)), asfloat(r3.xxx), asfloat(r4.xyz)));
    r3.xyz = asuint(mad(asfloat(asuint(cb1[2].xyz)), asfloat(r3.zzz), asfloat(r3.xyw)));
    r3.xyz = asuint((asfloat(r3.xyz) + asfloat(asuint(cb1[3].xyz))));
    r4.xyz = asuint((asfloat((r3.xyz ^ 0x80000000u)) + asfloat(asuint(cb0[30].xyz))));
    r0.z = asuint(dot(asfloat(r4.xyz), asfloat(r4.xyz)));
    o7.w = asuint(sqrt(asfloat(r0.z)));
    r4.xyz = asuint((asfloat(r3.yyy) * asfloat(asuint(cb0[95].xyz))));
    r4.xyz = asuint(mad(asfloat(asuint(cb0[94].xyz)), asfloat(r3.xxx), asfloat(r4.xyz)));
    r4.xyz = asuint(mad(asfloat(asuint(cb0[96].xyz)), asfloat(r3.zzz), asfloat(r4.xyz)));
    r4.xyz = asuint((asfloat(r4.xyz) + asfloat(asuint(cb0[97].xyz))));
    r0.z = asuint(dot(asfloat(r4.xyz), asfloat(r4.xyz)));
    r0.z = asuint(rsqrt(asfloat(r0.z)));
    r5.xyz = asuint((asfloat(r0.zzz) * asfloat(r4.xyz)));
    r4.xyz = asuint(mad(asfloat(r5.xyz), asfloat(asuint(cb2[11].www)), asfloat(r4.xyz)));
    r5.x = asuint(cb0[90].x);
    r5.y = asuint(cb0[92].x);
    r5.z = asuint(cb0[93].x);
    r4.w = 0x3f800000u;
    r5.x = asuint(dot(asfloat(r5.xyz), asfloat(r4.xzw)));
    r6.x = asuint(cb0[91].y);
    r6.y = asuint(cb0[92].y);
    r6.zw = asuint(cb0[93].yz);
    r5.y = asuint(dot(asfloat(r6.xyz), asfloat(r4.yzw)));
    r6.x = asuint(cb0[90].z);
    r6.y = asuint(cb0[91].z);
    r6.z = asuint(cb0[92].z);
    r0.z = asuint(dot(asfloat(r6.xyzw), asfloat(r4.xyzw)));
    r4.x = asuint(cb0[92].w);
    r4.y = asuint(cb0[93].w);
    r0.w = asuint(dot(asfloat(r4.xy), asfloat(r4.zw)));
    r3.w = ((asfloat(0x00000000u) != asfloat(asuint(cb2[58].z))) ? 0xffffffffu : 0u);
    r4.xy = v5.zw;
    r5.zw = v6.xy;
    r6.x = ((r3.w != 0u) ? asuint(cb0[35].w) : r4.x);
    r6.y = ((r3.w != 0u) ? asuint(cb0[36].w) : r4.y);
    r6.z = ((r3.w != 0u) ? asuint(cb0[37].w) : r5.z);
    r6.w = ((r3.w != 0u) ? asuint(cb0[38].w) : r5.w);
    r6.xyzw = ((r0.yyyy != 0u) ? asuint(cb2[27].xyzw) : r6.xyzw);
    r1.xyzw = asuint((asfloat(r1.xyzw) * asfloat(asuint(cb2[13].xyzw))));
    r4.xy = ((asfloat(uint2(0x3f000000u, 0x3f000000u)) < asfloat(asuint(cb2[58].wy))) ? 0xffffffffu : 0u);
    r7.x = asuint(cb0[39].y);
    r7.y = asuint(cb0[40].y);
    r7.z = asuint(cb0[41].y);
    r7.w = asuint(cb0[42].y);
    r8.xyzw = asuint((asfloat(r6.xyzw) * asfloat(r7.xyzw)));
    r9.xyzw = asuint((asfloat(r1.xyzw) * asfloat(r7.xyzw)));
    r8.xyzw = ((r4.yyyy != 0u) ? r8.xyzw : r7.xyzw);
    r7.xyzw = ((r4.yyyy != 0u) ? r9.xyzw : r7.xyzw);
    o5.xyzw = ((r4.xxxx != 0u) ? r8.xyzw : r6.xyzw);
    o6.xyzw = ((r4.xxxx != 0u) ? r7.xyzw : r1.xyzw);
    r0.y = asuint(max(asfloat(asuint(cb2[16].x)), asfloat(0x00000000u)));
    r0.y = asuint(min(asfloat(r0.y), asfloat(0x40a00000u)));
    x2[0].xy = v3.xy;
    x2[1].xy = r2.xy;
    x2[2].xy = v3.xy;
    x2[3].xy = r2.xy;
    x2[4].xy = v3.xy;
    x2[5].xy = r2.zw;
    r0.y = asuint(floor(asfloat(r0.y)));
    r0.y = (uint)(asfloat(r0.y));
    r1.xy = x2[r0.y + 0].xy;
    r0.y = ((asfloat(asuint(cb2[28].y)) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r2.xy = (r0.yy & v3.zw);
    r4.xy = (r0.yy & v4.zw);
    r1.zw = asuint((asfloat((r1.xy ^ 0x80000000u)) + asfloat(uint2(0x3f800000u, 0x3f800000u))));
    r6.xyzw = asuint((asfloat((r1.xyzw ^ 0x80000000u)) + asfloat(r1.yzwx)));
    r1.xyzw = asuint(mad(asfloat(asuint(cb2[17].xxxx)), asfloat(r6.xyzw), asfloat(r1.xyzw)));
    r6.xyzw = ((asfloat(asuint(cb2[16].wwww)) < asfloat(uint4(0x3f000000u, 0x3fc00000u, 0x40200000u, 0x477fe000u))) ? 0xffffffffu : 0u);
    r7.xyzw = (r6.xyzw & uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
    r6.xyz = ((r6.xyz != 0u) ? uint3(0xbf800000u, 0xbf800000u, 0xbf800000u) : uint3(0x80000000u, 0x80000000u, 0x80000000u));
    r6.xyz = asuint((asfloat(r6.xyz) + asfloat(r7.yzw)));
    r6.xyz = asuint(max(asfloat(r6.xyz), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r8.xy = r1.zw;
    r8.z = 0x3f800000u;
    r8.w = asuint((asfloat((asuint(cb2[17].x) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r9.xy = r1.xw;
    r9.z = 0x00000000u;
    r9.w = asuint((asfloat((asuint(cb2[17].x) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r10.xyzw = asuint((asfloat(r6.yyyy) * asfloat(r9.xyzw)));
    r10.xyzw = asuint(mad(asfloat(r8.xyzw), asfloat(r6.zzzz), asfloat(r10.xyzw)));
    r1.xz = r8.xz;
    r1.w = asuint(cb2[17].x);
    r6.xyzw = asuint(mad(asfloat(r1.xyzw), asfloat(r6.xxxx), asfloat(r10.xyzw)));
    r1.xz = r9.xz;
    r1.xyzw = asuint(mad(asfloat(r1.xyzw), asfloat(r7.xxxx), asfloat(r6.xyzw)));
    r1.xy = asuint((asfloat((r1.zw ^ 0x80000000u)) + asfloat(r1.xy)));
    r1.xy = asuint(mad(asfloat(r1.xy), asfloat(asuint(cb2[1].xy)), asfloat(asuint(cb2[1].zw))));
    r1.xy = asuint((asfloat(r1.zw) + asfloat(r1.xy)));
    r0.y = ((asfloat(0x3f000000u) < asfloat(asuint(cb2[25].z))) ? 0xffffffffu : 0u);
    r1.zw = asuint((asfloat(r0.xx) * asfloat(asuint(cb2[9].xy))));
    r1.zw = asuint(frac(asfloat(r1.zw)));
    r1.zw = asuint((asfloat(r1.zw) + asfloat(r4.xy)));
    r4.xy = ((asfloat(asuint(cb2[18].xy)) != asfloat(uint2(0x00000000u, 0x00000000u))) ? 0xffffffffu : 0u);
    r5.zw = asuint((asfloat(r1.zw) / asfloat(asuint(cb2[18].xy))));
    r5.zw = asuint(floor(asfloat(r5.zw)));
    r5.zw = asuint((asfloat(r5.zw) * asfloat(asuint(cb2[18].xy))));
    r1.zw = ((r4.xy != 0u) ? r5.zw : r1.zw);
    r1.zw = asuint((asfloat(r1.zw) + asfloat(r1.xy)));
    r0.xy = ((r0.yy != 0u) ? r1.zw : r1.xy);
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint(cb2[17].y))) ? 0xffffffffu : 0u);
    r1.yz = asuint(mad(asfloat(r2.xy), asfloat(uint2(0x40490fdbu, 0x3fb8aa3bu)), asfloat(r0.xy)));
    o2.xy = ((r1.xx != 0u) ? r1.yz : r0.xy);
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint(cb2[0].x))) ? 0xffffffffu : 0u);
    r0.y = asuint(max(asfloat(r0.z), asfloat(0x00000000u)));
    o0.z = ((r0.x != 0u) ? r0.y : r0.z);
    o0.xy = asuint(mad(asfloat(asuint(cb0[179].xy)), asfloat(r0.ww), asfloat(r5.xy)));
    o0.w = r0.w;
    o1.xyzw = v3.xyzw;
    o2.zw = uint2(0x00000000u, 0x00000000u);
    o3.xyzw = uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u);
    o4.y = 0x00000000u;
    o4.w = r4.z;
    o7.xyz = r3.xyz;
    o8.xy = v5.xy;
    o8.zw = uint2(0x00000000u, 0x00000000u);
    o9.xy = r2.zw;
    o9.z = v7.w;
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    result.output2 = asfloat(o2.xyzw);
    result.output3 = asfloat(o3.xyzw);
    result.output4 = asfloat(o4.xyzw);
    result.output5 = asfloat(o5.xyzw);
    result.output6 = asfloat(o6.xyzw);
    result.output7 = asfloat(o7.xyzw);
    result.output8 = asfloat(o8.xyzw);
    result.output9 = asfloat(o9.xyz);
    return result;
}

            #elif defined(SHADER_STAGE_FRAGMENT)
cbuffer ZZZGlobals : register(b0)
{
    column_major float4x4 _SceneWeatherParamsPart1 : packoffset(c31);
    column_major float4x4 _SceneWeatherParamsPart6 : packoffset(c51);
    column_major float4x4 _SceneWeatherParamsPart7 : packoffset(c55);
    float4 _MainLightColor : packoffset(c7);
    float4 _SceneParticleFogColorMultiply : packoffset(c10);
    float _GlobalMipBias : packoffset(c61);
    float4 _NapEffectBrightnessParams4 : packoffset(c169);
    float4 _NapEffectBrightnessExtraParams : packoffset(c170);
    float4 _SceneWeatherSandstorm : packoffset(c177);
    float _UIFxExtraAlphaWeight : packoffset(c182);
}

cbuffer UnityPerMaterial : register(b1)
{
    float _LerpBrightness : packoffset(c10.z);
    float4 _AmbientColor : packoffset(c15);
    float _MainTexClampU : packoffset(c16.y);
    float _MainTexClampV : packoffset(c16.z);
    float _PowerRGB : packoffset(c24.z);
    float _PowerAlpha : packoffset(c24.w);
    float _ColorChannelMapping : packoffset(c25);
    float _AlphaChannelMapping : packoffset(c25.y);
    float _OpaquenessFadeByScript : packoffset(c25.w);
    float _2ToneUsingVertexAlpha : packoffset(c26.y);
    float _AffectedByMainLightColor : packoffset(c26.w);
    float _AlphaFade : packoffset(c28.w);
    float _AlphaFade_Timeline : packoffset(c29);
    float _SceneClipOffset : packoffset(c29.z);
    float _BlendMode : packoffset(c43);
    float _Saturation : packoffset(c57.z);
    float _AffectByGlobalUseMultiply : packoffset(c58.y);
    float _AffectByEffectFogColor : packoffset(c59);
    float _AffectByCamera3DUIAlpha : packoffset(c59.y);
    float _AffectByEffectSandstormColor : packoffset(c59.z);
    float _CustomData1W : packoffset(c64.y);
    float _BackfaceRevert : packoffset(c65.y);
}
static const uint4 icb[4] = { uint4(0x3f800000u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x3f800000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x3f800000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u) };


SamplerState sampler_MainTex;
Texture2D<float4> _MainTex : register(t0);
static uint4 x0[5];
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float4 input1 : TEXCOORD0, float4 input2 : TEXCOORD1, float4 input3 : TEXCOORD2, float4 input4 : TEXCOORD3, float4 input5 : TEXCOORD4, float4 input6 : TEXCOORD5, float4 input7 : TEXCOORD6, float4 input8 : TEXCOORD7, float3 input9 : TEXCOORD8, bool input10 : SV_IsFrontFace0)
{
    uint4 r0, r1, r2, r3, r4, o0, v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
    v5.xyzw = asuint(input5);
    v6.xyzw = asuint(input6);
    v7.xyzw = asuint(input7);
    v8.xyzw = asuint(input8);
    v9.xyz = asuint(input9);
    v10.x = (input10 ? 0xffffffffu : 0u);
    x0[4].x = asuint((_Saturation));
    r0.x = (uint)(asfloat(asuint((_CustomData1W))));
    x0[r0.x + 0].x = v8.y;
    r0.xy = ((asfloat(asuint((float2(_MainTexClampU, _MainTexClampV)))) >= asfloat(uint2(0x3f000000u, 0x3f000000u))) ? 0xffffffffu : 0u);
    r0.xy = (r0.xy & uint2(0x3f800000u, 0x3f800000u));
    r0.z = ((asfloat(0x3f000000u) < asfloat(asuint((_BackfaceRevert)))) ? 0xffffffffu : 0u);
    r0.w = ((v10.x != 0u) ? 0x3f800000u : 0xbf800000u);
    r0.z = ((r0.z != 0u) ? r0.w : 0x3f800000u);
    r1.x = asuint((asfloat(r0.z) * asfloat(v2.x)));
    r1.y = v2.y;
    r0.zw = asuint(saturate(asfloat(r1.xy)));
    r0.zw = asuint((asfloat((r1.xy ^ 0x80000000u)) + asfloat(r0.zw)));
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(r0.zw), asfloat(r1.xy)));
    r0.xyzw = asuint(_MainTex.SampleBias(sampler_MainTex, asfloat(r0.xy), asfloat(asuint((_GlobalMipBias)))).xyzw);
    r1.xy = (uint2)(asfloat(asuint((float2(_ColorChannelMapping, _AlphaChannelMapping)))));
    r1.xy = min(r1.xy, uint2(0x00000003u, 0x00000003u));
    r1.y = asuint(dot(asfloat(r0.xyzw), asfloat(icb[r1.y+0].xyzw)));
    r0.w = asuint(dot(asfloat(r0.xyzw), asfloat(icb[r1.x+0].xyzw)));
    r2.xyzw = asuint((asfloat((v5.xyzw ^ 0x80000000u)) + asfloat(v6.xyzw)));
    r1.x = asuint(mad(asfloat(r1.y), asfloat(r2.w), asfloat(v5.w)));
    r1.z = ((asfloat(asuint((_2ToneUsingVertexAlpha))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r1.x = ((r1.z != 0u) ? v6.w : r1.x);
    r1.x = asuint((asfloat(r1.x) * asfloat(r1.y)));
    r1.y = asuint((asfloat(asuint((_SceneWeatherParamsPart6[0][3]))) + asfloat(asuint((_SceneClipOffset)))));
    r1.y = ((asfloat(v7.y) < asfloat(r1.y)) ? 0xffffffffu : 0u);
    r1.w = ((r1.y != 0u) ? 0x00000000u : r1.x);
    r2.w = ((asfloat(0x40600000u) < asfloat(asuint((_ColorChannelMapping)))) ? 0xffffffffu : 0u);
    r0.w = ((r2.w != 0u) ? 0x3f800000u : r0.w);
    r0.xyz = ((r2.www != 0u) ? r0.xyz : uint3(0x3f800000u, 0x3f800000u, 0x3f800000u));
    r0.w = asuint(saturate((asfloat(r0.w) * asfloat(asuint((_LerpBrightness))))));
    r2.xyz = asuint(mad(asfloat(r0.www), asfloat(r2.xyz), asfloat(v5.xyz)));
    r0.xyz = asuint((asfloat(r0.xyz) * asfloat(r2.xyz)));
    r2.xyz = asuint((asfloat(asuint((float3(_MainLightColor.x, _MainLightColor.y, _MainLightColor.z)))) + asfloat(uint3(0xbf800000u, 0xbf800000u, 0xbf800000u))));
    r2.xyz = asuint(mad(asfloat(asuint((float3(_AffectedByMainLightColor, _AffectedByMainLightColor, _AffectedByMainLightColor)))), asfloat(r2.xyz), asfloat(asuint((float3(_AmbientColor.x, _AmbientColor.y, _AmbientColor.z))))));
    r2.xyz = asuint((asfloat(r2.xyz) + asfloat(uint3(0x3f800000u, 0x3f800000u, 0x3f800000u))));
    r0.xyz = asuint((asfloat(r0.xyz) * asfloat(r2.xyz)));
    r2.xyz = asuint((asfloat(r1.www) * asfloat(r0.xyz)));
    r3.xyz = asuint(mad(asfloat(r2.xyz), asfloat(asuint((float3(_NapEffectBrightnessParams4.x, _NapEffectBrightnessParams4.x, _NapEffectBrightnessParams4.x)))), asfloat(asuint((float3(_NapEffectBrightnessParams4.y, _NapEffectBrightnessParams4.y, _NapEffectBrightnessParams4.y))))));
    r0.w = asuint(max(asfloat(r1.w), asfloat(0x3a83126fu)));
    r0.w = asuint((asfloat(0x3f800000u) / asfloat(r0.w)));
    r3.xyz = asuint(mad(asfloat(r3.xyz), asfloat(r0.www), asfloat((r0.xyz ^ 0x80000000u))));
    r4.xyz = ((asfloat(r2.xyz) >= asfloat(asuint((float3(_NapEffectBrightnessParams4.w, _NapEffectBrightnessParams4.w, _NapEffectBrightnessParams4.w))))) ? 0xffffffffu : 0u);
    r4.xyz = (r4.xyz & uint3(0x3f800000u, 0x3f800000u, 0x3f800000u));
    r3.xyz = asuint(mad(asfloat(r4.xyz), asfloat(r3.xyz), asfloat(r0.xyz)));
    r3.xyz = asuint(mad(asfloat(r3.xyz), asfloat(asuint((float3(_NapEffectBrightnessParams4.z, _NapEffectBrightnessParams4.z, _NapEffectBrightnessParams4.z)))), asfloat((r0.xyz ^ 0x80000000u))));
    r0.w = asuint(max(asfloat(r2.y), asfloat(r2.x)));
    r0.w = asuint(max(asfloat(r2.z), asfloat(r0.w)));
    r0.w = asuint((asfloat(r0.w) + asfloat((asuint((_NapEffectBrightnessExtraParams.x)) ^ 0x80000000u))));
    r0.w = asuint(saturate((asfloat(r0.w) * asfloat(asuint((_NapEffectBrightnessExtraParams.y))))));
    r0.xyz = asuint(mad(asfloat(r0.www), asfloat(r3.xyz), asfloat(r0.xyz)));
    r2.xyz = asuint((asfloat((r0.xyz ^ 0x80000000u)) + asfloat(asuint((float3(_SceneParticleFogColorMultiply.x, _SceneParticleFogColorMultiply.y, _SceneParticleFogColorMultiply.z))))));
    r0.w = asuint((asfloat(asuint((_SceneParticleFogColorMultiply.w))) * asfloat(asuint((_AffectByEffectFogColor)))));
    r2.xyz = asuint(mad(asfloat(r0.www), asfloat(r2.xyz), asfloat(r0.xyz)));
    r3.xyz = asuint((asfloat((r2.xyz ^ 0x80000000u)) + asfloat(asuint((float3(_SceneWeatherSandstorm.x, _SceneWeatherSandstorm.y, _SceneWeatherSandstorm.z))))));
    r0.w = asuint((asfloat(asuint((_SceneWeatherSandstorm.w))) * asfloat(asuint((_AffectByEffectSandstormColor)))));
    r2.xyz = asuint(mad(asfloat(r0.www), asfloat(r3.xyz), asfloat(r2.xyz)));
    r3.xyz = asuint((asfloat(asuint((float3(_SceneParticleFogColorMultiply.x, _SceneParticleFogColorMultiply.y, _SceneParticleFogColorMultiply.z)))) + asfloat(uint3(0xbf800000u, 0xbf800000u, 0xbf800000u))));
    r3.xyz = asuint(mad(asfloat(asuint((float3(_AffectByEffectFogColor, _AffectByEffectFogColor, _AffectByEffectFogColor)))), asfloat(r3.xyz), asfloat(uint3(0x3f800000u, 0x3f800000u, 0x3f800000u))));
    r0.xyz = asuint((asfloat(r0.xyz) * asfloat(r3.xyz)));
    r3.xyz = asuint((asfloat(asuint((float3(_SceneWeatherSandstorm.x, _SceneWeatherSandstorm.y, _SceneWeatherSandstorm.z)))) + asfloat(uint3(0xbf800000u, 0xbf800000u, 0xbf800000u))));
    r3.xyz = asuint(mad(asfloat(asuint((float3(_AffectByEffectSandstormColor, _AffectByEffectSandstormColor, _AffectByEffectSandstormColor)))), asfloat(r3.xyz), asfloat(uint3(0x3f800000u, 0x3f800000u, 0x3f800000u))));
    r0.xyz = asuint((asfloat(r0.xyz) * asfloat(r3.xyz)));
    r0.w = ((asfloat(0x3f000000u) < asfloat(asuint((_AffectByGlobalUseMultiply)))) ? 0xffffffffu : 0u);
    r1.xyz = ((r0.www != 0u) ? r0.xyz : r2.xyz);
    r0.xyzw = asuint(max(asfloat(r1.xyzw), asfloat(uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u))));
    r0.xyzw = asuint(log2(asfloat(r0.xyzw)));
    r0.xyzw = asuint((asfloat(r0.xyzw) * asfloat(asuint((float4(_PowerRGB, _PowerRGB, _PowerRGB, _PowerAlpha))))));
    r1.x = asuint((asfloat((asuint((_OpaquenessFadeByScript)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r1.x = asuint(mad(asfloat(r1.x), asfloat(0x3e99999au), asfloat(0x3f800000u)));
    r1.xyz = asuint((asfloat(r0.xyz) * asfloat(r1.xxx)));
    r0.xyzw = asuint(exp2(asfloat(r0.xyzw)));
    r1.xyz = asuint(exp2(asfloat(r1.xyz)));
    r1.w = ((asfloat(asuint((_BlendMode))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r0.xyz = ((r1.www != 0u) ? r1.xyz : r0.xyz);
    r0.w = asuint((asfloat(r0.w) * asfloat(asuint((_AlphaFade)))));
    r0.w = asuint(saturate((asfloat(r0.w) * asfloat(asuint((_AlphaFade_Timeline))))));
    r0.w = asuint((asfloat(r0.w) * asfloat(asuint((_OpaquenessFadeByScript)))));
    r1.x = asuint(dot(asfloat(r0.xyz), asfloat(uint3(0x3e59c6edu, 0x3f371437u, 0x3d93d07du))));
    r1.yzw = asuint((asfloat(r0.xyz) + asfloat((r1.xxx ^ 0x80000000u))));
    r2.x = x0[4].x;
    r2.y = asuint((asfloat(r2.x) * asfloat(asuint((_SceneWeatherParamsPart1[2][2])))));
    r2.x = asuint(mad(asfloat(r2.x), asfloat(asuint((_SceneWeatherParamsPart1[2][2]))), asfloat(0xbf800000u)));
    r2.x = ((asfloat(0x3a83126fu) < asfloat((r2.x & 0x7fffffffu))) ? 0xffffffffu : 0u);
    r1.xyz = asuint(mad(asfloat(r2.yyy), asfloat(r1.yzw), asfloat(r1.xxx)));
    r0.xyz = ((r2.xxx != 0u) ? r1.xyz : r0.xyz);
    r1.x = asuint((asfloat(r0.w) * asfloat(asuint((_SceneWeatherParamsPart7[0][3])))));
    r2.x = (asuint((_SceneWeatherParamsPart7[1][0])) ^ 0x80000000u);
    r2.y = (asuint((_SceneWeatherParamsPart7[1][2])) ^ 0x80000000u);
    r1.yz = asuint((asfloat(r2.xy) + asfloat(v7.xz)));
    r1.y = ((asfloat(asuint((_SceneWeatherParamsPart7[0][0]))) < asfloat((r1.y & 0x7fffffffu))) ? 0xffffffffu : 0u);
    r1.z = ((asfloat(asuint((_SceneWeatherParamsPart7[0][2]))) < asfloat((r1.z & 0x7fffffffu))) ? 0xffffffffu : 0u);
    r1.y = (r1.z | r1.y);
    r1.x = ((r1.y != 0u) ? r1.x : r0.w);
    r1.y = ((asfloat(0x3c23d70au) < asfloat(asuint((_SceneWeatherParamsPart7[1][3])))) ? 0xffffffffu : 0u);
    r0.w = ((r1.y != 0u) ? r1.x : r0.w);
    r1.x = asuint(mad(asfloat(r0.w), asfloat(asuint((_UIFxExtraAlphaWeight))), asfloat((asuint((_UIFxExtraAlphaWeight)) ^ 0x80000000u))));
    r1.x = asuint(saturate((asfloat(r1.x) + asfloat(0x3f800000u))));
    r1.x = asuint((asfloat(r0.w) * asfloat(r1.x)));
    r1.y = ((asfloat(0x3f000000u) < asfloat(asuint((_AffectByCamera3DUIAlpha)))) ? 0xffffffffu : 0u);
    r0.w = ((r1.y != 0u) ? r1.x : r0.w);
    o0.xyz = asuint((asfloat(r0.www) * asfloat(r0.xyz)));
    r0.x = asuint((asfloat(asuint((_BlendMode))) + asfloat(0xbf800000u)));
    o0.w = asuint(mad(asfloat(r0.w), asfloat(r0.x), asfloat(0x3f800000u)));
    result.output0 = asfloat(o0.xyzw);
    return result;
}

            #endif
            ENDHLSL
        }
        Pass
        {
            Name "Distortion"
            Blend One Zero
            BlendOp Add
            ZTest LEqual
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma target 4.0
            #pragma vertex CorinVertex
            #pragma fragment CorinPixel
            #if defined(SHADER_STAGE_VERTEX)
cbuffer ZZZGlobals : register(b0)
{
    column_major float4x4 glstate_matrix_projection : packoffset(c90);
    column_major float4x4 unity_MatrixV : packoffset(c94);
}

cbuffer UnityPerDraw : register(b1)
{
    column_major float4x4 unity_ObjectToWorld : packoffset(c0);
}

cbuffer UnityPerMaterial : register(b2)
{
    float _ZOffset : packoffset(c11.w);
    float _OpaquenessFadeByScript : packoffset(c25.w);
    float _DTIntensity : packoffset(c41.z);
    float _Dist_Intensity_PostProcessing : packoffset(c42);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float2 output1 : TEXCOORD0;
    float output2 : TEXCOORD1;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float3 input1 : NORMAL0, float4 input2 : COLOR0, float4 input3 : TEXCOORD0, float4 input4 : TEXCOORD1, float4 input5 : TEXCOORD2, float4 input6 : TEXCOORD3, float4 input7 : TEXCOORD4, float4 input8 : TEXCOORD6)
{
    uint4 r0, r1, o0, o1, v0, v1, v2, v3, v4, v5, v6, v7, v8;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyz = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
    v5.xyzw = asuint(input5);
    v6.xyzw = asuint(input6);
    v7.xyzw = asuint(input7);
    v8.xyzw = asuint(input8);
    r0.xyz = asuint((asfloat(v0.yyy) * asfloat(asuint((float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1]))))));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0])))), asfloat(v0.xxx), asfloat(r0.xyz)));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2])))), asfloat(v0.zzz), asfloat(r0.xyz)));
    r0.xyz = asuint((asfloat(r0.xyz) + asfloat(asuint((float3(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3]))))));
    r1.xyz = asuint((asfloat(r0.yyy) * asfloat(asuint((float3(unity_MatrixV[0][1], unity_MatrixV[1][1], unity_MatrixV[2][1]))))));
    r0.xyw = asuint(mad(asfloat(asuint((float3(unity_MatrixV[0][0], unity_MatrixV[1][0], unity_MatrixV[2][0])))), asfloat(r0.xxx), asfloat(r1.xyz)));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_MatrixV[0][2], unity_MatrixV[1][2], unity_MatrixV[2][2])))), asfloat(r0.zzz), asfloat(r0.xyw)));
    r0.xyz = asuint((asfloat(r0.xyz) + asfloat(asuint((float3(unity_MatrixV[0][3], unity_MatrixV[1][3], unity_MatrixV[2][3]))))));
    r0.w = asuint(dot(asfloat(r0.xyz), asfloat(r0.xyz)));
    r0.w = asuint(rsqrt(asfloat(r0.w)));
    r1.xyz = asuint((asfloat(r0.www) * asfloat(r0.xyz)));
    r0.xyz = asuint(mad(asfloat(r1.xyz), asfloat(asuint((float3(_ZOffset, _ZOffset, _ZOffset)))), asfloat(r0.xyz)));
    r1.x = asuint((glstate_matrix_projection[2][0]));
    r1.y = asuint((glstate_matrix_projection[2][1]));
    r1.z = asuint((glstate_matrix_projection[2][2]));
    r1.w = asuint((glstate_matrix_projection[2][3]));
    r0.w = 0x3f800000u;
    o0.z = asuint(dot(asfloat(r1.xyzw), asfloat(r0.xyzw)));
    r1.x = asuint((glstate_matrix_projection[0][0]));
    r1.y = asuint((glstate_matrix_projection[0][2]));
    r1.z = asuint((glstate_matrix_projection[0][3]));
    o0.x = asuint(dot(asfloat(r1.xyz), asfloat(r0.xzw)));
    r1.x = asuint((glstate_matrix_projection[1][1]));
    r1.y = asuint((glstate_matrix_projection[1][2]));
    r1.z = asuint((glstate_matrix_projection[1][3]));
    o0.y = asuint(dot(asfloat(r1.xyz), asfloat(r0.yzw)));
    r0.x = asuint((glstate_matrix_projection[3][2]));
    r0.y = asuint((glstate_matrix_projection[3][3]));
    o0.w = asuint(dot(asfloat(r0.xy), asfloat(r0.zw)));
    r0.x = asuint((asfloat(v2.w) * asfloat(asuint((_DTIntensity)))));
    r0.x = asuint((asfloat(r0.x) * asfloat(asuint((_OpaquenessFadeByScript)))));
    o1.z = asuint((asfloat(r0.x) * asfloat(asuint((_Dist_Intensity_PostProcessing)))));
    o1.xy = v3.xy;
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xy);
    result.output2 = asfloat(o1.z);
    return result;
}

            #elif defined(SHADER_STAGE_FRAGMENT)
cbuffer ZZZGlobals : register(b0)
{
    column_major float4x4 _GlobalTimeParamsA : packoffset(c13);
    column_major float4x4 _GlobalTimeParamsB : packoffset(c17);
    float _GlobalMipBias : packoffset(c61);
    float4 _ZBufferParams : packoffset(c62);
    float4 _ScreenSize : packoffset(c138);
}

cbuffer UnityPerMaterial : register(b1)
{
    float4 _DTTex_ST : packoffset(c8);
    float _Distortion : packoffset(c40);
    float _DTTexUVMode : packoffset(c40.y);
    float _DTTexClampU : packoffset(c40.z);
    float _DTTexClampV : packoffset(c40.w);
    float _DTTexFlip : packoffset(c41);
    float _DTTexRotation : packoffset(c41.y);
    float _SeparateRGBIntensity : packoffset(c41.w);
    float _DtUvMove : packoffset(c42.y);
    float _DtUSpeed : packoffset(c42.z);
    float _DtVSpeed : packoffset(c42.w);
    float _IgnoreTimeScale : packoffset(c43.y);
    float _DistortionMode : packoffset(c57.w);
}


SamplerState sampler_DepthMipChain;
SamplerState sampler_DTTex;
Texture2D<float4> _DepthMipChain : register(t0);
Texture2D<float4> _DTTex : register(t1);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
    float4 output1 : SV_Target1;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float2 input1 : TEXCOORD0, float input2 : TEXCOORD1)
{
    uint4 r0, r1, r2, r3, r4, r5, o0, o1, v0, v1;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xy = asuint(input1);
    v1.z = asuint(input2);
    r0.x = ((asfloat(asuint((_Distortion))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    if (r0.x != 0u) {
        o0.xyzw = uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u);
        o1.xyzw = uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u);
        result.output0 = asfloat(o0.xyzw);
        result.output1 = asfloat(o1.xyzw);
        return result;
    }
    r0.xy = asuint(mad(asfloat(v1.xy), asfloat(uint2(0x40000000u, 0x40000000u)), asfloat(uint2(0xbf800000u, 0xbf800000u))));
    r0.z = asuint(min(asfloat((r0.x & 0x7fffffffu)), asfloat((r0.y & 0x7fffffffu))));
    r0.w = asuint(max(asfloat((r0.x & 0x7fffffffu)), asfloat((r0.y & 0x7fffffffu))));
    r0.w = asuint((asfloat(0x3f800000u) / asfloat(r0.w)));
    r0.z = asuint((asfloat(r0.w) * asfloat(r0.z)));
    r0.w = asuint((asfloat(r0.z) * asfloat(r0.z)));
    r1.x = asuint(mad(asfloat(r0.w), asfloat(0x3caaae5fu), asfloat(0xbdae5a36u)));
    r1.x = asuint(mad(asfloat(r0.w), asfloat(r1.x), asfloat(0x3e3876e2u)));
    r1.x = asuint(mad(asfloat(r0.w), asfloat(r1.x), asfloat(0xbea91d04u)));
    r0.w = asuint(mad(asfloat(r0.w), asfloat(r1.x), asfloat(0x3f7ff738u)));
    r1.x = asuint((asfloat(r0.w) * asfloat(r0.z)));
    r1.y = ((asfloat((r0.x & 0x7fffffffu)) < asfloat((r0.y & 0x7fffffffu))) ? 0xffffffffu : 0u);
    r1.x = asuint(mad(asfloat(r1.x), asfloat(0xc0000000u), asfloat(0x3fc90fdbu)));
    r1.x = (r1.y & r1.x);
    r0.z = asuint(mad(asfloat(r0.z), asfloat(r0.w), asfloat(r1.x)));
    r0.w = ((asfloat(r0.x) < asfloat((r0.x ^ 0x80000000u))) ? 0xffffffffu : 0u);
    r0.w = (r0.w & 0xc0490fdbu);
    r0.z = asuint((asfloat(r0.w) + asfloat(r0.z)));
    r0.w = asuint(min(asfloat(r0.x), asfloat(r0.y)));
    r1.x = asuint(max(asfloat(r0.x), asfloat(r0.y)));
    r0.w = ((asfloat(r0.w) < asfloat((r0.w ^ 0x80000000u))) ? 0xffffffffu : 0u);
    r1.x = ((asfloat(r1.x) >= asfloat((r1.x ^ 0x80000000u))) ? 0xffffffffu : 0u);
    r0.w = (r0.w & r1.x);
    r1.x = ((r0.w != 0u) ? (r0.z ^ 0x80000000u) : r0.z);
    r0.x = asuint(dot(asfloat(r0.xy), asfloat(r0.xy)));
    r0.x = asuint(sqrt(asfloat(r0.x)));
    r1.y = asuint((asfloat(r0.x) * asfloat(0x3f000000u)));
    r0.xy = asuint((asfloat(r1.xy) + asfloat((v1.xy ^ 0x80000000u))));
    r0.xy = asuint(mad(asfloat(asuint((float2(_DTTexUVMode, _DTTexUVMode)))), asfloat(r0.xy), asfloat(v1.xy)));
    r0.zw = asuint((asfloat((r0.xy ^ 0x80000000u)) + asfloat(uint2(0x3f800000u, 0x3f800000u))));
    r1.xyzw = asuint((asfloat((r0.xyzw ^ 0x80000000u)) + asfloat(r0.yzwx)));
    r0.xyzw = asuint(mad(asfloat(asuint((float4(_DTTexRotation, _DTTexRotation, _DTTexRotation, _DTTexRotation)))), asfloat(r1.xyzw), asfloat(r0.xyzw)));
    r1.xyzw = ((asfloat(asuint((float4(_DTTexFlip, _DTTexFlip, _DTTexFlip, _DTTexFlip)))) < asfloat(uint4(0x3f000000u, 0x3fc00000u, 0x40200000u, 0x477fe000u))) ? 0xffffffffu : 0u);
    r2.xyzw = (r1.xyzw & uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
    r1.xyz = ((r1.xyz != 0u) ? uint3(0xbf800000u, 0xbf800000u, 0xbf800000u) : uint3(0x80000000u, 0x80000000u, 0x80000000u));
    r1.xyz = asuint((asfloat(r1.xyz) + asfloat(r2.yzw)));
    r1.xyz = asuint(max(asfloat(r1.xyz), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r3.xy = r0.zw;
    r3.z = 0x3f800000u;
    r3.w = asuint((asfloat((asuint((_DTTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r4.xy = r0.xw;
    r4.z = 0x00000000u;
    r4.w = asuint((asfloat((asuint((_DTTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r5.xyzw = asuint((asfloat(r1.yyyy) * asfloat(r4.xyzw)));
    r5.xyzw = asuint(mad(asfloat(r3.xyzw), asfloat(r1.zzzz), asfloat(r5.xyzw)));
    r0.xz = r3.xz;
    r0.w = asuint((_DTTexRotation));
    r1.xyzw = asuint(mad(asfloat(r0.xyzw), asfloat(r1.xxxx), asfloat(r5.xyzw)));
    r0.xz = r4.xz;
    r0.xyzw = asuint(mad(asfloat(r0.xyzw), asfloat(r2.xxxx), asfloat(r1.xyzw)));
    r0.xy = asuint((asfloat((r0.zw ^ 0x80000000u)) + asfloat(r0.xy)));
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_DTTex_ST.x, _DTTex_ST.y)))), asfloat(asuint((float2(_DTTex_ST.z, _DTTex_ST.w))))));
    r0.xy = asuint((asfloat(r0.zw) + asfloat(r0.xy)));
    r0.z = ((asfloat(0x3f000000u) < asfloat(asuint((_DtUvMove)))) ? 0xffffffffu : 0u);
    r0.w = ((asfloat(0x3f000000u) < asfloat(asuint((_IgnoreTimeScale)))) ? 0xffffffffu : 0u);
    r0.w = ((r0.w != 0u) ? asuint((_GlobalTimeParamsA[0][1])) : asuint((_GlobalTimeParamsB[1][0])));
    r1.xy = asuint((asfloat(r0.ww) * asfloat(asuint((float2(_DtUSpeed, _DtVSpeed))))));
    r1.xy = asuint(frac(asfloat(r1.xy)));
    r1.xy = asuint((asfloat(r0.xy) + asfloat(r1.xy)));
    r0.xy = ((r0.zz != 0u) ? r1.xy : r0.xy);
    r0.zw = ((asfloat(asuint((float2(_DTTexClampU, _DTTexClampV)))) >= asfloat(uint2(0x3f000000u, 0x3f000000u))) ? 0xffffffffu : 0u);
    r0.zw = (r0.zw & uint2(0x3f800000u, 0x3f800000u));
    r1.xy = asuint(saturate(asfloat(r0.xy)));
    r1.xy = asuint((asfloat((r0.xy ^ 0x80000000u)) + asfloat(r1.xy)));
    r0.xy = asuint(mad(asfloat(r0.zw), asfloat(r1.xy), asfloat(r0.xy)));
    r0.zw = asuint((asfloat(v0.xy) * asfloat(asuint((float2(_ScreenSize.z, _ScreenSize.w))))));
    r1.xyzw = asuint(_DepthMipChain.SampleLevel(sampler_DepthMipChain, asfloat(r0.zw), asfloat(0x00000000u)).xyzw);
    r1.y = v0.z;
    r0.zw = asuint(mad(asfloat(asuint((float2(_ZBufferParams.z, _ZBufferParams.z)))), asfloat(r1.xy), asfloat(asuint((float2(_ZBufferParams.w, _ZBufferParams.w))))));
    r0.zw = asuint((asfloat(uint2(0x3f800000u, 0x3f800000u)) / asfloat(r0.zw)));
    r0.z = asuint((asfloat((r0.w ^ 0x80000000u)) + asfloat(r0.z)));
    r0.z = ((asfloat(r0.z) < asfloat(0x00000000u)) ? 0xffffffffu : 0u);
    if (r0.z != 0u) discard;
    r1.xyzw = asuint(_DTTex.SampleBias(sampler_DTTex, asfloat(r0.xy), asfloat(asuint((_GlobalMipBias)))).xyzw);
    r0.xyzw = asuint(_DTTex.SampleLevel(sampler_DTTex, asfloat(r0.xy), asfloat(0x00000000u)).xyzw);
    r0.xyz = asuint((asfloat((r1.xyz ^ 0x80000000u)) + asfloat(r0.xyz)));
    r0.xyz = asuint(mad(asfloat(asuint((float3(_DTTexUVMode, _DTTexUVMode, _DTTexUVMode)))), asfloat(r0.xyz), asfloat(r1.xyz)));
    r0.xy = asuint((asfloat(r0.xy) + asfloat(uint2(0xbefefeffu, 0xbefefeffu))));
    r0.xy = asuint((asfloat(r0.xy) * asfloat(v1.zz)));
    r1.xy = asuint((asfloat(r0.xy) + asfloat(r0.xy)));
    r0.x = asuint((asfloat(r0.z) * asfloat(asuint((_SeparateRGBIntensity)))));
    r1.z = asuint((asfloat(r0.x) * asfloat(v1.z)));
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_DistortionMode)))) ? 0xffffffffu : 0u);
    r1.w = 0x3f800000u;
    o0.xyzw = ((r0.xxxx != 0u) ? uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u) : r1.xyzw);
    o1.xyzw = (r0.xxxx & r1.xyzw);
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    return result;
}

            #endif
            ENDHLSL
        }
    }
}
