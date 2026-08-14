## MODIFIED Requirements

### Requirement: Timeline Animation Analysis必须是按需领域工具

主Timeline Editor MAY在Sequence文档通过显式Character Editor provider提供Animation Analysis面板。面板 MUST默认关闭，并显式显示当前Sequence、AnimationClip、Analysis Source、artifact状态、Left/Right选择和单一metric。Action Timeline文档选择Sequence Segment时 MUST只提供Open Sequence导航，不得在Timeline owner分析或应用Marker。生成曲线 MUST只读且不得进入selection、Undo或Curve Channel Catalog。

#### Scenario: 查看Run脚分析

- **WHEN** 作者在Run Sequence文档打开Analysis
- **THEN** 面板 MUST按Run Sequence的精确Clip/Rig/Analysis identity检查artifact
- **AND** MUST不反向搜索Profile Binding、Blend Space sample或Action Timeline

#### Scenario: 没有精确Sequence上下文

- **WHEN** Action Timeline Segment无法解析唯一Sequence或Sequence缺少Analysis Source
- **THEN** Analysis工具 MUST显示Unavailable并禁用Apply
- **AND** MUST不按Profile、目录或当前selection猜测输入

#### Scenario: 查看多个分析metric

- **WHEN** artifact提供多个左右脚metric
- **THEN** 面板 MUST要求作者显式选择脚侧和单一metric
- **AND** MUST不把全部generated channel加入主Curve Catalog

### Requirement: Timeline Analysis必须显示并显式应用脚接触候选

Animation Analysis面板 MUST在Sequence artifact Ready时显示左右脚contact候选及目标Sequence frame。候选 MUST保持瞬时只读。作者确认Apply时，provider MUST重新校验Sequence、Clip、Analysis Source、Rig、Calibration、采样参数和candidate revision，并通过Sequence正式mutation生成已有Point Marker。Apply MUST只替换目标左右脚Marker集合、保留其它Marker并触发现有validator/Projection stale链。

#### Scenario: 应用Sequence候选

- **WHEN** 作者在Run Sequence确认未过期的Left/Right候选
- **THEN** provider MUST把候选写入Run Sequence并尽量保持匹配occurrence identity
- **AND** Profile、Blend Space和Action Timeline MUST不变脏

#### Scenario: 候选过期

- **WHEN** 候选显示后Sequence、Clip dependency或artifact identity变化
- **THEN** Apply MUST停止并显示Stale
- **AND** MUST不按缓存frame继续写入

#### Scenario: Sequence映射不唯一

- **WHEN** Sequence素材duration、Loop/Finite或Marker coverage无法唯一映射候选frame
- **THEN** 面板 MAY显示只读候选但 MUST禁用Apply并说明原因
- **AND** MUST不选择最近frame或按名称猜测Marker occurrence

### Requirement: Timeline Analysis工具不得伪造Foot Placement世界

Sequence Animation Analysis MUST只显示离线AnimationClip局部特征。它 MUST不执行PhysicsScene查询、Foot Lock、Ground Envelope、Pelvis、FullBodyIK或Camera，不得把离线Plant Confidence或Notify显示为Gameplay contact。

#### Scenario: 查看Sequence脚分析

- **WHEN** 作者查看左右脚Plant Confidence候选
- **THEN** UI MUST明确标记为离线素材分析
- **AND** MUST不显示世界地面命中、锁脚状态或最终IK结果

### Requirement: Timeline 编辑器预览使用管线预览会话

主Timeline Editor MUST为当前文档建立唯一window-local preview session adapter。Action Timeline文档 MUST使用正式Action Timeline Preview session；Sequence文档 MUST使用Sequence Preview session。切换、关闭或重绑文档时 MUST释放旧preview owner。窗口 MUST不直接控制TimelinePlayer、Animancer direct Play或独立PlayableGraph fallback。

#### Scenario: Sequence文档点击播放

