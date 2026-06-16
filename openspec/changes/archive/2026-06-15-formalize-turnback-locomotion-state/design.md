# 正式化 TurnBack Locomotion 状态设计

## Context

当前工程已经有 `FullBody/Locomotion/TurnBack`、`BasicMovementPhase.TurnBack`、`Locomotion.Turn.Back` 动画 alias、TurnBack intent 和 Generic root motion 采集链路。问题不再是“能不能进入 TurnBack”，而是 TurnBack 进入后没有一个明确的状态级运动权威契约。

现在的行为由几块临时拼起来：

- 状态机输出 `TurnBack` phase。
- Animancer 播 `Locomotion.Turn.Back`。
- Presenter 在 TurnBack 时打开 Animator root motion 评价并积累 delta。
- Controller 对 TurnBack 特判采样 authored RootQ 或 runtime delta。
- Motion facts 抑制输入旋转和平面位移。
- 动画退出依赖播放结束或临时 duration。

这能让角色勉强转过去，但没有解决状态级语义：转身窗口是什么、是否吃动画位移、何时交还普通移动、RootT 基线如何处理、普通输入什么时候恢复。

## Goals

- TurnBack 是统一状态机里的正式 Locomotion 状态。
- TurnBack 默认只在 `MoveLoop + Run` 时触发，语义等价于参考工程 Sprinting/TurnRun，不作为起步、停止或走路阶段的通用急转。
- TurnBack 状态声明自己的 motion authority，不再散落在 controller/presenter 特判中。
- 第一版优先修 Generic/Sandbox 手感：转身窗口内由 baked motion profile 负责根位移和转向，普通输入位移和旋转关闭，转完后立刻交回普通 MoveLoop。
- 后半段跑步尾巴不继续作为 TurnBack 运动权威。
- 保持运动唯一出口：所有动画运动贡献必须转成 `MovementCommand` 或等价 motion facts 后由 motion executor 执行。
- 为后续 Dodge、Attack、Combo 的 animation-driven 状态保留同一类 policy 模型。

## Non-Goals

- 不把 Animator Controller 作为新的状态权威。
- 不让 Animancer presenter 直接切逻辑状态。
- 不在状态机 runner 中调用 Animancer 或 CharacterController。
- 不一次性实现通用 action motion policy。
- 不要求第一版清理所有历史 TurnBack 调试日志。

## Decisions

### Decision: TurnBack 状态拥有 motion policy

`FullBody/Locomotion/TurnBack` 的状态输出需要能声明：

- 动画 alias：默认 `Locomotion.Turn.Back`。
- 入口范围：默认只允许 `MoveLoop + Run`，其它 phase/gait 不直接进入。
- 转身目标：进入时锁定的世界方向或目标 yaw。
- 输入抑制：TurnBack 活跃期间关闭普通输入旋转和普通输入平面位移。
- 旋转来源：默认使用 baked motion profile 的 yaw 曲线；如果 profile 缺失，才通过统一 policy fallback 到 authored yaw 或 runtime delta，而不是散在 controller 临时判断。
- 位移来源：默认使用 baked motion profile 的转身窗口位移；不消费 TurnBack 动画后半段跑步尾巴，转完后回普通 MoveLoop 处理移动。
- 退出窗口：使用配置的 `turnCompleteNormalizedTime` 或等价 animation marker，而不是必须等整段动画播完。
- 进入时间：允许配置 fade、start normalized time、最小锁定时间和普通输入恢复时机。
- 烘焙数据：允许引用后续由编辑器生成的 yaw/translation 曲线、root motion sample、marker 和诊断元数据。

### Decision: 第一版默认使用 baked profile 驱动 TurnBack 根运动

当前 TurnBack 动画包含“转身 + 混回跑步”的内容，且 RootT 存在非零基线和尾巴位移。为了先得到可控手感，第一版默认策略是：

- 使用转身窗口内烘焙出的 yaw 贡献完成约 180 度朝向变化。
- 使用转身窗口内烘焙出的 local planar delta 驱动角色根位移。
- 视觉 TurnBack clip 可以清掉 root 平面位移，避免子物体表现层漂移；根运动仍来自 baked profile。
- 不把 TurnBack 动画后半段跑步尾巴作为 TurnBack 的平面位移权威。
- 到转身完成点后立刻退出到 MoveLoop/Idle，由普通移动重新接管速度和位移。

如果后续要做更强动画驱动，可新增 `AnimatorRootMotionFull` 或 `AnimatorRootMotionWindowed` policy，但必须仍走同一 motion facts/motion executor 出口。

### Decision: 烘焙数据是长期入口，不是被废弃

本变更反对的是把 RootQ/RootT 采样散落在 controller 临时补丁里，不反对烘焙数据本身。TurnBack 第一版就使用 baked motion profile；后续 Dodge、Attack、Combo 也可能需要编辑器烘焙运动曲线和窗口数据。第一版需要保留数据边界：

