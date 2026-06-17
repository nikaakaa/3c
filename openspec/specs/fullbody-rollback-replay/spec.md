# fullbody-rollback-replay Specification

## Purpose
定义 FullBody 回滚重放主线、输入帧回灌、状态 Capture/Restore、runtime facts 收敛和 Fantasy 接入前的边界。
## Requirements
### Requirement: FullBody 回滚重放主入口
系统 MUST 提供 FullBody 回滚重放能力，使本地 synctest 的 replay 能通过当前 Character frame pipeline 主线推进动作、移动、动作事实和动画事实。该能力 MUST 复用现有角色 runtime 入口、PlayerLocomotionController adapter、InputRequestBuffer、Locomotion graph 和 Action lifecycle，不得新增第二套角色控制器、第二套 gameplay tick 入口或默认 mixed 状态图。

#### Scenario: 重放走 Character frame 主线
- **GIVEN** replay adapter 收到 tick N 的 `PredictionInputFrame`
- **WHEN** replay 推进 tick N
- **THEN** 系统 MUST 将输入帧转换为 `BasicLocomotionInputSnapshot`
- **AND** MUST 调用 Character frame pipeline 或等价当前正式 FullBody 主线
- **AND** MUST NOT 只调用 `PlayerLocomotionController.Tick(...)` 作为 FullBody replay 的最终路径
- **AND** MUST NOT 通过默认 graph active `Action.Dodge` 表达 Dodge lifecycle

#### Scenario: 保留 locomotion-only adapter
- **GIVEN** 现有 locomotion-only synctest 测试仍需要窄范围验证
- **WHEN** 用户选择 locomotion-only replay adapter
- **THEN** 系统 MAY 继续只通过 `PlayerLocomotionController` 或 Movement module 重放
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
- **AND** `CharacterActionRequestSubmissionArbiter` MUST 能在同 tick 看到该请求

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
系统 MUST 定义 FullBody action 运行时状态的纯数据 capture/restore 边界，使 replay 从历史 tick 恢复时，Locomotion graph restore state、Action lifecycle restore state、input buffer restore state、pending frame facts 和影响下一 tick 输出的事实能回到快照时刻。restore state MUST NOT 保存 Unity Object、Animancer runtime object、Animator、AnimationClip、InputAction 或场景实例引用。

#### Scenario: 捕获 Locomotion 与 Action 状态
- **WHEN** tick N 的角色模拟快照被创建
- **THEN** 系统 MUST 能捕获当前 Locomotion graph active state、state time、variant 和 pending transition 或等价 facts
- **AND** MUST 能捕获 Action lifecycle active action、variant、state time、source step、completion state 和 pending release 或等价 facts
- **AND** 捕获结果 MUST 是纯数据
- **AND** active Dodge MUST NOT 要求 Locomotion graph active state 为 `Action.Dodge`

#### Scenario: 恢复 active Dodge
- **GIVEN** 系统持有 tick N 的 FullBody restore state
- **AND** restore state 中 Action lifecycle active action 为 `Action.Dodge`
- **WHEN** replay 从 tick N 恢复
- **THEN** Action module MUST 恢复到 active Dodge lifecycle state
- **AND** Locomotion graph MUST 恢复到自己的 Locomotion state
- **AND** 恢复后下一 tick 的 Action facts 与恢复前同一输入序列一致

#### Scenario: 不污染输入历史
- **WHEN** replay 恢复 FullBody 状态
- **THEN** 系统 MUST NOT 将动作消费结果写回 `PredictionInputHistory`
- **AND** 后续 replay MUST 仍从原始输入帧重新推导动作请求

### Requirement: FullBody Runtime Facts 收敛
系统 MUST 在 FullBody replay 中重新写入 action facts、locomotion facts、animation facts 和 runtime blackboard facts，使同一输入序列重放后的最终快照能与原始快照在定义容差内比较。Action facts MUST 从 Action lifecycle 或 action output 派生；Locomotion facts MUST 从 Movement module 派生；若 animation facts 仍受表现层播放进度影响，系统 MUST 输出字段级 differences 以定位缺口。

