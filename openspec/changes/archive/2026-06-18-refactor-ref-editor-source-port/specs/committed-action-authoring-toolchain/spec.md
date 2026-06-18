## ADDED Requirements
### Requirement: Ref 源码级移植不得创建第二 Action Authoring 路径
Committed Action authoring toolchain MUST 将 Ref 源码级 editor 移植视为现有正式 authoring toolchain 的 UI 替换，而不是新的 action authoring path。Branch graph、Timeline editor、inspector、preview 和 tests MUST 继续通过 `CharacterActionDefinitionSO`、Committed Action branch authoring、TimelineNode authoring、project serialized adapter、validator、compiler 和 runtime evaluator 形成同一条数据链路。

#### Scenario: 替换 UI 后编译路径不变
- **GIVEN** 设计者通过 Ref-equivalent Branch Graph 和 Timeline Editor 修改 Dodge branch 与 timeline
- **WHEN** 保存并调用 action definition compiler
- **THEN** compiler MUST 从同一个 `CharacterActionDefinitionSO` 读取 branch、TimelineNode、track、clip 和 payload
- **AND** 输出 MUST 仍是项目正式 `CommittedActionBranchDefinition` 和 `ActionTimelineDefinition` 或批准等价 runtime model
- **AND** MUST NOT 读取 Ref `BaseTree`、`RunnableTree`、`Timeline`、`Track`、`Clip` 或 sample asset

#### Scenario: 无 fallback 和无重复入口
- **WHEN** 检查 editor 菜单、窗口和 adapter
- **THEN** MUST NOT 存在旧 card/list branch editor、旧 half-port timeline editor、Dodge-only branch authoring 正式入口或隐藏 fallback editor path
- **AND** 保留的菜单入口 MUST 指向同一套正式 Ref-equivalent editor shell 和同一份正式 serialized data
