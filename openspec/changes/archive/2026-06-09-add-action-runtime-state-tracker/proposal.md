# Change: 新增 Action 运行时状态跟踪器

## Why

当前动作打断线已经有纯仲裁器和策略数据源雏形，但仲裁器需要的当前动作事实还没有统一来源。系统现在可以计算 `ActionInterruptDecision`，却还没有一个极小模块记录“当前 Action 是谁、过了多久、当前抗性是多少、当前 tick 是多少”。

本变更只规划一个 Action 运行时状态跟踪器。它不是完整行为状态机，不使用状态机库，不做状态图，不做自动退出，不接输入，不接动画，不接 Locomotion。它只是给仲裁器提供当前 Action 事实，并在 accepted decision 后更新当前 Action 状态。

## What Changes

- 新增 `action-runtime-state-tracker` 能力，用纯 C# 保存当前 Action 状态事实。
- 新增 `ActionRuntimeStateSnapshot` 或等价快照，包含 current state、elapsed seconds、current resistance、current tick。
- 新增 `ActionRuntimeStateTracker` 或等价 tracker，支持 reset、enter/set state、tick、create interrupt context、apply decision。
- tracker 默认处于 `Action.None` 或等价空 action state。
- tracker 可从当前事实生成现有 `ActionInterruptContext`，供 `ActionInterruptArbiter` 使用。
- tracker 可应用 accepted `ActionInterruptDecision`，进入 decision target state 并重置 elapsed seconds。
- tracker 对 rejected decision 不改变当前状态。
- tracker 的目标状态 resistance 由调用方显式传入或使用安全默认值，不在本变更里引入完整状态定义或 catalog。
- 增加 EditMode 测试和静态边界验证，证明 tracker 不依赖 Unity 场景对象、动画、输入、Locomotion 或 BBB 运行时类型。

## Non-Goals

- 不新增完整 Action 状态机。
- 不使用 UnityHFSM 或其他状态机库。
- 不新增 action transition graph、condition 或 priority。
- 不新增 `ActionStateDefinition`、`ActionStateCatalogSO` 或动作配置编辑器。
- 不实现自动退出、duration、return state 或 combo 流程。
- 不接真实输入，不消费 `InputRequestBuffer`。
- 不把按钮输入转换成 `ActionInterruptRequest`。
- 不接 `PlayerLocomotionController`，不修改 `Idle / MoveStart / MoveLoop / MoveStop`。
- 不播放动画，不接 Animancer、Animator、AnimationClip、TransitionAsset 或动画 alias。
- 不做 CharacterRoot 或黑板。
- 不接网络协议、预测或回滚。
- 不复制 BBB 的 `OverrideState`、`ActionController` 或 `BBBCharacterController` 管线。

## Impact

- Affected specs:
  - `action-runtime-state-tracker`
- Related active changes:
  - `add-action-interrupt-arbiter`
  - `add-action-interrupt-policy-data`
- Supersedes planning-only change:
  - `add-minimal-action-state-machine`
- Affected code planned:
  - `Assets/Scripts/Character/Action/Model/ActionRuntimeStateSnapshot.cs`
  - `Assets/Scripts/Character/Action/Solver/ActionRuntimeStateTracker.cs`
  - `Assets/Tests/Editor/ActionRuntimeStateTrackerTests.cs`

## Dependencies

- 本变更依赖现有 `ActionStateId`、`ActionInterruptContext`、`ActionInterruptDecision` 和 `ActionInterruptArbiter`。
- 本变更不依赖 `ActionInterruptPolicySetSO` 接入运行时；测试可直接构造 runtime policies。
- 如果 `add-action-interrupt-policy-data` 尚未完成，implementation 不能创建第二套 policy 数据源。

## Assumptions

- 第一版默认空 action state 使用 `Action.None`。
- 第一版 resistance 只由调用方传入 tracker，不从状态定义表解析。
- 第一版 elapsed seconds 只用 `float` 秒表达；预测回滚的 fixed-point/tick 映射以后单独规划。
