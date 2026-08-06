# Change: 打通可组合 Pose Graph 编辑器工作流

## Why

当前项目已经拥有共享 Graph Authoring 画布、PoseStateMachine、Transition Blend、Pose Source binding、Timeline Marker/Curve 编辑模块、Foot Analysis、Foot Placement Planner、Native TwoBoneIK 与 Pose Preview 基础，但这些能力尚未形成一条完整作者链：

- Pose Graph 的所有姿势端口仍是无空间语义的单一`Pose`，作者无法判断一个骨骼控制节点应在Local Space还是Component Space工作，也没有显式空间转换节点。
- `FootPlacement`虽然显示在Pose Graph中，Native evaluator却只复制输入到输出；真正的Planner、Physics query和Final IK变换写入发生在图外`PresentPosePostProcess`。因此下游节点、Pose Watch、Preview和编译拓扑看不到真实Foot Placement结果。
- Pose Plan只有固定的`native pose -> world-aware postprocess -> final publication`单向阶段，无法在world-aware节点之后继续连接普通Pose节点。
- 腿部语义骨骼由Prefab上的`CharacterFootPlacementRig`保存，Pose buffer使用另一份Rig Definition；校准、分析、编译与Runtime没有共同的唯一Rig身份。
- 持续Locomotion Pose Source的Marker和Foot Placement Weight虽然归Profile binding所有，但实际入口仍是普通Inspector文本字段和`CurveField`，没有Timeline现有的时间尺、Marker lane、Curve key、切线、多选、框选、Undo与分析候选应用工作流。
- Pose Graph底部Preview显示Action producer并调用Timeline Preview，而不是编辑Fact并执行Pose Graph Preview；现有PoseGraph Preview到world-aware阶段只报告Unavailable，无法在精确Host上下文中观察Foot Placement真实输出。
- Agent Document current spec仍把Marker Sync可写数据限定在AnimationTrack，与持续Pose Source binding已经成为正式owner的现行规格冲突。
- 活跃`repair-foot-placement-calibration-and-limb-solving`明确保留Final IK适配器和图外后处理位置；这与本次“真实可组合Pose节点”的目标冲突。

用户要求的是完整编辑器工作流，不以迁移成本为理由缩减功能。因此本change将Pose空间、world-aware分段执行、Foot Placement真实Pose输出、Rig/Calibration、Pose Source时间编辑、Preview/Pose Watch/Live Debug、Document与正式发布收敛为唯一链路，而不是继续修补当前图外后处理。

## What Changes

- 为Pose Graph安装正式Pose空间类型：
  - 以`Local Pose`与`Component Pose`替换无空间语义的通用Pose端口。
  - 新增显式`Local To Component`与`Component To Local`节点；作者图保存转换节点，Compiler不得静默插入或猜测空间。
  - Sequence、Blend、StateMachine、Slot、Inertialization、Layered/Additive等节点在Local Pose工作；Modify Bone、TwoBoneIK与Foot Placement在Component Pose工作；Output Pose只接收Local Pose。
- 把固定Pose Plan升级为有序的拓扑分段执行计划：
  - Compiler按依赖、Pose空间与执行领域把同一DAG切成Fact/source、pure-pose、world-aware与final writer stage。
  - world-aware stage完成后 MUST能继续执行后续pure-pose stage；每个source仍只采样一次，每帧仍只有一次PlayableGraph Evaluate和一次最终骨骼写入。
  - 删除`FootPlacement`的透传operation和图外`PresentPosePostProcess`旁路。
- 将Foot Placement变成真实Component Pose Skeletal Control：
  - 节点读取上游Component Pose、Body表现帧、Profile、Calibration和精确PhysicsScene，输出已经修正pelvis与双腿的Component Pose。
  - Planner继续只负责接触、预测、support、lock/replant、目标脚姿与pelvis计划；可复用解析式腿部Pose solver负责把计划应用到Pose buffer。
  - 删除`ICharacterFootPlacementSolver`、Final IK适配器、`CharacterFootPlacementComposition`与Foot Placement专用Transform写入路径。
  - 保留Planner与solver内部模块边界，但不向作者暴露没有组合价值的Plan端口。
