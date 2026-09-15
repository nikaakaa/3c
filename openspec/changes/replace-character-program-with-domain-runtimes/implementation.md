# 实施记录

## 当前状态

- change：`replace-character-program-with-domain-runtimes`
- 本窗口持续按独立小步提交；当前任务仍在继续。
- OpenSpec 任务：1.1、1.2、1.3、1.4、1.8、1.9、2.2、2.3、2.4、2.5 已完成；2.1 按 D12 重新打开，1.5—1.7、1.10、2.6 及后续任务仍未完成。1.1—1.3 的现有交付仍复用旧 `CharacterSimulationProgram` 容器，不代表最终独立 execution data 已完成。2.6 已开始收敛：Input request、Action activation request、Action instance、完整 MotionWarp 状态、GameplayEffect、Equipment 和 Control 已进入角色状态分区；Ability 下的 Timeline 保留态已删除，但控制机器内部状态、技能调用帧、目标、效果、装备和统一角色 Step 的完整 Capture／Restore 尚未闭合。
- 按 D13—D15，Timeline 直接内容、播放私有状态和 Pose 原生图内部实现归各自既有任务；本窗口只接它们的 Prepare／Create、typed Pending、Commit／Discard、Stop 和采用事实，不复制其 cursor、节点状态或缓冲实现。核心继续负责 Host／Factory、Ability 最终数据与 Provider、角色 Step／快照／codec、网络／manifest、共享技能编译入口和总 Program／Projection 清理。
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
- `ef66cd0c7`：删除 MotionWarp 剩余 15 个 Program state slot、MotionModifier owner、descriptor 槽位范围和 `MotionModifierPolicy`；Float32／Fixed 以 `MotionWarpStates` 按 operation identity 持有完整生命周期、姿态、限制结果、推进进度和 Action 引用，事务统一处理重置、推进、savepoint／Restore，状态 codec、Server Authority checkpoint、程序 artifact 与布局版本同步升级。
- `9b4b517ab`：将 Ability provider 合同从类别级 owner 身份扩展为依赖成员级的 ValueKind、Revision、ProviderSemanticVersion 和 RuntimeHandle；Character Definition 从真实 Input、Gameplay Effect、Equipment 与 Control 配置生成成员绑定，Ability DataAsset 与 Target 编译入口统一校验实际依赖成员。
- `e140ae68d`：新增 Float32／Fixed `GameplayAbilityExecutionData`，Ability Asset 的公开 Load／Definition 入口改为返回技能域数据，数据公开调用帧、技能操作、局部状态、依赖引用、来源和 Provider 合同，不再把 `CharacterSimulationProgram` 作为调用方结果返回；内部运行时消费链仍需继续从旧 Program evaluator 切换。
- `8741f20cf`：将 Float32 Ability canonical artifact、Numeric Target、根身份、元数据和 Provider 合同校验移入核心 `Float32GameplayAbilityExecutionDataLoader`，Unity DataAsset 只组装持久化元数据并取得执行数据。
- `fc8abc1a4`：按同一合同完成 Fixed Ability loader，保持 Float32／Fixed 的产物读取边界对称。
- `d885db31b`：修正两个 loader 的跨程序集可见性，使 Unity DataAsset 能调用核心正式入口；生成的 Unity csproj 只作为本机编译索引，不纳入仓库。
- `2832a8380`：新增 Float32 Ability execution catalog 与唯一 Program 转换工厂，ActionRuntime、AbilityDomainRuntime 和 Timeline decision 改从执行数据目录读取 binding。
- `32f75a37c`：按同一边界完成 Fixed Ability execution catalog，保持两种数值后端的运行时消费对称。
- `c101cdb54`：撤回上述以角色 Program 生成 Ability catalog 的接线；角色根不能伪造 Ability 根，等待角色配置直接提供独立技能数据。
- `59cad96bd`：按 D17 先删除整角色 Program wrapper、目标 artifact、Fixed 旧 codec、全局 Program catalog／ExecutionLayout、整角色 BuildService、全角色 artifact store 和旧 Ability loader；Float32 codec 因并行占用暂缓，保留技能编译与动画算法待引用拆接。
- `2bde1c946`：按 D17 删除旧 Projection wrapper、发布后读取器、Projection 编译上下文、修订计算器和旧验证器；保留仍有业务用途的表现资源与算法。
- `17656634f`：将 Float32／Fixed Ability artifact 改为独立 `.ability` 格式，DataAsset 直接读取 `GameplayAbilityExecutionData` 并校验 typed provider；新增独立 execution-data artifact store，删除旧 Ability Program artifact store。
- `d8bb0d932`、`256428d93`：把 Float32／Fixed Ability Target 编译器及 Unity meta 统一改为领域命名，移除已经失真的 CharacterSimulationProgram 编译器文件名。
- `6254f5008`：删除 Float32／Fixed ProgramRuntime 聚合运行时、Unity ProgramRuntime 定义基类／派生定义和对应运行时配置资产，拔掉角色注册表到全角色 Kernel 的旧入口载体。
- `2b549a9bd`：删除没有消费者的 Float32／Fixed 旧 Program ValueResolver。
- `fe9347cc0`：删除 Float32／Fixed Timeline Program reader 和对应生成资产；Timeline 不再通过旧 Target artifact 读回整套 CharacterSimulationProgram。
- `163931b99`：删除 Float32／Fixed 旧 Timeline 播放器及只加载它们的场景 Host，去掉第二套 Timeline 播放状态实现。
- `18a534fd0`：删除旧 Character／Timeline Target Adapter、Program product、artifact stage 和 wrapper 发布集合；后续各领域使用自己的发布入口。
- `39ac0bfb4`：从编译报告文件中删除旧整角色 Build／Timeline Build 结果合同，保留 Ability／Control 编译仍使用的诊断报告。
- `527db0d89`：删除旧整角色 Build orchestrator 及后台 partial，移除 Semantic→Target→Projection 的总编译、发布和采用入口。
- `4881a17bc`：删除旧 Program 到表现合同、诊断 source map 的 Float32／Fixed 适配器和旧 DebugProgram builder。
- `59706b897`：抽出 Fixed Ability 执行原语，删除 Fixed CharacterSimulationProgramManifest 与 CharacterSimulationProgram 整体类。
- `295502b48`：删除 Float32／Fixed ProgramEvaluate、ProgramFinalize Pass 与 ProgramRuntime 端口。
- `fd14cc72a`：删除旧 Timeline Program MCP 构建／示例工具。
- `afd92e310`：删除只调用旧角色 Semantic Frontend 的 Semantic IR 检视窗口。
- `9748e8398`：删除已无实现对应的 Float32 ProgramEvaluate／Finalize Pass 定义和 StandardLocal 配置资产。
- `f3fec70b6`：删除把整角色 Program、Projection、旧诊断和表现 runtime 捆在一起的 CharacterSimulationActorRegistration。
- `aad466b13`：删除从整角色 Program operation 反查 Timeline 的预览辅助器。
- `167e3b20e`：删除 Float32／Fixed 旧整角色 Program 诊断适配器。
- `a9f06ae6f`：删除 ServerAuthoritative 侧旧 Program 角色链，包括 Authority Actor、Remote Presentation、Fantasy endpoint/connection、Hybrid model 和旧 Session Source。
- `49f3a4f96`：删除全局无调用者的旧 GameplayAbilityProgramDefinition／GameplayAbilityCatalog 定义目录。
- `65c06746b`：删除 Control 侧旧 Program catalog owner binding、Program catalog validator 和按旧 Program 校验 Ability 的路径；Control catalog 只按正式 ModuleId/Contract 取得模块。
- `ee1057bc4`（共享工作区并行提交）：同时删除 Float32／Fixed 旧 Session Composer 及 Unity Composer；本窗口未重复提交该改动。
- `7f96d8947`：将独立 Ability 的 Binding、Catalog、EndRule 合同统一改为 Execution 命名，移除独立领域对旧 Program 合同名的依赖。
- `e46a19f9f`：删除旧 GameplayLab 总 Program 构建器和 Editor 启动器。
- `04452ae98`：删除 Float32／Fixed 旧 Control state、Control port 和 Timeline target 适配器；保留通用 Operation control 算法。
- `466ab44e`：让 Float32／Fixed Ability 数据对象直接构建并持有 `OperationExecutionTopology` 与 `ProgramCatalogRuntimeIndex`。
- `fd429fad0`：删除 Float32／Fixed 旧 Control 领域运行适配器。
- `8142bd4e7`：删除旧角色 `SimulationKernel` 实现和 `CharacterDomainRuntimeInstance` 包装，不再保留整角色 Program evaluator 入口。
- `8e9e355f2`：从 Session checkpoint 合同文件移除 Program Epoch、ProgramBinding 和 Program adoption 定义；checkpoint/replay 数据合同暂保留。
- `f6a3b1bb6`：新增只保存独立 Ability 数据对象引用的安装集合，按 AbilityId 建立查找索引，不复制操作或状态。
- `31c7efaeb`：让 Character Definition 的 Float32／Fixed Load 入口可以直接装配独立 Ability 数据集合。
- `69756dac4`：删除旧 Program Runtime 组件描述器。
- `1b08dfbed`：移除 `ProgramRuntime` 组件角色身份，保留其它 Backend／Source／Solver／Snapshot 角色编号。
- `af0986661`：删除旧 Session Program Runtime 兼容性 evaluator 和 Inspector；保留 Pipeline Inspector。
- `aa7f6a708`：删除 Float32／Fixed `SimulationActorBinding`，不再用整角色 Program、ProgramHash 和 LayoutHash 作为角色注册 wrapper。
- `a32bffebe`：把仍需保留的 Value 类型推导算法提取为不依赖角色 Program 的 `GameplayAbilityExecutionValueResolver`。
- `c19010bdd`：删除仍强制持有 Character Program 和 Presentation Projection 的旧 GameplayLab Bootstrap、Variant 和 Inspector。
- `9965d868`：删除旧 GameplayLab 代码删除后留下的空 Editor 程序集。
- `270f573e7`：把数值 Target 对旧 Kernel specialization 的依赖改为独立 `SimulationExecutionTargetManifest`。
- `9d7e464c7`：收紧 Ability Value resolver 的输入接口，移除无效的旧上下文参数。
- `40be04e41`：让 DotRecast manifest loader 从 Float32 正式 Execution Target 校验操作集版本，不再读取 Kernel specialization。
- `6556051ee`：从公共角色 registration contract 移除 Program epoch、Program binding 和整角色 Program 身份成员。
- `60a758ef0`：从 Fixed registration contract 移除 Character Program 和 ProgramIdentity 成员。
- `1a3865f9f`：将保留的 Session checkpoint/replay 合同重命名为 `SimulationSessionCheckpointContracts.cs`，不再用 Epoch 文件名承载旧 adoption。
- `8f2675c6d`：从 Float32／Fixed 旧 Kernel contracts 中删除 Program-bound Evaluate／Pending／Finalize 请求，把仍由 WorldResolve／Finalize 使用的 BodySample／ActorTickResult 提取为 Step Result 合同。
- `9211434a7`：删除 Float32／Fixed Pass RuntimeHandle 的 Program Epoch 状态、adoption 方法和 ProgramRuntime 诊断项，保留 LogicTick、checkpoint、回放和资源生命周期。
- `9f8c1bf25`：将 Pipeline compiler/context 的第一输入从旧 Program Runtime 改为 `SimulationExecutionTargetManifest` 和显式 Target 能力。
- `459c0b273`：删除 Float32／Fixed Standard 合同中的旧 ProgramEvaluate/ProgramFinalize Pass 及其 ProgramRuntime port requirement。
- `7ad93c3d6`：删除 ServerAuthoritative/Rollback Pass 合同的 ProgramRuntime port requirement。
- `ee56d2cac`：清理干净的 Local、ServerAuthority、Rollback Package 对已删除 Program Pass 的配置和运行工厂引用。
- `25dd37ddb`：让 Session CompositionDescriptor/LaunchPlan 持有 Execution Target，移除 ProgramRuntime component 输入并升级 Composition identity。
- `240adee9d`：提取 Pipeline 初始状态选择合同，删除 Float32/Fixed 旧 Pass Backend composition request 及其整角色 ProgramRuntime/Actor roster 载体。
- `c0c85cc5a`：让 Unity Backend Definition 发布正式 Execution Target，移除 Session Composition 的旧 ProgramRuntime 序列化字段。
- `13c28dd4a`：删除无实际 Composer 实现的旧 Session Composition Preparation/Composer 编排路径，保留 PreparedRuntime 和输出生命周期合同。
- `dd38dde00`：建立独立 Ability 执行布局的索引算法和 Float32/Fixed 工厂。
- `ad3bdc28b`：按程序集边界把 Ability layout/value resolver 从 Core 移到 Float32/Fixed 执行目录，避免 Core 依赖数值私有操作类型。
- `3ac938eec`：修正 Ability 数据集合 `TryGet` 的 out 参数赋值，使公共 Core 编译恢复。

