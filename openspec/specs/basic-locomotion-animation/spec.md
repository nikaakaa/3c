# basic-locomotion-animation Specification

## Purpose
定义基础移动动画上下文、Walk/Run 动画配置、Animancer 外观层边界、动画退出事实和烘焙运动采样规则，确保动画表现不接管逻辑状态或位移权威。
## Requirements
### Requirement: 移动动画上下文
系统 MUST 提供不依赖 Animancer 和场景对象的移动动画上下文，用于把当前基础移动阶段、Walk/Run 档位、输入强度、世界方向和当前速度传递给动画外观层。

#### Scenario: 上下文承载移动阶段
- **WHEN** 基础移动阶段更新为 `Idle / MoveStart / MoveLoop / MoveStop`
- **THEN** 移动动画上下文 MUST 记录当前阶段
- **AND** 该上下文 MUST 不包含 Animancer 运行时类型

#### Scenario: 上下文承载 Walk/Run 档位
- **WHEN** 普通移动选择 Walk 或 `Action.Dodge` Directional 完成后的 Run latch 选择 Run
- **THEN** 移动动画上下文 MUST 记录当前基础移动档位
- **AND** Walk/Run MUST NOT 替代 `BasicMovementPhase`
- **AND** Run 档位 MUST NOT 依赖 Shift 持续按住

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
系统 MUST 通过正式 animation presenter / Presentation Layer 消费移动动画上下文，并根据 `phase + gait` 解析和播放 Walk/Run 基础移动动画。该表现入口 MUST 只负责动画播放和播放进度暴露，不负责状态机切换或位移执行。

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
系统 MUST 将当前角色的 Walk/Run 基础移动动画表归属到角色正式 animation presenter 绑定配置，而不是要求每个场景实例重复维护基础移动动画配置。

#### Scenario: 角色 prefab 持有 Walk/Run 动画表
- **WHEN** 设计者配置当前演示角色的基础移动动画
- **THEN** 角色 prefab 上的正式 animation presenter 绑定配置 MUST 引用对应的 Walk/Run 基础移动动画配置
- **AND** 配置 MUST 提供 `Idle / WalkStart / WalkLoop / WalkEnd / RunStart / RunLoop / RunEnd`

#### Scenario: 场景不重复维护动画 Clip
- **WHEN** 同一角色 prefab 被放入不同演示场景
- **THEN** 场景实例 MUST NOT 需要分别维护一套 Walk/Run 基础移动动画引用才能播放移动动画
- **AND** 场景仍 MAY 维护输入、相机和移动参数等场景装配引用

### Requirement: WASD 动画外观层显式绑定
系统 MUST 要求当前基础移动运行时组装入口通过正式角色配置、prefab binding 或批准的等价装配点获得 animation presenter。缺失绑定时 MUST 报告明确配置错误或跳过动画提交，MUST NOT 通过层级扫描、Resources 或全局单例隐式发现表现入口。

#### Scenario: 同对象显式绑定 Presenter
- **WHEN** 基础移动运行时组装入口的动画表现入口未绑定
- **AND** 同一 GameObject 上存在 animation presenter
- **THEN** 基础移动入口 MUST NOT 自动扫描并绑定该表现入口
- **AND** 系统 MUST 要求通过正式配置或 prefab binding 显式连接

#### Scenario: 子对象不隐式发现 Presenter
- **WHEN** 基础移动运行时组装入口的动画表现入口未绑定
- **AND** 同一 GameObject 上不存在表现入口
- **AND** 当前角色子层级内存在 animation presenter
- **THEN** 基础移动入口 MUST NOT 通过子层级扫描连接该表现入口
- **AND** 系统 MUST 报告缺失正式绑定或跳过动画提交

#### Scenario: 禁止跨角色全局查找
- **WHEN** 基础移动运行时组装入口缺失动画外观层绑定
- **THEN** 系统 MUST NOT 使用全场景查找连接其他角色的 Presenter
- **AND** MUST NOT 使用当前角色层级扫描作为 fallback

