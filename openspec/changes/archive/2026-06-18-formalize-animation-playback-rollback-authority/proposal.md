# Change: 正式化动画驱动运动采样窗口回滚边界

## Why

TurnBack 使用 `TickSampledMotion` profile 后，动画 normalized time 和 previous/current 采样窗口已经直接影响角色 root position/yaw。当前 F6 synctest 暴露出 replay 从 TurnBack 中段恢复后，播放进度可能被表现层当作首次进入重新归零，导致 profile 采样窗口从历史中段变成动画开头，后续 position/yaw 和 runtime blackboard 发散。

这个问题不是单个 TurnBack 补丁，也不是要求所有动画都变成确定性 simulation 状态。真正需要明确的是业务可选边界：表现层动画可以保持非确定播放；只有当某个状态或动作声明用动画时间驱动 motion facts、root motion profile、warp window、命中窗口或等价逻辑输出时，该采样窗口才必须由纯数据状态捕获、恢复和推进，表现层只能跟随，不能在 restore/replay 中覆盖。

## What Changes

- 将动画播放分成两类：纯表现播放、声明为 animation-driven sampled motion 的逻辑采样播放。
- 对业务声明使用 `TickSampledMotion`、root motion profile、Motion Warping playback window 或等价采样模式的状态/动作，playback window 和 previous/current sampling window 必须作为 simulation 可恢复状态处理。
- 对纯表现动画，系统 MAY 不捕获 normalized time，也不要求回滚后视觉逐帧确定，只要求不反向影响 simulation 输出。
- 基础移动 Animancer Presenter 的 `RestorePlaybackProgress` 只在 sampled motion 需要恢复时表示 resume 到指定进度；后续同 alias `Present` 不得把该恢复段当作首次进入归零。
- TurnBack 作为第一条验收业务：one-shot restart 只允许发生在真实状态进入或 alias 新播放时，不允许覆盖 sampled motion restore 的中段进度。
- F6/F8 严格工具必须能发现 sampled motion playback window 或 profile delta 分叉，并用于验收 TurnBack 中段 restore replay 收敛。
- 不新增第二套 movement path，不让 `OnAnimatorMove` pending delta 回到 simulation motion source。

## Non-Goals

- 不改变当前 `character-frame-rollback-replay` 和 `animation-motion-source-pipeline` 规格中的 EntryLocal 坐标空间语义。
- 不重新设计 `TickSampledMotion` / `AnimatorDirect` 的模式选择。
- 不要求所有动画 normalized time、blend 权重、表现事件或纯视觉播放都纳入 rollback state。
- 不把 Animator runtime root delta 作为预测回滚权威。
- 不新增完整网络预测、服务端校正或 Fantasy 协议字段。
- 不删除现有 TurnBack / rollback 诊断日志。
- 不运行 Unity batchmode。

## Impact

- Affected specs:
  - `animation-phase-timeline-facts`
  - `basic-locomotion-animation`
  - `character-frame-rollback-replay`
  - `wasd-locomotion-pipeline`
- Affected code:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Animation/Runtime/BasicLocomotionAnimancerPresenter.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Pipeline/Runtime/CharacterRuntimeCore.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Pipeline/Runtime/CharacterFrameRuntimeController.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Runtime/LocomotionRuntimeModule.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Simulation/Rollback/LocomotionRuntimeRollbackState.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Simulation/Rollback/CharacterSimulationSnapshot.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Simulation/Rollback/CharacterFrameRollbackSimulation.cs`
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/Simulation`
- Current spec boundaries:
  - `animation-motion-source-pipeline` 定义 TickSampledMotion 作为回滚友好的动画运动源。
  - `character-frame-rollback-replay` 定义 TurnBack profile translation 的 EntryLocal 空间和正式 replay 入口。
  - `local-rollback-synctest-foundation` 提供严格 F6/F8 工具验收和 first mismatch 诊断。
