## MODIFIED Requirements
### Requirement: 单驱动权威
系统 SHALL 保证同一玩家 FullBody base layer 在任一运行模式下只有一个统一状态机 runner 推进状态，不得同时由 Unity frame 路径、Locomotion 局部图和 Action runtime 路径多重驱动。正式入口 MUST 是 `CharacterFrameRuntimeController`、`CharacterFrameRuntimeHost` 和统一状态机运行时模块，而不是 `PlayerFullBodyActionController`。

#### Scenario: Character runtime tick 统一 runner
- **GIVEN** `CharacterFrameRuntimeController` 启用
- **WHEN** 它处理一帧输入
- **THEN** 它 MUST 通过唯一 `CharacterStateMachineRuntime` 或等价模块 tick 统一状态机 runner
- **AND** MUST 根据统一状态机输出和 `CharacterFramePlan` 选择基础移动或 FullBody Action 输出

#### Scenario: Locomotion controller 不拥有第二状态图
- **GIVEN** `CharacterFrameRuntimeController` 接管 FullBody base layer
- **WHEN** 它调度 Locomotion runtime 或 `PlayerLocomotionController` adapter
- **THEN** Locomotion runtime MUST 使用同一统一状态机 runner 产生的状态 facts
- **AND** MUST NOT 推进独立 Locomotion 状态图

#### Scenario: 不新增绕过入口
- **WHEN** 后续接入 simulation tick、网络预测、回放或 AI 输入
- **THEN** 调度层 MUST 合流到同一个统一状态机入口
- **AND** 系统 MUST NOT 新增绕过统一状态机的第二移动控制器
- **AND** 系统 MUST NOT 重新引入 `PlayerFullBodyActionController`
