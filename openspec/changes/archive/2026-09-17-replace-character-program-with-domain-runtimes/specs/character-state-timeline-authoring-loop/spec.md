## MODIFIED Requirements

### Requirement: Corin Skill Graph必须表达角色Gameplay流程层

AbilityGraph MUST作为对应GameplayAbility的Gameplay执行层，包含输入、Gameplay移动控制、Action StateMachine、Action Timeline和有限Gameplay生命周期内容。角色主线状态与Movement规则属于C# ControlModule及其领域状态；AbilityGraph不得重新承载角色总RootTree。AbilityGraph MUST不平铺Idle、Walk、Run、Start、Stop、Turn等纯表现Pose State，持续Locomotion PoseStateMachine MUST只存在于Presentation Pose Graph。

#### Scenario: 打开Corin Skill Graph

- **WHEN** 作者从Definition的SkillId和EntryGraphAuthoringId打开Corin Skill Graph
- **THEN** 作者 SHOULD看到该Skill的Gameplay执行入口
- **AND** Attack1的Timeline细节 MUST位于Action State下钻图
- **AND** Locomotion Pose State MUST通过Open Presentation导航查看而不成为Skill Graph节点



### Requirement: Corin Locomotion StateMachine必须只控制Gameplay运动

Corin C# Locomotion StateMachine MUST只表达输入准入、Gameplay movement mode、Motion authority、转向与加速度约束、Action对移动的打断以及其它影响Simulation结果的控制。只有具有Gameplay时序、Motion或事实输出的移动行为 MAY使用Timeline；它 MUST不包含只为Idle、WalkStart、WalkLoop、RunStart、RunLoop、RunEnd或MovingTurn动画存在的Timeline playback，不提交BaseLocomotion AnimationSelection，并 MUST不使用ActionOverride停止基础Pose输出。持续Pose选择 MUST由Corin Pose Graph中的Locomotion PoseStateMachine完成。

#### Scenario: 角色开始跑动

- **WHEN** Gameplay接受移动输入并由Motor产生速度
- **THEN** C#控制模块 MUST只更新移动控制和committed Body结果
- **AND** Presentation PoseStateMachine MUST根据Fact选择Start或Locomotion Pose

#### Scenario: FullBody Action活跃

- **WHEN** Action取得Motion authority
- **THEN** Gameplay Locomotion control MUST按正式Motion arbitration让渡或限制Motor
- **AND** MUST不进入ActionOverride动画状态



### Requirement: Corin一次性状态行为必须默认使用inline Graph

Corin Locomotion Gameplay状态行为 MUST由C#控制模块拥有；基础连招状态行为 MUST默认保存为StateNode内部inline graph data。只有多个状态明确复用同一行为图时，作者 MAY显式抽取shared `BaseTreeAsset`。外层Action category MUST不复制leaf数据或创建一次性SubTree asset。

#### Scenario: 下钻Attack1

- **WHEN** 作者下钻Attack1 StateNode
- **THEN** 编辑器 MUST打开该StateNode的inline StateBehaviorSubTree
- **AND** 项目 MUST不要求`Attack1SubTree.asset`
