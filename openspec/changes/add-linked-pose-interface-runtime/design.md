## Context

当前正式 Presentation 链只有一份 root Pose Graph、一份编译 Projection、一套 source backend、一次 Animancer Evaluate Barrier 和一个 final writer。`PoseSubgraph` 是作者宏，Compiler 会把它递归展开，不会留下运行时调用边界。现有 Equipment Runtime 能提交稳定装备状态，但 Presentation 还不能在不替换整张根图的前提下，把一个稳定调用点切到另一套持续 Pose 逻辑。

需要新增的不是任意 Graph 热插拔，而是一个有限动态分发机制：宿主固定拓扑，Interface 固定 ABI，候选 Implementation 全部预编译，业务 selector 只提交 Implementation 身份，运行时在 Prepare 阶段切换已准备 fragment。

## Goals / Non-Goals

### Goals

- 提供状态来源无关的 Linked Pose 核心。
- 让一个 Group 的多个 Entry 原子选择同一 Implementation 与 generation。
- 让装备成为第一种正式 selector，同时允许未来通过新增 selector 接入载具、形态或受伤状态。
- 保持唯一 Pose Plan 事务、source backend、Evaluate Barrier 与 final writer。
- 复用现有 Pose Graph、Fact、Capability、Compiler、Document、Preview 与诊断体系。
- 首版完整编译和准备全部候选，不产生运行中等待与 fallback。

### Non-Goals

- 不允许运行时装入未知 Graph、未知 opcode 或任意 C# 执行代码。
- 不实现下载内容、活动 Session 热更或跨版本节点状态迁移。
- 不把普通 Locomotion 状态全部拆成 Linked Implementation。
- 不允许 Implementation 拥有 Action Slot、world query、Foot Placement、FullBodyIK、OutputPose 或 final writer。
- 不恢复旧 Layer catalog、Animancer layer 顺序或第二套 PlayableGraph。
- 不负责定义 Corin 锯刃的正式 Equipment 业务身份。

## Decisions

### 1. 核心只消费通用选择帧

Linked Pose runtime 的输入固定为：

```text
CharacterLinkedPoseSelectionFrame
  GroupId
  InterfaceId
  ImplementationId
  SelectionRevision
```

核心不得读取 `EquipmentSlotId`、`EquipmentId`、Vehicle 状态、Renderer、Feature、显示名或资源路径。它只验证 Group、Interface 与 Implementation 是否属于当前 Projection，并比较 SelectionRevision 决定是否开始新 generation。

业务状态通过 selector 转为该帧。第一版提供 `CharacterEquipmentLinkedPoseSelectionBinding`，以后载具或形态通过新的 binding/provider 产生相同帧。不得新增中央 `SelectionSourceKind` 和大 switch，因为那会让每种新业务都修改 Linked Pose 核心。

业务取舍：通用核心多了一层明确映射，但它把“为什么选这个实现”和“怎么执行这个实现”分开。业务扩展只新增 selector，底层执行、内存和事务不随业务数量膨胀。

### 2. 每个 Group 必须恰好有一个 selector

`CharacterLinkedPoseGroupBinding` 只保存：

- `LinkedPoseGroupId`
- Interface 引用

具体 selector binding 另行声明自己服务的 Group。Session 准备必须对每个 Group 解析到恰好一个 selector。没有 selector、多个 selector、selector 指向别的 Interface 或产生未进入当前目录的 Implementation，都属于结构错误。

Group 的候选 Implementation 集合由 selector 的精确映射推导，不再同时维护一份通用 candidates 列表。这样 Build、Session admission 与运行时选择使用同一数据源。

业务取舍：显式候选目录便于浏览，却会与 selector 映射产生重复配置。由 selector 映射推导候选能避免“允许切换”和“实际可选”两套真相。

### 3. Equipment 是第一种 selector，不是核心特例

`CharacterEquipmentLinkedPoseSelectionBinding` 位于 `CharacterAnimationPresentationProfile`，声明：

- 目标 Group
- 唯一 `EquipmentSlotId`
- 精确 `EquipmentId -> LinkedPoseImplementationId` 映射
- 显式 Empty Equipment -> Empty Implementation 映射

adapter 只读取 committed Equipment selection，并发布通用 selection frame。Equipment Feature、Renderer、Visual Profile 与 Implementation 不得反向选择动画实现。

空槽是正式业务状态，对应正式 Empty Implementation。未知 Equipment、缺失映射或重复映射直接使 Character Build 或 Session 准备失败；运行时不得沿用上一实现、取第一个候选或使用默认资源。

### 4. Interface 是唯一动态 ABI

