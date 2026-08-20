import csv
import gzip
import json
import math
import os
import sys
from collections import Counter


def number(row, key):
    try:
        return float(row.get(key, ""))
    except (TypeError, ValueError):
        return math.nan


def integer(row, key):
    try:
        return int(row.get(key, ""))
    except (TypeError, ValueError):
        return 0


def boolean(row, key):
    return row.get(key, "").lower() == "true"


def load(path):
    header = None
    rows = []
    widths = Counter()
    for name in sorted(os.listdir(path)):
        if not name.endswith(".csv.gz"):
            continue
        with gzip.open(os.path.join(path, name), "rt", encoding="utf-8", newline="") as stream:
            reader = csv.reader(stream)
            current_header = next(reader)
            if header is None:
                header = current_header
            elif current_header != header:
                raise RuntimeError(f"header mismatch: {name}")
            for values in reader:
                widths[len(values)] += 1
                if len(values) == len(header):
                    rows.append(dict(zip(header, values)))
    rows.sort(key=lambda row: integer(row, "frame_sequence"))
    return header, rows, widths


def groups(rows):
    values = []
    current = None
    for row in rows:
        frame = integer(row, "frame_sequence")
        if current is None or frame != current[-1] + 1:
            current = [frame]
            values.append(current)
        else:
            current.append(frame)
    return [{"first": value[0], "last": value[-1], "count": len(value)} for value in values]


def context(rows, index, leg):
    result = []
    for item in rows[max(0, index - 1):min(len(rows), index + 2)]:
        result.append({
            "frame": integer(item, "frame_sequence"),
            "event": item.get(f"{leg}_landing_event_identity"),
            "plan": integer(item, f"{leg}_predictive_plan_sequence"),
            "state": item.get(f"{leg}_predictive_plan_state"),
            "transition": item.get(f"{leg}_predictive_plan_transition"),
            "rewritten": boolean(item, f"{leg}_rewritten"),
            "reject": item.get(f"{leg}_prediction_reject"),
            "path_y": number(item, f"{leg}_current_path_world_y"),
            "baseline_y": number(item, f"{leg}_baseline_goal_world_y"),
            "goal_y": number(item, f"{leg}_final_goal_world_y"),
            "reach_clearance": number(item, f"{leg}_reach_clearance"),
            "penetration": number(item, f"{leg}_final_physical_residual_penetration"),
            "anchor": boolean(item, f"{leg}_has_anchor"),
            "anchor_blend": number(item, f"{leg}_anchor_blend"),
        })
    return result


def top_delta(rows, key, count=10):
    values = []
    for index in range(1, len(rows)):
        current = number(rows[index], key)
        previous = number(rows[index - 1], key)
        if math.isfinite(current) and math.isfinite(previous):
            values.append({
                "frame": integer(rows[index], "frame_sequence"),
                "delta": current - previous,
                "value": current,
                "previous": previous,
            })
    return sorted(values, key=lambda value: abs(value["delta"]), reverse=True)[:count]


