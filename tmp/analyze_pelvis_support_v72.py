import csv
import glob
import gzip
import json
import math
import os
import sys
from collections import Counter


def number(value):
    try:
        result = float(value)
        return result if math.isfinite(result) else None
    except (TypeError, ValueError):
        return None


def truth(value):
    return str(value).strip().lower() in {"true", "1", "yes"}


def integer(value):
    value = number(value)
    return int(value) if value is not None else 0


def quantiles(values):
    values = sorted(value for value in values if value is not None and math.isfinite(value))
    if not values:
        return {"count": 0}

    def at(fraction):
        return values[round((len(values) - 1) * fraction)]

    return {
        "count": len(values),
        "min": values[0],
        "p50": at(0.5),
        "p95": at(0.95),
        "p99": at(0.99),
        "max": values[-1],
    }


def load(run_dir, expected_width):
    rows = []
    header = None
    width_errors = []
    files = sorted(glob.glob(os.path.join(run_dir, "*.csv.gz")))
    for path in files:
        with gzip.open(path, "rt", encoding="utf-8-sig", newline="") as stream:
            reader = csv.reader(stream)
            file_header = next(reader)
            if header is None:
                header = file_header
            elif header != file_header:
                width_errors.append([os.path.basename(path), "header_mismatch"])
            if len(file_header) != expected_width:
                width_errors.append([os.path.basename(path), "header_width", len(file_header)])
            for line_number, values in enumerate(reader, 2):
                if len(values) != expected_width:
                    width_errors.append([os.path.basename(path), line_number, len(values)])
                    continue
                rows.append(dict(zip(header, values)))
    return files, header, rows, width_errors


def delta(current, previous):
    return current - previous if current is not None and previous is not None else None


