## 1. 准备与边界确认
- [x] 1.1 读取本 change 的 `proposal.md`、`design.md`、`tasks.md` 和 spec delta。
- [x] 1.2 读取 `openspec/project.md`，确认输入、状态、运动、动画、网络边界。
- [x] 1.3 读取 `add-local-preinput-buffer`，确认本地输入 step 和输入缓冲语义。
- [x] 1.4 读取 `integrate-unityhfsm-locomotion`，确认 Locomotion/HFSM 主线。
- [x] 1.5 搜索 `InputRequestBuffer`，记录当前 step 调用点。
- [x] 1.6 搜索 `PlayerLocomotionController.Tick`，记录当前 Unity frame 驱动入口。
- [x] 1.7 搜索 `BasicLocomotionPipeline`，确认移动命令生成边界。
- [x] 1.8 搜索 `IBasicLocomotionMotionExecutor`，确认位移权威入口。
- [x] 1.9 搜索 Fantasy server 和 proto 目录，确认本变更不修改协议。
- [x] 1.10 若发现需要修改协议、完整 rollback 或第二控制入口，停止并回到 OpenSpec。

## 2. 目录与命名
- [x] 2.1 确认新增 tick core 的目录位置。
- [x] 2.2 tick core 目录必须与 `Assets/Scripts/Input` 分离。
- [x] 2.3 tick core 命名空间必须表达 Simulation/Timing 职责。
- [x] 2.4 不把 tick 类型放进 Locomotion 具体实现目录。
- [x] 2.5 不把 tick 类型放进 Unity scene adapter 目录。

## 3. SimulationTick 值对象
- [x] 3.1 定义 `SimulationTick` 纯 C# 值对象。
- [x] 3.2 使用整数原始值保存 tick id。
- [x] 3.3 暴露只读原始值。
- [x] 3.4 支持相等比较。
- [x] 3.5 支持大小比较。
- [x] 3.6 支持加正向偏移。
- [x] 3.7 支持减偏移。
- [x] 3.8 支持计算两个 tick 的差值。
- [x] 3.9 明确负 tick 是否被允许；若不允许，构造时保护。
- [x] 3.10 不引用 Unity 类型。

## 4. Tick rate 与 fixed delta
- [x] 4.1 定义 `SimulationTickRate` 或等价 settings。
- [x] 4.2 使用 `ticksPerSecond` 作为主配置。
- [x] 4.3 从 `ticksPerSecond` 派生 `fixedDeltaSeconds`。
- [x] 4.4 拒绝 0 或负数 tick rate。
- [x] 4.5 保留默认 tick rate，默认值必须在文档或测试中可见。
- [x] 4.6 客户端和服务端读取同一语义的 tick rate。
- [x] 4.7 不把 tick rate 绑定到 Unity `Time.fixedDeltaTime`。

## 5. Tick context
- [x] 5.1 定义 `SimulationTickContext`。
- [x] 5.2 context 包含当前 `SimulationTick`。
- [x] 5.3 context 包含 `fixedDeltaSeconds`。
- [x] 5.4 context 包含本 tick 的序号或原始值读取方式。
- [x] 5.5 context 不持有 Unity scene object。
- [x] 5.6 context 可在测试中直接构造。

## 6. Client accumulator
- [x] 6.1 定义 `SimulationTickAccumulator`。
- [x] 6.2 支持输入 real delta seconds。
- [x] 6.3 不足一 tick 时输出 0 个 tick。
- [x] 6.4 刚好一 tick 时输出 1 个 tick。
- [x] 6.5 多倍 tick delta 输出多个 tick。
- [x] 6.6 支持每帧最大追帧 tick 数。
- [x] 6.7 超过追帧上限时行为明确，保留或丢弃余量必须有测试。
- [x] 6.8 处理负 delta 时采用明确策略，拒绝或 clamp 必须有测试。
- [x] 6.9 accumulator 不读取 `Time.deltaTime`。
- [x] 6.10 Unity adapter 才能读取 `Time.deltaTime`。

