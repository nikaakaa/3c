## 1. 基线与依赖

- [x] 1.1 重新读取本change的proposal、design、tasks和全部spec delta。
- [x] 1.2 记录当前BaseTreeWindow、TreeWindowUtility、page stack和window type实例规则。
- [x] 1.3 记录当前BaseTree、OneRootTree、StateMachineGraph、ConditionRuleGraph的节点创建规则。
- [x] 1.4 记录Node Search、拖拽、粘贴、脚本和Agent调用CanCreateNodeType的入口。
- [x] 1.5 盘点可复用Flow、Pure Value、Blackboard与Debug节点。
- [x] 1.6 盘点必须禁止进入AI Graph的Character、Timeline、Motion、Animation与GameplayEffect节点。
- [x] 1.7 记录CharacterPipelineAuthoringContext与Graph Data Catalog扩展合同。
- [x] 1.8 记录ISimulationInputAdapter当前只能读取Actor、Tick、Numeric Profile与sequence的合同。
- [x] 1.9 记录LocalInputIngressPass当前Source/Program Runtime read port、CanonicalInputBatch唯一writer与ExternalSource state class。
- [x] 1.10 记录Execution Backend的outer transaction、Pipeline participant checkpoint、restore与atomic publish合同。
- [x] 1.11 记录CharacterSimulationInput value/request catalog、sequence与source tick合同。
- [x] 1.12 确认add-corin-targeted-motion-warp-demo已经完成Control Source factory、typed ActionTargetSnapshot input和训练敌人Actor。
- [x] 1.13 记录MotionWarp Demo玩家target provider读取Actor registration Body缓存的入口与所有消费点。
- [x] 1.14 记录当前Agent v14只支持Character Controller，并确认本change不修改其Snapshot、Patch、Validator、MCP或技能合同。
- [x] 1.15 确认不存在已安装AI Controller、AI Program或MonoBehaviour Bot旁路。
- [x] 1.16 确认当前Session没有正式Team/Faction或任意公开Gameplay observation fact所有者。
- [x] 1.17 确认本change可沿唯一BTSMTL、Program、Control Input Ingress和Session事务链完成。

## 2. Node Authoring Capability

- [x] 2.1 定义稳定NodeAuthoringCapability值与identity。
- [x] 2.2 定义Graph Role到允许capability的正式policy合同。
- [x] 2.3 为共享Sequence、Selector、Parallel与Root节点声明SharedFlow。
- [x] 2.4 为允许的Decorator节点声明SharedFlow。
- [x] 2.5 为已具备portable AI语义的Compare与Logic节点声明SharedPureValue；Math/Vector节点在安装对应operation前保持拒绝。
- [x] 2.6 为通用Blackboard节点声明SharedBlackboard。
- [x] 2.7 为只读Debug节点声明EditorOnlyDebug。
- [x] 2.8 为Character执行节点声明CharacterExecution。
- [x] 2.9 为Timeline Decision节点声明TimelineDecision。
- [x] 2.10 让BaseGraph节点创建统一查询Graph policy。
- [x] 2.11 让Node Search只显示当前Graph policy允许的节点。
- [x] 2.12 让drag/drop与paste拒绝不兼容capability。
- [x] 2.13 让脚本创建和Compiler Validator复用同一policy，并暴露后续Agent可消费的正式查询入口。
- [x] 2.14 让未知或缺失capability在AI Graph中明确失败。
- [x] 2.15 删除AI专用Type名单或NodePath推断分支。

## 3. AI Definition与Graph

- [x] 3.1 定义AIControllerDefinition稳定ControllerId。
- [x] 3.2 增加AI RootTreeAsset正式引用。
- [x] 3.3 增加受控CharacterPipelineDefinition正式引用。
- [x] 3.4 增加AIPerceptionProfile正式引用。
- [x] 3.5 增加generated AIIntentProgramAsset正式引用与identity状态。
- [x] 3.6 定义AIControllerTree为OneRootTree的AI Graph role。
- [x] 3.7 初始化AIControllerTree唯一RootNode。
- [x] 3.8 让AIControllerTree只允许Shared与AI capability。
- [x] 3.9 禁止AIControllerTree创建CharacterExecution节点。
- [x] 3.10 禁止AIControllerTree创建TimelineDecision节点。
- [x] 3.11 保持AI Tree由BaseTreeAsset持有唯一Graph数据。
- [x] 3.12 增加AI Controller Definition创建菜单与默认空资产。
- [x] 3.13 增加AI RootTree创建与显式绑定流程。
- [x] 3.14 校验Definition、RootTree、Character与Perception引用完整。
- [x] 3.15 校验RootTree实际类型为AIControllerTree。

