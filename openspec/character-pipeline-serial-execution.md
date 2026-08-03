# Character Pipeline 串行执行基线

运行业务恢复与大改前闭环基准见[`character-pipeline-runtime-behavior-baseline.md`](character-pipeline-runtime-behavior-baseline.md)。后续Corin链路修复必须先满足该基准，不得继续用速度阈值把普通Walk、前闪避后的Run与MovingTurn恢复混为一类。

## 目标

本文件固定当前工作区从BTSMTL作者基础、动画表现数据、共享Graph UI、已有动画能力接入、Corin资产迁移到既有DeterministicRollback产品重新发布的唯一串行顺序。各active change的任务编号继续作为稳定追踪ID，但实际实施顺序以本文件和`openspec/project.md`为准。

本轮不重新设计Rollback、Fixed KCC、Virtual Bone、TwoBoneIK、FootPlacement、BlendSpace求解、Motion Matching搜索、Transition Routing或动画运行算法。已经完成的能力只接入新的共享authoring、Document v3、Pose IR、资产迁移和产品发布链。

## 最终正式链路

```text
Human UI / Agent Document v3
  -> 唯一Authoring Capability Catalog
  -> 共享Graph Authoring Domain Framework
  -> typed Gameplay / Timeline / Presentation Mutation
  -> 正式Unity authoring资产
  -> BTSMTL Semantic IR + Pose IR
  -> Float32 / Fixed Program + Presentation Projection + Native Pose Program
  -> 既有Local / DeterministicRollback Composition
  -> 既有Relay + Peer A + Peer B产品
```

表现Graph也必须编译，但它不进入Rollback权威状态：Pose authoring先降低为Pose IR，再生成Presentation Projection与Native Pose Program；网络不发送Pose、节点实例或Graph运行内存。Local Float32、Local Fixed与DeterministicRollback由显式Variant选择Simulation Program和Session/Network Model；它们可以使用不同同步方式，但必须引用同一角色语义、Presentation Projection、Rig和Pose产品身份。同步方式属于Variant与网络装配，不属于Pose节点或编辑器字段。

## 当前执行指针

