# BTSMTL Agent Authoring 当前合同

## 目录

- 正式调用链
- MCP action
- schema v16 Patch operation
- Condition term 与节点白名单
- 代码所有权地图
- Patch 示例
- 影响扫描清单

## 正式调用链

```text
manage_btsmtl_agent_authoring
  -> AgentPatchAuthoringService
  -> domain-aware Character/AI Snapshot exporter
  -> AgentPatchCommandLowerer
  -> immutable AgentPatchCommandPlan
  -> AgentPatchCompileSession preflight
  -> domain-aware transaction owners
  -> one Undo transaction
  -> AgentPatchCompiler.Apply(same plan)
  -> typed handler + formal BTSMTL authoring API
  -> domain-aware formal Validator
  -> dirty touched owners + SaveAssets
  -> CharacterSimulationProgramBuildService or AIIntentProgramBuildService
  -> publish domain generated product
```

`AgentPatchCompiler` 不拥有 Undo、dirty、rollback 或 SaveAssets。MCP bridge 和 Editor Window 都调用同一个 `AgentPatchAuthoringService`。

## MCP action

| action | 输入 | 副作用 | 输出重点 |
|---|---|---|---|
| `export_snapshot` | domain + root asset path | 无 | v16 Full Snapshot；Character输出Graph/Timeline/MotionWarp/Marker/Curve与只读PoseGraph/BlendLibrary/Rig/AnimationChannel→PoseSlot/source合同，AI输出Definition/Tree/Capability/Blackboard/Perception/Character catalog/generated Program合同 |
| `dry_run_patch` | domain + root asset path + Patch JSON | 无 | planned diff、message、metrics |
| `apply_patch` | domain + root asset path + 同一 Patch JSON | 单一 Undo 事务，成功后保存 | applied diff、`applied`、`saved` |
| `validate` | domain + root asset path | 无 | domain正式Validator + compiler report |

domain必须是`CharacterController`或`AIController`；root路径必须是使用`/`的精确`Assets/...`路径并解析成匹配Definition类型。四个action在Unity编译、AssetDatabase更新、Play Mode或切换Play Mode时都会被拒绝。

## schema v16 Patch operation

当前`AgentAuthoringSchema.Version`：`agent-character-controller-synthesis.v16`。v15及更早reader、writer、converter和operation alias已删除。Patch根必须显式携带`domain`、`rootIdentity`与`sourceRevision`。

CharacterController Snapshot的`presentation`只读投影固定包含Profile、PoseGraph、BlendLibrary、Rig的asset identity与revision、`channelBindings[].animationChannelId/poseSlotId/outputPolicy`以及producer的`animationChannelId/poseSlotId/sourceAssetPath/sourceAssetGuid/sourceAssetType`。Timeline Track与Marker Group统一使用`animationChannelId`。旧`layers`、`layerId`、`transitionLibraryAsset*`、`transitionAsset*`与`easing`字段已删除，不提供alias。Patch catalog仍不提供PoseGraph、BlendLibrary、Rig、PoseSlot或producer source mutation。

Presentation authoring bootstrap不属于Agent mutation。`CharacterAnimationPresentationProfileEditor`只调用`CharacterAnimationPresentationAuthoringService`，从显式Definition的RootTree与正式composition roots递归枚举Timeline、AnimationTrack和source clip stable identity，再按Pose Graph声明解析`AnimationChannelId -> PoseSlotId`。该流程不读取已生成Program/Projection，不按Layer、显示名、目录或旧binding发现producer；Agent只消费最终只读Snapshot投影。

