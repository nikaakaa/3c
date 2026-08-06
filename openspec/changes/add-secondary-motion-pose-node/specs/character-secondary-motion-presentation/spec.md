# character-secondary-motion-presentation Specification

## Purpose

定义角色次级动画的Profile、Pose节点、Magica Cloth 2实现边界、全局批处理事务、状态生命周期和Corin内容装配。

## ADDED Requirements

### Requirement: Secondary Motion必须是root Pose Graph中的正式节点

Pose Graph MUST提供`SecondaryMotion`节点。节点 MUST接收`pose.local`并输出`pose.local`，且 MUST只允许位于root Pose Graph最终`ComponentToLocalPose`之后和唯一`OutputPose`之前。同一root Pose Graph MUST最多包含一个SecondaryMotion节点；State Pose Graph、Pose Subgraph、Linked Pose Entry与Motion Matching Entry Graph MUST拒绝该节点。Runtime MUST不在图外补建或跳过Secondary Motion。

#### Scenario: Corin接入裙摆后处理

- **WHEN** 作者为Corin root Pose Graph连接`ComponentToLocalPose -> SecondaryMotion -> OutputPose`
- **THEN** Compiler MUST生成同一Pose Plan中的External Physical Pose stage
- **AND** FullBodyIK后的腿部Pose MUST先于裙摆模拟成为节点输入

#### Scenario: Linked Entry创建Secondary Motion

- **WHEN** 作者尝试在Linked Pose Entry创建SecondaryMotion节点
- **THEN** Capability与Validator MUST拒绝该节点上下文
- **AND** MUST不把它提升到root或创建图外组件

### Requirement: Secondary Motion Profile必须唯一表达group与collider语义

`CharacterSecondaryMotionProfile` MUST唯一保存稳定ProfileId、revision、Rig lineage、有序Group与Collider。每个Group MUST保存稳定GroupId、有序root Physical BoneId、连接模式、精确controlled Physical Bone集合、固定/可动规则、Animation Follow、Simulation Weight、约束、碰撞、惯性和reset策略；每个Collider MUST保存稳定ColliderId、Physical BoneId、明确shape、local offset和尺寸。未知Bone、Virtual Bone、跨Rig引用、重复identity、group controlled bone重叠、非法root后代或非有限参数 MUST使Build失败。Profile MUST不保存Transform、GameObject、Magica组件引用、backend选择或fallback。

#### Scenario: 裙摆和头发共享一根controlled bone

- **WHEN** 两个Group声明同一个Physical Bone
- **THEN** Profile Validator MUST报告两个GroupId和冲突BoneId
- **AND** Projection Build MUST失败而不是按顺序覆盖

#### Scenario: Collider引用Virtual Bone

- **WHEN** Collider绑定Rig中的Virtual Bone
- **THEN** Build MUST拒绝该Profile
- **AND** Runtime MUST不按名称寻找同名Transform

### Requirement: 全局Magica设置必须只有一个正式owner

系统 MUST由Gameplay Presentation装配根显式引用唯一`CharacterSecondaryMotionRuntimeSettings`，并由其唯一保存Magica simulation frequency、max substep、global time scale和Manual update policy。Character Profile MUST不复制Manager级设置。缺失设置、同一运行产品存在两份冲突设置或Magica Manager仍处于Before/AfterLateUpdate MUST使preparation失败，不得读取插件默认值或场景残留配置。

#### Scenario: 两个角色Profile使用不同group参数

- **WHEN** Corin与另一角色各自使用不同Group约束但属于同一Presentation batch
- **THEN** 两者 MAY保留各自Group参数
- **AND** 两者 MUST共用同一正式Global Settings与一次manual simulation

### Requirement: Magica必须通过唯一显式backend实现Secondary Motion

Pose Graph、Profile、Projection与Runtime合同 MUST依赖项目的Secondary Motion抽象；当前唯一正式实现 MUST为`CharacterMagicaCloth2SecondaryMotionBackend`。Backend MUST把编译group、collider和参数映射到Magica Bone Cloth team，并继续使用Magica现有Transform read、Simulation、Constraint、Collision和Transform write数学。系统 MUST不提供运行时backend selector、无Magica passthrough、自研solver fallback、Spring Bone fallback或自动质量降级。

