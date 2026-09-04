import argparse
import base64
import json
import struct
import sys
from pathlib import Path

from capture_zzz_shader_parameters import SnapshotProperties
from recover_zzz_shader import sha256, write_json


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--metadata-dir", type=Path, required=True)
    parser.add_argument("--renderer-snapshot", type=Path, required=True)
    parser.add_argument("--bindings", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    sys.dont_write_bytecode = True
    sys.path.insert(0, str(args.metadata_dir.resolve()))
    from export_gameplay_metadata import GameplaySession, IMAGE, IMAGE_SHA256, sha
    from offline_sources import PEImage

    snapshot = json.loads(args.renderer_snapshot.read_text(encoding="utf-8"))
    player_source = next(p for p in snapshot["sources"] if Path(p["path"]).name == "UnityPlayer.dll")
    if sha(IMAGE) != IMAGE_SHA256 or sha(Path(player_source["path"])) != player_source["sha256"]:
        raise ValueError("Module identity differs from the inspected snapshot")
    session = GameplaySession("829")
    player = PEImage(Path(player_source["path"]))
    player_base = int(snapshot["unityplayer_base"], 16)
    reader = SnapshotProperties(session, player_base)
    statics = reader.qword(session.base + 0x5360730)
    materials = reader.qword(statics + 136096)
    if reader.uint(materials + 24) != 3:
        raise ValueError("LUT material array differs")
    material = reader.qword(materials + 32 + 2 * 8)
    native_material = reader.qword(material + 16)
    material_id = struct.unpack("<i", reader.read(native_material + 8, 4))[0]
    native_lut_material_id = struct.unpack("<i", reader.read(player_base + 0x1E68F60 + 0xF8 + 3 * 4, 4))[0]
    if material_id != native_lut_material_id:
        raise ValueError("Character pass does not refer to the selected LUT material")
    cooked = reader.qword(native_material + 0x240)
    if not cooked or not reader.qword(cooked + 0x10) or not (reader.uint(cooked + 0x15C) & 1):
        raise ValueError("Material cooked properties are not ready")
    reader.use_sheet(cooked + 0x18, "character-lut-material-cooked-cache")
    bindings = json.loads(args.bindings.read_text(encoding="utf-8"))
    parameters, absent = [], []
    for parameter in bindings["parameters"]:
        if parameter["kind"] != "vector" or parameter["m_ArraySize"]:
            raise ValueError("Unexpected LUT parameter shape")
        value = reader.value(parameter["name"], 0 if parameter["m_Dim"] == 1 else 1)
        if value["status"] == "published":
            parameters.append({"name": parameter["name"], "values": value["float_view"], "evidence": value})
        else:
            absent.append(value)
    if [v["name"] for v in absent] != ["_UserLut_Params"]:
        raise ValueError("Unexpected missing character LUT inputs")

    owner = reader.qword(statics + 21456)
    if reader.qword(reader.qword(owner) + 0x58) != session.tables["types"] + 5726 * 80:
        raise ValueError("Character LUT pass owner identity differs")
    textures, curve_bindings = [], []
    indices = {}
    for offset, name in enumerate(("_CurveBlue", "_CurveGreen", "_CurveHueVsHue", "_CurveHueVsSat",
                                   "_CurveLumVsSat", "_CurveMaster", "_CurveRed", "_CurveSatVsSat")):
        texture = reader.qword(owner + 176 + offset * 8)
        native = reader.qword(texture + 16)
        image = reader.qword(native + 0x60)
        vtable = reader.qword(image)
        getter_expectations = ((0, bytes.fromhex("488b41104803c2c3")),
                               (0x28, bytes.fromhex("8b4138c3")),
                               (0x38, bytes.fromhex("488b4140c3")))
        for slot, expected in getter_expectations:
            getter_rva = reader.qword(vtable + slot) - player_base
            if player.read(getter_rva, len(expected)) != expected:
                raise ValueError("Image data/format/size getter differs")
        width, height, format_id = struct.unpack("<3I", reader.read(image + 0x30, 12))
        size = reader.qword(image + 0x40)
        if (width, height, format_id, size) != (128, 1, 15, 256):
            raise ValueError("Curve texture is not the evidenced 128x1 RHalf image")
        pixels = reader.read(reader.qword(image + 0x10), size)
        if native not in indices:
            indices[native] = len(textures)
            textures.append({"name": f"CorinCurve{len(textures)}", "width": width, "height": height,
                "format": format_id, "filterMode": reader.uint(native + 0x40),
                "wrapMode": reader.uint(native + 0x4C),
                "native": hex(native), "managed": hex(texture), "image": hex(image),
                "rawBase64": base64.b64encode(pixels).decode("ascii"), "sha256": sha256(pixels),
                "samples": list(struct.unpack("<128e", pixels))})
        curve_bindings.append({"name": name, "textureIndex": indices[native]})

    args.output.mkdir(parents=True, exist_ok=False)
    result = {"schemaVersion": 1, "name": "Corin829CustomHdrLut", "lutSize": 32,
        "keyword": "_TONEMAP_CUSTOM", "userLutEnabled": False,
        "parameters": parameters, "absentProperties": absent, "textures": textures,
        "curveBindings": curve_bindings, "material": hex(material), "materialInstanceId": material_id,
        "owner": hex(owner), "nativeMaterial": hex(native_material), "cookedProperties": hex(cooked),
        "session": "829", "live_process_read": False, "atomic_frame_capture": False,
        "reads": reader.reads,
        "sources": [{"path": str(p), "sha256": sha(p)} for p in [Path(__file__), args.renderer_snapshot,
            args.bindings, IMAGE, Path(player_source["path"]), session.heap.binary_path, session.heap.map_path,
            session.source.binary_path, session.source.map_path]]}
    write_json(args.output / "character-lut.json", result)
    print(json.dumps({"output": str(args.output), "parameters": len(parameters),
                      "curveTextures": len(textures), "curveBindings": len(curve_bindings)}, indent=2))


if __name__ == "__main__":
    main()
