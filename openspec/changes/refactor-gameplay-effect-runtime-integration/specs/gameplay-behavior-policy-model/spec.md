## MODIFIED Requirements

### Requirement: Gameplay Behavior 必须提供统一行为身份

系统 MUST 使用 Gameplay Behavior 或等价模型为所有 gameplay 行为提供统一作者身份。每个 behavior MUST 至少声明稳定 `BehaviorId`、`BehaviorKind`、tags、display name 和 debug category。Gameplay Behavior MUST 是作者和模型 policy lookup 身份，MUST NOT 自己保存具体 Network Model policy，也 MUST NOT 直接替代 Graph 节点、Timeline clip、ActionInstance、MotionContribution、GameplayEffectLifecycleFact 或 GameplayCueFact。

#### Scenario: 作者配置轻攻击

- **WHEN** 作者配置 `Attack.Light.01`
- **THEN** 该行为 MUST 有稳定 `BehaviorId = Attack.Light.01`
- **AND** 该行为 MUST 标记为 `BehaviorKind.Transaction`
- **AND** 它的运行时执行 MUST 继续通过 ActionInstance 和 ActionSyncDomain，而不是通过 Graph 路径或 Timeline asset 身份同步

#### Scenario: 作者配置 Gameplay Effect

- **WHEN** 作者配置 `Effect.CrowdControl.Stun`
- **THEN** GameplayEffectDefinition MUST 使用 EffectId 提供同值 BehaviorId
- **AND** 该行为 MUST 标记为 `BehaviorKind.Effect`
- **AND** EffectDefinition MUST 不保存 ServerAuthoritative policy

#### Scenario: 作者配置普通移动

- **WHEN** 作者配置普通 locomotion 或移动输入行为
- **THEN** 该行为 MUST 有稳定 BehaviorId，例如 `Movement.Locomotion.Move`
- **AND** 该行为 MUST 标记为 `BehaviorKind.Stream`
- **AND** 系统 MUST NOT 为每一帧普通移动创建 ActionInstance
