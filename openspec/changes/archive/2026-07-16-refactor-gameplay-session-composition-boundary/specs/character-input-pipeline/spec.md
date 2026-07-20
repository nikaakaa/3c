## REMOVED Requirements

### Requirement: 输入历史只属于需要预测重放的 Model Driver

**Reason**: 旧 Driver类型删除，history所有权改由具体 Model Source或有状态 Pipeline Pass显式承担。

**Migration**: Local Source与 Standard Local Pipeline不保存 replay history；模型 history必须声明 ExternalSource或 SnapshotParticipant状态所有权。

## ADDED Requirements

### Requirement: 输入历史只属于需要预测重放的 Model Source 或 Pass

Input history MUST不再由公共 CharacterInputStage、Program Runtime或标准 Pipeline默认拥有。Local Session Source与 Standard Local Pipeline MUST不创建 replay history；需要 prediction/replay的后续 Network Model MUST在自己的 Source或明确有状态 Pipeline Pass中保存 portable CharacterSimulationInput history，并声明 ExternalSource或 SnapshotParticipant所有权。

#### Scenario: Local Pipeline 提交输入

- **WHEN** Standard Local Pipeline完成本次 SimulationStep
- **THEN** Core MUST不创建 model history或假 rollback buffer
- **AND** Local Source MUST不保留未声明的 replay state

