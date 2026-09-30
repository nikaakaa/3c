import argparse
import json
from collections import Counter
from pathlib import Path


FIELD_NAMES = {
    "m_Mesh": "mesh",
    "m_Mesh1": "mesh1",
    "m_Mesh2": "mesh2",
    "m_Mesh3": "mesh3",
}


def slot_status(dependency):
    if dependency["pathId"] == 0:
        return "empty"
    if dependency["scope"] == "external":
        return "external"
    if dependency["scope"] == "local":
        return "unresolvedLocal"
    return "unresolvedInvalidFileId"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--renderer-data", type=Path, required=True)
    parser.add_argument("--archive-map", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    renderer_data = json.loads(
        args.renderer_data.read_text(encoding="utf-8")
    )
    archive_data = json.loads(args.archive_map.read_text(encoding="utf-8"))
    archive_targets = {}
    for renderer_key, references in archive_data["entries"].items():
        for reference in references:
            key = (reference["field"], reference["meshPathId"])
            target = {
                "meshName": reference["meshName"],
                "blockId": reference["blockId"],
                "objPath": reference["objPath"],
            }
            previous = archive_targets.get(key)
            if previous is not None and previous != target:
                raise RuntimeError(f"archive mesh target conflict: {key}")
            archive_targets[key] = target

    entries = {}
    status_counts = Counter()
    for record in renderer_data["records"]:
        dependencies = {
            dependency["field"]: dependency
            for dependency in record["dependencies"]
            if dependency["field"] in FIELD_NAMES
        }
        slots = {}
        for source_field, target_field in FIELD_NAMES.items():
            dependency = dependencies.get(source_field) or {
                "field": source_field,
                "fileId": 0,
                "pathId": 0,
                "scope": "local",
            }
            status = slot_status(dependency)
            status_counts[(target_field, status)] += 1
            slot = {
                "status": status,
                "fileId": dependency["fileId"],
                "pathId": dependency["pathId"],
            }
            if status == "external":
                target = archive_targets[(source_field, dependency["pathId"])]
                slot.update(target)
            slots[target_field] = slot

        renderer_key = f"{record['cab']}:{record['pathId']}"
        entries[renderer_key] = {
            "renderMode": record["values"]["m_RenderMode"],
            "slots": slots,
        }

    result = {
        "schema": "zzz-corin-renderer-mesh-map/v2",
        "rendererData": str(args.renderer_data.resolve()),
        "archiveMap": str(args.archive_map.resolve()),
        "rendererCount": len(entries),
        "slotStatusCounts": {
            f"{field}:{status}": count
            for (field, status), count in sorted(status_counts.items())
        },
        "entries": entries,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    print(json.dumps({
        "output": str(args.output),
        "rendererCount": result["rendererCount"],
        "slotStatusCounts": result["slotStatusCounts"],
    }, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
