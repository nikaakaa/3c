# character-action-instance-runtime Specification

## Purpose
定义 compiled Action operation 与 `CharacterSimulationState` Action slots 的动作事务语义：动作身份通过 `ActivateActionInstance` operation、Action Context 和 lifecycle transition 表达，不通过节点身份、ActionModule、AbilityBody、ActionTree 或静态结构归属表达。
## Requirements
### Requirement: 旧节点 Action 身份链路必须删除
系统 MUST 删除当前节点 Action 身份链路，并且 MUST NOT 保留兼容 alias、桥接字段或并行旧路径。清理范围 MUST 包含 `ActionModule`、`ActionIdentity`、`IActionIdentitySink`、显式 Action 节点，以及 GraphContext/PipelineOutput 中的 active action 写入链路。

#### Scenario: 删除节点模块
- **WHEN** 检查正式runtime  
- **THEN** 正式 runtime 中 MUST 不存在 `ActionModule`、`ActionIdentity` 或 `IActionIdentitySink`
- **AND** 普通 BTSMTL 节点 MUST 不再通过节点模块表达 action 身份

#### Scenario: 删除显式 Action 节点
- **WHEN** 检查正式runtime  
- **THEN** 正式 runtime 中 MUST 不存在 `ActionSubTreeNode` 或 `ActionStateNode`
- **AND** `SubTreeNode` 和 `StateNode` MUST 保持纯图结构语义

#### Scenario: 删除 pipeline action 输出
- **WHEN** 检查正式runtime  
- **THEN** `StrictGameplayOutput` MUST 不再暴露 `ActionId`、`ActionDisplayName`、`ActionPhase`、`ActionTargetKey`、`ActionNetworkIdentity` 或 `ActionTags`
- **AND** active action 信息 MUST 只能通过后续正式 action runtime scope 暴露

### Requirement: Graph 和 Timeline 不得静态拥有动作身份
系统 MUST 保持 Graph、StateNode、SubTreeNode、Timeline clip 和 NodeModule 的结构或表现职责。动作身份 MUST 来自正式运行时动作事务或 action scope，MUST NOT 通过静态节点 membership、Timeline clip membership 或节点模块表达。

#### Scenario: 普通状态行为
- **WHEN** 作者在 `StateNode` 的状态行为图中编排移动、动画或 Timeline
- **THEN** 该图 MUST 保持普通行为图语义
- **AND** 系统 MUST NOT 要求它被标记为 ActionTree 或 AbilityTree

#### Scenario: 可追踪动作流程
- **WHEN** 攻击、闪避或受击流程需要动作身份
- **THEN** 身份 MUST 由正式运行时 action scope 建立
- **AND** Graph、StateNode 或 Timeline asset 本身 MUST NOT 成为网络确认、拒绝或校正身份

### Requirement: 动作边界不得绕过 Timeline 和 Motion 主链路
系统 MUST 保持 Timeline 和 Motion 的既有职责边界。Action operation MUST NOT 直接播放动画资源、修改 Transform、调用 `CharacterController.Move` 或调用具体 WorldSolver。

#### Scenario: 动作触发 Timeline
- **WHEN** 后续动作流程需要播放攻击或闪避 Timeline
- **THEN** BTSMTL Timeline MUST编译为当前 Action body 的正式 Program operation
- **AND** Timeline gameplay 采样 MUST由 SimulationKernel 执行

#### Scenario: 动作影响运动
- **WHEN** 动作需要位移或击退                     
- **THEN** 它 MUST通过 compiled Timeline、motion contribution 或 modifier 进入唯一 CharacterMotionRequest
- **AND** Action operation MUST NOT 绕过 Session WorldSolver 直接移动角色

### Requirement: 动作运行时必须使用 ActionInstance 表达一次动作实例

CharacterSimulationState MUST使用typed ActionInstance state表达一次被接受的动作启动，并至少保存ActionId、ActionInstanceId、PredictionKey、input sequence、start SimulationTick、target snapshot、phase、state、last transition、transition tick、source tick与reason。Action activation request与target snapshot也 MUST使用正式typed state kind。外部确认 MUST通过typed SimulationIngress中的instance/prediction identity匹配，MUST不通过Graph path、Timeline asset或model packet identity确认动作。系统 MUST不保存独立Action lifecycle bytes或Action context镜像；active context MUST由Program级Action index与唯一typed ActionInstance解析。

#### Scenario: Compiled Graph 激活动作

- **WHEN** Program执行ActivateActionInstance operation
- **THEN** MUST在当前State Transaction创建稳定typed ActionInstance

#### Scenario: 外部确认动作

