# Change: 完善 Timeline Point Marker 与 Continuous Curve 作者层

## Why

当前动画表现链已经分开了 Gameplay 选择、Timeline 采样、播放生命周期和 Animancer 混合：

```text
Program/Session 提交 producer 与原始 Timeline time
  -> CharacterAnimationPlaybackRuntime 在 PresentationFrame 重采样
  -> AnimationPlaybackLifecycle 管理 Pending/Current/Outgoing/Retired
  -> Animancer 执行 state、layer、fade 和最终 pose
```

这条链能够在逻辑 Tick 之间连续播放动画，但 producer 切换仍直接使用目标 Timeline 自己的原始时间。Walk 切 Run 时会突然换脚；RunStart、RunEnd、MovingTurn 等有限时长动画与循环 locomotion 切换时也无法表达共同支撑相位；某些动作动画若确实需要按共同姿态点衔接，同样没有通用作者能力。

原 `add-locomotion-gait-phase-matching` 提案把能力限制为 `WalkLoop/RunLoop`，并只在切换瞬间计算一次时间偏移。这个范围和算法都不完整：不同时长的动画在 fade 期间会继续漂移，有限时长动画完全无法加入，而且系统名称把通用动画同步误写成 locomotion 专用业务。

本 change 将其整体替换为 producer 级 `Animation Marker Sync`。每个可达 AnimationTrack 都显式选择不参与或加入一个 Marker Group；循环与有限时长序列使用同一组命名 marker 语义。发生同层、同组 handoff 时，运行时只在真实 outgoing Current 与 incoming target 之间按 `SyncRole` 解析一次映射方向，并在共同可见期持续同步；source 退休后，follower 从最后映射时间连续推进。Gameplay 状态、动作窗口、Motion、WorldSolver、Snapshot 与网络结果完全不改变。

现有实现已经闭合 Marker Sync 运行时、Projection、基础子轨和 Inspector 列表，但 Timeline 作者体验仍不完整：Marker 子轨不能直接在空白帧创建点，名称复用依赖作者手输，循环末端到下一周期首点的闭合关系不可见，拖动提交与 Preview 刷新也没有形成统一交互合同。更严重的是，旧文档曾把步调、Foot Placement 与 Distance Matching 的连续参数混写成曲线，作者无法从界面直接判断“哪一帧左脚落地、哪一帧右脚落地”。

因此本 change 重新打开作者层工作，先把 Timeline 内容收敛为三种清楚的语义，再同时完善 Marker 点和曲线编辑：占据时间区间的 `Span Clip`、发生在单帧的 `Point Marker`、表达连续数值的 `Continuous Curve`。Marker Sync 只使用 Point Marker；Animation、MotionCurve、MotionWarp、Camera等Clip已经拥有的正式`AnimationCurve`字段统一作为typed Continuous Curve Channel进入Timeline；未来 Distance Matching 仍需先建立自己的正式curve channel与runtime capability。三类内容共享时间坐标、选择、吸附、Undo和刷新机制，但不共享数据所有权或运行时语义。

当前曲线作者链同样不完整。`TimelineAnimationCurveLaneView`硬编码只认识Animation Clip的`FootPlacementCurve`；Animation Clip自身的Weight/Ease、MotionCurve Clip的Position/Yaw/Weight/Ease、MotionWarp Clip的Position/Yaw Progress和Camera Clip的Weight/Ease仍依赖分散Inspector，无法在同一时间轴看到真实曲线、key和Clip边界。此次抽象必须把这些已存在的正式曲线收敛到一个显式typed channel catalog和同一个Curve Lane交互实现，不能只把Foot Placement换个标题就算完成。

“每个 Timeline 都支持”不等于“每个动画都自动做步态同步”。Attack 连段仍由 Action window、State transition 与目标 ClipIn 决定；Dodge、Attack、Turn、Start、End 只有在作者明确声明共同 Marker Group 且 marker 契约完整时才参与。Camera、TreeClip、MotionCurve 等没有 AnimationTrack 的 Timeline 不产生动画同步配置。

## What Changes

