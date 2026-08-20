## ADDED Requirements

### Requirement: 响应式脚掌模块必须只发布接触提案

响应式脚掌模块 MUST只消费同帧原生 Component Pose、Rig Calibration、Foot Placement Profile、当前 PhysicsScene 和正式自碰撞过滤，并为左右脚分别发布 typed `Reactive Foot Contact Proposal`。Proposal MUST携带 Frame、Completion、Rig、Foot Side、Measurement Revision、Surface Identity、接触点、法线、查询距离、Original Sole/Ankle、Reactive Sole/Ankle、有符号 Component Up 修正和 typed availability。

响应式接触几何 MUST直接由 `Assets/_HoaxGames/iStep/Scripts` 中从 `FootIK.findNewIKPos` 拆出的唯一 `HoaxGames` Contact Solver 实现。该Solver MUST保留原iStep BoxCast、SphereCast、坡面法线修复、命中点回算和脚底高度补偿数学；GameScripts MUST不重新实现或复制第二份等价算法。项目窄Adapter MAY调用该Solver并把iStep Result转换成Proposal。

模块 MUST不读取预测 Landing、Ground Path、Support Lock、Pelvis 或 FinalIK 内部状态，MUST不写 GoalSet、Animator、Transform、VisualRoot、Gameplay Body、KCC 或 Physical Bone。正式调用 MUST不经过 `FootIK.OnAnimatorIK`、iStep Grounded、Body Placement、骨骼writer或Demo类型。

#### Scenario: 当前脚附近存在合法接触面

- **WHEN** 同帧原生脚掌几何、Profile、Calibration 和 PhysicsScene 完整且响应查询命中合法表面
- **THEN** 修改后的iStep Contact Solver MUST计算接触，项目Adapter MUST发布一份具有同 Frame、Completion 与 Rig lineage 的 Reactive Proposal
- **AND** MUST不修改任何骨骼、Goal、Pelvis 或角色 Transform

#### Scenario: 响应式输入不可用

- **WHEN** PhysicsScene、Rig、Calibration、Profile 或当前脚几何不完整
- **THEN** 模块 MUST发布对应 typed rejection
- **AND** MUST不创建默认平面、旧接触或 iStep 组件旁路

### Requirement: 响应式查询必须使用唯一脚掌Footprint事实

每脚正式响应查询 MUST由修改后的iStep Contact Solver使用 Calibration 的 Heel、Toe、Sole Frame 与 `SoleHalfWidth`构造定向 Footprint BoxCast。脚掌前后范围 MUST由 Heel/Toe 得到，横向范围 MUST只来自同一 Calibration，查询高度、距离、坡度与Ground Layer MUST来自 Foot Placement Profile。Solver MUST沿用原iStep查询和命中点回算，并过滤自身 Collider、初始重叠、非有限几何、零法线、负距离和超坡度命中。

BoxCast选出合法主 Surface 后，Backend MAY执行一次受约束 SphereCast修复法线。修复命中 MUST与主命中属于同一 Surface、位于正式修复距离内且法线合法；它 MUST不替代主接触点、选择另一踏面或在BoxCast失败时生成接触。一次响应查询最终 MUST只发布一个Surface、点和法线。

#### Scenario: Footprint命中楼梯踏面

- **WHEN** 修改后的iStep定向BoxCast命中合法踏面
- **THEN** Solver MUST发布该次BoxCast的唯一主Surface
- **AND** MAY只使用同Surface邻域SphereCast结果修复法线

#### Scenario: 法线修复命中另一踏面

- **WHEN** SphereCast命中与主BoxCast不同的Surface或超出正式修复距离
- **THEN** Builder MUST拒绝该法线修复结果并保持主命中法线
- **AND** MUST不把另一踏面作为替代接触

#### Scenario: 原FootIK继续服务iStep Demo

- **WHEN** 原`FootIK`需要执行自己的Demo Animator IK流程
- **THEN** `FootIK` MUST调用同一个修改后Contact Solver取得接触结果
- **AND** MUST不在`FootIK`内部保留第二份`findNewIKPos`算法

### Requirement: 响应式模块必须只稳定测量而不平滑最终Goal

每脚 MUST拥有唯一 Measurement Pending/Committed页，至少保存Measurement Revision、Surface、点、法线和查询距离。Pending MUST从Committed初始化，但当前 Proposal只有在本帧获得合法测量时才可Accepted。同Surface的新点位与法线变化处于正式死区内时 MAY复用Committed测量和revision；超出死区或Surface改变时 MUST发布新的当前测量revision。查询失败时当前Proposal MUST为Rejected，旧Committed测量 MUST不驱动当前Goal或Arbiter。

响应式模块 MUST不保存最终脚Goal、世界绝对锁点、GoalTransition、Pelvis弹簧、外推速度、Reset Lerp或SmoothDamp状态。最终脚与骨盆的连续性 MUST由统一Foot Placement在Proposal裁决之后处理。

#### Scenario: 同一表面的毫米级查询噪声

- **WHEN** 本帧合法测量与Committed测量属于同一Surface且点位和法线变化均在正式死区内
- **THEN** Pending MAY复用Committed测量和Measurement Revision
- **AND** 当前Proposal仍 MUST标记为本帧合法事实

#### Scenario: 查询切换到新踏面

- **WHEN** 本帧合法主命中Surface与Committed Surface不同
- **THEN** Pending MUST立即发布新Surface和新Measurement Revision
- **AND** MUST不以死区或旧测量平滑冻结原踏面

#### Scenario: 当前查询失败

- **WHEN** 本帧没有合法Footprint接触
- **THEN** 当前Proposal MUST为Rejected
- **AND** MUST不沿用Committed接触作为当前事实或执行最终Goal淡出

### Requirement: 响应式状态必须服从Foot Placement外层事务

Measurement Pending页和响应式只读诊断 MUST加入现有Foot Placement `BeginPending -> Seal/Discard`事务。只有Seal可提交Measurement Revision与接触事实；Discard、Body discontinuity、Reset、Retarget和Dispose MUST按现有Foot Placement生命周期恢复或清除状态。响应式事实只属于Presentation，不得进入Character State、World State、Snapshot、Hash或网络packet。

#### Scenario: Foot Placement帧被Discard

- **WHEN** 响应模块已经产生新Measurement但外层表现事务在Seal前Discard
- **THEN** Committed Measurement MUST保持Discard前的逐值状态
- **AND** 下一帧 MUST不得读取被丢弃的Surface、点、法线或revision
