# character-action-animation-authoring-workspace Specification

## Purpose
TBD - created by archiving change add-action-animation-authoring-workspace. Update Purpose after archive.
## Requirements
### Requirement: 有限Action动画必须提供统一作者工作面

Character Editor MUST提供`Action Animation Workspace`，从精确Character Definition、ActionProfile、Action Context call site、有限Action Timeline、Action Animation producer、直接AnimationClip引用、Presentation producer binding、AnimationSlot consumer与Runtime Debug binding建立typed session。Workspace MUST聚合关系并提供Open Timeline、Open Animation Clip和Open Slot导航，MUST不创建新的Montage、Action Sequence、Timeline、AnimationClip、Clip Curve、Slot配置或运行时播放器。

#### Scenario: 作者打开Attack动作动画

- **WHEN** 作者从Corin Attack ActionProfile打开Action Animation Workspace
- **THEN** Workspace MUST显示该Action的Gameplay、Timeline Segment、direct Clip、Slot、Blend、Preview和Live关系
- **AND** 每项关系 MUST解析到唯一正式owner

#### Scenario: 缺少唯一Timeline

- **WHEN** 当前Action没有有限Timeline或解析出多个候选Timeline
- **THEN** Workspace MUST显示typed authoring错误并定位Action call site
- **AND** MUST不按显示名、目录或generated Program猜测Timeline

### Requirement: Workspace必须保持跨owner唯一写入口

Action admission与退出语义 MUST继续由ActionProfile和Gameplay Graph拥有；Animation Segment的Clip引用、Start/End、ClipIn、Weight与Ease MUST由有限Action Timeline拥有；AnimationClip骨骼内容与注册Curve MUST由原生AnimationClip拥有并通过Unity Animation Window编辑；Window、Motion、Warp和Cue MUST继续由Timeline拥有；producer identity、Rig与Analysis装配 MUST继续由Animation Presentation Profile拥有；Slot topology与Blend Policy MUST继续由Pose Graph拥有。Workspace mutation MUST写入对应正式owner，不得保存镜像字段或第二Undo。

#### Scenario: 修改攻击动画引用

- **WHEN** 作者在Workspace替换Animation Segment引用的Clip
- **THEN** mutation MUST写入正式Timeline Segment
- **AND** Workspace、ActionProfile与Pose Graph MUST不保存Clip副本

#### Scenario: 修改Clip表现曲线

- **WHEN** 作者从Workspace打开Foot Placement Weight
- **THEN** Workspace MUST打开精确AnimationClip和Preview Target
- **AND** MUST不在Timeline Segment或Profile创建Curve副本

### Requirement: Workspace必须区分Action逻辑时间与表现采样时间

Workspace MUST分别显示Simulation Action Logic Time、committed raw visual sample与Projected Presentation Time。Action Logic Time和committed raw sample来自Simulation committed output；Projected Presentation Time由表现帧插值或受限外推。Action Workspace MUST不显示或计算Marker Effective Time、Locomotion Phase或同步relation，也 MUST不以单一可写`Montage Position`混合这些时间。

#### Scenario: 两个Fixed sample之间渲染

- **WHEN** Presentation Frame位于两个committed raw sample之间
- **THEN** Workspace MUST同时显示前后raw sample与当前projected time
- **AND** MUST不把projected time显示为新的Gameplay Timeline state

#### Scenario: Locomotion同步同时发生

- **WHEN** Action Slot外的Base Pose正在执行Locomotion Phase relation
- **THEN** Workspace MAY提供Open Pose Graph Live导航
- **AND** MUST不把Phase effective time归属到Action Timeline

### Requirement: Workspace Preview必须只运行表现链

Workspace Preview MUST复用正式Animation Preview Runtime、Action Playback fixture、Base Pose fixture、AnimationSlot、Transition Routing、Pose Plan与Rig，并按Presentation Delta生成Action Pose、Slot输出和Final Pose。Preview MUST不创建Simulation Session、不推进Gameplay Timeline、不提交Window、Motion、Warp、Cue或Action lifecycle事实。

#### Scenario: 预览Attack进入Run基础Pose

- **WHEN** 作者在Workspace选择合法Base Pose fixture和Attack playback fixture
- **THEN** Preview MUST通过正式AnimationSlot与Transition Routing生成最终Pose
- **AND** MUST不运行Corin Gameplay StateMachine

#### Scenario: Projection失效

- **WHEN** 当前Projection缺失、Invalid或revision不匹配
- **THEN** Preview MUST停止并报告正式失败原因
- **AND** MUST不临时编译Plan或创建fallback播放器

### Requirement: Workspace Live Debug必须只读取正式Trace

Workspace Live Debug MUST从匹配revision的RuntimeDebugSession显示ActionInstance、Action lifecycle、committed Timeline sample、projected presentation sample、Playback lifecycle、AnimationSlot route、Blend/Stored/Inertialization状态与Final Pose贡献。Live Debug MUST只读，不得重新执行Gameplay Graph、Timeline或Pose Graph，也不得显示Action Phase relation。

#### Scenario: Action被Hit打断

- **WHEN** Runtime发生Attack到Hit的Action replacement
- **THEN** Live Debug MUST显示旧Action terminal、替换command、Slot route、混合策略和最终Pose贡献
- **AND** 所有数据 MUST来自同一正式Trace

#### Scenario: Trace过期

- **WHEN** Trace revision与当前Definition或Projection不匹配
- **THEN** Workspace MUST显示stale并停止关联
- **AND** MUST不自动Build或按显示名重建关系

### Requirement: Workspace必须保持Numeric Target与显式Build边界

Workspace authoring与Preview MUST不保存NumericProfile或Float32/Fixed runtime state。Live Debug MAY只读显示当前Session Numeric Target。相同Presentation Contract的Float32与Fixed Session MUST映射到同一producer、AnimationSlot和Pose Plan。Workspace打开、selection、mutation、Preview、Live Debug与asset import MUST不自动Build、重分析或自动选中生成资产。

#### Scenario: 查看Fixed Session动作

- **WHEN** Workspace连接匹配合同的Fixed Session
- **THEN** Live Debug MUST显示Fixed Target identity与committed raw sample
- **AND** Preview与Presentation配置 MUST继续复用target-neutral Projection

#### Scenario: 修改Timeline Clip

- **WHEN** 作者完成Timeline mutation
- **THEN** owner MUST标记dirty并进入原有Undo
- **AND** 系统 MUST等待作者明确执行已有Dry Run或Build命令
