import argparse
import hashlib
import importlib.util
import json
import re
from collections import Counter, defaultdict
from pathlib import Path


LABELS = {
    "m_TransitionDuration": "混合时长", "m_TransitionOffset": "目标切入偏移",
    "m_TransitionOffsetCount": "目标切入帧数", "m_FrameCount": "转场帧字段",
    "m_ExitTime": "退出归一化时间", "m_TotalFramesSrc": "源状态总帧数",
    "m_TotalFramesDest": "目标状态总帧数", "m_HasExitTime": "启用退出时间",
    "m_UseFrameCount": "使用帧数", "m_HasFixedDuration": "固定混合时长",
    "m_InterruptionSource": "中断来源枚举", "m_OrderedInterruption": "顺序中断",
    "m_AutoTransitionOffset": "自动切入偏移", "m_AutoTransitionOffsetValue": "自动偏移值",
    "m_AutoTransitionOffsetRatio": "自动偏移比例", "m_CanTransitionToSelf": "允许转回自身",
    "m_Atomic": "Atomic 原字段", "m_Speed": "播放速度", "m_CycleOffset": "循环偏移",
    "m_Loop": "循环", "m_Mirror": "镜像", "m_IKOnFeet": "IKOnFeet 原标记",
    "Fov": "FOV", "FovVariationType": "FOV 变化枚举", "DelayTime": "延迟时间",
    "StartTime": "进入时间", "LastTime": "保持时间", "EndTime": "退出时间",
    "StartCurveKey": "进入曲线引用", "EndCurveKey": "退出曲线引用",
    "RadiusRatio": "距离比例", "RotationZ": "侧倾", "PosOffsetX": "X 偏移",
    "PosOffsetY": "Y 偏移", "PosOffsetZ": "Z 偏移", "CamOffsetLocalCoords": "局部坐标开关",
    "StretchTime": "拉伸时间", "HoldTime": "保持时间", "RecoilTime": "回弹时间",
    "ShakeTotalTime": "震动持续时间", "Frequency": "频率", "RadiusLength": "位移幅度",
    "AngleVertical": "方向角", "NoiseAngle": "方向噪声", "NoiseRatio": "噪声比例",
    "RollAmplitude": "Roll 幅度", "PitchAmplitude": "Pitch 幅度", "YawAmplitude": "Yaw 幅度",
    "DissipationMode": "衰减模式枚举", "CurveKey": "衰减曲线引用",
    "CustomCurveKey": "空间曲线引用", "StandardConfigKey": "标准配置引用",
    "FadeInDuration": "淡入时间", "FadeOutDuration": "淡出时间",
    "PlayPriority": "播放优先级", "DataPriority": "数据优先级枚举",
    "PlayStackingType": "播放叠加枚举", "StackingType": "叠加枚举",
    "IgnoreWorldTimeScale": "忽略世界时间缩放", "IgnoreOwnerTimeScale": "忽略角色时间缩放",
    "IngoreTimeScale": "忽略时间缩放（原字段拼写）", "priority": "优先级",
    "trackSetting": "轨道设置", "miscSetting": "偏移与 FOV", "blendIn": "进入混合",
    "blendOut": "退出混合", "duration": "持续时间", "curve": "曲线引用",
    "StartFrame": "起始帧", "EndFrame": "结束帧", "FrameCount": "总帧字段",
    "MaxStartFrame": "最大起始帧标记", "MaxEndFrame": "最大结束帧标记",
    "StartNormalizedTime": "起始归一化时间", "EndNormalizedTime": "结束归一化时间",
}
CAMERA_FIELDS = {
    "CameraShakeKey": "shake", "CameraZoomKey": "zoom", "CameraStretchKey": "stretch",
    "EndCameraZoomKey": "zoom", "EndCameraStretchKey": "stretch", "OverrideKey": "override",
    "CameraShotKey": "shot", "EndCameraShotKey": "shot",
}
CATEGORIES = {"shake": "震动", "zoom": "缩放", "stretch": "拉伸", "override": "轨道覆盖"}


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def digest(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def write_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2, allow_nan=False) + "\n", encoding="utf-8")


