import argparse
import collections
import json
import re
import struct
from pathlib import Path

from recover_zzz_shader import compile_hlsl, disassemble, sha256, write_json


LANES = "xyzw"


def operands(text):
    result, start, depth = [], 0, 0
    for index, char in enumerate(text):
        depth += char in "(["
        depth -= char in ")]"
        if char == "," and depth == 0:
            result.append(text[start:index].strip())
            start = index + 1
    result.append(text[start:].strip())
    return result


def vector(values, scalar="uint"):
    return values[0] if len(values) == 1 else f"{scalar}{len(values)}({', '.join(values)})"


def destination(operand):
    if operand.startswith("[precise]"):
        operand = operand.replace("[precise]", "", 1).strip()
    if operand.startswith("[precise("):
        match = re.match(r"\[precise\(([xyzw]+)\)\]\s+(.+)", operand)
        if match:
            operand = match[2]
    match = re.fullmatch(r"(x\d+)\[(\d+|r\d+(?:\.\w+)? \+ \d+)\]\.([xyzw]+)", operand)
    if match:
        return f"{match[1]}[{match[2]}]", match[3]
    match = re.fullmatch(r"(r\d+|o\d+|u\d+)\.([xyzw]+)", operand)
    if not match:
        raise ValueError(f"Unsupported destination: {operand}")
    return match[1], match[2]


def opcode_and_tail(line):
    depth = 0
    for index, char in enumerate(line):
        depth += char == "("
        depth -= char == ")"
        if char.isspace() and depth == 0:
            return line[:index], line[index:].strip()
    return line, ""


def opcode_counts(assembly):
    active = False
    counts = collections.Counter()
    for line in assembly.splitlines():
        line = line.strip()
        if re.fullmatch(r"(?:cs|vs|ps)_[45]_0", line):
            active = True
        if not active or not line or line.startswith(("//", "dcl_", "{")) or re.fullmatch(r"(?:cs|vs|ps)_[45]_0", line):
            continue
        counts[opcode_and_tail(line)[0]] += 1
    return dict(counts)


def raw_source(operand, lanes, integer_modifiers=False):
    negative = operand.startswith("-")
    if negative:
        operand = operand[1:]
    absolute = operand.startswith("|") and operand.endswith("|")
    if absolute:
        operand = operand[1:-1]
    if operand.startswith("l("):
        values = operands(operand[2:-1])
        def to_uint(value):
            if re.fullmatch(r"0x[0-9a-f]{8}", value):
                return value + "u"
            try:
                return str(struct.unpack("<I", struct.pack("<f", float(value)))[0]) + "u"
            except (ValueError, OverflowError):
                return "0u"
        selected = [to_uint(values[0 if len(values) == 1 else LANES.index(lane)]) for lane in lanes]
        result = vector(selected)
    else:
        match = re.fullmatch(r"(x\d+)\[(\d+|r\d+(?:\.\w+)? \+ \d+)\](?:\.([xyzw]+))?", operand)
        if match:
            reg_expr = f"{match[1]}[{match[2]}]"
            swizzle = match[3] or LANES
            swizzle = swizzle * 4 if len(swizzle) == 1 else swizzle
            result = reg_expr + "." + "".join(swizzle[LANES.index(lane)] for lane in lanes)
        else:
            match = re.fullmatch(r"(r\d+|v\d+|vThreadID|(?:cb\d+|icb)\[[^\]]+\])(?:\.([xyzw]+))?", operand)
            if not match:
                raise ValueError(f"Unsupported source: {operand}")
            register, swizzle = match[1], match[2] or LANES
            swizzle = swizzle * 4 if len(swizzle) == 1 else swizzle
            register = register.replace(" ", "")
            result = register + "." + "".join(swizzle[LANES.index(lane)] for lane in lanes)
            if register.startswith("cb"):
                result = f"asuint({result})"
    if absolute:
        result = f"asuint(abs(asint({result})))" if integer_modifiers else f"({result} & 0x7fffffffu)"
    if negative:
        result = f"(0u - {result})" if integer_modifiers else f"({result} ^ 0x80000000u)"
    return result


