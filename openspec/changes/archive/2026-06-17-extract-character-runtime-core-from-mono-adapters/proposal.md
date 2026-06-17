# Change: 抽出纯 C# Character Runtime Core，收薄 Mono Adapter

## Why
当前角色主线已经收口到 `CharacterFrameRuntimeController` 和 `CharacterFramePipeline`，但正式运行时状态、生命周期和部分 host 仍散落在 `PlayerLocomotionController`、`FullBodyActionRuntime` 等 MonoBehaviour 上。结果是 prefab 上组件职责偏重、测试需要 Unity 对象、rollback/replay 只能围绕 Mono 拼装，后续扩展 Action、Locomotion、网络同步和回滚都会继续把逻辑堆回 Mono。

本变更规划将正式角色运行时抽成纯 C# `CharacterRuntimeCore` 或批准的等价核心，由 MonoBehaviour 只负责显式引用、Unity 生命周期桥接和表现侧 adapter 绑定。

## What Changes
- 新增纯 C# 角色运行时核心作为正式 runtime owner，持有或组合 `CharacterFrameRuntimeHost`、正式 runtime port、Locomotion runtime、Action runtime、snapshot/restore 和诊断状态。
- 将 `CharacterFrameRuntimeController` 降级为 Unity 入口 adapter：读取显式序列化引用、创建/持有一个 core、把 Unity tick/input/config 转交给 core。
- 将 `PlayerLocomotionController` 降级为 Movement/Locomotion Unity adapter 或兼容 facade；正式 Locomotion state store、runtime blackboard、frame runtime host 和 output runtime host MUST 迁出 Mono owner。
- 将 `FullBodyActionRuntime` 降级为 Action Unity adapter 或兼容 facade；正式 `CharacterStateMachineRuntime`、`ActionLifecycleRuntime`、Action request/lifecycle/output runtime MUST 迁出 Mono owner。
- 保持 `CharacterFramePipeline` 是唯一角色帧主线；不得新增第二 pipeline、第二 runner、第二 motion executor、第二 animation presenter 或 fallback 配置。
- 与 `separate-rollback-debug-rig-from-character-runtime` 协作：Rollback Debug Rig 仍由该变更负责拆分；本变更只要求 debug/replay 通过显式目标引用复用同一个 `CharacterRuntimeCore`。
- 保持现有 Move、Run、TurnBack、Dodge 行为语义，不借本变更调整输入或动画规则。

## Impact
- Affected specs: `character-runtime-ports`, `character-frame-pipeline`, `wasd-locomotion-pipeline`, `fullbody-action-framework`
- Affected code:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Pipeline/Runtime/CharacterFrameRuntimeController.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Pipeline/Runtime/CharacterFrameRuntimePortAdapter.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Pipeline/Runtime/CharacterFramePipelineHost.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Runtime/LocomotionFrameRuntime.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Runtime/LocomotionOutputRuntime.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/Runtime/FullBodyActionRuntime.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/Runtime/FullBodyOutputRuntimeHost.cs`
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/`
- Dependencies:
  - Must coordinate with `formalize-character-frame-module-architecture` so this change does not duplicate submitter graph、frame plan 或角色配置根职责。
  - Must coordinate with `separate-rollback-debug-rig-from-character-runtime` so rollback debug tooling moves off official prefab there, while replay targets the formal runtime core here.

## Validation
实施阶段 MUST 使用 Unity EditMode 自动测试覆盖：
- 纯 C# core 可在无 GameObject/MonoBehaviour 的 fixture 中构造、tick、capture 和 restore。
- `CharacterFrameRuntimeController` 只持有一个 core 并委托 tick，不直接创建第二 pipeline 或第二 module runtime。
- `PlayerLocomotionController` 和 `FullBodyActionRuntime` 不再作为正式 runtime state owner。
- `CharacterFramePipeline` phase 顺序、Move/Run/TurnBack/Dodge 现有自动测试保持通过。
- 静态边界测试阻止正式路径引入 fallback 扫描、第二 runner、第二 motion executor、第二 animation presenter。

用户侧验证建议：在 Sandbox/CameraTest 中确认点按 Shift 后冲刺进入 Run，停止后经 RunEnd 回到 Walk/Idle；无输入后撤 Dodge 播完整动画回 Idle；TurnBack 被 Dodge 打断后不再残留 TurnBack 位移曲线。该手动验证不写入 `tasks.md`。
