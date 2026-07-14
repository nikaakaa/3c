# character-pipeline-runtime Specification Delta

## ADDED Requirements

### Requirement: CharacterPipelineDefinition 持有角色输入合同
系统 MUST 让 `CharacterPipelineDefinition` 持有该角色的正式 `CharacterInputProfile`。`CharacterPipelineHost` MUST NOT 单独持有 input profile 配置。运行时创建 `CharacterPipeline` 时，Host MUST 从 `CharacterPipelineDefinition` 读取 input profile、RootTree、AnimationLayers 和 ActionProfiles。

#### Scenario: Host 创建 pipeline
- **WHEN** `CharacterPipelineHost` 创建角色 pipeline
- **THEN** Host MUST 使用 `CharacterPipelineDefinition.InputProfile` 创建输入阶段
- **AND** Host MUST NOT 使用场景对象上的第二份 input profile 字段

#### Scenario: Definition 配置缺失 input profile
- **WHEN** `CharacterPipelineDefinition` 没有配置 `CharacterInputProfile`
- **THEN** definition 配置校验 MUST 报告错误
- **AND** 系统 MUST NOT 从 Host、场景对象或默认资源中寻找 fallback profile

#### Scenario: 输入 profile 配置错误
- **WHEN** `CharacterInputProfile` 中存在缺失 action、重复 input value id 或重复 request id
- **THEN** `CharacterPipelineDefinition` 配置校验 MUST 暴露这些错误
- **AND** Graph authoring MUST 继续以该 profile 作为唯一输入合同来源

### Requirement: CharacterPipelineDefinition 提供 RootTree authoring context
系统 MUST 允许 editor 从 `CharacterPipelineDefinition` 打开 RootTree，并将 definition 和 input profile 作为 editor-only authoring context 传给 TreeWindow。该 context 只服务 authoring UI，不改变 runtime Graph 执行语义。

#### Scenario: 从 Definition 打开 RootTree
- **WHEN** 用户从 `CharacterPipelineDefinition` editor 打开 RootTree
- **THEN** TreeWindow MUST 获得当前 definition 和 `InputProfile`
- **AND** Input authoring 素材区 MUST 使用该 context 展示输入定义

#### Scenario: 多个 Definition 复用 RootTree
- **WHEN** 多个 `CharacterPipelineDefinition` 引用同一个 RootTree
- **THEN** Input authoring 素材区 MUST 使用打开入口传入的 definition
- **AND** 系统 MUST NOT 通过 AssetDatabase 反查猜测唯一 definition
