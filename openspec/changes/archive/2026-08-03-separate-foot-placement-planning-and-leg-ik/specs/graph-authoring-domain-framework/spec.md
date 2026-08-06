## MODIFIED Requirements

### Requirement: Authoring Capability Catalog必须是UI与Document的唯一语义目录

唯一Framework MUST通过`GraphAuthoringCapabilityCatalog`查询每个domain的Graph kind、node kind、typed payload、静态/动态logical port、数据类型、Pose空间、非Pose瞬时value空间、execution domain、允许连接、资源引用、创建菜单、显示标题、Details provider、Mutation入口与Compiler handler。人工UI、Document exporter、strict parser、Reconciler、Validator和Compiler MUST读取同一Capability；固定port MUST不在实例数据中复制。Capability未声明的字段、port、Pose空间转换、瞬时value lineage或execution domain MUST不被任何入口创建或保存，系统 MUST不按C#类型名、显示名、窗口类型或字段路径重复硬编码能力。

#### Scenario: FootPlacement声明双输出

- **WHEN** FootPlacement Capability声明`pose.component`与`component.biped-leg-targets`两个输出
- **THEN** Canvas、Document、Validator与Compiler MUST从同一Capability识别两个稳定port及其lineage规则
- **AND** MUST不把targets伪装成Pose、动态字符串port或隐藏Compiler字段

#### Scenario: targets连接错误节点

- **WHEN** 作者或Document把`component.biped-leg-targets`连接到未声明该输入类型的节点
- **THEN** Mutation MUST在写资产前拒绝
- **AND** Compiler MUST继续执行同一规则作为完整性校验

### Requirement: Graph Canvas必须复用统一节点与端口投影

Graph Canvas MUST通过document projection和Capability生成通用Node View、Port View、Edge View、创建菜单、搜索结果与clipboard payload。领域adapter MAY提供业务标题、图标、颜色、状态badge与特殊交互命令，但 MUST不重新实现selection、拖线、框选、复制粘贴、Undo或GraphView生命周期。固定端口 MUST来自Capability；动态端口 MUST由node-local稳定identity声明并接受同一port policy裁决。Pose端口 MUST从stable type投影Local/Component空间颜色和标签；非Pose瞬时control value MUST使用独立稳定类型、标签与颜色。转换节点 MUST作为普通serialized authoring节点显示。Canvas MUST不根据C#类型名、显示名或Compiler operation猜测空间，也 MUST不隐藏插入未序列化节点。

#### Scenario: 作者连接FootPlacement与LegIK

- **WHEN** 作者从FootPlacement拖出Component Pose和Biped Leg Targets到LegIK
- **THEN** Canvas MUST显示两条不同类型edge并保留各自稳定port identity
- **AND** 框选、复制粘贴、Undo与Document往返 MUST保持完整双edge拓扑

#### Scenario: 作者查看空间转换

- **WHEN** Pose Graph包含LocalToComponentPose、FootPlacement与LegIK
- **THEN** Canvas MUST显示Local/Component Pose颜色以及独立targets颜色
- **AND** Diagnostics MAY显示compiled stage但作者数据 MUST不保存stage或workspace index

