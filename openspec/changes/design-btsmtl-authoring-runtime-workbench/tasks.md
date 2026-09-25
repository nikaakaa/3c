# Tasks

本清单只定义 Authoring Runtime Workbench 的三种产品形态和接入边界，不把手动验收写成任务。Preview 只是其中一种形态。历史实现记录保留在 `implementation-audit.md`，其中的旧任务勾选不代表本次三种产品形态已经实现。

2026-09-20 继续实施：重新核对六项曾过度勾选的实现缺口，见 [实施记录](implementation-current.md)。三种形态的入口仍在原 Timeline 窗口，Timeline 始终复用原 Slate 面板。2026-09-25 对照代码和作者反馈后，5.3 重新标为未完成；完成勾选只表达对应代码已有实现，不代表整个工作台已经可用或已经归档。

## 1. 产品形态与术语

- [x] 1.1 将 Authoring、Preview、RuntimeDebug 的用户目的、可写边界和数据来源写入共享工作台合同
- [x] 1.2 明确 ScenePlay 是 Preview 的唯一运行底座，不作为第四种产品形态
- [x] 1.3 清理把 Preview 等同于 Timeline 私有播放、RuntimeDebug overlay 或用户手动 Unity Play 的旧描述
- [x] 1.4 将 Workbench、Session、作者版本、运行采用版本和 RuntimeDebug 事实的术语统一到现有项目口径

## 2. 工作台外壳

- [x] 2.1 在原 `TimelineEditorWindow` 中提供 Authoring、Preview、RuntimeDebug 三种工作形态，删除独立 Workbench 窗口
- [x] 2.2 三种形态共用 Session、Actor、Ability、Revision 和当前调用目标显示，不新增 Dashboard 或平行控制器
- [x] 2.3 页面切换只改变本地视图和 interest，不能创建或销毁第二个 Scene、Actor、Session、时钟或执行器
- [x] 2.4 将 Start、Pause、Resume、Stop、Export、Prepare、Publish、Adopt、Capture、History 和 Resume Live 归入正式 ScenePlay / Runtime owner，工具表面只提交请求并显示结果
- [x] 2.5 新增唯一 `BtsmtlScenePlayProfile` SO，只保存 Scene、ContextId、DefaultActorId；Timeline 顶部只选择 Profile，不展开详细设置

## 3. Authoring 形态

- [x] 3.1 保留 FlowCanvas、RootTree、子图、Timeline、Track、Clip、Curve 和参数的正式编辑入口
- [x] 3.2 Authoring 只写唯一作者数据，经 Mutation、Validator、Undo 和 Export；不创建 runtime clone 或本地播放会话
- [x] 3.3 编辑游标、选择、缩放和布局保持窗口本地，不进入 Session、Snapshot 或作者资产
- [x] 3.4 Slate 与 FlowCanvas 继续作为作者表面，不新建第二套编辑器和数据模型

## 4. Preview 形态

- [x] 4.1 进入 Preview 时创建或连接唯一正式 ScenePlay Session，并显示真实准备阶段
- [x] 4.2 Preview 使用正式 Scene、Actor、Ability、RootTree、Timeline、Pose、Motion、Camera、World 和输入链产生结果
- [x] 4.3 作者修改通过 Export、Prepare、Publish 和 Adopt 进入当前 Session，不退出 Scene、Session 或 Actor
- [x] 4.4 兼容参数和内容修改按正式安全边界采用，分别显示作者已修改、已导出、已准备、待采用、已采用和应用失败
- [x] 4.5 不兼容 Timeline 拓扑、状态布局、Composition、Scene、Actor roster、C# 代码和运行模块变化明确拒绝混用并要求重建或新 Session
- [x] 4.6 不使用 Timeline 私有播放器、CMC MontagePlayer、Pose fixture 或 Edit Mode 假 Runtime 作为 Preview 执行路径

## 5. RuntimeDebug 形态

- [x] 5.1 RuntimeDebug 只消费 RuntimeDebugSession、SourceMap、Trace、Playback、Snapshot、Capture/History 和正式提交事实
- [ ] 5.2 RootTree / 子图执行时显示 source-mapped FlowCanvas 只读状态，Timeline / TreeClip 执行时显示对应 Slate 只读状态
- [ ] 5.3 绑定角色后跟随其正式技能调用栈，在 FlowCanvas 与 Slate 之间自动切换，并在子调用返回时恢复父路径；并行调用保持显式 Pin
- [x] 5.4 未执行的 Graph、Timeline、Track 和 Clip 不提前显示；并发 playback 使用 identity、调用点和 generation 隔离
- [x] 5.5 历史观察使用记录时的 SourceMap 和事实，不用当前作者资产重新求值；RuntimeDebug 不写作者数据

## 6. Slate / FlowCanvas 工具接入

- [x] 6.1 复用 Slate 时间尺、Track、Clip、缩放、滚动和绘制能力，复用 FlowCanvas 画布和导航能力
- [x] 6.2 分离 Authoring projection、Preview projection 和 RuntimeDebug projection 的数据来源
- [x] 6.3 RuntimeDebug 的 Slate 内容按正式 Runtime observation 动态维护为只读 TimelineData 投影，不把未执行的作者 Track / Clip 补进运行内容
- [x] 6.4 RuntimeDebug 的 Graph/Timeline 切换不创建额外窗口、额外播放器或额外时钟

## 7. 版本与轻量更新

- [x] 7.1 将作者版本、运行采用版本、导出/准备/发布版本、Session generation 和 RuntimeDebug revision 分开显示
- [x] 7.2 Prepare 与 Publish 期间保留旧版本运行，过期结果不能覆盖新的作者修改或当前采用版本
- [x] 7.3 兼容内容在正式 adoption barrier 采用，不兼容内容拒绝混合旧调用栈、旧 Snapshot 和新 SourceMap
- [x] 7.4 将 CwcMontage 仅登记为预热、手动刷新和局部缓存重建的体验参考，不接入正式执行链

## 8. 文档与清理

- [x] 8.1 统一 change 名、能力名和相关文档引用为 `design-btsmtl-authoring-runtime-workbench` / `btsmtl-authoring-runtime-workbench`
- [x] 8.2 对照现行 `btsmtl-timeline-editor-preview`、`btsmtl-runtime-diagnostics`、Graph Shell 和 Session Composition spec，指出冲突并以现行合同为准
- [x] 8.3 删除或改写把旧窗口播放器、独立时钟、Runtime overlay 当成 Preview 主体的旧规划描述
- [x] 8.4 保留 `implementation-audit.md` 作为历史实施证据，并标明它不等于本 Workbench 产品形态已完成
