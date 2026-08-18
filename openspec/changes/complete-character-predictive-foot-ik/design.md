# Design

## 1. 唯一计算链

完整预测 IK 仍然只有一条 Foot Placement 链，不增加第二个 Solver：

```text
动画 Foot Analysis
-> Current / Incoming Step
-> committed Body 速度 + Future Body Translation
-> 可见 yaw 与 trajectory revision
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
- 每个有效表现帧每脚一次正式 Landing SphereCast；没有命中就发布 typed `GroundQueryMissed`，不造默认地面。
- `LastLanding` 和 `NextSwingLanding` 组成 Ground Path 轴；没有 LastLanding 就发布 `CurrentLandingUnavailable`。
- Capsule 只表示路径采集范围，不表示鞋底或最终脚轨迹。
- 所有 Edge 必须通过 `MaximumReachableVerticalEdge`；失败不得删除障碍点后继续构造 Hull。
- Ground Envelope 是 feet-only 的地面下界，不改变脚的水平动画进度，不替代 KCC。
- 摆动脚垂直修正为：`Corrected = Original + up * max(0, EnvelopeSample - BaselineSample)`。
- Pelvis、LeftFoot、RightFoot 三个 Goal slot、`PelvisPreSolveTranslation`、一次 FullBodyIK 和一次 final writer。

`Confidence` 只进诊断，不改变 Goal 权重。空中、无权威 Step、有限 Action 占用或 Ground Path rejected 时，不另做传统 IK；无效 Goal 发布原生事实和零权重。

## 3. 同事件同踏面落点

同一 `LandingEventIdentity` 只有同时满足下面两个条件才属于同一踏面：

```text
SurfaceIdentity 相同
且 abs(dot(newPoint - cachedPoint, ComponentUp))
    <= MaximumSameEventVerticalJump
```

任一条件不成立都算换级。`MaximumSameEventVerticalJump` 是有限正数的正式 Profile 值，Build 时校验，Runtime 不猜楼梯高度。`SurfaceIdentity` 必须来自稳定查询命中，不得由 AuthorityTick、每帧坐标哈希或表现时间伪造。

同一事件、同一踏面且仍处于 PreSwing / Swing 时：

- 每个有效表现帧继续执行唯一正式 SphereCast。
- 新命中与缓存点距离小于 `LandingUpdateDistance` 时，复用 Accepted Landing 和已提交 Ground Path，但下一帧仍继续预测。
- 新命中仍属同一踏面且超过死区时，接受新点，并用这一次 SphereCast 结果重建同一 Foot Placement 事务中的 Ground Path。
- 新命中换级时，丢弃新命中，保留本事件最后 Accepted Landing，并发布 typed rejection 和 Warning；不得把新踏面写入 Path。
- Swing Event 完成时，最后一个 Accepted `NextSwingLanding` 才晋级为 `LastLanding`；新事件才允许新踏面。

这不是冻结实时落点：同一踏面内预测仍可实时滑动，只有换级被拒绝。

## 4. 转向与唯一 trajectory revision

GDC 的 Pivot 目的是减少接触脚因角色转向产生的位移，但项目规则不允许刚体旋转已经提交的旧 Foot Route、命中 Surface 或 Ground Envelope。有效转向必须建立新的 Foot Placement trajectory revision；旧 Plan 只负责上一完成输出的连续性交接。

临时支撑候选只用于判断 Pivot 是否有合法支点，不发布步伐或骨盆 Goal：

```text
previousForward = ProjectOnPlane(previousCommittedVisibleForward, up)
currentForward  = ProjectOnPlane(currentVisibleForward, up)
visibleYawDelta = SignedAngle(previousForward, currentForward, up)
pivotDelta      = Clamp(visibleYawDelta,
                         -MaximumPivotYawDeltaDegrees,
                         +MaximumPivotYawDeltaDegrees)
