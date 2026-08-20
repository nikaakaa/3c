import csv
import gzip
import json
import math
import pathlib
import statistics
import sys


def number(row, key):
    try:
        value = float(row.get(key, ""))
        return value if math.isfinite(value) else None
    except (TypeError, ValueError):
        return None


def load(run_dir):
    rows = []
    header = None
    widths = set()
    for path in sorted(pathlib.Path(run_dir).glob("chunk-*.csv.gz")):
        with gzip.open(path, "rt", encoding="utf-8-sig", newline="") as stream:
            reader = csv.reader(stream)
            current_header = next(reader)
            header = current_header if header is None else header
            if current_header != header:
                raise RuntimeError(f"header mismatch: {path}")
            for values in reader:
                widths.add(len(values))
                if len(values) == len(header):
                    rows.append(dict(zip(header, values)))
    rows.sort(key=lambda row: int(row["frame_sequence"]))
    return header, widths, rows


def percentile(values, ratio):
    values = sorted(values)
    if not values:
        return None
    return values[min(len(values) - 1, int(math.ceil(len(values) * ratio)) - 1)]


def delta_values(rows, key):
    result = []
    for previous, current in zip(rows, rows[1:]):
        a = number(previous, key)
        b = number(current, key)
        if a is not None and b is not None:
            result.append(abs(b - a))
    return result


