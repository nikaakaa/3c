# Design

## 设计目标
`CharacterPipeline` 的目标不是替代 BTSMTL 图，也不是复制 BBB 的代码状态机。它是 Goal 文档中混合架构的客户端动作运行主线：本地玩家预测，远端玩家快照插值，PvP 命中服务端权威加 combat rewind，PvE/目标点服务端权威，客户端平滑修正。

```text
CharacterPipelineRunner
-> CharacterPipeline
   -> NetworkReceiveStage
   -> InputStage
   -> CharacterBTSMTLPhase
      -> RootTree.UpdateTree(deltaTime)
      -> StateMachineNode
      -> StateMachineGraphRuntime
      -> StateNode.SubTree / StateBehaviorSubTree
      -> TimelineNode
      -> TimelinePlaybackScheduler
   -> MotionStage
   -> PresentationStage
   -> NetworkSendStage
   -> FrameEndCleanup
```

第一版可以不接真实 transport，但不能没有 `NetworkStage` 和权威边界。网络晚实现，结构现在定。

## 关键对象

### CharacterPipelineRunner
全局 tick 源。它是少量 MonoBehaviour 之一，负责统一调用所有已注册 `CharacterPipeline`：

- `Update` 阶段：接收网络输入缓存、输入采样、意图转换、BTSMTL 图 tick、Timeline/window/cue 输出收集。
- `LateUpdate` 阶段：Motion 结算、strict gameplay output 写入、Presentation 应用、network output 收集、帧末清理。

它不持有角色业务配置，不创建状态，不读取节点细节。

### CharacterPipelineHost
每个角色一个装配点。它只负责：

- 序列化角色管线定义和 Unity 组件引用，例如 `CharacterPipelineDefinition`、Animancer、CharacterController、输入配置。
- 不直接暴露 BTSMTL RootTree 或 BTSMTL component 类型到角色 prefab 的 Host 字段。
- 创建 `CharacterPipeline` 和 `CharacterGraphContext`。
- `OnEnable` 注册到 runner。
- `OnDisable` 反注册。
- `OnDestroy` 释放 pipeline 和图实例。

它不写状态转换、不判断动作、不直接处理 combat。

### CharacterPipeline
纯 C# 主体，不继承 MonoBehaviour，不自己 tick。它接收 runner 传入的 tick context，执行阶段：

- `UpdatePhase(context)`：输入、图决策、Timeline 输出收集。
- `LatePhase(context)`：motion、strict output、presentation、network output、帧末清理。
- `Dispose()`：释放图实例和运行时缓存。

### CharacterGraphContext
传给 BTSMTL `BaseGraph.InitTree(user)` 的正式 user。它直接实现当前 BTSMTL 节点需要的运行接口：

- `ITimelinePlaybackService`
- `IInputActionValueSource`
- authority mode / network tick context
- gameplay facts / tags / resources
- server snapshot / correction 缓存入口
- network command output 收集入口

这样 `TimelineNode`、InputAction ValueNode 和后续节点都从同一正式上下文读取数据，不需要场景搜索，也不新增 fallback。

## 混合架构边界

当前技术选择不是纯服务器权威、全局帧同步、完整世界 rollback 或客户端权威，而是：

```text
Server-authoritative result
+ client prediction
+ snapshot interpolation
+ combat rewind
+ correction smoothing
```

角色管线必须按对象和数据分权：

| 对象或系统 | 权威 | Pipeline 职责 |
|---|---|---|
| 本地玩家移动 | 客户端预测 + 服务端校正 | 立即执行本地 MotionIntent，接收 correction 后平滑修正 |
| 本地动作启动 | 客户端预测 + 服务端确认 | 立即推进 Graph/Timeline，输出 action request 和 phase |
| 远端玩家 | 服务器快照 | 不完整重跑本地图，使用 snapshot/interpolation 驱动表现 |
| PvP 命中 | 服务端权威 + combat rewind | 本地产生窗口和 hit candidate，最终等 confirmed event |
| PvE/目标点 | 服务端权威 | 本地表现预测，目标归属和结果等服务端确认 |
| 相机/VFX/SFX | 客户端表现 | local-only，不进入 strict gameplay output |

## 运行时数据流

```text
NetworkReceiveStage
-> InputStage
-> CharacterInputSnapshot
-> CharacterPipelineFrame
-> CharacterBTSMTLPhase
-> BTSMTL RootTree / StateMachine / TimelinePlaybackScheduler
-> CharacterPipelineOutput
   -> StrictGameplayOutput
   -> PresentationOutput
   -> NetworkOutput
-> MotionStage / PresentationStage / NetworkSendStage
```

