## MODIFIED Requirements

### Requirement: Corin 状态切换动画混合必须配置在 Transition 边

Corin Locomotion、外层 Action 与内层 combo 的 Transition edge MUST 显式配置 HandoffRole。真正主导可见 Base owner 变化的 edge MUST 使用 Driver，并保存 strategy、duration 与 curve；结构 edge MUST 使用 None。Timeline clip ease 只表达同一 Timeline 内混合，MUST NOT冒充跨 State handoff。

#### Scenario: 可见 Locomotion 切换

- **WHEN** WalkLoop -> RunLoop、RunStart -> RunLoop 或其它可见 Locomotion owner 变化
- **THEN** edge MUST 使用 Driver
- **AND** 对应 Timeline MUST 继续输出正式 contributions

#### Scenario: 结构 Locomotion 切换

- **WHEN** edge 进入 ActionOverride 或无动画 WalkEnd
- **THEN** edge MUST 使用 None
- **AND** edge MUST 不保存有效 blend strategy 数据

#### Scenario: Combo

- **WHEN** Attack1 与 Attack2 互相切换
- **THEN** combo edge MUST 使用 Driver
- **AND** strategy MUST 配置在该 edge

## ADDED Requirements

### Requirement: Corin 必须由 Pipeline 解析 Base Previous 与 Desired

Corin MUST 保持单一 Base layer，并显式配置 OutputPolicy=RequireOutput。StateMachine edge 只声明 None/Driver；Arbitrator MUST 从当前 Base FinalOutput、完整 Registry DesiredCandidate 与 ordered transition records生成每帧唯一 Base LayerPlan。系统 MUST NOT新增隐藏 layer、隐藏 Timeline、默认 Idle 或 endpoint HandoffMode。

#### Scenario: 开始 Dodge

- **WHEN** Locomotion -> ActionOverride 为 None
- **AND** None -> DodgeBack/DodgeForward 为 Driver
- **THEN** Previous Base MUST 是当前 Locomotion output
- **AND** Desired Base MUST 是 Dodge contribution
- **AND** Arbitrator MUST 提交一个 Locomotion -> Dodge HandoffPlan

#### Scenario: Dodge 后继续移动

- **WHEN** Dodge -> None 为 Driver
- **AND** ActionOverride -> RunLoop 为 None
- **THEN** Previous Base MUST 是 Dodge
- **AND** Desired Base MUST 是 RunLoop
- **AND** Arbitrator MUST 提交一个 Dodge -> RunLoop HandoffPlan

#### Scenario: Dodge 后停止

- **WHEN** Dodge -> None 为 Driver
- **AND** ActionOverride -> RunEnd 为 None
- **THEN** Desired Base MUST 是 RunEnd
- **AND** target None MUST NOT被解释为 Empty

#### Scenario: 首次攻击

- **WHEN** None -> Attack 为 Driver
- **AND** inner Attack1 ready/sample 到达
- **THEN** Previous Base MUST 是当前 Locomotion output
- **AND** Desired Base MUST 来自 Attack1
- **AND** 外层 Attack 结构 owner MUST NOT成为 endpoint

#### Scenario: 攻击 leaf 完成

- **WHEN** Attack1/Attack2 -> inner Exit 为 None
- **AND** outer Attack -> None 为 Driver
- **THEN** inner Exit MUST 不创建 handoff
- **AND** outer Driver MUST 从最后 Attack FinalOutput 接回 Locomotion DesiredCandidate

#### Scenario: 无动画 WalkEnd

- **WHEN** 可见 Locomotion -> WalkEnd 为 None
- **THEN** RequireOutput MUST 保持上一合法 Base output
- **AND** WalkEnd -> 后续可见 Locomotion/Idle records MUST 在 target candidate 出现时进入同一 causal commit

#### Scenario: 快速 Locomotion 连续激活

- **WHEN** RunLoop#4 -> RunEnd#5 -> MovingTurn#6 -> RunEnd#7 在一个表现 commit 前依次成立
- **THEN** Corin StateMachine MUST 保留全部合法 transition facts与 activation generation
- **AND** Arbitrator MUST 将连通 facts归并为一个 Base causal component
- **AND** LayerRuntime MUST 只执行一个从当前 FinalOutput 到最终 RunEnd#7 的 HandoffPlan

#### Scenario: 结构 State 保持单一职责

- **WHEN** 迁移完成
- **THEN** ActionOverride、None 与外层 Attack MUST 不新增 animation、Timeline 或 motion producer
- **AND** 项目 MUST NOT创建一次性 SubTree asset 维持动画
