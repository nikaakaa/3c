## ADDED Requirements

### Requirement: Timeline Editor 必须按时间语义抽象作者内容

Timeline Editor MUST将作者内容明确分为占据起止区间的`Span Clip`、位于单一整数帧的`Point Marker`和沿时间连续求值的`Continuous Curve`。三类内容 MUST共享Timeline frame geometry、选择、帧吸附、pointer capture、本地草稿、单次Undo提交和提交后刷新合同，但 MUST继续使用各自正式数据所有权与authoring API。该抽象 MUST只属于Editor交互层，不得创建统一宽序列化DTO、新Runtime Track、第二份`TimelineData`或运行时按类型反射分派。

#### Scenario: 同一AnimationTrack展开全部作者内容

- **WHEN** 作者展开同时包含Animation Clip、Marker Sync和Foot Placement Weight的AnimationTrack
- **THEN** Clip MUST显示在Span Clip行，Sync Marker MUST显示为Point Marker，Foot Placement Weight MUST显示在Continuous Curve行
- **AND** 三类内容 MUST使用同一时间轴缩放与滚动坐标
- **AND** Marker MUST不以曲线key或连续phase值显示

#### Scenario: 编辑器提交一次拖动

- **WHEN** 作者拖动Point Marker或Continuous Curve key并释放指针
- **THEN** pointer capture期间 MUST只更新当前元素的本地草稿
- **AND** Pointer Up或意外Capture Out MUST只产生一个正式Undo事务
- **AND** Pointer Cancel MUST丢弃草稿且不得修改资产

#### Scenario: Editor抽象进入Runtime

- **WHEN** Timeline编译或运行
- **THEN** Runtime MUST继续消费现有Track、Clip、Projection和Program合同
- **AND** MUST不存在Span、Point、Curve统一运行时解释器或第二条Tick路径

### Requirement: Timeline Editor 必须完整编辑显式注册的 Continuous Curve Channel

Timeline Editor MUST通过显式typed Curve Channel Catalog显示和编辑Timeline Clip已经正式拥有的Continuous Curve。每个descriptor MUST声明稳定ChannelId、owner类型、显示名、颜色、time domain、value domain、单位、完整curve读取、正式owner mutation和领域validator。Catalog MUST首批覆盖Animation Clip的Weight、Ease In、Ease Out和Foot Placement Weight，MotionCurve Clip的Weight、Position X/Y/Z、Yaw和Ease In/Out，MotionWarp Clip的Position Progress与Yaw Progress，以及CameraStateClip与CameraResponseClip的Weight和Ease In/Out。Editor MUST不通过反射、字段名、SerializedProperty path或任意字符串发现和修改curve。

每个具有registered channel的Track MUST显示可折叠`CURVES`分组，展开后每个ChannelId拥有独立lane。每个Clip MUST只在自己的StartFrame..EndFrame范围显示自己的curve、key与边界；重叠Clip MUST不在作者层合并curve。Curve Lane MUST按完整`AnimationCurve.Evaluate`结果绘制插值，显示原始key、tangent handle、当前游标time/value、value reference与单位。Bounded channel MUST使用声明范围；unbounded channel MUST提供独立vertical fit、scale和zero line，不得Clamp到`[0,1]`。

#### Scenario: 展开Animation Track曲线

- **WHEN** 作者展开包含Animation Clip的CURVES分组
- **THEN** Editor MUST显示Weight、Ease In、Ease Out与Foot Placement Weight四个typed channel
- **AND** Foot Placement Weight MUST只是其中一个channel，不得拥有专用Curve View实现
- **AND** Sync Marker MUST继续只显示在独立SYNC MARKERS子轨

#### Scenario: 展开MotionCurve Track曲线

- **WHEN** 作者展开MotionCurve Clip的CURVES分组
- **THEN** Editor MUST显示Weight、Position X/Y/Z、Yaw与Ease In/Out
- **AND** Position与Yaw MUST使用unbounded value view及明确单位
- **AND** Editor MUST不把它们Clamp到权重范围

#### Scenario: 展开MotionWarp与Camera曲线

- **WHEN** 作者展开MotionWarp或Camera Clip的CURVES分组
- **THEN** Editor MUST只显示Catalog为该owner登记的channel
- **AND** MotionWarp progress MUST继续接受其单调与端点领域校验
- **AND** Camera曲线 MUST继续由Camera领域消费者拥有运行语义

#### Scenario: 绘制实际插值

- **WHEN** channel包含非线性或weighted tangent key
- **THEN** Curve Lane MUST按完整AnimationCurve插值绘制
- **AND** MUST显示原始key与tangent handle
- **AND** MUST不使用key之间的直线替代实际曲线

