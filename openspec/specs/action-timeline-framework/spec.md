# action-timeline-framework Specification

## Purpose
定义 Action 生命周期内的纯数据 timeline、frame 权威、clip 评估、outcome 边界，以及它如何通过 CommittedActionBranch 进入角色帧管线。
## Requirements
### Requirement: ActionTimeline 纯数据定义
系统 MUST 提供 `ActionTimelineDefinition` 或等价纯数据定义，用于表达单个 Action 生命周期内的时序轨道和片段。Authoring 数据 MUST 使用 seconds 表达 duration、clip 起止和 cue 时间；runtime definition MUST 使用由 fixed tick interval 量化得到的 deterministic tick duration、tick 区间和 cue tick。ActionTimeline MUST 使用稳定 action id、track、clip、tick 区间和类型化 payload 表达运行时需要的数据。该定义 MUST NOT 保存 Unity 场景实例、动画 runtime 对象、输入系统对象或 MonoBehaviour runner。

#### Scenario: 定义保存动作时序
- **WHEN** 运行时读取某个 Action 的 timeline 定义
- **THEN** 定义 MUST 能提供 action id、duration ticks、tracks 和 clips
- **AND** 每个 runtime clip MUST 有稳定 kind、start tick 和 end tick 或 cue tick
- **AND** 定义 MUST NOT 持有 `Transform`、`CharacterController`、`Animator`、Animancer runtime object、`AnimationClip`、`InputAction` 或 `MonoBehaviour`

#### Scenario: 非法 seconds 区间被校验
- **GIVEN** authoring clip 的 end seconds 小于 start seconds
- **WHEN** 运行 ActionTimeline 校验或编译
- **THEN** 校验 MUST 报告错误
- **AND** runtime MUST NOT 通过隐藏默认区间继续使用该 clip

#### Scenario: Tick 是运行时权威
- **GIVEN** 工具层显示某个 clip 的 seconds 范围
- **WHEN** runtime 构建或评估 ActionTimeline
- **THEN** runtime MUST 将正式采样表达为 local tick
- **AND** seconds MUST 只作为 authoring、editor 显示、诊断或量化输入
- **AND** runtime 仲裁、测试断言和 rollback 对比 MUST 以 tick 结果作为权威来源

### Requirement: ActionTimeline Evaluator
系统 MUST 提供 `ActionTimelineEvaluator` 或等价纯逻辑评估模块，将 active action 的 local tick、source step 和 runtime timeline 定义评估为本 tick `ActionTimelineOutcome`。Evaluator MUST 是确定性的，并且 MUST NOT 自行保存跨帧 gameplay 状态、tick 角色帧管线或执行 Unity 副作用。Evaluator MUST NOT 从 seconds、Unity 时间、Animator 播放时间或 editor preview 状态推导采样位置。

#### Scenario: 当前 local tick 命中 clip
- **GIVEN** ActionTimeline 有一个 start tick 为 3、end tick 为 8 的 Motion clip
- **WHEN** evaluator 以 local tick 5 评估
- **THEN** outcome MUST 标记该 Motion clip active
- **AND** outcome MUST 包含该 clip 的纯数据 motion intent

#### Scenario: 当前 local tick 不命中 clip
- **GIVEN** ActionTimeline 有一个 start tick 为 3、end tick 为 8 的 Motion clip
- **WHEN** evaluator 以 local tick 8 评估
- **THEN** outcome MUST NOT 标记该 Motion clip active
- **AND** outcome MUST NOT 输出该 clip 的 motion intent

#### Scenario: Evaluator 不执行副作用
- **WHEN** evaluator 评估任意 ActionTimeline
- **THEN** evaluator MUST NOT 调用 motion executor
- **AND** MUST NOT 调用 animation presenter
- **AND** MUST NOT 写 `CharacterRuntimeBlackboard`
- **AND** MUST NOT Instantiate 或 Destroy Unity 对象

### Requirement: ActionTimeline Outcome 边界
ActionTimeline MUST 只产出本帧 Outcome。Outcome MAY 表达 animation intent、motion intent、hitbox window、cancel window 和 cue request，但 MUST 先进入 CommittedActionBranch outcome，再通过 `CharacterFrameSubmission`、Action candidate 或批准的等价角色帧数据合同进入 `CharacterFramePipeline`。ActionTimeline MUST NOT 直接写 runtime facts 或黑板。

#### Scenario: Outcome 进入角色帧提交
- **GIVEN** active action timeline 本帧输出 animation intent 和 motion intent
- **WHEN** Action submitter 构建本帧输出
- **THEN** animation intent 和 motion intent MUST 被合并进 `CharacterFrameSubmission` 或批准的 Action candidate
- **AND** 最终副作用 MUST 仍由 `CharacterFramePipeline` 的 output applier 执行

