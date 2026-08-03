# agent-character-controller-synthesis Specification

## Purpose
定义Agent在编辑器内通过Document v3目录包、canonical Snapshot、Mutation Compiler、Validator与Report生成并修复正式BTSMTL角色控制器资产的唯一链路。
## Requirements
### Requirement: Agent必须保持Generated Foot Analysis只读

Agent Character Document package MUST只把正式Graph、StateMachine、Timeline、Marker、registered editable Curve Channel和Profile identity放入相应可写或只读分片。Animation Clip的Foot Placement Weight MUST继续作为完整可编辑curve进入`curves.json`；Projection生成的sole speed、height、plant confidence、next landing confidence、delay与offset MUST不进入editable分片或Mutation Plan。Agent MUST不复制generated payload，也 MUST不创建Foot Analysis mutation。

#### Scenario: Agent尝试写Generated channel

- **WHEN** package editable分片提交未登记的LeftPlant、RightPlant或Landing ChannelId
- **THEN** Reconciler MUST按未知Curve Channel拒绝整个package
- **AND** MUST不修改Timeline、Projection或Analysis Source

### Requirement: Agent Validator必须透传正式Foot Analysis编译诊断

Agent Validator MUST透传正式Artifact Builder、Artifact Store、Projection binding与Build Transaction诊断，区分Missing、Stale、Corrupt、Source/Rig/Calibration不匹配和stable clip binding缺失。Agent Document与Mutation链 MUST不采样AnimationClip、不写artifact、不输出feature payload，也不得新增Foot Analysis rebuild或generated curve mutation。

#### Scenario: Document修改Foot Placement Weight

- **WHEN** 合法Document只修改现有Foot Placement Weight并显式apply
- **THEN** 正式Definition Build MUST在apply事务中重新校验所需artifact并发布Projection
- **AND** Agent Document链 MUST不直接读取或修改artifact文件

### Requirement: Agent 生成链路必须是 editor-only authoring 编译链路

系统 MUST将Agent生成角色控制器实现为editor-only Document package authoring链路。Agent Document package、canonical Snapshot、Mutation Plan与Report MUST只服务编辑期checkout、修改、修复和评估。运行时 MUST只执行由正式BTSMTL资产显式编译并发布的Character Program与Presentation Projection，MUST不读取Document package、调用LLM或解释authoring Graph。

#### Scenario: 运行时加载角色

- **WHEN** CharacterPipelineHost向Session Host注册角色
- **THEN** runtime MUST只读取匹配身份的已发布Program、Projection和Session composition
- **AND** MUST不读取Agent Document package、Mutation Plan或LLM输出文件

### Requirement: Agent Snapshot 必须是只读投影

系统 MUST能从当前`CharacterPipelineDefinition`和BTSMTL树生成只读canonical Snapshot，作为Document package checkout和Reconciler比较的唯一当前状态投影。Snapshot MUST包含Agent正式可写结构、stable identity、ownership与可引用catalog。Pose Graph、PoseStateMachine、Graph-owned typed Source Slot、Profile-owned typed Source Binding、AnimationSlot、有限Action channel binding、BlendSpace/MM资源绑定和node-local Policy MUST进入Document v3的editable Presentation目标状态；Rig/Virtual Bone资源正文、Body Motion、Foot Analysis、runtime state和generated product MUST只进入紧凑只读context或完全省略。Pose Player MUST通过结构化Unity对象引用指向精确Source Slot，Binding MUST通过结构化对象引用指向Slot与实际资源；Document的`assetPath + assetGuid + signed non-zero localFileId`只承担持久同步身份。Projection编译时 MUST把可达Slot与Binding降低为连续dense source index，runtime sample与Player必须比较该Projection-local index、NodeId、generation和lease；由Compiler从NodeId生成的typed provider identity MAY额外用于单帧路由，但 MUST不进入Document editable或资源查找。Motion Matching MUST只作为PoseState provider，不得投影Gameplay channel、`ProgramProducerId`、`PlaybackId`或`ProgramProducerIndex`。`ActionPlaybackInput`与`AnimationSlot` MUST单独标识`ActionAnimationChannel` owner，且AnimationSlot MUST是每个有限Action channel的唯一consumer。有限Action producer天生只能是Timeline，Snapshot MUST不输出或接收`sourceKind`可选字段。系统 MUST不包含旧`MotionMatchingSelectionInput`、`AnimationSelection` port、Pose Graph `MarkerSync`、Timeline locomotion producer、旧BlendLibrary、PoseSlot、Layer或TransitionLibrary。Snapshot MUST不成为正式配置来源，不保存runtime临时状态，不暴露Unity YAML，也不因导出触发build。

