## MODIFIED Requirements

### Requirement: Equipment Presentation 不得拥有动画空间拓扑

Equipment Feature authoring MUST 不保存 LayerId、BlendMode、OutputPolicy、LinkedPoseInterfaceId、LinkedPoseImplementationId 或 Presentation producer requirement。`EquipmentFeatureRouteImplementation.RequiredProducerIds` MUST 仅表达 Gameplay route 完整性，MUST 不进入 Presentation Projection 的 channel、Player、transition、Linked Group、selector 或 Animancer resolution。Equipment Presentation Profile 与 Projection MUST 只保存 VisualBinding、Prefab/socket、Renderer 登记与 local pose 资源绑定，MUST 不复制 ActionPlaybackInput、AnimationSlot、Pose source、PoseNode、Linked Group 或 selector 字段。

动态 Equipment Pose 选择 MUST 只由 `CharacterAnimationPresentationProfile` 中的 `CharacterEquipmentLinkedPoseSelectionBinding` 表达。Equipment 资产只通过 committed `EquipmentSlotId + EquipmentId` 事实被 adapter 读取，MUST 不直接引用 Linked Implementation。

#### Scenario: Equipment Feature 声明 Gameplay route producer

- **WHEN** Equipment route 使用 RequiredProducerIds 校验 Gameplay Graph 实现完整性
- **THEN** Semantic/Gameplay compiler MAY 保留该纯 route 依赖
- **AND** Projection Compiler MUST 不把它解释为 AnimationChannel、PoseNode、Linked Implementation 或表现层 producer binding

#### Scenario: 武器需要动态替换 Pose 实现

- **WHEN** 武器业务需要替换 Interface 规定的持续 Pose 与 Hand Goals 实现
- **THEN** Animation Presentation Profile MUST 用 Equipment selector 的精确映射产生通用 selection frame
- **AND** Equipment Visual 链 MUST 不提供 passthrough、兼容 Layer、临时 Player 或 Graph 引用

#### Scenario: Equipment Profile 尝试引用 Linked 实现

- **WHEN** 作者在 Equipment Feature 或 Equipment Presentation Profile 写入 Linked Implementation 引用
- **THEN** Capability 与 Validator MUST 拒绝该字段
- **AND** MUST 要求在 Animation Presentation Profile 配置 selector binding

## ADDED Requirements

### Requirement: Linked Pose 作者资产必须保持 Interface、Implementation、Group 与 selector 分工

工程师拥有的 `CharacterLinkedPoseInterfaceAsset` MUST 只定义稳定 Entry、typed signature 与 Fact contract；`CharacterLinkedPoseImplementationAsset` MUST 拥有 required Entry Graph 与独立 authoring identity；`CharacterAnimationPresentationProfile` MUST 唯一拥有 Group binding、selector binding 与全部 source binding。Group binding MUST 只保存 Group 与 Interface；业务映射 MUST 只存在于服务该 Group 的 selector binding。Profile Inspector 与 Navigator MUST 按 Group、Interface、selector、Implementation 与 Entry 组织作者入口，并显示精确 identity、signature、候选闭包与编译状态。

#### Scenario: 作者新增步枪实现

- **WHEN** 作者为既有 Equipment Interface 创建 Rifle Implementation
- **THEN** Implementation MUST 完整绑定 required Entry Graph
- **AND** Equipment selector MUST 用正式 EquipmentId 显式映射该实现

#### Scenario: 作者重复维护候选目录

- **WHEN** Group binding 或其它 Profile 字段尝试另存一份 Implementation candidates
- **THEN** authoring schema 与 Validator MUST 拒绝该重复配置
- **AND** Build MUST 只从唯一 selector 映射推导候选闭包

### Requirement: Linked Pose 变更必须继续使用显式 Build

编辑 Interface、Implementation、Group、selector 或 Entry Graph MUST 只标记相关 authoring 与 generated Projection stale。Inspector 绘制、资产选择、Document 文件变化与 Play Mode 进入 MUST 不自动 Build 或 Apply。作者 MUST 通过现有显式 Character Build 入口原子生成 Projection、Native Pose Program 与 generated references。

#### Scenario: 作者只移动 Entry Graph 节点

- **WHEN** 作者只修改 layout view-state
- **THEN** 系统 MUST 不改变 Implementation content revision 或 Projection
- **AND** MUST 不触发自动 Build

#### Scenario: 作者修改 Equipment 映射

- **WHEN** selector binding 把一个 EquipmentId 改映射到另一 Implementation
- **THEN** Profile 与 Projection MUST 标记 stale
- **AND** 活动 runtime MUST 不直接读取尚未 Build 的 authoring 变化
