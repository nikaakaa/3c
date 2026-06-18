## ADDED Requirements

### Requirement: ActionTimeline 纯数据定义
系统 MUST 提供 `ActionTimelineDefinition` 或等价纯数据定义，用于表达单个 Action 生命周期内的时序轨道和片段。该定义 MUST 使用稳定 action id、track、clip、frame 区间和类型化 payload 表达运行时需要的数据。ActionTimeline 的正式时间单位 MUST 是对齐 simulation tick / gameplay tick 的 frame；seconds 只允许作为工具层显示或编辑换算。该定义 MUST NOT 保存 Unity 场景实例、动画 runtime 对象、输入系统对象或 MonoBehaviour runner。

#### Scenario: 定义保存动作时序
- **WHEN** 运行时读取某个 Action 的 timeline 定义
- **THEN** 定义 MUST 能提供 action id、duration frames、tracks 和 clips
- **AND** 每个 clip MUST 有稳定 kind、start frame 和 end frame
- **AND** 定义 MUST NOT 持有 `Transform`、`CharacterController`、`Animator`、Animancer runtime object、`AnimationClip`、`InputAction` 或 `MonoBehaviour`

#### Scenario: 非法 frame 区间被校验
- **GIVEN** 一个 clip 的 end frame 小于 start frame
- **WHEN** 运行 ActionTimeline 校验
- **THEN** 校验 MUST 报告错误
- **AND** runtime MUST NOT 通过隐藏默认区间继续使用该 clip

#### Scenario: Seconds 不是运行时权威
- **GIVEN** 工具层显示某个 clip 的 seconds 范围
- **WHEN** runtime 构建或评估 ActionTimeline
- **THEN** runtime MUST 将正式时间表达为 tick frame
- **AND** seconds MUST 只通过 tick interval 或 frame rate 换算得到
- **AND** runtime 仲裁、测试断言和 rollback 对比 MUST 不以 seconds 作为权威来源

### Requirement: ActionTimeline Evaluator
系统 MUST 提供 `ActionTimelineEvaluator` 或等价纯逻辑评估模块，将 active action 的 state time、source step 和 timeline 定义评估为本帧 `ActionTimelineOutcome`。Evaluator MUST 是确定性的，并且 MUST NOT 自行保存跨帧 gameplay 状态、tick 角色帧管线或执行 Unity 副作用。

#### Scenario: 当前帧命中 clip
- **GIVEN** ActionTimeline 有一个 start frame 为 3、end frame 为 8 的 Motion clip
- **WHEN** evaluator 以 current frame 5 评估
- **THEN** outcome MUST 标记该 Motion clip active
- **AND** outcome MUST 包含该 clip 的纯数据 motion intent

#### Scenario: 当前帧不命中 clip
- **GIVEN** ActionTimeline 有一个 start frame 为 3、end frame 为 8 的 Motion clip
- **WHEN** evaluator 以 current frame 9 评估
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
Action lifecycle MUST 是 ActionTimeline 的正式运行时推进位置。active action、state time、started/exited 标记和播放意图身份 MUST 仍由 `ActionLifecycleRuntime` 或批准的等价 Action lifecycle state 持有。ActionTimeline MUST NOT 新增 `TreeRunner`、`TimelinePlayer`、MonoBehaviour Update 或第二角色帧 runner。

#### Scenario: accepted action 启动 timeline
- **GIVEN** request 仲裁 accepted 一个带 ActionTimeline 的 Action
- **WHEN** Action lifecycle tick 该 accepted action
- **THEN** lifecycle MUST 将 active action state time 从该动作实例开始推进
- **AND** MUST 使用该 action 的 timeline 定义评估本帧 outcome
- **AND** MUST NOT 创建新的 runtime runner

#### Scenario: restore 后继续同一动作时序
- **GIVEN** rollback restore 后 Action lifecycle 仍 active 同一个 action
- **WHEN** restore 后下一帧 tick
- **THEN** timeline current frame MUST 从恢复的 state time 派生
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
系统 MUST 为 ActionTimeline 框架提供自动测试和静态边界验证，证明其评估确定、边界纯净、不会引入第二运行路径，并能等价表达现有 Dodge 行为。

#### Scenario: 自动测试覆盖 evaluator
- **WHEN** 运行 ActionTimeline EditMode 测试
- **THEN** 测试 MUST 覆盖 clip 起止边界
- **AND** MUST 覆盖多个 track 同帧输出
- **AND** MUST 覆盖 cue 一次性触发
- **AND** MUST 覆盖空 timeline 和非法 timeline 校验

#### Scenario: 静态边界验证
- **WHEN** 运行边界测试或静态搜索
- **THEN** 验证 MUST 确认 ActionTimeline runtime 不引用 `TreeRunner`
- **AND** MUST 确认 ActionTimeline runtime 不引用 `TimelinePlayer`
- **AND** MUST 确认 ActionTimeline runtime 不引用 Unity 场景实例或 animation runtime 对象

#### Scenario: Dodge 等价验证
- **GIVEN** 现有 Dodge definition 和等价 ActionTimeline 测试定义
- **WHEN** 使用相同输入、delta 和 source step 评估
- **THEN** timeline 输出的 duration、distance、rotateToDirection 和 animation key MUST 与现有 Dodge 行为等价
- **AND** Dodge 完成和返回 Locomotion 的行为 MUST 不回退

#### Scenario: Dodge 不污染抽象模型
- **GIVEN** Dodge 作为第一个 ActionTimeline concrete instance
- **WHEN** 检查 ActionTimeline definition、outcome 和 evaluator
- **THEN** 这些抽象模型 MUST NOT 引用 Dodge 专用类型
- **AND** Dodge variant 到 timeline 的转换 MUST 位于 Dodge adapter、builder 或等价 concrete implementation
