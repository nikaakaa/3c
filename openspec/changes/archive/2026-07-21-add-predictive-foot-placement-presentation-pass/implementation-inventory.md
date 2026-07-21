# 实施清单

## 基线

- 唯一表现协调入口：`CharacterSimulationPresentationRuntime.Present`。
- 最终顺序：`CharacterBodyPresentationRuntime.Present -> CharacterAnimationPlaybackRuntime.Present -> CharacterFootPlacementRuntime.Present -> CharacterCameraPresentationRuntime.Present`。
- 动画唯一求值链：`CharacterAnimationPlaybackRuntime.Present -> AnimationPlaybackLifecycle.Apply -> IAnimationPlaybackAdapter.Evaluate -> AnimancerPlaybackAdapter.Evaluate`。
- `CharacterBodyPresentationFrame` 已包含可见/目标位置、旋转、线速度、可见/目标 yaw velocity、Grounded、ResetSequence 与 ResetReason。
- 正式创建入口：`CharacterPresentationRuntimeFactory.CreateLocalOwner`、`CreateSimulatedActor`、`CreateObservedActor`。Foot Placement只在Play Mode正式角色runtime执行并由Live Debug观察；纯Timeline Authoring Preview使用`PreviewPlaybackEngine`且只有动画上下文。
- Corin 正式角色资产位于`Assets/Prefabs/Characters/RuntimeProfiles`。Standalone、Rollback与网络产品分别使用显式Runtime Profile Prefab；VisualRoot 为 Animancer Animator transform，Host 显式绑定 Animancer、WorldBodyBinding、VisualRoot、Body Profile 与 Camera。
- Corin 明确骨骼：`Bip001 Pelvis`、左右 `Thigh`、`Calf`、`Foot`、`Toe0`，可形成两条三骨 Limb IK 链。
- Final IK 位于 `Assets/Plugins/RootMotion`，已安装插件官方 `RootMotion.Runtime.asmdef` 与Editor-only `RootMotion.Editor.asmdef`，vendor runtime和Editor分别进入`RootMotion`与`RootMotionEditor`。

## 变更冲突核对

- `add-timeline-animation-marker-sync` 已明确只映射AnimationTrack表现采样时间，不提供foot contact或plant权威。
- 本 change 的contact只读取Animancer最终姿势、Body Grounded与Physics support，不读取marker/effective phase、Timeline window、Blackboard、Action、Tag或State。
- `add-corin-targeted-motion-warp-demo` 可能修改 Host、角色 role 与 Corin 资产。本 change 每次编辑共享文件前均以工作树最新内容为准；只增加 Presentation-owned Foot Placement 参数，不改其 Input、Motion Warp、Program 或 Session 语义。
- 本 change 沿唯一 Presentation Factory/runtime 闭环，不修改 Simulation、Network或WorldSolver。Timeline Animation Clip只保存单一Foot Placement Weight曲线，并通过Agent v13正式Snapshot/Patch合同读写；不直接写Graph YAML，也不建立第二份策略表。

## 最终类型与程序集

| Owner | 最终内容 |
|---|---|
| `ThirdPersonClient.Runtime` | Pose Post Process合同、Foot Placement Profile/Rig、Planner/runtime、Physics query、constraint、pelvis、diagnostics snapshot与Animancer visible contribution |
| `RootMotion` | 未修改的 Final IK vendor runtime |
| `RootMotionEditor` | 未修改的 Final IK vendor Editor |
| `ThirdPersonCharacter.Presentation.FinalIK` | 唯一 `ICharacterFootPlacementSolver` Final IK Limb adapter |
| `ThirdPersonClient.Editor` | Profile/Host配置诊断、Live Debug、Scene gizmo与Agent v13 Timeline曲线事务 |
| `BTSMTL.Timeline` | Animation Clip单一normalized Foot Placement Weight曲线和唯一mutation API |
| `BTSMTL.Timeline.Editor` | 每个AnimationTrack默认折叠的Curves分组；展开后投影单一曲线，支持直接编辑且不保存第二份曲线 |

## 正式资产

