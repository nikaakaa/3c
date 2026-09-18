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

- [x] 4.1 角色动画表现时钟保留两种时间域：`PresentationDelta` 按渲染 delta 自走，`CommittedMovement` 读取逻辑 locomotion 进度；`CommittedMovementPlaybackClock` 只作为逻辑事实，不新增第三时钟
- [x] 4.2 `ActionCommittedSampleHistory`/`ActionPresentationSampleProjector`/`ActionAnimationPlaybackLifecycleRegistry` 保留为 `CommittedFollow` 通用能力；回放/观战的带 channel Action 可按 committed 采样插值，普通 locomotion 不消费这条历史链
- [x] 4.3 表现时钟策略合同 `IActionPresentationClockPolicy` 统一封装 `DriveClock`：`FreeRunPresentationClockPolicy`、`CommittedMovementPresentationClockPolicy`、`CommittedFollowPresentationClockPolicy`
- [x] 4.4 `CharacterPoseNativeClipPlayerHandler.PrepareFrame` 只调用 `m_ClockPolicy.DriveClock(...)`，播放器不包含模式分支；策略同时接收正式 `CharacterPresentationFactFrame`，从中读取 Committed Movement clock
- [x] 4.5 `CharacterPresentationDomainRuntimeFactory` 按 `CharacterClipPlayerClockSource` 选择 FreeRun/CommittedMovement；SimulatedActor 额外装配 CommittedFollow coordinator，供 channel-bound Action 使用并接通 Registry 命令喂入

## 5. 编辑器吸附

- [x] 5.1 `BtsmtlSlateTimelineBinding.SnapTime` 量化粒度改读会话 tick 步长（`1/tickRate`），帧吸附显示与拖拽手感保持

## 6. 缩放字段清理

- [x] 6.1 删除 `TimelineData.m_Scale`、`Scale` 属性及 `TimelineContentClosure` 指纹传递链

## 7. Track / Clip 执行域与表现 TreeClip 事件

- [x] 7.1 为 Timeline Track / Clip authoring 合同增加 `Logic`、`Presentation` 与 `DualProjection` 执行域；历史资产缺少字段时固定解释为 `Logic`，保持 Model-neutral
- [x] 7.2 保持 Runtime 直接遍历正式只读 Timeline 内容；按当前 Advance / Present 形成 Logic Evaluation 与 Presentation Evaluation 结果分区，不生成 Semantic operation 或常驻操作表
- [x] 7.3 新增 PresentationFrame 驱动的表现游标与 evaluation 路径，不改变现有 Logic Tick 推进和 Commit / Discard 协议
- [x] 7.4 Marker 重构为与 Clip 同级的点触发实体：TimelineData 内容模型（Track 持有 Marker 列表）、内容闭包与指纹纳入；废弃 clip 子列表与 Pulse/Stateful 区间模型（含拆除已落码的对应实现）
- [x] 7.5 将 DualProjection TreeClip 的既有 AssetTree 固定为 Logic 投影，并把同一 Clip 的 Marker 输出分流到 Presentation Evaluation；AssetTree 不得在 PresentationFrame 重复执行
- [x] 7.7 Presentation 输出接入正式下游域：Marker 事件补齐 EventId，相机 State / Cue / Response / Resource 经稳定 EventId 交给 Camera domain 调和；表现动画继续走 ActionPlaybackCommandInbox；不写 Gameplay fact，也不为未建正式 domain 的特效 / 音效伪造命令
- [x] 7.8 Marker 触发图合同：仅暴露 OnEnable 回调的触发图引用与编译；Logic 域经 Advance / Commit 确定性触发，Presentation 域经表现游标跨点触发
- [x] 7.9 Marker 作者 UI：Slate 时间轴轨上 Marker 点的创建、绘制、拖拽与选中，触发图引用编辑
- [x] 7.6 Presentation Marker 事件 identity 定义为 playback handle、generation、marker 与 traversal index：同一次经过只交付一次、循环重触发换新 index、停止或 generation 变化后旧 generation 不再触发

## 8. 编辑器 MVC 与当前消费边界

- [x] 8.1 Timeline 顶栏拆成 `TimelineEditorBindingState` 只读模型、`TimelineEditorToolbarView` 视图和 `TimelineEditorWindow` controller；按钮按文档导航、Workspace 模式、运行状态分组
- [x] 8.2 登记 Corin `AttackProperty` 消费边界：主控转成 Timeline Marker / Ability 打击帧，Timeline runtime 与编辑器只消费正式内容，不新增私有解析器或第二运行链- [x] 8.3 收口 Corin Attack ActionCue 合同：ActionCue 只在 Logic commit 后发布 `CueType`/`CueId` 领域事件，Timeline 不解析 `AttackProperty`，不代行 Camera/VFX/Audio 消费
