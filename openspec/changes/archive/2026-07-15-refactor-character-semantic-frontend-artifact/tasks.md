## 1. 锁定现状与删除清单

- [x] 1.1 盘点 `CharacterSimulationProgramCompiler.Compile`、`CharacterSimulationCompileResult`、`CharacterSimulationGraphCompiler` 和 raw Float32 lowerer 的全部调用点。
- [x] 1.2 盘点 Definition Inspector、BuildService、Agent Validator、Projection builder 与普通 .NET Reader 当前使用的编译入口。
- [x] 1.3 记录当前 Corin ProgramId、CompilerVersion、SourceRevision、SemanticHash、ProgramHash、LayoutHash、operation/state/source-map 数量和 source-map content hash。
- [x] 1.4 记录当前 Frontend 输出表的稳定排序与 Emitter coverage，形成实施期间的业务结构核对清单。
- [x] 1.5 建立旧路径删除清单：单体 GraphCompiler、`CompileOnce`、raw `Lower` 外部调用、`CompileResult.SemanticIr` 与 Program-only Reader 入口。
- [x] 1.6 确认仓库不存在正式 `.csir` store、Semantic IR Inspector、IR ScriptableObject 或第二套 IR schema。

## 2. 建立 Portable Semantic IR Artifact 合同

- [x] 2.1 为 Semantic IR artifact 暴露只读 header 类型，覆盖 magic/version、ProgramId、CompilerVersion、OperationSetVersion、TickRate、SourceRevision、SemanticHash 与 capability identity。
- [x] 2.2 将 ProgramId 与 TickRate 写入正式 artifact header，并保持 payload manifest 与 header 严格一致。
- [x] 2.3 建立 `ValidatedSemanticIrArtifact`，只读保存 header、canonical bytes 与已解码 IR。
- [x] 2.4 限制 `ValidatedSemanticIrArtifact` 的创建路径，只允许 codec 完整校验后构造。
- [x] 2.5 为 codec 增加不依赖外部 expectation 的 header-only 读取入口，供 Inspector 和普通 .NET Reader 使用。
- [x] 2.6 保留带 expectation 的完整读取入口，并校验 ProgramId、CompilerVersion、OperationSetVersion、TickRate、SourceRevision 与 SemanticHash。
- [x] 2.7 校验 artifact header 与 payload manifest 的全部重复身份字段，不只比较 SemanticHash。
- [x] 2.8 对 artifact trailing bytes、非法 count、非法 enum、越界引用与不完整 payload 保持明确失败。
- [x] 2.9 让 canonical encode 直接产生 validated artifact 所需 bytes，不引入 Unity serializer、JSON 或 BinaryFormatter。
- [x] 2.10 让 artifact decode 恢复现有唯一 `CharacterGameplaySemanticIr` DTO，不创建 Inspection 专用 schema。
- [x] 2.11 更新 artifact/payload version 并删除旧版本兼容 parser。
- [x] 2.12 确认 `ThirdPersonSimulation.Core` 继续 `noEngineReferences`，artifact 合同不引用 UnityEngine 或 UnityEditor。

## 3. 拆分 Authoring Discovery

