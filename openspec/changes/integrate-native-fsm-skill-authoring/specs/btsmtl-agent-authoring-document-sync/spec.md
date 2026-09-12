## RENAMED Requirements

- FROM: `### Requirement: Document v7必须原子替代v6`
- TO: `### Requirement: Document v8必须原子替代v7`

## MODIFIED Requirements

### Requirement: Document v8必须原子替代v7

系统 MUST只接受`btsmtl-agent-authoring-document.v8`，删除v7及更早版本的正式reader、writer、manifest识别和兼容入口。五个生命周期工具及唯一资产事务 MUST保持不变；旧包 MUST显式重新checkout，不得静默覆盖未提交Document差异。非Skill分片 MUST保留各自业务形状，整包版本切换不构成其它领域的重新实现。

#### Scenario: 读取v3文档包

- **WHEN** service发现schema为`btsmtl-agent-authoring-document.v3`
- **THEN** dry-run与apply MUST拒绝该文档且不修改资产
- **AND** 调用方 MUST显式重新checkout

#### Scenario: 旧v7包仍有未提交修改

- **WHEN** v7包处于DocumentDirty或Conflict并请求进入新协议
- **THEN** 系统 MUST先保留并报告差异，拒绝直接apply旧包或覆盖该目标
- **AND** 差异裁决后 MUST经显式checkout与同一v8事务处理，不使用兼容reader

### Requirement: Agent Authoring Document必须是按需生成的持久化目录包

系统 MUST为每个已有合法`CharacterPipelineDefinition`提供唯一确定性`btsmtl-agent-authoring-document.v8`文档包。Behavior Designer AI不以BTSMTL Document为根。文档包 MUST位于Unity项目内、`Assets/`之外的`AgentAuthoring/Documents/CharacterController/<root-key>.btsmtl/`，并只在显式checkout时从当前正式Unity authoring创建或刷新。文档包 MUST不成为BTSMTL正式真相、Unity资产、Player内容或runtime输入。

#### Scenario: AI首次编辑现有Character Controller

- **WHEN** Agent对已有合法Character root显式checkout
- **THEN** 系统 MUST从当前正式Skill Graph、Macro、Skill Timeline、Presentation与可达Clip Curve生成规范目录包
- **AND** response MUST返回唯一文档包绝对路径
- **AND** 系统 MUST不修改或保存Unity资产

#### Scenario: Agent首次编辑现有Character Controller

- **WHEN** Agent对已有合法Character root显式checkout
- **THEN** 系统 MUST从当前正式Skill FlowGraph、原生FSM、Macro、Timeline、Presentation与可达Clip Curve生成v8规范目录包
- **AND** response MUST返回唯一文档包绝对路径
- **AND** 系统 MUST不修改或保存Unity资产

#### Scenario: 普通人工编辑期间没有AI会话

- **WHEN** 作者修改Graph、Timeline或AnimationClip但没有显式checkout
- **THEN** 系统 MUST不创建或刷新文档包
- **AND** MUST不触发reconcile、compile、build或publish


#### Scenario: 旧版本文档包请求写入

- **WHEN** Agent使用v7或更早的Document包请求dry-run或apply
- **THEN** 系统 MUST拒绝该包并要求从精确根重新checkout v8，不自动转换或兼容读取

## ADDED Requirements

### Requirement: 文档必须直接表达正式图和Macro闭包

Document MUST通过稳定业务kind、typed字段、逻辑端口和显式owner表达技能及原生参数化子图接口与调用。导出和对账 MUST直接访问正式作者图，MUST不输出第三方C#类型、私有序列化字段、执行委托或观测状态。私有子图、共享引用、接口变化与根资产 MUST进入同一整包hash、Mutation、保存和反向导出事务。

原生FSM MUST使用同一Skill图文件闭包和正式能力catalog。StateBody MUST由State节点拥有；私有条件图 MUST使用`kind=edge`、`graphId`、`edgeId`、`referenceKey=condition`归属转移；FSM整体生命周期执行体 MUST由唯一系统钩子节点拥有。转移 MUST保存稳定identity、逻辑端点、condition、priority、abortPolicy和同来源唯一order，不保存State steps；非转移边不得携带转移参数。Exporter、Reconciler、Mutation、Validator和编译 MUST消费同一定义，不新增FSM专用包、第二事务或JSON直编入口。

#### Scenario: 只由FSM连接拥有的条件图

- **WHEN** Agent修改一条原生FSM转移的私有ConditionRule
- **THEN** 该条件图 MUST进入完整导出、typed Mutation、owner事务、复制/删除和反向导出闭包
- **AND** 错误edge owner、重复order或旧state steps MUST被明确拒绝，不从旧图补读

