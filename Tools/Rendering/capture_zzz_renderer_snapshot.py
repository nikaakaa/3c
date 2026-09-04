import argparse
import hashlib
import json
import struct
import sys
from pathlib import Path


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--metadata-dir", type=Path, required=True)
    parser.add_argument("--unity-player", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--decompiler", type=Path, required=True)
    args = parser.parse_args()
    sys.dont_write_bytecode = True
    sys.path.insert(0, str(args.metadata_dir.resolve()))
    from export_gameplay_metadata import GameplaySession, IMAGE, IMAGE_SHA256, sha
    from offline_sources import PEImage
    from recover_zzz_shader import chunks, decompile, disassemble

    if sha(IMAGE) != IMAGE_SHA256:
        raise ValueError("GameAssembly identity mismatch")
    player_sha = sha(args.unity_player)
    if player_sha != "66493677ce2739e188c41634230c3a35e5f584c4575650949d0a53b5785c6c44":
        raise ValueError("UnityPlayer identity mismatch")
    session = GameplaySession("829")
    player = PEImage(args.unity_player)
    args.output.mkdir(parents=True, exist_ok=False)
    reads = []

    def read(address, size):
        raw = session.read(address, size)
        reads.append({"address": hex(address), "size": size, "raw_hex": raw.hex()})
        return raw

    def qword(address):
        return struct.unpack("<Q", read(address, 8))[0]

    static_base = qword(session.base + 0x5360730)
    renderer = qword(static_base + 0x21248)
    config_array = qword(renderer + 0x178)
    config_header = read(config_array, 32)
    count = struct.unpack_from("<I", config_header, 24)[0]
    if count != 39:
        raise ValueError("ShaderConfig count differs from the verified snapshot")
    config_data = read(config_array + 32, count * 16)
    configs = []
    for index in range(count):
        name_pointer, features = struct.unpack_from("<QI", config_data, index * 16)
        length = struct.unpack("<I", read(name_pointer + 16, 4))[0]
        name = read(name_pointer + 20, length * 2).decode("utf-16-le")
        configs.append({"index": index, "name": name, "features": features})

    name_function = qword(session.base + 0x540BE00)
    name_code = read(name_function, 64)
    file_offset = player.data.find(name_code)
    if file_offset < 0 or player.data.find(name_code, file_offset + 1) >= 0:
        raise ValueError("Object.GetName code does not identify a unique UnityPlayer function")
    section = next(row for row in player.sections if row[2] <= file_offset < row[2] + row[1])
    name_rva = section[0] + file_offset - section[2]
    player_base = name_function - name_rva
    if name_rva != 0xBE3680 or player.read(0xB1BB10, 13) != bytes.fromhex("4885c9750333c0c3488b4110c3"):
        raise ValueError("Native object getter does not match the inspected instruction chain")
    manager = qword(static_base + 0x21918)
    managed_compute = qword(manager + 0x48)
    native_compute = qword(managed_compute + 0x10)
    vtable = qword(native_compute)
    get_name = qword(vtable + 0x58)
    if player.read(get_name - player_base, 5) != bytes.fromhex("488b4138c3"):
        raise ValueError("ComputeShader name getter layout differs")
    name_address = qword(native_compute + 0x38)
    name_raw = read(name_address, 64).split(b"\0", 1)[0]
    if name_raw != b"NapEntityPrepare":
        raise ValueError("Unexpected ComputeShader identity")
    platform_array = qword(native_compute + 0x40)
    platform_count = qword(native_compute + 0x50)
    platform_data = read(platform_array, platform_count * 80)
    renderer_kind = struct.unpack_from("<I", platform_data)[0]
    kernel_array = struct.unpack_from("<Q", platform_data, 8)[0]
    kernel_count = struct.unpack_from("<Q", platform_data, 24)[0]
    if renderer_kind != 2 or kernel_count != 3:
        raise ValueError("D3D11 platform/kernel identity differs")
    kernels = []
    for index in range(kernel_count):
        header = read(kernel_array + index * 0xD8, 0xD8)
        address = struct.unpack_from("<Q", header, 0xA8)[0]
        size = struct.unpack_from("<Q", header, 0xB8)[0]
        group = struct.unpack_from("<III", header, 0xC8)
        code = read(address, size)
        if code[:4] != b"DXBC" or struct.unpack_from("<I", code, 24)[0] != size or group != (64, 1, 1):
            raise ValueError("Kernel header/bytecode mismatch")
        asm = disassemble(code)
        if "cs_5_0" not in asm or "dcl_thread_group 64, 1, 1" not in asm:
            raise ValueError("Kernel profile/group declaration mismatch")
        stem = args.output / f"kernel{index}"
        stem.with_suffix(".dxbc").write_bytes(code)
        stem.with_suffix(".asm").write_text(asm, encoding="utf-8")
        stem.with_suffix(".decompiled.hlsl").write_text(decompile(code, args.decompiler), encoding="utf-8")
        kernels.append({"index": index, "size": size, "address": hex(address), "thread_group": group,
                        "sha256": hashlib.sha256(code).hexdigest(), "chunks": chunks(code)})
    sources = [IMAGE, args.unity_player, args.decompiler, Path(__file__), args.metadata_dir / "session_829.json",
               session.heap.binary_path, session.heap.map_path, session.source.binary_path, session.source.map_path]
    result = {"session": "829", "live_process_read": False, "gameassembly_base": hex(session.base),
              "unityplayer_base": hex(player_base), "get_name_rva": hex(name_rva), "renderer_va": hex(renderer),
              "shader_configs": configs, "manager_va": hex(manager), "managed_compute_va": hex(managed_compute),
              "native_compute_va": hex(native_compute), "compute_name": name_raw.decode(), "kernels": kernels,
              "sources": [{"path": str(p), "sha256": sha(p)} for p in sources], "reads": reads}
    (args.output / "renderer_snapshot.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"shader_configs": len(configs), "compute": name_raw.decode(), "kernels": kernels}, indent=2))


if __name__ == "__main__":
    main()
