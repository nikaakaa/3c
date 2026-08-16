## MODIFIED Requirements

### Requirement: Animation Foot Analysis必须拥有Editor-only规范产物

Animation Foot Analysis MUST为`in-place Pose AnimationClip + Rig v4 + Sampling Rig + Calibration v4 + Geometry Validation + Analysis Settings + Analyzer Version`生成一个不可变Editor-only规范Artifact。现有`AnimationFootAnalysisArtifact` MUST原地升级为Biomechanical Step schema，不得建立第二资产或并行reader。

Artifact identity MUST覆盖全部输入identity、revision、dependency hash、采样域、事件分段、位置与旋转重建、clearance、constraint、support leg、orientation、body pivot、对侧Landing配对和algorithm version。旧format v26、位置-only route、缺少重建门禁或缺少Action Clock域的artifact MUST判为Stale；系统 MUST不提供兼容reader、字段默认值、runtime补建或旧Projection继续运行。

连续路线 MUST使用由schema固定的同一Action Phase采样域。采样数 MUST属于algorithm identity而不是作者配置；若当前采样数无法满足固定重建容差，实施 MUST提升format与algorithm并整体重建，不得只提高某个字段或在Runtime补点。

#### Scenario: Analyzer升级为Biomechanical Step schema

- **WHEN** 当前Library仍保存format v26的Foot/Ankle/Hip位置路线
- **THEN** Store MUST把旧artifact判为Stale并要求明确重建
- **AND** Runtime与Projection Build MUST不从旧位置-only payload继续运行

#### Scenario: Calibration鞋底几何改变

- **WHEN** Heel、Toe、Sole Frame或Geometry Validation identity变化
- **THEN** 位置、旋转、Clearance、Support Leg、Landing和Pivot相关payload MUST全部失效
- **AND** MUST不只比较数值revision继续复用旧artifact

### Requirement: 单Clip Analyzer不得依赖Tree或Projection

Analyzer MUST只接受精确in-place Pose AnimationClip、Rig v4、Sampling Rig、Calibration v4、Analysis Settings与Analyzer Version。它 MUST通过独立PlayableGraph完成确定采样，并从完整采样序列生成Biomechanical Step事实。它 MUST不读取Tree、StateMachine、Timeline call site、Character Definition、Presentation Projection、Scene、Gameplay速度、Body Target Velocity、KCC路径、Camera输入或当前角色Transform，也不得从Foot轨迹、Plant或输入幅值重建世界Root位移。

Analyzer MUST采样左右Heel、Toe、Sole、Ankle、Knee与Hip的root-local位置，以及Sole与Ankle的root-local旋转。所有语义 MUST来自同一Pose evaluation和同一Rig/Calibration identity。

#### Scenario: 分析Corin in-place Run Clip

- **WHEN** Pose Clip的Root平面位移为零且左右脚具有有效步态
- **THEN** Analyzer MUST发布root-local位置、旋转、Clearance、Constraint、Support Leg、Pivot与Landing Event
- **AND** MUST不补建Move Speed、Action Motion Curve、默认步幅或世界Body路线

#### Scenario: 输入Clip包含正式Root平面位移

- **WHEN** 当前Analysis Source声明in-place但Clip采样得到非零Root平面位移
- **THEN** Analyzer MUST明确拒绝该Clip
- **AND** MUST不把Root Motion与Simulation移动混合为第二位移来源

### Requirement: Definition Build必须精确消费Artifact并发布Projection

Definition Build MUST校验所有可达source的新artifact format、algorithm、Rig、Calibration、Geometry Validation、Flat Reconstruction、event continuity与payload完整性，并把固定容量Biomechanical Step Event发布进Projection。任一旧format、重建误差超限、残留Action Root字段、缺失位置或旋转路线、缺失Clearance、Constraint、Support Leg、Pivot、Orientation、对侧Landing或Action Clock域不匹配 MUST阻止发布。

Definition Build MUST删除旧Projection payload和旧generated产品，不得同时发布v26与新schema，也不得以旧产品继续启动Runtime。

#### Scenario: Corin Projection引用旧Foot Analysis

- **WHEN** 任一Start、Loop、Stop或MovingTurn source只具有v26 payload
- **THEN** Character Build MUST失败并报告精确source binding与缺失字段
- **AND** MUST不发布部分新schema或保留旧Projection作为fallback

#### Scenario: 全部可达source通过重建门禁

