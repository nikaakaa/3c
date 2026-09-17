# Tasks

2026-09-17 决策登记：时钟域方案 A（事件驱动插值）经用户拍板；ZZZ dump / HoMiyabi / UE 源码调研结论见 design.md。本清单不含测试与验证任务。

## 1. 时钟注入

- [ ] 1.1 删除 `TimelineUtility.FrameRate` static 字段；运行时换算率经 Timeline 准备上下文注入，来源为 pipeline tick 率配置
- [ ] 1.2 编辑器 Slate 会话预览刻度改为独立配置（默认 60），不再读运行时全局
- [ ] 1.3 `TimelineContentClosure` 与各 `*.Runtime.cs` 派生换算改为消费注入刻度，清理全部 `TimelineUtility.FrameRate` 引用

## 2. 推进换算与回滚快照

- [ ] 2.1 `FixedAbilityOperationControlRuntime.TickTimeline` 的 `deltaFrames` 改为按 tick 率与 timeline 帧率比率的整数累加换算，移除写死 `1`
- [ ] 2.2 帧累加器余数作为确定性状态进入 `TimelineRuntimePreparation` 播放快照（Capture/Restore 对称）
- [ ] 2.3 timeline 帧率默认取 pipeline tick 率，60:60 时推进行为与现状逐 tick 一致

## 3. 编译层 tick 率

- [ ] 3.1 `GameplayAbilityAuthoringCompilationModel.TickRate` 改读 `CharacterPipelineDefinition.SimulationTickRate`，编译产物携带 tick 率
- [ ] 3.2 核对 `SimulationSessionHost` tick 率一致性校验与 `RollbackProtocolMessages` 握手 TickRate 校验覆盖新配置链路

## 4. 表现样本历史修正

- [ ] 4.1 审计 `ActionCommittedSampleHistory` 在回滚重放 tick 倒退样本流时的替换/清理路径，缺口按 `character-presentation-interpolation` 的整批更新方式补齐
- [ ] 4.2 修正瞬间表现从当前可见状态平滑接管，不重置动画时钟、不累计旧偏移

## 5. 编辑器吸附

- [ ] 5.1 `BtsmtlSlateTimelineBinding.SnapTime` 量化粒度改读会话 tick 步长（`1/tickRate`），帧吸附显示与拖拽手感保持

## 6. 缩放字段清理

- [ ] 6.1 删除 `TimelineData.m_Scale`、`Scale` 属性及 `TimelineContentClosure` 指纹传递链
