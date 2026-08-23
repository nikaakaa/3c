# Change: 接入离散楼梯表现

## Why

当前项目已经具备两种楼梯所需的Gameplay能力：

- Ramp楼梯已经通过`StairTraversalSurfaceAuthoring`把`CharacterTraversal` Gameplay Ramp与`FootPlacementSurface`真实踏面分离。
- Fixed KCC已经完整实现Philippe语义的Step Detection、Step Commit、Ground Probe与Ground Snap，普通`Ground`真实台阶无需另一套Motor即可通行。

缺口不在KCC。当前Gameplay Lab只有Ramp连续楼梯和孤立`StepCapabilityCourse`，没有一段可与Ramp直接比较的连续真实台阶内容；同时`CharacterVisualTrajectoryFollower`在Grounded时强制visible Y直接等于target Y，真实台阶产生的逐级Body高度变化会直接传给VisualRoot和默认Camera。

因此本change保持KCC和现有Ramp工具不变：真实台阶只作为普通`Ground`场景碰撞进入同一个Collision Artifact；表现侧在Body target采样与VisualRoot发布之间增加唯一的接地竖直不连续修正层，让真实台阶Gameplay保持离散而角色模型、脚、骨盆和默认Camera连续表现。

## What Changes

- 保持现有Ramp楼梯链路不变：
  - `CharacterTraversal` Ramp继续作为唯一Gameplay表面进入Deterministic Collision Artifact。
  - `FootPlacementSurface`真实踏面继续只供Foot Placement查询。
  - `StairTraversalSurfaceAuthoring`继续只表达Ramp与真实踏面分离，不扩展双策略枚举。
- 正式定义离散楼梯场景口径：
  - 离散楼梯不挂`StairTraversalSurfaceAuthoring`，不创建Traversal Ramp。
  - 持久化阶梯形Collider代理使用普通`Ground`层，并被恰好一个`DeterministicCollisionSurfaceAuthoring`拥有。
  - 同一组`Ground`阶梯Collider同时进入Collision Artifact并供Foot Placement Physics查询，不复制`FootPlacementSurface`副本。
  - Collision Baker继续只按现有显式Surface owner收集，不增加楼梯特判、自动几何生成或运行时fallback。
- 在Gameplay Lab共享环境中保留现有Ramp楼梯，并增加一段与LowStairs相同rise/run口径的连续离散楼梯，形成同场景可解释对照；独立`StepCapabilityCourse`继续表达0.14m、0.24m准入与0.40m拒绝边界。
- 在`CharacterBodyPresentationRuntime`内部增加唯一`Grounded Vertical Discontinuity`表现阶段：
  - 只读取正式Body interval的previous/current position、`GroundedBefore/After`、stream update与Reset语义。
  - 只有普通连续interval在两端都Grounded且垂直高度差达到显式阈值时，才建立竖直不连续修正。
  - 竖直目标立即采用current Body终点，VisualRoot从当前visible Y通过有界临界阻尼收敛；连续台阶从当前visible状态重新定向同一修正状态，不叠加多条尾巴。
  - 普通坡面、连续Ramp和小于阈值的接地Y变化继续直接重采样，不运行持续竖直低通。
  - Initialization、SelectedStream Reset、Committed Branch Replacement与Airborne清除或重锚定该状态，不把跳跃、传送或网络纠偏当成台阶。
- `CharacterBodyPresentationProfile`增加独立于`Direct/BoundedCorrection`分支纠偏模式的接地竖直响应配置：Response Mode、Discontinuity Threshold、Half-life、Maximum Error与Settle Distance；Corin的Direct、Rollback和Observed Body Profile都显式迁移。
- `CharacterBodyPresentationFrame`与现有Presentation diagnostics暴露当前竖直不连续Kind、offset、velocity、active/clamped/settled，供排查但不成为动画状态、GameplayFact或同步事实。
- 默认Camera继续从同一个最终`CharacterBodyPresentationFrame.VisiblePosition`生成follow point，不增加Camera专用台阶检测或第二份Body插值历史。
- 通过现有显式Unity菜单重新Bake唯一Collision Artifact并更新CollisionWorldHash；不自动运行Unity、构建或发布Network Product，不归档本change。

## Impact

- 影响`CharacterBodyPresentationProfile`、Body settings、`CharacterBodyPresentationRuntime`、`CharacterVisualTrajectoryFollower`之后的竖直表现阶段、`CharacterBodyPresentationFrame`和Body diagnostics。
- 影响Corin Direct、Rollback与Observed三个Body Presentation Profile资产。
- 影响Gameplay Lab共享环境Prefab、Deterministic Surface作者层级、唯一Collision Artifact及其Hash。
- 只需审计Camera现有消费者继续读取最终Body Frame，不重写Camera resolver。
- 不修改Deterministic KCC Motor、KCC配置、WorldSolveResult、Fixed Gameplay状态、VerticalVelocity、Snapshot、Hash、Program ABI或网络packet。
- 不新增Unity Layer、`StairTraversalPolicy`、第二楼梯组件、运行时Ramp/Step切换或任何fallback。

## 与现行Spec及Active Change对比

- current `character-stair-surface-authoring`把所有“连续梯段”描述成Ramp楼梯，容易被读成真实连续台阶不合法。本change将该要求收窄为“注册`StairTraversalSurfaceAuthoring`的Ramp楼梯”，并新增普通`Ground`离散楼梯合同；现有Ramp作者组件与validator实现不改。
- current `deterministic-kcc-world-solver`的Baker楼梯段落同样使用宽泛“连续梯段”措辞。本change明确楼梯validator只约束已注册Ramp绑定；未注册的离散台阶继续沿普通显式`Ground` Surface owner进入Artifact。KCC算法要求不变。
- current `character-presentation-interpolation`要求正常连续interval不持续运行第二次SmoothDamp，并让Grounded branch replacement的Y直接使用target。本change不改变这两个要求；新增阶段只处理普通canonical interval中达到阈值的接地竖直不连续，且不参与branch replacement纠偏。
- current `character-camera-pipeline`已经要求VisualRoot和默认Camera使用同一个Body visible pose。本change继续沿该路径传递竖直修正，不创建Camera专用滤波器。
- completed但未归档的`separate-stair-gameplay-and-foot-surfaces`仍然是Ramp楼梯工具与现有六条Ramp的实现来源；本change不撤销、不复制其作者工具，只在同一场景旁增加普通Ground离散楼梯。
