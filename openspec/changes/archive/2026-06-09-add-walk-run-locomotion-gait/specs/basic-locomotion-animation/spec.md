## MODIFIED Requirements

### Requirement: 移动动画上下文
系统 MUST 提供不依赖 Animancer 和场景对象的移动动画上下文，用于把当前基础移动阶段、Walk/Run 档位、输入强度、世界方向和当前速度传递给动画外观层。

#### Scenario: 上下文承载移动阶段
- **WHEN** 基础移动阶段更新为 `Idle / MoveStart / MoveLoop / MoveStop`
- **THEN** 移动动画上下文 MUST 记录当前阶段
- **AND** 该上下文 MUST 不包含 Animancer 运行时类型

#### Scenario: 上下文承载 Walk/Run 档位
- **WHEN** 普通移动选择 Walk 或按住 Run 输入选择 Run
- **THEN** 移动动画上下文 MUST 记录当前基础移动档位
- **AND** Walk/Run MUST NOT 替代 `BasicMovementPhase`

#### Scenario: 上下文承载移动参数
- **WHEN** 角色执行基础移动命令后
- **THEN** 移动动画上下文 MUST 记录当前输入强度、世界移动方向和当前平面速度

### Requirement: 基础移动动画配置
系统 MUST 使用 ScriptableObject 配置基础移动 Walk/Run 动画 alias、退出策略和 motion profile 绑定，避免在代码中写死 `WalkStart / WalkLoop / WalkEnd / RunStart / RunLoop / RunEnd` 的播放路径。

#### Scenario: Walk/Run 动画可配置
- **WHEN** 设计者配置当前基础移动动画
- **THEN** 配置模块 MUST 暴露 `Idle`
- **AND** MUST 暴露 `WalkStart / WalkLoop / WalkEnd`
- **AND** MUST 暴露 `RunStart / RunLoop / RunEnd`
- **AND** 每个 entry MUST 能配置 alias key 和阶段退出策略

#### Scenario: 动画资源不写死
- **WHEN** 更换角色或更换 Animancer alias
- **THEN** 设计者 MUST 能通过配置资产替换 Walk/Run 基础移动 alias 或退出策略
- **AND** 不需要修改移动逻辑状态机代码

#### Scenario: Phase 加档位解析动画
- **WHEN** 动画外观层收到 `MoveStart + Walk`
- **THEN** 配置 MUST 解析到 `WalkStart` 或等价 alias
- **AND** 收到 `MoveStart + Run` 时 MUST 解析到 `RunStart` 或等价 alias

#### Scenario: 停止阶段使用最后移动档位
- **WHEN** 角色从 Walk 移动进入 `MoveStop`
- **THEN** 配置 MUST 使用 `MoveStop + Walk` 解析 WalkEnd alias 和退出策略
- **AND** 当角色从 Run 移动进入 `MoveStop` 时 MUST 使用 `MoveStop + Run` 解析 RunEnd alias 和退出策略

### Requirement: Animancer 基础移动外观层
系统 MUST 提供一个 Animancer 基础移动外观层，消费移动动画上下文并根据 `phase + gait` 播放 Walk/Run 基础移动动画。外观层 MUST 只负责动画播放和播放进度暴露，不负责状态机切换或位移执行。

#### Scenario: 阶段和档位驱动动画播放
- **WHEN** 移动动画上下文阶段为 `MoveLoop` 且档位为 Walk
- **THEN** Animancer 外观层 MUST 播放配置中的 WalkLoop alias
- **AND** 当档位为 Run 时 MUST 播放配置中的 RunLoop alias
- **AND** 该播放逻辑 MUST 集中在动画外观层内

#### Scenario: 停止阶段播放对应停止动画
- **WHEN** 移动动画上下文阶段为 `MoveStop` 且 last moving gait 为 Walk
- **THEN** Animancer 外观层 MUST 播放 WalkEnd alias
- **AND** 当 last moving gait 为 Run 时 MUST 播放 RunEnd alias
- **AND** 外观层 MUST NOT 等待动画完成后主动切换逻辑状态

#### Scenario: 避免重复重播
- **WHEN** 连续多帧收到相同移动阶段、相同档位和相同 alias key
- **THEN** Animancer 外观层 MUST 避免每帧从头重播同一个阶段动画

#### Scenario: 调试状态可见
- **WHEN** 动画外观层接收移动动画上下文
- **THEN** 系统 MUST 暴露当前阶段、当前档位、当前动画名和当前速度作为只读调试信息

#### Scenario: 外观层不接管逻辑
- **WHEN** Animancer 外观层播放 WalkEnd 或 RunEnd
- **THEN** 外观层 MUST NOT 调用状态机切换 API
- **AND** MUST NOT 调用运动执行端口
- **AND** MUST NOT 写入角色 Transform

### Requirement: WASD 到动画外观层组装
系统 MUST 允许当前基础移动运行时组装入口在执行移动后向动画外观层提交携带 Walk/Run 档位的移动动画上下文，但组装入口 MUST NOT 直接散落 Animancer 播放细节。

#### Scenario: 提交动画上下文
- **WHEN** 基础移动入口完成移动意图、Walk/Run 档位、方向、阶段和移动命令执行
- **THEN** 基础移动入口 MUST 构建移动动画上下文
- **AND** 如果绑定了动画外观层，MUST 将上下文提交给该外观层

