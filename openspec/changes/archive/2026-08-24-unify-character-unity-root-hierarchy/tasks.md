## 1. 根层级合同

- [x] 1.1 新增唯一 `CharacterRootHierarchyBinding`，显式保存并验证 LogicRoot、VisualRoot、PoseRoot 与严格父子关系。
- [x] 1.2 将 Animator、Animancer、Animation Rig、World-Aware Presentation、自碰撞根与相机锚点验证收口到该绑定合同，删除 Transform/名称搜索和缺失 fallback。
- [x] 1.3 将 Runtime、Editor 和 diagnostics 中含糊的 Animator Root 业务命名统一迁移为 PoseRoot，删除旧命名入口。

## 2. LogicRoot 单向投影

- [x] 2.1 在 Standard Local Unity CharacterController 边界对账 LogicRoot 与 Solver binding，保证成功事务后的 Transform 精确等于 committed Body，失败事务不留下未提交姿态。
- [x] 2.2 在 Local Fixed result commit 中只把事务最后一个 FinalBody投影到 LogicRoot，删除 `m_LogicalSpawn` 并由 LogicRoot建立初始 Body。
- [x] 2.3 在 Deterministic Rollback result commit 中只把 Replay/Current 最终分支的最后一个 FinalBody投影到 LogicRoot，Reset/失败时不播放或保留错误历史 Transform。
- [x] 2.4 在 ServerAuthoritative observed/remote selected Body commit 中投影最终 selected Body到 LogicRoot，删除 VisualRoot-only remote template路径。

## 3. VisualRoot 与 PoseRoot 应用

- [x] 3.1 让 `CharacterBodyPresentationRuntime` 从唯一 BodyFrame计算 visible world pose，并只应用为 LogicRoot 下的 VisualRoot local pose。
- [x] 3.2 更新 `CharacterPresentationRuntimeFactory` 与各 Host/Registration，使 Body、Animation、Foot Placement 和 Camera只消费同一 Root Hierarchy Binding。
- [x] 3.3 保持 PoseRoot 的 Body/Foot Placement 世界位移写入为零，动画结果继续只通过现有 Pose Plan 和 Final Writer发布。

## 4. Prefab 与 Builder 单路迁移

- [x] 4.1 将 Gameplay Lab 构建器从 `EnsureStrictAnimatorRoot` 迁移为唯一 Root Hierarchy构建，直接生成 `LogicRoot -> VisualRoot -> PoseRoot`。
- [x] 4.2 迁移 Local Corin、Training Enemy、Local Fixed Gameplay Lab 与 Deterministic Rollback prefab，删除 `m_LogicalSpawn` 和 `AnimatorRoot` 旧结构。
- [x] 4.3 迁移 ServerAuthoritative Unity/DotRecast Client、Remote Presentation Template 与相关 Scene/Variant显式引用，不保留 VisualRoot-only 模板。
- [x] 4.4 将 Local Owner camera follow/aim anchors归入 VisualRoot表现子树，并保持无相机角色不生成空绑定。
- [x] 4.5 对账全部 Character Runtime Profile prefab 的唯一根层级、组件归属、SelfColliderRoot、Rig physical bone与显式Host引用。

## 5. 诊断与文档收口

- [x] 5.1 扩展根层级 Runtime diagnostics 与 Foot Landing CSV，分别发布 Body、LogicRoot、VisualRoot、PoseRoot 和 Pelvis 姿态。
