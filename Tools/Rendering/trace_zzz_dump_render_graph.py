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


def int32(data, offset):
    return struct.unpack_from("<i", data, offset)[0]


def map_target_pages(stream, size, cr3, targets, va_min, va_max):
    mappings = {page: [] for page in targets}
    pml4_start = (va_min >> 39) & 0x1FF
    pml4_end = ((va_max - 1) >> 39) & 0x1FF
    for pml4_index in range(pml4_start, pml4_end + 1):
        pml4e = entry(stream, size, cr3 & ADDRESS_MASK, pml4_index)
        if not pml4e & 1:
            continue
        pdpt_table = pml4e & ADDRESS_MASK
        for pdpt_index in range(512):
            prefix = (pml4_index << 39) | (pdpt_index << 30)
            if prefix + (1 << 30) <= va_min or prefix >= va_max:
                continue
            pdpte = entry(stream, size, pdpt_table, pdpt_index)
            if not pdpte & 1:
                continue
            if pdpte & 0x80:
                base = pdpte & 0x000FFFFFC0000000
                for target in targets:
                    if base <= target < base + (1 << 30):
                        mappings[target].append(prefix + target - base)
                continue
            pd_table = pdpte & ADDRESS_MASK
            for pd_index in range(512):
                pd_prefix = prefix | (pd_index << 21)
                if pd_prefix + (1 << 21) <= va_min or pd_prefix >= va_max:
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
                for pt_index, pte in enumerate(struct.unpack("<512Q", raw)):
                    page = pte & ADDRESS_MASK
                    if pte & 1 and page in mappings:
                        mappings[page].append(pd_prefix | (pt_index << 12))
    return mappings


def read_array(stream, size, cr3, address, max_count=4096):
    if address == 0:
        return {"address": "0x0", "available": True, "count": 0, "items": []}
    header = virtual_read(stream, size, cr3, address, 32)
    if header is None:
        return {"address": hex(address), "available": False, "reason": "header-page-not-resident"}
    count = qword(header, 24)
    if count > max_count:
        return {"address": hex(address), "available": False, "reason": "invalid-count", "count": count}
    raw = virtual_read(stream, size, cr3, address + 32, count * 8)
    if raw is None:
        return {"address": hex(address), "available": False, "reason": "items-page-not-resident", "count": count}
    return {"address": hex(address), "available": True, "count": count,
            "items": [qword(raw, index * 8) for index in range(count)]}


