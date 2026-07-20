# character-state-timeline-authoring-loop Specification

## MODIFIED Requirements

### Requirement: Corin TimelineNode 必须默认拥有 inline Timeline

Corin 的 Locomotion 与 Action 状态 Timeline MUST 默认保存为对应 TimelineNode 私有的 inline TimelineData。只有多个节点明确复用同一 Timeline 时，作者 MAY 显式 Extract Shared 或分配 shared TimelineAsset。Compiler MUST 将 inline/shared resolved Timeline 编译为同一不可变 Program/Projection 合同；每个 playback MUST 只在 CharacterSimulationState 中获得独立 activation state，不得创建 TimelineData 或 TimelineRunningTree runtime clone。

#### Scenario: 下钻 Attack1 Timeline

- **WHEN** 作者从 Attack1 State body 打开 Play Attack1 Timeline 节点
- **THEN** 独立 TimelineEditorWindow MUST 绑定 Attack1 inline TimelineData
- **AND** 来源 Graph 窗口 MUST 保持 Attack1 State body 可见
- **AND** TimelineEditorWindow MUST 显示 Animation、Motion 和 Decision Tree tracks
- **AND** 作者从 Hit 或 Cancel TreeClip 下钻时 MUST 在来源 Graph 窗口打开 inline TimelineRunningTree authoring data

#### Scenario: 下钻 locomotion Timeline

- **WHEN** 作者从 Idle、WalkStart、WalkLoop、RunStart、RunLoop、RunEnd 或 MovingTurn 状态 body 打开 TimelineNode
- **THEN** 独立 TimelineEditorWindow MUST 绑定对应节点的 inline TimelineData
- **AND** 来源 Graph 窗口 MUST 保持当前状态行为 Graph 可见
- **AND** 项目 MUST NOT 要求对应一次性 Timeline asset 存在

#### Scenario: 显式复用 Timeline

- **WHEN** 作者决定多个状态或角色复用同一 Timeline
- **THEN** 作者 MUST 通过 Extract Shared 或显式 Shared ownership 创建/选择 TimelineAsset
- **AND** owner inline 真数据 MUST 被清理
- **AND** 每个 playback request MUST 继续拥有独立 Program state address
