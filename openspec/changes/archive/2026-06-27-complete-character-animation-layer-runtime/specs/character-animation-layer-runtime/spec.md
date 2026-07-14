# character-animation-layer-runtime Specification Delta

## ADDED Requirements

### Requirement: 动画贡献是动画层唯一输入合同
系统 MUST 使用统一的动画贡献数据作为角色动画层的唯一输入。Timeline、状态行为、Tree、Action 或后续其它来源如果需要影响角色动画，MUST 写入同一种动画贡献集合。系统 MUST NOT 让任意来源绕过动画层直接写入 Animator、Animancer、TimelinePlayer 或 PlayableGraph。

#### Scenario: Timeline 输出动画
- **WHEN** active Timeline 的 AnimationTrack 采样到有效 clip
- **THEN** 轨道 MUST 写入动画贡献
- **AND** 贡献 MUST 至少包含来源身份、clip、layer id、priority、clip time 和 weight
- **AND** 轨道 MUST NOT 直接播放该 clip

#### Scenario: 状态行为输出动画
- **WHEN** Idle、Move、Attack 或 Hit 状态行为需要播放动画
- **THEN** 状态行为 MUST 通过正式节点、模块或 Timeline 写入动画贡献
- **AND** 状态行为 MUST NOT 直接调用 Animator 或 Animancer 播放动画

### Requirement: 动画层定义来自管线定义
系统 MUST 使用 `CharacterPipelineDefinition` 或等价正式管线定义作为角色动画层的唯一正式定义来源。该来源 MUST 明确每个 layer 的身份、排序、mask、blend mode 约束和是否可被 Animancer 应用。系统 MUST NOT 同时让旧 SO/config、Timeline track 或节点各自保存互相冲突的 layer 真数据。

#### Scenario: Timeline 引用 layer id
- **WHEN** `CharacterPipelineDefinition` 持有动画层表
- **THEN** Timeline track 和节点 MUST 引用该层表中的 layer id
- **AND** track 或节点 MUST NOT 重新定义该 layer 的固定 mask 和 additive 语义

#### Scenario: 缺失 layer
- **WHEN** 动画贡献引用了管线定义中不存在的 layer id
- **THEN** 动画层运行时 MUST 报告配置错误
- **AND** 该贡献 MUST NOT 进入最终播放计划

### Requirement: 动画层运行时负责仲裁
系统 MUST 使用角色动画层运行时合并本帧所有动画贡献并生成最终播放计划。仲裁 MUST 至少处理 layer 分组、非法 layer、priority、override 权重归一、additive 贡献保留和 snapshot 输出。Animancer adapter MUST NOT 承担这些业务仲裁。

#### Scenario: 同一 layer 有多个 override 贡献
- **WHEN** 同一帧同一 layer 存在多个 override 贡献
- **THEN** 动画层运行时 MUST 选择最高 priority 组
- **AND** 同 priority 的 override 贡献总权重大于 1 时 MUST 归一化
- **AND** 低 priority override 贡献 MUST 不进入最终播放计划

#### Scenario: 同一 layer 有 additive 贡献
- **WHEN** 同一帧同一 layer 存在 additive 贡献
- **THEN** 动画层运行时 MUST 保留合法 additive 贡献
- **AND** additive 贡献 MUST 与该 layer 的 additive/mask 约束一致

### Requirement: Animancer 只是最终播放 adapter
系统 MUST 将 Animancer 限定为角色动画层的最终 Unity adapter。`AnimancerAnimationPresenter` MUST 只消费动画层播放计划，并按计划设置 Animancer layer、state、time、weight、mask 和 additive。它 MUST NOT 决定动作状态、transition、打断、Idle fallback 或 Timeline 播放时间。

#### Scenario: 应用播放计划
- **WHEN** 动画层运行时生成播放计划
- **THEN** Animancer adapter MUST 为计划中的 clip 创建或复用 Animancer state
- **AND** adapter MUST 根据计划设置 state time、speed 和 weight
- **AND** adapter MUST 根据计划设置 layer mask、additive 和 layer weight

#### Scenario: 计划为空
- **WHEN** 本帧动画层没有任何播放计划
- **THEN** Animancer adapter MAY 停止或静音自己管理的 state
- **AND** adapter MUST NOT 自动播放隐藏 Idle、默认 clip 或 fallback controller state

### Requirement: 基础姿态必须由正式来源输出
系统 MUST 要求 base pose、Idle、Move 或其它基础动画由正式 Graph、State、Timeline 或 Action 来源输出动画贡献。系统 MUST NOT 在 presenter、pipeline host 或 Animancer adapter 内置隐藏基础姿态 fallback。

#### Scenario: Idle 状态
- **WHEN** 当前状态是 Idle
- **THEN** Idle 状态行为 MUST 通过 TimelineNode、动画贡献节点或后续正式 Action 来源输出 base layer contribution
- **AND** base layer contribution MUST 进入同一个动画层运行时仲裁

#### Scenario: 配置缺失
- **WHEN** 当前 Graph 没有任何来源输出 base layer contribution
- **THEN** 系统 MUST 暴露为空动画输出或明确配置错误
- **AND** 系统 MUST NOT 自动回退到旧 locomotion、Animator controller 或隐藏 Idle clip

### Requirement: 角色管线不依赖旧动画播放路径
系统 MUST 保持角色管线运行链路只有一条动画播放路径：动画来源写贡献，动画层运行时仲裁，Animancer adapter 应用。角色管线 MUST NOT 读取旧 `AnimationPresentationPolicySO`、旧 locomotion/action SO、旧 bodyclaim policy，MUST NOT 依赖 `TimelinePlayer` autonomous playback。

#### Scenario: 搜索旧直接播放入口
- **WHEN** 实现阶段发现角色管线运行路径仍引用 `Animator.Play`、`Animator.CrossFade`、`TimelinePlayer` autonomous playback 或旧动画策略 SO
- **THEN** 该引用 MUST 删除、迁移到正式动画层，或明确隔离为 BTSMTL 编辑器预览路径
- **AND** 系统 MUST NOT 保留兼容分支让旧路径继续驱动角色动画

#### Scenario: BTSMTL 编辑器预览保留
- **WHEN** BTSMTL Timeline 编辑器预览仍需要 `TimelinePlayer` 或 PlayableGraph
- **THEN** 该代码 MAY 保留在 BTSMTL 编辑器或参考预览边界
- **AND** `CharacterPipelineHost`、`CharacterPipeline`、`TimelinePlaybackScheduler` 和 `CharacterPresentationStage` MUST NOT 依赖该预览路径