def leg_summary(rows, leg):
    swing = [row for row in rows if boolean(row, f"{leg}_current_event_is_swing")]
    missing = [row for row in swing if not boolean(row, f"{leg}_plan_has_executable_path")]
    executing = [row for row in rows if row.get(f"{leg}_predictive_plan_state") == "Executing"]
    dropped = [row for row in executing if not boolean(row, f"{leg}_rewritten")]
    dropped_contexts = []
    for index in range(1, len(rows) - 1):
        current = rows[index]
        if current.get(f"{leg}_predictive_plan_state") != "Executing" or boolean(current, f"{leg}_rewritten"):
            continue
        sequence = integer(current, f"{leg}_predictive_plan_sequence")
        if sequence and integer(rows[index - 1], f"{leg}_predictive_plan_sequence") == sequence and integer(rows[index + 1], f"{leg}_predictive_plan_sequence") == sequence:
            dropped_contexts.append(context(rows, index, leg))
    reach_rows = [row for row in rows if math.isfinite(number(row, f"{leg}_reach_clearance"))]
    reach_rows.sort(key=lambda row: number(row, f"{leg}_reach_clearance"), reverse=True)
    penetration_rows = [row for row in rows if math.isfinite(number(row, f"{leg}_final_physical_residual_penetration"))]
    penetration_rows.sort(key=lambda row: number(row, f"{leg}_final_physical_residual_penetration"), reverse=True)
    return {
        "states": Counter(row.get(f"{leg}_predictive_plan_state", "") for row in rows),
        "rejects": Counter(row.get(f"{leg}_prediction_reject", "") for row in rows),
        "end_reasons": Counter(row.get(f"{leg}_predictive_plan_end_reason", "") for row in rows),
        "swing_rows": len(swing),
        "swing_without_executable_rows": len(missing),
        "swing_without_executable_by_reject": Counter(row.get(f"{leg}_prediction_reject", "") for row in missing),
        "swing_without_executable_groups": groups(missing)[:20],
        "executing_rows": len(executing),
        "executing_not_rewritten_rows": len(dropped),
        "same_plan_drop_contexts": dropped_contexts[:20],
        "maximum_reach_clearance": number(reach_rows[0], f"{leg}_reach_clearance") if reach_rows else math.nan,
        "maximum_reach_context": context(rows, rows.index(reach_rows[0]), leg) if reach_rows else [],
        "maximum_penetration": number(penetration_rows[0], f"{leg}_final_physical_residual_penetration") if penetration_rows else math.nan,
        "maximum_penetration_context": context(rows, rows.index(penetration_rows[0]), leg) if penetration_rows else [],
        "top_goal_y_deltas": top_delta(rows, f"{leg}_final_goal_world_y"),
        "top_path_y_deltas": top_delta(rows, f"{leg}_current_path_world_y"),
        "top_baseline_y_deltas": top_delta(rows, f"{leg}_baseline_goal_world_y"),
    }


def summarize(path):
    header, rows, widths = load(path)
    return {
        "run": os.path.basename(path),
        "rows": len(rows),
        "columns": len(header),
        "unique_columns": len(set(header)),
        "widths": widths,
        "route_phases": Counter(row.get("left_plan_invariants_route_phase", "") for row in rows),
        "top_pelvis_y_deltas": top_delta(rows, "pelvis_translation_y"),
        "left": leg_summary(rows, "left"),
        "right": leg_summary(rows, "right"),
    }


def json_default(value):
    if isinstance(value, Counter):
        return dict(value)
    raise TypeError(type(value).__name__)


def compact(summary):
    result = {
        "run": summary["run"],
        "rows": summary["rows"],
        "columns": summary["columns"],
        "widths": summary["widths"],
        "pelvis": summary["top_pelvis_y_deltas"][:5],
    }
    for leg in ("left", "right"):
        source = summary[leg]
        result[leg] = {
            "states": source["states"],
            "rejects": source["rejects"],
            "end_reasons": source["end_reasons"],
            "swing_rows": source["swing_rows"],
            "swing_without_executable_rows": source["swing_without_executable_rows"],
            "swing_without_executable_groups": source["swing_without_executable_groups"],
            "executing_not_rewritten_rows": source["executing_not_rewritten_rows"],
            "maximum_reach_clearance": source["maximum_reach_clearance"],
            "maximum_reach_context": source["maximum_reach_context"],
            "maximum_penetration": source["maximum_penetration"],
            "maximum_penetration_context": source["maximum_penetration_context"],
            "top_goal_y_deltas": source["top_goal_y_deltas"][:5],
            "top_path_y_deltas": source["top_path_y_deltas"][:5],
            "top_baseline_y_deltas": source["top_baseline_y_deltas"][:5],
        }
    return result


