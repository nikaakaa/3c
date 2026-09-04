import json
from collections import defaultdict
from pathlib import Path

from build_guide import Sources, differences, digest, flatten, read_json, table, write_json


def load_native(search, sources):
    path = search / "native-camera/manifest.json"
    sources.record("native-camera-manifest", path)
    records = []
    for i, record in enumerate(read_json(path)["results"]):
        source = Path(record["path"])
        data = read_json(source)
        if digest(Path(data["source"]["RawPath"])) != record["sourceSha256"]:
            raise ValueError("镜头原生数据的来源哈希不一致")
        sources.record(f"native-camera:{i}", source)
        records.append((record, data))
    return records


def render_native(analysis, records, odin_variants):
    index = {"shot": defaultdict(list), "lock": defaultdict(list)}
    lines = ["# 镜头原生参数补缺", "", "12 个对象均消费到文件末尾：5 份 Shot 列表、4 份锁定列表、3 份基础镜头尾部。",
             "其中两份 Shot 的全部字段可按 829 元数据命名；另外三份采用不同混合布局，每条记录有 5 个 uint 暂未赋予字段名。完整消费字节不代表这 5 个值已经解释。", "",
             table(["对象", "来源 Block", "条目", "布局", "未命名字数", "原生数据"],
             [(r["name"], Path(d["source"]["Plan"]["SourceBlock"]).stem, r["entries"], r["layout"], r["unmappedWordCount"], f"[JSON]({r['path']})") for r, d in records]), "",
             "读取过程复用现有 Odin 头部和 Unity 曲线读取器；补出的类字段来自同版本元数据。每个字段记录原始字节位置，原资源不改写。", "",
             "## 基础镜头", "", "普通目标/Boss 锁定字段已与原 Odin 字典合并到同一 camera-data 文件，详见 [基础镜头](基础镜头.md)。", "",
             "## 时长的实际判定", "", "同版本 CameraShotData.get_durationByEvent 的指令比较 _duration 与 0，确认 `_duration < 0` 时返回 true。因此 -1 会启用这个标记；具体由哪个结束事件停止，仍需追消费者。见 [getter 证据](camera-resource-search/native-camera/duration-rule.json)。", "",
             "## 尚未确认", "", "- 三份紧凑 Shot 布局的 5 个原始字的具体含义。", "- Shot 结束事件调度、绑定类型及播放模式等枚举的完整消费者语义。",
             "- 镜头 prefab 路径已经取得，同名根、动画与 Timeline 候选见 [镜头资源对账](镜头资源对账.md)；真实引用闭环仍未确认。", "- 原游戏实际选择哪份资源、效果叠加和打断顺序。", "- CamShake_A_*/E_* 标准模板仍未定位正文。", ""]
    for record, document in records:
        category = {"CameraCutscenes": "shot", "CameraLockDatas": "lock"}.get(record["name"])
        if category is None:
            continue
        for i, row in enumerate(document["nativeFields"]["KeyValueInfoList"]):
            index[category][row["keyInst"]].append({"path": record["path"], "rowIndex": i, "sourceSha256": record["sourceSha256"],
                                                  "block": Path(document["source"]["Plan"]["SourceBlock"]).stem, "layout": record["layout"]})
    for name, category, root in (("CameraShakes_Common", "shake", "cameraShakes"), ("CameraZooms_Common", "zoom", "cameraZooms"),
                                  ("CameraStretchs_Common", "stretch", "cameraStretchs"), ("CameraOverrideTracks_Common", "override", "config")):
        index[category] = defaultdict(list)
        for variant in odin_variants[name]:
            for key in variant["roots"][root]:
                index[category][key].append({"path": (analysis / variant["dataFile"]).as_posix(), "dictionary": root,
                                             "key": key, "sourceId": variant["sourceId"]})
    write_json(analysis / "shared-camera-index.json", index)
    (analysis / "镜头原生参数.md").write_text("\n".join(lines), encoding="utf-8")
    documents = {r["path"]: d for r, d in records}
    shot_lines = ["# Corin、Unagi、Anbi 的 Shot 参数", "", "以下按资源键中的角色名列出阅读资料；名字匹配不代表该动作实际触发。角色事件到 Shot 的精确引用见各角色的镜头补缺页。",
                  "阅读基准固定为 Persistent/2733648653，所有其它来源均保留。基准是展示选择，尚未证明游戏实际加载这份。",
                  "`_duration=-1` 会使原代码的 durationByEvent 属性返回 true，结束事件的调度仍待确认。枚举保留原值。", ""]
    prefab_refs = []
    for key, variants in index["shot"].items():
        if not any(actor in key for actor in ("Corin", "Unagi", "Anbi")):
            continue
        base = next(v for v in variants if v["block"] == "2733648653")
        value = documents[base["path"]]["nativeFields"]["KeyValueInfoList"][base["rowIndex"]]["valueInst"]
        shot_lines += [f"## {key}", "", table(["字段", "原值"], list(flatten(value))), "",
                       "来源：" + "；".join(f"[{v['block']}]({v['path']})" for v in variants), ""]
        prefab_refs.append({"key": key, "prefabPath": value["_cinePrefab"], "definition": base})
    write_json(analysis / "camera-resource-search/native-camera/prefab-references.json", prefab_refs)
    (analysis / "camera-resource-search/native-camera/prefab-terms.txt").write_text("\n".join(sorted({Path(r["prefabPath"]).stem for r in prefab_refs if r["prefabPath"]})) + "\n", encoding="utf-8")
    (analysis / "镜头Shot参数.md").write_text("\n".join(shot_lines), encoding="utf-8")
    lock_lines = ["# 锁定配置", "", "原列表逐项保存 keyInst、valueType、JSON 正文，正文中的字段名直接来自源文件。下表保留不同来源，没有按同名覆盖。", ""]
    for key, variants in index["lock"].items():
        lock_lines += [f"## {key}", "", table(["Block", "具体类型", "参数"],
                       [(v["block"], row["valueType"], row["valueInst"]) for v in variants
                        for row in [documents[v["path"]]["nativeFields"]["KeyValueInfoList"][v["rowIndex"]]]]), ""]
    (analysis / "锁定配置.md").write_text("\n".join(lock_lines), encoding="utf-8")
    return index


