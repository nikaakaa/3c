## ADDED Requirements

### Requirement: Agent v14 必须完整读写 Timeline Marker 与 Curve Channel

Agent Snapshot/Patch schema MUST原子提升为`agent-character-controller-synthesis.v14`。Snapshot MUST按Timeline与Track稳定identity输出sync mode、sync group、Finite/Cyclic topology、SyncRole、call site playback mode，以及每个marker的AuthoringId、MarkerId和frame；还 MUST按Curve owner stable identity输出Catalog登记的ChannelId、time domain、value domain、unit、wrap mode与完整Keyframe字段。Patch MUST保留typed configure、ensure、move和delete marker操作，并 MUST使用唯一`configure_timeline_curve_channel`按`OwnerAuthoringId + ChannelId + Full Curve`原子替换typed curve。Lowerer MUST生成immutable command plan，dry-run与apply MUST消费同一plan，handler与Timeline Editor MUST只调用Timeline正式authoring API和Curve Channel MutationAdapter，Validator MUST复用Marker Sync及各curve领域唯一校验服务。Marker MUST保持离散Point Marker语义，不得被编码为Foot Placement、phase或Distance曲线。v13及更早reader、Foot Placement专用curve operation、operation alias、converter和兼容分支 MUST删除。

#### Scenario: Agent导出循环producer

- **WHEN** Agent导出包含WalkLoop AnimationTrack的Full Snapshot
- **THEN** Snapshot MUST包含track stable identity、MarkerGroup模式、Locomotion.Gait、Cyclic topology和全部marker
- **AND** MUST不只输出显示名、asset path或数组index

#### Scenario: Agent导出有限producer

- **WHEN** Agent导出由Once TimelineNode调用的Finite AnimationTrack
- **THEN** Snapshot MUST输出frame 0到DurationFrame的marker序列
- **AND** MUST输出对应call site stable identity与Once模式

#### Scenario: Agent新增重复语义marker

- **WHEN** v14 Patch为Finite track确保第二个LeftPlant marker
- **THEN** dry-run MUST按不同MarkerAuthoringId接受该occurrence
- **AND** apply MUST通过同一plan创建稳定identity
- **AND** 再次导出 MUST按frame稳定显示两个LeftPlant occurrence

#### Scenario: Agent导出typed curve channel

- **WHEN** Agent导出包含MotionCurve Clip的Timeline
- **THEN** Snapshot MUST按owner stable identity输出Position X/Y/Z、Yaw、Weight与Ease channel
- **AND** 每个channel MUST包含稳定ChannelId、time/value domain、unit、wrap mode和完整key字段
- **AND** MUST不把字段名或Inspector path作为channel identity

#### Scenario: Agent修改weighted curve

- **WHEN** v14 Patch通过`configure_timeline_curve_channel`修改一个registered channel
- **THEN** Patch MUST提交完整curve并保留time、value、in/out tangent、in/out weight、WeightedMode与wrap mode
- **AND** handler MUST只调用该descriptor的正式MutationAdapter

#### Scenario: Agent提交未知curve channel

- **WHEN** v14 Patch提交Catalog未登记的ChannelId
- **THEN** lowerer MUST在mutation前拒绝
- **AND** MUST不按字段名、显示名或AnimationCurve类型猜测目标

#### Scenario: 旧schema输入

- **WHEN** Agent收到v13或更早Snapshot、Patch或operation payload
- **THEN** 工具 MUST明确拒绝schema不匹配
- **AND** MUST不升级、转换、猜测字段或调用旧parser

### Requirement: Agent 必须阻止 Marker Sync 数据分裂

Agent MUST只把Marker Sync作者数据写入AnimationTrack，不得写入CharacterAnimationPresentationProfile、TimelineNode、Blackboard、StateMachine edge、ActionProfile、FootPhase资产或generated Projection。Patch target MUST使用stable authoring identity或前序operation output；名称、路径、breadcrumb和列表index MUST不作为fallback。Agent Validator MUST覆盖Unspecified、None残留、marker identity、Finite/Cyclic边界、call site一致性、group directed pair contract与animation output coverage。

#### Scenario: Patch给None track保留marker

