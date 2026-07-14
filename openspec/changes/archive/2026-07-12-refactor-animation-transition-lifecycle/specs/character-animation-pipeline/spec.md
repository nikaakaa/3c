## MODIFIED Requirements

### Requirement: 动画混合模型是运行时核心

系统 MUST 使用统一动画贡献 Registry、`CharacterAnimationTransitionRuntime`、`CharacterAnimationLayerRuntime` 和 `CharacterPresentationStage` 组成动画混合模型。Registry MUST 只表达 playback、contribution、owner membership 和 producer lifecycle；TransitionRuntime MUST 表达 animation transition identity、target readiness、strategy、capture、progress、supersede 和 retirement；LayerRuntime MUST 表达层、权重、优先级和最终层结果；PresentationStage MUST 组织 visual time、transition、layer arbitration 和最终 adapter 的单一表现批次。任意 producer MUST NOT 绕过该模型应用最终动画。

#### Scenario: 多来源贡献同一动画层

- **WHEN** Registry 与 TransitionRuntime 向同一动画层提供多个有效 contributions
- **THEN** PresentationStage MUST 将普通、target 与 transition-owned contributions 汇总为同一个表现批次
- **AND** LayerRuntime MUST 按优先级从高到低分配该层 `0..1` 的覆盖权重
- **AND** 高优先级未占满的剩余权重 MUST 由后续低优先级贡献填充
- **AND** 同优先级贡献总权重超过剩余权重时 MUST 在组内归一化

#### Scenario: Timeline 和状态行为同时提交动画

- **WHEN** active State 行为和 Timeline 轨道同时提交动画贡献
- **THEN** 它们 MUST 进入同一 Registry、TransitionRuntime 和 LayerRuntime 链路
- **AND** 任意一方 MUST NOT 直接写 Animator、Animancer 或 PlayableGraph

#### Scenario: Producer 当帧未提交

- **WHEN** producer 在某个表现帧没有新的 Sample
- **THEN** Registry MUST 依据显式 lifecycle 保留或释放其 membership
- **AND** LayerRuntime MUST NOT 根据 transient submission list 推断 producer 已释放

### Requirement: CharacterPresentationStage 是 Unity 动画应用边界

系统 MUST 让 `CharacterPresentationStage` 或其下属正式 adapter 成为最终写入 Animator、Animancer PlayableGraph 和 Unity animation output job 的边界。PresentationStage MUST 在同一表现批次中先合并 target sample 与 TargetReady，再推进 TransitionRuntime、LayerRuntime 和最终 adapter。Timeline 轨道、TimelineNode、StateMachine runtime、Registry 和 TransitionRuntime MUST NOT 绕过该边界直接应用最终动画。

#### Scenario: 应用动画混合结果

- **WHEN** presentation frame 生成本帧普通 contribution 和 transition strategy 输出
- **THEN** PresentationStage MUST 统一生成最终 layer playback plan
- **AND** adapter MUST 只消费该正式结果
- **AND** 其它 stage MUST NOT 写入同一个最终动画状态

#### Scenario: target 首帧与 handoff 同批处理

- **WHEN** target State 首次提交 TargetReady 与 animation Sample
- **THEN** PresentationStage MUST 在同一 batch 中先注册 Sample 再启动 transition capture
- **AND** source release 与 target first sample 之间 MUST NOT 产生生命周期空计划帧

#### Scenario: 表现 adapter 应用动画计划

- **WHEN** adapter 应用最终 layer plan 与 inertialization job 数据
- **THEN** adapter MUST NOT 自主创建 Timeline playback、owner lifecycle 或 transition strategy
- **AND** adapter MUST NOT根据 clip 缺失选择 fallback 动画

### Requirement: 状态切换动画混合必须由表现层消费正式切换事实

系统 MUST 通过正式 `AnimationTransitionRequest` 表达状态切换动画事实，并由 `CharacterAnimationTransitionRuntime` 和 `CharacterPresentationStage` 消费。StateMachine runtime 和 Timeline scheduler MUST NOT 直接写 Animator、Animancer 或 PlayableGraph；Animancer adapter MUST NOT 自行决定 strategy、duration、curve 或 transition completion。

#### Scenario: 状态机发生带策略的切换

