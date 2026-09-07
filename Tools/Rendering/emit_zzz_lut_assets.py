import argparse
import json
import re
from pathlib import Path


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--recovery", type=Path, required=True)
    parser.add_argument("--capture", type=Path, required=True)
    args = parser.parse_args()
    root = "3cDemo/Client/3C_Client/Assets/Render/ZZZRestored/Generated/CharacterLut"
    capture = json.loads(args.capture.read_text(encoding="utf-8"))
    if capture["keyword"] != "_TONEMAP_CUSTOM" or capture["lutSize"] != 32:
        raise ValueError("Unexpected character LUT contract")
    inputs = {key: capture[key] for key in ("schemaVersion", "name", "lutSize", "keyword", "userLutEnabled",
                                          "curveBindings")}
    inputs["parameters"] = [{"name": row["name"], "values": row["values"]} for row in capture["parameters"]]
    inputs["textures"] = [{key: row[key] for key in ("name", "width", "height", "format", "filterMode", "wrapMode",
                                                    "rawBase64", "sha256")} for row in capture["textures"]]
    inputs["sourceCapture"] = str(args.capture.resolve())
    files = {root + "/CorinSourceLut.json": json.dumps(inputs, ensure_ascii=False, indent=2) + "\n"}
    for entry, stage in ((3, "Vertex"), (11, "Fragment")):
        source = (args.recovery / f"entry{entry}.named.hlsl").read_text(encoding="utf-8")
        source = re.sub(r"cbuffer \w+\s*:\s*register\(b\d+\)\s*\{(.*?)\}",
                        lambda match: re.sub(r"\s*:\s*packoffset\([^)]*\)", "", match[1]), source, flags=re.S)
        source = source.replace("ZZZSampler_85", "zzz_linear_clamp_sampler")
        source = source.replace("SamplerState s1 :", "SamplerState sampler_UserLut :").replace("SampleLevel(s1,", "SampleLevel(sampler_UserLut,")
        files[root + f"/OriginalLut{stage}.hlsl"] = source
    properties = []
    for parameter in inputs["parameters"]:
        name, values = parameter["name"], parameter["values"]
        kind = "Float" if len(values) == 1 else "Vector"
        value = format(values[0], ".9g") if len(values) == 1 else "(" + ",".join(format(v, ".9g") for v in values) + ")"
        properties.append(f'        [HideInInspector] {name}("{name}", {kind}) = {value}')
    properties.append('        [HideInInspector] _UserLut_Params("_UserLut_Params", Vector) = (0,0,0,0)')
    for binding in inputs["curveBindings"]:
        name = binding["name"]
        properties.append(f'        [HideInInspector] {name}("{name}", 2D) = "" {{}}')
    files[root + "/CharacterLutHDR.shader"] = '''Shader "Hidden/ZZZ/Restored/CharacterLutHDR"
{
    Properties
    {
''' + "\n".join(properties) + '''
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Name "LutBuilderHdr"
            Cull Off
            ZWrite Off
            ZTest Always
            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11
            #pragma vertex main
            #pragma fragment main
            #if defined(SHADER_STAGE_VERTEX)
                #include "OriginalLutVertex.hlsl"
            #elif defined(SHADER_STAGE_FRAGMENT)
                #include "OriginalLutFragment.hlsl"
            #endif
            ENDHLSL
        }
    }
}
'''
    patch = ["*** Begin Patch"]
    for path, content in files.items():
        if Path(path).exists():
            raise ValueError(f"Generated target already exists: {path}")
        patch.append("*** Add File: " + path)
        patch.extend("+" + line for line in content.splitlines())
    patch.append("*** End Patch")
    print("\n".join(patch))


if __name__ == "__main__":
    main()
