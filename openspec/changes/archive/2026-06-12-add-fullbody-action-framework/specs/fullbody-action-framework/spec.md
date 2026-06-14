## ADDED Requirements
### Requirement: FullBody 主调度入口
系统 MUST 提供一个 FullBody 主行为域调度入口，用于协调基础 Locomotion 和全身 Action。该入口 MUST 负责每帧行为 owner 选择、运动命令提交和 base layer 动画命令提交。该入口 MUST NOT 复制 BBB 的完整角色主控、状态注册表或运行时命名空间依赖。

#### Scenario: 单一 FullBody owner
- **WHEN** FullBody 主调度入口处理一帧角色逻辑
- **THEN** 系统 MUST 选择一个当前 FullBody owner
- **AND** owner MAY 是基础 Locomotion
- **AND** owner MAY 是一个 active FullBody Action
- **AND** 系统 MUST NOT 允许 Locomotion 和 FullBody Action 同帧同时拥有平面位移权威

#### Scenario: 不复制 BBB 主控
- **WHEN** 实现 FullBody 主调度入口
- **THEN** 新增运行时代码 MUST NOT 引用 `BBBNexus` 命名空间
- **AND** MUST NOT 复制 `BBBCharacterController`、`PlayerStateRegistry`、`PlayerBaseState` 或 `OverrideState` 作为项目运行时主线

#### Scenario: 薄入口不写死具体动作
- **WHEN** FullBody 主调度入口初始化
- **THEN** 它 MUST 通过配置、注册表、序列化引用或等价端口发现可用 FullBody Action module
- **AND** MUST NOT 在主调度入口中写死 Dodge 运动数值、具体动画资源或具体角色动画名

### Requirement: Locomotion 作为 FullBody 子职责
系统 MUST 将基础 Locomotion 视为 FullBody 主层下的局部子图或模块。Locomotion MAY 继续使用 `Idle / MoveStart / MoveLoop / MoveStop` 局部状态图，但它的运动和 base layer 动画提交 MUST 受 FullBody 主调度入口选择结果控制。

#### Scenario: 无 Action 时提交 Locomotion
- **GIVEN** 当前没有 active FullBody Action
- **WHEN** FullBody 主调度入口处理本帧
- **THEN** Locomotion MAY 成为当前 FullBody owner
- **AND** 系统 MAY 提交基础移动运动命令
- **AND** 系统 MAY 提交基础移动 base layer 动画上下文

#### Scenario: Action active 时 Locomotion 只提供事实
- **GIVEN** 当前存在 active FullBody Action
- **WHEN** FullBody 主调度入口处理本帧
- **THEN** Locomotion MAY 继续提供 Move/Look、移动意图、世界方向或 phase 事实
- **AND** Locomotion MUST NOT 同时提交平面位移命令
- **AND** Locomotion MUST NOT 同时提交 base layer 动画上下文

#### Scenario: Locomotion 局部状态图不接管 Action
- **WHEN** 玩家触发 Dodge、Roll、Jump 或等价 FullBody Action 请求
- **THEN** Locomotion 局部状态图 MUST NOT 自行消费该请求
- **AND** MUST NOT 把该请求建模为 `BasicMovementPhase`
- **AND** MUST NOT 把该请求建模为 Walk/Run gait

### Requirement: FullBody Action Module
系统 MUST 提供 FullBody Action module 端口，使单个全身动作能通过统一请求、仲裁、生命周期、运动输出和动画输出接入 FullBody 主行为域。Action module MUST 是 FullBody 主层内部模块，不得成为独立角色控制路径。

#### Scenario: Module 不是独立 Action 状态机
- **WHEN** 系统注册或执行 FullBody Action module
- **THEN** module MUST 作为 FullBody 主树 Action 叶子的行为执行单元存在
- **AND** MUST NOT 拥有独立状态树拓扑
- **AND** MUST NOT 决定 FullBody owner
- **AND** MUST NOT 形成和 Locomotion 平级争夺 base layer 或平面位移的 Action 状态机