def text_value(value):
    if value is None:
        return "null"
    if isinstance(value, float):
        return format(value, ".8g")
    if isinstance(value, (dict, list, bool)):
        return json.dumps(value, ensure_ascii=False, separators=(",", ":"))
    return str(value)


def cell(value):
    return text_value(value).replace("|", "\\|").replace("\n", " ")


def table(headers, rows):
    return "\n".join(["| " + " | ".join(headers) + " |", "| " + " | ".join("---" for _ in headers) + " |"] +
                     ["| " + " | ".join(cell(v) for v in row) + " |" for row in rows])


def flatten(value, prefix=""):
    if isinstance(value, dict):
        for key, child in value.items():
            yield from flatten(child, f"{prefix}.{key}" if prefix else key)
    elif isinstance(value, list):
        for index, child in enumerate(value):
            yield from flatten(child, f"{prefix}[{index}]")
    else:
        yield prefix, value


def simplify(value):
    if isinstance(value, list):
        return [simplify(v) for v in value]
    if not isinstance(value, dict):
        return value
    if "__type" in value and "items" in value:
        return simplify(value["items"])
    return {( "$type" if k == "__type" else k): simplify(v) for k, v in value.items()}


def differences(left, right, path=""):
    if isinstance(left, dict) and isinstance(right, dict):
        for key in sorted(left.keys() | right.keys()):
            if key not in left or key not in right:
                yield {"path": path + "/" + key, "leftPresent": key in left, "rightPresent": key in right,
                       "left": left.get(key), "right": right.get(key)}
            else:
                yield from differences(left[key], right[key], path + "/" + key)
    elif isinstance(left, list) and isinstance(right, list):
        if len(left) != len(right):
            yield {"path": path + "/length", "left": len(left), "right": len(right)}
        for index, (a, b) in enumerate(zip(left, right)):
            yield from differences(a, b, path + "/" + str(index))
    elif left != right:
        yield {"path": path, "left": left, "right": right}


