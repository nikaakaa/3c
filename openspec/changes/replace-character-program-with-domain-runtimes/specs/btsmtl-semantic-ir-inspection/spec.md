## MODIFIED Requirements

### Requirement: Unity Editor 必须提供只读 Semantic IR Inspector

系统 MUST为 GameplayAbilityAbility 提供显式打开的只读 Semantic IR Inspector。Inspector MUST从当前 validated `.csir` artifact 显示 Manifest、Operations、Literals、ControlFlow、StateSlots、Scopes、WorldRequests、OutputChannels、CatalogEntries、Producers 和 SourceMap，并支持按 operation code、identity 与精确 source identity 搜索。Inspector MUST不编辑 artifact、authoring 或 generated Ability artifact，也 MUST不在普通 Repaint 时自动运行 Frontend。

#### Scenario: 查看 Corin StateMachine Operation

- **WHEN** 作者从 Corin Ability 打开 Semantic IR Inspector 并选择一个 StateMachine operation
- **THEN** Inspector MUST显示其 handle、operation code、operands、literal references、state slots、control-flow edges 与 source location
- **AND** 显示内容 MUST来自当前 artifact，不得从 Graph 重新推断 operation table

#### Scenario: 当前 IR Cache 过期

- **WHEN** Inspector 发现 cache SourceRevision 与当前 Ability 不一致
- **THEN** MUST显示明确 stale 状态并停止展示旧 tables
- **AND** MUST只通过作者显式 `Compile Semantic IR` 命令调用正式 Frontend，不在 Repaint 隐式刷新




### Requirement: Semantic IR Inspector必须展示结构化Value输入

Unity Semantic IR Inspector MUST直接读取artifact中的Value edge和constant input binding，并提供按target operation查看的Value Inputs section。输出 MUST显示target operation、target port、resolved value kind以及source operation/output port或constant index。Inspector MUST不解析constant identity、反射authoring node或调用Runtime layout来重建缺失关系。

#### Scenario: 在Inspector检查Compare输入

- **WHEN** 作者在Semantic IR Inspector选择Value Inputs并定位一个Compare operation
- **THEN** Inspector MUST分别显示Left与Right的结构化source和resolved kind
- **AND** SourceMap导航 MUST仍定位原Graph port或constant source

## ADDED Requirements

### Requirement: 原生Pose观察不得要求Semantic产物

Pose 观察 MUST直接使用原生图节点／端口／调用实例与已完成结果，Semantic Inspector MUST只负责技能及原有独立内容的编译数据。取消 Character 根后检查入口 MUST显式选择 Ability，旧角色 Program 检查不得伪造为仍可运行。

#### Scenario: 查看没有编译Image的Pose节点
- **WHEN** 作者观察原生 Pose 图的当前结果
- **THEN** 工具 MUST定位真实图实例，不生成隐藏 IR 或 ProgramImage
