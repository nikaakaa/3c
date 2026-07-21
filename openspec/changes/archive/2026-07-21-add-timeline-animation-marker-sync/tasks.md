## 1. 基线与实施顺序

- [x] 1.1 使用 PowerShell UTF-8 重新读取本 change 的 proposal、design、tasks 与全部 spec delta。
- [x] 1.2 导出最新 Agent v13 Corin Full Snapshot，记录全部可达 Timeline、AnimationTrack、Clip 与 call site stable identity。
- [x] 1.3 记录每个 AnimationTrack 对应 Timeline duration、clip coverage、LayerId 和 AnimationProducerId。
- [x] 1.4 记录每个 producer 的全部 TimelineNode call site 及 Once/Loop playback mode。
- [x] 1.5 记录 CharacterPresentationProjection 从 Program producer解析 AnimationTrack binding 的唯一入口。
- [x] 1.6 记录 CharacterAnimationPlaybackRuntime raw visual time、sample demand、lifecycle Apply 与 Animancer adapter 调用顺序。
- [x] 1.7 搜索并确认仓库没有仍被读取的 FootPhase SO、registry、Blackboard phase、旧 gait matcher或同步Profile。
- [x] 1.8 确认 `add-corin-targeted-motion-warp-demo` 已完成 Corin managed-reference资产迁移，当前 Agent v13 能完整读取这些正式资产。
- [x] 1.9 确认 `refactor-timeline-authoring-preview-to-presentation-only` 已完成或先按其最终纯表现Preview边界重基线。
- [x] 1.10 记录 `add-predictive-foot-placement-presentation-pass` 与本change共同修改的Playback/Adapter/Diagnostics文件。
- [x] 1.11 确认本change可沿AnimationTrack -> Projection -> PlaybackRuntime -> Lifecycle -> Animancer闭环，不需要修改Gameplay/Network command ABI。

## 2. AnimationTrack Marker Sync 作者模型

- [x] 2.1 定义稳定 `AnimationSyncMode.Unspecified`、`None` 与 `MarkerGroup` 枚举值。
- [x] 2.2 定义稳定 `AnimationMarkerSequenceTopology.Finite` 与 `Cyclic` 枚举值。
- [x] 2.3 定义 AnimationSyncMarker，保存稳定 AuthoringId、语义 MarkerId 与整数 Timeline Frame。
- [x] 2.4 为 AnimationTrack 增加 mode、SyncGroupId、topology 与 marker集合的唯一序列化所有权。
- [x] 2.5 为 AnimationTrack 提供只读同步配置访问API。
- [x] 2.6 为 AnimationTrack 提供原子 ConfigureNone authoring API并清空残留group/topology/role/markers。
- [x] 2.7 为 AnimationTrack 提供原子 ConfigureMarkerGroup authoring API。
- [x] 2.8 为 AnimationTrack 提供按stable identity确保marker的正式authoring API。
- [x] 2.9 为 AnimationTrack 提供重命名、移动和删除marker的正式authoring API。
- [x] 2.10 让新marker通过现有authoring identity service生成唯一identity。
- [x] 2.11 让复制AnimationTrack时重新生成track、clip与marker identity并保留语义配置。
- [x] 2.12 让inline与shared Timeline使用同一作者模型，不增加call site覆盖字段。
- [x] 2.13 删除原提案中的`CyclicMarkers`命名、locomotion专用字段和固定phase offset模型。

## 3. 唯一 Marker Sync 校验服务

- [x] 3.1 建立Timeline Inspector、Compiler、Projection Builder和Agent Validator共用的唯一校验服务。
- [x] 3.2 拒绝可达AnimationTrack仍为Unspecified。
- [x] 3.3 拒绝None模式残留SyncGroupId、topology或markers。
- [x] 3.4 拒绝MarkerGroup缺少canonical SyncGroupId、topology或至少两个marker。
- [x] 3.5 拒绝空白、首尾空白或无法规范化的MarkerId。
- [x] 3.6 拒绝缺失或重复MarkerAuthoringId。
- [x] 3.7 拒绝重复frame、逆序frame和零长度segment。
- [x] 3.8 允许同一track重复MarkerId并保持每个occurrence的独立AuthoringId。
- [x] 3.9 拒绝Timeline duration非有限或不大于零。
- [x] 3.10 校验Cyclic marker位于`[0, DurationFrame)`并建立末尾回绕segment。
- [x] 3.11 校验Finite首marker位于frame 0、末marker位于DurationFrame且不建立回绕segment。
- [x] 3.12 拒绝Cyclic producer存在非Loop call site。
- [x] 3.13 拒绝Finite producer存在非Once call site。
- [x] 3.14 拒绝shared producer同时存在Once与Loop call site。
- [x] 3.15 校验marker覆盖区内AnimationTrack每个采样区间都有正式animation output。
- [x] 3.16 建立每个producer的有向MarkerId pair集合。
- [x] 3.17 校验同Layer同SyncGroup producer拥有相同有向pair集合。
- [x] 3.18 允许同一pair在producer内拥有多个occurrence并生成稳定排序。
- [x] 3.19 为每类失败定义稳定issue code、authoring path和相关identity。
- [x] 3.20 删除按Timeline名、Track名、Clip名、State名或脚骨名猜测同步语义的分支。

