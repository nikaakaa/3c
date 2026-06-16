## ADDED Requirements
### Requirement: Runtime facts 从模块输出派生
系统 MUST 允许 runtime blackboard facts 从状态节点模块输出和 adapter 回传 facts 派生，而不是从互斥 `Locomotion / Action` owner 分支直接推导。Blackboard MUST 保持纯数据边界，并继续支持预测回滚 snapshot/restore。

#### Scenario: Action facts 从动作模块输出派生
- **WHEN** 当前节点具备 Dodge 动作请求或动作位移模块
- **THEN** action runtime facts MUST 能派生当前 action state、variant、完成事实和 source step
- **AND** MUST NOT 依赖独立 Action runtime 作为第二状态权威

#### Scenario: Locomotion facts 从移动模块和 adapter facts 派生
- **WHEN** 当前节点具备 Locomotion phase 模块
- **THEN** locomotion runtime facts MUST 能派生 phase、gait、move intent 和 motion facts
- **AND** MUST NOT 通过第二 Locomotion 状态机决定 phase

#### Scenario: 回滚快照保持纯数据
- **WHEN** 捕获 rollback snapshot
- **THEN** snapshot MUST 保存 active state、state time、variant、模块必要 payload 和 runtime facts
- **AND** MUST NOT 保存 Unity 对象、Animancer state 或模块实例对象引用
