# Design

## 0. 当前实施阶段：实时 Landing 与最终 Goal 换代

当前代码已经实施每只脚的 Landing Event 生命周期重构和最终 Goal 换代。Landing、Ground Path 与 Envelope 只表达本帧当前有效预测；连续性只由写入唯一 GoalSet 前的相对踝骨修正换代负责。已确认的运行事实仍只有 Accepted Landing 与 Ground Path 线；实时跨踏面更新、Goal 换代后的物理踝骨、Ground Envelope、Swing Foot Motion、支撑锁脚、Pelvis、脚掌朝向、Pivot、FullBodyIK 消费和最终物理骨骼写入都不在当前验收结论内。

Landing 生命周期是 Foot Placement 的状态 Module，不是查询算法。每只脚只保存一个完整 Committed Frame 和一个完整 Pending Frame，不再把相同字段拆成两组平行变量。状态只在外层表现事务 Seal 后推进，Discard 不得改变上一份已提交事实：

```text
Empty
-> Tracking
-> Accepted
-> Tracking（本帧预测失败，旧事实只保留为晋级历史，不再发布当前 Path）
-> Accepted（同一事件的最新合法命中；死区内可复用上一命中）
-> Empty（事件完成后晋级 LastLanding；同帧新事件可再进入 Tracking / Accepted）
```

预测失败或没有合法候选时，生命周期保留该事件最后一个有效落点只供完成时晋级，但本帧 Snapshot 必须回到 `Tracking` 且不发布 `NextSwingLanding`。Ground Path 因而在同一事务中发布 Rejected 并清空当前 Envelope，不能继续显示旧踏面。只有本帧合法命中，Snapshot 才发布唯一当前 Accepted 落点。该 Module 不拥有 Ground Path、Envelope、Goal 或骨骼写入。

## 1. 唯一计算链

完整预测 IK 仍然只有一条 Foot Placement 链，不增加第二个 Solver：

```text
动画 Foot Analysis
-> Current / Incoming Step
-> 每脚唯一查询事件选择
-> committed Body 速度 + Future Body Translation
-> Action 占脚、Grounded 与 HorizontalSpeed 正式输入
-> 可见 yaw 与虚拟 trajectory revision Pose
-> Raw Landing + 唯一 SphereCast
-> LastLanding + NextSwingLanding
-> Capsule Ground Path
-> Edge / Reachability / 上侧 Hull / Ground Envelope
-> 当前步伐
-> PelvisPreSolveTranslation
-> Swing Foot Motion
-> Support Foot Plant
-> Locked / Sliding / Unlocked
-> Support Foot Orientation
-> 同一 Final Goal Set
-> 唯一 FullBodyIK
-> 唯一 final writer
-> Physical Bones
```

IK Solver 只执行最终目标，不查询地面、不决定支撑脚、不锁脚、不做后处理。

## 2. 本 change 直接消费的已有事实

- 左右脚独立的 Foot Analysis、Current/Incoming Step、`IsSwing`、`TimeToLandingSeconds`、`LandingEventIdentity` 和 root-local landing。
- 每个有效表现帧先按 Step header 为每只脚选择零个或一个查询事件，再至多执行一次正式 Landing SphereCast；不能先查 Current、再查 Incoming 后择优。没有命中就发布 typed `GroundQueryMissed`，不造默认地面。
- `LastLanding` 和 `NextSwingLanding` 组成 Ground Path 轴；没有 LastLanding 就发布 `CurrentLandingUnavailable`。
- Capsule 只表示路径采集范围，不表示鞋底或最终脚轨迹。
- 所有 Edge 必须通过 `MaximumReachableVerticalEdge`；失败不得删除障碍点后继续构造 Hull。
- Ground Envelope 是 feet-only 的地面下界，不改变脚的水平动画进度，不替代 KCC。
- 摆动脚垂直修正为：`Corrected = Original + up * max(0, EnvelopeSample - BaselineSample)`。
- Pelvis、LeftFoot、RightFoot 三个 Goal slot、`PelvisPreSolveTranslation`、一次 FullBodyIK 和一次 final writer。
- `CharacterFootPlacementPoseInput.Contributions` 中的 `AnimationPoseSourceContribution`，包括 `SourceId.SourceActionInstanceId`、`LeftFootWeight` 与 `RightFootWeight`。
- `CharacterPresentationFactFrame.Grounded` 与 `CharacterPresentationFactFrame.HorizontalSpeed`。

