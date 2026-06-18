# dodge-action Specification

## Purpose
定义 Shift Dodge / `Action.Dodge` 的语义、输入消费、动作生命周期、Directional/Backstep 变体、FullBody claim、Run latch、运动输出和 Locomotion 边界。
## Requirements
### Requirement: Shift Dodge 动作语义
系统 MUST 提供最小 Shift Dodge / `Action.Dodge` 动作语义，用于表达玩家按下 Shift 时的动作状态、变体、方向、优先级、持续时间和 FullBody claim。该动作 MUST 属于 Action 域，不得作为基础 Locomotion phase、Walk/Run gait 或 `MoveStop -> MoveStart` 的替代规则。第一版使用 `Action.Dodge` 作为稳定 action state，不得新增独立 `Action.Sprint`。

#### Scenario: 有方向输入执行方向冲刺
- **GIVEN** 本地输入缓冲存在可消费的 Shift Dodge 动作请求
- **AND** 当前移动意图存在有效方向
- **WHEN** 玩法层构建动作请求
- **THEN** 动作变体 MUST 为 `Directional`
- **AND** 世界方向 MUST 使用当前相机相对移动方向
- **AND** 动作开始时角色朝向 MUST 立即转到该冲刺方向
- **AND** 目标 Action state MUST 为 `Action.Dodge`

#### Scenario: 无方向输入执行后闪
- **GIVEN** 本地输入缓冲存在可消费的 Shift Dodge 动作请求
- **AND** 当前移动意图不存在有效方向
- **WHEN** 玩法层构建动作请求
- **THEN** 动作变体 MUST 为 `Backstep`
- **AND** 世界方向 MUST 使用角色 facing 的反方向或等价 facing provider 输出
- **AND** 动作开始时角色朝向 MUST 保持当前 facing
- **AND** 目标 Action state MUST 为 `Action.Dodge`

#### Scenario: 动作不进入基础移动 phase
- **WHEN** Shift Dodge 动作被构建或执行
- **THEN** 基础 Locomotion 状态图 MUST 仍只输出 `Idle / MoveStart / MoveLoop / MoveStop`
- **AND** 该动作 MUST NOT 新增为 `BasicMovementPhase`
- **AND** 该动作 MUST NOT 新增为 Walk/Run gait

### Requirement: Shift 输入消费和仲裁
系统 MUST 通过现有输入缓冲和 Action 打断仲裁地基消费 Shift Dodge 动作请求。输入层只记录请求，是否消费 MUST 由玩法层基于 Action 仲裁结果决定。Shift held MUST NOT 直接决定基础移动 Run 档位。

#### Scenario: Shift pressed 生成动作请求
- **WHEN** 玩家按下 Shift
- **THEN** 输入适配层 MUST 生成该 Dodge 动作对应的本地输入请求
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
- **THEN** 请求 MUST 使用 `ActionRequestType.Dodge` 或等价当前 Dodge 动作请求类型
- **AND** 请求目标 state MUST 为 `Action.Dodge`
- **AND** 请求 MUST 使用显式优先级和来源顺序参与现有确定性仲裁

### Requirement: Action.Dodge 运行时生命周期
系统 MUST 提供最小 `Action.Dodge` 运行时生命周期，使 accepted 动作能进入 active 状态、推进时间并在持续时间结束后退出。`ActionRuntimeStateTracker` MUST 继续只保存事实，不得把自动退出逻辑塞入 tracker。

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

### Requirement: Action.Dodge 配置参数
系统 MUST 通过正式 `CharacterActionDefinitionSO`、Character Action Catalog 或批准的等价数据源提供 `Action.Dodge` 的请求绑定、优先级、抗性、selector、Directional timeline、Backstep timeline 和 body claim policy。Directional 与 Backstep 的正式 runtime motion、animation key、duration ticks、timeline window 和 cue request MUST 来自 selected ActionTimeline 的 seconds authoring 经固定 tick interval 量化后的 clip payload。旧 Directional / Backstep variant 字段 MAY 作为迁移输入或 authoring 诊断存在，但 MUST NOT 作为正式 runtime motion、animation 或 timeline fallback。

