# 完整预测 IK 分步验收

这份文档是实现完成后的操作顺序。每一步都先看 Scene，再对账 CSV；当前步骤不过，不进入下一步。Unity 端只刷新工程、等待脚本重新加载并从 Launcher 面板启动采样，不运行 batchmode，不把编译结果当成验收结果。

## 0. 共同前置条件

1. 使用 Gameplay Lab 中已有的平地、上行楼梯、下行楼梯、坡面和原地转向区域。
2. 保持正式 Corin Foot Placement Profile；不要临时打开第二个 Grounding、传统 IK、Set Mesh 或 VisualRoot 修正。
3. 在 Launcher 面板启动 Foot Landing Sampling，记录本次 CSV 路径、角色 Rig identity、Profile identity、采样开始和结束时间。
4. 先确认 Console 没有 `AnimationPresentationDiagnosticsInterest`、目标缺失、Goal ABI 或 PhysicsScene 错误；有错误时本轮全部作废。
5. 每一个通过条件都要同时满足：Scene 中的人眼结果、Foot Placement Goal、唯一 FullBodyIK、Final Writer 和 Physical Bone 属于相同 Frame、Completion、Rig lineage。
6. CSV 必须能看到同帧 `CharacterPresentationFactFrame.Grounded/HorizontalSpeed`，以及从 Pose Contribution 解析的左右 `ActionInstanceId/FootWeight`；缺失时动作占用和跑步朝向结论无效。

当前验收入口已注册为 Unity MCP 工具 `character.foot_landing_stair_ad`，只通过正式 Launcher/Gameplay Lab 路线驱动，不使用 compute-use。运行时代码已经接入实时 Landing/Path 合同、每脚唯一最终 Goal 换代和对应 CSV 诊断。直线楼梯自动采样已经完成一次，但报告仍失败；Ground Envelope、Swing Foot Motion、Goal 换代后的物理踝骨、Pelvis、支撑锁脚、脚掌朝向、Pivot、FullBodyIK 消费和最终 Physical Bone 写入均未通过，不能从 Goal 字段或画面叠加推断完成。

生命周期重构后的第一项用户验收改为：使用 Launcher 采样同一条路线，对照 CSV 与 Scene，确认同一事件的落点、Ground Path 与 Envelope始终来自本帧当前有效预测；跨Surface或高度变化时立即换到新踏面，查询失败时当前Path消失而不是冻结旧线。最后有效落点只在事件完成时晋级为LastLanding。

## 第 1 步：同一次迈步，落点与Path实时跟随当前有效预测

状态：待按实时Path与Goal换代新合同重新确认

### 操作

沿普通楼梯连续走过至少三次上行和三次下行迈步，分别观察左右脚的上一落点球、下一落点球、落点法线和包络线。对同一个 `LandingEventIdentity` 找出连续多帧记录，重点覆盖预测点从一级踏面切到下一级踏面的时刻。

### Scene 观察

- 下一落点球在 PreSwing 到落地前持续跟随本帧有效预测，同一事件可以从当前踏面切到下一踏面。
- 该脚落地后，最后一个 Accepted 下一落点晋级为 LastLanding，另一脚建立新的事件。
- 命中另一级踏面且移动超过 `LandingUpdateDistance` 时，落点、Path端点和Envelope同帧切到新事实，不能继续留在旧踏面。
- 查询失败或没有合法候选时当前落点、Accepted Path和Envelope不显示；下一帧查询恢复后再发布新事实。
- 小于`LandingUpdateDistance`时路径可以复用，但下一帧仍继续预测。

### CSV 对账

