# character-animation-presentation-authoring Delta

## ADDED Requirements

### Requirement: Presentation作者入口必须唯一装配Secondary Motion Profile

Pose Graph中的`SecondaryMotion`节点 MUST通过强类型对象引用选择唯一`CharacterSecondaryMotionProfile`。现有Character Animation Presentation Profile Inspector与Pose Graph Workspace MUST作为该引用和Profile状态的唯一跨资产入口，并提供Open Profile命令。Profile正文 MUST由专用typed Inspector编辑Group、Physical Bone、Collider和物理参数；Prefab、Timeline、Gameplay Graph、Runtime组件与generated Projection MUST不保存可写副本。Profile、Rig或节点引用变化 MUST只使Projection stale，MUST不自动Build。

#### Scenario: 作者替换Corin Secondary Motion Profile

- **WHEN** 作者在SecondaryMotion节点选择另一个匹配Rig的Profile
- **THEN** typed Presentation Mutation MUST更新唯一节点引用并使Projection stale
- **AND** Corin Prefab MUST不新增或改写手工Magica组件配置

#### Scenario: Profile Rig不匹配

- **WHEN** 节点引用的Secondary Motion Profile与Presentation Rig identity或revision不一致
- **THEN** Inspector与Validator MUST显示精确lineage错误
- **AND** Character Build MUST不生成旧Rig兼容setup

### Requirement: Secondary Motion Build必须从正式Profile生成唯一运行时setup

Character Build MUST从Pose Graph节点、`CharacterSecondaryMotionProfile`、Presentation Rig和唯一Global Settings生成dense group/collider descriptor、Magica setup payload或PreBuild artifact、固定容量与依赖hash，并把它们编入Presentation Projection。Runtime Prefab上的Magica组件正文 MUST不成为输入真相。打开窗口、选择Profile、保存资产、Domain Reload或进入Play Mode MUST不自动生成setup；只有显式Character Build MAY发布新产物。

#### Scenario: 修改裙摆Collider后未Build

- **WHEN** 作者修改Corin Profile中的Upper Leg Collider尺寸但尚未显式Build
- **THEN** Inspector与Preview MUST显示Projection stale并停止消费旧setup
- **AND** Runtime MUST不从旧Magica组件或默认Collider继续运行

### Requirement: Secondary Motion UI必须使用业务分组而不是模型拆分术语

Profile Inspector MUST按Skirt、Hair、Accessory等业务Group显示root chain、controlled bone、Collider、Animation Follow、Simulation Weight与约束，并以Rig Bone业务名和Unity资产对象作为主要作者信息。UI MUST不要求拆分SkinnedMesh、创建Animator Layer、输入Transform路径、Magica team id、GUID、dense index或hash。武器机械Group MUST显示其由Action/Clip动画拥有的排除诊断。

#### Scenario: 编辑Corin Skirt Group

- **WHEN** 作者打开Corin Secondary Motion Profile的Skirt Group
- **THEN** Inspector MUST显示八条root chain、24根controlled bone和绑定腿部Collider
- **AND** MUST不要求选择`Corin_body`网格子对象或创建裙摆Renderer
