## ADDED Requirements
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
