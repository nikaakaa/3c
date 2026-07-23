## RENAMED Requirements

- FROM: `### Requirement: Patch IR 必须是确定性的 graph 编辑指令`
- TO: `### Requirement: Agent Authoring Document必须是声明式控制器结构`
- FROM: `### Requirement: Agent Patch 编译必须维护 identity 生命周期`
- TO: `### Requirement: Agent Document reconcile必须维护 identity 生命周期`
- FROM: `### Requirement: Agent Patch Compiler内部必须使用唯一类型化命令计划`
- TO: `### Requirement: Agent Document必须降低为唯一类型化Mutation计划`
- FROM: `### Requirement: Agent Snapshot schema v16 必须输出稳定 authoring identity`
- TO: `### Requirement: Agent Document必须输出稳定 authoring identity`
- FROM: `### Requirement: Agent Patch 必须通过类型化命令修改 MotionWarp`
- TO: `### Requirement: Agent Document必须通过类型化Mutation修改 MotionWarp`
- FROM: `### Requirement: Agent 必须完整修改 Action target authoring`
- TO: `### Requirement: Agent Document必须完整表达 Action target authoring`
- FROM: `### Requirement: Agent v16 CharacterController 必须通过正式类型化操作配置 Animation Channel`
- TO: `### Requirement: Agent Document必须通过正式类型化Mutation配置 Animation Channel`
- FROM: `### Requirement: Agent v16 CharacterController 必须完整读写 Timeline Marker 与 Curve Channel`
- TO: `### Requirement: Agent Document必须完整读写 Timeline Marker 与 Curve Channel`

## REMOVED Requirements

### Requirement: Agent Intent 必须表达角色动作业务意图

**Reason**：可编辑Document已经直接使用StateMachine、State、Condition、Action、Timeline和Blackboard业务结构表达目标控制器；保留独立Intent会形成第二份不完整业务模型。

**Migration**：删除`AgentControllerIntent`及其窗口入口，AI只编辑checkout生成的正式Document正文。

#### Scenario: 删除Intent入口

- **WHEN** 新Document合同安装
- **THEN** MCP、Window和service MUST不再接收AgentControllerIntent
- **AND** MUST不提供Intent到Document转换器

### Requirement: Macro 必须将业务意图展开为受限 Patch IR

**Reason**：Macro会隐藏节点、生命周期和业务默认值，当前实现也已经拒绝展开。Document Reconciler必须只降低AI明确写出的完整业务结构。

**Migration**：删除`AgentMacroLibrary`与`AgentMacroCoverageEvaluator`；业务样例覆盖改为检查Document及其Mutation Plan。

#### Scenario: 删除two_hit_combo Macro

- **WHEN** AI需要表达二连击
- **THEN** AI MUST在Document中显式描述外层Attack、内层StateMachine、两个攻击状态、条件、Action和Timeline
- **AND** 系统 MUST不调用宏补充默认结构

## MODIFIED Requirements

### Requirement: Agent必须保持Generated Foot Analysis只读

Agent Character Document MUST只把正式Graph、StateMachine、Timeline、Marker、registered editable Curve Channel和Profile identity放入相应可写或只读区域。Animation Clip的Foot Placement Weight MUST继续作为完整可编辑curve进入Document；Projection生成的sole speed、height、plant confidence、next landing confidence、delay与offset MUST不进入editable正文或Mutation Plan。Agent MUST不复制generated payload，也 MUST不创建Foot Analysis mutation。

#### Scenario: Agent尝试写Generated channel

- **WHEN** Document editable正文提交未登记的LeftPlant、RightPlant或Landing ChannelId
- **THEN** Reconciler MUST按未知Curve Channel拒绝整个Document
- **AND** MUST不修改Timeline、Projection或Analysis Source

### Requirement: Agent 生成链路必须是 editor-only authoring 编译链路

系统 MUST将Agent生成角色控制器实现为editor-only Document authoring链路。Agent Document、canonical Snapshot、Mutation Plan与Report MUST只服务编辑期checkout、修改、修复和评估。运行时 MUST只执行由正式BTSMTL资产显式编译并发布的Character Program与Presentation Projection，MUST不读取Document、调用LLM或解释authoring Graph。

