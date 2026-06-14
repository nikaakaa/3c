## ADDED Requirements
### Requirement: 仲裁器消费窗口事实而不拥有窗口时间
状态请求仲裁入口 MUST 将窗口时间视为外部事实。仲裁器 MAY 使用 `StateTimelineWindowFacts` 中的 active facts、request window、min priority、resistance 和 force 参与裁决，但 MUST NOT 自己计算状态 normalized time、动画 normalized time、clip length 或窗口 start/end。新增状态请求准入 MUST 优先依赖 required fact id 与 window facts；旧 elapsed time timing rule 只作为迁移兼容。

#### Scenario: required window 未激活时拒绝
- **GIVEN** 请求策略要求 `attack-combo` window
- **AND** `StateTimelineWindowFacts` 中没有 active `attack-combo` request window
- **WHEN** 仲裁器处理该请求
- **THEN** 裁决 MUST 为 rejected
- **AND** 拒绝原因 MUST 能诊断为窗口未满足或 timing 未满足

#### Scenario: required fact 未激活时拒绝
- **GIVEN** 请求策略要求 `ComboInputOpen` fact
- **AND** `StateTimelineWindowFacts` 中没有 active `ComboInputOpen`
- **WHEN** 仲裁器处理 LightAttack 请求
- **THEN** 裁决 MUST 为 rejected
- **AND** 仲裁器 MUST NOT 尝试读取 Attack01 的窗口 start/end

#### Scenario: 仲裁器不读取动画时间
- **WHEN** 仲裁器处理 TurnBack、Dodge 或 Attack 请求
- **THEN** 仲裁器 MUST NOT 读取 Animancer state
- **AND** MUST NOT 读取 Animator state
- **AND** MUST NOT 读取 AnimationClip length

### Requirement: 状态请求打断仲裁入口
系统 MUST 将现有动作打断仲裁能力扩展为状态请求准入入口，能够处理 TurnBack、Dodge、Attack、HitReact 或等价 FullBody 状态请求。仲裁入口 MUST 继续保持纯数据边界，并 MUST NOT 直接切换统一状态机、播放动画或提交运动命令。

#### Scenario: TurnBack 请求经过仲裁
- **GIVEN** 当前状态为 `FullBody/Locomotion/MoveLoop`
- **AND** 当前 gait 为 Run
- **AND** 输入方向与角色朝向满足 TurnBack 请求条件
- **WHEN** 状态请求仲裁入口处理请求
- **THEN** TurnBack 请求 MUST 按 priority、resistance 和 timeline window policy 被 accepted 或 rejected
- **AND** 只有 accepted 请求 MAY 进入统一状态机事实

#### Scenario: Dodge 继续走同一仲裁
- **GIVEN** 输入缓冲中存在 Dodge 请求
- **WHEN** 状态请求仲裁入口处理请求
- **THEN** Dodge MUST 继续使用 priority、resistance、force 和 timing/window 规则
- **AND** 系统 MUST NOT 新增 Dodge 专用状态准入路径

#### Scenario: 仲裁器不接管状态机
- **WHEN** 仲裁入口接受某个状态请求
- **THEN** 仲裁结果 MUST 只返回纯数据 decision
- **AND** MUST NOT 调用 `ChangeState`
- **AND** MUST NOT 写入动画或运动输出

### Requirement: Window Facts 驱动时间许可
状态请求仲裁入口 MUST 能使用 timeline window facts 判断请求是否位于允许窗口。第一版 MAY 保留 elapsed time 规则以兼容现有 ActionInterruptPolicy，但新增状态窗口判断 MUST 通过 facts 进入仲裁器，而不是让状态机 transition evaluator 或 MonoBehaviour 重复判断。

#### Scenario: 窗口未开启时拒绝
- **GIVEN** 当前请求匹配到需要 `TurnBackInterrupt` 或等价 window 的策略
- **AND** timeline window facts 表示该窗口未 active
- **WHEN** 仲裁入口执行裁决
- **THEN** 裁决 MUST 为 rejected
- **AND** 拒绝原因 MUST 能表达时间窗口未满足

#### Scenario: 窗口开启且优先级满足时接受
- **GIVEN** 当前请求匹配到一个 active window
- **AND** 请求 priority 满足策略 min priority
- **AND** 请求 priority 高于当前 resistance 或策略 force 为 true
- **WHEN** 仲裁入口执行裁决
- **THEN** 裁决 MUST 为 accepted

### Requirement: 状态请求仲裁诊断
系统 MUST 为状态请求仲裁输出可追踪日志，说明 request kind、from state、target state、priority、resistance、matched policy、window id 和 rejected reason。

#### Scenario: TurnBack 被窗口拒绝可诊断
- **GIVEN** 玩家输入满足 TurnBack 几何条件
- **AND** 当前不在允许 TurnBack 的状态或窗口
- **WHEN** 仲裁入口拒绝请求
- **THEN** 诊断日志 MUST 能说明拒绝发生在状态/window/priority/resistance 哪一层
