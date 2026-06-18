# wasd-locomotion-pipeline Specification

## Purpose
定义 WASD 输入到基础移动管线的意图解析、运动命令、动画上下文提交和职责分离，确保输入、逻辑、运动执行和表现层解耦。
## Requirements
### Requirement: WASD 主链调度入口
系统 MUST 保留一个当前演示用的 WASD 主链调度入口，并让该入口按固定顺序协调输入、意图、相机相对方向、阶段、运动命令、运动执行、动画表现和相机 Resolve。

#### Scenario: 主链顺序固定
- **WHEN** WASD 主链处理一帧输入
- **THEN** 系统 MUST 先读取输入快照
- **AND** MUST 再生成移动意图
- **AND** MUST 再解析相机相对世界方向
- **AND** MUST 再推进移动阶段
- **AND** MUST 再构建运动命令
- **AND** MUST 再提交给运动驱动
- **AND** MUST 再提交动画表现上下文
- **AND** MUST 最后完成相机 Resolve

#### Scenario: 不新增第二主入口
- **WHEN** 实现 WASD pipeline 重构
- **THEN** 系统 MUST NOT 新增绕过当前 WASD 主链的独立角色控制器
- **AND** MUST NOT 复制 BBB 的完整 `BBBCharacterController` 作为当前角色主入口

### Requirement: 输入快照与移动意图分离
系统 MUST 将本帧输入读取结果与移动意图处理分离，使输入快照只表达 Move、Look 和时间信息，移动意图只表达死区、归一化输入、输入强度和是否存在移动意图。

#### Scenario: 输入快照不依赖场景表现
- **WHEN** 系统读取本帧 Move 和 Look 输入
- **THEN** 输入快照 MUST NOT 依赖 `Transform`
- **AND** MUST NOT 依赖 Animancer
- **AND** MUST NOT 依赖 Cinemachine 具体相机实例

#### Scenario: 移动意图处理死区
- **WHEN** Move 输入幅度低于配置死区
- **THEN** 移动意图 MUST 标记为无移动意图
- **AND** 归一化输入 MUST 为零

#### Scenario: 移动意图限制强度
- **WHEN** Move 输入幅度大于 1
- **THEN** 移动意图强度 MUST 不超过 1
- **AND** 后续运动命令 MUST 使用该强度计算平面速度

### Requirement: 相机相对移动边界
系统 MUST 通过项目侧 `ICameraMovementBasisProvider` 获取相机平面方向，并使用该方向将移动意图转换为世界平面移动方向。

#### Scenario: 前向输入使用相机平面前方
- **WHEN** 玩家只输入前进
- **THEN** 世界移动方向 MUST 等于 `ICameraMovementBasisProvider.CameraPlanarForward` 的平面归一化方向

#### Scenario: 横向输入使用相机平面右方
- **WHEN** 玩家只输入向右
- **THEN** 世界移动方向 MUST 等于 `ICameraMovementBasisProvider.CameraPlanarRight` 的平面归一化方向

#### Scenario: 移动逻辑不直接依赖具体相机
- **WHEN** WASD pipeline 计算世界移动方向
- **THEN** 移动逻辑 MUST NOT 直接读取 `Camera.main`
- **AND** MUST NOT 直接读取 `CinemachineFreeLook`
- **AND** MUST NOT 直接读取场景相机 `Transform`

### Requirement: 运动命令与位移权威
系统 MUST 将世界移动方向、移动意图、阶段和配置转换为 `MovementCommand`，并且 MUST 只通过 `CharacterMotionDriver` 执行基础 WASD 位移。

#### Scenario: 命令提交给运动驱动
- **WHEN** WASD pipeline 构建出 `MovementCommand`
- **THEN** 系统 MUST 将该命令提交给 `CharacterMotionDriver.ExecuteBasicMovement`
- **AND** `BasicWASDMovementController` MUST NOT 直接调用 `CharacterController.Move`

#### Scenario: 位移权威唯一
- **WHEN** 角色执行基础 WASD 位移
- **THEN** `CharacterController.Move` MUST 只在 `CharacterMotionDriver` 内部调用
- **AND** 动画表现层 MUST NOT 写入角色 `transform.position`

