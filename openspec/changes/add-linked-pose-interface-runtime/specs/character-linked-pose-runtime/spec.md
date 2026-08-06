## ADDED Requirements

### Requirement: Linked Pose 必须通过稳定 typed Interface 限制替换边界

系统 MUST 使用 `CharacterLinkedPoseInterfaceAsset` 定义唯一动态 Pose 调用合同。Interface MUST 拥有稳定 `LinkedPoseInterfaceId`、revision、signature hash 与有序 Entry；每个 Entry MUST 拥有稳定 `LinkedPoseEntryId` 及有序 typed input/output ports。Signature MUST 覆盖 port identity、direction、kind、required、order、Pose 空间、Goal ABI、Presentation Fact contract 与 execution contract。Call 与 Implementation MUST 精确匹配同一 Interface identity、revision 和 signature；系统 MUST 不按显示名、C# 类型名、资源路径或隐式 cast 接受实现。

#### Scenario: Implementation 使用旧接口签名

- **WHEN** Implementation 记录的 Interface identity 相同但 signature hash 已变化
- **THEN** Projection Build MUST 拒绝该 Implementation
- **AND** Runtime MUST 不尝试按端口名称兼容连接

#### Scenario: Interface 只有一个 Entry

- **WHEN** Interface 只声明一个 required Entry
- **THEN** 同一 `LinkedPoseCall` 机制 MUST 支持该单 Entry 动态替换
- **AND** 系统 MUST 不建立第二套 Linked Graph 运行时

### Requirement: Linked Implementation 必须完整实现 Interface 并只读取正式表现事实

`CharacterLinkedPoseImplementationAsset` MUST 拥有稳定 `LinkedPoseImplementationId`、revision 与 authoring content hash，并精确引用一个 Interface identity 与 signature。Implementation MUST 为全部 required Entry 绑定唯一 Pose Graph；Entry Graph MUST 通过 `GraphInput` 与 `GraphOutput` 完整实现 typed signature。Implementation MAY 使用静态 `PoseSubgraph`，但 MUST 不包含 `OutputPose`、`ActionPlaybackInput`、`AnimationSlot`、`PredictiveFootPlacement`、`FullBodyIK`、world query、Gameplay node、另一个 `LinkedPoseCall` 或第二 final writer。

Implementation MAY 读取 Interface signature 声明的不可变 `CharacterPresentationFactFrame` 合同，并 MUST 继续按稳定 FactId 与 kind 解析。它 MUST 不读取 Equipment 对象、Renderer、Transform、Gameplay Graph、mutable runtime 对象或未声明 Fact。

#### Scenario: Implementation 遗漏 required Entry

- **WHEN** Interface 要求 Pose 与 Hand Goals 两个 Entry，但 Implementation 只绑定 Pose Entry
- **THEN** Validator 与 Projection Build MUST 报告缺失 required Entry
- **AND** MUST 不以 root 旧分支、默认 Pose 或隐式 Goals 补齐

#### Scenario: Implementation 读取未声明事实

- **WHEN** Entry Graph 引用不属于 Interface Fact contract 的 FactId
- **THEN** Validator 与 Compiler MUST 拒绝该 Implementation
- **AND** Runtime MUST 不通过对象引用或反射补充该值

#### Scenario: Implementation 嵌套动态调用

- **WHEN** Entry Graph 包含另一个 `LinkedPoseCall`
- **THEN** Capability、Mutation 与 Compiler MUST 拒绝该节点上下文
- **AND** Runtime MUST 不创建递归 dispatch 或动态调用栈

### Requirement: Linked Pose 核心必须与业务选择来源解耦

Linked Pose runtime MUST 只消费包含 `GroupId`、`InterfaceId`、`ImplementationId` 与 `SelectionRevision` 的 `CharacterLinkedPoseSelectionFrame`。核心 MUST 不读取 `EquipmentSlotId`、`EquipmentId`、Vehicle 状态、Feature、Renderer、显示名、资源路径或其它业务对象。新的业务来源 MUST 通过实现统一 selector provider 合同产生相同 selection frame；系统 MUST 不使用中央 `SelectionSourceKind` 枚举或核心 switch 分派业务类型。

