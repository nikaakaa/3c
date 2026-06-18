# Design: Character Behavior Submission Runtime Chain

## 当前状态
已有运行时结构：

```text
CharacterFramePipeline
 -> ICharacterFrameRequestSubmitter
 -> ICharacterFrameOutputSubmitter
 -> CharacterBehaviorSubmissionRunner
 -> LocomotionBehaviorSubmissionLeaf
 -> CommittedActionBehaviorSubmissionLeaf
 -> CharacterBehaviorSubmissionComposer
 -> CharacterFrameSubmission
 -> DefaultBodyArbiter
 -> CharacterFramePlan
 -> CharacterFrameOutputApplier
```

这条链方向正确，但还有三个问题：

1. leaf 仍然包旧 submitter，typed submission 不够清楚。
2. composer 选择最终 output 的规则过于隐式。
3. Dodge timeline outcome 虽然已经存在，但没有被完整定义为 behavior channel submission 边界。

## 目标运行时链
正式链路为：

```text
CharacterRuntimeCore
 -> CharacterFramePipeline
 -> CharacterBehaviorSubmissionRunner
    -> RequestPass
       -> LocomotionSource
       -> CommittedActionSource
    -> OutputPass
       -> LocomotionSource
       -> CommittedActionSource
 -> CharacterBehaviorSubmissionSet
 -> CharacterBehaviorSubmissionComposer
 -> CharacterFrameSubmission
 -> DefaultBodyArbiter
 -> CharacterFramePlan
 -> CharacterFrameOutputApplier
```

Runner 和 leaf 只提交数据，不执行 motion，不播放 animation，不写 blackboard。

## Source 与 Channel
### LocomotionSource
LocomotionSource 输出：

- locomotion decision
- locomotion state decision
- locomotion frame
- state frame
- locomotion motion candidate
- locomotion animation candidate
- locomotion facts / preemption input

### CommittedActionSource
CommittedActionSource 输出：

- action request submission
- action interrupt decision
- resolved action
- action lifecycle frame
- committed action branch outcome
- body occupancy claim
- action motion candidate
- action animation candidate
- active window facts
- cue requests

### Channel
Action timeline outcome 映射到 channel：

- AnimationKey -> Animation channel
- MotionSpec -> Motion channel
- HitboxWindow / CancelWindow -> Window / Facts channel
- Cue -> Cue channel
- BodyOccupancyClaim -> Claim input

这些 channel 进入 `CharacterFrameSubmission` / `CharacterFrameArbitrationInput`，最后仍由 `DefaultBodyArbiter` 和 output applier 执行。

## Composer 决策
Composer 不应长期依赖“最后一个 required output”。目标规则：

1. 必须存在 LocomotionSource output。
2. CommittedActionSource output 可选，但当有 accepted/resolved action 或 action output 时必须被消费。
3. Locomotion 的基础候选与 CommittedAction 的 action 候选一起进入 `CharacterFrameArbitrationInput`。
4. FullBody claim 只来自 claim 数据，不来自 source/root 名称。
5. Composer 输出单个 `CharacterFrameSubmission`。
6. Composer 不执行副作用。

## Dodge 示例
Directional Dodge：

```text
Input Dodge + MoveIntent
 -> CommittedAction request accepted
 -> Active Action.Dodge variant Directional
 -> Dodge selector 选 Directional timeline
 -> timeline 输出 AnimationKey / Motion / Window / Cue
 -> FullBody claim 进入 BodyArbiter
 -> BaseSlot owner = CommittedAction
 -> UpperBodySlot suppressed
```

Backstep Dodge：

```text
Input Dodge + NoMoveIntent
 -> CommittedAction request accepted
 -> Active Action.Dodge variant Backstep
 -> Dodge selector 选 Backstep timeline
 -> timeline 输出 Backstep channel
 -> FullBody claim 进入 BodyArbiter
```

## 边界
- `CharacterFramePipeline` 仍只做 phase 调度。
- `DefaultBodyArbiter` 仍是 body/slot/claim 仲裁权威。
- `CharacterFrameOutputApplier` 仍是 motion、animation、facts 的副作用出口。
- Runner、leaf、composer、branch evaluator、timeline evaluator 都不得持有 Unity scene object 或 Editor type。

## 迁移计划
1. 为现有 submission contracts 补齐 source/channel 字段和审计。
2. 把 Locomotion leaf 输出拆成明确 typed source output。
3. 把 CommittedAction leaf 输出拆成明确 typed source output。
4. 将 Dodge timeline outcome 映射为 action channel submission。
5. 重写 composer 显式合成规则。
6. 保留旧 submitter 作为 leaf 内部迁移 helper 时，测试标记删除条件。
7. 用 golden line 验证 Directional / Backstep / rejected Dodge 行为不变。

## 风险
- 现有大量测试可能断言 `CharacterFrameSubmissionSource.FullBody` 或旧诊断字符串，需要逐步迁移到 source/channel 语义。
- 如果一次性删除旧 submitter helper，风险较高；可以先保持内部 helper，但正式输出必须 typed 化并有删除条件。
- Composer 显式化可能暴露旧链路中被 pass-through 掩盖的缺配置问题，必须报正式错误，不加 fallback。
