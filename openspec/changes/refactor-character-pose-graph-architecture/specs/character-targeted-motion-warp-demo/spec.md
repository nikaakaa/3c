## REMOVED Requirements

### Requirement: Standalone Gameplay 必须提供同 Session 的 Corin 训练敌人

### Requirement: 玩家目标 provider 必须显式绑定训练敌人

### Requirement: 训练敌人闭环不得冒充完整 Combat 或完整敌人 AI

## ADDED Requirements

### Requirement: Standalone Gameplay只运行Corin并允许无目标攻击

Standalone Gameplay MUST只注册一个Corin正式Actor，不得注册训练敌人、第二角色或目标占位Actor。Corin的target input MUST固定为`None`，不得通过Scene搜索、Tag、GameObject名称、Roster绑定或独立Target Provider产生目标。target input为None时，Attack1到Attack5的OptionalSnapshot MUST允许动作与Timeline正常启动并继续执行原始主MotionCurve、后摇和其它动作语义；Runtime MUST不伪造目标快照、命中、伤害或敌人反应。

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
