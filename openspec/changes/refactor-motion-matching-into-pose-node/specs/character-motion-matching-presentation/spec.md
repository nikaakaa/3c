# character-motion-matching-presentation Specification

## RENAMED Requirements

- FROM: `### Requirement: Motion Matching必须是Animation Presentation Profile装配的正式Pose Source`
- TO: `### Requirement: Motion Matching必须是Animation Presentation Profile装配的正式Pose节点`
- FROM: `### Requirement: Motion Matching必须降低为state-local Pose source sample`
- TO: `### Requirement: Motion Matching必须编译为state-local Pose程序`
- FROM: `### Requirement: Motion Matching验证必须使用独立正式配置且不得修改Corin`
- TO: `### Requirement: Motion Matching必须使用独立正式角色Prefab完成内容闭包`

## ADDED Requirements

### Requirement: Motion Matching展示必须由新增角色Prefab完整装配

项目 MUST新增`MotionMatchingDemoCharacter.prefab`作为首个正式MM内容载体。该Prefab MUST通过标准`CharacterPipelineHost`引用自己的Character Pipeline Definition；Definition MUST引用自己的Animation Presentation Profile，Profile MUST引用自己的Pose Graph、唯一Presentation Rig、MM Profile、Chooser和正式生成物。该Prefab MUST复用现有Session、CharacterPipeline、Pose Compiler、Search Kernel和Blend Stack Kernel，不得挂第二MM Runtime、MxMAnimator、自主Player或shadow skeleton。Corin现有Definition、Presentation Profile、Pose Graph、Rig与生成物 MUST不由本change修改。

#### Scenario: 装配新增MM角色Prefab

- **WHEN** 作者检查`MotionMatchingDemoCharacter.prefab`
- **THEN** MUST能沿Prefab、Definition、Presentation Profile、Pose Graph和MM binding追踪完整正式链
- **AND** Prefab MUST不包含绕过CharacterPipeline的MM组件或Animator Controller路径

#### Scenario: 新角色使用不同骨架

- **WHEN** 新Prefab的GASP目标骨架与Corin不同
- **THEN** 新Prefab MAY拥有自己的正式Presentation RigId与Revision
- **AND** 该Prefab的FeatureSchema、Database、SourceSet和全部生成物 MUST只闭合到这一个Rig identity

## MODIFIED Requirements

### Requirement: Motion Matching必须是Animation Presentation Profile装配的正式Pose节点

Motion Matching MUST由`CharacterAnimationPresentationProfile`中的typed binding装配，并由Pose Graph中的`MotionMatchingPose`节点消费。节点 MUST直接输出Local Pose并内部拥有搜索、source playback和Jump Blend。Profile binding MUST引用MM Profile、Chooser、SearchDomain与generated artifacts；MUST不发布MM Pose Source Slot给外部Player，也 MUST不保存独立fixture或fallback source。

#### Scenario: Profile装配Motion Matching节点

- **WHEN** `MotionMatchingDemoCharacter` Grounded state引用一个MotionMatchingPose节点
- **THEN** 节点 MUST从该角色Presentation Profile解析唯一正式binding
- **AND** 不同Profile或未绑定节点 MUST不共享selection state

### Requirement: Motion Source采样兼容性必须显式且在目标Rig上证明

每个Motion Source MUST通过显式SourceSet登记并在唯一Presentation Rig上证明采样兼容性。Presentation Profile Rig、MM FeatureSchema Rig、Database TargetRig、SourceSet TargetRig及所有artifact binding MUST具有相同RigId和Revision。Humanoid Avatar类型相同、骨骼名称相同或Retarget可行 MUST不替代该闭包。

#### Scenario: GASP Humanoid clip进入新增角色SourceSet

- **WHEN** 作者把GASP Humanoid clip登记到`MotionMatchingDemoCharacter` Grounded SourceSet
- **THEN** Analysis Build MUST使用该角色唯一Presentation Rig的正式采样binding
- **AND** 缺失或旧revision binding MUST阻止数据库生成

### Requirement: Pose History必须只记录匹配MM节点的正式Pose结果

Pose History MUST由Pose Graph中的显式`PoseHistoryCollector`记录对应MM节点已完成的基础Local Pose。History MUST包含frame、Rig、source与completion lineage，并 MUST排除AnimationSlot、Root/World修正、Foot Placement和IK结果。MM搜索 MUST只读取上一帧已完成history page，不得读取同帧未提交Pose。

