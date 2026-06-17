## ADDED Requirements
### Requirement: FullBody Action 不拥有 Locomotion
FullBody Action framework 在目标架构中 MUST 只表达全身动作请求、动作状态、占用声明、动作运动候选和动作动画候选。它 MAY 通过 full-body occupancy claim 让角色级仲裁压制 Locomotion 输出，但 MUST NOT 将 Locomotion 定义为 FullBody 的长期子职责或内部下级 owner。

#### Scenario: FullBody 提交 claim 而非直接接管
- **GIVEN** 当前动作状态为 `FullBody/Action/Dodge` 或等价全身动作
- **WHEN** FullBody Action submitter 构建本帧请求
- **THEN** submitter MUST 提交 full-body occupancy claim
- **AND** MAY 提交动作位移和动作动画候选输出
- **AND** MUST NOT 直接调用 Locomotion output runtime 执行压制结果

#### Scenario: Locomotion 压制来自角色级计划
- **GIVEN** Locomotion 已提交基础移动候选输出
- **AND** FullBody Action 已提交 full-body occupancy claim
- **WHEN** BodyArbiter 生成 CharacterFramePlan
- **THEN** plan MAY 标记 Locomotion motion 或 animation output suppressed
- **AND** FullBody Action framework MUST 只消费该结果或提交候选数据
- **AND** MUST NOT 让 FullBody controller 私有字段成为压制权威

#### Scenario: 当前 FullBodySubmissionBuilder 是过渡实现
- **WHEN** 当前实现仍由 `FullBodySubmissionBuilder` 调用 Locomotion frame runtime
- **THEN** 该实现 MUST 被记录为迁移期 integrated submitter
- **AND** 后续拆分时 MUST 将 Locomotion 和 FullBody Action 提交者分离到 Character frame owner 下
- **AND** 不得把该集成形态扩展为 UpperBody、HitReact 或 Aim 的正式接入模型

#### Scenario: FullBody view 是派生解释
- **WHEN** 诊断、兼容测试或旧 adapter 需要读取 FullBody owner、ActionState 或 LocomotionPhase
- **THEN** 这些解释 MAY 从状态机 snapshot、capability metadata 和 frame plan 派生
- **AND** 派生 view MUST NOT 反向决定本帧仲裁结果
- **AND** 派生 view MUST NOT 成为第二状态权威
