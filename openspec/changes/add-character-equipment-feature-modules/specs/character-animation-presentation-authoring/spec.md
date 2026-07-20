## ADDED Requirements

### Requirement: Animation Profile必须验证Equipment Feature表现需求

唯一`CharacterAnimationPresentationProfile` authoring/compile service MUST对每个已编译Feature验证Required LayerId、blend mode、AvatarMask/output policy和Producer binding覆盖，并把结果纳入Projection source revision。Feature只保存需求identity，不得保存Layer定义、Transition或Animancer对象副本。

#### Scenario: Sawblade只使用Base层

- **WHEN** Sawblade Feature声明Base层producer集合
- **THEN** Profile validator MUST确认每个producer拥有唯一正式binding
- **AND** Feature MUST不复制Transition资源

#### Scenario: Gun要求不存在的UpperBody层

- **WHEN** Gun Feature声明UpperBody但Profile未配置
- **THEN** Projection build MUST失败
- **AND** MUST不把producer重写到Base层

### Requirement: Equipment Visual binding必须属于唯一Equipment Presentation Profile

`CharacterEquipmentPresentationProfile` authoring MUST提供按稳定VisualBindingId配置`ExistingRigObject`与`SpawnedVisualAsset`的唯一入口，并通过正式Rig/Socket binding catalog选择目标。Gameplay Equipment Profile只引用VisualBindingId；Animation Profile、RootTree与Feature graph MUST不直接编辑GameObject路径、Renderer数组或Prefab实例。

#### Scenario: 配置Corin existing weapon

- **WHEN** 作者为CorinSawblade选择ExistingRigObject
- **THEN** Inspector MUST从正式Rig binding catalog选择Renderer set
- **AND** serialized binding MUST不依赖显示名称搜索

#### Scenario: Feature尝试内嵌Animation Layer

- **WHEN** Feature authoring尝试创建Layer或保存Animancer transition副本
- **THEN** authoring validator MUST拒绝
- **AND** 作者 MUST继续使用唯一Presentation Profile Inspector
