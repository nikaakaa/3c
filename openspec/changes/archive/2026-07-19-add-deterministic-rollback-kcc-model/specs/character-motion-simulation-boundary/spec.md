# character-motion-simulation-boundary Specification

## MODIFIED Requirements

### Requirement: 确定性模拟必须属于独立完整 Network Model

Deterministic KCC、CollisionWorldArtifact、canonical input bundle、Fixed Program/State/Kernel、Fixed `SimulationWorldStateSet/WorldSimulationState/SimulationWorldSnapshot` history、restore/replay、state hash、snapshot recovery 和 side-effect commit MUST共同属于完整 DeterministicRollback Network Model。该模型 MUST从与 Float32 模型相同的 validated Semantic IR artifact 生成独立 Fixed ABI，但 MUST不复用 Float32 CharacterSimulationProgram/SimulationKernel，也 MUST不使用 Unity CharacterController、DotRecast 或 ServerAuthoritative correction 作为 deterministic world execution。

#### Scenario: 完整安装 Rollback Model

- **WHEN** ModelDefinition、Endpoint、History、KCC、Collision World、Replay、Hash、Recovery 和 Committer 全部可用
- **THEN** DeterministicRollback MAY出现在 SessionHost authoring UI
