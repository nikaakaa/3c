## MODIFIED Requirements

### Requirement: Pipeline Definition 必须引用唯一 Animation Presentation Profile

`CharacterPipelineDefinition` MUST引用唯一`CharacterAnimationPresentationProfile`。Profile MUST唯一引用Character Presentation Pose Graph、node-local Blend/Inertialization Policy、Character Animation Rig Definition，保存稳定Timeline producer source binding，以及显式Foot Analysis Mode与Analysis Source identity。当且仅当Pose Graph声明至少一个可达Motion Matching provider时，Profile MUST唯一引用一个Character Motion Matching Profile；该Profile MUST唯一装配Feature Schema、Trajectory/Cost/Search Policy、Database Definition与provider-to-SearchDomain binding。未声明MM provider的Profile MUST不引用MM Profile。Definition、Graph、Timeline、Prefab、Presenter、独立Workbench或Runtime SO MUST不保存这些配置的第二份真相。

#### Scenario: 独立验证配置装配Pose Graph与Motion Matching

- **WHEN** 独立验证Definition引用声明MM provider的正式Animation Presentation Profile
- **THEN** Profile validation MUST精确解析Pose Graph、node-local Policy、Rig、Foot Analysis与MM Profile identity
- **AND** Definition MUST不内联复制slot、database、schema、cost或clip字段

#### Scenario: Corin保持未配置Motion Matching

- **WHEN** Corin Profile没有声明MM provider且没有引用MM Profile
- **THEN** Profile validation MUST按非MM正式配置处理
- **AND** Corin Graph、Timeline、Definition、Projection、Prefab与动画引用 MUST不因本能力改变

#### Scenario: 两个角色复用同一Gameplay Graph

- **WHEN** 两个Definition复用同一BTSMTL Graph但使用不同Rig或Locomotion数据库
- **THEN** 两个Animation Presentation Profile MAY引用不同MM Profile与Projection payload
- **AND** 共享Graph MUST不保存任一角色的Database identity

### Requirement: CharacterAnimationPresentationProfile Inspector 必须是唯一 Presentation 配置入口

系统 MUST在`CharacterAnimationPresentationProfile` Inspector中唯一编辑Pose Graph、node-local Blend/Inertialization Policy、Rig Definition、Timeline producer source binding、Foot Analysis与Motion Matching Profile引用。Inspector MUST从MM Profile进入Schema、Policy、Database、Motion Source Set与Coverage工具，但 MUST不内联复制其数据。重建MM Artifact MUST是带目标Database、Source Set、Clip/sample数量、Foot Artifact状态与内存上界提示的显式可取消重操作；Inspector repaint、selection、普通Compile或Play Mode切换 MUST不触发Build。

#### Scenario: 作者打开独立验证配置的Motion Matching

- **WHEN** 作者从独立验证Animation Presentation Profile进入MM配置
- **THEN** Editor MUST打开Profile引用的真实MM owner
- **AND** Undo/dirty MUST作用于真实Profile、Schema或Database asset

#### Scenario: 作者只修改普通Timeline producer binding

- **WHEN** 作者修改与MM Analysis无关的Presentation binding
- **THEN** Inspector MUST只标记Projection stale
- **AND** MUST不自动重建任一`.mmdb`

## ADDED Requirements

### Requirement: Motion Matching Analysis与Character Build必须显式分离

MM Analysis Builder MUST唯一负责从Clip、Rig、Schema与Foot Artifact生成`.mmdb`；Character Presentation Projection Compiler MUST只消费合法Artifact并发布Runtime payload。两者 MUST拥有独立request/result与diagnostic，不得由Projection Compiler临时分析Clip或由MM Builder编译Semantic IR/Target Program。

#### Scenario: 显式Character Build消费现有数据库

- **WHEN** 所有MM Artifact identity均匹配当前authoring
- **THEN** Character Build MUST直接编译Projection和请求的Numeric Target
- **AND** MUST不重新采样AnimationClip

#### Scenario: Artifact缺失

- **WHEN** Profile引用的Database没有已发布Artifact
- **THEN** Projection Build MUST失败并指向显式MM Build入口
- **AND** MUST不跳过该Database或发布空payload
