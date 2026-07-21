# character-equipment-presentation Specification

## Purpose
定义装备 VisualBinding、Projection payload 与 Unity 外观实例的单向表现链路。
## Requirements
### Requirement: Equipment动画与外观必须由两个唯一Presentation Profile分工

Feature MUST只声明Required Layer、blend/output policy、ProducerId与VisualBindingId；唯一`CharacterAnimationPresentationProfile` MUST继续拥有Layer、AvatarMask、Transition、Animancer binding与output policy，唯一`CharacterEquipmentPresentationProfile` MUST拥有VisualBinding到Rig/Renderer/Prefab/Socket的映射。Feature、Gameplay Equipment Profile、RootTree和Prefab MUST不保存重复Layer、transition或visual binding表。

#### Scenario: Gun要求UpperBody层

- **WHEN** Gun Feature声明UpperBody Additive或Override需求
- **THEN** 唯一Animation Profile MUST提供匹配Layer和producer binding
- **AND** Gun Feature MUST不内嵌第二个Animation或Equipment Presentation Profile

#### Scenario: Layer配置缺失

- **WHEN** Feature要求的LayerId不存在
- **THEN** Projection build MUST失败并定位Feature与Layer
- **AND** Runtime MUST不改用Base层

### Requirement: Equipment动画必须继续通过Timeline producer提交

Feature graph中的动画 MUST由正式Timeline AnimationTrack产生typed producer command，并经过Presentation Queue、Animation Playback Lifecycle与Presenter。Equipment Host、Action runtime和Visual runtime MUST不直接调用Animancer、Animator.Play、CrossFade或修改Layer weight。

#### Scenario: Sawblade攻击播放

- **WHEN** Sawblade Route进入Attack1 Timeline
- **THEN** Timeline MUST提交已绑定的producer command
- **AND** Animation Playback Lifecycle MUST拥有播放与交接状态

#### Scenario: Persistent持枪姿态

- **WHEN** Feature Persistent Graph需要上半身持枪循环
- **THEN** MUST通过对应Layer的Timeline producer表达
- **AND** MUST不由Equipment visual component驱动Animator

### Requirement: 装备外观必须使用稳定显式binding

`CharacterEquipmentPresentationProfile` MUST包含按VisualBindingId索引的Equipment visual catalog，并支持正式`ExistingRigObject`与`SpawnedVisualAsset` binding。Projection compiler MUST把该catalog编译进Presentation Projection。两种binding MUST显式记录Slot、Rig/Prefab、Renderer或Socket binding及local pose/lifecycle；MUST不按GameObject名称、Transform路径模糊匹配、Tag或第一个子物体寻找外观。

#### Scenario: Corin锯刃使用现有Rig对象

- **WHEN** CorinSawblade被装备
- **THEN** ExistingRigObject binding MUST启用已登记Renderer set
- **AND** MUST不搜索名为Weapon_saw的对象

#### Scenario: 未来枪械使用Prefab

- **WHEN** Equipment visual binding kind为SpawnedVisualAsset
- **THEN** runtime MUST在声明Socket binding创建唯一实例并应用local pose
- **AND** missing socket MUST明确失败而不是挂到角色根

### Requirement: Equipment visual selection必须是持久表现状态

每个Slot的committed EquipmentId、VisualBindingId与EquipmentRevision MUST投影为持久Equipment visual selection。Presentation runtime MUST按单调revision替换Slot visual，并能在actor presentation创建、销毁后重建或Character State恢复后从最新committed selection重建状态。Visual selection MUST不依赖一次性Cue必达。

#### Scenario: Actor Presentation重新创建

- **WHEN** Actor Presentation从committed EquipmentState重新创建且MainWeapon为Sawblade revision 8
- **THEN** Equipment visual runtime MUST直接显示revision 8绑定
- **AND** MUST不等待历史Equip Cue

#### Scenario: 旧revision晚到

- **WHEN** Presentation已应用revision 9后收到revision 8 selection
- **THEN** MUST忽略stale selection并保留revision 9
- **AND** MUST记录结构化diagnostic

### Requirement: Equipment visual lifecycle不得反写Gameplay

Prefab实例、Renderer enabled、socket Transform、visual load状态和presentation blend MUST只属于Presentation runtime，不进入CharacterSimulationState、WorldState、StateHash或Action admission。Visual加载失败 MAY使Presentation进入明确invalid状态，但 MUST不改写当前EquipmentId或自动取消Action。

#### Scenario: Weapon Prefab加载失败

- **WHEN** committed Equipment有效但visual prefab无法创建
- **THEN** Presentation MUST报告VisualBindingFailure
- **AND** Gameplay EquipmentState与Action MUST保持权威结果

#### Scenario: Renderer被外部禁用

- **WHEN** 场景脚本修改已绑定Renderer
- **THEN** Equipment gameplay query MUST仍读取CharacterState
- **AND** MUST不把Renderer状态当作Unequipped

### Requirement: Equipment Presentation必须与动画层生命周期分工

Equipment Visual Runtime MUST只管理物体/Renderer/socket生命周期；Animation Playback Lifecycle MUST只管理producer播放与层输出。二者 MUST通过同一EquipmentRevision和Presentation frame ordering协调，但 MUST不互相拥有mutable state或直接调用对方内部实现。

#### Scenario: 换装commit同帧切换动画与外观

- **WHEN** Equipment commit同时输出visual selection和新Feature producer
- **THEN** Presentation Stage MUST先应用同revision外观选择再解析该帧动画输出
- **AND** 两个runtime MUST保持独立所有权

#### Scenario: 动画producer完成

- **WHEN** Equip Timeline动画播放完成
- **THEN** Animation lifecycle MUST退休producer
- **AND** Equipment visual MUST继续保持当前装备显示

