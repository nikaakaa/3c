## 1. 稳定 Authoring Identity

- [x] 1.1 定义统一 Graph、Element、Timeline、Track 和 Clip authoring identity 值类型与格式
- [x] 1.2 将 `BaseGraph.BlackboardOwnerId` 正式重命名为 `GraphAuthoringId`
- [x] 1.3 更新 Graph 创建流程生成 `GraphAuthoringId`
- [x] 1.4 更新 Graph clone 流程保留 `GraphAuthoringId`
- [x] 1.5 更新 Node/Edge 创建与复制流程维护稳定 identity
- [x] 1.6 将 Pipeline Blackboard declaration owner reference 切换到 `GraphAuthoringId`
- [x] 1.7 更新 Blackboard runtime address 和 validator 使用统一 Graph identity
- [x] 1.8 迁移现有 Root、inline、shared、StateMachine、State body、ConditionRule 和 TreeClip Graph identity
- [x] 1.9 删除旧 `BlackboardOwnerId` 字段、API 和命名
- [x] 1.10 为 `TimelineData` 增加稳定 authoring identity
- [x] 1.11 为所有 Track 增加稳定 authoring identity
- [x] 1.12 为所有 Clip 增加稳定 authoring identity
- [x] 1.13 更新 Timeline/Track/Clip 创建流程生成 identity
- [x] 1.14 更新 Timeline/Track/Clip authoring 复制流程生成新 identity
- [x] 1.15 更新 Timeline runtime clone 保留 source identity
- [x] 1.16 更新 Timeline validator 拒绝缺失和重复 identity
- [x] 1.17 迁移 Corin 所有 inline Timeline、Track、Clip 和 TreeClip identity
- [x] 1.18 迁移现有 shared Timeline asset identity

## 2. Agent Snapshot Schema v4

- [x] 2.1 将 Agent Snapshot schema 常量升级为 v4
- [x] 2.2 在 Graph summary/full model 输出 `GraphAuthoringId`
- [x] 2.3 在 Node/Edge model 输出稳定 authoring identity
- [x] 2.4 在 Timeline model 输出 Timeline authoring identity
- [x] 2.5 在 Track model 输出 Track authoring identity
- [x] 2.6 在 Clip/TreeClip model 输出 Clip authoring identity
- [x] 2.7 更新 Snapshot exporter 构建 identity-based source path 关联
- [x] 2.8 更新 Patch IR 以 identity 指定已有元素
- [x] 2.9 更新 Patch compiler 在修改时保持 identity
- [x] 2.10 更新 Patch compiler 在创建/复制时生成新 identity
- [x] 2.11 更新 Agent validator 拒绝 identity 断裂和歧义
- [x] 2.12 删除 schema v3 解析、兼容和 path/index fallback
- [x] 2.13 重新导出 Corin schema v4 snapshot

## 3. Runtime Diagnostics Contracts

- [x] 3.1 创建不引用 UnityEditor 的 runtime diagnostics assembly
- [x] 3.2 定义 ProgramId、CompilationRevision 和 SourceContentHash
- [x] 3.3 定义 source element handle 和 element kind
- [x] 3.4 定义 Character、Graph、State activation、Timeline playback 和 TreeClip runtime instance key
- [x] 3.5 定义 Logic、Presentation 和 Lifecycle trace domain
- [x] 3.6 定义 Graph、StateMachine、Timeline、Blackboard、Animation 和 Motion channel
- [x] 3.7 定义统一 Trace event header 和单调 sequence
- [x] 3.8 定义受限 debug value snapshot 类型
- [x] 3.9 定义 Debug Source Map 只读合同
- [x] 3.10 定义 Trace sink 和 channel 查询合同
- [x] 3.11 实现按完整 tick/frame segment 淘汰的有界 Trace Buffer
- [x] 3.12 实现 target termination 和 buffer disposal 生命周期

## 4. Source Map 与 Revision

- [x] 4.1 实现当前解释执行 source map builder
- [x] 4.2 映射 Graph、Node、Edge 和 declaration source
- [x] 4.3 映射 Timeline、Track、Clip 和 TreeClip source
- [x] 4.4 为当前 Character authoring source 计算确定性 SourceContentHash
- [x] 4.5 将 source revision 绑定到 Character runtime session
- [x] 4.6 在 source map 中表达多个 execution handles 到同一 source element
- [x] 4.7 拒绝缺失、重复或无法解析的 source handle
- [x] 4.8 定义未来 compiler 产出同一 source map 的公开合同，不实现 compiler backend

