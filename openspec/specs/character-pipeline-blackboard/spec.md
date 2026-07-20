# character-pipeline-blackboard Specification

## Purpose
定义角色 Pipeline Blackboard 的声明、类型、作用域、运行时读写、ConditionRuleGraph 读取和 GameplayFact 投影边界。
## Requirements
### Requirement: Pipeline Blackboard 必须统一图变量和运行时黑板

Blackboard declaration、ExposedProperty authoring、Graph Data Catalog 和 scope/lifetime 语义 MUST继续是唯一黑板数据源。Compiler MUST将 declaration/reference 解析为 Program layout，Kernel MUST只通过 CharacterSimulationState Blackboard slots 读写。

#### Scenario: Compiled ValueNode 读取变量

- **WHEN** ConditionRuleGraph operation 读取 Blackboard declaration
- **THEN** MUST通过 compiled address 访问 CharacterSimulationState
- **AND** MUST不反射 authoring ExposedProperty object

### Requirement: Blackboard Variable 必须声明类型、作用域和生命周期

每个 Pipeline Blackboard variable MUST 声明稳定 declaration identity、owner 内唯一 key、值类型、默认值、declaration owner、作用域、生命周期、authority、sync policy 和层级 category path。运行时 MUST 按 declaration 初始化、校验、寻址、清理和展示变量。`Config` lifetime MUST 是 runtime 只读。系统 MUST NOT 依赖散字符串 key、`object` 值或非法 scope/lifetime 组合作为正式业务合同。

#### Scenario: State 作用域变量

- **WHEN** 某个变量声明为 State scope 且生命周期为 StateEnterToExit
- **THEN** 进入 `StateMachineExecutionScope` 时 runtime MUST 为该 activation 初始化独立 bucket
- **AND** 离开该 execution scope 时 runtime MUST 只清理该 activation 的值
- **AND** 其它并行状态机和后续 activation MUST NOT 被清理或读到遗留值

#### Scenario: Graph 局部配置

- **WHEN** 一个状态行为 Graph 需要只在该 Graph 内可见的只读调参值
- **THEN** 作者 MUST 能声明 `Graph + Config` variable
- **AND** declaration MUST 随该 inline/shared Graph 序列化
- **AND** runtime MUST 拒绝对该 Config variable 的写入

#### Scenario: 非法 scope 和 lifetime

- **WHEN** 作者将 State scope 配成 ManualClear 或将 Frame scope 配成 Spawn
- **THEN** authoring validation MUST 报告非法组合
- **AND** runtime MUST NOT 猜测清理时机或降级成 Character scope

#### Scenario: 类型不匹配

- **WHEN** 节点以 Float 读取声明为 Vector2 的 variable
- **THEN** graph validation 和 runtime MUST 报告类型不匹配
- **AND** 系统 MUST NOT 尝试字符串转换、默认零值或其它 fallback

### Requirement: ExposedProperty 必须成为 Pipeline Blackboard 的 authoring 表面

BaseExposedProperty MUST继续是 Pipeline Blackboard declaration 的唯一 authoring/serialization 表面。Compiler MUST将 declaration owner、reference、scope、lifetime、default value 与 projection 编译进 Program layout；Runtime MUST不同时维护 CharacterGraphContext dictionary、局部散字段或第二 Blackboard service。

#### Scenario: State body 创建 Local 变量

- **WHEN** 作者在 inline State body 创建 State scope declaration
- **THEN** declaration MUST仍归属该 Graph authoring
- **AND** Compiler MUST生成对应 owner/layout entry

### Requirement: Transition Rule 必须通过纯 ValueNode 读取黑板

ConditionRuleGraph 中读取 Pipeline Blackboard variable 的节点 MUST 是纯 ValueNode 兼容节点，并保存显式 declaration reference。该节点 MUST 不 tick Timeline、Action、RunnableNode 或状态行为 graph。现有 Runnable 形态的 ExposedPropertyNode MUST NOT 被放入 ConditionRuleGraph。读取失败 MUST 使本次条件求值失败，系统 MUST NOT 向输出端口写入零值、空值或 authoring 默认值后继续比较。

