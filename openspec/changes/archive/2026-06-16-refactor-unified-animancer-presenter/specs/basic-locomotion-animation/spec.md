## MODIFIED Requirements
### Requirement: Animancer 基础移动外观层
系统 MUST 通过统一 FullBody Animancer 表现入口消费移动动画上下文，并根据 `phase + gait` 解析和播放 Walk/Run 基础移动动画。该表现入口 MUST 只负责动画播放和播放进度暴露，不负责状态机切换或位移执行。

#### Scenario: 阶段和档位驱动动画播放
- **WHEN** 移动动画上下文阶段为 `MoveLoop` 且档位为 Walk
- **THEN** 统一 Animancer 表现入口 MUST 播放配置中的 WalkLoop alias
- **AND** 当档位为 Run 时 MUST 播放配置中的 RunLoop alias
- **AND** 该播放逻辑 MUST 集中在统一表现入口或其纯数据请求构建模块内

#### Scenario: 停止阶段播放对应停止动画
- **WHEN** 移动动画上下文阶段为 `MoveStop` 且 last moving gait 为 Walk
- **THEN** 统一 Animancer 表现入口 MUST 播放 WalkEnd alias
- **AND** 当 last moving gait 为 Run 时 MUST 播放 RunEnd alias
- **AND** 表现入口 MUST NOT 等待动画完成后主动切换逻辑状态

#### Scenario: 避免重复重播
- **WHEN** 连续多帧收到相同移动阶段、相同档位和相同 alias key
- **THEN** 统一 Animancer 表现入口 MUST 避免每帧从头重播同一个阶段动画

#### Scenario: 调试状态可见
- **WHEN** 统一 Animancer 表现入口接收移动动画上下文
- **THEN** 系统 MUST 暴露当前阶段、当前档位、当前动画名和当前速度作为只读调试信息

#### Scenario: 外观层不接管逻辑
- **WHEN** 统一 Animancer 表现入口播放 WalkEnd 或 RunEnd
- **THEN** 表现入口 MUST NOT 调用状态机切换 API
- **AND** MUST NOT 调用运动执行端口
- **AND** MUST NOT 写入角色 Transform

### Requirement: 角色基础移动动画表归属
系统 MUST 将当前角色的 Walk/Run 基础移动动画表归属到角色 prefab 上的统一 FullBody Animancer 表现入口或其绑定配置，而不是要求每个场景实例重复维护基础移动动画配置。

#### Scenario: 角色 prefab 持有 Walk/Run 动画表
- **WHEN** 设计者配置当前演示角色的基础移动动画
- **THEN** 角色 prefab 上的统一 Animancer 表现入口 MUST 引用对应的 Walk/Run 基础移动动画配置
- **AND** 配置 MUST 提供 `Idle / WalkStart / WalkLoop / WalkEnd / RunStart / RunLoop / RunEnd`

#### Scenario: 场景不重复维护动画 Clip
- **WHEN** 同一角色 prefab 被放入不同演示场景
- **THEN** 场景实例 MUST NOT 需要分别维护一套 Walk/Run 基础移动动画引用才能播放移动动画
- **AND** 场景仍 MAY 维护输入、相机和移动参数等场景装配引用

### Requirement: WASD 自动发现动画外观层
系统 MUST 允许当前基础移动运行时组装入口在未显式绑定动画表现入口时，从当前角色对象层级内发现现有统一 FullBody Animancer 表现入口并提交移动动画上下文。

#### Scenario: 同对象发现 Presenter
- **WHEN** `PlayerLocomotionController` 的动画表现入口未绑定
- **AND** 同一 GameObject 上存在统一 FullBody Animancer 表现入口
- **THEN** 基础移动入口 MUST 使用该表现入口接收 `MovementAnimationContext`

#### Scenario: 子对象发现 Presenter
- **WHEN** `PlayerLocomotionController` 的动画表现入口未绑定
- **AND** 同一 GameObject 上不存在表现入口
- **AND** 当前角色子层级内存在统一 FullBody Animancer 表现入口
- **THEN** 基础移动入口 MUST 使用该子层级表现入口接收 `MovementAnimationContext`

#### Scenario: 禁止跨角色全局查找
- **WHEN** `PlayerLocomotionController` 自动发现动画外观层
- **THEN** 自动发现 MUST 限制在当前角色对象层级内
- **AND** MUST NOT 使用全场景查找连接其他角色的 Presenter

#### Scenario: 不隐式创建配置路径
- **WHEN** 当前角色对象层级内没有统一 FullBody Animancer 表现入口
- **THEN** 基础位移 MUST 仍可按现有逻辑运行
- **AND** 基础移动入口 MUST NOT 自动创建 Presenter、AnimancerComponent 或动画配置资产
- **AND** 基础移动入口 MUST NOT 通过 `Resources.Load` 或全局单例隐式加载动画表

### Requirement: 基础移动动画配置不改变位移权威
系统 MUST 在统一 Walk/Run 基础移动动画配置归属后继续保持基础移动位移权威在运动执行端口，统一 Animancer 表现入口只负责表现。

#### Scenario: 统一配置后仍走运动执行端口
- **WHEN** 角色通过 prefab 上的 Walk/Run 基础移动动画表播放移动动画
- **THEN** 角色位移 MUST 仍由运动执行端口执行
- **AND** 统一 Animancer 表现入口 MUST NOT 调用 `CharacterController.Move`
- **AND** 统一 Animancer 表现入口 MUST NOT 写入角色 `transform.position`

#### Scenario: Root Motion 仍需单独审批
- **WHEN** 实现统一配置归属时发现必须让完整 Root Motion 驱动基础移动
- **THEN** 实现 MUST 停止
- **AND** MUST 另建或更新 OpenSpec proposal 说明位移权威边界变化

### Requirement: Animancer 基础移动播放进度边界
系统 MUST 允许统一 Animancer 表现入口暴露当前基础移动播放进度快照，但表现入口 MUST 只负责播放和只读进度，不负责判断 `CanExit`、不负责打断仲裁、不负责状态机切换。

#### Scenario: Presenter 暴露只读进度
- **WHEN** 统一 Animancer 表现入口正在播放基础移动 alias
- **THEN** 表现入口 MUST 能提供当前 phase、alias key、normalized time 和是否已结束的只读快照
- **AND** 该快照 MUST 不携带 Animancer runtime 对象给逻辑层

#### Scenario: Presenter 不决定退出事实
- **WHEN** 统一 Animancer 表现入口提供播放进度快照
- **THEN** 表现入口 MUST NOT 计算 `CanExit`
- **AND** MUST NOT 读取 Locomotion 状态图条件
- **AND** MUST NOT 读取动作打断仲裁器

#### Scenario: Presenter 不通过 OnEnd 切状态
- **WHEN** 基础移动动画自然播放到结束
- **THEN** 统一 Animancer 表现入口 MUST NOT 通过 `OnEnd` 或等价回调直接切换 `Idle / MoveStart / MoveLoop / MoveStop`
- **AND** MUST NOT 调用 Locomotion 状态机切换 API
