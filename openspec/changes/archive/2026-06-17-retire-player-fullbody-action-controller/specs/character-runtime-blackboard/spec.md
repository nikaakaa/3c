## MODIFIED Requirements
### Requirement: 状态机读取黑板快照
系统 SHALL 允许统一状态机 context 读取黑板 snapshot 中的纯数据 facts，用于后续方向起步、脚步相位、转身和转角等条件判断。状态机 runner 自身 SHALL NOT 成为黑板字段维护器。context 组装 MUST 来自角色级 runtime、状态机 runtime 或 Locomotion/FullBody Action 窄模块，而不是旧 FullBody controller。

#### Scenario: Context 承载黑板 snapshot
- **WHEN** `CharacterFrameRuntimeController`、`CharacterStateMachineRuntime`、Locomotion runtime 或 FullBody Action runtime 组装 `CharacterStateMachineContext`
- **THEN** context MAY 携带黑板 snapshot 或等价只读 facts view
- **AND** transition evaluator MUST 只读取该只读 facts view
- **AND** evaluator MUST NOT 读取黑板可变实例
- **AND** context 组装 MUST NOT 依赖 `PlayerFullBodyActionController`

#### Scenario: Runner 不维护黑板
- **WHEN** `CharacterStateMachineRunner` tick 一帧
- **THEN** runner MUST NOT 直接写入黑板
- **AND** runner MAY 在输出 frame 中表达需要调用方应用的纯数据结果
- **AND** 调用方 adapter 负责把允许的结果写入对应 facts
