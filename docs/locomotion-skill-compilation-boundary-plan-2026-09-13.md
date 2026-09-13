# Locomotion 与 Skill 编译边界规划

日期：2026-09-13。规划窗口：`01a09a5f-7316-79f2-9058-1d2cce37e801`。实现窗口：`01a09a5f-8d64-7c11-a461-7889623b7459`。

状态：源码调查与方案已整理；本文不是实现完成记录，也不代表运行验证通过。调查时 HEAD 为 `b1a8eff00`，共享工作区存在大量其它修改，以下以当时读到的工作区源码为准。

本窗口只维护本文，不修改实现代码、正式 spec 或协调总表。当前请求未启动 OpenSpec workflow；读取 `openspec/specs/` 用于核对正式合同，不新建 proposal。实现窗口按已收到的授权独立推进，不等待消息；不发送日常进度、提交或完成回执。

## 1. 要达到的业务结果

角色什么时候走、跑、转身，以及什么时候请求 Attack/Dodge，由运行时 C# ControlModule 决定。AbilityGraph 只表达技能自己的执行流程、准入、效果、窗口、Timeline 和有限生命周期。持续基础姿态仍由 Pose Graph 根据 committed Body/Intent 决定。

编译后的 Skill 数据不再描述角色 Locomotion 行为，也不夹带只供控制模块使用的 Motion 资源。角色仍通过同一正式 Program/Projection 启动，不新增运行时、资产加载旁路或默认补值。

必须分开三个命题：

- **不编译 C# 状态机行为**：不把 C# 状态切换条件或执行函数翻译成 Skill operations。
- **不复制 C# 静态 Motion 描述**：速度、空间、执行模式等不再从 C# Contract 抄成 `Motion:*` 字段再重建描述对象。
- **Control Motion 资源不进入 Skill IR**：真实曲线仍需持久化、校验、降低和发布，但由角色控制资源合同拥有。

仅删除旧 emitter 的方法名或图扫描，不能证明后两项已经完成。C# 拥有行为也不等于它可以绕过 Program State、Motion 仲裁或目标数值规则。

## 2. 已核实的代码链

以下路径相对 `3cDemo/Client/3C_Client/Assets/GameScripts/Main/`。

| 入口 | 输入与输出 | 对本任务的含义 |
| --- | --- | --- |
| `Runtime/Character/Control/Rules/CorinCharacterControlModule.cs`，构造函数、`Tick`、`BuildContract` | Contract 中的状态/转换身份与 C# 判断函数建立 UnityHFSM；读取输入和状态，提交 Motion、Ability 请求 | 真正的 Locomotion 行为 owner 已是 C#；不能另编一份条件和切换逻辑 |
| `Runtime/Simulation/Core/Float32/Execution/Float32OperationEvaluator.cs`，构造与 `Evaluate` | 通过 `ControlModuleBinding` 创建模块实例和 Control State Port；每 Tick 先 `TickCharacterControl`，再 `TickAbilityPrograms` | Ability 推进并不靠遍历 Locomotion Root；保留小 Root 不要求保留旧 Locomotion 图 |
| Fixed 同名 evaluator、control output 与 motion runtime | 与 Float32 对应的控制输入输出和目标数值处理 | 两个目标必须成套迁移，不能只改 Float32 |
| `Runtime/Simulation/Core/Program/CharacterControlProgramContracts.cs`，Catalog、Validator | ModuleId/version 解析、每 actor 工厂、Control slots owner 校验、AbilityPrograms 关联校验 | Binding、状态布局和 Ability 合同属于必要连接信息，不是第二份状态机 |
| `Editor/CharacterSimulation/Compilation/Semantic/CharacterSemanticFrontendCompiler.cs`，`Emit` | 编译 Gameplay catalog；解析 C# module、参数；发射 Control catalog/slots；发现并声明 Motion 曲线；编译 Ability；声明 Root reference | 当前在同一方法混合了技能语义、控制连接和控制资源装配 |
| `.../Semantic/CharacterSemanticControlModuleEmitter.cs`，`Emit` | 输出 Module identity/version、InitialState、参数、`Motion:*`、Control slots、transition SourceMap | 没有把 transition predicate 编成 operations；真正复制的是静态 Motion 描述，状态和来源不能一刀切删掉 |
| `.../Control/CharacterControlMotionCompilationDiscovery.cs` | 按 `SourceMotionIdentity` 从旧 graph roots 或 `Definition.ControlMotionTimelines` 找到唯一 MotionCurveClip | 当前还混合旧图查找与正式控制曲线查找，唯一性错误仍需由正式资源 owner 处理 |
| `.../Semantic/CharacterControlMotionCatalogEmitter.cs` | 将 Timeline/Track/MotionCurve 和曲线 bytes 写入 Program catalog 所需的 IR 表 | 这是 SourceCurve 的实际数据供给，不只是无用图编译 |
| `.../Semantic/CharacterSemanticEmitter.cs`，`EmitControlAbilityPrograms` | 声明 Ability 状态与 Blackboard；遍历 motion.Graph；编译 AbilityRecords；生成空参数 Root | 可以去除控制资源驱动的 Graph 编译，但要保留 Ability 编译、Blackboard scopes 和 Root reference |

