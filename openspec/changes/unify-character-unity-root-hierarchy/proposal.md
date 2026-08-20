# Change: 统一角色 Unity 逻辑根、表现根与姿态根层级

## Why

当前 `WorldSimulationState` 或 Network Model selected Body 是角色逻辑位姿真相，但 Local Fixed、Deterministic Rollback 和远端观察角色没有把该结果投影到预制体最外层 Transform。`m_LogicalSpawn` 只在创建初始 Body 时读取一次，之后 `CharacterBodyPresentationRuntime` 直接以世界位姿写 `VisualRoot`。因此运行时层级表现为外层对象永远停在出生点、整套模型只由 `VisualRoot` 移动，场景作者无法从预制体根观察当前逻辑位置，也无法用层级区分逻辑位移、表现纠偏和骨骼姿态。

现有 Gameplay Lab 构建器还会在 Animator 位于 `VisualRoot` 时自动生成名为 `AnimatorRoot` 的中间层。该层实际承担 Component Pose 原点，但名称、组件归属和写入边界没有形成正式统一合同；Local、Fixed、Rollback 和 ServerAuthoritative Remote 使用的层级也不一致。

这不是 Foot Placement 的 Pelvis 参数问题。继续在 Foot IK 内调整 Spring、`VisualRoot` 或 Mesh Transform 会让 Gameplay Body、表现根和骨骼根形成更多写入路径。必须先把 Unity 根层级与单向投影边界收成一条正式链，再继续 Pelvis 支撑闭环。

## What Changes

- 引入唯一显式 `CharacterRootHierarchyBinding`，正式声明 `LogicRoot -> VisualRoot -> PoseRoot`：
  - `LogicRoot` 必须是角色预制体最外层，表示当前已提交或已选择逻辑 Body 的 Unity 单向投影；
  - `VisualRoot` 必须是 `LogicRoot` 的直接子级，只承载相对逻辑 Body 的表现插值、纠偏和绑定偏移；
  - `PoseRoot` 必须是 `VisualRoot` 的直接子级，承载 Animator、Animancer、Animation Rig 与 Component Pose，不承载 Gameplay 或 Body Presentation 世界位移。
- `WorldSimulationState` 或 Network Model selected Body 继续是唯一逻辑真相。LogicRoot 只在成功的 outer commit/selected Body commit 后镜像最终 Body，不进入 Snapshot、Hash、Solver 输入或 Gameplay 读取，不允许反向写回逻辑状态。
- Fixed、Deterministic Rollback 和远端 observed 角色在同一正式提交边界更新 LogicRoot；一个事务包含 Replay/多步结果时只投影最终当前 Body，不逐步播放历史 Transform。
- `CharacterBodyPresentationRuntime` 继续从 Body sample 历史计算唯一 visible world pose，但只把该结果转换为 LogicRoot 下的 VisualRoot local pose；它不得写 LogicRoot。
- 删除 `m_LogicalSpawn` 运行时语义与字段，统一为显式 LogicRoot。初始 Body 从 LogicRoot 读取，后续 LogicRoot 由正式提交结果单向更新，不保留 spawn-only 旧路径。
- 删除自动生成和继续识别 `AnimatorRoot` 的旧命名路径，正式迁移为 `PoseRoot`；缺失、层级错误或组件归属错误时构建/运行直接失败，不按名称搜索或运行时补建。
- 统一迁移 Local、AI、Local Fixed、Rollback、ServerAuthoritative Unity/DotRecast Client 和 Remote Presentation Template 的预制体与 Gameplay Lab 生成器。相机表现锚点归入 VisualRoot 表现子树，自碰撞根继续明确绑定 LogicRoot。
- 增加只读根层级诊断，分别暴露 committed/selected Body、LogicRoot 世界姿态、VisualRoot local/world 姿态和 PoseRoot local/world 姿态，避免再把 Body 沿坡高度与 Pelvis 骨骼平移混成一个数值。
- Foot Placement 仍只产生 Goal 和 `PelvisPreSolveTranslation`，不得写 LogicRoot、VisualRoot 或 PoseRoot。本 change 不改变 KCC、Body Motion、Foot Goal、FBBIK 数学或网络状态。

## Impact

- 受影响 current specs：
  - `character-motion-simulation-boundary`
  - `character-presentation-interpolation`
- 受影响 active changes：
  - `complete-character-predictive-foot-ik` 继续保持根 Transform 零写入；本 change 只让其诊断明确读取统一 PoseRoot 与 LogicRoot。
  - `add-discrete-stair-presentation` 若以后恢复，只能继续在同一 `CharacterBodyPresentationFrame` 内形成 visible pose，不能再拥有 LogicRoot、VisualRoot 或相机专用 Transform 路径。
  - `replace-animation-sequence-with-clip-authoring` 修改了 `character-presentation-interpolation` 的 Body history requirement；本 change 不修改该 requirement，但实施/归档时仍需对账合并顺序。
- 受影响代码：Character Root hierarchy binding、Presentation Factory/Body Runtime、Fixed 与 Rollback Host/Registration、Unity CharacterController Host validation、ServerAuthoritative Remote Presentation、Gameplay Lab/Network Product prefab builders、根层级诊断。
- 受影响资产：全部正式 Character Runtime Profile prefab、Gameplay Lab Local Fixed prefab、远端表现模板及引用它们的 Scene/Variant。
- 不新增 fallback、旧字段兼容、双写 Transform、运行时层级搜索或第二套 Body truth。
