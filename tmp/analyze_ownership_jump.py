import csv
import gzip
import json
import math
import os
import sys
from collections import Counter
from glob import glob


def number(value):
    try:
        result = float(value)
        return result if math.isfinite(result) else None
    except (TypeError, ValueError):
        return None


def load(run_dir):
    rows = []
    header = None
    width_errors = 0
    for path in sorted(glob(os.path.join(run_dir, "*.csv.gz"))):
        with gzip.open(path, "rt", encoding="utf-8-sig", newline="") as stream:
            reader = csv.reader(stream)
            current_header = next(reader)
            if header is None:
                header = current_header
            elif current_header != header:
                raise RuntimeError("Header mismatch")
            for values in reader:
                if len(values) != len(header):
                    width_errors += 1
                    continue
                rows.append(dict(zip(header, values)))
    rows.sort(key=lambda row: int(number(row["frame_sequence"]) or 0))
    return rows, len(header or ()), width_errors


def percentile(values, rate):
    if not values:
        return 0.0
    values = sorted(values)
    position = (len(values) - 1) * rate
    lower = math.floor(position)
    upper = math.ceil(position)
    if lower == upper:
        return values[lower]
    return values[lower] * (upper - position) + values[upper] * (position - lower)


def summarize(rows, side):
    prefix = side + "_"
    starts = []
    ownership_round_trips = []
    for index, row in enumerate(rows):
        if row.get(prefix + "predictive_plan_transition") != "PlanExecutionStarted":
            continue
        lift = number(row.get(prefix + "required_lift")) or 0.0
        start_path = number(row.get(prefix + "current_path_world_y"))
        sequence = row.get(prefix + "predictive_plan_sequence")
        item = {
            "frame": int(number(row["frame_sequence"]) or 0),
            "sequence": sequence,
            "lift_cm": round(lift * 100.0, 4),
            "path_y": start_path,
            "blend": number(row.get(prefix + "committed_prediction_blend")),
            "anchor": row.get(prefix + "has_anchor"),
        }
        starts.append(item)
        if index == 0 or index + 1 >= len(rows):
            continue
        previous = rows[index - 1]
        following = rows[index + 1]
        if following.get(prefix + "predictive_plan_sequence") != sequence:
            continue
        before_path = number(previous.get(prefix + "current_path_world_y"))
        after_path = number(following.get(prefix + "current_path_world_y"))
        if before_path is None or start_path is None or after_path is None:
            continue
        away = abs(start_path - before_path)
        back = abs(after_path - before_path)
        if away > 0.04 and back < away * 0.5:
            ownership_round_trips.append({
                "frame": item["frame"],
                "sequence": sequence,
                "before_y": before_path,
                "start_y": start_path,
                "after_y": after_path,
                "away_cm": round(away * 100.0, 4),
                "back_cm": round(back * 100.0, 4),
            })
    lifts = [item["lift_cm"] for item in starts]
    penetrations = [number(row.get(prefix + "final_physical_residual_penetration")) or 0.0 for row in rows]
    residuals = [number(row.get(prefix + "position_residual")) or 0.0 for row in rows]
    correction_y = []
    final_local_y = []
    correction_rows = []
    final_local_rows = []
    for row in rows:
        baseline = number(row.get(prefix + "baseline_goal_world_y"))
        final = number(row.get(prefix + "final_goal_world_y"))
        root = number(row.get("pose_root_world_y"))
        if baseline is not None and final is not None:
            correction_y.append(final - baseline)
            correction_rows.append(row)
        if final is not None and root is not None:
            final_local_y.append(final - root)
            final_local_rows.append(row)
    correction_deltas = [abs(correction_y[index] - correction_y[index - 1]) for index in range(1, len(correction_y))]
    final_local_deltas = [abs(final_local_y[index] - final_local_y[index - 1]) for index in range(1, len(final_local_y))]
    correction_round_trips = []
    for index in range(1, len(correction_y) - 1):
        first = correction_y[index] - correction_y[index - 1]
        second = correction_y[index + 1] - correction_y[index]
        if first * second < 0.0 and min(abs(first), abs(second)) > 0.04:
            row = correction_rows[index]
            correction_round_trips.append({
                "frame": int(number(row["frame_sequence"]) or 0),
                "first_cm": round(first * 100.0, 4),
                "second_cm": round(second * 100.0, 4),
                "plan": row.get(prefix + "predictive_plan_sequence"),
                "state": row.get(prefix + "predictive_plan_state"),
                "plan_transition": row.get(prefix + "predictive_plan_transition"),
                "contact": row.get(prefix + "contact"),
                "transition": row.get(prefix + "transition"),
            })
    correction_delta_items = []
    for index, value in enumerate(correction_deltas, 1):
        row = correction_rows[index]
        correction_delta_items.append({
            "frame": int(number(row["frame_sequence"]) or 0),
            "delta_cm": round(value * 100.0, 4),
            "correction_cm": round(correction_y[index] * 100.0, 4),
            "path_y": number(row.get(prefix + "current_path_world_y")),
            "plan": row.get(prefix + "predictive_plan_sequence"),
            "state": row.get(prefix + "predictive_plan_state"),
            "plan_transition": row.get(prefix + "predictive_plan_transition"),
            "contact": row.get(prefix + "contact"),
            "transition": row.get(prefix + "transition"),
        })
    distance_releases = [
        int(number(row["frame_sequence"]) or 0)
        for row in rows
        if row.get(prefix + "contact_decision") == "AnchorDistanceExceeded"
    ]
    return {
        "execution_starts": len(starts),
        "start_lift_mean_cm": round(sum(lifts) / len(lifts), 4) if lifts else 0.0,
        "start_lift_max_cm": max(lifts, default=0.0),
        "start_lifts_over_8cm": sum(value > 8.0 for value in lifts),
        "ownership_path_round_trips_over_4cm": ownership_round_trips,
        "anchor_distance_release_frames": distance_releases,
        "plan_end_reasons": dict(Counter(row.get(prefix + "predictive_plan_end_reason") for row in rows)),
        "contact_transitions": dict(Counter(row.get(prefix + "transition") for row in rows)),
        "correction_delta_p95_cm": round(percentile(correction_deltas, 0.95) * 100.0, 4),
        "correction_delta_max_cm": round(max(correction_deltas, default=0.0) * 100.0, 4),
        "correction_round_trips_over_4cm": len(correction_round_trips),
        "top_correction_round_trips": correction_round_trips[:12],
        "top_correction_deltas": sorted(correction_delta_items, key=lambda item: item["delta_cm"], reverse=True)[:12],
        "final_local_y_delta_p95_cm": round(percentile(final_local_deltas, 0.95) * 100.0, 4),
        "final_local_y_delta_max_cm": round(max(final_local_deltas, default=0.0) * 100.0, 4),
        "penetration_max_cm": round(max(penetrations, default=0.0) * 100.0, 5),
        "solver_residual_p95_cm": round(percentile(residuals, 0.95) * 100.0, 5),
        "solver_residual_max_cm": round(max(residuals, default=0.0) * 100.0, 5),
        "solver_residual_max_frame": int(number(rows[residuals.index(max(residuals))]["frame_sequence"]) or 0) if residuals else 0,
        "top_start_lifts": sorted(starts, key=lambda item: item["lift_cm"], reverse=True)[:12],
    }


def main():
    result = []
    for run_dir in sys.argv[1:]:
        rows, columns, width_errors = load(run_dir)
        result.append({
            "run": os.path.basename(run_dir.rstrip("/\\")),
            "rows": len(rows),
            "columns": columns,
            "width_errors": width_errors,
            "left": summarize(rows, "left"),
            "right": summarize(rows, "right"),
        })
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
