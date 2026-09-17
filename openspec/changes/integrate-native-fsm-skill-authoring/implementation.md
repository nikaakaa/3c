# 实施记录

## 当前边界

本记录保留2026-09-13的r2—r4实现记录；当前执行窗口进入tasks.md的r5共享准入命名与StateBody系统锚点收口。未完成的正式资产生成仍单独标注，不把代码链或导出源码误写成资产闭环已完成。

## 6.1 Applier有效操作迁出

| 旧Applier操作 | 正式承接位置 | 旧协议部分 |
| --- | --- | --- |
| CreateNativeStateMachine | BtsmtlSkillGraphAssetFactory.CreatePrivateStateMachine、BtsmtlSkillNativeStateMachine.ConfigureIdentity/ConfigureOwner | Agent target、local表、Session touch和包路径分派 |
| SyncNativeStateMachine | BtsmtlSkillNativeStateMachineAuthoring.ResolveState/CreateState/EnsureState/ConfigureState | JObject字段分派和包节点集合同步 |
| SyncNativeAnchors | BtsmtlSkillNativeStateMachineAuthoring.AnchorType/ResolveAnchor及Entry Prime配置 | 包anchor对象及协议local身份 |
| SyncNativeEdges | ResolveConnection/CreateConnection/EnsureConnection/ConfigureConnection | 包Edge对象、协议默认值和旧差异删除 |
| ValidateNativeOwner | BtsmtlSkillNativeStateMachineAuthoring.RequireOwner | Applied包回读和重复owner规则 |
| ResolveNativeEndpoint | ResolveAnchor/ResolveState加当前apply的local映射 | local计划身份到正式对象的临时映射 |

Applier仍暂时承载Skill Document字段解析，因为公共调用者尚未完成退役；它不再拥有FSM创建、状态配置、转移改接或业务约束的独立实现。调用者切换后只删除协议部分，不把整个文件改名搬迁。

## 6.2 正式FSM authoring API

BtsmtlSkillNativeStateMachineAuthoring提供以下typed入口：

- ResolveState、ResolveAnchor、ResolveConnection：按正式业务identity读取对象。
- CreateState、CreateConnection：创建新对象并使用原生FSM GraphEditor/Mutation owner。
- EnsureState、EnsureConnection：恢复稳定identity；系统锚点按类型复用，不重复创建。
- ConfigureState：配置名称、layout和StateBody，并回收同一资产文件内被替换的旧私有Body。
- ConfigureConnection：配置ConditionRule、priority、abortPolicy和order。
- PruneConnections、RequireOwner：清理明确转移集合并约束调用owner。

BtsmtlSkillFlowGraphAuthoring对普通Skill FlowGraph提供同样的节点/连线读取、创建、改接和清理入口。系统节点按正式类型复用，普通节点和连线按stable identity恢复。

## 6.3 系统入口、身份与转移

生成代码创建新FSM时先由工厂建立Entry/Any/Exit，再按导出identity复用这三个对象；不会把默认锚点和导出锚点各创建一份。Entry重新绑定后设为Prime。

Skill Document的local:*计划身份仍只存在于当前协议apply映射表。协议apply创建对象使用原生生成的正式UID；公共C#生成使用导出的业务identity，不把local:*写进正式对象。

原生Connection可按全机范围读取。已有identity端点变化时直接通过正式Connection API改接并保留identity、payload和顺序；新连接只通过原生FSM连接工厂创建。

## 6.4 闭包读取与输出接入

现有BtsmtlSkillNativeStateMachineContract.References提供StateBody和ConditionRule闭包；代码导出适配器从正式FSM、State和Connection收集对象变量，并将内部子资产与范围外外部资源分开。

Skill C#输出现在：

- 输出状态名称与layout；
- 输出StateBody引用；
- 输出Connection的condition、priority、abortPolicy和order；
- 输出普通Skill Graph的节点名称与layout；
- 输出普通Graph和原生FSM的连线identity；
- 生成代码执行时清理输出范围外的旧节点和旧连线。