#### Scenario: 载具状态未来选择另一实现

- **WHEN** 一个载具 selector 为已声明 Group 产生合法 selection frame
- **THEN** Linked Pose runtime MUST 按通用 Group、Interface 与 Implementation 合同执行切换
- **AND** MUST 不要求修改核心以识别 Vehicle 类型

#### Scenario: selector 产生目录外实现

- **WHEN** selection frame 指向不属于当前 Group 候选闭包的 Implementation
- **THEN** Session admission 或 Prepare MUST 结构化失败
- **AND** Runtime MUST 不沿用旧实现或搜索资源路径

### Requirement: 每个 Linked Group 必须恰好装配一个 selector

`CharacterAnimationPresentationProfile` MUST 使用 `CharacterLinkedPoseGroupBinding` 声明稳定 `LinkedPoseGroupId` 与唯一 Interface。每个 Group MUST 在 Session 准备时恰好解析到一个 selector provider。候选 Implementation 集合 MUST 由该 selector 的精确映射推导；系统 MUST 不再维护可能与 selector 分裂的第二份通用候选列表。

#### Scenario: Group 没有 selector

- **WHEN** Profile 声明 Linked Group 但没有 selector binding 服务该 Group
- **THEN** Validator、Build 与 Session admission MUST 失败并定位 Group
- **AND** Runtime MUST 不选择默认 Implementation

#### Scenario: Group 有两个 selector

- **WHEN** 两个 selector binding 声明服务同一 Group
- **THEN** Validator 与 Build MUST 拒绝歧义装配
- **AND** Runtime MUST 不按声明顺序或优先级选择其中一个

### Requirement: Equipment selector 必须把 committed 装备状态精确映射为通用选择帧

`CharacterEquipmentLinkedPoseSelectionBinding` MUST 声明目标 Group、唯一 `EquipmentSlotId`、精确 `EquipmentId -> ImplementationId` 映射与显式 Empty Equipment -> Empty Implementation 映射。adapter MUST 只读取 committed Equipment selection，并输出通用 `CharacterLinkedPoseSelectionFrame`；Equipment Feature、Visual、Renderer 与 Implementation MUST 不反向驱动选择。

#### Scenario: Equipment 槽为空

- **WHEN** committed Equipment selection 表明指定 Slot 为空
- **THEN** Equipment selector MUST 选择显式配置的 Empty Implementation
- **AND** MUST 不把空槽当作缺失映射或 fallback

#### Scenario: EquipmentId 没有映射

- **WHEN** 当前角色可能提交的 EquipmentId 没有精确映射
- **THEN** Character Build 或 Session admission MUST 失败
- **AND** Runtime MUST 不沿用上一 Implementation、取第一个候选或使用默认资源

### Requirement: 同一 Group 必须共享选择事务但隔离 Entry 节点状态

同一 Group 的全部 Entry MUST 在同一帧共享 Implementation handle、selection revision、generation、Prepare/Seal/Discard 生命周期与 source demand 事务。每个 Entry MUST 拥有独立 operation range、workspace、Pose/Value page、StateMachine/player/inertial state、completion 与 diagnostics range。跨 Entry 共享数据 MUST 通过正式只读 Fact 或 typed port 表达，MUST 不通过隐藏 mutable state 引用表达。

#### Scenario: Pose 与 Hand Goals 位于同一 Group

- **WHEN** root 在一帧调用同一 Group 的 `EquipmentPose` 与 `EquipmentHandGoals`
- **THEN** 两个 Call MUST 读取同一 Implementation 与 generation
- **AND** 每个 Entry MUST 只推进自己的节点状态范围

#### Scenario: 一个 Entry 执行失败

- **WHEN** incoming Implementation 的任一 required Entry 在提交前失败
- **THEN** 整个 Group generation MUST 不得 Seal
- **AND** MUST 不提交新 Pose Entry 与旧 Hand Goals Entry 的混合组合

### Requirement: PoseSubgraph 与 LinkedPoseCall 必须保持不同编译语义

