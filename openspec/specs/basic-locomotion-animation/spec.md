# basic-locomotion-animation Specification

## Purpose
TBD - created by archiving change add-basic-locomotion-animation. Update Purpose after archive.
## Requirements
### Requirement: 移动动画上下文
系统 MUST 提供不依赖 Animancer 和场景对象的移动动画上下文，用于把当前 WASD 移动阶段、输入强度、世界方向和当前速度传递给动画外观层。

#### Scenario: 上下文承载移动阶段
- **WHEN** WASD 移动阶段更新为 `Idle / MoveStart / MoveLoop / MoveStop`
- **THEN** 移动动画上下文 MUST 记录当前阶段
- **AND** 该上下文 MUST 不包含 Animancer 运行时类型

#### Scenario: 上下文承载移动参数
- **WHEN** 角色执行基础移动命令后
- **THEN** 移动动画上下文 MUST 记录当前输入强度、世界移动方向和当前平面速度

### Requirement: 基础移动动画配置
系统 MUST 使用 ScriptableObject 配置基础移动四阶段动画资源和淡入参数，避免在代码中写死动画资源路径。

#### Scenario: 四阶段动画可配置
- **WHEN** 设计者配置基础移动动画
- **THEN** 配置模块 MUST 暴露 `Idle / MoveStart / MoveLoop / MoveStop` 四个动画引用
- **AND** 每个阶段 MUST 能配置对应淡入时间

#### Scenario: 动画资源不写死
- **WHEN** 更换角色或更换动画 Clip
- **THEN** 设计者 MUST 能通过配置资产替换动画资源
- **AND** 不需要修改移动逻辑代码

### Requirement: Animancer 基础移动外观层
系统 MUST 提供一个 Animancer 基础移动外观层，消费移动动画上下文并播放配置中的四阶段动画。

#### Scenario: 阶段驱动动画播放
- **WHEN** 移动动画上下文阶段为 `MoveLoop`
- **THEN** Animancer 外观层 MUST 播放配置中的循环移动动画
- **AND** 该播放逻辑 MUST 集中在动画外观层内

#### Scenario: 避免重复重播
- **WHEN** 连续多帧收到相同移动阶段
- **THEN** Animancer 外观层 MUST 避免每帧从头重播同一个阶段动画

#### Scenario: 调试状态可见
- **WHEN** 动画外观层接收移动动画上下文
- **THEN** 系统 MUST 暴露当前阶段、当前动画名和当前速度作为只读调试信息

### Requirement: WASD 到动画外观层组装
系统 MUST 允许当前 WASD 运行时组装入口在执行移动后向动画外观层提交移动动画上下文，但 WASD 入口 MUST NOT 直接散落 Animancer 播放细节。

#### Scenario: 提交动画上下文
- **WHEN** WASD 入口完成移动意图、方向、阶段和移动命令执行
- **THEN** WASD 入口 MUST 构建移动动画上下文
- **AND** 如果绑定了动画外观层，MUST 将上下文提交给该外观层

#### Scenario: 禁止播放细节泄漏
- **WHEN** WASD 入口接入动画表现
- **THEN** WASD 入口 MUST NOT 直接调用 `AnimancerComponent.Play`
- **AND** Animancer 具体播放逻辑 MUST 保持在动画外观层

### Requirement: 动画不接管基础位移
系统 MUST 保持基础 WASD 位移权威在 `CharacterMotionDriver`，基础移动动画第一版 MUST NOT 通过 Root Motion 或直接 Transform 写入驱动角色移动。

#### Scenario: 位移仍走运动驱动
- **WHEN** 玩家按 WASD 移动角色并播放移动动画
- **THEN** 角色位移 MUST 仍由 `CharacterMotionDriver` 执行
- **AND** 动画外观层 MUST NOT 调用 `CharacterController.Move`
- **AND** 动画外观层 MUST NOT 写入角色 `transform.position`

#### Scenario: Root Motion 需要单独审批
- **WHEN** 实现发现必须让动画 Root Motion 驱动基础移动才能达到目标效果
- **THEN** 实现 MUST 停止
- **AND** 创建或更新 OpenSpec proposal 说明运动权威边界变化