#### Scenario: Directional Timeline 提供运行时参数
- **WHEN** 设计者配置 Directional 变体
- **THEN** 正式 Dodge action definition MUST 能定位 Directional timeline
- **AND** Directional timeline MUST 能通过 Motion clip 表达 seconds authoring duration、distance、rotateToDirection 和必要 motion payload
- **AND** Directional timeline MUST 能通过 AnimationKey clip 表达 `Action.Dodge.Directional` 或等价稳定 key
- **AND** runtime definition MUST 能将 Directional timeline 编译为 deterministic duration ticks 和 clip tick 区间
- **AND** 请求 priority 和 resistance MUST 能从 action definition、interrupt policy 或批准的正式请求策略入口追踪

#### Scenario: Backstep Timeline 提供运行时参数
- **WHEN** 设计者配置 Backstep 变体
- **THEN** 正式 Dodge action definition MUST 能定位 Backstep timeline
- **AND** Backstep timeline MUST 能通过 Motion clip 表达 seconds authoring duration、distance、rotateToDirection 和必要 motion payload
- **AND** Backstep timeline MUST 能通过 AnimationKey clip 表达 `Action.Dodge.Backstep` 或等价稳定 key
- **AND** runtime definition MUST 能将 Backstep timeline 编译为 deterministic duration ticks 和 clip tick 区间
- **AND** 请求 priority 和 resistance MUST 能从 action definition、interrupt policy 或批准的正式请求策略入口追踪

#### Scenario: 缺失配置不 fallback
- **GIVEN** 正式 Dodge action definition 缺失 selector、Directional timeline、Backstep timeline、必要 Motion clip 或必要 AnimationKey clip
- **WHEN** 系统尝试构建 Dodge motion 输出
- **THEN** 系统 MUST 报告配置错误或拒绝该动作输出
- **AND** MUST NOT 使用代码内置默认值、状态机旧 `output` 字段、旧 variant 字段、场景临时字段、Behavior Graph 或 Resources 资产继续运行

#### Scenario: 非法配置被校验报告
- **GIVEN** timeline 中存在负 seconds、负距离、非法 seconds 区间、缺失 payload、负优先级或负抗性
- **WHEN** 系统校验动作配置
- **THEN** 校验 MUST 报告对应问题
- **AND** 正式 gameplay 路径 MUST NOT 静默把非法值改成另一套隐藏默认手感

#### Scenario: 状态机不复制动作手感参数
- **WHEN** 设计者检查 `Action.Dodge` 状态节点
- **THEN** 状态机节点 MAY 保存 action state id、variant key、timeline binding key 或 output module binding
- **AND** 状态机节点 MUST NOT 并行保存决定 Directional 或 Backstep motion duration/distance 的第二套正式参数

### Requirement: Action 装配闭环
系统 SHOULD 提供明确的 Action 装配闭环，使设计者能追踪 Shift Dodge 的逻辑配置、FullBody claim 策略和动画表现配置。动作逻辑入口 MAY 引用或内嵌运动参数、打断策略和未来 cooldown/cost 配置；动作动画表现 MUST 通过独立动作动画绑定入口或等价边界解析。系统 MUST NOT 要求设计者只能在多个互不关联的散配置资产之间手工同步 Dodge。

#### Scenario: 动作逻辑入口聚合 Dodge 逻辑配置
- **WHEN** 设计者检查或配置 `Action.Dodge`
- **THEN** 系统 SHOULD 提供一个 Action 定义、Dodge Action Profile 或等价动作逻辑入口
- **AND** 该动作逻辑入口 SHOULD 能定位 Directional/Backstep 的运动参数和打断策略
- **AND** 该动作逻辑入口 MUST NOT 直接持有动作动画 Profile

#### Scenario: 动作动画绑定补齐表现配置
- **WHEN** 角色 Action 配置或 Character Action Catalog 装配 `Action.Dodge`
- **THEN** 系统 SHOULD 通过动作动画绑定集或等价动画配置入口定位 `Action.Dodge` 的动作动画 Profile
- **AND** 缺失动画 Profile 或必要动作动画 key 时 SHOULD 能被校验发现
- **AND** 动作动画 Profile MUST NOT 成为动作进入条件、运动参数或状态树拓扑的权威

