# 设计：开放时长 TreeClip 预览观察

## grill 定案（2026-09-17）

- 预览载体：ScenePlay 会话观察（现行 `btsmtl-timeline-editor-preview` spec 口径），不引入编辑器本地即时预览；`CharacterTimelineHost.Update(deltaTime)` 的 preview handle 路径是遗留代码，不作为本 change 载体。
- 变长内容两类：树决定退出的 TreeClip（格挡、蓄力等待）；循环等待类（活跃至 playback 停止，无 clip 级退出）。
- 退出来源分类：`FrameBoundary`（现状默认）/ `TreeDecision`（clip 级，树退出事件，timeline 继续）/ `PlaybackBound`（播放级，随 playback 停止定型）。循环等待类归 `PlaybackBound`，不硬拆 clip 级退出——转场判定属 ability program 播放级职责，拆到 clip 级会让 program 与 timeline 互相等待。
- 可视化：可视 End 跟随 Runtime 游标、退出定型，视觉区分从简（不要求特殊样式）。

## runtime 回传通道

现状：TreeClip Enter/Exit 全部由 StartFrame/EndFrame 帧边界产生（`TimelineRuntimePreparation` 评估器按 clip 区间边界输出 `TimelineRuntimeTreeClipRequest`），树侧 Enable/Disable/Destroy hook 节点方向为 timeline → 树，无回传。

新增通道：

1. **退出来源标记**：TreeClip 序列化字段标记退出来源（默认 FrameBoundary）。标记 TreeDecision 的 clip，评估器到达 EndFrame 后不再产出 Exit，改为维持活跃。
2. **退出事件上报**：树侧 TimelineDisable 钩子执行或树完成时，经现有 TreeClip 调用链（`CharacterTimelineTreeClipService` 的 ActiveTreeClip 通道）向 Timeline runtime 上报该 clip 退出；runtime 产生真实 Exit 边界请求，进入既有 Advance/Commit 协议（候选 → 调用方 Step 提交），不旁路提交路径。
3. **确定性**：回传事件属于播放实例状态，进既有快照体系；回滚重放时退出边界由重放的树执行结果重现，不由表现层记忆。

## 预览显示

- 显示数据全部来自正式 Runtime 已发布事实：游标（Runtime 位置观察）、活跃 clip 集合、Enter/Exit 事实（既有 playback observation 与 trace 链路已发布）。
- 可视 End 规则：clip 活跃且无定型 Exit 时，End 显示为当前 Runtime 游标位置；Exit 定型后固定于实际退出帧。
- 循环等待类：同规则，playback 停止即定型。
- UI 不新增时钟、不做时长估算、不补长——`preview-integration-plan` 的"不做假延长"禁令由真实退出事件满足。

## 前置与排期

- 前置：时钟域 change（`add-timeline-clock-domain-config`）策略合同与装配开关收尾；主体已落码（`8a508f68e`、`6104e6387`）。
- 实施归属：runtime 回传合同归 timeline runtime 域；树侧上报归树 hook 节点域；显示归 ScenePlay 预览观察链。本 change 文档统一登记合同，实施按域分工，不新建并行实现。
