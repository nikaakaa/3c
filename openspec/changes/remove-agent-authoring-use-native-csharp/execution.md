# 实施记录

## 1. 实施基线

- 规划版本：`r2-skill-and-generated-source-2026-09-13`。
- 规划入口：`proposal.md`、`design.md`、`tasks.md` 与 `specs/`。
- 实施入口：本文件。
- 本记录只描述本 change 的公共 C# authoring、旧 Agent authoring 退役、领域迁移和真实执行结果；运行时 motor 参数桥、动画变量推进与消费闭环仍是外部依赖，不由本 change 代为完成。

## 2. 正式代码链

公共入口位于 `Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/CodeGeneration/`：

- `BtsmtlAuthoringCodeExportService` 只接收精确资产、Definition、输出源码路径、recipe、入口类型与命名空间；领域适配直接读取正式对象，公共层只负责对象变量、外部依赖、创建/配置/绑定/连线/根绑定阶段和源码写出。
- `BtsmtlAuthoringGenerationService` 只执行精确的已编译 `IBtsmtlAuthoringGenerationEntry`，核对源码路径、recipe、入口类型、Definition 和输出路径后才调用正式业务 API，并返回创建、替换、删除和保存结果。
- `BtsmtlSkillAuthoringCodeAdapter` 直接遍历 Skill Graph 闭包，覆盖 Graph、Macro、Blackboard、原生 FSM、State Body、Timeline、Track、Clip、Section、Node、Edge、Curve 和根绑定；不再经过 Skill Document。
- `BtsmtlPoseAuthoringCodeAdapter` 直接读取 Pose Graph、动态端口、Source/Resource Slot、Pose StateMachine、Transition Rule、layout 和正式节点 payload；无法表达的 payload 明确失败，不以默认值或 JSON 补齐。
- EventGraph 通过独立领域薄适配接入公共导出，不恢复 `EventGraphAuthoringDocument` 或旧文档 mutation。
- `TimelineAuthoringClipBinding` 保留 typed 配置与读取，删除 JSON token、`JObject` 应用、旧 Export 和重复属性校验；Slate 投影只消费 typed binding。

正式导出创建源码保留在：

- `Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/CodeGeneration/Generated/CorinAttackSkillAuthoringCode.cs`
- `Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/CodeGeneration/Generated/LocomotionFullBodyPoseGraphAuthoringCode.cs`

两份源码均由 `ThirdPersonClient.Editor.csproj` 编译，作为正式入口而不是临时验证文件。

## 3. 旧链删除

D7.1 门槛已经满足：作者调用者已转到正式 C# / UI / domain API，Skill、Pose、EventGraph 和 Timeline 的有效操作已有正式承接，且新的导出、生成、绑定和保存入口可用。随后完成以下删除：

- `81ea8289d` `移除AgentAuthoring旧工具链`：删除 `Editor/CharacterPipeline/AgentAuthoring` 整个旧协议目录、五个旧 MCP、scheduler、窗口、Document/Snapshot/Codec/Store/Exporter/Reconciler/Mutation/Session/Validator/Report、协议测试和 asmdef，共 61 个文件。
- `2bf921408` `删除Skill旧Agent文档链`：删除 `Authoring/SkillDocument` 协议适配及 `BtsmtlSkillNodeAuthoringBinding.cs`，共 35 个文件；正式 Skill/FSM/Timeline API 保留在 Runtime/Authoring/FlowGraphs。
- `9c9520afa` `删除Presentation旧Agent文档链`：删除 `Authoring/PresentationDocument` 目录下的 codec、exporter、reconciler、validator、mutation plan 和协议模型，共 27 个文件；Pose 正式 adapter/mutation 保留。
- `6b7be5c7b` 与 `ad698053a`：删除 Pose/EventGraph 侧旧文档桥接和调用者，EventGraph 保留正式运行、变量和 mutation 业务。
- 删除 `.codex/skills/btsmtl-agent-authoring/` 下的 `SKILL.md`、`references/current-contract.md` 和 `agents/openai.yaml`；新的作者合同归当前 change 文档和正式 API，不保留旧 skill 别名。

删除后源码扫描结果：

```text
rg --files Assets/GameScripts/Main | rg "AgentAuthoring|SkillDocument|PresentationDocument|BtsmtlSkillNodeAuthoringBinding"
=> 无结果

rg -n "AgentAuthoring|AgentSkill|SkillDocument|PresentationDocument|AgentDocument|BtsmtlSkillNodeAuthoringBinding|EventGraphAuthoringDocument" Assets/GameScripts/Main -g "*.cs"
=> 无结果
```

当前 Unity MCP 注册表共 49 个项目工具；按 `btsmtl.` 过滤只有：

- `btsmtl.export_code`
- `btsmtl.generate_assets`
- `btsmtl.scene_play`（Scene Play，非 authoring 工具）

旧 `checkout_document`、`rebase_document`、`dry_run_document`、`apply_document`、`validate` 均未注册。

## 4. 真实执行结果

### 4.1 静态编译

命令：

```text
dotnet build ThirdPersonClient.Editor.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false
dotnet build-server shutdown
```

结果：`ThirdPersonClient.Editor.csproj` 0 个错误、93 个既有警告，成功生成 `ThirdPersonClient.Editor.dll`；随后 MSBuild 与 VB/C# 编译器服务器均成功关闭。没有新增 CodeGeneration 编译错误。

### 4.2 export_code

