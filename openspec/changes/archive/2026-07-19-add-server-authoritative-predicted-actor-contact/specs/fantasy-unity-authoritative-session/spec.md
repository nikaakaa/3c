## MODIFIED Requirements

### Requirement: Simulation、Command、Snapshot与Remote采样策略必须独立

`SimulationTickRate`、`CommandPacketRate`、`SnapshotPacketRate`、`CommandSlackTicks`、`MaximumRemoteBodyExtrapolationTicks`和`MaxGameplayDatagramBytes` MUST是独立、进入模型configuration identity的策略。系统 MUST删除统一`ObservationCadenceTicks`与Remote Presentation独立Body delay，MUST不让任一传输频率改变Program或WorldSolver固定步进。

#### Scenario: Corin使用正式Demo频率

- **WHEN** `SimulationTickRate=60`、`CommandPacketRate=30`且`SnapshotPacketRate=20`
- **THEN** Program、Pipeline和WorldSolver MUST按60Hz推进
- **AND** command与snapshot MUST分别按自己的packet cadence发送
- **AND** Prediction Schedule MUST按正式Remote Body采样策略选择target tick
- **AND** Remote Presentation MUST消费Schedule提交的selected Body

#### Scenario: Prediction建立Remote Body anchor

- **WHEN** Data Plane已Ready但locked remote roster尚无合法Body anchor
- **THEN** Prediction Schedule MUST保持RemoteObservationPriming并产生零Current step
- **AND** 首个完整anchor集合到达后 MUST按目标tick选择Body
- **AND** Remote Presentation MUST不建立独立authority Body cursor或delay