#### Scenario: 黑板只保存确认后的 facts
- **GIVEN** ActionTimeline 本帧输出 cancel window active
- **WHEN** 角色帧管线尚未应用最终输出
- **THEN** `CharacterRuntimeBlackboard` MUST NOT 被 ActionTimeline 直接改写
- **AND** 只有经角色帧管线确认后的事实 MAY 写入黑板

### Requirement: Action Lifecycle 驱动 Timeline
Action lifecycle MUST 是 ActionTimeline 的正式运行时推进位置。active action、action start step、local tick、started/exited 标记和播放意图身份 MUST 由 `ActionLifecycleRuntime` 或批准的等价 Action lifecycle state 持有。state time / elapsed seconds MAY 作为从 local tick 派生的诊断读数存在，但 MUST NOT 作为 timeline sampling 的权威。ActionTimeline MUST NOT 新增 `TreeRunner`、`TimelinePlayer`、MonoBehaviour Update 或第二角色帧 runner。

#### Scenario: accepted action 启动 timeline
- **GIVEN** request 仲裁 accepted 一个带 ActionTimeline 的 Action
- **WHEN** Action lifecycle tick 该 accepted action
- **THEN** lifecycle MUST 记录该动作实例的 action start step 或批准的等价整数 local tick state
- **AND** MUST 使用 action-local tick 评估本 tick outcome
- **AND** MUST NOT 创建新的 runtime runner

#### Scenario: restore 后继续同一动作时序
- **GIVEN** rollback restore 后 Action lifecycle 仍 active 同一个 action
- **WHEN** restore 后下一 tick 执行
- **THEN** timeline local tick MUST 从恢复的整数 action 时序状态和当前 source step 派生
- **AND** 播放意图身份 MUST 保持与恢复的 active action 对应

### Requirement: Action Catalog 装配 ActionTimeline
Action Catalog 或批准的等价 Action module 配置入口 MUST 能为 Action definition 定位 ActionTimeline 定义。runtime action definition MUST 只携带纯数据 timeline 信息或稳定引用解析结果，不得引用 editor-only graph、Unity scene object、TreeRunner、TimelinePlayer 或表现层 runtime。缺失必需 timeline 配置时 MUST 报告配置错误，不得 fallback 到旧 Dodge 字段、Resources、场景查找或代码默认 timeline。

#### Scenario: Action definition 定位 timeline
- **GIVEN** Action Catalog 包含 `Action.Dodge` 或等价 action definition
- **WHEN** runtime 构建该 action definition
- **THEN** 结果 MUST 能定位或包含对应 ActionTimeline 定义
- **AND** timeline 数据 MUST 是纯 runtime 数据

#### Scenario: 缺失 timeline 不 fallback
- **GIVEN** 某个必须使用 ActionTimeline 的 action definition 缺失 timeline
- **WHEN** runtime 校验或解析该 action
- **THEN** 系统 MUST 报告明确配置错误
- **AND** MUST NOT 通过旧动作字段、Resources、全局单例、场景对象或代码默认值继续运行

### Requirement: ActionTimeline 是 TimelineNode 数据
ActionTimeline MUST 作为 CommittedActionBranch 中 TimelineNode 的内部时序数据，而不是顶层技能结构或角色图结构。正式 gameplay runtime MUST 通过 CommittedActionBranch 进入 TimelineNode，再由 ActionTimelineEvaluator 评估 timeline。系统 MUST NOT 使用 Ref 项目的 `TreeRunner` 或 `TimelinePlayer` 作为正式 Action runner。

#### Scenario: CommittedActionBranch 节点引用 timeline
- **GIVEN** CommittedActionBranch 中存在 TimelineNode
- **WHEN** 工具层保存或构建 runtime 数据
- **THEN** TimelineNode MUST 引用或包含 ActionTimeline 定义
- **AND** 正式 gameplay MUST 先评估 CommittedActionBranch，再评估该 TimelineNode 的 ActionTimeline

#### Scenario: TimelineNode 不直接驱动 Unity 对象
- **WHEN** 正式 gameplay 执行某个 TimelineNode
- **THEN** TimelineNode 和 ActionTimeline runtime MUST NOT 直接调用 Animator、Transform、Prefab、ParticleSystem、PlayableGraph、motion executor 或 animation presenter
- **AND** MUST NOT 直接写 runtime blackboard

### Requirement: 第一版 Clip 能力范围
第一版 ActionTimeline MUST 至少支持 `AnimationKey`、`Motion`、`HitboxWindow`、`CancelWindow` 和 `Cue` 五类 clip 的纯数据表达。`HitboxWindow`、`CancelWindow` 和 `Cue` 第一版 MAY 只输出 outcome/fact/cue request，不要求实现物理命中、伤害结算或表现播放。Cue 第一版 MUST 只进入 outcome 和 diagnostics，不扩展正式 presentation cue submission 模型。

