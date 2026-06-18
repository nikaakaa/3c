# animation-phase-timeline-facts Specification

## Purpose
定义动画阶段播放进度、退出事实和 timeline facts 的纯数据边界，用于逻辑层判断阶段可退出性。
## Requirements
### Requirement: 动画阶段播放进度快照
系统 MUST 提供纯数据动画阶段播放进度快照，用于把动画播放层的当前播放进度传递给动画事实采样器。该快照 MUST NOT 持有 Animancer 运行时对象、AnimationClip、TransitionAsset、UnityEngine.Object、Transform、Animator 或场景实例引用。

#### Scenario: 快照承载当前播放进度
- **WHEN** 动画播放层正在播放某个基础移动 phase alias
- **THEN** 播放进度快照 MUST 能表达当前 phase、alias key、normalized time、是否有有效播放状态和是否已结束
- **AND** 该快照 MUST 是可复制的纯数据

#### Scenario: 无有效播放状态
- **WHEN** 动画播放层没有当前 Animancer state 或当前状态无法对应 phase
- **THEN** 播放进度快照 MUST 标记为无有效播放状态
- **AND** MUST NOT 用空对象、Unity 对象引用或默认 clip 伪造有效播放状态

#### Scenario: 不暴露 Animancer 对象
- **WHEN** 逻辑层或 sampler 读取播放进度快照
- **THEN** 它们 MUST NOT 能通过该快照访问 `AnimancerState`
- **AND** MUST NOT 能访问 `AnimationClip`
- **AND** MUST NOT 能访问 `TransitionAsset`

### Requirement: 动画阶段退出事实采样
系统 MUST 提供动画阶段退出事实采样器，根据 phase config、phaseTime 和播放进度快照产出 `CanExit`。采样器 MUST 是纯逻辑模块，不得读取 Animancer、Animator、AnimationClip、TransitionLibrary、场景对象或 Unity 时间单例。

#### Scenario: Manual 不可退出
- **GIVEN** 当前 phase config 的退出策略为 `Manual`
- **WHEN** sampler 采样退出事实
- **THEN** `CanExit` MUST 为 false

#### Scenario: AfterDuration 按阶段时间退出
- **GIVEN** 当前 phase config 的退出策略为 `AfterDuration`
- **AND** exit duration 为非负值
- **WHEN** phaseTime 小于 exit duration
- **THEN** `CanExit` MUST 为 false
- **WHEN** phaseTime 大于或等于 exit duration
- **THEN** `CanExit` MUST 为 true

#### Scenario: OnAnimationEnd 按播放结束退出
- **GIVEN** 当前 phase config 的退出策略为 `OnAnimationEnd`
- **WHEN** 播放进度快照有效且表示当前动画已结束
- **THEN** `CanExit` MUST 为 true
- **WHEN** 播放进度快照有效但当前动画未结束
- **THEN** `CanExit` MUST 为 false

#### Scenario: OnAnimationEnd 缺少播放进度
- **GIVEN** 当前 phase config 的退出策略为 `OnAnimationEnd`
- **WHEN** 播放进度快照无效
- **THEN** `CanExit` MUST 为 false
- **AND** sampler MUST NOT 猜测 clip 长度或自动退回 `AfterDuration`

### Requirement: Timeline Fact 扩展边界
系统 MUST 将 `CanExit` 作为未来 Timeline Fact 的第一项事实。后续 marker、window、事件、IK 和预测回滚 MUST 复用事实采样边界，而不是让动画播放层、状态机或编辑器各自创建独立判断路径。

#### Scenario: 当前变更只输出 CanExit
- **WHEN** 本变更实施完成
- **THEN** sampler MUST 至少输出 `CanExit`
- **AND** 本变更 MUST NOT 实现 attack cancel window、hitbox window、IK window、VFX/SFX event 或 camera event

#### Scenario: 未来编辑器只写数据
- **WHEN** 后续新增 Timeline 编辑器
- **THEN** 编辑器 MUST 写入 marker、window、event 或等价数据资产
- **AND** 运行时 MUST 继续通过 sampler 产出 facts
- **AND** 编辑器 MUST NOT 成为运行时状态切换的必需组件

#### Scenario: 不使用 Animancer OnEnd 作为逻辑权威
- **WHEN** 动画阶段需要自然结束后退出
- **THEN** 系统 MUST 通过播放进度快照和 sampler 产出 `CanExit`
- **AND** MUST NOT 让 Animancer `OnEnd` 直接调用基础 Locomotion 状态机切换

