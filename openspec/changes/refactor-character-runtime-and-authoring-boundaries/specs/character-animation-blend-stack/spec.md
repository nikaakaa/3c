## MODIFIED Requirements

### Requirement: 每个显式Blend Stack节点必须拥有唯一有序状态

原生图装配后的每个显式`BlendStack` Pose节点 MUST按稳定PoseNodeId创建唯一运行时实例。实例 MUST唯一拥有active entry、push order、CrossFade clock、Stored Pose、source usage与workspace。未连接`BlendStack`节点的图分支 MUST不创建Stack，Runtime和Preview MUST不自动补建Stack或fade。

#### Scenario: 两个节点读取同一Selection

- **WHEN** 两个显式Blend Stack节点读取同一AnimationSelectionFrame
- **THEN** 两个节点 MUST拥有互不共享的entry历史与clock
- **AND** 任一节点的中断 MUST不修改另一节点状态

#### Scenario: 图分支使用直接Player

- **WHEN** Selection只连接SelectedPosePlayer
- **THEN** Runtime MUST不为该分支分配Blend Stack状态