## 当前实现边界

- Ability 前端不读取 CharacterPipelineDefinition，不生成 Character 控制、Body Motion、Equipment 或 Pose 目录。
- Ability 图通过现有 BTSMTL Skill 图编译器复用图算法；外部 Input、Gameplay Effect、Character State 通过 provider owner、依赖成员、值类型、成员版本和运行句柄接入，配置身份与实际消费成员分开校验。
- Ability 根入口直接指向私有图的 Root operation；Float32／Fixed 的 Ability 生命周期、Action／Effect catalog、状态槽和 artifact metadata 已接通。
- Ability 目录现在为 Input／Gameplay Effect／Equipment／Character State 外部依赖发布成员级 typed provider requirement；缺少 owner、成员、运行句柄、同一依赖绑定多个 owner、成员版本或值类型不符时，Target 发布直接失败。1.9 的真实成员解析已落地，1.10 的独立数据格式和发布入口已落地；运行时执行消费者仍待切换。
- Ability 独立前端不再声明 Gameplay Effect aggregate、随机数、句柄分配器或事实序号等角色级服务状态；这些服务由角色运行时 owner 提供，Ability 只保留自己的 action／execution state 和必要 provider requirement。1.8 与 1.9 已完成，1.10 仍剩独立数据进入运行时 evaluator／binding 的接线。
- `Float32GameplayAbilityExecutionData` 与 `FixedGameplayAbilityExecutionData` 是当前独立 artifact 数据对象；独立 codec 读写 `.ability`，`GameplayAbilityDataAsset.Load` 和 `FixedGameplayAbilityDataAsset.Load` 直接返回技能域数据，`GameplayAbilityExecutionDataArtifactStore` 直接发布它们。旧 Program→Ability catalog 转换、旧 Ability loader、旧整角色 Program artifact store 和旧 Program reader 已删除，不能恢复 CharacterSimulationProgram 转换、整包复制或兼容 reader。现有 evaluator／binding／拓扑仍有 CharacterSimulationProgram 残余引用，属于激进删除后的待接线错误，不是角色正式入口。
- typed provider binding 已通过 Character Definition 的 Float32／Fixed Ability Load 入口实际消费；缺失 provider 或缺失／类型／版本／句柄不符的成员在资源绑定阶段失败，任务 1.4 的身份入口和 1.9 的成员合同分别保留其边界。
- 当前 Character Host、Session、Network 和部分 Evaluate／State 链仍引用旧整角色 Program；这些是本轮删除后暴露出来的待拆接线，不是继续服务角色的正式入口。Ability 资源集合尚未装配进新的领域运行实例，后续由角色领域工厂直接组装。
- 旧 `SimulationKernel`、Float32／Fixed `CharacterDomainRuntimeInstance` 和整角色 Program evaluator 入口已删除；Session／Pipeline／Network 的旧请求、ProgramRuntime port 和 Program identity 消费者仍是删除后暴露的待接线错误。后续角色工厂必须直接装配 Control、独立 Ability 数据集合、Timeline、Pose、Camera、Motion、Effect、Equipment 和 World owner，不恢复 Kernel、全局 Layout 或转换 Factory。Ability 数据已能由 Character Definition 直接加载为按 AbilityId 索引的集合，实际 evaluator／Host／Pass 消费仍待接通。旧 Timeline reader、播放器和场景 Host 已删除；Timeline 私有播放状态仍归 Timeline owner，核心尚未接入 D14 的 Prepare／CreatePlayback／Pending 提交合同。
- Ability execution data 现在自持 `OperationExecutionTopology`、`ProgramCatalogRuntimeIndex`；Float32/Fixed 各自执行程序集拥有 `GameplayAbilityExecutionLayout` 和 `GameplayAbilityExecutionValueResolver`，按独立 Ability data 构建值输入、状态地址、Action/Input/Timeline/MotionWarp 索引。`SimulationExecutionTargetManifest` 只描述数值后端、ABI 和操作集，不再通过 Kernel 提供 Target 身份。
- 旧 GameplayLab Bootstrap、Session Variant 和空 Editor 程序集已删除；性能采样与网络产品脚本仍引用旧 GameplayLab 类型，属于后续正式角色启动入口的待接线消费者。
- Session checkpoint/replay 数据结构仍保留，但旧 Program adoption、Actor binding、Program Runtime component 和 Kernel implementation 已退出；Float32/Fixed Pipeline、Host、Network、Rollback 和性能脚本中的旧签名暂时保持错误，后续统一改成领域内容 identity、roster 和状态合同。
- Float32／Fixed 旧 Kernel 请求合同已经删除，新的领域 Step 请求尚未建立；现有 Step Result 只负责携带已完成状态、Body sample、Motion 和事实／表现／诊断输出，不再暗含 Program binding。
- Pipeline 的旧 Program Evaluate/Finalize Pass、ProgramRuntime port、Backend composition request 和 Session Composer 已删除；WorldResolve、Pass product、checkpoint/replay 算法仍保留，但新的领域 Evaluate/Finalize、Backend composition request 和 Session preparation 尚未重建。
- Float32／Fixed Control 参数链路的旧 `SimulationActorBinding` wrapper 和 Program adoption 合同已删除；现有 `CharacterPipelineDefinition.ControlParameters`、`CharacterControlRuntimeBinding` 与 `SimulationEvaluateRequest` 仍是待接入的新角色装配材料。绑定会校验 ModuleId、semantic version、参数 kind 和 ContentHash，后续由角色 Factory 直接把它交给 Control owner。
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
- Action activation request 现在由 `CharacterSimulationState.ActionActivationRequests` 持有，列表中的请求保留 Action、Skill、Context、输入序号、开始 Tick、目标快照、来源、装备上下文和替换实例身份。Action instance 现在由同一角色状态的 `ActionInstances` 列表持有，保留生命周期、执行 generation、目标快照、装备上下文、停止过渡和 segment generation。Float32／Fixed 的 ActionStateStore 不再创建 Action StatePort，也不再读写 Action Program slot；Stage、pending 查找、容量、实例复用、生命周期写入和清理统一经过主状态事务。状态 codec 和 Server Authority full／delta checkpoint 以独立 bytes 携带请求与实例，Program catalog 只提供 Action 容量与内容身份。Timeline 私有 retained action context 已从 Ability 状态、事务和角色状态 codec 删除；MotionWarp 完整聚合状态仍由 `MotionWarpStates` 按 MotionWarp operation identity 持有，MotionWarp target 经同一角色事务读写并进入状态 codec 与 Server Authority checkpoint。

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
- 2026-09-14 MotionWarp 完整状态分区后，`ThirdPersonSimulation.Float32.csproj`、`ThirdPersonSimulation.Fixed.csproj` 均为 0 warning、0 error；`ThirdPersonSimulation.ServerAuthoritative.csproj` 为 1 个既有 warning、0 error；`ThirdPersonSimulation.Fixed.Compiler.csproj` 为 2 个 Unity TestRunner 既有 warning、0 error。每次编译结束后均已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。
- 2026-09-14 Ability Provider 成员合同后，`ThirdPersonSimulation.Core.csproj` 和 `ThirdPersonSimulation.Fixed.Compiler.csproj` 均为 0 error；Core 为 0 warning，Fixed.Compiler 为 2 个 Unity TestRunner 既有 warning。`ThirdPersonClient.Runtime.csproj` 为 2 个并行 Pose 文件缺少 `Component` using 的既有错误，未出现本步 Provider 文件错误；每次编译结束后均已执行 `dotnet build-server shutdown`。
- 2026-09-14 Ability execution data 返回边界后，`ThirdPersonSimulation.Core.csproj`、`ThirdPersonSimulation.Float32.csproj`、`ThirdPersonSimulation.Fixed.csproj` 均为 0 warning、0 error；未运行 Unity、测试或资产生成。`ThirdPersonClient.Runtime.csproj` 仍被并行 Pose 文件的 2 个 `Component` 类型错误阻断，未出现本步 Ability data 文件错误；每次编译结束后均已执行 `dotnet build-server shutdown`。
- 2026-09-14 Ability artifact loader 边界后，`ThirdPersonSimulation.Float32.csproj`、`ThirdPersonSimulation.Fixed.csproj` 和 `ThirdPersonSimulation.Unity.csproj` 均为 0 warning、0 error；第一次 Unity 编译发现新 loader 未进入显式生成工程，补入本机忽略的 csproj 索引后复编通过。每次编译结束后均已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。
- 2026-09-14 Ability execution catalog 接线后，`ThirdPersonSimulation.Float32.csproj` 与 `ThirdPersonSimulation.Fixed.csproj` 均为 0 warning、0 error；未运行 Unity、测试或资产生成。每次编译结束后均已执行 `dotnet build-server shutdown`。
- 2026-09-14 撤回旧 Program→Ability catalog 接线后，`ThirdPersonSimulation.Float32.csproj` 与 `ThirdPersonSimulation.Fixed.csproj` 均为 0 warning、0 error；未运行 Unity、测试或资产生成。每次编译结束后均已执行 `dotnet build-server shutdown`。
- 2026-09-14 D17 首批整角色 Program 载体删除后，`ThirdPersonSimulation.Float32.csproj` 出现 94 个预期旧 `ProgramExecutionLayout`／`SimulationProgramCatalog` 消费者错误；未新增恢复类型或兼容路径，构建结束后已执行 `dotnet build-server shutdown`。这是删除批次的接线清单，不代表保留业务已接通；未运行 Unity、测试或资产生成。
- 2026-09-14 D17 激进删除继续完成 ProgramRuntime、Timeline reader／播放器和旧 Target 集合清理；本轮未重新编译或运行 Unity，故不宣称构建恢复。剩余旧 Program 消费者错误保持为后续领域接线清单。
- 2026-09-14 Fixed 整角色 Program 类和旧 Pipeline Pass 删除后，`ThirdPersonSimulation.Fixed.csproj` 只报 6 个生成工程 `CS2001`：生成 csproj 仍列出已删除的 Fixed 源文件；没有新增原语的编译诊断。已执行 `dotnet build-server shutdown`，未运行 Unity、测试或资产生成。
- 2026-09-14 激进删除继续移除角色 Actor Registration、旧 Program 预览／诊断和 ServerAuthoritative Program 角色链；没有重新编译或运行 Unity，当前残余错误是待接线清单，不宣称网络或角色运行时已恢复。
- 2026-09-14 Control owner 与旧 Ability 定义目录删除后未重新编译；生成工程和旧 Runtime 消费者仍按删除后的断裂状态保留，未运行 Unity、测试或资产生成。
- 2026-09-14 Ability layout/value resolver 搬到 Float32/Fixed 程序集后，Core 本机索引修正并编译为 0 warning、0 error；Float32/Fixed 窄编译仍因旧 State/Pipeline/evaluator/Backend 消费者报错，过滤输出未命中本步 layout/resolver 错误。每次编译结束后均已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。
- 每次编译结束后已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 下一小步

下一步进入删除后的核心接线：让保留的 Ability 执行入口直接消费 Float32／Fixed 独立 execution data，建立按 Ability 数据的执行布局和状态边界；不恢复角色 Program、全局 Layout 或转换 Factory。随后接入 D14 的 Timeline Prepare／CreatePlayback／Pending Commit／Discard、D15 的 Pose 阶段接口、2.1 的非旧 Program 角色工厂和 2.6 的统一 Step／快照收口。Timeline／Pose 内部状态不在本窗口重复实现。

## 2026-09-15 Ability Invocation 状态依赖切口

- 提交 `6907a4696` 将 Float32 与 Fixed 的 Ability Invocation 构造入口改为接收独立 `IFloat32AbilityExecutionStateTransaction` / `IFixedAbilityExecutionStateTransaction`、对应 `IFloat32AbilityDomainStatePort` / `IFixedAbilityDomainStatePort`、裁剪后的 Ability Execution Input 和 Body Facts，以及显式 accept 回调。
- 角色评估入口负责 `BindAbility`，构造完成后调用 `invocation.Accept()`，再由显式回调节点角色事务 `AcceptAbility`。Ability Invocation 内部不再保存 `Float32CharacterRuntimeStateTransaction` / `FixedCharacterRuntimeStateTransaction`，也不再接收完整 `CharacterSimulationInput` 和 `WorldBodyState`。
- 本切口不宣称 1.11 已完成：Invocation 当前仍接收 Control / Equipment binding 并在内部装配 Gameplay Effect、Equipment、Control 等 runtime；Frontend 按可达节点声明能力、统一非角色调用方入口和删除无条件 Effect 要求仍是剩余工作。
- `ThirdPersonSimulation.Float32.csproj` 与 `ThirdPersonSimulation.Fixed.csproj` 窄编译均为 0 warning、0 error；每次编译后已执行 `dotnet build-server shutdown`。未运行 Unity、测试或资产生成。