某只脚的有限 Action 占用定义为：同帧存在 `Live` Pose Contribution，`SourceId.SourceActionInstanceId != 0`，且该脚的 Contribution Foot Weight 大于几何容差。Captured/Stored Contribution、普通 Locomotion Contribution 和零脚权重不得创建 Action 占用。若多个 Action Contribution 同时满足条件，诊断记录脚权重最大的 ActionInstance；权重相同时按 Contribution 的稳定输入顺序选择。Foot Placement 不查询 Action Runtime、Timeline owner 或作者字符串。

`Confidence` 只进诊断，不改变 Goal 权重。空中、无权威 Step、有限 Action 占用或 Ground Path rejected 时，不另做传统 IK；无效 Goal 发布原生事实和零权重。跑步朝向关闭只读 `CharacterPresentationFactFrame.HorizontalSpeed`，不得从 Step、输入幅值、动画位移或可见 Transform 差分重建速度。

## 3. 同事件实时落点与唯一当前 Path

`LandingEventIdentity` 只表示同一次迈步，不表示该事件必须绑定首次命中的踏面。楼梯上身体继续移动时，同一事件的合法预测可以从当前踏面切到下一踏面；`SurfaceIdentity`、高度和法线都属于本帧命中事实，不能成为冻结旧落点的门槛。

每只脚在世界查询前先处理事件换代：

1. Current Step 已完成、其 identity 等于 Pending `NextSwingLanding` 时，把缓存的最后 Accepted `NextSwingLanding` 原值晋级为 `LastLanding`，包括点、法线、Surface、Landing Event 与 Accepted trajectory revision identity；完成帧不为晋级查询地面。
2. 在尚未完成、identity 不等于 `LastLanding` 的权威 Current / Incoming PreSwing 或 Swing 中选择 `TimeToLandingSeconds` 较小者作为唯一查询事件；相等时稳定选择 Current。
3. 候选 header 自身非法、时间越界或身份不一致时直接发布 typed rejection，不查询另一候选作为 fallback。
4. 只有选中的事件执行本帧该脚唯一 SphereCast。没有候选时该脚查询次数为零。

同一事件仍处于 PreSwing / Swing 时：

- 每个有效表现帧继续执行唯一正式 SphereCast。
- 新命中与当前 Accepted 点距离小于 `LandingUpdateDistance` 时，复用 Accepted Landing 和已提交 Ground Path，但下一帧仍继续预测。
- 新命中超过死区时，无论 SurfaceIdentity 或沿 Component Up 的高度是否变化，都接受新点，并用这一次 SphereCast 结果重建同一 Foot Placement 事务中的唯一 Ground Path。
- 查询失败、Selected Event 无效或没有合法候选时，当前 Snapshot 不发布 `NextSwingLanding`，Ground Path 发布 typed rejection，Envelope 为空。生命周期内部最后有效命中只允许用于事件完成晋级，不能继续驱动线或 Goal。
- Swing Event 完成时，最后一个 Accepted `NextSwingLanding` 才原值晋级为 `LastLanding`；完成帧的新命中、Current Sole 或默认点都不得替换它。晋级后才允许 Incoming 成为本帧唯一查询事件。

因此 Scene 中的落点、Path 与 Envelope 始终表达同一个当前有效查询 revision；它们可以跳到新的真实踏面，但不能冻结旧踏面。

## 4. 转向与唯一 trajectory revision

GDC 的 Pivot 目的是让未来脚步路线围绕接触脚改变，而不是把身体和两只脚绕胶囊原点一起甩。项目不修改实体 Pose，也不刚体旋转已经提交的旧 Foot Route、命中 Surface 或 Ground Envelope；Foot Placement 用独立虚拟 Body Pose 生成新的查询事实。

每个 Actor 只有一份 trajectory revision Pending/Committed 状态，左右脚共享：

```text
TrajectoryRevisionIdentity
VisiblePositionAtCommit
VisibleRotationAtCommit
VisibleForwardAtCommit
RevisionPosition
RevisionRotation
RevisionForward
ResidualYawDegrees
PivotSupportSide
PivotSupportLandingEventIdentity
```

