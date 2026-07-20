## ADDED Requirements

### Requirement: Agent Patch Compiler内部必须使用唯一类型化命令计划

系统 MUST将schema v8 `AgentPatchOperation`只作为editor-only JSON边界DTO，并通过唯一operation catalog与`AgentPatchCommandLowerer`一次降低为immutable typed command plan。Dry-run与apply MUST消费同一typed command plan和同一handler catalog；后续Planner、Handler与Condition builder MUST不再次按原始`op`字符串解释宽DTO。Typed plan MAY保存operation output的kind与owner scope symbol，但 MUST不复制Graph、Node、Edge、Timeline或Unity序列化对象形成第二份authoring模型。

#### Scenario: 同一Patch执行dry-run和apply

- **WHEN** `AgentPatchAuthoringService`收到合法schema v8 Patch并请求apply
- **THEN** service MUST先lower一次typed command plan并完成无副作用preflight
- **AND** apply MUST在资产级事务中消费相同plan
- **AND** MUST不重新解析出另一组operation语义

#### Scenario: 后序operation引用前序输出

- **WHEN** 后序typed command通过operation id引用前序command计划创建的State、Node或Edge
- **THEN** dry-run MUST通过窄planning symbol验证输出kind与owner scope
- **AND** apply MUST把前序实际创建对象注册到同一operation id
- **AND** 系统 MUST不创建虚拟Graph clone来解析该引用

#### Scenario: 未知operation进入lowering

- **WHEN** Patch包含schema v8 catalog未登记的operation
- **THEN** lowerer MUST在任何Graph mutation前返回结构化unknown operation错误
- **AND** MUST不选择fallback handler或动态反射实现

### Requirement: Agent Compiler模块必须按authoring职责聚合

`AgentPatchCompiler` MUST保持唯一Compiler facade，但单次Definition、Snapshot、Resolver、Graph Index、operation symbol、diff与touched owner MUST由每次调用独占的compile session拥有。StateMachine、StateBehavior、Node/Asset、GraphLink与ConditionRule MUST由按共享authoring不变量聚合的handler处理。Compiler MUST不拥有Undo、dirty、rollback或SaveAssets；这些资产事务职责 MUST继续只属于`AgentPatchAuthoringService`。

#### Scenario: 连续编译两个Definition

- **WHEN** 同一Compiler实例连续dry-run两个不同`CharacterPipelineDefinition`
- **THEN** 第二次调用 MUST创建新的compile session
- **AND** MUST不读取第一次调用的Resolver、Index、operation output或touched owner

#### Scenario: Apply修改多个inline与shared owner

- **WHEN** typed command plan修改多个可达Graph serialized owner
- **THEN** compile session MUST报告实际touched owner
- **AND** application service MUST在唯一Undo事务内统一dirty、验证和保存
- **AND** handler MUST不直接调用`AssetDatabase.SaveAssets`

### Requirement: 通用Agent Validator与业务样例覆盖必须分层

`AgentGraphValidator` MUST只检查对任意Character Definition成立的Graph kind、Condition纯度、Timeline ownership、serialized owner/path、TreeClip ownership、Action Context、Input/ActionProfile引用、authoring identity和正式Compiler语义。它 MUST不读取Definition名称，不得硬编码Corin、状态display name、连招数量、cancel key或具体transition集合。具体Macro的业务覆盖 MUST由Synthesis/Macro coverage evaluator在对应样例范围内检查typed command plan，MUST不进入普通`validate` action。

#### Scenario: 验证非Corin角色

- **WHEN** 作者验证一个使用不同Action状态名和不同连招层数的合法角色
- **THEN**通用Validator MUST只按正式authoring语义判断
- **AND** MUST不要求`None/Attack/DodgeBack/DodgeForward`或`Attack1/Attack2`

#### Scenario: 评估two_hit_combo Macro

- **WHEN** Synthesis Evaluator评估`two_hit_combo`
- **THEN** Macro coverage evaluator MUST检查该Macro的typed plan包含外层Attack、内层combo、两个攻击leaf、Timeline、combo与exit命令
- **AND**该检查 MUST只影响当前样例coverage report
- **AND**普通Graph validate MUST不执行该业务规则

### Requirement: Agent Snapshot schema v8 必须输出稳定 authoring identity

Agent Snapshot MUST使用schema v8，并为Graph、Node、Edge、Timeline、Track、Clip、Blackboard declaration与Timeline animation producer输出正式稳定authoring identity。Snapshot path和列表index MAY作为可读定位信息，但 MUST不取代identity。Snapshot MUST不输出Tree animation Driver、ExecutionLineage、LayerPlan或runtime playback lifecycle。schema v8 Snapshot MUST成为生成v8 Patch的唯一上下文，不提供v6/v7镜像输出。

