## MODIFIED Requirements

### Requirement: Agent Authoring Document必须是按需生成的持久化目录包

系统 MUST为每个已有合法`CharacterPipelineDefinition`或`AIControllerDefinition`提供唯一确定性`btsmtl-agent-authoring-document.v7`文档包。文档包 MUST位于Unity项目内、`Assets/`之外的`AgentAuthoring/Documents/<domain>/<root-key>.btsmtl/`，并只在显式checkout时从当前正式Unity authoring创建或刷新。文档包 MUST不成为BTSMTL正式真相、Unity资产、Player内容或runtime输入。

#### Scenario: AI首次编辑现有Character Controller

- **WHEN** Agent对已有合法Character root显式checkout
- **THEN** 系统 MUST从当前正式Graph、StateMachine、Timeline、Presentation与可达Clip Curve生成规范目录包
- **AND** response MUST返回唯一文档包绝对路径
- **AND** 系统 MUST不修改或保存Unity资产

#### Scenario: 普通人工编辑期间没有AI会话

- **WHEN** 作者修改Graph、Timeline或AnimationClip但没有显式checkout
- **THEN** 系统 MUST不创建或刷新文档包
- **AND** MUST不触发reconcile、compile、build或publish


#### Scenario: 旧版本文档包请求写入

- **WHEN** Agent使用v6或更早的Document包请求dry-run或apply
- **THEN** 系统 MUST拒绝该包并要求从精确根重新checkout v7，不自动转换或兼容读取

## ADDED Requirements

### Requirement: 文档必须直接表达正式图和Macro闭包

Document MUST通过稳定业务kind、typed字段、逻辑端口和显式owner表达技能及原生参数化子图接口与调用。导出和对账 MUST直接访问正式作者图，MUST不输出第三方C#类型、私有序列化字段、执行委托或观测状态。私有子图、共享引用、接口变化与根资产 MUST进入同一整包hash、Mutation、保存和反向导出事务。

#### Scenario: 修改共享子图接口
- **WHEN** Agent修改可写共享子图接口及其调用连接
- **THEN** dry-run MUST对完整声明闭包计算修改和引用合法性，apply MUST只采用相同document hash
- **AND** 任一owner保存或反向导出失败 MUST完整回滚，不留下只更新接口的调用点

非技能分片 MUST保持原有业务语义及正式作者模型；协议版本升级不得触发其他领域资产迁移。

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