#### Scenario: 不隐式创建配置路径
- **WHEN** 当前角色缺失正式 animation presenter 绑定
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

### Requirement: 基础移动动画配置校验
系统 MUST 提供基础移动 Walk/Run alias 配置的校验能力，帮助设计者在普通 Inspector 或轻量 editor 中发现缺失 alias、缺失 motion profile 绑定和逻辑退出策略问题，但该校验 MUST NOT 接管 Animancer TransitionAsset 的播放参数编辑。

#### Scenario: 空 alias 报错
- **WHEN** `Idle / WalkStart / WalkLoop / WalkEnd / RunStart / RunLoop / RunEnd` 任一必需 alias key 为空
- **THEN** 校验结果 MUST 报告对应 alias 缺失

#### Scenario: 必需退出时长报错
- **WHEN** 校验要求某个基础移动阶段必须显式配置退出时长
- **AND** 对应 Walk 或 Run 阶段的退出时长缺失
- **THEN** 校验结果 MUST 报告对应 gait 和 phase 的退出时长缺失

#### Scenario: 不校验 Animancer 播放参数
- **WHEN** 设计者运行项目侧基础移动配置校验
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

#### Scenario: AfterDuration 作为正式时间退出策略
- **WHEN** `MoveStart` 或其它 phase config 继续使用 `AfterDuration`
- **THEN** 系统 MUST 继续使用 phaseTime 和 exit duration 产出 `CanExit`
- **AND** 不需要对应 phase 存在有效 Animancer 播放进度

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

### Requirement: Locomotion 动画不由状态节点万能动画字段配置
系统 MUST 继续通过 Locomotion phase、运行时 gait facts、基础移动动画配置和 Animancer presenter 解析基础移动动画。普通 Locomotion 状态节点 MUST NOT 通过万能 animation binding 字段配置具体动画 key；如 TurnBack 等特殊 Locomotion 状态需要 timeline alias 或 motion alias，MUST 通过明确的 Locomotion animation alias / TurnBack motion policy 模块表达。

#### Scenario: MoveLoop 使用 phase 和 gait
- **WHEN** 当前状态节点具备 `MoveLoop` Locomotion phase 模块
- **AND** 运行时 gait fact 为 Run
- **THEN** 基础移动动画系统 MUST 使用 `MoveLoop + Run` 或等价 facts 解析 Animancer key
- **AND** `MoveLoop` 节点 MUST NOT 需要配置独立 action animation key

#### Scenario: TurnBack 使用单一 alias 来源
- **WHEN** 当前状态节点具备 TurnBack motion policy 模块
- **THEN** TurnBack 播放、timeline binding 和 baked motion profile MUST 使用同一正式 alias 来源或明确映射
- **AND** 配置者 MUST NOT 在状态节点万能 animation 字段和 TurnBack policy 字段重复填写同一个 alias

#### Scenario: gait 不进入状态图
- **WHEN** 角色从 Walk 切换到 Run
- **THEN** 状态路径 MUST NOT 因 gait 变化变成 WalkLoop 或 RunLoop
- **AND** gait MUST 作为运行时事实进入动画和运动解析

### Requirement: TurnBack Intent 保持候选事实边界
基础移动系统 MUST 可以继续计算 `LocomotionTurnBackIntent` 来表达 `MoveStart` 或 `MoveLoop` 反向输入候选，但该 intent MUST 只作为状态请求仲裁入口的输入。基础移动系统 MUST NOT 因 intent 本身直接切换到 TurnBack、播放 TurnBack 动画或提交 TurnBack motion。