#### Scenario: Action覆盖MM Pose

- **WHEN** 本帧Attack Slot覆盖Grounded MM基础Pose
- **THEN** Collector MUST记录Slot之前的MM基础Pose
- **AND** 下一帧query MUST不把Attack手臂姿势当作locomotion history

### Requirement: MM source identity必须完整表达provider与Selection Generation

每个MM internal entry identity MUST完整表达MM node identity、Profile、Chooser result、Database、SourceSet、clip/source、segment、sample time、selection generation、Rig lineage和artifact revision。该identity MUST由MotionMatchingPose owner用于采样、entry processing、blend、usage与release；MUST不再通过`CharacterMotionMatchingPoseSourceSlot`跨节点传递。

#### Scenario: Jump建立新entry identity

- **WHEN** 搜索从当前sample Jump到另一个database segment
- **THEN** 新entry MUST保存完整source与generation lineage
- **AND** internal Blend Stack MUST使用该identity采样和释放

### Requirement: Motion Matching必须编译为state-local Pose程序

Projection Compiler MUST把MotionMatchingPose编译为包含Chooser、Search、entry source capture、entry processing、internal Blend Stack和Local Pose输出的state-local程序。程序 MUST不产生供SelectedPosePlayer消费的中间source sample；外层StateMachine和AnimationSlot MUST只看到完成的Local Pose Value。

#### Scenario: 编译Grounded MM state

- **WHEN** Grounded inline graph包含合法History与MotionMatchingPose
- **THEN** Projection MUST生成一个node-local MM程序和固定workspace
- **AND** MUST不生成SelectedPosePlayer opcode或显式MM BlendStack opcode

### Requirement: MM运行时必须响应Presentation分支重置

每个MotionMatchingPose节点 MUST按自身relevance、binding revision、Rig revision、Preview seek和明确reset命令原子重置query、entries、Stored Pose、history lease和source usage。共享Frame Context或Search Kernel MUST不统一清除同Actor其它节点。重置后首次选择 MUST遵循Profile initial selection规则，不得恢复旧Slot sample。

#### Scenario: Rig revision切换

- **WHEN** Presentation Profile绑定的Rig revision变化
- **THEN** 所有引用旧revision的MM node state和artifacts MUST失效
- **AND** Runtime MUST不把旧entry迁入新Rig

### Requirement: Motion Matching必须提供完整只读诊断与Search Replay

Diagnostics和Search Replay MUST记录typed Frame Context、Chooser rule/result、History view、query features、admission、cost、plan、Continue/Jump、entry identity、entry program、blend weights和最终Pose completion。工具 MUST通过正式MM node program重放查询解释，但 MUST不创建独立player、shadow history或独立validation profile。

#### Scenario: 重放一次异常Jump

- **WHEN** 作者从Trace选择某次Jump
- **THEN** Replay MUST显示当时Chooser集合、候选成本与continuity条件
- **AND** MUST能关联到产生Pose的node、generation和artifact identity

### Requirement: Motion Matching必须使用独立正式角色Prefab完成内容闭包

首个正式MM内容闭包 MUST使用`MotionMatchingDemoCharacter` Prefab、专属Character Pipeline Definition、Presentation Profile、唯一Rig、正式Pose Graph、typed Chooser和明确SourceSets。Grounded首期范围 MUST只包含业务事实能够解释的Idle、Walk、Run和Sprint；Crouch、Airborne、Slide和Traversal在缺少对应正式Gameplay/Root Motion合同前 MUST不进入Chooser数据库。系统 MUST不把孤立validation fixture或“无合适动画”配置当作正式角色接入，也 MUST不为此改写Corin。

#### Scenario: 构建新增角色Grounded数据库

- **WHEN** `MotionMatchingDemoCharacter`唯一Rig revision稳定且作者显式执行Analysis Build
- **THEN** Build MUST从登记的GASP Grounded SourceSets生成Profile内数据库artifacts
- **AND** Coverage MUST按Idle、Walk、Run、Sprint正式范围报告

#### Scenario: 仅按文件名发现Jump素材

- **WHEN** GASP目录存在名称包含Jump的动画但新角色没有对应MM业务事实和状态合同
- **THEN** 自动或显式SourceSet装配 MUST不把它加入Grounded数据库
- **AND** Chooser MUST不从文件名推断Airborne状态
