## 1. 固定依赖与删除边界

- [x] 1.1 记录 `refactor-pipeline-blackboard-owned-scopes` 的最终 declaration owner、scope/lifetime 和 address 合同
- [x] 1.2 记录 `restore-timeline-treeclip-pipeline-runtime` 的 Decision/Commit 调度与 TreeClip authoring service 合同
- [x] 1.3 记录 `unify-graph-data-catalog-authoring` 的唯一 Catalog 与 declaration details 扩展入口
- [x] 1.4 列出 `ActionWindowTrack`、`ActionWindowClip` 和 `TimelineActionWindowSample` 的全部代码引用
- [x] 1.5 列出 Scheduler 的 ActionWindow 采样、prepared list、dedupe key 和 submit helper
- [x] 1.6 列出 CharacterGraphContext 的 timeline decision window cache 与查询入口
- [x] 1.7 列出 `ActionWindowActiveInfoNode` 的 runtime、editor、Agent 和资产引用
- [x] 1.8 列出 `SubmitActionWindowSampleNode` 的 runtime、editor、Agent 和资产引用
- [x] 1.9 统计 Corin 四个 Timeline 中的 ActionWindowTrack/Clip
- [x] 1.10 统计 Corin RootTree 中的 ActionWindow reader
- [x] 1.11 记录保留的 ActionWindowSample、SyncFacts、ActionProfile policy 和 adapter 边界
- [x] 1.12 固定本 change 不修改 Animation、Motion、Cue 和 Camera Track
- [x] 1.13 固定本 change 不新增测试和不运行 Unity batchmode

## 2. 定义 Blackboard fact projection 数据模型

- [x] 2.1 在现有 declaration 数据上增加可选 fact projection kind
- [x] 2.2 将 projection 默认值定义为 None
- [x] 2.3 增加 ActionWindow projection descriptor
- [x] 2.4 为 ActionWindow projection 保存稳定 WindowType
- [x] 2.5 为 ActionWindow projection 保存稳定 WindowId
- [x] 2.6 为 ActionWindow projection保存 Digest
- [x] 2.7 保证 projection 不保存 authority、history、replication 或 packet policy
- [x] 2.8 将 projection 纳入 declaration identity 一致性校验
- [x] 2.9 将 projection 纳入 inline/shared declaration 冲突校验
- [x] 2.10 限制 ActionWindow projection 的值类型为 Bool
- [x] 2.11 限制 ActionWindow projection 的 scope/lifetime 为 Frame/Frame
- [x] 2.12 限制 ActionWindow projection 的 SyncPolicy 为 SyncFact
- [x] 2.13 校验 ActionWindow projection 的 WindowType 非空
- [x] 2.14 校验 ActionWindow projection 的 WindowId 非空
- [x] 2.15 拒绝非法 projection，不静默降级为 None
- [x] 2.16 将 projection 加入 declaration debug snapshot

## 3. 建立结构化 Blackboard 写入 provenance

- [x] 3.1 定义 Pipeline Blackboard write provenance 合同
- [x] 3.2 在 provenance 中保存 local logic tick
- [x] 3.3 在 provenance 中保存 source Graph owner identity
- [x] 3.4 在 provenance 中保存 source runtime identity
- [x] 3.5 在 Timeline 来源 provenance 中保存 playback handle
- [x] 3.6 在 Timeline 来源 provenance 中保存 track/clip/cycle identity
- [x] 3.7 在 action-scoped provenance 中保存显式 Action Context
- [x] 3.8 让 TimelineRunningTree 从正式 Clip runtime context 提供 provenance
- [x] 3.9 让普通 Graph 的 action-bound 写入要求显式 Action Context
- [x] 3.10 禁止 provenance 从 ambient current action 推导 ActionInstance
- [x] 3.11 将 provenance 传入 PipelineBlackboardRuntime 的正式 Set 入口
- [x] 3.12 保持普通无 projection variable 写入无需 Action Context
- [x] 3.13 在缺失 action provenance 时报告稳定错误
- [x] 3.14 在 Graph/runtime owner 断裂时拒绝写入

## 4. 建立单一 Window fact projection

