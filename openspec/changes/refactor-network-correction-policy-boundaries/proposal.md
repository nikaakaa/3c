# Change: 拆分角色网络校正策略边界

## Why

当前 `ActionCorrectionPolicy` 同时表示三类互不等价的事情：动作被服务端拒绝后的生命周期结果、MotionStage 对逻辑位姿误差的应用方式，以及表现层是否立即贴合。该字段又同时存在于 `ActionProfile`、`ActionMotionPolicy`、`GameplayBehaviorProfile` 和 `ActionContext`，但实际 MotionStage 完全不读取这些配置，而是使用自身硬编码阈值；`MotionCorrectionAck` 是否发送反而由这个表现含义不明的字段决定。

这导致 Inspector 展示的配置不等于运行时行为，也让后续 owner prediction/reconciliation、远端 snapshot interpolation 和表现平滑无法作为独立能力设计。继续在该枚举上增加选项只会制造更多无效组合，因此必须先把现有混杂模型删干净。

## What Changes

- **BREAKING** 删除 `ActionCorrectionPolicy`，并删除 `ActionProfile`、`ActionMotionPolicy`、`GameplayBehaviorProfile`、`ActionContext`、模板、Inspector 和 Corin 资产中的全部 `CorrectionPolicy` 字段；不保留兼容字段、别名或迁移读取路径。
- 将动作网络结果收口为 `ActionLifecycleTransition`：`Reject` 固定为 terminal，`Correct` 固定为 non-terminal，除非 incoming decision 明确携带 terminal 语义；动作配置不再选择平滑、强制或拒绝处理方式。
- 删除 `ActionMotionSourceType.Correction` 以及 `ActionMotionSample`、`GameplayActionMotionDigest` 中始终为零的 `CorrectionId`。Action motion outgoing 只由 motion source 的 prediction 语义和 ActionProfile 的 authority/replication 语义解析。
- 保持 `CharacterMotionStage` 当前数值行为不变，但不再把当前“部分应用误差/完整应用误差”算法伪装成 Action、Behavior 或 Pipeline authoring policy。本 change 不新增纠偏算法配置面。
- 新增正式 `MotionCorrectionApplicationResult`，客观记录本 tick 是否应用、部分或完整应用了多少误差；Presentation 只消费该结果，不再从 motion debug 数据读取运行决策。
- 将 incoming `Correction` 与 outgoing `MotionCorrectionAcknowledgement` 拆成两个运行时合同，并将 `GameplayMotionCorrection` 与 `GameplayMotionCorrectionAcknowledgement` 拆成两个 packet payload。Ack 只在校正确实应用后产生，并继续通过正式 `SyncFactBehaviorBinding` 和 Stream behavior policy 路由。
- 更新 Action、Behavior、Motion、Presentation、SyncFact binding 的规范口径与调试摘要，删除“动作 correction policy”“拒绝纠偏”“smooth/force action motion”等混合表述。

本 change 不实现 input history restore/replay、远端 snapshot interpolation、VisualRoot 误差衰减、Fantasy peer、服务端裁决、可切换网络角色或公开的 correction tuning。这些能力必须在本次边界清理完成后分别设计，不能把当前直接纠偏算法配置化后冒充最终 reconciliation。

## Impact

- 受影响规范：`character-network-correction-boundaries`、`gameplay-behavior-policy-model`、`character-action-network-policy-authoring`、`character-action-authoring-closure`、`character-action-instance-runtime`、`character-motion-semantics`、`character-presentation-interpolation`、`character-syncfact-behavior-binding`。
- 受影响代码：Character Action profile/context/resolver、Gameplay Behavior profile/resolver、MotionStage 与 motion output/debug、PresentationInterpolator、Character/GameplaySync packet 和 adapter、相关 Inspector。
- 受影响资产：Corin ActionProfile 和 GameplayBehaviorProfile 资产中的旧序列化字段；Corin PipelineDefinition 不新增 correction 算法配置。
- 行为目标：保持现有小误差部分应用/大误差完整应用算法的数值行为，但让运行结果、Ack 和动作生命周期各自只有一个明确归属；不宣称本 change 已完成真正的 owner replay reconciliation，也不把当前算法升级为正式作者能力。