## 4. AI编辑窗口

- [x] 4.1 定义AIControllerTreeWindow薄派生类型。
- [x] 4.2 让TreeWindowUtility按AI Graph打开AI窗口类型。
- [x] 4.3 保持Character BaseTreeWindow与AI窗口可同时存在。
- [x] 4.4 复用BaseTreeView且不创建AI GraphView副本。
- [x] 4.5 复用navigation controller、page stack与breadcrumb。
- [x] 4.6 复用selection、Undo、dirty和serialized owner处理。
- [x] 4.7 复用Graph Data Catalog基础与Blackboard交互。
- [x] 4.8 增加AI窗口标题、Controller identity和Graph role标识。
- [x] 4.9 定义AIControllerAuthoringContext。
- [x] 4.10 在AI Inspector显示Definition、Perception与Character input contract。
- [x] 4.11 在Definition Inspector增加Open AI Tree命令。
- [x] 4.12 直接打开孤立AI Tree时显示缺失context状态。
- [x] 4.13 保持AI子Graph下钻继承同一authoring context。
- [x] 4.14 保持窗口布局、页栈和选择不写入业务资产。
- [x] 4.15 删除任何AI双画布、永久Character侧栏或Workbench试验入口。

## 5. AI Blackboard

- [x] 5.1 定义AI Blackboard Controller scope。
- [x] 5.2 定义AI Blackboard Tick scope。
- [x] 5.3 保持Graph scope局部owner语义。
- [x] 5.4 定义AI Blackboard value kind catalog。
- [x] 5.5 支持稳定ActorId与ActionTargetSnapshot值。
- [x] 5.6 编译Controller scope到AIControllerState地址。
- [x] 5.7 编译Tick scope到单次Evaluate workspace地址。
- [x] 5.8 在每次AI Evaluate开始清理Tick scope。
- [x] 5.9 保持Controller scope进入AI State snapshot/hash。
- [x] 5.10 禁止AI declaration使用Character/State/ActionInstance scope。
- [x] 5.11 禁止AI Blackboard resolver访问CharacterSimulationState。
- [x] 5.12 扩展AI Graph Data Catalog创建、分类和引用declaration。
- [x] 5.13 显示declaration owner、scope、type和runtime value。
- [x] 5.14 删除任何AI Memory写入Character Pipeline Blackboard的入口。

## 6. Committed Observation与Perception合同

- [x] 6.1 定义portable CommittedActorObservationSnapshot identity与schema。
- [x] 6.2 定义ObservationTick与locked roster identity。
- [x] 6.3 定义每个Actor observation的ActorId与最近committed Logic Body。
- [x] 6.4 保持Actor observation按稳定ActorId排序且不可变。
- [x] 6.5 定义正式Committed Observation read port descriptor与capability。
- [x] 6.6 从outer transaction开始前的committed World state构建唯一snapshot。
- [x] 6.7 定义portable AIPerceptionFrame。
- [x] 6.8 定义AIPerceptionProfile的显式候选ActorId绑定。
- [x] 6.9 定义显式候选过滤与稳定排序配置。
- [x] 6.10 为全部AI Actor从同一observation tick构建PerceptionFrame。
- [x] 6.11 定义Self、configured candidate与selected target observation访问器。
- [x] 6.12 拒绝缺失Self、重复Actor与不存在的显式候选ActorId。
- [x] 6.13 拒绝读取当前Tick部分World result。
- [x] 6.14 拒绝读取Character私有State、VisualRoot、Animator root或Camera。
- [x] 6.15 拒绝Scene、Tag、名称、全局registry与ActorId前缀推断。
- [x] 6.16 禁止第一版Observation声明Team、Faction或任意公开Gameplay fact字段。
- [x] 6.17 将Observation与Perception schema纳入AI Program compatibility identity。
- [x] 6.18 输出Observation Tick、缺失Self、重复Actor与无效候选的typed诊断。

## 7. AI节点Authoring

