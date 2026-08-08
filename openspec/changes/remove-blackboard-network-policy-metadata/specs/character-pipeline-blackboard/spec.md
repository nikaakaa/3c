## RENAMED Requirements

- FROM: `### Requirement: InputDerived Blackboard 必须从正式 portable input 投影`
- TO: `### Requirement: Blackboard 输入绑定必须从正式 portable input 投影`

## MODIFIED Requirements

### Requirement: Blackboard Variable 必须声明类型、作用域和生命周期

每个 Pipeline Blackboard variable MUST声明稳定 declaration identity、owner 内唯一 key、值类型、默认值、declaration owner、作用域、生命周期和层级 category path。运行时 MUST按 declaration 初始化、校验、寻址、清理和展示变量。`Config` lifetime MUST是 runtime 只读。输入绑定与事实投影 MUST作为独立可选 payload 存在；declaration MUST不保存 authority、sync policy、replication、correction 或其它 Network Model 策略。系统 MUST NOT依赖散字符串 key、`object` 值或非法 scope/lifetime 组合作为正式业务合同。

#### Scenario: State 作用域变量

- **WHEN** 某个变量声明为 State scope 且生命周期为 StateEnterToExit
- **THEN** 进入 `StateMachineExecutionScope` 时 runtime MUST为该 activation 初始化独立 bucket
- **AND** 离开该 execution scope 时 runtime MUST只清理该 activation 的值
- **AND** 其它并行状态机和后续 activation MUST NOT被清理或读到遗留值

#### Scenario: Graph 局部配置

- **WHEN** 一个状态行为 Graph 需要只在该 Graph 内可见的只读调参值
- **THEN** 作者 MUST能声明 `Graph + Config` variable
- **AND** declaration MUST随该 inline/shared Graph 序列化
- **AND** runtime MUST拒绝对该 Config variable 的写入
- **AND** MUST不要求作者选择 ConfigVersion 或 LocalOnly 标签

#### Scenario: 非法 scope 和 lifetime

- **WHEN** 作者将 State scope 配成 ManualClear 或将 Frame scope 配成 Spawn
- **THEN** authoring validation MUST报告非法组合
- **AND** runtime MUST NOT猜测清理时机或降级成 Character scope

#### Scenario: 类型不匹配

- **WHEN** 节点以 Float 读取声明为 Vector2 的 variable
- **THEN** graph validation 和 runtime MUST报告类型不匹配
- **AND** 系统 MUST NOT尝试字符串转换、默认零值或其它 fallback

### Requirement: ExposedProperty 必须成为 Pipeline Blackboard 的 authoring 表面

BaseExposedProperty MUST继续是 Pipeline Blackboard declaration 的唯一 authoring/serialization 表面。基础 declaration、可选 Input Binding 与可选 Fact Projection MUST通过三个独立 typed authoring API 配置；Compiler MUST将 declaration owner、reference、scope、lifetime、default value、InputValueId 与 projection 编译进 Program layout。Runtime MUST不同时维护 CharacterGraphContext dictionary、局部散字段、第二 Blackboard service 或变量级网络策略表。

#### Scenario: State body 创建 Local 变量

- **WHEN** 作者在 inline State body 创建 State scope declaration
- **THEN** declaration MUST仍归属该 Graph authoring
- **AND** Compiler MUST生成对应 owner/layout entry
- **AND** 创建入口 MUST不填充 authority 或 sync policy 默认值

#### Scenario: 为 Character 变量增加输入绑定

- **WHEN** 作者为既有 Character/Spawn declaration 配置 Input Binding
- **THEN** authoring MUST只增加稳定 InputValueId payload
- **AND** MUST不修改基础 declaration owner、scope、lifetime 或 fact projection

### Requirement: Decision TreeClip 必须通过声明式 Frame Blackboard 输出决策

Decision TreeClip写入的变量 MUST来自ExposedProperty对应的Pipeline Blackboard declaration，并且 MUST使用`Frame` scope和`Frame` lifetime。Runtime MUST在Frame开始推进当前Frame generation，在当前clip active时重新求值并写入，并在State.OnExit完成后的Frame结束统一flush当前generation的projection candidate。Frame value读取发现owner generation不匹配时 MUST表现为declaration default且不得物理写入State；只有当前Frame第一次真实写入才可materialize value、typed owner token与write stamp。Frame结束 MUST使该generation后续不可读、不可投影，但 MUST不通过遍历全部Frame group写默认值或清空State实现。没有Fact Projection的写入 MUST保持本地；显式ActionWindow projection MUST继续通过唯一projection stage暂存candidate并在EndFrame生成正式fact。

#### Scenario: Dodge恢复段开放动作切换

- **WHEN** Dodge Timeline的`RecoveryOpen` Decision TreeClip在当前Tick active
- **THEN** Tree MUST写入owner-local Bool Frame declaration
- **AND** 唯一projection stage MUST暂存当前ActionInstance的ActionWindow candidate
- **AND** Dodge Transition MUST能在同一Tick通过`ActionWindowActiveInfoNode`读取该WindowType

#### Scenario: Decision clip不再active

- **WHEN** 新logic frame中Decision TreeClip不在active时间范围
- **THEN** Frame Blackboard MUST把上一generation的true表现为declaration default
- **AND** Runtime MUST NOT依赖OnDisable写false或EndFrame物理清零才能关闭gate

#### Scenario: 当前Frame没有Decision写入