`PoseSubgraph` MUST 继续作为编译期作者宏递归展开，且其 `GraphInput`、`GraphOutput` 与 call frame MUST 从 generated plan 消失。`LinkedPoseCall` MUST 作为动态 dispatch operation 保留在 root Projection，保存 Group、Interface 与 Entry identity，并只能 dispatch 到当前 Session 锁定目录中相同 signature 的预编译 Entry fragment。Runtime MUST 不读取 authoring Graph、解释 node payload 或重连 root typed edge。

#### Scenario: root 同时使用静态与动态子图

- **WHEN** root 包含一个 `PoseSubgraph` 和一个 `LinkedPoseCall`
- **THEN** Compiler MUST 静态展开前者并只为后者生成 dispatch operation
- **AND** diagnostics MUST 能区分静态展开来源与动态 Implementation 来源

#### Scenario: Fragment 包含未知操作

- **WHEN** Implementation fragment 要求当前 runtime 不支持的 opcode
- **THEN** Projection Build 或 Session admission MUST 拒绝该 fragment
- **AND** Runtime MUST 不回读 Graph 并解释该节点

### Requirement: Linked 目录与 Entry fragment 必须编入唯一 Presentation Projection

Projection Compiler MUST 生成 Interface、Group、selector、Implementation、Entry Fragment 与 root Dispatch descriptors。每个 Implementation descriptor MUST 包含 Implementation identity/revision/content hash、Interface signature、Rig identity、Fact contract、source closure、operation/stage ranges、workspace/state layout 与 Runtime ABI。同一 Profile 全部候选 source MUST 进入唯一 dense source binding。

Linked authoring、目录、fragment、source closure 与布局变化 MUST 改变 `ProjectionRevision`，并 MUST 不改变 gameplay `ContractHash`。任何派生 catalog hash MUST 只作为 Projection 内部 stale detection 与 diagnostics，不得成为第二套 gameplay 或内容版本真相。

#### Scenario: Linked Graph 只改变表现拓扑

- **WHEN** Implementation Entry 的合法节点拓扑或动画 source binding 发生变化
- **THEN** 新 Build MUST 产生新的 `ProjectionRevision`
- **AND** gameplay `ContractHash` MUST 保持不变

#### Scenario: Implementation source 没有进入闭包

- **WHEN** Entry fragment 引用的 source binding 不在同一 Profile dense source 目录
- **THEN** Projection Build MUST 失败并定位 Implementation 与 Entry
- **AND** Runtime MUST 不按资源路径临时加载

### Requirement: Linked 候选必须预分配互不重叠的 typed owner 范围

Compiler MUST 为每个候选 Implementation 合计其全部 required Entry 的 operation workspace、Pose/Value pages、StateMachine/player/Motion Matching/inertialization/Root Orientation Warp 等正式 typed owner、source demand、completion 与 diagnostics 容量。全部候选 fragment MUST 在唯一 runtime 中获得互不重叠的正式 owner range，并继续使用各 owner 已有的 committed / pending frame transaction；系统 MUST 不新增通用 `byte[] node state`、影子状态页或第二 executor。Compiler MUST 同时为 Group 逐项派生候选最大容量作为 admission、预算与 diagnostics 度量。运行时切换 MUST 不扩容或分配托管状态容器。

#### Scenario: 候选实现需要更大 StateMachine 状态页

- **WHEN** 一个 Group 的第二个 Implementation 比当前实现需要更多 StateMachine、Motion Matching 或 inertialization 状态
- **THEN** Build MUST 把 Group 容量设为全部候选的合法最大值
- **AND** Runtime 切换 MUST 只重置预分配的目标 typed owner range

#### Scenario: incoming generation 被丢弃

- **WHEN** 新选择在 Evaluate Barrier 前验证失败
- **THEN** Runtime MUST 通过各正式 owner 的 Discard 丢弃 pending frame 中的本次重置
- **AND** committed handle 与 committed typed owner 状态 MUST 保持不变

### Requirement: Linked Entry 必须参与唯一 Pose 事务与唯一 source backend

