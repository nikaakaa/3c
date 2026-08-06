## Context

Corin现有Rig已经包含裙摆、头发、挂件和武器Physical Bone。模型网格合并只影响Renderer组织，不妨碍按骨链修改Pose。真正的问题是当前Clip保存了烘焙次级曲线，而FullBodyIK会在同帧改变骨盆、双腿和上身；裙摆仍按原曲线采样时不知道腿的新位置。

Magica Cloth 2的Bone Cloth会从当前Transform Pose建立proxy mesh，按动画Pose、惯性、距离、角度、弯曲和碰撞约束求解，再把结果写回注册骨骼。当前本地源码确认：

- `MagicaManager`默认Update Location是`AfterLateUpdate`。
- `ClothManager.ClothUpdate`先触发`OnPreSimulation`，读取Transform，调度Simulation，等待完成，再触发`OnPostSimulation`。
- Bone Cloth最终通过`WriteTransformSchedule`写回Transform。
- `animationPoseRatio`参与模拟基线，`blendWeight`在原动画结果和模拟结果之间插值。
- Bone root支持`Line`、`AutomaticMesh`、`SequentialLoopMesh`和`SequentialNonLoopMesh`。

当前角色动画事务在`CharacterAnimationPresentationRuntime.Present`中同步完成`Prepare -> Validate -> Animancer Evaluate -> Pose stage -> Final Writer -> Seal`。这与Magica默认时序不兼容：如果让插件自动更新，正式Final Pose已经发布；如果每个Actor自己手动更新全局Magica Manager，同一帧多个Actor会重复推进全部team。

## Goals

- 保留Clip中已经烘焙的裙摆、头发和挂件动画，把模拟作为IK后的修正。
- 让Secondary Motion成为Pose Graph可见、可编译、可验证、可Watch的正式节点。
- 让可见Physical Rig、Committed Final Pose页和`FinalAnimationPoseFrame`完全一致。
- 同一RenderFrame只推进一次Magica全局模拟。
- 让裙摆先看到FullBodyIK后的腿部结果，再执行碰撞修正。
- 复用Magica成熟约束和碰撞数学，不在项目里再写一套布料solver。
- 保持Secondary Motion为纯视觉状态，不污染Gameplay、Rollback与网络。
- 保持Authoring Capability、Document v3、人工UI、Compiler和Runtime单链。

## Non-Goals

- 不拆Corin SkinnedMesh、不复制Renderer、不把裙摆导成独立角色。
- 不把裙摆、头发或武器做成Animator Layer；Layer解决动画混合所有权，不解决IK后碰撞。
- 不把武器机械骨骼交给cloth solver。
- 不让Magica直接修改Gameplay Body、KCC、Foot Placement或FullBodyIK目标。
- 不追求视觉次级模拟的Rollback确定性。
- 不增加Spring Bone、Dynamic Bone、自研Verlet或无碰撞旧动画作为fallback。
- 不允许Prefab作者绕过Profile自行挂自动更新Magica角色组件。

## Selected Architecture

```text
全部Actor Prepare
  -> Fact / PoseState / Source / Slot
  -> LocalToComponent
  -> Goal Sources / FullBodyIK
  -> ComponentToLocal
  -> SecondaryMotion输入Base Local Pose
  -> Animancer Evaluate + Base Physical Pose Apply
       |
       +-- 全部Actor在同一RenderFrame登记validated team
             -> Magica Global Manual Batch（恰好一次）
                  -> Post-secondary完整Physical Pose Capture
                       -> SecondaryMotion Local Pose输出
                            -> OutputPose / FinalPublication
                                 -> Seal / Diagnostics / Camera
```

`SecondaryMotion`在作者图上是一个Local Pose passthrough节点，但不是PurePose数学节点。Compiler把它降低为`ExternalPhysicalPose` stage和对应的pre-secondary input、team plan、post-secondary output、completion slot。Native Pose executor只完成它之前的Pose；Physical Publication Coordinator负责基础Pose应用、全局Magica求解、完整Rig捕获和最终publication。

## Decision: 一个root节点拥有多个group

同一root Pose Graph最多一个`SecondaryMotion`节点。节点引用一个`CharacterSecondaryMotionProfile`，Profile内包含多个互不重叠group。

收益：

- Magica全局Manager每帧只需要一个batch，不会因为裙摆、头发分节点而重复推进。
- Graph末段只有一个状态边界，FinalPublication只等待一个completion。
- group仍能独立配置权重、碰撞、reset和Diagnostics，不丢失内容组织。

代价：

- 作者不能通过串接多个节点表达group先后顺序。
- group之间必须通过同一batch并行求解；若未来确实需要前一组结果驱动后一组，需要另立有证据的solver-order change，不能在本节点中增加隐藏多pass。

## Decision: 节点固定在IK后

