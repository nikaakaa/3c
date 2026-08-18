# 实施任务

所有任务按依赖顺序执行。每项只关闭一个可审查的代码或文档闭环；没有完成实现并通过对应的 `verification.md` 阶段前，不得勾选。任务列表不记录手动验证过程，手动验证只记录在独立的验证文档中。

## 1. 统一数据合同

- [ ] 1.1 在 Foot Placement Pending/Committed landing page 中明确 `LandingEventIdentity`、`SurfaceIdentity`、点、法线、时间和 Accepted 状态的所有字段。
- [ ] 1.2 为同一 Landing Event 增加“已接受踏面”的状态，不使用 AuthorityTick、坐标哈希或预测误差作为身份。
- [ ] 1.3 为 `MaximumSameEventVerticalJump` 增加正式 Profile 字段和序列化值。
- [ ] 1.4 为 `LockDistance`、`SlideDistance`、`UnlockBlendSeconds` 增加正式 Profile 字段和序列化值。
- [ ] 1.5 为 `StrideSwitchCooldownSeconds` 增加正式 Profile 字段和序列化值。
- [ ] 1.6 为朝向限制增加 `MaximumPitchDegrees`、`MaximumRollDegrees`、`UphillLevelBlend`、`DownhillSlopeBlend` 和 `OrientationRunSpeed`。
- [ ] 1.7 为转向路径增加 `MaximumPivotYawDeltaDegrees`，并把它作为有限输入校验，不作为运行时 fallback。
- [ ] 1.8 在 Profile Build 校验中拒绝非有限值、非正值和 `LandingUpdateDistance >= LockDistance`、`LockDistance >= SlideDistance` 的配置。
- [ ] 1.9 删除预测误差淡出距离及其 Projection、Runtime、诊断字段；不保留兼容读取。

## 2. 同事件落点和 Ground Path

- [ ] 2.1 保证 PreSwing/Swing 每个有效表现帧只执行一次正式 Landing SphereCast。
- [ ] 2.2 在更新 NextSwingLanding 前检查 Landing Event identity；事件变化时建立新事件页，事件未变化时只更新当前页。
- [ ] 2.3 先比较 `SurfaceIdentity`，再比较沿 Component Up 的高度差，统一实现换级判定。
- [ ] 2.4 同踏面且超过 `LandingUpdateDistance` 时提交新的 Accepted Landing，并用同一次 SphereCast 结果重建 Ground Path。
- [ ] 2.5 同踏面但未超过死区时复用 Accepted Landing 和已提交 Ground Path，同时保留下一帧预测查询。
- [ ] 2.6 换级命中时发布 typed rejection 和 Warning，保留本事件最后一个 Accepted Landing，不把新命中写入 Path。
- [ ] 2.7 Swing Event 完成时只把最后一个 Accepted NextSwingLanding 晋级为 LastLanding。
- [ ] 2.8 Ground Path 输入只消费 LastLanding、NextSwingLanding 和各自 SurfaceIdentity，不读取 Animated Sole 或固定高度。
- [ ] 2.9 保持 Capsule、Reachability、Hull 和 Envelope 的唯一查询链，不新增脚下 Trace。

## 3. 当前步伐仲裁

- [ ] 3.1 实现支撑脚判定：权威 Step 非 Swing、拥有有效 LastLanding、未被有限 Action 占用。
- [ ] 3.2 实现摆动脚判定：权威 Step 为 Swing、NextSwingLanding 存在且 Event identity 一致。
- [ ] 3.3 两脚同时满足摆动合同且均有可用增量时，只选择垂直包络增量较大的一脚。
- [ ] 3.4 双 Swing 中未被选择的脚只有在拥有 LastLanding 时才进入支撑合同，否则发布原生事实和零权重。
- [ ] 3.5 处理双脚无 LastLanding、Step identity 不一致、无唯一摆动脚和 degenerate stride 的拒绝原因。
- [ ] 3.6 只在事件晋级或权威 Step 身份交换时切换支撑侧。
- [ ] 3.7 实现 `StrideSwitchCooldownSeconds`：旧支撑合同仍有效时才延迟候选切换；旧支撑已变Swing、失去LastLanding或被有限Action占用时立即清除旧合同，不得用冷却保持旧锁脚。

## 4. 步伐骨盆

- [ ] 4.1 实现步伐水平轴、Pose Root 投影和 `strideProgress` 的纯计算 Builder。
- [ ] 4.2 按起点到终点的 Component Up 高差确定 Flat、Ascending、Descending。
- [ ] 4.3 实现上坡落地后抬升和下坡支撑仍接触时下降的有符号 `rawPelvisTargetAlongUp`。
- [ ] 4.4 将必要位移定义为同一支撑连续帧的总目标差值，支撑切换时不重复叠加旧目标。
- [ ] 4.5 将 `previousStrideStart`、`previousRawPelvisTargetAlongUp`、`previousSpringOutput`、SupportSide 和弹簧速度纳入 Pending/Committed 页。
- [ ] 4.6 实现临界阻尼弹簧的单一 `springOutput`，禁止把诊断分解值再次叠加到 Goal。
- [ ] 4.7 使用修正后的双脚 Sole 计算同帧原生净空下限，并将不足差值补入骨盆总输出。
- [ ] 4.8 没有完整步伐、Path rejected、空中或有限 Action 占用时清零骨盆 Goal，不沿用上一帧目标。
- [ ] 4.9 将骨盆输出限制为 `PelvisPreSolveTranslation`，不写 VisualRoot、Gameplay Body、KCC 或 Set Mesh。