#### Scenario: checkout生成Character Document package

- **WHEN** Agent对已有Character root显式checkout
- **THEN** exporter MUST从当前树建立canonical Snapshot并写出v3目录包
- **AND** snapshot/export MUST不修改Graph或触发Program build

#### Scenario: Presentation能力进入正式Document

- **WHEN** 某项Presentation能力已经安装进唯一Capability catalog
- **THEN** Document editable Presentation MUST复用该Capability、stable identity与typed payload
- **AND** MUST通过唯一Presentation Mutation处理，不得增加能力私有schema或第二mutation入口

### Requirement: Compiler 必须调用 BTSMTL 正式 authoring API

系统 MUST通过Document Reconciler和Mutation Compiler把目标package应用到BTSMTL graph。Compiler与handler MUST继续调用`BaseGraph.CreateNode`、`BaseGraph.Link`、`BaseGraph.UnLink`、`BaseGraph.LinkProperty`、正式Property Edge断开、节点配置入口和Timeline ownership API，尊重`CanCreateNodeType`、逻辑port到PropertyPort PortId映射、Graph kind与inline/shared ownership。系统 MUST不维护第二套节点、边、端口、Timeline或Workbench数据。

#### Scenario: Document要求非法节点位置

- **WHEN** Document把TimelineNode放入StateMachineGraph
- **THEN** Reconciler或preflight MUST拒绝并输出Document entity路径
- **AND** 正式Graph MUST保持不变

### Requirement: Node Emitter 必须使用白名单

系统 MUST使用唯一authoring capability catalog限定Document可表达并可创建的Node与Graph。每个Node kind MUST声明允许的Graph kind、typed properties、正式默认值、逻辑Flow/Property ports、资产引用和create/configure/delete lowering；每个Graph kind MUST声明owner slot与系统anchor。Exporter、strict parser、Reconciler、handler和Validator MUST复用该catalog。未知kind、field、port、anchor或未登记参数 MUST被拒绝，MUST不降级为placeholder、fallback节点、C# type name或字符串脚本。

#### Scenario: Document创建未登记节点

- **WHEN** editable Graph包含未登记Node kind
- **THEN** Reconciler MUST报告未知kind及package entity路径
- **AND** MUST不产生Mutation

### Requirement: 资产解析必须来自当前角色 authoring context

系统 MUST通过当前`CharacterPipelineDefinition`、canonical Snapshot与Document只读catalog解析Input、ActionProfile、Timeline和RootTree引用。Resolver MUST使用稳定identity或明确正式引用，MUST不扫描场景、目录、同名asset、旧配置或全局单例作为fallback。

#### Scenario: Document引用ActionProfile

- **WHEN** Document中的Action activation引用`Attack.Light.01`
- **THEN** Resolver MUST只从当前Definition正式ActionProfile catalog解析
- **AND** 找不到时 MUST报错且不搜索替代资产

### Requirement: Validator 必须检查 Agent 生成 graph 的 BTSMTL 语义

系统 MUST提供 Agent graph validator，在 apply 前后检查 Agent 生成结构。Validator MUST检查 graph 类型规则、ConditionRuleGraph 纯条件语义、TimelineNode 位置、Timeline inline/shared ownership、TimelineData serialized owner/path、TreeClip graph ownership、ActionProfile 引用、Input request 引用、Action Context 链路和 AnyState 条件。Validator MUST输出机器可读错误路径和建议修复。

#### Scenario: TimelineNode 位于错误图层

- **WHEN** 生成结果中 TimelineNode 位于 StateMachineGraph
- **THEN** validator MUST报告 graph kind 错误
- **AND** report MUST指出 TimelineNode 应位于 StateNode 的状态行为 Graph

#### Scenario: TimelineNode 存在双真相

- **WHEN** TimelineNode 同时保存 inline TimelineData 和 shared TimelineAsset
- **THEN** validator MUST报告 ownership 冲突
- **AND** validator MUST NOT按优先级静默选择其中一份

