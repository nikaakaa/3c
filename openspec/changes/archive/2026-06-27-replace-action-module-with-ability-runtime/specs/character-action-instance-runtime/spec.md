# character-action-instance-runtime Specification

## ADDED Requirements

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
- **WHEN** 动作需要 root motion、motion warp、击退或校正
- **THEN** 它 MUST 通过 Timeline、runtime fact、motion contribution 或 motion modifier 数据进入 MotionStage
- **AND** 动作事务层 MUST NOT 绕过 MotionStage 直接移动角色
