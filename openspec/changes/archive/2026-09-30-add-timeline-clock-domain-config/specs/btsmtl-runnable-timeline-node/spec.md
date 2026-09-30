## MODIFIED Requirements

### Requirement: Timeline 资产不承载节点生命周期

系统 MUST 将 Timeline authoring data 与 Unity asset 身份分离。普通 C# 可序列化 `TimelineData` MUST 作为 tracks、clips、markers、sections 和秒制时间的唯一数据模型，不保留旧 Scale 或可写作者帧副本；`TimelineAsset` MUST 只作为显式 shared 复用和 Project 直接打开的 ScriptableObject 外壳持有一份 TimelineData。TimelineData 与 TimelineAsset MUST NOT 继承 `RunnableNode`，也 MUST NOT 直接承担 Graph 节点生命周期。

#### Scenario: Graph 播放 inline Timeline

- **WHEN** Graph 中的 TimelineNode 使用默认 Inline ownership
- **THEN** TimelineNode MUST 解析自己持有的 TimelineData 并提交正式播放请求
- **AND** 系统 MUST NOT 要求作者先创建 TimelineAsset，Graph MUST NOT 直接 tick authoring TimelineData

#### Scenario: Graph 播放 shared Timeline

- **WHEN** TimelineNode 显式选择 Shared Asset ownership
- **THEN** resolved TimelineData MUST 来自 TimelineAsset 持有的数据
- **AND** TimelineNode MUST NOT 同时保留一份生效的 inline TimelineData，Graph MUST NOT 直接 tick TimelineAsset

### Requirement: TreeClip编译为TimelineBody图operation invocation

Logic Track 中的 TreeClip MUST将其 AssetTree（TimelineBody 图）编译为正式 operations：TimelineClip caller MUST按 clip 声明 OnEnable、OnDisable、OnDestroy 三个边界 hook entry 与 Root（技能入口）entry，SourceMap MUST按 clipId 登记 Root handle 供运行时查询。Presentation 内容 MUST使用与 Clip 同级的表现 Marker；Presentation Marker 图只能通过表现安全执行上下文运行，MUST NOT绑定、编译或执行 TimelineBody 图。系统 MUST不恢复 Timeline.Bind/Evaluate/Unbind 自主播放路径。

#### Scenario: Decision TreeClip 穿过 Loop 边界

- **WHEN** 一个 SimulationTick 穿过 Logic TreeClip 的 Timeline loop 边界
- **THEN** compiled evaluator MUST按尾段、中间 cycle 和头段顺序求值
- **AND** Frame Blackboard MUST保持唯一结果

## ADDED Requirements

### Requirement: Marker图只能暴露表现安全的OnEnable入口

Timeline Marker MUST是与 Clip 同级的一等内容实体：零时长触发点、稳定 MarkerId、秒制触发位置与触发图引用。Marker 的触发图 MUST只暴露 OnEnable 一个回调，MUST NOT拥有 Update、Exit、Disable 或结束片段生命周期。Logic Marker 可经正式 SimulationTick Advance / Commit 执行；Presentation Marker MUST经表现采样结果跨点触发，并绑定正式表现执行上下文。Presentation 图 MUST不访问 SimulationState、Gameplay fact、canonical input 或 Kernel Evaluate / Finalize；能力不满足时 MUST在编译或准备边界失败。Marker MUST不作为 Clip 的子列表存在。

#### Scenario: 表现域Marker触发

- **WHEN** 表现采样结果跨过 Presentation Marker 且所属 playback 仍有事件资格
- **THEN** 触发图 MUST只执行 OnEnable 一次并进入表现帧接受 / 丢弃边界
- **AND** MUST不借用只在 Logic Tick 范围内有效的技能 invoker或产生 Gameplay fact

#### Scenario: 逻辑域Marker触发

- **WHEN** SimulationTick 跨过 Logic Marker 的触发点
- **THEN** 其触发图 MUST经 Advance / Commit 协议执行 OnEnable 入口一次
- **AND** 同一经过 Discard 后 MUST不产生执行残留

#### Scenario: 与Clip同级共存

- **WHEN** 作者在同一 Timeline 使用 Clip 与 Marker
- **THEN** Marker MUST作为与 Clip 同级的内容实体创建、存储与推进
- **AND** Marker MUST NOT作为 Clip 的子列表存在

### Requirement: Timeline运行时不得复制表现图执行语言

Presentation Marker 图的 authoring data、图身份、节点能力和输出合同 MUST复用正式 Graph / Timeline owner 的编译与准备边界。运行时 MAY形成当前表现帧的候选结果，但 MUST NOT为了 Presentation 新建一套可跨播放复用的操作码、影子图资产、私有事件系统或第二 Timeline 内容。

#### Scenario: 表现图准备失败

- **WHEN** Presentation Marker 的图身份、revision、能力或下游 domain 缺失
- **THEN** Prepare MUST报告明确失败
- **AND** MUST NOT返回空图、默认图或兼容替代