#### Scenario: 读取移动阈值

- **WHEN** Idle 到 WalkStart 的 Transition 需要比较输入幅度和 `WalkThreshold`
- **THEN** 规则图 MUST 通过显式 reference 读取 Character `WalkThreshold`
- **AND** Compare/And/Or 等纯条件节点 MUST 负责组合最终 Bool
- **AND** 规则图 MUST NOT 使用 Runnable `ExposedPropertyNode` 或裸字符串 key

#### Scenario: 规则图引用缺失 declaration

- **WHEN** ConditionRuleGraph 引用的 declaration 已删除、不可见或无法解析 owner
- **THEN** 校验 MUST 报告非法结构
- **AND** runtime MUST 让本次规则求值失败
- **AND** runtime MUST NOT 用硬编码默认值让 Transition 继续求值

### Requirement: Runtime Fact 和 Blackboard Variable 必须命名分层

Blackboard variable MUST只表达 Program 内运行变量、调参值或当前 scope state；`SimulationActorTickResult` typed gameplay fact MUST表达当前 Tick 已发生、可记录、调试或由模型消费的事实。只有正式 fact projection MAY从当前 Blackboard write provenance 生成 typed fact。Model adapter 与 Committer MUST不直接读取 Blackboard key/value。

#### Scenario: Timeline 产出攻击窗口

- **WHEN** Decision TreeClip 写入合法 ActionWindow-bound Frame variable
- **THEN** Program MUST让后续 operation 读取该 variable
- **AND** projection MUST另外产生带 ActionInstance 与 EventId 的 ActionWindow fact

#### Scenario: 本地调参变量

- **WHEN** RunThreshold 只参与 ConditionRuleGraph
- **THEN** MUST保持 Config Blackboard 语义
- **AND** MUST不自动成为 `SimulationActorTickResult.GameplayFacts` 中的 fact

### Requirement: Runtime value 必须按 declaration 与 scope owner 共同寻址

Compiler MUST为declaration identity、Character、Graph activation、State execution path、ActionInstance和Frame owner生成稳定compiled address rule。Program layout MUST为每个scope owner分配稳定CompiledOwnerIndex；Kernel MUST使用`ScopeKind + CompiledOwnerIndex + Generation`的typed owner token隔离实例，MUST不使用runtime object reference、dictionary object identity、拼接字符串或显示路径作为真值地址。Character与Graph Config owner MUST在初始State建立；Graph、State和Action generation MUST来自各自正式lifecycle；Frame generation MUST来自当前SimulationTick。需要fact projection的真实写入 MUST保存typed write stamp，人类可读owner/provenance只能由diagnostics按需格式化。

#### Scenario: 两次State activation

- **WHEN** 同一State第二次进入
- **THEN** 新owner generation MUST与上一次State activation隔离
- **AND** Runtime MUST不通过字符串execution path比较或旧value清零来建立隔离

#### Scenario: 两个ActionInstance使用同一declaration

- **WHEN** 同一Action-scoped declaration先后由两个ActionInstance写入
- **THEN** typed owner token MUST使用各自ActionInstance generation
- **AND** 后一个instance MUST不读取或投影前一个instance的值

#### Scenario: Diagnostics显示State owner

- **WHEN** diagnostics实际请求Blackboard owner或write provenance
- **THEN** formatter MAY通过Program SourceMap和typed token生成可读路径
- **AND** 关闭diagnostics的正常Tick MUST不构造该字符串

### Requirement: Pipeline Blackboard authoring 必须提供上下文化分类视图

Pipeline Blackboard authoring MUST 提供 scope、当前上下文、层级 `CategoryPath` 和文本搜索视图。Graph tab 与 Transition selection MUST 都能访问该视图。分类 MUST 是 declaration metadata 与 UI 视图，MUST NOT 创建按分类拆分的 Blackboard asset 或第二套配置来源。