- `LandingEventIdentity` 连续帧不变时，`GroundPathNextSwingLandingSurfaceIdentity`与高度允许随本帧合法命中变化。
- 新Accepted点超过死区时，Landing点、`GroundPathNextSwingLanding*`、Path identity和Envelope端点必须来自同一Completion。
- 当前查询Rejected或没有Selected Event时，Accepted Ground Path数量必须为0，Envelope顶点必须为空，不能继续发布上一次Accepted Path。
- `SelectedQueryStepSource` 与 `SelectedQueryLandingEventIdentity` 每脚每帧最多只有一组；Current/Incoming同时合法时必须选择较小TimeToLanding，相等时选择Current。
- `LandingQueryCount` 每脚每帧只能为0或1；路径重建和Current/Incoming竞争都不能带来第二次SphereCast。
- Event完成后，`GroundPathLastLandingEventIdentity`才变成刚完成的identity；`PromotedFromAcceptedRevisionIdentity`、点、法线和Surface必须逐值等于完成前最后Accepted NextSwingLanding。
- 完成帧查询结果不得出现在晋级字段；晋级后若Incoming合法，可作为该帧唯一新查询事件。
- 更新死区内`AttemptedTrajectoryRevisionIdentity`可变化，但Accepted Landing与Ground Path的revision identity必须保持旧值。

### 通过 / 不通过

- 通过：落点、Path、Envelope实时反映当前合法预测，线可以在同一事件内换踏面；骨骼连续性留到第4步验收。
- 不通过：Surface/高度变化后仍冻结旧踏面、无当前有效查询仍显示旧Accepted Path、落点停止实时更新、或同一帧出现第二次落点查询。

结果：

- CSV：`Temp/FootLandingSamples/foot-landing-20260819-062514-942-0383dd1d990f402f87146fa0c9f14494.csv`
- Scene：`Assets/Screenshots/foot-ik-visual-validation-regression-game.png`、`Assets/Screenshots/foot-ik-visual-validation-regression-scene.png`
- 结论：该旧记录采用“换级保留旧落点”口径，不能作为新实时Path合同的通过证据，文件保留用于回归对比。

## 第 2 步：人跟着当前步伐站起来

状态：未确认

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
- `CommittedSpring*` 与`PendingSpringInput*`证明Pending从Committed复制；支撑切换前后必须记录旧/新stride start、重基量、重基后的raw target/output、necessary、closed-form spring output/velocity。
- `StrideRawPelvisDelta*`、`StrideSpringTarget`、`StrideSpringOutput`、`StridePelvisDelta*` 有唯一总目标与最终 Goal 分解；`springDelta` 不得再次加到 `FinalPelvisGoal*`。
- 以CSV中的frequency、deltaSeconds、spring input/velocity重算design闭式公式，必须在浮点容差内等于Pending spring output/velocity；不得出现显式Euler积分或可调damping ratio字段。
- `PelvisPositionWeight > 0` 时，`FinalPelvisGoal*`、`FinalPhysicalPelvisComponentPosition*` 和 `FinalPhysicalPelvisGoalResidual` 必须来自同一 Completion。
- 没有完整步伐或 Path rejected 的帧，`PelvisPositionWeight = 0`，不得沿用上一帧骨盆 Goal。

### 通过 / 不通过

- 通过：上楼看得出人站起来，平地不乱晃，骨盆物理残差能对上同一 Goal。
- 不通过：人仍蹲在胶囊上、每步跳一下、或骨盆只有 Gizmo 结果没有 Physical Bone 写入证据。

结果：

- CSV：`Temp/FootLandingSamples/foot-landing-20260819-062514-942-0383dd1d990f402f87146fa0c9f14494.csv`
- 对账：同一 `CompletionIdentity` 上 `PelvisPositionWeight > 0` 的 105 个帧均有 `FinalIkPelvisAvailable`；`FinalIkInputCompletionIdentity`、`FinalIkOutputCompletionIdentity` 与 `FinalPhysicalWriteCompletionIdentity` 一致，`FinalIkPelvisPositionResidual` 最大为 `0`，`FinalPhysicalPelvisGoalResidual` 最大约 `3.6e-7`。
- Scene：`Assets/Screenshots/foot-ik-runtime-game-mid.png`、`Assets/Screenshots/foot-ik-runtime-scene-mid.png`。
- 结论：旧记录不能作为本轮证据；盆骨、FullBodyIK 和 Physical Writer 尚未验收。
- 最新失败样本：`Temp/FootLandingSamples/foot-landing-20260819-140819-595-bafd5e2d66ad49be9b46c1190172ac65.csv` 共205帧，`maxPelvisStep=1.696/1.717`、`pelvisCuts=37/28`，证明未验收Pelvis输出已经进入FBBIK并造成米级跳变。本轮已把当前阶段恢复为Swing-only输出；修正结果必须重新采样，旧CSV不能作为通过证据。

