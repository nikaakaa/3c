## 1. 现状确认
- [ ] 1.1 搜索运行时代码中 `RequestPriorityAtLeast` 的所有引用。
- [ ] 1.2 搜索配置资产中 `kind: 5` 或等价 RequestPriority 条件的所有引用。
- [ ] 1.3 搜索测试中对 `RequestPriorityAtLeast` 的所有断言和 fixture。
- [ ] 1.4 确认 `CharacterStateTransitionDefinition.Priority` 只用于 transition 选择顺序。
- [ ] 1.5 确认默认 `Locomotion/* -> Dodge` transition 只使用 `HasInputRequest(Dodge)`。
- [ ] 1.6 记录 `PlayerFullBodyActionController` 当前使用 `DodgeActionConfig.Default` 的位置。
- [ ] 1.7 记录 `currentStateResistance` 当前传入 `0` 的位置。

## 2. 清理状态机动作优先级条件
- [ ] 2.1 判断是否可以完全删除 `RequestPriorityAtLeast` 条件。
- [ ] 2.2 若删除 enum 项，先固定现有 enum 数值或确认资产无需迁移。
- [ ] 2.3 从 `CharacterStateTransitionConditionKind` 移除或废弃 `RequestPriorityAtLeast`。
- [ ] 2.4 从 `CharacterStateTransitionCondition` 移除或废弃 `RequestPriorityAtLeast` factory。
- [ ] 2.5 从 `CharacterStateTransitionEvaluator` 移除该 evaluator 分支。
- [ ] 2.6 保留 `CharacterStateTransitionDefinition.Priority`。
- [ ] 2.7 更新默认状态机相关测试命名，使其验证“状态机不承载动作请求 priority 准入”。

## 3. 统一 Dodge 配置来源
- [ ] 3.1 新增或确认默认 `DodgeActionConfigSO` 资产。
- [ ] 3.2 在默认角色 prefab 上绑定 `DodgeActionConfigSO`。
- [ ] 3.3 为 `PlayerFullBodyActionController` 增加 Dodge 配置序列化入口。
- [ ] 3.4 增加 `ResolveDodgeActionConfig` 或等价方法，输出纯 `DodgeActionConfig`。
- [ ] 3.5 将 gate 调用中的 `DodgeActionConfig.Default` 改为解析后的配置。
- [ ] 3.6 将策略校验中的 `DodgeActionConfig.Default` 改为解析后的配置。
- [ ] 3.7 缺失配置时输出校验错误或 warning，运行时保守 fallback 不作为正式配置。
- [ ] 3.8 确认 Dodge 的方向/后撤变体解析仍由 Dodge 请求构建或 Dodge resolver 负责。
- [ ] 3.9 确认 Dodge 的动作位移、转向、run latch 和返回 Locomotion 规则仍由统一状态机输出负责。
- [ ] 3.10 确认 Dodge 作为 FullBody Action 管线实例运行，没有新增第二条准入或输出管线。

## 4. 接入当前 Action resistance
- [ ] 4.1 新增纯逻辑 resistance resolver 或等价方法。
- [ ] 4.2 resolver 从 `CharacterStateMachineSnapshot` 读取当前 Action state。
- [ ] 4.3 resolver 在 Locomotion 或空 Action 时返回 `Action.None` 和 resistance `0`。
- [ ] 4.4 resolver 在 `Action.Dodge` 时返回 `Action.Dodge` 和 `dodgeConfig.Resistance`。
- [ ] 4.5 resolver 不读取 Animator、Animancer、Input System、CharacterController 或 BBB 类型。
- [ ] 4.6 `PlayerFullBodyActionController` 调 gate 前解析当前 resistance。
- [ ] 4.7 `FullBodyActionInterruptGate` 使用真实 resistance 创建 `ActionInterruptContext`。
- [ ] 4.8 确认 `ActionRuntimeStateTracker` 不成为状态权威；若使用 tracker，只由 snapshot 同步。