- `motionPolicy` 可以引用 baked motion profile 或等价资产。
- baked 数据只保存纯数据曲线、marker、窗口和校验摘要。
- 运行时 sampler 只消费 baked facts，不读取编辑器 API。
- 编辑器可以后续提供 clip 预览、root 曲线提取、turn complete marker、entry/exit window 和警告。

### Decision: 动画进入/退出时间属于状态策略

TurnBack 的手感不只由转 180 度决定，还由进入 fade、起播点、输入锁定窗口、可退出时间和回接 MoveLoop 的时机决定。这些时间必须进入状态 policy 或动画 phase config，而不是写死在某个 presenter 或日志调试代码中。

### Decision: 视觉 clip 可以清位移，但不把删除源曲线作为修复手段

Animation 窗口看到的 RootT.z 偏移可能是 clip 根轨迹基线，不等同于本帧 gameplay 位移。直接手工删除源 RootT/RootQ 或 skeleton 根位移曲线会改变 Unity root stream 的参考基线，容易造成跳闪。第一版使用工具生成可播放的 TurnOnly 视觉 clip，同时从原始 rootmotion clip 烘焙 motion profile；运行时用状态 motion policy 决定消费哪份纯数据。

### Decision: 参考工程只作为行为模型

参考工程的关键点是：

- Sprint 中检测人物朝向和输入目标方向夹角。
- 进入 ReturnRun/TurnRun 状态。
- TurnRun 前段不执行普通代码旋转。
- `OnAnimatorMove` 通过 Animator root motion 和 CharacterController 统一处理。
- 动画事件或状态退出回 Sprint/Idle。

当前工程保留自己的统一状态机、Animancer facade、motion executor 和 rollback 边界，不直接复制参考工程 Animator Controller。

### Decision: 默认只从 RunLoop 触发 TurnBack

TurnBack 动画语义是高速移动反向急转，不是起步修正、停止打断或 Walk 转向。默认入口必须收窄到 `FullBody/Locomotion/MoveLoop` 且当前 gait 为 Run：

- `Idle` 不直接进入 TurnBack。
- `MoveStart` 不直接进入 TurnBack。
- `MoveStop` 不直接进入 TurnBack。
- `MoveLoop + Walk` 不直接进入 TurnBack。
- `MoveLoop + Run` 且 TurnBack intent 有效时进入 TurnBack。

如果后续需要 Walk pivot、起步反向修正或停止反向打断，应作为新的 animation-driven locomotion transition 规划，不能复用 Run TurnBack 动画偷偷覆盖。

## Proposed Runtime Shape

```text
LocomotionDecisionFacts.TurnBackIntent
-> CharacterStateMachine transition
-> FullBody/Locomotion/TurnBack
-> TurnBackStateMotionPolicy
-> MovementCommand(animation yaw, optional animation planar delta, suppress input)
-> motion executor
-> Animancer presenter plays Locomotion.Turn.Back and reports progress/root motion facts
-> turnCompleteNormalizedTime reached
-> MoveLoop or Idle
```

## Risks / Trade-offs

- 风险：baked profile 的 local delta 与视觉脚底姿态不完全一致，可能仍有少量滑步。
  - Mitigation: 第一版只烘转身窗口，转完即回普通 MoveLoop；后续可在编辑器中调整窗口和曲线。
- 风险：退出点太早导致视觉切换硬。
  - Mitigation: 退出点配置化，并在 Sandbox 手测调整；默认参考已观测到的 180 度完成点。
- 风险：motion policy 变成 TurnBack 专用枚举。
  - Mitigation: 类型命名和接口面向 animation-driven movement，TurnBack 是第一个使用者。
- 风险：第一版不实现完整编辑器，后续数据入口又被代码写死。
  - Mitigation: 第一版就把 policy、baked 数据引用和窗口字段建成纯数据，编辑器只作为 authoring adapter。
- 风险：与 `refactor-locomotion-decision-pipeline` 重叠。
  - Mitigation: 本变更只补状态权威和 motion policy，不重做 intent 派生阶段；同时明确覆盖旧提案中 MoveStart/MoveStop 可进入 TurnBack 的宽入口。

## Open Questions

- 最终 TurnBack clip 是否继续使用原始 rootmotion 动画，还是由编辑器工具生成 turn-only clip？第一版运行时不依赖用户必须切 clip。
- `turnCompleteNormalizedTime` 默认值是否取 0.40、0.47 或由配置资产显式填写？实现时先从当前资源诊断值设置默认，并允许 Inspector 调整。
- 编辑器第一版是只做 bake/validate 按钮，还是同时做曲线预览和 marker 拖拽？本变更只要求运行时数据边界预留。
