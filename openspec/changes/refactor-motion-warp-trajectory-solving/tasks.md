# Tasks: 重构 MotionWarp 目标姿态与累计轨迹解算

## 1. 最终基线与资产盘点

- [x] 1.1 使用UTF-8重读本change的proposal、design、tasks与全部spec delta。
- [x] 1.2 记录最终安装的Character Operation Set、Float32 ABI/codec、Fixed ABI/codec与Program/Layout format版本。
- [x] 1.3 记录`MotionWarpClip`、`MotionWarpTrack`与全部authoring mutation入口。
- [x] 1.4 记录Timeline Inspector、Curve Catalog与MotionWarp validator入口。
- [x] 1.5 记录Timeline emitter、Semantic IR字段、motion source reference与source map入口。
- [x] 1.6 记录Float32 Program descriptor、codec、state slot与runtime入口。
- [x] 1.7 记录Fixed Program descriptor、codec、state slot与runtime入口。
- [x] 1.8 记录portable Reader、Semantic IR Inspector、Program Inspector与Trace入口。
- [x] 1.9 记录最终Agent v15 Snapshot、Patch、lowerer、handler、validator与MCP bridge入口。
- [x] 1.10 通过正式Agent Snapshot盘点全部可达MotionWarp Track/Clip及source identity。
- [x] 1.11 记录全部旧PositionMode、RotationMode、offset、weight、clamp与progress curve值。
- [x] 1.12 确认是否存在Corin五段攻击之外的非空MotionWarp资产。
- [x] 1.13 确认是否存在非1的PositionWeight或YawWeight并记录其作者意图缺口。
- [x] 1.14 记录Corin五段攻击的主MotionCurve、AnimationTrack、命中TreeClip/Cue与后摇边界。
- [x] 1.15 确认active Agent v15 change已经完成；未完成时停止Agent与资产迁移任务，不建立v14分支。

## 2. Portable MotionWarp语义

- [x] 2.1 定义portable `ProgramMotionWarpTranslationMode`。
- [x] 2.2 加入`Disabled`、`ScaleToTarget`、`SkewToTarget`与`LinearToTarget`稳定值。
- [x] 2.3 定义portable `ProgramMotionWarpTargetOffsetSpace`。
- [x] 2.4 加入`TargetLocal`、`ApproachDirection`、`ActorStartLocal`与`World`稳定值。
- [x] 2.5 保留并校正portable `ProgramMotionWarpRotationMode`目标语义。
- [x] 2.6 定义portable `ProgramMotionWarpRotationMethod`。
- [x] 2.7 加入`ProgressCurve`、`ConstantRate`与`ScaleSourceYaw`稳定值。
- [x] 2.8 定义portable `ProgramMotionWarpLimitPolicy`。
- [x] 2.9 加入`ApplyClamped`与`PreserveSource`稳定值。
- [x] 2.10 重构`ProgramMotionModifierDescriptor`保存新typed字段。
- [x] 2.11 将TargetPlanarOffset与TargetYawOffset保持为目标姿态字段。
- [x] 2.12 增加MaximumYawRateDegreesPerSecond常量引用。
- [x] 2.13 让PositionProgress只在Skew/Linear模式必需。
- [x] 2.14 让YawProgress只在ProgressCurve方法必需。
- [x] 2.15 删除descriptor中的PositionWeight与YawWeight常量引用。
- [x] 2.16 更新portable descriptor构造校验拒绝未知组合。
- [x] 2.17 更新Operation Set manifest声明新MotionWarp语义。
- [x] 2.18 从最终基线提升Character Operation Set版本。

## 3. Timeline Authoring模型

