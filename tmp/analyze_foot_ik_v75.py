import csv
import gzip
import json
import math
import pathlib
import statistics
import sys
from collections import Counter, defaultdict


run_dir = pathlib.Path(sys.argv[1])
files = sorted(run_dir.glob("chunk-*.csv.gz"))
rows = []
header = None
width_errors = []
for path in files:
    if ".partial." in path.name:
        continue
    with gzip.open(path, "rt", encoding="utf-8", newline="") as stream:
        reader = csv.reader(stream)
        current_header = next(reader)
        if header is None:
            header = current_header
        elif current_header != header:
            raise RuntimeError(f"header mismatch: {path.name}")
        for line_number, values in enumerate(reader, 2):
            if len(values) != len(header):
                width_errors.append([path.name, line_number, len(values)])
                continue
            rows.append(dict(zip(header, values)))


def find_all(*parts):
    return [name for name in header if all(part.lower() in name.lower() for part in parts)]


interesting = {}
for parts in [
    ("frame",), ("direction",), ("plan", "sequence"), ("plan", "state"),
    ("foot", "rate"), ("ground", "path", "progress"), ("action", "progress"),
    ("lift", "phase"), ("action", "phase"), ("goal", "y"),
    ("path", "y"), ("pelvis", "target"), ("pelvis", "current"),
    ("pelvis", "resolved"), ("residual",), ("physical",),
    ("creation", "reject"), ("transition",), ("surface", "id"),
]:
    interesting["+".join(parts)] = find_all(*parts)

print(json.dumps({
    "files": len(files),
    "rows": len(rows),
    "columns": len(header or []),
    "width_errors": width_errors[:20],
    "interesting": interesting,
}, ensure_ascii=False, indent=2))


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


def finite(value):
    return math.isfinite(value)


def quantile(values, fraction):
    values = sorted(value for value in values if finite(value))
    if not values:
        return None
    index = (len(values) - 1) * fraction
    first = int(math.floor(index))
    second = int(math.ceil(index))
    if first == second:
        return values[first]
    return values[first] + (values[second] - values[first]) * (index - first)


def correlation(left, right):
    pairs = [
        (a, b)
        for a, b in zip(left, right)
        if finite(a) and finite(b)
    ]
    if len(pairs) < 2:
        return None
    left_mean = statistics.fmean(a for a, _ in pairs)
    right_mean = statistics.fmean(b for _, b in pairs)
    covariance = sum((a - left_mean) * (b - right_mean) for a, b in pairs)
    left_variance = sum((a - left_mean) ** 2 for a, _ in pairs)
    right_variance = sum((b - right_mean) ** 2 for _, b in pairs)
    denominator = math.sqrt(left_variance * right_variance)
    return covariance / denominator if denominator > 1e-12 else None


def sequence(value):
    if not value:
        return []
    return [float(item) for item in value.split(";") if item]


def interpolate(xs, ys, value):
    if len(xs) != len(ys) or not xs:
        return math.nan
    if value <= xs[0]:
        return ys[0]
    for index in range(1, len(xs)):
        if value <= xs[index] + 1e-8:
            span = xs[index] - xs[index - 1]
            if span <= 1e-8:
                return ys[index]
            t = (value - xs[index - 1]) / span
            return ys[index - 1] + (ys[index] - ys[index - 1]) * t
    return ys[-1]


summary = {
    "pose_root_y": {
        "min": min(number(row, "pose_root_world_y") for row in rows),
        "max": max(number(row, "pose_root_world_y") for row in rows),
    },
    "sides": {},
    "pelvis": {},
}

frame_deltas = [number(row, "presentation_delta_seconds") for row in rows]
observed_root_speeds = []
previous = None
for row in rows:
    if previous is not None and integer(row, "frame_sequence") == integer(previous, "frame_sequence") + 1:
        delta_seconds = number(row, "presentation_delta_seconds")
        if delta_seconds > 1e-8:
            observed_root_speeds.append(math.hypot(
                number(row, "pose_root_world_x") - number(previous, "pose_root_world_x"),
                number(row, "pose_root_world_z") - number(previous, "pose_root_world_z")) / delta_seconds)
    previous = row
