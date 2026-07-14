## ADDED Requirements

### Requirement: TreeClip 私有下钻 Graph 必须默认 inline

Timeline TreeClip 作为拥有下钻 Graph 的 authoring owner 时，编辑器 MUST 自动创建并保存 inline `TimelineRunningTree` graph data。作者需要复用时 MAY 显式 Extract Shared 到 `BaseTreeAsset`。Inline 与 shared MUST 共享同一 resolved graph 合同，并且同一 TreeClip 只能有一个真数据来源。系统 MUST NOT 要求作者为普通 TreeClip 创建一次性 Tree asset。

#### Scenario: 新建 TreeClip

- **WHEN** 作者在 Timeline 中创建 TreeClip
- **THEN** Clip MUST 自动拥有 inline TimelineRunningTree
- **AND** 作者 MUST 能通过双击或 Open 下钻编辑
- **AND** 创建流程 MUST NOT 弹出或要求分配 BaseTreeAsset

#### Scenario: 抽取 shared Tree

- **WHEN** 作者对 inline TreeClip 执行 Extract Shared
- **THEN** 系统 MUST 创建持有同一 Graph data 的 shared BaseTreeAsset
- **AND** TreeClip MUST 切换到 shared 引用
- **AND** 原 inline 真数据 MUST 被清理

#### Scenario: 多 playback 使用同一 TreeClip

- **WHEN** 多个 Timeline playback 同时使用同一 inline 或 shared TimelineRunningTree template
- **THEN** 每个 playback/clip runtime MUST 获得隔离工作副本
- **AND** 一个 runtime 的节点状态、ExposedProperty 临时值或 Clip context MUST NOT 污染另一个 runtime
