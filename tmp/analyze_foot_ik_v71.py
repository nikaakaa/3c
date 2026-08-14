import csv
import glob
import gzip
import json
import math
import os
import sys
from collections import Counter, defaultdict


def number(value):
    try:
        result = float(value)
        return result if math.isfinite(result) else None
    except (TypeError, ValueError):
        return None


def integer(value):
    value = number(value)
    return int(value) if value is not None else None


def truth(value):
    return str(value).strip().lower() in {"true", "1", "yes"}


def quantiles(values):
    values = sorted(v for v in values if v is not None and math.isfinite(v))
    if not values:
        return {"count": 0}

    def at(fraction):
        index = min(len(values) - 1, max(0, round((len(values) - 1) * fraction)))
        return values[index]

    return {
        "count": len(values),
        "min": values[0],
        "p50": at(0.50),
        "p95": at(0.95),
        "p99": at(0.99),
        "max": values[-1],
    }


def main():
    run_dir = sys.argv[1]
    files = sorted(glob.glob(os.path.join(run_dir, "*.csv.gz")))
    if not files:
        raise SystemExit("no chunks")

    expected_width = int(sys.argv[3]) if len(sys.argv) > 3 else 1011
    header = None
    width_errors = []
    rows = []
    for path in files:
        with gzip.open(path, "rt", encoding="utf-8-sig", newline="") as stream:
            reader = csv.reader(stream)
            file_header = next(reader)
            if header is None:
                header = file_header
            elif file_header != header:
                width_errors.append({"file": os.path.basename(path), "kind": "header_mismatch"})
            if len(file_header) != expected_width:
                width_errors.append({"file": os.path.basename(path), "kind": "header_width", "width": len(file_header)})
            for line_number, values in enumerate(reader, 2):
                if len(values) != expected_width:
                    width_errors.append({"file": os.path.basename(path), "line": line_number, "kind": "row_width", "width": len(values)})
                    continue
                rows.append(dict(zip(header, values)))

    directions = Counter(row["left_plan_invariants_route_direction"] for row in rows)
    main_directions = [value for value in ("start-to-end", "end-to-start") if directions[value]]
    summary = {
        "files": len(files),
        "rows": len(rows),
        "header_width": len(header),
        "unique_headers": len(set(header)),
        "width_errors": width_errors[:20],
        "direction_rows": directions,
        "legs": {},
    }

    for leg in ("left", "right"):
        leg_summary = {}
        for direction in main_directions + ["hold-start", "hold-end", "settle-start", "settle-end"]:
            subset = [row for row in rows if row[f"{leg}_plan_invariants_route_direction"] == direction]
            if not subset:
                continue

            state_counts = Counter(row[f"{leg}_predictive_plan_state"] for row in subset)
            transition_counts = Counter(row[f"{leg}_predictive_plan_transition"] for row in subset if row[f"{leg}_predictive_plan_transition"])
            end_reason_counts = Counter(row[f"{leg}_predictive_plan_end_reason"] for row in subset if row[f"{leg}_predictive_plan_end_reason"])
            envelope_rejects = Counter(row[f"{leg}_ground_envelope_reject"] for row in subset if row[f"{leg}_ground_envelope_reject"])

            path_missing = 0
            executable_rows = 0
            pre_liftoff_rows = 0
            pre_liftoff_progress_violations = 0
            pre_liftoff_predictive_lift_violations = 0
            goal_jumps = []
            path_jumps = []
            same_plan_goal_jumps = []
            same_plan_path_jumps = []
            progress_jumps = []
            solved_jumps = []
            heel_distances = []
            toe_distances = []
            penetrations = []
            predictive_penetrations = []
            solver_residuals = []
            required_minus_applied = []
            terrain_deltas = {}
            plan_hashes = defaultdict(lambda: defaultdict(set))
            plan_progress = defaultdict(list)
            plan_action_progress = defaultdict(list)
            surface_series = []
            anomalies = []
            previous = None

            for row in subset:
                frame = integer(row["frame_sequence"])
                plan = integer(row[f"{leg}_predictive_plan_sequence"])
                has_geometry = truth(row[f"{leg}_plan_has_path_geometry"])
                executable = truth(row[f"{leg}_plan_has_executable_path"])
                if executable:
                    executable_rows += 1
                if not has_geometry:
                    path_missing += 1

                seconds_to_lift = number(row[f"{leg}_plan_seconds_to_lift_off"])
                ground_progress = number(row[f"{leg}_landing_ground_path_progress"])
                action_progress = number(row[f"{leg}_landing_action_progress"])
                applied_lift = number(row[f"{leg}_applied_lift"])
                required_lift = number(row[f"{leg}_required_lift"])
                event_phase = number(row[f"{leg}_landing_event_phase"])
                lift_off_phase = number(row[f"{leg}_landing_lift_off_phase"])
                if (event_phase is not None and lift_off_phase is not None and
                        event_phase + 1e-5 < lift_off_phase and executable):
                    pre_liftoff_rows += 1
                    if ground_progress is not None and ground_progress > 1e-4:
                        pre_liftoff_progress_violations += 1
                    if applied_lift is not None and applied_lift > 1e-4:
                        pre_liftoff_predictive_lift_violations += 1

                heel = number(row[f"{leg}_final_physical_heel_plane_distance"])
                toe = number(row[f"{leg}_final_physical_toe_plane_distance"])
                penetration = number(row[f"{leg}_final_physical_residual_penetration"])
                predictive_penetration = number(row[f"{leg}_predictive_residual_penetration"])
                residual = number(row[f"{leg}_position_residual"])
                if truth(row[f"{leg}_final_physical_evaluated"]):
                    heel_distances.append(heel)
                    toe_distances.append(toe)
                    penetrations.append(penetration)
                predictive_penetrations.append(predictive_penetration)
                solver_residuals.append(residual)
                if required_lift is not None and applied_lift is not None:
                    required_minus_applied.append(required_lift - applied_lift)

                if plan and plan > 0:
                    if ground_progress is not None:
                        plan_progress[plan].append((frame, ground_progress))
                    if action_progress is not None:
                        plan_action_progress[plan].append((frame, action_progress))
                    for name in ("ground_probe_hash", "plan_invariants_plan_hashes_seq", "foot_rate_action_phase_seq", "foot_rate_ground_path_progress_seq", "clearance_path_start_height_seq", "clearance_path_end_height_seq"):
                        plan_hashes[plan][name].add(row[f"{leg}_{name}"])
                    if plan not in terrain_deltas:
                        start_y = number(row[f"{leg}_fixed_path_start_world_y"])
                        landing_y = number(row[f"{leg}_fixed_landing_world_y"])
                        if start_y is not None and landing_y is not None:
                            terrain_deltas[plan] = landing_y - start_y

                surface_series.append((frame, row[f"{leg}_current_path_surface"], plan))
                if previous is not None:
                    same_plan = plan is not None and plan > 0 and plan == previous["plan"]
                    goal_y = number(row[f"{leg}_final_goal_world_y"])
                    path_y = number(row[f"{leg}_current_path_world_y"])
                    solved_y = number(row[f"{leg}_solved_y"])
                    prev_goal_y = previous["goal_y"]
                    prev_path_y = previous["path_y"]
                    prev_progress = previous["progress"]
                    prev_solved_y = previous["solved_y"]
                    goal_delta = goal_y - prev_goal_y if goal_y is not None and prev_goal_y is not None else None
                    path_delta = path_y - prev_path_y if path_y is not None and prev_path_y is not None else None
                    progress_delta = ground_progress - prev_progress if ground_progress is not None and prev_progress is not None and same_plan else None
                    solved_delta = solved_y - prev_solved_y if solved_y is not None and prev_solved_y is not None else None
                    goal_jumps.append(abs(goal_delta) if goal_delta is not None else None)
                    path_jumps.append(abs(path_delta) if path_delta is not None else None)
                    if same_plan:
                        same_plan_goal_jumps.append(abs(goal_delta) if goal_delta is not None else None)
                        same_plan_path_jumps.append(abs(path_delta) if path_delta is not None else None)
                    progress_jumps.append(abs(progress_delta) if progress_delta is not None else None)
                    solved_jumps.append(abs(solved_delta) if solved_delta is not None else None)
                    score = max(abs(goal_delta or 0), abs(path_delta or 0), penetration or 0)
                    anomalies.append({
                        "score": score,
                        "frame": frame,
                        "plan": plan,
                        "same_plan": same_plan,
                        "state": row[f"{leg}_predictive_plan_state"],
                        "transition": row[f"{leg}_predictive_plan_transition"],
                        "seconds_to_lift_off": seconds_to_lift,
                        "action_progress": action_progress,
                        "ground_path_progress": ground_progress,
                        "goal_delta_y": goal_delta,
                        "path_delta_y": path_delta,
                        "solved_delta_y": solved_delta,
                        "required_lift": required_lift,
                        "applied_lift": applied_lift,
                        "physical_penetration": penetration,
                        "solver_residual": residual,
                        "surface": row[f"{leg}_current_path_surface"],
                    })
                previous = {
                    "plan": plan,
                    "goal_y": number(row[f"{leg}_final_goal_world_y"]),
                    "path_y": number(row[f"{leg}_current_path_world_y"]),
                    "progress": ground_progress,
                    "solved_y": number(row[f"{leg}_solved_y"]),
                }

            monotonic_violations = []
            for plan, values in plan_progress.items():
                for (previous_frame, previous_value), (frame, value) in zip(values, values[1:]):
                    if value + 1e-5 < previous_value:
                        monotonic_violations.append({"plan": plan, "frame": frame, "previous_frame": previous_frame, "previous": previous_value, "current": value})

            changing_plans = {}
            for plan, fields in plan_hashes.items():
                changed = {name: len(values) for name, values in fields.items() if len(values) > 1}
                if changed:
                    changing_plans[plan] = changed

            aba = []
            for index in range(2, len(surface_series)):
                a = surface_series[index - 2]
                b = surface_series[index - 1]
                c = surface_series[index]
                if a[1] and a[1] == c[1] and a[1] != b[1] and a[2] == b[2] == c[2] and a[2]:
                    aba.append({"frames": [a[0], b[0], c[0]], "plan": a[2], "surfaces": [a[1], b[1], c[1]]})

            terrain_values = list(terrain_deltas.values())
            leg_summary[direction] = {
                "rows": len(subset),
                "state_counts": state_counts,
                "transition_counts": transition_counts,
                "end_reason_counts": end_reason_counts,
                "envelope_rejects": envelope_rejects,
                "path_missing_rows": path_missing,
                "executable_rows": executable_rows,
                "pre_liftoff_rows": pre_liftoff_rows,
                "pre_liftoff_progress_violations": pre_liftoff_progress_violations,
                "pre_liftoff_predictive_lift_violations": pre_liftoff_predictive_lift_violations,
                "terrain_delta": quantiles(terrain_values),
                "positive_terrain_plans": sum(1 for value in terrain_values if value > 0.02),
                "negative_terrain_plans": sum(1 for value in terrain_values if value < -0.02),
                "flat_terrain_plans": sum(1 for value in terrain_values if abs(value) <= 0.02),
                "goal_abs_delta_y": quantiles(goal_jumps),
                "path_abs_delta_y": quantiles(path_jumps),
                "same_plan_goal_abs_delta_y": quantiles(same_plan_goal_jumps),
                "same_plan_path_abs_delta_y": quantiles(same_plan_path_jumps),
                "ground_progress_abs_delta": quantiles(progress_jumps),
                "solved_abs_delta_y": quantiles(solved_jumps),
                "physical_heel_plane_distance": quantiles(heel_distances),
                "physical_toe_plane_distance": quantiles(toe_distances),
                "physical_residual_penetration": quantiles(penetrations),
                "predictive_residual_penetration": quantiles(predictive_penetrations),
                "solver_position_residual": quantiles(solver_residuals),
                "required_minus_applied": quantiles(required_minus_applied),
                "monotonic_violation_count": len(monotonic_violations),
                "monotonic_violations": monotonic_violations[:20],
                "changing_plan_snapshot_count": len(changing_plans),
                "changing_plan_snapshots": dict(list(changing_plans.items())[:10]),
                "surface_aba_count": len(aba),
                "surface_aba": aba[:20],
                "top_anomalies": sorted(anomalies, key=lambda value: value["score"], reverse=True)[:10],
            }
        summary["legs"][leg] = leg_summary

    payload = json.dumps(summary, ensure_ascii=False, indent=2, default=lambda value: dict(value))
    if len(sys.argv) > 2:
        with open(sys.argv[2], "w", encoding="utf-8", newline="\n") as stream:
            stream.write(payload)
            stream.write("\n")
    else:
        print(payload)


if __name__ == "__main__":
    main()