- [x] 3.1 将authoring位置enum替换为四种Translation Mode。
- [x] 3.2 增加Target Offset Space authoring enum。
- [x] 3.3 增加Rotation Method authoring enum。
- [x] 3.4 增加Limit Policy authoring enum。
- [x] 3.5 将`TargetLocalPlanarOffset`重命名为`TargetPlanarOffset`。
- [x] 3.6 增加MaximumYawRateDegreesPerSecond字段。
- [x] 3.7 删除PositionWeight字段。
- [x] 3.8 删除YawWeight字段。
- [x] 3.9 更新`HasPositionWarp`与`HasYawWarp`派生条件。
- [x] 3.10 更新MotionWarp正式ConfigureAuthoring参数合同。
- [x] 3.11 更新MotionWarp copy/clone保持新字段与稳定source identity。
- [x] 3.12 更新Position Progress channel仅对Skew/Linear公开。
- [x] 3.13 更新Yaw Progress channel仅对ProgressCurve公开。
- [x] 3.14 更新Curve mutation使用新ConfigureAuthoring合同。
- [x] 3.15 删除旧weight与旧target-local字段的反射显示和序列化入口。

## 4. Authoring校验与Inspector

- [x] 4.1 校验全部Translation Mode、Offset Space、Rotation Mode、Rotation Method与Limit Policy合法。
- [x] 4.2 校验位置启用时TargetPlanarOffset与MaximumPlanarCorrection有限且合法。
- [x] 4.3 校验Rotation启用时TargetYawOffset与MaximumYawCorrection合法。
- [x] 4.4 校验ConstantRate需要正的有限MaximumYawRate。
- [x] 4.5 在authoring与Semantic发布前校验ScaleToTarget源窗口终点非零，并保留runtime invariant检查。
- [x] 4.6 在authoring与Semantic发布前校验ScaleSourceYaw源窗口总yaw非零，并保留runtime invariant检查。
- [x] 4.7 校验Skew/Linear必须拥有canonical Position Progress。
- [x] 4.8 校验ProgressCurve必须拥有canonical Yaw Progress。
- [x] 4.9 禁止不消费的curve/rate/offset字段进入编译hash。
- [x] 4.10 保持Warp窗口完整位于source StartFrame到CurveEndFrame内。
- [x] 4.11 保持同一source的Warp窗口不重叠。
- [x] 4.12 更新Inspector显示Source、模式、空间、offset、限制与策略。
- [x] 4.13 按Translation Mode条件显示Position Progress。
- [x] 4.14 按Rotation Method条件显示Yaw Progress或Maximum Yaw Rate。
- [x] 4.15 让Inspector错误继续来自唯一MotionWarp validator。
- [x] 4.16 禁止Inspector自动Clamp、补curve、猜offset空间或转换旧字段。
- [x] 4.17 Authoring与Semantic发布拒绝带Gameplay Ease或非单位WeightCurve的MotionWarp source。

## 5. Semantic IR与Frontend

- [x] 5.1 更新Timeline emitter写入Translation Mode。
- [x] 5.2 更新Timeline emitter写入Target Offset Space。
- [x] 5.3 更新Timeline emitter写入TargetPlanarOffset。
- [x] 5.4 更新Timeline emitter写入Rotation Mode与Rotation Method。
- [x] 5.5 更新Timeline emitter写入TargetYawOffset与Maximum Yaw Rate。
- [x] 5.6 更新Timeline emitter写入Limit Policy与两个最大修正。
- [x] 5.7 仅为当前mode写入所需progress curve常量。
- [x] 5.8 删除Semantic IR中的position/yaw weight literal。
- [x] 5.9 更新Semantic IR MotionWarp typed validation。
- [x] 5.10 更新SemanticHash覆盖全部新字段且不覆盖未消费字段。
- [x] 5.11 保持Warp到source MotionCurve的唯一typed reference。
- [x] 5.12 保持Action Context与target requirement发布前校验。
- [x] 5.13 更新Semantic IR canonical codec格式版本。
- [x] 5.14 删除旧Semantic IR payload reader与旧字段解析。
- [x] 5.15 更新Semantic IR Inspector显示新目标姿态与solver语义。
- [x] 5.16 更新portable Reader显示新MotionWarp descriptor。

## 6. Program Layout与State合同

