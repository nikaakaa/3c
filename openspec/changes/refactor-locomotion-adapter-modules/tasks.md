## 1. 现状确认
- [ ] 1.1 读取本变更 `proposal.md`、`design.md` 和全部 spec delta。
- [ ] 1.2 确认 `refactor-state-machine-runtime-authority` 的唯一 runner owner 方向已被当前代码实现。
- [ ] 1.3 确认 `formalize-animation-playback-rollback-authority` 是否仍在修改动画 playback restore 相关文件。
- [ ] 1.4 统计 `PlayerLocomotionController` 当前方法、日志、公开 API 和直接调用点。
- [ ] 1.5 搜索 `TickFromInputSource`、`TryEvaluateLocomotion`、`LocomotionTickAdapter` 的外部引用。
- [ ] 1.6 搜索 `new CharacterStateMachineRunner`，确认本变更前只有 FullBody controller 是正式 runtime 创建点。
- [ ] 1.7 搜索 `DodgeActionConfig.Default`、旧平铺配置字段读取和 fallback 路径。

## 2. 测试先行
- [ ] 2.1 新增静态测试：`PlayerLocomotionController` 不创建 `CharacterStateMachineRunner`。
- [ ] 2.2 新增静态测试：拆出的 Locomotion 模块不引用 `MonoBehaviour`、`CharacterController`、Animancer runtime 或 InputAction。
- [ ] 2.3 新增静态测试：拆出的 Locomotion 模块不注册 simulation tick driver。
- [ ] 2.4 新增静态测试：退役直驱 API 不参与正式 runtime 调用链。
- [ ] 2.5 新增日志 key 测试：关键 Locomotion / TurnBack eventId 在迁移前后保持。
- [ ] 2.6 新增 characterization 测试：同一输入和 snapshot 下，拆分前后 locomotion facts 一致。
- [ ] 2.7 新增 characterization 测试：同一 TurnBack 输入下，拆分前后 intent 结果一致。
- [ ] 2.8 新增 characterization 测试：同一 state frame 和 playback facts 下，拆分前后 motion facts 一致。

## 3. Locomotion facts 模块
- [ ] 3.1 创建 `LocomotionFactsBuilder` 或等价纯逻辑类型。
- [ ] 3.2 将移动意图解析迁移到 builder。
- [ ] 3.3 将相机/facing 空间事实解析迁移到 builder，Unity 引用仍由 controller 传入纯数据。
- [ ] 3.4 将 locomotion facts 派生迁移到 builder。
- [ ] 3.5 将 state machine context 构建迁移到 builder。
- [ ] 3.6 保持 `PlayerLocomotionController.TryPrepareDecisionFrame` 的外部调用形状。
- [ ] 3.7 运行 locomotion facts 定向测试。

## 4. TurnBack intent 模块
- [ ] 4.1 创建 `TurnBackIntentResolver` 或等价纯逻辑类型。
- [ ] 4.2 迁移 TurnBack reference facing 解析。
- [ ] 4.3 迁移 TurnBack intent 角度、阈值和 clear 规则。
- [ ] 4.4 保持 `LocomotionTurnBackIntent` 输出模型不变。
- [ ] 4.5 运行 TurnBack intent 定向测试。

## 5. TurnBack motion 模块
- [ ] 5.1 创建 `TurnBackMotionResolver` 或等价纯逻辑类型。
- [ ] 5.2 迁移 TurnBack motion window / input lock 解析。
- [ ] 5.3 迁移 entry-local/world/local delta 计算。
- [ ] 5.4 迁移 baked motion profile sample 读取边界，不改变 playback restore 语义。
- [ ] 5.5 迁移 yaw delta 和 suppress input 输出。
- [ ] 5.6 保持 `BasicMovementMotionFacts` 输出一致。
- [ ] 5.7 运行 TurnBack motion 定向测试。

## 6. 状态输出到 Locomotion frame
- [ ] 6.1 创建 `LocomotionStateMotionBuilder` 或等价纯逻辑类型。
- [ ] 6.2 迁移 `CharacterStateMachineFrame` 到 `BasicLocomotionFrame` 的构建逻辑。
- [ ] 6.3 保持 `ExecuteBasicMovement` 和 `PresentLocomotionAnimation` 的 owner 判断在 FullBody pipeline 外层。
- [ ] 6.4 确认 builder 不直接调用 motion executor 或 presenter。
- [ ] 6.5 运行 FullBody pipeline / Locomotion frame 定向测试。

