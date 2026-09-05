import argparse
import json
import struct
from pathlib import Path


FORMATS = {3: "RGB24", 4: "RGBA32", 10: "DXT1", 17: "RGBAHalf", 25: "BC7"}


def parse_texture(path):
    data = path.read_bytes()
    cursor = 0

    def read(kind):
        nonlocal cursor
        size = struct.calcsize("<" + kind)
        value = struct.unpack_from("<" + kind, data, cursor)
        cursor += size
        return value[0] if len(value) == 1 else value

    def align():
        nonlocal cursor
        cursor = (cursor + 3) & ~3

    def string():
        nonlocal cursor
        length = read("i")
        value = data[cursor:cursor + length].decode("utf-8")
        cursor += length
        align()
        return value

    name = string()
    forced_fallback = read("i")
    downscale_fallback = read("?")
    align()
    width, height, complete_size, texture_format, mip_count = read("5i")
    readable = read("?")
    preprocessed = read("?")
    ignore_master_limit = read("?")
    streaming_mipmaps = read("?")
    align()
    streaming_priority = read("i")
    compressed = read("?")
    align()
    image_count, dimension = read("2i")
    filter_mode, aniso = read("2i")
    mip_bias = read("f")
    wrap_u, wrap_v, wrap_w = read("3i")
    lightmap_format, color_space = read("2i")
    inline_size = read("i")
    if inline_size:
        stream_offset = cursor
        stream_size = inline_size
        stream_path = ""
        cursor += inline_size
    else:
        external_mip_index = read("I")
        stream_offset, stream_size = read("2I")
        stream_path = string()
        if external_mip_index != 0:
            raise ValueError(f"Unsupported external mip index in {path}")
    if cursor != len(data):
        raise ValueError(f"Texture serialization was not consumed exactly: {path}")
    return {
        "Name": name,
        "SerializedObject": str(path.resolve()),
        "SerializedBytes": len(data),
        "ForcedFallbackFormat": forced_fallback,
        "DownscaleFallback": downscale_fallback,
        "Width": width,
        "Height": height,
        "CompleteImageBytes": complete_size,
        "FormatValue": texture_format,
        "Format": FORMATS.get(texture_format, str(texture_format)),
        "MipCount": mip_count,
        "Readable": readable,
        "Preprocessed": preprocessed,
        "IgnoreMasterTextureLimit": ignore_master_limit,
        "StreamingMipmaps": streaming_mipmaps,
        "StreamingPriority": streaming_priority,
        "Compressed": compressed,
        "ImageCount": image_count,
        "Dimension": dimension,
        "FilterMode": filter_mode,
        "Aniso": aniso,
        "MipBias": mip_bias,
        "WrapU": wrap_u,
        "WrapV": wrap_v,
        "WrapW": wrap_w,
        "LightmapFormat": lightmap_format,
        "ColorSpace": color_space,
        "StreamOffset": stream_offset,
        "StreamBytes": stream_size,
        "StreamPath": stream_path
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--serialized-root", type=Path, required=True)
    parser.add_argument("--raw-manifest", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        raise ValueError("Output file already exists")
    raw = json.loads(args.raw_manifest.read_text(encoding="utf-8"))
    names = {row["Name"] for row in raw["Textures"]}
    parsed = {}
    for path in args.serialized_root.rglob("*.dat"):
        if path.stem not in names:
            continue
        record = parse_texture(path)
        if record["Name"] in parsed:
            raise ValueError(f"Duplicate serialized texture {record['Name']}")
        parsed[record["Name"]] = record
    if set(parsed) != names:
        raise ValueError("Serialized texture set differs from raw image set")
    rows = []
    for source in raw["Textures"]:
        serialized = parsed[source["Name"]]
        for field in ("Width", "Height", "Format", "MipCount", "FilterMode", "Aniso", "MipBias"):
            if source[field] != serialized[field]:
                raise ValueError(f"Texture field mismatch: {source['Name']} {field}")
        if source["Bytes"] != serialized["CompleteImageBytes"] or source["Bytes"] != serialized["StreamBytes"]:
            raise ValueError(f"Texture byte length mismatch: {source['Name']}")
        rows.append({**source, **serialized})
    result = {**raw, "Schema": "zzz-texture-source-contract/1", "Textures": rows}
    args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"output": str(args.output.resolve()), "textures": len(rows),
                      "formats": sorted({row["Format"] for row in rows}),
                      "colorSpaces": sorted({row["ColorSpace"] for row in rows})}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