#### Scenario: Locomotion 只产出候选 intent
- **GIVEN** 当前基础移动 phase 为 MoveStart 或 MoveLoop
- **AND** 当前 gait 为 Run
- **AND** 输入方向与角色朝向满足反向阈值
- **WHEN** 基础移动系统派生 locomotion decision facts
- **THEN** 它 MAY 产出有效 `LocomotionTurnBackIntent`
- **AND** MUST NOT 在该阶段直接切换逻辑状态

#### Scenario: intent 不直接驱动运动输出
- **GIVEN** locomotion facts 中存在有效 `LocomotionTurnBackIntent`
- **AND** Locomotion 状态图当前状态尚未进入 `Locomotion.TurnBack`
- **WHEN** 基础移动系统构建本帧运动
- **THEN** 系统 MUST NOT 采样 TurnBack baked motion
- **AND** MUST NOT 因 intent 本身锁定普通输入旋转或平面位移

#### Scenario: TurnBack 状态后才消费窗口运动
- **GIVEN** Locomotion 状态图已经通过 accepted TurnBack request fact 进入 `Locomotion.TurnBack`
- **AND** 当前 timeline facts 表示 motion window active
- **WHEN** 基础移动系统构建本帧运动
- **THEN** 系统 MUST 通过 TurnBack motion policy 采样 configured baked motion
- **AND** input lock 行为 MUST 由 timeline facts 和 TurnBack motion policy 决定

### Requirement: 已审批动画运动源边界
系统 MUST 允许经 OpenSpec 审批的基础移动或 Locomotion 逻辑状态通过通用动画运动源管线贡献运动。第一版贡献 MUST 在 `TickSampledMotion` 模式下转换为纯数据 movement facts 并由统一 motion executor 应用。该能力不得改变普通 Walk/Run 动画只负责表现的默认边界。

#### Scenario: 普通基础移动仍只表现
- **WHEN** 角色播放 Idle、MoveStart、MoveLoop 或 MoveStop 的普通 Walk/Run 动画
- **THEN** 动画外观层 MUST 继续只消费移动动画上下文和暴露只读播放进度
- **AND** MUST NOT 直接移动角色根

#### Scenario: 已审批状态使用动画运动源
- **GIVEN** 当前逻辑状态声明了通用动画运动源策略
- **WHEN** 动画播放进度产生本 tick 采样窗口
- **THEN** 基础移动动画系统 MUST 能按策略提供该状态的 yaw 或 translation 数据
- **AND** MUST 提交为 movement facts

#### Scenario: TurnBack 作为首个使用者
- **GIVEN** 当前逻辑状态为 `Locomotion.TurnBack`
- **WHEN** TurnBack 配置启用通用动画运动源策略
- **THEN** 系统 MUST 使用该通用策略解析 `Locomotion.Turn.Back` 的动画运动贡献
- **AND** 默认 MUST 选择 `TickSampledMotion` 以支持后续预测、回滚和预测矫正
- **AND** MUST NOT 依赖 TurnBack 专用 pending runtime root delta 分支作为默认运动来源

### Requirement: 基础移动脚相位 Profile 绑定
系统 MUST 允许基础移动动画配置绑定 Locomotion 脚相位 Profile，使 `TurnBack` 和 `RunLoop` 能通过正式配置参与脚相位匹配。该绑定 MUST 归属于现有基础移动动画配置或其明确子配置，不得新增游离全局配置或 Resources 隐式加载路径。

#### Scenario: RunLoop 绑定脚相位 Profile
- **GIVEN** 当前角色基础移动动画配置包含 `RunLoop` alias
- **WHEN** 设计者为 `MoveLoop + Run + RunLoop` 绑定脚相位 Profile
- **THEN** 运行时 MUST 能通过 phase、gait 和 alias key 解析到该 profile

#### Scenario: TurnBack 绑定脚相位 Profile
- **GIVEN** 当前角色基础移动动画配置包含 `Locomotion.Turn.Back` alias
- **WHEN** 设计者为 `TurnBack + Run + Locomotion.Turn.Back` 绑定脚相位 Profile
- **THEN** 运行时 MUST 能通过 phase、gait 和 alias key 解析到该 profile

