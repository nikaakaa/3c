## 1. 锁定主要参考与实施边界

- [x] 1.1 读取本地`com.janooba.kcc` package metadata并确认版本为`1.0.1`。
- [x] 1.2 校验`KinematicCharacterMotor.cs` SHA-256与proposal锁定值一致。
- [x] 1.3 校验`package.json` SHA-256与proposal锁定值一致。
- [x] 1.4 建立Philippe movement policy到Fixed Motor的逐方法映射清单。
- [x] 1.5 标出Philippe源码中全部Sweep、Raycast和Overlap调用点。
- [x] 1.6 标出Philippe源码中全部会读取上一帧Grounding状态的分支。
- [x] 1.7 标出不进入本change的Rigidbody、PhysicsMover、任意Up和Controller callback分支。
- [x] 1.8 确认正式asmdef、manifest和Player依赖不引用本地参考package。

## 2. 补齐canonical Fixed Raycast

- [x] 2.1 定义Fixed raycast输入参数、hit与query summary合同。
- [x] 2.2 为raycast hit加入SurfaceId。
- [x] 2.3 为raycast hit加入PrimitiveId与FeatureId。
- [x] 2.4 为raycast hit加入Fixed distance、point与normal。
- [x] 2.5 实现Plane解析raycast。
- [x] 2.6 实现Box slab raycast。
- [x] 2.7 实现one-sided Triangle raycast。
- [x] 2.8 为Box边界和Triangle边界定义canonical feature tie-break。
- [x] 2.9 按SurfaceId、PrimitiveId、FeatureId和point固定raycast hit排序。
- [x] 2.10 将raycast平行、距离和退化容差纳入query identity。
- [x] 2.11 将raycast candidate与hit容量纳入locked configuration。
- [x] 2.12 让raycast复用现有collision world index与预分配workspace。
- [x] 2.13 删除Step或Grounding中用capsule landing近似reference ray probe的helper。

## 3. 建立Philippe语义的Fixed报告合同

- [x] 3.1 定义唯一Fixed Hit Stability Report。
- [x] 3.2 加入base stable与最终IsStable字段。
- [x] 3.3 加入FoundInnerNormal与InnerNormal字段。
- [x] 3.4 加入FoundOuterNormal与OuterNormal字段。
- [x] 3.5 加入ValidStepDetected与SteppedSurfaceId字段。
- [x] 3.6 加入LedgeDetected字段。
- [x] 3.7 加入IsOnEmptySideOfLedge字段。
- [x] 3.8 加入DistanceFromLedge字段。
- [x] 3.9 加入IsMovingTowardsEmptySideOfLedge字段。
- [x] 3.10 加入LedgeGroundNormal与canonical ledge direction字段。
- [x] 3.11 扩展Fixed Ground Report保存inner/outer normal与SnappingPrevented。
- [x] 3.12 保持报告不引用Unity Collider、RaycastHit或Vector3。

## 4. 扩展Deterministic KCC前态

- [x] 4.1 为Body State加入previous FoundAnyGround。
- [x] 4.2 保留previous IsStableOnGround。
- [x] 4.3 将support SurfaceId加入Body State。
- [x] 4.4 保留support PrimitiveId与FeatureId。
- [x] 4.5 为Body State加入GroundNormal。
- [x] 4.6 为Body State加入InnerGroundNormal。
- [x] 4.7 为Body State加入OuterGroundNormal。
- [x] 4.8 为Body State加入SnappingPrevented。
- [x] 4.9 为Body State加入LedgeState。
- [x] 4.10 为Body State加入LastMovementIterationFoundAnyGround。
- [x] 4.11 更新Body State不变量并拒绝自相矛盾组合。
- [x] 4.12 升级KCC state codec并删除旧codec reader。
- [x] 4.13 将新增前态纳入snapshot与world hash。
- [x] 4.14 确认KCC state不复制Position与VerticalVelocity所有权。

## 5. 按Philippe顺序实现Hit Stability