### 2.1 当前没有证据说明存在第二套正在执行的 Locomotion IR 状态机

`CharacterAuthoringSourceCompilationModel.cs` 中 `CharacterAuthoringDiscovery.Discover` 创建 `roots = new List<CharacterCompositionRoot>()`，当前方法没有填充它，而是发现原生 AbilityGraph 闭包后直接返回。控制 Timeline 的发现记录使用 `Graph = null`；`EmitControlAbilityPrograms` 对这种记录直接跳过图编译。

所以，沿当前 Character 路径，旧 `CompileGraph(motions[i].Graph)` 循环没有实际 Locomotion graph 输入。它是可清理的旧路径，但不能据此声称当前每次都额外编译整套 Locomotion 状态机，也不能承诺删除它会明显缩短编译时间。实际耗时尚未测量。

### 2.2 SourceCurve 是真实运行依赖

Corin 的 MovingTurn 描述使用 `CharacterControlMotionDisplacementMode.SourceCurve`，指向明确的 `timeline:/track:/clip:` 身份。其余已读到的 WalkStart、WalkLoop、RunLoop 使用 ConstantSpeed。

当前消费顺序：

1. C# module 提交 Binding、连续 tick、generation 等请求。
2. `Float32CharacterControlOutputPort.RequireMotion` 从 `ControlModule` catalog 的 `Motion:*` 字段重建 descriptor；Fixed 有对应实现。
3. Locomotion runtime 向现有 Motion accumulator 提交贡献。
4. `Float32MotionRuntime.ResolveControlSourceCurve` 按 descriptor 查 MotionCurve，再追到 Track/Timeline，取 frame rate、起止帧和曲线。
5. 用前后 tick 的归一化采样差值计算位置、yaw，继续走当前 Motion 仲裁和 Body 提交。

直接删 `CharacterControlMotionCatalogEmitter.Declare` 会导致 MovingTurn 找不到 Program 曲线；把 SourceCurve 改成 ConstantSpeed 会改变转身位移与旋转，不能算保持原行为。

此外，`UnityFixedCharacterInputAdapter` 扫描 `ControlModule` catalog 中 `Motion:*:Input`、`Motion:*:Space` 来识别 CameraRelative 输入。只迁移 output port 会遗漏这条输入消费者。

## 3. 正式职责、输入和输出

| Owner | 正式输入 | 正式输出 | 不应承担 |
| --- | --- | --- | --- |
| C# ControlModule | Input、committed 状态、控制参数、Ability 状态/窗口 | Control State 更新，Motion/Ability 请求 | Skill IR 图生成、动画 Pose 选择、直接改 Transform |
| Skill/Ability frontend | AbilityGrants 的真实 AbilityGraph/子图引用/Timeline 闭包，Gameplay profiles 和 provider 合同 | Skill operations、控制流、Ability execution state、Gameplay catalog、SourceMap | C# transition predicate、持续 Locomotion 图、独立 Pose/ACL 资源内容 |
| 角色控制连接装配 | ModuleId/version、参数、Control State schema、Ability/provider 要求 | 现有 Program ControlModuleBinding、Control slots/layout、必要来源与根连接 | 再解释 C# 条件、复制可写状态机 |
| 角色控制资源构建 | 明确引用且由 Motion descriptor 使用的曲线与稳定身份 | 可持久化、可校验、可供两种目标降低的控制曲线资源 | 技能图发现、Timeline scheduler、动画 ACL 重建 |
| Numeric Target | 唯一已校验构建产物 | Float32/Fixed Program、唯一状态布局和数值转换结果 | Unity authoring 扫描、现场读取 TimelineAsset、补缺失资源 |
| Presentation 构建与运行 | Profile、Pose Graph、有限 Action producer 合同、已发布 ACL | 现有 Projection 及其正式资源引用 | 决定角色运动逻辑、成为 Skill 编译前置条件 |

