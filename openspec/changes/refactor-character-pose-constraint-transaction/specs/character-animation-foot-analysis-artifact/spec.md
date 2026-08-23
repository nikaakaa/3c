## ADDED Requirements

### Requirement: Foot Analysis必须证明Predictive Constraint锁入与释放连续性

Projection Build MUST对全部接入Predictive Foot Placement的可达循环Locomotion Clip及Blend Space Dynamic Sample，使用精确匹配的Foot Analysis Artifact验证每个左右脚Landing Event的Constraint与Support连续区间。每个正式Event MUST具有稳定Event identity、FootDown前接近0的Constraint起点、连续单调上升到完整锁定的区间、非零Support区间，以及FootUp时连续单调下降回接近0的释放区间。

连续性门槛 MUST属于versioned compiler algorithm，不得成为Runtime Contact Duration、Transition HalfLife或素材专属可调补偿。Artifact缺失、事件覆盖不完整、Constraint跳过锁入区间、未达到完整锁定、Release缺失、左右脚事件重叠非法或实际source coverage截断正式区间时 MUST阻止Projection发布；Runtime不得用固定0.12秒、默认曲线、旧GoalTransition或Safety Release替代正常动画Transition。

#### Scenario: 循环Run的左脚Constraint完整

- **WHEN** 左脚Event在FootDown前从接近0连续上升到1，保持非零Support，并在FootUp连续下降回接近0
- **THEN** Projection Build MUST发布该Event的稳定Biomechanical Step Constraint/Support计划
- **AND** Runtime Landing与正常Release MAY直接使用该计划作为单调Transition进度

#### Scenario: Constraint在FootDown从0跳到1

- **WHEN** 可达循环Locomotion Clip的某脚Constraint没有连续锁入覆盖而在相邻采样间从接近0直接跳到1
- **THEN** Projection Build MUST拒绝该素材并报告Clip、脚侧、Event identity和缺失区间
- **AND** MUST不生成固定Contact Duration或运行时平滑补偿

#### Scenario: 有限source出口截断Release

- **WHEN** Clip总时长包含完整FootUp，但正式PoseState source coverage在Release前结束
- **THEN** 质量校验 MUST按实际coverage判定Predictive Constraint合同不完整
- **AND** MUST不使用coverage外样本证明该Event可正常释放

### Requirement: Foot Analysis必须发布空间Foot Path采样所需的脚部事实

Projection中每脚Biomechanical Step计划 MUST继续携带稳定Landing Event、RootLocalLanding、Constraint、Support与Phase事实；Runtime Swing空间进度 MUST从同帧原生Animated Sole和两次世界Landing计算，不得由Artifact发布预计算世界Path进度、FootPath Correction、Ground Height或IK Goal。Foot Analysis只证明动画接触时序和提供root-local事实，不拥有Runtime地形、Path Transition或FrozenPatch。

#### Scenario: Runtime在不同台阶显示同一Run Clip

- **WHEN** 同一个已验证Run Clip分别在平地与楼梯运行
- **THEN** 两次运行 MUST消费相同的Biomechanical Step Event/Constraint/Support计划
- **AND** 各自的Swing空间进度、Ground Envelope和FrozenPatch MUST只由当前Presentation Frame与World Query生成