#### Scenario: inline Timeline owner path 断裂

- **WHEN** TimelineNode inline TimelineData 无法绑定到 RootTree serialized owner/path
- **THEN** validator MUST报告稳定 node path 与断裂字段
- **AND** 系统 MUST NOT把数据保存到临时 Timeline asset

#### Scenario: Action Context 断链

- **WHEN** 攻击状态播放带 projected Window TreeClip 的 Timeline 但没有 Action Context 来源
- **THEN** validator MUST报告 Action Context 缺失
- **AND** report MUST建议在状态进入或动作开始处创建 action activation 并把 context 传给 TimelineNode

#### Scenario: TreeClip owner path 断裂

- **WHEN** resolved TimelineData 中的 TreeClip inline TimelineRunningTree 缺少稳定 owner/path
- **THEN** validator MUST报告 TimelineNode、track、clip 和 graph identity
- **AND** validator MUST拒绝该 authoring 结果

### Requirement: Compile Report 必须支持 Agent 自修复

系统 MUST输出`AgentCompileReport`，包含Document package schema、sync、引用、reconcile、preflight、apply、语义错误、planned/applied diff、metrics和建议修复。Report MUST使用机器可读文件路径与entity path定位Graph、Node、Edge、Timeline或asset，并 MUST返回同步状态与整包document hash。Report MUST不再要求AI生成下一轮Patch operation。

#### Scenario: Document reconcile失败

- **WHEN** Reconciler拒绝一个Timeline entity
- **THEN** report MUST标出package文件与entity路径、错误code、原因和建议
- **AND** AI MUST能直接修改同一文件后重新dry-run

### Requirement: Agent 评估必须区分结构、语义和业务覆盖

系统 MUST统计Document schema合法率、reconcile成功率、preflight成功率、语义合法率、引用解析成功率、修复轮数、diff size、非目标人工内容保留率和业务覆盖度。受控样例 MUST检查Document目标结构及其Mutation Plan，MUST不执行Agent JSON或运行时LLM。

#### Scenario: 评估二连击Document

- **WHEN** 样例要求AI生成二连击
- **THEN** evaluator MUST检查Document含外层Attack、内层状态机、两个攻击状态、Action、Timeline与条件
- **AND** MUST检查Reconciler和Validator接受最终结构

### Requirement: 正式资产必须仍由人类可微调

系统 MUST保持apply后的正式结果为普通BTSMTL Graph、Timeline、ActionProfile及其正式Definition引用。作者可以继续使用Graph Editor、Timeline Editor和各正式Profile Inspector。人工编辑只使live authoring revision变化；系统 MUST不自动刷新Document package或build。AI再次编辑时 MUST显式checkout或处理Conflict，MUST不覆盖未合并的人工变化。

#### Scenario: 作者微调后AI继续编辑

- **WHEN** 作者在上次Clean后修改Graph且AI未修改Document
- **THEN** 状态 MUST为TreeDirty
- **AND** 显式checkout MUST从当前正式树刷新Document
- **AND** checkout MUST从当前正式Presentation刷新editable目标状态且不修改Unity资产

### Requirement: Agent Snapshot 与 Validator 必须递归理解嵌套 StateMachine

Agent Snapshot MUST递归输出Gameplay RootTree、Runnable、inline/shared Graph、BTSMTL nested StateMachine、logical transition、Action activation、有限Action Timeline与稳定producer identity。Presentation editable section MUST递归输出Pose Graph、PoseStateMachine/State/Transition、Pose source binding、AnimationSlot、node-local Policy与Action channel binding；Rig identity及generated产品只作为只读context。Validator MUST区分Gameplay StateMachine与PoseStateMachine，MUST不把持续Pose source伪装为Timeline producer，也不得接受旧Patch写入路径。

#### Scenario: Corin Snapshot

- **WHEN** 导出迁移后的Corin compact Snapshot
- **THEN** Gameplay section MUST显示None/Attack/Dodge及其Action Timeline
- **AND** Presentation editable section MUST显示Locomotion PoseStateMachine、Pose source、FullBodyAction Slot与Policy，context MUST显示Rig identity
- **AND** BTSMTL Graph Node/Edge MUST不输出Bone Mask或Pose composition字段

#### Scenario: Timeline channel identity断裂

