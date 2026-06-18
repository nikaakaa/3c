# simulation-tick-system Specification

## Purpose
定义 simulation tick 系统的固定步进、tick driver、调度顺序和与现有玩法主线的接入边界。
## Requirements
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
系统 MUST 使用固定且可测试的 tick phase 顺序调度输入、玩法、运动、表现桥接和快照。表现桥接 MUST 晚于运动执行且早于快照写入，使本 tick 的动画事实能够进入同 tick 快照。

#### Scenario: phase 顺序固定
- **WHEN** tick runner 执行 tick N
- **THEN** runner MUST 依次执行 ReadInput、UpdateInputBuffer、GameplayDecision、BuildMotion、ExecuteMotion、PresentationBridge、WriteSnapshotAndEvents

#### Scenario: 输入早于玩法判定
- **WHEN** GameplayDecision phase 运行
- **THEN** ReadInput 和 UpdateInputBuffer phase MUST 已在同一 tick 内完成

#### Scenario: 表现桥接晚于运动执行
- **WHEN** PresentationBridge phase 运行
- **THEN** ExecuteMotion phase MUST 已在同一 tick 内完成

#### Scenario: 快照晚于 Character 输出
- **WHEN** WriteSnapshotAndEvents phase 运行
- **THEN** ExecuteMotion 和 PresentationBridge phase MUST 已在同一 tick 内完成

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
系统 MUST 让 tick 系统调度现有 Character frame pipeline 主线，而不是新增绕过 `CharacterFrameRuntimeController`、`LocomotionRuntimeModule` 或 motion executor 的第二套基础移动路径。

#### Scenario: 现有主线仍是移动入口
- **WHEN** 基础移动在 simulation tick 中执行
- **THEN** 系统 MUST 继续通过 `CharacterFrameRuntimeController` 或等价角色级入口推进 `CharacterFramePipeline`
- **AND** MUST 继续通过 `IBasicLocomotionMotionExecutor` 或等价正式运动端口提交移动

#### Scenario: 不新增第二控制入口
- **WHEN** 实施 tick 系统
- **THEN** 系统 MUST NOT 新增绕过当前 Character frame pipeline 的 player movement controller

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

### Requirement: 表现层插值读数
系统 MUST 为渲染帧表现层提供只读 simulation tick 插值读数，使角色可见表现、相机、动画、VFX 或 UI 等表现系统可以基于当前 tick 余量计算 0..1 的插值 alpha，同时不得改变 tick 权威推进语义。

#### Scenario: 不足一个 tick 时输出 alpha
- **WHEN** 客户端 tick accumulator 累积了小于一个 fixed delta 的剩余时间
- **THEN** 表现层 MUST 能读取到 0..1 范围内的 interpolation alpha
- **AND** alpha MUST 表达剩余时间相对 fixed delta 的比例

#### Scenario: tick 推进后保留余量语义
- **WHEN** 单个渲染帧产生一个或多个 simulation tick
- **THEN** tick accumulator MUST 保留追帧后的剩余时间
- **AND** 表现层读取到的 alpha MUST 基于该剩余时间计算

#### Scenario: 只读边界
- **WHEN** 角色可见表现、相机、动画、VFX 或 UI 表现层读取 interpolation alpha
- **THEN** 它们 MUST NOT 修改 accumulator 内部状态
- **AND** 它们 MUST NOT 改变 `SimulationTick` 单调推进结果

#### Scenario: core 不依赖表现系统
- **WHEN** 检查 simulation core 代码
- **THEN** simulation core MUST NOT 引用 Cinemachine、相机 runtime、Animancer、VFX、UI 或场景 Transform 类型
- **AND** 表现层适配 MUST 位于 runtime adapter 边界

### Requirement: Character Gameplay Phase 接入
系统 MUST 让当前角色 gameplay 主线通过唯一 `CharacterFramePipeline` 接入 `SimulationTickPhase` 的固定顺序，而不是将输入缓冲更新、玩法判定、运动构建、运动执行和表现提交整包放入单个 `ExecuteMotion` handler。tick runner 仍只负责调度，具体玩法逻辑必须位于 Character frame pipeline、LocomotionSource、CommittedActionSource 或其 adapter 中。

#### Scenario: 输入缓冲早于玩法判定
- **WHEN** tick runner 执行 tick N
- **THEN** 输入请求缓冲更新 MUST 发生在 `UpdateInputBuffer` phase
- **AND** Action 请求仲裁 MUST 发生在 `GameplayDecision` phase 或之后
- **AND** Action 仲裁 MUST 能看到同 tick 写入的输入请求
- **AND** 这些 phase MUST 由 Character frame pipeline 统一推进

