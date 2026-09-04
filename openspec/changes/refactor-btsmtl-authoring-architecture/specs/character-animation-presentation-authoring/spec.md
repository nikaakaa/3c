## MODIFIED Requirements

### Requirement: 跨资产表现配置必须保持唯一写入口

Pose Graph Workspace、Navigator与Details MAY只读显示Action Timeline Segment、Profile direct Clip Binding、Locomotion Sync Group、Clip注册Curve、Policy、Rig与Analysis状态。修改Action Segment编排 MUST导航到Timeline Editor；修改Clip骨骼或注册Curve MUST打开Unity Animation Window中的精确Clip与Preview Target；修改Profile Binding或Sync Group MUST导航到Profile；修改State transition与Slot Policy MUST导航到Pose Graph/Policy owner。人工入口与Document v5 Reconciler MUST分别调用同一正式Mutation和资产事务，系统 MUST不复制字段、提供第二mutation命令、按窗口类型分叉写链或保留字符串binding镜像。

#### Scenario: 从Pose Graph调整Run Phase

- **WHEN** 作者在State source引用面板选择Open Source Curve
- **THEN** 必须打开RunLoop原生AnimationClip与正式Preview Target
- **AND** Pose Graph节点与Profile Binding MUST保持只读Clip引用摘要


### Requirement: Pose Graph Producer Navigator必须从显式Definition上下文投影

Pose Graph Navigator MUST要求精确Definition context，并从Profile、Pose Graph和角色控制合同和技能producer目录分别投影Pose source与有限Action producer。Locomotion分组 MUST显示PoseState、Clip/BlendSpace/MM consumer、Source Slot业务名、Profile binding与实际资源名；Action分组 MUST显示Timeline、Track、AnimationChannel与AnimationSlot业务名。Navigator MUST不读取generated Program/Projection完成bootstrap，不按显示名猜测，不显示机器identity作为项目项名称，也不得保存第二份binding。

#### Scenario: 查看Locomotion sources

- **WHEN** 作者从Corin Definition展开Locomotion
- **THEN** Navigator MUST列出Idle、Start、Move、Stop、Turn的Source Slot及其实际资源
- **AND** MUST不列出BaseLocomotion Timeline producer或Source Id字符串

#### Scenario: 缺少Definition上下文

- **WHEN** 作者直接打开shared Pose Graph且没有精确Definition call-site context
- **THEN** Producer Navigator MUST显示Unavailable及缺失上下文原因
- **AND** MUST不搜索使用该图的任意角色或使用上一次窗口context
