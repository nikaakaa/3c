# Change: 退役 PlayerFullBodyActionController

## Why
`PlayerFullBodyActionController` 现在同时承载 Unity 装配、配置解析、状态机 runner、兼容 Tick、FullBody 输出依赖、诊断 view 和 rollback restore 入口。它的名字和职责都会继续诱导后续 Attack、Jump、UpperBody 或回滚逻辑把规则塞回 FullBody 大类，形成角色级管线之外的分裂路径。

## What Changes
- **BREAKING**：删除 `PlayerFullBodyActionController` 作为正式运行时组件、测试 fixture 类型和 prefab/scene 绑定。
- 将唯一角色帧入口保持在 `CharacterFrameRuntimeController` / `CharacterFrameRuntimeHost` / `CharacterFramePipeline`。
- 先拆分 `LocomotionFrameSubmitter` 与 `FullBodyActionFrameSubmitter` 的构建边界，删除共享 `FullBodySubmissionBuilder` 作为正式集成中心的语义。
- 将 frame output source 从 `LegacyFullBodyIntegrated` 收口到角色级候选/输出来源语义，避免旧 FullBody 集成路径继续作为权威身份。
- 将统一状态机 runner、snapshot、capture/restore 迁入 `CharacterStateMachineRuntime` 或等价状态机运行时模块。
- 将 Dodge/Attack/Jump 等 FullBody Action 请求配置、policy、resistance 和 resolved action facts 迁入 `FullBodyActionRuntime` 或等价窄模块。
- 将 `FullBodyOutputRuntimeHost` 从 controller 内部类迁出为独立 output dependencies host，不再依赖 controller 大操作面板。
- 将 runtime port 改为组合 Character、StateMachine、Locomotion、FullBody Action、Output、Diagnostics 等窄端口，不再包装 `PlayerFullBodyActionController`。
- 更新 FullBody rollback、simulation tick、runtime blackboard、Locomotion 单驱动和 prefab/scene 校验，使它们不再把旧 controller 当作权威入口。

## Impact
- Affected specs:
  - `character-frame-pipeline`
  - `character-runtime-ports`
  - `fullbody-action-framework`
  - `fullbody-rollback-replay`
  - `unified-character-state-machine`
  - `simulation-tick-locomotion`
  - `character-runtime-blackboard`
  - `locomotion-state-graph-config`
- Affected code:
- `Assets/Scripts/Character/Action/FullBody/Runtime/PlayerFullBodyActionController.cs`
- `Assets/Scripts/Character/Action/FullBody/Runtime/FullBodySubmissionBuilder.cs`
- `Assets/Scripts/Character/Action/FullBody/Runtime/FullBodyIntegratedFrameAdapter.cs`
- `Assets/Scripts/Character/Action/FullBody/Runtime/FullBodyRuntimePortAdapter.cs`
  - `Assets/Scripts/Character/Pipeline/Runtime/CharacterFrameRuntimeController.cs`
  - `Assets/Scripts/Character/Pipeline/Runtime/CharacterFrameRuntimePortAdapter.cs`
  - `Assets/Scripts/Simulation/Rollback/FullBodyRollbackSimulation.cs`
  - `Assets/Scripts/Simulation/Rollback/LocomotionSnapshotHistoryRecorder.cs`
  - Corin prefab/scene bindings and related EditMode tests
