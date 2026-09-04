import json
from collections import defaultdict
from pathlib import Path

from build_guide import Sources, differences, flatten, read_json, table, write_json


def canonical(value):
    return json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(",", ":"))


def main():
    settings = read_json(Path(__file__).with_name("sources.json"))
    out = Path(settings["output"])
    analysis = out / "analysis"
    search = analysis / "camera-resource-search"
    sources = Sources(settings)
    manifest = read_json(search / "decoded-camera/manifest.json")
    variants = defaultdict(list)
    source_file = {}
    for index, entry in enumerate(manifest["results"]):
        key = f"public-camera:{index}"
        roots = sources.decoded(key, Path(entry["output"]))
        filename = Path(entry["output"]).name
        write_json(analysis / "camera-data" / filename, roots)
        variants[entry["unityName"]].append({"sourceId": key, "dataFile": "camera-data/" + filename, "roots": roots})
        source_file[key] = entry["output"]
    write_json(analysis / "camera-data/source-index.json", sources.records)
    curve_variants = defaultdict(list)
    curve_manifest = read_json(search / "native-curves/manifest.json")
    for entry in curve_manifest["results"]:
        library = read_json(Path(entry["path"]))
        for group, rows in library["groups"].items():
            for row in rows:
                value = {key: data for key, data in row["curve"].items() if key not in ("byteOffset", "byteLength")}
                curve_variants[row["name"]].append({"library": entry["path"], "group": group,
                                                     "sourceSha256": entry["sourceSha256"], "curve": value})
    dependencies = read_json(out / "data/dependencies.json")
    resolved = {}
    for key, references in dependencies.items():
        found = curve_variants.get(key, [])
        signatures = {canonical(v["curve"]) for v in found}
        resolved[key] = {"references": references, "status": "curve-values-confirmed" if found and len(signatures) == 1 else "curve-variant-conflict" if found else "standard-config-not-located",
                         "variants": found}
    write_json(analysis / "resolved-camera-dependencies.json", resolved)
    lines = ["# 已补齐的公共镜头曲线", "", "原 AnimationCurveLibrary 并非 Odin 字典正文，而是 Unity 原生 StringAnimationCurve 列表。已按同版本 Keyframe/AnimationCurve 格式读取三个完整文件，全部消费到文件末尾；保留切线、加权模式、权重和边界模式。", "",
             table(["资源键", "状态", "来源数", "关键帧数"], [(key, value["status"], len(value["variants"]), len(value["variants"][0]["curve"]["keys"]) if value["variants"] else "未取得") for key, value in resolved.items()]), ""]
    for key, record in resolved.items():
        if record["status"] != "curve-values-confirmed":
            continue
        curve = record["variants"][0]["curve"]
        lines += [f"## {key}", "", f"三个来源的此曲线数值一致。PreInfinity={curve['preInfinity']}，PostInfinity={curve['postInfinity']}，RotationOrder={curve['rotationOrder']}。", "",
                  table(["time", "value", "inSlope", "outSlope", "weightedMode", "inWeight", "outWeight"],
                        [[k[f] for f in ("time", "value", "inSlope", "outSlope", "weightedMode", "inWeight", "outWeight")] for k in curve["keys"]]), ""]
    (analysis / "公共曲线.md").write_text("\n".join(lines), encoding="utf-8")
    plot_curves(analysis, resolved)
    lines = ["# 基础镜头与公共效果", "", "新定位到 Pipeline_Camera_Avatar_Config 的三个来源。以下明确使用 Persistent/1021078955.blk 的资源作为阅读基准，所有变体均保留。Default_Normal 是配置键；本轮尚未证明当前 Corin 实例选择该键。", ""]
    profile_variants = variants["Pipeline_Camera_Avatar_Config"]
    base = next(v for v in profile_variants if "1021078955" in v["dataFile"])
    delta = []
    for variant in profile_variants:
        changes = list(differences(base["roots"], variant["roots"]))
        delta.append({"source": variant["sourceId"], "dataFile": variant["dataFile"], "differences": changes})
    write_json(analysis / "camera-profile-variant-differences.json", delta)
    lines += ["## 角色镜头组", "", table(["组", "FOV", "半径", "默认仰角", "默认平滑时间"],
             [(key, data.get("DEFAULTSPHEREDATA", {}).get("CAMERA_FOV"), data.get("CAMERA_LOCATE_RADIUS"), data.get("ELEVATION_ANGLE"), data.get("DEFAULT_SMOOTH_TIME")) for key, data in base["roots"]["cameraAvatarGroup"].items()]), "",
             "## Default_Normal 完整已解码字段", "", table(["原字段", "值"], list(flatten(base["roots"]["cameraAvatarGroup"]["Default_Normal"]))), "",
             "## 状态到球面配置键", "", table(["原状态枚举", "配置键"], base["roots"]["CameraStateDefaultSphereKeyDict"].items()), "",
             "## 球面配置", ""]
    for key, data in base["roots"]["cameraAvatarSphereGroup"].items():
        lines += [f"### {key}", "", table(["原字段", "值"], list(flatten(data))), ""]
    lines += ["## 公共效果文件", "", table(["资源", "变体数", "阅读数据"],
              [(name, len(records), "；".join(f"[版本 {i}]({r['dataFile']})" for i, r in enumerate(records))) for name, records in variants.items() if name != "Pipeline_Camera_Avatar_Config"]), "",
              "## 尚未完成的原生尾部", "", "Pipeline_Camera_Avatar_Config 的 Odin 字典之外仍有 Unity 原生序列化尾部。普通目标/Boss 锁定等字段不能因为三个字典解析成功就算全部读齐。CameraCutscenes 和 CameraLockDatas 原始对象已取得，但其空 Odin payload 后的原生列表仍待类型布局解析。", ""]
    (analysis / "基础镜头.md").write_text("\n".join(lines), encoding="utf-8")
    findings = {"publicCurvesResolved": sum(v["status"] == "curve-values-confirmed" for v in resolved.values()),
                "publicCurvesConflict": sum(v["status"] == "curve-variant-conflict" for v in resolved.values()),
                "standardConfigKeysUnresolved": [k for k, v in resolved.items() if v["status"] == "standard-config-not-located"],
                "cameraProfileGroups": len(base["roots"]["cameraAvatarGroup"]), "cameraSphereGroups": len(base["roots"]["cameraAvatarSphereGroup"]),
                "cameraOdinDocuments": len(manifest["results"]), "cameraNativeObjectsPending": len(manifest["errorList"]),
                "profileVariantDifferences": {x["dataFile"]: len(x["differences"]) for x in delta}}
    write_json(analysis / "camera-completion-summary.json", findings)
    print(json.dumps(findings, ensure_ascii=False))


