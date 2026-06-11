## ADDED Requirements

### Requirement: 基础移动烘焙运动 Profile
系统 MUST 提供项目自有的基础移动烘焙运动 Profile 数据，用于保存特定 `BasicMovementPhase + alias key` 对应动画的累计本地位移曲线和偏航曲线，使 `RunEnd` 这类带位移动画可以通过统一运动出口减少滑步。

#### Scenario: RunEnd 绑定烘焙运动数据
- **GIVEN** `MoveStop` phase 使用 alias key `RunEnd`
- **WHEN** 设计者为 `RunEnd` 配置烘焙运动 Profile
- **THEN** Profile MUST 记录对应 phase 为 `MoveStop`
- **AND** MUST 记录对应 alias key 为 `RunEnd`
- **AND** MUST 提供可按 normalized time 采样的累计本地平面位移数据

#### Scenario: Profile 不复制动画播放参数
- **WHEN** 设计者配置基础移动烘焙运动 Profile
- **THEN** Profile MUST NOT 要求重复配置 Animancer fade duration
- **AND** MUST NOT 要求重复配置 Animancer playback speed
- **AND** MUST NOT 替代 Animancer TransitionLibrary 的动画播放表

#### Scenario: BBB 只作为参考来源
- **WHEN** 实现参考 BBB 的 Root Motion 烘焙代码
- **THEN** 新增运行时代码 MUST NOT 引用 `BBBNexus` 命名空间
- **AND** MUST NOT 依赖 `Ref/BBB-Nexus` 下的运行时类型、Prefab 或 ScriptableObject

### Requirement: 烘焙运动 Profile 采样事实
系统 MUST 将烘焙运动 Profile 和动画播放进度窗口采样为纯数据运动 facts，供逻辑/运动层读取，而不是让动画外观层直接移动角色。

#### Scenario: 没有有效 Profile 时不贡献运动
- **GIVEN** 当前 phase 没有匹配的烘焙运动 Profile
- **WHEN** 系统采样当前动画运动 facts
- **THEN** 输出 MUST 标记为没有动画运动贡献
- **AND** 本帧基础移动 MUST 回退到输入驱动运动命令

#### Scenario: RunEnd 采样本帧位移
- **GIVEN** 当前 phase 为 `MoveStop`
- **AND** 当前 alias key 为 `RunEnd`
- **AND** 存在匹配的烘焙运动 Profile
- **WHEN** 播放进度从上一帧 normalized time 推进到当前 normalized time
- **THEN** sampler MUST 输出本帧本地平面位移 delta
- **AND** MUST 输出该 delta 是否有效

#### Scenario: 播放重启时重置采样窗口
- **GIVEN** 当前 phase 或 alias key 发生变化
- **WHEN** 系统开始采样新的动画运动 Profile
- **THEN** sampler MUST 从新的播放窗口开始计算 delta
- **AND** MUST NOT 把上一个 phase 的累计位移差值带入新 phase

#### Scenario: Sampler 保持纯数据边界
- **WHEN** 运行时采样烘焙运动 facts
- **THEN** sampler MUST NOT 读取 Animancer runtime 对象
- **AND** MUST NOT 读取 `AnimationClip`
- **AND** MUST NOT 调用 `CharacterController.Move`
- **AND** MUST NOT 写 Transform

## MODIFIED Requirements

### Requirement: 动画不接管基础位移
系统 MUST 保持基础 WASD 位移权威在 `CharacterMotionDriver` 或当前已审批的基础 Locomotion 运动执行端口。动画外观层 MUST NOT 通过 Root Motion、直接 Transform 写入或直接 `CharacterController.Move` 驱动角色移动；但经过审批的烘焙运动 Profile 可以被采样成纯数据运动 facts，并通过统一运动执行端口参与本帧运动。

#### Scenario: 位移仍走运动驱动
- **WHEN** 玩家按 WASD 移动角色并播放移动动画
- **THEN** 角色位移 MUST 仍由 `CharacterMotionDriver` 或基础 Locomotion 运动执行端口执行
- **AND** 动画外观层 MUST NOT 调用 `CharacterController.Move`
- **AND** 动画外观层 MUST NOT 写入角色 `transform.position`

#### Scenario: 烘焙运动通过统一出口生效
- **GIVEN** 当前基础移动 phase 存在有效烘焙运动 facts
- **WHEN** 系统构建本帧基础移动命令
- **THEN** 烘焙运动 delta MUST 通过 `MovementCommand` 或等价纯数据命令进入运动执行端口
- **AND** MUST NOT 绕过运动执行端口直接移动角色

#### Scenario: 完整 Animator Root Motion 仍需单独审批
- **WHEN** 实现发现必须启用 `Animator.applyRootMotion` 直接驱动基础移动
- **THEN** 实现 MUST 停止
- **AND** 创建或更新 OpenSpec proposal 说明完整 Root Motion 位移权威边界变化