## 2026-09-15 Ability领域服务装配外移

- 提交 `a2ae94e02` 引入 `IFloat32AbilityExecutionServiceFactory` 与 `IFixedAbilityExecutionServiceFactory`。`Float32AbilityInvocationRuntime` / `FixedAbilityInvocationRuntime` 只通过 typed service factory 获取 Gameplay Effect、Equipment 和 Locomotion runtime，不再接收 Control binding、Equipment binding 或 Gameplay Effect scratch。
- `Float32CharacterEvaluationRuntime` / `FixedCharacterEvaluationRuntime` 现在构造每个 Ability 的 execution workspace 和 `CharacterAbilityExecutionServiceFactory`，由角色调用方提供正式领域服务并负责 Bind / Accept。
- Factory 按安装数据声明 Equipment 能力返回 Equipment runtime；缺 binding 时返回 null，Invocation 在实际需要 Equipment 的能力时显式失败。Gameplay Effect catalog 缺失时 Factory 返回 null，Invocation 在执行对应能力时显式失败。
- 本切口继续不勾选 1.11：Frontend 仍需按可达节点声明 Gameplay Effect / Equipment / Character State 能力，TreeClip 等非角色调用方尚未统一到同一 factory 合同。
- `ThirdPersonSimulation.Float32.csproj` 与 `ThirdPersonSimulation.Fixed.csproj` 窄编译均为 0 warning、0 error；每次编译后已执行 `dotnet build-server shutdown`。未运行 Unity、测试或资产生成。

## 2026-09-15 Frontend按可达节点声明GameplayEffect

- 提交 `d411b9de6` 在 `GameplayAbilitySemanticFrontendCompiler.RequireGraphCapabilities` 中遍历 `EntryGraph.EnumerateOccurrences()`，检查每个 occurrence 的 FlowNode。
- 只有发现 `BtsmtlSkillApplyGameplayEffectFlowNode` 或 `BtsmtlSkillRemoveGameplayEffectFlowNode` 时，才调用 `RequireGameplayCapability("GameplayEffect")`。
- StateMachine 和 Timeline 的原有能力声明保持不变；已删除按 Ability 全量无条件声明 Gameplay Effect 的路径。
- `ThirdPersonClient.Editor.csproj` 全量编译被本机生成索引对 Fixed / Float32 新源的旧引用阻断；该基线错误出现在依赖 `ThirdPersonSimulation.Fixed.csproj` 和 `ThirdPersonSimulation.Float32.csproj`。Frontend 改动先通过语法审查提交，待 Unity 刷新生成索引后再做全量验证。
- 未运行 Unity、测试或资产生成。

## 2026-09-15 角色宿主与注册器退出旧Program/Projection必要条件

- 提交 `2c25bc026`。基线 `dc581b621` 之前的 `886415fd7`（提交信息为清理 ACL 缓存，实际改动 569 个文件）把 `FixedCharacterHost.cs` 回退成旧 `FixedCharacterSimulationProgramAsset` 形态并删除 49 个核心文件；`dc581b621` 只恢复了核心文件，未恢复该 Host，导致 Host 引用已删除的 `FixedCharacterSimulationProgramAsset`、`CharacterSimulationProgram`、`CharacterPresentationContractAdapter`、`CharacterRuntimeDebugProgramBuilder`，`ThirdPersonSimulation.Unity.csproj` 报 25 个错误。本批按 `1c70d267c`（让 Fixed 角色宿主直接组装 Character Runtime）的权威形态重建。
- `FixedCharacterHost`：删除 `m_Program` 与 `m_PresentationProjection` 序列化字段、`programAsset.Load()` 与 Program TickRate 比较；投影改从 `CharacterPipelineDefinition.PresentationProjection` 读取；技能数据改由 `LoadFixedCharacterAbilities()` 加载，经 `SimulationActorBinding` 装配 `FixedCharacterRuntime`；`tickRate` 由 `SessionHost.Composition.TickRate` 提供并与 Definition 校验（对应 design D22「NumericProfile／TickRate 由正式 Session／数值配置决定，不从第一个技能反推」）；诊断身份由 `program.Manifest` 改为 `RuntimeProgramRevision`。
- `FixedCharacterRegistration` 与 `Float32CharacterRegistration`：移除 `CharacterPresentationSemanticContract` 参数、`PresentationContract` 属性与 `Projection.RequireContract` 调用。该合同运行时已无构造入口（`CharacterPresentationSemanticReader` 只在 Editor），且属性无消费者，属旧 Projection 必要条件。
- `Float32CharacterRegistration` 从 `ThirdPersonSimulation.Unity` 移入新建的 `ThirdPersonSimulation.Float32.Unity` 桥接程序集（与 `ThirdPersonSimulation.Fixed.Unity` 同构）。原位置无 `ThirdPersonClient.Runtime` 引用却使用 Presentation／Diagnostics 类型，反向引用会形成循环依赖。该注册器当前无任何调用方，尚未接入 Host。
- `Fixed`／`Float32SimulationSessionCompositionPreparation`：去掉 `new GameplayContentHash(...)` 多余包装，`SimulationCharacterRuntimeDescriptor` 直接接收 `GameplayContentHash`。
- 验证：`ThirdPersonSimulation.Unity.csproj` 由 25 个错误降为 0 warning、0 error（本机 dotnet build，禁用 build server，结束执行 `dotnet build-server shutdown`）。`ThirdPersonClient.Runtime.csproj` 仍有 Pose 旧 `CharacterPoseProgramImage`／`CharacterPoseNative*Operation` 体系的中间断裂，属 Pose 窗口范围，本批不涉及；rollback Host、`CharacterPipelineHost`、DotRecast manifest 的旧 Program 消费者仍未接线。未运行 Unity、测试或资产生成。

## 2026-09-15 Rollback运行时宿主接入独立Fixed Runtime

- `DeterministicRollbackCharacterHost` 删除旧的 `FixedCharacterSimulationProgramAsset`、独立 Projection 序列化字段和 `programAsset.Load()`；Projection 从 `CharacterPipelineDefinition` 读取，技能执行数据由 Definition 的 `LoadFixedCharacterAbilities()` 生成，连同 Control／BodyMotion／GameplayEffect／Equipment binding 装配为 `SimulationActorBinding`，再创建 `FixedCharacterRuntime`。
- Rollback 的 tick rate 统一取 `SimulationSessionHost.Composition.TickRate`，并与 Character Definition 校验；输入适配器直接接收正式 `CharacterControlModuleContract`，Presentation 只使用 Definition 的 Projection 和当前 Runtime tick rate。诊断修订号改为 Runtime actor identity、首个 Ability source revision 与 `GameplayContentHash`，不再从整角色 Program manifest 生成。
- `DeterministicRollbackCharacterRegistration` 不再保存或暴露 `Program`、`ProgramIdentity`、`PresentationContract` 和重复的领域 binding；它持有 `FixedCharacterRuntime` 与同一 Runtime roster 中的 `SimulationActorBinding`，以 Runtime／binding 内容 hash 生成输出与诊断配置身份，Rollback Pass 继续从 registration 的正式 Runtime 接口取角色数据。
- 这一步没有新增兼容字段或桥接。Rollback Editor authoring 调用方、`CharacterPipelineHost`、DotRecast manifest 和 Endpoint 仍有旧链路，分别留给其所属的 Editor／Network／Authority 接线；不能为了让中间工程暂时通过而复活旧 Program。
- 验证：执行 `ThirdPersonSimulation.DeterministicRollback.Unity.csproj` 的 `dotnet build --disable-build-servers /nr:false /p:UseSharedCompilation=false --no-restore`，随后执行 `dotnet build-server shutdown`。构建被并行窗口已有的 Endpoint 旧 `RollbackHandshake`／`RollbackActorHash` 字段错误，以及 `Float32CharacterRegistration` 装配错误阻断；输出未出现本步两个 Rollback 文件的错误。未运行 Unity、测试或资产生成。

## 2026-09-15 本地Float32宿主退出旧Program

- `CharacterPipelineHost` 不再读取 `CharacterPipelineDefinition.SimulationProgram`、调用 `Load()`、建立 `CharacterPresentationSemanticContract` 或创建旧 `CharacterSimulationActorRegistration`；它现在从 Definition 生成 Control／BodyMotion／GameplayEffect／Equipment binding，加载 Float32 Ability 执行数据，组装 `SimulationActorBinding` 与 `Float32CharacterRuntime`，再交给 `Float32CharacterRegistration`。
- Local Host 的 tick rate 只取 `SimulationSessionHost.Composition.TickRate`，输入源只接收正式 `CharacterControlModuleContract`；Projection 通过 `CharacterPresentationRuntimeFactory.LoadProjection` 读取，诊断身份使用 Runtime actor、首个 Ability source revision 和 Runtime content hash。Presentation factory 复用宿主已有的正式创建路径，不再向注册器传入旧 Program 闭包。
- `Float32CharacterRegistration` 放回 `ThirdPersonClient.Runtime` 所拥有的 Character Pipeline Unity 目录，与 `CharacterPipelineHost` 位于同一程序集边界；删除只承载该注册器且反向引用 `ThirdPersonClient.Runtime` 的 `ThirdPersonSimulation.Float32.Unity` 桥接程序集，避免 Host → 注册器 → Host 的循环依赖。注册器的 Unity GUID 保持不变，运行时类型命名空间不变。
- 验证：`ThirdPersonSimulation.Unity.csproj` 和旧生成工程仍把已迁移注册器按旧路径编入，构建因此继续报告注册器缺少 Client Runtime Presentation／Diagnostics 引用；这是 Unity 重新生成程序集工程前的生成索引阻断，未出现 `CharacterPipelineHost.cs` 的错误。已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-15 BlendSpace预览退出旧Program读取

- `CharacterAnimationBlendSpaceWindow` 的状态判断、已发布计划读取和相位预览现在只依赖 `CharacterPresentationProjectionAsset.Load()` 返回的正式 Projection；删除 `CharacterPipelineDefinition.SimulationProgram` 必须存在、`SimulationProgram.Load()` 和 `Float32CharacterPresentationContractAdapter` 组合。
- 这条工具链的业务输入是 BlendSpace 的 Projection 计划与作者参数，不执行角色 Ability，也不需要整角色 Program identity；保留 Projection revision、BlendSpace content revision 和已有权重／相位算法校验，避免把工具职责扩成角色构建入口。
- 未运行 Unity 或测试；当前 Editor 全量工程仍受并行窗口的 Pose 旧 `CharacterPoseProgramImage`／`CharacterPoseNative*Operation` 中间断裂影响，待对应 Pose 公共接线收口后统一编译。

## 2026-09-15 Fixed回放身份改用Character Runtime

- `CharacterFixedInputTraceWorkflow` 的回放身份不再检查或加载 `FixedCharacterHost.ProgramAsset`；它要求已激活的 `FixedCharacterRegistration`，从注册器的 `FixedCharacterRuntime` 读取 Runtime 内容 hash、tick rate，并从其 Ability 数据读取来源修订和语义 hash。
- 回放证明字段由 `program_id`／`program_hash` 改为 `runtime_id`／`runtime_content_hash`，调度证明与普通证明的 schema 分别升级，比较、校验和证明 hash 使用同一 Runtime 身份。旧证明不再解析，避免把旧 Program 证明伪装成新 Runtime 结果。
- 未运行 Unity 或测试；当前 Editor 工程仍受并行窗口的 Pose 旧链路阻断，未把该阻断伪装成回放验证结果。

## 2026-09-15 技能诊断入口直接定位Character Definition

- `BtsmtlSkillHostEntry` 不再从 `FixedCharacterHost.ProgramAsset` 反查 `DefinitionGuid`；Fixed Host 与本地 Host 一样直接暴露并返回 `CharacterDefinition`。
- 这条链路只负责编辑器技能图定位，不需要读取运行时 Program、构建 Program identity 或重新加载资产；删除旧 reader 后保持作者入口的输入仍是正式 Character Definition。
- 未运行 Unity 或测试；本步为独立静态接线，后续随 Editor 工程统一刷新验证。

## 2026-09-15 Pose作者工作区退出旧Program读取

- `CharacterPoseGraphWorkspace` 的已发布 Projection 查询不再要求 `CharacterPipelineDefinition.SimulationProgram`，也不再调用已经删除的 `CharacterPosePublishedProjectionReader`。
- 工作区继续校验 Definition、Animation Profile 与 Projection 是同一组作者上下文，然后直接读取 `CharacterPresentationProjectionAsset.Load()`；异常仍只作为作者状态返回，不把 Pose 工作区变成角色运行时装配入口。
- 未运行 Unity 或测试；本步只移除旧 Program 读路径，Pose 节点与采样算法仍由 Pose 领域负责。

## 2026-09-15 Presentation运行身份退出Program命名

