# Pose UE式作者组织：实施状态与历史记录

## 当前状态（2026-09-10，对账修正）

本提案不能按49项全部完成验收。此前把Capability注册、Compiler内部operation支持和一次旧作者拓扑的Character Build误判为完整UE式作者架构；当前任务状态见tasks.md，其中2.5、4.2、4.4、7.3、7.5、9.2仍按实际缺口跟踪，9.4已由当前v29正式产物发布完成。

已经完成的边界仍然有效：直接AnimationClip Player、Animation Slot与Slot Group、有限Action Timeline的Slot／Section／片段混合字段、Control Rig输入与FBIK展开、独立Pose输入合同、CharacterAnimationBuildContracts.CreatePoseOnlyInput()、CharacterSimulationBuildOrchestrator的animationBuildInput.CreatePoseOnlyInput()、Document v7闭包以及旧迁移器删除。

当前精确Corin根图已通过正式Document事务变为Locomotion Pose State Machine → 通用 Inertialization → Full Body Action Slot → Control Rig → Output Pose，共6个节点、5条Pose边；Locomotion↔Turn两条转换使用Inertialization，其余转换保持StandardBlend。Corin没有Motion Matching或其它多源Selection source，因此没有强行创建显式BlendStack；Full Body Action Slot的内部StackPolicy只负责Action endpoint，不能冒充Locomotion BlendStack。Corin也没有真实UpperBody Overlay，所以暂不实例化Layered Blend Per Bone；Layered capability与Local／Component空间编译链已补齐。

当前面板的IdentityReference详情已接入Animation Channel、Animation Slot、Pose Graph、Linked Pose、Presentation Fact、Gameplay State和Pose History的正式选项源，提交ad8f4ae1e、8f961a9c4和c60d12591均已通过Unity MCP脚本编译；Applied Values在没有Play／Preview target时仍会等待运行角色，这是运行观察状态，不应被误写成作者资源缺失。

本次正式Document apply由Unity MCP完成根图重组与回滚两轮事务；最终checkout返回syncState=Clean、plannedDiff=[]、sourceRevision=e674554c9c9cff6542ddb960f87e216ab0785c27c016d69c1fae77bea576f7b0、documentHash=3217042941faa9c871b356bf7a6629a912b73116fb05f170d2c32e7932d46d7b。Action外部惯性化事务虽然成功写入过，但因Pose-only route contract缺失已被正式回滚，最终 authoring 以6节点/5边为准。

随后通过Unity MCP重新发布了Corin Float32与Fixed products，二者共享Projection revision `e88957af0b8628f4d217d9dd769a1b188a8b50cc6b955a5f2abab6a499e16e0a`，生成的Pose Program Image已写入`character-presentation-pose-plan/v29`与`character-presentation-pose-runtime/v29`。Float32 wrapper与Fixed wrapper均返回成功；生成资产属于Build输出，未进入本轮代码提交。

曾尝试把FullBody Action Slot后的动作请求接入第二个通用Inertialization节点。正式Build证明当前`CreatePoseOnlyInput()`不携带完整Character的ACL Action producer endpoint，导致Action Slot route matrix在Pose-only阶段没有可用惯性route；继续硬接会破坏Pose-only与完整Character的隔离。该拓扑已通过Document dry-run/apply正式回滚，Slot-owner compiler/runtime扩展保留为后续独立Action route contract的实现基础，当前Corin仍由Slot内部Action route处理。

现有character-animation-blend-stack、character-animation-transition-routing-module和Presentation authoring spec已经明确要求这些边界，本轮对账没有修改spec。没有新增测试代码；端到端行为仍由作者验收。

## 实施增量历史（2026-09-09）

以下内容记录方案实施过程中的阶段性判断；与上面的当前状态不一致的“尚未完成”描述均只属于当时，不代表当前代码、资产或正式产物。

已为Pose Canvas Graph加入持久化作者角色合同：AnimGraph、Animation Layer、State Pose、Transition Rule、Control Rig、Subgraph和Linked Pose Entry。作者工作区、调参入口和编辑写入会优先读取图自身角色；没有角色字段的旧图继续按现有状态图关系推断。Capability补充了Animation Layer与Control Rig的Document Role，并扩大现有Pose能力在这些角色中的合法范围。

这只是作者角色基础，不代表Player直接资源、Timeline Slot／Section、Control Rig展开、Document v7、Corin迁移或最终Build已经完成；tasks仍按49项实施清单统计。

随后将Action Playback Input、Pose Parameter Resolve和Goal Assembler标记为编译器拥有的Capability。它们仍可被旧资产和唯一Compiler识别，但不再出现在新作者节点创建菜单；编辑写入入口也会拒绝直接创建这些内部节点。Pose Bone IK Goals没有被误标为内部节点，仍是Control Rig目标作者能力。

已在Animation Rig Definition中增加正式Animation Slot目录。每个Slot拥有独立Slot identity、Slot Group identity和作者显示名；同一Group允许多个Slot，共用Group只表达动作互斥，不与IK Effector或内部AnimationChannel混用。Rig校验会拒绝缺失或重复Slot identity，后续Corin迁移时把现有动作入口写入该目录。

Pose Slot节点的Rig校验已接入该目录：Slot identity不再只依赖Pose节点字符串和编译期路由，必须能在当前Rig的正式Slot目录中解析；现有Corin旧资产在完成迁移前会被明确判为未迁移，而不是自动补默认Slot。

编译后的Animation Slot descriptor已携带Slot Group identity，Program Image seal会把Group纳入产物身份，运行观察也会返回Slot与Group的分离结果。这样动作互斥组不会再只能从Slot名称或AnimationChannel反推。

Clip Player的有效播放策略已进入作者Payload、编译Descriptor和运行时Clip Player：`Loop Animation`不再读取导入资源的循环标记，运行时按该次Player usage决定有限或循环时间。该字段仍暂时与旧Source Slot共存，直接AnimationClip引用和Source Catalog去重尚未完成，因此不能勾选2.2或2.3。

同时将该策略纳入Clip Player编译器版本和Program Image seal identity，避免只改运行时字段而复用旧产物。旧v4 Clip Player产物会因版本不一致进入Stale，等待后续统一迁移与重新发布。

