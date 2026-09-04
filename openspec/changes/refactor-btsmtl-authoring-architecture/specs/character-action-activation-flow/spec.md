## RENAMED Requirements

- FROM: `### Requirement: Graph 必须通过 ActivateActionInstance operation 产生 ActionInstance`
- TO: `### Requirement: 角色控制必须通过唯一准入建立 ActionInstance`

## MODIFIED Requirements

### Requirement: 角色控制必须通过唯一准入建立 ActionInstance

角色代码 MUST通过唯一Action准入与activation服务建立ActionInstance，并传入精确SkillDefinition／Program、ActionProfile和来源。C#控制State MAY在保持active时形成请求；activation MUST不隐式触发控制Transition，State进入或退出 MUST不被当作技能建立或终止的替代操作，需要同时发生时必须显式提交相应请求。角色级ActivateActionInstance图节点与调用链 MUST删除。技能Tree／Timeline只能消费自己的Action Context或提交后续候选，不直接建立第二个释放循环。

#### Scenario: 从输入启动技能

- **WHEN** 代码控制接受LightAttack请求
- **THEN** MUST通过唯一入口建立ActionInstance和显式Action Context
- **AND** 普通移动 MUST不被迫建立ActionInstance

#### Scenario: 从输入启动动作

- **WHEN** 代码控制接受LightAttack request并选择Attack技能
- **THEN** 唯一事务入口 MUST创建ActionInstance和Action Context
- **AND** 普通移动 MUST不被迫创建实例


### Requirement: ActivateActionInstance 必须携带动作事务来源

正式activation请求 MUST 携带精确Skill identity、 ActionProfile identity、source request、InputSequence、source logic tick、target snapshot 和 代码模块／稳定代码位置或技能调用点identity。服务端 tick MAY 出现在模型 decision 中，但 MUST NOT 替代本地来源 tick。

#### Scenario: 调试动作来源

- **WHEN** 动作来自输入、AI 或资源条件
- **THEN** Debug MUST 能定位 正式代码或技能来源、logic tick 和可选 input request


### Requirement: Target activation 不得隐式结束 Source Action

replacement MUST显式指明被替换source，并先完成该source的stop barrier与退出，再经唯一准入建立target。source未关闭时 MUST返回SourceActionStillActive或等价原因，不得自动覆盖Context、吞掉terminal或隐式取消。策略允许的独立并发释放不等同于replacement，MUST以不同ActionInstance／上下文隔离。

#### Scenario: 替换源尚未关闭

- **WHEN** replacement指向的source仍在停止
- **THEN** target MUST不能越过stop barrier，source及Tag所有权必须保持可解释

#### Scenario: Recovery后启动Dodge

- **WHEN** Attack到Dodge的replacement被接受
- **THEN** Attack MUST先按RecoveryCancel结束，Dodge随后建立独立实例

#### Scenario: 独立并发释放

- **WHEN** 两个请求未声明相互替换且准入允许共存
- **THEN** MUST允许独立实例，不得按Actor任意active action隐式覆盖

#### Scenario: Source 尚未关闭

- **WHEN** replacement所指的source仍active或停止未完成
- **THEN** 准入 MUST返回SourceActionStillActive或等价reason
- **AND** source、Context和Tag来源 MUST保持一致，独立并发请求另按准入策略处理

#### Scenario: Recovery 后启动 Dodge

- **WHEN** Attack 到 Dodge replacement 提交
- **THEN** Attack OnExit MUST 先 `Cancel(RecoveryCancel)`
- **AND** Dodge target MUST 随后创建独立 ActionInstance


### Requirement: Action operation runtime 必须保持事务层职责

唯一Action事务服务 MUST只负责 catalog/profile 查询、准入、ActionInstance 创建和 lifecycle 状态流转。技能解释、Timeline、Motion、Cue、命中与世界求解 MUST 由各自正式模块处理。

#### Scenario: 动作校正

- **WHEN** typed correction ingress 到达
- **THEN** Action operation MUST 只更新实例状态与原因
- **AND** world restore 与 visual recovery MUST 留在各自模块


### Requirement: Equipment Route选择不得进入Action runtime

Action事务服务 MUST只处理已选择技能的profile、admission、instance和lifecycle。Slot／Feature／Route及其Skill binding选择 MUST由代码控制模块按锁定catalog完成；技能Tree／Timeline和equipment change继续由正式领域执行模块承担。MUST不恢复旧Equipment Host图、ActionModule或ActionId到Graph callback registry。

#### Scenario: Route选择Attack技能

- **WHEN** 代码控制解析Sawblade PrimaryAction binding
- **THEN** Action事务 MUST只接收精确技能、activation请求和Equipment Context
- **AND** MUST不持有Feature作者对象

#### Scenario: Feature Action取消

- **WHEN** Dodge按准入规则取消Feature Action
- **THEN** lifecycle MUST发布取消结果，技能解释器按该实例范围执行停止

#### Scenario: Route已选择Attack ActionProfile

- **WHEN** 代码控制解析Sawblade PrimaryAction及其精确Skill binding
- **THEN** Action事务 MUST只接收activation请求与Equipment Context
- **AND** MUST不持有Feature作者对象

#### Scenario: Action被取消

- **WHEN** Dodge按正式规则结束Feature Action
- **THEN** lifecycle MUST发布取消结果
- **AND** 技能控制生命周期 MUST按该ActionInstance停止完整执行范围


### Requirement: 系统不得恢复结构身份式 ActionModule

系统 MUST不恢复以节点membership定义释放身份的ActionModule、ActionSubTree、ActionStateNode或旧Ability对象执行接口。允许技能作者role与SkillDefinition拥有执行图，但释放identity必须来自ActionInstance；角色级选择由代码控制，技能图表达局部执行或正式候选。

#### Scenario: 同一状态启动不同动作

- **WHEN** 代码控制的Guard模式可选择Guard或ParryCounter技能
- **THEN** 控制模块 MUST提交精确技能与不同activation请求
- **AND** 控制State MUST不静态等同于唯一ActionProfile或一次技能释放