#### Scenario: 子配置可以继续分层
- **WHEN** 动作逻辑入口引用 `CharacterActionCatalogSO`、`CharacterActionDefinitionSO`、`ActionInterruptPolicySetSO` 或等价子资产，动作动画绑定入口引用 `ActionAnimationProfileSO` 或等价子资产
- **THEN** 这些子资产 MAY 保持独立文件以支持复用和角色 override
- **AND** 它们 MUST 通过动作逻辑入口、动作动画绑定入口和角色级 Action 装配点组成一个 Dodge 配置闭环
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

### Requirement: Action.Dodge 运动输出
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
- **WHEN** Shift Dodge 动作能力实现完成
- **THEN** 系统 MUST NOT 新增绕过 `CharacterFramePipeline`、`LocomotionRuntimeModule` 或 motion executor 的第二套移动控制路径
- **AND** 系统 MUST NOT 复制 BBB 的完整角色控制器作为动作入口
- **AND** 系统 MUST NOT 保留一套 WASD 状态机和一套 Dodge/Action 状态机同时拥有 base layer 或平面位移权威

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

### Requirement: Action.Dodge 可测试和可验证
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

### Requirement: Dodge 通过 Action Catalog 配置
`Action.Dodge` 的正式动作逻辑配置 MUST 通过 Character Action Catalog 或批准的等价 ActionSet 进入运行时。Dodge 的 action id、request type、source input、priority、resistance、selector、Directional timeline、Backstep timeline、body claim policy 和动作动画 key payload MUST 能从 catalog entry、`CharacterActionDefinitionSO` 或其正式子配置追踪。`CharacterConfigSO.DodgeAction`、旧 Directional / Backstep variant 字段和 Behavior Graph embedded branch MUST NOT 作为正式 gameplay 解析入口或缺失 catalog / timeline 时的 fallback。

#### Scenario: Dodge definition 包含 selector 和两个 timeline
- **WHEN** 设计者检查 `Action.Dodge` definition
- **THEN** definition MUST 包含 Dodge selector
- **AND** definition MUST 包含 Directional timeline
- **AND** definition MUST 包含 Backstep timeline
- **AND** 两个 timeline MUST 都能配置 Animation、Motion、Window 和 Cue clip 中需要的正式 payload
- **AND** 缺失任一必要字段 MUST 被配置校验报告

#### Scenario: Directional Dodge 行为保持
- **GIVEN** Action Catalog 包含有效 `Action.Dodge` definition
- **AND** 输入缓冲中存在 Dodge 输入且当前移动事实支持 directional dodge
- **WHEN** 通用 provider/resolver 路径处理该请求
- **THEN** Dodge resolver MUST 输出 directional dodge resolved action context
- **AND** accepted 后 selector MUST 选择 Directional timeline
- **AND** Directional 的 motion seed、animation key seed、window 和 cue MUST 来自 selected timeline outcome

#### Scenario: Backstep Dodge 行为保持
- **GIVEN** Action Catalog 包含有效 `Action.Dodge` definition
- **AND** 输入缓冲中存在 Dodge 输入且当前移动事实支持 backstep
- **WHEN** 通用 provider/resolver 路径处理该请求
- **THEN** Dodge resolver MUST 输出 backstep dodge resolved action context
- **AND** accepted 后 selector MUST 选择 Backstep timeline
- **AND** Backstep 的 motion seed、animation key seed、window 和 cue MUST 来自 selected timeline outcome

#### Scenario: 缺失 catalog 不使用旧 Dodge 字段
- **GIVEN** `CharacterConfigSO` 缺失 Action Catalog
- **OR** Action Catalog 缺失 `Action.Dodge` definition
- **OR** `Action.Dodge` definition 缺失 selector、Directional timeline 或 Backstep timeline
- **WHEN** 正式 gameplay 路径尝试处理 Dodge 输入
- **THEN** 系统 MUST 报告配置错误或拒绝动作输出
- **AND** MUST NOT 从 `CharacterConfigSO.DodgeAction`、旧 Directional / Backstep variant 字段、Behavior Graph、Resources、全局单例或代码默认值继续运行

