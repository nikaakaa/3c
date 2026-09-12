## RENAMED Requirements

- FROM: `### Requirement: Document v7必须原子替代v6`
- TO: `### Requirement: Document v8必须原子替代v7`
- FROM: `### Requirement: Document v7失败恢复必须同时覆盖Unity owner与正式package`
- TO: `### Requirement: Document v8失败恢复必须同时覆盖Unity owner与正式package`

## MODIFIED Requirements

### Requirement: Agent Authoring Document必须是按需生成的持久化目录包

系统 MUST为每个已有合法`CharacterPipelineDefinition`提供唯一确定性`btsmtl-agent-authoring-document.v8`文档包。Behavior Designer AI不以BTSMTL Document为根。文档包 MUST位于Unity项目内、`Assets/`之外的`AgentAuthoring/Documents/CharacterController/<root-key>.btsmtl/`，并只在显式checkout时从当前正式Unity authoring创建或刷新。文档包 MUST不成为BTSMTL正式真相、Unity资产、Player内容或runtime输入。

#### Scenario: Agent首次编辑现有Character Controller

- **WHEN** Agent对已有合法Character root显式checkout
- **THEN** 系统 MUST从当前正式Skill Graph、Macro、Skill Timeline、Presentation与可达Clip Curve生成规范目录包
- **AND** response MUST返回唯一文档包绝对路径
- **AND** 系统 MUST不修改或保存Unity资产

#### Scenario: 普通人工编辑期间没有AI会话

- **WHEN** 作者修改Graph、Timeline或AnimationClip但没有显式checkout
- **THEN** 系统 MUST不创建或刷新文档包
- **AND** MUST不触发reconcile、compile、build或publish

### Requirement: 可编辑能力必须由唯一authoring capability catalog闭合

系统 MUST使用同一authoring capability catalog驱动exporter、strict parser、Reconciler、handler preflight、Validator及只读Skill Node/Graph catalog。Skill正式节点上的Editor-only metadata marker、BTSMTL共享Graph descriptor与正式Mutation binding MUST投影为该唯一catalog；`AgentPackage...`只能是v8包外壳，不得成为第二作者语义来源。每个editable Skill Node kind MUST声明允许Graph role、typed properties、默认值、逻辑ports、资产引用与create/configure/delete lowering。任何可导出实体若不能完整创建、修改、连接、删除和反向导出，checkout MUST以`authoring_capability_incomplete`失败，不得输出假可编辑结构。

#### Scenario: Exporter发现未登记Node类型

- **WHEN** 当前正式Graph包含一个未形成完整capability descriptor的可写Node
- **THEN** checkout MUST报告Node identity、Graph identity与缺失能力
- **AND** MUST不把C# type name直接写入editable作为绕过

### Requirement: Agent实现必须是通用Document适配器

Agent实现 MUST只拥有v8 Document文件生命周期、规范解析与写出、整包Hash、同步状态、通用闭包检查、通用Diff、正式Mutation Dispatcher的调用顺序以及唯一事务编排。Agent实现 MUST不再拥有独立的Unity语义快照、按Skill/Pose/Timeline分别维护的领域对象模型、相同目标的Draft到Plan重复中间表示或领域字段/端口/owner规则。正式领域Module MUST通过Authoring Capability、正式字段描述、引用描述和Mutation binding提供这些语义；Agent只传递稳定identity、capability、typed properties、logical ports、references和owner。

#### Scenario: Agent读取正式作者状态

- **WHEN** Agent执行checkout或dry-run
- **THEN** Document生命周期 MUST从正式领域Module获得只读live context和可编辑目标投影
- **AND** MUST不先生成另一套包含领域语义的`AgentGraphSnapshot`
- **AND** MUST不把领域字段复制到Agent专属快照类型后再转换成Document

#### Scenario: Agent修改任意已登记Skill能力

- **WHEN** AI在合法闭包中新增或修改已登记Capability的Node、Macro、Timeline或Blackboard声明
- **THEN** 通用Document Diff MUST把目标交给正式Capability和Mutation binding
- **AND** MUST不为每个Node kind增加Agent专用模型、字段表、端口表或Apply分支

#### Scenario: Agent完成正式apply