- 将 change id 从 `add-locomotion-gait-phase-matching` 更名为 `add-timeline-animation-marker-sync`，删除 locomotion-only 与一次性 offset 设计。
- 建立 Timeline 作者内容分类：`Span Clip`、`Point Marker`、`Continuous Curve`。抽象只统一 Editor 交互与事务，不新增通用运行时 Track、不改变现有 Timeline Tick，也不把三类内容序列化到同一宽模型。
- 建立显式typed Curve Channel Catalog。每个正式Clip类型通过代码注册稳定ChannelId、显示名、颜色、owner类型、时间域、值域、单位、默认曲线、读写adapter与校验器；Editor不得通过反射、字段名扫描或任意字符串发现曲线。
- 首批Catalog覆盖现有正式曲线：Animation Clip的Weight、Ease In、Ease Out、Foot Placement Weight；MotionCurve Clip的Weight、Position X/Y/Z、Yaw、Ease In/Out；MotionWarp Clip的Position Progress与Yaw Progress；两类Camera Clip的Weight与Ease In/Out。
- 将AnimationTrack专用`TimelineAnimationCurveLaneView`收敛为可复用Curve Group/Channel Lane。每个Track只显示其Clip实际注册的channel；同一channel中每个Clip保持自己的曲线owner和StartFrame..EndFrame显示区间，重叠Clip不得合并成一条作者曲线。
- Curve Lane必须绘制实际`AnimationCurve.Evaluate`插值结果、原始key、Clip边界、值域参考线与单位；bounded channel使用固定范围，Position/Yaw等unbounded channel使用独立vertical fit/scale，不得一律Clamp到`[0,1]`。
- Curve key必须支持点击/框选、双击或右键新增、拖动时间和值、数值Inspector、删除、复制与粘贴，以及Auto/Clamped Auto/Linear/Constant/Free和weighted tangent编辑。所有key字段包括time、value、in/out tangent、in/out weight与WeightedMode必须无损保留。
- Curve key拖动与Marker拖动复用同一本地草稿和单次Undo事务；横轴按Channel时间域映射并吸附Timeline整数帧，纵轴按typed value domain约束。编辑结果只能通过channel对应的正式owner mutation API提交。
- 为 AnimationTrack 增加显式 `Unspecified`、`None`、`MarkerGroup` 模式；发布前必须消除 `Unspecified`。
- 为 MarkerGroup producer 增加 `SyncGroupId`、`Finite/Cyclic` 序列拓扑、`CanBeLeader/AlwaysLeader/AlwaysFollower` 同步角色，以及带稳定 authoring identity、语义 MarkerId 和 Timeline frame 的 marker 序列。
- marker 归 AnimationTrack/producer 唯一拥有；TimelineNode 继续只拥有 Once/Loop 调用语义。shared Timeline 的全部调用点必须与 track 拓扑一致，不能按调用节点覆盖同步配置。
- 建立唯一作者校验：identity、frame、有限边界、循环回绕、动画输出覆盖、调用点 Once/Loop、一致的有向 marker segment 集和同层 group 兼容性。
- 将合法配置编入 CharacterPresentationProjection；同步数据只属于 source revision 与表现资源绑定，不进入 Semantic IR operation payload、Float32/Fixed Program ABI、Character state、StateHash 或网络协议。
- 新增通用 `AnimationMarkerSyncRuntime`。它在 source/target 共同可见期间每个 PresentationFrame 按同名相邻 marker 与 segment fraction 持续映射 target 时间，而不是保存一次性固定 offset。
- relation 只由当前 AnimationPlaybackLifecycle 的真实 Current 与 incoming target 建立；同步角色决定本次 handoff 由 outgoing 还是 incoming 领导，快速连续切换按实际 playback identity 形成无环依赖图，不读取 StateMachine、Action 名称、Priority 或 Graph edge。
- source 退休时将 target 重基线到最后 effective time，之后继续使用自身 Timeline raw delta，避免时间跳回。
- Animancer 继续唯一拥有 transition、fade、weight、mixer 与最终 pose；项目不启用 Animancer 自动 normalized-time 同步，也不实现第二套 crossfade 权重。
- 在独立 Timeline Editor 中为每个 AnimationTrack 增加固定、可折叠的 `SYNC MARKERS` 子轨；`None` 显示只读禁用摘要，`MarkerGroup` 在子轨显示可选择、拖动的 marker。子轨只是同一 Track 的编辑投影，不创建第二种可执行 Track、FootPhase 资产、同步 Profile 或第三个动画窗口。
- 在 Marker 子轨空白帧提供右键新增；候选名称从当前 Definition 中同 Layer、同 Sync Group 的正式 Track 动态投影，作者也可显式输入新名称。该候选索引只存在于 Editor，不序列化、不进入 Projection、不成为第二份 Marker registry。
- 在 Marker 点提供右键选择、定位、重命名与删除；拖动期间只更新本地预览，释放或失去 pointer capture 时以一个 Undo 事务提交，Pointer Cancel 时丢弃草稿。提交后必须显式刷新 Timeline、Inspector、Projection 状态与 Authoring Preview。
- 对 `Cyclic` 子轨显示末 Marker 到下一周期首 Marker 的闭合方向，并在 Preview 游标处突出当前有向 Marker Pair 与 fraction；`Finite` 只显示首尾覆盖，不伪造回绕。
- Marker Sync 不显示或消费步态相位曲线。Animation Clip 的 `Foot Placement Weight` 只是`CURVES`分组中的一个typed channel；Prediction、Pelvis与Rotation不恢复为逐Clip曲线。未来 Distance Matching 必须先注册独立typed channel并建立正式Projection/Program/runtime consumer，不能新增无消费者的任意曲线。
- Authoring Preview 只做 Projection + 正式动画 playback 的表现预览；Live Debug 显示真实 runtime relation、marker segment、effective time 与 detach 原因，不恢复 Preview Simulation Session。
- 将当前Agent v13原子提升为v14：保留Marker typed operation，并用统一`configure_timeline_curve_channel`替换Foot Placement专用曲线修改入口。Snapshot按owner stable identity输出可编辑typed curve channel、时间域、值域和完整key；Patch只能选择Catalog登记的ChannelId。删除v13 reader、Foot Placement专用curve operation和兼容分支。
- 迁移 Corin 全部可达 AnimationTrack：每个 producer 显式选择 `None` 或 `MarkerGroup`；WalkLoop/RunLoop 必须进入 `Locomotion.Gait`，其它 Start/End/Turn/Attack/Dodge 仅按真实动画语义与完整 marker 契约选择，不按状态名猜测。
- 重新生成匹配 source revision 的 Corin Projection 以及 Float32/Fixed Program wrapper；Program 的 Gameplay operation 语义保持不变。

