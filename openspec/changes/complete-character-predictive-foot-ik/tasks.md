# 实施任务

所有任务按依赖顺序执行。每项只关闭一个可审查的代码或文档闭环，完成对应实现后勾选。人工观察与采样结论只记录在 `verification.md`，不作为任务项或任务勾选条件。

## 0. Landing 生命周期重构

- [x] 0.1 把每脚 `LastLanding`、`NextSwingLanding`、事件观察、Seal、Discard 与 Reset 收入独立 `CharacterFootLandingLifecycle` Module。
- [x] 0.2 把每脚状态收敛为一个完整 Committed Frame 与一个完整 Pending Frame，删除同字段平行散落的生命周期变量。
- [x] 0.3 增加显式 `Empty`、`Tracking`、`Accepted` 状态，并让 Runtime 只通过不可变 `CharacterFootLandingSnapshot` 读取 Pending Landing facts。
- [x] 0.4 把“最后有效落点历史”与“本帧当前Accepted落点”分开：查询失败或没有候选时历史只供完成晋级，Snapshot不得继续发布旧`NextSwingLanding`。
- [x] 0.5 让Ground Path只消费本帧当前Accepted Snapshot；Snapshot无落点时提交Rejected页和空Envelope，不复用旧Accepted Path。

## 1. 统一输入、状态与配置合同

- [ ] 1.1 扩展每脚 Landing Pending/Committed 页，保存 Landing Event、Surface、点、法线、Accepted 状态与独立 Accepted trajectory revision identity。
- [ ] 1.2 新增左右脚共享的 trajectory revision Pending/Committed 页，保存独立 identity、Visible Position/Rotation/Forward、Revision Position/Rotation/Forward、Residual Yaw 与 Pivot 主支撑身份。
- [ ] 1.3 新增每脚锁脚 Pending/Committed 页，保存 Lock Event、锁入准备起始时间/权重、Committed Goal、释放起点修正/权重与剩余时间。
- [ ] 1.4 从 `CharacterFootPlacementPoseInput.Contributions` 解析 Live ActionInstance 与左右脚权重，形成唯一每脚 Action 占用事实。
- [ ] 1.5 把 `CharacterPresentationFactFrame.Grounded` 与 `HorizontalSpeed` 显式送入支撑和朝向纯计算输入，不新增第二状态源。
- [x] 1.6 为 `GoalTransitionHalfLifeSeconds` 增加正式Profile、序列化值与Build校验，删除`MaximumSameEventVerticalJump`配置和资产字段。
- [ ] 1.7 为 `LockDistance`、`SlideDistance`、`UnlockBlendSeconds` 增加正式 Profile、Projection payload 与序列化值。
- [ ] 1.8 为 `StrideSwitchCooldownSeconds` 与 `MaximumPivotYawDeltaDegrees` 增加正式 Profile、Projection payload 与序列化值。
- [ ] 1.9 为朝向增加 `MaximumPitchDegrees`、`MaximumRollDegrees`、`UphillLevelBlend`、`DownhillSlopeBlend` 与 `OrientationRunSpeed` 正式配置。
- [ ] 1.10 只保留有限正数 `PelvisSpringFrequency`，删除旧 `PelvisSpringDampingRatio` 配置、Projection 字段与Runtime读取。
- [x] 1.11 在 Profile Build 校验中拒绝非有限值、非正值以及 `LandingUpdateDistance >= LockDistance`、`LockDistance >= SlideDistance`，并要求Goal换代半衰期为有限正数。
- [ ] 1.12 删除预测误差淡出距离、约束权重及其 Profile、Projection、Runtime、诊断字段，不保留兼容读取。

## 2. 单次 Landing 查询与事件晋级