summary["cadence"] = {
    "delta_seconds_p50": quantile(frame_deltas, 0.5),
    "delta_seconds_p95": quantile(frame_deltas, 0.95),
    "delta_seconds_max": max(frame_deltas),
    "fps_from_p50_delta": 1 / quantile(frame_deltas, 0.5),
    "fps_from_p95_delta": 1 / quantile(frame_deltas, 0.95),
    "frames_over_33ms": sum(value > 1 / 30 for value in frame_deltas),
    "frames_over_50ms": sum(value > 0.05 for value in frame_deltas),
    "observed_root_speed_p50": quantile(observed_root_speeds, 0.5),
    "observed_root_speed_p95": quantile(observed_root_speeds, 0.95),
}

for side in ("left", "right"):
    plans = {}
    rows_by_plan = defaultdict(list)
    rejects = Counter()
    states = Counter()
    executable_rows = 0
    rewritten_rows = 0
    physical_penetrations = []
    solver_residuals = []
    predictive_penetrations = []
    lift_deficits = []
    same_plan_goal_jumps = []
    same_plan_path_jumps = []
    goal_rebounds = []
    surface_aba = []
    absolute_goal_jumps = []
    absolute_path_jumps = []
    absolute_root_jumps = []
    frame_delta_samples = []
    planar_path_errors_by_direction = defaultdict(list)
    planar_root_errors_by_direction = defaultdict(list)
    local_route_residuals_by_direction = defaultdict(list)
    required_lifts_by_direction = defaultdict(list)
    previous = None
    previous_delta = None
    for row in rows:
        state = row[f"{side}_predictive_plan_state"]
        states[state] += 1
        reject = row[f"{side}_prediction_reject"]
        if reject and reject != "None":
            rejects[reject] += 1
        plan_sequence = integer(row, f"{side}_predictive_plan_sequence")
        if plan_sequence and state in ("Planned", "Executing"):
            executable_rows += 1
            plans.setdefault(plan_sequence, row)
            rows_by_plan[plan_sequence].append(row)
        if boolean(row, f"{side}_rewritten"):
            rewritten_rows += 1
        if boolean(row, f"{side}_final_physical_evaluated"):
            physical_penetrations.append(number(row, f"{side}_final_physical_residual_penetration"))
        solver_residuals.append(number(row, f"{side}_position_residual"))
        predictive_penetrations.append(number(row, f"{side}_predictive_residual_penetration"))
        required = number(row, f"{side}_required_lift")
        applied = number(row, f"{side}_applied_lift")
        if finite(required) and finite(applied):
            lift_deficits.append(required - applied)
        if state == "Executing":
            direction = row[f"{side}_plan_invariants_route_direction"]
            planar_error = math.hypot(
                number(row, f"{side}_current_sole_world_x") - number(row, f"{side}_current_path_world_x"),
                number(row, f"{side}_current_sole_world_z") - number(row, f"{side}_current_path_world_z"))
            if finite(planar_error):
                planar_path_errors_by_direction[direction].append(planar_error)
            root_error_x = number(row, "pose_root_world_x") - number(row, f"{side}_current_path_root_world_x")
            root_error_z = number(row, "pose_root_world_z") - number(row, f"{side}_current_path_root_world_z")
            root_error = math.hypot(root_error_x, root_error_z)
            if finite(root_error):
                planar_root_errors_by_direction[direction].append(root_error)
            local_residual = math.hypot(
                (number(row, f"{side}_current_sole_world_x") - number(row, "pose_root_world_x")) -
                (number(row, f"{side}_current_path_world_x") - number(row, f"{side}_current_path_root_world_x")),
                (number(row, f"{side}_current_sole_world_z") - number(row, "pose_root_world_z")) -
                (number(row, f"{side}_current_path_world_z") - number(row, f"{side}_current_path_root_world_z")))
            if finite(local_residual):
                local_route_residuals_by_direction[direction].append(local_residual)
            if finite(required):
                required_lifts_by_direction[direction].append(required)

        if previous is not None:
            same_plan = (
                plan_sequence != 0 and
                plan_sequence == integer(previous, f"{side}_predictive_plan_sequence") and
                integer(row, "frame_sequence") == integer(previous, "frame_sequence") + 1
            )
            goal_delta = number(row, f"{side}_final_goal_world_y") - number(previous, f"{side}_final_goal_world_y")
            path_delta = number(row, f"{side}_current_path_world_y") - number(previous, f"{side}_current_path_world_y")
            if same_plan and finite(goal_delta):
                delta_seconds = number(row, "presentation_delta_seconds")
                clearance = number(row, f"{side}_final_goal_world_y") - number(row, f"{side}_current_path_world_y")
                previous_clearance = number(previous, f"{side}_final_goal_world_y") - number(previous, f"{side}_current_path_world_y")
                clearance_delta = clearance - previous_clearance
                root_delta = number(row, "pose_root_vertical_delta")
                item = {
                    "frame": integer(row, "frame_sequence"),
                    "plan": plan_sequence,
                    "direction": row[f"{side}_plan_invariants_route_direction"],
                    "state": state,
                    "transition": row[f"{side}_predictive_plan_transition"],
                    "action_progress": number(row, f"{side}_landing_action_progress"),
                    "ground_progress": number(row, f"{side}_landing_ground_path_progress"),
                    "goal_delta_cm": goal_delta * 100,
                    "goal_speed_cm_per_second": goal_delta * 100 / delta_seconds if delta_seconds > 1e-8 else None,
                    "path_delta_cm": path_delta * 100 if finite(path_delta) else None,
                    "path_speed_cm_per_second": path_delta * 100 / delta_seconds if finite(path_delta) and delta_seconds > 1e-8 else None,
                    "clearance_delta_cm": clearance_delta * 100 if finite(clearance_delta) else None,
                    "delta_seconds": delta_seconds,
                    "required_lift_cm": required * 100 if finite(required) else None,
                    "applied_lift_cm": applied * 100 if finite(applied) else None,
                    "surface": row[f"{side}_current_path_surface"],
                    "solver_residual_cm": number(row, f"{side}_position_residual") * 100,
                    "physical_penetration_cm": number(row, f"{side}_final_physical_residual_penetration") * 100,
                    "pose_root_delta_cm": root_delta * 100,
                }
                same_plan_goal_jumps.append(item)
                absolute_goal_jumps.append(abs(goal_delta))
                absolute_root_jumps.append(abs(root_delta))
                frame_delta_samples.append(delta_seconds)
                if finite(path_delta):
                    same_plan_path_jumps.append(item)
                    absolute_path_jumps.append(abs(path_delta))
                if previous_delta is not None and goal_delta * previous_delta < 0 and max(abs(goal_delta), abs(previous_delta)) >= 0.03:
                    goal_rebounds.append(item)
                previous_delta = goal_delta
            else:
                previous_delta = None

        previous = row

    plan_rate_metrics = []
    invalid_rate_plans = []
    for plan_id, row in plans.items():
        phases = sequence(row[f"{side}_foot_rate_action_phase_seq"])
        rates = sequence(row[f"{side}_foot_rate_ground_path_progress_seq"])
        lift = number(row, f"{side}_landing_lift_off_phase")
        lift_rate = interpolate(phases, rates, lift)
        post_rates = [rate for phase, rate in zip(phases, rates) if phase > lift + 1e-6]
        first_post = post_rates[0] if post_rates else math.nan
        monotonic = all(rates[i] + 1e-7 >= rates[i - 1] for i in range(1, len(rates)))
        metric = {
            "plan": plan_id,
            "direction": row[f"{side}_plan_invariants_route_direction"],
            "lift_phase": lift,
            "lift_rate": lift_rate,
            "first_post_lift_rate": first_post,
            "landing_rate": rates[-1] if rates else math.nan,
            "monotonic": monotonic,
        }
        plan_rate_metrics.append(metric)
        if not monotonic or abs(lift_rate) > 1e-5 or not rates or abs(rates[-1] - 1) > 1e-5:
            invalid_rate_plans.append(metric)

    trajectory_metrics = []
    for plan_id, plan_rows in rows_by_plan.items():
        first = min(plan_rows, key=lambda value: number(value, f"{side}_landing_action_progress"))
        last = max(plan_rows, key=lambda value: number(value, f"{side}_landing_action_progress"))
        first_progress = number(first, f"{side}_landing_action_progress")
        last_progress = number(last, f"{side}_landing_action_progress")
        frozen_velocity = math.hypot(
            number(first, f"{side}_frozen_planar_velocity_x"),
            number(first, f"{side}_frozen_planar_velocity_z"))
        frozen_root_distance = math.hypot(
            number(first, f"{side}_frozen_root_landing_world_x") - number(first, f"{side}_frozen_root_start_world_x"),
            number(first, f"{side}_frozen_root_landing_world_z") - number(first, f"{side}_frozen_root_start_world_z"))
        actual_root_distance = math.hypot(
            number(last, "pose_root_world_x") - number(first, "pose_root_world_x"),
            number(last, "pose_root_world_z") - number(first, "pose_root_world_z"))
        root_landing_error = math.hypot(
            number(last, "pose_root_world_x") - number(last, f"{side}_frozen_root_landing_world_x"),
            number(last, "pose_root_world_z") - number(last, f"{side}_frozen_root_landing_world_z"))
        path_landing_error = math.hypot(
            number(last, f"{side}_current_path_world_x") - number(last, f"{side}_fixed_landing_world_x"),
            number(last, f"{side}_current_path_world_z") - number(last, f"{side}_fixed_landing_world_z"))
        trajectory_metrics.append({
            "plan": plan_id,
            "direction": first[f"{side}_plan_invariants_route_direction"],
            "first_progress": first_progress,
            "last_progress": last_progress,
            "frozen_speed": frozen_velocity,
            "duration": number(first, f"{side}_plan_swing_duration"),
            "prediction_distance": number(first, f"{side}_prediction_distance"),
            "frozen_root_distance": frozen_root_distance,
            "actual_observed_root_distance": actual_root_distance,
            "root_landing_planar_error": root_landing_error,
            "path_landing_planar_error": path_landing_error,
            "query_count": integer(first, f"{side}_predictive_query_count"),
            "raw_hit_count": integer(first, f"{side}_predictive_raw_hit_count"),
            "accepted_hit_count": integer(first, f"{side}_prediction_accepted_hit_count"),
            "rejected_query_count": integer(first, f"{side}_predictive_rejected_query_count"),
            "route_sample_count": integer(first, f"{side}_prediction_route_sample_count"),
            "envelope_segment_count": integer(first, f"{side}_ground_envelope_segment_count"),
        })
    completed_trajectories = [item for item in trajectory_metrics if item["last_progress"] >= 0.9]

    surfaces_by_plan = defaultdict(list)
    for row in rows:
        plan_id = integer(row, f"{side}_predictive_plan_sequence")
        surface = row[f"{side}_current_path_surface"]
        if plan_id and surface:
            values = surfaces_by_plan[plan_id]
            if not values or surface != values[-1][1]:
                values.append((integer(row, "frame_sequence"), surface))
    for plan_id, values in surfaces_by_plan.items():
        for index in range(2, len(values)):
            if values[index - 2][1] == values[index][1] and values[index - 2][1] != values[index - 1][1]:
                surface_aba.append({"plan": plan_id, "events": values[index - 2:index + 1]})

    summary["sides"][side] = {
        "unique_executable_plans": len(plans),
        "state_rows": dict(states),
        "reject_rows": dict(rejects),
        "executable_row_ratio": executable_rows / len(rows),
        "rewritten_row_ratio": rewritten_rows / len(rows),
        "foot_rate": {
            "invalid_plan_count": len(invalid_rate_plans),
            "invalid_plans": invalid_rate_plans[:10],
            "max_abs_lift_rate": max((abs(item["lift_rate"]) for item in plan_rate_metrics), default=math.nan),
            "max_first_post_lift_rate": max((item["first_post_lift_rate"] for item in plan_rate_metrics if finite(item["first_post_lift_rate"])), default=math.nan),
            "max_abs_landing_error": max((abs(item["landing_rate"] - 1) for item in plan_rate_metrics), default=math.nan),
        },
        "trajectory": {
            "completed_plan_count": len(completed_trajectories),
            "frozen_speed_p50": quantile([item["frozen_speed"] for item in completed_trajectories], 0.5),
            "frozen_speed_p95": quantile([item["frozen_speed"] for item in completed_trajectories], 0.95),
            "duration_p50": quantile([item["duration"] for item in completed_trajectories], 0.5),
            "prediction_distance_p50": quantile([item["prediction_distance"] for item in completed_trajectories], 0.5),
            "frozen_root_distance_p50": quantile([item["frozen_root_distance"] for item in completed_trajectories], 0.5),
            "actual_observed_root_distance_p50": quantile([item["actual_observed_root_distance"] for item in completed_trajectories], 0.5),
            "root_landing_planar_error_p50": quantile([item["root_landing_planar_error"] for item in completed_trajectories], 0.5),
            "root_landing_planar_error_p95": quantile([item["root_landing_planar_error"] for item in completed_trajectories], 0.95),
            "path_landing_planar_error_p95": quantile([item["path_landing_planar_error"] for item in completed_trajectories], 0.95),
            "first_progress_p50": quantile([item["first_progress"] for item in completed_trajectories], 0.5),
            "first_progress_p95": quantile([item["first_progress"] for item in completed_trajectories], 0.95),
            "query_count_p50": quantile([item["query_count"] for item in completed_trajectories], 0.5),
            "query_count_p95": quantile([item["query_count"] for item in completed_trajectories], 0.95),
            "raw_hit_count_p50": quantile([item["raw_hit_count"] for item in completed_trajectories], 0.5),
            "raw_hit_count_p95": quantile([item["raw_hit_count"] for item in completed_trajectories], 0.95),
            "route_sample_count_p50": quantile([item["route_sample_count"] for item in completed_trajectories], 0.5),
            "envelope_segment_count_p50": quantile([item["envelope_segment_count"] for item in completed_trajectories], 0.5),
            "worst_root_landing_errors": sorted(
                completed_trajectories,
                key=lambda item: item["root_landing_planar_error"],
                reverse=True)[:10],
        },
        "same_plan_goal_jump_cm": {
            "p95": quantile([abs(item["goal_delta_cm"]) for item in same_plan_goal_jumps], 0.95),
            "max": max((abs(item["goal_delta_cm"]) for item in same_plan_goal_jumps), default=None),
            "top20": sorted(same_plan_goal_jumps, key=lambda item: abs(item["goal_delta_cm"]), reverse=True)[:20],
        },
        "same_plan_path_jump_cm": {
            "p95": quantile([abs(item["path_delta_cm"]) for item in same_plan_path_jumps], 0.95),
            "max": max((abs(item["path_delta_cm"]) for item in same_plan_path_jumps), default=None),
        },
        "same_plan_jump_correlations": {
            "abs_goal_vs_delta_seconds": correlation(absolute_goal_jumps, frame_delta_samples),
            "abs_path_vs_delta_seconds": correlation(absolute_path_jumps, frame_delta_samples),
            "abs_goal_vs_abs_root_delta": correlation(absolute_goal_jumps, absolute_root_jumps),
            "abs_goal_vs_abs_path_delta": correlation(absolute_goal_jumps, absolute_path_jumps),
        },
        "planar_path_error_cm_by_direction": {
            direction: {
                "count": len(values),
                "p50": quantile(values, 0.5) * 100,
                "p95": quantile(values, 0.95) * 100,
                "max": max(values) * 100,
            }
            for direction, values in planar_path_errors_by_direction.items()
        },
        "planar_root_error_cm_by_direction": {
            direction: {
                "p50": quantile(values, 0.5) * 100,
                "p95": quantile(values, 0.95) * 100,
                "max": max(values) * 100,
            }
            for direction, values in planar_root_errors_by_direction.items()
        },
        "local_route_residual_cm_by_direction": {
            direction: {
                "p50": quantile(values, 0.5) * 100,
                "p95": quantile(values, 0.95) * 100,
                "max": max(values) * 100,
            }
            for direction, values in local_route_residuals_by_direction.items()
        },
        "required_lift_cm_by_direction": {
            direction: {
                "p05": quantile(values, 0.05) * 100,
                "p50": quantile(values, 0.5) * 100,
                "p95": quantile(values, 0.95) * 100,
                "negative_ratio": sum(value < -0.001 for value in values) / len(values),
            }
            for direction, values in required_lifts_by_direction.items()
            if values
        },
        "goal_rebound_count": len(goal_rebounds),
        "surface_aba_count": len(surface_aba),
        "surface_aba": surface_aba[:10],
        "physical_penetration_cm": {
            "p95": quantile(physical_penetrations, 0.95) * 100 if physical_penetrations else None,
            "max": max(physical_penetrations, default=math.nan) * 100,
            "over_1mm": sum(value > 0.001 for value in physical_penetrations),
        },
        "predictive_penetration_cm_max": max((value for value in predictive_penetrations if finite(value)), default=math.nan) * 100,
        "solver_position_residual_cm": {
            "p95": quantile(solver_residuals, 0.95) * 100 if solver_residuals else None,
            "max": max((value for value in solver_residuals if finite(value)), default=math.nan) * 100,
        },
        "lift_deficit_cm": {
            "p95": quantile(lift_deficits, 0.95) * 100 if lift_deficits else None,
            "max": max((value for value in lift_deficits if finite(value)), default=math.nan) * 100,
        },
    }