#### Scenario: Root Motion 边界
- **WHEN** 实现发现必须由 Root Motion 执行基础 WASD 位移
- **THEN** 实现 MUST 停止
- **AND** MUST 另建或更新 OpenSpec proposal 说明运动权威边界变化

### Requirement: 动画表现只读移动结果
系统 MUST 让基础移动动画表现层只消费移动结果上下文，并且 MUST NOT 让动画表现层拥有移动阶段或位移权威。

#### Scenario: 提交移动表现上下文
- **WHEN** 运动驱动执行完基础移动命令
- **THEN** WASD pipeline MUST 构建 `MovementAnimationContext`
- **AND** 若绑定了正式 `ILocomotionAnimationPresenter`，MUST 将该上下文提交给统一 Presenter

#### Scenario: Presenter 不接管移动
- **WHEN** 正式 Locomotion Presenter 播放 Idle、MoveStart、MoveLoop 或 MoveStop
- **THEN** Presenter MUST NOT 调用 `CharacterController.Move`
- **AND** MUST NOT 写入角色 `transform.position`
- **AND** MUST NOT 成为移动阶段真相源

#### Scenario: 不恢复独立动画表
- **WHEN** 实现本次 WASD pipeline 重构
- **THEN** 系统 MUST NOT 恢复 `BasicLocomotionAnimationConfigSO`
- **AND** MUST NOT 新增等价的运行时基础移动动画表

### Requirement: Cinemachine FreeLook 配置不被 WASD 覆盖
系统 MUST 保持 Cinemachine FreeLook 的手动配置权，WASD pipeline 只能通过项目侧相机入口提交 Look 输入和请求 Resolve，不得在运行时覆盖 FreeLook 配置。

#### Scenario: Look 输入经项目侧相机入口
- **WHEN** 玩家产生 Look 输入
- **THEN** WASD pipeline MAY 将 Look 输入提交给项目侧相机控制入口
- **AND** FreeLook 轴输入 MUST 通过项目相机适配链路消费

#### Scenario: 不覆盖手动配置
- **WHEN** 开发者在 Inspector 中调整 FreeLook 轨道、Follow、LookAt、轴范围或阻尼
- **THEN** WASD pipeline MUST NOT 在初始化或 Tick 中覆盖这些配置

### Requirement: 可验证的最小闭环
系统 MUST 在重构后保持当前第三人称 WASD 可演示闭环，并提供自动验证和手动验证路径。

#### Scenario: 自动验证
- **WHEN** 实施完成
- **THEN** 项目 MUST 能通过现有 C# 编译检查
- **AND** 静态搜索 MUST 能确认基础位移权威仍在 `CharacterMotionDriver`
- **AND** 静态搜索 MUST 能确认移动 pipeline 没有直接依赖 `Camera.main` 或 `CinemachineFreeLook`

#### Scenario: 手动验证
- **WHEN** 开发者进入 Unity Play Mode 并操作 WASD 与 Look 输入
- **THEN** 角色 MUST 按 FreeLook 平面方向移动
- **AND** 角色 MUST 朝移动方向旋转
- **AND** Idle、MoveStart、MoveLoop、MoveStop 表现 MUST 仍能触发
- **AND** FreeLook 手动配置 MUST 不被运行时代码覆盖

### Requirement: Character Frame 接入后的 Locomotion 模块边界
系统 MUST 允许现有 WASD/Locomotion 主链在 Action 框架接入后作为 `CharacterFramePipeline` 下的 Locomotion 决策管线被调度。该模块负责读取或接收移动输入快照、解析移动意图、解析空间事实、派生 Locomotion 决策事实、构建状态机 context、根据状态机输出构建运动候选和动画候选；最终 motion 和 base layer animation 是否执行 MUST 服从 `CharacterFramePlan` 的 slot/claim 仲裁结果。

#### Scenario: Locomotion 可被角色帧管线调度
- **WHEN** Character frame 需要 Locomotion 本帧结果
- **THEN** Locomotion 模块 MUST 能提供移动意图和世界方向事实
- **AND** MUST 能提供 Locomotion 决策事实
- **AND** MUST 能提供当前基础移动 phase
- **AND** MAY 提供基础移动 motion candidate 和 animation candidate 供 `CharacterFramePlan` 仲裁

