## ADDED Requirements
### Requirement: Shift FullBody 动作语义
系统 MUST 提供最小 Shift FullBody 动作语义，用于表达玩家按下 Shift 时的动作状态、变体、方向、优先级和持续时间。该动作 MUST 属于 Action 域，不得作为基础 Locomotion phase、Walk/Run gait 或 `MoveStop -> MoveStart` 的替代规则。第一版使用 `Action.Dodge` 作为稳定 action state，不得新增独立 `Action.Sprint`。

#### Scenario: 有方向输入执行方向冲刺
- **GIVEN** 本地输入缓冲存在可消费的 Shift FullBody 动作请求
- **AND** 当前移动意图存在有效方向
- **WHEN** 玩法层构建动作请求
- **THEN** 动作变体 MUST 为 `Directional`
- **AND** 世界方向 MUST 使用当前相机相对移动方向
- **AND** 动作开始时角色朝向 MUST 立即转到该冲刺方向
- **AND** 目标 Action state MUST 为 `Action.Dodge`

#### Scenario: 无方向输入执行后闪
- **GIVEN** 本地输入缓冲存在可消费的 Shift FullBody 动作请求
- **AND** 当前移动意图不存在有效方向
- **WHEN** 玩法层构建动作请求
- **THEN** 动作变体 MUST 为 `Backstep`
- **AND** 世界方向 MUST 使用角色 facing 的反方向或等价 facing provider 输出
- **AND** 动作开始时角色朝向 MUST 保持当前 facing
- **AND** 目标 Action state MUST 为 `Action.Dodge`

#### Scenario: 动作不进入基础移动 phase
- **WHEN** Shift FullBody 动作被构建或执行
- **THEN** 基础 Locomotion 状态图 MUST 仍只输出 `Idle / MoveStart / MoveLoop / MoveStop`
- **AND** 该动作 MUST NOT 新增为 `BasicMovementPhase`
- **AND** 该动作 MUST NOT 新增为 Walk/Run gait

### Requirement: FullBody 行为域和层级状态机边界
系统 MUST 将基础移动和 Shift FullBody 动作收束到同一个 FullBody 行为域。基础 Locomotion 局部状态图 MAY 作为模块或子图存在，但 MUST NOT 和 Dodge/FullBody Action 形成两套平级、同时争夺 base layer 动画或角色平面位移的状态路径。本变更 MUST NOT 引入 UpperBody、Facial、IK、Additive 或等价并行表现状态层。

#### Scenario: Dodge 属于 FullBody 主层
- **WHEN** `Action.Dodge` 仲裁被接受
- **THEN** Dodge 执行 MUST 归属于 FullBody 主行为域中的状态或模块
- **AND** 它 MUST 接管本次 base layer 动作动画命令和动作位移输出
- **AND** 它 MUST NOT 作为独立于 FullBody 主层的第二套 WASD/Action 状态机运行

#### Scenario: Locomotion 局部图是 FullBody 子职责
- **WHEN** 没有 FullBody 动作接管 base layer
- **THEN** 基础 Locomotion 局部状态图 MAY 继续决定 `Idle / MoveStart / MoveLoop / MoveStop`
- **AND** 该局部图 MUST 通过 FullBody 行为域或等价统一调度入口输出运动和动画命令
- **AND** 该局部图 MUST NOT 绕过 Action 仲裁自行处理 Shift FullBody 动作

#### Scenario: 当前不实现并行表现层
- **WHEN** 实现 Shift FullBody 动作
- **THEN** 系统 MUST NOT 创建 UpperBody、Facial、IK、Additive 或等价并行表现状态层
- **AND** MUST NOT 使用并行表现层决定 `Action.Dodge` 是否进入、结束或转入 Run latch
- **AND** 后续如需这些层 MUST 另开 OpenSpec

#### Scenario: 模块化不等于分裂路径
- **WHEN** 系统为 Dodge 提供独立类、配置资产、测试夹具或内部 runner
- **THEN** 这些实现单元 MUST 被视为 FullBody 行为域内部模块
- **AND** 它们 MUST 通过统一输入、仲裁、运动和动画端口协作
- **AND** 它们 MUST NOT 形成独立角色控制器、独立 Transform 写入路径或独立 base layer 状态权威

### Requirement: Shift 输入消费和仲裁
系统 MUST 通过现有输入缓冲和 Action 打断仲裁地基消费 Shift FullBody 动作请求。输入层只记录请求，是否消费 MUST 由玩法层基于 Action 仲裁结果决定。Shift held MUST NOT 直接决定基础移动 Run 档位。

#### Scenario: Shift pressed 生成动作请求
- **WHEN** 玩家按下 Shift
- **THEN** 输入适配层 MUST 生成该 FullBody 动作对应的本地输入请求
- **AND** 请求 MUST 记录来源 step
- **AND** 请求 MUST 记录过期 step

