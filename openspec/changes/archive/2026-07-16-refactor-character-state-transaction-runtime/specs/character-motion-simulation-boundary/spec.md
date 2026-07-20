## MODIFIED Requirements

### Requirement: 运动语义、世界约束执行和逻辑位姿必须分层

Compiled motion operations MUST在Program Evaluate Pass中将portable contribution提交到当前Evaluation workspace，由唯一Target MotionAccumulator产生当前Step的CharacterMotionRequest与WorldRequest；这些数据 MUST只存在于Evaluation Frame和PendingCharacterEvaluation，不得进入committed Character State、Snapshot或StateHash。正式WorldSolve Pass MUST汇总当前SimulationStep全部Actor request并调用一次`ICharacterWorldSolver.ResolveBatch`；Program Finalize Pass MUST使用精确匹配的WorldSolverResult更新Character/World working state并产生唯一MotionResult。Graph、Timeline、Action、Session Source、其它Pipeline Pass和Presentation MUST不直接写Transform或调用concrete solver。

#### Scenario: Timeline MotionCurve 提交位移

- **WHEN** compiled Timeline在当前Step产生Action motion contribution
- **THEN** Evaluate Pass MUST在当前transaction frame生成portable world request
- **AND** request MUST与同Step其它Actor request一起进入唯一ResolveBatch
- **AND** Finalize Pass MUST记录Solver actual result
- **AND** committed Character State MUST不保存pending motion bytes

#### Scenario: Replay 当前 Step

- **WHEN** Network Model恢复committed Snapshot并重演一个SimulationStep
- **THEN** Program Evaluate MUST从恢复后的typed State与canonical input重新生成Motion contribution和WorldRequest
- **AND** MUST不从Snapshot恢复旧MotionAccumulator或PendingWorldRequest

