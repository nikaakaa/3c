import argparse
import json
import re
from pathlib import Path

from recover_zzz_shader import sha256


def format_number(value):
    return format(value, ".9g")


def property_line(prop):
    name = prop["m_Name"]
    value = prop["m_DefValue"]
    kind = prop["m_Type"]
    if kind == "Float":
        declaration = f"Float) = {format_number(value[0])}"
    elif kind == "Range":
        declaration = f"Range({format_number(value[1])},{format_number(value[2])})) = {format_number(value[0])}"
    elif kind in ("Color", "Vector"):
        declaration = f"{kind}) = ({','.join(format_number(item) for item in value)})"
    elif kind == "Texture":
        dimension = {"Tex2D": "2D", "Tex2DArray": "2DArray"}[prop["m_DefTexture"]["m_TexDim"]]
        declaration = f'{dimension}) = "{prop["m_DefTexture"]["m_DefaultName"]}" {{}}'
    else:
        raise ValueError(f"Unsupported property type {kind}")
    return f'        [HideInInspector] {name}("{name}", {declaration}'


def adapt_stage(source, entry):
    source = re.sub(r"cbuffer \w+\s*:\s*register\(b\d+\)\s*\{(.*?)\}",
                    lambda match: re.sub(r"\s*:\s*packoffset\([^)]*\)", "", match[1]), source, flags=re.S)
    source = source.replace("SamplerState ZZZSampler_1 : register(s0);", "SamplerState zzz_linear_repeat_sampler;")
    source = source.replace("SamplerComparisonState ZZZSampler_341 : register(s1);",
                            "SamplerComparisonState sampler_LinearClampCompare;")
    source = source.replace("ZZZSampler_1", "zzz_linear_repeat_sampler")
    source = source.replace("ZZZSampler_341", "sampler_LinearClampCompare")
    if entry == 2705:
        source = source.replace("SamplerState s2 : register(s2);", "SamplerState sampler_MatCap2DArray;")
        source = re.sub(r"\bs2\b", "sampler_MatCap2DArray", source)
    if entry == 4024:
        source = source.replace("SamplerState s0 : register(s0);", "SamplerState sampler_OverrideOutlineTex;")
        source = source.replace("SamplerState s1 : register(s1);", "SamplerState sampler_TransitionTex;")
        source = re.sub(r"\bs0\b", "sampler_OverrideOutlineTex", source)
        source = re.sub(r"\bs1\b", "sampler_TransitionTex", source)
    return source


def state_value(value, constants=None):
    constants = constants or {}
    if value.get("name") not in (None, "<noninit>"):
        return f'[{value["name"]}]'
    numeric = value["val"]
    return constants.get(int(numeric), format_number(numeric))


def stencil_block(state):
    values = [state["stencilRef"], state["stencilReadMask"], state["stencilWriteMask"],
              state["stencilOp"]["comp"], state["stencilOp"]["pass"],
              state["stencilOp"]["fail"], state["stencilOp"]["zFail"]]
    if all(value.get("name") in (None, "<noninit>") for value in values):
        return ""
    compares = {0: "Disabled", 1: "Never", 2: "Less", 3: "Equal", 4: "LEqual", 5: "Greater",
                6: "NotEqual", 7: "GEqual", 8: "Always"}
    operations = {0: "Keep", 1: "Zero", 2: "Replace", 3: "IncrSat", 4: "DecrSat",
                  5: "Invert", 6: "IncrWrap", 7: "DecrWrap"}
    return """            Stencil
            {
                Ref %s
                ReadMask %s
                WriteMask %s
                Comp %s
                Pass %s
                Fail %s
                ZFail %s
            }
""" % (state_value(values[0]), state_value(values[1]), state_value(values[2]),
         state_value(values[3], compares), state_value(values[4], operations),
         state_value(values[5], operations), state_value(values[6], operations))