def plot_curves(analysis, resolved):
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    import numpy as np
    curves = [(key, r["variants"][0]["curve"]) for key, r in resolved.items() if r["status"] == "curve-values-confirmed"]
    figure, axes = plt.subplots(3, 2, figsize=(12, 9), constrained_layout=True)
    for axis, (name, curve) in zip(axes.flat, curves):
        keys = curve["keys"]
        if any(k["weightedMode"] != 0 for k in keys):
            raise ValueError("此预览只表达本批已确认的非加权 Hermite 曲线")
        for start, end in zip(keys, keys[1:]):
            u = np.linspace(0, 1, 100)
            duration = end["time"] - start["time"]
            y = (2 * u**3 - 3 * u**2 + 1) * start["value"] + (u**3 - 2 * u**2 + u) * duration * start["outSlope"] + (-2 * u**3 + 3 * u**2) * end["value"] + (u**3 - u**2) * duration * end["inSlope"]
            axis.plot(start["time"] + u * duration, y, color="#2563eb", linewidth=2)
        axis.scatter([k["time"] for k in keys], [k["value"] for k in keys], color="#e11d48", s=22, zorder=3)
        axis.set_title(name, fontsize=10)
        axis.set_xlabel("Curve input (original domain)")
        axis.set_ylabel("Value")
        axis.grid(alpha=.18)
    figure.suptitle("Corin camera curves | original keyframes + unweighted Hermite interpolation", fontsize=13)
    figure.savefig(analysis / "camera-curves.png", dpi=160)
    plt.close(figure)
    path = analysis / "公共曲线.md"
    content = path.read_text(encoding="utf-8")
    intro = "\n\n曲线横轴保留原时间域，不统一假设为秒；例如空间曲线的横轴到 10。蓝线为原关键帧及切线的非加权 Hermite 插值，红点为原关键帧，未模拟完整相机。\n\n![公共镜头曲线](" + (analysis / "camera-curves.png").as_posix() + ")\n"
    path.write_text(content + intro, encoding="utf-8")


if __name__ == "__main__":
    main()