- [ ] 2.1 每帧 Prepare 时把左右 Landing Committed 页逐字段复制到 Pending，不从空页重建历史。
- [ ] 2.2 在世界查询前识别已完成 Current Event，并只把其最后 Accepted `NextSwingLanding` 原值晋级为 `LastLanding`。
- [ ] 2.3 晋级时逐字段保留点、法线、Surface、Landing Event 与 Accepted revision identity，禁止使用完成帧查询或Animated Sole覆盖。
- [ ] 2.4 在查询前过滤非法、已完成、与LastLanding同identity或超出时间范围的Current/Incoming header。
- [ ] 2.5 Current与Incoming都合法时按较小`TimeToLandingSeconds`选择，相等时稳定选择Current。
- [ ] 2.6 每脚每表现帧只为选中事件执行零次或一次正式Landing SphereCast，删除Current与Incoming双查询结构。
- [ ] 2.7 选中事件查询失败时发布typed rejection，不查询另一事件作为fallback。
- [x] 2.8 删除SurfaceIdentity与高度换级拒绝；同事件任一合法命中都能成为当前Accepted候选。
- [x] 2.9 新命中超过`LandingUpdateDistance`时提交新的Accepted Landing，并复用同一次SphereCast结果重建Ground Path。
- [x] 2.10 新命中未超过死区时保留Accepted Landing、Accepted revision与Committed Ground Path，同时允许下一帧继续查询。
- [x] 2.11 查询失败、Selected Event无效或没有候选时把当前Snapshot退回Tracking，Ground Path发布Rejected且Envelope为空；最后有效落点只保留作晋级历史。
- [ ] 2.12 Ground Path输入只消费LastLanding、NextSwingLanding及其Surface/revision身份，不读取Animated Sole或固定高度。
- [ ] 2.13 保持Capsule、Reachability、Hull与Envelope唯一查询链，不新增脚下Trace。

## 3. Foot Placement trajectory revision

- [ ] 3.1 每帧 Prepare 时把Committed revision完整复制到Pending，Body discontinuity时统一Reset。
- [ ] 3.2 从合法Locked支撑候选建立Pivot资格，未Grounded、Action占用或LastLanding失效时拒绝资格。
- [ ] 3.3 所有双支撑帧优先延续上一Committed且仍Locked的主支撑，GroundedStationary不得每帧重新比较左右脚。
- [ ] 3.4 旧主支撑失效时只从Locked候选按较小水平误差、再按稳定Side顺序重选，删除Sole前后、yaw符号、每帧法线与动画交叉启发式。
- [ ] 3.5 首次建立、主支撑identity变化或旧主支撑失效时把Revision Pose对齐当前Visible Pose并清零Residual Yaw。
- [ ] 3.6 从同帧未被Foot Placement改写的Component Pose读取Current Visible Position/Rotation/Forward，并用当前与上一提交Visible Position的世界位移推进Committed Revision Position；禁止从旧Route或旧查询点重建Visible Pose。
- [ ] 3.7 用Committed Residual Yaw加本帧Visible yaw增量形成requested yaw，并应用单帧角限。
- [ ] 3.8 以`LastLanding + RotateAroundUp(VisiblePositionForRevision - LastLanding, pivotDelta)`计算Virtual Body/Revision Position，并以`PivotRotation * CommittedRevisionRotation`计算Virtual Body/Revision Rotation与Forward。
- [ ] 3.9 保存未消化Residual Yaw，使其只随成功Seal推进，Discard不丢失或重复消化。
- [ ] 3.10 以`VirtualBodyPosition + FutureBodyTranslationWorld + VirtualBodyRotation * RootLocalLanding`计算Raw Landing，禁止旋转Future Body Translation，也禁止旋转旧Route、Surface、Hull或Envelope冒充新事实。
- [ ] 3.11 为每个执行查询的Pending Frame分配一个Foot Placement独立revision identity，左右脚共享且不复用Tick、Event或TrajectoryGeneration。
- [ ] 3.12 把尝试revision、Accepted Landing revision、Ground Path与Envelope identity串成同一lineage；死区复用时保留旧Accepted revision。
- [ ] 3.13 revision输入或查询失败时发布typed rejection，不旋转旧Route、Surface、Hull或Envelope补洞。
- [ ] 3.14 明确阻断Revision Pose写入VisualRoot、Gameplay Body、KCC、Animator Root或实体胶囊。

## 4. 当前步伐与主辅支撑仲裁