def blend_lines(state):
    if not state["rtSeparateBlend"]:
        return ""
    factors = {0: "Zero", 1: "One", 2: "DstColor", 3: "SrcColor", 4: "OneMinusDstColor",
               5: "SrcAlpha", 6: "OneMinusSrcColor", 7: "DstAlpha", 8: "OneMinusDstAlpha",
               9: "SrcAlphaSaturate", 10: "OneMinusSrcAlpha"}
    lines = []
    for index, target in enumerate(state["rtBlend"]):
        values = [state_value(target[key], factors) for key in ("srcBlend", "destBlend", "srcBlendAlpha", "destBlendAlpha")]
        lines.append(f"            Blend {index} {values[0]} {values[1]}, {values[2]} {values[3]}")
    return "\n".join(lines) + "\n"


def pass_source(name, tag, vertex, fragment, state, conditional=False):
    if conditional:
        includes = f"""            #if defined(SHADER_STAGE_VERTEX)
                #if defined(_MATCAP_ON)
                    #include \"OriginalStage65.hlsl\"
                #else
                    #include \"OriginalStage60.hlsl\"
                #endif
            #elif defined(SHADER_STAGE_FRAGMENT)
                #if defined(_MATCAP_ON)
                    #include \"OriginalStage2705.hlsl\"
                #else
                    #include \"OriginalStage2700.hlsl\"
                #endif
            #endif"""
        keyword = "            #pragma shader_feature_local _ _MATCAP_ON\n"
    else:
        includes = f"""            #if defined(SHADER_STAGE_VERTEX)
                #include \"OriginalStage{vertex}.hlsl\"
            #elif defined(SHADER_STAGE_FRAGMENT)
                #include \"OriginalStage{fragment}.hlsl\"
            #endif"""
        keyword = ""
    cull = state_value(state["culling"], {0: "Off", 1: "Front", 2: "Back"})
    ztest = state_value(state["zTest"], {0: "Disabled", 1: "Never", 2: "Less", 3: "Equal",
                                                 4: "LEqual", 5: "Greater", 6: "NotEqual", 7: "GEqual", 8: "Always"})
    zwrite = state_value(state["zWrite"], {0: "Off", 1: "On"})
    return f"""        Pass
        {{
            Name \"{name}\"
            Tags {{ \"LightMode\"=\"{tag}\" }}
            Cull {cull}
            ZTest {ztest}
            ZWrite {zwrite}
{blend_lines(state)}{stencil_block(state)}            HLSLPROGRAM
            #pragma target 5.0
            #pragma only_renderers d3d11
            #pragma vertex main
            #pragma fragment main
{keyword}{includes}
            ENDHLSL
        }}
"""


def depth_normals_source(vertex_source, fragment_source, vertex, fragment):
    vertex_parameters = re.search(r"ShaderOutput main\(([^\n]+)\)", vertex_source)[1]
    fragment_parameters = re.search(r"ShaderOutput main\(([^\n]+)\)", fragment_source)[1]
    vertex_arguments = ", ".join(re.findall(r"\b(input\d+)\s*:", vertex_parameters))
    fragment_arguments = ", ".join(re.findall(r"\b(input\d+)\s*:", fragment_parameters))
    return f'''#if defined(SHADER_STAGE_VERTEX)
#define main ZZZOriginalDepthVertex
#define ShaderOutput ZZZOriginalDepthVertexOutput
#include "OriginalStage{vertex}.hlsl"
#undef ShaderOutput
#undef main
struct ZZZDepthNormalsVertexOutput
{{
    ZZZOriginalDepthVertexOutput original;
    float3 normalWS : TEXCOORD15;
}};
ZZZDepthNormalsVertexOutput ZZZDepthNormalsVertex({vertex_parameters})
{{
    ZZZDepthNormalsVertexOutput result;
    result.original = ZZZOriginalDepthVertex({vertex_arguments});
    result.normalWS = normalize(mul(input1, (float3x3)unity_WorldToObject));
    return result;
}}
#elif defined(SHADER_STAGE_FRAGMENT)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#define main ZZZOriginalDepthFragment
#define ShaderOutput ZZZOriginalDepthFragmentOutput
#include "OriginalStage{fragment}.hlsl"
#undef ShaderOutput
#undef main
float4 ZZZDepthNormalsFragment(float3 normalWS : TEXCOORD15, {fragment_parameters}) : SV_Target0
{{
    ZZZOriginalDepthFragment({fragment_arguments});
    float3 normal = normalize(normalWS);
#if defined(_GBUFFER_NORMALS_OCT)
    return float4(PackFloat2To888(saturate(PackNormalOctQuadEncode(normal) * 0.5 + 0.5)), 0);
#else
    return float4(normal, 0);
#endif
}}
#endif
'''


