## ADDED Requirements

### Requirement: MotionWarp Gameplay Preview 必须显式提供目标快照

Timeline Authoring Preview MAY在窗口session state中配置editor-only `ActionTargetSnapshot`，但该值 MUST不写入Timeline、Graph、ActionProfile或Blackboard资产。包含MotionWarp的完整Gameplay Preview MUST将该快照通过正式Action admission与ActionInstance链传入隔离Preview Session；缺失必需快照时 MUST停止并显示正式reject reason。

#### Scenario: 预览目标攻击轨迹

- **WHEN** 作者为Preview Session提供合法目标快照并播放包含MotionWarp的Timeline
- **THEN** Preview MUST经过compiled Program、ActionInstance、Motion resolver、Modifier和Preview WorldSolver
- **AND** MUST不直接修改preview target Transform来模拟Warp

#### Scenario: 只采样动画资源

- **WHEN** 用户处于没有Program、Action Context和WorldSolver的纯动画预览
- **THEN** MotionWarp MUST不执行
- **AND** UI MUST不把视觉Transform移动显示成Gameplay Warp结果

### Requirement: Timeline Live Debug 必须显示 MotionWarp 正式运行事实

Live Debug MUST从正式runtime trace显示MotionWarp window、source MotionCurve、ActionInstance、target snapshot、nominal end、desired pose、position/yaw progress、total correction、final request和actual solver result。Live Debug MUST不重新计算Warp，也 MUST不读取mutable accumulator或scene target。

#### Scenario: Warp 请求被墙阻挡

- **WHEN** Live Debug观察到Warp final request与actual result不同
- **THEN** UI MUST同时显示请求修正和Solver实际结果
- **AND** 作者 MUST能区分clamp与collision造成的未到达