def inspect(path, frames):
    _, rows, _ = load(path)
    wanted = set()
    for frame in frames:
        wanted.update(range(frame - 2, frame + 3))
    for row in rows:
        frame = integer(row, "frame_sequence")
        if frame not in wanted:
            continue
        item = {
            "frame": frame,
            "phase": row.get("left_plan_invariants_route_phase"),
            "actor_y": number(row, "left_plan_invariants_route_actor_y"),
            "pelvis_y": number(row, "pelvis_translation_y"),
            "pelvis_lyra_target": row.get("pelvis_lyra_target"),
            "pelvis_resolved_target": row.get("pelvis_resolved_target"),
            "pelvis_current": row.get("pelvis_current"),
            "pelvis_support_side": row.get("pelvis_support_side"),
            "pelvis_support_switched": boolean(row, "pelvis_support_switched"),
            "pelvis_support_plan": integer(row, "pelvis_support_plan_sequence"),
            "pelvis_current_support_target": row.get("pelvis_current_support_target"),
            "pelvis_selected_support_target": row.get("pelvis_selected_support_target"),
        }
        for leg in ("left", "right"):
            item[leg] = {
                "event": row.get(f"{leg}_landing_event_identity"),
                "pre": boolean(row, f"{leg}_current_event_is_pre_swing"),
                "swing": boolean(row, f"{leg}_current_event_is_swing"),
                "event_weight": number(row, f"{leg}_current_event_foot_pose_weight"),
                "event_phase": number(row, f"{leg}_landing_event_phase"),
                "lift_off_phase": number(row, f"{leg}_landing_lift_off_phase"),
                "support_phase": row.get(f"{leg}_pelvis_support_phase"),
                "plan": integer(row, f"{leg}_predictive_plan_sequence"),
                "generated": integer(row, f"{leg}_predictive_plan_generated_frame"),
                "state": row.get(f"{leg}_predictive_plan_state"),
                "transition": row.get(f"{leg}_predictive_plan_transition"),
                "end": row.get(f"{leg}_predictive_plan_end_reason"),
                "executable": boolean(row, f"{leg}_plan_has_executable_path"),
                "rewritten": boolean(row, f"{leg}_rewritten"),
                "reject": row.get(f"{leg}_prediction_reject"),
                "fading": boolean(row, f"{leg}_plan_fading_out"),
                "retention": number(row, f"{leg}_plan_retention_weight"),
                "prediction_blend": number(row, f"{leg}_plan_prediction_blend"),
                "authoritative_blend": number(row, f"{leg}_authoritative_prediction_blend"),
                "revision": integer(row, f"{leg}_revision_plan_sequence"),
                "revision_weight": number(row, f"{leg}_plan_revision_blend_weight"),
                "motion_error": number(row, f"{leg}_motion_landing_error"),
                "motion_tolerance": number(row, f"{leg}_motion_landing_tolerance"),
                "intent_error": number(row, f"{leg}_intent_landing_displacement_error"),
                "intent_threshold": number(row, f"{leg}_intent_landing_displacement_threshold"),
                "path_y": number(row, f"{leg}_current_path_world_y"),
                "path_surface": row.get(f"{leg}_current_path_surface"),
                "sole_support_surface": row.get(f"{leg}_sole_support_surface"),
                "sole_support_y": number(row, f"{leg}_sole_support_point_y"),
                "sole_clearance_target": row.get(f"{leg}_sole_clearance_target"),
                "predictive_penetration": number(row, f"{leg}_predictive_residual_penetration"),
                "baseline_y": number(row, f"{leg}_baseline_goal_world_y"),
                "goal_y": number(row, f"{leg}_final_goal_world_y"),
                "current_y": number(row, f"{leg}_current_grounding_y"),
                "animated_ankle_component_y": number(row, f"{leg}_animated_ankle_component_y"),
                "current_sole_y": number(row, f"{leg}_current_sole_world_y"),
                "contact": row.get(f"{leg}_contact"),
                "decision": row.get(f"{leg}_contact_decision"),
                "anchor": boolean(row, f"{leg}_has_anchor"),
                "anchor_blend": number(row, f"{leg}_anchor_blend"),
                "reach": number(row, f"{leg}_reach_clearance"),
                "reach_ratio": number(row, f"{leg}_prediction_reach_ratio"),
                "authored_clearance": number(row, f"{leg}_authored_animation_clearance"),
                "continuity_offset": number(row, f"{leg}_animation_clearance_continuity_offset"),
                "continuity_contribution": number(row, f"{leg}_animation_clearance_continuity_contribution"),
                "composite_clearance": number(row, f"{leg}_composite_animation_clearance"),
                "required_lift": number(row, f"{leg}_required_lift"),
                "applied_lift": number(row, f"{leg}_applied_lift"),
                "path_root_y": number(row, f"{leg}_current_path_root_world_y"),
                "path_hip_y": number(row, f"{leg}_current_path_hip_world_y"),
                "predicted_hip_y": number(row, f"{leg}_predicted_hip_world_y"),
                "penetration": number(row, f"{leg}_final_physical_residual_penetration"),
            }
        print(json.dumps(item, ensure_ascii=False, separators=(",", ":")))


if len(sys.argv) > 2 and sys.argv[1].startswith("--inspect="):
    inspect(sys.argv[2], [int(value) for value in sys.argv[1].split("=", 1)[1].split(",")])
else:
    for path in sys.argv[1:]:
        print(json.dumps(compact(summarize(path)), ensure_ascii=False, separators=(",", ":"), default=json_default))