- **WHEN** 所有Biomechanical Step Artifact identity与Flat Reconstruction结果合法
- **THEN** Float32与Fixed Projection MUST发布相同事件schema和source identity
- **AND** Player Runtime MUST只消费该Projection，不读取Library或AnimationClip补建

## ADDED Requirements

### Requirement: 每个Landing Event必须原子发布Biomechanical Step事实

每只脚的每个Landing Event MUST作为一个不可拆分值发布以下事实：

- 稳定Source、Cycle、Event Ordinal、Landing Event identity与Foot Side；
- 精确Release、LiftOff、ApproachContact、Landing phase和Action Step duration；
- root-local Heel、Toe、Sole、Ankle、Knee、Hip位置路线；
- root-local Sole与Ankle旋转路线；
- Animation Foot Planar Route与Animation Clearance；
- Constraint Mode区间与Constraint Weight；
- Support Weight、Support Leg Length、Compression Reserve与Knee Bend Plane；
- Support Foot Pivot位置与权重；
- Foot Orientation Policy；
- 本脚下一Landing前的权威对侧Landing identity、time、cycle和root-local Sole pose。
- 同脚下一Landing的完整Incoming Step Event、Clock、路线与全部Biomechanical事实，以及相对当前Artifact采样点的Landing time。

Projection source selection MUST完整选择一个事件。系统 MUST不分别混合Landing、路线、Clearance、Constraint、Support Leg、Orientation或Pivot。Pose MAY连续Blend，但离散Biomechanical Step事实 MUST保持同源。

Analyzer MUST在Artifact Build时同时生成Current与Incoming Step。Runtime MUST只按同一effective sample time读取二者并绑定同一source occurrence，不得扫描未来曲线、缓存后继候选或从Current Step补建Incoming。

#### Scenario: Blend的两个source具有不同右脚事件

- **WHEN** Start与Loop同时贡献Pose且右脚Landing identity不同
- **THEN** 最终右脚Biomechanical Step Event MUST完整来自一个权威目标source
- **AND** MUST不从两个source分别取得路线、Clock、Support Leg或Pivot

#### Scenario: 目标source某脚Pose权重暂时为零

- **WHEN** Blend Profile暂时令目标脚Pose贡献为0但目标source已成为当前状态事实
- **THEN** 目标source的Landing Event与Action Step Clock MUST继续作为唯一离散事实发布
- **AND** 退出source、Stored Pose与Inertial History MUST不重新取得事件所有权

### Requirement: Artifact必须通过Flat Reconstruction Gate

Analyzer MUST使用同一Action Phase从Artifact重建Heel、Toe、Sole、Ankle、Knee、Hip位置及Sole、Ankle旋转，并与原AnimationClip采样结果逐相位比较。Artifact MUST保存或伴随可精确复算的最大误差、P95误差、路线弧长误差、侧向范围误差、Landing端点误差和事件相位误差。

固定容差 MUST属于Analyzer algorithm identity，不得作为角色临时调参。任一必需语义超过容差 MUST使artifact Invalid并阻止Projection Build。

#### Scenario: 平地动画路线长度被重建成约两倍

- **WHEN** Artifact重建的Sole planar route弧长或Landing端点明显偏离原AnimationClip
- **THEN** Flat Reconstruction Gate MUST失败并报告脚侧、事件、语义与误差
- **AND** 系统 MUST不进入Ground Query或通过运行时对齐掩盖该错误

#### Scenario: 位置正确但Sole旋转错误

- **WHEN** Sole位置满足容差而旋转角误差超限
- **THEN** Artifact MUST仍判为Invalid
- **AND** Foot Orientation与Landing MUST不使用该payload

### Requirement: Animation Clearance必须独立于Ground Path和世界高度

Analyzer MUST把动画Foot运动分成平面路线与相对参考Foot Path的Clearance。Clearance MUST表达动画本身高于Foot Path的非负高度轮廓，不得包含运行时地形高度、Current Support、Future Landing、KCC Y或世界Y。

#### Scenario: 平地Swing净空为10cm

- **WHEN** 原动画Sole相对参考Foot Path最高为0.10m
- **THEN** Artifact Clearance峰值 MUST约为0.10m
- **AND** Runtime上0.20m台阶时 MUST能形成约`GroundPath + 0.10m`的候选高度

### Requirement: Constraint、Support Leg与Body Pivot必须来自同一动作采样域

