# character-action-instance-runtime Specification

## Purpose
定义 `ActionInstance` 和 `ActionRuntime` 的动作事务层语义：动作身份通过 `ActionActivationRequest`、Action Context 和 `ActionLifecycleTransition` 表达，不通过节点身份、ActionModule、AbilityBody、ActionTree 或静态结构归属表达。
## Requirements
### Requirement: 旧节点 Action 身份链路必须删除
系统 MUST 删除当前节点 Action 身份链路，并且 MUST NOT 保留兼容 alias、桥接字段或并行旧路径。清理范围 MUST 包含 `ActionModule`、`ActionIdentity`、`IActionIdentitySink`、显式 Action 节点，以及 GraphContext/PipelineOutput 中的 active action 写入链路。

#### Scenario: 删除节点模块
- **WHEN** 本变更实现完成
- **THEN** 正式 runtime 中 MUST 不存在 `ActionModule`、`ActionIdentity` 或 `IActionIdentitySink`
- **AND** 普通 BTSMTL 节点 MUST 不再通过节点模块表达 action 身份

#### Scenario: 删除显式 Action 节点
- **WHEN** 本变更实现完成
- **THEN** 正式 runtime 中 MUST 不存在 `ActionSubTreeNode` 或 `ActionStateNode`
- **AND** `SubTreeNode` 和 `StateNode` MUST 保持纯图结构语义

#### Scenario: 删除 pipeline action 输出
- **WHEN** 本变更实现完成
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
系统 MUST 保持 Timeline 和 Motion 的既有职责边界。动作身份清理或后续动作事务层 MUST NOT 直接播放 Timeline、采样 Timeline、修改 Transform 或调用 `CharacterController.Move`。

#### Scenario: 动作触发 Timeline
- **WHEN** 后续动作流程需要播放攻击或闪避 Timeline
- **THEN** 流程 MUST 通过 Graph/BTSMTL 节点提交正式 Timeline 播放请求
- **AND** Timeline 播放和采样权威 MUST 保持在正式 Timeline 调度链路

#### Scenario: 动作影响运动
- **WHEN** 动作需要 root motion、motion warp 或击退
- **THEN** 它 MUST 通过 Timeline、runtime output、motion contribution 或 motion modifier 数据进入 MotionStage
- **AND** 动作事务层 MUST NOT 绕过 MotionStage 直接移动角色

### Requirement: 动作运行时必须使用 ActionInstance 表达一次动作实例
系统 MUST 使用 `ActionInstance` 或等价运行时数据表达一次被接受的动作启动。`ActionInstance` MUST 至少记录 action id、instance id、prediction key、input sequence、start tick、target snapshot、phase 和 state。系统 MUST NOT 使用 Graph、SubTree、StateNode 或 Timeline 资产本身作为网络确认、拒绝或校正的运行时身份。

#### Scenario: Graph 激活动作
- **WHEN** Graph/BTSMTL 通过正式 service 或节点提交 `ActionActivationRequest`
- **THEN** Action runtime MUST 在接受后创建 `ActionInstance`
- **AND** 返回稳定 instance id 和 prediction key

#### Scenario: 服务端确认动作
- **WHEN** NetworkReceiveStage 收到某次动作的 confirmed event
- **THEN** 系统 MUST 通过 instance id、prediction key 或 input sequence 匹配本地 `ActionInstance`
- **AND** MUST NOT 通过同步 Graph 执行路径来确认动作

### Requirement: ActionRuntime 必须是动作事务层而不是执行编排层

`ActionRuntime` MUST 只负责 profile 查询、activation 验证、ActionInstance 创建和 lifecycle transition 状态流转。`ActionRuntime` MUST NOT tick Graph、播放 Timeline、采样 Motion、应用 actor motion correction、播放 Cue 或裁决命中。生命周期变化 MUST 通过 `ActionLifecycleTransition` 或等价事实进入 runtime，而不是由 graph 是否继续 tick 隐式推断。

#### Scenario: 动作激活成功

- **WHEN** `ActionRuntime` 接受 `ActionActivationRequest`
- **THEN** 它 MUST 创建 `ActionInstance` 并返回 Action Context 所需的 instance id、prediction key、input sequence 和 start tick
- **AND** 后续 Timeline 播放、Motion 结算和 GameplayResult 裁决 MUST 由对应 stage 或 Graph 继续处理