- Corin EventGraph 根已通过 EventGraph 薄适配导出检查，响应无领域诊断；临时 EventGraph 输出文件未作为正式产物保留。
- Pose 根 `Assets/Configs/Animation/Presentation/PoseGraphs/LocomotionFullBodyPoseGraph.asset` 使用精确 Corin Definition 导出到 `LocomotionFullBodyPoseGraphAuthoringCode.cs`，响应成功、diagnostics 为空；输出覆盖 9 个 Graph、7 个 Source Slot、5 个 Resource Slot、StateMachine、Transition Rule 与 layout，并返回 Definition/Profile 外部依赖。
- Skill Attack 的正式输出源码为 `CorinAttackSkillAuthoringCode.cs`，包含原生 FSM 和 Skill-owned Timeline 的创建、配置、连接与根绑定。

### 4.3 generate_assets

使用已编译 Pose 入口：

```text
source_code_path: Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/CodeGeneration/Generated/LocomotionFullBodyPoseGraphAuthoringCode.cs
recipe_type: character.pose.corin.locomotion/v1
entry_type_name: ThirdPersonCharacter.Generated.LocomotionFullBodyPoseGraphAuthoringCode
definition_asset_path: Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset
output_asset_path: Assets/Configs/Animation/Presentation/PoseGraphs/LocomotionFullBodyPoseGraph.asset
```

Unity 返回：`saved=true`、`replaced_asset_paths` 包含精确 Pose 根、`created_asset_paths=[]`、`deleted_asset_paths=[]`、`diagnostics=[]`。这证明新入口完成了正式 Pose Graph 替换、Profile/Definition 绑定和保存，没有调用旧 Document 生命周期。

### 4.4 Character Float32 / Presentation Projection

在原生 FSM 闭包的正式 discovery、semantic emission、SourceMap 和 TargetLowering 修正后，使用同一 Unity 实例和精确 Corin Definition 执行 `character.build_float32_products`，job 为 `8a40f68244b34acc92b5d620e804be42`。

结果：Exact Float32 Program 和 Presentation Projection 已发布。

- Program wrapper：`Assets/Configs/Character/Corin/Pipeline/Definition/Generated/CorinCharacterPipelineDefinition.SimulationProgram.asset`
- Presentation Projection：`Assets/Configs/Character/Corin/Pipeline/Definition/Generated/CorinCharacterPipelineDefinition.PresentationProjection.asset`
- Numeric profile：`float32-ieee754`，Target ABI：`8`
- Program ID：`character:c7a7c1e3f7e64d81b5a04a90cbeb8d4e`
- Source revision：`aa43ee9c9f02ac8de30c6c8dc3c0cdad54f3b6defaeed1ea54bcd425ee3a6285`
- Semantic hash：`54f5241e0aee361b758c273957bbe4f766b9b0c05ae2f82fd211ce3b0c69f7ae`
- Program hash：`13b21c66820073e108c63f64eee960f70f65459e468b535f128559643be61cfe`
- Projection revision：`c0f3985f670358d320b6973afb6883300e9410c8a660beeb73e7976ce2bf236f`
- Canonical bytes：`2109553`

期间暴露的空 `Child`、State Body 可见范围、Entry/Any/Exit 生命周期 owner、SourceMap content hash 和原生 FSM edge identity 参数错误均已按正式来源修正；没有用 fallback 或默认 hash 放行。Float32 结果中的 `float32_literal_rounded` 与 `float32_state_abi` 是正式 TargetLowering 信息诊断，不是失败。

### 4.5 Unity 边界

Unity 实例为 `3C_Client@e852139597e42532`，项目路径为 `D:/Unity_Project_1/3C/3cDemo/Client/3C_Client`。最近状态为非 Play、idle、未编译、未进行 AssetDatabase 刷新，`ready_for_tools=true`。

当前 Console 中保留了此前已有的 Unity 内部 Inspector 空对象异常：`GameObjectInspector.OnEnable`、`SerializedObjectNotCreatableException` 和 `GameObjectInspector.OnDisable`；本次 C# authoring 生成响应没有新增领域 diagnostic 或 authoring 堆栈。该 Console 状态不被静态编译结果掩盖，仍需用户按自己的端到端流程处理或复核。

本 change 没有新增测试代码，也没有把用户手动验收写入 tasks。

## 5. 当前规范口径

- 删除 current `agent-character-controller-synthesis`、`btsmtl-agent-authoring-document-sync` 和 `btsmtl-agent-authoring-mcp-bridge`。
- 新增 current `character-csharp-authoring`，规定两个显式 MCP、完整导出、明确生成、非同步语义、正式领域校验、外部资源边界和旧协议激进删除。
- 更新 `graph-authoring-domain-framework`、`character-presentation-pose-graph`、`character-state-timeline-authoring-loop`、`character-animation-presentation-authoring`、`character-animation-clip-authoring`、`character-input-pipeline`、`character-pose-plan-compilation`、`character-motion-warp-authoring`、`character-camera-pipeline`、`character-state-interruption-authoring`、`btsmtl-compiled-simulation-program`、`btsmtl-graph-core`、`btsmtl-timeline-editor-preview` 和 `unity-simulation-assembly-ownership`，删除旧 Agent/Document 正向作者链，保留各领域业务约束。
- 更新 `openspec/project.md`：当前作者合同改为显式 C# export/generate，CharacterPipeline 路径移除 `AgentAuthoring`，Equipment 后续接入目标改为正式 Equipment authoring API；Performance Capture 的独立 Capture Agent 仍是不同职责，不受本 change 删除。

## 6. 未并入本 change 的外部依赖

固定 motor 参数桥、动画变量的运行时推进与消费闭环，以及 Fixed 产品 Build 不因作者协议退役自动完成。它们继续由各自领域任务和显式 Build 入口负责；本 change 不恢复旧 Agent 协议，也不把 C# authoring 成功描述为运行时闭环完成。
