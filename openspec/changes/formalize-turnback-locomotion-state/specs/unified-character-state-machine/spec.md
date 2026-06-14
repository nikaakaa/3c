## ADDED Requirements
### Requirement: TurnBack Locomotion 正式状态契约
系统 MUST 将移动反向急转表达为 `FullBody/Locomotion/TurnBack` 正式逻辑状态，并由该状态声明本次转身的动画请求、目标朝向、运动权威策略、输入抑制、动画进入时间和退出窗口。默认 TurnBack 动画只允许从 `FullBody/Locomotion/MoveLoop` 且当前 gait 为 Run 时进入；Walk、MoveStart、MoveStop 和 Idle MUST NOT 直接触发该 TurnBack 动画。TurnBack MUST 仍由统一状态机 transition 进入，MUST NOT 由动画外观层、motion executor 或 controller 特判直接切换状态。

#### Scenario: TurnBack 由统一状态机进入
- **GIVEN** 当前状态为 `FullBody/Locomotion/MoveLoop`
- **AND** 当前 gait 为 Run
- **AND** 统一 Locomotion 决策事实中存在有效 TurnBack intent
- **WHEN** `MoveTurnBackRequested` 或等价 transition 条件通过
- **THEN** 统一状态机 MUST 进入 `FullBody/Locomotion/TurnBack`
- **AND** 进入行为 MUST 锁定本次目标朝向或目标方向
- **AND** 动画外观层 MUST NOT 直接调用状态切换 API

#### Scenario: WalkLoop 不直接进入 TurnBack
- **GIVEN** 当前状态为 `FullBody/Locomotion/MoveLoop`
- **AND** 当前 gait 为 Walk
- **AND** 统一 Locomotion 决策事实中存在有效 TurnBack intent
- **WHEN** 统一状态机评估本帧 transition
- **THEN** 状态机 MUST NOT 进入 `FullBody/Locomotion/TurnBack`

#### Scenario: MoveStart 和 MoveStop 不直接进入 TurnBack
- **GIVEN** 当前状态为 `FullBody/Locomotion/MoveStart` 或 `FullBody/Locomotion/MoveStop`
- **AND** 统一 Locomotion 决策事实中存在有效 TurnBack intent
- **WHEN** 统一状态机评估本帧 transition
- **THEN** 状态机 MUST NOT 进入 `FullBody/Locomotion/TurnBack`

#### Scenario: Idle 不直接进入 TurnBack
- **GIVEN** 当前状态为 `FullBody/Locomotion/Idle`
- **AND** 统一 Locomotion 决策事实中存在有效 TurnBack intent
- **WHEN** 统一状态机评估本帧 transition
- **THEN** 状态机 MUST NOT 进入 `FullBody/Locomotion/TurnBack`

#### Scenario: TurnBack 锁定目标不被相机抖动覆盖
- **GIVEN** 角色已经进入 `FullBody/Locomotion/TurnBack`
- **AND** 本次 TurnBack 已锁定目标朝向
- **WHEN** 后续帧相机朝向或输入基准发生变化
- **THEN** TurnBack 状态 MUST 继续使用进入时锁定的目标朝向
- **AND** MUST NOT 每帧重新用相机基准改写本次转身目标

#### Scenario: TurnBack 状态声明输入抑制
- **GIVEN** 当前逻辑状态为 `FullBody/Locomotion/TurnBack`
- **WHEN** 系统构建本帧运动命令
- **THEN** TurnBack 状态输出 MUST 声明普通输入旋转被抑制
- **AND** MUST 声明普通输入平面位移被抑制

#### Scenario: TurnBack 状态声明动画时间窗口
- **GIVEN** 当前逻辑状态为 `FullBody/Locomotion/TurnBack`
- **WHEN** 系统构建状态输出
- **THEN** TurnBack 状态输出 MUST 能携带进入 fade、start normalized time、输入锁定窗口、转完点和退出窗口
- **AND** 这些时间事实 MUST 可由配置或 baked motion profile 提供

#### Scenario: TurnBack 按转完点退出
- **GIVEN** 当前逻辑状态为 `FullBody/Locomotion/TurnBack`
- **AND** TurnBack policy 配置了转完 normalized time 或等价 marker
- **WHEN** 动画播放进度达到转完点
- **AND** 当前仍有移动输入
- **THEN** 状态机 MUST 转入 `FullBody/Locomotion/MoveLoop`
- **WHEN** 动画播放进度达到转完点
- **AND** 当前没有移动输入
- **THEN** 状态机 MUST 转入 `FullBody/Locomotion/Idle`

#### Scenario: TurnBack 不等待跑步尾巴
- **GIVEN** `Locomotion.Turn.Back` 动画包含转身后的跑步尾巴
- **WHEN** TurnBack 已达到转完点
- **THEN** 状态机 MUST 允许退出 TurnBack
- **AND** MUST NOT 要求整段动画播放结束后才能交还普通移动

## MODIFIED Requirements
### Requirement: 状态输出配置
系统 MUST 允许逻辑状态节点配置进入、更新和退出时的纯数据输出。输出 MAY 包含运动命令、动画转换请求、输入请求消费、Run latch 写入、状态事实写入和诊断事实，但 MUST 由统一状态机先决定当前状态后再产出。TurnBack 这类 animation-driven locomotion transition MUST 通过状态输出声明运动权威策略，而不是散落在 controller 或 presenter 的临时特判中。

#### Scenario: Locomotion 状态输出基础移动
- **WHEN** 当前逻辑状态为 `MoveLoop`
- **THEN** 状态输出 MUST 能根据当前移动意图产出基础移动运动命令
- **AND** MUST 能产出 `MoveLoop` 对应的动画转换请求或持续播放请求
- **AND** MUST NOT 通过独立 Locomotion runtime 绕过统一状态机提交 base layer 动画

#### Scenario: Dodge 状态输出动作位移
- **WHEN** 当前逻辑状态为 `Dodge` 且变体为 `Directional`
- **THEN** 状态输出 MUST 能按配置距离和时长产出动作位移命令
- **AND** MUST 能产出立即转向输出
- **AND** MUST 能在完成时产出 Run latch 写入
- **AND** Locomotion 状态 MUST NOT 同时产出第二份平面位移或 base layer 动画输出

#### Scenario: Backstep 不写 Run latch
- **WHEN** 当前逻辑状态为 `Dodge` 且变体为 `Backstep`
- **THEN** 状态输出 MUST 能按配置距离和时长产出后闪位移命令
- **AND** MUST NOT 在完成时强制写入 Run latch

#### Scenario: TurnBack 状态输出动画驱动策略
- **WHEN** 当前逻辑状态为 `FullBody/Locomotion/TurnBack`
- **THEN** 状态输出 MUST 能声明 `Locomotion.Turn.Back` 动画请求
- **AND** MUST 能声明 TurnBack motion policy
- **AND** motion policy MUST 能引用 baked motion profile 或等价纯数据资产
- **AND** motion policy MUST 能声明默认入口为 `MoveLoop + Run`
- **AND** MUST 能声明普通输入旋转和平面位移抑制
- **AND** MUST NOT 直接调用 Animancer、CharacterController 或 motion executor
