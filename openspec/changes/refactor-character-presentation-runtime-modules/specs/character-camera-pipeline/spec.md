## MODIFIED Requirements

### Requirement: CharacterSimulationPresentationRuntime 必须是相机 runtime 唯一边界

系统 MUST使用 `CharacterSimulationPresentationRuntime` 作为角色相机runtime的唯一公开编排边界。该协调器 MAY拥有不可被Host、Network adapter或Gameplay代码直接访问的内部 `CharacterCameraPresentationRuntime`；内部Camera Runtime MUST唯一拥有Camera State/Response/Target/Cue lifecycle、resolver、look input、bind offset和`ICameraRigAdapter`调用。协调器 MUST在`PresentationFrame`中使用同一 `CharacterBodyPresentationFrame` 的visible pose推进Animation与Camera，并把Camera结果交给rig adapter。Camera capability MUST通过Factory的完整显式binding创建，MUST不决定Body clock策略。系统 MUST不保留Camera MonoBehaviour自主`LateUpdate`、外部Camera resolver调用或无相机Actor分配Camera容器的路径。

#### Scenario: Local Owner推进相机

- **WHEN** `CharacterPresentationFrameTarget`调用唯一 `ICharacterPresentationRuntime.Present`
- **THEN** 协调器 MUST先取得本帧唯一Body visible pose
- **AND** 内部Camera Runtime MUST使用该pose、已提交camera command、target binding和look input生成并应用CameraPosePlan

#### Scenario: 无相机 Simulated Actor

- **WHEN** Factory创建一个完整模拟但不拥有本地相机的Actor
- **THEN** MUST不创建Camera Runtime、request容器或resolver
- **AND** 该Actor仍 MUST使用其显式Body clock策略

#### Scenario: 无Camera组合收到Camera命令

- **WHEN** observed或simulated无Camera Actor收到Camera PresentationCommand
- **THEN** 唯一协调器 MUST报告明确配置错误
- **AND** MUST不搜索场景相机或创建默认Camera Runtime

#### Scenario: 禁止双驱动

- **WHEN** camera rig已由内部Camera Runtime驱动
- **THEN** Host、Network adapter和旧相机控制器 MUST不再修改同一个follow、aim、FOV或priority状态
