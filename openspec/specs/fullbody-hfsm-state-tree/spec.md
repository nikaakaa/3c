# fullbody-hfsm-state-tree Specification

## Purpose
定义 FullBody 分层 HFSM 状态树的可见路径、Locomotion/Action 子域、状态输出和不直接移动角色的边界。
## Requirements
### Requirement: FullBody 分层 HFSM 状态树
系统 MUST 提供一个 FullBody 主行为域的显式分层 HFSM 状态树，用于表达 Locomotion 和 FullBody Action 的主状态路径。该状态树 MUST 建立在现有 FullBody Action 框架之上，MUST NOT 新增第二套角色控制器、第二套基础移动状态机或 BBB 运行时依赖。

#### Scenario: 状态树包含 Locomotion 和 Action 分支
- **WHEN** FullBody HFSM 初始化
- **THEN** 状态树 MUST 包含 `FullBody/Locomotion` 分支
- **AND** 状态树 MUST 包含 `FullBody/Action` 分支
- **AND** 第一版 Action 分支 MUST 至少能表达 `Action.Dodge`
- **AND** `FullBody/Action` MUST 是 FullBody 主树内的动作子域，不得成为与 FullBody 并列的独立状态机权威

#### Scenario: 不新增第二控制路径
- **WHEN** FullBody HFSM 状态树接入运行时
- **THEN** 系统 MUST 继续通过现有 FullBody 主调度入口或等价 coordinator 提交运动和动画命令
- **AND** MUST NOT 新增绕过该入口的 per-action controller
- **AND** MUST NOT 复制 `BBBCharacterController`、`PlayerStateRegistry` 或 `PlayerBaseState`

### Requirement: Locomotion 子树映射
系统 MUST 将现有 `Idle / MoveStart / MoveLoop / MoveStop` Locomotion phase 映射为 FullBody 状态路径。该映射 MUST 复用现有 `BasicLocomotionStateMachine` 或等价 Locomotion 局部状态图，不得复制一份新的基础移动 transition 规则。

#### Scenario: WASD 是 Locomotion 子状态机职责
- **WHEN** 系统处理基础 WASD 移动
- **THEN** `Idle / MoveStart / MoveLoop / MoveStop` MUST 属于 `FullBody/Locomotion` 子域
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

#### Scenario: Locomotion transition 规则不复制
- **WHEN** `MoveStop` 中重新出现移动输入
- **THEN** `MoveStop -> MoveStart` MUST 仍由现有 Locomotion 局部状态图决定
- **AND** FullBody HFSM MUST NOT 通过另一套独立条件重新实现该规则

### Requirement: Action.Dodge 子状态映射
系统 MUST 将现有 `Action.Dodge` 接入 FullBody HFSM 的 Action 子状态。`Action.Dodge` 的进入许可 MUST 继续由 Action 仲裁决定，动作生命周期和输出 MUST 继续复用现有 Action module 或等价模块。

#### Scenario: Dodge accepted 进入 Action 路径
- **GIVEN** 输入缓冲存在有效 Dodge 请求
- **AND** Action 仲裁接受该请求
- **WHEN** FullBody HFSM 处理本帧
- **THEN** FullBody 状态路径 MUST 进入 `/FullBody/Action/Dodge` 或等价层级路径
- **AND** 当前 FullBody owner MUST 为 Action
- **AND** 当前 Action state MUST 为 `Action.Dodge`

#### Scenario: Dodge active 期间压制 Locomotion 输出
- **GIVEN** 当前 FullBody 状态路径为 `/FullBody/Action/Dodge`
- **WHEN** 本帧处理运动和动画输出
- **THEN** 系统 MUST 提交 Dodge 动作运动或动画命令
- **AND** Locomotion MUST NOT 同时提交平面位移命令
- **AND** Locomotion MUST NOT 同时提交 base layer 动画上下文

#### Scenario: Dodge 完成回到 Locomotion
- **GIVEN** 当前 FullBody 状态路径为 `/FullBody/Action/Dodge`
- **WHEN** Dodge module 报告完成
- **THEN** FullBody HFSM MUST 退出 Action.Dodge
- **AND** 状态路径 MUST 回到 `FullBody/Locomotion` 子树
- **AND** `ActionRuntimeStateTracker` MUST 回到 `Action.None` 或等价空 action state

