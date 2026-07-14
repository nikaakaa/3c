## ADDED Requirements

### Requirement: Action 必须使用统一 Gameplay Effect 作为玩法状态输入

Action activation 和 lifecycle 决策 MUST 从角色统一 Gameplay Effect 读取 tag、attribute 与 effect 事实。`ActionRuntime` MUST 删除私有字符串 tag 集合、`SetTag` 和等价状态副本，并 MUST NOT 承担 effect tick、modifier 聚合或 attribute 存储。

#### Scenario: Graph 判断动作是否可激活

- **WHEN** 动作要求 `State.Grounded`、不存在 `State.CrowdControl.Stun` 且 Stamina 足够
- **THEN** Graph MUST 从统一 Gameplay Effect 读取这些条件后提交 `ActionActivationRequest`
- **AND** `ActionRuntime` MUST 只处理事务 profile、验证结果和实例生命周期

#### Scenario: Action 生命周期结束

- **WHEN** ActionInstance 完成且存在以该 ActionInstanceId 为 source 的临时 effect
- **THEN** 正式协调边界 MAY 按显式 removal policy 移除对应 effect
- **AND** `ActionRuntime` MUST NOT 遍历或直接修改 active effect collection
