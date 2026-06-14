## ADDED Requirements
### Requirement: FullBody Action 策略装配入口
系统 MUST 为 FullBody Action 运行时准入提供明确的策略集合装配入口。该入口 MAY 位于 FullBody Action 控制器、角色动作配置或等价主装配点，但 MUST NOT 位于 Locomotion controller、movement pipeline 或 animation presenter。

#### Scenario: FullBody 控制器定位策略集合
- **WHEN** 角色 FullBody Action 请求门面处理 Dodge 请求
- **THEN** 它 MUST 能定位用于 `ActionInterruptArbiter` 的策略集合
- **AND** 策略集合 MUST 编译为纯 runtime policy 列表后再参与仲裁

#### Scenario: 缺失策略集合可诊断
- **GIVEN** 角色没有配置策略集合或策略集合无法编译
- **WHEN** 玩家提交 FullBody Action 请求
- **THEN** 系统 MUST 产生 rejected decision 或配置错误诊断
- **AND** 系统 MUST NOT 绕过策略集合直接让状态机进入动作

#### Scenario: Locomotion 不读取策略集合
- **WHEN** 基础移动处理 `Idle / MoveStart / MoveLoop / MoveStop`
- **THEN** Locomotion controller MUST NOT 读取动作打断策略集合
- **AND** movement pipeline MUST NOT 读取动作打断策略集合
- **AND** animation presenter MUST NOT 读取动作打断策略集合

### Requirement: 默认 Dodge 打断策略
系统 MUST 为默认可琳 FullBody Dodge 提供可配置的进入策略，表达从空 Action 或当前可允许状态进入 `Action.Dodge` 的最小优先级、时间规则、force 和抗性语义。

#### Scenario: 默认策略允许合法 Dodge
- **GIVEN** 当前动作状态为空 Action 或等价可允许状态
- **AND** Dodge 请求优先级满足策略最小优先级
- **AND** 当前 resistance 不阻挡请求
- **WHEN** FullBody Action 请求门面执行仲裁
- **THEN** `ActionInterruptArbiter` MUST 返回 accepted decision

#### Scenario: 默认策略拒绝低优先级 Dodge
- **GIVEN** 当前动作状态匹配默认 Dodge 策略
- **AND** Dodge 请求优先级低于策略最小优先级
- **WHEN** FullBody Action 请求门面执行仲裁
- **THEN** `ActionInterruptArbiter` MUST 返回 rejected decision
- **AND** 拒绝原因 MUST 表示优先级不足

## MODIFIED Requirements
### Requirement: 现有运行时边界保持
系统 MUST 保持当前 Locomotion、Animancer Presenter 和动作打断仲裁器的边界。动作打断策略集合 MAY 作为 FullBody Action 请求准入配置接入运行时，但 MUST NOT 改变 `Idle / MoveStart / MoveLoop / MoveStop` 状态图，也不得让配置数据成为 `MoveStop -> MoveStart` 的必需依赖。

#### Scenario: 基础移动不依赖策略集合
- **WHEN** 当前基础移动状态机处理 `MoveStop` 中重新输入
- **THEN** `MoveStop -> MoveStart` MUST 继续由 Locomotion 状态图处理
- **AND** 基础移动状态机 MUST NOT 依赖动作打断策略集合

#### Scenario: Presenter 不读取策略集合
- **WHEN** 基础移动动画 Presenter 播放移动阶段 alias
- **THEN** Presenter MUST NOT 读取动作打断策略集合
- **AND** Presenter MUST NOT 通过策略集合决定业务打断

#### Scenario: FullBody Action 准入读取策略集合
- **WHEN** FullBody Action 请求门面处理 Dodge 或后续 Action 请求
- **THEN** 它 MAY 读取动作打断策略集合并编译 runtime policy
- **AND** 该读取 MUST 只用于动作请求仲裁
- **AND** MUST NOT 直接提交运动命令或动画播放命令