### Requirement: FullBody 状态快照
系统 MUST 暴露统一 FullBody 状态快照，用于调试、测试和后续同步映射。快照 MUST 使用稳定 ID 和可读路径表达当前状态，不得暴露 UnityHFSM 内部对象、Unity 场景对象或动画播放对象作为权威状态。

#### Scenario: 快照包含核心字段
- **WHEN** FullBody 主调度入口完成一帧处理
- **THEN** 快照 MUST 包含当前 FullBody owner
- **AND** MUST 包含当前可读状态路径
- **AND** MUST 包含当前 Locomotion phase
- **AND** MUST 包含当前 Action state
- **AND** MUST 包含当前状态持续时间或等价时间事实

#### Scenario: 快照不泄漏具体运行时对象
- **WHEN** 读取 FullBody 状态快照
- **THEN** 快照 MUST NOT 暴露 Animancer state、Animator state、AnimationClip、CharacterController、InputAction、Cinemachine 实例或 UnityHFSM 内部 state 对象
- **AND** 后续网络同步 MUST 能基于稳定 ID 另行映射，而不是直接同步这些运行时对象

### Requirement: HFSM 与输出权威分离
系统 MUST 保持 FullBody HFSM、动画 Presenter 和 motion executor 的职责分离。HFSM 负责状态路径和 transition 权威，Presenter 只消费动画命令并反馈播放事实，motion executor 只执行当前 owner 的运动命令。

#### Scenario: HFSM 不直接播放动画
- **WHEN** FullBody HFSM 进入或更新某个状态
- **THEN** HFSM MAY 产出状态事实或状态输出意图
- **AND** MUST NOT 直接调用 Animancer 或 Animator 播放 API
- **AND** MUST NOT 通过动画回调直接决定业务状态切换

#### Scenario: HFSM 不直接移动角色
- **WHEN** FullBody HFSM 进入或更新某个状态
- **THEN** HFSM MAY 决定当前运动命令来源
- **AND** MUST NOT 直接调用 `CharacterController.Move`
- **AND** MUST NOT 直接写入角色 `Transform.position`

#### Scenario: Action 仲裁不藏进 transition 顺序
- **WHEN** FullBody Action 请求参与状态切换
- **THEN** 是否允许进入 Action MUST 由 `ActionInterruptArbiter` 或等价仲裁层决定
- **AND** HFSM transition MUST 读取仲裁结果或 module active 事实
- **AND** MUST NOT 仅依赖多个全局 transition 的注册顺序表达业务优先级

### Requirement: 可测试和可验证
系统 MUST 为 FullBody HFSM 状态树提供自动测试、静态边界验证和手动验证，证明状态树提升了可见性且没有破坏现有 Locomotion 和 Dodge 行为。

#### Scenario: 自动测试覆盖主路径
- **WHEN** 运行 FullBody HFSM EditMode 测试
- **THEN** 测试 MUST 覆盖初始 Locomotion Idle 路径
- **AND** MUST 覆盖移动输入进入 Locomotion MoveStart 或 MoveLoop 路径
- **AND** MUST 覆盖 Dodge accepted 进入 Action.Dodge 路径
- **AND** MUST 覆盖 Dodge completed 回到 Locomotion 路径

#### Scenario: 静态边界验证
- **WHEN** 检查 FullBody HFSM 新增源码
- **THEN** 静态搜索 MUST 能确认新增源码不引用 `BBBNexus`
- **AND** MUST 能确认新增源码不直接调用 `CharacterController.Move`
- **AND** MUST 能确认新增源码不直接调用 Animancer 或 Animator 播放 API

#### Scenario: 手动验证状态路径
- **WHEN** 用户在 Play Mode 中测试普通 WASD 和 Shift Dodge
- **THEN** 调试信息 MUST 能显示当前 FullBody 状态路径
- **AND** 普通 WASD MUST 显示 Locomotion 子路径变化
- **AND** Shift Dodge MUST 显示 Action.Dodge 子路径
- **AND** Dodge active 时基础移动 MUST 不叠加额外平面位移或 base layer 动画
