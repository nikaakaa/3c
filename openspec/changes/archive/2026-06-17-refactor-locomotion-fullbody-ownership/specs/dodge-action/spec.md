## REMOVED Requirements

### Requirement: FullBody 行为域和层级状态机边界

**Reason**: Dodge 不应被定义为 FullBody root 状态树中的叶子或 Locomotion 子域 sibling。

**Migration**: Dodge 归属 Action domain；full-body 语义通过 body/channel claim 表达。

## ADDED Requirements

### Requirement: Dodge 属于 Action domain

系统 MUST 将 Shift Dodge 视为 Action domain 中的全身动作。Dodge lifecycle MAY 使用 action instance、timeline 或局部 FSM/HFSM；对外 MUST 输出 `Action.Dodge`、action facts、body/channel claim、motion candidate 和 animation candidate。基础 Locomotion MUST 作为独立领域提交移动 facts 和候选输出。二者最终输出 MUST 由 Character frame plan 仲裁。

#### Scenario: Dodge accepted 进入 Action domain
- **WHEN** `Action.Dodge` 仲裁被接受
- **THEN** Dodge MUST 作为 Action domain 的 action state、action instance 或 resolved action id 运行
- **AND** 它 MUST 提交 full-body 或等价 body/channel claim
- **AND** 它 MUST NOT 要求 Locomotion 处于 FullBody 子树才能执行

#### Scenario: Locomotion 是独立基础移动领域
- **WHEN** 没有 active Dodge 或其它 full-body claim
- **THEN** Locomotion module MUST 继续决定 `Locomotion.Idle`、`Locomotion.MoveStart`、`Locomotion.MoveLoop`、`Locomotion.MoveStop` 或 `Locomotion.TurnBack`
- **AND** Locomotion module MUST 继续提交基础移动 motion 和 animation candidate
- **AND** Locomotion module MUST NOT 被表达为 `FullBodyOwnerKind.Locomotion`

#### Scenario: 模块化不等于分裂路径
- **WHEN** 系统为 Dodge 提供独立类、配置资产、测试夹具、action instance 或内部 lifecycle
- **THEN** 这些实现单元 MUST 被视为 Action domain submitter 的内部职责
- **AND** 它们 MUST 通过统一输入、仲裁、body claim、motion candidate 和 animation candidate 协作
- **AND** 它们 MUST NOT 形成独立角色控制器、独立 Transform 写入路径或独立 frame pipeline

#### Scenario: 当前不实现并行表现层
- **WHEN** 实现 Shift Dodge 的归属迁移
- **THEN** 系统 MUST NOT 创建 UpperBody、Facial、IK、Additive 或等价并行表现状态层
- **AND** MUST NOT 使用并行表现层决定 `Action.Dodge` 是否进入、结束或转入 Run latch
- **AND** 后续如需这些层 MUST 另开 OpenSpec
