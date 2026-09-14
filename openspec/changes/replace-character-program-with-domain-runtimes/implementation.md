# 实施记录

## 当前状态

- change：`replace-character-program-with-domain-runtimes`
- 本窗口持续按独立小步提交；当前任务仍在继续。
- OpenSpec 任务：1.1、1.2、1.3、1.4、1.8、2.2、2.3、2.4、2.5 已完成；2.1 按 D12 重新打开，1.5—1.7、1.9—1.10、2.6 及后续任务仍未完成。1.1—1.3 的现有交付仍复用旧 `CharacterSimulationProgram` 容器，不代表最终独立 execution data 已完成。2.6 已开始收敛：Input request、Action activation request、Action instance、Timeline retention 和 MotionWarp Action 引用已进入角色状态分区，但控制机器内部状态、技能调用帧、目标、效果、装备与跨 Tick MotionWarp 数值状态的统一 Capture／Restore 尚未闭合。
- Unity Console、PlayMode 和运行时行为：尚未验证。

## 已提交的小步

- `24e63cf3f`：新增 `SimulationProgramRootKind.Ability`，并登记 `ability:` 入口身份。
- `25155d0ed`：新增 Ability 私有图发现模型、黑板声明收集、子图关系检查和资产依赖 source revision。
- `bcab5f853`：新增独立 Ability Semantic Frontend，按 Ability 根编译图操作、调用帧、黑板状态、常量、来源、能力要求和 Ability catalog。
- `f0aa981d8`：记录 Ability 前端实现边界、编译阻断和未验证范围。
- `6291d490c`：接入 Float32／Fixed Ability Target 入口与按 Ability identity 原子发布 `.csim` 的 store。
- `49c1d8df4`：补记 Ability 迁移进度和当前旧容器边界。
- `4744b49a`：将语义构建器、Emitter、Float32／Fixed Program 合同中的 Skill 语义统一为 Ability。
- `5ed2d1d50`：迁移 Ability 发现模型、语义目录、Action catalog、绑定和装备引用。
- `59f29541f`：统一 Ability 执行生命周期、实例身份、终止规则和运行时端口。
- `94b69acc6`：统一 Ability 数据合同、Control 请求／输出边界和 Equipment route 绑定。
- `252d7bde5`：迁移 Float32 Ability 执行状态、生命周期和当前执行上下文。
- `65bfba4c1`：迁移 Fixed Ability 执行状态、生命周期和当前执行上下文。
- `ee5eca3b3`：接通 Float32 Ability 黑板、控制端口、Motion、准入和 Program 执行链路。
- `bb3219653`：迁移 Float32 Ability 状态编码与执行聚合。
- `11567d11e`：接通 Fixed Ability 黑板、控制端口、Motion、准入和 Program 执行链路。
- `798ab26a3`：迁移 Fixed Ability 状态编码与 Fixed state schema。
- `b72f6926e`：补齐 Ability action／effect catalog 字段、生命周期全局状态和动作槽布局。
- `a903df4f9`：修正 Ability Effect catalog 的重复身份字段。
- `3a226a941`：新增 Float32／Fixed `GameplayAbilityDataAsset`，严格校验 Ability artifact metadata、root、hash 和 catalog。
- `833c8082a`：补齐本实施记录，写明 Ability 迁移边界与编译证据。
- `27bf006c0`：发布 `GameplayAbilityProviderContract`，把 Input、Gameplay Effect、Equipment、Character State 依赖收敛为 typed requirement，并在 Ability Target 发布入口校验。
- `90b7c2cb2`：让 Float32 `GameplayAbilityDataAsset.Load` 必须接收并校验 typed provider binding。
- `30518b1c9`：让 Fixed `FixedGameplayAbilityDataAsset.Load` 采用同一 typed provider binding；同时固化该文件已有的 Fixed 类型限定。
- `0ef3af238`：由 Character Definition 生成 typed provider binding 并接入 Float32 Ability Load；Fixed 通过 Fixed assembly extension 接入同一入口，避免共享 Definition 反向依赖 Fixed。
- `99d7426fb`：解除 Float32／Fixed Ability 生命周期对 Character ControlModule 存在性的短路依赖。
- `2749c9f44`：记录角色 Definition 到独立 Ability 资源的 typed binding 入口。
- `6126fc0c0`：让 Float32／Fixed Evaluate／Finalize Pass 通过角色领域运行 Interface 调用，不再直接依赖 Kernel 属性。
- `8dcbfb2ed`：把 Program Runtime 和 Backend composition 对外的 Kernel seam 收敛为 `CharacterRuntime`，具体 Kernel 只留在内部安装路径。
- `c47640401`：将 Float32／Fixed Ability 生命周期推进、停止屏障、generation、EntryOperation 和终态处理移入独立 `AbilityDomainRuntime` Module，Evaluator 只保留顺序编排。
- `9828d7afa`：记录 Ability Module 从总 Evaluator 拆出的边界。
- `e7f8c7fe3`：将 Float32／Fixed ControlModule 装配、参数读取、StateLayout、读写端口和 UnityHFSM Tick 移入独立 `ControlDomainRuntime` Module。
- `5e72ea7be`：将每个 Actor 的 Workspace／Evaluator 组合移入 `CharacterDomainRuntimeFactory`，Kernel 只保留 roster/binding 选择和实例生命周期。
- `65f588944`：让 Float32／Fixed Control 输出端口直接读取已安装 `CharacterControlModuleContract.Motions`，删除从旧 Program catalog 解码静态 Motion 描述的路径。
- `a4708e1b3`：让 Float32 角色注册、Evaluate 请求和 `ControlDomainRuntime` 使用 Definition 生成的 `CharacterControlRuntimeBinding`，删除控制参数从 Program catalog 读取的路径；Local、Server Authority 与 DotRecast manifest 共用同一份绑定身份。
- `0326bde46`：建立独立 `CharacterControlRuntimeState`、schema、事务和 codec，先把 Control 状态从角色级统一槽表拆为单独分区。
- `01a7a707b`：记录 Float32 Control 绑定的 Definition → Actor → Evaluate 链路，并同步实现边界。
- `1f23251e3`：把双目标 Control 分区状态接入角色状态、快照、ServerAuthoritative 检查点、Fixed 注册和回滚装配，状态载荷升级为拒绝旧格式的版本。
- `1dfb72acc`：删除 Control 参数、静态 Motion 字段和 Control state slots 的 Program 发射与 `CharacterControlStateLayout`，输入适配器改读正式 Control contract。
- `d1ddde86d`：为 Control state 建立独立 value kind、semantic 和 v2 codec，删除 Program Control owner、semantic 与旧 ControlState source-map 映射。
- `ff1640381`：以 RootMotionCurveAsset 完整曲线、源区间和 Timeline 帧映射建立 portable Control Motion binding，接入 Float32／Fixed／DotRecast，并删除 `CharacterControlMotionCatalogEmitter`。
- `268ba7909`：将 BodyMotionProfile 编译为独立 `CharacterBodyMotionBinding`，接入 Float32／Fixed／回滚／服务端注册、Evaluate／Finalize／WorldResolve 和 DotRecast manifest；删除 ProgramBodyMotionDescriptor 及其 payload，升级 Program artifact 格式。
- `c6331fb6c`：由 CharacterPipelineHost 构建并传入 BodyMotion 运行绑定，完成本地角色宿主接线。
- `121c306e6`：将 CharacterGameplayEffectProfile 编译为独立 portable `CharacterGameplayEffectRuntimeBinding`，让 Character Program layout、Float32／Fixed、Local／Rollback／Server Authority 与 DotRecast manifest 使用同一份 Effect 目录；Editor 与 Runtime 共用 Effect Definition 编码器。
- `46236f591`：将 CharacterEquipmentProfile 编译为独立 portable `CharacterEquipmentRuntimeBinding`，让 Equipment layout、Float32／Fixed 参数读取、Local／Fixed／Rollback／Server Authority 与 DotRecast manifest 使用同一份 Equipment 目录；manifest schema 升到 8。
- `5be0c3d3c`：删除 Equipment aggregate 的 Program state kind、semantic、layout address 和 Program 发射；由 CharacterSimulationState、Float32／Fixed 事务、state codec 与 Server Authority checkpoint 持有和传输独立 Equipment 状态。
- `5bde181b4`：删除 Gameplay Effect aggregate 的 Program state kind、semantic、layout address 和 Ability 前端的角色级 aggregate 声明；由 CharacterSimulationState、Float32／Fixed 事务、state codec 与 Server Authority checkpoint 持有和传输独立 Effect 状态。
- `759a49725`：把 Equipment local-state 的 kind、default、当前值、reset 和 codec 全部迁入 Equipment 领域，删除 Equipment local-state Program slot、semantic 和 state port；2.5 的 Effect／Equipment 目录与运行状态迁移完成。
- `19eb0581c`：为 Control runtime state 增加非消费式 Preview，让 Float32／Fixed 主状态事务在同一入口校验并提交 Control、Effect、Equipment 与 Program 分区；Kernel Finalize 不再先后消费两个独立提交入口。
- `fc30eaffb`：删除 Ability 独立前端对 Gameplay Effect aggregate、runtime:rng、runtime:handle-allocator 和 runtime:fact-sequence 的无条件状态声明，保留局部 action／execution state；1.8 的角色级状态归属清理完成。
- `acc4b59f5`：删除 FactSequence 的 Program 槽、语义和 EventSequence 独立 state port；Float32／Fixed 的 EventSequence 与 Finalize 事件统一通过角色状态事务递增，序号由 CharacterSimulationState 持有并接入 savepoint、状态 codec、Server Authority full／delta checkpoint 与 checkpoint hash；同步升级程序、状态和 checkpoint 格式版本。
- `2e292a3db`：删除 ActionEventSequence 的 Program 语义和 Action typed state 地址；ActionStateStore 的预测 key 改由 Float32／Fixed 角色状态事务递增，序号接入 savepoint、状态 codec、Server Authority full／delta checkpoint 与 checkpoint hash；同步升级程序、状态和 checkpoint 格式版本。
- `f01846176`：删除 `runtime:handle-allocator` Program 槽和访问策略；Action、Gameplay Effect、Equipment 共用角色事务的句柄分配、Capture、Restore 与提交，状态 codec 和 Server Authority checkpoint 同步携带句柄状态并升级格式版本。
- `56a0af13f`：删除没有模拟运行时消费者的 RandomState Program 槽、语义校验和空的 Runtime／Random owner，并升级 Float32／Fixed 程序格式；Unity 普通 RandomNode 不属于该模拟状态链，未做改动。
- `7ee1c2f4e`：删除角色级 `AbilityExecutionState` Program 槽、value kind 和旧 `CharacterStateValue` 聚合封装；ActionStateStore 通过角色事务读写 Ability 执行帧聚合，局部 Runnable／StateMachine／Timeline／Blackboard 状态仍由 `GameplayAbilityExecutionSlotMap` 映射，状态 codec 与 Server Authority checkpoint 同步保存该分区。
- `b57940900`：删除 Input request 的 Program state slot、semantic 和 Float32／Fixed 重复状态结构；请求身份只保留在 Program catalog，值按排序后的 request identity 由 `CharacterSimulationState` 持有，Input runtime 通过角色状态事务完成读取、写入、消费与 savepoint／Restore；状态 codec、Server Authority full／delta checkpoint 和格式版本同步升级。
- `d67c16fd2`：删除 Action activation request 的 Program state slot、semantic 和 Float32／Fixed 重复状态结构；请求按 Action identity 由 `CharacterSimulationState` 持有，Action runtime 通过角色状态事务暂存、查找、清理，状态 codec、Server Authority full／delta checkpoint 和格式版本同步升级；Action instance 仍保留现有 Program state slot，等待后续独立迁移。
- `02059b33c`：删除 Action instance 的 Program state slot、semantic、StatePort 和 ActionPolicy；实例及其目标快照由 `CharacterSimulationState` 持有，ActionStateStore 通过主角色事务完成实例查找、容量、生命周期写入和 savepoint／Restore，Action catalog 只提供容量与内容身份；状态 codec、Server Authority checkpoint、布局和程序格式同步升级，Timeline retention 与 MotionWarp 仍保留各自的 ActionInstanceReference。
- `1e09d91d2`：删除 Timeline retention 的 Program state slot 和 semantic；按 Timeline operation identity 由角色状态持有 `ActionInstanceReference`，Timeline control port 通过主角色事务读写，状态 codec、Server Authority checkpoint 和程序格式同步升级；Timeline 播放、循环和 LogicTime 仍由原 Timeline owner 持有。
- `7d8f7cfab`：删除 MotionWarp Action 引用的 Program state slot、semantic 和 `ActionInstanceReference` value kind；按 MotionWarp operation identity 由角色状态持有引用，MotionWarp target 经主角色事务读写，状态 codec、Server Authority checkpoint、布局和程序格式同步升级；其余 15 个 MotionWarp 数值状态仍由 MotionModifier Program 分区持有。

