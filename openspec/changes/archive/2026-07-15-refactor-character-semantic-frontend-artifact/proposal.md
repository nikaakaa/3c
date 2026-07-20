# Change: 将 Character Semantic Frontend 与 IR Artifact 固化为正式编译边界

## Why

`refactor-character-simulation-core` 已建立 numeric-neutral `CharacterGameplaySemanticIr`、canonical codec 和 Float32 lowering，但当前实现仍由 `CharacterSimulationProgramCompiler.Compile()` 在同一个调用中完成 Graph 发现、语义发射、内存 IR 创建、Float32 lowering、Projection 构建与 artifact 校验。IR 只在该方法内短暂存在，Float32 Target 直接接收内存对象，Unity 作者和普通 .NET 工具都无法查看 Corin 的真实 operations、control flow、state slots、scopes、producers 与 source map。

当前 `CharacterSimulationGraphCompiler` 还以一个 1076 行类同时承担可达对象发现、identity/ownership 校验、Blackboard 布局、Graph/Edge 发射、Timeline/TreeClip 发射、catalog 绑定和 source map 生成。这使 Frontend 只有代码层面的命名，没有可独立调用、缓存、读取和审查的正式输出边界。

现有 change 的 `tasks.md` 已把“普通 .NET 可读取 Semantic IR diagnostic artifact”标为完成，但仓库中的 `ThirdPersonSimulation.Reader` 实际只读取 Float32 Program，BuildService 也没有保存 IR artifact。这一完成声明与实现不一致，必须由正式 change 补齐，不能继续把临时内存对象描述成完整多 Target 编译基础。

在拆分 4310 行 `SimulationOperationMachine` 或实现 Fixed Target 之前，需要先固定可阅读的业务输入：同一份 canonical Semantic IR artifact 必须成为 Float32 与未来 Fixed Target 的唯一正式输入，使后续执行器重构能够核对 operation、control flow、state layout、producer 和 source mapping 是否发生业务变化。

## Dependencies

- `refactor-character-simulation-core` MUST 已完成；本 change 复用其 Semantic IR schema、canonical codec、Float32 Program 和 Program/Projection 运行时合同。
- `refactor-character-pipeline-definition-config-boundary` MUST 已完成；Frontend 的唯一 root 继续是 `CharacterPipelineDefinition` 及其正式 Config/Profile 引用。
- 本 change MUST 在 `add-deterministic-rollback-kcc-model` 开始 Fixed Target 实施前完成；Fixed Target 不得重新遍历 BTSMTL authoring。

## What Changes

- 将当前 `CharacterSimulationGraphCompiler` 拆为明确的 Authoring Discovery 与 Semantic Emission 阶段：Discovery 只建立 editor-only、稳定排序、不可变的可达 authoring model；Emission 只从该 model 生成 Semantic IR tables。
- 新增正式 `Character Semantic Frontend` 入口，独立产出经过 canonical encode、decode 与 hash 校验的 Semantic IR artifact payload，不同时生成 Float32 Program。
- 将 Semantic IR artifact 作为 generated cache 保存到 `Library/CharacterSimulation/SemanticIr/<definition-guid>.csir`；它不是 Unity Asset、不写入 Definition、不进入 Player、不成为第二份 authoring 真相。
- 新增 artifact header/descriptor 与 atomic store，严格记录 ProgramId、CompilerVersion、OperationSetVersion、SourceRevision、SemanticHash 和 payload identity；缺失、损坏或身份不匹配时明确失败或通过正式 Frontend 重新生成，不近似读取旧 artifact。
- 将 Float32 Target 的正式入口改为只消费已校验的 Semantic IR artifact；删除组合编译器中直接把新建内存 IR 交给 lowerer 的路径，Target 不得读取 Definition、Graph、Node、Timeline 或 Unity object。
- 将 Program 与 Projection 构建组织为单一 build transaction：Frontend artifact、Float32 Program 与 Projection 必须来自同一 SourceRevision/SemanticHash，全部成功后才更新 generated assets。
- 保留现有 Program/Projection 运行时链路；Host 仍只加载 target Program 与 Projection，不加载、解释或依赖 `.csir`。
- 增加显式打开的只读 Semantic IR Inspector，显示 Manifest、Operations、Literals、ControlFlow、StateSlots、Scopes、WorldRequests、OutputChannels、Catalog、Producers 和 SourceMap，并通过精确 source identity 导航到 Graph/Node/Edge/Timeline/Track/Clip/Declaration。
- 扩展受版本控制的普通 .NET Reader，使用显式 `semantic-ir` 与 `program` 子命令读取两种 canonical artifact，不做 magic 猜测或 Unity 依赖。
- 删除旧 `CharacterSimulationCompileResult.SemanticIr` 直通对象、旧组合 `CompileOnce -> Lower(IR object)` 入口、旧单体 GraphCompiler 和任何并行 IR dump/schema。
- 更新 `project.md` 与下游 active change 文档：Fixed Target 从同一 Semantic IR artifact 生成独立 Fixed Program，不复用 Float32 `CharacterSimulationProgram`。

