import argparse
import hashlib
import json
import struct
from pathlib import Path

from trace_zzz_dump_render_graph import int32, qword, virtual_read


def vector(data, offset, count):
    return list(struct.unpack_from("<" + "f" * count, data, offset))


def read_list_header(stream, size, cr3, address):
    raw = virtual_read(stream, size, cr3, address, 32)
    if raw is None:
        raise ValueError(f"List header is not resident: {address:#x}")
    return qword(raw, 16), int32(raw, 24)


def decode_input(data):
    return {
        "position": vector(data, 0, 3),
        "forceInShadow": vector(data, 12, 1)[0],
        "faceForward": vector(data, 16, 3),
        "blendLightStart": int32(data, 28),
        "facePosition": vector(data, 32, 3),
        "blendLightEnd": int32(data, 44),
        "mainLightData": vector(data, 48, 4),
        "toonLightIndices": vector(data, 64, 4),
        "toonLightCount": int32(data, 80),
        "isGPUCrowd": vector(data, 84, 1)[0],
        "indexChanged": vector(data, 88, 1)[0],
        "directionalLightSize": vector(data, 92, 1)[0],
        "worldToObjectMatrix": vector(data, 96, 16),
        "cameraPositionWS": vector(data, 160, 3),
        "isFirstTimeCalculateRadian": int32(data, 172),
        "overridenMainLightColor": vector(data, 176, 4),
        "lockLightAngleRatio": vector(data, 192, 1)[0],
        "fixShadowCoverageOutOfFrustum": vector(data, 196, 1)[0],
        "disableLightBlendShadowTemporalFade": vector(data, 200, 1)[0],
        "dummy": vector(data, 204, 1)[0]
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dump", type=Path, required=True)
    parser.add_argument("--cr3", type=lambda value: int(value, 0), required=True)
    parser.add_argument("--manager", type=lambda value: int(value, 0), required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        raise ValueError("Output directory already exists")
    size = args.dump.stat().st_size
    with args.dump.open("rb") as stream:
        manager = virtual_read(stream, size, args.cr3, args.manager, 256)
        if manager is None:
            raise ValueError("Manager is not resident")
        gpu_buffer_wrapper = qword(manager, 80)
        gpu_buffer_object = virtual_read(stream, size, args.cr3, gpu_buffer_wrapper, 32)
        if gpu_buffer_object is None:
            raise ValueError("ComputeBuffer wrapper is not resident")
        gpu_buffer_native = qword(gpu_buffer_object, 16)
        gpu_buffer_native_bytes = virtual_read(stream, size, args.cr3, gpu_buffer_native, 512)
        native_children = []
        if gpu_buffer_native_bytes is not None:
            for offset in range(0, 256, 8):
                address = qword(gpu_buffer_native_bytes, offset)
                if 0x40000000000 <= address < 0x80000000000:
                    child = virtual_read(stream, size, args.cr3, address, 512)
                    native_children.append((offset, address, child))
        input_list = qword(manager, 96)
        visible_list = qword(manager, 120)
        input_array, input_count = read_list_header(stream, size, args.cr3, input_list)
        visible_array, visible_count = read_list_header(stream, size, args.cr3, visible_list)
        input_bytes = virtual_read(stream, size, args.cr3, input_array + 32, input_count * 208)
        visible_bytes = virtual_read(stream, size, args.cr3, visible_array + 32, visible_count * 8)
        if input_bytes is None or visible_bytes is None:
            raise ValueError("List payload is not resident")
    args.output.mkdir(parents=True)
    if gpu_buffer_native_bytes is not None:
        (args.output / "entity-gpu-buffer-native-head.bin").write_bytes(gpu_buffer_native_bytes)
    child_records = []
    for offset, address, child in native_children:
        path = None
        if child is not None:
            path = args.output / f"native-child-{offset:03x}-{address:x}.bin"
            path.write_bytes(child)
        child_records.append({"sourceOffset": offset, "address": hex(address), "resident": child is not None,
                              "path": str(path.resolve()) if path else None})
    records = []
    for index in range(input_count):
        raw = input_bytes[index * 208:(index + 1) * 208]
        path = args.output / f"prepare-input-{index}.bin"
        path.write_bytes(raw)
        records.append({
            "index": index,
            "entity": hex(qword(visible_bytes, index * 8)) if index < visible_count else None,
            "sha256": hashlib.sha256(raw).hexdigest(),
            "path": str(path.resolve()),
            "values": decode_input(raw)
        })
    report = {
        "schemaVersion": 1,
        "dump": str(args.dump.resolve()),
        "cr3": hex(args.cr3),
        "manager": hex(args.manager),
        "entityGpuDataBufferWrapper": hex(gpu_buffer_wrapper),
        "entityGpuDataBufferNative": hex(gpu_buffer_native),
        "entityGpuDataBufferNativeHeadResident": gpu_buffer_native_bytes is not None,
        "nativeChildren": child_records,
        "prepareInputList": hex(input_list),
        "visibleEntityList": hex(visible_list),
        "prepareInputCount": input_count,
        "visibleEntityCount": visible_count,
        "records": records
    }
    (args.output / "entity-gpu-input.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