1. 阶段1的Document基础、五个生命周期工具与旧Patch入口清理已经闭合。
2. 阶段2的Compiler、Runtime、Preview、diagnostics和旧运行路径清理已经闭合。
3. 阶段3的Capability、typed payload、Presentation Mutation、Validator、Pose IR、Document v3、Reconciler与统一事务已经闭合。
4. 阶段4已经按BTSMTL原UI行为基线完成原地共享抽象，Pose与Action Workspace消费同一Canvas、Node、Port、Details、Navigator和StateMachine作者表面；BTSMTL原操作不再改动。
5. 阶段5的Action Workspace与14A动画能力接入门禁已经闭合，没有新增第二GraphView、第二Mutation或第二Compiler入口。
6. 阶段6已经完成唯一Corin Document v3事务：精确Definition checkout、目标状态编辑、dry-run、exact hash apply、Gameplay/Timeline/Presentation原子提交、canonical reverse export；最终状态为`Clean`且再次dry-run为`0 diff`。
7. 阶段7已经通过精确Corin Definition显式发布Float32 Program、Fixed Program、Presentation Projection与内嵌Native Pose Program；Float32、Fixed与Projection拥有相同Program identity、Source revision和Semantic hash，旧revision内容已经被正式产物原子替换。
8. 阶段8的装配结构已经闭合：Local Fixed与Rollback Variant共享Fixed Program、Projection、KCC和Collision引用，Rollback Product workflow仍只发布IL2CPP Player与纯.NET Relay。包含0.40m超限楼梯的唯一Collision Artifact已于2026-08-03显式重新Bake，正式`CollisionWorldHash`为`02512d39104d34b650a5667c276cbc46ce5f6a7e77383f758b2862ff27a66ff5`；上一份KccId和产品manifest继续失效，当前产品闭包等待Local Fixed验证和Peer A/B重新发布验证。
9. 2026-07-31的Locomotion运行回归已经通过同一正式链修正：Gameplay提交Movement Mode Identity，PoseStateMachine只消费该离散事实；完整MovingTurn曲线、DodgeForward到RunLoop和MovingTurn回RunLoop业务边已经由唯一Document v3事务恢复；Local Fixed与Relay + Peer A/B运行闭环完成。当前主链执行指针已结束，BlendSpace、Motion Matching独立内容和Corin训练AI继续排在其后，不得反向修改Corin Rollback装配。
10. 2026-07-31曾尝试把Gameplay MotionCurve sample绑定为MovingTurn Sequence时钟；该口径已被正式动画控制边界否决。当前唯一实现是Gameplay Timeline独立提交Body Root Motion，Pose Sequence只观察committed Movement Mode并按`PresentationDelta`连续播放，不读取Gameplay Timeline、MotionCurve sample或第二时钟事实。
11. 2026-08-01按最终手感选择将MovingTurn收口为固定180°短Root Motion：Gameplay只允许RunLoop以135°门槛进入，RunEnd重新收到输入时先回规范RunLoop再由同一门禁决定；60Hz有限Timeline保留0–28帧，29个贡献直接使用Root Motion Baker输出的Unity米制值，累计X/Z为`(-0.9001478, 0.4623734)`、yaw为180°，不再乘`0.01`。Gameplay输入转向和Pose RootOrientationWarp不再重复拥有朝向；Presentation的RunStart、RunEnd只在观察到已提交Turn事实时进入同一Turn Pose。状态只在`state_root_completed`后释放；进入Turn使用0.12秒Inertialization，退出到循环Locomotion使用0.30秒，循环状态保留连续相位。该资产变化已通过唯一Document v3事务提交，并显式发布Float32、Fixed、Projection与Native Pose产品。
12. 2026-08-01运行闭环使用真实Input System键盘事件经过Input Profile、Fixed Adapter和Fixed Program采集正式诊断：三个相互隔离的反向输入样本均恰好进入和退出Turn一次；运行时Pose快照读取到0.12秒进入与0.30秒退出，未出现角色链路错误。Local Fixed与DeterministicRollback继续注册同一Animation Presentation诊断目标；临时输入与抓帧探针已删除。
13. 2026-08-01产品目录清理阶段移除了Gameplay Lab根目录与聚合子目录并存的重复资产：生成器、Local Fixed Prefab、Gameplay Lab场景和Product adapter现在只引用`Compositions/Pipelines/Sources/Variants`正式GUID。该阶段的Build与Run身份已由第15项当前产品闭包替换。
14. 2026-08-01最终修复动作与MovingTurn的选择冲突：唯一`RunLoop -> MovingTurn`边同时要求Attack与Dodge Action Context未激活，动作存续期间不再隐藏提交Turn，动作退出后持续反向输入只进入一次。随后把门槛调整为135°、Turn进入调整为0.12秒、退出调整为0.30秒，并让Idle、WalkLoop与RunLoop保留连续相位。Document v3重新回到`Clean`，当前SourceRevision为`bc2b4a28de68e42cb6e88abe8dc1d26e70c4ad30deb1f37b909707ffc8d0a974`；Float32与Fixed共同使用产品SourceRevision `ed4eb13950a91eb0861b643fb0475aa03cf9df2ae751c2302e8eee2d1ba71b3a`和SemanticHash `c9aad75ce4fa7a113260da33b4bc16757d38e95e3b8dff7e5e46a18355b21324`，Fixed ProgramHash为`fdb5cdbf9ce175dc55588fff903068611d620a33ca983a135637e5d5dab4866d`，ProjectionRevision为`46be7b6a85b7a2571655a7ff318b758c28a203613da53fc2359deffb339d8652`，ContractHash为`1fbdc21bacec544e9b7e9ea6b6ee7d7f1d53895cb231cff80549b11ac0acce73`。
15. 2026-08-01以第14项当前产品身份重新执行正式Prepare、IL2CPP Product Build与manifest-only Run。BuildId `20260801-133626`的91项精确闭包直接锁定Fixed ProgramHash `fdb5cdbf9ce175dc55588fff903068611d620a33ca983a135637e5d5dab4866d`和ProjectionRevision `46be7b6a85b7a2571655a7ff318b758c28a203613da53fc2359deffb339d8652`；Run `20260801-214029`推进到`canonical=4538`，Peer A/B前沿为`4539/4538`，`invalid=0`、`dropped=0`且无角色链路错误，结束后三端进程与`24100/24101/24102`已精确释放。
16. 2026-08-03的KCC收口已经把源码升级为Motor `fixed-philippe-kcc-motor/6`、Solver `8`与KCC schema `deterministic-kcc/6`，并完成Gameplay Lab作者环境与唯一Collision Artifact重烘。第15项只保留旧产品历史证据，不再代表当前源码闭包；下一执行指针固定为先验证Local Fixed，再按当前Motor与Collision身份重新发布并验证Relay与Peer A/B，期间不自动构建也不把KCC状态接入动画事务。

