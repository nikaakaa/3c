# basic-locomotion-animation Specification

## Purpose
TBD - created by archiving change add-basic-locomotion-animation. Update Purpose after archive.
## Requirements
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

### Requirement: 基础移动动画配置校验
系统 MUST 提供基础移动 Run-only alias 配置的校验能力，帮助设计者在普通 Inspector 或轻量 editor 中发现缺失 alias 和逻辑退出时长问题，但该校验 MUST NOT 接管 Animancer TransitionAsset 的播放参数编辑。

#### Scenario: 空 alias 报错
- **WHEN** `Idle / RunStart / RunLoop / RunEnd` 任一 alias key 为空
- **THEN** 校验结果 MUST 报告对应 alias 缺失

#### Scenario: 必需退出时长报错
- **WHEN** 校验要求 `RunEnd` 必须显式配置退出时长
- **AND** `RunEnd` 退出时长缺失
- **THEN** 校验结果 MUST 报告 `RunEnd` 退出时长缺失

#### Scenario: 不校验 Animancer 播放参数
- **WHEN** 设计者运行项目侧 Run 配置校验
- **THEN** 校验器 MUST NOT 把 Animancer TransitionAsset 的 fade、speed 或 normalized start time 作为本配置的错误来源

### Requirement: 基础移动动画阶段退出策略
系统 MUST 提供基础移动动画 phase config 的退出策略，使逻辑层能用纯数据判断某个阶段是否达到可退出时间。第一版退出策略 MUST 至少支持 `Manual` 和 `AfterDuration`。

#### Scenario: Manual 不产生时间退出事实
- **GIVEN** 当前 phase config 的退出策略为 `Manual`
- **WHEN** 逻辑层查询该 phase 是否达到退出时间
- **THEN** 查询结果 MUST 为 false
- **AND** 其它非时间条件仍可驱动状态切换

#### Scenario: AfterDuration 产生时间退出事实
- **GIVEN** 当前 phase config 的退出策略为 `AfterDuration`
- **AND** 退出时长为非负数
- **WHEN** 当前 phase 计时达到该退出时长
- **THEN** 逻辑层 MUST 能得到当前 phase 已达到退出时间的事实

#### Scenario: 配置校验
- **WHEN** phase config 的 alias key 为空
- **THEN** 校验结果 MUST 报告对应 phase 的 alias 缺失
- **WHEN** phase config 的退出策略为 `AfterDuration` 且退出时长小于 0
- **THEN** 校验结果 MUST 报告对应 phase 的退出时长非法

#### Scenario: 不校验 Animancer 播放参数
- **WHEN** 设计者运行项目侧 Run 配置校验
- **THEN** 校验器 MUST NOT 把 Animancer TransitionAsset 的 fade、speed、normalized start time 或 event 作为本配置的错误来源

### Requirement: 基础移动 OnAnimationEnd 退出策略
系统 MUST 允许基础移动 phase config 使用 `OnAnimationEnd` 退出策略，使 `MoveStop / RunEnd` 能通过动画播放结束事实退出，而不是要求设计者手动维护与动画长度一致的 exit duration。

#### Scenario: MoveStop 使用动画结束退出
- **GIVEN** `MoveStop` phase config 的 alias key 为 `RunEnd`
- **AND** `MoveStop` phase config 的退出策略为 `OnAnimationEnd`
- **WHEN** `RunEnd` 播放尚未结束
- **THEN** 动画事实层 MUST 产出 `CanExit=false`
- **WHEN** `RunEnd` 播放结束
- **THEN** 动画事实层 MUST 产出 `CanExit=true`

#### Scenario: OnAnimationEnd 不重复配置播放参数
- **WHEN** 设计者将 `MoveStop` 配置为 `OnAnimationEnd`
- **THEN** 项目侧 Run 基础移动配置 MUST NOT 要求填写 clip length
- **AND** MUST NOT 要求填写 Animancer fade duration
- **AND** MUST NOT 要求填写 Animancer speed
- **AND** MUST NOT 要求填写 normalized start time

#### Scenario: AfterDuration 兼容保留
- **WHEN** `MoveStart` 或其它 phase config 继续使用 `AfterDuration`
- **THEN** 系统 MUST 继续使用 phaseTime 和 exit duration 产出 `CanExit`
- **AND** 不需要对应 phase 存在有效 Animancer 播放进度

### Requirement: Animancer 基础移动播放进度边界
系统 MUST 允许 Animancer 基础移动外观层暴露当前基础移动播放进度快照，但外观层 MUST 只负责播放和只读进度，不负责判断 `CanExit`、不负责打断仲裁、不负责状态机切换。

#### Scenario: Presenter 暴露只读进度
- **WHEN** Animancer 外观层正在播放基础移动 alias
- **THEN** 外观层 MUST 能提供当前 phase、alias key、normalized time 和是否已结束的只读快照
- **AND** 该快照 MUST 不携带 Animancer runtime 对象给逻辑层

#### Scenario: Presenter 不决定退出事实
- **WHEN** Animancer 外观层提供播放进度快照
- **THEN** 外观层 MUST NOT 计算 `CanExit`
- **AND** MUST NOT 读取 Locomotion 状态图条件
- **AND** MUST NOT 读取动作打断仲裁器

#### Scenario: Presenter 不通过 OnEnd 切状态
- **WHEN** 基础移动动画自然播放到结束
- **THEN** Animancer 外观层 MUST NOT 通过 `OnEnd` 或等价回调直接切换 `Idle / MoveStart / MoveLoop / MoveStop`
- **AND** MUST NOT 调用 Locomotion 状态机切换 API

### Requirement: RunEnd 播完退出手动验证
系统 MUST 提供清晰的验证路径，证明 `RunEnd` 在无输入时能播完再回 `Idle`，并且在中途重新输入时能立即回到移动阶段。

#### Scenario: 无输入播完回 Idle
- **GIVEN** 当前基础移动配置将 `MoveStop` 设置为 `OnAnimationEnd`
- **AND** 当前角色已经进入 `MoveLoop`
- **WHEN** 玩家松开移动输入并保持无输入
- **THEN** 系统 MUST 播放 `RunEnd`
- **AND** MUST 在 `RunEnd` 播放结束后切换到 `Idle`

#### Scenario: 中途输入立即打断 RunEnd
- **GIVEN** 当前基础移动配置将 `MoveStop` 设置为 `OnAnimationEnd`
- **AND** 当前角色正在 `MoveStop` 播放 `RunEnd`
- **WHEN** 玩家在 `RunEnd` 播完前重新输入移动
- **THEN** 系统 MUST 立即切换到 `MoveStart` 或等价起步阶段
- **AND** MUST NOT 等待 `RunEnd` 播放结束

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