- [x] 5.1 从hit normal和MinimumGroundNormalY计算base稳定性。
- [x] 5.2 从hit normal计算障碍内侧水平单位方向。
- [x] 5.3 用Fixed raycast执行inner ledge probe。
- [x] 5.4 用Fixed raycast执行outer ledge probe。
- [x] 5.5 分别保存inner与outer normal稳定性。
- [x] 5.6 只在inner与outer稳定性不同的时候标记LedgeDetected。
- [x] 5.7 计算角色位于ledge空侧还是实侧。
- [x] 5.8 计算角色轴线到ledge的Fixed平面距离。
- [x] 5.9 从requested planar displacement计算是否朝空侧移动。
- [x] 5.10 按MaximumStableDistanceFromLedge取消越界稳定性。
- [x] 5.11 按当前inner/outer normal与previous inner normal计算denivelation变化。
- [x] 5.12 按MaximumStableDenivelationAngle设置SnappingPrevented。
- [x] 5.13 只在最终hit不稳定时进入Step Detection。
- [x] 5.14 删除当前Step Support Evaluator对edge contact的第二套判定。

## 6. 实现Standard-first Step Detection

- [x] 6.1 从character position、hit point与MaximumStepHeight计算standard step cast起点。
- [x] 6.2 沿障碍内侧加入锁定的CollisionOffset偏移。
- [x] 6.3 从standard起点向下执行完整capsule cast。
- [x] 6.4 收集MaximumStepHeight加CollisionOffset范围内的全部canonical hit。
- [x] 6.5 按最远向下距离优先选择待检查hit。
- [x] 6.6 用canonical identity解决相同距离tie。
- [x] 6.7 在每个候选最终角色位置执行capsule overlap。
- [x] 6.8 overlap存在时丢弃当前候选并继续下一个hit。
- [x] 6.9 从候选world hit point执行outer stable ray probe。
- [x] 6.10 outer ray normal不稳定时丢弃当前候选。
- [x] 6.11 从当前角色位置向上capsule cast候选实际rise。
- [x] 6.12 向上clearance受阻时丢弃当前候选。
- [x] 6.13 在角色中心对应高度执行inner stable ray probe。
- [x] 6.14 中心inner失败后从hit point内侧短偏移执行第二inner ray probe。
- [x] 6.15 inner stable后记录候选SurfaceId为SteppedSurfaceId。
- [x] 6.16 不要求outer、inner与blocker具有相同PrimitiveId。
- [x] 6.17 不要求outer与inner capsule contact高度近似相等。

## 7. 实现唯一Extra Step补充路径

- [x] 7.1 只在Standard没有valid step时进入Extra路径。
- [x] 7.2 从角色中心按MinimumRequiredStepDepth进入障碍内侧。
- [x] 7.3 在MaximumStepHeight上方建立Extra capsule cast起点。
- [x] 7.4 向下执行Extra capsule cast。
- [x] 7.5 让Extra hit复用唯一CheckStepValidity流程。
- [x] 7.6 将MinimumRequiredStepDepth限制为大于零且不超过CapsuleRadius。
- [x] 7.7 删除MinimumStepDepth旧命名与旧高度一致性语义。
- [x] 7.8 不增加Standard/Extra serialized开关。

## 8. 在movement loop提交Step

- [x] 8.1 只允许当前movement sweep contact触发Step Commit。
- [x] 8.2 要求Hit Stability Report包含ValidStepDetected。
- [x] 8.3 按VerticalObstructionCorrelation确认近似垂直障碍。
- [x] 8.4 要求previous state稳定grounded。
- [x] 8.5 明确向上MotionRequest时禁止Step Commit。
- [x] 8.6 从safe position沿障碍内侧前移SteppingForwardDistance。
- [x] 8.7 从MaximumStepHeight上方向下执行commit capsule cast。
- [x] 8.8 只接受SurfaceId等于SteppedSurfaceId的landing。
- [x] 8.9 按canonical顺序选择同SurfaceId landing。
- [x] 8.10 以CollisionOffset建立最终step position。
- [x] 8.11 提交前执行最终capsule overlap。
- [x] 8.12 提交后把运动方向投影到水平面。
- [x] 8.13 保留尚未消费的remaining magnitude。
- [x] 8.14 将remaining重新送回同一multi-plane movement loop。
- [x] 8.15 Step rejection继续使用原contact执行普通projection。
- [x] 8.16 Step处理不写入或推导VerticalVelocity。

