# Change: 重构角色Pose Graph架构

## Why

当前Pose Graph的作者拓扑、typed端口、单次PlayableGraph Evaluate、唯一Goal Assembler、唯一FullBodyIK和唯一Final Writer方向正确，但运行与编译实现没有形成同等清晰的所有权。

当前工作区中：

- `PosePlanExecutionRuntime`约5184行，同时持有Pose source、Player、PoseState、Blend Stack、AnimationSlot、Animancer backend、Physical Source、Motion Matching、Native Program、Workspace、Inertialization、Foot Placement、Goal、FBBIK、Final Writer、release与Diagnostics。
- `CharacterPoseGraphStagedExecutor`约4172行，把Program、Workspace、Slot、Value、Inertialization、Goal和Diagnostics的一百多个Native页展开到单个执行器中，调用Interface几乎暴露全部Implementation布局。
- `CharacterPoseGraphNativeProgram`同时保存不可变操作表、可变控制页、Pending/Committed状态和Goal workspace，因此“Program”并非不可变程序。
- `CharacterPresentationPoseOperation`使用约40项字段表达全部节点，大量`-1`既表示“不适用”又参与合法性判断；任一Goal、Linked Pose、Player或Control字段变化都要求跨Compiler、Projection、Native Program、Executor、Validator和Diagnostics同步。
- `CharacterPresentationPosePlanCompiler.CompilationState`是中央可变黑板；闭包、端口、节点语义、Value、Workspace、Operation和Stage规划互相原地修改，无法从一个阶段的固定输出定位错误。
- 节点语义分散在Capability Catalog、Authoring Adapter、Clipboard、Document、Compiler Handler、Projection Validator和Payload Codec。现有`ICharacterPoseCompilerHandler`暴露大量节点特例布尔值，但中央Compiler仍识别具体节点，形成没有隐藏复杂度的浅Module。
- World-aware Foot Operation由外层Runtime先执行并写入Goal，再由Staged Executor执行同一Operation的另一部分；Diagnostics又分别读取Native Program、Workspace和Pose Constraint，导致一个节点、一个结果和一个完成身份存在多个知识Owner。

结果不是单纯“文件太长”，而是业务改动需要理解太多内部布局：新增一个节点、修改一个Goal字段、调整一次Source生命周期或诊断字段都会横跨多处；任何遗漏都可能表现为运行期非法组合、半帧状态、重复控制权或行为回归。

本change不重新设计动画效果，而是把已批准的动画行为放入最终PoseGraph架构：用唯一Node Definition、固定Compiler Pass、不可变Program Image、每Actor状态、每帧事务、深Runtime Module、单一Operation执行Owner和Committed Result诊断链，取代当前巨型协调器、中央黑板、万能Operation与跨Module内部页读取。

## What Changes

