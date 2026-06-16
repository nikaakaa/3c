# 预测回滚权威域与比较域设计

## Context

当前角色控制器正在从本地动作 demo 走向预测、回滚和未来网络校正。最近的 TurnBack 调试已经说明：如果动画播放时间会驱动 profile 位移/yaw，它就不是纯表现层；如果 WalkLoop 或 Action 动画 normalized time 只是视觉进度，它不应该让 F6 strict replay fail。

现有代码已经出现 `differences` 与 `presentationDifferences` 的雏形，但 strict 判断仍依赖局部规则，例如 TurnBack alias 硬编码。这个变更要把雏形升级成底层协议：状态、动画、运动来源、黑板 facts 和 snapshot comparer 都能按同一套权威域声明工作。

## Goals

- 给角色控制器机架建立可复用的预测回滚权威矩阵。
- 让业务后续能按状态声明 strict、predictive、presentation 或 ignored，而不是改 comparer。
- 保证 TurnBack 这种 profile-driven motion 仍严格回滚。
- 允许 MOBA/MMO 风格的视觉动画 drift 只做诊断，不阻塞 gameplay replay。
- 为未来 Dodge、Attack、技能窗口、hitbox、cancel window 进入 tick timeline 留出正式入口。
- 保持一条 FullBody/Locomotion/motion executor 主线。

## Non-Goals

- 不决定最终游戏类型是格斗、MOBA 还是 MMO。
- 不要求所有动画使用定点数。
- 不要求所有表现层动画进 simulation tick。
- 不在本变更中实现完整技能系统或攻击判定窗口。
- 不新增 Animator root motion 作为 strict rollback 的默认权威。

## Decisions

### Decision: 权威域描述“谁说了算”

每个会进入状态、黑板、snapshot 或 motion 的事实，都必须能归入一个权威域：

- `VisualOnly`：只负责表现，允许 drift。
- `LogicTimed`：逻辑由 simulation tick/timeline 掌权，动画跟随。
- `ProfileDriven`：动画 profile 的播放窗口驱动 gameplay 位移/yaw 或其它逻辑事实，必须 strict。
- `AnimatorRuntimeDirect`：运行时直接消费 Animator delta，适合非 strict 或弱预测模式，不作为强回滚默认路径。

### Decision: 比较域描述“不一致是否失败”

回滚比较结果必须拆成至少两组：

- `StrictGameplay`：不一致即 F6/F8 fail。
- `PredictiveGameplay`：允许本地预测后校正，但必须记录和可度量。
- `PresentationDrift`：只做诊断，不导致 strict replay 失败。
- `Ignored`：不进入比较。

`PresentationDrift` 不是删除信息。日志仍应保留 first drift tick、字段名、expected/actual 摘要，方便视觉校正调试。

### Decision: 状态策略是分类入口，不是 comparer 硬编码

TurnBack、MoveLoop、Dodge、Attack 的分类应来自状态 policy、动画 policy、motion source policy 或等价的纯数据表。Comparer 可以有默认保守规则，但不能长期靠 `alias == Locomotion.Turn.Back` 作为唯一 strict 判断。

初始权威矩阵建议：

| 状态/事实 | 动画权威 | 运动权威 | 比较域 |
| --- | --- | --- | --- |
| root position/yaw | LogicTimed | MotionExecutor | StrictGameplay |
| FullBody active state/state time | LogicTimed | StateMachine | StrictGameplay |
| TurnBack playback window | ProfileDriven | AnimationProfile | StrictGameplay |
| TurnBack profile delta/yaw | ProfileDriven | AnimationProfile | StrictGameplay |
| MoveLoop normalized time | VisualOnly | KinematicInput | PresentationDrift |
| Action animation normalized time | VisualOnly 或 LogicTimed | StateTimeline | PresentationDrift，直到业务声明 strict |
| Dodge 逻辑窗口 | LogicTimed | StateTimeline | StrictGameplay |
| Attack hit/cancel/recovery | LogicTimed | StateTimeline | StrictGameplay |
| Animancer blend weight | VisualOnly | Presentation | PresentationDrift 或 Ignored |

### Decision: Simulation 输出播放命令，Presenter 只适配

对于 strict 或 logic-timed 状态，simulation 应输出纯数据 playback command 或 timeline facts；Presenter 只执行 play/seek/blend。Presenter 可以提供诊断性的当前播放状态，但不得反向覆盖 strict gameplay facts。

### Decision: UE 类似做法作为参考，不照搬 API

UE 常见模式是 `CharacterMovement`、capsule 和 Ability/Gameplay 逻辑掌权，AnimBP 多数跟随；只有 Root Motion Montage/Root Motion Source 等正式声明的动画驱动运动才进入移动权威。当前项目应采用相同原则：动画可以成为权威，但必须被声明、快照化、可回放，而不是表现层自由状态意外进入 gameplay。

## Risks / Trade-offs

- 风险：比较域被滥用，把真实 gameplay mismatch 标成表现漂移。
  - Mitigation: 默认保守；影响 position/yaw、状态机、动作 facts、profile playback window 的字段必须 strict。
- 风险：短期内需要兼容现有 hardcoded TurnBack 规则。
  - Mitigation: 允许第一步保留兼容映射，但任务必须包含迁移到 policy/table 的检查项。
- 风险：Action 动画现在既被逻辑读取又像表现层。
  - Mitigation: 本变更先要求分类；未来 Action Timeline/window 进入 `LogicTimed` 后再把对应 facts 标 strict。
- 风险：过早抽象导致实现复杂。
  - Mitigation: 第一阶段只实现 enum/metadata/comparer scope，不重写状态机和 motion source。

## Migration Plan

1. 建立 authority/scope 枚举和纯数据 policy。
2. 给现有快照比较建立字段级默认分类。
3. 把当前 TurnBack strict 判断迁移为 policy 或表驱动的 `ProfileDriven + StrictGameplay`。
4. 将 MoveLoop 和 Action animation normalized time 归入 `PresentationDrift`，但保留日志。
5. 为 Dodge/Attack 的未来 `LogicTimed` 入口预留状态 timeline/window 分类。
6. 用 EditMode 测试锁定 strict mismatch、presentation drift、first drift、F6/F8 pass/fail 语义。
7. Play Mode 手动验证 TurnBack strict 仍能抓错，WalkLoop/Action 视觉 drift 不导致 strict 失败。

## Open Questions

- 第一阶段 authority/scope 是否放在 snapshot comparer 配置表，还是直接放在 state/animation policy asset 上；建议先做纯代码默认表，后续再接 SO。
- `PredictiveGameplay` 在本地工具里是否先只记录不 fail，还是提供单独 warning gate；建议第一版只记录，不影响 strict pass。
- Action/Dodge 的 animation normalized time 是否已有业务窗口依赖；若有，后续应迁入 `LogicTimed` timeline，而不是继续读取 Animancer time。
