## Context
当前项目里已经存在三类相近但职责不同的模型：

- `StateTimelinePolicy` / `StateTimelineWindowFacts`：表达状态生命周期内的 motion、input lock、request/cancel/interrupt、exit、priority、resistance、min priority 和 force。
- `ActionInterruptPolicy` / `ActionInterruptArbiter`：表达请求 priority、当前 resistance、force、elapsed time rule、window id 以及 accepted/rejected。
- 状态机 transition：表达状态图边、transition priority 和条件求值。

本变更只收口数据模型与配置边界。窗口负责定义 timing 并产出纯数据 facts；请求策略负责描述 from state、target state、request kind、priority、force 和 required fact id；状态机 transition 只在 accepted fact 或普通条件满足后选边。单帧 current/projected/target facts 权威、transition evaluator 拆分和 action motion output 数学由当前 specs 的稳定合同承接。

## Goals
- 把窗口 timing 的正式数据源收口到 `StateTimelinePolicy`。
- 把 runtime 可消费的窗口结果收口为稳定 `TimelineFactId` 或等价类型化 fact。
- 把请求准入策略收口为纯数据 from/target/request/min priority/force/required fact id。
- 明确 `transitionPriority`、`requestPriority`、`stateResistance`、`windowMinPriority`、`force` 的不同含义。
- 保留旧 elapsed timing rule 的迁移兼容，同时要求新增状态请求优先使用 required fact id。
- 为 policy、facts、request policy 和仲裁兼容性建立自动测试与配置校验。

## Non-Goals
- 不实现轻攻击连招。
- 不迁移 TurnBack、Dodge、Attack 或 HitReact 的具体运行时接入。
- 不定义单帧 current/projected/target facts 的权威归属。
- 不拆分 transition evaluator。
- 不移动 action motion output 数学。
- 不实现完整 timeline 编辑器。
- 不把 clip、fade、speed、start time、TransitionAsset、TransitionLibrary key 或 Animancer event 放进 timeline policy。
- 不新增 hidden fallback 配置。

## Decisions
### Decision: StateTimelinePolicy 只拥有窗口数据
`StateTimelinePolicy` 是状态窗口 timing 的正式配置源。它可以表达 window kind、time domain、start/end、request kind、priority、resistance、min priority、force、window id 和 fact id，但不能引用 MonoBehaviour、Transform、Animator、Animancer、AnimationClip、TransitionAsset、CharacterController 或场景实例。

### Decision: facts 使用类型化标识
window id 用于编辑、诊断和校验。runtime 准入、自然退出、输入锁和运动输出必须优先依赖 `TimelineFactId` 或等价类型化 fact。新增逻辑不得通过临时字符串分支，也不得让状态机、仲裁器和编辑器各自维护一套 tag 名称。

### Decision: 请求策略引用 required fact id
新增状态请求策略只描述 from state、target state、request kind、min priority、force 和 required fact id。旧 elapsed time timing rule 仅作为迁移兼容保留；新增 TurnBack、Attack combo、HitReact 或等价状态请求不得重新配置同一窗口的 start/end。

### Decision: 仲裁器消费 facts，不拥有时间
仲裁器只能消费 request、request policy、当前 state resistance 和 `StateTimelineWindowFacts`。它不得采样 timeline policy，不得读取动画外观层，不得读取 Animancer、Animator、AnimationClip 或 TransitionAsset。

### Decision: transition priority 不表达请求准入
状态机 transition 的 priority 只用于多条状态图边同时满足时选边。请求是否能进入目标状态，只能由 request priority、state resistance、window min priority、force、required fact 和策略匹配决定。

### Decision: natural exit、interrupt/cancel、motion/input lock 分离
`Exit` window 只表达当前状态自然收尾。`Interrupt` / `Cancel` / request window 才表达外部请求可进入仲裁。`Motion` window 只表达该状态是否输出 motion facts。`InputLock` window 只表达普通输入是否被抑制。

### Decision: visual fade 完全属于表现层
修改 Animancer transition fade、clip、speed、start time 或 TransitionAsset 不能改变逻辑状态切换、window facts、request accepted/rejected 或 baked motion 采样结果。

