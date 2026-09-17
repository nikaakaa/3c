# 2026-09-14 Pose与Timeline旧编译链删除记录

用户在规划D17后明确要求本窗口直接帮助删除，并进一步限定为Pose／Timeline旧编译。本批执行删除优先，不等待原生运行或直接播放全部接通；不接管整角色迁移和其它窗口正在修改的算法。

## 实际删除

删除28个C#源码文件及其28个.meta，共10,746行源码。原Timeline发射器文件中的技能图编译结果类型原样提取为31行的TimelineSemanticTreeCompilation.cs，另有对应.meta；本批C#源码净减少10,715行，净减少27个C#文件。统计包含空行，不含文档和.meta。

- Pose：16个文件，删除Closure、Typed Lowering、Topology、Symbolic Lowering、Family Payload绑定、Stage Schedule、Value Lifetime／Workspace、Worker Batch Plan、Image Seal、总编译入口／合同，以及Linked Pose／Motion Matching／Inertialization面向旧Image的计划编译。
- Timeline：12个文件，删除独立Timeline编译前端、Semantic发现适配、Emitter registry／上下文／调用合同、根发射，以及动画／相机／运动／Cue／Scene Parameter／TreeClip的轨道和Clip操作码发射。
- 删除的是将作者图或轨道转换为旧执行计划的职责。没有重建替代编译器、兼容类型、默认输出或运行开关。

路径均相对客户端Main下的Editor/CharacterSimulation/Compilation：

- `Presentation/PoseGraph/Passes/CharacterPoseWorkerBatchPlanPass.cs`
- `Presentation/PoseGraph/Passes/CharacterPoseValueLifetimePass.cs`
- `Presentation/PoseGraph/Passes/CharacterPoseTypedLoweringPass.cs`
- `Presentation/PoseGraph/Passes/CharacterPoseTopologyPass.cs`
- `Presentation/PoseGraph/Passes/CharacterPoseSymbolicFamilyLoweringPass.cs`
- `Presentation/PoseGraph/Passes/CharacterPoseStageSchedulePass.cs`
- `Presentation/PoseGraph/Passes/CharacterPoseProgramImageSealPass.cs`
- `Presentation/PoseGraph/Passes/CharacterPoseGraphClosurePass.cs`
- `Presentation/PoseGraph/Passes/CharacterPoseFamilyPayloadBindingPass.cs`
- `Presentation/PoseGraph/Passes/CharacterPoseFamilyPayloadBinding.cs`
- `Presentation/PoseGraph/Passes/CharacterPoseFamilyPayloadAdapters.cs`
- `Presentation/CharacterPoseCompilerModule.cs`
- `Presentation/CharacterPoseCompilationContracts.cs`
- `Presentation/CharacterMotionMatchingPosePlanCompiler.cs`
- `Presentation/CharacterLinkedPoseProjectionCompiler.cs`
- `Semantic/TimelineTreeEmitterRegistration.cs`
- `Semantic/TimelineSemanticRootEmitter.cs`
- `Semantic/TimelineSemanticFrontendCompiler.cs`
- `Semantic/TimelineSemanticEmitterRegistry.cs`
- `Semantic/TimelineSemanticEmissionContracts.cs`
- `Semantic/TimelineSemanticContentDiscovery.cs`
- `Semantic/TimelineSceneParameterEmitterRegistration.cs`
- `Semantic/TimelineMotionEmitterRegistration.cs`
- `Semantic/TimelineCueEmitterRegistration.cs`
- `Semantic/TimelineCameraEmitterRegistration.cs`
- `Semantic/TimelineAnimationEmitterRegistration.cs`
- `Semantic/CharacterSemanticTimelineEmitter.cs`
- `Presentation/CharacterPresentationInertializationPlanCompiler.cs`

## 保留的业务与代码

