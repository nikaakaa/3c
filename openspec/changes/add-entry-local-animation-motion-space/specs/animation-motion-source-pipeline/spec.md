## ADDED Requirements

### Requirement: 动画运动平面 Delta 坐标空间
系统 MUST 允许 `TickSampledMotion` 或等价动画运动源在 movement facts 中声明 sampled planar delta 的坐标空间。坐标空间 MUST 至少区分 `Local`、`World` 和 `EntryLocal`，并且该声明 MUST 作为纯数据进入 motion executor。

#### Scenario: Local 保持当前朝向语义
- **GIVEN** sampled planar delta 声明为 `Local`
- **WHEN** motion executor 解析该 delta
- **THEN** executor MUST 使用执行时当前 motion root 朝向将 local X/Z 转换为 world delta
- **AND** MUST 保持现有依赖当前 root local 的动画运动行为

#### Scenario: World 保持直接世界语义
- **GIVEN** sampled planar delta 声明为 `World`
- **WHEN** motion executor 解析该 delta
- **THEN** executor MUST 将该平面 delta 作为 world delta 使用
- **AND** MUST NOT 再根据 root 或 entry basis 旋转它

#### Scenario: EntryLocal 使用固定进入基准
- **GIVEN** sampled planar delta 声明为 `EntryLocal`
- **AND** movement facts 携带有效 entry planar basis
- **WHEN** motion executor 解析该 delta
- **THEN** executor MUST 使用 entry basis forward/right 将 local X/Z 转换为 world delta
- **AND** MUST NOT 使用当前 motion root yaw 解释该 translation

### Requirement: EntryLocal 不形成第二运动路径
系统 MUST 将 `EntryLocal` 作为现有 movement facts、movement command 和 motion executor 的扩展语义实现，不得新增绕过统一 motion executor 的 TurnBack 专用控制器、直接 Transform 写入或 Animator runtime root-motion fallback。

#### Scenario: 仍由统一 executor 应用
- **GIVEN** 当前状态通过 `TickSampledMotion` 输出 `EntryLocal` planar delta
- **WHEN** 本 tick 执行运动
- **THEN** delta MUST 先进入 movement facts 或等价纯数据 command
- **AND** MUST 由现有 motion executor 应用角色根位移

#### Scenario: 缺少 basis 不回退到隐式 Local
- **GIVEN** sampled planar delta 声明为 `EntryLocal`
- **AND** movement facts 缺少有效 entry basis
- **WHEN** pipeline 构建或执行运动
- **THEN** 系统 MUST 输出诊断或校验结果
- **AND** MUST NOT 静默改用当前 root local 解释该 delta
