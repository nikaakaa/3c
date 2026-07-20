# character-gameplay-effect-authoring Specification

## MODIFIED Requirements

### Requirement: Gameplay Effect authoring 必须构建不可变 Runtime Definition

Compiler MUST在生成 CharacterSimulationProgram 前闭包校验 CharacterGameplayEffectProfile、Tag Catalog、Attribute Definition 和 Effect Definition，并将其编译为不可变 portable GameplayEffect catalog/operation data。Runtime MUST不回读 CharacterPipelineDefinition、ScriptableObject、asset path 或 Inspector context，也 MUST不创建空 registry/default Effect fallback。

#### Scenario: 编译角色 Gameplay Effect

- **WHEN** CharacterPipelineDefinition 的 GE 配置闭包完整
- **THEN** Compiler MUST将 catalog写入 Program canonical bytes
- **AND** CharacterSimulationState MUST按对应 layout创建 GE slots

#### Scenario: Definition 闭包不完整

- **WHEN** Effect 引用未注册 Tag、Attribute 或 Additional Effect
- **THEN** Program build MUST失败并报告精确 authoring identity