`CharacterLinkedPoseInterfaceAsset` 保存：

- `LinkedPoseInterfaceId`
- revision
- signature hash
- 有序 Entry 集合
- 每个 Entry 的稳定 `LinkedPoseEntryId`
- 有序 typed input/output port descriptors
- Presentation Fact contract identity
- Pose runtime execution contract version

signature 覆盖端口 identity、方向、kind、required、顺序、Pose 空间、Goal ABI、Fact contract 与执行合同。Call、Implementation 与 Projection 必须记录相同 Interface identity 与 signature。任何一项变化都会形成新签名，不进行名称兼容或隐式 cast。

Interface 可以只有一个 Entry，也可以有多个 Entry。单 Entry 相当于受限 Linked Anim Graph；多 Entry 相当于一组 Linked Anim Layer 入口。两者共享一套资产、编译和运行时模型。

### 5. Implementation 完整实现 Interface

`CharacterLinkedPoseImplementationAsset` 保存：

- `LinkedPoseImplementationId`
- revision
- authoring content hash
- Interface identity 与 signature
- 全部 required Entry 到 Graph identity 的一一映射

每个 Entry Graph 使用现有 `GraphInput` / `GraphOutput` 实现签名，可以包含静态 `PoseSubgraph`。Compiler 必须拒绝遗漏、重复或多余 Entry，也必须拒绝 `OutputPose`、`ActionPlaybackInput`、`AnimationSlot`、`PredictiveFootPlacement`、`FullBodyIK`、world-aware 节点、Gameplay node、另一个 `LinkedPoseCall` 与其它 final writer。

Implementation 读取与 root 相同的不可变 `CharacterPresentationFactFrame`，但其 Fact 合同必须由 Interface signature 固定。它不得访问 Equipment 对象、Transform、Renderer、Gameplay Graph 或其它 mutable runtime 对象。Compiler 继续按稳定 FactId 与 kind 校验读取，不新增任意对象参数或反射入口。

业务取舍：完全隔离 Fact 会迫使 root 把移动速度、朝向等已有表现事实逐项穿过每个 Call；允许同一只读 Fact frame 可复用现有 PoseStateMachine，但通过 Interface 的 Fact contract 保持可验证、不可越权。

### 6. Group 共享选择，不共享 Entry 内部节点状态

同一 Group 的所有 Entry 共享：

- 当前 Implementation handle
- selection revision
- generation
- Prepare / Seal / Discard 生命周期
- source demand 聚合与延迟释放事务

每个 Entry 独立拥有：

- operation 与 stage range
- node workspace
- Pose / Value page
- StateMachine、player、Motion Matching 与 inertial state
- completion 与 diagnostics range

因此 `EquipmentPose` 与 `EquipmentHandGoals` 不会在一帧选到不同武器，但也不会假装共享一块可变 AnimInstance 内存。需要跨 Entry 共享的数据必须进入正式只读 Fact 或 typed port，不能靠隐藏状态引用。

### 7. 第一份 Equipment Interface 保持最小职责

第一份 Interface 固定两个 Entry：

| Entry | 输入 | 输出 | 业务职责 |
| --- | --- | --- | --- |
| `EquipmentPose` | Local Pose | Local Pose | 持械待机、移动与持续姿态组合 |
| `EquipmentHandGoals` | Component Pose | `component.full-body-ik-goals` | 从当前 Pose 和正式 Rig 绑定生成手部 Goals |

root graph 唯一拥有：

- Locomotion 主链
- `ActionPlaybackInput -> AnimationSlot`
- `LocalToComponentPose`
- `PredictiveFootPlacement`
- 唯一 `FullBodyIK`
- `ComponentToLocalPose`
- `OutputPose`
- final publication

有限攻击、换弹等 Timeline 动作继续进入 root AnimationSlot。Linked Implementation 负责持续状态，不创建第二 Slot。Idle、Run 等细状态继续由选中实现内部的 `PoseStateMachine` 处理，不为每个小状态切换 Implementation。

业务取舍：整张角色图替换自由度更高，但会更换不可逆边界并使状态恢复、IK、Action 与调试失去唯一所有者。把 Linked 边界放在持续 Pose 与 Goals 入口，可替换真正随业务形态变化的部分，同时保住根图稳定性。

### 8. Empty Implementation 必须产生合法空 Goals

当前 GoalSet 合同不能把 `GoalCount=0` 当作非法或继续读取上一帧。新增正式的 Empty Goals operation / descriptor，使 `EquipmentHandGoals` 的 Empty Implementation 能发布：

