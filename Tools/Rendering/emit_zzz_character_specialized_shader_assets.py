import argparse
import json
from pathlib import Path

from emit_zzz_character_shader_assets import adapt_stage, pass_source, property_line
from recover_zzz_shader import sha256


def specialized_stage(source, entry):
    source = adapt_stage(source, entry)
    source = source.replace("SamplerState ZZZSampler_85 : register(s0);", "SamplerState zzz_linear_clamp_sampler;")
    source = source.replace("ZZZSampler_85", "zzz_linear_clamp_sampler")
    if entry == 2946:
        source = source.replace("SamplerState s0 : register(s0);", "SamplerState sampler_OverrideOutlineTex;")
        source = source.replace("SamplerState s1 : register(s1);", "SamplerState sampler_TransitionTex;")
        source = source.replace("s0,", "sampler_OverrideOutlineTex,")
        source = source.replace("s1,", "sampler_TransitionTex,")
    return source


def shader_source(name, properties, passes):
    return f"""Shader \"ZZZ/Restored/{name}\"
{{
    Properties
    {{
{properties}
    }}
    SubShader
    {{
        Tags {{ \"RenderPipeline\"=\"UniversalPipeline\" \"RenderType\"=\"Opaque\" \"Queue\"=\"Geometry\" }}
{''.join(passes)}    }}
}}
"""


def generate_face(shader, recovery, root):
    entries = (0, 8, 12, 44, 60, 1980, 2940, 2946)
    files = {}
    face_root = root / "Face"
    for entry in entries:
        source = (recovery / f"entry{entry}.named.hlsl").read_text(encoding="utf-8")
        files[face_root / f"OriginalStage{entry}.hlsl"] = specialized_stage(source, entry)
    states = [row["m_State"] for row in shader["m_ParsedForm"]["m_SubShaders"][0]["m_Passes"]]
    passes = [
        pass_source("ShadowCaster", "ShadowCaster", 0, 8, states[0]),
        pass_source("FaceOutlineDeferred", "FaceOutlineDeferred", 12, 44, states[1]),
        pass_source("FaceToonDeferred", "FaceToonDeferred", 60, 1980, states[2]),
        pass_source("FaceToonDeferredWithStencilShadow", "FaceToonDeferredWithStencilShadow", 60, 1980, states[3]),
        pass_source("CharDepthOnly", "CharDepthOnly", 2940, 2946, states[4])
    ]
    properties = "\n".join(property_line(prop) for prop in shader["m_ParsedForm"]["m_PropInfo"]["m_Props"])
    files[face_root / "NapAvatarStandardFace.shader"] = shader_source("NapAvatarStandardFace", properties, passes)
    return files, entries


def generate_eye(shader, recovery, root):
    entries = (0, 480)
    files = {}
    eye_root = root / "Eye"
    for entry in entries:
        source = (recovery / f"entry{entry}.named.hlsl").read_text(encoding="utf-8")
        files[eye_root / f"OriginalStage{entry}.hlsl"] = specialized_stage(source, entry)
    state = shader["m_ParsedForm"]["m_SubShaders"][0]["m_Passes"][0]["m_State"]
    properties = "\n".join(property_line(prop) for prop in shader["m_ParsedForm"]["m_PropInfo"]["m_Props"])
    files[eye_root / "NapAvatarStandardEye.shader"] = shader_source(
        "NapAvatarStandardEye", properties,
        [pass_source("CharacterOpaqueEye", "CharacterOpaqueEye", 0, 480, state)])
    return files, entries


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--face-shader-json", type=Path, required=True)
    parser.add_argument("--face-recovery", type=Path, required=True)
    parser.add_argument("--eye-shader-json", type=Path, required=True)
    parser.add_argument("--eye-recovery", type=Path, required=True)
    parser.add_argument("--output-root", type=Path,
                        default=Path("3cDemo/Client/3C_Client/Assets/Render/ZZZRestored/Generated/Character"))
    args = parser.parse_args()
    face = json.loads(args.face_shader_json.read_text(encoding="utf-8-sig"))
    eye = json.loads(args.eye_shader_json.read_text(encoding="utf-8-sig"))
    face_files, face_entries = generate_face(face, args.face_recovery, args.output_root)
    eye_files, eye_entries = generate_eye(eye, args.eye_recovery, args.output_root)
    files = {**face_files, **eye_files}
    report = {
        "schemaVersion": 1,
        "runtimeBound": False,
        "face": {"shader": face["m_ParsedForm"]["m_Name"], "entries": list(face_entries),
                 "source": str(args.face_shader_json.resolve()),
                 "sourceSha256": sha256(args.face_shader_json.read_bytes()),
                 "recoverySha256": sha256((args.face_recovery / "recovery.json").read_bytes())},
        "eye": {"shader": eye["m_ParsedForm"]["m_Name"], "entries": list(eye_entries),
                "source": str(args.eye_shader_json.resolve()),
                "sourceSha256": sha256(args.eye_shader_json.read_bytes()),
                "recoverySha256": sha256((args.eye_recovery / "recovery.json").read_bytes())}
    }
    files[args.output_root / "SpecializedShaderSource.json"] = json.dumps(report, ensure_ascii=False, indent=2) + "\n"
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