### Requirement: 动画事实校验和测试
系统 MUST 为动画阶段 Timeline Fact 提供自动测试、配置校验和静态边界验证，证明 sampler 行为确定且不污染逻辑层和播放层边界。

#### Scenario: 自动测试覆盖退出策略
- **WHEN** 运行动画阶段 Timeline Fact EditMode 测试
- **THEN** 测试 MUST 覆盖 `Manual`
- **AND** MUST 覆盖 `AfterDuration` 未到达和到达
- **AND** MUST 覆盖 `OnAnimationEnd` 无效播放进度、未结束和已结束

#### Scenario: 配置校验覆盖 OnAnimationEnd
- **WHEN** phase config 使用 `OnAnimationEnd`
- **THEN** 配置校验 MUST 不要求 exit duration 为正
- **AND** 仍 MUST 校验 alias key 非空

#### Scenario: 静态边界可验证
- **WHEN** 检查 sampler 源码
- **THEN** 静态搜索 MUST 能确认 sampler 不引用 Animancer
- **AND** MUST 能确认 sampler 不引用 `AnimationClip`
- **AND** MUST 能确认 sampler 不引用 `TransitionLibrary`

### Requirement: Action 动画播放进度事实
系统 MUST 提供纯数据 Action 动画播放进度事实，用于把动作动画外观层的当前播放进度传递给逻辑状态机和未来 Timeline Fact sampler。该事实 MUST NOT 持有 Animancer runtime 对象、AnimationClip、TransitionAsset、UnityEngine.Object、Transform、Animator 或场景实例引用。

#### Scenario: 动作进度事实承载当前播放
- **WHEN** 动作动画外观层正在播放 `Action.Dodge.Backstep` 或等价动作动画 key
- **THEN** 动作播放进度事实 MUST 能表达 action key、normalized time、是否有有效播放状态和是否已结束
- **AND** 该事实 MUST 是可复制的纯数据

#### Scenario: 动作进度无有效播放
- **WHEN** 动作动画外观层没有当前动作播放状态
- **THEN** 动作播放进度事实 MUST 标记为无有效播放状态
- **AND** MUST NOT 用空对象、Unity 对象引用或默认 clip 伪造有效播放状态

#### Scenario: 不暴露动作播放层对象
- **WHEN** 逻辑层、状态机条件或 sampler 读取动作播放进度事实
- **THEN** 它们 MUST NOT 能通过该事实访问 `AnimancerState`
- **AND** MUST NOT 能访问 `AnimationClip`
- **AND** MUST NOT 能访问 `TransitionAsset`

### Requirement: Action 恢复退出事实
系统 MUST 能从 Action 动画播放进度事实产生动作恢复退出事实，使动作状态可以等待表现恢复完成后退出。第一版 MUST 至少支持按动作动画播放结束判断 `ActionCanExit`，并且 MUST 保持动作位移时长和动作恢复退出时机分离。

#### Scenario: Backstep 播放未结束不可退出
- **GIVEN** 当前逻辑状态为 `Action.Dodge`
- **AND** 当前变体为 `Backstep`
- **AND** 动作播放进度事实匹配 `Action.Dodge.Backstep`
- **WHEN** 动作播放进度有效但尚未结束
- **THEN** Action 恢复退出事实 MUST 为 false

#### Scenario: Backstep 播放结束可以退出
- **GIVEN** 当前逻辑状态为 `Action.Dodge`
- **AND** 当前变体为 `Backstep`
- **AND** 动作播放进度事实匹配 `Action.Dodge.Backstep`
- **WHEN** 动作播放进度有效且已结束
- **THEN** Action 恢复退出事实 MUST 为 true

#### Scenario: 缺少动作播放进度不可猜测
- **GIVEN** 当前逻辑状态为 `Action.Dodge`
- **AND** 当前变体为 `Backstep`
- **WHEN** 动作播放进度事实无效
- **THEN** Action 恢复退出事实 MUST 为 false
- **AND** sampler 或状态机条件 MUST NOT 猜测 clip 长度
- **AND** MUST NOT 自动退回动作位移 duration 作为恢复退出事实

