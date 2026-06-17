# Change: 正式化动画播放进度回滚权威

## Why

TurnBack 使用 `TickSampledMotion` profile 后，动画 normalized time 和 previous/current 采样窗口已经直接影响角色 root position/yaw。当前 F6 synctest 暴露出 replay 从 TurnBack 中段恢复后，播放进度可能被表现层当作首次进入重新归零，导致 profile 采样窗口从历史中段变成动画开头，后续 position/yaw 和 runtime blackboard 发散。

这个问题不是单个 TurnBack 补丁，而是动画驱动运动进入预测回滚前必须明确的权威边界：会影响 simulation 输出的动画播放时钟必须由纯数据状态捕获、恢复和推进，表现层只能跟随，不能在 restore/replay 中覆盖。

## What Changes

- 将“首次进入动画状态”和“从 rollback snapshot 恢复动画状态”定义为两种不同语义。
- 对需要预测/回滚的 `TickSampledMotion` 状态，动画播放进度和 profile 采样窗口必须作为 simulation 可恢复状态处理。
- 基础移动 Animancer Presenter 的 `RestorePlaybackProgress` 必须表示 resume 到指定进度；后续同 alias `Present` 不得把该状态当作首次进入归零。
- TurnBack 的 one-shot restart 只允许发生在真实状态进入或 alias 新播放时，不允许覆盖 rollback restore 的中段进度。
- F6/F8 严格工具必须能发现播放进度、采样窗口或 profile delta 分叉，并用于验收 TurnBack 中段 restore replay 收敛。
- 不新增第二套 movement path，不让 `OnAnimatorMove` pending delta 回到 simulation motion source。

## Non-Goals

- 不改变当前 `fullbody-rollback-replay` 和 `animation-motion-source-pipeline` 规格中的 EntryLocal 坐标空间语义。
- 不重新设计 `TickSampledMotion` / `AnimatorDirect` 的模式选择。
- 不把 Animator runtime root delta 作为预测回滚权威。
- 不新增完整网络预测、服务端校正或 Fantasy 协议字段。
- 不删除现有 TurnBack / rollback 诊断日志。
- 不运行 Unity batchmode。

## Impact

- Affected specs:
  - `animation-phase-timeline-facts`
  - `basic-locomotion-animation`
  - `fullbody-rollback-replay`
- Affected code:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Animation/Runtime/BasicLocomotionAnimancerPresenter.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Pipeline/Runtime/CharacterRuntimeCore.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Pipeline/Runtime/CharacterFrameRuntimeController.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Runtime/LocomotionRuntimeModule.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Simulation/Rollback/LocomotionRuntimeRollbackState.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Simulation/Rollback/CharacterSimulationSnapshot.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Simulation/Rollback/FullBodyRollbackSimulation.cs`
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/Simulation`
- Current spec boundaries:
  - `animation-motion-source-pipeline` 定义 TickSampledMotion 作为回滚友好的动画运动源。
  - `fullbody-rollback-replay` 定义 TurnBack profile translation 的 EntryLocal 空间和正式 replay 入口。
  - `local-rollback-synctest-foundation` 提供严格 F6/F8 工具验收和 first mismatch 诊断。
