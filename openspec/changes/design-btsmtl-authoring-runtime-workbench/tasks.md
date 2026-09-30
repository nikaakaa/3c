# Tasks

本清单只定义 Authoring Runtime Workbench 的三种产品形态和接入边界，不把手动验收写成任务。Preview 只是其中一种形态。历史实现记录保留在 `implementation-audit.md`，其中的旧任务勾选不代表本次三种产品形态已经实现。

2026-09-30 更新：本清单按当前代码事实重新标记。RuntimeDebug 的运行闭环、基础编排效果预览、Ability 执行投影、Preview 的隐性正式场景入口和具备 TreeDecision 的动态 TreeClip 观察仍未完成；完成勾选只表达局部代码存在，不代表 Workbench 已可用或已归档。

## 1. 产品形态与术语

- [x] 1.1 将 Authoring、Preview、RuntimeDebug 的用户目的、可写边界和数据来源写入共享工作台合同
- [x] 1.2 明确 ScenePlay 是 Preview 的唯一运行底座，不作为第四种产品形态
- [x] 1.3 清理把 Preview 等同于 Timeline 私有播放、RuntimeDebug overlay 或用户手动 Unity Play 的旧描述
- [x] 1.4 将 Workbench、Session、作者版本、运行采用版本和 RuntimeDebug 事实的术语统一到现有项目口径

## 2. 工作台外壳

- [x] 2.1 原 Timeline/FlowCanvas 保留作者工作面，独立可停靠 Preview 承载共享角色视口与执行时间线，删除旧内嵌预览入口
- [x] 2.2 三种形态共用 Session、Actor、Ability、Revision 和当前调用目标显示，不新增 Dashboard 或平行控制器
- [x] 2.3 页面切换只改变本地视图和 interest，不能创建或销毁第二个 Scene、Actor、Session、时钟或执行器
- [x] 2.4 将 Start、Pause、Resume、Stop、Export、Prepare、Publish、Adopt、Capture、History 和 Resume Live 归入正式 ScenePlay / Runtime owner，工具表面只提交请求并显示结果
- [x] 2.5 新增唯一 `BtsmtlScenePlayProfile` SO，只保存 AssemblyPrefab、ContextId、DefaultActorId；Preview 与 Timeline 入口共享 Profile 选择，不展开详细设置

- [x] 2.6 Preview 上下分区可拖动并保留布局，重复打开聚焦同一窗口；关闭作者窗口保留预览，关闭 Preview 释放唯一隐藏宿主
- [x] 2.7 在 Preview 视口旁接入当前实例黑板，变量写入复用原作用域与生命周期

## 3. Authoring / Assembling 基础编排

- [x] 3.1 保留 FlowCanvas、RootTree、子图、Timeline、Track、Clip、Curve 和参数的正式编辑入口
- [x] 3.2 Authoring 只写唯一作者数据，经 Mutation、Validator、Undo 和 Export；不创建 runtime clone 或窗口私有播放器；效果查看通过正式宿主
- [x] 3.3 编辑游标、选择、缩放和布局保持窗口本地，不进入 Session、Snapshot 或作者资产
- [x] 3.4 Slate 与 FlowCanvas 继续作为作者表面，不新建第二套编辑器和数据模型

- [ ] 3.5 将纯 Timeline 播放、暂停和拖动效果查看接入正式内容 owner，不要求先构造 Ability 图
- [ ] 3.6 分开显示编辑定位请求与实际到达位置，接入角色或合法非 Skill 目标绑定

- [ ] 3.7 编辑提交自动组织 Timeline 内容采用，区分内容刷新、Track/Clip 生命周期重建与受影响图 Build，不要求用户逐步执行四个阶段

## 4. Ability Preview 形态

- [ ] 4.1 打开预览区时自动创建隔离隐藏 Scene，不进入 Unity Play，装配唯一正式 Session；正常操作不暴露准备步骤，耗时或失败时显示真实原因
- [ ] 4.2 Preview 使用正式 Scene、Actor、Ability、RootTree、Timeline、Pose、Motion、Camera、World 和输入链产生结果
- [x] 4.3 作者修改通过 Export、Prepare、Publish 和 Adopt 进入当前 Session，不退出 Scene、Session 或 Actor
- [x] 4.4 兼容参数和内容修改按正式安全边界采用，分别显示作者已修改、已导出、已准备、待采用、已采用和应用失败
- [x] 4.5 状态布局、Composition、Scene、Actor roster、C# 代码和不兼容运行模块变化明确拒绝混用并要求重建或新 Session；普通 Timeline 结构更新见 3.7
- [x] 4.6 不使用 Timeline 私有播放器、CMC MontagePlayer、Pose fixture 或 Edit Mode 假 Runtime 作为 Preview 执行路径

