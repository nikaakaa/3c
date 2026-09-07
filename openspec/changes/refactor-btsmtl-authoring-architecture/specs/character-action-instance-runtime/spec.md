## MODIFIED Requirements

### Requirement: Graph 和 Timeline 不得静态拥有动作身份

技能定义 MUST能拥有执行图并引用ActionProfile，但Graph、State、SubTree和Timeline的作者身份 MUST与一次释放的ActionInstanceId分离。模板或节点membership不得作为网络确认、拒绝、校正及运行变量归属。技能局部图只能通过显式运行上下文关联本次释放。

#### Scenario: 复用技能模板

- **WHEN** 两个释放实例引用同一技能执行图
- **THEN** 两者 MUST具有不同ActionInstanceId和执行状态
- **AND** 模板identity MUST不能作为任一释放的网络身份

#### Scenario: 技能局部状态

- **WHEN** 作者创建技能内部StateMachine和Timeline
- **THEN** 它们 MUST表达当前技能内部流程，不独立创建动作身份

#### Scenario: 普通状态行为

- **WHEN** 作者在技能局部State行为图编排运动或Timeline
- **THEN** 图 MUST保持局部结构语义并使用当前技能Context
- **AND** MUST不以State或Graph模板identity替代ActionInstance

#### Scenario: 可追踪动作流程
- **WHEN** 攻击、闪避或受击流程需要动作身份
- **THEN** 身份 MUST 由正式运行时 action scope 建立
- **AND** Graph、StateNode 或 Timeline asset 本身 MUST NOT 成为网络确认、拒绝或校正身份


### Requirement: 动作运行时必须使用 ActionInstance 表达一次动作实例

CharacterSimulationState MUST使用typed ActionInstance state表达一次被接受的动作启动，并至少保存ActionId、ActionInstanceId、PredictionKey、input sequence、start SimulationTick、target snapshot、精确SkillProgram引用、phase、state、last transition、transition tick、source tick与reason。Action activation request与target snapshot也 MUST使用正式typed state kind。外部确认 MUST通过typed SimulationIngress中的instance/prediction identity匹配，MUST不通过Graph path、Timeline asset或model packet identity确认动作。系统 MUST不保存独立Action lifecycle bytes或Action context镜像；active context MUST由Program级技能目录、实例索引与唯一typed ActionInstance解析。

#### Scenario: 角色代码激活动作

- **WHEN** 代码控制经唯一准入服务接受技能启动
- **THEN** MUST在当前State Transaction创建稳定typed ActionInstance

#### Scenario: 外部确认动作

- **WHEN** Model Ingress Pass提交Action confirm ingress
- **THEN** Program MUST通过ActionInstanceId、PredictionKey或input sequence匹配本地typed实例
- **AND** MUST不读取原始network packet

#### Scenario: 动作生命周期变化

- **WHEN** ActionInstance从Predicted进入Confirmed或Terminal状态
- **THEN** phase、state、last transition与reason MUST在同一typed ActionInstance中原子更新
- **AND** MUST不写入第二份lifecycle state
#### Scenario: 技能执行状态归属

- **WHEN** 释放内部产生节点、Timeline或子图跨Tick状态
- **THEN** 这些状态 MUST归属同一ActionInstance并参与完整事务与恢复
- **AND** MUST不新增独立Skill生命周期或context镜像

#### Scenario: Compiled Graph 激活动作

- **WHEN** 旧角色图仍试图使用activation节点直接启动角色动作
- **THEN** 作者能力与构建 MUST拒绝旧入口
- **AND** 转换后的控制代码 MUST通过唯一事务服务建立typed ActionInstance


### Requirement: Action operation runtime 必须是动作事务层而不是执行编排层

唯一Action事务服务及技能内合法lifecycle操作 MUST只负责 profile 查询、activation 验证、ActionInstance 创建和 lifecycle transition。它们 MUST不调用 Graph runtime、播放 Timeline、调用 WorldSolver、应用 model correction、播放 Cue 或裁决命中。Timeline、Motion 与 GameplayResult 通过 Program operation、world batch 和 typed facts继续处理。

#### Scenario: 动作激活成功

- **WHEN** 角色控制通过唯一Action事务入口接受动作激活输入
- **THEN** MUST创建 ActionInstance 并输出正式 Action Context

#### Scenario: 生命周期 ingress

- **WHEN** 角色控制、技能、SimulationIngress 或系统生命周期提交 ActionLifecycleTransition
- **THEN** Action operation MUST按 transition type 更新实例 state、phase 和 reason

#### Scenario: 动作事务校正

- **WHEN** Model Ingress Pass提交非终止 Correct ingress
- **THEN** MUST只更新 ActionInstance corrected state
- **AND** world restore或 visual recovery MUST分别由 Pipeline Runtime与 Committer处理


### Requirement: Graph 必须通过运行时 action scope 关联动作输出

技能Graph、Timeline、Motion、GameplayResult和Presentation输出 MUST显式关联由角色控制准入建立的ActionInstance。SkillDefinition与模板来源只用于内容、依赖和诊断，MUST不维护静态节点membership来替代释放身份。terminal后旧scope必须关闭，后续普通移动或新技能不得继承。

