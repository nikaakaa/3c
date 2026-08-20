## MODIFIED Requirements

### Requirement: WorldSimulationState 必须唯一拥有逻辑位姿读写

WorldSimulationState中的 BodyState MUST替代 Logic Pose Port/Transform作为 Core逻辑位姿真值。每个有可见角色的Unity组合 MUST显式提供最外层LogicRoot作为当前committed或model-selected Body的单向场景投影；LogicRoot MUST不成为Gameplay、Solver request、Snapshot、Hash、History、Perception或Presentation的反向数据源。Unity Host MUST只在WorldSolver binding或成功outer/model egress commit边界把最终Body投影到LogicRoot，MUST不让Transform、Character stage、Session Source、其它Pipeline Pass或Presentation保存第二份可反写逻辑真值。一个成功事务包含多个Replay/Current step或多个selected Body sample时，LogicRoot MUST只采用该事务最终当前分支的最后一个Body，不得依次显示历史step。失败、Abort或没有Body sample的事务 MUST不留下新的LogicRoot姿态。

#### Scenario: 应用 Solver Result

- **WHEN** WorldSolve Pass返回Actor的actual body result且outer transaction成功
- **THEN** Execution Backend MUST在最终Commit前更新唯一WorldSimulationState
- **AND** Unity Body commit projection MUST使LogicRoot精确对应最终committed Body
- **AND** Presentation MUST只从committed body sample驱动VisualRoot

#### Scenario: Rollback事务包含历史重放

- **WHEN** 一个成功Rollback outer transaction先执行多个Replay step再提交Current step
- **THEN** WorldState与History MUST按正式事务语义提交最终连续分支
- **AND** LogicRoot MUST只投影最终Current Body
- **AND** LogicRoot MUST不逐Replay step倒退或前进

#### Scenario: Body事务失败

- **WHEN** Solver、Finalize、Egress或Commit失败
- **THEN** WorldState MUST不发布部分结果
- **AND** LogicRoot MUST保持上一成功提交姿态
