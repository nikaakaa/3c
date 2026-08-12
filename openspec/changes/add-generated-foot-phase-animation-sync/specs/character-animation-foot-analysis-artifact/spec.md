## ADDED Requirements

### Requirement: Foot Analysis必须生成只用于时间对齐的双脚同步描述

Animation Foot Analysis MUST在同一次完整Clip采样中生成Editor-only `AnimationFootSynchronizationDescriptor`。描述 MUST按同一sample clock保存左右脚root-local sole平面位置、相对Calibration地面的sole高度、sole local velocity与Plant Confidence，并 MUST与artifact的Clip、Rig v4、Sampling Rig、Calibration v4、Geometry Validation、sample rate和algorithm identity原子绑定。Analyzer MUST复用已经完成的heel、toe与sole采样，不得为同步描述建立第二PlayableGraph、第二Analyzer或按动画名称推断脚侧。

同步描述 MUST只用于Character Build编译source时间映射；它 MUST不成为可编辑Curve Channel、Marker、FootPhase资产、Blackboard字段、Gameplay事实、Runtime contact来源、Snapshot或Network字段。现有contact Marker candidate MUST继续作为Editor session瞬时建议，不得写入同步描述。

#### Scenario: In-place Walk与Run生成同步输入

- **WHEN** Walk与Run的Animation Root平面位移为零但左右脚在Visual Root局部空间具有合法交替运动
- **THEN** Analyzer MUST保留两只脚的root-local sole平面轨迹、速度、高度与Plant Confidence
- **AND** MUST不从Gameplay速度、Body位移或其它Clip补全同步描述

#### Scenario: Foot Analysis输入发生变化

- **WHEN** Clip import dependency、Rig、Sampling Rig、Calibration、Geometry Validation、sample rate或同步算法变化
- **THEN** 旧同步描述 MUST随同一artifact整体变为Stale
- **AND** Store MUST不单独复用旧同步描述或旧普通Foot Feature

### Requirement: Projection Compiler必须编译确定性Foot Phase Time Warp

当可达source relation明确选择`GeneratedFootPhase`时，Projection Compiler MUST读取两侧精确匹配的Foot Analysis artifact和Marker segment occurrence，为每个实际可达leader/follower occurrence组合编译固定容量`AnimationFootPhaseTimeWarpPlan`。Compiler MUST在每个Marker区间内规范化两只脚的位置、高度、速度与Plant Confidence，以端点固定、索引单调、稳定tie-break的确定性序列对齐生成`leader fraction -> follower fraction`映射，并把algorithm identity、两侧artifact hash、source identity、marker pair、occurrence与严格单调knots编入Projection。

编译计划 MUST只覆盖PoseState relation、AnimationSlot可达Action pair与Blend Space固定Phase Reference关系，不得生成按名称扫描的全动画库pair table。缺失、Stale或Corrupt artifact、非法Calibration尺度、样本不足、非单调路径、reduction误差或容量超限 MUST阻止发布；Compiler MUST不改用线性Marker比例或normalized time。

#### Scenario: Walk到Run使用生成式映射

- **WHEN** Walk与Run共享合法MarkerGroup、Time Mapping均为`GeneratedFootPhase`且两份artifact Ready
- **THEN** Projection MUST为解析后的leader方向和每个匹配segment occurrence发布warp plan
- **AND** Runtime MUST不需要读取artifact或AnimationClip即可计算target effective time

#### Scenario: GeneratedFootPhase缺少target artifact

- **WHEN** relation两侧Marker完整但target Foot Analysis artifact Missing或Stale
- **THEN** Character Build MUST失败并定位source owner、target owner与artifact identity
- **AND** MUST不发布只含MarkerSegmentFraction的降级计划

#### Scenario: Blend Space使用固定Phase Reference

- **WHEN** Blend Space的Reference Sample与一个Dynamic Sample明确选择GeneratedFootPhase
- **THEN** Compiler MUST使用同一warp compiler和plan格式生成Reference到Sample的映射
- **AND** Blend Space Runtime MUST不复制foot cost或动态规划算法