#### Scenario: Dodge request 使用统一 Locomotion facts
- **WHEN** Action gate 构建 Dodge 输入请求事实
- **THEN** Dodge 按钮请求 MAY 来自 `InputRequestBuffer`
- **AND** directional dodge 的世界方向 MUST 来自本帧 `LocomotionDecisionFacts` 中已解析的世界移动方向
- **AND** backstep dodge 的世界方向 MUST 来自本帧 `LocomotionDecisionFacts` 中已解析的人物平面朝向
- **AND** Action gate MUST NOT 重新从 raw Move 输入、相机 basis 或 facing provider 解析移动方向

#### Scenario: Action active 时不提交 Locomotion 输出
- **GIVEN** `CharacterFramePlan` 选择 CommittedAction 接管 BaseSlot 或压制 Locomotion 输出
- **WHEN** Locomotion 模块已经生成基础移动运动命令或动画上下文
- **THEN** 系统 MUST NOT 将该基础移动运动命令提交给 motion executor
- **AND** MUST NOT 将该基础移动动画上下文提交给 base layer presenter

#### Scenario: Locomotion 状态图职责保持
- **WHEN** 没有 active Action
- **THEN** Locomotion 模块 MUST 继续通过Locomotion 状态图处理 `Idle / MoveStart / MoveLoop / MoveStop / TurnBack`
- **AND** `MoveStop -> MoveStart` 仍 MUST 由 Locomotion 状态图处理
- **AND** Action framework MUST NOT 把 Walk/Run 建模为新的 Locomotion phase

#### Scenario: 不恢复第二主入口
- **WHEN** Action framework 接入完成
- **THEN** 系统 MUST NOT 同时保留一套独立 WASD 主入口和一套独立 Action 主入口共同提交平面位移
- **AND** 系统 MUST NOT 让 `PlayerDodgeActionController` 或等价 per-action controller 长期绕过 `CharacterFramePipeline` / `CharacterFramePlan` 提交 base layer 动画或平面位移

### Requirement: TurnBack 运动命令权威
系统 MUST 在 TurnBack 状态期间通过统一运动命令表达动画驱动转身，而不是让普通输入运动、动画外观层和 root motion 采样各自成为位移或旋转权威。TurnBack 的动画运动贡献或烘焙运动贡献 MUST 转换为 movement facts、movement submission 或等价纯数据，再由 Character frame pipeline 的 output applier 通过现有 motion executor 执行。

#### Scenario: TurnBack 抑制普通输入运动
- **GIVEN** 当前 phase 为 `TurnBack`
- **WHEN** Locomotion builder 构建本帧 `MovementCommand` 或等价 movement submission
- **THEN** command MUST 标记普通输入旋转被抑制
- **AND** command MUST 标记普通输入平面位移被抑制
- **AND** command MUST NOT 同时应用普通输入旋转和 TurnBack yaw

#### Scenario: TurnBack yaw 仍走 motion executor
- **GIVEN** TurnBack motion policy 从动画采样或 baked profile 产出本帧 yaw delta
- **WHEN** 系统构建本帧运动输出
- **THEN** yaw delta MUST 进入 `MovementCommand`、movement facts 或等价 movement submission
- **AND** MUST 由现有 motion executor 应用到角色根
- **AND** 状态机 runner 和动画 presenter MUST NOT 直接写角色根旋转

#### Scenario: TurnBack 平移默认来自烘焙 profile
- **GIVEN** TurnBack motion policy 的 translation source 为 baked motion profile 或等价配置
- **WHEN** TurnBack 播放窗口推进
- **THEN** Locomotion builder MUST 从 baked profile 读取本帧 local planar delta
- **AND** MUST 将该 delta 写入 `MovementCommand`、movement facts 或等价 movement submission
- **AND** 角色普通跑步位移 MUST 在退出 TurnBack 后由 MoveLoop 命令恢复

