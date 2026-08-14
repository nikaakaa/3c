import csv
import gzip
import json
import math
import pathlib
import statistics
import sys
from collections import Counter, defaultdict


run_dir = pathlib.Path(sys.argv[1])
files = sorted(path for path in run_dir.glob("chunk-*.csv.gz") if ".partial." not in path.name)
header = None
rows = []
width_errors = []
for path in files:
    with gzip.open(path, "rt", encoding="utf-8", newline="") as stream:
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


def number(row, name):
    try:
        return float(row[name])
    except (KeyError, TypeError, ValueError):
        return math.nan


def integer(row, name):
    try:
        return int(row[name])
    except (KeyError, TypeError, ValueError):
        return 0


def boolean(row, name):
    return row.get(name, "").lower() == "true"


def quantile(values, fraction):
    values = sorted(value for value in values if math.isfinite(value))
    if not values:
        return None
    position = (len(values) - 1) * fraction
    lower = int(math.floor(position))
    upper = int(math.ceil(position))
    if lower == upper:
        return values[lower]
    return values[lower] + (values[upper] - values[lower]) * (position - lower)


def stats_cm(values):
    values = [abs(value) * 100 for value in values if math.isfinite(value)]
    return {
        "count": len(values),
        "p50": quantile(values, 0.5),
        "p95": quantile(values, 0.95),
        "max": max(values, default=None),
    }


summary = {
    "files": len(files),
    "rows": len(rows),
    "columns": len(header or []),
    "width_errors": width_errors[:20],
    "directions": dict(Counter(row["left_plan_invariants_route_direction"] for row in rows)),
    "sides": {},
}

