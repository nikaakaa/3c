# animation-phase-timeline-facts Specification

## Purpose
TBD - created by archiving change add-animation-phase-timeline-facts. Update Purpose after archive.
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

