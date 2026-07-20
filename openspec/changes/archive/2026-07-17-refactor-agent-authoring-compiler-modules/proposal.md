# Change: 重构 Agent Authoring Compiler 内部模块边界

## Why

当前 Agent authoring 对外已经形成唯一 `Snapshot -> Intent/Macro -> Patch IR -> Compiler -> Validator -> Report -> BTSMTL assets` 链路，但内部实现仍把 schema 校验、identity 引用、dry-run 计划、资产解析、Graph mutation、ConditionRule 合成、operation 结果索引和 dirty owner 管理集中在 1789 行的 `AgentPatchCompiler` 中。新增一种 Patch operation 需要同步修改多处字符串 switch，容易出现“schema 声称支持、apply 实际没有行为”或 dry-run/apply 解释不一致。

`AgentGraphValidator` 还会按 `CharacterPipelineDefinition.name` 判断 Corin，并硬编码 `None/Attack/DodgeBack/DodgeForward`、`Attack1/Attack2`、cancel key 和 transition 形状。该规则属于二连击样例覆盖，不是所有 Character Graph 的通用 BTSMTL 合法性。继续保留会让通用 Validator 随具体角色内容扩张。

当前代码已经统一输出 schema v7，但 current spec 仍写 schema v6；同时 v7 把 `bind_asset_reference` 列为支持操作，apply 时却只输出 no-op 信息，没有执行绑定。此次重构需要把这两个事实一次收敛，不把旧版本、假操作或兼容解析带进新模块。

## Dependencies

- 当前 `agent-character-controller-synthesis` 与 `btsmtl-agent-authoring-mcp-bridge` MUST继续作为外部行为真相。
- 本change MAY与`add-dotrecast-authoritative-server-backend`并行实施；两者不得编辑同一代码、资产或spec所有权。
- 本change MUST不修改 Gameplay Program、Simulation Session、Network Model、WorldSolver、Presentation、Scene、Build Profile或协议。

## What Changes

- 将 editor-only Agent Snapshot、Patch IR和 Report schema直接提升到v8，只接受v8，不保留v6/v7 reader或兼容分支。
- 从v8移除没有正式行为的`bind_asset_reference`操作；资产绑定继续由对应typed ensure command和正式Node Emitter原子完成。
- 建立唯一`AgentPatchCommandLowerer`与operation catalog，把宽序列化`AgentPatchOperation`一次降低为immutable typed command plan。
- 建立每次编译独占的`AgentPatchCompileSession`，统一拥有Definition、Snapshot、Resolver、Graph Index、operation symbol、apply结果和touched owner，不再把单次运行状态保存在Compiler实例字段中。
- 将Patch执行拆成按职责聚合的StateMachine、StateBehavior、Node/Asset、GraphLink handler，以及独立ConditionRule builder与term emitter registry；不为每个operation创建一套重复框架。
- 让dry-run和apply消费同一typed command plan与同一handler catalog；dry-run使用窄symbol table表达前序operation输出，不克隆Graph或建立第二份authoring模型。
- 让`AgentPatchAuthoringService`继续唯一拥有Undo、dirty、rollback和SaveAssets；Compiler只报告touched owner与结构化diff。
- 删除`AgentGraphValidator`中的Corin名称判断和具体连招拓扑检查；通用Validator只检查正式Graph、Timeline、identity、ownership和Character authoring语义。
- 将`two_hit_combo`等具体样例覆盖放到Agent synthesis/macro coverage evaluator，只验证对应Macro产出的typed command plan，不污染通用Graph Validator。
- 删除旧字符串operation分派、旧Condition term switch、Compiler内`EditorUtility.SetDirty`、Corin专用Validator helper和`bind_asset_reference` no-op。

## Non-Goals

- 不重构`AgentGraphSnapshotExporter`、`AgentPatchIdentityBinder`、MCP transport或Agent Controller Window UI。
- 不新增Patch operation、Condition term、Node Emitter或角色动作能力。
- 不改变BTSMTL Graph、Timeline、Blackboard、ActionProfile或CharacterPipelineDefinition资产格式。
- 不改变MCP的四个action、请求字段或`AgentCompileReport`业务字段。
- 不新增测试、人工验证task、Unity batchmode或运行时Agent能力。
- 不建立v7到v8的converter、双schema入口、fallback operation或旧Compiler facade。

## Current Spec Comparison

- current `agent-character-controller-synthesis`要求Compiler使用正式BTSMTL authoring API、Validator检查通用语义、Agent评估检查二连击业务覆盖。本change保留这些要求，并明确把通用Validator与Macro样例覆盖分层。
- current spec仍声明schema v6，而代码中的`AgentAuthoringSchema.Version`已是v7。本change将Snapshot、Patch、Report和服务入口统一到v8，并删除旧版本接受路径。
- current Patch IR requirement把`bind asset`描述为正式操作，但当前Compiler中的`bind_asset_reference`没有mutation行为。本change删除独立no-op，资产绑定只允许由需要该资产的typed ensure command通过正式Emitter完成。
- current `btsmtl-agent-authoring-mcp-bridge`已经要求MCP和Window共用唯一`AgentPatchAuthoringService`以及完整Undo事务。本change不修改Bridge能力，只收紧Service与Compiler内部所有权。
- 未发现active网络change拥有AgentAuthoring目录或Agent specs；本change与网络主线没有实现重叠。

## Impact

- 修改能力：`agent-character-controller-synthesis`。
- 主要代码范围：`Assets/GameScripts/Main/Runtime/Character/Pipeline/Editor/AgentAuthoring/`。
- 保留入口：`AgentPatchAuthoringService`、`AgentPatchCompiler`、`AgentGraphValidator`、现有MCP Bridge与Editor Window。
- 删除：Compiler多处字符串分派、`bind_asset_reference` no-op、Compiler dirty owner写入、Corin专用Validator路径和旧schema接受口径。
- 网络并行边界：不编辑`Runtime/Networking`、`Runtime/Simulation`、`Runtime/Character/Pipeline/Presentation`、网络Scene、协议和网络change文档。