#### Scenario: 正式Runtime缺少Magica backend

- **WHEN** Projection包含SecondaryMotion stage但Magica backend未安装或版本不匹配
- **THEN** Actor preparation MUST失败
- **AND** MUST不把Base Pose当作SecondaryMotion成功输出

### Requirement: Magica角色team必须只由manual batch推进

Magica I/O seam MUST支持显式Manual update、presentation delta、RenderFrame、预期team集合和完成结果。Graph-owned team MUST不再由BeforeLateUpdate或AfterLateUpdate自动推进。同一RenderFrame的全部参与Actor MUST只触发一次global Magica simulation；每Actor调用、重复global call、未知team参与或completion集合不一致 MUST作为typed failure处理。

#### Scenario: 同帧存在两个Corin Actor

- **WHEN** 两个Actor都完成Base Physical Pose并登记各自裙摆team
- **THEN** Batch Coordinator MUST把全部team交给一次Magica manual call
- **AND** 任一team MUST不因Actor数量被重复推进

#### Scenario: 自动AfterLateUpdate仍开启

- **WHEN** graph-owned team同时注册了Magica AfterLateUpdate更新
- **THEN** preparation MUST失败并报告重复更新所有权
- **AND** MUST不依赖脚本执行顺序压住其中一次更新

### Requirement: Secondary Motion必须位于统一Physical Publication Barrier内

每个Presentation batch MUST按`Prepare all actors -> Animancer Evaluate and Base Physical Pose Apply -> one Global Secondary Motion Batch -> Post-secondary Full Rig Capture -> FinalPublication and Seal`执行。Base Pose MUST包含全部source、transition、AnimationSlot、Local/Component控制和FullBodyIK结果。Secondary Motion完成前 MUST不发布`FinalAnimationPoseFrame`、Committed Final Pose、Diagnostics或Camera。每个Actor的Final Pose MUST来自post-secondary完整PhysicalBoneCount capture，并携带同一RenderFrame、Rig、Profile、Projection和completion lineage。

#### Scenario: Magica完成后发布最终Pose

- **WHEN** 参与team全部返回匹配completion且完整Rig capture合法
- **THEN** SecondaryMotion节点 MUST发布post-secondary Local Pose
- **AND** OutputPose、Committed Final Pose与可见Rig MUST表达同一结果

#### Scenario: 基础Pose已写但Magica失败

- **WHEN** Actor已经跨过Animancer Evaluate并应用Base Physical Pose后Secondary Motion失败
- **THEN** 对应Actor MUST进入Faulted并阻止FinalPublication
- **AND** MUST不恢复Physical Bone快照、沿用上一帧或关闭节点继续

### Requirement: 多Actor失败必须按可证明的副作用范围处理

Actor在不可逆barrier前失败 MUST只Discard该Actor Pending事务。Actor team在global call前验证失败 MUST使该ActorFaulted并从参与集合移除，其他合法Actor MAY继续。Magica global manual call抛异常、完成集合不确定或产生无法归属的Physical写入时，全部参与Actor MUST进入Faulted。Final capture或publication失败 MUST使对应ActorFaulted。Faulted Actor MUST拒绝后续Presentation，不得自动重建team或切换更新路径。

#### Scenario: Global simulation发生异常

- **WHEN** Magica在包含多个Actor team的manual call中抛出异常
- **THEN** Batch Coordinator MUST把全部参与Actor标记Faulted
- **AND** MUST不假定某个team尚未写入并继续其下一帧

### Requirement: Secondary Motion状态必须按表现连续性显式Reset

Backend MUST在Body stream reset、committed branch replacement、teleport、visual root discontinuity、Rig/Profile/Projection revision变化、Preview scrub、target切换、session restart和visibility resume时，将对应Group reset到下一次Base Physical Pose。普通连续帧 MUST保留模拟history。项目角色 MUST不使用Magica camera或distance culling静默跳过team；visibility suspend/resume MUST由Presentation lifecycle显式拥有。

#### Scenario: Actor传送后恢复裙摆

- **WHEN** Body frame报告teleport或reset sequence变化
- **THEN** Backend MUST在本帧Base Pose应用后、Magica求解前reset裙摆team
- **AND** MUST不把传送位移作为一帧巨大惯性保留