- `AnimationPresentationProgramIdentity` 改名为 `AnimationPresentationIdentity`，其实际身份只由 Projection revision、Pose graph revision 与 Pose plan hash 组成，不再把 Presentation 诊断目标称为角色 Program。
- Fixed、Float32、Rollback 注册器和 Host 使用新 Presentation identity；Foot IK、Presentation Replication 采样元数据与 Pose 作者观察入口同步改名，采样算法和身份内容不变。
- 未运行 Unity 或测试；本步只收口身份命名，未修改 Pose 内部状态或诊断算法。

## 2026-09-15 Fixed回放调度证明退出program_hash

- `CharacterFixedInputPresentationSchedule` 的绑定字段和 JSON 字段由 `program_hash` 改为 `runtime_content_hash`，schema 升级为 `character-fixed-input-presentation-schedule/2`。
- 调度文件、回放身份和调度绑定现在使用同一 Fixed Runtime 内容 hash；旧 schema 不再解析，避免旧 Program 调度文件混入新 Runtime 回放。
- 未运行 Unity 或测试；本步只收口回放调度格式。

## 2026-09-15 Frontend按可达节点声明Equipment能力

- 在 `GameplayAbilitySemanticFrontendCompiler.RequireGraphCapabilities` 的可达节点遍历中增加 Equipment 节点识别：`ReadEquipmentIdentityNode`、`ReadEquipmentParameterNode`、`EquipmentChangeOperationNode`（`ThirdPersonCharacter.Pipeline.Graph`），命中即 `RequireGameplayCapability("Equipment")`，与既有 GameplayEffect 的条件声明同类。
- 背景：`Equipment` 能力被 `Fixed`／`Float32SimulationActorBinding`、`GameplayAbilityExecutionInstallationSet`、`CharacterRuntimePorts`、`AbilityInvocationRuntime` 与 `NetworkTestProductAdapterUtility` 读取，用于决定是否创建角色装备状态分区与装配装备服务；但 Frontend 此前没有任何声明点，使用装备节点的技能其能力位恒为 false，角色装备状态不会初始化。
- 按 D22／1.11 的口径，依赖角色事实的节点只要求自身需要的服务：使用装备节点才声明 Equipment，未使用不声明；装配阶段缺 binding 由 `SimulationActorBinding` 明确失败，不做空实现或全局启用。
- 本切口不勾选 1.11：TreeClip 等非角色调用方尚未统一到 `AbilityInvocationRuntime` 同一执行入口，`PendingAbilityEvaluation` 仍强持角色事务，BodyFacts 仍要求必给。
- 未编译验证：`ThirdPersonClient.Editor.csproj` 依赖的 `ThirdPersonClient.Runtime.csproj` 仍被 Pose 旧 `CharacterPoseProgramImage`／`CharacterPoseNative*Operation` 消费者的中间断裂阻断；本改动引用的类型均位于 `ThirdPersonClient.Runtime`，待该断裂收口后随 Editor 全量编译验证。未运行 Unity、测试或资产生成。

## 2026-09-15 Projection身份退出Program字段

- `CharacterPresentationProjection` 的序列化身份字段由 `m_ProgramId`／`ProgramId` 改为 `m_PresentationId`／`PresentationId`，Projection ABI 从 v15 升到 v16；旧 Projection 资产直接失效，不保留兼容字段。
- Local Host、Rollback Editor 检查、Presentation Runtime、Motion Matching、Pose tuning target 与性能采样均改用 `PresentationId`；Projection 仍校验现有 semantic contract 和 Pose tuning layout 的正式版本，不建立转换路径。
- 未运行 Unity 或测试；统一 Runtime 工程仍受 Unity 生成 csproj 引用已迁移 `Float32CharacterRegistration.cs` 旧路径的 `CS2001` 阻断，已执行 `dotnet build-server shutdown`。

## 2026-09-15 DotRecast Authority manifest退出整角色Program载荷

- Authority manifest 的单个 `CharacterProgram.csim` binding 改为已排序的 Ability artifact binding 集合；每个条目锁定 Ability GUID、AbilityId、执行内容 hash、状态 schema、canonical 字节 hash、编译／语义／数值 ABI、TickRate、执行身份和 root。
- Authority loader 先按 manifest 读取并校验所有独立 Ability artifact，再装配当前 `SimulationActorBinding`、`Float32CharacterRuntime` 和公开的 `Float32CharacterRuntimeStateCodec`；初始状态按实际角色 Runtime 解码，网络 replication policy 也按当前 Ability producer catalog 校验。
- Authority Scene Runtime 复用 loader 已建立的 Character Runtime，Source、Pipeline、初始角色状态不再接收或重建 `CharacterSimulationProgram`；Snapshot codec identity 改用正式 `Float32SimulationTarget.Manifest.ExecutionTarget`。
- manifest schema 从 8 升为 9，旧整角色 Program manifest 直接拒绝。portable DotRecast Authority 工程已编译为 0 warning、0 error；未运行 Unity、测试或资产生成。

## 2026-09-15 Character Runtime禁止从首个Ability推断TickRate

- 删除 `Float32CharacterRuntime.Create(roster, controlModules)` 与 `FixedCharacterRuntime.Create(roster, controlModules)` 两个隐式工厂入口。
- Character Runtime 的 NumericProfile、TickRate、OperationSetVersion 现在只能由调用方从正式 Simulation Session／Execution Target 传入；Runtime 不再读取第一个 Ability 的 TickRate，也不再要求 roster 至少安装一个 Ability 才能创建。
- 现有 Local、Fixed、Rollback 与 Session Composition Preparation 已使用显式 tick rate；DotRecast Authority 旧 manifest 仍调用被删除的入口，作为后续按独立 Ability artifact 迁移的明确断点。
- 未运行 Unity 或测试；Runtime 工程编译按项目规则执行，若仍出现 Unity 生成工程的旧路径错误，只记录为生成索引阻断。

## 2026-09-15 Float32角色状态codec开放给Authority程序集

- `Float32CharacterRuntimeStateCodec` 从程序集内部接口改为公开正式接口，保留现有 `Write`、`Read` 与 `ComputeHash` 的同一状态格式和校验规则。
- DotRecast Authority 后续直接用当前 Float32 Runtime 的 Ability 安装集合、角色内容 hash 和领域状态读取初始状态；不再依赖旧 `CharacterSimulationState` 或整角色 Program reader。
- 未运行 Unity 或测试；Float32 portable Runtime 编译按项目规则执行并清理 .NET Host。

## 2026-09-15 角色旧Program入口与产品工具链清理

- 提交 `061763694`、`30a9acbf3`、`85ffc2369`，从正式角色组合、回滚 Source 和 Fixed／Rollback 角色 Prefab 中删除悬空的 `Program`、`ProgramRuntime`、`FixedProgram` 与 `PresentationProjection` 序列化字段；运行时入口已经由 Character Definition、Session Composition 和正式 Presentation 数据提供。
- 提交 `d01074187`，将只接收 Ability 根的 `CharacterSimulationProgramBuilder` 移到技能编译目录并改名为 `GameplayAbilitySemanticBuilder`，保留脚本 GUID；技能编译职责不再挂在角色 Program 目录和命名下。
- 提交 `36c08802d`，删除只读取已不存在整角色产物的 `ThirdPersonSimulation.Reader` Program 入口和 `FixedProgramBuildTool`，同步删除仓库策略白名单；不保留旧 reader、兼容别名或空壳构建器。
- 提交 `899c2d101`，删除无消费者的三份 GameplayLab Variant、两份生成角色 Program 资产、空 Variants 目录元文件及源清单中的失效资产登记。

## 2026-09-15 回滚网络与Authority身份接回正式Runtime

- 提交 `254dda56e`、`9f969dbb1`，回滚候选清单、Relay、GM 查询和差异诊断改用 `GameplayContentHash` 与 `CharacterStateHash`；旧 `Program`、角色模块和整角色布局字段不再进入网络握手、候选校验或差异定位。
- 提交 `310226b1f`，Unity Authority 注册处理器从删除的 `request.Program` 改读现行 `request.Runtime`，使用 Character Runtime、State Codec、Checkpoint Layout、Operation Set 的正式身份完成注册映射；Unity Authority 服务端编译为 0 warning、0 error。
- 提交 `8f7518e1c`，回滚网络测试产品直接从正式 Character Definition、Session Composition、Source、Pipeline、World Solver、Endpoint 取运行闭包；场景直接实例化双角色根 Prefab，不再经过旧 Bootstrap。

## 2026-09-15 固定诊断与性能产品改用正式领域身份

- 提交 `b63ccb28f`、`b450026b7`，固定性能场景和本地角色 Prefab 补齐正式 Character Definition；固定输入回放、性能采集和 Launcher 改用 `fixed-player`／`fixed-target`、`runtime_id`、`content_identity`，删除 Bootstrap、Session Variant、Launcher Registry 和角色 Program 元数据依赖。
- 提交 `8bd8a914d`，网络产品清单将实际的 Character／Gameplay 内容身份从 `programIdentity` 改为 `contentIdentity`，候选清单 schema 从 3 升为 4 并要求内容身份存在。
- 提交 `3b6ece2f9`，外部性能 Controller／Publisher 与共享性能契约对齐：BuildIdentity 读取 `content_identity`，采集 Manifest 记录 `runtime_id`／`content_identity`，删除 Variant 启动参数和比较字段；Controller 编译为 0 warning、0 error。

## 2026-09-15 Ability执行服务边界收口

- 提交 `ea53b69de`，修正 Float32 独立 Ability artifact 写入字段顺序，使其与 Fixed 产物和当前 codec 合同一致；Float32 portable 编译为 0 warning、0 error。
- 提交 `f38cf726d`，Float32／Fixed Ability Invocation 不再因未提供 Body Facts 就拒绝创建；能力真正读取位置、速度、朝向、接地等角色事实时才通过按需服务明确失败，不用默认零值伪造事实。Float32／Fixed portable 编译均为 0 warning、0 error。
- 当前仍未完成 1.11：Pending 角色结果仍携带外层角色事务和 WorldSolve 请求；InputRequests 已从 Ability 总端口拆为独立共享消费入口，但底层仍由角色事务持有。Timeline 的真实播放结果、Pose 的真实采样结果和统一角色 Step 的跨领域提交仍待接线。未运行 Unity、测试或资产生成。

## 2026-09-15 Ability输入请求端口独立化

- 提交 `319fa44f0`，从 Float32／Fixed `AbilityDomainStatePort` 移除 `GetInputRequest`／`SetInputRequest`，新增角色共享的 `InputRequestStatePort`；Ability 执行帧、Input 操作、角色输入聚合和 Control 读取各自只依赖输入请求端口。
- 输入请求数据仍由同一个角色状态事务保存、保存点恢复和最终提交，角色状态 codec 不变；本步拆的是职责入口，不复制状态、不增加兼容实现，后续可在不改 Ability 执行器的前提下把输入状态聚合真正移出角色大事务。
- Float32／Fixed portable 编译均为 0 warning、0 error，并已清理 .NET Host；未运行 Unity、测试或资产生成。本步推进 D22 的 Ability 端口边界，但不将 1.11 标记为完成。

## 2026-09-15 Ability动作状态端口独立化

- 提交 `2397d7ac6`，从 Float32／Fixed `AbilityDomainStatePort` 移除 Action 实例、激活请求和 Action 事件序号，新增 `ActionRuntimeStatePort`；Ability ActionStore、Action 生命周期读取和角色 Control 活跃判断改用专用端口。
- Action 数据仍由同一个角色状态事务保存、保存点恢复和最终提交，角色状态 codec 不变；本步只拆职责入口，保持一个提交／回滚路径，不创建第二份 Action 状态或兼容接口。
- Float32／Fixed portable 编译均为 0 warning、0 error，并已清理 .NET Host；未运行 Unity、测试或资产生成。本步推进 D22 的 Control／Action 边界，但不将 1.11 标记为完成。

## 2026-09-15 Ability句柄分配端口独立化

- 提交 `2a1a7bc48`，从 Float32／Fixed `AbilityDomainStatePort` 移除 Handle 分配、捕获和恢复操作，新增 `HandleAllocatorStatePort`；Ability 执行帧和句柄服务只通过专用端口操作角色句柄序列。
- 句柄序列仍由同一个角色状态事务保存、保存点恢复和最终提交，未改变状态 codec、事件语义或网络身份；本步只收窄 Ability 可见的状态入口，不增加兼容路径。
- Float32／Fixed portable 编译均为 0 warning、0 error，并已清理 .NET Host；未运行 Unity、测试或资产生成。本步推进 D22 的 Ability 基础服务边界，但不将 1.11 标记为完成。

## 2026-09-15 Ability事件序号端口独立化

