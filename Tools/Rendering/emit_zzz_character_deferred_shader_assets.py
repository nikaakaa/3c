import argparse
import json
import re
from pathlib import Path

from recover_zzz_shader import sha256


def adapt(source, samplers):
    source = re.sub(r"cbuffer \w+\s*:\s*register\(b\d+\)\s*\{(.*?)\}",
                    lambda match: re.sub(r"\s*:\s*packoffset\([^)]*\)", "", match[1]), source, flags=re.S)
    for original, replacement in samplers.items():
        source = re.sub(rf"SamplerState {original}\s*:\s*register\(s\d+\);", f"SamplerState {replacement};", source)
        source = re.sub(rf"\b{original}\b", replacement, source)
    return source


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--recovery", type=Path, required=True)
    parser.add_argument("--output-root", type=Path, required=True)
    args = parser.parse_args()
    deferred = adapt((args.recovery / "entry6.named.hlsl").read_text(encoding="utf-8"),
                     {"ZZZSampler_85": "sampler_InternalLut_Char", "ZZZSampler_84": "sampler_CameraDepthTexture"})
    post = adapt((args.recovery / "entry4664.named.hlsl").read_text(encoding="utf-8"),
                 {"ZZZSampler_84": "sampler_InputTex"})
    vertex = """#include \"Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl\"
struct ZZZFullscreenOutput
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};
ZZZFullscreenOutput ZZZFullscreenVertex(uint vertexID : SV_VertexID)
{
    ZZZFullscreenOutput result;
    result.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
    result.uv = GetFullScreenTriangleTexCoord(vertexID);
    return result;
}
"""
    wrapper = """Shader \"ZZZ/Restored/CharacterDeferredComposite\"
{
    SubShader
    {
        Tags { \"RenderPipeline\"=\"UniversalPipeline\" }
        Pass
        {
            Name \"DeferredShadingForCharacter\"
            Cull Off
            ZTest Always
            ZWrite Off
            Stencil
            {
                Ref 128
                ReadMask 128
                WriteMask 0
                Comp Equal
                Pass Keep
            }
            HLSLPROGRAM
            #pragma target 5.0
            #pragma only_renderers d3d11
            #pragma vertex ZZZFullscreenVertex
            #pragma fragment main
            #if defined(SHADER_STAGE_VERTEX)
                #include \"FullscreenVertex.hlsl\"
            #elif defined(SHADER_STAGE_FRAGMENT)
                #include \"OriginalDeferredShading.hlsl\"
            #endif
            ENDHLSL
        }
        Pass
        {
            Name \"CharacterPostProcess\"
            Cull Off
            ZTest Always
            ZWrite Off
            Stencil
            {
                Ref 128
                ReadMask 128
                WriteMask 0
                Comp Equal
                Pass Keep
            }
            HLSLPROGRAM
            #pragma target 5.0
            #pragma only_renderers d3d11
            #pragma vertex ZZZFullscreenVertex
            #pragma fragment main
            #if defined(SHADER_STAGE_VERTEX)
                #include \"FullscreenVertex.hlsl\"
            #elif defined(SHADER_STAGE_FRAGMENT)
                #include \"OriginalCharacterPostProcess.hlsl\"
            #endif
            ENDHLSL
        }
    }
}
"""
    report = {
        "schemaVersion": 1,
        "source": str((args.recovery / "recovery.json").resolve()),
        "sourceSha256": sha256((args.recovery / "recovery.json").read_bytes()),
        "entries": [6, 4664],
        "passes": ["DeferredShadingForCharacter", "CharacterPostProcess"],
        "runtimeBound": False
    }
    files = {
        args.output_root / "FullscreenVertex.hlsl": vertex,
        args.output_root / "OriginalDeferredShading.hlsl": deferred,
        args.output_root / "OriginalCharacterPostProcess.hlsl": post,
        args.output_root / "CharacterDeferredComposite.shader": wrapper,
        args.output_root / "CharacterDeferredCompositeSource.json": json.dumps(report, ensure_ascii=False, indent=2) + "\n"
    }
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
