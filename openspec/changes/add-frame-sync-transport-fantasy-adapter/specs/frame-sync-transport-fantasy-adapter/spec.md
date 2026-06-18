## ADDED Requirements

### Requirement: 帧同步 Transport Port
系统 MUST 定义帧同步 transport port，使 fake transport 与 Fantasy adapter 能通过同一纯数据边界收发 handshake、input、confirmed input set、checksum、correction 和 diagnostic 消息。Transport port MUST NOT 拥有角色模拟、rollback 算法、Action 解释或 snapshot 读写职责。

#### Scenario: Fake 与 Fantasy 共用 Port
- **GIVEN** 系统存在 fake transport 和 Fantasy adapter
- **WHEN** 二者接入 frame sync core
- **THEN** 二者 MUST 实现同一 transport port
- **AND** frame sync core MUST NOT 因 transport 类型不同而改变 prediction 或 rollback 算法

#### Scenario: Port 不引用 Fantasy
- **WHEN** 检查 transport port core
- **THEN** 它 MUST NOT 引用 Fantasy Session、Entity、Scene 或 Handler 类型

#### Scenario: Port 不推进 Gameplay
- **WHEN** transport 收到 confirmed input set 或 correction
- **THEN** transport MUST 只投递纯数据事件
- **AND** MUST NOT 直接调用 `CharacterFramePipeline`、`ILocalRollbackSynctestSimulation` 或 `CharacterController.Move`

### Requirement: Fantasy Adapter 边界
系统 MUST 将 Fantasy 接入限制在 adapter 层。Fantasy Handler MUST 只负责协议 DTO 校验、session/player 绑定、room input queue 写入、confirmed input set 广播、checksum/correction 转发和诊断输出。

#### Scenario: Handler 不推进角色
- **WHEN** Fantasy Handler 处理客户端输入
- **THEN** Handler MUST NOT 创建或推进服务端角色控制器
- **AND** MUST NOT 调用 Locomotion、Action 或 Character frame runtime

#### Scenario: Unity push handler 只入队事件
- **WHEN** Unity 客户端收到 confirmed input set push
- **THEN** push handler MUST 将 DTO 转为 transport event
- **AND** MUST NOT 在 callback 中直接 restore、replay 或写 Transform

### Requirement: Fake Transport 合同测试
系统 MUST 提供 fake transport，用于在不启动 Fantasy 进程的情况下验证 input submit、confirmed input set、latency、reorder、duplicate、missing、late input 和 correction 注入。

#### Scenario: Fake room 产出 confirmed input set
- **GIVEN** 多个 fake client 向 fake room 提交同 tick 输入
- **WHEN** fake room 确认该 tick
- **THEN** fake transport MUST 产出与 Fantasy adapter 相同合同的 confirmed input set

#### Scenario: Fake transport 不模拟角色
- **WHEN** fake transport 运行
- **THEN** 它 MUST NOT 创建角色 runtime、状态机 runner、motion executor 或 animation presenter

### Requirement: Fantasy Protocol Export 验证
系统 MUST 将 Fantasy protocol export 和 server build 作为后续实现验收的一部分。协议生成文件 MUST 由导出工具生成，不得手动修改 `.g.cs` 或导出产物。

#### Scenario: 协议导出
- **WHEN** frame sync Fantasy protocol 被定义
- **THEN** 系统 MUST 能运行 protocol export
- **AND** 生成客户端和服务端消息代码

#### Scenario: Handler 编译
- **WHEN** Fantasy Handler 被实现
- **THEN** server build MUST 通过
- **AND** Handler MUST 使用 Fantasy source generator 注册