## 当前实现边界

- Ability 前端不读取 CharacterPipelineDefinition，不生成 Character 控制、Body Motion、Equipment 或 Pose 目录。
- Ability 图通过现有 BTSMTL Skill 图编译器复用图算法；外部 Input、Gameplay Effect、Character State 只通过 provider owner 和最小 catalog 依赖接入。
- Ability 根入口直接指向私有图的 Root operation；Float32／Fixed 的 Ability 生命周期、Action／Effect catalog、状态槽和 artifact metadata 已接通。
- Ability 目录现在为 Input／Gameplay Effect／Equipment／Character State 外部依赖发布 typed provider requirement；缺少 owner、同一依赖绑定多个 owner 或绑定类型不符时，Target 发布直接失败。
- Ability 独立前端不再声明 Gameplay Effect aggregate、随机数、句柄分配器或事实序号等角色级服务状态；这些服务由角色运行时 owner 提供，Ability 只保留自己的 action／execution state 和必要 provider requirement。1.8 已完成，但 1.9 的真实成员解析与 1.10 的独立 execution data 仍未完成。
- `GameplayAbilityDataAsset` 与 `FixedGameplayAbilityDataAsset` 当前仍从 canonical bytes 读取 `CharacterSimulationProgram`，只是严格的 Ability root/catalog 校验入口；它们不是最终独立 execution data，运行时 Ability 数据接口、领域工厂、角色绑定替换和旧 Character Program 清理尚未完成。
- typed provider binding 已通过 Character Definition 的 Float32／Fixed Ability Load 入口实际消费；缺失 provider 在资源绑定阶段失败，任务 1.4 已完成。
- 当前 Character Host 仍加载旧整角色 Program，尚未把 Ability 资源集合装配进新的领域运行实例；这部分仍属于后续角色领域工厂工作。
- `SimulationKernel` 仍负责跨 Actor roster/binding 和 World request，但每个 Actor 的 Workspace／Evaluator 已由 `CharacterDomainRuntimeFactory` 创建，Pass 通过 `CharacterRuntime` Interface 调用 Evaluate/Finalize；Control 的静态 Motion、BodyMotion 的数值配置和 Ability 的生命周期已分别进入独立 Module。Effect、Equipment、FactSequence、ActionEventSequence、HandleAllocator、Input request、Action activation request、Action instance、Timeline retention 和 MotionWarp Action 引用已进入独立角色状态分区；Character Program 仍承载 Runnable、StateMachine、Timeline 播放、Blackboard 以及 MotionWarp 剩余数值状态，旧 Program 数据清理仍未完成。
- Float32／Fixed Control 参数链路已改为 `CharacterPipelineDefinition.ControlParameters` → `CharacterControlRuntimeBinding` → `SimulationActorBinding`／`SimulationEvaluateRequest` → 对应 `ControlDomainRuntime`。绑定会校验 ModuleId、semantic version、参数 kind 和 ContentHash；Program adoption 也拒绝改变已安装 Actor 的 Control binding。
- Control 状态现在由每个角色的 `CharacterSimulationState.ControlState` 持有，Evaluate 为它单独开启 `CharacterControlRuntimeStateTransaction`，只有 World resolve 成功才通过主状态事务的统一入口和 Program state 一起提交；角色状态 codec、World snapshot 和 ServerAuthoritative full/delta checkpoint 都携带同一份 Control state。Control state descriptor、value kind、semantic 和 codec 已由 Control 自己拥有，旧 Program Control owner、semantic 与 ControlState source-map 映射已删除。
- Control catalog 现在只发射身份、版本和初始状态字段；参数由 `CharacterControlRuntimeBinding` 提供，静态 Motion 由 `CharacterControlModuleContract.Motions` 提供，Control state 不再发射为 Program slot。Unity 输入适配器直接消费正式 Control contract。
- `CharacterControlMotionBinding` 保存 RootMotionCurve 的关键帧、wrap、求值模式、源秒区间、Clip 帧范围、源修订和内容 hash；Unity builder 从 Definition 的真实 MotionCurveClip 生成它，运行时不读取 Unity asset。Float32／Fixed 通过同一 `EvaluateDelta` 时间映射消费，DotRecast manifest schema 8 携带同一 codec bytes；旧 `CharacterControlMotionCatalogEmitter` 和 Control Motion 的 Program catalog 回读已删除，MovingTurn 的 SourceCurve 与 CameraRelative 输入仍由正式 contract 驱动。
- `CharacterBodyMotionProfile` 现在由 `CharacterPipelineDefinition.BuildBodyMotionRuntimeBinding` 生成唯一 `CharacterBodyMotionBinding`，经 Local／Fixed／Rollback／Server Authority 的 Actor registration 进入 `SimulationActorBinding`、Evaluate request 和 Pending evaluation；Float32／Fixed 的 `CharacterBodyMotionRuntime` 只消费这份运行绑定，垂直积分、Motion 仲裁、WorldResolveBatch 和 `AirborneVerticalMotion` 能力校验仍由原正式链处理。CharacterSimulationProgram 不再保存 BodyMotion descriptor 或编码字段，Float32／Fixed Program artifact 与 payload 版本已升级并拒绝旧格式；DotRecast manifest schema 8 携带 BodyMotion 配置及 hash。
- Local Host 与 Server Authority Host 直接从 Character Definition 构造 Control／BodyMotion binding；加载后的 Authority runtime 使用 manifest 中的同一绑定，不再从 Program catalog 补参数。
- `CharacterGameplayEffectProfile` 现在由 `CharacterPipelineDefinition.BuildGameplayEffectRuntimeBinding` 编译为带 source identity、content revision、完整 Tag／Attribute／Effect portable bytes 的 binding，进入 Program layout、Actor binding、Evaluate request 和 DotRecast manifest；Float32／Fixed 的 `SimulationGameplayEffectProgram` 对 Character 只从该 binding 解码，不再从 Character Program catalog 读取 Effect 定义。`GameplayEffectStateAggregate` 已由 `CharacterSimulationState` 独立持有，现有 Effect working state、事务 savepoint、state codec 和 Authority checkpoint 使用同一份状态；Program 内保留的 Effect identity／producer 仍服务于现有 operation 引用。
- `CharacterEquipmentProfile` 现在由 `CharacterPipelineDefinition.BuildEquipmentRuntimeBinding` 编译为带 source identity、content revision、slots／features／items／routes／route implementations／parameter values、local-state kind 和 default 的 portable binding，进入 Program layout、Actor binding、Evaluate request 和 DotRecast manifest；Float32／Fixed 的 Equipment layout 与参数读取只消费该 binding，不再从 Character Program constants 读取 Equipment 参数。`EquipmentStateAggregate` 现在由 `CharacterSimulationState` 独立持有 slots、local-state 当前值、pending change 和 contribution handles，事务、state codec 和 Authority checkpoint 使用同一份状态；当前 operation identity references 仍复用 Program 的正式接口。
- `FactSequence` 现在由 `CharacterSimulationState.EventSequence` 持有，Float32／Fixed 事务的 `NextEventSequence` 负责递增、溢出检查、savepoint／Restore 和最终提交；EventSequence frame 与 Finalize 事件不再查找或写入 Program state slot。角色状态 codec、Server Authority full／delta checkpoint 和 checkpoint hash 读写同一序号，旧格式由版本和 identity 变化拒绝；角色 Program 不再声明 `runtime:fact-sequence`，构建诊断明确标记 `FactSequence=external`。
- `ActionEventSequence` 现在由 `CharacterSimulationState.ActionEventSequence` 持有，`Float32ActionStateStore`／`FixedActionStateStore` 只通过事务申请下一个预测 key；ProgramExecutionLayout 不再为每个 Action 复制全局序号地址，ActionPolicy 也不再开放该 Program semantic。状态 codec 与 Server Authority full／delta checkpoint 使用同一字段顺序，旧程序和旧状态由格式版本或 identity 变化拒绝；构建诊断标记 `ActionEventSequence=external`。
- `HandleAllocator` 现在由 `CharacterSimulationState.HandleAllocator` 持有，`Float32HandleAllocator`／`FixedHandleAllocator` 只调用角色事务的递增、Capture、Restore；Action、Gameplay Effect 和 Equipment 的句柄来源保持同一条事务链，Gameplay Effect 的局部失败恢复仍通过显式 allocator Capture／Restore 回到该事务。角色状态 codec、Server Authority full／delta checkpoint 和 checkpoint hash 都携带该字段，构建诊断标记 `HandleAllocator=external`。
- `RandomState` 没有实际模拟运行时读写者，已从全局 Emitter、ProgramStateSemantic 和 owner 枚举中删除；随机节点仍可作为普通 Unity 节点存在，但不会借用角色模拟状态伪装成正式 RNG 服务。
- `AbilityExecutionState` 现在由 `CharacterSimulationState.AbilityExecutionState` 持有，`GameplayAbilityExecutionManager` 的 Add／Remove／generation／局部值写入都通过 Float32／Fixed 主状态事务保存；`GameplayAbilityExecutionSlotMap` 只负责识别需要按 Ability 实例隔离的局部 Program 状态，不再提供角色级聚合存储。状态 codec 额外编码执行帧聚合，Server Authority checkpoint 使用独立 bytes 载荷；这仍不是 1.10 要求的独立 Ability execution data，Ability 资源本身仍从旧 Program 容器读取。
- Input request 由 Program catalog 只提供稳定 identity；`ProgramExecutionLayout` 按 identity 建立排序后的 request 列表，`CharacterSimulationState` 保存每个请求的 request id、sequence、source／expire tick、priority 和 consumed 状态。Float32／Fixed 的 Input runtime 不再持有自己的状态或 Input policy，而是经同一角色状态事务读写；事务的 savepoint／Restore、提交和清理都与其它角色分区共用。请求 codec 使用共享核心合同，角色状态 codec 与 Server Authority checkpoint 按同一字段顺序携带该分区，旧 Program state identity 和 payload format 由版本升级拒绝。这样 Input 的配置身份仍来自正式 Program catalog，运行值只有一个角色状态 owner；但 2.6 仍未完成，因为统一角色 Step 还没有把所有剩余领域分区的跨 Tick 状态一次性纳入同一套完整 Capture／Restore 合同。
- Action activation request 现在由 `CharacterSimulationState.ActionActivationRequests` 持有，列表中的请求保留 Action、Skill、Context、输入序号、开始 Tick、目标快照、来源、装备上下文和替换实例身份。Action instance 现在由同一角色状态的 `ActionInstances` 列表持有，保留生命周期、执行 generation、目标快照、装备上下文、停止过渡和 segment generation。Float32／Fixed 的 ActionStateStore 不再创建 Action StatePort，也不再读写 Action Program slot；Stage、pending 查找、容量、实例复用、生命周期写入和清理统一经过主状态事务。状态 codec 和 Server Authority full／delta checkpoint 以独立 bytes 携带请求与实例，Program catalog 只提供 Action 容量与内容身份。Timeline retention 现在由 `TimelineRetainedActionContexts` 按 Timeline operation identity 持有，并由 Timeline control port 经事务读写；MotionWarp 的 `ActionInstanceReference` 仍留在 MotionModifier 状态，两个 owner 尚未合并。
- Action activation request 现在由 `CharacterSimulationState.ActionActivationRequests` 持有，列表中的请求保留 Action、Skill、Context、输入序号、开始 Tick、目标快照、来源、装备上下文和替换实例身份。Action instance 现在由同一角色状态的 `ActionInstances` 列表持有，保留生命周期、执行 generation、目标快照、装备上下文、停止过渡和 segment generation。Float32／Fixed 的 ActionStateStore 不再创建 Action StatePort，也不再读写 Action Program slot；Stage、pending 查找、容量、实例复用、生命周期写入和清理统一经过主状态事务。状态 codec 和 Server Authority full／delta checkpoint 以独立 bytes 携带请求与实例，Program catalog 只提供 Action 容量与内容身份。Timeline retention 现在由 `TimelineRetainedActionContexts` 按 Timeline operation identity 持有，并由 Timeline control port 经事务读写；MotionWarp 的 Action 引用现在由 `MotionWarpActionContexts` 按 MotionWarp operation identity 持有，MotionWarp target 经事务读写；MotionWarp 剩余 15 个数值状态仍在 MotionModifier Program 分区，不能把本步当成完整 MotionWarp 迁移。

