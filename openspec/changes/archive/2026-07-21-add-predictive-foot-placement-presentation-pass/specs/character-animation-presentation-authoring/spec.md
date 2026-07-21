# character-animation-presentation-authoring Specification

## MODIFIED Requirements

### Requirement: CharacterAnimationPresentationProfile Inspector 必须是唯一动画播放配置入口

系统 MUST在 `CharacterAnimationPresentationProfile` Inspector 中唯一编辑 Layer catalog、TransitionLibrary 引用与 producer presentation binding。CharacterPipelineDefinition Inspector MUST只编辑 Profile 引用并提供打开该资产的导航，不得保存或显示这些数据的可写副本。系统 MUST不提供独立 Animation Presentation 窗口；Graph Inspector和StateMachine Editor MUST不提供这些数据的可写副本。Timeline Editor继续独占LayerId、clip、time、loop、ease、producer内部Weight以及Animation Clip的单一Foot Placement Weight曲线。独立`CharacterFootPlacementProfile` Inspector MAY只编辑Trace、Contact、Prediction、Constraint、Pelvis、Rotation和Smoothing等角色级算法参数，但 MUST不保存producer policy或复制Timeline曲线，也 MUST不创建第三个Animation/Pose图窗口。

#### Scenario: 编辑 producer transition

- **WHEN** 作者在 CharacterAnimationPresentationProfile Inspector 选择一个 animation producer
- **THEN** 作者 MUST能查看其 layer、stable key 与 Animancer transition binding
- **AND** transition 细节 MUST通过 Animancer 正式 authoring API 或窗口编辑
- **AND** Graph/Timeline 逻辑资产 MUST保持不变

#### Scenario: 编辑 Timeline clip

- **WHEN** 作者需要修改 clip 时间、ease 或 Weight
- **THEN** CharacterAnimationPresentationProfile Inspector MUST导航到独立 Timeline Editor
- **AND** MUST不复制这些字段

#### Scenario: 同时观察逻辑与 Timeline

- **WHEN** 作者从 CharacterAnimationPresentationProfile Inspector 打开来源 Graph 和 Timeline
- **THEN** Graph 与 Timeline MUST保持两个可同时观察的独立窗口
- **AND** Timeline MUST不进入 Graph 页签栈
- **AND** 系统 MUST不创建第三个 Presentation 窗口

#### Scenario: 调节Run的Foot Placement权重

- **WHEN** 作者在Timeline选中Run Animation Clip并编辑单一归一化Foot Placement Weight曲线
- **THEN** dirty owner MUST是该Timeline且Presentation Projection MUST更新
- **AND** AnimationPresentationProfile、FootPlacementProfile、Graph与Gameplay Program MUST保持不变
