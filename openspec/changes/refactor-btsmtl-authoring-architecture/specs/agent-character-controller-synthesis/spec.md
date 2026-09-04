## MODIFIED Requirements

### Requirement: Agent Snapshot 必须是只读投影

系统 MUST从当前`CharacterPipelineDefinition`和BTSMTL树生成只读canonical Snapshot，作为Document package checkout和Reconciler比较的唯一当前状态投影。Snapshot MUST包含Agent正式可写结构、stable identity、ownership与可引用catalog。Pose Graph、PoseStateMachine、Graph-owned typed Source Slot、Profile-owned direct Clip/BlendSpace/MM Binding、Locomotion Sync Group、AnimationSlot、有限Action channel binding、Timeline direct Clip引用、当前Definition可达原生AnimationClip注册Curve和node-local Policy MUST进入Document v5 editable目标状态；Rig/Virtual Bone资源正文、Body Motion、Foot Analysis、runtime state和generated product MUST只进入紧凑只读context或完全省略。Clip与子资产引用 MUST使用`assetPath + assetGuid + signed non-zero localFileId`结构化身份。Projection-local dense index、runtime generation、lease与provider index MUST不进入editable。Snapshot MUST不包含旧Sequence同步数据、Timeline locomotion producer、旧Selection、runtime临时状态、Unity YAML或generated payload，也不得因导出触发Build。

#### Scenario: checkout生成Character Document package

- **WHEN** Agent对已有Character root显式checkout
- **THEN** exporter MUST从当前树建立canonical Snapshot并写出v4目录包
- **AND** snapshot/export MUST不修改Graph、Clip或触发Program/Projection Build

#### Scenario: Presentation能力进入正式Document

- **WHEN** 某项Presentation能力已经安装进唯一Capability catalog
- **THEN** Document editable Presentation MUST复用该Capability、stable identity与typed payload
- **AND** MUST通过唯一Presentation Mutation处理，不得增加能力私有schema或第二mutation入口


### Requirement: Agent 不得形成第二个动画表现 authoring 入口

Document package editable分片与Mutation Compiler MUST只编辑正式Graph、StateMachine、Timeline、Blackboard、Presentation及唯一Clip Curve catalog已安装的业务能力。Pose Graph、PoseStateMachine、Blend、direct Clip/BlendSpace/MM Binding、Locomotion Sync Group、AnimationSlot与Policy MUST通过共享typed payload和唯一Presentation Mutation进入editable Presentation；AnimationClip注册Curve MUST通过唯一Clip Curve Mutation进入editable Clip分片；Rig资源正文、Foot Analysis与generated Projection MUST保持只读context。人工作者表面与Document apply MUST调用同一Mutation、Validator和事务服务，不得形成第二个动画表现authoring入口。未知Presentation或Clip变化 MUST被拒绝，MUST不转换成默认配置。

#### Scenario: Document配置Pose Graph

- **WHEN** AI修改Document v5中Capability已登记的Pose Graph业务字段
- **THEN** Reconciler MUST生成与人工编辑相同的typed Presentation Mutation
- **AND** 未登记字段、Rig正文、generated payload或能力私有mutation MUST被拒绝


### Requirement: Agent Authoring Document必须是声明式控制器结构

系统 MUST使用`btsmtl-agent-authoring-document.v5`目录包作为CharacterController与AIController唯一AI-facing编辑合同，并通过显式domain区分根。Character editable分片 MUST按控制binding／参数、SkillDefinition、技能Graph／局部StateMachine／State／Transition／Condition、ActionProfile、Timeline、局部变量、Presentation和AnimationClip注册Curve描述目标结构；AI editable分片 MUST继续表达Perception和Character input/request intent binding。Graph MUST使用稳定kind、typed properties、逻辑port、系统anchor、正式owner和Flow/Property Edge完整目标集合，MUST不暴露C# type name、重复port metadata、`operations[]`、内部handler、创建顺序、前序operation output、Unity YAML或任意SerializedProperty写入。Document Reconciler MUST只把正式支持的整包变化降低为内部typed Mutation。