- **WHEN** 作者在Sequence文档点击播放
- **THEN** window MUST通过Sequence Preview adapter推进表现采样与游标
- **AND** MUST不创建ActionInstance或运行Gameplay Timeline

#### Scenario: Action Timeline文档点击播放

- **WHEN** 作者在Action Timeline文档点击播放
- **THEN** window MUST继续通过正式Action Preview adapter执行Slot/Pose Plan
- **AND** MUST不把Sequence Preview状态写入Timeline资产

#### Scenario: inline Timeline窗口点击播放

- **WHEN** 用户从TimelineNode打开inline Action Timeline并点击播放
- **THEN** Action Preview adapter MUST只读使用该节点resolved TimelineData
- **AND** MUST不修改TimelineNode authoring data或调用旧TimelinePlayer

#### Scenario: shared Timeline root page点击播放

- **WHEN** 用户直接打开shared TimelineAsset并点击播放
- **THEN** 必须使用与inline Timeline相同的Action Preview实现
- **AND** shared资产 MUST不保存preview time或target

#### Scenario: TreeClip跨窗口下钻

- **WHEN** 用户从Action Timeline打开TreeClip Graph page或返回
- **THEN** 当前Action preview session MUST继续归属原Timeline文档
- **AND** Graph页面切换 MUST不接管或释放该session

### Requirement: Timeline 资产不保存编辑器播放状态

系统 MUST把播放目标、playhead、速度、session identity与播放状态保存在window-local document session中。Action Timeline、Animation Sequence、Track、Segment、Marker、Notify与Curve MUST只保存authoring数据；两个页面打开同一owner时 MUST拥有独立预览状态。

#### Scenario: 两个页面预览同一Sequence

- **WHEN** 两个主Timeline Editor页面预览同一Run Sequence
- **THEN** 两个页面 MUST拥有独立playhead、速度与target binding
- **AND** 任一页面seek或关闭 MUST不修改Sequence或另一个页面

#### Scenario: 两个页面预览同一shared Timeline

- **WHEN** 两个页面预览同一shared Action Timeline
- **THEN** 每个页面 MUST拥有独立time、playback generation和播放状态
- **AND** 任一页面操作 MUST不改写Timeline资产或另一页面

#### Scenario: 预览inline Timeline

- **WHEN** TimelineNode inline TimelineData被预览
- **THEN** Action Preview session MUST从authoring data创建独立工作状态
- **AND** runtime track state与当前time MUST不写回RootTree资产

### Requirement: Source Time Authoring模块必须跨正式owner复用

Timeline Editor MUST把time ruler、frame geometry、viewport、playhead、selection、Span/Point/Curve lane host、pointer draft、clipboard、rendering与Preview controls抽象为不依赖`TimelineData`、`Track`、`Clip`或`CharacterAnimationSequenceAsset`的`AnimationTimeCanvas`。Action Timeline与Animation Sequence MUST分别通过typed document adapter使用同一Canvas，并把Mutation提交给各自正式owner。系统 MUST不复制数据、不为Sequence创建TimelineData、不提供任意SerializedProperty入口或Inspector内嵌时间轴。

#### Scenario: Action Timeline与Sequence编辑Point元素

- **WHEN** 作者分别移动Action Timeline Section与Sequence Sync Marker
- **THEN** 两个文档 MUST共享pointer capture、frame snap、draft与单次Undo交互实现
- **AND** Mutation MUST分别写入Timeline Section与Sequence Marker owner

#### Scenario: 文档adapter缺少能力

- **WHEN** 当前Sequence文档不支持Action Window Span或当前Action Timeline不拥有Sequence Notify
- **THEN** Canvas MUST不显示对应创建命令
- **AND** MUST不按运行时类型、字段名或fallback descriptor注入lane

#### Scenario: Timeline与Sequence编辑同类曲线

- **WHEN** 作者分别编辑Action Segment Weight与Run Sequence Foot Placement Weight
- **THEN** 两个文档 MUST共享key/tangent/selection/Undo交互实现
- **AND** 数据 MUST分别写入Segment与Sequence正式owner

