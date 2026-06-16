# presentation-transform-interpolation Specification

## Purpose
定义表现层 Transform 插值的快照、插值窗口、视觉平滑和不改变模拟权威的边界，确保渲染表现可平滑但不写回逻辑状态。
## Requirements
### Requirement: 表现 Transform 插值边界
系统 MUST 提供通用表现层 Transform 插值能力，使渲染帧表现对象基于 simulation tick 后的真实 pose 样本输出连续 visual pose，同时不得接管 gameplay 位移权威。

#### Scenario: 渲染帧输出连续 pose
- **WHEN** 真实模拟根只在 simulation tick 后更新 position 或 rotation
- **AND** 渲染帧率高于 simulation tick rate
- **THEN** 表现层 Transform MUST 能在渲染帧输出上一 tick pose 与当前 tick pose 之间的插值 pose
- **AND** 插值 alpha MUST 来自 tick 系统的只读插值读数

#### Scenario: 不写真实模拟根
- **WHEN** 表现层 Transform 输出 visual pose
- **THEN** 它 MUST NOT 修改角色真实模拟根 Transform
- **AND** 它 MUST NOT 调用 `CharacterController.Move`
- **AND** 它 MUST NOT 绕过 `PlayerLocomotionController`、`BasicLocomotionPipeline` 或 `IBasicLocomotionMotionExecutor`

#### Scenario: 首帧和样本缺失安全退化
- **WHEN** 表现层 Transform 缺少上一 tick 样本、当前 tick 样本或 tick driver
- **THEN** 它 MUST 安全退化为当前真实 pose 或当前 visual pose
- **AND** 它 MUST NOT 输出 NaN、Infinity 或无效 Transform

#### Scenario: 超距变化 snap
- **WHEN** 上一 tick pose 与当前 tick pose 的距离超过配置的 snap threshold
- **THEN** 表现层 Transform MUST snap 到当前真实 pose
- **AND** 它 MUST NOT 在 teleport、重生或场景切换时拉出错误拖尾

### Requirement: 角色真实根与表现根分离
系统 MUST 在本地可控角色上分离真实模拟根与可见表现根，使 tick 主线写真实根，Animator、骨骼和渲染器等可见对象通过表现根按渲染帧显示。

#### Scenario: 真实根保留 gameplay 权威
- **WHEN** 检查本地可控角色 prefab 或场景实例
- **THEN** `CharacterController`、locomotion tick adapter、输入适配和运动执行入口 MUST 位于真实模拟根或其明确 gameplay 子模块
- **AND** 基础移动 MUST 继续由 simulation tick 主线提交到 motion executor

#### Scenario: 表现根承载可见对象
- **WHEN** 检查本地可控角色 prefab 或场景实例
- **THEN** Animator、Animancer 外观层、骨骼和 SkinnedMeshRenderer MUST 位于表现根或表现根子树
- **AND** 可见对象 MUST NOT 直接依赖 tick 阶梯化真实根作为最终渲染位置

#### Scenario: 动画绑定保持
- **WHEN** 角色表现根迁移完成
- **THEN** Idle、MoveStart、MoveLoop 和 MoveStop 动画 MUST 仍能正常播放
- **AND** 表现根插值 MUST NOT 破坏 Animator avatar、Animancer facade 或现有动画配置引用

#### Scenario: 相机跟随同一表现结果
- **WHEN** 相机目标代理解析角色锚点
- **THEN** 相机 MUST 跟随表现根或表现层派生锚点
- **AND** 相机目标与角色可见位置 MUST 来自同一条表现层输出

### Requirement: 表现更新顺序
系统 MUST 保证表现层 Transform 在渲染帧中先于相机目标代理和 Cinemachine 采样完成更新，使相机读取到同一帧的表现 pose。

#### Scenario: 表现先于相机代理
- **WHEN** 一个渲染帧执行表现层和相机更新
- **THEN** 表现层 Transform MUST 先写入 visual pose
- **AND** `ThirdPersonCameraController` MUST 之后读取该 visual pose 来更新 `CameraFollowTarget` / `CameraAimTarget`

#### Scenario: 相机代理先于 CinemachineBrain
- **WHEN** CinemachineBrain 在当前渲染帧采样 live vcam
- **THEN** `CameraFollowTarget` / `CameraAimTarget` MUST 已经反映当前帧 visual pose
- **AND** Cinemachine MUST NOT 采样上一渲染帧的表现结果

#### Scenario: 一帧多 tick 后使用最新样本
- **WHEN** 单个渲染帧内产生多个 simulation tick
- **THEN** 表现层 Transform MUST 使用最新两个有效 tick pose 样本
- **AND** 插值 alpha MUST 基于追帧后保留的 tick 余量计算

### Requirement: 可测试的表现插值
系统 MUST 用自动测试和静态验证证明表现插值的纯逻辑、运行时边界、角色 prefab 结构和相机接入行为。

#### Scenario: 纯逻辑测试
- **WHEN** 运行表现插值 resolver 的 EditMode 测试
- **THEN** 测试 MUST 覆盖 position 插值、rotation 插值、alpha clamp、首帧 snap、样本缺失和超距 snap

#### Scenario: 运行时组件测试
- **WHEN** 运行表现层 Transform 运行时组件的 EditMode 测试
- **THEN** 测试 MUST 证明组件只写 visual target
- **AND** 测试 MUST 证明组件不会写真实模拟根或调用运动执行入口

#### Scenario: Prefab 结构测试
- **WHEN** 运行角色 prefab 结构测试
- **THEN** 测试 MUST 证明本地可控角色真实根与表现根已分离
- **AND** 测试 MUST 证明相机跟随来源接入表现层输出

#### Scenario: 手动高刷新率验证
- **WHEN** 用户在 `Sandbox` 中以高刷新率或解除 VSync 持续 WASD 直线移动
- **THEN** 角色可见模型和相机跟随 MUST 不再表现为 60Hz 阶梯抖动
- **AND** 将表现层插值临时关闭后 SHOULD 能复现 tick 阶梯抖动，作为对照验证

### Requirement: 表现 Debug Restore 本地化
系统 MAY 为 F6/F8 Debug Tooling 捕获表现层恢复状态，但该状态 MUST 仅用于恢复本地画面现场。表现 debug restore state MUST NOT 被视为 gameplay rollback snapshot，也 MUST NOT 被网络同步、prediction snapshot 或 rollback core 持有。

#### Scenario: Debug restore 不进入 simulation snapshot
- **WHEN** 检查 `CharacterSimulationSnapshot` 或等价 simulation snapshot
- **THEN** 它 MUST NOT 包含 presentation interpolation sample、visual pose correction state 或表现层 restore state
- **AND** presentation restore 数据 MUST 只通过 Debug Tooling 层临时持有

#### Scenario: Hidden replay 后恢复表现现场
- **GIVEN** F6/F8 默认 hidden 模式触发前已有 visual pose 和 interpolation state
- **WHEN** hidden replay 结束
- **THEN** Debug Tooling MUST 恢复触发前表现状态或安全 reset 到触发前 visual pose
- **AND** 表现层 MUST NOT 将 replay 中间态保留为下一渲染帧的长期状态

#### Scenario: 命名避免误导 gameplay rollback
- **WHEN** 表现恢复状态类型或方法被命名
- **THEN** 命名 SHOULD 表达 debug restore 或 local presentation restore 语义
- **AND** SHOULD 避免让调用方误以为该状态属于预测 gameplay rollback snapshot