#### Scenario: 在 Transition 中选择变量

- **WHEN** 作者打开 Transition rule 并添加 blackboard ValueNode
- **THEN** picker MUST 展示当前 rule 可见且类型兼容的 declarations
- **AND** 作者 MUST 能按 scope 和 category 定位变量
- **AND** 选择 inherited declaration MUST 只创建 reference

#### Scenario: 查看局部动作变量

- **WHEN** 作者打开 Attack1 state body 并筛选 `Current Context + Action`
- **THEN** 面板 MUST 只展示当前上下文可见的 ActionInstance declarations
- **AND** 每项 MUST 显示 declaration owner 与 Local/Inherited 状态

### Requirement: Decision TreeClip 必须通过声明式 Frame Blackboard 输出决策

Decision TreeClip写入的变量 MUST来自ExposedProperty对应的Pipeline Blackboard declaration，并且 MUST使用`Frame` scope和`Frame` lifetime。Runtime MUST在Frame开始推进当前Frame generation，在当前clip active时重新求值并写入，并在State.OnExit完成后的Frame结束统一flush当前generation的projection candidate。Frame value读取发现owner generation不匹配时 MUST表现为declaration default且不得物理写入State；只有当前Frame第一次真实写入才可materialize value、typed owner token与write stamp。Frame结束 MUST使该generation后续不可读、不可投影，但 MUST不通过遍历全部Frame group写默认值或清空State实现。Projection=None的写入 MUST保持本地；显式ActionWindow projection MUST继续通过唯一projection stage暂存candidate并在EndFrame生成正式fact。

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

#### Scenario: 声明策略冲突

- **WHEN** Timeline inline Tree与RootTree对同一Blackboard key声明不同类型、scope、lifetime、authority或sync policy
- **THEN** validator或runtime MUST报告配置错误
- **AND** 系统 MUST NOT选择任一声明作为fallback
### Requirement: Decision TreeClip 必须保持纯决策边界

Decision TreeClip graph MUST 只包含允许的纯读取、值转换、条件组合和 Blackboard 写入能力。它 MUST NOT 包含跨 Tick Running、Wait、TimelineNode、Action lifecycle、Motion、Cue、Camera、GameplayResult、网络发送或场景副作用节点。

#### Scenario: Decision Tree 包含副作用节点

- **WHEN** 作者在 Decision TreeClip 下钻 Graph 中加入 Motion 或 Cue 提交节点
- **THEN** graph validator MUST 报告非法节点能力
- **AND** runtime MUST NOT 执行该 Decision Graph

### Requirement: Blackboard declaration 必须显式声明 fact projection

Pipeline Blackboard declaration MAY保存一个显式fact projection。ActionWindow projection MUST只允许Bool、Frame scope、Frame lifetime和SyncFact policy，并 MUST保存稳定WindowType、WindowId与Digest。Projection MUST不保存Network Model policy；Program MUST只负责产生带ActionInstance与EventId的 `ActionWindowFact`。具体Model Egress只有在自己的正式fact-kind coverage支持ActionWindow时才可消费；ActionProfile、Blackboard declaration、Graph与Timeline MUST不复制模型配置。非法projection MUST由authoring validator和runtime拒绝，不得fallback为普通变量或默认Window。

#### Scenario: ActionWindow-bound Frame variable

- **WHEN** active Decision TreeClip 在当前 Tick 写入合法 ActionWindow-bound variable=true
- **AND** 写入 provenance 包含有效 Action Context
- **THEN** runtime MUST 记录一个本帧 projection candidate
- **AND** Program 决策完成后的统一 projection MUST 最多生成一个对应 `ActionWindowFact`
- **AND** 当前ServerAuthoritative模型没有ActionWindow packet映射时 MUST保持该fact为本地Gameplay输出，不得推导默认packet

