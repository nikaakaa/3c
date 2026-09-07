import argparse
import json
from collections import defaultdict
from pathlib import Path

from build_guide import read_json, table, write_json


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--sources", type=Path, required=True)
    args = parser.parse_args()
    settings = read_json(args.sources)
    out = Path(settings["output"])
    summary = read_json(out / "data/summary.json")
    dependencies = read_json(out / "data/dependencies.json")
    shared = Path(settings["sharedEvidenceRoot"])
    libraries = read_json(shared / "camera-resource-search/native-curves/manifest.json")
    by_name = defaultdict(list)
    for record in libraries["results"]:
        library = read_json(Path(record["path"]))
        for group, rows in library["groups"].items():
            for row in rows:
                value = {k: v for k, v in row["curve"].items() if k not in ("byteOffset", "byteLength")}
                by_name[row["name"]].append({"library": record["path"], "sourceSha256": record["sourceSha256"], "group": group, "curve": value})
    resolved = {}
    for key, references in dependencies.items():
        variants = by_name.get(key, [])
        signatures = {json.dumps(v["curve"], sort_keys=True) for v in variants}
        status = "curve-values-confirmed" if len(signatures) == 1 else "curve-variant-conflict" if signatures else "standard-config-not-located"
        resolved[key] = {"references": references, "status": status, "variants": variants}
    write_json(out / "analysis/resolved-camera-dependencies.json", resolved)
    missing_clips = []
    missing_files = []
    unbound_states = []
    unresolved_cameras = []
    shared_patterns = []
    for path in sorted((out / "data/actions").glob("*.json")):
        action = read_json(path)
        name = action["state"]["data"]["Name"]
        if not action["eventPatterns"]:
            unbound_states.append(name)
        for pattern in action["eventPatterns"]:
            if pattern != settings["character"] + "_" + name:
                shared_patterns.append({"state": name, "pattern": pattern})
        for clip in action["clips"]:
            if not clip["resources"]:
                missing_clips.append({"state": name, "clip": clip["reference"]})
            for resource in clip["resources"]:
                if resource["ExportedPath"] and not Path(resource["ExportedPath"]).exists():
                    missing_files.append({"state": name, "path": resource["ExportedPath"]})
        for event in action["events"]:
            for reference in event["cameraReferences"]:
                if not reference["resolved"]:
                    unresolved_cameras.append({"state": name, "eventId": event["id"], **reference})
    report = {"character": settings["character"], "focusedStates": summary["focusedStates"],
              "missingClipIdentities": missing_clips, "missingExportedFiles": missing_files,
              "statesWithoutEventBinding": unbound_states, "explicitSharedEventPatterns": shared_patterns,
              "unresolvedFocusedCameraReferences": unresolved_cameras,
              "resolvedPublicCurves": [k for k, v in resolved.items() if v["status"] == "curve-values-confirmed"],
              "unresolvedDependencyKeys": [k for k, v in resolved.items() if v["status"] != "curve-values-confirmed"]}
    write_json(out / "analysis/coverage.json", report)
    duplicates = read_json(out / "data/duplicate-sources.json")
    lines = [f"# {settings['character']} 资料对账与分析", "",
             "本轮只整理原始资料与引用关系，不修改 Unity 配置。没有绘图。", "",
             "## 覆盖", "", table(["内容", "数量"], [(k, v) for k, v in summary.items() if k not in ("schema", "character")]), "",
             "## 动作引用对账", "", table(["检查项", "结果"], [(k, v) for k, v in report.items() if k not in ("character", "focusedStates")]), "",
             "未绑定事件的状态可能本就没有事件，保留事实，不按相似名字补绑定。不同状态共享事件组时以 CharacterScriptConfig 的 pairList 为准。", "",
             "## 已读出的点击和长按", "", "[技能与输入表](../技能与输入.md)列出按钮编号、直接激活技能及 Trigger/Int/Bool 写入。它能回答按键映射成哪些参数，但不能单凭这张表确认缓冲时长或释放后的清除规则。", "",
             "## 公共曲线", ""]
    for key, record in resolved.items():
        if record["status"] != "curve-values-confirmed":
            continue
        curve = record["variants"][0]["curve"]
        lines += [f"### {key}", "", f"已取得 {len(record['variants'])} 个来源，曲线内容一致；PreInfinity={curve['preInfinity']}，PostInfinity={curve['postInfinity']}。", "",
                  table(["time", "value", "inSlope", "outSlope", "weightedMode", "inWeight", "outWeight"], [[k[f] for f in ("time", "value", "inSlope", "outSlope", "weightedMode", "inWeight", "outWeight")] for k in curve["keys"]]), ""]
    lines += ["## 来源变体", "", table(["资源", "来源数", "相对基准的字段差异数"], [(r["name"], r["copies"], [v["differencesFromBaseline"] for v in r["variants"]]) for r in duplicates]), "",
              "完整差异与每个版本正文见 [变体目录](../data/duplicate-sources.json)。这里的计数包括字段存在性与诊断字段，不等于同样数量的业务差异。读取基准在 sources 配置中显式指定；本轮不替用户决定实际运行应采用哪个来源。", "",
              "## 仍需继续分析", "", "- 未定位的标准模板或其它资源键，见上面的逐项清单。", "- 输入缓冲、长按释放、Trigger 消费与清除时点。",
              "- 原始比较模式、叠加枚举、负时间与镜头中断/恢复规则。", "- Shot 正文已接入公共索引；仍需紧凑布局未命名字和 prefab 动画/绑定对账，见 [镜头补缺](../镜头补缺.md)。",
              f"- 同版本共用的镜头与时间区域函数证据：[共享分析]({shared.as_posix()}/README.md)。", ""]
    (out / "analysis/README.md").write_text("\n".join(lines), encoding="utf-8")
    print(json.dumps({"character": settings["character"], "actions": summary["focusedStates"], "missingClips": len(missing_clips), "missingFiles": len(missing_files), "sharedEventPatterns": len(shared_patterns), "unresolvedFocusedCameras": len(unresolved_cameras), "publicCurves": len(report["resolvedPublicCurves"])}, ensure_ascii=False))


if __name__ == "__main__":
    main()