#### Scenario: 不新增隐式配置路径
- **WHEN** 当前配置未绑定脚相位 Profile
- **THEN** 系统 MUST 报告配置缺失或匹配无效
- **AND** MUST NOT 通过 `Resources.Load`、全局单例或硬编码路径寻找 profile

### Requirement: 移动动画上下文携带相位匹配请求
系统 MUST 扩展移动动画上下文，使其可以携带纯数据脚相位匹配请求或匹配结果。该上下文 MUST 不携带脚相位 Profile、Animancer runtime、AnimationClip、TransitionAsset、Transform、CharacterController 或 InputAction。

#### Scenario: TurnBack 后 RunLoop 上下文携带匹配结果
- **GIVEN** 黑板中存在有效 TurnBack exit foot phase
- **AND** RunLoop profile 解析出有效 start normalized time
- **WHEN** 系统构建 `MoveLoop + Run` 的移动动画上下文
- **THEN** 上下文 MUST 携带有效的 RunLoop start normalized time override

#### Scenario: 普通移动上下文不携带匹配请求
- **GIVEN** 当前不是从 TurnBack 进入 RunLoop
- **WHEN** 系统构建移动动画上下文
- **THEN** 上下文 MUST 标记为没有脚相位匹配 override

#### Scenario: 上下文保持纯数据
- **WHEN** 动画外观层读取移动动画上下文
- **THEN** 它 MUST NOT 能通过上下文访问脚相位 Profile 资产
- **AND** MUST NOT 能访问 Unity 场景对象或 Animancer runtime

### Requirement: Animancer RunLoop 起播相位应用
Animancer 基础移动外观层 MUST 在新进入 `MoveLoop + RunLoop` 时消费脚相位匹配结果，并设置一次目标 state 的 normalized time。外观层 MUST NOT 因脚相位匹配决定逻辑状态、移动命令或 TurnBack 退出。

#### Scenario: 新播放 RunLoop 应用 start override
- **GIVEN** 移动动画上下文阶段为 `MoveLoop`
- **AND** gait 为 `Run`
- **AND** alias key 解析为 `RunLoop`
- **AND** 上下文携带有效 start normalized time override
- **WHEN** Presenter 新播放 RunLoop
- **THEN** Presenter MUST 设置新 state 的 `NormalizedTime` 为该 override
- **AND** MUST 记录诊断说明该 override 已应用

#### Scenario: 相同 RunLoop 不重复应用 start override
- **GIVEN** 当前 Presenter 已经在播放 `MoveLoop + RunLoop`
- **AND** 下一帧收到相同 phase、gait 和 alias key
- **WHEN** 上下文仍携带 start normalized time override
- **THEN** Presenter MUST 保持现有播放进度
- **AND** MUST NOT 每帧重设 `NormalizedTime`

#### Scenario: 无效 override 不改变播放
- **GIVEN** 上下文没有有效 start normalized time override
- **WHEN** Presenter 新播放 RunLoop
- **THEN** Presenter MUST 使用现有 Animancer 播放行为
- **AND** MUST NOT 猜测脚相位起播点

### Requirement: 基础移动脚相位自动测试和手动验证
系统 MUST 为基础移动脚相位匹配提供 EditMode 测试和手动验证步骤，证明 TurnBack 后 RunLoop 起播相位被正确应用，且普通移动动画不受影响。

#### Scenario: 自动测试覆盖 Presenter 起播
- **WHEN** 运行基础移动动画 EditMode 测试
- **THEN** 测试 MUST 覆盖 RunLoop 新进入时应用 start override
- **AND** MUST 覆盖相同 RunLoop 连续帧不重复应用 override

