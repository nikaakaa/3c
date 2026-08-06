# Tasks

- [ ] 0.1 确认 `replace-pose-ik-with-finalik-full-body-solver` 已完成并归档，current spec 已从旧 `FootPlacement + LegIK` 收口到 `PredictiveFootPlacement Goal Source + FullBodyIK`。
- [ ] 0.2 确认 Corin Rig v4、Calibration v4、Foot Analysis artifact、Presentation Projection 与目标 Program 的输入 identity 处于同一正式链路。
- [ ] 0.3 记录当前 Corin Foot Placement Profile、Idle binding、Foot Analysis identity、Profile revision 和生成 Projection revision。
- [ ] 1.1 通过正式 Character Presentation authoring 入口将 Corin Foot Placement Profile 的 `LockType` 设为 `PivotAroundToe`，删除 `Unlocked` 作为 Corin 内容路径的有效配置。
- [ ] 1.2 保持 Corin Idle source 的 `Foot Placement Weight` 曲线全程为 `1`，并保持其 Foot Analysis Source、Rig 与 Calibration 引用精确匹配。
- [ ] 1.3 检查所有 Corin source 不存在第二份 LockType、Idle 专用脚相位字段、Fallback binding 或按动画名称选择脚锁策略。
- [ ] 1.4 让 Profile revision、Presentation dependency 和 Stale 状态按现有 authoring contract 更新；不得在 `OnValidate`、Inspector 或 Preview 中执行分析或编译。
- [ ] 2.1 在现有显式 Character Build 入口增加 Corin 内容验证：拒绝 `Unlocked`、缺失 Idle binding、Idle 权重不完整、Foot Analysis identity 不匹配和旧 Pose/IK contract。
- [ ] 2.2 确保该验证在 Projection/Program 重操作前完成，并且不由资产选择、重绘、`OnInspectorGUI` 或自动 dirty callback 触发。
- [ ] 2.3 保持 Foot Analysis artifact 只在 Clip、Rig、Calibration 或 Analyzer identity 变化时重建；本 change 只因 Profile 改动重新发布依赖它的 Projection/Program。
- [ ] 3.1 使用显式 Character Build 原子生成匹配 Profile revision 的 Presentation Projection、请求的 Float32/Fixed Target Program、Pose tuning layout 与 Unity wrapper。
- [ ] 3.2 为 Build Request 增加 Profile/Projection/Program/IK backend identity 的交叉校验；任一失败时恢复旧发布组，不产生兼容或半发布路径。
- [ ] 3.3 确认生成 Projection 只保存 `PivotAroundToe` 对应的正式 Profile revision，不保存旧 `Unlocked` 值或运行时补默认逻辑。
- [ ] 4.1 复核现有 Presentation diagnostics 能表达双脚 `Free -> Locked`、`ContactCommitted`、`Sliding`、释放原因和 Foot Goal contribution；若缺少字段，只补同一 snapshot 链，不创建第二调试链。
- [ ] 4.2 更新 Corin 内容 spec、项目当前状态和 active change 依赖说明，使“脚锁已启用”只在显式 Build 发布后成立。
- [ ] 5.1 删除实施过程中发现的旧 Corin Unlocked 配置、旧生成引用或临时脚锁数据；不保留迁移兼容、fallback 或双写资产。
