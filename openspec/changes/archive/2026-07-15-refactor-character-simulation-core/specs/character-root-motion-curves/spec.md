# character-root-motion-curves Specification

## MODIFIED Requirements

### Requirement: RootMotionCurveAsset 与 Timeline 内联位移必须保持单向边界

RootMotionCurveAsset MUST继续作为动画派生累计曲线 authoring source；Compiler MUST将 Timeline 正式引用的曲线编译为 portable Program constants。Runtime MUST只读取 compiled constants，MUST不同时读取 RootMotionCurveAsset 与另一份 inline runtime curve。

#### Scenario: 编译 Dodge 曲线

- **WHEN** Dodge Timeline 引用 RootMotionCurveAsset
- **THEN** Compiler MUST生成唯一 portable curve constant
- **AND** Kernel MUST不读取 Unity AnimationCurve asset

### Requirement: Timeline 不得通过动画片段直接提交 Root Motion

Compiled Timeline MUST只通过正式 MotionCurve operation 产生 MotionContribution。AnimationClip、Animancer state、fade 与 sampled pose MUST不进入 WorldRequest 或 WorldSimulationState。

#### Scenario: Attack 动画包含 Root Transform

- **WHEN** Presentation 播放带 Root Transform 的动画片段
- **THEN** 逻辑位移 MUST仍只来自 compiled MotionCurve

### Requirement: Root Motion 通过角色 motion 管线应用

Root Motion curve delta MUST进入 Kernel Evaluate 的统一 contribution resolve，生成 portable WorldRequest，再由 Session WorldSolver batch 产生 actual body result。曲线 MUST不直接写 Transform 或调用 CharacterController。

#### Scenario: Root Motion 被墙阻挡

- **WHEN** compiled curve 请求的位移穿过墙面
- **THEN** WorldSolver actual result MUST决定 WorldSimulationState

### Requirement: 动画表现淡入淡出不得成为 Root Motion 路径

Animancer fade、animation state weight、Presentation retention 和 visual Timeline sample MUST不改变 compiled MotionCurve contribution、WorldRequest 或 Character/World state。Gameplay 位移权重只能来自 Program authoring 规则。

#### Scenario: 攻击动画淡出

- **WHEN** Attack animation 仍在 Outgoing fade
- **THEN** Presentation MAY继续采样 pose
- **AND** MUST不继续产生 Gameplay Root Motion