- TimelineSemanticTreeCompiler和BtsmtlSkillGraphCompiler等实际技能图编译保留。TimelineSemanticTreeCompilation只从已删的registry文件提取，成员、构造和Lifecycle实现与原声明逐字比较一致；它不是为消错新增的占位类型。
- FlowCanvas图／节点／端口与原生Pose运行、Player／StateMachine／Blend／惯性化／IK算法、Source／Constraint／Final Publication未由本批修改。
- Timeline正式内容、Frame映射、播放／取消／恢复算法和Float32TimelinePlayback／FixedTimelinePlayback未由本批修改。它们仍需领域owner接成直接内容运行，不能据编译器删除宣称已替换播放器。
- 网络Pipeline／Pass／Backend／Source／Solver与资源处理未由本批修改。Corin作者资产、ACL／Motion Matching／Foot资源未删除。
- CharacterPoseIrCompilation混合文件仍含作者节点定义与Lower方法；CharacterPoseProgramImage／IrContracts和运行消费者尚在，不属于本批完成声明。旧Image类型最终仍需退出，节点定义和算法不能按目录整删。

## 剩余消费者与并行改动

| 位置 | 删除暴露的接线 | 处理归属 |
| --- | --- | --- |
| Build/CharacterSimulationBuildOrchestrator.cs及.Background.cs | 仍调用TimelineSemanticFrontendCompiler | 核心删除旧Timeline编译产品入口，改接直接内容准备 |
| Compilation/Skills/GameplayAbilityAuthoringCompilationModel.cs、CharacterSkillCompilationDiscovery.cs、BtsmtlSkillGraphCompiler.cs、BtsmtlSkillGraphOccurrence.cs | 仍传递Timeline emitter registry或Semantic发现结果 | 核心改为正式内容依赖，不恢复registry |
| Compilation/Skills/BtsmtlSkillTimelineCompiler.cs | 仍调用TimelineSemanticEmitter | 核心接直接内容引用及TreeClip服务，保留实际技能编译 |
| Compilation/Semantic/CharacterAuthoringSourceCompilationModel.cs | 仍有旧Timeline发现／registry；同文件含TreeClip编译需用的内容记录 | 核心拆除旧角色发现，保留业务所需合同 |
| Compilation/Presentation/CharacterPresentationProjectionCompiler.cs | 仍创建PoseCompilationRequest并调用已删Module | 核心取消总Projection编译入口并调用Pose领域准备 |
| Compilation/Presentation/CharacterPoseIrCompilation.cs | 仍有旧拓扑Compiler和作者定义中的Lower／Family绑定声明 | Pose拆除混合文件的编译成员，保留作者能力与校验 |

删除前逐文件比较工作区状态和SHA256，28个删除目标没有未提交修改。CharacterPoseTransitionRuleCompiler、CharacterPresentationProjectionCompiler、CharacterAuthoringSourceCompilationModel、BtsmtlSkillGraphCompiler以及Pose执行器等存在并行改动，本批没有覆盖它们。上述表是实际残留交接，不是第二份领域任务清单，也不表示相关原任务完成。

## 检查与结果边界

- 显式指定Unity实例e852139597e42532，核对projectRoot为D:/Unity_Project_1/3C/3cDemo/Client/3C_Client；删除前状态为非Play、未编译、无待Domain Reload。
- 逐项检查删除目标及配对.meta不存在；核对TreeClip结果类型提取未改变实现；新脚本GUID扫描唯一；git diff --check通过。
- 本批未创建测试，未执行dotnet build、Unity Build、Play或主动Refresh；按D17接受中间编译失败。
- 删除后读取Unity Console，当前有FixedEvaluationFrame、FixedDomainPorts、FixedPipelineRuntimePorts及FixedSimulationSessionSnapshotCodec缺少ProgramExecutionLayout／BlackboardInputStateBinding／SimulationProgramCatalog的错误。本批未修改这些文件；并行工作区当前不是可运行完成态，不能用这份Console记录证明本批或整项目编译通过。
- 28个目标均最终由系统补丁删除；首次批量命令被工具策略拒绝，TypedLowering首次补丁失败，随后明确路径补丁成功。没有修改权限、采用其它删除通道或绕开并行修改。
- 需要恢复本批时以包含本记录的独立提交为边界，成套恢复对应源码和.meta并撤销提取文件；本批没有资源生成或持久数据迁移，不回退其它任务提交。

本批完成的是列出的旧编译链删除；角色总Program、共享工具接线、Pose Image运行类型清理及Timeline／Pose最终运行替换仍由原任务继续完成。
