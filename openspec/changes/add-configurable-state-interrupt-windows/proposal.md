# Change: 新增状态 Timeline Policy 与请求策略数据模型

## Why
当前项目里 priority、resistance、force、状态窗口、动作打断策略和动画退出时间已经都有雏形，但职责边界还不够清晰：`ActionInterruptPolicy` 能表达 elapsed time window，`StateTimelinePolicy` 也能表达 request window，TurnBack 自己还有局部 lock/exit timing 字段，状态机 transition 也有 priority。继续在这个基础上接攻击连招或编辑器，会把“窗口”和“打断”混成第二套运行路径。

本变更只收口数据模型和配置边界：窗口负责定义 timing 并产出纯数据 timeline facts；请求策略负责描述 from/target、priority、resistance、force 和 required fact id。单帧 facts 权威、transition evaluator 拆分和 Action motion output 已拆到独立 change，不在这里继续膨胀。

## What Changes
- 明确三层职责：
  - `StateTimelinePolicy`：定义当前状态生命周期内的 motion、input lock、request、cancel 和 exit window。
  - `StateRequestInterruptPolicy` / 现有 `ActionInterruptPolicy` 演进：定义某类请求从 from state 到 target state 的准入规则、最小优先级、force 和 required fact id。
  - 统一状态机 transition：只处理 accepted request fact、自然退出和普通状态图选边，不裁决请求优先级、抗性或窗口。
- 将窗口 timing 从动作局部字段收口到状态 timeline policy；具体状态迁移和帧内使用由后续小 change 接入。
- 将窗口采样结果表达为稳定 `TimelineFactId` 或等价类型化 tag；窗口 id 主要用于编辑、诊断和校验，仲裁策略优先引用 required fact id。
- 将请求准入数据从状态机条件和动作局部判断中拆出；Dodge、TurnBack、后续 Attack 复用同一套 priority/resistance/force/window facts 规则。
- 保留现有 elapsed time timing rule 作为迁移兼容，但新增状态请求准入优先使用 required fact id + `StateTimelineWindowFacts`。
- 明确 transition priority、request priority、state resistance、window min priority 和 force 的不同语义，避免配置面板里同名字段被误用。
- 明确 visual fade、clip、TransitionAsset、speed、start time 只属于动画表现配置，不能改变逻辑窗口、自然退出或打断许可。
- 第一版只收口数据模型、配置校验、现有 Dodge 策略兼容、诊断字段和测试；攻击连招与编辑器只作为后续消费者，不在本变更实现。

## Non-Goals
- 不实现轻攻击三段连招。
- 不实现完整 timeline 编辑器。
- 不新增 hitbox、hurtbox、伤害、命中停顿、VFX、SFX、IK 或相机事件轨道。
- 不新增 TurnBack、Dodge 或 Attack 专用仲裁器。
- 不让 Animancer、Animator、动画事件或 TransitionAsset 决定业务状态切换。
- 不引入 fallback 配置；缺少正式 timeline/policy 配置必须诊断失败。

## Impact
- Affected specs:
  - `state-timeline-policy`
  - `action-interrupt-arbiter`
  - `action-interrupt-policy-data`
  - `animation-phase-timeline-facts`
- Affected code:
  - `Assets/Scripts/Character/StateMachine/Model/*`
  - `Assets/Scripts/Character/StateMachine/Solver/*`
  - `Assets/Scripts/Character/Action/Model/*Interrupt*`
  - `Assets/Scripts/Character/Action/Solver/*Interrupt*`
  - `Assets/Scripts/Character/Action/FullBody/*`
  - `Assets/Scripts/Character/Movement/*`
  - `Assets/Scripts/Character/Animation/*`
  - `Assets/Configs/3C/StateMachine/*`
  - `Assets/Configs/3C/Action/*`
- Related changes:
  - Blocks `add-light-attack-combo-action` implementation until the shared window/request boundary is stable.
  - `refactor-state-timeline-facts-authority` handles single-frame current/projected/target facts usage.
  - `refactor-transition-condition-evaluators` handles evaluator extension boundaries.
  - `refactor-state-action-motion-output` handles action motion output math ownership.
