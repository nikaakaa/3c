## ADDED Requirements

### Requirement: Motion Matching轨迹必须使用表现帧正式Source与时间

`CharacterMotionMatchingPresentationModule` MUST按已提交Body的正式SourceMode从内部Accepted Intent或Selected Body adapter消费与Character Body presentation cursor一致的source frame，并以PresentationFrame delta和编译horizon生成Trajectory Envelope。Factory、Host与网络模型 MUST不装配另一份trajectory source。Body visual correction MAY影响可见body pose，但 MUST不改写accepted intent、candidate root feature或Gameplay state。Runtime MUST在diagnostics区分target body、visible body、intent与envelope。

#### Scenario: Visual correction正在收敛

- **WHEN** VisualRoot仍在向新的Committed Body收敛
- **THEN** MM query MUST使用正式trajectory source和同帧Body reset identity
- **AND** MUST不因PositionError直接修改Gameplay movement或selected Clip速度

#### Scenario: 两个Logic Tick之间多次表现帧

- **WHEN** PresentationFrame多次发生而Simulation没有新Tick
- **THEN** trajectory envelope与MM plan MAY按表现时间连续推进
- **AND** MUST不产生额外motion request、state mutation或network fact

### Requirement: Motion Matching历史必须随Body分支原子重置

Body `ResetSequence`、Committed branch replacement、Selected stream reset、Rollback presentation replacement、Presentation reset与Projection replacement MUST在下一次MM query前清除trajectory history、pose history、selection plan和protected contact。Reset MUST只修改Presentation-owned状态，不恢复或回卷CharacterSimulationState。

#### Scenario: Committed分支被替换

- **WHEN** CharacterBodyPresentationRuntime提升ResetSequence
- **THEN** MM Runtime MUST在同一PresentationFrame观察新sequence并进入Initialization
- **AND** MUST不查询旧分支Pose History

### Requirement: Remote Motion Matching必须服从Selected Body horizon

Observed Actor的MM trajectory source MUST消费与Remote Body Presentation相同的selected interval、tick、age与reset，不得从更晚未选中snapshot、最新packet或Scene Transform构造未来轨迹。Remote uncertainty MUST通过Trajectory Envelope tolerance/confidence表达。

#### Scenario: Prediction选择较旧Remote Body tick

- **WHEN**当前接触约束选择Tick T的Remote Body作为观察流
- **THEN** Remote MM MUST同样以Tick T interval生成trajectory source
- **AND** MUST不使用队列中Tick T+1的未选中Body
