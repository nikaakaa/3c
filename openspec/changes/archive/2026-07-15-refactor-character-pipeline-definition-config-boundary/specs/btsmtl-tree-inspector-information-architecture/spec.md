## MODIFIED Requirements

### Requirement: Tree Inspector 必须将 Data 与 Inspector 作为互斥工作页

Tree Inspector MUST提供 Data 与 Inspector 两个互斥工作页。Data 页 MUST只承载唯一 Graph Data Catalog；Inspector 页 MUST只承载当前选中 Node/Edge 的 BTSMTL authoring 内容，或无选择时的 Graph Authoring Settings。角色动画 Layer、producer binding、transition、fade、playback lifecycle 和 Animancer 配置 MUST不进入 Tree Inspector 可写内容。

#### Scenario: 打开角色 RootTree

- **WHEN** 作者从 Character Pipeline 打开 RootTree
- **THEN** 左侧默认显示 Data 页和唯一 Graph Data Catalog
- **AND** Data 页 MUST不显示动画播放生命周期字段

#### Scenario: 选择 Transition edge

- **WHEN** 作者选择一条 StateMachine Transition edge
- **THEN** Inspector MUST显示 priority、condition ownership、rule 与 interruption
- **AND** Inspector MUST不显示 HandoffRole、animation strategy、duration、curve 或 producer binding

#### Scenario: 手动查看无选择 Inspector

- **WHEN** 当前没有选中 Node 或 Edge，作者切换到 Inspector 页
- **THEN** 页面 MUST显示当前 Graph 的合法 authoring settings 或明确空状态
- **AND** 系统 MUST不使用 runtime lifecycle 或伪默认 Presentation 配置填充

#### Scenario: 打开动画表现配置

- **WHEN** 作者需要调整 producer transition 或 animation layer
- **THEN** 系统 MUST从 CharacterPipelineDefinition 引用导航到 CharacterAnimationPresentationProfile Inspector 或 Animancer Transition Library 正式入口
- **AND** Tree Inspector MUST不创建同一数据的第二写入口