#### Scenario: Shift held 不重复触发
- **WHEN** 玩家持续按住 Shift 多帧
- **THEN** 输入适配层 MUST NOT 每帧重复生成新的动作请求
- **AND** 是否触发动作 MUST 由 Action 层消费 pressed 请求决定

#### Scenario: 仲裁接受后消费请求
- **GIVEN** 输入缓冲中存在未过期动作请求
- **AND** `ActionInterruptArbiter` 接受进入 `Action.Dodge`
- **WHEN** 动作进入执行
- **THEN** 对应输入请求 MUST 被标记为已消费
- **AND** `ActionRuntimeStateTracker` MUST 进入 `Action.Dodge`

#### Scenario: 仲裁拒绝时保留请求
- **GIVEN** 输入缓冲中存在未过期动作请求
- **AND** `ActionInterruptArbiter` 拒绝进入 `Action.Dodge`
- **WHEN** 玩法层处理本帧动作
- **THEN** 对应输入请求 MUST NOT 被消费
- **AND** 该请求 MUST 继续保留到过期或后续合法消费

#### Scenario: 使用现有 ActionInterruptRequest
- **WHEN** 系统提交动作仲裁请求
- **THEN** 请求 MUST 使用 `ActionRequestType.Dodge` 或等价当前 FullBody 动作请求类型
- **AND** 请求目标 state MUST 为 `Action.Dodge`
- **AND** 请求 MUST 使用显式优先级和来源顺序参与现有确定性仲裁

### Requirement: FullBody 动作运行时生命周期
系统 MUST 提供最小 FullBody 动作运行时生命周期，使 accepted 动作能进入 active 状态、推进时间并在持续时间结束后退出。`ActionRuntimeStateTracker` MUST 继续只保存事实，不得把自动退出逻辑塞入 tracker。

#### Scenario: 动作进入 active
- **GIVEN** 动作仲裁结果为 accepted
- **WHEN** 玩法层应用该裁决
- **THEN** action runtime MUST 标记当前动作 active
- **AND** `ActionRuntimeStateTracker` current state MUST 为 `Action.Dodge`
- **AND** elapsed time MUST 从 0 开始

#### Scenario: 动作到期退出
- **GIVEN** action runtime 当前 active
- **AND** elapsed time 达到配置 duration
- **WHEN** 系统推进 action runtime
- **THEN** action runtime MUST 退出 active
- **AND** Action runtime state MUST 回到 `Action.None` 或等价空 action state

#### Scenario: Tracker 不负责自动退出
- **GIVEN** `ActionRuntimeStateTracker` 处于 `Action.Dodge`
- **WHEN** tracker tick 任意时长
- **THEN** tracker MUST NOT 因 duration、动画结束或 hidden rule 自动改变 current state
- **AND** 动作退出 MUST 由 action runtime 或等价 action driver 明确执行

#### Scenario: 动作结束后可再次触发
- **GIVEN** 上一次 Directional 或 Backstep 已达到 duration 并退出到 `Action.None`
- **AND** 玩家已经松开 Shift
- **WHEN** 玩家再次按下 Shift
- **THEN** 输入适配层 MUST 生成新的 Dodge 请求
- **AND** 该请求 MUST 能重新参与 Action 仲裁
- **AND** 若仲裁接受，系统 MUST 再次进入 `Action.Dodge`

#### Scenario: 退出不污染 resistance
- **WHEN** action runtime 退出到 `Action.None`
- **THEN** `ActionRuntimeStateTracker` 的 resistance MUST 不因 current step 或退出 tick 被错误抬高
- **AND** 系统 MUST NOT 因 accidental resistance、旧请求残留或错误过期规则造成只能 Shift 一次

### Requirement: FullBody 动作配置参数
系统 MUST 通过配置资产或等价数据源提供 Directional 和 Backstep 的距离、时长、优先级、抗性和旋转策略。代码 MAY 提供保守 fallback，但 gameplay 手感参数 MUST NOT 只能通过修改代码调整。

#### Scenario: 配置提供 Directional 参数
- **WHEN** 设计者配置 Directional 变体
- **THEN** 配置 MUST 能表达 duration、distance、priority、resistance 和 rotateToDirection
- **AND** Directional 第一版默认可使用约 0.35s、4m、priority 30、resistance 20、rotateToDirection true

#### Scenario: 配置提供 Backstep 参数
- **WHEN** 设计者配置 Backstep 变体
- **THEN** 配置 MUST 能表达 duration、distance、priority、resistance 和 rotateToDirection
- **AND** Backstep 第一版默认可使用约 0.30s、2m-2.5m、priority 30、resistance 20、rotateToDirection false

