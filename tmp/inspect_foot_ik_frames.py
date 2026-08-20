import csv
import gzip
import json
import math
import pathlib
import sys


def number(value):
    try:
        parsed = float(value)
        return parsed if math.isfinite(parsed) else value
    except (TypeError, ValueError):
        return value


run_dir = pathlib.Path(sys.argv[1])
side = sys.argv[2]
targets = {int(value) for value in sys.argv[3].split(",")}
wanted = targets | {frame - 1 for frame in targets} | {frame + 1 for frame in targets}
rows = {}
for path in sorted(run_dir.glob("chunk-*.csv.gz")):
    if ".partial." in path.name or path.stat().st_size == 0:
        continue
    with gzip.open(path, "rt", encoding="utf-8", newline="") as stream:
        reader = csv.DictReader(stream)
        for row in reader:
            frame = int(row["frame_sequence"])
            if frame in wanted:
                rows[frame] = row

keys = [
    "frame_sequence",
    "pose_root_world_x",
    "pose_root_world_y",
    "pose_root_world_z",
    "pose_root_vertical_delta",
    "pelvis_lyra_target",
    "pelvis_resolved_target",
    "pelvis_current",
    "pelvis_support_side",
    "pelvis_support_plan_sequence",
    f"{side}_plan_invariants_route_direction",
    f"{side}_plan_invariants_route_lap",
    f"{side}_plan_invariants_simulation_tick",
    f"{side}_landing_event_identity",
    f"{side}_landing_event_phase",
    f"{side}_landing_lift_off_phase",
    f"{side}_current_event_is_pre_swing",
    f"{side}_current_event_is_swing",
    f"{side}_incoming_predicted_step_valid",
    f"{side}_incoming_landing_event_identity",
    f"{side}_incoming_event_phase",
    f"{side}_incoming_lift_off_phase",
    f"{side}_predictive_plan_sequence",
    f"{side}_predictive_plan_generated_frame",
    f"{side}_predictive_plan_state",
    f"{side}_predictive_plan_transition",
    f"{side}_predictive_plan_end_reason",
    f"{side}_landing_action_progress",
    f"{side}_landing_ground_path_progress",
    f"{side}_plan_prediction_blend",
    f"{side}_committed_prediction_blend",
    f"{side}_motion_linear_landing_error",
    f"{side}_motion_angular_landing_error",
    f"{side}_motion_landing_error",
    f"{side}_motion_landing_tolerance",
    f"{side}_current_path_root_world_x",
    f"{side}_current_path_root_world_y",
    f"{side}_current_path_root_world_z",
    f"{side}_final_source",
    f"{side}_contact",
    f"{side}_transition",
    f"{side}_contact_decision",
    f"{side}_has_anchor",
    f"{side}_anchor_blend",
    f"{side}_surface",
    f"{side}_hit_location_y",
    f"{side}_sole_support_surface",
    f"{side}_current_path_surface",
    f"{side}_current_path_world_y",
    f"{side}_current_path_support_y",
    f"{side}_current_sole_world_x",
    f"{side}_current_sole_world_y",
    f"{side}_current_sole_world_z",
    f"{side}_fixed_landing_world_x",
    f"{side}_fixed_landing_world_y",
    f"{side}_fixed_landing_world_z",
    f"{side}_anchor_world_y",
    f"{side}_authored_animation_clearance",
    f"{side}_animation_clearance_continuity_contribution",
    f"{side}_composite_animation_clearance",
    f"{side}_target_offset",
    f"{side}_offset_target",
    f"{side}_sole_constraint_offset",
    f"{side}_current_offset",
    f"{side}_required_lift",
    f"{side}_applied_lift",
    f"{side}_baseline_goal_world_y",
    f"{side}_baseline_goal_world_x",
    f"{side}_baseline_goal_world_z",
    f"{side}_final_goal_world_y",
    f"{side}_final_goal_world_x",
    f"{side}_final_goal_world_z",
    f"{side}_animated_ankle_component_y",
    f"{side}_pre_clearance_heel_path_distance",
    f"{side}_pre_clearance_toe_path_distance",
    f"{side}_post_clearance_heel_path_distance",
    f"{side}_post_clearance_toe_path_distance",
    f"{side}_position_residual",
]

output = []
for frame in sorted(wanted):
    row = rows.get(frame)
    if row is None:
        continue
    item = {key: number(row.get(key, "")) for key in keys}
    previous = rows.get(frame - 1)
    if previous is not None:
        for key in (
            f"{side}_final_goal_world_y",
            f"{side}_baseline_goal_world_y",
            f"{side}_current_path_world_y",
            f"{side}_current_offset",
            "pelvis_current",
        ):
            current_value = number(row.get(key, ""))
            previous_value = number(previous.get(key, ""))
            item[f"delta_{key}_cm"] = round((current_value - previous_value) * 100, 5) if isinstance(current_value, float) and isinstance(previous_value, float) else None
    output.append(item)

print(json.dumps(output, ensure_ascii=False, indent=2))
