# Pose 原生编辑接入盘点

读取范围为精确 Corin Pose 作者资产；本清单读取正式 YAML，不修改它。节点、布局及资源引用的完整读取记录在 `.codex-tmp/canvas-core/pose-flowcanvas-author-inventory.json`。

| 图 | 节点数 | 连线数 |
|---|---:|---:|
| `corin.locomotion.turn.graph` | 2 | 1 |
| `239fb85af79509b36208db12ad91354f` | 2 | 1 |
| `corin.locomotion.start.graph` | 2 | 1 |
| `corin.locomotion.idle.graph` | 2 | 1 |
| `ed8ff472330e4057a900af3eae5dfb8f` | 11 | 12 |
| `658ab38fd39b80592ae222925f31e25c` | 2 | 1 |
| `corin.locomotion.locomotion.graph` | 2 | 1 |
| `corin.locomotion.stop.graph` | 2 | 1 |

## 唯一数据链

正式资产仍保存 `CharacterPoseCanvasGraph / CharacterPoseCanvasNode / CharacterPoseCanvasConnection`，只改变其原生编辑基类，不新增一份可编辑拓扑，不重分配 GraphId、NodeId、EdgeId。GraphCatalog、Payload、资源引用和布局保留。

端口从 `CharacterPoseGraphAuthoringCapabilities` 经 `CharacterPoseAuthoringPortProjection.Get` 投影到原生端口。Compiler 仍直接读取同一 Graph、Node、Payload；`CharacterPoseFamilyPayloadPlanPass` 生成正式 Program Image，Native/Job 继续执行。

状态机和规则仍以现有 Document 为作者真相，`CharacterPoseDocumentCanvas` 为不保存的原生 FlowGraph 页面。它不保存第二份业务图；原来的自绘端口和拖线代码已删除。

## 修改和删除

- 原生 Graph、FlowNode、BinderConnection 负责显示和交互；领域 Mutation 负责合法性、owner、Undo 与保存。
- 删除旧 `CharacterPoseAuthoringBottomDock` 的独立播放器、假输入、Seek 和预览角色面板；保留只读面板基类和正式作者调参服务。
- 删除 `CharacterPoseCanvasPortsGUI`，保留命令错误显示和状态标记的小型 `CharacterPoseCanvasInteraction`。
- 纯 Capability 定义移动到 Runtime 合同层，Editor lowering 注册留在 Editor。
- `CharacterAnimationPreviewFixtureSession` 仍被正式 `CharacterPoseResetObservationMcpTool` 使用，本次不删除其它诊断消费者；PoseGraph 窗口已无该依赖。

## 迁移边界

本批没有主动改写 Pose 作者节点、连接或布局；类型名和稳定identity保持不变，不需要作者数据迁移，不执行无业务变化的apply或保存。剩余工作为精确Corin Definition的正式产物重建。

编译产物新增作者节点、输出端口、各图版本和转换条件读取来源，最新源码使用 Pose Plan v28。此前的 v27 Build属于历史产物；v28发布放在代码完成后的资产阶段。

## 正式能力对应

全部使用同一 Port Shape、同一原生注册器和各自已注册的 lowering，不给每个节点新增独立 UI 类型。下表为当前代码注册项；不是迁移数量限制。

| 能力 | Payload |
|---|---|
| `ProgramParameterInput` | `CharacterProgramParameterInputPosePayload` |
| `ActionPlaybackInput` | `CharacterActionPlaybackInputPosePayload` |
| `SelectedPosePlayer` | `CharacterSelectedPosePlayerPayload` |
| `BlendSpacePlayer` | `CharacterBlendSpacePlayerPosePayload` |
| `ClipPlayer` | `CharacterClipPlayerPosePayload` |
| `PoseStateMachine` | `CharacterPoseStateMachineNodePayload` |
| `AnimationSlot` | `CharacterAnimationSlotPosePayload` |
| `BlendStack` | `CharacterBlendStackPosePayload` |
| `Inertialization` | `CharacterInertializationPosePayload` |
| `BlendPose` | `CharacterBlendPosePayload` |
| `LayeredBoneBlend` | `CharacterLayeredBoneBlendPosePayload` |
| `AdditivePose` | `CharacterAdditivePosePayload` |
| `PoseParameterResolve` | `CharacterPoseParameterResolvePayload` |
| `ModifyBone` | `CharacterModifyBonePosePayload` |
| `RootOrientationWarp` | `CharacterRootOrientationWarpPosePayload` |
| `FootPlacement` | `CharacterFootPlacementPosePayload` |
| `PoseBoneIKGoals` | `CharacterPoseBoneIkGoalsPayload` |
| `FullBodyIkGoalAssembler` | `CharacterFullBodyIkGoalAssemblerPayload` |
| `FullBodyIK` | `CharacterFullBodyIkPosePayload` |
| `LocalToComponentPose` | `CharacterLocalToComponentPosePayload` |
| `ComponentToLocalPose` | `CharacterComponentToLocalPosePayload` |
| `LinkedPoseCall` | `CharacterLinkedPoseCallPayload` |
| `PoseSubgraph` | `CharacterPoseSubgraphPayload` |
| `GraphInput` | `CharacterGraphInputPosePayload` |
| `GraphOutput` | `CharacterGraphOutputPosePayload` |
| `MotionMatchingPose` | `CharacterMotionMatchingPosePayload` |
| `PoseHistoryCollector` | `CharacterPoseHistoryCollectorPayload` |
| `EntryPoseInput` | `CharacterEntryPoseInputPayload` |
| `OutputPose` | `CharacterOutputPosePayload` |

正式 Document 重读：1 个 Pose 状态机，7 个状态、21 条转移；context 的 node-catalog、graph-kinds、asset-catalog、dependencies 均已读取。checkout 与 dry-run 为 Clean、差异 0，见实施记录。