Clip Player作者字段已改为直接`AnimationClip`资产引用，Source Compiler增加了按结构化Clip引用分配独立source index的入口，Pose Binding Pass也能消费该direct index；旧Source Slot只作为待迁移旧Payload保留。直接Clip的Foot Analysis键和Corin资源迁移尚未完成，当前不能宣称2.2、2.3已完成。

Direct Clip source已经贯通唯一Source Compiler、Foot Analysis binding key、Pose Compilation Request、Family Binding Pass和Clip Player source plan。直接资源不再通过Profile Pose Source binding查找；其分析键使用Clip的GUID与local file id。当前还需要在Corin迁移前完成已有分析artifact的对应重生成，并移除旧Source Slot分支。

2026-09-09通过Unity实例`e852139597e42532`请求脚本编译。首次编译发现Direct Clip字典的`AnimationClip`类型歧义和Foot Analysis解析器缺少Editor命名空间，已修复；Domain Reload完成后同一实例重新注册，Console错误数为0。该结果只证明当前脚本可编译，不证明新作者资产和Build已完成。

随后扩展Direct Blend Space与Animation Layer图角色，Unity实例`e852139597e42532`再次完成脚本编译；Domain Reload期间连接暂时断开，恢复后Console错误数为0。当前仍未执行资产迁移，故编译成功不等于Corin产物可发布。

Timeline动画轨道增加了可选外部Slot identity。BTSMTL只保存稳定字符串，避免通用Timeline反向依赖Character模块；Character Semantic IR和Projection编译阶段将其解析为typed `AnimationSlotId`。Unity实例`e852139597e42532`编译通过，Console错误数为0。

Timeline的Slot identity已进入`TimelineAnimationContribution`与`CharacterPresentationAnimationClipBinding`，使原Action Timeline的动画片段能够在同一owner中携带Slot来源；仍未添加第二Montage资产或第二时钟。Unity实例`e852139597e42532`在Domain Reload后编译通过，Console错误数为0。

有限Action Timeline的Section增加了`NextSectionId`及owner内目标校验，并提供同一Timeline上的Section跳转配置入口；Section仍复用原Timeline时钟和生命周期，没有新增Montage游标。Unity实例`e852139597e42532`编译通过，Console错误数为0。

Timeline字段扩展后再次通过Unity实例`e852139597e42532`脚本编译，Console错误数为0。当前Section的运行时跳转命令、Montage混合Profile和Corin动作轨道迁移仍未完成。

Blend Profile identity已归入Timeline Animation Clip而非Track，动画贡献和Presentation Clip Binding各自携带片段级设置；这是Montage式Blend设置的正式owner。Unity实例`e852139597e42532`编译通过，Console错误数为0。

Timeline Animation Clip的Blend Profile identity已继续写入Semantic动画operation字段；Slot identity和Blend Profile identity保持两个独立设置owner。Unity实例`e852139597e42532`编译通过，Console错误数为0。

Linked Pose调用已正式开放到Animation Layer，Layer图也进入Pose IR的图角色和边界校验；Layer仍复用现有Implementation／Group／call-site运行状态，不创建独立Layer Executor。Layer图按GraphInput／GraphOutput边界编译，内部状态机、Slot和骨骼混合仍走同一Pose计划。

Direct Clip Player的作者命令已改为直接打开／定位AnimationClip，不再提供Profile Source Binding入口；旧Source Player仍使用旧命令，直到剩余Player类型完成同样的资源合同迁移。

Direct Blend Space Player的作者命令也已改为直接打开／定位Blend Space资产；Blend Space source plan可由直接资源生成，旧Profile binding仅保留为迁移输入。Unity实例`e852139597e42532`编译通过，Console错误数为0。

Direct Blend Space source编译已去除临时ScriptableObject Slot／Binding，Source Catalog直接保存Blend Space引用并分配source index，避免编译重复执行时泄漏Unity对象。Unity实例`e852139597e42532`再次编译通过，Console错误数为0。

Control Rig现在是独立的Pose IR图角色，拥有自己的GraphInput／GraphOutput边界和Document Role；Control Rig图内的Foot Placement、Pose Bone IK Goals与Full Body IK仍交给原唯一Compiler和FBBIK后端展开。Unity实例`e852139597e42532`编译通过，Console错误数为0。

Timeline动画片段增加了独立的Blend Profile外部identity，并将其写入Timeline贡献、Presentation Clip Binding和Semantic动画operation；该identity仍等待Rig/Blend Profile正式目录解析，尚未进入动作运行时混合计算。

Rig Definition现拥有Blend Profile目录，并按ProfileId、RigId和RigRevision严格校验Timeline片段的Blend Profile identity；Producer不会按显示名或AssetDatabase猜测Profile。Unity实例`e852139597e42532`编译通过，Console错误数为0。

Timeline动画片段的Blend Profile identity已进入Semantic动画operation的字段集合，作为动作片段自己的混合设置来源；当前动作运行时仍需把该identity接入既有Transition Routing的请求选择，不能在Slot中新增一份Policy。

Timeline Track inspector已显示可编辑Slot identity；Section inspector已显示Next Section选择，并通过TimelineData的owner mutation保存和校验。Unity实例`e852139597e42532`编译通过，Console错误数为0。

Timeline动画片段带有Slot identity时，Producer编译会严格向Rig Slot目录解析并拒绝悬空引用；Slot与Blend Profile仍各自保留在对应owner。Unity实例`e852139597e42532`脚本编译通过，Console错误数为0。

盘点确认当前Assets没有`SelectedPosePlayer`作者节点；该旧Source Slot入口已标记为Compiler-owned并从新作者创建菜单隐藏，避免继续生成重复Profile Source Binding。已有Motion Matching Pose直接能力和旧资产读取定义保留到最终迁移清理。

Selected Pose旧入口退役后通过Unity实例`e852139597e42532`脚本编译核验，Domain Reload完成后Console错误数为0。

