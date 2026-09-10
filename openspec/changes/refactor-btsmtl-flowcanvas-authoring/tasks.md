## 当前执行位置

当前：公开能力对账完成；原生技能编译、Macro参数、Timeline／TreeClip、黑板、私有闭包复制、端口与边诊断、子调用代次、运行导航及Host实例选择代码已落地。Document基础已接入，按作者A选择统一独立技能根；现行全局协议为v7。旧自研AI作者/运行链已经删除，Behavior Designer不进入BTSMTL Skill Document。Corin Skill资产已完成最后迁移并通过正式checkout/dry-run闭合；本任务剩余是Build、运行观察、网络和事务证据，不把编译或Clean文档状态冒充这些证据。

本表是执行清单。原来的大项拆为代码、接入和证据任务，原完成标准保留在对应标题中，不删减要求。勾选代码任务只表示该段实现已提交，不表示实际技能已经使用它；接入及验证分别勾选。不再用旧的“1/35”或提交数量推算整体百分比。

尚未完成：Simulation Pipeline/网络Adapter正式接合证据、Document与owner的完整Undo/保存往返、采样性能及运行观察对账。Skill provider面板、Skill Local声明、输入/Tag/Attribute/GameplayEffect引用节点及其Document导出/Apply代码已通过脚本编译；Character State只读typed projection、GAS provider owner隔离以及双 Target/Snapshot State Layout 与 codec 已由正式validate核对；Character不再通过旧通用Graph/State/Timeline/Blackboard入口访问RootTree，Character Document也不再生成顶层Blackboard、旧Graph或旧Timeline分片。旧自研AI作者与运行链已删除，Behavior Designer的正式任务接入归独立`replace-btsmtl-ai-with-behavior-designer` change，不在本change恢复AI图或AI Document。实例选择和调用导航已有代码，相关运行证据项仍保持未勾选。最新正式 Character checkout 与 dry-run 已重新通过，当前v7包为`syncState=Clean`、`plannedDiff=[]`、`documentHash=17271f8622b4dbf68823b95976ea928feb55d32a235799c0b87e07b93450e1d5`、`planHash=4f53cda18c2baa0c0354bb5f9a3ecbe5ed12ab4d8e11ba873c2f11161202b945`、`editableHash=1d88461ecbe8377c601bc51f9c85564038ff885ef41695793792667a859a7e9b`；未执行Character Build、Play、网络回放或性能采样。

未完成项按缺口阅读，不能一律理解为尚未编写：

| 缺口 | 对应工作 |
|---|---|
| 仍需补代码与集成 | 2.1.2导入／复制身份合同核对；2.4完整owner事务；4.5 Simulation Pipeline与网络正式接合 |
| 主链代码已有，仍需完成规定的执行核对 | 2.1.2身份保存／重载；3.3—3.4编译语义与页面导航；4.1.4—4.3正式构建；6.2—6.4版本、调用诊断与开销；7.2—7.5实例选择和观察生命周期；8.5规范与Rootless残留核对 |
| 基础已提交，集成及资产往返未完成 | 5.3的根／私有／共享owner保存与失败恢复证据 |
| 最后执行的资产与交付工作 | 5.6产物发布；8.1运行对账；8.4.2最终代码／资产／CLI证据汇总 |

本表只解释缺口，不替代各任务的完整完成标准。任务总量随架构决策补充而变化，不再使用固定总数推算进度；编译错误只影响对应验证，不作为停止独立代码或文档工作的理由。

维护规则：每次小步提交后同步本表中的对应项和提交证据；结束一项后继续下一项，不到最后再统一补记。

## 1. 基线与替代范围

执行顺序（用户最新指示）：先完成代码、编辑器及实例观察；现有资产迁移最后执行。迁移器和Document适配代码可先实现，但5.4的实际迁移执行、5.5的apply及5.6产物发布不得提前运行。期间仅刷新检查本次脚本，不处理其他领域报错。

当前实施约束（2026-09-08最新）：用户已允许Unity刷新及脚本编译检查，只修本次BTSMTL引入的问题；其他领域报错记录但不处理。内容Build、整根Validator和回放仍不主动触发，其验证条件保留为收口门槛，未执行不得勾选完成。正式入口按实例工作区组织，不恢复Character Definition的Open Root Tree路径。


