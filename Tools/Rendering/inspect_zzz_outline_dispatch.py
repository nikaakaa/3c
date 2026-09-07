import argparse
import json
import re
import struct
import sys
from pathlib import Path

from capture_zzz_shader_parameters import SnapshotProperties
from recover_zzz_shader import write_json


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--metadata-dir", type=Path, required=True)
    parser.add_argument("--renderer-snapshot", type=Path, required=True)
    parser.add_argument("--tag-types", type=Path, required=True)
    parser.add_argument("--tag-initializer", type=Path, required=True)
    parser.add_argument("--draw-asm", type=Path, required=True)
    parser.add_argument("--state-asm", type=Path, nargs="+", required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    sys.dont_write_bytecode = True
    sys.path.insert(0, str(args.metadata_dir.resolve()))
    from export_gameplay_metadata import GameplaySession, IMAGE, IMAGE_SHA256, sha

    snapshot = json.loads(args.renderer_snapshot.read_text(encoding="utf-8"))
    player = next(p for p in snapshot["sources"] if Path(p["path"]).name == "UnityPlayer.dll")
    if snapshot["session"] != "829" or sha(IMAGE) != IMAGE_SHA256 or sha(Path(player["path"])) != player["sha256"]:
        raise ValueError("Module/snapshot identity mismatch")
    session = GameplaySession("829")
    player_base = int(snapshot["unityplayer_base"], 16)
    reader = SnapshotProperties(session, player_base)
    types = json.loads(args.tag_types.read_text(encoding="utf-8"))
    tags = next(t for t in types if t["full_name"] == "UnityEngine.NAPRenderPipeline0.ShaderTagIds")
    initializer = args.tag_initializer.read_text(encoding="utf-8").splitlines()
    identities = []
    for field in tags["fields"]:
        rva = 0x5360740 + field["metadata_offset"]
        stores = [index for index, line in enumerate(initializer) if f"RIP_RVA={rva:#x}" in line]
        if field["storage_flags"] != 2 or len(stores) != 1:
            raise ValueError("ShaderTagId static storage lacks a unique initializer")
        index = stores[0]
        literal_rva = int(re.search(r"RIP_RVA=(0x[0-9a-f]+)", initializer[index - 2])[1], 16)
        pointer = reader.qword(session.base + literal_rva)
        length = reader.uint(pointer + 16)
        name = reader.read(pointer + 20, length * 2).decode("utf-16-le")
        raw = reader.read(session.base + rva, 4)
        identities.append({"field": field["name"], "name": name, "gameassembly_rva": hex(rva),
                           "id": struct.unpack("<I", raw)[0], "raw_hex": raw.hex(),
                           "initializer": initializer[index - 2:index + 1]})
    by_id = {row["id"]: row["name"] for row in identities}
    asm = args.draw_asm.read_text(encoding="utf-8")
    accesses = []
    for line in asm.splitlines():
        match = re.search(r"RIP_RVA=(0x1f22[0-9a-f]+)", line)
        if not match:
            continue
        rva = int(match[1], 16)
        raw = reader.read(player_base + rva, 4)
        value = struct.unpack("<I", raw)[0]
        accesses.append({"instruction": line, "native_rva": hex(rva), "raw_hex": raw.hex(),
                         "value": value, "registered_name_match": by_id.get(value)})
    property_names = ["_IsStencilReceiverPass", "_CharacterStencilReadMask", "_CharacterStencilWriteMask",
                      "_CharacterStencilComp", "_CharacterStencilPass", "_StencilShadowStencilRef",
                      "_StencilShadowStencil", "_StencilShadowBlendDebugMode", "_IsBlackCanvasOn",
                      "_BlendSrcFactor", "_BlendDstFactor", "_BlendSrcFactorMV", "_BlendDstFactorMV",
                      "_CharacterStencil", "_CharacterStencilRef", "_ZTestPreZ", "_ZWritePreZ"]
    property_identities = [dict(name=name, **reader.name_id(name)) for name in property_names]
    property_ids = {item["id"]: item["name"] for item in property_identities if item["status"] == "registered"}
    state_inputs = []
    for path in args.state_asm:
        for line in path.read_text(encoding="utf-8").splitlines():
            match = re.search(r"RIP_RVA=(0x[0-9a-f]+)", line)
            if not match:
                continue
            rva = int(match[1], 16)
            raw = reader.read(player_base + rva, 4)
            integer, scalar = struct.unpack("<I", raw)[0], struct.unpack("<f", raw)[0]
            state_inputs.append({"source": str(path), "instruction": line, "raw_hex": raw.hex(),
                                 "uint_view": integer, "float_view": scalar,
                                 "property_name_match": property_ids.get(integer) if "lea " in line else None})
    sources = [Path(__file__), args.renderer_snapshot, args.tag_types, args.tag_initializer, args.draw_asm, *args.state_asm, IMAGE,
               Path(player["path"]), session.heap.binary_path, session.heap.map_path,
               session.source.binary_path, session.source.map_path]
    args.output.mkdir(parents=True, exist_ok=False)
    write_json(args.output / "outline-dispatch.json", {
        "session": "829", "live_process_read": False, "atomic_frame_capture": False,
        "meaning": "ShaderTagId initializer strings and typed static IDs matched against native draw global operands. Shader property IDs are a separate registry. This does not prove a branch executed in the captured frame.",
        "identities": identities, "native_accesses": accesses, "state_inputs": state_inputs,
        "property_identities": property_identities, "reads": reader.reads,
        "sources": [{"path": str(path), "sha256": sha(path)} for path in sources]})
    print(json.dumps({"native_accesses": accesses}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