for side in ("left", "right"):
    plan_rows = defaultdict(list)
    plan_first = {}
    states = Counter()
    rejects = Counter()
    goal_jumps = []
    path_jumps = []
    rebounds = 0
    previous = None
    previous_goal_delta = None
    final_penetrations = []
    solver_residuals = []
    required_by_direction = defaultdict(list)
    plan_root_errors_by_direction = defaultdict(list)
    actual_root_speeds = []
    planned_root_speeds = []
    root_motion_direction_dots = []
    root_error_by_progress = defaultdict(list)
    static_goal_jumps = []
    top = []
    for row in rows:
        state = row[f"{side}_predictive_plan_state"]
        states[state] += 1
        reject = row[f"{side}_prediction_reject"]
        if reject and reject != "None":
            rejects[reject] += 1
        plan = integer(row, f"{side}_predictive_plan_sequence")
        if plan and state in ("Planned", "Executing"):
            plan_rows[plan].append(row)
            plan_first.setdefault(plan, row)
        direction = row[f"{side}_plan_invariants_route_direction"]
        if state == "Executing":
            required_by_direction[direction].append(number(row, f"{side}_required_lift"))
            plan_root_errors_by_direction[direction].append(math.hypot(
                number(row, "pose_root_world_x") - number(row, f"{side}_current_path_root_world_x"),
                number(row, "pose_root_world_z") - number(row, f"{side}_current_path_root_world_z")))
        if boolean(row, f"{side}_final_physical_evaluated"):
            final_penetrations.append(number(row, f"{side}_final_physical_residual_penetration"))
        solver_residuals.append(number(row, f"{side}_position_residual"))
        if previous is not None and integer(row, "frame_sequence") == integer(previous, "frame_sequence") + 1:
            same_plan = plan != 0 and plan == integer(previous, f"{side}_predictive_plan_sequence")
            goal_delta = number(row, f"{side}_final_goal_world_y") - number(previous, f"{side}_final_goal_world_y")
            path_delta = number(row, f"{side}_current_path_world_y") - number(previous, f"{side}_current_path_world_y")
            if same_plan:
                tick_delta = number(row, f"{side}_plan_invariants_simulation_tick") - number(previous, f"{side}_plan_invariants_simulation_tick")
                tick_rate = number(row, f"{side}_plan_invariants_tick_rate")
                delta_seconds = tick_delta / tick_rate if tick_delta > 0 and tick_rate > 0 else number(row, "presentation_delta_seconds")
                actual_delta_x = number(row, "pose_root_world_x") - number(previous, "pose_root_world_x")
                actual_delta_z = number(row, "pose_root_world_z") - number(previous, "pose_root_world_z")
                planned_delta_x = number(row, f"{side}_current_path_root_world_x") - number(previous, f"{side}_current_path_root_world_x")
                planned_delta_z = number(row, f"{side}_current_path_root_world_z") - number(previous, f"{side}_current_path_root_world_z")
                actual_length = math.hypot(actual_delta_x, actual_delta_z)
                planned_length = math.hypot(planned_delta_x, planned_delta_z)
                if delta_seconds > 1e-6:
                    actual_root_speeds.append(actual_length / delta_seconds)
                    planned_root_speeds.append(planned_length / delta_seconds)
                if actual_length > 1e-6 and planned_length > 1e-6:
                    root_motion_direction_dots.append(
                        (actual_delta_x * planned_delta_x + actual_delta_z * planned_delta_z) /
                        (actual_length * planned_length))
                progress_bucket = min(3, int(number(row, f"{side}_landing_action_progress") * 4))
                root_error_by_progress[progress_bucket].append(math.hypot(
                    number(row, "pose_root_world_x") - number(row, f"{side}_current_path_root_world_x"),
                    number(row, "pose_root_world_z") - number(row, f"{side}_current_path_root_world_z")))
                goal_jumps.append(goal_delta)
                path_jumps.append(path_delta)
                if previous_goal_delta is not None and goal_delta * previous_goal_delta < 0 and max(abs(goal_delta), abs(previous_goal_delta)) >= 0.03:
                    rebounds += 1
                top.append({
                    "frame": integer(row, "frame_sequence"),
                    "plan": plan,
                    "direction": direction,
                    "action_progress": number(row, f"{side}_landing_action_progress"),
                    "goal_delta_cm": goal_delta * 100,
                    "path_delta_cm": path_delta * 100,
                    "required_lift_cm": number(row, f"{side}_required_lift") * 100,
                    "applied_lift_cm": number(row, f"{side}_applied_lift") * 100,
                    "solver_residual_cm": number(row, f"{side}_position_residual") * 100,
                })
                previous_goal_delta = goal_delta
            else:
                previous_goal_delta = None
            if number(row, f"{side}_plan_invariants_actual_planar_speed") <= 0.01:
                static_goal_jumps.append(goal_delta)
        previous = row

    timeline = []
    landing_errors = []
    for plan, values in plan_rows.items():
        first = min(values, key=lambda row: number(row, f"{side}_landing_action_progress"))
        last = max(values, key=lambda row: number(row, f"{side}_landing_action_progress"))
        current_velocity = math.hypot(
            number(first, f"{side}_plan_invariants_current_planar_velocity_x"),
            number(first, f"{side}_plan_invariants_current_planar_velocity_z"))
        continuation_velocity = math.hypot(
            number(first, f"{side}_plan_invariants_continuation_planar_velocity_x"),
            number(first, f"{side}_plan_invariants_continuation_planar_velocity_z"))
        switch_delay = number(first, f"{side}_plan_invariants_current_segment_switch_delay_seconds")
        has_continuation = boolean(first, f"{side}_plan_invariants_has_continuation")
        timeline.append((round(current_velocity, 4), round(continuation_velocity, 4), round(switch_delay, 4), has_continuation))
        if number(last, f"{side}_landing_action_progress") >= 0.9:
            landing_errors.append(math.hypot(
                number(last, "pose_root_world_x") - number(last, f"{side}_frozen_root_landing_world_x"),
                number(last, "pose_root_world_z") - number(last, f"{side}_frozen_root_landing_world_z")))

    summary["sides"][side] = {
        "unique_plans": len(plan_rows),
        "states": dict(states),
        "rejects": dict(rejects),
        "timeline_combinations": {str(key): count for key, count in Counter(timeline).most_common()},
        "plans_with_continuation": sum(item[3] for item in timeline),
        "completed_root_landing_error_cm": stats_cm(landing_errors),
        "plan_root_error_cm_by_direction": {key: stats_cm(value) for key, value in plan_root_errors_by_direction.items()},
        "root_kinematics": {
            "actual_speed": {
                "p50": quantile(actual_root_speeds, 0.5),
                "p95": quantile(actual_root_speeds, 0.95),
            },
            "planned_speed": {
                "p50": quantile(planned_root_speeds, 0.5),
                "p95": quantile(planned_root_speeds, 0.95),
            },
            "direction_dot": {
                "p05": quantile(root_motion_direction_dots, 0.05),
                "p50": quantile(root_motion_direction_dots, 0.5),
            },
            "error_cm_by_progress_quarter": {
                str(key): stats_cm(value) for key, value in root_error_by_progress.items()
            },
        },
        "same_plan_goal_jump_cm": stats_cm(goal_jumps),
        "same_plan_path_jump_cm": stats_cm(path_jumps),
        "goal_rebounds": rebounds,
        "static_goal_jump_cm": stats_cm(static_goal_jumps),
        "required_lift_by_direction": {
            key: {
                "count": len(value),
                "p05_cm": quantile(value, 0.05) * 100 if value else None,
                "p50_cm": quantile(value, 0.5) * 100 if value else None,
                "p95_cm": quantile(value, 0.95) * 100 if value else None,
                "negative_ratio": sum(item < -0.001 for item in value) / len(value) if value else None,
            }
            for key, value in required_by_direction.items()
        },
        "physical_penetration_cm": stats_cm(final_penetrations),
        "physical_penetration_over_1mm": sum(value > 0.001 for value in final_penetrations),
        "solver_residual_cm": stats_cm(solver_residuals),
        "top_goal_jumps": sorted(top, key=lambda item: abs(item["goal_delta_cm"]), reverse=True)[:20],
    }

print(json.dumps(summary, ensure_ascii=False, indent=2, allow_nan=False))
