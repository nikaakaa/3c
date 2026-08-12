## MODIFIED Requirements

### Requirement: Timeline pose time与显式Player time必须独立连续推进

CharacterSimulationState MUST保存Gameplay Timeline logic time。`ActionCommittedSampleHistory` MUST保存已提交的Action raw sample锚点；`ActionPresentationSampleProjector` MUST按presentation delta在锚点之间生成独立`ProjectedPresentationSampleTime`。持续Movement producer MUST从同一权威simulation tick派生`CommittedMovementPlaybackClock`，并与获胜Motion Contribution原子进入Body、Trajectory与Presentation Fact committed history。Presentation MUST只在相邻committed Movement锚点之间投影raw source time；它不得直接读取Gameplay Timeline、Locomotion operation或权威tick服务。

每个Sequence Player、Action Player与transition clock MUST只在PresentationFrame推进。新committed sample、rollback replacement或stream reset MUST按完整playback identity重基线表现投影。相同Movement owner与generation内authority tick和continuous ticks MUST单调；owner或generation改变 MUST作为显式新局部时钟接管。retained outgoing source MUST保持进入relevance时锁定的clock identity。Animancer MUST只按resolved sample descriptor采样。Projected time MUST不覆盖committed raw time，不得写回Timeline或产生Window、Motion、Warp、Cue与Action lifecycle。Marker Sync与GeneratedFootPhase MUST只从raw source time生成effective time，不拥有clock。Body Visual Trajectory Follower MUST不修改Animation sample、Player delta、Pose Plan completion或playback generation。

#### Scenario: 两个Logic Tick之间渲染

- **WHEN** PresentationFrame在下一个SimulationTick前推进
- **THEN** Action projected time、Movement projected time、SequencePlayer、Slot transition与最终visual animation sample MUST连续推进
- **AND** Timeline Gameplay state、committed raw time与Action lifecycle MUST保持不变

#### Scenario: Rollback替换Movement分支

- **WHEN** replay用新的最终Movement producer或generation替换已提交分支
- **THEN** outer transaction MUST只发布最终分支的Body、Intent与Movement clock锚点
- **AND** Presentation MUST按完整identity重基线，Sequence Player与Marker relation状态 MUST不进入rollback snapshot或network

#### Scenario: MovingTurn时钟锚点

- **WHEN** Timeline Motion Curve提交MovingTurn位移
- **THEN** 同一committed result MUST携带该Timeline owner、generation与连续playhead
- **AND** Presentation MUST不从Locomotion Input elapsed或当前Action sample推断MovingTurn raw time
