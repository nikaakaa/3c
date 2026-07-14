## ADDED Requirements

### Requirement: Graph Data Catalog 必须提供 Gameplay Effect 正式来源

Graph Data Catalog MUST 新增 Gameplay Effect source，统一投影当前 `CharacterPipelineDefinition` 可见的 Tag、Attribute 和 GameplayEffect 条目。条目 MUST 显示稳定 identity、业务类型、只读/命令能力和来源 definition；Catalog MUST 复用现有搜索、source filter、详情和拖拽创建链路，不得建立独立 GE 浏览窗口。

#### Scenario: 作者查找 Stamina

- **WHEN** 作者在 RootTree context 搜索 `Stamina`
- **THEN** Catalog MUST 返回 Attribute 条目及其 value node 创建能力
- **AND** 条目 MUST 来自当前 CharacterGameplayEffectProfile 的正式 registry

#### Scenario: 当前 Graph 只允许纯决策

- **WHEN** 作者在 Condition Rule Graph 选择一个 GameplayEffect 条目
- **THEN** Catalog MUST 只提供 `CanApplyEffect` 等只读节点能力
- **AND** MUST 撤销 Apply/Remove 命令节点能力

### Requirement: Gameplay Effect Catalog 必须遵守条目所有权

Tag、Attribute 和 Effect 的 identity 与 definition 编辑 MUST 归属其正式 catalog/asset owner。Graph Data Catalog MAY 提供定位和引用能力，但 MUST NOT 在 Graph 内复制或内联修改共享 effect definition；角色初始值的编辑 MUST 只通过当前 `CharacterGameplayEffectProfile` 的正式入口完成。

#### Scenario: 拖拽 Effect 到执行 Graph

- **WHEN** 当前 Graph 允许副作用命令且作者拖拽一个 Effect 条目
- **THEN** Catalog MAY 创建引用稳定 EffectId 的 ApplyEffect node
- **AND** MUST NOT 把 effect component 配置复制进节点

#### Scenario: Effect 不在当前 registry

- **WHEN** Graph node 引用的 EffectId 不属于当前角色可见 registry
- **THEN** Catalog 和 validation MUST 显示明确无效引用
- **AND** MUST NOT 按名称搜索其它资产自动补齐
