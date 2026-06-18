# character-behavior-submission-runtime-chain Specification

## Purpose
定义 CharacterBehaviorSubmissionRunner、LocomotionSource、CommittedActionSource、Composer、BodyArbiter 和 OutputApplier 的运行时提交闭环，确保行为源只提交纯数据候选并经角色帧计划统一执行。
## Requirements
### Requirement: Character Behavior Submission Runner 是正式提交组合入口
系统 MUST 在 `CharacterRuntimeCore -> CharacterFramePipeline` 主线内使用 `CharacterBehaviorSubmissionRunner` 或批准的等价组合模块作为 request/output submission 入口。该 runner MUST 只提交纯数据，不得成为第二个角色帧管线、Unity tick owner、motion executor、animation presenter 或 blackboard writer。

#### Scenario: Pipeline 使用 Runner 提交请求和输出
- **GIVEN** 正式角色 runtime 已装配 behavior runtime definition
- **WHEN** `CharacterFramePipeline` 执行 GameplayDecision 和 BuildMotion phase
- **THEN** request submission MUST 通过 behavior submission runner
- **AND** output submission MUST 通过 behavior submission runner
- **AND** pipeline phase 顺序 MUST 保持不变

#### Scenario: Runner 不执行副作用
- **WHEN** behavior submission runner 执行 request pass 或 output pass
- **THEN** runner MUST NOT 调用 motion executor
- **AND** MUST NOT 调用 animation presenter
- **AND** MUST NOT 消费 input buffer
- **AND** MUST NOT 写 runtime blackboard

### Requirement: LocomotionSource 提交 typed locomotion submissions
Locomotion leaf MUST 作为 LocomotionSource 提交可审计 typed submissions。提交内容 MUST 表达 locomotion decision、state frame、locomotion frame、motion candidate、animation candidate 和必要 facts，但 MUST NOT 直接决定 CommittedAction branch 或执行输出副作用。

#### Scenario: LocomotionSource 输出基础候选
- **WHEN** OutputPass 运行 LocomotionSource
- **THEN** LocomotionSource MUST 提交 locomotion state frame
- **AND** MUST 提交基础 motion candidate 或明确的无 motion candidate
- **AND** MUST 提交基础 animation candidate 或明确的无 animation candidate
- **AND** MUST 保留 source id、source step 和 pass 信息

#### Scenario: LocomotionSource 不拥有 Action
- **WHEN** 本帧存在 Dodge request
- **THEN** LocomotionSource MAY 提供 movement facts 或 locomotion context
- **AND** MUST NOT 直接选择 `Action.Dodge`
- **AND** MUST NOT 直接创建 Dodge timeline outcome

### Requirement: CommittedActionSource 提交 typed action submissions
CommittedAction leaf MUST 作为 CommittedActionSource 提交可审计 typed submissions。提交内容 MUST 表达 action request、interrupt decision、resolved action、action lifecycle、branch outcome、claim、motion、animation、window 和 cue，但 MUST NOT 绕过角色级 frame plan 执行运动或动画。

#### Scenario: CommittedActionSource 输出 Dodge Action
- **GIVEN** Dodge request 被 action request/interrupt 仲裁接受
- **WHEN** OutputPass 运行 CommittedActionSource
- **THEN** CommittedActionSource MUST 提交 resolved `Action.Dodge`
- **AND** MUST 提交 committed action branch outcome
- **AND** MUST 提交 Dodge claim 和 channel outputs
- **AND** MUST 保留 source id、source step 和 pass 信息

#### Scenario: Rejected Action 无副作用
- **GIVEN** Dodge request 被拒绝
- **WHEN** CommittedActionSource 输出本帧 submissions
- **THEN** 它 MUST 提交 rejection diagnostic 或等价 request result
- **AND** MUST NOT 提交 action motion candidate
- **AND** MUST NOT 提交 action animation candidate
- **AND** MUST NOT 消费输入

### Requirement: ActionTimelineOutcome 映射为正式 Channel 输出
CommittedAction timeline outcome MUST 映射为正式 channel output。AnimationKey MUST 进入 Animation channel，MotionSpec MUST 进入 Motion channel，HitboxWindow / CancelWindow MUST 进入 Window/Facts channel，Cue MUST 进入 Cue channel。Timeline track 或 Editor lane MUST NOT 被当成 gameplay slot 或 claim 权威。