- `Availability = Ready`
- `GoalCount = 0`
- 当前 frame / rig identity
- 完整 completion 与 lineage

`FullBodyIK` 接收该 GoalSet 时必须按零个额外手部目标执行，而不是跳过整个 solver、复用旧目标或触发 fallback。Predictive Foot Placement 仍可在 root 提供脚部目标。

### 9. `PoseSubgraph` 与 `LinkedPoseCall` 的编译语义不同

`PoseSubgraph`：

- 编译时递归展开。
- `GraphInput`、`GraphOutput` 与 call frame 从 generated plan 消失。
- 不保留 runtime dispatch 或独立 generation。

`LinkedPoseCall`：

- 编译后保留 dispatch operation。
- 端口由 Interface 投影并进入 root typed DAG。
- 运行时只能选择当前 Projection 中已编译、签名相同的 Entry fragment。
- 不允许重连 root graph 的其它边。

第一版每个 required `Group + Entry` 在 root graph 中必须恰好有一个 Call。重复 Call 会让同一 Entry 的可变状态被多处推进；缺失 Call 会产生永远无法执行的 required Entry，因此两者都在 Build 阶段拒绝。

### 10. Projection 保存目录与不可变 fragment

Presentation Projection 新增：

- `LinkedPoseInterfaceDescriptor[]`
- `LinkedPoseGroupDescriptor[]`
- `LinkedPoseSelectorDescriptor[]`
- `LinkedPoseImplementationDescriptor[]`
- `LinkedPoseEntryFragment[]`
- root `LinkedPoseDispatchOperation[]`
- 全部候选 source closure 与 dense binding index
- Group 容量、generation layout 与 diagnostics range

每个 Entry Graph 经过与 root 相同的 Frontend、Capability、typed port、拓扑、Rig、Fact、source、stage 与 completion validation，再编译成不可变 operation/stage fragment。Runtime 不读取 authoring asset，不遍历 node，也不解释未知 payload。

Linked authoring 的身份、签名、fragment、source closure 与布局全部属于 Presentation Projection。它们改变 `ProjectionRevision`，不改变 gameplay `ContractHash`。如果诊断需要目录内容 identity，可在 Projection 内记录派生 catalog hash，但它不是第二套版本真相，也不参与 gameplay 合同。

### 11. 全部候选预分配正式 typed owner 范围

Compiler 先为每个 Implementation 合计其全部 required Entry 的：

- operation workspace
- Pose / Value pages
- StateMachine、player、Motion Matching、inertialization 与 Root Orientation Warp 等正式 typed temporal owner
- source demand slots
- completion 与 diagnostics slots

全部候选 fragment 在唯一 Pose runtime 内拥有互不重叠的正式 owner range；StateMachine、player、Motion Matching、inertialization、source、completion 与 diagnostics 继续使用各自已有的 committed / pending frame transaction，不新增通用 `byte[] node state` 或第二套执行页。Compiler 同时对同一 Group 的候选逐项取最大值，作为 Session admission、预算与诊断度量，不把它伪装成另一份 runtime 状态。

切换在现有 pending frame 中重置目标 Group 的 typed owner ranges并更换 active fragment table，不扩容、不创建托管容器，也不修改 committed owner。Seal 继续提交各正式 owner 的 pending 状态与 Linked handle；Discard 保留旧 committed owner 与 handle。

业务取舍：完整驻留成本按全部候选求和，高于复用一块最大候选内存；但它直接复用现有 typed owner 与事务，不引入地址重映射、影子状态页或第二 executor。Group 最大值仍让单个候选的最坏成本可在 Build 与诊断中直接看到。

### 12. 切换是 Prepare 阶段的 generation 事务

每帧由 selector adapter 先从 committed 业务状态生成 selection frame。每个 Group 首次 Call 前：

1. 校验 frame 的 Group、Interface、Implementation 与 revision 单调性。
2. 从 Session 锁定目录解析唯一 Implementation descriptor。
3. 校验 Interface signature、Rig、Projection ABI、Fact contract 与 source readiness。
4. 在现有 pending frame 中按规范默认值重置所选 Implementation 全部 required Entry 的 typed owner range。
5. 聚合 incoming Entry source demand。
6. Prepare 成功后，本帧同 Group 全部 Call 读取同一 incoming handle。
7. Seal 成功后提交 handle 与 generation，并按现有协议延迟释放旧 source。
8. Barrier 前 Discard 保留旧 committed handle 与各 owner 状态；Barrier 后失败沿现有合同进入 Faulted。