### Requirement: Curve Key编辑必须无损且原子

Curve Lane MUST支持单选、Shift追加、框选、双击或右键新增、一个或多个key拖动、Delete或右键删除、复制粘贴、数值Inspector以及Auto、Clamped Auto、Linear、Constant、Free和Weighted tangent编辑。横轴 MUST通过descriptor在Timeline frame与curve local time之间映射，并按整数Timeline frame吸附；纵轴 MUST按typed value domain处理。一次手势或Inspector提交 MUST只修改本地完整curve草稿并通过descriptor MutationAdapter生成一个Undo事务。Pointer Cancel MUST丢弃草稿；Pointer Up或意外Capture Out MUST提交最后草稿。提交后 MUST重新读取owner并刷新Timeline、Inspector、领域validation、Projection stale状态和可用Authoring Preview。

Curve mutation MUST原子保存pre/post wrap mode及每个key的time、value、in/out tangent、in/out weight和WeightedMode。Curve key不获得持久AuthoringId；Editor MAY在当前owner revision内使用临时key index选择，Agent与持久Patch MUST以`OwnerAuthoringId + ChannelId + Full Curve`替换完整channel，不得按key index跨revision修改。

#### Scenario: 拖动多个curve key

- **WHEN** 作者框选多个key并拖动
- **THEN** pointer capture期间 MUST只更新本地curve草稿
- **AND** 所有key MUST按相同Timeline frame delta和值delta移动并保持合法顺序
- **AND** 释放时 MUST只产生一个Undo事务

#### Scenario: 精确编辑weighted tangent

- **WHEN** 作者在Inspector修改一个key的in/out tangent、weight与WeightedMode
- **THEN** MutationAdapter MUST原子保存完整Keyframe字段
- **AND** 未修改的key与wrap mode MUST无损保留

#### Scenario: 复制到不兼容channel

- **WHEN** 作者把unbounded Position key粘贴到bounded Weight channel
- **THEN** Editor MUST拒绝该操作并说明time/value domain不兼容
- **AND** MUST不Clamp、不换算单位也不部分写入

#### Scenario: 外部修改使key选择过期

- **WHEN** owner curve revision在编辑手势外被Agent或其它正式入口替换
- **THEN** Editor MUST使临时key选择失效并重新读取完整curve
- **AND** MUST不按旧key index写入新revision

### Requirement: Curve Editor必须保持领域运行链唯一

通用Curve Editor MUST只提供作者投影与正式mutation，不得创建`GenericTimelineCurveRuntime`。Animation控制曲线 MUST继续进入CharacterPresentationProjection与现有Presentation consumer；MotionCurve和MotionWarp曲线 MUST继续进入Semantic IR、Numeric Program与各自evaluator/modifier；Camera曲线 MUST继续进入既有Camera compile/presentation链。RootMotionCurveAsset、导入AnimationClip骨骼/BlendShape/属性曲线和没有正式consumer的任意Float Curve MUST不进入Timeline Curve Channel Catalog。

#### Scenario: 编辑MotionWarp progress

- **WHEN** 作者修改MotionWarp Position Progress channel
- **THEN** Timeline只通过MotionWarpClip正式mutation保存curve
- **AND** 后续Compiler MUST沿既有MotionWarp semantic operation编译
- **AND** Editor MUST不直接执行MotionWarp或创建第二runtime sampler

#### Scenario: 请求任意自定义curve

- **WHEN** 某功能只提供显示名但没有registered ChannelId、owner mutation、validator或runtime consumer
- **THEN** Curve Editor MUST拒绝创建该channel
- **AND** MUST不保存未知AnimationCurve字段或fallback数据

### Requirement: Timeline Editor 必须编辑 AnimationTrack Marker Sync

Timeline Editor MUST在AnimationTrack Inspector与同一track时间轴中编辑SyncMode、SyncGroupId、Finite/Cyclic topology、SyncRole和marker。每个AnimationTrack MUST拥有一个固定存在、可折叠的`SYNC MARKERS`子轨；该子轨 MUST只是父Track作者数据的编辑投影，不得加入`TimelineData.Tracks`、获得独立AuthoringId、接受Clip或执行Tick。折叠状态 MUST只改变显示高度并保留group、topology、role和marker数量摘要。`None`子轨 MUST显示禁用摘要，`MarkerGroup`子轨 MUST按稳定MarkerAuthoringId显示、选择和拖动Point Marker，并按整数Timeline frame吸附。