Analyzer MUST从完整Plant、Sole位置/旋转与腿部Pose序列生成精确Locked、Sliding、Unlocked区间和连续Constraint Weight。它 MUST同时生成Support Weight、Hip到Ankle支撑长度、Compression Reserve、Knee Bend Plane以及Support Foot Pivot位置与权重。上述事实只表达动画意图；Runtime仍 MUST使用唯一Current Support、surface distance、reach和reset验证世界约束。

Constraint、Support Leg与Pivot MUST使用同一个Landing Event和Action Step Clock，不得由Runtime根据当前Plan状态、速度阈值或LiftOff枚举临时重建。

#### Scenario: 支撑脚在in-place动画中相对Root向后移动

- **WHEN** Foot平面速度较高但Sole仍处于稳定Plant和支撑Pose
- **THEN** Artifact MUST允许该区间保持Locked或Sliding及有效Support Weight
- **AND** MUST不因局部水平速度把它误判为Unlocked

#### Scenario: 身体接近锁定支撑脚旋转

- **WHEN** 动画数据进入Support Foot Pivot有效区间
- **THEN** Artifact MUST发布有限pivot位置与权重
- **AND** Runtime MUST不只从LiftOff前后推导`Pelvis / SupportFoot`二值枚举

### Requirement: Foot Orientation Policy必须区分上坡、下坡与跑步

Biomechanical Step Event MUST发布与动作类型一致的Foot Orientation Policy。正式Policy MUST至少能表达：保留动画、上坡趋于水平、下坡趋于支撑面、跑步关闭坡面orientation。Runtime MAY结合冻结移动方向与Ground Path法线求最终有限旋转，但 MUST不从当前脚高度或动画名称猜策略。

#### Scenario: Running source进入坡面

- **WHEN** 权威Biomechanical Step Event声明跑步保留动画
- **THEN** Runtime MUST不把Foot强制旋转到坡面
- **AND** Policy MUST与该source的其它事件事实保持同源

#### Scenario: Walking source下坡

- **WHEN** 权威Policy允许下坡贴坡且Ground Path下降
- **THEN** Runtime MAY让Foot趋于与支撑面平行
- **AND** MUST受同一Support Leg reach与orientation limit约束

### Requirement: Action Step Clock必须由Projection随权威source发布

Projection MUST把Simulation提交的Locomotion Motion Elapsed Ticks映射成当前事件的单调Action Step Clock，并与Pose、Foot Placement Weight和Biomechanical Step Fact使用同一effective sample time与cycle。Presentation MAY在相邻Simulation事实间插值，但Plan、Constraint、Support Leg、Orientation与Pivot不得通过Presentation Delta建立第二时钟。

Start、Loop、Stop与MovingTurn MUST在LiftOff前发布当前脚PreSwing事件，并保持Marker Epoch、Occurrence、Cycle与Phase连续。

#### Scenario: Start进入Loop

- **WHEN** Start与Loop共享左右脚Marker组并发生Pose Blend
- **THEN** 目标Loop MUST在两脚LiftOff前取得连续Action Step Clock与Biomechanical Event
- **AND** 视觉Blend结束后 MUST从映射相位继续，不得重置到0或复活Start事件

#### Scenario: Render Frame快于Simulation Tick

- **WHEN** 两个Simulation事实之间执行多个Presentation Frame
- **THEN** Pose与Biomechanical Step Fact MUST保持同一Simulation动作时间域
- **AND** MUST不按每个Render Delta重复推进Landing、Constraint或Pivot

### Requirement: Foot Analysis不得拥有角色世界位移

Corin in-place Artifact MUST不保存角色世界平移、运行速度、Action Motion Curve或KCC路径。Simulation/KCC MUST唯一发布未来Body Position、Facing、Linear Velocity和Angular Velocity。Runtime只可在初始Plan或离散Revision创建事务中冻结该trajectory，并与Artifact局部路线组合；已提交Plan不得重读输入幅值、Visible导数、当前Transform或地形弧长改写自身。

#### Scenario: 同一Walk Clip用于直行与A/D圆周移动

- **WHEN** 两次运行使用同一Biomechanical Step Event但Simulation Future Body Trajectory不同
- **THEN** 两次计划 MUST共享相同动画局部路线与事件身份语义
- **AND** 世界位置与朝向差异 MUST只来自各自committed Future Body Trajectory
- **AND** Analyzer MUST不发布第二份角色位移事实
