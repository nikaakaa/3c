# Change: 重构 Presentation Projection 的 Numeric Target 边界

## Why

当前 `CharacterSimulationBuildOrchestrator` 必须先生成 Float32 Program，再把该 Program 交给 `CharacterPresentationProjection.Build`。Projection 因而读取 Float32 producer、source map、operation 和 constant，保存 Float32 `ProgramHash`、`NumericProfile` 与 Target ABI，并把 Float32 `ProgramHash` 纳入 `ProjectionRevision`。

Fixed Host 与 Deterministic Rollback 又需要复用同一套动画、相机、Cue、Equipment Visual 与 Foot Analysis。现有实现没有把 Projection 编译提升到 Numeric Target 分叉之前，而是增加 `RequireSemanticProgram`，让 Fixed 绕过 Projection 中的 Float32 `ProgramHash`、NumericProfile 与 ABI。结果是同一 Projection Module 同时提供“精确 Float32 Program 绑定”和“跨 Target 语义绑定”两种 Interface，Fixed 的完整表现产品还隐藏依赖 Float32 编译结果。

这已经造成三类正式冲突：

- `character-animation-layer-runtime` 仍要求 Runtime 只读取匹配 `ProgramHash` 的 Projection，而 `add-local-fixed-gameplay-lab` 要求 Float32 与 Fixed 复用同一 Projection。
- `CharacterPresentationProjection` 同时拥有 Runtime payload、Editor 编译、Authoring 查找、Float32 operation 解码、stale identity 与加载校验，职责过大且无法独立于 Numeric Target。
- Fixed Program build、Rollback product build 与 Local Fixed Host 分别手工构造语义 identity，公共编译和运行链没有唯一的 Presentation contract Adapter。

本 change 将 Projection 定义为 target-neutral Unity 表现产物：它由 validated Semantic IR 的 producer contract、唯一 Presentation authoring 和正式 Animation Analysis artifacts 生成；Float32 与 Fixed 只负责把各自 Program 投影为同一个语义契约。Numeric Program 继续严格拥有自己的 ProgramHash、LayoutHash、NumericProfile、ABI 与 State codec，Projection 不再替它校验数值目标。

## What Changes

- 新增 target-neutral `CharacterPresentationSemanticContract`，规范保存 ProgramId、Gameplay SourceRevision、SemanticHash、按 index 排序的 producer contract 和稳定 ContractHash。
- 把 Projection 编译实现从 Runtime `CharacterPresentationProjection` 移入唯一 Editor `CharacterPresentationProjectionCompiler` Module；Runtime Projection 只保存已验证 payload、提供只读查询并校验 Presentation contract。
- Projection Compiler 正式输入改为 validated Semantic IR artifact、Presentation authoring inventory 和已解析 Animation Analysis artifact set，禁止接收 Float32/Fixed Program、ProgramHash、NumericProfile 或 Target ABI。
- Graph Camera 等非 Timeline 表现 producer 直接从 numeric-neutral Semantic IR operation、literal、reference 与 source map 解码；删除通过 Float32 `ProgramConstant.ToSingle()` 反读表现数据的路径。
- Projection identity 删除 ProgramHash、NumericProfileId 与 TargetAbiVersion；`ProjectionRevision` 改由 Projection schema、Presentation semantic contract、Presentation authoring dependency 和 Analysis artifact identity/content hash计算。
- 删除 `RequireProgram`、`RequireSemanticProgram`、`CharacterPresentationProgramIdentity.From(Float32 Program)` 和 Fixed 手工 identity 拼装，统一为一个 Presentation contract 校验 Interface。
- 新增 Float32 与 Fixed Presentation contract Adapter。Adapter 只从已加载目标 Program 提取公共语义字段和 producer contract，不编译 Projection、不读取 Authoring、不改变目标 Program 校验。
- 将 `CharacterSimulationBuildOrchestrator` 收敛为显式 Build Request 驱动的唯一协调 Module：Frontend 与 Analysis 各执行一次，Projection 与请求的 Numeric Target 独立编译，随后验证公共 Presentation contract 并在同一事务中发布请求产物。
- 删除域重载、资产导入和退出 Play Mode 触发的自动 stale 扫描与构建；Character authoring 只由显式 Editor Build 或 Product Build 请求编译，运行时与产品构建继续严格拒绝 stale 产物。
- 将 Float32/Fixed Target 编译与发布接入两个正式 Build Adapter；删除 Rollback workflow 内复制的 Fixed artifact 写入实现和任何“Fixed Build 先生成 Float32 只为得到 Projection”的隐式前置步骤。
- 更新 stale detector、Inspector、Preview、Local Float32、Local Fixed、Rollback、远端 Presentation 与 Product Build 调用链，统一使用 ContractHash、ProjectionRevision 和目标 Program 自身 identity。
- 激进迁移生成资产：重新生成 Projection 和目标 Program wrapper，删除旧 Projection 序列化字段，不提供旧数据读取、字段猜测、兼容 overload 或 fallback。