#### Scenario: TurnBack 没有第二运动出口
- **WHEN** TurnBack 状态执行中
- **THEN** `CharacterController.Move` MUST 仍只通过现有 motion executor 或等价运动端口调用
- **AND** Locomotion builder MUST NOT 新增绕过 `CharacterFramePipeline`、`LocomotionRuntimeModule` 或 Character output applier 的 TurnBack 控制器
- **AND** Locomotion builder MUST NOT 恢复 TurnInPlace、MovingPivotTurn 或旧的散落式 baked yaw/profile 运行路径

### Requirement: TurnBack 手动验证闭环
系统 MUST 提供能在 Sandbox 中验证 TurnBack 状态权威的手动路径，使用户能确认触发范围、转身窗口、输入抑制、回接普通移动和诊断日志。

#### Scenario: Sandbox RunLoop 前后反向急转
- **GIVEN** 用户在 Sandbox 使用 Generic 可琳
- **AND** Locomotion 与 Animation 诊断日志已启用
- **AND** 角色已经进入 RunLoop
- **WHEN** 用户先按 W 跑动再切换到 S
- **THEN** 日志 MUST 显示进入 `Locomotion.TurnBack`
- **AND** TurnBack 期间 MUST 显示普通输入旋转和平面位移被抑制
- **AND** 转完点后 MUST 回到 `MoveLoop` 或 `Idle`

#### Scenario: 非 RunLoop 不触发 TurnBack
- **GIVEN** 用户在 Sandbox 使用 Generic 可琳
- **WHEN** 角色处于 Walk、MoveStart、MoveStop 或 Idle
- **AND** 用户输入反向移动
- **THEN** 系统 MUST NOT 直接进入 `Locomotion.TurnBack`

#### Scenario: 横向切换不误触发前后 TurnBack
- **GIVEN** 用户在 Sandbox 使用 Generic 可琳
- **WHEN** 用户在 A/D 横向输入之间切换
- **THEN** 系统 MUST NOT 因前后 TurnBack 规则误触发 `Locomotion.TurnBack`

#### Scenario: 诊断搜索关键字
- **WHEN** 用户复制诊断日志给开发者
- **THEN** 用户 MUST 能通过 `locomotion-turnback-state-policy|turnback-root-motion-consumed|animation-motion-executor|locomotion-animation-played` 或等价关键字找到 TurnBack 状态、动画和运动命令链路

### Requirement: Locomotion Frame Runtime 模块化
系统 MUST 将 Locomotion prepare/evaluate/build 的运行时实现拆分为明确的 frame runtime modules。`ILocomotionFrameRuntimePort` MUST 保持为 Character frame submitters 访问 Locomotion 子职责的唯一入口；该入口背后的实现 MUST 由 `CharacterRuntimeCore` 组合的 `LocomotionRuntimeModule` 或批准等价模块承载。纯 `LocomotionFrameBuilder` MUST 继续只处理纯数据构建，不得接管 Unity 引用解析、运动执行、动画表现或状态机 runner ownership。

#### Scenario: submitter 只看 frame runtime port
- **WHEN** Character frame submitter 需要 Locomotion decision 或 motion frame
- **THEN** 它 MUST 只调用 `ILocomotionFrameRuntimePort`
- **AND** MUST NOT 引用 `PlayerLocomotionController`
- **AND** MUST NOT 读取 Locomotion controller 的 Unity scene object

#### Scenario: Frame runtime 由 module 承载
- **WHEN** Locomotion frame runtime 执行 prepare/evaluate/build
- **THEN** 具体实现 MUST 位于 `LocomotionFrameRuntime`、adapter 或等价模块中
- **AND** `LocomotionRuntimeModule` MUST 持有正式 Locomotion runtime state
- **AND** 旧 controller 或 facade MUST NOT 继续复制完整 frame runtime 操作面板

#### Scenario: Pure builder 不执行副作用
- **WHEN** `LocomotionFrameBuilder` 构建 decision 或 motion frame
- **THEN** 它 MUST NOT 调用 motion executor
- **AND** MUST NOT 调用 animation presenter
- **AND** MUST NOT 引用 `MonoBehaviour`、`Transform`、`CharacterController`、Animancer runtime type 或 InputAction

#### Scenario: Runtime state restore 保持一致
- **WHEN** Locomotion runtime state 被 capture 后 restore
- **THEN** run latch、last moving gait、current intent、current tick、phase time、previous direction 和 pending TurnBack intent MUST 与迁移前保持等价
- **AND** rollback/replay tests MUST 能证明 restore 后同输入序列结果一致