## Capabilities

### Modified Capabilities

- `character-animation-presentation-authoring`：增加 AnimationTrack 唯一 Marker Sync 作者模型、校验与 Projection 投影。
- `character-animation-layer-runtime`：增加持续 marker relation、effective time 重采样、chain 与 detach 生命周期。
- `btsmtl-timeline-editor-preview`：增加 marker lane、纯表现 handoff preview 与 Live Debug 投影。
- `character-foot-placement-presentation`：将单一Foot Placement Weight迁入通用typed Curve Channel Editor，同时保持正式Projection与Pose Post Process消费链。
- `character-root-motion-curves`：让MotionCurve Clip现有Weight、Position、Yaw与Ease曲线进入同一Curve Editor，不改变Program Motion执行。
- `character-motion-warp-authoring`：让Position/Yaw Progress进入同一Curve Editor并继续复用canonical累计曲线校验。
- `character-camera-pipeline`：让Camera Clip现有Weight/Ease进入同一Curve Editor，不改变Camera runtime边界。
- `agent-character-controller-synthesis`：提升到v14，保留稳定identity的Marker操作，并增加Catalog受限的通用typed curve channel Snapshot/Patch。
- `btsmtl-agent-authoring-mcp-bridge`：透传同一v14 typed transaction，不增加任意字段写入口或curve专用旁路action。
- `character-state-timeline-authoring-loop`：迁移 Corin 全部 animation producer 的显式同步策略与首个 Locomotion group。

## Current Spec Comparison