- 新增唯一`CharacterPoseNodeDefinitionModule`。每种Pose节点通过一个Definition Adapter集中声明Payload类型、字段合同、固定/动态端口、允许Graph Role、Execution Domain、Authoring codec、局部校验、typed lowering和Operation Family。Canvas、Document v4、Clipboard、Mutation、Compiler与局部Validator读取同一定义；删除旧Compiler Handler布尔矩阵和重复node-kind switch。
- 将Pose Plan Compiler重构为固定不可逆Pass：`Graph Closure -> Typed IR -> Topology -> Value Plan -> Workspace Plan -> Family Payload -> Stage Schedule -> Seal Program Image`。每个Pass只消费上个Pass的不可变Result并产生结构化诊断；删除中央可变`CompilationState`。
- 用不可变`CharacterPoseProgramImage`替换当前混合状态的`CharacterPoseGraphNativeProgram`。Program Image只保存Projection identity、Rig、typed stage schedule、operation headers、family payload pages、常量页、value layout、workspace layout、source map与容量，不保存Actor状态、Pending/Committed页或当前Frame结果。
- 将每Actor可变状态收敛为`CharacterPoseActorState`，保存PoseState、Player continuity、Slot、Blend Stack、Routing、Inertialization等会影响下一帧的已提交状态；将当前帧写入收敛为`CharacterPoseFrameTransaction`，保存同一Frame/Completion/Rig/Program lineage下的Pending控制状态、Value workspace、Module result、journal和可选Diagnostics页。
- 用分段typed ABI替换万能`CharacterPresentationPoseOperation`：公共Header只保存调度与typed value引用，Player、StateMachine、Slot、Blend、Inertialization、Composition、Space Conversion、Component Control、Goal Contribution、Goal Assembler、FullBodyIK、Linked Pose与Output等Operation Family分别拥有固定Payload页。旧万能字段、`-1`组合、旧reader与兼容Projection直接删除。
- 新增深`CharacterPoseSourceModule`，唯一拥有provider sample装配、Animancer/Playable source、Physical Source Registry、capture binding、prepared resource、usage、retirement与deferred release。PoseState、Player、Transition、Slot与Blend的逻辑决定仍由Pose Program节点拥有；Source Module不得成为第二选择器或权重Owner。
- 新增深`CharacterPoseProgramRuntime`，唯一拥有Program Image、Actor State、Frame Transaction、Value workspace、持久Executor和全部Pose Operation调度。它按编译Stage恰好执行每个Operation一次；外层Runtime、Source Module、Constraint Module和Diagnostics不得再次解释Operation。
- 保留并深化前置Foot重构产生的`CharacterPoseConstraintRuntime`，唯一拥有Foot Placement、PoseBone Goal、Goal Contribution、Goal Assembler、唯一Goal Set、FBBIK、BendHistory和Solver Result。它只接收typed Component Pose与Frame facts并返回typed Constraint Result，不向调用方暴露NativeSlice、Goal offset、Operation index、Callsite index或内部Bank页。
- 新增具体`CharacterFinalPosePublication` Module，唯一负责完整Final Pose验证、Physical Writer binding、一次整Rig写入、Committed/Pending Final Pose与Publication Result；当前只有一个Writer Implementation，不建立假设性可替换接口。
- 将`CharacterAnimationPresentationRuntime`收窄为帧级协调根：`Begin -> Plan Control/Demand -> Prepare Sources -> Prepare Program -> Validate Barrier -> Animancer Evaluate -> Complete Program/Constraints -> Publish Final Pose -> Seal`。它只交换typed Result和共享Frame Lease，不保存节点业务状态、不理解Operation字段、不执行Foot/Goal/FBBIK数学。
- 将Diagnostics改为单向Projector，只从同一成功Frame提交的`Source Result + Program Result + Constraint Result + Final Publication Result`及interest-gated冻结页生成Snapshot。删除对Native Program内部数组、Pending Workspace、Constraint内部状态和Physical Transform反推的读取。
- Runtime与Preview继续使用同一Program Image、Module Factory、Frame事务、Source backend、World Context Adapter和FinalIK Pose Buffer backend；Preview不得保留简化Executor或临时Program。
- 迁移完成后删除旧`PosePlanExecutionRuntime`巨型Implementation、旧`CharacterPoseGraphNativeProgram`混合容器、旧`CharacterPoseGraphStagedExecutor`巨型构造、旧`CharacterPresentationPoseOperation`万能ABI、旧Compiler Handler Registry、旧中央CompilationState、重复Validator/Codec知识和Diagnostics内部读取，不保留wrapper、开关、fallback或双运行链。

## Dependencies And Sequencing

1. `refactor-character-pose-constraint-transaction`必须完成、由用户验收并归档，使Goal Contribution、唯一Assembler、唯一Goal Set、FBBIK、Writer与Pose Constraint Bank先形成稳定外部合同。
2. `improve-character-foot-placement-behavior`必须随后完成、由用户验收并归档，使本change能把最终Foot行为作为不可修改Oracle，而不是在PoseGraph迁移期间继续改变Foot政策。
3. 已完成的`replace-animation-sequence-with-clip-authoring`必须归档，`add-character-presentation-blend-space`必须完成剩余实现并归档；本change只迁移最终ClipPlayer、BlendSpacePlayer和source-local runtime，不保留Sequence命名或第二实现。
4. Linked Pose、Motion Matching、Transition Routing、Blend Stack与Inertialization按实施时current spec作为节点与Source生命周期输入；本change只迁移所有权和ABI，不改变其业务数学。
5. 本change实施期间不得并行实施新的Pose节点、动画行为或Constraint行为change。Proposal可以现在存在，但Runtime实施只按上述顺序开始。

## Impact

- Affected specs: `character-presentation-pose-graph`、`character-animation-pipeline`、`character-animation-selection-runtime`
- Added specs: `character-pose-graph-runtime-architecture`、`character-pose-plan-compilation`
- Affected runtime: Animation Presentation协调根、Pose source lifecycle、Pose Program、Native workspace、staged execution、Pose Constraint装配、Final Pose publication、Diagnostics
- Affected editor/compiler: Pose capability/definition、Document/Clipboard/Mutation adapter、Projection Compiler、Topology Validator、Source Map、Projection codec与Build validation
- Affected generated data: Character Presentation Projection schema、Pose Program Image schema、Operation ABI、Workspace layout、ContractHash与ProjectionRevision
- 不影响Gameplay Program、Simulation Tick、KCC、Root Motion移动决策、Network Model、Rollback Snapshot或业务Timeline语义

