## ADDED Requirements

### Requirement: Character Equipment 必须由唯一Profile拥有

`CharacterPipelineDefinition` MUST只通过一个`CharacterEquipmentProfile`引用装备authoring。Profile MUST唯一拥有稳定Slot、Route、Equipment、Feature引用与initial Loadout catalog；Definition、RootTree、Prefab和Animation Profile MUST不复制同一装备catalog。Profile缺失时角色 MAY明确声明不支持装备；声明支持装备但Profile缺失或无效 MUST阻止Program发布，MUST不创建默认装备。

#### Scenario: Character声明装备能力

- **WHEN** Corin Definition声明支持Equipment capability
- **THEN** MUST精确引用一个Corin Equipment Profile
- **AND** Compiler MUST不从目录、Prefab名称或RootTree节点猜测装备

#### Scenario: 无装备角色

- **WHEN** 一个角色明确声明不支持Equipment capability
- **THEN** Definition MAY没有Equipment Profile
- **AND** Runtime MUST不为其安装空的隐藏Equipment系统

### Requirement: Slot与Action Route必须分离

Profile MUST使用稳定`EquipmentSlotId`表达装配位置，并使用稳定`EquipmentActionRouteId`表达Character向装备请求的业务能力。每个Route MUST显式绑定Owner Slot、现有Character Input RequestId、request consumption与missing implementation policy。Route MUST不使用显示名、数组index、Feature priority或第一个匹配实现选择Feature。

#### Scenario: 两种主武器实现普通攻击

- **WHEN** Sawblade与Gun Feature都实现`PrimaryAction` Route
- **THEN** MainWeapon Slot当前Equipment MUST唯一决定选中实现
- **AND** RootTree MUST不增加Sawblade/Gun枚举分支

#### Scenario: 两个槽位争用Route

- **WHEN** 一个Loadout使两个active Slot同时提供同一Owner Route
- **THEN** authoring validation MUST失败
- **AND** MUST不按Feature顺序或Priority选择一个实现

### Requirement: EquipmentDefinition 必须只组合稳定身份与类型化值

`EquipmentDefinition` MUST保存稳定EquipmentId、目标SlotId、唯一FeatureId、Feature声明参数的完整类型化值集合和VisualBindingId。参数 MUST按稳定ParameterId及声明value kind校验，缺失、额外、重复、非有限或类型不匹配的值 MUST被拒绝。EquipmentDefinition MUST不保存Graph runtime、Animator、Network Model、Solver或任意Dictionary payload。

#### Scenario: Sawblade提供Motion倍率

- **WHEN** Sawblade Feature声明Scalar参数`MotionScale`
- **THEN** Sawblade Equipment MUST恰好提供一个合法Scalar值
- **AND** Compiler MUST按ParameterId降低为Target typed constant

#### Scenario: Item提供未知参数

- **WHEN** EquipmentDefinition包含Feature未声明的参数
- **THEN** validation MUST拒绝该item
- **AND** Runtime MUST不忽略未知参数

### Requirement: FeatureDefinition 必须是静态链接的authoring单元

`CharacterEquipmentFeatureDefinition` MUST拥有稳定FeatureId/revision、类型化Parameter schema、局部State declaration、Granted Tag、Passive Effect、Presentation Requirement、可选Persistent Graph和Action Route实现。它 MUST只作为Character Compiler输入，不得实现runtime Action/Ability接口、持有mutable gameplay state或启动独立Tick。

#### Scenario: 编译Sawblade Feature

- **WHEN** Compiler发现Corin Equipment Profile引用Sawblade Feature
- **THEN** MUST把Feature graph、catalog与state declaration静态链接进同一Character Program
- **AND** Runtime MUST不加载Feature Unity asset解释业务

#### Scenario: Feature注册任意C#处理器

- **WHEN** Feature尝试通过类型名、反射或Service Locator注册runtime callback
- **THEN** authoring/compiler MUST拒绝
- **AND** MUST不建立装备插件执行旁路

### Requirement: Feature Graph 必须使用inline普通BTSMTL图

Persistent与Route body MUST是Feature serialized owner内的inline普通BTSMTL graph，并继续使用正式Graph/Node/Edge/PropertyPort identity、StateMachine、ConditionRule和Timeline authoring。Feature MUST不创建一次性SubTree asset、`AbilityTree`、`ActionTree`、`AbilityBodyGraph`或装备专用边/端口模型。编辑器 MUST提供从Feature/Route下钻编辑该inline graph的正式入口。

#### Scenario: 编辑Sawblade攻击连段

- **WHEN** 作者打开Sawblade PrimaryAction Route
- **THEN** MUST在Feature拥有的inline graph中下钻现有Attack层级StateMachine
- **AND** MUST复用同一BaseTreeView、Inspector、Undo和Graph mutation API

#### Scenario: Feature引用one-off SubTree

- **WHEN** 作者仅为承载一个Route body创建SubTree asset
- **THEN** Validator MUST拒绝该迁移形状
- **AND** Route body MUST改为Feature inline graph

### Requirement: Feature必须显式声明能力与表现需求

Feature MUST声明其需要的Operation capability、World capability、LayerId、blend/output policy与ProducerId集合；Compiler MUST从实际graph再次推导并核对。缺失声明、声明与实际使用不一致、Target不支持或Presentation Profile不满足 MUST阻止Program/Projection发布。Feature MUST不把未安装能力标记为optional后继续生成部分Program。

#### Scenario: Gun引用Hitscan但项目未安装Combat能力

- **WHEN** Gun Feature graph引用Hitscan operation而当前Operation Set/World不提供该能力
- **THEN** Compiler MUST报告Feature、Route、Node和缺失capability
- **AND** MUST拒绝整个目标Program

#### Scenario: Feature要求UpperBody层

- **WHEN** Feature声明UpperBody Override层及producer集合
- **THEN** Projection build MUST验证唯一Animation Profile中的层与binding
- **AND** Feature MUST不创建自己的Animation Profile

### Requirement: Initial Loadout必须完整且可编译

Profile MUST为每个required Slot提供恰好一个已登记EquipmentId，optional Slot MAY显式为None。Initial Loadout MUST满足Route coverage、Feature capability、参数、Tag/Effect与Visual binding约束，并进入SourceRevision。缺失required item或引用未编译item MUST失败，MUST不选择catalog第一个item。

#### Scenario: Corin初始装备锯刃

- **WHEN** Corin MainWeapon Slot为required
- **THEN** Initial Loadout MUST显式绑定CorinSawblade EquipmentId
- **AND** Program initial state MUST由该identity生成

#### Scenario: Optional槽为空

- **WHEN** Optional OffHand Slot显式配置None
- **THEN** Compiler MUST保留None作为canonical loadout值
- **AND** MUST不补默认盾牌或复制MainWeapon

### Requirement: 装备作者数据必须保持唯一稳定identity

Profile、Slot、Route、Equipment、Feature、Parameter、State declaration及inline graph入口 MUST各自拥有稳定authoring identity。重排列表 MUST不改变identity；复制元素 MUST生成新identity；跨Profile引用或重复identity MUST失败。Source revision MUST覆盖引用资产GUID、内容、graph topology、Timeline、Tag/Effect与Presentation requirement。

#### Scenario: 重排Equipment列表

- **WHEN** 作者只重排Sawblade与Gun item显示顺序
- **THEN** EquipmentId与authoring identity MUST保持
- **AND** canonical catalog MUST按稳定identity排序

#### Scenario: Feature资产GUID变化

- **WHEN** Feature `.meta` identity改变
- **THEN** SourceRevision MUST改变
- **AND** 旧Program MUST被判定过期