#### Scenario: 手动验证 TurnBack 衔接
- **GIVEN** 用户在 Sandbox 使用当前 Corin 角色
- **AND** Locomotion 与 Animation 诊断日志已启用
- **WHEN** 用户从 RunLoop 触发 TurnBack 并继续移动
- **THEN** TurnBack 退出后 MUST 回到 RunLoop
- **AND** 日志 MUST 能显示 exit foot phase 和 RunLoop matched start normalized time

### Requirement: TurnBack 动画运动策略
系统 MUST 为 `Locomotion.TurnBack` 提供独立于普通 Walk/Run 基础移动的动画运动策略。该策略 MUST 允许 TurnBack 在转身窗口内使用 baked motion profile 或等价采样事实驱动根位移和朝向，并允许第一版忽略 TurnBack 动画平移尾巴，转完后交还普通 MoveLoop。该策略 MUST 使用烘焙运动数据入口，使编辑器可以生成 yaw、translation、marker、entry timing 和 exit timing 的纯数据资产。

#### Scenario: TurnBack 播放配置 alias
- **GIVEN** 当前逻辑状态为 `Locomotion.TurnBack`
- **WHEN** 系统构建移动动画上下文
- **THEN** 动画外观层 MUST 播放 `Locomotion.Turn.Back` 或配置中等价 alias
- **AND** 该 alias MUST 来自现有动画配置或状态输出绑定

#### Scenario: TurnBack yaw 作为纯数据事实
- **GIVEN** TurnBack 动画包含转身 yaw
- **WHEN** 动画外观层或采样器读取本帧播放窗口
- **THEN** 系统 MUST 产出本帧 yaw 贡献作为纯数据事实
- **AND** 该事实 MUST 不携带 Animancer runtime state
- **AND** 该事实 MUST 不直接写 Transform

#### Scenario: TurnBack 可消费烘焙运动数据
- **GIVEN** TurnBack motion policy 引用了有效 baked motion profile
- **WHEN** 运行时采样当前播放窗口
- **THEN** 系统 MUST 能从 baked profile 读取 yaw、translation 或 marker 事实
- **AND** 采样结果 MUST 仍以纯数据 movement facts 进入运动命令
- **AND** 运行时 sampler MUST NOT 依赖 UnityEditor API

#### Scenario: TurnBack 第一版只消费烘焙转身窗口平移
- **GIVEN** `Locomotion.Turn.Back` 动画包含转身后的继续跑动位移
- **WHEN** TurnBack motion policy 的 translation source 为 baked motion profile 或等价配置
- **THEN** 系统 MUST 只将烘焙转身窗口内的平面位移作为 TurnBack 平面位移贡献
- **AND** MUST NOT 将该跑步尾巴平移作为 TurnBack 平面位移贡献
- **AND** 转完后 MUST 由普通 MoveLoop 位移重新接管

#### Scenario: Presenter 不拥有 TurnBack 逻辑
- **WHEN** Animancer 外观层播放 TurnBack 动画
- **THEN** 外观层 MUST 只负责播放、暴露进度、采样或转发 root motion 事实
- **AND** MUST NOT 决定 TurnBack 是否进入
- **AND** MUST NOT 决定 TurnBack 是否退出
- **AND** MUST NOT 调用 motion executor 或 `CharacterController.Move`

#### Scenario: 不靠手工删除源曲线修复 RootT 基线
- **GIVEN** TurnBack 动画 RootT 存在非零基线或预览偏移
- **WHEN** 运行时 TurnBack motion policy 使用 baked motion profile
- **THEN** 系统 MUST 通过 motion policy 消费生成后的纯数据平移和 yaw
- **AND** MAY 使用工具生成不带平面漂移的视觉 clip
- **AND** MUST NOT 要求用户手工删除源 RootT、RootQ 或 skeleton 根位移曲线作为正确运行前提

