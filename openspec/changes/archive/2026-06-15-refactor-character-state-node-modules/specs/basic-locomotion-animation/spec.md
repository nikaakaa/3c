## ADDED Requirements
### Requirement: Locomotion 动画不由状态节点万能动画字段配置
系统 MUST 继续通过 Locomotion phase、运行时 gait facts、基础移动动画配置和 Animancer presenter 解析基础移动动画。普通 Locomotion 状态节点 MUST NOT 通过万能 animation binding 字段配置具体动画 key；如 TurnBack 等特殊 Locomotion 状态需要 timeline alias 或 motion alias，MUST 通过明确的 Locomotion animation alias / TurnBack motion policy 模块表达。

#### Scenario: MoveLoop 使用 phase 和 gait
- **WHEN** 当前状态节点具备 `MoveLoop` Locomotion phase 模块
- **AND** 运行时 gait fact 为 Run
- **THEN** 基础移动动画系统 MUST 使用 `MoveLoop + Run` 或等价 facts 解析 Animancer key
- **AND** `MoveLoop` 节点 MUST NOT 需要配置独立 action animation key

#### Scenario: TurnBack 使用单一 alias 来源
- **WHEN** 当前状态节点具备 TurnBack motion policy 模块
- **THEN** TurnBack 播放、timeline binding 和 baked motion profile MUST 使用同一正式 alias 来源或明确映射
- **AND** 配置者 MUST NOT 在状态节点万能 animation 字段和 TurnBack policy 字段重复填写同一个 alias

#### Scenario: gait 不进入状态图
- **WHEN** 角色从 Walk 切换到 Run
- **THEN** 状态路径 MUST NOT 因 gait 变化变成 WalkLoop 或 RunLoop
- **AND** gait MUST 作为运行时事实进入动画和运动解析
