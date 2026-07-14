## 1. 固定现状与迁移边界

- [x] 1.1 记录 `Timeline` 当前 ScriptableObject 数据字段与 editor-only 字段
- [x] 1.2 记录 Track、Clip、TreeClip 对 Timeline owner 的全部依赖
- [x] 1.3 记录 TimelineNode 的 TimelineReferenceModule、Action Context 和 PlaybackMode 合同
- [x] 1.4 记录 TimelinePlaybackScheduler 的 Object.Instantiate 与 runtime dispose 路径
- [x] 1.5 记录 TimelinePreviewSession 的 source/runtime clone 路径
- [x] 1.6 记录 TimelineEditorWindow 的 selection、preview、field 和 inspector 生命周期
- [x] 1.7 记录 BaseTreeWindow 页面栈、breadcrumb、dirty 和 authoring context 合同
- [x] 1.8 记录 TreeClipAuthoringService 的 Open、Extract Shared 和 Use Inline 合同
- [x] 1.9 统计项目全部 Timeline 资产及其引用者
- [x] 1.10 固定 Corin 11 个 Timeline 与对应 TimelineNode 的一对一映射
- [x] 1.11 固定 Corin 8 个 Decision TreeClip 的 track、clip、frame、phase 和 output identity
- [x] 1.12 固定本 change 不修改 Track/Clip 业务采样语义
- [x] 1.13 固定本 change 不新增测试且不运行 Unity batchmode

## 2. 分离 TimelineData 与 shared asset 外壳

- [x] 2.1 定义普通 C# 可序列化 TimelineData
- [x] 2.2 将 name、tracks、scale 和 authoring 数据迁入 TimelineData
- [x] 2.3 保持 duration、max frame 和 current time 为 TimelineData runtime 状态
- [x] 2.4 定义 TimelineAsset ScriptableObject 外壳
- [x] 2.5 让 TimelineAsset 只持有一份 TimelineData
- [x] 2.6 为 TimelineData 提供正式初始化入口
- [x] 2.7 为 TimelineData 提供正式 managed-reference 深克隆入口
- [x] 2.8 保证 clone 覆盖全部 Track 和 Clip 派生类型
- [x] 2.9 保证 clone 覆盖 TreeClip inline TimelineRunningTree
- [x] 2.10 删除 TimelineData 对 UnityEngine.Object 身份的依赖
- [x] 2.11 更新 Timeline 创建菜单创建 TimelineAsset
- [x] 2.12 更新 Timeline asset Inspector 读取 TimelineAsset.Data

## 3. 建立 TimelineData serialized ownership

- [x] 3.1 定义 TimelineData serialized owner object
- [x] 3.2 定义 TimelineData serialized property path
- [x] 3.3 让 TimelineAsset.Data 绑定 asset owner 与 data path
- [x] 3.4 让 TimelineNode inline data 绑定 RootTree asset owner 与 node module path
- [x] 3.5 让 Track 初始化回指 resolved TimelineData
- [x] 3.6 让 Clip 初始化回指 Track 与 TimelineData
- [x] 3.7 让 TreeClip 从 TimelineData owner path 派生 inline tree path
- [x] 3.8 让 TimelineEditorView 从 owner/path 创建 SerializedProperty
- [x] 3.9 让 Undo 注册到真实 serialized owner
- [x] 3.10 让 dirty 与保存标记真实 serialized owner
- [x] 3.11 在 owner 或 property path 断裂时报告配置错误
- [x] 3.12 禁止通过临时 ScriptableObject 镜像编辑 inline TimelineData

## 4. 将 TimelineNode 改为 inline-first ownership

- [x] 4.1 定义 Timeline ownership 的 Inline、Shared 和 Missing 状态
- [x] 4.2 将 TimelineReferenceModule 替换为 Timeline ownership module
- [x] 4.3 在 module 中保存 inline TimelineData
- [x] 4.4 在 module 中保存 shared TimelineAsset 引用
- [x] 4.5 提供唯一 ResolvedTimelineData 入口
- [x] 4.6 创建 TimelineNode 时自动创建 inline TimelineData
- [x] 4.7 根据节点显示名初始化 inline Timeline 名称
- [x] 4.8 实现选择 shared asset 并清理 inline data
- [x] 4.9 实现 Extract Shared 并清理 inline data
- [x] 4.10 实现 Use Inline 克隆 shared data 并清理 shared 引用
- [x] 4.11 拒绝 inline data 与 shared asset 同时存在
- [x] 4.12 保持 Action Context 与 PlaybackMode 不受 ownership 切换影响
- [x] 4.13 删除外部 Timeline asset 缺失时的 fallback 行为
- [x] 4.14 更新 TimelineNode asset reference 与 graph reference 展示语义

