import csv
import gzip
import json
import os
import sys
from glob import glob


run_dir = sys.argv[1]
frames = {int(value) for value in sys.argv[2].split(",")}
rows = []
for path in sorted(glob(os.path.join(run_dir, "*.csv.gz"))):
    with gzip.open(path, "rt", encoding="utf-8-sig", newline="") as stream:
        for row in csv.DictReader(stream):
            frame = int(row["frame_sequence"])
            if frame not in frames:
                continue
            result = {
                "frame": frame,
                "dt": row.get("presentation_delta_seconds"),
                "root_y": row.get("pose_root_world_y"),
                "root_vertical_delta": row.get("pose_root_vertical_delta"),
                "pelvis_current": row.get("pelvis_current"),
                "pelvis_target": row.get("pelvis_resolved_target"),
            }
            for side in ("left", "right"):
                prefix = side + "_"
                result[side] = {
                    "baseline_y": row.get(prefix + "baseline_goal_world_y"),
                    "final_y": row.get(prefix + "final_goal_world_y"),
                    "sole_y": row.get(prefix + "current_sole_world_y"),
                    "fixed_path_y": row.get(prefix + "fixed_path_start_world_y"),
                    "path_y": row.get(prefix + "current_path_world_y"),
                    "path_root_y": row.get(prefix + "current_path_root_world_y"),
                    "path_hip_y": row.get(prefix + "current_path_hip_world_y"),
                    "required_lift": row.get(prefix + "required_lift"),
                    "applied_lift": row.get(prefix + "applied_lift"),
                    "clearance": row.get(prefix + "composite_animation_clearance"),
                    "continuity": row.get(prefix + "animation_clearance_continuity_contribution"),
                    "contact": row.get(prefix + "contact"),
                    "transition": row.get(prefix + "transition"),
                    "decision": row.get(prefix + "contact_decision"),
                    "target_distance": row.get(prefix + "contact_target_distance"),
                    "target_distance_accepted": row.get(prefix + "contact_target_distance_accepted"),
                    "anchor": row.get(prefix + "has_anchor"),
                    "anchor_blend": row.get(prefix + "anchor_blend"),
                    "plan": row.get(prefix + "predictive_plan_sequence"),
                    "revision_plan": row.get(prefix + "revision_plan_sequence"),
                    "has_revision": row.get(prefix + "has_plan_revision"),
                    "revision_blend": row.get(prefix + "plan_revision_blend_weight"),
                    "state": row.get(prefix + "predictive_plan_state"),
                    "plan_transition": row.get(prefix + "predictive_plan_transition"),
                    "plan_end_reason": row.get(prefix + "predictive_plan_end_reason"),
                    "prediction_reject": row.get(prefix + "prediction_reject"),
                    "ground_envelope_reject": row.get(prefix + "ground_envelope_reject"),
                    "query_count": row.get(prefix + "predictive_query_count"),
                    "rejected_query_count": row.get(prefix + "predictive_rejected_query_count"),
                    "reject_counts": {
                        "no_candidate": row.get(prefix + "predictive_reject_no_candidate_count"),
                        "height": row.get(prefix + "predictive_reject_height_discontinuity_count"),
                        "edge_gap": row.get(prefix + "predictive_reject_edge_gap_count"),
                        "surface": row.get(prefix + "predictive_reject_surface_discontinuity_count"),
                        "reach": row.get(prefix + "predictive_reject_reach_exceeded_count"),
                        "slope": row.get(prefix + "predictive_reject_slope_exceeded_count"),
                        "step": row.get(prefix + "predictive_reject_step_exceeded_count"),
                        "invalid": row.get(prefix + "predictive_reject_invalid_candidate_count"),
                        "center": row.get(prefix + "predictive_reject_unsupported_center_count"),
                    },
                    "current_pre_swing": row.get(prefix + "current_event_is_pre_swing"),
                    "current_swing": row.get(prefix + "current_event_is_swing"),
                    "event_identity": row.get(prefix + "landing_event_identity"),
                    "source_identity": row.get(prefix + "source_sample_identity"),
                    "incoming_valid": row.get(prefix + "incoming_predicted_step_valid"),
                    "incoming_event_identity": row.get(prefix + "incoming_landing_event_identity"),
                    "incoming_phase": row.get(prefix + "incoming_event_phase"),
                    "incoming_liftoff": row.get(prefix + "incoming_lift_off_phase"),
                    "plan_event_identity": row.get(prefix + "plan_landing_event_identity"),
                    "plan_source_identity": row.get(prefix + "plan_source_sample_identity"),
                    "phase": row.get(prefix + "landing_event_phase"),
                    "liftoff": row.get(prefix + "landing_lift_off_phase"),
                    "progress": row.get(prefix + "predictive_execution_progress"),
                    "action_progress": row.get(prefix + "landing_action_progress"),
                    "prediction_blend": row.get(prefix + "committed_prediction_blend"),
                    "plan_prediction_blend": row.get(prefix + "plan_prediction_blend"),
                    "pose_prediction_blend": row.get(prefix + "pose_synchronized_prediction_blend"),
                    "reach_ratio": row.get(prefix + "prediction_reach_ratio"),
                    "animated_ankle_y": row.get(prefix + "animated_ankle_component_y"),
                    "swing_duration": row.get(prefix + "plan_swing_duration"),
                    "solver_residual": row.get(prefix + "position_residual"),
                }
            rows.append(result)
rows.sort(key=lambda row: row["frame"])
print(json.dumps(rows, ensure_ascii=False, indent=2))