## 4. Timeline Editor Marker Sync 编辑

- [x] 4.1 在AnimationTrack Inspector增加SyncMode控件。
- [x] 4.2 仅在MarkerGroup模式显示SyncGroupId与Finite/Cyclic topology控件。
- [x] 4.3 增加marker列表的添加命令。
- [x] 4.4 增加marker MarkerId重命名命令。
- [x] 4.5 增加marker整数frame编辑命令。
- [x] 4.6 增加marker删除命令。
- [x] 4.7 在AnimationTrack时间轴绘制marker竖线、短标签和选中态。
- [x] 4.8 让marker拖动复用Timeline frame geometry并吸附整数frame。
- [x] 4.9 让Inspector与lane选择共享同一marker stable identity。
- [x] 4.10 让全部操作进入现有Undo、dirty、identity与RebindTimeline链。
- [x] 4.11 在对应track显示唯一校验服务返回的本地issue。
- [x] 4.12 显示同Layer同Group producer的directed pair coverage摘要。
- [x] 4.13 显示shared Timeline call site Once/Loop冲突的来源节点定位。
- [x] 4.14 保持Timeline独立窗口，不把marker编辑加入Graph页签栈。
- [x] 4.15 确认没有创建FootPhase窗口、同步Profile、第三个Presentation窗口或ScriptableObject数据源。

## 5. Presentation Projection 编译

- [x] 5.1 定义不可变Projection AnimationMarkerSyncBinding。
- [x] 5.2 定义不可变Projection Marker与SegmentOccurrence数据。
- [x] 5.3 将SyncMode、canonical GroupId、topology和duration编入对应animation producer binding。
- [x] 5.4 将marker frame规范化为按time seconds排序的marker序列。
- [x] 5.5 为Cyclic producer编译末尾到首marker的回绕segment。
- [x] 5.6 为Finite producer编译不回绕的有限segment序列。
- [x] 5.7 为每个有向MarkerId pair编译稳定occurrence索引。
- [x] 5.8 在Projection构建前运行完整Marker Sync校验。
- [x] 5.9 在Definition级建立Layer + Group兼容性索引。
- [x] 5.10 让Projection binding构造完成后不再依赖TimelineData或AnimationTrack。
- [x] 5.11 将同步配置纳入Definition source revision与Projection content hash。
- [x] 5.12 保持Gameplay Semantic operation payload不包含marker sync字段。
- [x] 5.13 保持Float32/Fixed Program ABI、Character state codec与StateHash不包含marker sync字段。
- [x] 5.14 保持PresentationCommand与ServerAuthoritative/Rollback codec不增加call site或sync字段。
- [x] 5.15 扩展AnimationPresentationBindingIndex校验sync binding与Program producer identity一致。
- [x] 5.16 更新Projection Inspector显示只读mode、group、topology、marker与segment摘要。

## 6. AnimationMarkerSyncRuntime

- [x] 6.1 定义不引用Animancer、Graph、Simulation state或Network的marker sync输入输出合同。
- [x] 6.2 定义raw/effective playback sample结构。
- [x] 6.3 定义source marker segment、fraction与target mapped time结构。
- [x] 6.4 定义以完整AnimationPlaybackId为key的SyncRelation状态。
- [x] 6.5 按Cyclic effective time定位含回绕的source segment。
- [x] 6.6 按Finite effective time定位不回绕的source segment。
- [x] 6.7 计算有限且位于`[0,1]`的source segment fraction。
- [x] 6.8 从target occurrence索引解析同一有向MarkerId pair。
- [x] 6.9 为Finite target按candidate与raw time绝对距离选择首次occurrence。
- [x] 6.10 为Cyclic target按模duration距离与最近展开cycle选择首次occurrence。
- [x] 6.11 为距离相同candidate实现frame与AuthoringId稳定tie-break。
- [x] 6.12 relation建立后固定当前occurrence，不在每帧重新选择最近候选。
- [x] 6.13 source跨marker时按有序pair推进target occurrence。
- [x] 6.14 在共同可见期每帧重新计算target effective time。
- [x] 6.15 正确规范化Cyclic target的effective cycle与local time。
- [x] 6.16 拒绝Finite target倒退、越界或缺少下一occurrence。
- [x] 6.17 为None、不同Layer、不同Group和缺少Current返回typed NotApplicable reason。
- [x] 6.18 为损坏Projection、缺segment、非有限结果和coverage exceeded返回typed Invalid reason。
- [x] 6.19 禁止runtime排序marker、使用LINQ、反射、资产路径或完整Projection扫描。
- [x] 6.20 禁止runtime读取StateMachine、Action、Motion、Transform、foot bone、priority或Network数据。

## 7. 播放生命周期与持续 relation 集成

