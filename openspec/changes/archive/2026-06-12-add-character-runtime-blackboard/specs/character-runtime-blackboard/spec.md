## ADDED Requirements
### Requirement: Typed 角色运行时黑板
系统 SHALL 提供一个 typed 角色运行时黑板，用于集中承载角色运行时纯数据 facts。黑板 SHALL NOT 使用任意字符串 key 到 `object` 的通用字典作为核心模型。

#### Scenario: 黑板保存纯数据 facts
- **WHEN** 系统创建角色运行时黑板
- **THEN** 黑板 MUST 能保存 locomotion、action 和 animation 相关的纯数据 facts
- **AND** 黑板 facts MUST 可通过强类型字段或强类型 snapshot 读取
- **AND** 黑板核心模型 MUST NOT 暴露 `object` 值字典作为主要读写接口

#### Scenario: 黑板不是第二状态机
- **WHEN** 统一状态机 runner 推进状态
- **THEN** 黑板 MUST NOT 自行执行状态转移
- **AND** 黑板 MUST NOT 保存一套独立于统一状态机的 active state path 作为状态权威
- **AND** 统一状态机 snapshot 仍 MUST 是逻辑状态权威

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
系统 SHALL 允许统一状态机 context 读取黑板 snapshot 中的纯数据 facts，用于后续方向起步、脚步相位、转身和转角等条件判断。状态机 runner 自身 SHALL NOT 成为黑板字段维护器。

#### Scenario: Context 承载黑板 snapshot
- **WHEN** `PlayerFullBodyActionController` 或 locomotion runtime 组装 `CharacterStateMachineContext`
- **THEN** context MAY 携带黑板 snapshot 或等价只读 facts view
- **AND** transition evaluator MUST 只读取该只读 facts view
- **AND** evaluator MUST NOT 读取黑板可变实例

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
系统 SHALL 为角色运行时黑板提供自动测试和运行时诊断路径，证明黑板不会破坏统一状态机、动画表现层和运动执行端口的既有边界。

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
