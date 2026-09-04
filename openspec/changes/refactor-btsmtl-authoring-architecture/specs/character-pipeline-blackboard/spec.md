## RENAMED Requirements

- FROM: `### Requirement: InputDerived Blackboard 必须从正式 portable input 投影`
- TO: `### Requirement: 角色与技能输入必须从正式 portable input 投影`

## MODIFIED Requirements

### Requirement: Pipeline Blackboard 必须统一图变量和运行时黑板

技能Blackboard declaration与ExposedProperty MUST继续是图内变量的唯一作者来源，并编译进实例／调用scope的typed状态。C#角色控制状态 MUST由控制模块schema声明，不得由可写Graph Blackboard镜像。技能只能经显式只读事实读取控制信息，通过typed请求要求控制变化。

#### Scenario: 技能读取移动模式

- **WHEN** 技能条件需要已声明的角色移动事实
- **THEN** MUST读取正式只读投影
- **AND** MUST不复制或写入控制模块私有状态

#### Scenario: 技能写局部变量

- **WHEN** 子图更新本次调用的变量
- **THEN** MUST只修改所属实例typed状态

#### Scenario: Compiled ValueNode 读取变量

- **WHEN** ConditionRuleGraph operation 读取 Blackboard declaration
- **THEN** MUST通过 compiled address 访问 CharacterSimulationState
- **AND** MUST不反射 authoring ExposedProperty object


### Requirement: Blackboard Variable 必须声明类型、作用域和生命周期

每个技能变量 MUST声明稳定identity、owner内唯一key、类型、默认值、owner、scope、lifetime及category。输入绑定和fact projection MUST使用明确typed字段，不恢复变量级网络策略或任意object值。Config MUST只读；Graph、State、ActionInstance与Frame scope MUST在所属技能／调用frame内解析，角色控制状态不由这些变量声明。

#### Scenario: 非法变量配置

- **WHEN** 变量使用非法scope／lifetime、重复key或已删除网络策略字段
- **THEN** 作者校验与Document MUST拒绝

#### Scenario: 共享Config

- **WHEN** 多个实例读取同技能只读配置
- **THEN** MUST得到相同配置且不能通过变量Set改写模板

#### Scenario: State 作用域变量

- **WHEN** 技能局部变量声明为 State scope 且生命周期为 StateEnterToExit
- **THEN** 进入 `StateMachineExecutionScope` 时 runtime MUST 为该 activation 初始化独立typed状态区
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

技能局部变量 MUST继续使用唯一ExposedProperty作者／序列化表面，构建解析声明、绑定、scope、默认值与projection。代码控制字段 MUST由独立模块schema拥有，并以显式只读事实供技能使用，不伪造可写ExposedProperty。Runtime不得维护GraphContext字典或第二Blackboard服务。

#### Scenario: 新增技能局部变量

- **WHEN** 作者经UI或Document添加变量
- **THEN** 两者 MUST生成同一声明和typed布局

#### Scenario: 编辑控制状态

- **WHEN** 作者尝试把代码控制字段当局部可写变量
- **THEN** Capability与Mutation MUST拒绝

#### Scenario: State body 创建 Local 变量

- **WHEN** 作者在 inline State body 创建 State scope declaration
- **THEN** declaration MUST仍归属该 Graph authoring
- **AND** Compiler MUST生成对应 owner/layout entry


### Requirement: Runtime value 必须按 declaration 与 scope owner 共同寻址

技能变量地址 MUST结合声明、编译调用位置、ActionInstance与activation generation；局部State还需完整执行路径，Frame需当前SimulationTick。实际地址 MUST使用typed索引／handle，不以对象、字典identity或拼接显示路径为真值。跨子图参数／输出或允许的owner绑定必须显式声明；写入投影保留typed provenance，诊断按需格式化。

#### Scenario: 同子图并行调用

- **WHEN** 两个调用读取和写入同一声明模板
- **THEN** 值和write stamp MUST按当前释放与调用generation隔离

#### Scenario: 恢复调用作用域

- **WHEN** snapshot恢复到嵌套子图中间
- **THEN** 所有变量 MUST解析到同一调用frame，不继承之后的generation

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


### Requirement: Decision TreeClip 必须通过声明式 Frame Blackboard 输出决策

Decision TreeClip写入的变量 MUST来自ExposedProperty对应的Pipeline Blackboard declaration，并且 MUST使用`Frame` scope和`Frame` lifetime。Runtime MUST在Frame开始推进当前Frame generation，在当前clip active时重新求值并写入，并在技能owner停止／局部State.OnExit完成后的Frame结束统一flush当前generation的projection candidate。Frame value读取发现owner generation不匹配时 MUST表现为declaration default且不得物理写入State；只有当前Frame第一次真实写入才可materialize value、typed owner token与write stamp。Frame结束 MUST使该generation后续不可读、不可投影，但 MUST不通过遍历全部Frame group写默认值或清空State实现。Projection=None的写入 MUST保持本地；显式ActionWindow projection MUST继续通过唯一projection stage暂存candidate并在EndFrame生成正式fact。

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
#### Scenario: 取消某个技能实例

- **WHEN** 同Actor一个释放取消而另一个仍运行
- **THEN** 只被取消owner的候选 MUST失效，不能清空另一实例Frame值


### Requirement: Blackboard declaration 必须显式声明 fact projection

Pipeline Blackboard declaration MAY保存一个显式fact projection。ActionWindow projection MUST只允许Bool、Frame scope、Frame lifetime及明确ActionWindow投影声明，并 MUST保存稳定WindowType、WindowId与Digest。Projection MUST不保存Network Model policy；Program MUST只负责产生带ActionInstance与EventId的 `ActionWindowFact`。具体Model Egress只有在自己的正式fact-kind coverage支持ActionWindow时才可消费；ActionProfile、Blackboard declaration、Graph与Timeline MUST不复制模型配置。非法projection MUST由authoring validator和runtime拒绝，不得fallback为普通变量或默认Window。

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


### Requirement: 角色与技能输入必须从正式 portable input 投影

角色控制输入 MUST按正式InputValueId及类型进入当前Target事务，并在技能Decision与角色控制决策前可用。技能读取输入 MUST绑定同一portable input合同或显式调用参数；不得保留依赖角色RootTree的可写Character Blackboard输入镜像。迁移必须保留原InputValueId、值类型和输入时序，禁止按key猜测、从Scene／Presentation补值或由Host直写黑板。

#### Scenario: 角色与技能读取同一输入

- **WHEN** 当前Tick输入MoveAxis或释放按键
- **THEN** 控制代码与技能 MUST看到同一规范输入值和Tick

#### Scenario: 移除旧输入镜像

- **WHEN** 旧角色图InputDerived声明迁移到控制输入合同
- **THEN** 旧声明和重复写入链 MUST删除，技能引用须转换为正式输入绑定

#### Scenario: 投影攻击目标输入

- **WHEN** 当前Tick含ActionTargetSnapshot且输入合同绑定同一InputValueId
- **THEN** 目标 MUST在准入及activation前进入同一事务的正式输入事实
- **AND** 纯查询与提交 MUST读取相同快照，不创建Character Blackboard镜像

#### Scenario: 输入类型与声明不一致

- **WHEN**Program binding要求`ActionTargetSnapshot`但输入提交其它value kind
- **THEN**当前 Evaluate MUST明确失败
- **AND**系统 MUST不写入默认对象、裸字符串或上一 Tick残留值后继续执行