### Requirement: Dodge 属于 Action domain
系统 MUST 将 Shift Dodge 视为 Action domain 中的全身动作。Dodge lifecycle MAY 使用 action instance、timeline 或局部 FSM/HFSM；对外 MUST 输出 `Action.Dodge`、action facts、FullBody claim、motion candidate 和 animation candidate。FullBody claim MUST 只表示 Dodge 请求全身占用；claim 被采纳后的结果 MUST 是 Action-side owner 接管 `BaseSlot` 并压制冲突的 `UpperBodySlot`，不得表示 `FullBody` behavior node、`FullBody` runtime source、slot owner 或 Locomotion 父树。

基础 Locomotion MUST 保持为 sibling movement module，不得被迁移到 Action 内部。

#### Scenario: Dodge accepted 后进入 Action domain
- **GIVEN** input intent 提供 Shift Dodge 请求
- **WHEN** Action domain 接受 Dodge
- **THEN** 系统 MUST 创建或推进 `Action.Dodge` 的 lifecycle
- **AND** Dodge 输出 MUST 以 Action submission 参与本帧仲裁

#### Scenario: Locomotion 独立保持
- **WHEN** Dodge 处于 active lifecycle
- **THEN** Locomotion source 仍 MAY 计算基础移动 intent、facts 或移动候选
- **AND** 身体仲裁 MUST 根据 Dodge 的 FullBody claim 决定 `BaseSlot` 是否被 Action 接管

#### Scenario: 模块化不等于分裂运行时
- **WHEN** Dodge 需要 Action lifecycle、位移、动画、窗口或 cue
- **THEN** 这些输出 MUST 通过现有 CharacterFramePipeline / CharacterFramePlan / output applier 链路汇合
- **AND** 系统 MUST NOT 新增第二角色控制入口、第二 motion executor、第二 animation presenter 或第二 blackboard writer

#### Scenario: 当前阶段不引入并行动作表现层
- **WHEN** 需要讨论 FullBody、UpperBody、Facial 或 Additive 输出
- **THEN** 当前 Dodge MUST 只交付基础全身动作接管
- **AND** UpperBody、Facial 或 Additive runtime source MUST 通过单独 change 批准后再实现

#### Scenario: Dodge 不需要 FullBody 节点
- **WHEN** authoring graph、runtime branch 或 compiler 表达 Dodge
- **THEN** Dodge MAY 位于 CommittedAction branch、selector 或 ActionTimeline
- **AND** graph MUST NOT 要求存在名为 `FullBody` 的 gameplay 节点才能编译 `Action.Dodge`

### Requirement: Dodge Timeline 作为运行时权威
`Action.Dodge` 的正式运行时 motion、animation key、duration ticks、timeline window 和 cue request MUST 来自 selected ActionTimeline 或批准的等价 timeline definition。旧 Directional / Backstep variant 字段 MAY 作为迁移输入存在，但 MUST NOT 作为正式 runtime motion 或 animation 权威。Authoring seconds MUST 通过固定量化规则编译为 runtime tick 数据后再参与采样。

#### Scenario: Directional 内容来自 Timeline
- **GIVEN** Dodge selector 选择 Directional timeline
- **WHEN** CommittedActionBranchEvaluator 在 tick N 评估
- **THEN** Directional Dodge 的 motion spec MUST 来自 Directional timeline 的 motion clip
- **AND** animation key MUST 来自 Directional timeline 的 animation clip
- **AND** resolver MUST NOT 从旧 Directional variant 字段补齐 runtime motion 或 animation

#### Scenario: Backstep 内容来自 Timeline
- **GIVEN** Dodge selector 选择 Backstep timeline
- **WHEN** CommittedActionBranchEvaluator 在 tick N 评估
- **THEN** Backstep Dodge 的 motion spec MUST 来自 Backstep timeline 的 motion clip
- **AND** animation key MUST 来自 Backstep timeline 的 animation clip
- **AND** resolver MUST NOT 从旧 Backstep variant 字段补齐 runtime motion 或 animation

