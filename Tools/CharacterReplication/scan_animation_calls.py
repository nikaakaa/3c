import argparse
import csv
import json
import struct
import sys
from collections import defaultdict
from pathlib import Path

import numpy as np
from capstone import Cs, CS_ARCH_X86, CS_MODE_64
from capstone.x86 import X86_OP_MEM, X86_REG_RIP

from build_guide import digest, read_json, write_json


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, default=Path("D:/ZZZ_Dump/output/character_replication/analysis/animation-sync/native-calls"))
    args = parser.parse_args()
    metadata = Path("D:/ZZZ_Dump/PIK分析包/元数据")
    sys.path.insert(0, str(metadata))
    from offline_sources import PEImage
    with (metadata / "控制器与战斗/methods.csv").open(encoding="utf-8-sig", newline="") as stream:
        methods = list(csv.DictReader(stream))
    target_names = {"CrossFade", "CrossFadeInFixedTime", "Play", "PlayInFixedTime"}
    targets = {int(m["RVA"], 16): m for m in methods if m["owner"] == "UnityEngine.Animator" and m["method"] in target_names and m["RVA"].startswith("0x")}
    by_rva = defaultdict(list)
    for method in methods:
        if method["RVA"].startswith("0x"):
            by_rva[int(method["RVA"], 16)].append(method)
    path = Path("D:/Normal_Software/HoYoPlay/games/ZenlessZoneZero Game/GameAssembly.dll")
    expected = next(s["sha256"] for s in read_json(metadata / "控制器与战斗/verification.json")["sources"] if Path(s["path"]).name == path.name)
    actual = digest(path)
    if actual != expected:
        raise ValueError("GameAssembly 与元数据构建不一致")
    image = PEImage(path)
    decoder = Cs(CS_ARCH_X86, CS_MODE_64)
    decoder.detail = True
    slots = {}
    for rva, method in targets.items():
        if not method["parameters"].startswith("int "):
            continue
        instructions = list(decoder.disasm(image.read(rva, 16), rva))
        if len(instructions) < 2 or instructions[0].mnemonic != "mov" or instructions[1].op_str != "rax" or instructions[1].mnemonic != "jmp":
            continue
        instruction = instructions[0]
        if len(instruction.operands) != 2 or instruction.operands[1].type != X86_OP_MEM or instruction.operands[1].mem.base != X86_REG_RIP:
            continue
        slot = instruction.address + instruction.size + instruction.operands[1].mem.disp
        slots[slot] = method
    pe = struct.unpack_from("<I", image.data, 0x3C)[0]
    count = struct.unpack_from("<H", image.data, pe + 6)[0]
    section_start = pe + 24 + struct.unpack_from("<H", image.data, pe + 20)[0]
    candidates = []
    for index in range(count):
        at = section_start + index * 40
        if not struct.unpack_from("<I", image.data, at + 36)[0] & 0x20000000:
            continue
        rva, size, offset = struct.unpack_from("<III", image.data, at + 12)
        data = np.frombuffer(image.data, dtype=np.uint8, count=size, offset=offset)
        positions = np.flatnonzero((data[:-4] == 0xE8) | (data[:-4] == 0xE9))
        displacement = sum(data[positions + i].astype(np.int64) << (8 * (i - 1)) for i in range(1, 5))
        displacement = np.where(displacement >= 0x80000000, displacement - 0x100000000, displacement)
        destinations = positions + rva + 5 + displacement
        selected = np.isin(destinations, np.array(list(targets), dtype=np.int64))
        candidates.extend((int(p + rva), int(d), "direct") for p, d in zip(positions[selected], destinations[selected]))
        positions = np.flatnonzero((data[:-5] == 0xFF) & ((data[1:-4] == 0x15) | (data[1:-4] == 0x25)))
        displacement = sum(data[positions + i].astype(np.int64) << (8 * (i - 2)) for i in range(2, 6))
        displacement = np.where(displacement >= 0x80000000, displacement - 0x100000000, displacement)
        destinations = positions + rva + 6 + displacement
        selected = np.isin(destinations, np.array(list(slots), dtype=np.int64))
        candidates.extend((int(p + rva), int(d), "indirect-slot") for p, d in zip(positions[selected], destinations[selected]))
    cache = {}
    results = []
    rejected = []
    args.output.mkdir(parents=True, exist_ok=True)
    for address, destination, kind in candidates:
        try:
            begin = image.function(address)[0]
            if begin not in cache:
                cache[begin] = image.disassemble_function(begin)
            evidence = cache[begin]
            instructions = evidence["instructions"]
            instruction = next((i for i in instructions if i[0] == address), None)
            valid = instruction is not None and instruction[1] in ("call", "jmp")
            if valid and kind == "direct":
                valid = instruction[2] == hex(destination)
            if valid and kind == "indirect-slot":
                decoded = next(decoder.disasm(image.read(address, 6), address))
                valid = decoded.operands[0].type == X86_OP_MEM and decoded.operands[0].mem.base == X86_REG_RIP and decoded.address + decoded.size + decoded.operands[0].mem.disp == destination
            if not valid:
                rejected.append({"address": address, "reason": "不在已解码指令边界"})
                continue
            labels = by_rva.get(begin, [])
            method_id = f"function-{begin:08X}"
            write_json(args.output / (method_id + ".json"), evidence)
            (args.output / (method_id + ".asm")).write_text("\n".join(f"{a:08X} {m:10s} {o}" for a, m, o in instructions) + "\n", encoding="utf-8")
            position = instructions.index(instruction)
            results.append({"callRva": address, "functionRva": begin,
                            "callerMethods": [{k: m[k] for k in ("owner", "method", "parameters", "RVA")} for m in labels],
                            "target": (targets if kind == "direct" else slots)[destination], "referenceKind": kind,
                            "targetRvaOrSlot": destination, "instruction": instruction,
                            "context": instructions[max(0, position - 16):position + 3], "file": method_id + ".asm"})
        except ValueError as error:
            rejected.append({"address": address, "reason": str(error)})
    write_json(args.output / "index.json", {"source": path.as_posix(), "sourceSha256": actual,
               "targets": list(targets.values()), "nativeSlots": {hex(k): v for k, v in slots.items()},
               "verifiedCalls": results, "unverifiedCandidates": rejected,
               "scope": "同版本 GameAssembly 的直接 E8/E9 以及由已确认 Animator thunk 指定的 FF15/FF25 槽引用；不包括寄存器间接调用或 UnityPlayer 内部调用"})
    print(json.dumps({"nativeSlots": len(slots), "candidateCalls": len(candidates), "verifiedCalls": len(results), "unverified": len(rejected), "callerFunctions": len(cache)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