## 9. 统一Ground Probe、Snap与下降

- [x] 9.1 实现MinimumGroundProbingDistance短探测路径。
- [x] 9.2 previous snap未被禁止且previous stable时启用扩展ground probe。
- [x] 9.3 LastMovementIterationFoundAnyGround时启用扩展ground probe。
- [x] 9.4 扩展ground probe使用max(Radius, MaximumStepHeight)。
- [x] 9.5 将GroundDetectionExtraDistance加入扩展探测距离。
- [x] 9.6 ground capsule sweep命中后生成唯一Hit Stability Report。
- [x] 9.7 stable且未SnappingPrevented时提交ground snap位置。
- [x] 9.8 stable但SnappingPrevented时保留movement位置。
- [x] 9.9 非稳定ground hit按GroundProbeReboundDistance更新探测起点。
- [x] 9.10 非稳定ground hit沿命中面投影剩余探测方向。
- [x] 9.11 固定Ground Probe最大迭代与query budget。
- [x] 9.12 明确向上MotionRequest只走短探测且不提交snap。
- [x] 9.13 删除独立TryStepDown请求与candidate事务。
- [x] 9.14 删除GroundSnapDistance字段与微距Snap专用算法。
- [x] 9.15 删除SteppedDown对求解流程的控制语义。

## 10. 收敛唯一Motor策略

- [x] 10.1 将ground projection改为读取扩展previous grounding状态。
- [x] 10.2 保持initial penetration recovery在movement policy前执行。
- [x] 10.3 让ground hit与movement hit复用同一EvaluateHitStability。
- [x] 10.4 让Step Commit与普通multi-plane projection共享同一iteration budget。
- [x] 10.5 Step成功后清理失效constraint plane。
- [x] 10.6 Step成功后使用landing normal建立唯一support constraint。
- [x] 10.7 每轮movement更新LastMovementIterationFoundAnyGround。
- [x] 10.8 final ground report只来自统一Ground Probe与最终contact。
- [x] 10.9 Actor contact后static reconstraint复用同一Motor策略。
- [x] 10.10 保持ResolveBatch一次原子commit边界。

## 11. 删除未验收Step实现

- [x] 11.1 删除`DeterministicKccStepSolver`。
- [x] 11.2 删除`DeterministicKccStepGeometry`。
- [x] 11.3 删除`DeterministicKccStepSupportEvaluator`。
- [x] 11.4 删除outer/inner capsule landing request与result合同。
- [x] 11.5 删除primitive adjacency Step准入。
- [x] 11.6 删除outer/inner landing高度一致性检查。
- [x] 11.7 删除独立Step Down candidate合同。
- [x] 11.8 删除旧Step phase、stage与rejection枚举。
- [x] 11.9 删除Motor持有的第二Step Solver对象。
- [x] 11.10 搜索确认运行时不存在旧TryStep或当前未验收算法。
- [x] 11.11 搜索确认不存在fallback、mode开关或兼容reader。

## 12. 迁移正式配置与身份

- [x] 12.1 将SkinWidth收敛为唯一CollisionOffset语义。
- [x] 12.2 新增GroundDetectionExtraDistance正式字段。
- [x] 12.3 新增GroundProbeReboundDistance正式字段。
- [x] 12.4 新增MinimumGroundProbingDistance正式字段。
- [x] 12.5 新增SecondaryProbeVerticalDistance正式字段。
- [x] 12.6 新增SecondaryProbeHorizontalDistance正式字段。
- [x] 12.7 新增SteppingForwardDistance正式字段。
- [x] 12.8 新增MinimumRequiredStepDepth正式字段。
- [x] 12.9 新增MaximumStableDistanceFromLedge正式字段。
- [x] 12.10 新增MaximumStableDenivelationAngle正式字段。
- [x] 12.11 新增VerticalObstructionCorrelation正式字段。
- [x] 12.12 删除GroundSnapDistance与MinimumStepDepth旧字段。
- [x] 12.13 更新DeterministicKccConfiguration constructor与不变量。
- [x] 12.14 更新configuration canonical hash顺序。
- [x] 12.15 更新Unity Solver Definition唯一映射。
- [x] 12.16 原位迁移`CorinDeterministicKcc.asset`。
- [x] 12.17 升级Motor semantic version。
- [x] 12.18 升级WorldSolver version。
- [x] 12.19 升级KCC identity schema。
- [x] 12.20 删除旧字段reader与旧identity接受路径。

