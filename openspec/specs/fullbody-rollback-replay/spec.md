# fullbody-rollback-replay Specification

## Purpose
定义 FullBody 回滚重放主线、输入帧回灌、状态 Capture/Restore、runtime facts 收敛和 Fantasy 接入前的边界。
## Requirements
### Requirement: FullBody 回滚重放主入口
系统 MUST 提供 FullBody 回滚重放能力，使本地 synctest 的 replay 能通过当前 `PlayerFullBodyActionController` 主线推进动作、移动、动作事实和动画事实。该能力 MUST 复用现有 `PlayerFullBodyActionController`、`PlayerLocomotionController`、`InputRequestBuffer` 和统一状态机，不得新增第二套角色控制器或第二套状态机。

#### Scenario: 重放走 FullBody 主线
- **GIVEN** replay adapter 收到 tick N 的 `PredictionInputFrame`
- **WHEN** replay 推进 tick N
- **THEN** 系统 MUST 将输入帧转换为 `BasicLocomotionInputSnapshot`
- **AND** MUST 调用 `PlayerFullBodyActionController.Tick(...)` 或等价当前 FullBody 主入口
- **AND** MUST NOT 只调用 `PlayerLocomotionController.Tick(...)` 作为 FullBody replay 的最终路径

#### Scenario: 保留 locomotion-only adapter
- **GIVEN** 现有 locomotion-only synctest 测试仍需要窄范围验证
- **WHEN** 用户选择 locomotion-only replay adapter
- **THEN** 系统 MAY 继续只通过 `PlayerLocomotionController` 重放
- **AND** 该 adapter MUST 明确标识为 locomotion-only，不得作为 Sandbox 动作 demo 的完整回滚验收

#### Scenario: 不创建分裂控制路径
- **WHEN** FullBody replay 推进角色
- **THEN** 系统 MUST NOT 直接调用 `BasicLocomotionPipeline`
- **AND** MUST NOT 直接调用 `CharacterController.Move`
- **AND** MUST NOT 通过旧 FullBody/HFSM/Dodge 缝合路径恢复状态权威

### Requirement: 输入帧回灌到输入请求缓冲
系统 MUST 能从 `PredictionInputFrame` 的离散按钮事实重建 `InputRequestBuffer` 请求，使 Dodge、Attack、Jump 和 Interact 在 replay 中重新经过玩法准入和消费规则。输入历史 MUST 继续保存输入事实，不得保存“已进入某动作”的结果。

#### Scenario: Dodge pressed 生成请求
- **GIVEN** tick N 的 `PredictionInputFrame.Dodge.Pressed` 为 true
- **WHEN** FullBody replay 推进 tick N
- **THEN** 系统 MUST 在 tick N 将 Dodge pressed 回灌为 Dodge 输入请求
- **AND** `FullBodyActionInterruptGate` MUST 能在同 tick 看到该请求

#### Scenario: held 不重复生成请求
- **GIVEN** tick N 的按钮事实只有 held 且 pressed 为 false
- **WHEN** FullBody replay 推进 tick N
- **THEN** 系统 MUST NOT 为该 held 事实重复生成 pressed 请求

#### Scenario: released 不生成动作请求
- **GIVEN** tick N 的按钮事实只有 released
- **WHEN** FullBody replay 推进 tick N
- **THEN** 系统 MUST NOT 为 released 事实生成新的动作请求

#### Scenario: 请求 step 与 simulation tick 对齐
- **GIVEN** replay 正在推进 tick N
- **WHEN** 系统写入 `InputRequestBufferComponent`
- **THEN** buffer 的 current step MUST 设置为 N 或等价 tick step
- **AND** 过期请求 MUST 基于 N 裁剪

### Requirement: FullBody 状态 Capture/Restore
系统 MUST 定义 FullBody action 运行时状态的纯数据 capture/restore 边界，使 replay 从历史 tick 恢复时，FullBody 当前状态、动作状态、pending transition 和影响下一 tick 输出的事实能回到快照时刻。restore state MUST NOT 保存 Unity Object、Animancer runtime object、Animator、AnimationClip、InputAction 或场景实例引用。

#### Scenario: 捕获 FullBody 状态
- **WHEN** tick N 的角色模拟快照被创建
- **THEN** 系统 MUST 能捕获当前 FullBody owner、action state、state time、variant 和 pending transition 或等价事实
- **AND** 捕获结果 MUST 是纯数据

