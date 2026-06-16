## Context
当前默认状态机资产已经表达为 `FullBody / Locomotion / Action` 层级结构，运行时 `CharacterStateMachineRunner` 以叶子状态路径推进状态。问题不在状态树模型，而在运行时所有权：`PlayerLocomotionController` 和 `PlayerFullBodyActionController` 都能创建 runner，`LocomotionTickAdapter` 和 `FullBodyActionTickAdapter` 都能成为 tick driver，配置解析还允许旧字段或代码默认值参与。

## Goals
- 让当前角色正式 gameplay 路径只有一个 `CharacterStateMachineRunner`。
- 让 FullBody pipeline 成为状态推进、owner 选择、运动输出和 base layer 动画输出的唯一正式入口。
- 让 Locomotion 保持为 FullBody 下的 adapter/module，而不是平级 runtime owner。
- 让缺失正式配置变成可诊断错误，而不是旧字段 fallback 或代码默认值继续运行。
- 保持现有 Locomotion/Dodge/TurnBack 行为语义和已有日志可观测性。

## Non-Goals
- 不引入完整 HFSM active stack、父级 enter/exit 冒泡、父节点输出继承或并行层。
- 不删除现有诊断日志。
- 不新增第二套角色控制器、第二套运动执行器或绕过 `MotionExecutor` 的运动路径。
- 不在本变更中重命名动画资产或清理测试命名 asset，除非它直接阻塞正式配置校验。
- 不运行 Unity batchmode。

## Decisions
- Decision: `PlayerFullBodyActionController` 是唯一状态机 runner owner。
  - Reason: FullBody pipeline 已经拥有输入缓冲、Action 请求、Locomotion facts、状态机推进、运动输出、动画输出和 snapshot 写入的固定顺序；把 runner 放在这里可以让 owner、状态时间、variant 和 pending transition 只有一个来源。
- Decision: `PlayerLocomotionController` 不再创建 runner。
  - Reason: Locomotion 的职责是生成纯数据 facts 并执行当前 owner 允许的运动/动画输出。它仍可暴露被 FullBody pipeline 调用的方法，但不得自行决定状态流转。
- Decision: `LocomotionTickAdapter` 从正式当前角色装配中退役。
  - Reason: 双 adapter 冲突检测是迁移保护，不是长期架构。正式 simulation tick 应驱动 FullBody pipeline，然后由 pipeline 决定本帧是否提交 Locomotion。
- Decision: 配置缺失时报错并停止相关帧，不使用运行时 fallback。
  - Reason: 项目规则要求正式配置，不允许未审批 fallback。旧字段和 `DodgeActionConfig.Default` 会隐藏装配错误，并让手感参数来源不可追踪。
- Decision: 保留旧序列化字段到迁移结束，但不作为运行时解析来源。
  - Reason: 直接删除字段可能造成资产数据丢失；先让字段成为只读迁移遗留，并用测试确认正式路径不读取它们。

## Risks / Trade-offs
- Risk: 旧场景只挂了 `PlayerLocomotionController` 或 `LocomotionTickAdapter` 后会停止运行。
  - Mitigation: 实施任务必须包含 Sandbox/Prefab 装配迁移，并给旧入口输出明确诊断。
- Risk: 测试中仍有直接调用 `PlayerLocomotionController.TryEvaluateLocomotion` 的用例。
  - Mitigation: 测试分两类处理：纯数据 helper 测试改用显式 runner 参数或测试构造器；runtime 行为测试改走 `PlayerFullBodyActionController.Tick`。
- Risk: `character-config-root` 当前正式 spec 仍描述 fallback 兼容。
  - Mitigation: 本变更明确修改该 spec，兼容目标从“继续 fallback 运行”改为“序列化数据不丢失，但运行时报错并要求迁移正式配置”。
- Risk: 活跃变更 `refactor-fullbody-frame-pipeline` 仍把双 driver 当迁移态。
  - Mitigation: 本变更依赖其 phase pipeline 产物，并把后续收尾目标推进为正式唯一 driver。

## Migration Plan
1. 增加静态测试锁定 runner owner、tick driver 和配置来源。
2. 让 FullBody controller 从正式 `CharacterConfigSO`/子配置解析状态机与动作配置。
3. 移除 Locomotion controller 内部 runner 创建和自驱状态机入口。
4. 迁移 runtime tests 和 Sandbox/Prefab 装配到 FullBody tick driver。
5. 将旧字段 fallback 测试改成缺配置诊断测试。
6. 最后删除未调用的 FullBody 旧输出方法，确保没有第二套输出顺序残留。

## Open Questions
- 旧平铺字段在本变更中是否只标记 `[Obsolete]` 并保留序列化，还是在资产迁移确认后删除字段和 `.meta` 影响？
- `LocomotionTickAdapter` 是否立即从代码中删除，还是先保留为 Editor 迁移诊断组件但不允许参与正式场景？