#### Scenario: AnimationKey clip 输出动画意图
- **GIVEN** 当前帧命中 AnimationKey clip
- **WHEN** evaluator 评估 ActionTimeline
- **THEN** outcome MUST 包含对应 `ActionAnimationKey` 或等价动画意图
- **AND** 实际播放 MUST 仍经角色帧输出和正式 animation presenter

#### Scenario: Cue clip 不播放表现
- **GIVEN** 当前帧触发 Cue clip
- **WHEN** evaluator 评估 ActionTimeline
- **THEN** outcome MAY 包含 cue request
- **AND** evaluator MUST NOT 直接播放 VFX、SFX、camera shake 或 post-processing
- **AND** 系统 MUST NOT 因第一版 Cue 新增第二条表现层运行路径

### Requirement: ActionTimeline 可测试和可验证
系统 MUST 为 ActionTimeline 框架提供自动测试和静态边界验证，证明其 seconds authoring、tick 量化、评估确定性、边界纯净、不会引入第二运行路径，并能等价表达现有 Dodge 行为。

#### Scenario: 自动测试覆盖 evaluator
- **WHEN** 运行 ActionTimeline EditMode 测试
- **THEN** 测试 MUST 覆盖 seconds -> tick 量化边界
- **AND** MUST 覆盖 clip `[startTick,endTick)` 起止边界
- **AND** MUST 覆盖多个 track 同 tick 输出
- **AND** MUST 覆盖 cue 单 tick 触发
- **AND** MUST 覆盖空 timeline 和非法 timeline 校验

#### Scenario: 静态边界验证
- **WHEN** 运行边界测试或静态搜索
- **THEN** 验证 MUST 确认 ActionTimeline runtime 不引用 `TreeRunner`
- **AND** MUST 确认 ActionTimeline runtime 不引用 `TimelinePlayer`
- **AND** MUST 确认 ActionTimeline runtime 不引用 Unity 场景实例或 animation runtime 对象
- **AND** MUST 确认 runtime 不读取 editor preview state 作为采样权威

#### Scenario: Dodge 等价验证
- **GIVEN** 现有 Dodge definition 和等价 seconds authoring timeline 测试定义
- **WHEN** 使用相同输入、fixed tick interval 和 source step 序列评估
- **THEN** timeline 输出的 duration ticks、distance、rotateToDirection 和 animation key MUST 与预期 Dodge 行为等价
- **AND** Dodge 完成和返回 Locomotion 的行为 MUST 不回退

#### Scenario: Dodge 不污染抽象模型
- **GIVEN** Dodge 作为第一个 ActionTimeline concrete instance
- **WHEN** 检查 ActionTimeline definition、outcome 和 evaluator
- **THEN** 这些抽象模型 MUST NOT 引用 Dodge 专用类型
- **AND** Dodge variant 到 timeline 的转换 MUST 位于 Dodge adapter、builder 或等价 concrete implementation

### Requirement: Motion Clip 可声明 Warp Payload
ActionTimeline Motion clip MUST 能以纯数据声明可选 Motion Warping payload。该 payload MAY 包含 warp policy id、target binding id、motion profile id、compiled tick duration 或 motion window binding、axis mask、rotation policy、攻击吸附开关、转向修正开关；MUST NOT 持有 Unity scene object、Animancer runtime object、Animator、AnimationClip、CharacterController 或 MonoBehaviour runner。

#### Scenario: Motion clip 携带 warp payload
- **GIVEN** ActionTimeline 中存在 Motion clip
- **AND** 该 clip 配置了 warp policy id 和 target binding id
- **WHEN** runtime 构建 `ActionTimelineDefinition`
- **THEN** definition MUST 保存这些 warp 字段的纯数据形式
- **AND** definition MUST NOT 保存场景目标对象或表现层对象

#### Scenario: 未配置 warp 时保持普通 motion
- **GIVEN** ActionTimeline Motion clip 只配置 duration、distance 和 rotateToDirection 或等价普通 motion payload
- **WHEN** evaluator 命中该 clip
- **THEN** outcome MUST 继续输出普通 motion intent
- **AND** 现有 Dodge Directional / Backstep 行为 MUST 不因 warp payload 支持而改变

### Requirement: Timeline Evaluator 不解析 Warp Target
`ActionTimelineEvaluator` MUST 只以 action-local tick 评估当前命中的 Motion clip 并输出 motion intent。它 MUST NOT 解析 warp target、运行 Motion Warping solver、调用 motion executor、读取场景对象或写 runtime blackboard。

#### Scenario: Evaluator 只输出 intent
- **GIVEN** 当前 action-local tick 命中带 warp payload 的 Motion clip
- **WHEN** `ActionTimelineEvaluator` 评估 timeline
- **THEN** outcome MUST 包含对应 motion intent 和 warp payload
- **AND** evaluator MUST NOT 输出已经应用到角色根的 delta
- **AND** evaluator MUST NOT 读取目标 `Transform`

