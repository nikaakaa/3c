# Design

## 1. 整条链

参考文章和项目已有层对上之后，完整预测 IK 是这一条，不是另一套 Solver：

```text
动画 Foot Analysis
-> Current / Incoming Step（时间、距离、Landing Event、IsSwing）
-> committed Body 速度 + Future Body Translation
-> Raw Landing + 一次 SphereCast
-> LastLanding + NextSwingLanding
-> Capsule Ground Detection
-> 排序 / Edge / Reachability / 上侧 Hull
-> 当前步伐（支撑 LastLanding -> 摆动 NextSwingLanding）
-> 盆骨采样 + 必要位移 + 临界弹簧 + 净空下限
-> 摆动脚：Envelope - Baseline，只加在 Component Up
-> 支撑脚：垂直接地，再 Locked / Sliding / Unlocked
-> 支撑脚有限朝向
-> 转向时摆动路径绕支撑落点
-> 同一 GoalSet
-> PelvisPreSolve 后唯一 FBBIK
-> 唯一 final writer
```

前 7 行的查询和包络已经归档，但同一次迈步里落点仍会换级，所以第 3 节是修复，不是「数据层已经做完」。IK Solver 仍只是最后执行目标的工具。

## 2. 已经存在、本 change 不重做

- 左右脚独立 Step 与自动分析。
- 每帧一次正式 Landing SphereCast。没有命中就 `GroundQueryMissed`，不造默认地面。
- Ground Path 只用两个 Accepted 落点做轴。没有 LastLanding 就 `CurrentLandingUnavailable`。
- Capsule 是路径采集范围，不是鞋底。
- 任一 Edge 超过 `MaximumReachableVerticalEdge` 整条 Path 失败，不删障碍点继续 Hull。
- Ground Envelope 是 feet-only 下界，不改脚的水平进度，不替代 KCC。
- 摆动脚公式：`Corrected = Original + up * max(0, EnvelopeSample - BaselineSample)`。
- 三个 Goal slot、先盆骨后 FBBIK、一次 writer。

GDC 要的 Contact / Delay / Distance / 约束区间，项目里已经落在 Step 上，本 change 不另做分析资产：

- Contact 窗口：`IsPreSwing` / `IsSwing` / 落地后非 Swing。
- Delay：`TimeToLandingSeconds`。
- Distance：`RootLocalLanding` 加 Future Body。
- 约束区间：非 Swing 且拥有 LastLanding 才能进锁脚；不另做 lock 曲线。
- `Confidence` 只进诊断，不得改 Goal 权重。

空中、无权威 Step、有限 Action 占用该脚时，三个 Goal 权重必须为零。不得给跳跃或攻击补一套传统 IK。

## 3. 同一次迈步的落点

问题不是「要不要实时更新」，而是「更新能不能换台阶」。

整段楼梯经常共用一个 `SurfaceIdentity`。只比表面身份不够，换级还要看沿 `Component Up` 的高度跳变。

同一 `LandingEventIdentity` 且仍处于 PreSwing / Swing：

- 每帧继续做那一次正式 SphereCast。
- 新命中与缓存点距离小于 `LandingUpdateDistance`：复用落点和 Path。
- 新命中未换级：接受滑动，超过死区则重建 Path。
- 新命中换级：丢掉这次命中，保留本事件已接受的踏面，并打 Warning。不得把脚送到另一级台阶。
- 事件完成：最后一个 Accepted 落点晋级为 `LastLanding`。新事件才允许新踏面。

换级成立，当且仅当下面任一为真：

- `SurfaceIdentity` 不同。
- `abs(dot(newPoint - cachedPoint, up))` 大于 Profile 显式 `MaximumSameEventVerticalJump`。

`MaximumSameEventVerticalJump` 必须是有限正数，Build 校验，Runtime 不猜楼梯高度。SurfaceIdentity 必须来自查询命中的稳定表面，不得用 AuthorityTick 或每帧坐标哈希冒充。

这和冻死整条包络不是一回事。同一级踏面上的预测漂移仍会带动包络变形；换层才被拒绝。

## 4. 当前步伐

一步属于两条腿。

- 支撑脚：权威 Step 不是 Swing，且拥有 `LastLanding`。
- 摆动脚：权威 Step 是 Swing，且 `NextSwingLanding` 与该 Step 的 Landing Event 一致。
- 起点：支撑脚 `LastLanding`。
- 终点：摆动脚 `NextSwingLanding`。

