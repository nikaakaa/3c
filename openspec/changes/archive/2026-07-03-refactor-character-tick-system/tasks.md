# Tasks

## 1. Tick 语义命名

- [x] 1.1 全量梳理正式 runtime 中 `SimulationTick` 的使用点。
- [x] 1.2 将本地预测来源的 tick 语义定义为 `LocalLogicTick`。
- [x] 1.3 将服务端权威来源的 tick 语义定义为 `ServerTick`。
- [x] 1.4 将每帧表现序号定义为 `RenderFrame`。
- [x] 1.5 保留 `InputSequence` 作为输入确认和校正身份。
- [x] 1.6 明确 `LocalLogicTick` 不等于 `ServerTick`。
- [x] 1.7 明确 `RenderFrame` 不参与服务端权威对齐。

## 2. Tick 配置

- [x] 2.1 新增 `GameplayTickSettings` 或等价正式配置。
- [x] 2.2 配置包含本地逻辑 tick rate。
- [x] 2.3 配置包含最大 catch-up tick 数。
- [x] 2.4 配置包含 accumulator 溢出处理策略。
- [x] 2.5 配置不得提供 fallback tick source。
- [x] 2.6 配置不得引用 Fantasy、BTSMTL Graph 或 Unity editor 类型。

## 3. Tick 上下文

- [x] 3.1 新增 `GameplayLogicTickContext`。
- [x] 3.2 `GameplayLogicTickContext` 包含 fixed delta。
- [x] 3.3 `GameplayLogicTickContext` 包含 render frame。
- [x] 3.4 `GameplayLogicTickContext` 包含 local logic tick。
- [x] 3.5 `GameplayLogicTickContext` 包含 input sequence。
- [x] 3.6 `GameplayLogicTickContext` 包含 authority mode。
- [x] 3.7 新增 `GameplayPresentationFrameContext`。
- [x] 3.8 `GameplayPresentationFrameContext` 包含 scaled delta。
- [x] 3.9 `GameplayPresentationFrameContext` 包含 unscaled delta。
- [x] 3.10 `GameplayPresentationFrameContext` 包含 render frame。
- [x] 3.11 `GameplayPresentationFrameContext` 包含最近 local logic tick。
- [x] 3.12 `GameplayPresentationFrameContext` 包含 interpolation alpha。
- [x] 3.13 context 中不得把 server tick 作为本地主时钟字段。

## 4. GameplayTickSystem

- [x] 4.1 新增纯 C# `GameplayTickSystem`。
- [x] 4.2 `GameplayTickSystem` 支持注册 `IGameplayTickTarget`。
- [x] 4.3 `GameplayTickSystem` 支持反注册 `IGameplayTickTarget`。
- [x] 4.4 `GameplayTickSystem` 维护 render frame。
- [x] 4.5 `GameplayTickSystem` 维护 local logic tick。
- [x] 4.6 `GameplayTickSystem` 维护 accumulator。
- [x] 4.7 `GameplayTickSystem.FrameUpdate()` 使用正式 time source 配置选择 accumulator delta。
- [x] 4.8 `GameplayTickSystem.FrameUpdate()` 按 fixed delta 推进 `LogicTick`。
- [x] 4.9 `GameplayTickSystem.FrameUpdate()` 限制单帧 catch-up tick 数。
- [x] 4.10 `GameplayTickSystem.FrameUpdate()` 计算 interpolation alpha。
- [x] 4.11 `GameplayTickSystem.FrameLateUpdate()` 推进 `PresentationFrame`。
- [x] 4.12 `GameplayTickSystem` 不继承 MonoBehaviour。
- [x] 4.13 `GameplayTickSystem` 不实现 TEngine Module。
- [x] 4.14 `GameplayTickSystem` 不直接持有 `CharacterPipeline` 专用列表。

## 5. TEngine 驱动接入

- [x] 5.1 定义正式 bootstrap 将 TEngine frame source 接到 `GameplayTickSystem`。
- [x] 5.2 bootstrap 注册 update 回调。
- [x] 5.3 bootstrap 注册 late update 回调。
- [x] 5.4 bootstrap 在释放时反注册 update 回调。
- [x] 5.5 bootstrap 在释放时反注册 late update 回调。
- [x] 5.6 bootstrap 不直接 tick BTSMTL Graph。
- [x] 5.7 bootstrap 不直接 tick `CharacterPipeline`。
- [x] 5.8 bootstrap 不创建 fallback runner。

