## 1. Scope 确认

- [x] 1.1 确认本变更只处理基础移动 Run-only 四阶段。
- [x] 1.2 确认本变更不新增 Walk/Run gait 选择。
- [x] 1.3 确认本变更不新增攻击、闪避、受击或死亡状态。
- [x] 1.4 确认本变更不新增通用 InterruptPolicy。
- [x] 1.5 确认本变更不新增 Timeline 编辑器。
- [x] 1.6 确认实现必须沿用 `PlayerLocomotionController -> BasicLocomotionPipeline -> BasicLocomotionAnimancerPresenter` 主链。
- [x] 1.7 确认 `refactor-locomotion-animation-config-boundaries` 的播放参数边界不得回退。

## 2. 当前字段审计

- [x] 2.1 检查 `RunLocomotionAnimationEntry` 当前字段。
- [x] 2.2 检查 `RunLocomotionAnimationConfigSO` 当前字段。
- [x] 2.3 检查 `BasicMovementConfigSO` 的 `moveStartMinTime` 和 `moveStopMinTime`。
- [x] 2.4 检查 `BasicMovementSettings` 当前 phase timing 字段。
- [x] 2.5 检查 `LocomotionStateGraphCondition` 当前时间条件。
- [x] 2.6 检查 `LocomotionStateGraphConditionEvaluator` 当前时间条件读取来源。
- [x] 2.7 检查默认 Run 动画配置资产的当前序列化字段。
- [x] 2.8 检查默认状态图资产是否引用旧时间条件。
- [x] 2.9 检查 Presenter 是否只读取 alias。

## 3. 数据模型

- [x] 3.1 新增 `LocomotionAnimationExitPolicy`。
- [x] 3.2 定义 `Manual` 退出策略。
- [x] 3.3 定义 `AfterDuration` 退出策略。
- [x] 3.4 新增 `LocomotionAnimationPhaseConfig`。
- [x] 3.5 在 phase config 中保存 `aliasKey`。
- [x] 3.6 在 phase config 中保存 `exitPolicy`。
- [x] 3.7 在 phase config 中保存 `exitDuration`。
- [x] 3.8 确保 phase config 不引用 `AnimationClip`。
- [x] 3.9 确保 phase config 不引用 Animancer 类型。
- [x] 3.10 确保 phase config 不引用场景对象。

## 4. Run 配置结构

- [x] 4.1 将 `RunLocomotionAnimationConfigSO.idle` 改为 phase config。
- [x] 4.2 将 `RunLocomotionAnimationConfigSO.moveStart` 或等价字段改为 phase config。
- [x] 4.3 将 `RunLocomotionAnimationConfigSO.moveLoop` 或等价字段改为 phase config。
- [x] 4.4 将 `RunLocomotionAnimationConfigSO.moveStop` 或等价字段改为 phase config。
- [x] 4.5 移除顶层 `runEndExitDuration` 特例字段。
- [x] 4.6 提供按 `BasicMovementPhase` 解析 phase config 的 API。
- [x] 4.7 提供按 `BasicMovementPhase` 解析 alias key 的 API。
- [x] 4.8 提供将 phase config 转成纯 movement timing 的 API。
- [x] 4.9 `ResetToDefaultConfig` 设置 `Idle` 默认 alias 和 `Manual`。
- [x] 4.10 `ResetToDefaultConfig` 设置 `MoveStart` 默认 alias、`AfterDuration` 和默认时长。
- [x] 4.11 `ResetToDefaultConfig` 设置 `MoveLoop` 默认 alias 和 `Manual`。
- [x] 4.12 `ResetToDefaultConfig` 设置 `MoveStop` 默认 alias、`AfterDuration` 和默认时长。

## 5. Movement 纯数据

- [x] 5.1 新增或调整 `BasicMovementPhaseTiming` 纯数据结构。
- [x] 5.2 让 timing 能表达 `MoveStart` 退出策略和时长。
- [x] 5.3 让 timing 能表达 `MoveStop` 退出策略和时长。
- [x] 5.4 保留 `BasicMovementConfigSO.moveStartMinTime` 作为缺失 phase timing 时的 fallback。
- [x] 5.5 保留 `BasicMovementConfigSO.moveStopMinTime` 作为缺失 phase timing 时的 fallback。
- [x] 5.6 `BasicMovementSettings` 能携带 phase timing。
- [x] 5.7 `BasicMovementSettings` 继续不引用 Animancer。
- [x] 5.8 `BasicMovementSettings` 继续不引用 AnimationClip。
- [x] 5.9 `BasicMovementSettings` 继续不引用 TransitionAsset。

