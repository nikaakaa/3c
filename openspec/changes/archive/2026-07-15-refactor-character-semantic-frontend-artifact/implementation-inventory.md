# Character Semantic Frontend 实施清单

## 迁移前正式入口

- `CharacterSimulationProgramCompiler.Compile` 同时负责 root 解析、SourceRevision、Graph/Timeline 编译、Semantic IR、Float32 lowering、Projection、双编译确定性比较与 artifact round-trip。
- `CharacterSimulationCompileResult` 直接暴露 `SemanticIr`、`Program`、`PresentationProjection` 与 `Report`。
- `CharacterSimulationGraphCompiler` 同时负责 Discovery、identity 校验、Emitter coverage、Blackboard/Scope、Graph/Edge、Timeline/TreeClip、Catalog 绑定与 SourceMap。
- `Float32CharacterSimulationProgramLowerer.Lower(CharacterGameplaySemanticIr)` 是 public raw lowering 入口。

## 迁移前调用者

- `CharacterSimulationProgramBuildService`：显式菜单、自动 stale build、generated asset 发布与 metadata 校验。
- `CharacterPipelineDefinitionEditor`：Compile、Diagnostics 与 generated artifact 状态。
- `AgentGraphValidator`：正式编译报告与 `CompileResult.SemanticIr` 成功判定。
- `CharacterPresentationProjection.Build`：由组合编译器使用 Program、Presentation Profile 与 GraphCompiler 收集的 Timeline 表构建。
- `ThirdPersonSimulation.Reader`：只支持单参数 Program artifact，不支持 Semantic IR。

## Corin 迁移前基线

- ProgramId：`character:c7a7c1e3f7e64d81b5a04a90cbeb8d4e`
- CompilerVersion：`character-simulation-compiler/12`
- SourceRevision：`f4509e49fd16ddfe8dd9c55bd55c747f782f85ccceec1b9e9e9f0c51c4d21f5d`
- SemanticHash：`0a3c3c5bbef534113d703dafee8fff917cfd7b7dd7599e8f5c63f280eedd197e`
- ProgramHash：`3aec29b4fbe5b3f24e41494f11cc2f0b6fe6ba2f646de28da17b64833283752a`
- LayoutHash：`0618222660eaf877db0331ceee8056060b914614d1a6e1d234bf9c30b4215d6e`
- Operations：`485`
- StateSlots：`804`
- SourceMap：`2636`
- SourceMapContentHash：`0d2468714ced0c6741ad3ff938bc112a4e12deb15adbd8397234a13888fb5748`

## 稳定排序核对清单

- SourceRevision dependency：normalized asset path ordinal；每项写入 path、GUID、完整 bytes。
- Input/Request/Action/Behavior/Tag/Attribute/GameplayEffect catalog：各自正式 identity ordinal。
- Graph declaration：DeclarationId ordinal。
- Graph node：Node GUID ordinal。
- Edge/PropertyEdge：Edge GUID ordinal。
- Nested graph reference：reference key ordinal；route 包含 owner Node GUID、reference key 与 scope id。
- Timeline Track：Track AuthoringId ordinal；Clip：Clip AuthoringId ordinal。
- Scope：Scope identity ordinal。
- Node field accessor：FieldKey ordinal。
- SourceMap 与所有 handle/index 继续按稳定发射顺序生成，不按 Unity InstanceId、显示名或 Dictionary 遍历顺序生成。

## Emitter coverage 核对清单

- Graph：Root、Loop、Parallel、Sequence、Selector、Succeed。
- State：StateMachine、State、Enter、AnyState、Exit、State OnEnter/OnExit/RootCompleted/ExitCause。
- Timeline：TimelineEnter、Timeline，以及 Animation、MotionCurve、Tree、ActionCue、CameraState、CameraCue、CameraResponse Track/Clip。
- Blackboard/Input/Value：ExposedProperty、Pipeline Blackboard value、Input Bool/Float/Vector2/Magnitude、ActionRequest、Compare、And、Or、Not。
- Action/GameplayEffect：ActionContext、ActivateAction、SubmitLifecycle、Tag、TagQuery、Attribute、Apply/Remove Effect。
- Motion：LocomotionInputMotion 与 Timeline motion curve。
- Node modules：ScopedGraphReference、StateBehaviorGraphReference、TreeReference、TimelineOwnership。
- Discovery 必须在发射前拒绝未注册 Node、Track、Clip 或 module；Emission 不允许静默跳过。

