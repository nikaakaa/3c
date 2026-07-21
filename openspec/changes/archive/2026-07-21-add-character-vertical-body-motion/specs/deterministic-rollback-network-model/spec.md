## ADDED Requirements

### Requirement: Rollback必须恢复确定性垂直动力状态

Deterministic Rollback完整Fixed `SimulationWorldSnapshot`、History、WorldStateHash、分层desync hash与Snapshot Recovery MUST包含每个Actor的VerticalVelocity。Restore与Replay MUST同时恢复Body pose、actual Velocity、VerticalVelocity、Grounded、Collision和KCC stable support state，并在下一Tick执行唯一Fixed Body Motion Prepare。旧snapshot或缺失VerticalVelocity的payload MUST被拒绝，MUST不按当前KCC Grounded或actual Velocity.Y重建。

#### Scenario: Late Input回退到自由落体Tick

- **WHEN** Late Input使Peer回退到Actor处于自由落体的历史Tick
- **THEN** Restore MUST恢复该Tick完整VerticalVelocity
- **AND** Replay MUST对相同input和world contact产生相同后续轨迹与Hash

