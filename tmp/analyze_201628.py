import csv
from collections import OrderedDict, defaultdict

path = r"D:\Unity_Project_1\3C\3cDemo\Client\3C_Client\Temp\FootLandingSamples\foot-landing-20260818-201628-692-d405c969094546449b2055b340024b5c.csv"
with open(path, encoding="utf-8") as f:
    rows = list(csv.DictReader(f))

print("rows", len(rows))
print("cols has Stride", "StrideState" in (rows[0] if rows else {}))
print("header stride-related", [c for c in (rows[0].keys() if rows else []) if "Stride" in c or "Pelvis" in c or "Plant" in c or "FootMotion" in c][:40])

seen = OrderedDict()
for r in rows:
    key = (r.get("RootInstanceId"), int(r["FrameSequence"]), r["Side"])
    if key not in seen:
        seen[key] = r

by_root = defaultdict(list)
for r in seen.values():
    by_root[r["RootInstanceId"]].append(r)

for rid, items in by_root.items():
    print("root", rid, "unique", len(items))

# pick the one with stride accepted if possible
focus = None
for rid, items in by_root.items():
    acc = sum(1 for x in items if x.get("StrideState") == "Accepted")
    print(" root", rid, "strideAccepted", acc, "fmAccepted", sum(1 for x in items if x.get("FootMotionState")=="Accepted"))
    if focus is None or acc > 0:
        focus = rid

print("FOCUS", focus)
items = by_root[focus]
# unique frames left/right
frames = OrderedDict()
for r in items:
    frames.setdefault(int(r["FrameSequence"]), {})[r["Side"]] = r

print("Frame Lfm Lrej Lw Lvert Rfm Rrej Rw Rvert Stride StRej Sup Swg Prog Slope PelW PelResid")
n = 0
for frame, pair in frames.items():
    L = pair.get("Left")
    R = pair.get("Right")
    if not L or not R:
        continue
    lw = float(L.get("FinalGoalPositionWeight") or 0)
    rw = float(R.get("FinalGoalPositionWeight") or 0)
    lv = float(L.get("FootMotionVerticalCorrection") or 0)
    rv = float(R.get("FootMotionVerticalCorrection") or 0)
    st = L.get("StrideState", "")
    if st == "" and n > 30:
        continue
    # print a window of interesting frames
    if n < 40 or lw > 0.01 or rw > 0.01 or st == "Accepted":
        print(
            f"{frame:4d} {L.get('FootMotionState','?'):8} {L.get('FootMotionRejectReason','?'):22} {lw:4.2f} {lv:5.3f} "
            f"{R.get('FootMotionState','?'):8} {R.get('FootMotionRejectReason','?'):22} {rw:4.2f} {rv:5.3f} "
            f"{st:8} {L.get('StrideRejectReason','?'):20} {L.get('StrideSupportSide','?'):5} {L.get('StrideSwingSide','?'):5} "
            f"{float(L.get('StrideProgress') or 0):4.2f} {L.get('StrideSlope','?'):4} "
            f"{float(L.get('PelvisPositionWeight') or 0):4.2f} {float(L.get('FinalPhysicalPelvisGoalResidual') or 0):6.3f}"
        )
        n += 1
        if n >= 80:
            break

# summaries
swing_w = 0
plant_like = 0
zero = 0
for r in items:
    st = r.get("FootMotionState")
    w = float(r.get("FinalGoalPositionWeight") or 0)
    rej = r.get("FootMotionRejectReason")
    if st == "Accepted" and w > 0.01:
        if rej == "None" and float(r.get("FootMotionProgress") or 0) > 0:
            swing_w += 1
        else:
            plant_like += 1
    else:
        zero += 1
print("nonzero accepted", swing_w, "other accepted nonzero", plant_like, "zeroish", zero)
