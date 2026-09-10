## Context

本变更的正式对象是Character技能，不是AI Controller，也不是PoseGraph本体。Character RootTree已经从正式资产和Definition合同中删除；Character主线由C# ControlModule、编译后的Character Program以及Simulation Pipeline执行。AI自研RootTree已由独立`replace-btsmtl-ai-with-behavior-designer` change退役，Behavior Designer不进入本Skill Document或BTSMTL Graph。

当前角色技能作者源由`CharacterPipelineDefinition.SkillGraphs`和`CharacterSkillAuthoringDefinition.EntryGraphAuthoringId`组成。Skill Graph由FlowCanvas保存，运行时只消费Semantic IR和Numeric Program，不启动FlowCanvas委托、协程或自动Update。Character Program进入`SimulationSessionCompositionDefinition`，再由ProgramRuntime、ExecutionBackend、WorldSolver、SimulationPipeline和SessionSource组成正式Session。

项目已有Float32、Fixed、Server Authority和Deterministic Rollback路径。网络相关行为通过Session Source、Pipeline Pass和Adapter接入，不能把Character authoring或Skill Graph当作网络边界。本文借鉴GAS的职责划分，不引入Unreal对象模型、AbilitySystemComponent或其复制实现。

## Goals / Non-Goals

**Goals:**

- 一个GA式Skill只有一个稳定Entry Graph，SkillDefinition承担激活和业务合同，FlowGraph承担执行体。
- Skill Graph、Macro、State、Condition、Timeline和Blackboard使用同一正式作者拓扑、owner和Mutation事务。
- Skill Timeline保留类似Montage/AbilityTask的有限动作时序，并向Presentation/PoseGraph发出播放合同。
- 将变量按C# Character State、Ability Attribute/GameplayEffect、GameplayTag、Input/TargetData、Skill Local Blackboard、Activation/Frame Scope分层，不建立万能共享Blackboard。
- Character Program与Simulation Pipeline解耦，使Rollback、Server Authority和其他正式网络Adapter可以复用同一编译产物。
- Document v7直接读写正式Skill Graph闭包，保持稳定身份、owner、hash、回滚和反向导出。

**Non-Goals:**

- 不恢复Character RootTree，不把角色主线重新塞回图资产。
- 不把Character ControlModule、Movement Runtime或PoseGraph改造成Skill Graph。
- 不直接移植UE GAS运行时；只采用Ability、Attribute、Tag、Effect、ActivationData和Montage式Timeline的职责划分。
- 不让Skill直接写Velocity、BodyYaw、最终Pose、网络传输对象或任意C#字段；这些必须通过typed contract或正式Command。
- 不在本文实现Behavior Designer任务接入、Pose Graph拓扑迁移、IK算法重写或具体网络传输实现。

## Decisions

### 1. GA外壳与单根执行图

`CharacterSkillAuthoringDefinition`是GA式外壳，保存`SkillId`、`EntryGraphAuthoringId`、ActionProfile、ActionContext、输入、目标、替换和后续技能关系。一个Skill只能有一个Entry Graph。复杂流程通过Macro、State、Condition和Timeline闭包组织，不提供多个技能执行根。

Skill Graph的节点、端口和连接是唯一正式技能拓扑。编译、Document、观察和编辑器都直接读取该拓扑。旧BaseGraph只能作为未迁移其他领域的合法数据源，不能作为Character Skill转换中间层。

取舍：单根让网络请求、ActionInstance、generation、SourceMap和观察入口有唯一根；多入口虽然能减少某些图跳转，但会使激活身份、停止传播和网络重演出现第二条解释路径，因此拒绝多执行根。

### 2. 变量采用GAS式职责分层

Blackboard面板是多个正式provider的统一视图，不是一个物理万能字典。

| Provider | 内容 | Skill访问方式 |
|---|---|---|
| Character State | C# ControlModule和Movement Runtime事实 | 只读Get；修改走Control Command |
| Ability Attribute | 会被效果修改的数值资源、层数、冷却 | Get；Set通过GameplayEffect/typed effect command |
| GameplayTag | 激活、阻断、取消和状态标记 | Tag Query/Tag Command |
| Input/TargetData | 输入值、Action Request、Target Snapshot | 只读Get |
| Skill Local Blackboard | 当前Skill私有值 | Get/Set |
| State/ActionInstance | 当前状态或一次激活的局部值 | 按生命周期Get/Set |
| Frame Fact | Hit、IFrame、ComboAccept、Recovery窗口 | Timeline写，Skill读 |
| Presentation | Animation Playback/Producer Request | Emit，不直接写Pose |

每个可见字段必须具有owner、稳定ID、类型、读写权限、生命周期、预测和回滚合同。节点保存`ownerId + declarationId`，不得按显示名、路径扫描或反射猜测变量。

