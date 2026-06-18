## ADDED Requirements

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

## MODIFIED Requirements

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