#### Scenario: Timeline 采样使用 local tick
- **GIVEN** Dodge action 已在 source step S 被 accepted
- **WHEN** CommittedActionBranchEvaluator 在 source step `S + 5` 评估
- **THEN** selected timeline MUST 使用 local tick 5 采样
- **AND** MUST NOT 使用 Unity deltaTime、Animator normalized time 或 editor preview position 推导采样位置

### Requirement: Dodge Variant Selector
`Action.Dodge` MUST 使用 Action selector / condition 或批准的等价 committed action node 选择 Directional 或 Backstep timeline。选择条件 MUST 只读取纯数据 movement intent、facing、request context 或 runtime snapshot，MUST NOT 读取 Unity input object 或 scene object。

#### Scenario: 有移动意图选择 Directional
- **GIVEN** Dodge request 已被 action request 仲裁接受
- **AND** 当前 movement intent 有有效方向
- **WHEN** Dodge selector 评估
- **THEN** selector MUST 选择 Directional timeline
- **AND** Backstep timeline MUST 不输出 motion、animation、fact 或 cue

#### Scenario: 无移动意图选择 Backstep
- **GIVEN** Dodge request 已被 action request 仲裁接受
- **AND** 当前 movement intent 没有有效方向
- **WHEN** Dodge selector 评估
- **THEN** selector MUST 选择 Backstep timeline
- **AND** Directional timeline MUST 不输出 motion、animation、fact 或 cue

### Requirement: Dodge Timeline 使用 Tick 时间权威
`Action.Dodge` 的 timeline runtime MUST 使用 action-local tick、duration ticks 和 window tick range 作为采样权威。Seconds MUST 作为 authoring、editor 和诊断语言存在，并 MUST 在进入 runtime definition 前通过固定 tick interval 编译为 tick。旧 frame 字段 MAY 只作为迁移输入或诊断存在，MUST NOT 作为正式 runtime fallback。

#### Scenario: Runtime 不读取 Seconds 权威
- **GIVEN** Dodge timeline definition 已被编译或加载到 runtime
- **WHEN** CommittedActionBranchEvaluator 在 tick N 评估
- **THEN** duration、window 和 timeline sampling MUST 基于 compiled tick 字段
- **AND** runtime MUST NOT 读取 seconds 字段作为推进 timeline 的权威来源

#### Scenario: 旧 frame 字段不作为 fallback
- **GIVEN** Dodge asset 仍包含 legacy frame 字段
- **WHEN** seconds authoring 字段缺失或非法
- **THEN** 正式 runtime MUST 报告配置错误或拒绝动作输出
- **AND** MUST NOT 从 legacy frame 字段静默补齐正式 runtime timeline

#### Scenario: 旧 frame 只允许按 legacy rate 迁移
- **GIVEN** Dodge asset 仍包含 legacy Directional 或 Backstep frame 字段
- **WHEN** 迁移器读取这些 frame 字段
- **THEN** 迁移器 MUST 使用显式 legacy authoring frame rate 转换为 seconds，默认 60Hz
- **AND** compiler MUST 再按 simulation tick settings 的 fixed tick interval 编译为 runtime ticks
- **AND** 正式 runtime MUST NOT 将 legacy frame 直接解释为 Dodge local tick

### Requirement: Dodge 无隐藏 Fallback
如果 `Action.Dodge` 的 selector、condition、Directional timeline 或 Backstep timeline 缺失或非法，正式 gameplay MUST 报告配置错误或拒绝动作输出。系统 MUST NOT 使用旧 variant 字段、Resources、代码默认 timeline、场景对象或全局单例补齐缺失配置。

#### Scenario: 缺失 Directional timeline 报错
- **GIVEN** Dodge 配置缺失 Directional timeline
- **WHEN** 有移动意图的 Dodge 请求被处理
- **THEN** 系统 MUST 报告配置错误或拒绝动作输出
- **AND** MUST NOT 使用旧 Directional variant 字段继续运行

#### Scenario: 缺失 Backstep timeline 报错
- **GIVEN** Dodge 配置缺失 Backstep timeline
- **WHEN** 无移动意图的 Dodge 请求被处理
- **THEN** 系统 MUST 报告配置错误或拒绝动作输出
- **AND** MUST NOT 使用旧 Backstep variant 字段继续运行

