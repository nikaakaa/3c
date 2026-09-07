## MODIFIED Requirements

### Requirement: Demo 必须使用两个Unity Client与一个纯.NET Dedicated Relay Server

Player MUST使用唯一共享 GameplayLab 场景和显式 Rollback Variant，不恢复旧 Bootstrap／Peer Scene 分裂入口。

每个Demo Run MUST从一个精确DeterministicRollback Candidate启动两个独立Unity Client、一个纯.NET Dedicated Relay和一个独立开发GM。两端 MUST加载相同CandidateId、Model、SemanticHash、Fixed ProgramHash、TickRate、CollisionWorldHash、KCC identity和stable actor roster。Relay MUST只拥有网络职责和GM窄只读查询桥，不执行Gameplay Program、KCC、Presentation或Unity Scene；GM MUST不参与gameplay handshake、canonical input、rollback history或hash。不同Slot中的Demo Run MUST拥有独立RunId、SessionId、endpoint、token、进程与日志。

#### Scenario: 双端开始模拟

- **WHEN** 一个Run的Client A/B完成精确Candidate与Session handshake
- **THEN** Relay MUST校验全部deterministic和Run identities后才允许SimulationTick推进
- **AND** GM MUST只查询该Run的Relay快照

#### Scenario: Demo 使用选择性输入时序

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

#### Scenario: 启动开发产品

- **WHEN** 作者从正式 Candidate 入口启动本次开发 Run
- **THEN** MUST由该 Candidate 的工具与 Session Plan 启动 GM、Relay、Client A、Client B
- **AND** 四个角色 MUST绑定同一 Candidate／Run／Session，只有两个进程是 Unity Player

#### Scenario: 工具访问四个只读命令

- **WHEN** 作者在独立 GM 进程的文本控制台提交正式查询
- **THEN** 独立 GM MUST通过 Relay 查询桥获得该会话事实
- **AND** MUST不修改移动、Offensive 延迟、最大预测领先量或表现链路

#### Scenario: 旧 Unity Host 进入产品

- **WHEN** Scene closure、manifest 或参数包含旧 Canonical Host 或 Host Player role
- **THEN** Build MUST失败，不保留 fallback

#### Scenario: 两端 Handshake

- **WHEN** Client A 与 Client B 加入 Demo
- **THEN** Relay Server MUST校验全部deterministic identities后才允许SimulationTick推进
- **AND** Server MUST不加载Fixed Program或Collision World内容

#### Scenario: Demo 启动产品

- **WHEN** 作者选择已发布的精确 Rollback Candidate 与合法 Slot
- **THEN** Run MUST启动该候选的 Dedicated Relay、独立 GM、Client A 与 Client B
- **AND** 只有两个进程是 Unity Player，Run MUST不重新 Build

#### Scenario: 旧 Unity Host 资产进入产品

- **WHEN** Build scene closure、manifest或启动参数包含Canonical Host Scene或Host Player role
- **THEN** Build MUST失败
- **AND** MUST不把旧Host保留为fallback