- [x] 7.1 增加ReadSelfObservation节点。
- [x] 7.2 增加EnumerateConfiguredCandidates节点。
- [x] 7.3 增加SelectNearestCandidate节点。
- [x] 7.4 增加ReadTargetDistance节点。
- [x] 7.5 增加ReadTargetDirection节点。
- [x] 7.6 增加ReadAIMemory与WriteAIMemory节点。
- [x] 7.7 增加WriteContinuousInput节点。
- [x] 7.8 增加WriteActionTargetSnapshot节点。
- [x] 7.9 增加SubmitActionRequest节点。
- [x] 7.10 为全部AI节点声明稳定NodePath和AI capability。
- [x] 7.11 Intent节点从受控Character input catalog选择InputId/RequestId。
- [x] 7.12 拒绝自由字符串、显示名或默认request绑定。
- [x] 7.13 校验continuous value kind与Character input kind一致。
- [x] 7.14 校验request identity与timing class存在。
- [x] 7.15 定义离散request每次node activation只提交一次。
- [x] 7.16 定义显式repeat策略产生新request sequence。
- [x] 7.17 禁止AI节点直接创建ActionInstance或Timeline playback。
- [x] 7.18 禁止AI节点写Transform、World body或Presentation。

## 8. AI Semantic IR与Program

- [x] 8.1 定义AI Semantic IR schema与canonical identity。
- [x] 8.2 复用BTSMTL Graph/Node/Edge/PropertyPort stable identity。
- [x] 8.3 盘点现有portable control topology、Sequence、Selector、Parallel、Decorator、Running、activation与abort实现。
- [x] 8.4 将仍绑定Character具体类型的control基础提取到唯一portable Core合同。
- [x] 8.5 让Character现有runtime继续消费同一control Core且不改变业务语义。
- [x] 8.6 让AI Frontend与runtime直接复用同一control Core实现。
- [x] 8.7 全局拒绝第二套AI control evaluator、RunnableTree解释器或复制的control state machine。
- [x] 8.8 定义AI Observation operations。
- [x] 8.9 定义AI Memory operations。
- [x] 8.10 定义AI Intent operations。
- [x] 8.11 编译AI value edges与constant bindings。
- [x] 8.12 编译AI Blackboard address layout。
- [x] 8.13 编译Character input/request catalog binding。
- [x] 8.14 定义AIIntentProgramId、ProgramHash与LayoutHash。
- [x] 8.15 定义Float32 AIIntentProgram canonical codec。
- [x] 8.16 定义AIControllerState与state codec。
- [x] 8.17 将control node state、Controller Blackboard和request sequence写入AI State。
- [x] 8.18 保持Tick workspace不进入committed AI State。
- [x] 8.19 构建ProgramExecutionLayout与复用workspace。
- [x] 8.20 禁止runtime读取authoring Graph或创建RunnableTree clone。
- [x] 8.21 拒绝未知AI operation或不支持Numeric Target。
- [x] 8.22 发布exact-byte generated AI Program asset与source revision。

## 9. AI运行时与Character Input

- [x] 9.1 在MotionWarp Demo提供的统一Control Source合同上增加显式runtime capability要求。
- [x] 9.2 定义AI Control Source的ControllerId、ActorId、AI Program与Character Program binding。
- [x] 9.3 定义AI Program Evaluate输入为PerceptionFrame与正式AIState。
- [x] 9.4 在Evaluate前从正式AIState创建candidate state。
- [x] 9.5 定义AI Program输出builder的单Tick生命周期。
- [x] 9.6 从Character Program catalog生成完整continuous input模板。
- [x] 9.7 应用AI写入的Move/Target等typed values。
- [x] 9.8 Finalize时生成prepared CharacterSimulationInput。
- [x] 9.9 生成稳定InputSequence与SourceTick。
- [x] 9.10 在candidate AIState中生成稳定Action request sequence。
- [x] 9.11 保持未写continuous值为catalog neutral值。
- [x] 9.12 保持未提交request时Requests为空。
- [x] 9.13 同一node activation持续Running时不重复生成离散request。
- [x] 9.14 显式repeat或新activation才推进新request sequence。
- [x] 9.15 定义candidate AIState的commit、discard与checkpoint restore合同。
- [x] 9.16 将AIState canonical bytes、schema与hash接入Pipeline participant状态。
- [x] 9.17 禁止创建AICommand、BotAction或第二request buffer。
- [x] 9.18 AI Evaluate失败时丢弃全部candidate state和prepared input。
- [x] 9.19 禁止失败时回退Neutral、上一Tick input或空request。
- [x] 9.20 保持Character Program只看到普通CharacterSimulationInput。
- [x] 9.21 让玩家显式ActionTarget selector从CommittedActorObservationSnapshot解析绑定ActorId。
- [x] 9.22 删除玩家target provider读取Actor registration Body缓存的入口。

