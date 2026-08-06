## MODIFIED Requirements

### Requirement: PresentationFrame必须输出完整最终Pose Plan结果

PresentationFrame MUST消费committed Body/Intent、构造typed Presentation Fact，并消费完整有限Action Selection batch与Parameter page；随后按Projection编译的ordered Pose/Value DAG执行PoseState selection、State source demand/capture、Action playback、Marker time resolve、AnimationSlot、Transition Routing、Local Pose composition、显式Local/Component转换、Component Pose控制、从同一Component Pose分支执行FinalIK Grounding-backed PredictiveFootPlacement Goal Source与PoseBoneIKGoals、把全部Goals汇聚到唯一pure pose FullBodyIK、执行后续Pose stage与FinalPublication。只有唯一OutputPose及全部必需stage完成后才可由唯一final writer发布`FinalAnimationPoseFrame`并推进Camera；Goal Source调度 MUST不产生中间骨骼Pose。任一Fact、source、MarkerSync、Player、Slot、转换、Pose operation、Grounding query、Predictive Extension、Goal validation、FinalIK binding或FullBodyIK solver失败 MUST阻止部分最终结果发布，不得沿用上一帧、只发布pelvis pre-solve结果或绕过节点。

#### Scenario: Foot Goals与FullBodyIK Pose不匹配

- **WHEN** 同帧Goal Set CompletionIdentity或Rig revision与FullBodyIK Component Pose输入不一致
- **THEN** PresentationFrame MUST阻断FullBodyIK、后续stage和FinalPublication
- **AND** MUST不使用上一次Goals或按节点顺序猜测配对

#### Scenario: 完整FullBodyIK链成功

- **WHEN** PredictiveFootPlacement与PoseBoneIKGoals发布合法Goals且FullBodyIK完成一次全身求解
- **THEN** FinalAnimationPoseFrame MUST包含FullBodyIK输出及全部后续Pose操作
- **AND** Runtime MUST不保留第二Foot Placement、TwoBoneIK、LegIK或图外FinalIK结果

#### Scenario: Action等待第一Selection sample

- **WHEN** Program已经选择Action但Presentation尚无合法Selection sample
- **THEN** AnimationSlot MUST按compiled pending/availability policy处理
- **AND** Locomotion PoseState MUST继续来自同帧Fact而不是历史BaseLocomotion selection

### Requirement: Pipeline domain debug 必须进入统一 Trace

Input、ingress、Program operation、StateMachine、Timeline、Blackboard、WorldRequest/Result、Action、Effect、commit、Animation、FinalIK Grounding、Predictive Extension、FullBodyIK和Camera diagnostics MUST进入统一structured Trace/view model。Inspector MUST不遍历旧stage、FinalIK组件、TwoBoneIK/LegIK私有结果或runtime service私有集合形成平行调试链。Grounding、Predictive Extension与FullBodyIK trace MUST只读取正式Presentation snapshot，不得重新执行地面查询或solver。

#### Scenario: Trace查看FullBodyIK失败

- **WHEN** Actor因FullBodyIK mapping failure进入Faulted
- **THEN** 统一Trace MUST关联Pose Node、Rig、Goal completion、backend identity与typed failure
- **AND** Inspector MUST不读取FinalIK MonoBehaviour或Animator Transform重建诊断
