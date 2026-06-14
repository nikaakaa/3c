# 动画运动源采样管线设计

## Context

当前角色运动主线已经具备 simulation tick、FullBody pipeline、统一状态机输出、motion executor 和 Animancer 外观层。这个结构适合把角色根运动权威放在 simulation/motion executor 一侧。

TurnBack 调试暴露的问题是：`BasicLocomotionAnimancerPresenter.OnAnimatorMove` 在 Unity Animator evaluation 阶段产生 root delta，而 simulation tick 在另一个节奏构建 movement facts。把 `OnAnimatorMove` delta 放进 pending buffer 再由 tick 拉取消费，会天然出现空消费、延迟消费、同帧多 tick 重复消费或 rollback 状态不一致。

考虑到项目后续需要客户端预测、回滚、预测矫正和可测试动画混合，本变更不实现 Animator runtime direct 兼容模式。第一版只保留 Simulation 权威路线：动画运动先变成运行时 profile 或等价 tick 可采样数据，再由 simulation tick 按播放窗口采样。

## Goals

- 提供状态可选的通用动画运动源能力，而不是 TurnBack 专用特判。
- `TickSampledMotion` MUST 与 simulation tick 的播放进度窗口对齐，并以 movement facts 进入 motion executor。
- `OnAnimatorMove` pending delta MUST 不再被 simulation tick 拉取消费。
- Presenter MUST 不再暴露 pending runtime root delta source 或 rollback provider。
- TurnBack 默认使用 `TickSampledMotion` profile 采样作为权威运动路线。
- Generic rootmotion 原动画 MUST 被视为运动母带；profile 和 cleaned in-place visual clip 均从该母带派生。
- 保留现有状态机进入、input lock、motion window、exit window 和诊断能力。

## Non-Goals

- 不实现 `AnimatorRuntimeDirect`。
- 不新增 root motion sink。
- 不把 Animator Controller 或 Animancer state 变成状态机权威。
- 不由表现层直接写角色根 Transform。
- 不一次性设计网络 DTO、服务端同步或远端插值。
- 不删除现有 runtime root delta 诊断日志。
- 不让缺失动画运动数据时自动 fallback 到未声明来源。

## Decisions

### Decision: 动画运动源是纯数据状态输出

状态输出可以声明动画运动源策略。策略至少表达：

- 是否启用动画运动贡献。
- yaw source。
- translation source。
- source alias/profile id。
- motion window 外是否丢弃尾部 delta。
- 普通输入旋转和平面位移是否被抑制。

这些字段必须是纯数据，不携带 Animator、AnimancerState、Transform 或 CharacterController 引用。

### Decision: TickSampledMotion 是第一版唯一正式路线

`TickSampledMotion` 模式采用播放窗口采样：

- simulation tick 推进或读取动画播放进度。
- 构建 `previousNormalizedTime -> currentNormalizedTime` 的采样窗口。
- sampler 根据窗口输出本 tick 的 local/world planar delta 和 yaw delta。
- motion executor 在 ExecuteMotion 阶段统一应用。

实现复用现有 `AnimationMotionPlaybackWindow`、`LocomotionMotionProfileSO` 和 motion profile sampler。该模式是后续需要预测、回滚或服务端复现状态的默认权威路线。

### Decision: Generic rootmotion 母带派生两类运行时资产

Generic rootmotion 原动画是运动母带。系统 MUST 从该母带派生两类资产：

- runtime motion profile：供 `TickSampledMotion` 在 simulation tick 内采样 yaw/translation。
- cleaned in-place visual clip：清掉 root XZ 位移和 root yaw，仅供 Animancer/Animator 视觉播放。

同一状态不得同时使用 rootmotion 原动画的 Animator delta 和 motion profile 推动角色根。TurnBack 在 `TickSampledMotion` 默认方案下 MUST 播放 cleaned in-place visual clip，并由 profile 贡献权威运动。

### Decision: 运行时 clip root curve 需要运行时可用数据

之前实验过的 `AnimationClipRootMotionSampler` 依赖 `UnityEditor.AnimationUtility` 读取 `RootT/RootQ`，而且 source 来自 Presenter 当前 `AnimancerState.Clip`。这会造成 Editor 测试可采样、Player 运行时不可采样，以及表现层当前 clip 反向影响运动权威的问题。

第一版不保留这条 authored root motion 入口。如果要从未裁剪原生动画使用 root motion，必须在导入、配置或 editor 工具阶段把 root curve/profile 转为运行时可序列化数据，再由 simulation tick 通过配置的 motion profile 采样。

### Decision: Animator runtime delta 不作为配置路线

`OnAnimatorMove` 读取的 `Animator.deltaPosition/deltaRotation` 属于 Unity Animator evaluation。第一版不把它作为正式运动输入，也不缓存到 pending buffer，不写入 rollback state，也不提供可配置 source。

### Decision: 不做静默 fallback

状态声明了某个动画运动源后，缺失数据必须诊断并输出无贡献或配置错误。系统不得静默 fallback 到其它 source，也不得恢复普通输入移动来掩盖配置问题。

## Risks / Trade-offs

- 风险：暂不支持只能通过 Animator runtime root motion 表现的动画。
  - Mitigation: 先以预测/回滚主线为准；这类动画需要先生成 motion profile 与 cleaned in-place visual clip。
- 风险：从 clip 生成运行时 motion profile 需要 editor 工具或资产迁移。
  - Mitigation: 第一版复用已有 motion profile 数据结构，后续再评估工具化。
- 风险：采样曲线和视觉动画播放进度不一致。
  - Mitigation: 自动测试覆盖播放窗口重置，手动验证检查动画名、normalized time、motion delta 和角色根实际变化。

## Migration Plan

1. 保留现有 TurnBack request、状态图、timeline window 和诊断日志。
2. 将现有 TurnBack motion policy 固定到 profile source。
3. 修正 Generic TurnBack rootmotion 母带到 motion profile 的烘焙和验证链路。
4. 从同一 Generic rootmotion 母带生成 cleaned in-place visual clip。
5. 将 TurnBack source 从 Animator pending delta 消费迁移到 `TickSampledMotion` profile 采样。
6. 删除 Presenter pending runtime root delta source 和 rollback provider。
7. 删除 Presenter 当前 clip authored root motion source、delta 类型和采样器。
8. 添加自动测试覆盖 source 选择、采样窗口、无 fallback、motion executor 应用和 Presenter 不暴露 pending delta。
9. 在 Sandbox 中手动验证 TurnBack 稳定触发、稳定转身、退出正常。

## Open Questions

- Generic TurnBack 母带应固定使用哪个 clip 和 root path 作为正式烘焙来源？
- 后续 Dodge/Attack 是否共用同一 policy 字段，还是在 FullBody action 输出中添加单独的 action motion policy？