- [x] 7.1 为AnimationPlaybackLifecycle提供按LayerId读取实际Current playback的窄只读API。
- [x] 7.2 为lifecycle提供Pending target与共同可见playback的窄只读快照，不暴露可写集合。
- [x] 7.3 保持Current/Pending/Outgoing/Retired唯一写入仍在lifecycle内部。
- [x] 7.4 将PlaybackRuntime采样拆为raw time解析、relation求值与effective producer采样三个阶段。
- [x] 7.5 为全部demanded playback在同一PresentationFrame解析raw time/cycle。
- [x] 7.6 在incoming首样本前使用selection commit前的实际Current建立relation。
- [x] 7.7 保证新generation即使producer相同也建立新的relation identity。
- [x] 7.8 保证同playback Replace保留raw sampling与relation状态。
- [x] 7.9 让Current成为Outgoing后仍作为relation source持续提供effective time。
- [x] 7.10 让target在共同可见期使用mapped time重采样整个producer。
- [x] 7.11 建立按playback generation稳定排序的relation依赖图。
- [x] 7.12 从最老source到最新target求值`A -> B -> C` chain。
- [x] 7.13 拒绝relation环、同target多source与跨layer依赖。
- [x] 7.14 source退休时保存target raw/effective continuation anchor。
- [x] 7.15 source退休后删除relation并按target raw delta连续推进effective time。
- [x] 7.16 target作为下游source时继续提供重基线后的effective time。
- [x] 7.17 在target退休、Reset与Dispose时清理relation、anchor与sample state。
- [x] 7.18 保持PendingFirstSample、RequireOutput、terminal与PresentationRetention语义不变。
- [x] 7.19 保持Animancer TransitionLibrary、FadeMode、duration modifier、easing和weight权威不变。
- [x] 7.20 保持Animancer child `DontSynchronize`，不启用自动normalized-time同步。
- [x] 7.21 删除固定phase offset、只在Play时写一次state time或locomotion专用matcher路径。

## 8. Authoring Preview 与 Live Debug

- [x] 8.1 将Marker Sync Authoring Preview建立在纯Projection与正式AnimationPlaybackRuntime之上。
- [x] 8.2 在单producer预览显示raw/effective time、当前marker pair与fraction。
- [x] 8.3 提供按stable producer identity选择同Projection、同Layer、同Group source的比较入口。
- [x] 8.4 让比较入口只生成现有preview selection/sample命令，不创建Simulation Session。
- [x] 8.5 让Preview relation复用正式AnimationMarkerSyncRuntime与lifecycle。
- [x] 8.6 删除Preview直接设置Animancer normalized time或维护独立offset/relation cache的入口。
- [x] 8.7 保持TreeClip、Action、MotionCurve、MotionWarp和WorldSolver不在Authoring Preview执行。
- [x] 8.8 定义Marker Sync runtime snapshot。
- [x] 8.9 在snapshot记录layer、source/target playback、group、pair、fraction与target occurrence。
- [x] 8.10 在snapshot记录raw/effective time、effective cycle、relation depth与lifecycle phase。
- [x] 8.11 为NotApplicable、Created、Continued、Rebased与Invalid定义稳定reason code。
- [x] 8.12 将snapshot接入现有Animation Playback Trace发布链。
- [x] 8.13 在Timeline Live Debug只读显示正式snapshot，不重新计算marker映射。
- [x] 8.14 保持RuntimeDebugSession/Host view为唯一运行时调试来源，不增加第三个调试窗口。

## 9. Agent v13 Authoring 闭环

- [x] 9.1 将Agent Snapshot、Patch、Intent与Validation根schema收敛为当前`agent-character-controller-synthesis.v13`。
- [x] 9.2 在Timeline Track Snapshot输出syncMode、syncGroupId与sequenceTopology。
- [x] 9.3 为每个marker输出AuthoringId、MarkerId与frame并保持稳定排序。
- [x] 9.4 在Snapshot输出call site playback mode与group compatibility摘要。
- [x] 9.5 定义typed `configure_animation_track_marker_sync` Patch operation与command。
- [x] 9.6 定义typed `ensure_animation_sync_marker` Patch operation与command。
- [x] 9.7 定义typed `move_animation_sync_marker` Patch operation与command。
- [x] 9.8 定义typed `delete_animation_sync_marker` Patch operation与command。
- [x] 9.9 让operation只接受Timeline、Track与Marker stable identity或前序output reference。
- [x] 9.10 在operation catalog登记全部v13 marker operation。
- [x] 9.11 扩展lowerer并一次生成immutable typed command plan。
- [x] 9.12 让dry-run与apply消费同一command plan。
- [x] 9.13 让handler只调用AnimationTrack正式authoring API。
- [x] 9.14 让handler维持Undo、dirty、identity与RebindTimeline。
- [x] 9.15 让Agent Validator复用唯一Marker Sync校验服务。
- [x] 9.16 让Agent Compile Report返回group、call site、pair coverage与frame issue。
- [x] 9.17 更新Agent emitter、operation whitelist与Snapshot exporter。
- [x] 9.18 更新MCP bridge透传v13 Snapshot、Patch、dry-run、apply与validation结果。
- [x] 9.19 确认MCP bridge未增加YAML、SerializedProperty、反射或任意字段写入。
- [x] 9.20 删除v10 reader、converter、operation alias、兼容错误提示与旧schema输出。
- [x] 9.21 更新`.codex/skills/btsmtl-agent-authoring/SKILL.md`为v13 marker事务工作流。

## 10. Corin 资产迁移

