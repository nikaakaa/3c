## ADDED Requirements

### Requirement: Agent Authoring Document必须是按需生成的持久化工作副本

系统 MUST为每个已有合法`CharacterPipelineDefinition`或`AIControllerDefinition`提供唯一确定性Agent Authoring Document路径。Document MUST位于Unity项目内、`Assets/`之外的正式Agent工作目录，并且只在显式`checkout_document`时从当前正式树创建或刷新。Document MUST不成为BTSMTL authoring真相、Unity资产、Player内容或runtime输入。

#### Scenario: AI首次编辑现有Character Controller

- **WHEN** Agent对一个已有合法Character root显式调用`checkout_document`
- **THEN** 系统 MUST从当前正式Graph、StateMachine、Timeline与可写依赖生成规范JSON
- **AND** response MUST返回唯一Document绝对路径
- **AND** 系统 MUST不修改或保存任何Unity资产

#### Scenario: 普通人工编辑期间没有AI会话

- **WHEN** 作者在Graph Editor或Timeline Editor修改正式树但没有调用checkout
- **THEN** 系统 MUST不创建或刷新Document
- **AND** MUST不触发任何Agent reconcile、build或publish

### Requirement: Agent Authoring Document必须分离同步头、可编辑正文和只读上下文

Document MUST只接受`btsmtl-agent-authoring-document.v1`，并 MUST包含显式domain、root identity、service-owned sync header、AI可编辑authoring正文与只读context。已有可写entity MUST使用stable authoring identity，新entity MUST使用本Document内唯一local identity。Presentation、Body Motion、Foot Analysis generated data、runtime state与generated product MUST只进入只读context或完全省略，不得进入可写正文。

#### Scenario: AI读取Character Document

- **WHEN** checkout导出Character Controller
- **THEN** editable MUST表达Agent正式可写的Graph、StateMachine、Condition、Timeline与Blackboard结构
- **AND** context MUST只读表达Input、Action、Presentation、Body Motion与generated product身份
- **AND** Document MUST不暴露Unity YAML、managed-reference布局或私有SerializedProperty path

#### Scenario: AI尝试修改只读上下文

- **WHEN** Document中的Presentation、Body Motion或generated product字段与checkout基线不同
- **THEN** strict parser或Reconciler MUST返回`readonly_context_modified`
- **AND** MUST不把该变化降低为mutation

### Requirement: Document codec必须严格解析并规范化JSON

系统 MUST使用唯一strict parser和canonical writer处理Document。Parser MUST拒绝未知字段、重复属性、非法discriminator、缺失必需字段、非有限数值和对service-owned同步字段的修改。Writer MUST使用UTF-8无BOM、稳定字段顺序、稳定entity顺序与明确数值格式。Content hash MUST基于规范语义内容，不得因缩进、换行或输入属性顺序变化。

#### Scenario: AI只格式化Document

- **WHEN** AI只改变缩进、换行或JSON属性输入顺序
- **THEN** canonical content hash MUST保持不变
- **AND** 同步状态 MUST不因此变为DocumentDirty

#### Scenario: Document包含未知字段

- **WHEN** AI在Timeline entity中加入schema未声明字段
- **THEN** parser MUST在reconcile前拒绝Document
- **AND** MUST不忽略未知字段或按默认值继续apply

### Requirement: 同步状态必须由live revision和canonical hash推导

系统 MUST通过当前live authoring source revision与Document的`baseSourceRevision`比较树是否变化，并通过当前正文canonical hash与`baseContentHash`比较Document是否变化。系统 MUST只产生`Clean`、`TreeDirty`、`DocumentDirty`和`Conflict`四种状态，MUST不保存可由AI编辑的dirty布尔值。

#### Scenario: 只有作者修改树

- **WHEN** 当前live source revision变化且Document正文hash仍等于基线
- **THEN** 状态 MUST为`TreeDirty`
- **AND** 下一次显式checkout MAY从当前树刷新Document

#### Scenario: 只有AI修改Document

- **WHEN** live source revision未变化且Document正文hash变化
- **THEN** 状态 MUST为`DocumentDirty`
- **AND** Unity树与generated product MUST保持不变

#### Scenario: 双方同时修改

- **WHEN** live source revision与Document正文hash都偏离基线
- **THEN** 状态 MUST为`Conflict`
- **AND** dry-run与apply MUST拒绝继续

### Requirement: live authoring source revision不得依赖generated product

Character与AI domain MUST从当前Definition、可达Graph、StateMachine、Condition、Timeline和各domain正式可写依赖计算live authoring source revision。Character Program、Presentation Projection与AIIntentProgram的revision和stale状态 MUST只作为只读诊断，MUST不充当TreeDirty基线。Revision计算 MUST只读，MUST不调用build、publish或SaveAssets。

#### Scenario: 作者改树但没有编译Program

- **WHEN** 作者修改一个Transition后没有执行Character Program Build
- **THEN** live authoring source revision MUST立即在下一次显式状态查询时变化
- **AND** Document MUST显示TreeDirty或Conflict
- **AND** 系统 MUST不为了计算revision而编译Program

### Requirement: checkout必须保护未应用的AI修改

