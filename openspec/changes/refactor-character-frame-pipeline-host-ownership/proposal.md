# Change: 收敛 CharacterFramePipeline 持有关系

## Why
当前 `PlayerFullBodyActionController` 和 `FullBodyActionTickAdapter` 都会直接创建 `CharacterFramePipeline`，同时 `CharacterFramePipeline` 内部直接创建 `FullBodySubmissionBuilder`。这让 FullBody 提交者和 tick adapter 仍然像管线 owner，后续预测、回滚、synctest 容易产生 live 一条、replay 一条、phase tick 一条的分裂路径。

本变更要把持有关系收敛到一个纯 C# `CharacterFramePipelineHost` Module，使 MonoBehaviour 只做 Unity Adapter，FullBody/Locomotion/Action 只做 request 或 frame output 提交者。

## What Changes
- 新增纯 C# `CharacterFramePipelineHost` 作为每个角色唯一的角色帧运行时持有者。
- `PlayerFullBodyActionController` 或等价 MonoBehaviour 只持有 host，不再直接创建或持有 `CharacterFramePipeline`。
- `FullBodyActionTickAdapter` 复用同一个 host 的逐 phase 入口，不再创建自己的 `CharacterFramePipeline`。
- `CharacterFramePipeline` 通过角色帧 request/output submitter Interface 调用提交者，不再直接创建 FullBody 具体实现。
- request submission 与 frame output submission 在 Interface 层保持拆分。
- FullBody 生产实现作为 submitter Adapter 接入 host，不重新拥有 pipeline phase。
- FullBody replay、synctest 和后续本地高延迟校正复用同一个 host -> pipeline 链路。

## Impact
- Affected specs:
  - `character-runtime-ports`
  - `fullbody-rollback-replay`
- Affected code:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Pipeline/Runtime/CharacterFramePipeline.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Pipeline/Contracts/...`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/FullBody/Runtime/PlayerFullBodyActionController.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/FullBody/Runtime/FullBodyActionTickAdapter.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/FullBody/Runtime/FullBodySubmissionBuilder.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/FullBody/Runtime/FullBodyRuntimePortAdapter.cs`
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/UnifiedCharacterStateMachineTests.cs`
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/Simulation/FullBodyRollbackReplayTests.cs`