class Sources:
    def __init__(self, settings):
        self.root = Path(settings["dumpRoot"])
        self.records = {}
        self.settings = settings
        spec = importlib.util.spec_from_file_location("character_odin", self.root / "kern_tools/OdinBinaryDecoder.py")
        self.decoder = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(self.decoder)
        self.record("odin-decoder", self.root / "kern_tools/OdinBinaryDecoder.py")

    def record(self, key, path):
        self.records[key] = {"path": path.as_posix(), "sha256": digest(path)}

    def load(self, key):
        path = self.root / self.settings[key]
        self.record(key, path)
        return read_json(path)

    def config_catalog(self):
        if self.settings["configCatalogFormat"] == "config-index":
            return self.load("configIndex")
        if self.settings["configCatalogFormat"] != "raw-export":
            raise ValueError("未注册的配置清单格式")
        raw_entries = []
        decoded_entries = []
        for key, paths in (("rawConfigManifests", self.settings["rawConfigManifests"]), ("decodedConfigManifests", self.settings["decodedConfigManifests"])):
            for index, relative in enumerate(paths):
                path = self.root / relative
                self.record(f"{key}:{index}", path)
                records = read_json(path)["results"]
                (raw_entries if key == "rawConfigManifests" else decoded_entries).extend(records)
        by_source = {Path(e["source"]): e for e in decoded_entries}
        entries = []
        for entry in raw_entries:
            path = Path(entry["RawPath"])
            decode = by_source.get(path)
            entries.append({"name": entry["Plan"]["Name"], "relative": path.as_posix(),
                            "output": decode["output"] if decode else None,
                            "kind": "odin-fields" if decode else "raw-object",
                            "identity": {k: entry["Plan"][k] for k in ("SerializedFile", "PathId", "ClassId")}})
        return {"rawRoot": "", "entries": entries}

    def animation_catalog(self):
        manifest = self.load("animationManifest")
        if self.settings["animationManifestFormat"] == "replication-summary":
            for clip in manifest["Clips"]:
                clip["ExportedPath"] = (Path(manifest["ProjectAnimationDirectory"]) / "dump" / clip["OutputFile"]).as_posix() if clip["Kind"] == "UnityAnimation" else None
            return manifest
        if self.settings["animationManifestFormat"] != "animation-export":
            raise ValueError("未注册的动画清单格式")
        directory = self.root / self.settings["animationDirectory"]
        manifest["UnfilteredEntryCount"] = len(manifest["Clips"])
        manifest["Clips"] = [clip for clip in manifest["Clips"] if re.match(self.settings["animationNamePattern"], clip["ClipName"])]
        for clip in manifest["Clips"]:
            clip["Name"] = clip["ClipName"]
            clip["Kind"] = "UnityAnimation" if clip["OutputFile"].endswith(".anim") else "RawOnly"
            clip["ExportedPath"] = (directory / clip["OutputFile"]).as_posix() if clip["Kind"] == "UnityAnimation" else None
        return manifest

    def decoded(self, key, relative):
        path = self.root / self.settings["decodedRoot"] / relative
        document = read_json(path)
        raw = Path(document["source"])
        self.record(key, path)
        actual = digest(raw)
        if actual != document["sha256"]:
            raise ValueError(f"原始文件哈希与解码记录不一致: {raw}")
        self.records[key].update(rawPath=raw.as_posix(), rawSha256=actual, unityName=document["unityName"])
        identities = {}

        def collect(node):
            if isinstance(node, dict):
                if "$id" in node:
                    identities[node["$id"]] = node
                for field in node.get("fields", []):
                    collect(field)
                for item in node.get("items", []):
                    collect(item)

        for root in document["roots"]:
            collect(root)

        def resolve(node, visited=frozenset()):
            if not isinstance(node, dict):
                return node
            if node.get("$kind") == "internal-reference":
                identity = node["id"]
                if identity in visited:
                    return node
                if identity not in identities:
                    raise ValueError(f"Odin 引用缺少目标: {key}/{identity}")
                return resolve(identities[identity], visited | {identity})
            result = dict(node)
            if "fields" in node:
                result["fields"] = [resolve(v, visited) for v in node["fields"]]
            if "items" in node:
                result["items"] = [resolve(v, visited) for v in node["items"]]
            return result

        return {root["$name"]: simplify(self.decoder.materialize(resolve(root))) for root in document["roots"]}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--sources", type=Path, required=True)
    args = parser.parse_args()
    settings = read_json(args.sources)
    out = Path(settings["output"])
    out.mkdir(parents=True, exist_ok=True)
    sources = Sources(settings)
    controller = sources.load("controller")
    animation_manifest = sources.animation_catalog()
    config_index = sources.config_catalog()
    configs = {key: sources.decoded(key, path) for key, path in settings["selectedConfigs"].items()}
    selected = {key: value[settings["configRoots"][key]] for key, value in configs.items() if key in settings["configRoots"]}
    cameras = {category: selected[category] for category in CATEGORIES if category in selected}
    character = configs.get("character", {})
    attacks = selected["attack"]
    events = []
    for domain in ("battle", "effect", "audio"):
        for pattern_index, pattern in enumerate(selected[domain]):
            for order, event in enumerate(pattern.get("entries", [])):
                camera_refs = [{"field": field, "category": category, "key": event[field],
                                "resolved": event[field] in cameras.get(category, {})}
                               for field, category in CAMERA_FIELDS.items() if event.get(field)]
                attack_key = next((event.get(field) for field in ("AnimEventID", "EventKey") if event.get(field) in attacks), None)
                if attack_key:
                    attack = attacks[attack_key]
                    for field, nested, category in (("CameraShake", "shakeConfigKey", "shake"), ("CameraZoom", "ZoomConfigKey", "zoom"), ("CameraStretch", "StretchConfigKey", "stretch")):
                        payload = attack.get(field)
                        if payload and payload.get(nested):
                            camera_refs.append({"field": f"AttackProperty.{field}.{nested}", "category": category,
                                                "key": payload[nested], "triggerPath": "attack-property",
                                                "attackPropertyKey": attack_key, "payload": payload,
                                                "resolved": payload[nested] in cameras.get(category, {})})
                events.append({"id": f"{domain}:{pattern_index}:{order}", "source": domain,
                               "pattern": pattern["name"], "order": order, "data": event,
                               "cameraReferences": camera_refs, "attackPropertyKey": attack_key})
    windows = []
    for zone_key, zone in selected["zones"].items():
        for layer, states in (zone.get("LayerStateSegmentDict") or {}).items():
            for state, segments in states.items():
                for index, segment in enumerate(segments):
                    windows.append({"id": f"{zone_key}:{layer}:{state}:{index}", "source": "zones",
                                    "zone": zone_key, "layer": int(layer), "state": state, "data": segment})
    states = []
    transitions = []
    for machine in controller["StateMachines"]:
        for state in machine["States"]:
            identity = f"sm{machine['Index']}:state{state['Index']}"
            states.append({"id": identity, "machineIndex": machine["Index"], "data": state})
            for transition in state["Transitions"]:
                transitions.append({"sourceStateId": identity, "sourceState": state["Name"], "data": transition})
    scoped = [s for s in states if re.match(settings["focusedStatePattern"], s["data"]["Name"])]
    dependencies = defaultdict(list)
    for category, entries in cameras.items():
        for key, data in entries.items():
            for field, value in flatten(data):
                if isinstance(value, str) and value and ("CurveKey" in field or field.endswith(".curve") or field.endswith("StandardConfigKey")):
                    dependencies[value].append({"category": category, "resource": key, "field": field})
    unresolved_refs = [dict(eventId=e["id"], **r) for e in events for r in e["cameraReferences"] if not r["resolved"]]
    duplicates = defaultdict(list)
    for entry in config_index["entries"]:
        duplicates[entry["name"]].append(entry)
    duplicate_report = []
    selected_names = {r.get("unityName") for r in sources.records.values() if r.get("unityName")}
    for name in sorted(selected_names):
        variants = []
        baseline_key = next(k for k, r in sources.records.items() if r.get("unityName") == name)
        selected_path = Path(sources.records[baseline_key]["path"])
        decoded_copies = [e for e in duplicates[name] if e.get("output")]
        if name in settings.get("variantPaths", {}):
            allowed = {sources.root / settings["decodedRoot"] / p for p in settings["variantPaths"][name]}
            copies = [e for e in decoded_copies if Path(e["output"]) in allowed]
        elif settings["configCatalogFormat"] == "raw-export":
            baseline = next(e for e in decoded_copies if Path(e["output"]) == selected_path)
            copies = [e for e in decoded_copies if e["identity"] == baseline["identity"]]
        else:
            copies = decoded_copies
        raw_hashes = {digest(Path(config_index["rawRoot"]) / e["relative"]) for e in copies}
        for i, copy in enumerate(copies):
            relative = Path(copy["output"]).relative_to(sources.root / settings["decodedRoot"])
            variant = sources.decoded(f"variant:{baseline_key}:{i}", relative)
            delta = list(differences(configs[baseline_key], variant))
            variant_name = f"{baseline_key}-{i}.json"
            write_json(out / "data/variants" / variant_name, variant)
            variants.append({"sourceKey": f"variant:{baseline_key}:{i}", "data": "variants/" + variant_name,
                             "differencesFromBaseline": len(delta), "differenceExamples": delta[:30]})
        duplicate_report.append({"name": name, "baselineSourceKey": baseline_key, "copies": len(copies), "rawHashes": sorted(raw_hashes),
                                 "sameRawBytes": len(raw_hashes) == 1, "paths": [e["relative"] for e in copies]})
        duplicate_report[-1]["variants"] = variants
    data_sets = {"controller": controller, "states": states, "transitions": transitions, "character": character,
                 "events": events, "windows": windows, "zones": selected["zones"],
                 "cameras": cameras, "attack-properties": attacks,
                 "animation-resources": animation_manifest["Clips"], "parameters": controller["Parameters"],
                 "dependencies": dict(sorted(dependencies.items())), "duplicate-sources": duplicate_report,
                 "unresolved-event-references": unresolved_refs}
    for name, data in data_sets.items():
        write_json(out / "data" / f"{name}.json", data)
    sources.record("guide-sources", args.sources.resolve())
    sources.record("guide-builder", Path(__file__).resolve())
    write_json(out / "data/source-index.json", sources.records)
    summary = {"schema": "character-replication-guide/1", "character": settings["character"], "states": len(states), "focusedStates": len(scoped), "parameters": len(controller["Parameters"]),
               "transitions": len(transitions), "events": dict(Counter(e["source"] for e in events)),
               "zones": len(selected["zones"]), "windows": len(windows), "cameraResources": {k: len(v) for k, v in cameras.items()},
               "attackProperties": len(attacks), "eventAttackLinks": sum(e["attackPropertyKey"] is not None for e in events),
               "unresolvedCameraEventReferences": len(unresolved_refs), "externalCameraDependencies": len(dependencies),
               "rawAnimationEntries": len(animation_manifest["Clips"]), "characterConfigIncluded": bool(character),
               "skills": len(character["Skills"]) if "Skills" in character else None,
               "clickControls": len(character["ClickControls"]) if "ClickControls" in character else None,
               "holdControls": len(character["HoldControls"]) if "HoldControls" in character else None}
    write_json(out / "data/summary.json", summary)
    render_actions(out, scoped, events, windows, cameras, sources.records, animation_manifest, attacks, settings, character)
    render_character(out, character)
    render_cameras(out, cameras, dependencies, sources.records)
    render_reference(out, summary, sources.records, settings)
    print(json.dumps({"output": str(out), **summary}, ensure_ascii=False))


