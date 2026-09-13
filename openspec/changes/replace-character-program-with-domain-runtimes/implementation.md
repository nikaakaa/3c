# 实施记录

## 当前状态

- change：`replace-character-program-with-domain-runtimes`
- 本窗口持续按独立小步提交；当前任务仍在继续。
- OpenSpec 任务：1.1、1.2、1.3、1.4 已完成；2.1 按 D12 重新打开，1.5—1.10、2.2 及后续任务仍未完成。1.1—1.3 的现有交付仍复用旧 `CharacterSimulationProgram` 容器，不代表最终独立 execution data 已完成。
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

## 当前实现边界

- Ability 前端不读取 CharacterPipelineDefinition，不生成 Character 控制、Body Motion、Equipment 或 Pose 目录。
- Ability 图通过现有 BTSMTL Skill 图编译器复用图算法；外部 Input、Gameplay Effect、Character State 只通过 provider owner 和最小 catalog 依赖接入。
- Ability 根入口直接指向私有图的 Root operation；Float32／Fixed 的 Ability 生命周期、Action／Effect catalog、状态槽和 artifact metadata 已接通。
- Ability 目录现在为 Input／Gameplay Effect／Equipment／Character State 外部依赖发布 typed provider requirement；缺少 owner、同一依赖绑定多个 owner 或绑定类型不符时，Target 发布直接失败。
- `GameplayAbilityDataAsset` 与 `FixedGameplayAbilityDataAsset` 当前仍从 canonical bytes 读取 `CharacterSimulationProgram`，只是严格的 Ability root/catalog 校验入口；它们不是最终独立 execution data，运行时 Ability 数据接口、领域工厂、角色绑定替换和旧 Character Program 清理尚未完成。
- typed provider binding 已通过 Character Definition 的 Float32／Fixed Ability Load 入口实际消费；缺失 provider 在资源绑定阶段失败，任务 1.4 已完成。
- 当前 Character Host 仍加载旧整角色 Program，尚未把 Ability 资源集合装配进新的领域运行实例；这部分仍属于后续角色领域工厂工作。
- `SimulationKernel` 仍负责跨 Actor roster/binding 和 World request，但每个 Actor 的 Workspace／Evaluator 已由 `CharacterDomainRuntimeFactory` 创建，Pass 通过 `CharacterRuntime` Interface 调用 Evaluate/Finalize；Control 的静态 Motion 和 Ability 的生命周期已分别进入独立 Module。Effect／Equipment／Timeline／Motion 的其余 owner 迁移和旧 Program 数据清理仍未完成。
- Float32／Fixed Control 参数链路已改为 `CharacterPipelineDefinition.ControlParameters` → `CharacterControlRuntimeBinding` → `SimulationActorBinding`／`SimulationEvaluateRequest` → 对应 `ControlDomainRuntime`。绑定会校验 ModuleId、semantic version、参数 kind 和 ContentHash；Program adoption 也拒绝改变已安装 Actor 的 Control binding。
- Control 状态现在由每个角色的 `CharacterSimulationState.ControlState` 持有，Evaluate 为它单独开启 `CharacterControlRuntimeStateTransaction`，只有 World resolve 成功才和 Program state 一起提交；角色状态 codec、World snapshot 和 ServerAuthoritative full/delta checkpoint 都携带同一份 Control state。当前 Control state descriptor 仍复用 `ProgramStateValueKind`／`ProgramStateSemantic` 名称，旧执行服务和诊断映射也还保留，因此 2.2 仍不勾选完成。
- Control catalog 现在只发射身份、版本和初始状态字段；参数由 `CharacterControlRuntimeBinding` 提供，静态 Motion 由 `CharacterControlModuleContract.Motions` 提供，Control state 不再发射为 Program slot。Unity 输入适配器直接消费正式 Control contract；Control 曲线 Catalog 仍等待 2.3 的 Timeline/Motion 唯一时间映射迁移。
- Local Host 与 Server Authority Host 直接从 Character Definition 构造 Control binding；DotRecast Authority manifest schema 已升为 6，将模块身份、版本、参数值及两级 hash 写入 canonical payload，加载后的 Authority runtime 使用 manifest 中的同一绑定，不再从 Program catalog 补参数。

## 编译证据与阻断

- Float32 core：`dotnet build 3cDemo/Client/3C_Client/ThirdPersonSimulation.Float32.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false --no-restore` 成功，0 warnings、0 errors。
- Fixed core：`dotnet build 3cDemo/Client/3C_Client/ThirdPersonSimulation.Fixed.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false --no-restore` 成功，0 warnings、0 errors。
- Full Editor build：`dotnet build 3cDemo/Client/3C_Client/ThirdPersonClient.Editor.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false --no-restore` 成功，0 errors、94 warnings；警告来自现有项目／依赖代码，不能替代 Unity Console、PlayMode 或端到端行为验证。
- Float32 runtime build：`dotnet build 3cDemo/Client/3C_Client/ThirdPersonClient.Runtime.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false --no-restore` 成功，0 errors、34 warnings；警告来自现有项目／依赖代码。
- Fixed provider 接入使用同一 `ThirdPersonClient.Runtime.csproj` 编译通过，0 errors、34 warnings；尚未运行 Unity、测试或端到端行为验证。
- Float32 Control runtime binding 变更后，`ThirdPersonSimulation.Core.csproj`、`ThirdPersonSimulation.Float32.csproj`、`ThirdPersonSimulation.DotRecastAuthority.csproj`、`ThirdPersonClient.Runtime.csproj` 和 `ThirdPersonClient.Editor.csproj` 均编译成功，均为 0 errors；DotRecast Authority 3 warnings、Client Runtime 34 warnings、Client Editor 94 warnings，均来自现有项目或依赖代码。
- 2026-09-14 当前 Control 分区状态与 catalog 清理后，`ThirdPersonSimulation.Float32.csproj`、`ThirdPersonSimulation.Fixed.csproj` 均为 0 warnings、0 errors；`ThirdPersonSimulation.DotRecastAuthority.csproj` 为 3 warnings、0 errors；`ThirdPersonClient.Editor.csproj` 未出现本次改动错误，输出中的 warning 仍来自现有 Unity/package 代码。
- 每次编译结束后已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 下一小步

下一步把 Control state 的 value kind／semantic 合同从 `ProgramState*` 命名中彻底拆出，并清理旧执行服务、诊断和 source-map 对 Control slots 的残留假设；随后迁移 Effect、Equipment、Timeline、Motion 的实例 owner，最后把 Target artifact 从旧 `CharacterSimulationProgram` 容器拆成真正的 Ability execution data。