#### Scenario: 提取模块后编辑Timeline本地Point

- **WHEN** 作者在Action Timeline拖动Section或其它Timeline-local Point
- **THEN** Timeline element identity与Mutation语义 MUST保持不变
- **AND** Sequence MUST不获得副本

## ADDED Requirements

### Requirement: Timeline Editor必须提供Sequence与Action Timeline双文档模式

唯一主Timeline Editor窗口 MUST承载`Sequence`与`Action Timeline`两种typed文档。窗口 MUST共享文档tab、breadcrumb、播放控制、viewport、Details和Tools区域；每个文档 MUST拥有独立window-local selection、scroll、zoom、playhead和Preview session状态。Action Segment双击 MUST打开精确Sequence文档，返回时 MUST恢复原Action Timeline上下文。

#### Scenario: 从Attack进入攻击Sequence

- **WHEN** 作者在Attack Action Timeline双击一个Sequence Segment
- **THEN** 主窗口 MUST打开Segment明确引用的Sequence
- **AND** MUST不创建Pose Source Editor、普通Inspector时间轴或第二Timeline窗口

#### Scenario: domain reload恢复文档

- **WHEN** Editor domain reload后恢复Sequence与Action Timeline页签
- **THEN** 窗口 MUST按稳定owner identity重新绑定文档
- **AND** MUST不恢复旧对象实例或把view-state写入资产

### Requirement: Action Timeline必须只编辑动作编排内容

Action Timeline文档 MUST编辑Sequence Segment、Section、ActionWindow、Motion、MotionWarp、Decision、Cue、TreeClip和Timeline-local registered Curve。Sequence Segment MUST只保存Sequence引用、Start/End、ClipIn、Extrapolation、Weight与Ease；Timeline AnimationTrack、Segment与Inspector MUST不编辑或复制Sequence Marker、Time Mapping、Notify、Foot Placement Weight或Analysis配置。

#### Scenario: 编辑Attack片段

- **WHEN** 作者移动或裁剪Attack Sequence Segment
- **THEN** Timeline mutation MUST只改变Segment的动作编排范围或ClipIn
- **AND** 被引用Sequence的Marker、Curve与Notify MUST保持不变

#### Scenario: 查看素材Marker

- **WHEN** 作者在Action Timeline选择一个Sequence Segment
- **THEN** Details MAY只读显示Sequence同步摘要并提供Open Sequence导航
- **AND** Timeline文档 MUST不显示可写Sequence Marker lane或复制Marker到Track

## MODIFIED Requirements

### Requirement: Authoring Preview 必须复用正式 Marker Sync 表现链

Action Timeline Authoring Preview MUST通过Segment引用的Sequence plan解析Marker binding、Time Mapping、AnimationChannelId与Slot/Player，并复用session-local Action Playback、AnimationSlot、source backend与Pose Plan。Sequence Preview MUST从自身正式Sequence plan显示raw/effective time、当前Marker segment和mapping policy；没有source handoff时 MUST明确显示没有relation plan。两种Preview MUST不从旧Timeline Track、Profile Binding或Blend Space sample读取Marker副本。

#### Scenario: Action Segment预览Marker relation

- **WHEN** 作者预览引用MarkerGroup Sequence的Action Segment
- **THEN** Preview MUST显示Segment、Sequence、marker pair、mapping policy与effective time
- **AND** Timeline owner MUST不生成Marker副本

#### Scenario: 单Sequence预览Marker

- **WHEN** 作者拖动Run Sequence游标
- **THEN** Preview MUST显示该时间所在marker pair与leader fraction
- **AND** 没有handoff时effective time MUST等于raw time

#### Scenario: 比较Locomotion Walk与Run handoff

- **WHEN** 作者从Pose Graph Preview选择Walk与Run Sequence consumer
- **THEN** Pose Graph Workspace MUST通过正式Sequence plan与PoseState relation执行比较
- **AND** Action Timeline Preview MUST不生成BaseLocomotion Selection