- **WHEN** 当前Tick没有任何Decision TreeClip写入某个Frame declaration
- **THEN** 读取 MUST返回declaration default且不能生成write provenance或projection
- **AND** 该declaration的value、owner、provenance和candidate state MUST不因Frame begin/end被标记dirty

#### Scenario: 声明合同冲突

- **WHEN** Timeline inline Tree与RootTree对同一Blackboard key声明不同类型、scope、lifetime、Input Binding或Fact Projection
- **THEN** validator或runtime MUST报告配置错误
- **AND** 系统 MUST NOT选择任一声明作为fallback

### Requirement: Blackboard declaration 必须显式声明 fact projection

Pipeline Blackboard declaration MAY保存一个独立显式Fact Projection payload。ActionWindow projection MUST只允许Bool、Frame scope和Frame lifetime，并 MUST保存稳定WindowType、WindowId与Digest。Projection MUST不保存Network Model policy；Program MUST只负责产生带ActionInstance与EventId的 `ActionWindowFact`。具体Model Egress只有在自己的正式fact-kind coverage支持ActionWindow时才可消费；ActionProfile、Blackboard declaration、Graph与Timeline MUST不复制模型配置。非法projection MUST由authoring validator和runtime拒绝，不得fallback为普通变量或默认Window。

#### Scenario: ActionWindow-bound Frame variable

- **WHEN** active Decision TreeClip 在当前 Tick 写入合法 ActionWindow-bound variable=true
- **AND** 写入 provenance 包含有效 Action Context
- **THEN** runtime MUST记录一个本帧 projection candidate
- **AND** Program 决策完成后的统一 projection MUST最多生成一个对应 `ActionWindowFact`
- **AND** 当前ServerAuthoritative模型没有ActionWindow packet映射时 MUST保持该fact为本地Gameplay输出，不得推导默认packet

#### Scenario: 缺失 Action Context

- **WHEN** ActionWindow-bound variable 的写入 provenance 没有有效 Action Context
- **THEN** validator 或 runtime MUST报告错误
- **AND** 系统 MUST NOT使用 ambient current action、最后 active action 或默认 ActionInstance 补齐

#### Scenario: 同一变量被不同 ActionInstance 写入

- **WHEN** 同一 declaration 在同一 Tick 由两个不同 ActionInstance provenance 写入 true
- **THEN** projection MUST按 ActionInstance 保留两个独立 candidate
- **AND** 最终单一 Blackboard Bool value MUST NOT导致任一 ActionInstance 身份丢失

### Requirement: Pipeline Blackboard declaration 必须作为 Graph Data Catalog 的正式来源

Pipeline Blackboard MUST将当前 context 可见的 `BaseExposedProperty` declaration 投影到唯一 `Graph Data Catalog`，保留 identity、真实 owner、local/inherited、类型、scope、lifetime、category、可选Input Binding、可选Fact Projection和默认值。Catalog MUST NOT复制 declaration、建立第二套变量或窗口配置，也 MUST不显示或编辑 authority、sync policy、replication 或 correction 字段。

#### Scenario: 显示 inline Timeline 本地 declaration

- **WHEN** 作者打开拥有 local `RecoveryOpen` declaration 的 inline Timeline
- **THEN** Catalog MUST显示 Timeline owner 的 local editable 条目
- **AND** 显示 ActionWindow projection、WindowType 与稳定 identity
- **AND** MUST不显示 SyncFact 或其它网络策略标签

#### Scenario: 显示 RootTree declaration

- **WHEN** inline state body 可见 RootTree 的 `RunThreshold`
- **THEN** Catalog MUST显示 inherited read-only 条目和真实 owner

#### Scenario: 同 key 不同 owner

- **WHEN** 两个 owner 有同名但不同 identity 的 declaration
- **THEN** Catalog MUST按 identity 和 owner 区分，MUST NOT合并

### Requirement: Blackboard 输入绑定必须从正式 portable input 投影

Pipeline Blackboard declaration MAY保存一个可选 Input Binding，且 payload MUST只包含唯一非空 `InputValueId`。Input Binding MUST只允许 Character scope、Spawn lifetime。Compiler MUST将 declaration、Program input catalog kind与typed Character State address编译为唯一 input-to-state binding；Float32与Fixed Evaluate MUST在Timeline Decision和Graph control之前，把当前 Tick同名同类型的portable input写入该slot。系统 MUST不保存 InputDerived mode，不按Blackboard key猜input，不从Presentation或Scene对象补值，也 MUST不在Host中建立第二条Blackboard直写路径。

#### Scenario: 投影攻击目标输入

- **WHEN** 当前 Tick包含`ActionTargetSnapshot` input且Corin declaration的Input Binding引用同一`InputValueId`
- **THEN** Blackboard Input Binding阶段 MUST在Action admission和activation之前写入该目标快照
- **AND** `CanActivateAction`与`ActivateActionInstance` MUST读取同一Character State transaction中的值

#### Scenario: 输入类型与声明不一致

- **WHEN** Program binding要求`ActionTargetSnapshot`但输入提交其它value kind
- **THEN** 当前 Evaluate MUST明确失败
- **AND** 系统 MUST不写入默认对象、裸字符串或上一 Tick残留值后继续执行

#### Scenario: declaration 没有 Input Binding

- **WHEN** 普通 Config、Frame 或 AI declaration 没有 Input Binding payload
- **THEN** Compiler MUST不为其建立 input-to-state binding
- **AND** MUST不使用旧 sync policy 默认值推断绑定