## 第 3 步：Ground Envelope 越过踢面

状态：未开始

### 操作

继续使用第 2 步的慢走楼梯路线，观察摆动脚原生 Sole、修正 Sole、Ground Envelope 和台阶踢面之间的相对位置。

### Scene 观察

- 包络线从支撑点到下一落点形成连续上侧折线，越过台阶踢面。
- 摆动脚的水平进度仍来自原生动画，不会因为 IK 把脚水平吸到落点。
- 当前预测换踏面时包络线同帧重建到新端点；当前查询失败时Envelope为空，不沿用旧线。

### CSV 对账

- `GroundPathState = Accepted` 且无 Invalid Segment 时，`GroundEnvelopeVertexCount` 大于零并对应当前 Path identity。
- `GroundPathHasInvalidSegment = 0`；若为 1，本步必须停止，不能用旧 Envelope 继续验收。
- `FootMotionOriginalSole*` 和 `FootMotionCorrectedSole*` 的水平分量保持动画事实；只有 `max(0, EnvelopeSample - BaselineSample)` 产生垂直增量。
- 不允许出现第二个地面查询字段、第二条 Grounding 链或按脚下实时 Trace 覆盖 Envelope。

### 通过 / 不通过

- 通过：线和实际脚路径都从踢面上方通过，且包络随当前有效落点实时重建。
- 不通过：线穿台阶、脚的水平轨迹被落点直接替换、或 Ground Path 失败后仍沿用旧包络。

结果：

- CSV：
- 结论：

## 方案二直线楼梯自动采样记录

- CSV：`Temp/FootLandingSamples/foot-landing-20260819-151949-560-fb14a17fc6f3471aa302b7d283b78b4f.csv`
- 路线：Unity MCP `character.foot_landing_stair_ad(action=start_straight)`，24 秒直线上下楼梯。
- 采样：764 个表现帧，左右脚展开 43234 行，`GroundPathState=Accepted` 43191 行；CSV 已包含 Goal 换代 Committed/Pending 修正、权重、Source Path identity 与 `GoalTransitionHalfLifeSeconds=0.03`。
- 自动验收：`expandedMismatch=0`、`dualGoal=0`、`dualStride=0`、`pelvisCuts=0/0`，但 `missedPromotion=1`、`closureFailures=117`、`maxAnkleResidual=0.06`，报告为 `FAIL`。
- 结论：本次证明采样器和 Goal 换代诊断链可用，且没有新增米级骨盆跳变；最终 Foot Goal 到物理踝骨的闭环仍未通过，方案二不能标记完成。该 CSV 只作为当前失败基线，不是通过证据。

## 第 4 步：摆动脚贴路径，支撑脚只向上接地

状态：未开始

### 操作

在慢走楼梯和低矮坡面上重复第 3 步路线，同时观察支撑脚在落地前后和摆动脚越过踢面时的踝骨。

### Scene 观察

- 摆动脚只在 Envelope 高于动画基线时抬高，垂直增量为零时回到原生事实。
- 落点或Path跨踏面换代时，线可以当帧变化，但最终踝骨Goal从上一提交修正连续收敛到新修正，不出现一帧硬切。
- 支撑脚 Sole 只在低于 LastLanding 时向 Component Up 抬高，不会被 IK 向下拽。
- 支撑脚的 Ankle 与 Sole 保持同一帧原生 Sole-to-Ankle 偏移，不出现踝骨被单独拉走。
- 骨盆净空不低于同帧原生动画净空，脚贴面后不会把人重新压回去。

