---
name: btsmtl-agent-authoring
description: 通过项目正式的BTSMTL Agent authoring工具读取、修改和验证CharacterController与AIController的Graph、StateMachine、Timeline、Blackboard、Perception和Intent关系，并在相关authoring代码变化时同步更新Agent Snapshot、Patch schema、lowerer、handler、validator和MCP bridge。Use when changing BTSMTL assets, Character or AI controller authoring, Agent Patch IR, or code that changes Agent-visible or Agent-writable authoring semantics.
---

# BTSMTL Agent Authoring

## 核心边界

把 `manage_btsmtl_agent_authoring` 作为自动修改 BTSMTL 资产的唯一入口。只通过它执行：

```text
export_snapshot
  -> dry_run_patch
  -> apply_patch
  -> export_snapshot
  -> validate
```

禁止直接编辑 Graph、StateMachine、ConditionRule 或内联 Timeline 的 Unity YAML。禁止用 `execute_code`、反射、剪贴板、临时菜单、临时 Patch 文件或第二个 MCP 服务绕过正式工具。

修改 C#、OpenSpec 和 Skill 文件仍使用 Codex 文件工具，不通过 Unity MCP 写代码。

## 先判断任务类型

### 修改 authoring 资产

只要目标是持久化修改以下内容，就执行“资产修改流程”：

- RootTree、SubTree、inline graph 或 shared graph。
- StateMachine、State、Transition、ConditionRule。
- State lifecycle body 中的节点和 flow/property edge。
- TimelineNode 的 inline/shared ownership、Timeline TreeClip、Action activation/lifecycle、Input node。
- AnimationTrack Marker Sync mode、group、topology、sync role与stable marker。
- Agent 已正式支持的 Character Blackboard declaration、TreeClip write、ActionWindow 与 ActionProfile 准入条件。
- ActionProfile granted/cancel tag policy 与 Definition 的 GameplayTagCatalog。
- AIControllerDefinition、AIControllerTree、AI Blackboard、Configured Candidate、Observation、Memory与Character Input/Request intent binding。

### 修改相关代码

只要代码变化会改变 Agent 能看到、能引用、能创建、能连接或必须验证的 authoring 语义，就执行“代码同步流程”。不能只改 runtime/compiler/editor 后留下过期 Agent 工具。

纯 runtime 私有实现变化可以不改 Agent schema，但必须完成影响扫描并说明为什么 Snapshot、Patch 和 Validator 都不受影响。

### 不属于此写入口

`CharacterAnimationPresentationProfile`、Animancer TransitionLibrary、动画 Layer/Priority 和 Presentation producer binding 只允许由各自正式 authoring 入口修改。Agent Snapshot 可以只读理解它们；Agent Patch 不得获得第二个写入口。

Animation Foot Analysis的Source identity、版本和算法摘要只允许Snapshot只读输出。Sole Speed、Height、Plant、Landing及其Library artifact是generated data，不得进入Timeline Curve Channel catalog、Patch payload或Rebuild operation；Agent仍只能修改正式注册的`Foot Placement Weight`等editable channel。

`CharacterBodyMotionProfile`同样不属于Agent写入口。当前schema v15 CharacterController Snapshot从精确`CharacterPipelineDefinition`引用只读投影Profile identity、content revision、GravityAcceleration、MaximumFallSpeed、semantic version与`AirborneVerticalMotion`要求；不得输出runtime VerticalVelocity或pending integration plan。Patch catalog、lowerer、handler与MCP bridge不得增加Profile mutation或任意SerializedProperty写入口。

## 资产修改流程

1. 明确`CharacterController`或`AIController` domain，并确定对应`CharacterPipelineDefinition`或`AIControllerDefinition`的唯一`Assets/...`精确路径。不得从资产类型、目录、显示名或场景猜domain和目标。
2. 确认 Unity 不在编译、更新 AssetDatabase、Play Mode 或 Play Mode 切换中。遇到 `editor_busy` 或 `play_mode_active` 时停止 authoring；退出 Play Mode 或等待编译结束即可，不要重启 Unity，不要改走 fallback。
3. 使用同一`domain + root_asset_path`调用`export_snapshot`。只使用当前schema v15 Snapshot的`rootIdentity`、`sourceRevision`、stable authoring identity和operation output reference；不得从YAML、列表index、display name、Actor名称或Tag猜identity。
4. 根据Snapshot生成最小`AgentPatchIR`。Patch必须使用`agent-character-controller-synthesis.v15`并原样携带`domain`、`rootIdentity`和`sourceRevision`；每个operation使用唯一`id`，后序operation只能引用已出现的前序output。
5. 调用 `dry_run_patch`。逐条处理机器可读的 `path/code/message/suggestion`，并确认 `plannedDiff` 正是业务预期。Dry-run 不得 dirty 资产。
6. 只有 dry-run 无错误时才用完全相同的 `patch_json` 调用 `apply_patch`。必须同时看到 `success=true`、`applied=true`、`saved=true`。
7. 使用同一domain和root再次调用`export_snapshot`与`validate`，确认root identity、source revision、拓扑、ownership、引用和正式Compiler report一致。
8. CharacterController检查SimulationProgram/PresentationProjection；AIController检查AIIntentProgram。只允许正式compiler发布generated asset，不直接编辑产物。