不同 Implementation 之间不迁移 StateMachine、player time、Motion Matching history 或 inertial state。切换发布 `PoseDiscontinuity`。视觉连续性只能由 root 在 Call 后显式配置 `Inertialization` 或 Blend，runtime 不注入隐藏 crossfade。

### 13. 首版只支持当前 Projection 内完整准备

Character Build 把 selector 映射能产生的全部 Implementation、Entry fragment 与 source closure 编入同一 Projection。Session 创建前完成：

- selector 唯一性验证
- mapping 完整性验证
- Interface / Implementation / Rig / Fact ABI 验证
- 全部 source closure 准备
- Group 最大布局分配

对局内状态切换不触发 Addressables、YooAsset、Unity 资产查找、异步下载或 runtime compile。任何候选未准备都在 Build 或 Session admission 阶段失败。

业务取舍：按需加载能降低驻留资源，但会引入“业务状态已经提交、动画实现尚未 Ready”的 Pending 状态以及取消、超时、占位和回滚合同。当前 change 先把确定性切换做好，内容热更新以后独立设计。

### 14. Document v3 保持唯一作者修改链

Document 新增：

- `readonly/presentation/linked-pose-interfaces/.../interface.json`
- `editable/presentation/linked-pose-implementations/.../implementation.json`
- Implementation 下现有 `pose-graphs/.../{graph,layout}.json`
- Profile 分片中的 group 与 selector bindings

Interface 是只读 context；Implementation、group binding 与 selector binding 是 editable。Capability Catalog、Exporter、strict codec、package hash、Reconciler、typed Presentation Mutation、Validator 与 MCP 固定五个生命周期工具必须同步。

不得新增 `create_linked_pose` 等 MCP 旁路。Document apply 必须降低为唯一 Mutation Plan，并在资产事务后从最终 Unity 树重导出整个 package。Inspector、文件变化与选择事件不得自动 Build 或 Apply。

## Risks / Trade-offs

- 最大布局提高固定内存；以 Group 容量诊断暴露真实成本，不用运行时分配绕开。
- 切换重置内部状态会产生不连续；显式 Inertialization 可处理视觉过渡，但不能伪造两个状态机的语义对应关系。
- 全量准备扩大 Session 资源闭包；它换来装备状态提交后立即可执行，不产生 Pending 或 fallback。
- Interface 签名严格版本化会提高改端口成本；它保护 host 与 Implementation 的 ABI，避免旧内容静默误连。
- 一个 Group 只能有一个 selector 限制了组合方式；需要多个业务共同决定时，应在业务侧形成一个明确 selector，而不是让 runtime 隐式争用优先级。
- 独立 Implementation 资产增加作者资产数量；Navigator 与 Profile inspector 必须按 Group、Interface、Implementation、selector 组织。

## Migration Plan

1. 先对齐 FinalIK change 的 Rig v4、GoalSet ABI、唯一 FullBodyIK 与 generated projection 合同。
2. 引入 Interface、Implementation、Group、通用 selection frame 与 selector provider contract，不改变现有 root 运行。
3. 加入 Equipment selector binding 与显式 Empty Implementation，不接临时 Equipment 身份。
4. 扩展 Capability、Document v3、typed Mutation、Validator 与 MCP，使所有作者入口共享同一语义。
5. 扩展 Projection ABI、fragment compiler、Fact contract、Group 最大布局与 source closure。
6. 扩展 runtime dispatch、generation 事务、completion、GoalSet、source lifecycle 与诊断。
7. 在正式 Presentation Profile 增加第一份 Equipment Interface 与 root Call。
8. 等独立 Equipment 迁移提供 Corin 正式 EquipmentSlotId / EquipmentId 后，一次迁移持续 Pose 分支并删除 root 旧分支。

迁移期间不得同时保留“root 按 Equipment 分支”和“Linked selector 选择 Implementation”两条正式路径。若正式 Equipment 身份尚未就绪，应停在能力完成状态，不得使用临时字符串、Renderer 状态、FeatureId 或 default Implementation 接线。

## Resolved Questions

- Linked Implementation 可读取现有不可变 `CharacterPresentationFactFrame`，其 Fact contract 纳入 Interface signature；不暴露 mutable Fact 对象，也不为首个 Equipment Interface增加任意业务参数口。
- 第一版同一 `Group + Entry` 只允许一个 root Call，避免共享可变 Entry state 的多次推进语义。
- 第一版不承担内容热更新。稳定 identity、revision 与 content hash 只服务 Build、stale detection 与诊断。
- 首个 Equipment 实现不要求仍在独立 active change 中演进的 Motion Matching 或 Blend Space 能力；Linked compiler 只复用届时已正式注册的现有节点，不复制专用版本。
