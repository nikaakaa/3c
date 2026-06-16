## ADDED Requirements
### Requirement: TurnBack Intent 保持候选事实边界
基础移动系统 MUST 可以继续计算 `LocomotionTurnBackIntent` 来表达 `MoveStart` 或 `MoveLoop` 反向输入候选，但该 intent MUST 只作为状态请求仲裁入口的输入。基础移动系统 MUST NOT 因 intent 本身直接切换到 TurnBack、播放 TurnBack 动画或提交 TurnBack motion。

#### Scenario: Locomotion 只产出候选 intent
- **GIVEN** 当前基础移动 phase 为 MoveStart 或 MoveLoop
- **AND** 当前 gait 为 Run
- **AND** 输入方向与角色朝向满足反向阈值
- **WHEN** 基础移动系统派生 locomotion decision facts
- **THEN** 它 MAY 产出有效 `LocomotionTurnBackIntent`
- **AND** MUST NOT 在该阶段直接切换逻辑状态

#### Scenario: intent 不直接驱动运动输出
- **GIVEN** locomotion facts 中存在有效 `LocomotionTurnBackIntent`
- **AND** 统一状态机当前状态尚未进入 `FullBody/Locomotion/TurnBack`
- **WHEN** 基础移动系统构建本帧运动
- **THEN** 系统 MUST NOT 采样 TurnBack baked motion
- **AND** MUST NOT 因 intent 本身锁定普通输入旋转或平面位移

#### Scenario: TurnBack 状态后才消费窗口运动
- **GIVEN** 统一状态机已经通过 accepted TurnBack request fact 进入 `FullBody/Locomotion/TurnBack`
- **AND** 当前 timeline facts 表示 motion window active
- **WHEN** 基础移动系统构建本帧运动
- **THEN** 系统 MUST 通过 TurnBack motion policy 采样 configured baked motion
- **AND** input lock 行为 MUST 由 timeline facts 和 TurnBack motion policy 决定
