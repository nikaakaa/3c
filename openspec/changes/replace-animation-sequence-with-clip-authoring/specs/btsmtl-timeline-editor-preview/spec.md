## ADDED Requirements

### Requirement: Timeline Editor必须只编辑Action Timeline并导航直接AnimationClip

Timeline Editor MUST只拥有有限Action的Track、Segment、Window、Motion、MotionWarp、Decision、Cue与Timeline-local Curve。Animation Segment MUST直接引用精确AnimationClip并保存Start/End、ClipIn、Extrapolation、Weight与Ease。作者双击Segment或执行Open Clip时，Editor MUST通过精确Character Definition、Profile、Clip与Preview Target打开Unity Animation Window。Timeline Editor MUST不提供Sequence模式、素材Marker lane、素材Curve lane、Foot Analysis面板或Sequence Preview。

#### Scenario: 双击Attack Segment

- **WHEN** 作者双击Attack Timeline中的Animation Segment
- **THEN** 系统 MUST打开精确AnimationClip和正式Preview Target
- **AND** Timeline selection与AnimationClip Curve MUST继续由各自owner保存

#### Scenario: Timeline没有Character上下文

- **WHEN** 作者独立打开shared Timeline
- **THEN** Timeline本地Action编排 MUST保持可编辑
- **AND** Open Clip需要的Preview Target不可解析时 MUST显示typed Unavailable而不搜索任意Definition

### Requirement: Timeline Editor 必须按时间语义抽象作者内容

Timeline Editor MUST将本地作者内容明确分为占据起止区间的`Span Clip`和沿时间连续求值的`Continuous Curve`。两类内容 MUST共享Timeline frame geometry、选择、帧吸附、pointer capture、本地草稿、单次Undo提交和提交后刷新合同，但 MUST继续使用各自正式数据所有权与authoring API。该抽象 MUST只属于Editor交互层，不得创建统一宽序列化DTO、新Runtime Track、第二份`TimelineData`或运行时按类型反射分派。AnimationClip注册Curve与Locomotion Phase MUST不进入该抽象。

#### Scenario: 同一AnimationTrack展开作者内容

- **WHEN** 作者展开包含Animation Segment、Weight和Ease的AnimationTrack
- **THEN** Segment MUST显示在Span Clip行，Weight与Ease MUST显示在Continuous Curve行
- **AND** Foot Placement Weight与Locomotion Phase MUST不显示为Timeline lane

#### Scenario: 编辑器提交一次拖动

- **WHEN** 作者拖动Continuous Curve key并释放指针
- **THEN** pointer capture期间 MUST只更新当前元素的本地草稿
- **AND** Pointer Up或意外Capture Out MUST只产生一个正式Undo事务
- **AND** Pointer Cancel MUST丢弃草稿且不得修改资产

### Requirement: Timeline Editor 必须完整编辑显式注册的 Continuous Curve Channel

Timeline Editor MUST通过显式typed Curve Channel Catalog显示和编辑Timeline owner已经正式拥有的Continuous Curve。每个descriptor MUST声明稳定ChannelId、owner类型、显示名、颜色、time domain、value domain、单位、完整curve读取、正式owner mutation和领域validator。Catalog MUST覆盖Animation Segment的Weight、Ease In和Ease Out，MotionCurve Clip的Weight、Position X/Y/Z、Yaw和Ease In/Out，MotionWarp Clip的Position Progress与Yaw Progress，以及CameraStateClip与CameraResponseClip的Weight和Ease In/Out。`presentation.locomotion-phase`、`presentation.foot-placement-weight`、AnimationClip骨骼曲线和其它Clip内注册Curve MUST只由Unity Animation Window编辑，不得进入Timeline Catalog。

每个具有registered Timeline channel的Track MUST显示可折叠`CURVES`分组，展开后每个ChannelId拥有独立lane。每个Clip或Segment MUST只在自己的StartFrame..EndFrame范围显示自己的Timeline-local curve、key与边界；重叠内容 MUST不在作者层合并curve。Curve Lane MUST按完整`AnimationCurve.Evaluate`结果绘制插值，显示原始key、tangent handle、当前游标time/value、value reference与单位。

#### Scenario: 展开Animation Track曲线

- **WHEN** 作者展开包含Animation Segment的CURVES分组
- **THEN** Editor MUST只显示Weight、Ease In与Ease Out三个Timeline-local typed channel
- **AND** Foot Placement Weight与Locomotion Phase MUST不出现在该分组

#### Scenario: 展开MotionCurve Track曲线

