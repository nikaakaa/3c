import argparse
import json
from pathlib import Path


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--map", type=Path, action="append", required=True)
    parser.add_argument("--merge", type=Path, action="append", default=[])
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    report = json.loads(args.report.read_text(encoding="utf-8"))
    targets = {}
    for material in report["Materials"]:
        for texture_env in material["TextureEnvs"]:
            if texture_env["TargetState"] != "Missing":
                continue
            targets[texture_env["PathId"]] = texture_env["Key"]

    candidates_by_identity = {}
    for merge_path in args.merge:
        prior = json.loads(merge_path.read_text(encoding="utf-8"))
        for candidate in prior["candidates"]:
            key = (candidate["key"], candidate["source"], candidate["name"])
            candidates_by_identity[key] = candidate
    for map_path in args.map:
        asset_map = json.loads(map_path.read_text(encoding="utf-8"))
        for entry in asset_map["AssetEntries"]:
            path_id = entry["PathID"]
            if entry["Type"] == "Texture2D" and path_id in targets:
                candidates_by_identity[(targets[path_id], entry["Source"], entry["Name"])] = (
                    {
                        "key": targets[path_id],
                        "pathId": path_id,
                        "name": entry["Name"],
                        "container": entry["Container"],
                        "source": entry["Source"],
                        "sha256": entry["SHA256Hash"],
                        "map": str(map_path.resolve()),
                        "evidence": "pathIdAndTexture2DType",
                    }
                )

    candidates = sorted(candidates_by_identity.values(), key=lambda item: (item["key"], item["source"], item["name"]))
    matched_keys = {item["key"] for item in candidates}
    result = {
        "schema": "zzz-effect-texture-global-target-trace/v1",
        "report": str(args.report.resolve()),
        "mapFiles": [str(path.resolve()) for path in args.map],
        "missingTargetCount": len(targets),
        "matchedKeyCount": len(matched_keys),
        "candidateCount": len(candidates),
        "candidates": candidates,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    print(
        json.dumps(
            {
                "output": str(args.output),
                "missingTargets": len(targets),
                "matchedKeys": len(matched_keys),
                "candidates": len(candidates),
            }
        )
    )


if __name__ == "__main__":
    main()
