## ADDED Requirements
### Requirement: 自研分层状态机代码与文档归属
系统 MUST 将自研统一分层状态机的模型、配置、运行时解释、timeline facts、输出解析和诊断文档放在可发现的 Character 状态机目录和 agent 文档中。目录和文档命名 MUST 体现它是当前角色主线的状态图运行时，而不是 UnityHFSM adapter、BBB 旧状态机或 Locomotion 局部状态机。

#### Scenario: 状态机代码目录清晰
- **WHEN** 新增或调整角色状态机运行时代码
- **THEN** 纯数据模型 MUST 放在 `Assets/Scripts/Character/StateMachine/Model/`
- **AND** ScriptableObject 配置 MUST 放在 `Assets/Scripts/Character/StateMachine/Config/`
- **AND** runner 和状态生命周期 MUST 放在 `Assets/Scripts/Character/StateMachine/Solver/Runtime/`
- **AND** timeline sampler MUST 放在 `Assets/Scripts/Character/StateMachine/Solver/Timeline/`
- **AND** transition evaluator MUST 放在 `Assets/Scripts/Character/StateMachine/Solver/Transition/`
- **AND** output resolver MUST 放在 `Assets/Scripts/Character/StateMachine/Solver/Output/`
- **AND** validator MUST 放在 `Assets/Scripts/Character/StateMachine/Solver/Validation/`
- **AND** Runtime Adapter MUST NOT 混入状态机 solver 目录

#### Scenario: 状态机配置资产目录清晰
- **WHEN** 新增或迁移角色状态机配置资产
- **THEN** 中心状态机配置 MUST 放在 `Assets/Configs/3C/StateMachine/`
- **AND** 默认状态机配置 MUST 位于 `Assets/Configs/3C/StateMachine/DefaultCharacterStateMachine.asset`
- **AND** 旧 `Assets/Configs/3C/Statemachine/` MUST NOT 作为并行状态机配置目录保留
- **AND** 状态机配置资产 MUST NOT 保存 AnimationClip、Animancer TransitionAsset、fade、speed 或 normalized start time

#### Scenario: 文档指向当前主线
- **WHEN** 新增或更新 agent 状态机指南
- **THEN** 指南 MUST 以项目自研统一分层状态机为当前主线
- **AND** UnityHFSM 指南 MUST 标记为历史参考、第三方库参考或未来另行审批方向
- **AND** 文档 MUST 说明状态机运行时不得直接执行运动、播放动画或读取输入系统对象

#### Scenario: 没有隐式 fallback 配置
- **WHEN** 状态机配置缺失或非法
- **THEN** 文档和实现 MUST 要求明确诊断错误
- **AND** MUST NOT 通过 Resources、全局单例、旧字段或代码默认状态图隐式恢复运行

#### Scenario: 测试和验证可发现
- **WHEN** 实施状态机 runtime 重构
- **THEN** tasks MUST 包含自动测试、静态边界验证和手动 Play Mode 验证说明
- **AND** 手动验证 MUST 明确如何观察 active path、pending transition、timeline facts 和 rollback 结果
