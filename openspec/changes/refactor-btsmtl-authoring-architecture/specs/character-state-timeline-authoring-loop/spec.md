## RENAMED Requirements

- FROM: `### Requirement: Corin RootTree 必须只表达角色主流程层`
- TO: `### Requirement: Corin角色主流程必须由代码控制与技能目录表达`

- FROM: `### Requirement: Corin Locomotion StateMachine必须只控制Gameplay运动`
- TO: `### Requirement: Corin移动控制必须由代码模块产生Gameplay运动`

- FROM: `### Requirement: Corin基础连招必须使用Action StateMachine和Timeline编排`
- TO: `### Requirement: Corin基础连招必须由代码选择技能并统一执行`

- FROM: `### Requirement: Corin一次性状态行为必须默认使用inline Graph`
- TO: `### Requirement: Corin技能私有图必须默认使用inline数据`

## MODIFIED Requirements

### Requirement: Corin资产迁移必须通过正式Agent Document事务

有限Action Timeline、Gameplay Graph、Blackboard、Presentation Binding、Pose Graph、Locomotion Sync Group、AnimationClip注册Curve与旧Locomotion数据清理 MUST通过`btsmtl-agent-authoring-document.v5`的`checkout_document -> editable修改 -> dry_run_document -> apply_document(expected_document_hash) -> validate`唯一事务完成。Reconciler MUST把全部目标降低为同一immutable Mutation Plan和Undo事务。实现 MUST不直接修改Unity YAML、不恢复旧Patch链、不创建一次性migrator或第二mutation service。Document apply MUST只修改authoring并标记生成物Stale，不得自动Build。

#### Scenario: 应用Corin Document

- **WHEN** dry-run成功并返回exact document hash
- **THEN** apply MUST消费同一hash并在一个Undo事务保存Clip、Profile、Pose Graph、Timeline与Gameplay authoring
- **AND** 成功后反向导出Package MUST为Clean
- **AND** Document MUST不再包含Sequence、Marker、BaseLocomotion、ActionOverride或旧Selection字段


### Requirement: Corin生成产物必须显式重建

只有AnimationClipAnalysisInputHash或Phase Validation Descriptor实际变化时，迁移才 MUST按精确身份显式重建对应Foot Analysis Artifact；本次技能／控制迁移 MUST复用仍精确匹配的Artifact，不重新改写已正确的Clip Curve或Pose资产。相关作者变化仍经Document v5完成，Curve写回不得使无关Artifact stale。Document apply成功后，Presentation Projection、Float32 Program wrapper与Fixed Program wrapper MUST通过精确Definition的正式显式Build入口按依赖顺序重建。Program MUST不包含BaseLocomotion animation producer；Projection MUST包含PoseStateMachine、Clip/BlendSpace state-local source、Locomotion Phase endpoint、AnimationSlot、完整Rig v4与唯一ordered Pose Plan。产物 MUST共享匹配的source revision闭包，不得自动Build、部分发布或使用旧wrapper、Clip plan或Phase relation。

#### Scenario: 迁移后显式Build

- **WHEN** Corin新schema Foot Analysis已Ready且Document apply成功
- **THEN** 作者 MUST显式触发Projection、Float32 Build与Fixed Build
- **AND** 任一阶段失败 MUST保留明确typed diagnostic且不得发布混合revision


### Requirement: Corin角色主流程必须由代码控制与技能目录表达

Corin输入、Gameplay移动模式、动作选择和角色级切换 MUST由明确C#控制模块组织。作者入口 MUST显示控制binding／参数和技能目录，不再提供角色RootTree。具体技能拥有Tree／Timeline等内容，持续Locomotion PoseStateMachine仍只属于Presentation。

#### Scenario: 打开Corin RootTree

- **WHEN** 作者打开迁移后的Corin角色入口
- **THEN** MUST显示代码控制配置和明确技能目录
- **AND** Attack1细节 MUST导航到其SkillDefinition
- **AND** Locomotion Pose仍通过Open Presentation查看，不重建角色RootTree


### Requirement: Corin移动控制必须由代码模块产生Gameplay运动

Corin普通移动的输入准入、movement mode、Motion authority、转向与加速度约束及动作影响 MUST迁入C#控制模块并使用同一状态事务和Motion规则。需要Gameplay时序的移动技能可使用Timeline；纯Idle／Start／Loop／Stop／Turn动画仍由Pose source承担，不生成BaseLocomotion producer或ActionOverride。

#### Scenario: 角色开始跑动

- **WHEN** Gameplay接受移动输入并由Motor产生速度
- **THEN** C#控制 MUST更新移动状态与正式运动意图
- **AND** Presentation PoseStateMachine仍根据committed Fact选择Pose

#### Scenario: FullBody Action活跃

- **WHEN** Action取得Motion authority
- **THEN** 代码移动控制 MUST按同一Motion arbitration让渡或限制Motor
- **AND** MUST不进入ActionOverride动画状态


