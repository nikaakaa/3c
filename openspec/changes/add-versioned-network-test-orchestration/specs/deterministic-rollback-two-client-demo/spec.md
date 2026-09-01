## MODIFIED Requirements

### Requirement: Demo 必须使用两个Unity Client与一个纯.NET Dedicated Relay Server

每个Demo Run MUST从一个精确DeterministicRollback Candidate启动两个独立Unity Client、一个纯.NET Dedicated Relay和一个独立开发GM。两端 MUST加载相同CandidateId、Model、SemanticHash、Fixed ProgramHash、TickRate、CollisionWorldHash、KCC identity和stable actor roster。Relay MUST只拥有网络职责和GM窄只读查询桥，不执行Gameplay Program、KCC、Presentation或Unity Scene；GM MUST不参与gameplay handshake、canonical input、rollback history或hash。不同Slot中的Demo Run MUST拥有独立RunId、SessionId、endpoint、token、进程与日志。

#### Scenario: 双端开始模拟

- **WHEN** 一个Run的Client A/B完成精确Candidate与Session handshake
- **THEN** Relay MUST校验全部deterministic和Run identities后才允许SimulationTick推进
- **AND** GM MUST只查询该Run的Relay快照

#### Scenario: Demo使用选择性输入时序

- **WHEN** 一个Run的双Client开始推进Rollback Session
- **THEN** 连续移动与Immediate request MUST使用0 Tick模型延迟，Corin Offensive request MUST使用2 Tick延迟
- **AND** confirmed frontier MUST继续使用独立confirmation delay

#### Scenario: 启动两个开发Session

- **WHEN** 作者用两个不同Slot启动两份Candidate
- **THEN** 每个Run MUST各自启动Relay、GM、Client A、Client B四个进程
- **AND** 两个Run MUST不共享endpoint、token、mutable runtime或日志目录

#### Scenario: 旧Unity Host或固定Product入口进入候选

- **WHEN** Candidate Session Plan、Scene closure或启动参数包含Canonical Host、Host Player、固定ProductRoot或StopExisting
- **THEN** Build或Run MUST失败
- **AND** MUST不保留旧入口作为fallback
