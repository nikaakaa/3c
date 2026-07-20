## MODIFIED Requirements

### Requirement: Macro 必须将业务意图展开为受限 Patch IR

系统 MUST提供 Agent Macro 层，将受限业务意图展开为 Patch IR。动作状态机 Macro MUST使用普通 StateMachineNode、inline StateMachineGraph、StateNode、Transition edge、ConditionRuleGraph、Action activation/lifecycle 和 Timeline TreeClip 表达动作类别、具体 leaf、时间资格与 replacement。Macro MUST NOT新增 Attack/Dodge 专用 opcode、直接修改 BTSMTL asset、硬编码 Corin 状态名、连段数量、特殊攻击路由、窗口名或 edge priority。保留的 Macro MUST要求调用方显式提供状态、窗口、ActionProfile、target 与 priority。

#### Scenario: 展开动作恢复取消

- **WHEN** Macro 接收 source state、target state、WindowType、request、target ActionProfile 与 priority
- **THEN** Macro MUST产出普通 Transition 与 `Request AND ActionWindowActive AND CanActivateAction` condition term 的 v9 Patch IR
- **AND** Macro MUST不根据 Attack、Dodge 或 Corin 名称猜默认窗口和 target

### Requirement: Patch IR 必须是确定性的 graph 编辑指令

系统 MUST定义 schema v9 Agent Patch IR 作为确定性的 Character authoring 编辑边界。Patch IR MUST使用 stable authoring id 或前序 operation output 引用定位编辑目标，并 MUST支持正式 graph/node/edge 操作、typed `action_window_active`/`action_can_activate` condition term、Blackboard declaration、Timeline TreeClip/write、状态 body typed Blackboard write、State/Transition 删除、Gameplay tag 与 ActionProfile tag/query mutation。资产引用 MUST作为实际消费该资产的 typed command 参数原子解析。Patch IR MUST不直接写 Unity YAML、GUID 映射集合、runtime 状态或旧配置路径，也 MUST不提供反射、字符串 handler 或 fallback operation。

普通 flow edge 的迁移 MUST通过 `delete_flow_edge` 按 stable graph/edge identity 删除，并通过 `link_flow` 重接；不得为了改连接而删除并重建仍然有效的节点。

`ensure_transition` 与 `ensure_condition_rule` MUST显式携带 stable edge identity，并 MUST只更新或创建该 identity 对应的具体 transition。同端点但业务条件或 priority 不同的边 MUST保持独立，Agent MUST NOT按 source/target 端点将它们合并。

已有 Blackboard declaration 跨 Graph owner 迁移时 MUST通过 `move_blackboard_declaration` 在同一事务内保留 stable declaration identity 并更新正式 metadata。该命令 MUST在 declaration 已位于 target owner 时幂等确认，MUST NOT要求两个会留下中间无效资产的独立 apply。

#### Scenario: 添加动作窗口条件

- **WHEN** v9 Patch 表达 `DodgeRequest AND RecoveryEarly AND CanActivate(DodgeProfile)`
- **THEN** lowerer MUST生成 immutable typed condition plan
- **AND** handler MUST通过正式 authoring API 创建两个通用 InfoNode 与 And 组合

#### Scenario: 合成 source OnExit lifecycle

- **WHEN** v9 Patch 通过 `ensure_action_exit_lifecycle` 提交 replacement 条件
- **THEN** `cancelConditionGroups` MUST按组内 AND、组间 OR 合成
- **AND** Patch MUST显式提供 Cancel、Interrupt、Abort 与 Complete reason
- **AND** handler MUST将 StateTransition replacement、自中止或低优先级中止、父级停止与自然退出分别映射为 Cancel、Interrupt、Abort 与 Complete
- **AND** 重复 apply MUST按 stable `Action Exit` selector identity 精确替换宏拥有的子树
- **AND** 首次迁移的旧 lifecycle 节点 MUST由显式删除 command 处理，宏 MUST不扫描或删除其它 lifecycle slot 的提交节点

#### Scenario: 迁移 local TreeClip declaration

- **WHEN** v9 Patch 确保 owner-local declaration、TreeClip range 和 write target
- **THEN** dry-run MUST解析相同 owner 与 identity
- **AND** apply MUST通过正式 Timeline/Blackboard authoring service 修改同一资产

#### Scenario: 删除旧 transition

- **WHEN** v9 Patch 通过 stable edge identity 删除旧 Cancel-key Transition
- **THEN** handler MUST删除该 edge 及 owner-local rule graph
- **AND** MUST不按 display name 猜测或保留 disabled 兼容 edge

#### Scenario: 删除 legacy state

- **WHEN** v9 Patch 通过 stable State identity 删除 legacy state
- **THEN** handler MUST删除该 State、关联 incoming/outgoing transition 与 inline body owner
- **AND** MUST不按 display name 猜测 State 或留下不可达 Timeline/ConditionRuleGraph

### Requirement: Agent Patch 编译必须维护 identity 生命周期

Agent Patch compiler MUST在更新现有元素时保持其 authoring identity，在创建新元素时生成新 identity，在复制元素时生成新 identity。系统 MUST只接受 schema v9，不得保留 v6/v7/v8 兼容解析或按 path、display name 猜测 identity。Typed command lowering MUST在 mutation 前验证 authoring identity 格式、operation id 唯一性、前序 operation reference 顺序、TreeClip/declaration owner 与 ActionProfile/tag catalog owner。

#### Scenario: 更新现有 Timeline Clip

- **WHEN** Patch 修改一个由 authoring identity 指定的 TreeClip range
- **THEN** compiler MUST修改该 Clip
- **AND** Clip、declaration、WindowId 与 Digest identity MUST保持