输出入口不引用Agent DTO、JToken、SerializedObject路径或旧生成子资产GUID。

## 6.5 约束归属

原生FSM模块负责状态类型、锚点类型、端点、owner、ConditionRule角色、priority、abortPolicy、order和NodeCanvas运行参数约束。BtsmtlSkillNativeStateMachineContract继续负责完整FSM结构约束；GraphClosure负责StateBody/ConditionRule实际owner闭包。Applier只保留包字段到typed参数的映射错误，不重复解释业务规则。

## 当前未完成边界

- Corin Attack已通过公共`btsmtl.generate_assets`完成一次性原生FSM重建并保存原Skill资产：状态、StateBody、条件、priority、abortPolicy、order、layout和root binding均由正式API写入；旧Startup/Setup/Clear节点、旧FSM子资产和失败残留空FSM已删除。
- 2026-09-13已通过公共`btsmtl.generate_assets`完成DodgeBack与DodgeForward两个Skill资产重建并保存：两者均拥有Entry/Any/Exit、唯一动作State、StateBody、Timeline生命周期、ActionExit条件和正式FSM转移；两个资产的`Startup Branches`、`Enter Dodge`、Directional Dodge Run Intent Setup/Clear根节点计数均为0。
- Dodge迁移过程中修正了C# authoring的Timeline非序列化编辑器状态初始化、黑板声明的`DeclarationId + OwnerId`归属判断，以及Forward五条ActionExit连线误用根Graph的问题；统一输出器现在按FlowGraph、Native FSM、黑板顺序清理。
- 一次性Corin迁移入口已在资产写入成功后删除；Attack已通过公共`btsmtl.export_code`输出为`Generated/CorinAttackSkillAuthoringCode.cs`，并在Unity重新导入后无该生成文件的编译错误。
- DodgeBack与DodgeForward已在资产保存后分别通过公共`btsmtl.export_code`反向导出到`Generated/CorinDodgeBackSkillAuthoringCode.cs`与`Generated/CorinDodgeForwardSkillAuthoringCode.cs`，两次diagnostics均为空，源码继续只调用正式C# authoring API。
- 现有`BtsmtlSkillGraphOccurrence → BtsmtlSkillGraphCompiler → EmitNativeStateMachine`已直接读取native FSM；本轮不调用独立Build，避免把用户明确禁止的Build混入作者迁移。
- r2规范对账已完成：本change不发布Document v8、不新增五个Agent工具，只保留Skill/FSM正式API、native资产和既有编译链；公共C# authoring工具及Agent协议退役由另一change负责。
- 生成过程中首次暴露的Timeline `SerializedTimeline`空状态和黑板owner误判均已修正；修正后DodgeBack、DodgeForward正式生成均返回`saved: true`，没有失败diagnostic。
- 旧Skill/Presentation Document、AgentAuthoring协议、Applier调用者和Skill legacy状态字段已由公共退役动作删除；authoring写入链只保留`btsmtl.export_code`与`btsmtl.generate_assets`。独立的`btsmtl.scene_play`仍是正式预览/观察工具，不属于旧Agent authoring链。
- `m_Name`历史错误的完整调用栈落在`Slate.CutsceneGroupInspector`，不属于本Skill/FSM作者层；不改名、不清缓存。
- 本轮未运行Build或Play；Unity MCP只用于执行正式资产生成入口。
- tasks.md第7节的GameplayAbilityDefinition完整入口、AbilityGrant迁移及旧SkillDefinition删除仍未执行，属于下一阶段剩余边界；本轮不提前合并或改写该架构。

## r4 当前代码进展

