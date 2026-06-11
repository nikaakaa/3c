## ADDED Requirements

### Requirement: Locomotion 烘焙运动 Facts 组装
系统 MUST 允许基础 Locomotion 主链在推进阶段后、构建运动命令前读取烘焙运动 facts，使 `MoveStop / RunEnd` 的动画位移可以进入统一运动出口，同时状态机和 pipeline MUST 保持不依赖具体动画运行时对象。

#### Scenario: MoveStop 读取 RunEnd 运动 facts
- **GIVEN** 当前阶段为 `MoveStop`
- **AND** 当前动画 alias key 为 `RunEnd`
- **AND** 当前 RunEnd 存在有效烘焙运动 Profile
- **WHEN** `PlayerLocomotionController` 或等价组装层处理本帧输入
- **THEN** 系统 MUST 在构建 `MovementCommand` 前采样 RunEnd 的烘焙运动 facts
- **AND** MUST 将 facts 提交给 pipeline 或 command builder

#### Scenario: 状态机不读取动画资产
- **WHEN** 基础 Locomotion 阶段机推进 `Idle / MoveStart / MoveLoop / MoveStop`
- **THEN** 阶段机 MUST NOT 引用 `AnimationClip`
- **AND** MUST NOT 引用 `AnimationCurve`
- **AND** MUST NOT 引用 Animancer runtime 类型
- **AND** MUST NOT 解析烘焙运动 Profile 资产

#### Scenario: 中途输入打断停止位移
- **GIVEN** 当前阶段为 `MoveStop`
- **AND** RunEnd 烘焙运动 facts 正在贡献位移
- **WHEN** 本帧重新存在移动输入
- **THEN** 阶段机 MUST 优先切换到 `MoveStart` 或等价起步阶段
- **AND** 系统 MUST 停止继续消费旧 `RunEnd` 的剩余烘焙位移

### Requirement: 运动命令消费烘焙运动贡献
系统 MUST 通过基础 Locomotion 运动命令或等价纯数据命令表达动画烘焙运动贡献，并由当前运动执行端口统一执行输入驱动运动、烘焙动画运动和重力。

#### Scenario: 无输入急停仍按 Profile 移动
- **GIVEN** 当前阶段为 `MoveStop`
- **AND** 本帧没有移动输入
- **AND** 烘焙运动 facts 提供本帧本地平面位移
- **WHEN** 运动执行端口执行本帧命令
- **THEN** 角色 MUST 按该烘焙平面位移移动
- **AND** 重力处理 MUST 仍由运动执行端口统一处理

#### Scenario: 有输入时回到输入驱动
- **GIVEN** 当前阶段从 `MoveStop` 切换到 `MoveStart`
- **WHEN** 系统构建本帧运动命令
- **THEN** 本帧命令 MUST 使用当前阶段对应的运动来源
- **AND** MUST NOT 继续使用上一段 `RunEnd` 的剩余烘焙位移

#### Scenario: CharacterController Move 仍只在执行端口
- **WHEN** 实现接入烘焙运动贡献
- **THEN** `CharacterController.Move` MUST 只出现在当前 CharacterController executor 或 adapter 内
- **AND** `PlayerLocomotionController` MUST NOT 直接调用 `CharacterController.Move`
- **AND** `BasicLocomotionPipeline` MUST NOT 直接调用 `CharacterController.Move`

### Requirement: 烘焙运动接入可测试和可诊断
系统 MUST 为烘焙运动 Profile 接入提供自动测试、静态边界验证和清晰手动验证路径。

#### Scenario: 自动测试覆盖采样和命令
- **WHEN** 实施完成
- **THEN** EditMode 测试 MUST 覆盖 Profile 采样输出本帧 delta
- **AND** MUST 覆盖无 Profile 时没有动画运动贡献
- **AND** MUST 覆盖 `MovementCommand` 或等价命令能携带烘焙运动贡献
- **AND** MUST 覆盖 fake motion executor 能接收到该贡献

#### Scenario: 静态边界验证
- **WHEN** 实施完成
- **THEN** 静态搜索 MUST 能确认基础 Locomotion 状态机不引用 Animancer
- **AND** MUST 能确认基础 Locomotion 状态机不引用 `AnimationClip`
- **AND** MUST 能确认动画外观层不调用 `CharacterController.Move`
- **AND** MUST 能确认新增运行时代码不引用 `BBBNexus`

#### Scenario: 手动验证急停滑步减少
- **GIVEN** `MoveStop / RunEnd` 已绑定烘焙运动 Profile
- **WHEN** 玩家进入 `MoveLoop` 后松开移动输入
- **THEN** 角色 MUST 播放 `RunEnd`
- **AND** 胶囊位移 MUST 跟随 `RunEnd` 的烘焙刹车位移
- **AND** `RunEnd` 播放完成后 MUST 回到 `Idle`

