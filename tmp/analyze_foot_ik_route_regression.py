import csv
import gzip
import json
import math
import pathlib
import sys


def finite(value):
    try:
        result = float(value)
        return result if math.isfinite(result) else None
    except (TypeError, ValueError):
        return None


def integer(value):
    try:
        return int(value)
    except (TypeError, ValueError):
        return 0


def sequence(row, key):
    value = row.get(key, "")
    if not value or value == "None":
        return []
    result = []
    for item in value.split(";"):
        number = finite(item)
        if number is None:
            return []
        result.append(number)
    return result


def points(row, prefix):
    xs = sequence(row, f"{prefix}_x_seq")
    ys = sequence(row, f"{prefix}_y_seq")
    zs = sequence(row, f"{prefix}_z_seq")
    if not xs or len(xs) != len(ys) or len(xs) != len(zs):
        return []
    return list(zip(xs, ys, zs))


def lerp(a, b, t):
    return tuple(a[i] + (b[i] - a[i]) * t for i in range(3))


def evaluate(phases, route, phase):
    if len(phases) < 2 or len(phases) != len(route):
        return None
    if phase <= phases[0]:
        return route[0]
    if phase >= phases[-1]:
        return route[-1]
    for index in range(1, len(phases)):
        if phase > phases[index]:
            continue
        duration = phases[index] - phases[index - 1]
        t = 1.0 if duration <= 1e-8 else (phase - phases[index - 1]) / duration
        return lerp(route[index - 1], route[index], max(0.0, min(1.0, t)))
    return route[-1]


def planar_distance(a, b):
    return math.hypot(a[0] - b[0], a[2] - b[2])


def distance(a, b):
    return math.sqrt(sum((a[i] - b[i]) ** 2 for i in range(3)))


def planar_length(route):
    return sum(planar_distance(route[index - 1], route[index]) for index in range(1, len(route)))


def percentile(values, fraction):
    if not values:
        return None
    ordered = sorted(values)
    index = min(len(ordered) - 1, max(0, round((len(ordered) - 1) * fraction)))
    return ordered[index]


def summary(values):
    return {
        "count": len(values),
        "median_cm": None if not values else round(percentile(values, 0.5) * 100.0, 3),
        "p95_cm": None if not values else round(percentile(values, 0.95) * 100.0, 3),
        "maximum_cm": None if not values else round(max(values) * 100.0, 3),
    }


run_dir = pathlib.Path(sys.argv[1])
manifest = json.loads((run_dir / "manifest.json").read_text(encoding="utf-8"))
expected_columns = integer(manifest.get("columnCount"))
header = None
rows = []
bad_width = []
for path in sorted(run_dir.glob("chunk-*.csv.gz")):
    if ".partial." in path.name or path.stat().st_size == 0:
        continue
    with gzip.open(path, "rt", encoding="utf-8-sig", newline="") as stream:
        reader = csv.reader(stream)
        current_header = next(reader)
        if header is None:
            header = current_header
        elif current_header != header:
            raise RuntimeError(f"header mismatch: {path.name}")
        for line, values in enumerate(reader, 2):
            if len(values) != len(header):
                bad_width.append((path.name, line, len(values)))
                continue
            row = dict(zip(header, values))
            row["__chunk"] = path.name
            rows.append(row)


plan_reports = []
frame_reports = []
same_plan_deltas = []
idle_rows = []
seen_plans = set()
previous_by_side = {}


def recorded_route(direction):
    return direction.endswith("start-to-end") or direction.endswith("end-to-start")


