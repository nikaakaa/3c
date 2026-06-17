## MODIFIED Requirements
### Requirement: WASD 主链调度入口
系统 MUST 保留一个当前演示用的 WASD/Locomotion 主链调度入口，并让该入口按固定顺序协调输入、意图、空间事实、Locomotion 决策事实、统一状态机、运动命令、运动执行、动画表现、动画反馈和相机 Resolve。该入口 MUST NOT 为 TurnBack、Run、Stop 或后续移动派生行为新增第二条控制路径。

#### Scenario: 主链顺序固定
- **WHEN** WASD/Locomotion 主链处理一帧输入
- **THEN** 系统 MUST 先读取输入快照
- **AND** MUST 再生成移动意图
- **AND** MUST 再解析相机相对世界方向和人物当前朝向
- **AND** MUST 再派生 Locomotion 决策事实
- **AND** MUST 再构建统一状态机 context
- **AND** MUST 再推进统一状态机
- **AND** MUST 再构建运动命令
- **AND** MUST 再提交给运动驱动或 motion executor
- **AND** MUST 再提交动画表现上下文
- **AND** MUST 最后完成动画反馈事实和相机 Resolve

#### Scenario: 不新增第二主入口
- **WHEN** 实现 WASD/Locomotion pipeline 重构
- **THEN** 系统 MUST NOT 新增绕过当前 WASD/Locomotion 主链的独立角色控制器
- **AND** MUST NOT 复制 BBB 的完整 `BBBCharacterController` 作为当前角色主入口
- **AND** MUST NOT 为 TurnBack 新增绕过统一状态机的专用运行主线

### Requirement: 输入快照与移动意图分离
系统 MUST 将本帧输入读取结果与移动意图处理分离，使输入快照只表达 Move、Look、Run 输入和时间信息，移动意图只表达死区、归一化输入、输入强度、是否存在移动意图和移动档位候选。移动意图 MUST NOT 读取相机、人物 Transform、Animator 或状态机 runtime。

#### Scenario: 输入快照不依赖场景表现
- **WHEN** 系统读取本帧 Move、Look 和 Run 输入
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

#### Scenario: 移动意图不派生 TurnBack
- **WHEN** 系统生成 `MovementInputIntent` 或等价移动意图
- **THEN** 该结构 MUST NOT 自行判断 TurnBack
- **AND** TurnBack MUST 在后续 Locomotion 决策事实阶段由移动意图、世界方向和人物朝向共同派生

## ADDED Requirements
### Requirement: Locomotion 决策事实
系统 MUST 在统一状态机 tick 前构建 Locomotion 决策事实。该事实 MUST 由输入意图、空间事实、当前 phase、动画/phase 可退出事实和运行时配置派生，并作为纯数据进入 `CharacterStateMachineContext` 或等价 context。

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
- **WHEN** 统一状态机 tick 执行
- **THEN** transition evaluator MUST 从 context 中读取 Locomotion 决策事实
- **AND** MUST NOT 直接读取相机或人物 Transform 来重新构造这些事实

#### Scenario: TurnBack root motion 由代码接管
- **WHEN** 统一状态机进入移动 TurnBack phase
- **THEN** 系统 MUST 打开 Animator root motion 评价入口以产出 `deltaPosition` 和 `deltaRotation`
- **AND** `OnAnimatorMove()` MUST 采集 TurnBack root motion delta
- **AND** 输入旋转和输入平面位移 MUST 被 suppress
- **AND** motion executor MUST 是唯一把 TurnBack root motion delta 应用到角色运动根的出口
- **AND** 系统 MUST NOT 使用 baked yaw/profile 或 TurnInPlace/MovingPivot 路线替代该链路

## MODIFIED Requirements
### Requirement: FullBody 框架接入后的 Locomotion 模块边界
系统 MUST 允许现有 WASD/Locomotion 主链在 FullBody Action 框架接入后作为统一角色状态机的 Locomotion 决策管线被调度。该模块负责读取或接收移动输入快照、解析移动意图、解析空间事实、派生 Locomotion 决策事实、构建状态机 context、根据状态机输出构建运动命令和动画上下文；最终运动提交和 base layer 动画提交 MUST 服从 FullBody 主调度入口的 owner 选择。

#### Scenario: Locomotion 可被 FullBody 调度
- **WHEN** FullBody 主调度入口请求 Locomotion 本帧结果
- **THEN** Locomotion 模块 MUST 能提供移动意图和世界方向事实
- **AND** MUST 能提供 Locomotion 决策事实
- **AND** MUST 能提供当前基础移动 phase
- **AND** MAY 提供基础移动运动命令和动画上下文供 FullBody 主调度入口选择提交

#### Scenario: Dodge request 使用统一 Locomotion facts
- **WHEN** FullBody Action gate 构建 Dodge 输入请求事实
- **THEN** Dodge 按钮请求 MAY 来自 `InputRequestBuffer`
- **AND** directional dodge 的世界方向 MUST 来自本帧 `LocomotionDecisionFacts` 中已解析的世界移动方向
- **AND** backstep dodge 的世界方向 MUST 来自本帧 `LocomotionDecisionFacts` 中已解析的人物平面朝向
- **AND** Action gate MUST NOT 重新从 raw Move 输入、相机 basis 或 facing provider 解析移动方向

#### Scenario: Action active 时不提交 Locomotion 输出
- **GIVEN** FullBody 主调度入口选择 active FullBody Action 作为本帧 owner
- **WHEN** Locomotion 模块已经生成基础移动运动命令或动画上下文
- **THEN** 系统 MUST NOT 将该基础移动运动命令提交给 motion executor
- **AND** MUST NOT 将该基础移动动画上下文提交给 base layer presenter

#### Scenario: Locomotion 状态图职责保持
- **WHEN** 没有 active FullBody Action
- **THEN** Locomotion 模块 MUST 继续通过统一状态机处理 `Idle / MoveStart / MoveLoop / MoveStop / TurnBack`
- **AND** `MoveStop -> MoveStart` 仍 MUST 由统一状态机处理
- **AND** FullBody Action framework MUST NOT 把 Walk/Run 建模为新的 Locomotion phase

#### Scenario: 不恢复第二主入口
- **WHEN** FullBody Action framework 接入完成
- **THEN** 系统 MUST NOT 同时保留一套独立 WASD 主入口和一套独立 FullBody Action 主入口共同提交平面位移
- **AND** 系统 MUST NOT 让 `PlayerDodgeActionController` 或等价 per-action controller 长期绕过 FullBody 主调度入口提交 base layer 动画或平面位移
