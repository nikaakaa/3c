## 1. 现状确认
- [x] 1.1 读取本变更 `proposal.md`、`design.md` 和全部 spec delta。
- [x] 1.2 确认 `refactor-fullbody-frame-pipeline` 的 phase pipeline 已是当前 FullBody 主线。
- [x] 1.3 确认 `refactor-locomotion-decision-pipeline` 的 Locomotion facts 可被 FullBody pipeline 直接消费。
- [x] 1.4 搜索 `new CharacterStateMachineRunner`，列出所有运行时创建点。
- [x] 1.5 搜索 `LocomotionTickAdapter` 和 `FullBodyActionTickAdapter`，列出当前场景/测试/代码引用。
- [x] 1.6 搜索 `DodgeActionConfig.Default`、`ResolveRunAnimationConfig`、`ResolveMovementConfig`、`ResolveStateMachineDefinition`，列出 fallback 路径。
- [x] 1.7 确认不引入完整 HFSM active stack、并行层或第二角色控制器。

## 2. 静态测试先行
- [x] 2.1 新增静态测试：正式运行时代码中只有 `PlayerFullBodyActionController` 创建 `CharacterStateMachineRunner`。
- [x] 2.2 新增静态测试：`PlayerLocomotionController` 不包含 `new CharacterStateMachineRunner`。
- [x] 2.3 新增静态测试：`PlayerLocomotionController` 不通过无参/内部 runner 入口推进状态机。
- [x] 2.4 新增静态测试：正式 Sandbox/Prefab 不同时启用 `LocomotionTickAdapter` 与 `FullBodyActionTickAdapter`。
- [x] 2.5 新增静态测试：runtime 不再读取 `DodgeActionConfig.Default` 作为缺配置运行路径。
- [x] 2.6 新增静态测试：旧平铺字段不覆盖 `CharacterConfigSO` 正式子配置。

## 3. FullBody 配置入口收口
- [x] 3.1 给 `PlayerFullBodyActionController` 增加或接入正式 `CharacterConfigSO` 根配置来源。
- [x] 3.2 状态机定义只从正式 `CharacterConfigSO.StateMachine` 或批准的 FullBody 正式配置入口解析。
- [x] 3.3 缺状态机配置时输出诊断错误并停止状态机 tick。
- [x] 3.4 将 `ResolveDodgeActionConfig` 改为显式 `TryResolveDodgeActionConfig` 或等价结果类型。
- [x] 3.5 缺 `DodgeActionConfigSO` 时输出诊断错误，不返回 `DodgeActionConfig.Default`。
- [x] 3.6 更新 `FullBodyFramePipeline` 中 Dodge 配置调用点，缺配置时本帧不进入 Dodge。
- [x] 3.7 更新 Action interrupt policy 校验，缺正式配置时报告错误且测试可断言。

## 4. Locomotion 退为 adapter
- [x] 4.1 移除 `PlayerLocomotionController` 内部 `CharacterStateMachineRunner` 字段或将其降级为测试不可达遗留。
- [x] 4.2 移除 `TryEnsureStateMachine` 的正式运行时调用链。
- [x] 4.3 `OnEnable` 不再因为自身状态机配置缺失而创建或禁用 Locomotion adapter。
- [x] 4.4 `TryEvaluateLocomotion` 改为非正式 helper、删除，或要求外部显式传入 runner。
- [x] 4.5 保留 `TryPrepareDecisionFrame`、`TryEvaluatePreparedGameplayDecision`、`TryBuildMotionFromStateDecision` 等 FullBody pipeline 调用点。
- [x] 4.6 `ActiveStatePath` 等调试读取改为来自最近一次 FullBody/state frame 缓存或明确返回空。
- [x] 4.7 确认 Locomotion adapter 仍不直接调用 Action interrupt policy 或动作配置。