- **WHEN** 通用Diff已经通过Capability验证
- **THEN** Document MUST直接生成一个正式Mutation Plan并交给唯一Dispatcher
- **AND** MUST不经过`AgentMutationDraft`等与正式Plan重复的中间语义层
- **AND** Undo、Rollback、Save、reverse export仍 MUST由唯一Document Transaction拥有

### Requirement: Presentation分片必须保持整包同步与稳定owner

Document v8 MUST使用`editable/presentation/profile.json`、`editable/presentation/pose-graphs/<graph-id>/graph.json`、对应`layout.json`，以及`editable/presentation/pose-state-machines/<state-machine-id>/state-machine.json`与对应`layout.json`表达Presentation目标状态。Graph分片覆盖AnimGraph、Animation Layer、State Pose、Transition Rule和Control Rig；Profile MUST表达原生直接资源、有限Action producer binding与Locomotion Sync Group，Rig关联Slot／Group／Blend Profile通过正式owner context和typed Mutation表达；Pose StateMachine MUST只表达Entry、State、Alias、Transition、Rule与Blend，不保存Marker或同步override。Player与Timeline AnimationClip MUST通过包含asset GUID、有符号且非零local file id和一致asset path的结构化对象引用表达。新建子资产 MAY使用`local:*`，AnimationClip MUST不允许local identity。分片 MUST通过稳定owner identity互相引用，并继续服从整包checkout、hash、dry-run、apply、Conflict与反向导出语义；不得提供文件级apply、旧单文件reader、按显示名解析或缺失local file id fallback。

#### Scenario: Agent只修改一个Player资源

- **WHEN** 一个Pose Graph ClipPlayer改为引用另一个既有原生AnimationClip
- **THEN** dry-run与apply MUST锁定整个Document包及精确Pose Graph/Clip owner
- **AND** 反向导出 MUST更新整包基线与唯一结构化资源引用

#### Scenario: AI提交退役Profile binding

- **WHEN** editable使用`local:*`声明新的Profile-owned Source binding
- **THEN** strict parser MUST拒绝该退役语义
- **AND** MUST要求Player直接引用原生资源

### Requirement: Presentation Reconciler必须调用唯一Presentation Mutation

Document v8 Reconciler MUST按owner依赖生成类型化Presentation Mutation计划，并与人工编辑共用validator、资产级transaction、子资产identity allocator、dirty owner与诊断。直接Clip／Blend Space／MM资源、Animation Slot／Group、Locomotion Sync Group、Pose Graph和PoseStateMachine的创建、修改、引用与删除 MUST在同一个正式资产事务中处理；Reconciler MUST不直接写Unity YAML、SerializedObject path、generated Projection或第二份字符串binding。

#### Scenario: apply提交退役Source Slot与binding

- **WHEN** 文档目标状态新增Graph-owned Source Slot或Profile-owned Source Binding
- **THEN** Reconciler MUST拒绝该退役作者模型并要求Player直接引用原生资源
- **AND** MUST不创建兼容子资产

#### Scenario: apply修改Locomotion Sync Group

- **WHEN** 文档目标状态调整Group中的原生AnimationClip成员
- **THEN** Reconciler MUST使用结构化Clip引用生成Profile Mutation并校验成员唯一性
- **AND** MUST不修改Clip Curve或自动Build Projection

### Requirement: Document v8必须原子替代v7

系统 MUST删除v7及更早schema、reader、writer、manifest识别、文档包兼容与升级器，只接受v8 Document。已有v7工作目录 MUST要求显式重新checkout生成v8，不得静默迁移、fallback读取或并存两种apply路径。五个生命周期工具及其事务语义 MUST保持不变。

#### Scenario: 读取v3文档包

- **WHEN** service发现schema为`btsmtl-agent-authoring-document.v3`
- **THEN** dry-run与apply MUST拒绝该文档且不修改资产
- **AND** 调用方 MUST显式重新checkout

### Requirement: Document v8失败恢复必须同时覆盖Unity owner与正式package

Application Service MUST在首次Mutation前解析并锁定全部Gameplay、Timeline、AnimationClip与Presentation serialized owner，并注册一个完整Undo事务。只有Mutation、全域Validator、Unity authoring保存、最终树反向导出、staging重读与hash校验、正式package原子替换全部成功后，apply才可返回`applied=true`、`saved=true`与`Clean`。任一步失败 MUST恢复全部Unity owner并保留上一份正式package；Character apply MUST不发布Foot Analysis、Program、Projection或Native Pose Program。Clip registered Curve Mutation MUST只改变完整dependency baseline与Registered Curve Hash并使相关Projection stale，不得修改`AnimationClipAnalysisInputHash`或把匹配Artifact标记为stale。

