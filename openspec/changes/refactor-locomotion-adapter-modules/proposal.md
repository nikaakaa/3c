# Change: 拆分 Locomotion Adapter 职责

## Why
`PlayerLocomotionController` 已经从状态机 owner 退为 FullBody pipeline 下的 Locomotion adapter，但实现文件仍承载输入读取、空间事实、TurnBack、运动构建、动画采样、snapshot/restore、日志和迁移壳等多种职责。外部运行路径已经收口，内部模块边界还没有跟上，导致代码难读、难测试，也容易让后续改动误以为可以恢复 Locomotion 直驱路径。

## What Changes
- 将 `PlayerLocomotionController` 收窄为 Unity 装配和 FullBody pipeline facade。
- 将 Locomotion decision frame 构建、TurnBack intent、TurnBack motion facts、状态输出转基础移动帧、snapshot/restore 和诊断日志拆到明确模块。
- 保持现有 FullBody runtime authority：拆出的模块不得创建 `CharacterStateMachineRunner`，不得注册 tick driver， 不得提交第二份 motion 或 base layer animation。
- 保留现有日志 key 和必要错误诊断；除非用户明确批准，不删除 log。
- 先做静态边界和行为锁定测试，再按模块小步迁移。

## Folder Layout
本变更采用现有 `Assets/Scripts/Character/Movement/` 下的四层目录边界，不新建很深的功能目录：

```text
Assets/Scripts/Character/Movement/
  Contracts/
  Model/
  Solver/
  Runtime/
  Diagnostics/
```

- `Runtime/` 只放 Unity MonoBehaviour、场景组件、生命周期和引用解析，例如 `PlayerLocomotionController`、退役迁移期的 `LocomotionTickAdapter`。
- `Model/` 只放纯数据类型，例如 `LocomotionDecisionFrame`、`LocomotionDecisionFacts`、`LocomotionSpatialFacts`、`LocomotionStateDecisionFrame`、`LocomotionTurnBackIntent`。
- `Solver/` 放纯逻辑模块，例如 `LocomotionDecisionFrameBuilder`、`LocomotionStateMotionBuilder`、`TurnBackIntentResolver`、`TurnBackMotionResolver`、`LocomotionSnapshotAdapter`。
- `Diagnostics/` 放日志提交模块，例如 `LocomotionDiagnostics`，只格式化和提交 `RuntimeDiagnosticLog`，不得计算状态、执行运动、播放动画或改写黑板。
- `Contracts/` 保留现有端口接口，例如输入源、运动执行器和动画播放进度接口。

第一轮不新增 `Movement/Solver/TurnBack/` 子目录；只有当 TurnBack solver 文件继续增长到多个独立子模块时，再通过后续审批细分。

## Non-Goals
- 不改变 Idle、MoveStart、MoveLoop、MoveStop、TurnBack、Dodge 的状态机语义。
- 不改变 `FullBodyActionTickAdapter -> PlayerFullBodyActionController -> FullBodyFramePipeline` 的正式 tick 主线。
- 不重新定义动画播放进度或 rollback 权威；该范围归属 `formalize-animation-playback-rollback-authority`。
- 不新增完整 HFSM active stack、并行层或第二角色控制器。
- 不新增 fallback 配置入口，不恢复旧平铺字段 runtime 读取。
- 不运行 Unity batchmode。

## Impact
- Affected specs:
  - `wasd-locomotion-pipeline`
  - `unified-character-state-machine`
  - `runtime-diagnostic-logging`
- Affected code:
  - `Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `Assets/Scripts/Character/Movement/Runtime/LocomotionTickAdapter.cs`
  - `Assets/Scripts/Character/Movement/Model`
  - `Assets/Scripts/Character/Movement/Solver` 或等价纯逻辑模块目录
  - `Assets/Tests/Editor/UnifiedCharacterStateMachineTests.cs`
  - `Assets/Tests/Editor/CharacterConfigRootTests.cs`
  - `Assets/Tests/Editor/Simulation`
- Related active changes:
  - `refactor-state-machine-runtime-authority` 定义当前唯一 runner owner 和 FullBody tick 主线；本变更依赖该方向，不覆盖它。
  - `formalize-animation-playback-rollback-authority` 定义动画播放进度 rollback 权威；本变更只移动代码边界，不改变其语义。