root stages 与当前选择的 Entry fragment stages MUST 共同执行同一 `Prepare -> Validate -> Animancer Evaluate Barrier -> Seal`。每个 source 每帧 MUST 最多 capture 一次，PlayableGraph MUST 最多 Evaluate 一次，Physical Transform MUST 只由唯一 final writer 写一次。Implementation MUST 不创建第二 executor、第二 PlayableGraph、第二 source backend 或第二 final writer；其 source demand、readiness、capture、retirement permission 与 physical release MUST 复用正式协议。

#### Scenario: Pose 与 Hand Goals 使用同一动画 source

- **WHEN** 同一 Implementation 的两个 Entry 引用相同 source binding
- **THEN** source backend MUST 合并同帧 demand 并最多 capture 一次
- **AND** 两个 Entry MUST 消费同一 Evaluate 产生的合法 sample

#### Scenario: Linked Entry 在 Barrier 后失败

- **WHEN** 已跨 Animancer Evaluate Barrier 后 Entry stage 或 lineage 验证失败
- **THEN** 后续 stage 与 FinalPublication MUST 被阻止且 Actor Animation Runtime MUST 进入 Faulted
- **AND** MUST 不逆序恢复 source time、node state 或 Physical Bone 快照

### Requirement: Implementation 切换必须使用显式 generation 事务

Runtime MUST 在跨越 Evaluate Barrier 前解析 selection frame 并校验 Interface、Implementation、Rig、Projection ABI、Fact contract 与 source readiness。新 Implementation MUST 在预分配 incoming page 按规范默认值初始化全部 required Entry；不同 Implementation 之间 MUST 不迁移 StateMachine、player time、Motion Matching history 或 inertial state。Seal 成功后 MUST 原子提交 handle 与 generation，并通过现有延迟释放协议回收旧 source。切换 MUST 发布 `PoseDiscontinuity`，连续性 MUST 只由图中显式 Inertialization 或 Blend 处理。

#### Scenario: 武器选择在 Prepare 阶段变化

- **WHEN** Equipment selector 将 Group 从 Rifle Implementation 切到 Pistol Implementation 且 incoming 资源 Ready
- **THEN** 同 Group 全部 Call MUST 在本帧使用同一 incoming generation
- **AND** Seal 后才可提交新 handle 并退休旧 source

#### Scenario: root 需要平滑切换

- **WHEN** 作者要求 Implementation 切换具有视觉连续性
- **THEN** root MUST 在 Linked Call 边界后显式配置 Inertialization 或 Blend
- **AND** Runtime MUST 不注入隐藏 crossfade

### Requirement: 第一版 Linked 目录必须在 Session 前完整准备

当前 Gameplay Session 的 root Projection、Interface、Group、selector、全部候选 Implementation fragments 与 source closure MUST 在 Session 创建前验证并锁定。运行时状态切换 MUST 不触发 Addressables、YooAsset、Unity 资产异步查找、下载或 runtime compile，也 MUST 不存在等待资源时的临时 Implementation。未知、未编译或未准备候选 MUST 在 Build 或 Session admission 阶段失败。

#### Scenario: Equipment selector 映射四个候选实现

- **WHEN** Session 准备使用包含四个精确 Equipment 映射的 Projection
- **THEN** admission MUST 验证四个 Implementation 及其 source closure
- **AND** 对局内切换 MUST 不发起内容加载或走 fallback

### Requirement: Linked Pose 调试必须暴露选择与 fragment 真实身份

Preview、Live Debug、Pose Watch 与 Trace MUST 复用正式 selector adapter 与 Projection，并显示 Group、Interface/signature、Entry、Implementation/content hash、selector identity、selection revision、generation、switch/reset/failure、stage 贡献与 source 生命周期。Preview fixture MUST 提供 selector 所需的业务状态，不得直接指定 Implementation 资源路径。Pose Watch MUST 按 Call、Entry、Implementation 与 generation 观察已完成值，MUST 不重新执行候选 fragment。

#### Scenario: Preview 切换武器

- **WHEN** 作者在 Preview fixture 把 committed EquipmentId 从 Empty 改为 Rifle
- **THEN** Preview MUST 经过 Equipment selector 产生通用 selection frame 并执行 generation 切换
- **AND** diagnostics MUST 显示对应 Implementation 与 `PoseDiscontinuity`
