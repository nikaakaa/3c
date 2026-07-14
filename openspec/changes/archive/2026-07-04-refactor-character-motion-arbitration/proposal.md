# Proposal: 重构角色 Motion 仲裁规则

## Why

当前角色 motion 链路已经有 `MotionContribution -> MotionResolver -> MotionIntent -> MotionModifier -> CharacterController.Move` 的形状，但仲裁语义还没有成立：

- `MotionContribution` 已有 `Priority`，但 `MotionResolver` 仍然把 base intent 和所有 contribution 简单相加，`Priority` 没有业务意义。
- 输入移动通过 `MotionIntent` 直接写入结果，Timeline root motion 通过 `MotionContribution` 提交，二者不是同一种来源模型。
- Timeline root motion、动作位移、受击击退、支援防守、网络 correction 同帧出现时，系统没有正式规则说明谁覆盖谁、谁叠加谁、谁可以吃掉低层移动。
- 网络 correction 目前在 resolver 前直接 `SetPositionAndRotation`，这会绕过动作手感和 MotionStage 的可解释链路。
- `character-root-motion-curves` 已经要求多来源通过正式 motion 仲裁规则生成最终 `MotionIntent`，但当前 `character-motion-semantics` 只定义了 Contribution/Intent/Modifier/Result，没有定义仲裁规则本身。

动作 demo 的业务目标不是做任意公式编辑器，而是让第三人称动作角色在输入移动、攻击 root motion、闪避、受击、防守、网络纠偏之间有可调、可调试、可解释的有限规则。

## What Changes

本变更规划一套有限层级的 motion 仲裁模型：

- `MotionContribution` 必须携带正式仲裁语义，例如 channel、blend mode、priority、weight、source type、是否消费低层 channel。
- 输入移动不再作为特殊 base intent 绕过 contribution 模型；locomotion 输入应作为 `Locomotion` channel 的 contribution 进入 resolver。
- Timeline root motion 应作为 `Action` channel 的 contribution，继续由 Timeline 采样，但最终位移由 MotionStage 仲裁。
- 受击、击退、强制位移等 gameplay result 应作为高于 action 的 `GameplayResult` channel 进入仲裁。
- 网络 correction 不得在 resolver 前硬改 Transform，必须进入 MotionStage 的正式 correction phase，并按策略 smooth 或 force。
- MotionWarp 保持 Move 前 modifier，不被伪装成普通 contribution；它只修正已仲裁出的 gameplay intent。
- `MotionResolver` 使用固定 channel 顺序和固定 blend/override 规则，不做动态公式解释器或插件注册表。
- Runtime debug 需要能暴露本帧参与仲裁的来源、最终采用的 channel/source 和 modifier/correction 结果。

## Non-Goals

- 不做通用数学公式编辑器。
- 不做完整 locomotion 状态机、完整连招树或完整动作库。
- 不做编译导出/runtime data 压缩。
- 不做完整 rollback/replay。
- 不实现真实 Fantasy 服务端裁决。
- 不恢复旧 locomotion SO、ActionSO、footphase profile、bodyclaim policy 或 BBB motion 配置。
- 不新增并行 Workbench、并行 port registry 或旧路径兼容。

## 当前代码事实

- `CharacterPipeline.LogicTick` 顺序是 network receive、action resolve、input、BTSMTL、motion、network send。
- `SetMotionIntentFromInputNode` 直接写 `StrictGameplay.MotionIntent`。
- `TimelinePlaybackScheduler` 采样 root motion 后写 `StrictGameplay.MotionContributions`，同时提交 `ActionMotionSample(RootMotion)`。
- `MotionResolver` 当前把 base intent 和所有 contribution 加权相加，没有使用 `Priority`。
- `CharacterMotionStage.ApplyNetworkCorrections` 当前在 resolver 前直接 `SetPositionAndRotation`。
- `MotionWarpModifier` 已经是 Move 前 modifier，适合作为 action/root motion 之后的贴目标修正阶段。

## 决策和 Tradeoff

### 方案 A：继续所有 motion 简单相加

- 优点：实现最少，当前代码几乎不用动。
- 缺点：攻击 root motion、输入移动、击退和 correction 会互相污染；`Priority` 字段没有意义；调试时无法说明为什么角色这一帧移动成这样。
- 业务取舍：不适合求职向动作 demo，因为面试官会看到动作手感不稳定，攻击、闪避、受击缺少明确优先级。

### 方案 B：把所有东西都做成 modifier

- 优点：顺序很直接，全部后处理。
- 缺点：会丢失来源语义；root motion、击退、网络 correction 全都像“修正”，很难表达动作本体位移和受击强制位移的区别。
- 业务取舍：短期能跑，但后续做 action profile、网络策略、debug 面板时会继续返工。

### 方案 C：有限 channel 仲裁 + 固定 modifier/correction 顺序

- 优点：输入、动作、受击、网络纠偏各自有业务层级；规则固定、可调试、可复用到状态机和 Timeline；不用给作者暴露任意公式。
- 缺点：需要重构 `MotionContribution`、`MotionResolver`、输入节点、Timeline root motion 提交和 correction 应用。
- 业务取舍：最贴合“动作丝滑 + 网络压力场景可解释”的 demo 目标。

本 proposal 选择方案 C。

## 与现有 Spec 的关系

- `character-motion-semantics` 已经定义 Contribution、Intent、Modifier、Result，本变更补上 Contribution 到 Intent 的正式仲裁规则。
- `character-root-motion-curves` 已经要求多 motion 来源通过正式 motion 仲裁规则生成最终 `MotionIntent`，本变更实现该规则口径。
- `character-pipeline-runtime` 已要求 Timeline 和节点不能直接结算 Transform，本变更继续保持 `CharacterMotionStage` 是唯一移动边界。
- `character-action-network-policy-authoring` 已要求 motion 策略按 action profile 和 source type 集中解析，本变更让 motion source type 能映射到 channel 和 correction phase。
- `separate-sync-facts-from-network-output` 已将 `NetworkOutput` 正式收敛为 `SyncFacts`。本变更实现时必须接入 `SyncFacts.Motion`，不得恢复旧 `NetworkOutput` 命名。