## 5. 支撑脚接地和锁脚

- [ ] 5.1 实现 `plantHeight = max(0, dot(LastLanding - originalSole, ComponentUp))`。
- [ ] 5.2 以 `plantHeight` 同时构造 Sole 和 Ankle 的垂直接地目标，禁止负向下拉。
- [ ] 5.3 实现水平误差计算以及 Locked、Sliding、Unlocked 的互斥状态判定。
- [ ] 5.4 实现 Locked 的 `plantedSole + horizontalOffset` 目标和原生 Sole 到 Ankle 的偏移传递。
- [ ] 5.5 实现 Sliding 的 `slideT` 和水平插值，垂直部分继续使用非负 plantHeight。
- [ ] 5.6 实现每只脚的 Lock Event identity、锁入计时和锁入权重；锁入时基只能来自该事件的 TimeToLandingSeconds。
- [ ] 5.7 实现每只脚的 `UnlockBlendRemainingSeconds`，只在成功 Seal 后递减，Discard 恢复旧值。
- [ ] 5.8 Unlocked 时回到原生动画目标并在正式解锁时间内降到零权重，不钉住过远落点。
- [ ] 5.9 Locked 或 Sliding 时停止该脚 Envelope 采样和 NextSwingLanding 追踪。
- [ ] 5.10 Idle/GroundedStationary 且两脚均为有效支撑时，让两脚复用同一支撑合同，不创建第二套 Grounding 或 IK。

## 6. 支撑脚朝向

- [ ] 6.1 实现落点法线和步伐前进方向的切平面基向量计算，并为退化输入发布零旋转权重。
- [ ] 6.2 实现上坡趋水平、下坡趋法线的 `targetUp` 合成。
- [ ] 6.3 在 Component 空间拆分 Pitch/Roll 并应用 Profile 角限，再重建目标旋转。
- [ ] 6.4 仅允许 Locked/Sliding 支撑脚发布 Rotation Goal，摆动脚保持零旋转权重。
- [ ] 6.5 达到 OrientationRunSpeed 时关闭所有支撑脚朝向，不把坡面法线写入脚踝。

## 7. 转向 Pivot

- [ ] 7.1 从上一已提交可见前向和当前可见前向计算 Component Up 轴的 Signed yaw 增量。
- [ ] 7.2 应用 `MaximumPivotYawDeltaDegrees` 限制，并把超出部分留给后续表现帧。
- [ ] 7.3 用支撑 LastLanding 启动唯一 trajectory revision，重新执行本帧唯一 Landing SphereCast、Capsule、Reachability、Hull 和 Envelope；不得刚体旋转旧 Route、Surface 或 Envelope。
- [ ] 7.4 Pivot 失效时不创建 revision，不猜方向，不发布基于旧 Path 旋转的摆动目标；支撑合同按当前有效事实处理。
- [ ] 7.5 明确禁止 Pivot 写 VisualRoot、KCC、Gameplay Body 或实体胶囊朝向。

## 8. 唯一 Goal 链和诊断

- [ ] 8.1 按“临时支撑候选 -> yaw/revision -> 落点更新 -> Path/Envelope -> 步伐 -> 摆动/支撑/朝向 -> 骨盆 -> 同一 GoalSet”顺序组装，并保证骨盆和脚消费同一 revision 后端点。
- [ ] 8.2 保证 Pelvis、LeftFoot、RightFoot 三个 slot 在同一 FullBodyIK 前只产生一个最终值。
- [ ] 8.3 保证唯一 FullBodyIK 和唯一 final writer 使用同一 Frame、Completion 和 Rig lineage。
- [ ] 8.4 为每只脚增加 SurfaceIdentity、锁脚状态、水平误差、锁入/解锁剩余时间和朝向权重诊断。
- [ ] 8.5 保留步伐起止点、PreviousStrideStart、重基后的 raw target、necessary、spring、Progress、Slope 和最终 Pelvis Goal 诊断。
- [ ] 8.6 Gizmo 只显示事实，不重新查询世界、不重算 Path/Envelope、不执行 FBBIK；状态使用颜色和线框，不显示文字。
- [ ] 8.7 CSV 增加最终物理骨盆、物理踝骨、Goal 残差、写入 Completion 和 typed rejection 对账字段。

## 9. 正式内容和文档收口

- [ ] 9.1 为 Corin 和 TrainingEnemy 写入同一正式 Foot Placement Profile 配置，不增加角色旁路配置。
- [ ] 9.2 删除 `add-character-foot-placement-stride-hips` 的独立实施口径，保留本 change 的唯一验收顺序。
- [ ] 9.3 将 `verification.md` 的七阶段观察字段与实现后的诊断/CSV字段逐项对齐。
- [ ] 9.4 对照 current spec、`openspec/project.md` 和冲突 change，确认归档时只合并本 change 的完整预测 IK 口径。
- [ ] 9.5 运行 `openspec validate complete-character-predictive-foot-ik --strict --no-interactive`。
