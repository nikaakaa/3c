# Design: Character Semantic Frontend 与正式 IR Artifact

## Context

当前代码已经拥有正确的三个数据概念：

```text
Unity Authoring
CharacterGameplaySemanticIr
Float32 CharacterSimulationProgram
```

但实现调用仍是：

```text
CharacterSimulationProgramCompiler.Compile(definition)
  -> CompileOnce(definition)
       -> CharacterSimulationGraphCompiler
       -> CharacterGameplaySemanticIr object
  -> Lower(semanticIr object)
       -> Float32 Program
  -> Build Projection
```

`CharacterGameplaySemanticIrCodec.WriteArtifact/ReadArtifact` 只在该方法内做两次编译比较和 round-trip，自身没有正式 build identity、cache、Inspector 或普通 .NET reader 入口。IR 因而隔离了类型依赖，却没有隔离编译阶段、工具调用和下游 Target。

本设计把 IR 固定为正式的编译产物边界，但不把它提升为 authoring 真相或 Runtime 数据源。

## Goals

- Frontend 可以独立运行，只负责把一个 Definition 的完整可达 authoring 编译为 canonical Semantic IR artifact。
- Float32 与未来 Fixed Target 只能消费同一 artifact contract，不读取 Unity authoring。
- 作者可以查看 Corin 的真实 IR，而不是只能相信 ProgramHash 或阅读编译器源码。
- 普通 .NET 工具可以读取同一 `.csir` bytes，不复制 schema。
- Program/Projection Runtime 行为保持不变。
- 为后续拆分 `SimulationOperationMachine` 建立可比较的 operation/control-flow/state/source-map 基准。

## Architecture

```text
CharacterPipelineDefinition
  RootTree + Config/Profile references
                |
                v
Character Semantic Frontend
  1. Authoring Discovery
  2. Semantic Emission
  3. Canonical Encode
  4. Decode + Hash Verification
                |
                v
Validated Semantic IR Artifact
  canonical bytes
  manifest + SemanticHash
  generated Library cache
         |                       |
         v                       v
Float32 Target Compiler     IR Inspector / DotNet Reader
         |
         v
Float32 CharacterSimulationProgram
         |
         +---- Presentation Projection Compiler
                consumes same discovery/source identity context
         |
         v
Atomic Program + Projection publish

Runtime Host
  Program + Projection only
  never loads .csir
```

## Frontend Phases

### Authoring Discovery

Discovery 只读取 `CharacterPipelineDefinition` 及其正式依赖，建立 editor-only `CharacterAuthoringCompilationModel`。该 model 包含：

- Definition/Program identity 与 SourceRevision 输入。
- 稳定排序的 Graph、Node、Edge、PropertyEdge 和 nested graph route。
- StateMachine/State inline/shared ownership 与 scope identity。
- Blackboard declaration、owner、scope、lifetime 与默认值来源。
- Timeline、Track、Clip、TreeClip ownership 与稳定 authoring identity。
- Action、Behavior、GameplayEffect、motion curve 与 Presentation Profile 引用清单。
- 精确 source location，不包含 target scalar 或 runtime state。

Discovery 必须完成可达性、循环引用、重复 identity、缺失 owner、缺失 Emitter 和 Unity asset identity 校验。其输出顺序由稳定 identity 决定，不依赖 `GetInstanceID`、显示名、Inspector 顺序或无序集合。

`CharacterAuthoringCompilationModel` 只在 Editor 进程内存在，不写入 `.csir`，因为它仍包含 Unity authoring object 引用，服务于后续 Semantic Emission 与 Projection 构建。

### Semantic Emission

Emission 只能消费已校验 Discovery model 与唯一 Emitter registries，生成：

```text
SemanticOperation
SemanticLiteral
ControlFlow
Reference
StateDeclaration
Scope
WorldRequest
OutputChannel
CatalogEntry
Producer
SourceMap
Capability manifest
```

