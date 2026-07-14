# Change: 将 TimelineNode 收口为 inline-first Timeline 作者模型与双窗口协作

## Why

当前 Corin 的四个动作 Timeline 资产已经真实包含 `TreeTrack / TreeClip / inline TimelineRunningTree`：Attack1、Attack2 各有 Hit/Cancel 两个 Decision TreeClip，DodgeForward、DodgeBack 各有 MoveCancel/IFrame 两个 Decision TreeClip。作者直接打开 Timeline 时感知不到这条结构链路，问题不在资产缺失，而在所有权和编辑导航仍然分裂：

- `TimelineNode` 只通过 `TimelineReferenceModule` 引用外部 `Timeline ScriptableObject`，即使某条 Timeline 只服务一个状态，也必须创建一次性资产；
- Graph 下钻使用 `BaseTreeWindow` 的页面栈，Timeline 编辑使用独立 `TimelineEditorWindow`，但 TreeClip 仍通过旧服务另开无来源关系的 Tree 窗口；
- Graph 与 Timeline 本应同时可见，Timeline 进入 Graph 内部页签栈会让作者无法同时观察状态结构和时间结构；
- Corin 当前 11 个 Timeline 资产与 11 个 TimelineNode 是一对一关系，没有体现“默认私有、显式复用”的项目口径。

现行 `btsmtl-runnable-timeline-node` 和 `openspec/project.md` 仍明确要求 Timeline asset 不默认嵌入节点，这与用户确认的新方向冲突。本变更将 Timeline 数据与 shared asset 外壳分离，让 `TimelineNode` 默认拥有 inline Timeline 数据，只有明确复用时才引用 shared Timeline asset，并让 Timeline 与 TreeClip 成为现有作者页栈中的正式页面。

## What Changes

- 将 Timeline 的 tracks、clips、scale 和序列化 ownership 抽为普通 C# `TimelineData`；shared `TimelineAsset` 只作为 Unity Project 入口与复用外壳持有一份 `TimelineData`。
- 将 `TimelineNode` 改为 inline-first：新节点自动创建可立即下钻的 inline `TimelineData`，并提供 `Inline`、`Shared Asset`、`Missing` ownership。
- `TimelineNode` 的 inline data 与 shared asset 互斥；`Extract Shared`、选择 shared asset 和 `Use Inline` 必须原子切换并清理另一份真数据。
- 删除 `TimelineReferenceModule` 的“必须引用外部 Timeline asset”正式语义，改为单一 Timeline ownership/reference module；不保留旧字段兼容读取或 asset fallback。
- 让 playback request、`TimelinePlaybackScheduler`、preview session 和 Track/Clip runtime 统一消费 resolved `TimelineData`；每次 playback 从 authoring data 创建隔离工作副本，不再依赖 `Object.Instantiate(Timeline ScriptableObject)`。
- 为 inline/shared TimelineData 建立与 inline Graph 相同的 serialized owner + property path 绑定，使 Undo、dirty、SerializedProperty、TreeClip inline Graph owner path 和保存都落到真实 owner。
- 保持 Graph 与 Timeline 两个独立 EditorWindow：Graph 内部页面栈只支持 Graph page 和 TreeClip Graph page，Timeline 不进入 Graph breadcrumb。
- 从 TimelineNode 执行 Open 或双击时，Graph 页面保持不变，现有 TimelineEditorWindow 绑定 resolved TimelineData、serialized owner/path 和来源 authoring context。
- 从 TimelineEditorWindow 打开 TreeClip 时，在来源 Graph 窗口 push resolved `TimelineRunningTree` page；Timeline 窗口保持当前时间轴可见。
- 直接打开 shared Timeline asset 时进入同一个 TimelineEditorWindow；直接打开 shared Tree asset 时继续进入 Graph 窗口。
- Timeline field、track/clip inspector 和 preview session 继续由可复用 TimelineEditorView 承载，但 TimelineEditorWindow 是其唯一正式窗口宿主，不保留 Timeline external page 入口。
- TimelineNode Inspector 显示 ownership、Open、Extract Shared、Use Inline 和显式 shared asset 选择；默认创建不显示“先创建 Timeline asset”的心智。
- TreeClip 的 inline/shared `TimelineRunningTree` 语义保持不变，并继承 TimelineNode 来源窗口的 Character Root authoring context 和可见 Blackboard declarations。
- 更新 Agent Patch、snapshot、validator 和 report：默认创建 inline Timeline；只有显式 `Shared` 请求才保留 asset 引用；snapshot 以 node ownership 与 resolved timeline path 描述 Timeline/TreeClip。
- 原子迁移 Corin 11 个一对一 Timeline 资产到各自 TimelineNode inline data，保持 Animation、MotionCurve、TreeClip、frame range、phase、Action Context 和 playback mode，确认无剩余引用后删除 11 个外部 Timeline 资产。
- 更新 `openspec/project.md`，删除“Timeline asset 天然可复用且不默认嵌入节点”的旧口径。

