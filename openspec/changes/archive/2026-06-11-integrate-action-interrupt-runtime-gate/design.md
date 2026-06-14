## Context
项目已归并到统一角色逻辑状态机：`FullBody/Locomotion/*` 和 `FullBody/Action/Dodge` 在同一棵状态机里表达。动作打断仲裁模块负责 request、context、policy、priority、resistance、force 和 timing window 规则。

本 change 实施前，FullBody 运行链路没有调用 `ActionInterruptArbiter`，Dodge 入口依赖状态机 transition 的 `RequestPriorityAtLeast`。这会让“动作仲裁”和“状态机 transition 条件”形成两套动作准入规则。

本 change 实施后，默认 Dodge 动作准入已经收束为一条主线：`InputRequestBuffer -> FullBodyActionInputRequestBuilder.TryBuildDodgeRequest -> DodgeActionRequest -> FullBodyActionInterruptGate -> ActionInterruptArbiter -> accepted CharacterInputRequestFact -> unified CharacterStateMachine`。状态机只消费已被仲裁接受的动作请求事实，默认 `Locomotion/* -> Dodge` 入口不再使用 `RequestPriorityAtLeast`。

`RequestPriorityAtLeast` 仍作为迁移期通用状态机条件保留，但不属于默认 FullBody Action 准入主线；后续不能把它重新加回默认 Dodge、Attack、HitReact、Death 等动作入口来裁决 priority、resistance、force 或 timing window。

## BBB 参考对照
BBB 的参考实现使用一条 override 接管路径：
- `ActionController` 在输入触发后构造 `ActionRequest`，通过 `RequestOverride` 写入运行时仲裁上下文。
- `ActionArbitrationContext.Submit` 每帧只保留最高优先级请求。
- `ArbiterPipeline.ProcessUpdateArbiters` 调用 `ActionArbiter.Arbitrate`。
- `ActionArbiter` 读取最高优先级请求，比较当前状态 resistance；允许时写入 `RuntimeData.Override` 并切到 `OverrideState`，已经在 `OverrideState` 内时可按 priority 重新 apply。
- `OverrideState` 负责播放 full-body override 动画、阻断部分通道，并在 clip 结束后回到 return state、MoveLoop 或 Idle。
- `DodgeInterceptorSO` / `PlayerDodgeState` 是 BBB 的常规 FullBody 状态拦截和 Dodge 执行路径，和 `ActionArbiter -> OverrideState` 属于不同抽象。

3C 只借鉴 BBB 的“请求先集中仲裁，再由一个边界决定是否放行”的方向，不复制 BBB 的 `ActionArbiter` 直接 `ChangeState(OverrideState)` 模式。3C 的仲裁结果只决定是否生成统一状态机可消费的 `CharacterInputRequestFact`；状态切换、动画请求和动作位移仍由统一角色状态机及其输出边界负责。

## Goals / Non-Goals
- Goals:
  - FullBody Action 请求进入统一状态机前必须先经过 `ActionInterruptArbiter`。
  - 统一状态机只消费已被仲裁接受的动作请求事实。
  - `ActionInterruptPolicySetSO` 成为 FullBody Action 准入配置来源之一。
  - 删除默认 Dodge 路径上的 `RequestPriorityAtLeast` 直接准入规则。
  - 保持 Locomotion 四阶段不依赖动作策略集合。
- Non-Goals:
  - 不新增第二套 FullBody 状态机。
  - 不让仲裁器直接调用状态机、Animancer、Animator、CharacterController 或 Transform。
  - 不实现 Attack、HitReact、Death、cooldown、cost 或网络回滚。
  - 不修改动画播放资源和动作位移权威。

## Decisions
- Decision: 新增或收束一个 FullBody Action 请求门面，负责把输入缓冲中的 Dodge 请求转换为 `ActionInterruptRequest`，调用 `ActionInterruptArbiter`，再把 accepted decision 映射为 `CharacterInputRequestFact`。
- Decision: `CharacterStateMachineRunner` 和 transition evaluator 不调用 `ActionInterruptArbiter`，避免状态机 solver 依赖 Action solver。
- Decision: 默认动作 transition 不使用 `RequestPriorityAtLeast`。状态机 transition 只判断请求事实是否存在，优先级、抗性、force 和时间窗口在请求事实产生前完成。
- Decision: `RequestPriorityAtLeast` 允许在迁移期保留为通用条件类型，但默认 FullBody Action 入口不得使用；若没有非动作场景需要它，实施阶段可以删除该条件和对应测试。
- Decision: 策略集合配置接入 FullBody 控制器或其配置边界，不接入 Locomotion controller、movement pipeline 或 animation presenter。
- Decision: rejected decision 不生成 `CharacterInputRequestFact`，也不消费输入缓冲请求；请求保留到过期或后续合法消费。

## Risks / Trade-offs
- Risk: 没有策略集合或策略缺失会导致 Dodge 无法进入。
  - Mitigation: 提供保守默认策略或明确配置校验；自动测试覆盖缺失策略时 rejected 且请求保留。
- Risk: 仲裁 accepted 后，状态机 transition 仍可能因为配置错误无法进入目标状态。
  - Mitigation: 测试 accepted fact 能驱动默认状态机进入 Dodge，并增加配置静态检查。
- Risk: 同步删除 `RequestPriorityAtLeast` 可能影响现有统一状态机测试。
  - Mitigation: 先修改默认状态机和测试期望，再移除或降级条件 evaluator。

## Migration Plan
1. 在 FullBody 动作请求边界接入策略集合和仲裁调用。
2. 让 rejected decision 不消费输入请求，accepted decision 才生成状态机 input fact 并消费请求。
3. 移除默认状态机 Dodge 入口的 `RequestPriorityAtLeast` 条件。
4. 更新状态机静态测试，确认默认动作入口不再通过状态机优先级条件准入。
5. 保留或删除 `RequestPriorityAtLeast` 时同步测试和配置资产。

## Implementation Status
- 已新增 `FullBodyActionInterruptGate` 作为 FullBody Action 请求准入门。
- 已将 `PlayerFullBodyActionController` 接入 `ActionInterruptPolicySetSO`，由 gate 调用 `ActionInterruptArbiter`。
- 已将默认 Dodge transition 收敛为 `HasInputRequest(Dodge)`，不再叠加 `RequestPriorityAtLeast`。
- 已提供默认 Dodge interrupt policy set，并绑定到默认可琳 prefab。
- 已补自动测试覆盖 accepted、rejected、resistance、force、缺失策略、默认状态机不使用 `RequestPriorityAtLeast`、accepted 后进入 Dodge 并消费输入。

## Open Questions
- 后续如果要让当前状态动态提供 resistance，必须在 `FullBodyActionInterruptGate.CreateContext` 或等价 action context 边界接入，不能回到状态机 transition 条件里另起优先级判断。
