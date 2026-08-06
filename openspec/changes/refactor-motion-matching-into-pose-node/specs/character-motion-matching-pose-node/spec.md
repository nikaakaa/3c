# character-motion-matching-pose-node Specification

## ADDED Requirements

### Requirement: Motion Matching必须是直接输出Local Pose的正式节点

Pose Graph MUST提供`MotionMatchingPose`节点。该节点 MUST接收显式History、Trajectory、Presentation Facts和Motion Matching Binding输入，并直接输出`pose.local`。节点 MUST在同一节点实例内拥有数据库搜索、选中source播放时间、Continue/Jump generation、internal Blend Stack和source usage；MUST不把选择结果发布给第二个`SelectedPosePlayer`、显式MM `BlendStack`或图外播放器。

#### Scenario: Motion Matching节点正常求值

- **WHEN** 节点输入、Rig闭包、Chooser结果和生成物均有效
- **THEN** 节点 MUST在自己的状态中完成搜索、采样和混合
- **AND** 下游 MUST直接收到一个完成的Local Pose

#### Scenario: 作者尝试连接旧MM播放器

- **WHEN** Pose Graph保存`MotionMatchingPoseSourceSlot -> SelectedPosePlayer`或显式MM BlendStack路径
- **THEN** Document validation和Projection Build MUST拒绝该图
- **AND** 系统 MUST不生成兼容播放器或隐式转换

### Requirement: Motion Matching节点必须唯一拥有选择与播放生命周期

每个编译后的`MotionMatchingPose`节点实例 MUST唯一拥有当前selection identity、selection generation、database/segment/sample identity、source time、query cadence、active entries、Stored Pose、retention和release token。actor级共享服务、Profile、Chooser、Database和其它节点 MUST不保存或修改这些可变状态。

#### Scenario: 同一Actor存在两个MM节点

- **WHEN** 两个Pose State各自包含一个`MotionMatchingPose`节点
- **THEN** Runtime MUST为两个节点分配不同的state identity和entry workspace
- **AND** 一个节点的Jump、reset或release MUST不修改另一个节点

#### Scenario: selection generation不连续

- **WHEN** 节点收到重复、倒退或跨node identity的selection generation
- **THEN** 当前表现帧 MUST进入Invalid
- **AND** 节点 MUST不继续播放旧entry或创建替代generation

### Requirement: Motion Matching节点端口必须使用typed合同

`MotionMatchingPose` MUST只接受`history.pose`、`trajectory.query`、`presentation.facts`和`motion-matching.binding`四类typed输入，并只输出`pose.local`。节点payload MUST显式保存binding identity、Blend Policy、entry processing graph identity、relevance reset policy和search cadence policy；MUST不保存运行时选择、数据库缓存、字符串参数名或Animator引用。

#### Scenario: 端口类型不匹配

- **WHEN** 作者把普通Local Pose、字符串Blackboard值或Action playback连接到MM typed输入
- **THEN** Canvas与Validator MUST拒绝该edge
- **AND** Compiler MUST不插入隐式cast

### Requirement: Pose History必须由显式Collector提供

Pose Graph MUST提供`PoseHistoryCollector` Local Pose passthrough节点。Collector MUST在本帧搜索前只读暴露上一帧已完成history page，并在MM基础Pose完成后提交本帧Pose、root kinematics、source lineage、frame identity和Rig lineage。Collector记录点 MUST位于MM输出之后、AnimationSlot和全部world-aware Pose修正之前。

#### Scenario: 记录基础MM Pose

- **WHEN** MM节点完成本帧Local Pose且下游Action与IK尚未执行
- **THEN** Collector MUST把该基础Pose提交到history ring
- **AND** Action Slot、Foot Placement或IK结果 MUST不进入该history sample

#### Scenario: 首帧没有历史

- **WHEN** Collector处于Unseeded、重相关、Rig revision变化或明确reset状态
- **THEN** MM节点 MUST使用Profile定义的initial selection规则
- **AND** Runtime MUST不复制Animator当前Pose或上一帧final Pose作为隐藏seed

### Requirement: History绑定必须在编译期唯一且无环

每个可达`MotionMatchingPose`节点 MUST绑定且只绑定一个同Rig `PoseHistoryCollector`。Compiler MUST证明Collector的read发生在搜索前、commit发生在目标MM基础Pose完成后，并证明同一帧不存在两个互相覆盖的history writer。缺失、多重、跨Rig、循环或顺序不确定的绑定 MUST使Build失败。

#### Scenario: MM节点缺少Collector