#### Scenario: 添加状态和Transition

- **WHEN** package增加带local identity的Attack状态、owner body Graph及其到现有状态的Transition
- **THEN** Reconciler MUST生成有序State、Transition和Condition typed Mutation
- **AND** AI MUST不填写`ensure_state`、`ensure_transition`、`link_flow`或调用节点级工具

#### Scenario: 请求未知结构字段

- **WHEN** Document包含schema未登记字段或节点能力
- **THEN** strict parser或Reconciler MUST在mutation前拒绝
- **AND** MUST不创建placeholder或动态反射操作


### Requirement: Agent Document reconcile必须维护 identity 生命周期

Document Reconciler与Mutation Compiler MUST在更新现有entity时保持stable authoring identity，在`local:<meaningful-id>`创建时生成新identity，在复制entity时生成新identity。系统 MUST只接受Document package v5，不得保留v1/v2/v3/v4、v16/v17 Patch parser或按path、display name、Actor名称、Tag和列表index猜identity。Node kind与Graph kind MUST不可原地改变。AnimationClip不能由Document创建或复制，只能通过结构化对象引用选择现有原生`.anim`。Apply成功后的整包反向导出 MUST把可创建entity的新local identity替换为正式stable identity。

#### Scenario: 更新现有Timeline Segment

- **WHEN** Document通过stable identity修改一个现有Segment
- **THEN** Mutation Compiler MUST修改同一Segment并保持identity
- **AND** 最终canonical Document MUST继续输出该identity

#### Scenario: 请求创建AnimationClip

- **WHEN** Document使用local identity声明新的AnimationClip
- **THEN** strict parser或Reconciler MUST拒绝该请求
- **AND** MUST要求从Asset Catalog引用现有可写原生`.anim`


### Requirement: Agent Document必须输出稳定 authoring identity

Document package v5与其canonical Snapshot MUST按显式domain输出Graph、Node、Flow Edge、Property Edge、Timeline、Track、Segment、Curve owner、AnimationClip对象引用、Blackboard declaration、Presentation owner、Input request timing和domain正式producer的stable identity。物理文件路径与列表index MAY用于阅读但不得取代identity。Document MUST不输出Sequence identity、Marker identity、runtime mutable state、C# type name或重复port metadata。Document checkout MUST成为AI编辑的唯一领域上下文，不提供v1/v2/v3/v4、v16/v17 Patch或Snapshot镜像。

#### Scenario: Timeline元素重排后checkout

- **WHEN** 作者重排Track或Segment后显式checkout
- **THEN** 对应stable identity MUST保持
- **AND** 可读顺序和path MAY更新


### Requirement: Agent Document必须完整读写Clip注册Curve与Timeline本地Curve

Character Document v5 MUST在`editable/animation-clips/<stable-segment>/curves.json`按结构化AnimationClip对象引用表达允许的秒域注册Curve，并在`editable/timelines/**/curves.json`按Timeline owner表达Timeline-local registered Curve。两类Curve MUST使用同一canonical Keyframe语义，但不同Capability、owner和Mutation handler。Clip channel MUST按完整`EditorCurveBinding(path + type + property)`识别；Reconciler MUST为完整Curve替换生成typed Mutation；Clip handler MUST只调用Clip registered Curve Mutation，Timeline handler MUST只调用Timeline Curve MutationAdapter。系统 MUST不接受key级MCP操作、Marker字段、Sequence分片、旧Patch operation、仅propertyName匹配或字段名目标。

#### Scenario: 修改weighted Clip Curve

- **WHEN** Document替换原生AnimationClip注册channel的完整Curve
- **THEN** Mutation MUST保留time、value、tangent、weight、WeightedMode和wrap mode
- **AND** unknown channel MUST在mutation前失败