- [x] 1.1 固定实施worktree、源码及资产基线，在原BTSMTL与Scene Play changes记录本提案替代项及责任边界；交付逐项对账表，不自动归档旧任务。见implementation.md；其余工作区改动保留。
### 1.2 节点、端口与调用闭包盘点

- [x] 1.2.1 静态盘点Corin三项技能的可达节点、状态、条件、内联／共享Timeline和编译调用者，记录入口身份与迁移去向。证据：skill-inventory.md，b3d066d12。
- [x] 1.2.2 将盘点与当前完整Capability目录逐项对账，补齐未在Corin出现的公开能力、唯一端口形状和删除项，确认无遗漏。证据：skill-inventory.md“2026-09-09公开能力补充对账”，49项旧注册逐项归属及53个原生类型；不代替动态端口代码合并和构建验收。
- [x] 1.3 记录精确Definition的现有编译／回放／诊断基线及未完成资产事务；通过正式CLI结果核对，不把历史实验当作当前基线。证据：精确 Corin Definition 的 checkout/dry-run/apply/re-checkout 已执行；当前基线为 v7 `syncState=Clean`、`plannedDiff=[]`、apply `applied=true/saved=true`，Character Build/validate 仍被 Presentation Projection 闭包错误阻塞。

## 2. 共用作者基础

### 2.1 正式图与稳定身份

- [x] 2.1.1 实现直接保存原生拓扑的技能FlowGraph、状态／条件页面及FlowNode基础，使用原生Node／Edge UID和稳定步骤Port ID，不创建旧图镜像。证据：ebc41bfcc。
- [ ] 2.1.2 完成Document导入及复制时的Node／Port／Edge身份写入合同，并核对保存／重载后的身份和正式编译输入。

### 2.2 唯一能力和端口目录

- [x] 2.2.1 增加原生目录筛选与连接规则接口，技能连接拒绝隐式类型转换，从连线创建节点时列出明确目标端口。证据：00761c2aa、ebc41bfcc。
- [x] 2.2.2 为原生AND／OR／NOT及浮点／整数比较登记原生端口到Program端口的映射。证据：ebc41bfcc、ee557a353。
- [x] 2.2.3 把正式Capability和唯一Port Shape贯通原生注册、创建目录及Document，消除重复字段／端口声明，交付完整能力映射与校验报告。

  代码/报告证据：BtsmtlSkillCapabilityCatalog统一kind、字段、anchor和固定Port Shape；原生创建规则、Document exporter/mapper/validator、Graph closure均读取该目录；ValidateCatalog输出固定端口方向/类型/容量/required差异诊断，动态Macro/步骤/黑板端口由正式文档数据投影。

### 2.3 全部写入口与typed Mutation

- [x] 2.3.1 实现节点创建、连线创建、节点／连线删除、整端口断开和步骤修改的编辑事务代码；步骤换序保留ID，已连接步骤拒绝删除。证据：ebc41bfcc。
- [x] 2.3.2 将全部节点字段、原生默认值编辑和重连接入正式typed Mutation预检，拒绝非法修改后再写数据。

  代码证据：技能节点Inspector、原生Simplex技能wrapper、步骤编辑、Macro接口编辑和连线创建均进入BtsmtlSkillFlowEditorMutation；非法值、容量、类型或已连接接口修改在写入前拒绝。
- [x] 2.3.3 实现批量复制、粘贴和删除的整包预检与原子写入，包括私有子图闭包；非法集合不得部分写入。

  代码证据：复制/粘贴/剪切/批量删除均先校验领域、系统锚点、拓扑和私有闭包，再在单一Undo事务创建副本、重绑Node/Edge/Timeline/Blackboard引用；异常由外层事务恢复，shared主资产不进入删除集合。
- [x] 2.3.4 将上述入口统一到真实owner及Document Mutation，审计并关闭遗漏的原生直接写入口。

  代码证据：Skill Graph/Macro Graph结构入口、节点字段、默认值、Native Simplex、Macro接口锚点、复制/删除和Document Applier均通过真实FlowGraph owner与BtsmtlSkillFlowEditorMutation；旧通用锚点仅作为既有资产读取兼容，不作为新技能写入口。