- **WHEN** Patch把AnimationTrack配置为None但同时提交SyncGroupId或marker
- **THEN** dry-run MUST拒绝整个事务
- **AND** apply MUST不产生部分资产修改

#### Scenario: Patch使用显示名寻址

- **WHEN** Patch只提供`RunLoop`显示名而没有Timeline/Track stable identity
- **THEN** lowerer MUST报告target identity缺失
- **AND** MUST不搜索第一个同名track

#### Scenario: Patch尝试配置TimelineNode覆盖

- **WHEN** Patch尝试在TimelineNode上保存sync group或marker override
- **THEN** operation catalog MUST将其作为未知或非法字段拒绝
- **AND** MUST不修改Program或PresentationCommand schema补偿

## RENAMED Requirements

- FROM: `### Requirement: Agent Snapshot schema v13 必须输出稳定 authoring identity`
- TO: `### Requirement: Agent Snapshot schema v14 必须输出稳定 authoring identity`

## MODIFIED Requirements

### Requirement: Patch IR 必须是确定性的 graph 编辑指令

系统 MUST使用schema v14 Agent Patch IR作为确定性的graph编辑指令边界。Patch IR MUST使用stable authoring id或前序operation output引用定位编辑目标，只能表达正式authoring操作，例如ensure state machine、ensure state、ensure transition、ensure condition rule、ensure state behavior node、ensure action activation/lifecycle、ensure timeline node、ensure input node、configure animation track marker sync与SyncRole、ensure/move/delete animation sync marker、configure registered timeline curve channel、link flow和link property。资产引用 MUST作为实际消费该资产的ensure command参数，由对应正式Emitter或handler原子解析和写入。Patch IR MUST不直接写Unity YAML、GUID映射集合、runtime状态或旧配置路径，也 MUST不提供独立通用`bind_asset_reference`操作。

#### Scenario: 添加状态

- **WHEN** Patch IR表达添加`Attack1`状态
- **THEN** lowerer MUST生成typed State command
- **AND** handler MUST通过正式节点创建入口创建StateNode
- **AND** Patch IR MUST不包含直接插入节点集合的操作

#### Scenario: 连接Transition

- **WHEN** Patch IR表达`Attack1 -> Attack2`
- **THEN** lowerer MUST生成typed Transition command与typed element reference
- **AND** handler MUST通过正式flow link入口创建Transition edge
- **AND** 合法Transition MUST拥有inline ConditionRuleGraph

#### Scenario: 请求独立资产绑定

- **WHEN** schema v14 Patch包含`bind_asset_reference`
- **THEN** lowerer MUST将其作为未知operation拒绝
- **AND** 系统 MUST不返回成功no-op
- **AND** 资产绑定 MUST改由对应ensure command携带明确引用

### Requirement: Agent Patch 编译必须维护 identity 生命周期

Agent Patch compiler MUST在更新现有元素时保持其authoring identity，在创建新元素时生成新identity，在复制元素时生成新identity。系统 MUST只接受schema v14，不得保留v6至v13兼容解析或按path、display name猜测identity。Typed command lowering MUST在mutation前验证authoring identity格式、operation id唯一性和前序operation reference顺序。

#### Scenario: 更新现有Timeline Clip

- **WHEN** Patch修改一个由authoring identity指定的Clip参数
- **THEN** compiler MUST修改该Clip
- **AND** Clip identity MUST保持

#### Scenario: 平移现有Timeline Clip

- **WHEN** `move_timeline_clip`通过Timeline、Track与Clip identity平移现有Clip
- **THEN** handler MUST保持Clip identity并重算该Track的overlap mix
- **AND** MotionCurveClip MUST同步平移绝对CurveEndFrame

#### Scenario: 配置Timeline Clip self ease

- **WHEN** `configure_timeline_clip_ease`为现有Clip提交显式SelfEaseInFrame与SelfEaseOutFrame
- **THEN** lowerer与handler MUST拒绝负数、超出Duration或与当前overlap冲突的值
- **AND** Snapshot MUST同时输出self、other与effective ease帧数
- **AND** compiler MUST不按path、display name或列表index猜测目标Clip

#### Scenario: 创建新Track

