## MODIFIED Requirements

### Requirement: Transition Policy必须按明确owner完整编译

每条PoseState Transition、每个AnimationSlot、每个直接Player下游Inertialization和每个保留的显式BlendStack MUST拥有明确且唯一的Policy owner。Projection Compiler MUST把exact endpoint、Standard Blend或Inertialization、duration、canonical curve、dense Blend Profile、capture/release request layout、PlanId与Revision编入固定Routing Plan。PoseState Transition的curve/profile MUST来自该edge的Blend Mode、Custom Curve Asset与Blend Profile资产；直接Player discontinuity仅在不存在上游transition owner时由Inertialization exact policy提供同类数学。Runtime与Preview MUST只装载匹配Projection revision的计划，不得现场编译、缺省补pair、让下游Policy覆盖edge或使用旧plan。

PoseState Transition的Source Sync Plan MUST由source/target provider的Pose Source Binding共同Sync Group自动派生。没有共同group时 MUST编译None；同一State存在多个可同步候选、共同group角色冲突或Marker topology不完整时 MUST失败。Transition MUST不保存运行同步开关。

#### Scenario: Slot缺少Action到Source Pose规则

- **WHEN** Compiler无法为可达Action endpoint物化`Action -> SourcePoseEndpoint`
- **THEN** Projection Build MUST失败并定位Slot与endpoint
- **AND** Runtime MUST不把Source Pose解释为Empty

#### Scenario: PoseState edge选择Inertialization

- **WHEN** target Ready且edge的compiled route为Inertialization
- **THEN** owner MUST提交typed capture/release请求及可由RuleId解析的edge canonical curve/profile
- **AND** source MUST在正式capture permission前保持相关资源

#### Scenario: Standard Blend进入Native执行

- **WHEN** PoseState edge选择非零Duration的Standard Blend
- **THEN** Native control MUST携带transition elapsed、duration、curve index与profile index
- **AND** Native evaluator MUST按每个Pose Bone的duration multiplier求值target weight

#### Scenario: 同一惯性事件存在两个时间owner

- **WHEN** StateMachine edge已经提供Inertialization数学且下游节点仍声明一份覆盖该事件的temporal rule
- **THEN** Projection Build MUST失败并定位edge与node policy
- **AND** MUST不定义优先级或选择任一配置继续运行
