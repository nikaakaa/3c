# character-animation-layer-runtime Specification

## MODIFIED Requirements

### Requirement: 基础Pose必须由正式state-local source输出

每个可达Pose State MUST通过正式state-local节点输出基础Local Pose。SequencePlayer与BlendSpacePlayer继续消费各自Profile binding；MotionMatchingPose MUST内部消费MM Profile、Chooser、Database artifacts和History并直接输出Local Pose。Runtime MUST不在state图外解析MM source slot、创建SelectedPosePlayer或补建MM BlendStack。

#### Scenario: Grounded状态使用MM

- **WHEN** Grounded state成为当前relevant state
- **THEN** 其MotionMatchingPose MUST成为该state基础Pose owner
- **AND** 下游layer MUST只消费节点输出的Local Pose

### Requirement: 动画帧必须按固定职责顺序执行

动画表现帧 MUST按`typed facts/trajectory -> MM Frame Context -> History Read -> state-local source或MM Search/Entry/Blend -> History Commit -> state transition composition -> finite Action Slot -> 下游Local/Component Pose处理 -> FinalPublication`执行。具体IK拓扑 MAY由已批准的IK capability修改，但MM History Commit MUST始终位于Action、Root/World修正和IK之前。Runtime MUST不通过第二次PlayableGraph Evaluate或LateUpdate补做MM。

#### Scenario: 同帧存在MM与Foot Placement

- **WHEN** Grounded MM和下游Foot Placement都可达
- **THEN** History MUST记录Foot Placement之前的MM基础Pose
- **AND** Foot Placement MUST消费已经完成state/action composition后的正式Pose输入

### Requirement: PoseState source必须按provider demand和state relevance管理

Sequence和BlendSpace provider MUST继续按state relevance获得明确demand。MotionMatchingPose节点 MUST按自身relevance policy决定初始化、暂停、继续或reset其query、entry和history binding状态；共享Frame Context和Search Kernel MUST不替所有节点统一reset。离开relevance后仍需参与state transition的节点 MUST保持到其最终权重归零并完成source release。

#### Scenario: MM State淡出

- **WHEN** PoseStateMachine从Grounded MM state切换到另一个state且旧state仍有非零transition权重
- **THEN** 旧MM节点 MUST按编译policy继续提供Pose或冻结明确计划
- **AND** 只有旧state权重归零后 MAY完成节点reset与source release

### Requirement: 每类连续性必须只有一个明确owner

Pose State连续性 MUST只由PoseStateMachine拥有，Motion Matching selection Jump连续性 MUST只由对应MotionMatchingPose internal Blend Stack拥有，有限Action连续性 MUST只由AnimationSlot/Action owner拥有，source采样连续性 MUST只由实际source player拥有。系统 MUST不为MM Jump同时运行SelectedPosePlayer fade、外接BlendStack、Inertialization或Animancer Transition。

#### Scenario: MM Jump与Action退出同帧发生

- **WHEN** MM搜索Jump且Action Slot正在退出
- **THEN** MM节点 MUST只更新基础Pose内部entries
- **AND** AnimationSlot MUST只处理Action到基础Pose的退出权重

### Requirement: Runtime、Preview和Live Debug必须使用同一事实源

Runtime、Preview和Live Debug MUST从同一角色Prefab引用的Presentation Profile、typed fact frame、Trajectory、Chooser、MM artifacts、Pose Graph Projection和Native Pose Program求值。工具 MAY注入可见的Preview facts，但 MUST不使用孤立MM validation config、硬编码数据库数组、私有player或未被`MotionMatchingDemoCharacter`正式Definition引用的fixture作为发布依据。

#### Scenario: 新角色Preview与Runtime比较

- **WHEN** `MotionMatchingDemoCharacter` Preview与Runtime使用相同frame facts、Trajectory和时间输入
- **THEN** Chooser集合、query plan、selection generation和Pose Program路径 MUST可逐项对账
- **AND** 差异 MUST来自明确数值模式或输入而不是第二配置源
