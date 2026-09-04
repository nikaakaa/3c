## RENAMED Requirements

- FROM: `### Requirement: Document v4必须原子替代v3`
- TO: `### Requirement: Document v5必须原子替代v4`

- FROM: `### Requirement: Document v4失败恢复必须同时覆盖Unity owner与正式package`
- TO: `### Requirement: Document v5失败恢复必须同时覆盖Unity owner与正式package`

## MODIFIED Requirements

### Requirement: Agent Authoring Document必须是按需生成的持久化目录包

系统 MUST为每个已有合法`CharacterPipelineDefinition`或`AIControllerDefinition`提供唯一确定性`btsmtl-agent-authoring-document.v5`文档包。文档包 MUST位于Unity项目内、`Assets/`之外的`AgentAuthoring/Documents/<domain>/<root-key>.btsmtl/`，并只在显式checkout时从当前正式Unity authoring创建或刷新。文档包 MUST不成为BTSMTL正式真相、Unity资产、Player内容或runtime输入。

#### Scenario: AI首次编辑现有Character Controller

- **WHEN** Agent对已有合法Character root显式checkout
- **THEN** 系统 MUST从当前正式控制配置、技能定义／Graph／局部StateMachine／Timeline、Presentation与可达Clip Curve生成规范目录包
- **AND** response MUST返回唯一文档包绝对路径
- **AND** 系统 MUST不修改或保存Unity资产

#### Scenario: 普通人工编辑期间没有AI会话

- **WHEN** 作者修改Graph、Timeline或AnimationClip但没有显式checkout
- **THEN** 系统 MUST不创建或刷新文档包
- **AND** MUST不触发reconcile、compile、build或publish


### Requirement: 文档包必须分离可编辑authoring、只读context与service基线

文档包 MUST包含service-owned `manifest.json`与`.sync.json`、AI可编辑`editable/`和service-owned只读`context/`。`.sync.json` MUST只保存base source revision、base editable hash与base context hash，不得保存业务authoring。Character Presentation的Profile、Pose Graph、PoseStateMachine、direct Clip Binding、Locomotion Sync Group、AnimationSlot与Policy MUST进入`editable/presentation/`；当前Definition可达原生AnimationClip注册Curve MUST进入`editable/animation-clips/`；Rig资源正文、Body Motion、Foot Analysis generated data、runtime state、Projection与Native Program MUST只进入紧凑只读context或完全省略。

#### Scenario: AI读取Character文档包

- **WHEN** checkout导出Character Controller
- **THEN** editable MUST表达Agent正式可写的控制binding／参数、技能定义／Graph／局部StateMachine／Condition／Timeline／Blackboard、ActionProfile、Presentation与Clip Curve结构
- **AND** context MUST只读表达Node/Graph schema、可引用asset、dependency与必要能力摘要
- **AND** 文档包 MUST不暴露Unity YAML、managed-reference布局或私有SerializedProperty path

#### Scenario: AI尝试修改只读context

- **WHEN** context文件semantic hash与checkout基线不同
- **THEN** parser或Reconciler MUST返回`readonly_context_modified`
- **AND** MUST不把变化降低为Mutation
#### Scenario: 修改代码或运行状态

- **WHEN** Document目标包含C#代码、控制state schema或生成SkillProgram字段
- **THEN** 严格解析 MUST拒绝，相关合同只能作为只读context


### Requirement: 新Graph必须声明正式owner

每个editable Graph MUST拥有`owner.entityId`与`owner.slot`。已有Graph MUST保持stable authoring identity；新Graph MUST使用local identity并引用同文档包内已有或新建owner。系统 MUST不接受无owner Graph、按路径猜owner或以独立Graph asset作为默认私有下钻。

#### Scenario: AI为新State创建body Graph

- **WHEN** AI增加`local:attack-state`并增加owner为该State、slot为`body`的`local:attack-body`
- **THEN** Reconciler MUST先建立State planning symbol再创建inline body Graph
- **AND** apply成功后两者 MUST反向导出为正式stable identity
#### Scenario: 创建技能入口或子图

- **WHEN** Agent提交local技能定义和归属于它的入口Graph／调用子图
- **THEN** Reconciler MUST先建立技能与调用owner再创建图，提交后反向导出稳定identity
- **AND** MUST不接受Character RootTree owner或按目录猜归属


### Requirement: Document v5必须原子替代v4

系统 MUST只接受v5 Document，并删除v4及更早schema、reader、writer、manifest识别和兼容apply。旧工作目录 MUST在正式资产迁移后显式重新checkout，不得自动解释为新技能格式。CharacterController／AIController整包domain和五个生命周期工具 MUST保持唯一。

#### Scenario: 读取旧v4包

- **WHEN** service读取btsmtl-agent-authoring-document.v4或更早格式
- **THEN** dry-run与apply MUST拒绝且不修改资产
- **AND** 调用方 MUST在精确根上重新checkout唯一v5

#### Scenario: 读取v3文档包

- **WHEN** service发现schema为`btsmtl-agent-authoring-document.v3`
- **THEN** dry-run与apply MUST拒绝该文档且不修改资产
- **AND** 调用方 MUST显式重新checkout


### Requirement: Document v5失败恢复必须同时覆盖Unity owner与正式package

Application Service MUST在首次Mutation前解析并锁定全部控制配置、SkillDefinition、技能Graph、Gameplay、Timeline、AnimationClip与Presentation serialized owner，并注册一个完整Undo事务。只有Mutation、全域Validator、Unity authoring保存、最终树反向导出、staging重读与hash校验、正式package原子替换全部成功后，apply才可返回`applied=true`、`saved=true`与`Clean`。任一步失败 MUST恢复全部Unity owner并保留上一份正式package；Character apply MUST不发布Foot Analysis、Program、Projection或Native Pose Program。Clip registered Curve Mutation MUST只改变完整dependency baseline与Registered Curve Hash并使相关Projection stale，不得修改`AnimationClipAnalysisInputHash`或把匹配Artifact标记为stale。

