# character-foot-placement-presentation Specification

## Purpose

定义Corin当前Landing Prediction、Foot Placement Goal事务与唯一FinalIK FBBIK之间的正式表现边界。

## Requirements

### Requirement: Foot Placement必须是唯一Goal事务

`CharacterFootPlacementRuntime` MUST只消费同帧Component Pose、左右原子Biomechanical Step Read Page、Body Presentation、Locomotion Motion Timeline、正式Future Body Translation与当前PhysicsScene，并只输出Pelvis、LeftFoot、RightFoot三个Goal。一次Frame只能拥有一个Pending结果，并且必须由外层表现事务`Seal`或`Discard`。

系统 MUST不提供第二Grounding、第二Pelvis、LegIK、TwoBoneIK、默认地面、固定高度、fallback、兼容Goal链或FBBIK后处理。

#### Scenario: 一帧完成

- **WHEN** Foot Placement完成左右脚Landing判断
- **THEN** Runtime MUST发布同Frame、Completion与Rig identity的三个Goal
- **AND** 外层事务 MUST对该Pending结果执行一次`Seal`或`Discard`

### Requirement: Landing Prediction必须形成独立世界事实

每只脚 MUST按`Current/Incoming Step -> committed Body Target世界速度 + Timeline段边界/Continuation -> KCC Future Body Translation -> Raw Landing -> Future Landing SphereCast -> Accepted/Rejected Landing`执行。Step必须携带稳定Landing Event identity；Raw Landing必须按`VisiblePosition + FutureBodyTranslation + VisibleRotation * RootLocalLanding`从本帧输入重新投影，不得旋转旧查询结果。

Future Body Translation的当前平面速度 MUST来自同帧committed Body Target世界速度；Timeline只提供当前有限段剩余时间和显式Continuation世界速度。KCC MUST在原世界空间积分并裁剪平移，不得按输入方向、速度方向、Body Yaw、相邻表现速度方向差或任何推导曲率旋转世界速度。`RootLocalLanding` MUST只乘本帧已经显示的`VisibleRotation`；当前阶段没有正式未来朝向Plan，因此 MUST不外推Future Body Yaw，不得把瞬时Yaw Velocity维持到Landing时刻。

SphereCast MUST从Raw Landing上方沿Component Down使用Profile声明的半径和有限距离查询。查询 MUST过滤自身Collider、初始重叠、非法点、非法法线与超坡度命中，并在固定容量返回集合中按距离和稳定identity选择最近合法命中。没有合法命中时 MUST发布`GroundQueryMissed`，不得创建默认Surface。

#### Scenario: Future Landing命中

- **WHEN** SphereCast返回合法Surface
- **THEN** diagnostics MUST发布唯一Accepted Landing、Surface identity、点、法线与实际查询距离

#### Scenario: Landing输入不可用

- **WHEN** Step、Motion Timeline、Body Target、Future Body Translation或合法Surface不可用
- **THEN** 该脚 MUST发布明确Rejected原因
- **AND** MUST不沿用上一帧Landing或生成替代落点

### Requirement: 当前Landing阶段必须保持Pose恒等

当前阶段只验证未来落点，不实现Foot Motion、FootLock、Constraint、Anchor、Pelvis、Ground Envelope、Capsule Path、Edge、Hull或Reachability，也不引入参考未定义的额外规划层、版本层或兼容路径。Pelvis与双脚Goal的位置和旋转权重 MUST全部为零；唯一FullBodyIK MUST在验证Goal lineage后跳过FBBIK求解并保持输入Pose不变。

#### Scenario: Landing被接受

- **WHEN** 任一脚得到Accepted Landing
- **THEN** Landing事实 MUST只进入diagnostics
- **AND** 脚、骨盆和其它Physical Bone MUST继续使用原动画Pose

### Requirement: Foot Placement配置与Rig必须显式

FootPlacement节点 MUST显式引用唯一Profile与Calibration。Projection、Profile、Calibration、Rig v4和Animation Rig Binding的identity与revision MUST精确匹配；PhysicsScene、World-Aware Binding或正式Future Body Translation source缺失时 MUST报告不可用，不得从Transform名称、Animator Avatar、旧Prefab组件或默认配置补全。

#### Scenario: Projection与Calibration不匹配

- **WHEN** Projection保存的Calibration identity或revision与Runtime资产不同
- **THEN** Runtime创建 MUST失败并报告stale identity
- **AND** MUST不继续使用旧Goal

### Requirement: Foot Placement必须与Gameplay和Network隔离

Landing、Goal、查询命中和diagnostics只属于Presentation。它们 MUST不进入Character State、World State、Gameplay Fact、Blackboard、Snapshot、Hash或网络packet，也 MUST不写VisualRoot或Gameplay Body。

#### Scenario: 两端显示同一角色

- **WHEN** 两个客户端以不同Presentation时刻显示同一committed Body
- **THEN** 两端 MAY独立计算Landing diagnostics
- **AND** 结果 MUST不改变Gameplay或网络确认

### Requirement: Foot Placement诊断必须只显示当前事实

Scene诊断 MUST只显示Current Animated Sole、Raw Landing、实际SphereCast和Accepted或Rejected Landing。诊断 MUST从成功提交的只读摘要发布，不得重新采样动画、重新查询世界、保存完整Foot Feature或伪装旧Grounding、Predictive Modifier、Plan、Anchor、Pelvis和Ground Envelope语义。

#### Scenario: 查看Landing诊断

- **WHEN** 用户打开Foot Placement Scene诊断
- **THEN** 显示内容 MUST与最近一次成功Seal的Landing事务一致
- **AND** 读取诊断 MUST不改变下一帧结果