#### Scenario: 运行时加载角色

- **WHEN** CharacterPipelineHost向Session Host注册角色
- **THEN** runtime MUST只读取匹配身份的已发布Program、Projection和Session composition
- **AND** MUST不读取Agent Document、Mutation Plan或LLM输出文件

### Requirement: Agent Snapshot 必须是只读投影

系统 MUST能从当前`CharacterPipelineDefinition`和BTSMTL树生成只读canonical Snapshot，作为Document checkout和Reconciler比较的唯一当前状态投影。Snapshot MUST包含Agent正式可写结构、stable identity、ownership与可引用catalog，并 MUST把Presentation、Body Motion、Foot Analysis和generated product限制为只读context。Snapshot MUST不成为正式配置来源，不保存runtime临时状态，不暴露Unity YAML，也不因导出触发build。

#### Scenario: checkout生成Character Document

- **WHEN** Agent对已有Character root显式checkout
- **THEN** exporter MUST从当前树建立canonical Snapshot并写出Document
- **AND** snapshot/export MUST不修改Graph或触发Program build

### Requirement: Agent Authoring Document必须是声明式控制器结构

系统 MUST使用`btsmtl-agent-authoring-document.v1`作为CharacterController与AIController唯一AI-facing编辑合同，并通过显式domain区分根。Document editable正文 MUST按Graph、StateMachine、State、Transition、Condition、Action、Timeline、Blackboard、Perception和Intent实体描述目标结构；MUST不暴露`operations[]`、内部handler、创建顺序、前序operation output、Unity YAML或任意SerializedProperty写入。Document Reconciler MUST只把正式支持的变化降低为内部typed Mutation。

#### Scenario: 添加状态和Transition

- **WHEN** Document增加带local identity的Attack状态及其到现有状态的Transition
- **THEN** Reconciler MUST生成有序State、Transition和Condition typed Mutation
- **AND** AI MUST不填写`ensure_state`、`ensure_transition`或`link_flow`

#### Scenario: 请求未知结构字段

- **WHEN** Document包含schema未登记字段或节点能力
- **THEN** strict parser或Reconciler MUST在mutation前拒绝
- **AND** MUST不创建placeholder或动态反射操作

### Requirement: Compiler 必须调用 BTSMTL 正式 authoring API

系统 MUST通过Document Reconciler和Mutation Compiler把目标Document应用到BTSMTL graph。Compiler与handler MUST继续调用`BaseGraph.CreateNode`、`BaseGraph.Link`、`BaseGraph.LinkProperty`、正式节点配置入口和Timeline ownership API，尊重`CanCreateNodeType`、PropertyPort PortId、Graph kind与inline/shared ownership。系统 MUST不维护第二套节点、边、端口、Timeline或Workbench数据。

#### Scenario: Document要求非法节点位置

- **WHEN** Document把TimelineNode放入StateMachineGraph
- **THEN** Reconciler或preflight MUST拒绝并输出Document entity路径
- **AND** 正式Graph MUST保持不变

### Requirement: Node Emitter 必须使用白名单

系统 MUST使用Node Emitter与domain capability白名单限定Document可表达并可创建的节点。每个能力 MUST声明允许的graph kind、必需参数、资产引用和typed Mutation lowering。未知节点、未知字段、未知端口或未登记参数 MUST被拒绝，MUST不降级为placeholder、fallback节点或字符串脚本。

#### Scenario: Document创建未登记节点

- **WHEN** editable正文包含未登记node type
- **THEN** Reconciler MUST报告未知节点及Document路径
- **AND** MUST不产生Mutation

### Requirement: 资产解析必须来自当前角色 authoring context

系统 MUST通过当前`CharacterPipelineDefinition`、canonical Snapshot与Document只读catalog解析Input、ActionProfile、Timeline和RootTree引用。Resolver MUST使用稳定identity或明确正式引用，MUST不扫描场景、目录、同名asset、旧配置或全局单例作为fallback。

#### Scenario: Document引用ActionProfile

- **WHEN** Document中的Action activation引用`Attack.Light.01`
- **THEN** Resolver MUST只从当前Definition正式ActionProfile catalog解析
- **AND** 找不到时 MUST报错且不搜索替代资产

