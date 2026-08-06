# Change: 将Magica次级动画接入Pose Graph正式节点

## Why

Corin的裙摆、头发、腰部挂件和武器机构都在同一套骨架里，`Corin_body`虽然是一整块SkinnedMesh，但裙摆本身已经由8条、每条3根的独立`Skirt_*`骨链驱动；头发、`Spring_*`、`S_Chain*`和武器机构也各有独立Physical Bone。网格是否拆成多个Renderer不决定能否做次级动画，真正的边界是“哪些骨骼由谁在什么时点写”。

当前动画Clip已经烘焙这些骨骼的运动。FullBodyIK在Component Pose阶段改变骨盆和腿后，烘焙裙摆仍然按原Clip轨迹运动，因此容易与新的腿部Pose穿插。Magica Cloth 2可以把当前动画Pose作为基线，再通过距离、角度、弯曲、碰撞和惯性约束得到修正结果；`animationPoseRatio`决定模拟基线跟随当前动画的程度，`blendWeight`决定最终结果在原动画和模拟结果之间的权重。因此它适合做“保留烘焙动画，再对裙摆、头发做后处理”，而不是要求先把模型或Clip拆成多个Animator Layer。

直接在Corin Prefab挂`MagicaCloth`并使用默认`AfterLateUpdate`不能成为正式方案。当前Pose Graph在一次Animancer Evaluate后由`AnimationFinalPosePhysicalWriter`写完整Physical Rig并立即`Seal`；Magica默认随后再次写骨。这样可见骨架与`FinalAnimationPoseFrame`、Pose Watch和事务completion不一致，并形成图外第二写入。多角色时Magica Manager还是全局批处理，若每个Actor各自调用一次，会让同一帧的所有cloth team被重复推进。

本change把次级动画定义为一个有状态、需要Physical Rig与全局批处理屏障的正式Pose节点。它不伪装成普通Native纯Pose算子，也不保留组件自动更新旁路。

## What Changes

- 新增root-only `SecondaryMotion` Pose节点：
  - 接收`pose.local`并输出`pose.local`。
  - 只能位于最终`ComponentToLocalPose`之后、`OutputPose`之前。
  - 同一root Pose Graph最多一个节点；裙摆、头发和挂件作为该节点引用Profile中的多个互不重叠group，不串接多个全局Magica求解器。
  - Linked Pose Entry、State Pose Graph、MM Entry Graph和普通Subgraph不得拥有该节点。
- 新增`CharacterSecondaryMotionProfile`作为次级动画语义真相：
  - 保存稳定Profile/Group/Collider identity、精确Rig BoneId、root chain顺序、连接拓扑、固定/可动范围、动画跟随、模拟权重、约束、碰撞、惯性和reset策略。
  - controlled bone、collider bone必须来自同一Rig的Physical Bone；Virtual Bone、名称搜索、Transform路径猜测和group骨骼重叠全部拒绝。
  - Profile不保存backend选择。当前唯一正式实现是Magica Cloth 2；不存在运行时fallback、质量自动降级或第二solver。
- 分离语义与实现：
  - Pose Graph、Profile、Projection和Runtime只依赖项目的Secondary Motion合同。
  - `CharacterMagicaCloth2SecondaryMotionBackend`是唯一实现adapter，负责把编译group、collider和参数映射到Magica team。
  - Magica的solver方程、约束顺序和碰撞数学保持vendor实现；项目只扩展显式manual update、显式presentation delta、team/completion身份和生命周期I/O seam。
- 将动画表现帧改为唯一三段批处理：
  1. 所有Presentation target按稳定顺序完成Fact、source、Slot、Pose Graph、FullBodyIK、`ComponentToLocalPose`和基础Physical Pose应用，但保持Pending事务未`Seal`。
  2. `CharacterSecondaryMotionBatchCoordinator`汇总全部已验证team，以同一RenderFrame和presentation delta调用Magica manual simulation恰好一次。
  3. 所有target按原稳定顺序捕获post-secondary完整Physical Local Pose，完成`FinalPublication`、`Seal`、Diagnostics和Camera。
