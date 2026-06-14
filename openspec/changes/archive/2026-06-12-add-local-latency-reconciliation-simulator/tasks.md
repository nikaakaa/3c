## 1. 现状确认
- [x] 1.1 读取 `update-fullbody-rollback-replay` 的 proposal、design、spec delta，确认其全链路接口。
- [x] 1.2 确认 `ILocalRollbackSynctestSimulation` 接口稳定且不计划在本次变更中修改。
- [x] 1.3 确认 `PredictionInputHistory` 和 `PredictionSnapshotHistory` 的 API 能满足延迟模拟和 reconciliation 需求。
- [x] 1.4 读取 GGPO `Sync`、`InputQueue`、`SyncTestBackend` 参考代码，记录映射关系。
- [x] 1.5 确认 `CharacterSimulationSnapshotComparer` 的比较字段覆盖 reconciliation 所需的所有维度。
- [x] 1.6 确认本变更不修改 Fantasy proto、协议导出工具或服务端代码。

## 2. 远端输入延迟队列
- [x] 2.1 新增 `LatencySimulator` 纯数据类。
- [x] 2.2 支持按 tick 写入输入帧并指定延迟到达 tick。
- [x] 2.3 支持按 tick 查询输入是否已到达。
- [x] 2.4 支持按 tick 取出已到达输入帧。
- [x] 2.5 未到达时返回"未到达"状态（非异常）。
- [x] 2.6 容量上限，超容时裁剪最旧帧。
- [x] 2.7 支持修剪已确认 tick 前的帧。
- [x] 2.8 不保存 Unity Object 引用。
- [x] 2.9 测试：零延迟写入后立即可取出。
- [x] 2.10 测试：延迟 3 tick 后在正确 tick 到达。
- [x] 2.11 测试：未到达 tick 返回未到达。
- [x] 2.12 测试：容量裁剪正确。

## 3. 输入预测策略
- [x] 3.1 定义 `IPredictionInputStrategy` 接口。
- [x] 3.2 实现 `RepeatLastFramePredictionStrategy`（重复上一帧）。
- [x] 3.3 上一帧存在时返回重复内容，标记为预测。
- [x] 3.4 无上一帧时返回无法预测诊断。
- [x] 3.5 预测不写入真实输入历史。
- [x] 3.6 测试：有上一帧时重复。
- [x] 3.7 测试：无上一帧时失败。
- [x] 3.8 测试：预测帧 Tick 正确。
- [x] 3.9 测试：真实输入可替代预测帧。

## 4. Reconciliation 输入解析
- [x] 4.1 定义每 tick 的解析后输入（远端真实输入 或 预测输入）。
- [x] 4.2 实现输入解析器：先从延迟队列取真实输入，缺失时用预测策略填充。
- [x] 4.3 解析结果标记是真实还是预测。
- [x] 4.4 测试：真实输入存在时优先使用。
- [x] 4.5 测试：真实缺失时使用预测。
- [x] 4.6 测试：标记正确。

## 5. Reconciliation 编排引擎
- [x] 5.1 新增 `LocalLatencyReconciliationRunner`。
- [x] 5.2 接收 `ILocalRollbackSynctestSimulation`、`PredictionInputHistory`（本地）、`LatencySimulator`（远端）、`PredictionSnapshotHistory`。
- [x] 5.3 实现 `CheckSimulation`：从 confirmed tick 开始逐 tick 比对本地快照和远端重放快照。
- [x] 5.4 找到 first incorrect tick 后停止。
- [x] 5.5 实现 `AdjustSimulation`：恢复 first incorrect tick - 1 快照，从 first incorrect tick 开始用远端/预测输入重放到当前。
- [x] 5.6 调整后比较最终快照。
- [x] 5.7 输出 `LocalLatencyReconciliationResult`（含 first incorrect tick、追帧数、最终比较结果）。
- [x] 5.8 所有 tick 一致时不执行回滚。
- [x] 5.9 恢复快照缺失时返回失败诊断。
- [x] 5.10 不绕过 `ILocalRollbackSynctestSimulation` 接口。
- [x] 5.11 不直接调用 `CharacterController.Move` 或 `BasicLocomotionPipeline`。

## 6. 自动测试：延迟队列
- [x] 6.1 测试延迟队列零延迟写入取出。
- [x] 6.2 测试延迟 3 tick 后正确到达。
- [x] 6.3 测试未到达 tick 查询返回 false。
- [x] 6.4 测试容量裁剪移除最旧帧。
- [x] 6.5 测试 TrimConfirmedBefore 正确裁剪。

