## ADDED Requirements

### Requirement: Animation Sequence必须是原始动画素材的唯一作者owner

系统 MUST使用正式`CharacterAnimationSequenceAsset`表达一段原始动画素材。Sequence MUST拥有稳定identity、精确AnimationClip引用、Rig、Loop/Finite语义、默认播放倍率、Marker Sync、registered typed素材Curve、typed Notify和Foot Analysis Source引用。Profile Binding、Blend Space sample、Action Timeline Segment、Pose Graph、StateMachine、ActionProfile与generated Projection MUST不保存这些素材字段的可写副本。

#### Scenario: 作者配置Run素材

- **WHEN** 作者打开Run Sequence并修改左右脚Marker与Foot Placement Weight
- **THEN** mutation MUST只写入Run Sequence正式owner
- **AND** 所有引用Run的Pose Source、Blend Space与Action Segment MUST通过同一Sequence identity读取结果

#### Scenario: 相同AnimationClip需要不同标记

- **WHEN** 两个业务用途需要同一导入AnimationClip但具有不同Marker或素材Curve
- **THEN** 作者 MUST创建两个明确Sequence资产
- **AND** 系统 MUST不在引用点增加override或按AnimationClip合并两份作者语义

### Requirement: Sequence Marker、Curve与Notify必须保持不同业务语义

Sequence Marker MUST表达有向同步区间与Time Mapping；registered Sequence Curve MUST表达随素材时间连续求值的表现参数；Sequence Notify MUST表达typed presentation-only瞬时事件。三者 MAY共享time ruler、frame geometry、selection与Undo交互，但 MUST拥有各自stable identity、typed mutation、validator和consumer。Notify MUST不产生Gameplay Fact、Window、Cue、Motion、Warp、Action lifecycle或State transition。

#### Scenario: 在落脚帧同时存在Marker与Notify

- **WHEN** 作者在同一整数帧放置RightFootContact Marker与FootstepAudio Notify
- **THEN** Marker MUST只参与同步计划，Notify MUST只进入注册的表现consumer
- **AND** 两者 MUST不共享identity或互相转换

#### Scenario: 未注册Notify kind

- **WHEN** 作者尝试创建没有typed payload和正式presentation consumer的Notify kind
- **THEN** Sequence authoring MUST拒绝创建
- **AND** MUST不回退Unity AnimationEvent、反射方法名或字符串广播

### Requirement: Sequence必须通过主时间编辑器完成时间作者操作

主Timeline Editor MUST提供`Sequence`文档模式，使用共享UI Toolkit time ruler、Marker lane、Notify lane、typed Curve lane、Analysis overlay、Preview控制、selection、zoom/pan、pointer draft、clipboard和单次Undo手势。Sequence模式 MUST通过typed Sequence document adapter修改正式Sequence owner，不得构造临时`TimelineData`、内嵌Inspector时间轴或第二IMGUI时间编辑器。

#### Scenario: 精确编辑Run曲线

- **WHEN** 作者在Sequence文档框选多个Foot Placement Weight key并编辑weighted tangent
- **THEN** 主时间编辑器 MUST通过Sequence typed curve mutation原子提交完整curve
- **AND** Profile Inspector与Blend Space Details MUST不显示第二个Curve编辑器

#### Scenario: 从Action Segment打开Sequence

- **WHEN** 作者双击Action Timeline中的RunEnd Sequence Segment
- **THEN** 同一主Timeline Editor MUST打开被引用Sequence文档
- **AND** 返回Action Timeline后 MUST恢复window-local文档选择和viewport而不修改任一资产

### Requirement: Sequence引用必须使用精确对象identity

Profile Sequence Binding、Blend Space sample与Action Timeline Sequence Segment MUST通过强类型对象引用和稳定Sequence identity定位唯一Sequence。Compiler、Editor、Agent Document与迁移器 MUST不按AnimationClip、资源名、目录、列表index或当前selection猜测Sequence。

#### Scenario: 两个Sequence引用同一AnimationClip

- **WHEN** Project中存在两个引用同一AnimationClip的Sequence
- **THEN** 每个Binding、sample与Segment MUST解析其明确引用的Sequence
- **AND** 系统 MUST不选择第一个、名称相似或最近打开的Sequence

### Requirement: Sequence Preview必须只执行表现采样

Sequence Preview MUST通过typed preview adapter、正式Sequence plan、Rig与Pose Preview链执行表现采样，并显示Marker、Notify、typed Curve和只读Analysis overlay。Preview MUST不创建Gameplay Simulation Session、ActionInstance、Timeline operation或运行时fallback播放器；缺少匹配Rig、Projection或artifact时 MUST显示typed Unavailable。

#### Scenario: 预览TurnBack Sequence

- **WHEN** 作者播放TurnBack Sequence且存在合法Preview target
- **THEN** Preview MUST按Sequence source time采样Pose并移动同一主时间轴游标
- **AND** MUST不触发TurnBack Gameplay状态、Motion、Cue或Action Window

### Requirement: Foot Analysis必须从Sequence唯一解析素材输入

Sequence MUST显式引用Foot Analysis Source；Artifact identity MUST覆盖Sequence identity及其精确AnimationClip、Rig、Analysis Source和算法依赖。候选、连续feature和generated warp仍 MUST保持只读；作者显式Apply时只能把接触候选写为Sequence Marker，不能修改Profile、Blend Space、Action Timeline或generated Projection。

#### Scenario: 应用Run脚接触候选

- **WHEN** Run Sequence的精确artifact为Ready且作者确认Apply
- **THEN** 系统 MUST通过Sequence正式Marker mutation更新Left/Right contact occurrence
- **AND** 所有引用Run的消费者 MUST不保存候选或Marker副本