作者 MUST能在子轨空白帧通过右键菜单新增Marker。菜单 MUST从当前正式Definition authoring context内同Layer、同canonical SyncGroup的AnimationTrack动态投影已使用MarkerId候选，并 MUST允许显式输入新的合法MarkerId。候选索引 MUST只读且不得序列化为全局catalog、Profile或Track副本。Marker右键菜单 MUST提供选择、定位、重命名与删除；Inspector MUST继续提供精确MarkerId和frame输入。新增、重命名、移动、删除与模式切换 MUST通过Timeline正式authoring API进入Undo、dirty、identity、唯一校验、RebindTimeline和Authoring Preview刷新链，不得使用YAML、SerializedProperty任意写入或独立FootPhase资产。

#### Scenario: 拖动一个marker

- **WHEN** 作者在AnimationTrack marker lane拖动RightPlant
- **THEN** 编辑器 MUST保持该marker的AuthoringId
- **AND** pointer capture期间 MUST只更新本地整数frame预览
- **AND** 释放或意外失去capture时 MUST以一个Undo事务提交最后frame并触发正式validation、Projection stale状态与Preview刷新
- **AND** Pointer Cancel MUST恢复原frame且不得写入资产

#### Scenario: 在空白帧新增同组marker

- **WHEN** 作者在RunLoop的SYNC MARKERS子轨空白帧打开右键菜单
- **THEN** 菜单 MUST显示`Locomotion.Gait`同Layer其它正式Track已经使用的MarkerId候选
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
- **AND** Preview游标 MUST突出当前有向Marker Pair与fraction
- **AND** Finite子轨 MUST不显示回绕

#### Scenario: 切换为None

- **WHEN** 作者把MarkerGroup track切换为None
- **THEN** authoring API MUST原子清空group、topology、SyncRole和markers
- **AND** Undo MUST能恢复完整旧配置

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
- **THEN** Track Handle MUST保留SyncMode、Group、Topology、Role和Marker数量摘要
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

纯表现Authoring Preview MUST通过CharacterPresentationProjection解析producer marker binding，并复用CharacterAnimationPlaybackRuntime、AnimationMarkerSyncRuntime、AnimationPlaybackLifecycle与AnimancerPlaybackAdapter。单producer预览 MUST显示raw/effective time和当前marker segment。作者 MAY显式选择同一Projection、同Layer、同Group的source producer进行handoff比较；该比较 MUST只生成现有preview selection/sample命令，不得创建Preview Simulation Session、执行Gameplay operation或直接设置Animancer normalized time。

#### Scenario: 单producer预览marker

- **WHEN** 作者拖动一个MarkerGroup AnimationTrack的Timeline游标
- **THEN** Preview MUST显示该时间所在的marker pair与segment fraction
- **AND** 在没有source handoff时effective time MUST等于raw time

#### Scenario: 比较Walk与Run handoff

- **WHEN** 作者显式选择WalkLoop作为source并预览RunLoop target
- **THEN** Preview MUST通过正式MarkerSyncRuntime持续映射target effective time
- **AND** Animancer MUST通过正式TransitionLibrary执行fade

#### Scenario: Preview包含TreeClip或MotionWarp

- **WHEN** 当前Timeline还包含TreeClip、MotionCurve或MotionWarp
- **THEN** Authoring Preview MUST只显示并编辑这些track
- **AND** MUST不创建Simulation Source、Pipeline、WorldSolver或Action target输入来执行它们

### Requirement: Timeline Live Debug 必须显示正式 Sync Relation

Timeline Live Debug MUST从共享RuntimeDebugSession的正式Animation trace显示source/target playback、LayerId、canonical SyncGroupId、有向marker pair、source fraction、target occurrence、raw/effective time、effective cycle、relation depth、lifecycle phase与detach/failure reason。Live Debug MUST不按authoring游标重新采样、不读取脚骨Transform、不推导StateMachine transition，也不得维护第二份relation状态。

#### Scenario: 观察连续切换

- **WHEN** runtime发生`Walk -> Run -> Turn`且存在relation chain
- **THEN** Live Debug MUST按playback generation显示每条source-target relation与depth
- **AND** 显示值 MUST来自当帧正式runtime snapshot

#### Scenario: source退休

- **WHEN** source fade完成并触发target continuation rebase
- **THEN** Live Debug MUST显示`SourceRetiredRebased`及最后raw/effective anchor
- **AND** MUST不把该事件显示为Gameplay State transition

#### Scenario: target显式None

- **WHEN** incoming target未参与Marker Sync
- **THEN** Live Debug MUST显示`TargetExplicitNone`
- **AND** MUST继续显示target原始Timeline采样与普通Animancer lifecycle