### Requirement: Locomotion Frame Runtime 职责分层
系统 MUST 将 Locomotion frame runtime 分为 adapter、runtime coordinator、runtime state store、facts providers 和 pure frame builder。每层 Module 的 Interface MUST 只暴露下一层所需的 plain facts 或 result，不得把 Unity host、output side effects 或状态机 runner ownership 泄漏进 pure builder。

#### Scenario: Facts provider 输出 plain facts
- **WHEN** frame runtime provider 解析 input、camera、facing、phase 或 motion profile
- **THEN** provider MUST 输出 plain data facts
- **AND** pure builder MUST NOT 接收 `Transform`、`Camera`、`CharacterController` 或 input runtime object
- **AND** provider MUST NOT 执行动作位移或动画表现

#### Scenario: Runtime coordinator 只编排 frame 构建
- **WHEN** `LocomotionFrameRuntime` 执行本帧 Locomotion 构建
- **THEN** 它 MUST 编排 prepare/evaluate/build
- **AND** MUST NOT 提交最终角色输出
- **AND** MUST NOT 调用 `CharacterFramePipeline`
- **AND** MUST NOT 创建独立 Locomotion tick 主线

#### Scenario: State store 是唯一 Locomotion 局部状态来源
- **WHEN** run latch、last moving gait、previous direction 或 pending TurnBack intent 被读取或写入
- **THEN** 访问 MUST 经过 Locomotion runtime state store 或等价集中 Module
- **AND** 旧 controller 或 Unity-facing adapter MUST NOT 同时保存第二份 authoritative value
- **AND** rollback capture/restore MUST 使用同一状态来源

### Requirement: Locomotion Frame Runtime 不得恢复分裂主线
系统 MUST 保持 Character frame submitters 通过 `ILocomotionFrameRuntimePort` 向 Locomotion 提交数据请求。Locomotion frame runtime MUST NOT 重新成为独立最终输出管线，也不得绕过统一角色帧 pipeline。

#### Scenario: Locomotion 只提交 frame 数据
- **WHEN** Character frame submission 需要 Locomotion 数据
- **THEN** Locomotion frame runtime MUST 返回 decision/motion frame 数据
- **AND** MUST NOT 自己写入最终 `CharacterFrameSubmission`
- **AND** MUST NOT 自己调用 final output applier

#### Scenario: Direct tick 不回到正式主线
- **WHEN** 项目保留 Locomotion direct tick 诊断或测试入口
- **THEN** 该入口 MUST 标记为非正式提交主线
- **AND** MUST NOT 与 unified character frame pipeline 竞争 authoritative output

### Requirement: Locomotion Output Runtime 模块化
系统 MUST 将 Locomotion 输出副作用拆分为明确的 output runtime modules。`ILocomotionOutputRuntimePort` MUST 作为 Character frame output runtime 访问基础移动 motion execution、locomotion animation presentation、runtime facts 写入和 output completion 的唯一入口。输出模块 MUST NOT 选择逻辑状态、创建状态机 runner 或重算 frame decision。

#### Scenario: Motion execution 只经 motion executor
- **WHEN** Locomotion output runtime 执行基础移动位移
- **THEN** 它 MUST 只通过 `IBasicLocomotionMotionExecutor` 或等价 motion executor 端口执行
- **AND** MUST NOT 直接调用 `CharacterController.Move`
- **AND** MUST NOT 直接写角色 `Transform.position`

#### Scenario: Animation presentation 只消费上下文
- **WHEN** Locomotion output runtime 提交基础移动动画
- **THEN** 它 MUST 构建并提交 `MovementAnimationContext` 或等价表现上下文
- **AND** animation presenter MUST NOT 决定当前逻辑状态
- **AND** output runtime MUST NOT 直接读取 Animancer runtime state 作为 transition 权威

#### Scenario: Runtime facts 写入顺序明确
- **WHEN** Locomotion output runtime 写入 action facts、animation facts 或 locomotion facts
- **THEN** facts MUST 使用当前 frame/result 的 source step
- **AND** facts writer MUST NOT 重新推进状态机
- **AND** rollback/replay MUST 能比较迁移前后的 facts 结果