## 5. Runtime Trace Producers

- [x] 5.1 在 Runnable lifecycle 中发布 Node enter/status/stop/exit events
- [x] 5.2 在 Composite child 选择和 abort 边界发布 Graph events
- [x] 5.3 在 ConditionRuleGraph 求值边界发布 edge/result events
- [x] 5.4 在 StateMachine source exit、waiting、commit 和 force stop 发布 events
- [x] 5.5 在 State owner ready、membership release 和 activation generation 发布 events
- [x] 5.6 在 Timeline request/start/update/complete/cancel 发布 playback events
- [x] 5.7 在 Timeline logic time、cycle 和 terminal presentation 发布 time events
- [x] 5.8 在 Track/Clip membership 变化发布 Timeline events
- [x] 5.9 在 Decision/Commit TreeClip lifecycle 发布 TreeClip events
- [x] 5.10 在 Blackboard write、clear 和 projection 发布 Blackboard events
- [x] 5.11 在 Motion contribution 与 channel resolve 发布 Motion events
- [x] 5.12 在 Animation Registry sample/release/complete/owner release 发布 Animation events
- [x] 5.13 在 ordered handoff submit/ready 与 playback lifecycle run/retire 发布 Animation events
- [x] 5.14 在 Arbitrator priority allocation、ordered causal commit、LayerPlan 与 final output 发布 Animation events
- [x] 5.15 在 PresentationFrame 发布 interpolation 和 visual time events
- [x] 5.16 确认所有 producer 只观察正式状态且不创建平行 runtime 数据
- [x] 5.17 确认 channel 关闭时不构建对应非必要 payload

## 6. Character Diagnostics Target

- [x] 6.1 为每个 CharacterPipeline 创建唯一 runtime diagnostics session identity
- [x] 6.2 将 Trace Buffer 和 source revision 注入 CharacterPipeline 正式生命周期
- [x] 6.3 创建 editor 可发现但不暴露 runtime Graph 对象的 target metadata
- [x] 6.4 在 CharacterPipelineHost activate 时注册 diagnostics target
- [x] 6.5 在 deactivate/dispose 时注销 target 并发布 termination
- [x] 6.6 删除 Host Inspector 对 runtime 私有 debug collection 的平行读取入口

## 7. RuntimeDebugSession 与分析模型

- [x] 7.1 创建 editor-only diagnostics target registry
- [x] 7.2 创建唯一 RuntimeDebugSession service
- [x] 7.3 实现显式 Character target 选择
- [x] 7.4 实现 Graph/State/Timeline/TreeClip runtime instance 索引
- [x] 7.5 实现 Follow Selection 与 Pin instance 状态
- [x] 7.6 实现 channel 开关状态
- [x] 7.7 实现 live、pause 和有界历史位置
- [x] 7.8 按 domain、tick/frame 和 sequence 分析 Trace
- [x] 7.9 重建 Node、Edge、StateMachine 和 lifecycle snapshot
- [x] 7.10 重建 Timeline playback、time、Track/Clip 和 TreeClip snapshot
- [x] 7.11 重建 Blackboard、Motion、Animation 和 Presentation snapshot
- [x] 7.12 实现 source revision 严格匹配和 mismatch 状态
- [x] 7.13 实现 target termination 自动 detach
- [x] 7.14 提供 Graph、Timeline 和 Inspector 共用只读 view model

## 8. Graph Live Debug UI

- [x] 8.1 在 BaseTreeWindow 增加 Authoring/Live Debug 显式模式状态
- [x] 8.2 在 Live Debug 模式附着共享 RuntimeDebugSession
- [x] 8.3 根据 `GraphAuthoringId` 筛选可选 runtime instances
- [x] 8.4 根据 Node identity 叠加运行状态颜色和 lifecycle
- [x] 8.5 根据 Edge identity 叠加 evaluated/selected/transition 状态
- [x] 8.6 在 StateMachine 节点显示 active/exiting/target runtime state
- [x] 8.7 在 breadcrumb 下钻时解析对应 child runtime instance
- [x] 8.8 显示当前 target、instance、tick/frame 和 revision
- [x] 8.9 在 revision mismatch 时停止 overlay 并显示错误
- [x] 8.10 在 Live Debug 模式保持 authoring data 只读
- [x] 8.11 删除 BaseNodeView 直接读取 `RunnableNode.State` 的旧高亮路径
- [x] 8.12 删除任何 runtime clone page/binding 入口

