# Change: 重构 Unity Simulation 与 Network Model 程序集所有权

## Why

portable `ThirdPersonSimulation.Core`、`ThirdPersonSimulation.Float32`、`ThirdPersonSimulation.ServerAuthoritative`、Transport 与 DotRecast 已有独立程序集，但 Unity 侧的 Session Composition、Network Model Definition、ServerAuthoritative Pipeline/Endpoint/Source/Presentation adapter 与 Character Host 仍主要依赖预定义 `Assembly-CSharp`。当前 `UnityFloat32SimulationSessionComposer` 已通过 Runtime Package 与 Runtime Launcher 消除具体模型类型分支，但该限制只由代码约定保证，程序集依赖仍不能阻止公共 Unity Composition 重新引用 ServerAuthoritative、Fantasy 或可选 DotRecast 实现。

直接在现有 `Simulation/Unity` 目录增加一个 asmdef 会失败：公共 Composer 仍引用位于 Character Host 目录的 output/diagnostics aggregate，Character Host 又反向引用 Session Composition，形成编译环；嵌在 Runtime 下的 Editor 文件也会被错误纳入运行时程序集。因此需要一次正式的程序集所有权迁移，而不是增加一个无法闭环的 asmdef。

## Dependencies

- `refactor-float32-session-runtime-launcher-boundary`、`refactor-server-authoritative-host-portability`与`refactor-agent-authoring-compiler-modules` MUST先完成并通过strict validation，使 current specs成为迁移基线；本change按用户要求不代替实机验收执行归档。
- `add-dotrecast-authoritative-server-backend` MUST暂停在当前已完成的1.20之后；本change完成前不得继续其Unity Client Composition、Editor exporter或Scene接线任务。
- `refactor-server-authoritative-prediction-state-modules` MUST在本change之后串行实施，再恢复DotRecast change。

## What Changes

- 新增`ThirdPersonSimulation.Unity`程序集，唯一拥有model-neutral Unity Session Composition、Float32 Unity request lowering、标准Local/Preview Pipeline authoring、通用Actor registration合同与Unity CharacterController Solver adapter。
- 将只负责按Actor路由输出和diagnostics的aggregate迁入`ThirdPersonSimulation.Unity`，删除公共Composer对Character Host实现类型的反向依赖。
- 将Character专属Program/Projection wrapper、Input adapter与Presentation/Scene Host留在客户端Host程序集，不让公共Simulation程序集引用Camera、Animancer、Character authoring或具体Presentation runtime。
- 新增`ThirdPersonSimulation.DotRecast.Unity`程序集，唯一拥有`NavigationSurfaceAsset`、DotRecast state binding与DotRecast Solver Definition；公共`ThirdPersonSimulation.Unity`不得引用可选DotRecast实现。
- 新增`ThirdPersonGameplay.NetworkModel.Unity`程序集，唯一拥有model-neutral `GameplayNetworkModelDefinition`、Source requirement与preparation validation，不引用ServerAuthoritative或Fantasy。
- 新增`ThirdPersonSimulation.ServerAuthoritative.Unity`程序集，唯一拥有ServerAuthoritative Unity Model、Prediction/Authority Pipeline与Pass Definition、Endpoint/Launch、Fantasy control/data adapter、Source preparation、Authority/Client scene adapter和remote presentation adapter。
- 新增明确的客户端Runtime与Editor程序集，使剩余Character/Camera/Presentation Host代码不再借用`Assembly-CSharp`作为隐式依赖中介；Editor代码只引用正式Runtime程序集，不进入Player。
- 固定单向依赖图，任何公共Unity程序集对ServerAuthoritative、Fantasy或DotRecast具体程序集的引用都视为迁移失败。
- 保持全部现有namespace、ScriptableObject/MonoBehaviour类型名、`.meta` GUID、资产引用、Program/Pipeline/Composition identity与运行行为不变。
- 迁移完成后删除旧目录中的脚本副本、旧预定义程序集编译路径、临时asmref和任何为了绕过依赖环新增的服务定位器、反射registry或fallback入口。

## Non-Goals

- 不修改Session、Pipeline、Runtime Launcher、WorldSolver、Prediction、Animation或Presentation业务语义。
- 不改变ServerAuthoritative协议、packet、checkpoint、history、correction或Fantasy Scene所有权。
- 不实现DotRecast Authority Scene、Fixed Target、Rollback KCC或新Network Model。
- 不按每个类创建程序集，也不把portable Core重新并回Unity程序集。
- 不使用`InternalsVisibleTo("Assembly-CSharp")`、反射、字符串类型加载、默认factory或双份ScriptableObject类型绕过依赖。
- 不新增测试或人工验证task，不运行Unity batchmode。

## Current Spec Comparison

- `gameplay-simulation-session-composition`已禁止公共Host与Unity Composer识别具体Network Model，但尚未要求用程序集依赖强制执行；本change补上物理所有权。
- `gameplay-network-model-boundary`已要求新增模型不修改公共Composer，但model-neutral Definition与ServerAuthoritative Unity实现目前仍共享预定义程序集；本change将二者分离。
- `character-simulation-kernel`已要求portable source set可由Unity asmdef与普通.NET工程编译；本change不改变portable层，只收敛Unity adapter与客户端Host层。

## Impact

- 新能力：`unity-simulation-assembly-ownership`。
- 受影响能力：`gameplay-simulation-session-composition`、`gameplay-network-model-boundary`。
- 主要代码范围：`Runtime/Simulation/Unity`、`Runtime/Networking/GameplayNetwork`、`Runtime/Networking/GameplayNetwork/ServerAuthoritative`、`Runtime/Character/Pipeline/Unity`、`Main/Editor`及嵌套Editor目录。
- 主要资产风险：MonoScript程序集迁移与`SerializeReference` assembly-qualified typename；实施前必须完成精确盘点，不能使用旧类型桥接掩盖不安全资产。
