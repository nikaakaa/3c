# action-animation-profile Specification

## Purpose
定义 Action 动画 Profile 的稳定 key、角色级替换边界、Locomotion 配置分离规则和动作表现校验要求。
## Requirements
### Requirement: 动作动画 Profile 数据源
系统 MUST 提供动作动画 Profile 数据源，用于把稳定 action animation key 映射到具体动画表现。动作逻辑、状态生命周期接口和状态图或 Action 输出 MUST 只输出 action animation key，不得写死具体角色 clip、可琳动画名、Animancer transition asset 或 BBB 运行时资源路径。

#### Scenario: Profile 保存动作动画条目
- **WHEN** 设计者配置动作动画 Profile
- **THEN** Profile entry MUST 能保存 action animation key
- **AND** MUST 能保存具体动画引用或等价 Animancer transition 引用
- **AND** MAY 保存 fade 参数、播放参数和调试名

#### Scenario: Profile 不是行为状态机
- **WHEN** 动作动画 Profile 配置 `Action.Dodge.Directional` 或 `Action.Dodge.Backstep`
- **THEN** Profile MUST 只表达动作语义 key 到动画表现资源的映射
- **AND** Profile MAY 使用直接 clip、Animancer transition 或等价 transition asset
- **AND** Profile MUST NOT 替代 Action lifecycle、行为图状态注册、进入条件、退出条件或运动权威

#### Scenario: Profile 通过动画绑定入口接入
- **WHEN** 系统提供 Action 动画绑定集或等价动画配置入口
- **THEN** 动作动画 Profile MAY 作为该绑定入口的子配置或引用存在
- **AND** 设计者 SHOULD 能通过角色级 Action 动画绑定入口追踪到 Directional 和 Backstep 的动画表现资源
- **AND** 动作动画 Profile MUST NOT 被要求成为和动作逻辑入口、动画绑定入口无绑定关系的游离配置

#### Scenario: 状态生命周期不写死 clip
- **WHEN** `Enter`、`Tick` 或 `Exit` 生命周期产出动作动画请求
- **THEN** 生命周期输出 MUST 使用 `Action.Dodge.Directional`、`Action.Dodge.Backstep` 或等价稳定 key
- **AND** 生命周期实现 MUST NOT 直接引用具体 `AnimationClip`
- **AND** 生命周期实现 MUST NOT 直接引用具体 Animancer transition asset

#### Scenario: 动作逻辑不写死 clip
- **WHEN** Shift Dodge 请求动画表现
- **THEN** 动作逻辑 MUST 输出 `Action.Dodge.Directional` 或 `Action.Dodge.Backstep` key
- **AND** 动作逻辑 MUST NOT 直接引用具体 `AnimationClip`
- **AND** 动作逻辑 MUST NOT 直接引用具体角色动画资源名

#### Scenario: 角色可替换动画套件
- **GIVEN** 同一个 Shift Dodge 动作逻辑和 Action lifecycle 输出
- **WHEN** 设计者替换动作动画 Profile 中的 Directional 或 Backstep 动画引用
- **THEN** 系统 MUST 使用新的动画表现
- **AND** 不需要修改动作逻辑代码或状态机资产

### Requirement: Action.Dodge 动画 Key
系统 MUST 为 Shift Dodge / `Action.Dodge` 第一版提供两个稳定动作动画 key：方向冲刺和后闪。key MUST 表达动作语义，而不是表达具体角色、clip 文件名或导入来源。

#### Scenario: 方向冲刺 key
- **WHEN** 动作变体为 `Directional`
- **THEN** 动作动画 key MUST 为 `Action.Dodge.Directional` 或等价稳定 ID
- **AND** 该 key MUST 可由动作动画 Profile 解析

#### Scenario: 后闪 key
- **WHEN** 动作变体为 `Backstep`
- **THEN** 动作动画 key MUST 为 `Action.Dodge.Backstep` 或等价稳定 ID
- **AND** 该 key MUST 可由动作动画 Profile 解析

#### Scenario: key 不绑定可琳
- **WHEN** 系统定义动作动画 key
- **THEN** key MUST NOT 包含可琳、Corin、具体 fbx、具体 clip 文件名或 BBB 路径

### Requirement: 动作动画 Profile 校验
系统 MUST 对动作动画 Profile 提供可测试的校验，帮助设计者发现空 key、重复 key 和缺失动画引用。校验 MUST 不接管动作逻辑、状态仲裁或运动执行。

#### Scenario: 空 key 报错
- **GIVEN** Profile 中存在空 action animation key
- **WHEN** 运行 Profile 校验
- **THEN** 校验结果 MUST 包含错误

#### Scenario: 重复 key 报告错误或 warning
- **GIVEN** Profile 中存在重复 action animation key
- **WHEN** 运行 Profile 校验
- **THEN** 校验结果 MUST 报告重复 key
- **AND** 重复项 MUST NOT 被静默忽略

#### Scenario: 缺失动画引用报错
- **GIVEN** Profile entry 没有可播放动画引用
- **WHEN** 运行 Profile 校验
- **THEN** 校验结果 MUST 包含错误

