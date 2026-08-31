## ADDED Requirements

### Requirement: 有效Foot位置目标不得由修正幅度撤销

同帧正式State/Support Target与Sole/Ankle目标解析成功的Ready Foot MUST按正式FootPlacementWeight发布PositionWeight。Correction为零或小于GeometryEpsilon MUST不撤销该位置约束。Unavailable与Suppress MUST继续零权重；作者权重为零 MUST保持原关闭语义。Rotation MUST继续使用现有正式Contact与LockWeight政策。

#### Scenario: Swing修正趋零但目标仍有效

- **WHEN** Swing目标合法、正式FootPlacementWeight非零，Correction从0.1毫米以上变为更小值或零
- **THEN** PositionWeight MUST继续等于正式作者权重，目标仍随本帧Swing更新
- **AND** MUST不因Pelvis先平移而默许脚脱离这个有效目标，也不得冻结为世界Anchor

#### Scenario: 作者关闭或目标不可用

- **WHEN** 正式作者权重为零，或本帧目标解析失败/被Suppress
- **THEN** 系统 MUST保持原零权重/不可用语义
- **AND** MUST不使用上一目标、默认点或第二Goal补全

### Requirement: 共同Pelvis目标必须来自同帧双脚高度需求

在现有Pelvis生产资格内，共同期望偏移 MUST由同帧原动画双脚Sole与Resolved有效目标Sole沿同一Component Up的最低高度差产生：`min(targetL,targetR)-min(animatedL,animatedR)`。该偏移 MUST保留正负号，替换旧地形相对高度加正向抬脚补偿，不与其叠加。目标 MUST不读取上一帧最终Physical脚底反推，也不得产生第二目标Owner。

#### Scenario: 较低目标脚位于较低动画脚之下

- **WHEN** 有效目标脚最低高度小于原动画脚最低高度
- **THEN** 共同期望偏移 MUST为负值，并交给同一Pelvis响应
- **AND** MUST不因旧正向max门丢弃下降需求

#### Scenario: 最低脚身份不同

- **WHEN** 原动画最低脚与有效目标最低脚属于不同Side
- **THEN** 系统 MUST分别取各对双脚的最低高度后相减
- **AND** MUST不暗改为同脚修正最小值、平均值或旧Stride地形高度

### Requirement: 骨盆可达性观察不得硬改骨盆与脚目标

系统 MUST保留同帧typed Reach Request的逐腿几何与交集观察，以及原Landing完成可达资格。Reach MUST不再夹取骨盆响应目标或输出、清边界速度、阻止骨盆Release回零或强开骨盆权重，Primary Support MUST不作为例外。Module MUST删除末端Foot径向夹脚，原Resolved目标与正式权重直接进入唯一GoalSet。

系统 MUST只持有原根Bank内Spring，沿原频率及Handoff／背向速度规则积分preferredTarget。软姿态偏好 MUST只在原动画零偏移与共同请求之间选择，不附加Reach硬边界。MUST删除硬执行选择、公共执行上下界、动作事实和夹脚API，不保留恒值兼容分支。

#### Scenario: 主支撑可达上界低于响应输出

- **WHEN** 主支撑几何观察给出低于Spring输出的上界
- **THEN** MUST保留观察，不通过它硬改骨盆或Foot Goal
- **AND** 实际求解不足 MUST照实保留，不将Solver成功等同最终骨骼准确到位

#### Scenario: Release沿原响应回零

- **WHEN** 骨盆处于Release
- **THEN** 原Spring MUST追零，完成只由原输出／速度容差决定
- **AND** MUST保留真实Up及逐腿观察，不产生无历史Reach启动

#### Scenario: Landing完成与骨盆执行分开

- **WHEN** Foot到达原Landing完成检查阶段
- **THEN** MUST按本腿实际加权位移可达性执行原资格检查
- **AND** MUST不据此后置夹脚、硬压骨盆或修改作者权重

### Requirement: 骨盆观测必须区分原Pose、修正量和最终世界写回

最终Physical Pelvis世界点 MUST由唯一Physical Writer完成本次骨骼写入后取得，并与组件点及同一Completion一起冻结。Sampler MUST只消费这份正式结果，不通过采样时live Root变换冒充同Completion世界事实。原Pose输入有效性 MUST与HeightTarget/Posture求值有效性分离；产生合法Pelvis Goal的Releasing也 MUST发布真实源Pose。

#### Scenario: Release阶段检查最终骨盆Goal残差

- **WHEN** Releasing仍有非零Pelvis Goal且同Completion的最终Physical写回有效
- **THEN** 残差 MUST使用本帧真实源Pelvis组件点加正式加权Goal作为期望
- **AND** MUST不把未执行HeightTarget的默认零点当原Pose

#### Scenario: 最终世界运动与额外修正不同

- **WHEN** Root、原动画和Pelvis修正同时变化
- **THEN** Diagnostics MUST分别表达最终世界点、组件点和相对修正，不把其中一项直接命名成另一项的跳变
- **AND** 必要阶段缺失 MUST标Unavailable，不改质量规则或用占位零值宣布通过