### Requirement: Dodge 行为回归保持
Dodge timeline 迁移 MUST 保持 Directional、Backstep、Run latch、input consume、interrupt resistance、animation-end 等待、动作结束后再次触发和 rollback restore 的现有行为语义。

#### Scenario: Directional Run latch 保持
- **GIVEN** Directional Dodge 完成帧仍有移动输入
- **WHEN** behavior submission 被最终 frame plan 采用并应用
- **THEN** Run latch 行为 MUST 与迁移前一致
- **AND** 后续移动输入 MUST 继续使用 Run 档位

#### Scenario: Backstep 不写 Run latch
- **GIVEN** Backstep Dodge 完成
- **WHEN** final frame output 被应用
- **THEN** 系统 MUST NOT 因 Backstep 写入 Run latch
- **AND** 行为 MUST 与迁移前一致

#### Scenario: Restore 后 frame 一致
- **GIVEN** rollback restore 到 Dodge timeline 中间帧
- **WHEN** 下一 tick 继续评估
- **THEN** selected timeline frame、motion output 和 animation intent MUST 与 restore state 对应
- **AND** MUST NOT 依赖 evaluator 实例保存状态

### Requirement: Dodge 仍走统一 Behavior Submission
Dodge timeline 迁移后，Dodge MUST 继续通过 Action domain、BehaviorSubmission、CharacterFrameSubmission 或 CharacterFramePlan 进入唯一角色帧管线。Dodge MUST NOT 新增第二角色控制器、第二 runner、第二 motion executor、第二 animation presenter 或直接 Transform 写入路径。

#### Scenario: Dodge 输出进入统一提交
- **WHEN** Dodge timeline 在 tick N 输出 motion 和 animation
- **THEN** 输出 MUST 进入 Action behavior submission 或批准的等价角色帧提交
- **AND** 最终是否采用 MUST 由 CharacterFramePlan 或等价计划决定
- **AND** Dodge timeline MUST NOT 直接应用 motion 或播放 animation

### Requirement: Dodge 使用通用 Branch Authoring Tree
`Action.Dodge` MUST 作为通用 Committed Action branch authoring 的第一个 concrete instance。正式 Dodge action definition MUST 通过通用 branch authoring 表达 selector、Directional condition、Backstep condition、Directional TimelineNode、Backstep TimelineNode 和 FullBody claim。Dodge 专用 `DodgeCommittedActionBranchAuthoring` 只能作为历史资产的一次性迁移读取对象，迁移后 MUST 删除或脱离正式装配；它 MUST NOT 作为正式 authoring 保存目标、runtime 解析入口或 fallback。

#### Scenario: Dodge 节点树表达两个变体
- **WHEN** 设计者检查正式 `Action.Dodge` definition
- **THEN** branch authoring MUST 包含一个 selector root 或批准等价选择入口
- **AND** MUST 包含 Directional condition 到 Directional TimelineNode 的路径
- **AND** MUST 包含 Backstep condition 到 Backstep TimelineNode 的路径
- **AND** 两个 TimelineNode MUST 保存正式 Animation、Motion、Window 和 Cue payload

#### Scenario: Dodge 行为保持
- **GIVEN** Dodge branch authoring 已迁移到通用节点树
- **WHEN** 有移动意图的 Dodge 请求被接受并评估
- **THEN** selector MUST 选择 Directional TimelineNode
- **AND** Directional motion、animation key、window、cue 和 Run latch 行为 MUST 与迁移前等价
- **WHEN** 无移动意图的 Dodge 请求被接受并评估
- **THEN** selector MUST 选择 Backstep TimelineNode
- **AND** Backstep motion、animation key、window、cue 和不写 Run latch 行为 MUST 与迁移前等价

#### Scenario: 不保留 Dodge 专用 Fallback
- **GIVEN** 通用 Dodge branch authoring 缺失或非法
- **WHEN** 正式 gameplay 路径尝试处理 Dodge
- **THEN** 系统 MUST 报告配置错误或拒绝动作输出
- **AND** MUST NOT 从 `DodgeCommittedActionBranchAuthoring`、旧 Directional / Backstep variant、single timeline、Resources 或代码默认值继续运行