新增一次性Editor迁移入口`CharacterPoseDirectPlayerMigration`：它从精确Corin Definition解析旧Clip Player的Source Slot→Clip关系，准备直接AnimationClip Payload，并为Rig写入`corin.full-body-action` Slot/Group及现有两个Blend Profile引用。入口仅由显式菜单触发，使用Undo事务回滚；尚未执行，等待全部作者代码和Document v7完成。迁移代码已通过Unity实例`e852139597e42532`编译，Console错误数为0。

2026-09-09执行该一次性菜单后，Corin的7个Clip Player已实际写入直接AnimationClip；随后重跑同一菜单完成旧Source Slot与Profile Pose Source Binding清理，当前Pose资产和Profile均为空旧Source目录，Rig包含`corin.full-body-action` Slot/Group以及两个现有Blend Profile。动作根图的旧Action Playback Input、Parameter Resolve、Goal Assembler尚未重组，故9.2和最终发布仍未完成。

在Timeline、Rig Profile目录、Direct Player和Control Rig角色增量后，Unity实例`e852139597e42532`完成一次完整脚本编译，Domain Reload恢复后Console错误数为0。当前验证仍限于脚本编译，未把资产迁移或Character Build写成完成证据。

## 本轮确认的作者分工

| 内容 | 已确定的目标 |
|---|---|
| AnimGraph | 组合状态机、动画层、Slot、骨骼混合、显式惯性化及Control Rig引用。 |
| Animation Layer | 复用现有Linked Pose体系，可以封装状态机、Slot及按骨骼混合；不会自动获得Mask或私有播放实例。 |
| Montage职责 | 由现有有限Action Timeline承担；原位补Slot轨道、Sections及动画Blend设置，不新增Montage资产或第二时钟。 |
| Slot与骨骼范围 | Slot负责动作插入；Mask／Branch Filter与层Alpha决定骨骼混合。Layer实例共享组与Slot Group分开。 |
| 状态与动作混合 | 转换边拥有状态混合设置，各Action Timeline拥有自身动画进入／退出设置，Blend Profile作为Rig关联配置引用。 |
| Control Rig | 组织控制目标、Foot Placement和FBIK；内部Goal组装由Compiler展开，现有算法、Native／Job及唯一Writer保留。 |
| Player与参数 | Player直接选择资源或typed资源参数；内部Source binding与默认参数汇总退出作者界面。 |
| 迁移 | 新组织确实改变作者数据，代码完成后才迁移Corin；Timeline动画片段和玩法窗口仍在原owner，无法无损转换的旧策略明确报告冲突。 |

## 已有基础与尚待实施的区别

已有FlowCanvas原生编辑、typed Mutation、flat catalog、状态／条件详情、v28来源元数据及普通Play观察代码可以复用。这些基础并未实现上述完整图角色、层内Slot、Timeline的Montage式设置、直接资源Player或Control Rig作者合同。

新范围的实施必须沿tasks逐项推进，不能沿用旧任务编号和已完成标记。检查、运行、性能和发布记录只放在本文件，不写成tasks里的验证或验收任务。

现行spec与新设计的冲突及同步方向见design.md第12节。当前规范中的显式Assembler作者拓扑、Source Slot双重绑定、惯性化直接owner限制和Document旧版本不能被静默忽略，也不能当成新方案已经实现的依据。

## 提交与产物记录的适用范围

| 提交／记录 | 范围 |
|---|---|
| `149273005`、`5b529238a` | 旧作者模型上的选择、详情、导航和编辑反馈修正。 |
| `b16397bee`、`70ee5b50f` | v28来源元数据、条件读取记录、状态／别名详情等可复用基础。 |
| `52690a462` | 旧UI范围下撤去不必要迁移及当时Build失败的记录；不约束新组织的数据迁移。 |
| `7fe744de9` | 重写原提案的8份规划文档，确立UE式作者分工及Timeline承接Montage职责；没有实施新代码。 |
| v27发布成功、v28发布失败 | 都是旧阶段记录，不证明新组织已构建或可以运行。 |

## 历史记录（旧UI接入范围）

以下保留当时的提交、哈希、命令结果和问题经过，供追溯。其“本轮”“当前”“最终”等表述均指对应历史阶段，不代表今天的任务状态或实时Unity状态。历史错误没有在本次文档更新中重新检查；不能据此断言它们现在仍存在，也不能把旧Console零错误当成新方案通过。

### 旧范围的v28发布尝试

用户确认“开始”后，调用正式`character.build_fixed_products`，目标Definition为`Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset`，Fixed wrapper为`Assets/Configs/Simulation/DeterministicRollback/Programs/CorinFixedProgram.asset`。该入口统一发布Float32、Fixed和共享Projection，不重复启动两次Build，不修改作者数据。

实际返回`success=false`、`character_build_failed`，失败阶段为AuthoringDiscovery。三条`skill_entry_graph_missing`分别对应Attack、DodgeBack、DodgeForward：它们的技能入口尚未进入Definition正式SkillGraphs。遵守用户不处理其它领域报错的范围，不修改技能配置、不绕过共享编译链，也未将Build任务勾选完成。

构建前后读取Definition、Pose作者资产、Float32 Program、Fixed Program及Presentation Projection的SHA-256，五个文件均未变化。原始结果为`.codex-tmp/canvas-core/pose-v28-build-result.json`，前后记录为同目录`pose-v28-build-before.json`和`pose-v28-build-after.json`。这次没有发布新产物。


### v28代码阶段的接口改动

