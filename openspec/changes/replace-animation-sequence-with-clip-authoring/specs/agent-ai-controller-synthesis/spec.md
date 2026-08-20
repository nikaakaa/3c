## MODIFIED Requirements

### Requirement: Agent必须通过正式AI Authoring合同修改AI Controller

系统 MUST使用`btsmtl-agent-authoring-document.v4`目录包作为CharacterController与AIController唯一AI-facing编辑合同，并通过显式domain选择AI root。AI package MUST从已有合法`AIControllerDefinition`、AIControllerTree、Graph、AI Blackboard、Perception和受控Character input/request catalog checkout；editable分片 MUST继续表达AI Definition binding、Shared Flow/Value、Observation、Memory与Character input/request intent binding目标结构，不得出现Character AnimationClip Curve或Presentation分片。Graph MUST使用稀疏Node kind、逻辑port、系统anchor和Edge完整目标集合。Document Reconciler MUST降低为统一immutable`AgentMutationPlan`，handler MUST调用正式BTSMTL与AI authoring API。系统 MUST不直接编辑YAML，不建立AI专用package解释器，不保留v1/v2/v3、v15-v17 Patch、bootstrap或双写输出。

#### Scenario: Agent编辑AI接近与攻击结构

- **WHEN** AI package增加Configured Candidate、距离条件、MoveAxis与Attack request结构
- **THEN** Reconciler MUST从统一capability catalog生成immutable Mutation Plan
- **AND** handler MUST通过正式AI authoring API和Graph policy写入

#### Scenario: AI package包含Clip Curve

- **WHEN** AI domain package出现`editable/animation-clips/**`
- **THEN** strict parser MUST按domain capability拒绝整包
- **AND** MUST不把它路由到Character handler

#### Scenario: Agent提交旧合同

- **WHEN** Document链收到v1/v2/v3或v15-v17 Snapshot/Patch payload
- **THEN** 系统 MUST明确报告schema不匹配
- **AND** MUST不转换、双写或兼容解释
