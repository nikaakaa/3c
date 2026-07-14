# Proposal: 打通角色 Gameplay 管线闭环

## Why

当前项目已经有 BTSMTL authoring、`CharacterPipeline`、`ActionRuntime`、`TimelinePlaybackScheduler`、`CharacterMotionStage`、`CharacterPresentationStage`、`SyncFacts` 和 `GameplaySyncRuntime` 的主要形状，但这些模块还没有被规划成一个可交付的 2v2vE demo 纵切闭环：

- `refactor-character-motion-arbitration` 还未完成，motion 来源之间的业务优先级仍然缺少正式实现。
- `CharacterPipeline` 已经按 network receive、action resolve、input、BTSMTL、motion、network send 分阶段执行，但 demo 级链路还没有明确“哪些事实必须在一帧内从 authoring 走到 sync/debug”。
- `ActionInstance`、`ActionWindow`、`GameplayResult` 三层事实已经出现，但缺少一个 demo 闭环要求把输入请求、动作事务、Timeline 输出、motion、表现、同步策略和调试串起来。
- 网络当前以 `None` 和 `LocalLoopback` 后端为第一阶段，适合做网络压力口径验证，但还缺少面向 2v2vE demo 的最小 actor/objective/result 闭环规划。
- 如果继续只按单点能力推进，容易出现动作能播、motion 能动、网络能收包，但无法讲清一条动作从输入到服务端结果再到修正表现的完整工程链路。

求职 demo 的业务目标不是做完整 PvPvE 产品，也不是做通用编辑器产品；这次规划的目标是把当前正式主线打通成一个能被玩到、看到、调试到、讲清楚的动作客户端纵切。

## What Changes

新增一条 `character-gameplay-pipeline-closure` 能力，定义第一阶段角色 Gameplay 闭环：

- `CharacterPipelineDefinition` 作为 authoring 装配入口，必须把输入配置、RootTree、ActionProfile、动画 layer 和后续 demo 事实配置汇入同一角色管线。
- 输入值和 ActionRequest 必须进入 `CharacterInputFrame`、`CharacterGraphContext` 和 `SyncFacts`，不得暴露 `ClientCommand` 给 BTSMTL 作者心智。
- Graph / StateMachine 负责动作决策，必须通过 `ActionActivationRequest` 进入 `ActionRuntime`，并生成 `ActionInstance` 和显式 Action Context。
- Timeline 只输出动画贡献、root motion contribution、motion warp window、action window、cue 和 action motion sample，不直接改 Transform、不直接判命中。
- Motion 闭环依赖 `refactor-character-motion-arbitration`，最终顺序必须是 contribution resolve、motion modifier、network correction phase、Move、MotionResult。
- Presentation 闭环必须从 `AnimationContribution` 到 `AnimationLayerPlaybackPlan` 再到 Animancer adapter，不新增 Timeline 自主播放路径。
- Sync 闭环必须从 `SyncFacts` 到 `CharacterNetworkSendStage`、`CharacterGameplaySyncAdapter`、`GameplaySyncRuntime`，第一阶段只要求 `None` 和 `LocalLoopback` 后端。
- Debug 闭环必须能按 ActionInstance 展示输入、激活、Timeline 输出、motion resolve、窗口、cue、gameplay result、policy decision 和 correction。
- 2v2vE demo 第一阶段只做角色动作压力闭环和最小目标/结果事件，不做完整匹配、账号、背包、大地图、完整怪物 AI 或真实 Fantasy 服务端裁决。

## Non-Goals

- 不实现真实 Fantasy transport 或完整权威服务端。
- 不实现完整 PvPvE 产品功能、匹配、账号、背包、大地图、多职业或完整断线重连。
- 不做通用编辑器商业产品，不让作者配置任意网络策略或任意 motion 公式。
- 不恢复旧 Workbench、旧 `Assets/Scripts`、旧 `Charactor`、旧 locomotion/action SO、footphase、bodyclaim、AnimationPresentationPolicy 或 FrameSyncAuthority。
- 不新增第二套角色控制器、第二套 tick、第二套 Timeline 播放器、第二套网络输出或临时桥接路径。
- 不把手动 Unity 端到端验证写入 tasks。