for name in ("pelvis_resolved_target", "pelvis_current", "pelvis_selected_support_target"):
    deltas = []
    top = []
    previous = None
    for row in rows:
        value = number(row, name)
        if previous is not None and integer(row, "frame_sequence") == integer(previous, "frame_sequence") + 1:
            delta = value - number(previous, name)
            if finite(delta):
                deltas.append(abs(delta))
                top.append({
                    "frame": integer(row, "frame_sequence"),
                    "direction": row["left_plan_invariants_route_direction"],
                    "delta_cm": delta * 100,
                    "pose_root_delta_cm": number(row, "pose_root_vertical_delta") * 100,
                    "plan_sequence": integer(row, "pelvis_support_plan_sequence"),
                    "switched": boolean(row, "pelvis_support_switched"),
                })
        previous = row
    summary["pelvis"][name] = {
        "p95_delta_cm": quantile(deltas, 0.95) * 100 if deltas else None,
        "max_delta_cm": max(deltas, default=math.nan) * 100,
        "top10": sorted(top, key=lambda item: abs(item["delta_cm"]), reverse=True)[:10],
    }

world_pelvis_deltas = []
world_pelvis_top = []
previous = None
for row in rows:
    current_world = number(row, "pose_root_world_y") + number(row, "pelvis_current")
    if previous is not None and integer(row, "frame_sequence") == integer(previous, "frame_sequence") + 1:
        previous_world = number(previous, "pose_root_world_y") + number(previous, "pelvis_current")
        delta = current_world - previous_world
        root_delta = number(row, "pose_root_vertical_delta")
        relative_delta = number(row, "pelvis_current") - number(previous, "pelvis_current")
        if finite(delta):
            world_pelvis_deltas.append(abs(delta))
            world_pelvis_top.append({
                "frame": integer(row, "frame_sequence"),
                "direction": row["left_plan_invariants_route_direction"],
                "world_delta_cm": delta * 100,
                "pose_root_delta_cm": root_delta * 100,
                "relative_offset_delta_cm": relative_delta * 100,
                "selected_target_delta_cm": (
                    number(row, "pelvis_selected_support_target") -
                    number(previous, "pelvis_selected_support_target")
                ) * 100,
                "plan_sequence": integer(row, "pelvis_support_plan_sequence"),
                "switched": boolean(row, "pelvis_support_switched"),
            })
    previous = row
