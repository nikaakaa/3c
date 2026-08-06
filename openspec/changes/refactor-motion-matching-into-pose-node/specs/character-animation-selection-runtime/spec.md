# character-animation-selection-runtime Specification

## MODIFIED Requirements

### Requirement: 持续Pose source与有限Action必须使用不同ABI

Sequence与BlendSpace等外部持续source MUST继续通过正式Pose source ABI被对应Player消费；有限Action MUST继续通过Action playback ABI进入AnimationSlot。Motion Matching不再属于外部Pose source ABI：`MotionMatchingPose` MUST在节点内部消费数据库source并直接输出Local Pose。系统 MUST删除`CharacterMotionMatchingPoseSourceSlot`和MM `PresentationPoseSourceSample`，并 MUST不把MM选择伪装为有限Action或普通外部source。

#### Scenario: MM与Attack同时存在

- **WHEN** Grounded基础Pose来自MotionMatchingPose且Attack通过AnimationSlot播放
- **THEN** MM节点 MUST输出持续基础Local Pose
- **AND** Attack MUST通过独立有限Action lifecycle覆盖或叠加该Pose

### Requirement: Pose Graph必须显式选择source Player和transition owner

Pose Graph MUST为Sequence和BlendSpace显式保存相应Player，并为state transition与Action transition保存明确owner。Motion Matching MUST以单个`MotionMatchingPose`节点同时表达查询、entry player和MM internal Blend owner；图 MUST不再保存`SelectedPosePlayer`或消费MM Slot的显式BlendStack。PoseStateMachine、MM internal Blend Stack和AnimationSlot MUST分别只拥有state transition、MM Jump和有限Action transition。

#### Scenario: 作者配置MM状态

- **WHEN** state-local图选择Motion Matching能力
- **THEN** 作者 MUST配置一个MotionMatchingPose节点及其binding/history依赖
- **AND** MUST不再选择第二个Player或MM transition owner

### Requirement: Source usage、retention与release必须由实际consumer闭环

外部Pose source MUST继续由实际Player consumer发布usage。MM数据库entry source MUST由拥有它的`MotionMatchingPose` internal Blend Stack按真实entry权重、Stored Pose引用和generation发布usage、retention与release。Search Kernel、Chooser、Database Profile和History Collector MUST不伪造consumer usage。

#### Scenario: MM旧entry淡出完成

- **WHEN** 旧entry权重归零且不再被Stored Pose引用
- **THEN** MM节点 MUST释放该entry source token
- **AND** Search Kernel MUST不因候选仍存在而保留该source

### Requirement: Transition Policy必须按明确owner完整编译

每种transition policy MUST绑定唯一owner并完整编译。Pose State policy属于PoseStateMachine，MM Jump Blend Policy属于具体MotionMatchingPose，有限Action policy属于AnimationSlot或对应Action owner。Compiler MUST拒绝同一MM Jump同时配置节点Blend Policy、外部Inertialization或显式BlendStack；MUST不从Profile默认值或其它节点推断缺失policy。

#### Scenario: MM节点缺少Blend Policy

- **WHEN** 可达MM节点未绑定完整Jump Blend Policy
- **THEN** Projection Build MUST失败
- **AND** Runtime MUST不使用固定时长或Immediate fallback

### Requirement: Animancer必须只负责source采样

Animancer或其它source backend MUST只按编译后的entry plan采样AnimationClip Pose并报告精确source lifecycle。MM搜索、Chooser、Continue/Jump决策、Blend权重、Stored Pose、Pose History和Gameplay状态 MUST不由Animancer State或TransitionLibrary拥有。

#### Scenario: MM节点采样两个entry

- **WHEN** internal Blend Stack有两个非零权重entry
- **THEN** source backend MUST按两个明确source time提供Pose
- **AND** 统一Blend Stack Kernel MUST计算最终混合而不是调用第二套Animancer transition