## 5. 将 runtime 收口到 TimelineData

- [x] 5.1 将 ITimelinePlaybackService 请求参数改为 TimelineData
- [x] 5.2 让 TimelineNode 提交 ResolvedTimelineData
- [x] 5.3 保持 playback request 的 source node identity
- [x] 5.4 保持显式 Action Context 进入 playback request
- [x] 5.5 将 TimelinePlaybackScheduler source 类型改为 TimelineData
- [x] 5.6 删除 scheduler 的 Object.Instantiate Timeline ScriptableObject
- [x] 5.7 让 scheduler 使用正式 TimelineData clone 服务
- [x] 5.8 保证每个 request 拥有独立 TimelineData 工作副本
- [x] 5.9 更新 ActiveTimeline 与 terminal handoff 持有 TimelineData
- [x] 5.10 更新 TimelineTreeRuntimeSet 消费 runtime TimelineData
- [x] 5.11 更新 TreeClip runtime context 保存 playback TimelineData
- [x] 5.12 更新 Timeline 名称与 debug identity 解析
- [x] 5.13 删除 runtime 对 Timeline Unity null/bool 运算的依赖
- [x] 5.14 保持 Once、Loop、cancel、terminal presentation 和 dispose 语义不变

## 6. 将 preview 收口到 TimelineData

- [x] 6.1 将 TimelinePreviewSession source 改为 TimelineData
- [x] 6.2 让 preview 使用正式 TimelineData clone 服务
- [x] 6.3 删除 preview 的 Object.Instantiate Timeline ScriptableObject
- [x] 6.4 保持每个 preview page 独立 session identity
- [x] 6.5 保持 preview Registry 与 runtime Registry 隔离
- [x] 6.6 让 inline TimelineEditorWindow 能选择正式 TimelinePreviewTarget
- [x] 6.7 让 shared Timeline root page 使用同一 preview controls
- [x] 6.8 保持 seek、play、pause、speed 和 target 切换语义
- [x] 6.9 保证 preview state 不写入 inline TimelineData
- [x] 6.10 保证 preview state 不写入 shared TimelineAsset

## 7. 保持 Graph 页面栈为 Graph/TreeClip 单一语义

- [x] 7.1 保留 editor-only AuthoringPageEntry
- [x] 7.2 定义 Graph page kind
- [x] 7.3 定义 TreeClip Graph page kind
- [x] 7.4 在 page entry 中保存显示名与来源 graph identity
- [x] 7.5 在 page entry 中保存 serialized owner 与 property path
- [x] 7.6 在 page entry 中保存并继承 authoring context
- [x] 7.7 让 Push、Pop 和 PopTo 只处理 Graph page
- [x] 7.8 让 breadcrumb 只显示 Graph 与 TreeClip page
- [x] 7.9 禁止 TimelineData 进入 BaseTreeWindow 页面栈
- [x] 7.10 让 VisibleTrees 只收集栈内 Graph page
- [x] 7.11 让 Blackboard source resolver 从 Graph 栈收集可见 Graph
- [x] 7.12 让 dirty routing 使用当前 Graph page serialized owner
- [x] 7.13 删除 BaseTreeWindow external Timeline page 容器与生命周期

## 8. 让 TimelineEditorWindow 成为唯一 Timeline 作者窗口

- [x] 8.1 保留可复用 TimelineEditorView
- [x] 8.2 让 TimelineEditorWindow 挂载 track hierarchy
- [x] 8.3 让 TimelineEditorWindow 挂载 track/clip inspector
- [x] 8.4 让 TimelineEditorWindow 挂载 preview controls
- [x] 8.5 让 TimelineEditorWindow 绑定 TimelineData owner/path
- [x] 8.6 让窗口重绑时释放旧 preview owner
- [x] 8.7 让窗口关闭时释放 preview session
- [x] 8.8 让 view rebuild 保持 Timeline scale 与 selection
- [x] 8.9 将 TimelineAsset OnOpenAsset 路由到 TimelineEditorWindow
- [x] 8.10 让 TimelineEditorWindow 保持单一正式绑定入口
- [x] 8.11 禁止 TimelineEditorWindow 跟随普通 Unity Selection 自动切换
- [x] 8.12 禁止为同一次绑定创建并行 preview session

