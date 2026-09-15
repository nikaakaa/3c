using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZZZRestored
{
    /// <summary>
    /// Hand-tuning GUI for the restored Nap character shaders.
    /// Groups the full official property surface into tabs, with a zone selector:
    /// zone 1 = unsuffixed properties, zones 2-5 = suffixed, F = Fallback (MatCap family).
    /// "All" shows every zone's row. Missing properties (Face/Eye shader variants) are skipped.
    /// </summary>
    public class NapAvatarStandardGUI : ShaderGUI
    {
        // ---------------- tab definitions ----------------
        // Each entry: (property base name, label override or null)
        static readonly string[][] kTabs =
        {
            // 0 色带与明暗
            new[] {
                "_ShadowColor", "_ShallowColor", "_AlbedoSmoothness", "_AOParameters", "_DarkAOScale",
                "_PowMin", "_PowMax", "_CharacterRampTex", "_RampTexParams0", "_RampTexParams1",
                "_RampSource", "_BrightMultiplier", "_VolumeLutScale", "_LocalIndirectColor",
                "_ShadowColorFadeByZ", "_ShadowNormalBias", "_MaterialNum"
            },
            // 1 高光
            new[] {
                "_ToonSpecular", "_SpecularRange", "_ShapeSoftness", "_HighlightShape",
                "_SpecularColor", "_SpecIntensity", "_Anisotropy", "_Glossiness", "_Metallic",
                "_ShiftAngle", "_Softness", "_ColorA", "_ColorB", "_LerpPosition",
                "_Override2Tone", "_Override2ToneMultiplyAlbedo", "_Override2ToneBlendMode",
                "_LightSource", "_PointPosition", "_PointSpace"
            },
            // 2 MatCap
            new[] {
                "_MatCap", "_MatCapTex", "_MatCapTexID", "_MatCapColorTint", "_MatCapColorBurst",
                "_MatCapAlphaBurst", "_MatCapBlendMode", "_MatCapUSpeed", "_MatCapVSpeed",
                "_MatCapRefract", "_RefractDepth", "_RefractParam", "_UseMatCapMask",
                "_MatCapTexFallback", "_MatCapColorTintFallback", "_MatCapColorBurstFallback",
                "_MatCapAlphaBurstFallback", "_MatCapUSpeedFallback", "_MatCapVSpeedFallback",
                "_MatCapBlendModeFallback", "_MatCapRefractFallback", "_RefractDepthFallback", "_RefractParamFallback"
            },
            // 3 Rim
            new[] {
                "_RimGlow", "_RimGlowLightColor", "_RimGlowShadowColor",
                "_OverrideRimGlow", "_OverrideRimGlowColor", "_OverrideRimGlowTexFX",
                "_OverrideRimGlowSpeed", "_OverrideRimGlowUseUV2", "_OverrideRimGlowMode"
            },
            // 4 描边
            new[] {
                "_Outline", "_OutlineWidth", "_OutlineColor", "_ModelSize", "_MaxOutlineZOffset",
                "_VertexOffset", "_OverrideOutline", "_OverrideOutlineColor", "_OverrideOutlineTex",
                "_OverrideOutlineSpeed", "_OverrideOutlineUseUV2", "_OutlineFX", "_OutlineColorFX", "_OutlineWidthFX"
            },
            // 5 纹理输入
            new[] {
                "_MainTex", "_Color", "_LightTex", "_OtherDataTex", "_OtherDataTex2",
                "_TransmissionTexture", "_ThreadMap", "_BumpScale",
                "_UseOverlayTex", "_OverlayTexScale",
                "_UseChannelMixer", "_ChannelMixTex", "_ChannelMixerUsingUV4",
                "_RChannelColor", "_GChannelColor", "_BChannelColor", "_AChannelColor",
                "_SymmetryUV", "_SilkPackedParams0", "_SilkFresnelColorFront", "_SilkFresnelColorEdge"
            },
            // 6 硬光/软光/自发光
            new[] {
                "_HardLight", "_HardLightWidth", "_HardLightColor",
                "_SoftLight", "_SoftLightWidth", "_SoftLightColor",
                "_Emission", "_EmissionColor",
                "_SecondaryEmission", "_SecondaryEmissionTex", "_SecondaryEmissionTexSpeed",
                "_SecondaryEmissionTexRotation", "_SecondaryEmissionColor", "_SecondaryEmissionUseUV2",
                "_SecondaryEmissionChannel", "_MultiplyAlbedo",
                "_SecondaryEmissionMaskTex", "_SecondaryEmissionMaskChannel",
                "_SpecialWeaponEmission", "_SpecialWeaponEmissionTex", "_SpecialWeaponEmissionTexSpeed",
                "_SpecialWeaponEmissionColor", "_SpecialWeaponEmissionMaskTex", "_SpecialWeaponEmissionColor2",
                "_SpecialWeaponMergeParam01", "_SpecialWeaponMergeParam02", "_SpecialWeaponMergeParam03"
            },
            // 7 光照输入
            new[] {
                "_ReceiveShadows", "_ReceiveAddShadows", "_CharacterMainLightData",
                "_OverrideMainLightParam", "_OverrideMainLightColor",
                "_AdditionalLightIntensity", "_PerObjectShadowData", "_PerObjectShadowIntensity",
                "_ShadowZOffset", "_ShadowZOffsetWholeMesh",
                "_LightDirectionFromCamera", "_CameraToLightRadian",
                "_HeadMatrixWS2OS0", "_HeadMatrixWS2OS1", "_HeadMatrixWS2OS2", "_HeadMatrixWS2OS3"
            },
            // 8 脸部/眨眼
            new[] {
                "_Blink", "_BlinkFrequency", "_BlinkOpacity", "_SkinMatId",
                "_UseFaceShadowPoint", "_FixedLightDirection", "_LightMapUVFlip",
                "_NoseLineHoriDisp", "_NoseLineLkDnDisp", "_NoseLineScale", "_NoseSpecularScale",
                "_RampTexParams2", "_CharacterMainLightData1", "_UseAlpha"
            },
            // 9 眼睛
            new[] {
                "_EyeColorMap", "_UseVertexMaterialID", "_UseFaceShadowPoint",
                "_FixedLightDirection", "_LightMapUVFlip"
            },
            // 10 特效/状态
            new[] {
                "_Glitch", "_ScreenImage", "_ScreenScale", "_MultiplySrcColor", "_ScreenColor",
                "_ScreenTex", "_ScreenTexRotation", "_ScreenTexRotationAxis", "_ScreenMask",
                "_ScreenMaskUV", "_UseInvSecondaryEmissionMask", "_ScreenImageUvMove",
                "_BlockMaskTex", "_BlockColorA", "_BlockColorB", "_BlockColorC", "_BlockColorD", "_BlockMoveSpeed",
                "_Transition", "_TransitionCompletion", "_TransitionTex", "_TransitionWidth", "_TransitionColor",
                "_AbnormalProperty", "_AbnormalPropertyElectro", "_AbnormalPropertyBurn", "_AbnormalPropertyFreeze",
                "_PropertyType", "_PropertyColor", "_FresnelColor", "_DetailColor",
                "_PropertyTexUseUV2", "_PropertyMaskUseUV2", "_PropertyMask2UseUV2", "_PropertyNormalUseUV2",
                "_PropertyMaskChannel", "_PropertyMask2Channel",
                "_PropertyTexUVSpeed", "_PropertyTexUVFlipSpeed", "_PropertyMaskUVSpeed",
                "_PropertyMaskUVFlipSpeed", "_PropertyMask2UVSpeed", "_PropertyNormalUVSpeed",
                "_FresnelWidth", "_FresnelMaskWidth", "_FresnelFlashing", "_DecolorizationContrast",
                "_DitherAlpha", "_UseDitherCenter", "_DitherCenter", "_CenterMinAlpha", "_FxUVDitherValue",
                "_Override", "_OverrideColor", "_MultiplyAlbedoForCGLighting", "_ColorOverrideAlbedo"
            },
            // 11 渲染状态
            new[] {
                "_Cull", "_DoubleSided", "_ZWrite", "_ZTest", "_AlphaClip", "_Cutoff",
                "_BlendSrcFactor", "_BlendDstFactor", "_BlendSrcFactorMV", "_BlendDstFactorMV",
                "_ZWritePreZ", "_ZTestPreZ", "_CharacterStencil", "_StencilReadMask", "_StencilComFunc",
                "_StencilRef", "_StencilRefShadow", "_StencilWriteMask", "_StencilWriteMaskShadow",
                "_CharacterHalfResAlphaDstBlend", "_CharacterHalfResAlphaSrcBlend",
                "_CharacterHalfResDstBlend", "_CharacterHalfResSrcBlend",
                "_SrcBlend", "_DstBlend", "_AlphaSrcBlend", "_AlphaDstBlend", "_Surface", "_UseAlpha",
                "_UseClipPlane", "_ClipPlane", "_PlaneClipReverse", "_ClipPlaneXZ", "_ReversePlaneXZ", "_PlaneXZScale",
                "_VertexStretch", "_StretchDirection", "_StretchMask", "_MaskRChannelUVSpeed",
                "_MaskTexFactor", "_NormalThreshold", "_StretchDistance", "_StretchVector",
                "_StretchToPoint", "_StretchPercentage",
                "_IgnoreTimeScale", "_MiddlePointPosition", "_ShadowDitherOff",
                "_MarkAsIgnisFatuusMask", "_MarkAsGhostMask", "_MarkAsVfxMask", "_CharacterSimplify",
                "_LayerIDColor", "_RenderType", "_PropertyType"
            }
        };

        static readonly string[] kTabLabels =
        {
            "色带与明暗", "高光", "MatCap", "Rim", "描边", "纹理输入",
            "硬光/发光", "光照输入", "脸部", "眼睛", "特效/状态", "渲染状态"
        };

        // zone: 0=all, 1=base, 2..5, 6=Fallback
        static readonly string[] kZoneLabels = { "全部", "区1", "区2", "区3", "区4", "区5", "FB" };
        static readonly Dictionary<Object, int> s_Tab = new Dictionary<Object, int>();
        static readonly Dictionary<Object, int> s_Zone = new Dictionary<Object, int>();
        static bool s_ShowMissing;

        static string PropName(string baseName, int zone)
        {
            if (zone <= 1) return baseName;
            if (zone == 6) return baseName + "Fallback";
            return baseName + zone.ToString();
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            var material = (Material)materialEditor.target;

            // ---- MatCap keyword toggle (drives which stage variant the toon pass uses) ----
            EditorGUILayout.Space(2);
            var matcapOn = material.IsKeywordEnabled("_MATCAP_ON");
            var matcapNew = EditorGUILayout.Toggle(new GUIContent("MatCap 变体 (_MATCAP_ON)", "切换后重新序列化材质以选择对应 stage"), matcapOn);
            if (matcapNew != matcapOn)
            {
                if (matcapNew) material.EnableKeyword("_MATCAP_ON");
                else material.DisableKeyword("_MATCAP_ON");
                EditorUtility.SetDirty(material);
            }

            // ---- zone selector ----
            EditorGUILayout.Space(2);
            if (!s_Zone.ContainsKey(material)) s_Zone[material] = 1;
            s_Zone[material] = GUILayout.Toolbar(s_Zone[material], kZoneLabels);
            int zone = s_Zone[material];

            // ---- tab selector ----
            if (!s_Tab.ContainsKey(material)) s_Tab[material] = 0;
            s_Tab[material] = GUILayout.Toolbar(s_Tab[material], kTabLabels);
            EditorGUILayout.Space(4);

            var names = kTabs[s_Tab[material]];
            foreach (var baseName in names)
            {
                if (zone == 0)
                {
                    // draw every existing zone variant of this property
                    bool drewAny = false;
                    for (int z = 1; z <= 6; z++)
                    {
                        var p = FindProperty(PropName(baseName, z), props, false);
                        if (p != null)
                        {
                            DrawWithZoneTag(materialEditor, p, z);
                            drewAny = true;
                        }
                    }
                    if (!drewAny && s_ShowMissing)
                        MissingRow(baseName);
                }
                else
                {
                    var p = FindProperty(PropName(baseName, zone), props, false);
                    if (p != null) materialEditor.ShaderProperty(p, p.displayName);
                    else
                    {
                        // fall back to the base property (global, not zoned)
                        var g = FindProperty(baseName, props, false);
                        if (g != null) materialEditor.ShaderProperty(g, g.displayName);
                        else if (s_ShowMissing) MissingRow(baseName);
                    }
                }
            }

            EditorGUILayout.Space(6);
            s_ShowMissing = EditorGUILayout.ToggleLeft("显示缺失属性(灰色)", s_ShowMissing);
            materialEditor.RenderQueueField();
            materialEditor.EnableInstancingField();
        }

        static void DrawWithZoneTag(MaterialEditor editor, MaterialProperty p, int zone)
        {
            var label = zone == 1 ? p.displayName : $"{p.displayName}  [区{zone}]";
            editor.ShaderProperty(p, label);
        }

        static void MissingRow(string name)
        {
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.35f);
            EditorGUILayout.LabelField(name, "(此shader不含)");
            GUI.color = old;
        }
    }
}
