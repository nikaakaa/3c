# Change: 角色运行时端口化与大类收口

## Why
当前 `CharacterFramePipeline` 已经成为唯一角色帧管线，但它和 `FullBodySubmissionBuilder` 仍直接依赖 `PlayerFullBodyActionController`，再通过它访问 `PlayerLocomotionController`。这让两个 MonoBehaviour 继续承担 host、adapter、配置解析、状态机 owner、输出副作用和诊断入口等多重职责，后续新增 Attack、Jump、UpperBody 或回滚验证时容易继续把规则塞回大类。

本变更规划把这些大类降级为 Unity runtime host / adapter，并在 Character frame pipeline、FullBody submission、Locomotion runtime 之间建立窄 Interface seam。目标是提高 Module depth 和 locality：管线只知道端口契约，Unity 对象、引用解析和具体副作用留在 adapter 实现里。

当前混杂点按优先级确认如下：
- `PlayerLocomotionController` 同时承载旧 direct tick、frame builder facade、rollback snapshot、diagnostic、reference resolve、camera/facing resolve、animation playback clock 和 motion executor adapter，是最高优先级。
- `PlayerFullBodyActionController` 已退成大 adapter，但仍同时承载 Unity tick host、pipeline 操作面板、runner rebuild、reference resolve 和 interrupt policy cache，是第二优先级。
- `CharacterFramePipelineTypes` 正在变成角色帧总线对象，但当前只有 FullBody，先记录为后续模型拆分风险。
- `CharacterStateMachineTypes` 混合通用状态图 model 和角色 FullBody/Locomotion/Action 业务词，属于后续 model 口径收敛，不纳入本次行为拆分。

## What Changes
- 新增 `character-runtime-ports` 能力，定义角色帧管线、FullBody 提交者和 Locomotion runtime adapter 之间的端口契约。
- 将 `CharacterFramePipeline` 和 `FullBodySubmissionBuilder` 从直接依赖 `PlayerFullBodyActionController` 迁移为依赖角色运行时端口或等价窄接口。
- 通过 `FullBodyRuntimePortAdapter` 或等价包装 adapter 将 `PlayerFullBodyActionController` 收窄为 Unity host、配置/runner owner 和端口装配者，不让 controller 直接成为 pipeline 的宽 Interface。
- 将 `PlayerLocomotionController` 收窄为 Locomotion runtime adapter，第一阶段拆出 prepare/build 与 output/apply 两类端口；不恢复 Locomotion 自驱状态机或第二 pipeline。
- 明确本变更优先收口行为混杂类，不进行 `CharacterFramePipelineTypes` 或 `CharacterStateMachineTypes` 的大规模模型文件拆分。
- 明确 `CharacterFramePipelineTypes` 和 `CharacterStateMachineTypes` 需要后续独立 model 拆分 change，本变更只记录风险和防止继续膨胀。
- 保留现有 `LocomotionFrameBuilder`、`CharacterFramePipeline`、统一状态机 runner、motion executor 和 Animancer presenter 权威，不引入 fallback 配置或第二控制路径。
- 增加 EditMode 自动测试和静态边界测试，证明 pipeline 可通过测试端口运行，且正式 runtime 不再从核心管线直接触碰具体 MonoBehaviour 大类。

## Impact
- Affected specs:
  - `character-runtime-ports`
  - `fullbody-action-framework`
  - `simulation-tick-locomotion`
  - `unified-character-state-machine`
- Affected code:
  - `Assets/Scripts/Character/Pipeline/Runtime/CharacterFramePipeline.cs`
  - `Assets/Scripts/Character/Pipeline/Contracts/ICharacterFrameRuntimePort.cs`
  - `Assets/Scripts/Character/Action/FullBody/Runtime/FullBodySubmissionBuilder.cs`
  - `Assets/Scripts/Character/Action/FullBody/Runtime/PlayerFullBodyActionController.cs`
  - `Assets/Scripts/Character/Action/FullBody/Runtime/FullBodyActionTickAdapter.cs`
  - `Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `Assets/Scripts/Character/Movement/Solver/LocomotionFrameBuilder.cs`
  - `Assets/Scripts/Character/Movement/Contracts`
  - `Assets/Tests/Editor/UnifiedCharacterStateMachineTests.cs`
  - `Assets/Tests/Editor/Simulation/FullBodyRollbackReplayTests.cs`
- Coordination:
  - 依赖 `refactor-character-frame-submission-pipeline` 完成唯一 `CharacterFramePipeline` 和 `CharacterFrameSubmission` 口径。
  - 与 `refactor-locomotion-frame-pipeline-mainline` 协作：该变更负责 Locomotion 纯数据 builder，本变更负责 builder 外围 runtime port 和大 MonoBehaviour 收口。
  - 不抢 `formalize-animation-playback-rollback-authority` 的 playback restore/window 语义。

## Validation
- `openspec validate refactor-character-runtime-ports --strict --no-interactive`
- Unity EditMode 定向测试：角色帧管线端口 fake、FullBody submission、Locomotion runtime port、Dodge/TurnBack/MoveLoop 回归。
- 静态边界测试：`CharacterFramePipeline`、`FullBodySubmissionBuilder` 不直接引用 `PlayerFullBodyActionController` / `PlayerLocomotionController`；端口契约不引用 `MonoBehaviour`、`Transform`、`CharacterController`、Animancer runtime 或 InputAction。