- [x] 10.1 使用v13 Agent导出最新Corin Full Snapshot。
- [x] 10.2 按stable identity建立全部可达AnimationTrack迁移清单。
- [x] 10.3 为WalkLoop与RunLoop确定真实左右支撑marker frame与有向pair序列。
- [x] 10.4 将WalkLoop与RunLoop配置为`MarkerGroup/Cyclic/Locomotion.Gait`。
- [x] 10.5 检查RunStart、RunEnd与MovingTurn实际clip是否能提供frame 0到duration的完整Locomotion.Gait pair coverage。
- [x] 10.6 对满足契约的one-shot配置`MarkerGroup/Finite`，对资源不满足者显式配置None并记录缺口。
- [x] 10.7 检查Attack1..5与Dodge实际业务是否存在需要共同姿态同步的producer组。
- [x] 10.8 仅对业务与资源都满足的Action producer建立独立Marker Group，不复用combo window语义。
- [x] 10.9 为其余全部可达AnimationTrack生成显式None Patch。
- [x] 10.10 对完整Corin v13 Patch执行正式dry-run并消除全部validation issue。
- [x] 10.11 通过正式Agent apply同一command plan。
- [x] 10.12 再次导出Snapshot并确认全部track不再是Unspecified。
- [x] 10.13 确认资产迁移未直接修改managed-reference YAML且未创建一次性migrator。
- [x] 10.14 重新生成Corin Semantic IR source revision与Float32/Fixed Program wrapper。
- [x] 10.15 重新生成Corin CharacterPresentationProjection并校验exact Presentation contract identity。
- [x] 10.16 确认Projection包含canonical marker group/segment索引且Program operation没有sync payload。
- [x] 10.17 确认没有AnimationTrack的状态未获得伪Timeline、伪clip或伪marker。

## 11. 清理与架构文档

- [x] 11.1 搜索并删除`AnimationGaitPhaseMatcher`、`CyclicMarkers`和固定phase offset旧命名或实现。
- [x] 11.2 搜索并确认没有FootPhase SO、registry、Blackboard phase或同步Profile。
- [x] 11.3 搜索并确认PresentationProfile、StateMachine edge、ActionProfile与TimelineNode未复制marker配置。
- [x] 11.4 搜索并确认Simulation state、Semantic operation、codec、StateHash与Network协议未增加marker sync字段。
- [x] 11.5 搜索并确认Animancer automatic synchronization仍禁用且没有第二动画时钟。
- [x] 11.6 搜索并确认runtime不按Walk、Run、Attack、Dodge、Left、Right或clip名称硬编码。
- [x] 11.7 搜索并确认Preview没有恢复Simulation Source、Pipeline、WorldSolver或Gameplay evaluator。
- [x] 11.8 搜索并确认Foot Placement不读取marker sync作为contact/plant真相。
- [x] 11.9 删除Agent v10文档、parser和兼容分支。
- [x] 11.10 更新`openspec/project.md`说明AnimationTrack Marker Sync与continuous relation链。
- [x] 11.11 更新current specs的最终Agent schema identity为v13并删除v12及更早兼容路径。
- [x] 11.12 更新实现清单，记录Finite/Cyclic、SyncRole pairwise leader、continuous relation与explicit None边界。
- [x] 11.13 更新其它active change中对旧`add-locomotion-gait-phase-matching`名称和Preview边界的引用。

## 12. 编译与 OpenSpec 校验

- [x] 12.1 使用`--disable-build-servers /nr:false /p:UseSharedCompilation=false`编译`BTSMTL.Timeline.csproj`并立即shutdown build server。
- [x] 12.2 使用相同参数编译`BTSMTL.Timeline.Editor.csproj`并立即shutdown build server。
- [x] 12.3 使用相同参数编译`ThirdPersonClient.Runtime.csproj`并立即shutdown build server。
- [x] 12.4 使用相同参数编译`ThirdPersonClient.Editor.csproj`并立即shutdown build server。
- [x] 12.5 使用相同参数编译`Assembly-CSharp.csproj`并立即shutdown build server。
- [x] 12.6 使用相同参数编译`Assembly-CSharp-Editor.csproj`并立即shutdown build server。
- [x] 12.7 运行`openspec validate add-timeline-animation-marker-sync --strict --no-interactive`。
- [x] 12.8 确认未运行Unity batchmode且未新增测试或人工验证task。
- [x] 12.9 确认全部实现、迁移与清理真实完成后再将本清单逐项标记为`[x]`。

## 13. Marker 子轨与同步角色文档补全

- [x] 13.1 对照现有实现重新记录Timeline marker overlay与文档marker lane的不一致。
- [x] 13.2 在proposal中明确固定Marker Sync子轨不是第二种Timeline Track。
- [x] 13.3 在design中定义CanBeLeader、AlwaysLeader与AlwaysFollower的pairwise解析规则。
- [x] 13.4 在authoring spec中增加SyncRole唯一所有权、None清理与Projection要求。
- [x] 13.5 在runtime spec中增加反向relation与冲突角色失败语义。
- [x] 13.6 在Timeline Editor spec中增加每个AnimationTrack固定子轨与组合行重排要求。

