# Change: 收敛 Timeline 编辑器预览到管线权威

## Why

当前 `TimelinePlaybackScheduler` 已经把角色管线模式下的 Timeline 播放推进、轨道采样和动画贡献输出收进 `CharacterBTSMTLPhase`。动画表现主链路也已经落到 `AnimationContribution -> CharacterAnimationLayerRuntime -> AnimancerAnimationPresenter`。

但是 BTSMTL Timeline 编辑器 UI 仍然直接引用 `TimelinePlayer`：

- `TimelineEditorWindow` 的 target field 只接受 `TimelinePlayer`。
- 播放、暂停、速度和游标拖拽都调用 `Timeline.TimelinePlayer`。
- `Timeline` 资产本身还持有 `TimelinePlayer`、`PlayableGraph` 和 bind/evaluate 状态。

这会让编辑器预览和正式角色管线形成两套播放真相。用户在 Timeline 编辑器里看到的动画结果可能不是 `CharacterPipelineDefinition` 动画层表、priority、weight 和 Animancer adapter 最终应用出来的结果。这个分裂路径也和当前 `character-animation-pipeline` 的“不新增 Timeline 播放分裂路径”要求冲突。

## What Changes

- 新增 `btsmtl-timeline-editor-preview` 能力，定义 Timeline 编辑器预览的正式链路。
- Timeline 编辑器预览目标改为 `TimelinePreviewTarget`，由 `CharacterPipelineHost` 实现正式角色管线预览目标，而不是 `TimelinePlayer`。
- 新增 editor-only `TimelinePreviewSession` 概念，负责播放状态、时间、速度、目标绑定、采样和预览输出。
- 预览采样复用正式 Timeline 轨道采样逻辑，动画结果进入 `AnimationContribution`、`CharacterAnimationLayerRuntime` 和 `AnimancerAnimationPresenter`。
- Timeline 编辑器 UI 的播放按钮、速度输入和游标拖拽只控制 `TimelinePreviewSession`。
- 收紧现有动画 spec：BTSMTL 编辑器预览不再作为 `TimelinePlayer` 例外路径保留。
- 明确旧 `TimelinePlayer`、`Timeline.Bind/Unbind/Evaluate`、PlayableGraph 字段和依赖 `Timeline.TimelinePlayer` 的旧轨道必须迁移或删除，不能作为并行预览/runtime 路径继续存在。

## Out of Scope

- 不实现完整 Timeline track 类型迁移。
- 不实现新的 gameplay solver、命中、伤害或服务端裁决。
- 不新增测试。
- 不新增独立 Workbench、并行端口系统、旧 SO/config 或 fallback 配置。
- 不把 Unity 手动端到端验证写入任务。

## Spec 对比和冲突

- `character-animation-pipeline` 已要求角色管线 Timeline 播放主链路唯一：节点提交请求，`TimelinePlaybackScheduler` 推进，轨道采样输出数据，`PresentationStage` 应用表现。
- `character-animation-layer-runtime` 当前仍有“BTSMTL 编辑器预览可保留 `TimelinePlayer` 或 PlayableGraph”的场景。这个场景会保留编辑器预览分裂路径，和当前管线权威方向冲突。
- 本变更选择收紧该例外：编辑器预览可以是 editor-only，但必须复用正式采样、动画层和 Animancer 应用规则；不能继续由 `TimelinePlayer` autonomous playback 表达预览真相。

## Impact

- 影响 BTSMTL Timeline 编辑器窗口、Timeline field、Timeline 资产播放状态、旧 `TimelinePlayer` 和依赖 `Timeline.TimelinePlayer` 的轨道。
- 影响 `CharacterPipelineHost` 的只读装配信息和 Timeline 预览目标实现，方便 editor preview session 通过正式管线目标应用动画预览。
- 不改变 `TimelineNode` 的运行语义：它仍然只提交播放请求，不直接播放 Timeline。
- 不改变 `TimelinePlaybackScheduler` 作为角色管线 Timeline 播放权威的定位。