## 9. Timeline Live Debug UI

- [x] 9.1 在 TimelineEditorWindow 增加 Authoring Preview/Live Debug 分段模式
- [x] 9.2 保持 Authoring Preview 只由 TimelinePreviewSession 驱动
- [x] 9.3 在 Live Debug 模式附着共享 RuntimeDebugSession
- [x] 9.4 按 Timeline authoring identity 列出 playback instances
- [x] 9.5 实现 Follow Graph Selection 和 Pin Playback
- [x] 9.6 显示 logic time、visual time、cycle 和 playback lifecycle
- [x] 9.7 按 Track/Clip identity 高亮真实 membership
- [x] 9.8 显示 Decision/Commit TreeClip evaluation 和 runtime instance
- [x] 9.9 显示 animation contribution、owner、priority、weight 和 final weight
- [x] 9.10 显示 terminal、cancel 和 stop cause
- [x] 9.11 在 Live Debug 模式禁止 authoring 修改和 preview target 操作
- [x] 9.12 在 revision mismatch 或 target detach 时清理 runtime overlay

## 10. Pipeline Inspector Diagnostics UI

- [x] 10.1 让 CharacterPipelineHostEditor 绑定共享 RuntimeDebugSession view model
- [x] 10.2 添加持续 editor repaint 调度
- [x] 10.3 显示当前 target、runtime session、revision 和 buffer 状态
- [x] 10.4 显示 Action、Blackboard、Motion 和 Camera domain snapshot
- [x] 10.5 按 layer 显示 Registry 输入 contributions
- [x] 10.6 显示 ordered handoff records 与 causal component dispositions
- [x] 10.7 显示 priority group 需求、分配容量和 scale
- [x] 10.8 显示最终 Animancer playback plans
- [x] 10.9 提供 event identity 到 Graph/Timeline source 的打开入口
- [x] 10.10 删除旧 Inspector 平行格式化和直接 runtime collection 读取

## 11. 清理与文档同步

- [x] 11.1 删除旧 runtime Node direct-state editor path
- [x] 11.2 删除旧 Host Inspector 平行 debug 数据链
- [x] 11.3 确认 TimelinePreviewSession 不参与 Live Debug
- [x] 11.4 确认 editor diagnostics 不持有 runtime Graph/Node/Track/Clip reference
- [x] 11.5 确认项目不存在按名称或 index source mapping fallback
- [x] 11.6 更新 `openspec/project.md` 的 schema v4 和 runtime diagnostics 架构口径
- [x] 11.7 更新相关代码跳转文档和调试入口说明

## 12. 静态验证

- [x] 12.1 使用 required build flags 编译受影响 runtime assemblies
- [x] 12.2 每次 build 后立即执行 `dotnet build-server shutdown`
- [x] 12.3 使用 required build flags 编译受影响 editor assemblies
- [x] 12.4 每次 build 后立即执行 `dotnet build-server shutdown`
- [x] 12.5 运行 Agent Graph validator 验证 Corin identity 和 schema v4 snapshot
- [x] 12.6 运行 `openspec validate add-btsmtl-compiled-runtime-debugging --strict --no-interactive`

## 13. Live Debug 可见执行位置闭环

- [x] 13.1 复现 Play Mode 打开 Graph 后 Source revision mismatch 导致 overlay 被拒绝的问题
- [x] 13.2 阻止 Play Mode Graph 窗口初始化改写 authoring tree 内容
- [x] 13.3 使 Graph Live Debug 基于匹配的 source revision 构建节点和边运行态 overlay
- [x] 13.4 使 Timeline Live Debug 基于匹配的 playback 构建 visual time、Track 和 Clip 运行态 overlay
- [x] 13.5 运行 `openspec validate add-btsmtl-compiled-runtime-debugging --strict --no-interactive`
