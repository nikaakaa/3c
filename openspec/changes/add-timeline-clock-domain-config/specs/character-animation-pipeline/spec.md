## MODIFIED Requirements

### Requirement: Timeline逻辑采样与表现采样必须分离

Gameplay Timeline sampling MUST只按SimulationTick/canonical fraction发生；Action visual sampling、Presentation TreeClip Marker 与state-local Pose sampling MUST只按PresentationFrame发生。两个Simulation Tick之间的多个PresentationFrame MUST不重复产生TreeClip逻辑、Motion、ActionWindow、Cue fact或Effect mutation。Presentation sample MUST不推进CharacterSimulationState的Timeline clock。Presentation TreeClip 事件必须使用稳定 EventId 调和，只改变表现事件生命周期，不得写 Gameplay state 或 facts。

#### Scenario: 两个逻辑Tick之间多次渲染

- **WHEN** PresentationFrame多次采样同一Action、Pose source或Presentation TreeClip Marker
- **THEN** 动画Pose MAY连续变化，Marker MAY按稳定EventId保持或调和
- **AND** Gameplay state与facts MUST保持不变

### Requirement: Timeline Track / Clip 执行域必须分离

Timeline Track / Clip MUST显式声明 `Logic`、`Presentation` 或 `DualProjection` 执行域。Logic 内容 MUST由 SimulationTick 推进并遵守 Commit / Discard；Presentation 内容 MUST由 PresentationFrame 推进并只产生表现结果；DualProjection MUST只把同一作者 Clip 的 Logic 图和 Presentation Marker 投影到各自路径，不是第三个时钟或第二份 Timeline runtime。普通表现动画、特效、音效和相机 MUST不因为归属于同一 Timeline 而被迫等待逻辑 Tick。Presentation Event MUST不写 Gameplay fact。

#### Scenario: TreeClip 触发表现事件

- **WHEN** TreeClip 的 Presentation Marker 被视觉游标跨过
- **THEN** PresentationFrame MUST直接产生可调和的 Presentation Event
- **AND** 该事件 MUST不等待 SimulationTick
- **AND** Gameplay TreeClip 的逻辑输出仍 MUST按 Logic Tick 与 Commit / Discard 执行
