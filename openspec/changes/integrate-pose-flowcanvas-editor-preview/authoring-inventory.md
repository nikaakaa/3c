# Pose作者基线与UE式组织去向

本文件保留2026-09-08对精确Corin作者资产的只读盘点，并列出新方案中的去向。它不是新作者模型的完成清单；新模型以design.md和tasks.md为准。本轮只整理文档，没有重新采集Unity布局或修改作者资产。原始盘点为`.codex-tmp/canvas-core/pose-flowcanvas-author-inventory.json`。

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

## 已有数据链（待重组基线）

正式资产仍保存 `CharacterPoseCanvasGraph / CharacterPoseCanvasNode / CharacterPoseCanvasConnection`，旧阶段只改变原生编辑基类，保留GraphCatalog、Payload、资源引用和identity。新方案将进一步修改作者角色、引用和节点职责；可保留的GraphId、NodeId、StateId沿用，内部化节点的旧作者identity退役并记录对应关系。

端口从 `CharacterPoseGraphAuthoringCapabilities` 经 `CharacterPoseAuthoringPortProjection.Get` 投影到原生端口。Compiler 仍直接读取同一 Graph、Node、Payload；`CharacterPoseFamilyPayloadPlanPass` 生成正式 Program Image，Native/Job 继续执行。

Unity作者资产是唯一真相。状态机和规则通过Document投影到不保存的原生FlowGraph页面，Document工作包不是另一份运行图。新图角色继续遵守这一数据边界。

## 已有编辑器改动（可复用基础）

- 原生 Graph、FlowNode、BinderConnection 负责显示和交互；领域 Mutation 负责合法性、owner、Undo 与保存。
- 删除旧 `CharacterPoseAuthoringBottomDock` 的独立播放器、假输入、Seek 和预览角色面板；保留只读面板基类和正式作者调参服务。
- 删除 `CharacterPoseCanvasPortsGUI`，保留命令错误显示和状态标记的小型 `CharacterPoseCanvasInteraction`。
- 纯 Capability 定义移动到 Runtime 合同层，Editor lowering 注册留在 Editor。
- `CharacterAnimationPreviewFixtureSession` 仍被正式 `CharacterPoseResetObservationMcpTool` 使用，本次不删除其它诊断消费者；PoseGraph 窗口已无该依赖。

## 新组织中的迁移边界

旧阶段只换编辑基类，没有必要迁移作者数据。2026-09-09的新方案改变图职责、Player资源来源、Slot、Curve处理和Control Rig接口，因此需要在代码完成后进行真实作者迁移；不能把旧阶段结论延续成“现在只需Build”。

有限Action Timeline继续拥有原动画片段、窗口和动作时间，在原资产中补Slot轨道、Sections及动画混合设置；不会迁出或复制为另一份Montage资产。Foot／目标／FBIK进入Control Rig，Goal Assembler与默认参数汇总改为编译展开。资源绑定、类型和old kind按正式事务收敛，成功后删除无消费者数据。

当前代码的Pose Program Image为v28，Document为v6；新方案计划统一升级Document v7及所需产物schema，当前还没有完成对应迁移或发布。实际范围只包括Corin及其明确引用owner，不包括TrainingEnemy。

## 旧能力及新方案去向

下表是旧阶段登记的29种能力，新增一列描述新方案去向。能力数量不作为新范围硬编码限制，也不代表每种新作者行为已经实现。

| 旧能力 | 现有Payload | 新方案去向 |
|---|---|---|
| `ProgramParameterInput` | `CharacterProgramParameterInputPosePayload` | 保留有业务名称的typed变量入口，按需要驱动Alpha等字段 |
| `ActionPlaybackInput` | `CharacterActionPlaybackInputPosePayload` | 删除作者节点，归入Slot的编译展开 |
| `SelectedPosePlayer` | `CharacterSelectedPosePlayerPayload` | 保留选择播放业务，资源与usage绑定归入新Player合同 |
| `BlendSpacePlayer` | `CharacterBlendSpacePlayerPosePayload` | 直接引用Blend Space或typed资源参数 |
| `ClipPlayer` | `CharacterClipPlayerPosePayload` | Sequence Player作者表面，直接引用原生AnimationClip |
| `PoseStateMachine` | `CharacterPoseStateMachineNodePayload` | 用于AnimGraph或动画层，状态／规则各自下钻 |
| `AnimationSlot` | `CharacterAnimationSlotPosePayload` | 根图、层或State Pose使用，消费原Timeline的播放结果 |
| `BlendStack` | `CharacterBlendStackPosePayload` | 保留明确的局部混合历史能力，不恢复固定每Slot私有Stack |
| `Inertialization` | `CharacterInertializationPosePayload` | 保留显式节点，扩展UE式上游请求处理 |
| `BlendPose` | `CharacterBlendPosePayload` | 普通姿势混合并拥有相应Curve设置 |
| `LayeredBoneBlend` | `CharacterLayeredBoneBlendPosePayload` | Layered Blend Per Bone，明确Mask／Branch Filter和Alpha |
| `AdditivePose` | `CharacterAdditivePosePayload` | 保留明确的Additive姿势组合 |
| `PoseParameterResolve` | `CharacterPoseParameterResolvePayload` | 删除必接作者节点，迁入实际组合节点的Curve策略 |
| `ModifyBone` | `CharacterModifyBonePosePayload` | 按空间合同作为骨骼控制或Rig步骤使用 |
| `RootOrientationWarp` | `CharacterRootOrientationWarpPosePayload` | 保留既有控制业务和算法，归入对应控制表面 |
| `FootPlacement` | `CharacterFootPlacementPosePayload` | Control Rig中的脚部目标来源 |
| `PoseBoneIKGoals` | `CharacterPoseBoneIkGoalsPayload` | Control Rig中的明确骨骼／Effector目标输入 |
| `FullBodyIkGoalAssembler` | `CharacterFullBodyIkGoalAssemblerPayload` | 删除作者节点，保留内部typed组装operation |
| `FullBodyIK` | `CharacterFullBodyIkPosePayload` | Control Rig中的FBIK作者节点，复用现有求解后端 |
| `LocalToComponentPose` | `CharacterLocalToComponentPosePayload` | 保留显式空间转换，并支持声明接口边界的编译转换 |
| `ComponentToLocalPose` | `CharacterComponentToLocalPosePayload` | 保留显式空间转换，并支持声明接口边界的编译转换 |
| `LinkedPoseCall` | `CharacterLinkedPoseCallPayload` | 复用为Animation Layer接口与调用基础 |
| `PoseSubgraph` | `CharacterPoseSubgraphPayload` | 保留一般图复用，明确接口与调用作用域 |
| `GraphInput` | `CharacterGraphInputPosePayload` | 统一图、层及控制图的typed输入 |
| `GraphOutput` | `CharacterGraphOutputPosePayload` | 统一子图返回，不增加Physical Writer |
| `MotionMatchingPose` | `CharacterMotionMatchingPosePayload` | 保留已有Motion Matching业务，接入新资源／调用合同 |
| `PoseHistoryCollector` | `CharacterPoseHistoryCollectorPayload` | 保留明确的Pose历史用途及唯一状态owner |
| `EntryPoseInput` | `CharacterEntryPoseInputPayload` | 归入对应层／控制图的显式Pose输入 |
| `OutputPose` | `CharacterOutputPosePayload` | 根图最终输出或相应图返回，仅最终根进入Publication |

历史Document盘点为1个Pose状态机、7个状态、21条转换。其零差异往返记录仅对应当时作者格式，见implementation.md；本次没有执行新组织的作者迁移。
