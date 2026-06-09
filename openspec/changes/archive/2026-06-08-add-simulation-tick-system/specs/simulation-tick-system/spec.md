## ADDED Requirements

### Requirement: Simulation Tick 标识
系统 MUST 使用项目级 `SimulationTick` 表达模拟步编号，使客户端、服务端、输入缓冲、玩法判定和未来回滚系统共享同一 tick 语义。

#### Scenario: tick 单调推进
- **WHEN** tick 系统从 tick N 推进一个模拟步
- **THEN** 当前 tick MUST 变为 N+1
- **AND** tick id MUST 可比较

#### Scenario: tick 可序列化
- **WHEN** 输入事实、状态快照或诊断信息需要标记模拟步
- **THEN** 系统 MUST 能读取 `SimulationTick` 的稳定整数值
- **AND** MUST NOT 使用浮点时间作为 tick 的主标识

### Requirement: 固定 Tick Rate
系统 MUST 使用固定 tick rate 派生 fixed delta，使客户端和服务端可以在同一模拟时间步上运行。

#### Scenario: 从 tick rate 派生 fixed delta
- **WHEN** tick rate 配置为 60 ticks per second
- **THEN** fixed delta MUST 等于 1/60 秒的语义值

#### Scenario: 非法 tick rate 被拒绝
- **WHEN** tick rate 小于或等于 0
- **THEN** 系统 MUST 拒绝该配置
- **AND** MUST NOT 产生可运行的 tick 设置

#### Scenario: 不绑定 Unity 固定帧
- **WHEN** tick rate 被读取
- **THEN** tick 系统 MUST NOT 依赖 Unity `Time.fixedDeltaTime` 作为唯一事实来源

### Requirement: 客户端 Tick Accumulator
系统 MUST 在 Unity 客户端使用 accumulator 将可变帧时间转换为 0..N 个固定 simulation tick，并通过最大追帧上限避免单帧无限补 tick。

#### Scenario: 不足一个 tick
- **WHEN** 累积 delta 小于 fixed delta
- **THEN** accumulator MUST 输出 0 个 simulation tick
- **AND** MUST 保留剩余时间用于后续帧

#### Scenario: 多个 tick
- **WHEN** 累积 delta 覆盖多个 fixed delta
- **THEN** accumulator MUST 输出对应数量的 simulation tick
- **AND** 每个 tick MUST 使用连续 tick id

#### Scenario: 追帧上限
- **WHEN** 单帧累积 delta 可产生的 tick 数超过配置上限
- **THEN** accumulator MUST 最多输出上限数量的 tick
- **AND** MUST 以测试覆盖超限余量处理策略

### Requirement: 服务端 Tick Driver 合约
系统 MUST 为服务端提供不依赖 Unity 生命周期的 tick driver 合约，使 Fantasy 服务端后续能按同一 tick rate 推进权威模拟。

#### Scenario: 服务端手动推进
- **WHEN** 服务端测试或服务器主循环请求推进一个 tick
- **THEN** 服务端 tick driver MUST 使用同一 `SimulationTick` 和 fixed delta 语义生成 tick context

#### Scenario: 不依赖 Unity Update
- **WHEN** 服务端 tick driver 被实现
- **THEN** 它 MUST NOT 依赖 Unity `Update`、`Time.deltaTime` 或 Unity 场景对象

#### Scenario: 协议不在本变更修改
- **WHEN** 实施项目级 tick 系统
- **THEN** 实施 MUST NOT 修改 Fantasy proto
- **AND** MUST NOT 新增真实网络发包流程

### Requirement: Tick Phase 顺序
系统 MUST 使用固定且可测试的 tick phase 顺序调度输入、玩法、运动、快照和表现桥接。

#### Scenario: phase 顺序固定
- **WHEN** tick runner 执行 tick N
- **THEN** runner MUST 依次执行 ReadInput、UpdateInputBuffer、GameplayDecision、BuildMotion、ExecuteMotion、WriteSnapshotAndEvents、PresentationBridge

#### Scenario: 输入早于玩法判定
- **WHEN** GameplayDecision phase 运行
- **THEN** ReadInput 和 UpdateInputBuffer phase MUST 已在同一 tick 内完成

#### Scenario: 快照晚于运动执行
- **WHEN** WriteSnapshotAndEvents phase 运行
- **THEN** ExecuteMotion phase MUST 已在同一 tick 内完成

### Requirement: Tick Runner 纯调度边界
系统 MUST 将 tick runner 作为纯调度层，不得让 runner 直接拥有具体输入读取、角色位移、动画播放或网络协议职责。

