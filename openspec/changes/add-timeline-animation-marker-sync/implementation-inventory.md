# Timeline Animation Marker Sync 实现清单

## 1. 唯一数据所有权

- `AnimationTrack` 唯一保存 `AnimationSyncMode`、`SyncGroupId`、`AnimationMarkerSequenceTopology`、`AnimationMarkerSyncRole` 与 `AnimationSyncMarker`。
- 每个 marker 保存稳定 `AuthoringId`、业务 `MarkerId` 与整数 Timeline frame。
- `None` 会原子清空 group、topology、role 与 markers；`MarkerGroup` 是唯一进入 Projection 的同步配置。
- StateMachine edge、Presentation Profile、Action 配置、Foot Placement、Program operation、Simulation state 与 Network protocol 均不复制 marker 数据。

## 2. 作者链路

```text
AnimationTrack
  -> AnimationMarkerSyncAuthoring
  -> CharacterAnimationMarkerSyncAuthoringContext
  -> CharacterPresentationProjectionBuilder
  -> AnimationMarkerSyncBinding
```

- `AnimationMarkerSyncAuthoring` 是唯一规则实现，校验单 Track、callsite playback mode、同 Layer/Group 的有向 marker pair 集合与采样覆盖。
- `CharacterAnimationMarkerSyncAuthoringContext` 从完整 RootTree topology 收集全部可达 Timeline 与 callsite，再调用唯一规则实现。
- Timeline Inspector 负责编辑当前 Track，并在绑定 `CharacterPipelineHost` 时显示完整 Definition 上下文错误及同组 producer pair 覆盖。
- Projection 编译 marker time、segment、回绕 segment 与重复 pair occurrence 索引；运行时不扫描资产、不排序 marker。
- Timeline Editor 为每个 AnimationTrack 显示固定 Marker Sync 子轨；子轨只投影父 Track 数据，不进入 `TimelineData.Tracks`，不执行 Tick。

当前Marker作者能力已经收敛：`SYNC MARKERS`子轨可独立折叠，空白帧右键可从同Layer、同Group候选或显式新MarkerId创建点；Marker支持选择、Inspector定位、右键重命名/删除和整数帧拖动。拖动期间只更新本地草稿，Pointer Up或Capture Out只提交一次Undo，Pointer Cancel丢弃草稿；提交统一刷新布局、Inspector、校验、Projection stale状态与Authoring Preview。Cyclic显示末点到下一周期首点的闭合方向，Finite只显示首尾coverage，Preview游标突出当前pair与fraction。

Timeline Editor当前统一使用三类作者内容语义：

```text
Span Clip        -> 正式Track拥有的区间内容
Point Marker     -> AnimationTrack拥有的离散Marker
Continuous Curve -> Animation Clip拥有的连续控制曲线
```

该分类只统一Editor时间坐标、选择、吸附、pointer capture、Undo和刷新，不创建统一Runtime Track或第二份序列化数据。

## 2.1 Typed Curve Channel与通用Curve Editor

Editor-only `TimelineCurveChannelCatalog` 以稳定ChannelId显式登记owner类型、显示名、颜色、ClipNormalized时间域、bounded/unbounded值域、单位、默认曲线、读取、原子替换与owner validator。Catalog不反射字段、不扫描SerializedProperty，也不进入Player runtime。

当前已登记channel：

| Owner | Channel |
|---|---|
| Animation Clip | Weight、Ease In、Ease Out、Foot Placement Weight |
| MotionCurve Clip | Weight、Position X/Y/Z、Yaw、Ease In/Out |
| MotionWarp Clip | Position Progress、Yaw Progress |
| CameraStateClip、CameraResponseClip | Weight、Ease In、Ease Out |

每个具有registered channel的Track显示可独立折叠的`CURVES`分组，每个channel可在当前Editor session隐藏或显示。每个Clip只在自己的StartFrame到EndFrame内绘制曲线、key与边界，重叠Clip不预混。Curve Lane使用`AnimationCurve.Evaluate`绘制实际Hermite/weighted结果，支持单选、Shift追加、框选、整数Timeline frame吸附的单/多key拖动、双击/右键新增、删除、复制粘贴、数值Inspector、wrap mode、tangent handle、Auto/Clamped Auto/Linear/Constant/Free、WeightedMode和unbounded vertical pan/zoom/Frame Selected。