- [x] 2.3.5 实现provider-aware Blackboard面板、变量创建、Skill Local声明、外部provider引用和typed Get/Set拖拽；通过owner、声明ID、类型和读写权限的Mutation/Undo核对。证据：Skill Graph Authoring Panel、正式Skill Mutation、provider节点编译、Document Export/Apply已接通；Unity脚本编译通过。
- [x] 2.3.6 从Definition的GA列表按SkillId和EntryGraphAuthoringId精确打开指定Skill Graph；通过入口路径核对不默认打开第一个Graph且不恢复Character RootTree。证据：Definition Editor逐Skill解析EntryGraphAuthoringId，导航工具按稳定GraphAuthoringId打开，未恢复Character RootTree入口。

### 2.4 Undo、保存与回滚

- [x] 2.4.1 实现单图编辑事务中子操作复用外层Undo、异常回滚及重新反序列化的代码。证据：ebc41bfcc；尚无完整交互验收证据。
- [ ] 2.4.2 完成人工单操作与Document整包Undo的统一边界，覆盖根、私有页及实际修改的共享owner，核对保存／重载身份，交付owner链说明及正式执行证据，不新增测试代码。
- [x] 2.5 将目录、Details、Toolbar及观察区域接到同一原生GraphEditor，删除被替代的技能图交互入口；通过源码搜索核对无第二画布或独立选择集合。

  代码/审计证据：技能Graph/Macro使用FlowCanvas原生GraphEditor，节点Inspector、原生创建目录、Timeline Open、Macro接口Toolbar和BtsmtlSkillObservationToolbar均挂在同一GraphEditor；RuntimeDebugSourceNavigator与ObservationSession直接打开该GraphEditor，技能目录未发现第二技能GraphView、Workbench或独立selection集合。

## 3. Macro及状态页面

### 3.1 原生Macro与所有权

- [x] 3.1.1 实现技能Macro类型，复用原生接口锚点和调用节点；端口声明不写共享运行委托，提供私有子资产创建工厂。证据：00761c2aa、ebc41bfcc。
- [x] 3.1.2 完成作者可用的私有／共享Macro创建与引用入口、所有权显示及反向导出身份记录。

  代码证据：原生菜单支持创建私有Macro，新增共享Macro主资产创建入口与引用入口，Macro页面显示Inline/Shared所有权；调用节点、单入口接口及私有页面在同一编辑事务创建，Document继续导出Macro接口与owner身份。
- [x] 3.1.3 完成私有Macro闭包复制和删除回收，删除调用不得删除共享定义；形成正式创建、复制、删除计划并归入根事务。

  代码证据：BtsmtlSkillGraphCopy捕获Node、Macro、Timeline、黑板owner与条件引用闭包并在同一Undo事务重绑；BtsmtlSkillOwnedAssets只回收当前根失去引用的SubAsset，shared主资产永不进入回收集合；Document Applier对移除闭包沿同一事务删除。

### 3.2 Macro接口变化及引用校验

- [x] 3.2.1 实现图闭包检查代码，拒绝递归、跨领域引用、重复身份和非法条件调用；错误包含图／节点路径。证据：ebc41bfcc。
- [x] 3.2.2 实现接口重命名、类型变化及所有调用方端口检查，保持稳定ID，拒绝会破坏调用连接的不完整发布。

  代码证据：Macro接口编辑窗口保留参数ID，重命名只改显示名；类型变化与删除先检查全部正式调用方连接，修改后刷新调用节点并逐调用位置执行稳定端口/类型校验，失败由跨owner Undo回滚。
- [x] 3.2.3 接入正式Validator，输出子图及具体调用位置的闭包与接口错误证据。

  代码证据：BtsmtlSkillGraphClosure在每个Macro调用位置执行接口与闭包校验，错误路径包含父Graph/Node；Document Validator同步核对Graph owner、Macro分片、Edge Port Shape、递归和引用闭包。

### 3.3 局部状态及组合语义

- [x] 3.3.1 实现状态机／状态／条件结构节点、进入与退出入口，以及顺序、选择、并行、循环和结束节点的作者代码；步骤采用独立Flow输出，未把Flip Flop作为Sequence。证据：ebc41bfcc。
- [x] 3.3.2 补齐剩余公开业务能力，并将新状态页及组合节点进入完整编译；按能力与语义发射清单逐项核对等待、转换、完成及停止语义。

  代码/语义证据：公开能力补充对账覆盖37项技能kind、53个原生类型；SkillLeafEmitter、GraphFlowEmitter、MacroCompilation和TimelineCompiler分别覆盖叶节点、状态/组合边、Macro等待/完成和Timeline生命周期。ActivateActionInstance不再重复发射，旧AI专用kind已经删除；正式产物运行证据仍归8.1。
