## MODIFIED Requirements

### Requirement: Foot Placement必须是Pose Graph中唯一有状态world-aware骨骼控制节点

启用Foot Placement的Character Presentation Pose Graph MUST显式包含一个接收并输出Component Pose的`FootPlacement`节点。Pose Graph Compiler MUST按作者拓扑保留Virtual Bone派生与`TwoBoneIK`，并把FootPlacement降低为对应位置的world-aware stage。FootPlacement MUST只读取Rig v3 Physical腿链、同帧上游Component Pose、最终Foot Features、PhysicsScene support与Rig Calibration，再由解析式Limb Pose Solver写入节点output workspace；它 MUST不读取Virtual Bone作为ankle/toe/sole、Joint Target、预测落点、Foot Lock或surface anchor。Runtime MUST不在图外追加第二Foot Placement、Final IK、隐藏target或第二骨骼写入路径。

#### Scenario: 双臂TwoBoneIK后执行FootPlacement

- **WHEN** 同一Pose Plan先完成左右臂TwoBoneIK再到达FootPlacement
- **THEN** FootPlacement MUST只消费TwoBoneIK输出中的同帧Physical Component Pose与既有脚部正式输入
- **AND** MUST不重新派生Virtual Bone或再次求解手臂

#### Scenario: Virtual Bone被配置为脚部世界锚点

- **WHEN** 作者或Runtime尝试把Virtual Bone用于预测落点、Foot Lock或support anchor
- **THEN** validation MUST拒绝该配置
- **AND** 系统 MUST不按Virtual Bone的component pose伪造世界接触