#### Scenario: Directional Dodge Channel 映射
- **GIVEN** Dodge selector 选择 Directional timeline
- **WHEN** `ActionTimelineEvaluator` 在当前帧产出 outcome
- **THEN** AnimationKey MUST 映射为 action animation submission
- **AND** MotionSpec MUST 映射为 action motion submission
- **AND** active window facts MUST 映射为 window/facts submission
- **AND** cue requests MUST 映射为 cue submission

#### Scenario: Backstep Dodge Channel 映射
- **GIVEN** Dodge selector 选择 Backstep timeline
- **WHEN** `ActionTimelineEvaluator` 在当前帧产出 outcome
- **THEN** Backstep 的 AnimationKey、MotionSpec、window facts 和 cue requests MUST 按同一 channel 规则提交
- **AND** channel 输出 MUST NOT 直接调用 presenter、motion executor 或 cue presenter

### Requirement: Composer 显式合成 CharacterFrameSubmission
`CharacterBehaviorSubmissionComposer` 或等价 composer MUST 显式消费 LocomotionSource 与 CommittedActionSource 的 typed submissions，并生成现有 `CharacterFrameSubmission` / `CharacterFrameArbitrationInput` / `CharacterFramePlan` 可消费的输入。Composer MUST NOT 长期依赖“最后一个 required output”或单一 pass-through submission 作为正式规则。

#### Scenario: Composer 合成兄弟候选
- **GIVEN** LocomotionSource 提交基础 motion / animation candidate
- **AND** CommittedActionSource 提交 Dodge claim 和 action motion / animation candidate
- **WHEN** Composer 构建 `CharacterFrameSubmission`
- **THEN** submission MUST 包含 Locomotion 候选
- **AND** MUST 包含 CommittedAction 候选
- **AND** MUST 包含 BodyArbiter 可消费的 claim 和 arbitration input

#### Scenario: Required Submission 未消费时报错
- **GIVEN** 某个 required source submission 已产生
- **WHEN** Composer 无法把它映射到 frame submission 或 diagnostics
- **THEN** Composer MUST 报告正式错误
- **AND** MUST NOT 静默丢弃
- **AND** MUST NOT 用 fallback output 替代

### Requirement: BodyArbiter 和 OutputApplier 保持唯一权威
Body/slot/claim 仲裁 MUST 继续由 `DefaultBodyArbiter`、`CharacterFramePlan` 或批准的等价角色级 plan 模块完成。最终 motion、animation、input consume、facts 和 snapshot 副作用 MUST 继续由角色级 output applier 完成。

#### Scenario: FullBody Claim 只影响 Slot 计划
- **GIVEN** CommittedActionSource 为 Dodge 提交 FullBody claim
- **WHEN** BodyArbiter 创建 `CharacterFramePlan`
- **THEN** BaseSlot owner MUST 是 CommittedAction
- **AND** UpperBodySlot MUST 被压制
- **AND** FullBody MUST NOT 成为 source、slot、graph root 或 runtime owner

#### Scenario: OutputApplier 是唯一副作用出口
- **WHEN** `CharacterFramePlan` 选择最终 motion 和 animation
- **THEN** motion executor 调用 MUST 只发生在角色级 output applier
- **AND** animation presenter 调用 MUST 只发生在角色级 output applier
- **AND** behavior runner、leaf、composer、branch evaluator 和 timeline evaluator MUST NOT 执行副作用

### Requirement: Runtime Chain 可测试和可审计
系统 MUST 提供 EditMode 测试和静态边界测试，证明 runtime submission chain 的顺序、source、channel、composer 和边界符合正式架构。

#### Scenario: 自动测试覆盖 Runtime Chain
- **WHEN** 运行 behavior submission runtime chain 测试
- **THEN** 测试 MUST 覆盖 request pass 顺序
- **AND** MUST 覆盖 output pass 顺序
- **AND** MUST 覆盖 LocomotionSource typed submission
- **AND** MUST 覆盖 CommittedActionSource typed submission
- **AND** MUST 覆盖 Dodge channel mapping
- **AND** MUST 覆盖 composer 显式合成规则
- **AND** MUST 覆盖 Directional / Backstep / rejected Dodge golden line

#### Scenario: 静态边界测试
- **WHEN** 运行 runtime static boundary 测试
- **THEN** 测试 MUST 确认 runtime 不引用 UnityEditor、GraphView、TimelinePlayer、PlayableGraph、Taco runner 或 Ref runtime tree
- **AND** 测试 MUST 确认没有第二 motion executor、第二 animation presenter、第二 blackboard writer 或第二角色控制入口