def file_name(state):
    return f"sm{state['machineIndex']}-{state['data']['Index']:03d}-{state['data']['Name']}.md"


def conditions(transition):
    return " 且 ".join(f"{c['EventName']} [模式 {c['m_ConditionMode']}] {text_value(c['m_EventThreshold'])}" for c in transition["Conditions"]) or "无参数条件"


def render_actions(out, states, events, windows, cameras, sources, animation_manifest, attacks, settings, character):
    directory = out / "动作"
    directory.mkdir(exist_ok=True)
    index = []
    layers = read_json(Path(sources["controller"]["path"]))["Layers"]
    for state in states:
        data = state["data"]
        name = data["Name"]
        layer_ids = [layer["Index"] for layer in layers if layer["m_StateMachineIndex"] == state["machineIndex"]]
        if settings["eventBindingMode"] == "character-map":
            bindings = [pair for layer in layer_ids for pair in character["AnimatorStateEventPatternsDict"].get(str(layer), {}).get("pairList", []) if pair["stateName"] == name]
            pattern_names = {pair["pattern"] for pair in bindings}
        elif settings["eventBindingMode"] == "exact-prefix":
            pattern_names = {settings["eventPatternPrefix"] + name}
        else:
            raise ValueError("未注册的事件绑定方式")
        matching_events = [e for e in events if e["pattern"] in pattern_names]
        matching_windows = [w for w in windows if w["state"] == name and w["layer"] in layer_ids]
        camera_keys = sorted({(r["category"], r["key"]) for e in matching_events for r in e["cameraReferences"] if r["resolved"]})
        clips = []
        for tree in data["BlendTrees"]:
            for node in tree["Nodes"]:
                reference = node.get("Clip")
                if not reference or reference.get("PathId") == 0:
                    continue
                matches = [c for c in animation_manifest["Clips"] if c["PathId"] == reference["PathId"] and c["SerializedFile"] == reference["ExternalFile"]]
                clips.append({"reference": reference, "resources": matches, "blendTreeIndex": tree["Index"], "nodeIndex": node["Index"]})
        lines = [f"# {name}", "", "[返回动作索引](../动作索引.md)", "",
                 f"原状态身份：`{state['id']}`；路径：`{data.get('FullPathName', '')}`。",
                 "", "表格小数为阅读显示；精确值在 data JSON。帧字段、归一化时间和枚举原值全部保留，不自动推断比较符或把 -1 改成无限。",
                 "", "不同 Block 的同名配置已另存到 data/variants；本页使用 source-index 中明确列出的基准文件，不表示已确认运行时选择此变体。", "",
                 "## 动画资源", "", table(["动画", "资源身份", "导出状态", "工程文件"],
                 [(c["reference"]["Name"], f"{c['reference']['ExternalFile']} / {c['reference']['PathId']}",
                   [r["Kind"] for r in c["resources"]],
                   "；".join(f"[动画]({r['ExportedPath']})" for r in c["resources"] if r["Kind"] == "UnityAnimation")) for c in clips]),
                 "", "动画按 SerializedFile + PathID 对账，不按相似名字替换；无匹配时保留未解析身份。完整 BlendTree 参数随动作 JSON 保存。",
                 "", "## 动画播放", "", table(["字段", "原值"],
                 [(f"{LABELS.get(k, k)} `{k}`", data[k]) for k in ("m_Speed", "m_CycleOffset", "m_Loop", "m_Mirror", "m_IKOnFeet", "SpeedParameter") if k in data]),
                 "", "## 转场与响应条件", "", table(["原顺序", "目标", "混合时长", "转场帧字段", "目标切入帧", "退出时间启用", "条件（原始比较模式）"],
                 [(t["Index"], t["DestinationStateName"], t["m_TransitionDuration"], t["m_FrameCount"], t["m_TransitionOffsetCount"], t["m_HasExitTime"], conditions(t)) for t in data["Transitions"]]),
                 "", "`special_*` 目标是原控制器保留的特殊目标编码，不直接当成具体动作。参数条件、退出门槛及中断规则须同时考虑；本表不是已经确认的输入缓冲窗口。",
                 "", "## 明确时间区域", "", table(["区域", "起始帧", "结束帧", "MaxStart", "MaxEnd", "总帧字段", "归一化起止"],
                 [(w["zone"], w["data"].get("StartFrame"), w["data"].get("EndFrame"), w["data"].get("MaxStartFrame"), w["data"].get("MaxEndFrame"), w["data"].get("FrameCount"), [w["data"].get("StartNormalizedTime"), w["data"].get("EndNormalizedTime")]) for w in matching_windows]),
                 "", "MaxEnd=true 时 EndFrame=0 不能直接解释为零长度。帧与归一化值不一致时保留两者，待原采样函数确认采用条件。",
                 "", "## 按原顺序排列的事件", "", f"绑定方式：`{settings['eventBindingMode']}`；事件组：`{', '.join(sorted(pattern_names))}`。", "", table(["域", "原序号", "帧", "长度", "最大帧标记", "事件类型", "资源或参数"],
                 [(e["source"], e["order"], e["data"].get("frame"), e["data"].get("frameLength"), e["data"].get("maxFrame"), e["data"].get("$type", "").split(",")[0].split(".")[-1],
                   {k: v for k, v in e["data"].items() if k in ("AnimEventID", "EventKey", "TriggerID", "Enable", "StartedSkillName", "AbilityName") or k in CAMERA_FIELDS}) for e in matching_events]),
                 "", "Battle、Effect、Audio 是三个来源域；同帧跨域调用顺序尚未确认。完整事件包含强制触发、进出转场标记、标签条件，见本动作 JSON。",
                 "", "## 攻击事件关联的镜头", "", table(["帧", "攻击配置", "镜头类型", "资源键", "原触发配置"],
                 [(e["data"].get("frame"), r["attackPropertyKey"], r["category"], r["key"], r["payload"]) for e in matching_events for r in e["cameraReferences"] if r.get("triggerPath") == "attack-property"]),
                 "", "上述镜头来自攻击处理路径，不能当成同帧必定播放的 Timeline 事件；是否命中、ShakeOnNotHit 等原条件必须保留。",
                 "", "## 本动作引用的镜头", ""]
        for category, key in camera_keys:
            lines += [f"### {CATEGORIES[category]}：{key}", "", table(["字段", "原值"],
                     [(f"{LABELS.get(field.split('.')[-1], field)} `{field}`", value) for field, value in flatten(cameras[category][key]) if "$type" not in field]), ""]
        lines += ["## 来源", "", f"- [控制器原文件]({sources['controller']['path']})",
                  f"- [区域原文件]({sources['zones']['path']})", f"- [完整动作数据](../data/actions/{file_name(state).replace('.md', '.json')})", ""]
        (directory / file_name(state)).write_text("\n".join(lines), encoding="utf-8")
        write_json(out / "data/actions" / file_name(state).replace(".md", ".json"),
                   {"state": state, "events": matching_events, "windows": matching_windows,
                    "cameraKeys": camera_keys, "clips": clips, "eventPatterns": sorted(pattern_names),
                    "attackProperties": {e["attackPropertyKey"]: attacks[e["attackPropertyKey"]] for e in matching_events if e["attackPropertyKey"]},
                    "sourceIndex": "../source-index.json"})
        index.append((f"[{name}](动作/{file_name(state)})", state["machineIndex"], len(data["Transitions"]), len(matching_windows), len(matching_events), len(camera_keys)))
    (out / "动作索引.md").write_text(f"# {settings['character']} 动作索引\n\n当前重点为攻击、分支、蓄力、冲刺攻击、反击与闪避；切人不进入本轮动作页。完整控制器原结构保留在 data/controller.json。\n\n" +
                                     table(["动作", "状态机", "转场", "时间区域", "事件", "直接镜头引用"], index) + "\n", encoding="utf-8")