节点只允许位于最终`ComponentToLocalPose`与`OutputPose`之间。它接收和输出`pose.local`，但solver内部根据Physical Transform在world/component空间执行碰撞。

选择该位置的业务原因是裙摆必须看到同帧FullBodyIK后的骨盆与腿。若放在IK前，IK仍可能把腿推入已经求解的裙摆；若放在Action Slot或PoseState分支内，不同动作会重复配置或丢失统一碰撞所有权。

Linked Pose Entry、State Pose Graph、MM Entry Graph和普通Subgraph不允许节点，因为这些图是局部Pose片段，不能拥有全局Physical Rig、PlayerLoop或final publication生命周期。

## Decision: Profile是语义真相，Magica是唯一实现

`CharacterSecondaryMotionProfile`保存：

- ProfileId、revision与content hash。
- 有序GroupId集合。
- 每组有序root BoneId、连接模式、精确controlled Physical Bone集合和固定点规则。
- Animation Follow、Simulation Weight、damping、gravity、inertia、distance、angle、bending、motion constraint与collision设置。
- 有序ColliderId、绑定Physical BoneId、Sphere/Capsule/Plane形状、local offset和尺寸。
- Reset policy、visibility suspend policy和diagnostic label。

Profile不保存Transform、GameObject、组件引用、backend enum或fallback。Projection Compiler把BoneId解析为同一Rig的dense physical index，生成固定group/collider/team descriptor和workspace容量。

实现层分成：

- `ICharacterSecondaryMotionSolverBackend`：项目侧batch、team、reset、completion和capture合同。
- `CharacterMagicaCloth2SecondaryMotionBackend`：唯一正式实现。
- Magica vendor I/O seam：manual update、显式delta、team identity与completion；不修改solver核心数学。

这不是运行时backend选择。Profile和Projection都要求当前唯一Magica实现；backend缺失直接preparation失败。接口只负责抽象与实现分离，使Pose Graph、Document和Compiler不直接依赖vendor组件类型。

## Decision: 全局参数只有一个正式owner

Magica simulation frequency、每帧最大substep、manual update location和全局time scale属于Manager级设置，不能由每个角色Profile各自声明。新增唯一`CharacterSecondaryMotionRuntimeSettings`，由Gameplay Presentation装配根显式引用，并由`GameplayTickSystem`的Physical Publication Coordinator消费。

角色Profile只保存group级物理参数。若运行时缺少Global Settings或出现第二份不同设置，启动失败；不采用第一个、场景默认值或Magica Inspector残留值。

## Decision: 显式Character Build生成运行时setup

Authoring Profile与Rig是唯一输入。Character Build负责：

1. 校验Profile、Rig、Bone/Collider identity和group互斥。
2. 将root、controlled bone与collider BoneId降低为dense physical index。
3. 生成Magica setup payload、固定容量和Profile/Rig lineage。
4. 把payload编入Presentation Projection及其依赖闭包。

Runtime preparation从Projection和现有`CharacterAnimationRigBinding`解析Transform，并一次性创建、初始化和预热Magica team与collider。若Magica需要PreBuild数据，Editor compiler从同一Profile/Rig生成并把artifact identity/hash编入Projection；Prefab组件不得成为第二作者真相。打开Inspector、进入Play Mode或运行时不得自动Build。

如果本地Magica API无法从编译payload构建team，且必须依赖可手工编辑的Prefab `MagicaCloth`组件正文，实施在Hard Stop Gate停止；不得把组件字段与Profile双写。

## Decision: 保留烘焙动画而不是先删曲线

Secondary Motion的输入Base Pose已经包含Sequence/BlendSpace/MM、State transition、有限Action Slot、全部Local/Component控制和FullBodyIK结果。裙摆、头发等原Clip曲线继续存在。

参数语义固定为：

- `Animation Follow`映射Magica `animationPoseRatio`：值越高，约束基线越接近本帧动画Pose。
- `Simulation Weight`映射Magica `blendWeight`：0为完全保留Base Pose，1为完全采用模拟结果。

首轮内容调参应让Animation Follow保持较高、Simulation Weight保持低到中等，使Magica主要纠正惯性和穿插而不是抹掉角色原有表演。具体数值属于Corin Profile资产，不写死在Runtime或spec中。

这只能在合理初始Pose和Collider几何下改善穿模。若Clip让裙摆深度穿入腿内、root排序错误或Collider体积不合适，solver不能保证自动恢复；Build Validator负责结构正确，内容参数由正式Profile调整。

## Decision: Physical Publication使用三段批事务

旧单Actor同步调用迁移为统一批次：

### PrepareActor