#### Scenario: FSM迁移中反向导出失败

- **WHEN** 原生FSM和部分StateBody已创建但后续保存或反向导出失败
- **THEN** 同一事务 MUST恢复原有资产、引用和正式package，并回收仅本次创建的对象
- **AND** MUST不保留半迁移状态机，不发布Program或第二份可编辑图

#### Scenario: 修改共享子图接口
- **WHEN** Agent修改可写共享子图接口及其调用连接
- **THEN** dry-run MUST对完整声明闭包计算修改和引用合法性，apply MUST只采用相同document hash
- **AND** 任一owner保存或反向导出失败 MUST完整回滚，不留下只更新接口的调用点

非技能分片 MUST保持原有业务语义及正式作者模型；协议版本升级不得触发其他领域资产迁移。CharacterController Document MUST不生成或读取`editable/blackboard.json`、`editable/graphs/**`和`editable/timelines/**`；这些内容只能通过Skill Flow闭包表达。

### Requirement: Document技能根必须遵守独立资产事务

Document MUST把技能RootAsset解析或创建为独立FlowGraph主资产，Definition只持有根引用。根拥有的私有Graph、Macro与Timeline MUST保存于同一根文件，共享Macro拥有的私有内容 MUST保存于该共享Macro文件。预检 MUST核对声明ownership、实际资产类型及调用方文件归属，MUST拒绝Definition子资产形式的技能根。

新增技能根的目标路径 MUST在apply写入前确定，并进入同一Document事务的创建计划；已存在的资产或meta MUST导致冲突，不得覆盖或自动改用另一条路径。apply后任一保存或反向导出失败，事务 MUST恢复已有owner及引用，并清理本次新建的技能根文件及其私有内容，MUST不删除事务开始前已经存在的资产。

#### Scenario: 新技能根目标路径被占用
- **WHEN** dry-run解析出的新技能根目标已有资产或meta
- **THEN** 预检 MUST报告路径冲突并拒绝该计划
- **AND** MUST不覆盖已有文件或将根改存为Definition子资产

#### Scenario: 创建技能根后反向导出失败
- **WHEN** apply已经创建技能根及部分私有内容，但后续保存或反向导出失败
- **THEN** 同一事务 MUST恢复Definition及已有owner的修改，移除本次新建根文件及其私有内容
- **AND** 事务开始前的共享资产 MUST保留，结果 MUST不得报告applied、saved或Clean成功

### Requirement: Document必须保持Skill变量provider和声明合同

Document MUST表达Skill变量所属provider、owner identity、稳定声明ID、类型、scope、lifetime、读写权限及输入/事实绑定。Character State、Ability Attribute、GameplayTag、Input/TargetData和Skill Local Blackboard MUST保持不同的业务语义；Document MUST不把外部provider复制为Skill局部变量。

#### Scenario: 导出Skill变量引用
- **WHEN** 正式Skill Graph引用Character State或Ability provider变量
- **THEN** Document MUST保存显式owner和声明身份
- **AND** 反向导出 MUST保持引用而不是创建第二份变量值

### Requirement: Document不得成为网络运行状态包

Document MUST不保存PredictionKey、ActionInstance当前值、Rollback Snapshot、State Hash、Session transport状态或最终Animation Pose。Document MAY保存编译来源、provider schema和稳定业务引用；网络运行状态 MUST由正式Simulation Session管理。

#### Scenario: Checkout运行过的Skill
- **WHEN** Agent对已有Network Session执行checkout
- **THEN** 文档 MUST只导出作者源和只读编译上下文
- **AND** MUST不把运行时状态写入editable内容或反向导出目标

### Requirement: Validate可以显式核对Session Composition兼容性

`btsmtl.validate` MAY 接收一个精确的 `composition_asset_path`。当提供该路径时，Validator MUST 直接调用现有 `SimulationSessionCompositionCompatibility.Evaluate`，核对 ProgramRuntime、ExecutionBackend、Pipeline、SessionSource、WorldSolver、Required Pass、Source Port、ExecutionSupport 与确定性合同，并把结果加入同一只读诊断报告。该参数 MUST 不扫描目录、不修改资产、不执行Build或Play，也 MUST 不增加第二个Composition Mutation或MCP入口。

#### Scenario: Agent校验指定Local Composition

- **WHEN** Agent在Character `btsmtl.validate`中提供一个精确的 `SimulationSessionCompositionDefinition` 资产路径
- **THEN** Validator MUST 返回该Composition的正式兼容结果、Pipeline plan identity与Source/网络身份
- **AND** MUST不从Selection、第一个资产或目录扫描推断Composition
