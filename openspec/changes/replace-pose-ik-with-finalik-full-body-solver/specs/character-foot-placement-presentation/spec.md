## MODIFIED Requirements

### Requirement: Foot Placement必须是唯一单帧Goal事务

系统 MUST把Original Component Pose、原子Biomechanical Step、committed Body Trajectory与World事实送入唯一`CharacterFootPlacementRuntime`事务，并只输出Pelvis、Left Foot、Right Foot三个Final Goal。FinalIK FBBIK MUST只执行该Goal Set。系统 MUST不提供第二Grounding、第二Pelvis、LegIK/TwoBoneIK、默认地面、固定高度、fallback或FBBIK后处理。

#### Scenario: 一帧Foot Placement成功

- **WHEN** 同一Completion的Pose、Step、Trajectory和World事实全部有效
- **THEN** Foot Placement MUST一次提交三个同Completion Goal
- **AND** FinalIK MUST只执行该Goal Set

#### Scenario: 任一上游事实无效

- **WHEN** Step、Trajectory、Query、Hull或Landing任一事实无效
- **THEN** Foot Placement MUST发布精确typed failure
- **AND** MUST不由Current Grounding或Original动画伪装预测成功

## ADDED Requirements

### Requirement: Landing Prediction必须先形成可独立验证的世界事实

系统 MUST先以权威Current或Incoming Biomechanical Step的`RootLocalLanding`、落地时间和Landing Event identity，结合Simulation committed Body position、facing、linear/angular trajectory生成Raw Landing。系统 MUST对Raw Landing执行实际向下SphereCast，并只把合法坡度命中发布为Accepted Landing；无Step、identity不匹配、Body trajectory不可用或查询无命中 MUST发布精确拒绝原因。该阶段 MUST不生成Path、Hull、Foot Motion、Lock或Pelvis，也 MUST不修改脚和骨盆。

#### Scenario: 直线行走产生合法落点

- **WHEN** 当前脚拥有权威下一落脚事实、committed Body trajectory和合法可踩命中
- **THEN** 系统 MUST同时发布Raw Landing、实际SphereCast请求和Accepted Landing
- **AND** Gizmo MUST分别绘制Native Sole、Raw Landing、查询与Accepted Landing

#### Scenario: 前方没有合法踏面

- **WHEN** Raw Landing下方SphereCast没有合法命中
- **THEN** 系统 MUST发布`GroundQueryMissed`
- **AND** MUST不创建绿色落点、默认地面或伪Path

#### Scenario: Landing阶段尚未拥有Foot Motion

- **WHEN** Landing Prediction阶段运行
- **THEN** FootPlacement MUST向唯一FBBIK ABI提交三个零权重Goal并保持原动画姿势
- **AND** MUST不让Current Grounding或其它owner修改脚或骨盆

### Requirement: 平地Animation Foot Route必须先与Native Sole对齐

系统 MUST在进入Ground Query前，按同一权威Action Phase证明Animation Foot Route可重建Native Sole的XZ、旋转和Landing端点。Gizmo MUST绘制完整Route与当前Phase采样点。实际Foot Motion与该采样不一致时，Plan MUST无效。

#### Scenario: 平地直线Locomotion

- **WHEN** 角色在平地以不变committed trajectory执行一步
- **THEN** Native Sole、Animation Foot Route当前采样和最终Sole的平面误差 MUST保持在鞋底几何容差内
- **AND** Route、Plan和Goal owner MUST不发生无输入原因的换代

### Requirement: 有效转向必须重新查询并重建Ground Envelope

系统 MUST在committed trajectory generation或有效移动方向改变时创建Revision。Revision MUST重新计算Landing、重新执行Physics采样，并重新构造Edge Plane、Reachability和Ground Envelope。系统 MUST不旋转或平移旧Foot Route、旧命中点、旧Surface法线或旧Hull冒充新地形结果。

#### Scenario: A/D输入改变角色移动方向

- **WHEN** 当前Step仍在Swing且committed trajectory方向发生有效改变
- **THEN** 系统 MUST生成新Revision并执行新的Landing与路径查询
- **AND** 新Gizmo Path MUST只来自新查询结果
- **AND** 旧Plan MUST保持不可变直到新Plan成功交接

#### Scenario: Revision查询失败

- **WHEN** 新方向没有合法Landing或Ground Envelope
- **THEN** 系统 MUST保留旧Plan作为交接旧侧并发布Rejected原因
- **AND** MUST不旋转旧Path、不清空旧Path、不让响应式Grounding接管Swing