#### Scenario: 导出Full Snapshot

- **WHEN** Agent exporter导出`CharacterPipelineDefinition` Full Snapshot
- **THEN**每个Graph、Node、Edge、Timeline、Track、Clip和animation producer MUST包含稳定authoring identity
- **AND** snapshot MUST标记schema v8
- **AND** snapshot MUST输出当前source revision所需的逻辑与Timeline内容

#### Scenario: Timeline Track重排后导出

- **WHEN**作者重排Track或Clip后重新导出Snapshot
- **THEN**对应元素和producer identity MUST保持
- **AND** index/path MAY更新

## MODIFIED Requirements

### Requirement: Patch IR 必须是确定性的 graph 编辑指令

系统 MUST定义schema v8 Agent Patch IR作为确定性的graph编辑指令边界。Patch IR MUST使用stable authoring id或前序operation output引用定位编辑目标，只能表达正式authoring操作，例如ensure state machine、ensure state、ensure transition、ensure condition rule、ensure state behavior node、ensure action activation/lifecycle、ensure timeline node、ensure input node、link flow和link property。资产引用 MUST作为实际消费该资产的ensure command参数，由对应正式Emitter或handler原子解析和写入。Patch IR MUST不直接写Unity YAML、GUID映射集合、runtime状态或旧配置路径，也 MUST不提供独立的通用`bind_asset_reference`操作。

#### Scenario: 添加状态

- **WHEN** Patch IR表达添加`Attack1`状态
- **THEN** lowerer MUST生成typed State command
- **AND** handler MUST通过正式节点创建入口创建`StateNode`
- **AND** Patch IR MUST不包含直接插入节点集合的操作

#### Scenario: 连接Transition

- **WHEN** Patch IR表达`Attack1 -> Attack2`
- **THEN** lowerer MUST生成typed Transition command与typed element reference
- **AND** handler MUST通过正式flow link入口创建Transition edge
- **AND**合法Transition MUST拥有inline `ConditionRuleGraph`

#### Scenario: 请求独立资产绑定

- **WHEN** schema v8 Patch包含`bind_asset_reference`
- **THEN** lowerer MUST将其作为未知operation拒绝
- **AND**系统 MUST不返回成功no-op
- **AND**资产绑定 MUST改由对应ensure command携带明确引用

### Requirement: Agent Patch 编译必须维护 identity 生命周期

Agent Patch compiler MUST在更新现有元素时保持其authoring identity，在创建新元素时生成新identity，在复制元素时生成新identity。系统 MUST只接受schema v8，不得保留v6/v7兼容解析或按path、display name猜测identity。Typed command lowering MUST在mutation前验证authoring identity格式、operation id唯一性和前序operation reference顺序。

#### Scenario: 更新现有Timeline Clip

- **WHEN** Patch修改一个由authoring identity指定的Clip参数
- **THEN** compiler MUST修改该Clip
- **AND** Clip identity MUST保持

#### Scenario: 创建新Track

- **WHEN** Patch创建新的Timeline Track
- **THEN** compiler MUST为该Track生成新identity
- **AND** validator MUST拒绝缺失或重复identity

#### Scenario: 旧schema输入

- **WHEN** Patch或Snapshot请求使用v6或v7 schema
- **THEN** service MUST返回明确unsupported schema错误
- **AND** MUST不通过converter、index、display name或path fallback apply

## REMOVED Requirements

### Requirement: Agent Snapshot schema v6 必须输出稳定 authoring identity

Agent Snapshot MUST使用 schema v6，并为 Graph、Node、Edge、Timeline、Track、Clip、Blackboard declaration 与 Timeline animation producer 输出正式稳定 authoring identity。Snapshot path 和列表 index MAY作为可读定位信息，但 MUST不取代 identity。Snapshot MUST不输出 Tree animation Driver、ExecutionLineage、LayerPlan 或 runtime playback lifecycle。

#### Scenario: 导出 Full Snapshot

- **WHEN** Agent exporter 导出 CharacterPipelineDefinition Full Snapshot
- **THEN** 每个 Graph、Node、Edge、Timeline、Track、Clip 和 animation producer MUST包含稳定 authoring identity
- **AND** snapshot MUST输出当前 source revision 所需的逻辑与 Timeline 内容

#### Scenario: Timeline Track 重排后导出

- **WHEN** 作者重排 Track 或 Clip 后重新导出 Snapshot
- **THEN** 对应元素和 producer identity MUST保持
- **AND** index/path MAY更新
