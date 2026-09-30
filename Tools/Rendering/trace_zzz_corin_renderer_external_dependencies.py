import argparse
import json
from collections import Counter
from pathlib import Path


def load_targets(scan_path):
    scan = json.loads(scan_path.read_text(encoding="utf-8"))
    reference_counts = Counter()
    for record in scan["records"]:
        for dependency in record["dependencies"]:
            if dependency["scope"] != "external":
                continue
            cab = dependency["external"].split("/")[-1]
            reference_counts[(cab, dependency["pathId"])] += 1

    return {
        "schema": scan["schema"],
        "scan": str(scan_path.resolve()),
        "referenceCounts": reference_counts,
    }


def load_map_entries(map_path):
    asset_map = json.loads(map_path.read_text(encoding="utf-8"))
    return asset_map["AssetEntries"]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--scan", type=Path, required=True)
    parser.add_argument("--map", type=Path, action="append", required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    traced = load_targets(args.scan)
    entries_by_key = {}
    map_sources = []
    for map_path in args.map:
        entries = load_map_entries(map_path)
        map_sources.append(str(map_path.resolve()))
        for entry in entries:
            key = (entry["PathID"], entry["Name"], entry["Type"])
            entries_by_key.setdefault(key, []).append(
                {
                    "container": entry["Container"],
                    "source": entry["Source"],
                    "sha256": entry["SHA256Hash"],
                }
            )

    targets = []
    for (cab, path_id), reference_count in sorted(
        traced["referenceCounts"].items(), key=lambda item: (item[0][0], item[0][1])
    ):
        candidates = []
        for (entry_path_id, name, asset_type), locations in entries_by_key.items():
            if entry_path_id == path_id:
                candidates.append(
                    {
                        "name": name,
                        "type": asset_type,
                        "locations": locations,
                    }
                )
        targets.append(
            {
                "cab": cab,
                "pathId": path_id,
                "referenceCount": reference_count,
                "resolved": bool(candidates),
                "candidates": candidates,
            }
        )

    result = {
        "schema": "zzz-corin-renderer-external-dependency-trace/v1",
        "scan": traced["scan"],
        "mapFiles": map_sources,
        "targetCount": len(targets),
        "resolvedTargetCount": sum(target["resolved"] for target in targets),
        "targets": targets,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    print(
        json.dumps(
            {
                "output": str(args.output),
                "targetCount": result["targetCount"],
                "resolvedTargetCount": result["resolvedTargetCount"],
            }
        )
    )


if __name__ == "__main__":
    main()