- [x] 4.1 定义帧内 ActionWindow projection candidate
- [x] 4.2 只为显式 ActionWindow-bound declaration 的 true 写入创建 candidate
- [x] 4.3 让 false 或未设置值不创建 candidate
- [x] 4.4 在 candidate 中保存 declaration identity
- [x] 4.5 在 candidate 中保存 WindowType、WindowId 和 Digest
- [x] 4.6 在 candidate 中保存 ActionInstanceId
- [x] 4.7 在 candidate 中保存 source provenance
- [x] 4.8 在 BeginFrame 清理上一 Tick candidates
- [x] 4.9 在 Decision TreeClip 写入时收集 candidate
- [x] 4.10 让普通 Graph 的显式 action-bound Frame 写入复用同一 candidate 路径
- [x] 4.11 在 RootTree 决策完成后执行 WindowFactProjection
- [x] 4.12 按 declaration、ActionInstanceId 和 local tick 去重
- [x] 4.13 将合法 candidate 转换为 ActionWindowSample
- [x] 4.14 将 sample 写入 SyncFacts.Action.WindowSamples
- [x] 4.15 将 sample 写入 ActionRuntime output debug
- [x] 4.16 保持 ActionProfile window policy resolver 不变
- [x] 4.17 保持 NetworkSendStage 只消费 SyncFacts
- [x] 4.18 在 projection 完成后清空 candidates
- [x] 4.19 保证 cancelled source 本 Tick已观察 candidate 最多提交一次
- [x] 4.20 保证 local-only None projection gate 不产生 ActionWindowSample

## 5. 收口 Decision TreeClip Window 运行时

- [x] 5.1 让 Decision TreeClip 继续在 Prepare 阶段按目标 Timeline 时间求值
- [x] 5.2 让 Window TreeClip 只通过 ExposedPropertyNode 写 Bool Frame variable
- [x] 5.3 允许显式 projection 在 Tree 求值后形成 candidate
- [x] 5.4 保持 Decision TreeClip 禁止 Running 节点
- [x] 5.5 保持 Decision TreeClip 禁止 Motion、Cue、Camera 和 GameplayResult 副作用
- [x] 5.6 保持 Decision TreeClip 不直接访问 SyncFacts 或网络 adapter
- [x] 5.7 保证 Window variable 在 RootTree Transition 前可见
- [x] 5.8 保证 State.OnExit 在同 Tick仍可读取 source Window variable
- [x] 5.9 保证 Frame cleanup 移除不再 active 的 Window variable
- [x] 5.10 保证 Loop Timeline 的 Window TreeClip 按 cycle identity 求值
- [x] 5.11 保证同一 clip/cycle/tick 不重复执行 Decision
- [x] 5.12 保证 PresentationFrame 不执行 Window TreeClip

## 6. 删除 ActionWindow Timeline 专用路径

- [x] 6.1 删除 `Timeline.ActionWindow.cs` 中的 TimelineActionWindowSample
- [x] 6.2 删除 ActionWindowTrack
- [x] 6.3 删除 ActionWindowClip
- [x] 6.4 删除 ActionWindowClipInspectorView
- [x] 6.5 删除 Timeline Editor 的 ActionWindow clip inspector 注册
- [x] 6.6 删除 Scheduler 的 m_ActionWindowSamples
- [x] 6.7 删除 Scheduler 的 m_PreparedDecisionWindows
- [x] 6.8 删除 Scheduler 的 m_PreparedDecisionWindowKeys
- [x] 6.9 删除 Scheduler 的 ActionWindowTrack 扫描
- [x] 6.10 删除 Scheduler 的 SampleDecisionWindows
- [x] 6.11 删除 Scheduler 的 SubmitActionWindow helper
- [x] 6.12 删除旧 ActionWindow sample key dedupe helper
- [x] 6.13 删除 CharacterGraphContext.BeginTimelineDecisionFacts
- [x] 6.14 删除 CharacterGraphContext.AddTimelineDecisionWindow
- [x] 6.15 删除 CharacterGraphContext.IsCurrentTickActionWindowActive
- [x] 6.16 删除 timeline decision window 临时集合
- [x] 6.17 保留并收口 projection stage 内部 ActionWindowSample 提交入口
- [x] 6.18 搜索并确认 Scheduler 不再认识 ActionWindowTrack/Clip