## 7. Snapshot adapter
- [ ] 7.1 创建 `LocomotionSnapshotAdapter` 或等价协作类型。
- [ ] 7.2 迁移 Locomotion phase、gait、run latch、world direction capture。
- [ ] 7.3 迁移 restore 调用边界，不改变动画 playback 权威。
- [ ] 7.4 保持 `CharacterRuntimeBlackboard` capture/restore 语义不变。
- [ ] 7.5 如果与 `formalize-animation-playback-rollback-authority` 冲突，暂停本节并等待该变更完成。
- [ ] 7.6 运行 rollback foundation / fullbody replay 定向测试。

## 8. Diagnostics 模块
- [ ] 8.1 创建 `LocomotionDiagnostics` 或等价日志提交类型。
- [ ] 8.2 迁移 state output probe 日志。
- [ ] 8.3 迁移 locomotion facts 日志。
- [ ] 8.4 迁移 TurnBack intent / root motion / state policy 日志。
- [ ] 8.5 迁移 retired direct tick 和 missing config 错误日志。
- [ ] 8.6 保持现有 eventId、level 和关键消息文本。
- [ ] 8.7 运行日志 key 定向测试。

## 9. 退役壳清理
- [ ] 9.1 基于静态搜索列出 `TickFromInputSource` 外部调用点。
- [ ] 9.2 基于静态搜索列出 `TryEvaluateLocomotion` 外部调用点。
- [ ] 9.3 若无正式外部调用，删除或隔离退役直驱 API。
- [ ] 9.4 若仍有测试/工具调用，保留 `[Obsolete]` facade 并保持只诊断不推进 gameplay。
- [ ] 9.5 审计 `LocomotionTickAdapter` 是否可移动到诊断/迁移目录。
- [ ] 9.6 确认没有恢复 `LocomotionTickAdapter` 正式 driver 注册。

## 10. 自动验证
- [ ] 10.1 运行 `dotnet build .\Assembly-CSharp.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [ ] 10.2 运行 `dotnet build .\Assembly-CSharp-Editor.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [ ] 10.3 使用 Unity Test Runner 运行 `Tests.Editor.UnifiedCharacterStateMachineTests`。
- [ ] 10.4 使用 Unity Test Runner 运行 `Tests.Editor.CharacterConfigRootTests`。
- [ ] 10.5 使用 Unity Test Runner 运行 `ThirdPersonSimulation.Tests.FullBodyRollbackReplayTests`。
- [ ] 10.6 使用 Unity Test Runner 运行新增 Locomotion module 边界测试。
- [ ] 10.7 读取 Unity Console，确认正常场景 error 为 0。
- [ ] 10.8 运行 `openspec validate refactor-locomotion-adapter-modules --strict --no-interactive`。
- [ ] 10.9 不运行 Unity batchmode。

## 11. 手动验证
- [ ] 11.1 打开 Sandbox 场景。
- [ ] 11.2 确认当前角色仍只有 FullBody 正式 driver active。
- [ ] 11.3 WASD 移动仍进入 Idle、MoveStart、MoveLoop、MoveStop。
- [ ] 11.4 RunLoop 反向输入仍进入 TurnBack。
- [ ] 11.5 TurnBack motion/input lock 语义不变。
- [ ] 11.6 Shift Dodge Directional 和 Backstep 仍可触发并恢复 Locomotion。
- [ ] 11.7 打开诊断日志后确认关键 Locomotion / TurnBack 日志仍可定位。

## 12. 收尾
- [ ] 12.1 检查 `PlayerLocomotionController` 只保留 facade、Unity 装配和状态缓存职责。
- [ ] 12.2 检查拆出的模块职责单一，且不持有 Unity 场景对象。
- [ ] 12.3 检查没有新增第二套 runner owner。
- [ ] 12.4 检查没有新增 fallback 配置加载。
- [ ] 12.5 检查没有新增绕过 motion executor 的运动路径。
- [ ] 12.6 检查没有删除用户未要求删除的 log。
- [ ] 12.7 更新相关调试文档或 Path 文档中关于 Locomotion adapter 模块边界的说明。
- [ ] 12.8 全部任务真实完成后再将 checklist 标为 `- [x]`。