- **WHEN** 可达AnimationTrack缺失或引用未知AnimationChannelId
- **THEN** Validator MUST输出对应Graph/Timeline source错误
- **AND** Compiler transaction MUST回滚

### Requirement: Agent Compiler模块必须按authoring职责聚合

Document Mutation Compiler MUST保持唯一Compiler facade，但单次Definition、Snapshot、Resolver、Graph Index、planning symbol、diff与touched owner MUST由每次调用独占的Mutation session拥有。StateMachine、StateBehavior、Node/Asset、GraphLink、Timeline与ConditionRule MUST由按共享authoring不变量聚合的handler处理。Compiler MUST不拥有Undo、dirty、rollback或SaveAssets；这些职责 MUST继续只属于统一Document application service。

#### Scenario: 连续reconcile两个Definition

- **WHEN** 同一Compiler实例连续dry-run两个不同Definition
- **THEN** 第二次调用 MUST创建全新Mutation session
- **AND** MUST不读取第一次调用的Resolver、Index、planning symbol或touched owner

### Requirement: 通用Agent Validator与业务样例覆盖必须分层

`AgentGraphValidator` MUST只检查对任意Definition成立的Graph kind、Condition纯度、Timeline ownership、serialized owner、TreeClip、Action Context、Input/ActionProfile引用、identity和正式Compiler语义。具体样例结构覆盖 MUST由Document Synthesis Evaluator检查Document与Mutation Plan，MUST不进入普通`validate`。

#### Scenario: 验证非Corin角色

- **WHEN** 作者验证使用不同状态名和连招层数的合法角色
- **THEN**通用Validator MUST只按正式authoring语义判断
- **AND** MUST不要求Corin状态名、连招数量或Macro名称

### Requirement: Agent 不得形成第二个动画表现 authoring 入口

Document package editable分片与Mutation Compiler MUST只编辑正式Graph、StateMachine、Timeline、Blackboard及唯一Capability catalog已安装的业务能力。CharacterAnimationPresentationProfile中的Pose Graph、PoseStateMachine、Blend、provider/source binding、AnimationSlot与Policy MUST通过共享typed payload和唯一Presentation Mutation进入editable Presentation；Rig资源正文和generated Projection MUST保持只读context。人工共享作者表面与Document apply MUST调用同一Mutation、Validator和事务服务，不得形成第二个动画表现authoring入口。只有带Action Context的有限Timeline AnimationTrack允许进入editable Action producer链；Timeline AnimationTrack Marker Sync继续由Timeline entity唯一拥有，不得恢复已删除的Pose Graph MarkerSync节点或摘要。未知Presentation变化 MUST被拒绝，MUST不转换成默认配置。

#### Scenario: Document配置Pose Graph

- **WHEN** AI修改Document v3中Capability已登记的Pose Graph业务字段
- **THEN** Reconciler MUST生成与人工编辑相同的typed Presentation Mutation
- **AND** 未登记字段、Rig正文、generated payload或能力私有mutation MUST被拒绝

### Requirement: Agent Snapshot 必须完整投影 MotionWarp authoring

Agent Snapshot MUST输出MotionWarp Track/Clip subtype、stable identity、SourceMotionClipId、resolved source path、position/rotation mode、target offset、weight、clamp和两条canonical progress curve。Snapshot MUST只投影正式Timeline资产，不输出Preview target或runtime mutable state。

#### Scenario: 导出带 MotionWarp 的 Timeline

- **WHEN** Agent导出包含MotionWarp的Character Definition
- **THEN** Snapshot MUST能唯一定位Warp与source MotionCurve
- **AND** Track重排后两者identity关系 MUST保持不变

### Requirement: Agent Validator 必须复用 MotionWarp 正式校验

Agent Validator MUST检查source identity、Timeline owner、窗口、Action channel、Override语义、mode、offset、weight、clamp、progress curve、Action Context与ActionTargetRequirement，并与Inspector和Semantic Compiler复用同一校验服务。任何错误 MUST定位到Document entity、Graph、Timeline、Track、Clip、ActionProfile与相关source。

#### Scenario: Document为无目标动作配置Warp

- **WHEN** Document为`ActionTargetRequirement.None`动作增加MotionWarp
- **THEN** dry-run MUST失败并报告目标要求矛盾
- **AND** apply MUST不修改任何资产

