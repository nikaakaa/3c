# gameplay-sync-runtime Specification

## MODIFIED Requirements

### Requirement: Common Network Session 必须只管理模型生命周期

系统 MUST提供model-neutral Session composition boundary，用于持有唯一model definition、创建/锁定model session、注册actor binding并管理dispose。Common host MUST不定义packet、history、prediction、correction、rollback、snapshot、World Solver或commit语义。

#### Scenario: 创建ServerAuthoritative模型

- **WHEN** SessionHost读取ServerAuthoritative definition
- **THEN** MUST创建该模型session/Driver
- **AND** common host MUST不读取其packet或solver

#### Scenario: 创建DeterministicRollback模型

- **WHEN** SessionHost读取Rollback definition
- **THEN** MUST创建不同完整model session/Driver
- **AND** common host MUST不解释input bundle、snapshot或hash

### Requirement: 同步 Runtime、Packet、History 和 Debug 必须声明模型归属

任何管理network packet、history、queue、rollback、correction和model diagnostics的runtime MUST归属明确Network Model。ServerAuthoritative与DeterministicRollback MUST拥有独立packet/history/protocol模块；系统 MUST不使用无模型限定的GameplaySync类型混装两者语义。Model-neutral comparison metrics只可读取两者正式输出，MUST不成为共享策略runtime。

#### Scenario: 搜索通用同步类型

- **WHEN** 实现完成后检查正式runtime
- **THEN** 通用Session只保留definition/session lifecycle和actor binding合同
- **AND** ServerAuthoritative与Rollback专属类型 MUST位于各自模块