BodyMotion 的重力参数、Input/Effect/Action 数据属于 Gameplay 的真实依赖，不能为了“Skill 轻量”全部剔除。Skill 中真正使用的 MotionCurve、Timeline、Camera command 或有限动画 producer 合同也不能仅凭类型名称删除；排除的是无关资源内容和表现拓扑。

## 4. 技术决策与业务取舍

### 决策 A：Control 静态描述的所有权

| 方案 | 业务收益 | 成本和约束 |
| --- | --- | --- |
| A1：Motion descriptor 直接来自已安装 C# module Contract；Program 保留身份、版本、可配置参数和状态布局 | 改角色控制代码时只有一个描述来源；消除 `Motion:*` 编码/重建与重复常量 | 运行时安装的模块版本必须匹配；描述中的数值仍需按各 Target 的确定规则转换，不能把原先先量化再使用悄悄改成另一种计算顺序；输入适配器必须改为读同一 Contract |
| A2：Program 持有独立 typed Control Binding 数据，构建时从 C# Contract 固化数值/资源要求 | 发布包完整记录控制配置；目标数值与输入映射可在构建时固定，利于查历史版本 | 仍有构建后的描述副本；须明确属于角色连接数据，不再用 Skill 字符串 catalog 冒充行为，codec/hash 与两 Target 都需迁移 |

两者均可保持一个运行时。A1 更符合“C# 行为与静态描述只有一个 owner”；A2 更强调发布包对控制参数的完整记录。无论选哪种，C# 方法逻辑不会因 Program 存了 descriptor 而自动变成可离线执行的数据。

### 决策 B：控制曲线如何离开 Skill IR

当前 Target 只接受 `ValidatedSemanticIrArtifact`，Program 所需曲线也只来自其中。下面两种是完整方案，不是可同时保留的两条路径。

| 方案 | 业务收益 | 成本和约束 |
| --- | --- | --- |
| B1：沿现有唯一 artifact/envelope 增加有明确 owner 的角色连接/控制资源区，Skill semantic payload 只保存技能内容；Target 从同一个已校验 artifact 装配现有 Program | 继续一次持久化、一次正式 Target 输入；控制曲线不进入 Skill 数据模型，发布时仍完整一致 | 必须升级唯一 schema、header/hash、codec、store、后台构建、两个 Target 和检查工具；这属于真实合同变化，不能只把 emitter 改名后宣称完成 |
| B2：控制曲线成为独立正式不可变资源产品；现有 Program 记录精确资源身份，现有 Session 服务按显式绑定提供资源 | 曲线可以独立构建和复用，改技能不复制控制曲线 | 增加一个正式资源产品及版本/发布/加载合同；需接入现有发布事务与 Session 生命周期，不新增播放器或绕过 Program 的运行链；不能直接交 TimelineAsset 给 Runtime |

若仍用现有 IR catalog 存 SourceCurve，虽然能清理旧 Graph 扫描并保持行为，但**尚未满足“控制资源离开 Skill IR”**。该状态可记录为部分完成，不能作为最终合同的替代方案。

B1/B2 的选择会影响公共产物边界。已有授权要求保持 Program/Projection，未明确指定采用独立资源产品；若实现需要扩大到新发布/装载合同，应将具体文件、合同变化和取舍写清楚后提出实际决策，不能借“轻量编译”自行开一条 Editor 内存直传路线。

### 决策 C：编译 API 的范围

| 方案 | 业务收益 | 成本和约束 |
| --- | --- | --- |
| C1：现有 Character frontend 原地收窄发现/发射职责，由现有 Build 编排技能、控制连接与表现阶段 | 一个作者入口、一个发布链；容易避免重复 stale/错误报告语义 | 内部必须拆清依赖和修订身份；方法返回 Character 结果并不意味着 Skill 阶段还可以访问整份 Definition |
| C2：抽出明确的 Skill frontend API，Character Build 组合它与控制连接/表现阶段，原来的技能实现迁入该 API | 作者与工具可以真正只处理技能，接口直接表达输入边界 | 必须迁移全部调用者和缓存；旧 Character 全量 Skill 编译实现须删掉或委托唯一 API，不能保留两套发现/发射 |