### Requirement: 动作动画 Presenter 边界
系统 MUST 通过正式 Animancer 表现入口或等价 Action animation presenter 消费动作动画命令并播放动画。该表现入口 MUST 只负责动画表现和只读播放进度，不得决定动作是否允许、不得切换业务状态、不得执行位移。

#### Scenario: Presenter 播放 Profile 动画
- **GIVEN** 动作动画命令包含 `Action.Dodge.Directional`
- **AND** Profile 能解析该 key
- **WHEN** 统一 Animancer 表现入口接收该命令
- **THEN** 表现入口 MUST 播放 Profile 中对应动画
- **AND** 表现入口 MUST 暴露当前 key 和播放进度作为只读调试信息

#### Scenario: Presenter 不做业务仲裁
- **WHEN** 统一 Animancer 表现入口接收动作动画命令
- **THEN** 表现入口 MUST NOT 调用 `ActionInterruptArbiter`
- **AND** MUST NOT 消费 `InputRequestBuffer`
- **AND** MUST NOT 决定 Dodge 是否允许进入

#### Scenario: Presenter 不执行位移
- **WHEN** 统一 Animancer 表现入口播放动作动画
- **THEN** 表现入口 MUST NOT 调用 `CharacterController.Move`
- **AND** MUST NOT 写入角色 Transform
- **AND** MUST NOT 成为 Dodge 运动事实来源

#### Scenario: 不保留动作专用正式播放组件
- **WHEN** 当前角色已经接入统一 Animancer 表现入口
- **THEN** 系统 MUST NOT 要求再挂载独立 `ActionAnimationAnimancerPresenter` 才能播放 `Action.Dodge.Directional` 或 `Action.Dodge.Backstep`
- **AND** 动作播放进度 MUST 来自统一表现入口的只读快照

### Requirement: 与基础移动动画配置分离
系统 MUST 保持动作动画 Profile 与现有基础移动 Walk/Run alias 配置分离。动作动画 Profile 不得替代 `RunLocomotionAnimationConfigSO` 的基础移动职责，统一 Animancer 表现入口也不得通过动作 Profile 决定 Locomotion phase 播放。

#### Scenario: 基础移动仍使用基础移动配置
- **WHEN** 统一 Animancer 表现入口播放 Idle、WalkStart、WalkLoop、WalkEnd、RunStart、RunLoop 或 RunEnd
- **THEN** 它 MUST 继续使用基础移动动画配置或等价基础移动 alias 解析
- **AND** MUST NOT 要求存在动作动画 Profile

#### Scenario: 动作动画 Profile 不接管 Locomotion
- **WHEN** 动作动画 Profile 配置 Shift Dodge 动画
- **THEN** Profile MUST NOT 定义 `Idle / MoveStart / MoveLoop / MoveStop` 状态图规则
- **AND** MUST NOT 决定 `MoveStop -> MoveStart` 或 `MoveStop -> Idle`

#### Scenario: 统一入口不合并配置归属
- **WHEN** 统一 Animancer 表现入口同时支持 Locomotion 和 Action 播放
- **THEN** Locomotion alias、退出策略和 motion profile MUST 仍归基础移动动画配置
- **AND** Action key 到动画表现资源的映射 MUST 仍归动作动画 Profile 或等价动作动画绑定入口

### Requirement: 动作动画 Profile 可测试和可验证
系统 MUST 提供自动测试和验证，证明动作动画 Profile 可配置、可校验、可替换，并且不会污染 Locomotion 和运动权威边界。

#### Scenario: 自动测试覆盖 Profile 行为
- **WHEN** 运行动作动画 Profile EditMode 测试
- **THEN** 测试 MUST 覆盖 key 解析、空 key、重复 key、缺失动画引用、Directional/Backstep 两个 key 和替换动画引用

#### Scenario: 静态边界验证
- **WHEN** 检查动作动画 Profile 和统一 Animancer 表现入口源码
- **THEN** 静态搜索 MUST 能确认它们不引用 `BBBNexus` 命名空间
- **AND** 表现入口源码 MUST 不直接调用 `CharacterController.Move`
- **AND** 当前角色正式 prefab/scene MUST NOT 同时挂载动作专用和基础移动专用两个正式 Animancer Presenter

#### Scenario: 替换动画验证
- **WHEN** 用户替换 Profile 中 `Action.Dodge.Directional` 或 `Action.Dodge.Backstep` 的动画引用
- **THEN** Play Mode 中对应动作表现 MUST 使用替换后的动画
- **AND** 动作方向、输入消费、Run latch 和基础移动恢复规则 MUST 不需要修改代码

### Requirement: 动作动画模块只保存稳定语义 key
系统 MUST 允许动作状态节点通过动作动画模块保存稳定 animation key 或 timeline binding key，用于产出动作动画请求。具体 Clip、TransitionAsset、fade、speed、start time 和 Animancer runtime state MUST 继续归属 Action Animation Profile、Animancer TransitionLibrary 或等价表现配置入口。

