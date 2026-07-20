## MODIFIED Requirements

### Requirement: Timeline Preview 必须按正式阶段展示 TreeClip

Timeline Editor MUST显示 TreeClip的 Decision/Commit阶段、inline/shared ownership和 Blackboard输出摘要。Authoring Preview MAY将已创作 MotionCurve 只读投影到 visual root，但 MUST NOT执行 TreeClip、Program operation、SimulationKernel、Action、跨来源 Motion arbitration、MotionWarp、Blackboard、GameplayEffect、碰撞或 WorldSolver。TreeClip 的真实 Decision/Commit、输出与终止事实 MUST只由正式运行 Session产生，并通过 Live Debug显示。系统 MUST NOT创建 Preview Simulation Session、临时 `CharacterGraphContext`、`TimelineRunningTree` clone、写入 authoring默认值或形成第二套 TreeClip执行语义。

#### Scenario: Authoring Preview 打开含 TreeClip 的 Timeline

- **WHEN** 作者在 Authoring Preview 打开包含 TreeClip 的 Timeline
- **THEN** Timeline Editor MUST继续显示 Clip、阶段、Graph、ownership 与声明摘要
- **AND** Authoring Preview MUST只采样 AnimationTrack 表现和单来源 MotionCurve 只读轨迹
- **AND** MUST不执行 TreeClip 或产生 Gameplay 事实

#### Scenario: Live Debug 观察 TreeClip

- **WHEN** 正式运行 Session 执行当前 Timeline 的 TreeClip
- **THEN** Live Debug MUST显示正式 runtime trace 中的 Decision/Commit 与输出事实
- **AND** MUST不根据 authoring preview time 推测执行结果

### Requirement: Timeline Editor 必须分离 Authoring Preview 与 Live Debug

`TimelineEditorWindow` MUST 提供语义明确且互斥的 Authoring Preview 与 Live Debug 模式。Authoring Preview MUST由 `TimelinePreviewSession` 驱动且只采样动画表现与已创作 MotionCurve 的只读轨迹；Live Debug MUST由 `RuntimeDebugSession` 的共享增量 provider current state或显式Capture history与Timeline窗口本地runtime binding观察真实Program/Session trace，不得调用preview evaluator、修改runtime playback或改写其它Graph/Timeline窗口的binding。

#### Scenario: Authoring Preview

- **WHEN** 用户选择 Authoring Preview
- **THEN** TimelineEditor MUST使用显式 preview target、preview time 与独立 animation lifecycle
- **AND** MUST不创建 Simulation Session、输入、logic Tick、Action target、MotionWarp或WorldSolver
- **AND** UI MUST不把结果标记为真实 gameplay runtime

#### Scenario: Live Debug

- **WHEN** 用户选择 Live Debug
- **THEN** TimelineEditor MUST以当前Timeline identity/content hash请求正式target解析
- **AND** 成功附着时 MUST使用该窗口本地binding观察真实playback
- **AND** Timeline编辑内容 MUST只读
- **AND** `TimelinePreviewSession` MUST不参与该模式

### Requirement: 预览采样必须复用正式动画播放链路

Timeline Authoring Preview MUST通过 `CharacterPresentationProjection` 将稳定 producer identity解析为表现资源，并复用正式 `CharacterAnimationPlaybackCommandQueue`、`AnimationPlaybackLifecycle` 与 `AnimancerPlaybackAdapter`。Preview session MUST为每层生成零或一个带独立preview EventId/playback generation的producer command和sample；它 MAY从 MotionCurve authoring区间采样单来源只读轨迹，但 MUST不生成Gameplay、Motion request或World请求，不执行跨来源Priority仲裁，不直接播放Clip，也不实现第二套layer mixing。

#### Scenario: 当前时间采样

- **WHEN** preview time位于AnimationTrack clip范围
- **THEN** session MUST提交该producer的唯一preview command与sample
- **AND** AnimationPlaybackLifecycle MUST完成PendingFirstSample/Current提交
- **AND** AnimancerPlaybackAdapter MUST应用Projection中的正式producer binding
- **AND** Authoring Preview MUST以Immediate transition evaluation应用首次采样姿势，不得等待表现时间推进fade

#### Scenario: 非动画轨道同时存在

- **WHEN** Timeline同时包含TreeClip、Action Cue、MotionCurve或MotionWarp
- **THEN** Authoring Preview MUST继续采样AnimationTrack
- **AND** MAY将单来源MotionCurve累计轨迹投影到visual root
- **AND** MUST不执行TreeClip、Action Cue、MotionWarp或要求Preview Simulation配置

#### Scenario: MotionCurve 手动 seek

- **WHEN** 作者把 preview time 拖到含单一 MotionCurve contribution 的任意时间
- **THEN** Authoring Preview MUST从预览原始姿态和 Timeline 零时刻绝对求值累计位移与朝向
- **AND** Local 位移 MUST按求值过程中的累计朝向旋转，World 位移 MUST直接累加
- **AND** 反复前后 seek MUST不累积漂移
- **AND** logic root、CharacterController、Simulation body与WorldSolver MUST不被修改

#### Scenario: MotionCurve 来源重叠

- **WHEN** 同一采样区间出现多个可解析 MotionCurve contribution
- **THEN** Authoring Preview MUST显式报告不支持的跨来源仲裁
- **AND** MUST不自行比较Priority或猜测正式运行结果

#### Scenario: MotionCurve Preview 结束

- **WHEN** Authoring Preview关闭、Timeline切换或Target切换
- **THEN** visual root MUST恢复进入预览前的位置与旋转

#### Scenario: 手动 seek

- **WHEN** 作者在同一 Timeline 与 Target 上将 preview time 跳转到新的动画采样时间
- **THEN** session MUST复用当前producer selection与playback generation，并以新的sample event更新producer sample time
- **AND** AnimancerPlaybackAdapter MUST在零表现时间步应用目标时间的精确姿势
- **AND** session MUST不把seek解释为producer切换或重新开始淡入

#### Scenario: Preview ownership 变化

- **WHEN** Timeline、Target或authoring内容切换
- **THEN** session MUST retire旧preview playback并清理对应playback lifecycle与Animancer state
- **AND** 新ownership MUST使用新的preview playback generation建立command/sample

#### Scenario: 连续播放

- **WHEN** session连续播放
- **THEN** 同一preview playback generation MUST持续更新producer sample time
- **AND** session MUST不在每个表现帧重新创建隐藏producer

## REMOVED Requirements

### Requirement: MotionWarp Gameplay Preview 必须显式提供目标快照

**Reason**: Authoring Preview 不再执行 Gameplay、MotionWarp 或 WorldSolver；目标快照与真实 Warp 结果统一由正式 Session 和 Live Debug 提供。

**Migration**: 删除 Timeline Editor Action Target UI、preview target snapshot state、Preview Simulation输入与配置。MotionWarp作者参数继续在Timeline Inspector编辑，运行结果在Live Debug观察。
