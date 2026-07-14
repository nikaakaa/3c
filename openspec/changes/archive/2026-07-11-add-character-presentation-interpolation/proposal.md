# Change: 增加角色表现插值层

## Why

当前角色管线已经区分 `LogicTick` 和 `PresentationFrame`，`GameplayTickSystem` 也已经提供 `InterpolationAlpha`。但角色侧还没有正式消费这个 alpha：动画表现只应用最近一次 logic tick 的 `clipTime`，motion 也直接使用 `CharacterController` 的逻辑 Transform 作为渲染看到的位置。

这会让当前 60Hz 本地逻辑 tick 看起来勉强可用，但如果后续把本地逻辑或网络快照降到 20/30Hz，动画和角色位移都会暴露离散阶梯。项目目标是动作客户端 demo，不能靠提高逻辑 tick 频率假装表现平滑，需要把“事实层”和“表现层”拆干净。

## What Changes

- 新增正式 `character-presentation-interpolation` 能力，定义角色表现插值层的输入、输出和边界。
- `CharacterPipeline` 在 logic tick 产出严格事实后，必须把本 tick 的 motion pose 和 animation playback sample 记录为表现历史。
- `PresentationFrame` 使用 `GameplayPresentationFrameContext.InterpolationAlpha` 在最近两个 logic samples 之间生成 visual pose 和 visual animation plan。
- Motion 插值只应用到正式 visual root / model root，不反写 `CharacterController`、Graph、Timeline、MotionStage 或 SyncFacts。
- 动画插值只影响 Animancer adapter 使用的显示 `clipTime`、`normalizedTime` 和 `weight`，不改变 Timeline 播放时间、window/cue/root motion 事实。
- `CharacterPipelineHost` 需要显式提供 visual root / model root 绑定；缺失时报告正式配置错误，不自动把 logic root 当 fallback visual root。
- 保持 `CharacterController` / logic root 作为碰撞、判定、网络预测和 motion correction 的逻辑真值。

## Out Of Scope

- 不改变 `GameplayTickSystem` 的 accumulator、catch-up 或默认 tick rate。
- 不实现真实远端 actor snapshot interpolation；本 change 只定义并落地角色本地表现插值基础设施，后续远端快照可复用同一 visual pose contract。
- 不修改 action window、cue、root motion、motion warp 或 SyncFacts 的 logic tick 发生时机。
- 不新增测试；只做编译和 OpenSpec 校验。
- 不新增 fallback 配置、兼容路径或第二套动画/motion 播放链路。

## Current Spec Comparison

- `gameplay-tick-system` 已要求 `PresentationFrame` 每表现帧推进，并能读取 interpolation alpha；本 change 补齐角色如何消费该 alpha。
- `character-pipeline-runtime` 已要求 `PresentationFrame` 推进表现层、动画应用、cue 和插值；当前实现尚未记录跨 tick 表现样本，本 change 与该方向一致。
- `character-animation-layer-runtime` 已要求 Animancer 只是最终播放 adapter；本 change 不让 Animancer 自主播放，只给它 visual playback plan。
- `character-motion-semantics` 已要求最终 Transform 由 `CharacterMotionStage` 结算；本 change 不改变逻辑 Move，只新增 visual root 的表现插值。
- `character-network-sync-domain-contract` 已要求 MotionSyncDomain 处理连续运动同步；本 change 不把表现插值写入 SyncFacts，避免表现层污染网络事实。

没有发现和现行 spec 的直接矛盾；现行缺口是缺少“logic pose / visual pose 分离”和“animation visual sample 插值”的正式要求。

## Impact

- 影响角色 pipeline runtime、motion output、presentation stage、Animancer adapter 和 Host 场景绑定。
- Corin 资产后续需要在角色 Host 上配置正式 visual root / model root。
- 现有 Timeline、Action、StateMachine 和黑板 authoring 语义不需要改变。
