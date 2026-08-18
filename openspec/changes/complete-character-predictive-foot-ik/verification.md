# 完整预测 IK 分步验收

这份文档是实现完成后的操作顺序。每一步都先看 Scene，再对账 CSV；当前步骤不过，不进入下一步。Unity 端只刷新工程、等待脚本重新加载并从 Launcher 面板启动采样，不运行 batchmode，不把编译结果当成验收结果。

## 0. 共同前置条件

1. 使用 Gameplay Lab 中已有的平地、上行楼梯、下行楼梯、坡面和原地转向区域。
2. 保持正式 Corin Foot Placement Profile；不要临时打开第二个 Grounding、传统 IK、Set Mesh 或 VisualRoot 修正。
3. 在 Launcher 面板启动 Foot Landing Sampling，记录本次 CSV 路径、角色 Rig identity、Profile identity、采样开始和结束时间。
4. 先确认 Console 没有 `AnimationPresentationDiagnosticsInterest`、目标缺失、Goal ABI 或 PhysicsScene 错误；有错误时本轮全部作废。
5. 每一个通过条件都要同时满足：Scene 中的人眼结果、Foot Placement Goal、唯一 FullBodyIK、Final Writer 和 Physical Bone 属于相同 Frame、Completion、Rig lineage。

当前已有的采样只能证明摆动脚链：`FinalGoalPositionWeight > 0`、`FinalIkSucceeded = 1`、`FinalIkFailure = None`、最终物理踝骨 residual 接近零、`GroundPathHasInvalidSegment = 0`。它不能证明本 change 后续四类目标已经接入。

## 第 1 步：同一次迈步，下一落点保持在同一级

状态：未开始

### 操作

沿普通楼梯连续走过至少三次上行和三次下行迈步，分别观察左右脚的上一落点球、下一落点球、落点法线和包络线。对同一个 `LandingEventIdentity` 找出连续多帧记录。这里“同一级”必须同时满足 `SurfaceIdentity` 相同和高度差不超过 `MaximumSameEventVerticalJump`。

### Scene 观察

- 下一落点球在 PreSwing 到落地前可以在同一踏面内跟随身体移动。
- 该脚落地后，最后一个 Accepted 下一落点晋级为 LastLanding，另一脚建立新的事件。
- 命中另一级踏面时，旧踏面球保留，不能出现球瞬间跳层或整条包络线抽到另一层。
- 同一踏面内超过 `LandingUpdateDistance` 时包络可以重建；小于死区时路径保持但下一帧仍继续预测。

### CSV 对账

- `LandingEventIdentity` 连续帧不变时，`GroundPathNextSwingLandingSurfaceIdentity` 不得在 Accepted 记录中换值。
- 高度差 `abs(dot(NewAccepted - CachedAccepted, ComponentUp))` 不得超过 `MaximumSameEventVerticalJump`。
- 换级帧必须有 typed rejection/Warning，且不得把被拒命中写入 `GroundPathNextSwingLandingX/Y/Z`。
- Event 完成后，`GroundPathLastLandingEventIdentity` 才能变成刚完成的事件 identity。
- `GroundPathQueryExecuted` 每个有效表现帧最多为一次；路径重建不能带来第二次 SphereCast。

### 通过 / 不通过

- 通过：一次迈步从开始到落地始终只属于一个踏面，换级只发生在新事件。
- 不通过：事件中换级、路径被冻结、落点停止实时更新、或同一帧出现第二次落点查询。

结果：

- CSV：
- 结论：

## 第 2 步：人跟着当前步伐站起来

状态：未开始

### 操作

在第 1 步通过的楼梯路线上慢走，先只观察骨盆和步伐线，不根据脚是否已经贴面判断本步是否通过。

### Scene 观察

- 绿色步伐线端点始终是支撑脚 LastLanding 和摆动脚 NextSwingLanding。
- 上楼时角色随当前步伐逐渐站高，下楼时在支撑仍接触期间随步伐下降。
- 平地时骨盆不因为目标重算而上下抖动；支撑脚切换只发生一次。
- 角色骨盆位置变化来自 Pose 链，不是胶囊、KCC、VisualRoot 或 Mesh Transform 被改写。

### CSV 对账

- `StrideState = Accepted` 时，`StrideSupportSide`、`StrideSwingSide`、`StrideStart*`、`StrideEnd*` 和 `StrideProgress` 同一 Completion 内一致。
- `StrideSlope` 与起止点沿 `GroundPathComponentUp` 的符号一致。
- `StrideRawPelvisDelta*`、`StrideSpringTarget`、`StrideSpringOutput`、`StridePelvisDelta*` 有明确的总目标、重基后的必要位移、弹簧输出和最终 Goal 分解；支撑切换前后必须记录旧/新 stride start，且不得把 `springDelta` 再次加到 `FinalPelvisGoal*`。
- `PelvisPositionWeight > 0` 时，`FinalPelvisGoal*`、`FinalPhysicalPelvisComponentPosition*` 和 `FinalPhysicalPelvisGoalResidual` 必须来自同一 Completion。
- 没有完整步伐或 Path rejected 的帧，`PelvisPositionWeight = 0`，不得沿用上一帧骨盆 Goal。