### Requirement: Compile Report 必须支持 Agent 自修复

系统 MUST输出`AgentCompileReport`，包含Document schema、sync、引用、reconcile、preflight、apply、语义错误、planned/applied diff、metrics和建议修复。Report MUST使用机器可读路径定位Document entity、Graph、Node、Edge、Timeline或asset，并 MUST返回同步状态与document hash。Report MUST不再要求AI生成下一轮Patch operation。

#### Scenario: Document reconcile失败

- **WHEN** Reconciler拒绝一个Timeline entity
- **THEN** report MUST标出Document entity路径、错误code、原因和建议
- **AND** AI MUST能直接修改同一Document后重新dry-run

### Requirement: Agent 评估必须区分结构、语义和业务覆盖

系统 MUST统计Document schema合法率、reconcile成功率、preflight成功率、语义合法率、引用解析成功率、修复轮数、diff size、非目标人工内容保留率和业务覆盖度。受控样例 MUST检查Document目标结构及其Mutation Plan，MUST不执行Agent JSON或运行时LLM。

#### Scenario: 评估二连击Document

- **WHEN** 样例要求AI生成二连击
- **THEN** evaluator MUST检查Document含外层Attack、内层状态机、两个攻击状态、Action、Timeline与条件
- **AND** MUST检查Reconciler和Validator接受最终结构

### Requirement: 正式资产必须仍由人类可微调

系统 MUST保持apply后的正式结果为普通BTSMTL Graph、Timeline、ActionProfile及其正式Definition引用。作者可以继续使用Graph Editor、Timeline Editor和各正式Profile Inspector。人工编辑只使live authoring revision变化；系统 MUST不自动刷新Document或build。AI再次编辑时 MUST显式checkout或处理Conflict，MUST不覆盖未合并的人工变化。

#### Scenario: 作者微调后AI继续编辑

- **WHEN** 作者在上次Clean后修改Graph且AI未修改Document
- **THEN** 状态 MUST为TreeDirty
- **AND** 显式checkout MUST从当前正式树刷新Document
- **AND** checkout MUST不修改只读Presentation配置

### Requirement: Agent Document reconcile必须维护 identity 生命周期

Document Reconciler与Mutation Compiler MUST在更新现有entity时保持stable authoring identity，在新local identity创建时生成新identity，在复制entity时生成新identity。系统 MUST只接受Document v1，不得保留v16/v17 Patch parser或按path、display name、Actor名称、Tag和列表index猜identity。Apply成功后的反向导出 MUST把新local identity替换为正式stable identity。

#### Scenario: 更新现有Timeline Clip

- **WHEN** Document通过stable identity修改一个现有Clip
- **THEN** Mutation Compiler MUST修改同一Clip并保持identity
- **AND** 最终canonical Document MUST继续输出该identity

#### Scenario: 创建新Marker occurrence

- **WHEN** Document使用local identity增加一个Marker occurrence
- **THEN** Reconciler MUST为其建立planning symbol
- **AND** apply后反向导出 MUST写入新MarkerAuthoringId

### Requirement: Agent Document必须降低为唯一类型化Mutation计划

系统 MUST让strict Document parser与`AgentDocumentReconciler`一次生成immutable typed `AgentMutationPlan`。CharacterController与AIController MUST复用同一planning symbol、preflight、资产事务和handler catalog基础；domain handler只消费正式authoring API。Dry-run与apply MUST基于同一Document semantic hash生成等价plan，后续handler不得读取原始JSON discriminator或建立AI专用compiler和第二事务。

#### Scenario: 同一Document执行dry-run和apply

- **WHEN** 合法Document先dry-run再以同一document hash申请apply
- **THEN** 两次lowering MUST产生相同plan hash和planned diff
- **AND** apply MUST在资产事务中消费等价typed plan

### Requirement: 通用Agent Validator与业务样例覆盖必须分层

`AgentGraphValidator` MUST只检查对任意Definition成立的Graph kind、Condition纯度、Timeline ownership、serialized owner、TreeClip、Action Context、Input/ActionProfile引用、identity和正式Compiler语义。具体样例结构覆盖 MUST由Document Synthesis Evaluator检查Document与Mutation Plan，MUST不进入普通`validate`。