- 已新增独立GameplayAbilityDefinition与AbilityGrant合同；Ability拥有准入规则、效果、结束规则、后续Ability和私有AbilityGraph引用，角色通过Grant持有输入消费与目标绑定。
- CharacterPipelineDefinition已接入AbilityGrant列表、AbilityGraph导航和Ability配置校验；作者入口可沿FlowCanvas原生GraphEditor打开私有AbilityGraph。
- 编译发现、语义发射、Program catalog、控制模块请求、Equipment route和Float32/Fixed执行器已开始切换到Ability命名；运行时执行状态改用AbilityExecutionState语义，现有Action实例状态存储不另建第二份。
- 新增AbilityLifecycleEntry与AbilityExecutionContext视图；Complete/Cancel/Interrupt/Abort由正式动作入口显式写入，再由操作控制链停止内容。StateBody现在固定生成不可删除的OnEnter/Root/OnExit生命周期锚点；OnExit只由运行时代码触发并负责StateBody清理，不接受作者终态分派。Submit生命周期节点及StateBody Succeed残留不再创建或发射。
- 公共C#导出适配已接入GameplayAbilityDefinition根、Ability私有图、Ability配置和生命周期清理；2026-09-13已通过正式`btsmtl.generate_assets`保存Corin Attack、DodgeBack和DodgeForward三个GameplayAbilityDefinition资产，诊断为空。三份新资产中均无`Submit`、`Action Exit`、`ActionExit`、`StateOnExit`或作者`OnExit`文本；旧三份Skill根及旧Attack/Dodge ActionContextSlot由正式生成入口删除。
- 终态决定的实际运行落点为`Fixed/Float32OperationEvaluator.TickSkillPrograms`与`Fixed/Float32ActionRuntime`的Ability生命周期入口：执行根结果和外部请求先写入Ability实例，再由`OperationStateMachineRuntime`停止StateOnEnter、StateRoot及其子内容；通用StateBehavior的内部OnExit仍只作为清理阶段运行。`StateExitCause`只保留状态内部事实，不再反推Ability终态。
- `GameplayAbilityDefinition.EndRules`已进入Program catalog，并由`AbilityLifecycleEntry`在ExecutionCompleted、CancelRequested、InterruptRequested和AbortRequested入口解析；没有显式规则时只使用代码层自然结果，不生成作者侧Complete壳或Submit节点。
- 结束触发名已放在无引擎的Simulation.Core合同中，Fixed/Float32只依赖该合同；`ThirdPersonCharacter.ActionSystem.GameplayAbilityEndTrigger`只留在作者配置层，避免角色作者程序集反向污染模拟核心。
- 通用StateBehavior的内部OnExit/ForceStop路径已保留；Ability StateBody的工厂、规则和编译输出生成固定OnExit入口，但不生成作者可编排的OnExit清理图或Submit节点。
- Skill专用leaf emitter已移除Submit生命周期操作分支；Simulation层保留的通用Submit操作只服务仍有明确消费者的非Skill路径。
- Ability终态写入不再提前清理Tags/Blackboard；Fixed/Float32在EntryOperation停止完成后由动作owner统一回收终端资源，保证replacement stop barrier期间上下文仍可定位。
- 导出器在迁移阶段曾裁剪旧生命周期节点及其边并把有效终态迁入Ability规则；迁移完成后已从Skill作者域移除`BtsmtlSkillSubmitActionLifecycleFlowNode`及旧ActionExit过滤/读取路径，`BtsmtlSkillStateOnExitFlowNode`只作为新的固定StateBody系统锚点保留，不属于作者可配置清理图。通用非Skill Submit与运行时内部OnExit仍保留。当前三份Corin旧Skill根已不存在，新Ability资产不再生成旧生命周期壳。
- 正式生成上下文新增了受控的旧资产删除结果；最后一个Ability Grant完成迁移时，生成入口会删除旧Skill根资产及其旧ActionContextSlot，并在`DeletedAssetPaths`中返回，不通过文件系统旁路删除。
- 已补齐`GameplayAbilityDefinition`作为`IGameplayBehaviorProfile`所需的`DebugCategory`与`Tags`，并让作者Inspector及正式C#导出保留这两个字段；同时删除旧Skill定义的重复`Serializable`特性。
- 旧Dodge的`Submit Window Cancel`迁移会沿Selector条件图提取唯一`ActionWindow`类型，输出`ActionWindowClosed`结束规则；正式停止请求携带窗口类型后由统一Ability生命周期按规则选择终态，未恢复作者Submit或OnExit节点。
- 当前源码已消除Ability结束规则访问级别和AbilityGrant标签目录作用域错误；Unity最新可读Console仍有相机Cinemachine引用与诊断采样合同错误，属于并行/外部范围，未触碰。
- GraphClosure现在拒绝脱离GameplayAbilityDefinition拥有关系的AbilityGraph子资产作为正式根，旧CharacterPipelineDefinition根仍按迁移输入保留到正式生成完成。
- AbilityExecution存储接口的状态槽合同已从`IsSkillStateSlot`收敛为`IsAbilityStateSlot`，Fixed/Float32共用同一现有执行状态，不增加第二份运行状态。
- 修正Ability导出适配器的调用签名与Definition读取方式：生成阶段使用正式导出上下文的资产读取边界，图/FSM/Timeline创建与裁剪调用不再传入不存在的根参数。
- 闭包校验、编译发射、节点Inspector和C#适配器已不再包含Skill作者层的旧ActionExit、Submit过滤或读取分支；Ability StateBody固定拥有正式OnEnter/Root/OnExit入口，OnExit通过已有StateOnExit运行时路径执行，普通Graph的合法生命周期仍由通用运行模块拥有。
- GameplayAbilityDefinition对外投影共享准入规则的`TargetRequirement`，作者入口显示其正式来源，不复制一份目标规则。
- 当前`ThirdPersonClient.Editor.csproj`静态编译为0错误；现有警告均来自既有包或外部诊断文件，未运行Unity Play/Build。
- 运行时生命周期入口已将内部执行对象从`AbilityLifecycleEntry<TActionState>`统一命名为`AbilityExecution<TActionState>`，继续复用原有Fixed/Float32 Action实例状态和`AbilityExecutionContext`，没有创建第二份执行状态。
- `CharacterPipelineDefinition`代码已移除`SkillDefinitions`和`SkillGraphs`字段，角色Definition当前只保存三条AbilityGrant；由于Unity对已删除字段的旧序列化键不会由普通SaveAssets自动剥离，authoring根绑定已增加定向`AssetDatabase.ForceReserializeAssets`，待MCP bridge恢复后再通过同一正式`generate_assets`入口完成该Definition的最终清理。
- 删除Skill作者节点后Unity首轮增量编译曾遗漏新加入的`CharacterControlMotionCompilationRecord`源文件；强制重新导入该正式源文件后Unity Tundra编译已成功，静态编辑器工程也以0错误生成。剩余仅为既有包/诊断warning。
- 当前OpenSpec实施进度已收口为36/36；正式`generate_assets`重保存后，Corin Definition序列化文本中的`m_SkillDefinitions`和`m_SkillGraphs`旧键已消失，三条AbilityGrant仍保留原输入与目标绑定。Unity客户端在返回结果前断开了MCP会话，但资产写入、Unity日志和文件状态均已落盘。
- 最终静态编辑器工程按规定参数生成成功，0个错误、34个既有包/诊断warning；OpenSpec change严格校验通过。本轮未运行Unity Play或Unity Build。

