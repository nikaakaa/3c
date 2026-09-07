import argparse
import bisect
import csv
import hashlib
import json
import mmap
import struct
from pathlib import Path

from capstone import Cs, CS_ARCH_X86, CS_MODE_64
from capstone.x86 import X86_OP_IMM, X86_OP_MEM, X86_REG_RIP


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--pe", type=Path, required=True)
    parser.add_argument("--sha256", required=True)
    parser.add_argument("--methods", type=Path, required=True)
    parser.add_argument("--rva", type=lambda s: int(s, 0), nargs="*", default=[])
    parser.add_argument("--thunk-slots", type=lambda s: int(s, 0), nargs="*", default=[])
    parser.add_argument("--thunk-targets", type=lambda s: int(s, 0), nargs="*", default=[])
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    with args.pe.open("rb") as stream, mmap.mmap(stream.fileno(), 0, access=mmap.ACCESS_READ) as data:
        digest = hashlib.sha256(data).hexdigest()
        if digest != args.sha256.lower():
            raise ValueError("PE SHA-256 does not match the metadata session")
        pe = struct.unpack_from("<I", data, 0x3C)[0]
        section_count = struct.unpack_from("<H", data, pe + 6)[0]
        optional_size = struct.unpack_from("<H", data, pe + 20)[0]
        optional = pe + 24
        if data[pe:pe + 4] != b"PE\0\0" or struct.unpack_from("<H", data, optional)[0] != 0x20B:
            raise ValueError("Expected PE32+")
        image_base = struct.unpack_from("<Q", data, optional + 24)[0]
        sections = []
        for i in range(section_count):
            offset = optional + optional_size + i * 40
            virtual_size, rva, raw_size, raw_offset = struct.unpack_from("<IIII", data, offset + 8)
            sections.append({"name": data[offset:offset + 8].split(b"\0")[0].decode("ascii"),
                             "rva": rva, "virtual_size": virtual_size, "raw_size": raw_size, "raw_offset": raw_offset})

        def file_offset(rva):
            for section in sections:
                delta = rva - section["rva"]
                if 0 <= delta < section["raw_size"]:
                    return section["raw_offset"] + delta
            raise ValueError(f"RVA {rva:#x} has no file bytes")

        exception_rva, exception_size = struct.unpack_from("<II", data, optional + 112 + 3 * 8)
        exception_offset = file_offset(exception_rva)
        functions = [struct.unpack_from("<III", data, offset)
                     for offset in range(exception_offset, exception_offset + exception_size, 12)]
        functions.sort()
        starts = [f[0] for f in functions]
        roots = {}

        def unwind_root(record):
            if record in roots:
                return roots[record]
            unwind_offset = file_offset(record[2])
            flags = data[unwind_offset] >> 3
            if flags & 4:
                code_slots = data[unwind_offset + 2]
                chain_offset = unwind_offset + 4 + ((code_slots + 1) & ~1) * 2
                parent = struct.unpack_from("<III", data, chain_offset)
                root = unwind_root(parent)
            else:
                root = record
            roots[record] = root
            return root

        unwind_groups = {}
        for record in functions:
            unwind_groups.setdefault(unwind_root(record), []).append(record)
        methods = {}
        with args.methods.open(encoding="utf-8-sig", newline="") as stream:
            for record in csv.DictReader(stream):
                if record["RVA"].startswith("0x"):
                    methods.setdefault(int(record["RVA"], 16), []).append(record)
        args.output.mkdir(parents=True, exist_ok=False)
        disassembler = Cs(CS_ARCH_X86, CS_MODE_64)
        disassembler.detail = True
        result = {"pe": str(args.pe.resolve()), "sha256": digest, "image_base": hex(image_base),
                  "methods_path": str(args.methods.resolve()), "methods_sha256": hashlib.sha256(args.methods.read_bytes()).hexdigest(),
                  "tool_sha256": hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
                  "sections": sections, "live_process_access": False, "functions": []}
        result["resolved_thunks"] = []
        for rva, names in methods.items():
            if not args.thunk_slots and not args.thunk_targets:
                break
            offset = file_offset(rva)
            decoded = list(disassembler.disasm(data[offset:offset + 16], rva))
            if not decoded:
                continue
            first = decoded[0]
            length = first.size
            if first.mnemonic == "jmp" and first.operands[0].type == X86_OP_IMM:
                if first.operands[0].imm in args.thunk_targets:
                    result["resolved_thunks"].append({"method_rva": hex(rva), "target_rva": hex(first.operands[0].imm),
                        "bytes_hex": data[offset:offset + length].hex(), "metadata": names})
                continue
            if first.mnemonic == "jmp" and first.operands[0].type == X86_OP_MEM:
                operand = first.operands[0]
            elif first.mnemonic == "mov" and len(decoded) > 1 and decoded[1].mnemonic == "jmp" and decoded[1].op_str == first.op_str.split(",")[0]:
                operand = first.operands[1]
                length += decoded[1].size
            else:
                continue
            if operand.type != X86_OP_MEM or operand.mem.base != X86_REG_RIP:
                continue
            slot = first.address + first.size + operand.mem.disp
            if slot in args.thunk_slots:
                result["resolved_thunks"].append({"method_rva": hex(rva), "slot_rva": hex(slot),
                    "bytes_hex": data[offset:offset + length].hex(), "metadata": names})
        for requested in args.rva:
            position = bisect.bisect_right(starts, requested) - 1
            begin, end, unwind = functions[position]
            if begin != requested or not begin < end:
                raise ValueError(f"Requested RVA {requested:#x} is not an exact .pdata function start")
            root = unwind_root(functions[position])
            if root[0] != requested:
                raise ValueError(f"Requested RVA is a chained fragment; use primary RVA {root[0]:#x}")
            segments = unwind_groups[root]
            end = max(segment[1] for segment in segments)
            code_parts, instructions, segment_records = [], [], []
            for segment_begin, segment_end, segment_unwind in segments:
                offset = file_offset(segment_begin)
                part = data[offset:offset + segment_end - segment_begin]
                decoded = list(disassembler.disasm(part, segment_begin))
                if sum(i.size for i in decoded) != len(part):
                    raise ValueError(f"Incomplete decoding at {segment_begin:#x}")
                segment_records.append({"begin_rva": hex(segment_begin), "end_rva": hex(segment_end),
                    "unwind_rva": hex(segment_unwind), "binary_offset": sum(map(len, code_parts)),
                    "bytes": len(part), "sha256": hashlib.sha256(part).hexdigest()})
                code_parts.append(part)
                instructions.extend(decoded)
            code = b"".join(code_parts)
            lines, calls, references = [], [], []
            for instruction in instructions:
                notes = []
                if instruction.mnemonic in ("call", "jmp") and instruction.operands[0].type == X86_OP_IMM:
                    target = instruction.operands[0].imm
                    names = [f"{m['owner']}.{m['method']}({m['parameters']})" for m in methods.get(target, [])]
                    if instruction.mnemonic == "call":
                        calls.append({"at": hex(instruction.address), "target": hex(target), "names": names})
                    if names:
                        notes.append(" | ".join(names))
                for operand in instruction.operands:
                    if operand.type == X86_OP_MEM and operand.mem.base == X86_REG_RIP:
                        target = instruction.address + instruction.size + operand.mem.disp
                        record = {"at": hex(instruction.address), "rva": hex(target)}
                        try:
                            target_offset = file_offset(target)
                            record["bytes16_hex"] = data[target_offset:target_offset + 16].hex()
                        except ValueError:
                            record["file_bytes_unavailable"] = True
                        references.append(record)
                        notes.append(f"RIP_RVA={target:#x}")
                line = f"{instruction.address:08X}  {instruction.bytes.hex():<30} {instruction.mnemonic:<9} {instruction.op_str}"
                if notes:
                    line += " ; " + "; ".join(notes)
                lines.append(line)
            filename = f"{begin:08X}"
            (args.output / (filename + ".bin")).write_bytes(code)
            (args.output / (filename + ".asm")).write_text("\n".join(lines) + "\n", encoding="utf-8")
            result["functions"].append({"rva": hex(begin), "end_rva": hex(end), "unwind_rva": hex(unwind),
                "bytes": len(code), "code_sha256": hashlib.sha256(code).hexdigest(),
                "unwind_segments": segment_records,
                "metadata": methods.get(begin, []), "calls": calls, "rip_references": references})
        (args.output / "native_evidence.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(json.dumps({"functions": [{k: f[k] for k in ("rva", "end_rva", "bytes", "code_sha256")} for f in result["functions"]],
                          "resolved_thunks": result["resolved_thunks"]}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