## 6. 状态图条件

- [x] 6.1 为状态图增加 `PhaseExitTimeReached` 或等价条件。
- [x] 6.2 让 `LocomotionStateGraphContext` 携带当前 phase。
- [x] 6.3 让 `PhaseExitTimeReached` 从当前 phase timing 判断。
- [x] 6.4 `Manual` policy 下 `PhaseExitTimeReached` 返回 false。
- [x] 6.5 `AfterDuration` policy 下 `PhaseExitTimeReached` 比较 `phaseTime >= exitDuration`。
- [x] 6.6 默认 `MoveStart -> MoveLoop` 使用 phase exit 条件。
- [x] 6.7 默认 `MoveStop -> Idle` 使用 phase exit 条件。
- [x] 6.8 `MoveStop -> MoveStart` 继续只依赖 `HasMoveIntent`。
- [x] 6.9 `MoveStop -> MoveStart` 继续优先于 `MoveStop -> Idle`。
- [x] 6.10 如保留旧 `MoveStartMinTimeReached`，确认其委托到 phase timing。
- [x] 6.11 如保留旧 `MoveStopMinTimeReached`，确认其委托到 phase timing。

## 7. 主链装配

- [x] 7.1 `PlayerLocomotionController` 继续解析 Run 动画配置。
- [x] 7.2 Controller 将 Run phase timing 转成纯 `BasicMovementSettings`。
- [x] 7.3 Controller 不把 `RunLocomotionAnimationConfigSO` 传进状态机。
- [x] 7.4 Controller 不读取 Animancer clip length。
- [x] 7.5 Controller 不读取 TransitionAsset。
- [x] 7.6 Controller 缺少 Run 配置时继续使用 movement config fallback。
- [x] 7.7 Controller 同时绑定自身 Run 配置和 Presenter Run 配置时保持现有解析优先级。

## 8. Presenter 边界

- [x] 8.1 Presenter 从 phase config 解析 alias key。
- [x] 8.2 Presenter 不读取 `exitPolicy`。
- [x] 8.3 Presenter 不读取 `exitDuration`。
- [x] 8.4 Presenter 不根据 `AfterDuration` 切换状态。
- [x] 8.5 Presenter 不注册 Animancer OnEnd 来驱动基础移动状态。
- [x] 8.6 Presenter 继续避免相同 phase 和 alias 每帧重播。
- [x] 8.7 Presenter 继续不覆盖 fade。
- [x] 8.8 Presenter 继续不覆盖 speed。
- [x] 8.9 Presenter 继续不覆盖 normalized start time。

## 9. 配置资产迁移

- [x] 9.1 更新 `DefaultRunLocomotionAnimationConfig.asset` 的 `Idle` phase config。
- [x] 9.2 更新 `DefaultRunLocomotionAnimationConfig.asset` 的 `MoveStart` phase config。
- [x] 9.3 更新 `DefaultRunLocomotionAnimationConfig.asset` 的 `MoveLoop` phase config。
- [x] 9.4 更新 `DefaultRunLocomotionAnimationConfig.asset` 的 `MoveStop` phase config。
- [x] 9.5 将旧 `runEndExitDuration` 数值迁移到 `MoveStop.exitDuration`。
- [x] 9.6 给 `MoveStart.exitDuration` 设置当前起步 fallback 值。
- [x] 9.7 确认 asset 不保存 Animancer playback 参数。
- [x] 9.8 确认 prefab 引用仍指向同一个 Run 配置资产。
- [x] 9.9 确认场景没有新增并行 Run 配置资产。

## 10. 配置校验

- [x] 10.1 校验空 `Idle.aliasKey`。
- [x] 10.2 校验空 `MoveStart.aliasKey`。
- [x] 10.3 校验空 `MoveLoop.aliasKey`。
- [x] 10.4 校验空 `MoveStop.aliasKey`。
- [x] 10.5 校验 `AfterDuration` 且 duration 小于 0。
- [x] 10.6 校验默认 `MoveStart` 使用可退出策略。
- [x] 10.7 校验默认 `MoveStop` 使用可退出策略。
- [x] 10.8 校验器不读取 Animancer TransitionAsset。
- [x] 10.9 校验器不修改配置资产。

