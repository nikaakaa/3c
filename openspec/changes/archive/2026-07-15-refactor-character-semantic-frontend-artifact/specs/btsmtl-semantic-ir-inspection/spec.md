## ADDED Requirements

### Requirement: Unity Editor 必须提供只读 Semantic IR Inspector

系统 MUST为 CharacterPipelineDefinition 提供显式打开的只读 Semantic IR Inspector。Inspector MUST从当前 validated `.csir` artifact 显示 Manifest、Operations、Literals、ControlFlow、StateSlots、Scopes、WorldRequests、OutputChannels、CatalogEntries、Producers 和 SourceMap，并支持按 operation code、identity 与精确 source identity 搜索。Inspector MUST不编辑 artifact、authoring 或 generated Program，也 MUST不在普通 Repaint 时自动运行 Frontend。

#### Scenario: 查看 Corin StateMachine Operation

- **WHEN** 作者从 Corin Definition 打开 Semantic IR Inspector 并选择一个 StateMachine operation
- **THEN** Inspector MUST显示其 handle、operation code、operands、literal references、state slots、control-flow edges 与 source location
- **AND** 显示内容 MUST来自当前 artifact，不得从 Graph 重新推断 operation table

#### Scenario: 当前 IR Cache 过期

- **WHEN** Inspector 发现 cache SourceRevision 与当前 Definition 不一致
- **THEN** MUST显示明确 stale 状态并停止展示旧 tables
- **AND** MUST只通过作者显式 `Compile Semantic IR` 命令调用正式 Frontend，不在 Repaint 隐式刷新

### Requirement: Semantic IR SourceMap 导航必须使用精确 Authoring Identity

Inspector MUST使用 artifact SourceMap 的 GraphId、NodeId、EdgeId、DeclarationId、TimelineId、TrackId 与 ClipId 解析 authoring 目标，并复用现有 Graph/Timeline 导航能力。无法精确解析的目标 MUST显示 unresolved；系统 MUST不按显示名、数组 index、asset path 片段、最近窗口或第一个匹配对象导航。

#### Scenario: 从 MotionCurve Operation 导航到 Clip

- **WHEN** 作者选择一个具有完整 TimelineId、TrackId 与 ClipId 的 TimelineMotionCurve operation
- **THEN** Inspector MUST打开或聚焦对应 Timeline 并选择精确 Clip
- **AND** 同名 Clip 或其它 Timeline 中的相同显示名 MUST不被选中

### Requirement: 普通 DotNet Reader 必须显式读取 Semantic IR 与 Program Artifact

受版本控制的普通 .NET Reader MUST使用显式 `semantic-ir` 与 `program` 子命令读取 Core canonical artifact，并支持稳定 text/JSON 只读输出。`semantic-ir` MUST至少输出 header、table counts、operations、control flow、state slots、scopes、producers 与 source map；Reader MUST不引用 UnityEngine、Editor assembly 或复制 schema，也 MUST不把 JSON 输出重新导入为 build input。

#### Scenario: DotNet 读取 Corin Semantic IR

- **WHEN** 普通 .NET 进程执行 `ThirdPersonSimulation.Reader semantic-ir <corin.csir>`
- **THEN** MUST通过 Core codec 校验并输出 Corin ProgramId、SourceRevision、SemanticHash 与 IR table 摘要
- **AND** MUST不加载 Unity project、ScriptableObject 或 Float32 Program Asset

#### Scenario: Reader 命令与 Artifact 类型不匹配

- **WHEN** 作者使用 `semantic-ir` 子命令读取 Program artifact，或使用 `program` 子命令读取 Semantic IR artifact
- **THEN** Reader MUST返回非零退出码并报告明确格式错误
- **AND** MUST不通过 magic 自动切换到另一个命令
