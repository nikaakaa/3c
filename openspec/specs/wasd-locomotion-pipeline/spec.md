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
- **AND** 若绑定了 `BasicLocomotionAnimancerPresenter`，MUST 将该上下文提交给 Presenter

#### Scenario: Presenter 不接管移动
- **WHEN** `BasicLocomotionAnimancerPresenter` 播放 Idle、MoveStart、MoveLoop 或 MoveStop
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

### Requirement: FullBody 框架接入后的 Locomotion 模块边界
系统 MUST 允许现有 WASD/Locomotion 主链在 FullBody Action 框架接入后作为 Locomotion 子图或 adapter 被调度。该模块 MAY 继续负责移动意图、相机相对方向、`Idle / MoveStart / MoveLoop / MoveStop` 局部 phase、基础移动运动命令构建和基础移动动画上下文构建，但最终运动提交和 base layer 动画提交 MUST 服从 FullBody 主调度入口的 owner 选择。

#### Scenario: Locomotion 可被 FullBody 调度
- **WHEN** FullBody 主调度入口请求 Locomotion 本帧结果
- **THEN** Locomotion 模块 MUST 能提供移动意图和世界方向事实
- **AND** MUST 能提供当前基础移动 phase
- **AND** MAY 提供基础移动运动命令和动画上下文供 FullBody 主调度入口选择提交

#### Scenario: Action active 时不提交 Locomotion 输出
- **GIVEN** FullBody 主调度入口选择 active FullBody Action 作为本帧 owner
- **WHEN** Locomotion 模块已经生成基础移动运动命令或动画上下文
- **THEN** 系统 MUST NOT 将该基础移动运动命令提交给 motion executor
- **AND** MUST NOT 将该基础移动动画上下文提交给 base layer presenter

#### Scenario: Locomotion 状态图职责保持
- **WHEN** 没有 active FullBody Action
- **THEN** Locomotion 模块 MUST 继续按现有规则处理 `Idle / MoveStart / MoveLoop / MoveStop`
- **AND** `MoveStop -> MoveStart` 仍 MUST 由 Locomotion 局部状态图处理
- **AND** FullBody Action framework MUST NOT 把 Walk/Run 建模为新的 Locomotion phase

#### Scenario: 不恢复第二主入口
- **WHEN** FullBody Action framework 接入完成
- **THEN** 系统 MUST NOT 同时保留一套独立 WASD 主入口和一套独立 FullBody Action 主入口共同提交平面位移
- **AND** 系统 MUST NOT 让 `PlayerDodgeActionController` 或等价 per-action controller 长期绕过 FullBody 主调度入口提交 base layer 动画或平面位移

### Requirement: TurnBack 运动命令权威
系统 MUST 在 TurnBack 状态期间通过统一运动命令表达动画驱动转身，而不是让普通输入运动、动画外观层和 root motion 采样各自成为位移或旋转权威。TurnBack 的动画运动贡献或烘焙运动贡献 MUST 转换为 movement facts 或等价纯数据，再由现有 motion executor 执行。

#### Scenario: TurnBack 抑制普通输入运动
- **GIVEN** 当前 phase 为 `TurnBack`
- **WHEN** WASD/Locomotion pipeline 构建本帧 `MovementCommand`
- **THEN** command MUST 标记普通输入旋转被抑制
- **AND** command MUST 标记普通输入平面位移被抑制
- **AND** command MUST NOT 同时应用普通输入旋转和 TurnBack yaw

#### Scenario: TurnBack yaw 仍走 motion executor
- **GIVEN** TurnBack motion policy 从动画采样或 baked profile 产出本帧 yaw delta
- **WHEN** 系统构建本帧运动命令
- **THEN** yaw delta MUST 进入 `MovementCommand` 或等价运动事实
- **AND** MUST 由现有 motion executor 应用到角色根
- **AND** 状态机 runner 和动画 presenter MUST NOT 直接写角色根旋转

#### Scenario: TurnBack 平移默认来自烘焙 profile
- **GIVEN** TurnBack motion policy 的 translation source 为 baked motion profile 或等价配置
- **WHEN** TurnBack 播放窗口推进
- **THEN** pipeline MUST 从 baked profile 读取本帧 local planar delta
- **AND** MUST 将该 delta 写入 `MovementCommand` 或等价运动事实
- **AND** 角色普通跑步位移 MUST 在退出 TurnBack 后由 MoveLoop 命令恢复

#### Scenario: TurnBack 没有第二运动出口
- **WHEN** TurnBack 状态执行中
- **THEN** `CharacterController.Move` MUST 仍只通过现有 motion executor 或等价运动端口调用
- **AND** pipeline MUST NOT 新增绕过 `PlayerLocomotionController` 的 TurnBack 控制器
- **AND** pipeline MUST NOT 恢复 TurnInPlace、MovingPivotTurn 或旧的散落式 baked yaw/profile 运行路径

### Requirement: TurnBack 手动验证闭环
系统 MUST 提供能在 Sandbox 中验证 TurnBack 状态权威的手动路径，使用户能确认触发范围、转身窗口、输入抑制、回接普通移动和诊断日志。

#### Scenario: Sandbox RunLoop 前后反向急转
- **GIVEN** 用户在 Sandbox 使用 Generic 可琳
- **AND** Locomotion 与 Animation 诊断日志已启用
- **AND** 角色已经进入 RunLoop
- **WHEN** 用户先按 W 跑动再切换到 S
- **THEN** 日志 MUST 显示进入 `FullBody/Locomotion/TurnBack`
- **AND** TurnBack 期间 MUST 显示普通输入旋转和平面位移被抑制
- **AND** 转完点后 MUST 回到 `MoveLoop` 或 `Idle`

#### Scenario: 非 RunLoop 不触发 TurnBack
- **GIVEN** 用户在 Sandbox 使用 Generic 可琳
- **WHEN** 角色处于 Walk、MoveStart、MoveStop 或 Idle
- **AND** 用户输入反向移动
- **THEN** 系统 MUST NOT 直接进入 `FullBody/Locomotion/TurnBack`

#### Scenario: 横向切换不误触发前后 TurnBack
- **GIVEN** 用户在 Sandbox 使用 Generic 可琳
- **WHEN** 用户在 A/D 横向输入之间切换
- **THEN** 系统 MUST NOT 因前后 TurnBack 规则误触发 `FullBody/Locomotion/TurnBack`

#### Scenario: 诊断搜索关键字
- **WHEN** 用户复制诊断日志给开发者
- **THEN** 用户 MUST 能通过 `locomotion-turnback-state-policy|turnback-root-motion-consumed|animation-motion-executor|locomotion-animation-played` 或等价关键字找到 TurnBack 状态、动画和运动命令链路