Emission 不重新遍历 AssetDatabase，不重新发现 nested graph，不读取 Numeric Target、Driver、Solver 或 Network Model。缺少 Emitter 或发现 model 与发射输入不一致时终止，不跳过 authoring element。

当前 `CharacterSimulationGraphCompiler` 在 Discovery、Emission 完成迁移后删除，不保留 facade、兼容 wrapper 或第二条 CompileGraph 路径。

## Semantic IR Artifact Contract

### Artifact Payload

正式 artifact 继续使用 Core 中唯一 `CharacterGameplaySemanticIrCodec`。Artifact header 至少包含：

```text
ArtifactMagic / ArtifactVersion / PayloadVersion
ProgramId
CompilerVersion
OperationSetVersion
TickRate
SourceRevision
SemanticHash
Required capabilities
Canonical payload bytes
```

Codec 增加无需 Unity 的 header 读取与 validated artifact 类型。`ValidatedSemanticIrArtifact` 同时持有只读 header、canonical bytes 和由这些 bytes 解码得到的 IR；其创建只能经过 codec 完整校验，不能用任意内存 IR 冒充已校验 artifact。

SemanticHash 继续覆盖 canonical IR manifest 与全部 tables。Target Program manifest 必须复制该 SemanticHash、SourceRevision 和 OperationSetVersion。

### Generated Cache

Unity Editor 将当前 artifact 保存到：

```text
Library/CharacterSimulation/SemanticIr/<definition-guid>.csir
```

选择固定 Definition GUID 路径，而不是按 SourceRevision 生成无限历史文件。每次成功 Frontend build 使用临时文件写入、flush、重新读取校验，再原子替换当前 `.csir`。

该文件：

- 不进入 `Assets` 和版本控制。
- 不创建 `.meta`、ScriptableObject 或 Definition 引用。
- 清理 `Library` 后可以由同一 Frontend 重建。
- 缺失时不使已经匹配 source revision 的 Runtime Program 失效，因为 Runtime 不依赖 cache。
- Target build 开始时必须先取得当前 build transaction 生成或精确验证的 artifact，不能因为磁盘中存在旧 `.csir` 就跳过 SourceRevision 检查。

### Authority

唯一业务真相仍是 Definition 可达 authoring。`.csir` 是 generated build artifact，Program 是 Numeric Target artifact，Projection 是 Unity Presentation artifact。三者都不可手工编辑。

不保存 source-controlled IR Asset，因为那会让作者面对“Graph 和 IR 哪个是准的”并造成大文件 churn；不只保留内存 IR，因为那不能支撑独立 Target、Inspector 和普通 .NET 读取。

## Compiler Entry Points

正式调用分为：

```text
CharacterSemanticFrontendCompiler.Compile(definition)
  -> CharacterSemanticFrontendResult
       ValidatedSemanticIrArtifact
       CharacterAuthoringCompilationModel (editor-only projection context)
       Frontend report

Float32CharacterSimulationTargetCompiler.Compile(validatedArtifact)
  -> Float32 Program + target report

CharacterSimulationBuildOrchestrator.Build(definition)
  -> Frontend
  -> persist/reload artifact
  -> Float32 Target
  -> Projection
  -> verify complete transaction
  -> publish Program/Projection assets
```

Float32 lowering 的底层实现可以继续操作解码后的 `CharacterGameplaySemanticIr`，但该方法必须为 Target 模块内部实现；Editor、Agent、BuildService 和未来 Target 不能直接调用 raw lowerer。

旧 `CharacterSimulationProgramCompiler.CompileOnce`、`Lower(CharacterGameplaySemanticIr)` 与 `CharacterSimulationCompileResult.SemanticIr` 删除。新的 build result 只暴露 artifact descriptor/identity、Program、Projection 和分阶段 report。

## Build Transaction

Build transaction 顺序固定为：