#### Scenario: Action facts 重放收敛
- **GIVEN** 原始运行中 Dodge 请求被 FullBody 主线接受
- **WHEN** replay 使用同一段输入重放到同一 end tick
- **THEN** replay 后的 action active/state/completed/sourceStep MUST 与原始快照一致或输出明确 differences
- **AND** 比较 MUST 不要求默认 graph active state 为 `Action.Dodge`

#### Scenario: Locomotion facts 重放收敛
- **GIVEN** 原始运行中 Locomotion graph 处于 `Locomotion.MoveLoop`
- **WHEN** replay 使用同一段输入重放到同一 end tick
- **THEN** replay 后的 locomotion phase、state time 和输出候选 facts MUST 与原始快照一致或输出明确 differences
- **AND** 比较 MUST 区分 Locomotion facts 与 Action lifecycle facts

#### Scenario: Animation facts 通过可控事实源测试
- **GIVEN** 自动测试使用 fake animation presenter 或 fake playback progress source
- **WHEN** replay 使用同一段输入重放
- **THEN** replay 后的 animation key、normalized time 和 sourceStep MUST 与原始快照一致

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

### Requirement: FullBody 可回滚状态完整性审计
系统 MUST 审计并覆盖 FullBody replay 中所有会影响下一 tick 输出的纯数据状态。至少包括 FullBody active state、state time、pending transition、input buffer restore state、locomotion runtime state、runtime blackboard、animation facts、profile sampling window、motion root pose 和 camera basis override。

#### Scenario: 捕获影响下一 Tick 的状态
- **WHEN** tick N 的 `CharacterSimulationSnapshot` 被创建
- **THEN** 快照 MUST 包含或可确定性重建 tick N+1 推进所需的 FullBody、Locomotion、action、animation 和 motion facts
- **AND** 快照 MUST NOT 保存 Unity Object、Animator、Animancer state 或场景实例引用

#### Scenario: 恢复后下一 Tick 一致
- **GIVEN** tick N 的快照已恢复
- **AND** tick N+1 的输入帧相同
- **WHEN** replay 推进 tick N+1
- **THEN** action state、locomotion state、runtime blackboard、motion root pose 和 animation facts MUST 与原始运行在容差内一致

#### Scenario: 缺失状态可诊断
- **GIVEN** 某个影响下一 tick 的事实没有进入快照也不能由输入重建
- **WHEN** replay 出现 first mismatch
- **THEN** differences MUST 指向对应字段类别
- **AND** 工具 MUST NOT 只输出笼统的 snapshot mismatch

### Requirement: Profile 驱动动画状态可严格重放
系统 MUST 能用严格 synctest 验证 profile 驱动的 Locomotion 动画状态。TurnBack EntryLocal 或等价 profile 驱动状态 MUST 通过正式 FullBody replay 主线恢复、重放和比较，不得在测试中直接绕过主线采样器。

#### Scenario: TurnBack EntryLocal 重放一致
- **GIVEN** TurnBack 使用 profile 采样产生位移和 yaw
- **AND** replay 从 TurnBack 中间 tick 恢复
- **WHEN** 严格 synctest 重放到 end tick
- **THEN** first mismatch MUST 为空
- **AND** end tick 的 position、yaw、state、runtime blackboard 和 animation facts MUST 一致

#### Scenario: Profile 采样窗口被恢复
- **GIVEN** profile 采样依赖 previous normalized time 和 current normalized time
- **WHEN** replay 从历史 tick 恢复
- **THEN** replay 使用的采样窗口 MUST 与原始运行一致
- **AND** 采样 delta MUST 不因表现层当前播放时间改变

