# Change: 新增状态驱动的受限 Linked Pose 接口运行时

## Why

当前 Pose Graph 已经能用 `PoseSubgraph` 拆分作者资产，但 Compiler 会把它静态展开进一份扁平 Pose Plan。它适合复用，不适合在角色状态变化时把同一稳定调用点切到另一套持续动画逻辑。把所有装备、载具、受伤形态分支堆进 root graph 会让根图不断膨胀；替换整个 `CharacterAnimationPresentationProfile` 或整张 Pose Graph 又会连同 Locomotion、Action Slot、Foot Placement、FullBodyIK、节点状态和调试身份一起更换，边界过大。

UE 的 Linked Anim Graph / Linked Anim Layer 采用的是受限替换：宿主和候选实现都预先编译，接口固定入口与类型，运行时只选择兼容实现。项目需要同样的边界，但仍必须保持一份 Presentation Projection、一个 source backend、一次 Animancer Evaluate Barrier 和一个 final writer。

这不是武器专用系统。核心只接受 `GroupId + InterfaceId + ImplementationId + SelectionRevision`，不知道 Equipment、Vehicle 或 Gameplay State。武器只是第一种正式 selector：它把已提交的 Equipment 状态精确映射成通用选择帧。以后新增载具、形态或受伤状态时，应新增 selector，而不是修改 Linked Pose 核心或增加中央 `SelectionSourceKind` 分支。

## What Changes

- 新增工程师拥有的 `CharacterLinkedPoseInterfaceAsset`：
  - 使用稳定 `LinkedPoseInterfaceId`、revision 与 signature hash。
  - 保存有序 Entry；每个 Entry 具有稳定 `LinkedPoseEntryId` 与精确 typed 输入输出。
  - 签名同时固定 Pose 空间、Goal ABI 与只读 Presentation Fact 合同。
- 新增 `CharacterLinkedPoseImplementationAsset`：
  - 使用稳定 `LinkedPoseImplementationId`、revision 与 authoring content hash。
  - 精确实现一个 Interface 的全部 required Entry。
  - 每个 Entry 仍使用现有 Pose Graph、Capability、Compiler 与 source binding，不建立第二套图系统。
- 在 `CharacterAnimationPresentationProfile` 新增通用 `CharacterLinkedPoseGroupBinding`：
  - 只保存稳定 `LinkedPoseGroupId` 与 Interface。
  - 候选 Implementation 由该 Group 唯一 selector 的精确映射产生，避免维护第二份候选目录。
  - Session 准备时每个 Group 必须恰好解析出一个 selector；缺失或重复直接失败。
- 新增通用 `CharacterLinkedPoseSelectionFrame`：
  - 只携带 Group、Interface、Implementation 与 SelectionRevision。
  - Linked Pose runtime 只消费该帧，不读取任何业务对象或业务枚举。
- 新增第一种正式 selector：`CharacterEquipmentLinkedPoseSelectionBinding`：
  - 位于 Animation Presentation Profile。
  - 把 committed `EquipmentSlotId + EquipmentId` 精确映射到 Implementation。
  - 空槽必须显式映射到 Empty Implementation；未知 Equipment 或缺失映射不是 fallback。
- Pose Graph 新增唯一动态调用节点 `LinkedPoseCall`：
  - 节点保存 Group、Interface 与 Entry 身份，端口由 Interface 精确投影。
  - 第一版同一 `Group + Entry` 在 root graph 中必须恰好出现一次；缺失 required Entry 或重复调用均拒绝 Build。
  - `PoseSubgraph` 继续静态展开，不改变现有语义。
- 第一份 Equipment Interface 固定两个入口：
  - `EquipmentPose`：`Local Pose -> Local Pose`，负责装备持续姿态与移动表现。
  - `EquipmentHandGoals`：`Component Pose -> component.full-body-ik-goals`，只生成手部 Goals。
  - Empty Implementation 必须通过正式空 Goals 操作发布 `Ready + GoalCount=0`，不能复用上一帧 Goals。
- root graph 继续唯一拥有 Locomotion、Action Slot、Pose 空间转换、Predictive Foot Placement、FullBodyIK、OutputPose 与 final publication；Implementation 不得拥有这些不可替换边界。
- Projection Compiler 将 Implementation Entry 编译为不可变 fragment；全部候选在唯一 runtime 的正式 typed owner 中预分配互不重叠范围，并为每个 Group 派生候选最大容量与活动实现成本诊断。
- Runtime 在 Prepare 阶段原子解析并切换 Implementation；不同实现之间不迁移 StateMachine、player time、Motion Matching history 或 inertial state，切换显式发布 `PoseDiscontinuity`。
- 当前版本把全部候选 Implementation 与 source closure 编入当前 Projection，并在 Session 创建前完整准备。此 change 不实现下载内容、新 Session 内容激活、运行中热更、Graph 解释器或未知 opcode 注入。
- Interface、Implementation、Group、selector、Call、Preview 与诊断完整接入 Document v3、Capability Catalog、typed Mutation、Validator 与 MCP 现有五个生命周期工具，不新增旁路。

## Impact

- Affected specs:
  - `character-linked-pose-runtime`
  - `character-presentation-pose-graph`
  - `character-animation-pipeline`
  - `character-animation-layer-runtime`
  - `character-animation-presentation-authoring`
  - `character-equipment-presentation`
  - `btsmtl-compiled-simulation-program`
  - `graph-authoring-domain-framework`
  - `btsmtl-agent-authoring-document-sync`
  - `btsmtl-agent-authoring-mcp-bridge`
  - `agent-character-controller-synthesis`
- Affected code:
  - Animation Presentation authoring assets、Document codec、Mutation、Validator 与 MCP bridge。
  - Pose Graph Capability、Compiler、generated Projection 与 Native Pose runtime。
  - Equipment 到 Presentation 的只读 selection adapter。
  - Preview、Live Debug、Pose Watch 与 Trace。
- Dependencies:
  - 必须沿用 `replace-pose-ik-with-finalik-full-body-solver` 最终确定的 Rig v4、GoalSet ABI、唯一 FullBodyIK 与 FinalIK generated projection 合同。
  - Linked Implementation 不复制 Motion Matching 或 Blend Space 节点实现；它只复用实施时已经进入正式 Capability/Compiler/runtime 的节点能力。本 change 的首个 Equipment 实现不依赖仍未闭合的实验节点。
- Compatibility:
  - 这是新的 Presentation Projection ABI；旧 Projection 不兼容，不提供 fallback 或双轨执行。
  - Linked authoring 内容只改变 `ProjectionRevision` 与 Presentation 产物身份，不改变 gameplay `ContractHash`。
  - 现有静态 `PoseSubgraph` 和 root graph 保持原语义；实际业务迁移时必须一次删除 root 中被替代的持续状态分支。

## Non-Goals

- 不把 Idle、Run 等普通细状态都拆成 Linked Implementation；它们继续由选中实现内部的 `PoseStateMachine` 管理。
- 不允许任意 Graph、任意节点或任意 C# 类型在运行时插拔。
- 不实现 Content 下载、YooAsset / Addressables 异步装载、活动 Session 热更、跨版本状态迁移或脚本 Pose VM。
- 不新增 `SelectionSourceKind` 中央枚举，也不让 Linked Pose 核心认识 Equipment、Vehicle 或其它业务状态。
- 不迁移 Corin 当前锯刃 Gameplay 资产的正式 EquipmentId；在独立 Equipment 迁移完成前，不得用临时字符串、Renderer 状态或默认实现冒充业务接线。
