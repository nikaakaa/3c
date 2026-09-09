## Why

BTSMTL技能需要成熟的节点、端口、参数化子图、动作时间轴和运行观察能力，同时保留既有技能编译、ActionInstance、预测、回滚及状态恢复链。本变更把技能收敛为类似Gameplay Ability的独立业务单元，但继续使用项目自己的确定性Simulation与网络管线。

Character RootTree已经不是有效的角色主线入口。角色主线由C# ControlModule和Simulation Pipeline负责，Skill Graph只拥有技能执行拓扑；RootTree删除后，作者、编译、运行和网络边界必须在规范中明确分开。PoseGraph仍由[独立提案](../integrate-pose-flowcanvas-editor-preview/proposal.md)管理，但技能Timeline可以承载类似Montage的有限动作动画，最终混合仍由Presentation/PoseGraph完成。

## What Changes

- **BREAKING**：Character不再有Character RootTree主流程、黑板或技能发现入口；`CharacterPipelineDefinition`只装配C#控制合同、SkillDefinitions、SkillGraphs、输入、效果、表现和生成产物。AIController RootTree不随本项迁移。
- **BREAKING**：每个GA式Skill只有一个稳定Entry Graph。`CharacterSkillAuthoringDefinition`是激活与业务合同外壳，Skill FlowGraph是执行体，Macro、State、Condition和Timeline属于该根的正式闭包。
- 技能编译继续沿`Skill Graph -> Semantic IR -> Numeric Program -> Simulation Session`执行，禁止启动FlowCanvas委托、协程或自动Update；ActionInstance、generation、PredictionKey和中断恢复保持唯一运行链。
- Skill Timeline保留类似Montage/AbilityTask的动作时序，可拥有AnimationTrack、AnimationClip、Action Slot、混合进入退出请求、命中窗口和取消窗口；Presentation/PoseGraph负责Locomotion混合、Layer、IK和最终Pose，不由GA直接写Pose。
- Blackboard不再被定义为一个万能共享字典。C# Character State作为只读typed projection；会被GameplayEffect修改的数值进入Ability Attribute；状态准入进入GameplayTag；输入与目标进入Input/TargetData；GA私有值进入Skill Local Blackboard；State、ActionInstance和Frame值按生命周期隔离。
- 统一Blackboard面板可以投影多个正式provider，但每个provider保留自己的owner、稳定ID、读写权限、生命周期、预测和回滚合同；不得把跨GA状态隐藏到某个Skill Graph，也不得通过变量名或反射猜测owner。
- Character Program与Simulation Pipeline分离。Character Pipeline只提供Program和Actor注册；Session Composition选择ProgramRuntime、ExecutionBackend、WorldSolver、SimulationPipeline和SessionSource；网络通过Rollback或Server Authority Pass/Adapter接入，不绑定UE或单一传输。
- 网络同步输入、canonical request、预测身份、确定性Program State、Hash和Snapshot，不复制FlowCanvas图、Blackboard名字、Timeline对象或最终Pose，不为每个技能变量增加独立RPC。
- 复用原生端口、节点交互、Macro接口、调用节点和导航。Capability统一提供字段、类型、role及编译合同，未登记能力不开放；所有写入口进入真实owner事务和Document v7。
- Document v7直接读写正式Skill Graph、Macro、Timeline、TreeClip和声明闭包；独立根资产、私有内容、共享owner、接口变化和失败回滚保持一套整包事务。
- 原子迁移精确技能闭包，删除被替代且无消费者的Character RootTree入口；AI、Pose和未迁移领域保留各自合法模型，不通过兼容开关恢复旧Character路径。

## Capabilities

### New Capabilities

- `btsmtl-flowcanvas-authoring`：技能正式图、单根GA式技能、能力目录、Timeline/Montage式动作、分层变量访问、直接编译、迁移与唯一写入。
- `btsmtl-flowcanvas-runtime-observation`：技能来源映射、实际释放实例、网络Session/ActionInstance定位、子图定位和Play Mode下的原生只读显示。

### Modified Capabilities

- `btsmtl-graph-core`：技能退出旧BaseGraph与TreeWindow作者链；未迁移领域继续原合同。
- `graph-authoring-domain-framework`：技能采用原生作者基础、provider化变量访问和统一Mutation，保持能力、事务与领域隔离。
- `graph-authoring-editor-shell`：技能正式入口改为Skill Graph和provider-aware Blackboard区域，其他领域不随之迁移。
- `btsmtl-agent-authoring-document-sync`：v7直接读写正式技能图、Macro、Timeline、TreeClip和声明owner；不写入网络运行状态，非技能分片不改变业务语义。

## Impact

影响Skill Graph作者、Blackboard provider目录、Timeline与表现输出合同、Gameplay编译前端、Simulation Pipeline Composition、Rollback/Server Authority适配、AgentAuthoring、技能诊断及必要框架扩展。Character RootTree资产和Character RootTree作者入口被删除；AI RootTree、Pose作者模型、Pose编译器、Pose runtime、动画算法和IK最终实现不被本项替换。

规范和在途变更对账见[design.md](design.md)。本文件由原混合变更`unify-flowcanvas-authoring-and-compiled-debug`拆出并更名，当前增量继续归入本Change；实现代码和真实资产证据由后续apply与收口任务完成。
