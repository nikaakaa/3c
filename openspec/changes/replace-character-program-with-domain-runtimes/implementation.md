# 实施记录

## 当前状态

- change：`replace-character-program-with-domain-runtimes`
- 本窗口持续按独立小步提交；当前任务仍在继续。
- OpenSpec 任务：1.1、1.2、1.3、1.4 已完成；1.5 及后续任务仍未完成。1.1—1.3 的现有交付仍复用旧 `CharacterSimulationProgram` 容器，不代表最终领域工厂和准备／采用接口已经完成。
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

## 当前实现边界

- Ability 前端不读取 CharacterPipelineDefinition，不生成 Character 控制、Body Motion、Equipment 或 Pose 目录。
- Ability 图通过现有 BTSMTL Skill 图编译器复用图算法；外部 Input、Gameplay Effect、Character State 只通过 provider owner 和最小 catalog 依赖接入。
- Ability 根入口直接指向私有图的 Root operation；Float32／Fixed 的 Ability 生命周期、Action／Effect catalog、状态槽和 artifact metadata 已接通。
- Ability 目录现在为 Input／Gameplay Effect／Equipment／Character State 外部依赖发布 typed provider requirement；缺少 owner、同一依赖绑定多个 owner 或绑定类型不符时，Target 发布直接失败。
- `GameplayAbilityDataAsset` 与 `FixedGameplayAbilityDataAsset` 当前仍从 canonical bytes 读取 `CharacterSimulationProgram`，只是严格的 Ability root/catalog 校验入口；它们不是最终独立 execution data，运行时 Ability 数据接口、领域工厂、角色绑定替换和旧 Character Program 清理尚未完成。
- typed provider binding 已通过 Character Definition 的 Float32／Fixed Ability Load 入口实际消费；缺失 provider 在资源绑定阶段失败，任务 1.4 已完成。
- 当前 Character Host 仍加载旧整角色 Program，尚未把 Ability 资源集合装配进新的领域运行实例；这部分仍属于后续角色领域工厂工作。

## 编译证据与阻断

- Float32 core：`dotnet build 3cDemo/Client/3C_Client/ThirdPersonSimulation.Float32.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false --no-restore` 成功，0 warnings、0 errors。
- Fixed core：`dotnet build 3cDemo/Client/3C_Client/ThirdPersonSimulation.Fixed.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false --no-restore` 成功，0 warnings、0 errors。
- Full Editor build：`dotnet build 3cDemo/Client/3C_Client/ThirdPersonClient.Editor.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false --no-restore` 成功，0 errors、94 warnings；警告来自现有项目／依赖代码，不能替代 Unity Console、PlayMode 或端到端行为验证。
- Float32 runtime build：`dotnet build 3cDemo/Client/3C_Client/ThirdPersonClient.Runtime.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false --no-restore` 成功，0 errors、34 warnings；警告来自现有项目／依赖代码。
- Fixed provider 接入使用同一 `ThirdPersonClient.Runtime.csproj` 编译通过，0 errors、34 warnings；尚未运行 Unity、测试或端到端行为验证。
- 每次编译结束后已执行 `dotnet build-server shutdown`；未运行 Unity、测试或资产生成。

## 下一小步

下一步把 Target artifact 从旧 `CharacterSimulationProgram` 容器拆成真正的 Ability execution data，并由角色领域工厂装配；随后迁移 Control、Effect、Equipment 和网络 Pass 的状态所有权。
