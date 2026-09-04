import csv
import json
import sys
from collections import defaultdict
from pathlib import Path

from build_guide import differences, digest, read_json, table, write_json


ROOT = Path("D:/ZZZ_Dump")
OUT = ROOT / "output/corin_replication/replication-guide"
META = ROOT / "PIK分析包/元数据/控制器与战斗"
ANALYSIS = OUT / "analysis"


def read_csv(name):
    with (META / name).open(encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(stream))


def variant_differences():
    records = read_json(OUT / "data/duplicate-sources.json")
    baseline = read_json(OUT / "data/variants/battle-0.json")["patterns"]
    comparison = read_json(OUT / "data/variants/battle-1.json")["patterns"]
    other = {p["name"]: p for p in comparison}
    changes = []
    for pattern in baseline:
        a = {e["guid_Editor"]: e for e in pattern["entries"]}
        b = {e["guid_Editor"]: e for e in other[pattern["name"]]["entries"]}
        for key in sorted(a.keys() | b.keys()):
            if a.get(key) == b.get(key):
                continue
            changes.append({"pattern": pattern["name"], "guid": key,
                            "kind": "added-in-2393852806" if key not in a else "missing-in-2393852806" if key not in b else "changed",
                            "baseline": a.get(key), "comparison": b.get(key)})
    write_json(ANALYSIS / "variant-event-differences.json", changes)
    window_changes = []
    a = read_json(OUT / "data/variants/zones-1.json")
    for i in (0, 2, 3):
        b = read_json(OUT / f"data/variants/zones-{i}.json")
        delta = list(differences(a, b))
        write_json(ANALYSIS / f"zone-variant-{i}-differences.json", delta)
        window_changes.append({"variant": i, "differences": len(delta)})
    lines = ["# 同名资源变体分析", "", "原始文件身份与哈希见 [重复资源对账](../data/duplicate-sources.json)。同名并非同内容；没有合并或决定运行版本。", "",
             "## 战斗事件", "", "按 guid_Editor 对齐事件身份后比较，避免把数组插入造成的位移当成多个参数修改。基准为 1510480920.blk，比较对象为 2393852806.blk。", "",
             table(["动作", "差异", "基准事件", "比较事件"],
                   [(c["pattern"], c["kind"], c["baseline"], c["comparison"]) for c in changes]), "",
             "## 区域配置", "", table(["变体序号", "字段差异数"], [(x["variant"], x["differences"]) for x in window_changes]), "",
             "基准是显式选择的 1387972831.blk 来源。其余变体包含区域成员变化，另有大量字段不同的版本。复刻实施前须沿角色配置加载引用确定实际变体，不能仅按名称挑一个或拼接数值。", ""]
    (ANALYSIS / "资源变体.md").write_text("\n".join(lines), encoding="utf-8")
    return changes, window_changes


def metadata_evidence():
    fields = read_csv("fields.csv")
    methods = read_csv("methods.csv")
    types = read_csv("type_index.csv")
    roots = {"MoleMole.Config.ConfigCameraZoom", "MoleMole.Config.ConfigCameraStretch", "MoleMole.Config.ConfigCameraShake",
             "MoleMole.CameraModuleAvatarDataConfig", "MoleMole.CameraModuleAvatarDataConfigExt",
             "MoleMole.Config.PipelineCameraAvatarConfigData", "MoleMole.Config.ConfigAnimationCurve",
             "MoleMole.Config.AnimatorZone", "MoleMole.Config.AnimatorStateTimeSegment"}
    wanted_fields = [f for f in fields if f["owner"] in roots]
    enum_names = {t["name"] for t in types if t["parent"] == "Enum"}
    used_enums = {f["type"] for f in wanted_fields if f["type"] in enum_names}
    enum_fields = [f for f in fields if f["owner"].split(".")[-1] in used_enums]
    wanted_methods = [m for m in methods if m["owner"] in roots]
    write_json(ANALYSIS / "camera-field-metadata.json", wanted_fields)
    write_json(ANALYSIS / "camera-method-metadata.json", wanted_methods)
    write_json(ANALYSIS / "camera-enum-declarations.json", enum_fields)
    lines = ["# 相机字段和枚举分析入口", "", "从已核验的 829 元数据筛出正式字段与方法。字段偏移是对象布局；literal 枚举字段的 0x0 登记偏移不是枚举值。", "",
             "## 枚举", "", table(["枚举", "成员", "数值状态"],
             [(name, [f["field"] for f in enum_fields if f["owner"].split(".")[-1] == name and f["literal"] == "True"], "常量值未展开，不按声明顺序编号") for name in sorted(used_enums)]), "",
             "## 函数入口", "", table(["类型", "方法", "参数", "RVA"], [(m["owner"], m["method"], m["parameters"], m["RVA"]) for m in wanted_methods]), ""]
    (ANALYSIS / "字段与函数入口.md").write_text("\n".join(lines), encoding="utf-8")
    return wanted_methods