### Requirement: 烘焙动画必须作为模拟基线保留

SecondaryMotion输入 MUST是本帧完整Base Local Pose，不得删除或忽略Clip中裙摆、头发和挂件的既有曲线。Profile的Animation Follow MUST映射Magica `animationPoseRatio`，Simulation Weight MUST映射Magica `blendWeight`。节点 MUST以Base Pose和模拟结果形成唯一输出，不得创建第二AnimationClip、Animator Layer或独立动画时钟。

#### Scenario: Simulation Weight为零

- **WHEN** Group的Simulation Weight为0且其它binding合法
- **THEN** 该Group输出 MUST等于本帧Base Pose
- **AND** 节点仍 MUST完成正式completion而不是被Runtime移除

### Requirement: Runtime setup必须由Projection和Rig Binding确定

Character Build MUST把Profile与Rig降低为dense physical bone、group、collider、team、setup artifact和固定workspace容量，并把identity、revision与hash编入Presentation Projection。Runtime preparation MUST只从Projection和现有`CharacterAnimationRigBinding`解析Transform，一次性创建并预热backend资源。正常PresentationFrame MUST不读取authoring资产、扫描Transform层级、按名称查找、创建组件、扩容或创建托管集合。Stale artifact、Rig mismatch、缺失binding或容量不一致 MUST使preparation失败。

#### Scenario: Rig revision变化后使用旧setup

- **WHEN** Corin Rig revision与Projection中的Secondary Motion setup lineage不一致
- **THEN** Runtime preparation MUST失败并要求显式Character Build
- **AND** MUST不按旧BoneId路径重新猜测binding

### Requirement: Secondary Motion必须只属于Presentation

Secondary Motion Profile、team state、collision result和post-secondary Pose MUST不进入Gameplay Semantic IR、Float32/Fixed Numeric Program、CharacterSimulationState、Rollback snapshot、World hash、网络协议、KCC、Foot Placement目标或FullBodyIK输入。Float32与Fixed角色 MUST复用同一Presentation Projection和同一视觉Secondary Motion实现。

#### Scenario: Rollback重演Gameplay Tick

- **WHEN** Fixed Session执行rollback replay
- **THEN** replay MUST不保存、恢复或哈希Magica team state
- **AND** 下一PresentationFrame MUST按committed Body continuity决定保留或reset视觉状态

### Requirement: Preview与Diagnostics必须观察post-secondary结果

Pose Graph Preview MUST使用同一Projection、Rig Binding、manual batch和Physical Publication Barrier。缺少完整world/rig/backend setup时，SecondaryMotion MUST返回typed Unavailable并阻止FinalPublication。Pose Watch与Live Debug MAY发布Base Pose、post-secondary Pose、Group、Collider、Team、reset generation、completion、碰撞统计和每骨修正量，但 MUST只从成功Seal的Committed固定页复制，不得从Transform反推、第二次执行Magica或影响正式结果。

#### Scenario: Preview缺少Magica setup

- **WHEN** Preview target没有匹配Projection的Secondary Motion setup
- **THEN** Preview MUST显示节点Unavailable和精确缺失项
- **AND** MUST不显示跳过Secondary Motion的伪最终Pose

### Requirement: Corin必须按骨链而不是网格拆分装配

Corin Secondary Motion Profile MUST把裙摆24根Physical Bone按`Skirt_01`至`Skirt_08`八条root chain和腰围有序`SequentialLoopMesh`装配；头发 MUST按Side、Front、Left Back与Right Back root chain分组；`Spring_L/R`与`S_ChainF/B` MUST作为独立挂件Group。裙摆Collider MUST显式绑定Pelvis与腿部Physical Bone，头发Collider MUST显式绑定Head、Neck、Shoulder与Upper Back。`Weapon_Lever_*`、`Weapon_saw*`和主要`Weapon_Etc_*` MUST继续由烘焙/业务动画拥有。系统 MUST不要求拆分Corin SkinnedMesh或增加Animator Layer。

#### Scenario: 作者把整把武器加入cloth group

- **WHEN** Corin Profile把武器机械chain声明为Secondary Motion controlled bone
- **THEN** Corin内容Validator MUST拒绝该业务装配
- **AND** MUST要求武器机械Pose继续来自Action/Clip链
