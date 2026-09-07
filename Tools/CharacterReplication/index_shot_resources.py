import argparse
from pathlib import Path

from build_guide import digest, read_json, table, write_json


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path("D:/ZZZ_Dump/output/corin_replication/replication-guide/analysis/camera-resource-search"))
    args = parser.parse_args()
    reference_path = args.root / "native-camera/prefab-references.json"
    report_path = args.root / "shot-resource-search/camerashotresources-assets.json"
    report = read_json(report_path)
    scan = read_json(report_path.parent / "block-scan.json")
    rows = []
    for reference in read_json(reference_path):
        name = Path(reference["prefabPath"]).stem
        candidates = {}
        for category, class_id, target in (("prefabRoot", "GameObject", name), ("animation", "AnimationClip", name),
                                           ("timeline", "MonoBehaviour", name + "_TimeLine"),
                                           ("playable", "MonoBehaviour", "AnimationPlayableAsset of " + name)):
            candidates[category] = [a for a in report["Assets"] if a["ClassId"] == class_id and a["Name"] == target]
        counts = {key: len({(a["SerializedFile"], a["PathId"]) for a in assets}) for key, assets in candidates.items()}
        rows.append({**reference, "candidateIdentityCounts": counts, "candidates": candidates,
                     "matchBasis": "资源对象名精确匹配 prefab 路径文件名；尚未验证 AssetBundle 路径映射和 PPtr 绑定闭环",
                     "referenceClosureVerified": False})
    result = {"source": report_path.as_posix(), "sourceSha256": digest(report_path), "referenceSha256": digest(reference_path),
              "blockCount": scan["BlockCount"], "scanFailures": scan["Failures"], "resolverFailures": report["Failures"],
              "resolverErrors": report["ResolverErrors"], "references": rows}
    write_json(report_path.parent / "resource-coverage.json", result)
    lines = ["# Shot 镜头资源对账", "", f"扫描 {scan['BlockCount']} 个 block；原始扫描错误 {len(scan['Failures'])}，候选对象解析错误 {len(report['ResolverErrors'])}。",
             "以下数量按 CAB + PathID 去重；所有 Block 来源仍在 JSON 保留。按精确对象名找到资源候选，还需验证 prefab 路径映射以及 Timeline、PlayableAsset、动画之间的真实引用，不能把同名当成绑定证明。", "",
             table(["Shot 键", "prefab 根候选", "动画候选", "Timeline 候选", "Playable 候选"],
                   [(r["key"], *[r["candidateIdentityCounts"][k] for k in ("prefabRoot", "animation", "timeline", "playable")]) for r in rows]), "",
             "`Test_Anbi_QuestStart_02` 所指的同名根、动画和 Timeline 未在此次扫描定位；这是 Test 配置引用，不据此断言安比正式开场镜头缺失。", "",
             "候选解析错误来自 1013241275.blk 的 AnimatorController，原异常保留在 resource-coverage.json；此次不能宣称所有候选对象均解析成功。", "",
             "- [全部候选身份、来源和错误](camera-resource-search/shot-resource-search/resource-coverage.json)",
             "- [原始 Shot 参数](镜头Shot参数.md)", ""]
    (args.root.parent / "镜头资源对账.md").write_text("\n".join(lines), encoding="utf-8")
    print({"shotKeys": len(rows), "prefabPaths": len({r['prefabPath'] for r in rows}),
           "unlocatedRoots": [r["key"] for r in rows if not r["candidates"]["prefabRoot"]]})


if __name__ == "__main__":
    main()
