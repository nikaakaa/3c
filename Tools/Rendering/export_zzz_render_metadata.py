import argparse
import csv
import importlib
import json
import struct
import sys
from pathlib import Path


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--metadata-dir", type=Path, required=True)
    parser.add_argument("--types", type=int, nargs="+", required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--matcap-statics", action="store_true")
    parser.add_argument("--native-evidence", type=Path)
    args = parser.parse_args()
    sys.dont_write_bytecode = True
    sys.path.insert(0, str(args.metadata_dir.resolve()))
    exporter = importlib.import_module("export_gameplay_metadata")
    if exporter.sha(exporter.IMAGE) != exporter.IMAGE_SHA256:
        raise ValueError("PE does not match the offline metadata decoder")
    catalog_path = args.metadata_dir / "控制器与战斗/catalog_829.json"
    catalog = json.loads(catalog_path.read_text(encoding="utf-8"))
    by_index = {row["type_index"]: row for row in catalog["types"]}
    session = exporter.GameplaySession("829")
    errors = {}
    records = [exporter.exported_type(session, by_index, index, ["ZZZ渲染生产者定点离线核对"], errors)
               for index in args.types]
    args.output.mkdir(parents=True, exist_ok=False)
    if args.matcap_statics:
        owner = next(r for r in records if r["type_index"] == 26804)
        reads = []

        def read(address, length):
            raw = session.read(address, length)
            reads.append({"va": hex(address), "bytes": length, "hex": raw.hex()})
            return raw

        def pointer(address):
            return struct.unpack("<Q", read(address, 8))[0]

        def array(address, stride):
            header = read(address, 32)
            count = struct.unpack_from("<I", header, 24)[0]
            if count > 64:
                raise ValueError("MatCap static array exceeds the inspected layout")
            return count, read(address + 32, count * stride)

        static_base = pointer(session.base + 0x5360730)
        id_arrays, arrays_by_pointer = {}, {}
        for field in owner["fields"]:
            if field["static"] and field["type"]["display"] == "int[]":
                address = pointer(static_base + field["metadata_offset"])
                count, raw = array(address, 4)
                id_arrays[field["name"]] = {"va": hex(address), "ids": list(struct.unpack("<" + "i" * count, raw))}
                arrays_by_pointer[address] = field["name"]
        vector_pointer = pointer(static_base + 0x23368)
        count, raw = array(vector_pointer, 16)
        vectors = [{"target_id": struct.unpack_from("<i", raw, i * 16 + 8)[0],
                    "source_array": arrays_by_pointer[struct.unpack_from("<Q", raw, i * 16)[0]]}
                   for i in range(count)]
        pack_pointer = pointer(static_base + 0x23348)
        count, raw = array(pack_pointer, 40)
        packed = [{"target_id": struct.unpack_from("<i", raw, i * 40 + 32)[0],
                   "xyzw_source_arrays": [arrays_by_pointer[struct.unpack_from("<Q", raw, i * 40 + offset)[0]]
                                           for offset in (8, 24, 16, 0)]}
                  for i in range(count)]
        literals = []
        if args.native_evidence:
            evidence = json.loads(args.native_evidence.read_text(encoding="utf-8"))
            string_type = next(r for r in catalog["types"] if r["full_name"] == "System.String")
            rvas = sorted({int(r["rva"], 16) for f in evidence["functions"] if f["rva"] == "0x1c4ca280"
                           for r in f["rip_references"] if 0x57D0000 <= int(r["rva"], 16) < 0x57E0000})
            for rva in rvas:
                address = pointer(session.base + rva)
                header = read(address, 20)
                klass = struct.unpack_from("<Q", header)[0]
                definition = pointer(klass + 0x58)
                if definition != session.tables["types"] + string_type["type_index"] * 80:
                    raise ValueError(f"Literal {rva:#x} does not refer to the System.String definition")
                length = struct.unpack_from("<I", header, 16)[0]
                text = read(address + 20, length * 2).decode("utf-16-le")
                literals.append({"slot_rva": hex(rva), "object_va": hex(address), "value": text})
        statics = {"session": "829", "live_process_access": False, "static_base": hex(static_base),
                   "id_arrays": id_arrays, "vector_properties": vectors, "float_pack_settings": packed,
                   "literals": literals, "raw_reads": reads}
        (args.output / "matcap_static_evidence.json").write_text(json.dumps(statics, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    (args.output / "types.json").write_text(json.dumps(records, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    with (args.output / "methods.csv").open("w", encoding="utf-8-sig", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=["owner_index", "owner", "method_index", "method", "parameters", "RVA"])
        writer.writeheader()
        for owner in records:
            for method in owner["methods"]:
                writer.writerow({"owner_index": owner["type_index"], "owner": owner["full_name"],
                    "method_index": method["definition_index"], "method": method.get("name"),
                    "parameters": ", ".join(p.get("type", {}).get("display", "?") + " " + p.get("name", "?") for p in method.get("parameters", [])),
                    "RVA": hex(method["rva"]) if method.get("rva") is not None else ""})
    sources = [Path(__file__), catalog_path, exporter.IMAGE, args.metadata_dir / "export_gameplay_metadata.py",
               args.metadata_dir / "export_metadata.py", args.metadata_dir / "offline_sources.py",
               args.metadata_dir / "session_829.json",
               session.heap.binary_path, session.heap.map_path, session.source.binary_path, session.source.map_path]
    if args.native_evidence:
        sources.append(args.native_evidence)
    report = {"session": "829", "live_process_access": False, "module_base": hex(session.base),
              "types": [{"index": r["type_index"], "name": r["full_name"], "fields": len(r["fields"]),
                         "methods": len(r["methods"]), "methods_with_rva": sum(m.get("rva") is not None for m in r["methods"]),
                         "issues": r["issues"]} for r in records], "descriptor_errors": errors,
              "sources": [{"path": str(p), "bytes": p.stat().st_size, "sha256": exporter.sha(p)} for p in sources]}
    (args.output / "metadata_evidence.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report["types"], ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
