## ADDED Requirements

### Requirement: 作者 UI 必须使用 Action Context 口径

系统 MUST 在作者可见 UI 中使用 `Action Context` 表达动作期间输出的归属输入/输出。作者主要编辑界面 MUST NOT 使用 `Action Handle Slot`、`ActionInstanceHandle` 或等价内部句柄词作为主要概念。内部实现 MAY 使用 slot、handle 或引用，但必须被封装在 Action Context 口径下。

#### Scenario: 配置动作激活节点

- **WHEN** 作者选中 `Activate Action Instance` 或等价动作激活节点
- **THEN** Inspector MUST 显示 `Output Action Context` 或等价业务字段
- **AND** MUST 通过正式 `ActionProfile` 或等价动作定义资产确定动作身份
- **AND** MUST NOT 要求作者手敲 `attack.handle`、`ActionId` 或等价字符串 key

#### Scenario: 配置 Timeline 节点

- **WHEN** 作者希望某个 Timeline 输出归属到一次动作
- **THEN** Timeline 节点 MUST 暴露 `Action Context` 输入或等价引用
- **AND** 空 Action Context MUST 表示普通 Timeline，不自动继承当前 active action

### Requirement: 作者必须能显式配置动作退出语义

系统 MUST 让作者在动作流程离开点配置退出语义，而不是只配置普通 graph exit。至少 MUST 支持 `Complete`、`Cancel`、`Interrupt` 和 `Abort` 的作者入口；`Reject` 和 `Correct` MAY 来自网络 decision，但 Debug 和配置说明 MUST 能显示它们。

#### Scenario: 状态机正常结束攻击

- **WHEN** 作者配置 `Attack.Recovery -> Locomotion`
- **THEN** 该离开边或等价生命周期节点 MUST 能配置为 `Complete`
- **AND** 运行时 MUST 提交对应 `ActionLifecycleTransition(Complete)`

#### Scenario: 状态机取消攻击

- **WHEN** 作者配置 `Attack.Any -> Dodge`
- **THEN** 该离开边或等价生命周期节点 MUST 能配置为 `Cancel`
- **AND** reason MUST 能配置为 `DodgeCancel` 或等价业务原因

#### Scenario: 外部受击打断

- **WHEN** 动作期间收到受击或硬控事件
- **THEN** 作者 MUST 能通过 Graph、状态机边或外部 lifecycle 节点表达 `Interrupt`
- **AND** 被打断动作的 Action Context MUST 在 transition 后失效

### Requirement: ActionScope 若引入必须只是作者组织层

如果系统引入 `ActionScope`、`ActionBody` 或等价编辑节点，它 MUST 只作为作者组织和默认 Action Context 继承工具。它 MUST NOT 让 subtree、StateNode、Timeline asset 或节点 membership 成为网络同步真相。

#### Scenario: Scope 内默认继承 Context

- **WHEN** 作者在 `ActionScope(Attack.Light.01)` 内放置 Timeline、Window、Cue 或 Result 节点
- **THEN** 这些节点 MAY 默认使用 scope 提供的 Action Context
- **AND** 最终 runtime 输出仍 MUST 携带 `ActionInstanceId` 和 lifecycle transition，而不是 subtree id

#### Scenario: Scope 离开

- **WHEN** `ActionScope` 的子流程正常完成、被取消或被打断
- **THEN** Scope MUST 将离开原因翻译为明确 `ActionLifecycleTransition`
- **AND** MUST NOT 靠 scope 停止 tick 来隐式销毁动作事务
