## MODIFIED Requirements

### Requirement: Timeline逻辑采样与表现采样必须分离

Gameplay Timeline sampling MUST只在 SimulationTick/canonical fraction 调用范围发生；Action visual sampling、Presentation Marker 与 state-local Pose sampling MUST只在 PresentationFrame 发生。动作进度 MUST由既有播放管理者计算，Timeline MUST被动消费相应域的时间经过，不得自持两套独立计时器。两个 SimulationTick 之间的多次表现调用 MUST不重复产生 TreeClip 逻辑、Motion、ActionWindow、Cue fact 或 Effect mutation。对同一 playback identity / generation，动作表现结果 MUST每帧计算一次，动画、Marker 与 Camera MUST共享该结果，不重复应用动作倍率。Presentation MUST不推进逻辑动作时间或写 Gameplay state / facts，事件 MUST按稳定 EventId 调和。

#### Scenario: 两个逻辑Tick之间多次渲染

- **WHEN** PresentationFrame多次采样同一Action、Pose source或Presentation Marker
- **THEN** 动画Pose MAY连续变化，Marker MAY按稳定EventId保持或调和
- **AND** Gameplay state与facts MUST保持不变
- **AND** 同一动作的表现消费者 MUST不各自累加出互相矛盾的动作位置

### Requirement: Timeline Track / Clip 执行域必须分离

Timeline Track MUST唯一声明 `Logic` 或 `Presentation`；Clip 与同级 Marker MUST继承所在 Track，MUST NOT提供 Clip 域覆盖或 DualProjection。Logic MUST在 SimulationTick 消费外部逻辑进度并遵守 Commit / Discard；Presentation MUST在表现帧消费外部采样并只产生表现结果。两类轨道 MUST共用正式播放管理者提供的动作进度，表现采样 MUST遵守已有 sample / horizon 合同，不得自行外推未来事件。逻辑结果传给表现 MUST走原提交链。Presentation MUST不执行 Logic TimelineBody 图或写 Gameplay fact；域声明 MUST不成为自主时钟或隐式进度策略开关。

#### Scenario: TreeClip 触发表现事件

- **WHEN** Presentation Marker 被视觉游标或同一动作的正式表现采样结果跨过
- **THEN** PresentationFrame MUST直接产生可调和的 Presentation Event
- **AND** 该事件 MUST不要求再次调用SimulationTick或Logic evaluator
- **AND** Gameplay TreeClip 的逻辑输出仍 MUST按 Logic Tick 与 Commit / Discard 执行

## ADDED Requirements

### Requirement: 同一动作的表现采样结果必须被统一消费

同一动作的 playback identity / generation 在每个 PresentationFrame MUST只产生一个表现位置结果，结果至少包含前后位置、循环经过、变化原因及 Marker 事件资格。动作动画、该动作的 Presentation Marker 和 Camera Track MUST消费同一结果；Clip 的起点、ClipIn、源速率、source phase、混合和 locomotion 继续由各自正式 owner 处理。此结果 MUST不成为第二份 Timeline 内容、逻辑事实或全角色统一游标。

#### Scenario: 动画和Camera共用动作位置

- **WHEN** 动作表现位置从 0.24 秒正常前进到 0.26 秒
- **THEN** 动画与 Camera MUST根据同一结果各自映射源内容
- **AND** 0.25 秒 Marker MUST按同一次经过决定是否交付

### Requirement: Presentation Marker执行必须遵守表现帧事务

Presentation Marker 触发图 MUST使用正式表现安全上下文，只读允许的表现事实并输出 typed 表现结果；MUST NOT访问可写 SimulationState、产生 Gameplay fact、调用 Kernel Evaluate / Finalize 或保留 Logic Tick 的临时 invoker。图候选、EventId 记账和下游命令 MUST进入既有 Prepare / Validate / Commit / Discard 表现帧边界。Stop、Cancel、generation替换生效后旧播放 MUST不再产生新的 Marker；正常循环才 MAY生成新的 TraversalIndex，修正采样不自动生成新的经过。

#### Scenario: 表现帧被丢弃

- **WHEN** Presentation Marker 图已形成候选但表现帧未通过正式接受阶段
- **THEN** 候选与未交付事件记账 MUST一起丢弃
- **AND** 下游 MUST不收到该事件，后续合法采样仍可交付尚未接受的同一事件，不得提前记为已消费

#### Scenario: 停止后继续渲染

- **WHEN** playback 的 Stop / Cancel 已被正式 owner 接受
- **THEN** 后续 PresentationFrame MUST不再产生该 playback / generation 的新 Marker
- **AND** 已生成动画、相机或效果尾部 MUST由其原领域 owner 处理，不得保持旧 Timeline 活跃

#### Scenario: 逻辑分支修正后的相机继续表现

- **WHEN** 已接受镜头事件的来源分支被撤销或角色目标位置被修正
- **THEN** Camera MUST从当前可见状态继续跟随修正后的目标，按稳定事件身份撤销失效请求并由原 owner 处理退出
- **AND** Camera MUST NOT恢复过去的平滑、碰撞、混合和效果计时状态
- **AND** 未接受候选的丢弃 MUST仅约束请求和事件交付记账，不要求跨 Pose／Cinemachine 物理写入回滚