#### Scenario: Complete tick 不成为第二主线
- **WHEN** Locomotion output runtime 完成本帧 output
- **THEN** 它 MAY 处理 camera resolve、rollback camera basis sync 和 run latch reset
- **AND** MUST NOT 读取输入并推进 gameplay
- **AND** MUST NOT 恢复 Locomotion direct tick 为正式路径

### Requirement: Locomotion Output Runtime 阶段必须分离
系统 MUST 将 Locomotion output runtime 分为 motion apply、animation presentation、runtime facts write 和 output completion 阶段。每个阶段 MUST 有独立 Module 或等价清晰职责，且 MUST 能被测试单独观察。

#### Scenario: Motion 和 animation 分离
- **WHEN** Locomotion output runtime 同一帧执行移动和动画
- **THEN** motion apply MUST 通过 motion output module 完成
- **AND** animation presentation MUST 通过 animation output module 完成
- **AND** 两者 MUST NOT 共享会改变逻辑状态的可变决策对象

#### Scenario: Facts writer 不做输出执行
- **WHEN** runtime facts writer 写入 blackboard facts
- **THEN** 它 MUST NOT 执行 movement
- **AND** MUST NOT present animation
- **AND** MUST 使用 upstream frame/result 提供的 source step

#### Scenario: Completion module 不读新输入
- **WHEN** output completion module 同步 camera basis 或 reset run latch
- **THEN** 它 MUST NOT 读取新输入
- **AND** MUST NOT 重新构建 Locomotion frame
- **AND** MUST NOT 触发状态机 transition

### Requirement: Locomotion Output Runtime 不得创建新执行出口
系统 MUST 保持现有 motion executor、animation presenter 和 unified character frame pipeline 为正式执行出口。Locomotion output moduleization MUST NOT 引入 fallback executor、parallel presenter 或直接 scene mutation path。

#### Scenario: 没有 fallback motion executor
- **WHEN** formal motion executor 缺失或未配置
- **THEN** production initialization MUST fail explicitly or block execution
- **AND** MUST NOT silently fall back to direct movement
- **AND** MUST NOT create hidden local executor

#### Scenario: 没有 parallel animation presenter
- **WHEN** locomotion animation output 需要表现
- **THEN** output runtime MUST use the configured presenter Interface
- **AND** MUST NOT create a second presenter path
- **AND** MUST NOT write Animancer state directly as gameplay authority

### Requirement: Locomotion 作为角色级兄弟提交者
Locomotion 在目标架构中 MUST 作为 Character frame owner 下的 sibling submitter 提交移动意图、移动事实、基础移动候选输出和 Locomotion animation 请求。Locomotion 可以被 Action 的角色级仲裁结果压制，但 MUST NOT 被定义为 Action framework 的长期内部子职责。

#### Scenario: Locomotion 提交候选输出
- **WHEN** Locomotion runtime 处理本帧移动输入
- **THEN** Locomotion MUST 能提交移动意图、世界方向、gait、phase、motion candidate 和 animation candidate
- **AND** 这些数据 MUST 进入 Character frame owner 或等价角色级汇集入口
- **AND** Locomotion MUST NOT 直接提交最终 movement 或 animation 副作用

#### Scenario: 被压制时不执行副作用
- **GIVEN** Locomotion 已提交基础移动候选输出
- **AND** CharacterFramePlan 标记该候选输出被 FullBody occupancy claim 压制
- **WHEN** output applier 执行本帧
- **THEN** Locomotion motion candidate MUST NOT 被提交给 motion executor
- **AND** Locomotion animation candidate MUST NOT 被提交给 base layer Presenter
- **AND** Locomotion runtime MUST NOT 通过独立 direct tick 补交同一输出

#### Scenario: Locomotion 不读取 FullBody 私有状态
- **WHEN** Locomotion 判断本帧是否应提交候选输出
- **THEN** 它 MAY 读取角色级 frame context、accepted request facts 或 arbitration result
- **AND** MUST NOT 直接读取旧 FullBody controller、旧 FullBody builder 或 FullBody 私有字段作为压制权威

