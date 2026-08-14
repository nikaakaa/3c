## MODIFIED Requirements

### Requirement: Animation Foot Analysis必须拥有Editor-only规范产物

Animation Foot Analysis MUST为`in-place Pose AnimationClip + Rig v4 + Sampling Rig + Calibration v4 + Geometry Validation + Analysis Settings + Analyzer Version`生成不可变Editor-only artifact。Artifact MUST保存同一采样域中的左右脚Plant Confidence、精确Release/Lift-Off/ApproachContact边界、root-local Foot平面路线、鞋底相对平地参考的Clearance、同相位Hip路线、Landing鞋底姿态和逐脚Landing Event。Artifact MUST不保存Action Motion Clip、RootMotionCurveAsset、Action Root路线、运行速度或世界位移。

Artifact identity MUST覆盖全部输入identity、revision、dependency hash、采样率、事件分段、constraint分类、root重建、clearance分解和algorithm version。旧三维Foot Route、只保存Landing Offset或缺少Action Clock域的artifact MUST判为Stale；系统 MUST不提供兼容reader或运行时补建。

同脚Landing Event内的root-local Foot、Ankle、Hip、平面路线与Animation Clearance MUST使用同一Action Phase采样格发布。当前正式schema MUST为这些连续几何保存25个等相位样本并由Projection与Plan原样消费；Constraint、Support、Orientation和Body Pivot MUST由精确`Release / LiftOff / ApproachContact`边界解析，不得量化为25点离散路线。旧7点几何路线或旧离散状态路线 MUST判为Stale，不得在Runtime插值补建、继续读取或只提升部分字段的分辨率。

#### Scenario: Analyzer算法删除Action Root并保留Foot与Clearance分解

- **WHEN** 新algorithm version要求`RootLocalFootPlanarRoute + SoleClearance`且禁止Action Root
- **THEN** 旧artifact MUST变为Stale并要求明确Character Build
- **AND** Runtime MUST不从旧三维Route或Action Root字段继续运行

#### Scenario: Calibration Heel或Toe改变

- **WHEN** Geometry Validation identity变化
- **THEN** Clearance、Landing鞋底姿态和Ankle-to-Sole相关artifact MUST全部失效
- **AND** MUST不只比较数值revision继续复用旧payload

#### Scenario: 落地前脚部存在快速回摆

- **WHEN** 原动画鞋底在`0.83 -> 1.0` Action Phase内先前摆再回到Landing
- **THEN** artifact的25点Foot/Ankle/Hip与Clearance MUST保留该弯折
- **AND** Projection与Plan MUST不把它重新降采样为跨过该弯折的7点直线

### Requirement: 单Clip Analyzer不得依赖Tree或Projection

Analyzer MUST只接受精确in-place Pose AnimationClip、Rig v4、Sampling Rig、Calibration v4、Analysis Settings和Analyzer Version。它 MUST通过独立PlayableGraph完成Pose确定采样，并从完整采样序列生成动作级步态事实。它 MUST不读取Action Motion Clip/Curve、Tree、StateMachine、Timeline call site、Character Definition、Presentation Projection、Scene、Gameplay速度、Body Target Velocity或当前角色Transform，也不得从Plant或Foot轨迹重建世界Root位移。

#### Scenario: 分析In-Place Run Clip

- **WHEN** Pose Clip的Root平面位移为零且左右脚具有有效步态
- **THEN** Analyzer MUST发布root-local Foot、Hip、Clearance和Landing Event
- **AND** MUST不补建Action Root、运行速度或默认步幅

#### Scenario: 整段Clip没有足够支撑事实

- **WHEN** Pose Clip自身包含非零Root平面位移
- **THEN** 当前in-place Analysis合同 MUST明确拒绝该Clip
- **AND** MUST不把Root Motion与Simulation Constant Speed混合

## ADDED Requirements

### Requirement: 每个Landing Event必须原子发布动作级步态事实

每只脚每个Landing Event MUST携带稳定event ordinal、Foot Side、精确Release/Lift-Off/ApproachContact phase、Action Clock domain、root-local Foot平面路线、Sole Clearance、Hip路线、由事件边界唯一解析的Constraint Mode、Support Phase、Foot Orientation Policy、Body Rotation Pivot Mode、Landing鞋底姿态，以及本脚下一次Landing之前的对侧Landing delay、event ordinal、cycle offset与该对侧鞋底在落地帧的root-local位置。所有值 MUST来自同一in-place Pose Clip、同一事件区间和同一采样时钟。

