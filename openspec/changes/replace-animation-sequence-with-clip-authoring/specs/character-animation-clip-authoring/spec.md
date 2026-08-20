## ADDED Requirements

### Requirement: 原生AnimationClip必须是素材时间数据的唯一owner

系统 MUST让可写原生`.anim`唯一保存骨骼动画、Root曲线和项目注册表现Curve。Pose Source Binding、Blend Space Sample、Action Timeline Segment、Profile与Editor session MUST不复制这些Curve，也 MUST不创建Animation Sequence包装资产。AnimationClip身份 MUST使用结构化Unity对象引用、dependency hash与注册Curve canonical hash；MUST不增加第二AuthoringId或ContentRevision。

#### Scenario: 同一Clip被Pose与Action引用

- **WHEN** 一个原生AnimationClip同时被Pose Source Binding与Action Timeline Segment引用
- **THEN** 两个使用点 MUST读取同一Clip注册Curve
- **AND** MUST不存在Sequence、Binding Curve或Segment Curve副本

### Requirement: Unity Animation Window必须是素材时间曲线的唯一人工编辑表面

项目 MUST通过精确Character Definition、Profile、AnimationClip与Preview Target打开Unity Animation Window。项目 MAY提供Clip选择、注册Curve创建、Analysis候选显式应用和诊断摘要，但 MUST不注入Unity内部窗口、不使用反射，也 MUST不维护Sequence Editor、素材Marker lane、素材Curve lane或第二播放游标。

#### Scenario: 作者打开RunLoop动画

- **WHEN** 作者从Profile或Pose Graph导航RunLoop AnimationClip
- **THEN** 系统 MUST选择正式Preview Target并打开Unity Animation Window中的精确Clip
- **AND** MUST不打开Sequence文档或项目第二素材时间轴

### Requirement: 项目注册表现Curve必须使用唯一channel catalog

唯一channel catalog MUST登记`presentation.locomotion-phase`与`presentation.foot-placement-weight`的Unity Curve Binding、值域、切线约束、必填条件和Projection降低方式。Foot Placement Weight MUST位于`[0,1]`。Locomotion Phase MUST在实际使用区间严格单调递增，并以整数表示右脚接触、整数加0.5表示左脚接触。缺失或非法Curve MUST阻止Projection发布，MUST不生成默认Curve。

#### Scenario: Locomotion Clip缺少Phase Curve

- **WHEN** Profile Locomotion Sync Group成员没有`presentation.locomotion-phase`
- **THEN** Character Build MUST报告精确Clip与缺失channel
- **AND** MUST不回退normalized time、Marker或旧Projection

### Requirement: 导入子Clip必须先显式归一化为原生AnimationClip

注册Curve作者链 MUST只接受可写原生`.anim`。ModelImporter子Clip、只读Clip或不稳定外部对象 MUST在authoring与Build时明确失败。系统 MUST不自动复制、解包或生成隐藏Clip；正式归一化必须是作者显式执行的独立内容操作。

#### Scenario: Profile引用FBX子Clip

- **WHEN** 作者把ModelImporter子Clip配置为需要注册Curve的Locomotion source
- **THEN** Validator MUST要求先产出正式原生`.anim`
- **AND** MUST不在Library、Temp或Assets中自动创建替代Clip
