## MODIFIED Requirements

### Requirement: Projection Foot Analysis必须拥有独立规范身份

Projection Foot Analysis identity MUST包含其所消费artifact的canonical content hash，以及AnimationClip、Analysis Source、Sampling Rig、Calibration、采样参数和算法版本的规范identity。Library路径与Editor-only Source/Sampling Rig对象 MUST不进入Runtime Projection payload。Projection stale detector MUST按expected artifact identity与content hash判断，不得按名称、path、duration或文件时间匹配。

#### Scenario: Artifact内容变化

- **WHEN** AnimationClip或Analysis输入变化并生成新artifact
- **THEN** ProjectionRevision MUST变化且旧Projection MUST变为Stale
- **AND** Gameplay ProgramHash MUST保持不变

### Requirement: Program 与 Projection 必须在同一 Build Transaction 中发布

Character Simulation Build MUST按`Frontend artifact -> Numeric Program -> resolve exact Animation Analysis artifacts -> Presentation Projection -> identity validation -> atomic publish`执行。单clip artifact MAY在该事务之前独立生成，但Build MUST重新校验其完整identity和payload hash。任一artifact或Projection阶段失败 MUST不发布本次Program/Projection，也不得更新一半generated reference。

#### Scenario: Ready artifact被复用

- **WHEN** Build发现全部artifact Ready且精确匹配
- **THEN** Build MAY跳过AnimationClip重新采样
- **AND** Program/Projection发布事务和最终identity校验 MUST仍完整执行

#### Scenario: Artifact损坏

- **WHEN** 任一artifact存在但codec或hash校验失败
- **THEN** Build MUST失败并定位对应stable clip binding
- **AND** MUST不使用旧Projection或默认feature继续发布
