# Change: 将Motion Matching重构为UE式Pose节点

## Why

当前Motion Matching已经完成数据库、特征Schema、离线构建、查询计划、候选过滤、成本分解、连续性决策、Pose History和固定容量工作区等大部分底层能力，但正式接入仍按`MotionMatchingPoseSourceSlot -> SelectedPosePlayer或显式BlendStack -> Pose Plan`拆成三段。这个结构能表达“先选片段、再交给播放器”，却不能表达UE Motion Matching节点最关键的作者与运行时语义：同一个节点既决定当前匹配姿势，也拥有该选择对应的播放时间、跳转和内部Blend Stack。

这不是UI命名差异。当前链路把一次MM跳转拆给多个owner：深Module决定Continue或Jump，Player持有采样时间，外接BlendStack持有新旧Pose与淡入。任何一段的generation、source usage、reset或Rig lineage不一致，都会出现“查询选中了A，播放器仍在B”或“历史记录的不是实际MM输出”这类难以解释的问题。继续在`SelectedPosePlayer`上增加MM字段，只会让MM节点和播放器之间形成一份长期存在的私有协议。

UE 5.7本地源码中的`FAnimNode_MotionMatching`直接继承`FAnimNode_BlendStack_Standalone`；发生跳转时节点内部调用`BlendTo`。GASP则把Trajectory/Pose History和数据库Chooser放在Motion Matching节点外部，把每个Blend Stack entry的Orientation/Steering处理放在节点内部Blend Stack Graph，动作Slot、Root Offset和IK继续位于节点下游。本change参照的是这组职责，不照搬UE反射、Blueprint、Pose Search资产格式或PBIK实现。

仓库已经导入1271个GASP Humanoid FBX，覆盖Idle、Walk、Run、Sprint、Crouch、Jump、Slide与Traversal等类别，因此旧change中“没有合适动画、只做独立fixture”的判断已经失效。首个正式内容载体不修改Corin，而是新增`MotionMatchingDemoCharacter`角色Prefab，使用其中Grounded Idle/Walk/Run/Sprint建立完整数据库和Pose Graph链。Crouch、Airborne、Slide和Traversal只有在该角色存在对应Gameplay事实和动作所有权后才能加入，不能让MM从动画名字反推Gameplay状态。

## What Changes

- 新增真正的`MotionMatchingPose`节点。它输入上一帧Pose History、Trajectory、typed表现事实和本节点配置，输出本帧Local Pose；节点内部唯一拥有查询状态、选择generation、选中source的采样时间、source usage和MM跳转Blend Stack。
- 新增显式`PoseHistoryCollector`节点。它是Local Pose passthrough节点，搜索阶段只暴露上一帧已完成历史，MM输出完成后再记录本帧MM基础Pose；动作Slot、Root Offset、Foot Placement和FullBodyIK不得污染MM查询历史。
- 新增编译期`MotionMatchingDatabaseChooser`资产。Chooser只读取`CharacterPresentationFactFrame`中的typed事实，返回当前MM Profile内数据库的有序子集、搜索开关和中断模式；不使用字符串反射、Blueprint式任意逻辑、目录扫描或默认数据库fallback。
- 每个`MotionMatchingPose`节点拥有一个root-owned flat内部Blend Stack Graph。作者双击节点进入该图；每个live entry先经过同一处理图再混合。新建节点时由正式Mutation显式创建`EntryPoseInput -> GraphOutput`身份图，缺失或非法图直接阻止Build。
- 内部Blend Stack只负责同一MM节点内的Continue/Jump连续性：Continue推进当前entry，Jump压入新entry，容量压力通过Stored Pose压缩，release由实际权重和source usage闭环决定。Pose State之间的转场仍由PoseStateMachine拥有，有限Action仍由AnimationSlot拥有。
- 把当前深`CharacterMotionMatchingPresentationModule`拆除为两个清晰部分：actor级`CharacterMotionMatchingFrameContext`只解析同帧Trajectory、typed事实和时间；无状态`CharacterMotionMatchingSearchKernel`只执行数据库查询。选择、历史、Player与Blend状态不得进入共享服务。
- 从作者和运行时合同删除MM专用`CharacterMotionMatchingPoseSourceSlot -> SelectedPosePlayer`路径，删除`SelectedPosePlayer`节点，并删除显式`BlendStack`作者节点对MM Slot的消费。现有Blend Stack数值、Stored Pose、per-bone权重和固定Animation Job能力收敛为可复用kernel，由MM内部owner使用，不保留第二套MM接法。
- 收紧Profile、Chooser、Database、SourceSet和Artifact的Rig闭包：Presentation Profile Rig、MM FeatureSchema Rig、Database TargetRig、SourceSet TargetRig及生成物binding必须具有完全相同的RigId和Revision；Chooser引用的每个数据库必须属于当前MM Profile。
- 新增`MotionMatchingDemoCharacter`角色Prefab及其专属Character Pipeline Definition、Animation Presentation Profile、Pose Graph、唯一Presentation Rig、MM Profile、SourceSets、数据库分区、Chooser和生成物。该Prefab使用GASP Idle/Walk/Run/Sprint作为Grounded基础内容，并复用项目唯一CharacterPipeline、Pose Graph Compiler和MM Runtime。
- Corin继续保持现有非MM表现配置，作为已有角色内容而不是首个MM载体。新Prefab与Corin分离的是角色资产、Rig identity和内容配置，不得复制CharacterPipeline、Session、Pose Graph Runtime、Search Kernel或Blend Stack Kernel实现。
- 保持数据库、Foot Analysis、Presentation Projection和Native Pose Program仅通过明确Build发布。Inspector、打开资产、切换选择、Domain Reload或进入Play Mode不得自动生成、修补或替换产物。
- 迁移一次完成：实现进入正式链时必须同时删除旧payload、旧IR opcode、旧compiler分支、旧runtime state、旧workspace页、旧Mutation入口和旧Validator规则；新Prefab只保存新节点合同，不提供兼容读取、双写或runtime fallback。