### 2026-07-30闭环回归校正

- 输入：Corin有限Action Timeline、已发布Presentation Projection、Local Fixed与Rollback共享Variant。
- 处理：Projection显式保存每个有限Action的最后合法采样时间；Action投影只钳制到该边界；Action退出到Source Pose时不再建立无target的marker relation；Local Fixed生成器把Animator放入Presentation VisualRoot严格子层并固定为Always Animate。
- 输出：Float32 Program、Fixed Program、Projection v9、Local Fixed生成资产与Rollback Product重新发布；CharacterController validator成功，Local Fixed和Peer A/B运行诊断均无Presentation failure。
- 删除的旧路径：Retained Action尾帧运行时重试、按Timeline逻辑duration越界采样、Action到Source Pose的伪marker relation、Local Fixed中VisualRoot与AnimatorRoot重合的生成结构。

### 2026-07-31 Locomotion误改记录与恢复结果

- 输入：Gameplay已存在的Walk/Run/MovingTurn状态与MotionCurve、Presentation Profile中的WalkStart/WalkLoop/RunStart/RunLoop/RunEnd/MovingTurn Pose Source、原五状态Locomotion PoseStateMachine和已编译Pose Plan。
- 已执行的结构修改：通过唯一Document v3事务新增WalkStart与WalkLoop State Graph，把PoseStateMachine改为Idle、WalkStart、WalkLoop、RunStart、RunLoop、RunEnd、Turn七状态；Projection创建时一次校验完整Pose payload，运行帧只校验非空计划。
- 删除的错误路径：删除按`HorizontalSpeed`定义Walk/Run、按`FacingError`猜测MovingTurn、MovingTurn固定回WalkLoop、Yaw提前锁180度以及旧RunStart孤立入口。
- 正式处理：Float32、Fixed与Rollback从已提交Gameplay owner解析`presentation.movement-mode`；Presentation Fact把Identity按离散值重采样；共享Transition Rule compiler/runtime以Identity Literal选择Pose状态。
- 资产事务：Document hash `4b51f5067a52fc638cea4dd90e75d15aed0b817122a8922d3b2a4efc8e9f9f42`通过plan hash `3d2e62fadea1740ba30508ba2e7d83467c202788fc922eee67149509f33a4a90`执行唯一apply，canonical reverse export后Document hash为`2a66c81df57bb94e0072b97eaa577ed933c4954f2d747c66f753bd868a3d7ab8`并回到`Clean`。
- 产品身份：Fixed ProgramHash `75e0a6e16d7739db8fd5f6324f283f12a117acad73e8d95c8b39429f30b3ecb7`、ProjectionRevision `8218402e6216bed3706a939ae6a416e42eef86da3c3dbc2f5f8d2eece098e02f`、SourceRevision `2ee4e149e8b5edde3a8b4ec135a20ad760c5d5b63601fc67e6909cf16f187912`和SemanticHash `8870a4d91b24944e14839f1f2e92dc589980bc6c83b89ca8c8e05ceadd3b63c8`成为新闭环基准。
- 性能输出：删除Native Pose缓存无条件全量清零，并让四个现有`IAnimationJob`进入Burst；两个Local Fixed Actor的Animation段由约`14.8 ms`降至`6.2–6.5 ms`，Native Graph由约`9.7 ms`降至`1.7–1.9 ms`，未关闭IK、FootPlacement、BlendStack、Inertialization或Pose节点。
- Rollback输出：BuildId `20260731-071614`的正式IL2CPP产品在双端运行`20260731-152210`中推进到`canonical=938`，Peer A/B输入前沿为`942/938`，`invalid=0`且`dropped=0`。

