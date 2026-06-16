## MODIFIED Requirements
### Requirement: Locomotion 子树映射
系统 MUST 将现有 `Idle / MoveStart / MoveLoop / MoveStop / TurnBack` Locomotion phase 映射为同一棵 FullBody 分层状态机下的状态路径。Locomotion 子域的 transition MUST 由统一分层状态机配置和 runner 解释；Locomotion adapter MAY 提供移动意图、空间方向、phase facts、motion facts 和动画 facts，但 MUST NOT 复用或恢复独立 `BasicLocomotionStateMachine` 作为第二状态权威。

#### Scenario: WASD 是统一分层状态机的 Locomotion 子域
- **WHEN** 系统处理基础 WASD 移动
- **THEN** `Idle / MoveStart / MoveLoop / MoveStop / TurnBack` MUST 属于 `FullBody/Locomotion` 子域
- **AND** 它们 MUST 与 `FullBody/Action` 共享同一个状态图运行时和 snapshot 来源
- **AND** 它们 MUST NOT 与 `FullBody/Action` 形成两个同时提交 base layer 或平面位移的平级权威

#### Scenario: Locomotion 路径可读
- **GIVEN** 当前 FullBody owner 为 Locomotion
- **WHEN** Locomotion phase 为 `MoveLoop`
- **THEN** FullBody 状态路径 MUST 能表达 `/FullBody/Locomotion/MoveLoop` 或等价层级路径
- **AND** 快照 MUST 仍暴露当前 `BasicMovementPhase`

#### Scenario: Walk 和 Run 不成为状态
- **WHEN** Locomotion gait 为 Walk 或 Run
- **THEN** FullBody HFSM MUST NOT 生成 `WalkStart`、`WalkLoop`、`RunStart` 或 `RunLoop` 作为逻辑状态
- **AND** Walk/Run MUST 继续作为 gait 事实进入运动命令和动画上下文

#### Scenario: Locomotion transition 规则不复制为第二状态机
- **WHEN** `MoveStop` 中重新出现移动输入
- **THEN** `MoveStop -> MoveStart` MUST 由统一分层状态机配置中的 transition 决定
- **AND** Locomotion adapter MUST NOT 通过独立状态机重新实现该规则
- **AND** FullBody pipeline MUST NOT 通过另一套外部条件绕过状态图运行时切换 Locomotion phase

### Requirement: HFSM 与输出权威分离
系统 MUST 保持 FullBody HFSM、状态生命周期接口、动画 Presenter 和 motion executor 的职责分离。HFSM 负责状态路径和 transition 权威；状态生命周期接口 MAY 产出 Enter、Tick、Exit 对应的纯数据输出；Presenter 只消费动画命令并反馈播放事实；motion executor 只执行当前 owner 的运动命令。

#### Scenario: HFSM 不直接播放动画
- **WHEN** FullBody HFSM 进入、更新或退出某个状态
- **THEN** HFSM MAY 产出状态事实或状态输出意图
- **AND** MUST NOT 直接调用 Animancer 或 Animator 播放 API
- **AND** MUST NOT 通过动画回调直接决定业务状态切换

#### Scenario: HFSM 不直接移动角色
- **WHEN** FullBody HFSM 进入、更新或退出某个状态
- **THEN** HFSM MAY 决定当前运动命令来源
- **AND** MUST NOT 直接调用 `CharacterController.Move`
- **AND** MUST NOT 直接写入角色 `Transform.position`

#### Scenario: 经典生命周期只产出纯数据
- **WHEN** 状态节点的 `Enter`、`Tick` 或 `Exit` 生命周期被调用
- **THEN** 生命周期实现 MUST 只向 frame builder、state output 或等价纯数据结构写入结果
- **AND** MUST NOT 直接消费输入缓冲、播放动画或执行位移
- **AND** FullBody pipeline MUST 继续统一执行输入消费、运动、动画表现和诊断提交

#### Scenario: Action 仲裁不藏进 transition 顺序
- **WHEN** FullBody Action 请求参与状态切换
- **THEN** 是否允许进入 Action MUST 由 `ActionInterruptArbiter` 或等价仲裁层决定
- **AND** HFSM transition MUST 读取仲裁结果或 module active 事实
- **AND** MUST NOT 仅依赖多个全局 transition 的注册顺序表达业务优先级
