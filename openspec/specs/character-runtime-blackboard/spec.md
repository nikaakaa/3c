# character-runtime-blackboard Specification

## Purpose
定义角色运行时黑板保存的 Locomotion、Action、Animation 和 Debug facts，以及 capture/restore 的事实边界。
## Requirements
### Requirement: Typed 角色运行时黑板
系统 SHALL 提供一个 typed 角色运行时黑板，用于集中承载角色运行时纯数据 facts。黑板 SHALL NOT 使用任意字符串 key 到 `object` 的通用字典作为核心模型。

#### Scenario: 黑板保存纯数据 facts
- **WHEN** 系统创建角色运行时黑板
- **THEN** 黑板 MUST 能保存 locomotion、action 和 animation 相关的纯数据 facts
- **AND** 黑板 facts MUST 可通过强类型字段或强类型 snapshot 读取
- **AND** 黑板核心模型 MUST NOT 暴露 `object` 值字典作为主要读写接口

#### Scenario: 黑板不是第二状态机
- **WHEN** 状态图 runtime 推进状态
- **THEN** 黑板 MUST NOT 自行执行状态转移
- **AND** 黑板 MUST NOT 保存一套独立于状态图 runtime 的 active state path 作为状态权威
- **AND** 状态图 snapshot 仍 MUST 是逻辑状态权威

### Requirement: 黑板纯数据边界
系统 SHALL 保证角色运行时黑板不保存 Unity 场景实例、Animancer 运行时对象或输入系统对象。黑板只能保存可测试、可快照、可恢复的纯数据。

#### Scenario: 禁止场景对象引用
- **WHEN** 实现黑板模型和黑板 snapshot
- **THEN** 黑板模型 MUST NOT 持有 `Transform`
- **AND** MUST NOT 持有 `Camera`
- **AND** MUST NOT 持有 `CharacterController`
- **AND** MUST NOT 持有 `UnityEngine.Object`

#### Scenario: 禁止表现层 runtime 引用
- **WHEN** 动画表现层向黑板提供动画事实
- **THEN** 黑板 MUST NOT 保存 Animancer runtime state
- **AND** MUST NOT 保存 `AnimationClip`
- **AND** MUST NOT 保存 TransitionAsset 或 TransitionLibrary 对象
- **AND** MAY 保存 alias key、normalized time、is ended 等纯数据摘要

#### Scenario: 禁止输入对象引用
- **WHEN** 输入层更新黑板相关 facts
- **THEN** 黑板 MUST NOT 保存 `InputAction`
- **AND** MUST NOT 保存输入系统设备对象
- **AND** MAY 保存当前 tick/step 对齐后的纯数据输入结果或请求摘要

### Requirement: 黑板写入权威
系统 SHALL 为每类黑板 facts 定义唯一写入权威。非权威模块只能读取 snapshot 或通过受控 adapter 提交事实，不得随意改写其它模块 facts。

#### Scenario: Locomotion facts 写入权威
- **WHEN** locomotion runtime 计算 last moving gait、MoveStop entry gait 或当前移动方向摘要
- **THEN** 只有 locomotion runtime 或其明确 adapter MAY 写入对应 Locomotion facts
- **AND** 状态机 runner、Action Presenter 和 Animation Presenter MUST NOT 直接改写对应 facts

#### Scenario: Action facts 写入权威
- **WHEN** action runtime 接受、完成或退出一个全身动作
- **THEN** 只有 action runtime 或其明确 adapter MAY 写入对应 Action facts
- **AND** locomotion runtime 和 animation Presenter MUST NOT 直接改写 action active/completed facts

#### Scenario: Animation facts 写入权威
- **WHEN** 动画播放进度需要被逻辑层读取
- **THEN** 只有动画 facts adapter MAY 将 Presenter 的只读播放进度转换为黑板 Animation facts
- **AND** Presenter MUST NOT 通过黑板请求状态切换
- **AND** Presenter MUST NOT 通过黑板请求移动执行

### Requirement: 黑板 Snapshot / Restore
系统 SHALL 提供角色运行时黑板的纯数据 snapshot 和 restore 能力，使跨帧事实可以参与本地回放、预测恢复和同步测试。

#### Scenario: 捕获黑板 snapshot
- **WHEN** 角色运行时捕获 simulation snapshot
- **THEN** 系统 MUST 捕获黑板当前纯数据 facts
- **AND** snapshot MUST 不包含 Unity 场景实例引用
- **AND** snapshot MUST 不包含 Animancer runtime 对象

