## MODIFIED Requirements

### Requirement: Blend Policy必须属于显式Blend Stack节点

`CharacterAnimationPresentationProfile` MUST通过Pose Graph中的显式Blend Stack节点引用`CharacterAnimationBlendPolicy`。Policy MUST只保存Stack容量、Stored Pose policy、canonical curve、dense Blend Profile、authoring default和exact override，MUST不保存Inertial technique或residual参数；Compiler MUST只为引用该Policy的节点物化可达endpoint完整table。Inertialization节点 MUST引用独立`CharacterPoseInertializationPolicy`并完整物化自己的endpoint pair。Timeline、BTSMTL Graph、Program、SelectedPosePlayer、Equipment Feature与Prefab MUST不保存第二份transition表。Animancer source backend MUST只复用和采样source playable，不得调用TransitionLibrary、AnimancerLayer.Play、StartFade或FadeGroup决定转场。

#### Scenario: 播放目标producer

- **WHEN** selected producer收到第一份合法sample
- **THEN** 对应显式BlendStack MUST按Projection中的exact source-target transition开始时间混合
- **AND** Animancer source backend MUST只提供该source pose sample

#### Scenario: FullBodyAction淡出到Empty

- **WHEN** FullBodyAction channel提交None且当前action source仍有贡献
- **THEN** FullBodyAction BlendStack MUST使用节点Policy中的source-to-Empty transition连续淡出
- **AND** 系统 MUST不从TransitionLibrary、Animancer state或默认duration补值

#### Scenario: 旧Policy包含Inertial override

- **WHEN** Build读取仍包含Inertial technique的Blend Policy
- **THEN** Build MUST失败并要求迁移到具体Inertialization节点Policy
