## MODIFIED Requirements

### Requirement: MotionWarpClip 必须显式引用唯一源 MotionCurveClip

`MotionWarpClip` MUST通过稳定authoring identity显式引用同一Timeline owner内的一个`MotionCurveClip`。源Clip MUST使用`Action` channel、`Override` blend mode、`ActorLocal` space、无Ease且全程为1的Gameplay WeightCurve；Warp窗口 MUST完整位于源Clip的`StartFrame..CurveEndFrame`内，同一源Clip上的Warp窗口 MUST不重叠。动画CrossFade MUST继续由Presentation独立表达，Gameplay source权重 MUST不改变Warp目标。系统 MUST不通过时间重叠、Track名称、Clip列表索引、CurveId或运行时扫描猜测source。ScaleToTarget的源窗口终点平面向量与ScaleSourceYaw的源窗口总yaw MUST在authoring/Semantic发布前满足对应非零前置条件，Runtime仍 MUST保留同一invariant检查。

#### Scenario: World-space source尝试使用MotionWarp

- **WHEN** 作者把World-space MotionCurve绑定为MotionWarp source
- **THEN** Inspector、Agent Validator与Compiler MUST拒绝发布
- **AND** Runtime MUST不猜测如何把world轨迹转换成warp-start局部轨迹

#### Scenario: Scale source在窗口内没有位移

- **WHEN** 作者选择ScaleToTarget
- **AND** source在Warp窗口StartFrame到EndFrame的平面累计终点为零
- **THEN** 发布 MUST失败并定位Warp、source与窗口
- **AND** MUST不等到运行时切换成LinearToTarget

#### Scenario: Warp source配置Gameplay淡入淡出

- **WHEN** 作者给Warp引用的MotionCurve配置非零Ease或非单位WeightCurve
- **THEN** Inspector、Agent Validator与Semantic发布 MUST拒绝该source
- **AND** MUST不按权重缩放Warp终点或在Runtime忽略该配置
- **AND** 动画表现淡入淡出 MAY继续由AnimationTrack与Presentation配置

### Requirement: MotionWarpClip 必须以类型化字段表达目标姿态

`MotionWarpClip` MUST把目标姿态与轨迹解算分开表达。Clip MUST保存`TranslationMode`、`TargetOffsetSpace`、`TargetPlanarOffset`、`RotationMode`、`RotationMethod`、`TargetYawOffsetDegrees`、最大平面修正、最大yaw修正、ConstantRate所需最大yaw速率及显式Limit Policy。`TargetPlanarOffset` MUST按所选空间生成窗口结束目标位置；yaw offset MUST只修改目标yaw。系统 MUST删除PositionWeight与YawWeight，不得用权重含糊表达部分目标对齐。第一阶段仍 MUST只修改XZ平面与yaw，MUST不修改源MotionCurve的Y位移。

#### Scenario: 普通攻击按接近方向保持站距

- **WHEN** 作者选择`ApproachDirection`并配置平面offset
- **THEN** 系统 MUST以target snapshot位置指向窗口开始Body位置的方向建立稳定平面基
- **AND** 目标位置 MUST由该基、offset和target snapshot唯一计算
- **AND** 目标与Body平面位置重合时 MUST报告无效基而不是借用target yaw

#### Scenario: 配对动作按目标朝向设置站位

- **WHEN** 作者选择`TargetLocal`
- **THEN** offset MUST按target snapshot yaw旋转
- **AND** MUST不读取目标Presentation Transform或当前Animator姿态

#### Scenario: 配置部分对齐限制

- **WHEN** 作者选择`ApplyClamped`且目标需要量超过最大修正
- **THEN** Runtime MUST使用受限有效目标并报告`AppliedClamped`
- **AND** MUST不报告已经达到原始目标姿态

### Requirement: MotionWarp 修正必须使用 canonical 累计进度曲线

MotionWarpClip MUST只为当前solver实际消费的进度保存canonical normalized cumulative progress curve。`SkewToTarget`与`LinearToTarget` MUST使用Position Progress；`ProgressCurve` rotation method MUST使用Yaw Progress。曲线 MUST只包含有限值，时间域 MUST为`[0,1]`，首值 MUST为0，末值 MUST为1并单调不下降。Timeline Curve Catalog与Agent MUST复用唯一MotionWarp校验，不得静默Clamp、补端点、重排非法key或为不消费curve的mode生成默认数据。`ScaleToTarget`、`ConstantRate`与`ScaleSourceYaw` MUST不把未消费curve写入SemanticHash或Program。