```

当支撑 LastLanding 有效、前后向量可投影且 `pivotDelta` 非零时，Foot Placement 建立唯一 trajectory revision。该 revision 以支撑 LastLanding 作为转向枢轴输入，重新投影 Future Body、计算 Raw Landing，并执行本表现帧唯一 SphereCast；随后重新执行 Capsule、Edge、Reachability、Hull 和 Ground Envelope。它不能把旧 Path 的点、Surface 或 Envelope 事后旋转后冒充新事实。

`MaximumPivotYawDeltaDegrees` 是防止掉帧时单帧跨越多圈的正式 Profile 限制，不是 fallback；剩余 yaw 误差留给后续表现帧。前向退化、支撑点无效或 revision 查询失败时，本帧不做 Pivot，不猜方向，不改 VisualRoot、KCC、Gameplay Body 或实体胶囊朝向。

## 5. 当前步伐仲裁

支撑脚是权威 Step 非 Swing、拥有有效 `LastLanding` 且未被有限 Action 占用的脚。摆动脚是权威 Step 为 Swing、`NextSwingLanding` 存在且 Event identity 一致的脚。正式步伐起点是支撑 `LastLanding`，终点是 revision 后的摆动 `NextSwingLanding`。

两脚同时满足摆动合同且都有可用垂直包络增量时，只选择增量较大的一脚作为唯一摆动脚。另一脚只有拥有 `LastLanding` 且未被有限 Action 占用时才进入支撑合同，否则保持原生事实和零权重。

两脚都没有 LastLanding、身份不一致、没有唯一摆动脚或端点退化时，本帧没有步伐，Pelvis 权重为零；没有 LastLanding 或被有限 Action 占用的脚支撑锁脚权重为零。若两脚都非 Swing、各自拥有有效 LastLanding 且未被有限 Action 占用，则进入 `GroundedStationary`：不发布步伐骨盆和摆动 Envelope，但两脚都可以复用同一支撑接地与锁脚合同。仍满足摆动包络合同的脚可以单独发布 Swing Goal。

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

弹簧必须先解决相对坐标系问题，再积分：

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
springOutput = 临界阻尼弹簧从 springInput 拉向 springTarget 后的输出
springDelta = springOutput - necessaryDelta
pelvisDelta = up * springOutput
```

`previousStrideStart`、`previousRawPelvisTargetAlongUp`、`previousSpringOutput`、SupportSide 和弹簧速度属于 Foot Placement Pending/Committed 状态。支撑切换时旧相对高度必须按旧起点到新起点重基；不能把旧起点的厘米数直接与新起点的目标相减。如果没有有效旧起点，以当前目标初始化。`springOutput` 是唯一最终输出，`springDelta` 只是诊断分解，不能再加回 Goal。

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
  目标回到原生动画，位置权重在正式 `UnlockBlendSeconds` 内降到零，不继续钉住旧落点。

两种锁定状态的 Ankle 目标都使用：

```text
targetAnkle = targetSole + (originalAnkle - originalSole)
```

因此锁脚只平移同一帧原生踝骨，不改变 Sole-to-Ankle 绑定长度。每只脚记录 `LockLandingEventIdentity`、`LockBlendElapsedSeconds` 和 `UnlockBlendRemainingSeconds`。锁入时基只能来自事件的 `TimeToLandingSeconds`；解锁剩余时间只在成功 Seal 后递减，Discard 不递减。

支撑脚处于有效 Locked 或 Sliding 时，即使 plantHeight 和水平误差为零，Position Weight 仍必须等于 `animation.foot-placement-weight`，否则 FBBIK 不会持续维护锁脚。Unlocked 才按正式解锁时间降权。摆动脚和无效支撑合同不进入三态。

## 9. 支撑脚朝向

只对 Locked/Sliding 支撑脚构造旋转，摆动脚 Rotation Weight 始终为零。若步伐前进方向投影到落点法线切平面后退化，或法线无效，本帧 Rotation Weight 为零，不猜方向。

```text
slopeForward = normalize(ProjectOnPlane(strideForward, landingNormal))
levelUp = componentUp
slopeUp = landingNormal
targetUp = 上坡 ? normalize(lerp(slopeUp, levelUp, UphillLevelBlend))
         : 下坡 ? normalize(lerp(levelUp, slopeUp, DownhillSlopeBlend))
         : slopeUp
targetRot = LookRotation(slopeForward, targetUp)
```

