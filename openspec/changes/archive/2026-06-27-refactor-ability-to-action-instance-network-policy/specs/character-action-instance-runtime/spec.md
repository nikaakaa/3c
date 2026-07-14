# character-action-instance-runtime Specification

## ADDED Requirements

### Requirement: 动作运行时必须使用 ActionInstance 表达一次动作实例
系统 MUST 使用 `ActionInstance` 或等价运行时数据表达一次被接受的动作启动。`ActionInstance` MUST 至少记录 action id、instance id、prediction key、input sequence、start tick、target snapshot、phase 和 state。系统 MUST NOT 使用 Graph、SubTree、StateNode 或 Timeline 资产本身作为网络确认、拒绝或校正的运行时身份。

#### Scenario: Graph 启动可追踪动作
- **WHEN** Graph/BTSMTL 通过正式 service 或节点提交 `BeginTrackedAction`
- **THEN** Action runtime MUST 在接受后创建 `ActionInstance`
- **AND** 返回稳定 instance id 和 prediction key

#### Scenario: 服务端确认动作
- **WHEN** NetworkReceiveStage 收到某次动作的 confirmed event
- **THEN** 系统 MUST 通过 instance id、prediction key 或 input sequence 匹配本地 `ActionInstance`
- **AND** MUST NOT 通过同步 Graph 执行路径来确认动作

### Requirement: ActionRuntime 必须是动作事务层而不是执行编排层
系统 MUST 让 `ActionRuntime` 只负责 begin、confirm、reject、cancel、end 等动作实例生命周期。`ActionRuntime` MUST NOT tick Graph、播放 Timeline、采样 Timeline、提交 Motion、播放 Cue 或裁决命中。

#### Scenario: 动作启动成功
- **WHEN** `ActionRuntime` 接受一个 start request
- **THEN** 它 MUST 只创建和记录 `ActionInstance`
- **AND** 后续执行流程 MUST 仍由 Graph/BTSMTL、TimelineStage、MotionStage 和 PresentationStage 完成

#### Scenario: 动作取消
- **WHEN** 当前 action instance 被取消
- **THEN** `ActionRuntime` MUST 更新实例 state
- **AND** Graph 或 Pipeline 后续 stage MUST 决定如何停止 Timeline、修正表现或输出 correction

### Requirement: Graph 必须通过运行时 action scope 关联事实
系统 MUST 通过运行时 action scope 将 Graph、Timeline、Motion、Combat 和 Presentation 产出的事实关联到 `ActionInstance`。系统 MUST NOT 维护静态 node membership table 来记录哪些节点属于某个 action 或 ability。

#### Scenario: 进入 action scope
- **WHEN** Graph 执行 `BeginTrackedAction` 并得到 instance id
- **THEN** 后续由该流程提交的 Timeline request、window fact、motion fact 或 cue MAY 关联该 instance id
- **AND** 关联 MUST 来自运行时上下文或显式参数，而不是静态节点归属表

#### Scenario: 离开 action scope
- **WHEN** Graph 执行 `EndTrackedAction` 或 action instance 被取消
- **THEN** 该 action scope MUST 关闭
- **AND** 后续普通 locomotion 或表现事实 MUST NOT 自动继承旧 instance id

### Requirement: Graph 和 Tree 不得被标记为网络动作类型
系统 MUST 保持 Graph、SubTree、StateNode 和 StateMachineNode 的结构语义。系统 MUST NOT 新增 `NetworkedTree`、`ActionTree`、`AbilityTree`、`NetworkedStateNode`、`AbilityBodyGraph` 或等价特殊图/节点类型作为第一阶段正式主线。

#### Scenario: 普通 locomotion graph
- **WHEN** locomotion Graph 只提交移动和表现事实
- **THEN** 它 MUST 保持普通 Graph/State 行为语义
- **AND** 不需要 action profile 或 action instance

#### Scenario: 攻击流程 graph
- **WHEN** 攻击流程需要网络追踪
- **THEN** 它 MUST 通过 `BeginTrackedAction` 生成 `ActionInstance`
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