#### Scenario: 不使用 Animancer OnEnd 作为逻辑权威
- **WHEN** 动作状态需要等待恢复完成后退出
- **THEN** 系统 MUST 通过动作播放进度事实和状态机条件判断是否可退出
- **AND** MUST NOT 让 Animancer `OnEnd` 或等价回调直接调用状态图切换

### Requirement: 动作打断窗口的未来归属
系统 MUST 将动作恢复、移动取消、Dodge 取消、攻击取消或等价动作打断窗口归属到未来 Action Timeline/window 数据和 sampler，而不是让动画外观层、状态机 evaluator 或 MonoBehaviour 各自硬编码一套窗口规则。本变更只允许 Backstep 恢复段的移动输入提前回移动阶段，不实现完整通用 Timeline 编辑器。

#### Scenario: 未来 Timeline 配置可打断窗口
- **WHEN** 后续新增 Action Timeline 或窗口配置
- **THEN** 设计者 MUST 能用数据表达某个动作动画或动作变体在哪些时间段允许被移动、Dodge、Attack 或等价请求打断
- **AND** 运行时 MUST 通过 sampler 将这些窗口转换为纯数据 facts
- **AND** Locomotion 状态图或 Action 仲裁器 MUST 读取这些 facts，而不是直接读取 Animancer runtime

#### Scenario: 本变更不实现完整窗口表
- **WHEN** 本变更实施完成
- **THEN** 系统 MUST NOT 新增完整 Timeline 编辑器
- **AND** MUST NOT 新增 hitbox、cancel、IK、VFX、SFX 或 camera 事件轨道
- **AND** MUST NOT 新增绕过 Action/Locomotion runtime的动作打断路径

### Requirement: Locomotion 脚相位 Timeline Fact
系统 MUST 将 Locomotion 脚相位作为动画 timeline facts 的扩展项。脚相位 fact MUST 从播放进度快照和脚相位 Profile 采样得到，并保持纯数据边界。

#### Scenario: 播放进度采样脚相位
- **GIVEN** 播放进度快照有效
- **AND** 当前 phase/gait/alias 存在有效脚相位 Profile
- **WHEN** timeline facts sampler 采样当前 normalized time
- **THEN** 输出 MUST 包含当前 locomotion foot phase fact

#### Scenario: 缺少 Profile 不猜测
- **GIVEN** 播放进度快照有效
- **AND** 当前 phase/gait/alias 没有有效脚相位 Profile
- **WHEN** timeline facts sampler 尝试采样脚相位
- **THEN** 输出 MUST 标记脚相位无效
- **AND** MUST NOT 根据 alias 名称或 normalized time 猜测左右脚

#### Scenario: Timeline fact 保持纯数据
- **WHEN** 逻辑层或黑板读取脚相位 timeline fact
- **THEN** 它们 MUST NOT 能通过该 fact 访问 Animancer state
- **AND** MUST NOT 能访问 AnimationClip、TransitionAsset 或 Unity 场景实例

### Requirement: TurnBack 退出脚相位 Fact
系统 MUST 能在 TurnBack 退出窗口采样并保留退出脚相位 fact，使下一段 RunLoop 可以进行相位匹配。该 fact MUST 不改变 TurnBack 的进入条件、退出条件或运动权威。

#### Scenario: TurnBack 可退出时采样退出脚相位
- **GIVEN** 当前 phase 为 `TurnBack`
- **AND** 当前播放进度达到 TurnBack 可退出窗口
- **AND** 当前脚相位 sample 有效
- **WHEN** 系统准备进入 `MoveLoop + Run`
- **THEN** timeline facts MUST 提供 TurnBack exit foot phase fact

#### Scenario: TurnBack 退出条件不由脚相位决定
- **GIVEN** 当前 phase 为 `TurnBack`
- **AND** 当前脚相位 sample 为 `LeftPlant` 或 `RightPlant`
- **WHEN** 状态机评估 TurnBack 是否可退出
- **THEN** 是否可退出 MUST 仍由现有 TurnBack exit policy、timeline window 或 StateCanExit 事实决定
- **AND** 脚相位 fact MUST NOT 单独允许或拒绝 TurnBack 退出

#### Scenario: 非 TurnBack 不覆盖退出脚相位
- **GIVEN** 当前 phase 不是 `TurnBack`
- **WHEN** timeline facts sampler 采样当前脚相位
- **THEN** sampler MUST NOT 把该 sample 作为 TurnBack exit foot phase 写入

