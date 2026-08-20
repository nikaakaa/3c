## MODIFIED Requirements

### Requirement: Agent必须保持Generated Foot Analysis只读

Agent Character Document package MUST只把正式Graph、StateMachine、Timeline-local Curve、Presentation装配和原生AnimationClip注册Curve放入对应editable分片。`presentation.foot-placement-weight`与`presentation.locomotion-phase` MUST只以完整Curve进入精确`editable/animation-clips/**/curves.json`；Projection生成的sole speed、height、plant confidence、next landing confidence、delay、offset与关系质量结果 MUST不进入editable分片或Mutation Plan。Agent MUST不复制generated payload、不创建Foot Analysis mutation，也不得把Analysis候选当作正式Curve。

#### Scenario: Agent修改Foot Placement Weight

- **WHEN** AI替换现有原生AnimationClip的完整Foot Placement Weight Curve
- **THEN** Reconciler MUST生成Clip registered Curve typed Mutation
- **AND** MUST不修改Foot Analysis Artifact或Timeline Segment

### Requirement: Agent Snapshot 必须是只读投影

系统 MUST从当前`CharacterPipelineDefinition`和BTSMTL树生成只读canonical Snapshot，作为Document package checkout和Reconciler比较的唯一当前状态投影。Snapshot MUST包含Agent正式可写结构、stable identity、ownership与可引用catalog。Pose Graph、PoseStateMachine、Graph-owned typed Source Slot、Profile-owned direct Clip/BlendSpace/MM Binding、Locomotion Sync Group、AnimationSlot、有限Action channel binding、Timeline direct Clip引用、当前Definition可达原生AnimationClip注册Curve和node-local Policy MUST进入Document v4 editable目标状态；Rig/Virtual Bone资源正文、Body Motion、Foot Analysis、runtime state和generated product MUST只进入紧凑只读context或完全省略。Clip与子资产引用 MUST使用`assetPath + assetGuid + signed non-zero localFileId`结构化身份。Projection-local dense index、runtime generation、lease与provider index MUST不进入editable。Snapshot MUST不包含Sequence、Marker Sync、Timeline locomotion producer、旧Selection、runtime临时状态、Unity YAML或generated payload，也不得因导出触发Build。

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

- **WHEN** AI修改Document v4中Capability已登记的Pose Graph业务字段
- **THEN** Reconciler MUST生成与人工编辑相同的typed Presentation Mutation
- **AND** 未登记字段、Rig正文、generated payload或能力私有mutation MUST被拒绝

### Requirement: Agent Authoring Document必须是声明式控制器结构

系统 MUST使用`btsmtl-agent-authoring-document.v4`目录包作为CharacterController与AIController唯一AI-facing编辑合同，并通过显式domain区分根。Character editable分片 MUST按Graph、StateMachine、State、Transition、Condition、Action、Timeline、Blackboard、Presentation和AnimationClip注册Curve描述目标结构；AI editable分片 MUST继续表达Perception和Character input/request intent binding。Graph MUST使用稳定kind、typed properties、逻辑port、系统anchor、正式owner和Flow/Property Edge完整目标集合，MUST不暴露C# type name、重复port metadata、`operations[]`、内部handler、创建顺序、前序operation output、Unity YAML或任意SerializedProperty写入。Document Reconciler MUST只把正式支持的整包变化降低为内部typed Mutation。

#### Scenario: 添加状态和Transition

- **WHEN** package增加带local identity的Attack状态、owner body Graph及其到现有状态的Transition
- **THEN** Reconciler MUST生成有序State、Transition和Condition typed Mutation
- **AND** AI MUST不填写`ensure_state`、`ensure_transition`、`link_flow`或调用节点级工具

#### Scenario: 请求未知结构字段

- **WHEN** Document包含schema未登记字段或节点能力
- **THEN** strict parser或Reconciler MUST在mutation前拒绝
- **AND** MUST不创建placeholder或动态反射操作

### Requirement: Agent Document reconcile必须维护 identity 生命周期