- 删除/禁用项目角色的Magica `BeforeLateUpdate`、`AfterLateUpdate`自动更新路径。正式角色cloth只能由编译节点登记到manual batch；Prefab上独立启用的Magica角色组件、第二PlayerLoop回调或每Actor私有求解均视为配置错误。
- 把现有不可逆边界扩展为一个`Physical Publication Barrier`：
  - 进入前完成全部Actor、Rig、Profile、team、collider、workspace、base writer、final capture和completion验证。
  - 内部依次执行Animancer Evaluate、pre-secondary Pose stage、基础Physical Pose应用、全局Secondary Motion batch、post-secondary完整Pose capture和FinalPublication。
  - Barrier内任一Actor局部失败只使该ActorFaulted并从可参与team集合移除；Magica全局调用本身发生异常时，全部参与Actor均Faulted，因为无法证明全局Physical副作用可回滚。
  - 不捕获before-image、不恢复Physical Bone、不沿用上一帧、不跳过节点。
- `FinalAnimationPoseFrame`、Committed Final Pose页、Pose Watch和Camera只消费post-secondary capture；基础Pose不得提前作为最终帧发布。
- Runtime preparation从Projection和现有`CharacterAnimationRigBinding`解析精确Transform，创建并预热Magica team、collider和固定workspace。正常PresentationFrame不扫描层级、不创建组件、不扩容、不生成托管集合。
- Secondary Motion只属于Unity Presentation：Float32与Fixed复用同一Presentation Projection和同一视觉节点；它不进入Gameplay Semantic IR、Numeric Program、Rollback snapshot、World hash、网络包或KCC/IK决策。
- 同步人工作者表面与Agent Document v3：
  - 唯一Capability Catalog登记节点kind、profile字段、root-only上下文、Local Pose端口和execution domain。
  - Profile资源作为只读Asset Catalog context；Document editable只保存节点对现有Profile的结构化对象引用。
  - Exporter、strict codec、Reconciler、typed Presentation Mutation、Validator、Preview、Live Debug、Pose Watch和五个既有MCP生命周期工具使用同一Capability，不新增Pose专用MCP或第二Mutation入口。
- 迁移Corin内容：
  - 裙摆group使用`Skirt_01`至`Skirt_08`八条root chain及其24根Physical Bone，按腰围作者顺序使用`SequentialLoopMesh`，碰撞体绑定Pelvis、左右大腿并按几何需要纳入小腿。
  - 头发按Head下的侧发、前发、左右后发根链拆成互不重叠group，碰撞体绑定Head、Neck、Shoulder与Upper Back。
  - `Spring_L/R`与`S_ChainF/B`作为独立挂件group，使用各自root chain和较小模拟权重。
  - `Weapon_Lever_*`、`Weapon_saw*`和主要`Weapon_Etc_*`继续由烘焙/业务动画拥有；机械机构不是布料。只有以后明确识别为松散装饰的武器骨链才能加入新group，不能把整把武器交给Secondary Motion。
  - Corin root Pose Graph形成`... -> FullBodyIK -> ComponentToLocalPose -> SecondaryMotion -> OutputPose`唯一末段，不拆网格、不增加Animator Layer、不改Clip所有权。
- 迁移完成后删除旧单调用Presentation target接口、旧`AnimationFinalPosePhysicalWriter`终态语义、项目角色Magica自动更新配置和任何独立secondary component wiring，不保留兼容reader或双路径。

## Impact

- 新增capability：`character-secondary-motion-presentation`。
- 修改`character-presentation-pose-graph`、`character-animation-pipeline`、`character-animation-layer-runtime`、`character-animation-presentation-authoring`、`gameplay-tick-system`和`btsmtl-agent-authoring-document-sync`。
- 影响Pose Graph Capability、typed payload、Mutation、Validator、Projection Compiler、Native stage table、Animation Presentation事务、GameplayTickSystem presentation target调度、Magica Cloth 2 I/O seam、Preview/Diagnostics和Corin Presentation内容资产。
- 不修改Corin模型网格、SkinnedMesh Renderer拆分、Gameplay状态、KCC、FullBodyIK数学、Timeline事件、网络模型或Rollback状态。
- Presentation Projection、Native Pose Program和Corin Float32/Fixed generated产品仍只由显式Character Build发布，不自动构建。