### 2026-07-31 MovingTurn曲线与Pose相位边界校正

- 输入：Float32/Fixed Locomotion Timeline的最终MotionCurve winner、已提交MovementMode identity和MovingTurn SequencePlayer。
- 处理：Gameplay MotionCurve只在Simulation侧提交Body Root Motion；Presentation Fact只投影离散Movement Mode；MovingTurn SequencePlayer进入时重置并从此按`PresentationDelta`连续推进，不读取Gameplay curve-local sample或duration。
- 输出：动画控制边界保持`PresentationDelta`为连续Sequence唯一时钟，Gameplay Timeline和Pose Sequence分别拥有Body运动与可见Pose，不增加Action playback、snapshot、网络字段或第二Timeline runtime。
- 删除的旧路径：MovementMode clock binding、Gameplay MotionCurve sample到Pose Sequence的只读时钟投影，以及Document中的`gameplay-movement-mode-id`时钟配置。

### 2026-08-01 MovingTurn固定180°短Root Motion

- 输入：Corin Root Tree中的MovingTurn Gameplay State、源Root Motion前28帧、Turn Sequence Pose Graph、Run/Walk/Idle Presentation Transition和唯一Character Document v3。
- 处理：Gameplay入口收窄为`RunLoop + move_has + turn_facing_angle(135°)`；RunEnd重新收到输入时先回规范RunLoop；MovingTurn Graph改为唯一Inline Timeline，MotionCurve以`Local / Locomotion / Override / Priority 100 / ConsumeLowerChannels`提交0–28帧Root Motion，前25帧完成180° yaw。X/Z与切线直接保持Root Motion Baker的Unity米制值，29个贡献累计为`(-0.9001478, 0, 0.4623734)`，不再乘`0.01`；状态只以`state_root_completed`释放。Turn Pose Graph删除RootOrientationWarp，Presentation进入Turn使用0.12秒Inertialization，退出使用0.30秒，Idle、WalkLoop与RunLoop保留连续相位。
- 输出：Document v3的SourceRevision为`bc2b4a28de68e42cb6e88abe8dc1d26e70c4ad30deb1f37b909707ffc8d0a974`、EditableHash为`ede2426df11bcbc13b984fcabc5ce801a56674e988fdb111b797ecebf9a1efe0`，状态为`Clean`且正式Validator通过。Float32 ProgramHash为`c0555c1f0c859b037df320ec10a8dfd59c014be5b53bfe24dd7f49c6c4716012`，Fixed ProgramHash为`fdb5cdbf9ce175dc55588fff903068611d620a33ca983a135637e5d5dab4866d`，ProjectionRevision为`46be7b6a85b7a2571655a7ff318b758c28a203613da53fc2359deffb339d8652`。三个相互隔离的真实Fixed输入样本均恰好命中一次Turn进入与一次Run出口，运行时读取到0.12秒进入和0.30秒退出。
- 删除的旧路径：MovingTurn输入运动节点与并行包装、Pose RootOrientationWarp和LocalYaw属性、RunEnd私有Turn条件、错误的厘米到米二次缩放、旧`CorinMovingTurnOrientationWarpCurve.asset`、Rollback Product adapter中的旧扁平资产路径常量；Gameplay Lab根目录下同名Composition、Pipeline、Source和Variant副本；临时输入与抓帧探针在采集后删除。

### 2026-08-01 MovingTurn动作互斥与产品再闭合

