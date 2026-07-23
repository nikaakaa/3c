## MODIFIED Requirements

### Requirement: Foot Placement必须只消费表现帧正式输入

FootPlacement MUST只消费完整native Pose阶段完成后的Pose与每脚feature。若上游包含Inertialization，FootPlacement MUST读取该节点经过下游composition形成的实际贡献，MUST不读取旧Stack Inertial contribution、Accumulator或Discontinuity来重新选择动画。

#### Scenario: 左脚分支正在惯性衰减

- **WHEN** 上游Inertialization对左脚Bone仍有残差
- **THEN** FootPlacement MUST消费最终left-foot feature与骨骼结果
- **AND** MUST不查询旧source或MM candidate