- 将Animation Rig破坏性升级为唯一Rig v3：
  - Rig Definition正式保存pelvis及左右`hip -> knee -> ankle -> toe`语义Physical Bone chain，替换旧Left/Right Foot字段和Prefab上的第二份Foot Placement rig。
  - Sampling Rig校准工具同时编辑Rig Mapping与Foot Calibration，并验证骨骼身份、父子链、腿长、sole frame和bend reference。
  - Foot Analysis Source显式引用同一个Rig Definition、Sampling Rig与Calibration；Analyzer、Projection、Preview和Runtime必须核对同一identity与revision。
- 建立正式Pose Source Editor：
  - 由Presentation Profile进入持续Pose Source的专用时间编辑页，不创建Locomotion Timeline。
  - Sequence source使用正式时间尺、Sync Marker lane、typed Curve lane、全量key/tangent编辑、多选、框选、Undo、Foot Analysis候选与Preview。
  - BlendSpace与Motion Matching仍进入各自正式编辑器；它们复用Marker/Curve/Analysis交互模块，不复制owner或生成Timeline。
  - 补齐Sequence Pose Source缺失的`SyncRole`，并让人工UI、Document、Validator、Compiler共用同一binding schema。
- 打通Preview、Pose Watch与Live Debug：
  - Pose Graph Preview改为Fact Preview，编辑Grounded、Speed、Acceleration、Vertical Speed、Movement/Desired Direction、Facing Error、Motion Phase与typed parameter page。
  - 精确CharacterPipelineHost提供Definition、Rig binding、Body fixture、world binding与实际PhysicsScene；上下文完整时Preview执行同一staged Pose Plan及Foot Placement，不完整时在第一个world-aware节点报告typed Unavailable，不创建假地面。
  - Pose Watch发布每个节点完成后的真实Pose、Pose空间与contribution；Foot Placement watch必须看到已求解结果。
  - Live Debug显示stage、space、input/output completion、world capability、Planner与solver结果，不读取Final IK私有状态。
- 同步唯一作者与Agent链：
  - Capability Catalog增加Pose空间、execution domain与stage barrier元数据，驱动端口、菜单、Details、Document、Validator和Compiler。
  - Document v3、codec、exporter、reconciler、Mutation与validator完整读写转换节点、Rig v3引用、Pose Source marker/curve/sync字段；Rig正文、Calibration正文、generated artifact与Projection继续只读。
  - 修正Agent Marker Sync规格：有限Action由Timeline AnimationTrack拥有，持续Pose Source由Profile binding拥有。
  - 保持现有五个Document生命周期工具，不增加Pose节点级工具或第二写入口。
- 原子迁移并清理：
  - 迁移Corin Rig、Profile、Pose Sources与Pose Graph，显式接入Local/Component转换和Foot Placement真实节点。
  - 删除旧通用Pose端口、Rig v2、Foot Placement rig/composition、Final IK solver、图外后处理、旧Pose Source普通Inspector与错误的Action型Pose Preview。
  - 校准合法后，使用唯一Build链重建Foot Analysis artifacts、Float32/Fixed Projection与Native Pose Program；不自动Build、不保留reader、fallback、双写或兼容开关。

## Scope

### In Scope

- Pose空间类型、转换节点、Capability、typed port、Validator、Compiler IR与staged executor。
- Foot Placement Planner到Pose buffer solver的完整world-aware节点链。
- Rig v3语义腿链、Sampling Rig映射/校准工具、Foot Analysis Source身份闭合。
- Pose Source时间编辑器及Marker、Curve、Analysis、Preview模块复用。
- Pose Graph Fact Preview、Pose Watch、Live Debug与正式Runtime同计划执行。
- Document v3、Exporter、Reconciler、Mutation、Validator和只读context同步。
- Corin资产迁移、旧路径删除与显式生成产物发布。
- current specs、活跃Foot Placement change、`openspec/project.md`与authoring contract对账。

