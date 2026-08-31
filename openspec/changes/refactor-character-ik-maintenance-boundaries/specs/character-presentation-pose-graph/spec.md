## MODIFIED Requirements

### Requirement: Goal Sources与FullBodyIK必须使用统一typed目标合同

全部Goal Source MUST发布`CharacterFullBodyIkGoalContribution`，至少携带Frame、Completion、Rig、Producer、Slot、Application、Component空间目标与权重。Foot Placement内部 MUST在Pelvis与既有Foot Reach裁决完成后才发布最终Resolved Pair；Foot Goal Encoder MUST只读取最终Resolved的目标和权重，Pelvis Goal Encoder MUST只读取唯一Pelvis Result。两者 MUST不读取Foot State、Lock Response、Context、Path、Residual或Diagnostics，不得在Goal编码后再次执行Foot可达裁决。

唯一Goal Assembler MUST把合法Contribution规范化为一个`CharacterFullBodyIkGoalSet`，只拥有身份、容量、合法性和重复Slot校验，不接管Foot或Pelvis数学。FBBIK MUST不理解Foot State Context、Contact Patch、Constraint State、Pelvis选择或Diagnostics。

FBBIK腿Effector跨帧稳定策略 MUST由正式FullBodyIK Profile、Rig准备结果和Pending BendHistory决定。Solver MUST不通过搜索FootPlacement SourceKind启用隐藏状态规则；Vendor FinalIK对象内部可变字段不得成为跨帧真相。正式初始化方向只允许来自同一参考姿态准备结果，不增加第二种默认策略。

#### Scenario: FBBIK消费Foot Placement贡献

- **WHEN** FootPlacement已完成请求、Pelvis、Reach与最终结果收口，并由Assembler完成唯一Goal Set
- **THEN** FBBIK MUST只按Goal Application、Slot、Profile、正式Rig准备结果与Pending BendHistory执行求解
- **AND** MUST不回调FootPlacement、读取Ground Path或修改Contact ownership

#### Scenario: 编码最终受限脚目标

- **WHEN** 原Foot请求不可达且Foot收口已按既有政策产生合法的最终受限目标
- **THEN** Encoder MUST只完成正式空间和权重编码，并保持最终Resolved与Contribution一致
- **AND** Assembler与FBBIK MUST不再次决定Foot状态或重新解释原Reach政策