#### Scenario: 非法配置安全处理
- **GIVEN** 配置中存在负时长、负距离、负优先级或负抗性
- **WHEN** 系统读取动作配置
- **THEN** 运行时 MUST 使用非负安全值
- **AND** 配置校验 SHOULD 报告对应问题

### Requirement: FullBody Action 装配闭环
系统 SHOULD 提供明确的 FullBody Action 装配闭环，使设计者能追踪 Shift FullBody 动作的逻辑配置和动画表现配置。动作逻辑入口 MAY 引用或内嵌运动参数、打断策略和未来 cooldown/cost 配置；动作动画表现 MUST 通过独立动作动画绑定入口或等价边界解析。系统 MUST NOT 要求设计者只能在多个互不关联的散配置资产之间手工同步 Dodge。

#### Scenario: 动作逻辑入口聚合 Dodge 逻辑配置
- **WHEN** 设计者检查或配置 `Action.Dodge`
- **THEN** 系统 SHOULD 提供一个 FullBody Action 定义、Dodge Action Profile 或等价动作逻辑入口
- **AND** 该动作逻辑入口 SHOULD 能定位 Directional/Backstep 的运动参数和打断策略
- **AND** 该动作逻辑入口 MUST NOT 直接持有动作动画 Profile

#### Scenario: 动作动画绑定补齐表现配置
- **WHEN** 角色 FullBody 主调度入口装配 `Action.Dodge`
- **THEN** 系统 SHOULD 通过动作动画绑定集或等价动画配置入口定位 `Action.Dodge` 的动作动画 Profile
- **AND** 缺失动画 Profile 或必要动作动画 key 时 SHOULD 能被校验发现
- **AND** 动作动画 Profile MUST NOT 成为动作进入条件、运动参数或状态树拓扑的权威

#### Scenario: 子配置可以继续分层
- **WHEN** 动作逻辑入口引用 `DodgeActionConfigSO`、`ActionInterruptPolicySetSO` 或等价子资产，动作动画绑定入口引用 `ActionAnimationProfileSO` 或等价子资产
- **THEN** 这些子资产 MAY 保持独立文件以支持复用和角色 override
- **AND** 它们 MUST 通过动作逻辑入口、动作动画绑定入口和 FullBody 装配点组成一个 Dodge 配置闭环
- **AND** 它们 MUST NOT 成为互相不知道存在的游离配置

#### Scenario: Locomotion 配置仍然独立
- **WHEN** 设计者配置 Walk/Run 的 Locomotion 状态图、TransitionLibrary 或基础移动动画 alias
- **THEN** 这些配置 MUST 仍属于基础 Locomotion 配置入口
- **AND** Dodge 主入口 MUST NOT 接管 `Idle / MoveStart / MoveLoop / MoveStop` 的 Locomotion 状态图规则

### Requirement: Directional 后进入 Run 档位
系统 MUST 在 `Directional` 变体完成后进入基础移动 Run 档位，并且该 Run 档位 MUST 不依赖 Shift 持续按住。`Backstep` 变体完成后 MUST NOT 强制进入 Run 档位。

#### Scenario: Directional 完成后 Run
- **GIVEN** action runtime 当前 active
- **AND** 当前变体为 `Directional`
- **WHEN** 动作达到配置 duration 并完成
- **THEN** Action state MUST 回到 `Action.None` 或等价空 action state
- **AND** 基础移动 MUST 设置 Run latch 或等价移动事实
- **AND** 后续移动输入 MUST 使用 `BasicMovementGait.Run`

#### Scenario: 不需要按住 Shift
- **GIVEN** `Directional` 已完成并设置 Run latch
- **WHEN** 玩家松开 Shift 但继续输入移动
- **THEN** 基础移动 MUST 继续使用 Run 档位

#### Scenario: Backstep 不进入 Run
- **GIVEN** action runtime 当前 active
- **AND** 当前变体为 `Backstep`
- **WHEN** 动作达到配置 duration 并完成
- **THEN** Action state MUST 回到 `Action.None` 或等价空 action state
- **AND** 基础移动 MUST NOT 因本次 Backstep 强制设置 Run latch

#### Scenario: 回 Idle 后重置
- **GIVEN** Run latch 已设置
- **WHEN** 玩家松开移动输入并且基础移动回到 Idle
- **THEN** Run latch MUST 重置
- **AND** 下次普通移动 MUST 使用 Walk 档位

### Requirement: FullBody 动作运动输出
系统 MUST 让动作位移通过统一运动出口或等价 motion executor 执行。动画表现层、Animancer 回调、完整 Animator Root Motion 或 Transform 写入 MUST NOT 成为动作位移权威。

