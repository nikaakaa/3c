# Change: 增加 FullBody 分层 HFSM 状态树规划

## Why
当前 `add-fullbody-action-framework` 已把 Locomotion 和 Dodge 收束到单一 FullBody owner，但运行时对外仍主要暴露 `CurrentOwner`、Locomotion phase 和 Action tracker 等分散事实。设计者仍不能直接看到一条统一的角色主状态路径，例如 `/FullBody/Locomotion/MoveLoop` 或 `/FullBody/Action/Dodge`。

需要在现有 FullBody 框架之上规划一个显式 UnityHFSM 分层状态树：内部继续保持 Locomotion、Action module、仲裁器、动画 Presenter 和 motion executor 分离；对外则提供统一、可诊断、可测试的角色主状态视图。

## What Changes
- 新增 `fullbody-hfsm-state-tree` 能力，定义 FullBody 主行为域的显式分层 HFSM 状态树。
- 将现有 Locomotion 四阶段作为 `FullBody/Locomotion` 子树接入，而不是再新建第二套基础移动状态机。
- 将 `Action.Dodge` 作为第一条 `FullBody/Action` 子状态接入，继续复用现有 Action module、Action 仲裁和 tracker。
- 定义统一状态快照，暴露当前 owner、状态路径、Locomotion phase、Action state、状态时间和 pending transition 诊断信息。
- 定义 HFSM 与现有 coordinator 的边界：HFSM 负责状态路径和 transition 权威，coordinator 仍负责端口连接、命令提交和 Unity 组件引用解析。
- 增加自动测试、静态边界检查和手动验证任务，证明状态树可见性提升没有新增运动路径、动画路径或 BBB 运行时依赖。

## Impact
- Affected specs:
  - `fullbody-hfsm-state-tree`
  - 关联活跃变更 `add-fullbody-action-framework`
  - 关联现有 `unityhfsm-locomotion`
  - 关联现有 `action-interrupt-arbiter`
  - 关联现有 `action-runtime-state-tracker`
- Affected code after approval:
  - `Assets/Scripts/Character/Action`
  - `Assets/Scripts/Character/Movement`
  - `Assets/Tests/Editor`
  - `docs/agents/character-animation-state-roadmap.md`
- Not in scope:
  - 不新增 Roll、Jump、Attack、Hit、Death 等新动作。
  - 不调整 Dodge 动画手感、clip、fade、8 向动画或 motion profile。
  - 不把 Walk/Run 建模为逻辑状态。
  - 不替换 `BasicLocomotionStateMachine` 的现有四阶段规则。
  - 不复制 BBB 的 `BBBCharacterController`、`PlayerStateRegistry`、`PlayerBaseState` 或状态内部互跳风格。
  - 不实现网络同步、预测回滚或 Fantasy 协议修改。