for row in rows:
    for side in ("left", "right"):
        direction = row.get(f"{side}_plan_invariants_route_direction", "")
        state = row.get(f"{side}_predictive_plan_state", "")
        plan_sequence = integer(row.get(f"{side}_predictive_plan_sequence"))
        landing_identity = integer(row.get(f"{side}_plan_landing_event_identity"))
        plan_key = (side, plan_sequence, landing_identity)
        if state == "Executing" and recorded_route(direction) and plan_sequence > 0:
            animation_phases = sequence(row, f"{side}_animation_foot_route_phase_seq")
            animation_route = points(row, f"{side}_animation_foot_route")
            probe_fractions = sequence(row, f"{side}_ground_probe_fraction_seq")
            probe_route = points(row, f"{side}_ground_probe")
            if plan_key not in seen_plans and animation_route and probe_route:
                seen_plans.add(plan_key)
                path_start_phase = animation_phases[0]
                split_phase = finite(row.get(f"{side}_virtual_ground_split_event_phase")) or 0.0
                split_fraction = finite(row.get(f"{side}_landing_virtual_ground_split_fraction")) or 0.0
                probe_phases = []
                for fraction in probe_fractions:
                    if 0.0 < split_fraction < 1.0 and path_start_phase < split_phase < 1.0:
                        phase = (
                            path_start_phase + (split_phase - path_start_phase) * fraction / split_fraction
                            if fraction <= split_fraction
                            else split_phase + (1.0 - split_phase) * (fraction - split_fraction) / (1.0 - split_fraction)
                        )
                    else:
                        phase = path_start_phase + (1.0 - path_start_phase) * fraction
                    probe_phases.append(phase)
                deviations = []
                for phase, point in zip(probe_phases, probe_route):
                    authored = evaluate(animation_phases, animation_route, phase)
                    if authored is not None:
                        deviations.append(planar_distance(point, authored))
                split_route = (
                    finite(row.get(f"{side}_virtual_ground_split_route_x")) or 0.0,
                    finite(row.get(f"{side}_virtual_ground_split_route_y")) or 0.0,
                    finite(row.get(f"{side}_virtual_ground_split_route_z")) or 0.0,
                )
                authored_at_split = evaluate(animation_phases, animation_route, split_phase)
                plan_reports.append({
                    "side": side,
                    "direction": direction,
                    "lap": row.get(f"{side}_plan_invariants_route_lap", ""),
                    "plan": plan_sequence,
                    "landing": landing_identity,
                    "generated_frame": integer(row.get(f"{side}_predictive_plan_generated_frame")),
                    "observed_frame": integer(row.get("frame_sequence")),
                    "has_revision": row.get(f"{side}_has_plan_revision", ""),
                    "revision_plan": integer(row.get(f"{side}_revision_plan_sequence")),
                    "revision_blend": finite(row.get(f"{side}_plan_revision_blend_weight")),
                    "final_source": row.get(f"{side}_final_source", ""),
                    "contact": row.get(f"{side}_contact", ""),
                    "has_anchor": row.get(f"{side}_has_anchor", ""),
                    "anchor_blend": finite(row.get(f"{side}_anchor_blend")),
                    "current_sole": tuple(finite(row.get(f"{side}_current_sole_world_{axis}")) for axis in "xyz"),
                    "animation_length_cm": round(planar_length(animation_route) * 100.0, 3),
                    "probe_length_cm": round(planar_length(probe_route) * 100.0, 3),
                    "endpoint_displacement_cm": round(planar_distance(animation_route[0], animation_route[-1]) * 100.0, 3),
                    "probe_start_to_animation_cm": round(planar_distance(probe_route[0], animation_route[0]) * 100.0, 3),
                    "probe_end_to_animation_cm": round(planar_distance(probe_route[-1], animation_route[-1]) * 100.0, 3),
                    "animation_start": animation_route[0],
                    "probe_start": probe_route[0],
                    "animation_end": animation_route[-1],
                    "probe_end": probe_route[-1],
                    "probe_to_animation_max_cm": round(max(deviations) * 100.0, 3) if deviations else None,
                    "probe_to_animation_mean_cm": round(sum(deviations) / len(deviations) * 100.0, 3) if deviations else None,
                    "split_phase": round(split_phase, 6),
                    "split_to_animation_cm": round(planar_distance(split_route, authored_at_split) * 100.0, 3)
                    if split_phase > 0.0 and authored_at_split is not None else None,
                    "split_fraction": split_fraction,
                    "probe_hash": row.get(f"{side}_ground_probe_hash", ""),
                    "animation_hash": row.get(f"{side}_animation_foot_route_hash", ""),
                })

            action_progress = finite(row.get(f"{side}_landing_action_progress"))
            foot_rate_phases = sequence(row, f"{side}_foot_rate_action_phase_seq")
            if animation_phases and animation_route and foot_rate_phases and probe_route and action_progress is not None:
                action_phase = animation_phases[0] + action_progress * (1.0 - animation_phases[0])
                animation_point = evaluate(animation_phases, animation_route, action_phase)
                probe_progresses = sequence(row, f"{side}_ground_probe_fraction_seq")
                probe_point = evaluate(probe_progresses, probe_route, finite(row.get(f"{side}_landing_ground_path_progress")) or 0.0)
                current_path = tuple(finite(row.get(f"{side}_current_path_world_{axis}")) for axis in "xyz")
                current_sole = tuple(finite(row.get(f"{side}_current_sole_world_{axis}")) for axis in "xyz")
                if animation_point and probe_point and None not in current_path and None not in current_sole:
                    frame_reports.append({
                        "side": side,
                        "direction": direction,
                        "lap": row.get(f"{side}_plan_invariants_route_lap", ""),
                        "frame": integer(row.get("frame_sequence")),
                        "tick": integer(row.get(f"{side}_plan_invariants_simulation_tick")),
                        "plan": plan_sequence,
                        "landing": landing_identity,
                        "action_phase": action_phase,
                        "prediction_blend": finite(row.get(f"{side}_committed_prediction_blend")) or 0.0,
                        "probe_animation_error": planar_distance(probe_point, animation_point),
                        "path_animation_error": planar_distance(current_path, animation_point),
                        "path_probe_error": planar_distance(current_path, probe_point),
                        "current_sole_animation_error": planar_distance(current_sole, animation_point),
                        "current_sole_path_error": planar_distance(current_sole, current_path),
                        "solver_position_residual": finite(row.get(f"{side}_position_residual")) or 0.0,
                        "physical_penetration": finite(row.get(f"{side}_final_physical_residual_penetration")) or 0.0,
                    })

            previous = previous_by_side.get(side)
            current_tick = integer(row.get(f"{side}_plan_invariants_simulation_tick"))
            if previous is not None and previous["plan"] == plan_sequence and previous["landing"] == landing_identity and current_tick > previous["tick"]:
                path = tuple(finite(row.get(f"{side}_current_path_world_{axis}")) for axis in "xyz")
                goal = tuple(finite(row.get(f"{side}_final_goal_world_{axis}")) for axis in "xyz")
                if None not in path and None not in goal:
                    same_plan_deltas.append({
                        "side": side,
                        "direction": direction,
                        "lap": row.get(f"{side}_plan_invariants_route_lap", ""),
                        "frame": integer(row.get("frame_sequence")),
                        "tick": current_tick,
                        "tick_delta": current_tick - previous["tick"],
                        "plan": plan_sequence,
                        "landing": landing_identity,
                        "path_delta": distance(path, previous["path"]),
                        "goal_delta": distance(goal, previous["goal"]),
                        "path_y_delta": path[1] - previous["path"][1],
                        "goal_y_delta": goal[1] - previous["goal"][1],
                    })
            path = tuple(finite(row.get(f"{side}_current_path_world_{axis}")) for axis in "xyz")
            goal = tuple(finite(row.get(f"{side}_final_goal_world_{axis}")) for axis in "xyz")
            if None not in path and None not in goal:
                previous_by_side[side] = {
                    "plan": plan_sequence,
                    "landing": landing_identity,
                    "tick": current_tick,
                    "path": path,
                    "goal": goal,
                }
        elif state == "Inactive" and (recorded_route(direction) or direction == ""):
            speed = finite(row.get(f"{side}_plan_invariants_actual_planar_speed"))
            if speed is not None and speed < 0.05:
                idle_rows.append({
                    "side": side,
                    "contact": row.get(f"{side}_contact", ""),
                    "anchor": row.get(f"{side}_has_anchor", ""),
                    "anchor_blend": finite(row.get(f"{side}_anchor_blend")) or 0.0,
                    "placement_weight": finite(row.get(f"{side}_placement_weight")) or 0.0,
                    "baseline_final_error": math.sqrt(sum(
                        ((finite(row.get(f"{side}_final_goal_world_{axis}")) or 0.0) -
                         (finite(row.get(f"{side}_baseline_goal_world_{axis}")) or 0.0)) ** 2
                        for axis in "xyz")),
                })


