## MODIFIED Requirements

### Requirement: Optional Pose与Reset不得伪造惯性目标

Inertialization MUST只在前后均合法的 Pose 到 Pose 转换时执行。Initialization、Presentation Reset、branch replacement、Invalid 与 NoPose 边界 MUST按原有 typed HardCut/propagation 规则清理或重建 history，不得使用 Bind Pose、上一帧缓存或 Empty 伪造 target。完整场景预览的重建 MUST沿正式 Actor 生命周期触发这些规则，编辑游标和 Capture 历史浏览 MUST不再产生独立 Preview seek 重置。

#### Scenario: NoPose进入Pose

- **WHEN** 节点从 NoPose 收到第一份合法 Pose
- **THEN** 节点 MUST建立新 history 并原样输出 Pose
- **AND** MUST不执行惯性进入

#### Scenario: 浏览历史时保持当前惯性状态

- **WHEN** 作者移动 Capture 历史位置
- **THEN** 当前角色 MUST保持正式运行产生的惯性状态
- **AND** 历史浏览 MUST不重算、清空或修改运行 history
