# character-pipeline-blackboard Specification

## MODIFIED Requirements

### Requirement: Runtime value 必须按 declaration 与 scope owner 共同寻址

Compiler MUST把 Blackboard declaration identity、type、scope、lifetime 和 owner relation 编译为 Program declaration index 与 state layout。SimulationState MUST使用 declaration index 加稳定 Character/Graph/State/ActionInstance/Frame owner generation 形成 runtime address；MUST不使用裸 BlackboardKey、Unity runtime object、随机 Guid 或 dictionary iteration 作为 canonical identity。Capture/Restore MUST完整保存 value、owner generation 和 write provenance。

#### Scenario: 并行状态机退出状态

- **WHEN** Attack1 State scope 退出而 RunLoop scope 仍 active
- **THEN** compiled cleanup operation MUST只清理 Attack1 owner generation 的 state slots
- **AND** RunLoop slots MUST保持不变

#### Scenario: rollback 恢复 Blackboard

- **WHEN** Driver 恢复某个 SimulationWorldSnapshot
- **THEN** Character、Graph、State、ActionInstance 和 Frame values/provenance MUST恢复
- **AND** MUST不从 authoring ExposedProperty 或旧 runtime dictionary 重新推断

### Requirement: Decision TreeClip 必须通过声明式 Frame Blackboard 输出决策

Decision TreeClip MUST编译为固定 Timeline segment 与 Blackboard write operation，并只写 Program 中声明的 Frame scope slot。SimulationKernel MUST在当前 SimulationTick 开始重置 Frame slots，Decision phase 写入，Graph/Transition phase读取，fact projection 使用同一 write provenance，并在 Tick 结束清理。Runtime MUST不创建 TimelineRunningTree authoring clone 或第二套 decision cache。

#### Scenario: Dodge 恢复段开放移动取消

- **WHEN** compiled Dodge Decision segment 在当前 Tick active
- **THEN** Frame slot MUST在同 Tick Transition 求值前写入
- **AND** replay 同一 Tick MUST产生相同 write provenance 和 projection

