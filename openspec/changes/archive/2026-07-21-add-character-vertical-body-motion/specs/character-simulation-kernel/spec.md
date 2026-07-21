## ADDED Requirements

### Requirement: World Body垂直动力必须原子参与Simulation状态

Float32与Fixed `WorldBodyState` MUST独立保存VerticalVelocity，且 MUST保持actual Velocity为Solver applied displacement速度。Body Motion integration plan MUST只存在Evaluate到WorldSolve/Finalize的同Step transaction内；成功Finalize后只有committed VerticalVelocity进入NextWorldState。WorldState codec、Snapshot、Hash、restore、equality与WorldSolve request/result hash MUST覆盖VerticalVelocity；Abort MUST丢弃pending plan且不修改before state。系统 MUST不从actual Velocity.Y或Grounded推导缺失状态。

#### Scenario: WorldSolve后续Actor失败

- **WHEN** 一个Actor已完成Body Motion Prepare但同一outer transaction的后续Actor或Pass失败
- **THEN** 全部pending integration plan MUST被丢弃
- **AND** committed WorldBodyState及VerticalVelocity MUST保持修改前值

#### Scenario: Snapshot恢复空中Actor

- **WHEN** Session原子恢复包含Airborne Actor的World Snapshot
- **THEN** Position、actual Velocity、VerticalVelocity、Grounded与Collision MUST同时恢复
- **AND** 下一次Evaluate MUST只读取恢复后的状态执行Prepare

