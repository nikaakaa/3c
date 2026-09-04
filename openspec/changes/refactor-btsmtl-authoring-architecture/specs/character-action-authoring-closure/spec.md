## RENAMED Requirements

- FROM: `### Requirement: Graph authoring 必须表达 request 提交而不是结构归属`
- TO: `### Requirement: 技能图必须表达局部执行与正式后续请求`

- FROM: `### Requirement: Equipment Host节点必须保持通用authoring语义`
- TO: `### Requirement: Equipment作者入口必须使用代码binding与技能引用`

## MODIFIED Requirements

### Requirement: CharacterPipelineDefinition 必须配置 ActionProfile 库

角色Definition MUST配置唯一ActionProfile库、技能定义目录和显式代码控制binding。技能定义 MUST引用已登记策略与入口图，控制binding MUST引用精确技能；不得通过角色RootTree或按策略显示名猜技能。多个技能复用策略时，实际启动请求仍必须指定唯一技能。

#### Scenario: 角色配置攻击技能

- **WHEN** 作者把SkillDefinition与ActionProfile加入角色目录
- **THEN** 构建 MUST验证控制引用、策略、技能入口和依赖闭合

#### Scenario: 同策略多个技能

- **WHEN** 两个技能引用同一ActionProfile
- **THEN** 控制请求 MUST区分具体技能，不能选择目录第一个

#### Scenario: 角色配置动作库
- **WHEN** 作者打开 `CharacterPipelineDefinition`
- **THEN** Inspector MUST 允许配置该角色可用的 ActionProfile 列表
- **AND** 初始化 pipeline 时 MUST 注册这些 profile

#### Scenario: 重复 action id
- **WHEN** 两个 ActionProfile 使用同一个 action id
- **THEN** 配置校验 MUST 报错
- **AND** 系统 MUST NOT 随机选择其中一个作为 fallback


### Requirement: 技能图必须表达局部执行与正式后续请求

技能图 MUST表达当前技能内部执行和显式后续候选；角色级输入消费、动作选择和激活改由代码控制。Graph节点、Timeline Clip或局部State的结构身份不得作为释放identity；后续候选只能进入正式决策阶段。

#### Scenario: 编辑技能组合

- **WHEN** 作者配置技能内后续动作候选
- **THEN** 作者入口 MUST验证精确技能／请求引用
- **AND** MUST不恢复角色RootTree activation节点或静态membership

#### Scenario: 创建格挡反击提交入口

- **WHEN** 作者在格挡技能内配置反击候选
- **THEN** UI MUST选择已登记Skill、ActionProfile及source RequestId
- **AND** 目标 MUST来自显式typed快照或签名参数，不能用自由target key查场景
- **AND** 候选 MUST由正式角色决策准入，不能直接激活第二角色流程

#### Scenario: 编辑网络策略

- **WHEN** 作者选中技能中的后续动作候选
- **THEN** UI MUST不暴露Action或Skill级网络模型策略
- **AND** Model Definition继续唯一配置其coverage和同步方式


### Requirement: 作者必须能显式配置动作退出语义

系统 MUST 让作者在动作流程离开点配置退出语义，而不是只配置普通 graph exit。至少 MUST 支持 `Complete`、`Cancel`、`Interrupt` 和 `Abort`；`Reject` 和 `Correct` MAY 来自网络 decision。State Transition、Tree graceful abort 和 ForceStop MUST 保持分层：State.OnExit 或正式 lifecycle 节点负责业务 terminal transition，Tree edge、通用 Runnable stop 和 TimelineNode MUST NOT 自动推导动作语义。

#### Scenario: 状态机正常结束攻击

- **WHEN** 作者配置攻击正常完成
- **THEN** root 或等价生命周期节点 MUST 提交 `Complete`
- **AND** 完成 Transition MUST NOT 再提交第二条 terminal transition

#### Scenario: 语义窗口替换动作

- **WHEN** Attack leaf 在 root 完成前通过 ComboAccept、RecoveryEarly 或 RecoveryLate 离开
- **THEN** source State.OnExit MUST 在 target 激活前提交 `Cancel(RecoveryCancel)`
- **AND** source Timeline MUST 通过 State root stop 取消
- **AND** target MUST 使用新的 Action Context
#### Scenario: Parent Tree abort 攻击 SMNode

- **WHEN** 攻击 StateMachineNode 因 Self、LowerPriority 或 Parent abort graceful stop
- **AND** source Action Context 仍 active
- **THEN** source State.OnExit MUST 能根据 StateExitContext 显式提交 `Cancel`、`Interrupt` 或 `Abort`
- **AND** SM runtime MUST NOT 自动选择其中一种业务语义
- **AND** parent Composite MUST 等待该 lifecycle 收口后启动 replacement