- 现行 `character-animation-presentation-authoring` 已规定 Timeline Editor 独占 LayerId、clip、time、loop、ease、producer 内部 Weight、Marker Sync 与单一 Foot Placement Weight 曲线；本 change 只补齐 Marker 点作者体验，不把 Layer catalog 或 Animancer transition 移回 Timeline。
- 现行 `character-animation-layer-runtime` 已规定 Timeline visual time 是采样权威、Animancer 是 fade 权威；本 change 只在正式 sampler 前把 raw visual time 映射为 effective visual time，不增加 Animator/Animancer 第二时钟。
- 现行 `character-state-timeline-authoring-loop` 已明确删除旧 FootPhase 数据源；本 change 不恢复 FootPhase SO、Blackboard phase 或状态专用同步表。
- 现行 `btsmtl-timeline-editor-preview` 已要求固定 Marker Sync 子轨、整数帧拖动和正式 Undo 链，但没有规定空白处直接新增、同组名称候选、循环闭合可视化、统一 pointer capture 事务和提交后 Preview 刷新；本 change 补齐这些缺口。
- 现行`character-foot-placement-presentation`只要求Animation Clip的单一Foot Placement Weight Curve Lane；它没有提供其它Timeline Clip曲线的通用Catalog、显示、选择、tangent和Inspector合同。本change保留该曲线业务语义，但删除Editor对Foot Placement字段的硬编码。
- 现行MotionCurve、MotionWarp和Camera作者模型已经各自保存正式AnimationCurve；本change只统一它们的Editor投影和mutation入口，不改变其Compiler与Runtime语义。
- 现行 `agent-character-controller-synthesis` 与 MCP bridge 已收敛为 v13，并已包含 Marker Sync与Foot Placement curve operation；本change提升到v14，统一curve channel合同并删除Foot Placement专用operation。
- 现行 `character-foot-placement-presentation` 已收敛为每个 Animation Clip 一条 `Foot Placement Weight` 曲线；Marker Sync 不得读取该曲线作为步态相位，Foot Placement 也不得读取 Marker 作为接触真相。
- `openspec/project.md` 此前残留“每个 Clip 四条 Foot Placement 曲线”的过时描述，本轮文档更新已同步修正为单一 Weight Curve。
- `refactor-timeline-authoring-preview-to-presentation-only` 已安装纯表现 Preview 边界；本 change 只复用其 Projection与正式播放链，不恢复完整 Gameplay Preview。
- `add-predictive-foot-placement-presentation-pass` 已安装Animancer最终pose之后的脚锁、地面查询和骨盆处理；Marker Sync只选择动画采样时间，不能向IK提交plant/contact真相，两个模块不得共享或复制foot state。

## Dependencies And Sequencing

- `add-corin-targeted-motion-warp-demo`、`refactor-timeline-authoring-preview-to-presentation-only`与`add-predictive-foot-placement-presentation-pass`均已安装；本change从当前Agent v13 Snapshot和纯表现Preview边界继续，不恢复旧版本合同。
- Marker点拖动应复用已安装Continuous Curve key的本地草稿、单次提交和取消语义，但不得复用曲线数据或Foot Placement contact。
- 若其它active change同时修改Timeline行布局、选择服务或Corin同一Timeline资产，必须按文件与资产所有权串行合并，不能建立临时双入口。
- `refactor-deterministic-rollback-input-propagation` 已完成且不与同步语义耦合；Marker资产变化后只按正式流程重建匹配source revision的Fixed/Float32 wrapper。

## Out Of Scope

- 不实现 Motion Matching、Stride Warping、Distance Matching、Motion Warping、Foot IK 或预测式 Foot Placement。
- 不在Timeline中复制或重写导入AnimationClip资源内部的骨骼、BlendShape或任意属性曲线；本change只编辑Timeline Clip本身已经正式拥有并由Catalog登记的控制曲线。
- 不用 Marker Sync 决定 Attack combo window、cancel、damage、IFrame、Motion 或状态 transition。
- 不自动把所有 Attack、Dodge、Idle 或 locomotion producer放入同一个组。
- 不增加 UE 式多候选权重仲裁。当前一次 handoff 只有 outgoing Current 与 incoming target 两个候选；SyncRole 只决定这两个 playback 的映射方向。若未来引入同层多候选 BlendTree，再单独增加组内权重选主。
- 不让 AnimationTrack marker 驱动 Gameplay event；Gameplay cue/window 继续使用 TreeClip 与正式 Timeline 事实。
- 不创建全局 Marker 名称资产、Skeleton Marker registry、同步 Profile 或按字符串自动修复组契约；同组名称候选只是当前正式 authoring context 的只读 Editor 投影。
- 不允许作者创建没有typed ChannelId、值域、owner mutation、Compiler投影或Runtime消费者的任意Float Curve；扩展新业务曲线必须注册正式channel并更新对应领域合同。
- 不把通用Curve Editor变成新的运行时曲线解释器；现有Animation、Motion、MotionWarp与Camera Compiler/Projection/Program消费者保持各自唯一权威。
- 不修改 Animancer 源码，不启用 Animancer 自己的 mixer synchronization。
- 不新增测试或人工验证 task，不运行 Unity batchmode。

## Stop Conditions

