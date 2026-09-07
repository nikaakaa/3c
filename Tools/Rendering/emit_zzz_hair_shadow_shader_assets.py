import argparse
import json
from pathlib import Path

from emit_zzz_character_shader_assets import adapt_stage, pass_source, property_line
from recover_zzz_shader import sha256


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--shader-json", type=Path, required=True)
    parser.add_argument("--recovery", type=Path, required=True)
    parser.add_argument("--output-root", type=Path, required=True)
    args = parser.parse_args()
    shader = json.loads(args.shader_json.read_text(encoding="utf-8-sig"))
    root = args.output_root
    vertex = adapt_stage((args.recovery / "entry0.named.hlsl").read_text(encoding="utf-8"), 0)
    fragment = adapt_stage((args.recovery / "entry6.named.hlsl").read_text(encoding="utf-8"), 6)
    properties = "\n".join(property_line(prop) for prop in shader["m_ParsedForm"]["m_PropInfo"]["m_Props"])
    state = json.loads((args.recovery / "entry0.render_state.json").read_text(encoding="utf-8"))
    wrapper = f"""Shader \"ZZZ/Restored/NapStencilShadowCaster\"
{{
    Properties
    {{
{properties}
    }}
    SubShader
    {{
        Tags {{ \"RenderPipeline\"=\"UniversalPipeline\" \"RenderType\"=\"Opaque\" \"Queue\"=\"Geometry\" }}
{pass_source("StencilShadowCaster", "StencilShadowCaster", 0, 6, state)}    }}
}}
"""
    report = {
        "schemaVersion": 1,
        "shader": shader["m_ParsedForm"]["m_Name"],
        "source": str(args.shader_json.resolve()),
        "sourceSha256": sha256(args.shader_json.read_bytes()),
        "recovery": str((args.recovery / "recovery.json").resolve()),
        "recoverySha256": sha256((args.recovery / "recovery.json").read_bytes()),
        "entries": [0, 6],
        "runtimeBound": False
    }
    files = {
        root / "OriginalStage0.hlsl": vertex,
        root / "OriginalStage6.hlsl": fragment,
        root / "NapStencilShadowCaster.shader": wrapper,
        root / "NapStencilShadowCasterSource.json": json.dumps(report, ensure_ascii=False, indent=2) + "\n"
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