| operation | typed command 业务 |
|---|---|
| `ensure_state_machine` | 创建或确认 StateMachineNode/inline StateMachineGraph |
| `ensure_state` | 创建或确认 State |
| `delete_state` | 按 StateMachine 与 State 的 stable identity 删除 State、所有入/出 transition、condition graph 与 inline state body |
| `ensure_transition` | 通过显式 stable edge identity 创建或确认一条具体 State transition；允许同端点存在不同业务条件和优先级的边 |
| `ensure_condition_rule` | 通过显式 stable edge identity 创建或更新一条具体 transition 及 inline ConditionRuleGraph |
| `ensure_action_exit_lifecycle` | 使用显式 `cancelConditionGroups`（组内 AND、组间 OR）、`reason`、`interruptReason`、`abortReason`、`completeReason` 合成 action exit selector；StateTransition replacement 提交 Cancel，Self/LowerPriority abort 提交 Interrupt，ParentStop 提交 Abort，自然退出提交 Complete；重复 apply 时用 `targetElementAuthoringId` 精确替换既有 `Action Exit` selector，首次迁移的旧节点必须显式删除 |
| `delete_state_behavior_node` | 从明确 State behavior graph 删除一个节点及其正式连接 |
| `ensure_state_behavior_node` | 在 State body 创建白名单节点 |
| `ensure_timeline_node` | 创建/配置 Inline 或 Shared TimelineNode |
| `ensure_motion_warp_track` | 在明确Timeline创建或幂等确认唯一MotionWarpTrack |
| `ensure_motion_warp_clip` | 在明确MotionWarpTrack按stable identity创建或幂等确认Warp窗口 |
| `configure_motion_warp_source` | 将Warp绑定到同一Timeline内明确stable identity的MotionCurveClip |
| `configure_motion_warp_parameters` | 原子配置TranslationMode、TargetOffsetSpace、RotationMode、RotationMethod、target offset、correction limits、yaw rate、LimitPolicy与canonical progress curves |
| `ensure_action_activation` | 创建并配置 Action activation |
| `ensure_action_lifecycle_transition` | 创建并配置单个 lifecycle submit 节点 |
| `ensure_input_node` | 创建并绑定正式 Input/Action request node |
| `delete_flow_edge` | 按 stable graph/edge identity 删除普通 flow edge，用于保留节点 identity 的结构重接 |
| `link_flow` | 通过正式端口连接 flow edge |
| `link_property` | 通过 PropertyPort PortId 连接 property edge |
| `ensure_blackboard_declaration` | 在明确 Graph owner 创建或配置 typed Blackboard declaration、projection metadata；`InputDerived`必须携带稳定`inputId`并满足Character/Spawn/非PresentationOnly约束 |
| `move_blackboard_declaration` | 在同一事务内把 stable declaration identity 从明确 source Graph 迁到明确 target Graph，并原子更新key、projection与input binding metadata；重复 apply 在 target owner 幂等确认 |
| `delete_blackboard_declaration` | 按 stable declaration identity 删除 declaration |
| `ensure_timeline_tree_clip` | 在明确 Timeline owner 创建或配置 TreeClip range、phase 与 inline tree |
| `move_timeline_clip` | 按 Timeline、Track、Clip stable identity 平移现有 Clip；MotionCurve 同步平移绝对 CurveEndFrame，并重算 Track overlap mix |
| `configure_timeline_clip_ease` | 按 Timeline、Track、Clip stable identity 原子配置 `SelfEaseInFrame` 与 `SelfEaseOutFrame`；拒绝负数、超出 Duration 或与 overlap 冲突的值 |
| `configure_timeline_curve_channel` | 按 Timeline、Track、Clip stable identity与registered ChannelId原子替换完整curve；保留pre/post wrap及全部Keyframe字段，拒绝未知channel、owner不匹配、字段名目标、缺key、无序或领域校验失败 |
| `configure_animation_track_channel` | 按Timeline与AnimationTrack stable identity原子替换非空`AnimationChannelId`；handler只调用`AnimationTrack.SetAnimationChannelId`，不联动PoseSlot、Pose Graph或Profile |
| `configure_animation_track_marker_sync` | 按Timeline与AnimationTrack stable identity原子配置None或MarkerGroup；MarkerGroup显式携带CanBeLeader、AlwaysLeader或AlwaysFollower，None清空group、topology、role与markers |
| `ensure_animation_sync_marker` | 按Timeline、Track与Marker stable identity创建或幂等确认MarkerId和整数frame |
| `move_animation_sync_marker` | 按Marker stable identity移动到整数frame，不按列表index定位 |
| `delete_animation_sync_marker` | 按Marker stable identity删除marker |
| `delete_timeline_clip` | 按 stable clip identity 删除 Timeline clip |
| `ensure_tree_clip_blackboard_write` | 在 TreeClip inline tree 创建指向正式 declaration 的 typed setter；declaration 使用 `declarationAuthoringId` 或同一 Patch 前序 `declarationOperationId` |
| `ensure_blackboard_write` | 在明确 State body 创建或确认普通 Bool Blackboard setter；不隐式连接 flow |
| `delete_transition` | 按 stable edge identity 删除 State transition |
| `ensure_gameplay_tag` | 在 Definition 的正式 GameplayTagCatalog 中创建 tag |
| `set_action_profile_granted_tags` | 原子替换 ActionProfile granted tags |
| `set_action_profile_cancel_query` | 原子替换 ActionProfile cancel query |
| `set_action_profile_target_requirement` | 按当前Definition内明确ActionProfile原子配置`None`、`OptionalSnapshot`或`SnapshotRequired` |
| `set_action_request_timing_class` | 按当前 Definition 的 request id 原子配置 `Immediate` 或 `Offensive` timing class |
| `ensure_ai_controller_definition` | 在AIController domain通过正式Definition API确认ControllerId与完整绑定 |
| `ensure_ai_controller_tree` | 确认Definition绑定的正式AIControllerTree资产与stable root identity |
| `bind_ai_controller_assets` | 原子绑定受控Character Definition与Perception Profile |
| `configure_ai_candidates` | 通过Perception Profile正式API原子替换候选ActorId与排序策略 |
| `ensure_ai_blackboard_declaration` | 在AI Graph创建或配置AIController/AITick/Graph scope的typed memory declaration |
| `ensure_ai_shared_node` | 在AI Graph创建Sequence、Selector、Loop、Compare或WaitTicks，并通过正式authoring API配置LoopStopType与CompareType |
| `ensure_ai_observation_node` | 创建ReadSelf、ConfiguredCandidates、SelectNearest、TargetDistance或TargetDirection节点 |
| `ensure_ai_memory_node` | 创建并绑定typed Read/Write AI Memory节点 |
| `ensure_ai_continuous_input` | 按受控Character catalog绑定连续InputId |
| `ensure_ai_action_target` | 按受控Character catalog绑定ActionTargetSnapshot InputId |
| `ensure_ai_action_request` | 按受控Character catalog绑定RequestId、buffer、priority与repeat policy |
| `ensure_bt_condition_rule` | 为AI Tree中明确的flow edge创建或配置ConditionRuleGraph与AbortPolicy；条件组内AND、组间OR |