#### Scenario: Clip Curve Validator失败

- **WHEN** Gameplay和Timeline mutation已经执行，但Clip Curve Validator发现Phase非单调
- **THEN** Application Service MUST回滚同一事务内全部Gameplay、Timeline、Clip与Presentation owner
- **AND** 正式Document package MUST保持apply前内容且响应不得报告`Clean`

### Requirement: AnimationClip注册Curve必须使用独立严格分片

Document v8 MUST只为当前Definition闭包中实际可达且位于可写原生`.anim`的AnimationClip输出`editable/animation-clips/<stable-segment>/curves.json`。分片 MUST包含结构化Clip对象引用、完整dependency baseline、只读`AnimationClipAnalysisInputHash`和Clip Curve catalog允许的秒域完整canonical Curve目标集合；从目标集合省略已有channel MUST表达删除。可达Clip的Foot Weight删除和仍为Locomotion Sync Group成员的Phase删除 MUST被Validator拒绝。分片 MUST不包含骨骼Curve、AnimationEvent、import设置、Rig、Foot Analysis Artifact、Phase Validation samples、Group或generated plan。Exporter、strict parser、Reconciler、handler、Validator与reverse exporter MUST复用同一Clip Curve capability，并按完整`EditorCurveBinding(path + type + property)`识别channel，不得只比较propertyName。每项替换或删除 MUST进入planned/applied diff、同一AnimationClip Undo owner与最终reverse export。

#### Scenario: checkout导出RunLoop Curve

- **WHEN** RunLoop原生Clip被当前Profile或Blend Space可达引用
- **THEN** exporter MUST输出其允许的注册Curve与dependency baseline
- **AND** MUST不导出骨骼曲线或Analysis payload

#### Scenario: Document修改Foot Weight Curve

- **WHEN** AI在dependency baseline与Analysis Input Hash仍匹配时替换Foot Placement Weight完整秒域Curve
- **THEN** preflight MUST通过唯一Clip Curve Mutation校验完整binding、秒域、值域与Registered Curve Hash
- **AND** apply成功后 MUST只使Projection stale，匹配Analysis Input Hash的Foot Analysis Artifact MUST继续Ready

#### Scenario: 组外Clip删除Locomotion Phase

- **WHEN** Document从非Group成员Clip的Curve目标集合移除`presentation.locomotion-phase`
- **THEN** dry-run MUST计划Clip Curve删除并锁定该AnimationClip owner
- **AND** apply MUST删除完整EditorCurveBinding且reverse export MUST省略该channel

#### Scenario: Document修改只读导入子Clip

- **WHEN** Clip分片引用ModelImporter子Clip或dependency baseline已变化
- **THEN** preflight MUST拒绝Mutation并定位精确对象
- **AND** MUST不复制或生成替代Clip

### Requirement: Agent Document必须从正式作者metadata投影完整闭包

Agent Document MUST支持从零创建或完整修改Character Skill与Presentation闭包，包括SkillDefinition、Entry Graph、嵌套Graph/State/Condition、Macro、Skill Timeline、TreeClip、局部Blackboard、Pose Graph、PoseStateMachine、Animation Layer、Control Rig、Slot/Group、Mask、Blend Policy、Animation Producer、Clip Curve与typed引用。Document JSON MUST只表达稳定业务identity、kind、typed properties、logical ports、references、owner和闭包关系。

正式作者类型、字段、引用关系和正式Mutation写入方法上的metadata MUST是上述字段可见性、可写性、类型、端口、owner和闭包规则的唯一来源。Agent Document MUST不维护第二份节点模型、字段表、端口表、Pose模型或owner推断。

Skill字段访问、端口与引用关系 MUST由正式节点定义提供；Document包对象只负责格式转换，不得维护同义作者配置。状态机转移的字段、端口、条件owner与顺序 MUST采用当前整包版本明确的形状，不能因内部payload重构而丢失完整引用闭包。

#### Scenario: Agent从空目标创建完整Skill