- [x] 6.1 更新Float32 Program MotionWarp descriptor lowering。
- [x] 6.2 更新Fixed Program MotionWarp descriptor lowering。
- [x] 6.3 更新两个Target Program descriptor校验。
- [x] 6.4 更新两个Target Program canonical codec字段顺序。
- [x] 6.5 更新两个Target LayoutHash与ProgramHash输入。
- [x] 6.6 增加WarpStartBodyPosition state semantic。
- [x] 6.7 增加WarpStartBodyYaw state semantic。
- [x] 6.8 增加SourceWindowStartPosition state semantic。
- [x] 6.9 增加SourceWindowStartYaw state semantic。
- [x] 6.10 增加ResolvedTargetPosition state semantic。
- [x] 6.11 增加ResolvedTargetYaw state semantic。
- [x] 6.12 增加PreviousWarpedCumulativePosition state semantic。
- [x] 6.13 增加PreviousWarpedCumulativeYaw state semantic。
- [x] 6.14 增加LimitResult state semantic。
- [x] 6.15 保留playback generation、ActionInstance与source operation identity state。
- [x] 6.16 删除MotionWarpTotalPlanarCorrection state semantic。
- [x] 6.17 删除MotionWarpTotalYawCorrection state semantic。
- [x] 6.18 删除旧WindowStart但未被新累计上下文消费的重复state。
- [x] 6.19 更新两个Target state slot emission与layout validation。
- [x] 6.20 从最终基线提升Float32 ABI、Program/Layout format与State codec identity。
- [x] 6.21 从最终基线提升Fixed ABI、Program/Layout format与State codec identity。
- [x] 6.22 删除旧Program、Layout与State codec reader。

## 7. Float32累计轨迹Runtime

- [x] 7.1 提取窗口StartFrame和EndFrame对应的源累计pose。
- [x] 7.2 以窗口StartFrame源pose为零点采样当前Source Window Pose。
- [x] 7.3 实现TargetLocal offset basis。
- [x] 7.4 实现ApproachDirection offset basis及零方向错误。
- [x] 7.5 实现ActorStartLocal offset basis。
- [x] 7.6 实现World offset basis。
- [x] 7.7 实现FaceTarget目标yaw与零方向错误。
- [x] 7.8 实现MatchTargetYaw目标yaw。
- [x] 7.9 实现ProgressCurve累计yaw solver。
- [x] 7.10 实现ConstantRate累计yaw solver与可达角计算。
- [x] 7.11 实现ScaleSourceYaw累计yaw solver与零源yaw错误。
- [x] 7.12 实现Disabled translation累计轨迹。
- [x] 7.13 实现ScaleToTarget平面相似映射与零源距离错误。
- [x] 7.14 实现SkewToTarget累计轨迹与endpoint residual。
- [x] 7.15 实现LinearToTarget累计轨迹。
- [x] 7.16 让Translation Solver消费同一累计yaw结果。
- [x] 7.17 实现MaximumPlanarCorrection与MaximumYawCorrection需要量计算。
- [x] 7.18 实现ApplyClamped有效Target Pose。
- [x] 7.19 实现PreserveSource typed结果且不初始化Warp state。
- [x] 7.20 用当前与previous Warped Cumulative Pose差生成Tick delta。
- [x] 7.21 取得resolved owner在本Tick进入Action channel的raw source delta。
- [x] 7.22 用warped source delta减raw source delta生成唯一modifier correction。
- [x] 7.23 保留同Action channel其它合法Additive与仲裁结果。
- [x] 7.24 禁止Warp correction再次按当前Body yaw旋转。
- [x] 7.25 保持source owner、window、ActionInstance与generation生命周期校验。
- [x] 7.26 保持Optional无目标时原样保留source且不初始化state。
- [x] 7.27 保持WorldSolver阻挡后不追补已损失delta。
- [x] 7.28 更新Float32 restore从累计state继续且不重复应用历史progress。
- [x] 7.29 删除旧CalculateTotalCorrection与独立residual增量实现。
- [x] 7.30 逻辑Tick跨越Warp边界时只替换窗口交集内的Float32 source delta。

## 8. Fixed累计轨迹Runtime

