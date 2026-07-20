## ADDED Requirements

### Requirement: MotionWarpClip 必须显式引用唯一源 MotionCurveClip

`MotionWarpClip` MUST通过稳定authoring identity显式引用同一Timeline owner内的一个`MotionCurveClip`。系统 MUST不通过时间重叠、Track名称、Clip列表索引、CurveId或运行时扫描猜测源。源Clip MUST使用`Action` channel与`Override` blend mode；Warp窗口 MUST完整位于源Clip的`StartFrame..CurveEndFrame`内。同一源Clip上的Warp窗口 MUST不重叠。

#### Scenario: 重排 Timeline Track

- **WHEN** 作者重排MotionCurveTrack与MotionWarpTrack但不删除Clip
- **THEN** MotionWarpClip MUST继续引用同一个源MotionCurveClip
- **AND** 编译结果 MUST不因列表顺序变化而改绑source

#### Scenario: 删除被引用的 MotionCurve

- **WHEN** 作者删除MotionWarpClip引用的MotionCurveClip
- **THEN** Inspector与Compiler MUST报告悬空source identity
- **AND** 系统 MUST不自动选择同区间的其它MotionCurveClip

### Requirement: MotionWarpClip 必须以类型化字段表达目标姿态

MotionWarpClip MUST分别表达位置模式与旋转模式。位置模式 MUST至少包含`Disabled`与`MatchTargetPlanarPosition`；旋转模式 MUST至少包含`Disabled`、`FaceTarget`与`MatchTargetYaw`。Clip MUST保存target-local平面offset、target yaw offset、position/yaw weight及最大总修正。第一版MUST只修正XZ平面与yaw，MUST不修改源MotionCurve的Y位移。

#### Scenario: 对齐目标前方的攻击接触点

- **WHEN** 作者选择平面位置匹配并配置target-local offset
- **THEN** desired position MUST由目标快照position、yaw与该offset唯一计算
- **AND** position correction MUST受weight与最大总距离限制

#### Scenario: 让角色朝向目标

- **WHEN** 作者选择FaceTarget
- **THEN** desired yaw MUST由desired actor position指向目标快照position的平面方向计算
- **AND** 零长度方向 MUST作为明确错误而不是沿用当前yaw

### Requirement: MotionWarp 修正必须使用 canonical 累计进度曲线

MotionWarpClip MUST分别保存position与yaw的normalized cumulative progress curve。每条curve MUST只包含有限值，时间域 MUST为`[0,1]`，首值 MUST为0，末值 MUST为1，并且 MUST单调不下降。Runtime MUST使用相邻Tick累计采样值之差计算本Tick修正，不得把EaseIn、EaseOut或AnimationTrack weight作为第二套Gameplay修正权重。

#### Scenario: 旋转早于位置完成

- **WHEN** yaw progress curve前半段增长更快而position progress curve后半段增长更快
- **THEN** 角色 MUST先完成更多yaw修正再完成更多position修正
- **AND** 两者最终累计修正 MUST分别到达已计算总量

### Requirement: MotionWarp authoring 必须在发布前拒绝不完整配置

Timeline Inspector、Semantic Compiler与Agent Validator MUST复用同一套MotionWarp校验。source、owner、window、mode、offset、weight、clamp、progress curve、Action Context与Action target requirement任一无效时，artifact发布 MUST失败。系统 MUST不静默禁用Warp、不写默认目标、不缩短窗口，也 MUST不创建fallback配置。

#### Scenario: Warp 所属动作未声明需要目标

- **WHEN** MotionWarp所在Timeline由`ActionTargetRequirement.None`的Action启动
- **THEN** 编译 MUST失败并定位ActionProfile、Timeline与MotionWarpClip
- **AND** 系统 MUST不在运行时把缺失目标解释为不Warp

### Requirement: 旧 MotionWarp 采样路径必须删除

正式代码 MUST删除`TimelineMotionWarpWindow`、`MotionWarpTrack.Sample()`、独立`TrySampleClip()`、MotionWarp `TargetKey`字符串和旧Weight/Ease采样。MotionWarpTrack与MotionWarpClip MUST只作为正式Compiler消费的authoring类型存在。

#### Scenario: 搜索 MotionWarp runtime 入口

- **WHEN** 迁移完成后检查正式代码
- **THEN** MUST只有Semantic IR到Target Program的MotionWarp执行链
- **AND** MUST不存在Timeline直接采样window或写Body/Transform的第二路径
