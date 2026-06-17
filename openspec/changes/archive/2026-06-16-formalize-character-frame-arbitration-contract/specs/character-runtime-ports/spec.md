## ADDED Requirements
### Requirement: Runtime ports 表达兄弟提交者而非 FullBody 归属
角色 runtime port 契约 MUST 支持 Locomotion、FullBody Action、UpperBody 或等价行为域作为 Character frame owner 下的 sibling submitters。port 命名、职责和 adapter 边界 MUST NOT 将目标架构表达为 FullBody 拥有 Locomotion。

#### Scenario: Locomotion port 提交基础移动事实
- **WHEN** Character frame owner 需要基础移动输入
- **THEN** Locomotion runtime port MUST 能提交移动意图、移动事实、基础移动候选输出或等价 frame data
- **AND** 该 port MUST NOT 要求调用方是 FullBody controller 才能作为正式目标架构成立

#### Scenario: FullBody Action port 提交占用声明
- **WHEN** FullBody Action runtime 进入 Dodge、Attack 或等价全身动作
- **THEN** FullBody Action submitter MUST 能提交 full-body occupancy claim
- **AND** MAY 提交 action motion、action animation 或 input consume 候选输出
- **AND** MUST NOT 直接修改 Locomotion runtime 内部状态来表达压制结果

#### Scenario: Character host 是最终目标 owner
- **WHEN** 后续迁移角色 runtime host
- **THEN** Character-level host MUST 成为正式一帧 owner
- **AND** `PlayerFullBodyActionController` 或等价 FullBody host adapter MAY 作为迁移期入口存在
- **AND** 该 adapter MUST NOT 被新增身体层视为长期上级 owner

#### Scenario: Port 不泄漏 Unity 执行对象
- **WHEN** sibling submitter 通过 runtime port 提交 request 或 facts
- **THEN** port result MUST 是纯数据或受控接口结果
- **AND** MUST NOT 泄漏 `Transform`、`CharacterController`、Animancer state、Animator state 或 InputAction 作为仲裁输入权威
