## MODIFIED Requirements

### Requirement: Animation Clip控制曲线必须作为typed Curve Channel编辑

项目表现控制曲线 MUST由唯一channel catalog注册，并直接保存于可写原生AnimationClip。Unity Animation Window MUST成为人工Curve key编辑入口；C#作者API MUST只读写同一注册Curve。Projection MUST把Curve降低为Runtime canonical plan。Profile、Timeline、Blend Space和Foot Analysis artifact MUST不保存可写Curve副本。

#### Scenario: 修改Foot Placement Weight

- **WHEN** 作者在Animation Window修改Clip的`presentation.foot-placement-weight`
- **THEN** 完整Clip dependency与Registered Curve Hash MUST变化并使Projection stale
- **AND** AnimationClipAnalysisInputHash与匹配Foot Analysis Artifact MUST保持不变
- **AND** Runtime MUST只在显式Build后消费新的Projection curve

### Requirement: 跨资产表现配置必须保持唯一写入口

Pose Graph Workspace、Navigator与Details MAY只读显示Action Timeline Segment、Player Source Slot及其Profile资源、Locomotion Sync Group、Clip注册Curve、Policy、Rig与Analysis状态。修改Action Segment编排 MUST导航到Timeline Editor；修改Clip骨骼或注册Curve MUST打开Unity Animation Window中的精确Clip与Preview Target；修改Sync Group MUST导航到Profile；修改State transition与Slot Policy MUST导航到Pose Graph/Policy owner。人工入口与C#作者API MUST分别调用同一正式Mutation和资产事务，系统 MUST不复制字段、提供第二mutation命令、按窗口类型分叉写链或保留字符串binding镜像。

#### Scenario: 从Pose Graph调整Run Phase

- **WHEN** 作者在State source引用面板选择Open Source Curve
- **THEN** 必须打开RunLoop原生AnimationClip与正式Preview Target
- **AND** Pose Graph节点与Profile Binding MUST保持只读Clip引用摘要