两脚同时 Swing 时，不直接把两脚都当成摆动脚：比较两脚本帧可用的垂直包络增量，只选择较大者作为本帧唯一摆动脚。另一脚只有在它拥有有效 `LastLanding` 且自身不是被有限 Action 占用时，才进入支撑合同；否则该脚保持原生事实和零权重。两脚都没有 `LastLanding`、身份对不上、或没有唯一摆动脚时，本帧没有步伐，盆骨和支撑锁脚权重必须为零。其中一脚若自身仍满足摆动包络合同，只发那一只脚的包络 Goal。

支撑切换只发生在：原摆动脚落地晋级，或左右权威 Step 身份交换。不得用 Sole 前后位置比较当主条件。

有些动画会在短时间交叉两次。Profile 必须给出有限正数 `StrideSwitchCooldownSeconds`。冷却未结束时不得切换步伐两端，也不得用上一帧盆骨目标硬切到新线。

## 5. 盆骨

GDC 第 9–10、17 页和 Shadow 第三节：髋部跟步伐坡度，不跟两脚平均。

```text
forward = ProjectOnPlane(strideEnd - strideStart, up)
strideProgress = saturate(dot(poseRoot - strideStart, forward) / |forward|)
sampledGround = lerp(strideStart, strideEnd, strideProgress)
uphill = dot(strideEnd - strideStart, up) > 几何容差
downhill = dot(strideEnd - strideStart, up) < -几何容差

上坡且支撑已落地：rawPelvisDelta = up * dot(sampledGround - strideStart, up)
下坡且支撑仍接触、摆动未落地：同样按 progress 下降
平地：rawPelvisDelta = 0
```

`rawPelvisDelta` 是相对步伐起点的地面升高，不是世界绝对高度，也不是 Set Mesh。

弹簧不把“总目标”和“本帧必须跟上的高度”混成一个值。先得到本帧的有符号总目标 `rawPelvisTargetAlongUp`，再按支撑脚是否切换拆分：

```text
rawPelvisTargetAlongUp = dot(rawPelvisDelta, up)
necessaryDelta = 支撑脚未切换时的
                 rawPelvisTargetAlongUp - previousRawPelvisTargetAlongUp
                 支撑脚切换时为 0
springTarget = rawPelvisTargetAlongUp
springInput = previousSpringOutput + necessaryDelta
springOutput = 临界阻尼弹簧从 springInput 拉向 springTarget 后的输出
springDelta = springOutput - necessaryDelta
pelvisDelta = up * springOutput
```

`previousRawPelvisTargetAlongUp`、`previousSpringOutput`、`SupportSide` 和弹簧速度属于 Foot Placement 的 Pending/Committed 状态。`springOutput` 是最终相对步伐起点的单一输出，`springDelta` 只是诊断分解，不能再加回 Goal。必要位移只在同一支撑连续帧中追随目标差值，支撑切换时不把旧步伐的绝对高度硬加到新步伐；这样上台阶不会被弹簧拖成慢爬，也不会因为每帧重新计算相对起点而重复抬高。弹簧只消化支撑切换和同表面落点滑动。频率和阻尼比必须在 Profile 里显式给出，Runtime 不猜。上坡和下坡都允许有符号目标，下坡的下降不能被错误地钳成上升。

双脚垂直目标算完后：

```text
animatedClearance = 原生动画盆骨到更低原生动画脚
correctedClearance = (动画盆骨 + pelvisDelta) 到更低修正脚
若 correctedClearance < animatedClearance：把差值补进 pelvisDelta
```

下限只用同帧动画净空，不用固定腿长。

没有步伐时盆骨权重为零，不得沿用上一帧目标。Discard / Fault 恢复上一提交弹簧。

参考用 Set Mesh 把角色挂到起点。项目已经有 `PelvisPreSolveTranslation`，继续走唯一 Pose 链，不写 VisualRoot，不改 KCC。

## 6. 摆动脚

合同与已归档 Swing Foot Motion 相同。水平进度来自动画 Sole 在 `LastLanding -> NextSwingLanding` 上的投影。最终脚不得低于 Envelope。垂直增量为零则权重为零，FBBIK 跳过无意义 Update。

不得把 NextSwingLanding 直接当成脚踝目标。不得用输入方向重画脚轨迹。不得再乘摆动相位或预测误差。

同一帧两脚都通过摆动合同：只保留垂直增量更大的一只作为摆动脚，另一只改走支撑合同。不得两脚同时用包络拉 FBBIK。

该脚一旦 Locked 或 Sliding，本帧不得再为它采样 Envelope，也不得用新的 NextSwingLanding 追路。站脚只对 LastLanding 负责。

## 7. 支撑脚接地与锁脚

先做垂直：

```text
plantHeight = max(0, dot(LastLanding - originalSole, up))
plantDelta = up * plantHeight
plantedSole = originalSole + plantDelta
plantedAnkle = originalAnkle + plantDelta
```