#### Scenario: Dodge 变体输出动作动画 key
- **WHEN** `Dodge` 节点的 Directional 变体进入
- **THEN** 动作动画模块 MUST 产出 `Action.Dodge.Directional` 或等价稳定 key
- **AND** 动作动画 Presenter MUST 只消费该 key 对应的播放请求
- **AND** 状态节点 MUST NOT 保存具体 AnimationClip 或 TransitionAsset

#### Scenario: 连续 Dodge 仍重播同 key
- **WHEN** `Dodge -> Dodge` transition 进入同一动作动画 key
- **THEN** 动作动画模块 MUST 再次产出动作动画请求
- **AND** Presenter MUST 将其视为新的播放意图
- **AND** 该行为 MUST NOT 依赖新建第二播放路径

### Requirement: Corin Prefab Action 动画绑定迁移
系统 MUST 让 Corin prefab 的 Action 动画表现绑定通过正式 animation presenter 路径解析。Prefab 迁移 MUST NOT 新增第二个 Action animation presenter 或绕过 Character output apply 阶段。

#### Scenario: Action presenter 引用保持唯一
- **WHEN** 自动校验 Corin prefab 上的 `CharacterFrameRuntimeController` 与 Action Unity-facing adapters
- **THEN** Action animation presenter dependency MUST 指向正式 action presenter 或已审批的统一 presenter
- **AND** prefab MUST NOT 同时启用两个正式 action animation presenter
- **AND** Action animation 播放 MUST 仍由 Character frame output 阶段提交

#### Scenario: 与统一 Presenter change 协调
- **WHEN** `refactor-unified-animancer-presenter` 已实施
- **THEN** 本变更 MUST 校验 prefab 不再同时挂载旧 Locomotion Presenter 和旧 Action Presenter 作为正式路径
- **AND** 若统一 Presenter 尚未实施，本变更 MUST NOT 提前删除旧 Presenter 导致当前正式播放路径断裂

### Requirement: Action 动画播放意图身份
系统 MUST 区分 Action 动画稳定语义 key 与 Action 动画播放意图身份。`ActionAnimationKey` 只表达要解析和播放的动作动画语义；播放意图身份 MUST 表达当前请求属于哪一次 Action 播放实例。Action 动画 Presenter MUST 使用播放意图身份决定是否复用当前播放段或重新播放，不得只凭相同 key 判断为同一次播放。

#### Scenario: 连续同 key Dodge 重播
- **GIVEN** 第一段 accepted Dodge 已经播放 `Action.Dodge.Directional`
- **AND** 第二段 accepted Dodge 也解析为 `Action.Dodge.Directional`
- **WHEN** 第二段 Dodge 的播放意图身份不同于第一段
- **THEN** Presenter MUST 将第二段 Dodge 视为新的播放意图
- **AND** MUST 重新播放该 key 对应动画
- **AND** MUST 将 Action 动画 normalized time 重置到新播放段起点

#### Scenario: 同一播放意图重复提交不重启
- **GIVEN** 当前 Action 动画 key 为 `Action.Dodge.Directional`
- **AND** 当前播放意图身份为 `A`
- **WHEN** 后续帧再次提交相同 key 和相同播放意图身份 `A`
- **THEN** Presenter MUST 保持当前播放段
- **AND** MUST NOT 每帧重新播放或重置 normalized time

#### Scenario: Restore 后同一播放意图不重启
- **GIVEN** Action 动画播放进度从 restore state 恢复到 key `Action.Dodge.Directional` 和播放意图身份 `A`
- **WHEN** 恢复后的同一次 Action 再次提交相同 key 和播放意图身份 `A`
- **THEN** Presenter MUST 保持恢复后的播放进度
- **AND** MUST NOT 把该请求当作新的 Dodge 播放段归零

#### Scenario: Presenter 不生成业务身份
- **WHEN** Presenter 接收 Action 动画播放请求
- **THEN** 播放意图身份 MUST 来自 Action 生命周期、状态机输出或等价纯数据上游
- **AND** Presenter MUST NOT 调用 Action 仲裁、读取输入缓冲或检查 Dodge 配置来生成播放意图身份

### Requirement: Action 动画重播保持配置边界
Action 动画重播语义 MUST 不改变动作动画 Profile 的配置职责。Profile 继续只负责将稳定 action animation key 解析为具体动画表现资源；播放意图身份 MUST NOT 写入 Profile entry，也不得要求设计者为连续 Dodge 配置第二份动画 key 或第二条播放路径。

#### Scenario: Profile 不复制连续 Dodge key
- **GIVEN** Profile 中存在 `Action.Dodge.Directional`
- **WHEN** 玩家连续两次进入 Directional Dodge
- **THEN** 系统 MUST 复用同一个稳定 key 解析动画资源
- **AND** MUST 通过不同播放意图身份触发第二次播放
- **AND** MUST NOT 要求新增 `Action.Dodge.Directional.2` 或等价重复 key

#### Scenario: 不新增 fallback 播放配置
- **WHEN** Action 动画播放意图身份缺失或无效
- **THEN** 系统 MUST 通过正式错误、拒绝播放或测试失败暴露问题
- **AND** MUST NOT 自动查找备用 Profile、备用 Presenter 或代码内置动画 key 继续运行
