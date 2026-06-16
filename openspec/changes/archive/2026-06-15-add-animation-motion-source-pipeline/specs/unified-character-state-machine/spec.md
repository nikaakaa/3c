## ADDED Requirements

### Requirement: 状态输出声明动画运动源策略
统一状态机 MUST 允许逻辑状态输出声明通用动画运动源策略。该策略 MUST 是纯数据输出，由后续 locomotion/motion pipeline 消费，不得让状态机 runner 直接调用 Animator、Animancer、CharacterController 或 motion executor。

#### Scenario: 状态输出携带策略
- **GIVEN** 设计者在状态配置中为某个状态启用动画运动源
- **WHEN** 统一状态机产出该状态的状态帧
- **THEN** 状态帧 MUST 携带该动画运动源策略
- **AND** 策略 MUST 能表达 yaw source、translation source、source id 和输入抑制语义

#### Scenario: Runner 保持纯数据边界
- **WHEN** 统一状态机 runner 构建状态帧
- **THEN** runner MUST NOT 采样 AnimationClip
- **AND** MUST NOT 读取 Animancer runtime state
- **AND** MUST NOT 调用 `CharacterController.Move`

#### Scenario: TurnBack 使用通用策略
- **GIVEN** 当前状态为 `FullBody/Locomotion/TurnBack`
- **WHEN** 状态输出声明 TurnBack 动画运动源策略
- **THEN** 后续管线 MUST 按通用动画运动源能力处理 TurnBack yaw 和 translation
- **AND** MUST NOT 在状态机 runner 内写入 TurnBack 专用运动逻辑