- [x] 3.1 建立 editor-only `CharacterAuthoringCompilationModel` 作为一次 Frontend build 的不可变 discovered model。
- [x] 3.2 在 discovered model 中保存 Definition GUID、ProgramId、SourceRevision 与正式 Config/Profile root identity。
- [x] 3.3 建立稳定排序的 Graph route 与 nested graph occurrence 记录。
- [x] 3.4 建立 Graph、Node、Edge、PropertyEdge 的精确 source location 记录。
- [x] 3.5 建立 StateMachine/State inline/shared ownership、scope identity 与 owner operation 来源记录。
- [x] 3.6 建立 Blackboard declaration、owner、scope、lifetime、authority、projection 与默认值来源记录。
- [x] 3.7 建立 Timeline、Track、Clip、TreeClip ownership 与 authoring identity 记录。
- [x] 3.8 建立 Action、Behavior、GameplayEffect、motion curve 与 Presentation Profile 引用记录。
- [x] 3.9 将 nested graph cycle、缺失 required reference、inline/shared owner 错误集中到 Discovery report。
- [x] 3.10 将重复 Graph/Node/Edge/Declaration/Timeline/Track/Clip identity 集中到 Discovery report。
- [x] 3.11 将缺失 Node/Track/Clip Emitter coverage 检查集中到 Discovery，不在 Emission 静默跳过。
- [x] 3.12 将 Unity asset GUID、serialized owner 与 dependency identity 校验集中到 Discovery。
- [x] 3.13 保证 discovered model 的集合使用 stable identity 排序，不暴露可变 Unity authoring list。
- [x] 3.14 保证 discovered model 只存在于 Editor build，不写入 Semantic IR artifact 或 Runtime Program。

## 4. 拆分 Semantic Emission

- [x] 4.1 建立 `CharacterSemanticEmitter`，只接收已校验 discovered model、唯一 Emitter registries 与 Semantic builder。
- [x] 4.2 将 Graph root、Sequence、Selector、Parallel、Loop 与 value operation 发射迁入 Semantic Emission。
- [x] 4.3 将 StateMachine、State、Enter/AnyState/Exit 与 State body control flow 发射迁入 Semantic Emission。
- [x] 4.4 将 Edge、ConditionRuleGraph、priority、order 与 abort policy 发射迁入 Semantic Emission。
- [x] 4.5 将 Timeline、Track、Animation/Motion/TreeClip/Cue/Camera operation 发射迁入 Semantic Emission。
- [x] 4.6 将 TreeClip OnEnable/OnDisable/OnDestroy 与 root lifecycle control flow 发射迁入 Semantic Emission。
- [x] 4.7 将 Blackboard declaration catalog、state address、scope layout 与 value edge 发射迁入 Semantic Emission。
- [x] 4.8 将 Action、Behavior 与 GameplayEffect catalog/reference 发射迁入 Semantic Emission。
- [x] 4.9 将 world request、output channel、capability manifest 与 producer declaration 发射迁入 Semantic Emission。
- [x] 4.10 将 operation、literal、state slot、catalog、producer 与 reference 的 SourceMap 发射迁入 Semantic Emission。
- [x] 4.11 禁止 Emission 再次递归 AssetDatabase、重新发现 nested graph 或按显示名解析 owner。
- [x] 4.12 禁止 Emission 读取 Numeric Target、Driver、WorldSolver、Network Model 或 Runtime state。
- [x] 4.13 让 Emission 在 discovered element 与 Emitter 输入不一致时明确失败，不跳过 operation。
- [x] 4.14 删除原 `CharacterSimulationGraphCompiler`，不保留 facade、partial、兼容 wrapper 或第二个 CompileGraph 入口。

## 5. 建立正式 Character Semantic Frontend

- [x] 5.1 建立 `CharacterSemanticFrontendCompiler`，只协调 SourceRevision、Discovery、Emission 与 artifact validation。
- [x] 5.2 建立 `CharacterSemanticFrontendResult`，返回 validated artifact、editor-only projection context 与分阶段 report。
- [x] 5.3 将 compile report 阶段细分为 AuthoringDiscovery、SemanticEmission、ArtifactValidation、TargetLowering 与 PresentationProjection。
- [x] 5.4 让 Frontend 对同一未修改 authoring 执行两次 Discovery/Emission 并比较 canonical bytes。
- [x] 5.5 让 Frontend 对 canonical bytes 执行 decode、header/payload identity 与 SemanticHash round-trip 校验。
- [x] 5.6 让 Frontend 失败结果不包含可供 Target 使用的半成品 artifact。
- [x] 5.7 保留 CharacterPipelineDefinition 作为唯一 root，不增加 Graph、Timeline 或 Profile 独立业务编译入口。
- [x] 5.8 将 CompilerVersion、OperationSetVersion 与 SourceRevision 计算所有权迁到 Frontend 正式入口。
- [x] 5.9 将依赖 asset GUID 与 bytes 纳入 SourceRevision 的现有规则迁入 Frontend，不按路径时间戳判断。
- [x] 5.10 删除旧 `CharacterSimulationProgramCompiler.CompileOnce` 与组合 Frontend 私有状态。