#### Scenario: Direct tick 只保留非正式用途
- **WHEN** 项目保留 Locomotion direct tick、诊断或测试入口
- **THEN** 该入口 MUST 标记为非正式主线
- **AND** MUST NOT 与 Character frame owner 竞争最终 movement、animation 或 camera output

### Requirement: Locomotion 作为角色级兄弟 Submitter 实装
Locomotion runtime MUST 在 Corin 正式主线中作为 `CharacterFrameRuntimeController` 下的 sibling submitter 接入。Locomotion submitter MUST 提交移动意图、世界方向、gait、phase、基础移动 motion candidate 和 Locomotion animation candidate。Locomotion submitter MUST NOT 作为独立 direct tick 主线提交最终输出。

#### Scenario: Locomotion submitter 产出候选输出
- **WHEN** Corin 正式角色处理基础移动输入
- **THEN** Locomotion submitter MUST 通过 Locomotion runtime port 读取移动事实
- **AND** MUST 提交基础移动 motion candidate
- **AND** MUST 提交 Locomotion animation candidate
- **AND** 最终是否执行 MUST 由 CharacterFramePlan 决定

#### Scenario: Action active 时 Locomotion 不补交输出
- **GIVEN** CharacterFramePlan 标记 Locomotion motion 或 animation 被 FullBody claim 压制
- **WHEN** output applier 执行本帧
- **THEN** Locomotion motion candidate MUST NOT 被提交给 motion executor
- **AND** Locomotion animation candidate MUST NOT 被提交给 presenter
- **AND** 旧 Locomotion direct tick MUST NOT 在管线外补交输出

#### Scenario: Direct tick 保留为非正式诊断
- **WHEN** 项目保留旧 Locomotion direct tick 或等价诊断 API
- **THEN** 这些 API MUST 标记为非正式 gameplay 主线
- **AND** MUST NOT 与 `CharacterFrameRuntimeController` 竞争 movement、animation 或 camera output
- **AND** MUST 可通过静态测试证明 Corin 正式 prefab/scene 不依赖 direct tick

### Requirement: Locomotion Unity-facing Adapter 不拥有主线
Locomotion 的 Unity-facing adapter MAY 提供输入、facing、camera basis、motion executor、animation presenter 或 diagnostics seam，但 MUST NOT 作为正式 Unity `Update` gameplay driver、状态机 owner 或 Character frame owner。正式 Locomotion runtime MUST 由 `CharacterRuntimeCore` 组合的 `LocomotionRuntimeModule` 或批准等价模块持有。

#### Scenario: AutoUpdate 不作为正式主线
- **WHEN** 检查 Corin 正式 prefab/scene
- **THEN** 旧 Locomotion `AutoUpdate` 或等价 direct driver MUST 不作为正式 gameplay driver
- **AND** frame update MUST 从 `CharacterFrameRuntimeController` 进入
- **AND** simulation tick MUST 从角色级 tick adapter 进入

#### Scenario: Locomotion 不创建 runner
- **WHEN** Locomotion submitter 或 Locomotion Unity-facing adapter 参与 Character frame
- **THEN** 它 MUST NOT 创建、重置或推进第二个 `CharacterStateMachineRunner`
- **AND** 状态权威 MUST 来自 Character runtime controller 装配的唯一 runner
- **AND** Locomotion phase view MUST 从 frame data、runtime state store 或Locomotion 状态图输出派生

### Requirement: Locomotion Runtime 迁出 Mono Adapter
Locomotion 的正式运行时状态、frame runtime host、output runtime host、snapshot/restore 和 diagnostics state MUST 由 core-owned Movement/Locomotion runtime module 持有。旧 Locomotion controller/facade 不得作为正式 Locomotion state owner 或正式 tick owner 保留；Unity-facing adapter 只能满足场景依赖注入和诊断 seam。

#### Scenario: State Store 归属 Pure Runtime
- **WHEN** Locomotion module 在角色帧内运行
- **THEN** `LocomotionRuntimeStateStore` MUST 由 `CharacterRuntimeCore` 组合的 Locomotion runtime module 持有
- **AND** 旧 Locomotion controller/facade MUST NOT 成为该 store 的 authoritative owner

