## ADDED Requirements

### Requirement: Fantasy Endpoint 必须实现 ServerAuthoritative 模型合同

系统 MUST 提供真实 `FantasyServerAuthoritativeEndpoint`，负责在 `ServerAuthoritativePacket` 与生成的 Fantasy C2G/G2C message 之间做强类型映射。Endpoint MUST 实现与 LocalLoopback 相同的模型专属 endpoint 合同，MUST NOT 直接访问 CharacterPipeline、ActionRuntime、Graph、Timeline、MotionStage、PresentationStage 或 Unity Transform。

#### Scenario: 发送 Owner MotionCommand

- **WHEN** ServerAuthoritativeHybridSession 将 Owner MotionCommand 交给 Fantasy endpoint
- **THEN** endpoint MUST 生成对应 C2G message并通过当前唯一 Fantasy Session 发送
- **AND** Character adapter MUST 不认识生成 message 类型

#### Scenario: 接收 MotionCorrection

- **WHEN** Fantasy Handler 收到 G2C MotionCorrection
- **THEN** Handler MUST 将其映射为正式 ServerAuthoritative packet并放入 endpoint incoming
- **AND** Handler MUST 不直接移动角色或调用 MotionStage

### Requirement: Fantasy 协议必须由唯一 Outer proto 生成

Join/roster、MotionCommand、MotionSnapshot、MotionCorrection、CorrectionAck、ActionDecision 和 ActionReplication MUST 定义在正式 Outer proto，并通过 ProtocolExportTool 生成 client/server C#。系统 MUST NOT 手写生成文件，也 MUST NOT 保留旧 FrameSync proto、opcode、message alias 或兼容 parser。

#### Scenario: 导出协议

- **WHEN** 运行 ProtocolExportTool
- **THEN** server generated code MUST 写入 Server Entity 程序集
- **AND** client generated code MUST 写入 GameProto 程序集
- **AND** GameLogic endpoint/Handler MUST 直接引用生成类型

### Requirement: Fantasy Endpoint 必须唯一拥有客户端 Session

每个 Unity 客户端 gameplay session MUST 只有一个 Fantasy endpoint 与一条 Fantasy Session。连接 MUST 显式配置 endpoint、KCP、connect timeout、heartbeat 和容量；缺失配置 MUST 明确失败。系统 MUST NOT 为每个远端 Character 创建 Session，也 MUST NOT 使用静态 SessionFacade 拥有 gameplay 连接。

#### Scenario: 两个客户端连接

- **WHEN** 两个独立 Unity 客户端启动
- **THEN** 每个进程 MUST 各自建立一条到本地服务端的 Fantasy Session
- **AND** 每条 Session MUST 同时承载该端 Owner 与远端 actor 的模型消息

#### Scenario: 连接断开

- **WHEN** Fantasy Session 断开
- **THEN** endpoint MUST 保存明确断开原因并清理 Session component/queue
- **AND** model session MUST 清理对应 roster/binding 状态
- **AND** MUST 不自动重连或回退 Loopback

### Requirement: Fantasy Handler 必须保持纯协议边界

Unity 与 Server Fantasy Handler MUST 只做 Session ownership 检查、生成 message 与模型/Room command 的转换、入队、reply 或 push。Unity Handler MUST NOT 操作 GameObject；Server Handler MUST NOT 运行 Unity Graph、Timeline、Animancer 或客户端 Character 逻辑。

#### Scenario: Unity 收到远端动作

- **WHEN** Unity Handler 收到 G2C ActionReplication
- **THEN** 它 MUST 只映射为 endpoint incoming packet
- **AND** 对应 Character binding MUST 在正式 tick 边界消费

#### Scenario: 服务端收到移动

- **WHEN** Server Handler 收到 C2G MotionCommand
- **THEN** 它 MUST 从 Session actor component 解析 ownership并将 command 入队
- **AND** canonical pose MUST 由 Room tick 更新
