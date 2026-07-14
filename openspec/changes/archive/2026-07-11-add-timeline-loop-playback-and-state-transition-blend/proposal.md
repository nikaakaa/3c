# Change: Timeline 循环播放与状态切换动画混合

## 背景

当前 Corin locomotion 的循环状态用普通 `LoopNode` 包住 `TimelineNode`。这对执行树语义是成立的，但对动画 Timeline 不成立：`LoopNode` 每轮都会让子 `TimelineNode` 重新进入生命周期，导致播放请求完成、停止、重新提交，表现层看到的是一串离散的一次性 Timeline，而不是同一个持续循环的动画源。

状态切换也缺少正式动画混合合同。`StateMachineGraphRuntime` 可以切换 active state，Timeline 可以输出动画贡献，`CharacterPresentationStage` 可以做渲染帧插值，但目前没有一条正式链路表达“从旧状态 pose 淡出、向新状态 pose 淡入”。如果把这个交给 Animancer CrossFade，就会让 adapter 变成状态切换权威，和现有 spec 冲突。

## 目标

- 让 `TimelineNode` 拥有正式 `Once` / `Loop` 播放模式，循环 Timeline 由 `TimelinePlaybackScheduler` 持续推进和回绕，不再依赖普通 `LoopNode` 重启子节点。
- 让循环 Timeline 在回绕边界保持同一个 request handle、同一个 source identity，并正确采样动画、motion、window、cue 等轨道事实。
- 让动画贡献携带足够的循环时间信息，使表现层在 `0.48s -> 0.02s` 这类回绕帧中按前进方向插值，而不是反向 lerp。
- 让状态切换动画混合成为 `StateMachineGraph` Transition edge 的正式表现元数据，由 runtime 发出切换事实，由 `CharacterPresentationStage` 保留 outgoing 动画计划并淡入 incoming 动画计划。
- 清理 Corin locomotion 状态 body 中用于动画循环的普通 `LoopNode`，改为 `TimelineNode PlaybackMode=Loop`；保留 RootTree 顶层 `Runtime Loop` 作为角色主流程持续运行入口。

## 非目标

- 不新增 Unity batchmode、自动化测试或手动验证任务。
- 不实现 motion warping、完整 root motion 烘焙策略、网络快照插值或服务端动画同步。
- 不把 Animancer CrossFade、Animator Controller、旧 locomotion/action SO 或旧 TimelinePlayer autonomous playback 作为新权威。
- 不创建兼容路径、fallback 配置、一次性 SubTree asset 或临时桥接节点。
- 不把 `LoopNode` 作为 Timeline 动画循环的正式解法；普通执行树仍可保留自己的通用 decorator 语义。

## 方案摘要

`TimelineNode` 继续只负责提交播放请求。节点新增播放模式配置，默认 `Once` 保持现有 `Succeeded -> Success` 语义；`Loop` 请求在到达 Timeline duration 时由 `TimelinePlaybackScheduler` 回绕播放时间并保持 `Running`，直到节点 stop/reset 或状态离开取消请求。

`TimelinePlaybackScheduler` 成为 loop 边界的唯一处理点。它需要保存未回绕的连续播放时间或等价 cycle 信息，对轨道采样使用回绕前后两个区间，避免边界丢采样或重复采样。动画贡献需要把 clip time 与循环上下文一起传给动画层和表现层。

状态切换的动画混合不进入 Condition rule graph。Condition rule 仍是纯条件；Transition edge 保存 priority、rule ownership 之外的表现 blend 元数据。runtime 发生状态切换时，只 tick 新 active state，不继续 tick 旧状态行为；表现层用旧状态上一帧已经产出的播放计划作为 outgoing pose，再把新状态产出的计划作为 incoming pose 按 edge blend 淡入。

## 与现有规格关系

- `btsmtl-runnable-timeline-node` 已规定 `TimelineNode` 是普通可执行节点、只提交请求、不直接推进 Timeline。本 change 增加播放模式要求，不改变节点不直接播放的边界。
- `character-animation-pipeline` 已规定 `TimelinePlaybackScheduler` 是 Timeline 播放权威。本 change 增加 loop request、回绕采样和状态切换表现混合。
- `character-animation-layer-runtime` 已规定动画贡献是唯一输入合同、Animancer 只是 adapter。本 change 增加循环时间信息和 transition blend 输入，不让 Animancer 决策切换。
- `btsmtl-sm-node-authoring` 已规定 Transition 是 edge、规则图纯条件、调度元数据在边上。本 change 把动画 blend 元数据也放在边上，不新增 `TransitionNode`。
- `character-state-timeline-authoring-loop` 已规定 Corin RootTree 和状态机闭环。本 change 收紧 Corin loop 状态的 Timeline 播放方式，避免状态 body 使用普通 `LoopNode` 重启 Timeline。

当前 `openspec list` 显示 `add-pipeline-blackboard-authoring` 已 Complete，但 `openspec/project.md` 仍提到该项未完成；这是项目说明的状态描述滞后，不影响本 change 的设计边界。

## 影响范围

- BTSMTL TimelineNode authoring 与请求合同。
- `TimelinePlaybackScheduler` active record、采样区间和 request 状态。
- 动画贡献 / 动画层运行时 / `CharacterPresentationStage` 的循环插值与 transition blend 输入。
- `StateMachineGraph` Transition edge 序列化、Inspector 和 runtime 切换输出。
- Corin RootTree 内联状态行为图与相关 locomotion TimelineNode 配置。

## 风险与缺口

- 如果当前 BTSMTL 序列化无法安全给现有 `TimelineNode` 增加播放模式字段，实施阶段必须停止并说明迁移缺口。
- 如果 Transition edge 当前序列化无法保存 blend 元数据且不能安全迁移，实施阶段必须停止并说明 tradeoff。
- 如果表现层无法从上一帧保留 outgoing 动画计划而只能通过旧状态继续 tick 获得 pose，实施阶段必须停止；继续 tick 旧状态会重复 gameplay facts、motion 或 action window。
