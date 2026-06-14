## Context
当前代码和规格里已经出现三套相近但不等价的东西：

- `StateTimelinePolicy` / `StateTimelineWindowFacts`：已经能表达 motion、input lock、interrupt、cancel、exit、priority、resistance、min priority 和 force。
- `ActionInterruptPolicy` / `ActionInterruptArbiter`：已经能按 request priority、current resistance、force、elapsed time rule 和 window id 做 accepted/rejected。
- 状态机 transition：有自己的 transition priority，并消费 `HasInputRequest`、`LocomotionAnimationCanExit` 等条件。

问题不是要不要拆开，而是拆开到哪一层：窗口和打断必须分离职责，但运行时只允许一条请求准入路径。窗口负责“当前状态此刻开放了什么事实”；这些事实用稳定 `TimelineFactId` 或等价类型化 tag 表达；打断负责“某个请求能不能使用这些事实进入目标状态”；状态机负责“accepted fact 出现后按状态图切换”。

## Goals
- 把窗口 timing 的唯一来源收口到 `StateTimelinePolicy`。
- 把请求准入的唯一裁决入口收口到 `ActionInterruptArbiter` 或其状态请求化演进。
- 明确 `transitionPriority`、`requestPriority`、`stateResistance`、`windowMinPriority`、`force` 的不同含义。
- 让 TurnBack、Dodge、后续 Attack 和 HitReact 复用同一套 window facts + request policy 规则。
- 让自然退出、取消/打断、输入锁、运动窗口和视觉混合拥有不同语义，不再互相代用。
- 保留未来编辑器边界：编辑器只编辑/校验正式配置，不生成第二套 runtime 图。

## Non-Goals
- 不实现攻击连招。
- 不实现完整 timeline 编辑器。
- 不把旧 `ActionInterruptPolicy` 一次性删除；可以先迁移语义并保留兼容字段。
- 不修改 Fantasy 协议或网络同步。
- 不新增 hidden fallback 配置。
- 不把 clip、fade、speed、start time、TransitionAsset、TransitionLibrary key 或 Animancer event 放进 timeline policy。

## Decisions
### Decision: 窗口和打断分开做，但用 facts 连接
`StateTimelinePolicy` 是窗口数据权威；`StateTimelineSampler` 将窗口采样为 `StateTimelineWindowFacts`。仲裁器只消费 facts，不自己采样时间，不读取动画外观层，不读取 timeline SO。

### Decision: facts 是类型化 tag，不是任意字符串
窗口配置可以有 window id 方便编辑器定位，但运行时准入必须依赖稳定 `TimelineFactId` 或等价强类型标识，例如 `InputLocked`、`MotionLocked`、`CancelableToDodge`、`ComboInputOpen`、`NaturalExitReady`。新增逻辑不得通过临时字符串判断分支，也不得让状态机、仲裁器和编辑器各自维护一套 tag 名称。

### Decision: 新增状态请求优先使用 required fact id
现有 elapsed time timing rule 作为迁移兼容保留，但新增 TurnBack、Attack combo、HitReact 等状态请求必须优先通过 `requiredFactId` 或等价字段关联到 timeline facts。这样窗口时间只配置一次。

### Decision: transition priority 不再表达请求准入
状态机 transition 的 priority 只用于多条状态图边同时满足时选边。请求是否能进目标状态，只能由 request priority、state resistance、window min priority、force 和策略匹配决定。

### Decision: natural exit 不等于 interrupt/cancel
`Exit` window 只允许当前状态自然收尾，例如 TurnBack 转完回 MoveLoop/Idle、Attack03 播完回 Locomotion。`Interrupt` / `Cancel` window 才允许外部请求进入仲裁，例如 Dodge cancel、Attack combo 或 HitReact。

### Decision: motion/input lock 是状态输出窗口，不是请求许可
`Motion` window 只控制该状态是否输出动画/烘焙运动贡献。`InputLock` window 只控制普通输入是否被抑制。二者不能授权 Dodge、Attack 或 TurnBack 请求。

### Decision: visual fade 完全属于表现层
修改 Animancer transition fade、clip、speed、start time 不能改变逻辑状态切换、window facts、request accepted/rejected 或 baked motion 采样结果。

### Decision: 先迁 TurnBack，再接攻击
TurnBack 已经有正式状态、baked profile 和 timeline policy 雏形，是最小可验证对象。攻击连招必须等本变更稳定后复用同一套窗口和仲裁模型。

## Runtime Shape
```text
Input / derived intent
-> request candidate
-> current state snapshot + timeline policy
-> StateTimelineWindowFacts
-> StateRequestInterruptPolicy(requiredFactId) + ActionInterruptArbiter
-> accepted CharacterInputRequestFact
-> unified state machine transition
-> state output
-> motion executor / animation presenter
```

## Field Ownership
- `transitionPriority`
  - Owner: 状态机 transition。
  - Meaning: 多条已满足 transition 的选边顺序。
  - Must not: 表达请求强度、抗性或窗口准入。
- `requestPriority`
  - Owner: 请求构建器或动作/状态请求配置。
  - Meaning: 当前请求强度。
  - Used by: 仲裁器。
- `stateResistance`
  - Owner: 当前状态 timeline policy 或当前 action runtime state。
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
  - Used by: 仲裁器、状态输出层和自然退出条件。
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
  - Must include: 输出的 request/cancel fact id，以及 request type 或等价过滤。

## Migration Plan
1. 术语收口：文档和配置字段说明先区分 transition priority、request priority、state resistance、window min priority 和 force。
2. 模型收口：确认 `StateTimelinePolicy` 是窗口 timing 唯一来源，新增请求策略只引用 required fact id。
3. 仲裁收口：让状态请求仲裁只消费 request + request policy + current state resistance + window facts。
4. TurnBack 迁移：把 TurnBack 的 motion/input lock/exit timing 从局部字段迁到 timeline policy。
5. 配置命名收口：将 `DefaultDodgeInterruptPolicySet` 这类已经承载 TurnBack 的资产改名为 FullBody/State request policy 语义。
6. 测试收口：先覆盖 TurnBack 和现有 Dodge 兼容，再允许 `add-light-attack-combo-action` 复用窗口模型。
7. 编辑器后置：只在 runtime 模型稳定后规划可视化编辑器。

## Risks / Trade-offs
- Risk: 保留旧 elapsed timing rule 会让设计者继续配置两套时间。
  - Mitigation: 新状态请求规范要求 required fact id；elapsed timing rule 仅作为迁移兼容和旧 Dodge 保护。
- Risk: TurnBack 当前实现仍有局部 timing 字段，迁移期间可能双写。
  - Mitigation: 任务要求先测试等价，再移除或降级局部 timing 权威。
- Risk: 配置字段名继续混淆。
  - Mitigation: 任务要求重命名资产和文档字段，测试中明确 transition priority 不参与 request priority 裁决。
- Risk: 活跃重构同时触碰 FullBody frame pipeline。
  - Mitigation: 本变更只定义 shared facts 和仲裁边界；遇到需要绕过当前 pipeline 的实现必须停止。

## Open Questions
- 旧 `ActionInterruptPolicy.TimingRule` 第一版是保留只读兼容，还是立刻迁为 required fact id 后废弃？
- TurnBack 默认窗口数值是否继续用当前 0.47 秒 baked profile 对应 normalized window，还是由你重新指定手感值？
- `DefaultDodgeInterruptPolicySet` 资产重命名为 `DefaultFullBodyRequestPolicySet` 还是 `DefaultStateRequestPolicySet`？
