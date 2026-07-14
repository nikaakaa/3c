## MODIFIED Requirements

### Requirement: 动画 visual playback 必须来自表现帧重采样和生命周期注册表

PresentationFrame MUST 根据 Timeline logic sample history 与 InterpolationAlpha 计算 visual Timeline time并重采样 AnimationTrack。Sample MUST 进入 Registry；完整有序 handoff/owner facts MUST 进入 Arbitrator；Arbitrator MUST 为每层生成唯一 LayerPlan；LayerRuntime MUST 使用 presentation delta执行 plan并生成最终 layer outputs。该链路 MUST NOT修改 Timeline logic time、ActionWindow、Motion、root motion 或 SyncFacts。

#### Scenario: 同一 playback 跨 tick

- **WHEN** Timeline playback 在 previous/current logic sample 间保持有效
- **THEN** AnimationTrack MUST 更新同一 contribution identity 的 visual clip time
- **AND** Update plan MUST 将本帧 sample交给 LayerRuntime
- **AND** ActiveHandoff MUST 使用 presentation delta 独立推进

#### Scenario: gameplay 与 visual 分离

- **WHEN** HitWindow 已在 logic tick 产生
- **THEN** animation resampling、causal reduction、CrossFade 或 Inertialization MUST NOT重复产生该 fact

#### Scenario: Evaluate(0)

- **WHEN** Presenter 以 `Evaluate(0)` 应用已算好的 clip time
- **THEN** LayerRuntime/output job MUST 显式接收真实 presentation delta
- **AND** Evaluate(0) MUST NOT作为 handoff 时钟

#### Scenario: incoming 延迟

- **WHEN** RequireOutput 的因果链尚未形成可执行 incoming
- **THEN** Arbitrator MUST 生成 Hold plan
- **AND** visual playback MUST 继续使用 HeldOutput
- **AND** 画面 MUST NOT短暂进入 Empty 或 bind pose

### Requirement: 表现插值必须提供调试可追踪性

系统 SHOULD 暴露 previous/current logic tick、interpolation alpha、visual Timeline time、Registry contributions、ordered handoff records、causal components、record disposition、LayerPlan、FinalOutput、ActiveHandoff、layer/state weights、elapsed、duration、supersede 与错误。Inertialization debug SHOULD 暴露 LayerId、pose offset 与 velocity 摘要。Debug MUST NOT成为 gameplay/Blackboard/网络输入。

#### Scenario: 排查快速转身链

- **WHEN** 开发者查看 RunLoop -> RunEnd -> MovingTurn -> RunEnd 的表现 commit
- **THEN** debug SHOULD 显示完整 ordered records 与单一 causal component
- **AND** debug SHOULD 显示 Selected Driver、Coalesced Drivers 与最终 LayerPlan
- **AND** debug SHOULD 显示最终 Animancer layer/state weights

#### Scenario: 独立 Driver 配置错误

- **WHEN** 可见 owner 变化存在多个相同最高 authority 的独立 components
- **THEN** debug SHOULD 显示全部 Conflict provenance
- **AND** debug SHOULD 显示 Invalid plan与当前 Held/FinalOutput

### Requirement: Inertialization 必须基于最终输出姿态并保持表现层纯度

Inertialization output job MUST 位于 Animancer layer 合成后、IK/程序化姿态前。Session MUST 由 LayerId + HandoffId 标识，并从当前/前一最终 local pose 计算 velocity，对 LayerPlan DesiredCandidate pose应用衰减 offset。Job MUST NOT继续求值 source clip，也 MUST NOT产生 gameplay、motion 或 sync facts。

#### Scenario: Action 抢占重入

- **WHEN** Base Inertialization Running 时新的 HandoffPlan 到达
- **THEN** 旧 handoff MUST Superseded
- **AND** 新 handoff MUST 从当前已修正 pose/velocity capture

#### Scenario: 缺少 rig

- **WHEN** Inertialization HandoffPlan 到达但 Animator stream/native storage 无效
- **THEN** 系统 MUST 报告配置错误
- **AND** 系统 MUST NOT降级到 CrossFade 或 Idle

#### Scenario: 重叠 layer

- **WHEN** 配置要求同时对重叠骨骼运行多个 layer Inertialization
- **THEN** validator/runtime MUST 报告当前不支持
- **AND** 系统 MUST NOT猜测 per-bone 合成顺序

#### Scenario: handoff 完成

- **WHEN** elapsed 达到 duration
- **THEN** offset MUST 衰减到完成边界
- **AND** native state MUST 释放或重置

### Requirement: Animation Transition 重入必须从当前视觉结果接管

同一 LayerId 的 ActiveHandoff 完成前收到新 HandoffPlan 时，LayerRuntime MUST 从当前 FinalOutput 接管并 Supersede 旧 handoff。系统 MUST NOT让 Arbitrator 重放中间逻辑状态，MUST NOT先清空旧 output，也 MUST NOT建立 handoff stack。

#### Scenario: CrossFade 重入

- **WHEN** CrossFade Running 时新 HandoffPlan 到达
- **THEN** 新 handoff MUST 冻结当前加权 FinalOutput
- **AND** 旧 handoff MUST 在新 capture 后 Retire

#### Scenario: Inertialization 重入

- **WHEN** Inertialization Running 时新 HandoffPlan 到达
- **THEN** 新 handoff MUST 捕获当前修正后 pose/velocity
- **AND** 画面 MUST NOT跳回旧 target 基准 pose
