## ADDED Requirements
### Requirement: FullBody 抢占 Locomotion transient 的帧事实
系统 MUST 在 Character frame pipeline 内提供纯数据 Locomotion preemption fact，用于表达 FullBody Action 已抢占当前 Locomotion transient motion source。该 fact MUST 由 submitter、plan、output 或等价 frame data contract 传递，不得通过 pipeline 核心硬编码具体 Action 或具体 Locomotion 状态完成状态切换。

#### Scenario: FullBody claim 抢占 TurnBack 时产出事实
- **GIVEN** Locomotion submitter 已提交 `Locomotion.TurnBack` 候选输出
- **AND** FullBody Action submitter 在同一帧开始 `Action.Dodge`
- **AND** `Action.Dodge` 的 full-body claim 被接受
- **WHEN** Character frame pipeline 生成本帧 plan/output
- **THEN** plan/output MUST 继续压制 Locomotion motion output
- **AND** plan/output MUST 携带一次性 Locomotion preemption fact
- **AND** preemption fact MUST 记录 source locomotion state、source action id 和 source step
- **AND** pipeline 本体 MUST NOT 直接切换 Locomotion state

#### Scenario: 非 transient Locomotion 不产生抢占事实
- **GIVEN** Locomotion submitter 当前处于 `Locomotion.Idle`、`Locomotion.MoveLoop` 或等价非 transient motion source
- **AND** FullBody Action submitter 开始 full-body action
- **WHEN** Character frame pipeline 生成本帧 plan/output
- **THEN** plan/output MAY 压制 Locomotion motion 或 animation output
- **AND** plan/output MUST NOT 产生 TurnBack preemption fact

#### Scenario: Pipeline 不认识 Dodge 与 TurnBack 细节
- **WHEN** 检查 `CharacterFramePipeline` 核心 phase 顺序代码
- **THEN** pipeline MUST 只调用 submitter、composer、applier 和 runtime port
- **AND** pipeline MUST NOT 通过 `Action.Dodge` 字符串判断是否抢占
- **AND** pipeline MUST NOT 通过 `Locomotion.TurnBack` 字符串执行状态切换