## 必删旧路径

- `CharacterSimulationGraphCompiler.cs` 及 `.meta`。
- `CharacterSimulationProgramCompiler.CompileOnce`。
- Editor/Agent/BuildService 可访问的 raw `Lower(CharacterGameplaySemanticIr)`。
- `CharacterSimulationCompileResult.SemanticIr`。
- Reader 的单参数 Program-only usage 与格式猜测空间。

## 不存在项确认

实施前仓库不存在正式 `.csir` store、Semantic IR Inspector、IR ScriptableObject、Definition IR serialized field或第二套 Semantic IR DTO/schema。现有 `CharacterGameplaySemanticIr` 与 `CharacterGameplaySemanticIrCodec` 是唯一 Core schema/codec。

## 迁移后代码所有权

- `CharacterAuthoringCompilationModel` / `CharacterAuthoringDiscovery`：只负责遍历 Definition 可达 authoring、identity/ownership/Emitter coverage 校验与稳定排序。
- `CharacterSemanticEmitter`：只消费 discovered model，发射 Semantic IR table，不重新遍历 Unity authoring。
- `CharacterSemanticFrontendCompiler`：固定执行 Discovery、Emission、canonical artifact validation 与双编译字节比较，不生成 Numeric Program。
- `CharacterGameplaySemanticIrCodec`：Core 中唯一 `.csir` header、payload、hash 与 validated artifact 构造入口。
- `CharacterSemanticIrArtifactStore`：只保存 `Library/CharacterSimulation/SemanticIr/<definition-guid>.csir`，原子替换并重新校验，不创建 Unity Asset。
- `Float32CharacterSimulationTargetCompiler`：只接受 `ValidatedSemanticIrArtifact`，生成 Float32 Program；raw lowerer 已收为 Target 内部实现。
- `CharacterSimulationBuildOrchestrator`：唯一 Build transaction，按 Frontend/store -> Float32 Target -> Projection -> 延迟发布顺序执行。
- `CharacterSemanticIrInspectorWindow`：显式打开的只读表格、搜索、详情与精确 Graph/Timeline source 导航，不编辑或导入 artifact。
- `ThirdPersonSimulation.Reader`：普通 .NET 的显式 `semantic-ir` / `program` 只读命令，直接链接 canonical Core/Float32 source。

## Corin 迁移后身份

- ProgramId：`character:c7a7c1e3f7e64d81b5a04a90cbeb8d4e`
- CompilerVersion：`character-simulation-compiler/13`
- OperationSetVersion：`character-gameplay-operations/3`
- SourceRevision：`2840b8b1ad6240f3a95808da7251041c258045949a8466b5d2bc9d3891546eb3`
- SemanticHash：`f6785b6c35dd3b32baf2b131dd16468d5e093fb88085a41055143e60abeb004e`
- ProgramHash：`5f39ddaeb5b39290657e5e162de75e9e6b130c2de275b64acf2e7b60e22b39aa`
- LayoutHash：`0618222660eaf877db0331ceee8056060b914614d1a6e1d234bf9c30b4215d6e`
- Operations / Literals / ControlFlow：`485 / 790 / 450`
- StateSlots / Scopes / Producers：`804 / 2 / 16`
- SourceMap：`2636`
- SourceMapContentHash：`0d2468714ced0c6741ad3ff938bc112a4e12deb15adbd8397234a13888fb5748`

迁移前后 `LayoutHash`、operation/state/source-map 数量与 `SourceMapContentHash` 完全一致。CompilerVersion、SourceRevision、SemanticHash 和 ProgramHash 因正式 artifact header、Frontend source set 与编译器版本升级而更新。

## 最终资产关系

- Definition generated references 仍只有 Float32 Program Asset 与 Presentation Projection Asset。
- `.csir` 只存在于 `Library`，没有 `.meta`、ScriptableObject 或 Definition serialized field。
- Program 与 Projection 共享 ProgramId、SourceRevision、SemanticHash；Projection 额外绑定同一次 Target 生成的 ProgramHash。