不存在按端点或显示名猜测的通用 `delete_edge`、任意 serialized field write 或 `bind_asset_reference`。Timeline Clip 只开放表格中的窄类型化编辑；需求超出表格时先扩展正式 Agent 工具，不得手改资产。

## Identity 规则

- Snapshot path、display name 和列表 index 只用于阅读，不能替代 stable authoring identity。
- Snapshot、Patch与Intent根的domain、rootIdentity和sourceRevision必须完全一致；不得从资产类型或内容推断domain。
- State body 中的 Action activation 与 lifecycle transition 必须输出 `nodeAuthoringId`，迁移删除不得按显示名或 lifecycle 类型猜测节点。
- 每个 Patch operation 必须有唯一 `id`。
- 已存在对象使用 `*AuthoringId`。
- 同一 Patch 内新建对象由后序 operation 使用 `*OperationId` 引用。
- State body 可以用 `targetGraphAuthoringId` 直接定位，或用 `stateMachineGraphAuthoringId + stateAuthoringId` 定位；两种形式不能同时出现。
- Asset 使用当前 Definition/Snapshot 中的稳定 logical id 或显式 path/GUID；Resolver 不扫描目录找同名替代品。

## Condition term 与节点白名单

Condition term：

- `move_stop`
- `move_has`
- `move_run`
- `move_walk`
- `turn_facing_angle`
- `blackboard_bool`
- `state_root_completed`
- `action_request`
- `action_window_active`，必须显式携带 typed `WindowType`
- `action_can_activate`，必须显式携带当前Definition内的稳定`ActionProfile` identity；Optional/Required Profile还必须携带`targetSnapshotBlackboardKey`

