# Tasks

## 0. 前置核对
- [x] 0.1 读取 `AGENTS.md`、`openspec/AGENTS.md`、`openspec/project.md`。
- [x] 0.2 运行 `openspec list` 和 `openspec list --specs`。
- [x] 0.3 读取 `formalize-character-behavior-submission-runtime-chain/proposal.md`、`design.md`、`tasks.md` 和 spec delta。
- [x] 0.4 读取相关 active changes：`refactor-character-behavior-authoring-source-boundary`、`migrate-ref-timeline-editor-to-formal-action-config`。
- [x] 0.5 读取当前 `CharacterFramePipeline`、`CharacterBehaviorSubmissionRunner`、`CharacterBehaviorSubmissionLeafs`、`CharacterBehaviorSubmissionComposer`。
- [x] 0.6 读取当前 `CommittedActionFrameSubmitter`、`LocomotionFrameSubmitter`、`CommittedActionBranchEvaluator`、`ActionTimelineEvaluator`。
- [x] 0.7 对将要修改的函数、类、方法运行 GitNexus `impact`。
- [x] 0.8 记录 HIGH / CRITICAL impact 的风险、直接调用方和受影响流程。

## 1. Runtime Entry 固化
- [x] 1.1 确认 `CharacterRuntimeCore` 是正式 runtime core owner。
- [x] 1.2 确认 `CharacterFramePipeline` 是唯一角色帧管线。
- [x] 1.3 确认 pipeline request submitter 使用 `CharacterBehaviorSubmissionRunner`。
- [x] 1.4 确认 pipeline output submitter 使用 `CharacterBehaviorSubmissionRunner`。
- [x] 1.5 缺 behavior runtime definition 时报告正式错误。
- [x] 1.6 不新增第二 pipeline、第二 runner、第二 motion executor 或第二 animation presenter。

## 2. Submission Contract 补齐
- [x] 2.1 审查 `CharacterBehaviorSubmissionSource` 是否足够表达 source id、source kind、pass、step、order。
- [x] 2.2 审查 request submission 是否足够表达 action request / decision / resolved action。
- [x] 2.3 审查 output submission 是否足够表达 Locomotion 和 CommittedAction 输出。
- [x] 2.4 新增或收敛 channel submission：Motion、Animation、Window、Cue、Facts。
- [x] 2.5 新增或收敛 claim submission：FullBody claim、UpperBody claim。
- [x] 2.6 新增或收敛 diagnostics/audit，禁止 required submission 未消费。
- [x] 2.7 确认 submission model 不持有 Unity runtime object、Editor object 或 Ref runtime object。

## 3. LocomotionSource Leaf
- [x] 3.1 拆分 Locomotion request context 输出。
- [x] 3.2 拆分 locomotion decision submission。
- [x] 3.3 拆分 locomotion state frame submission。
- [x] 3.4 拆分 locomotion motion candidate。
- [x] 3.5 拆分 locomotion animation candidate。
- [x] 3.6 拆分 locomotion facts / preemption input。
- [x] 3.7 保留旧 submitter helper 时标记删除条件。
- [x] 3.8 增加 LocomotionSource 不执行副作用测试。

## 4. CommittedActionSource Leaf
- [x] 4.1 拆分 action request submission。
- [x] 4.2 拆分 interrupt decision submission。
- [x] 4.3 拆分 resolved action submission。
- [x] 4.4 拆分 action lifecycle frame submission。
- [x] 4.5 拆分 committed action branch outcome submission。
- [x] 4.6 拆分 body occupancy claim submission。
- [x] 4.7 拆分 action motion candidate。
- [x] 4.8 拆分 action animation candidate。
- [x] 4.9 拆分 active window facts。
- [x] 4.10 拆分 cue requests。
- [x] 4.11 保留旧 submitter helper 时标记删除条件。
- [x] 4.12 增加 CommittedActionSource 不执行副作用测试。