#### Scenario: Pipeline ForceStop

- **WHEN** Pipeline Shutdown 或 Dispose ForceStop 攻击 SMNode
- **THEN** runtime MUST 释放本地 Action/Timeline/animation owner runtime 资源
- **AND** MUST NOT 伪造 gameplay Cancel、Interrupt 或 Abort 网络事实
#### Scenario: 技能父级被取消

- **WHEN** 当前技能包含多个Running子图与Timeline
- **THEN** 作者与运行观察 MUST能定位各调用的退出进度和最终原因
- **AND** MUST不把全部子图视为同步完成


### Requirement: ActionScope 若引入必须只是作者组织层

技能作者组织、入口图与参数签名 MUST不创建独立于ActionInstance的运行生命周期。SkillExecutionState可以表达ActionInstance内部节点／调用frame，但不得建立第二套active action、prediction identity或scope服务。

#### Scenario: 查看技能执行scope

- **WHEN** 工作区显示技能及其嵌套调用
- **THEN** 释放身份 MUST来自ActionInstance，子图身份来自实例内调用位置

#### Scenario: Scope 内默认继承 Context

- **WHEN** 技能内容或子图使用调用者提供的Action Context
- **THEN** 构建 MUST显式绑定到当前ActionInstance，输出继续携带其identity
- **AND** MUST不以subtree id或ambient全局动作替代

#### Scenario: Scope 离开

- **WHEN** 技能主流程完成、取消或被打断
- **THEN** MUST通过明确ActionLifecycleTransition结束释放并处理完整清理
- **AND** 普通子图返回 MUST只结束该调用；不得仅靠停止Tick隐式销毁动作


### Requirement: TreeClip 与 Scope Variable 必须是 Timeline Window 唯一作者入口

Decision TreeClip 与 owner-local Bool Frame scope variable MUST 是 Timeline Window 唯一时间入口。Projection MUST 只保存 WindowType、WindowId、Digest 和 Action Context provenance；网络策略只属于当前 Network Model。ConditionRuleGraph MAY 用 `ActionWindowActiveInfoNode` 只读同帧 candidate，但 MUST NOT 建第二份 fact、Blackboard key、cache 或 registry。RootTree MUST NOT 暴露逐段 Cancel/MoveCancel declaration。

#### Scenario: Attack HitWindow

- **WHEN** TreeClip 写 HitWindow declaration
- **THEN** projection MUST 生成同一 ActionInstance 的 Window fact
- **AND** Network Model 决定 history 与 packet

#### Scenario: Transition 读取 RecoveryEarly

- **WHEN** TreeClip 写 projected `RecoveryEarly`
- **THEN** typed query MUST 同帧读取同一 ActionInstance、WindowId 和 Digest

#### Scenario: Projection=None

- **WHEN** TreeClip 写普通本地变量
- **THEN** ValueNode MAY 读取
- **AND** typed WindowType query MUST NOT 命中
#### Scenario: 角色代码读取技能窗口

- **WHEN** Decision TreeClip为当前技能实例写入Frame窗口候选
- **THEN** C#控制 MUST通过当前ActionInstanceId与Tick的正式投影读取
- **AND** 窗口declaration MUST归属技能／调用frame，不能归属已删除的角色State body


### Requirement: Equipment作者入口必须使用代码binding与技能引用

角色级Equipment Host与Route图节点 MUST从Character作者能力及正式调用链删除。Feature作者配置 MUST使用已登记代码binding与SkillDefinition引用；技能内装备参数与事务操作继续使用通用typed能力，不建立装备专用图、端口或解释器。

#### Scenario: 配置Feature路由

- **WHEN** 作者为装备Route绑定技能
- **THEN** MUST使用精确控制binding和技能引用
- **AND** 创建菜单 MUST不再提供角色级Equipment Host节点

#### Scenario: RootTree配置PrimaryAction Host

- **WHEN** 迁移旧RootTree中的PrimaryAction Host
- **THEN** MainWeapon／PrimaryAction关系 MUST迁成精确代码route binding与技能引用
- **AND** 旧Host节点 MUST删除，新增Feature不得要求复制角色控制器

#### Scenario: Host节点配置未知Route

- **WHEN** 迁移后的控制binding引用不属于Equipment Profile的RouteId
- **THEN** Inspector与Compiler MUST失败
- **AND** MUST不建立自由字符串Route或恢复旧Host节点
