# Change: 重构 Timeline 动画播放权威到角色管线

## Why

当前 `TimelineNode` 已经能在 BTSMTL `RunnableTree` 中被 tick，并通过 `TimelinePlayer` 直接实例化、绑定和推进 `Timeline`。这能快速看到动画，但和角色管线目标冲突：动画播放权分散在节点和 `TimelinePlayer` 中，后续无法统一解释每 tick 的动画混合、动作窗口、表现命令、预测和调试预览。

本变更把语义收口为：`TimelineNode` 只表达“请求播放某个 Timeline”，`CharacterBTSMTLPhase` 内部 `TimelinePlaybackScheduler` 统一推进 Timeline 和采样轨道，动画轨道每 tick 产出数据贡献，`CharacterPresentationStage` 统一应用动画。动画混合预览读取运行时 snapshot，但不反向驱动运行时。

## What Changes

- 明确采纳方案 C：管线拥有 Timeline 播放权，Timeline/Track 保留采样语义，Presentation 层统一应用动画。
- 修改 `btsmtl-runnable-timeline-node` 当前规格：`TimelineNode` 不再直接播放 Timeline，也不再直接依赖 `TimelinePlayer` 评估 `PlayableGraph`。
- 新增 `character-animation-pipeline` 能力：定义 Timeline 播放请求、active Timeline runtime、动画轨道采样、动画混合状态、表现应用和调试 snapshot 的职责边界。
- 明确 `TimelinePlayer` 在角色管线模式下只能作为表现层 adapter 或 provider 资源，不再由 `TimelineNode` 直接推进。
- 明确动画层预览是调试视图，读取 `AnimationBlendSnapshot` 或等价 debug history；正式运行时只依赖精简命令和混合状态。
- 不新增 Workbench、并行端口协议、旧 SO/config 数据源或第二套 Timeline 播放路径。

## Chosen Approach

本变更采用“管线 tick，Timeline/Track 采样”的中间方案：

```text
TimelineNode
-> TimelinePlaybackRequest
-> CharacterBTSMTLPhase 内部 TimelinePlaybackScheduler 推进 active Timeline
-> Timeline/Track 按当前 time 采样
-> AnimationMixer 合并动画贡献
-> CharacterPresentationStage 应用结果
```

业务取舍：

- 不采用 `TimelineNode` 直接播放，因为它会绕过角色管线，后续动画层预览、预测、回放和网络校验无法解释动画来源和权重。
- 不采用管线完全重写 Timeline 资产解析，因为这会废掉 BTSMTL Timeline 现有轨道、clip、mute、overlap 和编辑器语义，短期成本过高。
- 采用方案 C，因为它保留 Timeline 的内容编辑和采样价值，同时把播放权、混合权和最终应用权收回角色管线。

## Non-Goals

- 本变更不设计完整 PlayableGraph mixer 的最终实现细节。
- 本变更不实现网络同步、服务端裁决或 combat solver。
- 本变更不恢复旧 locomotion、action、footphase、bodyclaim 或 animation presentation SO/config。
- 本变更不要求一次迁移所有 Timeline 轨道；第一阶段以动画轨道和播放状态闭环为主。

## Current Reality

- `TimelineNode` 当前位于 `Assets/GameScripts/Main/Runtime/BTSMTL/Timeline/Scripts/Tree/TimelineNode.cs`，直接持有 runtime Timeline、TimelinePlayer 和播放完成状态。
- Timeline 播放权需要收进角色管线内部的 Timeline runtime owner。
- `CharacterPresentationStage` 负责消费 `PresentationOutput.AnimationContributions` 并应用到表现层。
- 旧 `AnimationCommand` 只表达 Animator 参数和 CrossFade，不足以表达动画层混合贡献。
- current spec `btsmtl-runnable-timeline-node` 仍要求 `TimelineNode` 映射 Timeline 播放生命周期，本变更将修改该要求。

## Impact

- `TimelineNode` 的运行语义会破坏性变化：从“播放器”变为“播放请求节点”。
- `CharacterGraphContext` 或等价管线上下文需要提供 Timeline 请求提交和状态查询接口。
- `TimelinePlaybackScheduler` 会成为 Timeline runtime 的唯一推进位置。
- Timeline 动画轨道需要从直接绑定 `TimelinePlayer` 改为在采样时输出动画命令。
- 表现层需要承担最终 Animator/PlayableGraph 应用和混合结果 snapshot 输出。