### Requirement: Agent Snapshot必须只读投影Body Motion Profile

Character Document context MUST从显式Definition只读输出Body Motion Profile stable identity、content revision、GravityAcceleration、MaximumFallSpeed、semantic version、required AirborneVerticalMotion capability与Compiler状态。Editable分片与Mutation Plan MUST不提供Profile修改、任意SerializedProperty或第二Profile写入口。

#### Scenario: AI尝试修改Body Motion参数

- **WHEN** AI改变Document context中的GravityAcceleration
- **THEN** Reconciler MUST报告readonly context修改
- **AND** MUST不产生Profile Mutation

### Requirement: Agent 必须阻止 Marker Sync 数据分裂

Agent Document MUST只把Marker Sync可写数据放在AnimationTrack entity，不得放入Presentation、TimelineNode override、Blackboard、StateMachine edge、ActionProfile、FootPhase或generated Projection。Target MUST使用stable identity或Document local identity；名称、breadcrumb和index不得成为fallback。Validator MUST继续覆盖None残留、identity、Finite/Cyclic边界、call site、directed pair和animation output coverage。

#### Scenario: None Track保留Marker

- **WHEN** Document把Track设为None但仍保留group或Marker
- **THEN** dry-run MUST拒绝整个Document
- **AND** apply MUST不产生部分资产修改

### Requirement: Agent Authoring Document必须是声明式控制器结构

系统 MUST使用`btsmtl-agent-authoring-document.v3`目录包作为CharacterController与AIController唯一AI-facing编辑合同，并通过显式domain区分根。Editable分片 MUST按Graph、StateMachine、State、Transition、Condition、Action、Timeline、Blackboard、Presentation、Perception和Character input/request intent binding描述目标结构。Graph MUST使用稳定kind、typed properties、逻辑port、系统anchor、正式owner和Flow/Property Edge完整目标集合，MUST不暴露C# type name、重复port metadata、`operations[]`、内部handler、创建顺序、前序operation output、Unity YAML或任意SerializedProperty写入。Document Reconciler MUST只把正式支持的整包变化降低为内部typed Mutation。

#### Scenario: 添加状态和Transition

- **WHEN** package增加带local identity的Attack状态、owner body Graph及其到现有状态的Transition
- **THEN** Reconciler MUST生成有序State、Transition和Condition typed Mutation
- **AND** AI MUST不填写`ensure_state`、`ensure_transition`、`link_flow`或调用节点级工具

#### Scenario: 请求未知结构字段

- **WHEN** Document包含schema未登记字段或节点能力
- **THEN** strict parser或Reconciler MUST在mutation前拒绝
- **AND** MUST不创建placeholder或动态反射操作

### Requirement: Agent Document reconcile必须维护 identity 生命周期

Document Reconciler与Mutation Compiler MUST在更新现有entity时保持stable authoring identity，在`local:<meaningful-id>`创建时生成新identity，在复制entity时生成新identity。系统 MUST只接受Document package v3，不得保留v1/v2、v16/v17 Patch parser或按path、display name、Actor名称、Tag和列表index猜identity。Node kind与Graph kind MUST不可原地改变。Apply成功后的整包反向导出 MUST把新local identity替换为正式stable identity。

#### Scenario: 更新现有Timeline Clip

- **WHEN** Document通过stable identity修改一个现有Clip
- **THEN** Mutation Compiler MUST修改同一Clip并保持identity
- **AND** 最终canonical Document MUST继续输出该identity

#### Scenario: 创建新Marker occurrence

- **WHEN** Document使用local identity增加一个Marker occurrence
- **THEN** Reconciler MUST为其建立planning symbol
- **AND** apply后反向导出 MUST写入新MarkerAuthoringId

### Requirement: Agent Document必须降低为唯一类型化Mutation计划

系统 MUST让strict multi-file parser与`AgentDocumentReconciler`从整个package一次生成immutable typed`AgentMutationPlan`。CharacterController与AIController MUST复用同一planning symbol、preflight、资产事务和handler catalog基础；domain handler只消费正式authoring API。Dry-run与apply MUST基于同一整包document hash生成等价plan，后续handler不得读取原始JSON discriminator或建立AI专用compiler和第二事务。

#### Scenario: 同一Document执行dry-run和apply

