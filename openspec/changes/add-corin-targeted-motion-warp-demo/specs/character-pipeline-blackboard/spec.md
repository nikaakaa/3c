## ADDED Requirements

### Requirement: InputDerived Blackboard declaration 必须显式绑定 portable input

`SyncPolicy.InputDerived` 的 Blackboard declaration MUST保存非空稳定 InputValueId，并在编译期验证 declaration value kind 与 Program input kind 精确一致。InputDerived declaration MUST使用 Character scope 与 Spawn lifetime，MUST NOT使用 PresentationOnly authority。非 InputDerived declaration MUST NOT残留 InputValueId。

Compiler MUST把 input-to-state binding 编入 Program Layout。每个 Actor Evaluate MUST在 Graph control 之前，通过当前 Character State Transaction 将本帧 input value 覆盖到目标 Blackboard slot；Evaluate 失败时该写入 MUST随同一事务回滚。系统 MUST NOT提供 CharacterPipelineHost、Scene 组件或 Network Model 直接写 CharacterState Blackboard 的第二入口。

#### Scenario: 目标输入投影到黑板

- **WHEN** 当前 CharacterSimulationInput 包含绑定 InputId 的 `ActionTargetSnapshot`
- **THEN** Program MUST在执行 `CanActivateAction` 前写入对应 Blackboard slot
- **AND** Condition 与 Activate operation MUST读取同一事务中的值

#### Scenario: 当前帧目标变为 None

- **WHEN** 当前 input value 明确为无目标
- **THEN** InputDerived projection MUST覆盖上一 Tick 的目标值
- **AND** Blackboard MUST NOT保留 stale target snapshot

#### Scenario: declaration 与 input 类型不匹配

- **WHEN** InputDerived declaration 的 value kind 与绑定 input kind 不一致
- **THEN** Compiler 或 composition MUST拒绝该 Program
- **AND** Runtime MUST NOT执行字符串转换或默认值 fallback

#### Scenario: 非 InputDerived declaration 保存 InputId

- **WHEN** 作者将 declaration 的 SyncPolicy 改为其它策略
- **THEN** authoring validation MUST要求清除 InputValueId
- **AND** artifact 发布 MUST NOT保留失效 binding
