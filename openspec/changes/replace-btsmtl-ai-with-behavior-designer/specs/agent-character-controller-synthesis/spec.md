## MODIFIED Requirements

### Requirement: Agent Authoring Document必须是声明式控制器结构

系统 MUST使用唯一 Document v5 目录包表达已登记的角色作者合同，并通过显式非游戏AI domain 区分根。Character editable 分片 MUST按控制 binding/参数、技能定义、技能 Graph/局部状态、ActionProfile、Timeline、局部变量、Presentation 与允许的 AnimationClip 曲线描述目标结构；MUST删除 AIController 正文与其 Perception/Intent 分片。Graph MUST使用稳定kind、typed properties、逻辑port、系统anchor、正式owner和Flow/Property Edge完整目标集合，MUST不暴露C# type name、重复port metadata、`operations[]`、内部handler、创建顺序、前序operation output、Unity YAML或任意SerializedProperty写入。Document Reconciler MUST只把正式支持的整包变化降低为内部typed Mutation。

#### Scenario: 添加状态和Transition

- **WHEN** package增加技能内部带local identity的Attack状态、owner body Graph及其到现有局部状态的Transition
- **THEN** Reconciler MUST生成有序State、Transition和Condition typed Mutation
- **AND** AI MUST不填写`ensure_state`、`ensure_transition`、`link_flow`或调用节点级工具

#### Scenario: 请求未知结构字段

- **WHEN** Document包含schema未登记字段或节点能力
- **THEN** strict parser或Reconciler MUST在mutation前拒绝
- **AND** MUST不创建placeholder或动态反射操作

### Requirement: Agent Document必须降低为唯一类型化Mutation计划

系统 MUST让strict multi-file parser与`AgentDocumentReconciler`从整个package一次生成immutable typed`AgentMutationPlan`。所有正式登记的非游戏AI领域 MUST复用同一 planning symbol、preflight、资产事务和 handler catalog 基础；domain handler 只消费正式 authoring API，MUST不保留游戏 AIController handler。Dry-run与apply MUST基于同一整包document hash生成等价plan，后续handler不得读取原始JSON discriminator或建立AI专用compiler和第二事务。

#### Scenario: 同一Document执行dry-run和apply

- **WHEN** 合法Document先dry-run再以同一document hash申请apply
- **THEN** 两次lowering MUST产生相同plan hash和planned diff
- **AND** apply MUST在资产事务中消费等价typed plan
