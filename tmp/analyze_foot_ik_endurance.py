import csv
import gzip
import json
import math
import os
import sys
from collections import Counter


def as_float(row, key):
    try:
        return float(row.get(key, ""))
    except (TypeError, ValueError):
        return math.nan


def as_int(row, key):
    try:
        return int(row.get(key, ""))
    except (TypeError, ValueError):
        return 0


def as_bool(row, key):
    return row.get(key, "").lower() == "true"


def finite(value):
    return isinstance(value, (int, float)) and math.isfinite(value)


def delta3(current, previous, prefix):
    values = []
    for axis in "xyz":
        a = as_float(current, f"{prefix}_{axis}")
        b = as_float(previous, f"{prefix}_{axis}")
        if not finite(a) or not finite(b):
            return math.nan
        values.append(a - b)
    return math.sqrt(sum(value * value for value in values))


def delta_axis(current, previous, prefix, axis):
    a = as_float(current, f"{prefix}_{axis}")
    b = as_float(previous, f"{prefix}_{axis}")
    return a - b if finite(a) and finite(b) else math.nan


def load_run(path):
    rows = []
    widths = Counter()
    header = None
    files = sorted(
        os.path.join(path, name)
        for name in os.listdir(path)
        if name.endswith(".csv.gz")
    )
    for file_path in files:
        with gzip.open(file_path, "rt", encoding="utf-8", newline="") as stream:
            reader = csv.reader(stream)
            file_header = next(reader)
            if header is None:
                header = file_header
            elif file_header != header:
                raise RuntimeError(f"header mismatch: {file_path}")
            for values in reader:
                widths[len(values)] += 1
                if len(values) != len(header):
                    continue
                rows.append(dict(zip(header, values)))
    rows.sort(key=lambda row: as_int(row, "frame_sequence"))
    return header, rows, widths, files


def top_deltas(rows, prefix, limit=12):
    values = []
    for index in range(1, len(rows)):
        current = rows[index]
        previous = rows[index - 1]
        magnitude = delta3(current, previous, prefix)
        if not finite(magnitude):
            continue
        values.append({
            "frame": as_int(current, "frame_sequence"),
            "previous_frame": as_int(previous, "frame_sequence"),
            "magnitude": magnitude,
            "y_delta": delta_axis(current, previous, prefix, "y"),
            "route_phase": current.get("left_plan_invariants_route_phase", ""),
        })
    return sorted(values, key=lambda value: value["magnitude"], reverse=True)[:limit]


def top_scalar_deltas(rows, key, limit=12):
    values = []
    for index in range(1, len(rows)):
        current = as_float(rows[index], key)
        previous = as_float(rows[index - 1], key)
        if not finite(current) or not finite(previous):
            continue
        values.append({
            "frame": as_int(rows[index], "frame_sequence"),
            "delta": current - previous,
            "value": current,
            "previous": previous,
            "route_phase": rows[index].get("left_plan_invariants_route_phase", ""),
        })
    return sorted(values, key=lambda value: abs(value["delta"]), reverse=True)[:limit]


def aba_switches(rows, key):
    result = []
    for index in range(2, len(rows)):
        a = rows[index - 2].get(key, "")
        b = rows[index - 1].get(key, "")
        c = rows[index].get(key, "")
        if a and a != "0" and a == c and b and b != "0" and b != a:
            result.append({
                "frame": as_int(rows[index], "frame_sequence"),
                "a": a,
                "b": b,
            })
    return result


