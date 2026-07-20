## ADDED Requirements

### Requirement: Equipment Feature不得恢复旧Ability执行单元

Equipment Feature MAY拥有被Compiler静态链接的普通inline graph和导出ActionProfile，但正式Runtime MUST不出现`ActionModule`、`AbilityAsset`、`IAbilityBody`、`AbilityTree`、Feature graph clone或按Feature调用Graph的Action接口。Feature owner metadata MUST只用于编译、source map、route entry和diagnostics，不得成为Action身份或第二membership table。

#### Scenario: Feature Action进入Runtime

- **WHEN** Sawblade Route激活Attack
- **THEN** 动作身份 MUST仍为Attack ActionProfile与新ActionInstanceId
- **AND** FeatureId MUST只作为Equipment Context/source metadata

#### Scenario: 查找Action body

- **WHEN** runtime需要执行已选择Route body
- **THEN** compiled Equipment Host MUST使用Program entry index
- **AND** ActionInstance runtime MUST不加载AbilityBody

### Requirement: ActionInstance必须可选保存Equipment Context

ActionInstance state MUST新增可选Equipment Context，包含SlotId、EquipmentId、FeatureId、EquipmentRevision与RouteId，并进入copy、transaction、codec、snapshot、hash、fact与diagnostics。只有Feature Route创建的Action MUST携带该context；Core Action MUST为None。Context MUST在实例生命周期内不可变。

#### Scenario: Feature Action完成

- **WHEN** Sawblade Attack ActionInstance完成
- **THEN** lifecycle fact MUST携带同一Equipment Context
- **AND** context MUST不因当前Loadout变化被改写

#### Scenario: 恢复未知Feature context

- **WHEN** snapshot中的FeatureId不在当前Program catalog
- **THEN** restore MUST失败
- **AND** MUST不将context降级为None

