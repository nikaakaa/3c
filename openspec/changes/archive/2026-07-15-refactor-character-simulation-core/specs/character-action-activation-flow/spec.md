# character-action-activation-flow Specification

## MODIFIED Requirements

### Requirement: 动作生命周期变化必须通过 ActionLifecycleTransition 表达

系统 MUST使用 ActionLifecycleTransition 或等价 typed lifecycle fact 表达动作事务的确认、完成、取消、打断、拒绝、修正和中止。Compiled Graph/Timeline MAY在 Evaluate 中提交本地 lifecycle；外部模型 decision MUST先由 model Driver 转换为 SimulationIngress，再由 Program operation 应用。系统 MUST不根据某节点本 Tick 未执行而隐式关闭 ActionInstance。

#### Scenario: Timeline 正常完成

- **WHEN** 带 Action Context 的攻击 Timeline 达到结束点
- **THEN** compiled lifecycle operation MUST提交 Complete 并关闭对应 active context

#### Scenario: 闪避取消攻击

- **WHEN** Graph 决定从可取消攻击切换到闪避
- **THEN** MUST对旧攻击提交 Cancel 并为闪避创建独立 ActionInstance

#### Scenario: 外部拒绝预测动作

- **WHEN** model Driver 在 Tick plan 中提交合法 Reject ingress
- **THEN** Program MUST按 ActionInstance identity 应用 Reject
- **AND** MUST不读取 model packet 或 history metadata

#### Scenario: 系统中止

- **WHEN** Actor 从 Session roster 移除时仍有 active action
- **THEN** SessionRuntime MUST在移除前提交或记录 Abort 并清理该 Action state
