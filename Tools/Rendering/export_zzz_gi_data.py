import argparse
import csv
import hashlib
import json
import struct
import sys
from pathlib import Path


def digest(path):
    value = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1 << 20), b""):
            value.update(chunk)
    return value.hexdigest()


def floats(data, offset, count):
    return list(struct.unpack_from("<" + "f" * count, data, offset))


def read_array(source, address, stride, decoder=None):
    result = {"address": hex(address) if address else "0x0", "available": False}
    if not address:
        result.update({"available": True, "count": 0, "items": []})
        return result
    try:
        header = source.read(address, 32)
        count = struct.unpack_from("<i", header, 24)[0]
        result["headerRawHex"] = header.hex()
        result["count"] = count
        if count < 0 or count > 1_000_000:
            result["reason"] = "invalid-count"
            return result
        if stride is None:
            result.update({"available": True, "payloadReadable": False,
                           "reason": "element-stride-not-exported"})
            return result
        payload = source.read(address + 32, count * stride)
    except Exception as error:
        result["reason"] = str(error)
        return result
    result["available"] = True
    result["payloadSha256"] = hashlib.sha256(payload).hexdigest()
    result["items"] = [decoder(payload, index * stride) if decoder else payload[index * stride:(index + 1) * stride].hex()
                        for index in range(count)]
    return result


def decode_int(data, offset):
    return struct.unpack_from("<i", data, offset)[0]


def decode_position(data, offset):
    return floats(data, offset, 3)


def decode_gi(source, address):
    raw = source.read(address, 112)
    arrays = {
        "tileDataOffsets": read_array(source, struct.unpack_from("<Q", raw, 80)[0], 4, decode_int),
        "positions": read_array(source, struct.unpack_from("<Q", raw, 88)[0], 12, decode_position),
        "weathers": read_array(source, struct.unpack_from("<Q", raw, 96)[0], None)
    }
    return {
        "address": hex(address),
        "rawSha256": hashlib.sha256(raw).hexdigest(),
        "rawHex": raw.hex(),
        "class": hex(struct.unpack_from("<Q", raw, 0)[0]),
        "enabled": bool(raw[16]),
        "settings": hex(struct.unpack_from("<Q", raw, 24)[0]),
        "rect": floats(raw, 32, 4),
        "rectForCulling": floats(raw, 48, 4),
        "rowSize": struct.unpack_from("<i", raw, 64)[0],
        "colSize": struct.unpack_from("<i", raw, 68)[0],
        "tileCount": struct.unpack_from("<i", raw, 72)[0],
        "currentWeatherIndex": struct.unpack_from("<i", raw, 104)[0],
        "arrays": arrays,
        "activePayload": bool(raw[16]) and struct.unpack_from("<i", raw, 72)[0] > 0
    }


def decode_manager(source, address):
    raw = source.read(address, 40)
    buffers = []
    for name, offset in (("giPositionBuffer", 16), ("giIndexBuffer", 24), ("giColorBuffer", 32)):
        wrapper_address = struct.unpack_from("<Q", raw, offset)[0]
        item = {"name": name, "wrapper": hex(wrapper_address), "available": False}
        if wrapper_address:
            try:
                wrapper = source.read(wrapper_address, 32)
                native_address = struct.unpack_from("<Q", wrapper, 16)[0]
                item.update({"available": True, "wrapperRawHex": wrapper.hex(), "native": hex(native_address)})
                if native_address:
                    native = source.read(native_address, 512)
                    item.update({"nativeHeaderRawSha256": hashlib.sha256(native).hexdigest(),
                                 "nativeHeaderRawHex": native.hex()})
            except Exception as error:
                item["reason"] = str(error)
        buffers.append(item)
    return {"address": hex(address), "rawSha256": hashlib.sha256(raw).hexdigest(),
            "rawHex": raw.hex(), "class": hex(struct.unpack_from("<Q", raw, 0)[0]),
            "buffers": buffers, "gpuPayloadAvailable": False}


def decode_bake_result(source, address):
    raw = source.read(address, 416)
    values = floats(raw, 264, 27)
    channels = {name: values[index * 9:(index + 1) * 9] for index, name in enumerate(("R", "G", "B"))}
    return {
        "address": hex(address),
        "class": hex(struct.unpack_from("<Q", raw, 0)[0]),
        "rawSha256": hashlib.sha256(raw).hexdigest(),
        "bakeResultOffset": 264,
        "bakeResultRawHex": raw[264:372].hex(),
        "coefficientsChannelMajor": values,
        "coefficients": channels,
        "hasFlushBakeResult": bool(raw[372]),
        "shaderConfigs": hex(struct.unpack_from("<Q", raw, 376)[0]),
        "opaqueLayerMask": struct.unpack_from("<i", raw, 384)[0],
        "transparentLayerMask": struct.unpack_from("<i", raw, 388)[0]
    }