- [x] 8.1 用Fixed数值实现窗口Source Window Pose采样。
- [x] 8.2 用Fixed数值实现四种Target Offset Space。
- [x] 8.3 用Fixed数值实现FaceTarget与MatchTargetYaw。
- [x] 8.4 用Fixed数值实现ProgressCurve累计yaw solver。
- [x] 8.5 用Fixed数值实现ConstantRate累计yaw solver。
- [x] 8.6 用Fixed数值实现ScaleSourceYaw累计yaw solver。
- [x] 8.7 用Fixed数值实现Disabled translation累计轨迹。
- [x] 8.8 用Fixed数值实现ScaleToTarget平面相似映射。
- [x] 8.9 用Fixed数值实现SkewToTarget累计轨迹。
- [x] 8.10 用Fixed数值实现LinearToTarget累计轨迹。
- [x] 8.11 用Fixed数值实现两个Correction limit与Limit Policy。
- [x] 8.12 用Fixed累计pose差生成Tick delta。
- [x] 8.13 用Fixed warped source delta减raw source delta生成唯一modifier correction。
- [x] 8.14 保留同Action channel其它合法Additive与仲裁结果。
- [x] 8.15 保持Fixed source owner与lifecycle语义和Float32一致。
- [x] 8.16 保持Fixed Optional无目标与WorldSolver阻挡语义一致。
- [x] 8.17 更新Fixed restore从累计state继续。
- [x] 8.18 删除Fixed旧CalculateTotalCorrection与独立residual实现。
- [x] 8.19 确认Fixed实现不引用Float、Mathf、Unity向量或Float runtime。
- [x] 8.20 逻辑Tick跨越Warp边界时只替换窗口交集内的Fixed source delta。

## 9. Trace、Inspector与诊断

- [x] 9.1 定义Applied typed结果。
- [x] 9.2 定义AppliedClamped typed结果。
- [x] 9.3 定义PreservedByLimitPolicy typed结果。
- [x] 9.4 定义InvalidApproachBasis typed错误。
- [x] 9.5 定义ScaleSourceTranslationZero typed错误。
- [x] 9.6 定义ScaleSourceYawZero typed错误。
- [x] 9.7 Trace记录source window start/end与当前normalized time。
- [x] 9.8 Trace记录offset space、未限制Target Pose与有效Target Pose。
- [x] 9.9 Trace记录Translation Mode、Rotation Mode与Rotation Method。
- [x] 9.10 Trace记录Source Window Pose与Warped Cumulative Pose。
- [x] 9.11 Trace记录position/yaw current delta与最终Action channel。
- [x] 9.12 Trace区分limit、target missing、source not resolved与solver blocked。
- [x] 9.13 删除nominal curve-end、total position residual与total yaw residual旧Trace口径。
- [x] 9.14 更新Program Inspector与Live diagnostics摘要。

## 10. Agent v15唯一Authoring链

- [x] 10.1 基于最终v15 Character domain更新MotionWarp Snapshot DTO。
- [x] 10.2 Snapshot输出Translation Mode与Target Offset Space。
- [x] 10.3 Snapshot输出TargetPlanarOffset与Position Progress。
- [x] 10.4 Snapshot输出Rotation Mode、Rotation Method与TargetYawOffset。
- [x] 10.5 Snapshot输出Maximum correction、Maximum yaw rate与Limit Policy。
- [x] 10.6 删除Snapshot旧weight与旧target-local字段。
- [x] 10.7 更新Patch JSON schema接受全部新typed字段。
- [x] 10.8 更新Patch DTO与immutable command保存全部新字段。
- [x] 10.9 更新lowerer解析全部新enum与数值。
- [x] 10.10 更新handler只调用正式MotionWarp ConfigureAuthoring API。
- [x] 10.11 更新dry-run复用唯一MotionWarp validator。
- [x] 10.12 更新Agent Validator按mode检查curve/rate/offset/limit。
- [x] 10.13 保持generic `configure_timeline_curve_channel`编辑两条有效curve。
- [x] 10.14 更新MCP bridge只透传v15 generic transaction。
- [x] 10.15 更新`.codex/skills/btsmtl-agent-authoring`的MotionWarp字段与工作流。
- [x] 10.16 删除旧MotionWarp Agent payload、alias、reader与converter。

## 11. Corin五段攻击迁移