def depth_normals_pass_source(state):
    cull = state_value(state["culling"], {0: "Off", 1: "Front", 2: "Back"})
    return f'''        Pass
        {{
            Name "DepthNormalsOnly"
            Tags {{ "LightMode"="DepthNormalsOnly" }}
            Cull {cull}
            ZTest LEqual
            ZWrite On
            HLSLPROGRAM
            #pragma target 5.0
            #pragma only_renderers d3d11
            #pragma vertex ZZZDepthNormalsVertex
            #pragma fragment ZZZDepthNormalsFragment
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "UrpDepthNormals.hlsl"
            ENDHLSL
        }}
'''


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--shader-json", type=Path, required=True)
    parser.add_argument("--recovery", type=Path, required=True)
    parser.add_argument("--output-root", default="3cDemo/Client/3C_Client/Assets/Render/ZZZRestored/Generated/Character")
    args = parser.parse_args()
    shader = json.loads(args.shader_json.read_text(encoding="utf-8-sig"))
    root = Path(args.output_root)
    entries = (0, 16, 24, 48, 60, 65, 2700, 2705, 4020, 4024)
    files = {}
    for entry in entries:
        source_path = args.recovery / f"entry{entry}.named.hlsl"
        files[root / f"OriginalStage{entry}.hlsl"] = adapt_stage(source_path.read_text(encoding="utf-8"), entry)
    properties = "\n".join(property_line(prop) for prop in shader["m_ParsedForm"]["m_PropInfo"]["m_Props"])
    states = {entry: json.loads((args.recovery / f"entry{entry}.render_state.json").read_text(encoding="utf-8"))
              for entry in (0, 24, 60, 4020)}
    files[root / "UrpDepthNormals.hlsl"] = depth_normals_source(
        files[root / "OriginalStage4020.hlsl"], files[root / "OriginalStage4024.hlsl"], 4020, 4024)
    wrapper = f"""Shader \"ZZZ/Restored/NapAvatarStandard\"
{{
    Properties
    {{
{properties}
    }}
    SubShader
    {{
        Tags {{ \"RenderPipeline\"=\"UniversalPipeline\" \"RenderType\"=\"Opaque\" \"Queue\"=\"Geometry\" }}
{pass_source("ShadowCaster", "ShadowCaster", 0, 16, states[0])}
{pass_source("CharacterOutlineDeferred", "CharacterOutlineDeferred", 24, 48, states[24])}
{pass_source("CharacterToonDeferred", "CharacterToonDeferred", 60, 2700, states[60], True)}
{pass_source("CharDepthOnly", "DepthOnly", 4020, 4024, states[4020])}
{depth_normals_pass_source(states[4020])}
    }}
}}
"""
    files[root / "NapAvatarStandard.shader"] = wrapper
    report = {
        "schemaVersion": 1,
        "shader": shader["m_ParsedForm"]["m_Name"],
        "source": str(args.shader_json.resolve()),
        "sourceSha256": sha256(args.shader_json.read_bytes()),
        "recovery": str((args.recovery / "recovery.json").resolve()),
        "recoverySha256": sha256((args.recovery / "recovery.json").read_bytes()),
        "entries": list(entries),
        "properties": len(shader["m_ParsedForm"]["m_PropInfo"]["m_Props"]),
        "runtimeBound": False,
        "bindingAdaptation": "Unity named uniforms",
        "samplers": {"1": "linear-repeat", "341": "linear-clamp-compare", "material": "texture-associated"}
    }
    files[root / "NapAvatarStandardSource.json"] = json.dumps(report, ensure_ascii=False, indent=2) + "\n"
    patch = ["*** Begin Patch"]
    for path, content in files.items():
        if path.exists():
            raise ValueError(f"Generated target already exists: {path}")
        patch.append("*** Add File: " + path.as_posix())
        patch.extend("+" + line for line in content.splitlines())
    patch.append("*** End Patch")
    print("\n".join(patch))


if __name__ == "__main__":
    main()
