## MODIFIED Requirements

### Requirement: Agent Authoring Document必须是按需生成的持久化目录包

系统 MUST为已有合法 CharacterController 和其它正式登记的非游戏AI领域根提供唯一确定性的 Document v5 文档包；游戏 AIController 根不再受支持，插件行为不作为 BTSMTL Document 根。文档包 MUST位于Unity项目内、`Assets/`之外的`AgentAuthoring/Documents/<domain>/<root-key>.btsmtl/`，并只在显式checkout时从当前正式Unity authoring创建或刷新。文档包 MUST不成为BTSMTL正式真相、Unity资产、Player内容或runtime输入。

#### Scenario: AI首次编辑现有Character Controller

- **WHEN** Agent对已有合法Character root显式checkout
- **THEN** 系统 MUST从角色正式控制配置、技能、Timeline、Presentation与可达Clip Curve生成规范目录包
- **AND** response MUST返回唯一文档包绝对路径
- **AND** 系统 MUST不修改或保存Unity资产

#### Scenario: 普通人工编辑期间没有AI会话

- **WHEN** 作者修改Graph、Timeline或AnimationClip但没有显式checkout
- **THEN** 系统 MUST不创建或刷新文档包
- **AND** MUST不触发reconcile、compile、build或publish

#### Scenario: 请求旧AIController文档包

- **WHEN** 调用方指定已退出的 AIController domain 或旧 AI Definition
- **THEN** service MUST在读取或修改作者资产前明确返回不支持的领域
- **AND** MUST不创建包、不转换插件图、不转到 Character domain

### Requirement: apply成功后必须从最终Unity树反向发布整个文档包

Apply MUST在唯一资产事务内调用正式handler、Validator、dirty与Save。成功后系统 MUST从最终正式树重新导出完整规范文档包，将local identity替换为stable identity，更新sync基线并通过目录级staging原子发布。Document apply MUST不构建或发布 Character/技能/Timeline Program、Projection 或插件行为运行产物；精确 Build 继续使用各领域正式显式入口。任一 Mutation、Validator 或 package 发布失败 MUST不留下半成品或报告 Clean。

#### Scenario: Apply完整成功

- **WHEN** 同一document hash通过全部门禁与事务
- **THEN** 系统 MUST保存正式Unity资产
- **AND** MUST从最终Unity树反向发布完整文档包
- **AND** 文档包与Unity树 MUST回到Clean

#### Scenario: 最终package发布失败

- **WHEN** Unity Mutation成功但最终目录包无法原子切换
- **THEN** service MUST不报告完整成功或Clean
- **AND** MUST在正式可回滚边界内恢复上一份Unity资产与文档包

## ADDED Requirements

### Requirement: 游戏AI作者领域必须完整退役且不影响其它Document领域

唯一 Document schema、根解析、editable/context 文件族、导出、对账、Mutation、Validator、MCP 描述和技能说明 MUST删除游戏 AIController 领域及其 AI Program 发布入口。Character 的控制配置、技能、Presentation、允许的 Clip 曲线，以及独立 Timeline 变更正式登记的 domain MUST继续由各自唯一合同拥有；MUST不因删除 AI 撤销这些能力。旧 AI package MUST不再进入正式 checkout/apply，也不自动转换为插件内容。

#### Scenario: 删除AI领域后编辑技能

- **WHEN** Agent 使用合法 Character 文档包修改技能或允许的表现内容
- **THEN** 原有整包 hash、唯一 Mutation、资产事务与反向导出 MUST继续生效
- **AND** MUST不依赖旧 AI DTO 或 Compiler

#### Scenario: 独立Timeline领域已正式登记

- **WHEN** 唯一 schema 已包含独立 Timeline domain 且调用方提交该领域的合法根
- **THEN** 删除 AI 领域 MUST保留 Timeline 的正式根和内容闭包
- **AND** MUST不把领域集合重新硬编码为两个旧 Controller 类型