Curve手势使用owner revision保护：pointer capture期间只改本地完整curve草稿，Pointer Up或Capture Out一次提交，Pointer Cancel丢弃；外部替换owner时拒绝陈旧key index。所有提交都进入descriptor MutationAdapter与统一Timeline Undo/dirty/Rebind/Preview刷新入口。Foot Placement不再有专用Curve View写路径。

Curve Editor只统一authoring。Animation控制曲线、MotionCurve、MotionWarp与Camera继续进入各自现有Compiler/Projection/Program/Presentation消费者，不新增`GenericTimelineCurveRuntime`。RootMotionCurveAsset继续是外部烘焙资产，导入AnimationClip内部骨骼、BlendShape与属性曲线不进入Catalog；Distance Matching仍未安装，未创建占位channel或fallback。

## 3. 运行时链路

```text
Committed producer sample
  -> AnimationPlaybackLifecycle
  -> AnimationMarkerSyncRuntime
  -> effective presentation time
  -> CharacterAnimationPlaybackRuntime
  -> Animancer
```

- `AnimationPlaybackLifecycle` 仍唯一拥有 Current、Pending、Outgoing、Retired 与可见关系。
- 运行时只在实际 outgoing Current 与 incoming target 之间按两侧 SyncRole 解析 source/follower；默认 outgoing 领导，incoming `AlwaysLeader` 或 outgoing `AlwaysFollower` 时反向。
- `AnimationMarkerSyncRuntime` 将 source 的当前有向 marker segment 与 fraction 映射到 target 的同一 pair occurrence。
- 重复 pair 选择离 target raw time 最近的 occurrence，再按稳定索引裁决；关系存续期间保持选择。
- 长表现帧跨越多个 source segment 时按顺序推进全部 pair，不丢失 cycle。
- source 退休时 target 以当前 raw/effective anchor 重建，不把旧 source 继续当权威。
- 两个 `AlwaysLeader` 或两个 `AlwaysFollower` 作为 typed role conflict 失败；反向 relation 建立前会清除 outgoing 的旧上游 relation，求值按真实依赖递归而不假设 generation 顺序。
- 非法 Projection、缺失 segment、coverage 耗尽、relation 环与时间倒退使用带 `Reason` 和 `PlaybackId` 的 typed exception；正式 trace 在重新抛出前发布同一稳定失败原因，不继续播放或退回 normalized time。
- 输出只改动画表现采样时间；Animancer 继续拥有 fade、weight 与 child synchronization policy。

## 4. Preview 与诊断

- Timeline Authoring Preview 只加载正式 Presentation Projection，并复用正式 playback lifecycle、marker runtime 与 Animancer presenter。
- Preview 不创建 Simulation Session，不执行 TreeClip、Action、MotionCurve、MotionWarp、WorldSolver 或 Gameplay fact。
- Preview 的运行中重置会提交正式 Release；对象销毁只释放 playback adapter，不再提交命令、采样或 Evaluate 已失效的 PlayableGraph。
- Inspector 可选择同 Layer/Group 的 source producer，显示 raw/effective time、pair、fraction、cycle、occurrence、relation depth、lifecycle 与 reason。
- Live Debug 只读取正式 runtime snapshot，不重新计算 marker 映射。

## 5. Agent v14

- Full Snapshot与Patch唯一schema为 `agent-character-controller-synthesis.v14`。
- Snapshot 对每个 AnimationTrack 输出 mode、group、topology、SyncRole、marker stable identity、frame 与 callsite。
- Marker Sync部分提供四个typed operation：
  - `configure_animation_track_marker_sync`
  - `ensure_animation_sync_marker`
  - `move_animation_sync_marker`
  - `delete_animation_sync_marker`
