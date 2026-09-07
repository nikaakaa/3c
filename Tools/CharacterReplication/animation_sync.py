import re


def collect(controller):
    states = []
    automatic = []
    movement = []
    trees = []
    for machine in controller["StateMachines"]:
        for state in machine["States"]:
            identity = f"sm{machine['Index']}:state{state['Index']}"
            states.append({"id": identity, "name": state["Name"], **{k: state[k] for k in (
                "m_TimeParamID", "TimeParameter", "m_CycleOffset", "CycleOffsetParameter", "m_Speed", "SpeedParameter")}})
            for transition in state["Transitions"]:
                record = {"sourceStateId": identity, "sourceState": state["Name"], "data": transition}
                if transition["m_AutoTransitionOffset"]:
                    automatic.append(record)
                if re.match(r"^(Walk_|Run_|TurnBack)", state["Name"]):
                    movement.append(record)
            for tree in state["BlendTrees"]:
                for node in tree["Nodes"]:
                    if len(node.get("m_ChildIndices", [])) > 1:
                        trees.append({"stateId": identity, "state": state["Name"], "treeIndex": tree["Index"], "node": node})
    return {"schema": "character-animation-sync/1", "states": states, "layers": controller["Layers"],
            "multiChildNodes": trees, "automaticOffsetTransitions": automatic, "movementTransitions": movement,
            "explicitSyncGroupRecovered": False,
            "scope": "当前主控制器的已导出字段；未发现显式 SyncGroup 不等于排除引擎内部同步",
            "counts": {"states": len(states), "boundTimeParameters": sum(s["m_TimeParamID"] != 0 for s in states),
                       "nonzeroCycleOffsets": sum(s["m_CycleOffset"] != 0 for s in states),
                       "boundCycleParameters": sum(bool(s["CycleOffsetParameter"]) for s in states),
                       "multiChildNodes": len(trees), "automaticOffsetTransitions": len(automatic)}}


def render(out, character, data, table, conditions):
    count_labels = {"states": "全部状态", "boundTimeParameters": "绑定时间参数的状态", "nonzeroCycleOffsets": "非零状态循环偏移",
                    "boundCycleParameters": "绑定循环偏移参数的状态", "multiChildNodes": "多子节点 BlendTree", "automaticOffsetTransitions": "自动切入转场"}
    lines = [f"# {character} 动画同步与切入位置", "",
             "区分三件事：转场配置指定从哪里进入、运行代码指定从哪里播放、多个动画是否按相位或标记同步。前两者已有证据，第三者尚未解出完整规则。", "",
             "## 状态参数", "", table(["检查项", "数量"], [(count_labels[k], v) for k, v in data["counts"].items()]), "",
             "`m_TimeParamID=0` 表示当前导出状态没有绑定时间参数；控制器存在名为 NormalizedTime 的参数，本身不代表状态由该参数驱动。零循环偏移也不排除代码调用指定播放位置。", "",
             "## 层与动画集合", "", table(["层", "状态机", "MotionSet", "SyncedLayerAffectsTiming"],
             [(l["Index"], l["m_StateMachineIndex"], l["m_StateMachineMotionSetIndex"], l["m_SyncedLayerAffectsTiming"]) for l in data["layers"]]), "",
             "共享状态机、不同 MotionSet 的层关系保留在这里。SyncedLayerAffectsTiming 不能直接换成跨动画 SyncGroup，也没有提供脚步标记或主从选择规则。", "",
             "## 有多个子节点的 BlendTree", "", table(["状态", "树", "节点", "BlendType", "参数", "1D 阈值", "子节点"],
             [(t["state"], t["treeIndex"], n["Index"], n["m_BlendType"], n["BlendEvent"], n["Blend1dThresholds"], n["m_ChildIndices"])
              for t in data["multiChildNodes"] for n in [t["node"]]]), "",
             "统计只包括当前控制器实际存在的多子节点，不从角色移动方式猜测 BlendTree。", "",
             "## 启用了自动偏移的转场", "", table(["源状态", "目标", "偏移", "偏移帧", "自动值", "自动比例"],
             [(t["sourceState"], t["data"]["DestinationStateName"], t["data"]["m_TransitionOffset"], t["data"]["m_TransitionOffsetCount"],
               t["data"]["m_AutoTransitionOffsetValue"], t["data"]["m_AutoTransitionOffsetRatio"]) for t in data["automaticOffsetTransitions"]]), "",
             "这些是文件原值。自动值、比例、源/目标帧数如何参与最终切入时间，仍需该版本 UnityPlayer 的消费者；不能仅凭比例推成同相位。", "",
             "## 移动转场原始参数", "", table(["源状态", "原序号", "目标", "混合时长", "固定时长", "使用帧数", "目标偏移", "目标偏移帧", "自动偏移", "条件"],
             [(t["sourceState"], d["Index"], d["DestinationStateName"], d["m_TransitionDuration"], d["m_HasFixedDuration"],
               d["m_UseFrameCount"], d["m_TransitionOffset"], d["m_TransitionOffsetCount"], d["m_AutoTransitionOffset"], conditions(d))
              for t in data["movementTransitions"] for d in [t["data"]]]), "",
             "保留帧模式和固定时长开关，不把原帧字段擅自换算成秒。完整退出条件、中断规则和源/目标总帧数见 JSON。", "",
             "## 运行代码与待补证据", "",
             "- [三名角色共用的本机代码分析](../../character_replication/analysis/animation-sync/README.md)",
             "- [全部状态同步字段及完整转场](data/animation-sync.json)",
             "- [控制器精确来源](data/source-index.json)", ""]
    (out / "动画同步.md").write_text("\n".join(lines), encoding="utf-8")
