import argparse
import hashlib
import json
import time
from pathlib import Path


def digest(data):
    return hashlib.sha256(data).hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dump", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--text", nargs="+", required=True)
    parser.add_argument("--max-hits", type=int, default=4096)
    args = parser.parse_args()
    if args.output.exists():
        raise ValueError("Output directory already exists")
    size = args.dump.stat().st_size
    needles = {value: value.encode("utf-8") for value in args.text}
    hits = {value: [] for value in args.text}
    chunk_size = 256 * 1024 * 1024
    overlap_size = max(len(value) for value in needles.values()) - 1
    started = time.time()
    with args.dump.open("rb") as stream:
        first = stream.read(1024 * 1024)
        stream.seek(max(0, size - 1024 * 1024))
        last = stream.read(1024 * 1024)
        stream.seek(0)
        offset = 0
        overlap = b""
        while offset < size:
            data = stream.read(min(chunk_size, size - offset))
            if not data:
                break
            block = overlap + data
            block_offset = offset - len(overlap)
            for value, needle in needles.items():
                cursor = 0
                while len(hits[value]) < args.max_hits:
                    index = block.find(needle, cursor)
                    if index < 0:
                        break
                    hits[value].append(block_offset + index)
                    cursor = index + 1
            offset += len(data)
            overlap = block[-overlap_size:] if overlap_size else b""
            if offset % (2 * 1024 * 1024 * 1024) < chunk_size:
                print(json.dumps({"scannedGiB": round(offset / 1024 ** 3, 2),
                                  "hits": {key: len(value) for key, value in hits.items()},
                                  "seconds": round(time.time() - started, 1)}), flush=True)
    args.output.mkdir(parents=True)
    report = {
        "schemaVersion": 1,
        "dump": str(args.dump.resolve()),
        "dumpBytes": size,
        "dumpModifiedNs": args.dump.stat().st_mtime_ns,
        "firstMiBSha256": digest(first),
        "lastMiBSha256": digest(last),
        "liveProcessRead": False,
        "strings": [{"value": value, "hits": [hex(offset) for offset in hits[value]]}
                    for value in args.text],
        "elapsedSeconds": time.time() - started
    }
    (args.output / "string-scan.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"output": str(args.output),
                      "hits": {key: len(value) for key, value in hits.items()},
                      "elapsedSeconds": round(report["elapsedSeconds"], 1)}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