Projection source selection MUST把整份事件作为不可拆分值选择。系统 MUST不分别混合Landing Delay、Landing Offset、Root Route、Foot Route、Clearance、Hip、Constraint、Support或Orientation Policy。

正式Locomotion的Start、Loop、End与MovingTurn若作者化左右脚Marker，Analyzer MUST验证事件按时间严格左右交替；循环片段还 MUST验证首尾事件保持交替。运行时 MUST从本脚原子事件读取对侧配对，不得再独立选择另一只脚的当前Contribution来推导Virtual Ground分割。

#### Scenario: BlendSpace两个sample步相不同

- **WHEN** 两个sample都提供右脚Landing Event但事件相位不同
- **THEN** 最终右脚事件 MUST完整来自一个权威sample contribution
- **AND** Landing身份与所有运动事实 MUST保持同源

#### Scenario: StateMachine目标脚Pose权重暂时为零

- **WHEN** Start到Loop过渡的Blend Profile暂时令目标某只脚的Pose权重为0
- **THEN** 最新Live目标的该脚Landing Event、Phase与Route MUST仍作为唯一离散动作事实发布
- **AND** 退出源与Stored Pose MUST不因目标Pose权重为0重新取得预测时钟所有权
- **AND** Sole速度、高度与Plant MUST继续按逐脚Pose权重混合

#### Scenario: 同脚步幅中包含一次对侧落地

- **WHEN** 本脚当前事件的下一次Landing之前存在对侧Landing
- **THEN** artifact MUST在本脚事件中保存该对侧Landing的delay、ordinal、cycle offset与落地帧root-local鞋底位置
- **AND** Projection选择本脚事件时 MUST同时选择这份对侧配对，不得从另一份混合结果重新拼装

### Requirement: Animation Clearance必须独立于Ground Path和世界高度

Analyzer MUST把鞋底运动分成平面路线与相对动作参考脚下路径的Clearance。Clearance MUST表达动画本身高于脚下路径的非负高度轮廓，不得包含运行时地形高度、Current Support、Future Landing或世界Y。不同坡面和台阶只在Runtime Ground Path中加入。

#### Scenario: 平地Walk摆脚最高10cm

- **WHEN** 动作参考脚下路径为平地且鞋底最高高出10cm
- **THEN** artifact Clearance峰值 MUST约为10cm
- **AND** Runtime上20cm台阶时 MUST能够形成约`20cm + 10cm`的计划鞋底高度

### Requirement: Constraint Mode必须来自同一动作采样域

Analyzer MUST从同一Plant区间和接触过渡生成精确`Release / LiftOff / ApproachContact`边界。Runtime MUST按权威Action Phase与这些边界唯一解析`Locked / Sliding / Unlocked`期望模式；该模式只表达动画意图，不是世界接触事实。Runtime Stance仍必须结合合法Current Support、surface distance、reach和reset裁决最终约束状态。

#### Scenario: 动画脚进入摆动

- **WHEN** Plant Confidence离开稳定接触区且鞋底进入摆动
- **THEN** Constraint Mode MUST进入Unlocked
- **AND** Runtime MUST不因上一帧anchor存在而把分析曲线改写为Locked

### Requirement: 循环与非循环Landing occurrence必须使用不同cycle规则

Runtime source binding MUST显式提供`sourceLooping`。循环source MAY根据`sampleTime + timeToLanding`增加Landing cycle；非循环source MUST使用当前source cycle。非循环末帧Landing可以合法存在，但不得映射成下一cycle。

#### Scenario: 非循环动作的最终右脚落地

- **WHEN** Landing发生在Clip最后一个采样且sourceLooping为false
- **THEN** 事件 MUST使用当前source cycle与稳定event ordinal
- **AND** 同一步计划 MUST不在动作末端因cycle变化重建

#### Scenario: 循环Walk跨过周期边界

- **WHEN** 当前sample位于周期末端且Landing位于下一周期
- **THEN** Landing occurrence MUST使用下一source cycle
- **AND** 下一周期同侧事件 MUST获得不同LandingEventIdentity

