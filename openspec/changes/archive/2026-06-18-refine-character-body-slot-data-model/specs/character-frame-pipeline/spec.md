## MODIFIED Requirements

### Requirement: CharacterFramePlan 先于新身体层
系统 MUST 在新增正式 UpperBody、HitReact、Aim 或等价身体层 runtime 前，先提供角色级 `CharacterFramePlan` 或等价一帧计划契约。该计划 MUST 能表达 `BaseSlot`、`UpperBodySlot` 或经批准的等价 slot owner，并且 MUST 区分 source、action、claim、slot、channel 与 presentation layer。新身体层 MUST 通过该计划参与 output composer/applier，不能直接绕过角色级管线。

`CharacterFramePlan` 的正式身体结果契约 MUST 使用 slot 口径。旧 layer 口径属性如需保留，只能作为迁移兼容读取，MUST 转发到 slot result。

#### Scenario: 新 UpperBody 需要计划契约
- **WHEN** 要实现 UpperBody Aim 或 UpperBody HitReact
- **THEN** 设计必须先定义它如何向 `CharacterFramePlan` 提交候选
- **AND** 定义它如何与 Locomotion / Action 的 body claim 合成或冲突

#### Scenario: Plan 是纯数据
- **WHEN** `CharacterFramePipeline` 生成一帧计划
- **THEN** 计划只能包含候选、claim、slot owner、权重、优先级、窗口、事件与输出意图等纯数据
- **AND** 不能直接执行动画播放、移动、IK 或黑板写入

#### Scenario: Output applier 仍唯一执行副作用
- **WHEN** 计划被提交到输出层
- **THEN** 只有既有 motion executor、animation presenter、blackboard writer 或经批准的 presenter/applier 可以执行副作用
- **AND** 不得新增第二 motion executor、第二 animation presenter 或第二 blackboard writer

#### Scenario: Plan 表达 slot 而不是表现层
- **WHEN** FullBody claim 赢得本帧仲裁
- **THEN** `CharacterFramePlan` MUST 表达 `BaseSlot` 由 Action-side owner 接管，并表达 `UpperBodySlot` 是否被压制
- **AND** 计划 MUST NOT 把 Animancer layer、timeline track、GraphView node 或 editor view 当作 gameplay slot