- **WHEN** 可达MM节点没有兼容History绑定
- **THEN** Validator MUST报告`MissingHistoryCollector`
- **AND** Projection Build MUST不生成该Pose Plan

#### Scenario: 两个writer竞争同一Collector

- **WHEN** 两个同帧可达分支都可能向同一Collector提交Pose
- **THEN** Compiler MUST报告history write conflict
- **AND** MUST不按节点顺序选择一个writer

### Requirement: 数据库Chooser必须只返回Profile内的正式集合

`CharacterMotionMatchingDatabaseChooser` MUST读取typed`CharacterPresentationFactFrame`并输出数据库有序集合、`ShouldSearch`、`InterruptMode`及可选的正式policy identity。每个输出数据库和policy MUST属于当前Motion Matching Profile，且其RigId与Revision MUST通过完整闭包。Chooser MUST不读取动画文件名、GameObject、Transform、任意脚本返回值或未声明Blackboard字符串。

#### Scenario: Chooser选择Grounded数据库

- **WHEN** typed事实表达Grounded且Gait为Walk或Run
- **THEN** Chooser MUST返回Profile内明确配置的Grounded数据库集合
- **AND** MM搜索 MUST只遍历该集合

#### Scenario: Chooser返回Profile外数据库

- **WHEN** 规则引用的数据库不属于当前Profile
- **THEN** Profile validation和Projection Build MUST失败
- **AND** Runtime MUST不接受该数据库或替换为Profile第一项

### Requirement: Chooser规则冲突与空结果必须显式失败

Chooser规则 MUST具有明确priority和exclusive policy。多个同优先级互斥规则同时命中、结果为空、数据库重复、policy不属于Profile或事实页completion不一致时，结果 MUST为Invalid。系统 MUST不使用默认Idle、全库搜索、上一帧数据库集合或Inspector顺序作为fallback。

#### Scenario: 两条互斥规则同优先级命中

- **WHEN** 当前事实同时满足两条相同priority的exclusive规则
- **THEN** Chooser MUST返回明确冲突诊断
- **AND** MM节点 MUST阻止本帧Pose publication

### Requirement: Continue与Jump必须直接驱动节点内部Blend Stack

Search Kernel返回Continue时，节点 MUST只推进当前entry的source time和lineage；返回Jump时，节点 MUST以新generation向自己的internal Blend Stack压入新entry。Jump Blend Policy MUST属于该MM节点，外部Pose State transition、AnimationSlot或Inertialization MUST不接管同一Jump。

#### Scenario: 查询继续当前片段

- **WHEN** Search Plan结果为Continue
- **THEN** 节点 MUST保持当前entry identity并推进其采样时间
- **AND** MUST不创建零时长新entry或触发外部transition

#### Scenario: 查询跳到新片段

- **WHEN** Search Plan结果为Jump且generation有效
- **THEN** 节点 MUST把新source压入internal Blend Stack
- **AND** 新旧Pose混合 MUST使用该节点唯一Blend Policy

### Requirement: Internal Blend Stack必须复用统一Kernel并保持固定容量

MM节点 MUST通过统一`CharacterAnimationBlendStackKernel`计算独立clock、curve、per-bone规范化权重、Stored Pose压缩和source release。每个节点 MUST使用Build生成的固定entry容量和Pose pages；表现帧 MUST不新增托管分配、扩容集合、Playable层或临时AnimationClip缓存。

#### Scenario: Jump超过live entry容量

- **WHEN** 新Jump到达且旧entries仍有非零贡献并达到固定容量
- **THEN** Kernel MUST把可压缩旧贡献合成为一个Stored Pose
- **AND** MUST在不丢失当前总Pose贡献的前提下为新entry腾出固定slot

#### Scenario: entry权重归零

- **WHEN** entry不再被live blend或Stored Pose引用
- **THEN** 节点 MUST发布精确source release并回收对应slot
- **AND** MUST不按clip名称或全局引用计数猜测释放

### Requirement: 每个MM节点必须拥有显式Entry Processing Graph

每个`MotionMatchingPose`节点 MUST引用一个root-owned flat entry processing graph。该图 MUST且只能包含一个`EntryPoseInput`和一个到达`GraphOutput`的Local Pose路径。正式Mutation创建MM节点时 MUST在同一事务创建`EntryPoseInput -> GraphOutput`身份图；缺失、孤立、共享写入或owner identity不一致 MUST使Document和Build失败。

#### Scenario: 新建MM节点

- **WHEN** 作者通过正式Capability创建`MotionMatchingPose`
- **THEN** Mutation MUST同时创建唯一entry graph identity和身份连接
- **AND** 双击节点 MUST打开该图

