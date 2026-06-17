## REMOVED Requirements
### Requirement: FullBody 主调度入口
**Reason**: 该要求把历史 Implementation 描述为目标架构，容易让 FullBody 继续拥有角色级一帧和 Locomotion 输出。

**Migration**: 角色级一帧 owner、phase 顺序和 output authority MUST 归属 `CharacterFramePipeline`、Character-level runtime host、`BodyArbiter` 和 `CharacterFramePlan`。FullBody Action framework 只提交全身动作请求、occupancy claim、动作运动候选和动作动画候选。

#### Scenario: 退役后不再作为目标架构
- **WHEN** 后续实现或 proposal 引用 FullBody Action framework
- **THEN** 它 MUST NOT 将 FullBody 主调度入口视为角色级 frame owner
- **AND** MUST NOT 要求 Locomotion 作为 FullBody 下级 owner 才能参与正式主线

### Requirement: Locomotion 作为 FullBody 子职责
**Reason**: 该要求把 Locomotion 定义为 FullBody 的长期子职责，与角色级 sibling submitter 和 BodyArbiter 目标冲突。

**Migration**: Locomotion MUST 作为 Character frame owner 下的 sibling submitter 提交移动事实、基础移动候选和 Locomotion animation 候选。FullBody Action 只能通过角色级 plan 的 suppression/occupancy 结果压制 Locomotion 输出。

#### Scenario: 退役后 Locomotion 是兄弟提交者
- **WHEN** Locomotion 与 FullBody Action 同帧提交候选
- **THEN** Locomotion MUST 作为 Character-level sibling submitter 参与
- **AND** FullBody Action MUST NOT 直接拥有或驱动 Locomotion runtime

## ADDED Requirements
### Requirement: FullBody 集成提交器降级
当前 `FullBodySubmissionBuilder` 或等价 integrated submitter MUST 被定义为迁移期 Adapter，而不是 FullBody Action framework 的长期正式入口。它 MAY 暂时汇集 Locomotion、FullBody Action、状态机和 motion resolve 数据，但新增身体域 MUST NOT 继续扩展该 Module。

#### Scenario: Integrated submitter 不接新身体域
- **WHEN** 后续新增 UpperBody、HitReact、Aim 或等价身体域
- **THEN** 新身体域 MUST 作为 Character-level sibling submitter 接入
- **AND** MUST NOT 被塞进 `FullBodySubmissionBuilder`
- **AND** MUST NOT 读取 FullBody integrated submitter 的私有状态作为上级权威

#### Scenario: FullBody submitter 只提交动作候选
- **WHEN** FullBody Action submitter 处理 Dodge、Attack 或等价全身动作
- **THEN** 它 MUST 提交 action request、occupancy claim、action motion candidate 和 action animation candidate
- **AND** MUST NOT 直接执行 motion
- **AND** MUST NOT 直接播放 animation
- **AND** MUST NOT 直接消费 Locomotion 输出

### Requirement: FullBody 兼容 view 不反向决定仲裁
FullBody framework MAY 保留 owner、active action、Locomotion phase 或 diagnostic view 作为兼容观测面，但这些 view MUST 从 state snapshot、frame plan 或 runtime facts 派生。兼容 view MUST NOT 反向决定 `BodyArbiter` 或 `CharacterFramePlan` 的最终结果。

#### Scenario: 兼容 view 只读
- **WHEN** 诊断、旧测试或调试 UI 读取 FullBody owner view
- **THEN** view MAY 显示当前 active action、Locomotion phase 或 suppression 状态
- **AND** view MUST NOT 写回 arbitration decision
- **AND** view MUST NOT 成为第二状态权威
