本差量的源码与行为对照固定为用户指定提交`ad3527e103cc3235a63e8a1c1dbd26df5155e0ba`；233436仅是对应回放证据，不能用当前HEAD或采样目录替代源码基线。

## MODIFIED Requirements

### Requirement: Goal Sources与FullBodyIK必须使用统一typed目标合同

全部Goal Source MUST发布`CharacterFullBodyIkGoalContribution`，至少携带Frame、Completion、Rig、Producer、Slot、Application、Component空间目标与权重。Foot Placement内部 MUST在既有Pelvis响应、可达观察与原Landing完成判断后才发布最终Resolved Pair；Foot Goal Encoder MUST只读取最终Resolved的目标和权重，Pelvis Goal Encoder MUST只读取唯一Pelvis Result。两者 MUST不读取Foot State、Lock Response、Context、Path、Residual或Diagnostics，不得恢复业务层Reach夹紧或末端夹脚。

唯一Goal Assembler MUST把合法Contribution规范化为一个`CharacterFullBodyIkGoalSet`，继续由现有入口拥有身份、容量、合法性和重复Slot校验，不接管Foot或Pelvis数学，也不为内部请求分型逐层复制相同检查。FBBIK MUST不理解Foot State Context、Contact Patch、Constraint State、Pelvis选择或Diagnostics。

FBBIK腿Effector跨帧稳定策略 MUST由正式FullBodyIK Profile、Rig准备结果和Pending BendHistory决定。Solver MUST不通过搜索FootPlacement SourceKind启用隐藏状态规则；Vendor FinalIK对象内部可变字段不得成为跨帧真相。正式初始化方向只允许来自同一参考姿态准备结果，不增加第二种默认策略。

233436组合中已保留的可靠动画有符号膝向运输 MUST保持，Stable继续保存运输前动画方向，Applied继续保存实际请求，退化分支维持既有政策。结构迁移 MUST不恢复可靠动画半球强翻、SmoothKnee尾段或改变Bend权重来掩盖已知深折叠。

Goal对求解要求权威，Solved Pose对本次Solver输出权威，Physical Result对本次实际写入权威；三者 MUST保留同Completion阶段区别，不得互相代替或反推覆盖源Pose。Solver MUST只写Pending Pose及正式BendHistory，Writer MUST唯一写骨骼；Encoder、Assembler、Root调度和诊断不得取得额外Pose写入权。已有特殊Goal应用数学保持本change范围约定。

#### Scenario: FBBIK消费Foot Placement贡献

- **WHEN** FootPlacement已完成初步请求、Pelvis响应、可达观察和原Landing完成判断，并由Assembler完成唯一Goal Set
- **THEN** FBBIK MUST只按Goal Application、Slot、Profile、正式Rig准备结果与Pending BendHistory执行求解
- **AND** MUST不回调FootPlacement、读取Ground Path或修改Contact ownership

#### Scenario: 编码几何不可达但保持原值的目标

- **WHEN** Foot记录了本腿不可达观察且原流程保持该脚目标与作者权重
- **THEN** Encoder MUST只完成原空间和权重编码，保持最终Resolved与Contribution一致
- **AND** Assembler MUST不新增Reach拦截或夹脚，FBBIK继续原求解数学并保留真实误差
