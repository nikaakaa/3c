## MODIFIED Requirements

### Requirement: Agent Intent 必须表达角色动作业务意图

系统 MUST 提供面向 Agent 的 `AgentControllerIntent` schema，用于表达角色动作控制器业务意图。Intent SHOULD 使用 input request、action category、nested state machine、state、ActionProfile、Timeline、cancel、hit reaction 等业务概念。Intent MUST 能表达“外层 Attack category 拥有内层 combo StateMachine”，但 MUST NOT 要求作者或 Agent 直接填写 BTSMTL 内部字段、Unity YAML 路径、节点 GUID 或私有序列化字段。

#### Scenario: 描述二连击

- **WHEN** Agent 需要表达轻攻击二连击
- **THEN** Intent MUST 能描述外层 Attack category、内层 Attack1/Attack2、各自 ActionProfile、各自 Timeline 和 combo 条件
- **AND** Intent MUST NOT 把 Attack1/Attack2 强制平铺到外层 Action StateMachine
- **AND** Intent MUST NOT 直接包含 `m_Nodes`、`m_Edges` 或 Unity serialized property path

### Requirement: Macro 必须将业务意图展开为受限 Patch IR

系统 MUST 提供 Agent Macro 层，将受限业务意图展开为 Patch IR。二连击 Macro MUST 使用普通 `StateMachineNode`、inline `StateMachineGraph`、`StateNode`、Transition edge 和 ConditionRuleGraph 表达外层 Attack category 与内层 combo 状态机。Macro MUST NOT 新增 Attack 专用 opcode、直接修改 BTSMTL asset 或重新生成平铺 Attack1/Attack2。

#### Scenario: 展开二连击

- **WHEN** Macro 接收 `two_hit_combo` intent
- **THEN** Macro MUST 产出外层 Attack State、Attack state body 内的 StateMachineNode、内层 Attack1/Attack2/Exit 和 combo transition 的 Patch IR
- **AND** combo request 查询 MUST 位于内层 ConditionRuleGraph 或等价纯条件位置
- **AND**具体攻击 state MUST 继续产出 Action Context、Timeline 和 lifecycle 节点

## ADDED Requirements

### Requirement: Agent Snapshot 与 Validator 必须递归理解嵌套 StateMachine

Agent Snapshot MUST 递归输出 State body 内 StateMachineNode 的 graph identity、graph path、ownership、states、transitions、Action activation、Timeline 和 lifecycle 摘要。Validator MUST 检查嵌套 graph owner/path、execution path 与 animation transition domain 可解析，并拒绝同一 Attack1/Attack2 同时存在于父子两层。

#### Scenario: 导出 Corin Action Snapshot

- **WHEN** Corin Attack 已迁移为嵌套 StateMachine
- **THEN** compact Snapshot MUST 在外层 Action graph 显示 None、Attack、DodgeBack、DodgeForward
- **AND** Snapshot MUST 在 Attack body 下显示内层 Attack1、Attack2 与 combo transitions
- **AND** Snapshot MUST 继续显示两段 inline Timeline 与四个 Hit/Cancel TreeClip

#### Scenario: 父子层重复攻击状态

- **WHEN** 外层 Action graph 和内层 Attack graph 同时存在 Attack1 或 Attack2
- **THEN** Validator MUST 报告分裂结构
- **AND** apply MUST NOT 选择其中一份作为 fallback

#### Scenario: nested graph owner/path 断裂

- **WHEN** Attack Combo StateMachineNode 的 inline graph、State body 或 Timeline owner/path 无法从 RootTree 解析
- **THEN** Validator MUST 输出机器可读错误路径
- **AND** Compiler MUST 回滚事务
- **AND** 系统 MUST NOT 创建 shared 临时资产绕过断裂路径