- [x] 11.1 导出最新Corin v15 Full Snapshot并锁定source revision。
- [x] 11.2 根据Attack1源MotionCurve、AnimationTrack与命中事实确定独立Warp窗口。
- [x] 11.3 根据Attack2源MotionCurve、AnimationTrack与命中事实确定独立Warp窗口。
- [x] 11.4 根据Attack3源MotionCurve、AnimationTrack与命中事实确定独立Warp窗口。
- [x] 11.5 根据Attack4源MotionCurve、AnimationTrack与命中事实确定独立Warp窗口。
- [x] 11.6 根据Attack5源MotionCurve、AnimationTrack与命中事实确定独立Warp窗口。
- [x] 11.7 为五段攻击配置SkewToTarget与ApproachDirection。
- [x] 11.8 为五段攻击分别配置TargetPlanarOffset与MaximumPlanarCorrection。
- [x] 11.9 为五段攻击配置FaceTarget与ProgressCurve。
- [x] 11.10 为五段攻击分别配置TargetYawOffset与MaximumYawCorrection。
- [x] 11.11 为五段攻击分别配置Position Progress与Yaw Progress，不复用通用线性模板。
- [x] 11.12 保持五段攻击Limit Policy为显式ApplyClamped。
- [x] 11.13 确认五段Warp只引用各自主攻击MotionCurve。
- [x] 11.14 确认所有后摇MotionCurve均未绑定Warp。
- [x] 11.15 dry-run同一份Corin Patch并处理全部正式validator错误。
- [x] 11.16 apply未经修改的同一份Patch。
- [x] 11.17 re-export确认五段source identity、窗口和参数与Patch一致。
- [x] 11.18 运行正式Agent validate确认Graph、Timeline、Action Context与MotionWarp闭环。

## 12. Artifact重建与旧路径删除

- [x] 12.1 从最新Corin authoring重新生成canonical Semantic IR artifact。
- [x] 12.2 从同一Semantic IR重新生成Float32 Program与Projection。
- [ ] 12.3 从同一Semantic IR重新生成Fixed Program与匹配identity。
- [ ] 12.4 校验ProgramHash、LayoutHash、Operation Set与Target ABI来自新合同。
- [x] 12.5 删除旧PositionMode序列化值与解析分支。
- [x] 12.6 删除旧TargetLocalPlanarOffset字段名与读取路径。
- [x] 12.7 删除PositionWeight与YawWeight全部代码和资产字段。
- [x] 12.8 删除旧total correction state slot与codec字段。
- [x] 12.9 删除旧Program/Semantic reader与版本兼容路径。
- [x] 12.10 删除任何一次性migrator、YAML patch或默认转换代码。
- [x] 12.11 搜索确认Runtime只有唯一TimelineMotionWarp modifier入口。
- [x] 12.12 搜索确认MotionWarp不直接写Transform、Body state或调用WorldSolver。
- [x] 12.13 搜索确认AnimationTrack、Animancer与Presentation不产生Warp位移。
- [x] 12.14 搜索确认Float32与Fixed都实现全部声明mode且没有unsupported fallback。

## 13. 文档与静态验证

- [x] 13.1 更新`openspec/project.md`的MotionWarp目标姿态、累计轨迹state与Operation Set口径。
- [x] 13.2 更新受影响current specs并删除固定总修正旧描述。
- [x] 13.3 更新实现inventory记录新authoring到WorldSolver唯一链路。
- [x] 13.4 更新普通.NET Reader输出说明。
- [x] 13.5 使用带`--disable-build-servers /nr:false /p:UseSharedCompilation=false`的命令编译portable Core/Float32/Fixed相关工程。
- [x] 13.6 编译后立即执行`dotnet build-server shutdown`。
- [x] 13.7 使用同样build-server参数编译Unity生成Runtime与Editor程序集。
- [x] 13.8 编译后立即执行`dotnet build-server shutdown`。
- [x] 13.9 运行`openspec validate refactor-motion-warp-trajectory-solving --strict --no-interactive`。
- [ ] 13.10 确认全部task真实完成后再统一标记为已完成。