- **WHEN** 合法Document先dry-run再以同一document hash申请apply
- **THEN** 两次lowering MUST产生相同plan hash和planned diff
- **AND** apply MUST在资产事务中消费等价typed plan

### Requirement: Agent Document必须输出稳定 authoring identity

Document package v3与其canonical Snapshot MUST按显式domain输出Graph、Node、Flow Edge、Property Edge、Timeline、Track、Clip、Marker、Curve owner、Blackboard declaration、Presentation owner、Input request timing和domain正式producer的stable identity。物理文件路径与列表index MAY用于阅读但不得取代identity。Document MUST不输出runtime mutable state、C# type name或重复port metadata。Document checkout MUST成为AI编辑的唯一领域上下文，不提供v1/v2、v16/v17 Patch或Snapshot镜像。

#### Scenario: Timeline元素重排后checkout

- **WHEN** 作者重排Track、Clip或Marker后显式checkout
- **THEN** 对应stable identity MUST保持
- **AND** 可读顺序和path MAY更新

### Requirement: Agent Document必须通过类型化Mutation修改 MotionWarp

Character Document package MUST完整表达MotionWarp Track/Clip、source stable identity、typed参数和删除后的目标集合。Reconciler MUST降低为唯一typed Mutation Plan；handler MUST调用Timeline正式authoring API，不得直接编辑YAML、按名称猜source或创建第二套MotionWarp配置。

#### Scenario: Document创建目标攻击Warp

- **WHEN** Document新增Warp并引用现有或同Document新建的MotionCurveClip identity
- **THEN** Reconciler MUST解析为stable或local planning reference
- **AND** handler MUST创建合法MotionWarpClip并保持source关系

### Requirement: Agent Document必须完整表达 Action target authoring

Character Document package MUST表达`ActionTargetSnapshot` Blackboard declaration、InputDerived InputValueId、准入与activation引用，以及ActionProfile的`None`、`OptionalSnapshot`或`SnapshotRequired`。Reconciler、handler与Validator MUST调用正式authoring API，不得按显示名猜引用或形成第二个Action target入口。

#### Scenario: 为攻击建立目标链

- **WHEN** Document新增InputDerived ActionTargetSnapshot并绑定Attack Profile、CanActivate与Activate
- **THEN** dry-run MUST验证全部引用属于当前Definition且类型匹配
- **AND** apply MUST通过同一整包document hash原子写入正式资产

### Requirement: Agent Document必须通过正式类型化Mutation配置 Animation Channel

Character Document MUST按有限Action Timeline与AnimationTrack stable identity表达当前`AnimationChannelId`。AI修改该字段时，Reconciler MUST生成只调用`AnimationTrack.SetAnimationChannelId`的typed Mutation，并把真实Timeline owner纳入同一事务、Validator与Report。持续Locomotion Pose source MUST不拥有可写AnimationChannel字段。该变化 MUST不修改Pose Graph、PoseStateMachine、AnimationSlot、Blend、Rig、producer source或Motion Matching Profile。

#### Scenario: 修改AnimationTrack channel

- **WHEN** Document为现有AnimationTrack提交合法非空AnimationChannelId
- **THEN** dry-run MUST按stable identity报告旧值到新值
- **AND** apply后canonical Document MUST读取正式Track的新channel identity

### Requirement: Agent Document必须完整读写 Timeline Marker 与 Curve Channel

Character Document package MUST在`timeline.json`按Timeline与Track stable identity表达Marker Sync mode、group、topology、SyncRole、call-site playback和每个Marker identity/id/frame，并在`curves.json`按registered Curve Channel表达domain、unit、wrap和完整Keyframe语义。Reconciler MUST为Marker创建、移动、删除和完整Curve替换生成typed Mutation；handler MUST只调用Timeline正式authoring API和Curve MutationAdapter。系统 MUST不接受key级MCP操作、旧Patch operation或字段名目标。

#### Scenario: 修改weighted curve

- **WHEN** Document替换registered channel的完整curve
- **THEN** Mutation MUST保留time、value、tangent、weight、WeightedMode和wrap mode
- **AND** unknown channel MUST在mutation前失败

#### Scenario: 新增重复Marker语义

- **WHEN** Document增加第二个相同MarkerId但不同local identity的occurrence
- **THEN** Reconciler MUST接受独立occurrence
- **AND** apply后canonical Document MUST输出不同MarkerAuthoringId