## Impact

- Affected specs:
  - `btsmtl-runnable-timeline-node`
  - `btsmtl-graph-core`
  - `btsmtl-timeline-editor-preview`
  - `agent-character-controller-synthesis`
  - `character-state-timeline-authoring-loop`
- Affected runtime areas:
  - Timeline data、Track/Clip owner binding 与 runtime clone
  - TimelineNode resolved source 与 playback request
  - TimelinePlaybackScheduler、TimelineTreeRuntimeSet 和 preview session
- Affected editor areas:
  - BaseTreeWindow Graph/TreeClip 页面栈与 breadcrumb
  - TimelineEditorWindow、TimelineEditorView、TimelineFieldView、Track/Clip Inspector
  - TimelineNode Inspector、Node reference Open 行为
  - TreeClipAuthoringService 与 TreeClip 下钻
  - Timeline shared asset 创建/直接打开入口
- Affected Agent areas:
  - Patch IR Timeline ownership
  - Agent emitter、asset resolver、snapshot exporter 和 validator
- Affected Corin assets:
  - `CorinPlayableRootTree.asset`
  - 7 个 locomotion Timeline 资产
  - 4 个 action Timeline 资产
- Breaking authoring change:
  - TimelineNode 不再默认保存外部 Timeline asset 引用。
  - 旧 `Timeline ScriptableObject` 数据必须迁移为 TimelineNode inline data 或显式 shared TimelineAsset。
  - Timeline 不再进入 Graph 内部页签栈；TimelineEditorWindow 成为唯一正式 Timeline 作者窗口。

## Dependencies And Conflicts

- 本 change 依赖 `restore-timeline-treeclip-pipeline-runtime` 的 Decision/Commit scheduler、TreeClip runtime context 和 inline/shared TimelineRunningTree。
- 本 change 依赖 `refactor-pipeline-blackboard-owned-scopes` 的 declaration owner 与 editor authoring context；TimelineNode 下钻必须继续看见 Character Root declarations，不得复制变量。
- 本 change 依赖 `refactor-timeline-window-authoring-to-treeclips` 的 TreeClip + scope variable 单一路径；迁移必须保留八个现有 Decision TreeClip，不能因为 Timeline ownership 重构恢复 ActionWindowTrack。
- 本 change 依赖 `refactor-animation-transition-lifecycle` 的 playback/contribution/transition 生命周期；Timeline 数据所有权变化不得改变动画 lifecycle。
- 现行 `btsmtl-runnable-timeline-node` 要求 `TimelineNode` 通过 `TimelineReferenceModule` 引用 Timeline asset，本 change 明确替换该要求。
- 现行 `agent-character-controller-synthesis` 要求 compiler 绑定 Timeline asset，本 change 明确替换为默认 inline、显式 shared。
- 现行 `openspec/project.md` 写明 Timeline asset 不默认嵌入节点，本 change 实施时必须同步修改。
- 当前 active changes 尚未归档；归档顺序必须先合并它们的 TreeClip、Blackboard 和动画生命周期口径，再归档本 change，避免旧“Timeline asset”措辞覆盖 inline-first 最终口径。

## Stop Conditions

- 如果 Unity 当前 managed-reference 序列化无法在 `CorinPlayableRootTree.asset` 中安全保存 `TimelineData -> Track -> TreeClip -> TimelineRunningTree` 完整嵌套，apply 必须停止说明缺口，不保留外部资产作为 fallback。
- 如果现有 Timeline field 无法绑定 serialized owner + property path，apply 必须停止说明 Undo/dirty 代价，不通过双写 asset 与 inline data 绕过。
- 如果 TimelineEditorWindow 无法把来源 Graph authoring context 安全传给 TreeClip Graph，apply 必须停止说明黑板可见性缺口，不通过复制 declaration 或 key fallback 绕过。