#### Scenario: 缺失 Action Context

- **WHEN** ActionWindow-bound variable 的写入 provenance 没有有效 Action Context
- **THEN** validator 或 runtime MUST 报告错误
- **AND** 系统 MUST NOT 使用 ambient current action、最后 active action 或默认 ActionInstance 补齐

#### Scenario: 同一变量被不同 ActionInstance 写入

- **WHEN** 同一 declaration 在同一 Tick 由两个不同 ActionInstance provenance 写入 true
- **THEN** projection MUST 按 ActionInstance 保留两个独立 candidate
- **AND** 最终单一 Blackboard Bool value MUST NOT 导致任一 ActionInstance 身份丢失

### Requirement: Blackboard 写入 provenance 必须支撑事实投影

需要 fact projection 的 Blackboard 写入 MUST 携带结构化 provenance，包括 local logic tick、source Graph/runtime owner 和 projection 所需的业务上下文。Timeline TreeClip 写入 MUST 从正式 Clip runtime context 获得 playback/clip/cycle 与 Action Context；非 Timeline action 写入 MUST 显式提供 Action Context。Provenance MUST 只服务当前 frame 的正式投影与 debug，不得成为第二套持久化 Blackboard 或 authoring 数据。

#### Scenario: Timeline TreeClip 写入

- **WHEN** Scheduler 求值某个 Action Timeline 的 Decision TreeClip
- **THEN** Blackboard write provenance MUST 包含该 playback 和 Action Context
- **AND** runtime MUST NOT 从 Timeline asset 本身推导 ActionInstance

#### Scenario: Frame cleanup

- **WHEN** 当前 logic frame 完成
- **THEN** Frame variable 与未消费 projection candidate MUST 被清理
- **AND** 后续 Tick MUST NOT读取或投影上一 Tick的 Window 值

### Requirement: Pipeline Blackboard declaration 必须作为 Graph Data Catalog 的正式来源

Pipeline Blackboard MUST 将当前 context 可见的 `BaseExposedProperty` declaration 投影到唯一 `Graph Data Catalog`，保留 identity、真实 owner、local/inherited、类型、scope、lifetime、authority、sync policy、category、projection 和默认值。Catalog MUST NOT 复制 declaration 或建立第二套变量、窗口配置。

#### Scenario: 显示 inline Timeline 本地 declaration

- **WHEN** 作者打开拥有 local `RecoveryOpen` declaration 的 inline Timeline
- **THEN** Catalog MUST 显示 Timeline owner 的 local editable 条目
- **AND** 显示 ActionWindow projection、WindowType 与稳定 identity

#### Scenario: 显示 RootTree declaration

- **WHEN** inline state body 可见 RootTree 的 `RunThreshold`
- **THEN** Catalog MUST 显示 inherited read-only 条目和真实 owner

#### Scenario: 同 key 不同 owner

- **WHEN** 两个 owner 有同名但不同 identity 的 declaration
- **THEN** Catalog MUST 按 identity 和 owner 区分，MUST NOT 合并
### Requirement: Blackboard Catalog source 必须按 declaration 所有权限制写操作

Blackboard catalog source MUST 只允许作者编辑或删除当前 owner 持有的本地 declaration。继承 declaration MUST 是只读投影，并 MAY 提供定位原 owner 的命令。新增 declaration MUST 使用当前 owner 的正式 authoring API，并 MUST 遵守既有 scope/lifetime 合法组合。系统 MUST NOT 在当前 Graph 复制继承 declaration、静默改变 owner 或使用 fallback scope。

#### Scenario: 编辑本地默认值

- **WHEN** 作者在目录详情中修改当前 owner 的本地 Config declaration 默认值
- **THEN** 系统 MUST 更新该 owner 的原 declaration

#### Scenario: 删除继承 declaration

- **WHEN** 作者查看从 RootTree 继承的 declaration
- **THEN** 目录 MUST 不提供针对当前 inline graph 的删除命令

