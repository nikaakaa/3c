## MODIFIED Requirements

### Requirement: Blackboard declaration 必须显式声明 fact projection

Pipeline Blackboard declaration MAY 保存一个显式 fact projection。ActionWindow projection MUST 只允许 Bool、Frame scope、Frame lifetime 和 SyncFact policy，并 MUST 保存稳定 WindowType、WindowId 与 Digest。Projection MUST NOT 保存完整网络 policy；ActionWindowSample 的 effective policy MUST 由当前 Network Model adapter 使用 ActionInstance 对应的稳定 ActionId 从 model profile 解析。ActionProfile、Blackboard declaration、Graph 与 Timeline MUST NOT 复制该策略。非法 projection MUST 由 authoring validator 和 runtime 拒绝，不得 fallback 为普通变量或默认 Window。

#### Scenario: ActionWindow-bound Frame variable

- **WHEN** active Decision TreeClip 在当前 Tick 写入合法 ActionWindow-bound variable=true
- **AND** 写入 provenance 包含有效 Action Context
- **THEN** runtime MUST 记录一个本帧 projection candidate
- **AND** RootTree 决策后的统一 projection MUST 最多生成一个对应 ActionWindowSample
- **AND** 后续网络处理 MUST 从当前 Network Model profile 解析 effective policy

#### Scenario: 缺失 Action Context

- **WHEN** ActionWindow-bound variable 的写入 provenance 没有有效 Action Context
- **THEN** validator 或 runtime MUST 报告错误
- **AND** 系统 MUST NOT 使用 ambient current action、最后 active action 或默认 ActionInstance 补齐

#### Scenario: 同一变量被不同 ActionInstance 写入

- **WHEN** 同一 declaration 在同一 Tick 由两个不同 ActionInstance provenance 写入 true
- **THEN** projection MUST 按 ActionInstance 保留两个独立 candidate
- **AND** 最终单一 Blackboard Bool value MUST NOT 导致任一 ActionInstance 身份丢失
