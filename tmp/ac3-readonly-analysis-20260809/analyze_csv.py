from pathlib import Path
import json
import pandas as pd

CSV_PATH = Path(r"D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/Scenes/Standalone/foot-ik-ac3bfc2ad2944ea68ee48c269cdd3664.csv")

df = pd.read_csv(CSV_PATH, low_memory=False)


def boolean(series):
    if series.dtype == bool:
        return series.fillna(False)
    return series.fillna(False).astype(str).str.lower().isin(("true", "1"))


def numeric(column):
    return pd.to_numeric(df[column], errors="coerce").fillna(0.0)


def vector_distance(frame, prefix_a, prefix_b, dimensions=("x", "z")):
    total = 0.0
    for dimension in dimensions:
        a = pd.to_numeric(frame[f"{prefix_a}_{dimension}"], errors="coerce").fillna(0.0)
        b = pd.to_numeric(frame[f"{prefix_b}_{dimension}"], errors="coerce").fillna(0.0)
        total = total + (a - b) ** 2
    return total ** 0.5


def row_distance(row, prefix_a, prefix_b, dimensions=("x", "z")):
    total = 0.0
    for dimension in dimensions:
        a = float(row[f"{prefix_a}_{dimension}"])
        b = float(row[f"{prefix_b}_{dimension}"])
        total += (a - b) ** 2
    return total ** 0.5


def count_values(column):
    return {
        str(key): int(value)
        for key, value in df[column].fillna("<null>").astype(str).value_counts().items()
    }


result = {
    "shape": list(df.shape),
    "frame_range": [int(numeric("frame_sequence").min()), int(numeric("frame_sequence").max())],
    "completion_identity_match_rows": int((
        numeric("grounding_completion").eq(numeric("modifier_completion")) &
        numeric("modifier_completion").eq(numeric("solver_completion"))
    ).sum()),
    "has_modifier_rows": int(boolean(df["has_modifier"]).sum()),
    "node_executed_rows": int(boolean(df["node_executed"]).sum()),
    "solver_failure_counts": count_values("solver_failure"),
    "sides": {},
}


