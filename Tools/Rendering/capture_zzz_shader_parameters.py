import argparse
import json
import struct
import sys
from pathlib import Path

from recover_zzz_shader import write_json


class SnapshotProperties:
    def __init__(self, session, player_base):
        self.session = session
        self.player_base = player_base
        self.reads = []
        self.registry = self.qword(player_base + 0x1F23190)
        self.bucket_base = self.qword(self.registry)
        self.bucket_mask = self.uint(self.registry + 8)
        self.use_sheet(self.qword(player_base + 0x1F22AA0) + 0x88, "global")

    def use_sheet(self, address, source):
        self.sheet = address
        self.sheet_source = source
        self.sheet_header = self.read(address, 0xA0)
        self.key_count = struct.unpack_from("<I", self.sheet_header, 0x50)[0]
        value_count = struct.unpack_from("<I", self.sheet_header, 0x70)[0]
        if self.key_count != value_count:
            raise ValueError("Property sheet key/value counts differ")
        self.keys = self.read(self.qword(address + 0x40), self.key_count * 4)
        self.descriptors = self.read(self.qword(address + 0x60), self.key_count * 4)
        self.payload = self.qword(address + 0x80)

    def read(self, address, size):
        raw = self.session.read(address, size)
        self.reads.append({"address": hex(address), "bytes": size, "raw_hex": raw.hex()})
        return raw

    def uint(self, address):
        return struct.unpack("<I", self.read(address, 4))[0]

    def qword(self, address):
        return struct.unpack("<Q", self.read(address, 8))[0]

    def name_id(self, name):
        encoded = name.encode("utf-8")
        hashed = 0x811C9DC5
        for byte in encoded:
            hashed = ((hashed ^ byte) * 0x1000193) & 0xFFFFFFFF
        index, step = hashed & self.bucket_mask, 8
        probes = []
        for _ in range((self.bucket_mask + 8) // 8):
            address = self.bucket_base + index * 3
            raw = self.read(address, 24)
            tag = struct.unpack_from("<I", raw)[0]
            probes.append(hex(address))
            if tag == (hashed & 0xFFFFFFFC):
                pointer = struct.unpack_from("<Q", raw, 8)[0]
                if self.read(pointer, len(encoded) + 1) == encoded + b"\0":
                    return {"status": "registered", "id": struct.unpack_from("<I", raw, 16)[0],
                            "bucket": hex(address), "probes": probes}
            if tag == 0xFFFFFFFF:
                return {"status": "not_registered", "probes": probes}
            index = (index + step) & self.bucket_mask
            step += 8
        raise ValueError("Name lookup exhausted the original table's probe space")

    def value(self, name, kind, array_size=0):
        identity = self.name_id(name)
        result = {"name": name, "getter_kind": kind, "identity": identity,
                  "sheet_address": hex(self.sheet), "sheet_source": self.sheet_source}
        if identity["status"] != "registered":
            result["status"] = "not_registered"
            return result
        property_id = identity["id"]
        if self.sheet_source == "global" and property_id & 0xE0000000:
            result.update(status="thread_local_storage_required", packed_id=hex(property_id))
            return result
        start, end = struct.unpack_from("<ii", self.sheet_header, 0x10 + kind * 4)
        if not 0 <= start <= end <= self.key_count:
            raise ValueError("Typed property range is invalid")
        matches = [index for index in range(start, end)
                   if struct.unpack_from("<I", self.keys, index * 4)[0] == property_id]
        if not matches:
            result["status"] = "not_published_in_snapshot"
            return result
        if len(matches) != 1:
            raise ValueError("Duplicate property identity in the typed range")
        index = matches[0]
        descriptor = struct.unpack_from("<I", self.descriptors, index * 4)[0]
        offset = descriptor & 0xFFFFF
        stored_count = (descriptor >> 20) & 0x3FF
        if array_size and kind not in (1, 2):
            raise ValueError("Only the evidenced vector and matrix array layouts are supported")
        count = stored_count if array_size else 1
        if count < (array_size or 1):
            result.update(status="published_array_too_short", stored_count=stored_count)
            return result
        size = (4, 16, 64)[kind] * count
        address = self.payload + offset
        raw = self.read(address, size)
        result.update(status="published", index=index, descriptor=hex(descriptor), stored_count=stored_count,
                      payload_address=hex(address), raw_hex=raw.hex(),
                      float_view=list(struct.unpack("<" + "f" * (size // 4), raw)),
                      uint_view=list(struct.unpack("<" + "I" * (size // 4), raw)))
        return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--metadata-dir", type=Path, required=True)
    parser.add_argument("--renderer-snapshot", type=Path, required=True)
    parser.add_argument("--bindings", type=Path, nargs="+", required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    sys.dont_write_bytecode = True
    sys.path.insert(0, str(args.metadata_dir.resolve()))
    from export_gameplay_metadata import GameplaySession, IMAGE, IMAGE_SHA256, sha

    snapshot = json.loads(args.renderer_snapshot.read_text(encoding="utf-8"))
    if snapshot["session"] != "829" or sha(IMAGE) != IMAGE_SHA256:
        raise ValueError("Snapshot/module identity mismatch")
    player = next(p for p in snapshot["sources"] if Path(p["path"]).name == "UnityPlayer.dll")
    if sha(Path(player["path"])) != player["sha256"]:
        raise ValueError("UnityPlayer differs from the proven snapshot module")
    session = GameplaySession("829")
    if int(snapshot["gameassembly_base"], 16) != session.base:
        raise ValueError("GameAssembly base differs from the proven snapshot")
    args.output.mkdir(parents=True, exist_ok=False)
    reader = SnapshotProperties(session, int(snapshot["unityplayer_base"], 16))
    required = {}
    for path in args.bindings:
        binding = json.loads(path.read_text(encoding="utf-8"))
        for parameter in binding["parameters"]:
            if parameter["buffer"] in ("UnityPerMaterial", "UnityPerDraw", "UnityNapCB"):
                continue
            kind = 2 if parameter["kind"] == "matrix" else 0 if parameter["m_Dim"] == 1 else 1
            key = parameter["name"], kind, parameter["m_ArraySize"]
            required.setdefault(key, []).append({"binding": str(path.resolve()), "declaration": parameter})
    values = []
    for (name, kind, array_size), uses in sorted(required.items()):
        value = reader.value(name, kind, array_size)
        value["uses"] = uses
        values.append(value)

    statics = reader.qword(session.base + 0x5360730)
    manager = reader.qword(statics + 20576)
    array = reader.qword(manager + 48)
    count = reader.uint(array + 24)
    if count != 31:
        raise ValueError("GlobalRT configuration count differs")
    names = ("_GBuffer0", "_GBuffer1", "_GBuffer2", "_CameraNormalTexture")
    targets = []
    for index, name in enumerate(names):
        owner = reader.qword(array + 32 + index * 8)
        klass = reader.qword(owner)
        if reader.qword(klass + 0x58) != session.tables["types"] + 6028 * 80:
            raise ValueError("Render target wrapper type differs")
        raw = reader.read(owner + 16, 84)
        identity = reader.name_id(name)
        configured_id = struct.unpack_from("<i", raw, 48)[0]
        if identity.get("id") != configured_id:
            raise ValueError("Render target order/name does not match the property registry")
        targets.append({"index": index, "name": name, "wrapper": hex(owner), "property_id": configured_id,
            "render_texture_format": struct.unpack_from("<i", raw)[0],
            "graphics_format": struct.unpack_from("<i", raw, 4)[0],
            "real_rt_handle": struct.unpack_from("<i", raw, 52)[0],
            "last_created_width": struct.unpack_from("<i", raw, 56)[0],
            "last_created_height": struct.unpack_from("<i", raw, 60)[0]})
    files = [Path(__file__), args.renderer_snapshot, IMAGE, Path(player["path"]), session.heap.binary_path, session.heap.map_path,
             session.source.binary_path, session.source.map_path, *args.bindings]
    result = {"session": "829", "live_process_read": False, "atomic_frame_capture": False,
              "properties": values, "render_targets": targets,
              "reads": reader.reads, "sources": [{"path": str(p), "sha256": sha(p)} for p in files]}
    write_json(args.output / "shader-parameters.json", result)
    counts = {status: sum(v["status"] == status for v in values) for status in sorted({v["status"] for v in values})}
    print(json.dumps({"output": str(args.output), "properties": counts, "render_targets": targets}, indent=2))


if __name__ == "__main__":
    main()
