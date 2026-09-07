## MODIFIED Requirements

### Requirement: Demo 必须使用两个Unity Client与一个纯.NET Dedicated Relay Server

Demo MUST保留两个独立 Unity Client 与一个纯.NET Dedicated Relay Server 的 Gameplay 组合，并额外启动一个独立开发 GM 服务。两端 MUST加载相同 deterministic identity 和 stable actor roster。Relay MUST只拥有既有网络职责及工具只读查询桥，不执行 Gameplay Program、KCC、Presentation 或 Unity Scene。Player MUST使用现行共享 GameplayLab 场景和显式 Rollback Variant，不恢复旧 Bootstrap/Peer Scene 分裂入口。

#### Scenario: 双端开始模拟

- **WHEN** Client A、B 完成正式 handshake
- **THEN** Relay MUST先校验 deterministic identities，再允许 Tick 推进
- **AND** GM MUST不参与 gameplay handshake、canonical input、rollback history 或 hash

#### Scenario: 启动开发产品

- **WHEN** 作者运行已构建产品
- **THEN** MUST启动 GM、Relay、Client A、Client B 四个进程
- **AND** 只有两个进程是 Unity Player

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

#### Scenario: Demo 使用选择性输入时序

- **WHEN** 双Client开始推进Rollback Session
- **THEN** 连续移动与Immediate request MUST使用0 Tick模型延迟
- **AND** Corin Offensive request MUST使用2 Tick延迟
- **AND** confirmed frontier MUST使用独立confirmation delay

#### Scenario: Demo 启动产品

- **WHEN** 作者运行已经构建的DeterministicRollback network test product
- **THEN** Run MUST启动Dedicated Relay Server、独立GM、Client A Player与Client B Player
- **AND** 进程列表 MUST只有两个Unity Player

#### Scenario: 旧 Unity Host 资产进入产品

- **WHEN** Build scene closure、manifest或启动参数包含Canonical Host Scene或Host Player role
- **THEN** Build MUST失败
- **AND** MUST不把旧Host保留为fallback
