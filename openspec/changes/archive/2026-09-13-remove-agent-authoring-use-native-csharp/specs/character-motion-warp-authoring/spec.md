## MODIFIED Requirements

### Requirement: MotionWarpClip 必须显式引用唯一源 MotionCurveClip

`MotionWarpClip` MUST通过稳定authoring identity显式引用同一Timeline owner内的一个`MotionCurveClip`。源Clip MUST使用`Action` channel、`Override` blend mode、`ActorLocal` space、无Ease且全程为1的Gameplay WeightCurve；Warp窗口 MUST完整位于源Clip的`StartFrame..CurveEndFrame`内，同一源Clip上的Warp窗口 MUST不重叠。动画CrossFade MUST继续由Presentation独立表达，Gameplay source权重 MUST不改变Warp目标。系统 MUST不通过时间重叠、Track名称、Clip列表索引、CurveId或运行时扫描猜测source。ScaleToTarget的源窗口终点平面向量与ScaleSourceYaw的源窗口总yaw MUST在authoring/Semantic发布前满足对应非零前置条件，Runtime仍 MUST保留同一invariant检查。

#### Scenario: 重排 Timeline Track

- **WHEN** 作者重排MotionCurveTrack与MotionWarpTrack但不删除Clip
- **THEN** MotionWarpClip MUST继续引用同一个源MotionCurveClip
- **AND** 编译结果 MUST不因列表顺序变化而改绑source

#### Scenario: 删除被引用的 MotionCurve

- **WHEN** 作者删除MotionWarpClip引用的MotionCurveClip
- **THEN** Inspector与Compiler MUST报告悬空source identity
- **AND** 系统 MUST不自动选择同区间的其它MotionCurveClip

#### Scenario: World-space source尝试使用MotionWarp

- **WHEN** 作者把World-space MotionCurve绑定为MotionWarp source
- **THEN** Inspector、正式Timeline配置入口与Compiler MUST拒绝发布
- **AND** Runtime MUST不猜测如何把world轨迹转换成warp-start局部轨迹

#### Scenario: Scale source在窗口内没有位移

- **WHEN** 作者选择ScaleToTarget
- **AND** source在Warp窗口StartFrame到EndFrame的平面累计终点为零
- **THEN** 发布 MUST失败并定位Warp、source与窗口
- **AND** MUST不等到运行时切换成LinearToTarget

#### Scenario: Warp source配置Gameplay淡入淡出

- **WHEN** 作者给Warp引用的MotionCurve配置非零Ease或非单位WeightCurve
- **THEN** Inspector、正式Timeline配置入口与Semantic发布 MUST拒绝该source
- **AND** MUST不按权重缩放Warp终点或在Runtime忽略该配置
- **AND** 动画表现淡入淡出 MAY继续由AnimationTrack与Presentation配置

### Requirement: MotionWarp authoring 必须在发布前拒绝不完整配置

Timeline Inspector、Semantic Compiler与正式Timeline配置入口 MUST复用同一套MotionWarp校验。source、owner、window、Translation Mode、Offset Space、Rotation Mode、Rotation Method、offset、limit、所需curve、ConstantRate、Action Context与Action target requirement任一无效时，artifact发布 MUST失败。系统 MUST不猜目标空间、不替换solver、不自动生成curve或建立fallback配置。

MotionWarp所属动作 MAY声明`OptionalSnapshot`或`SnapshotRequired`。`None`与MotionWarp组合 MUST在发布前拒绝。`OptionalSnapshot`动作无目标时 MUST保留resolved source并产生typed无目标结果；合法Limit Policy导致的`AppliedClamped`或`PreservedByLimitPolicy` MUST与目标缺失、配置错误明确区分。

#### Scenario: Warp 所属动作未声明需要目标

- **WHEN** MotionWarp所在Timeline由`ActionTargetRequirement.None`的Action启动
- **THEN** 编译 MUST失败并定位ActionProfile、Timeline与MotionWarpClip
- **AND** 系统 MUST不在运行时把缺失目标解释为不Warp