#### Scenario: 旧 schema 输入

- **WHEN** service 收到 schema v8 或更早 Patch
- **THEN** MUST在任何 mutation 前明确拒绝
- **AND** MUST不升级、翻译或进入兼容 reader

### Requirement: Agent Patch Compiler内部必须使用唯一类型化命令计划

系统 MUST将 schema v9 `AgentPatchOperation` 只作为 editor-only JSON 边界 DTO，并通过唯一 operation catalog 与 command lowerer 一次降低为 immutable typed command plan。Dry-run 与 apply MUST消费同一 typed command plan 和同一 handler catalog；后续 Planner、Handler、Condition builder、Timeline/Blackboard/Profile mutator MUST不再次按原始 `op` 字符串解释宽 DTO。Typed plan MAY保存 operation output 的 kind 与 owner scope symbol，但 MUST不复制 Graph、Node、Edge、Timeline、ActionProfile 或 Unity 序列化对象形成第二份 authoring 模型。

#### Scenario: 同一 Patch 执行 dry-run 和 apply

- **WHEN** Agent authoring service 收到合法 schema v9 Patch 并请求 apply
- **THEN** service MUST先 lower 一次 typed command plan 并完成无副作用 preflight
- **AND** apply MUST在资产级事务中消费相同 plan
- **AND** MUST不重新解析出另一组 operation 语义

#### Scenario: 未知 operation 进入 lowering

- **WHEN** Patch 包含 schema v9 catalog 未登记的 operation
- **THEN** lowerer MUST在任何 asset mutation 前返回结构化 unknown operation 错误
- **AND** MUST不选择 fallback handler、反射或动态字符串实现

### Requirement: 通用Agent Validator与业务样例覆盖必须分层

`AgentGraphValidator` MUST只检查对任意 Character Definition 成立的 Graph kind、Condition purity、Timeline ownership、serialized owner/path、TreeClip/declaration ownership、Action Context、WindowType projection phase、Input/ActionProfile/tag 引用、authoring identity、ownership 对称性和正式 Compiler 语义。它 MUST不读取 Definition 名称，不得硬编码 Corin、状态 display name、连招数量、cancel key、窗口帧数或具体 transition 集合。具体 Macro 或 Corin 迁移覆盖 MUST由显式 Synthesis/Migration evaluator 检查对应 typed command plan，MUST不进入普通 `validate` action。

#### Scenario: 验证非 Corin 角色

- **WHEN**作者验证一个使用不同 Action 状态名、窗口类型和连招层数的合法角色
- **THEN**通用 Validator MUST只按正式 authoring 语义判断
- **AND** MUST不要求 `None/Attack/Dodge`、`Attack1..5`、任何特殊攻击名或 Corin priority

#### Scenario: 评估 Corin migration patch

- **WHEN**迁移 evaluator 检查本 change 的显式 Corin v9 typed command plan
- **THEN** evaluator MAY检查预期状态、WindowType、profile 与 edge 集合
- **AND**普通 Graph validate MUST不执行这些业务规则

## REMOVED Requirements

### Requirement: Agent Snapshot schema v8 必须输出稳定 authoring identity

**Reason**: v8 无法表达 typed ActionWindow/ActionAdmission condition、local TreeClip/declaration mutation、Transition 删除与 ActionProfile tag/query，继续兼容会迫使迁移绕过正式 Agent authoring 链。

#### Scenario: 删除 v8 Snapshot 边界

- **WHEN** Agent exporter 或 patch service 初始化
- **THEN**系统 MUST不再输出或读取 schema v8
- **AND** MUST不提供 v8 镜像、升级器或双写 payload

## ADDED Requirements

### Requirement: Agent Snapshot schema v9 必须输出动作切换完整 authoring 身份

Agent Snapshot MUST使用 schema v9，并为 Graph、Node、Edge、condition term、Timeline、Track、TreeClip、Blackboard declaration、projection、ActionProfile、Gameplay tag 与 animation producer 输出正式稳定 authoring identity。Snapshot MUST输出 local declaration owner、WindowType、WindowId、Digest、TreeClip range/write target、状态 body Action activation 与 lifecycle transition 的 node identity、typed Blackboard write 的 node identity/value/lifecycle phase、ActionProfile granted/cancel/block query 和 edge priority。Snapshot path 和列表 index MAY作为可读定位信息，但 MUST不取代 identity。schema v9 Snapshot MUST成为生成 v9 Patch 的唯一上下文，不提供旧 schema 镜像输出。

正式 MCP `export_snapshot` MUST输出 Full Snapshot；Compact Snapshot MAY用于编辑器内轻量浏览，但 MUST NOT作为 Patch identity binding 或正式迁移输入。

#### Scenario: 导出 Full Snapshot

- **WHEN** Agent exporter 导出 Corin CharacterPipelineDefinition Full Snapshot
- **THEN**每个动作切换 condition MUST能追溯 request、WindowType、target ActionProfile、edge 与 owner identity
- **AND**每个 local Timeline window MUST能追溯 declaration、TreeClip、WindowId 与 Digest
- **AND**每个 full-body Action ownership write MUST能追溯 setter identity、Bool value 与 OnEnter/OnExit phase
- **AND**每个 Action activation 与 lifecycle transition MUST能追溯 state body 内的 stable node identity
- **AND** snapshot MUST标记 schema v9

#### Scenario: Timeline Track 重排后导出

- **WHEN**作者重排 Track 或 TreeClip 后重新导出 Snapshot
- **THEN**对应元素、declaration 与 producer identity MUST保持
- **AND** index/path MAY更新但 Patch 定位 MUST继续依赖 stable identity