### Out of Scope

- 把持续Locomotion包装成Timeline或Montage。
- 把Gameplay StateMachine、Action arbitration、MotionWarp或Simulation逻辑搬入Pose Graph。
- 引入UE运行时、Control Rig或Motion Matching替代实现。
- 自动Compile、自动Build、selection/file watcher触发重操作。
- 新增节点级MCP工具、任意SerializedProperty入口或第二Graph编辑器。
- 新增测试；用户将自行做端到端验证。

## Impact

### Specs

- 修改`character-presentation-pose-graph`。
- 修改`character-foot-placement-presentation`。
- 修改`character-animation-presentation-authoring`。
- 修改`character-animation-foot-analysis-artifact`。
- 修改`graph-authoring-domain-framework`。
- 修改`btsmtl-timeline-editor-preview`。
- 修改`btsmtl-agent-authoring-document-sync`。
- 修改`agent-character-controller-synthesis`。
- 修改`character-pipeline-runtime`。

### Code

- Pose authoring contracts、Capability、port projection、Details、Mutation和Graph compiler handlers。
- Pose IR、workspace、Native evaluator、stage executor、source capture和final writer。
- Foot Placement planner/runtime、解析式腿部solver、Rig binding与world query binding。
- Rig Definition、Rig compiler、Sampling Rig校准窗口、Foot Analysis Source/Analyzer/Artifact/Projection。
- Presentation Profile与Pose Source Editor、共享Timeline Field marker/curve/analysis模块。
- PoseGraph Preview panel、AnimationPreviewRuntime、Pose Watch、Live Debug与Trace。
- Agent Document models、codec、exporter、reconciler、validator和capability context。
- Gameplay Lab prefab、Corin Profile/Pose Graph/Rig/Analysis Source与generated Presentation产物。

### Active Change关系

- `repair-foot-placement-calibration-and-limb-solving`已经完成的Calibration v2、鞋底语义与膝盖稳定工作作为本change的输入；其“保留Final IK”和“保持FootPlacement图外后处理位置”的设计边界被本change取代。
- 该change尚未完成的Corin校准修正仍是Rig v3迁移前置条件；其Float32/Fixed Projection与Native Pose Program发布任务转由本change在新schema与新executor安装后统一完成，禁止先向旧Final IK路径发布。
- `refactor-pose-graph-to-btsmtl-authoring-domain`已经提供共享Canvas、typed payload、Capability、Document v3与模块化Compiler基础；本change扩展正式Pose空间与execution domain，不恢复Pose专用GraphView。
- `refactor-pose-transition-blend-authoring`的Transition Blend Logic、Curve与Blend Profile保持不变；本change只让其Local Pose结果能与Component Pose控制节点正确组合。
- `refactor-animation-control-boundaries`的持续PoseState与有限Action Timeline边界保持不变；Pose Source Editor不是Timeline资产。

## Breaking Changes

- 通用`Pose`端口被`Local Pose`与`Component Pose`替换，旧edge不再读取。
- Rig v2被Rig v3替换，旧Left/Right Foot字段和Prefab Foot Placement rig不再读取。
- `FootPlacement`不再是透传operation，图外`PresentPosePostProcess`与Final IK适配器被删除。
- `CharacterFootPlacementComposition`、`CharacterFootPlacementRig`与`ICharacterFootPlacementSolver`被删除。
- 持续Sequence Pose Source不再使用普通Inspector `CurveField`和Marker文本字段，统一进入Pose Source Editor。
- Pose Graph Preview不再选择Action producer，统一使用Fact/parameter fixture。
- 旧Projection、Foot Analysis artifact和Native Pose Program必须按新Rig、port与stage schema重建；不提供兼容reader。