## 6. 建立 Library Artifact Store

- [x] 6.1 建立 Definition GUID 到 `Library/CharacterSimulation/SemanticIr/<guid>.csir` 的唯一安全路径映射。
- [x] 6.2 禁止使用 Definition 名称、asset path 片段或 ProgramId 冒号字符串作为文件名 fallback。
- [x] 6.3 建立只接受 validated artifact 的 store 写入入口。
- [x] 6.4 使用同目录临时文件写入完整 canonical bytes。
- [x] 6.5 在替换前重新读取临时文件并校验 header、payload 与 SemanticHash。
- [x] 6.6 通过原子替换发布当前 `.csir`，失败时保留原完整 cache。
- [x] 6.7 建立按当前 Definition expectation 加载 cache 的严格入口。
- [x] 6.8 cache 缺失、stale、损坏或版本不支持时返回精确状态，不读取旧版本兼容格式。
- [x] 6.9 保证 store 不创建 Unity Asset、`.meta`、Definition serialized reference 或 source-controlled 文件。
- [x] 6.10 保证清理 Library 后 Runtime Program/Projection 有效性不依赖 `.csir` 存在。

## 7. 强制 Float32 Target 消费 Validated Artifact

- [x] 7.1 建立 `Float32CharacterSimulationTargetCompiler` 正式入口，只接受 `ValidatedSemanticIrArtifact`。
- [x] 7.2 将当前 Float32 literal lowering、operation lowering、state layout、catalog、producer 与 source map lowering 迁入该 Target 入口。
- [x] 7.3 将直接接收 `CharacterGameplaySemanticIr` 的 raw lowerer 降为 Target 内部实现，不允许 Editor/Agent/BuildService 调用。
- [x] 7.4 在 Target 开始时校验 artifact OperationSetVersion 与 Float32 backend operation coverage。
- [x] 7.5 在 Target 开始时校验 artifact 不含不受支持的 semantic literal precision 或 capability。
- [x] 7.6 保持非法值、NaN/Infinity、超范围与 Float32 rounding 诊断附带原 source identity。
- [x] 7.7 保证 Program manifest 精确复制 artifact ProgramId、CompilerVersion、OperationSetVersion、SourceRevision 与 SemanticHash。
- [x] 7.8 保证 ProgramHash/LayoutHash 继续包含 Float32 NumericProfile 与 Target ABI。
- [x] 7.9 删除 `CharacterSimulationProgramCompiler.Lower(CharacterGameplaySemanticIr, report)` 外部组合路径。

## 8. 收口 Build Transaction 与调用者

- [x] 8.1 建立唯一 `CharacterSimulationBuildOrchestrator`，固定 Frontend、artifact store、Float32 Target、Projection 与 publish 顺序。
- [x] 8.2 让显式 `Assets/3C/Compile Character Simulation Program` 只调用该 orchestrator。
- [x] 8.3 让自动 stale build 只调用同一 orchestrator，不保留自动路径专用内存 lowering。
- [x] 8.4 让 BuildService 从 orchestrator result 取得 artifact descriptor、Program、Projection 与 report。
- [x] 8.5 让 Projection 构建复用 Frontend discovered model 的 Timeline/producer/source identity，不重新推导 gameplay flow。
- [x] 8.6 校验 Projection producer 集与 Semantic artifact producer 集完全匹配。
- [x] 8.7 校验 artifact、Program、Projection 的 ProgramId、SourceRevision、SemanticHash 与 ProgramHash 关系。
- [x] 8.8 将 Program Asset、Projection Asset 与 Definition generated references 延迟到整个 transaction 成功后更新。
- [x] 8.9 transaction 失败时保持旧 asset bytes 不变并报告当前 source stale，不把旧组合标记为 Ready。
- [x] 8.10 让 Definition Diagnostics 使用相同 Frontend/Target dry-run，但不写 Program/Projection。
- [x] 8.11 让 AgentGraphValidator 使用相同 Frontend/Target dry-run 与分阶段 report。
- [x] 8.12 用 artifact descriptor/identity 替换 `CharacterSimulationCompileResult.SemanticIr` 成功判定。
- [x] 8.13 删除旧 `CharacterSimulationCompileResult.SemanticIr` 属性和所有调用点。
- [x] 8.14 保持 Definition Inspector 普通 Repaint 不运行 Frontend、不计算完整 SourceRevision、不读取 `.csir` payload。