Condition group 内使用 AND，group 间使用 OR，最终连接 `ConditionRuleResultNode`。

当前普通节点 emitter 白名单：

- `StateMachineNode`、`StateNode`
- `SequenceNode`/`Sequence`、`SelectorNode`
- `TimelineNode`
- `ActivateActionInstanceNode`、`SubmitActionLifecycleTransitionNode`
- `CharacterActionRequestInfoNode`
- `CharacterInputBoolInfoNode`、`CharacterInputFloatInfoNode`、`CharacterInputVector2InfoNode`
- `CharacterInputVector2MagnitudeInfoNode`、`CharacterMoveFacingAngleInfoNode`
- `PipelineBlackboardBoolInfoNode`
- `ActionWindowActiveInfoNode`、`CanActivateActionInfoNode`
- `StateRootCompletedNode`、`StateExitCauseInfoNode`、`ActionContextActiveInfoNode`
- `SucceedNode`、`AndNode`、`NotNode`

节点即使在白名单内，仍必须通过目标 graph 的 `CanCreateNodeType`。

AIController节点catalog由正式`NodeAuthoringCapabilityPolicy`约束，包括SharedFlow/SharedPureValue/SharedBlackboard、AIObservation、AIMemory与AIIntent节点。CharacterExecution、Timeline、MotionWarp和Transform副作用节点禁止进入AI Graph。

## 代码所有权地图

根目录：

`3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/AgentAuthoring/`

| 文件 | 所有权 |
|---|---|
| `Mcp/ManageBtsmtlAgentAuthoringMcpTool.cs` | 唯一MCP薄桥；只解析action/domain/root_asset_path/patch_json |
| `AgentPatchAuthoringService.cs` | domain dispatch、Character/AI snapshot、preflight、Undo、apply、validate、rollback、dirty/save与正式产物发布 |
| `AgentPatchAuthoringModels.cs` | 四个 MCP action 的 request/response |
| `AgentAuthoringModels.cs` | schema version、Snapshot DTO、Intent、Patch DTO、Report |
| `AgentGraphSnapshotExporter.cs` | Character Definition/Graph/Timeline/Presentation的只读投影 |
| `AgentPatchCommandLowerer.cs` | 唯一 operation catalog、字段校验、DTO 到 typed command |
| `AgentPatchCommands.cs` | typed command、target reference、plan 和 output kind |
| `AgentPatchCompiler.cs` | Prepare/Apply facade，不保存跨调用状态 |
| `AgentPatchCompileSession.cs` | 单次 Definition、Index、Resolver、symbol、diff、touched owner |
| `AgentPatchCommandHandlers.cs` | domain handler catalog、AI typed handler与共享graph authoring utility |
| `AgentStateMachineCommandHandler.cs` | StateMachine/State/Transition/ConditionRule mutation |
| `AgentStateBehaviorCommandHandler.cs` | State body、Timeline、Activation、Lifecycle mutation |
| `AgentActionEligibilityCommandHandler.cs` | Blackboard、TreeClip、tag、ActionProfile policy 与 transition 删除 mutation |
| `AgentNodeAssetCommandHandler.cs` | Input node 与资产配置 |
| `AgentGraphLinkCommandHandler.cs` | flow/property edge mutation |
| `AgentNodeEmitterRegistry.cs` | Agent 可创建节点白名单与节点配置入口 |
| `AgentConditionRuleBuilder.cs` | Condition emitter、AND/OR/Result 连接 |
| `AgentAssetResolver.cs` | 当前 Definition/Snapshot 范围内的资产解析 |
| `AgentGraphAuthoringIndex.cs` | 当前正式 topology 的 stable identity 索引 |
| `AgentGraphTransactionOwnerCollector.cs` | Definition、RootTree、全部可达 Graph/Timeline serialized owner |
| `AgentGraphValidator.cs` | Character/AI authoring语义与正式只读compiler validation |
| `AgentMacroLibrary.cs` | v16明确拒绝带业务默认值的controller macro；业务迁移使用显式typed Patch |
| `AgentMacroCoverageEvaluator.cs` | 无业务样例；不进入通用 validate |

