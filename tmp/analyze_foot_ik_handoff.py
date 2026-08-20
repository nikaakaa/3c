import csv
import gzip
import json
import math
import os
import re
import sys
from glob import glob


def value(row, name):
    try:
        result = float(row[name])
        return result if math.isfinite(result) else None
    except (KeyError, TypeError, ValueError):
        return None


def frame(row, side):
    prefix = side + "_"
    names = (
        "animation_foot_speed",
        "plant_speed_threshold",
        "contact_target_distance",
        "contact_target_distance_accepted",
        "contact",
        "transition",
        "contact_decision",
        "current_event_is_pre_swing",
        "current_event_is_swing",
        "has_anchor",
        "anchor_blend",
        "anchor_distance",
        "anchor_distance_accepted",
        "predictive_plan_sequence",
        "predictive_plan_state",
        "predictive_plan_transition",
        "predictive_plan_end_reason",
        "landing_event_phase",
        "landing_lift_off_phase",
        "committed_prediction_blend",
        "baseline_goal_world_y",
        "final_goal_world_y",
        "required_lift",
        "applied_lift",
        "position_residual",
    )
    return {
        "frame": int(value(row, "frame_sequence") or 0),
        "route": row["_route"],
        "pelvis_constraint_mode": row.get(prefix + "pelvis_constraint_mode"),
        "pelvis_support_phase": row.get(prefix + "pelvis_support_phase"),
        **{
            name: value(row, prefix + name)
            if name not in (
                "contact_target_distance_accepted",
                "contact",
                "transition",
                "contact_decision",
                "current_event_is_pre_swing",
                "current_event_is_swing",
                "has_anchor",
                "anchor_distance_accepted",
                "predictive_plan_state",
                "predictive_plan_transition",
                "predictive_plan_end_reason",
            )
            else row.get(prefix + name)
            for name in names
        },
    }


def main():
    rows = []
    for path in sorted(glob(os.path.join(sys.argv[1], "*.csv.gz"))):
        match = re.match(r"chunk-\d+-(.+)-lap-\d+-frames-\d+-\d+\.csv\.gz", os.path.basename(path))
        route = match.group(1) if match else "unknown"
        with gzip.open(path, "rt", encoding="utf-8-sig", newline="") as stream:
            for row in csv.DictReader(stream):
                row["_route"] = route
                rows.append(row)
    rows.sort(key=lambda row: int(value(row, "frame_sequence") or 0))
    result = {}
    for side in ("left", "right"):
        prefix = side + "_"
        captures = [row for row in rows if row[prefix + "transition"] == "AnchorCaptured"]
        high = [
            row for row in captures
            if (value(row, prefix + "animation_foot_speed") or 0.0) >
               (value(row, prefix + "plant_speed_threshold") or 0.0)
        ]
        distance_releases = [
            row for row in rows
            if row[prefix + "transition"] == "AnchorDistanceExceeded"
        ]
        execution_starts = [
            row for row in rows
            if row[prefix + "predictive_plan_transition"] == "PlanExecutionStarted"
        ]
        result[side] = {
            "high_speed_captures": [frame(row, side) for row in high],
            "anchor_distance_releases": [frame(row, side) for row in distance_releases],
            "execution_starts": [frame(row, side) for row in execution_starts],
        }
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