def canonical(value):
    return json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(",", ":"))


def parameter_values(value):
    if isinstance(value, dict):
        return {k: parameter_values(v) for k, v in value.items() if k not in ("byteOffset", "byteLength")}
    if isinstance(value, list):
        return [parameter_values(v) for v in value]
    return value


def main():
    settings = read_json(Path(__file__).parent / "sources/corin.json")
    out = Path(settings["output"])
    analysis = out / "analysis"
    search = analysis / "camera-resource-search"
    sources = Sources(settings)
    native = load_native(search, sources)
    native_by_source = {Path(r["rawPath"]): d for r, d in native}
    manifest = read_json(search / "decoded-camera/manifest.json")
    variants = defaultdict(list)
    source_file = {}
    for index, entry in enumerate(manifest["results"]):
        key = f"public-camera:{index}"
        roots = sources.decoded(key, Path(entry["output"]))
        if entry["unityName"] == "Pipeline_Camera_Avatar_Config":
            tail = native_by_source[Path(entry["source"])]["nativeFields"]
            if roots.keys() & tail.keys():
                raise ValueError("原生尾部与 Odin 字典出现重叠字段")
            roots.update(parameter_values(tail))
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
              "## 已补出的普通目标与 Boss 锁定", "", table(["原字段", "原值"],
              [(k, v) for k, v in flatten({k: v for k, v in base["roots"].items() if k not in ("cameraAvatarGroup", "cameraAvatarSphereGroup", "CameraStateDefaultSphereKeyDict")})]), "",
              "三个原始对象的尾部均完整消费；字段已合并到本页引用的同一 camera-data 文件。布局及剩余语义问题见 [镜头原生参数](镜头原生参数.md)。", ""]
    (analysis / "基础镜头.md").write_text("\n".join(lines), encoding="utf-8")
    findings = {"publicCurvesResolved": sum(v["status"] == "curve-values-confirmed" for v in resolved.values()),
                "publicCurvesConflict": sum(v["status"] == "curve-variant-conflict" for v in resolved.values()),
                "standardConfigKeysUnresolved": [k for k, v in resolved.items() if v["status"] == "standard-config-not-located"],
                "cameraProfileGroups": len(base["roots"]["cameraAvatarGroup"]), "cameraSphereGroups": len(base["roots"]["cameraAvatarSphereGroup"]),
                "cameraOdinDocuments": len(manifest["results"]),
                "cameraNativeObjectsPending": len({Path(e["source"]) for e in manifest["errorList"]} - native_by_source.keys()),
                "cameraNativeObjectsDecoded": len(native), "cameraNativeObjectsWithUnmappedWords": sum(r["unmappedWordCount"] > 0 for r, _ in native),
                "profileVariantDifferences": {x["dataFile"]: len(x["differences"]) for x in delta}}
    write_json(analysis / "camera-completion-summary.json", findings)
    render_native(analysis, native, variants)
    print(json.dumps(findings, ensure_ascii=False))


if __name__ == "__main__":
    main()