| 作者操作／输入 | 实现与结果 |
|---|---|
| 修改状态名称或进入重置策略 | State Capability → Details → SetPoseStateFieldMutation → 原状态owner；保留StateId、子图和转换引用。 |
| 修改状态别名名称和成员 | 共享别名详情 → ConfigureStateAlias → 既有ConfigurePoseStateMachineMutation；成员使用正式State／Alias identity，空成员、未知成员及循环引用进入原事务约束。Document继续保存原aliases正文。 |
| 修改节点名称、普通参数或策略参数失败 | Mutation失败返回到当前详情，重新读取正式值后显示错误；不再以“已处理”吞掉失败。转换duration请求也进入正确的SetTransitionField分支。 |
| 保存图和调参策略 | 显式保存序列化图、根owner、Profile及当前图引用的Foot／FBBIK／Blend／Inertialization调参owner；不调用全项目SaveAssets或Build。这里只补实现，本轮未触发保存。 |
| 修改状态、转换或条件 | 版本变化后刷新同一原生文档视图，更新标题、转换标签及端口，保留仍存在的NodeId／EdgeId选择。 |
| 查看转换条件运行结果 | Rule Compiler保留作者OperationId；正式求值记录实际输入读取位，完成结果带编译状态机NodeId、作者OperationId、ReadInputA／ReadInputB。只读projector按当前调用和转换匹配，当前条件与目标预判分开选择。 |
| And／Or短路 | 只有RequireBool实际读取时才记录输入位，保留原短路语义；UI显示本次未读取、未采集、False及0各自含义，边高亮来自ReadInput记录。 |
| 修改子图或Linked图后继续观察 | Source Map保留每张图GraphRevision，窗口核对所有相关作者图版本、PlanHash和Rig版本，停止版本失配结果叠加。调参变化记录同时覆盖Linked作者图。 |

运行算法仍属于现有Native／Job及状态机求值链。本轮运行层只增加诊断来源字段和读取标记，没有新播放器、假输入或Editor侧求值。Pose Program Image为v29，Runtime ABI为v29，Transition Rule合同为v3；旧v28产物不会被当作新字段齐全的产物读取。

共享请求和UI合同没有新增Agent Document版本：状态name和aliases原本已在v5中，Editor和Document最终进入相同Presentation Mutation owner。原生角色绑定、完成帧租约、Pose Watch容量、关闭／退出／重载解绑沿用既有实现。

本轮代码提交：`b16397bee`提供条件观察来源、读取标记与共享别名展开；`70ee5b50f`完成作者详情、条件显示、版本绑定及保存owner接入。别名展开算法从编译器移到唯一CharacterPoseStateAliasResolver，配置请求在提交前调用，正式作者约束和编译复用同一实现。

代码检查记录单独放在本节：Unity刷新过程中发生过Domain Reload断连，随后恢复，最近一次Console读取为零错误；未新增测试，未执行回放、业务asset apply或Character Build。OpenSpec格式检查通过。此记录不占tasks项目，也不代表用户已完成端到端验收。


### 基线与归属

- 提案：`integrate-pose-flowcanvas-editor-preview`；目录：`D:/Unity_Project_1/3C`。
- 本轮读取的HEAD：`f74e9b2aba6d4d9d4476b48933b5fe01cfb29b2c`。工作区存在其它任务及此前Pose准备改动，不是干净checkout。
- 精确Definition：`Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset`。
- 作者资产：`Assets/Configs/Character/Corin/Pipeline/Presentation/PoseGraphs/CorinPresentationPoseGraph.asset`。
- 现有Program／Projection为Definition同目录Generated中的正式资产。本轮没有重建它们。

| 文件 | 本轮读取时SHA-256 |
|---|---|
| Definition | `005e281c11a2993e3256a2b9b5064cd17bf498720897520a7c7bb23a42856768` |
| Pose作者资产 | `2766503f845a89d7b743a2bd29d4381d3650abbd8a026c55fde35305920e7a39` |
| PresentationProjection | `a14e8124db25d26ad784e73d587f5529f947dc870cd2648136d69f827e983b1f` |
| SimulationProgram | `f5bf7bc97e38da263afdd2e339c6067996fdcabb8a824fcaeefd49bfc8c7ff76` |

本机读取记录：`.codex-tmp/canvas-core/pose-editor-apply-baseline.json`。这些哈希只是区分工作区内容，不证明行为或性能通过。

| 区域 | 本提案职责 | 技能侧边界 |
|---|---|---|
| Pose作者Graph／Node／Connection、窗口和详情 | 原生UI、同一Mutation、图闭包和Play观察 | 不由技能迁移替换 |
| Pose Compiler及Source Map | 直接消费同一作者图、增加必要观察映射 | 技能Semantic IR／发射仍归技能侧 |
| 共享作者合同 | 复用已分离的TreeDesigner.Authoring类型及唯一Port Shape | 不复制第二份合同，不改变技能业务 |
| FlowCanvas／CanvasCore钩子 | 最小领域无关的原生创建／改接和外部观察接入；修改前核对文件 | 技能侧复用同一钩子，实际同段冲突交给作者决定 |
| Document | Pose owner与现行业务分片适配 | 技能Macro正文继续由统一v7协议承接；不创建协议分支 |
| Runtime | 保留Program Image、Native页、Worker、Source／Constraint／Final Publication | 不启用FlowCanvas角色执行，不改技能runtime |

该历史阶段的用户指令允许刷新Unity检查当时的接入。只修复Pose／FlowCanvas接入造成的错误，其它领域错误记录但不修改；若阻止程序集加载，明确报告验证受阻。整根Build与回放仍按收口阶段实际条件执行，未执行不勾选通过。

### 历史证据的使用边界

此前已完成一次空节点清理、正式Document apply和Float32 Build，原始日志位于`.codex-tmp/canvas-core/`。当前源码已有后续变化，因此这些日志不能代替本提案的修改后验证。当前仍没有本提案的作者交互、同输入回放或观察开销闭环证据。

### 本轮代码与验证记录

代码提交 `41ac26f1a`。本轮继续实施，不视为全部任务完成，不归档。

- Graph、Node、BinderConnection 已使用原生 FlowCanvas 基类。保持原有序列化类型名、稳定 ID、GraphCatalog 与 Payload；编译器直接消费它们，没有作者图中转或原生 getter 求值。
- 原生创建、连接、改接与批量断口进入 typed Mutation；布局移动原地修改并由真实 Graph owner 记录 Undo，选择与重绘不标脏。
- 状态／规则页面也使用原生 FlowGraph / FlowNode / BinderConnection 的不持久化视图；删除旧自绘端口和拖线器。
- 独立窗口播放器、假速度／Grounded 输入、Seek 和预览角色面板已移除。Profile 入口按正式 Definition 引用解析，不再要求 Preview Fixture。
- 普通 Play 通过运行目标 Registry 精确绑定 Host.Definition 与 Pose 资产；窗口各自拥有兴趣 ID，只有唯一匹配实例才自动选择。下钻利用 Compiler 同一 Scope 函数及已发布 SourceMap 定位；多调用不混合结果。
- 原生节点与端口悬停只读取 10 Hz 更新得到的显示文本。Pose Watch 复用正式接口，每窗口上限 8 项；不调用 Job.Complete，不读 Pending 页，不执行作者 getter。
- 输出端口元数据新增到 Pose Plan v27，含作者节点、调用范围、端口种类和工作区索引。没有边读取证据时不播放边动画；未采集值不显示成零。

