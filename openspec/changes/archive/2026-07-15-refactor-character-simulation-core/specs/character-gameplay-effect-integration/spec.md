# character-gameplay-effect-integration Specification

## MODIFIED Requirements

### Requirement: CharacterPipeline 必须唯一持有 Gameplay Effect Adapter

CharacterSimulationProgram MUST唯一包含编译后的 GameplayEffect catalog/operations，CharacterSimulationState MUST唯一保存当前 Actor 的 Tag、Attribute、ActiveEffect 与 journal。Runtime Host、Committer 和 Presentation MUST不持有第二个 GameplayEffectRuntime state；Unity authoring adapter MUST只参与 Program 编译。

#### Scenario: 创建 Corin Local Session

- **WHEN** Host 加载 Corin Program
- **THEN** GE state MUST按 Program layout 创建在 CharacterSimulationState
- **AND** MUST不再创建 CharacterGameplayEffectAdapter-owned runtime object

### Requirement: Character ActorId 必须由 Character 实例唯一拥有

Simulation Session roster MUST为每个 CharacterSimulationState 指定唯一 ActorId，并将同一 identity 用于 SimulationIngress、GameplayEffect context、typed facts、world request 和 EventId。Host、Graph、Effect operation 和 Model adapter MUST不各自生成不同 ActorId。

#### Scenario: Effect 应用于 Self

- **WHEN** compiled operation 对 Self 提交 Effect request
- **THEN** source/target ActorId MUST来自当前 roster binding

### Requirement: Character Gameplay Effect Adapter 必须保持薄翻译边界

Unity authoring compiler adapter MUST只把 CharacterGameplayEffectProfile、EffectDefinition、Tag 与 Attribute 配置翻译为 portable Program catalog。运行时 model input adapter MUST只把 typed SimulationIngress 映射到已编译 operation input。任何 adapter MUST不重新实现 stacking、duration、period、magnitude 或 transaction 规则。

#### Scenario: 编译 EffectDefinition

- **WHEN** Compiler 处理 Corin GameplayEffect profile
- **THEN** adapter MUST生成 portable catalog entry
- **AND** runtime 规则 MUST由同一 GameplayEffect operation 执行

### Requirement: Gameplay Effect 必须进入角色固定逻辑 tick 的正式顺序

每个 SimulationTick MUST在 Evaluate 开始时按稳定顺序应用当前 Actor 的 typed SimulationIngress、开始 GameplayEffect transaction、推进到期/周期/抑制，再处理 control input、BTSMTL、Action 与 Timeline operation；Finalize MUST在 world result 应用后 drain 当前 Tick 唯一 ChangeSet 并输出 Effect、Attribute、Cue 与 Trace facts。PresentationFrame MUST不推进 duration、period 或修改 Effect state。

#### Scenario: 同 Tick 收到眩晕结果

- **WHEN** Driver 在 Tick plan 中提交对本 Actor 的合法 GameplayResult ingress
- **THEN** Evaluate MUST在 Graph decision 前应用对应 Effect 与 granted tag
- **AND** 同 Tick Program operation MUST能读取该 tag

#### Scenario: 单个 RenderFrame 执行多个逻辑 Tick

- **WHEN** GameplayTickSystem 在一个 RenderFrame 内补跑多个 LocalLogicTick
- **THEN** Effect duration、period 和 journal MUST分别按每个 SimulationTick 推进
- **AND** PresentationFrame MUST不额外推进 GameplayEffect

### Requirement: BTSMTL 必须通过 CharacterGraphContext 使用 Gameplay Effect

Compiled BTSMTL operation MUST只通过 Program 声明的 GE query/command operation 与当前 CharacterSimulationState交互。Operation MUST不持有 GameplayEffectRuntime object、Unity adapter、Container 或 Model policy；缺少所需 catalog/port declaration 时 Program 编译 MUST失败。

#### Scenario: Transition 查询 Stun Tag

- **WHEN** compiled ConditionRuleGraph 查询 Stun
- **THEN** MUST从当前 CharacterSimulationState 的 Tag slot读取

### Requirement: Character 子阶段必须只获得 Gameplay Effect 最小能力

Compiled Action、Timeline、Motion 与 Condition operation MUST只获得各自声明的 GE query/command capability。Motion operation MUST不能 apply/remove Effect，Presentation command MUST不能修改 Attribute，Diagnostics MUST只能读取 Trace。

#### Scenario: Motion 查询 Action Tag

- **WHEN** Motion modifier 需要查询某 Tag
- **THEN** MUST只获得只读 Tag query operation

### Requirement: ActionRuntime 与 Gameplay Effect 必须保持事务和持续状态边界

Action operation MUST拥有 ActionInstance activation/lifecycle；GameplayEffect operation MUST拥有 Tag、Attribute 和 ActiveEffect 持续状态。Action MAY通过正式 GE command改变效果，Effect MAY通过 tag requirement影响动作，但两者 MUST不复制对方 state 或直接关闭对方 transaction。

#### Scenario: Dodge 激活无敌 Effect

- **WHEN** Dodge Action operation 提交 Self Effect command
- **THEN** GE operation MUST创建独立 EffectInstance
- **AND** Dodge ActionInstance 与 EffectInstance MUST保持不同 identity

### Requirement: 跨角色 Gameplay Effect 必须经过 GameplayResult 路由

Program operation MAY直接提交当前 Actor Self command，但跨 Actor Effect MUST来自正式 GameplayResult/target routing，并以 typed SimulationIngress进入目标 Actor Evaluate。Graph、Timeline 和 Effect operation MUST不直接取得另一 Actor mutable state。

#### Scenario: ActorA 命中 ActorB

- **WHEN** 权威 result 指定 ActorB 承受 Effect
- **THEN** Driver/Result router MUST为 ActorB 生成 typed GameplayResult ingress
- **AND** ActorA operation MUST不直接写 ActorB state

### Requirement: Gameplay Effect 输出必须进入统一事实与表现边界

Finalize MUST将当前 Tick 唯一 GE ChangeSet 投影为 Effect lifecycle、Attribute value、GameplayCue 和 structured Trace，并带 ActorId、BehaviorId、EffectInstanceId、revision 与 EventId。Committer MAY消费 Cue，Model Output Adapter MAY消费 typed facts；两者 MUST不重新 drain 或读取 GE state internals。

#### Scenario: Effect 修改 Health 并触发 Cue

- **WHEN** GE transaction 成功提交 Attribute 与 Cue change
- **THEN** Tick result MUST包含一次对应 typed facts
- **AND** Committer/Model adapter MUST从同一结果消费