- 输入：唯一`RunLoop -> MovingTurn`条件图、Attack与Dodge Action Context事实、精确Corin Definition、正式Fixed包装产物与Fixed运行定义。
- 处理：Document v3把两个Action Context分别取反后与既有`move_has + turn_facing_angle(135°)`汇入唯一最终AND；Gameplay Lab从`CorinFixedProgram.asset`创建Fixed Presentation Contract并校验Projection；Float32与Fixed通过同一精确Definition分别显式发布。
- 输出：Document SourceRevision `bc2b4a28de68e42cb6e88abe8dc1d26e70c4ad30deb1f37b909707ffc8d0a974`、EditableHash `ede2426df11bcbc13b984fcabc5ce801a56674e988fdb111b797ecebf9a1efe0`，状态`Clean`且正式Validator通过。产品身份以当前执行指针第14项为准；三个独立GameplayLab样本均为0角色链路错误。
- 删除的旧路径：Dodge/Attack动作拥有角色时仍可隐藏选择MovingTurn的条件路径；以Float32发布元数据否决正确Fixed Projection的错误门禁；把`CorinFixedProgramRuntime.asset`误当包装产物的目标混用；临时Input System选择探针与Float32 Semantic IR诊断副本。

## 唯一串行阶段

### 阶段1：收口Agent Document基础

执行`refactor-agent-authoring-to-synced-json-document`剩余任务，只完成Document Store、strict package、Reconciler、Mutation Plan、Application Service、五个生命周期工具和旧Patch入口删除。该阶段不继续扩展Presentation只读v2模型，不迁移Corin资产，不Build生成产物。

完成门槛：

- v2基础设施中仍被v3复用的逻辑全部闭合。
- `checkout/rebase/dry-run/apply/validate`是唯一五工具合同。
- Reconciler只生成不可变计划，Application Service唯一拥有Undo、rollback、save和package原子发布。

### 阶段2：冻结动画运行与数据边界

执行`refactor-animation-control-boundaries`的代码、Compiler、Runtime、Preview、diagnostics和旧运行路径清理任务，保留其Corin资产迁移与最终Build任务未完成。

`add-character-animation-virtual-bones`已经完成的Rig v2、Virtual Bone、TwoBoneIK、Mask/Profile、Native operation和Corin业务配置作为输入合同，不重新实现。`add-character-presentation-blend-space`和`add-character-motion-matching-pose-source`已经完成的能力代码作为输入合同；其独立演示内容任务不在本阶段执行。

完成门槛：

- Action与state-local Pose source使用不同ABI。
- 正式Pose顺序固定为`PoseStateMachine -> AnimationSlot -> composition -> TwoBoneIK -> FootPlacement -> OutputPose`。
- Physical/Virtual/Pose Bone数量、Mask/Profile、Transition Routing、BlendStack和Inertialization所有权唯一。
- 旧Selection、旧Playback总管、BaseLocomotion AnimationChannel和图外手臂IK不可达。

### 阶段3：收口共享Pose authoring逻辑与编译链

执行`refactor-pose-graph-to-btsmtl-authoring-domain`的共享Capability、typed payload、Presentation Mutation、Validator、Pose IR、Native Plan builder、Document v3模型、Exporter、Reconciler和Application Service事务任务。

本阶段先完成逻辑与数据，不迁移Corin资产：

- BTSMTL、AI和Pose只通过唯一Capability表达kind、field、port、command和compiler handler。
- Pose节点使用独立typed payload，不保存大联合体或逐实例固定port镜像。
- 每个Pose capability拥有独立Validator和Pose IR handler。
- Character Document破坏性升级为v3，Presentation进入editable，Rig与Virtual Bone正文继续只读。
- Gameplay、Timeline和Presentation进入同一资产级事务，任一失败恢复全部Unity owner与上一份Document package。

### 阶段4：收口共享UI并接入Action Workspace

