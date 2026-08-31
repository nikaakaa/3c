## MODIFIED Requirements

### Requirement: Rollback Dedicated Relay Server必须是纯.NET网络产品

系统 MUST提供受版本控制的`ThirdPerson.DeterministicRollback.Server` .NET 8 executable。该产品 MUST只引用既有 portable Core、Fixed identity、DeterministicRollback protocol、Endpoint/Relay runtime 及独立开发只读查询桥所需的 .NET HTTP 和查询合同。MUST不引用Unity、Fantasy、ServerAuthoritative、DotRecast、Animancer、Editor 或 GM 命令处理器程序集，不加载 Scene、Asset、Character Program 或 Collision World 内容。

#### Scenario: Relay 查询运行状态

- **WHEN** 独立 GM 服务提交已认证的只读查询
- **THEN** Relay 查询桥 MUST 将读取排队到 Relay 运行线程并返回有身份的快照
- **AND** 网络等待 MUST 不阻塞 Relay Pump，不增加 Gameplay 执行权威

### Requirement: Rollback Network Test Product必须包含精确Server Closure

Rollback adapter MUST通过公共 artifact 合同发布 Unity Player、Dedicated Relay Server 和独立 GM Server。ProductRoot MUST包含`Player`、`Server`和`Gm`，全部 executable、依赖和配置 MUST进入 schema v2 exact closure。Relay gameplay manifest 保持现有身份语义，新增 Relay 查询 manifest、GM manifest、Player 工具连接 manifest MUST精确绑定同一 BuildId 和 SessionId，不将工具访问配置纳入 Gameplay hash。

#### Scenario: 构建包含 GM 的产品

- **WHEN** 作者执行 Rollback Build
- **THEN** MUST 原子发布三个 artifact 和开发访问配置
- **AND** Run MUST拒绝缺失、被修改或混用不同 Build 的工具配置，不生成配置修复产物

### Requirement: Rollback Run必须只启动一个Dedicated Relay Server与两个Unity Client

Rollback Development Run MUST启动一个 Dedicated Relay Server、一个独立 GM Server 与两个 Unity Client，使用`thirdperson.runtime-topology.deterministic-rollback.gm-relay-two-peers.v1`。MUST依次验证 Relay 和 GM 的工具身份，再启动显式 peer profile 的客户端；只有两个进程是 Unity Player。MUST不支持旧三进程 topology、Unity Host 或运行时构建/配置生成。启动失败 MUST清理本次创建的进程，不清理其它会话。GM 故障 MUST只让工具不可用，Relay 故障 MUST保持既有 Session 失败行为。

#### Scenario: GM 启动失败

- **WHEN** 本次 Run 的 GM endpoint、凭据或目标身份不合法
- **THEN** MUST 明确失败并回收本次已启动进程
- **AND** MUST不启动没有有效工具产品的客户端或搜索另一服务

#### Scenario: 运行中 GM 退出

- **WHEN** Gameplay 已运行而 GM 服务退出
- **THEN** 控制台 MUST报告连接不可用
- **AND** Relay 和客户端 MUST继续按既有模型推进，不把工具断线当成 Gameplay 断线