for side in ("left", "right"):
    plan_sequence = numeric(f"{side}_predictive_plan_sequence").astype(int)
    state = df[f"{side}_predictive_plan_state"].fillna("").astype(str)
    transition = df[f"{side}_predictive_plan_transition"].fillna("").astype(str)
    end_reason = df[f"{side}_predictive_plan_end_reason"].fillna("").astype(str)
    required = numeric(f"{side}_required_lift")
    applied = numeric(f"{side}_applied_lift")
    baseline_penetration = numeric(f"{side}_residual_sole_penetration")
    predictive_penetration = numeric(f"{side}_predictive_residual_penetration")
    y_delta = numeric(f"{side}_final_goal_world_y") - numeric(f"{side}_baseline_goal_world_y")
    post_min_distance = pd.concat((
        numeric(f"{side}_post_clearance_heel_path_distance"),
        numeric(f"{side}_post_clearance_toe_path_distance"),
    ), axis=1).min(axis=1)
    authoritative = boolean(df[f"{side}_has_authoritative_landing_event"])
    identity_valid = boolean(df[f"{side}_landing_event_identity_valid"])
    query_count = numeric(f"{side}_predictive_query_count")
    accepted_hits = numeric(f"{side}_prediction_accepted_hit_count")
    edge_candidates = numeric(f"{side}_prediction_edge_plane_candidate_count")
    edge_accepted = numeric(f"{side}_prediction_accepted_edge_plane_count")
    executable = boolean(df[f"{side}_plan_has_executable_path"])
    visible_plan = state.isin(("Planned", "Executing"))
    penetrating = baseline_penetration > 0.0001
    predictive_penetrating = predictive_penetration > 0.0001
    applied_positive = applied > 0.0001
    required_positive = required > 0.0001
    executing = state.eq("Executing")

    plan_rows = []
    for sequence in sorted(plan_sequence[plan_sequence > 0].unique()):
        group = df.loc[plan_sequence.eq(sequence)].copy()
        group_state = group[f"{side}_predictive_plan_state"].fillna("").astype(str)
        group_transition = group[f"{side}_predictive_plan_transition"].fillna("").astype(str)
        group_end = group[f"{side}_predictive_plan_end_reason"].fillna("").astype(str)
        physical = boolean(group[f"{side}_plan_physical_swing_observed"])
        executing_group = group_state.eq("Executing")
        start_candidates = group.loc[physical | executing_group]
        start = start_candidates.iloc[0] if not start_candidates.empty else group.iloc[0]
        contact_candidates = group.loc[
            group_transition.eq("ContactReached") | group_end.eq("ContactReached")
        ]
        end = contact_candidates.iloc[-1] if not contact_candidates.empty else group.iloc[-1]
        route_length = row_distance(
            start,
            f"{side}_planned_foot_route_world_0",
            f"{side}_planned_foot_route_world_6",
        )
        actual_displacement = row_distance(
            pd.Series({
                f"{side}_actual_start_x": start[f"{side}_baseline_goal_world_x"],
                f"{side}_actual_start_z": start[f"{side}_baseline_goal_world_z"],
                f"{side}_actual_end_x": end[f"{side}_baseline_goal_world_x"],
                f"{side}_actual_end_z": end[f"{side}_baseline_goal_world_z"],
            }),
            f"{side}_actual_start",
            f"{side}_actual_end",
        )
        landing_error = row_distance(
            pd.Series({
                f"{side}_fixed_x": start[f"{side}_fixed_landing_world_x"],
                f"{side}_fixed_z": start[f"{side}_fixed_landing_world_z"],
                f"{side}_actual_x": end[f"{side}_baseline_goal_world_x"],
                f"{side}_actual_z": end[f"{side}_baseline_goal_world_z"],
            }),
            f"{side}_fixed",
            f"{side}_actual",
        )
        speed = (
            float(start[f"{side}_frozen_animation_forward_velocity_x"]) ** 2 +
            float(start[f"{side}_frozen_animation_forward_velocity_y"]) ** 2 +
            float(start[f"{side}_frozen_animation_forward_velocity_z"]) ** 2
        ) ** 0.5
        plan_rows.append({
            "sequence": int(sequence),
            "generated_frame": int(float(start[f"{side}_predictive_plan_generated_frame"])),
            "first_frame": int(float(group["frame_sequence"].iloc[0])),
            "last_frame": int(float(group["frame_sequence"].iloc[-1])),
            "states": sorted(set(group_state)),
            "transitions": sorted(set(group_transition)),
            "end_reasons": sorted(set(group_end)),
            "physical_swing": bool(physical.any()),
            "contact_reached": bool(not contact_candidates.empty),
            "route_xz_length": round(route_length, 6),
            "actual_swing_xz_displacement": round(actual_displacement, 6),
            "route_to_actual_ratio": round(route_length / actual_displacement, 4) if actual_displacement > 0.01 else None,
            "fixed_landing_xz_error": round(landing_error, 6),
            "animation_forward_speed": round(speed, 6),
            "swing_duration": round(float(start[f"{side}_plan_swing_duration"]), 6),
            "query_count": int(float(start[f"{side}_predictive_query_count"])),
            "accepted_hits": int(float(start[f"{side}_prediction_accepted_hit_count"])),
            "edge_candidates": int(float(start[f"{side}_prediction_edge_plane_candidate_count"])),
            "edge_accepted": int(float(start[f"{side}_prediction_accepted_edge_plane_count"])),
            "has_executable_path": bool(boolean(group[f"{side}_plan_has_executable_path"]).any()),
            "max_projection_distance": round(float(pd.to_numeric(group[f"{side}_predictive_foot_route_projection_distance"], errors="coerce").fillna(0.0).max()), 6),
            "max_required_lift": round(float(pd.to_numeric(group[f"{side}_required_lift"], errors="coerce").fillna(0.0).max()), 6),
            "max_applied_lift": round(float(pd.to_numeric(group[f"{side}_applied_lift"], errors="coerce").fillna(0.0).max()), 6),
            "max_baseline_penetration": round(float(pd.to_numeric(group[f"{side}_residual_sole_penetration"], errors="coerce").fillna(0.0).max()), 6),
            "max_predictive_penetration": round(float(pd.to_numeric(group[f"{side}_predictive_residual_penetration"], errors="coerce").fillna(0.0).max()), 6),
        })

    valid_ratios = [item["route_to_actual_ratio"] for item in plan_rows if item["route_to_actual_ratio"] is not None]
    landing_errors = [item["fixed_landing_xz_error"] for item in plan_rows if item["physical_swing"]]
    side_result = {
        "authoritative_event_rows": int(authoritative.sum()),
        "identity_valid_authoritative_rows": int((authoritative & identity_valid).sum()),
        "state_counts": count_values(f"{side}_predictive_plan_state"),
        "transition_counts": count_values(f"{side}_predictive_plan_transition"),
        "end_reason_counts": count_values(f"{side}_predictive_plan_end_reason"),
        "reject_counts": count_values(f"{side}_prediction_reject"),
        "unique_plan_count": len(plan_rows),
        "visible_gizmo_rows": int(visible_plan.sum()),
        "query_rows": int((query_count > 0).sum()),
        "accepted_hit_rows": int((accepted_hits > 0).sum()),
        "edge_candidate_rows": int((edge_candidates > 0).sum()),
        "edge_accepted_rows": int((edge_accepted > 0).sum()),
        "executable_path_rows": int(executable.sum()),
        "required_lift_rows": int(required_positive.sum()),
        "applied_lift_rows": int(applied_positive.sum()),
        "maximum_required_lift": round(float(required.max()), 6),
        "maximum_applied_lift": round(float(applied.max()), 6),
        "maximum_goal_y_delta": round(float(y_delta.max()), 6),
        "goal_y_delta_matches_applied_max_error": round(float((y_delta - applied).abs().max()), 9),
        "baseline_penetration_rows": int(penetrating.sum()),
        "maximum_baseline_penetration": round(float(baseline_penetration.max()), 6),
        "predictive_penetration_rows": int(predictive_penetrating.sum()),
        "maximum_predictive_penetration": round(float(predictive_penetration.max()), 6),
        "negative_post_path_distance_rows": int((post_min_distance < -0.0001).sum()),
        "penetration_breakdown": {
            "baseline_penetration_not_executing": int((penetrating & ~executing).sum()),
            "baseline_penetration_executing": int((penetrating & executing).sum()),
            "baseline_penetration_required_zero": int((penetrating & ~required_positive).sum()),
            "baseline_penetration_required_positive_applied_zero": int((penetrating & required_positive & ~applied_positive).sum()),
            "baseline_penetration_applied_positive": int((penetrating & applied_positive).sum()),
            "predictive_penetration_with_applied_positive": int((predictive_penetrating & applied_positive).sum()),
        },
        "maximum_solver_position_residual": round(float(numeric(f"{side}_position_residual").max()), 9),
        "median_route_to_actual_ratio": round(float(pd.Series(valid_ratios).median()), 4) if valid_ratios else None,
        "maximum_route_to_actual_ratio": round(max(valid_ratios), 4) if valid_ratios else None,
        "median_fixed_landing_error": round(float(pd.Series(landing_errors).median()), 6) if landing_errors else None,
        "maximum_fixed_landing_error": round(max(landing_errors), 6) if landing_errors else None,
        "plans": plan_rows,
    }
    result["sides"][side] = side_result