## 7. 删除并行 Graph Window producer 与 reader

- [x] 7.1 删除 ActionWindowActiveInfoNode
- [x] 7.2 删除 ActionWindow reader 的 NodeName 与 NodePath 注册
- [x] 7.3 删除 SubmitActionWindowSampleNode
- [x] 7.4 删除 SubmitActionWindowSampleNode 的 Blackboard sample 写入分支
- [x] 7.5 删除 SubmitActionWindowSampleNode 的 Agent validator 分支
- [x] 7.6 删除 ActionWindowActiveInfoNode 的 Agent emitter 注册
- [x] 7.7 删除 Agent patch compiler 的 ActionWindow condition 创建分支
- [x] 7.8 删除 Agent macro 中专用 ActionWindow condition term
- [x] 7.9 将 Agent condition authoring 改为 declaration reference + Blackboard Bool reader
- [x] 7.10 拒绝旧 ActionWindow reader/submit patch kind
- [x] 7.11 搜索并确认正式节点集合不再包含专用 Window producer/reader

## 8. 扩展 Graph Data Catalog 与 TreeClip 作者体验

- [x] 8.1 在 Blackboard declaration details 中增加 Projection 行
- [x] 8.2 为 Projection 提供 None/ActionWindow 选择
- [x] 8.3 在 ActionWindow projection 下显示 WindowType
- [x] 8.4 在 ActionWindow projection 下显示 WindowId
- [x] 8.5 在 ActionWindow projection 下显示 Digest
- [x] 8.6 在非法类型时禁用 ActionWindow projection 并显示原因
- [x] 8.7 在非法 scope/lifetime 时显示配置错误
- [x] 8.8 在非法 SyncPolicy 时显示配置错误
- [x] 8.9 让 inherited declaration 只读显示 projection
- [x] 8.10 为 inherited projection 提供定位 owner 命令
- [x] 8.11 不新增独立 Window panel 或 Window asset editor
- [x] 8.12 在 TreeClip Inspector 显示引用的 Window output declaration 摘要
- [x] 8.13 让 TreeClip 下钻继续使用同一 Graph Data Catalog
- [x] 8.14 更新 Agent snapshot 导出 declaration projection
- [x] 8.15 更新 Agent validator 检查 TreeClip output reference 与 projection
- [x] 8.16 更新 Agent report 显示 Window TreeClip、variable 和 projected fact 关系

## 9. 迁移 Corin Window 资产

- [x] 9.1 记录 Attack1 Hit clip 的帧范围与输出身份
- [x] 9.2 记录 Attack1 Cancel clip 的帧范围与输出身份
- [x] 9.3 记录 Attack2 Hit clip 的帧范围与输出身份
- [x] 9.4 记录 Attack2 Cancel clip 的帧范围与输出身份
- [x] 9.5 记录 DodgeForward IFrame clip 的帧范围与输出身份
- [x] 9.6 记录 DodgeBack IFrame clip 的帧范围与输出身份
- [x] 9.7 为 Attack1Hit 创建 Bool Frame/Frame declaration 与 ActionWindow projection
- [x] 9.8 为 Attack1Cancel 创建 Bool Frame/Frame declaration 与 ActionWindow projection
- [x] 9.9 为 Attack2Hit 创建 Bool Frame/Frame declaration 与 ActionWindow projection
- [x] 9.10 为 Attack2Cancel 创建 Bool Frame/Frame declaration 与 ActionWindow projection
- [x] 9.11 为 DodgeForwardIFrame 创建 Bool Frame/Frame declaration 与 ActionWindow projection
- [x] 9.12 为 DodgeBackIFrame 创建 Bool Frame/Frame declaration 与 ActionWindow projection
- [x] 9.13 使用 TreeClipAuthoringService 创建 Attack1Hit Decision TreeClip
- [x] 9.14 使用 TreeClipAuthoringService 创建 Attack1Cancel Decision TreeClip
- [x] 9.15 使用 TreeClipAuthoringService 创建 Attack2Hit Decision TreeClip
- [x] 9.16 使用 TreeClipAuthoringService 创建 Attack2Cancel Decision TreeClip
- [x] 9.17 使用 TreeClipAuthoringService 创建 DodgeForwardIFrame Decision TreeClip
- [x] 9.18 使用 TreeClipAuthoringService 创建 DodgeBackIFrame Decision TreeClip
- [x] 9.19 保持六个新 TreeClip 的原 ActionWindow 时间范围
- [x] 9.20 让六个 inline Decision Graph 写入各自 declaration true
- [x] 9.21 将 Attack1 Transition 的 Cancel condition 改读 Attack1Cancel Blackboard
- [x] 9.22 将 Attack2 Transition 的 Cancel condition 改读 Attack2Cancel Blackboard
- [x] 9.23 将 Attack1 OnExit 的 Cancel condition 改读同一 Attack1Cancel declaration
- [x] 9.24 将 Attack2 OnExit 的 Cancel condition 改读同一 Attack2Cancel declaration
- [x] 9.25 保持 CanDodgeMoveCancel 为 None projection 本地 gate
- [x] 9.26 删除 Attack1 Timeline 的 ActionWindowTrack/Clip 数据
- [x] 9.27 删除 Attack2 Timeline 的 ActionWindowTrack/Clip 数据
- [x] 9.28 删除 DodgeForward Timeline 的 ActionWindowTrack/Clip 数据
- [x] 9.29 删除 DodgeBack Timeline 的 ActionWindowTrack/Clip 数据
- [x] 9.30 删除 Corin RootTree 的全部 ActionWindowActiveInfoNode 数据
- [x] 9.31 保留 Corin ActionProfile 的 Hit/Cancel/IFrame policy
- [x] 9.32 确认迁移未创建一次性 BaseTreeAsset 或 SubTree asset