## 10. Local Session接入

- [x] 10.1 定义Local Control Source roster descriptor并保存每个Actor的source identity与capability。
- [x] 10.2 让Session Preparation把完整Control Source roster写入不可变Launch Plan。
- [x] 10.3 Session Active前验证Control Source roster与Program roster ActorId一一对应。
- [x] 10.4 Session Active前验证AI Program、Character Program与Numeric ABI binding。
- [x] 10.5 Session Active前验证Committed Observation capability与schema。
- [x] 10.6 为Local Pipeline安装唯一Committed Observation target read port。
- [x] 10.7 从outer transaction开始前的committed World state填充Observation port。
- [x] 10.8 将LocalInputIngressPass提升为唯一Local Control Input Ingress。
- [x] 10.9 为Ingress绑定Program Runtime、Control Source与Committed Observation read port。
- [x] 10.10 保持Ingress为CanonicalInputBatch唯一writer。
- [x] 10.11 将拥有AI State的Ingress声明为正式SnapshotParticipant。
- [x] 10.12 按稳定ControllerId和ActorId捕获AI State checkpoint。
- [x] 10.13 从同一Observation Snapshot为全部AI Actor构建PerceptionFrame。
- [x] 10.14 按稳定ActorId准备Player、Neutral与AI输入。
- [x] 10.15 保持Player target selector与AI Perception读取同一Observation实例和Tick。
- [x] 10.16 在任何Character Evaluate前验证并冻结完整CanonicalInputBatch。
- [x] 10.17 任一Control Source失败时恢复全部已推进AI State。
- [x] 10.18 任一后续Pass失败时通过outer transaction恢复Tick前AI State。
- [x] 10.19 outer state publish成功后提交全部candidate AI State。
- [x] 10.20 将AI State participant写入Session snapshot、state hash与restore合同。
- [x] 10.21 保持所有Character Actor继续进入同一ResolveBatch。
- [x] 10.22 保持AI Actor使用同一WorldSolver与Presentation。
- [x] 10.23 不支持AI的Composition遇到AI actor时在Active前明确拒绝。
- [x] 10.24 禁止Network client本地推断Authority Bot输入。
- [x] 10.25 删除Local Session中的AI具体类型、Actor名称、Tag与fallback判断。
- [x] 10.26 删除任何AI专用CanonicalInputBatch writer或第二Ingress路径。

## 11. Diagnostics与清理

- [x] 11.1 注册AI Program Live Debug target。
- [x] 11.2 显示Observation tick与候选Actor摘要。
- [x] 11.3 显示active AI node path。
- [x] 11.4 显示AI Blackboard读取与写入。
- [x] 11.5 显示输出InputId、RequestId、InputSequence与SourceTick。
- [x] 11.6 通过ActorId与InputSequence关联Character Debug。
- [x] 11.7 保持diagnostics只读且不重新运行Perception或AI。
- [x] 11.8 显示AI candidate state的prepared、committed、discarded或restored状态。
- [x] 11.9 删除任何MonoBehaviour Bot决策脚本。
- [x] 11.10 删除任何AI Scene Transform、Tag或名称查询。
- [x] 11.11 删除Character RootTree中的AI试验SubTree。
- [x] 11.12 删除通用RunnableTree正式执行AI的入口。
- [x] 11.13 删除第二套AI control evaluator与复制的control state machine。
- [x] 11.14 删除AI失败回退Neutral的分支。
- [x] 11.15 更新openspec/project.md与2v2vE文档的AI核心状态，并明确Agent v14和Corin Neutral demo仍未迁移。
- [x] 11.16 全局搜索确认没有第二GraphView、AICommand、第二request buffer或第二CanonicalInputBatch writer。

## 12. 编译与规范校验

- [x] 12.1 构建BTSMTL Runtime与Editor程序集并禁用build server/shared compilation。
- [x] 12.2 构建AI portable Core与Float32程序集并使用相同参数。
- [x] 12.3 构建Character Runtime与Editor程序集并使用相同参数。
- [x] 12.4 每次编译后立即执行dotnet build-server shutdown。
- [x] 12.5 运行AI Semantic IR与Program artifact验证。
- [x] 12.6 运行`openspec validate add-btsmtl-ai-controller-authoring --strict --no-interactive`。
- [x] 12.7 核对tasks勾选与最终统一链路一致。