def render_cameras(out, cameras, dependencies, sources):
    lines = ["# 镜头参数", "", "这些是资源字段原值，不代表标准配置覆盖后或运行中的最终值。-1、枚举和坐标单位未经确认时不做解释。", ""]
    for category, entries in cameras.items():
        lines += [f"## {CATEGORIES[category]}：{len(entries)} 项", "", f"[原始解码文件]({sources[category]['path']})", ""]
        for key, data in entries.items():
            lines += [f"### {key}", "", table(["字段说明与原名", "原值"], [(f"{LABELS.get(field.split('.')[-1], field)} `{field}`", value) for field, value in flatten(data) if "$type" not in field]), ""]
    (out / "镜头参数.md").write_text("\n".join(lines), encoding="utf-8")
    progress_file = out / "analysis/resolved-camera-dependencies.json"
    progress = read_json(progress_file) if progress_file.exists() else {}
    labels = {"curve-values-confirmed": "曲线关键帧与切线已取得，来源一致", "curve-variant-conflict": "曲线变体有差异", "standard-config-not-located": "标准配置正文未定位"}
    lines = ["# 公共镜头依赖", "", "按精确资源键聚合全部引用。最新曲线数值与来源对账见 [本角色分析](analysis/README.md)。", "",
             table(["资源键", "引用数量", "引用字段", "当前状态"], [(key, len(refs), sorted({r["field"] for r in refs}), labels.get(progress.get(key, {}).get("status"), "待定位")) for key, refs in sorted(dependencies.items())]), ""]
    (out / "公共镜头依赖.md").write_text("\n".join(lines), encoding="utf-8")


