import csv
import gzip
import json
import math
import pathlib
import statistics
import sys


def load_rows(run_dir):
    header = None
    rows = []
    width_errors = []
    for path in sorted(run_dir.glob("chunk-*.csv.gz")):
        with gzip.open(path, "rt", encoding="utf-8-sig", newline="") as stream:
            reader = csv.reader(stream)
            current_header = next(reader)
            if header is None:
                header = current_header
            elif current_header != header:
                raise RuntimeError(f"header mismatch: {path.name}")
            for line_number, values in enumerate(reader, 2):
                if len(values) != len(header):
                    width_errors.append((path.name, line_number, len(values)))
                    continue
                rows.append(dict(zip(header, values)))
    rows.sort(key=lambda row: int(row["frame_sequence"]))
    return header, rows, width_errors


def number(row, key):
    try:
        value = float(row.get(key, ""))
        return value if math.isfinite(value) else 0.0
    except (TypeError, ValueError):
        return 0.0


def percentile(values, fraction):
    values = sorted(abs(value) for value in values)
    if not values:
        return 0.0
    position = (len(values) - 1) * fraction
    low = int(math.floor(position))
    high = int(math.ceil(position))
    if low == high:
        return values[low]
    return values[low] + (values[high] - values[low]) * (position - low)


def cm_stats(values):
    return {
        "p50": round(percentile(values, 0.5) * 100, 4),
        "p95": round(percentile(values, 0.95) * 100, 4),
        "max": round(max((abs(value) for value in values), default=0.0) * 100, 4),
    }


def analyze(run_dir):
    header, rows, width_errors = load_rows(run_dir)
    metrics = {
        "pelvis_current": [],
        "pelvis_selected_support_target": [],
        "pelvis_resolved_target": [],
        "left_final_goal_world_y": [],
        "right_final_goal_world_y": [],
    }
    switches = []
    previous = None
    for row in rows:
        if previous is not None and int(row["frame_sequence"]) == int(previous["frame_sequence"]) + 1:
            for key in metrics:
                metrics[key].append(number(row, key) - number(previous, key))
            support_switched = row.get("pelvis_support_switched", "").lower() == "true"
            identity_changed = (
                row.get("pelvis_support_side") != previous.get("pelvis_support_side")
                or row.get("pelvis_support_plan_sequence") != previous.get("pelvis_support_plan_sequence")
            )
            if support_switched or identity_changed:
                side = row.get("pelvis_support_side", "").lower()
                raw_key = f"{side}_pelvis_displacement" if side in ("left", "right") else ""
                switches.append({
                    "frame": int(row["frame_sequence"]),
                    "from": f"{previous.get('pelvis_support_side')}:{previous.get('pelvis_support_plan_sequence')}",
                    "to": f"{row.get('pelvis_support_side')}:{row.get('pelvis_support_plan_sequence')}",
                    "raw_target_cm": round(number(row, raw_key) * 100, 4) if raw_key else 0.0,
                    "selected_target_cm": round(number(row, "pelvis_selected_support_target") * 100, 4),
                    "selected_delta_cm": round((number(row, "pelvis_selected_support_target") - number(previous, "pelvis_selected_support_target")) * 100, 4),
                    "current_delta_cm": round((number(row, "pelvis_current") - number(previous, "pelvis_current")) * 100, 4),
                })
        previous = row
    support_rows = sum(row.get("pelvis_support_available", "").lower() == "true" for row in rows)
    return {
        "run": run_dir.name,
        "rows": len(rows),
        "columns": len(header or []),
        "width_errors": width_errors,
        "support_rows": support_rows,
        "metrics_cm": {key: cm_stats(values) for key, values in metrics.items()},
        "support_switches": len(switches),
        "top_switches": sorted(switches, key=lambda item: abs(item["current_delta_cm"]), reverse=True)[:15],
    }


print(json.dumps([analyze(pathlib.Path(path)) for path in sys.argv[1:]], ensure_ascii=False, indent=2))
