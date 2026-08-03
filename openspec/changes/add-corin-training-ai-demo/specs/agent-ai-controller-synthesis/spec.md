## MODIFIED Requirements

### Requirement: Agent必须通过正式AI Authoring Document修改AI Controller

系统 MUST将唯一Agent外部schema保持为`btsmtl-agent-authoring-document.v3`目录包，使用显式domain discriminator区分CharacterController与AIController根。AI Document editable MUST表达AIControllerDefinition、稀疏AI Graph、AI Blackboard、显式候选Actor Perception binding、Observation、Memory与Character Input/Request intent binding；context MUST只读输出能力目录、受控Character合同与generated AI Program identity。AIController domain只读引用受控Character的Presentation capability，不复制CharacterController domain中由共享Presentation Mutation拥有的editable Profile、PoseGraph、Pose State、source provider或AnimationSlot。Document Reconciler MUST从完整目标实体集合生成immutable `AgentMutationPlan`，handler MUST调用正式BTSMTL与AI Definition authoring API。系统 MUST只提供五个生命周期MCP并由AI使用通用文件工具编辑JSON，MUST不提供局部图工具、Patch、Intent、Macro、bootstrap、旧schema reader、converter、字段alias或双写输出。

#### Scenario: Agent创建AI接近与攻击结构

- **WHEN** AI编辑Document创建Configured Candidate、Loop、距离条件、MoveAxis、Attack request与WaitTicks实体
- **THEN** Reconciler MUST生成immutable AgentMutationPlan
- **AND** handler MUST通过正式AI authoring API和统一Graph policy写入
- **AND** apply后的canonical Document MUST显示真实stable identity、LoopStopType、CompareType、ConditionRuleGraph identity与AbortPolicy

#### Scenario: Agent投影AI Shared节点与条件边

- **WHEN** AI Graph使用Shared Sequence、Selector、Loop、Compare和ConditionRuleGraph组织AI节点
- **THEN** Document MUST按正式Node kind和逻辑port投影Shared节点
- **AND** ConditionRuleGraph MUST保留owner edge identity、AbortPolicy和完整内部Graph
- **AND** checkout、dry-run、apply与canonical re-export MUST不把条件边降为空节点或AI专用旁路

#### Scenario: Agent尝试创建Timeline节点

- **WHEN** AI Document的AI Graph包含TimelineNode或ActivateActionInstanceNode
- **THEN** dry-run MUST拒绝整次事务
- **AND** AI Tree MUST不发生部分修改

#### Scenario: Agent提交旧Patch

- **WHEN** Agent提交v15-v17 Snapshot、Patch、operation或`patch_json`
- **THEN** MCP bridge MUST报告未知工具或参数
- **AND** MUST不转换、双写或兼容解释

#### Scenario: Agent读取Character Presentation身份

- **WHEN** Agent checkout Character Controller Document
- **THEN** context MUST显示PoseGraph、Rig、Foot Analysis、AnimationChannel到SelectionInput与Blend Space摘要的正式identity
- **AND** producer MUST只显示AnimationChannel、SelectionInput与source asset identity
- **AND** editable MUST不存在Presentation mutation实体