#### Scenario: Preview包含TreeClip或MotionWarp

- **WHEN** 当前Action Timeline还包含TreeClip、MotionCurve或MotionWarp
- **THEN** Authoring Preview MUST只显示并编辑这些Track且不执行其Gameplay副作用
- **AND** MUST不创建Simulation Source、Pipeline或WorldSolver来运行它们

## ADDED Requirements

### Requirement: Action Timeline Section必须是稳定导航锚点

Action Timeline MAY拥有稳定identity、唯一业务名和整数frame的Section。Section MUST用于作者导航、选择和动作片段命名边界；本change中Section MUST不执行跳转、不替代Decision/TreeClip、不产生Gameplay事实，也不得作为Sequence Sync Marker或Notify消费。

#### Scenario: 创建Recovery Section

- **WHEN** 作者在攻击Timeline的恢复开始帧新增Recovery Section
- **THEN** Timeline MUST通过正式Section mutation保存identity、name与frame
- **AND** Gameplay Program、Sequence Marker与Action lifecycle MUST不因Section本身改变

## MODIFIED Requirements

### Requirement: Timeline Editor 必须按时间语义抽象作者内容

Timeline Editor MUST将作者投影分为占据起止区间的`Span`、位于单一整数帧的`Point`和连续求值的`Curve`。三类投影 MUST共享frame geometry、selection、帧吸附、pointer capture、本地草稿、单次Undo和刷新合同，但 MUST继续使用各自typed descriptor、正式owner、mutation与consumer。该抽象 MUST只属于Editor层，不得创建统一序列化DTO、Generic Runtime Track或第二份执行路径。

#### Scenario: Sequence展开全部素材内容

- **WHEN** 作者打开同时包含Marker、Notify和Foot Placement Weight的Sequence
- **THEN** Marker与Notify MUST显示为不同typed Point lane，Foot Placement Weight MUST显示为Curve lane
- **AND** 三者 MUST使用同一时间轴坐标但保持各自identity与consumer

#### Scenario: Action Timeline展开编排内容

- **WHEN** 作者打开包含Sequence Segment、Section与Action Window的Timeline
- **THEN** Segment与Window MUST显示为不同typed Span，Section MUST显示为Point
- **AND** Canvas MUST不把这些元素降低为同一种运行时对象

#### Scenario: 编辑器提交一次拖动

- **WHEN** 作者拖动任意Point或Curve key并释放
- **THEN** pointer capture期间 MUST只更新当前本地草稿
- **AND** Pointer Up/Capture Out MUST只产生一个正式Undo，Pointer Cancel MUST丢弃草稿

#### Scenario: Editor抽象进入Runtime

- **WHEN** Sequence或Action Timeline编译运行
- **THEN** Runtime MUST继续消费各自现有Sequence plan、Track、Projection与Program合同
- **AND** MUST不存在Span/Point/Curve统一运行时解释器

### Requirement: Timeline Editor 必须完整编辑显式注册的 Continuous Curve Channel

主Timeline Editor MUST通过显式typed Curve Channel Catalog显示和编辑当前文档正式拥有的Continuous Curve。Sequence Catalog MUST覆盖Foot Placement Weight及其它有正式Presentation consumer的素材channel；Action Timeline Segment Catalog MUST覆盖Weight、Ease In与Ease Out；MotionCurve、MotionWarp与Camera Timeline Clip继续覆盖各自现有registered channel。每个descriptor MUST声明稳定ChannelId、owner类型、显示名、颜色、time/value domain、单位、完整curve读取、正式owner mutation与领域validator。Editor MUST不通过反射、字段名、SerializedProperty path或任意字符串发现channel。

#### Scenario: 展开Sequence曲线

- **WHEN** 作者展开Run Sequence的CURVES分组
- **THEN** Editor MUST显示Sequence注册的Foot Placement Weight素材channel
- **AND** Action Timeline Segment与Profile Binding MUST不保存副本

#### Scenario: 展开Action Segment曲线

