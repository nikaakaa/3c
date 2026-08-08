## MODIFIED Requirements

### Requirement: 玩家目标 provider 必须显式绑定训练敌人

Standalone玩家Control Source MUST显式绑定训练敌人的Session Actor identity，并将其最近提交逻辑Body转换为portable target input。该绑定 MUST由正式配置表达，MUST NOT通过Scene搜索、Tag、GameObject名称、最近距离或Camera选择目标。

#### Scenario: 训练敌人可用

- **WHEN** 玩家采样输入且训练敌人存在有效committed Body
- **THEN** target input MUST包含训练敌人ActorId、position与yaw
- **AND** Attack admission与activation MUST通过唯一Blackboard Input Binding读取该值

#### Scenario: 训练敌人不可用

- **WHEN** 目标Actor尚未可用或已退出Session
- **THEN** 玩家target input MUST为None
- **AND** OptionalSnapshot Attack MUST按无目标业务语义继续执行源MotionCurve

### Requirement: Corin 五段攻击必须预置可调 MotionWarp

Corin Attack Profile MUST声明`OptionalSnapshot`，Dodge MUST保持`None`。Attack1到Attack5的`CanActivateAction`与`ActivateActionInstance` MUST引用同一个Character-scope、Spawn-lifetime、带正式Input Binding的`ActionTargetSnapshot` declaration。每段Attack Timeline MUST在主Action MotionCurve上拥有一个显式MotionWarpClip，`TranslationMode` MUST为`Disabled`，`RotationMode` MUST为`FaceTarget`，`RotationMethod` MUST为`ProgressCurve`；后摇MotionCurve MUST NOT作为Warp source。

#### Scenario: 有目标执行一段攻击

- **WHEN** Attack1激活时目标候选有效
- **THEN** ActionInstance MUST固定保存目标快照
- **AND** Attack1 MotionWarp MUST在作者配置窗口内只修正面向目标的yaw
- **AND** Attack1 MotionWarp MUST NOT修改主MotionCurve的平面位移
- **AND** 最终Body结果 MUST继续由唯一WorldSolver裁决

#### Scenario: 连续进入下一段攻击

- **WHEN** Attack2到Attack5中任一段创建新的ActionInstance
- **THEN** 该段 MUST从当前Input Binding写入的候选重新捕获自己的目标快照
- **AND** MUST NOT复用前一段ActionInstance的Warp跨Tick状态

#### Scenario: 无目标挥空

- **WHEN** 玩家在无目标条件下触发任一Attack
- **THEN** 动作与Timeline MUST正常启动
- **AND** 主MotionCurve MUST保持原始轨迹，后摇与其它动作语义 MUST不改变

