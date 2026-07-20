# Agent Authoring Compiler 实施盘点

## 外部链路

- Snapshot、Intent、Patch IR 与 Report 共享 `AgentAuthoringSchema.Version`，实施前代码版本为 `agent-character-controller-synthesis.v7`。
- `AgentPatchAuthoringService` 是 MCP 与 Editor Window 的唯一资产写入口。
- `AgentCharacterControllerSynthesisWindow` 与 `ManageBtsmtlAgentAuthoringMcpTool` 只调用 Service。
- `AgentSynthesisEvaluator` 直接调用 Macro、Compiler 与 Validator，只做编辑期样例评估。
- Patch apply 顺序是 parse、full snapshot、dry-run、owner collect、单一 Undo、apply、validator、rollback 或 dirty/save。

## v7 Operation

正式存在 mutation 行为的 operation：

- `ensure_state_machine`
- `ensure_state`
- `ensure_transition`
- `ensure_condition_rule`
- `ensure_action_exit_lifecycle`
- `delete_state_behavior_node`
- `ensure_state_behavior_node`
- `ensure_timeline_node`
- `ensure_action_activation`
- `ensure_action_lifecycle_transition`
- `ensure_input_node`
- `link_flow`
- `link_property`

`bind_asset_reference` 同时出现在支持列表与 apply switch，但只返回 `bind_asset_reference_noop`，没有 mutation，也没有 Macro 生成它。v8 直接删除该操作。

## 重复解释位置

- `IsSupportedOperation` 解释 operation name。
- `ValidateIdentityShape` 按 operation name 解释目标 identity 形状。
- `ValidateReferences` 按 operation name 解释资产引用。
- `Apply` 按 operation name 再次选择 mutation 方法。
- `CreateConditionTerm` 按字符串解释具体 Condition 节点。

## Condition Term

正式 term 白名单：

- `move_stop`
- `move_has`
- `move_run`
- `move_walk`
- `turn_facing_angle`
- `blackboard_bool`
- `state_root_completed`
- `action_request`

每个 term 输出 bool PropertyPort；组内使用 AND，组间使用 OR，最后连接 `ConditionRuleResultNode.m_Result`。

## Compiler 单次状态

实施前 `AgentPatchCompiler` 实例字段保存 Definition、Snapshot、Resolver、Graph Index、RootTree、operation id、实际 Graph/Node/Edge 输出与 dirty owner。apply 后 Compiler 直接调用 `EditorUtility.SetDirty`。

## Validator 业务污染

`ValidateCorinAttackHierarchy` 读取 Definition 名称并硬编码：

- 外层 `None/Attack/DodgeBack/DodgeForward`。
- 内层 `Attack1/Attack2`。
- 固定 transition 集合。
- `Attack1Cancel/Attack2Cancel` 与 `Attack` request 的具体条件形状。
- 每个攻击 leaf 的 activation、inline Timeline 与三条 lifecycle 数量。

这些规则迁移到 `two_hit_combo` 的 typed plan coverage；通用 Validator 保留 Graph、Timeline、identity、ownership、TreeClip、Action Context 与正式 compiler 语义检查。

## 并行所有权

本 change 只修改 `Editor/AgentAuthoring`、对应 `.meta`、本 change 文档与 Agent current spec。不得修改 Network、Simulation Runtime、Presentation、Fantasy 协议或 Server、网络 Scene、Build 脚本及 `add-dotrecast-authoritative-server-backend`。

## v8 实施结果

正式链路已经收敛为：

```text
v8 Patch JSON
  -> AgentPatchCommandLowerer
  -> immutable AgentPatchCommandPlan
  -> AgentPatchCompileSession preflight
  -> AgentPatchAuthoringService asset transaction
  -> same plan handler apply
  -> generic AgentGraphValidator
  -> dirty touched owners and SaveAssets
```

- `AgentPatchCompiler` 只保留 `Prepare` 与 `Apply` 编排，不保存 Definition、Resolver、Index、operation output 或 touched owner。
- `AgentPatchCompileSession` 持有单次调用的 RootTree、Resolver、Index、planned/applied output、diff 与 touched owner。
- StateMachine、StateBehavior、Node/Asset、GraphLink 分别由四组 typed handler 处理。
- Condition term 使用独立 emitter registry；组内 AND、组间 OR、Result 连接由 `AgentConditionRuleBuilder` 统一处理。
- planning symbol 只保存 output kind 与 owner scope；后序 operation 的 kind、顺序和跨 Graph owner 均在 mutation 前校验。
- dry-run 与 apply 复用同一个 prepared plan，不重读 Patch DTO，不复制 Graph、Node 或 Edge。
- `AgentPatchAuthoringService` 是唯一 Undo、rollback、dirty 和 SaveAssets 所有者。

## 删除结果

- schema 已统一为 v8，v6/v7 输入直接拒绝。
- `bind_asset_reference` 已从 operation catalog、Compiler 和支持列表删除。
- 旧 Compiler operation 总 switch、Condition term 创建 switch和 Compiler dirty owner 路径已删除。
- 通用 Validator 不再读取 Corin 名称，不再硬编码 Action 状态、连段数量、cancel key 或 transition 集合。
- `two_hit_combo` 的 Action、Timeline、combo、exit coverage 只在 `AgentSynthesisEvaluator` 的 typed plan 评估中执行。
- Macro 的攻击 leaf 统一生成 Activation 与 Timeline；combo leaf 使用唯一 Action Exit Lifecycle，不再漏 Activation 或重复 Complete。

## 当前验证

- `Assembly-CSharp-Editor.csproj --no-dependencies`：0 error，1 个既有 BBB warning。
- `openspec validate refactor-agent-authoring-compiler-modules --strict --no-interactive`：通过。
- `openspec validate --all --strict --no-interactive`：62 项通过，0 项失败。
- `Assembly-CSharp.csproj`：0 warning，0 error。
