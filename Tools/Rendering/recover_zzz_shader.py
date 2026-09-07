import argparse
import base64
import ctypes
import hashlib
import json
import re
import struct
from pathlib import Path


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def write_json(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def decompress_lz4(data, size):
    result = bytearray()
    cursor = 0
    while cursor < len(data):
        token = data[cursor]
        cursor += 1
        literal = token >> 4
        if literal == 15:
            while True:
                extra = data[cursor]
                cursor += 1
                literal += extra
                if extra != 255:
                    break
        result.extend(data[cursor:cursor + literal])
        cursor += literal
        if cursor == len(data):
            break
        offset = struct.unpack_from("<H", data, cursor)[0]
        cursor += 2
        length = (token & 15) + 4
        if (token & 15) == 15:
            while True:
                extra = data[cursor]
                cursor += 1
                length += extra
                if extra != 255:
                    break
        if offset == 0 or offset > len(result):
            raise ValueError("Invalid LZ4 match offset")
        block = bytes(result[-offset:])
        result.extend((block * ((length + offset - 1) // offset))[:length])
    if cursor != len(data) or len(result) != size:
        raise ValueError(f"LZ4 size mismatch: {len(result)} != {size}")
    return bytes(result)


def read_program(data):
    version, program_type = struct.unpack_from("<ii", data)
    cursor = 24 if version >= 201608170 else 20

    def strings():
        nonlocal cursor
        count = struct.unpack_from("<i", data, cursor)[0]
        cursor += 4
        if count < 0 or count > 1024:
            raise ValueError(f"Unexpected keyword count {count}")
        values = []
        for _ in range(count):
            length = struct.unpack_from("<i", data, cursor)[0]
            cursor += 4
            values.append(data[cursor:cursor + length].decode("utf-8"))
            cursor = (cursor + length + 3) & ~3
        return values

    keywords = strings()
    local = strings() if 201806140 <= version < 202012090 else []
    length = struct.unpack_from("<i", data, cursor)[0]
    cursor += 4
    code = data[cursor:cursor + length]
    if len(code) != length:
        raise ValueError("Truncated program")
    return {"version": version, "type": program_type, "keywords": keywords,
            "local_keywords": local, "program_sha256": sha256(code)}, code


def recover_material(raw_path, json_path):
    data = raw_path.read_bytes()
    exported = json.loads(json_path.read_text(encoding="utf-8-sig"))
    cursor = 0

    def same_float32(value, expected):
        if isinstance(value, dict):
            return set(value) == set(expected) and all(same_float32(v, expected[k]) for k, v in value.items())
        return struct.pack("<f", value) == struct.pack("<f", expected)

    def unpack(format):
        nonlocal cursor
        value = struct.unpack_from("<" + format, data, cursor)
        cursor += struct.calcsize("<" + format)
        return value[0] if len(value) == 1 else value

    def string():
        nonlocal cursor
        length = unpack("i")
        value = data[cursor:cursor + length].decode("utf-8")
        cursor = (cursor + length + 3) & ~3
        return value

    name = string()
    shader = {"m_FileID": unpack("i"), "m_PathID": unpack("q")}
    keyword_offset = cursor
    keywords = string()
    lightmap = unpack("I")
    instance_bytes = unpack("4B")
    queue = unpack("i")
    tags = {string(): string() for _ in range(unpack("i"))}
    disabled = [string() for _ in range(unpack("i"))]
    enabled_mask_offset = cursor
    enabled_mask = unpack("I")
    property_offset = cursor
    textures = {}
    for _ in range(unpack("i")):
        key = string()
        pointer = {"m_FileID": unpack("i"), "m_PathID": unpack("q")}
        textures[key] = {"m_Texture": pointer, "m_Scale": dict(zip("XY", unpack("2f"))),
                         "m_Offset": dict(zip("XY", unpack("2f")))}
    floats = {string(): unpack("f") for _ in range(unpack("i"))}
    colors = {string(): dict(zip("rgba", unpack("4f"))) for _ in range(unpack("i"))}
    properties = {"m_TexEnvs": textures, "m_Floats": floats, "m_Colors": colors}
    if name != exported["m_Name"]:
        raise ValueError("Material identity mismatch")
    for key, value in shader.items():
        if value != exported["m_Shader"][key]:
            raise ValueError("Material shader reference mismatch")
    for kind, values in properties.items():
        expected = exported["m_SavedProperties"][kind]
        if set(values) != set(expected):
            raise ValueError(f"Material property names mismatch: {kind}")
        for key, value in values.items():
            if kind == "m_TexEnvs":
                baseline = expected[key]
                valid = all(value["m_Texture"][field] == baseline["m_Texture"][field] for field in shader)
                valid = valid and same_float32(value["m_Scale"], baseline["m_Scale"]) and same_float32(value["m_Offset"], baseline["m_Offset"])
            else:
                valid = same_float32(value, expected[key])
            if not valid:
                raise ValueError(f"Material property value mismatch: {key}: raw={value}, json={expected[key]}")
    return {"m_Name": name, "m_Shader": shader, "m_ShaderKeywordsRaw": keywords,
            "m_ValidKeywords": keywords.split(), "m_CustomRenderQueue": queue,
            "m_LightmapFlags": lightmap, "m_EnableInstancingVariants": bool(instance_bytes[0]),
            "instancing_following_bytes_hex": bytes(instance_bytes[1:]).hex(),
            "stringTagMap": tags, "disabledShaderPasses": disabled, "enabledPassMask": enabled_mask,
            "m_SavedProperties": properties, "raw_path": str(raw_path.resolve()), "raw_sha256": sha256(data),
            "comparison_json_path": str(json_path.resolve()), "all_exported_properties_matched": True,
            "keyword_offset": keyword_offset, "enabled_pass_mask_offset": enabled_mask_offset,
            "property_sheet_offset": property_offset, "parsed_bytes": cursor, "raw_bytes": len(data),
            "trailing_bytes_hex": data[cursor:].hex(), "layout": "ZZZ Unity2019 Material with enabledPassMask"}


def get_dxbc(code):
    start = {0: 1, 1: 6, 2: 38}[code[0]]
    dxbc = code[start:]
    if dxbc[:4] != b"DXBC" or struct.unpack_from("<I", dxbc, 24)[0] != len(dxbc):
        raise ValueError("DXBC header/length mismatch")
    return dxbc


def decompile(dxbc, dll_path):
    dll = ctypes.CDLL(str(dll_path))
    fn = dll.Decompile
    fn.argtypes = [ctypes.c_void_p, ctypes.c_int, ctypes.POINTER(ctypes.c_void_p),
                   ctypes.POINTER(ctypes.c_int)]
    fn.restype = ctypes.c_int
    source = ctypes.create_string_buffer(dxbc)
    pointer = ctypes.c_void_p()
    length = ctypes.c_int()
    error = fn(source, len(dxbc), ctypes.byref(pointer), ctypes.byref(length))
    if error:
        raise RuntimeError(f"Decompiler returned {error}")
    try:
        return ctypes.string_at(pointer, length.value).decode("utf-8")
    finally:
        free = ctypes.WinDLL("kernel32").LocalFree
        free.argtypes = [ctypes.c_void_p]
        free.restype = ctypes.c_void_p
        free(pointer)


def blob_bytes(pointer):
    if not pointer:
        return b""
    vtable = ctypes.cast(pointer, ctypes.POINTER(ctypes.POINTER(ctypes.c_void_p))).contents
    get_pointer = ctypes.WINFUNCTYPE(ctypes.c_void_p, ctypes.c_void_p)(vtable[3])
    get_size = ctypes.WINFUNCTYPE(ctypes.c_size_t, ctypes.c_void_p)(vtable[4])
    release = ctypes.WINFUNCTYPE(ctypes.c_ulong, ctypes.c_void_p)(vtable[2])
    try:
        return ctypes.string_at(get_pointer(pointer), get_size(pointer))
    finally:
        release(pointer)


def compile_hlsl(source, profile):
    dll = ctypes.WinDLL("d3dcompiler_47.dll")
    fn = dll.D3DCompile
    fn.argtypes = [ctypes.c_void_p, ctypes.c_size_t, ctypes.c_char_p, ctypes.c_void_p,
                   ctypes.c_void_p, ctypes.c_char_p, ctypes.c_char_p, ctypes.c_uint,
                   ctypes.c_uint, ctypes.POINTER(ctypes.c_void_p), ctypes.POINTER(ctypes.c_void_p)]
    fn.restype = ctypes.c_long
    code, errors = ctypes.c_void_p(), ctypes.c_void_p()
    raw = source.encode("utf-8")
    result = fn(raw, len(raw), b"recovered.hlsl", None, None, b"main", profile.encode(),
                1 << 11, 0, ctypes.byref(code), ctypes.byref(errors))
    messages = blob_bytes(errors).decode("utf-8", errors="replace")
    compiled = blob_bytes(code)
    if result < 0:
        raise RuntimeError(messages)
    return compiled, messages


def disassemble(dxbc, flags=0):
    fn = ctypes.WinDLL("d3dcompiler_47.dll").D3DDisassemble
    fn.argtypes = [ctypes.c_void_p, ctypes.c_size_t, ctypes.c_uint, ctypes.c_char_p,
                   ctypes.POINTER(ctypes.c_void_p)]
    fn.restype = ctypes.c_long
    blob = ctypes.c_void_p()
    result = fn(dxbc, len(dxbc), flags, None, ctypes.byref(blob))
    if result < 0:
        raise RuntimeError(f"Disassembly failed: {result}")
    return blob_bytes(blob).decode("utf-8").rstrip("\x00")


def instructions(assembly):
    return [s.strip() for s in assembly.splitlines()
            if s.strip() and not s.lstrip().startswith(("//", "dcl_"))]


def chunks(dxbc):
    count = struct.unpack_from("<I", dxbc, 28)[0]
    result = {}
    for i in range(count):
        offset = struct.unpack_from("<I", dxbc, 32 + i * 4)[0]
        tag = dxbc[offset:offset + 4].decode("ascii")
        size = struct.unpack_from("<I", dxbc, offset + 4)[0]
        result[tag] = sha256(dxbc[offset + 8:offset + 8 + size])
    return result


def resolve_metadata(shader):
    entries = {}
    for subshader_index, subshader in enumerate(shader["m_ParsedForm"]["m_SubShaders"]):
        for pass_index, shader_pass in enumerate(subshader["m_Passes"]):
            names = {p["Value"]: p["Key"] for p in shader_pass["m_NameIndices"]}
            for stage in ["progVertex", "progFragment"]:
                for subprogram in shader_pass[stage]["m_SubPrograms"]:
                    if subprogram["m_GpuProgramType"] not in ("DX11VertexSM40", "DX11VertexSM50", "DX11PixelSM40", "DX11PixelSM50"):
                        continue
                    entries.setdefault(subprogram["m_BlobIndex"], []).append({
                        "subshader": subshader_index, "pass": pass_index,
                        "pass_name": shader_pass["m_State"]["m_Name"], "stage": stage,
                        "names": names, "subprogram": subprogram,
                        "state": shader_pass["m_State"]})
    return entries


def restore_names(source, metadata):
    names, program = metadata["names"], metadata["subprogram"]
    bindings = {p["m_NameIndex"]: p["m_Index"] for p in program["m_ConstantBufferBindings"]}
    words, declarations, dynamic_arrays, parameters = {}, [], {}, []
    for cb in program["m_ConstantBuffers"]:
        slot = bindings[cb["m_NameIndex"]]
        cb_name = names[cb["m_NameIndex"]]
        declarations.append(f"cbuffer {cb_name.replace('$', 'ZZZ')} : register(b{slot})\n{{")
        if cb["m_StructParams"]:
            raise ValueError("Struct constant layout needs explicit recovery")
        for kind, collection in [("matrix", cb["m_MatrixParams"]), ("vector", cb["m_VectorParams"])]:
            for p in collection:
                name, offset = names[p["m_NameIndex"]], p["m_Index"]
                count = p["m_ArraySize"] or 1
                register, component = divmod(offset, 16)
                component //= 4
                scalar_type = {0: "float", 1: "int", 2: "bool"}[p["m_Type"]]
                suffix = f"[{count}]" if p["m_ArraySize"] else ""
                packing = f"c{register}" + (f".{'xyzw'[component]}" if component else "")
                if kind == "matrix":
                    if p["m_RowCount"] != 4 or scalar_type != "float":
                        raise ValueError("Only evidenced float4x4 layout is supported")
                    declarations.append(f"    column_major float4x4 {name}{suffix} : packoffset({packing});")
                    for item in range(count):
                        matrix = name + (f"[{item}]" if p["m_ArraySize"] else "")
                        for col in range(4):
                            for row in range(4):
                                words[(slot, offset // 4 + item * 16 + col * 4 + row)] = f"{matrix}[{row}][{col}]"
                    if p["m_ArraySize"]:
                        dynamic_arrays.setdefault(slot, []).append((register, "matrix", name))
                else:
                    dim = p["m_Dim"]
                    vector_type = scalar_type + (str(dim) if dim > 1 else "")
                    declarations.append(f"    {vector_type} {name}{suffix} : packoffset({packing});")
                    if p["m_ArraySize"]:
                        if dim != 4 or scalar_type != "float":
                            raise ValueError("Dynamic vector arrays require evidenced float4 layout")
                        dynamic_arrays.setdefault(slot, []).append((register, "vector", name))
                    for item in range(count):
                        value = name + (f"[{item}]" if p["m_ArraySize"] else "")
                        for lane in range(dim):
                            expression = value + (f".{'xyzw'[lane]}" if dim > 1 else "")
                            if scalar_type != "float":
                                expression = f"asfloat({expression})"
                            words[(slot, offset // 4 + item * 4 + lane)] = expression
                parameters.append({"name": name, "buffer": cb_name, "slot": slot, "kind": kind, **p})
        declarations.append("}\n")

    source = re.sub(r"cbuffer cb\d+\s*:\s*register\(b\d+\)\s*\{[^}]+\}", "", source)
    helpers, accesses = {}, []

    def replace_cb(match):
        slot, index, swizzle = int(match[1]), match[2], match[3] or "xyzw"
        accesses.append(match[0])
        if index.isdigit():
            values = [words[(slot, int(index) * 4 + "xyzw".index(c))] for c in swizzle]
            return "(" + (values[0] if len(values) == 1 else f"float{len(values)}({', '.join(values)})") + ")"
        expression = re.fullmatch(r"([^+]+)\+(\d+)", index)
        if not expression:
            raise ValueError(f"Unresolved dynamic index expression {index}")
        constant = int(expression[2])
        arrays = [(base, kind, name) for base, kind, name in dynamic_arrays.get(slot, [])
                  if base <= constant < base + (4 if kind == "matrix" else 1)]
        if len(arrays) != 1:
            raise ValueError(f"Unresolved dynamic constant access {match[0]}")
        base, kind, name = arrays[0]
        if kind == "vector":
            return f"{name}[({index}) - {base}].{swizzle}"
        function = f"ZZZRead_{name}"
        helpers[function] = (f"float4 {function}(uint columnIndex)\n{{\n"
                             f"    uint packed = columnIndex - {base};\n"
                             "    uint matrixIndex = packed / 4;\n"
                             "    uint column = packed % 4;\n"
                             f"    return float4({', '.join(f'{name}[matrixIndex][{r}][column]' for r in range(4))});\n}}\n")
        return f"{function}({index}).{swizzle}"

    source = re.sub(r"cb(\d+)\[([^\]]+)\](?:\.([xyzw]+))?", replace_cb, source)
    resources = []
    for category in ["m_TextureParams", "m_BufferParams"]:
        for p in program[category]:
            old, name = f"t{p['m_Index']}", names[p["m_NameIndex"]]
            source = re.sub(rf"(?<!register\()\b{old}\b", name, source)
            source = re.sub(rf"\b{old}_t\b", name + "_Element", source)
            source = re.sub(rf"\b{old}_Element\b", name + "_Element", source)
            resources.append({"name": name, "category": category, **p})
    for p in program["m_Samplers"]:
        source = re.sub(rf"\bs{p['bindPoint']}_s\b", f"ZZZSampler_{p['sampler']}", source)
        source = re.sub(rf"(?<!register\()\bs{p['bindPoint']}\b", f"ZZZSampler_{p['sampler']}", source)
    main_position = re.search(r"(?:void|ShaderOutput) main\(", source).start()
    attribute = list(re.finditer(r"\[numthreads\([^\n]+\)\]\s*", source[:main_position]))
    if attribute:
        main_position = attribute[-1].start()
    source = source[:main_position] + "\n".join(helpers.values()) + source[main_position:]
    return "\n".join(declarations) + source, {
        "parameters": parameters, "resources": resources, "samplers": program["m_Samplers"],
        "constant_access_count": len(accesses), "dynamic_accesses": [s for s in accesses if not re.match(r"cb\d+\[\d+\]", s)]}


def main():
    from recover_dxbc_hlsl import opcode_counts, signature_records, translate

    parser = argparse.ArgumentParser()
    parser.add_argument("--shader-json", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--decompiler", type=Path)
    parser.add_argument("--entries", type=int, nargs="*", default=[])
    parser.add_argument("--material", type=Path, nargs=2, action="append", default=[], metavar=("RAW", "JSON"))
    args = parser.parse_args()
    if args.entries and not args.decompiler:
        parser.error("--entries requires --decompiler")
    args.output.mkdir(parents=True, exist_ok=False)
    raw = args.shader_json.read_bytes()
    shader = json.loads(raw.decode("utf-8-sig"))
    platform = shader["platforms"].index("D3D11")
    packed = base64.b64decode(shader["compressedBlob"], validate=True)
    segments = []
    for offset, size, decoded in zip(shader["offsets"][platform], shader["compressedLengths"][platform],
                                     shader["decompressedLengths"][platform]):
        segments.append(decompress_lz4(packed[offset:offset + size], decoded))
    count = struct.unpack_from("<I", segments[0])[0]
    metadata = resolve_metadata(shader)
    manifest, programs = [], {}
    for index in range(count):
        offset, length, segment = struct.unpack_from("<iii", segments[0], 4 + index * 12)
        info, code = read_program(segments[segment][offset:offset + length])
        info.update({"entry": index, "segment": segment, "offset": offset, "length": length,
                     "uses": [{k: m[k] for k in ["subshader", "pass", "pass_name", "stage"]}
                              for m in metadata.get(index, [])]})
        manifest.append(info)
        if index in args.entries:
            programs[index] = code
    write_json(args.output / "program_inventory.json", manifest)
    result = {"shader_path": str(args.shader_json.resolve()), "shader_sha256": sha256(raw),
              "tool_sha256": sha256(Path(__file__).read_bytes()), "platform": "D3D11",
              "translator_sha256": sha256(Path(__file__).with_name("recover_dxbc_hlsl.py").read_bytes()),
              "segment_sizes": [len(s) for s in segments], "entry_count": count,
              "runtime_bound": False, "recovered": [], "materials": []}
    if args.entries:
        compiler_path = Path("C:/Windows/System32/d3dcompiler_47.dll")
        result["decompiler"] = {"path": str(args.decompiler.resolve()), "sha256": sha256(args.decompiler.read_bytes())}
        result["compiler"] = {"path": str(compiler_path), "sha256": sha256(compiler_path.read_bytes())}
    for raw_path, json_path in args.material:
        material = recover_material(raw_path, json_path)
        write_json(args.output / (material["m_Name"] + ".complete.json"), material)
        result["materials"].append({key: material[key] for key in ["m_Name", "m_ValidKeywords", "m_CustomRenderQueue",
            "disabledShaderPasses", "enabledPassMask", "all_exported_properties_matched", "parsed_bytes", "raw_bytes", "trailing_bytes_hex"]})
    for index in args.entries:
        uses = metadata[index]
        signatures = {json.dumps(m["subprogram"], sort_keys=True) for m in uses}
        if len(signatures) != 1:
            raise ValueError(f"Entry {index} has different parameter layouts")
        meta = uses[0]
        dxbc = get_dxbc(programs[index])
        source = decompile(dxbc, args.decompiler)
        prefix = args.output / f"entry{index}"
        prefix.with_suffix(".original.dxbc").write_bytes(dxbc)
        prefix.with_suffix(".decompiled.hlsl").write_text(source, encoding="utf-8")
        compiler_source, instruction_map, translated_profile = translate(disassemble(dxbc, 128))
        prefix.with_suffix(".registers.hlsl").write_text(compiler_source, encoding="utf-8")
        write_json(prefix.with_suffix(".instruction-map.json"), instruction_map)
        restored, binding = restore_names(compiler_source, meta)
        prefix.with_suffix(".named.hlsl").write_text(restored, encoding="utf-8")
        write_json(prefix.with_suffix(".bindings.json"), binding)
        write_json(prefix.with_suffix(".render_state.json"), meta["state"])
        profile = {15: "vs_4_0", 16: "vs_5_0", 17: "ps_4_0", 18: "ps_5_0"}[manifest[index]["type"]]
        if profile != translated_profile:
            raise ValueError("Program container and DXBC profile differ")
        generic, generic_messages = compile_hlsl(source, profile)
        register_code, register_messages = compile_hlsl(compiler_source, profile)
        named, named_messages = compile_hlsl(restored, profile)
        prefix.with_suffix(".generic-recompiled.dxbc").write_bytes(generic)
        prefix.with_suffix(".registers-recompiled.dxbc").write_bytes(register_code)
        prefix.with_suffix(".named-recompiled.dxbc").write_bytes(named)
        generic_chunks, named_chunks = chunks(generic), chunks(named)
        assemblies = {"original": disassemble(dxbc), "generic-recompiled": disassemble(generic),
                      "registers-recompiled": disassemble(register_code),
                      "named-recompiled": disassemble(named)}
        for name, assembly in assemblies.items():
            prefix.with_suffix(f".{name}.asm").write_text(assembly, encoding="utf-8")
        executable_tag = "SHEX" if "SHEX" in generic_chunks else "SHDR"
        result["recovered"].append({"entry": index, "profile": profile, "keywords": manifest[index]["keywords"],
            "local_keywords": manifest[index]["local_keywords"], "original_dxbc_sha256": sha256(dxbc),
            "original_chunks": chunks(dxbc), "generic_chunks": generic_chunks, "named_chunks": named_chunks,
            "named_vs_generic_instruction_tokens_equal": generic_chunks[executable_tag] == named_chunks[executable_tag],
            "named_vs_generic_instructions_excluding_declarations_equal": instructions(assemblies["generic-recompiled"]) == instructions(assemblies["named-recompiled"]),
            "canonical_recovery": "DXBC hexadecimal instructions with uint register storage",
            "register_compiler_messages": register_messages,
            "original_opcodes": opcode_counts(assemblies["original"]),
            "named_opcodes": opcode_counts(assemblies["named-recompiled"]),
            "original_output_signature": signature_records(assemblies["original"], "Output"),
            "named_output_signature": signature_records(assemblies["named-recompiled"], "Output"),
            "named_material_flag_output": [line for line in assemblies["named-recompiled"].splitlines() if re.match(r"\w+\s+o2\.z\b", line.strip())],
            "generic_compiler_messages": generic_messages, "named_compiler_messages": named_messages})
    write_json(args.output / "recovery.json", result)
    print(json.dumps({"output": str(args.output.resolve()), "entries": count,
        "stages": [{"entry": row["entry"], "profile": row["profile"],
                    "messages": row["named_compiler_messages"]} for row in result["recovered"]],
        "runtime_bound": False}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
