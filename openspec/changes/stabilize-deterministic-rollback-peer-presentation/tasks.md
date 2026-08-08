# Tasks

## Policy 与身份

- [x] 在 `DeterministicRollbackModelPolicy` 增加 `MaximumPredictionLeadTicks`，校验其范围并升级 configuration hash 与 model semantic identity。
- [x] 在 Rollback Pipeline Definition 增加正式序列化字段，将双 Peer Demo 的 prediction lead 配为 8 Tick。
- [x] 在 Server Manifest、Build adapter、Product manifest 和读取校验中增加该字段。
- [x] 更新 Rollback handshake compatibility，使 prediction lead 成为 Active 前必须一致的模型配置。

## Schedule 与诊断

- [x] 修改 `RollbackSchedulePass`，forward prediction 使用 `MaximumPredictionLeadTicks`，restore/replay 继续使用 `MaximumRollbackDepthTicks`。
- [x] 保证达到 prediction lead 后只返回现有 `NoStep`，Ingress 继续处理 explicit、canonical 和 confirmation，不新增 predicted history。
- [x] 补充 prediction lead、Peer explicit frontier gap、paced NoStep、predicted fallback 与本地 dropped logic tick 的独立诊断。
- [x] 删除 `MaximumRollbackDepthTicks` 同时表达 forward prediction horizon 的代码、文档和诊断文案。

## Action 分支提交

- [x] 将 Rollback Output Disposition 中的 `CompleteProducer` 与 `ReleaseProducer` 归为 confirmed-only，保留 Select/Sample 的 predictable/reversible 语义。
- [x] 将 Fixed Unity Presentation Adapter 中的 animation selection/sample/terminal 记录收口为按 outer transaction 计算的最终 Action 分支。
- [x] 在 Adapter 内建立唯一的未确认动画撤销边界，不让回滚撤销 Select/Sample 继续调用合成 Release 的通用 `Retire`。
- [x] 让 Action Playback Runtime 继续只在现有 Evaluate Barrier 前事务内消费最终有效命令，保持 lifecycle、sample history、Slot usage、source continuity 与 release ownership 的单一Seal边界。
- [x] 在 confirmed terminal 成功提交后再裁剪 adapter sample/terminal history，使 `PruneConfirmed` 只拥有 rollback history 裁剪职责。
- [x] 将“未确认分支恢复同 generation”与“confirmed terminal 后同 generation Sample”分开处理，只对后者进入正式 Faulted。
- [x] 保持 Body branch owner、Presentation Clock、PoseStateMachine 和 Physical Bone transaction 现有边界，不新增 Transform、整 Rig 恢复或 confirmed Body 缓冲旁路。
- [x] 分离 Body branch sequence 与 Pose discontinuity generation，使普通 Committed branch replacement 只重基 Body/Intent history并重定向Foot Placement与Motion Matching，不重置Walk/Run连续播放。
- [x] 沿用现有 Presentation Runtime Fault 诊断，区分非法 terminal-after-sample 与可恢复的未确认分支撤销。

## 收口

- [x] 同步 DeterministicRollback Network Model、Character Presentation Interpolation 和 Character Animation Pipeline 的 spec delta。
- [x] 更新受影响的版本、identity 和生成产物引用，删除旧语义和旧调用链。
- [x] 执行 OpenSpec strict validation，保持 active Rollback character pipeline change 的范围不变。