- **WHEN** Patch创建新的Timeline Track
- **THEN** compiler MUST为该Track生成新identity
- **AND** validator MUST拒绝缺失或重复identity

#### Scenario: 创建新Marker occurrence

- **WHEN** Patch在现有AnimationTrack创建新的sync marker
- **THEN** compiler MUST为该occurrence生成新MarkerAuthoringId
- **AND** 相同MarkerId的其它occurrence identity MUST保持

#### Scenario: 替换完整Curve Channel

- **WHEN** Patch按owner stable identity与registered ChannelId提交完整curve
- **THEN** owner、Track与Clip identity MUST保持
- **AND** Keyframe MUST不生成持久AuthoringId
- **AND** preflight MUST拒绝与当前owner revision不一致的陈旧curve写入

#### Scenario: 旧schema输入

- **WHEN** Patch或Snapshot请求使用v6至v13 schema
- **THEN** service MUST返回明确unsupported schema错误
- **AND** MUST不通过converter、index、display name或path fallback apply

### Requirement: Agent Patch Compiler内部必须使用唯一类型化命令计划

系统 MUST将schema v14 `AgentPatchOperation`只作为editor-only JSON边界DTO，并通过唯一operation catalog与AgentPatchCommandLowerer一次降低为immutable typed command plan。Dry-run与apply MUST消费同一typed command plan和同一handler catalog；后续Planner、Handler与Condition builder MUST不再次按原始`op`字符串解释宽DTO。Typed plan MAY保存operation output的kind与owner scope symbol，但 MUST不复制Graph、Node、Edge、Timeline、Marker、Curve或Unity序列化对象形成第二份authoring模型。

#### Scenario: 同一Patch执行dry-run和apply

- **WHEN** AgentPatchAuthoringService收到合法schema v14 Patch并请求apply
- **THEN** service MUST先lower一次typed command plan并完成无副作用preflight
- **AND** apply MUST在资产级事务中消费相同plan
- **AND** MUST不重新解析出另一组operation语义

#### Scenario: 后序operation引用前序输出

- **WHEN** 后序typed command通过operation id引用前序command计划创建的State、Node、Edge或Marker
- **THEN** dry-run MUST通过窄planning symbol验证输出kind与owner scope
- **AND** apply MUST把前序实际创建对象注册到同一operation id
- **AND** 系统 MUST不创建虚拟Graph或Timeline clone来解析该引用

#### Scenario: 未知operation进入lowering

- **WHEN** Patch包含schema v14 catalog未登记的operation
- **THEN** lowerer MUST在任何资产mutation前返回结构化unknown operation错误
- **AND** MUST不选择fallback handler或动态反射实现

### Requirement: Agent Snapshot schema v14 必须输出稳定 authoring identity

Agent Snapshot MUST使用schema v14，并为Graph、Node、Edge、Timeline、Track、Clip、AnimationSyncMarker、Curve owner、Blackboard declaration、CharacterInputProfile request timing与Timeline animation producer输出正式稳定authoring identity。AnimationTrack 与 producer MUST额外输出 SyncRole；Curve channel MUST输出registered ChannelId与完整curve，不为Keyframe创建持久identity。Snapshot path和列表index MAY作为可读定位信息，但 MUST不取代identity。Snapshot MUST不输出Tree animation Driver、ExecutionLineage、LayerPlan、SyncRelation或runtime playback lifecycle。schema v14 Snapshot MUST成为生成v14 Patch的唯一上下文，不提供旧schema镜像输出。

#### Scenario: 导出Full Snapshot

- **WHEN** Agent exporter导出CharacterPipelineDefinition Full Snapshot
- **THEN** 每个Graph、Node、Edge、Timeline、Track、Clip、AnimationSyncMarker和animation producer MUST包含稳定authoring identity
- **AND** snapshot MUST标记schema v14
- **AND** snapshot MUST输出当前source revision所需的逻辑与Timeline作者内容

#### Scenario: Timeline元素重排后导出

- **WHEN** 作者重排Track、Clip或SyncMarker后重新导出Snapshot
- **THEN** 对应元素和producer identity MUST保持
- **AND** index与可读path MAY更新