#### Scenario: Timeline尝试声明Foot Placement Weight

- **WHEN** Timeline curves分片包含`presentation.foot-placement-weight`
- **THEN** strict parser MUST按owner capability拒绝该channel
- **AND** MUST不把它转交给Clip handler


### Requirement: 资产解析必须来自当前角色 authoring context

系统 MUST通过当前`CharacterPipelineDefinition`、canonical Snapshot与Document只读catalog解析Input、控制binding、SkillDefinition、ActionProfile、Timeline和技能子图引用。Resolver MUST使用稳定identity或明确正式引用，MUST不扫描场景、目录、同名asset、旧配置或全局单例作为fallback。

#### Scenario: Document引用ActionProfile

- **WHEN** Document中的Action activation引用`Attack.Light.01`
- **THEN** Resolver MUST只从当前Definition正式ActionProfile catalog解析
- **AND** 找不到时 MUST报错且不搜索替代资产


### Requirement: Agent Snapshot 与 Validator 必须递归理解嵌套 StateMachine

Agent Snapshot MUST递归输出控制binding与技能定义、技能Runnable、inline/shared子图、技能局部StateMachine、logical transition、后续动作候选、有限Action Timeline与稳定producer identity。Presentation editable section MUST递归输出Pose Graph、PoseStateMachine/State/Transition、Pose source binding、AnimationSlot、node-local Policy与Action channel binding；Rig identity及generated产品只作为只读context。Validator MUST区分Gameplay StateMachine与PoseStateMachine，MUST不把持续Pose source伪装为Timeline producer，也不得接受旧Patch写入路径。

#### Scenario: Corin Snapshot

- **WHEN** 导出迁移后的Corin compact Snapshot
- **THEN** Gameplay section MUST显示None/Attack/Dodge及其Action Timeline
- **AND** Presentation editable section MUST显示Locomotion PoseStateMachine、Pose source、FullBodyAction Slot与Policy，context MUST显示Rig identity
- **AND** BTSMTL Graph Node/Edge MUST不输出Bone Mask或Pose composition字段

#### Scenario: Timeline channel identity断裂

- **WHEN** 可达AnimationTrack缺失或引用未知AnimationChannelId
- **THEN** Validator MUST输出对应Graph/Timeline source错误
- **AND** Compiler transaction MUST回滚


### Requirement: Agent Document必须完整表达 Action target authoring

Character Document package MUST表达由代码控制合同声明的ActionTargetSnapshot输入、技能显式输入／参数绑定、精确Skill及准入请求引用，以及ActionProfile的`None`、`OptionalSnapshot`或`SnapshotRequired`。Reconciler、handler与Validator MUST调用正式authoring API，不得按显示名猜引用或形成第二个Action target入口。

#### Scenario: 为攻击建立目标链

- **WHEN** Document新增InputDerived ActionTargetSnapshot并绑定Attack Profile、CanActivate与Activate
- **THEN** dry-run MUST验证全部引用属于当前Definition且类型匹配
- **AND** apply MUST通过同一整包document hash原子写入正式资产


### Requirement: Agent 生成链路必须是 editor-only authoring 编译链路

系统 MUST将Agent生成角色控制器实现为editor-only Document package authoring链路。Agent Document package、canonical Snapshot、Mutation Plan与Report MUST只服务编辑期checkout、修改、修复和评估。运行时 MUST只执行由正式控制合同和技能资产构建发布的Character Program、匹配代码模块与Presentation Projection，MUST不读取Document package、调用LLM或解释authoring Graph。

#### Scenario: 运行时加载角色

- **WHEN** CharacterPipelineHost向Session Host注册角色
- **THEN** runtime MUST只读取匹配身份的已发布Program、Projection和Session composition
- **AND** MUST不读取Agent Document package、Mutation Plan或LLM输出文件
