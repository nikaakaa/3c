## ADDED Requirements
### Requirement: Dodge transition duration 单一真值源
`CharacterStateMachineDefinition.CreateDefault()` 中的 `Dodge → MoveLoop` 和 `Dodge → Idle` transition 的 `StateElapsedAtLeast` 条件 MUST 从 `DodgeActionConfig.Default.DirectionalDuration` 获取其时间值，SHALL NOT 使用本地硬编码 `const` 作为独立 duration 源。

#### Scenario: 默认状态机 Dodge transition 与 config 同步
- **GIVEN** `DodgeActionConfig.Default.DirectionalDuration` 为 0.35s
- **WHEN** 系统调用 `CreateDefault()` 构建状态机定义
- **THEN** `Dodge → MoveLoop` transition 的 `StateElapsedAtLeast` condition MUST 使用 `DirectionalDuration` 值
- **AND** `Dodge → Idle` transition 的 `StateElapsedAtLeast` condition MUST 使用同样值
- **AND** 状态机定义中 SHALL NOT 存在独立的 `const float DefaultDodgeDuration = 0.35f`

#### Scenario: 修改 config 默认值后 transition 跟随
- **GIVEN** `DodgeActionConfig.Default` 的 `DirectionalDuration` 被改为 0.5s
- **WHEN** 系统调用 `CreateDefault()` 构建状态机定义
- **THEN** `Dodge → MoveLoop` transition 的 `StateElapsedAtLeast` MUST 为 0.5s
- **AND** `Dodge → Idle` transition 的 `StateElapsedAtLeast` MUST 为 0.5s

### Requirement: Dodge→Dodge 连闪时间下限保护
`Dodge → Dodge` transition MUST 包含 `StateElapsedAtLeast(DodgeActionConfig.Default.DirectionalDuration)` 条件，确保前一次 Dodge 的核心位移窗口结束后才允许下一次 Dodge 重新进入。该条件 MUST 与 `HasInputRequest(InputRequestKind.Dodge)` 条件以 AND 语义共同生效。

#### Scenario: 同帧双请求被状态机层拦截
- **GIVEN** 当前状态为 `FullBody/Action/Dodge`，stateTime 为 0s
- **AND** 存在未消费的 Dodge 输入请求
- **WHEN** 状态机 runner 求值 `Dodge → Dodge` transition
- **THEN** transition MUST NOT 满足条件
- **AND** 状态机 MUST 保持在 `Dodge` 状态

#### Scenario: 位移窗口结束后允许连闪
- **GIVEN** 当前状态为 `FullBody/Action/Dodge`，stateTime >= `DirectionalDuration`（如 0.35s）
- **AND** 存在未消费的 Dodge 输入请求
- **WHEN** 状态机 runner 求值 transition
- **THEN** `Dodge → Dodge` transition 可以成立
- **AND** 状态机可以重新进入 `Dodge`

#### Scenario: 连闪时间下限与退出条件一致
- **WHEN** 比较 `Dodge → Dodge` 和 `Dodge → MoveLoop` transition 的时间下限
- **THEN** 两者的 `StateElapsedAtLeast` 值 MUST 相等
- **AND** MUST 均来自 `DodgeActionConfig.Default.DirectionalDuration`