#### 已完成的正式 Document 检查

同一精确 Corin Definition 的 checkout 与未修改正文 dry-run 都成功，`syncState=Clean`。dry-run 的 `plannedDiff=[]`、`touchedOwners=[]`、`diffSize=0`。

- SourceRevision：`5f692b49fd460f45732b1fb707a33afda75c4493c89bafeff7eeabe532577479`
- DocumentHash：`2562bc4d067334f8016bd326210ff48cdc24cdc98c1ef34e759130d9270836f2`
- PlanHash：`4f53cda18c2baa0c0354bb5f9a3ecbe5ed12ab4d8e11ba873c2f11161202b945`
- Package：`3cDemo/Client/3C_Client/AgentAuthoring/Documents/CharacterController/c7a7c1e3-001dd30a08d99da6.btsmtl`

无业务修改，因此上述动作是 `applied=false / saved=false`，不能写成资产 apply 成功。它证明的是导出／对账没有伪修改，不是交互操作全部验证通过。

#### 编译和构建证据边界

本轮 Unity 刷新中出现的 Pose 接入编译错误已修正，之后 Console 返回零错误。一次中间 v26 Build 在 Editor.log 记录编译 89509 ms、发布及收尾 15764 ms、总计 105273 ms；产物中实际读取到 v26 与 25 条作者节点 SourceMap。它不替代最终 v27 构建验证。

Editor 在程序集重载期间曾无响应并导致 CLI 超时，随后自行恢复；未关闭或重启用户 Editor。最终 v27 Build 及运行检查结果继续在下方登记。

#### 最终 v27 Build

正式 `character.build_float32_products` 返回 `success=true`，消息为精确 Float32 Program 与 Presentation Projection 已发布。结果对应同一 Corin Definition：

- ProgramHash：`847605d62325acc923ac06f70ac685bc058e0c28f96eedc7976b4d7ab077dd92`
- ProjectionRevision：`d97b4d79323aefbf04df5a98d41c12421d5b27afabcb3c0d172de6e609b659aa`
- SourceRevision：`c8a078e38b09eaa4457f38b781f168b54efbb0a0d590872d9db7d21f98d0f16c`
- NumericProfile：`float32-ieee754`；Target ABI：8。
- 实际产物 `m_SchemaVersion=character-presentation-pose-plan/v27`，25 条 SourceMap、24 个输出端口来源、8 个调用范围。来源摘录为 `.codex-tmp/canvas-core/pose-v27-source-map.json`。
- Build 信息只有 Float32 常量舍入与 ABI 说明，没有构建错误。生成资产此前已有工作区修改，本批未把这些混合资产改动整包提交。

#### 观察生命周期

| 条件 | 窗口行为 |
|---|---|
| Edit Mode | 显示未播放，允许作者编辑，不创建角色 |
| Play 且没有匹配 Host | 等待运行角色 |
| 同 Definition 唯一匹配 Host | 自动绑定 Registry 的具体 RuntimeInstanceId |
| 多个匹配 Host | 等待明确选择，列表带实例 ID 摘要 |
| 目标有效且产物版本一致 | 读取完成帧，10 Hz 更新纯显示文本 |
| 作者／产物版本变化 | 停止旧值叠加，显示版本不匹配 |
| Unity Pause | 不推进时钟，读取最后完成帧 |
| 目标移除、退出 Play、关闭窗口、脚本重载 | 解除该窗口 Diagnostics 与 Pose Watch 兴趣，不缓存 Native 页 |

真正显示的数据包括已完成节点可用性、权重、完成帧、已采集参数、已订阅 Pose Watch；输出端口值从 SourceMap 指定类型与索引关联。没有边读取采集时不画执行动画。上表为实现路径说明，不能替代每种场景的实际运行覆盖证据。

### 用户截图暴露的未完成项与新的执行顺序

用户指出侧栏挤占、内容重叠、节点缩放过小，以及未使用原生子图导航。此前的编译、Document、Build 记录不代表作者界面可用；作者 UI 验收没有通过。

用户明确要求：先完成代码和 UI，再迁移资产。自该指令起不再执行资产迁移、业务保存或 Build；只做代码修正与 Editor 刷新。

本次修正侧栏为 320 点宽、详情／图目录／运行观察页签，工具栏换行，默认 100% 阅读缩放；删除自定义 Breadcrumb Host 和 PageStack，Pose、状态及规则进入沿用原生画布子图导航。状态页面绑定端口类型从声明读取，不再依赖另一端端口已注册，修正已定位的空引用。

普通 GameplayLab Play 曾被其它领域错误阻止：`ProgramStateSemantic=144` 在 Fixed Program 读取中不合法，调用链为 `SimulationProgramSemanticsCodec → CharacterSimulationProgramCodec → FixedCharacterSimulationProgramAsset → DeterministicRollbackSessionSourceDefinition → GameplayLabBootstrap`。按用户要求未修改该领域；未因此绕开正式启动或创建替代角色。正常角色运行观察和性能对照尚无通过证据。

#### 截图后的代码验证状态

修正后再次读取 Console 为零错误，并通过正式菜单重新打开 Corin 图。UXML 中代码要求的所有区域均存在，自定义 Breadcrumb Host / PageStack 引用数为零。原生双击入口为 `Node.TryOpenEditorChild → GraphEditor.OpenEditorChild → Graph.SetCurrentEditorChild`，返回沿原生画布面包屑；Pose 状态／规则页面仍从唯一作者 Document 建立不持久化视图。