#### Scenario: 测试不绕过正式采样路径
- **WHEN** 自动测试验证 profile 驱动状态
- **THEN** 测试 MUST 通过 `FullBodyRollbackSimulation` 或等价 FullBody replay adapter 推进
- **AND** MUST NOT 直接调用底层 pipeline 或直接写 motion root 来制造通过结果

### Requirement: 动画变体和混合事实确定性
系统 MUST 将影响 rollback 结果的动画变体、转身方向、左右脚起步选择、motion space 和混合权重视为确定性事实。它们 MUST 由 tick 输入和配置确定性推导，或进入纯数据快照进行 capture/restore。

#### Scenario: 变体选择可重建
- **GIVEN** tick N 的状态选择了某个动画 variant
- **WHEN** replay 从 tick N-1 恢复并使用同一输入推进到 tick N
- **THEN** replay MUST 选择同一 variant
- **AND** 如果选择不同，first mismatch differences MUST 标记 variant 或 animation facts

#### Scenario: 混合权重影响采样时进入验收
- **GIVEN** 某个动画混合权重会影响 profile delta、yaw 或动作事实
- **WHEN** 该状态进入 rollback 验收
- **THEN** 该权重 MUST 由确定性数据恢复或重建
- **AND** strict synctest MUST 能检测权重不同导致的 replay 分叉

#### Scenario: 表现层混合不参与确定性验收
- **GIVEN** 某个 Animancer/Animator blend 只影响视觉，不影响 simulation tick 的 position、yaw、action 或 blackboard facts
- **WHEN** strict synctest 运行
- **THEN** 该表现层 blend MAY 不进入 simulation snapshot
- **AND** 它 MUST NOT 反向驱动 rollback core

### Requirement: AnimatorDirect 不作为回滚验收基准
系统 MUST 将 Unity Animator runtime delta 视为非确定性表现/兼容来源，不能作为预测回滚严格验收的唯一基准。需要回滚的动画位移 MUST 通过 tick 驱动 profile、纯数据曲线或等价确定性 motion source 验收。

#### Scenario: 严格测试拒绝 AnimatorDirect 作为唯一来源
- **GIVEN** 某状态只依赖 `OnAnimatorMove` 的 runtime delta 推进
- **WHEN** 该状态被纳入预测回滚严格验收
- **THEN** 测试 MUST 标记该状态缺少确定性 motion source
- **AND** MUST 要求提供 profile/曲线/纯数据采样或明确排除该状态的回滚承诺

#### Scenario: AnimatorDirect 作为表现兼容保留
- **GIVEN** 某动画只能暂时通过 AnimatorDirect 播放
- **WHEN** 它不参与预测回滚验收
- **THEN** 系统 MAY 保留该模式作为正式配置的非回滚能力
- **AND** 文档 MUST 标明它不提供本地预测确定性保证

### Requirement: Replay 复用 FullBody Frame Pipeline
系统 MUST 让 FullBody replay、synctest 和本地高延迟校正复用 live gameplay 使用的 `CharacterFramePipelineHost -> CharacterFramePipeline` 主线。Replay adapter MAY 从 `PredictionInputFrame` 构造 pipeline 输入，但 MUST NOT 通过另一套手工顺序直接拼接 input buffer、controller Tick、状态恢复和动画播放事实。Replay adapter MUST NOT 直接创建 `CharacterFramePipeline`、FullBody submitter、第二 runner、第二 motion executor 或第二 animation presenter。

#### Scenario: PredictionInputFrame 进入 Host 和 Pipeline
- **GIVEN** replay adapter 收到 tick N 的 `PredictionInputFrame`
- **WHEN** replay 推进 tick N
- **THEN** 系统 MUST 将该输入帧作为 `CharacterFramePipelineHost` 的输入
- **AND** host MUST 通过同一个 `CharacterFramePipeline` 推进
- **AND** 离散按钮事实 MUST 在 `CharacterFramePipeline` 的输入缓冲步骤写入 `InputRequestBuffer`
- **AND** Move/Look/Run facts MUST 在 `CharacterFramePipeline` 的输入或 facts 步骤进入 Locomotion decision