## 7. 自动测试：输入预测
- [x] 7.1 测试重复上一帧策略：Move 输入重复。
- [x] 7.2 测试重复上一帧策略：Dodge pressed 不重复为 pressed。
- [x] 7.3 测试重复上一帧策略：Run held 保持。
- [x] 7.4 测试无上一帧时失败。
- [x] 7.5 测试预测帧不污染真实历史。

## 8. 自动测试：Reconciliation 核心
- [x] 8.1 测试远端输入按时到达、预测正确时不回滚。
- [x] 8.2 测试远端输入延迟 2 tick 但预测正确时不回滚。
- [x] 8.3 测试远端输入内容不同（模拟不同按键）时检测到 first incorrect tick。
- [x] 8.4 测试找到 first incorrect tick 后回滚重放并收敛。
- [x] 8.5 测试回滚后追帧数量正确。
- [x] 8.6 测试恢复快照缺失时返回失败。
- [x] 8.7 测试 reconciliation 不修改 `PredictionInputHistory`。
- [x] 8.8 测试 Move + Dodge 场景下 delay → predict → reconcile 收敛。

## 9. Debug Runner 接入
- [x] 9.1 新增 `LocalLatencyReconciliationDebugRunner` MonoBehaviour。
- [x] 9.2 Inspector 配置：`LatencyTicks`、`PredictionStrategy`、`TriggerKey`。
- [x] 9.3 自动引用 `FullBodyRollbackSimulation`、recorder、history 组件。
- [x] 9.4 按键触发 reconciliation（默认 F7）。
- [x] 9.5 默认安全探针：reconciliation 后恢复触发前现场。
- [x] 9.6 可选可见 correction：应用结果 + `PresentationTransformInterpolator` 插值。
- [x] 9.7 Console 输出 first incorrect tick、追帧数、最终 differences。
- [x] 9.8 缺少组件引用时输出明确诊断。
- [x] 9.9 不修改现有 `LocalRollbackSynctestDebugRunner` 的 F6 行为。

## 10. 静态边界验证
- [ ] 10.1 搜索 reconciliation core 不引用 Fantasy。
- [ ] 10.2 搜索 reconciliation core 不引用协议 DTO。
- [ ] 10.3 搜索 reconciliation core 不引用 Animancer runtime object。
- [ ] 10.4 搜索 reconciliation core 不引用 Cinemachine。
- [ ] 10.5 搜索 reconciliation core 不直接调用 `CharacterController.Move`。
- [ ] 10.6 搜索 reconciliation core 不引用 `BasicLocomotionPipeline`。
- [ ] 10.7 搜索未新增第二套 player controller。
- [ ] 10.8 搜索未修改 `NetworkProtocol/*.proto`。

## 11. 手动验证
- [ ] 11.1 打开 `Assets/Scenes/Sandbox.unity`。
- [ ] 11.2 挂载 `LocalLatencyReconciliationDebugRunner` 到角色（含 `FullBodyRollbackSimulation`、recorder）。
- [ ] 11.3 设置 `LatencyTicks = 3`。
- [ ] 11.4 进入 Play Mode，移动、Run、Dodge 积累历史。
- [ ] 11.5 按 F7 触发 reconciliation。
- [ ] 11.6 验证 Console 输出 PASS 或 FAIL + differences。
- [ ] 11.7 验证不启用可见 correction 时角色不产生视觉跳变。
- [ ] 11.8 启用可见 correction 时验证差值插值平滑。
- [ ] 11.9 验证现有 F6 synctest 行为不变（无延迟的理想条件测试）。

## 12. 验证命令
- [ ] 12.1 运行 `openspec validate add-local-latency-reconciliation-simulator --strict --no-interactive`。
- [ ] 12.2 运行 Unity Editor 编译，确认 Console 无编译错误。
- [ ] 12.3 运行定向 EditMode 测试 `ThirdPersonSimulation.Tests.LocalRollbackSynctestFoundationTests`。
- [ ] 12.4 运行定向 EditMode 测试 `ThirdPersonSimulation.Tests.FullBodyRollbackReplayTests`。
- [ ] 12.5 运行新写的延迟模拟和 reconciliation 测试。
- [ ] 12.6 不使用 Unity batchmode。

## 13. 后续交接
- [ ] 13.1 在 `docs/agents/action-fighting-prediction-rollback-guide.md` 更新阶段状态。
- [ ] 13.2 明确本变更完成后再规划 Fantasy 接入。
- [ ] 13.3 确认 reconciliation 接口保持不变，后续替换远端输入源为 Fantasy 时无需架构变化。
