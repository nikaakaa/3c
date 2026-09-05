import argparse
import hashlib
import json
import struct
import time
from pathlib import Path

import numpy as np


PAGE_SIZE = 4096
ADDRESS_MASK = 0x000FFFFFFFFFF000


def scan_parents(path, target_pages, byte_limit, level):
    target_pfns = np.array(sorted(page // PAGE_SIZE for page in target_pages), dtype=np.uint64)
    links = []
    chunk_size = 256 * 1024 * 1024
    started = time.time()
    with path.open("rb") as stream:
        offset = 0
        while offset < byte_limit:
            data = stream.read(min(chunk_size, byte_limit - offset))
            if not data:
                break
            words = np.frombuffer(data, dtype="<u8")
            present = (words & np.uint64(1)) != 0
            if level > 0:
                present &= (words & np.uint64(0x80)) == 0
            pfns = (words >> np.uint64(12)) & np.uint64(0xFFFFFFFFFF)
            target_mask = pfns == target_pfns[0] if len(target_pfns) == 1 else np.isin(pfns, target_pfns)
            indices = np.nonzero(present & target_mask)[0]
            for index in indices:
                absolute = offset + int(index) * 8
                target_page = int(pfns[index]) * PAGE_SIZE
                links.append({"parentPage": absolute & ~(PAGE_SIZE - 1),
                              "entryIndex": (absolute & (PAGE_SIZE - 1)) // 8,
                              "targetPage": target_page,
                              "entry": int(words[index])})
            offset += len(data)
            if offset % (2 * 1024 ** 3) < chunk_size:
                print(json.dumps({"level": level, "scannedGiB": round(offset / 1024 ** 3, 2),
                                  "targets": len(target_pages), "links": len(links),
                                  "seconds": round(time.time() - started, 1)}), flush=True)
    unique = {(link["parentPage"], link["entryIndex"], link["targetPage"]): link for link in links}
    return list(unique.values())


def read_physical(stream, size, physical, count):
    if physical < 0 or physical + count > size:
        return None
    stream.seek(physical)
    data = stream.read(count)
    return data if len(data) == count else None


def walk(stream, size, cr3, virtual):
    table = cr3 & ADDRESS_MASK
    for level, shift in enumerate((39, 30, 21, 12)):
        raw = read_physical(stream, size, table + ((virtual >> shift) & 0x1FF) * 8, 8)
        if raw is None:
            return None
        entry = struct.unpack("<Q", raw)[0]
        if not entry & 1:
            return None
        if level == 1 and entry & 0x80:
            return (entry & 0x000FFFFFC0000000) + (virtual & 0x3FFFFFFF)
        if level == 2 and entry & 0x80:
            return (entry & 0x000FFFFFFFE00000) + (virtual & 0x1FFFFF)
        table = entry & ADDRESS_MASK
    return table + (virtual & 0xFFF)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dump", type=Path, required=True)
    parser.add_argument("--physical", type=lambda value: int(value, 0), required=True)
    parser.add_argument("--expected-va-min", type=lambda value: int(value, 0), required=True)
    parser.add_argument("--expected-va-max", type=lambda value: int(value, 0), required=True)
    parser.add_argument("--translate", type=lambda value: int(value, 0), nargs="+", default=[])
    parser.add_argument("--scan-gib", type=int, default=8)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        raise ValueError("Output directory already exists")
    size = args.dump.stat().st_size
    byte_limit = min(size, args.scan_gib * 1024 ** 3)
    physical_page = args.physical & ~(PAGE_SIZE - 1)
    levels = []
    targets = {physical_page}
    for level in range(4):
        links = scan_parents(args.dump, targets, byte_limit, level)
        levels.append(links)
        targets = {link["parentPage"] for link in links}
        if not targets:
            break
    paths = [{"targetPage": physical_page, "indices": [], "pages": []}]
    for links in levels:
        expanded = []
        by_target = {}
        for link in links:
            by_target.setdefault(link["targetPage"], []).append(link)
        for path in paths:
            child = path["pages"][-1] if path["pages"] else path["targetPage"]
            for link in by_target.get(child, []):
                expanded.append({"targetPage": path["targetPage"],
                                 "indices": path["indices"] + [link["entryIndex"]],
                                 "pages": path["pages"] + [link["parentPage"]]})
        paths = expanded
    candidates = []
    if len(levels) == 4:
        with args.dump.open("rb") as stream:
            for path in paths:
                pt, pd, pdpt, pml4 = path["indices"]
                virtual = (pml4 << 39) | (pdpt << 30) | (pd << 21) | (pt << 12) | (args.physical & 0xFFF)
                if not args.expected_va_min <= virtual <= args.expected_va_max:
                    continue
                cr3 = path["pages"][-1]
                if walk(stream, size, cr3, virtual) != args.physical:
                    continue
                translated = []
                for address in args.translate:
                    physical = walk(stream, size, cr3, address)
                    raw = read_physical(stream, size, physical, 512) if physical is not None else None
                    translated.append({"virtual": hex(address), "physical": hex(physical) if physical is not None else None,
                                       "bytes": len(raw) if raw is not None else 0,
                                       "sha256": hashlib.sha256(raw).hexdigest() if raw is not None else None,
                                       "hex": raw.hex() if raw is not None else None})
                candidates.append({"cr3": hex(cr3), "mappedVirtual": hex(virtual),
                                   "mappedPhysical": hex(args.physical), "indices": path["indices"],
                                   "translated": translated})
    args.output.mkdir(parents=True)
    report = {"schemaVersion": 1, "dump": str(args.dump.resolve()), "dumpBytes": size,
              "scanBytes": byte_limit, "liveProcessRead": False, "sourcePhysical": hex(args.physical),
              "expectedVirtualRange": [hex(args.expected_va_min), hex(args.expected_va_max)],
              "levels": levels, "candidates": candidates}
    (args.output / "page-table-resolution.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"output": str(args.output), "levelLinks": [len(level) for level in levels],
                      "candidates": candidates}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