## r5 共享准入命名与StateBody锚点

- `ActionProfile`作者类型已整体改为`GameplayAbilityAdmissionProfile`，保留`ActionId`作为运行时Action操作的稳定业务身份；没有旧类型别名或旧API双写。
- Definition的准入列表、Ability的`AdmissionProfile`、CanActivate/Activate/ActionTarget作者节点、Graph capability、语义依赖发现、Program Catalog字段、Ability binding、Fixed/Float32准入查询入口已统一为AdmissionProfile命名。Action catalog和`action:<ActionId>`运行时操作身份保持不变，避免把执行操作误改成Ability身份。
- 正式C#作者工具已增加准入Profile根的export/generate支持。旧Attack/Dodge准入资产已经分别通过`btsmtl.export_code`导出为`Generated/CorinAttackAdmissionProfileAuthoringCode.cs`和`Generated/CorinDodgeAdmissionProfileAuthoringCode.cs`；生成代码保留tag、Required/Block/Cancel query、目标要求和并发上限。
- StateBody代码链已固定`OnEnter`、`Root`、`OnExit`三个系统锚点；OnExit不进作者菜单、不能删除，编译器按退出阶段映射，运行时沿State stop barrier调用。当前Ability资产尚未重新生成出这次新锚点，因为Unity全程序集仍被并行Timeline缺失类型阻塞。
- 现行`openspec/specs`已清除旧`ActionProfile`命名和Ability StateBody两锚点例外，保留普通`StateBehaviorSubTree`的三入口生命周期合同；tasks.md的8.1与8.5已完成。
- 为恢复Unity编译，`FixedGameplayAbilityDataAsset`中的Fixed Program类型已改为显式限定，消除了当前暴露的Fixed/通用Simulation命名歧义；这不改变Ability authoring或运行时合同。