- **WHEN** Model Ingress Pass提交Action confirm ingress
- **THEN** Program MUST通过ActionInstanceId、PredictionKey或input sequence匹配本地typed实例
- **AND** MUST不读取原始network packet

#### Scenario: 动作生命周期变化

- **WHEN** ActionInstance从Predicted进入Confirmed或Terminal状态
- **THEN** phase、state、last transition与reason MUST在同一typed ActionInstance中原子更新
- **AND** MUST不写入第二份lifecycle state

### Requirement: Timeline 必须只保留类型化 ActionInstance 引用

需要跨Tick验证Action Context的Timeline MUST在自己的typed retention state中保存最小`ActionInstanceReference`，至少包含ActionId、ContextId、ActionInstanceId与PredictionKey。Timeline MUST通过Action state port解析当前typed ActionInstance并校验引用，不得复制完整ActionInstance、保存opaque bytes或持有Action runtime具体实现。

#### Scenario: Attack Timeline 跨 Tick 继续运行

- **WHEN** Attack1 Timeline启动并保留当前Action Context
- **THEN** Timeline state MUST保存typed ActionInstanceReference
- **AND** 后续Tick MUST通过该引用校验同一Action instance仍然active

#### Scenario: Action Context 已结束

- **WHEN** retained reference对应的ActionInstance已经terminal或被替换
- **THEN** Timeline MUST按正式ActionContextEnded stop生命周期退出
- **AND** MUST不从历史bytes副本恢复旧Action状态

### Requirement: Action operation runtime 必须是动作事务层而不是执行编排层

Compiled Action operations MUST只负责 profile 查询、activation 验证、ActionInstance 创建和 lifecycle transition。它们 MUST不调用 Graph runtime、播放 Timeline、调用 WorldSolver、应用 model correction、播放 Cue 或裁决命中。Timeline、Motion 与 GameplayResult 通过 Program operation、world batch 和 typed facts继续处理。

#### Scenario: 动作激活成功

- **WHEN** ActivateActionInstance operation 接受动作激活输入
- **THEN** MUST创建 ActionInstance 并输出正式 Action Context

#### Scenario: 生命周期 ingress

- **WHEN** Graph、Timeline、SimulationIngress 或系统生命周期提交 ActionLifecycleTransition
- **THEN** Action operation MUST按 transition type 更新实例 state、phase 和 reason

#### Scenario: 动作事务校正

- **WHEN** Model Ingress Pass提交非终止 Correct ingress
- **THEN** MUST只更新 ActionInstance corrected state
- **AND** world restore或 visual recovery MUST分别由 Pipeline Runtime与 Committer处理

### Requirement: Graph 必须通过运行时 action scope 关联动作输出
系统 MUST 通过运行时 action scope 将 Graph、Timeline、Motion、GameplayResult 和 Presentation 产出的动作输出关联到 `ActionInstance`。系统 MUST NOT 维护静态 node membership table 来记录哪些节点属于某个 action 或 ability。

#### Scenario: 进入 action scope
- **WHEN** Graph 的 ActivateActionInstanceNode 编译并执行后得到 instance id
- **THEN** 后续由该流程提交的 Timeline request、window sample、motion sample、cue event 或 gameplay result MAY 关联该 instance id
- **AND** 关联 MUST 来自运行时上下文或显式参数，而不是静态节点归属表

#### Scenario: 离开 action scope
- **WHEN** Graph 提交 terminal `ActionLifecycleTransition` 或 action instance 被取消
- **THEN** 该 action scope MUST 关闭
- **AND** 后续普通 locomotion、gameplay result 或表现输出 MUST NOT 自动继承旧 instance id

### Requirement: Graph 和 Tree 不得被标记为网络动作类型
系统 MUST 保持 Graph、SubTree、StateNode 和 StateMachineNode 的结构语义。系统 MUST NOT 新增 `NetworkedTree`、`ActionTree`、`AbilityTree`、`NetworkedStateNode`、`AbilityBodyGraph` 或等价特殊图/节点类型作为第一阶段正式主线。

#### Scenario: 普通 locomotion graph
- **WHEN** locomotion Graph 只提交移动和表现输出
- **THEN** 它 MUST 保持普通 Graph/State 行为语义
- **AND** 不需要 action profile 或 action instance

#### Scenario: 攻击流程 graph
- **WHEN** 攻击流程需要网络追踪
- **THEN** 它 MUST通过 ActivateActionInstance operation 生成 `ActionInstance`
- **AND** Graph 本身 MUST NOT 被静态标记为 action/ability 类型

### Requirement: 旧 Ability 执行单元语义必须删除
系统 MUST 删除旧 `AbilityAsset -> BodyGraph` 和 `IAbilityBody` 语义。保留下来的 activation id、prediction key、target snapshot、block/cancel 事务能力 MUST迁移到 ActionInstance、Action operation 与 CharacterSimulationState Action slots。