### CSV 对账

- `FootMotionCorrectedSoleY - FootMotionOriginalSoleY` 的 Component Up 投影不得为负的 plant 增量。
- 支撑脚需要接地时 `plantHeight = max(0, dot(LastLanding - OriginalSole, ComponentUp))`；已在落点上方时为零。
- 摆动脚 `FootMotionPositionWeight` 只能由正式动画脚位置权重提供，不再乘 Swing phase 或预测误差。
- `FinalPhysicalAnkleGoalResidual` 和 `FinalPhysicalPelvisGoalResidual` 必须在同一 Completion 归零到 Profile 几何容差内。
- `RawFootGoalCorrection*`可以随当前Path立即变化；`PendingGoalTransitionCorrection*`必须从上一`CommittedGoalTransitionCorrection*`按`alpha = 1 - pow(0.5, deltaSeconds / GoalTransitionHalfLifeSeconds)`重算，最终Goal等于当前Original Ankle加Pending修正。
- Path Rejected但Body仍Grounded且未被Action占用时，原始目标为零修正/零权重，最终Goal连续收敛回原生；离地或Action占用时必须当帧清零，不执行旧修正淡出。

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
- `Unlocked`：误差超过 SlideDistance 的首帧目标与权重连续等于上一提交 Goal，之后只让冻结的相对修正和权重按正式时长衰减；释放完成才回到当帧原生动画，腿不会被过远旧落点钉成直线。
- Idle 两脚都有 LastLanding 时两脚都能站稳；连续小幅转向时主支撑不左右来回切，另一脚可以滑动或释放。
- 跳跃、空中和有限 Action 占用时脚不继续钉在地面。

### CSV 对账

- 每只脚必须记录 `SupportLockState`、`SupportHorizontalError`、`LockLandingEventIdentity`、`LockPreparationStartTimeToLandingSeconds`、`LockPreparationWeight`、`UnlockStartCorrection*`、`UnlockStartPositionWeight`、`UnlockBlendRemainingSeconds` 和位置权重。
- 水平误差不超过 `LockDistance` 只能对应 Locked；大于 `LockDistance` 且不超过 `SlideDistance` 只能对应 Sliding；超过 `SlideDistance` 只能对应 Unlocked。Locked/Sliding 即使 plantHeight 和水平误差为零，位置权重也必须等于同帧动画位置权重。
- Locked/Sliding 帧不得产生新的 Envelope 采样或 NextSwingLanding 追踪。
- 同一Landing Event的LockPreparationWeight必须等于`max(previousCommitted, 1 - TimeToLanding/StartTimeToLanding)`，只增不减；事件完成时为1，不得出现第二条lock curve或delta累计时钟。
- 进入Unlocked首帧的Target与Position Weight必须逐值等于上一Committed Locked/Sliding Goal；后续权重按`UnlockStartPositionWeight * Remaining / UnlockBlendSeconds`递减到零。
- Unlocked释放期间目标只携带冻结的`UnlockStartCorrection`，不得重新追LastLanding；Discard帧不消耗Remaining。
- `PivotSupportSide/Event`在旧主脚仍Locked时保持不变；旧主脚失效后才按较小水平误差与稳定Side顺序重选。
- Fact未Grounded或Action Foot Weight大于容差时，该脚必须当帧原生目标和零权重，不能继续Unlocked释放旧世界锚。

### 通过 / 不通过

- 通过：站住不搓，误差过大能放开，动作占用和空中不被旧落点拉住。
- 不通过：三个状态没有互斥边界、锁脚继续追预测点、或解锁后仍有非零旧落点权重。

结果：

- CSV：
- 结论：

## 第 6 步：支撑脚有限贴坡，跑步关闭朝向

状态：未开始

### 操作

