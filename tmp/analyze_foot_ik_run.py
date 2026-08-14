import csv
import gzip
import json
import math
import os
import statistics
import sys


def number(row, key):
    try:
        value = float(row.get(key, ""))
        return value if math.isfinite(value) else math.nan
    except (TypeError, ValueError):
        return math.nan


def boolean(row, key):
    return row.get(key, "").lower() == "true"


def percentile(values, fraction):
    values = sorted(value for value in values if math.isfinite(value))
    if not values:
        return math.nan
    index = fraction * (len(values) - 1)
    lower = math.floor(index)
    upper = math.ceil(index)
    if lower == upper:
        return values[lower]
    return values[lower] + (values[upper] - values[lower]) * (index - lower)


run_path = os.path.abspath(sys.argv[1])
with open(os.path.join(run_path, "manifest.json"), encoding="utf-8") as stream:
    manifest = json.load(stream)

rows = []
bad_width = []
header = None
for chunk in manifest["chunks"]:
    path = os.path.join(run_path, chunk["file"])
    with gzip.open(path, "rt", encoding="utf-8", newline="") as stream:
        reader = csv.reader(stream)
        current_header = next(reader)
        if header is None:
            header = current_header
        elif current_header != header:
            raise RuntimeError(f"header mismatch: {chunk['file']}")
        for raw in reader:
            if len(raw) != len(header):
                bad_width.append((chunk["file"], len(raw)))
                continue
            row = dict(zip(header, raw))
            row["__direction"] = chunk["direction"]
            rows.append(row)

result = {
    "run": os.path.basename(run_path),
    "manifest_status": manifest["status"],
    "schema": manifest["schema"],
    "manifest_columns": manifest["columnCount"],
    "header_columns": len(header),
    "manifest_rows": manifest["totalRows"],
    "read_rows": len(rows),
    "bad_width_rows": len(bad_width),
    "directions": {},
    "lift_offs": [],
}

for direction in sorted(set(row["__direction"] for row in rows)):
    selected = [row for row in rows if row["__direction"] == direction]
    metrics = {}
    for side in ("left", "right"):
        goal_jumps = []
        baseline_jumps = []
        path_jumps = []
        pre_lift_blends = []
        previous = None
        for row in selected:
            if previous is not None:
                frame_delta = number(row, "frame_sequence") - number(previous, "frame_sequence")
                same_plan = (
                    row.get(f"{side}_predictive_plan_generated_frame") ==
                    previous.get(f"{side}_predictive_plan_generated_frame") and
                    row.get(f"{side}_plan_landing_event_identity") ==
                    previous.get(f"{side}_plan_landing_event_identity")
                )
                if frame_delta > 0 and same_plan:
                    goal_jumps.append(abs(number(row, f"{side}_final_goal_world_y") - number(previous, f"{side}_final_goal_world_y")))
                    baseline_jumps.append(abs(number(row, f"{side}_baseline_goal_world_y") - number(previous, f"{side}_baseline_goal_world_y")))
                    path_jumps.append(abs(number(row, f"{side}_current_path_world_y") - number(previous, f"{side}_current_path_world_y")))
                    phase = number(row, f"{side}_landing_event_phase")
                    previous_phase = number(previous, f"{side}_landing_event_phase")
                    lift_off = number(row, f"{side}_landing_lift_off_phase")
                    if previous_phase < lift_off <= phase:
                        result["lift_offs"].append({
                            "direction": direction,
                            "side": side,
                            "frame": int(number(row, "frame_sequence")),
                            "generated_frame": int(number(row, f"{side}_predictive_plan_generated_frame")),
                            "phase": phase,
                            "lift_off_phase": lift_off,
                            "execution_progress": number(row, f"{side}_predictive_execution_progress"),
                            "goal_jump_cm": goal_jumps[-1] * 100,
                            "baseline_jump_cm": baseline_jumps[-1] * 100,
                            "path_jump_cm": path_jumps[-1] * 100,
                            "required_lift_cm": number(row, f"{side}_required_lift") * 100,
                            "applied_lift_cm": number(row, f"{side}_applied_lift") * 100,
                            "plan_blend": number(row, f"{side}_plan_prediction_blend"),
                            "pose_blend": number(row, f"{side}_pose_synchronized_prediction_blend"),
                        })
            phase = number(row, f"{side}_landing_event_phase")
            lift_off = number(row, f"{side}_landing_lift_off_phase")
            blend = number(row, f"{side}_pose_synchronized_prediction_blend")
            if phase + 0.000001 < lift_off and math.isfinite(blend):
                pre_lift_blends.append(blend)
            previous = row
        metrics[side] = {
            "same_plan_goal_jump_p95_cm": percentile(goal_jumps, 0.95) * 100,
            "same_plan_goal_jump_max_cm": max(goal_jumps, default=math.nan) * 100,
            "same_plan_baseline_jump_max_cm": max(baseline_jumps, default=math.nan) * 100,
            "same_plan_path_jump_max_cm": max(path_jumps, default=math.nan) * 100,
            "pre_lift_blend_max": max(pre_lift_blends, default=math.nan),
        }
    result["directions"][direction] = metrics

result["lift_offs"] = sorted(result["lift_offs"], key=lambda item: item["goal_jump_cm"], reverse=True)
print(json.dumps(result, ensure_ascii=False, indent=2))
