## MODIFIED Requirements
### Requirement: 移动 TurnBack 逻辑状态
系统 MUST 将移动反向急转表达为 `FullBody/Locomotion/TurnBack` 逻辑状态。TurnBack 触发事实 MUST 由统一 Locomotion 决策管线在状态机 tick 前派生：使用当前移动意图、当前世界移动方向和人物当前平面朝向捕获 TurnBack intent，而不是在 transition evaluator 中临时解析空间关系，也不是使用上一有效移动方向作为触发来源。TurnBack MUST 播放 `Locomotion.Turn.Back`；TurnBack 结束后 MUST 根据当前输入回到 MoveLoop 或 Idle。

#### Scenario: MoveLoop 反向输入进入 TurnBack
- **GIVEN** 角色处于 `FullBody/Locomotion/MoveLoop`
- **AND** 统一 Locomotion 决策管线已经提供有效 TurnBack intent
- **WHEN** 统一状态机评估 `MoveTurnBackRequested`
- **THEN** 状态机 MUST 转入 `FullBody/Locomotion/TurnBack`
- **AND** 当前 locomotion phase MUST 为 `TurnBack`

#### Scenario: MoveStart 和 MoveStop 可消费 TurnBack intent
- **GIVEN** 角色处于 `FullBody/Locomotion/MoveStart` 或 `FullBody/Locomotion/MoveStop`
- **AND** 统一 Locomotion 决策管线已经提供有效 TurnBack intent
- **WHEN** 统一状态机评估 `MoveTurnBackRequested`
- **THEN** 状态机 MUST 转入 `FullBody/Locomotion/TurnBack`

#### Scenario: Idle 不直接消费 TurnBack intent
- **GIVEN** 角色处于 `FullBody/Locomotion/Idle`
- **AND** 统一 Locomotion 决策管线已经提供有效 TurnBack intent
- **WHEN** 统一状态机评估本帧 transition
- **THEN** 默认状态机 MUST NOT 直接转入 `FullBody/Locomotion/TurnBack`
- **AND** 角色 MAY 先按普通移动规则进入 `MoveStart`

#### Scenario: TurnBack 动画结束后退出
- **GIVEN** 角色处于 `FullBody/Locomotion/TurnBack`
- **WHEN** locomotion animation facts 显示 `Locomotion.Turn.Back` 已结束
- **AND** 当前仍有移动输入
- **THEN** 状态机 MUST 转入 `FullBody/Locomotion/MoveLoop`
- **WHEN** locomotion animation facts 显示 `Locomotion.Turn.Back` 已结束
- **AND** 当前没有移动输入
- **THEN** 状态机 MUST 转入 `FullBody/Locomotion/Idle`

#### Scenario: TurnBack 触发不依赖上一移动方向
- **GIVEN** runtime blackboard 中上一有效移动方向与当前输入方向不反向
- **AND** 人物当前平面朝向与当前世界移动输入方向的夹角达到 TurnBack 阈值
- **WHEN** 统一 Locomotion 决策管线派生 TurnBack intent
- **THEN** 状态机 MUST 能根据该 intent 进入 `FullBody/Locomotion/TurnBack`

#### Scenario: TurnBack intent 覆盖短空输入
- **GIVEN** step N 统一 Locomotion 决策管线捕获到有效 TurnBack intent
- **AND** step N+1 因 W/S 切换出现短暂无移动输入
- **WHEN** 当前 step 仍在 TurnBack intent 的短窗口内
- **THEN** 状态机 MUST 仍能消费该 intent
- **AND** 该 intent 过期或进入 TurnBack 后 MUST 被清理
