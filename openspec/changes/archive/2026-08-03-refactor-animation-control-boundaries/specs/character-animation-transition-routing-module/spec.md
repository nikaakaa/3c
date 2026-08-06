# character-animation-transition-routing-module Specification

## MODIFIED Requirements

### Requirement: Transition Routing模块必须独立于现有Pose执行链

Transition Routing模块 MUST继续作为target-neutral独立程序集，只处理exact rule编译、Blend Logic选择、typed request生命周期、capture/release握手、reset与诊断。Character Pose Runtime MAY通过显式adapter从PoseState transition或AnimationSlot handoff构造Frame Facts并消费输出；模块 MUST不引用Pose Graph资产、AnimationClip、Animancer、PlayableGraph、SequencePlayer、Slot实现、Bone Mask、FootPlacement、Gameplay Program或Unity对象。Editor Fixture MUST继续与正式角色接入隔离。

#### Scenario: PoseState transition调用模块

- **WHEN** PoseStateMachine已解析source state、target state、readiness和generation
- **THEN** adapter MUST把这些事实转换为模块正式Frame Input
- **AND** 模块 MUST不读取State Rule或State subgraph

#### Scenario: AnimationSlot调用模块

- **WHEN** Slot已解析Source Pose、当前Action和incoming Action或SourcePoseEndpoint
- **THEN** adapter MUST提交稳定endpoint与generation
- **AND** 模块 MUST不读取Action admission、Timeline或Slot weight

### Requirement: Routing Plan必须编译完整exact transition matrix

每个PoseStateMachine与AnimationSlot routing owner MUST从其全部可达endpoint编译完整exact transition matrix。PoseState endpoint MUST使用stable State/source identity，Slot endpoint MUST使用stable Action producer或`SourcePoseEndpoint` identity。缺失pair、重复pair、未知endpoint、非法duration、非法Blend Profile或Inertialization到不支持Source Pose的route MUST编译失败，MUST不生成默认Standard Blend或按名称推断。

#### Scenario: PoseState transition缺少规则

- **WHEN** Locomotion StateMachine存在Start到Locomotion edge但没有合法Blend Logic
- **THEN** Projection Build MUST失败并定位StateMachine和Transition identity
- **AND** Runtime MUST不使用默认CrossFade

#### Scenario: Slot缺少Action到Source Pose规则

- **WHEN** FullBodyAction Slot可达Attack和SourcePoseEndpoint但matrix不完整
- **THEN** Projection Build MUST失败并定位Slot与endpoint
- **AND** MUST不让Action硬切回Source Pose