## Impact

- 新增capability：`character-motion-matching-pose-node`、`character-motion-matching-runtime-kernel`。
- 修改`character-motion-matching-presentation`、`character-motion-matching-presentation-module`、`character-presentation-pose-graph`、`character-animation-selection-runtime`、`character-animation-blend-stack`、`character-animation-layer-runtime`和`character-animation-presentation-authoring`。
- 影响Pose Graph Document/Mutation/Capability Catalog/Validator/Compiler/Projection/Native Pose Program/Workspace、MM Profile与数据库绑定、Preview/Pose Watch/Live Debug，以及新增`MotionMatchingDemoCharacter`角色资产；不迁移Corin表现资产。
- 保留现有MM数据库格式、搜索算法、feature channel、admission gate、cost profile、continuity plan和显式离线构建能力，但把它们的运行时owner改为`MotionMatchingPose`节点。
- 不修改Gameplay KCC、网络同步、有限Action Timeline、身体Root Motion权威、Camera、Motion Warping或IK求解算法。

## 与UE和GASP的对应关系

- 对齐UE：Motion Matching是Pose Graph节点；节点内部拥有搜索、播放和Blend Stack；跳转不会再交给第二个播放器。
- 对齐GASP：Trajectory和Pose History保持显式输入；数据库集合先由Chooser筛选；每个live entry可经过内部Blend Stack Graph；Action、Root Offset与IK位于MM之后。
- 不照搬UE：本项目继续使用自己的typed事实、AnimationClip/Animancer采样后端、显式Build产物、Pose Program和固定容量workspace；不引入Blueprint VM、反射Property Access或UE Pose Search二进制格式。
- 本change只建立entry处理图的正式扩展点，不宣称已经拥有UE Orientation Warping、Stride Warping或Steering算法。以后增加这些能力时必须作为独立Pose节点/算法change进入内部图，不能在MM runtime中隐藏实现。

## 与Current Spec及Active Change对比

- `openspec/project.md`和current `character-animation-selection-runtime`规定MM provider发布source，由显式Player和transition owner消费。本change改为节点直接发布Local Pose，旧provider ABI仅保留给Sequence/BlendSpace等非MM source。
- current `character-motion-matching-presentation-module`规定唯一深Module拥有Trajectory、查询、选择、Pose History和Reset。本change删除该聚合owner，只保留actor级帧输入解析和无状态Search Kernel；全部可变选择状态回到具体MM节点实例。
- current `character-presentation-pose-graph`允许`SelectedPosePlayer`、显式`BlendStack`，但没有`MotionMatchingPose`和`PoseHistoryCollector`。本change新增后二者并删除前二者的MM作者路径。
- current `character-animation-blend-stack`把显式图节点作为状态owner。本change保留其数学和固定Job合同，删除当前未被资产使用的MM专用显式节点入口，把owner收敛到`MotionMatchingPose`内部。
- active `add-character-motion-matching-pose-source`已经完成的数据库、Schema、构建、查询和诊断基础继续复用；其中尚未完成的`PoseSourceSlot -> SelectedPosePlayer/外接BlendStack`和孤立validation fixture任务被本change取代。原fixture不继续扩建，而是由完整`MotionMatchingDemoCharacter` Prefab、Definition、Profile和Pose Graph正式装配取代。
- active `replace-pose-ik-with-finalik-full-body-solver`会把Animation Rig schema升级到v4并修改Pose Graph末段拓扑。新增角色 MAY拥有与Corin不同的正式RigId，因为它是独立角色Prefab；但该Prefab内部不得再有第二份MM Rig，全部MM资产始终引用它自己的Presentation Profile唯一Rig。节点代码可以先实现，正式数据库、Foot Analysis和Projection必须在Rig v4 schema及该Prefab最终Rig revision稳定后构建，否则生成物会立即因Rig lineage变化失效。

## 前置与实施顺序

1. 先冻结旧MM change中剩余的外接Player/BlendStack内容任务，保留已完成的搜索与构建基础。
2. 建立`MotionMatchingPose`、`PoseHistoryCollector`、Chooser、内部entry graph和编译/runtime合同，并一次删除旧MM节点路径。
3. 与FinalIK change确认Rig v4 schema，再为`MotionMatchingDemoCharacter`建立自己的唯一Presentation RigId/Revision；这不是在同一角色内再配一套MM Rig。
4. 在该Prefab Rig identity稳定后，建立Grounded GASP SourceSet、数据库、Chooser和Profile binding，再显式构建数据库、Foot Analysis、Projection与Native Pose Program。
5. 创建并完整装配`MotionMatchingDemoCharacter` Prefab，只连接新节点链；Corin保持原有正式资产引用，新Prefab也不保留旧selection fixture或双运行开关。

## References

- Epic Game Animation Sample Project: https://dev.epicgames.com/documentation/en-us/unreal-engine/game-animation-sample-project-in-unreal-engine
- Epic Motion Matching: https://dev.epicgames.com/documentation/en-us/unreal-engine/motion-matching-in-unreal-engine
- 本地UE 5.7 `FAnimNode_MotionMatching`：`C:/Program Files/Epic Games/UE_5.7/Engine/Plugins/Animation/PoseSearch/Source/Runtime/Public/PoseSearch/AnimNode_MotionMatching.h`
- 本地GASP素材：`3cDemo/Client/3C_Client/Assets/AssetArt/Animation/gasp`