### Requirement: TurnBack 动画退出事实
系统 MUST 能基于 TurnBack policy 的进入/退出时间、转完点或等价 marker 产生动画退出事实，使 TurnBack 可以在转身完成后退出，而不是必须等待整段动画自然结束。

#### Scenario: 进入时间由 policy 表达
- **GIVEN** TurnBack policy 配置了 start normalized time、fade 或 lock window
- **WHEN** TurnBack 状态进入
- **THEN** 动画请求 MUST 使用这些进入时间参数或其等价配置
- **AND** 输入锁定窗口 MUST 与 policy 中的时间事实一致

#### Scenario: 转完点产生 can exit
- **GIVEN** 当前状态为 `Locomotion.TurnBack`
- **AND** policy 配置了 turn complete normalized time
- **WHEN** 当前 `Locomotion.Turn.Back` 播放进度达到该 normalized time
- **THEN** 动画事实层 MUST 产出 TurnBack 可退出事实

#### Scenario: 未到转完点不能退出
- **GIVEN** 当前状态为 `Locomotion.TurnBack`
- **AND** 当前播放进度未达到 turn complete normalized time
- **WHEN** 状态机评估 TurnBack 退出 transition
- **THEN** `LocomotionAnimationCanExit` 或等价条件 MUST 为 false

#### Scenario: 自然结束必须是正式退出策略
- **GIVEN** TurnBack policy 显式配置 `NaturalEndExit` 或批准等价正式退出策略
- **WHEN** `Locomotion.Turn.Back` 播放到自然结束
- **THEN** 系统 MAY 使用现有动画结束事实允许退出
- **AND** MUST 输出诊断说明使用了自然结束退出策略
- **AND** 缺失 turn complete marker 时 MUST NOT 静默回退到自然结束退出

### Requirement: TurnBack 编辑器预留边界
系统 MUST 为 TurnBack animation motion policy 保留编辑器 authoring 边界。编辑器 MAY 在后续变更中从 animation clip 提取 root yaw、root translation、turn complete marker、entry timing、exit timing 和校验报告，但运行时 MUST 只依赖生成后的纯数据资产或配置。

#### Scenario: 编辑器生成数据不进入运行时依赖
- **WHEN** 后续编辑器工具生成 TurnBack baked motion profile
- **THEN** 生成结果 MUST 是运行时可读取的纯数据资产或等价配置
- **AND** 运行时代码 MUST NOT 引用 UnityEditor 命名空间

#### Scenario: 编辑器可校验动画窗口
- **WHEN** 设计者使用后续 TurnBack 编辑器检查动画
- **THEN** 编辑器 MAY 报告 RootT 基线、turn complete marker、entry timing、exit timing 和 yaw 累计值
- **AND** 这些报告 MUST NOT 改变运行时状态权威

### Requirement: Corin Prefab Locomotion 动画绑定迁移
系统 MUST 让 Corin prefab 的 Locomotion 动画绑定从正式角色配置根和正式 animation presenter 路径解析。Prefab 迁移 MUST NOT 通过旧 `runAnimationConfig` 字段形成第二动画配置权威。

#### Scenario: Locomotion module 从根配置解析动画配置
- **WHEN** 自动校验 Corin prefab 的正式角色 runtime 装配
- **THEN** `characterConfig.LocomotionAnimation` MUST 是正式 Locomotion 动画配置来源
- **AND** 旧 `runAnimationConfig` 字段 MUST NOT 作为正式 fallback
- **AND** 缺失 Locomotion 动画配置 MUST 被报告为配置错误

#### Scenario: Presenter 引用不丢失
- **WHEN** prefab 迁移完成后运行角色帧输出
- **THEN** Locomotion animation presentation MUST 仍通过正式 presenter 或统一 presenter 路径提交
- **AND** 状态机、motion executor 和 prefab 迁移逻辑 MUST NOT 直接调用 Animancer runtime 对象