作者反馈新版已经能看，但明确认为只达到可读，UI 仍未完成，不能据此写成整体视觉验收通过。后续仅继续代码修正，资产迁移与最终 Build 放在代码和 UI 完成之后。

#### 属性编辑与选中卡顿修正

用户后续要求按 AnimGraph 作者习惯整理属性，不展示空运行状态和无用元信息。代码新增领域结构化字段编辑器接口，Pose 提供参数策略列表与 IK 效应器绑定的具体控件，提交仍进入现有 typed Mutation。默认 Pose 详情采用 authoring-only 展示，运行目标控件移入运行观察页；默认不渲染 Runtime Inputs、Applied Values、空 References 等栏目，状态转移保留可编辑策略和条件图入口。

选中路径原先重复调用 `SimulationProgram.Load → Codec.ReadArtifact → Projection.Load`。新增按 ProgramHash 和 ProjectionRevision 绑定的窗口级只读缓存，同一版本的重复选择不重新解码；失败同样按版本记录，不在每个选择事件重试重加载。没有修改 Runtime 执行或引入替代产物。

原生曲线命中范围原先只覆盖端点矩形，已改为覆盖控制点包围范围，并用连续线段距离判断；状态转移增加方向箭头和双击条件图入口。原生面包屑在领域侧栏模式下不再因为有选中对象而隐藏。

这些修正尚无完整交互通过证据，资产迁移仍未继续。

#### 第二批代码提交与转移详情故障

代码提交：`149273005`（35 个文件）。用户已经确认节点选中卡顿改善；其后截图仍显示状态转换选中后没有策略，不能标记交互闭环。

实际堆栈进一步定位到 `Graph.UpdateNodeBBFields`：新建的只读文档视图未经历序列化收集，GraphSource 的 allParameters / allTasks 尚未初始化，导致切换在详情绑定前中断。已在 GraphSource 建立集合初始化不变量；不是跳过校验或返回备用图。刷新后当前 Console 无错误。

页面绑定现在以原生 currentGraph 为依据，构造和绑定阶段不发布中间选中状态。每份状态／规则文档拥有独立视图及绑定；状态转移对象按正式 EdgeId 复用，参数变化不会重建选中身份。原生导航按 NodeId / EdgeId 还原路径与选择，使用同一 editorObservation 接口提供只读绘制数据。

编辑入口核对：根 Pose 图创建／连接／改接／字段写入进入 CharacterPoseCanvasEditorWriteSession 和 typed Mutation；状态转换改接使用既有 source / target-state-id Mutation，保留条件与混合配置；规则改接采用一次批量断开／连接。剪贴板遵守现有 Copyable 与文档边界，不给不支持的页面提供可执行的复制入口。

尚未完成的证据：完整编辑交互覆盖、资产最终往返、正常角色运行观察与观察开销对照。用户要求继续完成代码后再处理资产，当前没有继续迁移或 Build。

#### 2026-09-08：目录导航、详情写入与观察调用定位

本批修改由当前任务单独实施，没有新增测试或迁移业务资产。

- 目录原先直接打开目标图，丢掉父页面。现在从唯一根图读取状态机、状态和Subgraph调用关系，使用相同的原生NodeId进入链导航；Corin目录的Idle入口会经过Root Pose Graph、Locomotion State Machine、Idle。共享图多处调用时显示调用路径菜单。未被根图引用的目录图仍明确作为独立编辑根打开，不伪造调用来源。
- 详情中的OpenChildSurface也进入GraphEditor.OpenEditorChild，和节点按钮、双击使用相同父子关系；没有新增活动PageStack或第二画布。
- Linked Pose目录构建和打开函数原先未被调用，现接回既有NavigatorDataSource；不新增Linked Pose资产或实施技能迁移。
- 原生居中选择不再把多选连线强转为Node，按节点矩形与连线中点矩形计算选择范围。
- 共享详情在预检或领域字段校验失败后重新读取正式值，并在同一详情面板显示错误，避免输入框保留未提交值。状态转移的Custom模式仍先等待Curve选择，再通过既有typed Mutation提交，不写半套配置。
- 运行完成帧只更新已采集的观察文字和高亮，不再每10Hz重建作者属性控件。状态机高亮同时检查StateMachineId和SourceMap中的GraphId、AuthorNodeId、CallSite、编译NodeId，防止同一作者状态机不同调用的结果合并。

源码核对排除了“Document永久持有旧状态机对象”的猜测：CharacterPoseStateMachineDocument及CharacterPoseTransitionRuleDocument已经按owner GraphId和NodeId取得当前Payload，未修改这条正确实现。

当前作者资产重新只读解析：8张图、25个节点、19条Pose边；SHA-256仍为`2766503f845a89d7b743a2bd29d4381d3650abbd8a026c55fde35305920e7a39`，与盘点基线一致。状态机、规则和资源引用保存在`.codex-tmp/canvas-core/pose-authoring-audit-20260908.json`。本次没有改写YAML。

验证过程：已通过Unity实例`e852139597e42532`发起刷新。期间编译被其它领域CharacterAuthoringSourceCompilationModel中的3个错误阻止：CharacterBlackboardDeclarationSnapshot不能转成BaseExposedProperty，以及两处CharacterAuthoringBlackboardDeclaration.Graph不存在。错误行号随其它任务编辑移动，曾读取为592、597、599。本任务没有修改或绕开这些错误。

22:11:59生成新程序集，随后实例恢复且Console为零错误。通过既有菜单重新打开Corin，get_windows确认唯一Canvas窗口为NodeCanvas.Editor.GraphEditor；这些结果只证明本批编译加载和窗口入口，不代替完整编辑交互。

随后调用精确Corin Definition的`btsmtl.validate`，正式校验返回`skill_entry_graph_missing`：Attack、DodgeBack、DodgeForward引用的技能入口不在Definition.SkillGraphs中。失败路径分别为compiler/AuthoringDiscovery/Attack、DodgeBack、DodgeForward；applied=false、saved=false、touchedOwners为空。依照用户要求，不修技能、不绕过唯一Definition校验，也未继续资产apply或Build。