#### Scenario: runner 调度 handler
- **WHEN** 某个 phase 注册了 handler
- **THEN** runner MUST 传入当前 tick context 调用该 handler

#### Scenario: 空 phase 安全跳过
- **WHEN** 某个 phase 没有注册 handler
- **THEN** runner MUST 跳过该 phase
- **AND** MUST 继续执行后续 phase

#### Scenario: runner 不接管运动
- **WHEN** runner 执行 ExecuteMotion phase
- **THEN** runner MUST NOT 直接调用 `CharacterController.Move`
- **AND** MUST NOT 直接播放 Animancer 动画

### Requirement: 输入缓冲 Tick 接入边界
系统 MUST 允许本地输入缓冲使用项目级 simulation tick 作为请求窗口的 step 事实，但输入缓冲仍只能保存输入事实和请求。

#### Scenario: 输入请求绑定 tick
- **WHEN** 玩家在 tick N 按下 Attack、Dodge、Jump 或 Interact
- **THEN** 输入缓冲 MUST 能用 tick N 作为请求来源 step
- **AND** MUST 能基于 tick 窗口计算过期 step

#### Scenario: 预输入不决定动作结果
- **WHEN** 输入缓冲记录一个 Attack 请求
- **THEN** 输入缓冲 MUST NOT 记录该请求必定在未来某个 tick 触发攻击动作
- **AND** MUST 等待玩法判定或状态机消费请求

#### Scenario: 输入缓冲不发包
- **WHEN** 输入缓冲更新请求
- **THEN** 输入缓冲 MUST NOT 直接发送网络包
- **AND** MUST NOT 修改网络协议 DTO

### Requirement: Locomotion 主线保持
系统 MUST 让 tick 系统调度现有 Locomotion 主线，而不是新增绕过 `PlayerLocomotionController`、`BasicLocomotionPipeline` 或 motion executor 的第二套基础移动路径。

#### Scenario: 现有主线仍是移动入口
- **WHEN** 基础移动在 simulation tick 中执行
- **THEN** 系统 MUST 继续通过现有 `PlayerLocomotionController` 或等价 adapter 调用 `BasicLocomotionPipeline`
- **AND** MUST 继续通过 `IBasicLocomotionMotionExecutor` 提交移动

#### Scenario: 不新增第二控制入口
- **WHEN** 实施 tick 系统
- **THEN** 系统 MUST NOT 新增绕过当前 Locomotion pipeline 的 player movement controller

#### Scenario: 表现层仍只读结果
- **WHEN** PresentationBridge phase 更新动画表现
- **THEN** 表现层 MUST 读取模拟或运动结果
- **AND** MUST NOT 接管基础位移权威

### Requirement: 预测回滚预留边界
系统 MUST 为未来 GGPO 风格的输入历史、状态快照、回滚和重放预留边界，但本变更不得实现完整 rollback runtime。

#### Scenario: 输入历史可按 tick 对齐
- **WHEN** 后续系统记录本地或远端输入事实
- **THEN** 输入历史 MUST 能以 `SimulationTick` 对齐输入

#### Scenario: 快照历史可按 tick 对齐
- **WHEN** 后续系统保存状态快照
- **THEN** 快照 MUST 能以 `SimulationTick` 标记保存点
- **AND** 快照数据 MUST 不依赖 Unity 场景对象

#### Scenario: 本变更不实现 rollback
- **WHEN** 实施项目级 tick 系统
- **THEN** 实施 MUST NOT 新增完整回滚驱动
- **AND** MUST NOT 新增状态快照历史实现
- **AND** MUST NOT 新增服务器权威校正流程

### Requirement: 可测试和可诊断
系统 MUST 用自动测试和静态验证证明 tick 系统的确定性边界、phase 顺序和非侵入性。

#### Scenario: 自动测试覆盖 tick core
- **WHEN** 运行定向 EditMode 测试
- **THEN** 测试 MUST 覆盖 tick id、tick rate、accumulator、追帧上限和 runner phase 顺序

#### Scenario: 静态验证纯 core
- **WHEN** 检查 tick core 目录
- **THEN** tick core MUST NOT 引用 Animancer、Cinemachine、CharacterController 或 Input System adapter 类型

#### Scenario: 手动验证现有玩法不回退
- **WHEN** 用户在当前演示场景进入 Play Mode
- **THEN** WASD、Look、Idle、MoveStart、MoveLoop、MoveStop 行为 MUST 不因为 tick 系统地基而回退
