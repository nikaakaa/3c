# btsmtl-sm-node-authoring Specification

## ADDED Requirements

### Requirement: Transition Rule 黑板读取必须保持纯条件图语义

系统 MUST 让 TransitionRuleGraph 中的 blackboard/exposed 读取保持纯 ValueNode 语义。读取节点 MUST 只输出值，不拥有 RunnableNode 生命周期、flow 输入、Timeline 播放、Action 提交或状态行为 graph 引用。

#### Scenario: 创建黑板读取节点

- **WHEN** 作者在 TransitionRuleGraph 中创建 blackboard float 读取节点
- **THEN** 该节点 MUST 被 `TransitionRuleGraph.CanCreateNodeType()` 接受
- **AND** 节点 MUST 能通过 PropertyPort 连接到 Compare、And、Or 或 TransitionRuleResultNode
- **AND** 节点 MUST NOT 创建 flow edge

#### Scenario: 拒绝 Runnable ExposedPropertyNode

- **WHEN** 作者或脚本尝试把 Runnable `ExposedPropertyNode` 放入 TransitionRuleGraph
- **THEN** graph creation 或 validation MUST 拒绝该节点
- **AND** 系统 MUST 提示使用纯 ValueNode blackboard 读取节点

### Requirement: Transition Rule 条件必须由输入、黑板值和逻辑节点组合表达

状态机 Transition 的业务条件 MUST 通过输入 ValueNode、blackboard ValueNode、Compare、And、Or、Not 和 TransitionRuleResultNode 等纯节点组合表达。系统 MUST NOT 为每个 Corin locomotion 分支长期保留业务特化条件节点。

#### Scenario: Idle 到 WalkStart

- **WHEN** Idle 到 WalkStart 需要判断移动输入超过走路阈值
- **THEN** 规则图 MUST 读取 MoveAxis 派生幅度和 `WalkThreshold`
- **AND** CompareNode MUST 输出是否超过阈值
- **AND** TransitionRuleResultNode MUST 接收最终 Bool

#### Scenario: WalkLoop 到 RunStart

- **WHEN** WalkLoop 到 RunStart 需要判断输入超过跑步阈值
- **THEN** 规则图 MUST 读取同一套输入派生值和 `RunThreshold`
- **AND** 条件组合 MUST 不依赖专用 `IsRunInput` 业务节点
