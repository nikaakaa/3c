# Design: 角色运行时黑板

## Context
当前项目已经从 BBB 参考架构中拆出了更强的纯数据边界：输入、移动意图、状态机 context、动画播放进度、动作仲裁和 movement facts 分别存在。这个方向利于测试和后续预测回滚，但随着脚步相位、方向起步、转身、转角动画加入，跨帧事实会越来越多。

如果直接引入一个全局 `RuntimeData`，短期会方便，但会让输入、状态机、动画 Presenter、动作模块和运动执行端口都能互相读写，重新产生隐式耦合。黑板需要存在，但必须是 typed facts blackboard，而不是任意模块可变的大对象。

## Goals
- 集中承载角色运行时纯数据 facts，减少跨模块字段散落。
- 保留统一状态机作为逻辑状态权威。
- 保留运动执行端口作为位移权威。
- 保留 Presenter 作为动画播放和只读进度来源。
- 支持 snapshot/restore，服务本地预测、回放和同步测试。
- 为脚步相位、方向角、转身/转角动画选择提供稳定扩展点。

## Non-Goals
- 不在本变更直接实现脚步相位推断。
- 不在本变更直接实现方向起步、原地转身或跑动转角状态。
- 不引入 BBB 的 `PlayerRuntimeData` 或依赖 `Ref/BBB-Nexus` 运行时类型。
- 不改变基础移动位移权威。
- 不让黑板保存 Unity 对象或 Animancer runtime state。
- 不让黑板成为第二套状态机。

## Decisions
- Decision: 黑板由多个 typed facts 组成，而不是 `object` 字典。
  - Reason: 编译期类型能约束写入和读取，避免字符串 key 漂移。
- Decision: 每类 facts 必须声明写入权威。
  - Reason: 黑板最大的风险是“谁都能写”。写入权威让诊断和测试能定位来源。
- Decision: 状态机读取黑板快照，不直接维护黑板字段。
  - Reason: 状态机 runner 应保持纯数据求值，不变成聚合点。
- Decision: Presenter 通过只读 adapter 提供动画 facts。
  - Reason: Presenter 可以报告播放进度，但不应直接决定逻辑状态或移动。
- Decision: 黑板 snapshot/restore 必须是纯数据。
  - Reason: 后续 prediction/rollback 不能依赖场景实例引用。

## Risks / Trade-offs
- Risk: 黑板变成全局可变大对象。
  - Mitigation: spec 明确禁止 Unity 对象、Animancer 对象和任意模块写入；任务中加入静态边界测试。
- Risk: 初期多一层数据结构，感觉比直接字段更重。
  - Mitigation: 第一版只迁移必要的运行时 facts，不一次性搬空所有 frame/context。
- Risk: 写入权威划分不清导致实现阶段争议。
  - Mitigation: tasks 先建立 facts 分类和 owner 表，再接入代码。

## Migration Plan
1. 建立黑板纯数据模型和 snapshot。
2. 将现有跨帧 facts 中最容易膨胀的部分接入黑板，例如 last moving gait、MoveStop entry gait、action exit facts。
3. 让 `CharacterStateMachineContext` 读取黑板快照中的只读 facts。
4. 保留现有 frame/context 字段，逐步过渡，避免一次性大重构。
5. 后续脚步相位、方向角、转身/转角能力以独立 proposal 接入黑板。

## Open Questions
- 第一版是否只接入 locomotion/action/animation facts，还是同时预留 upper body facts 命名空间？
- 黑板运行时组件是否挂在当前角色聚合点同对象，还是由 FullBody controller 内部持有纯 C# 实例？
