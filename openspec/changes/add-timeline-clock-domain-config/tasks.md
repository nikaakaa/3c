# Tasks

2026-09-17 决策登记：时钟域方案 A（事件驱动插值）经用户拍板；ZZZ dump / HoMiyabi / UE 源码调研结论见 design.md。本清单不含测试与验证任务。

## 1. 时钟注入

- [x] 1.1 `TimelineUtility.FrameRate` static 可变字段改为固定常量（资产帧基准语义，60）；运行时与编辑器禁止任何可变全局帧率
- [x] 1.2 编辑器 Slate 会话预览刻度改为独立配置（默认 60），不再引用任何可变全局
- [x] 1.3 `TimelineContentClosure`、Runtime 评估与各 `*.Runtime.cs` 派生换算统一消费帧基准常量与闭包 FrameRate，清理 static 可变引用

## 2. 推进换算与回滚快照

- [x] 2.1 `FixedAbilityOperationControlRuntime.TickTimeline` 的 `deltaFrames` 改为按 tick 率与 timeline 帧率比率的整数累加换算，移除写死 `1`
- [x] 2.2 帧累加器余数作为确定性状态进入 `TimelineRuntimePreparation` 播放快照（Capture/Restore 对称）
- [x] 2.3 timeline 帧基准 60 等于默认 pipeline tick 率，60:60 时推进行为与现状逐 tick 一致；tick 率偏离时真实秒时长不变

## 3. 编译层 tick 率

- [x] 3.1 `GameplayAbilityAuthoringCompilationModel.TickRate` 改读 `CharacterPipelineDefinition.SimulationTickRate`，编译产物携带 tick 率
- [x] 3.2 核对 `SimulationSessionHost` tick 率一致性校验与 `RollbackProtocolMessages` 握手 TickRate 校验覆盖新配置链路

## 4. 表现样本历史修正

- [x] 4.1 角色动画表现时钟按 Clip 显式分责：`PresentationDelta` 按渲染 delta 自走，`CommittedMovement` 跟随正式 Committed Movement playback clock；两者都不把逻辑采样阶梯直接暴露给表现
- [x] 4.2 `ActionCommittedSampleHistory`/`ActionPresentationSampleProjector`/`ActionAnimationPlaybackLifecycleRegistry` 保留为 `CommittedFollow` 通用能力；回放/观战的带 channel Action 可按 committed 采样插值，普通 locomotion 不消费这条历史链
- [x] 4.3 表现时钟策略合同 `IActionPresentationClockPolicy` 统一封装 `DriveClock`：`FreeRunPresentationClockPolicy`、`CommittedMovementPresentationClockPolicy`、`CommittedFollowPresentationClockPolicy`
- [x] 4.4 `CharacterPoseNativeClipPlayerHandler.PrepareFrame` 只调用 `m_ClockPolicy.DriveClock(...)`，播放器不包含模式分支；策略同时接收正式 `CharacterPresentationFactFrame`，从中读取 Committed Movement clock
- [x] 4.5 `CharacterPresentationDomainRuntimeFactory` 按 `CharacterClipPlayerClockSource` 选择 FreeRun/CommittedMovement；SimulatedActor 额外装配 CommittedFollow coordinator，供 channel-bound Action 使用并接通 Registry 命令喂入

## 5. 编辑器吸附

- [x] 5.1 `BtsmtlSlateTimelineBinding.SnapTime` 量化粒度改读会话 tick 步长（`1/tickRate`），帧吸附显示与拖拽手感保持

## 6. 缩放字段清理

- [x] 6.1 删除 `TimelineData.m_Scale`、`Scale` 属性及 `TimelineContentClosure` 指纹传递链







