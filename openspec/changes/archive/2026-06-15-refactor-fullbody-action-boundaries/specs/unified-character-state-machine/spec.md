## ADDED Requirements
### Requirement: Runner 状态 payload 通用化
统一状态机 runner MUST 只维护状态图推进所需的通用可恢复事实。Action locked direction、TurnBack locked direction、TurnBack entry basis forward 或后续 Attack/Jump/HitReact payload MUST 通过通用 state payload、状态输出或等价纯数据 carrier 表达，runner MUST NOT 以专用字段或 `CharacterStateIds.*` 特判保存具体业务状态 payload。

#### Scenario: TurnBack payload 不在 runner 专用字段中
- **GIVEN** accepted TurnBack request 进入 `FullBody/Locomotion/TurnBack`
- **WHEN** runner 应用 transition
- **THEN** TurnBack locked direction 和 entry basis forward MUST 写入通用 state payload 或等价输出数据
- **AND** runner MUST NOT 通过 `turnBackWorldDirection`、`turnBackEntryBasisForward` 专用字段保存
- **AND** 行为输出 MUST 仍能使用进入时锁定方向

#### Scenario: Action payload 不在 runner 专用字段中
- **GIVEN** accepted Dodge request 进入 `FullBody/Action/Dodge`
- **WHEN** runner 应用 transition
- **THEN** action locked direction 和 variant MUST 通过通用 state payload 或等价输出数据提供给 state output
- **AND** runner MUST NOT 通过 action 专用 direction 字段保存
- **AND** rollback restore 后 Dodge 方向 MUST 保持确定

#### Scenario: 新状态 payload 不修改 runner 字段
- **WHEN** 后续新增 Attack、Jump 或 HitReact 状态 payload
- **THEN** 新 payload MUST 通过通用 payload carrier 接入
- **AND** MUST NOT 要求在 runner 中新增 `attackPayload`、`jumpPayload`、`hitReactPayload` 或等价专用字段

### Requirement: Snapshot 与 FullBody 解释分离
`CharacterStateMachineSnapshot` MUST 只表达统一状态机身份和恢复诊断事实，包括 active state、active path、state time、variant、pending transition 和 tags。FullBody owner、ActionState、LocomotionPhase、IsAction、IsLocomotion 或等价业务解释 MUST 由外围 FullBody state view/adapter 从 snapshot 和状态定义派生，不能作为 snapshot 的核心职责。

#### Scenario: Snapshot 保持纯状态机身份
- **WHEN** 捕获状态机 snapshot
- **THEN** snapshot MUST 包含 active state、active path、state time、variant、pending transition 和 tags
- **AND** MUST NOT 暴露 FullBody owner 作为核心字段
- **AND** MUST NOT 暴露 Locomotion phase 或 ActionState 作为核心字段

#### Scenario: FullBody view 提供兼容解释
- **WHEN** FullBody pipeline、diagnostics、Locomotion adapter 或 Action facts 需要 owner、Locomotion phase 或 ActionState
- **THEN** 它们 MUST 通过 FullBody state view/adapter 或等价解释入口读取
- **AND** 该 view MUST 从 snapshot 和状态定义派生
- **AND** view MUST NOT 成为第二状态权威

#### Scenario: Snapshot 改名不破坏业务解释
- **WHEN** 状态 path 命名或层级结构调整但状态模块语义不变
- **THEN** FullBody 解释 MUST 优先使用状态定义、模块或受控 tag
- **AND** MUST NOT 仅依赖 `StartsWith("FullBody/Action")` 或最后 path segment 推导业务行为