#### Scenario: Blackboard 归属 Pure Runtime
- **WHEN** Locomotion facts builder 需要读取 runtime blackboard snapshot
- **THEN** snapshot MUST 来自 core-owned Locomotion runtime module
- **AND** 旧 Locomotion controller/facade MUST NOT 通过自身字段成为 blackboard authoritative owner

#### Scenario: Mono Controller 只桥接 Unity 依赖
- **GIVEN** Locomotion 需要 Transform、camera basis、motion executor 或 animation presenter
- **WHEN** Unity adapter 装配 Locomotion dependencies
- **THEN** adapter MAY 提供 Unity-facing dependency implementation
- **AND** adapter MUST NOT 直接执行正式 frame decision 或 output application

#### Scenario: Direct Tick 仍非正式
- **WHEN** 旧 Locomotion AutoUpdate、`LocomotionTickAdapter` 或兼容 direct tick 入口存在
- **THEN** 它们 MUST NOT 作为正式 gameplay 主线
- **AND** 正式 Move、Run、TurnBack、Dodge 压制关系 MUST 经 `CharacterRuntimeCore` 和 `CharacterFramePipeline` 推进

#### Scenario: Snapshot Restore 不依赖 Mono 生命周期
- **WHEN** rollback/replay 或测试对 Locomotion runtime 执行 capture/restore
- **THEN** capture/restore MUST 作用于 core-owned pure runtime state
- **AND** MUST NOT 依赖启用、禁用或重新创建旧 Locomotion controller/facade 才能恢复一致状态

### Requirement: Locomotion 决策事实
系统 MUST 在Locomotion 状态图 tick 前构建 Locomotion 决策事实。该事实 MUST 由输入意图、空间事实、当前 phase、动画/phase 可退出事实和运行时配置派生，并作为纯数据进入 `CharacterStateMachineContext` 或等价 context。

#### Scenario: 决策事实保持纯数据
- **WHEN** Locomotion 决策事实被创建或传入状态机 context
- **THEN** 它 MUST NOT 引用 `Transform`
- **AND** MUST NOT 引用 `Animator` 或 Animancer runtime state
- **AND** MUST NOT 引用 `InputAction`
- **AND** MUST NOT 引用 `CharacterController`

#### Scenario: 决策事实包含空间事实
- **WHEN** Locomotion 决策事实构建完成
- **THEN** 它 MUST 能提供当前世界移动方向
- **AND** MUST 能提供人物当前平面朝向
- **AND** MUST 能提供是否存在移动意图

#### Scenario: 决策事实包含移动派生意图
- **WHEN** 当前移动输入和人物朝向满足某个 Locomotion 派生行为条件
- **THEN** Locomotion 决策事实 MUST 能承载该派生事实
- **AND** 首个派生事实 MUST 覆盖移动反向 TurnBack intent
- **AND** 该派生事实 MUST NOT 直接切换状态或播放动画

#### Scenario: 状态机消费决策事实
- **WHEN** Locomotion 状态图 tick 执行
- **THEN** transition evaluator MUST 从 context 中读取 Locomotion 决策事实
- **AND** MUST NOT 直接读取相机或人物 Transform 来重新构造这些事实

#### Scenario: TurnBack 动画运动源由 TickSampledMotion 接管
- **WHEN** Locomotion 状态图进入移动 TurnBack phase
- **THEN** 系统 MUST 使用已审批的 `TickSampledMotion` 动画运动源策略或批准的等价 tick 采样策略
- **AND** 采样输入 MUST 来自可恢复的播放进度、previous/current sampling window 和正式 motion profile
- **AND** 输入旋转和输入平面位移 MUST 按 timeline facts / motion policy 被 suppress
- **AND** sampled yaw、translation 或等价 movement facts MUST 由统一 motion executor 应用到角色运动根
- **AND** `OnAnimatorMove()` runtime root delta MAY 只用于诊断或对比日志
- **AND** `OnAnimatorMove()` runtime root delta MUST NOT 通过 source、pending buffer 或 rollback state 成为 simulation tick motion source
- **AND** 系统 MUST NOT 恢复 AnimatorDirect、pending runtime root delta、TurnInPlace、MovingPivotTurn 或旧散落式 baked yaw/profile 路线作为正式 TurnBack 运动权威