#### Scenario: 恢复黑板 snapshot
- **GIVEN** 系统已经捕获一个黑板 snapshot
- **WHEN** 系统执行 restore
- **THEN** 黑板 MUST 恢复到 snapshot 中记录的 facts
- **AND** 恢复后的状态机 context MUST 能读取恢复后的 facts
- **AND** 重复 restore 同一个 snapshot MUST 得到一致结果

#### Scenario: Restore 不触发表现副作用
- **WHEN** 系统恢复黑板 snapshot
- **THEN** restore MUST NOT 播放动画
- **AND** restore MUST NOT 调用 `CharacterController.Move`
- **AND** restore MUST NOT 写入角色 Transform

### Requirement: 状态机读取黑板快照
系统 SHALL 允许状态图 context 读取黑板 snapshot 中的纯数据 facts，用于后续方向起步、脚步相位、转身和转角等条件判断。状态机 runner 自身 SHALL NOT 成为黑板字段维护器。context 组装 MUST 来自角色级 runtime、状态机 runtime 或 Locomotion/Action 窄模块，而不是旧 FullBody controller。

#### Scenario: Context 承载黑板 snapshot
- **WHEN** `CharacterFrameRuntimeController`、`CharacterStateMachineRuntime`、Locomotion runtime 或 Action runtime 组装 `CharacterStateMachineContext`
- **THEN** context MAY 携带黑板 snapshot 或等价只读 facts view
- **AND** transition evaluator MUST 只读取该只读 facts view
- **AND** evaluator MUST NOT 读取黑板可变实例
- **AND** context 组装 MUST NOT 依赖 旧 FullBody action controller

#### Scenario: Runner 不维护黑板
- **WHEN** `CharacterStateMachineRunner` tick 一帧
- **THEN** runner MUST NOT 直接写入黑板
- **AND** runner MAY 在输出 frame 中表达需要调用方应用的纯数据结果
- **AND** 调用方 adapter 负责把允许的结果写入对应 facts

### Requirement: 黑板支持后续动画决策扩展
系统 SHALL 将脚步相位、局部移动角、动作结束相位和转身/转角决策所需 facts 设计为黑板的后续扩展目标，但本变更 SHALL NOT 直接实现这些动画状态。

#### Scenario: 预留脚步相位事实
- **WHEN** 后续 proposal 接入脚步相位
- **THEN** 脚步相位 SHOULD 作为黑板中受控 Animation 或 Locomotion facts 的一部分
- **AND** 它的写入权威 MUST 在后续 proposal 中明确
- **AND** 停止动画选择 MAY 读取该事实选择左脚或右脚停止动画

#### Scenario: 预留方向角事实
- **WHEN** 后续 proposal 接入方向起步、原地转身或跑动转角
- **THEN** 局部移动角、目标朝向角或等价方向 facts SHOULD 通过黑板 snapshot 提供给状态机 context
- **AND** 状态机 MAY 使用这些 facts 进入对应 locomotion 状态
- **AND** 动画 Presenter MUST NOT 独自决定进入这些状态

#### Scenario: 本变更不实现新动画状态
- **WHEN** 实施本黑板变更
- **THEN** 系统 MUST NOT 在本变更中新增方向起步、原地转身或跑动转角状态
- **AND** MUST NOT 在本变更中替换现有 Walk/Run 基础动画配置策略
- **AND** MUST NOT 在本变更中改变现有 Dodge 动画配置入口

### Requirement: 黑板可测试和可诊断
系统 SHALL 为角色运行时黑板提供自动测试和运行时诊断路径，证明黑板不会破坏状态图 runtime、动画表现层和运动执行端口的既有边界。

#### Scenario: 自动测试覆盖默认值和写入规则
- **WHEN** 运行 EditMode 测试
- **THEN** 测试 MUST 覆盖黑板默认 facts
- **AND** MUST 覆盖 Locomotion、Action、Animation facts 的写入入口
- **AND** MUST 覆盖非权威模块无法直接改写其它 facts 的边界

#### Scenario: 自动测试覆盖 snapshot/restore
- **WHEN** 运行 EditMode 测试
- **THEN** 测试 MUST 覆盖黑板 snapshot 捕获
- **AND** MUST 覆盖黑板 restore
- **AND** MUST 覆盖重复 restore 的一致性