C#字段的编辑器投影只声明typed contract；它不是把C#字段复制到另一个Blackboard。Character State默认只读，Skill需要影响角色主线时提交`SubmitMotion`、`SubmitSkill`、`SubmitSkillStop`或其他正式Command。

跨GA共享数据优先使用Attribute、GameplayTag、GameplayEffect和ActivationData。若确实存在无法归类的复杂共享结构，必须新增明确的typed Ability State provider，不能把它藏进某个Skill Graph，也不能建立任意key-value共享黑板。

### 3. Character Movement保持独立

C# ControlModule负责移动规则、状态转换、输入解释和技能请求；Simulation Kernel、KCC和Motion Runtime负责速度、重力、碰撞、朝向和最终身体结果。它们共同承担类似UE CharacterMovementComponent的职责，但分成策略层和确定性求解层。

Skill可以读取Velocity、BodyYaw、Grounded、MovementMode和CurrentMoveDirection等事实。Skill不能直接设置Velocity或BodyYaw；需要改变Movement时必须提交正式Command，进入同一Tick、Program State、Rollback和Authority规则。

只有真正会被GameplayEffect修改的数值才进入Ability Attribute。基础MoveSpeed、TurnRate等控制配置仍属于ControlModule或Pipeline Config；当前帧方向、身体朝向和实际速度属于Movement Runtime事实，不作为普通Attribute或Skill Local Blackboard变量。

### 4. Timeline是Montage式动作合同

Skill Timeline可以拥有AnimationTrack、AnimationClip、Action Slot、播放模式、Blend In/Out请求、动作窗口和取消/停止时序。这是GA内部的有限动作时序合同，职责类似UE AbilityTask播放Montage并等待完成、混出、打断或取消；它不是第二个PoseGraph或通用Sequencer，不拥有Locomotion、IK或最终Pose混合。AnimationTrack和AnimationClip是动作播放请求的作者输入，不能被解释为Skill直接写骨骼。

Timeline编译为Program Operation和Animation Producer/Playback Request。Presentation Projection和PoseGraph负责Locomotion混合、Animation Layer、Mask、IK和最终Output Pose。Skill不得直接执行Pose混合或写骨骼。

作者层继续使用原生Timeline编辑器维护Timeline、Track、Clip和Producer的稳定身份；编译层使用TimelineAuthoringId、Track/Clip identity、Producer identity和Program SourceMap，不额外制造没有消费者的播放ID。GraphEditor只保存和打开Timeline引用，Timeline页面不进入Graph breadcrumb。

### 5. 直接编译和运行隔离

编译链为：

```text
Skill FlowGraph
  -> read-only occurrence
  -> Semantic IR
  -> Float32/Fixed Numeric Program
  -> Simulation Session
```

`BtsmtlSkillGraphCompiler`负责节点、边、Macro、State、Timeline和TreeClip的语义发射。Blackboard声明进入统一Program State Layout；ActionInstance、GraphInvocation、generation和父调用代次进入同一运行身份链。

运行时不读取作者图、不调用FlowCanvas getter、不启动Graph、不复制作者对象。编辑器观察只消费正式运行诊断、SourceMap和只读快照。

### 6. Character Program与Simulation Pipeline分离

Character Pipeline负责从Definition和Skill Graph生成Program与Actor Registration，不负责网络传输。

`SimulationSessionCompositionDefinition`选择：

- ProgramRuntime
- ExecutionBackend
- SimulationPipeline
- SessionSource
- WorldSolver

`SimulationPipelineDefinition`按照Ingress、Schedule、Step、Egress组织Pass。Pass由`SimulationPipelineCompiler`检查Program、Backend、WorldSolver、SnapshotCodec、ExecutionSupport、SourcePort和Product Contract的兼容性后形成Compiled Pipeline Plan。

网络适配发生在SessionSource、Pipeline Pass和Runtime Adapter层。Rollback使用InputIngress、Schedule、History、HashEgress、Snapshot和OutputDisposition等Pass；Server Authority使用自己的Authority、Prediction、Correction和Egress Pass。Skill和Character Program不绑定具体Transport或UE网络对象。

正式兼容检查沿用同一`SimulationSessionCompositionCompatibility.Evaluate`。`btsmtl.validate`可选接受一个精确`composition_asset_path`，把ProgramRuntime、ExecutionBackend、Pipeline、SessionSource、WorldSolver、Required Pass/Source Port及Network Model identity的结果加入只读报告；该参数只服务校验，不扫描目录、不修改Composition、不Build或Play。

### 7. 网络同步策略

网络同步输入、Canonical Request、Prediction身份、确定性Program State、State Hash和Snapshot，不复制FlowCanvas图、Blackboard名称、Timeline对象、AnimationClip对象或最终Pose。

