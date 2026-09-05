## REMOVED Requirements

### Requirement: Standalone Gameplay 必须提供同 Session 的 Corin 训练敌人

### Requirement: 玩家目标 provider 必须显式绑定训练敌人

### Requirement: 训练敌人闭环不得冒充完整 Combat 或完整敌人 AI

## MODIFIED Requirements

### Requirement: Corin 五段攻击必须预置可调 MotionWarp

Standalone Gameplay MUST只注册一个Corin正式Actor，不得注册训练敌人、第二角色或目标占位Actor。Corin的target input MUST固定为`None`，不得通过Scene搜索、Tag、GameObject名称、Roster绑定或独立Target Provider产生目标。Attack1到Attack5的`CanActivateAction`与`ActivateActionInstance`仍 MUST引用同一个Character-scope、Spawn-lifetime、InputDerived `ActionTargetSnapshot` declaration；输入为None时，OptionalSnapshot MUST允许动作正常启动。

每段Attack Timeline MUST在主Action MotionCurve上拥有一个显式MotionWarpClip，`TranslationMode` MUST为`Disabled`，`RotationMode` MUST为`FaceTarget`，`RotationMethod` MUST为`ProgressCurve`；后摇MotionCurve MUST NOT作为Warp source。无目标时MotionWarp MUST不改变主MotionCurve的平面位移或原始朝向轨迹，五段攻击的动作、后摇与其它动作语义 MUST保持原样，最终Body结果 MUST继续由唯一WorldSolver裁决。

#### Scenario: Gameplay Lab只运行Corin

- **WHEN** Standalone Gameplay完成Session composition
- **THEN** roster MUST只包含Corin玩家Actor
- **AND** target input MUST为None
- **AND** Session MUST不创建训练敌人、第二Actor或占位目标

#### Scenario: 无目标执行五段攻击

- **WHEN** 玩家在target input为None的条件下触发Attack1到Attack5任一段
- **THEN** OptionalSnapshot MUST按无目标业务语义允许动作与Timeline正常启动
- **AND** 主MotionCurve、后摇与其它动作语义 MUST保持原始结果
- **AND** Runtime MUST不伪造目标快照、命中、伤害或敌人反应