### Requirement: Corin基础连招必须由代码选择技能并统一执行

Corin的Attack1至Attack5、DodgeBack和DodgeForward MUST成为明确技能定义，保留原ActionProfile、Tree／Timeline、窗口、Motion和输出内容。角色代码 MUST选择技能并处理连段、取消与replacement，不再保留外层None／Attack／Dodge图及角色级nested combo状态机。技能内部仍可使用局部状态机和子图，准入、source stop与Action生命周期规则必须保持。

#### Scenario: Attack1进入Attack2

- **WHEN** Attack1的ComboAccept active、存在Attack request且Attack2 admission成立
- **THEN** source MUST先按统一Action lifecycle关闭
- **AND** target MUST消费request并创建新的ActionInstance
- **AND** FullBodyAction channel MUST提交Attack2 exact playback


#### Scenario: Attack被Dodge打断

- **WHEN** Gameplay规则允许Dodge替换Attack
- **THEN** source Action Timeline MUST按统一Runnable与Action lifecycle停止
- **AND** AnimationSlot MUST只处理Attack playback到Dodge playback的Pose transition


#### Scenario: Action自然结束

- **WHEN** 当前技能实例没有更高优先级replacement且Timeline完成
- **THEN** Gameplay MUST完成Action lifecycle并释放Motion authority
- **AND** FullBodyAction AnimationSlot MUST过渡回同帧当前Locomotion Source Pose


### Requirement: Corin技能私有图必须默认使用inline数据

Corin技能入口与私有子图 MUST由SkillDefinition或明确调用owner持有inline数据。真实复用时可显式Extract Shared，并保留稳定调用点与独立实例状态。角色移动和动作选择代码不得为了作者格式而重新生成StateNode或一次性SubTree资产。

#### Scenario: 下钻Attack1

- **WHEN** 作者打开Attack1技能
- **THEN** 编辑器 MUST打开其明确入口Tree与inline子图
- **AND** MUST不要求旧Attack1 StateNode或一次性Attack1SubTree资产


### Requirement: Corin有限Action Timeline必须默认使用inline Timeline

Corin有限Action与真正包含Gameplay时序的移动行为 Timeline MUST默认保存为对应TimelineNode私有的inline TimelineData。纯Idle、Start、Loop、Stop与Turn动画 MUST使用Presentation Pose source，MUST不为其创建TimelineNode或inline Timeline。Compiler MUST把保留的inline/shared Timeline编译为同一不可变Program与Action Playback合同，不得创建runtime clone。

#### Scenario: 下钻Attack1 Timeline

- **WHEN** 作者从Attack1技能图打开TimelineNode
- **THEN** Timeline Editor MUST显示Animation、Motion与Decision Tree tracks
- **AND** playback MUST属于Attack1 Action Context

#### Scenario: 编辑Run Pose

- **WHEN** 作者需要替换持续Run动画或marker
- **THEN** 必须导航到Presentation Profile的Run Pose source binding
- **AND** MUST不创建RunLoop inline Timeline


### Requirement: Corin Action Timeline Window必须由owner-local事实表达

Attack1至Attack5、DodgeForward和DodgeBack的inline Timeline MUST以Decision TreeClip和owner-local Bool Frame declaration表达Hit、IFrame、ComboAccept、RecoveryEarly、RecoveryLate与RecoveryOpen。ActionWindow projection MUST保留Action Context、WindowId、Digest、phase和frame range；角色代码、技能局部ConditionRuleGraph与EndFrame fact MUST消费同一candidate。系统 MUST不建立Root-owned per-state window key、WindowTrack、专用submit node、cache或registry。

#### Scenario: Attack窗口

- **WHEN** 作者打开任一Attack inline Timeline
- **THEN** Hit、ComboAccept、RecoveryEarly与RecoveryLate MUST位于该owner
- **AND** projection MUST指向当前ActionInstance


### Requirement: Corin Gameplay只能提交有限Action playback

Corin Gameplay MUST只为FullBodyAction及其它有限Gameplay-owned channel提交唯一playback selection。角色代码与技能执行 MUST在同一逻辑事务完成移动、动作选择／打断和Action channel所有权；持续BaseLocomotion MUST不再是AnimationChannel，也 MUST不提交AnimationPlaybackId。Pose Graph MUST从Presentation Fact选择Locomotion Pose，并通过FullBodyAction AnimationSlot组合有限Action。

#### Scenario: Locomotion正常运行

- **WHEN** 当前没有FullBodyAction且Body正在移动
- **THEN** Program MUST不提交BaseLocomotion selection
- **AND** PoseStateMachine MUST从movement fact生成基础Pose

#### Scenario: 同tick切换Locomotion与Action

- **WHEN** 同一logic tick内Gameplay movement mode和Attack ownership均变化
- **THEN** Program MUST只提交最终Gameplay Body事实与FullBodyAction playback
- **AND** Locomotion Pose source MUST由同帧Presentation Fact独立选择
