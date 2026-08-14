import csv
import glob
import gzip
import json
import math
import os
import sys
from collections import defaultdict


def f(value):
    try:
        value = float(value)
        return value if math.isfinite(value) else None
    except (TypeError, ValueError):
        return None


def q(values):
    values = sorted(value for value in values if value is not None and math.isfinite(value))
    if not values:
        return {"count": 0}

    def pick(frac):
        return values[round((len(values) - 1) * frac)]

    return {"count": len(values), "min": values[0], "p50": pick(.5), "p95": pick(.95), "max": values[-1]}


def distance_xz(a, b):
    return math.hypot(b[0] - a[0], b[1] - a[1])


def main():
    run_dir = sys.argv[1]
    rows = []
    for path in sorted(glob.glob(os.path.join(run_dir, "*.csv.gz"))):
        with gzip.open(path, "rt", encoding="utf-8-sig", newline="") as stream:
            rows.extend(csv.DictReader(stream))

    output = {}
    main_dirs = {"start-to-end", "end-to-start"}
    for leg in ("left", "right"):
        plans = defaultdict(list)
        for row in rows:
            if row[f"{leg}_plan_invariants_route_direction"] not in main_dirs:
                continue
            plan = int(float(row[f"{leg}_predictive_plan_sequence"] or 0))
            if plan > 0:
                plans[plan].append(row)

        buckets = defaultdict(lambda: {
            "plans": 0,
            "vertical_delta": [],
            "planned_foot_horizontal": [],
            "planned_root_horizontal": [],
            "actual_actor_horizontal": [],
            "root_distance_error": [],
            "same_plan_goal_delta_y": [],
            "same_plan_path_delta_y": [],
            "solver_residual": [],
            "physical_penetration": [],
            "predictive_penetration": [],
            "required_minus_applied": [],
            "goal_reversals_over_5cm": 0,
            "path_reversals_over_5cm": 0,
            "pre_liftoff_progress_violations": 0,
            "pre_liftoff_lift_violations": 0,
            "examples": [],
            "top_frames": [],
        })

        for plan, values in plans.items():
            values.sort(key=lambda row: int(float(row["frame_sequence"])))
            first = values[0]
            start = (f(first[f"{leg}_fixed_path_start_world_x"]), f(first[f"{leg}_fixed_path_start_world_y"]), f(first[f"{leg}_fixed_path_start_world_z"]))
            landing = (f(first[f"{leg}_fixed_landing_world_x"]), f(first[f"{leg}_fixed_landing_world_y"]), f(first[f"{leg}_fixed_landing_world_z"]))
            root_start = (f(first[f"{leg}_frozen_root_start_world_x"]), f(first[f"{leg}_frozen_root_start_world_y"]), f(first[f"{leg}_frozen_root_start_world_z"]))
            root_landing = (f(first[f"{leg}_frozen_root_landing_world_x"]), f(first[f"{leg}_frozen_root_landing_world_y"]), f(first[f"{leg}_frozen_root_landing_world_z"]))
            actor_start = (f(first[f"{leg}_plan_invariants_route_actor_x"]), f(first[f"{leg}_plan_invariants_route_actor_z"]))
            last = values[-1]
            actor_end = (f(last[f"{leg}_plan_invariants_route_actor_x"]), f(last[f"{leg}_plan_invariants_route_actor_z"]))
            if any(value is None for value in start + landing + root_start + root_landing + actor_start + actor_end):
                continue
            vertical = landing[1] - start[1]
            kind = "up" if vertical > .02 else "down" if vertical < -.02 else "flat"
            bucket = buckets[kind]
            bucket["plans"] += 1
            bucket["vertical_delta"].append(vertical)
            planned_foot = distance_xz((start[0], start[2]), (landing[0], landing[2]))
            planned_root = distance_xz((root_start[0], root_start[2]), (root_landing[0], root_landing[2]))
            actual_actor = distance_xz(actor_start, actor_end)
            bucket["planned_foot_horizontal"].append(planned_foot)
            bucket["planned_root_horizontal"].append(planned_root)
            bucket["actual_actor_horizontal"].append(actual_actor)
            bucket["root_distance_error"].append(planned_root - actual_actor)

            goal_deltas = []
            path_deltas = []
            previous_goal = None
            previous_path = None
            previous_goal_delta = None
            previous_path_delta = None
            for row in values:
                frame = int(float(row["frame_sequence"]))
                goal = f(row[f"{leg}_final_goal_world_y"])
                path = f(row[f"{leg}_current_path_world_y"])
                if goal is not None and previous_goal is not None:
                    delta = goal - previous_goal
                    goal_deltas.append(abs(delta))
                    if previous_goal_delta is not None and delta * previous_goal_delta < 0 and max(abs(delta), abs(previous_goal_delta)) > .05:
                        bucket["goal_reversals_over_5cm"] += 1
                    previous_goal_delta = delta
                if path is not None and previous_path is not None:
                    delta = path - previous_path
                    path_deltas.append(abs(delta))
                    if previous_path_delta is not None and delta * previous_path_delta < 0 and max(abs(delta), abs(previous_path_delta)) > .05:
                        bucket["path_reversals_over_5cm"] += 1
                    previous_path_delta = delta
                previous_goal = goal
                previous_path = path

                phase = f(row[f"{leg}_landing_event_phase"])
                lift_off = f(row[f"{leg}_landing_lift_off_phase"])
                progress = f(row[f"{leg}_landing_ground_path_progress"])
                applied = f(row[f"{leg}_applied_lift"])
                if phase is not None and lift_off is not None and phase + 1e-5 < lift_off:
                    if progress is not None and progress > 1e-4:
                        bucket["pre_liftoff_progress_violations"] += 1
                    if applied is not None and abs(applied) > 1e-4:
                        bucket["pre_liftoff_lift_violations"] += 1

                solver = f(row[f"{leg}_position_residual"])
                physical = f(row[f"{leg}_final_physical_residual_penetration"])
                predictive = f(row[f"{leg}_predictive_residual_penetration"])
                required = f(row[f"{leg}_required_lift"])
                bucket["solver_residual"].append(solver)
                bucket["physical_penetration"].append(physical)
                bucket["predictive_penetration"].append(predictive)
                if required is not None and applied is not None:
                    bucket["required_minus_applied"].append(required - applied)
                score = max(solver or 0, physical or 0, predictive or 0)
                bucket["top_frames"].append({
                    "score": score,
                    "frame": frame,
                    "plan": plan,
                    "phase": phase,
                    "lift_off": lift_off,
                    "progress": progress,
                    "goal_y": goal,
                    "path_y": path,
                    "required_lift": required,
                    "applied_lift": applied,
                    "solver_residual": solver,
                    "physical_penetration": physical,
                    "predictive_penetration": predictive,
                })

            bucket["same_plan_goal_delta_y"].extend(goal_deltas)
            bucket["same_plan_path_delta_y"].extend(path_deltas)
            bucket["examples"].append({
                "plan": plan,
                "frames": [int(float(first["frame_sequence"])), int(float(last["frame_sequence"]))],
                "vertical_delta": vertical,
                "planned_foot_horizontal": planned_foot,
                "planned_root_horizontal": planned_root,
                "actual_actor_horizontal": actual_actor,
                "generation_phase": f(first[f"{leg}_plan_generation_phase"]),
                "lift_off_phase": f(first[f"{leg}_landing_lift_off_phase"]),
            })

        result = {}
        for kind, bucket in buckets.items():
            result[kind] = {
                "plans": bucket["plans"],
                "vertical_delta": q(bucket["vertical_delta"]),
                "planned_foot_horizontal": q(bucket["planned_foot_horizontal"]),
                "planned_root_horizontal": q(bucket["planned_root_horizontal"]),
                "actual_actor_horizontal": q(bucket["actual_actor_horizontal"]),
                "root_distance_error": q(bucket["root_distance_error"]),
                "same_plan_goal_delta_y": q(bucket["same_plan_goal_delta_y"]),
                "same_plan_path_delta_y": q(bucket["same_plan_path_delta_y"]),
                "solver_residual": q(bucket["solver_residual"]),
                "physical_penetration": q(bucket["physical_penetration"]),
                "predictive_penetration": q(bucket["predictive_penetration"]),
                "required_minus_applied": q(bucket["required_minus_applied"]),
                "goal_reversals_over_5cm": bucket["goal_reversals_over_5cm"],
                "path_reversals_over_5cm": bucket["path_reversals_over_5cm"],
                "pre_liftoff_progress_violations": bucket["pre_liftoff_progress_violations"],
                "pre_liftoff_lift_violations": bucket["pre_liftoff_lift_violations"],
                "largest_vertical_examples": sorted(bucket["examples"], key=lambda value: abs(value["vertical_delta"]), reverse=True)[:5],
                "top_frames": sorted(bucket["top_frames"], key=lambda value: value["score"], reverse=True)[:10],
            }
        output[leg] = result

    payload = json.dumps(output, ensure_ascii=False, indent=2)
    if len(sys.argv) > 2:
        with open(sys.argv[2], "w", encoding="utf-8", newline="\n") as stream:
            stream.write(payload)
            stream.write("\n")
    else:
        print(payload)


if __name__ == "__main__":
    main()