#### Scenario: 进入技能scope

- **WHEN** C#控制接受技能启动并建立ActionInstance
- **THEN** 解释器 MUST收到该实例的显式Action Context
- **AND** 技能输出 MUST使用本次上下文

#### Scenario: 离开技能scope

- **WHEN** 实例被取消、拒绝或完成
- **THEN** 旧scope MUST关闭；清理之外不得继续产生普通技能输出

#### Scenario: 进入 action scope

- **WHEN** 角色控制准入建立ActionInstance并将Context交给技能
- **THEN** 后续Timeline、窗口、Motion和输出 MUST使用该显式实例
- **AND** MUST不从静态节点membership或ambient active action猜归属

#### Scenario: 离开 action scope
- **WHEN** Graph 提交 terminal `ActionLifecycleTransition` 或 action instance 被取消
- **THEN** 该 action scope MUST 关闭
- **AND** 后续普通 locomotion、gameplay result 或表现输出 MUST NOT 自动继承旧 instance id


### Requirement: Graph 和 Tree 不得被标记为网络动作类型

系统 MUST允许SkillGraph及其Tree／Timeline／局部状态机作为技能内容作者role，但不得以NetworkedTree、NetworkedStateNode或模板membership指定网络模型或释放身份。普通移动由角色代码控制；技能是否预测、确认或恢复仍由正式模型和ActionInstance处理。

#### Scenario: 编辑攻击技能图

- **WHEN** 作者创建攻击技能的入口Tree
- **THEN** 图 MUST具有技能作者role并引用明确策略
- **AND** MUST不声明网络authority或用图GUID确认动作

#### Scenario: 普通移动

- **WHEN** 角色代码推进locomotion
- **THEN** MUST无需技能图或ActionInstance

#### Scenario: 普通 locomotion graph

- **WHEN** 迁移原来只提交普通移动的角色locomotion graph
- **THEN** 流程 MUST进入C#控制并删除原角色图入口
- **AND** 普通移动 MUST仍不要求ActionProfile或ActionInstance

#### Scenario: 攻击流程 graph

- **WHEN** 技能攻击需要网络追踪
- **THEN** MUST由代码准入建立ActionInstance，再将Context传入技能图
- **AND** 图的技能作者role MUST不能代替网络或释放身份


### Requirement: 旧 Ability 执行单元语义必须删除

旧AbilityAsset.BodyGraph、IAbilityBody、ActionModule和作者对象执行接口 MUST保持删除。允许新的纯数据SkillDefinition引用作者图并编译为SkillProgram；Runtime MUST只通过角色管线中的解释器执行该数据，释放身份、准入、目标及生命周期继续属于唯一ActionInstance。

#### Scenario: 检查技能运行入口

- **WHEN** 系统运行技能定义引用的内容
- **THEN** MUST使用已编译SkillProgram与实例状态
- **AND** MUST不存在IAbilityBody、作者Graph clone或另一套Ability manager

#### Scenario: 检查策略资产

- **WHEN** SkillDefinition引用ActionProfile
- **THEN** 策略 MUST仍由ActionProfile唯一拥有，执行图不得成为第二份准入或生命周期

#### Scenario: 删除 BodyGraph

- **WHEN** 检查正式技能作者与Runtime
- **THEN** 旧AbilityAsset.BodyGraph MUST不存在
- **AND** SkillDefinition可以持有纯作者图引用，ActionProfile仍只拥有策略
- **AND** Runtime MUST只执行编译后的SkillProgram

#### Scenario: 删除 Ability body 接口

- **WHEN** 检查正式Runtime
- **THEN** IAbilityBody MUST不存在
- **AND** 角色控制与技能解释 MUST共同位于唯一Kernel事务；不得有作者对象执行体


### Requirement: Equipment Feature不得恢复旧Ability执行单元

Equipment Feature MUST通过声明的控制binding和SkillDefinition引用参与角色组合，不再拥有运行中的Persistent／Route Graph。FeatureId只作为Equipment Context及来源，MUST不替代ActionInstance身份。旧AbilityAsset／IAbilityBody、Feature graph clone和按资产调用执行体接口 MUST保持删除。

#### Scenario: Feature技能进入Runtime

- **WHEN** 装备Route经代码控制选择Attack技能
- **THEN** 动作 MUST使用新ActionInstanceId并捕获原Equipment Context

#### Scenario: 查找Feature技能

- **WHEN** 运行时解析已选择Route
- **THEN** MUST通过锁定catalog与typed Skill handle启动，不加载作者执行体

#### Scenario: Feature Action进入Runtime

- **WHEN** Sawblade Route激活Attack
- **THEN** 动作身份 MUST仍为Attack ActionProfile与新ActionInstanceId
- **AND** FeatureId MUST只作为Equipment Context/source metadata

#### Scenario: 查找Action body

- **WHEN** 运行时执行已选择装备Route绑定的技能
- **THEN** 代码控制 MUST使用锁定Skill entry并提交正式activation请求
- **AND** ActionInstance事务服务 MUST不加载AbilityBody