- 提交 `8aaca56b0`，从 Float32／Fixed `AbilityDomainStatePort` 移除事件序号分配，新增 `EventSequenceStatePort`；Ability 事件发射器只通过专用端口生成角色事件序号。
- 事件序列仍由同一个角色状态事务保存、保存点恢复和最终提交，事件格式、排序和网络身份不变；本步继续收窄 Ability 可见的角色事务入口，不增加兼容路径。
- Float32／Fixed portable 编译均为 0 warning、0 error，并已清理 .NET Host；未运行 Unity、测试或资产生成。本步推进 D22 的 Ability 基础服务边界，但不将 1.11 标记为完成。

## 2026-09-15 AbilityGameplayEffect状态端口独立化

- 提交 `ed332f7ec`，从 Float32／Fixed `AbilityDomainStatePort` 移除 GameplayEffect 工作态与已提交聚合态读取，新增专用 `GameplayEffectStatePort`；Effect Target、Mapping 和 Operation Runtime 只通过效果状态端口读取效果数据。
- 保存点、恢复、释放与诊断仍由角色事务控制端口提供，效果状态没有复制到第二份容器，状态编码、回滚语义和最终提交路径不变；本步只分离状态消费职责，不保留旧总端口兼容入口。
- Float32／Fixed portable 编译均为 0 warning、0 error，并已清理 .NET Host；未运行 Unity、测试或资产生成。本步推进 D22 的 GameplayEffect 边界，但不将 1.11 标记为完成。

## 2026-09-15 AbilityEquipment状态端口独立化

- 提交 `e1c43e383`，从 Float32／Fixed `AbilityDomainStatePort` 移除 Equipment 状态读写，新增专用 `EquipmentStatePort`；Equipment Runtime 的装备槽、装备本地状态和变更写入只通过该端口完成。
- 装备状态仍由同一个角色状态事务保存、保存点恢复和最终提交；装备运行时使用的保存点与诊断仍走事务控制入口，不复制状态、不增加兼容端口。
- Float32／Fixed portable 编译均尝试执行并复现 Unity 生成 `.csproj` 缺少现有 `GameplayAbilityExecutionLayout.cs` 等源文件的既有索引阻断（Float32 29 个错误、Fixed 25 个错误，均为缺失类型）；两次均为 0 warning，并已清理 .NET Host。未运行 Unity、测试或资产生成。本步推进 D22 的 Equipment 边界，但不将 1.11 标记为完成。

## 2026-09-15 Ability事务控制端口收口

- 提交 `9325c2ecc`，删除混合的 Float32／Fixed `AbilityDomainStatePort`，改为只包含保存点创建、恢复、释放和诊断的 `AbilityTransactionControlPort`；Effect Control Port、Effect Target 与 Equipment Mutation Scope 改用该专用入口。
- 角色身份字段和无消费者的角色事务 `Abort` 接口一并删除；角色状态仍由原有事务实例统一保存、回滚、最终提交或 Dispose，不建立第二条事务链。
- 未重新编译：当前 Float32／Fixed 生成 `.csproj` 仍缺少现有 `GameplayAbilityExecutionLayout.cs` 等源文件，前一步已复现同一源索引阻断并清理 .NET Host；未运行 Unity、测试或资产生成。本步完成 D22 的 Ability 事务控制入口收口，但不将 1.11 标记为完成。

## 2026-09-15 CharacterControl状态绑定端口独立化

- 提交 `d20ea70a2`，新增 Float32／Fixed `ControlRuntimeStatePort`；Character Control Runtime 不再接收具体 `CharacterRuntimeStateTransaction`，只通过端口绑定 Control 状态事务。
- Control 状态的编码、保存点恢复和角色事务最终提交路径不变；本步只移除 Control 对角色总事务实现类型的直接依赖，不创建第二份 Control 状态。
- 未重新编译：仍受前述 Unity 生成 `.csproj` 缺少现有 Ability Layout 源文件的源索引阻断；未运行 Unity、测试或资产生成。本步推进 D22 的 Control 状态边界，但不将 1.11 标记为完成。

## 2026-09-15 Ability下Timeline保留态删除

- 提交 `e106d1310`、`d5d9e0bc7`，删除 Float32／Fixed Ability 状态、Ability 事务和角色状态 codec 中没有运行时消费者的 Timeline retained action context；同步删除 Ability execution layout 的 Timeline retention 索引，不再把 Timeline 私有播放状态挂在技能分区下。
- Float32／Fixed 角色状态 codec 身份与 hash identity 升为 `/3`，旧状态载荷直接拒绝；MotionWarp 状态仍保留在技能执行状态分区，因为 Float32／Fixed Motion Runtime 当前确实读写它。Control 绑定端口的接口实现同时修正为公开成员，消除上一笔拆端口留下的接口实现错误。
- Float32／Fixed portable 编译均按项目规则执行并清理 .NET Host：Float32 28 个错误、Fixed 24 个错误，均为 Unity 生成 `.csproj` 漏掉现有 Ability Layout 源文件导致的缺失类型，0 warning；此前的 `BindControl` 接口实现错误已不再出现。未运行 Unity、测试或资产生成。本步推进 D22 的 Timeline 状态归属边界，但不将 1.11 或 2.6 标记为完成。

## 2026-09-15 Equipment角色状态布局脱离Ability安装

- 提交 `a45c7272b`，新增 `EquipmentProgramLayoutCompiler.CompileRoleStateLayout`；Float32／Fixed 角色初始 Equipment 状态和角色状态 codec 直接从 `CharacterEquipmentRuntimeBinding` 编译角色级布局，不再从第一个声明 Equipment 的 Ability 读取 catalog、references 和 producers。
- Ability 执行时仍使用自己的 operation reference 和 producer 校验；角色状态布局只负责 slots、features、items、routes、parameters 和 local states。这样角色状态不再依赖 Ability 安装顺序，也没有复制第二份 Equipment 配置或引入兼容路径。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error。Float32 仍被 Unity 生成 `.csproj` 漏掉 `GameplayAbilityExecutionLayout.cs` 等既有源文件阻断（28 个错误、0 warning）；Fixed 同一源索引阻断（24 个错误、0 warning）。每次编译后均已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-15 Authority加载复用Ability Provider合同

- 提交 `ba920e296`，DotRecast Authority manifest 从 schema 9 升为 schema 10；导出时保存 Character Definition 生成的完整 Provider binding，包含 Provider owner、合同版本、成员值类型、成员修订和运行句柄。
- Authority loader 读取独立 Ability artifact 后调用同一个 `GameplayAbilityProviderContract.RequireBinding`，因此 Unity Definition Load 与 Authority artifact Load 对成员存在性、值类型、合同版本和运行句柄使用同一条校验链；旧 manifest 直接拒绝，不保留兼容格式。
- 角色调用的 InstallationSet 只向声明 GameplayEffect 的 Ability 安装传入 Effect binding，未声明该能力的 Ability 不会被错误拒绝；必需服务缺失仍直接失败，不创建空实现或第二套绑定路径。
- Authority portable 编译仍被 Unity 生成 `ThirdPersonSimulation.Float32.csproj` 漏掉既有 Ability Layout 等源文件阻断（28 个缺失类型错误、0 warning）；编译结束已执行 `dotnet build-server shutdown`。未运行 Unity、测试或资产生成。

## 2026-09-15 Ability按需服务入口收口

- 提交 `ef197c18e`，Float32／Fixed `AbilityExecutionFrame` 不再强制接收事务控制、GameplayEffect 状态和 Equipment 状态；未提供时只有实际访问对应服务才明确抛出缺失错误，避免独立技能调用方伪造整套角色服务。
- Float32／Fixed Equipment Runtime 允许 Equipment-only 技能在没有 GameplayEffect Runtime 时创建；装备标签、被动效果和效果输出提交仍在实际调用时要求 GameplayEffect 服务，普通装备状态读写不再被无关能力阻断。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。未运行 Unity、测试或资产生成；1.11 仍未完成，角色 Pending 结果与跨领域 Step 接线继续保留为后续边界。

## 2026-09-15 角色输入请求状态拆出

- 提交 `43f9bcd5f`，Float32／Fixed 新增独立的 `CharacterInputRequestState`，角色运行时事务不再实现 `InputRequestStatePort`；Ability、角色输入聚合和角色 Control 都通过独立输入端口访问共享请求。
- 角色事务仍是单一 Savepoint／Commit 的编排者，但输入请求的字典复制、读取和写入已由输入状态对象负责；事务快照与恢复继续调用该对象，角色状态 codec、角色 Step 和输入请求格式不变，没有复制状态或增加兼容路径。
- 输入状态与角色事务绑定同一生命周期，事务释放时同步关闭输入端口，避免独立端口脱离当前角色 Step 后继续被使用。本步完成的是输入状态 owner 与消费入口拆分，不宣称角色总状态聚合已完成，1.11、2.6 仍未完成。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。Center 正式 compile 的修改前与修改后阶段均因打开的 Unity Editor 占用本 worktree 被 `WorkspaceEditorInUse` 拒绝；未关闭 Editor，未运行 Unity、测试或资产生成。

## 2026-09-15 角色Action运行时状态拆出

- 提交 `57f8460a3`，Float32／Fixed 新增独立的角色 Action 状态对象，统一持有 Action activation request、Action instance 和 Action event sequence；角色运行时事务不再实现 `ActionRuntimeStatePort`。
- Ability ActionStore 与角色 Control 改用专用 Action 端口，共享同一份角色动作事实；角色事务仍负责单一 Savepoint／Restore／Commit，并在快照中读出、恢复该对象，既有动作准入、替换、实例生命周期、状态 codec 和统一角色 Step 不变。
- Action 状态与角色事务同步释放，未保留旧总事务接口或第二份状态。本步只完成共享 Action 状态 owner 拆分，角色级完整 Capture／Restore 和跨领域结果接线仍未完成，1.11、2.6 仍未完成。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。本步 Center 正式 after compile 因打开的 Unity Editor 占用本 worktree 被 `WorkspaceEditorInUse` 拒绝；未关闭 Editor，未运行 Unity、测试或资产生成。

## 2026-09-15 角色事件序号状态拆出

- 提交 `dbe18ab1b`，Float32／Fixed 新增独立的角色事件序号状态，角色运行时事务不再实现 `EventSequenceStatePort`；Ability 的事件发射器改由专用端口取得角色级递增序号。
- 保留原有从角色状态读入、按帧递增、溢出拒绝、Savepoint／Restore／Commit 和 codec 编码语义；角色事务只负责在统一快照中读取并恢复该状态，不复制序号或建立旁路时钟。
- 事件序号状态与角色事务同步释放。本步只完成事件序号 owner 拆分，角色级完整跨领域 Capture／Restore 和网络恢复接线仍未完成，1.11、2.6 仍未完成。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。对应 Center 改动记录未执行正式 after compile，原因仍是打开的 Unity Editor 占用本 worktree；未关闭 Editor，未运行 Unity、测试或资产生成。

## 2026-09-15 角色句柄分配状态拆出

- 提交 `8c176d703`，Float32／Fixed 新增独立的角色句柄分配状态，角色运行时事务不再实现 `HandleAllocatorStatePort`；Ability、GameplayEffect 和 Equipment 继续通过同一专用端口共享句柄分配事实。
- 保留原有递增、溢出拒绝、捕获、恢复、Savepoint／Commit 和 codec 编码语义；角色事务只在统一快照中读出并恢复句柄状态，不复制分配器或引入旁路时钟。
- 句柄状态与角色事务同步释放。本步只完成句柄 owner 拆分，角色级完整跨领域 Capture／Restore 和网络恢复接线仍未完成，1.11、2.6 仍未完成。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。对应 Center 正式 after compile 因打开的 Unity Editor 占用本 worktree 被 `WorkspaceEditorInUse` 拒绝；未关闭 Editor，未运行 Unity、测试或资产生成。

## 2026-09-15 角色GameplayEffect状态拆出

- 提交 `8fe9b2ad8`，Float32／Fixed 新增独立的角色 GameplayEffect 状态，角色运行时事务不再实现 `GameplayEffectStatePort`；aggregate、lazy working state、执行 scratch 和 workspace 绑定由效果状态 owner 管理。
- Ability 继续通过专用效果端口访问按需服务；角色事务只在统一 Savepoint／Restore／Commit 中捕获和恢复效果状态，保留缺失服务拒绝、workspace 身份检查、效果规则、状态 codec 和角色 Step，不复制状态或增加兼容路径。
- 效果状态与角色事务同步释放。本步只完成 GameplayEffect 状态 owner 拆分，跨领域完整 Capture／Restore、网络恢复和 Pending 角色结果接线仍未完成，1.11、2.6 仍未完成。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。对应 Center 正式 after compile 因打开的 Unity Editor 占用本 worktree 被 `WorkspaceEditorInUse` 拒绝；未关闭 Editor，未运行 Unity、测试或资产生成。

## 2026-09-15 角色Equipment状态拆出