`TrajectoryRevisionIdentity` 是 Foot Placement 自己单调递增的非零 identity，不得复用 Future Body `TrajectoryGeneration`、Timeline AuthorityTick、Landing Event identity 或 Render Frame。每个执行查询的 Pending Frame 只分配一个 revision identity，左右脚本帧查询、Accepted Landing、Ground Path 与 Envelope 必须携带同一 identity。更新死区内复用旧 Accepted Landing 时，必须同时保留其旧 Accepted revision identity，并把本帧尝试 identity 单独写入诊断。

Pending 开始时完整复制 Committed revision。首次建立、Body discontinuity、主支撑 identity 变化或旧主支撑失效时，把 Revision Position/Rotation/Forward 对齐当前 Visible Pose，Residual yaw 清零；该帧只建立新基准，不把旧 Pivot offset 搬到新支撑。

Pivot 候选在计算几何前只选择一次。上一 Committed 主支撑仍满足 Grounded、LastLanding、非 Swing、非 Action 占用且已经 Locked 时继续使用；否则只在本帧 Locked 候选中按水平误差最小、再按稳定 Side 顺序选择。两脚都站住时也遵守这条规则，不能用 Sole 前后关系、当前 yaw 符号或每帧法线换枢轴。没有合法 Locked 候选时不应用 Pivot。

同一主支撑连续帧的计算如下。`currentVisiblePosition` 与 `currentVisibleRotation` 是尚未被 Foot Placement 改写的当前 Component Pose。Committed Revision 只保存上一成功帧的虚拟 Body Pose 连续性；它不是旧 Route、Surface 或 Envelope，也不能让这些旧查询事实参与新查询：

```text
currentVisibleForward = normalize(ProjectOnPlane(
    currentVisibleRotation * Vector3.forward, up))
visibleTranslation = currentVisiblePosition - committed.VisiblePositionAtCommit
visiblePositionForRevision = committed.RevisionPosition + visibleTranslation
visibleYawDelta = SignedAngle(committed.VisibleForwardAtCommit,
                              currentVisibleForward,
                              up)
requestedYaw = NormalizeSigned(committed.ResidualYawDegrees + visibleYawDelta)
pivotDelta = Clamp(requestedYaw,
                   -MaximumPivotYawDeltaDegrees,
                   +MaximumPivotYawDeltaDegrees)
pivotRotation = AngleAxis(pivotDelta, up)

virtualBodyPosition = LastLanding
                    + pivotRotation * (visiblePositionForRevision - LastLanding)
virtualBodyRotation = pivotRotation * committed.RevisionRotation
revisionPosition = virtualBodyPosition
revisionRotation = virtualBodyRotation
revisionForward = normalize(ProjectOnPlane(revisionRotation * Vector3.forward, up))
residualYaw = NormalizeSigned(requestedYaw - pivotDelta)

rawLanding = virtualBodyPosition
           + futureBodyTranslationWorld
           + virtualBodyRotation * RootLocalLanding
```

查询原点仍是 `rawLanding + up * CastAbove`。`futureBodyTranslationWorld` 是 KCC 在原世界空间积分和裁剪后的位移，绝不能随 `pivotRotation` 再旋转。`RevisionPosition` 与 `RevisionRotation` 只参与 Foot Placement 的 Raw Landing、Path 和 Envelope；不得写回 VisualRoot、KCC、Gameplay Body、Animator Root 或实体胶囊。

`MaximumPivotYawDeltaDegrees` 只限制本帧消化的 yaw；`ResidualYawDegrees` 必须作为状态留到下一个成功帧，不能丢弃或改写 Body。前向投影退化、没有合法 Locked 主支撑或 revision 输入非法时，本帧 `PivotApplied = false`，使用当前 Visible Pose 建立普通 Raw Landing，不猜前向。查询失败发布本 revision 的 typed rejection，不旋转旧 Path 作为替代。只有外层 Seal 才提交 revision Pose、identity、主支撑和 residual；Discard 完整恢复上一 Committed 状态。

## 5. 当前步伐仲裁

支撑脚是权威 Step 非 Swing、拥有有效 `LastLanding`、`CharacterPresentationFactFrame.Grounded` 为真且未被有限 Action 占用的脚。摆动脚是权威 Step 为 Swing、`NextSwingLanding` 存在、Event identity 与 Selected Query Step 一致且未被有限 Action 占用的脚。正式步伐起点是支撑 `LastLanding`，终点是 revision 后的摆动 `NextSwingLanding`。