## 9. 接通 TimelineNode 与 TreeClip 双窗口协作

- [x] 9.1 为 TimelineNode 提供 Open TimelineEditorWindow 命令
- [x] 9.2 让 TimelineNode 双击绑定并聚焦 TimelineEditorWindow
- [x] 9.3 让 TimelineNode Inspector Open 绑定并聚焦 TimelineEditorWindow
- [x] 9.4 让 inline Timeline 窗口显示 Inline ownership
- [x] 9.5 让 shared Timeline 窗口显示 Shared Asset ownership
- [x] 9.6 让 TreeClip 双击在来源 Graph 窗口 push TimelineRunningTree
- [x] 9.7 让 TreeClip Inspector Open 使用同一跨窗口路径
- [x] 9.8 让 TreeClip Graph page 继承 Character Root authoring context
- [x] 9.9 让 TreeClip Graph page 继承可见 Blackboard declarations
- [x] 9.10 让 shared TreeClip graph 保持 Shared Asset 标识
- [x] 9.11 让 Graph 窗口在打开 Timeline 时保持当前 Graph page 不变
- [x] 9.12 让 Timeline 窗口在打开 TreeClip 时保持当前 Timeline 可见
- [x] 9.13 删除 Timeline 进入 BaseTreeWindow breadcrumb 的路径
- [x] 9.14 删除 TreeClipAuthoringService 另开无 context Tree 窗口的路径

## 10. 完成 TimelineNode ownership Inspector

- [x] 10.1 在节点选择 Inspector 显示 Timeline ownership
- [x] 10.2 在 Inline 状态显示 Open 与 Extract Shared
- [x] 10.3 在 Shared 状态显示 Open 与 Use Inline
- [x] 10.4 只在显式切换 shared 时显示 TimelineAsset 选择器
- [x] 10.5 禁止默认节点要求作者先创建 TimelineAsset
- [x] 10.6 为非法 Missing 状态显示稳定错误
- [x] 10.7 为 shared asset 类型不匹配显示稳定错误
- [x] 10.8 保持节点画布不强制展开 shared asset 配置
- [x] 10.9 让 ownership 切换进入统一 Undo
- [x] 10.10 让 ownership 切换后刷新 page stack 与 Inspector

## 11. 更新 Agent authoring 与校验

- [x] 11.1 在 TimelineNode Patch IR 中增加 Timeline ownership
- [x] 11.2 将 TimelineNode Patch ownership 默认值定义为 Inline
- [x] 11.3 为 Shared ownership 保存显式 TimelineAsset path
- [x] 11.4 为 Inline ownership支持从 TimelineAsset 导入 template data
- [x] 11.5 保证 inline template path 不保留为 runtime asset reference
- [x] 11.6 删除旧 timelineAssetPath 的默认绑定语义
- [x] 11.7 更新 AgentNodeEmitterRegistry 配置 Timeline ownership module
- [x] 11.8 更新 AgentPatchCompiler 调用正式 ownership authoring API
- [x] 11.9 更新 AgentAssetResolver 解析 TimelineAsset
- [x] 11.10 更新 snapshot 导出 TimelineNode ownership
- [x] 11.11 更新 snapshot 导出 resolved Timeline track/clip summary
- [x] 11.12 将 TreeClip summary 归属到 TimelineNode graph path
- [x] 11.13 只为 Shared ownership 导出 shared asset path
- [x] 11.14 更新 validator 拒绝 inline/shared 双真相
- [x] 11.15 更新 validator 检查 TimelineData serialized owner/path
- [x] 11.16 更新 validator 检查 TreeClip inline graph owner/path
- [x] 11.17 更新 report 显示 State -> TimelineNode -> Timeline -> TreeClip 链路

## 12. 原子迁移 Corin Timeline