#### Scenario: 静态边界验证
- **WHEN** 运行边界测试或静态搜索
- **THEN** 验证 MUST 确认黑板模型不引用 Animancer runtime 类型
- **AND** MUST 确认黑板模型不引用 `Transform`、`Camera`、`CharacterController` 或 `InputAction`
- **AND** MUST 确认 Presenter 不通过黑板调用状态机切换或运动执行

#### Scenario: 手动验证现有行为不回退
- **WHEN** 开发者进入 Unity Play Mode 验证当前角色
- **THEN** WASD Idle、MoveStart、MoveLoop、MoveStop MUST 保持可用
- **AND** Shift 跑步松开后 RunEnd MUST 保持可用
- **AND** Dodge 后返回 Idle 或 MoveLoop 的现有行为 MUST 保持可用

### Requirement: Runtime facts 从模块输出派生
系统 MUST 允许 runtime blackboard facts 从状态节点模块输出和 adapter 回传 facts 派生，而不是从互斥 `Locomotion / Action` owner 分支直接推导。Blackboard MUST 保持纯数据边界，并继续支持预测回滚 snapshot/restore。

#### Scenario: Action facts 从动作模块输出派生
- **WHEN** 当前节点具备 Dodge 动作请求或动作位移模块
- **THEN** action runtime facts MUST 能派生当前 action state、variant、完成事实和 source step
- **AND** MUST NOT 依赖独立 Action runtime 作为第二状态权威

#### Scenario: Locomotion facts 从移动模块和 adapter facts 派生
- **WHEN** 当前节点具备 Locomotion phase 模块
- **THEN** locomotion runtime facts MUST 能派生 phase、gait、move intent 和 motion facts
- **AND** MUST NOT 通过第二 Locomotion 状态机决定 phase

#### Scenario: 回滚快照保持纯数据
- **WHEN** 捕获 rollback snapshot
- **THEN** snapshot MUST 保存 active state、state time、variant、模块必要 payload 和 runtime facts
- **AND** MUST NOT 保存 Unity 对象、Animancer state 或模块实例对象引用

### Requirement: 黑板保存 Locomotion 脚相位事实
系统 SHALL 扩展角色运行时黑板的 Animation facts，使其可以保存当前 locomotion 脚相位和最近一次 locomotion 退出脚相位。脚相位 facts SHALL 是纯数据，可 snapshot/restore，不得保存 Unity 场景实例或 Animancer runtime 对象。

#### Scenario: 当前脚相位写入黑板
- **WHEN** animation facts adapter 从当前 locomotion 播放进度采样到有效脚相位
- **THEN** 黑板 Animation facts MUST 保存当前 alias key、normalized time、foot phase、是否有效和 source step

#### Scenario: TurnBack 退出脚相位写入黑板
- **GIVEN** 当前 locomotion phase 为 `TurnBack`
- **AND** 当前脚相位 sample 有效
- **WHEN** 系统确认 TurnBack 将退出到 `MoveLoop + Run`
- **THEN** 黑板 Animation facts MUST 保存最近一次 locomotion exit foot phase

#### Scenario: 无效脚相位不伪造事实
- **WHEN** 当前播放进度无效或缺少有效 foot phase profile
- **THEN** 黑板 Animation facts MUST 标记当前脚相位无效
- **AND** MUST NOT 用 `Unknown` 伪装成可匹配脚相位

### Requirement: 脚相位事实 Snapshot / Restore
系统 SHALL 将 locomotion 脚相位 facts 纳入黑板 snapshot/restore，使本地回放、预测恢复和同步测试能恢复相同的相位匹配输入。

#### Scenario: Snapshot 捕获脚相位
- **GIVEN** 黑板中存在有效当前脚相位和 exit foot phase
- **WHEN** 系统捕获黑板 snapshot
- **THEN** snapshot MUST 包含这些脚相位 facts
- **AND** snapshot MUST 不包含 Unity 对象引用

#### Scenario: Restore 恢复脚相位
- **GIVEN** 系统已经捕获包含脚相位 facts 的 snapshot
- **WHEN** 系统 restore 该 snapshot
- **THEN** 黑板 MUST 恢复相同的当前脚相位和 exit foot phase
- **AND** 重复 restore 同一 snapshot MUST 得到一致结果

#### Scenario: Restore 不触发表现副作用
- **WHEN** 系统 restore 包含脚相位 facts 的黑板 snapshot
- **THEN** restore MUST NOT 播放动画
- **AND** restore MUST NOT 调用 `CharacterController.Move`
- **AND** restore MUST NOT 写入角色 Transform

