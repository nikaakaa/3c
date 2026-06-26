# Design

## 设计目标
`CharacterPipeline` 的目标不是替代 Taco 图，也不是复制 BBB 的代码状态机。它是角色每帧 gameplay runtime 的解释和结算入口：

```text
CharacterPipelineRunner
-> CharacterPipeline
   -> InputStage
   -> GraphStage
      -> RootTree.UpdateTree(deltaTime)
      -> StateMachineNode
      -> StateMachineGraphRuntime
      -> StateNode.SubTree / StateBehaviorSubTree
      -> TimelineNode
   -> MotionStage
   -> PresentationStage
   -> ClearFrame
```

## 关键对象

### CharacterPipelineRunner
全局 tick 源。它是少量 MonoBehaviour 之一，负责统一调用所有已注册 `CharacterPipeline`：

- `Update` 阶段：输入采样、意图转换、Taco 图 tick、输出收集。
- `LateUpdate` 阶段：Motion 结算、Presentation 应用、帧末清理。

它不持有角色业务配置，不创建状态，不读取节点细节。

### CharacterPipelineHost
每个角色一个装配点。它只负责：

- 序列化 Unity 引用，例如 Taco RootTree、Animator、CharacterController、TimelinePlayer、InputActionAsset。
- 创建 `CharacterPipeline` 和 `CharacterPipelineGraphContext`。
- `OnEnable` 注册到 runner。
- `OnDisable` 反注册。
- `OnDestroy` 释放 pipeline 和图实例。

它不写状态转换、不判断动作、不直接处理 combat。

### CharacterPipeline
纯 C# 主体，不继承 MonoBehaviour，不自己 tick。它接收 runner 传入的 tick context，执行阶段：

- `UpdatePhase(context)`：输入、图决策、Timeline 输出收集。
- `LatePhase(context)`：motion、presentation、帧末清理。
- `Dispose()`：释放图实例和运行时缓存。

### CharacterPipelineGraphContext
传给 Taco `BaseGraph.InitTree(user)` 的正式 user。它直接实现当前 Taco 节点需要的运行接口：

- `ITimelinePlayerProvider`
- `IInputActionValueSource`

这样 `TimelineNode`、InputAction ValueNode 和后续节点都从同一正式上下文读取数据，不需要场景搜索，也不新增 fallback。

## 运行时数据流

```text
输入设备
-> CharacterInputStage
-> CharacterInputSnapshot
-> CharacterPipelineFrame
-> CharacterGraphStage
-> Taco RootTree / StateMachine / Timeline
-> CharacterPipelineOutput
-> MotionStage / PresentationStage
```

第一版输出可以很薄，但必须区分类型：

- `MotionProposal`：节点或 Timeline 产生的移动意图。
- `MotionResult`：MotionStage 结算后的最终位移结果。
- `AnimationCommand`：动画播放或参数命令。
- `GameplayWindowFact`：攻击、无敌、取消等窗口事实，第一版可为空结构。
- `PresentationCue`：VFX、SFX、Camera cue，第一版可为空结构。
- `NetworkSyncPayload`：预留结构，第一版不实现网络发送。

## Timeline tick 权威
现有 `TimelineNode` 会通过 `Owner.TryGetUser(out ITimelinePlayerProvider)` 获取 `TimelinePlayer`，再在节点生命周期内调用 `Timeline.Evaluate(deltaTime)` 和 `TimelinePlayer.EvaluatePlayableGraph(deltaTime)`。

因此 `CharacterPipelineRunner` 应成为 Timeline/动画图评估的上层 tick 权威。`TimelinePlayer` 在角色 pipeline 模式下只作为 provider 和 PlayableGraph adapter，不应该在自己的 `FixedUpdate` 中再次推进同一帧。

后续实现需要选择一个正式策略：

- 给 `TimelinePlayer` 增加外部 tick 模式，pipeline host 装配时启用。
- 或移除 `TimelinePlayer.FixedUpdate` 自主评估，让所有运行时入口显式调用。

取舍：

- 外部 tick 模式影响面较小，但会多一个正式模式概念。
- 移除自主评估更干净，但会影响已有非 pipeline 使用方式，需要同步迁移所有调用点。

因为当前项目强调清理和统一，最终应收敛为 pipeline/runner 显式 tick，而不是长期保留两条 tick 权威。

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
当前项目中存在旧拼写 `Assets/Scripts/Charactor`。新代码应放入：

```text
Assets/Scripts/Character/Pipeline
```

实现阶段如果旧 `Charactor/Pipeline` 为空或只剩 meta，应删除或迁移，不继续扩展错误命名。

## 与现有 Taco specs 的关系
- `taco-sm-node-authoring` 仍负责状态机 authoring 和 runtime 解释。
- `taco-runnable-timeline-node` 仍负责 `TimelineNode` 生命周期和 Timeline 播放。
- `taco-input-action-node-authoring` 仍负责 InputAction ValueNode。
- 本变更只提供角色 runtime context 和调度入口，不改变 Taco 节点创作模型。
