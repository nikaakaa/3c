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

### Requirement: 骨盆可达硬边界必须与姿态偏好分责

Pelvis MUST从同帧typed Reach Request的真实腿长与正式安全余量形成唯一双腿硬区间。原动画额外弯曲余量 MUST只属于目标姿态偏好，不得再形成另一份输出硬夹紧。完整边界 MUST交给同一骨盆响应阶段，外部Module不得在其后再次改写Pelvis输出。

系统 MUST继续只持有一份根Bank内Spring状态、使用现有正式响应配置和Handoff规则。输出触界时 MUST保留腿长安全并处理朝外速度；几何不可达 MUST通过既有typed Reach/Goal保护表达，不得以悬空、默认目标、未授权降权或FBBIK完全伸直代替明确政策。

#### Scenario: 原动画弯曲余量大于安全余量

- **WHEN** 动画姿态希望保留更多弯曲，但实际腿长与正式安全余量允许更高骨盆
- **THEN** 动画弯曲要求 MAY影响响应目标，MUST不单独强压最终骨盆输出
- **AND** 只有统一硬区间可裁决必要的输出夹紧

#### Scenario: 当前几何要求必要下蹲

- **WHEN** 双脚目标与身体位置使当前骨盆输出超过真实硬可达边界
- **THEN** 唯一Pelvis响应阶段 MUST保持腿长安全并发布必要的边界调整
- **AND** MUST不承诺在不变脚目标、身体和腿长的同时绝对连续，也不得另加后置平滑绕过限制

### Requirement: 骨盆观测必须区分原Pose、修正量和最终世界写回

最终Physical Pelvis世界点 MUST由唯一Physical Writer完成本次骨骼写入后取得，并与组件点及同一Completion一起冻结。Sampler MUST只消费这份正式结果，不通过采样时live Root变换冒充同Completion世界事实。原Pose输入有效性 MUST与HeightTarget/Posture求值有效性分离；产生合法Pelvis Goal的Releasing或仅LandingReach也 MUST发布真实源Pose。

#### Scenario: Release阶段检查最终骨盆Goal残差

- **WHEN** Releasing仍有非零Pelvis Goal且同Completion的最终Physical写回有效
- **THEN** 残差 MUST使用本帧真实源Pelvis组件点加正式加权Goal作为期望
- **AND** MUST不把未执行HeightTarget的默认零点当原Pose

#### Scenario: 最终世界运动与额外修正不同

- **WHEN** Root、原动画和Pelvis修正同时变化
- **THEN** Diagnostics MUST分别表达最终世界点、组件点和相对修正，不把其中一项直接命名成另一项的跳变
- **AND** 必要阶段缺失 MUST标Unavailable，不改质量规则或用占位零值宣布通过