只准非负垂直增量。已经在落点上方则这一项为零。

再按 GDC 第 13–16 页看水平误差：

```text
horizontalOffset = ProjectOnPlane(originalSole - LastLanding, up)
horizontalError  = |horizontalOffset|
```

- `horizontalError <= LockDistance`：Locked。
  `lockedSole = LastLanding + up * plantHeight`。旋转仍自由。
- `LockDistance < horizontalError <= SlideDistance`：Sliding。
  `slideT = 1 - (horizontalError - LockDistance) / (SlideDistance - LockDistance)`
  `lockedHorizontal = ProjectOnPlane(LastLanding, up)`，`animatedHorizontal = ProjectOnPlane(originalSole, up)`，目标水平为 `lerp(animatedHorizontal, lockedHorizontal, slideT)`，垂直仍使用 `plantHeight`。
- `horizontalError > SlideDistance`：Unlocked。
  位置回到原生动画。位置权重在 Profile 显式 `UnlockBlendSeconds` 内从 `animation.foot-placement-weight` 降到 0。不得继续钉在过远旧落点。

`targetAnkle = targetSole + (originalAnkle - originalSole)`，因此锁脚只改变同一帧原生踝骨的平移，不凭空改变脚踝到鞋底的绑定长度。每只脚的 Pending/Committed 状态记录 `LockLandingEventIdentity`、`LockBlendElapsedSeconds` 和 `UnlockBlendRemainingSeconds`。从摆动进入锁脚时，唯一的锁入时基是该事件的 `TimeToLandingSeconds`：事件开始时记录初始剩余时间，锁入权重为 `saturate(1 - currentTimeToLanding / initialTimeToLanding)`；事件已落地或初始时间无效时直接使用完整的动画位置权重，不新增另一条曲线。进入 Unlocked 时装载 `UnlockBlendSeconds`，每个成功 Seal 的表现帧递减；Discard 不递减。阈值必须在 Profile 里显式给出，且 `LandingUpdateDistance < LockDistance < SlideDistance`，`UnlockBlendSeconds > 0`。没有 LastLanding 或该脚是当前摆动脚时，不进这三态。

Idle 且两脚都有 LastLanding、都不是 Swing：两脚都可以按支撑合同接地。这不是第二套传统 IK，仍是同一 Goal 链。

## 8. 脚掌朝向

只对 Locked / Sliding 的支撑脚写旋转。摆动脚旋转权重保持为零。

用落点法线和步伐前进方向构造目标旋转。若前进方向投影到落点切平面的长度小于几何容差，则该脚本帧不发布旋转 Goal，而不是猜一个方向：

```text
slopeForward = normalize(ProjectOnPlane(strideForward, landingNormal))
levelUp      = componentUp
slopeUp      = landingNormal
uphillBlend  = Profile.UphillLevelBlend   // (0,1]，越大越接近水平
downhillBlend= Profile.DownhillSlopeBlend // (0,1]，越大越接近坡面
targetUp     = 上坡 ? normalize(lerp(slopeUp, levelUp, uphillBlend))
             : 下坡 ? normalize(lerp(levelUp, slopeUp, downhillBlend))
             : slopeUp
targetRot    = LookRotation(slopeForward, targetUp)
```

`targetRot` 先转换到 Component 空间，再取绕 Component Right 的 Pitch 和绕 Component Forward 的 Roll；分别按 Profile 的 `MaximumPitchDegrees`、`MaximumRollDegrees` 截断后重建旋转。上坡的 `UphillLevelBlend` 越大越靠近水平，下坡的 `DownhillSlopeBlend` 越大越靠近法线。跑步关闭：当前权威 Step 水平速度或脚步分析速度达到 Profile 显式 `OrientationRunSpeed` 时，旋转权重为零。法线无效、前进方向退化、脚不在 Locked/Sliding 或当前步伐不成立时，Rotation Weight 为零。

GDC 明确说脚底始终贴合法线不是目标。跑起来完全关闭。`UphillLevelBlend`、`DownhillSlopeBlend`、角限和跑步阈值都必须是 Profile 显式有限值，Runtime 不猜。

## 9. 转向枢轴

GDC 要把旋转中心移到接触脚。项目不能转胶囊，也不能写 VisualRoot。

本 change 只做脚这一层：

```text
previousForward = ProjectOnPlane(previousCommittedVisibleForward, up)
currentForward  = ProjectOnPlane(currentVisibleForward, up)
visibleYawDelta = SignedAngle(previousForward, currentForward, up)
pivotDelta      = Clamp(visibleYawDelta,
                         -MaximumPivotYawDeltaDegrees,
                         +MaximumPivotYawDeltaDegrees)
rotatedLanding  = LastLanding + RotateAroundUp(NextSwingLanding - LastLanding, pivotDelta)
rotatedEnvelope = RotateAroundUp(EnvelopeSample - LastLanding, pivotDelta) + LastLanding
```

