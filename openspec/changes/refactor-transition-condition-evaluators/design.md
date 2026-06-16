## Context
当前条件枚举已经混合了三类语义：通用状态图条件、Locomotion 条件和 Action/Animation 条件。中心 evaluator 因此必须理解多域 facts，也直接变成新增动作时最容易被修改的文件。

## Goals
- runner 不再知道业务条件实现。
- condition key 到 evaluator 的映射可校验、可测试。
- evaluator 只读取纯数据 facts。
- 现有条件迁移后行为保持。
- 后续新增轻攻击、跳跃、受击时新增 evaluator adapter 或 facts，而不是修改 runner。

## Non-Goals
- 不做运行时热插拔插件。
- 不让 ScriptableObject 直接承载 C# 回调。
- 不让 evaluator 读取 Unity 场景对象。
- 不改变 transition priority 选边语义。

## Decisions
### Decision: Condition key 是配置接口
transition condition 数据中保留稳定 key、数值参数和枚举参数。配置不直接保存 evaluator 对象。

### Decision: Evaluator collection 是运行时接口
runner 通过 evaluator collection 解析 condition key。collection 由正式代码装配，不从场景对象动态扫描。

### Decision: 内置 evaluator 先覆盖现有条件
`HasMoveIntent`、`NoMoveIntent`、`StateCanExit`、`HasInputRequest`、`StateElapsedAtLeast` 可先归为 core evaluator；`MoveTurnBackRequested` 归 Locomotion evaluator；`LocomotionAnimationCanExit` 归 Locomotion/Animation evaluator；`ActionCanExit` 归 Action/Animation evaluator。

### Decision: 缺失 evaluator 是配置错误
状态机启动或校验时必须发现 condition key 没有 evaluator，不允许运行时静默返回 false。

### Decision: Evaluator 不提交诊断日志
evaluator 返回 result 和 trace。日志由外围 diagnostics adapter 提交。

## Interface Shape
### ConditionDefinition
- Owner: 状态机配置。
- Contains: condition key、参数、比较值、请求类型、tag 或等价纯数据。
- Does not contain: C# callback、MonoBehaviour、ScriptableObject evaluator 引用。

### ConditionEvaluationContext
- Owner: runner 调用 evaluator 时构造。
- Contains: 当前 snapshot、current timeline facts、runtime blackboard facts、accepted input fact、state time、current step。
- Does not contain: Animancer state、Transform、CharacterController、InputAction。

### ConditionEvaluatorAdapter
- Owner: domain 模块。
- Responsibility: 判断自己支持的 condition key，并返回 result + trace。
- Invariant: 不提交日志，不改变状态，不消费输入。

### ConditionEvaluatorCollection
- Owner: runner 装配入口。
- Responsibility: 根据 key 找 evaluator，检测缺失和重复。
- Invariant: 缺失 evaluator 是配置错误，不是 false。

## Adapter Groups
- Core evaluator: `HasMoveIntent`、`NoMoveIntent`、`StateCanExit`、`HasInputRequest`、`StateElapsedAtLeast`。
- Locomotion evaluator: `MoveTurnBackRequested` 和后续 Locomotion 专属 facts。
- Animation evaluator: `LocomotionAnimationCanExit`、播放进度匹配、自然退出 facts。
- Action evaluator: `ActionCanExit` 和后续 Action 恢复/取消 facts。

## Rejected Alternatives
### Alternative: 保留中心 switch，只把方法拆小
拒绝原因：中心 switch 仍是新增业务条件的修改点，无法提供 Locality。新增攻击或受击还是要改同一个核心文件。

### Alternative: 运行时任意注册插件
拒绝原因：当前项目需要审批、配置可见和确定性回放。任意运行时代码注册会让测试和回滚难以约束。

### Alternative: condition 直接引用 ScriptableObject evaluator
拒绝原因：配置会持有行为实现引用，容易把 Unity 对象和编辑器资产带进运行时状态机核心。

## Test Surface
- 状态机配置校验是缺失/重复 evaluator 的测试入口。
- runner transition selection 是 adapter collection 调用顺序测试入口。
- diagnostics adapter 是 condition trace 输出测试入口。
- 静态测试必须覆盖 runner 和 evaluator adapter 的禁止依赖。

## Risks / Trade-offs
- Risk: 只有一套默认 evaluator collection 时 seam 可能偏浅。
  - Mitigation: 第一版仍保持内部 adapter，但通过轻攻击/跳跃/受击后续需求验证真实变化点。
- Risk: condition key 与旧 enum 并存导致混乱。
  - Mitigation: 先做兼容映射和静态测试，完成迁移后再废弃中心业务分支。
- Risk: evaluator collection 过度泛化。
  - Mitigation: 不做任意回调，不做动态插件，只做项目内正式 adapter。

## Migration Plan
1. 增加现有条件 behavior characterization 测试。
2. 定义 condition evaluator result 和 trace。
3. 定义 evaluator collection。
4. 将 core 条件迁入 core evaluator。
5. 将 TurnBack 条件迁入 Locomotion evaluator。
6. 将 animation can exit 条件迁入 animation facts evaluator。
7. 将 Action can exit 条件迁入 Action evaluator。
8. 修改 runner 只调用 collection。
9. 将日志提交迁到 diagnostics adapter。
10. 添加静态测试防止中心 evaluator 新增业务分支。
