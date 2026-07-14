# Change: add-character-pipeline-runtime-entry

## Summary
新增 `CharacterPipeline` 运行时入口规划，让角色 gameplay runtime 从 BTSMTL authoring 数据启动，并从第一版结构上服务 Goal 文档定义的混合网络架构：`server-authoritative result + client prediction + snapshot interpolation + combat rewind + correction smoothing`。

该变更不是恢复 BBB 的代码状态机、旧 `PlayerSO/LocomotionSO/ActionSO` 或多个 MonoBehaviour 控制器链路，也不是现在实现完整网络通信。它要先把客户端动作管线、网络边界和输出分层摆正，避免后续把预测、快照、校正和表现补成分裂路径。

该变更建立三层职责：

- `CharacterPipelineRunner`：全局 tick 源，统一调度所有角色管线。
- `CharacterPipelineHost`：每个角色的 Unity 装配点，只负责创建、注册和释放 pipeline。
- `CharacterPipeline`：纯 C# 角色动作管线，负责输入、BTSMTL 图、Timeline、Motion、Presentation、Network 边界的每帧阶段调度。

## Motivation
当前 BTSMTL authoring 底座已经能表达 `RootTree -> StateMachineNode -> StateMachineGraph -> StateNode -> SubTree/StateBehaviorSubTree -> TimelineNode`，但角色 runtime 还没有一个正式入口承载这条链路。没有入口时，图只能在编辑器或零散 runner 中被验证，动作、移动、动画、预测、快照、校正和远端插值没有统一落点。

BBB 参考项目提供了一个有价值的形状：单根 MonoBehaviour 装配、输入清洗、处理管线、运动驱动、帧末清理。但 BBB 的业务核心是代码继承状态机和大量 SO 配置，这与当前目标冲突。本变更只吸收它的运行时调度思想，不复制它的数据源和特化状态类。

Goal 文档已经定义网络技术路线：本地玩家预测移动、闪避、攻击启动、动画、特效和镜头；远端玩家使用服务器快照和插值；PvP 命中由服务端权威加 combat rewind；PvE 和目标点由服务端权威；校正由客户端平滑修正。本变更必须让 `CharacterPipeline` 成为这条混合架构的客户端动作主线，而不是只服务单机运行。

## Goals
- 建立一个正式的角色运行时入口，让 BTSMTL RootTree 可以在角色管线中被 tick。
- 使用单例/全局 runner 统一 tick，不让每个 `CharacterPipeline` 自己决定什么时候执行。
- 让 `CharacterPipelineHost` 只做角色管线定义、Unity 组件装配和 pipeline 注册，不承担业务逻辑，也不直接序列化 BTSMTL RootTree 或 BTSMTL component 类型。
- 让 `CharacterPipeline` 作为纯 C# 主体，分阶段处理 network receive、input、graph、timeline、motion、presentation、network send 和帧末清理。
- 让 BTSMTL `BaseGraph.User` 获得正式执行上下文，直接提供 `ITimelinePlaybackService` 和 `IInputActionValueSource`。
- 让 BTSMTL `BaseGraph.User` 同时承载 gameplay facts、authority mode、network tick 和 command context 的正式入口。
- 定义 `CharacterPipelineTickContext` 至少包含 deltaTime、frame index、simulation tick、input sequence 和 authority mode。
- 定义 `CharacterPipelineOutput` 的 strict gameplay、presentation 和 network 三类输出边界。
- 第一版保留正式 `NetworkReceiveStage` 和 `NetworkSendStage`，即使它们暂不接真实 Fantasy transport。
- 明确 Timeline 和动画 tick 权威：由 `CharacterPipelineRunner` 驱动，不依赖 `TimelinePlayer.FixedUpdate` 等自主 tick。
- 保持旧 locomotion/action/footphase/bodyclaim 等 SO/config 不作为当前数据源。
- 将新运行时代码放入 TEngine 主程序稳定层 `Assets/GameScripts/Main/Runtime/Character/Pipeline` 路径；不继续扩展旧 `Assets/Scripts` 或旧拼写 `Charactor` 路径。

## Non-Goals
- 不实现完整 Fantasy transport、完整服务端裁决、完整 combat rewind 或完整商业 PvPvE 后端。
- 不实现全局帧同步、完整世界 rollback、纯客户端权威或纯服务器权威角色控制器。
- 不实现完整 MotionResolver、碰撞、坡度、root motion warp 或动画混合树。
- 不恢复 BBB 的 `PlayerBaseState`、`PlayerStateRegistry`、`PlayerSO` 动作配置或特化 locomotion 状态类。
- 不新增 Workbench、并行端口系统、并行 graph runtime 或 fallback 配置。
- 不把 Timeline 升级成状态机，也不让 Timeline 直接裁决命中、伤害或最终位移。
- 不新增测试任务；用户会在 Unity 中做端到端验证。

## Impact
- 新增 `character-pipeline-runtime` 能力规格。
- 后续实现会新增角色 pipeline 文件夹和少量运行时类。
- 后续实现会新增正式但可为空逻辑的 network receive/send stage，并建立 strict/presentation/network 输出分层。
- 后续实现会让 tick context 和 graph context 具备 simulation tick、input sequence、authority mode 和 network command 入口。
- 后续实现可能需要调整 `TimelinePlayer` 的自主 tick 行为，使其能作为被 pipeline 外部驱动的 provider，而不是自己在 `FixedUpdate` 中推进。
- 后续实现需要清理或停止扩展旧 `Assets/Scripts` 和旧 `Charactor/Pipeline` 路径，统一迁移到 `Assets/GameScripts/Main/Runtime/Character/Pipeline`。

## Open Questions
- 第一版是否直接让 `CharacterGraphContext` 读取 `InputActionAsset`，还是先包装现有 `InputActionAssetValueSource` 的读取逻辑？倾向前者，因为它少一个 MonoBehaviour，并且能直接作为 `BaseGraph.User`。
- 第一版 MotionStage 是否只输出空/简单 velocity，还是直接接 `CharacterController.Move`？倾向只做最小正式结构，避免把 motion 业务提前写死。
- `TimelinePlayer` 外部 tick 模式是通过新增显式模式字段实现，还是直接删除自主 `FixedUpdate` 评估？倾向显式外部 tick 模式，但不得作为 fallback。
- 第一版是否只支持 `LocalPredicted` authority mode，还是同时创建 `RemoteProxy` 空路径？倾向先定义枚举和 output 边界，远端插值 stage 可以先为空实现。
- `NetworkStage` 第一版是否只收集 `NetworkOutput`，还是同时定义 `ServerSnapshot/Correction` 输入缓存？倾向两者都定义结构，但不接真实 transport。