先完成`refactor-pose-graph-to-btsmtl-authoring-domain/tasks.md`的0U恢复与原地抽象门禁。共享Canvas、Node、Port、Details、Navigator与StateMachine Surface必须从现有`BaseTreeWindow`、`BaseTreeView`、`BaseNodeView`、`BasePortView`、`PropertyPortView`、Edge View、`BaseTreeInspectorView`与`GraphDataCatalog`成熟实现中原地提取；禁止新写功能更少的替代GraphView再切换BTSMTL。

阶段4唯一顺序：

```text
恢复被错误删除的BTSMTL UI与可编译状态
  -> 固定全部原操作的输入、处理、输出和owner
  -> 从原实现原地提取domain-neutral交互
  -> BTSMTL binding先接回同一实现并保持原行为
  -> Pose binding接入同一实现
  -> Action Workspace接入同一实现
  -> 删除被抽空的BTSMTL专用壳与错误替代UI
```

原BTSMTL窗口分区、节点信息密度、黑板变量拖拽、Flow/Property Port、搜索与创建、selection、框选、clipboard、Undo、Inspector、SubTree/StateMachine/Condition Rule下钻和Live Debug属于业务行为基线。若共享化必须改变其中任何一项，立即停止并等待用户选择，不得自行重设计。

Action Workspace只读取阶段2已经稳定的Action Playback、AnimationSlot、三层时间、Preview和diagnostics合同，并复用阶段3安装的Capability、typed Mutation、owner导航和Document v3边界。它不得等待`refactor-animation-control-boundaries`资产迁移或归档，也不得创建第二GraphView、第二Timeline、第二Presentation Mutation或角色私有资产。

完成门槛：

- BTSMTL与Pose不再保留独立Canvas、Node、Port、selection、clipboard、Undo或Inspector基础实现。
- 唯一共享实现来自原BTSMTL UI代码的抽取；BTSMTL全部既有操作保持，且不存在替代式新Canvas。
- 正常作者界面不显示GUID、revision、compiler index、workspace offset、Document hash或generated payload。
- Action Workspace能够从精确Definition/Action/Timeline/Profile/Pose owner建立typed session，不复制字段。
- selection、focus、repaint、asset import、Preview和Live Debug均不触发Build、Bake或分析。

### 阶段5：对账全部已有动画能力

在资产迁移前完成下列接入，不重写既有算法：

| 能力 | 新authoring接入 | 保持不变的运行边界 |
|---|---|---|
| Virtual Bone | Rig v2只读catalog与Bone picker | 每个source capture阶段派生，不绑定Animator Transform |
| TwoBoneIK | typed payload、Capability、Details、Document v3、Pose IR handler | 降低到既有Native descriptor，只写Physical chain |
| FootPlacement | typed payload、Profile/Calibration引用、Capability、Pose IR handler | 唯一world-aware阶段，位于TwoBoneIK之后 |
| BlendSpacePlayer | typed payload、动态/固定port、state-local source binding | 使用既有BlendSpace solver，不进入Gameplay channel |
| Motion Matching | PoseState provider capability、只读Profile/Database引用 | 使用既有MM Module和state-local ABI，不进入Action Playback |
| AnimationSlot | typed channel/slot/policy引用 | 只消费有限Action committed sample |
| BlendStack/Inertialization | node-local typed policy | 继续拥有各自连续性状态，不建立图外平滑 |
| Layer/Additive/Mask | typed payload与Rig v2 picker | 全部按PoseBoneCount运输 |
| Transition Routing | Transition/Slot typed policy与只读diagnostics | 只提供route decision和release握手，不拥有Pose/Player |

任何能力若仍要求在Pose专用GraphView、Inspector switch、Agent专属catalog或顶层Compiler kind switch中重复登记，阶段5不得完成。

### 阶段6：一次性迁移Corin正式资产

阶段1至5全部完成后，执行`refactor-pose-graph-to-btsmtl-authoring-domain`迁移器和`refactor-animation-control-boundaries`剩余Corin资产任务。两份change描述的是同一次业务迁移，不得分别执行两次写入。

唯一迁移顺序：

```text
显式checkout Document v3
  -> 编辑Profile / Pose Graph / PoseStateMachine / Slot目标状态
  -> dry-run
  -> 使用同一Document hash执行apply
  -> Gameplay + Timeline + Presentation单事务提交
  -> canonical reverse export并回到Clean
```

