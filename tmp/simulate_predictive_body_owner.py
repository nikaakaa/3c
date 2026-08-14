import csv
import glob
import gzip
import math
import os
import sys


def number(value):
    try:
        result = float(value)
        return result if math.isfinite(result) else None
    except (TypeError, ValueError):
        return None


def integer(value):
    result = number(value)
    return int(result) if result is not None else 0


def truth(value):
    return str(value).strip().lower() in {"true", "1", "yes"}


def quantiles(values):
    values = sorted(value for value in values if value is not None)
    if not values:
        return {"count": 0}

    def at(fraction):
        return values[round((len(values) - 1) * fraction)]

    return {
        "count": len(values),
        "p50": at(0.5),
        "p95": at(0.95),
        "p99": at(0.99),
        "max": values[-1],
    }


def load(run_dir):
    rows = []
    header = None
    for path in sorted(glob.glob(os.path.join(run_dir, "*.csv.gz"))):
        with gzip.open(path, "rt", encoding="utf-8-sig", newline="") as stream:
            reader = csv.DictReader(stream)
            if header is None:
                header = reader.fieldnames
            elif reader.fieldnames != header:
                raise RuntimeError("header mismatch")
            rows.extend(reader)
    return rows


def candidate(row, side):
    sequence = integer(row[f"{side}_predictive_plan_sequence"])
    root_y = number(row[f"{side}_current_path_root_world_y"])
    pose_root_y = number(row["pose_root_world_y"])
    valid = (
        sequence > 0
        and truth(row[f"{side}_plan_has_executable_path"])
        and row[f"{side}_predictive_plan_state"] == "Executing"
        and root_y is not None
        and pose_root_y is not None
    )
    return {
        "valid": valid,
        "side": side,
        "sequence": sequence,
        "target": root_y - pose_root_y if valid else None,
        "remaining": 1.0 - (number(row[f"{side}_landing_action_progress"]) or 0.0),
        "phase": row[f"{side}_pelvis_support_phase"],
    }


def select_initial(options, previous_target):
    if len(options) == 1:
        return options[0]
    supporting = [option for option in options if option["phase"] in {"Supporting", "Releasing"}]
    if len(supporting) == 1:
        return supporting[0]
    choices = supporting or options
    if previous_target is None:
        return min(choices, key=lambda item: (item["remaining"] if item["remaining"] is not None else math.inf, item["side"]))
    return min(choices, key=lambda item: (abs(item["target"] - previous_target), item["side"]))


def summarize(rows, direction):
    subset = [row for row in rows if row["left_plan_invariants_route_direction"] == direction]
    owner = None
    previous_target = None
    available = 0
    switches = 0
    off_to_on = 0
    on_to_off = 0
    target_steps = []
    switch_steps = []
    same_plan_steps = []
    top = []

    for row in subset:
        options = [candidate(row, side) for side in ("left", "right")]
        options = [option for option in options if option["valid"]]
        selected = None
        if owner is not None:
            selected = next(
                (option for option in options if option["side"] == owner["side"] and option["sequence"] == owner["sequence"]),
                None,
            )
        switched = False
        if selected is None and options:
            selected = select_initial(options, previous_target)
            switched = owner is not None and (
                selected["side"] != owner["side"] or selected["sequence"] != owner["sequence"]
            )
            owner = {"side": selected["side"], "sequence": selected["sequence"]}
        elif selected is None:
            owner = None

        target = selected["target"] if selected is not None else None
        if target is not None:
            available += 1
        if previous_target is None and target is not None:
            off_to_on += 1
        if previous_target is not None and target is None:
            on_to_off += 1
        if switched:
            switches += 1
        if previous_target is not None and target is not None:
            step = abs(target - previous_target)
            target_steps.append(step)
            if switched:
                switch_steps.append(step)
            else:
                same_plan_steps.append(step)
            top.append({
                "step": step,
                "frame": integer(row["frame_sequence"]),
                "side": selected["side"],
                "sequence": selected["sequence"],
                "switched": switched,
                "target": target,
                "previous": previous_target,
                "left_sequence": integer(row["left_predictive_plan_sequence"]),
                "right_sequence": integer(row["right_predictive_plan_sequence"]),
            })
        previous_target = target

    return {
        "rows": len(subset),
        "available": available,
        "available_rate": available / len(subset) if subset else 0,
        "switches": switches,
        "off_to_on": off_to_on,
        "on_to_off": on_to_off,
        "target_abs_delta": quantiles(target_steps),
        "same_plan_abs_delta": quantiles(same_plan_steps),
        "switch_abs_delta": quantiles(switch_steps),
        "top": sorted(top, key=lambda item: item["step"], reverse=True)[:20],
    }


def main():
    rows = load(sys.argv[1])
    for direction in ("start-to-end", "end-to-start"):
        print(direction, summarize(rows, direction))


if __name__ == "__main__":
    main()
