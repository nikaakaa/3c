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

- [x] 4.1 结论（2026-09-17）：当前角色动画业务的形态确认为表现独立时钟——动画表现按渲染 delta 平滑推进、不消费逻辑采样、不回滚；该形态是业务选择之一，见 4.2
- [x] 4.2 `ActionCommittedSampleHistory`/`ActionPresentationSampleProjector`/`ActionAnimationPlaybackLifecycleRegistry` 保留为"跟随逻辑时间轴"模式的通用能力组件；当前角色动画业务采用独立时钟模式，不采用该组件；组件依赖的 `AbilityTimelineRuntimeSnapshot.FrameCarry` 字段已随本轮快照改造就位
- [ ] 4.3 新增表现时钟策略合同 `IActionPresentationClockPolicy`（DriveClock 封装时钟行为）：`FreeRunPresentationClockPolicy`（按渲染 delta 自走，当前角色动画装配用）与 `CommittedFollowPresentationClockPolicy`（组合 Registry+History+Projector，committed 采样间插值驱动，回放/观战等业务用）
- [ ] 4.4 `CharacterPoseNativeClipPlayerHandler.PrepareFrame` 消费点接入策略调用（`m_ClockPolicy.DriveClock(...)` 一行替换现有 `m_Player.Advance` 自走），播放器零分支；非 timeline 播放器装配 FreeRun 或不挂策略，行为不变
- [ ] 4.5 装配开关：`CharacterPresentationDomainRuntimeFactory` 按业务注入策略实例——角色动画注入 FreeRun（行为与现状逐帧一致），回放/观战等需要表现跟随逻辑的业务注入 CommittedFollow 并接通 Registry 命令喂入

## 5. 编辑器吸附

- [x] 5.1 `BtsmtlSlateTimelineBinding.SnapTime` 量化粒度改读会话 tick 步长（`1/tickRate`），帧吸附显示与拖拽手感保持

## 6. 缩放字段清理

- [x] 6.1 删除 `TimelineData.m_Scale`、`Scale` 属性及 `TimelineContentClosure` 指纹传递链







