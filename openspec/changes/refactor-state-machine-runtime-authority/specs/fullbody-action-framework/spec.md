## MODIFIED Requirements
### Requirement: FullBody 主调度入口
系统 MUST 提供一个 FullBody 主行为域调度入口，用于协调基础 Locomotion 和全身 Action。该入口 MUST 负责每帧行为 owner 选择、运动命令提交和 base layer 动画命令提交。该入口 MUST 拥有当前角色唯一的统一状态机 runner，并作为正式 gameplay 路径唯一推进该 runner 的模块。该入口 MUST NOT 复制 BBB 的完整角色主控、状态注册表或运行时命名空间依赖。

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

#### Scenario: 唯一 runner owner
- **WHEN** FullBody 主调度入口启用
- **THEN** 它 MUST 构建或恢复当前角色唯一的 `CharacterStateMachineRunner`
- **AND** 本帧 Locomotion facts、Action request facts、动画播放 facts 和 runtime blackboard facts MUST 输入同一个 runner
- **AND** 本帧 owner、运动输出和动画输出 MUST 来自同一个 runner 的 frame result

### Requirement: Locomotion 作为 FullBody 子职责
系统 MUST 将基础 Locomotion 视为 FullBody 主层下的局部子图或模块。Locomotion MAY 继续提供 `Idle / MoveStart / MoveLoop / MoveStop / TurnBack` 的事实构建、运动命令构建和动画表现桥接，但它的状态推进、运动提交和 base layer 动画提交 MUST 受 FullBody 主调度入口选择结果控制。

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

#### Scenario: Locomotion 不持有 runtime runner
- **WHEN** 当前角色通过正式 FullBody gameplay 路径运行
- **THEN** Locomotion adapter MUST NOT 持有、创建或重置 `CharacterStateMachineRunner`
- **AND** Locomotion adapter MUST NOT 维护独立 active state path 作为业务真值
- **AND** Locomotion adapter 的调试状态 MUST 来自 FullBody 主调度入口输出或明确标记为本地缓存

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
- **AND** MUST 能确认正式运行时代码只有 FullBody 主调度入口创建 `CharacterStateMachineRunner`

#### Scenario: 手动验证
- **WHEN** 用户在 Play Mode 中测试普通移动和 Shift Dodge
- **THEN** 普通 WASD MUST 仍能进入 Idle、MoveStart、MoveLoop 和 MoveStop
- **AND** Dodge active 时基础移动 MUST 不叠加额外平面位移
- **AND** Dodge 结束后再次按 Shift MUST 能重新触发