1. 计算 Definition GUID 与完整 SourceRevision。
2. 执行 Discovery 和 Semantic Emission。
3. 对 IR canonical encode 两次并比较 bytes。
4. 由 bytes decode 并校验 SemanticHash/header/payload。
5. 原子写入当前 `.csir` cache，再从磁盘重新读取校验。
6. Float32 Target 只从重新验证的 artifact lowering。
7. Projection 使用同一 Discovery model、producer/source identity 与目标 Program identity 构建。
8. 校验 artifact、Program、Projection 的 ProgramId/SourceRevision/SemanticHash/ProgramHash 关系。
9. 全部成功后更新 Program Asset、Projection Asset 与 Definition generated references。

任一步失败时不发布部分 generated asset。已有 Program/Projection 保持原 bytes，但会因 source revision 过期继续显示 Invalid；系统不把旧产物当作本次 build 成功，也不在 Runtime fallback。

Definition Diagnostics 与 Agent Validator 使用同一个 Frontend/Target pipeline 的 dry-run 结果，但不写 Program/Projection。它们不得复制 discovery、emission 或 target lowering。

## Presentation Projection

本 change 不重写动画运行时，也不拆 `CharacterPresentationProjection.cs` 的全部类型。Projection 构建继续是 Unity Editor 侧步骤，但其 gameplay identity 输入必须来自同一 Discovery model 和 validated artifact：

- 不再次递归 Graph 来推导 gameplay flow。
- 不创建第二份 producer/source map。
- Projection 中的 producer 必须精确匹配 artifact producer identity。
- Projection 继续记录目标 ProgramHash，因此每个 Numeric Target 可以生成自己的匹配 Projection；Unity 客户端当前只生成 Float32 组合。

将 Projection builder 彻底从 Runtime contracts 迁出可在后续独立清理中完成，本 change 只移除为正式 Frontend/Target 链路所必需的组合调用。

## Semantic IR Inspector

IR 数据量远超 Definition Inspector 适合承载的规模，因此使用显式打开的只读 EditorWindow，不把 485 条 operation 平铺回 Definition。

入口：

- Definition Inspector 的 Generated Artifacts/Diagnostics 折叠区提供 `Inspect Semantic IR`。
- Asset 菜单提供 `Assets/3C/Inspect Character Semantic IR`。
- 当前 cache 缺失或 stale 时只显示身份状态与明确 `Compile Semantic IR` 命令；窗口 Repaint 不自动编译。

窗口使用虚拟化列表和详情区，提供：

```text
Manifest
Operations
Literals
Control Flow
State Slots
Scopes
World Requests
Output Channels
Catalog
Producers
Source Map
```

搜索按 operation code、identity、source type、GraphId、NodeId、TimelineId、TrackId、ClipId 和 display path 精确过滤。选择记录后通过 SourceMap 导航现有 Graph/Timeline/asset UI；无法精确解析时显示 unresolved，不按名称、数组 index 或最近打开窗口猜测。

Inspector 不编辑 IR、不写 authoring、不修改 generated bytes、不执行 Runtime，也不形成新的配置入口。

## Ordinary DotNet Reader

受版本控制的 `ThirdPersonSimulation.Reader` 使用明确命令：

```text
ThirdPersonSimulation.Reader semantic-ir <path> [--section <name>] [--format text|json]
ThirdPersonSimulation.Reader program <path> [--section <name>] [--format text|json]
```

不使用 magic 自动猜测命令意图。`semantic-ir` 通过 Core codec 读取 header 和完整 payload，可输出 manifest、table counts、operations、control flow、state slots、scopes、producers 与 source map。JSON 输出只是一种只读投影，不是可重新导入 schema，也不进入 build。

Reader 不引用 UnityEngine、Editor assembly 或复制 IR DTO。CompilerVersion、OperationSetVersion、SourceRevision、SemanticHash 或 payload 校验失败时返回非零退出码。

## Failure Policy

