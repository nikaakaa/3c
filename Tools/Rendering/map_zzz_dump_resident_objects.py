import argparse
import json
import struct
from pathlib import Path


PAGE_SIZE = 4096
ADDRESS_MASK = 0x000FFFFFFFFFF000


def physical_read(stream, size, address, count):
    if address < 0 or address + count > size:
        return None
    stream.seek(address)
    data = stream.read(count)
    return data if len(data) == count else None


def entry(stream, size, table, index):
    raw = physical_read(stream, size, table + index * 8, 8)
    return struct.unpack("<Q", raw)[0] if raw is not None else 0


def walk(stream, size, cr3, virtual):
    table = cr3 & ADDRESS_MASK
    for level, shift in enumerate((39, 30, 21, 12)):
        value = entry(stream, size, table, (virtual >> shift) & 0x1FF)
        if not value & 1:
            return None
        if level == 1 and value & 0x80:
            return (value & 0x000FFFFFC0000000) + (virtual & 0x3FFFFFFF)
        if level == 2 and value & 0x80:
            return (value & 0x000FFFFFFFE00000) + (virtual & 0x1FFFFF)
        table = value & ADDRESS_MASK
    return table + (virtual & 0xFFF)


def virtual_read(stream, size, cr3, virtual, count):
    result = bytearray()
    while len(result) < count:
        current = virtual + len(result)
        physical = walk(stream, size, cr3, current)
        if physical is None:
            return None
        length = min(count - len(result), PAGE_SIZE - (current & (PAGE_SIZE - 1)))
        data = physical_read(stream, size, physical, length)
        if data is None:
            return None
        result.extend(data)
    return bytes(result)


def qword(data, offset):
    return struct.unpack_from("<Q", data, offset)[0]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dump", type=Path, required=True)
    parser.add_argument("--cr3", type=lambda value: int(value, 0), required=True)
    parser.add_argument("--scan", type=Path, required=True)
    parser.add_argument("--pointer", type=lambda value: int(value, 0), required=True)
    parser.add_argument("--va-min", type=lambda value: int(value, 0), required=True)
    parser.add_argument("--va-max", type=lambda value: int(value, 0), required=True)
    parser.add_argument("--object-bytes", type=int, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        raise ValueError("Output directory already exists")
    scan = json.loads(args.scan.read_text(encoding="utf-8"))
    pointer = next(row for row in scan["pointers"] if int(row["value"], 16) == args.pointer)
    offsets = [int(value, 16) for value in pointer["hits"]]
    targets = {offset & ~(PAGE_SIZE - 1) for offset in offsets}
    size = args.dump.stat().st_size
    mappings = {page: [] for page in targets}
    with args.dump.open("rb") as stream:
        pml4_start = (args.va_min >> 39) & 0x1FF
        pml4_end = ((args.va_max - 1) >> 39) & 0x1FF
        for pml4_index in range(pml4_start, pml4_end + 1):
            pml4e = entry(stream, size, args.cr3 & ADDRESS_MASK, pml4_index)
            if not pml4e & 1:
                continue
            pdpt_table = pml4e & ADDRESS_MASK
            for pdpt_index in range(512):
                prefix = (pml4_index << 39) | (pdpt_index << 30)
                if prefix + (1 << 30) <= args.va_min or prefix >= args.va_max:
                    continue
                pdpte = entry(stream, size, pdpt_table, pdpt_index)
                if not pdpte & 1 or pdpte & 0x80:
                    continue
                pd_table = pdpte & ADDRESS_MASK
                for pd_index in range(512):
                    pd_prefix = prefix | (pd_index << 21)
                    if pd_prefix + (1 << 21) <= args.va_min or pd_prefix >= args.va_max:
                        continue
                    pde = entry(stream, size, pd_table, pd_index)
                    if not pde & 1:
                        continue
                    if pde & 0x80:
                        base = pde & 0x000FFFFFFFE00000
                        for target in targets:
                            if base <= target < base + (1 << 21):
                                mappings[target].append(pd_prefix + target - base)
                        continue
                    pt_table = pde & ADDRESS_MASK
                    raw = physical_read(stream, size, pt_table, PAGE_SIZE)
                    if raw is None:
                        continue
                    values = struct.unpack("<512Q", raw)
                    for pt_index, pte in enumerate(values):
                        if pte & 1 and (pte & ADDRESS_MASK) in targets:
                            mappings[pte & ADDRESS_MASK].append(pd_prefix | (pt_index << 12))
        objects = []
        for physical in offsets:
            page = physical & ~(PAGE_SIZE - 1)
            for virtual_page in mappings[page]:
                virtual = virtual_page | (physical & (PAGE_SIZE - 1))
                data = virtual_read(stream, size, args.cr3, virtual, args.object_bytes)
                if data is None or qword(data, 0) != args.pointer:
                    continue
                objects.append({"virtual": hex(virtual), "physical": hex(physical),
                                "rawHex": data.hex(),
                                "napRenderEntity": {
                                    "rootGameObject": hex(qword(data, 24)),
                                    "monoRenderEntity": hex(qword(data, 32)),
                                    "flags": struct.unpack_from("<I", data, 68)[0],
                                    "napCBuffer": hex(qword(data, 96)),
                                    "middlePointTransform": hex(qword(data, 104)),
                                    "headBone": hex(qword(data, 112)),
                                    "rootBone": hex(qword(data, 120)),
                                    "rendererData": hex(qword(data, 304)),
                                    "indicatedLights": hex(qword(data, 312)),
                                    "middlePointPosition": list(struct.unpack_from("<3f", data, 192)),
                                    "headBonePosition": list(struct.unpack_from("<3f", data, 204)),
                                    "headBoneForward": list(struct.unpack_from("<3f", data, 216))
                                }})
    args.output.mkdir(parents=True)
    report = {"schemaVersion": 1, "dump": str(args.dump.resolve()), "cr3": hex(args.cr3),
              "scan": str(args.scan.resolve()), "classPointer": hex(args.pointer),
              "virtualRange": [hex(args.va_min), hex(args.va_max)], "liveProcessRead": False,
              "physicalHits": len(offsets), "mappedObjects": len(objects),
              "pageMappings": {hex(page): [hex(value) for value in values] for page, values in mappings.items()},
              "objects": objects}
    (args.output / "resident-objects.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"output": str(args.output), "physicalHits": len(offsets),
                      "mappedObjects": len(objects),
                      "objects": [{"virtual": row["virtual"], **row["napRenderEntity"]} for row in objects]},
                     ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