## 5. Dodge Timeline Channel 映射
- [x] 5.1 Directional timeline AnimationKey 映射到 Animation channel。
- [x] 5.2 Directional timeline Motion 映射到 Motion channel。
- [x] 5.3 Directional timeline HitboxWindow / CancelWindow 映射到 Window/Facts channel。
- [x] 5.4 Directional timeline Cue 映射到 Cue channel。
- [x] 5.5 Backstep timeline AnimationKey 映射到 Animation channel。
- [x] 5.6 Backstep timeline Motion 映射到 Motion channel。
- [x] 5.7 Backstep timeline HitboxWindow / CancelWindow 映射到 Window/Facts channel。
- [x] 5.8 Backstep timeline Cue 映射到 Cue channel。
- [x] 5.9 Dodge FullBody claim 映射到 claim input，且不作为 source 或 slot。

## 6. Composer 显式规则
- [x] 6.1 Composer 显式要求 LocomotionSource output。
- [x] 6.2 Composer 显式消费 CommittedActionSource request/output。
- [x] 6.3 Composer 合成 `CharacterFrameArbitrationInput`。
- [x] 6.4 Composer 合成 `CharacterFrameActionOutputSubmission`。
- [x] 6.5 Composer 合成 `CharacterFrameMovementSubmission`。
- [x] 6.6 Composer 合成 `CharacterFrameAnimationSubmission`。
- [x] 6.7 Composer 合成 `CharacterFrameRuntimeFactsSubmission`。
- [x] 6.8 Composer 不使用“最后一个 required output”作为长期正式规则。
- [x] 6.9 Composer 不执行 motion、animation、input consume 或 blackboard write。
- [x] 6.10 Unsupported / unconsumed required submission 报正式错误。

## 7. BodyArbiter 与 FramePlan
- [x] 7.1 FullBody claim 被采纳时 BaseSlot owner 为 CommittedAction。
- [x] 7.2 FullBody claim 被采纳时 UpperBodySlot 被压制。
- [x] 7.3 无 FullBody claim 时 Locomotion 保持 BaseSlot。
- [x] 7.4 UpperBody claim 只影响 UpperBodySlot，不接管 BaseSlot。
- [x] 7.5 确认 BodyArbiter 不认识 Dodge timeline editor 或 Ref runtime。

## 8. Golden Line 与回归
- [x] 8.1 Directional Dodge 从输入到 frame plan 等价。
- [x] 8.2 Backstep Dodge 从输入到 frame plan 等价。
- [x] 8.3 Rejected Dodge 不消费输入、不执行 motion、不提交 animation。
- [x] 8.4 基础 Locomotion Idle / MoveLoop 输出等价。
- [x] 8.5 Rollback restore 到 Dodge 中间帧后继续 tick 输出等价。
- [x] 8.6 TurnBack 被 Dodge 抢占时 preemption fact 等价。

## 9. 自动测试
- [x] 9.1 增加 runner request pass 顺序测试。
- [x] 9.2 增加 runner output pass 顺序测试。
- [x] 9.3 增加 LocomotionSource typed submission 测试。
- [x] 9.4 增加 CommittedActionSource typed submission 测试。
- [x] 9.5 增加 Dodge timeline channel mapping 测试。
- [x] 9.6 增加 composer explicit rule 测试。
- [x] 9.7 增加 required submission 未消费报错测试。
- [x] 9.8 增加 runtime 静态边界测试：不引用 UnityEditor、GraphView、TimelinePlayer、PlayableGraph、Taco runner。
- [x] 9.9 增加无第二 executor / presenter / blackboard writer / character entry 测试。

## 10. 验证
- [x] 10.1 运行 `openspec validate formalize-character-behavior-submission-runtime-chain --strict --no-interactive`。
- [x] 10.2 通过 Unity MCP 尽量运行相关 EditMode 测试。
- [x] 10.3 Unity MCP 不可用时记录未执行测试名和原因。
- [x] 10.4 运行 `detect_changes({scope:"all"})` 并记录影响范围。