- **WHEN** `StateMachineGraphRuntime` 命中带 AnimationTransitionDefinition 的 edge
- **THEN** runtime MUST 发布 transition instance identity、source、target、strategy、duration、curve 和 cause
- **AND** TransitionRuntime MUST 创建独立表现生命周期
- **AND** PresentationStage MUST 推进该生命周期并应用策略结果

#### Scenario: Immediate 切换

- **WHEN** 命中 strategy 为 Immediate 的 edge
- **THEN** TransitionRuntime MUST 在同一表现批次原子接受 target 并释放 source
- **AND** 系统 MUST NOT 通过隐藏 CrossFade 或 fallback clip 伪造混合

#### Scenario: Inertialization 切换

- **WHEN** 命中 strategy 为 Inertialization 的 edge
- **THEN** TransitionRuntime MUST 捕获当前最终 visual pose 与 velocity
- **AND** source State、Timeline 和 Action MUST NOT 为该表现切换继续 tick
- **AND** output job MUST 对新 target pose 应用衰减偏移

### Requirement: StateMachine transition 必须提交动画 owner handoff

系统 MUST 让 StateMachine runtime 为每次 State activation 提供稳定 owner scope，并为每次逻辑 transition 提交正式 `AnimationTransitionRequest`。Request MUST 与 condition evaluation 分离，MUST 携带 transition instance identity、runtime activation scope、source owner、target owner 或 Empty、definition 和 cause。TransitionRuntime MUST 保持 request 为 WaitingTarget，直到 target activation 至少实际执行一次并提交 TargetReady。TargetReady MUST 表示正式执行机会，MUST NOT 要求 target 存在动画 contribution。

#### Scenario: Target state body 尚未执行

- **WHEN** source State 已完成逻辑 transition 并停止 tick
- **AND** target State 尚未提交 TargetReady
- **THEN** TransitionRuntime MUST 保持 request 为 WaitingTarget
- **AND** source 的最后合法视觉输入 MUST 在 capture 前保持可用
- **AND** Registry MUST NOT 自己启动或推进 blend session

#### Scenario: Target state body 首次执行

- **WHEN** target State 的 OnEnter 或 Root graph 首次被正式 tick
- **THEN** StateMachine runtime MUST 提交 TargetReady
- **AND** 同 Tick Timeline request MUST 可由 Scheduler 接管并在表现帧提交 target Sample
- **AND** PresentationStage MUST 在统一 batch 中处理 target Sample 与 waiting request

#### Scenario: target 没有动画 contribution

- **WHEN** target 已 Ready
- **AND** target owner 没有合法 animation contribution
- **THEN** transition target MUST 是真实 Empty
- **AND** 系统 MUST NOT 隐式保留 source、播放 Idle 或调用 adapter fallback

#### Scenario: 并行 Locomotion 和 Action 状态机

- **WHEN** Locomotion 与 Action StateMachine 各自发生 transition
- **THEN** 两者 MUST 使用不同 runtime activation scopes
- **AND** 两个 scope MAY 各自拥有一个 active animation transition
- **AND** 任一 transition MUST NOT release 或重启另一 scope 的 contributions

## ADDED Requirements

### Requirement: 动画 Transition 必须拥有独立可重入生命周期

系统 MUST 为每个 animation transition instance 提供 `Requested`、`WaitingTarget`、`Capturing`、`Running`、`Completed` 和 `Retired` 生命周期，并记录 `Superseded` 终止结果。生命周期 MUST 由表现帧 delta 推进，MUST NOT 由 logic tick、Timeline logic time 或 Animancer `Evaluate(0)` 隐式推进。

#### Scenario: 普通 Transition 完成

- **WHEN** transition capture 完成且 elapsed 达到 definition duration
- **THEN** instance MUST 进入 Completed
- **AND** source snapshot 与 strategy native data MUST 在 retirement 边界释放
- **AND** target contributions MUST 继续按自身 owner lifecycle 存在

#### Scenario: Transition 运行中再次切换

- **WHEN** 同一 runtime activation scope 收到新的 transition request
- **THEN** 旧 instance MUST 记录 Superseded 和替代者 identity
- **AND** 新 instance MUST 从当前最终视觉结果重新 capture
- **AND** 系统 MUST NOT 构造无限 transition stack

#### Scenario: 不同 StateMachine 并行

- **WHEN** Locomotion 和 Action scope 同时存在 active transition
- **THEN** TransitionRuntime MUST 分别推进它们
- **AND** 两者结果 MUST 在统一 LayerRuntime 中仲裁
