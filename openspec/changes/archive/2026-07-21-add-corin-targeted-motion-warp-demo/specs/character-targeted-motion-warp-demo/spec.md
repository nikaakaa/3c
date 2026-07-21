## ADDED Requirements

### Requirement: Standalone Gameplay 必须提供同 Session 的 Corin 训练敌人

Standalone Gameplay MUST在同一个 `SimulationSessionHost` 中注册玩家 Corin 与训练敌人两个正式 Actor。训练敌人 MUST复用 Corin CharacterPipelineDefinition、compiled Program、Projection、WorldSolver 与 Presentation 链，使用 Neutral Input 和 SimulatedActor Presentation Role。训练敌人 MUST有独立稳定 ActorId、InitialBody 和 World body binding，MUST NOT拥有玩家 Camera 或设备输入。

#### Scenario: Standalone 创建两个 Actor

- **WHEN** Standalone Gameplay 完成 Session composition
- **THEN** roster MUST包含玩家 Actor 与 `corin-training-enemy`
- **AND** 两者 MUST由同一 Session 在同一 Logic Tick 顺序中执行

#### Scenario: 训练敌人保持静止

- **WHEN** 训练敌人没有 AI 或玩家输入
- **THEN** Neutral Input MUST保持连续输入为 neutral 且 request 为空
- **AND** 敌人 MUST继续拥有正式逻辑 Body、碰撞与动画表现

### Requirement: 玩家目标 provider 必须显式绑定训练敌人

Standalone 玩家 Control Source MUST显式绑定训练敌人的 Session Actor identity，并将其最近提交逻辑 Body 转换为 portable target input。该绑定 MUST由正式配置表达，MUST NOT通过 Scene 搜索、Tag、GameObject 名称、最近距离或 Camera 选择目标。

#### Scenario: 训练敌人可用

- **WHEN** 玩家采样输入且训练敌人存在有效 committed Body
- **THEN** target input MUST包含训练敌人 ActorId、position 与 yaw
- **AND** Attack admission 与 activation MUST通过唯一 InputDerived declaration 读取该值

#### Scenario: 训练敌人不可用

- **WHEN** 目标 Actor 尚未可用或已退出 Session
- **THEN** 玩家 target input MUST为 None
- **AND** OptionalSnapshot Attack MUST按无目标业务语义继续执行源 MotionCurve

### Requirement: Corin 五段攻击必须预置可调 MotionWarp

Corin Attack Profile MUST声明 `OptionalSnapshot`，Dodge MUST保持 `None`。Attack1 到 Attack5 的 `CanActivateAction` 与 `ActivateActionInstance` MUST引用同一个 Character-scope、Spawn-lifetime、InputDerived `ActionTargetSnapshot` declaration。每段 Attack Timeline MUST在主 Action MotionCurve 上拥有一个显式 MotionWarpClip；后摇 MotionCurve MUST NOT作为 Warp source。

#### Scenario: 有目标执行一段攻击

- **WHEN** Attack1 激活时目标候选有效
- **THEN** ActionInstance MUST固定保存目标快照
- **AND** Attack1 MotionWarp MUST在作者配置窗口内修正其主 MotionCurve 的平面位置与 yaw
- **AND** 最终 Body 结果 MUST继续由唯一 WorldSolver 裁决

#### Scenario: 连续进入下一段攻击

- **WHEN** Attack2 到 Attack5 中任一段创建新的 ActionInstance
- **THEN** 该段 MUST从当前 input-derived 候选重新捕获自己的目标快照
- **AND** MUST NOT复用前一段 ActionInstance 的 Warp 跨 Tick 状态

#### Scenario: 无目标挥空

- **WHEN** 玩家在无目标条件下触发任一 Attack
- **THEN** 动作与 Timeline MUST正常启动
- **AND** 主 MotionCurve MUST保持原始轨迹，后摇与其它动作语义 MUST不改变

### Requirement: 训练敌人闭环不得冒充完整 Combat 或 AI

本 capability MUST只交付目标输入、动作目标捕获、MotionWarp 与第二个正式 Actor。它 MUST NOT添加敌人决策树、寻路、攻击、格挡、Hitbox、命中检测、伤害、Health、死亡或目标评分旁路。后续 AI MUST通过替换 Control Source 提交同一 portable input/request；后续 Combat MUST通过正式 GameplayResult/Effect 边界接入。

#### Scenario: 训练敌人被玩家攻击

- **WHEN** 玩家 MotionWarp 向训练敌人执行攻击
- **THEN** 本 change 只保证玩家位移与朝向修正面向该逻辑 Body
- **AND** MUST NOT伪造命中、伤害或敌人反应结果