### Requirement: 脚相位写入权威
系统 SHALL 明确脚相位 facts 的写入权威。只有 animation facts adapter MAY 将播放进度和脚相位 profile 采样结果写入黑板；Presenter、状态机 runner 和 movement executor MUST NOT 直接改写脚相位 facts。

#### Scenario: Adapter 写入脚相位
- **WHEN** 动画播放进度需要转换为脚相位事实
- **THEN** animation facts adapter MAY 写入黑板 Animation facts
- **AND** 写入内容 MUST 是纯数据 sample

#### Scenario: Presenter 不写黑板
- **WHEN** `BasicLocomotionAnimancerPresenter` 播放 RunLoop 并应用 start override
- **THEN** Presenter MUST NOT 直接写入黑板
- **AND** Presenter MUST NOT 通过黑板请求状态切换

#### Scenario: 状态机不维护脚相位
- **WHEN** 状态图 runtime 推进状态
- **THEN** runner MUST NOT 直接计算或改写脚相位 facts
- **AND** runner MAY 读取黑板 snapshot 中已有的脚相位 facts 作为条件或输出输入

### Requirement: 黑板 facts 的回滚权威分类
角色运行时黑板 MUST 支持或可被外部 resolver 映射到回滚权威分类。Locomotion facts、Action facts 和 Animation facts MUST 能被区分为 strict gameplay、presentation drift、predictive gameplay 或 ignored。黑板自身 MUST 继续只保存 facts，不得成为第二状态机或 comparer 策略实现。

#### Scenario: Locomotion facts 默认为 strict
- **WHEN** comparer 比较 locomotion phase、gait、world direction 或 move intent facts
- **THEN** 这些 facts MUST 默认属于 strict gameplay
- **AND** 差异 MUST 导致 strict mismatch

#### Scenario: Action facts 默认为 strict
- **WHEN** comparer 比较 action active、state、completed 或 movement facts
- **THEN** 这些 facts MUST 默认属于 strict gameplay
- **AND** 差异 MUST 导致 strict mismatch

#### Scenario: Animation facts 可分层
- **WHEN** comparer 比较 animation facts
- **THEN** profile-driven playback facts MUST 能标记为 strict gameplay
- **AND** visual-only playback facts MUST 能标记为 presentation drift

### Requirement: 黑板不决定比较策略
黑板 MUST NOT 自行决定 F6/F8 是否失败。比较策略 MUST 由 rollback authority/scope resolver、state policy 或等价外部纯数据规则处理。黑板 MAY 提供 phase、alias、action key、normalized time 等事实供 resolver 判断。

#### Scenario: 黑板只提供事实
- **WHEN** comparer 需要判断某 animation fact 的 compare scope
- **THEN** 黑板 snapshot MAY 提供 phase、alias 和 action key
- **AND** 黑板 MUST NOT 持有 comparer 或 runner 实例

#### Scenario: Restore 不触发 scope 副作用
- **WHEN** 系统恢复黑板 snapshot
- **THEN** restore MUST 只恢复事实值
- **AND** MUST NOT 因 scope 分类播放动画、移动角色或切换状态

### Requirement: Action Facts 来自 Action Motion Resolver Result
角色运行时黑板 MUST 从状态机 frame 和 Action motion resolver result 写入 Action facts。黑板写入 MUST NOT 从状态输出解析层重新计算动作位移、完成状态或 run latch 派生。

#### Scenario: 写入动作位移事实
- **GIVEN** Action motion resolver 产出本帧动作运动结果
- **WHEN** FullBody 管线写入 runtime blackboard
- **THEN** Action facts MUST 使用 resolver result 中的 movement command、has movement、completed 和 source step
- **AND** MUST NOT 调用 `CharacterStateOutputResolver` 重算本帧距离

#### Scenario: 无动作规格写入空事实
- **GIVEN** 当前状态没有 action motion spec
- **WHEN** FullBody 管线写入 runtime blackboard
- **THEN** Action facts MUST 表示无 active action movement
- **AND** MUST NOT 使用上一帧 resolver result 伪造当前帧动作位移

### Requirement: Action Facts 保持纯数据
Action facts MUST 保持可复制纯数据，不得持有 motion executor、Transform、CharacterController、Animator、Animancer state、AnimationClip 或 UnityEngine.Object。

#### Scenario: 静态边界验证
- **WHEN** 检查 runtime blackboard 与 action facts 源码
- **THEN** 源码 MUST NOT 保存 Unity 场景实例引用
- **AND** MUST NOT 保存动画 runtime 对象
