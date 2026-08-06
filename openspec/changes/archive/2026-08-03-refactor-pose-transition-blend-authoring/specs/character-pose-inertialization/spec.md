## MODIFIED Requirements

### Requirement: Inertialization Policy必须完整覆盖直接Player endpoint pair

每个Inertialization节点 MUST拥有唯一node response配置，完整声明可达Pose Parameter的`Inertialize | Snap`与残差处理设置。仅当节点直接上游是没有transition owner的Player时，节点 MUST额外引用唯一exact temporal policy；Compiler MUST枚举该Player全部可达endpoint pair，并把authoring default与override物化为完整`HardCut | Inertialize` exact table。Direct Player Inertialize rule MUST包含duration、Blend Mode或Custom Curve Asset与dense per-bone Blend Profile。节点直接上游为PoseStateMachine或AnimationSlot时，duration、canonical curve与dense Blend Profile MUST来自上游exact transition owner，node response配置 MUST不复制或覆盖它们。Runtime缺少唯一owner、存在重复owner或缺少pair MUST失败且不得fallback。

#### Scenario: 可达Direct Player pair缺失

- **WHEN** 某个直接Player endpoint pair无法物化exact temporal rule
- **THEN** Compiler MUST失败并定位Inertialization PoseNodeId与pair

#### Scenario: StateMachine惯性化

- **WHEN** Inertialization节点直接连接选择Inertialization的PoseStateMachine
- **THEN** 节点 MUST从对应compiled edge rule读取duration、curve与Blend Profile
- **AND** node response配置 MUST只提供Parameter与残差响应设置

#### Scenario: StateMachine后节点保留旧temporal default

- **WHEN** StateMachine edge与下游Inertialization policy同时为同一事件声明duration或curve
- **THEN** Validator MUST拒绝该双owner拓扑
- **AND** MUST不沿用旧Policy作为隐藏覆盖或fallback