Document Reconciler与Mutation Compiler MUST在更新现有entity时保持stable authoring identity，在`local:<meaningful-id>`创建时生成新identity，在复制entity时生成新identity。系统 MUST只接受Document package v4，不得保留v1/v2/v3、v16/v17 Patch parser或按path、display name、Actor名称、Tag和列表index猜identity。Node kind与Graph kind MUST不可原地改变。AnimationClip不能由Document创建或复制，只能通过结构化对象引用选择现有原生`.anim`。Apply成功后的整包反向导出 MUST把可创建entity的新local identity替换为正式stable identity。

#### Scenario: 更新现有Timeline Segment

- **WHEN** Document通过stable identity修改一个现有Segment
- **THEN** Mutation Compiler MUST修改同一Segment并保持identity
- **AND** 最终canonical Document MUST继续输出该identity

#### Scenario: 请求创建AnimationClip

- **WHEN** Document使用local identity声明新的AnimationClip
- **THEN** strict parser或Reconciler MUST拒绝该请求
- **AND** MUST要求从Asset Catalog引用现有可写原生`.anim`

### Requirement: Agent Document必须输出稳定 authoring identity

Document package v4与其canonical Snapshot MUST按显式domain输出Graph、Node、Flow Edge、Property Edge、Timeline、Track、Segment、Curve owner、AnimationClip对象引用、Blackboard declaration、Presentation owner、Input request timing和domain正式producer的stable identity。物理文件路径与列表index MAY用于阅读但不得取代identity。Document MUST不输出Sequence identity、Marker identity、runtime mutable state、C# type name或重复port metadata。Document checkout MUST成为AI编辑的唯一领域上下文，不提供v1/v2/v3、v16/v17 Patch或Snapshot镜像。

#### Scenario: Timeline元素重排后checkout

- **WHEN** 作者重排Track或Segment后显式checkout
- **THEN** 对应stable identity MUST保持
- **AND** 可读顺序和path MAY更新

## ADDED Requirements

### Requirement: Agent Document必须完整读写Clip注册Curve与Timeline本地Curve

Character Document v4 MUST在`editable/animation-clips/<stable-segment>/curves.json`按结构化AnimationClip对象引用表达允许的注册Curve，并在`editable/timelines/**/curves.json`按Timeline owner表达Timeline-local registered Curve。两类Curve MUST使用同一canonical Keyframe语义，但不同Capability、owner和Mutation handler。Reconciler MUST为完整Curve替换生成typed Mutation；Clip handler MUST只调用Clip registered Curve Mutation，Timeline handler MUST只调用Timeline Curve MutationAdapter。系统 MUST不接受key级MCP操作、Marker字段、Sequence分片、旧Patch operation或字段名目标。

#### Scenario: 修改weighted Clip Curve

- **WHEN** Document替换原生AnimationClip注册channel的完整Curve
- **THEN** Mutation MUST保留time、value、tangent、weight、WeightedMode和wrap mode
- **AND** unknown channel MUST在mutation前失败

#### Scenario: Timeline尝试声明Foot Placement Weight

- **WHEN** Timeline curves分片包含`presentation.foot-placement-weight`
- **THEN** strict parser MUST按owner capability拒绝该channel
- **AND** MUST不把它转交给Clip handler

## REMOVED Requirements

### Requirement: Agent 必须阻止 Marker Sync 数据分裂

该Requirement被删除。Marker Sync本体已经删除；Validator改为拒绝任何旧Marker字段。

#### Scenario: Document包含Marker字段

- **WHEN** Timeline、Presentation或Clip分片包含SyncMode、Group、Topology、Role或Marker occurrence
- **THEN** strict parser MUST拒绝整个Document
- **AND** apply MUST不产生部分资产修改

### Requirement: Agent Document必须完整读写 Timeline Marker 与 Curve Channel

该Requirement被Clip注册Curve与Timeline本地Curve分片合同取代；Timeline Marker不再可写或导出。

#### Scenario: 旧timeline.json包含Marker

- **WHEN** v4 Timeline分片出现Marker Sync payload
- **THEN** strict parser MUST报告未知旧字段
- **AND** MUST不生成Marker Mutation
