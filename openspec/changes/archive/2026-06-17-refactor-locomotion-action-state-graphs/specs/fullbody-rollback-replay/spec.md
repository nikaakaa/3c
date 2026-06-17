# fullbody-rollback-replay Delta

## MODIFIED Requirements
### Requirement: FullBody 回滚重放主入口
系统 MUST 提供 FullBody 回滚重放能力，使本地 synctest 的 replay 能通过当前 Character frame pipeline 主线推进动作、移动、动作事实和动画事实。该能力 MUST 复用现有角色 runtime 入口、PlayerLocomotionController adapter、InputRequestBuffer、Locomotion graph 和 Action lifecycle，不得新增第二套角色控制器、第二套 gameplay tick 入口或默认 mixed 状态图。

#### Scenario: 重放走 Character frame 主线
- **GIVEN** replay adapter 收到 tick N 的 `PredictionInputFrame`
- **WHEN** replay 推进 tick N
- **THEN** 系统 MUST 将输入帧转换为 `BasicLocomotionInputSnapshot`
- **AND** MUST 调用 Character frame pipeline 或等价当前正式 FullBody 主线
- **AND** MUST NOT 只调用 `PlayerLocomotionController.Tick(...)` 作为 FullBody replay 的最终路径
- **AND** MUST NOT 通过默认 graph active `Action.Dodge` 表达 Dodge lifecycle

#### Scenario: 保留 locomotion-only adapter
- **GIVEN** 现有 locomotion-only synctest 测试仍需要窄范围验证
- **WHEN** 用户选择 locomotion-only replay adapter
- **THEN** 系统 MAY 继续只通过 `PlayerLocomotionController` 或 Movement module 重放
- **AND** 该 adapter MUST 明确标识为 locomotion-only，不得作为 Sandbox 动作 demo 的完整回滚验收

#### Scenario: 不创建分裂控制路径
- **WHEN** FullBody replay 推进角色
- **THEN** 系统 MUST NOT 直接调用 `BasicLocomotionPipeline`
- **AND** MUST NOT 直接调用 `CharacterController.Move`
- **AND** MUST NOT 通过旧 FullBody/HFSM/Dodge 缝合路径恢复状态权威

### Requirement: FullBody 状态 Capture/Restore
系统 MUST 定义 FullBody action 运行时状态的纯数据 capture/restore 边界，使 replay 从历史 tick 恢复时，Locomotion graph restore state、Action lifecycle restore state、input buffer restore state、pending frame facts 和影响下一 tick 输出的事实能回到快照时刻。restore state MUST NOT 保存 Unity Object、Animancer runtime object、Animator、AnimationClip、InputAction 或场景实例引用。

#### Scenario: 捕获 Locomotion 与 Action 状态
- **WHEN** tick N 的角色模拟快照被创建
- **THEN** 系统 MUST 能捕获当前 Locomotion graph active state、state time、variant 和 pending transition 或等价 facts
- **AND** MUST 能捕获 Action lifecycle active action、variant、state time、source step、completion state 和 pending release 或等价 facts
- **AND** 捕获结果 MUST 是纯数据
- **AND** active Dodge MUST NOT 要求 Locomotion graph active state 为 `Action.Dodge`

#### Scenario: 恢复 active Dodge
- **GIVEN** 系统持有 tick N 的 FullBody restore state
- **AND** restore state 中 Action lifecycle active action 为 `Action.Dodge`
- **WHEN** replay 从 tick N 恢复
- **THEN** Action module MUST 恢复到 active Dodge lifecycle state
- **AND** Locomotion graph MUST 恢复到自己的 Locomotion state
- **AND** 恢复后下一 tick 的 Action facts 与恢复前同一输入序列一致

#### Scenario: 不污染输入历史
- **WHEN** replay 恢复 FullBody 状态
- **THEN** 系统 MUST NOT 将动作消费结果写回 `PredictionInputHistory`
- **AND** 后续 replay MUST 仍从原始输入帧重新推导动作请求

