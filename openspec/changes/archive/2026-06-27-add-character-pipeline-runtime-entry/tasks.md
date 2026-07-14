# Tasks

## 1. 目录和命名收口
- [x] 1.1 确认 `Assets/GameScripts/Main/Runtime/Character` 作为正式角色稳定运行时代码目录。
- [x] 1.2 确认旧 `Assets/Scripts` 和旧 `Charactor/Pipeline` 不再作为正式代码路径。
- [x] 1.3 若旧路径无有效代码，删除旧 `Charactor/Pipeline` 路径。
- [x] 1.4 创建 `Assets/GameScripts/Main/Runtime/Character/Pipeline` 目录。
- [x] 1.5 创建 `Input`、`Graph`、`Motion`、`Presentation`、`Network` 子目录。

## 2. Runner tick 源
- [x] 2.1 新增 `CharacterPipelineRunner`。
- [x] 2.2 让 runner 维护已注册 pipeline 列表。
- [x] 2.3 让 runner 提供注册接口。
- [x] 2.4 让 runner 提供反注册接口。
- [x] 2.5 让 runner 在 `Update` 中调用所有 pipeline 的 update phase。
- [x] 2.6 让 runner 在 `LateUpdate` 中调用所有 pipeline 的 late phase。
- [x] 2.7 明确 runner 不持有角色业务状态。

## 3. Host 装配点
- [x] 3.1 新增 `CharacterPipelineHost`。
- [x] 3.2 Host 序列化 `CharacterPipelineDefinition` 引用。
- [x] 3.3 Host 序列化 Animancer 引用。
- [x] 3.4 Host 序列化 CharacterController 引用。
- [x] 3.5 Host 不序列化 Timeline provider component。
- [x] 3.6 Host 序列化输入配置引用。
- [x] 3.7 Host 在 Awake 创建 `CharacterPipeline`。
- [x] 3.8 Host 在 OnEnable 注册 pipeline。
- [x] 3.9 Host 在 OnDisable 反注册 pipeline。
- [x] 3.10 Host 在 OnDestroy 释放 pipeline。
- [x] 3.11 Host 不实现动作状态判断或 motion 结算业务。
- [x] 3.12 Host 不直接序列化 BTSMTL RootTree 或 BTSMTL component 类型。

## 4. Pipeline 主体
- [x] 4.1 新增纯 C# `CharacterPipeline`。
- [x] 4.2 `CharacterPipeline` 不继承 MonoBehaviour。
- [x] 4.3 `CharacterPipeline` 不直接读取 `Time.deltaTime`。
- [x] 4.4 新增 `CharacterPipelineTickContext`。
- [x] 4.5 在 tick context 中加入 `DeltaTime`。
- [x] 4.6 在 tick context 中加入 `FrameIndex`。
- [x] 4.7 在 tick context 中加入 `SimulationTick`。
- [x] 4.8 在 tick context 中加入 `InputSequence`。
- [x] 4.9 在 tick context 中加入 `AuthorityMode`。
- [x] 4.10 新增 `CharacterPipelineFrame`。
- [x] 4.11 新增 `CharacterPipelineOutput`。
- [x] 4.12 将 output 拆成 `StrictGameplayOutput`。
- [x] 4.13 将 output 拆成 `PresentationOutput`。
- [x] 4.14 将 output 拆成 `NetworkOutput`。
- [x] 4.15 新增 update phase。
- [x] 4.16 新增 late phase。
- [x] 4.17 新增 frame transient clear。
- [x] 4.18 新增 dispose 入口。

## 5. Graph 执行上下文
- [x] 5.1 新增 `CharacterGraphContext`。
- [x] 5.2 让 graph context 实现 `ITimelinePlaybackService`。
- [x] 5.3 让 graph context 实现 `IInputActionValueSource`。
- [x] 5.4 让 graph context 维护 Timeline 播放请求和状态。
- [x] 5.5 让 graph context 直接读取 Host 注入的输入配置。
- [x] 5.6 禁止 graph context 通过场景搜索补齐缺失引用。
- [x] 5.7 让 BTSMTL RootTree 初始化时使用 graph context 作为 `BaseGraph.User`。
- [x] 5.8 让 graph context 暴露 `AuthorityMode`。
- [x] 5.9 让 graph context 暴露 `SimulationTick`。
- [x] 5.10 让 graph context 暴露 gameplay facts / tags / resources 的正式入口。
- [x] 5.11 让 graph context 暴露 server snapshot / correction 输入缓存入口。