## 当前代码事实

- `CharacterPipeline.LogicTick` 当前顺序是 `BeginFrame -> GraphContext.BeginFrame -> NetworkReceiveStage.Collect -> ActionNetworkResolveStage.Resolve -> InputStage.Update -> BTSMTLPhase.Tick -> MotionStage.Update -> NetworkSendStage.Collect`。
- `CharacterGraphContext` 已经提供 input value 读取、request 查询/消费、Timeline 播放请求、ActionRuntime、Action Context、blackboard、SyncFacts 提交入口。
- `ActionRuntime` 已经注册 ActionProfile、激活 ActionInstance、记录 lifecycle/window/motion/cue/gameplay result debug 输出。
- `TimelinePlaybackScheduler` 已经采样 animation、root motion、motion warp、ActionWindow、ActionCue，并提交 `ActionMotionSample`。
- `CharacterMotionStage` 当前仍有 resolver 前 correction 硬设 Transform，需由 `refactor-character-motion-arbitration` 处理。
- `CharacterGameplaySyncAdapter` 已经从 `SyncFacts` 收集 client command、activation、lifecycle、window、motion、cue、gameplay result、state effect 和 correction ack，并写入 `GameplaySyncRuntime`。
- `CharacterGameplaySyncDriver` 当前只支持正式 `None` 和 `LocalLoopback` 后端，没有真实 Fantasy 后端。

## 决策和 Tradeoff

### 方案 A：先只做本地动作手感

- 优点：最快能在 Unity 里玩到攻击、移动和动画。
- 缺点：后续网络预测、确认、拒绝、纠偏和 debug 会返工，Timeline 和动作事实容易被写成本地表现脚本。
- 业务取舍：适合纯单机动作 demo，不适合当前 `Network-aware Third Person Action Combat Prototype` 的求职口径。

### 方案 B：先做网络框架和服务端

- 优点：架构看起来完整，后续可以接真实 Fantasy。
- 缺点：会把时间花在 transport、匹配、服务端工程和协议细节上，削弱第三人称动作客户端展示重点。
- 业务取舍：适合 Network Engineer 展示，不适合当前 Gameplay 客户端程序 demo。

### 方案 C：以 ActionInstance 事实链打通客户端纵切

- 优点：输入、动作事务、Timeline、motion、表现、SyncFacts、loopback 和 debug 能形成一条可解释链路；真实 Fantasy 可以作为后续 Adapter 替换，而不是改 gameplay 语义。
- 缺点：需要按阶段补齐多个模块的输入输出合同，对任务拆分和顺序要求高。
- 业务取舍：最贴合求职目标，能展示动作手感，也能展示网络压力下的工程设计。

本 proposal 选择方案 C。

## 与现有 Spec 的关系

- 与 `character-pipeline-runtime` 一致：继续使用 `CharacterPipeline` 作为纯 C# 主体和阶段调度入口，不新增角色控制分裂路径。
- 与 `character-action-activation-flow` 一致：动作必须通过 `ActionActivationRequest -> ActionRuntime -> ActionInstance / Action Context`，Timeline 只是动作输出来源。
- 与 `character-motion-semantics` 和 `character-root-motion-curves` 一致：节点和 Timeline 不直接结算 Transform，root motion 必须走正式 motion 管线。
- 与 active change `refactor-character-motion-arbitration` 存在明确依赖：本闭环不重复定义 motion channel 细节，要求该 change 完成后再做完整 demo 运动闭环。
- 与 `character-gameplay-sync-adapter`、`gameplay-sync-runtime`、`gameplay-sync-backend-selection` 一致：第一阶段只走 `None` / `LocalLoopback`，不新增 fake Fantasy 配置。
- 未发现需要推翻 current spec 的矛盾；当前主要缺口是“能力已经分散存在，但缺少 demo 纵切闭环规格和实施顺序”。
