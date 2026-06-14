## ADDED Requirements
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
- **GIVEN** 当前逻辑状态为 `FullBody/Action/Dodge`
- **AND** 当前变体为 `Backstep`
- **AND** 动作播放进度事实匹配 `Action.Dodge.Backstep`
- **WHEN** 动作播放进度有效但尚未结束
- **THEN** Action 恢复退出事实 MUST 为 false

#### Scenario: Backstep 播放结束可以退出
- **GIVEN** 当前逻辑状态为 `FullBody/Action/Dodge`
- **AND** 当前变体为 `Backstep`
- **AND** 动作播放进度事实匹配 `Action.Dodge.Backstep`
- **WHEN** 动作播放进度有效且已结束
- **THEN** Action 恢复退出事实 MUST 为 true

#### Scenario: 缺少动作播放进度不可猜测
- **GIVEN** 当前逻辑状态为 `FullBody/Action/Dodge`
- **AND** 当前变体为 `Backstep`
- **WHEN** 动作播放进度事实无效
- **THEN** Action 恢复退出事实 MUST 为 false
- **AND** sampler 或状态机条件 MUST NOT 猜测 clip 长度
- **AND** MUST NOT 自动退回动作位移 duration 作为恢复退出事实

#### Scenario: 不使用 Animancer OnEnd 作为逻辑权威
- **WHEN** 动作状态需要等待恢复完成后退出
- **THEN** 系统 MUST 通过动作播放进度事实和状态机条件判断是否可退出
- **AND** MUST NOT 让 Animancer `OnEnd` 或等价回调直接调用统一状态机切换

### Requirement: 动作打断窗口的未来归属
系统 MUST 将动作恢复、移动取消、Dodge 取消、攻击取消或等价动作打断窗口归属到未来 Action Timeline/window 数据和 sampler，而不是让动画外观层、状态机 evaluator 或 MonoBehaviour 各自硬编码一套窗口规则。本变更只允许 Backstep 恢复段的移动输入提前回移动阶段，不实现完整通用 Timeline 编辑器。

#### Scenario: 未来 Timeline 配置可打断窗口
- **WHEN** 后续新增 Action Timeline 或窗口配置
- **THEN** 设计者 MUST 能用数据表达某个动作动画或动作变体在哪些时间段允许被移动、Dodge、Attack 或等价请求打断
- **AND** 运行时 MUST 通过 sampler 将这些窗口转换为纯数据 facts
- **AND** 统一状态机或 Action 仲裁器 MUST 读取这些 facts，而不是直接读取 Animancer runtime

#### Scenario: 本变更不实现完整窗口表
- **WHEN** 本变更实施完成
- **THEN** 系统 MUST NOT 新增完整 Timeline 编辑器
- **AND** MUST NOT 新增 hitbox、cancel、IK、VFX、SFX 或 camera 事件轨道
- **AND** MUST NOT 新增绕过统一状态机的动作打断路径
