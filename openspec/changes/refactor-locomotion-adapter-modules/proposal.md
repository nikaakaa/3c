# Change: 拆分 Locomotion Adapter 职责

## Why
`PlayerLocomotionController` 已经从状态机 owner 退为 FullBody pipeline 下的 Locomotion adapter，但实现文件仍承载输入读取、空间事实、TurnBack、运动构建、动画采样、snapshot/restore、日志和迁移壳等多种职责。外部运行路径已经收口，内部模块边界还没有跟上，导致代码难读、难测试，也容易让后续改动误以为可以恢复 Locomotion 直驱路径。

## What Changes
- 将 `PlayerLocomotionController` 收窄为 Unity 装配和 FullBody pipeline facade。
- 将 Locomotion facts 构建、TurnBack intent、TurnBack motion facts、状态输出转基础移动帧、snapshot/restore 和诊断日志拆到明确模块。
- 保持现有 FullBody runtime authority：拆出的模块不得创建 `CharacterStateMachineRunner`，不得注册 tick driver， 不得提交第二份 motion 或 base layer animation。
- 保留现有日志 key 和必要错误诊断；除非用户明确批准，不删除 log。
- 先做静态边界和行为锁定测试，再按模块小步迁移。

## Folder Layout
本变更采用“两轴分法”：第一层按技术边界区分 Unity/数据/逻辑/日志，第二层只在 `Model/` 和 `Solver/` 下按功能域分组。目录最多两层，不再让所有 solver 平铺在一个目录里。

```text
Assets/Scripts/Character/Movement/
  Contracts/
  Runtime/
  Model/
    Facts/
    Motion/
    TurnBack/
    Snapshot/
  Solver/
    Facts/
    Motion/
    TurnBack/
    Snapshot/
  Diagnostics/
```

- `Runtime/` 只放 Unity MonoBehaviour、场景组件、生命周期和引用解析，例如 `PlayerLocomotionController`、退役迁移期的 `LocomotionTickAdapter`。
- `Model/Facts/` 放 Locomotion 给统一状态机消费的输入意图、空间事实、状态机输入事实和 frame 中转数据。若现有类型名仍包含 `Decision`，迁移期只表示“状态机判定前的 facts 聚合”，不得表示 Locomotion 自行决定状态。
- `Model/Motion/` 放基础移动输出、motion facts 中转模型或后续从 controller 拆出的运动结果数据。
- `Model/TurnBack/` 放 `LocomotionTurnBackIntent` 和 TurnBack 专用纯数据。
- `Model/Snapshot/` 只放 Movement 边界内的 snapshot/restore 数据模型；跨系统 `CharacterSimulationSnapshot` 仍留在 Simulation/Rollback 边界。
- `Solver/Facts/` 放 `LocomotionFactsBuilder` 或等价模块，负责把输入、空间事实和黑板 snapshot 组装成统一状态机 context 所需 facts。
- `Solver/Motion/` 放 `LocomotionStateMotionBuilder` 等状态输出到基础移动帧的转换逻辑。
- `Solver/TurnBack/` 放 `TurnBackIntentResolver`、`TurnBackMotionResolver` 等 TurnBack 专项逻辑。
- `Solver/Snapshot/` 放 `LocomotionSnapshotAdapter` 等 capture/restore 协作逻辑，但不重新定义动画播放进度权威。
- `Diagnostics/` 放日志提交模块，例如 `LocomotionDiagnostics`，只格式化和提交 `RuntimeDiagnosticLog`，不得计算状态、执行运动、播放动画或改写黑板。
- `Contracts/` 保留现有端口接口，例如输入源、运动执行器和动画播放进度接口。

如果某个功能域第一轮只有一个文件，仍按功能域落位，避免后续继续把不同职责塞回 `PlayerLocomotionController` 或平铺到 `Solver/` 根目录。

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