- [ ] 4.1 实现支撑资格：Fact Grounded、权威Step非Swing、有效LastLanding且该脚未被有限Action占用。
- [ ] 4.2 实现摆动资格：权威Step为Swing、NextSwingLanding与Selected Query Event/revision一致且未被Action占用。
- [ ] 4.3 两脚同时满足摆动合同且都有增量时只保留垂直包络增量较大的一脚。
- [ ] 4.4 双Swing未选中的脚只有拥有合法支撑资格时才进入支撑合同，否则发布原生事实和零权重。
- [ ] 4.5 处理无LastLanding、Event/revision不一致、无唯一摆动脚与退化步伐的typed拒绝原因。
- [ ] 4.6 支撑切换只由Accepted事件晋级或权威Step身份交换触发，不比较Sole前后位置。
- [ ] 4.7 `StrideSwitchCooldownSeconds`只延迟两个仍合法候选；旧支撑变Swing、失去LastLanding、离地或被Action占用时立即失效。
- [ ] 4.8 GroundedStationary不发布步伐骨盆或Swing Envelope，并分别计算主脚与辅脚锁脚状态。
- [ ] 4.9 Pivot主脚保持Locked Goal，辅脚按自身误差重新进入Locked、Sliding或Unlocked。

## 5. 步伐骨盆与临界弹簧

- [ ] 5.1 实现步伐水平轴、Pose Root投影与`strideProgress`纯计算Builder。
- [ ] 5.2 按起点到终点的Component Up高差确定Flat、Ascending、Descending。
- [ ] 5.3 实现上坡落地后抬升和下坡支撑仍接触时下降的有符号`rawPelvisTargetAlongUp`。
- [ ] 5.4 每帧计算前把Committed spring的旧起点、raw target、output、velocity与SupportSide逐字段复制到Pending。
- [ ] 5.5 按`dot(previousStrideStart - strideStart, ComponentUp)`把旧raw target与旧output重基到新起点坐标系。
- [ ] 5.6 同支撑连续帧只把新旧raw target差作为necessary delta，支撑切换时necessary delta为零。
- [ ] 5.7 按design闭式公式实现固定临界阻尼积分，`deltaSeconds = 0`时保持输入和速度。
- [ ] 5.8 保证`springOutput`是唯一最终骨盆输出，禁止把`springDelta`诊断再次叠加到Goal。
- [ ] 5.9 使用修正后的双脚Sole与同帧原生动画净空计算骨盆下限，并把不足差值补入总输出。
- [ ] 5.10 没有完整步伐、Path rejected、空中或Action占用时清零骨盆Goal，不沿用上一帧目标。
- [ ] 5.11 骨盆只输出`PelvisPreSolveTranslation`，不写VisualRoot、Gameplay Body、KCC或Set Mesh。

## 6. 支撑脚接地、锁入与释放

- [ ] 6.1 实现`plantHeight = max(0, dot(LastLanding - originalSole, ComponentUp))`并禁止负向下拉。
- [ ] 6.2 用同一plantHeight平移Sole与Ankle，保持同帧原生Sole-to-Ankle偏移。
- [ ] 6.3 实现水平误差以及Locked、Sliding、Unlocked互斥边界。
- [ ] 6.4 实现Locked的`plantedSole + horizontalOffset`目标，禁止在LastLanding上重复加plantHeight。
- [ ] 6.5 实现Sliding的`slideT`水平插值，垂直部分继续只使用非负plantHeight。
- [ ] 6.6 第一次Accepted NextSwingLanding时冻结Lock Event与起始TimeToLandingSeconds。
- [ ] 6.7 同事件按TimeToLandingSeconds比值计算单调LockPreparationWeight，只在Seal后推进，不新增第二曲线或时钟。
- [ ] 6.8 事件完成时让LockPreparationWeight到1，Locked/Sliding平地零修正仍发布完整动画位置权重。
- [ ] 6.9 从Committed Locked/Sliding进入Unlocked时冻结上一Goal相对当前Original的修正与上一Position Weight。
- [ ] 6.10 Unlocked首帧保持上一Committed目标和权重，后续只按Committed remaining线性降权。
- [ ] 6.11 `UnlockBlendRemainingSeconds`只通过Pending计算并在Seal提交，Discard不消耗。
- [ ] 6.12 释放归零时目标回到当帧Original、权重归零并清除释放状态。
- [ ] 6.13 重新进入Locked/Sliding时终止释放并消费当前LastLanding，不继续使用旧释放修正。
- [ ] 6.14 空中、Fact未Grounded、Step失效或Action占脚时立即清零对应Goal，不用Unlocked携带旧世界锚。
- [ ] 6.15 Locked/Sliding期间停止该脚Envelope采样与NextSwingLanding追踪，不建立第二Grounding。

