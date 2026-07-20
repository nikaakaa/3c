# Change: 重构 Float32 Session Runtime Launcher 边界

## Why

现行 `gameplay-simulation-session-composition` 已要求公共 Session Host、Unity adapter 与 Network Model 不复制 Float32 Runtime 构造，也不得让公共基座硬编码具体模型。但当前 `UnityFloat32SimulationSessionComposer` 直接引用 `ServerAuthoritativeAuthorityPreparedSource`，`Float32SimulationPipelineFactorySet` 也直接识别 `ServerAuthoritativeAuthorityPipelineDefinition`。这使公共 Unity Float32 组合层知道一个具体 Network Model，并要求新增其它 Float32 模型时继续修改公共 Composer。

`refactor-server-authoritative-host-portability` 已把 Authority Pipeline catalog、Source runtime 与 Host launch request 迁到 portable ServerAuthoritative 模块，但 Unity 公共 Composer 为调用该 request 新增了具体类型分支。该分支不是第二套 Gameplay Runtime，却破坏了模型实现对公共基座关闭修改的目标。

本 change 在 `add-dotrecast-authoritative-server-backend` 接入 Fantasy Server 内 DotRecast Authority Scene 的 Host launch task 前完成：把公共 Unity 组合过程拆成“构造通用 Float32 Composition Request”和“调用显式 Runtime Launcher”两步。公共基座只认识 portable Runtime Package 与 Launcher 接口；Standard 与 ServerAuthoritative 由各自 Launcher 调用同一个 portable Float32 Composer。

## Dependencies

- `refactor-gameplay-session-composition-boundary` MUST 已归档。
- `refactor-server-authoritative-host-portability` MUST 已完成且实现冻结；本 change 保留其 portable Authority Source、Pipeline catalog 与 Host launch语义。
- `add-dotrecast-authoritative-server-backend` MUST 在 task 3.12 前暂停；本 change 完成后该 change MUST 直接消费新的 ServerAuthoritative Runtime Launcher合同。
- `refactor-agent-authoring-compiler-modules` MAY 并行实施，因为它不编辑 Simulation、Networking 或 Composition 代码。

## What Changes

- 新增 portable `Float32SimulationPipelineRuntimePackage`，原子保存 Pipeline descriptor、portable Pass factory catalog、Float32 Pass runtime factory catalog、Product runtime catalog 与稳定 package identity。
- 建立 Float32 Pipeline runtime package provider合同；被 Float32 Composition选择的 Pipeline Definition必须显式提供完整 package，公共 builder不得识别具体 Pipeline类型。
- 新增 portable `IFloat32SimulationSessionRuntimeLauncher`；Launcher只能校验已经显式选择的 Composition输入并调用唯一 `Float32SimulationSessionComposer`，不得选择或替换 Program、Backend、Pipeline、Source或Solver。
- 让 `IFloat32SimulationSessionPreparedSource` 显式提供 Runtime Launcher。Local、Preview和ServerAuthoritative Prediction使用正式 Standard Launcher；ServerAuthoritative Authority使用模型模块提供的 Authority Launcher。
- 将 Unity Float32 Composer收敛为通用 Composition Request Builder与Launcher调用编排；移除对 ServerAuthoritative namespace、PreparedSource和Pipeline Definition具体类型的依赖。
- 让 ServerAuthoritative Authority Launcher持有 Source policy、locked roster与握手或manifest锁定的完整Authority PipelineIdentity，通过既有 Host launch request校验 neutral runtime package，并让同一个portable Float32 Composer在创建RuntimeHandle前精确核对编译后的PipelineHash。
- 删除 `Float32SimulationPipelineFactorySet.AuthorityCatalog`、Authority Pipeline具体类型判断、Authority PreparedSource具体类型转换和对应分支。
- 保持五项 `SimulationSessionCompositionDefinition` 不变，不增加第六个 Launcher配置资产；Launcher由已经显式选择并完成 preparation的 Source实现提供。
- 保持 ProgramHash、PipelineHash、Source policy hash、Composition identity、checkpoint/packet bytes与 Unity四进程外部行为不变。

## Non-Goals

- 不新增或修改 DotRecast Solver、Authority Scene、manifest、Build profile或协议。
- 不新增 Fixed Numeric Target、Deterministic Rollback、KCC或第二个 Execution Backend。
- 不重构 Animation、Presentation、Fantasy Endpoint或 Prediction history。
- 不在本 change 建立 Unity asmdef拆分；程序集物理边界另行规划。
- 不增加反射、运行时类型扫描、字符串 Launcher registry、默认 Launcher、fallback或兼容旧 Composer入口。
- 不新增测试或人工验证 task，不运行 Unity batchmode。

## Current Spec Comparison

- `gameplay-simulation-session-composition` 已要求每个 Program Runtime/Backend组合只有一个 target-specific Composer，且 Network Model不得复制 Runtime构造。本 change保留唯一 portable Float32 Composer，并补充“公共 Composer不得识别具体模型，模型差异只能通过显式 Launcher接口进入”。
- `gameplay-network-model-boundary` 已要求 Common Host不硬编码已知 Model类型。本 change将相同限制扩展到 Unity target adapter、Pipeline runtime package lowering和 Composer。
- `server-authoritative-host-portability` 要求 Authority Host通过唯一 Host launch request进入 portable Composer。本 change保留该校验入口，只把调用责任从公共 Composer迁给 ServerAuthoritative Authority Launcher。
- current `server-authoritative-hybrid-sync-model` 仍把权威 Solver写成 Unity CharacterController；该过时口径由正在实施的 `add-dotrecast-authoritative-server-backend` 修改，本 change不抢占 Solver/Host Profile spec所有权。

## Impact

- 新能力：`float32-session-runtime-launcher`。
- 修改能力：`gameplay-simulation-session-composition`、`gameplay-network-model-boundary`。
- Portable Float32：Runtime Package、Standard Launcher、唯一 Composer request输入。
- Unity Simulation：Pipeline package provider、通用 request builder、Prepared Source Launcher合同。
- ServerAuthoritative：Authority Launcher与Host launch request的neutral package输入。
- 删除：公共 Composer中的模型分支、公共 FactorySet中的Authority分支和具体类型依赖。