#### Scenario: Replay 不绕过 GameplayDecision
- **WHEN** replay 推进 Dodge、TurnBack 或未来 Attack 输入
- **THEN** 请求 MUST 重新经过 `CharacterActionRequestSubmissionArbiter` 和对应领域 runtime
- **AND** replay MUST NOT 直接写入“已进入某动作”的结果
- **AND** replay MUST NOT 直接调用 `BasicLocomotionPipeline` 作为 FullBody 最终路径

#### Scenario: Replay 快照来自 Pipeline 结果
- **WHEN** replay 推进 tick N 后捕获快照
- **THEN** 快照 MUST 来自同一 `CharacterFramePipeline` 写入的 FullBody 状态、runtime blackboard、input buffer restore state 和 motion executor restore state
- **AND** 快照 recorder MUST 不需要额外 enrich 一条独立 FullBody gameplay truth

#### Scenario: Replay 不创建分裂持有者
- **WHEN** FullBody replay 或 synctest 构造推进入口
- **THEN** replay MUST 使用角色正式 `CharacterFramePipelineHost`
- **AND** MUST NOT 为 replay 单独创建第二个 `CharacterFramePipeline`
- **AND** MUST NOT 直接调用 FullBody submitter 具体实现来绕过 host

### Requirement: Pipeline Replay 可诊断
系统 MUST 为 pipeline replay 提供字段级 diagnostics，使 replay mismatch 能区分输入回灌、Action 仲裁、状态图 runtime、运动执行、动画事实和 snapshot capture 的差异。

#### Scenario: Replay mismatch 标记阶段
- **WHEN** FullBody synctest 发现原始运行和 replay 不一致
- **THEN** diagnostics MUST 能标记差异发生在输入、状态、运动、动画事实或 snapshot 字段
- **AND** MUST 输出 restore tick、end tick 和当前 pipeline step 或等价阶段信息

#### Scenario: 同输入序列收敛
- **WHEN** 使用相同 `PredictionInputFrame` 序列、相同配置和相同 tick rate 重放 Move、Run、TurnBack 和 Dodge
- **THEN** replay 后的 FullBody 状态、运动根位置/朝向、输入消费状态和 runtime blackboard facts MUST 在定义容差内收敛

### Requirement: Simulation Snapshot 分组状态
系统 MUST 将 FullBody rollback snapshot 视为 simulation 状态集合，而不是 presentation 或 camera 状态集合。snapshot MUST 能表达 transform authority、state machine restore、runtime blackboard、input buffer restore、camera-relative basis、locomotion runtime、motion executor、animation clock 和 root motion pending state。snapshot MUST NOT 保存 Unity Object、Animancer runtime object、Animator、AnimationClip、InputAction、Cinemachine state 或场景实例引用。

#### Scenario: 保存影响 replay 的 simulation 状态
- **WHEN** tick N 的 FullBody 快照被创建
- **THEN** 快照 MUST 包含影响 tick N+1 输出的纯数据状态
- **AND** MUST 包含用于 WASD replay 解算的 `RollbackCameraBasisState`
- **AND** MUST 包含会影响位移、朝向、动作结束或动画事实的 motion / animation clock / root motion 相关纯数据

#### Scenario: 不保存 local-only 表现状态
- **WHEN** 检查 FullBody rollback snapshot
- **THEN** 快照 MUST NOT 保存真实 Cinemachine、FreeLook、Main Camera、camera target proxy、presentation interpolation sample 或 screen effect 状态
- **AND** 若 debug 工具需要这些状态恢复现场，MUST 通过 Debug Tooling 层独立捕获

#### Scenario: 子状态命名表达 ownership
- **WHEN** 开发者阅读 snapshot 字段或 factory
- **THEN** 字段命名 SHOULD 表达其属于 transform、state machine、blackboard、input buffer、camera basis、locomotion runtime、motion executor 或 animation clock
- **AND** 不应暴露容易被理解为真实相机 rollback 的独立 camera state 字段

