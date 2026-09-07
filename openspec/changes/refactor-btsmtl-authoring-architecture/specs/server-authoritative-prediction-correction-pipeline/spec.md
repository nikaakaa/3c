## MODIFIED Requirements

### Requirement: Authority Baseline必须覆盖完整Owner Gameplay恢复状态

网络层 MUST以ProgramHash/LayoutHash锁定的Full/Delta Network Checkpoint表达owner权威状态。Client MUST先通过dense layout重建并校验完整committed Character state（包含代码控制状态、ActionInstance、全部活动或停止中的技能调用状态及既有GE／Equipment）、owner body/world baseline、SimulationTick、NumericProfile、Target ABI、checkpoint schema、state/body hash、confirmed input sequence和confirmed EventId horizon，再产生`AuthoritativeActorBaseline`供Correction使用。Routine snapshot MUST不直接携带完整State codec bytes；仅包含position/yaw、motion delta或Animation state的消息 MUST不得用于gameplay reconciliation。

#### Scenario: 收到Pose-only Snapshot

- **WHEN** Observation缺少完整Character state或Program/Layout identity
- **THEN** Correction Schedule MUST拒绝其作为restore baseline
#### Scenario: 恢复嵌套技能

- **WHEN** 权威baseline对应owner正在运行多层子图的Tick
- **THEN** 客户端 MUST重建全部调用frame并通过原Correction Schedule重算
- **AND** MUST不只修正当前动作ID或Timeline时间


### Requirement: Prediction与Authority失败必须保持Session事务边界

任一baseline decode、restore、Replay step、WorldSolve、Finalize、history capture或OutputDisposition失败时，当前outer transaction MUST不发布部分Character/World/Pipeline state或外部output。Authority worker、Fantasy connection或reliable queue失败时Session MUST fail-stop，MUST不切换Local Pipeline、旧Driver或client pose authority。

#### Scenario: Replay中WorldResult不匹配

- **WHEN** Replay 102收到错误Solver identity
- **THEN** Backend MUST拒绝整个outer transaction
- **AND** Replay 101产生的表现与网络输出 MUST不被提交
#### Scenario: 代码模块身份不一致

- **WHEN** Prediction与Authority使用不匹配的控制模块或技能目录
- **THEN** 正式身份校验 MUST拒绝组合，不能用位置修正掩盖版本差异