#### Scenario: Locomotion 运行时引用保持可解析
- **WHEN** 自动校验 Corin prefab 的 Locomotion Unity-facing adapters
- **THEN** input source、motion executor、facing provider、camera reference 和 locomotion presenter 引用 MUST 保持可解析或明确为空且由正式 resolver 处理
- **AND** 迁移 MUST NOT 新增跨角色全局查找来补齐这些引用

### Requirement: 采样型播放恢复与首次进入分离
当基础移动状态声明使用 `TickSampledMotion`、root motion profile 或等价 animation-driven sampled motion 时，基础移动动画外观层 MUST 区分“首次进入或真实新播放”与“从 rollback snapshot 恢复后继续播放”。`RestorePlaybackProgress` 或等价恢复入口 MUST 将外观层 seek 到给定 phase/alias/normalized time，并建立后续同 alias `Present` 可识别的恢复播放段；同一播放段的后续 `Present` MUST NOT 执行 one-shot restart 或将 normalized time 重置为 start normalized time。纯表现动画 MAY 不执行该恢复流程。

#### Scenario: Restore 后同 alias 不归零
- **GIVEN** `TurnBack` 声明使用 sampled motion
- **AND** 外观层已通过 restore 恢复到 `TurnBack` alias 的 normalized time `0.35`
- **WHEN** 下一次 `Present` 收到相同 phase、gait 和 alias
- **THEN** 外观层 MUST 保持恢复后的播放段
- **AND** MUST NOT 将 normalized time 重置为 `0`
- **AND** MUST NOT 将该状态当作首次进入 TurnBack

#### Scenario: 真实新进入仍归零
- **GIVEN** 角色从非 TurnBack 状态真实进入 TurnBack
- **WHEN** 外观层播放 TurnBack alias
- **THEN** one-shot restart MAY 将 normalized time 设置为 policy start normalized time
- **AND** 该行为 MUST 不依赖 rollback debug runner 或 F6 特判

#### Scenario: 恢复入口不泄漏 Animancer 对象
- **WHEN** 逻辑层请求恢复基础移动播放进度
- **THEN** 请求 MUST 使用纯数据 playback progress
- **AND** 逻辑层 MUST NOT 读取或保存 `AnimancerState`、`AnimationClip`、`TransitionAsset` 或 Animator 引用

#### Scenario: 纯表现动画不强制恢复
- **GIVEN** 某基础移动动画只负责视觉 blend 或过渡表现
- **AND** 该动画播放进度未被 motion source 声明为 sampled motion 输入
- **WHEN** rollback restore 恢复角色 simulation 状态
- **THEN** 外观层 MAY 使用自身表现策略继续播放
- **AND** 该播放进度 MUST NOT 覆盖 sampled motion playback window、motion facts 或 runtime blackboard 权威事实

### Requirement: 基础移动外观层不覆盖 TickSampledMotion 权威
当基础移动状态声明使用 `TickSampledMotion` 或等价 profile-driven motion 时，外观层 MUST 只表现 simulation 提供的 sampled motion playback window。外观层 MAY 暴露只读播放进度给事实采样器，但不得在 rollback restore/replay 后用自身播放起点覆盖 simulation 的 playback progress 或 sampling window。

#### Scenario: Simulation 恢复进度后外观层跟随
- **GIVEN** simulation restore state 指定 phase、alias 和 normalized time
- **WHEN** 外观层恢复播放状态
- **THEN** 外观层 MUST seek 到该 normalized time
- **AND** 后续同 tick 的 animation facts MUST 与恢复进度一致

#### Scenario: 外观层不成为运动 source
- **GIVEN** 当前状态的位移或 yaw 来自 profile sampling
- **WHEN** 外观层播放对应视觉动画
- **THEN** 外观层 MUST NOT 通过 `OnAnimatorMove`、pending delta、Transform 写入或 motion executor 调用贡献 simulation movement facts
- **AND** profile sampling window MUST 来自 simulation playback state