#### Scenario: 动作输出本帧位移
- **GIVEN** action runtime 当前 active
- **AND** 动作配置包含 duration 和 distance
- **WHEN** 系统推进一帧动作
- **THEN** 系统 MUST 根据 elapsed window 采样本帧平面位移
- **AND** 位移方向 MUST 使用动作请求保存的世界方向
- **AND** 位移 MUST 交给统一运动出口或等价 motion executor

#### Scenario: 动作不由动画直接移动
- **WHEN** 动作动画播放
- **THEN** 动画 Presenter MUST NOT 调用 `CharacterController.Move`
- **AND** MUST NOT 写入角色 `transform.position`
- **AND** MUST NOT 通过 Animancer OnEnd 回调直接提交位移

#### Scenario: Root Motion 需要单独审批
- **WHEN** 实现发现必须让完整 Root Motion 驱动动作位移
- **THEN** 实现 MUST 停止
- **AND** MUST 新建或更新 OpenSpec proposal 说明运动权威边界变化

### Requirement: 与现有 Locomotion 边界
系统 MUST 保持现有基础移动主线和局部状态图职责。动作 active 期间可以覆盖或暂停基础移动输入驱动位移，但不得新增第二角色控制器、第二套 base layer 状态权威或绕过当前 movement pipeline 的平行路径。

#### Scenario: 基础移动路径不分裂
- **WHEN** Shift FullBody 动作能力实现完成
- **THEN** 系统 MUST NOT 新增绕过 `PlayerLocomotionController`、`BasicLocomotionPipeline` 或 motion executor 的第二套移动控制路径
- **AND** 系统 MUST NOT 复制 BBB 的完整角色控制器作为动作入口
- **AND** 系统 MUST NOT 保留一套 WASD 状态机和一套 Dodge/FullBody 状态机同时拥有 base layer 或平面位移权威

#### Scenario: MoveStop 重新输入仍由 Locomotion 处理
- **GIVEN** 当前基础移动阶段为 `MoveStop`
- **WHEN** 玩家重新输入移动方向
- **THEN** `MoveStop -> MoveStart` MUST 继续由 Locomotion 状态图规则处理
- **AND** action runtime MUST NOT 成为该流转的必需依赖

#### Scenario: 动作后基础移动恢复
- **GIVEN** action runtime 已结束
- **WHEN** 玩家继续输入 WASD
- **THEN** 基础移动主线 MUST 能继续生成移动意图、状态图 phase 和运动命令
- **AND** Idle、MoveStart、MoveLoop、MoveStop 表现 MUST 不因该动作能力回退

#### Scenario: 动作 active 期间相机 Look 继续响应
- **GIVEN** action runtime 当前 active
- **WHEN** 玩家输入 Look
- **THEN** 项目侧相机入口 MUST 继续接收 Look 输入或等价相机意图
- **AND** action runtime MUST NOT 直接读取或控制 Cinemachine 具体实例
- **AND** 相机响应 MUST NOT 成为动作位移权威

### Requirement: FullBody 动作可测试和可验证
系统 MUST 提供自动测试和手动验证路径，证明 Shift 输入、方向选择、输入消费、仲裁接入、生命周期、Run latch、动作结束后再次触发、运动输出和 Locomotion 边界保持正确。

#### Scenario: 自动测试覆盖核心规则
- **WHEN** 运行该动作的 EditMode 测试
- **THEN** 测试 MUST 覆盖 Shift pressed、Shift held、有输入方向、无输入方向、Directional 转向、Backstep 保持 facing、accepted 消费、rejected 保留、tracker 进入、duration 退出、动作结束后再次 Shift 可重新触发、退出不污染 tracker resistance、配置安全处理、Directional 后 Run、Backstep 不 Run、回 Idle 后 Run latch 重置和运动输出

#### Scenario: 静态边界验证
- **WHEN** 检查新增源码
- **THEN** 静态搜索 MUST 能确认动作逻辑不引用 `BBBNexus` 命名空间
- **AND** MUST 能确认动作运动逻辑不由动画 Presenter 直接调用 `CharacterController.Move`

#### Scenario: 手动验证两个变体
- **WHEN** 用户在 Play Mode 中先按方向再按 Shift
- **THEN** 角色 MUST 向输入方向冲刺
- **AND** 冲刺结束后继续移动时 MUST 保持 Run 档位且不需要按住 Shift
- **AND** 松开移动直到 Idle 后再次移动 MUST 回到 Walk 档位
- **AND** 动作结束后再次按 Shift MUST 能重新触发冲刺动作
- **WHEN** 用户在 Play Mode 中不按方向只按 Shift
- **THEN** 角色 MUST 执行后闪
- **AND** 后闪结束后 MUST NOT 强制进入 Run 档位
