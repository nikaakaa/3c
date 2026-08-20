import csv
import gzip
import pathlib
import sys


run = pathlib.Path(sys.argv[1])
frames = {int(value) for value in sys.argv[2].split(",")}
rows = []
for path in sorted(run.glob("chunk-*.csv.gz")):
    with gzip.open(path, "rt", encoding="utf-8-sig", newline="") as stream:
        for row in csv.DictReader(stream):
            if int(row["frame_sequence"]) in frames:
                rows.append(row)


def value(row, key):
    raw = row.get(key, "")
    try:
        return round(float(raw), 6)
    except ValueError:
        return raw


for row in sorted(rows, key=lambda item: int(item["frame_sequence"])):
    pelvis_keys = [
        "pelvis_current",
        "pelvis_resolved_target",
        "pelvis_support_side",
        "pelvis_support_plan_sequence",
        "pelvis_selected_support_target",
        "left_pelvis_displacement",
        "right_pelvis_displacement",
    ]
    pelvis = " ".join(f"{key}={value(row, key)}" for key in pelvis_keys)
    print(f"frame={row['frame_sequence']} rootY={row['pose_root_world_y']} {pelvis}")
    for side in ("left", "right"):
        keys = [
            "landing_event_identity",
            "landing_event_phase",
            "current_event_is_pre_swing",
            "current_event_is_swing",
            "current_event_foot_pose_weight",
            "contribution_continuity_identity",
            "predictive_plan_sequence",
            "plan_landing_event_identity",
            "plan_contribution_continuity_identity",
            "revision_plan_sequence",
            "has_plan_revision",
            "plan_revision_blend_weight",
            "plan_revision_smoothed_blend_weight",
            "plan_prediction_blend",
            "pose_synchronized_prediction_blend",
            "predictive_execution_progress",
            "landing_ground_path_progress",
            "motion_landing_error",
            "motion_landing_tolerance",
            "current_path_world_y",
            "current_path_hip_world_y",
            "predicted_hip_world_y",
            "authored_animation_clearance",
            "animation_clearance_continuity_contribution",
            "composite_animation_clearance",
            "baseline_goal_world_y",
            "final_goal_world_y",
            "contact",
            "transition",
            "has_anchor",
            "anchor_blend",
            "prediction_reach_ratio",
            "position_residual",
        ]
        fields = " ".join(f"{key}={value(row, side + '_' + key)}" for key in keys)
        print(f"  {side}: {fields}")
