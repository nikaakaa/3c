## MODIFIED Requirements

### Requirement: Demo 必须复用 Corin 同一 Gameplay Semantic Artifact

两个Peer MUST从完成迁移的同一Corin CharacterPipelineDefinition与同一validated Semantic IR生成唯一Fixed Program和target-neutral Presentation Projection。显式Float32与Fixed发布结束后 MUST共享同一Program identity、SourceRevision和SemanticHash，ProgramHash与ABI MAY按数值目标不同。Fixed包装产物 MUST只写入`Assets/Configs/Simulation/DeterministicRollback/Programs/CorinFixedProgram.asset`；Composition引用的`CorinFixedProgramRuntime.asset` MUST保持`FixedProgramRuntimeDefinition`且不得作为包装产物目标。Gameplay Lab MUST从精确Fixed Program创建Presentation Contract并直接校验Projection，不得使用Float32专用发布元数据代替Fixed闭包校验。Gameplay Lab Local Fixed Variant与DeterministicRollback Variant MUST引用同一ProgramHash、ProjectionRevision、KccId和CollisionWorldHash；两者只可在Session Source与Network Model装配上不同。Rollback Build adapter MUST只消费正式Rollback Variant，不得重新编译Character、复制Projection、重建KCC或生成第二collision artifact。

#### Scenario: Local Fixed与Rollback Variant对账

- **WHEN** 作者准备DeterministicRollback Product
- **THEN** Build MUST确认两个Variant引用相同Fixed Program、Projection、KCC与collision artifact identity
- **AND** 任一identity分裂 MUST在Player Build前失败

#### Scenario: Character authoring发生变化

- **WHEN** Corin Document v3、BTSMTL、Timeline、Profile或Pose Graph revision变化
- **THEN** 作者 MUST先通过精确Definition显式发布新的Float32 Program、Fixed Program与Projection
- **AND** Float32与Fixed产品 MUST拥有相同SourceRevision与SemanticHash
- **AND** Rollback Product Build MUST拒绝旧Variant或stale generated product

#### Scenario: Fixed包装产物与运行定义混用

- **WHEN** Fixed发布目标指向`CorinFixedProgramRuntime.asset`或该资产不再是`FixedProgramRuntimeDefinition`
- **THEN** Gameplay Lab与Rollback产品准备 MUST在装配前失败
- **AND** MUST不创建兼容wrapper或从目录中猜测替代资产

### Requirement: Demo 必须限制并明确世界能力范围

Gameplay Lab场景中唯一`DeterministicCollisionWorldAuthoring`及其显式surface marker MUST同时作为Local Fixed与DeterministicRollback可见环境的唯一作者源。显式Bake MUST生成两个Variant共同引用的规范Collision Artifact；Rollback adapter、Player Build与Run MUST不创建、复制或修复第二份世界数据。

#### Scenario: 两个Variant引用不同collision artifact

- **WHEN** Local Fixed与Rollback Variant的CollisionWorldHash或artifact identity不同
- **THEN** 产品准备 MUST失败并报告两个精确引用
- **AND** MUST不选择较新的artifact或自动重新Bake