- 如果任一现有Timeline曲线无法通过owner正式mutation API无损读写time、value、tangent、weight与wrap mode，停止该channel接入并补齐领域API；不得使用SerializedProperty、反射或只保存time/value的降级路径。
- 如果某条曲线无法明确声明owner、时间域、值域、Compiler投影或Runtime消费者，停止说明业务归属，不把它注册为“通用Float Curve”。
- 如果 shared Timeline 同时被 Once 与 Loop 调用，且业务确实要求同一 producer 在两个拓扑下拥有不同 marker 语义，停止说明需拆分 producer 的 tradeoff，不向 PresentationCommand 增加调用点 fallback。
- 如果现有 Projection/PlaybackId 无法在不修改 Gameplay/Network command ABI 的情况下稳定识别 relation，停止说明身份缺口，不按名称或当前 State 推导。
- 如果持续映射必须读取 Animancer state weight 才能选择 leader，停止说明当前单 Current 生命周期不足，不建立第二套动画仲裁。
- 如果 Corin 某个 one-shot 动画没有完整 marker coverage，明确配置 `None` 并记录资源缺口；不得伪造落脚点或恢复旧 FootPhase 数据。
- 如果 active Foot Placement change 已建立另一份 Timeline foot marker/contact authoring，停止并先统一所有权，不保留双数据源。

## Success Criteria

- 每个可达 AnimationTrack 都显式为 `None` 或 `MarkerGroup`，不存在运行时默认猜测。
- 每个 AnimationTrack 在 Timeline Editor 中都有固定 Marker Sync 子轨，子轨不进入 Timeline 运行时 Track 列表。
- MarkerGroup producer 显式声明同步角色；有限 Start/End/Turn 可以保持自身节奏，循环 Walk/Run 可以按 handoff 方向参与同步。
- 同层同组 handoff 在整个共同可见 fade 期间持续对齐相同 marker segment，不再使用一次性固定 offset。
- Cyclic 与 Finite producer 可通过同一 marker 合同互相映射；有限序列无回绕，循环序列只在自己的拓扑中回绕。
- source 退休后 target 从最后 effective time 连续推进，不跳回原始 Timeline time。
- 快速 `A -> B -> C` 使用实际 Current 的 effective time 建立无环 relation，不读取 StateMachine 或逻辑 priority。
- Timeline Editor、Projection、Runtime、Preview、Live Debug 和 Agent v14 使用同一作者数据与校验服务。
- Timeline Editor 明确区分 Span Clip、Point Marker 与 Continuous Curve；Marker Sync 不再以曲线、权重或 Foot Placement 参数表达。
- 作者可直接在 `SYNC MARKERS` 子轨右键新增、选择同组名称、创建新名称、拖动、重命名和删除 Marker，全部修改以单次 Timeline Undo 事务提交。
- Cyclic 子轨明确显示末点到下一周期首点的闭合关系；Preview 明确显示当前有向 Marker Pair 与 fraction。
- Marker 编辑提交后 Timeline、Inspector、校验摘要与 Authoring Preview 同步刷新，不需要重新选择 Track 才能看到结果。
- `CURVES`分组能够显示全部已登记Timeline控制曲线，而不是只显示Foot Placement；每条channel有稳定名称、颜色、值域、单位和Clip-local时间映射。
- 作者能直接新增、选择、框选、拖动、删除、复制、粘贴和数值编辑curve key，并能编辑tangent/weight；实际插值显示与Runtime保存的完整Keyframe字段一致。
- Bounded与unbounded curve使用各自typed值域，不把Position/Yaw错误Clamp到`[0,1]`。
- 同一曲线修改从UI或Agent进入同一Catalog adapter与owner mutation API，不存在Foot Placement专用Editor写入或反射字段修改。
- Animancer 仍唯一执行 fade/weight/final pose，Gameplay Program、State、Motion、Snapshot 与网络协议没有 marker sync 字段。
- Corin WalkLoop/RunLoop 完成 `Locomotion.Gait` 配置；其它 producer 按真实资源显式配置，不按 Attack/Run 等名称硬编码。
- Corin WalkStart、RunStart、RunEnd与MovingTurn必须读取真实动画后决定Finite Marker；资源满足完整coverage时完成配置，资源不满足时保持显式None并在实现清单记录缺口，不能伪造接触点。
- 仓库不存在旧 `AnimationGaitPhaseMatcher`、`CyclicMarkers`、FootPhase SO、同步 Profile、v10 Agent reader 或兼容路径。