## 9. 建立只读 Semantic IR Inspector

- [x] 9.1 增加 `Assets/3C/Inspect Character Semantic IR` 显式菜单及选中 Definition 校验。
- [x] 9.2 在 Definition Inspector 的 Generated Artifacts/Diagnostics 折叠区增加 IR cache 状态与打开命令。
- [x] 9.3 建立只读 Semantic IR EditorWindow，并锁定当前 Definition GUID 与 artifact identity。
- [x] 9.4 使用虚拟化列表显示 Manifest 与全部 table counts。
- [x] 9.5 建立 Operations 列表与 handle/code/template/operand/literal/state 详情。
- [x] 9.6 建立 Literals 与 canonical numeric source identity 详情。
- [x] 9.7 建立 ControlFlow 列表与 source/target/port/order/priority/abort/condition 详情。
- [x] 9.8 建立 StateSlots 与 Scopes 列表及 owner/default identity 详情。
- [x] 9.9 建立 WorldRequests、OutputChannels、CatalogEntries 与 Producers 列表。
- [x] 9.10 建立 SourceMap 列表与 Graph/Node/Edge/Declaration/Timeline/Track/Clip identity 详情。
- [x] 9.11 建立按 operation code、identity、source type 与各 authoring id 的稳定搜索过滤。
- [x] 9.12 复用现有 Graph 导航能力按 GraphId/NodeId/EdgeId/DeclarationId 精确定位。
- [x] 9.13 复用现有 Timeline 导航能力按 TimelineId/TrackId/ClipId 精确定位。
- [x] 9.14 无法精确解析 source 时显示 unresolved，不按显示名、index 或最近窗口猜测。
- [x] 9.15 cache missing/stale/invalid 时停止展示旧 tables，并提供显式 `Compile Semantic IR` 命令。
- [x] 9.16 `Compile Semantic IR` 只调用正式 Frontend/store，不生成 Program/Projection 或第二条 semantic path。
- [x] 9.17 保证 Inspector 没有编辑、导入、覆盖 artifact 或写回 authoring 的入口。
- [x] 9.18 保证 Inspector Repaint 不自动编译、不扫描全部 Definition、不解码无关 Program。

## 10. 扩展普通 DotNet Reader

- [x] 10.1 将 Reader 命令改为显式 `semantic-ir` 与 `program` 子命令。
- [x] 10.2 删除单参数自动按 Program 读取的旧命令入口，不保留兼容解析。
- [x] 10.3 让 `semantic-ir` 通过 Core header reader 与完整 codec 加载 validated artifact。
- [x] 10.4 输出 Semantic IR ProgramId、CompilerVersion、OperationSetVersion、TickRate、SourceRevision 与 SemanticHash。
- [x] 10.5 输出 operations、literals、control flow、state slots、scopes、world requests、outputs、catalog、producers 与 source map counts。
- [x] 10.6 支持按显式 section 输出 operations、control-flow、state-slots、scopes、producers 与 source-map。
- [x] 10.7 建立稳定 text 输出，不依赖当前文化、Dictionary 顺序或对象 `ToString()` 的非 canonical 格式。
- [x] 10.8 建立稳定 JSON 只读输出，并明确不提供 JSON import/build 路径。
- [x] 10.9 子命令与 artifact 类型不匹配时返回非零退出码和明确格式错误。
- [x] 10.10 保持 Reader/Core/Float32 三个普通 .NET 工程直接链接 canonical source，不引用 UnityEngine 或复制 DTO。