#### Scenario: 状态决策早于运动执行
- **WHEN** tick runner 执行 tick N
- **THEN** Locomotion local graph 和 Action lifecycle 的 gameplay decision MUST 发生在 `GameplayDecision` phase
- **AND** 运动命令构建 MUST 发生在 `BuildMotion` phase
- **AND** motion executor 调用 MUST 只发生在 `ExecuteMotion` phase
- **AND** motion executor 调用 MUST 来自 Character frame pipeline 的统一 output applier

#### Scenario: 表现提交不早于运动执行
- **WHEN** tick runner 执行 tick N
- **THEN** base layer 动画命令提交 MUST 发生在运动命令已构建之后
- **AND** 动画播放事实写入 MUST 不作为同 tick 状态进入的前置权威
- **AND** 动画提交 MUST 来自 Character frame pipeline 的统一 output applier

#### Scenario: 快照晚于 Character 输出
- **WHEN** `WriteSnapshotAndEvents` phase 捕获角色快照
- **THEN** 本 tick 的 Character frame plan、输入消费、运动执行结果和 runtime facts 写入 MUST 已完成
- **AND** 快照 recorder MUST NOT 需要主动重跑 gameplay 逻辑来补齐状态
- **AND** snapshot/events commit MUST 发生在 Character frame pipeline 的统一提交阶段

### Requirement: Phase Handler 不形成旁路
系统 MUST 防止旧 FullBody phase handler、Locomotion-only phase handler、rollback/debug phase handler 和未来身体域 handler 形成多条 gameplay 推进路径。保留的 handler MUST 明确标识用途，并且不得在同一角色同一 tick 中重复推进状态机或重复执行运动。正式 gameplay handler MUST 进入唯一 Character frame pipeline。

#### Scenario: Character handler 是动作 demo 主路径
- **GIVEN** 当前 Sandbox 使用 Character frame 动作 demo
- **WHEN** tick driver 推进角色
- **THEN** Character frame pipeline handler MUST 是 Move、TurnBack、Dodge 和后续 Attack 的主 gameplay 推进路径
- **AND** CommittedActionSource MAY 提交 FullBody claim，但 FullBody MUST NOT 作为该管线下的 source
- **AND** locomotion-only handler MUST 不同时推进同一角色

#### Scenario: Locomotion-only handler 明确窄用途
- **GIVEN** 测试或诊断需要 locomotion-only replay
- **WHEN** 使用 locomotion-only handler
- **THEN** handler MUST 明确标识为 locomotion-only
- **AND** MUST NOT 被作为 Character frame 动作 demo 的完整验收路径
- **AND** MUST NOT 与 Character frame pipeline 同 tick 推进同一角色

#### Scenario: Debug handler 不推进 gameplay
- **WHEN** rollback debug runner、snapshot recorder 或 presentation probe 注册到 tick phase
- **THEN** 它们 MUST 只记录、恢复或比较数据
- **AND** MUST NOT 调用状态机 Tick 或 motion executor 作为正常 gameplay 推进
- **AND** MUST NOT 绕过 Character frame pipeline 提交角色 gameplay 副作用

### Requirement: Simulation Tick 使用 Character Runtime 入口
simulation tick system MUST 通过 Character 级 runtime tick adapter 推进正式角色 gameplay。FullBody tick adapter、Locomotion tick adapter 或 per-action tick adapter MUST NOT 作为 Corin 正式 playable 主线的最高 tick registration owner。

#### Scenario: Tick driver 调用 CharacterFrameRuntimeController
- **GIVEN** 场景启用了 `UnitySimulationTickDriver`
- **WHEN** tick runner 执行角色 gameplay phases
- **THEN** phase handler MUST 调用 `CharacterFrameRuntimeController` 或等价角色级 runtime controller
- **AND** MUST 使用同一个 `CharacterFrameRuntimeHost`
- **AND** MUST NOT 通过 旧 FullBody action controller FramePipelineHost 作为正式路径推进

#### Scenario: FullBody tick adapter 退役
- **WHEN** 检查正式 Corin simulation tick 装配
- **THEN** 旧 FullBody action tick adapter MUST 不作为正式注册者
- **AND** 它 MAY 被删除、标记 obsolete 或转发到角色级 tick adapter
- **AND** 它 MUST NOT 创建独立 frame context 或 runtime host

#### Scenario: Locomotion tick adapter 不竞争 gameplay
- **WHEN** 同一角色存在 Locomotion tick adapter 或诊断 tick adapter
- **THEN** 该 adapter MUST NOT 与 Character runtime tick adapter 同时推进 gameplay
- **AND** 冲突 MUST 被装配校验或自动测试捕获
- **AND** 系统 MUST NOT 依赖运行时互相压制来维持长期正确性