#### Scenario: 动作生命周期变化

- **WHEN** Graph、Timeline、NetworkReceiveStage 或系统生命周期提交 `ActionLifecycleTransition`
- **THEN** `ActionRuntime` MUST 按 transition type 更新 `ActionInstance` 的 state、phase 和 reason
- **AND** terminal transition MUST 关闭 active instance，non-terminal transition MUST NOT 默认关闭 active instance

#### Scenario: 新动作覆盖旧动作

- **WHEN** `ActionRuntime` 接受新的 `ActionActivationRequest` 且当前 active action 可被新动作替换
- **THEN** 它 MUST 生成并应用旧动作的 `ActionLifecycleTransition(Cancel, reason = CancelledByNewAction)`
- **AND** activation outcome MUST 携带该 transition，供 Graph 或 Pipeline 转发到 `SyncFacts.Action.LifecycleTransitions`
- **AND** Graph 或 Pipeline MUST NOT 重新构造另一条等价 cancel transition 作为正式事实

#### Scenario: 动作事务校正

- **WHEN** 服务端 ActionInstance Correct decision 到达
- **THEN** `ActionRuntime` MUST 只更新 `ActionInstance` 的 corrected 状态和原因
- **AND** actor 位姿 correction MUST 作为独立 MotionSyncDomain 输入由 CharacterMotionStage 处理
- **AND** 表现修正 MUST 由 Presentation 根据正式运行结果处理

### Requirement: Graph 必须通过运行时 action scope 关联动作输出
系统 MUST 通过运行时 action scope 将 Graph、Timeline、Motion、GameplayResult 和 Presentation 产出的动作输出关联到 `ActionInstance`。系统 MUST NOT 维护静态 node membership table 来记录哪些节点属于某个 action 或 ability。

#### Scenario: 进入 action scope
- **WHEN** Graph 提交 `ActionActivationRequest` 并得到 instance id
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
- **THEN** 它 MUST 通过 `ActionActivationRequest` 生成 `ActionInstance`
- **AND** Graph 本身 MUST NOT 被静态标记为 action/ability 类型

### Requirement: 旧 Ability 执行单元语义必须删除
系统 MUST 删除旧 `AbilityAsset -> BodyGraph` 和 `IAbilityBody` 语义。保留下来的 activation id、prediction key、target snapshot、block/cancel 事务能力 MUST 迁移到 ActionInstance/ActionRuntime 命名。

#### Scenario: 删除 BodyGraph
- **WHEN** 本变更实现完成
- **THEN** 正式 runtime 中 MUST 不存在 `AbilityAsset.BodyGraph`
- **AND** action/profile 数据 MUST NOT 拥有执行图引用

#### Scenario: 删除 Ability body 接口
- **WHEN** 本变更实现完成
- **THEN** 正式 runtime 中 MUST 不存在 `IAbilityBody`
- **AND** Graph/BTSMTL 仍然是唯一玩法执行编排层

### Requirement: ActionRuntime 必须区分 terminal 和 non-terminal transition

系统 MUST 明确区分会结束动作事务的 terminal transition 和只更新状态的 non-terminal transition。`Complete`、`Cancel`、`Interrupt`、`Reject` 和 `Abort` MUST 关闭对应 active action instance；`Confirm` 和 `Correct` 默认 MUST NOT 关闭 active action instance，除非 incoming decision 明确携带终止语义。该规则 MUST 是 ActionRuntime invariant，不得由 profile 配置。

#### Scenario: Confirm 不结束动作

- **WHEN** 服务端确认本地预测攻击成立
- **THEN** `ActionRuntime` MUST 将该实例标记为 confirmed 或等价状态
- **AND** 该动作 MAY 继续输出后续 window、motion、cue 或 result

#### Scenario: Reject 结束动作

- **WHEN** 服务端拒绝本地预测攻击
- **THEN** `ActionRuntime` MUST 将该实例标记为 rejected
- **AND** 后续节点读取该 Action Context MUST 失败

#### Scenario: Interrupt 结束动作

- **WHEN** 受击结果要求打断当前动作
- **THEN** `ActionRuntime` MUST 将当前动作标记为 interrupted 或 cancelled-like terminal state
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