## 6. CharacterPipeline 入口迁移

- [x] 6.1 新增 `CharacterPipeline.LogicTick()`。
- [x] 6.2 新增 `CharacterPipeline.PresentationFrame()`。
- [x] 6.3 将 NetworkReceiveStage 迁入 `LogicTick`。
- [x] 6.4 将 InputStage 迁入 `LogicTick`。
- [x] 6.5 将 BTSMTLPhase 迁入 `LogicTick`。
- [x] 6.6 将 MotionStage 迁入 `LogicTick`。
- [x] 6.7 将 NetworkSendStage 迁入 `LogicTick` 的帧末收集。
- [x] 6.8 将 PresentationStage 迁入 `PresentationFrame`。
- [x] 6.9 将 transient 清理放到 `PresentationFrame` 后或明确的 frame end。
- [x] 6.10 删除或停用 `UpdatePhase()`。
- [x] 6.11 删除或停用 `LatePhase()`。

## 7. Host 和旧 Runner 清理

- [x] 7.1 修改 `CharacterPipelineHost.OnEnable()` 注册到 `GameplayTickSystem`。
- [x] 7.2 修改 `CharacterPipelineHost.OnDisable()` 从 `GameplayTickSystem` 反注册。
- [x] 7.3 Host 不直接读取 Unity `Time`。
- [x] 7.4 Host 不保存 tick 状态。
- [x] 7.5 删除 `CharacterPipelineRunner`。
- [x] 7.6 删除旧 runner 场景单例错误提示。
- [x] 7.7 使用 `rg` 确认正式 runtime 不再引用 `CharacterPipelineRunner`。

## 8. 输入层迁移

- [x] 8.1 `CharacterInputFrame` 字段迁移为 `LocalLogicTick`。
- [x] 8.2 `CharacterInputHistory` 支持按 `LocalLogicTick` 查询。
- [x] 8.3 `CharacterInputRequest` 创建 tick 使用 `LocalLogicTick`。
- [x] 8.4 request 过期判断使用 `LocalLogicTick`。
- [x] 8.5 `CharacterGraphContext` 输入查询使用 `LocalLogicTick`。
- [x] 8.6 `ClientCommand` 来源改为 `LocalLogicTick + InputSequence`。

## 9. 网络 tick 字段迁移

- [x] 9.1 `ServerSnapshot` 使用 `ServerTick`。
- [x] 9.2 `Correction` 使用 `ServerTick` 和 `InputSequence`。
- [x] 9.3 `ConfirmedEvent` 或后续 action decision 使用 `ServerTick`。
- [x] 9.4 网络输入缓存不得把 server tick 写入 local logic tick。
- [x] 9.5 NetworkSendStage 不直接发送 Fantasy 消息。
- [x] 9.6 NetworkReceiveStage 不直接修改 Transform。
- [x] 9.7 NetworkReceiveStage 不直接 tick BTSMTL。

## 10. Action 事务字段迁移

- [x] 10.1 `ActionActivationRequest` 使用 `LocalLogicTick` 表达本地启动来源。
- [x] 10.2 `ActionEndRequest` 使用 `LocalLogicTick` 表达本地结束来源。
- [x] 10.3 `ActionInstance` start tick 使用 `LocalLogicTick`。
- [x] 10.4 服务端确认使用 `ServerTick` 或 decision packet 字段表达。
- [x] 10.5 ActionRuntime 不直接解释 `ServerTick` 为本地 tick。

## 11. Loopback proposal 同步

- [x] 11.1 更新 `add-local-network-loopback-peer` 中的 `simulation tick` 口径。
- [x] 11.2 loopback latency 字段改为本地逻辑 tick 语义。
- [x] 11.3 loopback packet 保留 server tick 字段。
- [x] 11.4 loopback driver 不创建第二套 pipeline tick。
- [x] 11.5 loopback peer 不直接调用 Graph、Timeline、Motion 或 Presentation。