两脚同时满足摆动合同且都有可用垂直包络增量时，只选择增量较大的一脚作为唯一摆动脚。另一脚只有拥有 `LastLanding` 且未被有限 Action 占用时才进入支撑合同，否则保持原生事实和零权重。

两脚都没有 LastLanding、身份不一致、没有唯一摆动脚或端点退化时，本帧没有步伐，Pelvis 权重为零；没有 LastLanding、未 Grounded 或被有限 Action 占用的脚支撑锁脚权重为零。若两脚都非 Swing、各自拥有有效 LastLanding、Grounded 且未被有限 Action 占用，则进入 `GroundedStationary`：不发布步伐骨盆和摆动 Envelope，但两脚都先按同一支撑接地与锁脚合同计算。

`GroundedStationary` 的 Pivot 主支撑必须稳定：

1. 上一 Committed `PivotSupportSide` 仍处于 Locked、Landing Event identity 未变且未被 Action 占用时继续使用，不重新比较左右脚。
2. 旧主支撑失效时，只在当前 Locked 候选中选择水平误差较小的一脚；误差相等时按 `Left`、`Right` 的稳定 Side 顺序选择。
3. Sole 前后位置、动画交叉次数、当前 yaw 符号和每帧法线不得作为主条件。
4. Pivot 主脚继续以自己的 `LastLanding` 发布 Locked Goal；另一脚用 Pivot 后同帧原生 Sole 对自己的 `LastLanding` 重新判定 Locked / Sliding / Unlocked。两脚不得因为 Pivot 同时被强制 Locked。

仍满足摆动包络合同的脚可以单独发布 Swing Goal，但它不构成 GroundedStationary。

支撑切换只由原摆动脚落地晋级或权威 Step identity 交换触发，不以 Sole 前后位置作为主条件。

`StrideSwitchCooldownSeconds` 只延迟两个仍然有效的步伐候选之间的切换。若旧支撑脚已经变成 Swing、失去 LastLanding 或被有限 Action 占用，旧合同立即失效，必须清除旧支撑和 Pelvis Goal，不能用冷却保持旧锁脚；新合同冷却期间也不能沿用旧目标填补。

## 6. 步伐骨盆

```text
forward = ProjectOnPlane(strideEnd - strideStart, up)
strideProgress = saturate(dot(poseRoot - strideStart, forward) / |forward|)
sampledGround = lerp(strideStart, strideEnd, strideProgress)
rise = dot(strideEnd - strideStart, up)
```

上坡且支撑已落地时，`rawPelvisDelta = up * rise * strideProgress`；下坡且支撑仍接触、摆动未落地时同样按 progress 下降；平地为零。它是相对当前步伐起点的有符号目标，不是世界绝对 Y，也不是 Set Mesh。

弹簧 Pending 必须在本帧计算前逐字段复制 Committed，不能从 `Clear()` 后的零状态开始。弹簧先解决相对坐标系，再使用闭式临界阻尼积分：

```text
rawPelvisTargetAlongUp = dot(rawPelvisDelta, up)
rebaseAlongUp = dot(previousStrideStart - strideStart, up)
rebasedPreviousRawTarget = previousRawPelvisTargetAlongUp + rebaseAlongUp
rebasedPreviousSpringOutput = previousSpringOutput + rebaseAlongUp
necessaryDelta = 支撑脚未切换时的
                 rawPelvisTargetAlongUp - rebasedPreviousRawTarget
                 支撑脚切换时为 0
springTarget = rawPelvisTargetAlongUp
springInput = rebasedPreviousSpringOutput + necessaryDelta

omega = 2 * PI * PelvisSpringFrequency
x0 = springInput - springTarget
v0 = previousSpringVelocity
j0 = v0 + omega * x0
decay = exp(-omega * deltaSeconds)
springOutput = springTarget + (x0 + j0 * deltaSeconds) * decay
springVelocity = (v0 - omega * j0 * deltaSeconds) * decay
springDelta = springOutput - necessaryDelta
pelvisDelta = up * springOutput
```

`previousStrideStart`、`previousRawPelvisTargetAlongUp`、`previousSpringOutput`、SupportSide 和弹簧速度属于 Foot Placement Pending/Committed 状态。支撑切换时旧相对高度必须按旧起点到新起点重基；不能把旧起点的厘米数直接与新起点的目标相减。如果没有有效旧起点，以当前目标和零速度初始化。`deltaSeconds = 0` 时原样保留输入和速度。Profile 只保留有限正数 `PelvisSpringFrequency`；临界阻尼固定为 `1`，删除可把行为改成欠阻尼或过阻尼的旧 `PelvisSpringDampingRatio` 配置。`springOutput` 是唯一最终输出，`springDelta` 只是诊断分解，不能再加回 Goal。