def render_reference(out, summary, sources, settings):
    (out / "字段说明.md").write_text("# 字段与复刻边界\n\n" + table(["原字段", "中文说明"], sorted(LABELS.items())) +
        "\n\n## 必须保留的区别\n\n- 混合时长不是输入窗口长度；目标切入偏移不是混合时长。\n- 转场 m_FrameCount 与 Conditions 中 FrameCount 阈值是两个字段，不能互相覆盖。\n- Trigger 触发条件不等于按键缓冲时长，长按 Bool 也不等于重复点击。\n- 帧域来自原状态与事件，不直接当成 3C Simulation tick。\n- 曲线资源键不是曲线正文；标准配置键也可能改变最终生效参数。\n- 资源存在、解析成功、运行语义确认是三个不同状态。\n- 时间区域只表示该 Zone 生效，不统一解释成允许输入、无敌或伤害窗口；必须看 Zone 类型。\n- data JSON 保留原数值精度、原排序、类型及身份；Markdown 只做阅读投影。\n", encoding="utf-8")
    intro = [f"# {settings['character']} 复刻资料", "", "本包是现有离线资料的可重建阅读投影，不是另一套游戏配置。原始 dump、Unity authoring 和生成产物均不由本工具修改。", "",
             "## 阅读入口", "", "- [按动作查看混合、条件、窗口和镜头](动作索引.md)", "- [全部镜头原始参数](镜头参数.md)",
             "- [技能和点击／长按配置](技能与输入.md)",
             "- [公共镜头资源依赖](公共镜头依赖.md)", "- [字段说明与使用边界](字段说明.md)",
             "- [本角色资料缺口](资料缺口.md)", "- [机器可读数据与统计](data/summary.json)", "- [精确来源及 SHA-256](data/source-index.json)", "",
             "## 覆盖", "", table(["内容", "数量"], [(k, v) for k, v in summary.items() if k != "schema"]), "",
             "## 使用方式", "", "每个动作先确定原状态身份与动画，再按原顺序读取转场条件；时间区域和事件分别保留。配置 BTSMTL 时只迁移已确认语义，不把未解析枚举或负时间猜成默认规则。切人暂不进入本轮动作页。", "",
             "原始来源重复资源的哈希对账见 data/duplicate-sources.json。不同内容不会按同名自动合并。内部 Odin 引用使用同一解码器展开，外部引用保留显式标记。", "",
             f"生成器：[build_guide.py]({sources['guide-builder']['path']})；配置：[{Path(sources['guide-sources']['path']).name}]({sources['guide-sources']['path']})。", ""]
    (out / "README.md").write_text("\n".join(intro), encoding="utf-8")
    (out / "资料缺口.md").write_text("# 资料缺口\n\n" +
        "- 事件引用未匹配的镜头：[逐项记录](data/unresolved-event-references.json)。\n" +
        "- 同名资源与变体：[来源与差异](data/duplicate-sources.json)。\n" +
        "- 公共曲线、标准键：[依赖表](公共镜头依赖.md)。\n" +
        "- 本页不把源配置的点击/长按映射当成完整输入缓存逻辑；消费时点和未展开枚举仍需消费者证据。\n" +
        f"- 已有共享镜头与时间区域研究：[分析记录]({settings['sharedEvidenceRoot']}/README.md)。共享函数证据可以复用，角色配置不能借用其它角色的数值。\n", encoding="utf-8")
    analysis = out / "analysis"
    analysis.mkdir(exist_ok=True)
    if not (analysis / "README.md").exists():
        (analysis / "README.md").write_text("# 补缺分析\n\n当前资料已整理。后续公共资源导出、枚举数值、时间规则与消费者分析统一在本目录登记；不修改来源数据。\n", encoding="utf-8")