- 提交 `348d0e94b`，Float32／Fixed 新增独立的角色 Equipment 状态，角色运行时事务不再实现 `EquipmentStatePort`；装备 aggregate 的读写、空状态拒绝和生命周期由装备状态 owner 管理。
- Ability 继续通过专用装备端口访问状态，装备 mutation scope 仍走原有事务控制端口；角色事务只在统一 Savepoint／Restore／Commit 中捕获和恢复装备状态，保留装备规则、状态 codec 和角色 Step，不复制状态或增加兼容路径。
- 装备状态与角色事务同步释放。本步只完成 Equipment 状态 owner 拆分，跨领域完整 Capture／Restore、网络恢复和 Pending 角色结果接线仍未完成，1.11、2.6 仍未完成。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。对应 Center 正式 after compile 因打开的 Unity Editor 占用本 worktree 被 `WorkspaceEditorInUse` 拒绝；未关闭 Editor，未运行 Unity、测试或资产生成。

## 2026-09-16 角色Pending结果脱离挂起事务

- 提交 `64bb4008d`，Float32／Fixed Evaluate 在生成角色 WorldSolve 请求后完成角色状态事务的 Commit，并立即释放可变事务；Pending 结果改为保存不可变 `CandidateState`、World 请求和领域输出。
- Finalize 直接校验并消费候选角色状态，不再跨 Pass Claim 或 Dispose 角色状态事务；`AbortUnconsumed` 只结束 Pending 的消费生命周期，失败时不会反向中止外层角色事务。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。未运行 Unity、测试或资产生成；按 D22 收口 Pending／事务所有权边界，但 1.11、2.1、2.6 的完整技能独立执行和角色跨领域结果接线仍未完成。

## 2026-09-16 Ability局部状态事务脱离角色实现

- 提交 `2a9ecd815`，Float32／Fixed Ability 状态事务改为自己持有 Tick 和不透明绑定身份，不再保存或回指具体 `CharacterRuntimeStateTransaction`；Ability 执行帧继续只依赖局部状态接口。
- 角色聚合器仍唯一负责创建 Ability 状态事务、校验其绑定归属和接收快照，保留跨角色误接收拒绝，不复制状态、不创建第二条提交路径。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。未运行 Unity、测试或资产生成；本步推进 D22 的 Ability 局部状态边界，但 1.11 的完整外部 typed 服务执行入口仍未完成。

## 2026-09-16 角色评估结果管线命名统一

- 提交 `37f60185f`，Float32／Fixed Evaluate 到 Finalize 之间的中间产品从 `PendingActorEvaluation` 统一为 `CharacterEvaluationResult`；同步更新结果批次、Pass 读写端口、Pipeline transaction、Backend product slot 和标准 Pass contract。
- 产品合同从 `simulation.pending-actor-evaluations`／`target-pending-evaluations/1`／`actor/program/tick` 改为角色结果语义；这一步只清理旧 Program 命名，不改变角色候选状态、WorldSolve 请求、Finalize 消费和未消费结果生命周期。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。未运行 Unity、测试或资产生成；本步完成角色结果边界的命名收口，不将 2.1、2.6 或 3.2 的完整领域接线标记为完成。

## 2026-09-16 技能执行服务组装移交调用方

- 提交 `82bebd5dc`，Float32／Fixed `AbilityInvocationRuntime` 改为只接收 `AbilityExecutionAssembly`，不再在技能入口直接创建 Action、GameplayEffect、Equipment、Blackboard、Value、Motion、Locomotion、Control 和 Domain Runtime。
- 角色评估侧的执行服务工厂统一完成上述模块组装，并把正式的 InstallationSet、角色 Control／Equipment binding 和当前执行帧传入各模块；技能局部状态、输入、workspace 和执行顺序保持不变，没有新建第二套执行器、fallback 或兼容路径。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。未运行 Unity、测试或资产生成；本步推进 D22 的调用方组装边界，但非角色调用方、typed 服务入口以及完整 Timeline／Pose 前端接线仍未完成，1.11 不标记为完成。

## 2026-09-16 角色Runtime显式接收执行身份

- 提交 `7c66eb89f`，删除 Float32／Fixed `CharacterRuntime` 从全局 `SimulationTarget.Manifest` 读取 NumericProfile 和 OperationSetVersion 的静态工厂入口；Runtime 只保留接收 roster、NumericProfile、TickRate、OperationSetVersion 和 Control modules 的正式构造入口。
- Session Composition 从 `SimulationSessionCompositionDefinition.ExecutionTarget` 取得执行身份；角色 Host、Fixed Rollback、ServerAuthoritative Source、DotRecast Authority 导出与加载均在装配边界显式传入数值配置、TickRate 和操作集版本。角色 Runtime 不再隐藏读取会话目标，也不保留旧工厂别名或兼容路径。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。完整 Unity Runtime 工程仍被既有生成工程源索引缺失的 `TypedStateAddress`、`GameplayAbilityExecutionLayout` 等 30 个错误及 Unity UGUI 包只读属性错误阻断；未运行 Unity、测试或资产生成。

## 2026-09-16 技能执行服务工厂退出角色命名

- 提交 `7b16798c7`，将 Float32／Fixed `CharacterAbilityExecutionServiceFactory` 改为中性的 `AbilityExecutionServiceFactory`；角色 Evaluate 在一个 Step 内组装一次工厂，再把同一工厂传给该角色的所有 Ability Invocation。
- 工厂仍接收当前调用方提供的 Control／Equipment binding，实际模块仍按当前 Ability、InstallationSet、ExecutionFrame 和 Workspace 创建；改变的是服务装配 owner 和生命周期，不复制执行器、不增加 TreeClip 旁路，也不把角色事务重新塞回技能入口。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。非角色调用方还未接线，1.11 继续保持未完成；未运行 Unity、测试或资产生成。

## 2026-09-16 独立Ability集合加载入口去角色编译命名

- 提交 `0caed6059`，将 `CharacterPipelineDefinition.LoadFloat32CharacterAbilities`／`LoadFixedCharacterAbilities` 改为 `LoadFloat32AbilitySet`／`LoadFixedAbilitySet`，同步 Local Host、Fixed Host、Rollback Host、DotRecast Authority 导出和网络产品调用点。
- 入口仍只负责校验 Definition 的 Ability grants 与独立 Data asset 一一对应，再通过既有 Provider binding 加载 Ability execution data；没有改变 artifact 字节格式、Provider 合同、安装顺序或执行状态。
- 未重复执行完整 Unity 生成工程编译：该工程上一轮已被既有 `TypedStateAddress`、`GameplayAbilityExecutionLayout` 等源索引缺失和 UGUI 包错误阻断；本步已完成旧方法名的全仓源码残留检查，未运行 Unity、测试或资产生成。

## 2026-09-16 角色绑定持有Equipment需求

- 提交 `49bde3929`，Float32／Fixed `SimulationActorBinding` 在装配时汇总全部已安装 Ability 的 Equipment 能力，并公开角色级 `RequiresEquipment`；角色初始状态改为依据这一事实编译 Equipment 布局。
- 删除 Character Runtime 端对 Ability 安装列表的重复扫描、首个 Equipment 能力短路和运行时缺失服务判断；缺失服务仍在角色绑定装配时拒绝，Equipment 状态所有权与既有角色事务、codec 和 Step 不变。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。未运行 Unity、测试或资产生成；1.11、2.1、2.6 的完整独立技能执行与跨领域结果接线仍未完成。

## 2026-09-16 Ability执行数据加载命名统一

- 提交 `2cdfe0b69`，将 Float32／Fixed 单个和集合加载入口统一为 `Load*AbilityExecutionData`／`Load*AbilityExecutionDataSet`，同步 Character Definition 与 DotRecast Authority 导出调用点。
- 删除 `Load*GameplayAbility(ies)` 旧方法名，不保留兼容别名；加载行为仍是独立 Ability Data asset 的 Provider binding、artifact 读取与 execution data 组装，没有改变产物格式或安装顺序。
- 本步为 Unity Runtime／Editor 调用入口纯命名收口，未重复执行完整 Unity 生成工程编译：上一轮已被既有 `TypedStateAddress`、`GameplayAbilityExecutionLayout` 等源索引缺失和 UGUI 包错误阻断；未运行 Unity、测试或资产生成。

## 2026-09-16 Ability集合校验完整执行身份

- 提交 `da03adfc4`，Float32／Fixed Character Runtime 按 AbilityId 合并多个 Actor 的独立 execution data 时，新增 OperationSetVersion、NumericProfile 和 TickRate 校验，与已有 ContentHash、StateSchemaHash 一起组成完整执行身份。
- 不再允许第一份 Ability 数据静默代表其它 Actor 的不一致版本；不改变 Ability artifact、布局、安装顺序或执行算法，仍在角色 Runtime 装配边界拒绝身份冲突。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。未运行 Unity、测试或资产生成；1.10、1.11、2.1、2.6 的完整领域接线仍未完成。

## 2026-09-16 角色诊断改用Ability集合版本

- 提交 `0fb20137c`，Float32／Fixed Character Runtime 根据排序后的完整 Ability 集合生成 `AbilitySetSourceRevision`，包含 Ability 身份、来源修订、内容 hash 和状态 Schema hash。
- Character Pipeline、Fixed 和 Rollback Host 的诊断版本不再读取 `Abilities[0].SourceRevision`；角色诊断不再由某个 Ability 的排列顺序代表整套执行内容，角色 GameplayContentHash 和执行算法不变。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。未运行 Unity、测试或资产生成；Unity 完整生成工程仍受既有源索引与 UGUI 包错误阻断。

## 2026-09-16 Equipment能力集合事实收口

- 提交 `158c4531c`，Float32／Fixed `GameplayAbilityExecutionInstallationSet` 在构造时汇总已安装 Ability 的 `RequiresEquipment`，角色绑定与两个 Character State Codec 改为读取该集合事实。
- 删除角色绑定和 State Codec 对安装列表的重复 Equipment 能力扫描；Equipment 缺失服务仍在角色绑定装配时拒绝，初始状态、快照格式、Equipment 布局和执行顺序不变。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。未运行 Unity、测试或资产生成；1.10、1.11、2.1、2.6 的完整领域接线仍未完成。

## 2026-09-16 GameplayEffect能力集合事实收口

- 提交 `5de615a69`，Float32／Fixed `GameplayAbilityExecutionInstallationSet` 汇总已安装 Ability 的 `RequiresGameplayEffects` 与 `RequiresEquipment`；角色初始状态和 Evaluate 只在 Ability 实际声明 GameplayEffect 时创建效果目录与状态。
- 删除角色绑定对 GameplayEffect 能力的重复扫描；多余 GameplayEffect binding 不再创建角色级效果状态，缺失必需 binding 仍由安装集合拒绝，快照格式和执行算法不变。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。未运行 Unity、测试或资产生成；1.10、1.11、2.1、2.6 的完整领域接线仍未完成。

## 2026-09-16 Ability安装能力事实收口

- 提交 `8e613751a`，Float32／Fixed 单个 Ability 安装点解析并持有 `RequiresGameplayEffects`、`RequiresEquipment`；安装集合只汇总安装事实，服务装配只消费安装事实。
- 删除集合和每帧服务装配对能力字符串的重复解析；同一份 GameplayEffect binding 可由集合传入所有安装，但只有声明该能力的安装创建效果目录，额外 binding 不再被误当成能力状态。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。未运行 Unity、测试或资产生成；1.10、1.11、2.1、2.6 的完整领域接线仍未完成。

## 2026-09-16 Ability执行级存档点边界收口

- 提交 `38e168ffc` 与 `b01bbc97c`，Float32／Fixed Ability Frame、GameplayEffect、Equipment 和调用运行时改用数值后端自己的执行级 Savepoint 接口；角色状态事务只作为当前 Character 调用方的实现。
- 删除执行文件对 `Float32／FixedCharacterRuntimeStateSavepoint` 和角色事务诊断类型的直接依赖，保留角色快照的完整回滚内容、嵌套存档点顺序和 Effect／Equipment 原子恢复行为；没有新增第二套状态格式或兼容路径。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。未运行 Unity、测试或资产生成；独立 Ability 的非角色调用方装配仍需接入正式状态与服务提供者。

## 2026-09-16 Ability存档点端口命名统一

- 提交 `4aeafebe0`，将 Float32／Fixed Ability 执行内的 `TransactionControl` 统一改名为 `SavepointPort`，调用方传入的是执行级回滚能力，不再以角色事务命名。
- 只收口边界命名和缺失服务错误信息，GameplayEffect／Equipment 的存档、恢复、释放顺序与行为不变。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。未运行 Unity、测试或资产生成；独立 Ability 的非角色调用方装配仍需接入正式状态与服务提供者。

## 2026-09-16 角色内容身份按实际能力收口