双脚垂直目标完成后，使用同帧原生动画净空：

```text
animatedClearance = 原生动画盆骨到更低原生脚
correctedClearance = (animatedPelvis + pelvisDelta) 到更低修正脚
若 correctedClearance < animatedClearance：把差值补进 pelvisDelta
```

没有完整步伐、Path rejected、空中或有限 Action 占用时，Pelvis Position Weight 为零，不沿用上一帧目标。Pelvis 只能进入 `PelvisPreSolveTranslation`，不能写 VisualRoot、Gameplay Body 或 KCC。

## 7. 摆动脚

摆动脚水平进度始终来自动画 Sole 在 revision 后 `LastLanding -> NextSwingLanding` 轴上的投影；最终脚不得低于 Ground Envelope。只把 `max(0, EnvelopeSample - BaselineSample)` 沿 Component Up 加到原生 Ankle，不直接把 NextSwingLanding 当 Ankle 目标，不使用输入方向重画轨迹，不再乘摆动相位或预测误差。

同一帧两脚不得同时用 Envelope 拉 FullBodyIK。脚进入 Locked 或 Sliding 后停止该脚 Envelope 采样和 NextSwingLanding 追踪，只对 LastLanding 负责。

## 8. 支撑脚接地与锁脚

先做垂直接地：

```text
plantHeight = max(0, dot(LastLanding - originalSole, up))
plantDelta = up * plantHeight
plantedSole = originalSole + plantDelta
plantedAnkle = originalAnkle + plantDelta
```

只允许非负 plantHeight，已经高于 LastLanding 时不得向下拉。

```text
lockedHorizontalOffset = ProjectOnPlane(LastLanding - originalSole, up)
horizontalError = |lockedHorizontalOffset|
```

- `horizontalError <= LockDistance`：`Locked`。
  `lockedSole = plantedSole + lockedHorizontalOffset`。水平对齐 LastLanding，垂直与 plantedSole 完全相同，不重复加高。
- `LockDistance < horizontalError <= SlideDistance`：`Sliding`。
  `slideT = 1 - (horizontalError - LockDistance) / (SlideDistance - LockDistance)`，`slidingSole = originalSole + lockedHorizontalOffset * slideT + up * plantHeight`。
- `horizontalError > SlideDistance`：`Unlocked`。
  进入释放后先保留上一提交 Goal 的相对修正，位置权重在正式 `UnlockBlendSeconds` 内连续降到零；释放完成才回到当帧原生动画，不继续钉住旧落点。

Locked 与 Sliding 的 Ankle 目标都使用：

```text
targetAnkle = targetSole + (originalAnkle - originalSole)
```

因此锁脚只平移同一帧原生踝骨，不改变 Sole-to-Ankle 绑定长度。

每只脚的锁入准备状态包含 `LockLandingEventIdentity`、`LockPreparationStartTimeToLandingSeconds` 与 `LockPreparationWeight`。第一次接受该事件的 `NextSwingLanding` 时冻结起始剩余时间；同事件后续帧只由权威 Step 的剩余时间推进：

```text
start = LockPreparationStartTimeToLandingSeconds
candidate = start <= epsilon
          ? 1
          : saturate(1 - TimeToLandingSeconds / start)
LockPreparationWeight = max(committedWeightForSameEvent, candidate)
```

准备权重只在 Seal 后单调增加，Discard 不推进；事件变化时重新开始。Swing 时它只准备交接，不产生第二个 Goal。事件完成并把 Accepted `NextSwingLanding` 晋级后，`TimeToLandingSeconds = 0` 使准备权重为 1。Locked/Sliding 的 Position Weight 为 `animation.foot-placement-weight * LockPreparationWeight`，因此正常完成后等于完整动画权重，平地零修正也不丢所有权。不新增 Lock Duration、Lock Curve 或按表现 delta 独立累计的锁入计时。

从上一 Committed Locked/Sliding 因水平误差超过 `SlideDistance` 进入 Unlocked 时，必须从上一提交 Goal 连续释放，不能一帧把目标切成 Original：