def readable_report(result):
    current = result["currentDump"]["gi"]
    old = result["session829"]
    renderer = old["rendererData"]
    lines = [
        "# ZZZ 可琳 GI 与球谐数据导出",
        "",
        "本报告只整理离线快照；当前 dump 与 829 页快照不是同一帧，不能拼成同一时刻的输入。",
        "",
        "## 结论",
        "",
        f"当前 dump 的 NapCharacterGI：enabled={current['enabled']}，rowSize={current['rowSize']}，colSize={current['colSize']}，tileCount={current['tileCount']}。三份 GI 数组均为 0 项，因此这份当前 dump 没有可直接导出的有效 GI 采样点。",
        "",
        "829 页快照的 NapCharacterGI 同样是 disabled、0×0×0；它的 GI Manager 只有 ComputeBuffer 对象头，显存 payload 不在可读的 CPU 快照中。",
        "",
        "829 RendererData 的 bakeResult 是 27 个 float 的 SphericalHarmonicsL2，不是球谐贴图。当前导出的三个颜色通道数值完全相同；这只能作为该快照的 baked 字段，不能当作所有场景的动态 GI。",
        "",
        "## 当前 dump GI",
        "",
        "| 字段 | 值 |",
        "| --- | --- |",
        f"| 地址 | `{result['currentDump']['gi']['address']}` |",
        f"| enabled | `{current['enabled']}` |",
        f"| rect / rectForCulling | `{current['rect']}` / `{current['rectForCulling']}` |",
        f"| rowSize / colSize / tileCount | `{current['rowSize']}` / `{current['colSize']}` / `{current['tileCount']}` |",
        f"| tileDataOffsets / positions / weathers | `{current['arrays']['tileDataOffsets']['count']}` / `{current['arrays']['positions']['count']}` / `{current['arrays']['weathers']['count']}` |",
        f"| settings 指针 | `{current['settings']}`（当前 dump 不驻留） |",
        "",
        "## SphericalHarmonicsL2（829）",
        "",
        "系数按 R/G/B 三个通道、每通道 9 项保存；原始 108 字节见 `bake-result-spherical-harmonics-l2.f32`。",
        "",
        "| 系数索引 | R | G | B |",
        "| --- | ---: | ---: | ---: |"
    ]
    for index in range(9):
        lines.append(f"| {index} | {renderer['coefficients']['R'][index]:.9g} | {renderer['coefficients']['G'][index]:.9g} | {renderer['coefficients']['B'][index]:.9g} |")
    lines.extend([
        "",
        "## 仍缺的原始数据",
        "",
        "- 当前有效场景的 GI 采样数组：positions、tileDataOffsets、weathers 的非零内容。",
        "- 三个 GI ComputeBuffer 的实际显存内容，而不只是 ComputeBuffer/native 对象头。",
        "- 若场景使用独立 GI 纹理，需要该资源的对象引用、格式、尺寸和像素；本次快照没有证明存在一张“球谐贴图”。",
        "- GI 对应的场景和时间身份；不能把 829 的 baked SH 直接写成当前 Scene 的全局常量。",
        "",
        "## 来源",
        "",
        f"- 当前 dump：`{result['currentDump']['path']}`，大小 {result['currentDump']['bytes']} 字节，CR3 `{result['currentDump']['cr3']}`，GI 对象原始字节 SHA-256 `{current['rawSha256']}`。",
        f"- 829 页快照：`{old['source']}`，模块基址 `{old['moduleBase']}`。",
        f"- GameAssembly SHA-256 `{result['moduleIdentity']['gameAssemblySha256']}`；UnityPlayer SHA-256 `{result['moduleIdentity']['unityPlayerSha256']}`。",
        "- 所有读取均为离线读取，未访问运行中进程。"
    ])
    return "\n".join(lines) + "\n"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dump", type=Path, required=True)
    parser.add_argument("--cr3", type=lambda value: int(value, 0), required=True)
    parser.add_argument("--gi-address", type=lambda value: int(value, 0), required=True)
    parser.add_argument("--metadata-dir", type=Path, required=True)
    parser.add_argument("--metadata-gi-address", type=lambda value: int(value, 0), required=True)
    parser.add_argument("--metadata-manager-address", type=lambda value: int(value, 0), required=True)
    parser.add_argument("--renderer-address", type=lambda value: int(value, 0), required=True)
    parser.add_argument("--hash-dump", action="store_true")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        raise ValueError("Output directory already exists")
    sys.dont_write_bytecode = True
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    sys.path.insert(0, str(args.metadata_dir.resolve()))
    from export_gameplay_metadata import GameplaySession
    from trace_zzz_dump_render_graph import virtual_read

    source_session = GameplaySession("829")
    with args.dump.open("rb") as stream:
        dump_size = args.dump.stat().st_size

        class DumpSource:
            def read(self, address, count):
                data = virtual_read(stream, dump_size, args.cr3, address, count)
                if data is None:
                    raise ValueError(f"unmapped dump address: {address:#x}")
                return data

        dump_source = DumpSource()
        current_gi = decode_gi(dump_source, args.gi_address)
        current_raw = dump_source.read(args.gi_address, 112)

    metadata_gi = decode_gi(source_session.source, args.metadata_gi_address)
    metadata_manager = decode_manager(source_session.source, args.metadata_manager_address)
    renderer = decode_bake_result(source_session.source, args.renderer_address)
    renderer_raw = source_session.source.read(args.renderer_address, 416)

    args.output.mkdir(parents=True)
    (args.output / "current-gi-object.bin").write_bytes(current_raw)
    (args.output / "metadata-renderer-data.bin").write_bytes(renderer_raw)
    values = renderer["coefficientsChannelMajor"]
    (args.output / "bake-result-spherical-harmonics-l2.f32").write_bytes(struct.pack("<27f", *values))
    with (args.output / "bake-result-spherical-harmonics-l2.csv").open("w", encoding="utf-8-sig", newline="") as stream:
        writer = csv.writer(stream)
        writer.writerow(["coefficientIndex", "R", "G", "B"])
        for index in range(9):
            writer.writerow([index, renderer["coefficients"]["R"][index],
                             renderer["coefficients"]["G"][index], renderer["coefficients"]["B"][index]])
    result = {
        "schemaVersion": 1,
        "scope": "ZZZ可琳渲染光照离线导出；不同快照分开，不能合并为同帧",
        "liveProcessRead": False,
        "atomicFrameCapture": False,
        "moduleIdentity": {
            "gameAssemblySha256": "4cba5d52c5fbfd478d2a9ec217075f82216780d56ad1bd1e85e4f724dcce30b4",
            "unityPlayerSha256": "66493677ce2739e188c41634230c3a35e5f584c4575650949d0a53b5785c6c44"
        },
        "currentDump": {
            "path": str(args.dump.resolve()),
            "bytes": dump_size,
            "cr3": hex(args.cr3),
            "sha256": digest(args.dump) if args.hash_dump else None,
            "hashComputed": args.hash_dump,
            "gi": current_gi,
            "rawFile": "current-gi-object.bin"
        },
        "session829": {
            "source": str(source_session.source.binary_path.resolve()),
            "sourceSha256": "8897e2b6c83b4a3c6011ac4f0082e0c923f93ad15b9b392cd661d2e90e8b5c9d",
            "moduleBase": hex(source_session.base),
            "gi": metadata_gi,
            "manager": metadata_manager,
            "rendererData": renderer,
            "rendererRawFile": "metadata-renderer-data.bin",
            "sphericalHarmonicsRawFile": "bake-result-spherical-harmonics-l2.f32"
        },
        "interpretation": {
            "currentGIActive": current_gi["activePayload"],
            "currentGIArraysHavePayload": any(item["available"] and item.get("count", 0) > 0
                                               for item in current_gi["arrays"].values()),
            "sphericalHarmonicsIsTexture": False,
            "managerGpuPayloadAvailable": False,
            "missing": ["当前帧有效GI数组", "ComputeBuffer显存payload", "GI纹理像素（若场景另有该资源）"]
        }
    }
    (args.output / "gi-export.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    (args.output / "GI-可读报告.md").write_text(readable_report(result), encoding="utf-8")
    print(json.dumps({"output": str(args.output), "currentGI": current_gi,
                      "metadataGI": metadata_gi, "sphericalHarmonics": renderer["coefficients"]},
                     ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
