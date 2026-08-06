## MODIFIED Requirements

### Requirement: Authoring Capability Catalog必须是UI与Document的唯一语义目录

唯一Framework MUST通过`GraphAuthoringCapabilityCatalog`查询每个domain的Graph kind、node kind、typed payload、静态/动态logical port、数据类型、Pose空间、非Pose瞬时value空间、execution domain、允许连接、资源引用、创建菜单、显示标题、Details provider、Mutation入口与Compiler handler。人工UI、Document exporter、strict parser、Reconciler、Validator和Compiler MUST读取同一Capability；固定port MUST不在实例数据中复制。Capability未声明的字段、port、Pose空间转换、瞬时value lineage或execution domain MUST不被任何入口创建或保存，系统 MUST不按C#类型名、显示名、窗口类型或字段路径重复硬编码能力。

#### Scenario: PredictiveFootPlacement声明Goal输出

- **WHEN** PredictiveFootPlacement Capability声明Component Pose输入与`component.full-body-ik-goals`输出
- **THEN** Canvas、Document、Validator与Compiler MUST从同一Capability识别Pose只读输入、Goals输出及其lineage规则
- **AND** MUST把该节点投影为FinalIK Grounding-backed Goal Source而不是IK solver或Pose backbone节点

#### Scenario: Goal Source声明Value execution domain

- **WHEN** Capability只读取Pose并输出非Pose瞬时value
- **THEN** Catalog MUST要求其显式声明`PureValue`或`WorldAwareValue`
- **AND** Validator与Compiler MUST拒绝用`PurePose`或`WorldAwarePose`掩盖缺失的Pose输出和write set

#### Scenario: FullBodyIK声明动态Goal输入

- **WHEN** FullBodyIK Capability声明一个Component Pose输入、稳定动态Goals输入集合与Component Pose输出
- **THEN** Canvas、Document、Validator与Compiler MUST共同保留每个动态port identity和typed edge
- **AND** MUST共同拒绝重复Effector Slot与跨Rig Goal Set

#### Scenario: Goals连接错误节点

- **WHEN** 作者或Document把`component.full-body-ik-goals`连接到未声明该输入类型的节点
- **THEN** Mutation MUST在写资产前拒绝
- **AND** Compiler MUST继续执行同一规则作为完整性校验

#### Scenario: capability未声明UE PBIK字段

- **WHEN** UI或Document尝试向FullBodyIK写入Preferred Angle或逐骨Rotation Limit
- **THEN** Mutation MUST拒绝该命令并返回稳定诊断
- **AND** MUST不通过SerializedProperty path或扩展字典绕过目录

#### Scenario: Local Pose连接FullBodyIK

- **WHEN** 作者或Document创建Local Pose到FullBodyIK Component Pose输入的edge
- **THEN** 共享connection policy MUST在Mutation前拒绝
- **AND** Compiler MUST继续执行同一规则作为完整性校验

### Requirement: Graph Canvas必须复用统一节点与端口投影

Graph Canvas MUST通过document projection和Capability生成通用Node View、Port View、Edge View、创建菜单、搜索结果与clipboard payload。领域adapter MAY提供业务标题、图标、颜色、状态badge与特殊交互命令，但 MUST不重新实现selection、拖线、框选、复制粘贴、Undo或GraphView生命周期。固定端口 MUST来自Capability；动态端口 MUST由node-local稳定identity声明并接受同一port policy裁决。Pose端口 MUST从stable type投影Local/Component空间颜色和标签；`component.full-body-ik-goals` MUST使用独立稳定类型、标签与颜色。转换节点 MUST作为普通serialized authoring节点显示。Canvas MUST不根据C#类型名、显示名或Compiler operation猜测空间，也 MUST不隐藏插入未序列化节点。

#### Scenario: 作者连接两个Goal Sources与FullBodyIK

- **WHEN** 作者从PredictiveFootPlacement和PoseBoneIKGoals拖出Goals到FullBodyIK两个动态输入
- **THEN** Canvas MUST显示两条Goals edge并保留各自稳定port identity
- **AND** Component Pose MUST分别扇出到两个Goal Sources与FullBodyIK而不得被自动排成串行IK链

#### Scenario: FullBodyIK增加动态Goal输入

- **WHEN** 作者为FullBodyIK增加一个显式Goals输入port
- **THEN** Canvas MUST使用节点局部稳定identity投影并保存该port
- **AND** clipboard与Document往返 MUST保留其类型与连接

#### Scenario: 作者查看空间转换

- **WHEN** Pose Graph包含LocalToComponentPose
- **THEN** Canvas MUST显示Local输入与Component输出
- **AND** Diagnostics MAY显示compiled stage但作者数据 MUST不保存stage index