`MaximumPivotYawDeltaDegrees` 是防止掉帧时一次跨过多圈的正式 Profile 限制，不是 fallback；超过该值的剩余朝向误差留给下一表现帧。若前后可见前向无法投影为有效水平向量，或步伐、支撑落点不成立，本帧不做 Pivot。摆动脚使用旋转后的落点和 Envelope 采样继续计算增量，支撑脚位置保持锁定。胶囊仍绕自己的 Origin 转。

代价：快速原地转向时，腿仍会吸收胶囊和锁定脚之间的一部分误差。把实体绕脚转属于 Body Presentation，另立项，不在本 change 旁路。

## 10. 同一帧计算顺序

1. 左右各一次 Landing 预测，按第 3 节更新 NextSwingLanding
2. 建或复用 Ground Path / Envelope
3. 判定支撑 / 摆动 / 有没有步伐
4. 摆动脚包络增量
5. 支撑脚垂直 + 锁脚三态 + 朝向
6. 步伐盆骨、弹簧、净空下限
7. 转向时把摆动目标绕支撑落点旋转后写回 Goal
8. 写 Pelvis、LeftFoot、RightFoot
9. 外层 Seal 或 Discard

Position Weight 在对应合同成立且垂直或水平修正超过几何容差时等于 `animation.foot-placement-weight`。Rotation Weight 只在朝向合同成立时非零。

禁止 Set Mesh、第二脚下 Trace、默认地面、沿用失败帧的盆骨、第二 IK。

## 11. 可观察事实

Gizmo 已经有落点球、包络线、原脚 / 修脚、步伐线。本 change 补：

- 下一落点球换表面被拒绝时，原踏面点保留，失败用已有红色线框
- Locked / Sliding / Unlocked 用已有脚标记颜色区分，不写字
- 朝向用短法线，不写角度数字

CSV 增加表面身份、锁脚状态、水平误差、朝向权重、步伐和盆骨既有字段。CSV 不能单独算过。人眼过了再对账。

## 12. 取舍

### 12.1 同事件同表面，而不是冻 Path 或每帧换台阶

冻 Path：包络假稳定，你已经否决。每帧换台阶：目标乱跳，脚看起来像没 IK。同表面滑动是参考里「落地后仍更新终点」和「不要滑到下一级」同时成立的写法。

### 12.2 盆骨用 Goal，不用 Set Mesh

Set Mesh 验收快，但和 KCC 抢身高，破坏唯一 Pose 链。盆骨 Goal 验收必须看人高和步伐线，不能只看 Transform 层级。

### 12.3 先盆骨后锁脚朝向

Shadow 先算盆骨再应用脚。GDC 把锁脚、髋部、朝向、Pivot 放在 Envelope 之后。没有稳定落点和站起来的人，锁脚和朝向不可验收。

### 12.4 转向不转胶囊

转胶囊才是原文枢轴，但会改 Body 所有权。本 change 只转摆动路径，把实体绕脚留给以后的 Body change。

### 12.5 不做第二脚下 Trace

依卞和 Shadow 在 Path 后再向下打一枪。项目的唯一地面下界是 Capsule Hull。再打一枪就是第二 Grounding。边缘仍穿模，另立项，不在本 change 旁路包络。

### 12.6 不做专用楼梯动画和跑步上下楼特化

Shadow 自己写了走路勉强、跑步难受。那是动画和步距问题，不是再加一套 IK。

## 13. 参考里有、本 change 不做

这些不是忘了，是整套预测 IK 里被裁掉的后半段。七步过了也不包含它们。

- Virtual Ground：GDC 第 30 页用对侧接触拆路径过尖峰。现有 Reachability + Hull 先顶着。Hull 过不去尖峰再另立项，不先做空模块。
- 实体绕支撑脚转：GDC 第 21–28 页原文。本 change 只转摆动路径。
- 脚下第二 Trace：Shadow / 依卞在 Path 后再打一枪取更高点。项目只有 Capsule Hull。
- 非循环动作改传统 IK：Shadow 对攻击、跳跃切另一套。项目禁止第二 IK，这些帧三 Goal 权重为零。
- 预测点收到踏面中心、缩短步距、专用上下楼动画、跑步上下楼特化。
- 左右预测点平均当髋部高度：GDC 提过两脚可当身体高度参考，第 17 页又禁止平均。项目只用单步伐线。
- 独立 lock 曲线、独立 Ground Path Revision、Set Mesh。