### Decision: 缺失正式配置必须诊断失败
缺少必需 timeline policy、window、fact id 或 request policy 时，系统必须输出配置诊断。不得通过 Resources、全局单例、代码生成默认值、场景查找或隐式 fallback 让状态继续运行。

## Boundaries With Current Specs
- `animation-phase-timeline-facts`：负责单帧 current/projected/target facts 权威、采样入口和帧内使用顺序。
- `unified-character-state-machine`：负责 transition evaluator 插拔、条件上下文和状态机选边边界。
- `fullbody-action-framework`、`character-runtime-blackboard`：负责 action motion output 数学归属和 motion facts 的消费边界。
- 后续 Attack、Skill 或 HitReact 实现只能复用本变更的数据模型和请求策略边界，不在本变更内实现连招。

## Field Ownership
- `transitionPriority`
  - Owner: 状态机 transition。
  - Meaning: 多条已满足 transition 的选边顺序。
  - Must not: 表达请求强度、抗性或窗口准入。
- `requestPriority`
  - Owner: 请求构建器或状态请求配置。
  - Meaning: 当前请求强度。
  - Used by: 仲裁器。
- `stateResistance`
  - Owner: 当前状态 timeline policy 或当前 runtime action/state context。
  - Meaning: 当前状态抵抗被打断的能力。
  - Used by: 仲裁器。
- `windowMinPriority`
  - Owner: request/cancel/interrupt window。
  - Meaning: 当前窗口最低准入请求强度。
  - Used by: 仲裁器。
- `force`
  - Owner: request policy 或 request/cancel/interrupt window。
  - Meaning: 是否允许绕过 resistance。
  - Used by: 仲裁器。
- `timelineFactId`
  - Owner: timeline window 输出定义。
  - Meaning: 当前状态时间段产出的类型化事实。
  - Used by: 仲裁器、自然退出、输入锁和运动输出消费者。
  - Must not: 使用临时字符串或由多个系统各自命名。
- `motionWindow`
  - Owner: timeline policy。
  - Meaning: 状态是否输出 motion facts。
  - Must not: 授权请求打断。
- `inputLockWindow`
  - Owner: timeline policy。
  - Meaning: 状态是否抑制普通输入移动/旋转。
  - Must not: 授权请求打断。
- `exitWindow`
  - Owner: timeline policy。
  - Meaning: 状态是否允许自然退出。
  - Must not: 授权外部请求打断。
- `interrupt/cancel/requestWindow`
  - Owner: timeline policy。
  - Meaning: 状态是否允许某类外部请求进入仲裁。
  - Must include: request kind 或等价过滤、fact id、min priority/force 语义。

## Migration Plan
1. 文档收口：本 change 只保留数据模型、配置边界、校验和兼容测试。
2. 模型收口：定义或整理 `StateTimelinePolicy`、window definition、`TimelineFactId`、`StateTimelineWindowFacts` 和状态请求策略数据。
3. 校验收口：覆盖空 id、非法时间域、非法窗口范围、重复窗口、缺失 required fact id、缺少必需 TurnBack window。
4. 仲裁兼容：保证现有 Dodge / TurnBack 请求策略仍能通过纯数据 request、policy、context 和 facts 测试。
5. 消费者后置：运行时接入、transition evaluator 拆分和 motion output 消费分别在相关 change 中实现。

## Risks / Trade-offs
- Risk: 保留旧 elapsed timing rule 会让设计者继续配置两套时间。
  - Mitigation: 新增状态请求规范要求 required fact id；elapsed timing rule 只作为迁移兼容和旧 Dodge 保护。
- Risk: 本 change 重新膨胀到运行时迁移。
  - Mitigation: tasks 明确把 facts 权威、transition evaluator 和 action motion output 交给独立 change。
- Risk: 配置字段名继续混淆。
  - Mitigation: 文档和测试必须覆盖 transition priority、request priority、state resistance、window min priority 和 force 的差异。
- Risk: 缺失配置被隐式默认值掩盖。
  - Mitigation: 缺失正式配置必须诊断失败，不允许 fallback。

## Open Questions
- 旧 `ActionInterruptPolicy.TimingRule` 第一版是保留只读兼容，还是立刻迁为 required fact id 后废弃？
- 后续 state interrupt window 配置是否需要拆出角色级 override；若需要，命名仍 MUST 复用 Action Interrupt policy 语义，不新增 request policy 分支。