## 与Current Spec及Active Change对比

- current `character-presentation-pose-graph`只允许Pure/WorldAware Pose stage和一次final writer，节点清单没有`SecondaryMotion`。本change新增`ExternalPhysicalPose` execution domain，并把“只能有一个图外不可见writer”替换为“只有一个编译Physical Publication owner，可在其内部执行基础全Rig应用和声明骨集的Magica修正”。
- current `character-animation-pipeline`规定`Prepare -> Validate -> Animancer Evaluate Barrier -> Seal`在单Actor调用内同步完成，且`AnimationFinalPosePhysicalWriter`一次写完整Rig。本change保留单次Animancer Evaluate和Barrier后Fault语义，但在同一不可逆barrier中加入跨Actor的manual Magica batch与post-secondary capture；这是一项明确的事务合同修改，不是现有spec自然允许的普通节点。
- current `gameplay-tick-system`只逐个调用单方法`PresentationFrame`。本change把该接口原子迁移为`Prepare -> Global Physical Barrier -> Finalize`批次；不保留旧接口或只给Magica角色走的可选旁路。
- current `character-animation-presentation-authoring`没有Secondary Motion Profile或节点作者入口。本change把Profile编辑放进现有Presentation Profile/Pose Graph工作区，不创建独立Workbench。
- current Document v3已经规定新增Pose能力必须复用共享Capability、Exporter、Reconciler、Mutation和Validator。本change遵守该总合同，只扩展typed node payload和只读Profile catalog，不改变五工具生命周期或Document路径。
- active `replace-pose-ik-with-finalik-full-body-solver`明确要求“不需要Physical Transform中间写入”和“Physical Transform只由final writer写一次”，与Magica基于实际Transform读取/写回的正式接入直接冲突。本change实施必须排在该change完成并归档之后，并有意修改它安装后的writer合同；不能两份change并行修改同一末段事务。
- active `add-linked-pose-interface-runtime`要求Linked Entry不能拥有world-aware节点、FullBodyIK、OutputPose或第二final writer。本change的`SecondaryMotion`同样root-only，因此不改变Linked Entry局部执行模型。
- active `refactor-motion-matching-into-pose-node`要求MM History在Action、world修正和IK之前提交。Secondary Motion位于全部IK之后，不进入MM History，边界兼容。

## Hard Stop Gates

实施前必须按顺序证明：

1. Magica Cloth 2可以通过很小的vendor I/O seam接受显式manual batch与presentation delta，且不改写Simulation、Constraint、Collision核心方程。
2. 全部graph-owned team可以关闭默认Before/AfterLateUpdate，不会在manual batch之外再次恢复或写入Transform。
3. 单次global manual call可以覆盖同一RenderFrame全部Actor team，并返回可核对的team/completion结果；不能通过每Actor重复调用全局Manager实现。
4. 基础Physical Pose应用后，Magica能读取同帧FullBodyIK结果，并在返回前完成写回；不需要shadow skeleton、第二Animator、第二PlayableGraph或跨帧未闭合事务。
5. post-secondary capture可以形成完整PhysicalBoneCount Local Pose、同一Rig/frame/completion lineage，并作为唯一`FinalAnimationPoseFrame`发布。
6. Profile与Projection能够在preparation时完整确定group、bone、collider、team和workspace容量；正常帧不扫描Transform、不动态扩容、不创建组件或托管集合。
7. Corin八条裙摆root的腰围顺序和全部controlled bone能由Rig v4精确解析，且与头发、挂件和武器业务骨集无重叠。

任一门禁失败时必须停止实施并报告失败的Magica文件、API依赖和事务影响；不得退回默认AfterLateUpdate组件、每Actor重复求解、shadow skeleton、忽略Final Pose不一致或保留无碰撞的旧动画fallback。
