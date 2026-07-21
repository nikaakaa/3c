## MODIFIED Requirements

### Requirement: Corin 五段攻击必须预置可调 MotionWarp

Corin Attack Profile MUST继续声明`OptionalSnapshot`，Dodge MUST保持`None`。Attack1到Attack5的每段主Action MotionCurve MUST拥有一个显式MotionWarpClip；后摇MotionCurve MUST不作为Warp source。五段普通攻击 MUST使用`SkewToTarget`、`ApproachDirection`、`FaceTarget`与`ProgressCurve`作为正式起点，并分别保存与自身源MotionCurve、AnimationTrack和命中节奏匹配的窗口、站距、最大修正与position/yaw progress；系统 MUST不再让五段复用从源第0帧到末帧的同一线性模板。

#### Scenario: 有目标执行一段攻击

- **WHEN** 任一Attack激活时目标候选有效且目标在配置限制内
- **THEN** ActionInstance MUST固定保存目标快照
- **AND** Warp MUST沿ApproachDirection计算该段目标站位
- **AND** Skew累计轨迹 MUST在该段窗口结束时达到有效位置并面向目标
- **AND** 最终Body结果 MUST继续由唯一WorldSolver裁决

#### Scenario: 每段拥有独立窗口

- **WHEN** 作者检查Attack1到Attack5 Timeline
- **THEN** 每段Warp窗口 MUST根据自身接近目标到命中阶段配置
- **AND** 后摇阶段 MUST不继续拉位置或yaw
- **AND** 每段curve MUST可独立微调而不修改Runtime代码

#### Scenario: 无目标挥空

- **WHEN** 玩家在无目标条件下触发任一Attack
- **THEN** 动作与Timeline MUST正常启动
- **AND** 主MotionCurve MUST保持原始轨迹
- **AND** 新Translation/Rotation Solver MUST不初始化累计Warp state