- **WHEN** 作者展开Attack Sequence Segment的CURVES分组
- **THEN** Editor MUST只显示segment-local Weight与Ease编排channel
- **AND** MUST不显示可写Sequence Foot Placement Weight

#### Scenario: 展开Motion与Camera曲线

- **WHEN** 作者展开MotionCurve、MotionWarp或Camera Timeline Clip
- **THEN** Editor MUST继续显示各领域已注册channel并调用其正式mutation
- **AND** Sequence runtime MUST不消费这些Timeline-local curve

#### Scenario: 绘制实际插值

- **WHEN** 任一registered channel包含非线性或weighted tangent key
- **THEN** Curve lane MUST按完整AnimationCurve插值绘制并显示原始key与tangent handle
- **AND** MUST不使用key间直线替代实际曲线

### Requirement: Timeline Field内部交互、几何与渲染必须分属明确模块

主Timeline Editor MUST保留现有TimelineEditorWindow、TimelineField、Details和Authoring Preview/Live Debug入口作为唯一窗口入口，但文档host、selection/drag/move/resize交互状态、time/frame geometry与hit-test、lane/playhead/overlay rendering、Preview binding和Details projection MUST由职责独立模块拥有。selection MUST只读暴露，外部 MUST通过selection命令修改；interaction MUST只依赖窄Canvas host port，不得持有完整TimelineField；rendering MUST显式消费frame range、viewport、playhead或overlay输入，不得反向读取完整TimelineField。Sequence与Action Timeline adapter MUST只通过各自正式Mutation与Undo入口修改owner；geometry/rendering MUST输入驱动且不得写asset；preview/live binding MUST继续由窗口本地session adapter拥有。拆分 MUST不改变Timeline/Track/Clip identity、Source Map、右侧Details selection或双窗口页签行为。拆分后 MUST删除`AnimationTimeFieldAuthoring`与全部Inspector内嵌时间轴，不保留两套交互实现。

#### Scenario: Resize一个Action Segment

- **WHEN** 作者拖动Sequence Segment边缘改变范围
- **THEN** interaction模块 MUST使用共享geometry结果并调用Action Timeline adapter的唯一mutation
- **AND** mutation MUST在一个Undo边界更新原Segment identity对应的数据
- **AND** Sequence adapter与Sequence owner MUST不参与该事务

#### Scenario: 拖动一个Sequence Marker

- **WHEN** 作者拖动Sequence Marker并释放
- **THEN** 同一interaction模块 MUST调用Sequence adapter的Marker mutation
- **AND** Action Timeline、Profile Binding与Blend Space sample MUST不获得副本

#### Scenario: 点击Details设置

- **WHEN** 作者选择Sequence Marker、Notify、Segment或Section后提交Details字段
- **THEN** selection owner MUST在提交期间保持同一stable identity
- **AND** Canvas重绘 MUST不清空或切换selection

#### Scenario: Authoring Preview切换Live Debug

- **WHEN** Action Timeline文档从Authoring Preview切换到Live Debug
- **THEN** window/session adapter MUST停止preview并建立窗口本地runtime binding
- **AND** interaction MUST只读，geometry/rendering MUST复用同一Timeline identity

#### Scenario: 多个playback overlay

- **WHEN** 同一Action Timeline source存在多个runtime playback
- **THEN** overlay模块 MUST呈现各playback identity并服从Follow/Pin选择
- **AND** rendering MUST不按列表顺序选赢家或调用preview evaluator

## REMOVED Requirements

### Requirement: Timeline Editor 必须编辑 AnimationTrack Marker Sync

**Reason**: Marker Sync从Action Timeline AnimationTrack与Profile Binding迁移到唯一Animation Sequence owner；Timeline只引用Sequence并编排Segment。

**Migration**: 通过完整内容签名把现有Track/Binding Marker迁入Sequence。无法把Track级Marker唯一映射到单一Sequence时迁移失败并报告精确coverage，不复制或猜测。