目标旋转转换到 Component 空间后，按 `MaximumPitchDegrees`、`MaximumRollDegrees` 截断再重建。上坡更趋水平，下坡更趋法线。达到 `OrientationRunSpeed` 时关闭支撑脚朝向，旋转权重为零。`UphillLevelBlend`、`DownhillSlopeBlend`、角限和速度阈值都是正式 Profile 值。

## 10. 同一帧最终顺序

1. 读取 Step、LastLanding、committed Body 和可见前向，确定临时支撑候选，不发布 Goal。
2. 计算可见 yaw；有效转向启动唯一 trajectory revision，不旋转旧 Route、Surface 或 Envelope。
3. 左右各执行一次 revision 后的 Landing 预测，按同事件同踏面合同更新 NextSwingLanding。
4. 用 revision 后的 Accepted 落点建立或复用 Ground Path、Reachability、Hull 和 Envelope。
5. 用 revision 后的身份和端点正式判定支撑、摆动和当前步伐。
6. 计算摆动脚增量、支撑脚 plant、Locked/Sliding/Unlocked 和朝向。
7. 用同一 revision 后的步伐端点计算骨盆、必要位移、弹簧和净空下限。
8. 写 Pelvis、LeftFoot、RightFoot 到同一 GoalSet，执行一次 FullBodyIK 和一次 final writer。
9. 外层 Seal 或 Discard；只有 Seal 才提交 Landing、revision、锁脚和骨盆状态。

Position Weight 规则必须分开：Swing 只有垂直增量超过容差时非零；Pelvis 只有最终平移超过容差时非零；有效 Locked/Sliding 支撑脚始终使用同帧动画位置权重；Unlocked 按解锁时间递减。Rotation Weight 只在朝向合同成立时非零。

## 11. 诊断

Scene Gizmo 只显示当前事实：Accepted LastLanding、Cached NextSwingLanding、Ground Envelope、Invalid Segment、Original/Corrected Sole、Planted Sole、步伐线、Pelvis 标记、锁脚颜色和朝向短法线。Gizmo 不重新采样动画、查询世界、计算 Reachability、采样 Envelope 或执行 FBBIK。

CSV 记录 SurfaceIdentity、事件身份、轨迹 revision、锁脚状态、水平误差、锁入/解锁剩余时间、朝向权重、步伐端点与 progress、raw target、necessary、spring、Pelvis Goal，以及 final writer 后 Physical Pelvis/Ankle、Completion 和 Goal residual。CSV 不能单独算过，必须和 Scene 结果及同一 Completion 对账。

## 12. 取舍

### 12.1 同事件同踏面，而不是冻 Path 或每帧换台阶

冻 Path 会让包络失去实时性；每帧换级会让目标乱跳。同事件同踏面允许同一 SurfaceIdentity 且高度差在阈值内的点继续滑动，任一身份或高度条件失败就等新事件换级。

### 12.2 骨盆用 Goal，不用 Set Mesh

Set Mesh 能快速改变人高，但会和 Body/KCC 抢所有权。项目继续只写 `PelvisPreSolveTranslation`，由唯一 Pose 链和 Physical Bone 证据验收。

### 12.3 先稳定落点和轨迹，再锁脚朝向

Ground Path、Reachability 和同事件合同先成立；锁脚、朝向和骨盆消费同一 revision 后的事实，不在旧 Path 上补视觉修正。

### 12.4 转向使用唯一 trajectory revision

直接旋转旧 Path 便宜但会伪造 Surface、Hull 和 Envelope。重新查询成本更高，但能保证转向后的落点、可达性和脚路径属于同一事实链；不转胶囊，不写 Body。

### 12.5 不做第二脚下 Trace和第二 IK

Ground Envelope 是唯一 feet-only 下界。额外 Trace 或传统 IK 会重新引入分裂的 Grounding/Writer 所有权。

## 13. 本 change 不做

- Virtual Ground 和对侧脚接触拆路径。
- 实体绕支撑脚转向。
- 脚下第二 Trace。
- 攻击、跳跃、空中帧的传统 IK 旁路。
- 专用上下楼动画、跑步上下楼特化或步距自动缩短。
- 第二个 Ground Path revision；有效转向只复用 Foot Placement 唯一 trajectory revision。
