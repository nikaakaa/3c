# btsmtl-semantic-ir-inspection Specification

## Purpose
定义 Unity Editor 对 Ability canonical Semantic IR artifact 的只读检查、身份校验和精确 Authoring SourceMap 导航能力。
## Requirements
### Requirement: Unity Editor 必须提供只读 Semantic IR Inspector

系统 MUST为 CharacterPipelineDefinition 提供显式打开的只读 Semantic IR Inspector。Inspector MUST从当前 validated `.csir` artifact 显示 Manifest、Operations、Literals、ControlFlow、StateSlots、Scopes、WorldRequests、OutputChannels、CatalogEntries、Producers 和 SourceMap，并支持按 operation code、identity 与精确 source identity 搜索。Inspector MUST不编辑 artifact、authoring 或 generated Ability artifact，也 MUST不在普通 Repaint 时自动运行 Frontend。

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


### Requirement: Semantic IR Inspector必须展示结构化Value输入

Unity Semantic IR Inspector MUST直接读取artifact中的Value edge和constant input binding，并提供按target operation查看的Value Inputs section。输出 MUST显示target operation、target port、resolved value kind以及source operation/output port或constant index。Inspector MUST不解析constant identity、反射authoring node或调用Runtime layout来重建缺失关系。

#### Scenario: 在Inspector检查Compare输入

- **WHEN** 作者在Semantic IR Inspector选择Value Inputs并定位一个Compare operation
- **THEN** Inspector MUST分别显示Left与Right的结构化source和resolved kind
- **AND** SourceMap导航 MUST仍定位原Graph port或constant source
