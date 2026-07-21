## REMOVED Requirements

### Requirement: Timeline Preview 必须使用隔离 Simulation Session Composition

**Reason**: Timeline Authoring Preview只负责动画表现采样；完整Gameplay执行由正式运行Session与Live Debug承担。保留Preview Simulation Composition会增加无消费者的Source、Pipeline、passes、Solver与配置路径。

**Migration**: 删除Preview Session Source、Preview Pipeline、Preview pass、Preview input port、Character PreviewComposition字段和配置资产。其它Local、ServerAuthoritative、DotRecast与DeterministicRollback Composition不变。

#### Scenario: 打开含 Gameplay 轨道的 Timeline

- **WHEN** 作者打开含TreeClip、MotionCurve或MotionWarp的Timeline
- **THEN** Authoring Preview MUST不创建Simulation Session Composition
- **AND** Live Debug MUST从已运行的正式Session显示Gameplay事实