## 编译证据与阻断

- Float32 core：`dotnet build 3cDemo/Client/3C_Client/ThirdPersonSimulation.Float32.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false --no-restore` 成功，0 warnings、0 errors。
- Fixed core：`dotnet build 3cDemo/Client/3C_Client/ThirdPersonSimulation.Fixed.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false --no-restore` 成功，0 warnings、0 errors。
- Full Editor build：`dotnet build 3cDemo/Client/3C_Client/ThirdPersonClient.Editor.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false --no-restore` 成功，0 errors、94 warnings；警告来自现有项目／依赖代码，不能替代 Unity Console、PlayMode 或端到端行为验证。
- Float32 runtime build：`dotnet build 3cDemo/Client/3C_Client/ThirdPersonClient.Runtime.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false --no-restore` 成功，0 errors、34 warnings；警告来自现有项目／依赖代码。
- Fixed provider 接入使用同一 `ThirdPersonClient.Runtime.csproj` 编译通过，0 errors、34 warnings；尚未运行 Unity、测试或端到端行为验证。
- Float32 Control runtime binding 变更后，`ThirdPersonSimulation.Core.csproj`、`ThirdPersonSimulation.Float32.csproj`、`ThirdPersonSimulation.DotRecastAuthority.csproj`、`ThirdPersonClient.Runtime.csproj` 和 `ThirdPersonClient.Editor.csproj` 均编译成功，均为 0 errors；DotRecast Authority 3 warnings、Client Runtime 34 warnings、Client Editor 94 warnings，均来自现有项目或依赖代码。
- 2026-09-14 当前 Control 分区状态与 catalog 清理后，`ThirdPersonSimulation.Float32.csproj`、`ThirdPersonSimulation.Fixed.csproj` 均为 0 warnings、0 errors；`ThirdPersonSimulation.DotRecastAuthority.csproj` 为 3 warnings、0 errors；`ThirdPersonClient.Editor.csproj` 未出现本次改动错误，输出中的 warning 仍来自现有 Unity/package 代码。
- 2026-09-14 Control state 类型语义拆出后，`ThirdPersonSimulation.Float32.csproj`、`ThirdPersonSimulation.Fixed.csproj` 均为 0 warnings、0 errors；`ThirdPersonClient.Runtime.csproj` 未出现本次改动错误，包内既有 warning 仍保留。
- 2026-09-14 Control Motion portable binding 后，`ThirdPersonClient.Runtime.csproj` 为 1 个既有 warning、0 errors；`ThirdPersonSimulation.DotRecastAuthority.csproj` 为 3 个既有 warning、0 errors；`ThirdPersonSimulation.Fixed.Unity.csproj` 与 `ThirdPersonSimulation.DeterministicRollback.Unity.csproj` 均为 0 warning、0 error。
- 2026-09-14 BodyMotion 运行绑定与 Program 清理后，`ThirdPersonClient.Runtime.csproj` 为 1 个既有 warning、0 errors；`ThirdPersonSimulation.DotRecastAuthority.csproj` 为 1 个既有 warning、0 errors；`ThirdPersonSimulation.Fixed.Unity.csproj`、`ThirdPersonSimulation.DeterministicRollback.Unity.csproj` 均为 0 warning、0 error；`ThirdPersonClient.Editor.csproj` 为 56 个既有 warning、0 errors。
- 2026-09-14 Gameplay Effect 运行绑定后，`ThirdPersonSimulation.Core.csproj`、`ThirdPersonSimulation.Float32.csproj`、`ThirdPersonSimulation.Fixed.csproj` 均为 0 warning、0 error；`ThirdPersonClient.Runtime.csproj` 为 1 个既有 warning、0 errors；`ThirdPersonSimulation.DotRecastAuthority.csproj` 为 3 个既有 warning、0 errors；`ThirdPersonClient.Editor.csproj` 为 90 个既有 warning、0 errors。
- 2026-09-14 Equipment 运行绑定后，`ThirdPersonSimulation.Core.csproj`、`ThirdPersonSimulation.Float32.csproj`、`ThirdPersonSimulation.Fixed.csproj` 均为 0 warning、0 error；`ThirdPersonClient.Runtime.csproj` 为 1 个既有 warning、0 errors；`ThirdPersonSimulation.DotRecastAuthority.csproj` 为 3 个既有 warning、0 errors；`ThirdPersonClient.Editor.csproj` 为 56 个既有 warning、0 errors；`ThirdPersonSimulation.Fixed.Unity.csproj` 与 `ThirdPersonSimulation.DeterministicRollback.Unity.csproj` 均为 0 warning、0 error。
- 2026-09-14 Equipment 状态分区后，`ThirdPersonSimulation.Core.csproj`、`ThirdPersonSimulation.Float32.csproj`、`ThirdPersonSimulation.Fixed.csproj` 均为 0 warning、0 error；`ThirdPersonClient.Runtime.csproj` 为 34 个既有 warning、0 errors；`ThirdPersonSimulation.DotRecastAuthority.csproj` 为 3 个既有 warning、0 errors；`ThirdPersonClient.Editor.csproj` 为 56 个既有 warning、0 errors。
- 2026-09-14 Gameplay Effect 状态分区后，`ThirdPersonSimulation.Core.csproj`、`ThirdPersonSimulation.Float32.csproj`、`ThirdPersonSimulation.Fixed.csproj` 均为 0 warning、0 error；`ThirdPersonClient.Runtime.csproj` 为 34 个既有 warning、0 errors；`ThirdPersonSimulation.DotRecastAuthority.csproj` 为 3 个既有 warning、0 errors；`ThirdPersonClient.Editor.csproj` 为 56 个既有 warning、0 errors。
- 2026-09-14 Equipment local-state 迁移后，`ThirdPersonSimulation.Core.csproj`、`ThirdPersonSimulation.Float32.csproj`、`ThirdPersonSimulation.Fixed.csproj` 均为 0 warning、0 error；`ThirdPersonClient.Runtime.csproj` 为 34 个既有 warning、0 errors；`ThirdPersonSimulation.DotRecastAuthority.csproj` 为 3 个既有 warning、0 errors；`ThirdPersonClient.Editor.csproj` 为 56 个既有 warning、0 errors。
- 2026-09-14 Ability 角色级服务状态槽清理后，`ThirdPersonClient.Editor.csproj` 编译成功，93 个既有 warning、0 errors；未运行 Unity、测试或端到端行为验证。
- 2026-09-14 统一角色 Tick 提交入口后，`ThirdPersonSimulation.Core.csproj`、`ThirdPersonSimulation.Float32.csproj`、`ThirdPersonSimulation.Fixed.csproj` 均为 0 warning、0 error；Kernel 所在的 `ThirdPersonClient.Runtime.csproj` 与 `ThirdPersonClient.Editor.csproj` 未出现本次改动错误。
- 2026-09-14 FactSequence 迁移后，Portable `ThirdPersonSimulation.Core.csproj`、`ThirdPersonSimulation.Float32.csproj`、`ThirdPersonSimulation.Fixed.csproj` 均为 0 warning、0 error；`ThirdPersonClient.Runtime.csproj` 为 34 个既有 warning、0 errors；`ThirdPersonSimulation.DotRecast.csproj` 为 2 个既有 warning、0 errors；`ThirdPersonClient.Editor.csproj` 为 57 个既有 warning、0 errors。每次编译结束后均已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。
- 2026-09-14 ActionEventSequence 迁移后，Portable `ThirdPersonSimulation.Core.csproj`、`ThirdPersonSimulation.Float32.csproj`、`ThirdPersonSimulation.Fixed.csproj` 均为 0 warning、0 error；`ThirdPersonClient.Runtime.csproj` 为 34 个既有 warning、0 errors；`ThirdPersonSimulation.DotRecast.csproj` 为 2 个既有 warning、0 errors；`ThirdPersonClient.Editor.csproj` 为 57 个既有 warning、0 errors。每次编译结束后均已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。
- 2026-09-14 HandleAllocator 迁移后，Portable `ThirdPersonSimulation.Core.csproj`、`ThirdPersonSimulation.Float32.csproj`、`ThirdPersonSimulation.Fixed.csproj` 均为 0 warning、0 error；`ThirdPersonClient.Runtime.csproj` 为 34 个既有 warning、0 errors；`ThirdPersonSimulation.DotRecast.csproj` 为 2 个既有 warning、0 errors；`ThirdPersonClient.Editor.csproj` 使用 `-m:1` 编译成功，为 56 个既有 warning、0 errors。Editor 默认并行编译曾两次出现 MSBuild 子节点提前退出（MSB4166），没有产生 C# 错误；串行重跑成功。每次编译结束后均已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。
- 2026-09-14 RandomState 清理后，Portable `ThirdPersonSimulation.Core.csproj`、`ThirdPersonSimulation.Float32.csproj`、`ThirdPersonSimulation.Fixed.csproj` 均为 0 warning、0 error；`ThirdPersonClient.Editor.csproj` 使用 `-m:1` 编译成功，为 93 个既有 warning、0 errors。每次编译结束后均已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。
- 2026-09-14 Ability 执行聚合迁移后，Portable `ThirdPersonSimulation.Core.csproj`、`ThirdPersonSimulation.Float32.csproj`、`ThirdPersonSimulation.Fixed.csproj` 均为 0 warning、0 error；`ThirdPersonClient.Runtime.csproj` 为 34 个既有 warning、0 errors；`ThirdPersonSimulation.DotRecast.csproj` 为 2 个既有 warning、0 errors；`ThirdPersonSimulation.ServerAuthoritative.csproj` 为 1 个既有 warning、0 errors；`ThirdPersonClient.Editor.csproj` 使用 `-m:1` 编译成功，为 34 个既有 warning、0 errors。每次编译结束后均已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。
- 2026-09-14 Input request 状态分区后，`ThirdPersonSimulation.Core.csproj`、`ThirdPersonSimulation.Float32.csproj`、`ThirdPersonSimulation.Fixed.csproj` 均为 0 warning、0 error；`ThirdPersonClient.Runtime.csproj` 为 34 个既有 warning、0 errors；`ThirdPersonSimulation.DotRecast.csproj` 为 2 个既有 warning、0 errors；`ThirdPersonSimulation.ServerAuthoritative.csproj` 为 1 个既有 warning、0 errors；`ThirdPersonClient.Editor.csproj` 使用 `-m:1` 编译成功，为 93 个既有 warning、0 errors。每次编译结束后均已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。
- 2026-09-14 Action activation request 状态分区后，`ThirdPersonSimulation.Core.csproj`、`ThirdPersonSimulation.Float32.csproj`、`ThirdPersonSimulation.Fixed.csproj` 均为 0 warning、0 error；`ThirdPersonClient.Runtime.csproj` 为 34 个既有 warning、0 errors；`ThirdPersonSimulation.ServerAuthoritative.csproj` 为 1 个既有 warning、0 errors；`ThirdPersonClient.Editor.csproj` 使用 `-m:1` 编译成功，为 93 个既有 warning、0 errors。旧 Action activation request Program 槽和 API 扫描无残留；每次编译结束后均已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。
- 2026-09-14 Action instance 状态分区后，`ThirdPersonSimulation.Core.csproj`、`ThirdPersonSimulation.Float32.csproj`、`ThirdPersonSimulation.Fixed.csproj` 均为 0 warning、0 error；`ThirdPersonSimulation.ServerAuthoritative.csproj` 为 1 个既有 warning、0 errors；`ThirdPersonClient.Editor.csproj` 串行编译因并行窗口已有的 `BtsmtlSkillAuthoringCodeAdapter.cs` 缺失 `BtsmtlSkillAuthoringContract`、`GraphAuthoringFieldValue`、`BtsmtlSkillAuthoringFieldValue` 和 `FieldExpression` 报 6 个错误，另有 93 个 warning；输出未出现本步文件的编译错误。每次编译结束后均已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。
- 2026-09-14 Timeline retention 状态分区后，`ThirdPersonSimulation.Float32.csproj`、`ThirdPersonSimulation.Fixed.csproj` 和 `ThirdPersonSimulation.ServerAuthoritative.csproj` 均为 0 warning、0 error；`ThirdPersonClient.Runtime.csproj` 因并行窗口已有的 `Runtime/BTSMTL/Timeline/Scripts/TimelineAuthoringPropertyContract.cs` 缺失 `TreeClip`、`TimelineTreeExecutionPhase` 及 `CreateDefaultCurve` 成员而报 14 个错误，未出现本步 Simulation Core 文件的错误。每次编译结束后均已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。
- 2026-09-14 MotionWarp Action 引用分区后，`ThirdPersonSimulation.Float32.csproj`、`ThirdPersonSimulation.Fixed.csproj` 和 `ThirdPersonSimulation.ServerAuthoritative.csproj` 均为 0 warning、0 error；完整 `ThirdPersonClient.Runtime.csproj` 仍受并行窗口已有的 `Runtime/BTSMTL/Timeline/Scripts/TimelineAuthoringPropertyContract.cs` 14 个缺失类型／成员错误阻断，未出现本步 Simulation Core 文件的错误。每次编译结束后均已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。
- 每次编译结束后已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 下一小步

下一步继续按 2.6 补齐领域分区的 Capture／Restore 和统一角色 Step 事务内容，覆盖 Ability 调用帧、请求、目标和跨 Tick MotionWarp；并推进 1.9 的真实 provider 成员解析、1.10 的独立 Ability execution data，随后收口 Timeline 与角色领域状态，最后清理旧 `CharacterSimulationProgram` 容器。