frame_probe_animation = [item["probe_animation_error"] for item in frame_reports]
frame_path_animation = [item["path_animation_error"] for item in frame_reports]
frame_path_probe = [item["path_probe_error"] for item in frame_reports]
frame_current_animation = [item["current_sole_animation_error"] for item in frame_reports]
frame_current_path = [item["current_sole_path_error"] for item in frame_reports]
solver_residuals = [item["solver_position_residual"] for item in frame_reports]
penetrations = [item["physical_penetration"] for item in frame_reports]
idle_goal_errors = [item["baseline_final_error"] for item in idle_rows]


result = {
    "run": run_dir.name,
    "schema": manifest.get("schema"),
    "manifest_rows": manifest.get("totalRows"),
    "read_rows": len(rows),
    "header_columns": len(header or []),
    "manifest_columns": expected_columns,
    "bad_width_rows": bad_width,
    "unique_executing_plans": len(plan_reports),
    "plan_route_metrics": {
        "probe_to_animation_max": summary([item["probe_to_animation_max_cm"] / 100.0 for item in plan_reports if item["probe_to_animation_max_cm"] is not None]),
        "split_to_animation": summary([item["split_to_animation_cm"] / 100.0 for item in plan_reports if item["split_to_animation_cm"] is not None]),
        "probe_over_animation_length_ratio": {
            "median": round(percentile([item["probe_length_cm"] / item["animation_length_cm"] for item in plan_reports if item["animation_length_cm"] > 0.0], 0.5), 4) if plan_reports else None,
            "maximum": round(max([item["probe_length_cm"] / item["animation_length_cm"] for item in plan_reports if item["animation_length_cm"] > 0.0], default=0.0), 4),
        },
    },
    "executing_frame_metrics": {
        "probe_to_frozen_animation_xz": summary(frame_probe_animation),
        "consumed_path_to_frozen_animation_xz": summary(frame_path_animation),
        "consumed_path_to_probe_xz": summary(frame_path_probe),
        "current_sole_to_frozen_animation_xz": summary(frame_current_animation),
        "current_sole_to_consumed_path_xz": summary(frame_current_path),
        "fbbik_position_residual": summary(solver_residuals),
        "physical_penetration": summary(penetrations),
    },
    "same_plan_tick_deltas": {
        "path_3d": summary([item["path_delta"] for item in same_plan_deltas]),
        "goal_3d": summary([item["goal_delta"] for item in same_plan_deltas]),
        "path_y": summary([abs(item["path_y_delta"]) for item in same_plan_deltas]),
        "goal_y": summary([abs(item["goal_y_delta"]) for item in same_plan_deltas]),
    },
    "idle": {
        "rows": len(idle_rows),
        "anchor_true": sum(item["anchor"].lower() == "true" for item in idle_rows),
        "anchor_blend_over_099": sum(item["anchor_blend"] >= 0.99 for item in idle_rows),
        "final_to_baseline": summary(idle_goal_errors),
    },
    "top_plan_route_deviations": sorted(
        plan_reports,
        key=lambda item: item["probe_to_animation_max_cm"] or 0.0,
        reverse=True,
    )[:20],
    "top_executing_frame_route_deviations": sorted(
        frame_reports,
        key=lambda item: item["path_animation_error"],
        reverse=True,
    )[:20],
    "top_same_plan_goal_deltas": sorted(
        same_plan_deltas,
        key=lambda item: item["goal_delta"],
        reverse=True,
    )[:20],
}
print(json.dumps(result, ensure_ascii=False, indent=2))
