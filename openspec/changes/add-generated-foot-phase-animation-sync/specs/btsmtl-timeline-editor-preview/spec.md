## MODIFIED Requirements

### Requirement: Timeline Editor 必须编辑 AnimationTrack Marker Sync

Timeline Editor MUST在AnimationTrack Inspector与同一track时间轴中编辑SyncMode、SyncGroupId、Finite/Cyclic topology、SyncRole、Time Mapping和marker。每个AnimationTrack MUST拥有一个固定存在、可折叠的`SYNC MARKERS`子轨；该子轨 MUST只是父Track作者数据的编辑投影，不得加入`TimelineData.Tracks`、获得独立AuthoringId、接受Clip或执行Tick。折叠状态 MUST只改变显示高度并保留group、topology、role、Time Mapping和marker数量摘要。`None`子轨 MUST显示禁用摘要，`MarkerGroup`子轨 MUST要求作者明确选择`MarkerSegmentFraction`或`GeneratedFootPhase`，并按稳定MarkerAuthoringId显示、选择和拖动Point Marker，按整数Timeline frame吸附。

作者 MUST能在子轨空白帧通过右键菜单新增Marker。菜单 MUST从当前正式Definition authoring context内同AnimationChannelId、同显式MarkerSync可达集合、同canonical SyncGroup的AnimationTrack动态投影已使用MarkerId候选，并 MUST允许显式输入新的合法MarkerId。候选索引 MUST只读且不得序列化为全局catalog、Profile或Track副本。Marker右键菜单 MUST提供选择、定位、重命名与删除；Inspector MUST继续提供精确MarkerId和frame输入。新增、重命名、移动、删除、模式切换与Time Mapping修改 MUST通过Timeline正式authoring API进入Undo、dirty、identity、唯一校验、RebindTimeline和Authoring Preview刷新链，不得使用YAML、SerializedProperty任意写入、可编辑warp knot或独立FootPhase资产。

#### Scenario: 拖动一个marker

- **WHEN** 作者在AnimationTrack marker lane拖动RightPlant
- **THEN** 编辑器 MUST保持该marker的AuthoringId
- **AND** pointer capture期间 MUST只更新本地整数frame预览
- **AND** 释放或意外失去capture时 MUST以一个Undo事务提交最后frame并触发正式validation、Projection stale状态与Preview刷新
- **AND** Pointer Cancel MUST恢复原frame且不得写入资产

#### Scenario: 在空白帧新增同组marker

- **WHEN** 作者在Attack AnimationTrack的SYNC MARKERS子轨空白帧打开右键菜单
- **THEN** 菜单 MUST显示同Action AnimationChannelId、同Slot可达集合其它有限Action Track已经使用的MarkerId候选
- **AND** 选择候选 MUST通过正式authoring API在当前frame创建具有新AuthoringId的Marker

#### Scenario: 为组创建首个marker名称

- **WHEN** 当前Sync Group尚无MarkerId候选
- **THEN** 作者 MUST能显式输入新的合法MarkerId
- **AND** Editor MUST不创建独立Marker catalog作为先决条件

#### Scenario: 删除marker点

- **WHEN** 作者右键一个Marker并选择删除
- **THEN** Editor MUST按MarkerAuthoringId调用正式删除API
- **AND** Timeline、Inspector、pair coverage与Preview MUST在同一提交后刷新

#### Scenario: 查看循环闭合

- **WHEN** 作者展开Cyclic MarkerGroup子轨
- **THEN** 子轨 MUST明确显示末Marker到下一周期首Marker的有向闭合关系
- **AND** Preview游标 MUST突出当前有向Marker Pair、Time Mapping与leader fraction
- **AND** Finite子轨 MUST不显示回绕

#### Scenario: 切换为None

- **WHEN** 作者把MarkerGroup track切换为None
- **THEN** authoring API MUST原子清空Time Mapping、group、topology、SyncRole和markers
- **AND** Undo MUST能恢复完整旧配置

#### Scenario: 选择生成式脚相位

- **WHEN** 作者把MarkerGroup track的Time Mapping改为GeneratedFootPhase
- **THEN** Timeline Editor MUST显示精确Foot Analysis artifact readiness和Build期relation coverage诊断
- **AND** MUST不生成、编辑或序列化pair table与warp knot

#### Scenario: shared producer调用拓扑冲突

- **WHEN** 当前shared AnimationTrack同时被Once与Loop节点调用
- **THEN** Timeline Editor MUST显示全部冲突call site的stable identity与来源定位
- **AND** MUST不提供call site override作为修复

#### Scenario: None动画轨仍显示固定子轨

- **WHEN** 作者打开SyncMode为None的AnimationTrack
- **THEN** clip row下方 MUST显示固定Marker Sync子轨和None摘要
- **AND** 子轨 MUST不创建Timeline运行时Track

#### Scenario: 折叠Marker子轨

- **WHEN** 作者折叠SYNC MARKERS子轨
- **THEN** Track Handle MUST保留SyncMode、Group、Topology、Role、Time Mapping和Marker数量摘要
- **AND** 折叠 MUST不修改Marker数据、Track组合顺序或运行时Projection

