## RENAMED Requirements

- FROM: `### Requirement: Feature Graph 必须使用inline普通BTSMTL图`
- TO: `### Requirement: Feature执行内容必须绑定代码控制与技能`

## MODIFIED Requirements

### Requirement: FeatureDefinition 必须是静态链接的authoring单元

FeatureDefinition MUST拥有稳定FeatureId／revision、typed参数、控制模块binding与状态合同、Granted Tag／Passive Effect、Presentation requirement及Route到SkillDefinition的精确绑定。它只作为构建输入，不实现运行时Action接口、不持有可变状态、不启动Tick；旧Persistent／Route角色图字段 MUST迁移删除。

#### Scenario: 配置装备Feature

- **WHEN** Feature声明控制模块和多个技能Route
- **THEN** 构建 MUST验证参数、模块合同、技能闭包和能力
- **AND** MUST不加载Feature对象作为运行实现

#### Scenario: 编译Sawblade Feature

- **WHEN** Compiler发现有效角色引用Sawblade Feature
- **THEN** MUST将控制binding、Skill依赖、catalog和状态合同装入同一角色运行包
- **AND** Runtime MUST不加载Feature Unity资产解释业务

#### Scenario: Feature注册任意C#处理器

- **WHEN** Feature尝试通过类型名、反射或Service Locator注册runtime callback
- **THEN** authoring/compiler MUST拒绝
- **AND** MUST不建立装备插件执行旁路


### Requirement: Feature执行内容必须绑定代码控制与技能

Feature的Persistent／Route角色级Graph入口 MUST被代码控制binding与技能引用替代。技能内容 MUST使用共享BTSMTL图、端口与Timeline模型，并按技能定义的inline／shared ownership编辑。MUST不保留装备专用解释图或Feature callback。

#### Scenario: 下钻装备技能

- **WHEN** 作者从Feature的Route打开技能
- **THEN** MUST进入精确SkillDefinition及其执行图
- **AND** MUST不进入旧Route body Graph

#### Scenario: 迁移Persistent行为

- **WHEN** 原Feature有持续角色级行为
- **THEN** MUST迁入声明状态的代码模块，旧图入口删除

#### Scenario: 编辑Sawblade攻击连段

- **WHEN** 作者打开Sawblade PrimaryAction Route
- **THEN** MUST导航到精确SkillDefinition及其Tree／局部StateMachine／Timeline
- **AND** MUST复用共享画布、Inspector、Undo与Mutation，不恢复Feature角色图

#### Scenario: Feature引用one-off SubTree

- **WHEN** 作者用一次性SubTree资产绕过SkillDefinition承载Route body
- **THEN** Validator MUST拒绝该迁移形状
- **AND** 内容 MUST归属技能定义的inline图或显式共享图，不能恢复旧Feature body


### Requirement: Feature必须显式声明Gameplay能力与路由需求

Feature MUST声明其需要的Operation capability、World capability与Gameplay route所需ProducerId集合；Compiler MUST从代码模块合同和实际技能依赖再次推导并核对。Equipment Feature MUST不声明AnimationChannel、PoseNode、Player policy或动画空间拓扑，也 MUST不把RequiredProducerIds解释为Presentation binding。缺失声明、声明与实际使用不一致或Target不支持 MUST阻止Program发布。Feature MUST不把未安装能力标记为optional后继续生成部分Program。

#### Scenario: Gun引用Hitscan但项目未安装Combat能力

- **WHEN** Gun Feature graph引用Hitscan operation而当前Operation Set/World不提供该能力
- **THEN** Compiler MUST报告Feature、Route、Node和缺失capability
- **AND** MUST拒绝整个目标Program

#### Scenario: Feature尝试要求UpperBody表现层

- **WHEN** Feature声明UpperBody、PoseNode或Player policy
- **THEN** authoring validator MUST拒绝该动画拓扑需求
- **AND** Feature MUST不创建自己的Animation Profile或表现图入口


### Requirement: 装备作者数据必须保持唯一稳定identity

Profile、Slot、Route、Equipment、Feature、Parameter、控制状态字段、代码binding及技能入口 MUST各自拥有稳定authoring identity。重排列表 MUST不改变identity；复制元素 MUST生成新identity；跨Profile引用或重复identity MUST失败。Source revision MUST覆盖引用资产GUID、内容、技能graph topology、Timeline、控制模块语义版本、Tag/Effect与Presentation requirement。

#### Scenario: 重排Equipment列表

- **WHEN** 作者只重排Sawblade与Gun item显示顺序
- **THEN** EquipmentId与authoring identity MUST保持
- **AND** canonical catalog MUST按稳定identity排序

#### Scenario: Feature资产GUID变化

- **WHEN** Feature `.meta` identity改变
- **THEN** SourceRevision MUST改变
- **AND** 旧Program MUST被判定过期
