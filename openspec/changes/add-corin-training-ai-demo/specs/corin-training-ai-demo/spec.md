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
- **AND** 后续Action admission、Timeline、MotionWarp与动画 MUST由Corin Character Program决定

#### Scenario: Attack节点持续Running

- **WHEN** 同一个SubmitActionRequest activation跨越多个Logic Tick
- **THEN** AI MUST只提交一次离散Attack request
- **AND** 下一次Attack MUST来自显式新activation或重入条件

### Requirement: Corin训练AI资产必须通过Agent v15正式事务生成

Corin Training AI Definition、Tree、Blackboard、Perception和Intent配置 MUST通过Agent v15 Snapshot、dry-run、同Patch apply、re-export与validate流程写入。系统 MUST不直接编辑managed-reference YAML，也 MUST不保留一次性migrator、临时菜单、Patch watcher或旧Neutral fallback配置。

#### Scenario: 创建训练AI资产

- **WHEN** Agent应用Corin Training AI Patch
- **THEN** dry-run与apply MUST消费同一immutable typed plan
- **AND** re-export MUST以stable identity显示全部正式配置

### Requirement: 训练AI不得伪装寻路或Combat闭环

首个训练AI MAY沿目标平面方向直接输出MoveAxis，并 MAY被静态障碍阻挡。它 MUST NOT通过Transform位移、Scene NavMesh临时路径、穿墙、关闭碰撞或teleport绕过WorldSolver。本capability MUST NOT伪造命中、伤害、受击或死亡。

#### Scenario: 训练AI被障碍阻挡

- **WHEN** 训练AI的直接移动被WorldSolver障碍阻挡
- **THEN** 角色 MUST保持正式碰撞结果
- **AND** 系统 MUST不启动隐藏寻路或Transform修正