### 通过 / 不通过

- 通过：上楼看得出人站起来，平地不乱晃，骨盆物理残差能对上同一 Goal。
- 不通过：人仍蹲在胶囊上、每步跳一下、或骨盆只有 Gizmo 结果没有 Physical Bone 写入证据。

结果：

- CSV：
- 结论：

## 第 3 步：Ground Envelope 越过踢面

状态：未开始

### 操作

继续使用第 2 步的慢走楼梯路线，观察摆动脚原生 Sole、修正 Sole、Ground Envelope 和台阶踢面之间的相对位置。

### Scene 观察

- 包络线从支撑点到下一落点形成连续上侧折线，越过台阶踢面。
- 摆动脚的水平进度仍来自原生动画，不会因为 IK 把脚水平吸到落点。
- 被拒绝的换级命中不会把包络线拖到另一层。

### CSV 对账

- `GroundPathState = Accepted` 且无 Invalid Segment 时，`GroundEnvelopeVertexCount` 大于零并对应当前 Path identity。
- `GroundPathHasInvalidSegment = 0`；若为 1，本步必须停止，不能用旧 Envelope 继续验收。
- `FootMotionOriginalSole*` 和 `FootMotionCorrectedSole*` 的水平分量保持动画事实；只有 `max(0, EnvelopeSample - BaselineSample)` 产生垂直增量。
- 不允许出现第二个地面查询字段、第二条 Grounding 链或按脚下实时 Trace 覆盖 Envelope。

### 通过 / 不通过

- 通过：线和实际脚路径都从踢面上方通过，且包络没有因为换级被抽走。
- 不通过：线穿台阶、脚的水平轨迹被落点直接替换、或 Ground Path 失败后仍沿用旧包络。

结果：

- CSV：
- 结论：

## 第 4 步：摆动脚贴路径，支撑脚只向上接地

状态：未开始

### 操作

在慢走楼梯和低矮坡面上重复第 3 步路线，同时观察支撑脚在落地前后和摆动脚越过踢面时的踝骨。

### Scene 观察

- 摆动脚只在 Envelope 高于动画基线时抬高，垂直增量为零时回到原生事实。
- 支撑脚 Sole 只在低于 LastLanding 时向 Component Up 抬高，不会被 IK 向下拽。
- 支撑脚的 Ankle 与 Sole 保持同一帧原生 Sole-to-Ankle 偏移，不出现踝骨被单独拉走。
- 骨盆净空不低于同帧原生动画净空，脚贴面后不会把人重新压回去。

### CSV 对账

- `FootMotionCorrectedSoleY - FootMotionOriginalSoleY` 的 Component Up 投影不得为负的 plant 增量。
- 支撑脚需要接地时 `plantHeight = max(0, dot(LastLanding - OriginalSole, ComponentUp))`；已在落点上方时为零。
- 摆动脚 `FootMotionPositionWeight` 只能由正式动画脚位置权重提供，不再乘 Swing phase 或预测误差。
- `FinalPhysicalAnkleGoalResidual` 和 `FinalPhysicalPelvisGoalResidual` 必须在同一 Completion 归零到 Profile 几何容差内。

### 通过 / 不通过

- 通过：摆动脚越过踢面，站脚贴在踏面并且从不向下抽，人保持第 2 步的高度。
- 不通过：只改了线没有改骨骼、站脚下沉、踝骨偏移不再对应原生 Sole-to-Ankle，或出现落地硬切抖动。

结果：

- CSV：
- 结论：

## 第 5 步：支撑脚 Locked / Sliding / Unlocked

状态：未开始

### 操作

分别测试慢走后停住、轻微横向移动、快速改变方向、跳跃起步和有限攻击动作。必须能让同一支撑脚覆盖三种水平误差区间。

### Scene 观察

- `Locked`：站脚保持在 LastLanding 附近，水平不跟空中脚的 Envelope 跑，旋转仍有自由度；平地 plantHeight 为零时站脚仍保持锁定，不回到原生动画所有权。
- `Sliding`：误差不大时脚向原生动画方向平滑收回，不在旧点和动画之间来回跳。
- `Unlocked`：误差超过 SlideDistance 时脚回到原生动画，腿不会被过远旧落点钉成直线。
- Idle 两脚都有 LastLanding 时，两脚都能通过同一支撑合同站稳。
- 跳跃、空中和有限 Action 占用时脚不继续钉在地面。

### CSV 对账