## 14. Timeline Editor 固定 Marker Sync 子轨

- [x] 14.1 定义普通Track与AnimationTrack组合行的唯一布局度量。
- [x] 14.2 让TimelineTrackView为每个AnimationTrack创建固定Marker Sync子轨背景。
- [x] 14.3 让None子轨显示明确的禁用状态。
- [x] 14.4 让MarkerGroup子轨显示Group、Topology与SyncRole摘要。
- [x] 14.5 将TimelineAnimationMarkerView从clip row移入Marker Sync子轨。
- [x] 14.6 保持marker横坐标与Timeline frame几何一致。
- [x] 14.7 让Track Handle显示与子轨对应的只读摘要区域。
- [x] 14.8 让Track View与Track Handle使用同一组合行高度。
- [x] 14.9 将Track重排目标索引改为基于组合行边界计算。
- [x] 14.10 保持普通Track的原30px行高与既有拖动行为。
- [x] 14.11 保持marker选择、拖动、Inspector与Live Debug入口不变。
- [x] 14.12 确认未向TimelineData.Tracks新增Marker Track或第二份marker数据。

## 15. SyncRole 作者、Projection、Runtime 与 Agent v13

- [x] 15.1 在AnimationTrack增加Unspecified、CanBeLeader、AlwaysLeader、AlwaysFollower角色。
- [x] 15.2 让ConfigureNone原子清空SyncRole。
- [x] 15.3 让ConfigureMarkerGroup必须接受合法SyncRole。
- [x] 15.4 扩展唯一authoring validator拒绝缺失角色。
- [x] 15.5 将SyncRole编入AnimationMarkerSyncBinding。
- [x] 15.6 更新Projection builder与只读Inspector显示角色。
- [x] 15.7 定义pairwise leader/follower解析器和typed冲突原因。
- [x] 15.8 支持incoming AlwaysLeader建立反向relation。
- [x] 15.9 在反向relation前清理outgoing旧上游relation，禁止同follower双source。
- [x] 15.10 让relation求值不再假设leader generation早于follower。
- [x] 15.11 保持relation环检测、退休重基线与Animancer fade权威。
- [x] 15.12 将Agent Snapshot、Patch与Validation schema收敛到当前v13。
- [x] 15.13 在Snapshot与compile report输出SyncRole。
- [x] 15.14 扩展configure marker sync typed operation接受SyncRole。
- [x] 15.15 更新lowerer、handler、validator、emitter与MCP bridge到同一v13合同。
- [x] 15.16 删除v11 schema接受路径与兼容分支。
- [x] 15.17 更新btsmtl-agent-authoring skill的v13操作示例。

## 16. Corin 配置、产物与校验

- [x] 16.1 通过正式Agent v13导出Corin最新Snapshot。
- [x] 16.2 将WalkLoop与RunLoop配置为CanBeLeader并保留现有Cyclic marker。
- [x] 16.3 确认其余可达AnimationTrack继续显式None且不保留SyncRole残留。
- [x] 16.4 记录RunStart、RunEnd、MovingTurn缺少经确认完整finite marker coverage，当前不伪造配置。
- [x] 16.5 对同一v13 Patch执行dry-run与apply。
- [x] 16.6 再次导出Snapshot并运行正式Agent validate。
- [x] 16.7 重建Corin Projection与Float32/Fixed wrapper并核对source revision。
- [x] 16.8 更新implementation inventory与current specs的v13、child lane和SyncRole真相。
- [x] 16.9 编译BTSMTL Timeline Runtime与Editor程序集并立即shutdown build server。
- [x] 16.10 编译ThirdPersonClient Runtime与Editor程序集并立即shutdown build server。
- [x] 16.11 编译Assembly-CSharp与Assembly-CSharp-Editor并立即shutdown build server。
- [x] 16.12 运行`openspec validate add-timeline-animation-marker-sync --strict --no-interactive`。
- [x] 16.13 确认未运行Unity batchmode、未新增测试且未创建分裂Track或fallback。

## 17. Timeline 作者内容抽象

- [x] 17.1 盘点现有Clip、Marker与Curve View各自重复的时间坐标、选择、吸附、pointer capture、Undo和刷新代码。
- [x] 17.2 定义Editor-only `Span Clip`、`Point Marker`、`Continuous Curve`三类时间语义和最小交互合同。
- [x] 17.3 明确Span Clip继续由正式Track持有，Animation Sync Point Marker继续由AnimationTrack持有，各Continuous Curve继续由原Animation、MotionCurve、MotionWarp或Camera Clip持有。
- [x] 17.4 抽取唯一Timeline frame geometry转换与整数帧吸附服务，并让三类内容复用。
- [x] 17.5 抽取stable owner/element identity驱动的选择与Inspector定位合同。
- [x] 17.6 抽取pointer capture期间本地草稿、Pointer Up或Capture Out单次提交、Pointer Cancel丢弃的编辑事务合同。
- [x] 17.7 抽取提交后dirty、RebindTimeline、唯一校验摘要与Authoring Preview刷新的统一入口。
- [x] 17.8 保持各领域mutation API独立，禁止引入统一宽DTO、反射字段写入或第二份TimelineData。
- [x] 17.9 确认Editor抽象未进入Timeline Runtime Track列表、Program、Projection runtime schema或Tick执行路径。
- [x] 17.10 删除被统一交互合同替代的Marker与Curve重复拖动提交代码。