def render_character(out, character):
    if not character:
        (out / "技能与输入.md").write_text("# 技能与输入配置\n\n本包尚未纳入 CharacterScriptConfig 的技能与点击/长按根字段；这表示资料未纳入，不表示角色没有这些配置。转场条件仍见动作页。\n", encoding="utf-8")
        return
    lines = ["# 技能与输入配置", "", "数值直接来自 CharacterScriptConfig 的正式根字段。按钮编号保持原值，未将其猜成具体键盘按键。", ""]
    for control in ("ClickControls", "HoldControls"):
        lines += [f"## {control}", "", table(["按钮编号", "激活技能", "Trigger", "Int", "Bool"],
            [(key, data.get("activeSkillName"), data.get("animatorParamControl", {}).get("triggerDict"), data.get("animatorParamControl", {}).get("integerDict"), data.get("animatorParamControl", {}).get("booleanDict")) for key, data in character.get(control, {}).items()]), ""]
    lines += ["## 技能", ""]
    for key, data in character.get("Skills", {}).items():
        lines += [f"### {key}", "", table(["原字段", "原值"], list(flatten(data))), ""]
    lines += ["## 动画混合覆盖", ""]
    for key in ("BlendData", "StartBlendData", "BlendDataByTag"):
        if key in character:
            lines += [f"### {key}", "", table(["原字段", "原值"], list(flatten(character[key]))), ""]
    lines += ["完整角色根字段、事件组绑定和相机覆盖见 [角色数据](data/character.json)。", ""]
    (out / "技能与输入.md").write_text("\n".join(lines), encoding="utf-8")


if __name__ == "__main__":
    main()