#### Scenario: yaw早于位置完成

- **WHEN** SkewToTarget使用后段position progress且ProgressCurve rotation使用前段yaw progress
- **THEN** 同一累计Warp pose MUST先完成更多yaw再完成更多目标位置修正
- **AND** 窗口结束时两者 MUST达到各自有效目标

#### Scenario: ConstantRate不消费Yaw Progress

- **WHEN** 作者选择ConstantRate rotation method
- **THEN** Inspector与Compiler MUST只消费最大yaw速率和窗口时间
- **AND** 旧Yaw Progress数据 MUST不参与artifact identity或Runtime结果

### Requirement: MotionWarp authoring 必须在发布前拒绝不完整配置

Timeline Inspector、Semantic Compiler与Agent Validator MUST复用同一套MotionWarp校验。source、owner、window、Translation Mode、Offset Space、Rotation Mode、Rotation Method、offset、limit、所需curve、ConstantRate、Action Context与Action target requirement任一无效时，artifact发布 MUST失败。系统 MUST不猜目标空间、不替换solver、不自动生成curve或建立fallback配置。

MotionWarp所属动作 MAY声明`OptionalSnapshot`或`SnapshotRequired`。`None`与MotionWarp组合 MUST在发布前拒绝。`OptionalSnapshot`动作无目标时 MUST保留resolved source并产生typed无目标结果；合法Limit Policy导致的`AppliedClamped`或`PreservedByLimitPolicy` MUST与目标缺失、配置错误明确区分。

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

## ADDED Requirements

### Requirement: MotionWarp Translation Mode 必须表达唯一轨迹方法

系统 MUST支持`Disabled`、`ScaleToTarget`、`SkewToTarget`与`LinearToTarget`。Scale MUST使用源窗口累计轨迹到目标终点的稳定平面相似映射；Skew MUST在同一累计pose内组合源轨迹、累计yaw与endpoint residual；Linear MUST按Position Progress从窗口起始位置生成到有效目标的逐Tick轨迹。所有模式 MUST继续输出同一个Action channel motion delta并经过WorldSolver，MUST不直接写Transform。

#### Scenario: 只调整攻击距离

- **WHEN** 作者选择ScaleToTarget且源窗口终点向量有效
- **THEN** 源轨迹 MUST按稳定旋转与统一比例映射到有效目标位移
- **AND** 窗口终点 MUST达到有效目标位置

#### Scenario: 明确直线移动到目标

- **WHEN** 作者选择LinearToTarget
- **THEN** Gameplay平面轨迹 MUST由Position Progress与有效目标唯一生成
- **AND** 每Tickdelta MUST经过Motion accumulator与WorldSolver
- **AND** MUST不被实现为Teleport或Presentation位移

### Requirement: MotionWarp Rotation Method 必须表达唯一累计yaw方法

系统 MUST支持`ProgressCurve`、`ConstantRate`与`ScaleSourceYaw`。ProgressCurve MUST用canonical Yaw Progress分配最短角修正；ConstantRate MUST按窗口累计时间和最大角速度限制累计yaw；ScaleSourceYaw MUST按目标总yaw与源窗口总yaw的比例缩放源累计yaw。Rotation Mode为Disabled时 MUST保留源累计yaw且不消费Rotation Method参数。

#### Scenario: 固定角速度转向

- **WHEN** 作者选择ConstantRate
- **THEN** 每个累计时间点的yaw修正 MUST不超过最大角速度允许值
- **AND** 无法在窗口内完成的部分 MUST按显式Limit Policy处理

#### Scenario: 缩放转身动画原始yaw

- **WHEN** 作者选择ScaleSourceYaw且源窗口拥有非零总yaw
- **THEN** Runtime MUST保留源yaw随时间的形状并缩放到有效目标yaw
- **AND** 源总yaw为零时 MUST明确失败而不是切换ProgressCurve
