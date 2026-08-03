## ADDED Requirements

### Requirement: Corin训练敌人必须由AI Controller驱动正式Character Input

Standalone Gameplay中的Corin训练敌人 MUST使用正式Corin Training AI Controller与AI Control Source。它 MUST继续复用玩家Corin的CharacterPipelineDefinition、CharacterSimulationProgram、WorldSolver、Animation Projection与SimulatedActor Presentation。AI Controller MUST只替换训练敌人的Control Source，MUST NOT创建Enemy Character Runtime或直接驱动Transform。

#### Scenario: 训练AI进入Local Session

- **WHEN** Standalone Session完成roster preparation
- **THEN** 训练敌人Actor registration MUST绑定Corin AI Control Source
- **AND** 其Character Program、World binding与Presentation MUST与既有Corin正式链一致

### Requirement: Corin训练AI必须提供最小可审查行为树

Corin Training AI Tree MUST读取AIPerceptionProfile显式绑定的玩家ActorId，把其committed Body写为ActionTargetSnapshot，在目标距离大于AttackRange时输出朝向目标的MoveAxis，在攻击距离内输出zero MoveAxis并按一次性activation语义提交Attack request。AttackRange、重新请求条件与相关阈值 MUST作为AI Blackboard或Definition中的正式可调authoring数据，不得硬编码在runtime节点类。首版训练AI MUST不声明Team、Faction或按Tag、名称、ActorId前缀推断敌我。

#### Scenario: 目标位于攻击距离外

- **WHEN** committed玩家Body与训练AI的平面距离大于AttackRange
- **THEN** AI MUST输出指向目标的MoveAxis和目标快照
- **AND** 实际移动 MUST由Corin Character Program与WorldSolver完成

#### Scenario: 目标进入攻击距离

- **WHEN** committed玩家Body进入AttackRange且Attack分支获得新activation
- **THEN** AI MUST输出zero MoveAxis并提交一次Attack request
- **AND** 后续Action admission、Timeline与MotionWarp MUST由Corin Character Program决定
- **AND** 动画 MUST由实施时正式Corin Presentation按Action playback或PoseState fact生成

#### Scenario: Attack节点持续Running

- **WHEN** 同一个SubmitActionRequest activation跨越多个Logic Tick
- **THEN** AI MUST只提交一次离散Attack request
- **AND** 下一次Attack MUST来自显式新activation或重入条件

### Requirement: Corin训练AI资产必须通过Agent Document v3正式事务生成

Corin Training AI Definition、Tree、Blackboard、Perception和Intent配置 MUST通过Agent Document v3 package checkout、通用文件工具编辑`editable/**/*.json`、dry-run、同hash apply、re-export与validate流程写入，MUST不并存Patch、v1/v2或局部图工具。系统 MUST不直接编辑managed-reference YAML，也 MUST不保留一次性migrator、临时菜单、Patch watcher或旧Neutral fallback配置。

`AIControllerDefinition` MUST拥有可在Unity domain reload后恢复的正式MonoScript identity。Definition MUST位于与类型同名的独立脚本资产中，MUST NOT依赖同一脚本文件中另一个ScriptableObject的MonoScript引用。

#### Scenario: 应用训练AI Document

- **WHEN** Agent dry-run并以相同document hash应用Corin Training AI Document v3
- **THEN** dry-run与apply MUST消费同一immutable typed plan
- **AND** re-export MUST以stable identity显示全部正式配置

#### Scenario: 审查AI条件控制流

- **WHEN** Agent checkout Corin Training AI完整Document v3
- **THEN** package MUST显示Loop stop类型、Compare类型、ConditionRuleGraph identity和Edge AbortPolicy
- **AND** Agent不得把已经序列化的条件边投影为空

#### Scenario: Unity重新加载脚本域

- **WHEN** 创建训练AI资产后发生Unity domain reload
- **THEN** AssetDatabase MUST继续把Definition解析为`AIControllerDefinition`
- **AND** Agent export MUST继续从同一路径加载该正式根

### Requirement: Corin训练敌人必须使用唯一Corin表现链

Standalone训练敌人 MUST复用与玩家相同的Corin VisualRoot、Rig v3、Animancer与Presentation Projection，并由自己的CharacterPipelineHost和Actor状态驱动。Animator MUST不绑定Animator Controller fallback，也 MUST不申请Root Motion移动所有权或创建训练敌人专用动画链。

#### Scenario: 创建训练敌人表现

- **WHEN** Standalone加载`corin-training-enemy`
- **THEN** Host的Animancer、VisualRoot、Rig v3和Foot Placement MUST全部指向该Actor的Corin表现根
- **AND** 怪兽姿态 MUST只由正式Presentation链输出

#### Scenario: 训练敌人使用正式Foot Placement节点

- **WHEN** 训练敌人Presentation执行包含FootPlacement的staged Pose Plan
- **THEN** FootPlacement MUST消费该Actor同帧上游Component Pose、Rig v3腿链与正式world context
- **AND** MUST NOT使用Passthrough、NoOp、Final IK或图外solver伪造执行完成
- **AND** MUST NOT复用另一Actor的Transform引用或跳过Host的正式Foot Placement合同

### Requirement: 训练AI不得伪装寻路或Combat闭环

首个训练AI MAY沿目标平面方向直接输出MoveAxis，并 MAY被静态障碍阻挡。它 MUST NOT通过Transform位移、Scene NavMesh临时路径、穿墙、关闭碰撞或teleport绕过WorldSolver。本capability MUST NOT伪造命中、伤害、受击或死亡。

#### Scenario: 训练AI被障碍阻挡

- **WHEN** 训练AI的直接移动被WorldSolver障碍阻挡
- **THEN** 角色 MUST保持正式碰撞结果
- **AND** 系统 MUST不启动隐藏寻路或Transform修正
