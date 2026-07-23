## ADDED Requirements

### Requirement: Inertialization必须是显式局部单Pose节点

Pose Graph MUST提供单Pose输入、单Pose输出的`Inertialization`节点。节点 MUST只影响直接连接的局部分支，MUST不通过全局request bus、OutputPose自动注入或图外postprocess作用于其它分支。

#### Scenario: Locomotion局部惯性化

- **WHEN** BaseLocomotion Player连接Inertialization而Action分支不连接
- **THEN** Locomotion source jump MUST只更新该节点残差
- **AND** Action分支 MUST不获得同一惯性请求

### Requirement: Player必须发布typed Pose Discontinuity事实

`SelectedPosePlayer` MUST在source identity或selection generation发生离散变化时，随同一completion发布`PoseDiscontinuity`。该事实 MUST包含稳定event identity、前后endpoint、前后continuity、原因与reset语义，MUST不包含duration、curve、weight、旧Pose或Gameplay状态。连续sample MUST不产生新event。

#### Scenario: MM plan继续

- **WHEN** MM在同一generation内推进连续sample
- **THEN** Player MUST保持continuity且不发布新Discontinuity

#### Scenario: MM跳到新sample

- **WHEN** MM提升selection generation
- **THEN** Player MUST发布绑定新Pose的唯一Discontinuity

### Requirement: Inertialization Policy必须完整覆盖直接Player endpoint pair

每个Inertialization节点 MUST引用唯一`CharacterPoseInertializationPolicy`。Compiler MUST枚举直接上游Player全部可达endpoint pair，并把authoring default与override物化为完整`HardCut | Inertialize` exact table。Inertialize rule MUST包含duration、canonical curve、dense per-bone Blend Profile与完整Pose Parameter filter。Runtime缺少pair MUST失败且不得fallback。

#### Scenario: 可达pair缺失

- **WHEN** 某个Player endpoint pair无法物化exact rule
- **THEN** Compiler MUST失败并定位Inertialization PoseNodeId与pair

### Requirement: Inertialization必须从上一份完成输出建立每骨骼残差

节点 MUST保存上一份exact completed output及按真实Presentation delta求得的dense velocity。合法Inertialize event到达时，节点 MUST从上一完成输出相对当前target计算position、Quaternion最短弧Log rotation、scale及其velocity residual，并按canonical curve、duration和dense BoneId multiplier衰减到零。首份Pose MUST只建立history。

#### Scenario: 第一份合法Pose

- **WHEN** 节点没有上一份完成history并收到合法Pose
- **THEN** 节点 MUST原样输出target并建立history
- **AND** MUST不从Bind Pose或零Pose构造残差

### Requirement: 连续中断必须原子rebase单一Accumulator

活跃Inertialization期间收到新的合法Discontinuity时，节点 MUST以上一份已修正完成输出为Previous、当前Player Pose为Target重新计算残差，提升Accumulator generation并替换旧Accumulator。节点 MUST不叠加Accumulator、不恢复旧source，也 MUST不创建私有Blend Stack。

#### Scenario: 惯性衰减期间再次跳转

- **WHEN** 当前residual尚未结束且Player发布新Discontinuity
- **THEN** 新capture边界输出 MUST与上一完成输出连续
- **AND** 旧Accumulator MUST被原子替换

### Requirement: Optional Pose与Reset不得伪造惯性目标

Inertialization MUST只在`Pose -> Pose`且前后均合法时执行。Initialization、Presentation Reset、branch replacement、非连续Preview seek、Invalid、NoPose边界 MUST清理或重建history并按typed HardCut/propagation规则处理。Runtime MUST不使用Bind Pose、上一帧缓存或Empty伪造target。

#### Scenario: NoPose进入Pose

- **WHEN** 节点从NoPose收到第一份合法Pose
- **THEN** 节点 MUST建立新history并原样输出Pose
- **AND** MUST不执行惯性进入

### Requirement: 参数与Foot Feature必须按节点实际包络传播

Policy MUST为每个可达Pose Parameter显式声明`Inertialize | Snap`。Inertialize参数 MUST按节点output envelope连续，Snap参数 MUST立即使用target值。左右脚Foot Feature MUST按对应Bone envelope传播；Accumulator MUST不成为伪producer、伪clip或Gameplay contact。

#### Scenario: 离散参数切换

- **WHEN** Policy把一个离散参数声明为Snap
- **THEN** 输出 MUST在Discontinuity边界立即采用target值

### Requirement: Inertialization必须位于native Pose阶段且早于FootPlacement

Compiler MUST证明Pose与Discontinuity来自同一直接Player identity，节点位于native Pose阶段并早于FootPlacement/world-aware IK。Runtime MUST在唯一PlayableGraph Evaluate中完成节点job，不得在FootPlacement后第二次写骨骼。

#### Scenario: 节点位于FootPlacement之后

- **WHEN** 作者把Inertialization连接在FootPlacement输出之后
- **THEN** Compiler MUST失败并定位非法阶段边

### Requirement: Inertialization调试必须只读解释节点状态

Snapshot MUST按PoseNodeId显示InputPlayerNodeId、Discontinuity event、endpoint、rule、HardCut/Capture/Continue/Rebase/Complete/Reset/Invalid、elapsed、duration、选定Bone residual与envelope、Accumulator generation及history/output completion。Preview与Live Debug MUST不重新采样Clip或计算残差。

#### Scenario: 查看一次rebase

- **WHEN** 作者选择活跃Inertialization节点和BoneId
- **THEN** Snapshot MUST解释前后endpoint、rebase generation与该骨骼残差