- **WHEN** 作者展开MotionCurve Clip的CURVES分组
- **THEN** Editor MUST显示Weight、Position X/Y/Z、Yaw与Ease In/Out
- **AND** Position与Yaw MUST使用unbounded value view及明确单位

### Requirement: Curve Editor必须保持领域运行链唯一

Timeline Curve Editor MUST只提供Timeline-local作者投影与正式mutation，不得创建`GenericTimelineCurveRuntime`。Animation Segment Weight/Ease MUST继续进入Action Presentation计划；MotionCurve和MotionWarp曲线 MUST继续进入Semantic IR、Numeric Program与各自evaluator/modifier；Camera曲线 MUST继续进入既有Camera compile/presentation链。AnimationClip注册表现Curve MUST由Clip Curve catalog、Animation Window入口与Character Presentation Projection链拥有，MUST不经过Timeline Curve MutationAdapter。RootMotionCurveAsset、导入AnimationClip骨骼/BlendShape/属性曲线和没有正式consumer的任意Float Curve MUST不进入Timeline Curve Channel Catalog。

#### Scenario: 编辑MotionWarp progress

- **WHEN** 作者修改MotionWarp Position Progress channel
- **THEN** Timeline只通过MotionWarpClip正式mutation保存curve
- **AND** 后续Compiler MUST沿既有MotionWarp semantic operation编译

#### Scenario: 请求Clip注册Curve

- **WHEN** Timeline Curve Editor收到`presentation.locomotion-phase`或`presentation.foot-placement-weight`
- **THEN** Catalog MUST拒绝该channel并提供Open Animation Clip导航
- **AND** MUST不在Segment或Timeline创建Curve副本

## REMOVED Requirements

### Requirement: Source Time Authoring模块必须跨正式owner复用

该Requirement被删除。素材时间编辑直接使用Unity Animation Window，Timeline Field只服务Timeline-local内容。

#### Scenario: 请求跨owner素材时间模块

- **WHEN** Profile或Blend Space打开AnimationClip
- **THEN** 系统 MUST导航Unity Animation Window
- **AND** MUST不创建Timeline typed owner adapter

### Requirement: Timeline Animation Analysis必须是按需领域工具

该Requirement被删除。Foot Analysis候选属于AnimationClip作者入口，不再安装到Timeline窗口。

#### Scenario: Timeline请求Foot Analysis

- **WHEN** 作者从Action Segment请求Foot Analysis
- **THEN** Workspace MUST先导航到精确AnimationClip作者入口
- **AND** Timeline主窗口 MUST不托管Analysis工具

### Requirement: Timeline Analysis必须显示并显式应用脚接触候选

该Requirement被删除。Analysis只能显式应用完整Locomotion Phase Curve到原生AnimationClip，不能生成Timeline Marker。

#### Scenario: 应用旧接触候选

- **WHEN** Timeline尝试把候选写为AnimationSyncMarker
- **THEN** capability校验 MUST失败
- **AND** Timeline资产 MUST保持不变

### Requirement: Timeline Analysis工具不得伪造Foot Placement世界

该Requirement随Timeline Analysis工具删除；Foot Analysis的局部数据边界由Foot Analysis能力规范继续约束。

#### Scenario: Timeline打开旧Analysis面板

- **WHEN** 旧session请求Timeline Analysis provider
- **THEN** tool catalog MUST报告能力不存在
- **AND** MUST不创建兼容面板

### Requirement: Timeline Editor 必须编辑 AnimationTrack Marker Sync

该Requirement被删除。AnimationTrack不再保存SyncMode、Group、Topology、Role或Marker。

#### Scenario: 旧Marker字段进入Timeline

- **WHEN** Timeline JSON或Unity authoring仍包含AnimationSyncMarker
- **THEN** strict parser或Validator MUST拒绝该旧数据
- **AND** MUST不隐藏或迁移为Curve key

### Requirement: Authoring Preview 必须复用正式 Marker Sync 表现链

该Requirement被删除。Action Authoring Preview只按raw/projected Action sample执行正式AnimationSlot与Pose Plan。

#### Scenario: 预览旧Marker producer

- **WHEN** Action Preview加载包含Marker relation的旧Projection
- **THEN** Projection revision校验 MUST失败
- **AND** Preview MUST不继续播放旧relation

### Requirement: Timeline Live Debug 必须显示正式 Sync Relation

该Requirement被删除。Locomotion Phase relation只在Pose Graph Live Debug显示，Timeline Live Debug不再拥有Action Sync relation。

#### Scenario: 观察Walk到Run

- **WHEN** Runtime发生PoseState Phase relation
- **THEN** Timeline MAY提供Open Pose Graph Live导航
- **AND** MUST不显示虚假Timeline producer或AnimationPlaybackId