## 10. 清理配置、快照与文档口径

- [x] 10.1 更新 Agent snapshot 的 Timeline TreeClip output 摘要
- [x] 10.2 删除 snapshot 中旧 ActionWindowTrack/reader 专用摘要
- [x] 10.3 更新 Agent validator 拒绝任何残留 ActionWindowTrack/Clip managed reference
- [x] 10.4 更新 Agent validator 校验 projected Window declaration 的 ActionProfile policy 可解析
- [x] 10.5 更新 Runtime Debug 显示 declaration、TreeClip、ActionInstance 和 ActionWindowSample 链路
- [x] 10.6 更新 Runtime Debug 区分 None projection local gate 与 projected fact
- [x] 10.7 更新 `openspec/project.md` 的 Timeline Window 作者主链路
- [x] 10.8 删除 project context 中 ActionWindowTrack 作为正式输出轨道的描述
- [x] 10.9 搜索并删除代码中的旧 ActionWindow authoring 注释和错误文本
- [x] 10.10 搜索并确认资产中不存在 ActionWindowTrack 或 ActionWindowClip
- [x] 10.11 搜索并确认代码中不存在 ActionWindowActiveInfoNode 或 SubmitActionWindowSampleNode
- [x] 10.12 搜索并确认不存在专用 timeline decision window cache
- [x] 10.13 确认 ActionWindowSample 只能由统一 projection stage 产生
- [x] 10.14 确认不存在旧字段反序列化、兼容转换或 fallback path
- [x] 10.15 为 Corin RootTree 全部可达 inline Graph 补齐稳定 Blackboard owner identity
- [x] 10.16 将 Agent validator 的 Blackboard owner identity 校验扩展到全部可达 Graph

## 11. 工具校验

- [x] 11.1 运行 BTSMTL TreeDesigner 项目编译并使用禁用 build server 参数
- [x] 11.2 编译后立即执行 dotnet build-server shutdown
- [x] 11.3 运行 Assembly-CSharp 项目编译并使用禁用 build server 参数
- [x] 11.4 编译后立即执行 dotnet build-server shutdown
- [x] 11.5 运行 Corin CharacterPipelineDefinition Agent validator
- [x] 11.6 导出 Corin Agent snapshot
- [x] 11.7 检查 snapshot 只包含 TreeClip + Blackboard Window 作者路径
- [x] 11.8 运行 `openspec validate refactor-timeline-window-authoring-to-treeclips --strict --no-interactive`
- [x] 11.9 确认所有 tasks 完成后再统一更新为 `[x]`