def native_evidence(methods):
    sys.path.insert(0, str(ROOT / "PIK分析包/元数据"))
    from offline_sources import PEImage
    image_path = Path("D:/Normal_Software/HoYoPlay/games/ZenlessZoneZero Game/GameAssembly.dll")
    verification = read_json(META / "verification.json")
    expected = next(s["sha256"] for s in verification["sources"] if Path(s["path"]).name == "GameAssembly.dll")
    actual = digest(image_path)
    if actual != expected:
        raise ValueError("GameAssembly 与元数据来源构建不一致")
    image = PEImage(image_path)
    selected = {("MoleMole.CameraModuleAvatarDataConfigExt", "Init"),
                ("MoleMole.CameraModuleAvatarDataConfig", "InnerInit"),
                ("MoleMole.Config.ConfigCameraZoom", ".ctor"),
                ("MoleMole.Config.ConfigCameraStretch", ".ctor"),
                ("MoleMole.Config.ConfigCameraShake", ".ctor"),
                ("MoleMole.Config.AnimatorZone", "GetSegmentNameByAnimatorInfo")}
    result = []
    for method in methods:
        if (method["owner"], method["method"]) not in selected:
            continue
        record = dict(method)
        try:
            evidence = image.disassemble_function(int(method["RVA"], 16))
            name = method["owner"].split(".")[-1] + "_" + method["method"].replace(".", "") + "_" + method["RVA"]
            write_json(ANALYSIS / "native" / (name + ".json"), evidence)
            lines = [f"{address:08X}  {mnemonic:10s} {operand}" for address, mnemonic, operand in evidence["instructions"]]
            (ANALYSIS / "native" / (name + ".asm")).write_text("\n".join(lines) + "\n", encoding="utf-8")
            record.update(status="decoded", file="native/" + name + ".asm", begin=evidence["begin"], end=evidence["end"], byteSha256=evidence["byte_sha256"])
        except ValueError as error:
            record.update(status="unavailable", reason=str(error))
        result.append(record)
    write_json(ANALYSIS / "native/index.json", {"source": str(image_path), "sourceSha256": actual, "methods": result})
    boundary = image.disassemble_function(0x13A3D160)
    write_json(ANALYSIS / "native/WindowRange_13A3D160.json", boundary)
    (ANALYSIS / "native/WindowRange_13A3D160.asm").write_text("\n".join(
        f"{a:08X} {m:10s} {o}" for a, m, o in boundary["instructions"]) + "\n", encoding="utf-8")
    return result


def main():
    changes, windows = variant_differences()
    methods = metadata_evidence()
    native = native_evidence(methods)
    write_json(ANALYSIS / "analysis-summary.json", {"eventVariantChanges": len(changes), "windowVariants": windows,
               "nativeFunctions": [{"method": n["method"], "owner": n["owner"], "status": n["status"]} for n in native]})
    render_notes()
    print(json.dumps({"eventVariantChanges": len(changes), "nativeDecoded": sum(n["status"] == "decoded" for n in native)}, ensure_ascii=False))