- 读取同一PresentationFrame context。
- 完成Body、Fact、Action、source、Pose Graph、FullBodyIK和pre-secondary Base Local Pose。
- 验证Rig binding、Profile lineage、team/collider、workspace、base apply和final capture binding。
- 进入不可逆barrier，执行一次Actor Animancer Evaluate并把Base Pose应用到完整Physical Rig。
- 登记`ActorId + RenderFrame + CompletionIdentity + TeamSet`，保持事务Pending。

### ExecuteGlobalSecondaryMotion

- 等待本帧全部Presentation target完成PrepareActor。
- 按稳定ActorId、GroupId顺序形成固定team batch。
- 使用同一presentation delta调用Magica manual simulation恰好一次。
- 核对所有预期team完成且没有额外自动team参与。

`GameplayTickSystem`只依赖通用`IGameplayPresentationBatchCoordinator`合同，并在构造时获得唯一正式实例；它不引用Character、Animation或Magica类型。产品装配提供`CharacterPhysicalPublicationBatchCoordinator`作为唯一实例，该实例即使本帧没有Secondary Motion team也完成同一批协议，不使用Null fallback或第二调度分支。

### FinalizeActor

- 从同一Rig捕获完整PhysicalBoneCount Local Pose到Pending Final Pose页。
- 校验Base completion、Secondary completion、Rig/Profile/Projection lineage和有限数值。
- 完成`SecondaryMotion`节点输出和`OutputPose/FinalPublication`。
- 交换Committed页、提交Action/source生命周期、发布Diagnostics，最后推进Camera。

所有Presentation target都使用该三段接口。没有Secondary Motion节点的Actor仍走相同Prepare/Finalize协议，只是没有team登记；不保留旧单调用接口作为旁路。

## Decision: 失败以Actor或全局副作用范围为边界

- PrepareActor在Animancer Evaluate前失败：丢弃该Actor Pending事务。
- Base Physical Pose已经应用后失败：该Actor进入Faulted，不恢复Transform。
- 某Actor team在global call前验证失败：该Actor进入Faulted，其team不得进入本帧batch；其它合法Actor继续。
- Magica global manual call抛异常、team完成集合不确定或发生无法归属的写入：全部参与Actor进入Faulted。
- Final capture或FinalPublication失败：对应Actor进入Faulted。
- Faulted Actor拒绝后续Prepare，不自动重建team、不关闭节点继续旧动画、不切换自动AfterLateUpdate。

## Decision: Reset与可见性由节点生命周期统一管理

以下事件必须在下一次Base Pose应用后、Magica求解前调用group reset到当前动画Pose：

- Body stream reset或committed branch replacement。
- teleport、actor registration replacement或visual root discontinuity。
- Projection/Profile/Rig revision变化。
- Preview scrub、time jump、target切换或session restart。
- actor因不可见而暂停后重新进入正式Presentation。

正常连续帧保留Magica history。项目角色不使用Magica camera/distance culling；可见性暂停由Presentation runtime显式登记，恢复时执行上述reset。这样“没有求解”是可观察状态，不会被插件内部culling静默吞掉。

## Decision: Corin按业务分组，不按Renderer分组

### Skirt

- roots：`Skirt_01`、`Skirt_02`、`Skirt_03`、`Skirt_04`、`Skirt_05`、`Skirt_06`、`Skirt_07`、`Skirt_08`。
- 每条root包含3根骨，共24根controlled Physical Bone。
- roots按腰围实际顺序保存，使用`SequentialLoopMesh`。
- colliders绑定Pelvis、左右Upper Leg；是否加入Lower Leg由正式Collider几何决定，但不能运行时猜测。

### Hair

- Side Hair：`Hair_S_01`、`Hair_S_06`、`Hair_S_10`、`Hair_S_11`。
- Front Hair：`Hair_F_01`、`Hair_F_02`。
- Back Hair：`L_Backhair_A_01`与`R_Backhair_A_01`，其分支作为同root后代进入各自左右group。
- colliders绑定Head、Neck、左右Shoulder和Upper Back。

### Accessories

- Waist Spring：`Spring_L01`、`Spring_R01`。
- Torso Chain：`S_ChainF_01`、`S_ChainB_01`。
- 各自使用独立group和较小Simulation Weight，避免与裙摆共享碰撞或阻尼参数。

### Weapon

`Weapon_Lever_*`、`Weapon_saw*`和主要`Weapon_Etc_*`表达机械运动与攻击表演，继续由Clip/Action Pose拥有。把它们交给Secondary Motion会使攻击时序、锯片位置和手部关系失去作者控制。只有以后确认某条骨链纯属松散装饰，才可作为新group加入同一Profile；这不需要拆网格或新增节点类型。

## Decision: Preview、Watch和Diagnostics使用post-secondary真相

Pose Graph Preview使用同一Projection、Rig binding、manual Magica batch和三段事务。没有完整Rig或Magica setup时，节点返回typed Unavailable并阻止FinalPublication，不生成“跳过次级动画”的预览结果。