## 18. UE式 SYNC MARKERS 点编辑

- [x] 18.1 将每个AnimationTrack的Marker子轨标题统一为`SYNC MARKERS`并提供稳定折叠状态。
- [x] 18.2 让折叠摘要显示SyncMode、Group、Topology、SyncRole与Marker数量且不修改作者数据。
- [x] 18.3 为Marker子轨空白帧增加右键`Add Sync Marker`入口。
- [x] 18.4 从当前Definition authoring context按LayerId与canonical SyncGroupId投影已使用MarkerId候选。
- [x] 18.5 对候选MarkerId执行稳定去重与排序，不序列化Editor索引。
- [x] 18.6 在组内没有候选或作者需要新语义时提供显式新MarkerId输入。
- [x] 18.7 让新增命令通过正式AnimationTrack authoring API创建新MarkerAuthoringId并使用右键所在整数frame。
- [x] 18.8 为Marker点增加右键选择与Inspector精确定位。
- [x] 18.9 为Marker点增加右键重命名并复用唯一MarkerId校验。
- [x] 18.10 为Marker点增加右键删除并按MarkerAuthoringId调用正式API。
- [x] 18.11 将Marker拖动改为pointer capture期间仅更新本地frame草稿。
- [x] 18.12 让Pointer Up或意外Capture Out只提交最后frame并生成一个Undo事务。
- [x] 18.13 让Pointer Cancel恢复原frame且不写入资产。
- [x] 18.14 在Marker提交后同步刷新Timeline布局、Inspector、pair coverage、Projection stale状态与Authoring Preview。
- [x] 18.15 为Cyclic子轨显示末Marker到下一周期首Marker的有向闭合提示。
- [x] 18.16 为Finite子轨显示首尾coverage且禁止绘制回绕提示。
- [x] 18.17 在Authoring Preview游标处突出当前有向Marker Pair与fraction。
- [x] 18.18 保持Marker子轨不进入`TimelineData.Tracks`、不拥有独立AuthoringId、不接受Clip且不执行Tick。

## 19. Typed Curve Channel Catalog与正式mutation

- [x] 19.1 盘点Timeline全部正式`AnimationCurve`字段及其owner、Compiler和Runtime消费者。
- [x] 19.2 定义稳定`TimelineCurveChannelId`，禁止使用显示名、C#字段名或SerializedProperty path作为identity。
- [x] 19.3 定义Curve owner type、display name、color、time domain、value domain、unit和default curve descriptor。
- [x] 19.4 定义bounded与unbounded value domain，包含最小值、最大值、零线与显示单位。
- [x] 19.5 定义Timeline frame与Clip-local normalized time的双向映射合同。
- [x] 19.6 定义完整Curve payload，保留pre/post wrap mode与全部Keyframe字段。
- [x] 19.7 建立显式代码注册的唯一Curve Channel Catalog，不使用反射或字段扫描。
- [x] 19.8 注册Animation Clip Weight、Ease In、Ease Out与Foot Placement Weight channel。
- [x] 19.9 注册MotionCurve Clip Weight、Position X/Y/Z、Yaw与Ease In/Out channel。
- [x] 19.10 注册MotionWarp Clip Position Progress与Yaw Progress channel。
- [x] 19.11 注册CameraStateClip与CameraResponseClip的Weight与Ease In/Out channel。
- [x] 19.12 为Animation Clip增加按ChannelId读取完整curve副本的正式API。
- [x] 19.13 为Animation Clip增加按ChannelId原子替换完整curve的正式mutation API。
- [x] 19.14 为MotionCurve Clip增加对应typed curve读取与mutation API。
- [x] 19.15 为MotionWarp Clip增加对应typed curve读取与mutation API。
- [x] 19.16 为CameraStateClip与CameraResponseClip增加对应typed curve读取与mutation API。
- [x] 19.17 让每个descriptor连接owner领域唯一validator，不在Curve Editor复制单调、端点、范围或最小key规则。
- [x] 19.18 明确RootMotionCurveAsset继续是外部烘焙资产，不复制为Timeline inline channel。
- [x] 19.19 明确导入AnimationClip内部骨骼、BlendShape与属性曲线不进入Catalog。
- [x] 19.20 拒绝只有显示名但缺少owner mutation、validator或runtime consumer的任意Float Curve注册。
- [x] 19.21 删除`TimelineFootPlacementWeightCurve`这类Editor硬编码channel定义。

## 20. 通用 CURVES 分组与Curve Lane

