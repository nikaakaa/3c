import csv
import glob
import gzip
import json
import math
import os
import sys
from collections import defaultdict


def q(values, fraction):
    values = sorted(values)
    return values[round((len(values) - 1) * fraction)] if values else None


def spring(value, velocity, target, delta_seconds):
    omega = 2.5 * 2.0 * math.pi
    error = value - target
    c2 = velocity + error * omega
    e = 1.0 / (
        1.0 + 1.00746054 * omega * delta_seconds +
        0.45053901 * (omega * delta_seconds) ** 2 +
        0.25724632 * (omega * delta_seconds) ** 3)
    return (
        target + (error + c2 * delta_seconds) * e,
        (c2 - error * omega - c2 * omega * delta_seconds) * e,
    )


def summarize(values):
    return {
        "count": len(values),
        "p50": q(values, 0.5),
        "p95": q(values, 0.95),
        "p99": q(values, 0.99),
        "max": max(values) if values else None,
    }


def main():
    run = sys.argv[1]
    rows = []
    for path in sorted(glob.glob(os.path.join(run, "*.csv.gz"))):
        with gzip.open(path, "rt", encoding="utf-8-sig", newline="") as stream:
            rows.extend(csv.DictReader(stream))
    states = {
        "old": {"initialized": False, "value": 0.0, "velocity": 0.0},
        "rebased": {"initialized": False, "value": 0.0, "velocity": 0.0},
    }
    previous_reset = None
    previous_world = {}
    output = defaultdict(lambda: defaultdict(list))
    old_error = []
    for row in rows:
        reset = row["reset_sequence"]
        if reset != previous_reset:
            for state in states.values():
                state.update(initialized=False, value=0.0, velocity=0.0)
            previous_world.clear()
            previous_reset = reset
        target = float(row["pelvis_lyra_target"])
        root = float(row["pose_root_world_y"])
        delta = float(row["presentation_delta_seconds"])
        root_delta = float(row["pose_root_vertical_delta"])
        direction = row["left_plan_invariants_route_direction"]
        for name, state in states.items():
            if state["initialized"] and name == "rebased":
                state["value"] -= root_delta
            if not state["initialized"] and delta > 0.000001:
                state.update(initialized=True, value=target, velocity=0.0)
            else:
                state["value"], state["velocity"] = spring(
                    state["value"], state["velocity"], target, delta)
            world = root + state["value"]
            if name in previous_world and direction in ("start-to-end", "end-to-start"):
                output[direction][name].append(abs(world - previous_world[name]))
            previous_world[name] = world
        old_error.append(abs(states["old"]["value"] - float(row["pelvis_current"])))
    result = {
        "rows": len(rows),
        "old_simulation_error": summarize(old_error),
        "directions": {
            direction: {
                name: summarize(values)
                for name, values in variants.items()
            }
            for direction, variants in output.items()
        },
    }
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