#### Scenario: 可选目标动作当前没有目标

- **WHEN** MotionWarp所在Timeline由`OptionalSnapshot` Action启动
- **AND** 对应ActionInstance没有captured target snapshot
- **THEN** runtime MUST原样保留已仲裁的source MotionCurve contribution
- **AND** MUST不建立Warp跨Tick状态或产生position/yaw correction

#### Scenario: 必需目标动作缺少目标

- **WHEN** MotionWarp所在Timeline由`SnapshotRequired` Action启动
- **AND** call site没有有效目标declaration或候选值
- **THEN** authoring、admission或artifact validation MUST在Warp执行前失败
- **AND** runtime MUST不通过Optional语义继续动作

#### Scenario: ScaleToTarget缺少可缩放源距离

- **WHEN** 编译配置选择ScaleToTarget
- **AND** source窗口终点平面长度为零
- **THEN** Authoring与Semantic发布 MUST拒绝该配置
- **AND** Runtime若收到违反该合同的Program MUST产生稳定invariant错误并定位Warp与source
- **AND** MUST不切换成LinearToTarget

#### Scenario: PreserveSource处理超限目标

- **WHEN** 目标存在但需要修正超过限制
- **AND** Clip显式选择PreserveSource
- **THEN** Runtime MUST原样保留resolved source并报告`PreservedByLimitPolicy`
- **AND** MUST不初始化Warp跨Tickstate

## REMOVED Requirements

### Requirement: MotionWarp 修正必须使用 canonical 累计进度曲线

**Reason**: 旧要求把Agent/Document作为正式作者或校验参与方；删除该入口及相应协议场景。

**Migration**: 使用本规范新增的“MotionWarp累计进度曲线必须由正式作者入口校验”。原有领域业务规则和非协议场景随新要求保留；人工和C#直接调用正式业务API，不保留Agent流程或旧场景别名。

## ADDED Requirements

### Requirement: MotionWarp累计进度曲线必须由正式作者入口校验

MotionWarpClip MUST只为当前solver实际消费的进度保存canonical normalized cumulative progress curve。`SkewToTarget`与`LinearToTarget` MUST使用Position Progress；`ProgressCurve` rotation method MUST使用Yaw Progress。曲线 MUST只包含有限值，时间域 MUST为`[0,1]`，首值 MUST为0，末值 MUST为1并单调不下降。Timeline Curve Catalog与C#作者API MUST复用唯一MotionWarp校验，不得静默Clamp、补端点、重排非法key或为不消费curve的mode生成默认数据。`ScaleToTarget`、`ConstantRate`与`ScaleSourceYaw` MUST不把未消费curve写入SemanticHash或Program。

#### Scenario: 旋转早于位置完成

- **WHEN** yaw progress curve前半段增长更快而position progress curve后半段增长更快
- **THEN** 角色 MUST先完成更多yaw修正再完成更多position修正
- **AND** 窗口结束时两者 MUST达到各自有效目标

#### Scenario: ConstantRate不消费Yaw Progress

- **WHEN** 作者选择ConstantRate rotation method
- **THEN** Inspector与Compiler MUST只消费最大yaw速率和窗口时间
- **AND** 旧Yaw Progress数据 MUST不参与artifact identity或Runtime结果

#### Scenario: 在Timeline编辑进度曲线

- **WHEN** 作者在CURVES分组选择Yaw Progress并修改weighted tangent
- **THEN** Editor MUST保存完整Keyframe与wrap mode
- **AND** MotionWarp validator MUST重新校验端点、范围和单调性
- **AND** 非法结果 MUST拒绝整个mutation而不是自动修复

#### Scenario: C#作者API修改MotionWarp进度

- **WHEN** C#作者API通过registered ChannelId提交Yaw Progress完整curve
- **THEN** handler MUST调用同一MotionWarp mutation与validator
- **AND** MUST不使用MotionWarp专用第二curve patch入口