#### Scenario: entry graph被删除

- **WHEN** MM节点引用的entry graph不存在
- **THEN** Validator MUST报告broken reference
- **AND** Runtime MUST不以隐藏identity处理替代

### Requirement: Entry Processing Graph必须在每个live entry混合前独立执行

Compiler MUST把entry processing graph编译为每个live entry在Blend Stack混合前执行的Local Pose子程序。任何有状态inner node MUST按`MM Node Identity + Entry Generation + Inner Node Identity`分配状态。不同entry、不同MM节点或Stored Pose MUST不共享inner node状态。

#### Scenario: 两个live entry同时混合

- **WHEN** Jump后的旧entry和新entry都有非零权重
- **THEN** Runtime MUST分别采样并执行两次同一entry program
- **AND** Blend Stack MUST混合处理后的两个Pose而不是先混合再处理

### Requirement: Entry Processing Graph必须限制为局部Pose处理

Entry graph MUST不允许`StateMachine`、`MotionMatchingPose`、`PoseHistoryCollector`、`AnimationSlot`、`ActionPlaybackInput`、外部source Player、world-aware节点、Component Pose IK或最终Output节点。允许节点 MUST由Capability声明为entry-local且不得访问actor可变全局状态。没有正式Warping节点时，身份图 MUST保持显式，不得在MM Runtime隐藏Orientation、Stride或Steering数学。

#### Scenario: 作者在entry graph加入FullBodyIK

- **WHEN** entry graph包含world-aware或Component Pose IK节点
- **THEN** Canvas capability和Validator MUST拒绝该节点
- **AND** Compiler MUST不把它提升到MM节点外部

### Requirement: MM节点必须与Pose State、Action和IK保持单一所有权边界

MM internal Blend Stack MUST只处理同一节点内部的Pose选择跳转。Pose State之间的transition MUST继续由PoseStateMachine拥有；有限Action MUST继续由AnimationSlot/Timeline拥有；Root Motion、Root Offset、Foot Placement和FullBodyIK MUST继续位于MM基础Pose之后。MM MUST不根据动画候选修改Gameplay状态、动作所有权或Body movement。

#### Scenario: Attack覆盖Grounded MM

- **WHEN**有限Attack获得Action Slot所有权
- **THEN** Grounded MM节点 MAY继续按正式relevance policy维护或暂停自己的状态
- **AND** Attack进入、退出和Gameplay窗口 MUST不由MM候选决定

### Requirement: MM全链必须使用唯一Presentation Rig身份

Presentation Profile Rig、MM FeatureSchema Rig、Database TargetRig、SourceSet TargetRig、Database Artifact binding、Foot Analysis binding和Presentation Projection binding MUST具有完全相同的RigId与Revision。Humanoid Avatar、骨骼名称相似或旧revision MUST不构成兼容。任何闭包断裂 MUST阻止Build和Runtime publication。

#### Scenario: Profile使用Rig v4而数据库仍为Rig v3

- **WHEN** MM节点绑定的数据库artifact revision与Presentation Profile Rig不同
- **THEN** Projection Build MUST报告Rig lineage mismatch
- **AND** Runtime MUST不重定向、重采样或接受旧artifact

### Requirement: MM节点必须提供可追踪的正式诊断

Preview、Pose Watch、Live Debug和Trace MUST从MM node identity追踪Chooser规则、数据库集合、query cadence、admission结果、cost breakdown、Continue/Jump原因、generation、active entries、entry graph、最终权重、Stored Pose、source usage、history read/commit frame和Rig lineage。诊断 MUST只读取正式计划与已完成页，不得再次搜索、再次采样或维护shadow player。

#### Scenario: Preview查看一次Jump

- **WHEN** Preview中的MM节点从当前entry Jump到新entry
- **THEN** 诊断 MUST显示被选数据库、候选成本、Jump原因、新generation和混合权重
- **AND** Preview Pose MUST来自与Runtime相同的Projection和Pose Program

### Requirement: MM节点失败必须阻止Pose publication

Chooser、History、Rig、artifact、generation、source sampling、entry graph、Blend Stack或workspace任一合同失效时，MM节点 MUST返回typed Invalid并阻止后续Pose stage和FinalPublication。系统 MUST不输出上一帧Pose、默认clip、bind pose、旧MM provider结果或部分混合Pose。

#### Scenario: entry source采样失败

- **WHEN** 任一非零权重entry无法取得与计划一致的source pose
- **THEN** 节点 MUST使本帧Pose transaction失败
- **AND** MUST不丢弃该entry后重新规范化剩余权重

