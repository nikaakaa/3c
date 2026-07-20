## RENAMED Requirements

- FROM: `### Requirement: 训练敌人闭环不得冒充完整 Combat 或 AI`
- TO: `### Requirement: 训练敌人闭环不得冒充完整 Combat 或完整敌人 AI`

## MODIFIED Requirements

### Requirement: Standalone Gameplay 必须提供同 Session 的 Corin 训练敌人

Standalone Gameplay MUST在同一个`SimulationSessionHost`中注册玩家Corin与训练敌人两个正式Actor。训练敌人 MUST复用Corin CharacterPipelineDefinition、compiled Program、Projection、WorldSolver与Presentation链，使用Corin Training AI Control Source和SimulatedActor Presentation Role。训练敌人 MUST有独立稳定ActorId、InitialBody和World body binding，MUST NOT拥有玩家Camera或设备输入，也 MUST NOT保留Neutral Control Source fallback。

#### Scenario: Standalone 创建两个 Actor

- **WHEN** Standalone Gameplay完成Session composition
- **THEN** roster MUST包含玩家Actor与`corin-training-enemy`
- **AND** 两者 MUST由同一Session在同一Logic Tick顺序中执行

#### Scenario: 训练敌人由AI输入驱动

- **WHEN** Local Control Input Ingress准备训练敌人当前Tick输入
- **THEN** 输入 MUST由Corin Training AI Control Source产生
- **AND** 敌人 MUST继续拥有正式逻辑Body、碰撞与动画表现

### Requirement: 训练敌人闭环不得冒充完整 Combat 或完整敌人 AI

本capability MUST只交付显式目标、直线接近、普通Attack请求、动作目标捕获、MotionWarp与第二个正式Actor。它 MUST NOT添加Team/Faction、动态目标评分、寻路、格挡、Hitbox、命中检测、伤害、Health、死亡或完整怪物行为。后续Combat MUST通过正式GameplayResult/Effect边界接入。

#### Scenario: 训练敌人被玩家攻击

- **WHEN** 玩家MotionWarp向训练敌人执行攻击
- **THEN** 本change只保证玩家位移与朝向修正面向该逻辑Body
- **AND** MUST NOT伪造命中、伤害或敌人反应结果