普通Play检查：从未播放且未dirty的GameplayLab发起editor play；后续character.fixed_input_trace/status显示playing=false、mode=Idle、actor_id为空。Console显示两条“referenced script (Unknown) ... missing”，而manage_scene/validate报告GameplayLab为零缺失脚本、零损坏Prefab；未据此臆测或修改报错资产，也没有生成替代角色。真实运行观察、开关对照和性能采样没有获得通过结果。

本批小步提交曾被仓库已有.git/index.lock阻止；未删除锁或终止其它git进程。锁正常释放后，5个代码文件已提交为`5b529238a`（补齐Pose目录导航与属性编辑反馈）。149273005不包含本批新增修正。

该阶段收口了原能力盘点和旧UI消费者清理，其它部分当时仍未闭环；旧统计已被新作者组织的49项实施清单替代。

#### 2026-09-09：Corin 身体链进入 Control Rig 子图

在直接资源迁移成功后，使用一次性 Editor 入口将 Corin 根图中的 Local／Component 空间转换、Foot Placement、Full Body IK 和输出转换迁入 `corin.control-rig.body.graph`。该图声明 `entry.pose.input`、`foot-placement-weight` 输入和 `entry.pose.output` 输出，根图改为 `Locomotion State Machine → Full Body Action Slot → Control Rig: Body → Output Pose`，Foot Placement 权重通过同一调用边界传入。

迁移入口为 `Tools/3C/Pose/Migrate Corin Body Chain to Control Rig`。首次端口接口试验在 `AddGraph` 后抛错并留下孤立子资产，随后使用 `Tools/3C/Pose/Clean Corin Control Rig Graph Duplicates` 从该资产全部子资产枚举并清理重复 GraphId，当前只保留一份有效 Control Rig 图。当前 Pose 资产实际为 9 张图；根图不再包含旧身体链节点，唯一 Goal Assembler 和 Full Body IK 位于 Control Rig 图内。

当前仍未完成的结构项是 Action Playback Input、Pose Parameter Resolve 和 Goal Assembler 的作者节点完全内部化；本次只完成了身体链的正式图边界，没有把现有运行 operation 改成未经验证的隐式合成。统一 Character Build 仍被 Attack、DodgeBack、DodgeForward 缺少正式 Skill Graph Entry 阻塞。

随后通过 `Tools/3C/Pose/Clean Corin Root Parameter Resolver` 删除了迁移后无下游消费者的根图 `Pose Parameter Resolve`。当前 Corin 作者资产中该节点不存在；`Action Playback Input` 仍保留且仍连接 Slot，因为现有 Slot family binding 还要求其作为内部动作播放控制 operation 的作者来源，待 Slot 内部化实现后再删除。

#### 2026-09-09：Slot 内部动作控制

Slot 的作者 Capability 已移除 `action-playback` 输入端口。Family Binding 在处理 Slot 时生成同一 `ActionPlaybackInput` operation，使用 Slot 的 Animation Channel、AllowEmpty 选择策略和现有 Action Playback／Transition owner；生成 operation 的 SourceMap 归属 Slot call-site，不改变运行时播放器、时钟或 Action Timeline owner。随后通过 `Tools/3C/Pose/Clean Corin Root Action Playback Input` 删除 Corin 根图旧节点及连接。

当前 Corin Pose Graph YAML 核对结果：`CharacterActionPlaybackInputPosePayload`、`CharacterPoseParameterResolvePayload`、旧身体链节点和 `action-playback` 端口均为 0；Control Rig 图唯一且角色为 ControlRig。正式 Float32 Build 已重新发起，但仍在 AuthoringDiscovery 阶段被 Attack、DodgeBack、DodgeForward 缺少 Definition 正式 Skill Graph Entry 阻塞，因此尚未产生新的 Program／Projection 产物。

#### 2026-09-09：Control Rig 内部 Goal Assembly

Control Rig 的 Full Body IK 作者节点新增有序 typed `FullBodyIkGoalContribution` 输入，Foot Placement 直接连接该输入；作者 `Goal Assembler` 节点、其固定 Full Body IK Goals 连接和对应 Capability 已删除。Topology 允许直接 Goal Contribution，并仍检查贡献源与 Full Body IK 使用同一 Component Pose 分支；Binding 在 Full Body IK 前生成唯一 `FullBodyIkGoalAssembler` operation、Goal Set workspace 和 SourceMap owner，运行时边界与原 Goal Assembly 保持一致。

通过统一入口 `Tools/3C/Pose/Migrate Corin to UE Authoring` 按固定顺序完成写回后，Corin 资产核对为作者 Goal Assembler 0、作者 Action Playback Input 0、作者 Pose Parameter Resolve 0。原先用于分步恢复的菜单已取消，只保留一个一次性迁移入口。Unity Tundra build success，清空 Console 后错误数为 0。统一 Character Build 的外部 Skill Graph Entry 阻塞仍未绕过。

这三个旧作者 Capability 映射保留为 `systemOwned` 编译注册，以满足统一 Node Definition 与 Capability Catalog 的一一绑定；它们不能从作者目录创建。当前作者图节点已全部清理，仓库 `.asset` 扫描未发现其它资产仍序列化这三类作者 payload。

#### 2026-09-09：Corin Action Timeline Slot 写回

Corin 原有 `CorinAttack1Timeline.asset` 的 `FullBodyAction` 动画轨道原先没有 Slot 归属，且没有 Section。通过一次性入口 `Tools/3C/Pose/Migrate Corin Action Timeline Slot` 原位写入 `corin.full-body-action`，两个原生动画片段写入 `corin.animation-rig.action-blend-profile`，并补充 `Attack` Section；没有创建 Montage 副本、第二时钟或复制动画曲线。资产 YAML 已确认轨道、片段和 Section 字段实际保存，清空 Console 后错误数为 0。

#### 2026-09-09：角色图导出与严格解析