## 7. Tick phase 枚举与顺序
- [x] 7.1 定义 tick phase 枚举或等价常量。
- [x] 7.2 包含 ReadInput phase。
- [x] 7.3 包含 UpdateInputBuffer phase。
- [x] 7.4 包含 GameplayDecision phase。
- [x] 7.5 包含 BuildMotion phase。
- [x] 7.6 包含 ExecuteMotion phase。
- [x] 7.7 包含 WriteSnapshotAndEvents phase。
- [x] 7.8 包含 PresentationBridge phase。
- [x] 7.9 定义唯一 phase 顺序表。
- [x] 7.10 phase 顺序不可由注册顺序隐式决定。

## 8. Tick runner core
- [x] 8.1 定义 tick runner 接口或类。
- [x] 8.2 runner 按固定 phase 顺序调度。
- [x] 8.3 runner 为每个 phase 传入同一 tick context。
- [x] 8.4 runner 支持注册 phase handler。
- [x] 8.5 runner 支持无 handler 的 phase 安全跳过。
- [x] 8.6 runner 不捕获 Unity scene object。
- [x] 8.7 runner 不直接调用 `CharacterController.Move`。
- [x] 8.8 runner 不直接播放 Animancer。
- [x] 8.9 runner 不直接读取 Input System。
- [x] 8.10 runner 可用 fake handler 做纯 C# 测试。

## 9. 输入缓冲接入边界
- [x] 9.1 明确 `InputRequestBuffer` 仍是纯输入请求层。
- [x] 9.2 调用侧使用 `SimulationTick` 原始值或等价 tick id 作为 current step。
- [x] 9.3 输入缓冲不记录动作结果。
- [x] 9.4 输入缓冲不发网络包。
- [x] 9.5 输入缓冲不直接消费 Attack/Dodge/Jump/Interact。
- [x] 9.6 输入缓冲可在 UpdateInputBuffer phase 被调度。
- [x] 9.7 为后续输入历史保留输入事实与消费结果分离边界。
- [x] 9.8 不修改现有输入请求种类，除非另有 proposal。

## 10. Locomotion 接入边界
- [x] 10.1 明确 `PlayerLocomotionController` 仍是基础移动入口。
- [x] 10.2 若新增 adapter，只负责把 tick context 转换为现有 `Tick` 调用。
- [x] 10.3 不新增第二套 movement controller。
- [x] 10.4 不绕过 `BasicLocomotionPipeline`。
- [x] 10.5 不绕过 `IBasicLocomotionMotionExecutor`。
- [x] 10.6 不绕过 `BasicLocomotionAnimancerPresenter`。
- [x] 10.7 现有 WASD/Look 输入语义保持。
- [x] 10.8 现有 camera resolve 顺序若迁移到 tick phase，必须有测试或手动验证说明。

## 11. Client Unity driver
- [x] 11.1 定义客户端 Unity driver 的职责边界。
- [x] 11.2 driver 可以读取 Unity frame delta。
- [x] 11.3 driver 把 delta 交给 accumulator。
- [x] 11.4 driver 对每个输出 tick 调用 runner。
- [x] 11.5 driver 不直接处理玩法判定。
- [x] 11.6 driver 不直接处理网络同步。
- [x] 11.7 driver 可启用或禁用，避免与旧 frame Update 双驱动。
- [x] 11.8 若发现会造成双 Tick，必须停止并调整 proposal。

## 12. Server tick driver
- [x] 12.1 定义服务端 tick driver 合约。
- [x] 12.2 driver 不依赖 Unity `Update`。
- [x] 12.3 driver 使用同一 tick rate 语义。
- [x] 12.4 driver 能由测试手动 pump。
- [x] 12.5 driver 为未来 Fantasy timer 接入保留边界。
- [x] 12.6 本变更不修改 Fantasy proto。
- [x] 12.7 本变更不实现服务端完整角色模拟。
- [x] 12.8 本变更不实现网络输入等待策略。