#### Scenario: 验证非Corin角色

- **WHEN** 作者验证使用不同状态名和连招层数的合法角色
- **THEN**通用Validator MUST只按正式authoring语义判断
- **AND** MUST不要求Corin状态名、连招数量或Macro名称

### Requirement: Agent Document必须输出稳定 authoring identity

Document v1与其canonical Snapshot MUST按显式domain输出Graph、Node、Edge、Timeline、Track、Clip、Marker、Curve owner、Blackboard declaration、Input request timing和domain正式producer的stable identity。Path和index MAY用于阅读但不得取代identity。Document MUST不输出runtime mutable state。Document checkout MUST成为AI编辑的唯一上下文，不提供v16/v17 Patch或Snapshot镜像。

#### Scenario: Timeline元素重排后checkout

- **WHEN** 作者重排Track、Clip或Marker后显式checkout
- **THEN** 对应stable identity MUST保持
- **AND** 可读顺序和path MAY更新

### Requirement: Agent 不得形成第二个动画表现 authoring 入口

Document editable正文与Mutation Compiler MUST只编辑正式Graph、StateMachine、Timeline、Blackboard及已安装的Agent能力。CharacterAnimationPresentationProfile、Pose Graph、Blend、Rig、producer source binding和generated Projection MUST保持只读context。未知Presentation变化 MUST被拒绝，MUST不转换成默认配置。

#### Scenario: Document尝试配置Pose Graph

- **WHEN** AI修改Document只读Presentation字段或加入Pose Graph mutation
- **THEN** parser或Reconciler MUST拒绝
- **AND** MUST不建立第二个Presentation写入口

### Requirement: Agent Document必须通过类型化Mutation修改 MotionWarp

Character Document MUST完整表达MotionWarp Track/Clip、source stable identity、typed参数和删除后的目标集合。Reconciler MUST降低为唯一typed Mutation Plan；handler MUST调用Timeline正式authoring API，不得直接编辑YAML、按名称猜source或创建第二套MotionWarp配置。

#### Scenario: Document创建目标攻击Warp

- **WHEN** Document新增Warp并引用现有或同Document新建的MotionCurveClip identity
- **THEN** Reconciler MUST解析为stable或local planning reference
- **AND** handler MUST创建合法MotionWarpClip并保持source关系

### Requirement: Agent Document必须完整表达 Action target authoring

Character Document MUST表达`ActionTargetSnapshot` Blackboard declaration、InputDerived InputValueId、准入与activation引用，以及ActionProfile的`None`、`OptionalSnapshot`或`SnapshotRequired`。Reconciler、handler与Validator MUST调用正式authoring API，不得按显示名猜引用或形成第二个Action target入口。

#### Scenario: 为攻击建立目标链

- **WHEN** Document新增InputDerived ActionTargetSnapshot并绑定Attack Profile、CanActivate与Activate
- **THEN** dry-run MUST验证全部引用属于当前Definition且类型匹配
- **AND** apply MUST通过同一Document hash原子写入正式资产

### Requirement: Agent Snapshot必须只读投影Body Motion Profile

Character Document context MUST从显式Definition只读输出Body Motion Profile stable identity、content revision、GravityAcceleration、MaximumFallSpeed、semantic version、required AirborneVerticalMotion capability与Compiler状态。Editable正文与Mutation Plan MUST不提供Profile修改、任意SerializedProperty或第二Profile写入口。

#### Scenario: AI尝试修改Body Motion参数

- **WHEN** AI改变Document context中的GravityAcceleration
- **THEN** Reconciler MUST报告readonly context修改
- **AND** MUST不产生Profile Mutation

### Requirement: Agent Document必须通过正式类型化Mutation配置 Animation Channel

Character Document MUST按Timeline与AnimationTrack stable identity表达当前`AnimationChannelId`。AI修改该字段时，Reconciler MUST生成只调用`AnimationTrack.SetAnimationChannelId`的typed Mutation，并把真实Timeline owner纳入同一事务、Validator与Report。该变化 MUST不修改Pose Graph、Blend、Rig、producer source或Motion Matching Profile。

#### Scenario: 修改AnimationTrack channel

