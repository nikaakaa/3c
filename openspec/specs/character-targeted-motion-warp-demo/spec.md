# character-targeted-motion-warp-demo Specification

## Purpose

定义Standalone Gameplay中Corin玩家、同Session训练敌人、显式目标输入与五段攻击MotionWarp的正式演示闭环，同时明确该样例不冒充AI、命中或伤害系统。

## Requirements

### Requirement: Standalone Gameplay 必须提供同 Session 的 Corin 训练敌人

Standalone Gameplay MUST在同一个 `SimulationSessionHost` 中注册玩家 Corin 与训练敌人两个正式 Actor。训练敌人 MUST复用 Corin CharacterPipelineDefinition、compiled Program、Projection、WorldSolver 与 Presentation 链，使用 Neutral Input 和SimulatedActor Presentation Role。训练敌人 MUST有独立稳定ActorId、InitialBody和World body binding，MUST NOT拥有玩家Camera或设备输入。

#### Scenario: Standalone 创建两个 Actor

- **WHEN**Standalone Gameplay完成Session composition
- **THEN**roster MUST包含玩家Actor与`corin-training-enemy`
- **AND**两者 MUST由同一Session在同一Logic Tick顺序中执行

#### Scenario: 训练敌人保持静止

- **WHEN**训练敌人没有AI或玩家输入
- **THEN**Neutral Input MUST保持连续输入为neutral且request为空
- **AND**敌人 MUST继续拥有正式逻辑Body、碰撞与动画表现

### Requirement: 玩家目标 provider 必须显式绑定训练敌人

Standalone玩家Control Source MUST显式绑定训练敌人的Session Actor identity，并将其最近提交逻辑Body转换为portable target input。该绑定 MUST由正式配置表达，MUST NOT通过Scene搜索、Tag、GameObject名称、最近距离或Camera选择目标。

#### Scenario: 训练敌人可用

- **WHEN**玩家采样输入且训练敌人存在有效committed Body
- **THEN**target input MUST包含训练敌人ActorId、position与yaw
- **AND**Attack admission与activation MUST通过唯一InputDerived declaration读取该值

#### Scenario: 训练敌人不可用

- **WHEN**目标Actor尚未可用或已退出Session
- **THEN**玩家target input MUST为None
- **AND**OptionalSnapshot Attack MUST按无目标业务语义继续执行源MotionCurve

### Requirement: Corin 五段攻击必须预置可调 MotionWarp

Corin Attack Profile MUST声明`OptionalSnapshot`，Dodge MUST保持`None`。Attack1到Attack5的`CanActivateAction`与`ActivateActionInstance` MUST引用同一个Character-scope、Spawn-lifetime、InputDerived `ActionTargetSnapshot` declaration。每段Attack Timeline MUST在主Action MotionCurve上拥有一个显式MotionWarpClip；后摇MotionCurve MUST NOT作为Warp source。

#### Scenario: 有目标执行一段攻击

- **WHEN**Attack1激活时目标候选有效
- **THEN**ActionInstance MUST固定保存目标快照
- **AND**Attack1 MotionWarp MUST在作者配置窗口内修正其主MotionCurve的平面位置与yaw
- **AND**最终Body结果 MUST继续由唯一WorldSolver裁决

#### Scenario: 连续进入下一段攻击

- **WHEN**Attack2到Attack5中任一段创建新的ActionInstance
- **THEN**该段 MUST从当前input-derived候选重新捕获自己的目标快照
- **AND**MUST NOT复用前一段ActionInstance的Warp跨Tick状态

#### Scenario: 无目标挥空

- **WHEN**玩家在无目标条件下触发任一Attack
- **THEN**动作与Timeline MUST正常启动
- **AND**主MotionCurve MUST保持原始轨迹，后摇与其它动作语义 MUST不改变

### Requirement: 训练敌人闭环不得冒充完整 Combat 或 AI

本capability MUST只交付目标输入、动作目标捕获、MotionWarp与第二个正式Actor。它 MUST NOT添加敌人决策树、寻路、攻击、格挡、Hitbox、命中检测、伤害、Health、死亡或目标评分旁路。后续AI MUST通过替换Control Source提交同一portable input/request；后续Combat MUST通过正式GameplayResult/Effect边界接入。

#### Scenario: 训练敌人被玩家攻击

- **WHEN**玩家MotionWarp向训练敌人执行攻击
- **THEN**本change只保证玩家位移与朝向修正面向该逻辑Body
- **AND**MUST NOT伪造命中、伤害或敌人反应结果