## Current Spec Comparison

- current `character-presentation-pose-graph`已经规定唯一typed拓扑、有序Stage、一次PlayableGraph Evaluate、唯一FBBIK与唯一Writer。本change保留这些外部语义，只补足Program Image、Node Definition、Compiler Pass、Operation Family ABI和Runtime Owner。
- active `refactor-character-pose-constraint-transaction`把Goal Source从多个Goal Set改为typed Goal Contribution、唯一Assembler和唯一Goal Set。本change以其归档结果为输入，不恢复plural Goal Set、Empty Goal、LegIK、TwoBoneIK或第二Solver。
- current `character-animation-pipeline`已经规定唯一`Prepare -> Validate -> Animancer Evaluate Barrier -> Seal`事务、Dense双页、稀疏journal与Fault边界。本change不推翻事务政策，而是把Program、Actor State、Frame Transaction和Module Result明确分层，删除各内部对象重复持有Pending/Committed知识。
- current `character-animation-selection-runtime`已经规定PoseState、Player、Transition、Slot、Blend Stack、Inertialization和Animancer各自业务权限。本change保持这些节点语义；Source Module只收物理采样与资源生命周期，Program Runtime收逻辑节点状态，二者不得互相抢权。
- current `character-presentation-pose-graph`要求新增节点注册typed payload与compiler handler。本change将该要求改为注册唯一Node Definition Adapter；旧Handler Registry及其布尔能力矩阵在迁移后删除。
- current Preview、Pose Watch和Live Debug允许读取完成workspace，但当前实现仍跨Native Program、Constraint与Workspace取值。本change把读取对象收敛为Committed Result和interest-gated诊断页，不改变可观察业务内容。
- archived `refactor-animation-control-boundaries`曾规定外层协调器与Pose Plan执行职责分离，但archive不是current truth，当前实现也没有保持该Locality。本change把这项方向重新写成可执行的current delta，并明确删除标准，避免只新增类名而保留巨型Owner。

## Non-Goals

- 不改变PoseState选择、Transition rule、Standard Blend、Blend Stack、Inertialization、Slot、Linked Pose、Blend Space、Motion Matching、Foot Placement、Goal、FBBIK或Writer的逐帧业务结果。
- 不新增Pose节点、Layer、Control Rig、传统Animator Controller、第二PlayableGraph、第二Animator、第二IK solver或GPU动画路径。
- 不重新设计Foot Contact Plan、Heel/Toe、脚掌旋转、Reactive、移动平台或上下楼专用动画。
- 不改变Gameplay/Presentation边界，不把Pose、Program Actor State或Frame workspace写入Rollback snapshot或网络协议。
- 不创建通用插件容器、运行时反射Handler、任意Operation注册表、Solver抽象接口或只有一个Adapter的假设性Seam。
- 不以拆分文件行数作为完成标准；如果调用方仍需理解内部页、offset、index、Seal顺序或节点特例，视为重构未完成。
- 不新增自动测试；实施阶段只执行项目规定的编译、静态检查和OpenSpec严格校验。

## Success Criteria

```text
CharacterAnimationPresentationRuntime只编排Frame阶段和typed Result
Pose source物理资源只有CharacterPoseSourceModule一个Owner
Pose Operation只有CharacterPoseProgramRuntime一个执行Owner
Foot/Goal/FBBIK只有CharacterPoseConstraintRuntime一个业务Owner
Physical Bone只有CharacterFinalPosePublication一个写入Owner

Program Image完全不可变且不保存Actor或Frame状态
Actor State只保存跨帧Committed状态
Frame Transaction只保存当前Pending结果并共享唯一lineage

每种节点语义只在一个Node Definition Adapter中声明
Compiler没有中央可变CompilationState
Operation ABI没有万能40字段记录和无意义-1组合
每个Operation Family只读取自己的typed Payload页

Runtime、Preview与Diagnostics消费同一Program Image和完成语义
Diagnostics只读取Committed Result，不读取内部Workspace或重新执行节点
不存在旧Runtime wrapper、旧Program容器、旧Executor构造、旧Handler Registry、旧ABI reader或fallback

既有动画、Foot Placement、Goal、FBBIK和Final Pose业务结果保持迁移前Oracle
```
