# deterministic-rollback-relay-product Specification

## Purpose

定义DeterministicRollback纯.NET Dedicated Relay Server、portable runtime manifest、Network Test Product闭包与三进程启动边界。

## Requirements

### Requirement: Rollback Dedicated Relay Server必须是纯.NET网络产品

系统 MUST提供受版本控制的`ThirdPerson.DeterministicRollback.Server` .NET 8 executable。该产品 MUST只引用portable Core、Fixed identity、DeterministicRollback protocol与Endpoint/Relay runtime source set，MUST不引用UnityEngine、Unity程序集、Fantasy、ServerAuthoritative、DotRecast、Animancer或Editor程序集。Server MUST不加载Unity Scene、Asset、Character Program或Collision World内容，也 MUST不被实现或命名为Listen Host、Canonical Host或Gameplay Authority Host。

#### Scenario: 构建Dedicated Relay Server Project

- **WHEN** 普通dotnet build编译DeterministicRollback Server产品
- **THEN** MUST在不安装Unity Editor runtime的情况下完成编译
- **AND** 产物依赖闭包 MUST不包含Unity或Fantasy程序集

#### Scenario: Relay Server启动

- **WHEN** Server读取合法portable runtime manifest并监听endpoint
- **THEN** MUST只创建handshake、roster、input fanout、canonical/confirmation与snapshot routing runtime
- **AND** MUST不创建SimulationSession、Program、KCC或Presentation

### Requirement: Relay Server Runtime Manifest必须完整锁定会话身份

Build adapter MUST生成portable `DeterministicRollbackServerManifest`，至少记录SchemaVersion、BuildId、ProductId、SessionId、listen endpoint、expected client/actor roster、Model/Protocol identity、TickRate、SemanticHash、Fixed ProgramHash、LayoutHash、CollisionWorldHash、Kcc identity/capabilities、confirmation policy、capacity和snapshot source policy。Server MUST在监听前完整校验manifest，MUST不从Unity asset、环境目录、文件存在性或默认值补齐缺失事实。

#### Scenario: Manifest缺少ProgramHash

- **WHEN** Server runtime manifest缺少或包含无效Fixed ProgramHash
- **THEN** Server MUST以明确退出码拒绝启动
- **AND** MUST不等待Client连接后再猜测身份

#### Scenario: Client Handshake与Manifest不一致

- **WHEN** Client提交的ProtocolVersion、roster或deterministic identity与manifest不一致
- **THEN** Server MUST拒绝锁定roster
- **AND** SimulationTick MUST不开始

### Requirement: Rollback Network Test Product必须包含精确Server Closure

Rollback build adapter MUST通过公共model-neutral runtime artifact合同发布Server executable、依赖和runtime manifest，并将其exact closure与hash写入schema v2 Network Test Product manifest。ProductRoot MUST同时包含`Player`与`Server`目录。公共Build workflow MUST只验证adapter声明的identity、entrypoint、closure和hash，MUST不引用Rollback concrete type或按目录名猜测产品。

#### Scenario: 构建Rollback Product

- **WHEN** 作者执行Deterministic Rollback Build
- **THEN** MUST原子发布一个Player artifact与一个Dedicated Relay Server artifact
- **AND** schema v2 product manifest MUST精确绑定两者BuildId和hash

#### Scenario: Server文件在Build后变化

- **WHEN** Run前Server executable、依赖或runtime manifest的hash与product manifest不一致
- **THEN** Run MUST拒绝启动
- **AND** MUST不重新publish或复制文件修复产物

### Requirement: Rollback Run必须只启动一个Dedicated Relay Server与两个Unity Client

Rollback Run MUST先校验既有schema v2 product manifest，再启动Server executable并等待endpoint ready，随后启动带显式peer profile的Client A与Client B Player。Run MUST不启动第三个Unity Player，不接受`--deterministic-rollback-role=host`，不加载Canonical Host Scene，也不在运行阶段编译、publish或生成配置。Server退出 MUST结束当前Demo session，MUST不切换为Client-host、Local或ServerAuthoritative。

#### Scenario: 启动完整DS Demo

- **WHEN** product manifest、Server和Player closure全部有效
- **THEN** Run MUST启动三个进程，其中只有Client A与Client B是Unity Player
- **AND** Server MUST使用独立日志文件和RunId

#### Scenario: Server在运行中退出

- **WHEN** Dedicated Relay Server进程异常结束
- **THEN** 两个Client MUST结束当前Rollback Session并报告relay server unavailable
- **AND** MUST不由任一Client接管Server职责
