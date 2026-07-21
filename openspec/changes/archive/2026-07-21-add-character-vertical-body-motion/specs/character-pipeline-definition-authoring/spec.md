## ADDED Requirements

### Requirement: Body Motion Profile必须是唯一垂直动力作者配置

`CharacterPipelineDefinition` MUST显式引用一个`CharacterBodyMotionProfile`，Profile MUST唯一保存GravityAcceleration与MaximumFallSpeed作者配置。Definition Inspector MUST只在作者配置区显示Profile引用与配置错误，MUST不内联或复制Profile字段。Compiler MUST把Profile作为正式source revision和Program descriptor输入；Runtime Host、Scene、Network Model、WorldSolver与Blackboard MUST不保存第二份重力配置或缺失默认。

#### Scenario: Definition缺少Body Motion Profile

- **WHEN** 作者尝试编译或运行缺少Profile的CharacterPipelineDefinition
- **THEN** 配置校验或Compiler MUST明确失败
- **AND** Runtime MUST不创建默认Profile或按Solver补值

