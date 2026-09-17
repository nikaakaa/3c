# Timeline 时钟域与 tick 率配置化

## Why

当前 Timeline 时间体系把三处"60"隐式焊死，tick 率实际不可配置，逻辑判定域与表现采样域职责无正式边界：

1. `TimelineUtility.FrameRate` 是 static 可变字段（默认 60），运行时评估、编辑器会话、内容闭包全部读它；
2. `FixedAbilityOperationControlRuntime.TickTimeline` 写死 `deltaFrames = 1`，即"1 个逻辑 tick 恒等于 1 个 timeline 帧"，tick 率偏离 60 时播放速度与 cue/TreeClip 判定时刻全部错位；
3. `GameplayAbilityAuthoringCompilationModel.TickRate` 直接返回 const 60，ability 编译产物时长换算与 pipeline 配置脱钩。

项目是帧同步（确定性回滚）项目，逻辑域必须是固定 tick 整数域且可回滚；表现层确认不回滚，只消费 committed 事件流并连续插值。tick 率收敛为 pipeline 正式配置（`CharacterPipelineDefinition.m_SimulationTickRate` 已有序列化口子；现行 `gameplay-tick-system` spec 已要求 tick 率来自正式配置），Timeline 域接入该配置。默认 60:60 下行为与现状一致，资产零迁移。

## What Changes

- 逻辑判定域收敛为整数 tick：clip enter/exit、cue、TreeClip、Tick lifetime 绑定的判定全部用整数 tick 比较；秒与归一化时间是派生读数，供采样、显示与表现插值使用。
- `TimelineUtility.FrameRate` static 字段删除；运行时换算率从 pipeline tick 率配置注入，编辑器会话使用独立预览刻度。
- `TickTimeline` 的 `deltaFrames` 按 tick 率与 timeline 帧率比率整数累加换算，累加器余数进入 Timeline 回滚快照。
- `GameplayAbilityAuthoringCompilationModel.TickRate` 改读 pipeline 定义，编译产物携带 tick 率。
- 表现层维持 committed 事件流 + `ActionPresentationSampleProjector` 连续插值；审计并修正回滚重放时 committed 样本历史的替换路径。
- 编辑器 Slate 吸附粒度改读会话 tick 步长（`1/tickRate`）。
- `TimelineData.m_Scale` 无运行时语义残留字段删除（含 `TimelineContentClosure` 指纹传递链）。

调研证据（ZZZ 本体 dump、HoMiyabi 参考实现、UE 5.4 源码）与方案对比见 [design.md](design.md)。

## Capabilities

### New Capabilities

- `btsmtl-timeline-clock-domain`：Timeline 时钟域正式合同——tick 唯一权威、比率确定性推进与累加器快照、表现 committed 插值消费、编辑器吸附粒度、Scale 残留清理。

### Modified Capabilities

无。`gameplay-tick-system` 已有"tick 率来自正式配置"条款，Timeline 是其下游消费者；`character-presentation-interpolation` 已有"表现连续状态不回滚"条款，Timeline 动画贡献遵守它。

## Impact

- 代码面：`TimelineUtility`、`FixedAbilityOperationControlRuntime`、`TimelineRuntimePreparation`（快照扩展累加器）、`GameplayAbilityAuthoringCompilationModel`、`CharacterPipelineDefinition` 配置链路、`BtsmtlSlateTimelineBinding` 吸附、`ActionCommittedSampleHistory` 审计。
- 资产面：零迁移。Timeline 资产保持帧存储（与 ZZZ 本体、Unity Timeline 内部形态同构），默认 tick 率 60 下全部行为不变。
- 与现行 spec 对比：`gameplay-tick-system` 与 `character-presentation-interpolation` 条款一致不动；本 change 与 `restyle-timeline-editor-slate-style` 归档后的 `btsmtl-timeline-direct-runtime` 现行 spec 中"Timeline 唯一时间 owner 负责帧/秒/Tick"措辞由本 capability 细化为"tick 权威 + 秒读数"，无冲突；`btsmtl-runnable-timeline-node` 播放隔离与停止语义不受影响。
