## Context

本轮只做用户已确认的 Rollback GM 控制台。服务端负责命令准入和处理，客户端界面负责输入和展示；不提前实现玩法修改、采样或诊断系统。

当前 `ThirdPerson.DeterministicRollback.Server` 只创建 `RollbackInputRelayRuntime`。现有 manifest 和 runtime 提供会话/模型身份、预期角色名单、名单锁定状态、输入前沿、confirmed frontier 与网络计数，足以支撑首批只读命令。

## Goals / Non-Goals

- 跑通输入、解析、服务端校验、分发、执行和结果展示。
- 每条命令独立实现，新增命令通过正式注册接入，不改控制台业务分支。
- 保持 Rollback Gameplay 模型、8 Tick 预测配置、Action 和 IK 不变。
- 不扩大为多模型 GM，不迁移采样/诊断代码，不注册未来功能的空处理器。

## Decisions

### 1. 明确输入输出和责任

| 模块 | 输入 | 输出 | 不负责 |
| --- | --- | --- | --- |
| 客户端控制台 | 作者文本、窗口焦点、服务端响应 | 命令请求、连接状态、命令历史和结果 | 最终授权、具体命令实现、读取角色内存代替服务端 |
| 文本解析与合同 | 命令文本及明确参数格式 | 命令名、参数、语法错误 | 求值 C# 或调用任意方法 |
| 服务端命令目录与分发 | 已认证请求、命令定义、目标会话 | 处理器调用或明确拒绝 | 根据 UI 声明决定权限、为每个命令增加分发器业务分支 |
| 独立命令处理器 | 经过校验的参数、只读查询端口 | 对应结果 DTO | 修改同步状态、保存第二份 Relay 状态 |
| 查询适配器 | Relay 正式配置与运行时查询 | 有来源身份和时序的只读快照 | 扫描场景、反射私有字段、猜测客户端状态 |

客户端可以提前发现文本语法错误，但服务端必须按同一正式合同再次验证，不能信任客户端验证结果。命令文本不是脚本。

### 2. 首批四类命令

| 命令 | 参数 | 服务端返回 |
| --- | --- | --- |
| `help [command]` | 可选命令名 | 已安装命令、说明、参数和用法；指定命令不存在时明确拒绝 |
| `session.info` | 当前明确连接的服务/会话 | 服务运行身份、业务 SessionId、Build/Model/Protocol/Program 身份、TickRate、最大预测领先量等非敏感配置 |
| `actor.list` | 当前明确连接的会话 | 预期 Peer/Player/Actor 名单、服务端实际名单锁定状态和已有输入前沿；不能把 manifest 中预期成员冒充已连接成员 |
| `runtime.status` | 当前明确连接的会话 | 服务端接收/发送量、input/forward/dedupe/invalid、canonical/confirmed 前沿、可靠消息积压和 dropped 等已有事实 |

`runtime.status` 不声称查看客户端 FPS、最终骨骼、IK、角色属性或完整 Action 状态。将来需要这些数据时再安装正式客户端查询/采样能力。

清空控制台显示属于本地 UI 操作，不作为服务端 GM 命令，也不删除服务端日志或记录。命令历史和输出使用有界容量，绘制只读缓存结果。

### 3. 命令扩展边界

命令定义至少包含稳定 Id/版本、名称、说明、参数合同、所需开发权限和结果合同。每条命令有独立处理器，通过服务端装配根显式注册；首批处理器只依赖只读 Relay 查询端口。

控制台、解析器和分发器不包含 `session.info` 或 `actor.list` 的业务实现，不扫描程序集发现可调用方法。未知命令、未知参数和未安装处理器明确失败，不尝试近似名称或本地执行。

未来添加玩法 GM 或采样控制时，使用同一命令注册入口，但必须先补对应执行合同。特别是 Rollback 玩法修改需要统一执行 Tick、canonical 记录与 replay/hash 语义，不能仅注册一个会改单端内存的处理器。

### 4. 连接、请求和结果

控制台显式连接配置指定的 GM 服务，展示服务运行身份、会话和连接状态。服务不可用时保留明确失败，不搜索其它 endpoint、不按场景或 Actor 名字换目标、不退回客户端执行。