```text
进入 Unlocked 的首帧：
  UnlockStartCorrection = previousCommittedTargetAnkle - currentOriginalAnkle
  UnlockStartPositionWeight = previousCommittedPositionWeight
  UnlockBlendRemainingSeconds = UnlockBlendSeconds
  unlockedTargetAnkle = currentOriginalAnkle + UnlockStartCorrection
  unlockedPositionWeight = UnlockStartPositionWeight

后续帧：
  pendingRemaining = max(0, committedRemaining - deltaSeconds)
  releaseAlpha = pendingRemaining / UnlockBlendSeconds
  unlockedTargetAnkle = currentOriginalAnkle + committedUnlockStartCorrection
  unlockedPositionWeight = committedUnlockStartPositionWeight * releaseAlpha
```

`pendingRemaining` 只有 Seal 后才成为 Committed；Discard 不消耗时间。归零帧目标改为当帧 Original、权重为零并清除释放状态。重新满足 Locked/Sliding 合同时终止释放并重新消费当前 `LastLanding`。空中、Body 未 Grounded、Step 无效或有限 Action 占脚属于所有权失效，必须立即发布原生事实和零权重，不用 Unlocked 把旧世界锚继续带进动作或空中。

## 9. 支撑脚朝向

只对 Locked/Sliding 支撑脚构造旋转，摆动脚 Rotation Weight 始终为零。移动步伐使用 revision 后的 stride forward；`GroundedStationary` 没有步伐时使用同一 Pending revision 的 `RevisionForward`。两者都不可用、投影到落点法线切平面后退化或法线无效时，本帧 Rotation Weight 为零，不猜方向。

```text
slopeForward = normalize(ProjectOnPlane(orientationForward, landingNormal))
levelUp = componentUp
slopeUp = landingNormal
ascending = dot(slopeForward, componentUp) > epsilon
descending = dot(slopeForward, componentUp) < -epsilon
targetUp = ascending ? normalize(lerp(slopeUp, levelUp, UphillLevelBlend))
         : descending ? normalize(lerp(levelUp, slopeUp, DownhillSlopeBlend))
         : slopeUp
targetRot = LookRotation(slopeForward, targetUp)
```

目标旋转转换到 Component 空间后，按 `MaximumPitchDegrees`、`MaximumRollDegrees` 截断再重建。上坡更趋水平，下坡更趋法线。`CharacterPresentationFactFrame.HorizontalSpeed >= OrientationRunSpeed` 时关闭左右支撑脚朝向，旋转权重为零。`UphillLevelBlend`、`DownhillSlopeBlend`、角限和速度阈值都是正式 Profile 值。

## 10. 同一帧最终顺序

1. 把 Landing、Ground Path、revision、锁脚、释放和骨盆弹簧的 Committed 状态复制到 Pending。
2. 从同帧 Pose Contributions 解析左右 Action 占用，从 Fact Frame 读取 Grounded 与 HorizontalSpeed。
3. 先把已完成 Current Event 的最后 Accepted `NextSwingLanding` 原值晋级为 `LastLanding`，不为晋级查询。
4. 每脚在 Current / Incoming header 中选择零个或一个查询事件。
5. 用 LastLanding 与锁脚状态延续或重选稳定 Pivot 主支撑，按当前 Visible Pose 绕支撑脚建立虚拟 Body Position/Rotation，计算本帧唯一 revision Pose、identity 和 residual yaw。
6. 每脚只为选中的事件执行至多一次 revision Raw Landing SphereCast，按距离死区更新唯一当前 Accepted `NextSwingLanding`；查询失败时使当前 Path 失效。
7. 用 Accepted revision 落点建立或复用 Ground Path、Reachability、Hull 和 Envelope。
8. 正式判定摆动、支撑、GroundedStationary 与当前步伐；计算 Swing 增量、plant、锁入/释放、朝向和主辅支撑结果。
9. 用同一 revision 后步伐端点和同帧双脚结果计算骨盆重基、闭式弹簧和净空下限。
10. 每脚把原始 Foot Goal 转成相对同帧原生动画踝骨的修正和权重，通过唯一 Goal 换代状态得到最终 Foot Goal；Pelvis 继续使用自己的临界弹簧输出。
11. 写 Pelvis、LeftFoot、RightFoot 到同一 GoalSet，执行一次 FullBodyIK 和一次 final writer。
12. 外层 Seal 或 Discard；只有 Seal 才提交全部 Pending 状态和查询结果。

