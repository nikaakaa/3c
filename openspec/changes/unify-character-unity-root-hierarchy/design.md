# Design: 统一角色 Unity 根层级

## Context

Portable Simulation 必须把 `WorldSimulationState` 保存为唯一逻辑位姿真相；Unity Transform 不能成为 Fixed、Rollback、ServerAuthoritative 或 DotRecast 的共同逻辑数据结构。但角色预制体仍需要一个明确、可观察的外层逻辑位置，否则作者看到的场景层级与运行时 Body 完全脱节。

当前实现混合了三种口径：

1. Unity CharacterController 组合由 Solver 直接移动 LogicRoot Transform；
2. Fixed/Deterministic Rollback 只在启动时读取 `m_LogicalSpawn`，后续外层 Transform 静止；
3. Remote Presentation 直接实例化 Animator 所在对象作为 VisualRoot，没有统一 LogicRoot/PoseRoot。

三者最后都把模型世界姿态交给 `CharacterBodyPresentationRuntime`，但预制体层级和可观察的 Transform owner 不一致。

## Goals / Non-Goals

### Goals

- 所有有可见角色的 Unity 组合使用同一 `LogicRoot -> VisualRoot -> PoseRoot` 合同。
- 最外层 LogicRoot 能准确显示当前已提交或已选择的逻辑 Body。
- 保持 World/Model Body 为唯一真相，Transform 永远只做单向投影。
- VisualRoot local pose 能直接解释表现与逻辑 Body 的差值。
- PoseRoot 明确隔离动画 Component Pose 与 Gameplay/Presentation 世界位移。
- 预制体、构建器、运行时验证和诊断使用同一绑定，不存在按名称修复。

### Non-Goals

- 不修改 Gameplay Body、KCC、重力、坡面或 Step 运动语义。
- 不在 Foot Placement 中移动任何根 Transform。
- 不修改 Pelvis、Foot Goal、FBBIK 或动画 Clip 的数学。
- 不增加新的网络字段、Snapshot 字段或 Hash 字段。
- 不保留 `m_LogicalSpawn`、`AnimatorRoot` 或旧 Remote Visual Template 兼容入口。

## Decisions

### 1. World/Model Body 是真相，LogicRoot 是单向投影

选择：LogicRoot 只镜像成功提交后的最终 Body，不参与逻辑读取。

原因：直接把 Transform 升格为逻辑真相虽然便于 Inspector 观察，但 Fixed、Rollback、普通 .NET Authority 和 Unity CharacterController 会产生不同的数据 owner，恢复、Replay 与 Hash 也会重新出现 Transform 双真相。保持 Body 为真相，同时把其结果投影到最外层，既能满足场景作者观察，也不破坏 portable 边界。

提交规则：

- Standard Local/Unity CharacterController：Solver binding 使用同一 LogicRoot；outer transaction 失败时不得留下未提交姿态。
- Local Fixed：`CompleteResultCommit` 成功接受完整 Body transaction 后，把最后一个 sample 的 FinalBody 投影到 LogicRoot。
- Deterministic Rollback：Replay/Current 合并事务只投影最终当前分支的最后一个 FinalBody，不依次写 Replay 历史。
- ServerAuthoritative observed/remote：Model Egress 成功提交 selected Body batch 后，把最后一个 selected Body 投影到 LogicRoot；Reset 使用新 anchor，而不是保留旧 Transform。
- 零 Body sample 的事务不移动 LogicRoot。

LogicRoot projection 不写回 WorldState、CharacterState、History、Snapshot、Hash、Input、Perception 或 Solver request。

### 2. VisualRoot 只表达相对表现姿态

选择：Body Runtime 继续计算最终 visible world pose，但应用时转换为 LogicRoot local pose写入 VisualRoot。

设最终表现绑定后的世界姿态为 `VisibleWorld`，当前 LogicRoot 世界姿态为 `LogicWorld`：

```text
VisualLocalPosition = Inverse(LogicWorld) * VisibleWorldPosition
VisualLocalRotation = Inverse(LogicWorldRotation) * VisibleWorldRotation
```

这样 LogicRoot 在 logic commit 时可以离散更新，而 VisualRoot 在每个 PresentationFrame 继续插值或有界纠偏。最终模型世界姿态仍由唯一 Body Runtime 决定；父节点更新不会另加一份未裁决位移。

不选择继续直接写 VisualRoot 世界姿态作为正式合同，因为那会隐藏 LogicRoot 与 VisualRoot 的相对误差，Prefab 层级仍无法解释当前 correction。

### 3. 保留第三层，但正式命名为 PoseRoot

选择：不把 Animator 合并回 VisualRoot；将旧 `AnimatorRoot` 单路迁移为 `PoseRoot`。

原因：VisualRoot 属于 Body Presentation，PoseRoot 属于动画 Component Pose。FBBIK、Rig physical/virtual bone、Final Writer 和 Foot Placement 的 pose-space 计算需要一个不会被 Body Presentation 当成同一 Transform 写入的明确原点。合并两者会让“角色世界表现姿态”和“动画根骨骼姿态”重新共享 owner。

PoseRoot 规则：

- 必须是 VisualRoot 的直接子级；
- Animator 的 Transform 必须精确等于 PoseRoot；
- Animancer 与 Animation Rig Binding 归属 PoseRoot；
- 默认 local position/rotation/scale 为 identity；
- Body Runtime、KCC、Network Model 与 Foot Placement 不得写 PoseRoot Transform；
- 最终动画骨骼写入只能通过现有 Final Writer，Pelvis 平移继续属于 PoseRoot 下的骨骼结果。