## 5. 策略配置验证
- [ ] 5.1 确认默认 policy set 仍包含 `Action.None -> Action.Dodge`。
- [ ] 5.2 如需连续 Dodge 手动验证，新增或临时配置 `Action.Dodge -> Action.Dodge` 策略。
- [ ] 5.3 测试缺失 `Action.None -> Action.Dodge` policy 时 Dodge 请求 rejected 且输入保留。
- [ ] 5.4 测试缺失 `DodgeActionConfigSO` 时配置校验能报告问题。
- [ ] 5.5 测试配置 priority 低于 policy minPriority 时不会进入 Dodge。
- [ ] 5.6 测试配置 resistance 能影响 `Action.Dodge -> Action.Dodge` 的仲裁结果。

## 6. 自动测试
- [ ] 6.1 更新 `UnifiedCharacterStateMachineTests`，确认默认状态机定义不包含动作请求 priority 条件。
- [ ] 6.2 更新资产静态测试，确认默认状态机资产没有 `kind: 5` 或等价动作请求 priority 条件。
- [ ] 6.3 增加测试：controller 使用 `DodgeActionConfigSO` 的 priority 构建 Dodge request。
- [ ] 6.4 增加测试：controller 使用 `DodgeActionConfigSO` 的 resistance 构建仲裁上下文。
- [ ] 6.5 增加测试：当前为 Locomotion 时 resistance 为 `0`。
- [ ] 6.6 增加测试：当前为 `Action.Dodge` 时 resistance 为 Dodge 配置值。
- [ ] 6.7 增加测试：非 force 的连续 Dodge 在 priority 小于等于 resistance 时 rejected。
- [ ] 6.8 增加测试：force policy 可绕过当前 resistance。
- [ ] 6.9 增加测试：Dodge 仍能按输入解析 Directional 和 Backstep 变体。
- [ ] 6.10 增加测试：Dodge 结束仍按移动意图返回 MoveLoop 或 Idle。
- [ ] 6.11 静态测试确认状态机 runner 和 transition evaluator 不引用 `ActionInterruptArbiter` 或 `ActionInterruptPolicySetSO`。
- [ ] 6.12 静态测试确认 action resolver/gate 不引用 Animancer、Animator、CharacterController、Cinemachine、Input System 或 `BBBNexus`。

## 7. 手动验证
- [ ] 7.1 在 Unity Editor 中打开默认角色 prefab，确认绑定了 `DodgeActionConfigSO` 和 `ActionInterruptPolicySetSO`。
- [ ] 7.2 Play Mode 中按方向 + Shift，确认从 Locomotion 进入 Directional Dodge。
- [ ] 7.3 Play Mode 中无方向 + Shift，确认进入 Backstep Dodge。
- [ ] 7.4 临时配置 `Action.Dodge -> Action.Dodge` 且 non-force，确认连续 Shift 被 resistance 挡住时日志为 `BlockedByResistance`。
- [ ] 7.5 临时将同策略设为 force，确认连续 Shift 可被 accepted。
- [ ] 7.6 确认 WASD 的 Idle、MoveStart、MoveLoop、MoveStop 没有依赖 Dodge 配置或 policy set。

## 8. 验证命令
- [ ] 8.1 运行 `openspec validate refactor-action-interrupt-entry-cleanup --strict --no-interactive`。
- [ ] 8.2 运行 `dotnet build .\Assembly-CSharp.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [ ] 8.3 运行 `dotnet build .\Assembly-CSharp-Editor.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [ ] 8.4 运行定向 Unity EditMode 测试：`ActionInterruptArbiterTests`。
- [ ] 8.5 运行定向 Unity EditMode 测试：`ActionRuntimeStateTrackerTests`。
- [ ] 8.6 运行定向 Unity EditMode 测试：`UnifiedCharacterStateMachineTests`。
