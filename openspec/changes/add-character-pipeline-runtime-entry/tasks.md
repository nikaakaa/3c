# Tasks

## 1. 目录和命名收口
- [ ] 1.1 确认 `Assets/Scripts/Character` 作为正式角色运行时代码目录。
- [ ] 1.2 确认旧 `Assets/Scripts/Charactor/Pipeline` 是否为空或只剩 meta。
- [ ] 1.3 若旧路径无有效代码，删除旧 `Charactor/Pipeline` 路径。
- [ ] 1.4 创建 `Assets/Scripts/Character/Pipeline` 目录。
- [ ] 1.5 创建 `Input`、`Graph`、`Motion`、`Presentation` 子目录。

## 2. Runner tick 源
- [ ] 2.1 新增 `CharacterPipelineRunner`。
- [ ] 2.2 让 runner 维护已注册 pipeline 列表。
- [ ] 2.3 让 runner 提供注册接口。
- [ ] 2.4 让 runner 提供反注册接口。
- [ ] 2.5 让 runner 在 `Update` 中调用所有 pipeline 的 update phase。
- [ ] 2.6 让 runner 在 `LateUpdate` 中调用所有 pipeline 的 late phase。
- [ ] 2.7 明确 runner 不持有角色业务状态。

## 3. Host 装配点
- [ ] 3.1 新增 `CharacterPipelineHost`。
- [ ] 3.2 Host 序列化 Taco RootTree 引用。
- [ ] 3.3 Host 序列化 Animator 引用。
- [ ] 3.4 Host 序列化 CharacterController 引用。
- [ ] 3.5 Host 序列化 TimelinePlayer 引用。
- [ ] 3.6 Host 序列化 InputActionAsset 引用。
- [ ] 3.7 Host 在 Awake 创建 `CharacterPipeline`。
- [ ] 3.8 Host 在 OnEnable 注册 pipeline。
- [ ] 3.9 Host 在 OnDisable 反注册 pipeline。
- [ ] 3.10 Host 在 OnDestroy 释放 pipeline。
- [ ] 3.11 Host 不实现动作状态判断或 motion 结算业务。

## 4. Pipeline 主体
- [ ] 4.1 新增纯 C# `CharacterPipeline`。
- [ ] 4.2 `CharacterPipeline` 不继承 MonoBehaviour。
- [ ] 4.3 `CharacterPipeline` 不直接读取 `Time.deltaTime`。
- [ ] 4.4 新增 `CharacterPipelineTickContext`。
- [ ] 4.5 新增 `CharacterPipelineFrame`。
- [ ] 4.6 新增 `CharacterPipelineOutput`。
- [ ] 4.7 新增 update phase。
- [ ] 4.8 新增 late phase。
- [ ] 4.9 新增 frame transient clear。
- [ ] 4.10 新增 dispose 入口。

## 5. Graph 执行上下文
- [ ] 5.1 新增 `CharacterPipelineGraphContext`。
- [ ] 5.2 让 graph context 实现 `ITimelinePlayerProvider`。
- [ ] 5.3 让 graph context 实现 `IInputActionValueSource`。
- [ ] 5.4 让 graph context 直接持有 Host 注入的 TimelinePlayer。
- [ ] 5.5 让 graph context 直接读取 Host 注入的 InputActionAsset。
- [ ] 5.6 禁止 graph context 通过场景搜索补齐缺失引用。
- [ ] 5.7 让 Taco RootTree 初始化时使用 graph context 作为 `BaseGraph.User`。

## 6. GraphStage
- [ ] 6.1 新增 `CharacterGraphStage`。
- [ ] 6.2 启动时实例化 Host 配置的 RootTree。
- [ ] 6.3 对 RootTree 调用 `InitTree(graphContext)`。
- [ ] 6.4 对 RootTree 调用 `OnSpawn()`。
- [ ] 6.5 update phase 调用 `UpdateTree(deltaTime)`。
- [ ] 6.6 dispose 时调用 `OnUnspawn()`。
- [ ] 6.7 dispose 时调用 `DisposeTree()`。
- [ ] 6.8 保持 StateMachineNode、StateNode 和 TimelineNode 仍由 Taco 原链路解释。

## 7. InputStage
- [ ] 7.1 新增 `CharacterInputStage`。
- [ ] 7.2 新增 `CharacterInputSnapshot`。
- [ ] 7.3 第一版支持 Vector2、Float、Button 读取。
- [ ] 7.4 输入读取只进入 frame/context，不直接驱动 Transform。
- [ ] 7.5 InputAction ValueNode 通过 graph context 读取同一输入来源。
- [ ] 7.6 不新增独立输入 Graph 或 Workbench 输入路径。

## 8. MotionStage
- [ ] 8.1 新增 `MotionProposal`。
- [ ] 8.2 新增 `MotionResult`。
- [ ] 8.3 新增 `CharacterMotionStage`。
- [ ] 8.4 第一版允许没有 motion proposal 时输出空 motion。
- [ ] 8.5 第一版只通过 MotionStage 应用位移。
- [ ] 8.6 节点、Timeline 和 Graph 不直接修改 Transform。

## 9. PresentationStage
- [ ] 9.1 新增 `AnimationCommand`。
- [ ] 9.2 新增 `PresentationCue`。
- [ ] 9.3 新增 `CharacterPresentationStage`。
- [ ] 9.4 第一版允许没有 animation command 时不做动画操作。
- [ ] 9.5 PresentationStage 只消费 output，不反向改变图决策。

## 10. TimelinePlayer tick 权威
- [ ] 10.1 审查 `TimelinePlayer.FixedUpdate` 自主评估对 pipeline 的影响。
- [ ] 10.2 选择正式外部 tick 策略。
- [ ] 10.3 避免 `TimelineNode` 和 `TimelinePlayer.FixedUpdate` 在同一帧重复推进。
- [ ] 10.4 保持 `TimelineNode` 仍通过 `ITimelinePlayerProvider` 获得 TimelinePlayer。

## 11. 清理和一致性
- [ ] 11.1 确认没有恢复 BBB `PlayerBaseState`。
- [ ] 11.2 确认没有恢复 BBB `PlayerStateRegistry`。
- [ ] 11.3 确认没有恢复旧 `PlayerSO/LocomotionSO/ActionSO` 动作数据源。
- [ ] 11.4 确认没有新增 fallback 配置。
- [ ] 11.5 确认没有新增并行 Graph/Workbench runtime。