- [x] 3.4 接入原生下钻、breadcrumb及私有／共享标识，保留Timeline独立编辑和TreeClip返回关系；交付页面来源身份及生命周期对账记录。

  当前进度：技能Timeline节点及TreeClip.AssetTree已接现有资产打开入口。932421b16已接原生父子页面导航、父调用返回和Timeline调用上下文，沿明确调用路径绑定观察；不是尚未编写。完整私有／共享标识与页面来源、生命周期运行核对仍未完成，此项不勾选。

## 4. 直接编译

### 4.1 正式原生图直接编译

- [x] 4.1.1 分离操作写入和旧节点读取，使既有路径使用唯一CharacterSimulationOperationEmitter。证据：cfe1d9065。
- [x] 4.1.2 实现原生逻辑、已支持结构节点及输入／动作节点的叶节点发射器；读取serializedValue并校验Program端口合同，不调用getter。证据：ee557a353；后续已通过4.1.3接入正式技能发现，构建证据仍按对应任务核对。
- [x] 4.1.3 替换技能入口的旧图发现：Definition.SkillGraphs提供明确原生根，CharacterSkillCompilationDiscovery按SkillDefinition稳定入口ID解析并生成原生调用记录，CharacterSemanticEmitter通过BtsmtlSkillGraphCompiler调用叶节点／Macro／边发射器进入唯一Semantic IR。源码审计无旧图对象中转；脚本检查无CS编译错误。资产赋值与构建验收仍在5及8，不由此项代替。

  代码入口已切换；Corin存量资产已填入Definition.SkillGraphs，缺失时明确报错，不回到旧RootTree查找技能。SkillDefinition的入口ID保持稳定业务身份，不增加另一份技能定义。
- [x] 4.1.4 完成控制边、值边、条件页、状态页及黑板作用域发射；通过构建来源与依赖审计确认没有旧BaseGraph转换。

  当前进度：普通黑板使用原生Variable保存名称、类型和默认值，声明元数据只记录作用域／生命周期／输入与窗口绑定；编译取已存值，进入共用声明快照和作用域发射器。原生读写节点已接类型及Config只读检查。原生窗口查询按每次调用的祖先作用域匹配Decision TreeClip投射，记录候选来源，不使用共享图的首次出现代替其他调用；正式构建来源核对仍待完成。
- [x] 4.2 完成Macro参数、嵌套调用和调用实例布局降低，保持ActionInstance、generation及状态恢复；用现有编译报告核对同定义不同调用的独立状态范围。

  代码/审计证据：Macro route拥有独立参数StateSlot和GraphCallFrame，SubGraph运行使用SubgraphCompletion等待并读取独立调用输出；ActionInstance/generation沿Program调用状态保存。正式产物对账仍归8.1/8.4.2。
- [x] 4.3 将Timeline与TreeClip正式引用接入新作者遍历，保持调用方、完成和停止顺序；通过精确根构建及引用闭包报告核对。

  代码/审计证据：Timeline节点只引用真实TimelineAsset，私有内容由技能根拥有；BtsmtlSkillGraphOccurrence递归发现TreeClip原生Graph，BtsmtlSkillTimelineCompiler沿同一调用路径发射Timeline hook/完成/停止语义，闭包校验拒绝错误owner。精确根运行对账仍归8.1/8.4.2。
### 4.4 作者与运行隔离

- [x] 4.4.1 技能Graph／Macro拒绝原生运行初始化和Macro运行克隆，原生Macro端口声明不登记运行委托。证据：00761c2aa、ebc41bfcc。
- [x] 4.4.2 审计完整正式运行装配不引用作者图、不启动FlowScript或原生委托／协程，交付运行工厂到Program执行器的依赖链。见implementation.md“技能作者与运行依赖审计”；此项为源码依赖审计，不代替8.1运行对账。

### 4.5 状态Provider与Simulation Pipeline

