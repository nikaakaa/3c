# btsmtl-agent-authoring-document-sync Specification

## ADDED Requirements

### Requirement: Document唯一生命周期不得排除正式人工作者工作区

Agent Authoring Document MUST继续作为AI修改的唯一目录包与checkout、rebase、dry-run、apply、validate生命周期，但 MUST不被解释为人工作者的唯一UI。Animation Workspace人工命令与Document Reconciler MUST分别把交互或目标状态降低为同一种typed Presentation Mutation，并共享Capability、handler、Validator、asset identity allocator与Unity authoring truth。人工UI修改Unity authoring后，现有Document MUST按live revision与hash规则进入`TreeDirty`；系统 MUST不自动export、rebase、apply或覆盖Document editable分片。

#### Scenario: 人工UI修改Equipment mapping

- **WHEN** 已checkout Document对应的Profile在Animation Workspace被正式mutation修改
- **THEN** Document同步状态 MUST成为TreeDirty并在下次生命周期命令中报告live authoring变化
- **AND** 系统 MUST不自动重写package或把旧Document目标重新apply到Unity

#### Scenario: UI与Document产生同一Linked Pose目标

- **WHEN** 两条入口都创建相同Interface约束下的Implementation Entry Graph闭包
- **THEN** 最终Unity树 MUST满足相同identity、owner、port、revision与validation语义
- **AND** Document额外拥有的hash、dry-run与目录发布职责 MUST不进入人工UI presenter
