## Why

2026-09-12 FSM规划更新：本change负责原生NodeCanvas FSM作者接入、对应Document v8/编译适配和Corin残余清理；普通执行图继续使用FlowCanvas，状态机子图完整替换为原生FSM，不双存。共同业务参数与节点规则消费 [unify-skill-authoring-data-model](../unify-skill-authoring-data-model/proposal.md) 的成果，不把FSM迁移塞入其范围；旧Step/Edge阶段成果由add-skill-transfer-connections提供。本次只更新本change文档，不覆盖其它窗口正在调整的规划。旧v7与任务勾选只代表历史阶段，原生FSM和v8尚未交付。

BTSMTL技能需要成熟的节点、端口、参数化子图、动作时间轴和运行观察能力，同时保留既有技能编译、ActionInstance、预测、回滚及状态恢复链。本变更把技能收敛为类似Gameplay Ability的独立业务单元，但继续使用项目自己的确定性Simulation与网络管线。

Character RootTree已经不是有效的角色主线入口。角色主线由C# ControlModule和Simulation Pipeline负责，Skill Graph只拥有技能执行拓扑；RootTree删除后，作者、编译、运行和网络边界必须在规范中明确分开。PoseGraph仍由[独立提案](../integrate-pose-flowcanvas-editor-preview/proposal.md)管理，但技能Timeline可以承载类似Montage的有限动作动画，最终混合仍由Presentation/PoseGraph完成。

## What Changes

- **BREAKING**：Character不再有Character RootTree主流程、黑板或技能发现入口；`CharacterPipelineDefinition`只装配C#控制合同、SkillDefinitions、SkillGraphs、输入、效果、表现和生成产物。AI不再由BTSMTL拥有，Behavior Designer由独立change负责。
- **BREAKING**：每个GA式Skill只有一个稳定Entry Graph。`CharacterSkillAuthoringDefinition`是激活与业务合同外壳，Skill FlowGraph是执行体，Macro、State、Condition和Timeline属于该根的正式闭包。
- **BREAKING**：StateMachine子图使用原生FSM，State/FSMConnection只保存一份状态与转移数据；删除被替代的Skill自定义状态图、旧State/anchor存储及转换补读。Skill根、StateBody、ConditionRule、Sequence/Parallel、Macro和Timeline保留各自业务，不全部改成FSM。
- Entry、Exit、Any、FSM整体钩子与State生命周期分别映射BTSMTL合同；无条件边不得照搬插件OnFinish，未登记ActionList/ConditionTask、Stacked/Clean语义不开放。运行不启动NodeCanvas/FlowCanvas任务或GraphOwner。
- 清理Attack无消费Setup包装、重复意图声明及其私有闭包，保留全部连段/退出条件、优先级、中止策略和稳定顺序；Dodge有效并行监控、动作窗口和目标合同按真实消费者保留。StopThreshold归属与攻击清除正式跑步意图不在迁移中擅自改变。
- 技能编译继续沿`Skill Graph -> Semantic IR -> Numeric Program -> Simulation Session`执行，禁止启动FlowCanvas委托、协程或自动Update；ActionInstance、generation、PredictionKey和中断恢复保持唯一运行链。
- Skill Timeline保留类似Montage/AbilityTask的动作时序，可拥有AnimationTrack、AnimationClip、Action Slot、混合进入退出请求、命中窗口和取消窗口；Presentation/PoseGraph负责Locomotion混合、Layer、IK和最终Pose，不由GA直接写Pose。
- Blackboard不再被定义为一个万能共享字典。C# Character State作为只读typed projection；会被GameplayEffect修改的数值进入Ability Attribute；状态准入进入GameplayTag；输入与目标进入Input/TargetData；GA私有值进入Skill Local Blackboard；State、ActionInstance和Frame值按生命周期隔离。
- FlowCanvas原生`GraphEditor`是Skill authoring的唯一UI宿主：画布、Toolbar、原生Blackboard、Inspector、创建菜单、变量拖拽、selection、Undo和下钻均由原生表面承载；Skill domain adapter只提供provider catalog、typed payload、Mutation和diagnostics，不新增Skill专用UI Toolkit右栏或旁路编辑器。统一Blackboard视图可以投影多个正式provider，但每个provider保留自己的owner、稳定ID、读写权限、生命周期、预测和回滚合同；不得把跨GA状态隐藏到某个Skill Graph，也不得通过变量名或反射猜测owner。
- Character Program与Simulation Pipeline分离。Character Pipeline只提供Program和Actor注册；Session Composition选择ProgramRuntime、ExecutionBackend、WorldSolver、SimulationPipeline和SessionSource；网络通过Rollback或Server Authority Pass/Adapter接入，不绑定UE或单一传输。
- 网络同步输入、canonical request、预测身份、确定性Program State、Hash和Snapshot，不复制FlowCanvas图、Blackboard名字、Timeline对象或最终Pose，不为每个技能变量增加独立RPC。
- 普通Skill Graph复用FlowCanvas原生端口、节点交互、Macro接口和导航；状态机使用同一GraphEditor中的原生FSM适配。Capability统一提供字段、类型、role及编译合同，未登记能力不开放；所有写入口进入真实owner事务。
- **BREAKING**：本次FSM外部合同以Document v8原子替代v7，包含稳定状态/转移identity、逻辑端点、edge条件owner、order和生命周期引用。Unity资产仍是正式来源，Document只作工作副本；Exporter/Reconciler/Mutation/Validator共同适配，不增加JSON直编、第二事务或旧版兼容reader。非Skill分片只切换整包版本，不重做业务。
- 原子迁移精确技能闭包，删除被替代且无消费者的Character RootTree入口；Pose和未迁移的非AI领域保留各自合法模型，Behavior Designer不通过兼容开关恢复旧BTSMTL AI路径。

## Capabilities

### New Capabilities

- `btsmtl-flowcanvas-authoring`：技能正式图、单根GA式技能、能力目录、Timeline/Montage式动作、分层变量访问、直接编译、迁移与唯一写入。
- `btsmtl-flowcanvas-runtime-observation`：技能来源映射、实际释放实例、网络Session/ActionInstance定位、子图定位和Play Mode下的原生只读显示。

### Modified Capabilities

- `btsmtl-graph-core`：技能退出旧BaseGraph与TreeWindow作者链；未迁移领域继续原合同。
- `graph-authoring-domain-framework`：技能采用原生作者基础、provider化变量访问和统一Mutation，保持能力、事务与领域隔离。
- `graph-authoring-editor-shell`：技能正式入口改为Skill Graph和FlowCanvas原生provider-aware Blackboard/Inspector表面，其他领域不随之迁移。
- `btsmtl-agent-authoring-document-sync`：v8统一表达FlowGraph/FSM、Macro、Timeline、TreeClip和声明owner；保留唯一事务与非Skill业务形状，正式入口拒绝v7及更早版本。

## Impact

影响Skill Graph作者、Blackboard provider目录、Timeline与表现输出合同、Gameplay编译前端、Simulation Pipeline Composition、Rollback/Server Authority适配、AgentAuthoring、技能诊断及必要框架扩展。Character RootTree资产和Character RootTree作者入口被删除；AI自研链路由独立替换change退役，Pose作者模型、Pose编译器、Pose runtime、动画算法和IK最终实现不被本项替换。

规范和在途变更对账见[design.md](design.md)。本文件由原混合变更`unify-flowcanvas-authoring-and-compiled-debug`拆出并更名，当前增量继续归入本Change；实现代码和真实资产证据由后续apply与收口任务完成。
