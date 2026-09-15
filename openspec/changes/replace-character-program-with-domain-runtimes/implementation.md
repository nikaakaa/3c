# 实施记录

## 当前状态

- change：`replace-character-program-with-domain-runtimes`
- 本窗口持续按独立小步提交；当前任务仍在继续。
- OpenSpec 任务：1.1、1.2、1.3、1.4、1.8、2.2、2.3、2.4、2.5 已完成；2.1 按 D12 重新打开，1.5—1.7、1.9—1.10、2.6 及后续任务仍未完成。1.1—1.3 的现有交付仍复用旧 `CharacterSimulationProgram` 容器，不代表最终独立 execution data 已完成。2.6 已开始收敛：Input request、Action activation request、Action instance、Timeline retention 和完整 MotionWarp 状态已进入角色状态分区，但控制机器内部状态、技能调用帧、目标、效果、装备和统一角色 Step 的完整 Capture／Restore 尚未闭合。
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
- Action activation request 现在由 `CharacterSimulationState.ActionActivationRequests` 持有，列表中的请求保留 Action、Skill、Context、输入序号、开始 Tick、目标快照、来源、装备上下文和替换实例身份。Action instance 现在由同一角色状态的 `ActionInstances` 列表持有，保留生命周期、执行 generation、目标快照、装备上下文、停止过渡和 segment generation。Float32／Fixed 的 ActionStateStore 不再创建 Action StatePort，也不再读写 Action Program slot；Stage、pending 查找、容量、实例复用、生命周期写入和清理统一经过主状态事务。状态 codec 和 Server Authority full／delta checkpoint 以独立 bytes 携带请求与实例，Program catalog 只提供 Action 容量与内容身份。Timeline retention 现在由 `TimelineRetainedActionContexts` 按 Timeline operation identity 持有，并由 Timeline control port 经事务读写；MotionWarp 完整聚合状态现在由 `MotionWarpStates` 按 MotionWarp operation identity 持有，MotionWarp target 经同一角色事务读写，两个分区都进入状态 codec 与 Server Authority checkpoint。

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
- 当前仍未完成 1.11：Pending 角色结果仍携带外层角色事务和 WorldSolve 请求，InputRequests 尚未成为独立输入状态聚合；Timeline 的真实播放结果、Pose 的真实采样结果和统一角色 Step 的跨领域提交仍待接线。未运行 Unity、测试或资产生成。

## 2026-09-15 Ability输入请求端口独立化

- 提交 `319fa44f0`，从 Float32／Fixed `AbilityDomainStatePort` 移除 `GetInputRequest`／`SetInputRequest`，新增角色共享的 `InputRequestStatePort`；Ability 执行帧、Input 操作、角色输入聚合和 Control 读取各自只依赖输入请求端口。
- 输入请求数据仍由同一个角色状态事务保存、保存点恢复和最终提交，角色状态 codec 不变；本步拆的是职责入口，不复制状态、不增加兼容实现，后续可在不改 Ability 执行器的前提下把输入状态聚合真正移出角色大事务。
- Float32／Fixed portable 编译均为 0 warning、0 error，并已清理 .NET Host；未运行 Unity、测试或资产生成。本步推进 D22 的 Ability 端口边界，但不将 1.11 标记为完成。

## 2026-09-15 Ability动作状态端口独立化

- 提交 `2397d7ac6`，从 Float32／Fixed `AbilityDomainStatePort` 移除 Action 实例、激活请求和 Action 事件序号，新增 `ActionRuntimeStatePort`；Ability ActionStore、Action 生命周期读取和角色 Control 活跃判断改用专用端口。
- Action 数据仍由同一个角色状态事务保存、保存点恢复和最终提交，角色状态 codec 不变；本步只拆职责入口，保持一个提交／回滚路径，不创建第二份 Action 状态或兼容接口。
- Float32／Fixed portable 编译均为 0 warning、0 error，并已清理 .NET Host；未运行 Unity、测试或资产生成。本步推进 D22 的 Control／Action 边界，但不将 1.11 标记为完成。
