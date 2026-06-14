## ADDED Requirements
### Requirement: Reconciliation 区分预测错误和 Replay 不确定
系统 MUST 在本地 latency/reconciliation 工具中区分两类结果：预测输入与确认输入不同导致的合法 correction，以及相同 resolved input 重放仍不一致导致的 replay nondeterminism。后者 MUST 被视为工具或状态快照失败，不能被归类为普通网络预测误差。

#### Scenario: 预测错误但重放收敛
- **GIVEN** tick M 的预测输入与确认输入不同
- **AND** 从 tick M-1 恢复后使用 resolved input 重放到 current tick 可以逐 tick 一致
- **WHEN** reconciliation 完成
- **THEN** 结果 MUST 标记为 prediction correction
- **AND** MUST 输出 first incorrect tick、restore tick 和 replay frame count
- **AND** strict replay mismatch MUST 为空

#### Scenario: 相同输入重放仍分叉
- **GIVEN** reconciliation 已解析出 tick M..current 的 resolved input
- **AND** 从 tick M-1 恢复后使用同一段 resolved input 重放时出现 first mismatch
- **WHEN** reconciliation 生成结果
- **THEN** 结果 MUST 标记为 replay nondeterminism
- **AND** MUST 输出 first mismatch stage、tick 和 differences
- **AND** MUST NOT 把该失败归类为普通 prediction correction

#### Scenario: 无预测错误
- **GIVEN** confirmed tick 到 current tick 的输入都已确认且与本地历史一致
- **WHEN** reconciliation 检查区间
- **THEN** 结果 MUST 标记为 no correction required
- **AND** MUST NOT 执行 rollback adjust

### Requirement: Reconciliation 逐 Tick 严格验收
系统 MUST 在 reconciliation 的 check 和 adjust 阶段使用严格逐 tick 语义或等价比较。最终快照一致但中间 tick 分叉时，reconciliation MUST 返回失败诊断。

#### Scenario: Adjust 后最终收敛但中间分叉
- **GIVEN** reconciliation 从 restore tick 重放到 current tick
- **AND** 某个中间 tick 出现 first mismatch
- **AND** current tick 最终快照一致
- **WHEN** reconciliation 返回结果
- **THEN** 结果 MUST 失败或标记 replay nondeterminism
- **AND** MUST 保留 first mismatch 诊断

#### Scenario: Adjust 全区间一致
- **GIVEN** reconciliation 从 restore tick 重放到 current tick
- **AND** 每个可比较 tick 都一致
- **AND** current tick 最终快照一致
- **WHEN** reconciliation 返回结果
- **THEN** 结果 MUST 通过

### Requirement: Reconciliation 输入差异诊断
系统 MUST 输出 prediction input 和 confirmed/resolved input 的字段级差异，使开发者能判断 correction 是否来自 Move、Look、Run、Dodge、Attack、Jump、Interact、camera basis 或 tick 标记差异。

#### Scenario: 输出预测和确认输入差异
- **GIVEN** tick M 的预测输入与确认输入不同
- **WHEN** reconciliation 检测到 first incorrect tick
- **THEN** 日志 MUST 输出 tick M
- **AND** MUST 输出预测输入摘要
- **AND** MUST 输出确认或 resolved 输入摘要
- **AND** MUST 输出不同字段名

#### Scenario: 缺少确认输入时标记预测
- **GIVEN** tick N 的确认输入尚未到达
- **WHEN** reconciliation 需要推进 tick N
- **THEN** 结果或日志 MUST 标明该 tick 使用预测输入
- **AND** MUST NOT 将预测帧写回原始输入历史

### Requirement: Latency Debug Runner 安全探针一致
系统 MUST 保持 latency debug runner 的安全探针语义。未显式启用应用结果到场景时，reconciliation 执行结束后 MUST 恢复触发前现场状态；启用可见 correction 时，应用结果 MUST 通过正式配置和表现插值路径完成。

#### Scenario: 安全探针恢复现场
- **GIVEN** `LocalLatencyReconciliationDebugRunner` 未启用应用结果
- **WHEN** 用户触发本地 latency reconciliation
- **THEN** 工具 MAY 临时 restore 和 replay
- **AND** 结束后 MUST 恢复触发前最新现场快照
- **AND** MUST 释放临时 camera basis override

#### Scenario: 可见校正不走 fallback
- **GIVEN** 用户启用可见 correction
- **WHEN** reconciliation 需要应用校正结果
- **THEN** 校正 MUST 通过正式 motion/presentation 配置执行
- **AND** MUST NOT 新增未审批的 fallback 角色移动路径