Secondary Motion Watch可显示：

- Base Local Pose与post-secondary Local Pose。
- 每group的root、controlled bone、animation follow、simulation weight和reset generation。
- collider binding与碰撞统计。
- Magica team identity、substep、completion、耗时和typed failure。
- 每骨位置/旋转修正量，但只在存在Pose Watch interest时复制固定容量页。

Diagnostics只从成功Seal的Committed post-secondary页读取，不从当前Transform反推历史，不第二次调用Magica。

## Decision: Document v3只编辑节点装配

共享Capability为`SecondaryMotion`声明：

- root Pose Graph only。
- `profile`强类型对象引用。
- `pose.local`输入与输出。
- `ExternalPhysicalPose` execution domain。
- `Open Secondary Motion Profile`命令。

`CharacterSecondaryMotionProfile`正文涉及Physical Bone和Collider几何，保持专用Profile Inspector作者入口；Document v3的`context/asset-catalog.json`只读输出可引用Profile identity、类型、Rig lineage和revision。`graph.json`只保存结构化对象引用。Exporter、strict codec、Reconciler和typed Presentation Mutation能够创建、配置、删除节点并反向导出同一引用。

不增加Secondary Motion专用MCP、profile JSON分片、SerializedProperty写入或Magica组件路径。

## Migration and Cleanup

1. 等待`replace-pose-ik-with-finalik-full-body-solver`完成并归档，基于其最终Rig v4与FullBodyIK末段重对账本change delta。
2. 通过Magica manual/delta/team/completion Hard Stop Gates。
3. 建立Profile、Global Settings、Projection payload和Magica adapter。
4. 原子迁移Presentation target为三段批接口，删除旧单调用接口。
5. 安装`SecondaryMotion` Capability、Compiler、stage和Physical Publication Coordinator。
6. 同步Document v3、Mutation、Validator、Preview和Diagnostics。
7. 创建Corin Profile、Collider与root group，迁移root Pose Graph。
8. 显式重建Corin Presentation Projection、Native Pose Program和Float32/Fixed产品。
9. 删除旧Final Writer终态语义、自动Magica更新和任何临时组件接线。
10. 更新current specs、`openspec/project.md`与BTSMTL Agent Authoring当前合同。

不提供旧Presentation target adapter、自动AfterLateUpdate兼容、缺失Profile passthrough、无Magica backend passthrough或旧Final Pose reader。

## Tradeoffs

### 选择Magica全局manual batch

收益：复用成熟Bone Cloth约束、碰撞、惯性与多team Job调度；多Actor每帧只模拟一次；最终Pose可以在同一事务内捕获。

代价：必须维护一小块vendor I/O seam，并把Presentation调度从单调用改成批次；Magica升级时需要对账manual API。

### 不选择默认AfterLateUpdate组件

收益是接入最快，Prefab调参也最直接。

代价是它在`Seal`后改骨，`FinalAnimationPoseFrame`与可见结果分裂，Preview/Watch不可信，多角色更新顺序也不受Pose Graph控制，因此不作为正式方案或fallback。

### 不选择自研PurePose布料节点

收益是可以在Dense Pose Buffer内完成，保留一次Physical写入，也无需全局batch。

代价是项目要重新实现并维护约束、碰撞、稳定性、substep和工具，超出Gameplay demo重点；本change选择成熟Magica。

### 不选择shadow skeleton

收益是无需让Magica读取正式Physical Rig，也可以先模拟后一次写最终骨架。

代价是每Actor复制完整Transform层级，增加Pose双向拷贝、生命周期、内存和调试分裂，并违反项目禁止shadow skeleton的既有方向。

### 不选择一个group一个Pose节点

收益是Graph上每类次级动画更显眼，也能单独连权重。

代价是Magica全局Manager不能按普通节点顺序独立推进，容易产生多次global simulation或虚假的节点顺序。一个节点、多group更符合真实执行边界。

### 不拆SkinnedMesh

拆分Renderer可以改善内容管理或按部件换装，但不能自动解决骨骼穿模；还会带来网格重导出、材质、blend shape和蒙皮边界成本。现有独立骨链足以驱动同一整块网格，因此本change不拆网格。

## Open Questions Deferred by Evidence

- Magica运行时从编译payload建立Bone Cloth team是否完全支持PreBuild数据注入，必须由Hard Stop Gate证明。
- Magica manual delta seam的最小vendor修改面和插件升级冲突清单必须在实施前生成。
- Corin头发分支在Automatic Mesh与Line连接模式下哪一种更稳定属于正式Profile内容调参，不改变节点架构。
- 是否需要Lower Leg collider取决于Corin裙摆长度和动作范围；必须在同一Profile中显式选择，不能由Runtime自动增加。