在同一坡面上先慢走，再站住，最后逐渐提高速度到正式 `OrientationRunSpeed` 以上；分别测试上坡和下坡。

### Scene 观察

- 慢走上坡时支撑脚有有限贴面，但脚掌不会完全躺平到法线。
- 慢走下坡时脚掌比上坡更接近落点法线，但受 Pitch/Roll 角限约束。
- 坡面站住且没有步伐时，脚掌仍可使用Committed revision forward保持有限贴坡，不因Stride为空突然回正。
- 跑步达到阈值后脚掌朝向回到原生动画，不再追坡面。
- 摆动脚不因为包络增量获得旋转 Goal。

### CSV 对账

- `SupportRotationWeight` 只有Locked/Sliding且正式前进方向、法线有效时非零；移动时`OrientationForwardSource=Stride`，GroundedStationary时只能为同一Pending `TrajectoryRevision`，不得从旧Stride或Transform差分补前向。
- `SupportPitchDegrees`、`SupportRollDegrees` 的绝对值不得超过 Profile 角限。
- 上坡目标的 Up 向量更接近 Component Up，下坡目标更接近 Landing Normal；不能两种坡向使用同一固定法线。
- 关闭判定的速度必须逐值等于同帧`CharacterPresentationFactFrame.HorizontalSpeed`；不得来自Step、输入幅值或Transform差分。
- `HorizontalSpeed >= OrientationRunSpeed`后左右脚Rotation Weight均为零，且最终物理踝骨旋转不再向法线收敛。

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
- GroundedStationary两脚都可支撑时，主脚持续锁在同一LastLanding，另一脚根据水平误差滑动或释放；不能每帧换Pivot脚。
- 实体 Origin 和 KCC 仍绕自身旋转；Foot Placement 不把胶囊或 VisualRoot 绕支撑脚转。
- 可见 yaw 单帧过大时受 `MaximumPivotYawDeltaDegrees` 限制，后续帧继续消化，不出现一次跨越多圈。

### CSV 对账

- `CurrentVisiblePosition/Rotation*`、`VisiblePosition/RotationAtCommit*`、`CommittedRevisionPosition/Rotation*`、`VisiblePositionForRevision*`、`VirtualBodyPosition/Rotation*`、`RevisionPosition/Rotation/Forward*`、`VisibleYawDelta`、`RequestedYaw`、`PivotYawDelta`、`ResidualYaw`、`PivotSupportSide/Event/Landing*`、`AttemptedTrajectoryRevisionIdentity`和`PivotApplied`必须来自同一Frame/Completion。
- 先重算`CommittedRevisionPosition + (CurrentVisiblePosition - VisiblePositionAtCommit)`，必须等于`VisiblePositionForRevision`；再重算`LastLanding + RotateAroundUp(VisiblePositionForRevision - LastLanding, PivotYawDelta)`，必须等于Virtual Body与Revision Position。Revision Rotation必须逐值等于`PivotRotation * CommittedRevisionRotation`。
- Raw Landing必须逐值等于`VirtualBodyPosition + FutureBodyTranslationWorld + VirtualBodyRotation * RootLocalLanding`；Future Body Translation不得随Pivot旋转，旧Route、Surface、Hull和Envelope不得被旋转后冒充本帧新查询事实。
- `PivotYawDelta`绝对值不得超过Profile上限；`ResidualYaw = RequestedYaw - PivotYawDelta`并在后续成功Seal帧继续消化，Discard不得推进。
- trajectory revision identity必须独立于Future Body TrajectoryGeneration、Tick、Event与Render Frame；左右脚本帧查询共享同一attempt identity。
- Pivot后的摆动目标必须对应新revision的Ground Path/Envelope identity；支撑脚Goal不得被Pivot改成摆动目标，旧revision只能保留未被本帧替换的Committed事实。
- GroundedStationary旧主支撑仍Locked时`PivotSupportSide/Event`不得变化；辅脚状态变化不得反向切换主支撑。
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