## Capabilities

### New Capabilities

- `btsmtl-semantic-ir-inspection`：定义 Unity Editor 与普通 .NET 对正式 Semantic IR artifact 的只读审查、精确 source 导航和显式格式选择。

### Modified Capabilities

- `btsmtl-gameplay-semantic-ir`：将临时内存 IR 提升为独立 Frontend 产出的 canonical generated artifact，并固定其身份、存储和非运行时边界。
- `btsmtl-compiled-simulation-program`：要求 Numeric Target 只从已校验 Semantic IR artifact lowering，并要求 Program/Projection 在同一 build transaction 中发布。

## Non-Goals

- 不拆分或修改 `SimulationOperationMachine`、Runnable/StateMachine/Timeline/Blackboard/Action/GameplayEffect 的运行时执行语义。
- 不实现 FixedQ32.32 Target、Deterministic KCC、rollback、DotRecast 或任何 Network Model。
- 不改变 Corin Graph、状态机、Timeline、TreeClip、黑板、Action、GameplayEffect、motion 或动画配置。
- 不把 Semantic IR 变成 ScriptableObject、Definition 配置字段、source-controlled authoring asset 或 Runtime fallback。
- 不恢复 runtime Graph/Timeline clone，不增加第二个 Emitter registry、第二套 operation schema 或 JSON 业务真相。
- 不在 Definition Inspector Repaint 时自动运行完整 Frontend；编译、刷新和打开 Inspector 必须由正式 build 调度或作者显式命令触发。
- 不新增测试；使用 canonical round-trip、普通 .NET 构建/读取、Unity 程序集静态编译、结构搜索和 OpenSpec strict validation 收口。

## Current Spec Comparison

- `btsmtl-gameplay-semantic-ir` 已要求 Authoring 先编译为 numeric-neutral IR，并禁止 Runtime 解释 IR。本 change 保留这些要求，但把“只存在于编译和诊断边界”具体化为正式 canonical artifact、独立 Frontend 入口和 generated cache；不会把 IR 引入 Runtime。
- `btsmtl-compiled-simulation-program` 已写明 Numeric Target 消费 Semantic IR，但当前实现仍由组合编译器直接传递内存对象。本 change 修改该 requirement，要求 Target 的正式输入是经过 artifact codec 校验的 payload，而不是 Definition 或 Frontend 私有对象。
- `character-simulation-kernel` 的 Program/Session/Kernel/Driver/Solver 运行时合同不变，本 change 不修改该 spec。
- `character-pipeline-definition-authoring` 要求 Definition Inspector 保持紧凑且不在 Repaint 重编译。本 change 只在 Generated Artifacts/Diagnostics 区增加显式 IR 状态与打开命令，重型 IR 表格使用独立只读 Inspector，不恢复复合配置 UI。
- `agent-character-controller-synthesis` 继续只消费正式 Frontend/Program compile report。Agent 不拥有、修改或生成第二份 Semantic IR；其 validator 改用新的 Frontend/build result，不再读取 `CharacterSimulationCompileResult.SemanticIr`。
- `refactor-character-simulation-core/tasks.md` 的 3.11 完成声明与当前 Reader/BuildService 不一致；本 change 以实际 artifact store、Inspector 和普通 .NET Reader 闭合该缺口，不修改历史 checklist 来掩盖事实。
- `add-deterministic-rollback-kcc-model` 当前仍有“使用同一 CharacterSimulationProgram”的旧表述，与现行 Numeric Target spec 冲突。该 active change 必须改为“消费同一 Semantic IR artifact，生成独立 Fixed Program/State/Kernel ABI”后才能 apply。

## Impact

- Editor 编译：Character Simulation Frontend、Graph/Timeline discovery、semantic emission、compile report、BuildService、Definition diagnostics、Agent validator。
- Portable Core：Semantic IR artifact header/descriptor/codec 读取入口，不增加 Unity 引用。
- Float32 Target：正式 target compiler 输入与 lowering 可见性。
- 工具：只读 Semantic IR Inspector、普通 .NET Reader 子命令。
- 生成数据：新增 `Library` 下可删除、可重建的 `.csir` cache；现有 Program/Projection Unity assets 继续是 Runtime 唯一产物。
- 删除：旧单体 GraphCompiler、旧内存 IR 直通 compile result、旧组合 target 调用和任何一次性迁移代码。