- [x] 4.5.1 接入C# Character State的只读typed projection；验证Skill只能读取Movement事实，不能直接写Velocity、BodyYaw或Control State，影响主线必须提交正式Command。证据：`CharacterStateProviderFields`、四类只读Skill State节点和`CharacterStateRead`唯一发射路径；正式`btsmtl.validate`返回`compileSuccessCount=1`、`semanticValidCount=1`，未发现Character State写端口。

  当前代码：`CharacterStateProviderFields`固定Position、Velocity、VerticalVelocity、BodyYaw、Grounded五个只读字段；Skill Graph提供按输出类型分开的Get节点，Document保存`fieldId + providerOwnerId`，owner必须等于当前Definition的`control-module:<ControlModuleId>`，Float32/Fixed的`CharacterStateRead`只从当前Tick `WorldBodyState`读取，没有Set端口。作者校验、Document Apply和原生技能编译都执行同一owner约束。正式`btsmtl.validate` job `74f837ccb89a4a76832201242dded22f`成功返回`compileSuccessCount=1`、`semanticValidCount=1`；运行中的输入／Command行为仍不由此项代替。
- [x] 4.5.2 接入GAS式Ability Attribute、GameplayEffect、GameplayTag和ActivationData访问合同；验证跨GA状态不通过某个Skill Graph隐式共享。证据：`BtsmtlSkillProviderContract`统一ControlModule、InputProfile和GameplayEffectProfile owner分类；Skill Graph/Document/Leaf Emitter共用该合同，正式validate编译通过且GameplayEffectAggregate进入统一Program State。

  当前代码：Attribute、GameplayTag、GameplayEffect节点与`GameplayEffectStateAggregate`、Program catalog及ActionContext合同已接入；`BtsmtlSkillProviderContract`统一并强制Input/TargetData、Ability Attribute/GameplayTag/GameplayEffect和Character State的owner归属，Skill Local仍按图owner保存，跨Skill共享不经过某个Skill Graph。正式`btsmtl.validate` job `ae827fb415d84b5aa03e0d33322db42e`在该合同下成功完成编译、双Target State Layout和State/Snapshot codec核对；运行中的GA生命周期仍归8.1运行证据。
- [x] 4.5.3 将Skill Local、State、ActionInstance、Frame和Ability provider映射到统一Program State Layout；验证Float32、Fixed、Snapshot和State Hash使用同一稳定身份。证据：正式validate以Float32+Fixed逐槽核对1127个StateSlot，并对两种Target执行State、StateHash、World Snapshot canonical写回读回和Program/Layout绑定校验。

  当前代码：`CharacterSemanticBlackboardEmitter`、`ProgramExecutionLayout`、Action/SkillExecution、GameplayEffect aggregate和Float32/Fixed StateCodec共用Program State slot、LayoutHash、ProgramHash与CharacterStateHash；正式`btsmtl.validate`已用Float32+Fixed DryRun逐槽核对1127个StateSlot，并对两种Target执行State、StateHash和World Snapshot canonical round-trip，验证Actor Snapshot按对应Program/Layout解码。两种数值Target继续保留各自的ProgramHash、LayoutHash和状态Hash。
- [ ] 4.5.4 将Character Program接入Session Composition和Simulation Pipeline；验证ProgramRuntime、ExecutionBackend、WorldSolver、Pipeline Pass和SessionSource通过正式兼容校验。

  当前代码：`SimulationSessionCompositionDefinition`、`SimulationPipelineCompiler`、Float32/Fixed Composer及Pass Factory已形成唯一组合链，兼容检查覆盖NumericProfile、Target ABI、Backend、Solver、Source Pass/Port和ExecutionSupport；`1969a55e6`已把编辑器检查改为显式“刷新兼容性”入口，避免在`OnInspectorGUI`重绘中执行编译。正式 Corin 组合结果仍待通过该入口取得，不能用作者validate替代。
- [ ] 4.5.5 接入Rollback与Server Authority的正式Pass/Adapter；验证网络只传Input、Canonical Request、Hash和Snapshot，不复制Graph、Blackboard名称、Timeline对象或最终Pose。

  当前代码：Rollback与Server Authority各自通过Session Source、Pipeline Pass、Snapshot/Canonical Codec和Network Adapter接入同一Program；网络编解码只保存Input、Request、Program/Layout/State Hash、Snapshot与Output disposition。`RollbackInputCodec.ReadInput`、`ServerAuthoritativeCanonicalCodec.ReadInput/ReadBaseline`及其上层Egress入口均要求解码后重新编码与原字节完全一致；仍尚缺本变更的正式网络产品运行记录。