- 每只脚必须记录 `SupportLockState`、`SupportHorizontalError`、`LockLandingEventIdentity`、`UnlockBlendRemainingSeconds` 和位置权重。
- 水平误差不超过 `LockDistance` 只能对应 Locked；大于 `LockDistance` 且不超过 `SlideDistance` 只能对应 Sliding；超过 `SlideDistance` 只能对应 Unlocked。Locked/Sliding 即使 plantHeight 和水平误差为零，位置权重也必须等于同帧动画位置权重。
- Locked/Sliding 帧不得产生新的 Envelope 采样或 NextSwingLanding 追踪。
- Unlocked 的位置权重在 `UnlockBlendSeconds` 内递减到零；Discard 帧不消耗剩余时间。
- 进入 Locked 的锁入权重只能由该事件 `TimeToLandingSeconds` 推导，不得出现第二条 lock curve。

### 通过 / 不通过

- 通过：站住不搓，误差过大能放开，动作占用和空中不被旧落点拉住。
- 不通过：三个状态没有互斥边界、锁脚继续追预测点、或解锁后仍有非零旧落点权重。

结果：

- CSV：
- 结论：

## 第 6 步：支撑脚有限贴坡，跑步关闭朝向

状态：未开始

### 操作

在同一坡面上先慢走，再逐渐提高速度到正式 `OrientationRunSpeed` 以上；分别测试上坡和下坡。

### Scene 观察

- 慢走上坡时支撑脚有有限贴面，但脚掌不会完全躺平到法线。
- 慢走下坡时脚掌比上坡更接近落点法线，但受 Pitch/Roll 角限约束。
- 跑步达到阈值后脚掌朝向回到原生动画，不再追坡面。
- 摆动脚不因为包络增量获得旋转 Goal。

### CSV 对账

- `SupportRotationWeight` 只有 Locked/Sliding 且前进方向、法线有效时非零。
- `SupportPitchDegrees`、`SupportRollDegrees` 的绝对值不得超过 Profile 角限。
- 上坡目标的 Up 向量更接近 Component Up，下坡目标更接近 Landing Normal；不能两种坡向使用同一固定法线。
- `OrientationRunSpeed` 达到后左右脚 Rotation Weight 均为零，且最终物理踝骨旋转不再向法线收敛。

### 通过 / 不通过

- 通过：走坡看得到有限贴合，跑步关闭，脚掌不出现完全钉死。
- 不通过：永远贴法线、走坡完全不转、跑步仍强行追坡，或旋转写入摆动脚。

结果：

- CSV：
- 结论：

## 第 7 步：转向时摆动脚绕支撑脚走

状态：未开始

### 操作

让角色一只脚 Locked，在原地小幅转向、连续转向和掉帧后恢复；观察支撑落点、摆动落点球、包络和实体朝向。转向后要确认系统产生了新的 trajectory revision，而不是把上一条线整体旋转。

### Scene 观察

- 支撑脚位置继续留在 LastLanding 合同内。
- 摆动脚 NextSwingLanding 和 Envelope 必须来自以支撑 LastLanding 为 Pivot 输入的新 trajectory revision；不能把旧 Path、Surface 或 Envelope 事后旋转后当成新目标。
- 实体 Origin 和 KCC 仍绕自身旋转；Foot Placement 不把胶囊或 VisualRoot 绕支撑脚转。
- 可见 yaw 单帧过大时受 `MaximumPivotYawDeltaDegrees` 限制，后续帧继续消化，不出现一次跨越多圈。

### CSV 对账

- `VisibleYawDelta`、`PivotYawDelta`、`PivotSupportLanding*`、`TrajectoryRevision` 和 `PivotApplied` 来自同一 Frame/Completion。
- `PivotYawDelta` 的绝对值不得超过 Profile 上限；超出的部分必须保留为下一帧可见误差，而不是改写 Body。
- Pivot 后的摆动目标必须对应新 revision 的 Ground Path/Envelope identity；支撑脚 Goal 不得被 Pivot 改成摆动目标，旧 revision 只能作为上一完成输出交接。
- `VisualRoot`、KCC 和 Gameplay Body 的写入计数保持为零；唯一变化应出现在同一 Foot Placement Goal Set 和最终物理骨骼链。

### 通过 / 不通过

- 通过：站脚留在原处，摆动点绕它走，转向不破坏落点和包络。
- 不通过：两脚被原点一起甩、站脚被拧离踏面、Pivot 直接转胶囊、旧 Path 被刚体旋转冒充新事实，或转向产生第二套 IK/写骨链。

结果：

- CSV：
- 结论：

## 完成判定

七步全部通过后，才可以说普通走路、楼梯、坡面、站住转向和站住不搓的预测 IK 基础链完成。完成仍不包含 Virtual Ground、实体绕脚转、脚下第二 Trace、攻击/跳跃另做传统 IK、专用上下楼动画或跑步上下楼动画特化。

七步中任何一步失败，都只能记录为对应模块未完成；不能用编译成功、Goal 非零、CSV 残差为零、落点线正确或身体看起来抬高替代下一步证据。