C1/C2 都服务于同一个 Character Build 和 Program/Projection；选择的是作者工具需要多明确的技能入口，而不是是否另造运行时。现有 Graph Compile 已是轻量校验，不应为了提供新 API 再把它恢复成全 Character Build。

## 5. 删除与迁移范围

### 可先明确的清理边界

- 从 Ability 发射接口移除 Control Motion records 参数和由其触发的旧 Graph 编译；保留 AbilityRecords、Blackboard 声明/scopes 和 Root。
- `m_AbilityCompilationDepth` 目前仅由上述旧 motion.Graph 循环增加；移除后检查相关 ActivateActionInstance 跳过和 edge 分支，按真实剩余调用删除死语义。
- `CharacterControlMotionCompilationDiscovery` 中遍历旧 roots 的内容不再作为角色控制资源查找入口；正式曲线的存在、唯一身份和引用闭包检查必须有接管者。
- `CharacterSemanticControlModuleEmitter` 拆出必要连接发射，按决策 A 处理 Motion 字段；Control State 和 transition SourceMap 要按消费者判断，不能因名称含 Control 删除。
- Skill 引用 MotionCurve 与控制模块使用的同一个源曲线可能共享身份：保留一个正式资源定义，两个 owner 只能引用；不能让两个 emitter 分别声明冲突 catalog 条目。

### 必须联动的消费者

- Float32/Fixed control output port、Motion runtime、evaluator 构造接线。
- Unity 输入适配器的 CameraRelative 识别。
- Program constructors/validators、codec、hash、state layout、module version 校验。
- 若改变 artifact 结构：semantic artifact codec/store、Target 接口与实现、后台构建 request、IR inspection/export 与普通 .NET 消费入口。
- SourceMap 中 ControlState/ControlTransition 的导航和诊断消费；来源身份消失不能变成静默空诊断。

### 不在本窗口改动范围

- Corin 当前走跑转身条件、速度和时序调参。
- 已发布 ACL、ACL publisher、Pose Graph、Camera 与 Projection 的无关逻辑和资源。
-其它窗口脏文件、生成资产、正式 spec、测试代码、协调总表。

## 6. 依赖与修订身份不能只改发射代码

当前 `ComputeSourceRevision` 使用 `AssetDatabase.GetDependencies(definitionPath, true)` 后按 `.asset`/`.inputactions` 过滤，只排除生成的 ProgramAsset/ProjectionAsset 类型。这仍会扫描 Definition 引用的 Presentation Profile 等资产；而且 Definition 文件本身被整体按 bytes 哈希，单纯过滤依赖类型也不能隔离其表现引用字段。

`CharacterAuthoringDiscovery.ValidateDefinition` 调用整份 `definition.CollectConfigurationErrors`，还显式校验 AnimationPresentationProfile、EquipmentPresentationProfile。这是另一个表现配置阻断技能编译的入口。

完整拆分应具有如下结果：

- 技能发现、校验与技能内容修订只包含实际消费的 Gameplay 字段和引用闭包。
- 角色连接/控制资源有自己的内容身份；必须覆盖源曲线、模块版本、参数、state schema 和所需 Ability/provider 合同。
- Presentation 保留自己的修订和资源检查；整 Character 发布仍检查必要产品是否匹配。
- ProgramHash 必须覆盖真正影响运行的控制资源和数值；不能为了让 SkillHash 不变而遗漏这些输入。
- 若将 SemanticHash 定义为纯技能身份，需要同步解释 Program 对控制部分的身份覆盖；若维持为整 artifact 身份，则另外明确技能 payload 的身份含义，不能把同一个字段在不同入口解释成两种范围。

当前 `CharacterSimulationBuildOrchestrator.Background` 会把 frontend artifact round-trip 后交给 Target；运行结束还比较 SourceRevision。普通构建、后台构建和 stale 检查必须使用同一个正式合同。Target 不允许从 Frontend 私有 model 偷读资源来补被删掉的 IR 内容。

