## MODIFIED Requirements

### Requirement: Graph 引用和页面栈保持 editor-only

系统 MUST 让节点、边、模块、TimelineNode 和 Timeline Clip 通过正式 authoring reference 表达下钻内容。默认私有 Graph 和 Timeline MUST 支持 inline data，需要复用时才显式使用 shared asset。BaseTreeWindow 的作者页面栈 MUST 只支持 Graph page 和 TreeClip resolved Graph page；Timeline MUST 由独立 TimelineEditorWindow 编辑，不得进入 Graph breadcrumb。页面栈、窗口绑定、selection restore 和来源 identity MUST 保持 editor-only，不得参与 runtime 或序列化到业务数据。

#### Scenario: 节点下钻到内联 Graph

- **WHEN** 用户从节点的 inline graph reference 打开子 Graph
- **THEN** 编辑器 MUST push 该节点内部持有的 Graph page
- **AND** page entry MUST记录来源节点和引用 key

#### Scenario: 节点下钻到 shared Graph

- **WHEN** 用户从节点的 shared graph reference 打开子 Graph
- **THEN** 编辑器 MUST push shared graph asset 持有的 Graph page
- **AND** UI MUST显示该引用是 Shared Asset

#### Scenario: TimelineNode 下钻到 inline Timeline

- **WHEN** 用户从 TimelineNode 执行 Open 或双击
- **THEN** 来源 Graph 窗口 MUST保持当前 Graph page 不变
- **AND** 独立 TimelineEditorWindow MUST绑定该节点持有的 TimelineData
- **AND** TimelineEditorWindow MUST保存 serialized owner/path 与来源 authoring context
- **AND** Timeline MUST NOT进入 Graph 页面栈或 breadcrumb

#### Scenario: TimelineNode 下钻到 shared Timeline

- **WHEN** TimelineNode 使用 Shared Asset ownership 并执行 Open
- **THEN** 独立 TimelineEditorWindow MUST绑定 shared TimelineAsset 的 TimelineData
- **AND** UI MUST显示当前来源为 Shared Asset
- **AND** 来源 Graph 的 authoring context MUST继续可用于 TreeClip 下钻

#### Scenario: Timeline 下钻到 TreeClip

- **WHEN** 用户从 TimelineEditorWindow 打开 TreeClip
- **THEN** 来源 Graph 窗口 MUST push resolved TimelineRunningTree Graph page
- **AND** TimelineEditorWindow MUST保持当前 Timeline 可见
- **AND** Graph breadcrumb MUST只表达 Graph 与 TreeClip 来源路径，不得加入 Timeline page
- **AND** TreeClip Graph page MUST继承可见 Blackboard declarations

#### Scenario: 保存双窗口内容

- **WHEN** 用户在 Graph、TreeClip Graph 或 TimelineEditorWindow 修改数据
- **THEN** dirty 与 Undo MUST作用于当前数据的真实 serialized owner
- **AND** Graph 页面栈、Timeline 窗口绑定、breadcrumb、preview state 和返回位置 MUST NOT序列化到 Graph、TimelineData、TimelineAsset、节点、Track 或 Clip
