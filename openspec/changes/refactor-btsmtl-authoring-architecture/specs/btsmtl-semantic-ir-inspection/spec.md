## MODIFIED Requirements

### Requirement: Unity Editor 必须提供只读 Semantic IR Inspector

系统 MUST为 CharacterPipelineDefinition 提供显式打开的只读 Semantic IR Inspector。Inspector MUST从当前 validated `.csir` artifact 显示 Manifest、控制模块合同、技能执行根／依赖／调用签名、Operations、Literals、ControlFlow、StateSlots、Scopes、WorldRequests、OutputChannels、CatalogEntries、Producers 和 SourceMap，并支持按 operation code、identity 与精确 source identity 搜索。Inspector MUST不编辑 artifact、authoring 或 generated Program，也 MUST不在普通 Repaint 时自动运行 Frontend。

#### Scenario: 查看 Corin技能局部StateMachine Operation

- **WHEN** 作者从 Corin Definition 打开 Semantic IR Inspector 并选择一个 StateMachine operation
- **THEN** Inspector MUST显示其 handle、operation code、operands、literal references、state slots、control-flow edges 与 source location
- **AND** 显示内容 MUST来自当前 artifact，不得从 Graph 重新推断 operation table

#### Scenario: 当前 IR Cache 过期

- **WHEN** Inspector 发现 cache SourceRevision 与当前 Definition 不一致
- **THEN** MUST显示明确 stale 状态并停止展示旧 tables
- **AND** MUST只通过作者显式 `Compile Semantic IR` 命令调用正式 Frontend，不在 Repaint 隐式刷新
#### Scenario: 查看代码控制合同

- **WHEN** 角色已迁移为代码控制与技能目录
- **THEN** Inspector MUST显示模块版本／state合同并能导航技能根
- **AND** MUST不重建角色控制图或在重绘时重新编译

#### Scenario: 查看 Corin StateMachine Operation

- **WHEN** 作者从 Corin Definition 打开 Semantic IR Inspector 并选择一个技能局部StateMachine operation
- **THEN** Inspector MUST显示其 handle、operation code、operands、literal references、state slots、control-flow edges 与 source location
- **AND** 显示内容 MUST来自当前 artifact，不得从 Graph 重新推断 operation table