- 提交 `ef7de8bfe`，Float32／Fixed `SimulationActorBinding` 的 `GameplayContentHash` 只纳入已安装 Ability 实际声明的 GameplayEffect／Equipment binding；未使用的多余 binding 不再改变角色内容身份。
- 控制与 BodyMotion binding 仍始终属于角色执行身份；必需领域 binding 仍在安装或角色绑定阶段直接拒绝缺失，不引入空实现或兼容路径。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。未运行 Unity、测试或资产生成；1.10、1.11、2.1、2.6 的完整领域接线仍未完成。

## 2026-09-16 诊断链路清理角色Program命名

- 提交 `b603ac068`，将诊断版本从 `RuntimeProgramRevision` 改为 `RuntimeContentRevision`，字段统一为运行时身份、来源修订和内容 hash；诊断事件、Source Map、调试会话、时间线、Inspector、预览输出和三个角色 Host 同步更新。
- 诊断 epoch 改为 `RuntimeEpoch`，Source Map 的非 Source 索引目标改为 `IndexedTarget`；删除诊断链路对角色 Program 的暗示。Ability Semantic IR、Pose 图和 Session 自身仍保留各自实际使用的 `Program` 产物命名，没有跨领域误改。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；Unity 生成工程仍被既有 52 个源索引缺失错误阻断，未发现本步诊断程序集新增错误。未运行 Unity、测试或资产生成。

## 2026-09-16 角色结果解除世界请求强制依赖

- 提交 `4a57086eb`，Float32／Fixed 角色评估结果允许没有实际的 `CharacterWorldSolveRequest`；结果仍校验 Actor、Tick 和候选状态，若存在 WorldRequest 则校验其身份一致。
- 角色 Evaluate／Finalize Pass 在自己的角色世界流程边界明确要求每个 Actor 必须产出 WorldRequest，再创建 WorldSolveBatch 或消费 WorldSolveResult；角色现有行为不变，未来非角色 Ability 调用方无需伪造世界请求。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；Float32／Fixed 生成工程仍分别被既有 28／24 个源索引缺失错误阻断，未运行 Unity、测试或资产生成。

## 2026-09-16 角色Control解除首个Ability运行时依赖

- 提交 `a679201fc`，Float32／Fixed 角色 Evaluate 不再从 `invocations[0]` 借用 Ability 的输入、Locomotion 和 Trace；Control 改由角色自己的 Control motion、Control trace 和实际角色输入／Body facts 推进。
- Control motion 的位移、转向、SourceCurve、Movement playback clock 和 locomotion timeline 解析只保留一份实现；有 Ability 时仍在原有技能运动仲裁入口汇合，无 Ability 时由角色 Control 自己解析，角色控制不再因技能集合为空而停止。
- Control trace 使用角色内容身份和角色诊断边界写入结果，Ability 的局部动作协调仍由各自 Action runtime 提供；没有新增假 Ability、兼容入口或第二套技能执行器。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；Float32／Fixed 生成工程仍分别复现既有 28／24 个源索引缺失错误，未发现本步改动文件新增错误，未运行 Unity、测试或资产生成。

## 2026-09-16 删除Ability到Control的残留绑定

- 提交 `415afc3ba`，删除 Ability execution service factory 和技能 Locomotion runtime 中已失效的 `CharacterControlRuntimeBinding`、`SubmitControl` 入口；Ability 只保留技能图自己的 Locomotion 执行。
- Control 的 SourceCurve 解析、角色输入读取和角色运动贡献全部由角色 Control motion runtime 拥有；没有保留“参数还在但职责已搬走”的兼容字段或隐式路径。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；本步未重新运行 Unity 生成工程，前一轮已复现 Float32／Fixed 既有 28／24 个源索引缺失错误，未运行测试或资产生成。

## 2026-09-16 角色共享GameplayEffect目录

- 提交 `78f6e21ff`，Float32／Fixed Ability 安装集合依据已安装 Ability 的能力事实只创建一次共享 `GameplayEffect` 运行时目录；单个 Ability 安装只接收目录，非 GameplayEffect Ability 继续不持有该服务。
- 角色 Evaluate 和初始状态创建统一复用安装集合的目录，删除每个 Ability 安装、每次评估和初始状态阶段的重复 binding 解析；实际声明 GameplayEffect 时才要求正式 binding，不引入空实现或兼容入口。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；Float32／Fixed 生成工程分别复现既有 28／24 个源索引缺失错误，未发现本步改动文件新增错误，未运行 Unity、测试或资产生成。

## 2026-09-16 Ability安装阶段预编译Equipment布局

- 提交 `5bfdbb648`，Float32／Fixed Ability 安装集合将正式 `CharacterEquipmentRuntimeBinding` 传入安装边界；声明 Equipment 能力的安装在装配时生成不可变 `EquipmentProgramLayout`，不声明该能力的安装不创建装备布局。
- Ability execution service factory 删除对角色 Equipment binding 的持有和逐帧 `Compile`；Equipment runtime 只接收安装阶段的 typed 布局，缺失正式 binding 或布局直接在安装边界拒绝，不保留延迟解析、空服务或兼容入口。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；Float32／Fixed 生成工程分别复现既有 28／24 个源索引缺失错误，未发现本步改动文件新增错误，未运行 Unity、测试或资产生成。

## 2026-09-16 收窄Ability局部状态边界

- 提交 `6d886478b`，Float32／Fixed Ability 执行帧、动作状态、装备状态和 MotionWarp 改用 `IFloat32/FixedSkillExecutionState`；实现对象只保存单个技能调用分区，不再使用角色式 `...StateTransaction` 名称。
- 删除技能局部状态接口上的 `Abort`，调用候选丢弃只释放该技能状态；角色 Step 的完整事务仍由 `CharacterRuntimeStateTransaction` 持有，技能接口不能借此中止外层角色提交。
- Float32／Fixed 生成工程分别复现既有 28／24 个源索引缺失错误，未发现本步改名新增错误；未运行 Unity、测试或资产生成。

## 2026-09-16 技能安装解析脱离角色集合类型

- 提交 `0c6dbf953`，Float32／Fixed 的 Ability Action、Invocation 和执行服务工厂改用公开的安装解析服务接口，只依赖按 `CharacterSkillId` 获取安装的正式合同。
- 角色 `GameplayAbilityExecutionInstallationSet` 继续作为当前调用方的实现，但不再作为技能执行链的具体编译依赖；现有跨技能动作解析、安装顺序和运行行为保持不变，没有新增旁路、兼容类型或第二套执行器。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 技能执行工厂移出角色评估文件

- 提交 `b338e708f`，将 Float32／Fixed `AbilityExecutionServiceFactory` 从 `CharacterEvaluationRuntime` 的嵌套实现拆为独立 Ability 文件；角色评估文件只保留角色 Step 的评估、Ingress 处理和结果汇总。
- 工厂继续由角色调用方创建并提供安装解析服务，仍按当前安装、执行帧和 Workspace 组装既有模块；没有改变状态提交、模块顺序或增加第二套执行器。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 Ability状态值移除角色命名

- 提交 `f3604a8d5`，Float32／Fixed 的 `CharacterStateValue` 统一改为 `AbilityStateValue`，同步状态槽、黑板／操作值、动作状态、执行帧、快照 Codec 与值观察合同。
- 该值对象只表示单个 Ability 的状态和执行值；角色聚合状态仍由 `CharacterRuntimeState` 持有。此次不改变状态字节格式、布局索引、默认值或执行算法，旧类型和旧文件名已删除。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 角色内容身份显式消费能力事实

- 提交 `5bf26da6d`，Float32／Fixed `SimulationActorBinding` 将安装集合汇总的 GameplayEffect／Equipment 能力事实显式传入角色内容 Hash 计算，删除静态计算对实例属性的隐式引用。
- Hash 版本、控制与 BodyMotion binding、Ability 集合内容以及“只纳入实际声明能力”的过滤规则保持不变；本步只修正角色内容身份的输入边界。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 Ability安装脱离装备角色绑定

- 提交 `0aa3ed1ab`，Float32／Fixed 的单个 Ability 安装只接收安装集合预编译的 `EquipmentProgramLayout`；`CharacterEquipmentRuntimeBinding` 只保留在角色安装集合装配边界。
- 装备布局仍按每个 Ability 的目录、引用和 producer 编译一次，声明 Equipment 的能力仍在安装时必须拥有正式布局；运行时不增加延迟编译、空服务或兼容路径。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 统一Ability局部状态错误语义

- 提交 `b956ce4c1`，将 Float32／Fixed Ability 分区值复制失败的诊断文本改为 Ability runtime state，和已完成的 `AbilityStateValue` 领域归属一致。
- 只调整错误语义，不改变异常条件、状态复制、编码格式或运行路径；Core 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。

## 2026-09-16 删除Ability执行组装死出口

- 提交 `dfbfce1c2`，Float32／Fixed `AbilityExecutionAssembly` 不再把 Invocation 未消费的 `ActionStateStore`、`ValueRuntime` 作为返回出口；这两个服务继续由执行组装内部和对应模块持有。
- 本步只删除无消费者的组装出口，不改变服务创建、状态所有权、执行顺序或运行行为；未保留兼容属性或临时桥接。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 收窄Ability执行服务集合出口

- 提交 `fabad5762`，删除 Float32／Fixed `AbilityControlRuntime` 未被消费者使用的服务转发属性和 Cursor；`ServiceSet` 只保留控制器真正需要的 Target 与生命周期依赖。
- 工厂不再把未由服务集合消费的 Input、Motion 传入集合；Input 仍由 Invocation 直接持有，Motion 仍由 Invocation 直接汇总，执行顺序和状态所有权不变。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 隐藏Ability执行服务内部依赖

- 提交 `cbb2b6701`，Float32／Fixed `AbilityExecutionServiceSet` 将动作、效果、装备、值和黑板依赖改为私有字段，只通过控制器需要的 `Target` 与生命周期方法工作。
- 删除具体实现上的无消费者属性，不改变服务持有关系、Ingress 分流、生命周期顺序或技能执行结果；未新增替代出口。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 删除Ability调用组装无用出口

- 提交 `ea4cc3cc1`，Float32／Fixed Ability Invocation 不再公开未被角色或执行流程消费的 Frame、Input、Blackboard、Motion、Locomotion、Control、Domain；ExecutionAssembly 也不再返回 Invocation 未使用的 Locomotion。
- 角色继续直接消费 Actions、GameplayEffects、Equipment 和 Workspace，技能执行内部仍持有其它服务；本步只收窄组装边界，不改变执行顺序或结果。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 区分Ability操作控制命名

- 提交 `614a2d620`，Float32／Fixed Ability 图内部的操作控制器统一命名为 `AbilityOperationControlRuntime`，同步接口、实现文件、Unity 元数据和全部调用方。
- `CharacterControlRuntime` 继续表示角色输入与运动控制；本步只消除两个领域的命名混淆，不改变操作图状态机、技能执行顺序或状态格式。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 角色通过Ability动作端口提交控制命令

- 提交 `814bfe327`，Float32／Fixed 角色 Control 改用 Ability 提供的 `I*AbilityActionControlPort`，只依赖 `ActivateFromControl` 与 `StopFromControl`；角色评估不再向 Control 暴露具体 `ActionRuntime` 字典。
- `ActionRuntime` 继续拥有具体动作执行、准入和状态实现，Invocation 只向角色发布这两个正式命令能力；输入、动作状态和执行顺序保持不变，没有新增旁路或第二套动作执行器。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 角色通过装备只读端口读取动作上下文

- 提交 `6553449ca`，扩展现有装备动作上下文合同为只读 Reader，角色评估通过 `IEquipmentActionContextReader` 查询路由和上下文；完整 Provider 仍只由 Ability ActionRuntime 使用。
- 保留原有 `HasActionRoute` 先判、`TryReadActionContext` 再读的行为和无效路由处理，不复制装备状态或路由规则，不增加角色对 `EquipmentRuntime` 实现类的依赖。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 角色以Ability能力事实驱动效果推进

- 提交 `f202b5905`，Float32／Fixed 角色评估只读取 Invocation 的 `HasGameplayEffects` 能力事实，再调用既有 Gameplay Effect 推进入口；不再向角色暴露 `GameplayEffectOperationRuntime` 实现类。
- Effect 服务仍由 Ability 执行组装内部持有，Ingress、Advance 和共享效果状态顺序不变；本步只收窄角色观察边界。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 收紧Ability动作端口类型

- 提交 `fa4ce1dfc`，Float32／Fixed Invocation 对角色发布的 `Actions` 属性统一为 `I*AbilityActionControlPort`，移除具体 `ActionRuntime` 类型泄漏。
- 内部动作执行实例、准入规则、状态写入和角色 Control 命令路径保持不变；本步只修正上一小步遗漏的实现类型出口。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 角色通过Ability事实读取动作窗口

- 提交 `822451c48`，Float32／Fixed 角色 Control 不再读取 Ability Workspace 的可变动作窗口投影列表；Invocation 提供 `HasActionWindowProjection` 事实查询。
- Ability 继续拥有 Workspace 和窗口匹配逻辑，角色只消费查询结果；动作窗口判定、调用顺序和输出数据不变，没有复制投影列表或新增旁路。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 角色通过Ability身份事实隐藏安装对象

