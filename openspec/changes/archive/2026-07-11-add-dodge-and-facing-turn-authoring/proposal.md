# Change: 增加闪避分层回收与朝向转身闭环

## Why

Corin 已有完整 `Left Shift -> Dodge request -> Action StateMachine DodgeForward/DodgeBack -> Timeline/root motion/IFrame -> Complete` 链路。Dodge state OnEnter/OnExit 也已经通过 pipeline blackboard 写入 `IsDodging`。当前缺口不是 Dodge 本身，而是 Locomotion StateMachine 没有读取 `IsDodging` 来暂停 locomotion 所有权，并在 Dodge 完成后明确进入 RunLoop 或 RunEnd。

当前 MovingTurn 比较当前 MoveAxis 与上一 logic tick MoveAxis 的差角。这个量只表示一个 tick 内输入变化，玩家渐进转向时很难达到阈值，不能稳定表达角色朝向与目标移动方向的真实偏差。

## What Changes

- 保持现有 InputAction、`Left Shift`、Dodge request、DodgeForward/DodgeBack Action state、ActionProfile、Timeline、root-motion curve 和 IFrame 配置不变。
- 在 Locomotion StateMachine 中新增一个无动画、无 motion 的 `ActionOverride` 所有权状态。它不复制 Dodge 业务，只表示当前 base animation/motion 由上层 Action 独占。
- 为各 Locomotion 状态增加高优先级 `IsDodging -> ActionOverride` 边。`IsDodging` 继续来自现有 pipeline blackboard ExposedProperty。
- ActionOverride 在 `IsDodging=false` 后按当前移动输入分流：有输入直接进入 `RunLoop`，无输入进入 `RunEnd`。
- 在 logic tick 开始、BTSMTL 决策前捕获角色平面朝向快照；新增通用期望移动方向与角色朝向夹角节点，并复用 locomotion motion 的 camera-relative 方向解析。
- 将 MovingTurn 正式条件迁移为“当前期望世界移动方向与 tick 起点角色朝向的夹角”，阈值继续由 ExposedProperty 调整。
- 扩展 Agent Patch compiler/emitter/validator，使 ActionOverride、blackboard bool 条件和 facing-angle 条件走同一正式资产链路；删除 Corin 正式链路中的旧 input-angle-delta 条件。

## Impact

- 受影响规格：
  - `character-action-authoring-closure`
  - `character-pipeline-runtime`
  - `character-state-timeline-authoring-loop`
- 受影响实现：
  - `CharacterGraphContext` tick 起点 actor pose fact
  - camera-relative locomotion 方向解析与 facing-angle 信息节点
  - Agent authoring patch compiler/emitter/validator
  - Corin RootTree inline Locomotion StateMachine
- 依赖：`add-state-interruption-authoring-closure` 已完成的 State source-exit、Timeline cancel 和 OnExit 生命周期，保证 Dodge 被打断时仍会清理 `IsDodging`。

## Non-Goals

- 不新增、重命名或改绑任何输入。
- 不新增第二个 Dodge state、Dodge Timeline、ActionProfile 或 motion 数据源。
- 不把 ActionOverride 做成伪 Dodge 动画状态，也不创建空 Timeline。
- 不实现连续闪避、耐力、冷却或 Action 与攻击的新增互斥策略。
- 不新增网络消息；现有 Dodge request 和 ActionInstance 链路保持不变。
- 不新增测试，不运行 Unity batchmode。
