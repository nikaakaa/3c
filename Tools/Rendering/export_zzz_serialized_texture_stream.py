import argparse
import hashlib
import json
from pathlib import Path

from inspect_zzz_texture_serialization import parse_texture


def sha(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1 << 20), b""):
            digest.update(block)
    return digest.hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--serialized-object", type=Path, required=True)
    parser.add_argument("--source-assets", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    source = args.source_assets.resolve()
    texture = parse_texture(args.serialized_object.resolve())
    if args.output.exists():
        raise ValueError("Output directory already exists")
    if Path(texture["Name"]).name != texture["Name"]:
        raise ValueError("Texture name must be a filename")
    if texture["StreamPath"]:
        stream_path = source.parent / texture["StreamPath"]
        if stream_path.resolve().parent != source.parent:
            raise ValueError("Texture stream is outside the source asset directory")
    else:
        stream_path = args.serialized_object.resolve()
    with stream_path.open("rb") as stream:
        stream.seek(texture["StreamOffset"])
        pixels = stream.read(texture["StreamBytes"])
    if len(pixels) != texture["StreamBytes"] or len(pixels) != texture["CompleteImageBytes"]:
        raise ValueError("Texture stream length differs from serialized metadata")
    output_name = texture["Name"] + ".bytes"
    record = {
        **texture,
        "SourceAssets": str(source),
        "SourceAssetsSha256": sha(source),
        "SourceStream": str(stream_path.resolve()),
        "SourceStreamSha256": sha(stream_path),
        "SerializedSha256": sha(args.serialized_object),
        "Bytes": len(pixels),
        "Sha256": hashlib.sha256(pixels).hexdigest(),
        "OutputFile": output_name,
    }
    report = {"Schema": "zzz-texture-source-contract/1", "Textures": [record]}
    args.output.mkdir(parents=True, exist_ok=False)
    (args.output / output_name).write_bytes(pixels)
    (args.output / "texture-raw-export.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
