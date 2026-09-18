# 开放时长 TreeClip 预览观察

## Why

Timeline 里的 Logic TreeClip 承载格挡、蓄力等待等由树内部逻辑决定时长的内容：树激活后等待输入或条件，条件满足才退出，实际时长在播放前不可知。当前模型里 Logic TreeClip 的 Enter/Exit 全部由 StartFrame/EndFrame 帧边界机械触发（`TimelineRuntimePreparation` 按 clip 区间边界产出请求），runtime 没有"树内部决定退出并回传 timeline"的通道——格挡想提前退退不了，蓄力想多等等不了，预览也无法如实显示这类 clip 的真实活跃区间。

归档 change `2026-09-17-restyle-timeline-editor-slate-style` 的 preview-integration-plan 已登记该缺口："运行时长未知 Clip、开放 Track 生命周期是另一个正式执行合同问题，不由本轮 UI 解耦或版本报告补成假延长"，明确划归 runtime 合同并禁止 UI 侧伪造。本轮按 grill 结论立项：预览载体为 ScenePlay 会话观察（现行 `btsmtl-timeline-editor-preview` spec 既定口径），变长内容为树决定退出的 Logic TreeClip 与循环等待类内容。

## What Changes

- **开放时长 Logic TreeClip 合同**：Logic TreeClip 增加显式退出来源标记（默认帧边界保持现状；标记为树决定退出的 clip，EndFrame 降级为最短持有时长，到点前不许退、到点后由树退出事件产生真实 Exit 边界）。Presentation TreeClip 与 DualProjection 的 Presentation Marker 不使用该退出来源。
- **树退出回传通道**：Logic TreeClip 的树侧新增“结束片段”节点；执行到该节点时向 Timeline runtime 请求退出，产生该 clip 的真实 Exit 边界并定型；timeline 在 Exit 后继续走后续内容。
- **循环等待类不设 clip 级退出**：循环等待输入/转场的内容（如蓄力循环）活跃至所属 playback 被外部停止/转场为止，不新增 clip 级独立退出判定——转场判定本属 ability program 播放级职责。
- **预览观察显示**：ScenePlay 预览观察中，开放时长 Logic TreeClip 的可视 End 跟随 Runtime 游标实时增长，树退出事件到达时定型；循环等待类同样跟随游标至 playback 停止定型。显示数据全部消费正式 Runtime 已发布事实，不新增 UI 私有时钟或采样。

## Capabilities

### Modified Capabilities

- `btsmtl-timeline-editor-preview`：新增开放时长 Logic TreeClip 与循环等待类内容在预览观察中的显示行为条款，以及支撑显示的退出边界语义。

### New Capabilities

无。树退出回传是预览定型依据的组成部分，随本 change 写入预览 spec delta；其实施归属 timeline runtime 域（见 Impact）。

## Impact

- 代码面：Logic Timeline runtime（退出来源标记、树退出回传通道、Exit 边界产生）、树侧“结束片段”节点与 program 编译执行链、ScenePlay 预览观察链（开放 clip 的可视 End 显示）。UI 不新增时钟、不伪造采样；Presentation Marker 生命周期由时钟域 change 的表现游标合同拥有。
- 前置依赖：时钟域 change（`add-timeline-clock-domain-config`）的策略合同与装配开关收尾；其实施提交 `8a508f68e`、`6104e6387` 已落码主体。
- 存量兼容：默认帧边界语义不变，未标记的 Logic TreeClip 与全部既有 clip 行为零变化；Corin 等存量资产无需迁移。
- 与现行 spec 对比：`btsmtl-timeline-editor-preview` 现行条款（ScenePlay 独占生命周期、UI 只读、消费正式 Runtime 事实、多实例 Pin/Follow）全部保持；本 change 只新增开放时长内容的显示与退出语义，不修改既有条款。`preview-integration-plan` 中"不做假延长"禁令继续有效——本 change 的变长显示依据是 runtime 真实退出事件，不是 UI 补长。