## 7. 与正式 spec 的对照

| 正式文档 | 对照结论 | 需要明确的文档变化 |
| --- | --- | --- |
| `openspec/specs/character-state-timeline-authoring-loop/spec.md` | 第一条已说“角色主线状态与 Movement 规则属于 C# ControlModule 和 Program State”；后面却仍要求“Corin BTSMTL Locomotion StateMachine”，并要求 Locomotion 状态行为默认 StateNode inline graph | 真实矛盾。应将角色 Locomotion owner 统一为 C#；保留有限 Ability StateMachine/Timeline 场景，不整段误删 Action 合同 |
| `openspec/specs/character-pipeline-definition-authoring/spec.md` | 要求 Definition 为纯引用装配根、AbilityGrants 建立闭包；当前还存在 ControlParameters/ControlMotionTimelines 等额外字段，字段清单未覆盖 | 若保留或迁移控制配置，正式列明其 owner/引用，不以现有字段自动证明 spec 已同步 |
| 同上，composition roots Requirement | spec 要求 Equipment Feature Persistent/Route roots；当前所读 `Discover` 的旧 roots 列表为空 | 属于现状偏差；不能把本次删 Locomotion 旧路径当成已经完成 Equipment root 迁移，不顺手改变装备业务 |
| `openspec/specs/btsmtl-gameplay-semantic-ir/spec.md` | 当前要求 Definition 唯一根、全部可达 Graph/StateMachine/Timeline/MotionCurve 进入唯一 Gameplay IR | 应限定 Gameplay 可达闭包，明确 C# Control 行为排除；B1/B2 若改变 artifact 构成，要同步唯一 canonical artifact 和身份定义 |
| `openspec/specs/btsmtl-compiled-simulation-program/spec.md` | Target 正式输入只能是 validated artifact，不得接 Definition、Unity object、Frontend 私有 model | 保持这条边界；改变输入载体必须完整升级，不能创建未经 codec 的附加内存参数 |
| 同上，Projection Foot Analysis identity | 已要求表现资源变化时 Gameplay ProgramHash 不变 | 当前广泛 SourceRevision 扫描可能把表现修改带进 Program 身份，需要按实际字段闭包治理；本规划没有实测 hash 变化 |
| `openspec/specs/character-animation-layer-runtime/spec.md` | 持续 Locomotion 由 committed Body/Intent 和 PoseStateMachine 选择 source；Program 不提供持续 BaseLocomotion producer | 与本目标一致，保持；不能把有限 Action producer 合同也一起删掉 |

本文只列出矛盾和预期同步点，不宣称正式 spec 已修改。未来若执行 spec 更新，应保留未改变的场景，明确替换被废弃的 Locomotion ownership 表述。

## 8. 实施步骤与交付边界

1. 确定 A/B/C 的实际合同，记录选择及理由；锁定控制描述、曲线、hash 的唯一 owner，避免实现到中途用旁路补齐。
2. 原地清理旧 motion.Graph 编译与死的上下文分支，保留 Ability 与小 Root；此步不宣称已移出控制资源。
3. 按完整合同迁移控制描述及曲线数据，覆盖输入消费者和两个数值目标；接管曲线校验后再删原 catalog emitter 与旧发现模型。
4. 收窄技能发现、配置校验与修订输入；让显式 Character Build 组合现有 Program/Projection 阶段。
5. 清理旧字段、旧 schema 和废弃 API，更新唯一正式版本与消费者，不保留双读、双写或兼容路径。
6. 实现记录按小步中文提交列出变更路径、已完成边界与尚未接上的正式合同。仅提交本任务拥有的文件。

不新增测试代码，不把用户手动验收列为任务。本窗口只做静态调查；未运行编译、Unity、正式资源生成或回放。实现方若运行本地编译，应遵守项目 build server 参数与清理规则，并将静态编译、正式产物、运行时行为分别报告。

尚需实现时收敛的合同风险：目标数值转换顺序是否保持一致；Control/Skill 同源曲线如何唯一发布；模块代码变化如何强制反映版本；ControlTransition SourceMap 是否仍有消费；拆分修订后 Program/Projection 的共同发布 expectation 如何保持一致。未发现其它窗口已经修改这些合同并与本文发生直接冲突的证据，因此当前没有发送 `ACTUAL_CONFLICT`。
