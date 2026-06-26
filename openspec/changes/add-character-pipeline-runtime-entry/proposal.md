# Change: add-character-pipeline-runtime-entry

## Summary
新增 `CharacterPipeline` 运行时入口规划，让角色 gameplay runtime 从 Taco authoring 数据启动，而不是恢复 BBB 的代码状态机、旧 `PlayerSO/LocomotionSO/ActionSO` 或多个 MonoBehaviour 控制器链路。

该变更建立三层职责：

- `CharacterPipelineRunner`：全局 tick 源，统一调度所有角色管线。
- `CharacterPipelineHost`：每个角色的 Unity 装配点，只负责创建、注册和释放 pipeline。
- `CharacterPipeline`：纯 C# 角色动作管线，负责输入、Taco 图、Timeline、Motion、Presentation 的每帧阶段调度。

## Motivation
当前 Taco authoring 底座已经能表达 `RootTree -> StateMachineNode -> StateMachineGraph -> StateNode -> SubTree/StateBehaviorSubTree -> TimelineNode`，但角色 runtime 还没有一个正式入口承载这条链路。没有入口时，图只能在编辑器或零散 runner 中被验证，动作、移动、动画和后续网络同步没有统一落点。

BBB 参考项目提供了一个有价值的形状：单根 MonoBehaviour 装配、输入清洗、处理管线、运动驱动、帧末清理。但 BBB 的业务核心是代码继承状态机和大量 SO 配置，这与当前目标冲突。本变更只吸收它的运行时调度思想，不复制它的数据源和特化状态类。

## Goals
- 建立一个正式的角色运行时入口，让 Taco RootTree 可以在角色管线中被 tick。
- 使用单例/全局 runner 统一 tick，不让每个 `CharacterPipeline` 自己决定什么时候执行。
- 让 `CharacterPipelineHost` 只做 Unity 引用装配和 pipeline 注册，不承担业务逻辑。
- 让 `CharacterPipeline` 作为纯 C# 主体，分阶段处理 input、graph、motion、presentation 和帧末清理。
- 让 Taco `BaseGraph.User` 获得正式执行上下文，直接提供 `ITimelinePlayerProvider` 和 `IInputActionValueSource`。
- 明确 Timeline 和动画 tick 权威：由 `CharacterPipelineRunner` 驱动，不依赖 `TimelinePlayer.FixedUpdate` 等自主 tick。
- 保持旧 locomotion/action/footphase/bodyclaim 等 SO/config 不作为当前数据源。
- 将新运行时代码放入正确命名的 `Assets/Scripts/Character/Pipeline` 路径；不继续扩展旧拼写 `Charactor` 路径。

## Non-Goals
- 不实现完整网络同步、rollback、服务端裁决或 Combat Rewind。
- 不实现完整 MotionResolver、碰撞、坡度、root motion warp 或动画混合树。
- 不恢复 BBB 的 `PlayerBaseState`、`PlayerStateRegistry`、`PlayerSO` 动作配置或特化 locomotion 状态类。
- 不新增 Workbench、并行端口系统、并行 graph runtime 或 fallback 配置。
- 不把 Timeline 升级成状态机，也不让 Timeline 直接裁决命中、伤害或最终位移。
- 不新增测试任务；用户会在 Unity 中做端到端验证。

## Impact
- 新增 `character-pipeline-runtime` 能力规格。
- 后续实现会新增角色 pipeline 文件夹和少量运行时类。
- 后续实现可能需要调整 `TimelinePlayer` 的自主 tick 行为，使其能作为被 pipeline 外部驱动的 provider，而不是自己在 `FixedUpdate` 中推进。
- 后续实现需要清理或停止扩展旧 `Assets/Scripts/Charactor/Pipeline` 空路径，统一迁移到 `Assets/Scripts/Character/Pipeline`。

## Open Questions
- 第一版是否直接让 `CharacterPipelineGraphContext` 读取 `InputActionAsset`，还是先包装现有 `InputActionAssetValueSource` 的读取逻辑？倾向前者，因为它少一个 MonoBehaviour，并且能直接作为 `BaseGraph.User`。
- 第一版 MotionStage 是否只输出空/简单 velocity，还是直接接 `CharacterController.Move`？倾向只做最小正式结构，避免把 motion 业务提前写死。
- `TimelinePlayer` 外部 tick 模式是通过新增显式模式字段实现，还是直接删除自主 `FixedUpdate` 评估？倾向显式外部 tick 模式，但不得作为 fallback。