## 11. Corin 生成产物与文档收口

- [x] 11.1 通过正式 Frontend 生成 Corin 当前 `.csir` cache。
- [x] 11.2 通过 validated Corin artifact 重新生成 Float32 Program 与 Presentation Projection。
- [x] 11.3 核对 Corin operation code、control-flow identity、state semantic、scope、producer 与 source-map 集合未因 Frontend 拆分丢失。
- [x] 11.4 核对 Corin Program/Projection 共享新的 SourceRevision、SemanticHash 与 ProgramHash 关系。
- [x] 11.5 核对 Definition generated references 仍只指向 Program/Projection，不新增 IR asset 引用。
- [x] 11.6 更新 `implementation-inventory.md`，记录 Frontend、artifact、Target、Inspector 与 Reader 的最终代码所有权。
- [x] 11.7 更新 `openspec/project.md`，把 Semantic IR 描述为 formal generated artifact 而非仅内存边界。
- [x] 11.8 更新 `add-deterministic-rollback-kcc-model` 文档为“同一 Semantic IR artifact 生成独立 Fixed Program/State/Kernel ABI”。
- [x] 11.9 保持 ServerAuthoritative 与 DotRecast active change 只消费匹配 Float32 Program，不要求 Runtime 加载 `.csir`。

## 12. 删除旧路径与静态验证

- [x] 12.1 删除旧单体 `CharacterSimulationGraphCompiler` 文件及其 meta。
- [x] 12.2 删除旧 `CompileOnce -> raw IR -> Lower` 组合方法与命名。
- [x] 12.3 删除旧 `CharacterSimulationCompileResult.SemanticIr` 及其 Agent/Inspector 读取路径。
- [x] 12.4 删除 Program-only Reader 旧 usage、兼容参数和 artifact magic 自动猜测。
- [x] 12.5 使用 `rg` 确认 Numeric Target 不引用 CharacterPipelineDefinition、Graph、Node、Timeline 或 Unity object。
- [x] 12.6 使用 `rg` 确认项目只存在一个 Semantic IR DTO、一个 canonical codec 和一个 Emitter registry 集。
- [x] 12.7 使用 `rg` 确认 Runtime Host、Kernel、Session 与 Presentation 不读取 `.csir` 或 Library artifact store。
- [x] 12.8 使用 `rg` 确认不存在 IR ScriptableObject、Definition IR serialized field、JSON import 或旧 artifact parser。
- [x] 12.9 使用普通 .NET Reader 的 `semantic-ir` 命令读取 Corin `.csir` 并输出全部身份与 table 摘要。
- [x] 12.10 使用普通 .NET Reader 的 `program` 命令读取 Corin canonical Program artifact并核对 SemanticHash。
- [x] 12.11 使用 `dotnet build --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译 portable Core、Float32 与 Reader 工程。
- [x] 12.12 portable build 后立即执行 `dotnet build-server shutdown`。
- [x] 12.13 使用 `dotnet build --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译 Runtime/Assembly-CSharp 工程。
- [x] 12.14 Runtime build 后立即执行 `dotnet build-server shutdown`。
- [x] 12.15 使用 `dotnet build --disable-build-servers /nr:false /p:UseSharedCompilation=false /m:1` 编译 Editor/Assembly-CSharp-Editor 工程。
- [x] 12.16 Editor build 后立即执行 `dotnet build-server shutdown`。
- [x] 12.17 更新本 change 的 proposal/design/spec/task 与最终实现差异，确保文档不继续宣称不存在的路径。
- [x] 12.18 运行 `openspec validate refactor-character-semantic-frontend-artifact --strict --no-interactive` 并修复全部错误。