def summarize(rows, direction):
    subset = [row for row in rows if row["left_plan_invariants_route_direction"] == direction]
    support_available = 0
    selected_counts = Counter()
    support_switches = 0
    invalid_switches = []
    same_owner_dropouts = []
    selected_mismatches = []
    candidate_counts = Counter()
    phase_pairs = Counter()
    availability_transitions = Counter()
    target_delta = []
    resolved_delta = []
    current_delta = []
    lyra_delta = []
    selected_delta = []
    pelvis_translation_delta = []
    same_support_target_delta = []
    switch_target_delta = []
    same_owner_selected_target_delta = []
    switch_selected_target_delta = []
    no_support_target_delta = []
    anomalies = []
    previous = None

    for row in subset:
        frame = int(float(row["frame_sequence"]))
        available = truth(row.get("pelvis_support_available"))
        side = row.get("pelvis_support_side", "")
        sequence = integer(row.get("pelvis_support_plan_sequence"))
        switched = truth(row.get("pelvis_support_switched"))
        left_candidate = truth(row.get("left_pelvis_candidate"))
        right_candidate = truth(row.get("right_pelvis_candidate"))
        left_sequence = integer(row.get("left_pelvis_plan_sequence"))
        right_sequence = integer(row.get("right_pelvis_plan_sequence"))
        current_target = number(row.get("pelvis_current_support_target"))
        selected_target = number(row.get("pelvis_selected_support_target"))
        resolved_target = number(row.get("pelvis_resolved_target"))
        current = number(row.get("pelvis_current"))
        lyra = number(row.get("pelvis_lyra_target"))
        translation = number(row.get("pelvis_translation_y"))
        left_displacement = number(row.get("left_pelvis_displacement"))
        right_displacement = number(row.get("right_pelvis_displacement"))

        if available:
            support_available += 1
            selected_counts[side] += 1
        candidate_counts[f"left={left_candidate},right={right_candidate}"] += 1
        phase_pairs[f"{row.get('left_pelvis_support_phase')}|{row.get('right_pelvis_support_phase')}"] += 1
        if switched:
            support_switches += 1

        expected_selected = left_displacement if side == "Left" else right_displacement if side == "Right" else None
        if available and (expected_selected is None or selected_target is None or abs(expected_selected - selected_target) > 1e-5):
            selected_mismatches.append({"frame": frame, "side": side, "selected": selected_target, "expected": expected_selected})
        if previous is not None:
            availability_transitions[f"{previous['available']}->{available}"] += 1
            target_step = delta(current_target, previous["current_target"])
            resolved_step = delta(resolved_target, previous["resolved_target"])
            current_step = delta(current, previous["current"])
            lyra_step = delta(lyra, previous["lyra"])
            selected_step = delta(selected_target, previous["selected_target"])
            translation_step = delta(translation, previous["translation"])
            target_delta.append(abs(target_step) if target_step is not None else None)
            resolved_delta.append(abs(resolved_step) if resolved_step is not None else None)
            current_delta.append(abs(current_step) if current_step is not None else None)
            lyra_delta.append(abs(lyra_step) if lyra_step is not None else None)
            selected_delta.append(abs(selected_step) if selected_step is not None else None)
            pelvis_translation_delta.append(abs(translation_step) if translation_step is not None else None)

            same_support = available and previous["available"] and side == previous["side"] and sequence == previous["sequence"]
            changed_support = available and previous["available"] and (side != previous["side"] or sequence != previous["sequence"])
            if same_support:
                same_support_target_delta.append(abs(resolved_step) if resolved_step is not None else None)
                same_owner_selected_target_delta.append(abs(selected_step) if selected_step is not None else None)
            elif changed_support:
                switch_target_delta.append(abs(resolved_step) if resolved_step is not None else None)
                switch_selected_target_delta.append(abs(selected_step) if selected_step is not None else None)
                old_candidate = left_candidate if previous["side"] == "Left" else right_candidate
                old_sequence = left_sequence if previous["side"] == "Left" else right_sequence
                if old_candidate and old_sequence == previous["sequence"]:
                    invalid_switches.append({
                        "frame": frame,
                        "from": previous["side"],
                        "to": side,
                        "from_sequence": previous["sequence"],
                        "to_sequence": sequence,
                        "old_candidate": old_candidate,
                        "reported_switched": switched,
                    })
            elif not available:
                no_support_target_delta.append(abs(resolved_step) if resolved_step is not None else None)
                old_candidate = left_candidate if previous["side"] == "Left" else right_candidate
                old_sequence = left_sequence if previous["side"] == "Left" else right_sequence
                if previous["available"] and old_candidate and old_sequence == previous["sequence"]:
                    same_owner_dropouts.append({
                        "frame": frame,
                        "side": previous["side"],
                        "sequence": previous["sequence"],
                    })

            score = max(abs(resolved_step or 0), abs(current_step or 0), abs(translation_step or 0))
            anomalies.append({
                "score": score,
                "frame": frame,
                "available": available,
                "side": side,
                "sequence": sequence,
                "reported_switched": switched,
                "left_candidate": left_candidate,
                "right_candidate": right_candidate,
                "left_sequence": left_sequence,
                "right_sequence": right_sequence,
                "current_target": current_target,
                "selected_target": selected_target,
                "resolved_target": resolved_target,
                "pelvis_current": current,
                "target_delta": target_step,
                "resolved_delta": resolved_step,
                "current_delta": current_step,
                "translation_delta": translation_step,
                "left_mode": row.get("left_pelvis_constraint_mode"),
                "right_mode": row.get("right_pelvis_constraint_mode"),
                "left_phase": row.get("left_pelvis_support_phase"),
                "right_phase": row.get("right_pelvis_support_phase"),
            })

        previous = {
            "available": available,
            "side": side,
            "sequence": sequence,
            "current_target": current_target,
            "selected_target": selected_target,
            "resolved_target": resolved_target,
            "current": current,
            "lyra": lyra,
            "translation": translation,
        }

    return {
        "rows": len(subset),
        "support_available_rows": support_available,
        "support_available_rate": support_available / len(subset) if subset else 0,
        "selected_counts": selected_counts,
        "candidate_counts": candidate_counts,
        "phase_pairs": phase_pairs,
        "availability_transitions": availability_transitions,
        "reported_support_switches": support_switches,
        "switch_while_old_candidate_valid": invalid_switches,
        "same_owner_dropouts": same_owner_dropouts,
        "selected_candidate_mismatches": selected_mismatches,
        "current_support_target_abs_delta": quantiles(target_delta),
        "selected_support_target_abs_delta": quantiles(selected_delta),
        "resolved_target_abs_delta": quantiles(resolved_delta),
        "pelvis_current_abs_delta": quantiles(current_delta),
        "pelvis_lyra_target_abs_delta": quantiles(lyra_delta),
        "pelvis_translation_abs_delta": quantiles(pelvis_translation_delta),
        "same_support_resolved_abs_delta": quantiles(same_support_target_delta),
        "switch_resolved_abs_delta": quantiles(switch_target_delta),
        "same_owner_selected_target_abs_delta": quantiles(same_owner_selected_target_delta),
        "switch_selected_target_abs_delta": quantiles(switch_selected_target_delta),
        "no_support_resolved_abs_delta": quantiles(no_support_target_delta),
        "top_anomalies": sorted(anomalies, key=lambda item: item["score"], reverse=True)[:20],
    }


def main():
    run_dir = sys.argv[1]
    expected_width = int(sys.argv[2])
    files, header, rows, width_errors = load(run_dir, expected_width)
    result = {
        "files": len(files),
        "rows": len(rows),
        "header_width": len(header),
        "unique_headers": len(set(header)),
        "width_errors": width_errors,
        "directions": {
            direction: summarize(rows, direction)
            for direction in ("start-to-end", "end-to-start", "hold-start", "hold-end")
        },
    }
    payload = json.dumps(result, ensure_ascii=False, indent=2, default=dict)
    if len(sys.argv) > 3:
        with open(sys.argv[3], "w", encoding="utf-8", newline="\n") as stream:
            stream.write(payload)
            stream.write("\n")
    else:
        print(payload)


if __name__ == "__main__":
    main()