- [x] 12.1 迁移 Corin Idle Timeline 到 Idle TimelineNode inline data
- [x] 12.2 迁移 Corin WalkStart Timeline 到 WalkStart TimelineNode inline data
- [x] 12.3 迁移 Corin WalkLoop Timeline 到 WalkLoop TimelineNode inline data
- [x] 12.4 迁移 Corin RunStart Timeline 到 RunStart TimelineNode inline data
- [x] 12.5 迁移 Corin RunLoop Timeline 到 RunLoop TimelineNode inline data
- [x] 12.6 迁移 Corin RunEnd Timeline 到 RunEnd TimelineNode inline data
- [x] 12.7 迁移 Corin MovingTurn Timeline 到 MovingTurn TimelineNode inline data
- [x] 12.8 迁移 Corin Attack1 Timeline 到 Attack1 TimelineNode inline data
- [x] 12.9 迁移 Corin Attack2 Timeline 到 Attack2 TimelineNode inline data
- [x] 12.10 迁移 Corin DodgeForward Timeline 到 DodgeForward TimelineNode inline data
- [x] 12.11 迁移 Corin DodgeBack Timeline 到 DodgeBack TimelineNode inline data
- [x] 12.12 保持 11 个 TimelineNode 的 Once/Loop 配置
- [x] 12.13 保持 4 个 action TimelineNode 的 Action Context
- [x] 12.14 保持全部 AnimationTrack 与 AnimationClip 引用
- [x] 12.15 保持全部 MotionCurveTrack 与 curve 引用
- [x] 12.16 保持 Attack1 的 Hit/Cancel TreeClip 数据
- [x] 12.17 保持 Attack2 的 Hit/Cancel TreeClip 数据
- [x] 12.18 保持 DodgeForward 的 MoveCancel/IFrame TreeClip 数据
- [x] 12.19 保持 DodgeBack 的 MoveCancel/IFrame TreeClip 数据
- [x] 12.20 保持八个 TreeClip 的 Blackboard declaration references
- [x] 12.21 保持八个 TreeClip inline graph 的稳定 owner identity
- [x] 12.22 检查项目不存在对 11 个旧 Timeline 资产的剩余引用
- [x] 12.23 删除 11 个旧 Timeline 资产及对应 meta
- [x] 12.24 删除迁移后为空的 Timeline 资产目录

## 13. 清理旧路径与更新文档

- [x] 13.1 删除 TimelineReferenceModule 旧类型与字段
- [x] 13.2 删除旧 Timeline ScriptableObject asset-only 数据模型
- [x] 13.3 删除 Object.Instantiate Timeline 的全部调用
- [x] 13.4 删除 AssetDatabase.LoadAssetAtPath 作为 inline Timeline runtime 来源的调用
- [x] 13.5 删除 Timeline external page，并让 TimelineEditorWindow 成为唯一 Timeline 窗口入口
- [x] 13.6 删除 TreeClip 无来源 context 的独立 Open 路径
- [x] 13.7 搜索并确认不存在 inline/shared 双写或 fallback
- [x] 13.8 搜索并确认不存在旧 Corin Timeline asset 引用
- [x] 13.9 更新 `openspec/project.md` 的 TimelineNode ownership 口径
- [x] 13.10 更新 `openspec/project.md` 的 Graph/Timeline 双窗口协作口径
- [x] 13.11 更新 Runtime Debug 的 Timeline source ownership 显示
- [x] 13.12 更新 Agent snapshot schema version 与字段说明

## 14. 工具校验

- [x] 14.1 运行 BTSMTL Timeline runtime 项目编译并使用禁用 build server 参数
- [x] 14.2 编译后立即执行 dotnet build-server shutdown
- [x] 14.3 运行 BTSMTL Timeline editor 项目编译并使用禁用 build server 参数
- [x] 14.4 编译后立即执行 dotnet build-server shutdown
- [x] 14.5 运行 BTSMTL TreeDesigner editor 项目编译并使用禁用 build server 参数
- [x] 14.6 编译后立即执行 dotnet build-server shutdown
- [x] 14.7 运行 Assembly-CSharp 项目编译并使用禁用 build server 参数
- [x] 14.8 编译后立即执行 dotnet build-server shutdown
- [x] 14.9 运行 Corin CharacterPipelineDefinition Agent validator
- [x] 14.10 导出 Corin Agent snapshot
- [x] 14.11 检查 snapshot 的 11 个 TimelineNode 全部为 Inline
- [x] 14.12 检查 snapshot 的 8 个 Decision TreeClip 完整保留
- [x] 14.13 检查项目 Timeline 资产数量符合迁移结果
- [x] 14.14 运行 `openspec validate refactor-timeline-node-inline-shared-authoring --strict --no-interactive`
- [x] 14.15 确认所有任务真实完成后再统一更新为 `[x]`