## Current Spec Comparison

- current `character-presentation-pose-graph`要求图唯一表达FootPlacement，却又把Pose Plan固定为native后唯一world-aware尾阶段；实际代码中的FootPlacement operation只透传Pose，图外再写Transform。本change把“图是唯一拓扑”落实为可观察的真实节点输出，并允许world-aware节点后继续组合。
- current `character-presentation-pose-graph`只定义通用Pose port和Rig v2，无法表达Local/Component空间与组件空间骨骼控制。本change破坏性升级端口与Rig schema。
- current `character-foot-placement-presentation`规定复用`ICharacterFootPlacementSolver`并以Final IK应用计划；项目搜索确认Final IK除Foot Placement和Gameplay Lab校验外没有其它消费者。本change删除该依赖，由Pose buffer解析式solver成为唯一应用链。
- current `character-animation-presentation-authoring`已经规定持续Pose Source marker/curve归Profile binding，但没有要求完整时间编辑表面；实际Inspector无法完成精确key/tangent与marker编辑。本change补齐唯一Pose Source Editor，而不改变owner。
- current `btsmtl-timeline-editor-preview`已经拥有成熟Marker/Curve交互模块，但其抽象只在Timeline Field内部使用。本change把交互、几何与渲染模块提升为source-time authoring公共模块，数据仍写回各自owner。
- current `btsmtl-agent-authoring-document-sync`允许Presentation binding可编辑；current `agent-character-controller-synthesis`却把Marker Sync可写数据限定在AnimationTrack。两者与current Presentation authoring规格矛盾，本change修正Agent规则为Action/Pose Source双owner且互不复制。
- current `character-animation-foot-analysis-artifact`验证Sampling Rig与Calibration，但没有把Rig Definition纳入artifact identity。本change加入Rig v3 identity，避免分析骨骼与Runtime骨骼来自两份映射。
- active `repair-foot-placement-calibration-and-limb-solving`的保留Final IK/节点位置约束与本change目标直接冲突；本change明确取代该边界，不建立并行路径。
- current `graph-authoring-domain-framework`要求Capability是UI与Document唯一语义目录，但尚未声明Pose空间和execution domain；本change扩展同一目录，不新建Pose专用catalog。

## Success Criteria

- 作者能在同一Pose Graph中连接Local Pose节点、显式空间转换、多个Component Pose控制节点、反向转换与Output Pose；非法空间连接在连线时和Build时都被拒绝。
- Foot Placement节点的输出workspace包含已修改pelvis和双腿Pose，下游节点、Pose Watch、Preview、Live Debug与final writer看到同一结果。
- world-aware节点后可继续连接Component Pose或转换回Local Pose的节点，不存在固定尾后处理限制。
- 每个source每帧只采样一次、PlayableGraph只Evaluate一次、最终Physical Transform只写一次。
- Foot Placement不再依赖RootMotion.FinalIK，项目内不存在旧solver、composition、rig和图外postprocess入口。
- Rig Mapping、Calibration、Foot Analysis、Projection、Preview和Runtime核对同一Rig v3 identity/revision。
- 持续Sequence Pose Source拥有可用的Marker与Curve时间编辑器，支持精确key/tangent、多选、框选、Undo和Foot Analysis候选应用，且不创建Timeline。
- Pose Graph Preview以Fact和typed parameter驱动；精确Host世界上下文完整时可预览真实Foot Placement，不完整时明确Unavailable且不造假。
- Capability、人工UI、Document、Mutation、Validator与Compiler共享相同Pose空间、execution domain和binding字段定义。
- Corin只保留一条新链路，旧端口、Rig v2、Final IK与旧generated产物全部删除。
- 打开、选择、编辑、Preview、Document保存或文件变化均不自动Compile/Build。
