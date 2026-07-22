## MODIFIED Requirements

### Requirement: Agent必须通过正式AI Authoring合同修改AI Controller

系统 MUST将唯一Agent Snapshot、Patch、Intent与Validation根schema保持为`agent-character-controller-synthesis.v16`，使用显式domain discriminator区分Character Controller与AI Controller根。AI Snapshot MUST输出AIControllerDefinition、AI Graph role、Graph/Node/Edge identity、AI Blackboard declaration、显式候选Actor Perception binding、Character input/request binding与generated AI Program identity。Character Controller Snapshot中的Presentation MUST只读输出Profile、PoseGraph、BlendLibrary与Rig的资产identity和revision、AnimationChannel到PoseSlot映射，以及producer source identity；MUST不输出旧Layer、TransitionLibrary、transition asset或easing字段。Patch MUST不提供PoseGraph、BlendLibrary、Rig、PoseSlot或producer source mutation。Patch MUST通过唯一schema、typed command lowerer与handler catalog创建或修改AI Tree、AI declaration、Shared flow/value、Observation、Memory、Intent节点与BT条件边。`ensure_ai_shared_node` MUST只开放AI Graph允许的Sequence、Selector、Loop、Compare与WaitTicks，并 MUST显式保存LoopStopType或CompareType。`ensure_bt_condition_rule` MUST定位明确的flow edge，创建或配置ConditionRuleGraph与AbortPolicy。Handler MUST调用正式BTSMTL与AI Definition authoring API，MUST NOT直接编辑YAML或建立AI专用宽DTO解释器。系统 MUST不恢复v15及更早reader、converter、字段alias与双写输出。

#### Scenario: Agent创建AI接近与攻击结构

- **WHEN** v16 AI Controller Patch创建Configured Candidate、Loop、距离条件、MoveAxis、Attack request与WaitTicks节点
- **THEN** lowerer MUST生成immutable typed command plan
- **AND** handler MUST通过正式AI authoring API和统一Graph policy写入
- **AND** re-export MUST显示LoopStopType、CompareType、ConditionRuleGraph identity与AbortPolicy

#### Scenario: Agent尝试创建Timeline节点

- **WHEN** AI Graph Patch包含TimelineNode或ActivateActionInstanceNode
- **THEN** preflight MUST拒绝整次事务
- **AND** AI Tree MUST不发生部分修改

#### Scenario: Agent提交旧v15 Patch

- **WHEN** v16安装后Agent收到v15 Patch
- **THEN** 系统 MUST明确报告版本不匹配
- **AND** MUST不转换、双写或兼容解释

#### Scenario: Agent导出Character Presentation身份

- **WHEN** Agent导出Character Controller Snapshot
- **THEN** Presentation MUST显示PoseGraph、BlendLibrary、Rig和AnimationChannel到PoseSlot的正式identity
- **AND** producer MUST只显示AnimationChannel、PoseSlot与source asset identity
- **AND** Snapshot MUST不存在旧Layer或TransitionLibrary字段