## 11. 自动测试

- [x] 11.1 测试默认 phase config alias 为 `Idle / RunStart / RunLoop / RunEnd`。
- [x] 11.2 测试默认 `Idle` 为 `Manual`。
- [x] 11.3 测试默认 `MoveStart` 为 `AfterDuration`。
- [x] 11.4 测试默认 `MoveLoop` 为 `Manual`。
- [x] 11.5 测试默认 `MoveStop` 为 `AfterDuration`。
- [x] 11.6 测试 `RunEnd` 顶层特例字段不存在。
- [x] 11.7 测试 phase config 不暴露 fade。
- [x] 11.8 测试 phase config 不暴露 speed。
- [x] 11.9 测试 phase config 不暴露 normalized start time。
- [x] 11.10 测试 `MoveStart.exitDuration` 控制 `MoveStart -> MoveLoop`。
- [x] 11.11 测试 `MoveStop.exitDuration` 控制 `MoveStop -> Idle`。
- [x] 11.12 测试 `MoveStop` 中重新输入立即进入 `MoveStart`。
- [x] 11.13 测试缺少 Run 配置时使用 movement config fallback。
- [x] 11.14 测试 `Manual` policy 不触发 `PhaseExitTimeReached`。
- [x] 11.15 测试 `AfterDuration` policy 达到时长后触发 `PhaseExitTimeReached`。
- [x] 11.16 测试配置校验报告空 alias。
- [x] 11.17 测试配置校验报告非法 exit duration。
- [x] 11.18 静态测试状态机不引用 Animancer。
- [x] 11.19 静态测试状态机不引用 AnimationClip。
- [x] 11.20 静态测试状态机不引用 TransitionAsset。
- [x] 11.21 静态测试 Presenter 不读取状态图 builder。
- [x] 11.22 静态测试 Presenter 不读取运动执行端口。
- [x] 11.23 静态测试 Presenter 不注册 OnEnd 驱动基础移动状态。

## 12. 验证命令

- [x] 12.1 运行 `openspec validate add-locomotion-animation-phase-exit-policy --strict --no-interactive`。
- [x] 12.2 刷新 Unity 并确认 Console 没有 C# 编译错误。（MCP Console 当前 `no_unity_session`；已确认 `Assembly-CSharp.dll` / `Assembly-CSharp-Editor.dll` 于 16:03 重新生成，Editor.log 尾部无新的 `error CS`。）
- [x] 12.3 运行 Unity EditMode 定向测试 `ThirdPersonMovement.Tests.PlayerLocomotionControllerTests`。（MCP Test Runner 多次返回 `no_unity_session`，未完成。）
- [x] 12.4 运行静态搜索确认状态机边界。
- [x] 12.5 运行静态搜索确认 Presenter 边界。
- [x] 12.6 如果 Unity MCP 或测试不可用，记录原因和手动验证步骤，不伪造结果。

## 13. 手动端到端验证

- [x] 13.1 打开当前演示场景。
- [x] 13.2 确认角色 prefab 引用默认 Run phase config 资产。
- [x] 13.3 确认 `MoveStart.exitDuration` 可在 Inspector 中配置。
- [x] 13.4 确认 `MoveStop.exitDuration` 可在 Inspector 中配置。
- [x] 13.5 持续输入移动，确认播放 `RunStart` 后进入 `RunLoop`。
- [x] 13.6 松开输入，确认播放 `RunEnd`。
- [x] 13.7 不再输入，确认按 `MoveStop.exitDuration` 回到 `Idle`。
- [x] 13.8 在 `RunEnd` 中途重新输入，确认立即进入 `MoveStart` 并播放 `RunStart`。
- [x] 13.9 将 `MoveStop.exitDuration` 改短，确认回 Idle 变快。
- [x] 13.10 将 `MoveStop.exitDuration` 改长，确认回 Idle 变慢。
- [x] 13.11 修改 Animancer `Corin_RunEnd.asset` 的 fade，确认表现变化仍来自 Animancer。