同一事务必须保留并重写：

- Presentation Pose source binding。
- Locomotion PoseStateMachine、state-local Sequence/BlendSpace选择和Transition。
- FullBodyAction AnimationSlot与Blend/Inertialization Policy。
- Rig v2、Virtual Bone、Mask/Profile引用。
- 左右臂TwoBoneIK、FootPlacement与唯一OutputPose顺序。

同一事务必须删除：

- BaseLocomotion Gameplay AnimationChannel及旧Timeline表现数据。
- ActionOverride和旧ownership Blackboard数据。
- 旧Pose联合体、固定port镜像、旧inline carrier和旧Document v2正文。

### 阶段7：显式发布Character产品

只在Document v3为Clean且旧资产路径已删除后执行精确Definition Build：

```text
Corin Definition
  -> validated Semantic IR
  -> Float32 Program
  -> Fixed Program
  -> target-neutral Presentation Projection
  -> Native Pose Program
```

Build必须原子发布并校验SourceRevision、SemanticHash、ContractHash、ProgramHash、ProjectionRevision、Rig revision和ordered producer contract。不得由Inspector、selection、窗口恢复、Document apply或asset import自动触发。

### 阶段8：恢复既有DeterministicRollback产品闭环

执行`close-deterministic-rollback-character-pipeline`。该change只把阶段7的新Fixed Program和Projection接回已经存在的Rollback Composition、Fixed KCC、Collision Artifact、Relay与Peer A/B产品，不重新接入KCC算法，不创建第二场景、第二Host或第二碰撞作者源。

完成门槛：

- Local Fixed与Rollback Variant引用相同Program、Projection、KCC和Collision identity。
- Product manifest锁定相同SemanticHash、ProgramHash、ProjectionRevision、CollisionWorldHash与KccId。
- Build只打包已发布输入；Run只验证并启动既有Relay、Peer A与Peer B。

## Rollback闭环后的独立串行队列

以下change保留独立业务价值，但不得修改阶段1至8的Corin Rollback装配：

1. `add-character-presentation-blend-space`剩余独立内容演示；缺少完整八向素材时保持未完成。
2. `add-character-motion-matching-pose-source`剩余独立Definition、Profile、Database和Clip内容闭环；不得借用Corin资产。
3. `add-corin-training-ai-demo`；在Document v3安装后创建Local训练AI，不进入Rollback AI。

## 跨change任务归属

- `refactor-agent-authoring-to-synced-json-document`只拥有可复用Document基础，v3 schema与Presentation editable只属于Pose authoring change。
- `refactor-animation-control-boundaries`拥有动画运行职责和Corin业务目标；Pose authoring change拥有完成该目标所需的typed authoring、Document v3、迁移事务和生成产品接线。
- `add-character-animation-virtual-bones`拥有Rig v2、Virtual Bone与TwoBoneIK算法及既有Corin业务配置；Pose authoring change只负责新schema重编码和共享作者入口。
- BlendSpace与Motion Matching change拥有领域算法和独立内容；Pose authoring change拥有其节点Capability、公共UI和Document/Compiler接线。
- `add-action-animation-authoring-workspace`只拥有Editor聚合工作面，不拥有任何Gameplay、Timeline、Profile、Pose或Runtime数据。
- `close-deterministic-rollback-character-pipeline`只拥有迁移后产品重新装配，不拥有Character authoring、Pose算法、KCC算法或网络模型。

## 停止条件

- 需要恢复旧reader、fallback、双写或临时adapter才能继续时停止。
- 任一能力需要第二Capability Catalog、第二GraphView、第二Mutation或第二Compiler入口时停止。
- Corin迁移需要绕过Document v3直接修改YAML时停止。
- Character产品或Rollback产品需要按selection、场景名、目录或显示名猜输入时停止。
- 缺少独立BlendSpace/MM正式素材时只停止对应独立内容change，不阻塞阶段1至8。