- Definition/Config 缺失或 SourceRevision 计算失败：Frontend 失败，不读取旧 cache。
- Discovery 遇到循环引用、重复 identity、缺失 owner 或缺失 Emitter：Frontend 失败，不产生 artifact。
- 两次 canonical encode 不同：Frontend 失败并报告 nondeterministic source stage。
- cache header、payload 或 SemanticHash 不匹配：artifact 读取失败；显式 build 重新执行正式 Frontend，不尝试兼容旧版本。
- Target 接收到未校验 artifact、错误 OperationSetVersion 或 unsupported operation：Target build 失败。
- Projection producer 与 artifact producer 不一致：完整 build transaction 失败，不更新 Program/Projection。
- Inspector source identity 无法解析：只显示 unresolved，不近似导航。
- Runtime 缺少 `.csir`：无影响；Runtime 只校验 Program/Projection。

## Migration And Deletion

本 change 不迁移手工数据。第一次正式 build 为每个 Definition 生成 Library cache，并重建匹配 Program/Projection。

完成后删除：

- `CharacterSimulationGraphCompiler` 单体类。
- `CharacterSimulationProgramCompiler.CompileOnce` 与 raw `Lower` 组合路径。
- `CharacterSimulationCompileResult.SemanticIr`。
- Agent/Definition diagnostics 对旧组合 result 的读取。
- 任何临时 JSON dump、第二 artifact DTO、兼容 reader 或旧 artifact version parser。

已有 Program/Projection asset 通过正式 build 原地更新，不保留旧 compiler version reader。

## Decisions And Tradeoffs

### 保存到 Library，而不是 Assets

- 收益：不会形成第三份手工资产或 Definition 引用，不进入 Player和版本库；清理后可重建。
- 成本：Git review 不能直接看到 `.csir`，CI 或作者需要显式导出/Reader 输出才能比较。
- 选择原因：IR 是生成产物，不应与 Graph 并列成为 authoring 真相。

### Target 消费 validated artifact，而不是继续接收内存 IR

- 收益：真正验证 canonical codec、header 与 hash，Target 和普通 .NET 看到完全相同的输入；不能不经意绕回 Graph。
- 成本：Editor build 多一次 encode/decode 和 Library I/O。
- 选择原因：这些成本只发生在 Editor build，换来可独立审查和未来多 Target 的真实边界。

### 保留集中 opcode schema，而不是每个 Node 生成独立 Target handler

- 收益：同一 operation-set version 继续约束 Float32/Fixed，不制造几十个浅 handler 或模型专用节点。
- 成本：新增 operation 仍需升级统一 schema 与每个 Target backend。
- 选择原因：本 change 解决编译阶段和 artifact 边界，不把后续 runtime 模块化误做成“一节点一类”。

### Inspector 是只读窗口，而不是 Definition 内联表格

- 收益：足够空间审查大表，Definition 继续保持装配清单；窗口不成为写入口。
- 成本：作者需要显式打开另一个诊断窗口。
- 选择原因：这是编译产物检查工具，不是动画或角色配置编辑器，与 Graph/Timeline 双窗口工作流不竞争写权限。

### 本 change 不拆 OperationMachine

- 收益：先固定业务输入和可比较基准，后续执行器重构可以明确区分结构变化与业务变化。
- 成本：4310 行 partial 解释器在本 change 完成时仍存在。
- 选择原因：同时重写 Frontend artifact 和 Runtime evaluator 会让 SemanticHash、Program 与行为变化无法归因，也扩大回归面。

## Downstream Constraints

- `add-deterministic-rollback-kcc-model` 必须消费 `.csir` 对应的 validated artifact，生成独立 Fixed Program/State/Kernel/Snapshot ABI；不得复用 Float32 Program。
- `refactor-server-authoritative-hybrid-runtime` 与 DotRecast authoritative backend 可继续消费 Float32 Program，不需要加载 `.csir`；服务端 build/部署可以由普通 .NET 工具验证 artifact identity。
- 后续 `SimulationOperationMachine` 重构必须以相同 Semantic IR tables、source mapping 和 operation-set version 为输入，不新增第二套 IR 或 runtime Graph adapter。