## 5. Document v7和资产迁移

Document代码范围：5.1—5.3，以及2.1.2的导入身份、2.2.3的能力目录、2.3.4的统一Mutation和3.2.3的Validator接入。作者此前指定的「agent工具」任务已提交035057efd与a7a21da08的基础代码；本任务随后完成类型集成修正、A方案根资产所有权和部分能力规则统一。现行协议为v7，剩余集成继续归本change，不是等待另一任务尚未开始实施，也不表示这些整项已完成。

Document必须直接读写Definition.SkillGraphs所引用的原生技能图；普通图、Macro接口、调用节点、稳定步骤端口、黑板声明、Timeline和TreeClip都使用同一包、同一hash和现有Mutation dispatcher。具体合同见design.md第6节，不增加独立交接文档或第二套写入服务。

5.4—5.6属于最后的资产阶段。5.4与5.5的迁移计划、apply和工作包发布已经完成；5.6仍未运行Character Build，未获得对应Build证据的任务保持未勾选。

- [x] 5.1 定义v7图、Macro接口／调用／owner分片与manifest闭包，统一Exporter、Codec、Mapper和严格旧包拒绝；通过正式checkout及schema校验核对。证据：2026-09-09正式CLI checkout 返回 v7、`success=true`、`syncState=Clean`，package 为精确 Character Definition。
- [x] 5.2 同步Reconciler、Mutation、资产resolver、Validator及五生命周期说明，取消旧作者对象中转；通过无业务变化dry-run的零修改清单核对。证据：2026-09-09正式CLI dry-run 返回 `success=true`、`plannedDiff=[]`、`syncState=Clean`，并返回有效 plan/document hash。
- [ ] 5.3 完成根、私有Macro及实际修改共享owner的保存／反向导出和完整回滚；交付事务owner与失败恢复证据。

  当前进度：8480a753e已把技能根创建改为独立主资产，Definition只引用根；私有图与Timeline核对调用方文件归属，新根路径冲突预检及apply失败文件清理已接原Document事务。完整owner集成、实际保存／反向导出／回滚证据仍未完成；Corin三项Skill根、四项SharedGraph和Definition RootTree已由`b28cbd1ae`完成最终迁移并删除旧根资产。
- [x] 5.4 生成精确技能及其引用闭包迁移计划，核对stable identity、布局和资源引用；发生实际冲突按作者选择处理，不自动覆盖。

  交付物：skill-migration-plan.md。计划固定Attack/DodgeBack/DodgeForward旧入口、闭包规模、共享Timeline GUID、确定性根路径、能力映射、identity/layout/owner核对和apply顺序；实际冲突已按正式dry-run结果处理，拒绝的旧工作包已整体删除并由当前v7包重建。
- [x] 5.5 显式应用迁移、发布v7工作包并通过同hash dry-run／apply／重新checkout；成功证据必须包含applied、saved和Clean。证据：正式 Timeline 合同迁移后 checkout/dry-run 返回 `plannedDiff=[]`，以 `bb5af1f5514f1990b9da427799b81b0e253d32b5d38fa4a76b3457457e5fa169` 执行 apply，返回 `applied=true`、`saved=true`、`syncState=Clean`，随后 re-checkout 仍为同一 `documentHash` 与 `Clean`。
- [ ] 5.6 经唯一Character Build发布所需Numeric Target和Projection，核对产物组身份及依赖一致；不复用其他worktree生成资产冒充本批输出。

## 6. 编译来源及运行观测

- [x] 6.1 实现Node／Port／Edge到operation／value／state及调用点路径的编译来源映射，支持一对多及优化标记；通过编译输出核对映射完整性。

  代码/审计证据：ProgramSourceMapEntry记录Node/Port/Edge/State/调用路径并支持一对多；新增OptimizedAway目标和Builder声明合同，Runtime只显示作者来源不捏造执行操作。正式产物输出对账仍由8.1/8.4.2覆盖。
- [x] 6.2 将映射纳入现有产物组发布和哈希校验，区分作者版本与运行版本；通过版本不匹配诊断证明不投射错误节点。

  代码/审计证据：Semantic IR、Float32 Program和Fixed Program的canonical hash链包含SourceMap；RuntimeDebugProgram按ContentHash/SourceMap identity拒绝RevisionMismatch，作者版本与运行版本不混投。正式产物和运行记录由8.1/8.4.2补证。