## 5. Tick driver 收口
- [x] 5.1 将当前角色正式 simulation tick 入口收口到 `FullBodyActionTickAdapter`。
- [x] 5.2 `FullBodyActionTickAdapter` 保持注册固定 phase，并禁用 controller frame auto update。
- [x] 5.3 `LocomotionTickAdapter` 从当前角色正式装配中移除或改为迁移诊断组件。
- [x] 5.4 如果保留 `LocomotionTickAdapter`，启用时必须报告“旧 Locomotion tick 入口已退役”并不得推进 gameplay。
- [x] 5.5 更新或删除依赖 `LocomotionTickAdapter` 正式驱动的测试。
- [x] 5.6 增加测试覆盖 FullBody tick 下普通 WASD 仍只提交一次 Locomotion motion。

## 6. 移除配置 fallback
- [x] 6.1 `PlayerLocomotionController.ResolveRunAnimationConfig` 不再退到旧 `runAnimationConfig` 或 presenter 配置。
- [x] 6.2 `PlayerLocomotionController.ResolveMovementConfig` 不再退到旧 `config`。
- [x] 6.3 `PlayerLocomotionController.ResolveStateMachineDefinition` 不再退到旧 `stateMachineDefinition`。
- [x] 6.4 旧字段保留序列化但标记为迁移遗留，不作为正式 runtime 解析来源。
- [x] 6.5 更新 `CharacterConfigRootTests`：删除 fallback 成功测试，增加缺正式配置诊断测试。
- [x] 6.6 更新 Sandbox/Prefab，确保正式 `CharacterConfigSO` 和所有必需子配置已绑定。

## 7. 删除过渡期旧输出路径
- [x] 7.1 删除未调用的 `PlayerFullBodyActionController.ApplyStateFrameOutputs`。
- [x] 7.2 增加静态测试确认 FullBody 输出顺序只存在 `FullBodyFramePipeline` 一处。
- [x] 7.3 确认删除不影响现有日志关键字。

## 8. 自动验证
- [x] 8.1 运行 `dotnet build .\Assembly-CSharp.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [x] 8.2 运行 `dotnet build .\Assembly-CSharp-Editor.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [x] 8.3 使用 Unity Test Runner 定向运行统一状态机 EditMode 测试。
- [x] 8.4 使用 Unity Test Runner 定向运行 FullBody frame pipeline 测试。
- [x] 8.5 使用 Unity Test Runner 定向运行 CharacterConfig root 配置测试。
- [x] 8.6 使用 Unity Test Runner 定向运行 simulation tick / FullBody tick adapter 测试。
- [x] 8.7 读取 Unity Console，确认新增配置缺失/旧入口诊断只在对应测试中出现，正常场景 error 为 0。
- [x] 8.8 运行 `openspec validate refactor-state-machine-runtime-authority --strict --no-interactive`。
- [x] 8.9 不运行 Unity batchmode。

## 9. 手动验证
- [x] 9.1 打开 Sandbox 场景。
- [x] 9.2 确认当前角色只有 `FullBodyActionTickAdapter` 或等价 FullBody 正式 driver active。
- [x] 9.3 确认当前角色正式绑定 `CharacterConfigSO` 及必需子配置。
- [ ] 9.4 WASD 移动仍进入 Idle、MoveStart、MoveLoop、MoveStop。
- [ ] 9.5 RunLoop 反向输入仍进入 TurnBack，并保持 motion/input lock 语义。
- [ ] 9.6 Shift Dodge Directional 和 Backstep 仍可触发并恢复 Locomotion。
- [ ] 9.7 故意移除一个必需配置，确认系统报清晰诊断且不使用 fallback 继续运行。
- [ ] 9.8 观察诊断日志，确认每帧状态路径来自 FullBody runner。

## 10. 收尾
- [x] 10.1 检查没有新增第二套状态机 runner owner。
- [x] 10.2 检查没有新增 fallback 配置加载。
- [x] 10.3 检查没有新增绕过 motion executor 的运动路径。
- [x] 10.4 检查没有删除用户未要求删除的 log。
- [x] 10.5 更新相关调试文档或 Path 文档中关于正式 driver / runtime authority 的说明。
- [ ] 10.6 全部任务真实完成后再将 checklist 标为 `- [x]`。