## 13. 修正Gameplay Lab正式环境

- [x] 13.1 定位LowStairs与GentleRamp_12deg的作者Transform和碰撞范围。
- [x] 13.2 将低楼梯与坡道迁移为不重叠的独立路线。
- [x] 13.3 保持0.14m低楼梯的rise与tread作者尺寸。
- [x] 13.4 保持0.24m高楼梯的rise与tread作者尺寸。
- [x] 13.5 保持0.40m超限楼梯表达拒绝边界。
- [x] 13.6 删除缺失Mesh/Collider的OpenKCC楼梯实例或补成有效静态参考资产，二者只保留一个正式结果。
- [x] 13.7 补齐0.40m作者几何后重新生成唯一Deterministic Collision World artifact。
- [x] 13.8 升级CollisionWorldHash并删除旧artifact reader或镜像。
- [x] 13.9 保持Local Fixed与Rollback Variant引用同一KCC和collision identity。

## 14. 收口诊断与正式文档

- [x] 14.1 用Philippe语义阶段重写KCC movement diagnostics。
- [x] 14.2 输出base stability、inner/outer probe与ledge结果。
- [x] 14.3 输出Step Standard或Extra来源与SteppedSurfaceId。
- [x] 14.4 输出Step Detection和Commit唯一拒绝原因。
- [x] 14.5 输出Ground Probe距离、SnappingPrevented与denivelation结果。
- [x] 14.6 保持diagnostics不进入snapshot或hash。
- [x] 14.7 更新current `deterministic-kcc-world-solver` spec为实现后的唯一事实。
- [x] 14.8 更新`openspec/project.md` Current State并删除未验收旧算法口径。
- [x] 14.9 更新Deterministic KCC第三方说明的主要参考版本与边界。
- [x] 14.10 更新Rollback产品闭包清单使用新KCC identity。
- [x] 14.11 搜索并删除旧change名称、旧算法术语和失效任务引用。

## 15. 修正真实Fixed重力与楼梯接缝链路

- [x] 15.1 沿Fixed Body Motion Integrator确认每Tick完整XYZ请求包含已积分重力位移。
- [x] 15.2 在previous stable且没有向上意图时用当前稳定地面约束请求中的负Y分量。
- [x] 15.3 按Philippe `GetObstructionNormal`用previous ground normal重算非稳定hit的有效障碍法线。
- [x] 15.4 确保Step Detection提升后的最终`IsStable`不冒充hit normal的base stability。
- [x] 15.5 让垂直障碍判断、Step Commit前进方向和普通constraint plane共用有效障碍法线。
- [x] 15.6 在Step Detection候选位置扣除`CollisionOffset`。
- [x] 15.7 在Step Commit最终位置扣除`CollisionOffset`。
- [x] 15.8 将collision baker的`SurfaceId`分配粒度从Surface Authoring收敛为单个作者Collider。
- [x] 15.9 保持同一MeshCollider或TerrainCollider生成的全部Primitive共享一个`SurfaceId`。
- [x] 15.10 保持Step Detection候选可与movement blocker属于不同`SurfaceId`，只约束Detection与Commit的`SteppedSurfaceId`一致。
- [x] 15.11 按farthest-first与canonical identity固定等距离Step候选顺序。
- [x] 15.12 将LowStairs顶平台Collider起点接到`LowStep_12`末端并删除重叠。
- [x] 15.13 将HighStairs顶平台Collider起点接到`HighStep_08`末端并删除重叠。
- [x] 15.14 升级collision artifact schema并删除旧artifact接受路径。
- [x] 15.15 升级Motor semantic、WorldSolver version与KCC identity schema。
- [x] 15.16 用正式Unity菜单重新生成唯一Deterministic Collision World artifact。
- [x] 15.17 按最终代码更新current spec、project current state与实施清单。
