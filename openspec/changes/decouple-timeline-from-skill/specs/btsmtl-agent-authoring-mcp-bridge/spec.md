## MODIFIED Requirements

### Requirement: Bridge 必须复用正式 Agent compiler 与 BTSMTL authoring API

MCP bridge MUST复用唯一 v5 package exporter、Reconciler、Mutation、domain Validator 和报告。CharacterController、Timeline 及其他仍有效 domain 的修改 MUST经同一 Application Service 和所属 typed handler，使用各自正式作者 API。Timeline 增量不得重新登记已退役的游戏 AI domain 或建立插件 AI Document。Bridge MUST不直接写 Unity YAML、序列化曲线、节点/边集合或另建数据/执行链；新增 domain 不得增加节点/片段级 MCP 工具。

#### Scenario: Bridge应用Clip Curve变化

- **WHEN** v5 Character 包包含合法原生 Clip 注册曲线变化
- **THEN** bridge MUST将整包交给唯一服务及 Clip Curve handler
- **AND** MCP handler MUST不直接编辑动画数据

#### Scenario: Bridge应用Graph变化

- **WHEN** v5 包包含合法 Graph 目标变化
- **THEN** handler MUST使用正式 Graph Mutation，不能创建节点级工具

#### Scenario: Bridge应用独立 Timeline

- **WHEN** Timeline domain 包含合法片段与子树修改
- **THEN** bridge MUST透传同一整包服务，使用共享 Timeline/Tree handler

### Requirement: Definition 目标必须由调用上下文显式提供

MCP 与 Window 生命周期请求 MUST通过 domain 和 root_asset_path 精确选择已有合法根：CharacterController 对应 Character Definition，Timeline 对应 shared TimelineAsset；其他 domain 只按各自仍有效的正式合同分派。AIController 被其正式变更退役后，旧 AI Definition 路由 MUST拒绝，不能因本次新增 Timeline 而恢复。路径必须位于 Assets 下且类型匹配。包路径由 service 按 domain/root 唯一决定，MUST不通过 selection、目录扫描、同名匹配、场景、剪贴板或旧配置查找根。

#### Scenario: Definition路径合法

- **WHEN** 请求给出 domain 匹配的精确根路径
- **THEN** checkout、reconcile、validate MUST只作用于该根及其正式引用闭包

#### Scenario: Definition不存在

- **WHEN** 根缺失或类型不匹配
- **THEN** bridge MUST在 checkout 前返回错误，MUST不创建临时 root

#### Scenario: Timeline 路径合法

- **WHEN** 请求选择 Timeline domain 和合法 shared Timeline 根
- **THEN** 五个生命周期 MUST使用同一 Timeline 包，MUST不要求 Character Definition

## ADDED Requirements

### Requirement: Timeline 内容构建必须与 Document apply 分离

独立 Timeline 必须提供按精确资产、Numeric Target 和输出位置执行的正式 Build 命令，并由工具与人工入口调用同一构建服务。Timeline Document 的五个生命周期 MUST只处理作者包，apply 成功后产物仍按真实依赖显示是否过期，不自动构建。Build MUST返回实际产物身份、依赖、Target、hash 与诊断，不得扫描或猜测目标。

#### Scenario: Apply 后明确构建 Timeline

- **WHEN** Timeline apply 成功且调用方随后提交精确 Build 请求
- **THEN** Build MUST通过共享编译与发布基础生成独立产物
- **AND** apply 本身 MUST不产生或发布 Program