#### Scenario: 禁止播放细节泄漏
- **WHEN** 基础移动入口接入 Walk/Run 动画表现
- **THEN** 基础移动入口 MUST NOT 直接调用 `AnimancerComponent.Play`
- **AND** Animancer 具体播放逻辑 MUST 保持在动画外观层

### Requirement: 动画不接管基础位移
系统 MUST 保持基础移动位移权威在运动执行端口，Walk/Run 基础移动动画 MUST NOT 通过完整 Root Motion 或直接 Transform 写入驱动角色移动。

#### Scenario: 位移仍走运动执行端口
- **WHEN** 玩家按 WASD 移动角色并播放 Walk 或 Run 移动动画
- **THEN** 角色位移 MUST 仍由运动执行端口执行
- **AND** 动画外观层 MUST NOT 调用 `CharacterController.Move`
- **AND** 动画外观层 MUST NOT 写入角色 `transform.position`

#### Scenario: 烘焙运动贡献仍走 facts
- **WHEN** WalkEnd 或 RunEnd 需要提供动画烘焙位移贡献
- **THEN** 动画采样结果 MUST 先转换为 movement facts 或等价纯数据
- **AND** 再由统一运动执行端口合成位移

#### Scenario: Root Motion 需要单独审批
- **WHEN** 实现发现必须让完整 Animator Root Motion 驱动基础移动才能达到目标效果
- **THEN** 实现 MUST 停止
- **AND** 创建或更新 OpenSpec proposal 说明运动权威边界变化

### Requirement: 角色基础移动动画表归属
系统 MUST 将当前角色的 Walk/Run 基础移动动画表归属到角色 prefab 上的基础移动动画外观层或其绑定配置，而不是要求每个场景实例重复维护基础移动动画配置。

#### Scenario: 角色 prefab 持有 Walk/Run 动画表
- **WHEN** 设计者配置当前演示角色的基础移动动画
- **THEN** 角色 prefab 上的 `BasicLocomotionAnimancerPresenter` MUST 引用对应的 Walk/Run 基础移动动画配置
- **AND** 配置 MUST 提供 `Idle / WalkStart / WalkLoop / WalkEnd / RunStart / RunLoop / RunEnd`

#### Scenario: 场景不重复维护动画 Clip
- **WHEN** 同一角色 prefab 被放入不同演示场景
- **THEN** 场景实例 MUST NOT 需要分别维护一套 Walk/Run 基础移动动画引用才能播放移动动画
- **AND** 场景仍 MAY 维护输入、相机和移动参数等场景装配引用

### Requirement: WASD 自动发现动画外观层
系统 MUST 允许当前基础移动运行时组装入口在未显式绑定 `locomotionPresenter` 时，从当前角色对象层级内发现现有 `BasicLocomotionAnimancerPresenter` 并提交移动动画上下文。

#### Scenario: 同对象发现 Presenter
- **WHEN** `PlayerLocomotionController` 的 `locomotionPresenter` 未绑定
- **AND** 同一 GameObject 上存在 `BasicLocomotionAnimancerPresenter`
- **THEN** 基础移动入口 MUST 使用该 Presenter 接收 `MovementAnimationContext`

#### Scenario: 子对象发现 Presenter
- **WHEN** `PlayerLocomotionController` 的 `locomotionPresenter` 未绑定
- **AND** 同一 GameObject 上不存在 Presenter
- **AND** 当前角色子层级内存在 `BasicLocomotionAnimancerPresenter`
- **THEN** 基础移动入口 MUST 使用该子层级 Presenter 接收 `MovementAnimationContext`

#### Scenario: 禁止跨角色全局查找
- **WHEN** `PlayerLocomotionController` 自动发现动画外观层
- **THEN** 自动发现 MUST 限制在当前角色对象层级内
- **AND** MUST NOT 使用全场景查找连接其他角色的 Presenter

#### Scenario: 不隐式创建配置路径
- **WHEN** 当前角色对象层级内没有 `BasicLocomotionAnimancerPresenter`
- **THEN** 基础位移 MUST 仍可按现有逻辑运行
- **AND** 基础移动入口 MUST NOT 自动创建 Presenter、AnimancerComponent 或动画配置资产
- **AND** 基础移动入口 MUST NOT 通过 `Resources.Load` 或全局单例隐式加载动画表

### Requirement: 基础移动动画配置不改变位移权威
系统 MUST 在统一 Walk/Run 基础移动动画配置归属后继续保持基础移动位移权威在运动执行端口，动画外观层只负责表现。

#### Scenario: 统一配置后仍走运动执行端口
- **WHEN** 角色通过 prefab 上的 Walk/Run 基础移动动画表播放移动动画
- **THEN** 角色位移 MUST 仍由运动执行端口执行
- **AND** `BasicLocomotionAnimancerPresenter` MUST NOT 调用 `CharacterController.Move`
- **AND** `BasicLocomotionAnimancerPresenter` MUST NOT 写入角色 `transform.position`

#### Scenario: Root Motion 仍需单独审批
- **WHEN** 实现统一配置归属时发现必须让完整 Root Motion 驱动基础移动
- **THEN** 实现 MUST 停止
- **AND** MUST 另建或更新 OpenSpec proposal 说明位移权威边界变化