`checkout_document` MUST根据当前同步状态决定是否写Document。无Document、Clean或TreeDirty且Document未改时，checkout MUST从当前树写出规范Document；DocumentDirty时 MUST保留当前AI正文并返回现有路径；Conflict时 MUST拒绝覆盖任何一边并返回机器可读冲突。

#### Scenario: AI编辑中再次checkout

- **WHEN** Document处于DocumentDirty且树未变化
- **THEN** checkout MUST不覆盖AI正文
- **AND** response MUST返回同一路径与DocumentDirty状态

#### Scenario: checkout遇到冲突

- **WHEN** Document和树都已变化
- **THEN** checkout MUST返回Conflict及当前revision诊断
- **AND** MUST不自动重导出覆盖Document

### Requirement: Document必须确定性降低为内部Mutation Plan

系统 MUST使用唯一Document Reconciler比较当前规范Snapshot与Document目标正文，并生成immutable typed `AgentMutationPlan`。Reconciler MUST自行决定创建、更新、连接、删除、引用绑定和owner处理顺序；AI MUST不提交operation数组、前序operation output或内部handler名称。Reconciler MUST保持现有stable identity，为新local identity建立planning symbol，并拒绝unsupported或只读entity变化。

#### Scenario: AI在Document中增加Attack状态

- **WHEN** AI只在StateMachine正文中增加一个带local identity的Attack状态及其行为结构
- **THEN** Reconciler MUST生成创建State、行为节点、引用和连接所需的有序typed mutation
- **AND** AI MUST不填写`ensure_state`、`link_flow`或operation output引用

#### Scenario: AI从完整目标集合删除状态

- **WHEN** 一个已有可写State stable identity从Document目标集合中移除
- **THEN** Reconciler MUST计划删除State及正式受影响关系
- **AND** MUST不删除Document未管理的read-only或unsupported实体

### Requirement: dry-run必须锁定Document语义输入

`dry_run_document` MUST重新读取Document、计算live revision、推导同步状态、严格解析、reconcile并执行无副作用preflight。成功response MUST包含canonical`documentHash`、plan hash、planned diff、metrics与Document entity路径诊断。TreeDirty或Conflict MUST在任何mutation前失败。Dry-run MUST不dirty、save、build或publish。

#### Scenario: AI完成一轮Document编辑

- **WHEN** Document处于DocumentDirty且root、revision、context和正文均合法
- **THEN** dry-run MUST返回对应document hash和planned diff
- **AND** Unity资产、Program和Projection MUST保持不变

### Requirement: apply必须消费同一Document并在成功后反向规范化

`apply_document` MUST要求dry-run返回的`expectedDocumentHash`，重新确认当前Document semantic hash、live source revision、root identity和同步状态未变化，再建立等价immutable Mutation Plan并进入唯一资产事务。Apply MUST调用正式handler、Validator、dirty、Save与显式generated product发布；任一失败 MUST完整回滚。成功后系统 MUST从最终正式树重新导出规范Document，写回真实stable identity与新基线，并把状态恢复为Clean。

#### Scenario: dry-run后Document又被修改

- **WHEN** apply收到的expected hash与当前Document semantic hash不同
- **THEN** apply MUST在mutation前返回`document_hash_changed`
- **AND** MUST不修改Unity资产

#### Scenario: Apply完整成功

- **WHEN** 同一Document通过hash、revision、preflight、mutation、Validator与generated product发布
- **THEN** 系统 MUST原子保存正式资产
- **AND** MUST从最终树反向写回规范Document
- **AND** Document与树 MUST回到Clean

#### Scenario: 反向写回Document失败

- **WHEN** 正式树mutation成功但最终Document无法原子替换
- **THEN** service MUST不报告完整成功或Clean
- **AND** MUST在正式事务可回滚边界内避免留下假同步状态

### Requirement: Conflict必须通过显式rebase处理

系统 MUST在Conflict时拒绝apply，并提供显式`rebase_document`。Rebase MUST以当前树作为新基线，保留AI目标正文，不修改Unity资产、不build、不自动merge。Rebase完成后Document MUST重新成为DocumentDirty，并要求新的dry-run。

#### Scenario: AI显式接受当前人工树为新基线

- **WHEN** AI已把需要保留的人工变化整理进Document并调用rebase
- **THEN** service MUST更新base source revision和当前树canonical content hash
- **AND** MUST保留AI目标正文
- **AND** 后续apply MUST要求重新dry-run

### Requirement: 文件与编辑器事件不得自动触发重操作

Graph编辑、Timeline编辑、Inspector修改、JSON保存、selection变化、窗口focus、AssetDatabase refresh和domain reload MUST不自动执行checkout、reconcile、dry-run、apply、Program build、Projection build或AI Program build。只有显式`apply_document` MAY在成功事务内发布generated product。

#### Scenario: AI保存Document文件

- **WHEN** AI把修改后的JSON写入磁盘
- **THEN** 系统 MUST只在下一次显式查询时推导DocumentDirty
- **AND** MUST不自动唤醒Unity编译或修改树

#### Scenario: 用户在Project窗口选中Definition

- **WHEN** selection切换到Character或AI Definition
- **THEN** Window MAY显示上下文
- **AND** MUST不自动checkout、validate或build

