# Timeline 时钟域与 tick 率配置化

## Why

当前 Timeline 时间体系把三处"60"隐式焊死，tick 率实际不可配置，逻辑判定域与表现采样域职责无正式边界：

1. `TimelineUtility.FrameRate` 是 static 可变字段（默认 60），运行时评估、编辑器会话、内容闭包全部读它；
2. `FixedAbilityOperationControlRuntime.TickTimeline` 写死 `deltaFrames = 1`，即"1 个逻辑 tick 恒等于 1 个 timeline 帧"，tick 率偏离 60 时播放速度与 cue/TreeClip 判定时刻全部错位；
3. `GameplayAbilityAuthoringCompilationModel.TickRate` 直接返回 const 60，ability 编译产物时长换算与 pipeline 配置脱钩。

项目需要同时承载 Local、Prediction、Server Authority、Rollback 与 Replay 等 Network Model。逻辑域必须是固定 tick 整数域且按所选模型执行；表现域保持独立连续推进，默认消费表现 delta，并由角色表现域按 Network Model 选择自由播放、平滑纠正、硬切或严格跟随逻辑采样。`CommittedMovementPlaybackClock` 是逻辑时钟派生的 locomotion 进度事实，不是第三个时间域。tick 率收敛为 pipeline 正式配置（`CharacterPipelineDefinition.m_SimulationTickRate` 已有序列化口子；现行 `gameplay-tick-system` spec 已要求 tick 率来自正式配置），Timeline 域接入该配置。默认 60:60 下行为与现状一致，资产零迁移。

## What Changes

- 逻辑判定域收敛为整数 tick：clip enter/exit、cue、TreeClip、Tick lifetime 绑定的判定全部用整数 tick 比较；秒与归一化时间是派生读数，供采样、显示与表现插值使用。
- Timeline Track / Clip 增加显式执行域，区分 `Logic`、`Presentation` 与 `DualProjection`；`DualProjection` 是同一作者内容的双侧投影，不是第三个时钟，也不把具体 Network Model 或具体时钟实现写入 Clip。
- Timeline Runtime 继续直接遍历正式只读 Timeline 内容；每次逻辑或表现推进只形成对应域的 evaluation 输出，不把 Track / Clip 预编译成 Semantic operation 或常驻操作表。Logic 输出由 SimulationTick 推进，Presentation 输出由 PresentationFrame 推进。
- TreeClip 的 Gameplay 决策继续走逻辑链；表现侧触发改为与 Clip 同级的 Marker 点实体：单帧、稳定 MarkerId、可挂仅 OnEnable 回调的触发图；Marker 的域归属决定由 SimulationTick（Advance / Commit）或 PresentationFrame 推进。TreeClip 的图只属于其执行域，DualProjection 的图只在逻辑侧执行一次。
- Presentation Event 使用播放实例、generation、clip、marker 与循环经过序号组成稳定身份；表现游标重采样、分支替换或停止时按 keep / replace / cancel 调和，不再强制等待逻辑 Tick。
- `TimelineUtility.FrameRate` static 字段删除；运行时换算率从 pipeline tick 率配置注入，编辑器会话使用独立预览刻度。
- `TickTimeline` 的 `deltaFrames` 按 tick 率与 timeline 帧率比率整数累加换算，累加器余数进入 Timeline 回滚快照。
- `GameplayAbilityAuthoringCompilationModel.TickRate` 改读 pipeline 定义，编译产物携带 tick 率。
- 表现层保留 committed sample、selected sample 与自由表现三种可用输入形态；`ActionPresentationSampleProjector` 只在业务域选择严格跟随或回放时启用，并审计回滚重放时 committed 样本历史的替换路径。
- 编辑器 Slate 吸附粒度改读会话 tick 步长（`1/tickRate`）。
- `TimelineData.m_Scale` 无运行时语义残留字段删除（含 `TimelineContentClosure` 指纹传递链）。

调研证据（ZZZ 本体 dump、HoMiyabi 参考实现、UE 5.4 源码）与方案对比见 [design.md](design.md)。

## Capabilities

### New Capabilities

- `btsmtl-timeline-clock-domain`：Timeline 时钟域正式合同——tick 唯一权威、比率确定性推进与累加器快照、直接内容的双域 evaluation、与 Clip 同级的 Marker 点触发实体与事件生命周期、编辑器吸附粒度、Scale 残留清理。

### Modified Capabilities

- `btsmtl-timeline-direct-runtime`：保持直接消费正式内容的原则，同时明确双域 evaluation 只是当前推进的输出分区，不是 Semantic operation 或第二执行语言。
- `btsmtl-runnable-timeline-node`：既有 TimelineBody 图只属于 Logic TreeClip；Marker 改为与 Clip 同级的点触发实体，触发图仅暴露 OnEnable 回调，`TreeDecision` 退出只作用于 Logic 投影。
- `character-animation-pipeline`：补充 Timeline Track / Clip 执行域、PresentationFrame 事件输出与 Gameplay fact 隔离。

`gameplay-tick-system`、`character-presentation-interpolation` 与 `gameplay-network-model-boundary` 是本 change 的约束，不在本 change 内改写。Network Model 级 locomotion plan 由独立的 `add-network-model-locomotion-presentation-policy` change 收口。

## Impact

- 代码面：`TimelineUtility`、Track / Clip / Marker 执行域合同、同级 Marker 点实体、`FixedAbilityOperationControlRuntime`、`TimelineRuntimePreparation`（快照扩展累加器与逻辑 evaluation）、Timeline Presentation Runtime（表现游标、事件调和与消费）、`GameplayAbilityAuthoringCompilationModel`、`CharacterPipelineDefinition` 配置链路、`BtsmtlSlateTimelineBinding` 吸附、`ActionCommittedSampleHistory` 审计。
- 资产面：历史 Track / Clip 缺少执行域时有效值固定为 `Logic`，保持既有行为；新建 Presentation Marker 才产生新表现事件，不对历史资产做自动转换。
- 与现行 spec 对比：`gameplay-tick-system` 与 `character-presentation-interpolation` 条款一致不动；本 change 显式修改 direct runtime、runnable TreeClip 与 animation pipeline 三处合同，避免把双域输出误解为第二 Timeline runtime。`add-open-ended-treeclip-preview` 的 `TreeDecision` 退出合同只覆盖 Logic 投影，表现 Marker 不通过该回传通道结束。
