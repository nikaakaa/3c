## ADDED Requirements

### Requirement: Pose节点作者数据必须按能力使用独立typed payload

每个Pose node kind MUST拥有只包含本能力作者字段的独立typed payload，并由稳定node identity与capability identity关联。系统 MUST删除包含全部node字段的联合体定义、无关nullable字段与按kind读取同一大对象的Inspector路径。节点kind转换 MUST作为显式删除并新建语义处理，不得原地保留旧kind残留字段。

#### Scenario: 创建TwoBoneIK节点

- **WHEN** 作者创建TwoBoneIK
- **THEN** 资产 MUST只保存TwoBoneIK能力声明的chain、effector、joint target、reference与weight字段
- **AND** MUST不保存Sequence、Slot、StateMachine或BlendSpace字段

### Requirement: Pose Compiler必须由typed handler和Pose IR组成

Pose Compiler MUST按capability把typed authoring node降低为规范Pose IR，再从Pose IR生成固定Native Pose Program。每个node kind MUST由独立compiler handler拥有校验与降低逻辑；顶层Compiler MUST只负责拓扑、依赖、IR协调、buffer规划和诊断聚合。Runtime enum、switch与线性operation MAY作为compiled执行层保留，但 MUST不充当作者扩展点。

#### Scenario: 编译PoseLink依赖

- **WHEN** Pose Graph包含多个Pose输入和一个Output Pose
- **THEN** Compiler MUST从typed edge建立PoseLink依赖并生成确定性Pose IR
- **AND** Native Program MUST按依赖顺序线性化且不在Runtime补建作者拓扑

#### Scenario: handler缺失

- **WHEN** capability允许的node kind没有对应compiler handler
- **THEN** Compiler MUST失败并报告node identity与capability
- **AND** MUST不生成默认passthrough operation

## MODIFIED Requirements

### Requirement: Pose Graph必须唯一表达完整表现拓扑

`CharacterAnimationPresentationProfile`引用的Pose Graph MUST通过typed authoring node与typed edge唯一表达`ProgramParameterInput -> PoseStateMachine -> state-local Player -> AnimationSlot -> Pose composition -> TwoBoneIK -> FootPlacement -> OutputPose`。图 MAY包含已注册Presentation capability允许的Player、Blend、Layer、Additive、Parameter、Modify、Subgraph与Input/Output节点。Runtime MUST不在图外补建基础动画、Player、StateMachine、Slot、Blend、IK、FootPlacement或第二Output路径；Pose Graph MUST不保存旧AnimationSelectionInput、MotionMatchingSelectionInput、MarkerSync节点或旧联合体残留字段。

#### Scenario: 检查Corin正式表现链

- **WHEN** 作者打开迁移后的Corin Pose Graph
- **THEN** 图 MUST能沿typed edge追踪PoseState基础Pose、Action Slot、IK和最终输出
- **AND** MUST不显示BaseLocomotion Gameplay AnimationChannel或旧node payload

### Requirement: Pose Graph工作区必须准确映射Authoring、Live与References

正式窗口 MUST复用Graph Authoring Domain Framework的Definition-scoped Navigator、Graph Canvas、Details与Bottom Dock。Details MUST分离Authoring、Live、References与Diagnostics：Authoring只显示当前capability的业务字段并通过Presentation Mutation修改当前owner；Live只读取匹配PoseGraphId、PoseGraphRevision与ProjectionRevision的snapshot；References只读显示Profile binding、source map、Action producer、Rig、Policy和call site；Diagnostics默认折叠内部identity与revision。Live Debug模式下mutation MUST禁用，revision不匹配 MUST显示Stale并清空旧值。

#### Scenario: 查看Locomotion State

- **WHEN** 作者选中Locomotion State的Sequence或BlendSpace Player
- **THEN** Authoring MUST只显示当前Player业务字段且References显示其Pose source binding
- **AND** MUST不显示BaseLocomotion producer或其它node的空字段

#### Scenario: Runtime revision不匹配

- **WHEN** snapshot revision与当前文档或Projection不一致
- **THEN** Live MUST显示Stale
- **AND** MUST不从authoring默认值或Animancer state伪造结果

### Requirement: Pose Graph UI必须保留准确术语和serialized identity

共享UI MAY把正式`PoseStateMachine`显示为Animation State Machine、把`AnimationSlot`显示为Slot，并使用Anim Graph、Sequence Player、Transition Rule、State Alias、Layered Blend Per Bone、Inertialization、Sync Group、Pose Watch和Output Pose。capability与serialized payload MUST保留项目稳定node kind、port identity和entity identity，并明确区分BTSMTL Gameplay StateMachine与PoseStateMachine。内部C#类型名、compiled index和不适用字段 MUST不作为普通作者配置显示。

#### Scenario: 显示FullBodyAction

- **WHEN** Navigator选中FullBodyAction Slot
- **THEN** UI MUST显示Slot业务名、SlotId与绑定的Action AnimationChannel引用
- **AND** MUST不把AnimationChannel序列化为Slot或暴露compiled slot index
