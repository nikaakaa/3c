# Design: Timeline 动画播放权威收口

## 目标链路

本设计采纳方案 C：`CharacterPipeline` 拥有 tick 和播放权，Timeline/Track 保留内容采样语义，`CharacterPresentationStage` 统一应用最终动画。

```text
RunnableTree tick
-> TimelineNode 提交播放请求
-> CharacterBTSMTLPhase 内部 TimelinePlaybackScheduler 维护 active Timeline
-> Timeline / Track 按当前时间采样
-> AnimationTrack 输出 AnimationContribution
-> AnimationMixer 汇总层、权重、fade、优先级
-> CharacterPresentationStage 应用到 Animator / PlayableGraph
-> Debug Preview 读取 AnimationBlendSnapshot
```

## 职责划分

### TimelineNode

`TimelineNode` 保留为 `RunnableNode`，仍通过 `TimelineReferenceModule` 引用 Timeline 资产。它不再创建 runtime Timeline，不再绑定 `TimelinePlayer`，不再调用 `Timeline.Evaluate()` 或 `TimelinePlayer.EvaluatePlayableGraph()`。

节点生命周期只负责请求：

- `OnStart()` 提交 `TimelinePlaybackRequest`。
- `OnUpdate()` 查询 request handle 的状态并返回 `Running`、`Success` 或 `Failure`。
- `OnStop()` / `OnReset()` 取消未完成请求或释放 handle。

业务取舍：这样状态机、行为树和 Timeline 的控制权仍在 BTSMTL 节点生命周期里，但动画推进权收口到管线，避免节点和管线同帧双重播放。

### TimelinePlaybackScheduler

`TimelinePlaybackScheduler` 是 `CharacterBTSMTLPhase` 内部的 Timeline runtime owner。它负责：

- 读取本帧 Timeline 请求。
- 创建 active Timeline runtime record。
- 推进 active Timeline time。
- 调用轨道采样。
- 写入 strict gameplay output、presentation output 或 debug snapshot。
- 在完成、取消、打断或 pipeline deactivate 时清理 active runtime。

它不直接做最终 Animator 写入。动画输出交给 presentation/mixer。

### Timeline / Track

Timeline 资产继续是 authoring 数据。Track 负责解释自己的编辑语义，例如 clip 范围、重叠、权重、mute、loop 和采样。区别是：轨道采样产出纯数据，不直接控制场景对象或 PlayableGraph。

动画轨道第一阶段产出动画命令或动画贡献：

```text
source id
clip 或 state 名称
layer
clip time / normalized time
weight
fade
priority
blend mode
```

业务取舍：不让管线完全重写 Timeline 轨道解析，避免复制一套 Timeline runtime；也不让 Track 直接碰 Animator，避免播放权分裂。

### AnimationMixer

动画混合模型是运行时核心，不是纯调试功能。它至少需要保存：

- 当前层列表。
- 每层候选贡献。
- 每个贡献的来源、权重、时间和混合模式。
- 每层最终输出结果。

调试预览需要的 `AnimationBlendSnapshot` 从这些运行时状态导出。snapshot 可以记录更多文字、历史和 UI 字段，但不能成为运行时逻辑输入。

### CharacterPresentationStage

`CharacterPresentationStage` 是最终碰 `Animator` 或 `PlayableGraph` 的边界。它消费 animation mixer result 或 presentation commands，负责把纯数据应用到 Unity 表现系统。

如果后续保留 `TimelinePlayer`，它只能在这一层作为 PlayableGraph adapter 使用，不能由 `TimelineNode` 或 Timeline 轨道直接推进。

## 方案对比

### 方案 A：保持 TimelineNode 直接播放

短期最省事，已有代码能通过 `TimelinePlayer` 看到动画。代价是动画权威分散，后续动画混合预览只能观察最终图结果，无法解释来源、优先级、fade 和覆盖关系。网络预测、回放和动作窗口也会被 Timeline 直接播放路径绕开。

不采用。

### 方案 B：管线完全绕过 Timeline runtime 自己解析资产

管线最干净，但会重复实现 Timeline 的 track、clip、mute、overlap、rebind、采样语义。短期成本过高，也浪费 BTSMTL Timeline 现有编辑器语义。

不采用。

### 方案 C：管线拥有 tick，Timeline/Track 负责采样输出数据

Timeline 保留为 authoring 和采样模型，管线负责播放权、合并权和最终应用权。这样能支持 TimelineNode、StateMachine、Tree、Action 同时提交动画意图，也能支持动画层调试预览。

采用。

业务意义：

- Timeline 仍然是动作内容编辑器，不被降级成没有行为的死数据。
- `CharacterPipeline` 成为动画播放和混合权威，能统一处理本地预测、远端表现、回放和调试。
- 动画层预览可以解释过程，而不是只看到 PlayableGraph 的最终结果。
- 后续迁移其它轨道时仍沿用同一规则：轨道采样输出数据，stage 和 mixer 负责运行时权威。

## 迁移边界

- 不新增第二个 TimelineNode。
- 不新增第二套 TimelinePlayer 自主播放模式。
- 不新增旧 SO/config fallback。
- 不用 debug snapshot 驱动运行时。
- 旧直接播放逻辑迁移完成后应删除，不保留兼容分支。

## 与现有规格的冲突

`btsmtl-runnable-timeline-node` 当前要求 `TimelineNode` 自己实例化、绑定和评估 Timeline，并从执行上下文获取 `TimelinePlayer`。这与本变更目标冲突，必须通过 spec delta 修改，而不是在旁边新增一条 pipeline 播放路径。