- **WHEN** Document为现有AnimationTrack提交合法非空AnimationChannelId
- **THEN** dry-run MUST按stable identity报告旧值到新值
- **AND** apply后canonical Document MUST读取正式Track的新channel identity

### Requirement: Agent Document必须完整读写 Timeline Marker 与 Curve Channel

Character Document MUST按Timeline与Track stable identity表达Marker Sync mode、group、topology、SyncRole、call-site playback、每个Marker identity/id/frame，以及registered Curve Channel的domain、unit、wrap和完整Keyframe字段。Reconciler MUST为Marker创建、移动、删除和完整Curve替换生成typed Mutation；handler MUST只调用Timeline正式authoring API和Curve MutationAdapter。系统 MUST不接受旧Patch operation或字段名目标。

#### Scenario: 修改weighted curve

- **WHEN** Document替换registered channel的完整curve
- **THEN** Mutation MUST保留time、value、tangent、weight、WeightedMode和wrap mode
- **AND** unknown channel MUST在mutation前失败

#### Scenario: 新增重复Marker语义

- **WHEN** Document增加第二个相同MarkerId但不同local identity的occurrence
- **THEN** Reconciler MUST接受独立occurrence
- **AND** apply后canonical Document MUST输出不同MarkerAuthoringId

### Requirement: Agent 必须阻止 Marker Sync 数据分裂

Agent Document MUST只把Marker Sync可写数据放在AnimationTrack entity，不得放入Presentation、TimelineNode override、Blackboard、StateMachine edge、ActionProfile、FootPhase或generated Projection。Target MUST使用stable identity或Document local identity；名称、breadcrumb和index不得成为fallback。Validator MUST继续覆盖None残留、identity、Finite/Cyclic边界、call site、directed pair和animation output coverage。

#### Scenario: None Track保留Marker

- **WHEN** Document把Track设为None但仍保留group或Marker
- **THEN** dry-run MUST拒绝整个Document
- **AND** apply MUST不产生部分资产修改

### Requirement: Agent Validator必须透传正式Foot Analysis编译诊断

Agent Validator MUST透传正式Artifact Builder、Artifact Store、Projection binding与Build Transaction诊断，区分Missing、Stale、Corrupt、Source/Rig/Calibration不匹配和stable clip binding缺失。Agent Document与Mutation链 MUST不采样AnimationClip、不写artifact、不输出feature payload，也不得新增Foot Analysis rebuild或generated curve mutation。

#### Scenario: Document修改Foot Placement Weight

- **WHEN** 合法Document只修改现有Foot Placement Weight并显式apply
- **THEN** 正式Definition Build MUST在apply事务中重新校验所需artifact并发布Projection
- **AND** Agent Document链 MUST不直接读取或修改artifact文件

### Requirement: Agent Compiler模块必须按authoring职责聚合

Document Mutation Compiler MUST保持唯一Compiler facade，但单次Definition、Snapshot、Resolver、Graph Index、planning symbol、diff与touched owner MUST由每次调用独占的Mutation session拥有。StateMachine、StateBehavior、Node/Asset、GraphLink、Timeline与ConditionRule MUST由按共享authoring不变量聚合的handler处理。Compiler MUST不拥有Undo、dirty、rollback或SaveAssets；这些职责 MUST继续只属于统一Document application service。

#### Scenario: 连续reconcile两个Definition

- **WHEN** 同一Compiler实例连续dry-run两个不同Definition
- **THEN** 第二次调用 MUST创建全新Mutation session
- **AND** MUST不读取第一次调用的Resolver、Index、planning symbol或touched owner

### Requirement: Agent Validator 必须复用 MotionWarp 正式校验

Agent Validator MUST检查source identity、Timeline owner、窗口、Action channel、Override语义、mode、offset、weight、clamp、progress curve、Action Context与ActionTargetRequirement，并与Inspector和Semantic Compiler复用同一校验服务。任何错误 MUST定位到Document entity、Graph、Timeline、Track、Clip、ActionProfile与相关source。

#### Scenario: Document为无目标动作配置Warp

- **WHEN** Document为`ActionTargetRequirement.None`动作增加MotionWarp
- **THEN** dry-run MUST失败并报告目标要求矛盾
- **AND** apply MUST不修改任何资产