- **WHEN** Agent目标包含合法Skill Entry Graph及其完整Graph/State/Condition/Macro/Timeline/TreeClip/Blackboard闭包
- **THEN** checkout、dry-run和apply MUST按同一Document hash校验完整闭包并进入同一事务
- **AND** apply后的正式Skill作者入口 MUST能读取相同的stable identity、owner和引用关系

#### Scenario: Agent修改Skill和Pose的跨域引用

- **WHEN** Agent同时修改Skill Timeline的AnimationSlot引用和Presentation Pose Graph的Slot/Mask组合
- **THEN** dry-run MUST在同一Document hash中核对两个正式owner的引用闭包
- **AND** apply MUST使用与人工作者入口相同的typed Mutation和唯一事务

#### Scenario: Agent提交metadata未声明内容

- **WHEN** JSON目标写入metadata未声明的节点kind、字段、port、引用、owner、Pose空间或Clip Curve channel
- **THEN** strict parser、Reconciler、Validator或Mutation preflight MUST返回稳定路径诊断并拒绝目标
- **AND** MUST不按C#类型名、显示名、Unity序列化字段、SerializedProperty路径或Compiler operation猜测能力

#### Scenario: 正式作者内部实现变化

- **WHEN** 正式作者类型、文件组织或Compiler实现变化但Agent可见kind、typed field、logical port、owner和闭包语义不变
- **THEN** Agent Document schema MUST保持不变
- **AND** Exporter、Parser、Reconciler、Validator、Compiler和原生UI MUST继续消费同一正式metadata投影

## ADDED Requirements

### Requirement: Skill转移目标必须使用唯一Edge归属和稳定顺序

Document v8的状态机转移 MUST使用稳定Edge identity、from/to端点、conditionGraphId、priority、abortPolicy和显式order。order MUST在同一来源节点的转移中唯一；状态节点的固定转移输出为Transfer，状态输入为StateIn，入口与任意状态只提供Transfer，出口只提供StateIn。状态机节点和anchor MUST不接受steps；普通组合步骤及非状态机端口仍遵循各自正式定义。

私有转移条件图owner MUST使用kind=edge、graphId、edgeId和referenceKey=condition，必须指向同图内确实引用该条件图的转移。owner MUST不混用旧步骤identity；共享内容只能采用已声明共享合同，不得把同一私有条件静默归给多条边。值边或非转移边 MUST不接受转移字段。

#### Scenario: 创建只有Edge条件引用的状态机

- **WHEN** Document新增一个状态机及其转移条件图
- **THEN** dry-run MUST校验完整Edge端点、order、条件图role与双向owner关系
- **AND** apply MUST通过同一事务创建全部对象并反向导出稳定identity

#### Scenario: 提交旧状态机步骤字段

- **WHEN** v8目标仍在状态机节点或anchor中提交steps或旧步骤owner
- **THEN** parser或preflight MUST明确拒绝旧形状
- **AND** MUST不自动把旧步骤补读成转移数据

#### Scenario: 删除转移与私有条件

- **WHEN** Document目标删除一条转移及其不再使用的私有条件
- **THEN** plannedDiff与appliedDiff MUST同时包含连线和条件owner变化
- **AND** 保存失败 MUST恢复完整原引用关系，不能留下孤立条件或悬空连线

### Requirement: 包版本迁移必须保留未提交差异并维持单一生命周期

旧版本包与作者资产之间存在未提交差异时，版本迁移 MUST先记录精确root、版本、来源hash与差异；DocumentDirty或Conflict未经裁决 MUST不得被重新checkout覆盖。显式一次性迁移可以读取封存的旧目标并构造新目标，但 MUST进入现有正式Mutation与唯一事务，不得成为正常工具的兼容reader。成功切换后正式入口 MUST只接受v8，非Skill分片业务形状与五工具行为 MUST保持一致。

#### Scenario: 旧包有未应用修改

- **WHEN** v7工作包存在尚未应用的作者修改
- **THEN** 迁移 MUST保留差异并报告需要处理的目标
- **AND** MUST不静默覆盖为新的v8工作包

#### Scenario: 完成新版本往返

- **WHEN** 同一合法v8目标经过dry-run、apply、reverse export和re-checkout
- **THEN** identity、owner与完整Skill/Presentation引用 MUST一致且无修改对账无差异
- **AND** 正常读取 MUST不再调用旧版本reader或一次性迁移器