- [x] 20.1 将现有Animation专用Curve Lane拆为Curve Group、Channel Lane、Curve Renderer与Interaction Owner。
- [x] 20.2 为具有registered channel的Track创建固定存在、可折叠的`CURVES`分组。
- [x] 20.3 让Track Handle按ChannelId显示颜色swatch、名称、单位和值域摘要。
- [x] 20.4 为每个ChannelId创建独立lane并允许逐channel隐藏或显示。
- [x] 20.5 让channel显示状态只属于Editor session，不写入Timeline资产或Program。
- [x] 20.6 让每个Clip只在自身StartFrame..EndFrame区间绘制自己的curve背景与边界。
- [x] 20.7 让重叠Clip保持独立curve、key和selection，不在作者层预混。
- [x] 20.8 使用完整AnimationCurve Evaluate结果绘制实际Hermite与weighted插值。
- [x] 20.9 绘制原始key、选中key、tangent handle与当前游标sample。
- [x] 20.10 为bounded channel绘制typed min/mid/max参考线。
- [x] 20.11 为unbounded channel绘制零线、单位并实现当前可见内容vertical fit。
- [x] 20.12 为unbounded channel实现独立vertical pan与zoom，不改变Timeline主横轴。
- [x] 20.13 让曲线重绘采样数按可见像素有界并复用绘制buffer。
- [x] 20.14 让单击选择一个key，Shift点击追加或移除选择。
- [x] 20.15 实现框选多个curve key并保持owner/channel边界。
- [x] 20.16 实现双击空白处与右键`Add Key`并按descriptor映射time/value。
- [x] 20.17 实现单key拖动并按Timeline整数frame吸附横轴。
- [x] 20.18 实现多key按同一frame/value delta拖动并保持合法排序。
- [x] 20.19 实现Delete与右键删除，由领域validator决定最小key合法性。
- [x] 20.20 实现完整Keyframe复制与粘贴，仅允许兼容time/value domain。
- [x] 20.21 对不兼容channel粘贴返回明确错误，不Clamp或换算单位。
- [x] 20.22 在Timeline Inspector显示选中channel、owner Clip与curve revision。
- [x] 20.23 在Inspector精确编辑Timeline frame、normalized time与value。
- [x] 20.24 在Inspector精确编辑in/out tangent、in/out weight与WeightedMode。
- [x] 20.25 在Curve Lane显示并拖动tangent handle。
- [x] 20.26 提供Auto、Clamped Auto、Linear、Constant、Free与Weighted tangent context action。
- [x] 20.27 实现`Frame Selected`以适配当前channel的vertical view且不改变Timeline横轴。
- [x] 20.28 让curve手势在pointer capture期间只修改本地完整curve草稿。
- [x] 20.29 让Pointer Up或意外Capture Out以一个Undo事务提交最后草稿。
- [x] 20.30 让Pointer Cancel丢弃草稿并恢复owner当前curve。
- [x] 20.31 在owner revision被外部替换时清除临时key选择并拒绝陈旧index提交。
- [x] 20.32 提交后重新读取owner并刷新Timeline、Inspector、validation、Projection stale状态与可用Preview。
- [x] 20.33 删除Foot Placement专用Curve View写入路径并迁移到同一Channel Lane。
- [x] 20.34 保持Curve Group不进入`TimelineData.Tracks`、不执行Tick且不保存第二份curve。

## 21. Marker、Curve、Foot Placement与Distance边界收口

- [x] 21.1 搜索并删除仍把Marker Sync称为phase curve、gait curve或Foot Placement curve的Editor文本与文档。
- [x] 21.2 确认Animation Sync只读取命名Point Marker与相邻segment fraction。
- [x] 21.3 确认每个Animation Clip只保留单一`Foot Placement Weight`策略曲线，Prediction、Pelvis与Rotation不恢复为逐Clip曲线。
- [x] 21.4 确认Foot Placement Runtime不读取Marker作为contact或plant真相。
- [x] 21.5 确认Marker Sync Runtime不读取任何Curve Channel。
- [x] 21.6 确认Curve Channel Catalog不进入Player runtime作业务分派。
- [x] 21.7 确认Animation、MotionCurve、MotionWarp与Camera继续使用各自唯一Compiler/Projection/Program消费者。
- [x] 21.8 确认仓库没有独立Marker catalog、FootPhase资产、同步Profile或第二份marker registry。
- [x] 21.9 让`SYNC MARKERS`与`CURVES`独立折叠并保持Clip行不被覆盖。
- [x] 21.10 明确Distance Matching仍为独立未安装能力，不增加占位曲线、fallback或共享runtime字段。
- [x] 21.11 明确Timeline不复制导入AnimationClip内部骨骼、BlendShape或属性曲线。
- [x] 21.12 搜索并删除反射Curve发现、SerializedProperty curve写入与未知ChannelId fallback。

## 22. Agent v14 Marker与Curve作者闭环