## 7. 支撑脚朝向

- [ ] 7.1 移动步伐使用revision后stride forward；GroundedStationary没有步伐时必须使用同一Pending RevisionForward，保证坡面站住不因Stride为空突然丢失朝向。
- [ ] 7.2 将正式前向投影到Landing Normal切平面，退化输入发布零旋转权重。
- [ ] 7.3 按切平面方向沿Component Up的符号区分上坡与下坡。
- [ ] 7.4 实现上坡趋水平、下坡趋法线的targetUp合成。
- [ ] 7.5 在Component空间拆分Pitch/Roll并应用Profile角限后重建目标旋转。
- [ ] 7.6 只允许Locked/Sliding支撑脚发布Rotation Goal，Swing与Unlocked保持零旋转权重。
- [ ] 7.7 只用`CharacterPresentationFactFrame.HorizontalSpeed`与`OrientationRunSpeed`关闭跑步朝向。

## 8. 唯一Goal链与诊断

- [ ] 8.1 按design第10节顺序组装Prepare，保证晋级、Step选择、revision、查询、Path、步伐与Goal使用同一Pending事实。
- [ ] 8.2 保证Pelvis、LeftFoot、RightFoot三个slot在唯一FullBodyIK前各只有一个最终值。
- [ ] 8.3 保证唯一FullBodyIK与唯一final writer使用同一Frame、Completion与Rig lineage。
- [x] 8.4 新增每脚唯一Goal换代Module，保存Committed/Pending相对原生动画踝骨的Component空间位置/旋转修正与权重。
- [x] 8.5 用`GoalTransitionHalfLifeSeconds`计算帧率无关alpha，让本帧输出从上一成功输出收敛到最新原始Goal或零修正。
- [x] 8.6 换代途中原始Goal再次变化时直接以上一成功输出为新起点，不缓存旧Path、旧Envelope或第二目标队列。
- [x] 8.7 离地或有限Action占脚时当帧清空对应换代状态并发布原生Goal零权重，不携带旧世界修正。
- [x] 8.8 把Goal换代接入Foot Placement的BeginPending、Seal、Discard、Reset、Retarget与Dispose事务生命周期。
- [ ] 8.9 增加Selected Query Step、每脚查询次数、尝试/Accepted revision与晋级来源revision诊断。
- [ ] 8.10 增加Current Visible Position/Rotation、Virtual Body/Revision Position/Rotation/Forward、visible/requested/applied/residual yaw与Pivot主支撑身份诊断。
- [ ] 8.11 增加每脚ActionInstance、Action脚权重、Fact Grounded与HorizontalSpeed诊断。
- [ ] 8.12 增加Lock Event、准备起始时间/权重、锁脚状态、水平误差、释放起点修正/权重/剩余时间诊断。
- [ ] 8.13 增加PreviousStrideStart、重基前后raw target、necessary、spring input/output/velocity与最终Pelvis Goal诊断。
- [x] 8.14 增加原始Goal修正、Committed/Pending换代输出、原始/最终权重、半衰期与Source Path identity诊断。
- [ ] 8.15 Gizmo只显示Committed事实，不重新查询、重算Path/Envelope、推进状态或执行FBBIK；状态只用颜色和线框。
- [ ] 8.16 CSV增加上述字段以及最终物理Pelvis/Ankle、Goal residual、写入Completion与typed rejection。

## 9. 正式内容与旧路径清理

- [x] 9.1 为Corin与TrainingEnemy写入同一正式Foot Placement Profile配置，不增加角色旁路配置。
- [ ] 9.2 把已有步伐骨盆Builder迁移到本change的正式命名与输入输出合同，删除旧四步阶段命名和未接线调用。
- [ ] 9.3 删除旧预测误差约束、同事件Surface/高度冻结、可调阻尼比、双Step查询与完成帧重查实现，不保留兼容路径。
- [ ] 9.4 对账current spec、`openspec/project.md`与其它active change，保证只有本change拥有完整预测IK增量。
- [x] 9.5 运行`openspec validate complete-character-predictive-foot-ik --strict --no-interactive`。
