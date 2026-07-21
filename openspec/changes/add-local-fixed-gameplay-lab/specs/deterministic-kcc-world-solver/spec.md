## ADDED Requirements

### Requirement: Deterministic KCC必须是Fixed Numeric Target可复用的World Solver

Deterministic KCC及其Unity Definition、collision artifact loader与configuration identity MUST由Fixed Numeric Target或model-neutral Simulation程序集拥有，MUST不由DeterministicRollback Network Model程序集拥有。Local Fixed与DeterministicRollback MAY在各自完整Composition中选择同一个KCC Definition；KCC MUST不读取Source、Pipeline、Endpoint、Peer、history、prediction或rollback policy。

#### Scenario: Local Fixed与Rollback复用KCC

- **WHEN**两个Composition引用同一Fixed Program product、collision artifact与KCC Definition
- **THEN**它们的KCC ConfigurationHash、collision ContentHash与Solver identity MUST相同
- **AND**差异 MUST只存在于Source、Pipeline与model-specific ports

### Requirement: KCC手感配置必须显式进入身份且不得使用隐藏调试值

所有影响Deterministic KCC碰撞、Grounding、Step、Slide、Actor contact、capacity或iteration结果的参数 MUST由正式Definition构造`DeterministicKccConfiguration`并进入ConfigurationHash、KCC identity与World configuration identity。Gameplay Lab、EditorPrefs、static field、debug overlay或Presentation MUST不覆盖这些值。纯Character Program运动参数 MUST不被错误写入KCC ConfigurationHash。

#### Scenario: 两个Fixed Session使用不同Step Height

- **WHEN**Local Fixed与Rollback的MaximumStepHeight配置不同
- **THEN**它们的KCC/World identity MUST不同
- **AND**Composition或handshake MUST拒绝把两者视为同一World版本