第一版输出可以很薄，但必须区分类型：

- `StrictGameplayOutput`：active state、action id、action phase、motion result、gameplay windows、combat sample 等后续可同步/可校验字段。
- `PresentationOutput`：animation contribution、VFX、SFX、Camera cue、hit stop、screen effect 等 local-only 表现字段。
- `NetworkOutput`：client command、input sequence、action request、motion snapshot、window digest、correction acknowledgement 等后续 transport 消费字段。

`CharacterPipelineTickContext` 第一版至少包含：

- `DeltaTime`
- `FrameIndex`
- `SimulationTick`
- `InputSequence`
- `AuthorityMode`

`AuthorityMode` 第一阶段至少定义：

- `LocalPredicted`
- `RemoteProxy`
- `PresentationOnly`

## Timeline tick 权威
`TimelineNode` 通过 `Owner.TryGetUser(out ITimelinePlaybackService)` 向 `CharacterGraphContext` 提交播放请求，`CharacterBTSMTLPhase` 内部的 `TimelinePlaybackScheduler` 维护 active playback 并采样轨道。

因此 `CharacterPipelineRunner` 应成为 Timeline/动画图评估的上层 tick 权威。`TimelinePlayer` 或等价 PlayableGraph adapter 只能位于表现层边界，不应该在自己的 `FixedUpdate` 中再次推进同一帧。

后续实现需要选择一个正式策略：

- 给 `TimelinePlayer` 增加外部 tick 模式，pipeline host 装配时启用。
- 或移除 `TimelinePlayer.FixedUpdate` 自主评估，让所有运行时入口显式调用。

取舍：

- 外部 tick 模式影响面较小，但会多一个正式模式概念。
- 移除自主评估更干净，但会影响已有非 pipeline 使用方式，需要同步迁移所有调用点。

因为当前项目强调清理和统一，最终应收敛为 pipeline/runner 显式 tick，而不是长期保留两条 tick 权威。

## NetworkStage 定位

`NetworkStage` 在本 change 中只建立正式位置和数据边界，不接真实 Fantasy transport。

- `NetworkReceiveStage`：读取已经进入 pipeline 的 `ServerSnapshot`、`ConfirmedEvent`、`Correction` 或远端插值输入缓存。
- `NetworkSendStage`：从 `NetworkOutput` 收集 `ClientCommand`、`InputSequence`、`ActionState`、`GameplayWindow` 摘要和 correction ack。

它不能直接写 BTSMTL 节点，不能直接改 Transform，也不能绕过 `MotionStage` 和 `PresentationStage`。真实网络接入应在后续 network change 中实现。

## BBB 参考取舍

### 借鉴
- 单根入口装配思路。
- 输入先清洗再进入决策。
- Update 和 LateUpdate 分阶段。
- 帧末清理 transient intent/output。
- 统一 Motion driver/stage 结算最终位移。

### 不借鉴
- `PlayerBaseState` 代码继承状态机。
- `PlayerStateRegistry` 特化状态注册。
- `PlayerSO/LocomotionSO/ActionSO` 作为动作语义数据源。
- 多个上半身、表情、动作、音频 controller 早期拆散主链路。
- 状态类直接调用动画 facade 或 motion driver。

## 路径命名
当前项目中存在旧拼写 `Charactor` 的历史记录。新代码应放入 TEngine 主程序稳定层：

```text
Assets/GameScripts/Main/Runtime/Character/Pipeline
```

实现阶段如果旧 `Charactor/Pipeline` 为空或只剩 meta，应删除或迁移，不继续扩展错误命名。

## 与现有 BTSMTL specs 的关系
- `btsmtl-sm-node-authoring` 仍负责状态机 authoring 和 runtime 解释。
- `btsmtl-runnable-timeline-node` 仍负责 `TimelineNode` 生命周期和 Timeline 播放。
- `btsmtl-input-action-node-authoring` 仍负责 InputAction ValueNode。
- 本变更只提供角色 runtime context 和调度入口，不改变 BTSMTL 节点创作模型。
- 本变更会让 `CharacterPipeline` 成为后续网络 prediction、snapshot interpolation、combat rewind 和 correction smoothing 的客户端接入点，但不实现真实 transport。
