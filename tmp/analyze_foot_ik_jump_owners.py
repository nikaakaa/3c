import csv
import gzip
import json
import math
import pathlib
import sys


def number(row, key):
    try:
        value = float(row.get(key, ""))
        return value if math.isfinite(value) else None
    except (TypeError, ValueError):
        return None


def delta(current, previous, key):
    a = number(current, key)
    b = number(previous, key)
    return None if a is None or b is None else a - b


run_dir = pathlib.Path(sys.argv[1])
rows = []
header = None
for path in sorted(run_dir.glob("chunk-*.csv.gz")):
    if ".partial." in path.name or path.stat().st_size == 0:
        continue
    parts = path.name.split("-")
    with gzip.open(path, "rt", encoding="utf-8", newline="") as stream:
        reader = csv.reader(stream)
        current_header = next(reader)
        if header is None:
            header = current_header
        elif header != current_header:
            raise RuntimeError(path.name)
        for values in reader:
            if len(values) != len(header):
                continue
            row = dict(zip(header, values))
            row["__chunk"] = path.name
            rows.append(row)

rows.sort(key=lambda row: int(row["frame_sequence"]))
by_frame = {int(row["frame_sequence"]): row for row in rows}


def snapshot(row, side, previous=None):
    keys = [
        "frame_sequence",
        "pose_root_world_y",
        "pelvis_lyra_target",
        "pelvis_resolved_target",
        "pelvis_current",
        "pelvis_support_side",
        "pelvis_support_plan_sequence",
        f"{side}_plan_invariants_route_direction",
        f"{side}_plan_invariants_route_lap",
        f"{side}_plan_invariants_simulation_tick",
        f"{side}_landing_event_identity",
        f"{side}_landing_event_phase",
        f"{side}_landing_lift_off_phase",
        f"{side}_predictive_plan_sequence",
        f"{side}_predictive_plan_state",
        f"{side}_predictive_plan_transition",
        f"{side}_predictive_plan_end_reason",
        f"{side}_landing_action_progress",
        f"{side}_landing_ground_path_progress",
        f"{side}_plan_prediction_blend",
        f"{side}_committed_prediction_blend",
        f"{side}_final_source",
        f"{side}_contact",
        f"{side}_transition",
        f"{side}_contact_decision",
        f"{side}_has_anchor",
        f"{side}_anchor_blend",
        f"{side}_surface",
        f"{side}_hit_location_y",
        f"{side}_sole_support_surface",
        f"{side}_current_path_surface",
        f"{side}_current_path_world_y",
        f"{side}_current_path_support_y",
        f"{side}_authored_animation_clearance",
        f"{side}_animation_clearance_continuity_contribution",
        f"{side}_composite_animation_clearance",
        f"{side}_target_offset",
        f"{side}_offset_target",
        f"{side}_sole_constraint_offset",
        f"{side}_current_offset",
        f"{side}_sole_clearance_target",
        f"{side}_required_lift",
        f"{side}_applied_lift",
        f"{side}_baseline_goal_world_y",
        f"{side}_final_goal_world_y",
        f"{side}_final_physical_support_surface",
        f"{side}_position_residual",
    ]
    result = {}
    for key in keys:
        value = row.get(key, "")
        numeric = number(row, key)
        result[key] = round(numeric, 7) if numeric is not None else value
    if previous is not None:
        for key in (
            f"{side}_final_goal_world_y",
            f"{side}_baseline_goal_world_y",
            f"{side}_current_path_world_y",
            f"{side}_current_offset",
            "pelvis_current",
        ):
            value = delta(row, previous, key)
            result[f"delta_{key}_cm"] = None if value is None else round(value * 100, 4)
    return result


result = {"run": run_dir.name, "rows": len(rows), "sides": {}}
for side in ("left", "right"):
    candidates = []
    previous = None
    for row in rows:
        if previous is not None and int(row["frame_sequence"]) == int(previous["frame_sequence"]) + 1:
            goal_delta = delta(row, previous, f"{side}_final_goal_world_y")
            if goal_delta is not None:
                candidates.append({
                    "frame": int(row["frame_sequence"]),
                    "abs_goal_delta": abs(goal_delta),
                    "goal_delta": goal_delta,
                    "direction": row[f"{side}_plan_invariants_route_direction"],
                    "plan": row[f"{side}_predictive_plan_sequence"],
                    "same_plan": row[f"{side}_predictive_plan_sequence"] == previous[f"{side}_predictive_plan_sequence"],
                    "path_delta": delta(row, previous, f"{side}_current_path_world_y"),
                    "baseline_delta": delta(row, previous, f"{side}_baseline_goal_world_y"),
                    "current_offset_delta": delta(row, previous, f"{side}_current_offset"),
                    "surface_changed": row[f"{side}_surface"] != previous[f"{side}_surface"],
                    "owner_changed": row[f"{side}_final_source"] != previous[f"{side}_final_source"],
                })
        previous = row
    top = sorted(candidates, key=lambda item: item["abs_goal_delta"], reverse=True)[:30]
    contexts = []
    for candidate in top[:12]:
        frame = candidate["frame"]
        context = []
        for current_frame in range(frame - 2, frame + 3):
            row = by_frame.get(current_frame)
            if row is None:
                continue
            previous_row = by_frame.get(current_frame - 1)
            context.append(snapshot(row, side, previous_row))
        normalized = dict(candidate)
        for key in ("abs_goal_delta", "goal_delta", "path_delta", "baseline_delta", "current_offset_delta"):
            value = normalized[key]
            normalized[f"{key}_cm"] = None if value is None else round(value * 100, 4)
            del normalized[key]
        normalized["context"] = context
        contexts.append(normalized)
    result["sides"][side] = {
        "top": contexts,
        "uphill_over_10cm": sum(item["direction"] == "start-to-end" and item["abs_goal_delta"] >= 0.1 for item in candidates),
        "uphill_path_jump_over_10cm": sum(item["direction"] == "start-to-end" and item["path_delta"] is not None and abs(item["path_delta"]) >= 0.1 for item in candidates),
        "uphill_baseline_jump_over_10cm": sum(item["direction"] == "start-to-end" and item["baseline_delta"] is not None and abs(item["baseline_delta"]) >= 0.1 for item in candidates),
        "uphill_surface_change_over_10cm": sum(item["direction"] == "start-to-end" and item["abs_goal_delta"] >= 0.1 and item["surface_changed"] for item in candidates),
        "uphill_owner_change_over_10cm": sum(item["direction"] == "start-to-end" and item["abs_goal_delta"] >= 0.1 and item["owner_changed"] for item in candidates),
    }

print(json.dumps(result, ensure_ascii=False, indent=2))