AIController Patch只允许`ensure_ai_controller_definition`、`ensure_ai_controller_tree`、`bind_ai_controller_assets`、`configure_ai_candidates`、`ensure_ai_blackboard_declaration`、`ensure_ai_shared_node`、`ensure_ai_observation_node`、`ensure_ai_memory_node`、`ensure_ai_continuous_input`、`ensure_ai_action_target`、`ensure_ai_action_request`、`ensure_bt_condition_rule`以及复用的`link_flow`、`link_property`。`ensure_ai_shared_node`只开放Shared capability中的Sequence、Selector、Loop、Compare与WaitTicks；`ensure_bt_condition_rule`只绑定AI Tree中明确的flow edge并显式配置AbortPolicy。AI Graph禁止Character execution、Timeline、MotionWarp、Transform副作用节点；AI Definition、RootTree与Perception Profile必须进入同一Undo/rollback事务。

如果 Patch catalog 无法表达所需 mutation，先停止资产修改并扩展正式 Agent schema/lowerer/typed command/handler/validator；扩展完成后再从新 snapshot 开始。工具能力缺失绝不是手改 YAML 的理由。

Patch 字段、operation 和当前代码地图见 [current-contract.md](references/current-contract.md)。

## 代码同步流程

1. 先读 current specs：
   - `openspec/specs/agent-character-controller-synthesis/spec.md`
   - `openspec/specs/btsmtl-agent-authoring-mcp-bridge/spec.md`
2. 用 `rg` 定位 authoring 模型、正式 compiler、Agent snapshot、patch、validator 和 MCP 的全部消费点。
3. 按变化类型同步更新：

| 代码变化 | 必须检查或更新的 Agent 模块 |
|---|---|
| 新增/删除节点类型、端口或 graph kind 规则 | Snapshot models/exporter、NodeEmitterRegistry、typed command/handler、GraphValidator、正式 compiler 对应约束 |
| 新增/删除 State lifecycle 或 graph ownership | Topology projection、GraphAuthoringIndex、TransactionOwnerCollector、lowerer/handler、Snapshot、Validator |
| 新增 Agent 可写操作 | `AgentPatchOperation` DTO、command kind/type、operation catalog/lowerer、handler catalog/handler、report、current spec |
| 修改 Condition 语义 | Condition term catalog/emitter、ConditionRuleBuilder、Snapshot、Validator、正式 compiler |
| 修改 Input/ActionProfile/ActionContext/Timeline/Blackboard identity | AssetResolver、SnapshotExporter、lowerer/handler、Validator |
| 修改 Definition Body Motion Profile或Program垂直能力 | Snapshot models/exporter、Definition/Profile正式校验、Simulation Compiler report；保持Patch/MCP只读 |
| 修改 Macro 业务结构 | MacroLibrary 与 MacroCoverageEvaluator；不要把角色名或连招名塞进通用 Validator |
| 修改 MCP action 或事务生命周期 | MCP tool、AuthoringModels、AuthoringService、EditorWindow 调用方、bridge current spec |
| 修改AI Definition、Perception、AI节点或Intent catalog | v15 AI Snapshot、domain-aware lowerer、AI handler、AI Validator、AI Compiler与current spec |

4. 外部 Patch/Snapshot 合同发生破坏性变化时提升 `AgentAuthoringSchema.Version`，直接删除旧 parser 和兼容路径。不要保留旧 schema reader、converter 或双写。
5. Agent mutation 必须继续走 `BaseGraph.CreateNode`、`Link`、`LinkProperty` 和正式 Timeline ownership API。不得在 handler 中直接维护第二套节点、边或 Timeline 数据。
6. `AgentPatchAuthoringService` 继续唯一拥有 Undo、rollback、dirty 和 SaveAssets；Compiler/handler 不得自行保存。
7. 相关代码改完后，必须使用正式工具完成至少 `export_snapshot` 与 `validate`。若修改了写能力，还必须用真实目标执行 `dry_run_patch`，并在需要迁移资产时执行事务 `apply_patch`。

## 完成门槛

- 没有直接 YAML mutation、fallback、临时桥接或第二套 graph 数据。
- Agent 可见/可写语义变化与 Agent 代码在同一 change 中更新；不能留“以后再补 Agent”。
- `dry_run_patch` 与 `apply_patch` 使用同一个 Patch JSON，apply 后再次 snapshot/validate。
- Editor 编译使用 `--disable-build-servers /nr:false /p:UseSharedCompilation=false`，完成后立即 `dotnet build-server shutdown`。
- 不运行 Unity batchmode，不新增测试，除非用户明确要求。
- 若修改外部合同，同步 OpenSpec delta/current spec；若只改资产，至少运行对应 change 的 strict validate。

## 故障处理

- `play_mode_active`：退出 Play Mode，不重启 Unity。
- `editor_busy`：等待当前编译/导入结束，不排队、不重试脚本化 fallback。
- `unsupported_schema_version`：重新导出当前snapshot，生成v15 Patch；不转换旧Patch。
- Marker Sync迁移使用`configure_animation_track_marker_sync`、`ensure_animation_sync_marker`、`move_animation_sync_marker`与`delete_animation_sync_marker`；MarkerGroup配置必须显式携带`animationMarkerSyncRole`；handler只能调用AnimationTrack正式authoring API。
- Timeline曲线修改使用唯一`configure_timeline_curve_channel`；目标必须来自v15 CharacterController Snapshot中的Timeline、Track、Clip stable identity与registered `curveChannelId`，payload必须完整携带wrap mode及全部Keyframe字段。禁止使用字段名、SerializedProperty path或key index作为外部identity。
- `unknown_operation` / `unknown_node_type`：扩展正式 catalog/emitter 或承认当前工具不支持；禁止创建 placeholder。
- `transaction_owner_*`：修复 ownership/topology，使全部 serialized owner 可进入同一事务；禁止缩小 Undo 范围。
- apply 后 validator 失败：接受自动回滚，修 Patch 或 Agent 实现后重新从 snapshot 开始。
- MCP transport 错误：修复连接或 Editor 编译状态；禁止改用 YAML、`execute_code` 或剪贴板。