### 6.3 技能生命周期与调用身份

- [x] 6.3.1 浮点／定点trace发布技能根generation，与节点activation generation分开保留，并同步诊断payload差异判定。证据：87f16e650。
- [x] 6.3.2 从真实进入、完成及停止事件读取节点阶段，不把日志severity当成功或失败。证据：01f254500。
- [x] 6.3.3 发布完整Macro调用路径及调用代次、实际经过的边和等待原因，保持原技能状态所有权；用正式执行记录对账。

  代码/审计证据：Float32/Fixed diagnostics从实际ExecutionRecord发布Macro CallSite/调用代次/父代次、ControlFlowEdge、端口值和operation_waiting；SubGraph拥有标准生命周期槽，停止走正式子图分支。实际执行记录由8.1补证。

### 6.4 订阅、缓存与采集开销

- [x] 6.4.1 实现观察适配器对既有Graph／StateMachine通道的兴趣订阅、当前图缓存及失配清空代码。证据：01f254500。
- [ ] 6.4.2 接入端口值采集、事件覆盖／缺口标记及完整关闭释放验证；记录关闭、节点观察、值观察三档实际开销和限制。

  当前进度：Float32／Fixed在实际输入读取和输出求值后记录typed值，经OperationPort映射发布到现有Values通道。按Actor订阅开启，关闭时不创建值记录；每次求值最多4096个已登记端口采样，超限发布角色级缺口。实时状态及变化队列各保留最多4096项，淘汰触发完整同步并保留覆盖标记；新Tick的相同值仍更新采样时间。原生工具栏提供值采集开关，悬停只显示保留值与Tick。关闭释放代码已接，实际三档开销及运行证据尚未完成。

## 7. 原生可视化和普通运行观察

### 7.1 原生绘制桥接

- [x] 7.1.1 原生节点高亮、连接状态、状态文字和端口提示读取只读外部观察接口，外部连接绘制不依赖graph.isRunning、不调用Blink运行回调。证据：00761c2aa。
- [x] 7.1.2 实现BtsmtlSkillFlowObservation，将明确实例及版本的生命周期缓存交给原生绘制；未采集端口显示缺失，不调用getter补值。证据：01f254500。
- [x] 7.1.3 从正式技能入口装配观察适配器，接通实际边及端口值来源；审计整条链无StartGraph、getter求值及作者状态写入。现有诊断事件入口提供明确实例，来源导航到原生GraphEditor；独立实例选择与运行证据分别在7.2及8.1完成。

  代码链：真实诊断事件 → RuntimeDebugSourceNavigator → 原生GraphEditor与BtsmtlSkillFlowObservation；运行侧通过CharacterPortValueDiagnostics及CharacterControlFlowDiagnostics提供实际值和边。绘制只读事件快照，未调用StartGraph、BindPorts或原生getter，未向作者节点写入执行状态。f0a6115ee已补Host及原生工具栏的实例选择代码；完整运行证据仍归7.2及8.1。

### 7.2 精确运行实例选择

- [x] 7.2.1 诊断事件按真实角色和Session定位来源，Pinned失配保留原目标，同图多实例拒绝默认选第一个；同来源hash变化保留Pin。证据：009581b66、01f254500；底层资源解析已增加正式原生技能闭包，旧图解析仅用于尚未迁移的其他来源。
- [ ] 7.2.2 在正式原生技能入口实现角色、释放实例、generation及调用路径选择，区分瞬时经过、持续等待和终止，并交付共享子图多调用的独立观测证据。

  当前进度：f0a6115ee已在Host和原生工具栏提供实际SkillExecution选择，显示释放与调用代次、调用路径，菜单回调核对Actor和Session；Fixed Host通过正式Program的DefinitionGuid定位作者。瞬时／等待／终态区分及共享子图多调用的完整运行证据仍未完成，不能因入口代码存在而勾选。
- [ ] 7.3 将父调用节点、子图页面和breadcrumb绑定同一运行调用，结束实例保留明确终态；通过观测记录核对不自动混入新释放实例。

  当前进度：观察会话沿原生导航来源与编译父路径逐层解析，限定同一Actor／Session／ActionInstance／技能根代次并核对父调用代次；重复进入按最近真实记录解析，未执行时只订阅等待。父调用返回使用已记录父代次；Timeline打开带明确调用上下文，TreeClip前进／返回保留节点及clip位置。实例选择已有7.2.2所述代码；结束实例保留终态、不混入新释放及导航一致性的运行证据仍未完成。