## r5 收口记录

- 准入资产迁移已完成：旧Actions/Attack|Dodge/Corin*ActionProfile.asset通过git mv迁为Abilities/AdmissionProfiles/CorinAttackAdmissionProfile.asset与CorinDodgeAdmissionProfile.asset，GUID随.meta保留，Dodge单一共享对象继续被DodgeBack/DodgeForward共用；资产m_Name、生成Root.cs内ResolveExternalAsset/EnsureAdmissionProfileRoot路径字符串、corin-source-manifest.json的assetPath与owner命名同步更新；旧Pipeline/Actions目录及其全部.meta删除，全仓旧名旧路径仅剩本change delta的迁移场景描述。
- 8.3消费者链静态审计通过：CompileDiscovery经Ability.AdmissionProfile、语义依赖目录按ActionId声明身份、Float32/Fixed ExecutionServices以ActionAdmissionProfile按operation/actionId双索引、export_code适配器输出唯一共享对象；现行Attack.Float32Data的m_OperationSetVersion为character-gameplay-operations/16。
- 8.4 StateBody锚点合同静态审计通过：工厂生成/补齐三锚点、删除/剪切/清空/复制全链拒绝系统锚点、[DoNotList]与CapabilityCatalog过滤作者菜单、GraphClosure校验锚点唯一且齐备、编译器按Enter/Enter/Exit与order 0/1/2发射、运行时ContinueStateStop在stop barrier执行OnExit而ForceStopState仅ForceStop释放；Corin三张Ability资产已无Action Exit Selector/Submit清理链。
- 8.2/8.3/8.4已在tasks.md勾选并附证据；本节替换原“r5当前阻塞”段，该段所述DotRecast程序集错误与generate_assets不可用均已由后续提交解决，不再作为当前状态。

## r6 TreeClip控制流断裂报告

- 失误记录：TreeClip图（TimelineBody）从活图语义迁移到编译语义时控制流未迁移——编译器只为TimelineClip caller声明hook invocation且未实际生效，运行期Update驱动缺失。作者可见形态（root纯输出、无控制输入流）自ebc41bfcc（2026-09-08）起变更；该断裂与形态变更均未按规矩同步报告与spec，btsmtl-runnable-timeline-node至今保留已删除的RootTree operation旧挂靠条款。此为本change实施链的流程失职，留痕备查。
- 现状验证：解码Float32/Fixed执行产物，TimelineBody图身份1eac26e4、Root handle a7d4ffcf、TreeClip hook entry全部0命中；StateBody（894a4cd1）正常编译110处。窗口决策实际由TimelineData gameplay segment采样承担，TreeClip图内容为不执行的被引用逻辑。
- 接通方案：编译期TimelineBody图完整编译并登记Root entry（clipId到handle），运行期Update事件驱动Root entry每帧TickPersistent，边界hook保持一次性；设计详见design r6章节，任务见tasks第9节。
