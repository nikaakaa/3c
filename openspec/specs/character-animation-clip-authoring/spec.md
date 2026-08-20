# character-animation-clip-authoring Specification

## Purpose

定义原生AnimationClip作为动画素材、注册表现曲线、Unity Animation Window导航、Preview Target与Character Presentation装配之间的唯一作者合同，并固定其构建与运行时边界。

## Requirements

### Requirement: 原生AnimationClip必须是素材时间数据的唯一owner

系统 MUST让可写原生`.anim`唯一保存骨骼动画、Root曲线和项目注册表现Curve。Pose Source Binding、Blend Space Sample、Action Timeline Segment、Profile与Editor session MUST不复制这些Curve，也 MUST不创建Animation Sequence包装资产。AnimationClip持久身份 MUST使用结构化Unity对象引用；Projection依赖 MUST区分完整Unity dependency、`AnimationClipAnalysisInputHash`与注册Curve canonical hash，MUST不增加第二AuthoringId或ContentRevision。

#### Scenario: 同一Clip被Pose与Action引用

- **WHEN** 一个原生AnimationClip同时被Pose Source Binding与Action Timeline Segment引用
- **THEN** 两个使用点 MUST读取同一Clip注册Curve
- **AND** MUST不存在Sequence、Binding Curve或Segment Curve副本

### Requirement: Unity Animation Window必须是素材时间曲线的唯一人工编辑表面

项目 MUST通过精确Character Definition、Profile、AnimationClip与Preview Target打开Unity Animation Window。项目 MAY提供Clip选择、注册Curve创建、Analysis候选显式应用和诊断摘要，但 MUST不注入Unity内部窗口、不使用反射，也 MUST不维护Sequence Editor、素材Marker lane、素材Curve lane或第二播放游标。注册Curve MUST使用catalog规定的完整`EditorCurveBinding(path + type + property)`；作者接收器 MUST只安装在Preview Target，Production Prefab MUST不安装或读取该接收器，Runtime MUST不把接收器字段作为第二输入。

#### Scenario: 作者打开RunLoop动画

- **WHEN** 作者从Profile或Pose Graph导航RunLoop AnimationClip
- **THEN** 系统 MUST选择正式Preview Target并打开Unity Animation Window中的精确Clip
- **AND** MUST不打开Sequence文档或项目第二素材时间轴

### Requirement: 项目注册表现Curve必须使用唯一channel catalog

唯一channel catalog MUST登记`presentation.locomotion-phase`与`presentation.foot-placement-weight`的完整Unity Curve Binding、Clip秒域、值域、切线约束、必填条件和Projection降低方式。全部注册Curve key time MUST使用秒；`presentation.foot-placement-weight` MUST位于`[0,1]`并完整覆盖`[0, SourceDurationSeconds]`，且 MUST唯一降低为Runtime `animation.foot-placement-weight`参数。Direct Clip、Action、Blend Space与Motion Matching MUST消费同一catalog，MUST不按Runtime参数名或仅按`propertyName`查找第二条Clip Curve。Locomotion Phase MUST在声明coverage内检查Hermite段导数并连续严格单调递增，以整数表示右脚Landing/Plant onset、整数加0.5表示左脚Landing/Plant onset。缺失或非法Curve MUST阻止Projection发布，MUST不生成默认Curve。

#### Scenario: Locomotion Clip缺少Phase Curve

- **WHEN** Profile Locomotion Sync Group成员没有`presentation.locomotion-phase`
- **THEN** Character Build MUST报告精确Clip与缺失channel
- **AND** MUST不回退normalized time、Marker或旧Projection

#### Scenario: Motion Matching读取Foot Weight

- **WHEN** Motion Matching Projection编译一个包含Foot Placement Weight的现有原生Clip
- **THEN** resolver MUST通过catalog读取`presentation.foot-placement-weight`完整binding并降低为`animation.foot-placement-weight`
- **AND** MUST不要求Clip另存`animation.foot-placement-weight`作者Curve

### Requirement: 注册表现Curve不得定义Foot Analysis输入身份或基础素材时长

系统 MUST从排除项目注册表现Curve的骨骼/Root曲线、正式Loop设置与基础素材时长生成`AnimationClipAnalysisInputHash`。`SourceDurationSeconds` MUST由同一非注册素材输入计算；注册Curve不得延长或重新定义该时长。Foot Analysis Artifact identity MUST使用Analysis Input Hash而非完整Clip dependency。修改注册Curve MUST只改变Registered Curve Hash并使相关Projection stale；只有骨骼、Root、Loop或Source Duration变化才使Foot Analysis Artifact stale。

#### Scenario: 作者应用Phase候选

- **WHEN** 作者把新的Locomotion Phase Curve写入骨骼与Root内容未变的AnimationClip
- **THEN** Clip Registered Curve Hash与Projection stale状态 MUST变化
- **AND** 同一Analysis Input Hash对应的Foot Analysis Artifact MUST继续Ready

#### Scenario: 作者修改Root曲线

- **WHEN** 原生AnimationClip的Root曲线或正式Loop设置变化
- **THEN** AnimationClipAnalysisInputHash MUST变化并使Foot Analysis Artifact stale
- **AND** 系统 MUST不把旧Artifact用于Phase候选或Projection Build

### Requirement: 导入子Clip必须先显式归一化为原生AnimationClip

注册Curve作者链 MUST只接受可写原生`.anim`。ModelImporter子Clip、只读Clip或不稳定外部对象 MUST在authoring与Build时明确失败。系统 MUST不自动复制、解包或生成隐藏Clip；正式归一化必须是作者显式执行的独立内容操作。

#### Scenario: Profile引用FBX子Clip

- **WHEN** 作者把ModelImporter子Clip配置为需要注册Curve的Locomotion source
- **THEN** Validator MUST要求先产出正式原生`.anim`
- **AND** MUST不在Library、Temp或Assets中自动创建替代Clip