### Requirement: FullBody Runtime Facts 收敛
系统 MUST 在 FullBody replay 中重新写入 action facts、locomotion facts、animation facts 和 runtime blackboard facts，使同一输入序列重放后的最终快照能与原始快照在定义容差内比较。Action facts MUST 从 Action lifecycle 或 action output 派生；Locomotion facts MUST 从 Movement module 派生；若 animation facts 仍受表现层播放进度影响，系统 MUST 输出字段级 differences 以定位缺口。

#### Scenario: Action facts 重放收敛
- **GIVEN** 原始运行中 Dodge 请求被 FullBody 主线接受
- **WHEN** replay 使用同一段输入重放到同一 end tick
- **THEN** replay 后的 action active/state/completed/sourceStep MUST 与原始快照一致或输出明确 differences
- **AND** 比较 MUST 不要求默认 graph active state 为 `Action.Dodge`

#### Scenario: Locomotion facts 重放收敛
- **GIVEN** 原始运行中 Locomotion graph 处于 `Locomotion.MoveLoop`
- **WHEN** replay 使用同一段输入重放到同一 end tick
- **THEN** replay 后的 locomotion phase、state time 和输出候选 facts MUST 与原始快照一致或输出明确 differences
- **AND** 比较 MUST 区分 Locomotion facts 与 Action lifecycle facts

#### Scenario: Animation facts 通过可控事实源测试
- **GIVEN** 自动测试使用 fake animation presenter 或 fake playback progress source
- **WHEN** replay 使用同一段输入重放
- **THEN** replay 后的 animation key、normalized time 和 sourceStep MUST 与原始快照一致

## ADDED Requirements
### Requirement: Dodge Run latch 回滚收敛
系统 MUST 在 FullBody replay 和本地回滚中保持 Directional Dodge 完成后的 Run latch 行为确定。Run latch MUST 作为 Locomotion runtime state capture/restore 的一部分参与比较；Action lifecycle restore 只恢复动作状态，不得用默认 graph active `Action.Dodge` 或 Action facts 代替 Run latch。

#### Scenario: Directional 完成后 Run latch replay 收敛
- **GIVEN** 原始运行中 Directional Dodge 完成帧仍有移动输入
- **AND** 原始运行通过 frame output 写入 Run latch
- **WHEN** replay 从动作前或动作中快照恢复并重放同一输入序列
- **THEN** replay 后 Locomotion runtime Run latch MUST 与原始运行一致
- **AND** 后续保持移动输入时 gait MUST 同样解析为 Run
- **AND** 比较 MUST 不要求默认 graph active state 为 `Action.Dodge`

#### Scenario: 无移动完成或 Backstep 不产生 Run latch
- **GIVEN** 原始运行中 Directional Dodge 完成帧没有移动输入或 Dodge 变体为 Backstep
- **WHEN** replay 重放同一输入序列
- **THEN** replay 后 Locomotion runtime Run latch MUST 保持 false
- **AND** 后续 Locomotion phase/gait MUST 与原始运行一致

#### Scenario: Backstep 无输入重放等待动作动画完成
- **GIVEN** 原始运行中 Backstep Dodge 已达到动作位移 duration
- **AND** 本帧没有移动输入
- **AND** 匹配 `Action.Dodge.Backstep` 动作动画尚未播放完成
- **WHEN** replay 重放到同一 tick
- **THEN** Action lifecycle MUST 仍保持 active `Action.Dodge`
- **AND** replay MUST NOT 提前清除 action animation playback
- **AND** Locomotion runtime Run latch MUST 保持 false

#### Scenario: 停止清 latch 参与快照
- **GIVEN** Run latch 曾因 Directional Dodge 完成而 active
- **AND** 玩家停止移动并完成 RunEnd/Idle 收尾
- **WHEN** 系统 capture tick N 的 FullBody restore state
- **THEN** restore state MUST 记录清除后的 Run latch
- **AND** replay 从 tick N 恢复后的下一次移动 MUST 从 Walk 起步