## 13. GGPO 风格预留边界
- [x] 13.1 定义输入历史未来接入点。
- [x] 13.2 定义状态快照历史未来接入点。
- [x] 13.3 定义从分歧 tick 回滚未来接入点。
- [x] 13.4 定义重放 tick runner 的未来接入点。
- [x] 13.5 不引入 GGPO SDK。
- [x] 13.6 不暴露 GGPO 具体 API 到项目接口。
- [x] 13.7 文档说明本阶段只是地基，不是完整 rollback。

## 14. EditMode 自动测试
- [x] 14.1 测试 `SimulationTick` 相等比较。
- [x] 14.2 测试 `SimulationTick` 大小比较。
- [x] 14.3 测试 `SimulationTick` 偏移。
- [x] 14.4 测试 tick 差值。
- [x] 14.5 测试非法 tick 值策略。
- [x] 14.6 测试 tick rate 生成 fixed delta。
- [x] 14.7 测试非法 tick rate 被拒绝。
- [x] 14.8 测试 accumulator 不足一 tick 输出 0。
- [x] 14.9 测试 accumulator 一 tick 输出 1。
- [x] 14.10 测试 accumulator 多 tick 输出 N。
- [x] 14.11 测试 accumulator 追帧上限。
- [x] 14.12 测试 accumulator 超限余量策略。
- [x] 14.13 测试 runner phase 顺序。
- [x] 14.14 测试 runner 使用同一 tick context。
- [x] 14.15 测试空 phase 安全跳过。
- [x] 14.16 测试同一输入序列重复执行得到一致 phase 记录。
- [x] 14.17 测试 tick core 不需要 Unity scene object。

## 15. 静态验证
- [x] 15.1 搜索 tick core 不引用 Animancer。
- [x] 15.2 搜索 tick core 不引用 Cinemachine。
- [x] 15.3 搜索 tick core 不引用 `CharacterController`。
- [x] 15.4 搜索 tick core 不引用 `InputActionReference`。
- [x] 15.5 搜索 tick core 不调用 `CharacterController.Move`。
- [x] 15.6 搜索本变更不修改 Fantasy proto。
- [x] 15.7 搜索本变更不新增第二套 player controller。
- [x] 15.8 搜索本变更不新增完整 rollback runtime。

## 16. Play Mode 与用户复验
- [x] 16.1 确认当前 active scene 为 `Sandbox`。
- [x] 16.2 进入并退出 Play Mode 烟测。
- [x] 16.3 检查 Play Mode 烟测后 Console 无 error。
- [x] 16.4 静态确认 `UnitySimulationTickDriver` 未挂到 scene、prefab 或 asset。
- [x] 16.5 静态确认新增 tick driver 默认 `runAutomatically=false`，不会自动双驱动现有移动。
- [x] 16.6 向用户提供 WASD 复验步骤：进入 `Sandbox` Play Mode 后按 W/A/S/D，确认移动方向和速度无回退。
- [x] 16.7 向用户提供 Look 复验步骤：移动鼠标或摇杆 Look，确认相机行为无回退。
- [x] 16.8 向用户提供动画复验步骤：观察 Idle、MoveStart、MoveLoop、MoveStop 表现无回退。
- [x] 16.9 若后续把 `UnitySimulationTickDriver` 挂入场景并启用，必须再次确认没有双移动或双动画播放。

## 17. OpenSpec 与收尾
- [x] 17.1 运行 `openspec validate add-simulation-tick-system --strict --no-interactive`。
- [x] 17.2 修复所有 OpenSpec 校验问题。
- [x] 17.3 运行定向 EditMode 测试并记录结果。
- [x] 17.4 记录静态验证命令和结果。
- [x] 17.5 更新本任务清单为真实完成状态。
- [x] 17.6 向用户说明自动测试、静态验证和手动验证步骤。