def summarize(run_dir):
    header, widths, rows = load(run_dir)
    result = {
        "run": pathlib.Path(run_dir).name,
        "rows": len(rows),
        "columns": len(header),
        "rowWidths": sorted(widths),
    }
    pelvis_deltas = delta_values(rows, "pelvis_current")
    result["pelvis"] = {
        "deltaMaxCm": max(pelvis_deltas, default=0.0) * 100.0,
        "deltaP95Cm": percentile(pelvis_deltas, 0.95) * 100.0 if pelvis_deltas else 0.0,
        "supportSwitchRows": sum(row.get("pelvis_support_switched", "").lower() == "true" for row in rows),
        "supportRows": sum(bool(row.get("pelvis_support_side", "")) for row in rows),
    }
    result["sides"] = {}
    for side in ("left", "right"):
        goal_deltas = delta_values(rows, f"{side}_final_goal_world_y")
        path_deltas = delta_values(rows, f"{side}_current_path_world_y")
        correction_deltas = []
        for previous, current in zip(rows, rows[1:]):
            previous_final = number(previous, f"{side}_final_goal_world_y")
            current_final = number(current, f"{side}_final_goal_world_y")
            previous_baseline = number(previous, f"{side}_baseline_goal_world_y")
            current_baseline = number(current, f"{side}_baseline_goal_world_y")
            if None not in (previous_final, current_final, previous_baseline, current_baseline):
                correction_deltas.append(abs(
                    (current_final - current_baseline) -
                    (previous_final - previous_baseline)))
        reach = [number(row, f"{side}_prediction_reach_ratio") for row in rows]
        residual = [number(row, f"{side}_position_residual") for row in rows]
        penetration = [number(row, f"{side}_final_physical_residual_penetration") for row in rows]
        reach = [value for value in reach if value is not None]
        residual = [value for value in residual if value is not None]
        penetration = [value for value in penetration if value is not None]
        identity_mismatch = []
        revision_blend_rows = []
        jump_rows = []
        for previous, current in zip(rows, rows[1:]):
            previous_goal = number(previous, f"{side}_final_goal_world_y")
            current_goal = number(current, f"{side}_final_goal_world_y")
            if previous_goal is None or current_goal is None:
                continue
            previous_baseline = number(previous, f"{side}_baseline_goal_world_y")
            current_baseline = number(current, f"{side}_baseline_goal_world_y")
            previous_path = number(previous, f"{side}_current_path_world_y")
            current_path = number(current, f"{side}_current_path_world_y")
            previous_root = number(previous, "pose_root_world_y")
            current_root = number(current, "pose_root_world_y")
            jump_rows.append({
                "frame": int(current["frame_sequence"]),
                "goalDeltaCm": (current_goal - previous_goal) * 100.0,
                "baselineDeltaCm": None if previous_baseline is None or current_baseline is None else (current_baseline - previous_baseline) * 100.0,
                "pathDeltaCm": None if previous_path is None or current_path is None else (current_path - previous_path) * 100.0,
                "rootRelativeGoalDeltaCm": None if previous_root is None or current_root is None else (
                    (current_goal - current_root) -
                    (previous_goal - previous_root)) * 100.0,
                "plan": current.get(f"{side}_predictive_plan_sequence"),
                "revision": current.get(f"{side}_revision_plan_sequence"),
                "revisionBlend": current.get(f"{side}_plan_revision_blend_weight"),
                "eventChanged": current.get(f"{side}_landing_event_identity") != previous.get(f"{side}_landing_event_identity"),
                "surfaceChanged": current.get(f"{side}_current_path_surface") != previous.get(f"{side}_current_path_surface"),
                "contact": current.get(f"{side}_contact"),
                "transition": current.get(f"{side}_transition"),
                "reach": number(current, f"{side}_prediction_reach_ratio"),
                "pelvisCm": (number(current, "pelvis_current") or 0.0) * 100.0,
                "correctionDeltaCm": None if previous_baseline is None or current_baseline is None else (
                    (current_goal - current_baseline) -
                    (previous_goal - previous_baseline)) * 100.0,
            })
        for row in rows:
            active = number(row, f"{side}_predictive_plan_sequence") or 0.0
            revision = number(row, f"{side}_revision_plan_sequence") or 0.0
            plan_blend = number(row, f"{side}_plan_prediction_blend")
            pelvis_plan = number(row, f"{side}_pelvis_plan_sequence") or 0.0
            if revision > 0 and active == revision and plan_blend is not None and plan_blend < 0.999:
                revision_blend_rows.append(int(row["frame_sequence"]))
            if pelvis_plan > 0 and active > 0 and pelvis_plan != active:
                identity_mismatch.append(int(row["frame_sequence"]))
        result["sides"][side] = {
            "goalDeltaMaxCm": max(goal_deltas, default=0.0) * 100.0,
            "goalDeltaP95Cm": percentile(goal_deltas, 0.95) * 100.0 if goal_deltas else 0.0,
            "pathDeltaMaxCm": max(path_deltas, default=0.0) * 100.0,
            "correctionDeltaMaxCm": max(correction_deltas, default=0.0) * 100.0,
            "correctionDeltaP95Cm": percentile(correction_deltas, 0.95) * 100.0 if correction_deltas else 0.0,
            "reachMax": max(reach, default=0.0),
            "reachOverOneRows": sum(value > 1.0 for value in reach),
            "solverResidualMaxCm": max(residual, default=0.0) * 100.0,
            "physicalPenetrationMaxCm": max(penetration, default=0.0) * 100.0,
            "revisionClaimedBeforePromotionFrames": revision_blend_rows,
            "pelvisPlanMismatchFrames": identity_mismatch,
            "topJumps": sorted(jump_rows, key=lambda item: abs(item["goalDeltaCm"]), reverse=True)[:8],
            "topCorrectionJumps": sorted(
                [item for item in jump_rows if item["correctionDeltaCm"] is not None],
                key=lambda item: abs(item["correctionDeltaCm"]),
                reverse=True)[:8],
            "topRootRelativeGoalJumps": sorted(
                [item for item in jump_rows if item["rootRelativeGoalDeltaCm"] is not None],
                key=lambda item: abs(item["rootRelativeGoalDeltaCm"]),
                reverse=True)[:8],
            "topReachFrames": [
                {
                    "frame": int(row["frame_sequence"]),
                    "reach": number(row, f"{side}_prediction_reach_ratio"),
                    "residualCm": (number(row, f"{side}_position_residual") or 0.0) * 100.0,
                    "plan": row.get(f"{side}_predictive_plan_sequence"),
                    "contact": row.get(f"{side}_contact"),
                    "pelvisCm": (number(row, "pelvis_current") or 0.0) * 100.0,
                }
                for row in sorted(
                    rows,
                    key=lambda item: number(item, f"{side}_prediction_reach_ratio") or 0.0,
                    reverse=True)[:8]
            ],
            "topResidualFrames": [
                {
                    "frame": int(row["frame_sequence"]),
                    "reach": number(row, f"{side}_prediction_reach_ratio"),
                    "residualCm": (number(row, f"{side}_position_residual") or 0.0) * 100.0,
                    "plan": row.get(f"{side}_predictive_plan_sequence"),
                    "contact": row.get(f"{side}_contact"),
                    "pelvisCm": (number(row, "pelvis_current") or 0.0) * 100.0,
                }
                for row in sorted(
                    rows,
                    key=lambda item: number(item, f"{side}_position_residual") or 0.0,
                    reverse=True)[:8]
            ],
        }
    return result


print(json.dumps([summarize(path) for path in sys.argv[1:]], ensure_ascii=False, indent=2))