Position Weight 规则必须分开：Swing 只有垂直增量超过容差时非零；Pelvis 只有最终平移超过容差时非零；有效 Locked/Sliding 支撑脚始终使用同帧动画位置权重；Unlocked 按解锁时间递减。Rotation Weight 只在朝向合同成立时非零。

## 11. 最终 Foot Goal 换代

Landing、Ground Path 与 Envelope 不承担防抖职责。每只脚在唯一 GoalSet 写入前拥有一份独立 Pending/Committed 换代状态：

```text
OutputPositionCorrection
OutputRotationCorrection
OutputPositionWeight
OutputRotationWeight
SourceGroundPathIdentity
```

输入目标先转成相对同帧原生动画踝骨的 Component 空间修正。当前 Path 或目标变化时，不保存旧 Path，也不固定旧世界 Goal；本帧只从上一成功输出向最新修正和权重收敛：

```text
alpha = 1 - pow(0.5, deltaSeconds / GoalTransitionHalfLifeSeconds)
outputCorrection = lerp(committedOutputCorrection, currentCorrection, alpha)
outputWeight = lerp(committedOutputWeight, currentWeight, alpha)
finalGoal = currentOriginalAnkle + outputCorrection
```

旋转修正使用同一 `alpha` 做最短路径 Slerp。换代途中 Landing、Path 或 Envelope 再次变化时，紧邻上一成功输出自然成为新起点，不需要额外队列、旧目标页或第二 Path。当前 Path Rejected 时目标是零修正和零权重，最终 Goal 连续回到原生动画。`GoalTransitionHalfLifeSeconds` 是有限正数正式 Profile 值。

离地、有限 Action 占脚、Body discontinuity、Runtime Reset / Retarget / Dispose 属于所有权硬失效：状态当帧清零并发布原生 Goal 和零权重，不能借换代继续携带旧世界地面修正。只有外层 Seal 才提交输出；Discard 不推进半衰期响应。

## 12. 诊断

Scene Gizmo 只显示当前事实：Accepted LastLanding、Cached NextSwingLanding、Ground Envelope、Invalid Segment、Original/Corrected Sole、Planted Sole、步伐线、Pelvis 标记、锁脚颜色和朝向短法线。Gizmo 不重新采样动画、查询世界、计算 Reachability、采样 Envelope 或执行 FBBIK。

CSV 记录 Selected Query Step、每脚查询次数、尝试与 Accepted revision identity、Visible Position/Rotation、virtual Body Position/Rotation、revision Position/Rotation/Forward、visible/applied/residual yaw、主支撑 Side/Event、ActionInstance/脚权重、HorizontalSpeed、SurfaceIdentity、事件身份、锁脚状态、水平误差、锁入准备、释放起点修正/权重/剩余时间、朝向输入、步伐端点与 progress、重基前后 target、necessary、spring input/output/velocity、Pelvis Goal，以及 final writer 后 Physical Pelvis/Ankle、Completion 和 Goal residual。

晋级诊断必须同时记录 `PromotedFromAcceptedRevisionIdentity` 与晋级前最后 Accepted 点、法线、Surface；完成帧查询结果不得出现在这些字段。Pending 与 Committed revision、spring、lock/release state 必须可在同一 Completion 对账，证明 Prepare 从 Committed 开始且 Discard 没有推进。CSV 不能单独算过，必须和 Scene 结果及同一 Completion 对账。

CSV 还必须记录每脚当前原始修正、Committed/Pending输出修正、原始/最终权重、`GoalTransitionHalfLifeSeconds`与Source Ground Path identity，证明线可以立即换踏面而最终骨骼Goal连续。

## 13. 取舍与非目标

- 同事件始终消费当前合法落点，允许真实踏面切换；骨骼连续性由最终 Goal 换代解决，不用旧 Path 冒充稳定世界。
- 骨盆只写 `PelvisPreSolveTranslation`，不使用 Set Mesh、VisualRoot、Gameplay Body 或 KCC 高度。
- 转向重新查询唯一 revision，成本高于旋转旧 Path，但不会伪造 Surface、Hull 与 Envelope。
- Ground Envelope 是唯一 feet-only 下界；不增加第二脚下 Trace、第二 Grounding、第二 IK 或第二 writer。
- 不做 Virtual Ground、实体绕脚转向、攻击/跳跃传统 IK 旁路、专用上下楼动画、跑步特化或自动缩短步距。