def read_list(stream, size, cr3, address, max_count=4096):
    if address == 0:
        return {"address": "0x0", "available": True, "count": 0, "items": []}
    raw = virtual_read(stream, size, cr3, address, 32)
    if raw is None:
        return {"address": hex(address), "available": False, "reason": "list-page-not-resident"}
    items_address = qword(raw, 16)
    count = int32(raw, 24)
    if count < 0 or count > max_count:
        return {"address": hex(address), "available": False, "reason": "invalid-count", "count": count}
    array = read_array(stream, size, cr3, items_address, max_count)
    if not array["available"]:
        return {"address": hex(address), "available": False, "reason": array["reason"], "count": count,
                "itemsAddress": hex(items_address)}
    if array["count"] < count:
        return {"address": hex(address), "available": False, "reason": "backing-array-too-small",
                "count": count, "capacity": array["count"], "itemsAddress": hex(items_address)}
    return {"address": hex(address), "available": True, "count": count,
            "capacity": array["count"], "itemsAddress": hex(items_address), "items": array["items"][:count]}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dump", type=Path, required=True)
    parser.add_argument("--cr3", type=lambda value: int(value, 0), required=True)
    parser.add_argument("--entity-report", type=Path, required=True)
    parser.add_argument("--renderer-scan", type=Path, required=True)
    parser.add_argument("--renderer-class", type=lambda value: int(value, 0), required=True)
    parser.add_argument("--va-min", type=lambda value: int(value, 0), required=True)
    parser.add_argument("--va-max", type=lambda value: int(value, 0), required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        raise ValueError("Output directory already exists")
    size = args.dump.stat().st_size
    scan = json.loads(args.renderer_scan.read_text(encoding="utf-8"))
    pointer = next(row for row in scan["pointers"] if int(row["value"], 16) == args.renderer_class)
    physical_hits = [int(value, 16) for value in pointer["hits"]]
    target_pages = {value & ~(PAGE_SIZE - 1) for value in physical_hits}
    entity_report = json.loads(args.entity_report.read_text(encoding="utf-8"))
    with args.dump.open("rb") as stream:
        mappings = map_target_pages(stream, size, args.cr3, target_pages, args.va_min, args.va_max)
        renderer_wrappers = []
        wrapper_by_va = {}
        for physical in physical_hits:
            page = physical & ~(PAGE_SIZE - 1)
            for virtual_page in mappings[page]:
                virtual = virtual_page | (physical & (PAGE_SIZE - 1))
                raw = virtual_read(stream, size, args.cr3, virtual, 32)
                if raw is None or qword(raw, 0) != args.renderer_class:
                    continue
                row = {"virtual": hex(virtual), "physical": hex(physical),
                       "nativeObject": hex(qword(raw, 16)), "referencedBy": []}
                renderer_wrappers.append(row)
                wrapper_by_va[virtual] = row
        entities = []
        referenced_renderers = set()
        for source in entity_report["objects"]:
            entity_va = int(source["virtual"], 16)
            entity = source["napRenderEntity"]
            renderer_array = read_array(stream, size, args.cr3, int(entity["rendererData"], 16), 1024)
            result = {"virtual": source["virtual"], "physical": source["physical"],
                      "released": bool(virtual_read(stream, size, args.cr3, entity_va + 0x59, 1)[0]),
                      "rootGameObject": entity["rootGameObject"], "napCBuffer": entity["napCBuffer"],
                      "middlePointPosition": entity["middlePointPosition"],
                      "headBonePosition": entity["headBonePosition"],
                      "rendererArray": {key: value for key, value in renderer_array.items() if key != "items"},
                      "renderers": []}
            if renderer_array["available"]:
                for renderer_va in renderer_array["items"]:
                    renderer_result = {"virtual": hex(renderer_va)}
                    raw = virtual_read(stream, size, args.cr3, renderer_va, 160) if renderer_va else None
                    if raw is None:
                        renderer_result.update({"available": False, "reason": "object-page-not-resident"})
                    else:
                        managed_renderer = qword(raw, 16)
                        nap_materials = read_list(stream, size, args.cr3, qword(raw, 40), 128)
                        shared_materials = read_list(stream, size, args.cr3, qword(raw, 104), 128)
                        renderer_result.update({
                            "available": True,
                            "class": hex(qword(raw, 0)),
                            "managedRenderer": hex(managed_renderer),
                            "managedRendererMapped": managed_renderer in wrapper_by_va,
                            "entityBackReference": hex(qword(raw, 48)),
                            "gameObject": hex(qword(raw, 56)),
                            "napMaterials": {key: value for key, value in nap_materials.items() if key != "items"},
                            "napMaterialObjects": [hex(value) for value in nap_materials.get("items", [])],
                            "sharedMaterialsPerFrame": {key: value for key, value in shared_materials.items() if key != "items"},
                            "sharedMaterialObjects": [hex(value) for value in shared_materials.get("items", [])],
                            "propertyMode": int32(raw, 120),
                            "flags": hex(struct.unpack_from("<I", raw, 128)[0]),
                            "nativeHandle": hex(qword(raw, 144))
                        })
                        if managed_renderer:
                            referenced_renderers.add(managed_renderer)
                            if managed_renderer in wrapper_by_va:
                                wrapper_by_va[managed_renderer]["referencedBy"].append(
                                    {"entity": source["virtual"], "napRenderer": hex(renderer_va)})
                    result["renderers"].append(renderer_result)
            entities.append(result)
    args.output.mkdir(parents=True)
    referenced_mapped = sum(1 for value in referenced_renderers if value in wrapper_by_va)
    report = {
        "schemaVersion": 1,
        "dump": str(args.dump.resolve()),
        "cr3": hex(args.cr3),
        "liveProcessRead": False,
        "rendererClass": hex(args.renderer_class),
        "rendererClassPhysicalHits": len(physical_hits),
        "rendererWrappersMapped": len(renderer_wrappers),
        "referencedManagedRenderers": len(referenced_renderers),
        "referencedManagedRenderersMapped": referenced_mapped,
        "rendererWrappers": renderer_wrappers,
        "entities": entities
    }
    (args.output / "render-graph.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    summary = {
        "output": str(args.output),
        "rendererClassPhysicalHits": len(physical_hits),
        "rendererWrappersMapped": len(renderer_wrappers),
        "referencedManagedRenderers": len(referenced_renderers),
        "referencedManagedRenderersMapped": referenced_mapped,
        "entities": [{"virtual": row["virtual"], "released": row["released"],
                      "rendererCount": row["rendererArray"].get("count"),
                      "rendererArrayAvailable": row["rendererArray"]["available"],
                      "resolvedRenderers": sum(1 for renderer in row["renderers"] if renderer["available"]),
                      "mappedUnityRenderers": sum(1 for renderer in row["renderers"]
                                                  if renderer.get("managedRendererMapped")),
                      "middlePointPosition": row["middlePointPosition"]}
                     for row in entities]
    }
    print(json.dumps(summary, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