#### Scenario: Target 解析在后续 motion resolve
- **GIVEN** outcome 包含带 target binding id 的 motion intent
- **WHEN** Action submitter 或后续 motion resolve 阶段处理该 outcome
- **THEN** target binding MUST 在 motion resolve 边界解析为纯数据 target snapshot
- **AND** Timeline evaluator MUST 不参与 target provider 调用

#### Scenario: 攻击吸附与转向修正只作为 intent
- **GIVEN** 当前 action-local tick 命中带攻击吸附和转向修正 payload 的 Motion clip
- **WHEN** `ActionTimelineEvaluator` 评估 timeline
- **THEN** outcome MUST 只表达攻击吸附和转向修正 intent
- **AND** evaluator MUST NOT 计算 planar delta
- **AND** MUST NOT 计算 yaw delta

### Requirement: Warp Payload 校验
系统 MUST 对 ActionTimeline Motion clip 的 Motion Warping payload 提供校验。缺失必需 policy、target binding、profile 或非法 motion window / tick 区间 MUST 报告配置错误，runtime MUST NOT 通过隐藏默认值继续执行 warped motion。

#### Scenario: 必需 target binding 缺失
- **GIVEN** Motion clip 的 warp policy 要求 target
- **AND** clip 未配置 target binding id
- **WHEN** 运行 ActionTimeline 校验
- **THEN** 校验结果 MUST 报告错误
- **AND** runtime MUST NOT 使用默认 target 继续执行

#### Scenario: 非法 payload 不进入 solver
- **GIVEN** Motion clip 的 warp payload 校验失败
- **WHEN** runtime 构建或评估该 timeline
- **THEN** 系统 MUST 阻止该 warped motion 被送入 Motion Warping solver
- **AND** MUST 输出明确诊断或校验错误

### Requirement: ActionTimeline Seconds 到 Tick 量化
系统 MUST 提供 ActionTimeline seconds authoring 到 deterministic tick runtime 的统一量化规则。量化 MUST 使用 simulation tick system 的 fixed tick interval 语义，并 MUST 通过 ActionTimeline compile context 或批准的等价 seam 显式传入 compiler。Compiler MUST NOT 使用 Unity `Time.deltaTime`、`Time.fixedDeltaTime`、Animator playback time、editor preview time 或 render frame delta 作为权威来源。Legacy frame 迁移 MUST 使用显式 legacy authoring frame rate 先转换为 seconds，默认 legacy authoring frame rate 为 60Hz，再按 fixed tick interval 编译为 runtime ticks。

#### Scenario: Compiler 使用显式 tick interval seam
- **GIVEN** simulation tick settings 提供 fixed tick interval 为 `1/60` 秒
- **WHEN** 调用方编译 ActionTimeline authoring
- **THEN** 调用方 MUST 将该 fixed tick interval 通过 compile context 或批准的等价 seam 传入 compiler
- **AND** compiler MUST NOT 自行读取 Unity `Time.fixedDeltaTime`、Editor preview state 或 render frame delta

#### Scenario: 持续片段量化为 tick 区间
- **GIVEN** fixed tick interval 为 `1/60` 秒
- **AND** 一个 Motion clip authoring 范围为 `0.05s` 到 `0.22s`
- **WHEN** compiler 量化该 clip
- **THEN** start tick MUST 是第一个不早于 `0.05s` 的 local tick
- **AND** end tick MUST 是第一个不早于 `0.22s` 的 local tick
- **AND** runtime active 区间 MUST 为 `[startTick, endTick)`

#### Scenario: Cue 量化为单 tick
- **GIVEN** 一个 Cue authoring time 为 `0.08s`
- **WHEN** compiler 量化该 cue
- **THEN** cue tick MUST 是第一个不早于 `0.08s` 的 local tick
- **AND** runtime MUST 只在该 local tick 输出 cue request

#### Scenario: 量化不提前触发
- **GIVEN** authoring window 的 start seconds 不落在 tick 边界上
- **WHEN** runtime 采样早于该 start seconds 的 local tick
- **THEN** outcome MUST NOT 提前输出该 window、motion、animation 或 cue

#### Scenario: Legacy frame 迁移不直接解释为 runtime tick
- **GIVEN** legacy frame asset 的 Motion clip start frame 为 10
- **AND** legacy authoring frame rate 为 60Hz
- **WHEN** 迁移器转换该 legacy frame
- **THEN** 迁移器 MUST 先得到 start seconds `10 / 60`
- **AND** compiler MUST 再按 simulation tick settings 的 fixed tick interval 量化为 runtime tick
- **AND** runtime MUST NOT 将 legacy start frame 10 直接当作 local tick 10