#### Scenario: Clip Curve Validator失败

- **WHEN** Gameplay和Timeline mutation已经执行，但Clip Curve Validator发现Phase非单调
- **THEN** Application Service MUST回滚同一事务内全部Gameplay、Timeline、Clip与Presentation owner
- **AND** 正式Document package MUST保持apply前内容且响应不得报告`Clean`


### Requirement: Presentation分片必须保持整包同步与稳定owner

Document v5 MUST使用`editable/presentation/profile.json`、`editable/presentation/pose-graphs/<graph-id>/graph.json`、对应`layout.json`，以及`editable/presentation/pose-state-machines/<state-machine-id>/state-machine.json`与对应`layout.json`表达Presentation目标状态。Profile MUST表达direct Clip Binding、Blend Space/MM Binding、有限Action producer binding与Locomotion Sync Group；Pose StateMachine MUST只表达Entry、State、Alias、Transition、Rule与Blend，不保存Marker或同步override。Profile binding、Pose Graph Source Slot与AnimationClip MUST通过包含asset GUID、有符号且非零local file id和一致asset path的结构化对象引用表达。新建子资产 MAY使用`local:*`，AnimationClip MUST不允许local identity。分片 MUST通过稳定owner identity互相引用，并继续服从整包checkout、hash、dry-run、apply、Conflict与反向导出语义；不得提供文件级apply、旧单文件reader、按显示名解析或缺失local file id fallback。

#### Scenario: AI只修改一个Clip Binding

- **WHEN** 一个Pose Graph ClipPlayer改为引用另一个既有Source Slot且Profile Binding引用另一个原生Clip
- **THEN** dry-run与apply MUST锁定整个Document包及精确Profile/Pose Graph/Clip owner
- **AND** 反向导出 MUST更新整包基线与规范对象引用

#### Scenario: AI创建Profile binding子资产

- **WHEN** editable使用`local:*`声明新的Profile-owned binding并引用既有Source Slot与原生Clip
- **THEN** Reconciler MUST生成typed子资产创建、Profile数组更新和资源配置Mutation
- **AND** apply成功后reverse export MUST发布正式GUID与local file id引用


### Requirement: Presentation Reconciler必须调用唯一Presentation Mutation

Document v5 Reconciler MUST按owner依赖生成类型化Presentation Mutation计划，并与人工编辑共用validator、资产级transaction、子资产identity allocator、dirty owner与诊断。Source Slot、direct Clip/Blend Space/MM Binding、Locomotion Sync Group、Pose Graph和PoseStateMachine的创建、修改、引用与删除 MUST在同一个正式资产事务中处理；Reconciler MUST不直接写Unity YAML、SerializedObject path、generated Projection或第二份字符串binding。

#### Scenario: apply新增Clip Source Slot与binding

- **WHEN** 文档目标状态新增Graph-owned Source Slot、Profile-owned direct Clip Binding并让ClipPlayer引用该Slot
- **THEN** Reconciler MUST按子资产创建、binding配置、Player引用与owner保存顺序生成类型化Mutation
- **AND** 任一失败 MUST回滚全部子资产、数组、节点引用、Gameplay、Timeline、Clip与Presentation变化

#### Scenario: apply修改Locomotion Sync Group

- **WHEN** 文档目标状态调整Group中的原生AnimationClip成员
- **THEN** Reconciler MUST使用结构化Clip引用生成Profile Mutation并校验成员唯一性
- **AND** MUST不修改Clip Curve或自动Build Projection


### Requirement: AnimationClip注册Curve必须使用独立严格分片

Document v5 MUST只为当前Definition闭包中实际可达且位于可写原生`.anim`的AnimationClip输出`editable/animation-clips/<stable-segment>/curves.json`。分片 MUST包含结构化Clip对象引用、完整dependency baseline、只读`AnimationClipAnalysisInputHash`和Clip Curve catalog允许的秒域完整canonical Curve目标集合；从目标集合省略已有channel MUST表达删除。可达Clip的Foot Weight删除和仍为Locomotion Sync Group成员的Phase删除 MUST被Validator拒绝。分片 MUST不包含骨骼Curve、AnimationEvent、import设置、Rig、Foot Analysis Artifact、Phase Validation samples、Group或generated plan。Exporter、strict parser、Reconciler、handler、Validator与reverse exporter MUST复用同一Clip Curve capability，并按完整`EditorCurveBinding(path + type + property)`识别channel，不得只比较propertyName。每项替换或删除 MUST进入planned/applied diff、同一AnimationClip Undo owner与最终reverse export。

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


## ADDED Requirements

### Requirement: 技能定义分片必须进入唯一完整Document事务

CharacterController v5 MUST允许editable/skills/<canonical-id>/definition.json表达技能identity、ActionProfile、入口图、参数签名与依赖；控制配置只允许已登记模块binding、合法作者参数和技能引用。允许文件集合、local身份、Exporter、strict parser、Reconciler、Mutation、Validator、hash与reverse export必须共同覆盖新增内容。角色RootTree、C#正文、运行状态及生成数据 MUST不可写。

#### Scenario: 一次新增技能及嵌套图

- **WHEN** 目标包同时包含local技能、入口、子图和Timeline
- **THEN** dry-run MUST建立完整依赖计划并锁定同一hash
- **AND** 任何分片失败 MUST使整包apply失败，不能提交半个技能

#### Scenario: 未知技能文件

- **WHEN** editable/skills出现未声明文件或非法canonical目录
- **THEN** strict parser MUST拒绝，不能按目录前缀全部放行