#### Scenario: 恢复 FullBody 状态
- **GIVEN** 系统持有 tick N 的 FullBody restore state
- **WHEN** replay 从 tick N 恢复
- **THEN** `PlayerFullBodyActionController` MUST 恢复到该 restore state
- **AND** 恢复后下一 tick 的 FullBody active state MUST 与恢复前同一输入序列一致

#### Scenario: 不污染输入历史
- **WHEN** replay 恢复 FullBody 状态
- **THEN** 系统 MUST NOT 将动作消费结果写回 `PredictionInputHistory`
- **AND** 后续 replay MUST 仍从原始输入帧重新推导动作请求

### Requirement: FullBody Runtime Facts 收敛
系统 MUST 在 FullBody replay 中重新写入 action facts、animation facts 和 runtime blackboard facts，使同一输入序列重放后的最终快照能与原始快照在定义容差内比较。若 animation facts 仍受表现层播放进度影响，系统 MUST 输出字段级 differences 以定位缺口。

#### Scenario: Action facts 重放收敛
- **GIVEN** 原始运行中 Dodge 请求被 FullBody 主线接受
- **WHEN** replay 使用同一段输入重放到同一 end tick
- **THEN** replay 后的 action active/state/completed/sourceStep MUST 与原始快照一致或输出明确 differences

#### Scenario: Animation facts 通过可控事实源测试
- **GIVEN** 自动测试使用 fake animation presenter 或 fake playback progress source
- **WHEN** replay 使用同一段输入重放
- **THEN** replay 后的 animation key、normalized time 和 sourceStep MUST 与原始快照一致

#### Scenario: 手动场景保留动画差异诊断
- **GIVEN** Sandbox 使用真实 Animancer 播放外观
- **WHEN** replay 后 animation facts 无法收敛
- **THEN** Console MUST 输出 `blackboard.animation.*` 或等价字段级 differences
- **AND** 诊断 MUST 区分 animation fact 差异与 position/yaw/action replay 差异

### Requirement: Debug Runner FullBody 验证
系统 MUST 让 Play Mode debug runner 可用 FullBody replay adapter 执行本地 synctest，并保持安全探针和可见 correction 两种调试语义。

#### Scenario: 默认安全探针
- **GIVEN** debug runner 未启用应用 replay 结果到场景
- **WHEN** 用户触发 F6 或等价 debug synctest
- **THEN** 系统 MUST 临时 restore + replay + compare
- **AND** 执行结束后 MUST 恢复触发前最新现场快照

#### Scenario: 可见 correction 模式
- **GIVEN** 用户显式启用应用 replay 结果到场景
- **AND** 已配置 `PresentationTransformInterpolator`
- **WHEN** replay 后逻辑根 position 或 yaw 与触发前不同
- **THEN** 系统 MUST 将 replay 后逻辑根结果应用到场景
- **AND** 表现根 MUST 从触发前 visual pose 插值追到新的逻辑根 pose

#### Scenario: FullBody differences 可读
- **WHEN** FullBody synctest 失败
- **THEN** Console MUST 输出 reason、restore tick、end tick 和 differences
- **AND** differences SHOULD 能区分 position/yaw、stateTime、action facts、animation facts 和 runtime blackboard facts

### Requirement: Fantasy 前置边界
系统 MUST 将本变更限制为本地 FullBody replay 一致性，不得在本变更中接入真实 Fantasy 网络、修改协议文件或实现高延迟模拟器。该变更完成后，后续 MAY 单独规划本地 latency/reconciliation simulator，再将 transport 替换为 Fantasy。

#### Scenario: 不修改 Fantasy 协议
- **WHEN** 实施 FullBody replay
- **THEN** 系统 MUST NOT 修改 `3cDemo/Tools/NetworkProtocol/**/*.proto`
- **AND** MUST NOT 运行或要求协议导出作为本变更验收

#### Scenario: 不新增真实网络流程
- **WHEN** 实施 FullBody replay
- **THEN** 系统 MUST NOT 新增真实 C2G/G2C 发送接收流程
- **AND** MUST NOT 新增 Fantasy 服务端输入队列

#### Scenario: 后续高延迟模拟依赖本变更
- **WHEN** 规划本地高延迟预测回滚模拟器
- **THEN** 该规划 MUST 以 FullBody replay 在 Move/Run/Dodge 上可诊断收敛为前置条件