## 6. BTSMTLPhase
- [x] 6.1 新增 `CharacterBTSMTLPhase`。
- [x] 6.2 新增内部 `BehaviorTreeRuntime`。
- [x] 6.3 新增内部 `TimelinePlaybackScheduler`。
- [x] 6.4 启动时实例化 Host 配置的 RootTree。
- [x] 6.5 对 RootTree 调用 `InitTree(graphContext)`。
- [x] 6.6 对 RootTree 调用 `OnSpawn()`。
- [x] 6.7 update phase 先调用 `UpdateTree(deltaTime)`。
- [x] 6.8 update phase 再推进 active Timeline playback。
- [x] 6.9 dispose 时先取消 Timeline playback。
- [x] 6.10 dispose 时调用 `OnUnspawn()` 和 `DisposeTree()`。
- [x] 6.11 保持 StateMachineNode、StateNode 和 TimelineNode 仍由 BTSMTL 原链路解释。

## 7. InputStage
- [x] 7.1 新增 `CharacterInputStage`。
- [x] 7.2 新增 `CharacterInputSnapshot`。
- [x] 7.3 第一版支持 Vector2、Float、Button 读取。
- [x] 7.4 输入读取只进入 frame/context，不直接驱动 Transform。
- [x] 7.5 InputAction ValueNode 通过 graph context 读取同一输入来源。
- [x] 7.6 不新增独立输入 Graph 或 Workbench 输入路径。

## 8. MotionStage
- [x] 8.1 新增 `MotionIntent`。
- [x] 8.2 新增 `MotionResult`。
- [x] 8.3 新增 `CharacterMotionStage`。
- [x] 8.4 第一版允许没有 motion proposal 时输出空 motion。
- [x] 8.5 第一版只通过 MotionStage 应用位移。
- [x] 8.6 节点、Timeline 和 Graph 不直接修改 Transform。

## 9. PresentationStage
- [x] 9.1 新增 `AnimationContribution`。
- [x] 9.2 新增 `PresentationCue`。
- [x] 9.3 新增 `CharacterPresentationStage`。
- [x] 9.4 第一版允许没有 animation command 时不做动画操作。
- [x] 9.5 PresentationStage 只消费 output，不反向改变图决策。

## 10. NetworkStage
- [x] 10.1 新增 `CharacterAuthorityMode`。
- [x] 10.2 定义 `LocalPredicted` 模式。
- [x] 10.3 定义 `RemoteProxy` 模式。
- [x] 10.4 定义 `PresentationOnly` 模式。
- [x] 10.5 新增 `CharacterNetworkReceiveStage`。
- [x] 10.6 新增 `CharacterNetworkSendStage`。
- [x] 10.7 新增 `ClientCommand` 占位结构。
- [x] 10.8 新增 `ServerSnapshot` 占位结构。
- [x] 10.9 新增 `ConfirmedEvent` 占位结构。
- [x] 10.10 新增 `Correction` 占位结构。
- [x] 10.11 NetworkReceiveStage 第一版只读取已注入缓存，不接真实 transport。
- [x] 10.12 NetworkSendStage 第一版只收集 `NetworkOutput`，不发送真实消息。
- [x] 10.13 NetworkStage 不直接写 BTSMTL 节点。
- [x] 10.14 NetworkStage 不直接修改 Transform。

## 11. Timeline playback tick 权威
- [x] 11.1 审查 `TimelinePlayer.FixedUpdate` 自主评估对 pipeline 的影响。
- [x] 11.2 选择正式外部 tick 策略。
- [x] 11.3 避免 `TimelineNode` 和 `TimelinePlayer.FixedUpdate` 在同一帧重复推进。
- [x] 11.4 保持 `TimelineNode` 只通过 `ITimelinePlaybackService` 提交 Timeline 请求。

## 12. 清理和一致性
- [x] 12.1 确认没有恢复 BBB `PlayerBaseState`。
- [x] 12.2 确认没有恢复 BBB `PlayerStateRegistry`。
- [x] 12.3 确认没有恢复旧 `PlayerSO/LocomotionSO/ActionSO` 动作数据源。
- [x] 12.4 确认没有新增 fallback 配置。
- [x] 12.5 确认没有新增并行 Graph/Workbench runtime。
- [x] 12.6 确认没有把真实 transport、Fantasy handler 或服务端裁决写进本 change。
