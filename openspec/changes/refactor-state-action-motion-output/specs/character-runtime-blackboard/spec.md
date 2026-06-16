## ADDED Requirements
### Requirement: Action Facts 来自 Action Motion Resolver Result
角色运行时黑板 MUST 从状态机 frame 和 Action motion resolver result 写入 Action facts。黑板写入 MUST NOT 从状态输出解析层重新计算动作位移、完成状态或 run latch 派生。

#### Scenario: 写入动作位移事实
- **GIVEN** Action motion resolver 产出本帧动作运动结果
- **WHEN** FullBody 管线写入 runtime blackboard
- **THEN** Action facts MUST 使用 resolver result 中的 movement command、has movement、completed 和 source step
- **AND** MUST NOT 调用 `CharacterStateOutputResolver` 重算本帧距离

#### Scenario: 无动作规格写入空事实
- **GIVEN** 当前状态没有 action motion spec
- **WHEN** FullBody 管线写入 runtime blackboard
- **THEN** Action facts MUST 表示无 active action movement
- **AND** MUST NOT 使用上一帧 resolver result 伪造当前帧动作位移

### Requirement: Action Facts 保持纯数据
Action facts MUST 保持可复制纯数据，不得持有 motion executor、Transform、CharacterController、Animator、Animancer state、AnimationClip 或 UnityEngine.Object。

#### Scenario: 静态边界验证
- **WHEN** 检查 runtime blackboard 与 action facts 源码
- **THEN** 源码 MUST NOT 保存 Unity 场景实例引用
- **AND** MUST NOT 保存动画 runtime 对象