def typed_source(operand, lanes, kind):
    raw = raw_source(operand, lanes, integer_modifiers=kind in ("int", "uint"))
    return raw if kind == "uint" else f"as{kind}({raw})"


def signature_records(assembly, label):
    section = assembly.split(f"// {label} signature:", 1)[1].split("// --------------------", 1)
    if len(section) != 2:
        return []
    records = []
    for line in section[1].splitlines()[1:]:
        match = re.fullmatch(r"// (\S+)\s+(\d+)\s+([xyzw]+)\s+(\d+)\s+(\S+)\s+(float|uint|sint)\s*([xyzw ]*)\s*", line)
        if not match:
            break
        name, index, mask, register, system, kind, used = match.groups()
        records.append({"name": name, "index": int(index), "mask": mask, "register": int(register),
                        "system": system, "kind": "int" if kind == "sint" else kind, "used": used.replace(" ", "")})
    return records


def translate(assembly):
    profile = re.search(r"^(?:cs|vs|ps)_[45]_0$", assembly, re.M)
    if not profile:
        raise ValueError("Unsupported shader profile")
    profile = profile[0]
    compute = profile.startswith("cs")
    input_signature = [] if compute else signature_records(assembly, "Input")
    outputs = [] if compute else signature_records(assembly, "Output")
    declarations, records, emitted, texture_dimensions = [], [], [], {}
    immediate = re.search(r"dcl_immediateConstantBuffer\s*\{(.*?)\}\s*\}", assembly, re.S)
    if immediate:
        values = re.findall(r"0x[0-9a-f]{8}", immediate[1])
        if len(values) % 4:
            raise ValueError("Invalid immediate constant vector count")
        vectors = [vector([x + "u" for x in values[i:i + 4]]) for i in range(0, len(values), 4)]
        declarations.append(f"static const uint4 icb[{len(vectors)}] = {{ {', '.join(vectors)} }};")
        assembly = assembly[:immediate.start()] + assembly[immediate.end():]
    threads, temps, indent = None, 0, 1
    indexable_temps = {}

    def emit(text):
        emitted.append("    " * indent + text)

    def assign(target, expression, kind="uint", saturate=False):
        register, lanes = destination(target)
        if saturate:
            if kind != "float":
                expression = f"asfloat({expression})"
            expression = f"saturate({expression})"
            kind = "float"
        if kind != "uint":
            expression = f"asuint({expression})"
        emit(f"{register}.{lanes} = {expression};")

    for number, raw_line in enumerate(assembly.splitlines(), 1):
        line = raw_line.strip()
        if not line or line.startswith("//") or line == profile:
            continue
        if line.startswith("dcl_"):
            if match := re.fullmatch(r"dcl_constantbuffer CB(\d+)\[(\d+)\], (?:immediate|dynamic)Indexed", line):
                declarations.append(f"cbuffer cb{match[1]} : register(b{match[1]}) {{ float4 cb{match[1]}[{match[2]}]; }}")
            elif match := re.fullmatch(r"dcl_(resource|uav)_structured ([tu]\d+), (\d+)", line):
                name, stride = match[2], int(match[3])
                if stride % 4:
                    raise ValueError("Structured stride must be a multiple of four")
                declarations.append(f"struct {name}_Element {{ uint words[{stride // 4}]; }};")
                kind = "RWStructuredBuffer" if match[1] == "uav" else "StructuredBuffer"
                declarations.append(f"{kind}<{name}_Element> {name} : register({name});")
            elif match := re.fullmatch(r"dcl_resource_(texture2darray|texture2d) \(float,float,float,float\) (t\d+)", line):
                kind = {"texture2darray": "Texture2DArray", "texture2d": "Texture2D"}[match[1]]
                declarations.append(f"{kind}<float4> {match[2]} : register({match[2]});")
                texture_dimensions[match[2]] = match[1]
            elif match := re.fullmatch(r"dcl_sampler (s\d+), mode_(default|comparison)", line):
                kind = "SamplerComparisonState" if match[2] == "comparison" else "SamplerState"
                declarations.append(f"{kind} {match[1]} : register({match[1]});")
            elif match := re.fullmatch(r"dcl_thread_group (\d+), (\d+), (\d+)", line):
                threads = tuple(map(int, match.groups()))
            elif match := re.fullmatch(r"dcl_temps (\d+)", line):
                temps = int(match[1])
            elif match := re.fullmatch(r"dcl_indexableTemp x(\d+)\[(\d+)\], (\d+)", line):
                name, size, stride = f"x{match[1]}", int(match[2]), int(match[3])
                if stride not in (1, 2, 3, 4):
                    raise ValueError(f"Unsupported indexable temp stride: {stride}")
                indexable_temps[name] = size
                declarations.append(f"static uint{stride} {name}[{size}];")
            elif not compute and re.fullmatch(r"dcl_(?:input(?:_ps)?(?:_s[ig]v)?|output(?:_siv)?) (?:linear(?: noperspective)?(?: centroid)?|linear centroid|constant|centroid)?\s?[vo]\d+\.[xyzw]+(?:, \w+)?", line):
                pass
            elif line not in ("dcl_globalFlags refactoringAllowed", "dcl_input vThreadID.x"):
                raise ValueError(f"Unsupported declaration: {line}")
            continue
        opcode, tail = opcode_and_tail(line)
        args = operands(tail) if tail else []
        first_line = len(emitted) + 1
        saturated = opcode.endswith("_sat")
        op = opcode[:-4] if saturated else opcode
        if op in ("sample", "sample_l", "sample_b", "sample_c_lz", "ld"):
            texture = args[2].split(".")[0]
            op += f"_indexable({texture_dimensions[texture]})(float,float,float,float)"
        if op in ("if_nz", "if_z", "breakc_nz", "breakc_z"):
            condition = f"{raw_source(args[0], 'x')} {'!=' if op.endswith('_nz') else '=='} 0u"
            if op.startswith("if"):
                emit(f"if ({condition}) {{")
                indent += 1
            else:
                emit(f"if ({condition}) break;")
        elif op in ("else", "endif", "endloop"):
            indent -= 1
            emit("} else {" if op == "else" else "}")
            indent += op == "else"
        elif op == "loop":
            emit("[loop] while (true) {")
            indent += 1
        elif op in ("ret", "continue"):
            if op == "ret" and not compute:
                for index, output in enumerate(outputs):
                    name = f"o{output['register']}"
                    value = name + "." + output["mask"]
                    if output["kind"] != "uint":
                        value = f"as{output['kind']}({value})"
                    emit(f"result.output{index} = {value};")
                emit("return result;")
            else:
                emit("return;" if op == "ret" else "continue;")
        elif op == "discard_nz":
            emit(f"if ({raw_source(args[0], 'x')} != 0u) discard;")
        elif op.startswith("ld_structured_indexable"):
            target, address, offset, resource = args
            _, lanes = destination(target)
            match = re.fullmatch(r"([tu]\d+)\.([xyzw]{4})", resource)
            if not match:
                raise ValueError(f"Unsupported structured resource: {resource}")
            index, byte = raw_source(address, "x"), raw_source(offset, "x")
            values = [f"{match[1]}[{index}].words[({byte} / 4u) + {LANES.index(match[2][LANES.index(lane)])}u]" for lane in lanes]
            assign(target, vector(values))
        elif op == "store_structured":
            resource, address, offset, value = args
            name, lanes = destination(resource)
            if lanes != LANES[:len(lanes)]:
                raise ValueError("Structured stores require contiguous masks")
            index, byte = raw_source(address, "x"), raw_source(offset, "x")
            for lane in lanes:
                emit(f"{name}[{index}].words[({byte} / 4u) + {LANES.index(lane)}u] = {raw_source(value, lane)};")
        elif op.startswith("sample_"):
            target, position, texture, sampler, *extra = args
            _, lanes = destination(target)
            tex, swizzle = texture.split(".")
            coordinate_mask = "xyz" if "texture2darray" in op else "xy"
            if op.startswith("sample_c_lz_"):
                expression = f"{tex}.SampleCmpLevelZero({sampler}, {typed_source(position, coordinate_mask, 'float')}, {typed_source(extra[0], 'x', 'float')})"
            else:
                function = {"sample_indexable": "Sample", "sample_l_indexable": "SampleLevel", "sample_b_indexable": "SampleBias"}.get(op.split('(')[0])
                if not function:
                    raise ValueError(f"Unsupported sampling opcode: {op}")
                selected = "".join(swizzle[LANES.index(lane)] for lane in lanes)
                sampling_args = [sampler, typed_source(position, coordinate_mask, "float")] + [typed_source(x, "x", "float") for x in extra]
                expression = f"{tex}.{function}({', '.join(sampling_args)}).{selected}"
            assign(target, expression, "float")
        elif op == "ld_indexable(texture2d)(float,float,float,float)":
            target, position, texture = args
            _, lanes = destination(target)
            tex, swizzle = texture.split(".")
            selected = "".join(swizzle[LANES.index(lane)] for lane in lanes)
            assign(target, f"{tex}.Load({typed_source(position, 'xyw', 'int')}).{selected}", "float")
        elif op == "sincos":
            sin_target, cos_target, value = args
            temporary = f"sincosBits{len(records)}"
            emit(f"uint4 {temporary} = {raw_source(value, LANES)};")
            for target, function in [(sin_target, "sin"), (cos_target, "cos")]:
                if target != "null":
                    _, lanes = destination(target)
                    assign(target, f"{function}(asfloat({temporary}.{lanes}))", "float")
        else:
            if not args:
                records.append({"assembly_line": number, "instruction": line,
                                "body_first_line": len(emitted) + 1, "body_line_count": 0})
                continue
            if op == "imul":
                if args[0] != "null":
                    raise ValueError("High-product destination is not supported")
                args = args[1:]
            target, *inputs = args
            _, lanes = destination(target)
            bits = [raw_source(value, lanes) for value in inputs]
            unsigned = [typed_source(value, lanes, "uint") for value in inputs]
            signed = [typed_source(value, lanes, "int") for value in inputs]
            floats = [typed_source(value, lanes, "float") for value in inputs]
            kind = "uint"
            if op == "mov":
                expression = bits[0]
            elif op == "movc":
                expression = f"(({bits[0]} != 0u) ? {bits[1]} : {bits[2]})"
            elif op in ("add", "mul", "div"):
                symbol = {"add": "+", "mul": "*", "div": "/"}[op]
                expression, kind = f"({floats[0]} {symbol} {floats[1]})", "float"
            elif op == "mad":
                expression, kind = f"mad({', '.join(floats)})", "float"
            elif op in ("min", "max", "sqrt", "sqr", "rsq", "rcp", "frc", "exp", "log", "round_ni", "round_z"):
                function = {"rsq": "rsqrt", "sqr": "sqrt", "frc": "frac", "exp": "exp2", "log": "log2",
                            "round_ni": "floor", "round_z": "trunc"}.get(op, op)
                expression, kind = f"{function}({', '.join(floats)})", "float"
            elif op in ("dp2", "dp3", "dp4"):
                values = [typed_source(value, LANES[:int(op[-1])], "float") for value in inputs]
                expression, kind = f"dot({', '.join(values)})", "float"
            elif op in ("and", "or", "iadd", "imul", "ishl"):
                symbol = {"and": "&", "or": "|", "iadd": "+", "imul": "*", "ishl": "<<"}[op]
                second = f"({unsigned[1]} & 31u)" if op == "ishl" else unsigned[1]
                expression = f"({unsigned[0]} {symbol} {second})"
            elif op == "imad":
                expression = f"({unsigned[0]} * {unsigned[1]} + {unsigned[2]})"
            elif op == "ishr":
                expression, kind = f"({signed[0]} >> ({unsigned[1]} & 31u))", "int"
            elif op == "ushr":
                expression = f"({unsigned[0]} >> ({unsigned[1]} & 31u))"
            elif op in ("imin", "imax"):
                expression, kind = f"{op[1:]}({', '.join(signed)})", "int"
            elif op in ("umin", "umax"):
                expression = f"{op[1:]}({', '.join(unsigned)})"
            elif op in ("lt", "ge", "eq", "ne", "ilt", "ige", "ieq", "ult", "uge"):
                values = signed if op.startswith("i") else unsigned if op.startswith("u") else floats
                symbol = {"lt": "<", "ge": ">=", "eq": "==", "ne": "!=",
                          "lt": "<", "ge": ">=", "eq": "==", "ne": "!=",
                          "ult": "<", "uge": ">="}[op]
                expression = f"(({values[0]} {symbol} {values[1]}) ? 0xffffffffu : 0u)"
            elif op in ("ftou", "ftoi", "itof", "utof"):
                vector_type = {"ftou": "uint", "ftoi": "int", "itof": "float", "utof": "float"}[op]
                input_value = floats[0] if op.startswith("f") else signed[0] if op.startswith("i") else unsigned[0]
                expression = f"({vector_type}{len(lanes) if len(lanes) > 1 else ''})({input_value})"
                kind = vector_type
            elif op in ("bfi", "ubfe"):
                width_values = operands(inputs[0][2:-1]) if inputs[0].startswith("l(") else []
                shift_values = operands(inputs[1][2:-1]) if inputs[1].startswith("l(") else []
                if not width_values or not shift_values:
                    raise ValueError("Variable bitfield ranges need explicit recovery")
                widths = [int(width_values[0 if len(width_values) == 1 else LANES.index(lane)], 16) & 31 for lane in lanes]
                shifts = [int(shift_values[0 if len(shift_values) == 1 else LANES.index(lane)], 16) & 31 for lane in lanes]
                if any(width + shift > 32 for width, shift in zip(widths, shifts)):
                    raise ValueError("Bitfield range extends past the verified 32-bit boundary")
                masks = vector([f"0x{((1 << width) - 1):08x}u" for width in widths])
                if op == "ubfe":
                    expression = f"(({unsigned[2]} >> {unsigned[1]}) & {masks})"
                else:
                    shifted = f"({masks} << {unsigned[1]})"
                    expression = f"((({unsigned[2]} << {unsigned[1]}) & {shifted}) | ({unsigned[3]} & ~{shifted}))"
            else:
                raise ValueError(f"Unsupported opcode: {op}")
            assign(target, expression, kind, saturated)
        records.append({"assembly_line": number, "instruction": line, "body_first_line": first_line,
                        "body_line_count": len(emitted) - first_line + 1})
    if indent != 1 or (compute and threads != (64, 1, 1)):
        raise ValueError("Entrypoint/control-flow contract mismatch")
    names = [f"r{i}" for i in range(temps)]
    initializers = []
    if compute:
        signature = f"[numthreads({', '.join(map(str, threads))})]\nvoid main(uint3 vThreadID : SV_DispatchThreadID)\n{{"
    else:
        declarations.append("struct ShaderOutput {")
        for index, output in enumerate(outputs):
            dimension = str(len(output["mask"])) if len(output["mask"]) > 1 else ""
            declarations.append(f"    {output['kind']}{dimension} output{index} : {output['name']}{output['index']};")
            name = f"o{output['register']}"
            if name not in names:
                names.append(name)
        declarations.append("};")
        parameters = []
        for index, item in enumerate(input_signature):
            dimension = str(len(item["mask"])) if len(item["mask"]) > 1 else ""
            front_face = item["name"].lower() == "sv_isfrontface"
            kind = "bool" if front_face else item["kind"] + dimension
            name = f"v{item['register']}"
            parameters.append(f"{kind} input{index} : {item['name']}{item['index']}")
            value = f"input{index}"
            if front_face:
                value = f"({value} ? 0xffffffffu : 0u)"
            elif item["kind"] != "uint":
                value = f"asuint({value})"
            initializers.append(f"    {name}.{item['mask']} = {value};")
            if name not in names:
                names.append(name)
        signature = "ShaderOutput main(" + ", ".join(parameters) + ")\n{"
        initializers.insert(0, "    ShaderOutput result;")
    registers = "    uint4 " + ", ".join(names) + ";"
    source = "\n".join(declarations + [signature, registers] + initializers + emitted + ["}", ""])
    return source, records, profile


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dxbc", type=Path, nargs="+", required=True)
    parser.add_argument("--decompiled-dir", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=False)
    report = {"runtime_bound": False, "live_process_read": False,
              "tool_sha256": sha256(Path(__file__).read_bytes()), "programs": []}
    for path in args.dxbc:
        data = path.read_bytes()
        original = disassemble(data, 128)
        source, mapping, profile = translate(original)
        stem = args.output / path.stem
        stem.with_suffix(".original.hex.asm").write_text(original, encoding="utf-8")
        stem.with_suffix(".uint-registers.hlsl").write_text(source, encoding="utf-8")
        code, messages = compile_hlsl(source, profile)
        rebuilt = disassemble(code, 128)
        stem.with_suffix(".recompiled.dxbc").write_bytes(code)
        stem.with_suffix(".recompiled.hex.asm").write_text(rebuilt, encoding="utf-8")
        write_json(stem.with_suffix(".mapping.json"), mapping)
        program = {"input": str(path.resolve()), "input_sha256": sha256(data), "profile": profile,
                                  "source_sha256": sha256(source.encode()), "compiled_sha256": sha256(code),
                                  "instruction_count": len(mapping), "compiler_messages": messages,
                                  "original_opcodes": opcode_counts(original),
                                  "recompiled_opcodes": opcode_counts(rebuilt)}
        if args.decompiled_dir:
            legacy_path = args.decompiled_dir / (path.stem + ".decompiled.hlsl")
            legacy = legacy_path.read_text(encoding="utf-8")
            if legacy.count("void main)") != 1:
                raise ValueError("Legacy entrypoint does not match the evidenced decompiler defect")
            legacy = legacy.replace("void main)", "[numthreads(64,1,1)] void main(uint3 vThreadID : SV_DispatchThreadID)")
            legacy_code, legacy_messages = compile_hlsl(legacy, "cs_5_0")
            legacy_assembly = disassemble(legacy_code, 128)
            stem.with_suffix(".legacy-entrypoint-only.hlsl").write_text(legacy, encoding="utf-8")
            stem.with_suffix(".legacy-entrypoint-only.dxbc").write_bytes(legacy_code)
            stem.with_suffix(".legacy-entrypoint-only.asm").write_text(legacy_assembly, encoding="utf-8")
            program["legacy_entrypoint_only"] = {"path": str(legacy_path.resolve()),
                "sha256": sha256(legacy_path.read_bytes()), "compiler_messages": legacy_messages,
                "opcodes": opcode_counts(legacy_assembly)}
        report["programs"].append(program)
    write_json(args.output / "recovery.json", report)
    print(json.dumps({"output": str(args.output.resolve()), "programs": [
        {"input": p["input"], "profile": p["profile"], "messages": p["compiler_messages"]}
        for p in report["programs"]], "runtime_bound": False}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
