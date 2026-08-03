## MODIFIED Requirements

### Requirement: PresentationFrame必须输出完整最终Pose Plan结果

PresentationFrame MUST消费完整committed Animation Selection batch和typed Parameter page，按Projection编译的Selection、Player source membership、Marker time resolve、source sampling、native composition、world-aware postprocess与Output阶段原子推进。只有唯一OutputPose及其所有必需阶段完成后，Runtime才可发布`FinalAnimationPoseFrame`并推进Camera；任一Selection、MarkerSync、Player、Pose operation、FootPlacement或Solver失败 MUST阻止部分最终结果发布，不得沿用上一帧或绕过节点。

#### Scenario: Action等待第一Selection sample

- **WHEN** Program已经选择Action但Presentation尚无合法Selection sample
- **THEN** 对应Selection Input MUST保持Pending或NoPose语义
- **AND** Pose Plan MUST按编译availability policy决定是否仍能产生Final Pose

### Requirement: PresentationFrame必须原子提交动画播放与Pose节点生命周期

PresentationFrame MUST在同一外层事务中提交Selection cache、Player source usage、Marker relation/effective sample page、Player/Blend Stack状态、source capture、Pose operation completion、world-aware postprocess plan、Solver结果和final publication。Reset、branch replacement、Projection replacement或失败 MUST按编译Plan逆序清理全部stateful节点；不得只提交Marker relation或Blend entry而保留旧Output，也不得只发布Output而遗漏source retirement。

#### Scenario: Selection与首个Sample同批

- **WHEN** 新Selection与首份合法source sample在同一PresentationFrame到达
- **THEN** 目标Player节点 MUST原子初始化并参与本帧Pose Plan
- **AND** FinalAnimationPoseFrame MUST只反映该次完整事务结果
