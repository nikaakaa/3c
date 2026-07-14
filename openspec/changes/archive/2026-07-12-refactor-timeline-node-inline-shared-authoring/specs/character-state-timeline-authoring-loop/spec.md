## ADDED Requirements

### Requirement: Corin TimelineNode 必须默认拥有 inline Timeline

Corin 的 Locomotion 与 Action 状态 Timeline MUST默认保存为对应 TimelineNode 私有的 inline TimelineData。只有多个节点明确复用同一 Timeline 时，作者 MAY显式 Extract Shared 或分配 shared TimelineAsset。Corin 一对一状态 Timeline MUST NOT继续保留为独立一次性 Timeline asset，也 MUST NOT通过 asset fallback 维持旧引用。

#### Scenario: 下钻 Attack1 Timeline

- **WHEN** 作者从 Attack1 State body 打开 Play Attack1 Timeline 节点
- **THEN** 独立 TimelineEditorWindow MUST绑定 Attack1 inline TimelineData
- **AND** 来源 Graph 窗口 MUST保持 Attack1 State body 可见
- **AND** TimelineEditorWindow MUST显示 Animation、Motion 和 Decision Tree tracks
- **AND** 作者从 Hit 或 Cancel TreeClip 下钻时 MUST在来源 Graph 窗口打开 inline TimelineRunningTree

#### Scenario: 下钻 locomotion Timeline

- **WHEN** 作者从 Idle、WalkStart、WalkLoop、RunStart、RunLoop、RunEnd 或 MovingTurn 状态 body 打开 TimelineNode
- **THEN** 独立 TimelineEditorWindow MUST绑定对应节点的 inline TimelineData
- **AND** 来源 Graph 窗口 MUST保持当前状态行为 Graph 可见
- **AND** 项目 MUST NOT要求对应一次性 Timeline asset 存在

#### Scenario: 显式复用 Timeline

- **WHEN** 作者决定多个状态或角色复用同一 Timeline
- **THEN** 作者 MUST通过 Extract Shared 或显式 Shared ownership 创建/选择 TimelineAsset
- **AND** owner inline 真数据 MUST被清理
- **AND** 每个 playback request MUST继续拥有独立 runtime clone

### Requirement: Corin inline Timeline 迁移必须保留 TreeClip 事实链路

Corin Attack1、Attack2、DodgeForward 和 DodgeBack Timeline 迁入 TimelineNode 后 MUST完整保留现有八个 Decision TreeClip。Hit、Cancel、IFrame 和 MoveCancel 的 frame range、phase、inline TimelineRunningTree、Blackboard declaration reference、fact projection 和 Action Context provenance MUST保持不变。迁移 MUST NOT恢复 ActionWindowTrack、ActionWindowClip、专用 Window reader 或 SubmitActionWindowSampleNode。

#### Scenario: 迁移 Attack TreeClip

- **WHEN** Attack1 和 Attack2 Timeline 从外部 asset 迁入 TimelineNode
- **THEN** Attack1Hit、Attack1Cancel、Attack2Hit 和 Attack2Cancel TreeClip MUST仍位于各自 resolved TimelineData
- **AND** TreeClip MUST继续写入同一 Root-owned Frame declarations
- **AND** WindowFactProjection MUST继续生成同一 ActionWindowSample identity

#### Scenario: 迁移 Dodge TreeClip

- **WHEN** DodgeForward 和 DodgeBack Timeline 从外部 asset 迁入 TimelineNode
- **THEN** 两个 CanDodgeMoveCancel 与两个 IFrame TreeClip MUST完整保留
- **AND** CanDodgeMoveCancel MUST保持 Projection=None
- **AND** IFrame declarations MUST保持 ActionWindow projection

#### Scenario: 删除旧 Timeline assets

- **WHEN** 11 个 Corin Timeline 的 tracks、clips、引用和 playback mode 已迁入对应 TimelineNode
- **THEN** 项目 MUST确认不存在剩余引用后删除 11 个旧 Timeline assets
- **AND** 系统 MUST NOT保留旧字段反序列化、兼容 wrapper 或 asset fallback
