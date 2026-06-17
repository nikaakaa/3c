## ADDED Requirements
### Requirement: Corin Prefab 使用正式角色配置根
系统 MUST 让 Corin 正式角色 prefab 和正式场景实例通过 `CorinCharacterConfig.asset` 作为配置根入口。Prefab 和 scene override MUST NOT 通过旧平铺配置字段形成第二正式入口或 fallback。

#### Scenario: Prefab 绑定同一根配置
- **WHEN** 自动校验 `Assets/Prefabs/Character/可琳.prefab` 和 `Assets/Prefabs/Character/可琳_Humanoid.prefab`
- **THEN** `PlayerLocomotionController.characterConfig` MUST 指向 `CorinCharacterConfig.asset`
- **AND** `PlayerFullBodyActionController.characterConfig` MUST 指向同一个 `CorinCharacterConfig.asset`
- **AND** 两个 controller MUST NOT 通过各自平铺字段解析不同子配置

#### Scenario: Scene override 不恢复旧入口
- **WHEN** 自动校验正式场景中的 Corin 角色实例
- **THEN** scene override MUST NOT 覆盖为旧 `runAnimationConfig`、`config`、`stateMachineDefinition`、`interruptPolicySet` 或 `dodgeActionConfig` 正式入口
- **AND** scene MUST NOT 引入第二个正式角色配置根

#### Scenario: Humanoid Prefab 不保留重复根字段
- **WHEN** 自动校验 `Assets/Prefabs/Character/可琳_Humanoid.prefab`
- **THEN** `PlayerLocomotionController` 的有效 `characterConfig` MUST 指向 `CorinCharacterConfig.asset`
- **AND** YAML 或 SerializedObject 校验 MUST NOT 发现第二个会覆盖正式根配置的同名残留字段
