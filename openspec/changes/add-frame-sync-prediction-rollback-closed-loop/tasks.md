## 1. Prediction Buffer
- [ ] 1.0 对照 `Ref/ggpo/src/lib/ggpo/input_queue.*` 梳理 input queue 参考点
- [ ] 1.1 定义 pending outbound item
- [ ] 1.2 定义 pending outbound sent state
- [ ] 1.3 定义 pending outbound ack state
- [ ] 1.4 定义 predicted history
- [ ] 1.5 定义 confirmed history
- [ ] 1.6 定义 resolved input stream
- [ ] 1.7 定义 confirmed tick 裁剪
- [ ] 1.8 定义 late confirmed input 失败
- [ ] 1.9 定义 resend 查询但不持有 session

## 2. Reconciliation
- [ ] 2.0 对照 `Ref/ggpo/src/lib/ggpo/sync.*` 梳理 check/adjust/replay 参考点
- [ ] 2.1 定义 confirmed input resolver
- [ ] 2.2 定义 prediction vs confirmed 字段 diff
- [ ] 2.3 定义 first divergence tick
- [ ] 2.4 定义 restore tick
- [ ] 2.5 定义 replay end tick
- [ ] 2.6 定义 missing snapshot 结果
- [ ] 2.7 定义 missing input 结果
- [ ] 2.8 定义 prediction correction 结果
- [ ] 2.9 定义 replay nondeterminism 结果
- [ ] 2.10 定义 result diagnostic DTO

## 3. Rollback 接入
- [ ] 3.1 规划现有 runner 输入源抽象
- [ ] 3.2 保持本地 latency runner 现有公开行为和测试语义
- [ ] 3.3 网络 confirmed input 使用同一 runner 语义
- [ ] 3.4 restore 使用 `ILocalRollbackSynctestSimulation`
- [ ] 3.5 advance 使用 `ILocalRollbackSynctestSimulation`
- [ ] 3.6 capture 使用 `ILocalRollbackSynctestSimulation`
- [ ] 3.7 compare 使用 scoped comparison
- [ ] 3.8 禁止直接调用 `CharacterController.Move`
- [ ] 3.9 禁止创建第二 `CharacterFramePipeline`

## 4. Correction
- [ ] 4.1 定义 correction id
- [ ] 4.2 定义 correction reason
- [ ] 4.3 定义 authoritative tick
- [ ] 4.4 定义 restore tick
- [ ] 4.5 定义 replay range
- [ ] 4.6 定义 checksum mismatch summary
- [ ] 4.7 定义 correction queue
- [ ] 4.8 定义 simulation tick consume
- [ ] 4.9 定义 duplicate correction 去重
- [ ] 4.10 定义 correction apply failure

## 5. Checksum
- [ ] 5.0 对照 GGPO save/load/checksum callback 梳理 snapshot checksum 参考点
- [ ] 5.1 定义 strict projection 字段
- [ ] 5.2 定义字段排序
- [ ] 5.3 定义浮点量化
- [ ] 5.4 定义 config hash 参与规则
- [ ] 5.5 定义 checksum schema version
- [ ] 5.6 定义 presentation drift 排除
- [ ] 5.7 定义 mismatch diagnostic
- [ ] 5.8 定义 checksum report 频率

## 6. 自动测试
- [ ] 6.1 添加 pending outbound 入队测试
- [ ] 6.2 添加 confirmed tick 裁剪测试
- [ ] 6.3 添加 predicted 被 confirmed 替换测试
- [ ] 6.4 添加 no correction 测试
- [ ] 6.5 添加 prediction correction 测试
- [ ] 6.6 添加 replay nondeterminism 测试
- [ ] 6.7 添加 missing snapshot 测试
- [ ] 6.8 添加 missing input 测试
- [ ] 6.9 添加 correction queue 去重测试
- [ ] 6.10 添加 correction 不直接写 Transform 静态测试
- [ ] 6.11 添加 checksum 稳定测试
- [ ] 6.12 添加 presentation drift 不影响 checksum 测试
- [ ] 6.13 添加不创建第二 pipeline 静态测试

## 7. 验证
- [ ] 7.1 运行相关 EditMode 测试
- [ ] 7.2 运行 `openspec validate add-frame-sync-prediction-rollback-closed-loop --strict --no-interactive`
- [ ] 7.3 运行 `openspec validate --all --strict --no-interactive`