## Impact

### Specs

- `btsmtl-compiled-simulation-program`
- `character-animation-layer-runtime`
- `character-pipeline-runtime`
- `deterministic-rollback-two-client-demo`
- `openspec/project.md` 中 Authoring 编译链、Presentation identity 与 Editor 代码组织说明

### Code

- `Assets/GameScripts/Main/Editor/CharacterSimulation/CharacterSimulationBuildOrchestrator.cs`
- `Assets/GameScripts/Main/Editor/CharacterSimulation/CharacterSimulationProgramBuildService.cs`
- `Assets/GameScripts/Main/Editor/CharacterSimulation/FixedCharacterSimulationProgramBuildService.cs`
- `Assets/GameScripts/Main/Editor/CharacterSimulation/DeterministicRollback/DeterministicRollbackNetworkTestBuildAndRun.cs`
- 新的 Editor-only Projection Compiler、Semantic reader、Build Request/Result 和 target Build Adapter
- `Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/Contracts/CharacterPresentationProjection.cs`
- `Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/Contracts/CharacterPresentationProjection.Equipment.cs`
- `Assets/GameScripts/Main/Runtime/Character/Pipeline/Unity/CharacterPresentationProjectionAsset.cs`
- `Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/Contracts/AnimationPresentationBindingIndex.cs`
- `Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation` 下所有 Projection 加载调用方
- Float32 Character Host、Fixed Character Host、Deterministic Rollback Host 与远端 Presentation Adapter
- Corin generated Float32 Program、Fixed Program 与 Presentation Projection assets

### Active Change 关系

- 本 change 依赖 `refactor-timeline-animation-authoring-boundary` 安装后的正式 Animation Analysis artifact resolver；不会复制采样、artifact codec、stale 判定或 Foot Feature payload。
- 本 change 以 `add-local-fixed-gameplay-lab` 已安装的 Fixed Program/Host 为第二个真实 Adapter，并替换其现有手工语义 identity；不会建立新的 Fixed Host 或第二 Projection。
- `refactor-motion-warp-trajectory-solving` 与当前资产迁移先恢复公共编译基线。本 change 实施时必须基于届时 current specs 和代码重新核对字段，不能回退或覆盖并行改动。
- 后续 `add-character-presentation-pose-graph` 在本change已经建立的target-neutral边界上执行新的破坏性schema升级：把producer contract的`LayerId`迁移为`AnimationChannelId`，并把channel-to-PoseSlot binding、per-slot Blend Stack、dense Rig与compiled Pose Program加入同一Projection。该后续change不得恢复Numeric Target依赖，也不得把本change已删除的ProgramHash/NumericProfile/ABI重新写入Projection。

## Breaking Changes

- 旧 Projection asset 中的 ProgramHash、NumericProfileId 与 TargetAbiVersion 被删除，旧资产直接失效并重新生成。
- `RequireProgram`、`RequireSemanticProgram` 和现有两个 `CharacterPresentationProjectionAsset.Load` overload 被删除。
- `CharacterPresentationProjection.Build` 不再是 Runtime 公共入口，旧调用方必须迁移到 Editor Projection Compiler。
- ProjectionRevision 算法与 schema identity 提升；旧 revision 不兼容。
- Fixed 与 Rollback 不再允许手工拼接 producer identity 数组绕过正式 Adapter。
