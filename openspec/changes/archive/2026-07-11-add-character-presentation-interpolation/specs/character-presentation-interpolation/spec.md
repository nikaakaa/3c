# character-presentation-interpolation Specification

## ADDED Requirements

### Requirement: 角色表现插值必须基于 logic sample 历史

系统 MUST 为角色表现层保存最近的 logic sample 历史。logic sample MUST 来自正式 `CharacterPipeline` logic tick 结果，至少包含 local logic tick、logic pose 和该 tick 的 animation playback sample。PresentationFrame MUST 使用最近 logic samples 和 `GameplayPresentationFrameContext.InterpolationAlpha` 生成表现结果。系统 MUST NOT 在 PresentationFrame 重新 tick BTSMTL、Timeline、MotionResolver 或 ActionRuntime。

#### Scenario: 渲染帧高于 logic tick

- **WHEN** 当前 render frame 没有新的 `LocalLogicTick`
- **THEN** `CharacterPipeline` MUST 仍然调用 PresentationFrame
- **AND** 表现层 MUST 使用最近保存的 logic samples 和 interpolation alpha 生成 visual 输出
- **AND** 表现层 MUST NOT 因没有新 logic tick 而重新推进 Timeline

#### Scenario: 首个 logic sample

- **WHEN** 角色刚激活且只有一个 logic sample
- **THEN** 表现层 MUST 将 visual pose 和 visual animation 对齐到该 sample
- **AND** 系统 MUST NOT 生成隐藏 Idle、隐藏动画 fallback 或额外 motion fact

### Requirement: Motion visual pose 必须和逻辑 Transform 分离

系统 MUST 区分 logic root 和 visual root。`CharacterController` 或等价 logic root MUST 继续表达碰撞、判定、网络预测和 motion correction 的逻辑真值。表现插值 MUST 只应用到显式配置的 visual root / model root。PresentationFrame MUST NOT 调用 `CharacterController.Move`，MUST NOT 反写 logic root position/rotation，MUST NOT 修改 `MotionResult`。

#### Scenario: 本地 motion 插值

- **WHEN** previous logic sample 和 current logic sample 都有有效 logic pose
- **THEN** PresentationFrame MUST 使用 interpolation alpha 计算 visual position 和 visual rotation
- **AND** 计算结果 MUST 应用到 visual root
- **AND** logic root MUST 保持 MotionStage 在 logic tick 中结算出的 Transform

#### Scenario: 网络校正后表现贴合

- **WHEN** logic tick 收到 motion correction 并更新 logic pose
- **THEN** correction MUST 仍由 MotionStage 的 correction phase 处理
- **AND** 表现层 MAY 使用正式策略平滑或快速贴合 visual root
- **AND** 表现层 MUST NOT 把 correction 当作新的 motion contribution

### Requirement: Visual root 必须是正式配置

系统 MUST 让 `CharacterPipelineHost` 或等价 Unity 装配点显式持有 visual root / model root 绑定。缺少 visual root 时，系统 MUST 报告正式配置错误。系统 MUST NOT 自动使用 `CharacterController.transform`、Animancer 所在 transform、子节点搜索、同名对象搜索或 prefab 目录扫描作为 fallback。

#### Scenario: Host 配置 visual root

- **WHEN** 角色 Host 创建 `CharacterPipeline`
- **THEN** Host MUST 将正式 visual root 绑定传入表现层
- **AND** 表现层 MUST 只通过该绑定应用 visual pose

#### Scenario: 缺少 visual root

- **WHEN** 角色需要表现插值但 Host 没有配置 visual root
- **THEN** 系统 MUST 报告配置错误
- **AND** 系统 MUST NOT 静默把 logic root 当成 visual root 使用

### Requirement: 动画 visual playback 必须只影响显示时间

系统 MUST 让动画插值只生成 visual playback plan。visual playback plan MAY 插值 clip time、normalized time 和 weight，但 MUST NOT 改变 Timeline 的 logic playback time、TimelineNode 状态、Action window、Action cue、root motion、motion warp 或 SyncFacts。Animancer adapter MUST 只消费 visual playback plan 并应用显示姿态。

#### Scenario: 同一动画 plan 跨 tick 存在

- **WHEN** previous animation sample 和 current animation sample 中存在同一稳定 key 的播放计划
- **THEN** PresentationFrame MUST 使用 interpolation alpha 生成 visual clip time、normalized time 和 weight
- **AND** Animancer adapter MUST 应用该 visual plan
- **AND** TimelinePlaybackScheduler 的 active Timeline 时间 MUST 不被 PresentationFrame 改写

#### Scenario: 动作窗口与动画显示分离

- **WHEN** 攻击 Timeline 的 HitWindow 在某个 logic tick 触发
- **THEN** HitWindow fact MUST 只在 logic tick 中产生
- **AND** 动画 visual clip time 的插值 MUST NOT 重复提交该 HitWindow
- **AND** 网络同步事实 MUST 继续使用 logic tick fact

### Requirement: 表现插值不得产生同步事实

系统 MUST 保持 PresentationFrame 为表现消费阶段。表现插值 MAY 产生 visual pose、visual animation plan 和 runtime debug snapshot，但 MUST NOT 写入 `StrictGameplayOutput`、`CharacterSyncFacts`、ActionRuntime、Graph blackboard 或 NetworkSendStage 输出。

#### Scenario: 高帧率表现帧

- **WHEN** 120fps 渲染帧在两个 30Hz logic tick 之间多次调用 PresentationFrame
- **THEN** 每次 PresentationFrame MAY 更新 visual root 和 Animancer 显示姿态
- **AND** 系统 MUST NOT 为这些表现帧创建额外 ClientCommand、ActionWindowSample、ActionCueEvent 或 MotionSnapshot

### Requirement: 表现插值必须提供调试可追踪性

系统 SHOULD 暴露当前 visual pose 和 visual animation 的调试信息，至少包括 previous tick、current tick、interpolation alpha、visual position、visual rotation、参与插值的动画 key 和最终 visual clip time。调试信息 MUST 服务于动作手感、动画卡顿和 correction 抖动排查，MUST NOT 成为 gameplay 决策输入。

#### Scenario: 排查动画阶梯感

- **WHEN** 开发者查看角色 runtime debug
- **THEN** debug SHOULD 显示当前表现帧使用的 previous/current logic tick 和 alpha
- **AND** debug SHOULD 显示最终应用到 Animancer 的 visual clip time
- **AND** Graph transition 条件 MUST NOT 读取该 debug 数据