- `Assets/Configs/Character/Corin/Pipeline/Presentation/CorinFootPlacementProfile.asset`
- `Assets/Configs/Character/Corin/Pipeline/Graphs/CorinPlayableRootTree.asset` 与shared Attack Timeline中的19个Animation Clip曲线
- `Assets/Prefabs/Characters/RuntimeProfiles/CorinStandalonePlayer.prefab`
- `Assets/Prefabs/Characters/RuntimeProfiles/CorinDeterministicRollback.prefab`
- `Assets/Prefabs/Characters/RuntimeProfiles/CorinServerAuthoritativeUnityClient.prefab`
- `Assets/Prefabs/Characters/RuntimeProfiles/CorinServerAuthoritativeDotRecastClient.prefab`
- 上述正式角色资产及其产品装配上的唯一Rig、左右LimbIK、Final IK adapter与Composition
- Local Standalone使用的`Assets/Prefabs/Env/Plane.prefab`和`wall.prefab`全部环境Collider统一位于Ground Layer 9
- DeterministicRollback Peer使用的`Assets/Scenes/Shared/CharacterMovementTestEnvironment.prefab`及两个场景内Corin表现装配复用同一Profile、曲线和Final IK adapter

## 最终调用链

```text
Presentation output
  -> CharacterSimulationPresentationRuntime
  -> Body.Present
  -> Animation.Present
  -> Animancer.Evaluate
  -> CharacterFootPlacementRuntime.Present
  -> PelvisComponentVerticalOffset沿VisualRoot up转换到pelvis父骨空间
  -> FinalIKLimbFootPlacementSolver.Apply
  -> Camera.Present
```

## 明确不创建或删除的路径

- 不创建 GrounderBipedIK、GrounderFBBIK、GrounderIK 或自主 LateUpdate IK owner。
- 不创建 BTSMTL foot phase、Timeline foot window、Blackboard foot variable 或网络 IK packet。
- 不创建独立 Foot Placement EditorWindow、动态 pass registry、priority 排序或 local-only runtime。
- 不修改 Final IK vendor C# 源码。
- 不使用Animation Marker Sync提供foot contact或plant事实；Marker Sync只改变正式动画表现采样时间。
- 不保留Profile producer policy、旧曲线表、缺曲线默认值或按clip名称匹配路径。

## Timeline曲线可视化

- AnimationTrack组合行包含Clip行、Marker Sync行和默认折叠的Curves分组；展开后只显示Foot Placement Weight曲线行。
- 曲线行逐Clip读取唯一序列化槽位`FootPlacementCurve`，以`Foot Placement Weight`作者语义按`StartFrame..EndFrame`绘制`0/0.5/1`参考线、插值曲线和原始key。
- 点击曲线段选择对应`TimelineClipView`；拖动key、双击增加和右键删除均通过同一Timeline Undo、dirty与Projection重建路径提交。
- 曲线行没有AuthoringId、不进入`TimelineData.Tracks`、不执行Tick，也不参与Projection或Runtime采样；它们只投影同一Animation Clip曲线，不保存第二份数据。
- Agent v13 Snapshot核对结果为14个Timeline、19个AnimationClip；迁移前19/19旧四曲线逐项一致，最终Snapshot只输出单一Foot Placement Weight曲线。

## 实施后成熟度审计

- 成熟方案调研、当前实现对照和后续演进边界记录在`maturity-research.md`。
- 当前Runtime Profile中的左右heel/toe sole offset仍为零，字段已闭环不等于Corin鞋底几何已校准。
- Corin `Bip001`父骨预旋转已纳入solver坐标换算；Foot Placement不再把pelvis父骨local Y当作角色竖直方向。
- 当前Foot Placement Weight是唯一手工作者曲线；它只表达整体介入，不能视为左右脚独立Plant数据。
- 当前Contact依赖最终混合姿势逐帧差分，CrossFade可能产生假脚速。
- 当前脚踝旋转直接消费语义surface rotation，尚未应用rig-specific semantic foot frame delta。
- 当前Support Query是固定容量采样和高度连续过滤，不等同于完整Virtual Ground或凸包Ground Envelope。
- 上述项目没有被补写为已完成task。后续实现必须建立独立proposal，不恢复旧四曲线、FootPhase、Grounder或自主LateUpdate路径。