请求携带请求 Id、服务/会话运行身份、命令 Id/版本和参数。服务端验证开发访问权限、运行身份、命令和参数；响应携带同一请求 Id、结果状态与 typed payload。只读命令也记录接受/拒绝及结果，不记录访问凭据。

解析失败、未知命令、参数错误、无权限、目标已结束、超时和执行失败必须能区分。请求已发出或被接受不等于执行完成；断线/超时后不能显示成功。新服务运行实例不得接管旧实例的未完成请求。

消息、在途请求、命令历史与输出均有明确容量。网络请求不能阻塞 Unity 主线程；绘制回调不做 Build、文件扫描、完整状态序列化或其它重操作。只读状态通过 Relay 的正式边界读取，不在网络线程直接遍历其可变集合。

首版只声明开发工具访问配置，不引入完整商业账号系统。具体工具传输、endpoint、凭据交付与服务宿主一起确定，并写入正式产品配置；不能把已有 gameplay peer identity 直接当作 GM 授权。

### 5. Rollback 模型保持不变

这些查询不改变 Gameplay，因此不进入 canonical input、Simulation Tick、rollback history、state hash 或 Presentation。GM 所处服务端进程不因此获得角色模拟权威。

`RollbackInputRelayRuntime` 继续只负责网络模型职责。GM 独立模块通过窄只读端口查询实际状态，不加载 Character Program、KCC、Unity Scene 或 Animancer，不依赖 Fantasy。

### 6. 游戏内输入焦点

控制台使用正式开发 UI 和 Input System 配置，只装配到当前 Rollback 开发入口。共享设备适配层可提供焦点边界，但不向 Local Float32/Fixed 场景顺便新增 GM。

控制台获得交互焦点时，本地设备适配器按 Program input catalog 生成 neutral gameplay 输入，不产生 Attack/Dodge 请求，相机不消费 UI 鼠标。已有 committed request、输入历史和网络队列不变，Session 继续运行。

释放焦点后由原适配器恢复输入，不补发控制台期间的按键。关闭窗口只释放 UI 资源与焦点，不改变服务端会话。

### 7. 尚未选定的服务端宿主

| 方案 | 业务价值 | 成本及必须修改的合同 |
| --- | --- | --- |
| 现有 Relay 产品进程内的独立 GM 模块 | 少一个进程，能直接通过受控只读端口取得 Relay 状态 | 与 Relay 共用进程生命周期；需要修改 Relay 产品允许的模块/依赖、入口配置和 endpoint 合同，但不能把处理器塞进 Relay Runtime |
| 独立 GM 工具服务 | 生命周期可独立，后续便于集中管理多个测试会话 | 多一份构建与启动；需要正式的 Relay 状态查询连接，若由同一 Run 启动还要修改 artifact/topology 和进程数量合同 |

用户尚未在两者中选择。本轮不以“以后可配置”保留两套实现，也不暗中选定宿主。选择后只实现对应正式链路，并补齐精确的产品/配置 delta；工具协议选择也必须随之落定。

## 与现行规格的对比

- `deterministic-rollback-relay-product` 限制纯.NET依赖、启动职责及三个进程拓扑。两种宿主方案分别影响不同条款；宿主未定前，不伪造某份已定案的产品 delta，相关实现先停在该前置决策。
- `gameplay-network-model-boundary` 不允许通用 Host 解释模型消息。本轮 GM 查询不进入 SimulationSessionHost、角色 Program 或网络模型 Pass，保持该边界。
- `character-input-pipeline` 已规定 portable input 和请求历史归属。本 change 只新增控制台焦点约束，不清空 Program 请求、不改变输入历史。
- `btsmtl-runtime-diagnostics`、Foot storage/scoring 及已有 Editor 录制工作流不修改。原草案针对它们的增量已删除，不再要求这些 active change 先归档。

## 后续工作

玩法 GM、客户端状态查询、采样与诊断工具分别作为后续能力提出。它们可以注册到同一控制台命令入口，但不属于本 change 的实现或完成条件。当前 `Action playback command has no matching Select` 保持已知未修复，不由查询命令隐藏或修复。