- [x] 22.1 将Snapshot、Patch、Intent与Validation根schema原子提升为`agent-character-controller-synthesis.v14`。
- [x] 22.2 保留现有Marker Sync typed operation并更新到v14唯一catalog。
- [x] 22.3 在Snapshot按Curve owner stable identity输出全部registered ChannelId。
- [x] 22.4 为每个channel输出time domain、value domain、unit与wrap mode。
- [x] 22.5 为每个channel输出完整有序Keyframe字段且不生成Key AuthoringId。
- [x] 22.6 定义唯一typed `configure_timeline_curve_channel` operation与command。
- [x] 22.7 让curve operation只接受owner stable identity、registered ChannelId和完整curve payload。
- [x] 22.8 让lowerer拒绝未知ChannelId、字段名目标、key index目标和不兼容domain。
- [x] 22.9 让dry-run与apply消费同一immutable command plan。
- [x] 22.10 让handler只调用Catalog descriptor MutationAdapter与owner正式API。
- [x] 22.11 让Agent Validator复用Marker及curve owner领域唯一validator。
- [x] 22.12 删除`configure_animation_foot_placement_weight_curve`专用operation与handler。
- [x] 22.13 删除v13及更早reader、converter、alias与兼容错误分支。
- [x] 22.14 更新Snapshot exporter、Patch DTO、lowerer、handler、validator、emitter与operation whitelist。
- [x] 22.15 更新MCP bridge透传同一v14 generic transaction，不新增Curve专用action。
- [x] 22.16 更新btsmtl-agent-authoring skill为v14 Marker与typed Curve Channel工作流。

## 23. Corin有限Locomotion Marker配置

- [x] 23.1 使用Agent v14重新导出Corin完整Snapshot并确认WalkLoop与RunLoop现有Marker identity不变。
- [x] 23.2 读取WalkStart、RunStart、RunEnd与MovingTurn实际Animation Clip、Timeline duration和Once call site。
- [x] 23.3 从真实动画姿势确认每个有限producer能否覆盖frame 0到DurationFrame的`Locomotion.Gait`有向Marker Pair。
- [x] 23.4 对具有完整真实coverage的有限producer确定MarkerId、frame、Finite topology与SyncRole。
- [x] 23.5 若任一有限producer缺少可确认的完整coverage，停止该producer迁移并在implementation inventory记录资源缺口，不伪造Marker。
- [x] 23.6 使用v14 Marker typed operation生成唯一Corin Patch。
- [x] 23.7 对同一Patch执行正式dry-run并消除Marker Group、call site、coverage与pair contract错误。
- [x] 23.8 通过正式Agent apply同一command plan，不直接修改managed-reference YAML。
- [x] 23.9 再次导出Snapshot并确认有限producer配置、Marker identity与显式None边界。
- [x] 23.10 重建Corin Projection及Float32/Fixed wrapper并核对同一source revision。
- [x] 23.11 保持Attack1至Attack5与Dodge显式None，除非另有真实共同姿态Marker业务，不按动作名称加入Locomotion.Gait。

## 24. 文档、编译与严格校验

- [x] 24.1 更新current `btsmtl-timeline-editor-preview` spec为最终Span、Point、Curve抽象和完整编辑合同。
- [x] 24.2 更新current `character-animation-presentation-authoring` spec为Point Marker与Animation Curve Channel边界。
- [x] 24.3 更新受影响的MotionCurve、MotionWarp与Camera current spec说明typed Curve Channel只改变Editor入口。
- [x] 24.4 更新current `agent-character-controller-synthesis`与MCP spec为v14唯一合同。
- [x] 24.5 更新current `character-state-timeline-authoring-loop` spec为Corin最终Finite/None配置真相。
- [x] 24.6 更新`openspec/project.md`与implementation inventory并删除四条Foot Placement曲线旧描述。
- [x] 24.7 更新implementation inventory记录完整Curve Catalog、UI完成度与无GenericCurveRuntime边界。
- [x] 24.8 使用规定参数编译`BTSMTL.Timeline.csproj`并立即shutdown build server。
- [x] 24.9 使用规定参数编译`BTSMTL.Timeline.Editor.csproj`并立即shutdown build server。
- [x] 24.10 使用规定参数编译`ThirdPersonSimulation.Core.csproj`与Float32/Fixed目标并立即shutdown build server。
- [x] 24.11 使用规定参数编译`ThirdPersonClient.Runtime.csproj`与`ThirdPersonClient.Editor.csproj`并立即shutdown build server。
- [x] 24.12 使用规定参数编译`Assembly-CSharp.csproj`与`Assembly-CSharp-Editor.csproj`并立即shutdown build server。
- [x] 24.13 运行`openspec validate add-timeline-animation-marker-sync --strict --no-interactive`。
- [x] 24.14 确认未运行Unity batchmode、未新增测试或人工验证task、未创建fallback或分裂作者路径。

## 25. Timeline 视口与 Marker 交互收口

- [x] 25.1 删除反向Flex与基于worldBound移动左侧工具栏的布局补偿。
- [x] 25.2 固定左侧工具栏和右侧时间标尺，并让左右Track区域共享唯一纵向scroll offset。
- [x] 25.3 将普通Wheel、Shift+Wheel、Ctrl+Wheel和Middle Drag分别收敛为纵向浏览、横移、横向缩放和横移。
- [x] 25.4 删除Curve Lane对Middle Drag与普通Wheel的抢占，仅保留Alt修饰的unbounded纵向视图操作。
- [x] 25.5 扩大Point Marker命中区域并改用父Track Timeline坐标计算拖动frame。
- [x] 25.6 让Marker子轨先选择AnimationTrack并拒绝None模式下必然失败的新增入口。
- [x] 25.7 使用规定参数重新编译`BTSMTL.Timeline.Editor.csproj`并立即shutdown build server。
- [x] 25.8 运行`openspec validate add-timeline-animation-marker-sync --strict --no-interactive`。