### 4. 唯一显式 CharacterRootHierarchyBinding

选择：新增根层级 binding，并让所有 Host/Template 显式引用。Binding 只保存三项 Transform 与结构验证，不拥有更新循环、Body history或 Presentation state。

验证规则：

- LogicRoot 是角色实例最外层；
- VisualRoot.parent == LogicRoot；
- PoseRoot.parent == VisualRoot；
- 三者互不相同，rotation/scale 合法；
- Animator.transform == PoseRoot；
- World-Aware Presentation 的 PresentationRoot == VisualRoot、SelfColliderRoot == LogicRoot；
- Local Owner 的默认 camera follow/aim anchors 位于 VisualRoot 表现子树；
- 缺少任何正式引用直接失败，不执行 `GetComponentInChildren`、名称搜索、自动补建或 Transform fallback。

Host 不再分别保存 `m_LogicalSpawn` 与 `m_VisualRoot` 形成重复配置。初始 Body 从 binding.LogicRoot 读取，Presentation 从同一 binding.VisualRoot/PoseRoot 装配。

### 5. 预制体一次性迁移并删除旧生成路径

正式预制体统一为：

```text
<Character Instance>          LogicRoot
└── VisualRoot
    ├── PoseRoot
    │   └── Skeleton / Mesh / Equipment visuals
    └── CameraAimAnchor       仅 Local Owner 需要
```

相机 rig 本身不需要成为角色子节点；Camera Follow/Aim 仍由现有 Camera Presentation Runtime 使用同一 BodyFrame 计算。AI 或无相机角色不创建空相机绑定。

Gameplay Lab 与其它 Editor Builder 只创建上述正式结构。旧 `EnsureStrictAnimatorRoot` 替换为对 Root Hierarchy Binding 的精确构建/验证；旧 `AnimatorRoot`、Remote VisualRoot-only template 和 spawn-only字段直接删除。

## Data Flow

```text
WorldSolver / Network Model Egress
  -> atomic Body commit
  -> LogicRoot one-way projection
  -> CharacterBodyPresentationRuntime samples the same Body stream
  -> visible world pose
  -> VisualRoot local pose relative to LogicRoot
  -> PoseRoot animation Component Pose
  -> Foot Goals / Pelvis bone / FBBIK
  -> Final Writer physical bones
```

写入所有权固定为：

| 输出 | 唯一写入者 | 禁止写入者 |
| --- | --- | --- |
| World/Model Body | WorldSolver + outer transaction | Transform、Presentation、Foot Placement |
| LogicRoot | 成功 Body commit projection | Body Runtime、Foot Placement、动画 |
| VisualRoot | CharacterBodyPresentationRuntime | Solver、Foot Placement、动画 Final Writer |
| PoseRoot/骨骼 | Animation Pose Plan + Final Writer | Solver、Network Model、Body Runtime |
| Pelvis bone | Foot Goal + 唯一 FBBIK 的最终 Pose | LogicRoot/VisualRoot 修正路径 |

## Diagnostics

根层级诊断必须在同一 render frame identity 下提供：

- committed/selected Body Position/Rotation 与 tick；
- LogicRoot world Position/Rotation；
- VisualRoot local 与 world Position/Rotation；
- PoseRoot local 与 world Position/Rotation；
- Pelvis component/world position及相对 PoseRoot 平移；
- 每项本帧是否由对应正式 owner 写入。

CSV 和 Runtime Debug 由此可以先区分 Body 沿坡移动、Visual correction、动画根变化和 Pelvis/FBBIK，再分析脚部闭环。

## Risks / Tradeoffs

- LogicRoot 在 logic tick 离散更新，VisualRoot local offset会随表现帧变化；这是明确表达逻辑/表现差值，不是额外平滑链。
- Unity CharacterController 当前已经移动 LogicRoot，Fixed/Remote 是新增投影。实现必须统一提交时序，否则同一帧父节点先动、子节点晚动会短暂暴露错误世界姿态。
- 全部正式 prefab 与 builder 必须同时迁移；只改 Gameplay Lab 会让网络/回滚产品继续使用旧层级，形成禁止的双路径。
- `AnimatorRoot` 改名和组件搬迁会改变 Prefab fileID/引用。迁移必须由正式 Editor builder 一次性重建并对账所有显式引用，不保留旧名称识别。

## Spec Reconciliation

- current `character-motion-simulation-boundary` 已要求 BodyState 替代 Transform 作为逻辑真相，但“在 binding 边界对齐场景对象”没有规定 LogicRoot、提交时机和 Replay 多步语义；本 change 补全该缺口。
- current `character-presentation-interpolation` 要求 visual root 与逻辑 Transform 分离，但只配置 VisualRoot，没有正式 LogicRoot/PoseRoot 层级；本 change 修改该要求。
- current `character-foot-placement-presentation` 与 active `complete-character-predictive-foot-ik` 禁止 Foot Placement 写 VisualRoot/Gameplay Body，和本 change 一致；实施时只更新诊断引用，不放宽任何根写入权限。
- active `add-discrete-stair-presentation` 的 visible Y 修正若恢复，只能成为 Body Runtime visible pose 的一部分；它不得另写 LogicRoot、VisualRoot 或相机 Transform。
