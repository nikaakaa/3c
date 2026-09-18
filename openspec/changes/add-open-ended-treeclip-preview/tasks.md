# Tasks

2026-09-17 立项：grill 定案见 design.md。前置依赖时钟域 change 收尾（策略合同与装配开关）。本清单不含测试与验证任务。

## 1. 退出来源标记

- [x] 1.1 Logic TreeClip 增加退出来源序列化字段（默认 FrameBoundary），标记 TreeDecision 的 clip EndFrame 语义降级为最短持有时长
- [x] 1.2 编辑器 Logic TreeClip 属性区暴露退出来源选择，默认值保持现状语义
- [x] 1.3 内容闭包与指纹纳入退出来源字段

## 2. 树退出回传与 Exit 边界

- [x] 2.1 评估器对 Logic TreeDecision clip 到达 EndFrame 后维持活跃、不产出 Exit
- [x] 2.2 （2026-09-18 决策b）Timeline侧接收端落地：`CharacterTimelineTreeClipService.RequestTreeClipExit`/`TimelineRuntimeService.RequestTreeClipExit` 上行入口就位
- [x] 2.3 Timeline runtime 接收退出事件产生真实 Exit 边界请求，走既有 Advance/Commit 协议
- [x] 2.4 回传事件进入播放快照体系，回滚重放时退出边界由重放结果重现

## 3. 预览观察显示

- [x] 3.1 ScenePlay 预览观察链发布开放 clip 的生长状态（活跃、未定型、游标位置）
- [x] 3.2 Timeline 窗口开放 clip 可视 End 跟随 Runtime 游标，定型后固定于实际退出位置
- [x] 3.3 循环等待类 clip 同规则显示，playback 停止定型；不新增 clip 级退出判定（由3.1/3.2通用显示规则覆盖，无独立实现项）

## 4. 树侧触发节点

- [x] 4.1 新增作者可见的“结束片段”Timeline Body节点，编译为正式 `TimelineClipExitRequest` operation
- [x] 4.2 Fixed/Float32 Ability执行域在Logic TreeClip invocation上下文内触发退出请求，并接入Timeline接收端
- [x] 4.3 pending树退出进入播放状态；当前Advance discard时丢弃，commit后由下一个Advance注入真实Exit边界，回滚重放可重建
- [ ] 4.4 接入执行域合同：`TreeDecision` 与“结束片段”只允许 Logic 投影使用；Presentation TreeClip 拒绝该配置，DualProjection 只把请求路由到 Logic 投影