print(json.dumps(result, ensure_ascii=False, indent=2))

sample_output = {}
for side in ("left", "right"):
    sequence_column = f"{side}_predictive_plan_sequence"
    sequences = numeric(sequence_column).astype(int)
    if not (sequences > 0).any():
        continue
    sequence = int(sequences[sequences > 0].iloc[0])
    group = df.loc[sequences.eq(sequence)]
    interesting = group.loc[
        group[f"{side}_predictive_plan_transition"].fillna("").astype(str).ne("")
    ]
    indexes = list(interesting.index)
    indexes.extend((group.index[0], group.index[-1]))
    indexes = sorted(set(indexes))
    columns = [
        "frame_sequence", "presentation_delta_seconds",
        f"{side}_contact_state", f"{side}_plant_contact", f"{side}_has_anchor",
        f"{side}_predictive_plan_state", f"{side}_predictive_plan_transition",
        f"{side}_plan_physical_swing_observed", f"{side}_prediction_reject",
        f"{side}_predictive_clock_progress", f"{side}_plan_elapsed_seconds",
        f"{side}_plan_seconds_to_lift_off", f"{side}_plan_swing_duration",
        f"{side}_frozen_animation_forward_velocity_x",
        f"{side}_frozen_animation_forward_velocity_y",
        f"{side}_frozen_animation_forward_velocity_z",
        f"{side}_frozen_root_start_world_x", f"{side}_frozen_root_start_world_y", f"{side}_frozen_root_start_world_z",
        f"{side}_frozen_root_landing_world_x", f"{side}_frozen_root_landing_world_y", f"{side}_frozen_root_landing_world_z",
        f"{side}_fixed_path_start_world_x", f"{side}_fixed_path_start_world_y", f"{side}_fixed_path_start_world_z",
        f"{side}_fixed_landing_world_x", f"{side}_fixed_landing_world_y", f"{side}_fixed_landing_world_z",
        f"{side}_baseline_goal_world_x", f"{side}_baseline_goal_world_y", f"{side}_baseline_goal_world_z",
        f"{side}_current_path_world_x", f"{side}_current_path_world_y", f"{side}_current_path_world_z",
        f"{side}_future_point_x", f"{side}_future_point_y", f"{side}_future_point_z",
        f"{side}_future_landing_query_origin_x", f"{side}_future_landing_query_origin_y", f"{side}_future_landing_query_origin_z",
        f"{side}_future_landing_query_direction_x", f"{side}_future_landing_query_direction_y", f"{side}_future_landing_query_direction_z",
        f"{side}_prediction_route_sample_count", f"{side}_prediction_accepted_hit_count",
        f"{side}_prediction_edge_plane_candidate_count", f"{side}_prediction_accepted_edge_plane_count",
    ]
    columns.extend(
        f"{side}_planned_foot_route_world_{sample}_{axis}"
        for sample in range(7)
        for axis in ("x", "y", "z")
    )
    columns.extend(
        f"{side}_path_{sample}_{field}"
        for sample in range(8)
        for field in ("fraction", "position_x", "position_y", "position_z", "surface")
    )
    columns = [column for column in columns if column in df.columns]
    sample_output[side] = group.loc[indexes, columns].where(pd.notna(group.loc[indexes, columns]), None).to_dict("records")

print(json.dumps({"samples": sample_output}, ensure_ascii=False, indent=2))