#### Scenario: Module 使用 Action 仲裁
- **GIVEN** 输入缓冲存在一个 FullBody Action 请求
- **WHEN** module 尝试进入动作
- **THEN** module MUST 通过 `ActionInterruptArbiter` 或等价 Action 仲裁判断是否允许进入
- **AND** accepted 时 MUST 更新 `ActionRuntimeStateTracker` 或等价 Action facts
- **AND** rejected 时 MUST 不消费未过期请求

#### Scenario: Module 输出命令而不直接执行
- **WHEN** module active tick 产生动作位移或动作动画
- **THEN** module MUST 输出纯数据运动命令或等价命令
- **AND** MUST 输出动作动画 key/command 或等价命令
- **AND** MUST NOT 直接调用 `CharacterController.Move`
- **AND** MUST NOT 直接调用 Animancer 或 Animator 播放 API

#### Scenario: Module 显式退出
- **GIVEN** module 当前 active
- **WHEN** module 达到自身退出条件
- **THEN** module MUST 显式退出到 `Action.None` 或等价空 action state
- **AND** `ActionRuntimeStateTracker` MUST NOT 因隐藏 duration 规则自动退出

### Requirement: FullBody 输出权威
系统 MUST 保证每帧平面位移和 base layer 动画都只有一个 FullBody owner。统一 motion executor 是位移执行权威，动画 Presenter 只消费命令并反馈播放事实。

#### Scenario: 单一运动提交
- **WHEN** FullBody 主调度入口处理本帧输出
- **THEN** 它 MUST 最多向统一 motion executor 提交一个平面运动命令来源
- **AND** 该来源 MUST 是当前 FullBody owner
- **AND** 动画 Presenter、Animancer 回调或 Transform 写入 MUST NOT 成为平面位移权威

#### Scenario: 单一 base layer 动画提交
- **WHEN** FullBody 主调度入口处理本帧动画输出
- **THEN** 它 MUST 最多向 base layer 动画 Presenter 提交一个 owner 的动画命令或上下文
- **AND** Locomotion 和 FullBody Action MUST NOT 同帧同时要求播放 base layer 主动画
- **AND** Presenter MUST NOT 决定业务 Action 是否允许进入

#### Scenario: Look 不被 FullBody 动作锁死
- **GIVEN** 当前存在 active FullBody Action
- **WHEN** 玩家输入 Look
- **THEN** 项目侧相机入口 MUST 继续接收 Look 输入或等价相机意图
- **AND** Action module MUST NOT 直接读取或控制 Cinemachine 具体实例

### Requirement: FullBody Action 逻辑配置和动画绑定
系统 MUST 提供 FullBody Action 逻辑配置入口，聚合角色可用 FullBody Action 的稳定 action id、运动参数和打断策略。动作动画表现 MUST 通过独立的动作动画绑定入口或等价边界按稳定 action id 解析。FullBody 主调度入口 MUST 显式引用动作逻辑配置和动作动画绑定配置，不得把动画 Profile 塞回动作逻辑定义。

#### Scenario: 角色级 Action 列表
- **WHEN** 设计者检查角色 FullBody Action 逻辑配置
- **THEN** 系统 MUST 提供一个角色级 ActionSet 或等价逻辑入口
- **AND** 该入口 MUST 能列出当前角色可用的 FullBody Action
- **AND** action id MUST 使用稳定 ID

#### Scenario: Action 定义聚合逻辑子配置
- **WHEN** 设计者检查 `Action.Dodge` 或等价 Action 定义
- **THEN** 该定义 MUST 能定位动作运动参数配置
- **AND** MUST 能定位打断策略配置
- **AND** MUST NOT 直接持有动作动画 Profile

#### Scenario: 缺失配置可校验
- **GIVEN** Action 定义缺失必要 action id、运动参数或打断策略
- **WHEN** 运行配置校验
- **THEN** 校验结果 MUST 报告错误
- **AND** MUST 不要求设计者进入多个游离动作逻辑资产才能发现逻辑配置缺口

#### Scenario: 动作动画绑定独立解析
- **WHEN** FullBody 主调度入口准备提交 `Action.Dodge` 动画命令
- **THEN** 系统 MUST 通过动作动画绑定集或等价动画配置入口解析 `Action.Dodge` 的动作动画 Profile
- **AND** 该绑定入口 MUST 能校验缺失 Profile 或必要动作动画 key
- **AND** 动作动画绑定入口 MUST NOT 定义 FullBody 状态树拓扑、动作进入条件或动作位移权威