def render_notes():
    (ANALYSIS / "时间区域规则.md").write_text("""# 时间区域规则：本轮新增证据

原函数是 `MoleMole.Config.AnimatorZone.GetSegmentNameByAnimatorInfo`，RVA `0x12A45970`。已核对 GameAssembly 与 829 元数据使用同一 SHA-256。

## 已确认的代码行为

- 入口读取实参 `stateFrameCount` 和 `stateNormalizedTime`；前者为 0 时直接返回 false。
- `0x12A45B10` 判断配置对象的 `0x4E1` 布尔字段。元数据中 `ConfigMisc.IsAnimatorZoneUseFrame` 对应这个偏移；本轮尚未读取运行实例中该开关的实际值。
- 开关为 false 的分支在 `0x12A45B80` 直接读取区域的 `StartNormalizedTime` 和 `EndNormalizedTime`。
- 开关为 true 的一般分支在 `0x12A45BB6`、`0x12A45BD0` 将 `StartFrame`、`EndFrame` 转成浮点并除以传入的 `stateFrameCount`。
- `AnimatorStateTimeSegment.FrameCount` 位于区域对象 `0x28`。上述换算分支没有用它作分母，所以不能把资源中的这个缓存字段直接当成运行窗口分母。
- MaxStartFrame、MaxEndFrame 和零帧有专门分支，不应把 EndFrame=0 统一解释成窗口立即结束。
- 边界判断委托给 `0x13A3D160`，该函数包含端点和浮点误差处理。此轮保留完整指令，未把它简化成普通闭区间。

## 对复刻的直接影响

同一份资源同时保存帧边界和归一化边界，两者不同不能直接判定资源坏了。应先确认全局开关、调用时传入的真实状态帧数，再决定使用哪组边界。输入转场中的比较模式 9 是另一条判断链，不能从本函数推导其含义。

## 可核对来源

- [完整区域函数指令](native/AnimatorZone_GetSegmentNameByAnimatorInfo_0x12A45970.asm)
- [边界辅助函数指令](native/WindowRange_13A3D160.asm)
- [区域字段与类型](camera-field-metadata.json)
- [模块哈希和函数范围](native/index.json)

函数中还存在运行时重定向分支；这里描述的是当前磁盘函数的原生分支，未宣称已验证活动实例的最终执行路径。
""", encoding="utf-8")
    (ANALYSIS / "README.md").write_text("""# Corin 补缺分析

## 本轮已经补出的内容

| 项目 | 本轮结果 | 阅读入口 |
| --- | --- | --- |
| 公共镜头曲线 | 6 条全部取得关键帧、切线、权重和边界模式；三个资源变体中这 6 条数值一致 | [曲线数值与图](公共曲线.md) |
| 基础镜头 | 4 组角色镜头配置、36 组球面/轨道配置已解码；包含真实 FOV、偏移、阻尼和内嵌曲线 | [基础镜头](基础镜头.md) |
| 公共镜头资源 | 扫描 10,399 个 block，0 个扫描错误；精确导出 27 个原始对象，0 个导出错误 | [定位与身份](camera-resource-search/export-plan.json) |
| 原生曲线格式 | AnimationCurveLibrary 空 Odin 之后的原生列表已完整解码，三个文件分别 307、298、306 条曲线 | [原生曲线清单](camera-resource-search/native-curves/manifest.json) |
| Odin 简化输出 | 修正 Vector/Quaternion/Color 未命名分量、内嵌曲线被丢弃的问题；原字节不变，阅读包已重建 | [解码器改动](camera-resource-search/odin-positional-fields.diff) |
| 时间区域采样 | 已读取实际函数：帧分支以调用参数 stateFrameCount 换算，另有归一化分支 | [时间区域规则](时间区域规则.md) |
| 同名资源差异 | 战斗事件按稳定 guid 对账后实际有 3 项增删；区域配置也有变体差异 | [资源变体](资源变体.md) |

## 关键纠正

`Camera_ShakeDecay_Curve_04` 的两个关键帧均为 1，切线均为 0，实际是一条常量曲线，不能因为名字带 Decay 就套衰减公式。`Camera_ShakeSpatial_Curve_01` 的横轴为 0、2、4、10，不能一律压成 0～1 秒。

`CameraCutscenes` 已找到真正的 MonoBehaviour 配置对象，PathID 为 `7884625566397287362`；此前部分同名导出是 MonoScript，PathID 不同。新原始对象保存在 raw-camera 中，但其正文是原生序列化列表，空 Odin 并不代表没有镜头数据。

`CameraShakes_Common` 是通用震动效果集合，并不包含 `CamShake_A_01` 等六个标准模板键的定义。不能因为名字是 Common 就把它算成标准配置已补齐。

## 尚未完成的项目

| 缺口 | 已有入口 | 下一步要确认的事实 |
| --- | --- | --- |
| 六个 CamShake 标准配置键 | 全部引用和同版本资源扫描结果已保留 | 定位模板定义，或从消费者证明 StandardConfigKey 是否只供作者阶段使用；不能先假设运行时覆盖 |
| CameraCutscenes / CameraLockDatas | 9 个原始对象已按 MonoBehaviour 身份取得 | 解析 Unity 原生列表、Shot 参数及其资源引用 |
| 基础镜头的原生尾部 | 三个 Pipeline_Camera_Avatar_Config 原始对象 | 解析 Odin 字典以外的普通目标/Boss 锁定等字段 |
| Corin 的实际镜头组和变体 | Default_Normal 等 4 组值已读，来源差异已对账 | 沿角色加载与状态选择函数确认实际使用键和资源版本 |
| 枚举与特殊值 | 原值、枚举成员名、字段布局和方法地址已整理 | 展开常量或读消费者，确认 -1、叠加类型、FOV 变化类型、帧条件模式 9 |
| 镜头执行过程 | 触发、结束事件、配置和有限函数体 | 确认时间缩放、重入/打断、效果叠加顺序与最终 Cinemachine 输出 |
| 输入缓冲和长按 | 转场条件、事件触发键、区域数据 | 找到提前缓存时长、清除/消费时点以及长按/连点生成条件 |

以上没有通过近似值补齐。原始数据、已确认静态行为和待确认运行规则保持分别标注；切人仍不进入本轮重点动作页。
""", encoding="utf-8")


if __name__ == "__main__":
    main()