- [ ] 7.4 接入Unity Play状态、真实角色及ActionInstance的注册／结束／generation变化，自动发现并绑定明确目标；交付生命周期与目标选择记录，证明不创建预览实例或独立场景。
### 7.5 观察生命周期

- [x] 7.5.1 实现适配器换页、关窗和退出Play时解绑及释放缓存的代码，不驱动播放、单步或私有时钟。证据：01f254500。
- [ ] 7.5.2 接入正式窗口后核对Unity暂停保留结果、关闭与退出解绑；审计无私有时钟、窗口单步或断点，若保留释放按钮，只能调用正式技能请求入口。

## 8. 集成证据及清理

- [ ] 8.1 使用现有正式CLI完成技能启动／Timeline／正常结束及中断链的编译运行对账，记录业务差异；不新增测试代码或临时执行器。
- [x] 8.2 核对各旧作者模型、窗口、端口和转换层的消费者，删除零消费者实现；交付残留清单并说明未迁移领域的合法保留原因。

  残留清单见implementation.md“旧路径消费者审计”：Character RootTree已删除，Character Document不再生成顶层Blackboard、旧Graph或旧Timeline分片；零消费者的一次性迁移工作流、顶层Blackboard模型、快照旧摘要和通用Timeline资产猜测入口已删除（`b0d4704fa`、`18cf59f22`）。AI不再由BTSMTL拥有，Behavior Designer插件是后续AI唯一作者入口。源码仍保留的`OneRootTree`、`SubTree`和`TimelineRunningTree`只服务TreeDesigner/Skill Timeline内部通用作者模型，不是Character Definition入口。技能正式编译/观察不得有旧作者对象中转。文件命名同步见`6e4abfb91`。
- [x] 8.3 同步project、current specs、技能说明和原change替代记录，统一v7版本及作者／运行边界；运行OpenSpec严格校验并附规范冲突复核结论。证据：`595e09dc8`同步Character无RootTree规范，`5fb20d1e0`同步Skill FlowCanvas规范，`b012bf211`删除旧自研AI链，current spec改为Behavior Designer边界；change严格校验通过。Character入口为SkillDefinitions/SkillGraphs，AI不再保留BTSMTL RootTree、AI Program或AI Document。
### 8.4 交付与真实进度

- [x] 8.4.1 将当前代码拆成五个独立提交并记录盘点、脚本刷新事实、未接通及未验证项。证据：b3d066d12及implementation.md。
- [ ] 8.4.2 最终汇总代码与资产提交、正式CLI证据及各项结论；编译成功不得代替交互、运行观察或性能完成，所有任务按真实证据收口。
- [x] 8.5 核对Character RootTree删除后的Definition、GA精确导航、provider Blackboard、Simulation Pipeline和Network Adapter规范一致性；运行OpenSpec严格校验并输出仍存在的非技能RootTree残留清单。证据：`559c476b9`、`63d0ccfe3`、`dc98db8b4`、`2dd98c9cc`、`aa5b99a56`、`eb9dd7719`及目标Unity v7 checkout/dry-run；源码与包扫描无Character RootTree字段/路径，OpenSpec strict通过。

  当前残留清单：旧`AgentAI*`导出器、lowering、handler、AI事务owner和BTSMTL AI operation均已删除；`AIController` domain已不再被Document schema、MCP或Validator接收。Character Definition、Skill导航、provider Blackboard、Session Composition、Pipeline和网络Adapter均不再以Character RootTree作为入口；正式v7 checkout/dry-run已核对`Clean`且包内无顶层旧分片。源码中的`OneRootTree`、`SubTree`和`TimelineRunningTree`是TreeDesigner/Skill Timeline的非技能通用类型，不属于Character Definition入口。Behavior Designer插件尚未在本change中接入角色任务和网络Source，后续由`replace-btsmtl-ai-with-behavior-designer`独立change负责；不在此处建立第二套AI路径。Character State只读projection已接入并升级Gameplay operation set到`/15`。正式运行一致性和网络产品证据归8.1/8.4.2，仍未执行。

