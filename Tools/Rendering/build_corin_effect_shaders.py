import json
import re
from pathlib import Path


ROOT = Path(r"D:\ZZZ_Dump\output\corin_replication")
SELECTION = ROOT / "20260930_effect_shader_selected_programs_v1.json"
RECOVERY = ROOT / "20260930_effect_shader_selected_recovery_v14"
FINAL_RECOVERY = ROOT / "20260930_effect_shader_selected_recovery_v16"
METADATA_DIRECTORIES = [
    ROOT / "20260930_effect_shader_metadata_v37",
    ROOT / "20260930_effect_shader_metadata_v41",
]
PROJECT = Path(r"D:\Unity_Project_1\3C\3cDemo\Client\3C_Client\Assets\Render\ZZZRestored\Generated\Effect")
REPOSITORY = Path(r"D:\Unity_Project_1\3C")


def split_source(text):
    match = re.search(r"(?:ShaderOutput|void)\s+main\s*\(", text)
    if not match:
        raise ValueError("entrypoint missing")
    return text[:match.start()], text[match.start():]


def bind_samplers(text):
    textures = {}
    for match in re.finditer(
        r"\bTexture2D(?:Array|MS)?(?:<[^>]+>)?\s+(\w+)\s*:\s*register\(t(\d+)\)", text):
        textures[int(match[2])] = match[1]
    for match in re.finditer(
        r"\bSampler(?:State|ComparisonState)\s+(\w+)\s*:\s*register\(s(\d+)\)", text):
        source_name = match[1]
        texture_name = textures.get(int(match[2]))
        texture_alias = texture_name.lstrip("_") if texture_name else ""
        target_name = f"sampler_{texture_alias}" if texture_alias else f"{source_name}_linear_repeat"
        text = re.sub(rf"\b{re.escape(source_name)}\b", target_name, text)
    text = re.sub(
        r"(\bSampler(?:State|ComparisonState)\s+sampler_\w+)\s*:\s*register\([^)]+\)",
        r"\1",
        text,
    )
    return text


def load_stage(directory, entry, suffix, entrypoint):
    text = bind_samplers((directory / f"entry{entry}.named.hlsl").read_text(encoding="utf-8"))
    prefix, body = split_source(text)
    prefix = prefix.replace("struct ShaderOutput", f"struct {suffix}", 1)
    body = body.replace("ShaderOutput", suffix)
    body = re.sub(r"\bmain\s*\(", entrypoint + "(", body, count=1)
    return prefix, body


def render_state(path):
    state = json.loads(path.read_text(encoding="utf-8-sig"))
    culling = {0: "Off", 1: "Front", 2: "Back"}.get(state.get("m_Culling"), "Back")
    blend = {0: "Zero", 1: "One", 2: "DstColor", 3: "SrcColor", 4: "OneMinusDstColor", 5: "SrcAlpha",
             6: "OneMinusSrcColor", 7: "DstAlpha", 8: "OneMinusDstAlpha", 9: "SrcAlphaSaturate",
             10: "OneMinusSrcAlpha"}
    ztest = {1: "Less", 2: "Equal", 3: "LEqual", 4: "LEqual", 5: "Greater", 6: "NotEqual", 7: "GEqual", 8: "Always"}
    blend_op = {0: "Add", 1: "Subtract", 2: "RevSubtract", 3: "Min", 4: "Max"}
    return (f"            Blend {blend.get(state.get('m_SrcBlend'), 'One')} {blend.get(state.get('m_DstBlend'), 'Zero')}\n"
            f"            BlendOp {blend_op.get(state.get('m_BlendOp'), 'Add')}\n"
            f"            ZTest {ztest.get(state.get('m_ZTest'), 'LEqual')}\n"
            f"            ZWrite {'On' if state.get('m_ZWrite') else 'Off'}\n"
            f"            Cull {culling}\n")