### Requirement: 脚相位 Timeline Fact 测试
系统 MUST 为脚相位 timeline facts 提供自动测试，证明采样、无效输入和 TurnBack 退出 fact 行为确定。

#### Scenario: 自动测试覆盖有效采样
- **WHEN** 运行 animation timeline facts EditMode 测试
- **THEN** 测试 MUST 覆盖有效 profile 和播放进度产出当前 foot phase

#### Scenario: 自动测试覆盖无效输入
- **WHEN** 运行 animation timeline facts EditMode 测试
- **THEN** 测试 MUST 覆盖缺少 profile 时不猜测脚相位

#### Scenario: 自动测试覆盖 TurnBack exit fact
- **WHEN** 运行 animation timeline facts EditMode 测试
- **THEN** 测试 MUST 覆盖 TurnBack 可退出时产出 exit foot phase
- **AND** MUST 覆盖非 TurnBack 不覆盖 exit foot phase

### Requirement: 播放进度只作为 Timeline Facts 采样输入
动画播放进度事实 MUST 只作为 timeline facts 采样输入。Action request submission / interrupt arbitration、transition evaluator 和 output resolver MUST 消费采样后的 timeline facts，而不得分别读取播放进度并重算窗口。

#### Scenario: 播放进度集中采样
- **GIVEN** 动画外观层已经写入 Locomotion 或 Action 播放进度事实
- **WHEN** Character frame context 准备 current timeline facts
- **THEN** timeline sampler MUST 读取播放进度事实并产出 current timeline facts
- **AND** 后续请求准入和 transition 判断 MUST NOT 再自行读取播放进度重算同一窗口

#### Scenario: 无播放进度不猜测窗口
- **GIVEN** 当前状态 timeline policy 需要 normalized time
- **AND** 播放进度事实无效
- **WHEN** sampler 生成 current timeline facts
- **THEN** normalized-time 窗口 MUST 不活跃
- **AND** sampler MUST NOT 猜测 clip length、fade duration 或默认 normalized time

### Requirement: 通用状态 Timeline Window Facts
系统 MUST 将状态 timeline policy 和当前状态播放/计时进度采样为通用 window facts。facts MUST 表达 state id、normalized time、elapsed seconds、活跃窗口、稳定 fact id 和窗口携带的 priority/resistance 信息，并 MUST 保持纯数据边界。

#### Scenario: TurnBack motion window facts
- **GIVEN** 当前状态为 `Locomotion.TurnBack`
- **AND** 当前 normalized time 位于 motion window 内
- **WHEN** timeline sampler 采样
- **THEN** window facts MUST 标记 motion window active
- **AND** 输出 MUST 能被 TurnBack baked motion 采样器消费

#### Scenario: 输入锁窗口 facts
- **GIVEN** 当前状态位于 input lock window 内
- **WHEN** timeline sampler 采样
- **THEN** window facts MUST 标记 input lock active
- **AND** 运动输出层 MUST 能据此抑制普通输入旋转和平面位移

#### Scenario: 取消窗口 facts
- **GIVEN** 当前状态位于 interrupt/cancel window 内
- **WHEN** timeline sampler 采样
- **THEN** window facts MUST 标记对应 request kind 在当前窗口可被仲裁
- **AND** MUST 输出 `CancelableToDodge`、`ComboInputOpen` 或等价 typed fact id
- **AND** 仲裁入口 MUST 能读取该事实，而不是直接读取播放层对象

#### Scenario: typed facts 可枚举
- **WHEN** 诊断、测试或未来编辑器读取 active facts
- **THEN** 系统 MUST 能枚举当前 active fact id
- **AND** MUST 能区分 input lock、motion、natural exit、cancel、combo 等事实语义

### Requirement: Timeline Facts 不拥有业务裁决
timeline sampler MUST 只负责把配置和进度转换为 facts，不得直接切换状态、接受请求、播放动画或提交位移。priority、resistance、force 和 request 选择 MUST 由状态请求仲裁入口处理。

#### Scenario: sampler 不接受 Dodge 请求
- **GIVEN** 当前 window facts 表示 Dodge cancel window active
- **WHEN** sampler 输出 facts
- **THEN** sampler MUST NOT 生成 accepted Dodge request
- **AND** MUST NOT 调用状态机切换 API

#### Scenario: sampler 不提交 TurnBack 位移
- **GIVEN** 当前 TurnBack motion window active
- **WHEN** sampler 输出 facts
- **THEN** sampler MUST NOT 调用 motion executor
- **AND** MUST NOT 写 Transform