Current specs：

- `openspec/specs/agent-character-controller-synthesis/spec.md`
- `openspec/specs/agent-ai-controller-synthesis/spec.md`
- `openspec/specs/btsmtl-agent-authoring-mcp-bridge/spec.md`

## Patch 示例

删除 snapshot 已确认的 State body 空节点：

```json
{
  "schemaVersion": "agent-character-controller-synthesis.v16",
  "domain": "CharacterController",
  "rootIdentity": "<definition-asset-guid>",
  "sourceRevision": "<snapshot-source-revision>",
  "operations": [
    {
      "id": "delete-empty-exit-node",
      "op": "delete_state_behavior_node",
      "targetGraphAuthoringId": "<state-body-graph-authoring-id>",
      "targetElementAuthoringId": "<node-authoring-id>"
    }
  ]
}
```

配置已有 AnimationTrack 的 Marker Group：

```json
{
  "schemaVersion": "agent-character-controller-synthesis.v16",
  "domain": "CharacterController",
  "rootIdentity": "<definition-asset-guid>",
  "sourceRevision": "<snapshot-source-revision>",
  "operations": [
    {
      "id": "configure-marker-sync",
      "op": "configure_animation_track_marker_sync",
      "timelineAuthoringId": "<timeline-authoring-id>",
      "trackAuthoringId": "<animation-track-authoring-id>",
      "animationSyncMode": "MarkerGroup",
      "animationSyncGroupId": "Locomotion.Gait",
      "animationMarkerSequenceTopology": "Cyclic",
      "animationMarkerSyncRole": "CanBeLeader"
    }
  ]
}
```

原子替换一个Snapshot已登记的Timeline曲线channel：

```json
{
  "schemaVersion": "agent-character-controller-synthesis.v16",
  "domain": "CharacterController",
  "rootIdentity": "<definition-asset-guid>",
  "sourceRevision": "<snapshot-source-revision>",
  "operations": [
    {
      "id": "configure-animation-weight",
      "op": "configure_timeline_curve_channel",
      "timelineAuthoringId": "<timeline-authoring-id>",
      "trackAuthoringId": "<track-authoring-id>",
      "clipAuthoringId": "<clip-authoring-id>",
      "curveChannelId": "animation.weight",
      "curve": {
        "preWrapMode": "ClampForever",
        "postWrapMode": "ClampForever",
        "keys": [
          { "time": 0.0, "value": 1.0, "inTangent": 0.0, "outTangent": 0.0, "inWeight": 0.33333334, "outWeight": 0.33333334, "weightedMode": "None" },
          { "time": 1.0, "value": 1.0, "inTangent": 0.0, "outTangent": 0.0, "inWeight": 0.33333334, "outWeight": 0.33333334, "weightedMode": "None" }
        ]
      }
    }
  ]
}
```

必须先将这段 JSON 传给 `dry_run_patch`；预检成功后原样传给 `apply_patch`。不能根据名字删除，也不能在两次调用之间重新生成另一份 Patch。

## 影响扫描清单

修改以下区域时，即使用户没有主动提到 Agent，也要触发 Agent 同步审查：

- `Runtime/BTSMTL/TreeDesigner/` 的 Graph、Node、Edge、Port、StateMachine、ownership、identity。
- `Runtime/BTSMTL/Timeline/` 的 TimelineNode、TimelineData、Track、Clip、TreeClip、serialized owner/path。
- `Runtime/Character/Pipeline/` 的 Input、ActionProfile、ActionContext、Blackboard、authoring topology。
- `Runtime/Character/AI/`与`Editor/AI/`的Definition、Tree、Capability、Blackboard、Perception、Intent与Compiler。
- `Editor/CharacterSimulation/` 的 authoring discovery、semantic frontend、正式 compile 约束。
- `Editor/CharacterPipeline/AgentAuthoring/` 的任何文件。

完成时必须给出二选一证据：

1. Agent 代码和合同已随语义变化更新，并通过正式工具验证；或
2. 变化完全是 runtime 私有实现，不改变 snapshot、Patch 可写能力、identity、ownership 或 validator 规则，并列出核对过的消费点。