summary["pelvis"]["world_output"] = {
    "p95_delta_cm": quantile(world_pelvis_deltas, 0.95) * 100 if world_pelvis_deltas else None,
    "max_delta_cm": max(world_pelvis_deltas, default=math.nan) * 100,
    "top20": sorted(world_pelvis_top, key=lambda item: abs(item["world_delta_cm"]), reverse=True)[:20],
}

print("ANALYSIS")
print(json.dumps(summary, ensure_ascii=False, indent=2, allow_nan=False))

focus_frames = {79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 792, 793, 794, 795, 796, 1299, 1300, 1301, 1302, 1303, 1304, 1305, 1306, 1307, 1308}
focused = []
for row in rows:
    frame = integer(row, "frame_sequence")
    if frame not in focus_frames:
        continue
    item = {
        "frame": frame,
        "dt": number(row, "presentation_delta_seconds"),
        "root_y": number(row, "pose_root_world_y"),
        "root_dy": number(row, "pose_root_vertical_delta"),
        "pelvis": number(row, "pelvis_current"),
        "pelvis_world": number(row, "pose_root_world_y") + number(row, "pelvis_current"),
        "pelvis_target": number(row, "pelvis_selected_support_target"),
    }
    for side in ("left", "right"):
        item[side] = {
            "plan": integer(row, f"{side}_predictive_plan_sequence"),
            "state": row[f"{side}_predictive_plan_state"],
            "action": number(row, f"{side}_landing_action_progress"),
            "ground": number(row, f"{side}_landing_ground_path_progress"),
            "pose_weight": number(row, f"{side}_current_event_foot_pose_weight"),
            "plan_blend": number(row, f"{side}_plan_prediction_blend"),
            "sync_blend": number(row, f"{side}_pose_synchronized_prediction_blend"),
            "rewritten": boolean(row, f"{side}_rewritten"),
            "path_y": number(row, f"{side}_current_path_world_y"),
            "sole_y": number(row, f"{side}_current_sole_world_y"),
            "baseline_y": number(row, f"{side}_baseline_goal_world_y"),
            "final_y": number(row, f"{side}_final_goal_world_y"),
            "required_lift": number(row, f"{side}_required_lift"),
            "applied_lift": number(row, f"{side}_applied_lift"),
        }
    focused.append(item)
print("FOCUSED")
print(json.dumps(focused, ensure_ascii=False, indent=2, allow_nan=False))
print("TRAJECTORY")
print(json.dumps({
    "cadence": summary["cadence"],
    "left": summary["sides"]["left"]["trajectory"],
    "right": summary["sides"]["right"]["trajectory"],
}, ensure_ascii=False, indent=2, allow_nan=False))
print("GEOMETRY")
print(json.dumps({
    side: {
        "planar_path_error_cm_by_direction": summary["sides"][side]["planar_path_error_cm_by_direction"],
        "planar_root_error_cm_by_direction": summary["sides"][side]["planar_root_error_cm_by_direction"],
        "local_route_residual_cm_by_direction": summary["sides"][side]["local_route_residual_cm_by_direction"],
        "required_lift_cm_by_direction": summary["sides"][side]["required_lift_cm_by_direction"],
        "jump_correlations": summary["sides"][side]["same_plan_jump_correlations"],
    }
    for side in ("left", "right")
}, ensure_ascii=False, indent=2, allow_nan=False))
