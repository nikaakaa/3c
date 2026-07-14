## Context

当前 Action StateMachine 已有 `None -> DodgeBack/DodgeForward -> None`。DodgeForward 由 `Dodge request AND HasMove` 进入，DodgeBack 由 `Dodge request AND NoMove` 进入；`ActivateActionInstanceNode` 接受并消费 request。两个 state body 都在 OnEnter 设置 pipeline blackboard `IsDodging=true`，Root 播放正式 Dodge Timeline，OnExit 设置 `IsDodging=false` 并提交 Complete。

Locomotion 与 Action StateMachine 在 RootTree 中并行运行。Dodge Timeline 的 Action channel motion 会覆盖低层 Locomotion contribution，但 Locomotion 状态仍在后台推进，导致 Dodge 完成后的 locomotion 状态不是一个显式、可配置的回收决策。

MovingTurn 当前读取相邻 logic tick 的 MoveAxis 差角，描述输入变化速度而不是角色 facing error。

## Goals / Non-Goals

### Goals

- 复用现有 Dodge Action 闭环，不复制动作状态和资产。
- 让 Locomotion 在 Dodge 活跃期间显式交出 animation/motion 所有权。
- 让 Dodge 完成后有输入进入 RunLoop、无输入进入 RunEnd。
- 让转身条件读取 tick 起点稳定事实，并与实际 camera-relative locomotion 使用同一方向定义。
- 让资产修改通过 Agent authoring 正式链路幂等生成和校验。

### Non-Goals

- 不修改输入、Dodge ActionProfile、Timeline、root-motion curve 或 IFrame。
- 不让 Locomotion 再创建 Dodge state。
- 不让条件节点直接读取 Transform、Camera.main 或场景搜索结果。

## Decisions

### Decision: Action Dodge 保持唯一业务真相

Dodge request 的接受、ActionInstance、动画、motion、IFrame 和完成生命周期继续只属于 Action StateMachine 的 DodgeForward/DodgeBack。Locomotion 不消费 Dodge request，也不引用 Dodge Timeline。

业务取舍：动作事务、权威策略和 Timeline 事实只有一个 owner；Locomotion 只负责在上层动作独占期间停止产出。代价是两个状态机通过一个明确 blackboard ownership fact 协调，但这正是 pipeline blackboard 的业务用途。

### Decision: Locomotion 使用 ActionOverride 表达所有权让渡

ActionOverride 是 Locomotion StateMachine 的协调状态，没有动画、Timeline 和 motion body。各 locomotion state 在 `IsDodging=true` 时以高优先级进入 ActionOverride。它不能命名为 Dodge，避免复制动作状态真相。

业务取舍：调试时可以直接看到 Locomotion 已被 Action 接管，且不会继续提交低层 motion；代价是多一个协调状态和若干显式入边。使用每个 source 的明确边，不使用 AnyState 自跳转，避免 `IsDodging=true` 时 ActionOverride 每 tick 重入自身。

不在 RootTree 外层 abort Locomotion branch：branch 重启会丢失明确回收目标，并把状态生命周期变成树层隐式副作用。

### Decision: ActionOverride 退出只看 ownership fact 与 MoveAxis

当 `IsDodging=false`：MoveAxis 大于 stop threshold 直接进入 RunLoop，否则进入 RunEnd。离开不等待 StateRootCompleted，因为 ActionOverride 没有时序 body；完成时钟仍属于 Action Dodge Timeline。

业务取舍：Dodge 完成后的第一帧直接恢复持续跑或停止收尾，不重复播放 RunStart；这对应“闪避结束有输入就是 run”。无输入进入 RunEnd，保持现有 locomotion 停止表现。

### Decision: IsDodging 是 pipeline blackboard ownership fact

继续使用已有 `IsDodging` ExposedProperty 和 `PipelineBlackboardBoolInfoNode`。Dodge OnEnter 写 true，所有 source-exit 的 OnExit 写 false。Locomotion ConditionRuleGraph 只读取。

业务取舍：条件由现有节点、And/Not/Compare 和 ExposedProperty 拼接，不增加 Dodge 特化条件节点。该值是本地 pipeline 生命周期事实，不新增网络字段。

### Decision: MovingTurn 比较期望世界方向与 tick 起点角色朝向

Pipeline 在每个 logic tick 的 BTSMTL 前，从显式注入的 actor Transform 捕获平面 pose snapshot。`CharacterMoveFacingAngleInfoNode` 通过 PropertyPort 接收 MoveAxis，读取 camera basis 与 actor pose，输出无符号平面夹角。方向计算抽成 locomotion 共用解析器，Motion 与条件节点复用。

业务取舍：渐进转向也能在朝向偏差达到阈值时稳定触发。actor pose 是 pipeline 内部瞬态事实，不进入 blackboard或网络；只有 `MovingTurnAngleThreshold` 是可调 ExposedProperty。

## Risks / Trade-offs

- Action 和 Locomotion branch 的执行顺序会让 `IsDodging` 的获得/释放最多晚一个 logic tick 被另一个状态机观察，但整个 tick 内读取保持稳定，不引入同 tick 竞态旁路。
- ActionOverride 不提交 locomotion motion；Dodge Timeline 必须继续提供完整 Action channel motion。现有 DodgeForward/DodgeBack Timeline 已满足该条件。
- MovingTurn threshold 语义改变后，需要调整现有 ExposedProperty 初值。

## Migration Plan

1. 增加 actor pose snapshot、共享方向解析和 facing-angle 节点。
2. 扩展 Agent authoring 对 blackboard ownership 条件、ActionOverride 和 facing-angle 条件的编译与校验。
3. 用幂等 patch 修改 Corin inline Locomotion StateMachine，复用现有 `IsDodging`。
4. 删除旧 input-angle-delta 正式引用。
5. 编译、正常 Unity 导入并运行正式资产校验；不运行 Unity batchmode。

## Open Questions

无。现有 Dodge 动作资产与输入保持原样；ActionOverride 退出和 MovingTurn threshold 按本设计收口。
