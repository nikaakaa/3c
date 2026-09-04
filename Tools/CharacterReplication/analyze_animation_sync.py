import argparse
import csv
import hashlib
import struct
import sys
from pathlib import Path

from capstone import Cs, CS_ARCH_X86, CS_MODE_64
from capstone.x86 import X86_OP_MEM, X86_REG_RIP

from build_guide import Sources, differences, digest, read_json, write_json


FUNCTIONS = [0x1538A4C0, 0x15BDA820, 0x15BDB510, 0x15BEC110, 0x18B20B40,
             0x16FDB170, 0xD96A250, 0xD96A4D0, 0x1538D7C0, 0x15BD5560,
             0x1F858E10, 0x1F856900, 0x1F856A20]
THUNKS = {0x1F858E10, 0x1F856900, 0x1F856A20}
OWNERS = {"CKHHEGOGGCG", "DGIKPMOOLBH", "IILNEIJILBJ", "OAFMFKKNHJA", "OKIADFLACHO", "IAKAPNCLCNP"}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, default=Path("D:/ZZZ_Dump/output/character_replication/analysis/animation-sync"))
    args = parser.parse_args()
    metadata = Path("D:/ZZZ_Dump/PIK分析包/元数据")
    sys.path.insert(0, str(metadata))
    from offline_sources import MappedPages, PEImage
    verification_path = metadata / "控制器与战斗/verification.json"
    verification = read_json(verification_path)
    wanted = {"GameAssembly.dll", "game1500_306A84000_pages.bin", "game1500_306A84000_pages.map"}
    checked = []
    for source in verification["sources"]:
        path = Path(source["path"])
        if path.name not in wanted:
            continue
        if digest(path) != source["sha256"]:
            raise ValueError(f"来源与元数据版本不一致：{path}")
        checked.append(source)
    paths = {Path(s["path"]).name: Path(s["path"]) for s in checked}
    image = PEImage(paths["GameAssembly.dll"])
    snapshot = MappedPages(paths["game1500_306A84000_pages.bin"], paths["game1500_306A84000_pages.map"], "va-pa-offset")
    decoder = Cs(CS_ARCH_X86, CS_MODE_64)
    decoder.detail = True

    def memory_reference(address):
        instruction = next(decoder.disasm(image.read(address, 15), address))
        operand = next(o for o in instruction.operands if o.type == X86_OP_MEM and o.mem.base == X86_REG_RIP)
        return instruction.address + instruction.size + operand.mem.disp

    records = {}
    for name in ("methods", "fields"):
        path = metadata / f"控制器与战斗/{name}.csv"
        expected = next(a["sha256"] for a in verification["artifacts"] if a["path"] == path.name)
        if digest(path) != expected:
            raise ValueError(f"元数据 CSV 与核实记录不一致：{path}")
        with path.open(encoding="utf-8-sig", newline="") as stream:
            records[name] = list(csv.DictReader(stream))
    native = args.output / "native-flow"
    native.mkdir(parents=True, exist_ok=True)
    functions = []
    for address in FUNCTIONS:
        if address in THUNKS:
            raw = image.read(address, 10)
            if raw[:3] != bytes.fromhex("488b05") or raw[7:] != bytes.fromhex("48ffe0"):
                raise ValueError(f"Animator 叶函数不是已确认的槽跳转格式：{address:X}")
            evidence = {"begin": address, "end": address + 10, "byte_sha256": hashlib.sha256(raw).hexdigest(),
                        "format": "10-byte RIP-relative Animator native slot thunk",
                        "instructions": [(i.address, i.mnemonic, i.op_str) for i in decoder.disasm(raw, address)]}
        else:
            evidence = image.disassemble_function(address)
        evidence["methods"] = [m for m in records["methods"] if m["RVA"].lower() == hex(address)]
        filename = f"function-{address:08X}"
        write_json(native / (filename + ".json"), evidence)
        (native / (filename + ".asm")).write_text("\n".join(f"{a:08X} {m:10s} {o}" for a, m, o in evidence["instructions"]) + "\n", encoding="utf-8")
        functions.append({"rva": address, "sha256": evidence["byte_sha256"], "methods": evidence["methods"], "file": f"native-flow/{filename}.asm"})
    actors = {}
    for actor in ("corin", "unagi", "anbi"):
        settings = read_json(Path(__file__).parent / "sources" / (actor + ".json"))
        sources = Sources(settings)
        controller = sources.load("controller")
        comparison = read_json(args.output / actor / "export-comparison.json")
        old = read_json(sources.root / comparison["source"])
        stripped = read_json(sources.root / settings["controller"])
        added = []
        for machine in stripped["StateMachines"]:
            for state in machine["States"]:
                added.append({"machine": machine["Index"], "state": state["Index"], "m_TimeParamID": state.pop("m_TimeParamID"), "TimeParameter": state.pop("TimeParameter")})
        delta = list(differences(old, stripped))
        if delta:
            raise ValueError(f"{actor} 重新导出改变了既有字段：{delta[:3]}")
        character_path = settings["selectedConfigs"].get("character")
        if actor == "corin":
            character_path = "3367691705.blk_export/CAB-cf2cc6bd0231f98b5ce6cff06d377c1c/Avatar_Female_Size01_Corin.json"
        character = sources.decoded("character", character_path)
        actors[actor] = {"sources": sources.records, "oldControllerSha256": digest(sources.root / comparison["source"]),
                         "addedFields": added, "unexpectedChanges": delta,
                         "blendOverrides": {k: character[k] for k in ("BlendData", "StartBlendData", "BlendDataByTag")}}
        if actor == "corin":
            names = controller["Tos"]
    parameters = []
    base = int(verification["module_base"], 16)
    for instruction_rva, payload_offset, call_rva, thunk_rva in [(0x1538A69B, 40, 0x1538A6A4, 0x1F856900), (0x1538A6AA, 44, 0x1538A6B7, 0x1F856A20)]:
        rva = memory_reference(instruction_rva)
        value = struct.unpack("<I", snapshot.read(base + rva, 4))[0]
        call_slot, thunk_slot = memory_reference(call_rva), memory_reference(thunk_rva)
        if call_slot != thunk_slot:
            raise ValueError("参数 setter 与 Animator thunk 槽不符")
        parameters.append({"instructionRva": instruction_rva, "staticRva": rva, "snapshotVA": base + rva,
                           "id": value, "name": names[str(value)], "requestPayloadOffset": payload_offset,
                           "callRva": call_rva, "animatorThunkRva": thunk_rva, "nativeSlot": call_slot})
    constants = []
    for address, purpose in [(0x15BEC1B2, "请求目标位置解压比例"), (0x1538AB6B, "CrossFade 归一化混合时长")]:
        rva = memory_reference(address)
        raw = image.read(rva, 4)
        constants.append({"instructionRva": address, "dataRva": rva, "bytes": raw.hex(), "float": struct.unpack("<f", raw)[0], "purpose": purpose})
    write_json(args.output / "native-evidence.json", {"session": verification["session"], "moduleBase": verification["module_base"],
               "verifiedSources": checked, "verificationSha256": digest(verification_path), "actors": actors,
               "analysisTool": {"path": Path(__file__).resolve().as_posix(), "sha256": digest(Path(__file__))},
               "parameterIds": parameters, "constants": constants, "functions": functions,
               "fields": [f for f in records["fields"] if f["owner"] in OWNERS],
               "scope": "磁盘本机分支与既有快照中的静态参数 ID；没有证明当前运行实例、热更新分支或调用触发条件"})
    (args.output / "README.md").write_text((Path(__file__).parent / "evidence/animation-sync.md").read_text(encoding="utf-8"), encoding="utf-8")
    print({"actors": len(actors), "functions": len(functions), "parameters": parameters, "constants": constants})


if __name__ == "__main__":
    main()
