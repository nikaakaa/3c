## 1. Scope 确认

- [x] 1.1 确认本变更不新增 Walk/Run gait 选择。
- [x] 1.2 确认本变更不新增攻击、闪避、受击等动作状态。
- [x] 1.3 确认本变更不新增通用 InterruptPolicy。
- [x] 1.4 确认本变更不新增 Timeline 编辑器。
- [x] 1.5 确认实现必须沿用 `PlayerLocomotionController -> BasicLocomotionAnimancerPresenter` 主链。
- [x] 1.6 确认旧 `update-locomotion-animation-parameters` 不作为实现来源。

## 2. 当前字段审计

- [x] 2.1 检查 `RunLocomotionAnimationEntry` 当前字段。
- [x] 2.2 检查 `RunLocomotionAnimationConfigSO` 当前序列化结构。
- [x] 2.3 检查 `DefaultRunLocomotionAnimationConfig.asset` 当前序列化字段。
- [x] 2.4 检查 `BasicLocomotionAnimancerPresenter` 是否覆盖 fade duration。
- [x] 2.5 检查 `BasicLocomotionAnimancerPresenter` 是否覆盖 speed。
- [x] 2.6 检查 `BasicLocomotionAnimancerPresenter` 是否覆盖 normalized start time。
- [x] 2.7 检查 Animancer `Corin_TransitionLib.asset` alias 仍包含 `Idle / RunStart / RunLoop / RunEnd`。
- [x] 2.8 检查 `Corin_*` TransitionAsset 中仍保存 clip 和播放参数。

## 3. 数据模型收缩

- [x] 3.1 从 `RunLocomotionAnimationEntry` 移除 `fadeDuration`。
- [x] 3.2 从 `RunLocomotionAnimationEntry` 移除 `speed`。
- [x] 3.3 从 `RunLocomotionAnimationEntry` 移除 `normalizedStartTime`。
- [x] 3.4 保留 alias key。
- [x] 3.5 保留或等价表达 `RunEnd` exit duration。
- [x] 3.6 确保 alias key 空值校验保留。
- [x] 3.7 确保 exit duration 小于 0 时 fallback 到 `moveStopMinTime`。
- [x] 3.8 删除针对 speed 非法的 Run 配置校验。
- [x] 3.9 确认数据模型不引用 AnimationClip。
- [x] 3.10 确认数据模型不引用 Animancer TransitionAsset。

## 4. 配置资产调整

- [x] 4.1 更新 `DefaultRunLocomotionAnimationConfig.asset`，移除旧播放参数字段。
- [x] 4.2 确认 `idle` 只保存 `Idle` alias。
- [x] 4.3 确认 `runStart` 只保存 `RunStart` alias。
- [x] 4.4 确认 `runLoop` 只保存 `RunLoop` alias。
- [x] 4.5 确认 `runEnd` 保存 `RunEnd` alias。
- [x] 4.6 确认 `runEndExitDuration` 或等价字段保存逻辑退出时间。
- [x] 4.7 确认 prefab 引用仍指向同一个配置资产。
- [x] 4.8 确认场景引用没有新增并行配置资产。

## 5. Presenter 调整

- [x] 5.1 Presenter 从 Run 配置解析 alias key。
- [x] 5.2 Presenter 使用 `animancer.TryPlay(alias)` 或等价 alias 播放入口。
- [x] 5.3 Presenter 不再向 Animancer state 写入 speed。
- [x] 5.4 Presenter 不再向 Animancer state 写入 normalized time。
- [x] 5.5 Presenter 不再从 Run 配置读取 fade duration。
- [x] 5.6 Presenter 继续避免相同 phase 和 alias 每帧重播。
- [x] 5.7 Presenter 继续报告当前 phase。
- [x] 5.8 Presenter 继续报告当前动画名。
- [x] 5.9 Presenter 继续报告当前速度。
- [x] 5.10 Presenter 不调用状态机切换 API。
- [x] 5.11 Presenter 不调用运动执行端口。
- [x] 5.12 Presenter 不写 Transform。

## 6. 状态机和主链调整

- [x] 6.1 `PlayerLocomotionController` 继续解析 Run 配置。
- [x] 6.2 主链只从 Run 配置读取 `RunEndExitDuration`。
- [x] 6.3 主链将 `RunEndExitDuration` 写入纯 `BasicMovementSettings` 或等价数据。
- [x] 6.4 状态机只读取当前 `MoveStop` 退出时长数值。
- [x] 6.5 `MoveStop -> MoveStart` 继续优先于 `MoveStop -> Idle`。
- [x] 6.6 状态机不读取 alias key。
- [x] 6.7 状态机不引用 Animancer。
- [x] 6.8 状态机不引用 AnimationClip。
- [x] 6.9 状态机不引用 TransitionLibrary。

## 7. 文件夹分层

