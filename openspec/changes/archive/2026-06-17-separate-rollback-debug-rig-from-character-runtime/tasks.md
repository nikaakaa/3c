## 1. Scope Confirmation
- [x] 1.1 读取本变更 `proposal.md`。
- [x] 1.2 读取本变更 `design.md`。
- [x] 1.3 读取本变更所有 spec deltas。
- [x] 1.4 读取现有 `local-rollback-synctest-foundation` 规格。
- [x] 1.5 读取现有 `fullbody-rollback-replay` 规格。
- [x] 1.6 读取现有 `local-latency-reconciliation` 规格。
- [x] 1.7 确认本变更不处理 TurnBack 抢占生命周期。
- [x] 1.8 确认本变更不接入 Fantasy 网络。

## 2. Current Assembly Audit
- [x] 2.1 列出正式 Corin prefab 上的 rollback/debug/synctest 相关 MonoBehaviour。
- [x] 2.2 列出正式场景 Corin 实例上的 rollback/debug/synctest 相关 MonoBehaviour override。
- [x] 2.3 列出当前 F6/F7/F8 runner 的序列化引用。
- [x] 2.4 列出当前 recorder 的 tick driver 和输入源引用。
- [x] 2.5 列出当前 replay adapter 的角色 runtime 引用。
- [x] 2.6 区分正式角色 runtime 组件和 Debug Tooling 组件。
- [x] 2.7 记录需要迁移到 Debug Rig 的组件清单。

## 3. Character Runtime Boundary Tests
- [x] 3.1 新增 EditMode 测试，校验正式 Corin prefab 不挂载 `LocalRollbackSynctestDebugRunner`。
- [x] 3.2 新增 EditMode 测试，校验正式 Corin prefab 不挂载 `LocalLatencyReconciliationDebugRunner`。
- [x] 3.3 新增 EditMode 测试，校验正式 Corin prefab 不挂载 `LocalRollbackSoakDebugRunner`。
- [x] 3.4 新增 EditMode 测试，校验正式 Corin prefab 不挂载 `LocomotionSnapshotHistoryRecorder`。
- [x] 3.5 新增 EditMode 测试，校验正式 Corin prefab 不挂载 `PredictionInputHistoryTickRecorder`。
- [x] 3.6 新增 EditMode 测试，校验正式 Corin prefab 不挂载 `FullBodyRollbackSimulation`。
- [x] 3.7 新增 EditMode 测试，校验正式 Corin prefab 仍保留 `CharacterFrameRuntimeController`。
- [x] 3.8 新增 EditMode 测试，校验正式 Corin prefab 仍保留正式 Locomotion / Action runtime 引用。

## 4. Debug Rig Assembly Tests
- [x] 4.1 新增独立 `RollbackDebugRig` prefab。
- [x] 4.2 在 `RollbackDebugRig` prefab 中装配 prediction input source。
- [x] 4.3 在 `RollbackDebugRig` prefab 中装配 input history recorder。
- [x] 4.4 在 `RollbackDebugRig` prefab 中装配 snapshot history recorder。
- [x] 4.5 在 `RollbackDebugRig` prefab 中装配 FullBody replay adapter。
- [x] 4.6 在 `RollbackDebugRig` prefab 中装配 F6 synctest runner。
- [x] 4.7 在 `RollbackDebugRig` prefab 中装配 F7 latency runner。
- [x] 4.8 在 `RollbackDebugRig` prefab 中装配 F8 soak runner。
- [x] 4.9 测试 Debug Rig 的 replay adapter 显式引用目标 `CharacterFrameRuntimeController`。
- [x] 4.10 测试 Debug Rig 的 recorder 显式引用目标 tick driver 或等价 tick source。
- [x] 4.11 测试 Debug Rig 的 runner 显式引用 recorder 和 replay adapter。
- [x] 4.12 测试缺失关键引用时 Debug Rig 返回诊断失败。