def leg_summary(rows, leg):
    state_key = f"{leg}_predictive_plan_state"
    transition_key = f"{leg}_predictive_plan_transition"
    end_key = f"{leg}_predictive_plan_end_reason"
    reject_key = f"{leg}_prediction_reject"
    surface_key = f"{leg}_current_path_surface"
    swing_key = f"{leg}_current_event_is_swing"
    executable_key = f"{leg}_plan_has_executable_path"
    geometry_key = f"{leg}_plan_has_path_geometry"
    sequence_key = f"{leg}_predictive_plan_sequence"
    revision_key = f"{leg}_has_plan_revision"
    rewritten_key = f"{leg}_rewritten"
    traversal = [row for row in rows if row.get(f"{leg}_plan_invariants_route_phase") == "StartToEnd"]
    swing = [row for row in traversal if as_bool(row, swing_key)]
    executing = [row for row in traversal if row.get(state_key) == "Executing"]
    missing = [row for row in swing if not as_bool(row, executable_key)]
    missing_groups = []
    active_group = None
    for row in missing:
        frame = as_int(row, "frame_sequence")
        if active_group is None or frame != active_group[-1] + 1:
            active_group = [frame]
            missing_groups.append(active_group)
        else:
            active_group.append(frame)
    plan_changes = []
    for index in range(1, len(traversal)):
        current = as_int(traversal[index], sequence_key)
        previous = as_int(traversal[index - 1], sequence_key)
        if current != previous:
            plan_changes.append({
                "frame": as_int(traversal[index], "frame_sequence"),
                "from": previous,
                "to": current,
                "state": traversal[index].get(state_key, ""),
                "transition": traversal[index].get(transition_key, ""),
                "swing": as_bool(traversal[index], swing_key),
            })
    min_heel = min((as_float(row, f"{leg}_final_physical_heel_plane_distance") for row in traversal), default=math.nan)
    min_toe = min((as_float(row, f"{leg}_final_physical_toe_plane_distance") for row in traversal), default=math.nan)
    max_penetration = max((as_float(row, f"{leg}_final_physical_residual_penetration") for row in traversal), default=math.nan)
    max_predictive_penetration = max((as_float(row, f"{leg}_predictive_residual_penetration") for row in traversal), default=math.nan)
    max_solver_residual = max((as_float(row, f"{leg}_position_residual") for row in traversal), default=math.nan)
    return {
        "states": Counter(row.get(state_key, "") for row in traversal),
        "transitions": Counter(row.get(transition_key, "") for row in traversal if row.get(transition_key, "") != "None"),
        "end_reasons": Counter(row.get(end_key, "") for row in traversal if row.get(end_key, "") != "None"),
        "rejects": Counter(row.get(reject_key, "") for row in traversal if row.get(reject_key, "") != "None"),
        "traversal_rows": len(traversal),
        "swing_rows": len(swing),
        "executing_rows": len(executing),
        "executable_rows": sum(as_bool(row, executable_key) for row in traversal),
        "geometry_rows": sum(as_bool(row, geometry_key) for row in traversal),
        "rewritten_rows": sum(as_bool(row, rewritten_key) for row in traversal),
        "revision_rows": sum(as_bool(row, revision_key) for row in traversal),
        "swing_without_executable_rows": len(missing),
        "swing_without_executable_groups": [{"first": group[0], "last": group[-1], "count": len(group)} for group in missing_groups],
        "unique_plan_sequences": len({as_int(row, sequence_key) for row in traversal if as_int(row, sequence_key) > 0}),
        "plan_changes": plan_changes,
        "surface_aba": aba_switches(traversal, surface_key),
        "minimum_final_heel_plane_distance": min_heel,
        "minimum_final_toe_plane_distance": min_toe,
        "maximum_final_physical_penetration": max_penetration,
        "maximum_predictive_penetration": max_predictive_penetration,
        "maximum_solver_position_residual": max_solver_residual,
        "top_final_goal_deltas": top_deltas(traversal, f"{leg}_final_goal_world"),
        "top_current_path_deltas": top_deltas(traversal, f"{leg}_current_path_world"),
        "top_fixed_landing_deltas": top_deltas(traversal, f"{leg}_fixed_landing_world"),
        "top_intent_errors": sorted(
            ({
                "frame": as_int(row, "frame_sequence"),
                "error": as_float(row, f"{leg}_intent_landing_displacement_error"),
                "threshold": as_float(row, f"{leg}_intent_landing_displacement_threshold"),
                "state": row.get(state_key, ""),
                "sequence": as_int(row, sequence_key),
            } for row in traversal),
            key=lambda value: value["error"] if finite(value["error"]) else -1,
            reverse=True,
        )[:12],
    }


def summarize(path):
    header, rows, widths, files = load_run(path)
    manifest_path = os.path.join(path, "manifest.json")
    with open(manifest_path, "r", encoding="utf-8") as stream:
        manifest = json.load(stream)
    traversal = [row for row in rows if row.get("left_plan_invariants_route_phase") == "StartToEnd"]
    return {
        "run_id": manifest.get("runId"),
        "schema": manifest.get("schema"),
        "manifest_status": manifest.get("status"),
        "manifest_rows": manifest.get("totalRows"),
        "parsed_rows": len(rows),
        "header_columns": len(header),
        "row_widths": widths,
        "files": [os.path.basename(file_path) for file_path in files],
        "route_phases": Counter(row.get("left_plan_invariants_route_phase", "") for row in rows),
        "top_pelvis_translation_y_deltas": top_scalar_deltas(traversal, "pelvis_translation_y"),
        "top_pose_root_y_deltas": top_scalar_deltas(traversal, "pose_root_world_y"),
        "left": leg_summary(rows, "left"),
        "right": leg_summary(rows, "right"),
    }


def json_default(value):
    if isinstance(value, Counter):
        return dict(value)
    raise TypeError(type(value).__name__)


if __name__ == "__main__":
    for run_path in sys.argv[1:]:
        print(json.dumps(summarize(run_path), ensure_ascii=False, indent=2, default=json_default))