#### Scenario: Track重排保持组合行

- **WHEN** 作者拖动重排带Marker Sync子轨的AnimationTrack
- **THEN** clip row、Marker子轨和左侧Track Handle MUST作为一个组合行移动
- **AND** 不得与相邻Track重叠

#### Scenario: Marker与Foot Placement曲线同时存在

- **WHEN** AnimationTrack同时启用MarkerGroup且Clip拥有Foot Placement Weight曲线
- **THEN** Marker MUST只显示在SYNC MARKERS子轨
- **AND** Foot Placement Weight MUST只显示在独立CURVES子轨
- **AND** 两者 MUST不共享key、contact、phase或运行时状态

### Requirement: Authoring Preview 必须复用正式 Marker Sync 表现链

有限Action Timeline Authoring Preview MUST通过CharacterPresentationProjection解析producer marker binding、Time Mapping、Action AnimationChannelId与Slot/Player PoseNodeId，并复用session-local `CharacterActionPlaybackRuntime`、AnimationSlot、source backend与Pose Plan。单producer预览 MUST从正式relation snapshot显示raw/effective time、当前marker segment、mapping policy、leader fraction、warped follower fraction和可选warp plan identity；没有source handoff时 MUST明确显示没有relation plan，不得现场编译warp。持续Locomotion Sequence/BlendSpace source marker预览 MUST由Pose Graph Workspace从Profile binding和正式Projection执行，Timeline Editor不得复制或编辑这些marker。

#### Scenario: 单producer预览marker

- **WHEN** 作者拖动一个MarkerGroup AnimationTrack的Timeline游标
- **THEN** Preview MUST显示该时间所在的marker pair、Time Mapping与leader segment fraction
- **AND** 在没有source handoff时effective time MUST等于raw time且warped follower fraction MUST显示为不适用

#### Scenario: 比较Locomotion Walk与Run handoff

- **WHEN** 作者选择Walk与Run Presentation Pose source
- **THEN** Pose Graph Workspace MUST通过正式source-local mapping policy、Projection warp plan与PoseState transition执行比较
- **AND** Preview MUST分别显示leader fraction、warped follower fraction和target effective time
- **AND** Timeline Preview MUST不生成BaseLocomotion Selection

#### Scenario: Preview包含TreeClip或MotionWarp

- **WHEN** 当前Timeline还包含TreeClip、MotionCurve或MotionWarp
- **THEN** Authoring Preview MUST只显示并编辑这些track
- **AND** MUST不创建Simulation Source、Pipeline、WorldSolver或Action target输入来执行它们

### Requirement: Timeline Live Debug 必须显示正式 Sync Relation

Timeline Live Debug MUST从共享RuntimeDebugSession的正式Animation trace显示有限Action source/target PlaybackId、AnimationChannelId、Slot/Player PoseNodeId、canonical SyncGroupId、Time Mapping、有向marker pair、leader fraction、warped follower fraction、leader/follower occurrence、warp plan identity、raw/effective time、effective cycle、relation depth、lifecycle phase与detach/failure reason。PoseState Source Sync relation MUST在Pose Graph Live Debug显示PoseState、transition generation、Presentation source usage与同一mapping结果；Timeline Live Debug MAY提供只读跨工作区导航，但 MUST不把Pose relation伪装为Timeline playback。两者 MUST不按authoring游标重新采样、不推导State transition、不求值Pose Graph、不从Animancer weight重建贡献、不现场编译warp或维护第二份relation状态。

#### Scenario: 观察连续切换

- **WHEN** runtime发生`Attack1 -> Attack2 -> Dodge`且存在Action relation chain
- **THEN** Live Debug MUST按playback generation显示每条source-target relation、mapping policy、plan identity与depth
- **AND** 显示值 MUST来自当帧正式runtime snapshot

#### Scenario: source退休

- **WHEN** source fade完成并触发target continuation rebase
- **THEN** Live Debug MUST显示`SourceRetiredRebased`及最后raw/effective anchor
- **AND** MUST不把该事件显示为Gameplay State transition

#### Scenario: 观察Walk到Run

- **WHEN** runtime发生GeneratedFootPhase PoseState Source Sync
- **THEN** Pose Graph Live Debug MUST显示Walk leader fraction、Run warped follower fraction、plan identity与target effective time
- **AND** Timeline Live Debug MUST提供Open Pose Graph Live导航
- **AND** MUST不生成虚假AnimationPlaybackId

#### Scenario: target显式None

- **WHEN** incoming target未参与Marker Sync
- **THEN** Live Debug MUST显示`TargetExplicitNone`
- **AND** MUST继续显示target原始Timeline采样与普通Animancer lifecycle

#### Scenario: 生成式计划失败

- **WHEN** Runtime拒绝missing、identity mismatch或invalid knot的GeneratedFootPhase plan
- **THEN** Live Debug MUST显示稳定typed failure和精确relation identity
- **AND** MUST不显示已切换为MarkerSegmentFraction、normalized time或上一帧effective time
