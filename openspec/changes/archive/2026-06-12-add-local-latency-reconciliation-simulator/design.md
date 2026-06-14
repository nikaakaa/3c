## Context
`update-fullbody-rollback-replay` 完成后，本地 synctest（F6）已能证明 Move/Run/Dodge 在同一段输入下重放收敛。但这是理想条件：所有输入都是本地的，不存在远端延迟到达的问题。

真实网络环境下，动作格斗需要客户端**不等待远端输入就继续推进**（预测），等远端输入到达后再检查是否猜错，猜错就从错误帧回滚重放。GGPO 把这套流程标准化为：

```text
AddLocalInput(本地输入)
SynchronizeInputs(取当前帧所有玩家输入，远端缺失时预测)
IncrementFrame(帧+1，保存状态)
CheckSimulation(检查输入队列是否发现预测错误)
AdjustSimulation(LoadFrame → ResetPrediction → advance_frame 追帧)
```

本项目当前已有：
- 输入记录：`PredictionInputHistory`（类似 GGPO 输入队列的单玩家简化版）
- 快照保存：`PredictionSnapshotHistory`（类似 GGPO saved state ring buffer）
- 恢复/重放：`ILocalRollbackSynctestSimulation.Restore` + `Advance`
- 比较：`CharacterSimulationSnapshotComparer`

缺失的是：
- **远端输入源模拟**：没有可配延迟的"假远端输入"
- **输入预测策略**：远端缺失时不知道填什么
- **预测错误检测**：没有 `first_incorrect_frame` 的概念
- **回滚/追帧循环**：没有 `check → rollback → replay → compare` 的编排

本变更补齐这四块，但保持在**纯本地**环境运行，不接网络。

## Goals / Non-Goals
- Goals:
  - 创建可配延迟的假远端输入源，模拟"远端输入延迟 N tick 到达"。
  - 实现输入预测策略：缺失时重复上一帧（第一版），并预留扩展点。
  - 实现 reconciliation 引擎：比对本地预测快照与远端输入重放结果，找到 first incorrect frame，回滚并重放。
  - 让 Play Mode 可以配参数（延迟 tick 数、预测策略）并按键触发延迟同步测试。
  - 保持非侵入：`ILocalRollbackSynctestSimulation` 接口不变，reconciliation 是外层编排。
- Non-Goals:
  - 不接 Fantasy。
  - 不做服务器权威快照校正。
  - 不做多玩家真实输入合并。
  - 不做输入包乱序模拟（第一版假定输入按 tick 顺序到达，只是延迟）。
  - 不修改现有 `LocalRollbackSynctestRunner` 的行为（它仍是理想条件的直连测试）。

## Decisions
- Decision: Reconciliation 作为 `ILocalRollbackSynctestSimulation` 的编排层，而不是修改接口。
  - Reason: 现有接口只关心"给定输入、推进模拟、捕获快照"，reconciliation 的逻辑（延迟队列、预测、比对、回滚）应该在这一层之上。这符合 GGPO `Sync` 层在 `Backend` 之上的分层。

- Decision: 输入预测默认策略：重复上一帧的 `PredictionInputFrame`。
  - Reason: 等同于 GGPO 的默认预测策略（"Predict that the user will do the same thing they did last time"）。动作格斗中按键 pressed 不应重复预测（第一版用简单策略：held 保持，pressed/released 不重复），后续可迭代更精细策略。

- Decision: 第一版假远端输入通过"本地输入 + 延迟队列"模拟，不创建独立的多端进程。
  - Reason: 目标是验证预测/纠错逻辑正确性，不是验证网络传输。用同一进程内的延迟队列可以精确控制到达时机，测试可重现。

- Decision: `LocalLatencyReconciliationRunner` 复用 `LocalRollbackSynctestRunner` 的核心逻辑（restore → replay → compare），但加入"找到 first incorrect frame"的循环。
  - Reason: GGPO `CheckSimulation` 会遍历输入队列找 first incorrect frame，本项目类似：从最早可能出错的 tick 开始，逐 tick 用远端输入重放，比较快照直到发现差异。

