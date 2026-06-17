## MODIFIED Requirements
### Requirement: Scene Tick 组装
系统 MUST 在当前演示场景中提供明确的 tick driver 组装点，并将当前角色 gameplay 接入 Character frame pipeline。FullBody Action 和 Locomotion 是当前角色帧管线内的提交来源，不能通过独立 FullBody controller 或 Locomotion tick adapter 接入场景 tick driver。

#### Scenario: 场景存在 tick driver
- **WHEN** 打开 `Sandbox` 或当前演示场景
- **THEN** 场景 MUST 包含一个用于客户端 simulation tick 的 `UnitySimulationTickDriver` 或等价组件

#### Scenario: 当前角色接入 Character tick driver
- **WHEN** 当前演示角色存在 `CharacterFrameRuntimeController` 或等价角色级 runtime owner
- **THEN** 该角色 MUST 通过 Character frame pipeline 接入场景 tick driver
- **AND** MUST NOT 同时由 frame Update 直接驱动
- **AND** MUST NOT 同时由 `LocomotionTickAdapter`、`FullBodyActionTickAdapter` 或 `PlayerFullBodyActionController` 驱动

#### Scenario: 没有第二控制路径
- **WHEN** 场景完成 tick 接入
- **THEN** 场景 MUST NOT 新增绕过 `CharacterFramePipeline`、`CharacterFrameRuntimeController` 或 motion executor 的第二套移动控制路径
- **AND** 场景 MUST NOT 保留 `PlayerFullBodyActionController` 作为装配 adapter