## 12. 验证

- [x] 12.1 使用 `rg` 确认正式 runtime 不再存在 `CharacterPipelineRunner` 引用。
- [x] 12.2 使用 `rg` 确认 `CharacterPipeline` 不再暴露 `UpdatePhase` 和 `LatePhase`。
- [x] 12.3 使用 `rg` 确认本地输入和 command 不再使用 `SimulationTick` 命名。
- [x] 12.4 使用 `rg` 确认 server snapshot 和 correction 使用 `ServerTick` 命名。
- [x] 12.5 使用 `rg` 确认没有新增 TEngine Module 直接 tick CharacterPipeline。
- [x] 12.6 使用 `rg` 确认没有新增 fallback runner。
- [x] 12.7 运行 `openspec validate refactor-character-tick-system --strict --no-interactive`。

## 13. 输入锁存修正

- [x] 13.1 `CharacterPipeline.BeginRenderFrame()` 将表现帧边界传入输入层。
- [x] 13.2 `CharacterInputStage` 在表现帧采样连续输入值。
- [x] 13.3 `CharacterInputStage` 在表现帧采样动作触发边沿。
- [x] 13.4 动作触发边沿在没有 logic tick 的表现帧中保持待消费。
- [x] 13.5 一个表现帧内多个 catch-up logic tick 不重复消费同一动作触发。
- [x] 13.6 `CharacterInputStage.Update()` 只从锁存输入构建 `CharacterInputFrame`。
- [x] 13.7 `RemoteProxy` 和 `PresentationOnly` 不从本地 InputAction 锁存动作请求。

## 14. Tick time source 配置

- [x] 14.1 `GameplayTickSettings` 新增正式 time source 字段。
- [x] 14.2 time source 支持 scaled delta。
- [x] 14.3 time source 支持 unscaled delta。
- [x] 14.4 `GameplayTickSystem.FrameUpdate()` 通过 settings 选择 accumulator delta。
- [x] 14.5 默认 gameplay settings 使用 scaled delta。
- [x] 14.6 `GameplayTickSystem` 不再硬编码 unscaled delta 作为唯一逻辑时间源。

## 15. 追加验证

- [x] 15.1 使用 `rg` 确认 InputStage 不在 logic tick 中调用 `WasPressedThisFrame()`。
- [x] 15.2 使用 `rg` 确认 InputStage 不在 logic tick 中直接用当前 InputAction 构造 action request。
- [x] 15.3 使用 `rg` 确认 `GameplayTickSystem` 的 accumulator delta 来自 `GameplayTickSettings`。
- [x] 15.4 运行 `openspec validate refactor-character-tick-system --strict --no-interactive`。

## 16. Gameplay 命名和路径收束

- [x] 16.1 将 tick 系统文件移动到 `Assets/GameScripts/Main/Runtime/Gameplay/Tick`。
- [x] 16.2 删除旧 `Assets/GameScripts/Main/Runtime/Character/Pipeline/Tick` 路径。
- [x] 16.3 将 `CharacterTickSystem` 重命名为 `GameplayTickSystem`。
- [x] 16.4 将 `CharacterTickBootstrap` 重命名为 `GameplayTickBootstrap`。
- [x] 16.5 将 `CharacterTickSettings` 重命名为 `GameplayTickSettings`。
- [x] 16.6 将 `CharacterLogicTickContext` 重命名为 `GameplayLogicTickContext`。
- [x] 16.7 将 `CharacterPresentationFrameContext` 重命名为 `GameplayPresentationFrameContext`。
- [x] 16.8 新增 `IGameplayTickTarget`。
- [x] 16.9 `CharacterPipeline` 实现 `IGameplayTickTarget`。
- [x] 16.10 将 loopback hook 从角色私有 tick hook 收束到 gameplay tick hook。
- [x] 16.11 使用 `rg` 确认正式 runtime 不再引用 `CharacterTickSystem`。
- [x] 16.12 使用 `rg` 确认正式 runtime 不再引用旧 `Character/Pipeline/Tick` 路径。
- [x] 16.13 将 `CharacterAuthorityMode` 收束为 `GameplayAuthorityMode`。