#### Scenario: 删除 BodyGraph
- **WHEN** 检查正式runtime  
- **THEN** 正式 runtime 中 MUST 不存在 `AbilityAsset.BodyGraph`
- **AND** action/profile 数据 MUST NOT 拥有执行图引用

#### Scenario: 删除 Ability body 接口
- **WHEN** 检查正式runtime  
- **THEN** 正式 runtime 中 MUST 不存在 `IAbilityBody`
- **AND** BTSMTL authoring 编译出的 Program operation set MUST是唯一玩法执行语义

### Requirement: Action operation runtime 必须区分 terminal 和 non-terminal transition

系统 MUST明确区分会结束动作事务的 terminal transition 和只更新状态的 non-terminal transition。`Complete`、`Cancel`、`Interrupt`、`Reject` 和 `Abort` MUST关闭对应 active action instance；`Confirm` 和 `Correct` 默认 MUST NOT关闭 active action instance，除非 incoming ingress 明确携带终止语义。该规则 MUST是 Action operation invariant，不得由 profile 配置。

#### Scenario: Confirm 不结束动作

- **WHEN** 服务端确认本地预测攻击成立
- **THEN** Action operation MUST将该实例标记为 confirmed 或等价状态
- **AND** 该动作 MAY 继续输出后续 window、motion、cue 或 result

#### Scenario: Reject 结束动作

- **WHEN** 服务端拒绝本地预测攻击
- **THEN** Action operation MUST将该实例标记为 rejected
- **AND** 后续节点读取该 Action Context MUST 失败

#### Scenario: Interrupt 结束动作

- **WHEN** 受击结果要求打断当前动作
- **THEN** Action operation MUST将当前动作标记为 interrupted 或 cancelled-like terminal state
- **AND** 后续受击表现、击退或硬直 MUST 通过新的状态/动作输出表达

### Requirement: ActionInstance 必须记录生命周期来源和原因

系统 MUST 让 `ActionInstance` 或等价 debug record 能记录最近一次 lifecycle transition 的 type、reason、tick 和 source identity。Debug MUST 能解释某次动作为什么确认、完成、取消、打断、拒绝、修正或中止。

#### Scenario: 查看闪避取消

- **WHEN** 作者或面试官查看某次攻击被闪避取消的调试信息
- **THEN** Debug MUST 显示该 ActionInstance 的 transition type 为 `Cancel`
- **AND** MUST 显示 reason、tick 和触发该 transition 的 graph/node/source

#### Scenario: 查看服务端修正

- **WHEN** 服务端对某次动作发送 correction
- **THEN** Debug MUST 显示 `Correct` transition、服务端 tick 或 correction id
- **AND** MUST 能关联后续 Motion 或 Presentation correction 输出

### Requirement: Equipment Feature不得恢复旧Ability执行单元

Equipment Feature MAY拥有被Compiler静态链接的普通inline graph和导出ActionProfile，但正式Runtime MUST不出现`ActionModule`、`AbilityAsset`、`IAbilityBody`、`AbilityTree`、Feature graph clone或按Feature调用Graph的Action接口。Feature owner metadata MUST只用于编译、source map、route entry和diagnostics，不得成为Action身份或第二membership table。

#### Scenario: Feature Action进入Runtime

- **WHEN** Sawblade Route激活Attack
- **THEN** 动作身份 MUST仍为Attack ActionProfile与新ActionInstanceId
- **AND** FeatureId MUST只作为Equipment Context/source metadata

#### Scenario: 查找Action body

- **WHEN** runtime需要执行已选择Route body
- **THEN** compiled Equipment Host MUST使用Program entry index
- **AND** ActionInstance runtime MUST不加载AbilityBody

### Requirement: ActionInstance必须可选保存Equipment Context

ActionInstance state MUST新增可选Equipment Context，包含SlotId、EquipmentId、FeatureId、EquipmentRevision与RouteId，并进入copy、transaction、codec、snapshot、hash、fact与diagnostics。只有Feature Route创建的Action MUST携带该context；Core Action MUST为None。Context MUST在实例生命周期内不可变。

#### Scenario: Feature Action完成

- **WHEN** Sawblade Attack ActionInstance完成
- **THEN** lifecycle fact MUST携带同一Equipment Context
- **AND** context MUST不因当前Loadout变化被改写

#### Scenario: 恢复未知Feature context

- **WHEN** snapshot中的FeatureId不在当前Program catalog
- **THEN** restore MUST失败
- **AND** MUST不将context降级为None

