## ADDED Requirements

### Requirement: Character Definition 必须通过两个配置引用安装Equipment能力

`CharacterPipelineDefinition` MUST只保存可选的`CharacterEquipmentProfile`、`CharacterEquipmentPresentationProfile`引用与Equipment capability声明，不得内嵌Slot、Route、Equipment、Feature、Loadout、visual binding或generated catalog。前者唯一拥有Gameplay装备配置，后者唯一拥有Unity visual binding。Inspector MUST把二者作为纯配置引用显示；生成的Program、Projection与catalog详情 MUST进入只读诊断，不得在Definition主Inspector展开为可编辑副本。

#### Scenario: 为Corin安装Equipment Profile

- **WHEN** 作者在Corin Definition启用Equipment capability
- **THEN** Inspector MUST要求精确选择一个Gameplay Equipment Profile和一个Equipment Presentation Profile
- **AND** Slot/item/Feature与visual binding MUST分别在对应正式Inspector中完成

#### Scenario: Definition展开generated装备表

- **WHEN** 作者选中CharacterPipelineDefinition
- **THEN** Inspector MUST不序列化或绘制第二份generated Equipment catalog
- **AND** 编译状态 MAY以只读摘要显示

### Requirement: Character authoring discovery必须支持显式composition roots

Compiler discovery MUST从Definition的RootTree和Equipment Profile声明的全部Feature Persistent/Route graph建立一个canonical composition root集合，并递归解析各自正式Graph/Timeline引用。每个root MUST携带owner、role、Feature/Route identity和稳定source path。Compiler MUST不通过目录扫描、AssetDatabase全局查找、命名约定或运行时Loadout只发现部分Feature。

#### Scenario: 发现未装备Gun Feature

- **WHEN** Gun Equipment已在Corin Equipment Profile允许catalog中但不是initial Loadout
- **THEN** Compiler MUST仍发现并静态链接Gun Feature roots
- **AND** Session运行中切换到Gun MUST不需要重新发现Graph

#### Scenario: Feature graph owner无法解析

- **WHEN** inline graph缺失serialized owner或owner identity不一致
- **THEN** discovery MUST失败并定位Feature/Route
- **AND** MUST不把它当作RootTree子图猜测owner

### Requirement: Core与Feature ActionProfile必须合并为唯一catalog

Definition直接拥有的core ActionProfile与Equipment Feature导出的ActionProfile MUST按稳定ActionId合并、排序并校验为唯一Character Action catalog。Feature ownership MAY作为source metadata进入Program和diagnostics，但 MUST不成为第二个Action registry或运行时membership表。

#### Scenario: Core Dodge与Sawblade Attack编译

- **WHEN** Corin Definition拥有Core Dodge且Sawblade Feature导出Attack
- **THEN** Program MUST生成一个包含二者的Action catalog
- **AND** Action runtime MUST通过同一ActionId lookup执行准入

#### Scenario: 两个Feature重复ActionId

- **WHEN** Sawblade与Gun导出相同ActionId但并非同一共享ActionProfile identity
- **THEN** Compiler MUST拒绝重复定义
- **AND** MUST不按active Feature覆盖catalog条目