#### Scenario: 新建 State variable

- **WHEN** 当前 Graph owner 支持 State scope 且作者通过目录创建 State variable
- **THEN** 系统 MUST 创建属于当前 owner 的合法 declaration

### Requirement: Blackboard Catalog source 必须复用上下文化可见性和节点引用链路

Blackboard catalog source MUST 复用 Pipeline Blackboard 已有的 Graph/Transition context、local/inherited 可见性解析和显式 declaration reference 节点工厂。目录 MUST NOT 重新实现一套 owner 查找、裸 key 匹配或 runtime dictionary 查询。拖拽创建失败时 MUST 保持失败并报告原因，MUST NOT 写入零值、默认值或 object fallback 后继续 authoring。

#### Scenario: Transition 读取阈值

- **WHEN** 作者把可见的 `RunThreshold` 从目录拖入 ConditionRuleGraph
- **THEN** 系统 MUST 创建保存显式 declaration reference 的纯 ValueNode 兼容节点

#### Scenario: declaration 在当前 context 不可见

- **WHEN** 某 declaration 不属于当前 Graph 的 local/inherited 可见集合
- **THEN** 目录 MUST 不展示该条目，也 MUST NOT 通过裸 key 搜索把它加入结果

#### Scenario: 引用目标已失效

- **WHEN** 条目对应 declaration 在拖拽完成前被删除或 owner context 已切换
- **THEN** 节点创建 MUST 失败并报告失效引用，MUST NOT 创建绑定默认值的节点

### Requirement: 嵌套状态机必须按 declaration owner 解析 State activation frame

Nested StateMachine MUST使用 Program 中编译的 declaration owner 和完整 execution path 定位 State frame。Runtime MUST不从 Graph clone 或显示名推断 owner。

#### Scenario: 内层 State 读取自己的 Frame

- **WHEN** 内层 State operation 读取 State-scoped variable
- **THEN** MUST命中完整 outer-to-inner path 对应的 owner bucket

### Requirement: Gameplay Effect 不得存入 Pipeline Blackboard

GameplayTag、Attribute、ActiveEffect、stack、duration、period、inhibition 与 journal MUST只存在于 CharacterSimulationState 的正式 GE slots。Blackboard MAY保存局部计算值或 fact projection source，但 MUST不复制 GE 真值。Value operation MUST通过正式 GE query读取 Attribute/Tag。

#### Scenario: Graph 读取 Health

- **WHEN** compiled Value operation读取当前 Health
- **THEN** MUST通过 GE state query读取
- **AND** MUST不从同名 Blackboard slot读取

### Requirement: InputDerived Blackboard 必须从正式 portable input 投影

`InputDerived` declaration MUST显式保存唯一 `InputValueId`，并且只允许 Character scope、Spawn lifetime。Compiler MUST将 declaration、Program input catalog kind与typed Character State address编译为唯一 input-to-state binding；Float32与Fixed Evaluate MUST在Timeline Decision和Graph control之前，把当前 Tick同名同类型的portable input写入该slot。系统 MUST不按Blackboard key猜input，不从Presentation或Scene对象补值，也 MUST不在Host中建立第二条Blackboard直写路径。

#### Scenario: 投影攻击目标输入

- **WHEN**当前 Tick包含`ActionTargetSnapshot` input且Corin declaration绑定同一`InputValueId`
- **THEN**InputDerived阶段 MUST在Action admission和activation之前写入该目标快照
- **AND**`CanActivateAction`与`ActivateActionInstance` MUST读取同一Character State transaction中的值

#### Scenario: 输入类型与声明不一致

- **WHEN**Program binding要求`ActionTargetSnapshot`但输入提交其它value kind
- **THEN**当前 Evaluate MUST明确失败
- **AND**系统 MUST不写入默认对象、裸字符串或上一 Tick残留值后继续执行
                                                                                                
