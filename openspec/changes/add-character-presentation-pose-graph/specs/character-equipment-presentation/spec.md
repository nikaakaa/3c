## MODIFIED Requirements

### Requirement: Equipment Gameplay route与表现配置必须分工

Equipment Feature MUST只声明Operation capability、World capability、Gameplay route所需ProducerId与VisualBindingId。RequiredProducerIds MUST只校验Gameplay route完整性，不得表达AnimationChannel、PoseNode、blend/output policy或动画空间拓扑。唯一`CharacterAnimationPresentationProfile` MUST拥有Pose Graph、节点Policy、Rig与producer source binding；唯一`CharacterEquipmentPresentationProfile` MUST拥有VisualBinding到Rig/Renderer/Prefab/Socket的映射。Feature、Gameplay Equipment Profile、RootTree和Prefab MUST不保存重复Pose Graph、transition或visual binding表。

#### Scenario: Gun未来需要UpperBody表现

- **WHEN** 未来Gun业务需要动态UpperBody Pose实现
- **THEN** 系统 MUST由独立change定义Gameplay输入、Pose Graph/Projection schema与Runtime生命周期
- **AND** 当前Gun Feature MUST不声明兼容Layer、临时PoseNode或内嵌Presentation Profile

#### Scenario: Gameplay producer route缺失

- **WHEN** Feature声明的RequiredProducerId无法由其Gameplay route提供
- **THEN** Program build MUST失败并定位Feature与producer
- **AND** Presentation MUST不为Equipment创建AnimationChannel、Selection Input或fallback producer

### Requirement: Equipment动画必须继续通过Timeline producer提交

Feature graph中的动画 MUST由正式Timeline AnimationTrack产生typed producer command，并经过Presentation Queue、Animation Playback Lifecycle、Animation Selection、显式Player与Pose Graph Plan。Equipment Host、Action runtime和Visual runtime MUST不直接调用Animancer、Animator.Play、CrossFade或修改Player/Graph weight。

#### Scenario: Sawblade攻击播放

- **WHEN** Sawblade Route进入Attack1 Timeline
- **THEN** Timeline MUST提交已绑定的producer command
- **AND** Animation Playback Lifecycle MUST拥有播放与交接状态

#### Scenario: Persistent持枪姿态

- **WHEN** Feature Persistent Graph需要上半身持枪循环
- **THEN** MUST通过正式AnimationChannel中的Timeline producer表达
- **AND** MUST不由Equipment visual component驱动Animator

### Requirement: Equipment Presentation必须与动画播放生命周期分工

Equipment Visual Runtime MUST只管理物体、Renderer与socket生命周期；Animation Playback Lifecycle、显式Player节点与Pose Graph Plan MUST分别管理producer寿命、连续性、空间合成和最终输出。两条表现链 MAY通过同一EquipmentRevision和Presentation frame ordering协调，但 MUST不互相拥有mutable state或直接调用对方内部实现。

#### Scenario: 换装commit同帧切换动画与外观

- **WHEN** Equipment commit同时输出visual selection和新Feature producer
- **THEN** Presentation Stage MUST先应用同revision外观选择再解析该帧动画输出
- **AND** 两条表现链 MUST保持独立所有权

#### Scenario: 动画producer完成

- **WHEN** Equip Timeline动画播放完成
- **THEN** Animation lifecycle MUST退休producer
- **AND** Equipment visual MUST继续保持当前装备显示
