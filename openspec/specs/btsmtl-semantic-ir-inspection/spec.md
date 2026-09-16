# btsmtl-semantic-ir-inspection Specification

## Purpose

定义 Unity Editor 与普通 .NET Reader 对 canonical Gameplay Graph artifact 的只读检查、身份校验和精确 Authoring SourceMap 导航能力。该能力只检查 Graph 产物，不恢复 Character、Ability、Timeline、Pose 或整角色 Program Inspector。

## Requirements

### Requirement: Graph Inspector 必须只读显示正式Graph artifact

系统 MUST 为正式 Gameplay Graph 提供显式打开的只读 Inspector。Inspector MUST 从当前 validated Graph artifact 显示 manifest、节点、边、端口、常量、Graph-owned Blackboard、能力声明和 SourceMap，并支持按 node/edge identity 与 source identity 搜索。Inspector MUST 不编辑 artifact、authoring 或 domain runtime，也 MUST 不在普通 Repaint 时自动运行 Graph preparation。

#### Scenario: 查看Corin StateMachine Graph

- **WHEN** 作者打开 Corin Ability 使用的 Graph 并选择一个 StateMachine 节点
- **THEN** Inspector MUST 显示节点 identity、端口、状态声明、依赖和 source location
- **AND** 显示内容 MUST 来自当前 Graph artifact，不得从运行时对象重新推断

#### Scenario: Graph artifact过期

- **WHEN** Inspector 发现 artifact source revision 或依赖 identity 与当前 Graph 不一致
- **THEN** MUST 显示明确 stale 状态并停止展示旧 artifact 数据
- **AND** MUST 只允许作者显式请求 Graph preparation，不得在 Repaint 隐式刷新

### Requirement: Graph SourceMap 导航必须使用精确Authoring identity

Inspector MUST 使用 Graph artifact SourceMap 的 GraphId、NodeId、EdgeId、DeclarationId、TimelineId、TrackId 与 ClipId 解析 authoring 目标，并复用现有 Graph/Timeline 导航能力。无法精确解析的目标 MUST 显示 unresolved；系统 MUST 不按显示名、数组 index、asset path 片段、最近窗口或第一个匹配对象导航。

#### Scenario: 从Graph节点导航到Timeline Clip

- **WHEN** 作者选择一个带有 TimelineId、TrackId 与 ClipId 的 Graph Timeline reference
- **THEN** Inspector MUST 打开或聚焦对应 Timeline 并选择精确 Clip
- **AND** 同名 Clip 或其它 Timeline 中的相同显示名 MUST 不被选中

### Requirement: 普通DotNet Reader必须显式读取Graph artifact

受版本控制的普通 .NET Reader MUST 使用正式 Graph artifact codec 读取 canonical Graph artifact，并支持稳定 text/JSON 只读输出。Reader MUST 不引用 UnityEngine、Editor assembly 或复制 schema，也 MUST 不把 JSON 输出重新导入为 Graph preparation input。

#### Scenario: DotNet读取Graph artifact

- **WHEN** 普通 .NET 进程读取一个合法 Graph artifact
- **THEN** MUST 通过 canonical codec 校验并输出 GraphId、SourceRevision、GraphHash 与结构摘要
- **AND** MUST 不加载 Unity project、ScriptableObject 或 domain runtime state

#### Scenario: Reader读取非Graph内容

- **WHEN** Reader 收到 TimelineData、Pose binding 或旧整角色编译产物
- **THEN** MUST 返回明确格式错误
- **AND** MUST 不按 magic、名称或默认类型自动切换读取路径

### Requirement: Graph Value Input检查必须展示结构化来源

Graph Inspector 与 portable Reader MUST 直接读取 Graph artifact 中的 Value edge 和 constant input binding，并提供按 Graph operation 查看 Value Inputs 的 section。输出 MUST 显示 target operation、target port、resolved value kind 以及 source operation/output port 或 constant index。工具 MUST 不解析 authoring node、反射 runtime layout 或重建缺失关系。

#### Scenario: 检查Compare输入

- **WHEN** 作者在 Inspector 选择一个 Compare operation 的 Value Inputs
- **THEN** Inspector MUST 分别显示 Left 与 Right 的结构化 source 和 resolved kind
- **AND** SourceMap 导航 MUST 仍能定位原 Graph port 或 constant source
