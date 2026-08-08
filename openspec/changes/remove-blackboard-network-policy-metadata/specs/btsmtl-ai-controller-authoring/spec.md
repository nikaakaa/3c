## MODIFIED Requirements

### Requirement: AI Blackboard必须与Character Blackboard分离

AI Controller MUST使用独立AI Blackboard与AIControllerState。Controller scope MUST跨AI Logic Tick保存记忆；Tick scope MUST只存在于一次AI Evaluate；Graph scope MUST保持Graph局部owner。AI Blackboard MUST NOT解析Character、State、ActionInstance scope，也 MUST NOT直接访问CharacterSimulationState。AI declaration MUST只保存基础identity、类型、默认值、owner、scope、lifetime和category，不保存LocalOnly、Authority或SyncPolicy。当前AI Blackboard MUST拒绝Character Input Binding与ActionWindow Fact Projection。AI与Character之间唯一可写边界 MUST是最终CharacterSimulationInput。

#### Scenario: AI保存当前目标

- **WHEN** AI Tree把选中Actor写入Controller-scope CurrentTarget
- **THEN** 值 MUST进入AIControllerState并在下一AI Tick可读
- **AND** Character Pipeline Blackboard MUST不增加同一变量副本
- **AND** declaration MUST不要求LocalOnly或None策略标签

#### Scenario: AI读取Character动作变量

- **WHEN** AI节点尝试引用Character或ActionInstance scope declaration
- **THEN** Data Catalog与Compiler MUST拒绝该引用
- **AND** Runtime MUST不回退到同名AI变量

#### Scenario: AI declaration包含Character binding

- **WHEN** AI Document为AIController或AITick declaration配置Input Binding或ActionWindow Fact Projection
- **THEN** strict parser或Validator MUST在AIIntentProgram编译前拒绝
- **AND** MUST不把它转换为Observation或Character intent