静态Definition、Program和Presentation Projection通过Program/World/Pipeline identity和hash握手确认兼容。Skill激活由ActionInstance、PredictionKey和generation标识；可写Attribute、Tag、Effect、State和Local Blackboard值必须进入相应Program State、Snapshot和Hash路径。

因此网络可替换指更换SessionSource和Pass/Adapter，而不是允许每种网络拥有一套不同的Skill执行语义。Rollback和Server Authority共享Program合同，但各自拥有不同的输入确认、纠正和恢复策略。

### 8. Document、编辑器和事务

Skill Graph、Macro、Timeline、TreeClip、Blackboard declaration、Attribute/Tag provider引用和调用闭包均进入Document v7的稳定owner、hash和Mutation合同。人工编辑和Document apply必须使用同一能力目录、类型校验、真实owner、Undo和失败回滚。

#### 8.1 Skill Graph使用FlowCanvas原生作者表面

Skill Graph的唯一UI宿主是FlowCanvas原生`GraphEditor`。原生canvas、Toolbar、Blackboard、节点/连线Inspector、创建菜单、变量拖拽、selection、clipboard、Undo和Graph下钻保持启用；Skill domain adapter只向这些原生扩展点提供provider目录、typed字段、业务命令和只读诊断。adapter不得通过旁路UI重新实现Blackboard、Inspector、selection或创建流程，也不得设置自定义domain panel使原生Panels提前退出。

Blackboard和Inspector中的provider分组必须来自同一Capability与provider schema。当前图内创建变量只创建Skill Local declaration；Character State、Attribute、Tag、Input和Target provider由其正式schema提供，Skill只能引用或提交明确Command。拖拽变量创建Get/Set时必须写入ownerId和declarationId，不得只写显示名。该UI决策不改变真实owner、Mutation、Undo和Document v7合同。

## Risks / Trade-offs

- [Skill作者想直接改主线状态] -> 只开放Character State Get和正式Control Command，拒绝直接字段写入。
- [Ability共享状态变成新的万能黑板] -> 先按Attribute、Tag、Effect、ActivationData分类，复杂结构必须是显式typed provider。
- [Rollback和Server Authority共用一套状态但确认时机不同] -> 将确认、Restore、Hash和Egress放进各自Pipeline Pass，Program语义保持唯一。
- [Timeline动画与PoseGraph职责重叠] -> Timeline只通过原生Timeline编辑器维护类似Montage的有限动作播放/窗口合同，最终混合、IK和Pose输出只由Presentation/PoseGraph处理。
- [原生FlowCanvas不理解Skill provider] -> 由Skill domain adapter向原生Blackboard、Inspector和菜单注入provider目录与typed Mutation，不保留第二个Skill专用面板。
- [C#投影依赖反射导致网络不可重放] -> 使用稳定typed descriptor、State Layout、Serialize/Hash合同，不允许任意字段反射读写。
- [旧文档或旧测试继续引用Character RootTree] -> v7 checkout和正式入口拒绝旧RootTree路径；AI和未迁移领域保留自己的合法RootTree语义。
- [多网络Adapter产生分裂行为] -> 通过SimulationPipelineCompiler统一检查Program、Backend、Solver、Snapshot和Pass Contract，Adapter只实现接入策略。

## Migration Plan

1. 保留CharacterPipelineDefinition作为角色Program装配边界，删除Character RootTree字段、资产、入口、旧测试和零消费者Character路径；AI自研RootTree不保留，Behavior Designer接入由独立change管理。
2. 将C# ControlModule、Movement Runtime、Skill Program、GameplayEffect、GameplayTag和Presentation Projection的依赖整理为正式Program State和SourceMap合同。
3. 建立provider化Blackboard合同：Character State只读投影、Ability Attribute/GameplayEffect、GameplayTag、Input/TargetData、Skill Local Blackboard、State/ActionInstance/Frame Scope。
4. 在FlowCanvas原生GraphEditor的Blackboard、Inspector和创建菜单扩展点中提供provider分组、变量创建、Get/Set拖拽和外部provider显式引用；删除旁路Skill面板及其对原生Panels的替换，所有写入复用真实owner Mutation和Undo。
5. 将Skill Timeline的动作AnimationTrack/Clip、Action Window、停止和混出语义编译为Presentation Producer/Playback合同，PoseGraph继续拥有最终混合和IK。
6. 让Character Program通过Session Composition进入SimulationPipeline；Rollback、Server Authority和本地Pipeline分别接入正式Pass/Adapter，不能新增Skill专用网络分支。
7. 完成Document v7 checkout、dry-run、apply、反向导出和完整owner回滚；迁移只处理精确Skill闭包，旧Character RootTree不恢复。
8. 最后执行正式Character Build、网络产品闭包核对、Skill运行/中断/Timeline证据、Hash/Snapshot对账和OpenSpec严格校验。