- [x] 7.1 保持动画纯数据模型在 `Assets/Scripts/Character/Animation/Model/`。
- [x] 7.2 保持动画配置 SO 在 `Assets/Scripts/Character/Animation/Config/`。
- [x] 7.3 保持动画播放外观在 `Assets/Scripts/Character/Animation/Runtime/`。
- [x] 7.4 如新增编辑器，放在 `Assets/Scripts/Character/Animation/Editor/`。
- [x] 7.5 保持移动状态机纯模型在 `Assets/Scripts/Character/Movement/Model/`。
- [x] 7.6 保持移动状态机求解在 `Assets/Scripts/Character/Movement/Solver/`。
- [x] 7.7 保持移动主链 MonoBehaviour 在 `Assets/Scripts/Character/Movement/Runtime/`。
- [x] 7.8 保持项目侧移动动画配置资产在 `Assets/Configs/3C/Locomotion/`。
- [x] 7.9 保持 Animancer TransitionLibrary 和 TransitionAsset 在 `Assets/Configs/3C/Animacer/<角色>/`。
- [x] 7.10 不新增 `Resources` 隐式加载路径。
- [x] 7.11 不新增全局单例 Catalog。

## 8. 轻量编辑器和校验

- [x] 8.1 评估是否需要新增 `RunLocomotionAnimationConfigSOEditor`。
- [x] 8.2 如果新增 editor，只显示 alias 和 RunEnd exit duration。
- [x] 8.3 editor 或 validator 报告空 alias。
- [x] 8.4 editor 或 validator 报告缺失 RunEnd exit duration 且要求强校验的情况。
- [x] 8.5 editor 不读取或修改 Animancer TransitionAsset 的 fade。
- [x] 8.6 editor 不读取或修改 Animancer TransitionAsset 的 speed。
- [x] 8.7 editor 不读取或修改 Animancer TransitionAsset 的 normalized start time。
- [x] 8.8 editor 不做 Timeline 轨道。
- [x] 8.9 editor 不做运行时状态写入。

## 9. 自动测试

- [x] 9.1 测试 Run 配置默认 alias 为 `Idle / RunStart / RunLoop / RunEnd`。
- [x] 9.2 测试 Run 配置不暴露 fade duration。
- [x] 9.3 测试 Run 配置不暴露 speed。
- [x] 9.4 测试 Run 配置不暴露 normalized start time。
- [x] 9.5 测试 RunEnd exit duration override 生效。
- [x] 9.6 测试 RunEnd exit duration 缺失时 fallback 到 `moveStopMinTime`。
- [x] 9.7 测试 `MoveStop` 未到 exit duration 时保持 `MoveStop`。
- [x] 9.8 测试 `MoveStop` 到 exit duration 后回 `Idle`。
- [x] 9.9 测试 `MoveStop` 中重新输入立即回 `MoveStart`。
- [x] 9.10 测试配置校验报告空 alias。
- [x] 9.11 测试配置校验报告缺失必需 RunEnd exit duration。
- [x] 9.12 静态测试 Presenter 不引用状态图 builder。
- [x] 9.13 静态测试 Presenter 不引用具体运动执行实现。
- [x] 9.14 静态测试状态机不引用 Animancer。
- [x] 9.15 静态测试状态机不引用 AnimationClip。
- [x] 9.16 静态测试状态机不引用 TransitionLibrary。
- [x] 9.17 静态测试没有恢复旧 `LocomotionAnimationSetSO` 运行时引用。

## 10. 验证命令

- [x] 10.1 运行 `openspec validate refactor-locomotion-animation-config-boundaries --strict --no-interactive`。
- [x] 10.2 刷新 Unity 并确认 Console 没有 C# 编译错误。
- [x] 10.3 运行 Unity EditMode 定向测试 `PlayerLocomotionControllerTests`。
- [x] 10.4 运行静态搜索确认边界。
- [x] 10.5 如果 Unity MCP 或测试不可用，记录原因和手动验证步骤，不伪造结果。

## 11. 手动端到端验证

- [x] 11.1 打开当前演示场景。
- [x] 11.2 确认角色 prefab 引用 Run 配置资产。
- [x] 11.3 确认 Animancer TransitionLibrary alias 能找到 `Idle / RunStart / RunLoop / RunEnd`。
- [x] 11.4 持续输入移动，确认播放 `RunStart` 后进入 `RunLoop`。
- [x] 11.5 松开输入，确认播放 `RunEnd`。
- [x] 11.6 不再输入，确认按 RunEnd exit duration 回到 `Idle`。
- [x] 11.7 在 RunEnd 中途重新输入，确认立即进入 `MoveStart` 并播放 `RunStart`。
- [x] 11.8 修改 Animancer `Corin_RunEnd.asset` 的 fade，确认表现变化来自 Animancer。
- [x] 11.9 修改 Run 配置的 RunEnd exit duration，确认逻辑回 Idle 时间变化。

