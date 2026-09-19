## MODIFIED Requirements

### Requirement: Timeline逻辑采样与表现采样必须分离

Gameplay Timeline sampling MUST只按SimulationTick/canonical fraction发生；Action visual sampling、Presentation Marker 与state-local Pose sampling MUST只按PresentationFrame发生。两个Simulation Tick之间的多个PresentationFrame MUST不重复产生TreeClip逻辑、Motion、ActionWindow、Cue fact或Effect mutation。对同一 playback identity / generation，动作表现结果 MUST在该表现帧由正式 owner 计算一次，动作动画、该动作的 Presentation Marker 与 Camera 内容 MUST消费该结果。Presentation sample MUST不推进CharacterSimulationState的Timeline clock。Presentation Marker 事件必须使用稳定 EventId 调和，只改变表现事件生命周期，不得写 Gameplay state 或 facts。

#### Scenario: 两个逻辑Tick之间多次渲染

- **WHEN** PresentationFrame多次采样同一Action、Pose source或Presentation Marker
- **THEN** 动画Pose MAY连续变化，Marker MAY按稳定EventId保持或调和
- **AND** Gameplay state与facts MUST保持不变
- **AND** 同一动作的表现消费者 MUST不各自累加出互相矛盾的动作位置

### Requirement: Timeline Track / Clip 执行域必须分离

Timeline Track / Clip MUST显式声明 `Logic`、`Presentation` 或 `DualProjection` 执行域。Marker MUST是与 Clip 同级的点触发实体，并跟随所在 Track 的执行域由 SimulationTick 或 PresentationFrame 推进。Logic 内容 MUST由SimulationTick推进并遵守Commit / Discard；Presentation 内容 MUST由PresentationFrame推进并只产生表现结果；DualProjection MUST只把同一作者 Clip 的 Logic 图和 Presentation Marker 投影到各自路径，不是第三个时钟或第二份 Timeline runtime。普通表现动画、特效、音效和相机 MUST不因为归属于同一 Timeline 而被迫等待逻辑 Tick。Presentation TreeClip 图 MUST不在 PresentationFrame 执行 Logic TimelineBody 图；Presentation Event MUST不写 Gameplay fact。域声明 MUST不自动选择表现时钟策略，策略由正式业务装配决定。

#### Scenario: TreeClip 触发表现事件

- **WHEN** Presentation Marker 被视觉游标或同一动作的正式表现采样结果跨过
- **THEN** PresentationFrame MUST直接产生可调和的 Presentation Event
- **AND** 该事件 MUST不要求再次调用SimulationTick或Logic evaluator
- **AND** Gameplay TreeClip 的逻辑输出仍 MUST按 Logic Tick 与 Commit / Discard 执行

## ADDED Requirements

### Requirement: 同一动作的表现采样结果必须被统一消费

同一动作的 playback identity / generation 在每个 PresentationFrame MUST只产生一个表现位置结果，结果至少包含前后位置、循环经过、变化原因及 Marker 事件资格。动作动画、该动作的 Presentation Marker 和 Camera Track MUST消费同一结果；Clip 的起点、ClipIn、源速率、source phase、混合和 locomotion 继续由各自正式 owner 处理。此结果 MUST不成为第二份 Timeline 内容、逻辑事实或全角色统一游标。

#### Scenario: 动画和Camera共用动作位置

- **WHEN** 动作表现位置从 19.5 变化到 20.5
- **THEN** 动画与 Camera MUST根据同一结果各自映射源内容
- **AND** 第 20 帧 Marker MUST按同一次经过决定是否交付

### Requirement: Presentation Marker执行必须遵守表现帧事务

Presentation Marker 触发图 MUST使用正式表现安全上下文，只读允许的表现事实并输出 typed 表现结果；MUST NOT访问可写 SimulationState、产生 Gameplay fact、调用 Kernel Evaluate / Finalize 或保留 Logic Tick 的临时 invoker。图候选、EventId 记账和下游命令 MUST进入既有 Prepare / Validate / Commit / Discard 表现帧边界。Stop、Cancel、generation替换生效后旧播放 MUST不再产生新的 Marker；正常循环才 MAY生成新的 TraversalIndex，修正采样不自动生成新的经过。

#### Scenario: 表现帧被丢弃

- **WHEN** Presentation Marker 图已形成候选但表现帧未通过正式接受阶段
- **THEN** 候选与未交付事件记账 MUST一起丢弃
- **AND** 下游 MUST不收到该事件，下一帧也不得因旧记账缺失重复交付

#### Scenario: 停止后继续渲染

- **WHEN** playback 的 Stop / Cancel 已被正式 owner 接受
- **THEN** 后续 PresentationFrame MUST不再产生该 playback / generation 的新 Marker
- **AND** 已生成动画、相机或效果尾部 MUST由其原领域 owner 处理，不得保持旧 Timeline 活跃