- [x] 4.7 将 Preview 从 EditorPlayModeSceneLauncher 迁移到隐藏场景宿主，删除 Preview 的旧启动调用
- [ ] 4.8 接入现有 GameplayTickSystem 的编辑器驱动和正式输入端口，保证同一 Session 只有一个驱动来源
- [ ] 4.9 接入连续播放、暂停、单步、倍速和表现时钟策略
- [ ] 4.10 接入正式准备、停止和资源释放合同，按对齐后的规则处理重载、退出与进入 Unity Play
- [ ] 4.11 接入隐藏场景共享视口并显示正式角色与相机结果

- [ ] 4.12 接通打开 Ability 的节点图、执行时间线与共享角色视口
- [ ] 4.13 补齐正式执行事实中的节点发生身份、父子调用、Loop 迭代、决策值及所选分支
- [ ] 4.14 按已提交事实增量建立持续节点区间、瞬时决策和调用投影，不生成作者 Clip 资产
- [ ] 4.15 展开 Loop 每次迭代与并发调用，关联正式 Timeline playback/cycle 并实现来源定位
- [x] 4.16 接入执行历史游标，按记录版本显示节点、变量及截至该位置的开放区间
- [ ] 4.17 接通历史角色表现的正式记录或恢复能力，显示实际位置与缺失状态
- [x] 4.18 接入当前实例黑板正式写入，沿用原可写性、作用域、生命周期和业务投射，记录采用值和位置；不新增可调勾选、覆盖层或调参重放协议

## 5. RuntimeDebug 形态

- [x] 5.1 RuntimeDebug 只消费 RuntimeDebugSession、SourceMap、Trace、Playback、Snapshot、Capture/History 和正式提交事实
- [ ] 5.2 RootTree / 子图执行时显示 source-mapped FlowCanvas 只读状态，Timeline / TreeClip 执行时显示对应 Slate 只读状态
- [ ] 5.3 绑定角色后跟随其正式技能调用栈，在 FlowCanvas 与 Slate 之间自动切换，并在子调用返回时恢复父路径；并行调用保持显式 Pin
- [ ] 5.4 未执行的 Graph、Timeline、Track 和 Clip 不提前显示；并发 playback 使用 identity、调用点和 generation 隔离
- [x] 5.5 历史观察使用记录时的 SourceMap 和事实，不用当前作者资产重新求值；RuntimeDebug 不写作者数据

## 6. Slate / FlowCanvas 工具接入

- [x] 6.1 复用 Slate 时间尺、Track、Clip、缩放、滚动和绘制能力，复用 FlowCanvas 画布和导航能力
- [ ] 6.2 分离 Authoring、Preview 和 RuntimeDebug 的数据来源，并保持 RuntimeDebug 不复制或替换作者 TimelineData
- [ ] 6.3 RuntimeDebug 在正式 Slate 表面显示真实 playback、游标、活动 Track/Clip 和具备 TreeDecision 的动态 TreeClip 退出事实；当前动态 TreeClip 长度与历史重建仍未闭环
- [x] 6.4 RuntimeDebug 的 Graph/Timeline 切换不创建额外窗口、额外播放器或额外时钟

## 7. 版本与轻量更新

- [x] 7.1 将作者版本、运行采用版本、导出/准备/发布版本、Session generation 和 RuntimeDebug revision 分开显示
- [x] 7.2 Prepare 与 Publish 期间保留旧版本运行，过期结果不能覆盖新的作者修改或当前采用版本
- [x] 7.3 兼容内容在正式 adoption barrier 采用，不兼容内容拒绝混合旧调用栈、旧 Snapshot 和新 SourceMap
- [x] 7.4 明确采用 CwcMontage 式隐藏场景和编辑交互，业务执行仍复用正式 Session，不接入 CMC 播放器

- [ ] 7.5 消除 Workbench 编辑器稳定刷新链中的重复求值、无变化重绘和托管分配，暂停无命令时保持静止
- [ ] 7.6 将运行投影与导航更新限定到变化的正式实例和记录，避免每次刷新全量重建历史、作者内容或窗口表面
- [ ] 7.7 统一共享预览画面的渲染归属，避免多个页面为同一正式表现帧重复渲染，关闭后释放其资源与订阅

## 8. 文档与清理

- [x] 8.1 统一 change 名、能力名和相关文档引用为 `design-btsmtl-authoring-runtime-workbench` / `btsmtl-authoring-runtime-workbench`
- [x] 8.2 对照现行 `btsmtl-timeline-editor-preview`、`btsmtl-runtime-diagnostics`、Graph Shell 和 Session Composition spec，记录冲突与本 change 的替换条款，未归档能力不宣称已实现
- [x] 8.3 删除或改写把旧窗口播放器、独立时钟、Runtime overlay 当成 Preview 主体的旧规划描述
- [x] 8.4 保留 `implementation-audit.md` 作为历史实施证据，并标明它不等于本 Workbench 产品形态已完成