### Requirement: Action Step Clock必须由Projection随权威source发布

Projection MUST把Simulation提交的Locomotion Motion Elapsed Ticks映射成当前事件的单调Action Step Clock，并与Pose、Foot Placement Weight和Action Step Fact使用同一effective sample time/cycle。Presentation MAY在相邻Simulation事实间插值，但Sequence与Plan不得通过Presentation Delta重建第二时钟。

#### Scenario: Marker Sync改变Clip采样时间

- **WHEN** source visual time因Marker Sync变化
- **THEN** Pose、Action Step Fact和Step Clock MUST在同一映射后时间求值
- **AND** Runtime Plan Progress MUST不继续沿旧私有Elapsed推进

#### Scenario: Locomotion从有限Start进入循环Loop

- **WHEN** Start与Loop共享同一`Locomotion.Gait`左右脚Marker组
- **THEN** 目标Loop MUST按源Marker段取得同一左右脚相位并重建自己的Simulation Locomotion Clock基准
- **AND** Marker同步过渡 MUST不晚于有限Start最后一个完整左右脚Marker段开始，使两脚下一Landing事实均在各自LiftOff前连续可用
- **AND** 视觉Blend时长 MAY短于该Marker交接提前量；目标Loop的Landing Event、Phase与Route MUST独立于逐脚Pose Blend Weight持续可用，Pose权重只混合Sole速度、高度和Plant
- **AND** 视觉Blend完成后 MUST由同一目标Loop继续剩余Pose与Action Step Fact，不得保留已结束事件或创建预测私有时钟
- **AND** 过渡结束后的下一帧 MUST从该映射相位继续前进，不得重新从0或整段Locomotion elapsed取模

#### Scenario: 诊断负载使Render Frame快于Simulation Tick

- **WHEN** Presentation在两个Simulation locomotion事实之间执行多个Render Frame
- **THEN** Pose、Action Step Fact与Clearance MUST保持在同一Simulation动作时间域
- **AND** MUST不按每个Render Frame的Delta重复推进Landing

### Requirement: Foot Analysis不得拥有角色平面位移

Corin in-place Foot Analysis MUST不保存角色平面位移、速度或Action Motion Curve。Simulation Locomotion MUST唯一拥有作者Move Speed；Simulation/KCC MUST唯一发布按正式Movement Timeline、世界碰撞和Trajectory Curvature生成的未来可执行Body XYZ轨迹。Runtime在计划创建事务中 MAY冻结该轨迹、Movement最大转向能力与实际Trajectory Curvature，并和同一Pose Clip脚骨局部姿态差定义Future Query Route；脚骨局部姿态序列不是角色位移曲线。最终Swing Foot XZ MUST继续来自当前原动画Pose。计划提交后不得重读Body、键鼠输入幅值、地形三维弧长或当前脚投影改写该路线。

#### Scenario: 同一Walk动作运行在平地与楼梯

- **WHEN** 同一Landing Event分别在平地和楼梯建立计划
- **THEN** 两次计划的Pose event identity、cycle、相位、创建帧Body Target Velocity、Simulation Continuation、最大转向能力与Trajectory Curvature MUST可明确对账
- **AND** Runtime在平地与楼梯 MUST从同一Landing事件、冻结KCC轨迹和Trajectory Curvature建立Future Query Route；当前原动画Pose继续拥有最终Foot XZ，楼梯竖直变化只进入Ground Path高度和预测Hip
- **AND** Analyzer MUST不读取任何Body速度；Runtime只可在Plan创建事务中请求冻结KCC未来轨迹，Visible或Source速度不得进入预测

### Requirement: Definition Build必须精确消费新Artifact并发布Projection

Definition Build MUST校验所有可达source的新artifact format、algorithm、Rig、Calibration与Geometry Validation identity，并把Action Step Fact所需固定容量root-local曲线发布进Projection。任一旧format、残留Action Motion/Root字段、缺失Clearance、缺失Constraint、缺失Hip或Action Clock域不匹配 MUST阻止发布。

#### Scenario: Corin Projection仍引用旧Foot Analysis

- **WHEN** artifact只包含三维RootLocalFootRoute而缺少Planar/Clearance分解
- **THEN** Character Build MUST失败并报告精确source binding
- **AND** MUST不发布部分新schema或使用旧Projection继续运行