### Requirement: Camera Basis 清理路径
系统 MUST 使用 `RollbackCameraBasisState` 作为 replay 中 camera-relative 输入解算的唯一快照事实，并 MUST 逐步删除或内联独立 `CameraYaw` 兼容字段。真实 camera yaw/pitch、FreeLook 轴和 Main Camera transform MUST 保持 local-only。

#### Scenario: Replay 使用 camera basis
- **GIVEN** replay 从 tick N 快照恢复
- **WHEN** tick N+1 输入包含 Move 或 Look
- **THEN** replay MUST 使用快照中的 `RollbackCameraBasisState` 作为 WASD 世界方向解算起点
- **AND** replay MUST NOT 为此恢复真实 Cinemachine 或 FreeLook 轴

#### Scenario: 删除 CameraYaw 兼容语义
- **WHEN** 测试和日志已经改为读取 `CameraBasisState.Yaw`
- **THEN** 系统 SHOULD 删除 `CharacterSimulationSnapshot.CameraYaw` 或将其收敛为 `CameraBasisState.Yaw` 的只读兼容别名
- **AND** SHOULD 删除 `WithCameraYaw()` 并改用 `WithCameraBasis(...)`

#### Scenario: 静态验证无真实相机 rollback
- **WHEN** 运行静态边界验证
- **THEN** 系统 MUST 证明 FullBody rollback capture/restore 不调用真实 camera capture/restore
- **AND** MUST 证明不存在 `ThirdPersonCameraRollbackState` 或等价真实相机 rollback 状态

### Requirement: FullBody Restore 状态去诊断污染
系统 MUST 区分影响 replay 输出的 FullBody gameplay restore state 与只影响日志去重或调试显示的 diagnostic restore state。默认 snapshot comparison MUST 关注 gameplay facts，诊断字段不得导致本地 synctest 误判为 simulation mismatch。

#### Scenario: Gameplay restore 保留下一 tick 所需事实
- **WHEN** FullBody action 状态被 capture
- **THEN** gameplay restore state MUST 包含 owner、action state、state time、variant、pending transition、action direction 和会影响下一 tick 输出的状态机内部事实
- **AND** 捕获结果 MUST 是纯数据

#### Scenario: Diagnostic restore 单独存放
- **WHEN** 控制器需要恢复 last logged path、debug path 或日志去重状态
- **THEN** 这些字段 SHOULD 存放在 diagnostic restore state 或 Debug Tooling 层
- **AND** 它们 MUST NOT 与 gameplay restore state 混成同一不可区分的数据包

#### Scenario: Snapshot 比较不受诊断字段影响
- **WHEN** replay 后 gameplay facts 与 live 快照一致但诊断日志去重字段不同
- **THEN** synctest comparison MUST NOT 因诊断字段差异失败
- **AND** 如需定位诊断字段差异 SHOULD 使用单独 debug probe

### Requirement: FullBody replay 使用权威域比较
FullBody rollback replay MUST 使用预测回滚权威域和比较域判断 replay 结果。FullBody 状态机、Action gameplay facts、Locomotion gameplay facts、motion executor state 和 profile-driven motion facts MUST 属于 strict gameplay；纯视觉动画播放漂移 MUST 可诊断但 MUST NOT 单独导致 FullBody replay 失败。

#### Scenario: FullBody gameplay mismatch 仍失败
- **GIVEN** FullBody replay 后 action state 或 locomotion state 不一致
- **WHEN** 本地 synctest 比较快照
- **THEN** 结果 MUST 包含 strict differences
- **AND** FullBody replay MUST 失败

#### Scenario: 视觉 animation drift 不阻塞 FullBody replay
- **GIVEN** FullBody replay 后只有 Action animation normalized time 或 MoveLoop visual playback time 不一致
- **WHEN** 本地 synctest 比较快照
- **THEN** 结果 MUST 保留 presentation differences
- **AND** FullBody replay MAY 成功