## 5. Runtime Reference Resolution
- [x] 5.1 调整 `FullBodyRollbackSimulation` 引用解析，使正式语义以显式目标角色 runtime 为准。
- [x] 5.2 调整 `LocalRollbackSynctestDebugRunner` 引用解析，使 runner 优先使用显式 Debug Rig 引用。
- [x] 5.3 调整 `LocalLatencyReconciliationDebugRunner` 引用解析，使 runner 优先使用显式 Debug Rig 引用。
- [x] 5.4 调整 `LocalRollbackSoakDebugRunner` 引用解析，使 runner 优先使用显式 Debug Rig 引用。
- [x] 5.5 调整 `LocomotionSnapshotHistoryRecorder` 引用解析，使 recorder 不依赖挂在角色自身上才能工作。
- [x] 5.6 调整 `PredictionInputHistoryTickRecorder` 引用解析，使 recorder 不依赖挂在角色自身上才能工作。
- [x] 5.7 调整 `LocomotionPredictionInputFrameSource` 引用解析，使 input source 通过显式目标角色读取输入。
- [x] 5.8 保留必要的编辑期自动填充，但不把它作为正式 runtime fallback。

## 6. Asset / Scene Migration
- [x] 6.1 创建或更新最小 `RollbackDebugRig` prefab 资产。
- [x] 6.2 将 F6 runner 从正式角色装配迁移到 Debug Rig。
- [x] 6.3 将 F7 runner 从正式角色装配迁移到 Debug Rig。
- [x] 6.4 将 F8 runner 从正式角色装配迁移到 Debug Rig。
- [x] 6.5 将 input recorder 从正式角色装配迁移到 Debug Rig。
- [x] 6.6 将 snapshot recorder 从正式角色装配迁移到 Debug Rig。
- [x] 6.7 将 FullBody replay adapter 从正式角色装配迁移到 Debug Rig。
- [x] 6.8 将 prediction input source 从正式角色装配迁移到 Debug Rig。
- [x] 6.9 保持 Debug Rig 到目标角色的显式引用完整。
- [x] 6.10 保持正式角色配置根和正式 runtime 组件不新增 fallback 字段。

## 7. Static Boundary Verification
- [x] 7.1 新增静态边界测试，确认 rollback debug runner 不作为正式角色 runtime 必需组件。
- [x] 7.2 新增静态边界测试，确认 Debug Rig 不创建第二个 `CharacterFramePipeline`。
- [x] 7.3 新增静态边界测试，确认 Debug Rig 不创建第二个 motion executor。
- [x] 7.4 新增静态边界测试，确认 Debug Rig 不创建第二个 animation presenter。
- [x] 7.5 新增静态边界测试，确认 replay adapter 通过目标角色正式 runtime 入口推进。
- [x] 7.6 新增静态边界测试，确认缺失 Debug Rig 引用不会走隐藏 fallback 配置。

## 8. Behaviour Validation
- [x] 8.1 运行覆盖 `RollbackDebugRig` prefab 装配的 EditMode 测试。
- [x] 8.2 运行覆盖 FullBody replay adapter 的 EditMode 测试。
- [x] 8.3 运行覆盖 local rollback synctest runner 的 EditMode 测试。
- [x] 8.4 运行覆盖 local latency reconciliation runner 的 EditMode 测试。
- [x] 8.5 运行覆盖 local rollback soak runner 的 EditMode 测试。
- [x] 8.6 运行覆盖 Corin prefab 装配边界的 EditMode 测试。
- [x] 8.7 运行 `openspec validate separate-rollback-debug-rig-from-character-runtime --strict --no-interactive`。
- [x] 8.8 运行 C# 编译检查。

## 9. Final Review
- [x] 9.1 确认 `tasks.md` 中每项任务均已完成后再更新勾选状态。
- [x] 9.2 确认没有新增第二角色控制器路径。
- [x] 9.3 确认没有新增 fallback 配置。
- [x] 9.4 确认正式角色 prefab 不承载 rollback debug tooling。
- [x] 9.5 确认 Debug Rig 仍能执行 F6/F7/F8 对应工具入口。
- [x] 9.6 确认本变更没有实现 TurnBack 抢占生命周期。
- [x] 9.7 确认本变更没有修改 Fantasy 协议。