Presentation Exporter 不再把所有非根图默认为 Subgraph，而是优先按持久化 `CharacterPoseAuthoringGraphRole` 导出 `Animation Layer` 与 `Control Rig` role；Presentation Codec 同步接受这两个正式 role。Codec 在节点校验阶段拒绝 `Action Playback Input`、`Pose Parameter Resolve` 和 `Goal Assembler` 退役作者 capability，仍保留其 system-owned 编译注册以维持 Definition／Capability 一一绑定。Unity 编译成功并清空 Console 验证为 0 错误。

#### 2026-09-09：Timeline 字段进入 Document Mutation

Timeline snapshot、Document timeline 分片和 mutation draft 现在保存 `AnimationTrack.animationSlotId`、`TimelineSection.nextSectionId` 与 `AnimationClip.blendProfileId`。Document mapper 严格拒绝悬空的 next Section；Reconciler 会为 Track Slot 和 AnimationClip Blend Profile 生成独立 typed Mutation，统一 handler 分别调用 `SetAnimationSlotId` 与原生 Timeline AnimationClip 的 `BlendProfileId`，并按 Corin Rig 目录校验身份。该链仍使用既有 Timeline owner 与资产事务，没有文件级写入入口。Unity 编译验证通过。

#### 2026-09-09：Document schema 切换到 v7

`AgentAuthoringSchema.Version` 已从 `btsmtl-agent-authoring-document.v6` 切换为 `btsmtl-agent-authoring-document.v7`，现有 manifest、sync、snapshot、mutation draft、Store 和 codec 的版本比较统一读取该常量，因此旧 v6 工作包会被拒绝。v7 Presentation Codec 已接受 Animation Layer／Control Rig role，并拒绝非空旧 `poseSources` 与退役作者节点。当前精确 Corin checkout 仍在 AuthoringDiscovery 前被 Attack、DodgeBack、DodgeForward 缺少正式 Skill Graph Entry 阻塞，尚未生成新 v7 package；这不是用旧 package 冒充完成。

MCP 五生命周期工具描述已同步为 Document v7；Timeline 的 Slot、Section next 和 Clip Blend Profile 字段同时进入 snapshot、draft、lowering、handler 与 strict mapper。Unity Tundra 编译成功。当前 v7 的完整 checkout 仍需先解决 Definition 外部 Skill Graph Entry 闭包错误，未执行 apply 或生成 package。

Presentation Snapshot 的 Animation Slot 记录现在从 Rig Definition 解析并携带独立 `animationSlotGroupId`；观察与 Document context 不再只能从 SlotId 或 AnimationChannel 推断互斥 Group。该字段使用正式 Rig owner 的 GroupId，未新增运行时路由或第二份组配置。

现行规范对账已完成一轮：`openspec/project.md`、`character-presentation-pose-graph` 和 `character-animation-presentation-authoring` 已同步直接资源 Player、Slot／Group、Control Rig 内部 Goal Assembly、Animation Layer／Control Rig role 和 Document v7 术语；`btsmtl-agent-authoring-document-sync` 已同步 v7 package、退役 Source Slot 语义和严格拒绝规则。未修改 archive 历史文件，也未为规范冲突保留兼容路径。

进一步修正了隐藏 operation 的编译顺序：Symbolic Lowering 现在显式插入 Slot 的内部 Action Playback Input 和 Full Body IK 的内部 Goal Assembler，Binding Pass 按相同 sequence 消费并生成 bound operation；Stage Schedule 的 operation 数量、typed dependency 与 SourceMap 不再出现“只在Binding补 operation”的索引错位风险。Unity 编译成功。

本轮继续把通用 Controller synthesis、AI synthesis 和 MCP bridge 的现行规范口径同步为 Document v7，并将 Character Presentation 的旧 Source Slot／Profile Binding描述改为直接资源 Player与Slot／Group owner。运行时仍保留 Action Input 与 Goal Assembler operation 名称，因为它们是内部 ABI，不是作者节点。

又同步了 `agent-character-controller-synthesis`、`agent-ai-controller-synthesis`、`btsmtl-agent-authoring-mcp-bridge`、`character-pose-plan-compilation`、`character-state-timeline-authoring-loop`、`character-animation-blend-space`、`character-pipeline-definition-authoring` 与 `graph-authoring-domain-framework` 的旧版本或旧 Source Slot 表述，保持当前 spec 与 v7代码链一致。仅修改现行规范，未改变历史 archive。

补齐了直接资源迁移后的两个编译消费者：Animation Blend Compiler 的 Slot 选择端点现在由 Slot 自身合法 AnimationChannel 与 AllowEmpty 合同生成，不再寻找已删除的 Action Playback 作者端口；Animation Resource Closure Analyzer 现在收集直接 AnimationClip 与 Blend Space sample，不再依赖旧 Source Slot 扫描。Unity 编译成功。

Profile Inspector 的连续Pose区域已按新owner整理：当Pose Graph没有Source Slot／Profile Binding时，不再显示旧绑定编辑面板，改为提示Sequence Player、Blend Space Player和Motion Matching节点直接拥有资源。修改只影响作者窗口显示，不在Inspector重建图或触发Build。

本轮最新正式 Float32 Character Build（job `284f5c1165a64fae9b995d8ee664ec7e`）仍只在 AuthoringDiscovery 阶段报告三个 `skill_entry_graph_missing`：Attack、DodgeBack、DodgeForward。没有进入Pose编译、没有修改wrapper、没有发布新Program或Projection；依照统一链约束未修技能字段、未绕过入口校验。

对阻塞输入的只读核对显示：三个入口 identity 在 `CorinPlayableRootTree.asset` 中对应的是 `StateBehaviorSubTree`，而 `CharacterPipelineDefinition.SkillGraphs` 要求正式 `BtsmtlSkillFlowGraph` 根图；当前 Definition 的 `m_SkillGraphs` 为空。因此它不是 Pose 迁移可以自行修复的名称或路径问题。

由于该外部闭包阻塞尚未变化，后续代码复核重点转向直接资源链的潜在旧消费者：Slot Blend Compiler 已不再读取 `action-playback` 作者端口，Resource Closure Analyzer 已收集直接 Clip／Blend Space；这两处会在 Skill Graph 闭包恢复后直接进入正式 Build，而不会重新走旧 Source Slot 路径。