#### Scenario: Profile-driven motion 仍严格
- **GIVEN** FullBody replay 期间处于 TurnBack 或等价 profile-driven motion 状态
- **WHEN** playback window、profile delta、root position 或 yaw 不一致
- **THEN** 结果 MUST 包含 strict differences
- **AND** FullBody replay MUST 失败

### Requirement: FullBody 不写业务类型特判
FullBody replay MUST NOT 因项目暂时偏向 MOBA/MMO 或格斗而写死一套业务类型分支。业务差异 MUST 通过状态 policy、timeline facts、motion source 或 compare scope 声明表达。

#### Scenario: MOBA/MMO 风格状态
- **WHEN** 某技能状态声明逻辑窗口由 simulation tick 掌权，动画只表现
- **THEN** hit/cancel/recovery facts MUST 能 strict 比较
- **AND** animation visual playback drift MUST 能只作为 presentation differences

#### Scenario: 格斗风格状态
- **WHEN** 某攻击状态声明动画播放帧直接驱动 hitbox 或取消窗口
- **THEN** 该播放时钟 MUST 被标记为 strict gameplay
- **AND** replay 差异 MUST 导致 strict failure

#### Scenario: 同一主线支持不同策略
- **WHEN** 两个状态使用不同 compare scope
- **THEN** 它们 MUST 仍通过同一 FullBody replay adapter 推进
- **AND** MUST NOT 为某个业务类型创建第二套 replay 主线

### Requirement: FullBody Replay Adapter 属于 Debug Rig
`FullBodyRollbackSimulation` 或等价 `ILocalRollbackSynctestSimulation` Unity adapter MUST 作为独立 `RollbackDebugRig` prefab 的 simulation adapter 存在。该 adapter MUST 通过显式目标角色引用调用当前 `CharacterFrameRuntimeController`、`CharacterFramePipelineHost` 或等价正式角色帧入口推进 replay。正式角色 prefab MUST NOT 依赖该 adapter 作为 gameplay runtime 组件，也 MUST NOT 因该 adapter 缺失而影响正常 Play Mode 移动、动作或动画输出。

#### Scenario: Adapter 推进正式角色帧主线
- **GIVEN** `RollbackDebugRig` prefab 实例中的 FullBody replay adapter 已显式引用目标角色 runtime
- **WHEN** replay adapter 收到 tick N 的 `PredictionInputFrame`
- **THEN** adapter MUST 构造正式角色帧输入
- **AND** MUST 通过目标角色的 `CharacterFrameRuntimeController`、`CharacterFramePipelineHost` 或等价角色级主入口推进
- **AND** MUST NOT 直接创建第二个 `CharacterFramePipeline`

#### Scenario: 正式角色缺少 Adapter 不影响 gameplay
- **WHEN** 正式 Corin 角色 prefab 或正式场景实例未挂载 FullBody replay adapter
- **THEN** 角色正式 gameplay MUST 仍通过 `CharacterFrameRuntimeController` 推进
- **AND** Move、Run、TurnBack、Dodge 或后续 Action 不得依赖 replay adapter 才能运行

#### Scenario: Adapter 不创建分裂持有者
- **WHEN** FullBody replay adapter 执行 capture、restore 或 advance
- **THEN** adapter MUST 复用目标角色已有 runtime、state machine runner、motion executor 和 animation presenter
- **AND** MUST NOT new 第二套 runtime host、状态机 runner、motion executor 或 animation presenter
- **AND** MUST NOT 通过 fallback 配置补齐缺失目标角色引用