### Requirement: Timeline Facts 可测试和可诊断
系统 MUST 为通用 timeline window facts 提供自动测试和诊断输出，证明窗口命中、边界值、播放进度缺失和非法配置都可追踪。

#### Scenario: 自动测试覆盖窗口边界
- **WHEN** 运行 timeline facts EditMode 测试
- **THEN** 测试 MUST 覆盖窗口前、窗口起点、窗口中、窗口终点和窗口后

#### Scenario: 诊断日志显示窗口状态
- **WHEN** 诊断开关启用且当前状态存在 timeline policy
- **THEN** 日志 MUST 能显示 state id、normalized time、elapsed seconds 和当前 active windows

### Requirement: 动画驱动采样窗口可选择进入回滚权威
系统 MAY 让纯表现动画播放进度保持 Presentation Layer 非确定状态，不要求捕获 normalized time。系统 MUST 将会影响 simulation 输出的动画驱动采样窗口视为可回滚纯数据状态。对于声明使用 `TickSampledMotion`、root motion profile、Motion Warping playback window 或等价 profile-driven 输出的状态/动作，phase、alias key、current normalized time、previous sampled normalized time 和采样有效性 MUST 能通过 snapshot capture/restore 或确定性状态重建，且 MUST NOT 依赖 Animancer runtime object、Animator、AnimationClip、TransitionAsset、Unity frame time 或场景实例引用作为 replay 权威。

#### Scenario: Profile-driven 状态恢复采样窗口
- **GIVEN** 某状态声明使用 profile-driven motion
- **AND** 该状态的 profile delta 或 yaw 由动画 normalized window 采样
- **AND** replay 从该状态中段 tick 恢复
- **WHEN** 下一 tick 使用同一输入推进
- **THEN** sampler MUST 使用恢复后的 current normalized time 和 previous sampled normalized time
- **AND** MUST NOT 把恢复后的中段状态当作新播放段从 0 重新采样

#### Scenario: 首次进入仍从起始进度开始
- **GIVEN** 逻辑状态机真实进入一个新的 profile-driven 状态
- **WHEN** 该状态声明 start normalized time
- **THEN** 采样播放窗口 MUST 从该 start normalized time 开始
- **AND** previous sampled normalized time MUST 按新播放段规则初始化

#### Scenario: 表现层不是回滚权威
- **GIVEN** Animancer 或 Animator 当前视觉播放状态与 snapshot 中的 sampled motion playback window 不同
- **WHEN** replay 恢复并推进需要 profile-driven motion 的状态
- **THEN** simulation MUST 以 snapshot/纯数据 playback state 作为采样权威
- **AND** 表现层 MAY seek 到该进度，但 MUST NOT 反向覆盖 simulation playback state

#### Scenario: 纯表现动画不要求回滚权威
- **GIVEN** 某动画只用于视觉播放、blend、表情、上身表现或 VFX 节奏
- **AND** 该动画播放进度未被声明为 motion facts、warp window、hit/cancel window 或等价 simulation 输出的输入
- **WHEN** rollback replay 恢复角色状态
- **THEN** 系统 MAY 不捕获该动画 normalized time
- **AND** 该动画播放进度 MUST NOT 反向影响 simulation snapshot、motion facts 或 runtime blackboard 权威事实

### Requirement: 采样窗口恢复诊断
系统 MUST 在本地 rollback 诊断中能定位 sampled motion playback window 或 profile sampling window 分叉。字段级 differences 或相关诊断日志 MUST 能区分 current normalized time 差异、previous sampling window 差异、phase/alias 差异和最终 position/yaw 差异。

#### Scenario: Current normalized time 分叉
- **GIVEN** replay 后 current normalized time 与历史快照不同
- **WHEN** 本地 synctest 输出 first mismatch
- **THEN** differences MUST 标记 animation normalized time 或 runtime blackboard animation progress
- **AND** 日志 MUST 能看到 expected/actual 的 phase、alias 和 normalized time

#### Scenario: Sampling window 分叉
- **GIVEN** replay 的 current normalized time 相同但 previous sampled normalized time 不同
- **WHEN** profile delta 或 yaw 因窗口不同而分叉
- **THEN** 诊断 MUST 能输出或推导 previous/current window
- **AND** 不得只输出笼统的 snapshot mismatch