- Lowerer、handler 与 validator 使用 Timeline、Track、Marker stable identity；没有 v13及更早 reader、alias、converter 或兼容输出。
- 当前Marker Timeline Editor直接复用相同AnimationTrack authoring API；Marker UI完善本身不需要新增operation。
- Foot Placement专用curve operation已删除；唯一`configure_timeline_curve_channel`按Timeline、Track、Clip stable identity、registered ChannelId与完整curve原子替换。Snapshot输出curve domain、unit、wrap mode和全部Keyframe字段，不为key创建持久identity。
- Agent Validator 复用 `CharacterAnimationMarkerSyncAuthoringContext`，不维护第二套业务规则。

## 6. Corin 迁移结果

| Timeline | Playback | Sync | Marker |
|---|---|---|---|
| WalkLoop | Loop | `MarkerGroup / Locomotion.Gait / Cyclic / CanBeLeader` | `RightFootContact@0`, `LeftFootContact@18` |
| RunLoop | Loop | `MarkerGroup / Locomotion.Gait / Cyclic / CanBeLeader` | `RightFootContact@0`, `LeftFootContact@15` |
| Idle | Loop | `None` | 无 |
| WalkStart | Once | `None` | 无 |
| RunStart | Once | `None` | 无 |
| RunEnd | Once | `None` | 无 |
| MovingTurn | Once | `None` | 无 |
| Attack1..Attack5 | Once | `None` | 无 |
| DodgeBack、DodgeForward | Once | `None` | 无 |

WalkLoop 与 RunLoop 的左右支撑帧由实际动画骨骼采样确认：WalkLoop 为 36 frame 周期，RunLoop 为 30 frame 周期。WalkStart、RunStart、RunEnd与MovingTurn的正式Animation Clip和Once call site已由v14 Snapshot确认，但资源没有可作为业务真相的脚接触标注，无法可靠确认frame 0到DurationFrame的完整`Locomotion.Gait`有向pair；它们因此继续显式为`None`。其余Action/Dodge producer同样没有独立共同姿态Marker业务。没有按状态名、clip名、骨骼曲线极值或combo window猜Marker，也没有生成伪Timeline、伪clip或伪marker。

正式v14事务使用同一Patch完成dry-run与apply：四个有限Locomotion track重申`None`边界，并通过`configure_timeline_curve_channel`原子重写WalkLoop现有Foot Placement Weight完整curve。apply期间Unity插件因domain reload断开等待连接，但随后重新导出的source revision已从`f73a622d...`更新为`85d2a904...`，Walk/Run Marker identity保持不变，Float32 Program hash更新为`f2e4bb55...`，再次正式validate通过。

Corin 正式 stable identity：

- Walk Timeline：`e7ea1649-1085-47f2-b9ab-4b013ecb78b3`
- Walk Track：`762b4d39-92af-42f3-b8c0-7ac9be15afb7`
- Walk Right：`0d878358-7b77-44de-ab40-881a842236b6`
- Walk Left：`ec180c0d-dd78-45fd-89e7-8a8e5dacd546`
- Run Timeline：`68286aed-b84f-4b77-8906-a43806c41bfa`
- Run Track：`e9e8b58c-4813-4b9f-9e32-8b9f2b57d3f9`
- Run Right：`409be7a1-0618-49e2-8094-b52c4ae28634`
- Run Left：`633f740f-168c-4b64-abe1-4222817f0cee`

## 7. 边界确认

- Gameplay logic time、Timeline gameplay facts、Motion、MotionWarp、Foot Placement 与网络状态不读取 effective presentation time。
- Marker Sync 不决定攻击窗口、取消窗口、脚底接触或 root motion 位移。
- 没有 `AnimationGaitPhaseMatcher`、`CyclicMarkers`、固定 phase offset、locomotion 专用同步字段或第二套 marker registry。
- 新 Producer 类型只要进入同一 Projection、Lifecycle 与 Layer playback 链，就可使用同一 MarkerGroup 能力；不需要修改 Marker Sync runtime。
- Marker Sync只使用命名Point Marker。单一`Foot Placement Weight`属于Continuous Curve，未来Distance Matching距离曲线属于独立能力，三者不得互相解释或形成fallback。