#### Scenario: 测试可临时创建 Adapter
- **WHEN** EditMode 测试需要验证 FullBody replay
- **THEN** 测试 MAY 在 fixture 中临时创建 FullBody replay adapter
- **AND** fixture MUST 显式注入目标角色 runtime 依赖
- **AND** fixture MUST NOT 替代 `RollbackDebugRig` prefab 作为 Play Mode 工具入口
- **AND** 测试 MUST 证明 replay 仍走同一角色帧主线

### Requirement: Dodge Run latch 回滚收敛
系统 MUST 在 FullBody replay 和本地回滚中保持 Directional Dodge 完成后的 Run latch 行为确定。Run latch MUST 作为 Locomotion runtime state capture/restore 的一部分参与比较；Action lifecycle restore 只恢复动作状态，不得用默认 graph active `Action.Dodge` 或 Action facts 代替 Run latch。

#### Scenario: Directional 完成后 Run latch replay 收敛
- **GIVEN** 原始运行中 Directional Dodge 完成帧仍有移动输入
- **AND** 原始运行通过 frame output 写入 Run latch
- **WHEN** replay 从动作前或动作中快照恢复并重放同一输入序列
- **THEN** replay 后 Locomotion runtime Run latch MUST 与原始运行一致
- **AND** 后续保持移动输入时 gait MUST 同样解析为 Run
- **AND** 比较 MUST 不要求默认 graph active state 为 `Action.Dodge`

#### Scenario: 无移动完成或 Backstep 不产生 Run latch
- **GIVEN** 原始运行中 Directional Dodge 完成帧没有移动输入或 Dodge 变体为 Backstep
- **WHEN** replay 重放同一输入序列
- **THEN** replay 后 Locomotion runtime Run latch MUST 保持 false
- **AND** 后续 Locomotion phase/gait MUST 与原始运行一致

#### Scenario: Backstep 无输入重放等待动作动画完成
- **GIVEN** 原始运行中 Backstep Dodge 已达到动作位移 duration
- **AND** 本帧没有移动输入
- **AND** 匹配 `Action.Dodge.Backstep` 动作动画尚未播放完成
- **WHEN** replay 重放到同一 tick
- **THEN** Action lifecycle MUST 仍保持 active `Action.Dodge`
- **AND** replay MUST NOT 提前清除 action animation playback
- **AND** Locomotion runtime Run latch MUST 保持 false

#### Scenario: 停止清 latch 参与快照
- **GIVEN** Run latch 曾因 Directional Dodge 完成而 active
- **AND** 玩家停止移动并完成 RunEnd/Idle 收尾
- **WHEN** 系统 capture tick N 的 FullBody restore state
- **THEN** restore state MUST 记录清除后的 Run latch
- **AND** replay 从 tick N 恢复后的下一次移动 MUST 从 Walk 起步

### Requirement: Action Motion Resolver Result 参与回放一致性
FullBody rollback replay MUST 将 Action motion resolver result 视为 strict gameplay facts 的一部分。预测路径和正式路径 MUST 使用同一 action motion spec 与 resolver 输入，产出一致的 movement command、completed 和 run latch 派生。

#### Scenario: Dodge replay 结果一致
- **GIVEN** 相同输入序列触发 Dodge Directional
- **WHEN** rollback replay 从历史 tick 恢复并重放
- **THEN** replay 的 action motion resolver result MUST 与正式路径一致
- **AND** movement command planar distance、world direction、completed 和 source step MUST 匹配

#### Scenario: Backstep 不写 Run latch 保持一致
- **GIVEN** 相同输入序列触发 Dodge Backstep
- **WHEN** rollback replay 比较正式路径和重放路径
- **THEN** 两条路径 MUST 都不产生 run latch on complete
- **AND** 不得通过忽略 action facts 让测试通过

#### Scenario: Resolver 输入缺失时诊断失败
- **GIVEN** replay 恢复后缺少必要 action motion spec 或 locked direction
- **WHEN** resolver 无法产生 strict gameplay result
- **THEN** replay MUST 报告可读差异
- **AND** MUST NOT 使用默认 direction、默认 distance 或上一帧 result 作为 fallback
