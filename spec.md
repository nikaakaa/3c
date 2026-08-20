## RENAMED Requirements

- FROM: `### Requirement: 当前Landing阶段必须保持Pose恒等`
- TO: `### Requirement: 当前阶段必须只生成Swing脚垂直Goal`

## MODIFIED Requirements

### Requirement: 当前阶段必须只生成Swing脚垂直Goal

Foot Placement MUST只在Current Step权威且处于Swing、Landing Event identity与该脚NextSwingLanding一致、Ground Path全部Edge通过Reachability、状态为Accepted、Ground Envelope端点合法且垂直增量严格大于几何容差时，为该脚生成非零位置Goal。PreSwing、支撑脚、Landing完成帧、`UnreachableEdge`、其它Ground Path Rejected、身份不一致和垂直增量处于容差内的脚 MUST继续发布原生Ankle位置与旋转，但位置和旋转权重都为零。Pelvis Goal MUST继续保持零位置和旋转权重。

Swing Foot Motion MUST使用同帧Original Component Pose中的Animated Sole计算`LastLanding -> NextSwingLanding`水平纵向进度，并按该进度分别采样Ground Envelope和两个Landing端点之间的直线基线。最终Ankle与Sole MUST只沿`Component Up`增加`Ground Envelope Sample高度 - Baseline Sample高度`，该增量 MUST在数值容差外保持非负。系统 MUST保留原生动画的水平位置、抬脚高度和旋转，不得把NextSwingLanding直接作为Ankle目标，不得从输入方向、速度方向或旧IK Pose重建脚轨迹。

具有有效非零垂直增量的Swing脚Position Weight MUST只使用同帧现有`animation.foot-placement-weight`作为上限，并乘以当前Step在`LiftOffPhase`到`LandingPhase`之间的无状态连续相位权重；Rotation Weight MUST为零。通过输入合同但垂直增量为零的Foot Motion MUST保持Accepted诊断并发布零权重Goal，使FullBodyIK跳过无意义的FBBIK Update。系统 MUST不叠加Landing Confidence、跨帧Goal平滑、Spring、Pelvis、Foot Lock、Constraint、Anchor、脚底旋转或FBBIK后处理。

同一`LandingEventIdentity`的Accepted落点 MUST在PreSwing或Swing阶段接受实时权威预测更新。更新距离小于正式Profile的死区时 MUST复用当前落点；从事件首次落点累计的预测误差超过软阈值后，Foot Goal Position Weight MUST平滑降低，并在硬阈值处降为零。事件完成时 MUST使用最后一个Accepted落点晋升为LastLanding，支撑脚不得继续追逐新路径。

#### Scenario: Swing脚经过台阶包络

- **WHEN** Current authoritative Swing Step与全部Edge可达的Accepted Ground Path属于同一Landing Event且Ground Envelope高于Landing基线
- **THEN** Foot Placement MUST把原生Ankle沿Component Up抬高对应的包络增量
- **AND** MUST保持原生Ankle在垂直于Component Up平面内的位置不变
- **AND** 唯一FullBodyIK MUST消费该同帧Goal并执行一次FBBIK

#### Scenario: Swing脚经过平地包络

- **WHEN** Ground Envelope与LastLanding到NextSwingLanding基线重合
- **THEN** Vertical Correction MUST为零
- **AND** Foot Motion MUST保持Accepted且Foot Goal Position Weight MUST为零
- **AND** 唯一FullBodyIK MUST验证Goal lineage后跳过FBBIK Update

#### Scenario: Ground Path不可用

- **WHEN** Current Step处于Swing但Ground Path为`UnreachableEdge`、其它Rejected、Envelope非法或Landing Event identity不一致
- **THEN** 该脚 MUST发布明确Foot Motion rejection和零权重Goal
- **AND** MUST不沿用上一帧Goal、默认Envelope或LastLanding到NextSwingLanding直线

#### Scenario: 支撑脚与Pelvis参与同帧GoalSet

- **WHEN** 另一只脚拥有有效Swing Foot Goal
- **THEN** 支撑脚和Pelvis Goal权重 MUST保持为零
- **AND** 本阶段 MUST不根据Swing脚高度移动Pelvis

### Requirement: Foot Placement诊断必须只显示当前事实

Scene诊断 MUST保留上一已提交Accepted Landing、下一Landing Event的Cached Accepted Landing、左右脚Ground Envelope和上游Invalid Segment，并从最近一次成功Seal的只读摘要显示当前Swing脚的Original Animated Sole、Corrected Sole及二者之间的实际垂直修正。Original Sole MUST使用白色小标记；Corrected Sole MUST使用对应脚颜色；修正 MUST使用细线；Active Swing的Foot Motion rejection MUST在Original Sole位置显示红色线框标记。

只读摘要与CSV MUST记录Foot Motion State、typed Reject Reason、Landing Event、Ground Path identity、Reachability状态、路径distance与progress、Original Sole与Ankle、Baseline Sample、Envelope Sample、Vertical Correction、Corrected Sole、最终Component Ankle Goal和实际Goal权重。Diagnostics与Gizmo MUST不重新采样动画、查询世界、计算Reachability、采样Envelope、计算Foot Motion或执行FBBIK，也 MUST不显示文字、伪路径或Pelvis结果。

Foot Placement Scene诊断与CSV MUST只证明Ground Path、Foot Motion和Goal事实，不得把Goal存在或画面抖动描述为最终骨骼已经改变。最终骨骼消费 MUST通过现有同帧FootPlacement Goal Target Watch与FullBodyIK Pose Watch验证；两者 MUST具有相同Frame、Completion和Rig lineage，FullBodyIK effector diagnostics MUST记录对应脚的目标、solved position和residual。

#### Scenario: 查看有效Swing Foot Motion

- **WHEN** 用户查看最近一次成功Seal且具有有效Swing Foot Goal的Scene诊断
- **THEN** Corrected Sole与Original Sole的差 MUST逐值等于Component Up乘Vertical Correction
- **AND** CSV中的最终Goal、Position Weight和Pelvis Weight MUST逐值等于同一GoalSet事实

#### Scenario: 查看失败Swing Foot Motion

- **WHEN** 当前Swing脚因Ground Path或Foot Motion合同失败而发布零权重Goal
- **THEN** Scene诊断 MUST在Original Sole显示红色失败标记
- **AND** CSV MUST记录对应typed Reject Reason且不得保留上一帧Corrected Sole或Goal

#### Scenario: 验证Goal已经改变最终脚骨骼

- **WHEN** 当前Swing脚发布非零位置Goal且唯一FullBodyIK成功完成同帧求解
- **THEN** FootPlacement Goal Target Watch与FullBodyIK Pose Watch MUST具有相同Frame、Completion和Rig lineage
- **AND** FullBodyIK对应脚effector diagnostics MUST记录该Goal与最终solved position
- **AND** 用户 MUST不从Scene Gizmo、Foot Landing CSV或抖动单独推断骨骼已经消费Goal
