# character-motion-matching-runtime-kernel Specification

## ADDED Requirements

### Requirement: MM共享Runtime必须分为Frame Context与无状态Search Kernel

系统 MUST使用`CharacterMotionMatchingFrameContext`表达actor当前表现帧的Trajectory、typed facts、delta time、frame identity和Rig lineage，并使用无状态`CharacterMotionMatchingSearchKernel`执行数据库查询。两者 MUST不保存当前selection、source time、active entries、Pose History、Blend state或节点reset状态；这些状态 MUST只属于编译后的`MotionMatchingPose`节点实例。

#### Scenario: 同一Actor两个节点查询

- **WHEN** 同一表现帧内两个MM节点使用同一个Frame Context查询不同Profile或Chooser结果
- **THEN** 两次查询 MUST只共享不可变帧输入和数据库只读页
- **AND** 两个节点的selection state MUST保持隔离

### Requirement: Frame Context必须每帧解析一次并保持不可变

Presentation Stage MUST在任一MM节点求值前完成一次Frame Context resolve。完成后的context MUST带有明确completion与frame identity，并在该表现帧保持不可变。节点、Chooser和Search Kernel MUST不直接读取Input组件、KCC、Transform、Animator或Unity Time补充缺失数据。

#### Scenario: 节点收到旧Frame Context

- **WHEN** context frame identity与当前Pose transaction不一致
- **THEN** 节点 MUST报告stale frame input
- **AND** MUST不重新从actor组件解析一份私有context

### Requirement: Search Kernel必须完整消费显式Query并返回typed Plan

Search Kernel MUST只消费调用方传入的Feature Query、Pose History view、Chooser database set、search policy、continuity state和数据库artifact只读页，并返回`Continue | Jump | Invalid`计划及完整诊断。Kernel MUST不播放动画、不推进source time、不分配Blend entry、不提交history或修改source usage。

#### Scenario: Kernel返回Jump

- **WHEN**候选搜索通过admission并选中不同source sample
- **THEN** Kernel MUST返回包含database、segment、sample、time、cost与generation proposal的Jump计划
- **AND** 只有调用它的MM节点 MAY把计划应用到Blend Stack

### Requirement: Search Kernel必须保持Float32与Fixed算法语义一致

Float32与Fixed Presentation Program MUST从同一Profile、FeatureSchema、Database Artifact和Search Plan合同编译。量化格式 MAY不同，但candidate admission、channel顺序、cost term、Continue/Jump条件和diagnostic identity MUST保持同一语义；任一模式 MUST不拥有私有数据库筛选或fallback策略。

#### Scenario: 两种模式构建同一Profile

- **WHEN** 作者为同一MM Profile构建Float32与Fixed生成物
- **THEN** 两者 MUST保存相同的database/source/feature identity和查询阶段
- **AND** mode-specific数值参数 MUST由正式artifact明确表达

### Requirement: Search与Blend Kernel必须使用Build生成的固定工作区

Projection Build MUST计算每个MM节点所需candidate pages、feature pages、history pages、entry poses、Stored Pose和diagnostic pages的固定容量。Runtime MUST只租用这些页，不得按候选数、数据库数、entry数或骨骼数扩容托管集合。

#### Scenario: 查询超过构建容量

- **WHEN** Chooser结果或候选展开超过Projection中记录的容量
- **THEN** Runtime MUST报告capacity contract violation并阻止publication
- **AND** MUST不扩容、截断候选或退化为较小数据库集合

### Requirement: Search Kernel不得形成第二动画求值路径

Search Kernel和Frame Context MUST不创建PlayableGraph、Animancer State、Animator Controller、shadow skeleton或GameObject组件。AnimationClip采样 MUST只由编译后MM node entry player通过正式source backend完成，最终Pose MUST继续进入唯一Pose Plan和FinalPublication。

#### Scenario: 第三方查询库返回动画对象

- **WHEN** 底层搜索实现提供自带player或component更新入口
- **THEN** adapter MUST只读取其纯查询结果
- **AND** MUST不启用其动画求值、Transform写入或自主tick路径