- 提交 `e53c6b093`，Float32／Fixed Invocation 对角色只发布 `AbilityId`，角色评估不再读取 `invocation.Installation.Data.AbilityId`。
- 安装对象仍只参与 Ability Invocation 的内部执行组装；动作归属与动作窗口筛选使用同一个技能身份事实，未改变执行顺序或运行结果。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 删除执行帧安装死出口

- 提交 `0aa13c9c7`，删除 Float32／Fixed `AbilityExecutionFrame.Installation`；该属性只有构造赋值，没有读取方。
- 执行帧继续按实际消费者持有 `Data`、`Layout` 与 `Services`，Invocation 内部组装仍使用安装对象，未改变技能执行行为或数据格式。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 删除局部状态安装死出口

- 提交 `36cfdbdbd`，删除 Float32／Fixed 技能局部状态接口与运行状态上的 `Installation` 属性；这些属性没有读取方。
- 局部状态内部仍保留安装对象，用于快照构造和执行身份校验；状态槽、MotionWarp、提交与恢复路径不变。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 收窄动作绑定供应端口

- 提交 `5369f0fb3`，Float32／Fixed ActionRuntime 改用 `I*AbilityActionBindingProvider`，只取得动作准入需要的 `GameplayAbilityExecutionBinding`，不再读取完整 Ability Installation。
- 安装集合仍负责完整安装和角色装配；动作准入、生命周期、动作替换与控制命令使用同一 Binding 数据，未新增旁路或改变执行顺序。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 统一使用Ability身份事实

- 提交 `58bed96ac`，Float32／Fixed 角色评估以 Invocation 的 `AbilityId` 建立动作控制映射，不再从安装数据重复读取技能身份。
- 动作归属、动作窗口筛选和 Control 映射现在共用同一身份出口，未改变动作生命周期或角色输出。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 删除执行服务旧Ingress路径

- 提交 `6d507f7fc`，删除 AbilityExecutionServiceSet 与 OperationControlRuntime 中无调用方的统一 Ingress、Gameplay Effect 推进入口，并移除 Frame／Invocation 的死 Ingress 参数。
- 角色评估继续负责 Action／Gameplay Effect Ingress 分流和外层效果推进，Ability 服务只保留 Begin／End、局部 Tick 和实际领域服务，不改变执行顺序。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 删除状态事务身份死出口

- 提交 `cff0f464e`，删除 Float32／Fixed 角色状态事务未被消费的 `ActorId`、`Tick` 与 `TickRate` 属性。
- 事务内部仍保留时间和速率，用于技能局部状态、Control 状态和 Gameplay Effect 状态构造；本步只收窄状态事务发布面。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 移除局部输入请求副本

- 提交 `60a99e58d`，Float32／Fixed `AbilityExecutionInput` 不再携带角色级 `Requests` 列表；角色统一写入 `InputRequestState`，技能通过 `InputRequests` 端口查询和消费。
- 保留数值输入和序列输入，多个 Ability 共享同一请求消费事实，未改变 Control 或技能执行顺序。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 角色运行状态脱离Ability安装对象

- 提交 `d2a2e24a8`，Float32／Fixed `CharacterRuntimeState` 不再持有 Ability 安装集合；每个 Ability 分区只保存执行身份、局部状态、执行聚合和 MotionWarp 状态。
- Ability 安装集合继续由运行装配创建，并只在状态 Codec 读入时解析布局、校验身份和验证分区数量；状态事务的快照、恢复和提交不再反向拥有内容安装对象。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 移除Ability分区重复Tick

- 提交 `21068a8ff`，Float32／Fixed `AbilityRuntimeState` 删除未被消费的 `LastCompletedTick`；事务克隆、Codec 读入和初始状态不再把角色提交 Tick 复制到每个 Ability 分区。
- 角色唯一保留 `CharacterRuntimeState.LastCompletedTick` 作为快照、恢复和网络步骤进度，Ability 分区只保存自身局部执行状态；没有改变状态字节格式或角色提交顺序。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 统一Ability安装数据来源

- 提交 `0c9c9ee47`，Float32／Fixed `SimulationActorBinding` 不再同时发布 `AbilityData` 和 `AbilityInstallations`；角色运行、状态快照与 Authority checkpoint 统一从安装集合的 `Installation.Data` 读取。
- 删除没有消费者的 `GetAbilityData` 角色运行端口，配置输入仍在装配时创建安装集合；内容 Hash、Ability 排序、NumericProfile 校验和执行路径保持一致，没有新增第二份数据入口。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 删除未使用的可选Ability查找

- 提交 `237614267`，移除通用 Ability 数据集以及 Float32／Fixed 安装集合中没有调用方的 `TryGet` 出口。
- 当前执行与状态恢复链路继续使用按身份必须取得的 `Require`；不保留可选读取旁路，也不改变安装排序、身份校验或执行行为。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 移除状态事务无用Actor身份

- 提交 `e226d5a05`，Float32／Fixed 角色状态事务删除只用于有效性检查、但不参与状态处理的 `ActorId` 参数；角色评估入口不再向事务重复传递角色身份。
- Actor 与 WorldBody 的身份匹配仍在角色评估边界完成，事务只接收角色状态、当前 Tick 和实际状态服务；没有改变状态提交、快照或恢复顺序。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 修正状态事务时间错误语义

- 提交 `730ccdc19`，Float32／Fixed 状态事务异常文本从旧的身份语义改为明确的 Tick／TickRate 时间语义，与已删除的 `ActorId` 参数保持一致。
- 只修正诊断文本，不改变异常条件、状态提交或恢复行为；`ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。

## 2026-09-16 技能执行端口脱离角色状态实现

- 提交 `dd5b69bc4`，将 Float32／Fixed 技能局部状态、输入请求、动作、句柄、事件、Gameplay Effect 与 Equipment 的 typed 端口移到 Ability 执行域；Control 状态绑定端口单独归入角色 Control 执行域。
- 角色状态事务现在只实现这些调用方合同，不再同时定义技能执行合同；删除角色状态对象中没有调用方的 `RequireAbility` 出口。多技能聚合、角色事务提交、保存点恢复和状态编码行为不变。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。

## 2026-09-16 初始角色状态只接收Ability身份

- 提交 `e21b48be1`，Float32／Fixed `CharacterRuntimeState.CreateInitial` 从完整 Ability 安装集合收窄为只接收 `GameplayAbilityExecutionIdentity` 列表；角色运行端口在装配边界提取身份后再创建状态分区。
- 状态实现不再依赖安装对象类型，初始 Ability 分区排序、身份校验、后续事务提交和恢复链路保持不变；未引入第二份安装数据或兼容入口。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。

## 2026-09-16 收窄未消费结果的丢弃语义

- 提交 `6e059cb7b`，Float32／Fixed 评估结果、结果批次、Pass 清理和 Pipeline transaction 的 `AbortUnconsumed` 统一改为 `DiscardUnconsumed`。
- 失败清理仍只标记并丢弃未消费候选，不回滚或中止角色状态事务；命名与 1.11 的技能候选丢弃边界一致，没有改变外层提交和恢复行为。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。

## 2026-09-16 技能执行保存点收窄到领域状态

- 提交 `d3ee2c611`，Float32／Fixed 技能执行保存点删除整份 CharacterRuntimeState 快照，只保留 Gameplay Effect 聚合、Equipment 聚合、句柄分配器和事件序号；不再回滚 Ability 集合、Control、输入或动作状态。
- Gameplay Effect 状态恢复改为复用已有工作对象并原地加载聚合，避免回滚后执行目标继续引用已脱离状态事务的旧工作对象；同步删除整角色保存点专用的无调用方恢复函数。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`。Float32／Fixed 生成工程仍被既有 `.csproj` 对已删除源文件的索引阻断；未运行 Unity、测试或资产生成。

## 2026-09-16 执行帧直接使用技能局部状态

- 提交 `e7e2a729d`，删除 Float32／Fixed `AbilityExecutionFrame` 状态回退中的旧 `Transaction` 引用，状态重置和读取统一回到当前调用方提供的技能局部状态。
- 执行帧不再保留角色事务名称作为隐含入口；没有改变局部状态访问策略、操作状态语义或角色外层提交责任。
- Float32／Fixed 生成工程仍因既有 `.csproj` 索引已删除源文件而未能进入源码编译；两次构建结束均已执行 `dotnet build-server shutdown`，未运行 Unity、测试或资产生成。

## 2026-09-16 状态绑定只接收Ability执行数据

- 提交 `5d5d2a166`，Float32／Fixed `CharacterRuntimeStateTransaction.BindAbility` 从完整 Ability 安装对象收窄为执行身份、布局和执行数据；临时技能状态只保存这三项技能数据与局部状态，不再反向持有安装对象。
- 角色评估仍在装配边界持有安装对象，并在创建调用运行时前拆出三项数据传给状态事务；没有新增兼容入口、第二份状态来源或改变安装集合的所有权。
- Float32／Fixed 生成工程仍被既有 `.csproj` 对已删除 `*AbilityControlRuntime.cs`、`CharacterStateValue.cs` 的索引阻断；本步构建后已执行 `dotnet build-server shutdown`，未运行 Unity、测试或资产生成。

## 2026-09-16 Ability领域运行时脱离安装容器

- 提交 `573b0bd28`，Float32／Fixed `AbilityDomainRuntime` 删除对完整 Ability 安装对象的持有，只接收 `GameplayAbilityExecutionBinding` 和 `IGameplayAbilityExecutionServices`；领域 Tick 与完成来源路径继续从明确的执行合同读取。
- 服务工厂仍负责把安装对象拆成绑定和服务后组装领域运行时；没有复制安装数据，也没有为旧调用方保留兼容构造函数。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 控制运行时脱离安装容器

- 提交 `8f416e2d9`，Float32／Fixed `AbilityOperationControlRuntime` 从完整 Ability 安装对象收窄为执行数据和控制服务，只从执行数据读取拓扑与操作数量。
- 服务工厂继续作为组合边界，把安装对象拆成数据后创建控制运行时；Control 的状态目标、操作执行和停止语义不变，没有新增兼容构造函数。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 执行帧接收明确数据

- 提交 `81dd0f398`，Float32／Fixed `AbilityExecutionFrame` 不再接收完整 Ability 安装对象，直接接收执行数据、执行布局和执行服务；Invocation 只在组合边界完成安装对象拆解。
- Frame 的局部状态、输入、输出、诊断和领域服务访问保持原有顺序与来源；没有复制安装集合，也没有保留旧构造入口。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 删除安装对象拓扑死出口

- 提交 `f33ed358d`，删除 Float32／Fixed Ability 安装对象没有调用方的 `Topology` 属性；操作控制器直接从执行数据读取拓扑，安装对象不再重复发布同一份图结构信息。
- 安装对象仍保留状态 Codec、服务工厂和角色评估实际使用的身份、布局及能力服务，不改变技能执行或恢复行为。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 删除安装对象Access转发

- 提交 `c1f8d59b1`，删除 Float32／Fixed Ability 安装对象的 `Access` 转发属性；执行服务工厂直接从 `ExecutionServices.Access` 取得访问合同。
- Access 的所有权继续在执行服务，安装对象只保留角色评估、状态 Codec 和工厂实际需要的安装数据，不改变执行顺序或状态格式。
- `ThirdPersonSimulation.Core.csproj` 编译为 0 warning、0 error，并已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 2026-09-16 保留动作端口的内部实现边界

- 提交 `b10f70523`，Float32／Fixed `AbilityExecutionAssembly` 对外继续发布 `I*AbilityActionControlPort`，Invocation 的动作 Ingress 改为通过 Assembly 内部的 `ActionRuntime` 访问具体实现。
- 具体动作运行时只留在 Ability 执行域内部，角色调用方继续依赖窄端口；没有把实现类型重新泄漏到角色入口，也没有新增兼容路径。
- 刷新本地生成的 Float32／Fixed 工程源码清单后，两条目标工程均编译为 0 warning、0 error，并已分别执行 `dotnet build-server shutdown`；生成的 `.csproj` 被 `.gitignore` 忽略，未作为业务源码提交，未运行 Unity、测试或资产生成。

## 2026-09-16 隔离安装容器与技能执行入口

- 提交 `5dcf9303d`，Float32／Fixed 的 InvocationRuntime、执行服务工厂和角色评估入口改接独立的 `AbilityExecutionContext`；Context 只携带已加载的执行数据、布局、服务以及按能力声明绑定的 Gameplay Effect／Equipment 资源。
- 安装对象仍在角色装配边界创建并保留安装事实，技能执行入口不再接收或读取安装容器；没有复制第二份技能数据，也没有新增旧 Program 或兼容入口。
- Float32／Fixed 目标工程均编译为 0 warning、0 error，并已分别执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。