#### Scenario: Locomotion 配置不并入 Action
- **WHEN** 设计者配置基础 Locomotion 状态图、Walk/Run alias 或 TransitionLibrary
- **THEN** 这些配置 MUST 仍属于 Locomotion 配置入口
- **AND** FullBody Action 定义 MUST NOT 接管 `Idle / MoveStart / MoveLoop / MoveStop` 的 Locomotion 状态图规则

### Requirement: 当前变更只实现层级 FullBody 主树
系统 MUST 在本变更中只实现一个层级 FullBody 主行为树。该主树 MAY 包含 `FullBody/Locomotion` 和 `FullBody/Action` 子域，但 MUST NOT 引入并行状态层、UpperBody 状态机、AvatarMask layer 编排或 IK/Additive 状态层。

#### Scenario: 不引入并行表现层
- **WHEN** 实现 FullBody Action 框架
- **THEN** 系统 MUST NOT 在本变更中创建 UpperBody、Facial、IK、Additive 或等价并行表现状态层
- **AND** MUST NOT 使用并行状态层参与 FullBody owner 选择
- **AND** 后续如需并行表现层 MUST 另开 OpenSpec 说明层职责、动画合成和验证方式

#### Scenario: 层级子域互斥提交
- **WHEN** FullBody 主调度入口处理本帧
- **THEN** `FullBody/Locomotion` 和 `FullBody/Action` MUST 服从同一个 owner 选择
- **AND** 二者 MUST NOT 同帧同时提交 base layer 动画或平面位移

### Requirement: FullBody 固定调度顺序
系统 MUST 为 FullBody 主行为域提供可测试的固定调度顺序，使输入、Locomotion 意图、Action 仲裁、行为选择、运动输出、动画输出和相机处理保持确定。

#### Scenario: 调度顺序固定
- **WHEN** FullBody 主调度入口处理一帧
- **THEN** 系统 MUST 先收集输入事实和本地输入请求
- **AND** MUST 再生成 Locomotion 意图和世界方向事实
- **AND** MUST 再处理 FullBody Action 请求与 Action 仲裁
- **AND** MUST 再选择当前 FullBody owner
- **AND** MUST 再提交当前 owner 的运动命令
- **AND** MUST 再提交当前 owner 的 base layer 动画命令或上下文
- **AND** MUST 最后处理或请求相机 Resolve

#### Scenario: 同输入序列结果稳定
- **WHEN** 使用相同输入序列、相同配置和相同 delta/tick 序列运行 FullBody 主调度
- **THEN** 行为 owner 序列 MUST 一致
- **AND** Action 请求消费结果 MUST 一致
- **AND** 提交的运动命令来源 MUST 一致

### Requirement: FullBody 框架可测试和可验证
系统 MUST 为 FullBody Action 框架提供 EditMode 测试、静态边界验证和手动验证，证明框架没有引入分裂路径，也没有破坏现有基础移动和 Dodge 行为。

#### Scenario: 自动测试覆盖 owner 切换
- **WHEN** 运行 FullBody Action framework EditMode 测试
- **THEN** 测试 MUST 覆盖无 Action 时 Locomotion 成为 owner
- **AND** MUST 覆盖 Dodge active 时 Dodge 成为 owner
- **AND** MUST 覆盖 Dodge 结束后返回 Locomotion owner

#### Scenario: 静态边界验证
- **WHEN** 检查 FullBody framework 新增源码
- **THEN** 静态搜索 MUST 能确认新增源码不引用 `BBBNexus`
- **AND** MUST 能确认 Action module 不调用 `CharacterController.Move`
- **AND** MUST 能确认 Action module 不直接调用 Animancer 播放 API

#### Scenario: 手动验证
- **WHEN** 用户在 Play Mode 中测试普通移动和 Shift Dodge
- **THEN** 普通 WASD MUST 仍能进入 Idle、MoveStart、MoveLoop 和 MoveStop
- **AND** Dodge active 时基础移动 MUST 不叠加额外平面位移
- **AND** Dodge 结束后再次按 Shift MUST 能重新触发
