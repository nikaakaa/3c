## MODIFIED Requirements
### Requirement: FullBody 回滚重放主入口
系统 MUST 提供 FullBody 回滚重放能力，使本地 synctest 的 replay 通过当前角色正式 `CharacterFrameRuntimeController -> CharacterFrameRuntimeHost -> CharacterFramePipeline` 主线推进动作、移动、动作事实和动画事实。该能力 MUST 复用现有输入缓冲、Locomotion runtime、FullBody Action runtime、统一状态机 runtime 和正式 output runtime，不得新增第二套角色控制器或第二套状态机。

#### Scenario: 重放走 Character frame 主线
- **GIVEN** replay adapter 收到 tick N 的 `PredictionInputFrame`
- **WHEN** replay 推进 tick N
- **THEN** 系统 MUST 将输入帧转换为 `CharacterFrameInput`、`BasicLocomotionInputSnapshot` 或等价角色帧输入
- **AND** MUST 调用 `CharacterFrameRuntimeController`、`CharacterFrameRuntimeHost` 或等价角色级 replay 入口
- **AND** MUST NOT 调用 `PlayerFullBodyActionController.Tick(...)`

#### Scenario: 保留 locomotion-only adapter
- **GIVEN** 现有 locomotion-only synctest 测试仍需要窄范围验证
- **WHEN** 用户选择 locomotion-only replay adapter
- **THEN** 系统 MAY 继续只通过 `PlayerLocomotionController` 重放
- **AND** 该 adapter MUST 明确标识为 locomotion-only，不得作为 Sandbox 动作 demo 的完整回滚验收

#### Scenario: 不创建分裂控制路径
- **WHEN** FullBody replay 推进角色
- **THEN** 系统 MUST NOT 直接调用 `BasicLocomotionPipeline`
- **AND** MUST NOT 直接调用 `CharacterController.Move`
- **AND** MUST NOT 通过旧 FullBody controller、HFSM/Dodge 缝合路径或 submitter 具体实现恢复状态权威

### Requirement: FullBody 状态 Capture/Restore
系统 MUST 定义 FullBody action 运行时状态的纯数据 capture/restore 边界，使 replay 从历史 tick 恢复时，FullBody 当前状态、动作状态、pending transition 和影响下一 tick 输出的事实能回到快照时刻。restore state MUST NOT 保存 Unity Object、Animancer runtime object、Animator、AnimationClip、InputAction 或场景实例引用。capture/restore MUST 由 `CharacterStateMachineRuntime`、FullBody Action runtime 或等价窄模块承担，而不是由 `PlayerFullBodyActionController` 承担。

#### Scenario: 捕获 FullBody 状态
- **WHEN** tick N 的角色模拟快照被创建
- **THEN** 系统 MUST 能捕获当前 FullBody owner、action state、state time、variant 和 pending transition 或等价事实
- **AND** 捕获结果 MUST 是纯数据
- **AND** 捕获入口 MUST 不依赖 `PlayerFullBodyActionController`

#### Scenario: 恢复 FullBody 状态
- **GIVEN** 系统持有 tick N 的 FullBody restore state
- **WHEN** replay 从 tick N 恢复
- **THEN** 状态机运行时或 FullBody Action runtime MUST 恢复到该 restore state
- **AND** 恢复后下一 tick 的 FullBody active state MUST 与恢复前同一输入序列一致
- **AND** 恢复入口 MUST 不依赖 `PlayerFullBodyActionController`

#### Scenario: 不污染输入历史
- **WHEN** replay 恢复 FullBody 状态
- **THEN** 系统 MUST NOT 将动作消费结果写回 `PredictionInputHistory`
- **AND** 后续 replay MUST 仍从原始输入帧重新推导动作请求