def build_shader(name, item, source):
    shader_path = PROJECT / (re.sub(r"[\s\\/:*?\"<>|]", "_", name) + ".shader")
    metadata = None
    safe_name = re.sub(r"[\s\\/:*?\"<>|]", "_", name)
    for directory in METADATA_DIRECTORIES:
        candidate = directory / f"{name}.json"
        if candidate.exists():
            metadata = json.loads(candidate.read_text(encoding="utf-8-sig"))
            break
        candidate = directory / f"{safe_name}.json"
        if candidate.exists():
            metadata = json.loads(candidate.read_text(encoding="utf-8-sig"))
            break
    if metadata is None:
        raise FileNotFoundError(name)
    parsed = metadata["m_ParsedForm"]
    shader_name = parsed["m_Name"]
    property_lines = []
    for prop in parsed["m_PropInfo"]["m_Props"]:
        prop_type = prop["m_Type"]
        prop_type = prop_type["value__"] if isinstance(prop_type, dict) else prop_type
        default = prop["m_DefValue"]
        if prop_type == 0:
            value = "(" + ", ".join(str(float(value)) for value in default) + ")"
            declaration = "Color"
        elif prop_type == 1:
            value = "(" + ", ".join(str(float(value)) for value in default) + ")"
            declaration = "Vector"
        elif prop_type == 4:
            texture_default = prop["m_DefTexture"]["m_DefaultName"] or "white"
            value = f'"{texture_default}" {{}}'
            declaration = "2D"
        else:
            value = str(float(default[0]))
            declaration = "Float"
        property_lines.append(f"        {prop['m_Name']} (\"{prop['m_Description']}\", {declaration}) = {value}")
    passes = []
    for index, selected in enumerate(item["passes"]):
        recovery_dir = FINAL_RECOVERY / name
        if not recovery_dir.exists():
            recovery_dir = RECOVERY / name
        vendor_prefix, vertex = load_stage(
            recovery_dir, selected["progVertex"], "CorinVertexOut", "CorinVertex")
        pixel_prefix, pixel = load_stage(
            recovery_dir, selected["progFragment"], "CorinFragmentOut", "CorinPixel")
        target = "5.0" if "5_0" in (recovery_dir / f"entry{selected['progVertex']}.named.hlsl").read_text(encoding="utf-8")[:512] else "4.0"
        passes.append(
            f"        Pass\n        {{\n"
            f"            Name \"{selected['name']}\"\n"
            f"{render_state(recovery_dir / f'entry{selected[chr(112)+chr(114)+chr(111)+chr(103)+chr(86)+chr(101)+chr(114)+chr(116)+chr(101)+chr(120)]}.render_state.json')}"
            f"            HLSLPROGRAM\n"
            f"            #pragma target {target}\n"
            f"            #pragma vertex CorinVertex\n"
            f"            #pragma fragment CorinPixel\n"
            f"            #if defined(SHADER_STAGE_VERTEX)\n"
            f"{vendor_prefix}\n{vertex}\n"
            f"            #elif defined(SHADER_STAGE_FRAGMENT)\n"
            f"{pixel_prefix}\n{pixel}\n"
            f"            #endif\n"
            f"            ENDHLSL\n        }}")
    content = f"Shader \"{shader_name}\"\n{{\n    Properties\n    {{\n" + "\n".join(property_lines) + "\n    }\n    SubShader\n    {\n"
    content += "\n".join(passes) + "\n    }\n}\n"
    shader_path.write_text(content, encoding="utf-8", newline="\n")


def main():
    PROJECT.mkdir(parents=True, exist_ok=True)
    selection = json.loads(SELECTION.read_text(encoding="utf-8-sig"))
    recovery = {}
    sources = {}
    for name, item in selection.items():
        directory = RECOVERY / name
        sources[name] = {str(index): str(directory) for index in range(len(item["passes"]))}
    for name, item in selection.items():
        build_shader(name, item, sources)


if __name__ == "__main__":
    main()
