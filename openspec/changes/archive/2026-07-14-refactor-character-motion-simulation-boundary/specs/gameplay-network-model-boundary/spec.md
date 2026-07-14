## MODIFIED Requirements

### Requirement: Character Runtime 必须通过事实和语义输入连接模型

CharacterPipeline MUST 向模型暴露 canonical input frame、resolved motion、Action lifecycle、window、result、state、cue 和 correction application result 等事实，并只接收 Character/gameplay 语义输入。CharacterPipeline MUST NOT 持有 model packet、model history、endpoint、transport 或服务端 simulation backend。Model adapter MUST 区分用于权威端独立模拟的 canonical input/action request 与用于预测对账的 resolved motion result。

#### Scenario: Owner 完成本 tick 运动

- **WHEN** CharacterMotionStage 完成 LocalSolver 结算
- **THEN** Pipeline MUST 输出 resolved motion fact
- **AND** ServerAuthoritative adapter MAY 保存该 fact 作为 prediction comparison metadata
- **AND** 服务端 canonical motion MUST 从 canonical input/action state 独立生成
- **AND** Pipeline MUST 不直接创建 MotionCommand packet

#### Scenario: 收到动作确认

- **WHEN** ServerAuthoritative model 收到 ActionDecision packet
- **THEN** model adapter MUST 先转换为 Character 已有的 `ActionLifecycleTransition`
- **AND** prediction key、authority tick 和 defense-favor metadata MUST 留在模型内部
- **AND** CharacterNetworkReceiveStage MUST 不保存 model packet payload

#### Scenario: 替换服务端运动模拟实现

- **WHEN** ServerAuthoritativeHybrid 从 Unity authoritative process 改为纯 CSharp KCC server
- **THEN** CharacterPipeline facts 和语义输入合同 MUST 保持不变
- **AND** backend 选择 MUST 位于 model/server composition root
- **AND** Graph、Timeline、Action 和 client CharacterPipeline MUST 不增加 backend switch