- Decision: 第一版不支持输入乱序。远端输入按 tick 顺序到达，只是延迟。
  - Reason: 简化第一版，先验证延迟→预测→回滚这条主链路，乱序可以在后续扩。

## GGPO 模型到本变更的映射

| GGPO 概念 | 本项目对应 |
|---|---|
| `InputQueue` (per player) | `PredictionInputHistory` + 新 `LatencySimulator` |
| `SaveCurrentFrame` | 已在 `PredictionSnapshotHistory.Write` |
| `LoadFrame` | 已在 `ILocalRollbackSynctestSimulation.Restore` |
| `AdvanceFrame` | 已在 `ILocalRollbackSynctestSimulation.Advance` |
| `GetInput` (predict on miss) | 新增 `IPredictionInputStrategy` |
| `CheckSimulation` | 新增 `LocalLatencyReconciliationRunner.CheckSimulation` |
| `AdjustSimulation` | 新增 `LocalLatencyReconciliationRunner.AdjustSimulation` |
| `first_incorrect_frame` | `LocalLatencyReconciliationResult.FirstIncorrectTick` |
| `SyncTestBackend` | `LocalRollbackSynctestDebugRunner` 的理想条件测试 |

## Proposed Runtime Flow

```text
正常运行（本地 + 记录）
  ReadInput
    录本地输入到本地历史
    复制一份放入"远端延迟队列"（模拟网络发送）
  Tick正常流程
    本地输入正常推进角色
  WriteSnapshotAndEvents
    写快照历史

Reconciliation 检查（周期性或按键触发）
  取远端延迟队列中"已到"的输入（模拟远端输入到达）
  从 confirmed tick 开始，逐 tick：
    用远端输入重放一帧
    捕获快照
    与本地预测快照比较
    一致 → 继续
    不一致 → 记录 first incorrect frame，break
  
  如果找到 first incorrect frame：
    Restore(first incorrect frame - 1 的快照)
    从 first incorrect frame 到当前，逐 tick：
      取远端输入（如果已到）或预测输入（如果还没到）
      Advance
    比较最终快照
    输出 reconciliation 结果
```

## Risks / Trade-offs
- Risk: 延迟模拟器的"远端输入"实际上就是本地输入的延迟拷贝，不是独立客户端产生的输入。这意味着两台真实客户端的输入差异（如不同按键）无法被模拟。
  - Mitigation: 第一版接受此限制。真实多端输入差异在多玩家测试或 Fantasy 接入后才有。本变更只验证"相同按键但延迟到达"时的预测/回滚逻辑。

- Risk: 真实 Animancer 表现层可能导致 reconciliation 后快照比较仍有 animation 字段差异。
  - Mitigation: 自动测试用 fake presenter；手动测试中 animation differences 作为诊断输出，不阻塞 reconciliation 结果判断。

- Risk: 回滚后表现层会有视觉跳变。
  - Mitigation: 复用 `PresentationTransformInterpolator` 做插值平滑。第一版只在 debug runner 的可见 correction 模式下观察效果。

## Migration Plan
1. 新增 `LatencySimulator` 和 `IPredictionInputStrategy` 纯数据层，写 EditMode 测试。
2. 新增 `LocalLatencyReconciliationRunner`，写核心链路测试（延迟到达、预测、回滚）。
3. 新增 `LocalLatencyReconciliationDebugRunner` MonoBehaviour，挂到 Sandbox 角色上。
4. 手动在 Sandbox 验证：配 3 tick 延迟 → 移动+Dodge → 触发 reconciliation → 看 Console 输出。
5. 不修改现有 debug runner 的 F6 行为。

## Open Questions
- 第一版 reconciliation 是手动触发（按键）还是每 N 帧自动触发？建议手动触发 + 可选自动间隔。
- 预测策略中按钮 held 是否保持？建议第一版 held 保持，pressed/released 不重复。
- 是否需要支持两端输入完全不同的测试场景？建议第一版不支持，仅模拟同输入延迟到达。
