## ADDED Requirements
### Requirement: Locomotion 作为角色级兄弟 Submitter 实装
Locomotion runtime MUST 在 Corin 正式主线中作为 `CharacterFrameRuntimeController` 下的 sibling submitter 接入。Locomotion submitter MUST 提交移动意图、世界方向、gait、phase、基础移动 motion candidate 和 Locomotion animation candidate。Locomotion submitter MUST NOT 作为独立 direct tick 主线提交最终输出。

#### Scenario: Locomotion submitter 产出候选输出
- **WHEN** Corin 正式角色处理基础移动输入
- **THEN** Locomotion submitter MUST 通过 Locomotion runtime port 读取移动事实
- **AND** MUST 提交基础移动 motion candidate
- **AND** MUST 提交 Locomotion animation candidate
- **AND** 最终是否执行 MUST 由 CharacterFramePlan 决定

#### Scenario: Action active 时 Locomotion 不补交输出
- **GIVEN** CharacterFramePlan 标记 Locomotion motion 或 animation 被 FullBody claim 压制
- **WHEN** output applier 执行本帧
- **THEN** Locomotion motion candidate MUST NOT 被提交给 motion executor
- **AND** Locomotion animation candidate MUST NOT 被提交给 presenter
- **AND** `PlayerLocomotionController` direct tick MUST NOT 在管线外补交输出

#### Scenario: Direct tick 保留为非正式诊断
- **WHEN** 项目保留 `PlayerLocomotionController.Tick`、`TickFromInputSource` 或等价 direct tick API
- **THEN** 这些 API MUST 标记为非正式 gameplay 主线
- **AND** MUST NOT 与 `CharacterFrameRuntimeController` 竞争 movement、animation 或 camera output
- **AND** MUST 可通过静态测试证明 Corin 正式 prefab/scene 不依赖 direct tick

### Requirement: Locomotion Controller 降级为 Adapter
`PlayerLocomotionController` MUST 在正式角色主线中作为 Locomotion runtime adapter、output adapter 或 diagnostic view 存在。它 MUST NOT 作为正式 Unity `Update` gameplay driver、状态机 owner 或 Character frame owner。

#### Scenario: AutoUpdate 不作为正式主线
- **WHEN** 检查 Corin 正式 prefab/scene
- **THEN** `PlayerLocomotionController.AutoUpdate` MUST 不作为正式 gameplay driver
- **AND** frame update MUST 从 `CharacterFrameRuntimeController` 进入
- **AND** simulation tick MUST 从角色级 tick adapter 进入

#### Scenario: Locomotion 不创建 runner
- **WHEN** Locomotion submitter 或 controller 参与 Character frame
- **THEN** 它 MUST NOT 创建、重置或推进第二个 `CharacterStateMachineRunner`
- **AND** 状态权威 MUST 来自 Character runtime controller 装配的唯一 runner
- **AND** Locomotion phase view MUST 从 frame data、runtime state store 或统一状态机输出派生
